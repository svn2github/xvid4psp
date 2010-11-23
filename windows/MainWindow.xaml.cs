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
using MediaBridge;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media.Imaging;


namespace XviD4PSP
{
    public partial class MainWindow
    {
        public Massive m;
        public ArrayList outfiles = new ArrayList();
        public ArrayList deletefiles = new ArrayList();
        private ArrayList ffcache = new ArrayList();
        private ArrayList dgcache = new ArrayList();

        public enum MediaLoad { load = 1, update }

        private bool IsInsertAction = false;

        //player
        private Brush oldbrush;
        public TimeSpan oldpos;
        private System.Windows.WindowState oldstate;
        private const int WMGraphNotify = 0x0400 + 13;
        public MediaLoad mediaload;
        private int VolumeSet; //Громкость DirectShow плейера

        //PictureView
        private int pic_frame = 0;
        private int pic_total_frames = 0;
        private TimeSpan pic_duration = TimeSpan.Zero;

        private string total_frames = "";
        private int trim_start = 0;
        private int trim_end = 0;
        private bool trim_is_on = false;

        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaControl = null;
        private IMediaEventEx mediaEventEx = null;
        private IVideoWindow videoWindow = null;
        private IBasicAudio basicAudio = null;
        private IBasicVideo basicVideo = null;
        private IMediaSeeking mediaSeeking = null;
        private IMediaPosition mediaPosition = null;
        private IVideoFrameStep frameStep = null;

        private bool IsAudioOnly = false;
        private bool IsFullScreen = false;
        private bool IsAviSynthError = false;
        public PlayState currentState = PlayState.Init;

        private IntPtr Handle = IntPtr.Zero;

        private HwndSource source;
        private System.Timers.Timer timer;

        private IFilterGraph graph;
        private string filepath = "";
        private Thickness oldmargin;

        private BackgroundWorker worker = null;

        //Tray
        public System.Windows.Forms.NotifyIcon TrayIcon;                   //Иконка в трее
        private System.Windows.Forms.ToolStripMenuItem tmnExit;            //Пункт меню "Exit"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayClose;       //Пункт меню "Close to tray"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayMinimize;    //Пункт меню "Minimize to tray"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayClickOnce;   //Пункт меню "1-Click"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayNoBalloons;  //Пункт меню "Disable balloons"

        private string path_to_save;          //Путь для конечных файлов при перекодировании папки
        private int opened_files = 0;         //Кол-во открытых файлов при открытии папки
        private double fps = 0;               //Значение fps для текущего клипа; будет вычисляться один раз, при загрузке (обновлении) превью
        private bool OldSeeking = false;      //Способ позиционирования, old - непрерывное, new - только при отпускании кнопки мыши
        private double dpi = 1.0;             //Для масштабирования окна ДиректШоу-превью
        private bool IsBatchOpening = false;  //true при пакетном открытии
        private bool PauseAfterFirst = false; //Пакетное открытие с паузой после 1-го файла
        private string[] batch_files;         //Сохраненный список файлов для пакетного открытия с паузой
        private bool CloneIsEnabled = false;  //true, если есть открытый файл от которого можно брать параметры при пакетном открытии
        private string[] drop_data;           //Список забрасываемых файлов (drag-and-drop)
        private bool IsDragOpening = false;   //true всё время, пока идет открытие drag-and-drop
        public bool IsExiting = false;        //true, если надо выйти из программы, false - если свернуть в трей

        public enum MediaType
        {
            Audio,
            Video
        }

        public enum PlayState
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
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        public static extern bool DeleteObject(IntPtr hObject);

