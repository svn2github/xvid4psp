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
        public bool AllowDrop = false;
        public double DropThreshold = 0.5;

        private int Width = 0;
        private int Height = 0;
        public int TotalFrames = 0;
        public int CurrentFrame = 0;
        public double Framerate = 0;

        private int stride = 0;
        private IntPtr MemSection = IntPtr.Zero;
        private IntPtr MapView = IntPtr.Zero;
        public InteropBitmap InteropBitmapSource = null;
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

        public void Open(string script)
        {
            try
            {
                this.script = script;
                AllowDrop = Settings.PictureViewDropFrames;

                LoadAviSynth();
                if (HasVideo)
                {
                    CreateInteropBitmap();
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
                reader.ParseScript(script);

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
            if (InteropBitmapSource == null)
            {
                //Из-за бага в InteropBitmap вместо RGB24 придется использовать RGB32 (будет чуть медленнее).
                //Иначе картинка не будет обновляться при Invalidate - баг исправлен в каком-то новом FrameWork`е.
                PixelFormat format = PixelFormats.Bgr32;
                stride = Width * (format.BitsPerPixel / 8);
                uint bufSize = (uint)(stride * Height);

                if (MemSection == IntPtr.Zero)
                {
                    MemSection = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, bufSize, null);
                    if (MemSection == IntPtr.Zero) throw new Exception("CreateFileMapping: " + new Win32Exception().Message);
                }

                if (MapView == IntPtr.Zero)
                {
                    MapView = MapViewOfFile(MemSection, 0xF001F, 0, 0, bufSize);
                    if (MapView == IntPtr.Zero) throw new Exception("MapViewOfFile: " + new Win32Exception().Message);
                }

                InteropBitmapSource = Imaging.CreateBitmapSourceFromMemorySection(MemSection, Width, Height, format, stride, 0) as InteropBitmap;
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
                    //thread.Join();      //Дожидаемся окончания работы PlayingLoop (это блокирует основной поток, т.к. через Invoke в нем обновляется картинка!)
                    thread = null;
                }
                if (InteropBitmapSource != null)
                {
                    InteropBitmapSource = null;
                }
                if (MapView != IntPtr.Zero)
                {
                    UnmapViewOfFile(MapView);
                    MapView = IntPtr.Zero;
                }
                if (MemSection != IntPtr.Zero)
                {
                    CloseHandle(MemSection);
                    MemSection = IntPtr.Zero;
                }
                if (reader != null)
                {
                    reader.Close();
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
                while (!IsError && !IsAborted && CurrentFrame < reader.FrameCount) //Зацикливаем прогон кадров
                {
                    playing.WaitOne();

                    timer.Start();

                    if (IsAborted) break;
                    ReadFrame(CurrentFrame);

                    Interlocked.Increment(ref CurrentFrame);
                    ticksToWait = ticksPerFrame - Math.Min(ticksCorrection, ticksPerFrame);

                    //Будем пропускать кадры?
                    if (timer.ElapsedTicks > ticksPerFrame && AllowDrop)
                    {
                        //Без округления, просто прибавляем "порог" и отсекаем дробную часть
                        int drop = (int)((timer.ElapsedTicks / ticksPerFrame) + DropThreshold);
                        if (drop > 0)
                        {
                            Interlocked.Add(ref CurrentFrame, drop);
                            ticksToWait += drop * ticksPerFrame;
                        }
                    }

                    //Ждем оставшееся время перед показом следующего кадра
                    while (timer.ElapsedTicks < ticksToWait && !IsAborted)
                        Thread.Sleep(1);

                    //Нужна коррекция, т.к. пауза всегда длится дольше
                    ticksCorrection = timer.ElapsedTicks - ticksToWait;

                    timer.Reset();
                }

                ticksCorrection = 0;

                //Дошли до конца видео
                if (!IsError && !IsAborted)
                {
                    playing.Reset();
                    processing.WaitOne();

                    if (PlayerFinished != null)
                    {
                        App.Current.Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            PlayerFinished(this);
                        });
                    }
                }
            }
        }

        private void ReadFrame(int frame)
        {
            try
            {
                processing.Reset();
                reader.Clip.ReadFrame(MapView, stride, frame);
                processing.Set();

                //App.Current.Dispatcher.BeginInvoke(Priority, (ThreadStart)delegate()
                App.Current.Dispatcher.Invoke(Priority, (ThreadStart)delegate()
                {
                    try { InteropBitmapSource.Invalidate(); }
                    catch (Exception) { /*NullReference*/ };
                });
            }
            catch (Exception ex)
            {
                if (!IsAborted)
                    SetError(ex);
            }
            finally
            {
                processing.Set();
            }
        }

        private void SetError(Exception ex)
        {
            IsError = true;
            if (PlayerError != null)
            {
                App.Current.Dispatcher.Invoke((ThreadStart)delegate()
                {
                    PlayerError(this, ex);
                });
            }
        }

        public void Play()
        {
            if (!IsError && !IsAborted && HasVideo)
            {
                PlayerState = PlayState.Running;
                playing.Set();
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
                    if (Interlocked.Exchange(ref CurrentFrame, 0) != 0)
                        ReadFrame(0);
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

        public void SetFrame(int frame)
        {
            if (!IsError && !IsAborted && HasVideo)
            {
                if (PlayerState == PlayState.Running)
                {
                    //Pause->Set->Play
                    playing.Reset();
                    processing.WaitOne();
                    Interlocked.Exchange(ref CurrentFrame, frame);
                    playing.Set();
                }
                else
                {
                    Interlocked.Exchange(ref CurrentFrame, frame);
                    ReadFrame(frame);
                }
            }
        }
    }
}

