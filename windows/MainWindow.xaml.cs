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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace XviD4PSP
{
    public partial class MainWindow
    {
        public Massive m;
        public ArrayList outfiles = new ArrayList();
        public ArrayList deletefiles = new ArrayList();
        private ArrayList ffcache = new ArrayList();
        private ArrayList dgcache = new ArrayList();
        private static object backup_lock = new object();
        private static object avsp_lock = new object();
        private static object locker = new object();
        public Process avsp = null;

        //player
        private string total_frames = "";
        private string filepath = "";
        private Brush oldbrush;
        private TimeSpan oldpos;
        private Thickness oldmargin;
        private System.Windows.WindowState oldstate;
        private const int WMGraphNotify = 0x0400 + 13;
        private PlayState currentState = PlayState.Init;
        private MediaLoad mediaload;
        private int VolumeSet; //Громкость DirectShow плейера

        private IFilterGraph graph = null;
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

        //AviSynthPlayer (PictureView)
        private AviSynthPlayer avsPlayer = null;
        private int avsFrame = 0;

        private bool IsAudioOnly = false;
        private bool IsFullScreen = false;
        private bool IsAviSynthError = false;

        public IntPtr Handle = IntPtr.Zero;
        private IntPtr VHandle = IntPtr.Zero;
        private HwndSource source;
        private System.Timers.Timer timer;
        private BackgroundWorker worker = null;

        //Tray
        public System.Windows.Forms.NotifyIcon TrayIcon;                   //Иконка в трее
        private System.Windows.Forms.ToolStripMenuItem tmnExit;            //Пункт меню "Exit"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayClose;       //Пункт меню "Close to tray"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayMinimize;    //Пункт меню "Minimize to tray"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayClickOnce;   //Пункт меню "1-Click"
        private System.Windows.Forms.ToolStripMenuItem tmnTrayNoBalloons;  //Пункт меню "Disable balloons"

        private bool IsInsertAction = false;  //true, когда в list_tasks перемещаются задания
        private string path_to_save;          //Путь для конечных файлов при перекодировании папки
        private int opened_files = 0;         //Кол-во открытых файлов при открытии папки
        private double fps = 0;               //Значение fps для текущего клипа; будет определяться каждый раз при загрузке (обновлении) превью
        private bool OldSeeking = false;      //Способ позиционирования, old - непрерывное, new - только при отпускании кнопки мыши
        private bool IsBatchOpening = false;  //true при пакетном открытии
        private bool PauseAfterFirst = false; //Пакетное открытие с паузой после 1-го файла
        private string[] batch_files;         //Сохраненный список файлов для пакетного открытия с паузой
        private string[] drop_data;           //Список забрасываемых файлов (drag-and-drop)
        private bool IsDragOpening = false;   //true всё время, пока идет открытие drag-and-drop
        public bool IsExiting = false;        //true, если надо выйти из программы, false - если свернуть в трей

        private enum MediaLoad { load = 1, update }
        private enum PlayState { Stopped, Paused, Running, Init }

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
                //Установка параметров окна из сохраненных настроек
                if (Settings.WindowResize)
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
            }
            catch (Exception ex)
            {
                ErrorException("Initializing (WindowSize): " + ex.Message, ex.StackTrace);
            }

            try
            {
                //Определяем наличие Ависинта
                SysInfo.RetrieveAviSynthInfo();
            }
            catch (Exception ex)
            {
                ErrorException("Initializing (AviSynth): " + ex.Message, ex.StackTrace);
            }

            try
            {
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
            }
            catch (Exception ex)
            {
                ErrorException("Initializing (TrayIcon): " + ex.Message, ex.StackTrace);
            }

            try
            {
                UpdateRecentFiles();  //Список недавних файлов
                MenuHider(false);     //Делаем пункты меню неактивными
                SetHotKeys();         //Загружаем HotKeys
                SetLanguage();        //переводим лейблы

                textbox_name.Text = textbox_frame.Text = textbox_frame_goto.Text = "";
                textbox_time.Text = textbox_duration.Text = "00:00:00";

                //Определяем коэффициент dpi
                SysInfo.RetrieveDPI(this);
            }
            catch (Exception ex)
            {
                ErrorException("Initializing (Misc): " + ex.Message, ex.StackTrace);
            }

            try
            {
                //events
                this.PreviewKeyDown += new KeyEventHandler(MainWindow_KeyDown);
                this.StateChanged += new EventHandler(MainWindow_StateChanged);
                this.textbox_name.MouseEnter += new MouseEventHandler(textbox_name_MouseEnter); //Мышь вошла в зону с названием файла
                this.textbox_name.MouseLeave += new MouseEventHandler(textbox_name_MouseLeave); //Мышь вышла из зоны с названием файла
                if (Settings.TrayClickOnce) TrayIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);
                else TrayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(TrayIcon_Click);

                DDHelper ddh = new DDHelper(this);
                ddh.GotFiles += new DDEventHandler(DD_GotFiles);
            }
            catch (Exception ex)
            {
                ErrorException("Initializing (Events): " + ex.Message, ex.StackTrace);
            }
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;

            //Вторая попытка для dpi
            if (SysInfo.dpi == 0)
                SysInfo.RetrieveDPI(this);

            Calculate.CheckWindowPos(this, ref Handle, true);
            this.MaxWidth = double.PositiveInfinity;
            this.MaxHeight = double.PositiveInfinity;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TrayIcon.Visible = Settings.TrayIconIsEnabled;

            if (Settings.Win7TaskbarIsEnabled && !Win7Taskbar.InitializeWin7Taskbar())
            {
                ErrorException(Languages.Translate("Failed to initialize Windows 7 taskbar interface.") +
                    " " + Languages.Translate("This feature will be disabled!"));
                Settings.Win7TaskbarIsEnabled = false;
            }

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainFormLoader();
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
                    LoadFilteringPresets();

                    //загружаем списки профилей цвето коррекции
                    LoadSBCPresets();

                    //прописываем текущий профиль
                    if (combo_sbc.Items.Contains(Settings.SBC))
                        combo_sbc.SelectedItem = Settings.SBC;
                    else
                        combo_sbc.SelectedItem = Settings.SBC = "Disabled";

                    //загружаем профили видео кодирования
                    if (Settings.FormatOut != Format.ExportFormats.Audio)
                    {
                        LoadVideoPresets();
                        SetVideoPreset();
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
                    SetAudioPreset();

                    //загружаем настройки
                    LoadSettings();

                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        ErrorException("SearchTempFolder: " + ex.Message, ex.StackTrace);
                    }

                    //Запускаем таймер, по которому потом будем обновлять позицию слайдера, счетчик времени, и еще одну хреновину..
                    timer = new System.Timers.Timer();
                    timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                    timer.Interval = 30;

                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                    {
                        source = HwndSource.FromHwnd(this.Handle);
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
                    }

                    //Не в xaml, чтоб не срабатывали до загрузки
                    this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                    this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
                    this.grid_tasks.SizeChanged += new SizeChangedEventHandler(grid_tasks_SizeChanged);

                    //Если AviSynth не был найден при старте
                    if (SysInfo.AVSVersionFloat == 0)
                    {
                        throw new Exception(Languages.Translate("AviSynth is not found!") + "\r\n" +
                            Languages.Translate("Please install AviSynth 2.5.7 MT or higher."));
                    }

                    //Восстановление заданий из резервной копии (после вылета)
                    if (Settings.EnableBackup && File.Exists(Settings.TempPath + "\\backup.tsks"))
                    {
                        Message mes = new Message(this);
                        mes.ShowMessage(Languages.Translate("It seems that the previous session was finished abnormally.") + "\r\n" +
                            Languages.Translate("Do you want to restore previously saved tasks from a backup?"), Languages.Translate("Question"), Message.MessageStyle.YesNo);
                        if (mes.result == Message.Result.Yes)
                        {
                            Massive x = new Massive();
                            x.infilepath = Settings.TempPath + "\\backup.tsks";
                            action_open(x);
                            return;
                        }
                    }

                    //Открытие файлов из командной строки (command line arguments)
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length > 1)
                    {
                        //Проверяем файлы
                        ArrayList files = new ArrayList();
                        for (int i = 1; i < args.Length; i++)
                        {
                            if (File.Exists(args[i])) files.Add(args[i]);
                        }

                        if (files.Count == 1) //Один файл
                        {
                            //создаём массив и забиваем в него данные
                            Massive x = new Massive();
                            x.infilepath = files[0].ToString();
                            x.infileslist = new string[] { x.infilepath };

                            //ищем соседние файлы и спрашиваем добавить ли их к заданию при нахождении таковых
                            if (Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly && Calculate.IsValidVOBName(x.infilepath) ||
                                Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled)
                                x = OpenDialogs.GetFriendFilesList(x);
                            if (x != null) action_open(x);
                        }
                        else if (files.Count > 1) //Несколько файлов
                        {
                            PauseAfterFirst = Settings.BatchPause;
                            if (!PauseAfterFirst)
                            {
                                path_to_save = OpenDialogs.SaveFolder();
                                if (path_to_save == null) return;
                            }

                            string[] _files = new string[files.Count];
                            for (int i = 0; i < files.Count; i++)
                                _files[i] = files[i].ToString();

                            MultiOpen(_files);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorException("LoadSettings: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;

            if (FileMenu.IsKeyboardFocusWithin || textbox_frame_goto.Visibility != Visibility.Hidden || textbox_start.IsFocused || textbox_end.IsFocused || script_box.IsFocused)
            {
                return;
            }
            else if (e.Key == Key.Delete && list_tasks.IsKeyboardFocusWithin)
            {
                RemoveSelectedTask();
                e.Handled = true;
                return;
            }

            string key = new System.Windows.Input.KeyConverter().ConvertToString(e.Key);
            string mod = new System.Windows.Input.ModifierKeysConverter().ConvertToString(System.Windows.Input.Keyboard.Modifiers);
            string Action = HotKeys.GetAction("=" + ((mod.Length > 0) ? mod + "+" : "") + key);
            if (Action.Length > 0)
            {
                e.Handled = true;
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
                    case ("Decoding"): mnDecoding_Click(mnVideoDecoding, null); break;
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
                    case ("Apply test script"): if (m != null) { ApplyTestScript(null, null); menu_createtestscript.IsChecked = m.testscript; }; break;
                    case ("Save script"): SaveScript(null, null); break;
                    case ("Run script"): menu_run_script_Click(null, null); break;
                    case ("MT settings"): menu_mt_settings_Click(null, null); break;
                    case ("Windows Media Player"): menu_play_in_Click(menu_playinwmp, null); break;
                    case ("Media Player Classic"): menu_play_in_Click(menu_playinmpc, null); break;
                    case ("WPF Video Player"): menu_play_in_Click(menu_playinwpf, null); break;
                    //Settings
                    case ("Global settings"): menu_settings_Click(null, null); break;
                    //Tools
                    case ("Media Info"): menu_info_media_Click(menu_info_media, null); break;
                    case ("FFRebuilder"): menu_ffrebuilder_Click(null, null); break;
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
                    case ("10 frames backward"): Frame_Shift(-10); break;
                    case ("100 frames forward"): Frame_Shift(100); break;
                    case ("100 frames backward"): Frame_Shift(-100); break;
                    case ("30 sec. forward"): Frame_Shift(Convert.ToInt32(fps * 30)); break;
                    case ("30 sec. backward"): Frame_Shift(-Convert.ToInt32(fps * 30)); break;
                    case ("3 min. forward"): Frame_Shift(Convert.ToInt32(fps * 180)); break;
                    case ("3 min. backward"): Frame_Shift(-Convert.ToInt32(fps * 180)); break;
                    case ("Play-Pause"): PauseClip(); break;
                    case ("Fullscreen"): SwitchToFullScreen(); break;
                    case ("Volume+"): VolumePlus(); break;
                    case ("Volume-"): VolumeMinus(); break;
                    case ("Set Start"): button_set_trim_value_Click(button_set_start, null); break;
                    case ("Set End"): button_set_trim_value_Click(button_set_end, null); break;
                    case ("Next/New region"): button_trim_plus_Click(null, null); break;
                    case ("Previous region"): button_trim_minus_Click(null, null); break;
                    case ("Apply Trim"): button_apply_trim_Click(null, null); break;
                    case ("Add/Remove bookmark"): AddToBookmarks_Click(null, null); break;
                    case ("Edit format"): button_edit_format_Click(null, null); break;
                }
            }
        }

        private void grid_tasks_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsFullScreen) MoveVideoWindow();
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
                if (Settings.TrayIconIsEnabled && Settings.TrayMinimize) this.Hide();
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
            if (!IsExiting && Settings.TrayIconIsEnabled && Settings.TrayClose)
            {
                e.Cancel = true;
                this.StateChanged -= new EventHandler(MainWindow_StateChanged);
                this.WindowState = System.Windows.WindowState.Minimized;
                this.Hide();
                this.StateChanged += new EventHandler(MainWindow_StateChanged);
                if (!Settings.TrayNoBalloons) TrayIcon.ShowBalloonTip(5000, "XviD4PSP", " ", System.Windows.Forms.ToolTipIcon.Info);
                return;
            }
            else
                IsExiting = true;

            //Проверяем, есть ли задания в работе (кодируются)
            foreach (Task task in list_tasks.Items)
            {
                if (task.Status == TaskStatus.Encoding)
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

            //Проверяем, есть ли задания в очереди (ожидают кодирования)
            foreach (Task task in list_tasks.Items)
            {
                if (task.Status == TaskStatus.Waiting)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("The queue isn`t empty.") + "\r\n" + Languages.Translate("Are you sure you want to quit?"), Languages.Translate("Warning"), Message.MessageStyle.YesNo);
                    if (mes.result == Message.Result.No)
                    {
                        IsExiting = false;
                        e.Cancel = true;
                        return;
                    }
                    break;
                }
            }

            //Убиваем процесс вместо выхода, если нажат Shift
            bool kill_me = false;
            if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Shift)
            {
                kill_me = true;
                goto finish;
            }

            if (m != null) CloseFile();

            //Удаляем резервную копию заданий
            SafeDelete(Settings.TempPath + "\\backup.tsks");

            //Временные файлы
            if (Settings.DeleteTempFiles)
            {
                //удаляем мусор
                foreach (string dfile in deletefiles)
                {
                    if (!dfile.Contains("cache")) SafeDelete(dfile);
                }

                //Удаление индекс-файлов от FFmpegSource2
                if (Settings.DeleteFFCache)
                {
                    //Которые рядом с исходником
                    foreach (string file in ffcache) SafeDelete(file);

                    //Которые в Темп-папке
                    foreach (string file in Directory.GetFiles(Settings.TempPath, "*.ffindex")) SafeDelete(file);
                }

                //Удаление DGIndex-кэша
                if (Settings.DeleteDGIndexCache)
                {
                    foreach (string cache_path in dgcache)
                        SafeDirDelete(Path.GetDirectoryName(cache_path), false);
                }
            }

            finish:

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

            //Убиваемся, если надо
            if (kill_me) Process.GetCurrentProcess().Kill();
        }

        private void clear_dgindex_cache()
        {
            try
            {
                if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && Settings.DeleteDGIndexCache)
                {
                    //Выходим, если кэш-файл был создан не нами
                    if (!dgcache.Contains(m.indexfile) || m.indexfile == m.infilepath) return;

                    //Выходим, если кэш-файл используется в каком-либо задании
                    foreach (Task task in list_tasks.Items)
                    {
                        if (task.Mass.indexfile == m.indexfile) return;
                    }

                    //Удаляем папку с кэшем
                    SafeDirDelete(Path.GetDirectoryName(m.indexfile), false);
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
                x.vdecoder = AviSynthScripting.Decoders.FFmpegSource2;
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
                ErrorException("ResetSettings: " + ex.Message, ex.StackTrace);
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
                Massive x = OpenDialogs.OpenFile();
                if (x != null) action_open(x);
            }
        }

        private void action_add()
        {
            //запоминаем старое положени трекбара
            string[] oldfiles = m.infileslist;
            Massive x = m.Clone();

            try
            {
                FilesListWindow f = new FilesListWindow(x);
                if (f.m == null) return;
                x = f.m.Clone();

                //если что-то изменилось
                bool needupdate = false;
                if (oldfiles.Length != x.infileslist.Length)
                    needupdate = true;
                else
                {
                    for (int i = 0; i < x.infileslist.Length; i++)
                    {
                        //Файлы поменялись местами
                        if (oldfiles[i] != x.infileslist[i])
                        {
                            needupdate = true; break;
                        }
                    }
                }

                if (needupdate)
                {
                    string ext = Path.GetExtension(x.infilepath).ToLower();

                    //Для MPEG2Source надо переиндексировать - проще закрыть и открыть файл(ы) заново
                    if (x.vdecoder == AviSynthScripting.Decoders.MPEG2Source && !string.IsNullOrEmpty(x.indexfile) && ext != ".d2v")
                    {
                        //Наверно не самое удачное решение..
                        bool delete_temp = Settings.DeleteTempFiles;
                        bool delete_dgindex = Settings.DeleteDGIndexCache;
                        Settings.DeleteTempFiles = Settings.DeleteDGIndexCache = true;

                        try
                        {
                            //Тут должна удалиться индекс-папка
                            CloseFile();
                        }
                        finally
                        {
                            Settings.DeleteTempFiles = delete_temp;
                            Settings.DeleteDGIndexCache = delete_dgindex;
                        }

                        x.inaudiostreams = new ArrayList();
                        x.outaudiostreams = new ArrayList();
                        action_open(x);
                        return;
                    }

                    //Обновляем возможные индекс-файлы от FFmpegSource2
                    if (!x.ffms_indexintemp)
                    {
                        foreach (string file in x.infileslist)
                        {
                            if (!ffcache.Contains(file + ".ffindex"))
                                ffcache.Add(file + ".ffindex");
                        }
                    }

                    //Переиндексация для FFmpegSource2
                    if (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource2)
                    {
                        Indexing_FFMS ffindex = new Indexing_FFMS(x);
                        if (ffindex.m == null)
                        {
                            CloseFile();
                            return;
                        }
                    }

                    if (x.inaudiostreams.Count > 0)
                    {
                        AudioStream s = (AudioStream)x.inaudiostreams[x.inaudiostream];
                        ArrayList afiles = new ArrayList();
                        if (ext == ".d2v" || ext == ".dga" || ext == ".dgi")
                        {
                            foreach (string file in x.infileslist)
                            {
                                ArrayList afileslist = Indexing.GetTracks(file);
                                if (afileslist.Count > 0)
                                    afiles.Add(afileslist[Math.Min(x.inaudiostream, afileslist.Count - 1)]);
                            }
                        }
                        else if (!x.isvideo) //Для аудио файлов
                        {
                            foreach (string file in x.infileslist)
                                afiles.Add(file);

                            x.inframes = 0;
                            x.induration = TimeSpan.Zero;
                        }
                        else
                            afiles.Add(s.audiopath);

                        s.audiofiles = Calculate.ConvertArrayListToStringArray(afiles);
                    }

                    //создаём новый AviSynth скрипт
                    x = AviSynthScripting.CreateAutoAviSynthScript(x);

                    //подсчитываем размер
                    long sizeb = 0;
                    foreach (string file in x.infileslist)
                    {
                        sizeb += new FileInfo(file).Length;
                    }
                    x.infilesize = Calculate.ConvertDoubleToPointString((double)sizeb / 1049511, 1) + " mb";
                    x.infilesizeint = (int)((double)sizeb / 1049511);

                    //получаем длительность и фреймы
                    Caching cach = new Caching(x);
                    if (cach.m == null) return;
                    x = cach.m.Clone();

                    //Нужно обновить кол-во кадров в BlankClip()
                    if (x.vdecoder == AviSynthScripting.Decoders.BlankClip)
                        x = AviSynthScripting.CreateAutoAviSynthScript(x);

                    x = Calculate.UpdateOutFrames(x);

                    //загружаем обновлённый скрипт
                    m = x.Clone();
                    LoadVideo(MediaLoad.load);
                }
            }
            catch (Exception ex)
            {
                ErrorException("AppendFile: " + ex.Message, ex.StackTrace);
            }
        }

        //начало открытия файла
        private void action_open(Massive x)
        {
            try
            {
                if (x == null) return;

                //Если AviSynth не был найден при старте и не был установлен после него
                if (SysInfo.AVSVersionFloat == 0 && !SysInfo.RetrieveAviSynthInfo())
                {
                    throw new Exception(Languages.Translate("AviSynth is not found!") + "\r\n" +
                        Languages.Translate("Please install AviSynth 2.5.7 MT or higher."));
                }

                string ext = Path.GetExtension(x.infilepath).ToLower();

                //Уводим фокус с комбобоксов куда-нибудь!
                if (!slider_pos.Focus()) slider_Volume.Focus();

                //Проверка на "нехорошие" символы в путях
                if (ext != ".tsks" && !Calculate.ValidatePath(x.infilepath, !IsBatchOpening))
                    return;

                //Закрываем уже открытый файл; обнуляем трим и его кнопки
                if (!IsBatchOpening && m != null) CloseFile();
                else ResetTrim();

                //Загружаем сохраненные задания
                if (ext == ".tsks")
                {
                    RestoreTasks(x.infilepath, ref x);

                    //Выходим при ошибке или если были только одни задания;
                    //если был и массив, то открываем его
                    if (x == null) return;
                    else m = x.Clone();
                    x = null;

                    LoadVideoPresets();

                    if (m.outaudiostreams.Count > 0)
                    {
                        AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        LoadAudioPresets();
                        combo_aencoding.SelectedItem = outstream.encoding;
                        Settings.SetAEncodingPreset(m.format, outstream.encoding);
                    }
                    else
                    {
                        combo_aencoding.SelectedItem = "Disabled";
                    }

                    combo_format.SelectedItem = Format.EnumToString(Settings.FormatOut = m.format);
                    combo_sbc.SelectedItem = Settings.SBC = m.sbc;
                    combo_filtering.SelectedValue = Settings.Filtering = m.filtering;
                    combo_vencoding.SelectedItem = m.vencoding;
                    Settings.SetVEncodingPreset(m.format, m.vencoding);

                    goto finish;
                }

                //Определяем, где создавать индекс-файлы для FFmpegSource2 + занесение их в список на удаление
                if (!(x.ffms_indexintemp = (Settings.FFMS_IndexInTemp || Calculate.IsReadOnly(x.infilepath))))
                {
                    foreach (string file in x.infileslist)
                    {
                        if (!ffcache.Contains(file + ".ffindex"))
                            ffcache.Add(file + ".ffindex");
                    }
                }

                //присваиваем заданию уникальный ключ
                if (Settings.Key == "9999")
                    Settings.Key = "0000";
                x.key = Settings.Key;

                //имя
                if (x.taskname == null)
                    x.taskname = Path.GetFileNameWithoutExtension(x.infilepath);

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
                        x.inaudiostreams.Add(stream.Clone());
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

                    if (Format.GetValidVDecoder(x) == AviSynthScripting.Decoders.MPEG2Source)
                    {
                        //проверяем индекс папку (проверка содержимого файла - если там МПЕГ, то нужна индексация; тут-же идет запуск МедиаИнфо)
                        IndexChecker ich = new IndexChecker(x);
                        if (ich.m == null) return;
                        x = ich.m.Clone();

                        if (x.indexfile != null)
                        {
                            //индексация
                            if (!File.Exists(x.indexfile))
                            {
                                Indexing index = new Indexing(x);
                                if (index.m == null) return;
                                x = index.m.Clone();
                            }

                            //Добавление кэш-файла в список на удаление
                            if (!dgcache.Contains(x.indexfile) && Path.GetDirectoryName(x.indexfile).EndsWith(".index") && x.indexfile != x.infilepath)
                                dgcache.Add(x.indexfile);
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
                if (x.vdecoder == 0) x.vdecoder = Format.GetValidVDecoder(x);

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
                    Settings.FormatOut = x.format = Format.ExportFormats.Audio;
                    combo_format.SelectedItem = Format.EnumToString(Format.ExportFormats.Audio);
                    LoadAudioPresets();
                    SetAudioPreset();
                }

                //Индексация для FFmpegSource2
                if (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource2)
                {
                    Indexing_FFMS ffindex = new Indexing_FFMS(x);
                    if (ffindex.m == null) return;
                }

                if (x.inaudiostreams.Count > 0 && Settings.EnableAudio)
                {
                    //Автовыбор трека
                    if (x.inaudiostreams.Count > 1)
                    {
                        if (Settings.DefaultATrackMode == Settings.ATrackModes.Language)
                        {
                            //По языку
                            for (int i = 0; i < x.inaudiostreams.Count; i++)
                            {
                                if (((AudioStream)x.inaudiostreams[i]).language.ToLower() == Settings.DefaultATrackLang.ToLower())
                                {
                                    x.inaudiostream = i;
                                    break;
                                }
                            }
                        }
                        else if (Settings.DefaultATrackMode == Settings.ATrackModes.Number)
                        {
                            //По номеру
                            x.inaudiostream = Settings.DefaultATrackNum - 1;
                            if (x.inaudiostream >= x.inaudiostreams.Count)
                                x.inaudiostream = 0;
                        }

                        AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];

                        //Только FFmpegSource2 умеет переключать треки, для него их можно не извлекать
                        if (instream.audiopath == null && instream.decoder == 0 && x.inaudiostream > 0 &&
                            !(x.vdecoder == AviSynthScripting.Decoders.FFmpegSource2 && Settings.FFMS_Enable_Audio))
                        {
                            string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                            instream.audiopath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + outext;
                            instream.audiofiles = new string[] { instream.audiopath };
                            instream = Format.GetValidADecoder(instream);
                        }
                    }

                    //Извлечение звука для FFmpegSource2 и DirectShowSource, для DirectShowSource2 звук будет извлечен в Caching (тут, если это автовыбор)
                    if (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource2 && !Settings.FFMS_Enable_Audio ||
                        x.vdecoder == AviSynthScripting.Decoders.DirectShowSource && !Settings.DSS_Enable_Audio ||
                        ((AudioStream)x.inaudiostreams[x.inaudiostream]).audiopath != null && x.isvideo)
                    {
                        AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];
                        if (instream.audiopath == null || !File.Exists(instream.audiopath))
                        {
                            string outpath = (instream.audiopath == null) ? Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream +
                                Format.GetValidRAWAudioEXT(instream.codecshort) : instream.audiopath;

                            //удаляем старый файл
                            SafeDelete(outpath);

                            //извлекаем новый файл
                            if (Path.GetExtension(outpath) == ".wav")
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
                }

                //получаем выходной фреймрейт
                x = Format.GetValidFramerate(x);
                x = Calculate.UpdateOutFrames(x);

                //Получаем информацию через AviSynth и ловим ошибки
                Caching cach = new Caching(x);
                if (cach.m == null) return;
                x = cach.m.Clone();

                //забиваем-обновляем аудио массивы
                x = FillAudio(x);

                //выбираем трек
                if (x.inaudiostreams.Count > 1 && Settings.EnableAudio && Settings.DefaultATrackMode == Settings.ATrackModes.Manual)
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
                    if (IsBatchOpening && m != null && Settings.BatchCloneDeint)
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
                    //Клонируем разрешение от предыдущего файла
                    if (IsBatchOpening && m != null && Settings.BatchCloneAR)
                    {
                        x.outresw = m.outresw;
                        x.outresh = m.outresh;
                        x.cropl = x.cropl_copy = m.cropl;
                        x.cropt = x.cropt_copy = m.cropt;
                        x.cropr = x.cropr_copy = m.cropr;
                        x.cropb = x.cropb_copy = m.cropb;
                        x.blackw = m.blackw;
                        x.blackh = m.blackh;
                        x.flipv = m.flipv;
                        x.fliph = m.fliph;
                        x.outaspect = m.outaspect;
                        x.aspectfix = m.aspectfix;
                        x.sar = m.sar;
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
                                Autocrop acrop = new Autocrop(x, this, -1);
                                if (acrop.m != null) x = acrop.m.Clone();
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
                    if (IsBatchOpening && m != null && Settings.BatchCloneFPS)
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
                if (IsBatchOpening && m != null && Settings.BatchCloneTrim)
                {
                    x.trims = (ArrayList)m.trims.Clone();
                    x.trim_is_on = m.trim_is_on;
                    x.trim_num = m.trim_num;
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
                        if (norm.m != null)
                        {
                            x = norm.m.Clone();
                            x = AviSynthScripting.CreateAutoAviSynthScript(x);
                        }
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
                            " " + Format.EnumToString(x.format) + ": " + CopyProblems + "." + Environment.NewLine +
                            Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }

                //передаём массив
                m = x.Clone();
                x = null;

            finish:

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
                    ErrorException("LoadFile: " + ex.Message, ex.StackTrace);
            }
        }

        private void action_save(Massive mass)
        {
            if (mass != null)
            {
                if (currentState == PlayState.Running)
                    PauseClip();

                if (avsPlayer != null && !avsPlayer.IsError)
                    avsPlayer.UnloadAviSynth();

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

                        if (m.outaudiostreams.Count > 0)
                        {
                            //Клонируем исходные audiostreams чтоб потом восстановить их в массиве m, т.к. Mass.Clone() не клонирует,
                            //а создает связанные копии - баг, но его можно частично обойти :) (см. Massive.Clone())
                            AudioStream old_instream = ((AudioStream)m.inaudiostreams[m.inaudiostream]).Clone();
                            AudioStream old_outstream = ((AudioStream)m.outaudiostreams[m.outaudiostream]).Clone();

                            mass = UpdateOutAudioPath(mass);

                            //Восстанавливаем исходные audiostreams в массиве m,
                            //теперь audiostreams в m и в mass разделены
                            m.inaudiostreams[m.inaudiostream] = old_instream.Clone();
                            m.outaudiostreams[m.outaudiostream] = old_outstream.Clone();
                        }

                        //добавляем задание в список
                        AddTask(mass, TaskStatus.Waiting);
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
                        if (norm.m != null) mass = norm.m.Clone();
                    }

                    mass = AviSynthScripting.SetGain(mass);
                    UpdateTaskMassive(mass);
                }

                //Временный WAV-файл для 2pass AAC
                if (outstream.codec == "AAC" && mass.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass)
                {
                    if ((instream.audiofiles == null || instream.audiofiles.Length < 2) && File.Exists(instream.audiopath) && Path.GetExtension(instream.audiopath).ToLower() == ".wav" &&
                        !mass.trim_is_on && !m.testscript && instream.bits == outstream.bits && instream.channels == outstream.channels && instream.delay == outstream.delay &&
                        instream.gain == outstream.gain && instream.samplerate == outstream.samplerate && mass.script == AviSynthScripting.CreateAutoAviSynthScript(mass).script)
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
                            UpdateTaskStatus(mass.key, TaskStatus.Errors);
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
                ErrorException("RunEncoder: " + ex.Message, ex.StackTrace);
            }
        }

        private void CloseFile()
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            CloseClip();

            if (Settings.DeleteTempFiles)
            {
                clear_dgindex_cache();           // - Кэш от DGIndex
                clear_audio_and_video_caches();  // - Извлеченные или декодированные аудио и видео файлы
            }

            SafeDelete(Settings.TempPath + "\\preview.avs");

            m = null;
            ResetTrim();      //Обнуляем всё что связано с тримом
            MenuHider(false); //Делаем пункты меню неактивными

            if (IsExiting)
            {
                //Удаляем резервную копию заданий - при выходе
                SafeDelete(Settings.TempPath + "\\backup.tsks");
            }
            else
            {
                //Обновляем резервную копию заданий - при закрытии файла
                UpdateTasksBackup();
            }

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
                foreach (Task task in list_tasks.Items)
                {
                    if (task.Mass.infilepath == m.infilepath) //m.taskname, m.infilepath
                    {
                        busy = true; break;
                    }
                }

                //Удаляем кэши сразу, или помещаем их в список на удаление, если они участвует в кодировании
                foreach (AudioStream a in m.inaudiostreams)
                {
                    //Аудио
                    if (a.audiopath != null && Path.GetDirectoryName(a.audiopath) == Settings.TempPath)
                    {
                        //Защита от удаления исходников
                        bool garbage = (a.audiopath != m.infilepath);
                        foreach (string file in m.infileslist)
                        {
                            if (a.audiopath == file)
                            {
                                garbage = false; break;
                            }
                        }

                        if (garbage)
                        {
                            if (!busy) SafeDelete(a.audiopath);
                            else deletefiles.Add(a.audiopath);
                        }
                    }
                }

                //Видео
                if (Path.GetFileNameWithoutExtension(m.infilepath) != m.taskname &&
                    Path.GetDirectoryName(m.infilepath) == Settings.TempPath &&
                    m.infilepath_source != null)
                {
                    if (!busy) SafeDelete(m.infilepath);
                    else deletefiles.Add(m.infilepath);
                }
            }
            catch (Exception) { }
        }

        private void ErrorException(string message)
        {
            Message mes = new Message(this.IsLoaded ? this : null);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void ErrorException(string message, string info)
        {
            Message mes = new Message(this.IsLoaded ? this : null);
            mes.ShowMessage(message, info, Languages.Translate("Error"));
        }

        private void PreviewError(string text, Brush foreground)
        {
            ErrBox.Child = new TextBlock() { Text = text, Background = Brushes.Black, Foreground = foreground, TextAlignment = TextAlignment.Center, FontFamily = new FontFamily("Arial") };
            ErrBox.Visibility = Visibility.Visible;
        }

        private void LoadVideo(MediaLoad mediaload)
        {
            this.mediaload = mediaload;

            //Обновляем резервную копию заданий
            UpdateTasksBackup();

            //Сбрасываем флаг ошибки Ависинта при новом открытии
            if (mediaload == MediaLoad.load) IsAviSynthError = false;

            //Чтоб позиция не сбрасывалась на ноль при включенном ScriptView;
            //а так-же НЕ сохраняем позицию после ошибки Ависинта в предыдущее открытие
            if (script_box.Visibility == Visibility.Collapsed && !IsAviSynthError)
            { 
                oldpos = Position;
                if (avsPlayer != null && !avsPlayer.IsError)
                    avsFrame = avsPlayer.CurrentFrame;
            }

            // If we have ANY file open, close it and shut down DirectShow
            if (this.currentState != PlayState.Init)
                CloseClip();

            try
            {
                //Окно ошибок (если не закрылось в CloseClip)
                if (ErrBox.Visibility != Visibility.Collapsed)
                {
                    ErrBox.Child = null;
                    ErrBox.Visibility = Visibility.Collapsed;
                }

                //Пишем скрипт в файл
                AviSynthScripting.WriteScriptToFile(m.script, "preview");

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
                    this.IsAudioOnly = true;
                    if (mediaload == MediaLoad.load)
                        this.currentState = PlayState.Stopped;

                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                        PlayMovieInWindow(Settings.TempPath + "\\preview.avs");
                    else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                        PlayWithMediaBridge(Settings.TempPath + "\\preview.avs");
                    else
                        PlayWithAvsPlayer(Settings.TempPath + "\\preview.avs");

                    this.Focus();
                    slider_pos.Focus(); //Переводит фокус на полосу прокрутки видео

                    //Запускаем таймер обновления позиции
                    if (timer != null) timer.Start();
                }
            }
            catch (Exception ex)
            {
                CloseClip();

                if (mediaload == MediaLoad.load && ex.Message.Contains("DirectX"))
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("DirectX update required! Do it now?"),
                        Languages.Translate("Error"), Message.MessageStyle.YesNo);
                    if (mess.result == Message.Result.Yes)
                    {
                        m = null;
                        Process.Start(Calculate.StartupPath + "\\apps\\DirectX_Update\\dxwebsetup.exe");
                        Close();
                        return;
                    }
                }
                else
                {
                    ErrorException("LoadVideo: " + ex.Message, ex.StackTrace);
                    PreviewError(Languages.Translate("Error") + "...", Brushes.Red);
                }
            }

            //Делаем пункты меню активными
            if (mediaload == MediaLoad.load)
                MenuHider(true);

            textbox_name.Text = m.taskname;
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
                mnVideoDecoding.InputGestureText = HotKeys.GetKeys("Decoding");
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
                menu_createtestscript.InputGestureText = HotKeys.GetKeys("Apply test script");
                menu_save_script.InputGestureText = HotKeys.GetKeys("Save script");
                menu_run_script.InputGestureText = HotKeys.GetKeys("Run script");
                menu_mt_settings.InputGestureText = HotKeys.GetKeys("MT settings");
                menu_playinwmp.InputGestureText = HotKeys.GetKeys("Windows Media Player");
                menu_playinmpc.InputGestureText = HotKeys.GetKeys("Media Player Classic");
                menu_playinwpf.InputGestureText = HotKeys.GetKeys("WPF Video Player");
                //Settings
                menu_settings.InputGestureText = HotKeys.GetKeys("Global settings");
                //Tools
                menu_info_media.InputGestureText = HotKeys.GetKeys("Media Info");
                menu_ffrebuilder.InputGestureText = HotKeys.GetKeys("FFRebuilder");
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

                menu_avsp.Header = Languages.Translate("AvsP editor");
                menu_editscript.Header = Languages.Translate("Edit filtering script");
                menu_createautoscript.Header = Languages.Translate("Create auto script");
                mnUpdateVideo.Header = cmn_refresh.Header = Languages.Translate("Refresh preview");
                menu_createtestscript.Header = Languages.Translate("Apply test script");
                menu_save_script.Header = Languages.Translate("Save script");
                menu_run_script.Header = Languages.Translate("Run script");
                menu_mt_settings.Header = Languages.Translate("MT settings");

                mnAspectResolution.Header = Languages.Translate("Resolution/Aspect") + "...";
                menu_interlace.Header = Languages.Translate("Interlace/Framerate") + "...";

                mnAddSubtitles.Header = Languages.Translate("Add");
                mnRemoveSubtitles.Header = Languages.Translate("Remove");

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

                menu_auto_deinterlace.Header = Languages.Translate("Analyse interlace");

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
                cmenu_save_tasks.Header = Languages.Translate("Save tasks list");
                cmenu_save_tasks.ToolTip = Languages.Translate("This option is experimental!");

                menu_directx_update.Header = Languages.Translate("Update DirectX");
                menu_autocrop.Header = Languages.Translate("Detect black borders");
                menu_detect_interlace.Header = Languages.Translate("Detect interlace");
                menu_home.Header = Languages.Translate("Home page");
                menu_support.Header = Languages.Translate("Support forum");
                //menu_donate.Header = Languages.Translate("Donate");
                menu_avisynth_guide_en.Header = Languages.Translate("AviSynth guide") + " (EN)";
                menu_avisynth_guide_ru.Header = Languages.Translate("AviSynth guide") + " (RU)";

                if (m != null)
                {
                    SetTrimsButtons();
                }
                else
                {
                    button_set_start.Content = Languages.Translate("Set Start");
                    button_set_end.Content = Languages.Translate("Set End");
                    button_apply_trim.Content = Languages.Translate("Apply Trim");
                }
                ToolTipService.SetShowDuration(button_apply_trim, 100000);
                ToolTipService.SetShowDuration(target_goto, 10000);
                textbox_start.ToolTip = Languages.Translate("Enter frame number or time position (HH:MM:SS.ms or #h #m #s #ms), then press \"BUTTON\" button.").Replace("BUTTON", Languages.Translate("Set Start")) +
                    "\r\n" + Languages.Translate("If you leave this field empty, then current frame number will be entered automatically.");
                textbox_end.ToolTip = Languages.Translate("Enter frame number or time position (HH:MM:SS.ms or #h #m #s #ms), then press \"BUTTON\" button.").Replace("BUTTON", Languages.Translate("Set End")) +
                    "\r\n" + Languages.Translate("If you leave this field empty, then current frame number will be entered automatically.");
                button_trim_plus.ToolTip = Languages.Translate("Next/New region");
                button_trim_minus.ToolTip = Languages.Translate("Previous region");
                button_trim_delete.ToolTip = Languages.Translate("Delete current region");
                menu_open_folder.Header = Languages.Translate("Open folder") + "...";
                mnApps_Folder.Header = Languages.Translate("Open XviD4PSP folder");
                menu_info_media.ToolTip = Languages.Translate("Provides exhaustive information about the open file.") + Environment.NewLine + Languages.Translate("You can manually choose a file to open and select the type of information to show too.");
                target_goto.ToolTip = Languages.Translate("Frame counter. Click on this area to enter frame number to go to.") + "\r\n" + Languages.Translate("Rigth-click will insert current frame number.") +
                    "\r\n" + Languages.Translate("You can also enter a time (HH:MM:SS.ms or #h #m #s #ms).") + "\r\n\r\n" + Languages.Translate("Enter - apply, Esc - cancel.");

                check_pictureview_drop.Header = Languages.Translate("Drop frames");
                check_pictureview_drop.ToolTip = Languages.Translate("Allow frame dropping to preserve nominal playback speed");
                check_pictureview_audio.Header = Languages.Translate("Enable audio");
                check_pictureview_audio.ToolTip = Languages.Translate("Allow audio output. Note that this may or may not work and that the audio/video sync is also not guaranteed!");
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

            Settings.VRenderers vr = Settings.VideoRenderer;
            if (vr == Settings.VRenderers.Auto) vr_default.IsChecked = true;
            else if (vr == Settings.VRenderers.Overlay) vr_overlay.IsChecked = true;
            else if (vr == Settings.VRenderers.VMR7) vr_vmr7.IsChecked = true;
            else if (vr == Settings.VRenderers.VMR9) vr_vmr9.IsChecked = true;
            else if (vr == Settings.VRenderers.EVR) vr_evr.IsChecked = true;

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
            slider_Volume.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Volume_ValueChanged); //Не в xaml, чтоб не срабатывал до загрузки
            SetVolumeIcon();

            check_old_seeking.IsChecked = OldSeeking = Settings.OldSeeking;
            check_pictureview_drop.IsChecked = Settings.PictureViewDropFrames;
            check_pictureview_audio.IsChecked = Settings.PictureViewAudio;
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
                if (OldSeeking)
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
                else if (avsPlayer != null && !avsPlayer.IsError)
                {
                    if (currentState == PlayState.Running) PauseClip();
                    int new_frame = avsPlayer.CurrentFrame + step;
                    new_frame = (new_frame < 0) ? 0 : (new_frame > avsPlayer.TotalFrames) ? avsPlayer.TotalFrames : new_frame;
                    if (avsPlayer.CurrentFrame != new_frame) avsPlayer.SetFrame(new_frame);
                }
            }
            catch (Exception ex)
            {
                ErrorException("FrameShift: " + ex.Message, ex.StackTrace);
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
                if (source != null) source.RemoveHook(new HwndSourceHook(WndProc));
            }
            else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
            {
                VideoElement.MediaOpened -= new RoutedEventHandler(VideoElement_MediaOpened);
                VideoElement.MediaEnded -= new RoutedEventHandler(VideoElement_MediaEnded);
            }

            //Добавляем новое
            if (((MenuItem)sender).Header.ToString() == "DirectShow")
            {
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

                check_engine_mediabridge.IsChecked = true;
                Settings.PlayerEngine = Settings.PlayerEngines.MediaBridge;
            }
            else
            {
                check_engine_pictureview.IsChecked = true;
                Settings.PlayerEngine = Settings.PlayerEngines.PictureView;
            }

            currentState = cstate;

            //Обновляем иконку громкости
            SetVolumeIcon();

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
                button_play.Visibility = button_frame_back.Visibility = button_frame_forward.Visibility = slider_pos.Visibility = Visibility.Visible;
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
            try
            {
                if (m != null)
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
                else
                {
                    new Filtering(null, this);
                    LoadFilteringPresets();
                }
            }
            catch (Exception ex)
            {
                ErrorException("EditScript: " + ex.Message, ex.StackTrace);
            }
        }

        public void LoadFilteringPresets()
        {
            //загружаем список фильтров
            combo_filtering.Items.Clear();
            combo_filtering.Items.Add(new ComboBoxItem() { Content = "Disabled" });
            try
            {
                foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\filtering", "*.avs"))
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = Path.GetFileNameWithoutExtension(file);

                    if (Settings.ShowFToolTips)
                    {
                        item.ToolTipOpening += new ToolTipEventHandler(filtering_ToolTipOpening);
                        item.ToolTip = ""; //Временная пустышка
                    }

                    combo_filtering.Items.Add(item);
                }
            }
            catch (Exception) { }

            //прописываем текущий фильтр
            string preset = Settings.Filtering;
            foreach (ComboBoxItem item in combo_filtering.Items)
            {
                if (item.Content.ToString() == preset)
                {
                    combo_filtering.SelectedItem = item;
                    return;
                }
            }
            combo_filtering.SelectedValue = Settings.Filtering = "Disabled";
        }

        private void filtering_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            ComboBoxItem item = ((ComboBoxItem)sender);
            if (item.Tag != null) return;
            item.Tag = true;

            try
            {
                //Поиск и считывание нужного нам комментария
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\filtering\\" + item.Content + ".avs", System.Text.Encoding.Default))
                {
                    ArrayList in_lines = new ArrayList();
                    ArrayList out_lines = new ArrayList();
                    while (!sr.EndOfStream) in_lines.Add(sr.ReadLine());

                    bool got_something = false;
                    for (int i = in_lines.Count - 1; i >= 0; i--)
                    {
                        string line = in_lines[i].ToString().Trim();
                        if (line.StartsWith("#"))
                        {
                            //Обрезаем только первый символ #
                            out_lines.Add(line.Substring(1).Trim());
                            got_something = true;
                        }
                        else if (got_something || line.Length > 0)
                        {
                            //Это уже сам пресет
                            break;
                        }
                    }

                    if (out_lines.Count == 0)
                    {
                        //Пусто - отключаем тултип
                        item.ToolTip = null;
                        e.Handled = true;
                    }
                    else
                    {
                        //Не пусто - переворачиваем текст обратно
                        for (int i = out_lines.Count - 1; i >= 0; i--)
                            item.ToolTip += out_lines[i] + ((i > 0) ? Environment.NewLine : null);
                    }
                }
            }
            catch (Exception)
            {
                item.ToolTip = null;
                e.Handled = true;
            }
        }

        private void AspectResolution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null) new AspectResolution(null, this);
            else if (m.format == Format.ExportFormats.Audio)
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
            Decoders_Settings ds = new Decoders_Settings(m, this, (sender == mnAudioDecoding ? 2 : 1));

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
            bool restore = false;

            //Переключились на MPEG2Source
            if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && old_m.vdecoder != AviSynthScripting.Decoders.MPEG2Source)
            {
                //Если индекс-файла нет, то надо индексировать (со всеми проверками - проще открыть файл заново)
                if ((string.IsNullOrEmpty(m.indexfile) || !File.Exists(m.indexfile)))
                {
                    //Но проверить на МПЕГ можно и тут
                    if (m.invcodecshort == "MPEG1" || m.invcodecshort == "MPEG2")
                    {
                        Massive x = m.Clone();
                        x.inaudiostreams = new ArrayList();
                        x.outaudiostreams = new ArrayList();

                        CloseFile();
                        action_open(x);
                        return;
                    }
                    else
                    {
                        restore = true;
                        goto finish;
                    }
                }
            }

            //Переключились на FFmpegSource2
            if (m.vdecoder == AviSynthScripting.Decoders.FFmpegSource2 && old_m.vdecoder != AviSynthScripting.Decoders.FFmpegSource2)
            {
                //Индексация для FFmpegSource2
                Indexing_FFMS ffindex = new Indexing_FFMS(m);
                if (ffindex.m == null)
                {
                    restore = true;
                    goto finish;
                }
            }

            //Получаем информацию через AviSynth и ловим ошибки
            Caching cach = new Caching(m);
            if (cach.m == null)
            {
                restore = true;
                goto finish;
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

        finish:
            if (restore)
            {
                //Восстанавливаем массив из сохраненного
                m = old_m.Clone();
                old_m = null;
            }
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
                    ErrorException("SaveScript: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private void cmenu_save_all_scripts_Click(object sender, RoutedEventArgs e)
        {
            if (list_tasks.Items.Count == 0) return;
            try
            {
                foreach (Task task in list_tasks.Items)
                {
                    string path = Path.GetDirectoryName(task.Mass.outfilepath) + "\\" + Path.GetFileNameWithoutExtension(task.Mass.outfilepath) + ".avs";
                    WriteScriptToFile(task.Mass, path);
                }
                new Message(this).ShowMessage(Languages.Translate("Complete"), "OK");
            }
            catch (Exception ex)
            {
                ErrorException("SaveScripts: " + ex.Message, ex.StackTrace);
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
                foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\video", "*txt"))
                    combo_vencoding.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }
            combo_vencoding.Items.Add("Disabled");
            combo_vencoding.Items.Add("Copy");
        }

        private void SetVideoPreset()
        {
            Format.ExportFormats format = (m != null) ? m.format : Settings.FormatOut;
            string preset = ChooseVideoPreset(format);

            combo_vencoding.SelectedItem = preset;
            Settings.SetVEncodingPreset(format, preset);
        }

        private string ChooseVideoPreset(Format.ExportFormats format)
        {
            //Сначала пробуем пресет из настроек
            string preset = Settings.GetVEncodingPreset(format);
            if (!combo_vencoding.Items.Contains(preset))
            {
                //Потом дефолтный для данного формата
                preset = Format.GetValidVPreset(format);
                if (!combo_vencoding.Items.Contains(preset))
                {
                    //Если и его нет - то берём самый первый (или "Copy")
                    if (combo_vencoding.Items.Count == 0) preset = "Copy";
                    else preset = combo_vencoding.Items[0].ToString();

                    //Но только не "Disabled"
                    if (preset == "Disabled") preset = "Copy";
                }
            }

            return preset;
        }

        private void LoadAudioPresets()
        {
            string format = Format.EnumToString((m != null) ? m.format : Settings.FormatOut);

            combo_aencoding.Items.Clear();
            try
            {
                foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\audio", "*.txt"))
                    combo_aencoding.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }
            combo_aencoding.Items.Add("Disabled");
            combo_aencoding.Items.Add("Copy");
        }

        private void SetAudioPreset()
        {
            //прописываем текущий пресет кодирования
            if (m != null && m.outaudiostreams.Count == 0)
            {
                combo_aencoding.SelectedItem = "Disabled";
            }
            else
            {
                Format.ExportFormats format = (m != null) ? m.format : Settings.FormatOut;
                string preset = ChooseAudioPreset(format);

                combo_aencoding.SelectedItem = preset;
                Settings.SetAEncodingPreset(format, preset);
            }
        }

        private string ChooseAudioPreset(Format.ExportFormats format)
        {
            //Сначала пробуем пресет из настроек
            string preset = Settings.GetAEncodingPreset(format);
            if (!combo_aencoding.Items.Contains(preset))
            {
                //Потом дефолтный для данного формата
                preset = Format.GetValidAPreset(format);
                if (!combo_aencoding.Items.Contains(preset))
                {
                    //Если и его нет - то берём самый первый (или "Copy")
                    if (combo_aencoding.Items.Count == 0) preset = "Copy";
                    else preset = combo_aencoding.Items[0].ToString();

                    //Но только не "Disabled"
                    if (preset == "Disabled") preset = "Copy";
                }
            }

            return preset;
        }

        private void combo_format_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_format.IsDropDownOpen || combo_format.IsSelectionBoxHighlighted) && combo_format.SelectedItem != null)
            {
                Settings.FormatOut = Format.StringToEnum(combo_format.SelectedItem.ToString());

                if (m != null)
                {
                    m.format = Settings.FormatOut;

                    //загружаем профили
                    if (Settings.FormatOut != Format.ExportFormats.Audio)
                        LoadVideoPresets();
                    LoadAudioPresets();

                    //Подбираем подходящий аудио пресет
                    Settings.SetAEncodingPreset(m.format, ChooseAudioPreset(m.format));

                    //забиваем-обновляем аудио массивы
                    m = FillAudio(m);

                    //Подбираем подходящий видео пресет
                    if (Settings.FormatOut == Format.ExportFormats.Audio)
                    {
                        m.vencoding = "Disabled";
                        m.outvcodec = "Disabled";
                        m.vpasses.Clear();
                    }
                    else
                    {
                        m.vencoding = ChooseVideoPreset(m.format);
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

                    m.outfilesize = Calculate.GetEncodingSize(m);
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
                    SetVideoPreset();
                SetAudioPreset();

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
            if ((combo_filtering.IsDropDownOpen || combo_filtering.IsSelectionBoxHighlighted) && combo_filtering.SelectedItem != null)
            {
                Settings.Filtering = ((ComboBoxItem)combo_filtering.SelectedItem).Content.ToString();

                if (m != null)
                {
                    m.filtering = ((ComboBoxItem)combo_filtering.SelectedItem).Content.ToString();

                    //создаём новый AviSynth скрипт
                    m.filtering_changed = true;
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    m.filtering_changed = false;

                    //загружаем обновлённый скрипт
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
                }
            }
        }

        private void combo_sbc_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_sbc.IsDropDownOpen || combo_sbc.IsSelectionBoxHighlighted) && combo_sbc.SelectedItem != null)
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

        private void combo_aencoding_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_aencoding.IsDropDownOpen || combo_aencoding.IsSelectionBoxHighlighted) && combo_aencoding.SelectedItem != null)
            {
                Format.ExportFormats format;
                if (m == null) format = Settings.FormatOut;
                else format = m.format;

                if (format == Format.ExportFormats.Audio &&
                    combo_aencoding.SelectedItem.ToString() == "Disabled")
                {
                    if (m != null && m.inaudiostreams.Count > 0) combo_aencoding.SelectedItem = "MP3 CBR 128k";
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

        private void combo_vencoding_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_vencoding.IsDropDownOpen || combo_vencoding.IsSelectionBoxHighlighted) && combo_vencoding.SelectedItem != null)
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

        private void VideoEncodingSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null && m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else if (!(m == null && Settings.FormatOut == Format.ExportFormats.Audio))
            {
                VideoEncoding enc = new VideoEncoding(m, this);

                if (m != null) m = enc.m.Clone();
                Format.ExportFormats format = enc.m.format;
                string vencoding = enc.m.vencoding;

                LoadVideoPresets();

                //защита от удаления профиля
                if (!combo_vencoding.Items.Contains(vencoding))
                    vencoding = ChooseVideoPreset(format);

                combo_vencoding.SelectedItem = vencoding;
                Settings.SetVEncodingPreset(format, vencoding);

                if (m != null)
                {
                    //проверка на размер
                    //m.outfilesize = Calculate.GetEncodingSize(m);

                    UpdateTaskMassive(m);
                    ValidateTrimAndCopy(m);
                }
            }
        }

        private void AudioEncodingSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null && (m.inaudiostreams.Count == 0 || m.outaudiostreams.Count == 0))
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have audio streams!"), Languages.Translate("Error"));
            }
            else
            {
                if (m == null)
                {
                    AudioEncoding enc = new AudioEncoding(null, this);
                    LoadAudioPresets();

                    string aencoding = ((AudioStream)enc.m.outaudiostreams[enc.m.outaudiostream]).encoding;

                    //защита от удаления профиля
                    if (!combo_aencoding.Items.Contains(aencoding))
                        aencoding = ChooseAudioPreset(enc.m.format);
                    combo_aencoding.SelectedItem = aencoding;

                    if (aencoding != "Disabled")
                        Settings.SetAEncodingPreset(enc.m.format, aencoding);
                }
                else
                {
                    //определяем аудио потоки
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                    //Запоминаем старый кодер
                    string old_codec = outstream.codec;

                    AudioEncoding enc = new AudioEncoding(m, this);
                    m = enc.m.Clone();
                    LoadAudioPresets();

                    //защита от удаления профиля
                    if (!combo_aencoding.Items.Contains(outstream.encoding))
                        outstream.encoding = ChooseAudioPreset(m.format);
                    combo_aencoding.SelectedItem = outstream.encoding;

                    if (outstream.encoding != "Disabled")
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
                    ReloadChildWindows();
                    ValidateTrimAndCopy(m);
                }
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
                ErrorException("SafeFileDelete: " + ex.Message, ex.StackTrace);
            }
        }

        private void SafeDirDelete(string dir, bool recursive)
        {
            try
            {
                if (!Directory.Exists(dir)) return;

                if (recursive)
                {
                    //Удаляем папку целиком со всем содержимым
                    Directory.Delete(dir, true);
                }
                else
                {
                    //Удаляем все файлы, после чего саму папку..
                    foreach (string file in Directory.GetFiles(dir))
                        File.Delete(file);

                    //..если в ней нет никаких подпапок
                    if (Directory.GetDirectories(dir).Length == 0)
                        Directory.Delete(dir, false);
                }
            }
            catch (Exception ex)
            {
                ErrorException("SafeDirDelete: " + ex.Message, ex.StackTrace);
            }
        }

        private void DD_GotFiles(object sender, string[] files)
        {
            if (!IsDragOpening)
            {
                IsDragOpening = true;
                drop_data = files;
                new Thread(new ThreadStart(this.DragOpen)).Start();
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
                try
                {
                    if (drop_data.Length == 1) //Обычное открытие
                    {
                        //Копирование exe-файлов
                        if (Path.GetFileName(drop_data[0]).ToLower().EndsWith(".exe"))
                        {
                            bool _10bit = (m != null && m.outvcodec == "x264" && m.x264options.profile == x264.Profiles.High10);
                            string file_c = "", path_c = "", file_d = Path.GetFileName(drop_data[0]).ToLower();
                            if (file_d == "x264.exe") { path_c = Calculate.StartupPath + ((_10bit) ? "\\apps\\x264_10b\\" : "\\apps\\x264\\"); file_c = "x264.exe"; }
                            else if (file_d == "x264_64.exe") { path_c = Calculate.StartupPath + ((_10bit) ? "\\apps\\x264_10b\\" : "\\apps\\x264\\"); file_c = "x264_64.exe"; }
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
                            path_to_save = OpenDialogs.SaveFolder();
                            if (path_to_save == null) { IsDragOpening = false; return; }
                        }
                        MultiOpen(drop_data);
                    }
                }
                catch (Exception ex)
                {
                    ErrorException("DragOpen: " + ex.Message, ex.StackTrace);
                }
                finally
                {
                    IsDragOpening = false;
                }
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
                    Settings.SetAEncodingPreset(m.format, outstream.encoding);
                }
                else
                {
                    combo_aencoding.SelectedItem = "Disabled";
                }

                combo_format.SelectedItem = Format.EnumToString(Settings.FormatOut = m.format);
                combo_sbc.SelectedItem = Settings.SBC = m.sbc;
                combo_filtering.SelectedValue = Settings.Filtering = m.filtering;
                combo_vencoding.SelectedItem = m.vencoding;
                Settings.SetVEncodingPreset(m.format, m.vencoding);

                //запоминаем выделенное задание
                //OldSelectedIndex = list_tasks.SelectedIndex;
                //IsTaskSelection = true;

                if (script != m.script)
                    LoadVideo(MediaLoad.load);
                else
                    MenuHider(true);

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
                if (sender == cmenu_up) index = list_tasks.SelectedIndex - 1;
                else if (sender == cmenu_down) index = list_tasks.SelectedIndex + 1;
                else if (sender == cmenu_last) index = list_tasks.Items.Count - 1;

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

                if (task.Status == TaskStatus.Encoding)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("The tasks in encoding process are disabled for removal!"), Languages.Translate("Error"));
                    return;
                }

                list_tasks.Items.Remove(list_tasks.SelectedItem);
                if (outfiles.Contains(task.Mass.outfilepath))
                    outfiles.Remove(task.Mass.outfilepath);

                UpdateTasksBackup();
            }
        }

        private void AddTask(Massive mass, TaskStatus status)
        {
            list_tasks.Items.Add(new Task("THM", status, mass));
            UpdateTasksBackup();
        }

        public void UpdateTaskStatus(string key, TaskStatus status)
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
                if (task.Status == TaskStatus.Encoded && status == TaskStatus.Waiting)
                    outfiles.Add(task.Mass.outfilepath);

                IsInsertAction = true;
                list_tasks.Items.RemoveAt(index);
                task.Status = status;
                list_tasks.Items.Insert(index, task);
                IsInsertAction = false;

                UpdateTasksBackup();
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
                if (task.Id == mass.key && task.Status != TaskStatus.Encoding)
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
                list_tasks.Items.Insert(task_index, new Task(task.THM, TaskStatus.Waiting, mass.Clone()));
                list_tasks.SelectedIndex = task_index;
                IsInsertAction = false;

                UpdateTasksBackup();
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
            {
                list_tasks.Items.Remove(task_for_delete);
                UpdateTasksBackup();
            }
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
                    if (task.Status == TaskStatus.Waiting)
                    {
                        IsWaiting = true;
                        all_clear = false;
                        break;
                    }
                    else if (task.Status == TaskStatus.Encoding || task.Status == TaskStatus.Errors)
                    {
                        all_clear = false;
                    }
                }

                if (IsWaiting)
                {
                    UpdateTaskStatus(task.Id, TaskStatus.Encoding);
                    action_encode(task.Mass.Clone());
                }
            }

            return all_clear;
        }

        private void button_encode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null && PauseAfterFirst)
            {
                action_save(m.Clone());
            }
            else if (m != null || list_tasks.Items.Count > 0)
            {
                bool IsEncoding = false;
                bool IsWaiting = false;
                foreach (Task task in list_tasks.Items)
                {
                    if (task.Status == TaskStatus.Encoding) IsEncoding = true;
                    if (task.Status == TaskStatus.Waiting) IsWaiting = true;
                }

                if (IsEncoding && IsWaiting)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("Do you want to run one more encoding thread?"), Languages.Translate("Question"), Message.MessageStyle.YesNo);
                    if (mes.result == Message.Result.No)
                        return;
                }

                if (!IsWaiting && m != null)
                {
                    action_save(m.Clone());
                }
                EncodeNextTask();
            }
        }

        private void list_tasks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (list_tasks.SelectedItem != null)
            {
                Task task = (Task)list_tasks.SelectedItem;
                if (task.Status == TaskStatus.Encoded)
                {
                    //добавлем задание в лок
                    outfiles.Add(task.Mass.outfilepath);
                    //обновляем список
                    UpdateTaskStatus(task.Id, TaskStatus.Waiting);
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
                if (task.Status != TaskStatus.Encoding)
                    UpdateTaskStatus(task.Id, TaskStatus.Waiting);
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
                foreach (Task task in list_tasks.Items)
                {
                    if (task.Status == TaskStatus.Encoded)
                        ready.Add(task);
                }

                foreach (Task task in ready)
                    list_tasks.Items.Remove(task);

                UpdateTasksBackup();
            }
        }

        private void cmenu_delete_all_tasks_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_tasks.HasItems)
            {
                ArrayList fordelete = new ArrayList();
                foreach (Task task in list_tasks.Items)
                {
                    if (task.Status != TaskStatus.Encoding)
                    {
                        fordelete.Add(task);
                        if (outfiles.Contains(task.Mass.outfilepath))
                            outfiles.Remove(task.Mass.outfilepath);
                    }
                }

                foreach (Task task in fordelete)
                    list_tasks.Items.Remove(task);

                UpdateTasksBackup();
            }
        }

        private void cmenu_is_always_delete_encoded_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoDeleteTasks = cmenu_is_always_delete_encoded.IsChecked;
        }

        private void ColorCorrection_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null && m.outvcodec == "Copy")
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
            }
            else if (m != null && m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File doesn`t have video streams!"), Languages.Translate("Error"));
            }
            else
            {
                if (m == null)
                {
                    ColorCorrection col = new ColorCorrection(null, this);
                    LoadSBCPresets();

                    //прописываем текущий профиль
                    if (combo_sbc.Items.Contains(col.m.sbc))
                        combo_sbc.SelectedItem = col.m.sbc;
                    else
                    {
                        //Видимо профиль был удален, сбрасываем всё на дефолты
                        combo_sbc.SelectedItem = col.m.sbc = "Disabled";
                    }

                    Settings.SBC = col.m.sbc;
                }
                else
                {
                    
                    ColorCorrection col = new ColorCorrection(m, this);

                    string old_histogram = m.histogram;
                    bool old_dither = m.tweak_dither;
                    bool old_nocoring = m.tweak_nocoring;
                    bool old_colormatrix = m.iscolormatrix;
                    double old_saturation = m.saturation;
                    double old_contrast = m.contrast;
                    int old_brightness = m.brightness;
                    int old_hue = m.hue;

                    m = col.m.Clone();
                    LoadSBCPresets();

                    //прописываем текущий профиль
                    if (combo_sbc.Items.Contains(m.sbc))
                        combo_sbc.SelectedItem = m.sbc;
                    else
                    {
                        //Видимо профиль был удален, сбрасываем всё на дефолты
                        combo_sbc.SelectedItem = m.sbc = "Disabled";
                        m = ColorCorrection.DecodeProfile(m);
                    }

                    Settings.SBC = m.sbc;

                    //обновление при необходимости
                    if (old_histogram != m.histogram ||
                        old_dither != m.tweak_dither ||
                        old_nocoring != m.tweak_nocoring ||
                        old_colormatrix != m.iscolormatrix ||
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
        }

        private void LoadSBCPresets()
        {
            //загружаем списки профилей цвето коррекции
            combo_sbc.Items.Clear();
            combo_sbc.Items.Add("Disabled");
            try
            {
                foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\sbc", "*.avs"))
                    combo_sbc.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }
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

        private void menu_run_script_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null) { ScriptRunner sr = new ScriptRunner(m.script); }
        }

        private void menu_mt_settings_Click(object sender, RoutedEventArgs e)
        {
            MT_Settings mt_s = new MT_Settings(m, this);
            if (m != null && mt_s.NeedUpdate)
            {
                string oldscript = m.script;
                m.script = mt_s.m.script;

                //обновление при необходимости
                if (m.script.Trim() != oldscript.Trim())
                {
                    LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
                }
            }
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
                    string script_path = Settings.TempPath + "\\preview.avs";
                    if (!File.Exists(script_path)) AviSynthScripting.WriteScriptToFile(m.script, "preview");
                    info.Arguments = "\"" + script_path + "\"";
                }
                Process pr = new Process();
                pr.StartInfo = info;
                pr.Start();
            }
            catch (Exception ex)
            {
                ErrorException("PlayIn: " + ex.Message, ex.StackTrace);
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
                ErrorException("EditPlayerPath: " + ex.Message, ex.StackTrace);
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
                    Autocrop acrop = new Autocrop(m, this, -1);
                    if (acrop.m != null)
                    {
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
        }

        public void SwitchToFullScreen()
        {
            if (this.IsAudioOnly || (this.graphBuilder == null && script_box.Visibility != Visibility.Visible && Pic.Visibility != Visibility.Visible))
                if (!IsFullScreen && ErrBox.Visibility != Visibility.Visible) return; //Если файл был закрыт при фуллскрине, продолжаем, чтоб вернуть нормальный размер окна

            //Если не Фуллскрин, то делаем Фуллскрин
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

                if (graphBuilder != null || Pic.Visibility != Visibility.Collapsed)
                    MoveVideoWindow();

                script_box.Margin = ErrBox.Margin = new Thickness(0, 0, 0, 38);
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

                if (graphBuilder != null || Pic.Visibility != Visibility.Collapsed)
                    MoveVideoWindow();

                script_box.Margin = ErrBox.Margin = new Thickness(8, 56, 8, 8);
            }
            slider_pos.Focus();
        }

        private void CloseClip()
        {
            try
            {
                //Останавливаем таймер обновления позиции
                if (timer != null) timer.Stop();

                //DirectShow
                if (this.graphBuilder != null && this.VideoElement.Source == null)
                {
                    int hr = 0;

                    // Stop media playback
                    if (this.mediaControl != null)
                        hr = this.mediaControl.Stop();

                    // Free DirectShow interfaces
                    CloseInterfaces();

                    //EVR
                    if (VHost != null)
                    {
                        VHost.Dispose();
                        VHost = null;
                        VHandle = IntPtr.Zero;
                        VHostElement.Child = null;
                        VHostElement.Visibility = Visibility.Collapsed;
                        VHostElement.Width = VHostElement.Height = 0;
                        VHostElement.Margin = new Thickness(0);
                    }

                    // No current media state
                    if (mediaload != MediaLoad.update)
                        this.currentState = PlayState.Init;
                }

                //MediaBridge
                if (this.VideoElement.Source != null)
                {
                    VideoElement.Stop();
                    VideoElement.Close();
                    VideoElement.Source = null;
                    VideoElement.Visibility = Visibility.Collapsed;
                    VideoElement.Width = VideoElement.Height = 0;
                    VideoElement.Margin = new Thickness(0);

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

                //ScriptView
                if (script_box.Visibility != Visibility.Collapsed)
                {
                    script_box.Clear();
                    script_box.Visibility = Visibility.Collapsed;
                    this.currentState = PlayState.Init;
                }

                //AviSynthPlayer (PictureView)
                if (avsPlayer != null)
                {
                    avsPlayer.Abort();
                    avsPlayer.Close();
                    avsPlayer = null;

                    Pic.Source = null;
                    Pic.Visibility = Visibility.Collapsed;
                    Pic.Margin = new Thickness(0);
                    Pic.Width = Pic.Height = 0;

                    if (mediaload != MediaLoad.update)
                        this.currentState = PlayState.Init;
                }

                //Окно ошибок
                if (ErrBox.Visibility != Visibility.Collapsed)
                {
                    ErrBox.Child = null;
                    ErrBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex) 
            {
                ErrorException("CloseClip: " + ex.Message, ex.StackTrace);
            }

            //update titles
            textbox_name.Text = textbox_frame.Text = "";
            textbox_time.Text = textbox_duration.Text = "00:00:00";
            progress_top.Width = slider_pos.Value = 0.0;

            SetPlayIcon();
        }

        private void CloseInterfaces()
        {
            int hr = 0;
            try
            {
                lock (locker)
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

        private void PlayWithAvsPlayer(string scriptPath)
        {
            avsPlayer = new AviSynthPlayer(this);
            avsPlayer.EnableAudio = Settings.PictureViewAudio;
            avsPlayer.AllowDropFrames = Settings.PictureViewDropFrames;
            avsPlayer.PlayerError += new AvsPlayerError(AvsPlayerError);

            avsPlayer.Open(scriptPath);
            if (avsPlayer.IsError)
                return;

            IsAviSynthError = false;
            IsAudioOnly = !avsPlayer.HasVideo;
            avsPlayer.Volume = VolumeSet;

            if (!IsAudioOnly)
            {
                int frame = 0;
                if (mediaload == MediaLoad.load)
                {
                    //Новое открытие (сразу Play или переход на середину)
                    if (Settings.AfterImportAction == Settings.AfterImportActions.Play)
                        currentState = PlayState.Running;
                    else if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                        frame = avsPlayer.TotalFrames / 2;
                }
                else
                {
                    //Ограничиваем кол-во кадров (могло измениться, например, после Trim`а)
                    frame = (avsFrame > avsPlayer.TotalFrames) ? avsPlayer.TotalFrames : avsFrame;
                }

                avsPlayer.SetFrame(frame);
                if (avsPlayer.IsError)
                    return;

                avsPlayer.PlayerFinished += new AvsPlayerFinished(AvsPlayerFinished);
                avsPlayer.AvsStateChanged += new AvsState(AvsStateChanged);
                Pic.Source = avsPlayer.BitmapSource;
                Pic.Visibility = Visibility.Visible;

                if (currentState == PlayState.Running)
                {
                    //Запуск сразу с Play
                    avsPlayer.Play();
                    SetPauseIcon();
                }
                else
                {
                    //Запуск на паузе
                    avsPlayer.Pause();
                    this.currentState = PlayState.Paused;
                    avsPlayer.UnloadAviSynth();
                }

                MoveVideoWindow();

                avsFrame = frame;                                                                     //Текущий кадр
                fps = avsPlayer.Framerate;                                                            //fps скрипта
                slider_pos.Maximum = TimeSpan.FromSeconds(avsPlayer.TotalFrames / fps).TotalSeconds;  //Устанавливаем максимум для ползунка

                //Текущий кадр и общая продолжительность клипа
                textbox_frame.Text = frame + "/" + (total_frames = avsPlayer.TotalFrames.ToString());
                textbox_duration.Text = TimeSpan.Parse(TimeSpan.FromSeconds(avsPlayer.TotalFrames / fps).ToString().Split('.')[0]).ToString();
            }
            else
            {
                PreviewError("NO VIDEO", Brushes.Gainsboro);
            }
        }

        private void AvsPlayerError(object sender, Exception ex)
        {
            Pic.Source = null;
            Pic.Visibility = Visibility.Collapsed;

            if (ex is AviSynthException)
            {
                //Ависинтовские ошибки - выводим красным на чёрном
                PreviewError(ex.Message.Trim(), Brushes.Red);
                IsAviSynthError = true;
            }
            else
            {
                ErrorException("AviSynthPlayer: " + ex.Message.Trim(), ex.StackTrace);
                PreviewError(Languages.Translate("Error") + "...", Brushes.Red);
            }
        }

        private void AvsPlayerFinished(object sender)
        {
            StopClip();
        }

        private void AvsStateChanged(object sender, bool AvsIsLoaded)
        {
            if (AvsIsLoaded)
            {
                if (currentState == PlayState.Running)
                    SetPauseIcon();
                else
                    SetPlayIcon();
            }
            else
                SetStopIcon();
        }

        private void PlayMovieInWindow(string filename)
        {
            fps = 0;
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
            basicAudio.put_Volume(VolumeSet); //Ввод в ДиректШоу значения VolumeSet для установки громкости

            // Is this an audio-only file (no video component)?
            CheckIsAudioOnly();
            if (!this.IsAudioOnly)
            {
                if (videoWindow != null)
                {
                    // Setup the video window
                    hr = this.videoWindow.put_Owner(this.source.Handle);
                    DsError.ThrowExceptionForHR(hr);

                    hr = this.videoWindow.put_MessageDrain(this.source.Handle);
                    DsError.ThrowExceptionForHR(hr);

                    hr = this.videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings
                        | DirectShowLib.WindowStyle.ClipChildren);
                    DsError.ThrowExceptionForHR(hr);

                    //Определяем fps
                    double AvgTimePerFrame;
                    hr = basicVideo.get_AvgTimePerFrame(out AvgTimePerFrame);
                    DsError.ThrowExceptionForHR(hr);
                    fps = (1.0 / AvgTimePerFrame);
                }
                else if (EVRControl != null)
                {
                    //Определяем fps
                    DetermineEVRFPS();
                }

                //Ловим ошибку Ависинта
                IsAviSynthError = false;
                if (NaturalDuration.TotalMilliseconds == 10000.0)
                {
                    //Признаки ошибки: duration=10000.0 и fps=24 (округлённо)
                    if ((int)fps == 24 || fps == 0) IsAviSynthError = true;
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
                    VHostElement.Visibility = Visibility.Collapsed;
                    VHostElement.Width = VHostElement.Height = 0;
                    VHostElement.Margin = new Thickness(0);
                }

                //Ловим ошибку Ависинта 2 (когда нет видео окна)
                IsAviSynthError = (NaturalDuration.TotalMilliseconds == 10000.0);

                if (m.isvideo)
                {
                    //Видео должно было быть..
                    PreviewError("NO VIDEO", Brushes.Gainsboro);
                }
            }

            //Если выше не удалось определить fps - берём значение из массива
            if (fps == 0) fps = Calculate.ConvertStringToDouble(m.outframerate);

            // Have the graph signal event via window callbacks for performance
            hr = this.mediaEventEx.SetNotifyWindow(this.source.Handle, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

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
            try
            {
                if (this.videoWindow == null && this.EVRControl == null && VideoElement.Source == null && Pic.Visibility == Visibility.Collapsed)
                    return;

                double left = 0, top = 0;
                double width = 0, height = 0;
                double aspect = m.outaspect;
                double dpi = SysInfo.dpi;

                //Аспект сообщения об ошибке
                if (IsAviSynthError)
                {
                    if (basicVideo != null)
                    {
                        int w_err, h_err;
                        DsError.ThrowExceptionForHR(basicVideo.get_VideoWidth(out w_err));
                        DsError.ThrowExceptionForHR(basicVideo.get_VideoHeight(out h_err));
                        aspect = ((double)w_err / (double)h_err);
                    }
                    else if (EVRControl != null && VHost != null)
                    {
                        System.Drawing.Size size, size_ar;
                        MFError.ThrowExceptionForHR(EVRControl.GetNativeVideoSize(out size, out size_ar));
                        aspect = ((double)size.Width / (double)size.Height);
                    }
                    else if (VideoElement.Source != null && VideoElement.HasVideo)
                    {
                        aspect = ((double)VideoElement.NaturalVideoWidth / (double)VideoElement.NaturalVideoHeight);
                    }
                }

                //Считаем отступы и размеры для видео окна
                if (!IsFullScreen)
                {
                    top = (grid_top.Margin.Top + grid_top.ActualHeight + splitter_tasks_preview.ActualHeight +
                        grid_tasks.ActualHeight + grid_player_info.ActualHeight + 8);
                    height = (grid_player_window.ActualHeight - grid_player_info.ActualHeight - 12);
                    width = (aspect * height);
                    left = ((grid_left_panel_paper.ActualWidth + (this.grid_player_window.ActualWidth - width) / 2));
                    if (width > progress_back.ActualWidth)
                    {
                        width = progress_back.ActualWidth;
                        height = (width / aspect);
                        left = ((grid_left_panel_paper.ActualWidth + (this.grid_player_window.ActualWidth - width) / 2));
                        top += ((this.grid_player_window.ActualHeight - height) / 2.0) - (grid_player_info.ActualHeight) + 14;
                    }
                }
                else
                {
                    //Для ФуллСкрина
                    height = this.LayoutRoot.ActualHeight - this.grid_player_buttons.ActualHeight; //высота экрана минус высота панели
                    width = (aspect * height);
                    left = ((this.LayoutRoot.ActualWidth - width) / 2);
                    if (width > this.LayoutRoot.ActualWidth)
                    {
                        width = this.LayoutRoot.ActualWidth;
                        height = (width / aspect);
                        left = 0;
                        top = ((this.LayoutRoot.ActualHeight - this.grid_player_buttons.ActualHeight - height) / 2.0);
                    }
                }

                if (this.videoWindow != null)
                {
                    //Масштабируем и вводим
                    DsError.ThrowExceptionForHR(this.videoWindow.SetWindowPosition(Convert.ToInt32(left * dpi), Convert.ToInt32(top * dpi),
                        Convert.ToInt32(width * dpi), Convert.ToInt32(height * dpi)));

                    //Заставляем перерисовать окно
                    DsError.ThrowExceptionForHR(this.videoWindow.put_BorderColor(1));
                }
                else if (this.EVRControl != null && VHost != null)
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
                else if (VideoElement.Source != null && VideoElement.HasVideo)
                {
                    //Используем тот-же метод, что и для EVR
                    VideoElement.Margin = new Thickness(Convert.ToInt32(left), Convert.ToInt32(top), 0, 0);
                    VideoElement.Width = Convert.ToInt32(width);
                    VideoElement.Height = Convert.ToInt32(height);
                }
                else if (this.Pic.Visibility != Visibility.Collapsed)
                {
                    //Используем тот-же метод, что и для EVR
                    Pic.Margin = new Thickness(Convert.ToInt32(left), Convert.ToInt32(top), 0, 0);
                    Pic.Width = Convert.ToInt32(width);
                    Pic.Height = Convert.ToInt32(height);
                }
            }
            catch (Exception ex)
            {
                ErrorException("MoveVideoWindow: " + ex.Message, ex.StackTrace);
            }
        }

        private void VHost_RepaintRequired(object sender, EventArgs e)
        {
            if (!IsAudioOnly && EVRControl != null)
                EVRControl.RepaintVideo();
        }

        private void CheckIsAudioOnly()
        {
            int hr = 0;
            if (EVRControl != null)
            {
                System.Drawing.Size size, size_ar;
                hr = EVRControl.GetNativeVideoSize(out size, out size_ar);
                this.IsAudioOnly = (hr < 0 || size.Width == 0 || size.Height == 0);
            }
            else if (this.videoWindow == null || this.basicVideo == null)
            {
                // Audio-only files have no video interfaces.  This might also
                // be a file whose video component uses an unknown video codec.
                this.IsAudioOnly = true;
            }
            else
            {
                OABool lVisible;
                this.IsAudioOnly = false;
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
        }

        //Определяем fps при использовании EVR
        private void DetermineEVRFPS()
        {
            int hr;
            IBaseFilter filter = null;
            graphBuilder.FindFilterByName("Enhanced Video Renderer", out filter);
            if (filter != null)
            {
                IPin pin;
                hr = filter.FindPin("EVR Input0", out pin);
                DsError.ThrowExceptionForHR(hr);
                if (pin != null)
                {
                    AMMediaType mtype = new AMMediaType();
                    try
                    {
                        hr = pin.ConnectionMediaType(mtype);
                        DsError.ThrowExceptionForHR(hr);
                        if (mtype != null)
                        {
                            VideoInfoHeader vheader = (VideoInfoHeader)Marshal.PtrToStructure(mtype.formatPtr, typeof(VideoInfoHeader));
                            if (vheader != null) fps = (10000000.0 / vheader.AvgTimePerFrame);
                        }
                    }
                    finally
                    {
                        DsUtils.FreeAMMediaType(mtype);
                        mtype = null;
                    }
                }
            }
        }

        public void PauseClip()
        {
            if (this.mediaControl != null)
            {
                // Toggle play/pause behavior
                if (this.currentState == PlayState.Paused || this.currentState == PlayState.Stopped)
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
                if (this.currentState == PlayState.Paused || this.currentState == PlayState.Stopped)
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
            else if (avsPlayer != null && !avsPlayer.IsError && avsPlayer.HasVideo)
            {
                if (this.currentState == PlayState.Paused || this.currentState == PlayState.Stopped)
                {
                    avsPlayer.Play();
                    this.currentState = PlayState.Running;
                    SetPauseIcon();
                }
                else
                {
                    avsPlayer.Pause();
                    this.currentState = PlayState.Paused;
                    SetPlayIcon();
                }
            }
        }

        public void StopClip()
        {
            if (this.mediaControl != null && this.mediaSeeking != null)
            {
                // Stop and reset postion to beginning
                if (this.currentState == PlayState.Paused || this.currentState == PlayState.Running)
                {
                    int hr = this.mediaControl.Stop();

                    this.currentState = PlayState.Stopped;

                    // Seek to the beginning
                    DsLong pos = new DsLong(0);
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

            if (avsPlayer != null)
            {
                avsPlayer.Stop();
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
                case 0x0203: //0x0203 WM_LBUTTONDBLCLK (0x0201 WM_LBUTTONDOWN, 0x0202 WM_LBUTTONUP)
                    {
                        SwitchToFullScreen(); break;
                    }
                case 0x0205: //0x0205 WM_RBUTTONUP (0x0204 WM_RBUTTONDOWN, 0x0206 WM_RBUTTONDBLCLK)
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
                try
                {
                    if (this.graphBuilder != null && this.VideoElement.Source == null)
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
                    else if (avsPlayer != null && !avsPlayer.IsError && avsPlayer.HasVideo)
                    {
                        return TimeSpan.FromSeconds(avsPlayer.TotalFrames / avsPlayer.Framerate);
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
                    else if (avsPlayer != null && !avsPlayer.IsError && avsPlayer.HasVideo)
                    {
                        return TimeSpan.FromSeconds(avsPlayer.CurrentFrame / avsPlayer.Framerate);
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
                    else if (avsPlayer != null && !avsPlayer.IsError && avsPlayer.HasVideo)
                        avsPlayer.SetFrame(Convert.ToInt32(value.TotalSeconds * avsPlayer.Framerate));
                }
                catch (Exception ex)
                {
                    if (ex is AccessViolationException) throw;
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
                    if (visual == null || !visual.IsDescendantOf(slider_pos))
                    {
                        slider_pos.Value = Position.TotalSeconds;
                    }
                }
            }
        }

        private void VideoElement_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (VideoElement.HasVideo || VideoElement.HasAudio)
                {
                    IsAudioOnly = !VideoElement.HasVideo;
                    slider_pos.Maximum = VideoElement.NaturalDuration.TimeSpan.TotalSeconds;

                    fps = 0;

                    //Определяем fps
                    if (VideoElement.HasVideo)
                        DetermineEVRFPS();

                    //Ловим ошибку Ависинта
                    IsAviSynthError = false;
                    if (NaturalDuration.TotalMilliseconds == 10000.0)
                    {
                        //Признаки ошибки: duration=10000.0 и fps=24 (округлённо)
                        if ((int)fps == 24 || fps == 0) IsAviSynthError = true;
                    }

                    //Если выше не удалось определить fps - берём значение из массива
                    if (fps == 0) fps = Calculate.ConvertStringToDouble(m.outframerate);

                    if (mediaload == MediaLoad.update)
                    {
                        if (NaturalDuration >= oldpos) //Позиционируем только если нужная позиция укладывается в допустимый диапазон
                            Position = oldpos;
                    }
                    else if (mediaload == MediaLoad.load)
                    {
                        if (Settings.AfterImportAction == Settings.AfterImportActions.Play)
                            PauseClip();
                        else if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                            Position = TimeSpan.FromSeconds(NaturalDuration.TotalSeconds / 2.0);
                    }

                    if (VideoElement.HasVideo) MoveVideoWindow();
                    else VideoElement.Visibility = Visibility.Collapsed;

                    //Обновляем счетчик кадров
                    total_frames = Convert.ToString(Math.Round(VideoElement.NaturalDuration.TimeSpan.TotalSeconds * fps));
                    textbox_frame.Text = Convert.ToString(Math.Round(VideoElement.Position.TotalSeconds * fps)) + "/" + total_frames;

                    //Общая продолжительность клипа (для МедиаБридж)
                    TimeSpan tCode2 = TimeSpan.Parse(VideoElement.NaturalDuration.ToString().Split('.')[0]);
                    textbox_duration.Text = tCode2.ToString();
                }

                if (!VideoElement.HasVideo && m.isvideo)
                {
                    //Видео должно было быть..
                    PreviewError("NO VIDEO", Brushes.Gainsboro);
                }
            }
            catch (Exception ex)
            {
                ErrorException("VideoElement_MediaOpened: " + ex.Message, ex.StackTrace);
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

                hr = graphBuilder.RenderFile(filepath, null);
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                {
                    ErrorException("BridgeCallback: " + ex.Message, ex.StackTrace);
                    PreviewError(Languages.Translate("Error") + "...", Brushes.Red);
                });
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
            if (m == null) new Interlace(null, this);
            else if (m.format == Format.ExportFormats.Audio)
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

        private void SetStopIcon()
        {
            image_play.Source = new BitmapImage(new Uri(@"../pictures/stop_new.png", UriKind.RelativeOrAbsolute));
        }

        private void CloseChildWindows()
        {
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow is AudioOptions)
                    ownedWindow.Close();
            }
        }

        private void ReloadChildWindows()
        {
            if (m == null) return;

            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow is AudioOptions)
                {
                    ((AudioOptions)ownedWindow).Reload(m);
                }
            }
        }

        private void AudioOptions_Click(object sender, RoutedEventArgs e)
        {
            //разрешаем только одно окно
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow is AudioOptions)
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
                if (IsBatchOpening && m != null && Settings.BatchCloneAudio)
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

            try
            {
                System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
                folder.Description = Languages.Translate("Select drive or folder with VOB files:");
                folder.ShowNewFolderButton = false;

                if (Settings.DVDPath != null && Directory.Exists(Settings.DVDPath))
                    folder.SelectedPath = Settings.DVDPath;

                if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.Height = this.Window.Height + 1; //чтоб убрать остатки от окна выбора директории, вот такой вот способ...
                    this.Height = this.Window.Height - 1;

                    //Проверка на "нехорошие" символы в путях
                    Calculate.ValidatePath(folder.SelectedPath, true);

                    Settings.DVDPath = folder.SelectedPath;
                    Massive x = new Massive();
                    DVDImport dvd = new DVDImport(x, folder.SelectedPath);

                    if (dvd.m != null)
                        action_open(dvd.m);
                }
            }
            catch (Exception ex)
            {
                ErrorException("OpenDVD: " + ex.Message, ex.StackTrace);
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
                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.SupportMultiDottedExtensions = true;
                    s.DefaultExt = ".png";
                    s.AddExtension = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    s.Filter = "PNG " + Languages.Translate("files") + "|*.png" +
                        "|JPEG " + Languages.Translate("files") + "|*.jpg" +
                        "|BMP " + Languages.Translate("files") + "|*.bmp";

                    int frame = Convert.ToInt32(Math.Round(Position.TotalSeconds * fps));
                    s.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + " - [" + frame + "]";

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        SavePicture(s.FileName, 0, 0, false, frame);
                    }
                }
                catch (Exception ex)
                {
                    ErrorException("SaveFrame: " + ex.Message, ex.StackTrace);
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
                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.AddExtension = true;
                    //s.SupportMultiDottedExtensions = true;
                    s.Title = Languages.Translate("Select unique name for output file:");

                    int thm_w = 0, thm_h = 0; bool fix_ar = false;
                    if (m.format == Format.ExportFormats.PmpAvc)
                    {
                        thm_w = 144; thm_h = 80; fix_ar = true;
                        s.Filter = "PNG " + Languages.Translate("files") + "|*.png";
                        s.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + ".png";
                    }
                    else if (Formats.GetDefaults(m.format).IsEditable)
                    {
                        string thm_def = Formats.GetDefaults(m.format).THM_Format;
                        string thm_format = Formats.GetSettings(m.format, "THM_Format", thm_def);
                        if (thm_format != "JPG" && thm_format != "PNG" && thm_format != "BMP" && thm_format != "None") thm_format = thm_def;
                        if (thm_format != "None")
                        {
                            s.Filter = thm_format.Replace("JPG", "JPEG") + " " + Languages.Translate("files") + "|*." + thm_format.ToLower();
                            s.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + "." + thm_format.ToLower();
                        }
                        else
                        {
                            s.FileName = Path.GetFileNameWithoutExtension(m.infilepath);
                            s.Filter = "PNG " + Languages.Translate("files") + "|*.png" +
                                "|JPEG " + Languages.Translate("files") + "|*.jpg" +
                                "|BMP " + Languages.Translate("files") + "|*.bmp";
                        }

                        fix_ar = Formats.GetSettings(m.format, "THM_FixAR", Formats.GetDefaults(m.format).THM_FixAR);
                        thm_w = Formats.GetSettings(m.format, "THM_Width", Formats.GetDefaults(m.format).THM_Width);
                        thm_h = Formats.GetSettings(m.format, "THM_Height", Formats.GetDefaults(m.format).THM_Height);
                        if (thm_w == 0) thm_w = m.outresw; if (thm_h == 0) thm_h = m.outresh;
                    }
                    else
                    {
                        thm_w = 0; thm_h = 0; fix_ar = false;
                        s.FileName = Path.GetFileNameWithoutExtension(m.infilepath);
                        s.Filter = "PNG " + Languages.Translate("files") + "|*.png" +
                            "|JPEG " + Languages.Translate("files") + "|*.jpg" +
                            "|BMP " + Languages.Translate("files") + "|*.bmp";
                    }

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        SavePicture(s.FileName, thm_w, thm_h, fix_ar, Convert.ToInt32(Math.Round(Position.TotalSeconds * fps)));
                    }
                }
                catch (Exception ex)
                {
                    ErrorException("SaveTHM: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private void SavePicture(string path, int width, int height, bool fix_ar, int frame)
        {
            System.Drawing.Bitmap bmp = null;
            System.Drawing.Graphics g = null;
            AviSynthReader reader = null;
            string new_script = m.script;

            try
            {
                if (fix_ar && width != 0 && height != 0)
                {
                    int crop_w = 0, crop_h = 0;
                    double old_asp = m.outaspect;
                    double new_asp = (double)width / (double)height;

                    if (old_asp < new_asp)
                    {
                        crop_h = Math.Max(Convert.ToInt32((m.outresh - ((m.outresh * old_asp) / new_asp)) / 2), 0);
                    }
                    else if (old_asp > new_asp)
                    {
                        crop_w = Math.Max(Convert.ToInt32((m.outresw - (m.outresw / old_asp) * new_asp) / 2), 0);
                    }

                    new_script += ("Lanczos4Resize(" + width + ", " + height + ", " + crop_w + ", " + crop_h + ", -" + crop_w + ", -" + crop_h + ")\r\n");
                }

                reader = new AviSynthReader(AviSynthColorspace.RGB24, AudioSampleType.Undefined);
                reader.ParseScript(new_script);
                if (width == 0 || height == 0 || (width == reader.Width && height == reader.Height))
                {
                    bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                }
                else
                {
                    bmp = new System.Drawing.Bitmap(width, height);
                    g = System.Drawing.Graphics.FromImage(bmp);

                    //метод интерполяции при ресайзе
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(reader.ReadFrameBitmap(frame), 0, 0, width, height);
                }

                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".jpg")
                {
                    //процент cжатия jpg
                    System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                    System.Drawing.Imaging.EncoderParameters encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                    encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);

                    //jpg
                    bmp.Save(path, info[1], encoderParameters);
                }
                else if (ext == ".png")
                {
                    //png
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    //bmp
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
                }
            }
            catch (Exception ex)
            {
                ErrorException("SavePicture: " + ex.Message, ex.StackTrace);
            }
            finally
            {
                //завершение
                if (g != null) { g.Dispose(); g = null; }
                if (bmp != null) { bmp.Dispose(); bmp = null; }
                if (reader != null) { reader.Close(); reader = null; }
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
                if (m.format == Format.ExportFormats.BluRay)
                {
                    new Options_BluRay(this);
                }
                else if (Formats.GetDefaults(m.format).IsEditable)
                {
                    FormatSettings fs = new FormatSettings(m.format, this);
                    if (fs.update_massive && !fs.update_audio && !fs.update_framerate && !fs.update_resolution)
                    {
                        m.outfilesize = Calculate.GetEncodingSize(m);
                        if (m.outfilepath != null) m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + Format.GetValidExtension(m);

                        UpdateTaskMassive(m);
                    }
                    else if (fs.update_audio || fs.update_framerate || fs.update_resolution)
                    {
                        string old_script = m.script;

                        //забиваем-обновляем аудио массивы
                        if (fs.update_audio) m = FillAudio(m);

                        //перезабиваем специфику формата
                        if (fs.update_framerate)
                        {
                            m = Format.GetOutInterlace(m);
                            m = Format.GetValidFramerate(m);
                            m = Calculate.UpdateOutFrames(m);
                        }
                        if (fs.update_resolution)
                        {
                            m = Format.GetValidResolution(m);
                            m = Format.GetValidOutAspect(m);
                            m = AspectResolution.FixAspectDifference(m);
                        }

                        m.outfilesize = Calculate.GetEncodingSize(m);
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
                    new Message(this).ShowMessage(Languages.Translate("This format doesn`t have any settings."), Languages.Translate("Format"));
                }
            }
            else if (combo_format.SelectedItem != null)
            {
                Format.ExportFormats format = Format.StringToEnum(combo_format.SelectedItem.ToString());
                if (format == Format.ExportFormats.BluRay)
                {
                    new Options_BluRay(this);
                }
                else if (Formats.GetDefaults(format).IsEditable)
                {
                    new FormatSettings(format, this);
                }
                else
                {
                    new Message(this).ShowMessage(Languages.Translate("This format doesn`t have any settings."), Languages.Translate("Format"));
                }
            }
        }

        private void menu_settings_Click(object sender, RoutedEventArgs e)
        {
            Settings_Window sett = new Settings_Window(this, 1);
        }

        //Громкость+
        public void VolumePlus()
        {
            if (slider_Volume.Value <= 0.95)
                slider_Volume.Value = slider_Volume.Value + 0.05;
        }

        //Громкость-
        public void VolumeMinus()
        {
            if (slider_Volume.Value >= 0.05)
            {
                slider_Volume.Value = (slider_Volume.Value - 0.05);
                if (slider_Volume.Value < 0.05) //Чтоб гарантированно достигнуть нуля
                    slider_Volume.Value = 0;
            }
        }

        //Обработка изменения положения регулятора громкости, изменение иконки рядом с ним, пересчет значений для ДиректШоу
        private void Volume_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeSet = -(int)(10000 - Math.Pow(slider_Volume.Value, 1.0 / 5) * 10000); //Пересчитывание громкости для ДиректШоу
            if (this.graphBuilder != null && basicAudio != null) basicAudio.put_Volume(VolumeSet); //Задаем громкость для ДиректШоу
            else if (avsPlayer != null && !avsPlayer.IsError) avsPlayer.Volume = VolumeSet;        //Задаем громкость для PictureView

            //Иконка регулятора громкости
            SetVolumeIcon();

            //Запись значения громкости в реестр
            Settings.VolumeLevel = slider_Volume.Value;
        }

        private void SetVolumeIcon()
        {
            if (slider_Volume.Value <= 0.0 || Settings.PlayerEngine == Settings.PlayerEngines.PictureView && !Settings.PictureViewAudio)
            {
                image_volume_on.Visibility = Visibility.Collapsed;
                image_volume_off.Visibility = Visibility.Visible;
            }
            else
            {
                image_volume_on.Visibility = Visibility.Visible;
                image_volume_off.Visibility = Visibility.Collapsed;
            }
        }

        //Меняем громкость колесиком мышки
        private void Volume_Wheel(object sender, MouseWheelEventArgs e)
        {
            if (sender == slider_Volume || IsFullScreen && (VideoElement.Source != null || avsPlayer != null))
            {
                if (e.Delta > 0)
                    VolumePlus();
                else
                    VolumeMinus();
            }
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
            button_apply_trim.ToolTip = null;
            textbox_start.Text = textbox_end.Text = "";
            button_set_start.Content = Languages.Translate("Set Start");
            button_set_end.Content = Languages.Translate("Set End");
            button_apply_trim.Content = Languages.Translate("Apply Trim");
            textbox_start.IsReadOnly = textbox_end.IsReadOnly = false;
        }

        private void SetTrimsButtons()
        {
            string tooltip = null;
            for (int i = 0; i < m.trims.Count; i++)
            {
                tooltip += (i + 1) + ". " + (((Trim)m.trims[i]).start > 0 ? ((Trim)m.trims[i]).start.ToString() : "start") +
                    "-" + (((Trim)m.trims[i]).end > 0 ? ((Trim)m.trims[i]).end.ToString() : "end");

                if (i == m.trim_num) tooltip += " <-";
                if (i != m.trims.Count - 1) tooltip += "\r\n";
                else if (i < m.trim_num) tooltip += "\r\n" + (i + 2) + ". new_region <-";
            }

            Trim trim = (m.trims.Count > m.trim_num) ? (Trim)m.trims[m.trim_num] : null;

            //"Начало"
            if (trim != null && trim.start >= 0 || m.trim_is_on)
            {
                textbox_start.IsReadOnly = true;
                button_set_start.Content = Languages.Translate("Clear");
                textbox_start.Text = (trim != null && trim.start >= 0) ? trim.start.ToString() : "";
            }
            else
            {
                textbox_start.IsReadOnly = false;
                button_set_start.Content = Languages.Translate("Set Start");
                textbox_start.Text = "";
            }

            //"Конец"
            if (trim != null && trim.end >= 0 || m.trim_is_on)
            {
                textbox_end.IsReadOnly = true;
                button_set_end.Content = Languages.Translate("Clear");
                textbox_end.Text = (trim != null && trim.end >= 0) ? trim.end.ToString() : "";
            }
            else
            {
                textbox_end.IsReadOnly = false;
                button_set_end.Content = Languages.Translate("Set End");
                textbox_end.Text = "";
            }

            //"Обрезать"
            button_apply_trim.ToolTip = tooltip;
            button_apply_trim.Content = ((m.trim_is_on) ? Languages.Translate("Remove Trim") : Languages.Translate("Apply Trim")) +
                (m.trims.Count > 0 ? " (" + (m.trim_num + 1) + "/" + m.trims.Count + ")" : "");
        }

        private void button_trim_plus_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || m.trims.Count == 0 || m.trim_num >= m.trims.Count)
                return;

            if (m.trim_num == m.trims.Count - 1)
            {
                if (!m.trim_is_on && ((Trim)m.trims[m.trim_num]).start >= 0 && ((Trim)m.trims[m.trim_num]).end >= 0)
                {
                    m.trim_num += 1;
                }
                else
                    return;
            }
            else
                m.trim_num += 1;

            SetTrimsButtons();
        }

        private void button_trim_minus_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || m.trims.Count == 0)
                return;

            m.trim_num = Math.Max(m.trim_num - 1, 0);
            SetTrimsButtons();
        }

        private void button_trim_delete_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || m.trims.Count == 0 || m.trim_is_on)
                return;

            if (m.trim_num < m.trims.Count)
                m.trims.RemoveAt(m.trim_num);

            m.trim_num = Math.Max(m.trim_num - 1, 0);
            UpdateTaskMassive(m);
            SetTrimsButtons();
        }

        private void button_set_trim_value_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || m.trim_is_on)
                return;

            TextBox textbox = (sender == button_set_start) ? textbox_start : textbox_end;

            int value = -1;
            if (textbox.IsReadOnly) { } //Сброс
            else if (textbox.Text.Length == 0) //Ничего не вписано - определяем текущий кадр
            {
                value = Convert.ToInt32(Position.TotalSeconds * fps);
                textbox.Text = Convert.ToString(value);
            }
            else if (int.TryParse(textbox.Text, out value)) //Вписан номер кадра
            {
                if (value < 0)
                {
                    textbox.Text = "";
                    return;
                }
            }
            else
            {
                TimeSpan result = TimeSpan.Zero;
                if (Calculate.ParseTimeString(textbox.Text, out result)) //Вписано время - пересчитываем в кадр
                {
                    if (result > TimeSpan.Zero)
                    {
                        value = Convert.ToInt32(result.TotalSeconds * fps);
                        textbox.Text = Convert.ToString(value);
                    }
                    else
                    {
                        textbox.Text = "";
                        return;
                    }
                }
                else
                {
                    textbox.Text = "";
                    return;
                }
            }

            //Добавляем новый Trim
            if (m.trim_num >= m.trims.Count)
            {
                m.trims.Add(new Trim());
            }
            else if (value > 0)
            {
                Trim trim = (Trim)m.trims[m.trim_num];

                //Проверка введённого
                if (sender == button_set_start)
                {
                    if (value > trim.end && trim.end > 0)
                        ErrorException(Languages.Translate("Error") + ": [" + Languages.Translate("Set Start") + "] > [" + Languages.Translate("Set End") + "]");
                    else if (value == trim.end && trim.end > 0)
                        ErrorException(Languages.Translate("Error") + ": [" + Languages.Translate("Set Start") + "] = [" + Languages.Translate("Set End") + "]");
                }
                else
                {
                    if (value < trim.start)
                        ErrorException(Languages.Translate("Error") + ": [" + Languages.Translate("Set Start") + "] > [" + Languages.Translate("Set End") + "]");
                    else if (value == trim.start)
                        ErrorException(Languages.Translate("Error") + ": [" + Languages.Translate("Set Start") + "] = [" + Languages.Translate("Set End") + "]");
                }
            }
            else if (value < 0)
            {
                //Удаляем пустой Trim
                if (sender == button_set_start && ((Trim)m.trims[m.trim_num]).end < 0 ||
                    sender == button_set_end && ((Trim)m.trims[m.trim_num]).start < 0)
                {
                    button_trim_delete_Click(null, null);
                    return;
                }
            }

            //Вводим значения
            if (sender == button_set_start)
                ((Trim)m.trims[m.trim_num]).start = value;
            else
                ((Trim)m.trims[m.trim_num]).end = value;

            UpdateTaskMassive(m);
            SetTrimsButtons();
        }

        private void button_apply_trim_Click(object sender, RoutedEventArgs e)
        {
            if (m == null || m.trims.Count == 0)
                return;

            m.trim_is_on = !m.trim_is_on;
            UpdateScriptAndDuration();

            if (m.trim_is_on)
                ValidateTrimAndCopy(m);

            SetTrimsButtons();
        }

        private void textbox_trim_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (sender == textbox_start && !textbox_start.IsReadOnly)
                    button_set_trim_value_Click(button_set_start, null);
                else if (sender == textbox_end && !textbox_end.IsReadOnly)
                    button_set_trim_value_Click(button_set_end, null);
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
                string path_to_open = OpenDialogs.OpenFolder();
                if (path_to_open == null || Directory.GetFiles(path_to_open).Length == 0) return;

                //Проверка на "нехорошие" символы в путях
                Calculate.ValidatePath(path_to_open, true);

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
                ErrorException("OpenFolder: " + ex.Message, ex.StackTrace);
            }
        }

        private void MultiOpen(string[] files_to_open) //Для открытия и сохранения группы файлов
        {
            try
            {
                opened_files = 0; //Обнуляем счетчик успешно открытых файлов
                int count = files_to_open.Length; //Кол-во файлов для открытия

                //Вывод первичной инфы об открытии
                textbox_name.Text = count + " - " + Languages.Translate("total files") + ", " + opened_files + " - " + Languages.Translate("opened files") +
                    ", " + outfiles.Count + " - " + Languages.Translate("in queue");

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
                                if (m == null) { textbox_name.Text = ""; PauseAfterFirst = false; return; }
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
                    textbox_name.Text = count + " - " + Languages.Translate("total files") + ", " + opened_files + " - " + Languages.Translate("opened files") +
                        ", " + outfiles.Count + " - " + Languages.Translate("in queue");
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
                ErrorException("MultiOpen: " + ex.Message, ex.StackTrace);
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
                AddTask(mass, TaskStatus.Waiting);

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
                ErrorException("CheckTempFolder: " + ex.Message, ex.StackTrace);
            }
        }

        private void MenuHider(bool ShowItems)
        {
            try
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
                menu_demux.IsEnabled = ShowItems;
                menu_save_wav.IsEnabled = ShowItems;
                menu_audiooptions.IsEnabled = ShowItems;
                mnAddSubtitles.IsEnabled = ShowItems;
                mnRemoveSubtitles.IsEnabled = ShowItems;
                menu_createautoscript.IsEnabled = ShowItems;
                menu_save_script.IsEnabled = ShowItems;
                menu_run_script.IsEnabled = ShowItems;
                target_goto.IsEnabled = ShowItems;
                menu_createtestscript.IsEnabled = ShowItems;
                cmn_addtobookmarks.IsEnabled = cmn_deletebookmarks.IsEnabled = ShowItems;

                cmn_bookmarks.Items.Clear();

                string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                if (m != null)
                {
                    this.Title = Path.GetFileName(m.infilepath) + "  - XviD4PSP - v" + version;
                    this.menu_createtestscript.IsChecked = m.testscript;

                    //Восстанавливаем трим
                    if (m.trims.Count > 0)
                        SetTrimsButtons();
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
                    this.Title = "XviD4PSP - AviSynth-based MultiMedia Converter  -  v" + version;
                    this.menu_createtestscript.IsChecked = false;
                    this.cmn_bookmarks.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ErrorException("MenuHider: " + ex.Message, ex.StackTrace);
            }
        }

        private void GoTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                int frame;
                if (int.TryParse(textbox_frame_goto.Text, out frame))
                {
                    //Введён номер кадра
                    if (graphBuilder != null)
                    {
                        //Пересчет во время
                        TimeSpan newpos = TimeSpan.FromSeconds(frame / fps);
                        newpos = (newpos < TimeSpan.Zero) ? TimeSpan.Zero : (newpos > NaturalDuration) ? NaturalDuration : newpos;
                        if (Position != newpos) Position = newpos;
                    }
                    else if (avsPlayer != null && !avsPlayer.IsError)
                    {
                        //Для PictureView можно непосредственно указать нужный кадр
                        frame = (frame < 0) ? 0 : (frame > avsPlayer.TotalFrames) ? avsPlayer.TotalFrames : frame;
                        if (avsPlayer.CurrentFrame != frame) avsPlayer.SetFrame(frame);
                    }
                }
                else
                {
                    //Введено время
                    TimeSpan newpos = TimeSpan.Zero;
                    if (Calculate.ParseTimeString(textbox_frame_goto.Text, out newpos))
                    {
                        if (graphBuilder != null || Pic.Visibility == Visibility.Visible)
                        {
                            newpos = (newpos < TimeSpan.Zero) ? TimeSpan.Zero : (newpos > NaturalDuration) ? NaturalDuration : newpos;
                            if (Position != newpos) Position = newpos;
                        }
                    }
                }

                GoTo_Click(null, null);
            }
            else if (e.Key == Key.Escape)
                GoTo_Click(null, null);
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
                if (mass.trims.Count > 0 && mass.trim_is_on)
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
            UpdateTaskMassive(m);
        }

        private void button_avsp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (avsp == null)
                {
                    string path = "";
                    if (m != null)
                    {
                        //Пишем в файл текущий скрипт
                        path = Settings.TempPath + "\\AvsP_" + m.key + ".avs";
                        File.WriteAllText(path, m.script, System.Text.Encoding.Default);
                    }
                    else
                    {
                        //Путь к создаваемому скрипту
                        System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                        s.Title = Languages.Translate("Select unique name for output file:");
                        s.Filter = "AVS " + Languages.Translate("files") + "|*.avs";
                        s.SupportMultiDottedExtensions = true;
                        s.AddExtension = true;
                        s.DefaultExt = ".avs";
                        s.FileName = "Untitled.avs";

                        if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            string ext = (s.DefaultExt.StartsWith(".")) ? s.DefaultExt : "." + s.DefaultExt;
                            path = (Path.GetExtension(s.FileName).ToLower() != ext) ? s.FileName += ext : s.FileName;

                            //Пишем в файл начальный скрипт
                            File.WriteAllText(path, "#Write your script here, then Save it (Ctrl+S) and Exit (Alt+X).\r\n" +
                                "#Note: this script will be opened using Import(), so you can`t change it later!\r\n\r\n" +
                                "ColorBars() #just for example\r\n\r\n", Encoding.Default);
                        }
                    }

                    avsp = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    if (!string.IsNullOrEmpty(path))
                    {
                        info.Arguments = "\"" + path + "\"";
                        avsp.Exited += new EventHandler(AvsPExited);
                        avsp.EnableRaisingEvents = true;
                    }
                    avsp.StartInfo = info;
                    avsp.Start();

                    //Если не требуется ждать завершения работы AvsP
                    if (string.IsNullOrEmpty(path))
                        CloseAvsP();
                }
                else
                {
                    Win32.SetForegroundWindow(avsp.MainWindowHandle);
                }
            }
            catch (Exception ex)
            {
                ErrorException("AvsP editor: " + ex.Message, ex.StackTrace);
                CloseAvsP();
            }
        }

        internal delegate void AvsPExitedDelegate(object sender, EventArgs e);
        private void AvsPExited(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new AvsPExitedDelegate(AvsPExited), sender, e);
            }
            else
            {
                try
                {
                    string path = ((Process)sender).StartInfo.Arguments.Trim(new char[] { '"' });
                    CloseAvsP();

                    if (File.Exists(path) && new FileInfo(path).Length > 0)
                    {
                        if (m != null && path == Settings.TempPath + "\\AvsP_" + m.key + ".avs")
                        {
                            //Изменения в текущем задании
                            string oldscript = m.script;

                            //После завершения работы AvsP перечитываем измененный им файл скрипта и вводим его содержимое в массив
                            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
                            {
                                m.script = sr.ReadToEnd();

                                if (script_box.Visibility == Visibility.Visible)
                                    script_box.Text = m.script;
                            }

                            //обновление при необходимости
                            if (m.script.Trim() != oldscript.Trim())
                            {
                                LoadVideo(MediaLoad.update);
                                UpdateTaskMassive(m);
                            }

                            //Удаляем файл скрипта
                            File.Delete(path);
                        }
                        else if (m == null && !path.StartsWith(Settings.TempPath + "\\AvsP_"))
                        {
                            //Создаём новое задание
                            Massive x = new Massive();
                            x.infilepath = path;
                            x.infileslist = new string[] { path };
                            action_open(x);
                        }
                        else
                        {
                            //Видимо сменился номер задания или файл был закрыт.
                            //Ничего не делаем; скрипт не удаляем.
                        }
                    }
                }
                catch (Exception ex)
                {
                    new Message(this).ShowMessage("AvsP editor: " + ex.Message, Languages.Translate("Error"));
                }
            }
        }

        private void CloseAvsP()
        {
            lock (avsp_lock)
            {
                if (avsp != null)
                {
                    avsp.Close();
                    avsp.Dispose();
                    avsp = null;
                }
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

            if (old_renderer != new_renderer && m != null && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
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

        public void UpdateTasksBackup()
        {
            lock (backup_lock)
            {
                if (Settings.EnableBackup) SaveTasks(Settings.TempPath + "\\backup.tsks", false);
            }
        }

        private void cmenu_save_tasks_Click(object sender, RoutedEventArgs e)
        {
            if (m == null && list_tasks.Items.Count == 0) return;
            System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
            s.SupportMultiDottedExtensions = true;
            s.DefaultExt = ".tsks";
            s.AddExtension = true;
            s.Title = Languages.Translate("Select unique name for output file:");
            s.Filter = "TSKS " + Languages.Translate("files") + "|*.tsks";
            s.FileName = "stored_tasks_(" + DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss") + ").tsks";

            if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveTasks(s.FileName, true);
            }
        }

        private void SaveTasks(string path, bool show_errors)
        {
            try
            {
                if (m == null && list_tasks.Items.Count == 0 && !path.EndsWith("backup.tsks")) return;

                bool something_saved = false;
                using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    IFormatter formatter = new BinaryFormatter();

                    //Сохраняем задания
                    foreach (Task task in list_tasks.Items)
                    {
                        if (task.Status != TaskStatus.Encoding)
                        {
                            formatter.Serialize(stream, task);
                            something_saved = true;
                        }
                    }

                    //Сохраняем массив
                    if (m != null)
                    {
                        formatter.Serialize(stream, m);
                        something_saved = true;
                    }

                    //Сохраняем список файлов на удаление
                    if (something_saved && deletefiles.Count > 0)
                        formatter.Serialize(stream, deletefiles);
                }

                //Если сохранять было нечего, удаляем пустой файл
                if (!something_saved) File.Delete(path);
            }
            catch (Exception ex)
            {
                if (show_errors)
                    ErrorException("SaveTasks: " + ex.Message, ex.StackTrace);
            }
        }

        private void RestoreTasks(string path, ref Massive x)
        {
            try
            {
                using (Stream stream = new FileStream(x.infilepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    x = null; //Будем использовать его повторно
                    IFormatter formatter = new BinaryFormatter();

                    object obj = null;
                    while (stream.Position < stream.Length)
                    {
                        obj = formatter.Deserialize(stream);
                        if (obj is Massive)
                        {
                            //Massive
                            x = obj as Massive;

                            //Кэши на удаление
                            RestoreCaches(x);
                        }
                        else if (obj is Task)
                        {
                            //Task
                            Task task = obj as Task;
                            list_tasks.Items.Add(task);

                            //Имена конечных файлов
                            if (!outfiles.Contains(task.Mass.outfilepath))
                                outfiles.Add(task.Mass.outfilepath);

                            //Кэши на удаление
                            RestoreCaches(task.Mass);
                        }
                        else if (obj is ArrayList)
                        {
                            //Список файлов на удаление (при закрытии программы)
                            foreach (string file in obj as ArrayList)
                            {
                                try
                                {
                                    if (File.Exists(file) && !deletefiles.Contains(file))
                                        deletefiles.Add(file);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorException("RestoreTasks: " + ex.Message, ex.StackTrace);
                x = null;
            }
        }

        private void RestoreCaches(Massive mass)
        {
            //Возможные индекс-файлы от FFmpegSource2
            if (!mass.ffms_indexintemp)
            {
                foreach (string file in mass.infileslist)
                {
                    if (!ffcache.Contains(file + ".ffindex"))
                        ffcache.Add(file + ".ffindex");
                }
            }

            //Кэш от DGIndex
            if (mass.vdecoder == AviSynthScripting.Decoders.MPEG2Source && mass.indexfile != null)
            {
                try
                {
                    if (File.Exists(mass.indexfile) && !dgcache.Contains(mass.indexfile) &&
                        Path.GetDirectoryName(mass.indexfile).EndsWith(".index") && mass.indexfile != mass.infilepath)
                        dgcache.Add(mass.indexfile);
                }
                catch { }
            }
        }

        public void ReloadPresets()
        {
            LoadFilteringPresets();
            LoadSBCPresets();
            combo_sbc.SelectedItem = (m != null) ? m.sbc : Settings.SBC;
            LoadVideoPresets();
            SetVideoPreset();
            LoadAudioPresets();
            SetAudioPreset();
        }

        private void check_pictureview_drop_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PictureViewDropFrames = check_pictureview_drop.IsChecked;
            if (avsPlayer != null) avsPlayer.AllowDropFrames = check_pictureview_drop.IsChecked;
        }

        private void check_pictureview_audio_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.PictureViewAudio = check_pictureview_audio.IsChecked;
            if (Settings.PlayerEngine == Settings.PlayerEngines.PictureView)
            {
                SetVolumeIcon();

                if (m != null && avsPlayer != null)
                {
                    LoadVideo(MediaLoad.update);
                }
            }
        }
    }
}