        public MainWindow()
        {
            //разрешаем запустить только один экземпляр
            try
            {
                Process process = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(process.ProcessName);
                foreach (Process _process in processes)
                {
                    if (_process.Id != process.Id && _process.MainModule.FileName == process.MainModule.FileName)
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            this.InitializeComponent();

            try
            {
                //Установка параметров окна из сохраненных настроек (если эта опция включена)
                if (Settings.WindowResize == true)
                {
                    string[] value = (Settings.WindowLocation + "/" + Settings.TasksRows).Split('/');
                    if (value.Length == 6)
                    {
                        this.Width = Convert.ToDouble(value[0]);
                        this.Height = Convert.ToDouble(value[1]);
                        this.Left = Convert.ToDouble(value[2]);
                        this.Top = Convert.ToDouble(value[3]);
                        GridLengthConverter conv = new GridLengthConverter();
                        string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                        this.TasksRow.Height = (GridLength)conv.ConvertFromString(value[4].Replace(".", sep).Replace(",", sep));
                        this.TasksRow2.Height = (GridLength)conv.ConvertFromString(value[5].Replace(".", sep).Replace(",", sep));
                    }
                }

                //Трей
                System.Windows.Forms.ContextMenuStrip TrayMenu = new System.Windows.Forms.ContextMenuStrip();
                TrayIcon = new System.Windows.Forms.NotifyIcon();
                //Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/pictures/Top.ico")).Stream;
                Stream iconStream = Application.GetResourceStream(new Uri("main.ico", UriKind.RelativeOrAbsolute)).Stream;
                TrayIcon.Icon = new System.Drawing.Icon(iconStream);

                //Пункт меню "Close to tray"
                tmnTrayClose = new System.Windows.Forms.ToolStripMenuItem();
                tmnTrayClose.Text = "Close to tray";
                tmnTrayClose.CheckOnClick = true;
                tmnTrayClose.Checked = Settings.TrayClose;
                tmnTrayClose.Click += new EventHandler(tmnTrayClose_Click);
                TrayMenu.Items.Add(tmnTrayClose);

                //Пункт меню "Minimize to tray"
                tmnTrayMinimize = new System.Windows.Forms.ToolStripMenuItem();
                tmnTrayMinimize.Text = "Minimize to tray";
                tmnTrayMinimize.CheckOnClick = true;
                tmnTrayMinimize.Checked = Settings.TrayMinimize;
                tmnTrayMinimize.Click += new EventHandler(tmnTrayMinimize_Click);
                TrayMenu.Items.Add(tmnTrayMinimize);

                //Пункт меню "1-Click"
                tmnTrayClickOnce = new System.Windows.Forms.ToolStripMenuItem();
                tmnTrayClickOnce.Text = "Single click to open";
                tmnTrayClickOnce.CheckOnClick = true;
                tmnTrayClickOnce.Checked = Settings.TrayClickOnce;
                tmnTrayClickOnce.Click += new EventHandler(tmnTrayClickOnce_Click);
                TrayMenu.Items.Add(tmnTrayClickOnce);

                //Пункт меню "Disable balloons"
                tmnTrayNoBalloons = new System.Windows.Forms.ToolStripMenuItem();
                tmnTrayNoBalloons.Text = "Disable balloons";
                tmnTrayNoBalloons.CheckOnClick = true;
                tmnTrayNoBalloons.Checked = Settings.TrayNoBalloons;
                tmnTrayNoBalloons.Click += new EventHandler(tmnTrayNoBalloons_Click);
                TrayMenu.Items.Add(tmnTrayNoBalloons);
                TrayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

                //Пункт меню "Exit"
                tmnExit = new System.Windows.Forms.ToolStripMenuItem();
                tmnExit.Text = "Exit";
                tmnExit.Click += new EventHandler(mnExit_Click);
                TrayMenu.Items.Add(tmnExit);

                TrayIcon.ContextMenuStrip = TrayMenu;
                TrayIcon.Text = "XviD4PSP";

                UpdateRecentFiles();  //Список недавних файлов
                MenuHider(false);     //Делаем пункты меню неактивными
                SetHotKeys();         //Загружаем HotKeys
                SetLanguage();        //переводим лейблы

                textbox_name.Text = textbox_frame.Text = textbox_frame_goto.Text = "";
                textbox_time.Text = textbox_duration.Text = "00:00:00";

                //Определяем коэффициент для масштабирования окна ДиректШоу-превью
                IntPtr ScreenDC = GetDC(IntPtr.Zero); //88-w, 90-h
                double _dpi = (double)GetDeviceCaps(ScreenDC, 88) / 96.0;
                if (_dpi != 0) dpi = _dpi;
                ReleaseDC(IntPtr.Zero, ScreenDC);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }

            //events
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.PreviewKeyDown += new KeyEventHandler(MainWindow_KeyDown);
            this.StateChanged += new EventHandler(MainWindow_StateChanged);
            this.textbox_name.MouseEnter += new MouseEventHandler(textbox_name_MouseEnter); //Мышь вошла в зону с названием файла
            this.textbox_name.MouseLeave += new MouseEventHandler(textbox_name_MouseLeave); //Мышь вышла из зоны с названием файла
            if (Settings.TrayClickOnce) TrayIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
            else TrayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainFormLoader();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TrayIcon.Visible = Settings.TrayIconEnabled;
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
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
                    //загружаем список форматов
                    combo_format.Items.Clear();
                    foreach (string f in Format.GetFormatList()) combo_format.Items.Add(f);
                    combo_format.SelectedItem = Format.EnumToString(Settings.FormatOut);

                    //загружаем список фильтров
                    combo_filtering.Items.Clear();
                    combo_filtering.Items.Add("Disabled");
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\filtering"))
                    {
                        combo_filtering.Items.Add(Path.GetFileNameWithoutExtension(file));
                    }
                    //прописываем текущий фильтр
                    if (combo_filtering.Items.Contains(Settings.Filtering))
                        combo_filtering.SelectedItem = Settings.Filtering;
                    else
                        combo_filtering.SelectedItem = Settings.Filtering = "Disabled";

                    //загружаем списки профилей цвето коррекции
                    combo_sbc.Items.Clear();
                    combo_sbc.Items.Add("Disabled");
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\sbc"))
                    {
                        combo_sbc.Items.Add(Path.GetFileNameWithoutExtension(file));
                    }
                    //прописываем текущий профиль
                    if (combo_sbc.Items.Contains(Settings.SBC))
                        combo_sbc.SelectedItem = Settings.SBC;
                    else
                        combo_sbc.SelectedItem = Settings.SBC = "Disabled";

                    //загружаем профили видео кодирования
                    if (Settings.FormatOut != Format.ExportFormats.Audio)
                    {
                        LoadVideoPresets();
                        SetVideoPresetFromSettings();
                    }
                    else
                    {
                        combo_vencoding.Items.Add("Disabled");
                        combo_vencoding.SelectedItem = "Disabled";
                        combo_vencoding.IsEnabled = false;
                        button_edit_vencoding.IsEnabled = false;
                        combo_sbc.IsEnabled = false;
                        button_edit_sbc.IsEnabled = false;
                    }

                    //загружаем профили аудио кодирования
                    LoadAudioPresets();
                    SetAudioPresetFromSettings();

                    //загружаем настройки
                    LoadSettings();
                   
                    //ищем оптимальную темп папку
                    if (Settings.SearchTempPath)
                    {
                        string drivestring = "C:\\";
                        string dlabel = "";
                        long maxspace = long.MinValue;
                        foreach (DriveInfo drive in DriveInfo.GetDrives())
                            if (drive.DriveType == DriveType.Fixed && drive.IsReady && drive.AvailableFreeSpace > maxspace)
                            {
                                maxspace = drive.AvailableFreeSpace;
                                drivestring = drive.Name;
                                dlabel = drive.VolumeLabel;
                            }
                        if (drivestring != Settings.TempPath.Substring(0, 3) || Settings.Key == "0000")
                        {
                            Message mess = new Message(this);
                            mess.ShowMessage(Languages.Translate("Maximum free drive space detected on") + " " + drivestring.Substring(0, 2) + " (" + dlabel + ").\r\n" +
                                Languages.Translate("Do you want use this drive for temp files?"), Languages.Translate("Place for temp files"), Message.MessageStyle.YesNo);
                            if (mess.result == Message.Result.Yes)
                            {
                                Settings.TempPath = drivestring + "Temp";
                                TempFolderFiles(); //Проверка папки на наличие в ней файлов
                            }
                            else if (Settings.Key == "0000") //Чтоб не доставать каждый раз окном выбора Темп-папки, а только при первом запуске
                            {
                                new Settings_Window(this, 2);
                            }
                        }
                    }
                    if (!Directory.Exists(Settings.TempPath)) Directory.CreateDirectory(Settings.TempPath);
                                                         
                    //Запускаем таймер, по которому потом будем обновлять позицию слайдера, счетчик времени, и еще одну хреновину..
                    timer = new System.Timers.Timer();
                    timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                    timer.Interval = 30;
                    timer.Enabled = true;
                    timer.Start();

                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                    {
                        this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                        this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
                        this.grid_tasks.SizeChanged += new SizeChangedEventHandler(grid_tasks_SizeChanged);

                        source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                        source.AddHook(new HwndSourceHook(WndProc));
                    }
                    else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                    {
                        //set media element state for video loading
                        VideoElement.LoadedBehavior = MediaState.Manual;
                        //VideoElement.UnloadedBehavior = MediaState.Stop;
                        VideoElement.ScrubbingEnabled = true;

                        //events
                        VideoElement.MediaOpened += new RoutedEventHandler(VideoElement_MediaOpened);
                        VideoElement.MediaEnded += new RoutedEventHandler(VideoElement_MediaEnded);

                        VideoElement.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        button_play.Visibility = Visibility.Collapsed;
                        button_frame_back.Margin = new Thickness(4, 6.1, 0, 6.1);
                        button_frame_forward.Margin = new Thickness(31, 6.1, 0, 6.1);
                        slider_pos.Margin = new Thickness(60, 7.7, 4, 7.7);
                    }

                    //Открытие файла из командной строки (command line arguments)
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length > 1)
                    {
                        if (File.Exists(args[1]))
                        {
                            //создаём массив и забиваем в него данные
                            Massive x = new Massive();
                            x.infilepath = args[1];
                            x.infileslist = new string[] { args[1] };
                            x.owner = this;

                            //ищем соседние файлы и спрашиваем добавить ли их к заданию при нахождении таковых
                            if (Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly && Calculate.IsValidVOBName(x.infilepath) ||
                                Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled)
                                x = OpenDialogs.GetFriendFilesList(x);
                            if (x != null) action_open(x);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorException(ex.Message);
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.OwnedWindows.Count > 0 || textbox_frame_goto.Visibility != Visibility.Hidden || textbox_start.IsFocused || textbox_end.IsFocused || script_box.IsFocused) return;
            string key = new System.Windows.Input.KeyConverter().ConvertToString(e.Key);
            string mod = new System.Windows.Input.ModifierKeysConverter().ConvertToString(System.Windows.Input.Keyboard.Modifiers);
            string PressedKeys = "=" + ((mod.Length > 0) ? mod + "+" : "") + key;
            //textbox_frame.Text = PressedKeys;
            
            string Action = HotKeys.GetAction(PressedKeys);
            e.Handled = (Action.Length > 0);
            //textbox_frame.Text = Action;

            switch (Action)
            {
                //File
                case ("Open file(s)"): OpenFile_Click(null, null); break;
                case ("Open folder"): menu_open_folder_Click(null, null); break;
                case ("Open DVD folder"): OpenDVD_Click(null, null); break;
                case ("Decode file"): menu_decode_file_Click(null, null); break;
                case ("Join file"): menu_join_Click(null, null); break;
                case ("Close file"): button_close_Click(null, null); break;
                case ("Save task"): button_save_Click(null, null); break;
                case ("Save frame"): menu_save_frame_Click(null, null); break;
                case ("Save THM frame"): menu_savethm_Click(null, null); break;
                //Video
                case ("Refresh preview"): mnUpdateVideo_Click(null, null); break;
                case ("VDemux"): menu_demux_video_Click(null, null); break;
                case ("Detect black borders"): menu_autocrop_Click(null, null); break;
                case ("Detect interlace"): menu_detect_interlace_Click(null, null); break;
                case ("Color correction"): ColorCorrection_Click(null, null); break;
                case ("Resolution/Aspect"): AspectResolution_Click(null, null); break;
                case ("Interlace/Framerate"): menu_interlace_Click(null, null); break;
                case ("VEncoding settings"): VideoEncodingSettings_Click(null, null); break;
                //Audio
                case ("ADemux"): menu_demux_Click(null, null); break;
                case ("Save to WAV"): menu_save_wav_Click(null, null); break;
                case ("Editing options"): AudioOptions_Click(null, null); break;
                case ("AEncoding settings"): AudioEncodingSettings_Click(null, null); break;
                //Subtitles
                case ("Add subtitles"): mnAddSubtitles_Click(null, null); break;
                case ("Remove subtitles"): mnRemoveSubtitles_Click(null, null); break;
                //AviSynth
                case ("AvsP editor"): button_avsp_Click(null, null); break;
                case ("Edit filtering script"): EditScript(null, null); break;
                case ("Test script"): if (m != null) { ApplyTestScript(null, null); menu_createtestscript.IsChecked = m.testscript; }; break;
                case ("Save script"): SaveScript(null, null); break;
                case ("Windows Media Player"): menu_play_in_Click(menu_playinwmp, null); break;
                case ("Media Player Classic"): menu_play_in_Click(menu_playinmpc, null); break;
                case ("WPF Video Player"): menu_play_in_Click(menu_playinwpf, null); break;
                //Tools
                case ("Media Info"): menu_info_media_Click(menu_info_media, null); break;
                case ("FFRebuilder"): menu_ffrebuilder_Click(null, null); break;
                case ("MKVRebuilder"): menu_mkvrebuilder_Click(null, null); break;
                case ("DGIndex"): mn_apps_Click(mnDGIndex, null); break;
                case ("DGPulldown"): mn_apps_Click(menu_dgpulldown, null); break;
                case ("DGAVCIndex"): mn_apps_Click(mnDGAVCIndex, null); break;
                case ("VirtualDubMod"): mn_apps_Click(menu_virtualdubmod, null); break;
                case ("AVI-Mux"): mn_apps_Click(menu_avimux, null); break;
                case ("tsMuxeR"): mn_apps_Click(menu_tsmuxer, null); break;
                case ("MKVExtract"): mn_apps_Click(menu_mkvextract, null); break;
                case ("MKVMerge"): mn_apps_Click(menu_mkvmerge, null); break;
                case ("Yamb"): mn_apps_Click(menu_yamb, null); break;
                //Other
                case ("Frame forward"): Frame_Shift(1); break;
                case ("Frame back"): Frame_Shift(-1); break;
                case ("10 frames forward"): Frame_Shift(10); break;
                case ("10 frames backward"): Frame_Shift(-10); ; break;
                case ("Play-Pause"): PauseClip(); break;
                case ("Fullscreen"): SwitchToFullScreen(); break;
                case ("Volume+"): VolumePlus(); break;
                case ("Volume-"): VolumeMinus(); break;
                case ("Set Start"): button_set_start_Click(null, null); break;
                case ("Set End"): button_set_end_Click(null, null); break;
                case ("Apply Trim"): button_apply_trim_Click(null, null); break;
                case ("Add/Remove bookmark"): AddToBookmarks_Click(null, null); break;
            }
        }

        private void grid_tasks_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(!IsFullScreen) MoveVideoWindow();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.graphBuilder != null || Pic.Visibility == Visibility.Visible)
                UpdateClock(); 
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!IsFullScreen) MoveVideoWindow();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsFullScreen) MoveVideoWindow();
        }

        //Сворачиваемся в трей; активируем окна при разворачивании
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                if (Settings.TrayIconEnabled && Settings.TrayMinimize) this.Hide();
            }
            else
            {
                //Hidden - окна, спрятанные "вручную" (через код) - сами они не разворачиваются..
                foreach (Window wnd in this.OwnedWindows)
                    if (wnd.Name == "Hidden")
                    {
                        if (!wnd.IsVisible) wnd.Show();
                        if (wnd.WindowState == System.Windows.WindowState.Minimized)
                            wnd.WindowState = System.Windows.WindowState.Normal;
                        wnd.Name = "Window";
                    }
            }
        }

        //Разворачиваемся из трея
        private void TrayIcon_Click(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.Show();
                if (this.WindowState == System.Windows.WindowState.Minimized)
                    this.WindowState = System.Windows.WindowState.Normal;
                this.Activate();
            }
        }

        private void tmnTrayClose_Click(object sender, EventArgs e)
        {
            Settings.TrayClose = tmnTrayClose.Checked;
        }

        private void tmnTrayMinimize_Click(object sender, EventArgs e)
        {
            Settings.TrayMinimize = tmnTrayMinimize.Checked;
        }

        private void tmnTrayClickOnce_Click(object sender, EventArgs e)
        {
            if (Settings.TrayClickOnce = tmnTrayClickOnce.Checked)
            {
                TrayIcon.MouseDoubleClick -= new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
                TrayIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
            }
            else
            {
                TrayIcon.MouseClick -= new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
                TrayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
            }
        }

        private void tmnTrayNoBalloons_Click(object sender, EventArgs e)
        {
            Settings.TrayNoBalloons = tmnTrayNoBalloons.Checked;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Сворачиваемся в трей
            if (!IsExiting && Settings.TrayIconEnabled && Settings.TrayClose)
            {
                e.Cancel = true;
                this.StateChanged -= new EventHandler(MainWindow_StateChanged);
                this.WindowState = System.Windows.WindowState.Minimized;
                this.Hide();
                this.StateChanged += new EventHandler(MainWindow_StateChanged);
                if (!Settings.TrayNoBalloons) TrayIcon.ShowBalloonTip(5000, "XviD4PSP", " ", System.Windows.Forms.ToolTipIcon.Info);
                return;
            }

            //проверяем есть ли задания в работе
            foreach (object _task in list_tasks.Items)
            {
                Task task = (Task)_task;
                if (task.Status == "Encoding")
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("Some jobs are still in progress!") + "\r\n" + Languages.Translate("Are you sure you want to quit?"), Languages.Translate("Warning"), Message.MessageStyle.YesNo);
                    if (mes.result == Message.Result.No)
                    {
                        IsExiting = false;
                        e.Cancel = true;
                        return;
                    }

                    while (this.OwnedWindows.Count > 0)
                    {
                        foreach (Window OwnedWindow in this.OwnedWindows)
                        {
                            OwnedWindow.Close();
                        }
                    }
                    break;
                }
            }

            if (m != null) CloseFile();

            //Временные файлы
            if (Settings.DeleteTempFiles)
            {
                //удаляем мусор
                foreach (string dfile in deletefiles)
                {
                    if (!dfile.Contains("cache")) SafeDelete(dfile);
                }

                //подчищаем мусор за FFmpegSource
                if (Settings.DeleteFFCache)
                {
                    foreach (string dfile in ffcache) SafeDelete(dfile);

                    //Кэш от FFmpegSource2
                    foreach (string f in Directory.GetFiles(Settings.TempPath, "*.ffindex")) SafeDelete(f);
                }

                //Удаление DGIndex-кеша
                if (Settings.DeleteDGIndexCache)
                {
                    foreach (string cache_path in dgcache)
                    {
                        SafeDirDelete(Path.GetDirectoryName(cache_path));
                    }
                }
            }

            //Определяем и сохраняем размер и положение окна при выходе
            if (this.WindowState != System.Windows.WindowState.Maximized && this.WindowState != System.Windows.WindowState.Minimized) //но только если окно не развернуто на весь экран и не свернуто
            {
                Settings.WindowLocation = (int)this.Window.ActualWidth + "/" + (int)this.Window.ActualHeight + "/" + (int)this.Window.Left + "/" + (int)this.Window.Top;
                GridLengthConverter conv = new GridLengthConverter();
                Settings.TasksRows = conv.ConvertToString(this.TasksRow.Height) + "/" + conv.ConvertToString(this.TasksRow2.Height);
            }

            //Чистим трей
            TrayIcon.Visible = false;
            TrayIcon.Dispose();
        }

        private ArrayList add_ff_cache(string[] infileslist)
        {
            ArrayList cachefiles = new ArrayList();
            foreach (string dfile in infileslist)
            {
                for (int n = 0; n < 10; n++)
                {
                    string test = dfile + ".ffa" + n + "cache";
                    cachefiles.Add(test);
                    test = dfile + ".ffv" + n + "cache";
                    cachefiles.Add(test);
                }
            }
            return cachefiles;
        }

        private void clear_ff_cache()
        {
            try
            {
                if (Settings.DeleteFFCache && Settings.DeleteTempFiles)
                {
                    ArrayList filesinwork = new ArrayList();

                    //все файлы в текущем массиве
                    if (m != null)
                        foreach (string _file in m.infileslist)
                            filesinwork.Add(_file);

                    //все файлы в заданиях
                    if (list_tasks.Items.Count != 0)
                    {
                        foreach (object _task in list_tasks.Items)
                        {
                            Task task = (Task)_task;
                            foreach (string _file in task.Mass.infileslist)
                                filesinwork.Add(_file);
                        }
                    }

                    ArrayList cache_for_delete = new ArrayList();
                    //сопоставление
                    foreach (string _file in ffcache)
                    {
                        string cachefile = Calculate.RemoveExtention(_file, true);
                        //если файл больше не используется удаляем его кеш
                        if (!filesinwork.Contains(cachefile))
                            cache_for_delete.Add(_file);
                    }

                    //удаляем свободные кеш файлы
                    foreach (string _file in cache_for_delete)
                    {
                        ffcache.Remove(_file);
                        SafeDelete(_file);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void clear_dgindex_cache()
        {
            try
            {
                if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && Settings.DeleteDGIndexCache)
                {
                    //Если есть задания, проверяем, занят ли там наш кэш-файл, если не занят - то удаляем его
                    if (list_tasks.Items.Count != 0)
                    {
                        foreach (object _task in list_tasks.Items)
                        {
                            if (((Task)_task).Mass.indexfile == m.indexfile) return;
                        }
                    }
                    SafeDirDelete(Path.GetDirectoryName(m.indexfile));
                    dgcache.Remove(m.indexfile);
                }
            }
            catch (Exception) { }
        }

        private void OpenFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //закрываем все дочерние окна
            CloseChildWindows();
           
            //открываем файл
            OpenDialogs.owner = this;
            Massive x = OpenDialogs.OpenFile();
            if (x != null)
            {
                if (x.infileslist.Length > 1 && x.infilepath == null) //Мульти-открытие
                {
                    PauseAfterFirst = Settings.BatchPause;
                    if (!PauseAfterFirst)
                    {
                        path_to_save = OpenDialogs.SaveFolder();
                        if (path_to_save == null) return;
                    }
                    MultiOpen(x.infileslist);
                    return;
                }
                action_open(x); //Обычное открытие
            }
        }

        private void menu_decode_file_Click(object sender, RoutedEventArgs e)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            //открываем файл
            OpenDialogs.owner = this;
            Massive x = OpenDialogs.OpenFile();
            if (x == null) return;

            //присваиваем заданию уникальный ключ
            if (Settings.Key == "9999") Settings.Key = "0000";
            x.key = Settings.Key;

            string vpath = Settings.TempPath + "\\" + x.key + ".video.avi";
            Decoder vdec = new Decoder(x, Decoder.DecoderModes.DecodeVideo, vpath);
            if (vdec.IsErrors)
            {
                new Message(this).ShowMessage("Decoding video: " + vdec.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                return;
            }
            
            //проверка на удачное завершение
            if (File.Exists(vpath) && new FileInfo(vpath).Length != 0)
            {
                x.taskname = Path.GetFileNameWithoutExtension(x.infilepath);
                x.infilepath_source = x.infilepath;
                x.infilepath = vpath;
                x.infileslist = new string[] { vpath };
                x.vdecoder = AviSynthScripting.Decoders.FFmpegSource;
                deletefiles.Add(vpath);

                if (Settings.EnableAudio)
                {
                    string apath = Settings.TempPath + "\\" + x.key + ".audio.wav";
                    Decoder adec = new Decoder(x, Decoder.DecoderModes.DecodeAudio, apath);
                    if (adec.IsErrors)
                    {
                        new Message(this).ShowMessage("Decoding audio: " + adec.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                        SafeDelete(vpath);
                        return;
                    }

                    //проверка на удачное завершение
                    if (File.Exists(apath) && new FileInfo(apath).Length != 0)
                    {
                        MediaInfoWrapper med = new MediaInfoWrapper();
                        AudioStream s = med.GetAudioInfoFromAFile(apath);
                        s.decoder = AviSynthScripting.Decoders.WAVSource;
                        x.inaudiostreams.Add(s.Clone());
                        deletefiles.Add(apath);

                        action_open(x);
                    }
                }
                else
                    action_open(x);
            }
        }

        private void button_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null) CloseFile();
        }

        private void button_save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null) action_save(m.Clone());
        }

        private void mnExit_Click(object sender, EventArgs e)
        {
            IsExiting = true;
            Close();
        }

        private void mn_apps_Click(object sender, RoutedEventArgs e)
        {
            string path = Calculate.StartupPath;
            if (sender == mnDGIndex) path += "\\apps\\DGMPGDec\\DGIndex.exe";
            else if (sender == menu_dgpulldown) path += "\\apps\\DGPulldown\\DGPulldown.exe";
            else if (sender == mnDGAVCIndex) path += "\\apps\\DGAVCDec\\DGAVCIndex.exe";
            else if (sender == menu_virtualdubmod) path += "\\apps\\VirtualDubMod\\VirtualDubMod.exe";
            else if (sender == menu_avimux) path += "\\apps\\AVI-Mux\\AVIMux_GUI.exe";
            else if (sender == menu_tsmuxer) path += "\\apps\\tsMuxeR\\tsMuxerGUI.exe";
            else if (sender == menu_mkvextract) path += "\\apps\\MKVtoolnix\\MKVExtractGUI2.exe";
            else if (sender == menu_mkvmerge) path += "\\apps\\MKVtoolnix\\mmg.exe";
            else if (sender == menu_yamb) path += "\\apps\\MP4Box\\Yamb.exe";
            else if (sender == menu_directx_update) path += "\\apps\\DirectX_Update\\dxwebsetup.exe";
            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void menu_help_Click(object sender, RoutedEventArgs e)
        {
            string path = "http://forum.winnydows.com";
            if (sender == menu_home) path = "http://www.winnydows.com";
            else if (sender == menu_Google_code) path = "http://code.google.com/p/xvid4psp/";
            else if (sender == menu_my_mail) path = "mailto:forclip@gmail.com";
            else if (sender == menu_avisynth_guide_en) path = "http://avisynth.org/mediawiki/Internal_filters";
            else if (sender == menu_avisynth_guide_ru) path = "http://avisynth.org.ru/docs/russian/index.htm";
            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void mnAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            About a = new About(this);
        }

        private void mnResetSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Do you realy want to reset all settings") + "?" + Environment.NewLine +
                Languages.Translate("After that the program will be automatically restarted") + "!", Languages.Translate("Warning") + "!", Message.MessageStyle.OkCancel);
                if (mess.result == Message.Result.Ok)
                {
                    string lang = Settings.Language;
                    Settings.ResetAllSettings(this);
                    Settings.Language = lang;

                    //Перезапуск 
                    Process.Start(Calculate.StartupPath + "\\apps\\Launcher.exe", " 30 \"" + Calculate.StartupPath + "\""); //"30" - время ожидания завершения первой копии XviD4PSP, после чего лаунчер просто завершит свою работу
                    Close();
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
                Close();
            }
        }

        private void menu_join_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                action_add();
            else
            {
                //открываем файл
                OpenDialogs.owner = this;
                Massive x = OpenDialogs.OpenFile();
                action_open(x);
            }
        }

        private void action_add()
        {
            //запоминаем старое положени трекбара
            string[] oldfiles = m.infileslist;
            Massive x = m.Clone();

            try
            {
                bool needupdate = false;
                FilesListWindow f = new FilesListWindow(x);
                if (f.m != null)
                    x = f.m.Clone();
                if (x != null)
                {
                    //если что-то изменилось
                    if (oldfiles.Length != x.infileslist.Length)
                        needupdate = true;
                    else
                    {
                        int n = 0;
                        foreach (string file in oldfiles)
                        {
                            if (file != x.infileslist[n])
                            {
                                needupdate = true;
                            }
                            n++;
                        }
                    }

                    if (needupdate == true)
                    {
                        string ext = Path.GetExtension(x.infilepath).ToLower();
                        if (x.inaudiostreams.Count > 0)
                        {
                            AudioStream s = (AudioStream)x.inaudiostreams[x.inaudiostream];
                            ArrayList afiles = new ArrayList();
                            afiles.Add(s.audiopath);
                            if (ext == ".d2v")
                            {
                                string newfile = x.infileslist[x.infileslist.Length - 1];
                                ArrayList afileslist = Indexing.GetTracks(newfile);
                                afiles.Add(afileslist[x.inaudiostream]);
                            }
                            s.audiofiles = Calculate.ConvertArrayListToStringArray(afiles);
                        }

                        //создаём новый AviSynth скрипт
                        x = AviSynthScripting.CreateAutoAviSynthScript(x);

                        if (ext != ".d2v" && ext != ".dga" && ext != ".dgi")
                        {
                            //подсчитываем размер
                            long sizeb = 0;
                            //long msec = 0;
                            //long frames = 0;
                            foreach (string file in x.infileslist)
                            {
                                sizeb += new FileInfo(file).Length;
                                //MediaInfoWrapper med = new MediaInfoWrapper();
                                //med.Open(file);
                                //frames += med.Frames;
                                //msec += med.Milliseconds;
                                //med.Close();
                            }
                            x.infilesize = Calculate.ConvertDoubleToPointString((double)sizeb / 1049511, 1) + " mb";
                        }

                        //получаем длительность и фреймы
                        Caching cach = new Caching(x);
                        if (cach.m == null) return;
                        x = cach.m.Clone();


                        //x.inframes = (int)frames;
                        //x.induration = TimeSpan.FromMilliseconds(msec);
                        //x.outduration = x.induration;

                        x = Calculate.UpdateOutFrames(x);

                        //загружаем обновлённый скрипт
                        m = x.Clone();
                        LoadVideo(MediaLoad.load);
                    }
                }
                else
                    CloseFile();
            }
            catch (Exception ex)
            {
                x = null;
                ErrorException(ex.Message);
            }
        }

        //начало открытия файла
        private void action_open(Massive x)
        {
            try
            {
                if (x != null)
                {
                    //сразу-же обнуляем трим и его кнопки
                    ResetTrim();

                    //Пока-что вот так..
                    if (!IsBatchOpening && m != null /*&& list_tasks.Items.Count == 0*/) CloseFile();

                    string ext = Path.GetExtension(x.infilepath).ToLower();

                    //добавляем список на очистку
                    ffcache.AddRange(add_ff_cache(x.infileslist));

                    //присваиваем заданию уникальный ключ
                    if (Settings.Key == "9999")
                        Settings.Key = "0000";
                    x.key = Settings.Key;

                    //имя
                    if (x.taskname == null)
                        x.taskname = Path.GetFileNameWithoutExtension(x.infilepath);

                    //задаём хозяина
                    x.owner = this;

                    //забиваем основные параметры кодирования
                    x.format = Settings.FormatOut;
                    x.sbc = Settings.SBC;
                    x = ColorCorrection.DecodeProfile(x);
                    x.filtering = Settings.Filtering;
                    x.resizefilter = Settings.ResizeFilter;

                    //Звук для d2v, dga и dgi файлов
                    if (ext == ".d2v" || ext == ".dga" || ext == ".dgi")
                    {
                        x.indexfile = x.infilepath;
                        ArrayList atracks = Indexing.GetTracks(x.indexfile);
                        int n = 0;
                        if (atracks.Count > 0 && Settings.EnableAudio)
                        {
                            foreach (string apath in atracks)
                            {
                                //забиваем в список все найденные треки
                                MediaInfoWrapper med = new MediaInfoWrapper();
                                AudioStream stream = med.GetAudioInfoFromAFile(apath);
                                stream.delay = Calculate.GetDelay(apath);
                                x.inaudiostreams.Add(stream.Clone());
                                n++;
                            }
                            x.inaudiostream = 0;
                        }
                    }

                    //блок для файлов с обязательной разборкой
                    if (ext == ".dpg")
                    {
                        x.invcodecshort = "MPEG1";
                        string outext = Format.GetValidRAWVideoEXT(x);
                        string vpath = Settings.TempPath + "\\" + x.key + "." + outext;
                        string apath = Settings.TempPath + "\\" + x.key + "_0.mp2";

                        //удаляем старый файл
                        SafeDelete(vpath);
                        SafeDelete(apath);

                        //извлекаем новый файл
                        Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractVideo, vpath);
                        if (!dem.IsErrors && Settings.EnableAudio) dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, apath);

                        //проверка на удачное завершение
                        if (File.Exists(vpath) && new FileInfo(vpath).Length != 0)
                        {
                            x.infilepath_source = x.infilepath;
                            x.infilepath = vpath;
                            x.infileslist = new string[] { x.infilepath };
                            deletefiles.Add(vpath);
                        }

                        //проверка на удачное завершение
                        if (File.Exists(apath) && new FileInfo(apath).Length != 0)
                        {
                            AudioStream stream = new AudioStream();
                            stream.audiopath = apath;
                            stream.audiofiles = new string[] { apath };
                            stream = Format.GetValidADecoder(stream);
                            x.inaudiostreams.Add(stream);
                            x.inaudiostream = 0;
                            deletefiles.Add(apath);
                        }
                    }

                    //если файл MPEG делаем запрос на индексацию
                    if (Calculate.IsMPEG(x.infilepath) && ext != ".d2v")
                    {
                        if (Calculate.IsValidVOBName(x.infilepath))
                        {
                            x.dvdname = Calculate.GetDVDName(x.infilepath);
                            string title = Calculate.GetTitleNum(x.infilepath);
                            if (!string.IsNullOrEmpty(title)) title = "_T" + title;
                            x.taskname = x.dvdname + title;
                        }

                        if (Settings.MPEGDecoder == AviSynthScripting.Decoders.MPEG2Source)
                        {
                            //проверяем индекс папку (проверка содержимого файла - если там МПЕГ, то нужна индексация; тут-же идет запуск МедиаИнфо)
                            IndexChecker ich = new IndexChecker(x);
                            if (ich.m == null) return;
                            x = ich.m.Clone();

                            //индексация
                            if (x.indexfile != null && !File.Exists(x.indexfile))
                            {
                                Indexing index = new Indexing(x);
                                if (index.m == null) return;
                                x = index.m.Clone();
                                dgcache.Add(x.indexfile); //Добавление кэш-файла в сиписок на удаление
                            }
                        }
                    }

                    //получаем информацию через MediaInfo
                    if (ext != ".vdr")
                    {
                        Informer info = new Informer(x);
                        if (info.m == null) return;
                        x = info.m.Clone();
                    }

                    //определяем видео декодер
                    if (x.vdecoder == 0) x = Format.GetValidVDecoder(x);

                    //проверка на невозможность создать кеш файл для ffmpegsource
                    if (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource && Calculate.IsReadOnly(x.infilepath) && !Settings.FFmpegSource2)
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("Input file on CD or DVD! FFmpegSource decodes files only from HardDrive.") + Environment.NewLine +
                            Languages.Translate("Copy files to HardDrive or use DirectShowSource decoder.") + Environment.NewLine +
                            Languages.Translate("Switch decoder to DirectShowSource and try once again?"), Languages.Translate("Error"), Message.MessageStyle.YesNo);
                        if (mess.result == Message.Result.Yes)
                        {
                            x.vdecoder = AviSynthScripting.Decoders.DirectShowSource;
                        }
                        else return;
                    }
 
                    //принудительный фикс цвета для DVD
                    if (Settings.AutoColorMatrix && x.format != Format.ExportFormats.Audio)
                    {
                        if (x.iscolormatrix == false &&
                            x.invcodecshort == "MPEG2")
                        {
                            x.iscolormatrix = true;
                            if (combo_sbc.Items.Contains("MPEG2Fix") &&
                                Settings.SBC == "Disabled")
                                combo_sbc.SelectedItem = "MPEG2Fix";
                        }
                    }

                    //похоже что к нам идёт звковой файл
                    if (x.format != Format.ExportFormats.Audio && !x.isvideo)
                    {
                        x.format = Format.ExportFormats.Audio;
                        Settings.FormatOut = Format.ExportFormats.Audio;
                        combo_format.SelectedItem = Format.EnumToString(Format.ExportFormats.Audio);
                        LoadAudioPresets();
                        SetAudioPresetFromSettings();
                    }

                    //пытаемся точно узнать фреймрейт (если до этого не вышло)
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        if (x.inframerate == "" && ext != ".y4m" && ext != ".yuv")
                        {
                            FramerateDetector frd = new FramerateDetector(x);
                            if (frd.m != null)
                                x = frd.m.Clone();
                        }
                    }

                    if (x == null) return;

                    //Извлечение видео для FFmpegSource
                    if (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource)
                    {
                        //проверяем надо ли извлекать видео
                        FFMpegSourceHelper fhelp = new FFMpegSourceHelper(x);
                        if (fhelp.IsErrors)
                        {
                            if (!Settings.FFmpegSource2)
                            {
                                string outext = Format.GetValidRAWVideoEXT(x);
                                string outpath = Settings.TempPath + "\\" + x.key + "." + outext;

                                //удаляем старый файл
                                SafeDelete(outpath);

                                //извлекаем новый файл
                                Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractVideo, outpath);

                                //проверка на удачное завершение
                                if (!dem.IsErrors && File.Exists(outpath) && new FileInfo(outpath).Length != 0)
                                {
                                    x.taskname = Path.GetFileNameWithoutExtension(x.infilepath);
                                    x.infilepath_source = x.infilepath;
                                    x.infilepath = outpath;
                                    x.infileslist = new string[] { x.infilepath };
                                    deletefiles.Add(outpath);
                                }
                            }
                            else
                            {
                                //Для FFmpegSource2 просто выводим текст ошибки
                                throw new Exception(fhelp.error_message);
                            }
                        }
                    }

                    //Извлечение звука (1-й трек) для FFmpegSource и DirectShowSource, для DSS2 звук будет извлечен в Caching
                    if (x.inaudiostreams.Count > 0 && Settings.EnableAudio && (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource &&
                        !Settings.FFMS_Enable_Audio || x.vdecoder == AviSynthScripting.Decoders.DirectShowSource && !Settings.DSS_Enable_Audio))
                    {
                        AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];
                        if (instream.audiopath == null)
                        {
                            string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                            string outpath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + outext;

                            //удаляем старый файл
                            SafeDelete(outpath);

                            //извлекаем новый файл
                            if (outext == ".wav")
                            {
                                Decoder dec = new Decoder(x, Decoder.DecoderModes.DecodeAudio, outpath);
                                if (dec.IsErrors) throw new Exception("Decode to WAV: " + dec.error_message);
                            }
                            else
                            {
                                Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, outpath);
                                if (dem.IsErrors) throw new Exception(dem.error_message);
                            }

                            //проверка на удачное завершение
                            if (File.Exists(outpath) && new FileInfo(outpath).Length != 0)
                            {
                                instream.audiopath = outpath;
                                instream.audiofiles = new string[] { outpath };
                                instream = Format.GetValidADecoder(instream);
                                deletefiles.Add(outpath);
                            }
                        }
                    }

                    //получаем выходной фреймрейт
                    x = Format.GetValidFramerate(x);
                    x = Calculate.UpdateOutFrames(x);

                    //Получаем информацию через AviSynth и ловим ошибки
                    Caching cach = new Caching(x);
                    if (cach.m == null) return;
                    x = cach.m.Clone();

                    if (x.format != Format.ExportFormats.Audio)
                    {
                        //ситуация когда стоит попробовать декодировать аудио в wav
                        if (cach.error == "FFmpegSource: Audio codec not found")
                        {
                            AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];

                            string outpath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + ".wav";
                            Decoder dec = new Decoder(x, Decoder.DecoderModes.DecodeAudio, outpath);
                            if (dec.IsErrors) throw new Exception("Decode to WAV: " + dec.error_message);

                            //проверка на удачное завершение
                            if (File.Exists(outpath) && new FileInfo(outpath).Length != 0)
                            {
                                instream.audiopath = outpath;
                                instream.audiofiles = new string[] { outpath };
                                instream = Format.GetValidADecoder(instream);
                                deletefiles.Add(outpath);
                            }
                        }
                    }

                    //забиваем-обновляем аудио массивы
                    x = FillAudio(x);

                    //выбираем трек
                    if (x.inaudiostreams.Count > 1 && Settings.EnableAudio)
                    {
                        AudioOptions ao = new AudioOptions(x, this, AudioOptions.AudioOptionsModes.TracksOnly);
                        if (ao.m == null) return;
                        x = ao.m.Clone();
                    }

                    //извлечение трека при badmixing
                    if (x.inaudiostreams.Count == 1 && Settings.EnableAudio)
                    {
                        AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];

                        if (instream.badmixing)
                        {
                            string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                            instream.audiopath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + outext;
                            instream.audiofiles = new string[] { instream.audiopath };
                            instream = Format.GetValidADecoder(instream);

                            if (!File.Exists(instream.audiopath))
                            {
                                Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                                if (dem.IsErrors) throw new Exception(dem.error_message);
                            }
                        }
                    }

                    //забиваем видео настройки
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        x.vencoding = Settings.GetVEncodingPreset(Settings.FormatOut);
                        x.outvcodec = PresetLoader.GetVCodec(x);
                        x.vpasses = PresetLoader.GetVCodecPasses(x);
                    }
                    else
                    {
                        x.vencoding = "Disabled";
                        x.outvcodec = "Disabled";
                        x.vpasses.Clear();
                        combo_vencoding.SelectedItem = x.vencoding;
                    }

                    //забиваем аргументы к кодированию аудио и видео
                    x = PresetLoader.DecodePresets(x);

                    //автоматический деинтерлейс
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        //Клонируем деинтерлейс от предыдущего файла
                        if (IsBatchOpening && CloneIsEnabled && Settings.BatchCloneDeint)
                        {
                            x.interlace = m.interlace;
                            x.fieldOrder = m.fieldOrder;
                            x.deinterlace = m.deinterlace;
                        }
                        else
                        {
                            if (Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.AllFiles &&
                                x.outvcodec != "Copy" ||
                                Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.MPEGs &&
                                Calculate.IsMPEG(x.infilepath) &&
                                x.outvcodec != "Copy" ||
                                Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.MPEGs &&
                                ext == ".evo" &&
                                x.outvcodec != "Copy")
                            {
                                if (x.inframerate == "23.976")
                                {
                                    x.interlace = SourceType.PROGRESSIVE;
                                    x = Format.GetOutInterlace(x);
                                }
                                else
                                {
                                    //перепроверяем входной интерлейс
                                    SourceDetector sd = new SourceDetector(x);
                                    if (sd.m != null) x = sd.m.Clone();
                                }
                            }
                            else
                            {
                                x = Format.GetOutInterlace(x);
                            }
                        }
                    }

                    //ищем субтитры
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        string subs = Calculate.RemoveExtention(x.infilepath, true);
                        if (File.Exists(subs + ".srt")) x.subtitlepath = subs + ".srt";
                        if (File.Exists(subs + ".sub")) x.subtitlepath = subs + ".sub";
                        if (File.Exists(subs + ".idx")) x.subtitlepath = subs + ".idx";
                        if (File.Exists(subs + ".ssa")) x.subtitlepath = subs + ".ssa";
                        if (File.Exists(subs + ".ass")) x.subtitlepath = subs + ".ass";
                        if (File.Exists(subs + ".psb")) x.subtitlepath = subs + ".psb";
                        if (File.Exists(subs + ".smi")) x.subtitlepath = subs + ".smi";
                    }

                    //автокроп
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        //Клонируем АР от предыдущего файла
                        if (IsBatchOpening && CloneIsEnabled && Settings.BatchCloneAR)
                        {
                            x.outresw = m.outresw;
                            x.outresh = m.outresh;
                            x.cropl = x.cropl_copy = m.cropl;
                            x.cropt = x.cropt_copy = m.cropt;
                            x.cropr = x.cropr_copy = m.cropr;
                            x.cropb = x.cropb_copy = m.cropb;
                            x.blackw = m.blackw;
                            x.blackh = m.blackh;
                            x.sar = m.sar;
                            x.outaspect = m.outaspect;
                            x.aspectfix = m.aspectfix;
                        }
                        else
                        {
                            if (Settings.AutocropMode == Autocrop.AutocropMode.AllFiles &&
                            x.outvcodec != "Copy" ||
                            Settings.AutocropMode == Autocrop.AutocropMode.MPEGOnly &&
                            Calculate.IsMPEG(x.infilepath) &&
                            x.outvcodec != "Copy")
                            {
                                if (x.format != Format.ExportFormats.BluRay)
                                {
                                    Autocrop acrop = new Autocrop(x, this);
                                    if (acrop.m == null) return;
                                    x = acrop.m.Clone();
                                }
                            }

                            //подправляем входной аспект
                            x = AspectResolution.FixInputAspect(x);

                            //забиваем видео параметры на выход
                            x = Format.GetValidResolution(x);
                            x = Format.GetValidOutAspect(x);
                            x = AspectResolution.FixAspectDifference(x);
                        }
                        
                        //Клонируем частоту кадров от предыдущего файла
                        if (IsBatchOpening && CloneIsEnabled && Settings.BatchCloneFPS)
                        {
                            x.outframerate = m.outframerate;
                        }
                        else
                        {
                            x = Format.GetValidFramerate(x);
                        }
                        
                        //обновление выходных битрейтов
                        if (x.outvcodec == "Disabled") x.outvbitrate = 0;
                    }
                    else
                    {
                        //для звуковых заданий 
                        x.outframerate = x.inframerate;
                        x.outresw = x.inresw;
                        x.outresh = x.inresh;
                        x.outaspect = x.inaspect;

                        //обнуляем делей
                        foreach (object o in x.outaudiostreams)
                        {
                            AudioStream s = (AudioStream)o;
                            s.delay = 0;
                        }

                        //запрещаем видео разделы
                        combo_vencoding.IsEnabled = false;
                        button_edit_vencoding.IsEnabled = false;
                        combo_sbc.IsEnabled = false;
                        button_edit_sbc.IsEnabled = false;
                    }

                    //переполучаем параметры из профилей
                    x = PresetLoader.DecodePresets(x);

                    //Клонируем Трим от предыдущего файла
                    if (IsBatchOpening && CloneIsEnabled && Settings.BatchCloneTrim)
                    {                       
                        x.trim_start = m.trim_start;
                        x.trim_end = m.trim_end;
                    }

                    //Пересчитываем кол-во кадров и продолжительность
                    x = Calculate.UpdateOutFrames(x);

                    //создаём AviSynth скрипт
                    x = AviSynthScripting.CreateAutoAviSynthScript(x);

                    //Автогромкость
                    if (x.inaudiostreams.Count > 0)
                    {
                        //определяем громкоcть
                        x.volume = Settings.Volume;
                        if (Settings.Volume != "Disabled" &&
                            Settings.AutoVolumeMode == Settings.AutoVolumeModes.OnImport)
                        {
                            Normalize norm = new Normalize(x);
                            if (norm.m == null) return;
                            x = norm.m.Clone();
                            x = AviSynthScripting.CreateAutoAviSynthScript(x);
                        }
                    }

                    //проверка на размер
                    x.outfilesize = Calculate.GetEncodingSize(x);

                    //запрещаем профиль кодирования если нет звука
                    if (x.inaudiostreams.Count == 0 || x.outaudiostreams.Count == 0)
                    {
                        combo_aencoding.SelectedItem = "Disabled";
                    }
                    else
                    {
                        if (combo_aencoding.SelectedItem.ToString() == "Disabled")
                            combo_aencoding.SelectedItem = Settings.GetAEncodingPreset(x.format);
                    }

                    //проверяем можно ли копировать данный формат
                    if (x.vencoding == "Copy")
                    {
                        string CopyProblems = Format.ValidateCopyVideo(x);
                        if (CopyProblems != null)
                        {
                            Message mess = new Message(this);
                            mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                " " + Format.EnumToString(x.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                        }
                    }

                    //настройки форматов
                    x.dontmuxstreams = Format.GetMultiplexing(x.format);
                    x.split = Format.GetSplitting(x.format);
                   
                    //передаём массив
                    m = x.Clone();
                    x = null;

                    //снимаем выделение
                    list_tasks.SelectedIndex = -1;
                    //OldSelectedIndex = -1;

                    //загружаем скрипт в форму
                    if (!IsBatchOpening)
                    {
                        if (!PauseAfterFirst)
                        {
                            //Обновляем список недавно открытых файлов
                            string source = (string.IsNullOrEmpty(m.infilepath_source)) ? m.infilepath : m.infilepath_source;
                            string[] rfiles = Settings.RecentFiles.Split(new string[] { ";" }, StringSplitOptions.None);
                            string output = source + "; ";
                            for (int i = 0; i < rfiles.Length && i < 5; i++)
                            {
                                string line = rfiles[i].Trim();
                                if (line != source && line != "") output += (line + "; ");
                            }
                            Settings.RecentFiles = output;
                            UpdateRecentFiles();
                        }

                        LoadVideo(MediaLoad.load);
                    }

                    //удаляем старый кеш
                    clear_ff_cache();
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                //записываем плохой скрипт
                if (x != null && x.script != null)
                    AviSynthScripting.WriteScriptToFile(x.script, "error");

                x = null;

                if (ex.Message.Contains("DirectX"))
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Need update DirectX files! Do it now?"),
                        Languages.Translate("Error"), Message.MessageStyle.YesNo);
                    if (mess.result == Message.Result.Yes)
                    {
                        Process.Start(Calculate.StartupPath + "\\apps\\DirectX_Update\\dxwebsetup.exe");
                        Close();
                    }
                }
                else
                    ErrorException(ex.Message);
            }
        }

        private void action_save(Massive mass)
        {
            if (mass != null)
            {
                if (currentState == PlayState.Running)
                    PauseClip();

                string oldlocked_name = mass.outfilepath;

                mass.outfilepath = OpenDialogs.SaveDialog(mass);

                if (outfiles.Contains(mass.outfilepath) || mass.infilepath == mass.outfilepath)
                {
                    ErrorException(Languages.Translate("Select another name for output file!"));
                    return;
                }

                if (mass.outfilepath != null)
                {
                    //если задание после редактирования
                    if (list_tasks.SelectedItem != null)
                    {
                        //убираем старое имя из лока
                        if (oldlocked_name != null)
                            outfiles.Remove(oldlocked_name);

                        //добавлем задание в лок
                        outfiles.Add(mass.outfilepath);

                        m = mass.Clone();

                        mass = UpdateOutAudioPath(mass);
                        UpdateTaskMassive(mass);
                    }
                    else
                    {
                        //запоминаем уникальный ключ
                        int n = Convert.ToInt32(Settings.Key) + 1;
                        Settings.Key = n.ToString("0000");
                        m.key = Settings.Key;

                        //добавлем задание в лок
                        outfiles.Add(mass.outfilepath);

                        //убираем выделение из списка заданий
                        list_tasks.SelectedIndex = -1;

                        //Клонируем исходную outaudiostream чтоб потом восстановить её в массиве m, т.к. Mass.Clone() не клонирует,
                        //а создает связанную копию - баг, но я пока-что не знаю как его убрать, зато его можно частично обойти :)
                        AudioStream oldstream = new AudioStream();
                        if (m.outaudiostreams.Count > 0)
                            oldstream = ((AudioStream)m.outaudiostreams[m.outaudiostream]).Clone();

                        mass = UpdateOutAudioPath(mass);

                        //Восстанавливаем исходную outaudiostream в массиве m
                        if (m.outaudiostreams.Count > 0)
                            m.outaudiostreams[m.outaudiostream] = oldstream.Clone();

                        //добавляем задание в список
                        AddTask(mass, "Waiting");
                    }
                    if (PauseAfterFirst)
                    {
                        PauseAfterFirst = false;
                        button_save.Content = Languages.Translate("Enqueue");
                        button_encode.Content = Languages.Translate("Encode");
                        path_to_save = Path.GetDirectoryName(mass.outfilepath);
                        MultiOpen(batch_files);
                        batch_files = null;
                    }
                }
            }
        }

        private void action_encode(Massive mass)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            if (mass.inaudiostreams.Count > 0 && mass.outaudiostreams.Count > 0)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];
                AudioStream outstream = (AudioStream)mass.outaudiostreams[mass.outaudiostream];

                if (instream.audiopath != null && !File.Exists(instream.audiopath))
                {
                    if (!Format.IsDirectRemuxingPossible(mass) && outstream.codec == "Copy" ||
                        Format.IsDirectRemuxingPossible(mass) && outstream.codec != "Copy")
                    {
                        Demuxer dem = new Demuxer(mass, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                        if (dem.m != null) mass = dem.m.Clone();

                        //обновляем скрипт
                        mass = AviSynthScripting.CreateAutoAviSynthScript(mass);

                        UpdateTaskMassive(mass);
                    }
                }

                //определяем громкоcть (перед кодированием)
                if (Settings.Volume != "Disabled" && Settings.AutoVolumeMode == Settings.AutoVolumeModes.OnExport &&
                    outstream.codec != "Copy" && outstream.codec != "Disabled")
                {
                    if (!instream.gaindetected)
                    {
                        mass.volume = Settings.Volume;
                        Normalize norm = new Normalize(mass);
                        mass = norm.m.Clone();
                    }

                    mass = AviSynthScripting.SetGain(mass);
                    UpdateTaskMassive(mass);
                }

                //Временный WAV-файл для 2pass AAC
                if (outstream.codec == "AAC" && mass.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass)
                {
                    if (File.Exists(instream.audiopath) && Path.GetExtension(instream.audiopath) == ".wav" && mass.script == AviSynthScripting.CreateAutoAviSynthScript(mass).script &&
                        mass.trim_start == 0 && mass.trim_end == 0 && !m.testscript && instream.bits == outstream.bits && instream.channels == outstream.channels && instream.delay ==
                        outstream.delay && instream.gain == outstream.gain && instream.samplerate == outstream.samplerate)
                    {
                        outstream.nerotemp = instream.audiopath;
                    }
                    else
                    {
                        outstream.nerotemp = Settings.TempPath + "\\" + mass.key + "_nerotemp.wav";
                        Demuxer dem = new Demuxer(mass, Demuxer.DemuxerMode.NeroTempWAV, outstream.nerotemp);
                        if (dem.IsErrors)
                        {
                            ErrorException(dem.error_message);
                            UpdateTaskStatus(mass.key, "Errors");
                            return;
                        }
                        deletefiles.Add(outstream.nerotemp);
                    }
                }
            }

            try
            {
                //запускаем кодер
                Encoder enc = new Encoder(mass, this);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void CloseFile()
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            CloseClip();
            SetPlayIcon();

            if (Settings.DeleteTempFiles)
            {
                clear_ff_cache();                // - Кэш от FFmpegSource1            
                clear_dgindex_cache();           // - Кэш от DGIndex
                clear_audio_and_video_caches();  // - Извлеченные или декодированные аудио и видео файлы
            }

            SafeDelete(Settings.TempPath + "\\preview.avs");
            SafeDelete(Settings.TempPath + "\\AvsP.avs");
            SafeDelete(Settings.TempPath + "\\AutoCrop.log");

            m = null;
            ResetTrim();      //Обнуляем всё что связано с тримом
            MenuHider(false); //Делаем пункты меню неактивными

            //Если пользователь передумал пакетно открывать файлы
            if (PauseAfterFirst && button_encode.Content.ToString() == Languages.Translate("Resume"))
            {
                PauseAfterFirst = IsBatchOpening = false;
                button_save.Content = Languages.Translate("Enqueue");
                button_encode.Content = Languages.Translate("Encode");
                batch_files = null;
            }
        }

        private void clear_audio_and_video_caches()
        {
            try
            {
                //Если есть задания, проверяем, занят ли там наш файл
                bool busy = false;
                if (list_tasks.Items.Count != 0)
                {
                    foreach (object _task in list_tasks.Items)
                    {
                        if (((Task)_task).Mass.infilepath == m.infilepath) //m.taskname, m.infilepath
                        {
                            busy = true; break;
                        }
                    }
                }
                //Удаляем кэши сразу, или помещаем их в список на удаление, если они участвует в кодировании
                foreach (object s in m.inaudiostreams) //Аудио
                {
                    AudioStream a = (AudioStream)s;
                    if (a.audiopath != null &&
                        Path.GetDirectoryName(a.audiopath) == Settings.TempPath &&
                        a.audiopath != m.infilepath) //Защита от удаления исходника
                        if (!busy) SafeDelete(a.audiopath); else deletefiles.Add(a.audiopath);
                }
                if (Path.GetFileNameWithoutExtension(m.infilepath) != m.taskname && //Видео
                    Path.GetDirectoryName(m.infilepath) == Settings.TempPath)
                    if (!busy) SafeDelete(m.infilepath); else deletefiles.Add(m.infilepath);
            }
            catch (Exception) { }
        }

        private void ErrorException(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        public void LoadVideo(MediaLoad mediaload)
        {
            this.mediaload = mediaload;

            //Сбрасываем флаг ошибки Ависинта при новом открытии
            if (mediaload == MediaLoad.load) IsAviSynthError = false;

            //Чтоб позиция не сбрасывалась на ноль при включенном ScriptView
            //А так-же НЕ сохраняем позицию после ошибки Ависинта в предыдущее открытие
            if (script_box.Visibility == Visibility.Collapsed && !IsAviSynthError)
                oldpos = Position;

            // If we have ANY file open, close it and shut down DirectShow
            if (this.currentState != PlayState.Init)
                CloseClip();

            try
            {
                //Пишем скрипт в файл
                AviSynthScripting.WriteScriptToFile(m.script, "preview");
                AviSynthScripting.WriteScriptToFile(m.script, "AvsP"); //скрипт для AvsP

                // Start playing the media file
                if (Settings.ScriptView)
                {
                    this.IsAudioOnly = false;
                    this.currentState = PlayState.Stopped;
                    script_box.Visibility = Visibility.Visible;
                    script_box.Text = m.script;
                    fps = Calculate.ConvertStringToDouble(m.outframerate);
                }
                else
                {
                    // Reset status variables
                    if (mediaload != MediaLoad.update)
                        this.currentState = PlayState.Stopped;

                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                        PlayMovieInWindow(Settings.TempPath + "\\preview.avs");
                    else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                        PlayWithMediaBridge(Settings.TempPath + "\\preview.avs");
                    else
                    {
                        this.IsAudioOnly = false;
                        this.currentState = PlayState.Stopped;
                        if (mediaload == MediaLoad.load)
                        {
                            //Загружаем первый кадр или середину видео
                            pic_frame = ((Settings.AfterImportAction == Settings.AfterImportActions.Middle) ? m.thmframe : 0);
                        }
                        ShowWithPictureView(m.script, pic_frame);
                    }

                    this.Focus();
                    slider_pos.Focus(); //Переводит фокус на полосу прокрутки видео
                }

                if (mediaload == MediaLoad.load) 
                    MenuHider(true); //Делаем пункты меню активными

                textbox_name.Text = m.taskname;
            }
            catch (Exception ex)
            {
                CloseClip();
                m = null;
                MenuHider(false); //Делаем пункты меню неактивными

                if (ex.Message.Contains("DirectX"))
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("DirectX update required! Do it now?"),
                        Languages.Translate("Error"), Message.MessageStyle.YesNo);
                    if (mess.result == Message.Result.Yes)
                    {
                        Process.Start(Calculate.StartupPath + "\\apps\\DirectX_Update\\dxwebsetup.exe");
                        Close();
                    }
                }
                else
                {
                    ErrorException(ex.Message);
                }

                return;
            }
        }

        public void SetHotKeys()
        {
            try
            {
                HotKeys.FillData(); //Обновляем список действий и ключей к ним
                //File
                mnOpen.InputGestureText = HotKeys.GetKeys("Open file(s)");
                menu_open_folder.InputGestureText = HotKeys.GetKeys("Open folder");
                menu_dvd.InputGestureText = HotKeys.GetKeys("Open DVD folder");
                menu_decode_file.InputGestureText = HotKeys.GetKeys("Decode file");
                menu_join.InputGestureText = HotKeys.GetKeys("Join file");
                mnCloseFile.InputGestureText = HotKeys.GetKeys("Close file");
                mnSave.InputGestureText = HotKeys.GetKeys("Save task");
                menu_save_frame.InputGestureText = HotKeys.GetKeys("Save frame");
                menu_savethm.InputGestureText = HotKeys.GetKeys("Save THM frame");
                mnExit.InputGestureText = "Alt+F4";
                //Video
                mnUpdateVideo.InputGestureText = cmn_refresh.InputGestureText = HotKeys.GetKeys("Refresh preview");
                menu_demux_video.InputGestureText = HotKeys.GetKeys("VDemux");                //
                menu_autocrop.InputGestureText = HotKeys.GetKeys("Detect black borders");
                menu_detect_interlace.InputGestureText = HotKeys.GetKeys("Detect interlace");
                menu_saturation_brightness.InputGestureText = HotKeys.GetKeys("Color correction");
                mnAspectResolution.InputGestureText = HotKeys.GetKeys("Resolution/Aspect");
                menu_interlace.InputGestureText = HotKeys.GetKeys("Interlace/Framerate");
                menu_venc_settings.InputGestureText = HotKeys.GetKeys("VEncoding settings");
                //Audio
                menu_demux.InputGestureText = HotKeys.GetKeys("ADemux");                      //
                menu_save_wav.InputGestureText = HotKeys.GetKeys("Save to WAV");
                menu_audiooptions.InputGestureText = HotKeys.GetKeys("Editing options");      //
                menu_aenc_settings.InputGestureText = HotKeys.GetKeys("AEncoding settings");  //
                //Subtitles
                mnAddSubtitles.InputGestureText = HotKeys.GetKeys("Add subtitles");           //
                mnRemoveSubtitles.InputGestureText = HotKeys.GetKeys("Remove subtitles");     //
                //AviSynth
                menu_avsp.InputGestureText = HotKeys.GetKeys("AvsP editor");
                menu_editscript.InputGestureText = HotKeys.GetKeys("Edit filtering script");
                menu_createtestscript.InputGestureText = HotKeys.GetKeys("Test script");
                menu_save_script.InputGestureText = HotKeys.GetKeys("Save script");
                menu_playinwmp.InputGestureText = HotKeys.GetKeys("Windows Media Player");
                menu_playinmpc.InputGestureText = HotKeys.GetKeys("Media Player Classic");
                menu_playinwpf.InputGestureText = HotKeys.GetKeys("WPF Video Player");
                //Tools
                menu_info_media.InputGestureText = HotKeys.GetKeys("Media Info");
                menu_ffrebuilder.InputGestureText = HotKeys.GetKeys("FFRebuilder");
                menu_mkvrebuilder.InputGestureText = HotKeys.GetKeys("MKVRebuilder");
                mnDGIndex.InputGestureText = HotKeys.GetKeys("DGIndex");
                menu_dgpulldown.InputGestureText = HotKeys.GetKeys("DGPulldown");
                mnDGAVCIndex.InputGestureText = HotKeys.GetKeys("DGAVCIndex");
                menu_virtualdubmod.InputGestureText = HotKeys.GetKeys("VirtualDubMod");
                menu_avimux.InputGestureText = HotKeys.GetKeys("AVI-Mux");
                menu_tsmuxer.InputGestureText = HotKeys.GetKeys("tsMuxeR");
                menu_mkvextract.InputGestureText = HotKeys.GetKeys("MKVExtract");
                menu_mkvmerge.InputGestureText = HotKeys.GetKeys("MKVMerge");
                menu_yamb.InputGestureText = HotKeys.GetKeys("Yamb");

                cmn_addtobookmarks.InputGestureText = HotKeys.GetKeys("Add/Remove bookmark");
            }
            catch { }
        }

        //переводим лейблы
        private void SetLanguage()
        {
            try
            {
                //Считываем словарь
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\languages\\" + Settings.Language + ".txt", System.Text.Encoding.Default))
                    Languages.Dictionary = sr.ReadToEnd();
                                
                mnFile.Header = Languages.Translate("File");
                mnOpen.Header = Languages.Translate("Open file(s)") + "...";
                menu_decode_file.Header = Languages.Translate("Decode file") + "...";
                menu_join.Header = Languages.Translate("Join file") + "...";
                mnSave.Header = Languages.Translate("Add task") + "...";
                menu_dvd.Header = Languages.Translate("Open DVD folder") + "...";
                button_dvd.ToolTip = Languages.Translate("Open DVD folder");
                mnExit.Header = tmnExit.Text = Languages.Translate("Exit");
                mnCloseFile.Header = Languages.Translate("Close file");
                menu_save_frame.Header = Languages.Translate("Save frame") + "...";
                menu_savethm.Header = Languages.Translate("Save") + " THM...";
                mnRecentFiles.Header = Languages.Translate("Recent files");

                mnVideo.Header = Languages.Translate("Video");
                mnAudio.Header = Languages.Translate("Audio");
                mnSubtitles.Header = Languages.Translate("Subtitles");
                //mnPlayer.Header = Languages.Translate("Player");
                menu_audiooptions.Header = Languages.Translate("Processing options") + "...";
                menu_save_wav.Header = Languages.Translate("Save to WAV") + "...";
                menu_demux.Header = menu_demux_video.Header = Languages.Translate("Demux") + "...";
                //menu_demux.Header = Languages.Translate("Save to");
                //menu_demux_video.Header = Languages.Translate("Save to");

                mnUpdateVideo.Header = cmn_refresh.Header = Languages.Translate("Refresh preview");
                menu_createautoscript.Header = Languages.Translate("Create auto script");
                menu_createtestscript.Header = Languages.Translate("Test script");
                menu_editscript.Header = Languages.Translate("Edit filtering script");
                menu_avsp.Header = Languages.Translate("AvsP editor");

                mnAspectResolution.Header = Languages.Translate("Resolution/Aspect") + "...";
                menu_interlace.Header = Languages.Translate("Interlace/Framerate") + "...";

                mnAddSubtitles.Header = Languages.Translate("Add");
                mnRemoveSubtitles.Header = Languages.Translate("Remove");

                menu_save_script.Header = Languages.Translate("Save script");

                edit_wmp.ToolTip = edit_mpc.ToolTip = edit_wpf.ToolTip = Languages.Translate("Edit path");
                menu_playinwmp.Header = Languages.Translate("Play in") + " Windows Media Player";
                menu_playinmpc.Header = Languages.Translate("Play in") + " Media Player Classic";
                menu_playinwpf.Header = Languages.Translate("Play in") + " WPF Video Player";

                mnSettings.Header = Languages.Translate("Settings");
                menu_settings.Header = Languages.Translate("Global settings") + "...";
                mnLanguage.Header = Languages.Translate("Language");
                mnResetSettings.Header = Languages.Translate("Reset all settings") + "...";
                mnAfterImport.Header = Languages.Translate("After opening");
                after_i_play.Header = Languages.Translate("Play");
                after_i_nothing.Header = Languages.Translate("Nothing");
                after_i_middle.Header = Languages.Translate("Middle");

                menu_auto_crop.Header = Languages.Translate("Auto crop");
                menu_auto_volume.Header = Languages.Translate("Auto volume");
                auto_volume_disabled.Header = acrop_disabled.Header = auto_deint_disabled.Header = auto_join_disabled.Header =
                    Languages.Translate("Disabled");
                auto_volume_onexp.Header = Languages.Translate("Before encoding");
                auto_volume_onimp.Header = Languages.Translate("After opening");
                acrop_mpeg.Header = auto_deint_mpeg.Header = Languages.Translate("MPEG`s only");
                acrop_allfiles.Header = auto_deint_all.Header = Languages.Translate("All files");

                menu_auto_deinterlace.Header = Languages.Translate("Auto deinterlace");

                menu_auto_join.Header = Languages.Translate("Auto join");
                auto_join_enabled.Header = Languages.Translate("Enabled");
                auto_join_onlydvd.Header = Languages.Translate("DVD Only");

                mnVideoDecoding.Header = mnAudioDecoding.Header = Languages.Translate("Decoding") + "...";

                menu_fix_AVCHD.Header = Languages.Translate("Convert BluRay UDF to FAT32");

                menu_player_engine.Header = Languages.Translate("Player engine");

                mnTools.Header = Languages.Translate("Tools");
                mnHelp.Header = Languages.Translate("Help");
                mnAbout.Header = Languages.Translate("About");

                tmnTrayClose.Text = Languages.Translate("Close to tray");
                tmnTrayMinimize.Text = Languages.Translate("Minimize to tray");
                tmnTrayClickOnce.Text = Languages.Translate("Single click to open");
                tmnTrayNoBalloons.Text = Languages.Translate("Disable balloons");

                text_vencoding.Content = Languages.Translate("Video encoding") + ":";
                text_aencoding.Content = Languages.Translate("Audio encoding") + ":";
                text_filtering.Content = Languages.Translate("Filtering") + ":";
                text_sbc.Content = Languages.Translate("Color correction") + ":";
                menu_saturation_brightness.Header = Languages.Translate("Color correction") + "...";
                text_format.Content = Languages.Translate("Format") + ":";
                button_edit_filters.ToolTip = Languages.Translate("Edit filtering script");
                button_edit_vencoding.ToolTip = Languages.Translate("Edit video encoding settings");
                button_edit_aencoding.ToolTip = Languages.Translate("Edit audio encoding settings");
                menu_aenc_settings.Header = menu_venc_settings.Header = Languages.Translate("Encoding settings") + "...";
                button_edit_sbc.ToolTip = Languages.Translate("Edit saturation, brightness or contrast");
                button_edit_format.ToolTip = Languages.Translate("Edit format settings");

                button_open.Content = Languages.Translate("Open");
                button_open.ToolTip = Languages.Translate("Open new file(s)");
                button_configure.Content = Languages.Translate("Configure");
                button_configure.ToolTip = Languages.Translate("Configure audio-processing options");
                button_save.Content = Languages.Translate("Enqueue");
                button_save.ToolTip = Languages.Translate("Add task to the list");
                button_encode.Content = Languages.Translate("Encode");
                button_encode.ToolTip = Languages.Translate("Start files encoding");
                button_close.Content = Languages.Translate("Close");
                button_close.ToolTip = Languages.Translate("Close current file");

                list_tasks.ToolTip = Languages.Translate("Task list");

                slider_Volume.ToolTip = Languages.Translate("Volume");
                button_play.ToolTip = Languages.Translate("Play-Pause");
                button_frame_back.ToolTip = Languages.Translate("Frame back");
                button_frame_forward.ToolTip = Languages.Translate("Frame forward");
                button_save_script.Content = Languages.Translate("Apply");
                button_play_script.Content = Languages.Translate("Play");
                button_play_script.ToolTip = Languages.Translate("Play in") + " Media Player Classic";
                button_fullscreen.ToolTip = Languages.Translate("Fullscreen");

                cmenu_up.Header = Languages.Translate("Move up");
                cmenu_down.Header = Languages.Translate("Move down");
                cmenu_first.Header = Languages.Translate("Move to the first");
                cmenu_last.Header = Languages.Translate("Move to the last");
                cmenu_deselect.Header = Languages.Translate("Deselect");
                cmenu_delete_all_tasks.Header = Languages.Translate("Delete all tasks");
                cmenu_delete_encoded_tasks.Header = Languages.Translate("Delete encoded tasks");
                cmenu_delete_task.Header = Languages.Translate("Delete selected task");
                cmenu_is_always_delete_encoded.Header = Languages.Translate("Always delete encoded tasks from list");
                cmenu_reset_status.Header = Languages.Translate("Reset task status");
                cmenu_save_all_scripts.Header = Languages.Translate("Save all scripts");

                menu_directx_update.Header = Languages.Translate("Update DirectX");
                menu_autocrop.Header = Languages.Translate("Detect black borders");
                menu_detect_interlace.Header = Languages.Translate("Detect interlace");
                menu_home.Header = Languages.Translate("Home page");
                menu_support.Header = Languages.Translate("Support forum");
                menu_donate.Header = Languages.Translate("Donate");
                menu_avisynth_guide_en.Header = Languages.Translate("AviSynth guide") + " (EN)";
                menu_avisynth_guide_ru.Header = Languages.Translate("AviSynth guide") + " (RU)";

                button_set_start.Content = Languages.Translate("Set Start");
                button_set_end.Content = Languages.Translate("Set End");
                button_apply_trim.Content = Languages.Translate("Apply Trim");
                textbox_start.ToolTip = Languages.Translate("Enter frame number or time position (HH:MM:SS.ms), then press \"XXXXX\" button.").Replace("XXXXX", button_set_start.Content.ToString()) +
                    "\r\n" + Languages.Translate("If you leave this field empty, then current frame number will be entered automatically.");
                textbox_end.ToolTip = Languages.Translate("Enter frame number or time position (HH:MM:SS.ms), then press \"XXXXX\" button.").Replace("XXXXX", button_set_end.Content.ToString()) +
                    "\r\n" + Languages.Translate("If you leave this field empty, then current frame number will be entered automatically.");
                menu_open_folder.Header = Languages.Translate("Open folder") + "...";
                mnApps_Folder.Header = Languages.Translate("Open XviD4PSP folder");
                menu_info_media.ToolTip = Languages.Translate("Provides exhaustive information about the open file.") + Environment.NewLine + Languages.Translate("You can manually choose a file to open and select the type of information to show too");
                target_goto.ToolTip = Languages.Translate("Frame counter. Click on this area to enter frame number to go to.") + "\r\n" + Languages.Translate("Rigth-click will insert current frame number.") +
                    "\r\n" + Languages.Translate("Enter - apply, Esc - cancel.");
                
                check_old_seeking.ToolTip = Languages.Translate("If checked, Old method (continuous positioning while you move slider) will be used,") +
                    Environment.NewLine + Languages.Translate("otherwise New method is used (recommended) - position isn't set untill you release mouse button");
                check_scriptview_white.ToolTip = Languages.Translate("Enable white background for ScriptView");
                cmn_addtobookmarks.Header = Languages.Translate("Add/Remove bookmark");
                cmn_deletebookmarks.Header = Languages.Translate("Delete all bookmarks");
                cmn_bookmarks.Header = Languages.Translate("Bookmarks");
            }
            catch { }
        }

        //загружаем настройки
        private void LoadSettings()
        {
            string l = Settings.Language;
            if (l == "Russian") mnRussian.IsChecked = true;
            else if (l == "Ukrainian") check_ukrainian.IsChecked = true;
            else if (l == "Italian") check_italian.IsChecked = true;
            else if (l == "German") check_german.IsChecked = true;
            else if (l == "Hebrew") check_hebrew.IsChecked = true;
            else if (l == "Spanish") check_spanish.IsChecked = true;
            else if (l == "French") check_french.IsChecked = true;
            else if (l == "Portuguese") check_portuguese.IsChecked = true;
            else if (l == "Chinese") check_chinese.IsChecked = true;
            else if (l == "Hungarian") check_hungarian.IsChecked = true;
            else if (l == "Estonian") check_estonian.IsChecked = true;
            else mnEnglish.IsChecked = true;

            if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) check_engine_directshow.IsChecked = true;
            else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge) check_engine_mediabridge.IsChecked = true;
            else check_engine_pictureview.IsChecked = true;

            int vr = Settings.VideoRenderer;
            if (vr == 0) vr_default.IsChecked = true;
            else if (vr == 1) vr_overlay.IsChecked = true;
            else if (vr == 2) vr_vmr7.IsChecked = true;
            else if (vr == 3) vr_vmr9.IsChecked = true;
            
            if (Settings.ScriptView)
            {
                mn_scriptview.IsChecked = cmn_scriptview.IsChecked = true;
                button_save_script.Visibility = button_fullscreen.Visibility = button_avsp.Visibility = button_play_script.Visibility = Visibility.Visible;
                button_play.Visibility = button_frame_back.Visibility = button_frame_forward.Visibility = slider_pos.Visibility = Visibility.Collapsed;
            }
            else mn_preview.IsChecked = cmn_preview.IsChecked = true;

            if (Settings.ScriptView_Brushes == "#FFFFFFFF:#FF000000")
            {
                check_scriptview_white.IsChecked = true;
                script_box.Background = Brushes.White;
                script_box.Foreground = Brushes.Black;
            }
            else
            {
                string[] brushes = Settings.ScriptView_Brushes.Split(new string[] { ":" }, StringSplitOptions.None);
                if (brushes.Length == 2 && brushes[0].Length == 9 && brushes[1].Length == 9)
                {
                    BrushConverter bc = new BrushConverter();
                    script_box.Background = (Brush)bc.ConvertFromString(brushes[0]);
                    script_box.Foreground = (Brush)bc.ConvertFromString(brushes[1]);
                }
            }

            if (Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly) check_auto_join_onlydvd.IsChecked = true;
            else if (Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled) check_auto_join_enabled.IsChecked = true;
            else check_auto_join_disabled.IsChecked = true;

            if (Settings.AfterImportAction == Settings.AfterImportActions.Middle) menu_after_i_middle.IsChecked = true;
            else if (Settings.AfterImportAction == Settings.AfterImportActions.Nothing) menu_after_i_nothing.IsChecked = true;
            else menu_after_i_play.IsChecked = true;

            if (Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.AllFiles) check_auto_deint_all.IsChecked = true;
            else if (Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.Disabled) check_auto_deint_disabled.IsChecked = true;
            else check_auto_deint_mpeg.IsChecked = true;

            if (Settings.AutocropMode == Autocrop.AutocropMode.AllFiles) menu_acrop_allfiles.IsChecked = true;
            else if (Settings.AutocropMode == Autocrop.AutocropMode.Disabled) menu_acrop_disabled.IsChecked = true;
            else menu_acrop_mpeg.IsChecked = true;

            if (Settings.AutoVolumeMode == Settings.AutoVolumeModes.Disabled) menu_auto_volume_disabled.IsChecked = true;
            else if (Settings.AutoVolumeMode == Settings.AutoVolumeModes.OnImport) menu_auto_volume_onimp.IsChecked = true;
            else menu_auto_volume_onexp.IsChecked = true;

            cmenu_is_always_delete_encoded.IsChecked = Settings.AutoDeleteTasks;

            //Установка параметров регулятора громкости
            slider_Volume.Value = Settings.VolumeLevel; //Установка значения громкости из реестра..
            VolumeSet = -(int)(10000 - Math.Pow(slider_Volume.Value, 1.0 / 5) * 10000); //.. и пересчет его для ДиректШоу
            if (slider_Volume.Value == 0) image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume2.png", UriKind.RelativeOrAbsolute));

            check_old_seeking.IsChecked = OldSeeking = Settings.OldSeeking;
        }

        private void Languages_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (English.IsFocused) mnEnglish.IsChecked = true;
            else if (Russian.IsFocused) mnRussian.IsChecked = true;
            else if (Italian.IsFocused) check_italian.IsChecked = true;
            else if (Chinese.IsFocused) check_chinese.IsChecked = true;
            else if (Portuguese.IsFocused) check_portuguese.IsChecked = true;
            else if (Spanish.IsFocused) check_spanish.IsChecked = true;
            else if (German.IsFocused) check_german.IsChecked = true;
            else if (Hungarian.IsFocused) check_hungarian.IsChecked = true;
            else if (Ukrainian.IsFocused) check_ukrainian.IsChecked = true;
            else if (French.IsFocused) check_french.IsChecked = true;
            else if (Hebrew.IsFocused) check_hebrew.IsChecked = true;
            else if (Estonian.IsFocused) check_estonian.IsChecked = true;

            Settings.Language = ((MenuItem)sender).Header.ToString();
            SetLanguage();
        }     

        private void mnUpdateVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                LoadVideo(MediaLoad.update);
                UpdateTaskMassive(m);
            }
        }

        private void mnAddSubtitles_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                if (m.format == Format.ExportFormats.Audio)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
                }
                else
                {
                    string infilepath = null;
                    ArrayList files = OpenDialogs.GetFilesFromConsole("sub");
                    if (files.Count > 0)
                        infilepath = files[0].ToString();

                    if (infilepath != null)
                    {
                        m.subtitlepath = infilepath;

                        //создаём новый AviSynth скрипт
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);

                        //загружаем обновлённый скрипт
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
            }
        }

        private void mnRemoveSubtitles_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                if (m.subtitlepath != null)
                {
                    m.subtitlepath = null;

                    //создаём новый AviSynth скрипт
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);

                    //загружаем обновлённый скрипт
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
                }
                else
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Nothing for remove!"), Languages.Translate("Error"));
                }
            }
        }

        private void slider_pos_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (m != null && (this.graphBuilder != null || Pic.Visibility == Visibility.Visible)) //&& NaturalDuration != TimeSpan.Zero)
            {
                if (OldSeeking && slider_pos.IsMouseOver) //Непрерывное позиционирование (старый способ)
                {
                    Visual visual = Mouse.Captured as Visual;
                    if (visual != null && visual.IsDescendantOf(slider_pos))
                        Position = TimeSpan.FromSeconds(slider_pos.Value);
                }

                //устанавливаем фрейм для THM
                m.thmframe = Convert.ToInt32(slider_pos.Value * fps);
                textbox_frame.Text = Convert.ToString(m.thmframe) + "/" + total_frames; //Обновляем счетчик кадров
            }
        }

        private void slider_pos_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!OldSeeking)
                Position = TimeSpan.FromSeconds(slider_pos.Value); //Позиционирование при отпускании кнопки мыши (новый способ)
        }

        private void slider_pos_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.graphBuilder != null || Pic.Visibility == Visibility.Visible)
            {
                if (OldSeeking == true)
                {
                    OldSeeking = false;
                    textbox_frame.Text = ("New seeking");
                }
                else
                {
                    OldSeeking = true;
                    textbox_frame.Text = ("Old seeking");
                }
                check_old_seeking.IsChecked = OldSeeking;
                Settings.OldSeeking = OldSeeking;
            }
        }

        private void check_Old_Seeking_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.OldSeeking = OldSeeking = check_old_seeking.IsChecked;
        }

        private void button_play_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                PauseClip();
        }

        private void button_frame_back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Frame_Shift(-1);
        }

        private void button_frame_forward_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Frame_Shift(1);
        }

        //Кадр вперед\назад
        private void Frame_Shift(int step)
        {
            if (m == null) return;
            try
            {
                if (this.graphBuilder != null)
                {
                    if (currentState == PlayState.Running) PauseClip();
                    TimeSpan newpos = Position + TimeSpan.FromSeconds(step / fps);
                    newpos = (newpos < TimeSpan.Zero) ? TimeSpan.Zero : (newpos > NaturalDuration) ? NaturalDuration : newpos;
                    if (Position != newpos) Position = newpos;
                }
                else if (Pic.Visibility == Visibility.Visible)
                {
                    int new_frame = pic_frame + step;
                    new_frame = (new_frame < 0) ? 0 : (new_frame > pic_total_frames) ? pic_total_frames : new_frame;
                    if (pic_frame != new_frame) ShowWithPictureView(m.script, new_frame);
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void player_engine_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.PlayerEngine.ToString() == ((MenuItem)sender).Header.ToString()) return;
            
            PlayState cstate = currentState;
            if (m != null) CloseClip();
            
            //Удаляем старое
            if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
            {
                this.LocationChanged -= new EventHandler(MainWindow_LocationChanged);
                this.SizeChanged -= new SizeChangedEventHandler(MainWindow_SizeChanged);
                this.grid_tasks.SizeChanged -= new SizeChangedEventHandler(grid_tasks_SizeChanged);
                
                source.RemoveHook(new HwndSourceHook(WndProc));
            }
            else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
            {
                VideoElement.Visibility = Visibility.Collapsed;
                VideoElement.MediaOpened -= new RoutedEventHandler(VideoElement_MediaOpened);
                VideoElement.MediaEnded -= new RoutedEventHandler(VideoElement_MediaEnded);
            }
            else
            {
                if (!Settings.ScriptView) button_play.Visibility = Visibility.Visible;
                button_frame_back.Margin = new Thickness(38.2, 6.1, 0, 6.1);
                button_frame_forward.Margin = new Thickness(65.2, 6.1, 0, 6.1);
                slider_pos.Margin = new Thickness(94.15, 7.7, 4, 7.7);
                Pic.Visibility = Visibility.Collapsed;
            }

            //Добавляем новое
            if (((MenuItem)sender).Header.ToString() == "DirectShow")
            {
                this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
                this.grid_tasks.SizeChanged += new SizeChangedEventHandler(grid_tasks_SizeChanged);

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

                IsAudioOnly = false;
                check_engine_mediabridge.IsChecked = true;
                Settings.PlayerEngine = Settings.PlayerEngines.MediaBridge;
            }
            else
            {
                IsAudioOnly = false;
                button_play.Visibility = Visibility.Collapsed;
                button_frame_back.Margin = new Thickness(4, 6.1, 0, 6.1);
                button_frame_forward.Margin = new Thickness(31, 6.1, 0, 6.1);
                slider_pos.Margin = new Thickness(60, 7.7, 4, 7.7);

                check_engine_pictureview.IsChecked = true;
                Settings.PlayerEngine = Settings.PlayerEngines.PictureView;
            }

            currentState = cstate;

            //Обновляем превью
            if (m != null)
            {
                LoadVideo(MediaLoad.update);
            }
        }

        private void scriptview_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Header.ToString() == "ScriptView")
            {
                Settings.ScriptView = true;
                mn_scriptview.IsChecked = cmn_scriptview.IsChecked = true;
                button_save_script.Visibility = button_fullscreen.Visibility = button_avsp.Visibility = button_play_script.Visibility = Visibility.Visible;
                button_play.Visibility = button_frame_back.Visibility = button_frame_forward.Visibility = slider_pos.Visibility = Visibility.Collapsed;
            }
            else
            {
                Settings.ScriptView = false;
                mn_preview.IsChecked = cmn_preview.IsChecked = true;
                button_save_script.Visibility = button_fullscreen.Visibility = button_avsp.Visibility = button_play_script.Visibility = Visibility.Collapsed;
                button_frame_back.Visibility = button_frame_forward.Visibility = slider_pos.Visibility = Visibility.Visible;
                if (Settings.PlayerEngine != Settings.PlayerEngines.PictureView) button_play.Visibility = Visibility.Visible;
            }

            //Обновляем превью
            if (m != null)
            {
                LoadVideo(MediaLoad.update);
            }
        }

        private void check_auto_join_disabled_Click(object sender, RoutedEventArgs e)
        {
            check_auto_join_disabled.IsChecked = true;
            Settings.AutoJoinMode = Settings.AutoJoinModes.Disabled;
        }

        private void check_auto_join_enabled_Click(object sender, RoutedEventArgs e)
        {
            check_auto_join_enabled.IsChecked = true;
            Settings.AutoJoinMode = Settings.AutoJoinModes.Enabled;
        }

        private void check_auto_join_onlydvd_Click(object sender, RoutedEventArgs e)
        {
            check_auto_join_onlydvd.IsChecked = true;
            Settings.AutoJoinMode = Settings.AutoJoinModes.DVDonly;
        }

        public void Refresh(string script)
        {
            m.script = script;
            LoadVideo(MediaLoad.update);
            //MoveVideoWindow();//
            UpdateTaskMassive(m);
        }

        private void CreateAutoScript(object sender, RoutedEventArgs e)
        {
            string oldscript = m.script;
            if (m.inaudiostreams.Count > 0 &&
                m.outaudiostreams.Count > 0)
            {
                m = Format.GetValidSamplerate(m);
                m = Format.GetValidBits(m);
                m = Format.GetValidChannelsConverter(m);
                m = Format.GetValidChannels(m);
            }

            if (m.format != Format.ExportFormats.Audio)
            {
                m = Format.GetOutInterlace(m);
                m = Format.GetValidFramerate(m);
                m = Calculate.UpdateOutFrames(m);

                m = Format.GetValidResolution(m);
                m = Format.GetValidOutAspect(m);
                m = AspectResolution.FixAspectDifference(m);
            }

            m = AviSynthScripting.CreateAutoAviSynthScript(m);
            if (oldscript != m.script || Settings.ScriptView)
            {
                LoadVideo(MediaLoad.update);
                UpdateTaskMassive(m);
            }
        }

        public void EditScript(object sender, RoutedEventArgs e)
        {
            if (m == null) return;
            try
            {
                Filtering f = new Filtering(m, this);
                string oldscript = m.script;
                m.script = f.m.script;

                //обновление при необходимости
                if (m.script.Trim() != oldscript.Trim())
                {
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void AspectResolution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.outvcodec == "Copy")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                }
                else
                {
                    AspectResolution asres = new AspectResolution(m, this);
                    string oldscript = m.script;
                    AspectResolution.AspectFixes oldafix = m.aspectfix;
                    m = asres.m.Clone();
                    //обновление при необходимости
                    if (m.script != oldscript ||
                        m.aspectfix != oldafix)
                    {
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
            }
        }

        private void mnDecoding_Click(object sender, RoutedEventArgs e)
        {
            Decoders_Settings ds;

            if (((MenuItem)sender).Name == "mnAudioDecoding")
                ds = new Decoders_Settings(m, this, 2);
            else
                ds = new Decoders_Settings(m, this, 1);

            if (m != null && ds.NeedUpdate)
            {
                //Дублируем текущий массив для возможности восстановления
                Massive old_m = m.Clone();
                
                //Новый массив с измененными декодерами и скриптом
                m = ds.m.Clone();
                
                reopen_file(old_m);
            }
        }

        //повторное открытие файла после смены декодера
        private void reopen_file(Massive old_m)
        {           
            //Получаем информацию через AviSynth и ловим ошибки
            Caching cach = new Caching(m);
            if (cach.m == null)
            {
                //Восстанавливаем массив из сохраненного
                m = old_m.Clone();
                old_m = null;
                return;
            }
            m = cach.m.Clone();
            old_m = null;

            //перезабиваем специфику формата
            if (m.format != Format.ExportFormats.Audio)
            {
                m = Format.GetOutInterlace(m);
                m = Format.GetValidResolution(m);
                m = Format.GetValidOutAspect(m);
                m = AspectResolution.FixAspectDifference(m);
                m = Format.GetValidFramerate(m);
                m = Calculate.UpdateOutFrames(m);
            }

            m = FillAudio(m);

            //обнуляем громкость
            foreach (object s in m.inaudiostreams)
            {
                AudioStream stream = (AudioStream)s;
                stream.gain = "0.0";
                stream.gaindetected = false;
            }

            //обновляем скрипт с учётом полученных данных
            m = AviSynthScripting.CreateAutoAviSynthScript(m);

            //обновляем дочерние окна
            ReloadChildWindows();

            //загружаем видео в плеер
            LoadVideo(MediaLoad.update);
            UpdateTaskMassive(m);
        }

        private void menu_after_i_nothing_Click(object sender, RoutedEventArgs e)
        {
            menu_after_i_nothing.IsChecked = true;
            Settings.AfterImportAction = Settings.AfterImportActions.Nothing;
        }

        private void menu_after_i_middle_Click(object sender, RoutedEventArgs e)
        {
            menu_after_i_middle.IsChecked = true;
            Settings.AfterImportAction = Settings.AfterImportActions.Middle;
        }

        private void menu_after_i_play_Click(object sender, RoutedEventArgs e)
        {
            menu_after_i_play.IsChecked = true;
            Settings.AfterImportAction = Settings.AfterImportActions.Play;
        }

        private void menu_acrop_disabled_Click(object sender, RoutedEventArgs e)
        {
            menu_acrop_disabled.IsChecked = true;
            Settings.AutocropMode = Autocrop.AutocropMode.Disabled;
        }

        private void menu_acrop_mpeg_Click(object sender, RoutedEventArgs e)
        {
            menu_acrop_mpeg.IsChecked = true;
            Settings.AutocropMode = Autocrop.AutocropMode.MPEGOnly;
        }

        private void menu_acrop_allfiles_Click(object sender, RoutedEventArgs e)
        {
            menu_acrop_allfiles.IsChecked = true;
            Settings.AutocropMode = Autocrop.AutocropMode.AllFiles;
        }

        private void SaveScript(object sender, RoutedEventArgs e)
        {
            if (m == null) return;
            System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
            s.FileName = m.taskname + ".avs";
            s.Title = Languages.Translate("Save script") + ":";
            s.Filter = "AviSynth " + Languages.Translate("files") + "|*.avs";
            if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    WriteScriptToFile(m, s.FileName);
                }
                catch (Exception ex)
                {
                    new Message(this).ShowMessage(ex.Message, Languages.Translate("Error"));
                }
            }
        }

        private void cmenu_save_all_scripts_Click(object sender, RoutedEventArgs e)
        {
            if (list_tasks.Items.Count == 0) return;
            try
            {
                foreach (object _task in list_tasks.Items)
                {
                    Task task = (Task)_task;
                    string path = Path.GetDirectoryName(task.Mass.outfilepath) + "\\" + Path.GetFileNameWithoutExtension(task.Mass.outfilepath) + ".avs";
                    WriteScriptToFile(task.Mass, path);
                }
                new Message(this).ShowMessage(Languages.Translate("Complete"), "OK");
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void WriteScriptToFile(Massive mass, string path)
        {
            StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.Default);
            if (mass.isvideo && !string.IsNullOrEmpty(mass.sar) && mass.sar != "1:1")
            {
                //Сохраняем SAR в скрипте (для анаморфного кодирования)
                string[] sar = mass.sar.Split(new string[] { ":" }, StringSplitOptions.None);
                if (sar.Length == 2) sw.WriteLine("OUT_SAR_X = " + sar[0] + "\r\nOUT_SAR_Y = " + sar[1] + "\r\n");
            }
            sw.Write(mass.script);
            sw.Close();
        }

        private void menu_auto_volume_disabled_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            menu_auto_volume_disabled.IsChecked = true;
            Settings.AutoVolumeMode = Settings.AutoVolumeModes.Disabled;
        }

        private void menu_auto_volume_onexp_Click(object sender, RoutedEventArgs e)
        {
            menu_auto_volume_onexp.IsChecked = true;
            Settings.AutoVolumeMode = Settings.AutoVolumeModes.OnExport;
        }

        private void menu_auto_volume_onimp_Click(object sender, RoutedEventArgs e)
        {
            menu_auto_volume_onimp.IsChecked = true;
            Settings.AutoVolumeMode = Settings.AutoVolumeModes.OnImport;
        }

        private void check_auto_deint_disabled_Click(object sender, RoutedEventArgs e)
        {
            check_auto_deint_disabled.IsChecked = true;
            Settings.AutoDeinterlaceMode = Settings.AutoDeinterlaceModes.Disabled;
        }

        private void check_auto_deint_mpeg_Click(object sender, RoutedEventArgs e)
        {
            check_auto_deint_mpeg.IsChecked = true;
            Settings.AutoDeinterlaceMode = Settings.AutoDeinterlaceModes.MPEGs;
        }

        private void check_auto_deint_all_Click(object sender, RoutedEventArgs e)
        {
            check_auto_deint_all.IsChecked = true;
            Settings.AutoDeinterlaceMode = Settings.AutoDeinterlaceModes.AllFiles;
        }

        private void LoadVideoPresets()
        {
            string format = Format.EnumToString((m != null) ? m.format : Settings.FormatOut);

            combo_vencoding.Items.Clear();
            try
            {
                foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\video"))
                    combo_vencoding.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }
            combo_vencoding.Items.Add("Disabled");
            combo_vencoding.Items.Add("Copy");
        }

        private void SetVideoPresetFromSettings()
        {
            Format.ExportFormats format = (m != null) ? m.format : Settings.FormatOut;

            //прописываем текущий пресет кодирования
            if (combo_vencoding.Items.Contains(Settings.GetVEncodingPreset(format)))
                combo_vencoding.SelectedItem = Settings.GetVEncodingPreset(format);
            else
            {
                //если пресет из настроек не подошёл, грузим первый попавшийся
                combo_vencoding.SelectedItem = "Copy";
                Settings.SetVEncodingPreset(format, "Copy");
            }
        }

        private void LoadAudioPresets()
        {
            string format = Format.EnumToString((m != null) ? m.format : Settings.FormatOut);

            combo_aencoding.Items.Clear();
            try
            {
                foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\audio"))
                    combo_aencoding.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }
            combo_aencoding.Items.Add("Disabled");
            combo_aencoding.Items.Add("Copy");
        }

        private void SetAudioPresetFromSettings()
        {
            Format.ExportFormats format = (m != null) ? m.format : Settings.FormatOut;

            //прописываем текущий пресет кодирования
            if (m != null && m.outaudiostreams.Count == 0)
                combo_aencoding.SelectedItem = "Disabled";
            else if (combo_aencoding.Items.Contains(Settings.GetAEncodingPreset(format)))
                combo_aencoding.SelectedItem = Settings.GetAEncodingPreset(format);
            else
            {
                //если пресет из настроек не подошёл, грузим первый попавшийся
                combo_aencoding.SelectedItem = "Copy";
                Settings.SetAEncodingPreset(format, "Copy");
            }
        }

        private void combo_format_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_format.IsDropDownOpen || combo_format.IsSelectionBoxHighlighted)
            {
                Settings.FormatOut = Format.StringToEnum(combo_format.SelectedItem.ToString());

                if (m != null)
                {
                    m.format = Settings.FormatOut;

                    //загружаем профили
                    if (Settings.FormatOut != Format.ExportFormats.Audio)
                        LoadVideoPresets();
                    LoadAudioPresets();

                    //забиваем-обновляем аудио массивы
                    m = FillAudio(m);

                    //забиваем настройки из профиля
                    if (Settings.FormatOut == Format.ExportFormats.Audio)
                    {
                        m.vencoding = "Disabled";
                        m.outvcodec = "Disabled";
                        m.vpasses.Clear();
                    }
                    else
                    {
                        m.vencoding = Settings.GetVEncodingPreset(m.format);
                        if (!combo_vencoding.Items.Contains(m.vencoding))
                            m.vencoding = "Copy";
                        m.outvcodec = PresetLoader.GetVCodec(m);
                        m.vpasses = PresetLoader.GetVCodecPasses(m);
                    }

                    m = PresetLoader.DecodePresets(m);

                    //перезабиваем специфику формата
                    if (Settings.FormatOut != Format.ExportFormats.Audio)
                    {
                        m = Format.GetOutInterlace(m);
                        m = Format.GetValidFramerate(m);
                        m = Calculate.UpdateOutFrames(m);

                        //if (m.format != Format.ExportFormats.Mkv && m.format != Format.ExportFormats.Avi && m.format != Format.ExportFormats.Mov && m.format != Format.ExportFormats.Mp4 && m.format != Format.ExportFormats.TS)
                        {
                            m = Format.GetValidResolution(m);
                            m = Format.GetValidOutAspect(m);
                            m = AspectResolution.FixAspectDifference(m);
                        }

                        //принудительный фикс цвета для DVD
                        if (Settings.AutoColorMatrix &&
                            Calculate.IsMPEG(m.infilepath) &&
                            m.iscolormatrix == false &&
                            m.invcodecshort == "MPEG2")
                        {
                            m.iscolormatrix = true;
                            if (combo_sbc.Items.Contains("MPEG2Fix") &&
                                Settings.SBC == "Disabled")
                                combo_sbc.SelectedItem = "MPEG2Fix";
                        }
                    }

                    //перезабиваем настройки форматов
                    m.split = Format.GetSplitting(m.format);
                    m.outfilesize = Calculate.GetEncodingSize(m);
                    m.dontmuxstreams = Format.GetMultiplexing(m.format);
                    if (m.outfilepath != null) m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + Format.GetValidExtension(m);

                    //создаём новый AviSynth скрипт
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);

                    //механизм обхода ошибок SSRC
                    if (m.sampleratemodifer == AviSynthScripting.SamplerateModifers.SSRC &&
                        m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
                    {
                        AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                        AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        if (instream.samplerate != outstream.samplerate && outstream.samplerate != null &&
                            Calculate.CheckScriptErrors(m) == "SSRC: could not resample between the two samplerates.")
                        {
                            m.sampleratemodifer = AviSynthScripting.SamplerateModifers.ResampleAudio;
                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        }
                    }

                    //загружаем обновлённый скрипт
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);

                    //проверяем можно ли копировать данный формат
                    if (m.vencoding == "Copy")
                    {
                        string CopyProblems = Format.ValidateCopyVideo(m);
                        if (CopyProblems != null)
                        {
                            Message mess = new Message(this);
                            mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                        }
                    }
                }
                else
                {                   
                    //загружаем профили
                    if (Settings.FormatOut != Format.ExportFormats.Audio)
                        LoadVideoPresets();
                    LoadAudioPresets();
                }
                
                if (Settings.FormatOut != Format.ExportFormats.Audio)
                    SetVideoPresetFromSettings();
                SetAudioPresetFromSettings();

                //для звуковых заданий
                if (Settings.FormatOut == Format.ExportFormats.Audio)
                {
                    combo_vencoding.SelectedItem = "Disabled";
                    combo_vencoding.IsEnabled = false;
                    button_edit_vencoding.IsEnabled = false;
                    combo_sbc.IsEnabled = false;
                    button_edit_sbc.IsEnabled = false;
                }
                else
                {
                    combo_vencoding.IsEnabled = true;
                    button_edit_vencoding.IsEnabled = true;
                    combo_sbc.IsEnabled = true;
                    button_edit_sbc.IsEnabled = true;
                }

                if (m != null)
                {
                    //обновляем дочерние окна
                    ReloadChildWindows();
                    ValidateTrimAndCopy(m);
                }
            }
        }

        private void combo_filtering_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_filtering.IsDropDownOpen || combo_filtering.IsSelectionBoxHighlighted)
            {
                if (combo_filtering.SelectedItem != null)
                {
                    Settings.Filtering = combo_filtering.SelectedItem.ToString();

                    if (m != null)
                    {
                        m.filtering = combo_filtering.SelectedItem.ToString();

                        //создаём новый AviSynth скрипт
                        m.filtering_changed = true;
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        m.filtering_changed = false;

                        //загружаем обновлённый скрипт
                        LoadVideo(MediaLoad.update);
                    }
                }
            }
        }

        private void combo_sbc_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_sbc.IsDropDownOpen || combo_sbc.IsSelectionBoxHighlighted)
            {
                if (combo_sbc.SelectedItem != null)
                {
                    Settings.SBC = combo_sbc.SelectedItem.ToString();

                    if (m != null)
                    {
                        m.sbc = combo_sbc.SelectedItem.ToString();

                        //дешифруем парметры из профиля
                        m = ColorCorrection.DecodeProfile(m);

                        //создаём новый AviSynth скрипт
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);

                        //загружаем обновлённый скрипт
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
            }
        }

        private void combo_aencoding_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_aencoding.IsDropDownOpen || combo_aencoding.IsSelectionBoxHighlighted)
            {
                if (combo_aencoding.SelectedItem != null)
                {
                    Format.ExportFormats format;
                    if (m == null) format = Settings.FormatOut;
                    else format = m.format;

                    if (format == Format.ExportFormats.Audio &&
                        combo_aencoding.SelectedItem.ToString() == "Disabled")
                    {
                        if (m!= null && m.inaudiostreams.Count > 0) combo_aencoding.SelectedItem = "MP3 CBR 128k";
                        return;
                    }

                    if (combo_aencoding.SelectedItem.ToString() != "Disabled")
                        Settings.SetAEncodingPreset(format, combo_aencoding.SelectedItem.ToString());

                    if (m != null)
                    {
                        if (m.inaudiostreams.Count == 0 &&
                            combo_aencoding.SelectedItem.ToString() != "Disabled")
                        {
                            combo_aencoding.SelectedItem = "Disabled";
                            return;
                        }

                        //Запоминаем старый кодер
                        string old_codec = (m.outaudiostreams.Count > 0) ? 
                            ((AudioStream)m.outaudiostreams[m.outaudiostream]).codec : "";

                        //запрещаем или разрешаем звук
                        if (m.outaudiostreams.Count > 0 &&
                            combo_aencoding.SelectedItem.ToString() == "Disabled")
                        {
                            m.outaudiostreams.Clear();
                            old_codec = ".";
                        }
                        else
                        {
                            //забиваем-обновляем аудио массивы
                            m = FillAudio(m);
                        }

                        //Пересоздаем скрипт при изменении кодера. combo aencoding selection
                        if (m.outaudiostreams.Count > 0 && ((AudioStream)m.outaudiostreams[m.outaudiostream]).codec != old_codec ||
                            old_codec == ".")
                        {
                            //Меняем расширение
                            if (m.format == Format.ExportFormats.Audio && m.outfilepath != null)
                                m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + Format.GetValidExtension(m);
                            
                            string old_script = m.script;
                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                            if (old_script != m.script) LoadVideo(MediaLoad.update);
                        }

                        m = PresetLoader.DecodePresets(m);

                        //проверка на размер
                        m.outfilesize = Calculate.GetEncodingSize(m);

                        //загружаем обновлённый скрипт
                        UpdateTaskMassive(m);

                        //обновляем дочерние окна
                        ReloadChildWindows();
                        ValidateTrimAndCopy(m);
                    }
                }
            }
        }

        private void combo_vencoding_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_vencoding.IsDropDownOpen || combo_vencoding.IsSelectionBoxHighlighted)
            {
                if (combo_vencoding.SelectedItem != null)
                {
                    //переводим программу в режим кодирования звука
                    if (combo_vencoding.SelectedItem.ToString() == "Disabled")
                    {
                        combo_format.SelectedItem = Format.EnumToString(Format.ExportFormats.Audio);
                        Settings.FormatOut = Format.ExportFormats.Audio;
                        combo_vencoding.IsEnabled = false;
                        button_edit_vencoding.IsEnabled = false;
                        combo_sbc.IsEnabled = false;
                        button_edit_sbc.IsEnabled = false;

                        if (m != null) m.format = Format.ExportFormats.Audio;
                    }

                    Format.ExportFormats format;
                    if (m == null) format = Settings.FormatOut;
                    else format = m.format;

                    Settings.SetVEncodingPreset(format, combo_vencoding.SelectedItem.ToString());

                    if (m != null)
                    {
                        //забиваем настройки из профиля
                        m.vencoding = combo_vencoding.SelectedItem.ToString();

                        if (m.vencoding != "Disabled")
                        {
                            m.outvcodec = PresetLoader.GetVCodec(m);
                            m.vpasses = PresetLoader.GetVCodecPasses(m);
                        }
                        else
                        {
                            m.outvcodec = "Disabled";
                            m.vpasses.Clear();
                        }
                        m = PresetLoader.DecodePresets(m);

                        if (m.outvcodec == "Disabled") m.outvbitrate = 0;

                        //проверка на размер
                        m.outfilesize = Calculate.GetEncodingSize(m);

                        UpdateTaskMassive(m);

                        //проверяем можно ли копировать данный формат
                        if (m.vencoding == "Copy")
                        {
                            string CopyProblems = Format.ValidateCopyVideo(m);
                            if (CopyProblems != null)
                            {
                                Message mess = new Message(this);
                                mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                    " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                            }
                            ValidateTrimAndCopy(m);
                        }
                    }
                    
                    //обновляем дочерние окна
                    ReloadChildWindows();
                }
            }
        }

        private void VideoEncodingSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            else if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                VideoEncoding enc = new VideoEncoding(m, this);
                m = enc.m.Clone();
                LoadVideoPresets();

                //защита от удаления профиля
                if (!combo_vencoding.Items.Contains(m.vencoding))
                    m.vencoding = "Copy";

                combo_vencoding.SelectedItem = m.vencoding;

                Settings.SetVEncodingPreset(m.format, m.vencoding);

                //проверка на размер
                //m.outfilesize = Calculate.GetEncodingSize(m);

                UpdateTaskMassive(m);
                ValidateTrimAndCopy(m);
            }
        }

        private void AudioEncodingSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (m.inaudiostreams.Count == 0 || m.outaudiostreams.Count == 0)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have audio streams!"), Languages.Translate("Error"));
            }
            else
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                //Запоминаем старый кодер
                string old_codec = outstream.codec;

                AudioEncoding enc = new AudioEncoding(m);
                m = enc.m.Clone();
                LoadAudioPresets();

                //защита от удаления профиля
                if (!combo_aencoding.Items.Contains(outstream.encoding))
                    outstream.encoding = "Copy";

                combo_aencoding.SelectedItem = outstream.encoding;

                if (combo_aencoding.SelectedItem.ToString() != "Disabled")
                {
                    Settings.SetAEncodingPreset(m.format, outstream.encoding);
                }
                else
                {
                    m.outaudiostreams.Clear();
                    old_codec = ".";
                }

                //Пересоздаем скрипт при изменении кодера. audio encoding settings
                if (m.outaudiostreams.Count > 0 && ((AudioStream)m.outaudiostreams[m.outaudiostream]).codec != old_codec ||
                    old_codec == ".")
                {
                    //Меняем расширение
                    if (m.format == Format.ExportFormats.Audio && m.outfilepath != null)
                        m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + Format.GetValidExtension(m);
                    
                    string old_script = m.script;
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    if (old_script != m.script) LoadVideo(MediaLoad.update);
                }

                //проверка на размер
                //m.outfilesize = Calculate.GetEncodingSize(m);

                UpdateTaskMassive(m);
                ValidateTrimAndCopy(m);
            }
        }

        private void SafeDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void SafeDirDelete(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void grid_player_window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void grid_player_window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;

            if (!IsDragOpening)
            {
                IsDragOpening = true;
                drop_data = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (drop_data != null) new Thread(new ThreadStart(this.DragOpen)).Start();
                else IsDragOpening = false;
                this.Activate();
            }
        }

        internal delegate void DragOpenDelegate();
        private void DragOpen()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DragOpenDelegate(DragOpen));
            else
            {
                if (drop_data.Length == 1) //Обычное открытие
                {
                    //Копирование exe-файлов
                    if (Path.GetFileName(drop_data[0]).ToLower().EndsWith(".exe"))
                    {
                        string file_c = "", path_c = "", file_d = Path.GetFileName(drop_data[0]).ToLower();
                        if (file_d == "x264.exe") { path_c = Calculate.StartupPath + "\\apps\\x264\\"; file_c = "x264.exe"; }
                        else if (file_d == "x264_64.exe") { path_c = Calculate.StartupPath + "\\apps\\x264\\"; file_c = "x264_64.exe"; }
                        else if (file_d == "ffmpeg.exe") { path_c = Calculate.StartupPath + "\\apps\\ffmpeg\\"; file_c = "ffmpeg.exe"; }

                        if (!string.IsNullOrEmpty(file_c))
                        {
                            try
                            {
                                File.Copy(drop_data[0], path_c + file_c, true);
                                new Message(this).ShowMessage("The file \"" + file_c + "\" was successfully copied to \r\n\"" + path_c + "\"", Languages.Translate("Complete"));
                            }
                            catch (Exception ex)
                            {
                                new Message(this).ShowMessage("Copying file \"" + file_c + "\" to \"" + path_c + "\" thrown an Error:\r\n" + ex.Message, Languages.Translate("Error"));
                            }
                        }
                        else
                            new Message(this).ShowMessage("I don`t know what to do with \"" + Path.GetFileName(drop_data[0]) + "\"!", Languages.Translate("Error"));
                    }
                    else
                    {
                        Massive x = new Massive();
                        x.infilepath = drop_data[0];
                        x.infileslist = new string[] { drop_data[0] };
                        x.owner = this;

                        //ищем соседние файлы и спрашиваем добавить ли их к заданию при нахождении таковых
                        if (Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly && Calculate.IsValidVOBName(x.infilepath) ||
                            Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled)
                            x = OpenDialogs.GetFriendFilesList(x);
                        if (x != null) action_open(x);
                    }
                }
                else if (drop_data.Length > 1) //Мульти-открытие
                {
                    PauseAfterFirst = Settings.BatchPause;
                    if (!PauseAfterFirst)
                    {
                        OpenDialogs.owner = this;
                        path_to_save = OpenDialogs.SaveFolder();
                        if (path_to_save == null) { IsDragOpening = false; return; }
                    }
                    MultiOpen(drop_data);
                }
                IsDragOpening = false;
            }
        }

        private void list_tasks_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsInsertAction) return;

            if (list_tasks.SelectedIndex != -1)
            {
                Task task = (Task)list_tasks.Items[list_tasks.SelectedIndex];

                string script = (m != null) ? m.script : "";
                m = task.Mass.Clone();

                LoadVideoPresets();

                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    LoadAudioPresets();
                    combo_aencoding.SelectedItem = outstream.encoding;
                }
                else
                {
                    combo_aencoding.SelectedItem = "Disabled";
                }

                combo_format.SelectedItem = Format.EnumToString(m.format);
                combo_sbc.SelectedItem = m.sbc;
                combo_filtering.SelectedItem = m.filtering;
                combo_vencoding.SelectedItem = m.vencoding;


                //запоминаем выделенное задание
                //OldSelectedIndex = list_tasks.SelectedIndex;
                //IsTaskSelection = true;

                if (script != m.script)
                    LoadVideo(MediaLoad.load);

                //обновляем дочерние окна
                ReloadChildWindows();
            }
            else
            {
                if (m != null)
                    m.key = Settings.Key;
            }
        }

        private void cmenu_move_task_Click(object sender, RoutedEventArgs e)
        {
            if (list_tasks.SelectedIndex != -1 && list_tasks.SelectedItem != null && list_tasks.Items.Count > 1)
            {
                int index = 0;
                if (((MenuItem)sender).Name == "cmenu_up") index = list_tasks.SelectedIndex - 1;
                else if (((MenuItem)sender).Name == "cmenu_down") index = list_tasks.SelectedIndex + 1;
                else if (((MenuItem)sender).Name == "cmenu_last") index = list_tasks.Items.Count - 1;

                if (index >= 0 && index < list_tasks.Items.Count && list_tasks.SelectedIndex != index)
                {
                    IsInsertAction = true;
                    Task task = (Task)list_tasks.SelectedItem;
                    list_tasks.Items.Remove(task);
                    list_tasks.Items.Insert(index, task);
                    list_tasks.SelectedIndex = index;
                    IsInsertAction = false;
                }
                else cmenu_tasks.IsOpen = false;
            }
            else cmenu_tasks.IsOpen = false;
        }

        private void RemoveSelectedTask()
        {
            if (list_tasks.SelectedItem != null)
            {
                Task task = (Task)list_tasks.SelectedItem;

                if (task.Status == "Encoding")
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("The tasks in encoding process are disabled for removal!"), Languages.Translate("Error"));
                    return;
                }

                list_tasks.Items.Remove(list_tasks.SelectedItem);
                if (outfiles.Contains(task.Mass.outfilepath))
                    outfiles.Remove(task.Mass.outfilepath);

                clear_ff_cache();
            }
        }

        private void AddTask(Massive mass, string status)
        {
            list_tasks.Items.Add(new Task("THM", status, mass));
        }

        public void UpdateTaskStatus(string key, string status)
        {
            int index = 0;
            bool IsTask = false;
            Task task = null;

            foreach (object _task in list_tasks.Items)
            {
                task = (Task)_task;
                if (task.Id == key)
                {
                    IsTask = true;
                    break;
                }
                index++;
            }

            if (IsTask)
            {
                //добавлем задание в лок
                if (task.Status == "Encoded" && status == "Waiting")
                    outfiles.Add(task.Mass.outfilepath);

                IsInsertAction = true;
                list_tasks.Items.RemoveAt(index);
                task.Status = status;
                list_tasks.Items.Insert(index, task);
                IsInsertAction = false;
            }
        }

        private void UpdateTaskMassive(Massive mass)
        {
            //Перезапись (обновление) задания, если оно не в процессе кодирования
            int task_index = 0;
            bool IsTask = false;
            Task task = null;

            foreach (object _task in list_tasks.Items)
            {
                task = (Task)_task;
                if (task.Id == mass.key && task.Status != "Encoding")
                {
                    IsTask = true;
                    break;
                }
                task_index++;
            }

            if (IsTask)
            {
                IsInsertAction = true;
                list_tasks.Items.RemoveAt(task_index);
                list_tasks.Items.Insert(task_index, new Task(task.THM, "Waiting", mass.Clone()));
                list_tasks.SelectedIndex = task_index;
                IsInsertAction = false;
            }
        }

        public void RemoveTask(string key)
        {
            object task_for_delete = null;
            foreach (object _task in list_tasks.Items)
            {
                Task task = (Task)_task;
                if (task.Id == key)
                    task_for_delete = _task;
            }

            if (task_for_delete != null)
                list_tasks.Items.Remove(task_for_delete);
        }

        public bool EncodeNextTask()
        {
            bool all_clear = true;
            if (list_tasks.Items.Count != 0)
            {
                list_tasks.UnselectAll();

                bool IsWaiting = false;
                Task task = null;

                foreach (object _task in list_tasks.Items)
                {
                    task = (Task)_task;
                    if (task.Status == "Waiting")
                    {
                        IsWaiting = true;
                        all_clear = false;
                        break;
                    }
                    else if (task.Status == "Encoding" || task.Status == "Errors")
                    {
                        all_clear = false;
                    }
                }

                if (IsWaiting)
                {
                    UpdateTaskStatus(task.Id, "Encoding");
                    action_encode(task.Mass.Clone());
                }
            }

            return all_clear;
        }

        private void button_encode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null && PauseAfterFirst){ action_save(m.Clone()); return; }
            if (m != null || list_tasks.Items.Count > 0)
            {
                bool IsEncoding = false;
                bool IsWaiting = false;
                foreach (object _task in list_tasks.Items)
                {
                    Task task = (Task)_task;
                    if (task.Status == "Encoding") IsEncoding = true;
                    if (task.Status == "Waiting") IsWaiting = true;
                }

                if (IsEncoding && IsWaiting)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("Do you want to run one more encoding thread?"), Languages.Translate("Question"), Message.MessageStyle.YesNo);
                    if (mes.result == Message.Result.No)
                        return;
                }

                if (!IsWaiting) action_save(m.Clone());
                EncodeNextTask();
            }
        }

        private void list_tasks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (list_tasks.SelectedItem != null)
            {
                Task task = (Task)list_tasks.SelectedItem;
                if (task.Status == "Encoded")
                {
                    //добавлем задание в лок
                    outfiles.Add(task.Mass.outfilepath);
                    //обновляем список
                    UpdateTaskStatus(task.Id, "Waiting");
                }
            }
        }

        private void cmenu_deselect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_tasks.HasItems)
                list_tasks.UnselectAll();
        }

        private void cmenu_reset_status_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_tasks.SelectedItem != null)
            {
                Task task = (Task)list_tasks.SelectedItem;
                if (task.Status != "Encoding")
                    UpdateTaskStatus(task.Id, "Waiting");
            }
        }

        private void cmenu_delete_task_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_tasks.HasItems)
                RemoveSelectedTask();
        }

        private void cmenu_delete_encoded_tasks_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_tasks.HasItems)
            {
                ArrayList ready = new ArrayList();
                foreach (object _task in list_tasks.Items)
                {
                    Task task = (Task)_task;
                    if (task.Status == "Encoded")
                        ready.Add(_task);
                }

                foreach (object _task in ready)
                    list_tasks.Items.Remove(_task);

                clear_ff_cache();
            }
        }

        private void cmenu_delete_all_tasks_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_tasks.HasItems)
            {
                ArrayList fordelete = new ArrayList();
                foreach (object _task in list_tasks.Items)
                {
                    Task task = (Task)_task;
                    if (task.Status != "Encoding")
                    {
                        fordelete.Add(_task);
                        if (outfiles.Contains(task.Mass.outfilepath))
                            outfiles.Remove(task.Mass.outfilepath);
                    }
                }

                foreach (object _task in fordelete)
                    list_tasks.Items.Remove(_task);

                clear_ff_cache();
            }
        }

        private void cmenu_is_always_delete_encoded_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoDeleteTasks = cmenu_is_always_delete_encoded.IsChecked;
        }

        private void ColorCorrection_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (m.outvcodec == "Copy")
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
            }
            else if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                bool old_colormatrix = m.iscolormatrix;
                double old_saturation = m.saturation;
                double old_contrast = m.contrast;
                int old_brightness = m.brightness;
                int old_hue = m.hue;

                ColorCorrection col = new ColorCorrection(m, this);
                m = col.m.Clone();

                //загружаем списки профилей цвето коррекции
                combo_sbc.Items.Clear();
                combo_sbc.Items.Add("Disabled");
                try
                {
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\sbc"))
                        combo_sbc.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
                catch { }

                //прописываем текущий профиль
                if (combo_sbc.Items.Contains(m.sbc))
                    combo_sbc.SelectedItem = m.sbc;
                else
                {
                    //Видимо профиль был удален, сбрасываем всё на дефолты
                    combo_sbc.SelectedItem = m.sbc = "Disabled";
                    m = ColorCorrection.DecodeProfile(m);
                }

                Settings.SBC = m.sbc; //сохраняет название текущего профиля в реестре

                //обновление при необходимости
                if (old_colormatrix != m.iscolormatrix ||
                    old_saturation != m.saturation ||
                    old_contrast != m.contrast ||
                    old_brightness != m.brightness ||
                    old_hue != m.hue)
                {
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
                }
            }
        }

        private void menu_save_wav_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (m.inaudiostreams.Count == 0)
            {
                new Message(this).ShowMessage(Languages.Translate("File doesn`t have audio streams!"), Languages.Translate("Error"));
            }
            else action_save_wav();
        }        

        private void action_save_wav()
        {
            System.Windows.Forms.SaveFileDialog o = new System.Windows.Forms.SaveFileDialog();
            o.Filter = Languages.Translate("Windows PCM (*.wav)") + "|*.wav";
            o.Title = Languages.Translate("Select output file") + ":";
            o.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + ".wav";

            if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (o.FileName == m.infilepath)
                {
                    ErrorException(Languages.Translate("Select another name for output file!"));
                    return;
                }
                Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.DecodeToWAV, o.FileName);
                if (dem.IsErrors)
                {
                    new Message(this).ShowMessage(dem.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                }
            }
        }

        private void menu_demux_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (m.inaudiostreams.Count == 0)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have audio streams!"), Languages.Translate("Error"));
            }
            else
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                System.Windows.Forms.SaveFileDialog o = new System.Windows.Forms.SaveFileDialog();
                o.Filter = instream.codecshort + " " + Languages.Translate("files") + "|*." + instream.codecshort.ToLower();
                o.Title = Languages.Translate("Select output file") + ":";
                o.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + Format.GetValidRAWAudioEXT(instream.codecshort);

                if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (o.FileName == m.infilepath)
                    {
                        ErrorException(Languages.Translate("Select another name for output file!"));
                        return;
                    }
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, o.FileName);
                    if (dem.IsErrors)
                    {
                        new Message(this).ShowMessage(dem.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                    }
                }
            }
        }

        private void menu_demux_video_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                string ext = Format.GetValidRAWVideoEXT(m);

                System.Windows.Forms.SaveFileDialog o = new System.Windows.Forms.SaveFileDialog();
                o.Filter = ext.ToUpper() + " " + Languages.Translate("Video").ToLower() + Languages.Translate("files") + "|*." + ext;
                o.Title = Languages.Translate("Select output file") + ":";
                o.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + "." + ext;

                if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (o.FileName == m.infilepath)
                    {
                        ErrorException(Languages.Translate("Select another name for output file!"));
                        return;
                    }
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractVideo, o.FileName);
                    if (dem.IsErrors)
                    {
                        new Message(this).ShowMessage(dem.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                    }
                }
            }
        }

        private void menu_info_media_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaInfo media = new MediaInfo(((m != null) ? m.infilepath : null), MediaInfo.InfoMode.MediaInfo, this);
        }

        private void menu_play_in_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                string path = (sender == menu_playinwmp) ? Settings.WMP_Path : (sender == menu_playinmpc) ? Settings.MPC_Path : Settings.WPF_Path;
                if (!File.Exists(path)) throw new Exception(Languages.Translate("Can`t find file") + ((path != "") ? (": " + Path.GetFileName(path)) : "!"));

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = path;
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                if (m != null)
                {
                    if (!File.Exists(m.scriptpath)) AviSynthScripting.WriteScriptToFile(m);
                    info.Arguments = Settings.TempPath + "\\preview.avs";
                }
                Process pr = new Process();
                pr.StartInfo = info;
                pr.Start();
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void edit_player_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Forms.OpenFileDialog s = new System.Windows.Forms.OpenFileDialog();
                string path = (sender == edit_wmp) ? Settings.WMP_Path : (sender == edit_mpc) ? Settings.MPC_Path : Settings.WPF_Path;
                s.InitialDirectory = (path != "" && Directory.Exists(Path.GetDirectoryName(path))) ? Path.GetDirectoryName(path) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                s.FileName = File.Exists(path) ? Path.GetFileName(path) : "";
                s.Title = Languages.Translate("Select executable file") + ":";
                s.Filter = "EXE " + Languages.Translate("files") + "|*.exe|" + Languages.Translate("All files") + "|*.*";
                if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (sender == edit_wmp) Settings.WMP_Path = s.FileName;
                    else if (sender == edit_mpc) Settings.MPC_Path = s.FileName;
                    else Settings.WPF_Path = s.FileName;
                }
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void list_tasks_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) //keyup
        {
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                if (list_tasks.HasItems)
                    RemoveSelectedTask();
            }
        }

        private void menu_autocrop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.outvcodec == "Copy")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                }
                else
                {
                    Autocrop acrop = new Autocrop(m, this);
                    if (acrop.m == null) return;
                    m = acrop.m.Clone();

                    //подправляем входной аспект
                    m = AspectResolution.FixInputAspect(m);

                    m = Format.GetValidResolution(m);
                    m = Format.GetValidOutAspect(m);
                    m = AspectResolution.FixAspectDifference(m);

                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
                }
            }
        }

        public void SwitchToFullScreen()
        {
            if (this.IsAudioOnly || (this.graphBuilder == null && script_box.Visibility != Visibility.Visible && Pic.Visibility != Visibility.Visible))
                if (!IsFullScreen) return; //Если файл был закрыт при фуллскрине, продолжаем чтоб вернуть нормальный размер окна

            //если не Фуллскрин, то делаем Фуллскрин
            if (!IsFullScreen)
            {
                this.IsFullScreen = true;
                oldstate = this.WindowState;
                this.grid_tasks.Visibility = Visibility.Collapsed;
                this.grid_menu.Visibility = Visibility.Collapsed;
                this.grid_left_panel.Visibility = Visibility.Collapsed;
                this.splitter_tasks_preview.Visibility = Visibility.Collapsed;
                this.grid_player_info.Visibility = Visibility.Collapsed;
                this.grid_top.Visibility = Visibility.Collapsed;
                this.WindowStyle = System.Windows.WindowStyle.None;  //стиль окна (без стиля)
                this.WindowState = System.Windows.WindowState.Maximized; //размер окна (максимальный)
                this.grid_player_buttons.Margin = new Thickness(0, 0, 0, 0); //Убрать отступы для панели управления плейера
                oldbrush = this.LayoutRoot.Background;
                oldmargin = this.grid_player_window.Margin;
                this.LayoutRoot.Background = Brushes.Black;
                Grid.SetRow(this.grid_player_window, 0);//
                Grid.SetRowSpan(this.grid_player_window, 2);//
                this.grid_player_window.Margin = new Thickness(0, 0, 0, 0);//

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) MoveVideoWindow();
                this.VideoElement.Margin = script_box.Margin = Pic.Margin = new Thickness(0, 0, 0, 38);
            }
            else
            {
                //Выход из Фуллскрина
                Grid.SetRow(this.grid_player_window, 1);
                Grid.SetRowSpan(this.grid_player_window, 1);
                this.grid_player_window.Margin = oldmargin;
                this.grid_tasks.Visibility = Visibility.Visible;
                this.grid_menu.Visibility = Visibility.Visible;
                this.grid_left_panel.Visibility = Visibility.Visible;
                this.splitter_tasks_preview.Visibility = Visibility.Visible;
                this.grid_player_buttons.Visibility = Visibility.Visible;
                this.grid_player_window.Visibility = Visibility.Visible;
                this.grid_player_info.Visibility = Visibility.Visible;
                this.WindowState = oldstate; //размер окна (сохранённый)
                this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                this.grid_top.Visibility = Visibility.Visible;
                this.LayoutRoot.Background = oldbrush;
                this.grid_player_buttons.Margin = new Thickness(195.856, 0, 0, 0); //Установить дефолтные отступы для панели управления плейера              
                this.IsFullScreen = false;

                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) MoveVideoWindow();
                this.VideoElement.Margin = script_box.Margin = Pic.Margin = new Thickness(8, 56, 8, 8);
            }
            slider_pos.Focus();
        }

        private void CloseClip()
        {
            if (this.graphBuilder != null && this.VideoElement.Source == null)
            {
                int hr = 0;

                // Stop media playback
                if (this.mediaControl != null)
                    hr = this.mediaControl.Stop();

                // Free DirectShow interfaces
                CloseInterfaces();

                this.IsAudioOnly = true; //Перенесено сюда

                // No current media state
                if (mediaload != MediaLoad.update)
                    this.currentState = PlayState.Init;
            }
            else if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                VideoElement.Close();
                VideoElement.Source = null;
                VideoElement.Visibility = Visibility.Collapsed;
                if (mediaload != MediaLoad.update)
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
            else if (script_box.Visibility == Visibility.Visible)
            {
                script_box.Clear();
                script_box.Visibility = Visibility.Collapsed;
                this.currentState = PlayState.Init;
            }
            else if (Pic.Visibility == Visibility.Visible)
            {
                Pic.Visibility = Visibility.Collapsed;
                pic_duration = TimeSpan.Zero;
                pic_total_frames = 0;
                this.currentState = PlayState.Init;
            }

            //update titles
            textbox_name.Text = textbox_frame.Text = "";
            textbox_time.Text = textbox_duration.Text = "00:00:00";
            progress_top.Width = slider_pos.Value = 0.0;
        }

        private void CloseInterfaces()
        {
            int hr = 0;
            try
            {
                lock (this)
                {
                    // Relinquish ownership (IMPORTANT!) after hiding video window
                    if (!this.IsAudioOnly && this.videoWindow != null)
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

        private void ShowWithPictureView(string script, int frame)
        {
            //Картинка "Error..." по-дефолту
            Pic.Source = new BitmapImage(new Uri(@"../pictures/vc_background.png", UriKind.RelativeOrAbsolute));

            bool has_video = true;
            string avs_error = null;
            string exception = null;
            ImageSource picture = null;
            System.Drawing.Bitmap bmp = null;
            System.Drawing.Graphics g = null;
            IntPtr hObject = IntPtr.Zero;
            AviSynthReader reader = new AviSynthReader();

            try
            {
                //Сохраняем аспект (только в сторону увеличения разрешения)
                int new_w = Convert.ToInt32(m.outresh * m.outaspect), new_h = m.outresh;
                if (new_w < m.outresw)
                {
                    new_w = m.outresw;
                    new_h = Convert.ToInt32(m.outresw / m.outaspect);
                }

                try
                {
                    reader.ParseScript(script);
                    if (reader.FrameCount == 0 || reader.Width == 0 || reader.Height == 0)
                    {
                        has_video = false;
                        return;
                    }

                    //Ограничиваемся имеющимся кол-вом кадров
                    frame = (frame > reader.FrameCount) ? reader.FrameCount : frame;
                }
                catch (AviSynthException ex)
                {
                    avs_error = ex.Message;
                }

                bmp = new System.Drawing.Bitmap(new_w, new_h);
                g = System.Drawing.Graphics.FromImage(bmp);
                g.Clear(System.Drawing.Color.Black);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic; //HighQualityBicubic слишком сглаживает
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                if (avs_error != null)
                {
                    //Выводим текст ошибки Ависинта
                    System.Drawing.SizeF text_size;
                    System.Drawing.Font font = null;
                    float left = 0, top = 0, font_size = 100;
                    while (true)
                    {
                        //Уменьшаем размер шрифта, пока текст не впишется в кадр
                        font = new System.Drawing.Font("Arial", font_size, System.Drawing.FontStyle.Bold);
                        text_size = g.MeasureString(avs_error, font);

                        left = (new_w - text_size.Width + font_size) / 2;
                        top = (new_h - text_size.Height) / 2;

                        if ((left < 0 || top < 0) && font_size > 2) font_size -= 1;
                        else break;
                    }

                    g.DrawString(avs_error, font, System.Drawing.Brushes.Red, left, top, System.Drawing.StringFormat.GenericTypographic);
                    font.Dispose();
                    
                    total_frames = "0";
                }
                else
                {
                    g.DrawImage(reader.ReadFrameBitmap(frame), 0, 0, new_w, new_h);

                    //Обновляем счетчик кадров                                   //Сохраняем:
                    pic_frame = frame;                                           //текущий кадр
                    fps = reader.Framerate;                                      //fps скрипта
                    pic_total_frames = reader.FrameCount;                        //кол-во кадров в скрипте
                    pic_duration = TimeSpan.FromSeconds(pic_total_frames / fps); //продолжительность видео
                    slider_pos.Maximum = pic_duration.TotalSeconds;              //Устанавливаем максимум для ползунка

                    //Текущий кадр и общая продолжительность клипа
                    textbox_frame.Text = frame + "/" + (total_frames = pic_total_frames.ToString());
                    textbox_duration.Text = TimeSpan.Parse(TimeSpan.FromSeconds(reader.FrameCount / fps).ToString().Split('.')[0]).ToString();
                }

                hObject = bmp.GetHbitmap();                                      //Переводим картинку в Source
                picture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hObject, IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                Pic.Source = picture;
            }
            catch (Exception ex)
            {
                //Отрисовка ГУИ тормозится при удалении мусора в finally, оставляя остатки
                //сообщения если оно было выведено тут, поэтому перенесено
                exception = ex.Message;
            }
            finally
            {
                reader.Close();
                if (bmp != null) bmp.Dispose();
                if (g != null) g.Dispose();
                DeleteObject(hObject);
                GC.Collect();

                //Выводим, что получилось
                if (has_video) Pic.Visibility = Visibility.Visible;
                else textbox_frame.Text = "NO VIDEO";

                if (exception != null)
                    ErrorException("PictureView Error: " + exception);
            }
        }

        private void PlayMovieInWindow(string filename)
        {
            int hr = 0;
            this.graphBuilder = (IGraphBuilder)new FilterGraph();

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
            basicAudio.put_Volume(VolumeSet); //Ввод в ДиректШоу значения VolumeSet для установки громкости

            // Is this an audio-only file (no video component)?
            CheckVisibility();

            // Have the graph signal event via window callbacks for performance
            hr = this.mediaEventEx.SetNotifyWindow(this.source.Handle, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

            if (!this.IsAudioOnly)
            {
                // Setup the video window
                hr = this.videoWindow.put_Owner(this.source.Handle);
                DsError.ThrowExceptionForHR(hr);

                hr = this.videoWindow.put_MessageDrain(this.source.Handle);
                DsError.ThrowExceptionForHR(hr);

                hr = this.videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings
                    | DirectShowLib.WindowStyle.ClipChildren);
                DsError.ThrowExceptionForHR(hr);

                //Ловим ошибку Ависинта
                IsAviSynthError = false;
                if (NaturalDuration.TotalMilliseconds == 10000.0)
                {
                    double time; //Признаки ошибки: duration=10000.0 и fps=24 (округлённо)
                    if (basicVideo.get_AvgTimePerFrame(out time) != 0 || (int)(1 / time) == 24)
                        IsAviSynthError = true;
                }

                MoveVideoWindow();
                GetFrameStepInterface();
            }
            else 
            {
                //Ловим ошибку Ависинта 2 (когда нет видео окна)
                IsAviSynthError = (NaturalDuration.TotalMilliseconds == 10000.0);
            }

            if (mediaload == MediaLoad.update) //Перенесено из HandleGraphEvent, теперь позиция устанавливается до начала воспроизведения, т.е. за один заход, а не за два
            {
                if (NaturalDuration >= oldpos) //Позиционируем только если нужная позиция укладывается в допустимый диапазон
                    mediaPosition.put_CurrentPosition(oldpos.TotalSeconds);
                //else
                //    mediaPosition.put_CurrentPosition(NaturalDuration.TotalSeconds); //Ограничиваем позицию длиной клипа
            }

            // Run the graph to play the media file
            if (currentState == PlayState.Running)
            {
                //Продолжение воспроизведения, если статус до обновления был Running
                DsError.ThrowExceptionForHR(this.mediaControl.Run());
                SetPauseIcon();
            }
            else
            {
                //Запуск с паузы, если была пауза или это новое открытие файла
                DsError.ThrowExceptionForHR(this.mediaControl.Pause());
                this.currentState = PlayState.Paused;
                SetPlayIcon();
            }
        }

        private void MoveVideoWindow()
        {
            //Track the movement of the container window and resize as needed
            if (this.videoWindow != null)
            {
                double left = 0, top = 0, w = 0, h = 0, aspect = m.outaspect;
                if (IsAviSynthError)
                {
                    int w_err, h_err;
                    DsError.ThrowExceptionForHR(basicVideo.get_VideoWidth(out w_err));
                    DsError.ThrowExceptionForHR(basicVideo.get_VideoHeight(out h_err));
                    aspect = ((double)w_err / (double)h_err);
                }

                if (!IsFullScreen)
                {
                    top = (grid_menu.ActualHeight + grid_top.ActualHeight + splitter_tasks_preview.ActualHeight +
                        grid_tasks.ActualHeight + grid_player_info.ActualHeight + 10);
                    h = (grid_player_window.ActualHeight - grid_player_info.ActualHeight - 12);
                    w = (aspect * h);
                    left = ((grid_left_panel_paper.ActualWidth + (this.grid_player_window.ActualWidth - w) / 2));
                    if (w > progress_back.ActualWidth)
                    {
                        w = progress_back.ActualWidth;
                        h = (w / aspect);
                        left = ((grid_left_panel_paper.ActualWidth + (this.grid_player_window.ActualWidth - w) / 2));
                        top += ((this.grid_player_window.ActualHeight - h) / 2.0) - (grid_player_info.ActualHeight) + 14;
                    }
                }
                else
                {
                    //Для ФуллСкрина
                    h = this.LayoutRoot.ActualHeight - this.grid_player_buttons.ActualHeight; //высота экрана минус высота панели
                    w = (aspect * h);
                    left = ((this.LayoutRoot.ActualWidth - w) / 2);
                    if (w > this.LayoutRoot.ActualWidth)
                    {
                        w = this.LayoutRoot.ActualWidth;
                        h = (w / aspect);
                        left = 0;
                        top = ((this.LayoutRoot.ActualHeight - this.grid_player_buttons.ActualHeight - h) / 2.0);
                    }
                }

                //Масштабируем и вводим
                DsError.ThrowExceptionForHR(this.videoWindow.SetWindowPosition((int)(left * dpi), (int)(top * dpi), (int)(w * dpi), (int)(h * dpi)));
                //Заставляем перерисовать окно
                DsError.ThrowExceptionForHR(this.videoWindow.put_BorderColor(1));
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
                this.IsAudioOnly = true;
                return;
            }
            else
            {
                // Clear the global flag
                this.IsAudioOnly = false;
            }

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
                    this.IsAudioOnly = true;
                }
                else if (hr == unchecked((int)0x80040209)) //VFW_E_NOT_CONNECTED 
                {
                    this.IsAudioOnly = true;
                }
                else
                {
                    this.IsAudioOnly = true;               //Всё-равно видео окна скорее всего не будет
                    DsError.ThrowExceptionForHR(hr);
                }
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

        public void PauseClip()
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

            else if (VideoElement.Source != null)
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

        public void StopClip()
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
                    SetPlayIcon();
                }
            }

            if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                this.currentState = PlayState.Stopped;
                SetPlayIcon();
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
                        if (Mouse.DirectlyOver == null) player_area_cmn.IsOpen = true;
                        break;
                    }
                case 0x020A: //0x020A WM_MOUSEWHEEL
                    if (IsFullScreen) 
                    {
                        if (wParam.ToInt32() > 0) VolumePlus(); else VolumeMinus();
                    }
                    break;
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

                if (evCode == EventCode.ClockChanged)
                {
                    slider_pos.Maximum = NaturalDuration.TotalSeconds;

                    if (mediaload == MediaLoad.load)
                    {
                        if (Settings.AfterImportAction == Settings.AfterImportActions.Play)
                            PauseClip();

                        else if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                            Position = TimeSpan.FromSeconds(NaturalDuration.TotalSeconds / 2.0);
                    }

                    //Считаем fps
                    if (!IsAudioOnly)
                    {
                        double AvgTimePerFrame;
                        hr = basicVideo.get_AvgTimePerFrame(out AvgTimePerFrame);
                        DsError.ThrowExceptionForHR(hr);
                        fps = (1.0 / AvgTimePerFrame);
                    }
                    else
                        fps = Calculate.ConvertStringToDouble(m.outframerate);

                    //Обновляем счетчик кадров
                    total_frames = Convert.ToString(Math.Round(NaturalDuration.TotalSeconds * fps));
                    textbox_frame.Text = Convert.ToString(Math.Round(Position.TotalSeconds * fps)) + "/" + total_frames;

                    //Общая продолжительность клипа (для ДиректШоу)
                    TimeSpan tCode2 = TimeSpan.Parse(TimeSpan.FromSeconds(NaturalDuration.TotalSeconds).ToString().Split('.')[0]);
                    textbox_duration.Text = tCode2.ToString();
                }
            }
        }

        public TimeSpan NaturalDuration
        {
            get
            {
                if (this.graphBuilder != null && this.VideoElement.Source == null)// && mediaPosition != null)///
                {
                    int hr = 0;
                    double duration;
                    hr = mediaPosition.get_Duration(out duration);
                    DsError.ThrowExceptionForHR(hr);
                    return TimeSpan.FromSeconds(duration);
                }
                else if (this.graphBuilder != null && this.VideoElement.Source != null && VideoElement.NaturalDuration.HasTimeSpan)
                {
                    return this.VideoElement.NaturalDuration.TimeSpan;
                }
                else if (Pic.Visibility == Visibility.Visible)
                {
                    return pic_duration;
                }
                else
                    return TimeSpan.Zero;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (this.graphBuilder != null && this.VideoElement.Source == null)// && mediaPosition != null) ///
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
                else if (Pic.Visibility == Visibility.Visible)
                {
                    if (pic_frame != 0 && fps != 0)
                        return TimeSpan.FromSeconds(pic_frame / fps);
                    else
                        return TimeSpan.Zero;
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
                else if (Pic.Visibility == Visibility.Visible)
                {
                    int frame = Convert.ToInt32(value.TotalSeconds * fps);
                    ShowWithPictureView(m.script, frame);
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
                if ((this.graphBuilder != null || Pic.Visibility == Visibility.Visible) && NaturalDuration != TimeSpan.Zero)
                {
                    progress_top.Width = (slider_pos.Value / NaturalDuration.TotalSeconds) * progress_back.ActualWidth;
                    TimeSpan tCode = TimeSpan.Parse(TimeSpan.FromSeconds(slider_pos.Value).ToString().Split('.')[0]);
                    textbox_time.Text = tCode.ToString();
                    
                    Visual visual = Mouse.Captured as Visual;
                    if (visual == null)
                    {
                        slider_pos.Value = Position.TotalSeconds;
                    }
                }
            }
        }

        private void VideoElement_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            if (VideoElement.HasVideo || VideoElement.HasAudio)
            {
                slider_pos.Maximum = VideoElement.NaturalDuration.TimeSpan.TotalSeconds;

                fps = Calculate.ConvertStringToDouble(m.outframerate); //Получаем fps

                //Обновляем счетчик кадров
                total_frames = Convert.ToString(Math.Round(VideoElement.NaturalDuration.TimeSpan.TotalSeconds * fps));
                textbox_frame.Text = Convert.ToString(Math.Round(VideoElement.Position.TotalSeconds * fps)) + "/" + total_frames;

                //Общая продолжительность клипа (для МедиаБридж)
                TimeSpan tCode2 = TimeSpan.Parse(VideoElement.NaturalDuration.ToString().Split('.')[0]);
                textbox_duration.Text = tCode2.ToString();

                if (mediaload == MediaLoad.update)
                {
                    if (NaturalDuration >= oldpos) //Позиционируем только если нужная позиция укладывается в допустимый диапазон
                    {
                        IsAviSynthError = false;
                        Position = oldpos;
                    }
                    else if (NaturalDuration.TotalMilliseconds == 10000.0) IsAviSynthError = true;
                    else IsAviSynthError = false;
                }
                else if (mediaload == MediaLoad.load)
                {
                    if (Settings.AfterImportAction == Settings.AfterImportActions.Play)
                        PauseClip();
                    else if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                        Position = TimeSpan.FromSeconds(NaturalDuration.TotalSeconds / 2.0);
                }
            }
        }

        private void VideoElement_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            StopClip();
        }

        private void PlayWithMediaBridge(string filepath)
        {
            this.filepath = filepath;
            string url = "MediaBridge://MyDataString";
            MediaBridge.MediaBridgeManager.RegisterCallback(url, BridgeCallback);

            VideoElement.Visibility = Visibility.Visible;
            VideoElement.Source = new Uri(url);
            
            if (currentState != PlayState.Running) //Открытие по-дефолту, сразу на паузе
            {
                VideoElement.Play();
                VideoElement.Pause();
                currentState = PlayState.Paused;
            }
            else
            {
                VideoElement.Play(); //Продолжение воспроизведения, если оно было до обновления превью
                currentState = PlayState.Running;
                SetPauseIcon();
            }
        }

        private void BridgeCallback(MediaBridge.MediaBridgeGraphInfo GraphInfo)
        {
            try
            {
                int hr = 0;

                //Convert pointer of filter graph to an object we can use
                graph = (IFilterGraph)Marshal.GetObjectForIUnknown(GraphInfo.FilterGraph);
                graphBuilder = (IGraphBuilder)graph;
               
                //IBaseFilter videoRenderer;
                ///Find WPF renderer.  It's always named the same thing
                //hr = graphBuilder.FindFilterByName("Avalon EVR", out videoRenderer);//
                //hr = graphBuilder.FindFilterByName("Video Renderer", out videoRenderer);//
                //hr = graphBuilder.FindFilterByName("Enhanced Video Renderer", out videoRenderer);                
                //DsError.ThrowExceptionForHR(hr);

                //hr = this.graphBuilder.AddFilter(videoRenderer, "Enhanced Video Renderer");//
                //DsError.ThrowExceptionForHR(hr);

                hr = graphBuilder.RenderFile(filepath, null);
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void menu_detect_interlace_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) return;
            if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.outvcodec == "Copy")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                }
                else
                {
                    SourceDetector sd = new SourceDetector(m);
                    if (sd.m != null)
                    {
                        DeinterlaceType olddeint = m.deinterlace;
                        FieldOrder oldfo = m.fieldOrder;
                        SourceType olditype = m.interlace;
                        m = sd.m.Clone();

                        if (m.deinterlace != olddeint ||
                            m.fieldOrder != oldfo ||
                            m.interlace != olditype)
                        {
                            m = Format.GetOutInterlace(m);
                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                            m = Calculate.UpdateOutFrames(m);
                            LoadVideo(MediaLoad.update);
                            UpdateTaskMassive(m);
                        }
                    }
                }
            }
        }

        private void menu_interlace_Click(object sender, System.Windows.RoutedEventArgs e)       
        {
            if (m == null) return;
            if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.outvcodec == "Copy")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                }
                else
                {
                    Interlace inter = new Interlace(m, this);
                    if (inter.m == null) return;
                    DeinterlaceType olddeint = m.deinterlace;
                    FieldOrder oldfo = m.fieldOrder;
                    SourceType olditype = m.interlace;
                    string oldframerate = m.outframerate;
                    m = inter.m.Clone();

                    if (m.deinterlace != olddeint ||
                        m.fieldOrder != oldfo ||
                        m.interlace != olditype ||
                        m.outframerate != oldframerate)
                    {
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        m = Calculate.UpdateOutFrames(m);
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
            }
        }

        private void menu_ffrebuilder_Click(object sender, RoutedEventArgs e)
        {
            FFRebuilder ff = new FFRebuilder(this);
        }

        private void menu_mkvrebuilder_Click(object sender, RoutedEventArgs e)
        {
            MKVRebuilder mkv = new MKVRebuilder(this);
        }

        private void menu_donate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (Settings.Language == "Russian")
                //    Process.Start("http://ru.winnydows.com/page.php?4");
                //else
                //    Process.Start("http://www.winnydows.com/page.php?4");
                Donate don = new Donate(this);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
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

        private void CloseChildWindows()
        {
            string stitle = Languages.Translate("Editing audio options") + ":";
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow.Title == stitle)
                    ownedWindow.Close();
            }
        }

        private void ReloadChildWindows()
        {
            if (m == null) return;

            string stitle = Languages.Translate("Editing audio options") + ":";
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow.Title == stitle)
                {
                    AudioOptions ao = (AudioOptions)ownedWindow;
                    ao.Reload(m);
                }
            }
        }

        private void AudioOptions_Click(object sender, RoutedEventArgs e)
        {
            //разрешаем только одно окно
            string stitle = Languages.Translate("Editing audio options") + ":";
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow.Title == stitle)
                {
                    ownedWindow.Activate();
                    return;
                }
            }
            if (m != null)
            {
                new AudioOptions(m, this, AudioOptions.AudioOptionsModes.AllOptions);
            }
        }

        private Massive FillAudio(Massive mass)
        {
            //передаём активный трек на выход
            if (mass.inaudiostreams.Count > 0)
            {
                //if (mass.outaudiostreams.Count == 0)
                //{
                AudioStream stream = new AudioStream();
                mass.outaudiostreams.Clear();
                AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];
                if (Settings.ApplyDelay) stream.delay = instream.delay;
                mass.outaudiostreams.Add(stream);
                //}

                AudioStream outstream = (AudioStream)mass.outaudiostreams[mass.outaudiostream];

                //Клонируем звуковые параметры от предыдущего файла
                if (IsBatchOpening && CloneIsEnabled && Settings.BatchCloneAudio)
                {
                    if (m.outaudiostreams.Count > 0)
                    {
                        AudioStream old_outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        outstream.encoding = old_outstream.encoding;
                        outstream.codec = old_outstream.codec;
                        outstream.passes = old_outstream.passes;
                        outstream.samplerate = old_outstream.samplerate;
                        mass.sampleratemodifer = m.sampleratemodifer;
                        outstream.channels = old_outstream.channels;
                        instream.channelconverter = ((AudioStream)m.inaudiostreams[m.inaudiostream]).channelconverter;
                        outstream.bits = old_outstream.bits;
                    }
                    else
                    {
                        //Клонируем Audio = Disabled
                        mass.outaudiostreams.Clear();
                        return mass;
                    }
                }
                else
                {
                    //забиваем аудио настройки
                    outstream.encoding = Settings.GetAEncodingPreset(Settings.FormatOut);
                    outstream.codec = PresetLoader.GetACodec(mass.format, outstream.encoding);
                    outstream.passes = PresetLoader.GetACodecPasses(mass);
                    
                    mass = Format.GetValidSamplerate(mass);

                    //определяем битность
                    mass = Format.GetValidBits(mass);

                    //определяем колличество каналов
                    mass = Format.GetValidChannelsConverter(mass);
                    mass = Format.GetValidChannels(mass);
                }

                if (outstream.codec == "Disabled") outstream.bitrate = 0;

                //проверяем можно ли копировать данный формат
                if (outstream.codec == "Copy")
                {
                    //AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];
                    if (instream.audiopath == null)
                    {
                        string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                        string outpath = Settings.TempPath + "\\" + mass.key + "_" + mass.inaudiostream + outext;
                        outstream.audiopath = outpath;
                    }
                    else
                    {
                        outstream.audiopath = instream.audiopath;
                    }

                    outstream.bitrate = instream.bitrate;

                    string CopyProblems = Format.ValidateCopyAudio(mass);
                    if (CopyProblems != null)
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                            " " + Format.EnumToString(mass.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because audio encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }
                else
                {
                    string aext = Format.GetValidRAWAudioEXT(outstream.codec);
                    outstream.audiopath = Settings.TempPath + "\\" + mass.key + aext;
                }
            }
            return mass;
        }

        private void OpenDVD_Click(object sender, RoutedEventArgs e)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            folder.Description = Languages.Translate("Select drive or folder with VOB files:");
            folder.ShowNewFolderButton = false;

            if (Settings.DVDPath != null && Directory.Exists(Settings.DVDPath))
                folder.SelectedPath = Settings.DVDPath;

            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Height = this.Window.Height + 1; //чтоб убрать остатки от окна выбора директории, вот такой вот способ...
                this.Height = this.Window.Height - 1;
                
                Settings.DVDPath = folder.SelectedPath;
                Massive x = new Massive();
                x.owner = this;
                DVDImport dvd = new DVDImport(x, folder.SelectedPath, dpi);

                if (dvd.m != null)
                    action_open(dvd.m);
            }
        }

        private void menu_save_frame_Click(object sender, RoutedEventArgs e)
        {
            if (m == null) return;
            else if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                AviSynthReader reader = null;
                System.Drawing.Bitmap bmp = null;

                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.SupportMultiDottedExtensions = true;
                    s.DefaultExt = ".png";
                    s.AddExtension = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    s.Filter = "PNG " + Languages.Translate("files") + "|*.png" +
                        "|JPEG " + Languages.Translate("files") + "|*.jpg" + "|BMP " + Languages.Translate("files") + "|*.bmp";

                    int frame = (int)Math.Round(Position.TotalSeconds * fps);
                    s.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + " - [" + frame + "]";

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string ext = Path.GetExtension(s.FileName).ToLower();

                        reader = new AviSynthReader();
                        reader.ParseScript(m.script);

                        bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                        if (ext == ".jpg")
                        {
                            System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                            System.Drawing.Imaging.EncoderParameters encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                            encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);

                            bmp.Save(s.FileName, info[1], encoderParameters);
                        }
                        else if (ext == ".png") bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        else if (ext == ".bmp") bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                }
                catch (Exception ex)
                {
                    ErrorException(ex.Message);
                }
                finally
                {
                    //завершение
                    if (bmp != null) bmp.Dispose();
                    if (reader != null) reader.Close();
                }
            }
        }

        private void menu_savethm_Click(object sender, RoutedEventArgs e)
        {
            if (m == null) return;
            else if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                AviSynthReader reader = null;
                System.Drawing.Bitmap bmp = null;
                System.Drawing.Graphics g = null;

                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.AddExtension = true;
                    //s.SupportMultiDottedExtensions = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    //s.DefaultExt = ".png";

                    if (m.format == Format.ExportFormats.Mp4PSPAVC ||
                        m.format == Format.ExportFormats.Mp4PSPAVCTV ||
                        m.format == Format.ExportFormats.Mp4PSPASP)
                    {
                        s.Filter = "JPEG " + Languages.Translate("files") + "|*.jpg";
                        s.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + ".jpg";
                    }
                    else if (m.format == Format.ExportFormats.PmpAvc)
                    {
                        s.Filter = "PNG " + Languages.Translate("files") + "|*.png";
                        s.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + ".png";
                    }
                    else
                        s.Filter = "JPEG " + Languages.Translate("files") + "|*.jpg" +
                            "|PNG " + Languages.Translate("files") + "|*.png";

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string ext = Path.GetExtension(s.FileName).ToLower();
                        int frame = (int)Math.Round(Position.TotalSeconds * fps);

                        reader = new AviSynthReader();
                        reader.ParseScript(m.script);

                        if (ext == ".png")
                        {
                            if (m.format == Format.ExportFormats.PmpAvc)
                            {
                                bmp = new System.Drawing.Bitmap(144, 80);
                                g = System.Drawing.Graphics.FromImage(bmp);
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(reader.ReadFrameBitmap(frame), 0, 0, 144, 80);
                                bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            else
                            {
                                bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                                bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                        else if (ext == ".jpg")
                        {

                            if (m.format == Format.ExportFormats.Mp4PSPAVC ||
                               m.format == Format.ExportFormats.Mp4PSPAVCTV ||
                               m.format == Format.ExportFormats.Mp4PSPASP)
                            {
                                //масштабируем изображение
                                int sideHeight, sideWidth;
                                //выбираем большую сторону, по которой изменяем изображение
                                if (reader.Height < reader.Width)
                                {
                                    //если ширина больше высоты                                    
                                    sideHeight = 121;
                                    sideWidth = (int)((float)reader.Width * ((float)sideHeight / (float)reader.Height));
                                }
                                else
                                {
                                    //если высота больше ширины
                                    sideWidth = 161;
                                    sideHeight = (int)((float)reader.Height * ((float)sideWidth / (float)reader.Width));
                                }

                                bmp = new System.Drawing.Bitmap(160, 120);
                                g = System.Drawing.Graphics.FromImage(bmp);

                                //метод интерполяции при ресайзе
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                                //процент cжатия jpg
                                System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                                System.Drawing.Imaging.EncoderParameters encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                                encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);

                                //вывод и запись изображения в файл
                                g.DrawImage(reader.ReadFrameBitmap(frame), (int)(0.5 * (160 - (float)sideWidth)), (int)(0.5 * (120 - (float)sideHeight)), sideWidth, sideHeight);
                                bmp.Save(s.FileName, info[1], encoderParameters);
                            }
                            else
                            {
                                bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                                //сжатие jpg
                                System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                                System.Drawing.Imaging.EncoderParameters encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                                encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);

                                bmp.Save(s.FileName, info[1], encoderParameters);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorException(ex.Message);
                }
                finally
                {
                    //завершение
                    if (g != null) g.Dispose();
                    if (bmp != null) bmp.Dispose();
                    if (reader != null) reader.Close();
                }
            }
        }

        private Massive UpdateOutAudioPath(Massive mass)
        {
            foreach (object o in mass.outaudiostreams)
            {
                AudioStream outstream = (AudioStream)o;
                AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];

                //проверяем можно ли копировать данный формат
                if (outstream.codec == "Copy")
                {
                    //AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];
                    if (instream.audiopath == null)
                    {
                        string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                        string outpath = Settings.TempPath + "\\" + mass.key + "_" + mass.outaudiostream + outext;
                        outstream.audiopath = outpath;
                    }
                    else
                    {
                        outstream.audiopath = instream.audiopath;
                    }
                }
                else
                {
                    string aext = Format.GetValidRAWAudioEXT(outstream.codec);
                    outstream.audiopath = Settings.TempPath + "\\" + mass.key + aext;
                }
            }

            return mass;
        }

        private void menu_fix_AVCHD_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog f = new System.Windows.Forms.FolderBrowserDialog();
                f.Description = Languages.Translate("Select AVCHD folder") + ":";
                f.ShowNewFolderButton = false;

                string rootpath = Settings.AVCHD_PATH;
                if (rootpath != null && Directory.Exists(rootpath))
                    f.SelectedPath = rootpath;

                if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Settings.AVCHD_PATH = f.SelectedPath;


                    int fixedfiles = Calculate.MakeFAT32BluRay(f.SelectedPath);

                    if (fixedfiles != 0)
                    {
                        Message mes = new Message(this);
                        mes.ShowMessage(Languages.Translate("All file names fixed!"), "AVCHD");
                    }
                    else
                    {
                        Message mes = new Message(this);
                        mes.ShowMessage(Languages.Translate("Can`t find files for fixing!"), Languages.Translate("Error"));
                    }

                }
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void button_edit_format_Click(object sender, RoutedEventArgs e)
        {
            if (m != null)
            {
                if (m.format == Format.ExportFormats.M2TS ||
                    m.format == Format.ExportFormats.TS ||
                    m.format == Format.ExportFormats.Mkv)
                {
                    Options_M2TS m2ts = new Options_M2TS(m, this);
                    m = m2ts.m.Clone();
                    UpdateTaskMassive(m);
                }
                else if (m.format == Format.ExportFormats.Mpeg2PAL ||
                    m.format == Format.ExportFormats.Mpeg2NTSC)
                {
                    Options_MPEG2DVD dvd = new Options_MPEG2DVD(m, this);
                    m = dvd.m.Clone();
                    UpdateTaskMassive(m);
                }
                else if (m.format == Format.ExportFormats.BluRay)
                {
                    Options_BluRay blu = new Options_BluRay(m, this);
                    m = blu.m.Clone();
                    UpdateTaskMassive(m);
                }
                else if (m.format == Format.ExportFormats.Custom)
                {
                    FormatSettings custom = new FormatSettings(m, this);
                    if (custom.update_massive && !custom.update_audio && !custom.update_framerate && !custom.update_resolution)
                    {
                        m.split = Format.GetSplitting(m.format);
                        m.outfilesize = Calculate.GetEncodingSize(m);
                        m.dontmuxstreams = Format.GetMultiplexing(m.format);
                        if (m.outfilepath != null) m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + Format.GetValidExtension(m);
                        
                        UpdateTaskMassive(m);
                    }
                    else if (custom.update_audio || custom.update_framerate || custom.update_resolution)
                    {
                        string old_script = m.script;
                        
                        //забиваем-обновляем аудио массивы
                        if (custom.update_audio) m = FillAudio(m);
                                               
                        //перезабиваем специфику формата
                        if (custom.update_framerate)
                        {
                            m = Format.GetOutInterlace(m);
                            m = Format.GetValidFramerate(m);
                            m = Calculate.UpdateOutFrames(m);
                        }
                        if (custom.update_resolution)
                        {
                            m = Format.GetValidResolution(m);
                            m = Format.GetValidOutAspect(m);
                            m = AspectResolution.FixAspectDifference(m);
                        }
                       
                        //перезабиваем настройки форматов
                        m.split = Format.GetSplitting(m.format);
                        m.outfilesize = Calculate.GetEncodingSize(m);
                        m.dontmuxstreams = Format.GetMultiplexing(m.format);
                        if (m.outfilepath != null) m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + Format.GetValidExtension(m);
                        
                        //создаём новый AviSynth скрипт
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);

                        //загружаем обновлённый скрипт
                        if (old_script != m.script) LoadVideo(MediaLoad.update);
                        
                        UpdateTaskMassive(m);
                    }                    
                }
                else
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("This format doesn`t have any settings."), Languages.Translate("Format"));
                }
            }
            else if (combo_format.SelectedItem.ToString() == "Custom") new FormatSettings(null, this);
        }

        private void menu_settings_Click(object sender, RoutedEventArgs e)
        {
            Settings_Window sett = new Settings_Window(this, 1);
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
            if (this.graphBuilder != null && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) //Если ДиректШоу и Граф не пуст..
                basicAudio.put_Volume(VolumeSet); //..то задаем громкость для ДиректШоу
            
            //Иконка регулятора громкости
            if (slider_Volume.Value <= 0.0)
                image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume2.png", UriKind.RelativeOrAbsolute));
            else
                image_volume.Source = new BitmapImage(new Uri(@"../pictures/Volume.png", UriKind.RelativeOrAbsolute));
            
            //Запись значения громкости в реестр
            Settings.VolumeLevel = slider_Volume.Value;
        }

        //Меняем громкость колесиком мышки
        private void Volume_Wheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                VolumePlus();
            else
                VolumeMinus();
        }

        //Обработка двойного щелчка мыши для плейера
        private void Player_Mouse_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
                SwitchToFullScreen();
        }

        //Если мышь вошла в зону с названием файла, то таймер показывает общее время видео
        private void textbox_name_MouseEnter(object sender, MouseEventArgs e)
        {
            this.textbox_time.Visibility = Visibility.Collapsed;
            this.textbox_duration.Visibility = Visibility.Visible;
        }

        //Если мышь вышла из зоны с названием файла, то таймер снова показывает прошедшее время
        private void textbox_name_MouseLeave(object sender, MouseEventArgs e)
        {
            this.textbox_time.Visibility = Visibility.Visible;
            this.textbox_duration.Visibility = Visibility.Collapsed;
        }
     
        private void ResetTrim()
        {
            trim_start = trim_end = 0;
            textbox_start.Text = textbox_end.Text = "";
            button_set_start.Content = Languages.Translate("Set Start");
            button_set_end.Content = Languages.Translate("Set End");
            button_apply_trim.Content = Languages.Translate("Apply Trim");
            textbox_start.IsReadOnly = textbox_end.IsReadOnly = trim_is_on = false;
        }
        
        private void button_set_start_Click(object sender, RoutedEventArgs e)
        {
            if (m != null && !trim_is_on)
            {
                if (Convert.ToString(button_set_start.Content) != Languages.Translate("Clear"))
                {
                    if (textbox_start.Text == "") //Ничего не вписано - определяем текущий кадр
                    {
                        trim_start = Convert.ToInt32(Position.TotalSeconds * fps);
                        textbox_start.Text = Convert.ToString(trim_start);
                    }
                    else if (textbox_start.Text.Contains(":")) //Вписано время - пересчитываем в кадр
                    {
                        TimeSpan result = TimeSpan.Zero;
                        if (TimeSpan.TryParse(textbox_start.Text, out result))
                        {
                            trim_start = Convert.ToInt32(result.TotalSeconds * fps);
                            textbox_start.Text = Convert.ToString(trim_start);
                        }
                        else
                            return;
                    }
                    else if (!int.TryParse(textbox_start.Text, out trim_start)) //Вписан номер кадра
                        return;

                    textbox_start.IsReadOnly = true;
                    button_set_start.Content = Languages.Translate("Clear");
                }
                else
                {
                    trim_start = 0;
                    textbox_start.Text = "";
                    textbox_start.IsReadOnly = false;
                    button_set_start.Content = Languages.Translate("Set Start");
                }
            }
        }

        private void button_set_end_Click(object sender, RoutedEventArgs e)
        {
            if (m != null && !trim_is_on)
            {
                if (Convert.ToString(button_set_end.Content) != Languages.Translate("Clear"))
                {
                    if (textbox_end.Text == "") //Ничего не вписано - определяем текущий кадр
                    {
                        trim_end = Convert.ToInt32(Position.TotalSeconds * fps);
                        textbox_end.Text = Convert.ToString(trim_end);
                    }
                    else if (textbox_end.Text.Contains(":")) //Вписано время - пересчитываем в кадр
                    {
                        TimeSpan result = TimeSpan.Zero;
                        if (TimeSpan.TryParse(textbox_end.Text, out result))
                        {
                            trim_end = Convert.ToInt32(result.TotalSeconds * fps);
                            textbox_end.Text = Convert.ToString(trim_end);
                        }
                        else
                            return;
                    }
                    else if (!int.TryParse(textbox_end.Text, out trim_end)) //Вписан номер кадра
                        return;

                    textbox_end.IsReadOnly = true;
                    button_set_end.Content = Languages.Translate("Clear");
                }
                else
                {
                    trim_end = 0;
                    textbox_end.Text = "";
                    textbox_end.IsReadOnly = false;
                    button_set_end.Content = Languages.Translate("Set End");
                }
            }
        }

        private void button_apply_trim_Click(object sender, RoutedEventArgs e)
        {
            if (m == null) return;
            if (!trim_is_on && trim_start != trim_end)
            {
                if (trim_end != 0 && trim_start > trim_end) return;

                m.trim_start = trim_start;
                m.trim_end = trim_end;
                button_apply_trim.Content = Languages.Translate("Remove Trim");
                textbox_start.IsReadOnly = textbox_end.IsReadOnly = trim_is_on = true;
                UpdateScriptAndDuration();
                ValidateTrimAndCopy(m);
            }
            else if (trim_is_on)
            {
                if (Convert.ToString(button_set_start.Content) != Languages.Translate("Clear")) textbox_start.IsReadOnly = false;
                if (Convert.ToString(button_set_end.Content) != Languages.Translate("Clear")) textbox_end.IsReadOnly = false;
                button_apply_trim.Content = Languages.Translate("Apply Trim");
                m.trim_start = m.trim_end = 0;
                trim_is_on = false;
                UpdateScriptAndDuration();
            }
        }

        private void UpdateScriptAndDuration()
        {
            //Обновляем скрипт
            m = AviSynthScripting.CreateAutoAviSynthScript(m);
            LoadVideo(MediaLoad.update);

            //Пересчет кол-ва кадров в видео, его продолжительности и размера получаемого файла
            int outframes = 0;
            TimeSpan outduration = TimeSpan.Zero;
            AviSynthReader reader = null;
            bool is_errors = false;

            try
            {
                reader = new AviSynthReader();
                reader.ParseScript(m.script);
                outframes = reader.FrameCount;
                outduration = TimeSpan.FromSeconds((double)outframes / reader.Framerate); // / fps)
            }
            catch 
            {
                is_errors = true;
            }
            finally
            {
                reader.Close();
                reader = null;
            }

            if (!is_errors)
            {
                m.outframes = outframes;
                m.outduration = outduration; //TimeSpan.FromSeconds((double)m.outframes / fps);
                m.thmframe = m.outframes / 2;
            }
            else
                m = Calculate.UpdateOutFrames(m);

            m.outfilesize = Calculate.GetEncodingSize(m);
            UpdateTaskMassive(m);
        }

        //Открытие папки (пакетная обработка)
        private void menu_open_folder_Click(object sender, RoutedEventArgs e)
        {
            CloseChildWindows();

            try
            {
                OpenDialogs.owner = this;
                string path_to_open = OpenDialogs.OpenFolder();
                if (path_to_open == null || Directory.GetFiles(path_to_open).Length == 0) return;

                PauseAfterFirst = Settings.BatchPause;
                if (!PauseAfterFirst)
                {
                    path_to_save = OpenDialogs.SaveFolder();
                    if (path_to_save == null) return;
                }

                this.Height = this.Window.Height + 1; //чтоб убрать остатки от окна выбора директории, вот такой вот способ...
                this.Height = this.Window.Height - 1;

                MultiOpen(Directory.GetFiles(path_to_open, "*")); //Открываем-сохраняем
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void MultiOpen(string[] files_to_open) //Для открытия и сохранения группы файлов
        {
            try
            {
                opened_files = 0; //Обнуляем счетчик успешно открытых файлов
                int count = files_to_open.Length; //Кол-во файлов для открытия
                CloneIsEnabled = (m != null && (this.videoWindow != null || this.VideoElement.Source != null));

                //Вывод первичной инфы об открытии
                textbox_name.Text = count + " - " + Languages.Translate("total files, ") + opened_files + " - " + Languages.Translate("opened files, ") + outfiles.Count + " - " + Languages.Translate("in queue");

                //Делим строку с валидными расширениями на отдельные строчки
                string[] goodexts = Settings.GoodFilesExtensions.Split(new string[] { "/" }, StringSplitOptions.None);

                foreach (string file in files_to_open)
                {
                    string ext = Path.GetExtension(file).ToLower().Replace(".", "");

                    //Сравниваем расширение текущего файла со всеми строчками с валидными расширениями, и открываем файл при совпадении
                    foreach (string goodext in goodexts)
                    {
                        if (goodext == ext)
                        {
                            Massive x = new Massive();
                            x.infilepath = file;
                            x.infileslist = new string[] { file };
                            if (PauseAfterFirst) //Пакетное открытие с паузой после первого файла
                            {
                                ArrayList newArray = new ArrayList(); //Удаляем этот файл из общего списка
                                foreach (string f in files_to_open) { if (f != file) newArray.Add(f); }
                                batch_files = (string[])newArray.ToArray(typeof(string));
                                action_open(x);
                                if (m == null) { PauseAfterFirst = false; return; }
                                button_save.Content = button_encode.Content = Languages.Translate("Resume");
                                textbox_name.Text = Languages.Translate("Press Resume to continue batch opening");
                                return;
                            }
                            IsBatchOpening = true;
                            action_open(x);
                            IsBatchOpening = false;
                            if (m != null) action_auto_save(m.Clone());
                            break;
                        }
                    }

                    //Обновляем инфу об открытии
                    textbox_name.Text = count + " - " + Languages.Translate("total files, ") + opened_files + " - " + Languages.Translate("opened files, ") + outfiles.Count + " - " + Languages.Translate("in queue");
                }
                if (m != null && opened_files >= 1) //Если массив не пуст, и если кол-во открытых файлов больше нуля (чтоб не обновлять превью, если ни одного нового файла не открылось)
                {
                    LoadVideo(MediaLoad.load);
                }

                if (Settings.AutoBatchEncoding) EncodeNextTask(); //Запускаем кодирование

                Message mess = new Message(this);
                mess.ShowMessage(count + " - " + Languages.Translate("total files in folder") + Environment.NewLine + opened_files + " - " + Languages.Translate("successfully opened files")
                     + Environment.NewLine + outfiles.Count + " - " + Languages.Translate("total tasks in queue"), Languages.Translate("Complete"));

                if (m != null) textbox_name.Text = m.taskname;
                else textbox_name.Text = "";
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void action_auto_save(Massive mass)
        {
            if (mass != null && path_to_save != null)
            {               
                //Разбираемся с названием для перекодированного файла
                mass.outfilepath = path_to_save + "\\" + Path.GetFileNameWithoutExtension(m.infilepath) + Format.GetValidExtension(m);
                while(File.Exists(mass.outfilepath))
                    mass.outfilepath = Path.GetDirectoryName(mass.outfilepath) + "\\(" + Path.GetFileName(m.infilepath) + ") " + Path.GetFileName(mass.outfilepath);
                    //mass.outfilepath = path_to_save + "\\(" + Path.GetFileName(m.infilepath) + ") " + Path.GetFileNameWithoutExtension(m.infilepath) + Format.GetValidExtension(m);
                
                //Выход отсюда, если такое задание уже имеется (видимо была ошибка при открытии файла, и текущее задание дублирует предыдущее)
                if (outfiles.Contains(mass.outfilepath))
                    return;
           
                //запоминаем уникальный ключ
                int n = Convert.ToInt32(Settings.Key) + 1;
                Settings.Key = n.ToString("0000");
                m.key = Settings.Key;

                //добавлем задание в лок
                outfiles.Add(mass.outfilepath);

                //убираем выделение из списка заданий
                list_tasks.SelectedIndex = -1;
                
                //добавляем задание в список
                mass = UpdateOutAudioPath(mass);
                AddTask(mass, "Waiting");
                
                //Увеличиваем счетчик успешно открытых файлов
                opened_files += 1;            
            }
        }

        //Проверка темп-папки на наличие в ней файлов
        public void TempFolderFiles()
        {
            try
            {
                if (Directory.Exists(Settings.TempPath) && Directory.GetFiles(Settings.TempPath).Length != 0)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Selected temp folder is not empty") + " (" + Settings.TempPath.ToString() + ")." + Environment.NewLine + Languages.Translate("You must delete all unnecessary files before start encoding.")
                        + Environment.NewLine + Environment.NewLine + Languages.Translate("OK - open folder to view files, Cancel - ignore this message."), Languages.Translate("Temp folder is not empty"), Message.MessageStyle.OkCancel);
                    if (mess.result == Message.Result.Ok)
                        System.Diagnostics.Process.Start("explorer.exe", Settings.TempPath);
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void MenuHider(bool ShowItems)
        {
            //Делаем пункты меню (не)активными
            mnCloseFile.IsEnabled = ShowItems;
            mnSave.IsEnabled = ShowItems;
            menu_save_frame.IsEnabled = ShowItems;
            menu_savethm.IsEnabled = ShowItems;
            mnUpdateVideo.IsEnabled = ShowItems;
            cmn_refresh.IsEnabled = ShowItems;
            menu_demux_video.IsEnabled = ShowItems;
            menu_autocrop.IsEnabled = ShowItems;
            menu_detect_interlace.IsEnabled = ShowItems;
            menu_saturation_brightness.IsEnabled = ShowItems;
            mnAspectResolution.IsEnabled = ShowItems;
            menu_interlace.IsEnabled = ShowItems;
            menu_venc_settings.IsEnabled = ShowItems;
            menu_demux.IsEnabled = ShowItems;
            menu_save_wav.IsEnabled = ShowItems;
            menu_audiooptions.IsEnabled = ShowItems;
            menu_aenc_settings.IsEnabled = ShowItems;
            mnAddSubtitles.IsEnabled = ShowItems;
            mnRemoveSubtitles.IsEnabled = ShowItems;
            menu_editscript.IsEnabled = ShowItems;
            menu_createautoscript.IsEnabled = ShowItems;
            menu_save_script.IsEnabled = ShowItems;
            target_goto.IsEnabled = ShowItems;
            menu_createtestscript.IsEnabled = ShowItems;
            cmn_addtobookmarks.IsEnabled = cmn_deletebookmarks.IsEnabled = ShowItems;

            cmn_bookmarks.Items.Clear();
            AssemblyInfoHelper asinfo = new AssemblyInfoHelper();
            if (m != null)
            {
                this.Title = Path.GetFileName(m.infilepath) + "  - XviD4PSP - v" + asinfo.Version + "  " + asinfo.Trademark;
                this.menu_createtestscript.IsChecked = m.testscript;
                if (m.trim_start != 0 || m.trim_end != 0) //Восстанавливаем трим (из сохраненного задания)
                {
                    textbox_start.Text = (trim_start = m.trim_start).ToString();
                    textbox_end.Text = (trim_end = m.trim_end).ToString();
                    textbox_start.IsReadOnly = textbox_end.IsReadOnly = trim_is_on = true;
                    button_set_start.Content = button_set_end.Content = Languages.Translate("Clear");
                    button_apply_trim.Content = Languages.Translate("Remove Trim");
                }
                else
                    ResetTrim();

                //Восстанавливаем закладки
                if (m.bookmarks.Count > 0)
                {
                    foreach (TimeSpan bookmark in m.bookmarks)
                        add_bookmark_to_cmn(bookmark.ToString());
                    cmn_bookmarks.IsEnabled = true;
                }
                else
                    cmn_bookmarks.IsEnabled = false;
            }
            else
            {
                this.Title = "XviD4PSP - AviSynth-based MultiMedia Converter  -  v" + asinfo.Version + "  " + asinfo.Trademark;
                this.menu_createtestscript.IsChecked = false;
                this.cmn_bookmarks.IsEnabled = false;
            }
            asinfo = null;
        }

        private void GoTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                int frame;
                if (int.TryParse(textbox_frame_goto.Text, out frame))
                {
                    if (graphBuilder != null)
                    {
                        TimeSpan newpos = TimeSpan.FromSeconds(frame / fps);
                        newpos = (newpos < TimeSpan.Zero) ? TimeSpan.Zero : (newpos > NaturalDuration) ? NaturalDuration : newpos;
                        if (Position != newpos) Position = newpos;
                    }
                    else if (Pic.Visibility == Visibility.Visible)
                    {
                        //Для PictureView можно непосредственно указать нужный кадр
                        frame = (frame < 0) ? 0 : (frame > pic_total_frames) ? pic_total_frames : frame;
                        if (pic_frame != frame) ShowWithPictureView(m.script, frame);
                    }
                }
                GoTo_Click(null, null);
            }
            else if (e.Key == Key.Escape) GoTo_Click(null, null);
        }

        private void GoTo_Click(object sender, MouseButtonEventArgs e)
        {
            if (textbox_frame_goto.Visibility == Visibility.Hidden)
            {
                textbox_frame.Visibility = Visibility.Hidden;
                textbox_frame_goto.Visibility = Visibility.Visible;
                target_goto.Visibility = Visibility.Collapsed;
                textbox_frame_goto.Focus();
                if (e != null && e.RightButton == MouseButtonState.Pressed)
                    GoTo_MouseRightDown(null, null);
            }
            else
            {
                textbox_frame.Visibility = Visibility.Visible;
                textbox_frame_goto.Visibility = Visibility.Hidden;
                target_goto.Visibility = Visibility.Visible;
            }
        }

        private void GoTo_MouseRightDown(object sender, MouseButtonEventArgs e)
        {
             textbox_frame_goto.Text = Math.Round(Position.TotalSeconds * fps).ToString();            
        }

        private void ValidateTrimAndCopy(Massive mass)
        {
            if (combo_aencoding.SelectedItem.ToString() == "Copy" || combo_vencoding.SelectedItem.ToString() == "Copy")
            {
                if (mass.trim_start != 0 || mass.trim_end != 0)
                    new Message(this).ShowMessage(Languages.Translate("Trimming feature doesn't affect the track(s) in Copy mode!"), Languages.Translate("Warning"), Message.MessageStyle.Ok);

                if (mass.testscript)
                    new Message(this).ShowMessage(Languages.Translate("Test script and Copy mode are not compatible!"), Languages.Translate("Warning"), Message.MessageStyle.Ok);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void ApplyTestScript(object sender, RoutedEventArgs e)
        {
            m.testscript = !m.testscript;
            UpdateScriptAndDuration();
            ValidateTrimAndCopy(m);
        }

        private void UpdateRecentFiles()
        {
            mnRecentFiles.Items.Clear();
            string file = "";
            string[] rfiles = Settings.RecentFiles.Split(new string[] { ";" }, StringSplitOptions.None);
            for (int i = 0; i < rfiles.Length && i < 5; i++)
            {
                if (string.IsNullOrEmpty(file = rfiles[i].Trim())) break;
                MenuItem mn = new MenuItem();
                mn.Header = "_" + Calculate.GetShortPath(file);
                mn.ToolTip = file;
                mn.Icon = new TextBlock { Text = ((i + 1) + ".") };
                mn.Click += new RoutedEventHandler(menu_rf_Click);
                mnRecentFiles.Items.Add(mn);
            }
        }

        private void menu_rf_Click(object sender, RoutedEventArgs e)
        {
            string file = ((MenuItem)sender).ToolTip.ToString();
            if (File.Exists(file))
            {
                Massive x = new Massive();
                x.infilepath = file;
                x.infileslist = new string[] { x.infilepath };
                action_open(x);
            }
            else 
                new Message(this).ShowMessage(Languages.Translate("Can`t find file") + ": " + file, Languages.Translate("Error"), Message.MessageStyle.Ok);
        }

        private void button_apply_Click(object sender, RoutedEventArgs e)
        {
            if (m == null) return;
            m.script = script_box.Text;
            AviSynthScripting.WriteScriptToFile(m.script, "preview");
            AviSynthScripting.WriteScriptToFile(m.script, "AvsP");
            UpdateTaskMassive(m);
        }

        private void button_avsp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process pr = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);

                if (m == null)
                {
                    Massive x = new Massive();
                    x.owner = this;
                    x.taskname = "Untitled";
                    x.outfilepath = OpenDialogs.SaveDialog(x);
                    if (x.outfilepath != null)
                    {
                        //Создаем и открываем скрипт
                        File.WriteAllText(x.outfilepath, "#Write your script here, then Save it and Exit.\r\n#Note: this script will" +
                            " be opened using Import(), so you can`t change it later!\r\n\r\n\r\n", Encoding.Default);
                        info.Arguments = x.outfilepath;
                    }
                    pr.StartInfo = info;
                    pr.Start();

                    if (x.outfilepath == null) return;
                    pr.WaitForExit(); //Ждать завершения

                    if (File.Exists(x.outfilepath) && new FileInfo(x.outfilepath).Length > 0)
                    {
                        x.infilepath = x.outfilepath;
                        x.infileslist = new string[] { x.outfilepath };
                        x.outfilepath = x.taskname = null;
                        action_open(x);
                    }
                }
                else
                {
                    //Открываем текущий скрипт
                    info.Arguments = Settings.TempPath + "\\AvsP.avs";
                    pr.StartInfo = info;
                    pr.Start();
                    pr.WaitForExit(); //Ждать завершения
                    string oldscript = m.script;

                    //После завершения работы AvsP перечитываем измененный им файл AvsP.avs и вводим его содержимое в окно Фильтрация
                    using (StreamReader sr = new StreamReader(Settings.TempPath + "\\AvsP.avs", System.Text.Encoding.Default))
                        script_box.Text = m.script = sr.ReadToEnd();

                    //обновление при необходимости
                    if (m.script.Trim() != oldscript.Trim())
                    {
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage("AvsP editor: " + ex.Message, Languages.Translate("Error"));
            }
        }

        private void button_fullscreen_Click(object sender, RoutedEventArgs e)
        {
            SwitchToFullScreen();
        }

        private void button_play_script_Click(object sender, RoutedEventArgs e)
        {
            menu_play_in_Click(menu_playinmpc, null);
        }

        private void check_scriptview_white_Click(object sender, RoutedEventArgs e)
        {
            if (check_scriptview_white.IsChecked)
            {
                script_box.Background = Brushes.White;
                script_box.Foreground = Brushes.Black;
                Settings.ScriptView_Brushes = "#FFFFFFFF:#FF000000";
            }
            else
            {
                script_box.Background = Brushes.Transparent;
                script_box.Foreground = Brushes.White;
                Settings.ScriptView_Brushes = "#00000000:#FFFFFFFF";
            }
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

            if (old_value != new_value && m != null && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                LoadVideo(MediaLoad.update);
        }

        private void AddToBookmarks_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || (graphBuilder == null && Pic.Visibility != Visibility.Visible))
                return;

            //Удаляем закладку, если этот кадр уже сохранен
            int frame = Convert.ToInt32(Position.TotalSeconds * fps);
            foreach (TimeSpan bookmark in m.bookmarks)
            {
                if (frame == Convert.ToInt32(bookmark.TotalSeconds * fps))
                {
                    m.bookmarks.Remove(bookmark);
                    cmn_bookmarks.Items.Clear();

                    if (m.bookmarks.Count == 0)
                        cmn_bookmarks.IsEnabled = false;
                    else foreach (TimeSpan new_bookmark in m.bookmarks)
                            add_bookmark_to_cmn(new_bookmark.ToString());

                    UpdateTaskMassive(m);
                    return;
                }
            }

            //Сохраняем в массиве
            m.bookmarks.Add(Position);

            //Сохраняем в контекстном меню
            add_bookmark_to_cmn(Position.ToString());
            cmn_bookmarks.IsEnabled = true;

            UpdateTaskMassive(m);
        }

        private void add_bookmark_to_cmn(string time_pos)
        {          
            //Добавляем закладку в контекстное меню
            MenuItem item = new MenuItem();
            if (time_pos.Length < 12) item.Header = time_pos + ".000";
            else item.Header = time_pos.Remove(12);
            item.Click += new RoutedEventHandler(GoToBookmark_Click);
            cmn_bookmarks.Items.Add(item);
        }

        private void DeleteBookmarks_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || (graphBuilder == null && Pic.Visibility != Visibility.Visible))
                return;

            m.bookmarks = new ArrayList();
            cmn_bookmarks.Items.Clear();
            cmn_bookmarks.IsEnabled = false;
            UpdateTaskMassive(m);
        }

        private void GoToBookmark_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan position;
            if (TimeSpan.TryParse(((MenuItem)sender).Header.ToString(), out position) && position <= NaturalDuration)
                Position = position;
        }
    }
}