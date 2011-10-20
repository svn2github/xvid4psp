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
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Collections;

namespace XviD4PSP
{
    public partial class FFRebuilder
    {
        private ManualResetEvent locker = new ManualResetEvent(true);
        private static object lock_pr = new object();
        private static object lock_ff = new object();
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        private bool IsErrors = false;
        private bool IsPaused = false;
        private bool IsAborted = false;
        private FFInfo ff = null;

        //Таскбар
        private MainWindow p;
        private IntPtr ActiveHandle = IntPtr.Zero;
        private int Finished = -1; //0 - OK, 1 - Error

        private enum acodecs { COPY, PCM, FLAC, DISABLED }
        private enum vcodecs { COPY, FFV1, FFVHUFF, UNCOMPRESSED, DISABLED }
        private enum formats { AUTO, AVI, DV, MP4, M4V, MOV, MKV, WEBM, MPG, TS, FLV, OGG, WMV, H264, VC1, AC3, FLAC, WAV, M4A, AAC, MKA, MP3, MP2, WMA, DTS, TRUEHD }
        private enum colorspace { AUTO, YV12, YUY2, RGB24, RGB32 };

        private string infile;
        private string outfile;
        private vcodecs vcodec = vcodecs.COPY;
        private acodecs acodec = acodecs.COPY;
        private string format = "AUTO";
        private colorspace color = colorspace.YV12;
        private string aspect = "AUTO";
        private string framerate = "AUTO";
        private string bits = "S16LE";
        private string command_line = null;
        private string profile = Settings.FFRebuilder_Profile;
        private string last_search = "";
        private int last_search_index = 0;
        private ArrayList vtracks = new ArrayList();
        private ArrayList atracks = new ArrayList();
        private int atrack = 0;
        private string srate = "AUTO";
        private string channels = "AUTO";

        public FFRebuilder(MainWindow owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.p = owner;

            //переводим
            button_cancel.Content = Languages.Translate("Cancel");
            button_start.Content = Languages.Translate("Start");
            tab_main.Header = Languages.Translate("Main");
            tab_log.Header = Languages.Translate("Log");
            tab_help.Header = Languages.Translate("Help");
            label_infile.Content = Languages.Translate("Input file path:");
            label_outfile.Content = Languages.Translate("Output file path:");
            group_options.Header = Languages.Translate("Options");
            group_files.Header = Languages.Translate("Files");
            group_info.Header = Languages.Translate("Info");
            label_acodec.Content = Languages.Translate("Audio codec") + ":";
            label_vcodec.Content = Languages.Translate("Video codec") + ":";
            label_format.Content = Languages.Translate("Format") + ":";
            button_play.Content = Languages.Translate("Play");
            button_add_profile.ToolTip = Languages.Translate("Add profile");
            button_remove_profile.ToolTip = Languages.Translate("Remove profile");
            button_store_profile.ToolTip = Languages.Translate("Save changes");
            textbox_search.Text = (button_search.Content = Languages.Translate("Search")) + "...";
            button_open.ToolTip = Languages.Translate("Open");
            button_save.ToolTip = Languages.Translate("Save");
            progress.Maximum = 100;

            //Format
            //Enum formats используется только для заполнения комбобокса, т.к. пользователь
            //может добавить свои форматы, но Enum уже никак не изменить.
            //combo_format.ToolTip = Languages.Translate("Format");
            combo_format.Items.Add("");
            foreach (string _format in Enum.GetNames(typeof(formats)))
                combo_format.Items.Add(_format);

            //Vcodec
            foreach (vcodecs _vcodec in Enum.GetValues(typeof(vcodecs)))
                combo_vcodec.Items.Add(_vcodec);

            //Colorspace
            combo_colorspace.ToolTip = "Color space";
            foreach (colorspace _color in Enum.GetValues(typeof(colorspace)))
                combo_colorspace.Items.Add(_color);

            //AR
            combo_aspect.ToolTip = Languages.Translate("Aspect");
            combo_aspect.Items.Add("AUTO");
            combo_aspect.Items.Add("1:1");
            combo_aspect.Items.Add("4:3");
            combo_aspect.Items.Add("1.66");
            combo_aspect.Items.Add("16:9");
            combo_aspect.Items.Add("1.85");
            combo_aspect.Items.Add("2.00");
            combo_aspect.Items.Add("2.21");
            combo_aspect.Items.Add("2.35");

            //FPS
            combo_framerate.ToolTip = Languages.Translate("Framerate");
            combo_framerate.Items.Add("AUTO");
            combo_framerate.Items.Add("15.000");
            combo_framerate.Items.Add("18.000");
            combo_framerate.Items.Add("20.000");
            combo_framerate.Items.Add("23.976");
            combo_framerate.Items.Add("23.980");
            combo_framerate.Items.Add("24.000");
            combo_framerate.Items.Add("25.000");
            combo_framerate.Items.Add("29.970");
            combo_framerate.Items.Add("30.000");
            combo_framerate.Items.Add("50.000");
            combo_framerate.Items.Add("59.940");
            combo_framerate.Items.Add("60.000");

            //Acodec
            foreach (acodecs _acodec in Enum.GetValues(typeof(acodecs)))
                combo_acodec.Items.Add(_acodec);

            //PCM Bits
            combo_bits.Items.Add("S8");
            combo_bits.Items.Add("S16BE");
            combo_bits.Items.Add("S16LE");
            combo_bits.Items.Add("S24BE");
            combo_bits.Items.Add("S24LE");
            combo_bits.Items.Add("S32BE");
            combo_bits.Items.Add("S32LE");
            combo_bits.Items.Add("U8");
            combo_bits.Items.Add("U16BE");
            combo_bits.Items.Add("U16LE");
            combo_bits.Items.Add("U24BE");
            combo_bits.Items.Add("U24LE");
            combo_bits.Items.Add("U32BE");
            combo_bits.Items.Add("U32LE");

            //Аудио трек
            combo_atrack.Items.Add(new ComboBoxItem() { Content = "AUTO", ToolTip = Languages.Translate("Select audio track") });
            combo_atrack.SelectedIndex = 0;

            //Дискретизация
            combo_srate.ToolTip = Languages.Translate("Samplerate");
            combo_srate.Items.Add("AUTO");
            combo_srate.Items.Add("22050");
            combo_srate.Items.Add("32000");
            combo_srate.Items.Add("44100");
            combo_srate.Items.Add("48000");
            combo_srate.Items.Add("96000");

            //Кол-во каналов
            combo_channels.ToolTip = Languages.Translate("Channels");
            combo_channels.Items.Add("AUTO");
            combo_channels.Items.Add("1");
            combo_channels.Items.Add("2");
            combo_channels.Items.Add("3");
            combo_channels.Items.Add("4");
            combo_channels.Items.Add("5");
            combo_channels.Items.Add("6");
            combo_channels.Items.Add("7");
            combo_channels.Items.Add("8");

            //Help
            combo_help.Items.Add("-L");
            combo_help.Items.Add("-help");
            combo_help.Items.Add("-version");
            combo_help.Items.Add("-formats");
            combo_help.Items.Add("-codecs");
            combo_help.Items.Add("-filters");
            combo_help.SelectedIndex = 1;

            LoadAllProfiles(); //Список профилей
            LoadFromProfile(); //Настройки из профиля
            UpdateCombosIsEnabled();

            Show();

            this.ActiveHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        }

