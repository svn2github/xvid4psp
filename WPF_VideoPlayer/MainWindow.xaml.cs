using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Windows.Threading;
using System.ComponentModel;
using DirectShowLib;
using DirectShowLib.DES;
using MediaBridge;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media.Imaging;

namespace WPF_VideoPlayer
{
    public partial class MainWindow
    {
        private IBasicAudio basicAudio;
        private IBasicVideo basicVideo;
        private PlayState currentState;
        private string filepath = string.Empty;
        private IVideoFrameStep frameStep;
        private IFilterGraph graph;
        private IGraphBuilder graphBuilder;
        private IntPtr hDrain = IntPtr.Zero;
        private bool isAudioOnly;
        private bool isFullScreen;
        private IMediaControl mediaControl;
        private IMediaEventEx mediaEventEx;
        private IMediaPosition mediaPosition;
        private IMediaSeeking mediaSeeking;
        private Thickness oldmargin;
        private HwndSource source;
        private System.Timers.Timer timer;
        private IVideoWindow videoWindow;
        private int VolumeSet;
        private const int WMGraphNotify = 0x40d;
        private BackgroundWorker worker;
        private System.Windows.WindowState oldstate;
        private bool IsRendererARFixed = false;
        private bool OldSeeking = false;
        private double dpi = 1.0;

        internal enum PlayState
        {
            Stopped,
            Paused,
            Running,
            Init
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll", EntryPoint = "GetDC")]
        public static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        public MainWindow()
        {
            this.InitializeComponent();

            try
            {
                //Установка параметров окна из сохраненных настроек
                string[] value = (Settings.WindowLocation).Split('/');
                if (value.Length == 4)
                {
                    this.Width = Convert.ToDouble(value[0]);
                    this.Height = Convert.ToDouble(value[1]);
                    this.Left = Convert.ToDouble(value[2]);
                    this.Top = Convert.ToDouble(value[3]);
                }

                this.button_open.Content = Languages.Translate("Open");
                this.button_open.ToolTip = Languages.Translate("Open media file");
                this.button_close.Content = Languages.Translate("Close");
                this.button_close.ToolTip = Languages.Translate("Close current file");
                this.button_exit.Content = Languages.Translate("Exit");
                this.button_exit.ToolTip = Languages.Translate("Exit from application");
                this.button_play.ToolTip = Languages.Translate("Play-Pause");
                this.button_frame_back.ToolTip = Languages.Translate("Frame back");
                this.button_frame_forward.ToolTip = Languages.Translate("Frame forward");
                this.button_stop.ToolTip = Languages.Translate("Stop");
                this.button_fullscreen.ToolTip = Languages.Translate("Fullscreen mode");
                this.slider_Volume.ToolTip = Languages.Translate("Volume");
                this.button_settings.ToolTip = Languages.Translate("Settings");
                this.menu_player_engine.Header = Languages.Translate("Player engine");
                this.check_old_seeking.ToolTip = Languages.Translate("If checked, Old method (continuous positioning while you move slider) will be used,") +
                    Environment.NewLine + Languages.Translate("otherwise New method is used (recommended) - position isn't set untill you release mouse button");

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) check_engine_directshow.IsChecked = true;
                else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge) check_engine_mediabridge.IsChecked = true;

                int vr = Settings.VideoRenderer;
                if (vr == 0) vr_default.IsChecked = true;
                else if (vr == 1) vr_overlay.IsChecked = true;
                else if (vr == 2) vr_vmr7.IsChecked = true;
                else if (vr == 3) vr_vmr9.IsChecked = true;

                check_old_seeking.IsChecked = OldSeeking = Settings.OldSeeking;

                //Установка значения громкости из реестра
                slider_Volume.Value = Settings.VolumeLevel;
                VolumeSet = -(int)(10000 - Math.Pow(slider_Volume.Value, 1.0 / 5) * 10000);
                if (slider_Volume.Value == 0) image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume2.png", UriKind.RelativeOrAbsolute));

                //Определяем коэффициент для масштабирования окна ДиректШоу-превью
                IntPtr ScreenDC = GetDC(IntPtr.Zero); //88-w, 90-h
                double _dpi = (double)GetDeviceCaps(ScreenDC, 88) / 96.0;
                if (_dpi != 0) dpi = _dpi;
                ReleaseDC(IntPtr.Zero, ScreenDC);
            }
            catch (Exception) { }

