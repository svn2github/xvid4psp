using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Input;
using DirectShowLib;
using DirectShowLib.Dvd;
using MediaBridge;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections;
using System.Text;
using System.Windows.Media.Imaging;

namespace XviD4PSP
{
    internal enum MediaType
    {
        Audio,
        Video
    }

    internal enum PlayState
    {
        Stopped,
        Paused,
        Running,
        Init
    }

    public partial class DVDImport
    {
        private const int WMGraphNotify = 0x0400 + 13;
        private const int VolumeFull = 0;
        private const int VolumeSilence = -10000;

        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaControl = null;
        private IMediaEventEx mediaEventEx = null;
        private IVideoWindow videoWindow = null;
        private IBasicAudio basicAudio = null;
        private IBasicVideo basicVideo = null;
        private IMediaSeeking mediaSeeking = null;
        private IMediaPosition mediaPosition = null;
        private IVideoFrameStep frameStep = null;

        private string filepath = string.Empty;
        private bool isAudioOnly = false;
        private PlayState currentState = PlayState.Stopped;

        private IntPtr hDrain = IntPtr.Zero;

        private HwndSource source;
        private System.Timers.Timer timer;
        private BackgroundWorker worker = null;
        public Massive m;
        private ArrayList dvd;
        private bool isInfoLoading = true;

        private IFilterGraph graph;

#if DEBUG
        private DsROTEntry rot = null;
#endif

        public DVDImport(Massive mass, string dvdpath)
        {
            this.InitializeComponent();

            this.Owner = mass.owner;
            m = mass.Clone();

            //tooltips
            label_title.Content = Languages.Translate("Select title:");
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");       

            //events
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            this.KeyUp += new KeyEventHandler(MainWindow_KeyUp);

            //подготавливаем список титлов
            string[] maintitles = Directory.GetFiles(dvdpath, "VTS_*1.VOB", SearchOption.AllDirectories);
            int titlescount = maintitles.Length;

            //если нет ни одного титла
            if (titlescount == 0)
            {
                Message message = new Message(this.Owner);
                message.ShowMessage(Languages.Translate("Can`t find any VOB file in:") + 
                    " \"" + dvdpath + "\"!", Languages.Translate("Error")); 
                m = null;
                Close();
                return;
            }

            //сортируем и забиваем вобы в список
            dvd = new ArrayList();
            string[] vobs = new string[] { "" };
            for (int n = 0; n < titlescount; n++)
            {
                string title = Calculate.GetTitleNum(maintitles[n]);
                vobs = Directory.GetFiles(dvdpath, "VTS_" + title + "*.VOB", SearchOption.AllDirectories);
                ArrayList vobs_ar = Calculate.ConvertStringArrayToArrayList(vobs);
                if (vobs_ar[0].ToString().ToUpper().EndsWith("0.VOB"))
                    vobs_ar.RemoveAt(0);
                vobs = Calculate.ConvertArrayListToStringArray(vobs_ar);
                dvd.Add(vobs);
            }

            //забиваем и выделяем пустой титл
            combo_titles.Items.Add(Calculate.GetTimeline(0));
            combo_titles.SelectedIndex = 0;
            Title = "DVD: " + Calculate.GetDVDName(vobs[0]);

            this.ShowDialog();
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                if (e.Key == Key.Space)
                {
                    PauseClip();
                }
            }
        }

