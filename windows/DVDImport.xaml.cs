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

        private static object locker = new object();
        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaControl = null;
        private IMediaEventEx mediaEventEx = null;
        private IVideoWindow videoWindow = null;
        private IBasicAudio basicAudio = null;
        private IBasicVideo basicVideo = null;
        private IMediaSeeking mediaSeeking = null;
        private IMediaPosition mediaPosition = null;
        private IMFVideoDisplayControl EVRControl = null;
        private VideoHwndHost VHost = null;
        private IntPtr VHandle = IntPtr.Zero;

        private double dpi = 1.0;
        private double in_ar = 0;
        private string filepath = string.Empty;
        private bool isAudioOnly = false;
        private PlayState currentState = PlayState.Stopped;

        private HwndSource source;
        private System.Timers.Timer timer;
        private BackgroundWorker worker = null;
        public Massive m;
        private ArrayList dvd;
        private bool isInfoLoading = true;
        private IFilterGraph graph;

        public DVDImport(Massive mass, string dvdpath, double dpi)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();
            this.dpi = dpi;

            //tooltips
            label_title.Content = Languages.Translate("Select title:");
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_play.ToolTip = Languages.Translate("Play-Pause");
            button_stop.ToolTip = Languages.Translate("Stop");

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
                        double maximum = 0;
                        int index_of_maximum = 0;
                        int num = 0;

                        //забиваем длительность
                        foreach (object obj in dvd)
                        {
                            string[] titles = (string[])obj;
                            string ifopath = Calculate.GetIFO(titles[0]);

                            if (File.Exists(ifopath))
                            {
                                MediaInfoWrapper media = new MediaInfoWrapper();
                                media.Open(ifopath);
                                string info = media.Width + "x" + media.Height + " " + media.AspectString + " " + media.Standart;
                                media.Close();

                                //получаем информацию из ифо
                                VStripWrapper vs = new VStripWrapper();
                                vs.Open(ifopath);
                                double titleduration = vs.Duration().TotalSeconds;
                                //string   info = vs.Width() + "x" + vs.Height() + " " + vs.System(); //При обращении к ifoGetVideoDesc на некоторых системах происходит вылет VStrip.dll..
                                //Теперь нужная инфа будет браться из МедиаИнфо..
                                vs.Close();

                                string titlenum = Calculate.GetTitleNum(titles[0]);
                                combo_titles.Items.Add("T" + titlenum + " " + info + " " + Calculate.GetTimeline(titleduration) + " - " + titles.Length + " file(s)");

                                //Ищем самый продолжительный титл
                                if (titleduration > maximum)
                                {
                                    maximum = titleduration;
                                    index_of_maximum = num;
                                }
                                num += 1;
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
                        combo_titles.SelectedIndex = index_of_maximum;
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

                    //Определяем аспект для DirectShow плейера (чтоб не вызывать еще раз MediaInfo)
                    string aspect = Calculate.GetRegexValue(@"\s\d+x\d+\s(\d+:\d+)\s", combo_titles.SelectedItem.ToString());
                    if (!string.IsNullOrEmpty(aspect))
                    {
                        if (aspect == "4:3") in_ar = 4.0 / 3.0;
                        else if (aspect == "16:9") in_ar = 16.0 / 9.0;
                        else if (aspect == "2.2:1") in_ar = 2.21;
                    }

                    // Reset status variables
                    this.currentState = PlayState.Stopped;

                    // Start playing the media file
                    if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
                        PlayMovieInWindow(this.filepath);
                    else
                        PlayWithMediaBridge(this.filepath);
                }
                catch (Exception ex)
                {
                    textbox_name.Text = "";
                    CloseClip();
                    this.filepath = String.Empty;
                    ErrorException("MainFormLoader: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            textbox_name.Text = Languages.Translate("loading") + "...";
            textbox_time.Text = "00:00:00";

            if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
            {
                this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);

                source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                source.AddHook(new HwndSourceHook(WndProc));

                //Таймер для обновления позиции
                timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                timer.Interval = 50;

                VideoElement.Visibility = Visibility.Collapsed;
            }
            else
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
                    if (Settings.PlayerEngine != Settings.PlayerEngines.MediaBridge)
                        PlayMovieInWindow(this.filepath);
                    else
                        PlayWithMediaBridge(this.filepath);
                }
                catch (Exception ex)
                {
                    textbox_name.Text = "";
                    CloseClip();
                    this.filepath = String.Empty;
                    ErrorException("Drag&Drop: " + ex.Message, ex.StackTrace);
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
                if (this.graphBuilder != null && NaturalDuration != TimeSpan.Zero)
                {
                    progress_top.Width = (Position.TotalSeconds / NaturalDuration.TotalSeconds) * progress_back.ActualWidth;
                    TimeSpan tCode = TimeSpan.Parse(Position.ToString().Split('.')[0]);
                    textbox_time.Text = tCode.ToString();

                    Visual visual = Mouse.Captured as Visual;
                    if (visual == null || !visual.IsDescendantOf(slider_pos))
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

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseClip();
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
            try
            {
                //Останавливаем таймер обновления позиции
                if (timer != null) timer.Stop();

                if (this.graphBuilder != null && this.VideoElement.Source == null)
                {
                    int hr = 0;

                    // Stop media playback
                    if (this.mediaControl != null)
                        hr = this.mediaControl.Stop();

                    // Clear global flags
                    this.currentState = PlayState.Stopped;

                    // Free DirectShow interfaces
                    CloseInterfaces();

                    this.isAudioOnly = true; //Перенесено сюда

                    if (VHost != null)
                    {
                        VHost.Dispose();
                        VHost = null;
                        VHandle = IntPtr.Zero;
                        VHostElement.Child = null;
                    }

                    // No current media state
                    this.currentState = PlayState.Init;
                }
                else if (this.VideoElement.Source != null)
                {
                    VideoElement.Stop();
                    VideoElement.Source = null;
                    this.currentState = PlayState.Init;

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
            catch (Exception ex)
            {
                ErrorException("CloseClip: " + ex.Message, ex.StackTrace);
            }

            //update titles
            textbox_time.Text = "00:00:00";
            progress_top.Width = 0.0;
            slider_pos.Value = 0.0;
            in_ar = 0;
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

                    // Release and zero DirectShow interfaces
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
                    //bad way
                    //if (this.graphBuilder != null)
                    //Marshal.ReleaseComObject(this.graphBuilder); this.graphBuilder = null;

                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                ErrorException("CloseInterfaces: " + ex.Message, ex.StackTrace);
            }
        }

        private void PlayMovieInWindow(string filename)
        {
            if (filename == string.Empty) return;

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

                //Отключаем сохранение аспекта
                hr = EVRControl.SetAspectRatioMode(MFVideoAspectRatioMode.None);
                MFError.ThrowExceptionForHR(hr);
            }

            // Have the graph builder construct its the appropriate graph automatically
            hr = this.graphBuilder.RenderFile(filename, null);
            DsError.ThrowExceptionForHR(hr);

            if (EVRControl == null)
            {
                //Ищем рендерер и отключаем соблюдение аспекта (аспект будет определяться размерами видео-окна)
                IBaseFilter filter = null;
                graphBuilder.FindFilterByName("Video Renderer", out filter);
                if (filter != null)
                {
                    IVMRAspectRatioControl vmr = filter as IVMRAspectRatioControl;
                    if (vmr != null) DsError.ThrowExceptionForHR(vmr.SetAspectRatioMode(VMRAspectRatioMode.None));
                }
                else
                {
                    graphBuilder.FindFilterByName("Video Mixing Renderer 9", out filter);
                    if (filter != null)
                    {
                        IVMRAspectRatioControl9 vmr9 = filter as IVMRAspectRatioControl9;
                        if (vmr9 != null) DsError.ThrowExceptionForHR(vmr9.SetAspectRatioMode(VMRAspectRatioMode.None));
                    }
                }
            }

            // QueryInterface for DirectShow interfaces
            this.mediaControl = (IMediaControl)this.graphBuilder;
            this.mediaEventEx = (IMediaEventEx)this.graphBuilder;
            this.mediaSeeking = (IMediaSeeking)this.graphBuilder;
            this.mediaPosition = (IMediaPosition)this.graphBuilder;

            // Query for video interfaces, which may not be relevant for audio files
            this.videoWindow = (EVRControl == null) ? this.graphBuilder as IVideoWindow : null;
            this.basicVideo = (EVRControl == null) ? this.graphBuilder as IBasicVideo : null;

            // Query for audio interfaces, which may not be relevant for video-only files
            this.basicAudio = this.graphBuilder as IBasicAudio;
            basicAudio.put_Volume(-(int)(10000 - Math.Pow(Settings.VolumeLevel, 1.0 / 5) * 10000)); //Громкость для ДиректШоу
            
            // Is this an audio-only file (no video component)?
            CheckIsAudioOnly();
            if (!this.isAudioOnly)
            {
                //Определяем аспект, если он нам не известен
                if (in_ar == 0)
                {
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(filepath);
                    in_ar = media.Aspect;
                    media.Close();
                }

                if (videoWindow != null)
                {
                    // Setup the video window
                    hr = this.videoWindow.put_Owner(this.source.Handle);
                    DsError.ThrowExceptionForHR(hr);

                    hr = this.videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings |
                        DirectShowLib.WindowStyle.ClipChildren);
                    DsError.ThrowExceptionForHR(hr);
                }

                MoveVideoWindow();
            }
            else
            {
                if (VHost != null)
                {
                    VHost.Dispose();
                    VHost = null;
                    VHandle = IntPtr.Zero;
                    VHostElement.Child = null;
                }
            }

            // Have the graph signal event via window callbacks for performance
            hr = this.mediaEventEx.SetNotifyWindow(this.source.Handle, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

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

            //Запускаем таймер обновления позиции
            if (timer != null) timer.Start();
        }

        private void MoveVideoWindow()
        {
            try
            {
                if (this.videoWindow == null && this.EVRControl == null)
                    return;

                double left = 0, top = 0;
                int width = 0, height = 0;
                double aspect = in_ar;

                //Определяем аспект
                if (in_ar <= 0)
                {
                    if (basicVideo != null)
                    {
                        DsError.ThrowExceptionForHR(basicVideo.get_SourceWidth(out width));
                        DsError.ThrowExceptionForHR(basicVideo.get_VideoHeight(out height)); //get_SourceHeight ?
                        aspect = ((double)width / (double)height);
                    }
                    else if (EVRControl != null)
                    {
                        System.Drawing.Size size, size_ar;
                        MFError.ThrowExceptionForHR(EVRControl.GetNativeVideoSize(out size, out size_ar));
                        aspect = ((double)size_ar.Width / (double)size_ar.Height);
                    }
                }

                top = grid_top.ActualHeight + grid_player_info.ActualHeight + 4;
                height = (int)(LayoutRoot.ActualHeight - top - grid_buttons.ActualHeight - slider_pos.ActualHeight - 4);
                width = (int)(aspect * height);
                left = (LayoutRoot.ActualWidth - width) / 2;
                if (width > (int)progress_back.ActualWidth)
                {
                    width = (int)progress_back.ActualWidth;
                    height = (int)((double)width / aspect);
                    left = (LayoutRoot.ActualWidth - width) / 2;
                    top = ((LayoutRoot.ActualHeight - height) / 2.0) + 16;
                }

                if (this.videoWindow != null)
                {
                    //Масштабируем и вводим
                    DsError.ThrowExceptionForHR(this.videoWindow.SetWindowPosition(Convert.ToInt32(left * dpi), Convert.ToInt32(top * dpi),
                        Convert.ToInt32(width * dpi), Convert.ToInt32(height * dpi)));

                    //Заставляем перерисовать окно
                    DsError.ThrowExceptionForHR(this.videoWindow.put_BorderColor(1));
                }
                else if (EVRControl != null)
                {
                    //Идем на небольшую хитрость для указания позиции EVR-окна :)
                    //Её смысл в том, что элемент VHostElement располагается на макете страницы
                    //не там, где превью, а в левом верхнем углу окна - теперь можно использовать
                    //уже имеющиеся формулы для расчета требуемой позиции и размера EVR-окна.
                    VHostElement.Margin = new Thickness(Convert.ToInt32(left), Convert.ToInt32(top), 0, 0);
                    VHostElement.Width = Convert.ToInt32(width);
                    VHostElement.Height = Convert.ToInt32(height);
                    VHostElement.UpdateLayout();

                    //Т.к. MFRect принимает всё в int, то double приходится округлять - чтоб размеры окна и размеры фактической картинки
                    //совпадали, выше для VHostElement было проделано точно такое-же округление (иначе могла вылазить полоса в ~1 пиксель).
                    MFError.ThrowExceptionForHR(EVRControl.SetVideoPosition(null, new MFRect(0, 0, Convert.ToInt32(dpi * VHostElement.ActualWidth),
                        Convert.ToInt32(dpi * VHostElement.ActualHeight))));
                }
            }
            catch (Exception ex)
            {
                ErrorException("MoveVideoWindow: " + ex.Message, ex.StackTrace);
            }
        }

        private void VHost_RepaintRequired(object sender, EventArgs e)
        {
            if (!isAudioOnly && EVRControl != null)
                EVRControl.RepaintVideo();
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
                // Audio-only files have no video interfaces.  This might also
                // be a file whose video component uses an unknown video codec.
                this.isAudioOnly = true;
            }
            else
            {
                OABool lVisible;
                this.isAudioOnly = false;
                hr = this.videoWindow.get_Visible(out lVisible);
                if (hr < 0)
                {
                    // If this is an audio-only clip, get_Visible() won't work.
                    //
                    // Also, if this video is encoded with an unsupported codec,
                    // we won't see any video, although the audio will work if it is
                    // of a supported format.
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
            VideoElement.Volume = Settings.VolumeLevel; //Громкость для МедиаБридж

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

                hr = graphBuilder.RenderFile(filepath, null);
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                ErrorException("BridgeCallback: " + ex.Message, ex.StackTrace);
            }
        }

        private void VideoElement_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            if (VideoElement.HasVideo || VideoElement.HasAudio)
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
                if (visual == null || !visual.IsDescendantOf(slider_pos))
                {
                    slider_pos.Value = VideoElement.Position.TotalSeconds;
                }
            }
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
                        int hr = mediaPosition.get_Duration(out duration);
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
                catch
                {
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
                        int hr = mediaPosition.get_CurrentPosition(out position);
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
                catch
                {
                    return TimeSpan.Zero;
                }
            }
            set
            {
                try
                {
                    if (this.graphBuilder != null && this.VideoElement.Source == null)
                        mediaPosition.put_CurrentPosition(value.TotalSeconds);
                    else if (this.graphBuilder != null && this.VideoElement.Source != null)
                        VideoElement.Position = value;
                }
                catch (Exception ex)
                {
                    if (ex is AccessViolationException) throw;
                }
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

        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                MessageBox.Show(data + "\r\n\r\n" + info, Languages.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}