        void FFRebuilder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!Win7Taskbar.IsInitialized) return;
            if (this.IsVisible)
            {
                //Окно FFRebuilder`а развернулось, переключаем вывод прогресса на него
                ActiveHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                new Thread(new ThreadStart(this.SetTaskbarStatus)).Start();
            }
            else
            {
                //Окно FFRebuilder`а свернулось, переключаем вывод прогресса в MainWindow
                ActiveHandle = p.Handle;
                if (IsPaused) new Thread(new ThreadStart(this.SetTaskbarStatus)).Start();
            }
        }

        internal delegate void SetTaskbarStatusDelegate();
        private void SetTaskbarStatus()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetTaskbarStatusDelegate(SetTaskbarStatus));
            else
            {
                Thread.Sleep(100);
                Win7Taskbar.SetProgressState(p.Handle, TBPF.NOPROGRESS);

                if (Finished == 0)
                {
                    //"Готово" в Taskbar
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NOPROGRESS);
                    if (this.IsVisible) this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(FFRebuilder_IsVisibleChanged);
                }
                else if (Finished == 1)
                {
                    //"Ошибка" в Taskbar
                    Win7Taskbar.SetProgressTaskComplete(ActiveHandle, TBPF.ERROR);
                    if (this.IsVisible) this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(FFRebuilder_IsVisibleChanged);
                }
                else if (IsPaused)
                {
                    //"Пауза" в Taskbar
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.PAUSED);
                    Win7Taskbar.SetProgressValue(ActiveHandle, Convert.ToUInt64(progress.Value), 100);
                }
            }
        }

        private void LoadAllProfiles()
        {
            combo_profile.Items.Clear();
            combo_profile.Items.Add("Default");
            try
            {
                foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\ffrebuilder", "*.txt"))
                {
                    combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch { }
            combo_profile.SelectedItem = profile;
        }

        private void LoadFromProfile()
        {
            try
            {
                //Сбрасываем на дефолты
                bool cli_loaded = false;
                format = "AUTO";
                vcodec = vcodecs.COPY;
                color = colorspace.YV12;
                aspect = "AUTO";
                framerate = "AUTO";
                acodec = acodecs.COPY;
                bits = "S16LE";
                //atrack = 0; //Не сохраняется в пресете
                srate = "AUTO";
                channels = "AUTO";

                if (profile != "Default")
                {
                    using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\ffrebuilder\\" + profile + ".txt", System.Text.Encoding.Default))
                    {
                        string line;
                        while (!sr.EndOfStream)
                        {
                            line = sr.ReadLine();
                            if (line == "[FORMAT]") format = sr.ReadLine().ToUpper();
                            else if (line == "[VCODEC]") vcodec = (vcodecs)Enum.Parse(typeof(vcodecs), sr.ReadLine(), true);
                            else if (line == "[COLORSPACE]") color = (colorspace)Enum.Parse(typeof(colorspace), sr.ReadLine(), true);
                            else if (line == "[ASPECT]") aspect = sr.ReadLine().ToUpper();
                            else if (line == "[FRAMERATE]") framerate = sr.ReadLine().ToUpper();
                            else if (line == "[ACODEC]") acodec = (acodecs)Enum.Parse(typeof(acodecs), sr.ReadLine(), true);
                            else if (line == "[PCM_BITS]") bits = sr.ReadLine().ToUpper();
                            else if (line == "[SAMPLERATE]") srate = sr.ReadLine().ToUpper();
                            else if (line == "[CHANNELS]") channels = sr.ReadLine().ToUpper();
                            else if (line == "[COMMAND_LINE]") { text_cli.Text = sr.ReadLine(); cli_loaded = true; }
                        }
                    }
                }

                //Выставляем значения
                if (!combo_format.Items.Contains(format))
                    combo_format.Items.Add(format);
                combo_format.SelectedItem = format;
                combo_vcodec.SelectedItem = vcodec;
                combo_colorspace.SelectedItem = color;
                if (!combo_aspect.Items.Contains(aspect))
                    combo_aspect.Items.Add(aspect);
                combo_aspect.SelectedItem = aspect;
                if (!combo_framerate.Items.Contains(framerate))
                    combo_framerate.Items.Add(framerate);
                combo_framerate.SelectedItem = framerate;
                combo_acodec.SelectedItem = acodec;
                combo_bits.SelectedItem = bits;
                //combo_atrack.SelectedIndex = atrack;
                if (!combo_srate.Items.Contains(srate))
                    combo_srate.Items.Add(srate);
                combo_srate.SelectedItem = srate;
                if (!combo_channels.Items.Contains(channels))
                    combo_channels.Items.Add(channels);
                combo_channels.SelectedItem = channels;
                if (!cli_loaded || text_cli.Text.Trim() == "")
                    UpdateCommandLine();
                UpdateTracksMapping();
            }
            catch (Exception ex)
            {
                //При ошибке загружаем дефолты
                if (profile != "Default")
                {
                    ErrorException(Languages.Translate("Error loading profile") + " \"" + profile + "\": " + ex.Message +
                        "\r\n" + Languages.Translate("Retrying with profile") + " \"Default\"...");
                    combo_profile.SelectedItem = profile = Settings.FFRebuilder_Profile = "Default";
                    LoadFromProfile();
                    return;
                }
                else
                    ErrorException(Languages.Translate("Error loading profile") + " \"Default\": " + ex.Message);
            }
        }

        private void SetFFInfo(string filepath)
        {
            try
            {
                //Сброс треков
                atracks = new ArrayList();
                vtracks = new ArrayList();
                atrack = 0;

                ff = new FFInfo();
                ff.Open(filepath);

                if (ff.info != null)
                {
                    string sortedinfo = "";
                    string[] lines = ff.info.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("  configuration:") &&
                            !line.StartsWith("  lib") &&
                            !line.StartsWith("  built on") &&
                            !line.StartsWith("At least one output") &&
                            !line.StartsWith("This program is not") &&
                            line != "")
                            sortedinfo += line + Environment.NewLine;
                    }

                    text_info.Text = sortedinfo;
                    text_info.ScrollToEnd();
                }
                else
                    text_info.Clear();

                //Видео и аудио треки
                vtracks = ff.VideoStreams(); //Все видео
                atracks = ff.AudioStreams(); //Все аудио

                combo_atrack.Items.Clear();
                combo_atrack.Items.Add(new ComboBoxItem() { Content = "AUTO", ToolTip = Languages.Translate("Select audio track") });
                if (atracks.Count > 0)
                {
                    for (int i = 0; i < atracks.Count; i++)
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = "#" + (i + 1);
                        item.ToolTip = ff.StreamFull((int)atracks[i]);
                        combo_atrack.Items.Add(item);
                    }
                }
                combo_atrack.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                text_info.Text = Languages.Translate("Error") + ": " + ex.Message;
            }
            finally
            {
                CloseFF();
            }
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                DateTime start_time = DateTime.Now;

                //получаем колличество секунд
                ff = new FFInfo();
                ff.Open(infile);
                int seconds = (int)ff.Duration().TotalSeconds;
                CloseFF();

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = false;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;

                //Получаем аргументы
                info.Arguments = command_line.Replace("input_file", infile).Replace("output_file", outfile);
                SetLog(info.Arguments + Environment.NewLine);

                encoderProcess.StartInfo = info;
                encoderProcess.Start();

                string line;
                string pat = @"time=(\d+.\d+)";
                Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                Match mat;

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardError.ReadLine();

                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            double ctime = Calculate.ConvertStringToDouble(mat.Groups[1].Value);
                            double pr = ((double)ctime / (double)seconds) * 100.0;
                            worker.ReportProgress((int)pr);
                        }
                        else
                        {
                            if (!line.StartsWith("Press [q]") && !line.StartsWith("    Last"))
                                SetLog(line);
                        }
                    }
                }

                //Дочитываем остатки лога
                SetLog(encoderProcess.StandardError.ReadToEnd());
                
                //Проверяем на ошибки
                if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
                    IsErrors = true;

                //чистим ресурсы
                FinalizeEncoderProcess(false);

                if (!IsErrors && !IsAborted)
                {
                    //прописываем сколько это всё заняло у нас врмени
                    TimeSpan enc_time = (DateTime.Now - start_time);
                    string enc_time_s = "";
                    if (enc_time.Days > 0) enc_time_s += enc_time.Days + " day ";
                    if (enc_time.Hours > 0) enc_time_s += enc_time.Hours + " hour ";
                    if (enc_time.Minutes > 0) enc_time_s += enc_time.Minutes + " min ";
                    if (enc_time.Seconds > 0) enc_time_s += enc_time.Seconds + " sec";

                    SetLog("\r\n\r\n------------------------------");
                    SetLog(Languages.Translate("Total encoding time:") + " " + enc_time_s);
                }
            }
            catch (Exception ex)
            {
                if (!IsAborted)
                {
                    IsErrors = true;
                    SetLog("\r\n" + ex.Message);
                }
            }
        }

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progress.Value = e.ProgressPercentage;
            Title = "(" + e.ProgressPercentage + "%)";
            Win7Taskbar.SetProgressValue(ActiveHandle, Convert.ToUInt64(e.ProgressPercentage), 100);
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (this.IsVisible)
                    this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(FFRebuilder_IsVisibleChanged);

                //проверка на удачное завершение
                if (File.Exists(outfile))
                {
                    FileInfo out_info = new FileInfo(outfile);
                    if (out_info.Length > 0 && !IsErrors && !IsAborted)
                    {
                        //Нет ошибок
                        Finished = 0;
                        SetLog(Languages.Translate("Out file size is:") + " " + Calculate.ConvertDoubleToPointString((double)out_info.Length / 1024.0 / 1024.0, 2) + " mb");
                        button_play.Visibility = Visibility.Visible;

                        //"Готово" в Taskbar
                        Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NOPROGRESS);
                    }
                    else //if (out_info.Length == 0)
                    {
                        SafeDelete(outfile);
                    }
                }

                if (IsErrors && !IsAborted)
                {
                    //Есть ошибки
                    Finished = 1;
                    SetLog("\r\n\r\n" + Languages.Translate("Error") + "!");

                    //"Ошибка" в Taskbar
                    Win7Taskbar.SetProgressTaskComplete(ActiveHandle, TBPF.ERROR);
                }
                else if (IsAborted)
                {
                    SetLog("\r\n\r\n" + Languages.Translate("Cancelled") + "!");

                    //"Отмена" в Taskbar
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NOPROGRESS);
                }
            }
            catch (Exception ex)
            {
                SetLog(ex.Message);
            }

            button_start.Content = Languages.Translate("Start");
            Title = "FFRebuilder";
            progress.Value = 0;
        }

        private void UpdateCommandLine()
        {
            string _vcodec = " -vcodec copy";
            if (vcodec == vcodecs.DISABLED) _vcodec = " -vn";
            else if (vcodec == vcodecs.UNCOMPRESSED) _vcodec = " -vcodec rawvideo";
            else if (vcodec != vcodecs.COPY) _vcodec = " -vcodec " + vcodec.ToString().ToLower();

            string _acodec = " -acodec copy";
            if (acodec == acodecs.DISABLED) _acodec = " -an";
            else if (acodec == acodecs.FLAC) _acodec = " -acodec flac";
            else if (acodec == acodecs.PCM) _acodec = " -acodec pcm_" + bits.ToLower();

            string _format = "";
            if (format == "MPG") _format = " -f vob";
            else if (format == "H264") _format = " -f h264";
            else if (format == "VC1") _format = " -f vc1";
            else if (format == "WEBM") _format = " -f webm";
            else if (format == "TRUEHD") _format = " -f truehd";

            string _color = "";
            if (vcodec == vcodecs.FFV1 || vcodec == vcodecs.FFVHUFF || vcodec == vcodecs.UNCOMPRESSED)
            {
                if (color == colorspace.YV12) _color = " -pix_fmt yuv420p";
                else if (color == colorspace.YUY2) _color = " -pix_fmt yuv422p";
                else if (color == colorspace.RGB24) _color = " -pix_fmt bgr24";
                else if (color == colorspace.RGB32) _color = " -pix_fmt bgra";
            }

            string _aspect = "";
            if (aspect != "AUTO" && vcodec != vcodecs.DISABLED) _aspect = " -aspect " + aspect;

            string _framerate = "";
            if (framerate != "AUTO" && vcodec != vcodecs.DISABLED) _framerate = " -r " + framerate;

            string _vmap = "", _amap = "";
            if (atracks.Count > 0 && atrack > 0 && atracks.Count >= atrack && acodec != acodecs.DISABLED)
            {
                _amap = " -map 0." + (int)atracks[atrack - 1];
                if (vtracks.Count > 0 && vcodec != vcodecs.DISABLED)
                    _vmap = " -map 0." + (int)vtracks[0];
            }

            string _srate = "";
            if (srate != "AUTO" && (acodec == acodecs.FLAC || acodec == acodecs.PCM)) _srate = " -ar " + srate;

            string _channels = "";
            if (channels != "AUTO" && (acodec == acodecs.FLAC || acodec == acodecs.PCM)) _channels = " -ac " + channels;

            text_cli.Text = "-i \"input_file\" -sn" + _vmap + _amap + _vcodec + _color + _framerate + _aspect + _acodec + _srate + _channels + _format + " \"output_file\"";
            text_cli.CaretIndex = text_cli.Text.Length;
        }

        private void UpdateTracksMapping()
        {
            string text = "";

            //Сначала удаляем все " -map 0:x"
            text = Regex.Replace(text_cli.Text, @"\s-map\s0\.\d+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled).Trim();

            //Определяем, нужно ли вписывать -map
            string map = "";
            if (atracks.Count > 0 && atrack > 0 && atracks.Count >= atrack && acodec != acodecs.DISABLED)
            {
                map = " -map 0." + (int)atracks[atrack - 1];
                if (vtracks.Count > 0 && vcodec != vcodecs.DISABLED)
                    map = " -map 0." + (int)vtracks[0] + map;

                //Ищем, куда бы вписать..
                int index = -1;
                if ((index = text.IndexOf("-i \"input_file\" -sn", StringComparison.InvariantCultureIgnoreCase)) >= 0)
                    text = text.Insert(index + 19, map);
                else if ((index = text.IndexOf("-i \"input_file\"", StringComparison.InvariantCultureIgnoreCase)) >= 0)
                    text = text.Insert(index + 15, map);
                else
                    text = map + " " + text;
            }

            text_cli.Text = text;
        }

        private void ErrorException(string message)
        {
            ShowMessage(message, Languages.Translate("Error"));
        }

        internal delegate void MessageDelegate(string mtext, string mtitle);
        private void ShowMessage(string mtext, string mtitle)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), mtext, mtitle);
            else
            {
                Message mes = new Message((this.IsVisible) ? this : Owner);
                mes.ShowMessage(mtext, mtitle);
            }
        }

        internal delegate void LogDelegate(string data);
        private void SetLog(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new LogDelegate(SetLog), data);
            else
            {
                textbox_log.AppendText(data + Environment.NewLine);
                textbox_log.ScrollToEnd();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Нужно сбросить прогресс в MainWindow
            this.ActiveHandle = IntPtr.Zero;
            this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(FFRebuilder_IsVisibleChanged);
            Win7Taskbar.SetProgressState(p.Handle, TBPF.NOPROGRESS);

            CloseFF();
            FinalizeEncoderProcess(true);
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                if (IsPaused)
                    button_start_Click(null, null);

                CloseFF();
                FinalizeEncoderProcess(true);
            }
            else if (worker == null || !worker.IsBusy)
                Close();
        }

        private void CloseFF()
        {
            lock (lock_ff)
            {
                if (ff != null)
                {
                    ff.Close();
                    ff = null;
                }
            }
        }

        private void FinalizeEncoderProcess(bool is_aborted)
        {
            lock (lock_pr)
            {
                if (encoderProcess != null)
                {
                    try
                    {
                        if (is_aborted) IsAborted = true;
                        if (!encoderProcess.HasExited)
                        {
                            encoderProcess.Kill();
                            encoderProcess.WaitForExit();
                        }
                    }
                    catch { }
                    finally
                    {
                        encoderProcess.Close();
                        encoderProcess.Dispose();
                        encoderProcess = null;
                        if (is_aborted) SafeDelete(outfile);
                    }
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
                ErrorException(ex.Message);
            }
        }

        private void button_start_Click(object sender, RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                if (!IsPaused)
                {
                    locker.Reset();
                    IsPaused = true;
                    button_start.Content = Languages.Translate("Resume");
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.PAUSED);
                }
                else
                {
                    locker.Set();
                    IsPaused = false;
                    button_start.Content = Languages.Translate("Pause");
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NORMAL);
                }
            }
            else
            {
                if (textbox_infile.Text != "" && File.Exists(textbox_infile.Text) && textbox_outfile.Text != "")
                {
                    //запоминаем переменные
                    infile = textbox_infile.Text;
                    outfile = textbox_outfile.Text;
                    command_line = text_cli.Text;

                    if (File.Exists(outfile))
                    {
                        //Такой файл уже есть!
                        Message mes = new Message(this);
                        mes.ShowMessage(Languages.Translate("File \"file_name\" already exists! Overwrite?").Replace("file_name", Path.GetFileName(outfile)),
                            Languages.Translate("Question"), Message.MessageStyle.YesNo);
                        if (mes.result == Message.Result.Yes)
                        {
                            SafeDelete(outfile);
                        }
                        else
                        {
                            System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                            s.AddExtension = true;
                            s.SupportMultiDottedExtensions = true;
                            s.Title = Languages.Translate("Select unique name for output file:");
                            s.FileName = outfile;
                            s.Filter = Path.GetExtension(outfile).ToUpper().Replace(".", "") + " " + Languages.Translate("files") + "|*" + Path.GetExtension(outfile);
                            if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                textbox_outfile.Text = outfile = s.FileName;
                                SafeDelete(outfile);
                            }
                            else
                                return;
                        }
                    }

                    //Сброс
                    Finished = -1;
                    textbox_log.Clear();
                    tabs.SelectedIndex = 1;
                    IsErrors = IsAborted = IsPaused = false;
                    button_play.Visibility = Visibility.Collapsed;
                    button_start.Content = Languages.Translate("Pause");
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NOPROGRESS); //NORMAL
                    this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(FFRebuilder_IsVisibleChanged);

                    //фоновое кодирование
                    CreateBackgroundWorker();
                    worker.RunWorkerAsync();
                }
            }
        }

        private void button_open_Click(object sender, RoutedEventArgs e)
        {
            if (worker == null || !worker.IsBusy)
            {
                ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
                if (files.Count > 0) OpenFile(files[0].ToString());
            }
        }

        private void LayoutRoot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void LayoutRoot_Drop(object sender, DragEventArgs e)
        {
            foreach (string dropfile in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                OpenFile(dropfile);
                return;
            }
        }

        private void OpenFile(string path)
        {
            textbox_infile.Text = path;
            text_cli.Focus();
            this.Activate();

            if (format == "AUTO")
            {
                string ext = Path.GetExtension(textbox_infile.Text).ToLower();
                if (ext == ".avs") ext = ".avi";
                textbox_outfile.Text = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + ext;
            }
            else
                textbox_outfile.Text = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed." + format.ToLower();

            SetFFInfo(textbox_infile.Text);
            //UpdateCommandLine();
            UpdateTracksMapping();
            UpdateCombosIsEnabled();
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_infile.Text != "")
            {
                System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                s.AddExtension = true;
                s.SupportMultiDottedExtensions = true;
                s.Title = Languages.Translate("Select unique name for output file:");
                s.FileName = textbox_outfile.Text;
                s.Filter = Languages.Translate("All files") + "|*.*";

                if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textbox_outfile.Text = s.FileName;
                    SafeDelete(s.FileName);
                }
            }
        }

        private void combo_vcodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_vcodec.IsDropDownOpen || combo_vcodec.IsSelectionBoxHighlighted) && combo_vcodec.SelectedItem != null)
            {
                vcodec = (vcodecs)combo_vcodec.SelectedItem;
                UpdateCommandLine();
                SetCustomProfile();
                UpdateCombosIsEnabled();
            }
        }

        private void combo_colorspace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_colorspace.IsDropDownOpen || combo_colorspace.IsSelectionBoxHighlighted) && combo_colorspace.SelectedItem != null)
            {
                //FFV1 и FFVHUFF не поддерживают RGB24
                if ((vcodec == vcodecs.FFV1 || vcodec == vcodecs.FFVHUFF) && ((colorspace)combo_colorspace.SelectedItem) == colorspace.RGB24)
                {
                    combo_colorspace.SelectedItem = color;
                    return;
                }

                color = (colorspace)combo_colorspace.SelectedItem;
                UpdateCommandLine();
                SetCustomProfile();
            }
        }

        private void combo_aspect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_aspect.IsDropDownOpen || combo_aspect.IsSelectionBoxHighlighted) && combo_aspect.SelectedItem != null)
            {
                aspect = combo_aspect.SelectedItem.ToString();
                UpdateCommandLine();
                SetCustomProfile();
            }
        }

        private void combo_framerate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_framerate.IsDropDownOpen || combo_framerate.IsSelectionBoxHighlighted) && combo_framerate.SelectedItem != null)
            {
                framerate = combo_framerate.SelectedItem.ToString();
                UpdateCommandLine();
                SetCustomProfile();
            }
        }

        private void combo_acodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_acodec.IsDropDownOpen || combo_acodec.IsSelectionBoxHighlighted) && combo_acodec.SelectedItem != null)
            {
                acodec = (acodecs)combo_acodec.SelectedItem;
                UpdateCommandLine();
                SetCustomProfile();
                UpdateCombosIsEnabled();
            }
        }

        private void combo_bits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_bits.IsDropDownOpen || combo_bits.IsSelectionBoxHighlighted) && combo_bits.SelectedItem != null)
            {
                bits = combo_bits.SelectedItem.ToString();
                UpdateCommandLine();
                SetCustomProfile();
            }

            if (bits.StartsWith("S")) combo_bits.ToolTip = "Signed ";
            else combo_bits.ToolTip = "Unsigned ";
            if (bits.Contains("8")) combo_bits.ToolTip += "8-Bit";
            else if (bits.Contains("16")) combo_bits.ToolTip += "16-Bit";
            else if (bits.Contains("24")) combo_bits.ToolTip += "24-Bit";
            else combo_bits.ToolTip += "32-Bit";
            if (bits.EndsWith("LE")) combo_bits.ToolTip += " Little-Endian";
            else if (bits.EndsWith("BE")) combo_bits.ToolTip += " Big-Endian";
        }

        private void combo_atrack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_atrack.IsDropDownOpen || combo_atrack.IsSelectionBoxHighlighted) && combo_atrack.SelectedIndex != -1)
            {
                atrack = combo_atrack.SelectedIndex;
                UpdateTracksMapping();
            }
        }

        private void combo_srate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_srate.IsDropDownOpen || combo_srate.IsSelectionBoxHighlighted) && combo_srate.SelectedIndex != -1)
            {
                srate = combo_srate.SelectedItem.ToString();
                UpdateCommandLine();
                SetCustomProfile();
            }
        }

        private void combo_channels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_channels.IsDropDownOpen || combo_channels.IsSelectionBoxHighlighted) && combo_channels.SelectedIndex != -1)
            {
                channels = combo_channels.SelectedItem.ToString();
                UpdateCommandLine();
                SetCustomProfile();
            }
        }

        private void combo_format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_format.IsDropDownOpen || combo_format.IsSelectionBoxHighlighted || combo_format.IsEditable) && combo_format.SelectedItem != null)
            {
                if (combo_format.SelectedIndex == 0)
                {
                    //Включаем редактирование
                    combo_format.IsEditable = true;
                    combo_format.ToolTip = Languages.Translate("Enter - apply, Esc - cancel.");
                    combo_format.ApplyTemplate();
                    return;
                }
                else
                {
                    //AUTO, AVI, DV, MP4, M4V, MOV, MKV, WEBM, MPG, TS, FLV, OGG, WMV,
                    //H264, VC1, AC3, FLAC, WAV, M4A, AAC, MKA, MP3, MP2, WMA, DTS, TRUEHD
                    format = combo_format.SelectedItem.ToString();
                    if (format == "WAV")
                    {
                        combo_vcodec.SelectedItem = vcodec = vcodecs.DISABLED;
                        if (acodec != acodecs.COPY && acodec != acodecs.PCM)
                            combo_acodec.SelectedItem = acodec = acodecs.PCM;
                    }
                    else if (format == "FLAC")
                    {
                        combo_vcodec.SelectedItem = vcodec = vcodecs.DISABLED;
                        if (acodec != acodecs.COPY && acodec != acodecs.FLAC)
                            combo_acodec.SelectedItem = acodec = acodecs.FLAC;
                    }
                    else if (format == "MKA")
                    {
                        combo_vcodec.SelectedItem = vcodec = vcodecs.DISABLED;
                        if (acodec == acodecs.DISABLED)
                            combo_acodec.SelectedItem = acodec = acodecs.COPY;
                    }
                    else if (format == "M4A" || format == "AAC" || format == "MP3" || format == "MP2" ||
                        format == "AC3" || format == "WMA" || format == "DTS" || format == "TRUEHD")
                    {
                        combo_vcodec.SelectedItem = vcodec = vcodecs.DISABLED;
                        combo_acodec.SelectedItem = acodec = acodecs.COPY;
                    }
                    else if (format == "H264" || format == "VC1")
                    {
                        combo_vcodec.SelectedItem = vcodec = vcodecs.COPY;
                        combo_acodec.SelectedItem = acodec = acodecs.DISABLED;
                    }
                    else
                    {
                        if (vcodec == vcodecs.DISABLED)
                            combo_vcodec.SelectedItem = vcodec = vcodecs.COPY;
                        if (acodec == acodecs.DISABLED)
                            combo_acodec.SelectedItem = acodec = acodecs.COPY;
                    }

                    UpdateCommandLine();
                    SetCustomProfile();
                    UpdateCombosIsEnabled();
                }
            }

            if (combo_format.IsEditable)
            {
                //Выключаем редактирование
                combo_format.IsEditable = false;
                combo_format.ToolTip = null;
            }

            if (!string.IsNullOrEmpty(textbox_infile.Text))
            {
                string ext = ((format == "AUTO") ? Path.GetExtension(textbox_infile.Text) : "." + format.ToString()).ToLower();
                if (ext == ".avs") ext = ".avi";

                if (textbox_outfile.Text == "")
                    textbox_outfile.Text = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + ext;
                else
                    textbox_outfile.Text = Calculate.RemoveExtention(textbox_outfile.Text, true) + ext;
            }
        }

        private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            if (box.IsEditable && box.SelectedItem != null && !box.IsDropDownOpen && !box.IsMouseCaptured)
                combo_format_KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter));
        }

        private void combo_format_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                //Проверяем введённый текст
                string text = combo_format.Text.Trim().ToUpper();
                if (text == "" || Calculate.GetRegexValue(@"^(\w+)$", text) == null)
                { combo_format.SelectedItem = format; return; }

                //Добавляем и выбираем Item
                if (!combo_format.Items.Contains(text))
                    combo_format.Items.Add(text);
                combo_format.SelectedItem = text;
            }
            else if (e.Key == Key.Escape)
            {
                //Возвращаем исходное значение
                combo_format.SelectedItem = format;
            }
        }

        private void button_play_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_outfile.Text != "" && File.Exists(textbox_outfile.Text))
            {
                try
                {
                    Process.Start(textbox_outfile.Text);
                }
                catch (Exception ex)
                {
                    ErrorException(ex.Message);
                }
            }
        }

        private void combo_profile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_profile.SelectedItem == null) return;
            if ((combo_profile.IsDropDownOpen || combo_profile.IsSelectionBoxHighlighted))
            {
                Settings.FFRebuilder_Profile = profile = combo_profile.SelectedItem.ToString();
                LoadFromProfile();
                UpdateCombosIsEnabled();
            }

            combo_profile.ToolTip = Languages.Translate("Profile:") + " " + combo_profile.SelectedItem.ToString();
        }

        private void button_add_profile_Click(object sender, RoutedEventArgs e)
        {
            NewProfile newp = new NewProfile("Custom " + format, null, NewProfile.ProfileType.FFRebuilder, this);
            if (string.IsNullOrEmpty(newp.profile)) return;
            SaveProfile(newp.profile);
            LoadAllProfiles();
            Settings.FFRebuilder_Profile = profile;
        }

        private void button_store_profile_Click(object sender, RoutedEventArgs e)
        {
            if (profile != "Default")
                SaveProfile(profile);
        }

        private void SaveProfile(string name)
        {
            try
            {
                string text = "";
                text += "[FORMAT]\r\n" + combo_format.SelectedItem.ToString() + "\r\n\r\n";
                text += "[VCODEC]\r\n" + combo_vcodec.SelectedItem.ToString() + "\r\n\r\n";
                text += "[COLORSPACE]\r\n" + combo_colorspace.SelectedItem.ToString() + "\r\n\r\n";
                text += "[ASPECT]\r\n" + combo_aspect.SelectedItem.ToString() + "\r\n\r\n";
                text += "[FRAMERATE]\r\n" + combo_framerate.SelectedItem.ToString() + "\r\n\r\n";
                text += "[ACODEC]\r\n" + combo_acodec.SelectedItem.ToString() + "\r\n\r\n";
                text += "[PCM_BITS]\r\n" + combo_bits.SelectedItem.ToString() + "\r\n\r\n";
                text += "[SAMPLERATE]\r\n" + combo_srate.SelectedItem.ToString() + "\r\n\r\n";
                text += "[CHANNELS]\r\n" + combo_channels.SelectedItem.ToString() + "\r\n\r\n";
                text += "[COMMAND_LINE]\r\n";

                //Удаляем все " -map 0:x", т.к. эта опция зависит от исходника
                if (combo_atrack.SelectedIndex > 0)
                    text += Regex.Replace(text_cli.Text, @"\s-map\s0\.\d+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                else
                    text += text_cli.Text;

                string path = Calculate.StartupPath + "\\presets\\ffrebuilder\\" + name + ".txt";
                File.WriteAllText(path, text, System.Text.Encoding.Default);
                profile = name;
            }
            catch (DirectoryNotFoundException ex)
            {
                //Если папки нет, создаем её и пробуем снова
                if (!Directory.Exists(Calculate.StartupPath + "\\presets\\ffrebuilder"))
                {
                    try
                    {
                        Directory.CreateDirectory(Calculate.StartupPath + "\\presets\\ffrebuilder");
                    }
                    catch (Exception exc)
                    {
                        ErrorException("Can`t create directory: " + exc.Message);
                        return;
                    }
                    SaveProfile(name);
                }
                else
                    ErrorException(Languages.Translate("Can`t save profile") + ": " + ex.Message);
            }
            catch (Exception ex)
            {
                ErrorException(Languages.Translate("Can`t save profile") + ": " + ex.Message);
            }
        }

        private void button_remove_profile_Click(object sender, RoutedEventArgs e)
        {
            if (profile == "Default" || combo_profile.Items.Count <= 1) return;

            Message mess = new Message(this);
            mess.ShowMessage(Languages.Translate("Do you realy want to remove profile") + " \"" + profile + "\"?",
                Languages.Translate("Question"), Message.MessageStyle.YesNo);
            if (mess.result == Message.Result.Yes)
            {
                try
                {
                    File.Delete(Calculate.StartupPath + "\\presets\\ffrebuilder\\" + profile + ".txt");
                    Settings.FFRebuilder_Profile = profile = "Default";
                }
                catch (Exception ex)
                {
                    ErrorException(Languages.Translate("Can`t delete profile") + ": " + ex.Message);
                }

                LoadAllProfiles();
                LoadFromProfile();
                UpdateCombosIsEnabled();
            }
        }

        private void SomethingChanged(object sender, TextChangedEventArgs e)
        {
            button_play.Visibility = Visibility.Collapsed;
        }

        private void SetCustomProfile()
        {
            SaveProfile("Custom");
            LoadAllProfiles();
            Settings.FFRebuilder_Profile = profile;
        }

        private void button_help_Click(object sender, RoutedEventArgs e)
        {
            if (combo_help.Text == "") return;
            if (!combo_help.Items.Contains(combo_help.Text))
                combo_help.Items.Add(combo_help.Text);
            combo_help.SelectedItem = combo_help.Text;
            ShowHelp();
        }

        private void combo_help_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_help.IsDropDownOpen && combo_help.SelectedItem != null)
            {
                combo_help.Text = combo_help.SelectedItem.ToString();
                ShowHelp();
            }
        }

        private void ShowHelp()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                help.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = combo_help.Text;
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                help.RedirectStandardError = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                //Именно в таком порядке (а по хорошему надо в отдельных потоках)
                string std_out = p.StandardOutput.ReadToEnd();
                string std_err = p.StandardError.ReadToEnd();
                textbox_help.Text = std_err + "\r\n" + std_out;
                textbox_help.ScrollToHome();
            }
            catch (Exception ex)
            {
                textbox_help.Text = Languages.Translate("Error") + ": " + ex.Message;
            }
        }

        private void tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Это нужно только один раз
            if (tab_help.IsSelected)
            {
                ShowHelp();
                tabs.SelectionChanged -= new SelectionChangedEventHandler(tabs_SelectionChanged);
            }
        }

        private void button_search_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_search.Text == "" || textbox_search.Foreground != Brushes.Black)
            {
                textbox_search.Focus();
                return;
            }

            string search = textbox_search.Text;
            if (last_search_index >= textbox_help.Text.Length) last_search_index = 0;
            int search_index = (search.ToLower() == last_search.ToLower()) ? last_search_index : 0;
            int index = textbox_help.Text.IndexOf(search, search_index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                if (last_search_index > 0)
                {
                    last_search_index = 0;
                    button_search_Click(null, null);
                }
                return;
            }
            last_search = search;
            last_search_index = index + search.Length;

            textbox_help.Focus();
            textbox_help.SelectionStart = index;
            textbox_help.SelectionLength = search.Length;
        }

        private void textbox_search_GotFocus(object sender, RoutedEventArgs e)
        {
            //Это нужно только один раз
            if (textbox_search.IsFocused)
            {
                textbox_search.Text = "";
                textbox_search.Foreground = Brushes.Black;
                textbox_search.FontStyle = FontStyles.Normal;
                textbox_search.GotFocus -= new RoutedEventHandler(textbox_search_GotFocus);
            }
        }

        private void textbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (combo_help.IsSelectionBoxHighlighted) button_help_Click(null, null);
                else if (textbox_search.IsFocused) button_search_Click(null, null);
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control || e.Key == Key.F3)
            {
                if (textbox_search.Text == "" || textbox_search.Foreground != Brushes.Black) textbox_search.Focus();
                else button_search_Click(null, null);
            }
        }

        private void UpdateCombosIsEnabled()
        {
            if (vtracks.Count == 0)
            {
                combo_vcodec.IsEnabled = false;
                combo_colorspace.IsEnabled = false;
                combo_aspect.IsEnabled = false;
                combo_framerate.IsEnabled = false;
            }
            else if (vcodec == vcodecs.DISABLED)
            {
                combo_vcodec.IsEnabled = true;
                combo_colorspace.IsEnabled = false;
                combo_aspect.IsEnabled = false;
                combo_framerate.IsEnabled = false;
            }
            else if (vcodec == vcodecs.COPY)
            {
                combo_vcodec.IsEnabled = true;
                combo_colorspace.IsEnabled = false;
                combo_aspect.IsEnabled = true;
                combo_framerate.IsEnabled = true;
            }
            else
            {
                combo_vcodec.IsEnabled = true;
                combo_colorspace.IsEnabled = true;
                combo_aspect.IsEnabled = true;
                combo_framerate.IsEnabled = true;
            }

            if (atracks.Count == 0)
            {
                combo_acodec.IsEnabled = false;
                combo_bits.IsEnabled = false;
                combo_atrack.IsEnabled = false;
                combo_srate.IsEnabled = false;
                combo_channels.IsEnabled = false;
            }
            else if (acodec == acodecs.DISABLED)
            {
                combo_acodec.IsEnabled = true;
                combo_bits.IsEnabled = false;
                combo_atrack.IsEnabled = false;
                combo_srate.IsEnabled = false;
                combo_channels.IsEnabled = false;
            }
            else if (acodec == acodecs.COPY)
            {
                combo_acodec.IsEnabled = true;
                combo_bits.IsEnabled = false;
                combo_atrack.IsEnabled = true;
                combo_srate.IsEnabled = false;
                combo_channels.IsEnabled = false;
            }
            else if (acodec == acodecs.PCM)
            {
                combo_acodec.IsEnabled = true;
                combo_bits.IsEnabled = true;
                combo_atrack.IsEnabled = true;
                combo_srate.IsEnabled = true;
                combo_channels.IsEnabled = true;
            }
            else
            {
                combo_acodec.IsEnabled = true;
                combo_bits.IsEnabled = false;
                combo_atrack.IsEnabled = true;
                combo_srate.IsEnabled = true;
                combo_channels.IsEnabled = true;
            }
        }
    }
}