        internal delegate void MainFormLoaderDelegate();
        private void MainFormLoader()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MainFormLoaderDelegate(MainFormLoader));
            else
            {
                        try
                        {
                            // If we have ANY file open, close it and shut down DirectShow
                            if (this.currentState != PlayState.Init)
                                CloseClip();

                            //список плохих титлов
                            ArrayList badlist = new ArrayList();

                            if (isInfoLoading)
                            {
                                //забиваем длительность
                                foreach (object obj in dvd)
                                {
                                    string[] titles = (string[])obj;
                                    string ifopath = Calculate.GetIFO(titles[0]);

                                    if (File.Exists(ifopath))
                                    {
                                        //получаем информацию из ифо
                                        VStripWrapper vs = new VStripWrapper();
                                        vs.Open(ifopath);
                                        double titleduration = vs.Duration().TotalSeconds;
                                        string info = vs.Width() + "x" + vs.Height() + " " + vs.System();
                                        vs.Close();

                                        string titlenum = Calculate.GetTitleNum(titles[0]);
                                        combo_titles.Items.Add("T" + titlenum + " " + info + " " +  Calculate.GetTimeline(titleduration));
                                    }
                                    //метод если нет IFO
                                    else
                                    {
                                        try
                                        {
                                            int n = 0;
                                            double titleduration = 0.0;
                                            string info = "";
                                            foreach (string tilte in titles)
                                            {
                                                int hr = 0;

                                                this.graphBuilder = (IGraphBuilder)new FilterGraph();

                                                // Have the graph builder construct its the appropriate graph automatically
                                                hr = this.graphBuilder.RenderFile(tilte, null);
                                                DsError.ThrowExceptionForHR(hr);

                                                // QueryInterface for DirectShow interfaces
                                                this.mediaControl = (IMediaControl)this.graphBuilder;
                                                this.mediaPosition = (IMediaPosition)this.graphBuilder;

                                                //определяем длительность
                                                double cduration = 0.0;
                                                hr = mediaPosition.get_Duration(out cduration);
                                                DsError.ThrowExceptionForHR(hr);

                                                //определяем различные параметры
                                                if (n == 0)
                                                {
                                                    this.basicVideo = this.graphBuilder as IBasicVideo;
                                                    //this.basicAudio = this.graphBuilder as IBasicAudio;
                                                    int resw;
                                                    int resh;
                                                    double AvgTimePerFrame;
                                                    hr = basicVideo.get_SourceWidth(out resw);
                                                    DsError.ThrowExceptionForHR(hr);
                                                    hr = basicVideo.get_SourceHeight(out resh);
                                                    DsError.ThrowExceptionForHR(hr);
                                                    hr = basicVideo.get_AvgTimePerFrame(out AvgTimePerFrame);
                                                    double framerate = 1 / AvgTimePerFrame;
                                                    string system = "NTSC";
                                                    if (framerate == 25.0)
                                                        system = "PAL";

                                                    info += resw + "x" + resh + " " + system + " ";
                                                }

                                                //освобождаем ресурсы
                                                CloseInterfaces();

                                                titleduration += cduration;

                                                n++;
                                            }

                                            string titlenum = Calculate.GetTitleNum(titles[0]);
                                            combo_titles.Items.Add("T" + titlenum + " " + info + Calculate.GetTimeline(titleduration));
                                        }
                                        catch
                                        {
                                            badlist.Add(obj);
                                            CloseInterfaces();
                                        }
                                    }
                                }

                                combo_titles.Items.RemoveAt(0);
                                combo_titles.SelectedIndex = 0;
                                this.isInfoLoading = false;
                            }

                            //удаляем плохие титлы
                            foreach (object obj in badlist)
                                dvd.Remove(obj);

                            //загружаем титл
                            string[] deftitles = (string[])dvd[combo_titles.SelectedIndex];
                            this.filepath = deftitles[0];
                            string title_s = Calculate.GetTitleNum(this.filepath);
                            textbox_name.Text = Calculate.GetDVDName(this.filepath) + " T" + title_s;
                            m.infilepath = this.filepath;
                            m.infileslist = deftitles;

                            // Reset status variables
                            this.currentState = PlayState.Stopped;
                            //this.currentVolume = VolumeFull;

                            // Start playing the media file
                            if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                                PlayMovieInWindow(this.filepath);
                            if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                                PlayWithMediaBridge(this.filepath);
                        }
                        catch (Exception ex)
                        {
                            textbox_name.Text = "";
                            CloseClip();
                            this.filepath = String.Empty;
                            MessageBox.Show(ex.Message);
                        }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //string[] args = Environment.GetCommandLineArgs();
            //if (args.Length > 1 && File.Exists(args[1]))
                textbox_name.Text = Languages.Translate("loading") + "...";
            //else
            //    textbox_name.Text = "";
            textbox_time.Text = "00:00:00";

            if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
            {
                this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);

                source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                source.AddHook(new HwndSourceHook(WndProc));

                //timer
                timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                timer.Interval = 10;
                timer.Enabled = true;
                timer.Start();

                VideoElement.Visibility = Visibility.Collapsed;
            }
            if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
            {
                //set media element state for video loading
                VideoElement.LoadedBehavior = MediaState.Manual;
                //VideoElement.UnloadedBehavior = MediaState.Stop;
                VideoElement.ScrubbingEnabled = true;

                //events
                VideoElement.MediaOpened += new RoutedEventHandler(VideoElement_MediaOpened);
                VideoElement.MediaEnded += new RoutedEventHandler(VideoElement_MediaEnded);
                VisualTarget.Rendering += new EventHandler(VisualTarget_Rendering);

                VideoElement.Visibility = Visibility.Visible;
            }

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainFormLoader();
        }

        private void LayoutRoot_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
        }

        private void LayoutRoot_Drop(object sender, System.Windows.DragEventArgs e)
        {

            foreach (string dropfile in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                this.filepath = dropfile;
                break;
            }

            if (this.filepath != null)
            {
                try
                {
                    textbox_name.Text = Languages.Translate("loading") + "...";

                    // If we have ANY file open, close it and shut down DirectShow
                    if (this.currentState != PlayState.Init)
                        CloseClip();

                    textbox_name.Text = Path.GetFileName(this.filepath);

                    // Reset status variables
                    this.currentState = PlayState.Stopped;
                    //this.currentVolume = VolumeFull;

                    // Start playing the media file
                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                        PlayMovieInWindow(this.filepath);
                    if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                        PlayWithMediaBridge(this.filepath);
                }
                catch (Exception ex)
                {
                    textbox_name.Text = "";
                    CloseClip();
                    this.filepath = String.Empty;
                    MessageBox.Show(ex.Message);
                }
            }
        }

        internal delegate void UpdateClockDelegate();
        private void UpdateClock()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateClockDelegate(UpdateClock));
            else
            {
                if (this.graphBuilder != null)
                {
                    progress_top.Width = (Position.TotalSeconds / NaturalDuration.TotalSeconds) * progress_back.ActualWidth;
                    TimeSpan tCode = TimeSpan.Parse(Position.ToString().Split('.')[0]);
                    textbox_time.Text = tCode.ToString();

                    Visual visual = Mouse.Captured as Visual;
                    if (visual == null)
                    {
                        slider_pos.Value = Position.TotalSeconds;
                    }
                }
            }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                UpdateClock();
            }
        }

        //private void grid_player_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.Clicks == 2 && e.Button == System.Windows.Forms.MouseButtons.Left && graphBuilder != null)
        //    {
        //        SwitchToFullScreen();
        //    }
        //}

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopClip();
            CloseInterfaces();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!this.isAudioOnly)
                MoveVideoWindow();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!this.isAudioOnly)
                MoveVideoWindow();
        }

        private void button_open_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                string files = Languages.Translate("files");

                System.Windows.Forms.OpenFileDialog o = new System.Windows.Forms.OpenFileDialog();
                o.Filter = Languages.Translate("All media files") + "|*.avi;*.divx;*.wmv;*.mpg;*.mpe;*.mpeg;*.mod;*.asf;*.mkv;*.mov;*.qt;*.3gp;*.hdmov;*.mp4;*.ogm;*.avs;*.vob;*.dvr-ms;*.ts;*.m2p;*.m2t;*.m2v;*.d2v;*.m2ts;*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil;*.flv;*.pmp;*.mp3;*.wma;*.ogg;*.aac;*.m4a;*.dts;*.ac3;*.wav|" +
                    "AVI " + files + " (AVI DIVX ASF)|*.avi;*.divx;*.asf|" +
                    "MPEG " + files + " (MPG MPE MPEG VOB MOD TS M2P M2T M2TS)|*.mpg;*.mpe;*.mpeg;*.vob;*.ts;*.m2p;*.m2t;*.mod;*.m2ts|" +
                    "DGIndex " + files + " (D2V)|*.d2v|" +
                    "QuickTime " + files + " (MOV QT 3GP HDMOV)|*.mov;*.qt;*.3gp;*.hdmov|" +
                    "RealVideo " + files + " (RM RAM RMVB RPX SMI SMIL)|*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil|" +
                    "Matroska " + files + " (MKV)|*.mkv|" +
                    "OGG Media (Vorbis) " + files + " (OGM)|*.ogm|" +
                    "Windows Media " + files + " (WMV)|*.wmv|" +
                    "Media Center " + files + " (DVR-MS)|*.dvr-ms|" +
                    "PMP " + files + " (PMP)|*.pmp|" +
                    "Flash Video " + files + " (FLV)|*.flv|" +
                    "MP3 " + files + " (MP3)|*.mp3|" +
                    "OGG Vorbis " + files + " (OGG)|*.ogg|" +
                    "Windows media audio " + files + " (WMA)|*.wma|" +
                    "DTS " + files + " (DTS)|*.dts|" +
                    "AC3 " + files + " (AC3)|*.ac3|" +
                    "AAC " + files + " (AAC)|*.aac;*.m4a|" +
                    "Windows PCM " + files + " (WAV)|*.wav|" +
                    Languages.Translate("All files") + " (*.*)|*.*";
                o.Multiselect = false;
                o.Title = Languages.Translate("Select media file") + ":";

                if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textbox_name.Text = Languages.Translate("loading") + "...";

                    // If we have ANY file open, close it and shut down DirectShow
                    if (this.currentState != PlayState.Init)
                        CloseClip();

                    this.filepath = o.FileName;
                    textbox_name.Text = Path.GetFileName(filepath);

                    // Reset status variables
                    this.currentState = PlayState.Stopped;
                    //this.currentVolume = VolumeFull;

                    // Start playing the media file
                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                        PlayMovieInWindow(this.filepath);
                    if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                        PlayWithMediaBridge(this.filepath);
                }
            }
            catch (Exception ex)
            {
                this.textbox_name.Text = "";
                CloseClip();
                this.filepath = String.Empty;
                MessageBox.Show(ex.Message);
            }
        }

        private void button_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                this.textbox_name.Text = "";
                CloseClip();
                this.filepath = String.Empty;
            }
        }

        private void button_play_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                PauseClip();
            }
        }

        private void button_stop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                StopClip();
            }
        }

        private void slider_pos_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_pos.IsMouseOver && this.graphBuilder != null)
            {
                Visual visual = Mouse.Captured as Visual;
                if (visual != null && visual.IsDescendantOf(slider_pos))
                {
                    Position = TimeSpan.FromSeconds(slider_pos.Value);
                }
            }
        }

        private void CloseClip()
        {
            if (this.graphBuilder != null && this.VideoElement.Source == null)
            {
                int hr = 0;

                // Stop media playback
                if (this.mediaControl != null)
                    hr = this.mediaControl.Stop();

                // Clear global flags
                this.currentState = PlayState.Stopped;
                //this.isAudioOnly = true;

                // Free DirectShow interfaces
                CloseInterfaces();

                this.isAudioOnly = true; //Перенесено сюда

                // Clear file name to allow selection of new file with open dialog
                //this.filepath = string.Empty;

                // No current media state
                this.currentState = PlayState.Init;

                //update titles
                //textbox_name.Text = "";
                textbox_time.Text = "00:00:00";
                progress_top.Width = 0.0;
                slider_pos.Value = 0.0;
            }

            if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                VideoElement.Source = null;
                this.currentState = PlayState.Init;

                //update titles
                //textbox_name.Text = "";
                textbox_time.Text = "00:00:00";
                progress_top.Width = 0.0;
                slider_pos.Value = 0.0;

                if (this.graphBuilder != null)
                {
                    while (Marshal.ReleaseComObject(this.graphBuilder) > 0) ;
                    this.graphBuilder = null;
                    if (this.graph != null)
                    {
                        while (Marshal.ReleaseComObject(this.graph) > 0) ;
                        this.graph = null;
                    }
                    //Marshal.ReleaseComObject(this.graphBuilder);
                    //this.graphBuilder = null;
                    //Marshal.ReleaseComObject(this.graph);
                    //this.graph = null;
                    GC.Collect();
                }

                Thread.Sleep(100);

                string url = "MediaBridge://MyDataString";
                MediaBridge.MediaBridgeManager.UnregisterCallback(url);
            }
        }

        private void CloseInterfaces()
        {
            int hr = 0;

            try
            {
                lock (this)
                {
                    // Relinquish ownership (IMPORTANT!) after hiding video window
                    if (!this.isAudioOnly && this.videoWindow != null)//////////////////////////////////////
                    {
                        hr = this.videoWindow.put_Visible(OABool.False);
                        DsError.ThrowExceptionForHR(hr);
                        hr = this.videoWindow.put_Owner(IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    if (this.mediaEventEx != null)
                    {
                        hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                    }

#if DEBUG
                    if (rot != null)
                    {
                        rot.Dispose();
                        rot = null;
                    }
#endif
                    // Release and zero DirectShow interfaces
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
                    //bad way
                    //if (this.graphBuilder != null)
                    //Marshal.ReleaseComObject(this.graphBuilder); this.graphBuilder = null;

                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void PlayMovieInWindow(string filename)
        {
            int hr = 0;

            //grid_player.Visibility = Visibility.Visible;

            if (filename == string.Empty)
                return;

            this.graphBuilder = (IGraphBuilder)new FilterGraph();

            // Have the graph builder construct its the appropriate graph automatically
            hr = this.graphBuilder.RenderFile(filename, null);
            DsError.ThrowExceptionForHR(hr);

            // QueryInterface for DirectShow interfaces
            this.mediaControl = (IMediaControl)this.graphBuilder;
            this.mediaEventEx = (IMediaEventEx)this.graphBuilder;
            this.mediaSeeking = (IMediaSeeking)this.graphBuilder;
            this.mediaPosition = (IMediaPosition)this.graphBuilder;

            // Query for video interfaces, which may not be relevant for audio files
            this.videoWindow = this.graphBuilder as IVideoWindow;
            this.basicVideo = this.graphBuilder as IBasicVideo;

            // Query for audio interfaces, which may not be relevant for video-only files
            this.basicAudio = this.graphBuilder as IBasicAudio;

            // Is this an audio-only file (no video component)?
            CheckVisibility();

            // Have the graph signal event via window callbacks for performance
            hr = this.mediaEventEx.SetNotifyWindow(this.source.Handle, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

            if (!this.isAudioOnly)
            {
                // Setup the video window
                hr = this.videoWindow.put_Owner(this.source.Handle);
                DsError.ThrowExceptionForHR(hr);

                hr = this.videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child |
                    DirectShowLib.WindowStyle.ClipSiblings |
                    DirectShowLib.WindowStyle.ClipChildren);
                DsError.ThrowExceptionForHR(hr);

                MoveVideoWindow();

                GetFrameStepInterface();
            }

#if DEBUG
            rot = new DsROTEntry(this.graphBuilder);
#endif

            this.Focus();

            // Run the graph to play the media file
            hr = this.mediaControl.Run();
            DsError.ThrowExceptionForHR(hr);

            this.currentState = PlayState.Running;
            SetPauseIcon();

            double duration = 0.0;
            hr = mediaPosition.get_Duration(out duration);
            DsError.ThrowExceptionForHR(hr);
            slider_pos.Maximum = duration;
        }

        private void MoveVideoWindow()
        {
            int hr = 0;

            // Track the movement of the container window and resize as needed
            if (this.videoWindow != null)
            {
                int w;
                int h;
                hr = basicVideo.get_SourceWidth(out w);
                DsError.ThrowExceptionForHR(hr);
                hr = basicVideo.get_VideoHeight(out h);
                DsError.ThrowExceptionForHR(hr);

                double aspect = (double)w / (double)h;

                int top = (int)(grid_top.ActualHeight + grid_player_info.ActualHeight + 12);
                h = (int)(this.LayoutRoot.ActualHeight - top - grid_buttons.ActualHeight - slider_pos.ActualHeight - 6);
                w = (int)(aspect * h);
                int left = (int)((this.LayoutRoot.ActualWidth - w - 16) / 2);

                if (w > (int)progress_back.ActualWidth)
                {
                    w = (int)progress_back.ActualWidth;
                    h = (int)((double)w / aspect);
                    left = (int)((this.LayoutRoot.ActualWidth - w) / 2);
                    top = (int)((this.LayoutRoot.ActualHeight - h) / 2.0) + 14;
                }

                hr = this.videoWindow.SetWindowPosition(left, top, w, h);
                DsError.ThrowExceptionForHR(hr);
            }
        }

        private void CheckVisibility()
        {
            int hr = 0;
            OABool lVisible;

            if ((this.videoWindow == null) || (this.basicVideo == null))
            {
                // Audio-only files have no video interfaces.  This might also
                // be a file whose video component uses an unknown video codec.
                this.isAudioOnly = true;
                return;
            }
            else
            {
                // Clear the global flag
                this.isAudioOnly = false;
            }

            hr = this.videoWindow.get_Visible(out lVisible);
            if (hr < 0)
            {
                // If this is an audio-only clip, get_Visible() won't work.
                //
                // Also, if this video is encoded with an unsupported codec,
                // we won't see any video, although the audio will work if it is
                // of a supported format.
                if (hr == unchecked((int)0x80004002)) //E_NOINTERFACE
                {
                    this.isAudioOnly = true;
                }
                else
                    DsError.ThrowExceptionForHR(hr);
            }
        }

        //
        // Some video renderers support stepping media frame by frame with the
        // IVideoFrameStep interface.  See the interface documentation for more
        // details on frame stepping.
        //
        private bool GetFrameStepInterface()
        {
            int hr = 0;

            IVideoFrameStep frameStepTest = null;

            // Get the frame step interface, if supported
            frameStepTest = (IVideoFrameStep)this.graphBuilder;

            // Check if this decoder can step
            hr = frameStepTest.CanStep(0, null);
            if (hr == 0)
            {
                this.frameStep = frameStepTest;
                return true;
            }
            else
            {
                // BUG 1560263 found by husakm (thanks)...
                // Marshal.ReleaseComObject(frameStepTest);
                this.frameStep = null;
                return false;
            }
        }

        private void PauseClip()
        {
            if (this.mediaControl != null)
            {
                // Toggle play/pause behavior
                if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Stopped))
                {
                    if (this.mediaControl.Run() >= 0)
                        this.currentState = PlayState.Running;
                    SetPauseIcon();
                }
                else
                {
                    if (this.mediaControl.Pause() >= 0)
                        this.currentState = PlayState.Paused;
                    SetPlayIcon();
                }
            }

            if (VideoElement.Source != null)
            {
                if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Stopped))
                {
                    VideoElement.Play();
                    this.currentState = PlayState.Running;
                    SetPauseIcon();
                }
                else
                {
                    VideoElement.Pause();
                    this.currentState = PlayState.Paused;
                    SetPlayIcon();
                }
            }
        }

        private void StopClip()
        {
            int hr = 0;
            DsLong pos = new DsLong(0);

            if ((this.mediaControl != null) && (this.mediaSeeking != null))
            {

                // Stop and reset postion to beginning
                if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Running))
                {
                    hr = this.mediaControl.Stop();
                    this.currentState = PlayState.Stopped;
                    
                    // Seek to the beginning
                    hr = this.mediaSeeking.SetPositions(pos, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
                    
                    Thread.Sleep(100); //Иначе в некоторых случаях будет зависание или вылет после сикинга
                    
                    // Display the first frame to indicate the reset condition
                    hr = this.mediaControl.Pause();
                }
            }

            if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                this.currentState = PlayState.Stopped;
            }

            SetPlayIcon();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WMGraphNotify:
                    {
                        HandleGraphEvent();
                        break;
                    }
            }
            return IntPtr.Zero;
        }

        private void HandleGraphEvent()
        {
            int hr = 0;
            EventCode evCode;
            IntPtr evParam1, evParam2;

            // Make sure that we don't access the media event interface
            // after it has already been released.
            if (this.mediaEventEx == null)
                return;

            // Process all queued events
            while (this.mediaEventEx.GetEvent(out evCode, out evParam1, out evParam2, 0) == 0)
            {
                // Free memory associated with callback, since we're not using it
                hr = this.mediaEventEx.FreeEventParams(evCode, evParam1, evParam2);

                // If this is the end of the clip, reset to beginning
                if (evCode == EventCode.Complete)
                {
                    StopClip();
                }
            }
        }

        private void PlayWithMediaBridge(string filepath)
        {
            this.filepath = filepath;
            string url = "MediaBridge://MyDataString";
            MediaBridge.MediaBridgeManager.RegisterCallback(url, BridgeCallback);

            VideoElement.Source = new Uri(url);
            VideoElement.Play();

            this.currentState = PlayState.Running;
        }

        private void BridgeCallback(MediaBridge.MediaBridgeGraphInfo GraphInfo)
        {
            try
            {
                int hr = 0;

                //Convert pointer of filter graph to an object we can use
                graph = (IFilterGraph)Marshal.GetObjectForIUnknown(GraphInfo.FilterGraph);
                graphBuilder = (IGraphBuilder)graph;

                IBaseFilter videoRenderer;
                ////Find WPF renderer.  It's always named the same thing
                ////hr = graphBuilder.FindFilterByName("Avalon EVR", out videoRenderer);
                ////hr = graphBuilder.FindFilterByName("Video Renderer", out videoRenderer);
                hr = graphBuilder.FindFilterByName("Enhanced Video Renderer", out videoRenderer);
                DsError.ThrowExceptionForHR(hr);

                //hr = this.graphBuilder.AddFilter(videoRenderer, "Enhanced Video Renderer");
                //DsError.ThrowExceptionForHR(hr);

                hr = graphBuilder.RenderFile(filepath, null);
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void VideoElement_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            if (VideoElement.HasVideo ||
                VideoElement.HasAudio)
            {
                slider_pos.Maximum = VideoElement.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }

        private void VideoElement_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            StopClip();
        }

        private void VisualTarget_Rendering(object sender, EventArgs e)
        {
            if (VideoElement.NaturalDuration.HasTimeSpan)
            {
                progress_top.Width = (VideoElement.Position.TotalSeconds / VideoElement.NaturalDuration.TimeSpan.TotalSeconds) * progress_back.ActualWidth;
                TimeSpan tCode = TimeSpan.Parse(VideoElement.Position.ToString().Split('.')[0]);
                textbox_time.Text = tCode.ToString();

                Visual visual = Mouse.Captured as Visual;
                if (visual == null)
                {
                    slider_pos.Value = VideoElement.Position.TotalSeconds;
                }
            }
        }

        public TimeSpan NaturalDuration
        {
            get
            {
                if (this.graphBuilder != null && this.VideoElement.Source == null)
                {
                    int hr = 0;
                    double duration;
                    hr = mediaPosition.get_Duration(out duration);
                    DsError.ThrowExceptionForHR(hr);
                    return TimeSpan.FromSeconds(duration);
                }
                else if (this.graphBuilder != null && this.VideoElement.Source != null)
                {
                    return this.VideoElement.NaturalDuration.TimeSpan;
                }
                else
                    return TimeSpan.Zero;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (this.graphBuilder != null && this.VideoElement.Source == null)
                {
                    int hr = 0;
                    double position;
                    hr = mediaPosition.get_CurrentPosition(out position);
                    DsError.ThrowExceptionForHR(hr);
                    return TimeSpan.FromSeconds(position);
                }
                else if (this.graphBuilder != null && this.VideoElement.Source != null)
                {
                    return this.VideoElement.Position;
                }
                else
                    return TimeSpan.Zero;
            }
            set
            {
                if (this.graphBuilder != null && this.VideoElement.Source == null)
                    mediaPosition.put_CurrentPosition(value.TotalSeconds);
                else if (this.graphBuilder != null && this.VideoElement.Source != null)
                    VideoElement.Position = value;
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CloseClip();
            this.Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m = null;
            CloseClip();
            this.Close();
        }

        private void combo_titles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_titles.IsDropDownOpen || combo_titles.IsSelectionBoxHighlighted)
            {
                worker.RunWorkerAsync();
            }
        }

        private void SetPauseIcon()
        {
            // Create source.
            BitmapImage bi = new BitmapImage();
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            bi.BeginInit();
            bi.UriSource = new Uri(@"../pictures/pause_new.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
            image_play.Source = bi;
        }

        private void SetPlayIcon()
        {
            // Create source.
            BitmapImage bi = new BitmapImage();
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            bi.BeginInit();
            bi.UriSource = new Uri(@"../pictures/play_new.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
            image_play.Source = bi;
        }

    }
}