﻿using System;
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
using System.Drawing;

namespace WPF_VideoPlayer
{
    public partial class MainWindow
    {
        private static object locker = new object();
        private IBasicAudio basicAudio;
        private IBasicVideo basicVideo;
        private PlayState currentState = PlayState.Init;
        private PlayState oldplaystate = PlayState.Init;
        private TimeSpan oldpos = TimeSpan.Zero;
        private string filepath = string.Empty;
        private IFilterGraph graph;
        private IGraphBuilder graphBuilder;
        private IntPtr Handle = IntPtr.Zero;
        private IntPtr VHandle = IntPtr.Zero;
        private bool isAudioOnly = false;
        private bool isFullScreen = false;
        private IMediaControl mediaControl;
        private IMediaEventEx mediaEventEx;
        private IMediaPosition mediaPosition;
        private IMediaSeeking mediaSeeking;
        private Thickness oldmargin;
        private HwndSource source;
        private System.Timers.Timer timer;
        private IVideoWindow videoWindow;
        private IMFVideoDisplayControl EVRControl;
        private VideoHwndHost VHost;
        private int VolumeSet;
        private const int WMGraphNotify = 0x40d;
        private BackgroundWorker worker;
        private System.Windows.WindowState oldstate;
        private bool IsRendererARFixed = false;
        private bool OldSeeking = false;
        private double dpi = 0.0;
        private MediaLoad mediaload;

        private enum PlayState { Stopped, Paused, Running, Init }
        private enum MediaLoad { Load, Update }

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
                if ((check_window_size.IsChecked = Settings.WindowResize))
                {
                    string[] value = (Settings.WindowLocation).Split('/');
                    if (value.Length == 4)
                    {
                        this.Width = Convert.ToDouble(value[0]);
                        this.Height = Convert.ToDouble(value[1]);
                        this.Left = Convert.ToDouble(value[2]);
                        this.Top = Convert.ToDouble(value[3]);
                    }
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

                this.menu_misc.Header = Languages.Translate("Misc");
                this.check_win7_taskbar.Header = Languages.Translate("Enable Windows 7 taskbar progress indication");
                this.check_window_size.Header = Languages.Translate("Restore the size and location of the main window");
                this.check_window_pos.Header = Languages.Translate("Fit windows to the working area bounds");

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) check_engine_directshow.IsChecked = true;
                else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge) check_engine_mediabridge.IsChecked = true;

                Settings.VRenderers vr = Settings.VideoRenderer;
                if (vr == Settings.VRenderers.Auto) vr_default.IsChecked = true;
                else if (vr == Settings.VRenderers.Overlay) vr_overlay.IsChecked = true;
                else if (vr == Settings.VRenderers.VMR7) vr_vmr7.IsChecked = true;
                else if (vr == Settings.VRenderers.VMR9) vr_vmr9.IsChecked = true;
                else if (vr == Settings.VRenderers.EVR) vr_evr.IsChecked = true;

                check_old_seeking.IsChecked = OldSeeking = Settings.OldSeeking;
                check_win7_taskbar.IsChecked = Settings.Win7TaskbarIsEnabled;

