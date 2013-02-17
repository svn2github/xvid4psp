using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using SharpDX.DirectSound;
using SharpDX.Multimedia;

namespace XviD4PSP
{
    public delegate void AvsPlayerFinished(object sender);
    public delegate void AvsPlayerError(object sender, Exception ex);

    public class AviSynthPlayer
    {
        public enum PlayState { Stopped, Paused, Running, Init }
        public PlayState PlayerState = PlayState.Init;
        public event AvsPlayerFinished PlayerFinished;
        public event AvsPlayerError PlayerError;

        private static object locker = new object();
        private ManualResetEvent playing_v = new ManualResetEvent(false);
        private ManualResetEvent processing = new ManualResetEvent(true);
        private AviSynthReader reader = null;
        private Thread thread_v = null;
        private string script = null;
        private Window Owner = null;

        public bool HasVideo = false;
        public bool HasAudio = false;
        public bool IsError = false;
        private bool IsAborted = false;
        private bool IsInterop = false;
        private bool ResetTimer = false;
        public bool AllowDropFrames = false;
        public bool EnableAudio = false;
 
        private int Width = 0;
        private int Height = 0;
        public int TotalFrames = 0;
        public double Framerate = 0;
        public int CurrentFrame = 0;   //Номер кадра, который точно был показан
        private int _CurrentFrame = 0; //Номер кадра при расчетах в VideoLoop

        private int stride = 0;
        private int bufSize = 0;
        private IntPtr MemSection = IntPtr.Zero;
        private IntPtr VBuffer = IntPtr.Zero;
        public ImageSource BitmapSource = null;
        private ThreadStart UpdateDelegate = null;
        private Dispatcher OwnerDispatcher = null;
        public DispatcherPriority Priority = DispatcherPriority.Normal;

