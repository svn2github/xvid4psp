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

        public enum MediaLoad { load = 1, update }
        public enum ShowPreview { show, dntshow } //загружать или нет превью после открытия файла

        private bool IsInsertAction = false;

        public TimeSpan oldpos;
        public MediaLoad mediaload;
        public ShowPreview showpreview;

        //player
        private Brush oldbrush;

        private const int WMGraphNotify = 0x0400 + 13;
        private const int VolumeFull = 0;
        private const int VolumeSilence = -10000;
        private int VolumeSet; //Громкость DirectShow плейера

        private string total_frames = "";
        private int trim_start = 0;
        private int trim_end = 0;

        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaControl = null;
        private IMediaEventEx mediaEventEx = null;
        private IVideoWindow videoWindow = null;
        private IBasicAudio basicAudio = null;
        private IBasicVideo basicVideo = null;
        private IMediaSeeking mediaSeeking = null;
        private IMediaPosition mediaPosition = null;
        private IVideoFrameStep frameStep = null;

        private bool isAudioOnly = false;
        private bool isFullScreen = false;
        public bool isDelayUpdate = false;
        public PlayState currentState = PlayState.Init;

        private IntPtr hDrain = IntPtr.Zero; //хз что
        private IntPtr Handle = IntPtr.Zero; //добавлено для хз чего

        private HwndSource source;
        private System.Timers.Timer timer;

        private IFilterGraph graph;
        private string filepath = "";
        private Thickness oldmargin;

        private BackgroundWorker worker = null;

        private string path_to_open; //путь для исходных файлов при перекодировании папки
        private string path_to_save; //путь для конечных файлов при перекодировании папки

        private int opened_files = 0; //Кол-во открытых файлов при открытии папки

#if DEBUG
        private DsROTEntry rot = null;