                //Установка значения громкости из реестра
                slider_Volume.Value = Settings.VolumeLevel;
                VolumeSet = -(int)(10000 - Math.Pow(slider_Volume.Value, 1.0 / 5) * 10000);
                if (slider_Volume.Value == 0) image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume2.png", UriKind.RelativeOrAbsolute));
                slider_Volume.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Volume_ValueChanged); //Не в xaml, чтоб не срабатывал до загрузки

                //Определяем коэффициент dpi
                RetrieveDPI();

                DDHelper ddh = new DDHelper(this);
                ddh.GotFiles += new DDEventHandler(DD_GotFiles);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("LoadSettings: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            this.Handle = new WindowInteropHelper(this).Handle;

            //Вторая попытка для dpi
            if (dpi == 0)
                RetrieveDPI();

            if ((check_window_pos.IsChecked = Settings.CheckWindowsPos))
            {
                CheckWindowPos(this, Handle, true);
                this.MaxWidth = double.PositiveInfinity;
                this.MaxHeight = double.PositiveInfinity;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Win7TaskbarIsEnabled && !Win7Taskbar.InitializeWin7Taskbar())
            {
                System.Windows.MessageBox.Show(Languages.Translate("Failed to initialize Windows 7 taskbar interface.") +
                   " " + Languages.Translate("This feature will be disabled!"), Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                check_win7_taskbar.IsChecked = Settings.Win7TaskbarIsEnabled = false;
            }

            if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
            {
                base.LocationChanged += new EventHandler(this.MainWindow_LocationChanged);
                base.SizeChanged += new SizeChangedEventHandler(this.MainWindow_SizeChanged);
                this.source = HwndSource.FromHwnd(this.Handle);
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

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.MainFormLoader();
        }

        internal delegate void MainFormLoaderDelegate();
        private void MainFormLoader()
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
                System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MainFormLoaderDelegate(MainFormLoader));
            else
            {
                try
                {
                    string[] commandLineArgs = Environment.GetCommandLineArgs();
                    if (commandLineArgs.Length > 1 && File.Exists(commandLineArgs[1]))
                    {
                        PlayVideo(commandLineArgs[1], MediaLoad.Load);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("MainFormLoader: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BridgeCallback(MediaBridgeGraphInfo GraphInfo)
        {
            try
            {
                this.graph = (IFilterGraph) Marshal.GetObjectForIUnknown(GraphInfo.FilterGraph);
                this.graphBuilder = (IGraphBuilder) this.graph;
                DsError.ThrowExceptionForHR(this.graphBuilder.RenderFile(this.filepath, null));

                AddFiltersToMenu();
            }
            catch (Exception ex)
            {
                this.CloseClip();
                this.filepath = string.Empty;
                Win7Taskbar.SetProgressTaskComplete(this.Handle, TBPF.ERROR);
                System.Windows.MessageBox.Show("BridgeCallback: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void button_close_Click(object sender, RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                this.CloseClip();
                this.filepath = string.Empty;
            }
        }

        private void button_exit_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentState != PlayState.Init)
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
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("ShiftFrame: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                dialog.Filter = Languages.Translate("All media files") + "|*.avi;*.divx;*.wmv;*.mpg;*.mpe;*.mpeg;*.mod;*.asf;*.mkv;*.mov;*.qt;*.3gp;*.hdmov;*.mp4;" +
                    "*.ogm;*.avs;*.vob;*.dvr-ms;*.ts;*.m2p;*.m2t;*.m2v;*.d2v;*.m2ts;*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil;*.flv;*.pmp;*.mp3;*.wma;*.ogg;*.aac;*.m4a;" +
                    "*.dts;*.ac3;*.wav;*.vro|AVI " + str + " (AVI DIVX ASF)|*.avi;*.divx;*.asf|MPEG " + str + " (MPG MPE MPEG VOB MOD TS M2P M2T M2TS VRO)|*.mpg;*.mpe;" +
                    "*.mpeg;*.vob;*.ts;*.m2p;*.m2t;*.mod;*.m2ts;*.vro|DGIndex " + str + " (D2V)|*.d2v|QuickTime " + str + " (MOV QT 3GP HDMOV)|*.mov;*.qt;*.3gp;" +
                    "*.hdmov|RealVideo " + str + " (RM RAM RMVB RPX SMI SMIL)|*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil|Matroska " + str + " (MKV)|*.mkv|OGG Media (Vorbis) " +
                    str + " (OGM)|*.ogm|Windows Media " + str + " (WMV)|*.wmv|Media Center " + str + " (DVR-MS)|*.dvr-ms|PMP " + str + " (PMP)|*.pmp|Flash Video " + str +
                    " (FLV)|*.flv|MP3 " + str + " (MP3)|*.mp3|OGG Vorbis " + str + " (OGG)|*.ogg|Windows media audio " + str + " (WMA)|*.wma|DTS " + str + " (DTS)|*.dts|AC3 " +
                    str + " (AC3)|*.ac3|AAC " + str + " (AAC)|*.aac;*.m4a|Windows PCM " + str + " (WAV)|*.wav|" + Languages.Translate("All files") + " (*.*)|*.*";
                dialog.Multiselect = false;
                dialog.Title = Languages.Translate("Select media file") + ":";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Чтоб убрать остатки от окна выбора файлов
                    this.Height = this.Window.Height + 1;
                    this.Height = this.Window.Height - 1;

                    PlayVideo(dialog.FileName, MediaLoad.Load);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("ButtonOpen: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                this.StopClip();
        }

        private void CheckIsAudioOnly()
        {
            int hr = 0;
            if (EVRControl != null)
            {
                System.Drawing.Size size, size_ar;
                hr = EVRControl.GetNativeVideoSize(out size, out size_ar);
                this.isAudioOnly = (hr < 0 || size.Width == 0 || size.Height == 0);
            }
            else if ((this.videoWindow == null) || (this.basicVideo == null))
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

        internal delegate void CloseClipDelegate();
        private void CloseClip()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new CloseClipDelegate(CloseClip));
            else
            {
                try
                {
                    //Останавливаем таймер обновления позиции
                    if (timer != null) timer.Stop();

                    if ((this.graphBuilder != null) && (this.VideoElement.Source == null))
                    {
                        if (this.mediaControl != null)
                            this.mediaControl.Stop();

                        this.CloseInterfaces();
                        this.isAudioOnly = true;

                        if (VHost != null)
                        {
                            VHost.Dispose();
                            VHost = null;
                            VHandle = IntPtr.Zero;
                            VHostElement.Child = null;
                            VHostElement.Visibility = Visibility.Collapsed;
                        }
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
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("CloseClip: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

                SetPlayIcon();
                ClearFiltersMenu();

                this.Title = "WPF Video Player";
                this.currentState = PlayState.Init;
                this.textbox_time.Text = textbox_duration.Text = "00:00:00";
                this.slider_pos.Value = 0.0;

                Win7Taskbar.SetProgressState(this.Handle, TBPF.NOPROGRESS);
            }
        }

        private void CloseInterfaces()
        {
            int hr = 0;
            try
            {
                lock (locker)
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

                    if (EVRControl != null)
                    {
                        Marshal.ReleaseComObject(EVRControl);
                        EVRControl = null;
                    }

                    if (this.mediaEventEx != null)
                    {
                        hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                        this.mediaEventEx = null;
                    }

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
                    if (this.graphBuilder != null)
                    {
                        while (Marshal.ReleaseComObject(this.graphBuilder) > 0) ;
                        this.graphBuilder = null;
                    }
                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("CloseInterfaces: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void DD_GotFiles(object sender, string[] files)
        {
            try
            {
                if (File.Exists(files[0]))
                {
                    PlayVideo(files[0], MediaLoad.Load);
                    this.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Drag&Drop: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.currentState != PlayState.Init)
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
            else if (e.Key == Key.Left) { e.Handled = true; if (Keyboard.Modifiers == ModifierKeys.Control) Shift_Frame(-10); else Shift_Frame(-1); }
            else if (e.Key == Key.Right) { e.Handled = true; if (Keyboard.Modifiers == ModifierKeys.Control) Shift_Frame(10); else Shift_Frame(1); }
            else if (e.Key == Key.Up) { e.Handled = true; VolumePlus(); }
            else if (e.Key == Key.Down) { e.Handled = true; VolumeMinus(); }
            else if (e.Key == Key.F1) { e.Handled = true; menu_mediainfo_Click(null, null); }
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
            try
            {
                if (this.videoWindow != null)
                {
                    double left = 0, top = 0;
                    int width = 0, height = 0;

                    if (IsRendererARFixed) //Просто задаем рендереру место под окно, он сам исправит аспект (добавит бордюры)
                    {
                        left = 0;
                        top = toolbar_top.ActualHeight;
                        width = (int)LayoutRoot.ActualWidth;
                        height = (int)(LayoutRoot.ActualHeight - toolbar_top.ActualHeight - toolbar_bottom.ActualHeight - grid_slider.ActualHeight);
                    }
                    else
                    {
                        DsError.ThrowExceptionForHR(this.basicVideo.get_SourceWidth(out width));
                        DsError.ThrowExceptionForHR(this.basicVideo.get_VideoHeight(out height)); //get_SourceHeight ?
                        double asp = ((double)width) / ((double)height);

                        top = toolbar_top.ActualHeight;
                        height = (int)(LayoutRoot.ActualHeight - toolbar_top.ActualHeight - toolbar_bottom.ActualHeight - grid_slider.ActualHeight);
                        width = (int)(asp * height);
                        left = (LayoutRoot.ActualWidth - width) / 2.0;
                        if (width > (int)LayoutRoot.ActualWidth)
                        {
                            width = (int)LayoutRoot.ActualWidth;
                            height = (int)(((double)width) / asp);
                            left = 0;
                            top = (LayoutRoot.ActualHeight - height - grid_slider.ActualHeight) / 2.0;
                        }
                    }

                    //Масштабируем и вводим
                    DsError.ThrowExceptionForHR(this.videoWindow.SetWindowPosition(Convert.ToInt32(left * dpi), Convert.ToInt32(top * dpi),
                       Convert.ToInt32(width * dpi), Convert.ToInt32(height * dpi)));

                    //Заставляем перерисовать окно
                    DsError.ThrowExceptionForHR(this.videoWindow.put_BorderColor(1));
                }
                else if (EVRControl != null && VHost != null)
                {
                    //Масштабируем и вводим
                    MFError.ThrowExceptionForHR(EVRControl.SetVideoPosition(null, new MFRect(0, 0, Convert.ToInt32(dpi * VHostElement.ActualWidth),
                        Convert.ToInt32(dpi * VHostElement.ActualHeight))));
                }
            }
            catch (Exception ex)
            {
                //this.CloseClip();
                //this.filepath = string.Empty;
                System.Windows.MessageBox.Show("MoveVideoWindow: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VHost_RepaintRequired(object sender, EventArgs e)
        {
            if (!isAudioOnly && EVRControl != null)
                EVRControl.RepaintVideo();
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

        void PlayVideo(string file, MediaLoad mediaload)
        {
            try
            {
                this.mediaload = mediaload;

                if (this.currentState != PlayState.Init)
                    this.CloseClip();

                this.filepath = file;
                this.Title = Path.GetFileName(file) + " - " + Languages.Translate("loading") + "...";

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                    this.PlayMovieInWindow();
                else
                    this.PlayWithMediaBridge();

                this.slider_pos.Focus();
                this.Title = Path.GetFileName(file) + " - WPF Video Player";

                //Запускаем таймер обновления позиции
                if (timer != null) timer.Start();
            }
            catch (Exception ex)
            {
                this.CloseClip();
                this.filepath = string.Empty;
                Win7Taskbar.SetProgressTaskComplete(this.Handle, TBPF.ERROR);
                System.Windows.MessageBox.Show("PlayVideo: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayMovieInWindow()
        {
            int hr = 0;
            this.graphBuilder = (IGraphBuilder)new FilterGraph();

            //Добавляем в граф нужный рендерер (Auto - graphBuilder сам выберет рендерер)
            Settings.VRenderers renderer = Settings.VideoRenderer;
            if (renderer == Settings.VRenderers.Overlay)
            {
                IBaseFilter add_vr = (IBaseFilter)new VideoRenderer();
                hr = graphBuilder.AddFilter(add_vr, "Video Renderer");
                DsError.ThrowExceptionForHR(hr);
            }
            else if (renderer == Settings.VRenderers.VMR7)
            {
                IBaseFilter add_vmr = (IBaseFilter)new VideoMixingRenderer();
                hr = graphBuilder.AddFilter(add_vmr, "Video Renderer");
                DsError.ThrowExceptionForHR(hr);
            }
            else if (renderer == Settings.VRenderers.VMR9)
            {
                IBaseFilter add_vmr9 = (IBaseFilter)new VideoMixingRenderer9();
                hr = graphBuilder.AddFilter(add_vmr9, "Video Mixing Renderer 9");
                DsError.ThrowExceptionForHR(hr);
            }
            else if (renderer == Settings.VRenderers.EVR)
            {
                //Создаём Win32-окно, т.к. использовать WPF-поверхность не получится
                VHost = new VideoHwndHost();
                VHost.RepaintRequired += new EventHandler(VHost_RepaintRequired);
                VHostElement.Visibility = Visibility.Visible;
                VHostElement.Child = VHost;
                VHandle = VHost.Handle;

                //Добавляем и настраиваем EVR
                IBaseFilter add_evr = (IBaseFilter)new EnhancedVideoRenderer();
                hr = graphBuilder.AddFilter(add_evr, "Enhanced Video Renderer");
                DsError.ThrowExceptionForHR(hr);

                object obj;
                IMFGetService pGetService = null;
                pGetService = (IMFGetService)add_evr;
                hr = pGetService.GetService(MFServices.MR_VIDEO_RENDER_SERVICE, typeof(IMFVideoDisplayControl).GUID, out obj);
                MFError.ThrowExceptionForHR(hr);

                try
                {
                    EVRControl = (IMFVideoDisplayControl)obj;
                }
                catch
                {
                    Marshal.ReleaseComObject(obj);
                    throw;
                }

                //Указываем поверхность
                hr = EVRControl.SetVideoWindow(VHandle);
                MFError.ThrowExceptionForHR(hr);

                //Сохраняем аспект
                hr = EVRControl.SetAspectRatioMode(MFVideoAspectRatioMode.PreservePicture);
                MFError.ThrowExceptionForHR(hr);
            }

            // Have the graph builder construct its the appropriate graph automatically
            hr = this.graphBuilder.RenderFile(this.filepath, null);
            DsError.ThrowExceptionForHR(hr);

            if (EVRControl == null)
            {
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
            }
            else
                IsRendererARFixed = true;

            this.mediaControl = (IMediaControl)this.graphBuilder;
            this.mediaEventEx = (IMediaEventEx)this.graphBuilder;
            this.mediaSeeking = (IMediaSeeking)this.graphBuilder;
            this.mediaPosition = (IMediaPosition)this.graphBuilder;
            this.videoWindow = (EVRControl == null) ? this.graphBuilder as IVideoWindow : null;
            this.basicVideo = (EVRControl == null) ? this.graphBuilder as IBasicVideo : null;
            this.basicAudio = this.graphBuilder as IBasicAudio;
            this.basicAudio.put_Volume(VolumeSet);
            this.CheckIsAudioOnly();
            if (!this.isAudioOnly)
            {
                if (videoWindow != null)
                {
                    hr = this.videoWindow.put_Owner(this.source.Handle);
                    DsError.ThrowExceptionForHR(hr);

                    hr = this.videoWindow.put_MessageDrain(this.source.Handle);
                    DsError.ThrowExceptionForHR(hr);

                    hr = this.videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings | DirectShowLib.WindowStyle.ClipChildren);
                    DsError.ThrowExceptionForHR(hr);
                }

                this.MoveVideoWindow();
            }
            else
            {
                if (VHost != null)
                {
                    VHost.Dispose();
                    VHost = null;
                    VHandle = IntPtr.Zero;
                    VHostElement.Child = null;
                    VHostElement.Visibility = Visibility.Collapsed;
                }
            }

            hr = this.mediaEventEx.SetNotifyWindow(this.source.Handle, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

            //Восстанавливаем старую позицию
            if (mediaload == MediaLoad.Update && oldpos != TimeSpan.Zero)
            {
                if (NaturalDuration >= oldpos)
                {
                    hr = mediaPosition.put_CurrentPosition(oldpos.TotalSeconds);
                    DsError.ThrowExceptionForHR(hr);
                }
            }

            //Восстанавливаем старый PlayState
            if (mediaload == MediaLoad.Update && (oldplaystate == PlayState.Paused || oldplaystate == PlayState.Stopped))
            {
                hr = this.mediaControl.Pause();
                DsError.ThrowExceptionForHR(hr);
                this.currentState = PlayState.Paused;
                this.SetPlayIcon();
            }
            else
            {
                hr = this.mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);
                this.currentState = PlayState.Running;
                this.SetPauseIcon();
            }

            AddFiltersToMenu();
        }

        private void PlayWithMediaBridge()
        {
            string mediaUrl = "MediaBridge://MyDataString";
            MediaBridgeManager.RegisterCallback(mediaUrl, new MediaBridgeManager.NewMediaGraphInfo(this.BridgeCallback));
            this.VideoElement.Source = new Uri(mediaUrl);
            this.VideoElement.Visibility = Visibility.Visible;

            //Восстанавливаем старый PlayState
            if (mediaload == MediaLoad.Update && (oldplaystate == PlayState.Paused || oldplaystate == PlayState.Stopped))
            {
                this.VideoElement.Play();
                this.VideoElement.Pause();
                this.currentState = PlayState.Paused;
            }
            else
            {
                this.VideoElement.Play();
                this.currentState = PlayState.Running;
                this.SetPauseIcon();
            }
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

        internal delegate void AddFilterDelegate(string filter_name, bool has_properties);
        private void AddFilterToMenu(string filter_name, bool has_properties)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new AddFilterDelegate(AddFilterToMenu), filter_name, has_properties);
            else
            {
                MenuItem item = new MenuItem();
                item.Header = filter_name;
                if (has_properties)
                {
                    item.Click += new RoutedEventHandler(ShowFilterPropertyPage);
                }
                else
                {
                    item.IsEnabled = false;
                    //item.Foreground = Brushes.Gray;
                    //item.StaysOpenOnClick = true;
                }
                menu_filters.Items.Add(item);
                menu_filters.IsEnabled = true;
            }
        }

        //Список задействованных в графе фильтров
        private void AddFiltersToMenu()
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

                            AddFilterToMenu(filterInfo.achName, ((pFilter[0] as ISpecifyPropertyPages) != null) &&
                                !filterInfo.achName.StartsWith("mediabridge"));
                        }

                        Marshal.ReleaseComObject(pFilter[0]);
                    }
                }
            }
            catch (Exception)
            {
                AddFilterToMenu("An error has occurred..", false);
            }
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

        //Вывод окна с параметрами фильтра
        private void ShowFilterPropertyPage(object sender, RoutedEventArgs e)
        {
            int hr = 0;
            DsCAUUID caGuid;
            FilterInfo filterInfo;
            IBaseFilter filter = null;

            try
            {
                hr = this.graphBuilder.FindFilterByName(((MenuItem)sender).Header.ToString(), out filter);
                DsError.ThrowExceptionForHR(hr);

                hr = filter.QueryFilterInfo(out filterInfo);
                DsError.ThrowExceptionForHR(hr);

                if (filterInfo.pGraph != null)
                    Marshal.ReleaseComObject(filterInfo.pGraph);

                if ((filter as ISpecifyPropertyPages) != null)
                {
                    hr = (filter as ISpecifyPropertyPages).GetPages(out caGuid);
                    DsError.ThrowExceptionForHR(hr);

                    //OleCreatePropertyFrame игнорирует параметры x и y, всегда размещая
                    //своё окно в левом верхнем углу родительского окна. Поэтому единственный
                    //способ задать ему позицию - создать своё (невидимое) окно в нужном месте,
                    //и использовать его в качестве родительского для OleCreatePropertyFrame.
                    System.Drawing.Point point = System.Windows.Forms.Cursor.Position;
                    Window wnd = new Window()
                    {
                        Owner = this,
                        WindowStyle = System.Windows.WindowStyle.None,
                        ShowInTaskbar = false,
                        //ShowActivated = false, //Требуется 3.0 SP1, иначе будет MissingMethodException
                        AllowsTransparency = true,
                        Opacity = 0,
                        Width = 0,
                        Height = 0,
                        Left = (point.X / dpi - 25),
                        Top = (point.Y / dpi - 25)
                    };

                    //Но нужно сохранить модальность окна PropertyFrame..
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                    timer.Tick += (a, b) =>
                    {
                        //Крутимся тут, пока наше невидмое окно не отрисуется (а отрисовывается
                        //оно только после подачи ему команды ShowDialog() - и оно модально).
                        IntPtr hwndOwner = new WindowInteropHelper(wnd).Handle;
                        if (hwndOwner != IntPtr.Zero)
                        {
                            try
                            {
                                object[] objs = new object[1];
                                objs[0] = filter;

                                OleCreatePropertyFrame(hwndOwner, 0, 0, filterInfo.achName, objs.Length,
                                    objs, caGuid.cElems, caGuid.pElems, 0, 0, IntPtr.Zero);
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(caGuid.pElems);

                                if (wnd != null)
                                {
                                    wnd.Close();
                                    wnd = null;
                                    this.Activate();
                                }

                                timer.Stop();
                                timer = null;
                            }
                        }
                    };
                    timer.Start();

                    //Из-за этого и был нужен таймер, т.к. код после этой
                    //строчки был бы недоступен, пока окно не будет закрыто..
                    wnd.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("ShowFilterPropertyPage: " + ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (filter != null)
                {
                    Marshal.ReleaseComObject(filter);
                    filter = null;
                }
            }
        }

        [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int OleCreatePropertyFrame(
            [In] IntPtr hwndOwner,
            [In] int x,
            [In] int y,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpszCaption,
            [In] int cObjects,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown)] object[] ppUnk,
            [In] int cPages,
            [In] IntPtr pPageClsID,
            [In] int lcid,
            [In] int dwReserved,
            [In] IntPtr pvReserved
            );

        private void SetPauseIcon()
        {
            image_play.Source = new BitmapImage(new Uri(@"../pictures/pause_new.png", UriKind.RelativeOrAbsolute));
            Win7Taskbar.SetProgressState(this.Handle, TBPF.NORMAL);
        }

        private void SetPlayIcon()
        {
            image_play.Source = new BitmapImage(new Uri(@"../pictures/play_new.png", UriKind.RelativeOrAbsolute));
            Win7Taskbar.SetProgressState(this.Handle, TBPF.PAUSED);
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

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                    this.MoveVideoWindow();
                else
                    this.VideoElement.Margin = new Thickness(0, toolbar_top.ActualHeight, 0, toolbar_bottom.ActualHeight + grid_slider.ActualHeight);
            }
            else
            {
                base.WindowState = oldstate;
                base.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                this.isFullScreen = false;

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
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
                    Win7Taskbar.SetProgressValue(this.Handle, (ulong)slider_pos.Value, (ulong)slider_pos.Maximum);
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
                this.slider_pos.Maximum = NaturalDuration.TotalSeconds;
                TimeSpan tCode = TimeSpan.Parse(TimeSpan.FromSeconds(NaturalDuration.TotalSeconds).ToString().Split('.')[0]);
                textbox_duration.Text = tCode.ToString();

                //Восстанавливаем старую позицию
                if (mediaload == MediaLoad.Update && oldpos != TimeSpan.Zero)
                {
                    if (NaturalDuration >= oldpos)
                        Position = oldpos;
                }
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
                case 0x0203: //0x0203 WM_LBUTTONDBLCLK (0x0201 WM_LBUTTONDOWN, 0x0202 WM_LBUTTONUP)
                    {
                        SwitchToFullScreen(); break;
                    }
                case 0x0205: //0x0205 WM_RBUTTONUP (0x0204 WM_RBUTTONDOWN, 0x0206 WM_RBUTTONDBLCLK)
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
                    //System.Windows.MessageBox.Show(ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                    //System.Windows.MessageBox.Show(ex.Message, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

            PlayState old_state = this.currentState;
            if (this.currentState != PlayState.Init)
            {
                oldplaystate = old_state;
                oldpos = Position;
                CloseClip();
            }

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

                source = HwndSource.FromHwnd(this.Handle);
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
            if (old_state != PlayState.Init)
            {
                PlayVideo(this.filepath, MediaLoad.Update);
            }
        }

        private void Change_renderer_Click(object sender, RoutedEventArgs e)
        {
            Settings.VRenderers new_renderer = 0;
            Settings.VRenderers old_renderer = Settings.VideoRenderer;

            if (vr_Default.IsFocused)
            {
                vr_default.IsChecked = true;
                Settings.VideoRenderer = new_renderer = Settings.VRenderers.Auto;
            }
            else if (vr_Overlay.IsFocused)
            {
                vr_overlay.IsChecked = true;
                Settings.VideoRenderer = new_renderer = Settings.VRenderers.Overlay;
            }
            else if (vr_VMR7.IsFocused)
            {
                vr_vmr7.IsChecked = true;
                Settings.VideoRenderer = new_renderer = Settings.VRenderers.VMR7;
            }
            else if (vr_VMR9.IsFocused)
            {
                vr_vmr9.IsChecked = true;
                Settings.VideoRenderer = new_renderer = Settings.VRenderers.VMR9;
            }
            else if (vr_EVR.IsFocused)
            {
                vr_evr.IsChecked = true;
                Settings.VideoRenderer = new_renderer = Settings.VRenderers.EVR;
            }

            if (old_renderer != new_renderer && this.currentState != PlayState.Init && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
            {
                oldpos = Position;
                oldplaystate = currentState;

                CloseClip();
                PlayVideo(this.filepath, MediaLoad.Update);
            }
        }

        private void check_Old_Seeking_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.OldSeeking = OldSeeking = check_old_seeking.IsChecked; 
        }

        private void menu_mediainfo_Click(object sender, RoutedEventArgs e)
        {
            new MediaInfo(filepath, this);
        }

        private void check_win7_taskbar_Clicked(object sender, RoutedEventArgs e)
        {
            if ((Settings.Win7TaskbarIsEnabled = check_win7_taskbar.IsChecked))
            {
                if (!Win7Taskbar.InitializeWin7Taskbar())
                {
                    System.Windows.MessageBox.Show(Languages.Translate("Failed to initialize Windows 7 taskbar interface.") +
                    " " + Languages.Translate("This feature will be disabled!"), Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    check_win7_taskbar.IsChecked = Settings.Win7TaskbarIsEnabled = false;
                }
            }
            else
            {
                Win7Taskbar.UninitializeWin7Taskbar();
            }
        }

        private void check_window_size_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.WindowResize = check_window_size.IsChecked;
        }

        private void check_window_pos_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.CheckWindowsPos = check_window_pos.IsChecked;
        }

        //Коэффициент для масштабирования DIP<->DDP
        private void RetrieveDPI()
        {
            try
            {
                if (!this.IsVisible)
                {
                    //Первый дефолт
                    dpi = 0;

                    IntPtr ScreenDC = IntPtr.Zero;
                    try
                    {
                        //Когда окна еще нет
                        ScreenDC = GetDC(IntPtr.Zero); //88-x(w), 90-y(h)
                        dpi = (double)GetDeviceCaps(ScreenDC, 88) / 96.0;
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, ScreenDC);
                    }
                }
                else
                {
                    //Второй дефолт
                    dpi = 1;

                    //Когда окно уже есть (это вторая попытка)
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null)
                    {
                        CompositionTarget target = source.CompositionTarget; //M11-x(w), M22-y(h)
                        if (target != null) dpi = source.CompositionTarget.TransformToDevice.M11;
                    }
                }
            }
            catch (Exception) { }
        }

        public void CheckWindowPos(Window wnd, IntPtr hwnd, bool limit_size)
        {
            try
            {
                if (!wnd.IsVisible || hwnd == IntPtr.Zero)
                    return;

                //Монитор, на котором окно занимает бОльшую площадь (или ближайший к окну).
                //SystemParameters.WorkArea - только для основного монитора, но уже с dpi.
                Rectangle _WorkingArea = System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;

                //Масштабируем под dpi
                Rect WorkingArea = (_WorkingArea.Width <= 0 || _WorkingArea.Height <= 0) ? SystemParameters.WorkArea :
                    new Rect(_WorkingArea.X / dpi, _WorkingArea.Y / dpi, _WorkingArea.Width / dpi, _WorkingArea.Height / dpi);

                //Ограничение размеров окна
                if (limit_size)
                {
                    wnd.MaxWidth = WorkingArea.Width;
                    wnd.MaxHeight = WorkingArea.Height;
                    wnd.UpdateLayout();
                }

                //Вписывание в границы
                if (wnd.ActualWidth > 0 && wnd.ActualHeight > 0 && !double.IsNaN(wnd.Left) && !double.IsNaN(wnd.Top))
                {
                    if (wnd.Left < WorkingArea.Left)
                    {
                        //Слева
                        wnd.Left = WorkingArea.Left;
                    }
                    else if (wnd.Left - WorkingArea.Left + wnd.ActualWidth > WorkingArea.Width)
                    {
                        //Справа
                        wnd.Left = WorkingArea.Width - wnd.ActualWidth + WorkingArea.Left;
                    }

                    if (wnd.Top < WorkingArea.Top)
                    {
                        //Сверху
                        wnd.Top = WorkingArea.Top;
                    }
                    else if (wnd.Top - WorkingArea.Top + wnd.ActualHeight > WorkingArea.Height)
                    {
                        //Снизу
                        wnd.Top = WorkingArea.Height - wnd.ActualHeight + WorkingArea.Top;
                    }
                }
            }
            catch (Exception) { }
        }
    }
}