            base.Loaded += new RoutedEventHandler(this.MainWindow_Loaded);
            base.Closing += new CancelEventHandler(this.MainWindow_Closing);
            base.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.MainWindow_KeyDown);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.MainFormLoader();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1 && File.Exists(commandLineArgs[1]))
            {
                this.Title = Languages.Translate("loading") + "...";
            }
            else
            {
                this.Title = "WPF Video Player";
            }
            if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
            {
                base.LocationChanged += new EventHandler(this.MainWindow_LocationChanged);
                base.SizeChanged += new SizeChangedEventHandler(this.MainWindow_SizeChanged);
                this.source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                this.source.AddHook(new HwndSourceHook(this.WndProc));
            }
            else
            {
                this.VideoElement.LoadedBehavior = MediaState.Manual;
                this.VideoElement.ScrubbingEnabled = true;
                this.VideoElement.MediaOpened += new RoutedEventHandler(this.VideoElement_MediaOpened);
                this.VideoElement.MediaEnded += new RoutedEventHandler(this.VideoElement_MediaEnded);
            }

            this.timer = new System.Timers.Timer();
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
            this.timer.Interval = 50;

            this.worker = new BackgroundWorker();
            this.worker.DoWork += new DoWorkEventHandler(this.worker_DoWork);
            this.worker.RunWorkerAsync();
        }

        internal delegate void MainFormLoaderDelegate();
        private void MainFormLoader()
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MainFormLoaderDelegate(MainFormLoader));
            else
            {
                string[] commandLineArgs = Environment.GetCommandLineArgs();
                if (commandLineArgs.Length > 1 && File.Exists(commandLineArgs[1]))
                {
                    try
                    {
                        this.filepath = commandLineArgs[1];
                        PlayVideo(this.filepath);
                    }
                    catch (Exception exception)
                    {
                        this.CloseClip();
                        this.filepath = string.Empty;
                        System.Windows.MessageBox.Show(exception.Message);
                    }
                }
            }
        }

        private void BridgeCallback(MediaBridgeGraphInfo GraphInfo)
        {
            try
            {
                IBaseFilter filter;
                this.graph = (IFilterGraph) Marshal.GetObjectForIUnknown(GraphInfo.FilterGraph);
                this.graphBuilder = (IGraphBuilder) this.graph;
                DsError.ThrowExceptionForHR(this.graphBuilder.FindFilterByName("Enhanced Video Renderer", out filter));
                DsError.ThrowExceptionForHR(this.graphBuilder.RenderFile(this.filepath, null));

                ShowFilters();
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(exception.Message);
            }
        }

        private void button_close_Click(object sender, RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                this.CloseClip();
                this.filepath = string.Empty;
                SetPlayIcon();
            }
        }

        private void button_exit_Click(object sender, RoutedEventArgs e)
        {
            this.CloseClip();
            base.Close();
        }

        private void Shift_Frame(int n)
        {
            if (this.graphBuilder != null)
            {
                try
                {
                    if (this.currentState == PlayState.Running)
                        this.PauseClip();

                    TimeSpan span = Position + TimeSpan.FromSeconds(n);
                    if (span >= TimeSpan.Zero && span <= NaturalDuration) Position = span;
                }
                catch (Exception exception)
                {
                    System.Windows.MessageBox.Show(exception.Message);
                }
            }
        }

        private void button_frame_back_Click(object sender, RoutedEventArgs e)
        {
            Shift_Frame(-1);
        }

        private void button_frame_forward_Click(object sender, RoutedEventArgs e)
        {
            Shift_Frame(1);
        }

        private void button_fullscreen_Click(object sender, RoutedEventArgs e)
        {
            this.SwitchToFullScreen();
        }

        private void button_open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = Languages.Translate("files");
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.Filter = Languages.Translate("All media files") + "|*.avi;*.divx;*.wmv;*.mpg;*.mpe;*.mpeg;*.mod;*.asf;*.mkv;*.mov;*.qt;*.3gp;*.hdmov;*.mp4;*.ogm;*.avs;*.vob;*.dvr-ms;*.ts;*.m2p;*.m2t;*.m2v;*.d2v;*.m2ts;*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil;*.flv;*.pmp;*.mp3;*.wma;*.ogg;*.aac;*.m4a;*.dts;*.ac3;*.wav;*.vro|AVI " + str + " (AVI DIVX ASF)|*.avi;*.divx;*.asf|MPEG " + str + " (MPG MPE MPEG VOB MOD TS M2P M2T M2TS VRO)|*.mpg;*.mpe;*.mpeg;*.vob;*.ts;*.m2p;*.m2t;*.mod;*.m2ts;*.vro|DGIndex " + str + " (D2V)|*.d2v|QuickTime " + str + " (MOV QT 3GP HDMOV)|*.mov;*.qt;*.3gp;*.hdmov|RealVideo " + str + " (RM RAM RMVB RPX SMI SMIL)|*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil|Matroska " + str + " (MKV)|*.mkv|OGG Media (Vorbis) " + str + " (OGM)|*.ogm|Windows Media " + str + " (WMV)|*.wmv|Media Center " + str + " (DVR-MS)|*.dvr-ms|PMP " + str + " (PMP)|*.pmp|Flash Video " + str + " (FLV)|*.flv|MP3 " + str + " (MP3)|*.mp3|OGG Vorbis " + str + " (OGG)|*.ogg|Windows media audio " + str + " (WMA)|*.wma|DTS " + str + " (DTS)|*.dts|AC3 " + str + " (AC3)|*.ac3|AAC " + str + " (AAC)|*.aac;*.m4a|Windows PCM " + str + " (WAV)|*.wav|" + Languages.Translate("All files") + " (*.*)|*.*";
                dialog.Multiselect = false;
                dialog.Title = Languages.Translate("Select media file") + ":";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.Title = Languages.Translate("loading") + "...";
                    if (this.currentState != PlayState.Init)
                    {
                        this.CloseClip();
                    }
                    this.filepath = dialog.FileName;
                    PlayVideo(filepath);
                }
            }
            catch (Exception exception)
            {
                this.CloseClip();
                this.filepath = string.Empty;
                System.Windows.MessageBox.Show(exception.Message);
            }
        }

        private void button_play_Click(object sender, RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
                this.PauseClip();
        }

        private void button_stop_Click(object sender, RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                this.StopClip();
            }
        }

        private void CheckVisibility()
        {
            int hr = 0;
            if ((this.videoWindow == null) || (this.basicVideo == null))
            {
                this.isAudioOnly = true;
            }
            else
            {
                OABool @bool;
                this.isAudioOnly = false;
                hr = this.videoWindow.get_Visible(out @bool);
                if (hr < 0)
                {
                    if (hr == unchecked((int)0x80004002))      //E_NOINTERFACE
                    {
                        this.isAudioOnly = true;
                    }
                    else if (hr == unchecked((int)0x80040209)) //VFW_E_NOT_CONNECTED 
                    {
                        this.isAudioOnly = true;
                    }
                    else
                    {
                        this.isAudioOnly = true;               //Всё-равно видео окна скорее всего не будет
                        DsError.ThrowExceptionForHR(hr);
                    }
                }
            }
        }

        private void CloseClip()
        {
            //Останавливаем таймер обновления позиции
            if (timer != null) timer.Stop();

            if ((this.graphBuilder != null) && (this.VideoElement.Source == null))
            {
                if (this.mediaControl != null)
                    this.mediaControl.Stop();

                this.currentState = PlayState.Stopped;
                this.CloseInterfaces();
                this.isAudioOnly = true;
            }
            if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                VideoElement.Close();
                VideoElement.Source = null;
                VideoElement.Visibility = Visibility.Collapsed;
                if (this.graphBuilder != null)
                {
                    while (Marshal.ReleaseComObject(this.graphBuilder) > 0) ;
                    this.graphBuilder = null;

                    if (this.graph != null)
                    {
                        while (Marshal.ReleaseComObject(this.graph) > 0) ;
                        this.graph = null;
                    }
                    GC.Collect();
                }
                string mediaUrl = "MediaBridge://MyDataString";
                MediaBridgeManager.UnregisterCallback(mediaUrl);
            }

            SetPlayIcon();
            this.Title = "WPF Video Player";
            this.currentState = PlayState.Init;
            this.textbox_time.Text = textbox_duration.Text = "00:00:00";
            this.slider_pos.Value = 0.0;

            menu_filters.Items.Clear();
            menu_filters.IsEnabled = false;
        }

        private void CloseInterfaces()
        {
            int hr = 0;
            try
            {
                lock (this)
                {
                    // Relinquish ownership (IMPORTANT!) after hiding video window
                    if (!this.isAudioOnly && this.videoWindow != null)
                    {
                        hr = this.videoWindow.put_Visible(OABool.False);
                        DsError.ThrowExceptionForHR(hr);
                        hr = this.videoWindow.put_Owner(IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                        hr = this.videoWindow.put_MessageDrain(IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    if (this.mediaEventEx != null)
                    {
                        hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    if (this.mediaEventEx != null)
                        this.mediaEventEx = null;
                    if (this.mediaSeeking != null)
                        this.mediaSeeking = null;
                    if (this.mediaPosition != null)
                        this.mediaPosition = null;
                    if (this.mediaControl != null)
                        this.mediaControl = null;
                    if (this.basicAudio != null)
                        this.basicAudio = null;
                    if (this.basicVideo != null)
                        this.basicVideo = null;
                    if (this.videoWindow != null)
                        this.videoWindow = null;
                    if (this.frameStep != null)
                        this.frameStep = null;
                    if (this.graphBuilder != null)
                    {
                        while (Marshal.ReleaseComObject(this.graphBuilder) > 0) ;
                        this.graphBuilder = null;
                    }
                    GC.Collect();
                }
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(exception.Message);
            }
        }

        private bool GetFrameStepInterface()
        {
            IVideoFrameStep graphBuilder = null;
            graphBuilder = (IVideoFrameStep) this.graphBuilder;
            if (graphBuilder.CanStep(0, null) == 0)
            {
                this.frameStep = graphBuilder;
                return true;
            }
            this.frameStep = null;
            return false;
        }

        private void HandleGraphEvent()
        {
            if (this.mediaEventEx != null)
            {
                EventCode code;
                IntPtr ptr;
                IntPtr ptr2;
                while (this.mediaEventEx.GetEvent(out code, out ptr, out ptr2, 0) == 0)
                {
                    this.mediaEventEx.FreeEventParams(code, ptr, ptr2);
                    if (code == EventCode.Complete)
                    {
                        this.StopClip();
                    }
                    else if (code == EventCode.ClockChanged)
                    {
                        this.slider_pos.Maximum = NaturalDuration.TotalSeconds;

                        TimeSpan tCode = TimeSpan.Parse(TimeSpan.FromSeconds(NaturalDuration.TotalSeconds).ToString().Split('.')[0]);
                        textbox_duration.Text = tCode.ToString();
                    }
                }
            }
        }

        private void LayoutRoot_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Move | System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Scroll;
            }
        }

        private void LayoutRoot_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] data = (string[]) e.Data.GetData(System.Windows.DataFormats.FileDrop);
            int index = 0;
            while (index < data.Length)
            {
                string str = data[index];
                this.filepath = str;
                break;
            }
            if (this.filepath != null)
            {
                try
                {
                    this.Title = Languages.Translate("loading") + "...";
                    if (this.currentState != PlayState.Init)
                    {
                        this.CloseClip();
                    }

                    PlayVideo(filepath);
                }
                catch (Exception exception)
                {
                    this.CloseClip();
                    this.filepath = string.Empty;
                    System.Windows.MessageBox.Show(exception.Message);
                }
            }
        }
        
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.CloseClip();

            //Определяем и сохраняем размер и положение окна при выходе
            if (this.WindowState != System.Windows.WindowState.Maximized && this.WindowState != System.Windows.WindowState.Minimized)
            {
                Settings.WindowLocation = (int)this.Window.ActualWidth + "/" + (int)this.Window.ActualHeight + "/" +
                    (int)this.Window.Left + "/" + (int)this.Window.Top;
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.SystemKey == Key.Return) { e.Handled = true; SwitchToFullScreen(); }
            else if (e.Key == Key.Escape && isFullScreen) { e.Handled = true; SwitchToFullScreen(); }
            else if (e.Key == Key.Space) { e.Handled = true; PauseClip(); }
            else if (e.Key == Key.Left) { e.Handled = true; Shift_Frame(-1); }
            else if (e.Key == Key.Right) { e.Handled = true; Shift_Frame(1); }
            else if (e.Key == Key.Up) { e.Handled = true; VolumePlus(); }
            else if (e.Key == Key.Down) { e.Handled = true; VolumeMinus(); }
            else e.Handled = false;
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!this.isAudioOnly)
            {
                this.MoveVideoWindow();
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!this.isAudioOnly)
            {
                this.MoveVideoWindow();
            }
        }

        private void MoveVideoWindow()
        {
            if (this.videoWindow != null)
            {
                double left = 0, top = 0;
                int hr, actualWidth, actualHeight;
                DsError.ThrowExceptionForHR(this.basicVideo.get_SourceWidth(out actualWidth));
                DsError.ThrowExceptionForHR(this.basicVideo.get_VideoHeight(out actualHeight)); //get_SourceHeight ?
                double asp = ((double)actualWidth) / ((double)actualHeight);

                if (IsRendererARFixed) //Просто задаем рендереру место под окно, он сам исправит аспект (добавит бордюры)
                {
                    left = 0;
                    top = toolbar_top.ActualHeight;
                    actualWidth = (int)LayoutRoot.ActualWidth;
                    actualHeight = (int)(LayoutRoot.ActualHeight - toolbar_top.ActualHeight - toolbar_bottom.ActualHeight - grid_slider.ActualHeight);
                }
                else
                {
                    top = toolbar_top.ActualHeight;
                    actualHeight = (int)(LayoutRoot.ActualHeight - toolbar_top.ActualHeight - toolbar_bottom.ActualHeight - slider_pos.ActualHeight);
                    actualWidth = (int)(asp * actualHeight);
                    left = (LayoutRoot.ActualWidth - actualWidth) / 2.0;
                    if (actualWidth > (int)LayoutRoot.ActualWidth)
                    {
                        actualWidth = (int)LayoutRoot.ActualWidth;
                        actualHeight = (int)(((double)actualWidth) / asp);
                        left = 0;
                        top = (LayoutRoot.ActualHeight - actualHeight - grid_slider.ActualHeight) / 2.0;
                    }
                }

                //Масштабируем и вводим
                hr = this.videoWindow.SetWindowPosition((int)(left * dpi), (int)(top * dpi), (int)(actualWidth * dpi), (int)(actualHeight * dpi));
                DsError.ThrowExceptionForHR(hr);
                hr = this.videoWindow.put_BorderColor(1);
                DsError.ThrowExceptionForHR(hr);
            }
        }

        private void PauseClip()
        {
            if (this.mediaControl != null)
            {
                if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Stopped))
                {
                    if (this.mediaControl.Run() >= 0)
                        this.currentState = PlayState.Running;
                    this.SetPauseIcon();
                }
                else
                {
                    if (this.mediaControl.Pause() >= 0)
                        this.currentState = PlayState.Paused;
                    this.SetPlayIcon();
                }
            }
            if (this.VideoElement.Source != null)
            {
                if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Stopped))
                {
                    this.VideoElement.Play();
                    this.currentState = PlayState.Running;
                    this.SetPauseIcon();
                }
                else
                {
                    this.VideoElement.Pause();
                    this.currentState = PlayState.Paused;
                    this.SetPlayIcon();
                }
            }
        }

        void PlayVideo(string file)
        {
            this.Title = System.IO.Path.GetFileName(file) + " - WPF Video Player";
            this.currentState = PlayState.Stopped;

            if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
                this.PlayMovieInWindow(file);
            else
                this.PlayWithMediaBridge(file);

            this.slider_pos.Focus();

            //Запускаем таймер обновления позиции
            if (timer != null) timer.Start();
        }

        private void PlayMovieInWindow(string filename)
        {
            if (filename != string.Empty)
            {
                int hr = 0;
                this.graphBuilder = (IGraphBuilder) new FilterGraph();

                //Добавляем в граф нужный рендерер (0 - graphBuilder сам выберет рендерер)
                int renderer = Settings.VideoRenderer;
                if (renderer == 1)
                {
                    IBaseFilter add_vr = (IBaseFilter)new VideoRenderer();
                    hr = graphBuilder.AddFilter(add_vr, "Video Renderer");
                }
                else if (renderer == 2)
                {
                    IBaseFilter add_vmr = (IBaseFilter)new VideoMixingRenderer();
                    hr = graphBuilder.AddFilter(add_vmr, "Video Renderer");
                }
                else if (renderer == 3)
                {
                    IBaseFilter add_vmr9 = (IBaseFilter)new VideoMixingRenderer9();
                    hr = graphBuilder.AddFilter(add_vmr9, "Video Mixing Renderer 9");
                }
                DsError.ThrowExceptionForHR(hr);

                // Have the graph builder construct its the appropriate graph automatically
                hr = this.graphBuilder.RenderFile(filename, null);
                DsError.ThrowExceptionForHR(hr);

                //Ищем рендерер и ВКЛЮЧАЕМ соблюдение аспекта (рендерер сам подгонит картинку под размер окна, с учетом аспекта)
                IsRendererARFixed = false;
                IBaseFilter filter = null;
                graphBuilder.FindFilterByName("Video Renderer", out filter);
                if (filter != null)
                {
                    IVMRAspectRatioControl vmr = filter as IVMRAspectRatioControl;
                    if (vmr != null)
                    {
                        DsError.ThrowExceptionForHR(vmr.SetAspectRatioMode(VMRAspectRatioMode.LetterBox));
                        IsRendererARFixed = true;
                    }
                }
                else
                {
                    graphBuilder.FindFilterByName("Video Mixing Renderer 9", out filter);
                    if (filter != null)
                    {
                        IVMRAspectRatioControl9 vmr9 = filter as IVMRAspectRatioControl9;
                        if (vmr9 != null)
                        {
                            DsError.ThrowExceptionForHR(vmr9.SetAspectRatioMode(VMRAspectRatioMode.LetterBox));
                            IsRendererARFixed = true;
                        }
                    }
                }

                this.mediaControl = (IMediaControl) this.graphBuilder;
                this.mediaEventEx = (IMediaEventEx) this.graphBuilder;
                this.mediaSeeking = (IMediaSeeking) this.graphBuilder;
                this.mediaPosition = (IMediaPosition) this.graphBuilder;
                this.videoWindow = this.graphBuilder as IVideoWindow;
                this.basicVideo = this.graphBuilder as IBasicVideo;
                this.basicAudio = this.graphBuilder as IBasicAudio;
                this.basicAudio.put_Volume(VolumeSet);
                this.CheckVisibility();
                DsError.ThrowExceptionForHR(this.mediaEventEx.SetNotifyWindow(this.source.Handle, 0x40d, IntPtr.Zero));
                if (!this.isAudioOnly)
                {
                    DsError.ThrowExceptionForHR(this.videoWindow.put_Owner(this.source.Handle));
                    DsError.ThrowExceptionForHR(this.videoWindow.put_MessageDrain(this.source.Handle));
                    DsError.ThrowExceptionForHR(this.videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings | DirectShowLib.WindowStyle.ClipChildren));
                    this.MoveVideoWindow();
                    this.GetFrameStepInterface();
                }
                DsError.ThrowExceptionForHR(this.mediaControl.Run());
                this.currentState = PlayState.Running;
                this.SetPauseIcon();

                ShowFilters();
            }
        }

        private void PlayWithMediaBridge(string filepath)
        {
            this.filepath = filepath;
            string mediaUrl = "MediaBridge://MyDataString";
            MediaBridgeManager.RegisterCallback(mediaUrl, new MediaBridgeManager.NewMediaGraphInfo(this.BridgeCallback));
            this.VideoElement.Source = new Uri(mediaUrl);
            this.VideoElement.Visibility = Visibility.Visible;
            this.VideoElement.Play();
            this.currentState = PlayState.Running;
            this.SetPauseIcon();
        }

        internal delegate void ClearFiltersDelegate();
        private void ClearFiltersMenu()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ClearFiltersDelegate(ClearFiltersMenu));
            else
            {
                menu_filters.Items.Clear();
                menu_filters.IsEnabled = false;
            }
        }

        internal delegate void AddFilterDelegate(string filter);
        private void AddFilterToMenu(string filter)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new AddFilterDelegate(AddFilterToMenu), filter);
            else
            {
                MenuItem item = new MenuItem();
                item.Header = filter;
                item.StaysOpenOnClick = true;
                menu_filters.Items.Add(item);
                menu_filters.IsEnabled = true;
            }
        }

        //Список задействованных в графе фильтров
        private void ShowFilters()
        {
            ClearFiltersMenu();
            if (this.graphBuilder == null) return;
            
            IEnumFilters pEnum = null;
            IBaseFilter[] pFilter = null;
            try
            {
                this.graphBuilder.EnumFilters(out pEnum);
                if (pEnum != null)
                {
                    pFilter = new IBaseFilter[1];
                    pFilter[0] = null;
                    while (pEnum.Next(1, pFilter, IntPtr.Zero) == 0)
                    {
                        FilterInfo filterInfo;
                        if (pFilter[0].QueryFilterInfo(out filterInfo) == 0)
                        {
                            if (filterInfo.pGraph != null)
                                Marshal.ReleaseComObject(filterInfo.pGraph);

                            AddFilterToMenu(filterInfo.achName);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (pFilter[0] != null)
                {
                    Marshal.ReleaseComObject(pFilter[0]);
                    pFilter[0] = null;
                }
                if (pEnum != null)
                {
                    Marshal.ReleaseComObject(pEnum);
                    pEnum = null;
                }
            }
        }

        private void SetPauseIcon()
        {
            image_play.Source = new BitmapImage(new Uri(@"../pictures/pause_new.png", UriKind.RelativeOrAbsolute));
        }

        private void SetPlayIcon()
        {
            image_play.Source = new BitmapImage(new Uri(@"../pictures/play_new.png", UriKind.RelativeOrAbsolute));
        }

        //Позиционирование при перемещении ползунка (старый способ)
        private void slider_pos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OldSeeking && this.slider_pos.IsMouseOver && this.graphBuilder != null)
            {
                Visual visual = Mouse.Captured as Visual;
                if (visual != null && visual.IsDescendantOf(this.slider_pos))
                {
                    this.Position = TimeSpan.FromSeconds(this.slider_pos.Value);
                }
            }
        }

        //Позиционирование при отпускании кнопки мыши (новый способ)
        private void slider_pos_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!OldSeeking)
                Position = TimeSpan.FromSeconds(this.slider_pos.Value);
        }

        private void StopClip()
        {
            DsLong pCurrent = new DsLong(0L);
            if (((this.mediaControl != null) && (this.mediaSeeking != null)) && ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Running)))
            {
                this.mediaControl.Stop();
                this.currentState = PlayState.Stopped;
                this.mediaSeeking.SetPositions(pCurrent, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                Thread.Sleep(100); //Иначе в некоторых случаях будет зависание или вылет после сикинга
                this.mediaControl.Pause();
            }
            if (this.VideoElement.Source != null)
            {
                this.VideoElement.Stop();
                this.currentState = PlayState.Stopped;
            }
            this.SetPlayIcon();
        }

        private void SwitchToFullScreen()
        {
            if ((this.isAudioOnly || this.graphBuilder == null) && !isFullScreen) return;

            if (!isFullScreen)
            {
                this.isFullScreen = true;
                oldstate = this.WindowState;
                base.WindowStyle = System.Windows.WindowStyle.None;
                base.WindowState = System.Windows.WindowState.Maximized;
                this.oldmargin = this.VideoElement.Margin;

                if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
                    this.MoveVideoWindow();
                else
                    this.VideoElement.Margin = new Thickness(0, toolbar_top.ActualHeight, 0, toolbar_bottom.ActualHeight + grid_slider.ActualHeight);
            }
            else
            {
                base.WindowState = oldstate;
                base.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                this.isFullScreen = false;

                if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
                    this.MoveVideoWindow();
                else
                    this.VideoElement.Margin = this.oldmargin;
            }

            this.slider_pos.Focus();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.graphBuilder != null)
                this.UpdateClock();
        }

        internal delegate void UpdateClockDelegate();
        private void UpdateClock()
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateClockDelegate(UpdateClock));
            else if (this.graphBuilder != null && NaturalDuration != TimeSpan.Zero)
            {
                textbox_time.Text = TimeSpan.Parse(TimeSpan.FromSeconds(slider_pos.Value).ToString().Split('.')[0]).ToString();

                Visual visual = Mouse.Captured as Visual;
                if (visual == null || !visual.IsDescendantOf(slider_pos))
                {
                    this.slider_pos.Value = this.Position.TotalSeconds;
                }
            }
        }

        private void VideoElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.StopClip();
        }

        private void VideoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (this.VideoElement.HasVideo || this.VideoElement.HasAudio)
            {
                this.slider_pos.Maximum = this.VideoElement.NaturalDuration.TimeSpan.TotalSeconds;
                TimeSpan tCode = TimeSpan.Parse(TimeSpan.FromSeconds(NaturalDuration.TotalSeconds).ToString().Split('.')[0]);
                textbox_duration.Text = tCode.ToString();
            }
        }

        //Обработка двойного щелчка мыши для плейера
        private void Player_Mouse_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
                SwitchToFullScreen();
            else if (e.ClickCount == 1 && e.ChangedButton == MouseButton.Right)
            {
                button_settings.ContextMenu.IsOpen = true;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WMGraphNotify:
                    {
                        HandleGraphEvent(); break;
                    }
                case 0x0203: //0x0201 WM_LBUTTONDOWN, 0x0202 WM_LBUTTONUP, 0x0203 WM_LBUTTONDBLCLK
                    {
                        SwitchToFullScreen(); break;
                    }
                case 0x0205: //0x0204 WM_RBUTTONDOWN, 0x0205 WM_RBUTTONUP, 0x0206 WM_RBUTTONDBLCLK
                    {
                        //Мышь должна быть над окном рендерера, иначе правый клик будет срабатывать повсюду!
                        //Для 0x0203 и 0x0206 это условие каким-то образом выполняется само по себе :)
                        if (Mouse.DirectlyOver == null) button_settings.ContextMenu.IsOpen = true;
                        break;
                    }
                case 0x020A: //0x020A WM_MOUSEWHEEL
                    {
                        if (wParam.ToInt32() > 0) VolumePlus(); else VolumeMinus();
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public TimeSpan NaturalDuration
        {
            get
            {
                try
                {
                    if (this.graphBuilder != null && this.VideoElement.Source == null)
                    {
                        double duration;
                        DsError.ThrowExceptionForHR(this.mediaPosition.get_Duration(out duration));
                        return TimeSpan.FromSeconds(duration);
                    }
                    else if (this.graphBuilder != null && this.VideoElement.Source != null && VideoElement.NaturalDuration.HasTimeSpan)
                    {
                        return this.VideoElement.NaturalDuration.TimeSpan;
                    }
                    else
                        return TimeSpan.Zero;
                }
                catch //(Exception ex)
                {
                    //this.CloseClip();
                    //this.filepath = string.Empty;
                    //System.Windows.MessageBox.Show(ex.Message);
                    return TimeSpan.Zero;
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                try
                {
                    if (this.graphBuilder != null && this.VideoElement.Source == null)
                    {
                        double position;
                        DsError.ThrowExceptionForHR(this.mediaPosition.get_CurrentPosition(out position));
                        return TimeSpan.FromSeconds(position);
                    }
                    else if (this.graphBuilder != null && this.VideoElement.Source != null)
                    {
                        return this.VideoElement.Position;
                    }
                    else
                        return TimeSpan.Zero;
                }
                catch //(Exception ex)
                {
                    //this.CloseClip();
                    //this.filepath = string.Empty;
                    //System.Windows.MessageBox.Show(ex.Message);
                    return TimeSpan.Zero;
                }
            }
            set
            {
                try
                {
                    if (this.graphBuilder != null && this.VideoElement.Source == null)
                    {
                        this.mediaPosition.put_CurrentPosition(value.TotalSeconds);
                    }
                    else if (this.graphBuilder != null && this.VideoElement.Source != null)
                    {
                        this.VideoElement.Position = value;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is AccessViolationException) throw;
                }
            }
        }

        //Громкость+
        public void VolumePlus()
        {
            if (slider_Volume.Value <= 0.95)
                slider_Volume.Value = slider_Volume.Value + 0.05; //Увеличиваем громкость для МедиаБридж на 0,05
        }

        //Громкость-
        public void VolumeMinus()
        {
            if (slider_Volume.Value >= 0.05)
            {
                slider_Volume.Value = (slider_Volume.Value - 0.05); //Уменьшаем громкость для МедиаБридж на 0,05
                if (slider_Volume.Value < 0.05) //чтоб гарантировано достигнуть нуля
                    slider_Volume.Value = 0;
            }
        }

        //Обработка изменения положения регулятора громкости, изменение иконки рядом с ним, пересчет значений для ДиректШоу
        private void Volume_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeSet = -(int)(10000 - Math.Pow(slider_Volume.Value, 1.0 / 5) * 10000); //Пересчитывание громкости для ДиректШоу
            if (this.graphBuilder != null && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                basicAudio.put_Volume(VolumeSet);

            //Иконка регулятора громкости
            if (slider_Volume.Value <= 0.0)
                image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume2.png", UriKind.RelativeOrAbsolute));
            else
                image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume.png", UriKind.RelativeOrAbsolute));

            //Запись значения громкости в реестр
            Settings.VolumeLevel = slider_Volume.Value;
        }

        //Меняем громкость колесиком мышки
        private void Mouse_Wheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                VolumePlus();
            else
                VolumeMinus();
        }

        private void Mouse_In(object sender, MouseEventArgs e)
        {
            this.textbox_time.Visibility = Visibility.Collapsed;
            this.textbox_duration.Visibility = Visibility.Visible;
        }

        private void Mouse_Out(object sender, MouseEventArgs e)
        {
            this.textbox_time.Visibility = Visibility.Visible;
            this.textbox_duration.Visibility = Visibility.Collapsed;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void button_settings_Click(object sender, RoutedEventArgs e)
        {
            button_settings.ContextMenu.IsOpen = true;
        }

        private void player_engine_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.PlayerEngine.ToString() == ((MenuItem)sender).Header.ToString()) return;

            if (!string.IsNullOrEmpty(filepath))
                CloseClip();

            //Удаляем старое
            if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
            {
                this.LocationChanged -= new EventHandler(MainWindow_LocationChanged);
                this.SizeChanged -= new SizeChangedEventHandler(MainWindow_SizeChanged);

                source.RemoveHook(new HwndSourceHook(WndProc));
            }
            else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
            {
                VideoElement.Visibility = Visibility.Collapsed;
                VideoElement.MediaOpened -= new RoutedEventHandler(VideoElement_MediaOpened);
                VideoElement.MediaEnded -= new RoutedEventHandler(VideoElement_MediaEnded);
            }

            //Добавляем новое
            if (((MenuItem)sender).Header.ToString() == "DirectShow")
            {
                this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);

                source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                source.AddHook(new HwndSourceHook(WndProc));

                check_engine_directshow.IsChecked = true;
                Settings.PlayerEngine = Settings.PlayerEngines.DirectShow;
            }
            else if (((MenuItem)sender).Header.ToString() == "MediaBridge")
            {
                //set media element state for video loading
                VideoElement.LoadedBehavior = MediaState.Manual;
                //VideoElement.UnloadedBehavior = MediaState.Stop;
                VideoElement.ScrubbingEnabled = true;

                //add new events
                VideoElement.MediaOpened += new RoutedEventHandler(VideoElement_MediaOpened);
                VideoElement.MediaEnded += new RoutedEventHandler(VideoElement_MediaEnded);

                isAudioOnly = false;
                check_engine_mediabridge.IsChecked = true;
                Settings.PlayerEngine = Settings.PlayerEngines.MediaBridge;
            }

            //Обновляем превью
            if (!string.IsNullOrEmpty(filepath))
                PlayVideo(filepath);
        }

        private void Change_renderer_Click(object sender, RoutedEventArgs e)
        {
            int new_value = 0;
            int old_value = Settings.VideoRenderer;

            if (vr_Default.IsFocused)
            {
                vr_default.IsChecked = true;
                Settings.VideoRenderer = new_value = 0;
            }
            else if (vr_Overlay.IsFocused)
            {
                vr_overlay.IsChecked = true;
                Settings.VideoRenderer = new_value = 1;
            }
            else if (vr_VMR7.IsFocused)
            {
                vr_vmr7.IsChecked = true;
                Settings.VideoRenderer = new_value = 2;
            }
            else if (vr_VMR9.IsFocused)
            {
                vr_vmr9.IsChecked = true;
                Settings.VideoRenderer = new_value = 3;
            }

            if (old_value != new_value && !string.IsNullOrEmpty(filepath) && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
            {
                CloseClip();
                PlayVideo(filepath);
            }
        }

        private void check_Old_Seeking_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.OldSeeking = OldSeeking = check_old_seeking.IsChecked; 
        }
    }
}