#endif

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

        public MainWindow()
        {
            //разрешаем запустить только один экземпляр
            try
            {
                Process process = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(process.ProcessName);
                foreach (Process _process in processes)
                {
                    if (_process.Id != process.Id &&
                        _process.MainModule.FileName == process.MainModule.FileName &&
                        _process.MainWindowHandle != IntPtr.Zero)
                    {
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            this.InitializeComponent();

            //Установка параметров окна из сохраненных настроек (если эта опция включена)
            if (Settings.WindowResize == true)
            {
                this.Width = Settings.WindowWidth;
                this.Height = Settings.WindowHeight;
                this.Left = Settings.WindowLeft;
                this.Top = Settings.WindowTop;
            }

            //Для правильного отображения таймера
            this.textbox_time.Visibility = Visibility.Visible;//Показать прошедшее время воспроизведения..
            this.textbox_duration.Visibility = Visibility.Collapsed;//.. а общее время скрыть.

            try
            {
                textbox_name.Text = "";
                textbox_time.Text = "00:00:00";
                textbox_duration.Text = "00:00:00";
                textbox_frame.Text = "";

                //переводим лейблы
                SetLanguage();
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }

            //events
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.KeyUp += new KeyEventHandler(MainWindow_KeyUp);
            this.textbox_name.MouseEnter += new MouseEventHandler(textbox_name_MouseEnter);//Мышь вошла в зону с названием файла
            this.textbox_name.MouseLeave += new MouseEventHandler(textbox_name_MouseLeave);//Мышь вышла из зоны с названием файла
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainFormLoader();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                    foreach (string f in Format.GetFormatList())
                        combo_format.Items.Add(f);
                    combo_format.SelectedItem = Format.EnumToString(Settings.FormatOut);

                    //загружаем список фильтров
                    combo_filtering.Items.Clear();
                    combo_filtering.Items.Add("Disabled");
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\filtering"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        combo_filtering.Items.Add(name);
                    }
                    //прописываем текущий фильтр
                    if (combo_filtering.Items.Contains(Settings.Filtering))
                        combo_filtering.SelectedItem = Settings.Filtering;
                    else
                        combo_filtering.SelectedItem = "Disabled";

                    //загружаем списки профилей цвето коррекции
                    combo_sbc.Items.Clear();
                    combo_sbc.Items.Add("Disabled");
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\sbc"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        combo_sbc.Items.Add(name);
                    }
                    //прописываем текущий профиль
                    if (combo_sbc.Items.Contains(Settings.SBC))
                        combo_sbc.SelectedItem = Settings.SBC;
                    else
                        combo_sbc.SelectedItem = "Disabled";

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
                        if (drivestring != Settings.TempPath.Substring(0, 3))
                        {
                            Message mess = new Message(this);
                            mess.ShowMessage(Languages.Translate("Maximum free drive space detected on") + " " +
                                drivestring.Substring(0, 2) + " (" + dlabel + ")." +
                                Environment.NewLine +
                                Languages.Translate("Do you want use this drive for temp files?"), Languages.Translate("Place for temp files"),
                                 Message.MessageStyle.YesNo);
                            if (mess.result == Message.Result.Yes)
                                Settings.TempPath = drivestring + "Temp";
                        }
                        if (!Directory.Exists(Settings.TempPath))
                            Directory.CreateDirectory(Settings.TempPath);
                    }

                    //Проверяем, пуста ли Темп-папка
                    if (Settings.Key == "0000")
                        TempFolderFiles();                                   
                    
                    if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                    {
                        this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                        this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
                        this.grid_tasks.SizeChanged += new SizeChangedEventHandler(grid_tasks_SizeChanged);
                        this.grid_player_window.MouseDown += new MouseButtonEventHandler(Direct_Show_Mouse_Click); //мышь для Фуллскрина при ДиректШоу
                        this.grid_player_buttons.MouseDown += new MouseButtonEventHandler(Direct_Show_Mouse_Click); //мышь для Фуллскрина при ДиректШоу


                        source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                        source.AddHook(new HwndSourceHook(WndProc));

                        //timer
                        timer = new System.Timers.Timer();
                        timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                        timer.Interval = 10;
                        timer.Enabled = true;
                        timer.Start();

                        VideoElement.Visibility = Visibility.Visible;

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
                        VideoElement.MouseDown += new MouseButtonEventHandler(VideoElement_MouseDown);
                        VisualTarget.Rendering += new EventHandler(VisualTarget_Rendering);

                        VideoElement.Visibility = Visibility.Visible;
                    }

                    ////исправляем ffmpeg whitelist
                    //не обязательно и может вызывать другие проблемы
                    //try
                    //{
                    //    RegistryKey myHive = Registry.CurrentUser.OpenSubKey("SOFTWARE\\GNU\\ffdshow", true);
                    //    if (myHive != null)
                    //    {
                    //        object obj = myHive.GetValue("whitelist");
                    //        string whitelist = "";
                    //        if (obj != null)
                    //            whitelist = obj.ToString();
                    //        if (!whitelist.Contains("XviD4PSP.exe"))
                    //            myHive.SetValue("whitelist", whitelist + "WPF_VideoPlayer.exe;XviD4PSP.exe;", RegistryValueKind.String);
                    //    }
                    //    myHive.Close();
                    //    myHive = Registry.CurrentUser.OpenSubKey("SOFTWARE\\GNU\\ffdshow_audio", true);
                    //    if (myHive != null)
                    //    {
                    //        object obj = myHive.GetValue("whitelist");
                    //        string whitelist = "";
                    //        if (obj != null)
                    //            whitelist = obj.ToString();
                    //        if (!whitelist.Contains("XviD4PSP.exe"))
                    //            myHive.SetValue("whitelist", whitelist + "WPF_VideoPlayer.exe;XviD4PSP.exe;", RegistryValueKind.String);
                    //    }
                    //    myHive.Close();
                    //}
                    //catch (Exception ex)
                    //{
                    //    MessageBox.Show(ex.Message);
                    //}

                    //Открытие файла из командной строки
                    //command line arguments
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

                            //исключаем DVD меню
                            //if (Path.GetFileName(m.infilepath) == "VIDEO_TS.VOB")
                            //    m.infilepath = Path.GetDirectoryName(m.infilepath) + "\\VTS_01_1.VOB";

                            //ищем соседние файлы и спрашиваем добавить ли их к заданию при нахождении таковых
                            if (Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled ||
                                Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly &&
                                Calculate.IsValidVOBName(x.infilepath))
                                x = OpenDialogs.GetFriendFilesList(x);
                            if (x != null)
                                action_open(x, ShowPreview.show);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }

            }

        }


        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {

            if (this.graphBuilder != null)
            {
                //     if (e.Key == Key.System && e.SystemKey == Key.Return)
                //      {
                //          SwitchToFullScreen();
                //      }

                //Enter или Escape - переход в Фуллскрин и обратно     
                if (e.Key == Key.Escape && slider_pos.IsFocused)
                {
                    SwitchToFullScreen();
                }
                //Left - кадр назад
                if (e.Key == Key.Left && slider_pos.IsFocused)
                {
                    if (m != null)
                    {
                        try
                        {
                            if (currentState == PlayState.Running)
                                PauseClip();

                            double framerate = Calculate.ConvertStringToDouble(m.outframerate);
                            TimeSpan newpos = Position - TimeSpan.FromMilliseconds(1000.0 / framerate);
                            if (newpos >= TimeSpan.Zero)
                                Position = newpos;

                            //button_frame_back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        catch (Exception ex)
                        {
                            ErrorExeption(ex.Message);
                        }
                    }
                    else
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
                    }
                }
                //Right - кадр вперед        
                if (e.Key == Key.Right && slider_pos.IsFocused)
                {
                    if (m != null)
                    {
                        try
                        {
                            if (currentState == PlayState.Running)
                                PauseClip();

                            double framerate = Calculate.ConvertStringToDouble(m.outframerate);
                            TimeSpan newpos = Position + TimeSpan.FromMilliseconds(1000.0 / framerate);
                            if (newpos <= NaturalDuration)
                                Position = newpos;
                        }
                        catch (Exception ex)
                        {
                            ErrorExeption(ex.Message);
                        }
                    }
                    else
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
                    }
                }


                //Пробел - пауза
                if (e.Key == Key.Space && slider_pos.IsFocused)
                {
                    PauseClip();
                }

                //Вверх - громкость+
                if (e.Key == Key.Up && slider_pos.IsFocused)
                {
                    VolumePlus();
                }

                //Вниз - громкость-
                if (e.Key == Key.Down && slider_pos.IsFocused)
                {
                    VolumeMinus();
                }

                //F - Фильтрация
                if (e.Key == Key.F && slider_pos.IsFocused)
                {
                    EditScript();
                }
                //C - Цветокоррекция
                if (e.Key == Key.C && slider_pos.IsFocused)
                {
                    SaturationBrightness();
                }

                //R - кроп\ресайз
                if (e.Key == Key.R && slider_pos.IsFocused)
                {
                    AspectResolutionWindow();
                }

                //I - деинтерлейс
                if (e.Key == Key.I && slider_pos.IsFocused)
                {
                    InterlaceWindow();
                }


            }
        }

        private void grid_tasks_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!this.isAudioOnly)
                MoveVideoWindow();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.graphBuilder != null)
            {
                UpdateClock();
            }
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!this.isAudioOnly & this.graphBuilder != null)
            {
                MoveVideoWindow();
                this.videoWindow.put_BorderColor(1); //заставляет перерисовать окно в любом случае. 1 - цвет бордюра, не знаю какой именно это будет цвет..
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!this.isAudioOnly)
                MoveVideoWindow();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //проверяем есть ли задания в работе
            bool IsEncoding = false;
            foreach (object _task in list_tasks.Items)
            {
                Task task = (Task)_task;
                if (task.Status == "Encoding")
                    IsEncoding = true;
            }

            if (IsEncoding)
            {
                Message mes = new Message(this);
                mes.ShowMessage(Languages.Translate("Some jobs still encoding!"), Languages.Translate("Warning"));
                e.Cancel = true;
                return;
            }
            else
            {
                if (m != null)
                {
                    CloseFile();
                }
            }

            //удаляем мусор
            foreach (string dfile in deletefiles)
            {
                if (!dfile.Contains("cache"))
                    SafeDelete(dfile);
            }

            //подчищаем мусор за FFmpegSource
            if (Settings.DeleteFFCache)
            {
                foreach (string dfile in ffcache)
                    SafeDelete(dfile);

                //Кэш от FFmpegSource2
                foreach (string f in Directory.GetFiles(Settings.TempPath, "*.ffindex"))
                    SafeDelete(f);
            }


            //сообщения о пожертвоаниях
            //if (Convert.ToInt32(Settings.Key) > 10 && !Settings.WasDonate)
            //{
            //   Donate don = new Donate(null);
            //}

            //Определяем и сохраняем размер и положение окна при выходе
            if (this.WindowState != System.Windows.WindowState.Maximized && this.WindowState != System.Windows.WindowState.Minimized) //но только если окно не развернуто на весь экран и не свернуто
            {
                Settings.WindowWidth = Convert.ToInt32(this.Window.ActualWidth); //ширина
                Settings.WindowHeight = Convert.ToInt32(this.Window.ActualHeight); //высота
                Settings.WindowLeft = Convert.ToInt32(this.Window.Left); //отступ слева
                Settings.WindowTop = Convert.ToInt32(this.Window.Top); //отступ сверху
            }
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
                if (Settings.DeleteFFCache)
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

        private void mnOpen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            //if (m != null)
            //    m = null; //тууут

            //открываем файл
            OpenDialogs.owner = this;
            Massive x = OpenDialogs.OpenFile();
            action_open(x,ShowPreview.show);
        }

        private void menu_decode_file_Click(object sender, RoutedEventArgs e)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            //открываем файл
            OpenDialogs.owner = this;
            Massive x = OpenDialogs.OpenFile();

            if (x == null)
                return;

            //присваиваем заданию уникальный ключ
            if (Settings.Key == "9999")
                Settings.Key = "0000";
            x.key = Settings.Key;

            string vpath = Settings.TempPath + "\\" + x.key + ".video.avi";
            Decoder vdec = new Decoder(x, Decoder.DecoderModes.DecodeVideo, vpath);

            string apath = Settings.TempPath + "\\" + x.key + ".audio.wav";
            Decoder adec = new Decoder(x, Decoder.DecoderModes.DecodeAudio, apath);

            //проверка на удачное завершение
            if (File.Exists(vpath) &&
                new FileInfo(vpath).Length != 0)
            {
                x.taskname = Path.GetFileNameWithoutExtension(x.infilepath);
                x.infilepath_source = x.infilepath;
                x.infilepath = vpath;
                x.infileslist = new string[] { vpath };
                x.vdecoder = AviSynthScripting.Decoders.FFmpegSource;
            }

            //проверка на удачное завершение
            if (File.Exists(apath) &&
                new FileInfo(apath).Length != 0)
            {
                MediaInfoWrapper med = new MediaInfoWrapper();
                AudioStream s = med.GetAudioInfoFromAFile(apath);
                s.decoder = AviSynthScripting.Decoders.WAVSource;
                s.gainfile = Settings.TempPath + "\\" + x.key + "_" + 0 + "_gain.wav";
                x.inaudiostreams.Add(s.Clone());
            }

            action_open(x, ShowPreview.show);
        }

        private void mnCloseFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                CloseFile();
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void button_open_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            // if (m != null)
            //      m = null; //тууут

            //открываем файл
            OpenDialogs.owner = this;
            Massive x = OpenDialogs.OpenFile();
            action_open(x, ShowPreview.show);
        }

        private void button_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                CloseFile();
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }

            ////проверка
            //HeaderWriter.Test(m);
        }

        private void mnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                action_save(m.Clone());
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void button_save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                action_save(m.Clone());
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void mnExit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void mnDGIndex_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\DGMPGDec\\DGIndex.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_virtualdubmod_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\VirtualDubMod\\VirtualDubMod.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_home_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (Settings.Language == "Russian" || Settings.Language == "Ukrainian")
                    Process.Start("http://www.ru.winnydows.com");
                else
                    Process.Start("http://www.winnydows.com");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_avisynth_guide_ru_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://avisynth.org.ru/docs/russian/index.htm");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_avisynth_guide_en_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://avisynth.org/mediawiki/Internal_filters");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void mnAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            About a = new About();
            a.Owner = this;
            a.ShowDialog();
        }

        private void mnResetSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string lang = Settings.Language;
            Settings.ResetAllSettings(this);
            Settings.Language = lang;

            Message m = new Message(this);
            m.ShowMessage(Languages.Translate("Settings reseted. Program need restart."), Languages.Translate("Settings"));
            Close();
        }

        private void mnApps_Folder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath);
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
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
                action_open(x, ShowPreview.show);
            }
        }

        private void menu_audiooptions_Click(object sender, RoutedEventArgs e)
        {
            //разрешаем только одно окно
            string stitle = Languages.Translate("Editing audio options") + ":";
            foreach (Window ownedWindow in this.OwnedWindows)
            {
                if (ownedWindow.Title == stitle)
                    return;
            }

            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                //if (m.outacodec == "Copy")
                //{
                //    Message mess = new Message(this);
                //    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                //}
                //else if (m.outacodec == "Disabled")
                //{
                //    Message mess = new Message(this);
                //    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                //}
                //else if (m.inaudiostreams == 0)
                //{
                //    Message mess = new Message(this);
                //    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                //}
                //else
                //{
                AudioOptions ao = new AudioOptions(m, this, AudioOptions.AudioOptionsModes.AllOptions);
                //}
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

                        if (ext != ".d2v" && ext != ".dga")//AVC
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
                ErrorExeption(ex.Message);
            }
        }

        //начало открытия файла
        private void action_open(Massive x, ShowPreview showpreview)
        {
            try
            {
                if (x != null)
                {
                    //сразу-же обнуляем трим и его кнопки
                    trim_start = 0;
                    trim_end = 0;
                    textbox_start.Text = "";
                    textbox_end.Text = "";
                    button_set_start.Content = Languages.Translate("Set Start");
                    button_set_end.Content = Languages.Translate("Set End");
                    button_apply_trim.Content = Languages.Translate("Apply Trim");
                    textbox_start.IsReadOnly = false;
                    textbox_end.IsReadOnly = false;

                    string ext = Path.GetExtension(x.infilepath).ToLower();

                    //проверка на невозможность создать кеш файл для ffmpegsource
                    if (ext == ".avi" &&
                        Settings.AVIDecoder == AviSynthScripting.Decoders.FFmpegSource ||
                        ext != ".avi" &&
                        Settings.OtherDecoder == AviSynthScripting.Decoders.FFmpegSource)
                    {
                        if (Calculate.IsReadOnly(x.infilepath) && !Settings.FFmpegSource2)
                        {
                            Message mess = new Message(this);
                            mess.ShowMessage(Languages.Translate("Input file on CD or DVD! FFmpegSource decode files only from HardDrive.") +
                                Environment.NewLine +
                            Languages.Translate("Copy files to HardDrive or use DirectShowSource decoder.") +
                            Environment.NewLine +
                            Languages.Translate("Switch decoder to DirectShowSource and try again?"),
                                Languages.Translate("Error"), Message.MessageStyle.YesNo);

                            if (mess.result == Message.Result.Yes)
                            {
                                Settings.AVIDecoder = AviSynthScripting.Decoders.DirectShowSource;
                                Settings.OtherDecoder = AviSynthScripting.Decoders.DirectShowSource;
                                mn_avi_dec_ds.IsChecked = true;
                                mn_oth_dec_ds.IsChecked = true;
                            }
                            else
                                return;
                        }
                    }

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

                    //блок для импорта d2v файлов
                    if (ext == ".d2v")
                    {
                        x.indexfile = x.infilepath;
                        ArrayList atracks = Indexing.GetTracks(x.indexfile);
                        int n = 0;
                        if (atracks.Count > 0)
                        {
                            foreach (string apath in atracks)
                            {
                                //забиваем в список все найденные треки
                                MediaInfoWrapper med = new MediaInfoWrapper();
                                // med.Open(apath);
                                AudioStream stream = med.GetAudioInfoFromAFile(apath);
                                stream.delay = Calculate.GetDelay(apath);
                                stream.gainfile = Settings.TempPath + "\\" + x.key + "_" + n + "_gain.wav";
                                x.inaudiostreams.Add(stream.Clone());
                                //  med.Close();
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

                        //удаляем старый файл
                        SafeDelete(vpath);

                        //извлекаем новый файл
                        Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractVideo, vpath);

                        //string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                        //string outpath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + outext;
                        string apath = Settings.TempPath + "\\" + x.key + "_0.mp2";

                        //удаляем старый файл
                        SafeDelete(apath);

                        //извлекаем новый файл
                        Demuxer dem2 = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, apath);

                        //проверка на удачное завершение
                        if (File.Exists(vpath) &&
                            new FileInfo(vpath).Length != 0)
                        {
                            x.infilepath_source = x.infilepath;
                            x.infilepath = vpath;
                            x.infileslist = new string[] { x.infilepath };
                            //x.vdecoder = AviSynthScripting.Decoders.FFmpegSource;
                        }

                        //проверка на удачное завершение
                        if (File.Exists(apath) &&
                            new FileInfo(apath).Length != 0)
                        {
                            AudioStream stream = new AudioStream();
                            stream.audiopath = apath;
                            stream.audiofiles = new string[] { apath };
                            stream = Format.GetValidADecoder(stream);
                            x.inaudiostreams.Add(stream);
                            x.inaudiostream = 0;
                        }
                    }

                    //если файл MPEG делаем запрос на индексацию
                    if (Calculate.IsMPEG(x.infilepath) &&
                        ext != ".d2v")
                    {
                        if (Path.GetExtension(x.infilepath).ToLower() == ".vob" &&
                            Calculate.IsValidVOBName(x.infilepath))
                        {
                            x.dvdname = Calculate.GetDVDName(x.infilepath);

                            string title = Calculate.GetTitleNum(x.infilepath);
                            if (title != "")
                                title = "_T" + title;
                            x.taskname = x.dvdname + title;
                        }

                        if (Settings.MPEGDecoder == AviSynthScripting.Decoders.MPEG2Source)
                        {
                            //проверяем индекс папку
                            IndexChecker ich = new IndexChecker(x);
                            if (ich.m == null) return;
                            x = ich.m.Clone();

                            //индексация
                            if (x.indexfile != null &&
                                !File.Exists(x.indexfile))
                            {
                                Indexing index = new Indexing(x);
                                if (index.m == null) return;
                                x = index.m.Clone();
                            }
                        }
                    }

                    //AVC
                    if (ext == ".dga")
                    {
                        x.indexfile = x.infilepath;
                        ArrayList atracks = Indexing.GetTracks(x.indexfile);
                        int n = 0;
                        if (atracks.Count > 0)
                        {
                            foreach (string apath in atracks)
                            {
                                //забиваем в список все найденные треки
                                MediaInfoWrapper med = new MediaInfoWrapper();
                                //    med.Open(apath);
                                AudioStream stream = med.GetAudioInfoFromAFile(apath);
                                stream.delay = Calculate.GetDelay(apath);
                                stream.gainfile = Settings.TempPath + "\\" + x.key + "_" + n + "_gain.wav";
                                x.inaudiostreams.Add(stream.Clone());
                                //    med.Close();
                                n++;
                            }
                            x.inaudiostream = 0;
                        }
                    }
                    //AVC


                    //получаем информацию через MediaInfo
                    if (ext != ".vdr")
                    {
                        Informer info = new Informer(x);
                        if (info.m == null) return;
                        x = info.m.Clone();
                    }

                    //разборка EVO
                    if (ext == ".evo")
                    {
                        //string outext = Format.GetValidRAWVideoEXT(x);
                        //string vpath = Settings.TempPath + "\\" + x.key + "." + outext;

                        ////удаляем старый файл
                        //SafeDelete(vpath);

                        ////извлекаем новый файл
                        //Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractVideo, vpath);

                        //if (m.inaudiostreams.Count > 0)
                        //{
                        //    AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];
                        //    string outaext = Format.GetValidRAWAudioEXT(instream.codecshort);
                        //    //string outpath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + outext;
                        //    string apath = Settings.TempPath + "\\" + x.key + "_0.mp2";

                        //    //удаляем старый файл
                        //    SafeDelete(apath);

                        //    //извлекаем новый файл
                        //    Demuxer dem2 = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, apath);
                        //}

                        ////проверка на удачное завершение
                        //if (File.Exists(vpath) &&
                        //    new FileInfo(vpath).Length != 0)
                        //{
                        //    x.infilepath_source = x.infilepath;
                        //    x.infilepath = vpath;
                        //    x.infileslist = new string[] { x.infilepath };
                        //    //x.vdecoder = AviSynthScripting.Decoders.FFmpegSource;
                        //}

                        ////проверка на удачное завершение
                        //if (m.inaudiostreams.Count > 0)
                        //{
                        //    if (File.Exists(apath) &&
                        //        new FileInfo(apath).Length != 0)
                        //    {
                        //        AudioStream stream = new AudioStream();
                        //        stream.audiopath = apath;
                        //        stream.audiofiles = new string[] { apath };
                        //        stream = Format.GetValidADecoder(stream);
                        //        x.inaudiostreams.Add(stream);
                        //        x.inaudiostream = 0;
                        //    }
                        //}
                    }

                    //определяем видео декодер
                    if (x.vdecoder == 0)
                        x = Format.GetValidVDecoder(x);

                    //принудительный фикс цвета для DVD
                    if (Settings.AutoColorMatrix &&
                        x.format != Format.ExportFormats.Audio)
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
                    if (x.format != Format.ExportFormats.Audio &&
                        !x.isvideo)
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
                        if (x.inframerate == "")
                        {
                            FramerateDetector frd = new FramerateDetector(x);
                            if (frd.m != null)
                                x = frd.m.Clone();
                        }
                    }

                    if (x == null)
                        return;

                    //блок авто извлечения для быстрого и правильного декодирования через FFMpegSource
                    if (x.vdecoder == AviSynthScripting.Decoders.FFmpegSource)
                    {
                        //проверяем надо ли извлекать видео в отдельный поток
                        FFMpegSourceHelper fhelp = new FFMpegSourceHelper(x);
                        if (fhelp.NeedVExtract)
                        {
                            string outext = Format.GetValidRAWVideoEXT(x);
                            string outpath = Settings.TempPath + "\\" + x.key + "." + outext;

                            //удаляем старый файл
                            SafeDelete(outpath);

                            //извлекаем новый файл
                            Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractVideo, outpath);

                            //проверка на удачное завершение
                            if (File.Exists(outpath) &&
                                new FileInfo(outpath).Length != 0)
                            {
                                x.taskname = Path.GetFileNameWithoutExtension(x.infilepath);
                                x.infilepath_source = x.infilepath;
                                x.infilepath = outpath;
                                x.infileslist = new string[] { x.infilepath };
                            }
                        }

                        //теперь нам надо извлечь звук, если он есть и если точно знаем что за звук
                        if (x.inaudiostreams.Count > 0)
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
                                }
                                else
                                {
                                    Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, outpath);
                                }

                                //проверка на удачное завершение
                                if (File.Exists(outpath) &&
                                    new FileInfo(outpath).Length != 0)
                                {
                                    instream.audiopath = outpath;
                                    instream.audiofiles = new string[] { outpath };
                                    instream = Format.GetValidADecoder(instream);
                                }
                            }
                        }
                    }

                    //получаем выходной фреймрейт
                    x = Format.GetValidFramerate(x);
                    x = Calculate.UpdateOutFrames(x);

                    ////авто ассум fps
                    //double diff;
                    //double infr = Calculate.ConvertStringToDouble(x.inframerate);
                    //double outfr = Calculate.ConvertStringToDouble(x.outframerate);
                    //if (infr > outfr)
                    //    diff = infr - outfr;
                    //else
                    //    diff = outfr - infr;
                    //if (diff > 0 && diff < 1)
                    //    x.frameratemodifer = AviSynthScripting.FramerateModifers.AssumeFPS;
                    ////////////////////////////////////////////////
                    //может вызать проблемы с синхронизацией
                    ////////////////////////////////////////

                    //Получаем информацию через AviSynth и кешируем аудио для FFmpegSource
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

                            //проверка на удачное завершение
                            if (File.Exists(outpath) &&
                                new FileInfo(outpath).Length != 0)
                            {
                                instream.audiopath = outpath;
                                instream.audiofiles = new string[] { outpath };
                                instream = Format.GetValidADecoder(instream);
                            }
                        }
                    }

                    //забиваем-обновляем аудио массивы
                    x = FillAudio(x);

                    //выбираем трек
                    if (x.inaudiostreams.Count > 1)
                    {
                        AudioOptions ao = new AudioOptions(x, this, AudioOptions.AudioOptionsModes.TracksOnly);
                        if (ao.m == null) return;
                        x = ao.m.Clone();
                    }

                    //извлечение трека при badmixing
                    if (x.inaudiostreams.Count == 1)
                    {
                        AudioStream instream = (AudioStream)x.inaudiostreams[x.inaudiostream];

                        if (instream.badmixing)
                        {
                            string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                            instream.audiopath = Settings.TempPath + "\\" + x.key + "_" + x.inaudiostream + outext;
                            instream.audiofiles = new string[] { instream.audiopath };
                            instream = Format.GetValidADecoder(instream);

                            if (!File.Exists(instream.audiopath) &&
                                !Settings.DontDemuxAudio)
                            {
                                Demuxer dem = new Demuxer(x, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                                if (dem.m != null)
                                    x = dem.m.Clone();
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
                                if (sd.m != null)
                                    x = sd.m.Clone();
                            }
                        }
                        else
                        {
                            x = Format.GetOutInterlace(x);
                        }
                    }

                    //ищем субтитры
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        string subs = Calculate.RemoveExtention(x.infilepath, true);
                        if (File.Exists(subs + ".srt"))
                            x.subtitlepath = subs + ".srt";
                        if (File.Exists(subs + ".sub"))
                            x.subtitlepath = subs + ".sub";
                        if (File.Exists(subs + ".idx"))
                            x.subtitlepath = subs + ".idx";
                        if (File.Exists(subs + ".ssa"))
                            x.subtitlepath = subs + ".ssa";
                        if (File.Exists(subs + ".ass"))
                            x.subtitlepath = subs + ".ass";
                        if (File.Exists(subs + ".psb"))
                            x.subtitlepath = subs + ".psb";
                        if (File.Exists(subs + ".smi"))
                            x.subtitlepath = subs + ".smi";
                    }

                    //автокроп
                    if (x.format != Format.ExportFormats.Audio)
                    {
                        if (Settings.AutocropMode == Autocrop.AutocropMode.AllFiles &&
                            x.outvcodec != "Copy" ||
                            Settings.AutocropMode == Autocrop.AutocropMode.MPEGOnly &&
                            Calculate.IsMPEG(x.infilepath) &&
                            x.outvcodec != "Copy")
                        {
                            if (x.format != Format.ExportFormats.BluRay)
                            {
                                Autocrop acrop = new Autocrop(x);
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
                        x = Format.GetValidFramerate(x);
                        x = Calculate.UpdateOutFrames(x);

                        //обновление выходных битрейтов
                        if (x.outvcodec == "Disabled")
                            x.outvbitrate = 0;
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

                    //создаём AviSynth скрипт
                    x = AviSynthScripting.CreateAutoAviSynthScript(x);

                    //проверяем скрипт на ошибки и пытаемся их автоматически исправить
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

                    //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                    x = Calculate.UpdateOutFrames(x);

                    //проверка на размер
                    x.outfilesize = Calculate.GetEncodingSize(x);

                    //по умолчанию середина для картинки
                    x.thmframe = x.outframes / 2;

                    //запрежаем профиль кодирования если нет звука
                    if (x.inaudiostreams.Count == 0)
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
                            mess.ShowMessage(Languages.Translate("Stream contein parameter incompatible with format") +
                                " " + Format.EnumToString(x.format) + " - " + CopyProblems + ".", Languages.Translate("Warning"));
                        }
                    }

                    //настройки форматов
                    if (x.format == Format.ExportFormats.Mpeg2PAL ||
                        x.format == Format.ExportFormats.Mpeg2NTSC)
                        x.dontmuxstreams = Settings.Mpeg2MultiplexDisabled;
                    x.split = Settings.GetFormatPreset(x.format, "split");

                    //передаём массив
                    m = x.Clone();

                    //AviSynthScripting.WriteScriptToFile(m.script, "xxx");

                    //menu_demux.Header = Languages.Translate("Save to") + " " + m.inacodecshort;
                    //menu_demux_video.Header = Languages.Translate("Save to") + " " +
                    //    Format.GetValidRAWVideoEXT(m.invcodecshort, m.infilepath).ToUpper();

                    //снимаем выделение
                    list_tasks.SelectedIndex = -1;
                    //OldSelectedIndex = -1;

                    //загружаем скрипт в форму
                    if (showpreview != ShowPreview.dntshow)
                         LoadVideo(MediaLoad.load);

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
                    ErrorExeption(ex.Message);
            }
            x = null;//тууут
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
                    ErrorExeption(Languages.Translate("Select other name for output file!"));
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

                        //добавляем задание в список
                        mass = UpdateOutAudioPath(mass);
                        AddTask(mass, "Waiting");
                    }
                }
            }
        }

        private void action_encode(Massive mass)
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            if (mass.inaudiostreams.Count > 0 &&
                mass.outaudiostreams.Count > 0)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];
                AudioStream outstream = (AudioStream)mass.outaudiostreams[mass.outaudiostream];

                if (instream.audiopath != null &&
                   !File.Exists(instream.audiopath))
                {
                    if (!Format.IsDirectRemuxingPossible(mass) &&
                    outstream.codec == "Copy" ||
                        Format.IsDirectRemuxingPossible(mass) &&
                        outstream.codec != "Copy")
                    {
                        Demuxer dem = new Demuxer(mass, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                        if (dem.m != null)
                            mass = dem.m.Clone();

                        //обновляем скрипт
                        mass = AviSynthScripting.CreateAutoAviSynthScript(mass);

                        UpdateTaskMassive(mass);
                    }
                }

                //определяем громкоcть (перед кодированием)
                if (Settings.Volume != "Disabled" && Settings.AutoVolumeMode == Settings.AutoVolumeModes.OnExport &&
                    !instream.gaindetected &&
                    outstream.codec != "Copy" &&
                    outstream.codec != "Disabled")
                {
                    mass.volume = Settings.Volume;
                    Normalize norm = new Normalize(mass);
                    mass = norm.m.Clone();
                    //mass = AviSynthScripting.CreateAutoAviSynthScript(mass);
                    mass = AviSynthScripting.SetGain(mass);

                    UpdateTaskMassive(mass);
                }
            }

            try
            {
                //запускаем кодер
                Encoder enc = new Encoder(mass, this);
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void CloseFile()
        {
            //закрываем все дочерние окна
            CloseChildWindows();

            CloseClip();
            clear_ff_cache();
            SafeDelete(Settings.TempPath + "\\preview.avs");
            SafeDelete(Settings.TempPath + "\\AvsP.avs");
            SafeDelete(Settings.TempPath + "\\AutoCrop.log");

            //обнуляем всё что связано с тримом
            trim_start = 0;
            trim_end = 0;
            textbox_start.Text = "";
            textbox_end.Text = "";
            button_set_start.Content = Languages.Translate("Set Start");
            button_set_end.Content = Languages.Translate("Set End");
            button_apply_trim.Content = Languages.Translate("Apply Trim");
            textbox_start.IsReadOnly = false;
            textbox_end.IsReadOnly = false;


            if (m.infilepath != null &&
    Path.GetDirectoryName(m.infilepath) == Settings.TempPath)
                SafeDelete(m.infilepath);

            foreach (object s in m.inaudiostreams)
            {
                AudioStream a = (AudioStream)s;
                if (a.audiopath != null &&
                    Path.GetDirectoryName(a.audiopath) == Settings.TempPath)
                    SafeDelete(a.audiopath);
                SafeDelete(a.gainfile);
            }

            foreach (object s in m.outaudiostreams)
            {
                AudioStream a = (AudioStream)s;
                if (a.audiopath != null &&
                    Path.GetDirectoryName(a.audiopath) == Settings.TempPath)
                    SafeDelete(a.audiopath);
            }

            m = null;
        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        public void LoadVideo(MediaLoad mediaload)
        {
            oldpos = Position;
            this.mediaload = mediaload;

            // If we have ANY file open, close it and shut down DirectShow
            if (this.currentState != PlayState.Init)
                CloseClip();

            //удаляем старый скрипт
            File.Delete(Settings.TempPath + "\\preview.avs");
            string preview_script = AviSynthScripting.GetPreviewScript(m);
            AviSynthScripting.WriteScriptToFile(preview_script, "preview");
            AviSynthScripting.WriteScriptToFile(m.script, "AvsP"); //пишет в файл AvsP.avs скрипт для Avsp

            try
            {
                // Reset status variables
                if (mediaload != MediaLoad.update)
                    this.currentState = PlayState.Stopped;

                // Start playing the media file
                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                    PlayMovieInWindow(Settings.TempPath + "\\preview.avs");
                if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                    PlayWithMediaBridge(Settings.TempPath + "\\preview.avs");

                //обновляем счетчик кадров
                total_frames = Convert.ToString((int)(NaturalDuration.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate)));
                textbox_frame.Text = Convert.ToString((int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate))) + "/" + total_frames;
            }
            catch (Exception ex)
            {
                if (this.currentState != PlayState.Init)
                    CloseClip();

                m = null;

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
                {
                    ErrorExeption(ex.Message);
                }

                return;
            }

            textbox_name.Text = m.taskname;
            slider_pos.Focus(); //Переводит фокус на полосу прокрутки видео
        }

        //переводим лейблы
        private void SetLanguage()
        {
            mnFile.Header = Languages.Translate("File");
            mnOpen.Header = Languages.Translate("Open file") + "...";
            menu_decode_file.Header = Languages.Translate("Decode file") + "...";
            menu_join.Header = Languages.Translate("Join file") + "...";
            mnSave.Header = Languages.Translate("Save file") + "...";
            menu_dvd.Header = Languages.Translate("Open DVD folder") + "...";
            button_dvd.ToolTip = Languages.Translate("Open DVD folder");
            mnExit.Header = Languages.Translate("Exit");
            mnCloseFile.Header = Languages.Translate("Close file");
            menu_save_frame.Header = Languages.Translate("Save frame") + "...";
            menu_savethm.Header = Languages.Translate("Save") + " THM...";

            mnVideo.Header = Languages.Translate("Video");
            mnAudio.Header = Languages.Translate("Audio");
            mnSubtitles.Header = Languages.Translate("Subtitles");
            //mnPlayer.Header = Languages.Translate("Player");
            menu_audiooptions.Header = Languages.Translate("Editing options");
            menu_save_wav.Header = Languages.Translate("Save to WAV");
            //menu_demux.Header = Languages.Translate("Save to");
            menu_demux.Header = Languages.Translate("Demux");
            //menu_demux_video.Header = Languages.Translate("Save to");
            menu_demux_video.Header = Languages.Translate("Demux");

            mnUpdateVideo.Header = Languages.Translate("Refresh preview");
            menu_createautoscript.Header = Languages.Translate("Create auto script");
            menu_editscript.Header = Languages.Translate("Edit filtering script");

            mnAspectResolution.Header = Languages.Translate("Resolution/Aspect") + "...";
            menu_interlace.Header = Languages.Translate("Interlace") + "/" + Languages.Translate("Framerate");

            mnAddSubtitles.Header = Languages.Translate("Add");
            mnRemoveSubtitles.Header = Languages.Translate("Remove");

            menu_save_script.Header = Languages.Translate("Save script");

            menu_playinwmp.Header = Languages.Translate("Play in") + " Windows Media Player";
            menu_payinmpc.Header = Languages.Translate("Play in") + " Media Player Classic";
            menu_playinwpf.Header = Languages.Translate("Play in") + " WPF Video Player";

            mnSettings.Header = Languages.Translate("Settings");
            menu_settings.Header = Languages.Translate("Global settings");
            mnLanguage.Header = Languages.Translate("Language");
            mnResetSettings.Header = Languages.Translate("Reset all settings");
            mnAfterImport.Header = Languages.Translate("After opening");
            menu_after_i_play.Content = Languages.Translate("Play");
            menu_after_i_nothing.Content = Languages.Translate("Nothing");
            menu_after_i_middle.Content = Languages.Translate("Middle");

            menu_auto_crop.Header = Languages.Translate("Auto crop");
            menu_auto_volume.Header = Languages.Translate("Auto volume");
            menu_auto_volume_disabled.Content = Languages.Translate("Disabled");
            menu_auto_volume_onexp.Content = Languages.Translate("Before encoding");
            menu_auto_volume_onimp.Content = Languages.Translate("After opening");
            menu_acrop_mpeg.Content = Languages.Translate("MPEG`s only");
            menu_acrop_disabled.Content = Languages.Translate("Disabled");
            menu_acrop_allfiles.Content = Languages.Translate("All files");

            menu_auto_deinterlace.Header = Languages.Translate("Auto deinterlace");
            check_auto_deint_all.Content = Languages.Translate("All files");
            check_auto_deint_disabled.Content = Languages.Translate("Disabled");
            check_auto_deint_mpeg.Content = Languages.Translate("MPEG`s only");

            menu_auto_join.Header = Languages.Translate("Auto join");
            check_auto_join_disabled.Content = Languages.Translate("Disabled");
            check_auto_join_enabled.Content = Languages.Translate("Enabled");
            check_auto_join_onlydvd.Content = Languages.Translate("DVD Only");

            mnVideoDecoding.Header = Languages.Translate("Decoding");
            mnAVIFiles.Header = "AVI " + Languages.Translate("files");
            mnMPEGFiles.Header = "MPEG " + Languages.Translate("files");
            mnOtherFiles.Header = Languages.Translate("Other files");

            menu_fix_AVCHD.Header = Languages.Translate("Convert BluRay UDF to FAT32");

            menu_player_engine.Header = Languages.Translate("Player engine");

            mnTools.Header = Languages.Translate("Tools");
            mnHelp.Header = Languages.Translate("Help");
            mnAbout.Header = Languages.Translate("About");

            text_vencoding.Content = Languages.Translate("Video encoding") + ":";
            text_aencoding.Content = Languages.Translate("Audio encoding") + ":";
            text_filtering.Content = Languages.Translate("Filtering") + ":";
            text_sbc.Content = Languages.Translate("Color correction") + ":";
            menu_saturation_brightness.Header = Languages.Translate("Color correction");
            text_format.Content = Languages.Translate("Format") + ":";
            button_edit_filters.ToolTip = Languages.Translate("Edit filtering script");
            button_edit_vencoding.ToolTip = Languages.Translate("Edit video encoding settings");
            button_edit_aencoding.ToolTip = Languages.Translate("Edit audio encoding settings");
            menu_aenc_settings.Header = Languages.Translate("Encoding settings");
            menu_venc_settings.Header = Languages.Translate("Encoding settings");
            button_edit_sbc.ToolTip = Languages.Translate("Edit saturation, brightness or contrast");
            button_edit_format.ToolTip = Languages.Translate("Edit format settings");

            button_open.Content = Languages.Translate("Open");
            button_open.ToolTip = Languages.Translate("Open new file");
            button_configure.Content = Languages.Translate("Configure");
            button_configure.ToolTip = Languages.Translate("Configure editing and encoding options");
            button_save.Content = Languages.Translate("Save");
            button_save.ToolTip = Languages.Translate("Save task to list");
            button_encode.Content = Languages.Translate("Encode");
            button_encode.ToolTip = Languages.Translate("Start files encoding");
            button_close.Content = Languages.Translate("Close");
            button_close.ToolTip = Languages.Translate("Close current file");

            list_tasks.ToolTip = Languages.Translate("Task list");

            button_play.ToolTip = Languages.Translate("Play-Pause");
            button_frame_back.ToolTip = Languages.Translate("Frame back");
            button_frame_forward.ToolTip = Languages.Translate("Frame forward");

            cmenu_deselect.Header = Languages.Translate("Deselect");
            cmenu_delete_all_tasks.Header = Languages.Translate("Delete all tasks");
            cmenu_delete_encoded_tasks.Header = Languages.Translate("Delete encoded tasks");
            cmenu_delete_task.Header = Languages.Translate("Delete selected task");
            cmenu_is_always_close_encoding.Content = Languages.Translate("Always close encoding task dialog");
            cmenu_is_always_delete_encoded.Content = Languages.Translate("Always delete from list encoded tasks");
            cmenu_reset_status.Header = Languages.Translate("Reset task status");

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
            menu_open_folder.Header = Languages.Translate("Open folder...");
            mnApps_Folder.Header =Languages.Translate("Open XviD4PSP folder");

            //Тултипы для выбора видео-декодера
            mnAVIFiles.ToolTip = "Choose decoder that will be used for decoding AVI-files.";
            mn_avi_dec_ds.ToolTip = "This decoder will be use installed on your system DirecShow filters-decoders (and theirs settings!) for decoding Audio and Video from your file.";
            mn_avi_dec_ds2.ToolTip = "Mostly the same as DirectShowSource, but from Haali. This decoder can provide frame-accuracy seeking," + Environment.NewLine + "and didn`t use your system decoders for decoding Audio.";
            mn_avi_dec_ff.ToolTip = "This decoder (old or new) is fully independed from your system-decoders and theirs settings. But need some time for indexing video (especialy new FFmpegSource2).";
            mnMPEGFiles.ToolTip = "Choose decoder that will be used for decoding MPEG-files.";
            mn_mpg_dec_ds.ToolTip = "This decoder will be use installed on your system DirecShow filters-decoders (and theirs settings!) for decoding Audio and Video from your file.";
            mn_mpg_dec_ds2.ToolTip = "Mostly the same as DirectShowSource, but from Haali. This decoder can provide frame-accuracy seeking," + Environment.NewLine + "and didn`t use your system decoders for decoding Audio.";
            mn_mpg_dec_ff.ToolTip = "This decoder (old or new) is fully independed from your system-decoders and theirs settings. But need some time for indexing video (especialy new FFmpegSource2).";
            mn_mpg_dec_mpg.ToolTip = "I think it`s better decoder for decoding MPEG-files. Fully independed and frame-accurate.";
            check_force_film.ToolTip = "Turns on ForcedFilm for DGIndex(MPEG2Source), it will reduce fps to 23,976. Read DGIndex manual for more info!" + Environment.NewLine + "DON`T FORGET TO TURN IT OFF AFTER USING! NEWER USE IT IF YOU DON`T KNOW WHAT IT`S ALL ABOUT!";
            mnOtherFiles.ToolTip = "Choose decoder that will be used for decoding other file-types.";
            mn_oth_dec_ds.ToolTip = "This decoder will be use installed on your system DirecShow filters-decoders (and theirs settings!) for decoding Audio and Video from your file.";
            mn_oth_dec_ds2.ToolTip = "Mostly the same as DirectShowSource, but from Haali. This decoder can provide frame-accuracy seeking," + Environment.NewLine + "and didn`t use your system decoders for decoding Audio.";
            mn_oth_dec_ff.ToolTip = "This decoder (old or new) is fully independed from your system-decoders and theirs settings. But need some time for indexing video (especialy new FFmpegSource2).";
            mnFFmpegSource.ToolTip = "Choose what kind of FFmpegSource (old or new) will be used, if FFmpegSource is specified as decoder for the current file-type.";
            
        }

        //загружаем настройки
        private void LoadSettings()
        {
            string l = Settings.Language;
            if (l == "Russian")
                mnRussian.IsChecked = true;
            else if (l == "Italian")
                check_italian.IsChecked = true;
            else if (l == "German")
                check_german.IsChecked = true;
            else if (l == "Hebrew")
                check_hebrew.IsChecked = true;
            else if (l == "Spanish")
                check_spanish.IsChecked = true;
            else if (l == "French")
                check_french.IsChecked = true;
            else if (l == "Portuguese")
                check_portuguese.IsChecked = true;
            else if (l == "Chinese")
                check_chinese.IsChecked = true;
            else
                mnEnglish.IsChecked = true;

            if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                check_engine_mediabridge.IsChecked = true;
            else if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                check_engine_directshow.IsChecked = true;

            if (Settings.AutoJoinMode == Settings.AutoJoinModes.Disabled)
                check_auto_join_disabled.IsChecked = true;
            else if (Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly)
                check_auto_join_onlydvd.IsChecked = true;
            else if (Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled)
                check_auto_join_enabled.IsChecked = true;

            if (Settings.AVIDecoder == AviSynthScripting.Decoders.DirectShowSource)
                mn_avi_dec_ds.IsChecked = true;
            else if (Settings.AVIDecoder == AviSynthScripting.Decoders.FFmpegSource)
                mn_avi_dec_ff.IsChecked = true;
            else if (Settings.AVIDecoder == AviSynthScripting.Decoders.AVISource)
                mn_avi_dec_avi.IsChecked = true;

            if (Settings.MPEGDecoder == AviSynthScripting.Decoders.DirectShowSource)
                mn_mpg_dec_ds.IsChecked = true;
            else if (Settings.MPEGDecoder == AviSynthScripting.Decoders.FFmpegSource)
                mn_mpg_dec_ff.IsChecked = true;
            else if (Settings.MPEGDecoder == AviSynthScripting.Decoders.MPEG2Source)
                mn_mpg_dec_mpg.IsChecked = true;

            if (Settings.OtherDecoder == AviSynthScripting.Decoders.DirectShowSource)
                mn_oth_dec_ds.IsChecked = true;
            else if (Settings.OtherDecoder == AviSynthScripting.Decoders.DSS2)
                mn_oth_dec_ds2.IsChecked = true;
            else if (Settings.OtherDecoder == AviSynthScripting.Decoders.FFmpegSource)
                mn_oth_dec_ff.IsChecked = true;

            if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                menu_after_i_middle.IsChecked = true;
            else if (Settings.AfterImportAction == Settings.AfterImportActions.Nothing)
                menu_after_i_nothing.IsChecked = true;
            else if (Settings.AfterImportAction == Settings.AfterImportActions.Play)
                menu_after_i_play.IsChecked = true;

            if (Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.AllFiles)
                check_auto_deint_all.IsChecked = true;
            else if (Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.Disabled)
                check_auto_deint_disabled.IsChecked = true;
            else if (Settings.AutoDeinterlaceMode == Settings.AutoDeinterlaceModes.MPEGs)
                check_auto_deint_mpeg.IsChecked = true;

            if (Settings.AutocropMode == Autocrop.AutocropMode.AllFiles)
                menu_acrop_allfiles.IsChecked = true;
            else if (Settings.AutocropMode == Autocrop.AutocropMode.Disabled)
                menu_acrop_disabled.IsChecked = true;
            else if (Settings.AutocropMode == Autocrop.AutocropMode.MPEGOnly)
                menu_acrop_mpeg.IsChecked = true;

            if (Settings.AutoVolumeMode == Settings.AutoVolumeModes.Disabled)
                menu_auto_volume_disabled.IsChecked = true;
            else if (Settings.AutoVolumeMode == Settings.AutoVolumeModes.OnImport)
                menu_auto_volume_onimp.IsChecked = true;
            else if (Settings.AutoVolumeMode == Settings.AutoVolumeModes.OnExport)
                menu_auto_volume_onexp.IsChecked = true;

            cmenu_is_always_delete_encoded.IsChecked = Settings.AutoDeleteTasks;
            cmenu_is_always_close_encoding.IsChecked = Settings.AutoClose;

            //Установка параметров регулятора громкости
            slider_Volume.Minimum = 0; //-10000
            slider_Volume.Maximum = 1; //0
            slider_Volume.SmallChange = 0.05;
            slider_Volume.Value = Settings.VolumeLevel; //Установка значения громкости из реестра..
            //VolumeSet = Convert.ToInt32(-(10000 - Math.Sqrt(slider_Volume.Value) * 10000)); //.. и пересчет его для ДиректШоу
            VolumeSet = Convert.ToInt32(-(10000 - Math.Pow(slider_Volume.Value, 1.0 / 3) * 10000)); //.. и пересчет его для ДиректШоу (корень кубический)

            if (Settings.FFmpegSource2)
                mn_ffmpeg_new.IsChecked = true;
            else
                mn_ffmpeg_old.IsChecked = true;

            if (Settings.DGForceFilm)
                check_force_film.IsChecked = true;

        }

        private void mnEnglish_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mnEnglish.IsFocused)
            {
                Settings.Language = "English";
                SetLanguage();
            }
        }

        private void mnRussian_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mnRussian.IsFocused)
            {
                Settings.Language = "Russian";
                SetLanguage();
            }
        }

        private void check_italian_Checked(object sender, RoutedEventArgs e)
        {
            if (check_italian.IsFocused)
            {
                Settings.Language = "Italian";
                SetLanguage();
            }
        }

        private void check_chinese_Checked(object sender, RoutedEventArgs e)
        {
            if (check_chinese.IsFocused)
            {
                Settings.Language = "Chinese";
                SetLanguage();
            }
        }

        private void check_portuguese_Checked(object sender, RoutedEventArgs e)
        {
            if (check_portuguese.IsFocused)
            {
                Settings.Language = "Portuguese";
                SetLanguage();
            }
        }

        private void check_spanish_Checked(object sender, RoutedEventArgs e)
        {
            if (check_spanish.IsFocused)
            {
                Settings.Language = "Spanish";
                SetLanguage();
            }
        }

        private void check_german_Checked(object sender, RoutedEventArgs e)
        {
            if (check_german.IsFocused)
            {
                Settings.Language = "German";
                SetLanguage();
            }
        }

        private void check_hungarian_Checked(object sender, RoutedEventArgs e)
        {
            if (check_hungarian.IsFocused)
            {
                Settings.Language = "Hungarian";
                SetLanguage();
            }
        }

        private void check_ukrainian_Checked(object sender, RoutedEventArgs e)
        {
            if (check_ukrainian.IsFocused)
            {
                Settings.Language = "Ukrainian";
                SetLanguage();
            }
        }

        private void check_french_Checked(object sender, RoutedEventArgs e)
        {
            if (check_french.IsFocused)
            {
                Settings.Language = "French";
                SetLanguage();
            }
        }

        private void check_hebrew_Checked(object sender, RoutedEventArgs e)
        {
            if (check_hebrew.IsFocused)
            {
                Settings.Language = "Hebrew";
                SetLanguage();
            }
        }

        private void mnUpdateVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                LoadVideo(MediaLoad.update);
                UpdateTaskMassive(m);
            }
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void mnAddSubtitles_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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

        private void mnRemoveSubtitles_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (m.subtitlepath != null)
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

        private void slider_pos_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {

            if (slider_pos.IsMouseOver && this.graphBuilder != null)
            {
                Visual visual = Mouse.Captured as Visual;
                if (visual != null && visual.IsDescendantOf(slider_pos))
                {
                    Position = TimeSpan.FromSeconds(slider_pos.Value);

                    //устанавливаем фрейм для THM
                    if (m != null)
                        m.thmframe = (int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate));
                }

            }
            if (m != null) //обновляем текущий кадр/кол-во кадров
                textbox_frame.Text = Convert.ToString((int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate))) + "/" + total_frames;
            //textbox_frame.Text = Convert.ToString((int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate))) + "/" + 
            //Convert.ToString((int)(NaturalDuration.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate)));
        }

        private void button_play_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
                PauseClip();
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void button_frame_back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                try
                {
                    if (currentState == PlayState.Running)
                        PauseClip();

                    double framerate = Calculate.ConvertStringToDouble(m.outframerate);
                    TimeSpan newpos = Position - TimeSpan.FromMilliseconds(1000.0 / framerate);
                    if (newpos >= TimeSpan.Zero)
                        Position = newpos;

                    //button_frame_back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
            }
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void button_frame_forward_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                try
                {
                    if (currentState == PlayState.Running)
                        PauseClip();

                    double framerate = Calculate.ConvertStringToDouble(m.outframerate);
                    TimeSpan newpos = Position + TimeSpan.FromMilliseconds(1000.0 / framerate);
                    if (newpos <= NaturalDuration)
                        Position = newpos;
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
            }
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void check_engine_mediabridge_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_engine_mediabridge.IsFocused)
            {
                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                {
                    PlayState cstate = currentState;

                    if (m != null)
                        CloseClip();

                    //remove events
                    this.LocationChanged -= new EventHandler(MainWindow_LocationChanged);
                    this.SizeChanged -= new SizeChangedEventHandler(MainWindow_SizeChanged);
                    this.grid_tasks.SizeChanged -= new SizeChangedEventHandler(grid_tasks_SizeChanged);
                    this.grid_player_window.MouseDown -= new MouseButtonEventHandler(Direct_Show_Mouse_Click); //мышь для Фуллскрина при ДиректШоу
                    this.grid_player_buttons.MouseDown -= new MouseButtonEventHandler(Direct_Show_Mouse_Click); //мышь для Фуллскрина при ДиректШоу

                    source.RemoveHook(new HwndSourceHook(WndProc));

                    //close timer
                    timer.Stop();
                    timer.Enabled = false;
                    timer.Close();
                    timer.Dispose();
                    timer = null;

                    //set media element state for video loading
                    VideoElement.LoadedBehavior = MediaState.Manual;
                    //VideoElement.UnloadedBehavior = MediaState.Stop;
                    VideoElement.ScrubbingEnabled = true;

                    //add new events
                    VideoElement.MediaOpened += new RoutedEventHandler(VideoElement_MediaOpened);
                    VideoElement.MediaEnded += new RoutedEventHandler(VideoElement_MediaEnded);
                    VideoElement.MouseDown += new MouseButtonEventHandler(VideoElement_MouseDown);
                    VisualTarget.Rendering += new EventHandler(VisualTarget_Rendering);

                    VideoElement.Visibility = Visibility.Visible;

                    currentState = cstate;

                    Settings.PlayerEngine = Settings.PlayerEngines.MediaBridge;
                    if (m != null)
                    {
                        LoadVideo(MediaLoad.update);
                    }
                }
            }
        }

        private void check_engine_directshow_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_engine_directshow.IsFocused)
            {
                if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                {
                    PlayState cstate = currentState;

                    if (m != null)
                        CloseClip();

                    //remove events
                    VideoElement.MediaOpened -= new RoutedEventHandler(VideoElement_MediaOpened);
                    VideoElement.MediaEnded -= new RoutedEventHandler(VideoElement_MediaEnded);
                    VideoElement.MouseDown -= new MouseButtonEventHandler(VideoElement_MouseDown);
                    VisualTarget.Rendering -= new EventHandler(VisualTarget_Rendering);

                    //add new events
                    this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
                    this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
                    this.grid_tasks.SizeChanged += new SizeChangedEventHandler(grid_tasks_SizeChanged);
                    this.grid_player_window.MouseDown += new MouseButtonEventHandler(Direct_Show_Mouse_Click); //мышь для Фуллскрина при ДиректШоу
                    this.grid_player_buttons.MouseDown += new MouseButtonEventHandler(Direct_Show_Mouse_Click); //мышь для Фуллскрина при ДиректШоу

                    source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                    source.AddHook(new HwndSourceHook(WndProc));

                    //timer
                    timer = new System.Timers.Timer();
                    timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                    timer.Interval = 10;
                    timer.Enabled = true;
                    timer.Start();

                    VideoElement.Visibility = Visibility.Collapsed;

                    currentState = cstate;

                    Settings.PlayerEngine = Settings.PlayerEngines.DirectShow;
                    if (m != null)
                    {
                        LoadVideo(MediaLoad.update);
                    }
                }
            }
        }

        private void check_auto_join_disabled_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_auto_join_disabled.IsFocused)
                Settings.AutoJoinMode = Settings.AutoJoinModes.Disabled;
        }

        private void check_auto_join_enabled_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_auto_join_enabled.IsFocused)
                Settings.AutoJoinMode = Settings.AutoJoinModes.Enabled;
        }

        private void check_auto_join_onlydvd_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_auto_join_onlydvd.IsFocused)
                Settings.AutoJoinMode = Settings.AutoJoinModes.DVDonly;
        }

        private void menu_createautoscript_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                CreateAutoScript();
            }
        }

        public void Refresh(string script)
        {
            m.script = script;
            LoadVideo(MediaLoad.update);
            //MoveVideoWindow();//
            UpdateTaskMassive(m);
        }

        public Massive CreateAutoScript()
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
            if (oldscript != m.script)
            {
                LoadVideo(MediaLoad.update);
                UpdateTaskMassive(m);
            }
            return m;
        }

        private void menu_editscript_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EditScript();
        }

        public void EditScript()
        {

            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                //разрешаем только одно окно
                //string stitle = "XviD4PSP - AviSynth-based MultiMedia Converter";
                //foreach (Window ownedWindow in this.OwnedWindows)
                //{
                //    if (ownedWindow.Title != stitle)
                //       return;
                //}            

                try
                {
                    Filtering f = new Filtering(m, this);
                    string oldscript = m.script;
                    m = f.m.Clone();
                    //обновление при необходимости
                    if (m.script != oldscript)
                    {
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
            }
        }

        private void mnAspectResolution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AspectResolutionWindow();
        }

        private void AspectResolutionWindow()
        {

            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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
                    //разрешаем только одно окно
                    //string stitle = "XviD4PSP - AviSynth-based MultiMedia Converter";
                    //foreach (Window ownedWindow in this.OwnedWindows)
                    //{
                    //    if (ownedWindow.Title != stitle)
                    //        return;
                    //}                                   

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

        private void mn_avi_dec_ds_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_avi_dec_ds.IsFocused)
                Settings.AVIDecoder = AviSynthScripting.Decoders.DirectShowSource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext == ".avi")
                {
                    reopen_file();
                }
            }
        }

        private void mn_avi_dec_ds2_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_avi_dec_ds2.IsFocused)
                Settings.AVIDecoder = AviSynthScripting.Decoders.DSS2;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext == ".avi")
                {
                    reopen_file();
                }
            }
        }


        private void mn_avi_dec_ff_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_avi_dec_ff.IsFocused)
                Settings.AVIDecoder = AviSynthScripting.Decoders.FFmpegSource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext == ".avi")
                {
                    reopen_file();
                }
            }
        }

        private void mn_avi_dec_avi_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_avi_dec_avi.IsFocused)
                Settings.AVIDecoder = AviSynthScripting.Decoders.AVISource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext == ".avi")
                {
                    reopen_file();
                }
            }
        }

        private void mn_mpg_dec_ds_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_mpg_dec_ds.IsFocused)
                Settings.MPEGDecoder = AviSynthScripting.Decoders.DirectShowSource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".d2v" && Calculate.IsMPEG(m.infilepath))
                {
                    m.oldindexfile = m.indexfile;
                    m.indexfile = null;
                    reopen_file();
                }
            }
        }

        private void mn_mpg_dec_ds2_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_mpg_dec_ds2.IsFocused)
                Settings.MPEGDecoder = AviSynthScripting.Decoders.DSS2;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".d2v" && Calculate.IsMPEG(m.infilepath))
                {
                    m.oldindexfile = m.indexfile;
                    m.indexfile = null;
                    reopen_file();
                }
            }
        }

        private void mn_mpg_dec_ff_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_mpg_dec_ff.IsFocused)
                Settings.MPEGDecoder = AviSynthScripting.Decoders.FFmpegSource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".d2v" && Calculate.IsMPEG(m.infilepath))
                {
                    m.oldindexfile = m.indexfile;
                    m.indexfile = null;
                    reopen_file();
                }
            }
        }

        private void mn_mpg_dec_mpg_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_mpg_dec_mpg.IsFocused)
                Settings.MPEGDecoder = AviSynthScripting.Decoders.MPEG2Source;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".d2v" && Calculate.IsMPEG(m.infilepath))
                {
                    if (m.oldindexfile != null)
                        m.indexfile = m.oldindexfile;
                    reopen_file();
                }
            }
        }

        private void mn_oth_dec_ds_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_oth_dec_ds.IsFocused)
                Settings.OtherDecoder = AviSynthScripting.Decoders.DirectShowSource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".avi" && !Calculate.IsMPEG(m.infilepath))
                {
                    reopen_file();
                }
            }
        }

        private void mn_oth_dec_ds2_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_oth_dec_ds2.IsFocused)
                Settings.OtherDecoder = AviSynthScripting.Decoders.DSS2;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".avi" && !Calculate.IsMPEG(m.infilepath))
                {
                    reopen_file();
                }
            }
        }


        private void mn_oth_dec_ff_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mn_oth_dec_ff.IsFocused)
                Settings.OtherDecoder = AviSynthScripting.Decoders.FFmpegSource;
            if (m != null)
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (ext != ".avi" && !Calculate.IsMPEG(m.infilepath))
                {
                    reopen_file();
                }
            }
        }

        //повторное открытие файла после смены декодера
        private void reopen_file()
        {
            m = Format.GetValidVDecoder(m);

            //определяем аудио декодер
            foreach (object o in m.inaudiostreams)
            {
                AudioStream s = (AudioStream)o;
                s = Format.GetValidADecoder(s);
            }

            m = AviSynthScripting.CreateAutoAviSynthScript(m);

            //Получаем информацию через AviSynth и кешируем аудио для FFmpegSource
            Caching cach = new Caching(m);
            m = cach.m.Clone();

            //перезабиваем специфику формата
            m = Format.GetOutInterlace(m);
            m = Format.GetValidResolution(m);
            m = Format.GetValidOutAspect(m);
            m = AspectResolution.FixAspectDifference(m);
            m = Format.GetValidFramerate(m);
            m = Calculate.UpdateOutFrames(m);

            m = FillAudio(m);

            //обнуляем громкость
            foreach (object s in m.inaudiostreams)
            {
                AudioStream stream = (AudioStream)s;
                SafeDelete(stream.gainfile);
                stream.gain = "0.0";
                stream.gaindetected = false;
            }

            //определяем аудио потоки
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                //передеаём задержку
                instream.delay = 0;
            }

            //обновляем скрипт с учётом полученных данных
            m = AviSynthScripting.CreateAutoAviSynthScript(m);

            //обновляем дочерние окна
            ReloadChildWindows();

            //загружаем видео в плеер
            LoadVideo(MediaLoad.update);
            UpdateTaskMassive(m);

        }





        private void menu_after_i_nothing_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_after_i_nothing.IsFocused)
                Settings.AfterImportAction = Settings.AfterImportActions.Nothing;
        }

        private void menu_after_i_middle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_after_i_middle.IsFocused)
                Settings.AfterImportAction = Settings.AfterImportActions.Middle;
        }

        private void menu_after_i_play_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_after_i_play.IsFocused)
                Settings.AfterImportAction = Settings.AfterImportActions.Play;
        }

        private void menu_acrop_disabled_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_acrop_disabled.IsFocused)
                Settings.AutocropMode = Autocrop.AutocropMode.Disabled;
        }

        private void menu_acrop_mpeg_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_acrop_mpeg.IsFocused)
                Settings.AutocropMode = Autocrop.AutocropMode.MPEGOnly;
        }

        private void menu_acrop_allfiles_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_acrop_allfiles.IsFocused)
                Settings.AutocropMode = Autocrop.AutocropMode.AllFiles;
        }

        private void menu_save_script_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                s.FileName = Path.GetFileNameWithoutExtension(m.infilepath);

                if (Path.GetExtension(m.infilepath).ToLower() == ".vob" && Calculate.IsValidVOBName(m.infilepath))
                {
                    string title = Calculate.GetTitleNum(m.infilepath);
                    if (title != "")
                        title = "_T" + title;
                    s.FileName = m.dvdname;
                }
                s.AddExtension = true;
                s.Title = Languages.Translate("Save script") + ":";
                s.Filter = "AviSynth " + Languages.Translate("files") + "|*.avs";

                if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    StreamWriter sw = new StreamWriter(s.FileName, false, System.Text.Encoding.Default);
                    string[] separator = new string[] { Environment.NewLine };
                    string[] lines = m.script.Split(separator, StringSplitOptions.None);
                    foreach (string line in lines)
                        sw.WriteLine(line);
                    sw.Close();
                }
            }
        }

        private void menu_auto_volume_disabled_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_auto_volume_disabled.IsFocused)
                Settings.AutoVolumeMode = Settings.AutoVolumeModes.Disabled;
        }

        private void menu_auto_volume_onexp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_auto_volume_onexp.IsFocused)
                Settings.AutoVolumeMode = Settings.AutoVolumeModes.OnExport;
        }

        private void menu_auto_volume_onimp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (menu_auto_volume_onimp.IsFocused)
                Settings.AutoVolumeMode = Settings.AutoVolumeModes.OnImport;
        }

        private void check_auto_deint_disabled_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_auto_deint_disabled.IsFocused)
                Settings.AutoDeinterlaceMode = Settings.AutoDeinterlaceModes.Disabled;
        }

        private void check_auto_deint_mpeg_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_auto_deint_mpeg.IsFocused)
                Settings.AutoDeinterlaceMode = Settings.AutoDeinterlaceModes.MPEGs;
        }

        private void check_auto_deint_all_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_auto_deint_all.IsFocused)
                Settings.AutoDeinterlaceMode = Settings.AutoDeinterlaceModes.AllFiles;
        }

        private void LoadVideoPresets()
        {
            string format;
            if (m == null)
                format = Format.EnumToString(Settings.FormatOut);
            else
                format = Format.EnumToString(m.format);

            combo_vencoding.Items.Clear();
            foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\video"))
                combo_vencoding.Items.Add(Path.GetFileNameWithoutExtension(file));
            combo_vencoding.Items.Add("Disabled");
            combo_vencoding.Items.Add("Copy");
        }

        private void SetVideoPresetFromSettings()
        {
            Format.ExportFormats format;
            if (m == null)
                format = Settings.FormatOut;
            else
                format = m.format;

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
            string format;
            if (m == null)
                format = Format.EnumToString(Settings.FormatOut);
            else
                format = Format.EnumToString(m.format);

            combo_aencoding.Items.Clear();
            foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\audio"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                combo_aencoding.Items.Add(name);
            }
            combo_aencoding.Items.Add("Disabled");
            combo_aencoding.Items.Add("Copy");
        }

        private void SetAudioPresetFromSettings()
        {
            Format.ExportFormats format;
            if (m == null)
                format = Settings.FormatOut;
            else
                format = m.format;

            //прописываем текущий пресет кодирования
            if (combo_aencoding.Items.Contains(Settings.GetAEncodingPreset(format)))
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

                        m = Format.GetValidResolution(m);
                        m = Format.GetValidOutAspect(m);
                        m = AspectResolution.FixAspectDifference(m);

                        //принудительный фикс цвета для DVD
                        if (Settings.AutoColorMatrix &&
                            Calculate.IsMPEG(m.infilepath))
                        {
                            if (m.outvcodec == "MPEG2")
                                m.iscolormatrix = false;
                            else
                                m.iscolormatrix = true;
                        }
                        //   if (x.iscolormatrix == false &&
                        //       x.invcodecshort == "MPEG2")
                        //   {
                        //       x.iscolormatrix = true;
                        //        if (combo_sbc.Items.Contains("MPEG2Fix") &&
                        ////            Settings.SBC == "Disabled")
                        ///            combo_sbc.SelectedItem = "MPEG2Fix";
                        //    }

                    }

                    m.outfilesize = Calculate.GetEncodingSize(m);

                    //перезабиваем настройки форматов
                    m.split = Settings.GetFormatPreset(m.format, "split");

                    //создаём новый AviSynth скрипт
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);

                    //проверяем скрипт на ошибки и пытаемся их автоматически исправить
                    string er = Calculate.CheckScriptErrors(m);
                    if (er != null)
                    {
                        if (er == "SSRC: could not resample between the two samplerates.")
                            m.sampleratemodifer = AviSynthScripting.SamplerateModifers.ResampleAudio;

                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
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
                            mess.ShowMessage(Languages.Translate("Stream contein parameter incompatible with format") +
                                " " + Format.EnumToString(m.format) + " - " + CopyProblems + ".", Languages.Translate("Warning"));
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
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);

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
                    if (m == null)
                        format = Settings.FormatOut;
                    else
                        format = m.format;

                    if (format == Format.ExportFormats.Audio &&
                        combo_aencoding.SelectedItem.ToString() == "Disabled")
                    {
                        combo_aencoding.SelectedItem = "MP3 CBR 128k";
                        return;
                    }

                    if (combo_aencoding.SelectedItem.ToString() != "Disabled")
                        Settings.SetAEncodingPreset(format, combo_aencoding.SelectedItem.ToString());

                    if (m != null)
                    {
                        if (m.outaudiostreams.Count == 0 &&
                            combo_aencoding.SelectedItem.ToString() != "Disabled")
                        {
                            combo_aencoding.SelectedItem = "Disabled";
                            LoadVideo(MediaLoad.update);
                            return;
                        }

                        //запрещаем или разрешаем звук
                        if (m.outaudiostreams.Count > 0 &&
                            combo_aencoding.SelectedItem.ToString() == "Disabled")
                        {
                            m.outaudiostreams.Clear();
                        }
                        else
                        {
                            //забиваем-обновляем аудио массивы
                            m = FillAudio(m);
                        }

                        //создаём новый AviSynth скрипт (если в настройках разрешено обновлять скрипт). combo aencoding selection
                        if (Settings.RenewScript == true)
                        {
                            // string script = m.script;
                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                            // if (script != m.script)
                            //     LoadVideo(MediaLoad.update);
                        }

                        m = PresetLoader.DecodePresets(m);

                        //проверка на размер
                        m.outfilesize = Calculate.GetEncodingSize(m);

                        //загружаем обновлённый скрипт
                        UpdateTaskMassive(m);

                        //обновляем дочерние окна
                        ReloadChildWindows();
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

                        if (m != null)
                            m.format = Format.ExportFormats.Audio;
                    }

                    Format.ExportFormats format;
                    if (m == null)
                        format = Settings.FormatOut;
                    else
                        format = m.format;

                    Settings.SetVEncodingPreset(format, combo_vencoding.SelectedItem.ToString());

                    if (m != null)
                    {
                        //забиваем настройки из профиля
                        m.vencoding = combo_vencoding.SelectedItem.ToString();
                        //m.vencoding = Settings.GetVEncodingPreset(Format.EnumToString(m.format));

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

                        //создаём новый AviSynth скрипт (если в настройках разрешено обновлять скрипт). combo vencoding selection
                        if (Settings.RenewScript == true)
                        {
                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        }

                        if (m.outvcodec == "Disabled")
                            m.outvbitrate = 0;

                        //проверка на размер
                        m.outfilesize = Calculate.GetEncodingSize(m);

                        //загружаем обновлённый скрипт
                        //LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);

                        //проверяем можно ли копировать данный формат
                        if (m.vencoding == "Copy")
                        {
                            string CopyProblems = Format.ValidateCopyVideo(m);
                            if (CopyProblems != null)
                            {
                                Message mess = new Message(this);
                                mess.ShowMessage(Languages.Translate("Stream contein parameter incompatible with format") +
                                    " " + Format.EnumToString(m.format) + " - " + CopyProblems + ".", Languages.Translate("Warning"));
                            }
                        }
                    }

                    if (combo_vencoding.SelectedItem.ToString() == "Copy")
                    {
                        combo_filtering.SelectedItem = "Disabled";
                        combo_sbc.SelectedItem = "Disabled";

                        if (m != null)
                        {
                            m.sbc = "Disabled";
                            m.filtering = "Disabled";

                            Settings.SBC = "Disabled";
                            Settings.Filtering = "Disabled";
                        }
                    }

                    //обновляем дочерние окна
                    ReloadChildWindows();
                }
            }
        }

        private void button_edit_filters_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EditScript();
        }


        private void VideoEncodingSetting()
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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

                Format.ExportFormats format;
                if (m == null)
                    format = Settings.FormatOut;
                else
                    format = m.format;

                Settings.SetVEncodingPreset(format, m.vencoding);


                //создаём новый AviSynth скрипт (если в настройках разрешено обновлять скрипт). video encoding settings
                if (Settings.RenewScript == true)
                {
                    //string old_script = m.script; //сохраняем 
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    //m.script = old_script; //восстанавливаем
                }

                //проверка на размер
                //m.outfilesize = Calculate.GetEncodingSize(m);

                UpdateTaskMassive(m);

                if (combo_vencoding.SelectedItem.ToString() == "Copy")
                {
                    combo_filtering.SelectedItem = "Disabled";
                    combo_sbc.SelectedItem = "Disabled";

                    m.sbc = "Disabled";
                    m.filtering = "Disabled";

                    Settings.SBC = "Disabled";
                    Settings.Filtering = "Disabled";
                }
            }
        }

        private void button_edit_vencoding_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            VideoEncodingSetting();
        }

        private void menu_venc_settings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            VideoEncodingSetting();
        }

        private void button_edit_aencoding_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AudioEncodingSetting();
        }

        private void menu_aenc_settings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AudioEncodingSetting();
        }

        private void AudioEncodingSetting()
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.inaudiostreams.Count == 0)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                }
                else
                {
                    //определяем аудио потоки
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                    string oldacodec = outstream.codec;
                    string oldsrate = outstream.samplerate;

                    AudioEncoding enc = new AudioEncoding(m);
                    m = enc.m.Clone();
                    LoadAudioPresets();

                    //защита от удаления профиля
                    if (!combo_aencoding.Items.Contains(outstream.encoding))
                        outstream.encoding = "Copy";

                    combo_aencoding.SelectedItem = outstream.encoding;

                    Format.ExportFormats format;
                    if (m == null)
                        format = Settings.FormatOut;
                    else
                        format = m.format;

                    if (combo_aencoding.SelectedItem.ToString() != "Disabled")
                        Settings.SetAEncodingPreset(format, outstream.encoding);

                    //прописываем правильную частоту
                    if (outstream.codec != "Disabled" &&
                        outstream.codec != "Copy" &&
                        outstream.codec != oldacodec)
                        m = Format.GetValidSamplerate(m);

                    //создаём новый AviSynth скрипт (если в настройках разрешено обновлять скрипт). audio encoding settings
                    if (Settings.RenewScript == true)
                    {
                        // string old_script2 = m.script; //сохраняем 
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        // m.script = old_script2; //восстанавливаем
                    }

                    //проверка на размер
                    //m.outfilesize = Calculate.GetEncodingSize(m);

                    //загружаем обновлённый скрипт
                    if (outstream.codec == "Disabled" ||
                        oldacodec == "Disabled" ||
                        oldsrate != outstream.samplerate)
                        LoadVideo(MediaLoad.update);
                    UpdateTaskMassive(m);
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
                ErrorExeption(ex.Message);
            }
        }

        private void grid_player_window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
        }

        private void grid_player_window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            foreach (string dropfile in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                //открываем файл
                OpenDialogs.owner = this;
                if (Path.GetFileName(dropfile).ToLower() == "x264.exe")
                {
                    File.Copy(dropfile, Calculate.StartupPath + "\\apps\\x264\\x264.exe", true);
                    return;
                }
                else
                {
                    Massive x = new Massive();
                    x.infilepath = dropfile;
                    x.infileslist = new string[] { dropfile };
                    action_open(x, ShowPreview.show);
                    return;
                }
            }
        }

        private void list_tasks_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsInsertAction) return;

            if (list_tasks.SelectedIndex != -1)
            {
                Task task = (Task)list_tasks.Items[list_tasks.SelectedIndex];

                string script = null;
                if (m != null)
                    script = m.script;
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

            //if (m != null)
            //    Title = m.key;
        }

        private void RemoveSelectedTask()
        {
            if (list_tasks.SelectedItem != null)
            {
                Task task = (Task)list_tasks.SelectedItem;

                if (task.Status == "Encoding")
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("Disabled remove tasks in encoding progress!"), Languages.Translate("Error"));
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
            //проверяем не заблокированно ли задание
            bool IsEncoding = false;

            foreach (object _task in list_tasks.Items)
            {
                Task task = (Task)_task;
                if (task.Id == m.key)
                    if (task.Status == "Encoding")
                        IsEncoding = true;
            }

            if (!IsEncoding)
            {
                int task_index = 0;
                bool IsTask = false;
                Task task = null;

                foreach (object _task in list_tasks.Items)
                {
                    task = (Task)_task;
                    if (task.Id == m.key)
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

        public void EncodeNextTask()
        {
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
                        break;
                    }
                }

                if (IsWaiting)
                {
                    UpdateTaskStatus(task.Id, "Encoding");
                    action_encode(task.Mass.Clone());
                }
            }
        }

        private void button_encode_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null || list_tasks.Items.Count > 0)
            {
                bool IsEncoding = false;
                bool IsWaiting = false;
                foreach (object _task in list_tasks.Items)
                {
                    Task task = (Task)_task;
                    if (task.Status == "Encoding")
                        IsEncoding = true;
                    if (task.Status == "Waiting")
                        IsWaiting = true;
                }

                if (IsEncoding && IsWaiting)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("Do you want run one more encoding thread?"), Languages.Translate("Question"), Message.MessageStyle.YesNo);
                    if (mes.result == Message.Result.No)
                        return;
                }

                if (!IsWaiting)
                    action_save(m.Clone());

                EncodeNextTask();
            }
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
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

        private void list_tasks_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //if (list_tasks.SelectedItems.Count != 0)
            //{
            //    list_tasks.SelectedIndex = -1;
            //    m.key = Settings.Key;
            //    OldSelectedIndex = -1;
            //    IsTaskSelection = false;
            //}
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

        private void cmenu_is_always_delete_encoded_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cmenu_is_always_delete_encoded.IsFocused)
                Settings.AutoDeleteTasks = cmenu_is_always_delete_encoded.IsChecked.Value;
        }

        private void cmenu_is_always_delete_encoded_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cmenu_is_always_delete_encoded.IsFocused)
                Settings.AutoDeleteTasks = cmenu_is_always_delete_encoded.IsChecked.Value;
        }

        private void cmenu_is_always_close_encoding_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cmenu_is_always_close_encoding.IsFocused)
                Settings.AutoClose = cmenu_is_always_close_encoding.IsChecked.Value;
        }

        private void cmenu_is_always_close_encoding_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cmenu_is_always_close_encoding.IsFocused)
                Settings.AutoClose = cmenu_is_always_close_encoding.IsChecked.Value;
        }

        private void button_edit_sbc_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaturationBrightness();
        }

        private void menu_saturation_brightness_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaturationBrightness();
        }

        private void SaturationBrightness()
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.outvcodec == "Copy")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                }
                else if (m.format == Format.ExportFormats.Audio)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
                }
                else
                {
                    //разрешаем только одно окно
                    //string stitle = "XviD4PSP - AviSynth-based MultiMedia Converter";
                    //foreach (Window ownedWindow in this.OwnedWindows)
                    //{
                    //    if (ownedWindow.Title != stitle)
                    //        return;
                    //}                        

                    ColorCorrection col = new ColorCorrection(m, this);
                    bool oldcolormatrix = m.iscolormatrix;
                    double oldsaturation = m.saturation;
                    int oldhue = m.hue;
                    int oldbrightness = m.brightness;
                    double oldcontrast = m.contrast;

                    m = col.m.Clone();

                    //загружаем списки профилей цвето коррекции
                    combo_sbc.Items.Clear();
                    combo_sbc.Items.Add("Disabled");
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\sbc"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        combo_sbc.Items.Add(name);
                    }
                    //прописываем текущий профиль
                    if (combo_sbc.Items.Contains(m.sbc))
                        combo_sbc.SelectedItem = m.sbc;
                    else
                        combo_sbc.SelectedItem = "Disabled";

                    Settings.SBC = m.sbc; //сохраняет название текущего профиля в реестре

                    //обновление при необходимости
                    if (oldbrightness != m.brightness ||
                        oldcolormatrix != m.iscolormatrix ||
                        oldcontrast != m.contrast ||
                        oldhue != m.hue ||
                        oldsaturation != m.saturation)
                    {
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);
                    }
                }
            }
        }

        private void menu_directx_update_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start(Calculate.StartupPath + "\\apps\\DirectX_Update\\dxwebsetup.exe");
        }

        private void menu_save_wav_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                if (outstream.codec == "Copy")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                }
                else if (outstream.codec == "Disabled")
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                }
                else if (m.inaudiostreams.Count == 0)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                }
                else
                {
                    action_save_wav();
                }
            }
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
                    ErrorExeption(Languages.Translate("Select other name for output file!"));
                    return;
                }
                Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.DecodeToWAV, o.FileName);
            }
        }

        private void menu_demux_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                if (m.inaudiostreams.Count == 0)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                }
                else
                {
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                    if (instream.codecshort == "PCM" ||
                        instream.codecshort == "LPCM")
                    {
                        action_save_wav();
                    }
                    else
                    {
                        System.Windows.Forms.SaveFileDialog o = new System.Windows.Forms.SaveFileDialog();
                        o.Filter = instream.codecshort + " " + Languages.Translate("files") + "|*." + instream.codecshort.ToLower();
                        o.Title = Languages.Translate("Select output file") + ":";

                        o.FileName = Path.GetFileNameWithoutExtension(m.infilepath) + "." + instream.codecshort.ToLower();

                        if (o.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (o.FileName == m.infilepath)
                            {
                                ErrorExeption(Languages.Translate("Select other name for output file!"));
                                return;
                            }
                            Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, o.FileName);
                        }
                    }
                }
            }
        }

        private void menu_demux_video_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                if (!m.isvideo)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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
                            ErrorExeption(Languages.Translate("Select other name for output file!"));
                            return;
                        }

                        Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractVideo, o.FileName);
                    }
                }
            }
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
        }

        private void menu_info_media_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string filepath = null;
            if (m == null)
            {
                ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
                if (files.Count > 0)
                    filepath = files[0].ToString();
            }
            else
                filepath = m.infilepath;

            if (filepath != null)
            {
                MediaInfo media = new MediaInfo(filepath, MediaInfo.InfoMode.MediaInfo, this);
            }
        }

        private void menu_info_mp4_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string filepath = null;
            if (m == null)
            {
                ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
                if (files.Count > 0)
                    filepath = files[0].ToString();
            }
            else
                filepath = m.infilepath;

            if (filepath != null)
            {
                MediaInfo media = new MediaInfo(filepath, MediaInfo.InfoMode.MP4BOX, this);
            }
        }

        private void menu_info_mkv_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string filepath = null;
            if (m == null)
            {
                ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
                if (files.Count > 0)
                    filepath = files[0].ToString();
            }
            else
                filepath = m.infilepath;

            if (filepath != null)
            {
                MediaInfo media = new MediaInfo(filepath, MediaInfo.InfoMode.MKVINFO, this);
            }
        }

        private void menu_info_ffmpeg_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string filepath = null; ;
            if (m == null)
            {
                ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
                if (files.Count > 0)
                    filepath = files[0].ToString();
            }
            else
                filepath = m.infilepath;

            if (filepath != null)
            {
                MediaInfo media = new MediaInfo(filepath, MediaInfo.InfoMode.FFMPEG, this);
            }
        }

        private void menu_playinwmp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (m == null)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
                }
                else
                {
                    if (!File.Exists(m.scriptpath))
                        AviSynthScripting.WriteScriptToFile(m);

                    Process pr = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    info.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                        "\\Windows Media Player\\wmplayer.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.Arguments = Settings.TempPath + "\\preview.avs";
                    pr.StartInfo = info;
                    pr.Start();
                }
            }
            catch (Exception ex)
            {
                Message mess = new Message(this);
                mess.ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void menu_playinwpf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (m == null)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
                }
                else
                {
                    if (!File.Exists(m.scriptpath))
                        AviSynthScripting.WriteScriptToFile(m);

                    Process pr = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = Calculate.StartupPath +
                        "\\WPF_VideoPlayer.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.Arguments = Settings.TempPath + "\\preview.avs";
                    pr.StartInfo = info;
                    pr.Start();
                }
            }
            catch (Exception ex)
            {
                Message mess = new Message(this);
                mess.ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void menu_payinmpc_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (m == null)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
                }
                else
                {
                    if (!File.Exists(m.scriptpath))
                        AviSynthScripting.WriteScriptToFile(m);

                    Process pr = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                        "\\K-Lite Codec Pack\\Media Player Classic\\mplayerc.exe"))
                        info.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                            "\\K-Lite Codec Pack\\Media Player Classic\\mplayerc.exe"; //mplayerc.exe

                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                       "\\Media Player Classic\\mplayerc.exe"))
                        info.FileName = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                            "\\Media Player Classic\\mplayerc.exe";

                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.Arguments = Settings.TempPath + "\\preview.avs";
                    pr.StartInfo = info;
                    pr.Start();

                }
            }
            catch (Exception ex)
            {
                Message mess = new Message(this);
                mess.ShowMessage(ex.Message, Languages.Translate("Error"));
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
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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
                    Autocrop acrop = new Autocrop(m);
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

            if (this.isAudioOnly)
                return;

            //если не Фуллскрин, то делаем Фуллскрин
            //if (this.WindowStyle != System.Windows.WindowStyle.None)
            if (isFullScreen == false)
            {
                this.grid_tasks.Visibility = Visibility.Collapsed;
                this.grid_menu.Visibility = Visibility.Collapsed;
                this.grid_left_panel.Visibility = Visibility.Collapsed;
                this.splitter_tasks_preview.Visibility = Visibility.Collapsed;
                //  this.grid_player_buttons.Visibility = Visibility.Collapsed; //убрать кнопки плейера
                this.grid_player_info.Visibility = Visibility.Collapsed;
                this.grid_top.Visibility = Visibility.Collapsed;
                //  this.slider_pos.Visibility = Visibility.Collapsed; //убрать панель управления плейера
                this.WindowStyle = System.Windows.WindowStyle.None;  //стиль окна (без стиля)
                this.WindowState = System.Windows.WindowState.Maximized; //размер окна (максимальный)
                this.grid_player_buttons.Margin = new Thickness(0, 0, 0, 0); //Убрать отступы для панели управления плейера

                oldbrush = this.LayoutRoot.Background;
                oldmargin = this.grid_player_window.Margin;
                this.LayoutRoot.Background = Brushes.Black;
                this.isFullScreen = true;

                Grid.SetRow(this.grid_player_window, 0);//
                Grid.SetRowSpan(this.grid_player_window, 2);//
                this.grid_player_window.Margin = new Thickness(0, 0, 0, 0);//





                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                {
                    // this.grid_player_window.Visibility = Visibility.Collapsed;
                    //this.grid_player_window.Margin = new Thickness(0, -250, 0, 0);

                    //  Grid.SetRow(this.grid_player_window, 0);
                    //   Grid.SetRowSpan(this.grid_player_window, 2);
                    //  this.grid_player_window.Margin = new Thickness(0, 0, 0, 0);



                    MoveVideoWindow();
                }
                else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                {
                    //this.grid_player_window.Background = Brushes.Black;
                    //oldmargin = this.grid_player_window.Margin;
                    // Grid.SetRow(this.grid_player_window, 0);
                    // Grid.SetRowSpan(this.grid_player_window, 2);
                    // this.grid_player_window.Margin = new Thickness(0, 0, 0, 0);
                    this.VideoElement.Margin = new Thickness(0, 0, 0, 0);
                }
            }
            else
            {
                //Выход из Фуллскрина
                this.isFullScreen = false;
                // this.grid_player_window.Margin = olddmargine;

                Grid.SetRow(this.grid_player_window, 1);//
                Grid.SetRowSpan(this.grid_player_window, 1);//
                this.grid_player_window.Margin = oldmargin;//

                this.grid_tasks.Visibility = Visibility.Visible;
                this.grid_menu.Visibility = Visibility.Visible;
                this.grid_left_panel.Visibility = Visibility.Visible;
                this.splitter_tasks_preview.Visibility = Visibility.Visible;
                this.grid_player_buttons.Visibility = Visibility.Visible;
                this.grid_player_window.Visibility = Visibility.Visible;
                this.grid_player_info.Visibility = Visibility.Visible;
                this.WindowState = System.Windows.WindowState.Normal; //размер окна (дефолтный)
                this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                this.grid_top.Visibility = Visibility.Visible;
                this.slider_pos.Visibility = Visibility.Visible;
                this.LayoutRoot.Background = oldbrush;
                this.grid_player_buttons.Margin = new Thickness(195.856, 0, 0, 0); //Установить дефолтные отступы для панели управления плейера              


                if (Settings.PlayerEngine == Settings.PlayerEngines.DirectShow)
                {
                    // this.grid_player_window.Margin = olddmargine;

                    //    Grid.SetRow(this.grid_player_window, 1);
                    //    Grid.SetRowSpan(this.grid_player_window, 1);
                    //    this.grid_player_window.Margin = oldmargin;


                    MoveVideoWindow();
                }
                else if (Settings.PlayerEngine == Settings.PlayerEngines.MediaBridge)
                {
                    //   this.grid_player_window.Background = oldbrush;
                    //   Grid.SetRow(this.grid_player_window, 1);
                    //    Grid.SetRowSpan(this.grid_player_window, 1);
                    //    this.grid_player_window.Margin = oldmargin;
                    this.VideoElement.Margin = new Thickness(8, 56, 8, 8);
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

                if (mediaload != MediaLoad.update)
                    this.currentState = PlayState.Stopped;
                this.isAudioOnly = true;
                //this.isFullScreen = false; //Зачем при закрытии клипа выходить из Фуллскрина?

                // Free DirectShow interfaces
                CloseInterfaces();

                // No current media state
                if (mediaload != MediaLoad.update)
                    this.currentState = PlayState.Init;
            }

            if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                VideoElement.Close();
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
                string url = "MediaBridge://MyDataString";
                MediaBridge.MediaBridgeManager.UnregisterCallback(url);
            }

            //update titles
            textbox_name.Text = "";
            textbox_time.Text = "00:00:00";
            textbox_duration.Text = "00:00:00";
            progress_top.Width = 0.0;
            slider_pos.Value = 0.0;
            textbox_frame.Text = "";
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

            this.graphBuilder = (IGraphBuilder)new FilterGraph();

            ///       //хз чего
            //      DirectShowLib.IBaseFilter filter = (DirectShowLib.IBaseFilter)new DirectShowLib.VideoMixingRenderer9();
            //       hr = graphBuilder.AddFilter(filter, "Video Mixing Renderer 9");
            //       DirectShowLib.IVMRMonitorConfig9 vmrMonitorConfig = filter as
            //       DirectShowLib.IVMRMonitorConfig9;

            ///


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
            basicAudio.put_Volume(VolumeSet); //Ввод в ДиректШоу значения VolumeSet для установки громкости

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
                    DirectShowLib.WindowStyle.ClipSiblings | DirectShowLib.WindowStyle.ClipChildren);
                DsError.ThrowExceptionForHR(hr);


                MoveVideoWindow();

                GetFrameStepInterface();
            }

            // Complete window initialization
            //this.isFullScreen = false; //PlayMovieInWindow - зачем тут выход из Фуллскрин?