        [DllImport("kernel32.dll", EntryPoint = "CreateFileMapping", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", EntryPoint = "MapViewOfFile", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", EntryPoint = "UnmapViewOfFile", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        //Audio
        private DirectSound AudioDevice = null;
        private SecondarySoundBuffer AudioBuffer = null;
        private SoundBufferDescription BufferDesc;

        private int bytesPerSample = 0;
        private const double k = 1.0;       //Установка продолжительности буфера (0.5=0.5сек., 1.0=1сек., ..)
        private int samplesPerHalfBuff = 0; //Сэмплов на половину буфера
        private byte[] ABuffer;             //Промежуточный буфер, размером в половину основного
        private GCHandle h;

        private int Samplerate = 0;
        private long TotalSamples = 0;
        private long CurrentSample = 0;
        private int LastSamples = -1;
        private Thread thread_a = null;
        private bool ResetAudio = false;

        private static object locker_a = new object();
        private ManualResetEvent playing_a = new ManualResetEvent(false);
        public int Volume { set { if (AudioBuffer != null) try { AudioBuffer.Volume = value; } catch (Exception ex) { SetError(ex); } } }

        //Timer
        private uint _period = 0;
        private const uint period = 5; //~5ms достаточно, тем-более что наверняка кем-то где-то уже будет выставлено меньше

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint timeEndPeriod(uint period);
        [DllImport("winmm.dll", EntryPoint = "timeGetDevCaps")]
        private static extern uint timeGetDevCaps(ref TimeCaps timeCaps, uint sizeTimeCaps);
        [StructLayout(LayoutKind.Sequential)]
        private struct TimeCaps
        {
            public uint wPeriodMin;
            public uint wPeriodMax;
        };

        public AviSynthPlayer(Window owner)
        {
            Owner = owner;
            OwnerDispatcher = owner.Dispatcher;
        }

        public void Open(string scriptPath)
        {
            try
            {
                script = scriptPath;
                LoadAviSynth();

                if (HasVideo)
                {
                    try
                    {
                        //Framework 3.0 с SP1+
                        CreateInteropBitmap();
                        IsInterop = true;
                    }
                    catch (TypeLoadException)
                    {
                        //Framework 3.0 без SP1
                        CreateWriteableBitmap();
                        IsInterop = false;
                    }

                    CreateVideoThread();
                }

                if (HasAudio)
                {
                    SetUpAudioDevice();
                    CreateAudioThread();
                }

                #region ShowPictureViewInfo
                if (HasVideo && reader.GetVarBoolean("ShowPictureViewInfo", false))
                {
                    SpeakerConfiguration conf = 0; SpeakerGeometry geom = 0;
                    if (HasAudio) AudioDevice.GetSpeakerConfiguration(out conf, out geom);

                    Stopwatch sw = Stopwatch.StartNew();
                    for (int i = 0; i < 100; i++) Thread.Sleep(1);
                    sw.Stop();

                    Message mes = new Message(Owner);
                    mes.ShowMessage("Video\r\n  Resolution: " + reader.Width + "x" + reader.Height + ", FrameRate: " + reader.Framerate + ", Format: " +
                        reader.Clip.OriginalColorspace + ((reader.Clip.OriginalColorspace != AviSynthColorspace.RGB32) ? "->RGB32" : "") + " \r\n  Output: " +
                        ((IsInterop) ? "InteropBitmap" : "WriteableBitmap (Install SP1 for .NET Framework 3.0!)") + "\r\n\r\nAudio\r\n" +
                        ((HasAudio) ? "  Bits: " + BufferDesc.Format.BitsPerSample + ((reader.Clip.SampleType == AudioSampleType.FLOAT) ? " FLOAT" : "") +
                        ", SampleRate: " + BufferDesc.Format.SampleRate + ", Channels: " + BufferDesc.Format.Channels + ((BufferDesc.Format.Encoding == WaveFormatEncoding.Extensible) ?
                        ", Mask: " + ((WaveFormatExtensible)BufferDesc.Format).ChannelMask + " (" + (int)((WaveFormatExtensible)BufferDesc.Format).ChannelMask + ")" : "") +
                        "\r\n  PrimaryBuffers: " + AudioDevice.Capabilities.PrimaryBuffers + ", MixingBuffers: " + AudioDevice.Capabilities.MaxHardwareMixingAllBuffers +
                        " (" + AudioDevice.Capabilities.FreeHardwareMixingAllBuffers + "), 3DBuffers: " + AudioDevice.Capabilities.MaxHardware3DAllBuffers + " (" +
                        AudioDevice.Capabilities.FreeHardware3DAllBuffers + "), MemBytes: " + AudioDevice.Capabilities.TotalHardwareMemBytes + " (" +
                        AudioDevice.Capabilities.FreeHardwareMemBytes + "), SampleRate: " + AudioDevice.Capabilities.MinSecondarySampleRate + " - " +
                        AudioDevice.Capabilities.MaxSecondarySampleRate + (((AudioDevice.Capabilities.Flags & CapabilitiesFlags.ContinousRate) > 0) ? " (continuous)" : "") +
                        "\r\n  SpeakerConfiguration: " + conf + ", SpeakerGeometry: " + geom + "\r\n" : "  None" + ((!EnableAudio) ? " (disabled)" : "") + "\r\n") +
                        "\r\nTimers\r\n  Sleep(100): " + sw.ElapsedMilliseconds + ", HighResolutionStopwatch: " + Stopwatch.IsHighResolution, Languages.Translate("Info"));
                }
                #endregion
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private void LoadAviSynth()
        {
            if (reader == null)
            {
                reader = new AviSynthReader(AviSynthColorspace.RGB32, AudioSampleType.Undefined);
                reader.OpenScript(script);

                if (!reader.Clip.HasVideo || reader.FrameCount == 0)
                {
                    HasVideo = false;
                }
                else
                {
                    HasVideo = true;
                    Width = reader.Width;
                    Height = reader.Height;
                    TotalFrames = reader.FrameCount;
                    Framerate = reader.Framerate;
                }

                if (!EnableAudio || reader.SamplesCount == 0 || reader.Samplerate == 0 || reader.Channels == 0)
                {
                    HasAudio = false;
                }
                else
                {
                    HasAudio = true;
                    Samplerate = reader.Samplerate;
                    TotalSamples = reader.SamplesCount;
                }
            }
        }

        private void CreateInteropBitmap()
        {
            if (BitmapSource == null)
            {
                //Из-за бага в InteropBitmap вместо RGB24 придется использовать RGB32 (будет чуть медленнее).
                //Иначе картинка не будет обновляться при Invalidate - баг исправлен в каком-то новом Framework`е.
                PixelFormat format = PixelFormats.Bgr32;
                stride = Width * (format.BitsPerPixel / 8);
                bufSize = stride * Height;

                if (MemSection == IntPtr.Zero)
                {
                    MemSection = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, (uint)bufSize, null);
                    if (MemSection == IntPtr.Zero) throw new Exception("CreateFileMapping: " + new Win32Exception().Message);
                }

                if (VBuffer == IntPtr.Zero)
                {
                    VBuffer = MapViewOfFile(MemSection, 0xF001F, 0, 0, (uint)bufSize);
                    if (VBuffer == IntPtr.Zero) throw new Exception("MapViewOfFile: " + new Win32Exception().Message);
                }

                BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(MemSection, Width, Height, format, stride, 0) as InteropBitmap;
                UpdateDelegate = () => { try { if (BitmapSource != null) ((InteropBitmap)BitmapSource).Invalidate(); } catch (Exception) { } };
            }
        }

        private void CreateWriteableBitmap()
        {
            if (BitmapSource == null)
            {
                //WriteableBitmap в 3.0 без SP1 тоже работает только с RGB32, плюс оно само по себе медленнее.
                //Обновление через BackBuffer работает быстрее, чем через WritePixels, но для BackBuffer нужен SP2.
                PixelFormat format = PixelFormats.Bgr32;
                stride = Width * (format.BitsPerPixel / 8);
                bufSize = stride * Height;

                if (VBuffer == IntPtr.Zero)
                    VBuffer = Marshal.AllocHGlobal(bufSize);

                Int32Rect WBRect = new Int32Rect(0, 0, Width, Height);
                BitmapSource = new System.Windows.Media.Imaging.WriteableBitmap(Width, Height, 0, 0, format, null);
                UpdateDelegate = () => { try { if (BitmapSource != null) ((WriteableBitmap)BitmapSource).WritePixels(WBRect, VBuffer, bufSize, stride); } catch (Exception) { } };
            }
        }

        private void CreateVideoThread()
        {
            if (thread_v == null)
            {
                thread_v = new Thread(new ThreadStart(VideoLoop));
                //thread_v.Priority = ThreadPriority.AboveNormal;
                thread_v.Start();
            }
        }

        private void VideoLoop()
        {
            Stopwatch timer = new Stopwatch();
            long ticksPerSecond = Stopwatch.Frequency;
            long ticksPerFrame = (long)(ticksPerSecond * (1.0 / Framerate));
            long ticksToNextFrame = 0;

            while (!IsError && !IsAborted)
            {
                playing_v.WaitOne();
                if (IsError || IsAborted)
                    return;

                //После простоя
                if (ResetTimer)
                {
                    timer.Reset();
                    timer.Start();
                    ticksToNextFrame = 0;
                    ResetTimer = false;
                }

                processing.Reset();
                ShowFrame(_CurrentFrame);

                ticksToNextFrame += ticksPerFrame;
                Interlocked.Increment(ref _CurrentFrame);

                //Будем пропускать кадры?
                if (timer.ElapsedTicks >= ticksToNextFrame && AllowDropFrames)
                {
                    int drop = Convert.ToInt32((timer.ElapsedTicks - ticksToNextFrame) / ticksPerFrame);
                    Interlocked.Add(ref _CurrentFrame, drop);
                    ticksToNextFrame += drop * ticksPerFrame;
                }

                //Ждем оставшееся время перед показом следующего кадра
                while (timer.ElapsedTicks < ticksToNextFrame && !IsError && !IsAborted)
                    Thread.Sleep(1);

                processing.Set();

                //Дошли до конца видео
                if (_CurrentFrame >= TotalFrames)
                {
                    playing_v.Reset();
                    ResetTimer = true;

                    if (!IsError && !IsAborted)
                    {
                        if (PlayerFinished != null)
                        {
                            OwnerDispatcher.Invoke(Priority, (ThreadStart)delegate()
                            {
                                PlayerFinished(this);
                            });
                        }
                    }
                }
            }
        }

        private void ShowFrame(int frame)
        {
            try
            {
                Interlocked.Exchange(ref CurrentFrame, frame);
                reader.Clip.ReadFrame(VBuffer, stride, frame);

                OwnerDispatcher.BeginInvoke(Priority, UpdateDelegate);
            }
            catch (Exception ex)
            {
                processing.Set();
                SetError(ex);
            }
        }

        private void SetUpAudioDevice()
        {
            if (AudioDevice == null)
            {
                AudioDevice = new DirectSound();
                AudioDevice.SetCooperativeLevel(new WindowInteropHelper(Owner).Handle, CooperativeLevel.Normal);
            }

            if (AudioBuffer == null)
            {
                BufferDesc = new SoundBufferDescription();
                BufferDesc.Flags = BufferFlags.GlobalFocus | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume;
                BufferDesc.AlgorithmFor3D = Guid.Empty;

                //Вывод звука в Ависинте (через DirectShow\VFW)
                //v2.57   |   global OPT_AllowFloatAudio = True (по дефолту FLOAT преобразуется в 16бит, а 32бит и 24бит - выводятся как есть)
                //v2.58   |   global OPT_UseWaveExtensible = True (по дефолту WaveFormatExtensible не используется, даже для многоканального и многобитного звука)
                //v2.60   |   global OPT_dwChannelMask(int v) (переназначение дефолтной конфигурации каналов при использовании WaveFormatExtensible)
                //FFCHANNEL_LAYOUT в FFMS2

                if (reader.GetVarBoolean("OPT_UseWaveExtensible", true)) //У нас свой дефолт
                {
                    #region WaveFormatExtensible
                    WaveFormatExtensible format = new WaveFormatExtensible(reader.Samplerate, reader.BitsPerSample, reader.Channels);

                    //SharpDX считает, что весь 32-битный звук - FLOAT
                    if (reader.Clip.SampleType == AudioSampleType.INT32)
                        format.GuidSubFormat = new Guid("00000001-0000-0010-8000-00aa00389b71"); //PCM

                    #region channels
                    //AviSynth (дефолт)
                    //Chan. Mask MS channels
                    //----- ------ -----------------------
                    //1   0x0004 FC                      4
                    //2   0x0003 FL FR                   3
                    //3   0x0007 FL FR FC                7
                    //4   0x0033 FL FR BL BR             51
                    //5   0x0037 FL FR FC BL BR          55
                    //6   0x003F FL FR FC LF BL BR       63
                    //7   0x013F FL FR FC LF BL BR BC    319
                    //8   0x063F FL FR FC LF BL BR SL SR 1599

                    int mask = reader.GetVarInteger("OPT_dwChannelMask", -1);
                    if (mask != -1) format.ChannelMask = (Speakers)mask;
                    else if (reader.Channels == 1) format.ChannelMask = Speakers.Mono; //4
                    //else if (reader.Channels == 2) format.ChannelMask = Speakers.Stereo; //3
                    else if (reader.Channels == 3) format.ChannelMask = Speakers.Stereo | Speakers.FrontCenter; //7 //TwoPointOne; //11
                    else if (reader.Channels == 4) format.ChannelMask = Speakers.Quad; //51
                    else if (reader.Channels == 5) format.ChannelMask = Speakers.Quad | Speakers.FrontCenter; //55  //FourPointOne; //59
                    //else if (reader.Channels == 6) format.ChannelMask = Speakers.FivePointOne; //63
                    else if (reader.Channels == 7) format.ChannelMask = Speakers.FivePointOne | Speakers.BackCenter; //319
                    else if (reader.Channels == 8) format.ChannelMask = Speakers.SevenPointOneSurround; //1599  //SevenPointOne; //255
                    /*else //Этот способ уже был использован при вызове конструктора, хз насколько он корректный и насколько корректно всё то, что выше..
                    {
                        //NAudio\SharpDX
                        int dwChannelMask = 0;
                        for (int n = 0; n < 1; n++) dwChannelMask |= (1 << n);
                        format.ChannelMask = (Speakers)dwChannelMask;

                        //ch mask (SlimDX) [SharpDX]
                        //1    1 (FrontLeft) [FrontLeft]
                        //2    3 (Stereo) [FrontLeft | FrontRight]
                        //3    7 (Mono) [FrontLeft | FrontRight | FrontCenter]
                        //4   15 (Mono) [FrontLeft | FrontRight | FrontCenter | LowFrequency]
                        //5   31 (TwoPointOne | Mono | BackLeft) [FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft]
                        //6   63 (FivePointOne) [FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight]
                        //7  127 (FivePointOne | FrontLeftOfCenter) [FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight | FrontLeftOfCenter]
                        //8  255 (SevenPointOne) [FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight | FrontLeftOfCenter | FrontRightOfCenter]
                    }*/
                    #endregion

                    samplesPerHalfBuff = (int)((format.SampleRate / 2) * k); //Кол-во сэмплов на половину буфера
                    bytesPerSample = format.BlockAlign;

                    BufferDesc.BufferBytes = samplesPerHalfBuff * format.BlockAlign * 2; //Кол-во байт на полный буфер
                    BufferDesc.Format = format;
                    #endregion
                }
                else
                {
                    #region WaveFormat
                    WaveFormatEncoding tag = (reader.Clip.SampleType == AudioSampleType.FLOAT) ? WaveFormatEncoding.IeeeFloat : WaveFormatEncoding.Pcm;
                    WaveFormat format = WaveFormat.CreateCustomFormat(tag, reader.Samplerate, reader.Channels, reader.Clip.AvgBytesPerSec, reader.Channels * reader.Clip.BytesPerSample, reader.BitsPerSample);

                    samplesPerHalfBuff = (int)((format.SampleRate / 2) * k); //Кол-во сэмплов на половину буфера
                    bytesPerSample = format.BlockAlign;

                    BufferDesc.BufferBytes = samplesPerHalfBuff * format.BlockAlign * 2; //Кол-во байт на полный буфер
                    BufferDesc.Format = format;
                    #endregion
                }

                AudioBuffer = new SecondarySoundBuffer(AudioDevice, BufferDesc);

                if (ABuffer == null)
                    ABuffer = new byte[BufferDesc.BufferBytes / 2];

                if (!h.IsAllocated)
                    h = GCHandle.Alloc(ABuffer, GCHandleType.Pinned);
            }
        }

        private void CreateAudioThread()
        {
            if (thread_a == null)
            {
                thread_a = new Thread(new ThreadStart(AudioLoop));
                thread_a.Priority = ThreadPriority.AboveNormal;
                thread_a.Start();
            }
        }

        private void AudioLoop()
        {
            //Notify не используются из-за ихних багов..
            Stopwatch timer = new Stopwatch();
            long ticksPerSecond = Stopwatch.Frequency;
            long ticksPerStep = (long)(ticksPerSecond / 100);        //Интервал опроса позиции (DS обновляет её каждые 10ms)
            long ticksPerThird = (long)((ticksPerSecond / 3.3) * k); //"Защитный интервал" (чуть больше, чем продолжительность четверти буфера)
            int mid = ABuffer.Length;     //Середина двух половинок буфера
            int half = ABuffer.Length /2; //Первая половина первой половинки (первая четверть буфера)
            int half2 = mid + half;       //Первая половина второй половинки (третья четверть буфера)

            try
            {
                while (!IsError && !IsAborted)
                {
                    playing_a.WaitOne();
                    if (IsError || IsAborted)
                        return;

                    timer.Start();
                    ResetAudio = false;

                    if (CheckBufferIsLost(false))
                    {
                        timer.Reset();
                        continue;
                    }

                    int read = 0, write = 0;
                    AudioBuffer.GetCurrentPosition(out read, out write);
                    if (read >= 0 && read <= half || read >= mid && read <= half2) //0-2.5 || 5-7.5 [0-5-10]
                    {
                        int samplesToWait = LastSamples;
                        FillAudioBuffer(false);

                        if (samplesToWait < 0)
                        {
                            //Выжидаем "защитный интервал"
                            while (timer.ElapsedTicks < ticksPerThird && !ResetAudio && !IsError && !IsAborted)
                                Thread.Sleep(1);
                        }
                        else
                        {
                            //Даём доиграть последним семплам
                            long ticksToWait = (long)((samplesToWait / Samplerate) * ticksPerSecond);
                            while (timer.ElapsedTicks < ticksToWait && !ResetAudio && !IsError && !IsAborted)
                                Thread.Sleep(1);

                            if (IsError || IsAborted)
                                return;

                            if (LastSamples >= 0)
                            {
                                playing_a.Reset();
                                if (!ResetAudio)
                                    StopAudio();
                            }
                        }
                    }
                    else
                    {
                        while (timer.ElapsedTicks < ticksPerStep && !IsError && !IsAborted)
                            Thread.Sleep(1);
                    }

                    timer.Reset();
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private void FillAudioBuffer(bool first)
        {
            try
            {
                lock (locker_a)
                {
                    int samples = Math.Max(Math.Min((int)(TotalSamples - CurrentSample), samplesPerHalfBuff), 0);
                    if (samples < samplesPerHalfBuff)
                    {
                        //Дошли до конца, пустоту заполняем тишиной
                        for (int i = samples * bytesPerSample; i < ABuffer.Length; i++)
                            ABuffer[i] = 0;

                        LastSamples = samples;
                    }

                    if (samples > 0)
                        reader.Clip.ReadAudio(h.AddrOfPinnedObject(), CurrentSample, samples);

                    if (CheckBufferIsLost(first) || IsError || IsAborted)
                        return;

                    if (first || GetCurrentPosition() >= ABuffer.Length)
                        AudioBuffer.Write(ABuffer, 0, LockFlags.None);
                    else
                        AudioBuffer.Write(ABuffer, ABuffer.Length, LockFlags.None);

                    CurrentSample += samples;
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private void SetError(Exception ex)
        {
            if (IsError || IsAborted)
                return;

            IsError = true;

            if (HasAudio && AudioBuffer != null)
            {
                try { AudioBuffer.Stop(); }
                catch (Exception) { }
            }

            if (PlayerError != null)
            {
                OwnerDispatcher.Invoke(Priority, (ThreadStart)delegate()
                {
                    PlayerError(this, ex);
                });
            }
        }

        public void Close()
        {
            lock (locker)
            {
                try
                {
                    //Video
                    if (thread_v != null)
                    {
                        IsAborted = true;
                        playing_v.Set();      //Снимаем с паузы PlayingLoop, чтоб там сработала проверка на IsAborted
                        processing.WaitOne(); //Ждем, пока обработается текущий кадр, если его считывание еще не закончилось
                        thread_v.Join();      //Дожидаемся окончания работы PlayingLoop
                        thread_v = null;
                    }
                    if (BitmapSource != null)
                    {
                        BitmapSource = null;
                    }
                    if (VBuffer != IntPtr.Zero)
                    {
                        if (IsInterop) UnmapViewOfFile(VBuffer);
                        else Marshal.FreeHGlobal(VBuffer);
                        VBuffer = IntPtr.Zero;
                    }
                    if (MemSection != IntPtr.Zero)
                    {
                        CloseHandle(MemSection);
                        MemSection = IntPtr.Zero;
                    }

                    //Audio
                    if (thread_a != null)
                    {
                        IsAborted = true;
                        playing_a.Set();
                        thread_a.Join();
                        thread_a = null;
                    }
                    if (AudioBuffer != null)
                    {
                        AudioBuffer.Dispose();
                        AudioBuffer = null;
                    }
                    if (AudioDevice != null)
                    {
                        AudioDevice.Dispose();
                        AudioDevice = null;
                    }
                }
                catch (Exception)
                {
                    IsError = true;
                    throw;
                }
                finally
                {
                    if (h.IsAllocated)
                        h.Free();

                    AdjustMediaTimer(0);

                    if (reader != null)
                    {
                        try { reader.Close(); }
                        //catch (Exception) { } //Всё-равно вылезет в ~AviSynthClip()
                        finally { reader = null; }
                    }
                }
            }
        }

        public void Play()
        {
            lock (locker)
            {
                if (!IsError && !IsAborted && HasVideo)
                {
                    if (PlayerState != PlayState.Running)
                    {
                        AdjustMediaTimer(period);

                        PlayerState = PlayState.Running;
                        Interlocked.Exchange(ref _CurrentFrame, CurrentFrame);
                        ResetTimer = true;

                        if (HasAudio)
                        {
                            SetAudioSync();
                            playing_v.Set();
                            PlayAudio();
                        }
                        else
                        {
                            playing_v.Set();
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            lock (locker)
            {
                if (!IsError && !IsAborted && HasVideo)
                {
                    if (PlayerState == PlayState.Running || PlayerState == PlayState.Paused)
                    {
                        //Сброс на начало
                        playing_v.Reset();
                        processing.WaitOne();
                        Interlocked.Exchange(ref _CurrentFrame, 0);
                        if (Interlocked.Exchange(ref CurrentFrame, 0) != 0)
                            ShowFrame(0);

                        if (HasAudio)
                            StopAudio();

                        AdjustMediaTimer(0);
                    }

                    PlayerState = PlayState.Stopped;
                    playing_v.Reset();
                }
            }
        }

        public void Pause()
        {
            lock (locker)
            {
                if (!IsError && !IsAborted && HasVideo)
                {
                    PlayerState = PlayState.Paused;
                    ResetTimer = true;
                    playing_v.Reset();

                    if (HasAudio)
                        StopAudio();

                    AdjustMediaTimer(0);
                }
            }
        }

        public void Abort()
        {
            if (PlayerState != PlayState.Init)
            {
                IsAborted = true;
                playing_v.Set();
                playing_a.Set();
                processing.WaitOne();
            }
        }

        public void SetFrame(int frame)
        {
            lock (locker)
            {
                if (!IsError && !IsAborted && HasVideo)
                {
                    if (PlayerState == PlayState.Running)
                    {
                        //Pause->Set->Play
                        playing_v.Reset();
                        processing.WaitOne();
                        Interlocked.Exchange(ref _CurrentFrame, frame);
                        Interlocked.Exchange(ref CurrentFrame, frame);
                        ResetTimer = true;

                        if (HasAudio)
                        {
                            StopAudio();
                            SetAudioSync();
                            playing_v.Set();
                            PlayAudio();
                        }
                        else
                        {
                            playing_v.Set();
                        }
                    }
                    else
                    {
                        Interlocked.Exchange(ref _CurrentFrame, frame);
                        ShowFrame(frame);
                    }
                }
            }
        }

        private void StopAudio()
        {
            if (!IsError && !IsAborted)
            {
                try
                {
                    LastSamples = -1;
                    playing_a.Reset();

                    if ((AudioBuffer.Status & (int)BufferStatus.Playing) > 0)
                    {
                        CheckBufferIsLost(true);
                        AudioBuffer.Stop();
                        ResetAudio = true;
                    }
                }
                catch (Exception ex)
                {
                    SetError(ex);
                }
            }
        }

        private void PlayAudio()
        {
            if (!IsError && !IsAborted)
            {
                try
                {
                    //= - сэмпл был записан в буфер, но он еще не играл
                    if (CurrentSample <= TotalSamples)
                    {
                        CheckBufferIsLost(true);
                        AudioBuffer.Play(0, PlayFlags.Looping);
                        ResetAudio = true;
                        playing_a.Set();
                    }
                }
                catch (Exception ex)
                {
                    SetError(ex);
                }
            }
        }

        private void SetAudioSync()
        {
            if (!IsError && !IsAborted)
            {
                try
                {
                    CheckBufferIsLost(true);

                    if (GetCurrentPosition() != 0)
                        AudioBuffer.CurrentPosition = 0;

                    CurrentSample = (long)((CurrentFrame / Framerate) * Samplerate);
                    if (CurrentSample < TotalSamples)
                    {
                        LastSamples = -1;
                        FillAudioBuffer(true);
                    }
                }
                catch (Exception ex)
                {
                    SetError(ex);
                }
            }
        }

        private int GetCurrentPosition()
        {
            int read = 0, write = 0;
            AudioBuffer.GetCurrentPosition(out read, out write);
            return read;
        }

        private bool CheckBufferIsLost(bool _throw)
        {
            //true - потерян
            if ((AudioBuffer.Status & (int)BufferStatus.BufferLost) == 0)
                return false;

            //С первого раза может не получиться, но
            //и растягивать навечно смысла тоже нет..
            for (int i = 0; i <= 10; i++)
            {
                try { AudioBuffer.Restore(); }
                catch (Exception) { /*Буфер пока-что не может быть восстановлен*/ }

                if ((AudioBuffer.Status & (int)BufferStatus.BufferLost) == 0)
                    return false;

                if (IsError || IsAborted)
                    return true;

                if (i == 10)
                {
                    if (_throw)
                    {
                        throw new Exception(Languages.Translate("AudioBuffer is lost and can't be restored!") + "\r\n" +
                            Languages.Translate("Probably some other application uses it with higher privilege level."));
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    Thread.Sleep(100);
                    if (IsError || IsAborted)
                        return true;
                }
            }

            return true;
        }

        private void AdjustMediaTimer(uint ms)
        {
            #region
            //Это нужно только для того, чтоб Thread.Sleep(1) не длилось ~15ms (с дефолтными настройками).
            //Thread.Sleep(1) же нужны для того, чтоб во время "выжидания" временных интервалов не грузился CPU.
            //При DirectShow-превью интервал всегда задаётся в 1ms (при загрузке превью происходит установка, а
            //при выгрузке - сброс, тут же это будет только перед Play и при остановке). На Stopwatch() оно не влияет.
            //DSS2 и bassAudioSource тоже устанавливают интервал в 1ms.
            #endregion

            try
            {
                if (_period == 0 && ms > 0)
                {
                    TimeCaps tcaps = new TimeCaps();
                    if (timeGetDevCaps(ref tcaps, (uint)Marshal.SizeOf(typeof(TimeCaps))) == 0)
                    {
                        uint period = Math.Max(tcaps.wPeriodMin, ms);
                        if (timeBeginPeriod(period) == 0)
                            _period = period;
                    }
                }
                else if (_period > 0 && ms == 0)
                {
                    timeEndPeriod(_period);
                    _period = 0;
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }
    }
}

