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

namespace XviD4PSP
{
    public delegate void AvsPlayerFinished(object sender);
    public delegate void AvsPlayerError(object sender, Exception exception);

    public class AviSynthPlayer
    {
        public enum PlayState { Stopped, Paused, Running, Init }
        public PlayState PlayerState = PlayState.Init;
        public event AvsPlayerFinished PlayerFinished;
        public event AvsPlayerError PlayerError;

        private static object locker = new object();
        private ManualResetEvent playing = new ManualResetEvent(false);
        private ManualResetEvent processing = new ManualResetEvent(true);
        private AviSynthReader reader = null;
        private Thread thread = null;
        private string script = null;

        public bool HasVideo = false;
        public bool IsError = false;
        private bool IsAborted = false;
        private bool IsInterop = false;
        public bool AllowDropFrames = false;
        public double DropThreshold = 0.5;  //<0 - меньше кадров будет пропущено, но скорость может поплыть; >0 - больше кадров будет пропущено
 
        private int Width = 0;
        private int Height = 0;
        public int TotalFrames = 0;
        public double Framerate = 0;
        public int CurrentFrame = 0;   //Номер кадра, который точно был показан
        private int _CurrentFrame = 0; //Номер кадра при расчетах в PlayingLoop

        private int stride = 0;
        private int bufSize = 0;
        private IntPtr MemSection = IntPtr.Zero;
        private IntPtr VBuffer = IntPtr.Zero;
        public ImageSource BitmapSource = null;
        private ThreadStart UpdateDelegate = null;
        public DispatcherPriority Priority = DispatcherPriority.Normal;

        [DllImport("kernel32.dll", EntryPoint = "CreateFileMapping", SetLastError = true)]
        private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", EntryPoint = "MapViewOfFile", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", EntryPoint = "UnmapViewOfFile", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        public AviSynthPlayer()
        {
            //
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

                    CreatePlayingThread();
                }
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

        public void Close()
        {
            lock (locker)
            {
                if (thread != null)
                {
                    IsAborted = true;
                    playing.Set();        //Снимаем с паузы PlayingLoop, чтоб там сработала проверка на IsAborted
                    processing.WaitOne(); //Ждем, пока обработается текущий кадр, если его считывание еще не закончилось
                    thread.Join();        //Дожидаемся окончания работы PlayingLoop
                    thread = null;
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
                if (reader != null)
                {
                    try { reader.Close(); }
                    catch (Exception) { }
                    reader = null;
                }
            }
        }

        private void CreatePlayingThread()
        {
            if (thread == null)
            {
                thread = new Thread(new ThreadStart(PlayingLoop));
                thread.Start();
            }
        }

        private void PlayingLoop()
        {
            Stopwatch timer = new Stopwatch();
            long ticksPerSecond = Stopwatch.Frequency;
            long ticksPerFrame = (long)(ticksPerSecond * (1.0 / Framerate));
            long ticksToWait = ticksPerFrame;
            long ticksCorrection = 0;

            while (!IsError && !IsAborted) //Зацикливаем воспроизведение (переход с конца на начало)
            {
                while (!IsError && !IsAborted && _CurrentFrame < TotalFrames) //Зацикливаем прогон кадров
                {
                    playing.WaitOne();
                    timer.Start();

                    if (IsAborted)
                        return;

                    processing.Reset();
                    ShowFrame(_CurrentFrame);

                    Interlocked.Increment(ref _CurrentFrame);
                    ticksToWait = ticksPerFrame - Math.Min(ticksCorrection, ticksPerFrame);

                    //Будем пропускать кадры?
                    if (timer.ElapsedTicks > ticksPerFrame && AllowDropFrames)
                    {
                        //Без округления, просто прибавляем "порог" и отсекаем дробную часть
                        int drop = (int)((timer.ElapsedTicks / ticksPerFrame) + DropThreshold);
                        if (drop > 0)
                        {
                            Interlocked.Add(ref _CurrentFrame, drop);
                            ticksToWait += drop * ticksPerFrame;
                        }
                    }

                    //Ждем оставшееся время перед показом следующего кадра
                    while (timer.ElapsedTicks < ticksToWait && !IsAborted)
                        Thread.Sleep(1);

                    //Нужна коррекция, т.к. пауза всегда длится дольше
                    ticksCorrection = timer.ElapsedTicks - ticksToWait;

                    processing.Set();
                    timer.Reset();

                    //Чем меньше операций останется между timer.Reset() и timer.Start(), тем точнее будет скорость.
                    //Т.к. пока таймер остановлен, затраченное время не считается и погрешность будет накапливаться.
                }

                playing.Reset();
                ticksCorrection = 0;

                //Дошли до конца видео
                if (!IsError && !IsAborted)
                {
                    if (PlayerFinished != null)
                    {
                        App.Current.Dispatcher.Invoke(Priority, (ThreadStart)delegate()
                        {
                            PlayerFinished(this);
                        });
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

                App.Current.Dispatcher.BeginInvoke(Priority, UpdateDelegate);
            }
            catch (Exception ex)
            {
                processing.Set();
                if (!IsAborted)
                    SetError(ex);
            }
        }

        private void SetError(Exception ex)
        {
            IsError = true;
            if (PlayerError != null)
            {
                App.Current.Dispatcher.Invoke(Priority, (ThreadStart)delegate()
                {
                    PlayerError(this, ex);
                });
            }
        }

        public void Play()
        {
            if (!IsError && !IsAborted && HasVideo)
            {
                if (PlayerState != PlayState.Running)
                {
                    PlayerState = PlayState.Running;
                    playing.Set();
                }
            }
        }

        public void Stop()
        {
            if (!IsError && !IsAborted && HasVideo)
            {
                if (PlayerState == PlayState.Running || PlayerState == PlayState.Paused)
                {
                    //Сброс на начало
                    playing.Reset();
                    processing.WaitOne();
                    Interlocked.Exchange(ref _CurrentFrame, 0);
                    if (Interlocked.Exchange(ref CurrentFrame, 0) != 0)
                        ShowFrame(0);
                }

                PlayerState = PlayState.Stopped;
                playing.Reset();
            }
        }

        public void Pause()
        {
            if (!IsError && !IsAborted && HasVideo)
            {
                PlayerState = PlayState.Paused;
                playing.Reset();
            }
        }

        public void Abort()
        {
            if (PlayerState != PlayState.Init)
            {
                IsAborted = true;
                playing.Set();
                processing.WaitOne();
            }
        }

        public void SetFrame(int frame)
        {
            if (!IsError && !IsAborted && HasVideo)
            {
                if (PlayerState == PlayState.Running)
                {
                    //Pause->Set->Play
                    playing.Reset();
                    processing.WaitOne();
                    Interlocked.Exchange(ref _CurrentFrame, frame);
                    playing.Set();
                }
                else
                {
                    Interlocked.Exchange(ref _CurrentFrame, frame);
                    ShowFrame(frame);
                }
            }
        }
    }
}