#if DEBUG
            rot = new DsROTEntry(this.graphBuilder);
#endif

            this.Focus();

            // Run the graph to play the media file
            hr = this.mediaControl.Run();
            DsError.ThrowExceptionForHR(hr);

            if (mediaload != MediaLoad.update)
                this.currentState = PlayState.Running;
            SetPauseIcon();

        }

        private void MoveVideoWindow()
        {
            int hr = 0;

            // Track the movement of the container window and resize as needed
            if (this.videoWindow != null)
            {
                int top = (int)(grid_menu.ActualHeight +
                    grid_top.ActualHeight +
                    splitter_tasks_preview.ActualHeight +
                    grid_tasks.ActualHeight +
                    grid_player_info.ActualHeight + 10);
                int h = (int)(grid_player_window.ActualHeight -
                    grid_player_info.ActualHeight - 12);
                int w = (int)(m.outaspect * h);
                int left = (int)((grid_left_panel_paper.ActualWidth + (this.grid_player_window.ActualWidth - w) / 2));

                if (w > (int)progress_back.ActualWidth)
                {
                    w = (int)progress_back.ActualWidth;
                    h = (int)((double)w / m.outaspect);
                    left = (int)((grid_left_panel_paper.ActualWidth + (this.grid_player_window.ActualWidth - w) / 2));
                    top += (int)((this.grid_player_window.ActualHeight - h) / 2.0) - (int)(grid_player_info.ActualHeight) + 14;
                }

                //Пересчет размера и положения видео-окна для ДиректШоу, если используется Фуллскрин
                if (isFullScreen == true)
                {
                    top = 0; //h - высота, w - ширина
                    //this.grid_player_buttons.ActualHeight //высота панели управления плейера

                    h = (int)this.LayoutRoot.ActualHeight - (int)this.grid_player_buttons.ActualHeight; //высота экрана минус высота панели
                    ///h = (int)SystemParameters.WorkArea.Height;
                    w = (int)(m.outaspect * h);
                    left = (int)((this.LayoutRoot.ActualWidth - w) / 2); //ширина
                    ///left = (int)((SystemParameters.WorkArea.Width - w) / 2);

                    if (w > this.LayoutRoot.ActualWidth)
                    ///if (w > SystemParameters.WorkArea.Width)
                    {
                        w = (int)this.LayoutRoot.ActualWidth;
                        ///w = (int)SystemParameters.WorkArea.Width;
                        h = (int)((double)w / m.outaspect);
                        left = 0;
                        top = (int)((this.LayoutRoot.ActualHeight - h) / 2.0);
                        ///top = (int)((SystemParameters.WorkArea.Height - h) / 2.0);
                    }
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

                    if (mediaload != MediaLoad.update)
                        this.currentState = PlayState.Stopped;

                    // Seek to the beginning
                    hr = this.mediaSeeking.SetPositions(pos, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);

                    // Display the first frame to indicate the reset condition
                    hr = this.mediaControl.Pause();
                    SetPlayIcon();
                }
            }

            if (this.VideoElement.Source != null)
            {
                VideoElement.Stop();
                if (mediaload != MediaLoad.update)
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

                if (evCode == EventCode.ClockChanged)
                {
                    slider_pos.Maximum = NaturalDuration.TotalSeconds;

                    if (mediaload == MediaLoad.update)
                    {
                        if (currentState == PlayState.Paused)
                        {
                            currentState = PlayState.Running;
                            PauseClip();
                        }
                        if (!isDelayUpdate)
                            Position = oldpos;
                        else
                            isDelayUpdate = false;
                    }

                    if (mediaload == MediaLoad.load)
                    {
                        if (Settings.AfterImportAction == Settings.AfterImportActions.Nothing)
                            PauseClip();
                        else if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                        {
                            PauseClip();
                            Position = TimeSpan.FromSeconds(NaturalDuration.TotalSeconds / 2.0);
                        }
                    }
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


                    //Общая продолжительность клипа (для ДиректШоу)
                    TimeSpan tCode2 = TimeSpan.Parse(TimeSpan.FromSeconds(NaturalDuration.TotalSeconds).ToString().Split('.')[0]);
                    textbox_duration.Text = tCode2.ToString();

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
            if (VideoElement.HasVideo ||
                VideoElement.HasAudio)
            {
                slider_pos.Maximum = VideoElement.NaturalDuration.TimeSpan.TotalSeconds;
                //  textbox_duration.Text = Convert.ToString(VideoElement.NaturalDuration.TimeSpan.TotalSeconds);//Общее время для МедиаБридж
                if (mediaload == MediaLoad.update)
                {
                    if (currentState == PlayState.Paused)
                    {
                        currentState = PlayState.Running;
                        PauseClip();
                    }
                    if (!isDelayUpdate)
                        Position = oldpos;
                    else
                        isDelayUpdate = false;
                }

                if (mediaload == MediaLoad.load)
                {
                    if (Settings.AfterImportAction == Settings.AfterImportActions.Nothing)
                        PauseClip();
                    else if (Settings.AfterImportAction == Settings.AfterImportActions.Middle)
                    {
                        PauseClip();
                        Position = TimeSpan.FromSeconds(NaturalDuration.TotalSeconds / 2.0);
                    }
                }
            }
        }

        private void VideoElement_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            StopClip();
        }

        private void VisualTarget_Rendering(object sender, EventArgs e)
        {
            if (VideoElement.NaturalDuration.HasTimeSpan && this.VideoElement.Source != null)
            {
                progress_top.Width = (VideoElement.Position.TotalSeconds / VideoElement.NaturalDuration.TimeSpan.TotalSeconds) * progress_back.ActualWidth;
                TimeSpan tCode = TimeSpan.Parse(VideoElement.Position.ToString().Split('.')[0]);
                textbox_time.Text = tCode.ToString();

                //Общая продолжительность клипа (для МедиаБридж)
                TimeSpan tCode2 = TimeSpan.Parse(VideoElement.NaturalDuration.ToString().Split('.')[0]);
                textbox_duration.Text = tCode2.ToString();

                //кол-во кадров для МедиаБридж (тут - потому-что в LoadVideo получаем ноль, а в slider_pos_ValueChanged нет смысла пересчитывать это каждый раз когда сдвигается ползунок, т.к.
                //для ДиректШоу достаточно высчитать один раз - в LoadVideo, и потом брать готовое значение, а для МедиаБридж надо вычислять, иначе останется ноль.. 
                total_frames = Convert.ToString((int)(NaturalDuration.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate)));

                Visual visual = Mouse.Captured as Visual;
                if (visual == null)
                {
                    slider_pos.Value = VideoElement.Position.TotalSeconds;
                }
            }
        }

        private void VideoElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 &&
                e.ChangedButton == MouseButton.Left &&
                VideoElement.Source != null)
            {
                PauseClip();
            }
            else if (e.ClickCount == 2 &&
                e.ChangedButton == MouseButton.Left &&
                VideoElement.Source != null)
            {
                PauseClip();
                SwitchToFullScreen();
            }
        }

        private void PlayWithMediaBridge(string filepath)
        {
            this.filepath = filepath;
            string url = "MediaBridge://MyDataString";
            MediaBridge.MediaBridgeManager.RegisterCallback(url, BridgeCallback);

            VideoElement.Source = new Uri(url);
            VideoElement.Play();

            //принудительно ставит на паузу при выборе MediaBridge, если уже не пауза
            if ((this.currentState != PlayState.Paused))
            {
                PauseClip();
            }

            if (mediaload != MediaLoad.update)
                this.currentState = PlayState.Running;
            SetPauseIcon();
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
                ///Find WPF renderer.  It's always named the same thing
                //hr = graphBuilder.FindFilterByName("Avalon EVR", out videoRenderer);//
                //hr = graphBuilder.FindFilterByName("Video Renderer", out videoRenderer);//
                hr = graphBuilder.FindFilterByName("Enhanced Video Renderer", out videoRenderer);
                DsError.ThrowExceptionForHR(hr);

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

        private void menu_dvd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            action_open_dvd();
        }

        private void menu_detect_interlace_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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
            InterlaceWindow();
        }

        private void InterlaceWindow()
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (m.format == Format.ExportFormats.Audio)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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
                    //разрешаем только одно окно
                    //string stitle = "XviD4PSP - AviSynth-based MultiMedia Converter";
                    //foreach (Window ownedWindow in this.OwnedWindows)
                    //{
                    //    if (ownedWindow.Title != stitle)
                    //        return;
                    //}               

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

        private void menu_support_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://forum.winnydows.com");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
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
                ErrorExeption(ex.Message);
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
            if (m == null)
                return;

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

        private void menu_tsmuxer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\tsMuxeR\\tsMuxerGUI.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void button_configure_Click(object sender, RoutedEventArgs e)
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

            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
            {
                //if (m.outacodec == "Copy")
                //{
                //    Message mess = new Message(this);
                //    mess.ShowMessage(Languages.Translate("Can`t change parameters in COPY mode!"), Languages.Translate("Error"));
                //}
                //else if (m.outacodec == "Disabled")
                //{
                //    Message mess = new Message(this);
                //    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                //}
                //else if (m.inaudiostreams == 0)
                //{
                //    Message mess = new Message(this);
                //    mess.ShowMessage(Languages.Translate("File don`t have audio streams!"), Languages.Translate("Error"));
                //}
                //else
                //{
                AudioOptions ao = new AudioOptions(m, this, AudioOptions.AudioOptionsModes.AllOptions);
                //}
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
                stream.delay = instream.delay;
                mass.outaudiostreams.Add(stream);
                //}

                AudioStream outstream = (AudioStream)mass.outaudiostreams[mass.outaudiostream];

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

                if (outstream.codec == "Disabled")
                    outstream.bitrate = 0;

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
                        mess.ShowMessage(Languages.Translate("Stream contein parameter incompatible with format") +
                            " " + Format.EnumToString(mass.format) + " - " + CopyProblems + ".", Languages.Translate("Warning"));
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

        private void button_dvd_Click(object sender, RoutedEventArgs e)
        {
            action_open_dvd();
        }

        private void action_open_dvd()
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
                Settings.DVDPath = folder.SelectedPath;
                Massive x = new Massive();
                x.owner = this;
                DVDImport dvd = new DVDImport(x, folder.SelectedPath);

                if (dvd.m != null)
                    action_open(dvd.m, ShowPreview.show);
            }
        }

        private void menu_save_frame_Click(object sender, RoutedEventArgs e)
        {
            if (m != null)
            {
                try
                {
                    //AviFile.AviManager aviManager = new AviFile.AviManager(m.infilepath, true);
                    //AviFile.VideoStream stream = aviManager.GetVideoStream();
                    //MessageBox.Show(stream.FrameRate.ToString());
                    //aviManager.Close();

                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.AddExtension = true;
                    //s.SupportMultiDottedExtensions = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    //s.DefaultExt = ".png";

                    s.Filter = "PNG " + Languages.Translate("files") + "|*.png" +
                        "|JPEG " + Languages.Translate("files") + "|*.jpg";

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string ext = Path.GetExtension(s.FileName).ToLower();

                        //if (!s.DefaultExt.StartsWith("."))
                        //    s.DefaultExt = "." + s.DefaultExt;

                        //if (ext != s.DefaultExt)
                        //    s.FileName += s.DefaultExt;

                        int frame = (int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate));

                        if (ext == ".png")
                        {
                            AviSynthReader reader = new AviSynthReader();
                            reader.ParseScript(m.script);

                            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                            bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Png);

                            //завершение
                            bmp.Dispose();
                            reader.Close();
                        }

                        if (ext == ".jpg")
                        {
                            AviSynthReader reader = new AviSynthReader();
                            reader.ParseScript(m.script);

                            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                            bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);

                            //завершение
                            bmp.Dispose();
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
            }
            else if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
            }
        }

        private void menu_savethm_Click(object sender, RoutedEventArgs e)
        {
            if (m != null)
            {
                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.AddExtension = true;
                    //s.SupportMultiDottedExtensions = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    //s.DefaultExt = ".png";

                    if (m.format == Format.ExportFormats.Mp4PSPAVC ||
                        m.format == Format.ExportFormats.Mp4PSPAVC ||
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

                        //if (!s.DefaultExt.StartsWith("."))
                        //    s.DefaultExt = "." + s.DefaultExt;

                        //if (ext != s.DefaultExt)
                        //    s.FileName += s.DefaultExt;

                        int frame = (int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate));

                        if (ext == ".png")
                        {
                            AviSynthReader reader = new AviSynthReader();
                            reader.ParseScript(m.script);

                            if (m.format == Format.ExportFormats.PmpAvc)
                            {
                                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(144, 80);
                                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                                g.DrawImage(reader.ReadFrameBitmap(frame), 0, 0, 144, 80);
                                bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Png);

                                //завершение
                                g.Dispose();
                                bmp.Dispose();
                                reader.Close();
                            }
                            else
                            {
                                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                                bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Png);

                                //завершение
                                bmp.Dispose();
                                reader.Close();
                            }
                        }

                        if (ext == ".jpg")
                        {
                            AviSynthReader reader = new AviSynthReader();
                            reader.ParseScript(m.script);

                            if (m.format == Format.ExportFormats.Mp4PSPAVC ||
                                m.format == Format.ExportFormats.Mp4PSPAVC ||
                                m.format == Format.ExportFormats.Mp4PSPASP)
                            {
                                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(160, 120);
                                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                                g.DrawImage(reader.ReadFrameBitmap(frame), 0, 0, 160, 120);
                                bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);

                                //завершение
                                g.Dispose();
                                bmp.Dispose();
                                reader.Close();
                            }
                            else
                            {
                                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                                bmp.Save(s.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);

                                //завершение
                                bmp.Dispose();
                                reader.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
            }
            else if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else if (!m.isvideo)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("File don`t have video streams!"), Languages.Translate("Error"));
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

        private void menu_mkvmerge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\MKVtoolnix\\mmg.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_mkvextract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\MKVtoolnix\\MKVextractGUI.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_avimux_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\AVI-Mux\\AVIMux_GUI.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void menu_dgpulldown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\DGPulldown\\DGPulldown.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
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
                ErrorExeption(ex.Message);
            }
        }

        private void button_edit_format_Click(object sender, RoutedEventArgs e)
        {
            if (m == null)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Open file before do something!"), Languages.Translate("Error"));
            }
            else
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
                    FormatSettings cust = new FormatSettings(m, this);
                    if (cust.fstream != null)
                    {
                        LoadVideoPresets();
                        LoadAudioPresets();
                        //забиваем-обновляем аудио массивы
                        m = FillAudio(m);
                        //забиваем настройки из профиля
                        m.vencoding = Settings.GetVEncodingPreset(m.format);
                        if (!combo_vencoding.Items.Contains(m.vencoding))
                            m.vencoding = "Copy";
                        m.outvcodec = PresetLoader.GetVCodec(m);
                        m.vpasses = PresetLoader.GetVCodecPasses(m);
                        m = PresetLoader.DecodePresets(m);
                        //перезабиваем специфику формата
                        m = Format.GetOutInterlace(m);
                        m = Format.GetValidFramerate(m);
                        m = Calculate.UpdateOutFrames(m);
                        m = Format.GetValidResolution(m);
                        m = Format.GetValidOutAspect(m);
                        m = AspectResolution.FixAspectDifference(m);
                        //принудительный фикс цвета для DVD
                        if (Settings.AutoColorMatrix &&
                            Calculate.IsMPEG(m.infilepath))
                        {
                            if (m.outvcodec == "MPEG2")
                                m.iscolormatrix = false;
                            else
                                m.iscolormatrix = true;
                        }
                        m.outfilesize = Calculate.GetEncodingSize(m);
                        //перезабиваем настройки форматов
                        m.split = Settings.GetFormatPreset(m.format, "split");
                        //создаём новый AviSynth скрипт
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        //проверяем скрипт на ошибки и пытаемся их автоматически исправить
                        string er = Calculate.CheckScriptErrors(m);
                        if (er != null)
                        {
                            if (er == "SSRC: could not resample between the two samplerates.")
                                m.sampleratemodifer = AviSynthScripting.SamplerateModifers.ResampleAudio;

                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        }
                        //загружаем обновлённый скрипт
                        LoadVideo(MediaLoad.update);
                        UpdateTaskMassive(m);

                        //проверяем можно ли копировать данный формат
                        //if (m.vencoding == "Copy")
                        //{
                        //    string CopyProblems = Format.ValidateCopyVideo(m);
                        //    if (CopyProblems != null)
                        //    {
                        //        Message mess = new Message(this);
                        //        mess.ShowMessage(Languages.Translate("Stream contein parameter incompatible with format") +
                        //            " " + Format.EnumToString(m.format) + " - " + CopyProblems + ".", Languages.Translate("Warning"));
                        //    }
                        //}
                        SetVideoPresetFromSettings();
                        SetAudioPresetFromSettings();
                    }
                    //m = cust.m.Clone();
                    //UpdateTaskMassive(m);  
                }
                else
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("This format don`t have any settings."), Languages.Translate("Format"));
                }
            }
        }

        private void menu_settings_Click(object sender, RoutedEventArgs e)
        {
            Settings_Window sett = new Settings_Window(this);
        }

        //Прибавление громкости кнопками
        public void VolumePlus()
        {
            if (slider_Volume.Value <= 0.95)
            {
                slider_Volume.Value = slider_Volume.Value + 0.05; //Увеличиваем громкость для МедиаБридж на 0,05
            }

        }

        //Уменьшение громкости кнопками
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
            //            if (slider_Volume.IsFocused)

            // VolumeSet = Convert.ToInt32(-(10000 - Math.Sqrt(slider_Volume.Value) * 10000)); //Пересчитывание громкости для ДиректШоу  
            VolumeSet = Convert.ToInt32(-(10000 - Math.Pow(slider_Volume.Value, 1.0 / 3) * 10000)); //Пересчитывание громкости для ДиректШоу (корень кубический)
            if (this.graphBuilder != null && Settings.PlayerEngine == Settings.PlayerEngines.DirectShow) //Если ДиректШоу и Граф не пуст..
            {
                basicAudio.put_Volume(VolumeSet); //..то задаем громкость для ДиректШоу
            }

            //Иконка регулятора громкости
            if (slider_Volume.Value <= 0.0)
            {
                //Устанавливаем картинку для Громкость=0 
                BitmapImage Vl2 = new BitmapImage();
                Vl2.BeginInit();
                Vl2.UriSource = new Uri(@"../pictures/Volume_Disable.png", UriKind.RelativeOrAbsolute);
                Vl2.EndInit();
                image_volume.Source = Vl2; //Выводим картинку Громкость=0    
            }
            else
            {
                //Устанавливаем картинку для Громкость>0 
                BitmapImage Vl1 = new BitmapImage();
                Vl1.BeginInit();
                Vl1.UriSource = new Uri(@"../pictures/Volume1.png", UriKind.RelativeOrAbsolute);
                Vl1.EndInit();
                image_volume.Source = Vl1; //Выводим картинку Громкость>0  
            }
            //Запись значения громкости в реестр
            Settings.VolumeLevel = slider_Volume.Value;

        }


        //Обработка двойного щелчка мыши для ДиректШоу
        private void Direct_Show_Mouse_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 &&
                e.ChangedButton == MouseButton.Left && this.graphBuilder != null)
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

        private void mnDGAVCIndex_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\DGAVCDec\\DGAVCIndex.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }

        }

        private void button_set_start_Click(object sender, RoutedEventArgs e)
        {
            if (m != null && Convert.ToString(button_apply_trim.Content) != Languages.Translate("Remove Trim"))
            {
                if (Convert.ToString(button_set_start.Content) != Languages.Translate("Clear"))
                {
                    if (textbox_start.Text == "")
                    {
                        trim_start = (int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate));
                        textbox_start.Text = Convert.ToString(trim_start);
                    }
                    else
                    {
                        trim_start = Convert.ToInt32(textbox_start.Text);
                    }
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
            if (m != null && Convert.ToString(button_apply_trim.Content) != Languages.Translate("Remove Trim"))
            {
                if (Convert.ToString(button_set_end.Content) != Languages.Translate("Clear"))
                {
                    if (textbox_end.Text == "")
                    {
                        trim_end = (int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate));
                        textbox_end.Text = Convert.ToString(trim_end);
                    }
                    else
                    {
                        trim_end = Convert.ToInt32(textbox_end.Text);
                    }
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

            if (m != null)
            {
                if (Convert.ToString(button_apply_trim.Content) != Languages.Translate("Remove Trim") && (trim_start != trim_end))
                {
                    m.trim_start = trim_start;
                    m.trim_end = trim_end;

                    //Обновляем скрипт
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    LoadVideo(MediaLoad.update);

                    //пересчет кол-ва кадров в видео, его продолжительности и размера получаемого файла
                    AviSynthReader reader = new AviSynthReader();
                    reader.ParseScript(m.script);
                    m.outframes = reader.FrameCount;
                    reader.Close();
                    reader = null;
                    m.outduration = TimeSpan.FromSeconds((double)m.outframes / Calculate.ConvertStringToDouble(m.outframerate));
                    m.outfilesize = Calculate.GetEncodingSize(m);

                    UpdateTaskMassive(m);

                    button_apply_trim.Content = Languages.Translate("Remove Trim");
                    textbox_start.IsReadOnly = true;
                    textbox_end.IsReadOnly = true;
                    return;
                }
                if (Convert.ToString(button_apply_trim.Content) == Languages.Translate("Remove Trim"))
                {
                    if (Convert.ToString(button_set_start.Content) != Languages.Translate("Clear")) textbox_start.IsReadOnly = false;
                    if (Convert.ToString(button_set_end.Content) != Languages.Translate("Clear")) textbox_end.IsReadOnly = false;
                    m.trim_start = 0;
                    m.trim_end = 0;

                    //Обновляем скрипт
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    LoadVideo(MediaLoad.update);

                    //пересчет кол-ва кадров в видео, его продолжительности и размера получаемого файла
                    AviSynthReader reader = new AviSynthReader();
                    reader.ParseScript(m.script);
                    m.outframes = reader.FrameCount;
                    reader.Close();
                    reader = null;
                    m.outduration = TimeSpan.FromSeconds((double)m.outframes / Calculate.ConvertStringToDouble(m.outframerate));
                    m.outfilesize = Calculate.GetEncodingSize(m);

                    UpdateTaskMassive(m);

                    button_apply_trim.Content = Languages.Translate("Apply Trim");

                }

                //Convert.ToString((int)(Position.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate))) + "/" +
                //Convert.ToString((int)(NaturalDuration.TotalSeconds * Calculate.ConvertStringToDouble(m.outframerate)));
            }

        }

        private void mn_ffmpeg_old_Checked(object sender, RoutedEventArgs e)
        {
            if (mn_ffmpeg_old.IsFocused)
                Settings.FFmpegSource2 = false;
        }

        private void mn_ffmpeg_new_Checked(object sender, RoutedEventArgs e)
        {
            if (mn_ffmpeg_new.IsFocused)
                Settings.FFmpegSource2 = true;
        }

        private void menu_Yamb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Calculate.StartupPath + "\\apps\\MP4Box\\Yamb.exe");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }

        }

        //Открытие папки (пакетная обработка)
        private void menu_open_folder_Click(object sender, RoutedEventArgs e)
        {
            CloseChildWindows();
            
            //Папка с исходниками
            System.Windows.Forms.FolderBrowserDialog open_folder = new System.Windows.Forms.FolderBrowserDialog();
            open_folder.Description = Languages.Translate("Select folder where input files is located:");
            open_folder.ShowNewFolderButton = false;
            if(path_to_open != null)
                open_folder.SelectedPath = path_to_open;
            if (open_folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                path_to_open = open_folder.SelectedPath;
            else
                return;

            //Папка для перекодированного
            System.Windows.Forms.FolderBrowserDialog save_folder = new System.Windows.Forms.FolderBrowserDialog();
            save_folder.Description = Languages.Translate("Select folder for the encoded files:");
            save_folder.ShowNewFolderButton = true;
            save_folder.SelectedPath = path_to_open;
            if (save_folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                path_to_save = save_folder.SelectedPath;
            else
                return;
           
            this.Height = this.Window.Height + 1; //чтоб убрать остатки от окна выбора директории, вот такой вот способ...
            this.Height = this.Window.Height - 1;

            int files_in_folder = Directory.GetFiles(path_to_open).Length; //Кол-во файлов в папке
            opened_files = 0; //Обнуляем счетчик успешно открытых файлов

            //Делим строку с валидными расширениями на отдельные строчки
            string[] separator = new string[] { "/" };
            string[] goodexts = Settings.GoodFilesExtensions.Split(separator, StringSplitOptions.None);
            
            foreach (string files in Directory.GetFiles(path_to_open, "*"))
            {
                string ext = Path.GetExtension(files).ToLower().Replace(".", "");
                
                //Сравниваем расширение текущего файла со всеми строчками с валидными расширениями, и открываем файл при совпадении
                foreach (string goodext in goodexts)
                {
                    if (goodext == ext)
                    {
                        Massive x = new Massive();
                        x.infilepath = files;
                        x.infileslist = new string[] { files };
                        action_open(x, ShowPreview.dntshow);
                        action_auto_save(m.Clone());
                        break;
                    }
                }
            }
            if (m != null && opened_files >= 1) //Если массив не пуст, и если кол-во открытых файлов больше нуля (чтоб не обновлять превью, если ни одного нового файла не открылось)
                LoadVideo(MediaLoad.load);

            if (Settings.AutoBatchEncoding)
                EncodeNextTask(); //Запускаем кодирование

            Message mess = new Message(this);
            mess.ShowMessage(Convert.ToString(files_in_folder) + " - " + Languages.Translate("total files in folder") + Environment.NewLine + Convert.ToString(opened_files) + " - " + Languages.Translate("successfully opened files")
                 + Environment.NewLine + Convert.ToString(outfiles.Count) + " - " + Languages.Translate("total tasks in queue"), Languages.Translate("Complete"));          
        }

        private void action_auto_save(Massive mass)
        {
            if (mass != null)
            {               
                //Разбираемся с названием для перекодированного файла
                mass.outfilepath = path_to_save + "\\" + Path.GetFileNameWithoutExtension(m.infilepath) + Format.GetValidExtension(m);
                if (File.Exists(mass.outfilepath))
                    mass.outfilepath = path_to_save + "\\(" + Path.GetFileName(m.infilepath) + ") " + Path.GetFileNameWithoutExtension(m.infilepath) + Format.GetValidExtension(m);
                
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
                if (Directory.GetFiles(Settings.TempPath).Length != 0)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("Selected temp folder is not empty") + " (" + Settings.TempPath.ToString() + ")." + Environment.NewLine + Languages.Translate("You must delete all unnecessary files before start encoding.")
                        + Environment.NewLine + Environment.NewLine + Languages.Translate("OK - open folder to view files, Cancel - ignore this message."), Languages.Translate("Temp folder not empty"), Message.MessageStyle.OkCancel);
                    if (mess.result == Message.Result.Ok)
                        System.Diagnostics.Process.Start("explorer.exe", Settings.TempPath);
                }
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void check_Force_Film_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.DGForceFilm = check_force_film.IsChecked.Value;
        }



    }
}