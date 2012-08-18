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
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;

namespace XviD4PSP
{
	public partial class Encoder
	{
        private ManualResetEvent locker = new ManualResetEvent(true);
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        private AviSynthEncoder avs = null;
        private Massive m;
        private MainWindow p;
        private bool IsAborted = false;
        private bool IsPaused = false;
        private bool IsErrors = false;
        private bool CopyDelay = false;
        private bool IsSplitting = false;
        private bool IsAutoAborted = false;
        private bool IsNoProgressStage = false;
        private bool DontMuxStreams = false;
        private bool IsDirectRemuxingPossible = false;
        private bool AudioFirst = Settings.EncodeAudioFirst;
        private string unknown = Languages.Translate("Unknown");
        private string Splitting;

        private System.Timers.Timer timer;
        private int old_frame = 0;
        private int frame = 0;
        private DateTime old_time;
        private int step = 0;
        private int steps = 0;
        private double fps = 0.0;
        private double encoder_fps = 0.0;
        private List<double> fps_averages = new List<double>();
        private TimeSpan last_update = TimeSpan.Zero;
        private double last_update_threshold = 10; //Мин.
        private string busyfile;
        private string outpath_src;
        private Format.Muxers muxer;
        private DateTime start_time;
        private Shutdown.ShutdownMode ending = Settings.FinalAction;

        //Таскбар
        private IntPtr ActiveHandle = IntPtr.Zero;
        private UInt64 total_pr_max = 0;
        private int Finished = -1; //0 - OK, 1 - Error

        //Логи энкодеров
        private StringBuilder encodertext = new StringBuilder();
        private void AppendEncoderText(string text)
        {
            if (encodertext.Length > 0)
            {
                //Укорачиваем лог, если он слишком длинный
                if (encodertext.Length > 10000)
                {
                    int new_line_pos = encodertext.ToString().IndexOf(Environment.NewLine, 1000);
                    if (new_line_pos <= 0) new_line_pos = 1000;
                    encodertext.Remove(0, new_line_pos);
                    encodertext.Insert(0, ".....");
                }

                encodertext.Append(Environment.NewLine);
            }
            encodertext.Append(text);
        }

		public Encoder()
		{
			this.InitializeComponent();
		}

        public Encoder(Massive mass, MainWindow parent)
        {
            this.InitializeComponent();
            this.Owner = this.p = parent;
            this.m = mass.Clone();

            button_info.Visibility = Visibility.Hidden;
            button_play.Visibility = Visibility.Hidden;

            button_play.Content = Languages.Translate("Play");
            button_info.Content = Languages.Translate("Info");

            //Прячем окно, если программа минимизирована или свернута в трей
            if (!p.IsVisible || p.WindowState == WindowState.Minimized)
            {
                this.Hide();
                this.Name = "Hidden";
                this.ActiveHandle = p.Handle;
            }
            else
            {
                this.Show();
                this.ActiveHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            }

            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(Encoder_IsVisibleChanged);

            //Путь может измениться
            outpath_src = m.outfilepath;

            //А эти параметры вроде бы не меняются
            IsDirectRemuxingPossible = Format.IsDirectRemuxingPossible(m);
            DontMuxStreams = Format.GetMultiplexing(m.format);
            Splitting = Format.GetSplitting(m.format);
            muxer = Format.GetMuxer(m);

            //видео кодирование
            if (m.vencoding == "Copy") steps++;
            else steps = m.vpasses.Count;

            //аудио кодирование
            if (m.outaudiostreams.Count > 0)
            {
                if (muxer != Format.Muxers.Disabled || DontMuxStreams || m.format == Format.ExportFormats.Audio)
                    steps++;

                string codec = ((AudioStream)(m.outaudiostreams[m.outaudiostream])).codec;
                if (codec == "AAC")
                {
                    if (m.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass) steps++; //Два прохода
                    //if (muxer == Format.Muxers.tsmuxer) steps++; //Извлечение aac из m4a (видимо оно больше не нужно)
                }
                else if (codec == "QAAC")
                {
                    //if (muxer == Format.Muxers.tsmuxer) steps++; //Извлечение aac из m4a (видимо оно больше не нужно)
                }
                else if (codec == "Copy" && Settings.CopyDelay) CopyDelay = true;
            }

            //муксинг
            if (muxer == Format.Muxers.pmpavc)
            {
                steps++;
                steps++;
                steps++;
                steps++;
            }
            else if (muxer != Format.Muxers.Disabled && muxer != 0 && !DontMuxStreams)
                steps++;

            //точка отсчёта
            step--;

            //забиваем
            SetMaximum(m.outframes);

            combo_ending.Items.Clear();
            combo_ending.Items.Add(Languages.Translate("Wait"));
            combo_ending.Items.Add(Languages.Translate("Standby"));
            if (PowerManagementNativeMethods.IsPowerHibernateAllowed())
                combo_ending.Items.Add(Languages.Translate("Hibernate"));
            combo_ending.Items.Add(Languages.Translate("Shutdown"));
            combo_ending.Items.Add(Languages.Translate("Exit"));
            combo_ending.SelectedItem = Languages.Translate(ending.ToString());
            combo_ending.ToolTip = Languages.Translate("Final action");

            //фоновое кодирование
            CreateBackgoundWorker();
            worker.RunWorkerAsync();
        }

        void Encoder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!Win7Taskbar.IsInitialized) return;
            if (this.IsVisible)
            {
                //Окно энкодера развернулось, переключаем вывод прогресса на него
                ActiveHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                new Thread(new ThreadStart(this.SetTaskbarStatus)).Start();
            }
            else
            {
                //Окно энкодера свернулось, переключаем вывод прогресса в MainWindow
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
                    if (this.IsVisible) this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(Encoder_IsVisibleChanged);
                }
                else if (Finished == 1)
                {
                    //"Ошибка" в Taskbar
                    Win7Taskbar.SetProgressTaskComplete(ActiveHandle, TBPF.ERROR);
                    if (this.IsVisible) this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(Encoder_IsVisibleChanged);
                }
                else if (IsPaused)
                {
                    //"Пауза" в Taskbar
                    Win7Taskbar.SetProgressState(ActiveHandle, TBPF.PAUSED);
                    Win7Taskbar.SetProgressValue(ActiveHandle, Convert.ToUInt64(prTotal.Value), total_pr_max);
                }
            }
        }

        private void CreateBackgoundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                //AbortAction(false);
                Close();
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void AbortAction(bool is_auto_aborted)
        {
            if (is_auto_aborted)
            {
                IsErrors = true;
                IsAutoAborted = true;
            }
            else
                IsAborted = true;

            locker.Set();

            if (encoderProcess != null)
            {
                try
                {
                    if (!encoderProcess.HasExited)
                    {
                        encoderProcess.Kill();
                        encoderProcess.WaitForExit();
                    }
                }
                catch { }
            }

            if (avs != null && avs.IsBusy())
            {
                avs.stop();
                avs = null;
            }

            p.outfiles.Remove(outpath_src);
        }

        private void button_pause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsPaused)
            {
                locker.Reset();
                IsPaused = true;
                if (avs != null && avs.IsBusy()) avs.pause();
                button_pause.Content = Languages.Translate("Resume");
                button_pause.ToolTip = Languages.Translate("Resume encoding");
                Win7Taskbar.SetProgressState(ActiveHandle, TBPF.PAUSED);
            }
            else
            {
                locker.Set();
                IsPaused = false;
                if (avs != null && avs.IsBusy()) avs.resume();
                button_pause.Content = Languages.Translate("Pause");
                button_pause.ToolTip = Languages.Translate("Pause encoding");
                Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NORMAL);
            }

            encoder_fps = -1;
            last_update = TimeSpan.Zero;
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {  
            cbxPriority.Items.Add(Languages.Translate("Idle"));
            cbxPriority.Items.Add(Languages.Translate("Normal"));
            cbxPriority.Items.Add(Languages.Translate("Above normal"));
            cbxPriority.SelectedIndex = Settings.ProcessPriority;
            button_pause.Content = Languages.Translate("Pause");
            button_cancel.Content = Languages.Translate("Cancel");
            tbxLog.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            prCurrent.ToolTip = Languages.Translate("Current progress");
            prTotal.ToolTip = Languages.Translate("Total progress");
            button_pause.ToolTip = Languages.Translate("Pause encoding");
            cbxPriority.ToolTip = Languages.Translate("Change encoding priority");
            button_cancel.ToolTip = Languages.Translate("Cancel encoding");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                //Нужно сбросить прогресс в MainWindow
                this.ActiveHandle = IntPtr.Zero;
                this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(Encoder_IsVisibleChanged);
                Win7Taskbar.SetProgressState(p.Handle, TBPF.NOPROGRESS);

                AbortAction(false);
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void make_thm(int width, int height, bool fix_ar, string format)
        {
            Bitmap bmp = null;
            Graphics g = null;
            AviSynthReader reader = null;
            string new_script = m.script;
            string thmpath = Calculate.RemoveExtention(m.outfilepath) + format;

            SetLog("CREATING THM");
            SetLog("------------------------------");
            SetLog("Saving picture to: " + thmpath);
            SetLog(format.ToUpper() + " " + width + "x" + height + " \r\n");

            try
            {
                if (fix_ar)
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

                //проверка на выходы за пределы общего количества кадров
                int frame = (m.thmframe > reader.FrameCount) ? reader.FrameCount / 2 : m.thmframe;

                if (width == reader.Width && height == reader.Height)
                {
                    bmp = new System.Drawing.Bitmap(reader.ReadFrameBitmap(frame));
                }
                else
                {
                    bmp = new Bitmap(width, height);
                    g = Graphics.FromImage(bmp);

                    //метод интерполяции при ресайзе
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(reader.ReadFrameBitmap(frame), 0, 0, width, height);
                }

                if (format == "jpg")
                {
                    //процент cжатия jpg
                    System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                    System.Drawing.Imaging.EncoderParameters encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                    encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);

                    //jpg
                    bmp.Save(thmpath, info[1], encoderParameters);
                }
                else if (format == "png")
                {
                    //png
                    bmp.Save(thmpath, System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    //bmp
                    bmp.Save(thmpath, System.Drawing.Imaging.ImageFormat.Bmp);
                }
            }
            catch (Exception ex)
            {
                SetLog("\r\nError creating THM: " + ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                //завершение
                if (g != null) { g.Dispose(); g = null; }
                if (bmp != null) { bmp.Dispose(); bmp = null; }
                if (reader != null) { reader.Close(); reader = null; }
                SetLog("");
            }
        }

        private void make_x264()
        {
            bool is_x262 = (m.outvcodec == "x262");

            //10-bit depth энкодер
            bool is_10bit = (!is_x262 && m.x264options.profile == x264.Profiles.High10);

            //Хакнутый скрипт
            int bdepth = (!is_x262) ? Calculate.GetScriptBitDepth(m.script) : 8;

            //Убираем метку " --extra:"
            for (int n = 0; n < m.vpasses.Count; n++)
                m.vpasses[n] = m.vpasses[n].ToString().Replace(" --extra:", "");

            //Для Lossless нужно убрать ключ --profile
            if (!is_x262 && x264.IsLossless(m))
            {
                for (int n = 0; n < m.vpasses.Count; n++)
                    m.vpasses[n] = Regex.Replace(m.vpasses[n].ToString(), @"\s?--profile\s+\w+", "", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            }

            //прописываем интерлейс флаги
            if (m.interlace == SourceType.FILM ||
                m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                m.interlace == SourceType.INTERLACED)
            {
                if (m.deinterlace == DeinterlaceType.Disabled)
                {
                    for (int n = 0; n < m.vpasses.Count; n++)
                        m.vpasses[n] = m.vpasses[n].ToString() + (m.fieldOrder == FieldOrder.BFF ? " --bff" : " --tff");
                }
            }

            //проверяем есть ли --size режим
            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                int targetsize = (int)m.outvbitrate;
                int outabitrate = 0;
                int bitrate = 0;
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    outabitrate = outstream.bitrate;

                    if (File.Exists(outstream.audiopath))
                        bitrate = Calculate.GetBitrateForSize((double)targetsize, outstream.audiopath, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                    else
                        bitrate = Calculate.GetBitrateForSize((double)targetsize, outabitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                }
                else
                {
                    bitrate = Calculate.GetBitrateForSize((double)targetsize, outabitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                }

                m.outvbitrate = bitrate;

                for (int n = 0; n < m.vpasses.Count; n++)
                    m.vpasses[n] = m.vpasses[n].ToString().Replace("--size " + targetsize, "--bitrate " + bitrate);
            }

            step++;

            if (m.outvideofile == null)
                m.outvideofile = Settings.TempPath + "\\" + m.key + ((!is_x262) ? ".264" : ".m2v");

            //ffmpeg криво муксит raw-avc, нужно кодировать в контейнер
            if (!is_x262 && muxer == Format.Muxers.ffmpeg && !DontMuxStreams)
            {
                //А в АВИ наоборот, криво муксит из контейнера.. :(
                if (Path.GetExtension(m.outfilepath).ToLower() != ".avi")
                    m.outvideofile = Calculate.RemoveExtention(m.outvideofile, true) + ".mp4";
            }

            //Если можно кодировать сразу в контейнер или не требуется муксить
            if (muxer == Format.Muxers.Disabled || DontMuxStreams)
            {
                if (!DontMuxStreams)
                {
                    m.outvideofile = m.outfilepath;
                }
                else
                {
                    m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true) +
                        (m.format == Format.ExportFormats.BluRay ? "\\" + m.key : "") + //Кодирование в папку
                        ((!is_x262) ? ".264" : ".m2v");
                }
            }

            //Используем готовый файл, только если это не кодирование сразу в контейнер
            if ((muxer != Format.Muxers.Disabled || DontMuxStreams) && File.Exists(m.outvideofile) && new FileInfo(m.outvideofile).Length != 0)
            {
                SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile + Environment.NewLine);
                if (m.vpasses.Count >= 2) step += m.vpasses.Count - 1;
                return;
            }

            busyfile = Path.GetFileName(m.outvideofile);

            string passlog = Calculate.RemoveExtention(m.outvideofile) + "log";

            //info строка
            SetLog("Encoding video to: " + m.outvideofile);
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                SetLog(m.outvcodec + ((is_10bit) ? " 10-bit depth Q" : " Q") + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) +
                    " " + m.outresw + "x" + m.outresh + " " + m.outframerate + "fps (" + m.outframes + " frames)");
            //else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
            //    m.encodingmode == Settings.EncodingModes.ThreePassSize)
            //    SetLog(m.outvcodec + " " + m.outvbitrate + "mb " + m.outresw + "x" + m.outresh + " " +
            //        m.outframerate + "fps (" + m.outframes + " frames)");
            else
                SetLog(m.outvcodec + ((is_10bit) ? " 10-bit depth " : " ") + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " + m.outframerate + "fps (" + m.outframes + " frames)");

            if (m.vpasses.Count > 1) SetLog("\r\n...first pass...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            string in_depth = "";
            string executable = "";
            if (is_x262)
                info.FileName = Calculate.StartupPath + "\\apps\\x262\\x262.exe";
            else if (m.format == Format.ExportFormats.PmpAvc)
                info.FileName = Calculate.StartupPath + "\\apps\\x264_pmp\\x264.exe";
            else
            {
                if (bdepth != 8) in_depth = "--input-depth " + bdepth + " ";
                if (Settings.UseAVS4x264 || bdepth != 8) { executable = (SysInfo.GetOSArchInt() == 64) ? "-L x264_64.exe " : "-L x264.exe "; }
                info.FileName = Calculate.StartupPath + "\\apps\\" + ((is_10bit) ? "x264_10b\\" : "x264\\") + ((executable.Length > 0) ? "avs4x264.exe" : "x264.exe");
            }

            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string arguments; //начало создания командной строчки для x264-го

            string psnr = (!is_x262 && Settings.x264_PSNR || is_x262 && Settings.x262_PSNR) ? " --psnr" : "";
            string ssim = (!is_x262 && Settings.x264_SSIM || is_x262 && Settings.x262_SSIM) ? " --ssim" : "";

            string sar = "";
            if (!m.vpasses[m.vpasses.Count - 1].ToString().Contains("--sar "))
            {
                if (!string.IsNullOrEmpty(m.sar) && m.sar != "1:1")
                {
                    if (is_x262)
                    {
                        //Для x262 --sar = DAR, и возможны только стандартные для MPEG2 значения
                        if (Math.Abs(m.outaspect - 1.33) <= 0.01) sar = " --sar 4:3";
                        else if (Math.Abs(m.outaspect - 1.77) <= 0.01) sar = " --sar 16:9";
                        else if (Math.Abs(m.outaspect - 2.21) <= 0.01) sar = " --sar 221:100";
                        else sar = " --sar " + m.sar; //x262 покажет warning и будет использовать 1:1
                    }
                    else sar = " --sar " + m.sar;
                }
                else sar = " --sar 1:1";
            }

            //Добавляем новое
            for (int n = 0; n < m.vpasses.Count; n++)
                m.vpasses[n] = executable + in_depth + m.vpasses[n] + psnr + ssim + sar;

            arguments = m.vpasses[0].ToString();

            if (m.vpasses.Count == 1)
                info.Arguments = arguments + " --output \"" + m.outvideofile + "\" \"" + m.scriptpath + "\"";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                arguments += " --stats \"" + passlog + "\"";
                info.Arguments = arguments + " --output \"" + m.outvideofile + "\" \"" + m.scriptpath + "\"";
            }
            else
            {
                arguments += " --stats \"" + passlog + "\"";
                info.Arguments = arguments + " --output NUL \"" + m.scriptpath + "\"";
            }

            //Вывод аргументов командной строки в лог, если эта опция включена
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog(((!is_x262) ? ((executable.Length > 0) ? "avs4x264.exe: " : "x264.exe: ") : "x262.exe: ") + info.Arguments);
                SetLog("");
            }

            //Кодируем первый проход
            Do_x264_Cycle(info, is_x262);

            //второй проход
            if (m.vpasses.Count > 1 && !IsAborted && !IsErrors)
            {
                //режим двойного качества
                int vbitrate = 0;
                if (m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    double fsize = new FileInfo(m.outvideofile).Length;
                    fsize = fsize / 1024.0 / 1024.0;
                    vbitrate = Calculate.GetBitrateForSize(fsize, 0, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                    int maxbitrate = Format.GetMaxVBitrate(m);
                    SetLog(Languages.Translate("Best bitrate for") + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + ": " + vbitrate + "kbps");
                    if (vbitrate > maxbitrate)
                    {
                        vbitrate = maxbitrate;
                        SetLog(Languages.Translate("But it`s more than maximum bitrate") + ": " + vbitrate + "kbps");
                    }
                }

                if (m.vpasses.Count == 2) SetLog("...last pass...");
                else if (m.vpasses.Count == 3) SetLog("...second pass...");

                step++;
                arguments = m.vpasses[1] + " --stats \"" + passlog + "\"";

                if (m.vpasses.Count == 2)
                    info.Arguments = arguments + " --output \"" + m.outvideofile + "\" \"" + m.scriptpath + "\"";
                else
                    info.Arguments = arguments + " --output NUL \"" + m.scriptpath + "\"";

                //режим двойного качества
                if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                {
                    info.Arguments = info.Arguments.Replace("--crf " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "--bitrate " + vbitrate);
                    m.outvbitrate = vbitrate;
                }
                //режим тройного качества
                if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    info.Arguments = info.Arguments.Replace("--crf " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "--bitrate " + vbitrate);
                    m.vpasses[2] = m.vpasses[2].ToString().Replace("--crf " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "--bitrate " + vbitrate);
                    m.outvbitrate = vbitrate;
                }

                //прописываем аргументы командной строки
                SetLog("");
                if (Settings.ArgumentsToLog)
                {
                    SetLog(((!is_x262) ? ((executable.Length > 0) ? "avs4x264.exe: " : "x264.exe: ") : "x262.exe: ") + info.Arguments);
                    SetLog("");
                }

                //Кодируем второй проход
                Do_x264_Cycle(info, is_x262);
            }

            //третий проход
            if (m.vpasses.Count == 3 && !IsAborted && !IsErrors)
            {
                SetLog("...last pass...");

                step++;
                arguments = m.vpasses[2] + " --stats \"" + passlog + "\"";

                info.Arguments = arguments + " --output \"" + m.outvideofile + "\" \"" + m.scriptpath + "\"";

                //прописываем аргументы командной строки
                SetLog("");
                if (Settings.ArgumentsToLog)
                {
                    SetLog(((!is_x262) ? ((executable.Length > 0) ? "avs4x264.exe: " : "x264.exe: ") : "x262.exe: ") + info.Arguments);
                    SetLog("");
                }

                //Кодируем третий проход
                Do_x264_Cycle(info, is_x262);
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outvideofile) || new FileInfo(m.outvideofile).Length == 0)
            {
                IsErrors = true;
                ErrorException(Languages.Translate("Can`t find output video file!"));
            }

            SetLog("");

            SafeDelete(passlog);
            SafeDelete(passlog + ".mbtree");
        }

        private void Do_x264_Cycle(ProcessStartInfo info, bool is_x262)
        {
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            string pat = @"(\d+)/(\d+).frames,.(\d+.\d+).fps";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardError.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        if (encoder_fps >= 0) encoder_fps = Calculate.ConvertStringToDouble(mat.Groups[3].Value);
                    }
                    else
                    {
                        if (line.StartsWith("encoded"))
                        {
                            if (!is_x262) SetLog("x264 [total]: " + line);
                            else SetLog("x262 [total]: " + line);
                        }
                        else
                        {
                            //AppendEncoderText(line);
                            SetLog(line);
                        }
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) SetFilteredLog(r, line);
            SetLog("");

            //обнуляем прогресс
            ResetProgress();

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;

                //Незачем дважды повторять текст ошибки (всё, что есть в encodertext - уже
                //есть в логе, к тому-же в encodertext может не оказаться того, что было
                //считано в "Дочитываем остатки лога")
                //ErrorException(encodertext.ToString());

                SetLog(Languages.Translate("Error") + "!");
            }

            encodertext.Length = 0;
        }

        private void make_ffmpeg()
        {
            //FOURCC
            string fourcc = "";
            if (m.outvcodec == "HUFF" && m.ffmpeg_options.fourcc_huff != "FFVH")
            {
                fourcc = m.ffmpeg_options.fourcc_huff;
                for (int n = 0; n < m.vpasses.Count; n++)
                    m.vpasses[n] = m.vpasses[n].ToString().Replace(" -vtag " + fourcc, "");
            }

            //прописываем интерлейс флаги
            if (m.interlace == SourceType.FILM ||
                m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                m.interlace == SourceType.INTERLACED)
            {
                if (m.deinterlace == DeinterlaceType.Disabled)
                {
                    //порядок полей
                    int fieldorder = 1; //TopFieldFirst
                    if (m.fieldOrder == FieldOrder.BFF)
                        fieldorder = 0;

                    ArrayList newclis = new ArrayList();
                    foreach (string cli in m.vpasses)
                    {
                        string newclie = cli;

                        newclie = newclie + " -top " + fieldorder;
                        if (newclie.Contains("-flags "))
                            newclie = newclie.Replace("-flags ", "-flags +ildct+ilme");
                        else
                            newclie = newclie + " -flags +ildct+ilme";

                        newclis.Add(newclie);
                    }

                    //передаём обновленные параметры
                    m.vpasses = (ArrayList)newclis.Clone();
                }
            }

            //проверяем есть ли --size режим
            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                int targetsize = (int)m.outvbitrate;
                int outabitrate = 0;
                int bitrate = 0;

                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    outabitrate = outstream.bitrate;

                    if (File.Exists(outstream.audiopath))
                        bitrate = Calculate.GetBitrateForSize((double)targetsize, outstream.audiopath, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                    else
                        bitrate = Calculate.GetBitrateForSize((double)targetsize, outabitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                }
                else
                {
                    bitrate = Calculate.GetBitrateForSize((double)targetsize, outabitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                }

                m.outvbitrate = bitrate;

                ArrayList newclis = new ArrayList();
                foreach (string cli in m.vpasses)
                    newclis.Add(cli.Replace("-sizemode " + targetsize + "000", "-b " + bitrate + "000"));
                m.vpasses = (ArrayList)newclis.Clone();
            }

            step++;

            if (m.outvideofile == null)
            {
                if (DontMuxStreams)
                {
                    m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true);
                    if (m.format == Format.ExportFormats.BluRay)
                        m.outvideofile += "\\" + m.key; //Кодирование в папку
                    
                    if (m.outvcodec == "MPEG2")
                        m.outvideofile += ".m2v";
                    else if (m.outvcodec == "MPEG1")
                        m.outvideofile += ".m1v";
                    else
                        m.outvideofile += ".avi";
                }
                else
                {
                    if (m.outvcodec == "MPEG2")
                        m.outvideofile = Settings.TempPath + "\\" + m.key + ".m2v";
                    else if (m.outvcodec == "MPEG1")
                        m.outvideofile = Settings.TempPath + "\\" + m.key + ".m1v";
                    else
                        m.outvideofile = Settings.TempPath + "\\" + m.key + ".avi";
                }
            }

            //Используем готовый файл, только если это не кодирование сразу в контейнер
            if ((muxer != Format.Muxers.Disabled || DontMuxStreams) && File.Exists(m.outvideofile) && new FileInfo(m.outvideofile).Length != 0)
            {
                SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile + Environment.NewLine);
                if (m.vpasses.Count >= 2) step += m.vpasses.Count - 1;
                return;
            }

            busyfile = Path.GetFileName(m.outvideofile);

            string passlog1 = Calculate.RemoveExtention(m.outvideofile, true) + "_1";
            string passlog2 = Calculate.RemoveExtention(m.outvideofile, true) + "_2";

            //блок много процессорного кодирования
            int cpucount = Environment.ProcessorCount;

            //для кодеков без многопроцессорности
            if (m.outvcodec == "MJPEG" ||
                m.outvcodec == "FLV1")
                cpucount = 1;

            if (muxer != Format.Muxers.Disabled || DontMuxStreams)
            {
                //info строка для обычного кодирования
                SetLog("Encoding video to: " + m.outvideofile);
                if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.Quantizer ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    SetLog(m.outvcodec + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " " + m.outresw + "x" + m.outresh + " " +
                         m.outframerate + "fps (" + m.outframes + " frames)");
                else
                    SetLog(m.outvcodec + " " + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " + m.outframerate + "fps (" + m.outframes + " frames)");
            }
            else
            {
                //блок для кодирования в один присест
                SetLog("Encoding to: " + m.outfilepath);
                if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.Quantizer ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    SetLog(m.outvcodec + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " " + m.outresw + "x" + m.outresh + " " +
                         m.outframerate + "fps (" + m.outframes + " frames)");
                else
                    SetLog(m.outvcodec + " " + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " + m.outframerate + "fps (" + m.outframes + " frames)");

                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec == "MP3" && m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                        SetLog(outstream.codec + " Q" + m.mp3_options.quality + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
                    else if (outstream.codec == "FLAC")
                        SetLog(outstream.codec + " Q" + m.flac_options.level + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
                    else
                        SetLog(outstream.codec + " " + outstream.bitrate + "kbps " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
                }
            }

            if (m.vpasses.Count > 1) SetLog("\r\n...first pass...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            string arguments;

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            //фикс для частот которые не принимает ffmpeg
            if (m.outframerate == "23.976" ||
                m.outframerate == "29.970")
            {
                ArrayList newpass = new ArrayList();
                foreach (string p in m.vpasses)
                    newpass.Add("-r " + m.outframerate + " " + p);
                m.vpasses = (ArrayList)newpass.Clone();
            }

            //блок для кодирования в один присест
            string oldfilepath = m.outvideofile;
            if (muxer == Format.Muxers.Disabled && !DontMuxStreams)
            {
                m.outvideofile = m.outfilepath;
                if (m.outaudiostreams.Count > 0)
                {
                    //Перед объединением нужно убрать ключ -f из аудио cli.. 
                    string a_cli = Regex.Replace(((AudioStream)m.outaudiostreams[m.outaudiostream]).passes, @"(\s?\-f\s\w*)", "");
                    m.vpasses[m.vpasses.Count - 1] = m.vpasses[m.vpasses.Count - 1].ToString().Replace(" -an", "") + " " + a_cli.Trim();
                }
            }

            arguments = "-threads " + cpucount.ToString() + " " + m.vpasses[0];

            //прописываем аспект
            if (!string.IsNullOrEmpty(m.sar))
                arguments += " -aspect " + Calculate.ConvertDoubleToPointString(m.outaspect);

            //Доп. параметры муксинга
            string mux_o = "";
            if (muxer == Format.Muxers.Disabled && !DontMuxStreams && Formats.GetDefaults(m.format).IsEditable)
            {
                //Получение CLI и расшифровка подставных значений (только общие опции, т.к. тут нет аудио и видео файлов)
                string muxer_cli = Calculate.ExpandVariables(m, Formats.GetSettings(m.format, "CLI_ffmpeg", Formats.GetDefaults(m.format).CLI_ffmpeg));
                mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);
                mux_o = (!string.IsNullOrEmpty(mux_o)) ? " " + mux_o : "";
            }

            if (m.vpasses.Count == 1)
                info.Arguments = " -y -i \"" + m.scriptpath + "\" " + arguments + mux_o + " \"" + m.outvideofile + "\"";
            else
                info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 1 -passlogfile \"" + passlog1 + "\" " + arguments + " \"" + m.outvideofile + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //Кодируем первый проход
            Do_FFmpeg_Cycle(info);

            //второй проход
            if (m.vpasses.Count > 1 && !IsAborted && !IsErrors)
            {
                //режим двойного качества
                int vbitrate = 0;
                if (m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    double fsize = new FileInfo(m.outvideofile).Length;
                    fsize = fsize / 1024.0 / 1024.0;
                    vbitrate = Calculate.GetBitrateForSize(fsize, 0, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                    int maxbitrate = Format.GetMaxVBitrate(m);
                    SetLog(Languages.Translate("Best bitrate for") + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + ": " + vbitrate + "kbps");
                    if (vbitrate > maxbitrate)
                    {
                        vbitrate = maxbitrate;
                        SetLog(Languages.Translate("But it`s more than maximum bitrate") + ": " + vbitrate + "kbps");
                    }
                }

                if (m.vpasses.Count == 2) SetLog("...last pass...");
                else if (m.vpasses.Count == 3) SetLog("...second pass...");

                step++;
                arguments = "-threads " + cpucount.ToString() + " " + m.vpasses[1];

                //прописываем аспект
                if (!string.IsNullOrEmpty(m.sar))
                    arguments += " -aspect " + Calculate.ConvertDoubleToPointString(m.outaspect);

                if (m.vpasses.Count == 2)
                    info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 2 -passlogfile \"" + passlog1 + "\" " + arguments + mux_o + " \"" + m.outvideofile + "\"";
                else
                    info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 2 -passlogfile \"" + passlog1 + "\" " + arguments + " \"" + m.outvideofile + "\"";

                //режим двойного качества
                if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                {
                    info.Arguments = info.Arguments.Replace("-qscale " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "-b " + vbitrate + "k");
                    m.outvbitrate = vbitrate;
                }
                //режим тройного качества
                if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    info.Arguments = info.Arguments.Replace("-qscale " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "-b " + vbitrate + "k");
                    m.vpasses[2] = m.vpasses[2].ToString().Replace("-qscale " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "-b " + vbitrate + "k");
                    m.outvbitrate = vbitrate;
                }

                //прописываем аргументы командной строки
                SetLog("");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("ffmpeg.exe:" + " " + info.Arguments);
                    SetLog("");
                }

                //Кодируем второй проход
                Do_FFmpeg_Cycle(info);
            }

            //третий проход
            if (m.vpasses.Count == 3 && !IsAborted && !IsErrors)
            {
                SetLog("...last pass...");

                step++;
                arguments = "-threads " + cpucount.ToString() + " " + m.vpasses[2];

                //прописываем аспект
                if (!string.IsNullOrEmpty(m.sar))
                    arguments += " -aspect " + Calculate.ConvertDoubleToPointString(m.outaspect);

                info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 2 -passlogfile \"" + passlog1 + "\" " + arguments + mux_o + " \"" + m.outvideofile + "\"";

                //прописываем аргументы командной строки
                SetLog("");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("ffmpeg.exe:" + " " + info.Arguments);
                    SetLog("");
                }

                //Кодируем третий проход
                Do_FFmpeg_Cycle(info);
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outvideofile) || new FileInfo(m.outvideofile).Length == 0)
            {
                IsErrors = true;
                ErrorException(Languages.Translate("Can`t find output video file!"));
            }

            SafeDelete(passlog1 + "-0.log");
            SafeDelete(passlog2 + "-0.log");

            encodertext.Length = 0;
            SetLog("");

            if (IsAborted || IsErrors) return;

            //FOURCC
            if (!string.IsNullOrEmpty(fourcc) && fourcc.Length == 4 && Path.GetExtension(m.outvideofile).ToLower() == ".avi")
            {
                SetLog("FOURCC");
                SetLog("------------------------------");
                SetLog("FOURCC: FFVH > " + fourcc);
                SetLog("");
                
                //Не знаю, насколько корректно "вслепую" менять значения, но в "AVI FOURCC Code Changer" сделано именно так
                using (FileStream fs = new FileStream(m.outvideofile, FileMode.Open, FileAccess.Write))
                {
                    fs.Position = 112; //Description code
                    fs.Write(Encoding.ASCII.GetBytes(fourcc), 0, 4);
                    fs.Position = 188; //Used codec
                    fs.Write(Encoding.ASCII.GetBytes(fourcc), 0, 4);
                    fs.Flush();
                }

                SetLog("");
            }

            //возвращаем путь
            m.outvideofile = oldfilepath;
        }

        private void Do_FFmpeg_Cycle(ProcessStartInfo info)
        {
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            double fps = Calculate.ConvertStringToDouble(m.outframerate);
            Regex r = new Regex(@"time=(\d+.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            //первый проход
            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardError.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                    }
                    else
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //обнуляем прогресс
            ResetProgress();

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString() + encoderProcess.StandardError.ReadToEnd());
            }
        }

        private Thread readFromStdErrThread = null;
        private void readErrStream()
        {
            if (IsAborted || encoderProcess == null || encoderProcess.HasExited) return;
            using (StreamReader r = encoderProcess.StandardError)
            {
               AppendEncoderText(r.ReadToEnd());
            }
        }

        private void Do_XviD_Cycle(ProcessStartInfo info)
        {
            encodertext.Length = 0;
            encoderProcess.StartInfo = info;
            readFromStdErrThread = new Thread(new ThreadStart(readErrStream));
            encoderProcess.Start();
            readFromStdErrThread.Start();
            SetPriority(Settings.ProcessPriority);

            string line = "";
            string pat = @"(\d+):\Dkey=";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardOutput.ReadLine();
                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                        worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                    else
                    {
                        if (line != "" && !line.StartsWith("xvid_encraw"))
                            SetLog(line);
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) SetFilteredLog(r, line);
            SetLog("");

            //обнуляем прогресс
            ResetProgress();

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                Thread.Sleep(500);
                if (encodertext.Length > 0 && encodertext.ToString().Contains("Usage : xvid_encraw"))
                {
                    ErrorException("\r\nYou have specified an invalid command line key(s)/value(s).\r\n" +
                        "Check valid command line arguments in xvid_encraw.exe Help.");
                }
                else
                    ErrorException(encodertext.ToString());
            }

            //проверка на завершение
            if (!encoderProcess.HasExited)
            {
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
            }

            //StdErr
            if (readFromStdErrThread != null)
                readFromStdErrThread.Abort();
            readFromStdErrThread = null;
        }

        private void make_XviD()
        {
            //Версия XviD
            bool xvid_new = Settings.UseXviD_130;

            //Меняем ключи lumimasking/masking
            if (m.XviD_options.masking > 0)
            {
                for (int n = 0; n < m.vpasses.Count; n++)
                {
                    if (xvid_new)
                        m.vpasses[n] = m.vpasses[n].ToString().Replace("-lumimasking", "-masking 2");
                    else
                        m.vpasses[n] = m.vpasses[n].ToString().Replace("-masking " + m.XviD_options.masking, "-lumimasking");
                }
            }

            //Убираем -metric
            if (!xvid_new)
            {
                for (int n = 0; n < m.vpasses.Count; n++)
                    m.vpasses[n] = m.vpasses[n].ToString().Replace(" -metric " + m.XviD_options.metric, "");
            }

            //Проверяем путь к матрице
            if (m.XviD_options.qmatrix != "H263" && m.XviD_options.qmatrix != "MPEG")
            {
                for (int n = 0; n < m.vpasses.Count; n++)
                {
                    string qm_path = Calculate.GetRegexValue(@"\-qmatrix\s""(.+)""", m.vpasses[n].ToString());
                    if (qm_path != null && !File.Exists(qm_path))
                    {
                        string new_qm_path = Calculate.StartupPath + "\\presets\\matrix\\cqm\\" + Path.GetFileNameWithoutExtension(qm_path) + ".cqm";
                        if (File.Exists(new_qm_path))
                        {
                            m.vpasses[n] = m.vpasses[n].ToString().Replace("-qmatrix \"" + qm_path + "\"", "-qmatrix \"" + new_qm_path + "\"");
                        }
                    }
                }
            }

            //прописываем интерлейс флаги
            if (m.interlace == SourceType.FILM ||
                m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                m.interlace == SourceType.INTERLACED)
            {
                if (m.deinterlace == DeinterlaceType.Disabled)
                {
                    //(BFF:1, TFF:2) (1)
                    for (int n = 0; n < m.vpasses.Count; n++)
                        m.vpasses[n] = m.vpasses[n].ToString() + " -interlaced " + (m.fieldOrder == FieldOrder.TFF ? "2" : "1");
                }
            }

            //проверяем есть ли --size режим
            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                int targetsize = (int)m.outvbitrate;
                int outabitrate = 0;
                int bitrate = 0;

                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    outabitrate = outstream.bitrate;

                    if (File.Exists(outstream.audiopath))
                        bitrate = Calculate.GetBitrateForSize((double)targetsize, outstream.audiopath, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                    else
                        bitrate = Calculate.GetBitrateForSize((double)targetsize, outabitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                }
                else
                {
                    bitrate = Calculate.GetBitrateForSize((double)targetsize, outabitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                }

                m.outvbitrate = bitrate;
                for (int n = 0; n < m.vpasses.Count; n++)
                    m.vpasses[n] = m.vpasses[n].ToString().Replace("-size " + targetsize + "000", "-bitrate " + bitrate + "000");
            }

            //FOURCC
            if (m.XviD_options.fourcc != "XVID")
            {
                for (int n = 0; n < m.vpasses.Count; n++)
                    m.vpasses[n] = m.vpasses[n].ToString().Replace(" -fourcc " + m.XviD_options.fourcc, "");
            }

            step++;

            if (m.outvideofile == null) m.outvideofile = Settings.TempPath + "\\" + m.key + ".avi";

            //Если можно кодировать сразу в контейнер или не требуется муксить
            if (muxer == Format.Muxers.Disabled || DontMuxStreams)
            {
                m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true) + ".avi";
            }

            //Используем готовый файл, только если это не кодирование сразу в контейнер
            if ((muxer != Format.Muxers.Disabled || DontMuxStreams) && File.Exists(m.outvideofile) && new FileInfo(m.outvideofile).Length != 0)
            {
                SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile + Environment.NewLine);
                if (m.vpasses.Count >= 2) step += m.vpasses.Count - 1;
                return;
            }

            busyfile = Path.GetFileName(m.outvideofile);

            string passlog1 = Calculate.RemoveExtention(m.outvideofile, true) + "_1.log";
            string passlog2 = Calculate.RemoveExtention(m.outvideofile, true) + "_2.log";

            //Threads
            int cpucount = (m.vpasses[m.vpasses.Count - 1].ToString().Contains("-threads ")) ? 0 :
                (Settings.XviD_Threads == 0 ? Environment.ProcessorCount + 2 : Settings.XviD_Threads);

            //info строка
            SetLog("Encoding video to: " + m.outvideofile);
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                SetLog(m.outvcodec + (xvid_new ? " (1.3.x) Q" : " (1.2.2) Q") + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) +
                    " " + m.outresw + "x" + m.outresh + " " + m.outframerate + "fps (" + m.outframes + " frames)");
            //else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
            //     m.encodingmode == Settings.EncodingModes.ThreePassSize)
            //    SetLog(m.outvcodec + " " + m.outvbitrate + "mb " + m.outresw + "x" + m.outresh + " " +
            //        m.outframerate + "fps (" + m.outframes + " frames)");
            else
                SetLog(m.outvcodec + (xvid_new ? " (1.3.x) " : " (1.2.2) ") + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " + m.outframerate + "fps (" + m.outframes + " frames)");

            if (m.vpasses.Count > 1) SetLog("\r\n...first pass...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            string arguments;

            info.FileName = Calculate.StartupPath + "\\apps\\xvid_encraw" + (!xvid_new ? "\\1.2.2" : "") + "\\xvid_encraw.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            //прописываем sar
            string sar = "";
            if (!m.vpasses[m.vpasses.Count - 1].ToString().Contains("-par ") && !string.IsNullOrEmpty(m.sar))
                sar = " -par " + m.sar;
            //else
            //    sar = " -par 1:1";

            arguments = m.vpasses[0].ToString() + sar + ((cpucount == 0) ? "" : " -threads " + cpucount);

            if (m.vpasses.Count == 1)
                info.Arguments = arguments + " -avi \"" + m.outvideofile + "\" -i \"" + m.scriptpath + "\"";
            else if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                info.Arguments = arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";
            }
            else
            {
                arguments = "-pass1 \"" + passlog1 + "\" " + arguments;
                info.Arguments = arguments + " -i \"" + m.scriptpath + "\" -o NUL";
            }

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("xvid_encraw.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //Кодируем первый проход
            Do_XviD_Cycle(info);

            //второй проход
            if (m.vpasses.Count > 1 && !IsAborted && !IsErrors)
            {
                //режим двойного качества
                int vbitrate = 0;
                if (m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    double fsize = new FileInfo(m.outvideofile).Length;
                    fsize = fsize / 1024.0 / 1024.0;
                    vbitrate = Calculate.GetBitrateForSize(fsize, 0, (int)m.outduration.TotalSeconds, m.outvcodec, m.format);
                    int maxbitrate = Format.GetMaxVBitrate(m);
                    SetLog(Languages.Translate("Best bitrate for") + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + ": " + vbitrate + "kbps");
                    if (vbitrate > maxbitrate)
                    {
                        vbitrate = maxbitrate;
                        SetLog(Languages.Translate("But it`s more than maximum bitrate") + ": " + vbitrate + "kbps");
                    }
                    SetLog("");
                }

                if (m.vpasses.Count == 2) SetLog("...last pass...");
                else if (m.vpasses.Count == 3) SetLog("...second pass...");

                step++;
                arguments = m.vpasses[1].ToString() + sar + ((cpucount == 0) ? "" : " -threads " + cpucount);

                if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    info.Arguments = "-pass1 \"" + passlog1 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -o NUL";
                else if (m.vpasses.Count == 2)
                    info.Arguments = "-pass2 \"" + passlog1 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";
                else
                    info.Arguments = "-pass1 \"" + passlog2 + "\" -pass2 \"" + passlog1 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -o NUL";

                //режим двойного качества
                if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                {
                    info.Arguments = info.Arguments.Replace("-cq " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "-bitrate " + vbitrate);
                    m.outvbitrate = vbitrate;
                }
                //режим тройного качества
                if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    info.Arguments = info.Arguments.Replace("-cq " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "-bitrate " + vbitrate);
                    m.vpasses[2] = m.vpasses[2].ToString().Replace("-cq " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1), "-bitrate " + vbitrate);
                    m.outvbitrate = vbitrate;
                }

                //прописываем аргументы командной строки
                SetLog("");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("xvid_encraw.exe:" + " " + info.Arguments);
                    SetLog("");
                }

                //Кодируем второй проход
                Do_XviD_Cycle(info);
            }

            //третий проход
            if (m.vpasses.Count == 3 && !IsAborted && !IsErrors)
            {
                SetLog("...last pass...");

                step++;
                arguments = m.vpasses[2].ToString() + sar + ((cpucount == 0) ? "" : " -threads " + cpucount);

                if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    info.Arguments = "-pass2 \"" + passlog1 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";
                else
                    info.Arguments = "-pass2 \"" + passlog2 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";

                //прописываем аргументы командной строки
                SetLog("");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("xvid_encraw.exe:" + " " + info.Arguments);
                    SetLog("");
                }

                //Кодируем третий проход
                Do_XviD_Cycle(info);
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outvideofile) || new FileInfo(m.outvideofile).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }

            encodertext.Length = 0;
            SafeDelete(passlog1);
            SafeDelete(passlog2);

            SetLog("");

            //FOURCC
            if (m.XviD_options.fourcc != "XVID")
            {
                SetLog("FOURCC");
                SetLog("------------------------------");
                SetLog("FOURCC: XVID > " + m.XviD_options.fourcc);
                make_fourcc(m.XviD_options.fourcc);
            }
        }

        private void make_sound()
        {
            step++;

            if (m.format == Format.ExportFormats.Audio)
                SafeDelete(m.outfilepath);

            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //не делаем ничего со звуком если можно смонтировать напрямую
            //Если dontmuxstream, то все-же извлекаем звук сдесь, т.к. стадии муксинга не будет
            if (IsDirectRemuxingPossible && outstream.codec == "Copy" && !DontMuxStreams)
            {
                if (File.Exists(outstream.audiopath) && new FileInfo(outstream.audiopath).Length != 0)
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + outstream.audiopath + Environment.NewLine);
                }
                return;
            }

            //кодируем только звук
            if (m.vencoding == "Disabled") outstream.audiopath = m.outfilepath;

            string ext = Path.GetExtension(m.infilepath).ToLower();
            if (outstream.codec == "Copy" && ext == ".pmp") return;

            string aext;
            if (outstream.codec == "Copy")
                aext = Format.GetValidRAWAudioEXT(instream.codecshort);
            else
                aext = Format.GetValidRAWAudioEXT(outstream.codec);

            //if (outstream.audiopath == null)
            //    outstream.audiopath = Settings.TempPath + "\\" + m.key + aext;

            if (DontMuxStreams)
            {
                outstream.audiopath = Calculate.RemoveExtention(m.outfilepath, true) +
                    (m.format == Format.ExportFormats.BluRay ? "\\" + m.key : "") + //Кодирование в папку
                    Path.GetExtension(outstream.audiopath);
            }

            //подхватываем готовый файл
            if (outstream.codec == "Copy" && File.Exists(instream.audiopath))
            {
                if (DontMuxStreams)
                {
                    IsNoProgressStage = true;

                    //Копируем существующий файл в нужное место
                    SetLog(Languages.Translate("Using already created file") + ": " + instream.audiopath);
                    SetLog(Languages.Translate("Copying it to") + ": " + outstream.audiopath);
                    SetLog(Languages.Translate("Please wait") + "...\r\n");
                    File.Copy(instream.audiopath, outstream.audiopath);
                    return;
                }
                else
                    outstream.audiopath = instream.audiopath;
            }

            if (m.vencoding != "Disabled")
            {
                if (File.Exists(outstream.audiopath) && new FileInfo(outstream.audiopath).Length != 0)
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + outstream.audiopath + Environment.NewLine);
                    return;
                }
            }

            //Извлекаем звук для Copy
            if (outstream.codec == "Copy" && !File.Exists(instream.audiopath))
            {
                if (aext == ".wav")
                {
                    ExtractSound();
                }
                else
                {
                    Format.Demuxers dem = Format.GetDemuxer(m);
                    if (dem == Format.Demuxers.mkvextract) demux_mkv(Demuxer.DemuxerMode.ExtractAudio);
                    else if (dem == Format.Demuxers.pmpdemuxer) demux_pmp(Demuxer.DemuxerMode.ExtractAudio);
                    else if (dem == Format.Demuxers.mp4box) demux_mp4box(Demuxer.DemuxerMode.ExtractAudio);
                    else demux_ffmpeg(Demuxer.DemuxerMode.ExtractAudio);
                }
                return;
            }

            //Подменяем расширение для кодирования ААС (NeroEncAac кодирует в .m4a)
            if (Path.GetExtension(outstream.audiopath) == ".aac")
                outstream.audiopath = Calculate.RemoveExtention(outstream.audiopath, true) + ".m4a";

            busyfile = Path.GetFileName(outstream.audiopath);

            bool TwoPassAAC = false;
            SetLog("Encoding audio to: " + outstream.audiopath);
            if (outstream.codec == "MP3" && m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                SetLog(outstream.codec + " Q" + m.mp3_options.quality + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
            else if (outstream.codec == "FLAC")
                SetLog(outstream.codec + " Q" + m.flac_options.level + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
            else if (outstream.codec == "AAC" && m.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass)
            {
                SetLog(outstream.codec + " " + outstream.bitrate + "kbps 2pass " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
                TwoPassAAC = true;
            }
            else if (outstream.codec == "AAC" && m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                SetLog(outstream.codec + " Q" + m.aac_options.quality + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
            else if (outstream.codec == "QAAC" && m.qaac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                SetLog(outstream.codec + " Q" + m.qaac_options.quality + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
            else if (outstream.codec == "QAAC" && m.qaac_options.encodingmode == Settings.AudioEncodingModes.ALAC)
                SetLog(outstream.codec + " Lossless " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");
            else
                SetLog(outstream.codec + " " + outstream.bitrate + "kbps " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz");

            //удаляем старый файл
            SafeDelete(outstream.audiopath);
               
            //создаём кодер
            if (outstream.codec == "LPCM" && outstream.channels > 2 ||
                outstream.codec == "PCM" && outstream.channels > 2)
            {
                make_ffmpeg_audio(); //прямое кодирование через ffmpeg (не совсем понятно, зачем оно нужно)
            }
            else if (TwoPassAAC)
            {
                two_pass_aac(); //Двухпроходный AAC
            }
            else
            {
                avs = new AviSynthEncoder(m);

                if (outstream.codec == "AAC")
                {
                    if (m.format == Format.ExportFormats.PmpAvc)
                        avs.encoderPath = Calculate.StartupPath + "\\apps\\neroAacEnc_pmp\\neroAacEnc.exe";
                    else
                        avs.encoderPath = Calculate.StartupPath + "\\apps\\neroAacEnc\\neroAacEnc.exe";
                }
                else if (outstream.codec == "QAAC")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\qaac\\qaac.exe";
                else if (outstream.codec == "MP3")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\lame\\lame.exe";
                else if (outstream.codec == "MP2" || outstream.codec == "PCM" || outstream.codec == "LPCM" || outstream.codec == "FLAC")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                else if (outstream.codec == "AC3")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\aften\\aften.exe";

                //запускаем кодер
                avs.start();

                //прописываем аргументы командной строки
                while (avs.args == null && !avs.IsErrors) Thread.Sleep(50);
                SetLog("");

                if (Settings.ArgumentsToLog)
                {
                    SetLog(Path.GetFileName(avs.encoderPath) + ": " + avs.args);
                    SetLog("");
                }

                //Вывод прогресса кодирования
                while (avs.IsBusy())
                {
                    worker.ReportProgress(avs.frame);
                    Thread.Sleep(100);
                }

                if (avs.IsErrors)
                {
                    string error_txt = avs.error_text;
                    avs = null;
                    throw new Exception(error_txt);
                }

                //обнуляем прогресс
                ResetProgress();

                //чистим ресурсы
                avs = null;
            }

            SetLog("");

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outstream.audiopath) || new FileInfo(outstream.audiopath).Length == 0)
            {
                throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }

            //Похоже, что tsMuxeR уже умеет принимать m4a на вход..
            //if (muxer == Format.Muxers.tsmuxer && (outstream.codec == "AAC" || outstream.codec == "QAAC"))
            //    make_aac();
        }

        private void two_pass_aac()
        {
            //определяем аудио потоки
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            if (m.format == Format.ExportFormats.PmpAvc)
                info.FileName = Calculate.StartupPath + "\\apps\\neroAacEnc_pmp\\neroAacEnc.exe";
            else
                info.FileName = Calculate.StartupPath + "\\apps\\neroAacEnc\\neroAacEnc.exe";

            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            info.Arguments = outstream.passes + " -if \"" + outstream.nerotemp + "\" -of \"" + outstream.audiopath + "\"";

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("neroAacEnc.exe: " + info.Arguments + "\r\n");
            }

            string line;
            int oldpass = 0;
            double fps = Calculate.ConvertStringToDouble(m.outframerate);
            Regex r = new Regex(@"(\w+).+sed.(\d+).sec", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardError.ReadLine();
                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        if (oldpass == 0 && mat.Groups[1].Value == "First")
                        {
                            SetLog("...first pass...");
                            oldpass = 1;
                        }
                        else if (oldpass == 1 && mat.Groups[1].Value == "Second")
                        {
                            SetLog("...last pass...");
                            ResetProgress();
                            oldpass = 2;
                            step++;
                        }
                        worker.ReportProgress((int)(Convert.ToInt32(mat.Groups[2].Value) * fps));
                    }
                    else
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //обнуляем прогресс
            ResetProgress();

            //Удаляем временный WAV-файл
            if (outstream.nerotemp != m.infilepath && Path.GetDirectoryName(outstream.nerotemp) == Settings.TempPath)
                SafeDelete(outstream.nerotemp);

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.ExitCode != 0 && !IsAborted)
            {
                ErrorException(encodertext.ToString() + "\r\n" + encoderProcess.StandardError.ReadToEnd() + "\r\n" + encoderProcess.StandardOutput.ReadToEnd());
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            encodertext.Length = 0;
        }

        private void ExtractSound()
        {
            //определяем аудио потоки
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            SetLog("Decoding audio stream to: " + outstream.audiopath + "\r\n");

            //удаляем старый файл
            SafeDelete(outstream.audiopath);

            //создаём кодер
            avs = new AviSynthEncoder(m);

            //запускаем кодер
            avs.start();
            
            //извлекаем кусок на ext_frames фреймов
            while (avs.IsBusy())
            {
                worker.ReportProgress(avs.frame);
                Thread.Sleep(100);
            }

            //Ловим ошибки, если они были
            if (avs.IsErrors)
            {
                ErrorException(avs.error_text);
            }
            else
            {
                //Задержка уже была в скрипте, и звук декодировался вместе с ней
                CopyDelay = false;
            }

            //чистим ресурсы
            avs = null;
        }

        private void demux_ffmpeg(Demuxer.DemuxerMode dmode)
        {
            //if (m.infileslist.Length > 1)
            //    ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
            //        Languages.Translate("Warning"));

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            string format = "";
            string outfile = "";

            if (dmode == Demuxer.DemuxerMode.ExtractAudio)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                busyfile = Path.GetFileName(outstream.audiopath);

                outfile = outstream.audiopath;

                string forceformat = "";
                string outext = Path.GetExtension(outfile);
                if (outext == ".lpcm" || instream.codec == "LPCM")
                    forceformat = " -f s16be";

                info.Arguments = "-map 0." + instream.ff_order + " -i \"" + m.infilepath +
                    "\" -vn -acodec copy" + forceformat  + " \"" + outfile + "\"";

                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
            }
            else if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                step++;

                if (m.outvideofile == null)
                    m.outvideofile = Settings.TempPath + "\\" + m.key + "." + Format.GetValidRAWVideoEXT(m);

                //подхватываем готовый файл
                if (File.Exists(m.outvideofile))
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile + Environment.NewLine);
                    return;
                }

                busyfile = Path.GetFileName(m.outvideofile);

                SetLog("Demuxing video stream to: " + m.outvideofile);
                SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                    Environment.NewLine);

                string outext = Path.GetExtension(m.outvideofile).ToLower();
                if (outext == ".264" || outext == ".h264")
                    format = " -f h264";

                //if (outext == ".m2v")
                //    format = " -f vob";

                outfile = m.outvideofile;
                info.Arguments = "-i \"" + m.infilepath + "\" -vcodec copy -an" + format + " \"" + outfile + "\"";
            }

            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe: " + info.Arguments + Environment.NewLine);
            }

            //удаляем старый файл
            SafeDelete(outfile);

            int outframes = m.outframes;
            m.outframes = m.inframes;
            SetMaximum(m.outframes);

            //Демуксим
            Do_FFmpeg_Cycle(info);

            m.outframes = outframes;
            SetMaximum(m.outframes);

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }

            encodertext.Length = 0;
        }

        private void demux_mp4box(Demuxer.DemuxerMode dmode)
        {
            //if (m.infileslist.Length > 1)
            //    ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
            //        Languages.Translate("Warning"));

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\MP4Box.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string outfile = "";

            if (dmode == Demuxer.DemuxerMode.ExtractAudio)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                busyfile = Path.GetFileName(outstream.audiopath);

                outfile = outstream.audiopath;
                info.Arguments = "-raw " + instream.mi_id + " \"" + m.infilepath + "\" -out \"" + outfile + "\"";

                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
            }
            else if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                step++;

                if (m.outvideofile == null)
                    m.outvideofile = Settings.TempPath + "\\" + m.key + "." + Format.GetValidRAWVideoEXT(m);

                //подхватываем готовый файл
                if (File.Exists(m.outvideofile))
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile + Environment.NewLine);
                    return;
                }

                busyfile = Path.GetFileName(m.outvideofile);

                SetLog("Demuxing video stream to: " + m.outvideofile);
                SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                    Environment.NewLine);

                outfile = m.outvideofile;
                info.Arguments = "-raw " + m.invideostream_mi_id + " \"" + m.infilepath + "\" -out \"" + outfile + "\"";
            }

            if (Settings.ArgumentsToLog)
            {
                SetLog("mp4box.exe: " + info.Arguments + Environment.NewLine);
            }

            //удаляем старый файл
            SafeDelete(outfile);

            int outframes = m.outframes;
            m.outframes = m.inframes;
            SetMaximum(m.outframes);

            //Извлекаем
            Do_MP4Box_Cycle(info);

            m.outframes = outframes;
            SetMaximum(m.outframes);

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }

            encodertext.Length = 0;
        }

        private void demux_pmp(Demuxer.DemuxerMode dmode)
        {
            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //if (m.infileslist.Length > 1)
            //    ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
            //        Languages.Translate("Warning"));

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\pmp_muxer_avc\\pmp_demuxer.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            if (dmode == Demuxer.DemuxerMode.ExtractVideo)
                step++;

            busyfile = Path.GetFileName(m.infilepath);
            info.Arguments = "\"" + m.infilepath + "\"";

            m.outvideofile = m.infilepath + ".avi";
            outstream.audiopath = m.infilepath + ".1.aac";

            SetLog("Demuxing video stream to: " + m.outvideofile);
            SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                Environment.NewLine);

            if (outstream.codec == "Copy")
            {
                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
            }

            if (Settings.ArgumentsToLog)
            {
                SetLog("pmp_demuxer.exe: " + info.Arguments + Environment.NewLine);
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"(\d+)\D/\D(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            int outframes = m.outframes;
            m.outframes = m.inframes;
            SetMaximum(m.outframes);
            //первый проход
            while (!encoderProcess.HasExited)
            {
                line = encoderProcess.StandardOutput.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                    }
                    else
                    {
                        AppendEncoderText(line);
                    }
                }
            }
            m.outframes = outframes;
            SetMaximum(m.outframes);

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            if (!File.Exists(outstream.audiopath))
            {
                instream.codec = "MP3";
                outstream.audiopath = m.infilepath + ".1.mp3";
            }

            //проверка на удачное завершение
            if (!File.Exists(m.outvideofile) ||
                new FileInfo(m.outvideofile).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }

            if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                if (outstream.codec == "Copy")
                {
                    if (new FileInfo(outstream.audiopath).Length == 0)
                    {
                        IsErrors = true;
                        ErrorException(encodertext.ToString());
                    }
                }
                else
                    File.Delete(outstream.audiopath);
            }

            encodertext.Length = 0;

            //вытягиваем RAW h264 из AVI
            string old_infilepath = m.infilepath;
            m.infilepath = m.outvideofile;
            m.outvideofile = null;
            demux_ffmpeg(Demuxer.DemuxerMode.ExtractVideo);
            SafeDelete(m.infilepath);
            m.infilepath = old_infilepath;
            SafeDelete(m.infilepath + ".log");
        }

        private void demux_mkv(Demuxer.DemuxerMode dmode)
        {
           //список файлов
            string flist = "";
            foreach (string _file in m.infileslist)
                flist += "\"" + _file + "\" ";

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvextract.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = false;
            info.CreateNoWindow = true;

            string outfile = "";
            string charset = "";
            string _charset = Settings.MKVToolnix_Charset;

            try
            {
                if (_charset == "")
                {
                    info.StandardOutputEncoding = System.Text.Encoding.UTF8;
                    charset = " --output-charset UTF-8";
                }
                else if (_charset.ToLower() == "auto")
                {
                    info.StandardOutputEncoding = System.Text.Encoding.Default;
                    charset = " --output-charset " + System.Text.Encoding.Default.HeaderName;
                }
                else
                {
                    int page = 0;
                    if (int.TryParse(_charset, out page))
                        info.StandardOutputEncoding = System.Text.Encoding.GetEncoding(page);
                    else
                        info.StandardOutputEncoding = System.Text.Encoding.GetEncoding(_charset);
                    charset = " --output-charset " + _charset;
                }
            }
            catch (Exception) { }

            if (dmode == Demuxer.DemuxerMode.ExtractAudio)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                busyfile = Path.GetFileName(outstream.audiopath);

                outfile = outstream.audiopath;
                info.Arguments = "tracks " + flist + instream.mi_order + ":" + "\"" + outfile + "\"" + charset;

                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
            }
            else if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                step++;

                if (m.outvideofile == null)
                    m.outvideofile = Settings.TempPath + "\\" + m.key + "." + Format.GetValidRAWVideoEXT(m);

                //подхватываем готовый файл
                if (File.Exists(m.outvideofile))
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile + Environment.NewLine);
                    return;
                }

                busyfile = Path.GetFileName(m.outvideofile);

                outfile = m.outvideofile;
                info.Arguments = "tracks " + flist + m.invideostream_mi_order + ":" + "\"" + outfile + "\"" + charset;

                SetLog("Demuxing video stream to: " + m.outvideofile);
                SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                    Environment.NewLine);
            }

            if (Settings.ArgumentsToLog)
            {
                SetLog("mkvextract.exe: " + info.Arguments + Environment.NewLine);
            }

            //удаляем старый файл
            SafeDelete(outfile);

            int outframes = m.outframes;
            m.outframes = m.inframes;
            SetMaximum(m.outframes);

            //Извлекаем
            Do_MKVToolnix_Cycle(info);

            m.outframes = outframes;
            SetMaximum(m.outframes);

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }

            encodertext.Length = 0;
        }

        private void make_mp4()
        {
            //заглушка, PSP не читает видео без звука
            if (m.format == Format.ExportFormats.Mp4PSPAVC ||
                m.format == Format.ExportFormats.Mp4PSPAVCTV ||
                m.format == Format.ExportFormats.Mp4PSPASP)
            {
                if (m.outaudiostreams.Count == 0)
                {
                    AudioStream stream = new AudioStream();
                    stream.audiopath = Settings.TempPath + "\\" + m.key + ".m4a";
                    File.Copy(Calculate.StartupPath + "\\apps\\pmp_muxer_avc\\fake.m4a",
                        stream.audiopath);
                    m.outaudiostreams.Add(stream);
                }
            }

            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //info строка
            SetLog("Video file: " + m.outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                    SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            bool old_muxer = false; //Использование старого NicMP4Box в случае его наличия - необходим для воспроизведения на Иподе файлов с большим разрешением
            if (m.format == Format.ExportFormats.Mp4iPod55G && (/*m.outvcodec == "x264" || */m.outvcodec == "Copy") && File.Exists(Calculate.StartupPath + "\\apps\\MP4Box\\NicMP4Box.exe"))
            {
                old_muxer = true;
                info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\NicMP4Box.exe";
            }
            else
                info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\MP4Box.exe";

            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = false;
            info.CreateNoWindow = true;

            string rate = "-fps " + m.outframerate + " ";
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate != null)
                    rate = "-fps " + m.inframerate + " ";
                else
                    rate = "";
            }

            //Доп. параметры муксинга
            string mux_v = "", mux_a = "", mux_o = "";
            if (!old_muxer && (m.format == Format.ExportFormats.Mp4iPod50G || m.format == Format.ExportFormats.Mp4iPod55G))
            {
                //пробный агрумент для ипода (для больших разрешений и 5.5G его все-равно не достаточно)
                mux_o = "-ipod ";
            }
            else if (Formats.GetDefaults(m.format).IsEditable)
            {
                //Получение CLI и расшифровка подставных значений
                string muxer_cli = Calculate.ExpandVariables(m, Formats.GetSettings(m.format, "CLI_mp4box", Formats.GetDefaults(m.format).CLI_mp4box));
                mux_v = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", muxer_cli);
                mux_a = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", muxer_cli);
                mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);
                mux_o = (!string.IsNullOrEmpty(mux_o)) ? mux_o + " " : "";
            }

            string split = "";
            if (!string.IsNullOrEmpty(Splitting) && Splitting != "Disabled")
            {
                int size = 0;
                string svalue = Calculate.GetRegexValue(@"(\d+)\D*", Splitting);
                if (svalue != null) Int32.TryParse(svalue, NumberStyles.Integer, null, out size);
                if (size != 0) { split = "-split-size " + size + "000 "; IsSplitting = true; }
            }

            string addaudio = "";
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                addaudio = " -add \"" + outstream.audiopath + "\"" + mux_a + (CopyDelay ? " -delay 2=" + outstream.delay : "");
            }

            info.Arguments = mux_o + rate + split + "-add \"" + m.outvideofile + "\"" + mux_v + addaudio + " -new \"" + m.outfilepath +
                "\" -tmp \"" + Settings.TempPath + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog((old_muxer ? "NicMP4Box.exe: " : "MP4Box.exe: ") + info.Arguments);
                SetLog("");
            }

            //Муксим
            Do_MP4Box_Cycle(info);

            //обнуляем прогресс
            ResetProgress();

            if (IsAborted || IsErrors) return;

            //если включено деление
            if (IsSplitting) m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + "_001" + Path.GetExtension(m.outfilepath).ToLower();

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) || new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext.Length = 0;

            SetLog("");
        }

        private void Do_MP4Box_Cycle(ProcessStartInfo info)
        {
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            string pat = @"(\d+)/(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            int procent = 0;

            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardOutput.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) AppendEncoderText(Calculate.FilterLogMessage(r, line));

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;
        }

        private void make_aac()
        {
            if (m.outaudiostreams.Count == 0 ||
                m.inaudiostreams.Count == 0)
                return;

            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            string aacfile = Calculate.RemoveExtention(outstream.audiopath, true) + ".aac";

            SafeDelete(aacfile);

            busyfile = Path.GetFileName(aacfile);
            step++;

            //info строка
            SetLog("M4A > AAC");
            SetLog("------------------------------");
            SetLog("Remuxing to: " + aacfile);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\MP4Box.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            info.Arguments = "-raw 1 \"" + outstream.audiopath + "\" -out \"" + aacfile + "\" -tmp \"" + Settings.TempPath + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("mp4box.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //Извлекаем
            Do_MP4Box_Cycle(info);

            //обнуляем прогресс
            ResetProgress();

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(aacfile) || new FileInfo(aacfile).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }
            else
            {
                //передаём новый файл
                SafeDelete(outstream.audiopath);
                outstream.audiopath = aacfile;
            }

            encodertext.Length = 0;

            SetLog("");
        }

        private void make_dpg()
        {
            SafeDelete(m.outfilepath);

            //info строка
            SetLog("Video file: " + m.outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                    SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);

            ////прописываем аргументы командной строки
            //if (Settings.ArgumentsToLog)
            //    SetLog(info.Arguments);

            
            dpgmuxer muxer = new dpgmuxer();
            muxer.ProgressChanged += new dpgmuxer.ProgressChangedDelegate(muxer_ProgressChanged);
            muxer.MuxStreams(m);
            muxer.ProgressChanged -= new dpgmuxer.ProgressChangedDelegate(muxer_ProgressChanged);

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            SetLog("");
        }

        private void muxer_ProgressChanged(double progress)
        {
            //получаем текущий фрейм
            frame = (int)((progress / 100.0) * (double)m.outframes);
        }

        private void make_ffmpeg_mux()
        {
            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //info строка
            SetLog("Video file: " + m.outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                {
                    if (!File.Exists(outstream.audiopath) && outstream.codec == "Copy")
                        SetLog("Audio file: " + m.infilepath);
                    else
                        SetLog("Audio file: " + outstream.audiopath);
                }
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            //Доп. параметры муксинга
            string mux_v = "", mux_a = "", mux_o = "";
            if (m.format == Format.ExportFormats.Mp4iPod55G)
            {
                //Добавляем ipod atom для 5.5G
                mux_o = " -f ipod";
            }
            else if (Formats.GetDefaults(m.format).IsEditable)
            {
                //Получение CLI и расшифровка подставных значений
                string muxer_cli = Calculate.ExpandVariables(m, Formats.GetSettings(m.format, "CLI_ffmpeg", Formats.GetDefaults(m.format).CLI_ffmpeg));
                mux_v = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", muxer_cli);
                mux_a = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", muxer_cli);
                mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);

                mux_v = (!string.IsNullOrEmpty(mux_v)) ? " " + mux_v : "";
                mux_a = (!string.IsNullOrEmpty(mux_a)) ? " " + mux_a : "";
                mux_o = (!string.IsNullOrEmpty(mux_o)) ? " " + mux_o : "";
            }

            string rate = " -r " + m.outframerate;
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate != null)
                    rate = " -r " + m.inframerate;
                else
                    rate = "";
            }

            string addaudio = "";
            if (m.outaudiostreams.Count > 0)
            {
                string aformat = "";
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (CopyDelay)
                {
                    addaudio = " -ss " + TimeSpan.FromMilliseconds(outstream.delay * -1.0).ToString();
                    if (addaudio.Contains(".")) addaudio = addaudio.Remove(addaudio.Length - 4, 4);
                }
                if (!File.Exists(outstream.audiopath)) addaudio += " -i \"" + m.infilepath + "\"" + aformat + " -acodec copy" + mux_a;
                else addaudio += " -i \"" + outstream.audiopath + "\"" + aformat + " -acodec copy" + mux_a;
            }

            info.Arguments = "-i \"" + m.outvideofile + "\" -vcodec copy" + mux_v + addaudio + mux_o + rate + " \"" + m.outfilepath + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //Муксим
            Do_FFmpeg_Cycle(info);

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) || new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext.Length = 0;
            SetLog("");
        }

        private void make_ffmpeg_audio()
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            info.Arguments = "-i \"" + m.scriptpath + "\" " + outstream.passes + " -vn \"" + outstream.audiopath + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //Кодируем
            Do_FFmpeg_Cycle(info);

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outstream.audiopath) || new FileInfo(outstream.audiopath).Length == 0)
            {
                throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }

            encodertext.Length = 0;
        }

        private void make_tsmuxer()
        {
            SafeDelete(m.outfilepath);

            if (m.format == Format.ExportFormats.BluRay)
            {
                //проверяем есть ли файлы в папке
                if (Calculate.GetFolderSize(m.outfilepath) != 0)
                {
                    if (Directory.Exists(m.outfilepath + "\\BDMV"))
                        Directory.Delete(m.outfilepath + "\\BDMV", true);
                    if (Directory.Exists(m.outfilepath + "\\CERTIFICATE"))
                        Directory.Delete(m.outfilepath + "\\CERTIFICATE", true);
                }
            }

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //info строка
            SetLog("Video file: " + m.outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                {
                    if (!File.Exists(outstream.audiopath) && outstream.codec == "Copy")
                        SetLog("Audio file: " + m.infilepath);
                    else
                        SetLog("Audio file: " + outstream.audiopath);
                }
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\tsMuxeR\\tsMuxeR.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            //создаём мета файл
            string metapath = Settings.TempPath + "\\" + m.key + ".meta";
            string vcodec = (m.outvcodec == "Copy") ? m.invcodecshort : m.outvcodec;
            string vtag = (vcodec == "MPEG2" || vcodec == "x262") ? "V_MPEG-2" : (vcodec == "VC1") ? "V_MS/VFW/WVC1" : "V_MPEG4/ISO/AVC";
            string fps = (m.outvcodec == "Copy") ? ((!string.IsNullOrEmpty(m.inframerate)) ? ", fps=" + m.inframerate : "") : (!string.IsNullOrEmpty(m.outframerate)) ? ", fps=" + m.outframerate : "";
            string h264tweak = (vtag == "V_MPEG4/ISO/AVC") ? ", level=4.1, insertSEI, contSPS" : "";
            string bluray = (m.format == Format.ExportFormats.BluRay) ? " --blu-ray --auto-chapters=5" : "";

            string split = "";
            if (m.format == Format.ExportFormats.BluRay)
            {
                if (Settings.BluRayType == "FAT32 HDD/MS")
                    split = " --split-size=4000MB";
            }
            else
            {
                if (!string.IsNullOrEmpty(Splitting) && Splitting != "Disabled")
                {
                    int size = 0;
                    string svalue = Calculate.GetRegexValue(@"(\d+)\D*", Splitting);
                    if (svalue != null) Int32.TryParse(svalue, NumberStyles.Integer, null, out size);
                    if (size != 0) { split = " --split-size=" + size + "MB"; IsSplitting = true; }
                }
            }

            //контрольная проверка на FAT32
            if (split == "")
            {
                DriveInfo dinfo = new DriveInfo(Path.GetPathRoot(m.outfilepath));
                if (dinfo.DriveFormat == "FAT32")
                {
                    split = " --split-size=4000MB";
                    Splitting = "4000 mb FAT32";
                    SetLog("FAT32 detected. Auto 4GB split activated.");
                    IsSplitting = true;
                }
            }

            //video path
            string vpath = m.outvideofile, vtrack = "";
            if (m.outvcodec == "Copy" && IsDirectRemuxingPossible)
            {
                vpath = m.infilepath;
                //if (ext == ".mkv")
                vtrack = ", track=" + m.invideostream_mi_id;
                //else
                //    vtrack = ", track=" + m.invideostream_ff_order;
            }
            else if (Path.GetExtension(m.outvideofile).ToLower() == ".mp4")
            {
                vtrack = ", track=1";
            }

            //Доп. параметры муксинга
            string mux_v = "", mux_a = "", mux_o = "";
            if (Formats.GetDefaults(m.format).IsEditable)
            {
                //Получение CLI и расшифровка подставных значений
                string muxer_cli = Calculate.ExpandVariables(m, Formats.GetSettings(m.format, "CLI_tsmuxer", Formats.GetDefaults(m.format).CLI_tsmuxer));
                mux_v = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", muxer_cli);
                mux_a = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", muxer_cli);
                mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);

                mux_v = (!string.IsNullOrEmpty(mux_v)) ? ", " + mux_v : "";
                mux_a = (!string.IsNullOrEmpty(mux_a)) ? ", " + mux_a : "";
                mux_o = (!string.IsNullOrEmpty(mux_o)) ? " " + mux_o : "";
            }

            //Параметры мукса
            string meta = "";
            meta += "MUXOPT" + mux_o + bluray + split + Environment.NewLine;                               //meta - общие
            meta += vtag + ", \"" + vpath + "\"" + mux_v + fps + h264tweak + vtrack + Environment.NewLine; //meta - video

            if (m.outaudiostreams.Count > 0)
            {
                AudioStream i = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream o = (AudioStream)m.outaudiostreams[m.outaudiostream];

                string acodec = (o.codec == "Copy") ? i.codecshort : o.codec;

                string atag = "A_AC3";
                if (acodec == "DTS") atag = "A_DTS";
                else if (acodec == "MP2" || acodec == "MP3") atag = "A_MP3";
                else if (acodec == "AAC" || acodec == "QAAC") atag = "A_AAC";
                else if (acodec == "PCM" || acodec == "LPCM") atag = "A_LPCM";

                //audio path
                string apath = o.audiopath, atrack = "";
                if (File.Exists(o.audiopath))
                {
                    apath = o.audiopath;
                    if (Path.GetExtension(o.audiopath).ToLower() == ".m4a")
                        atrack = ", track=1";
                    else
                        atrack = ", track=0";
                }
                else
                {
                    if (o.codec == "Copy" && IsDirectRemuxingPossible)
                    {
                        apath = m.infilepath;
                        //if (ext == ".mkv")
                        atrack = ", track=" + i.mi_id;
                        //else
                        //    atrack = ", track=" + i.ff_order;
                    }
                }

                if (CopyDelay) atrack += ", timeshift=" + o.delay + "ms";

                meta += atag + ", \"" + apath + "\"" + mux_a + atrack; //meta - audio
            }

            //пишем meta в файл
            File.WriteAllText(metapath, meta, System.Text.Encoding.Default);

            info.Arguments = "\"" + metapath + "\" \"" + m.outfilepath + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("tsmuxer.exe:" + " " + info.Arguments);
                SetLog(meta);
                SetLog("");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            string pat = @"(\d+.\d+)%.complete";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardOutput.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        double procent = Calculate.ConvertStringToDouble(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * (double)m.outframes));
                    }
                    else
                    {
                        if (Settings.ArgumentsToLog)
                            SetLog(line);
                    }
                }
            }

            //обнуляем прогресс
            ResetProgress();

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorException(encoderProcess.StandardError.ReadToEnd());
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //если включено деление
            if (IsSplitting && m.format != Format.ExportFormats.BluRay)
                m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + ".split.1" + Path.GetExtension(m.outfilepath).ToLower();

            //проверка на удачное завершение
            if (m.format == Format.ExportFormats.BluRay)
            {
                if (!Directory.Exists(m.outfilepath) ||
                    Calculate.GetFolderSize(m.outfilepath) == 0)
                {
                    IsErrors = true;
                    ErrorException(encodertext.ToString());
                }
            }
            else
            {
                if (!File.Exists(m.outfilepath) ||
                    new FileInfo(m.outfilepath).Length == 0)
                {
                    IsErrors = true;
                    ErrorException(encodertext.ToString());
                    //throw new Exception(Languages.Translate("Can`t find output video file!"));
                }
            }

            encodertext.Length = 0;

            //удаляем мета файл
            SafeDelete(metapath);

            //правим BluRay структуру для флешек и FAT32 дисков
            if (m.format == Format.ExportFormats.BluRay && Settings.BluRayType == "FAT32 HDD/MS")
            {
                IsNoProgressStage = true;
                Calculate.MakeFAT32BluRay(m.outfilepath);
            }

            SetLog("");
        }

        private void make_avc2avi()
        {
            IsNoProgressStage = true;

            string aviout = Calculate.RemoveExtention(m.outvideofile, true) + ".avi";
            busyfile = Path.GetFileName(aviout);

            //info строка
            SetLog("Video file: " + m.outvideofile);
            SetLog("Rebuild to: " + aviout);
            SetLog(Languages.Translate("Please wait") + "...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\avc2avi\\avc2avi.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string rate = m.outframerate;
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate != null)
                    rate = m.inframerate;
                else
                    rate = "";
            }

            //поправка fps для avc2avi, который занижает значение для 23.976 и 29.970 фпс на 0.001
            if (rate != "")
            {
                if (rate == "23.976") rate = "23.9761";
                else if (rate == "29.970") rate = "29.9701";
                else if (rate == "59.940") rate = "59.9401";

                rate = " -f " + rate;
            }
            info.Arguments = "-i \"" + m.outvideofile + "\" -o \"" + aviout + "\"" + rate;

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("avc2avi.exe:" + " " + info.Arguments);
                SetLog("");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);
            encoderProcess.WaitForExit();

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(aviout) ||
                new FileInfo(aviout).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            //передаём файл
            if (!IsErrors)
            {
                SafeDelete(m.outvideofile);
                m.outvideofile = aviout;
            }
        }

        private void make_pulldown()
        {
            IsNoProgressStage = true;
            busyfile = Path.GetFileName(m.outvideofile);

            //info строка
            if (m.format == Format.ExportFormats.Mpeg2PAL)
                SetLog("DGPulldown: " + m.outframerate + " > 25.000");
            if (m.format == Format.ExportFormats.Mpeg2NTSC)
                SetLog("DGPulldown: " + m.outframerate + " > 29.970");
            SetLog(Languages.Translate("Please wait") + "...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\DGPulldown\\DGPulldown.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            //info.UseShellExecute = false;
            //info.RedirectStandardOutput = true;
            //info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            if (m.format == Format.ExportFormats.Mpeg2PAL)
                info.Arguments = "\"" + m.outvideofile + "\" -inplace -srcfps " + m.outframerate + " -destfps 25.000";
            if (m.format == Format.ExportFormats.Mpeg2NTSC)
                info.Arguments = "\"" + m.outvideofile + "\" -inplace -srcfps " + m.outframerate + " -destfps 29.970";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("dgpulldown.exe:" + " " + info.Arguments);
                SetLog("");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);
            encoderProcess.WaitForExit();

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //передаём новый фреймрейт для муксинга
            if (m.format == Format.ExportFormats.Mpeg2PAL)
                m.outframerate = "25.000";
            if (m.format == Format.ExportFormats.Mpeg2NTSC)
                m.outframerate = "29.970";

            SetLog("");
        }

        private void make_fourcc(string fourcc)
        {
            busyfile = Path.GetFileName(m.outvideofile);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\cfourcc\\cfourcc.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            //info.UseShellExecute = false;
            //info.RedirectStandardOutput = true;
            //info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            //myProcess.StartInfo.Arguments = "-u " & NewFOURCC & " -d " & NewFOURCC & " " & """" & FilePath & """"
            info.Arguments = "\"" + m.outvideofile + "\" -u " + fourcc + " -d " + fourcc;

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("cfourcc.exe:" + " " + info.Arguments);
                SetLog("");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            //SetPriority(Settings.ProcessPriority); //cfourcc.exe - работает слишком быстро :) иногда успевает закрыться до присвоения приоритета, что приводит к ошибке
            encoderProcess.WaitForExit();

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            SetLog("");
        }

        private void make_vdubmod()
        {
            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //info строка и определение размера итогового файла
            long totalsize = new FileInfo(m.outvideofile).Length;
            SetLog("Video file: " + m.outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                {
                    SetLog("Audio file: " + outstream.audiopath);
                    totalsize += new FileInfo(outstream.audiopath).Length;
                }
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\VirtualDubMod\\VirtualDubMod.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            info.Arguments = "/x /s\"" + Settings.TempPath + "\\" + m.key + ".vcf\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("virtualdubmod.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //прописываем фикс для реестра
            VirtualDubModWrapper.SetStartUpInfo();

            //создаём скрипт
            VirtualDubModWrapper.CreateMuxingScript(m, CopyDelay);

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            //SetPriority(Settings.ProcessPriority);

            //IsNoProgressStage = true;
            //encoderProcess.WaitForExit();
            while (!encoderProcess.HasExited)
            {
                Thread.Sleep(500);

                //А паузы тут нет..
                //locker.WaitOne();

                try
                {
                    if (File.Exists(m.outfilepath))
                    {
                        //Почему-то раньше всё это было задисаблено, хотя после исправления
                        //формулы этот способ определения прогресса выглядит вполне рабочим..
                        worker.ReportProgress(Convert.ToInt32((new FileInfo(m.outfilepath).Length * m.outframes) / totalsize));
                    }
                }
                catch (Exception) { }
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) || new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext.Length = 0;

            SetLog("");
        }

        private void make_mkv()
        {
            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //Видеофайл
            string outvideofile = (IsDirectRemuxingPossible && m.outvcodec == "Copy") ? m.infilepath : m.outvideofile;

            //info строка
            SetLog("Video file: " + outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                {
                    if (!File.Exists(outstream.audiopath) && outstream.codec == "Copy")
                        SetLog("Audio file: " + m.infilepath);
                    else
                        SetLog("Audio file: " + outstream.audiopath);
                }
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvmerge.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = false;
            info.CreateNoWindow = true;

            string charset = "";
            string _charset = Settings.MKVToolnix_Charset;

            try
            {
                if (_charset == "")
                {
                    info.StandardOutputEncoding = System.Text.Encoding.UTF8;
                    charset = "--output-charset UTF-8";
                }
                else if (_charset.ToLower() == "auto")
                {
                    info.StandardOutputEncoding = System.Text.Encoding.Default;
                    charset = "--output-charset " + System.Text.Encoding.Default.HeaderName;
                }
                else
                {
                    int page = 0;
                    if (int.TryParse(_charset, out page))
                        info.StandardOutputEncoding = System.Text.Encoding.GetEncoding(page);
                    else
                        info.StandardOutputEncoding = System.Text.Encoding.GetEncoding(_charset);
                    charset = "--output-charset " + _charset;
                }
            }
            catch (Exception) { }

            //Доп. параметры муксинга
            string mux_v = "", mux_a = "", mux_o = "";
            if (Formats.GetDefaults(m.format).IsEditable)
            {
                //Получение CLI и расшифровка подставных значений
                string muxer_cli = Calculate.ExpandVariables(m, Formats.GetSettings(m.format, "CLI_mkvmerge", Formats.GetDefaults(m.format).CLI_mkvmerge));
                mux_v = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", muxer_cli);
                mux_a = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", muxer_cli);
                mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);
            }

            //video
            string video = "";
            if (IsDirectRemuxingPossible && m.outvcodec == "Copy")
            {
                //Видео из исходника (режим Copy без демукса)
                string ext = Path.GetExtension(m.infilepath).ToLower();
                int vID = (ext == ".mpg" || ext == ".vob" || ext == ".ts" || ext == ".m2ts" || ext == ".m2t" || ext == ".mts") ? m.invideostream_ff_order :
                    (ext == ".mkv" || ext == ".webm" || ext == ".mp4" || ext == ".mov") ? m.invideostream_mi_order :
                    m.invideostream_mi_id; //avi, rm, ogm

                mux_v = (!string.IsNullOrEmpty(mux_v)) ? mux_v.Replace("%v_id%", vID.ToString()) + " " : "";
                video = "-d " + vID + " -A -S " + mux_v + "\"" + m.infilepath + "\" ";
            }
            else
            {
                //Обычный муксинг
                string ext = Path.GetExtension(m.outvideofile).ToLower();
                int vID = 0;

                string rate = "--default-duration " + vID + ":" + m.outframerate + "fps ";
                if (m.outvcodec == "Copy")
                {
                    if (m.inframerate == "")
                        rate = "";
                    else
                        rate = "--default-duration " + vID + ":" + m.inframerate + "fps ";
                }

                mux_v = (!string.IsNullOrEmpty(mux_v)) ? mux_v.Replace("%v_id%", vID.ToString()) + " " : "";
                video = rate + "-d " + vID + " -A -S " + mux_v + "\"" + m.outvideofile + "\" ";
            }

            //audio
            string audio = "";
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (IsDirectRemuxingPossible && outstream.codec == "Copy" && !File.Exists(outstream.audiopath))
                {
                    //Звук из исходника (режим Copy без демукса)
                    string ext = Path.GetExtension(m.infilepath).ToLower();
                    int aID = (ext == ".mpg" || ext == ".vob" || ext == ".ts" || ext == ".m2ts" || ext == ".m2t" || ext == ".mts") ? instream.ff_order :
                    (ext == ".mkv" || ext == ".webm" || ext == ".mp4" || ext == ".mov") ? instream.mi_order :
                    instream.mi_id; //avi, rm, ogm

                    string delay = "";
                    if (CopyDelay)
                    {
                        //Нужно учесть уже имеющуюся в файлах задержку
                        delay = " --sync " + aID + ":" + (outstream.delay - instream.delay);
                    }

                    mux_a = (!string.IsNullOrEmpty(mux_a)) ? mux_a.Replace("%a_id%", aID.ToString()) + " " : "";
                    audio = "-a " + aID + delay + " -D -S --no-chapters " + mux_a + "\"" + m.infilepath + "\" ";
                }
                else
                {
                    //Обычный муксинг
                    int aID = 0;
                    string aext = Path.GetExtension(outstream.audiopath).ToLower();
                    if (aext == ".avi") aID = 1; //Видеофайл, добавленный как внешний аудиотрек - не запрещено, но и не поддерживается!

                    string sbr = "";
                    if (outstream.codec == "Copy" && instream.codec.Contains("AAC"))
                    {
                        //Для правильного муксинга he-aac, aac+, или aac-sbr
                        if (instream.codec.Contains("HE") || instream.codec.Contains("AAC+") || instream.codec.Contains("SBR"))
                            sbr = " --aac-is-sbr 0:1";
                        else
                            sbr = " --aac-is-sbr 0:0";
                    }

                    string delay = (CopyDelay) ? " --sync " + aID + ":" + outstream.delay : "";
                    mux_a = (!string.IsNullOrEmpty(mux_a)) ? mux_a.Replace("%a_id%", aID.ToString()) + " " : "";
                    audio = "-a " + aID + sbr + delay + " -D -S --no-chapters " + mux_a + "\"" + outstream.audiopath + "\" ";
                }
            }

            //split
            string split = "";
            if (!string.IsNullOrEmpty(Splitting) && Splitting != "Disabled")
            {
                int size = 0;
                string svalue = Calculate.GetRegexValue(@"(\d+)\D*", Splitting);
                if (svalue != null) Int32.TryParse(svalue, NumberStyles.Integer, null, out size);
                if (size != 0) { split = "--split size:" + size + "M "; IsSplitting = true; }
            }

            //Ввод полученных аргументов командной строки
            mux_o = (!string.IsNullOrEmpty(mux_o)) ? mux_o + " " : "";
            info.Arguments = "-o \"" + m.outfilepath + "\" " + mux_o + video + audio + split + charset;

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("mkvmerge.exe:" + " " + info.Arguments);
                SetLog("");
            }

            //Муксим
            Do_MKVToolnix_Cycle(info);

            //обнуляем прогресс
            ResetProgress();

            if (IsAborted || IsErrors) return;

            //если включено деление
            if (IsSplitting) m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + "-001" + Path.GetExtension(m.outfilepath).ToLower();

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) || new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext.Length = 0;

            SetLog("");
        }

        private void Do_MKVToolnix_Cycle(ProcessStartInfo info)
        {
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            // +-> ЏаҐ¤ў аЁвҐ«м­л©  ­ «Ё§ FLAC д ©« : 100% - мусор
            // Џа®жҐбб: 100% - прогресс
            string pat = @"^[^\+].+:\s(\d+)%";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            int procent = 0;

            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardOutput.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else if (line != "")
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) AppendEncoderText(Calculate.FilterLogMessage(r, line.Replace("\r\r\n", "\r\n")));

            //Отлавливаем ошибку по ErrorLevel (1 - Warning, 2 - Error)
            if (encoderProcess.HasExited && !IsAborted)
            {
                if (encoderProcess.ExitCode == 1)
                {
                    SetLog(Languages.Translate("Warning") + ":");
                    SetLog(encodertext.ToString());
                }
                else if (encoderProcess.ExitCode != 0)
                {
                    IsErrors = true;
                    ErrorException(encodertext.ToString());
                }
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;
        }

        private void make_pmp()
        {
            IsNoProgressStage = true;

            if (m.outaudiostreams.Count == 0)
            {
                AudioStream stream = new AudioStream();
                stream.audiopath = Settings.TempPath + "\\" + m.key + ".m4a";
                File.Copy(Calculate.StartupPath + "\\apps\\pmp_muxer_avc\\fake.m4a",
                    stream.audiopath);
                m.outaudiostreams.Add(stream);
            }
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //проверка на правильное расширение
            string testext = Path.GetExtension(m.outvideofile);
            if (testext == ".h264")
            {
                string goodpath = Calculate.RemoveExtention(m.outvideofile, true) + ".264";
                File.Move(m.outvideofile, goodpath);
                m.outvideofile = goodpath;
            }

            //info строка
            SetLog("Video file: " + m.outvideofile);
            SetLog("Audio file: " + outstream.audiopath);
            SetLog("Muxing to: " + m.outfilepath);
            SetLog(Languages.Translate("Please wait") + "...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\pmp_muxer_avc\\pmp_muxer_avc.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string rate = "-r " + m.outframerate.Replace(".", "") + " ";
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate != null)
                    rate = "-r " + m.inframerate.Replace(".", "") + " ";
                else
                    rate = "";
            }

            string ovext = Path.GetExtension(m.outvideofile).ToLower();
            if (ovext == ".h264")
            {
                File.Move(m.outvideofile, Calculate.RemoveExtention(m.outvideofile, true) + ".264");
                m.outvideofile = Calculate.RemoveExtention(m.outvideofile, true) + ".264";
            }

            info.Arguments = rate + 
                "-w " + m.outresw.ToString() + 
                " -h " + m.outresh + " -s 1000" +
                " -v \"" + m.outvideofile + "\"" +
                " -a \"" + outstream.audiopath + "\""
                + " -o \"" + m.outfilepath + "\"";

            //прописываем аргументы командной строки
            SetLog("");
            if (Settings.ArgumentsToLog)
            {
                SetLog("pmp_muxer_avc.exe:" + " " + info.Arguments);
                SetLog("");
            }

            SetLog("...import video...");

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line = "";
            string pat = @"(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            bool IsNum;
            int aframes = 0;

            //первый проход
            //while (!encoderProcess.HasExited)
            while (line != null)
            {
                locker.WaitOne();
                line = encoderProcess.StandardOutput.ReadLine();

                if (line == null)
                    break;

                    mat = r.Match(line);

                    try
                    {
                        int n = Int32.Parse(line);
                        IsNum = true;
                    }
                    catch
                    {
                        if (line.Contains(" / ") && 
                            !line.Contains("unused_bytes") &&
                            !line.Contains("difference"))
                            IsNum = true;
                        else
                            IsNum = false;
                    }

                    if (line.Contains("first frame at"))
                    {
                        step++;
                        SetLog("...import audio...");
                        string[] separator = new string[] { " " };
                        string[] a = line.Split(separator, StringSplitOptions.None);
                        aframes = Convert.ToInt32(a[3]);
                    }

                    if (line.Contains("Interleaving ..."))
                    {
                        step++;
                        SetLog("...interleaving...");
                    }

                    if (line.Contains("Writing ..."))
                    {
                        step++;
                        SetLog("...writing...");
                    }

                    if (mat.Success && IsNum)
                    {
                        if (line.Contains(" / ") && !line.Contains("unused_bytes"))
                        {
                            string[] separator = new string[] { "/" };
                            string[] a = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            worker.ReportProgress((m.outframes / Convert.ToInt32(a[1])) * Convert.ToInt32(a[0]));
                        }
                        else
                        {
                            if (aframes == 0)
                                worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                            else
                                worker.ReportProgress((m.outframes / aframes) * Convert.ToInt32(mat.Groups[1].Value));
                        }
                    }
                    else
                    {
                        if (line.StartsWith("Status:"))
                        {
                            AppendEncoderText(line);
                            //SetLog(line);
                        }
                    }
            }

            //обнуляем прогресс
            ResetProgress();

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) || new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorException(encodertext.ToString());
            }
            else
            {
                //Удаление лога муксера
                File.Delete(m.outfilepath + ".log");
            }

            encodertext.Length = 0;

            SetLog("");
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

        //Обнуляем прогресс
        private void ResetProgress()
        {
            timer.Enabled = false;

            fps_averages.Clear();
            encoder_fps = fps = 0;
            old_frame = frame = 0;
            old_time = DateTime.Now;
            last_update = TimeSpan.Zero;
            IsNoProgressStage = false;

            timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                if (worker != null && worker.IsBusy && !IsPaused && ((System.Timers.Timer)source).Enabled)
                {
                    //получаем текущее время и текущий фрейм
                    DateTime current_time = DateTime.Now;
                    int current_frame = frame;
                    double estimated = 0;

                    //Определяем зависание
                    if (current_frame != old_frame)
                    {
                        //Сбрасываем счетчик "простоя"
                        last_update = TimeSpan.Zero;
                    }
                    else if (!IsNoProgressStage)
                    {
                        //Увеличиваем счетчик "простоя"
                        last_update += TimeSpan.FromMilliseconds(timer.Interval);

                        //Похоже, что кодирование зависло..
                        if (last_update.TotalMinutes > last_update_threshold && Settings.AutoAbortEncoding)
                        {
                            timer.Enabled = false;
                            timer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);
                            last_update = TimeSpan.Zero;
                            AbortFreezedTask();
                            return;
                        }
                    }

                    //вычисляем проценты прогресса
                    double percent = ((double)current_frame / m.outframes) * 100.0;
                    double percent_total = current_frame + (step * m.outframes);

                    //Определяем fps и оставшееся время
                    if (current_frame > 0)
                    {
                        if (encoder_fps > 0)
                        {
                            //Берём fps от энкодеров
                            fps = encoder_fps;

                            //запоминаем сравнительные значения
                            old_frame = current_frame;
                        }
                        else
                        {
                            //Или считаем сами
                            double interval = (current_time - old_time).TotalSeconds;

                            //Определяем fps с минимальным интервалом в 1 сек.
                            //"old_frame == 0" - самое первое определение прогресса, кодирование
                            //только началось и желательно выдать хоть что-то для наглядности процесса.
                            if (interval >= 1 || (old_frame == 0 && interval > 0))
                            {
                                //Возможно скорость очень низкая и за одну секунду прогресса не произошло,
                                //но ждать слишком долго (в зависимости от текущей скорости) точно бесполезно
                                if (current_frame != old_frame || (fps > 10 ? interval >= 5 : fps > 5 ? interval >= 10 : interval >= 15))
                                {
                                    //Считаем и усредняем fps (за 10 последних замеров)
                                    fps_averages.Add(Math.Max(((current_frame - old_frame) / interval), 0));
                                    if (fps_averages.Count >= 11) fps_averages.RemoveAt(0);

                                    double temp = 0;
                                    fps_averages.ForEach(n => temp += n);
                                    fps = temp / fps_averages.Count;

                                    //запоминаем сравнительные значения
                                    old_frame = current_frame;
                                    old_time = current_time;
                                }
                            }
                        }

                        //вычисляем сколько времени осталось кодировать
                        if (current_frame < m.outframes) estimated = (double)(m.outframes - current_frame) / fps;

                        string estimated_string = "";
                        if (!double.IsInfinity(estimated))
                        {
                            TimeSpan est = TimeSpan.FromSeconds(estimated);
                            if (est.Days > 0) estimated_string += est.Days + "day ";
                            if (est.Hours > 0) estimated_string += est.Hours + "hour ";
                            if (est.Minutes > 0) estimated_string += est.Minutes + "min ";
                            if (est.Seconds > 0) estimated_string += est.Seconds + "sec";
                        }
                        else
                            estimated_string = unknown;

                        if (((System.Timers.Timer)source).Enabled)
                        {
                            string title = percent.ToString("##0.00") + "% encoding to: " + busyfile;
                            string statistics = current_frame + "frames " + fps.ToString((encoder_fps > 0) ? "####0.00" : "####0") + "fps " + estimated_string;
                            p.TrayIcon.Text = "Step: " + (step + 1).ToString() + " of " + steps.ToString() + " (" + percent.ToString("##0.00") + "%)\r\n" + estimated_string;
                            Win7Taskbar.SetProgressValue(ActiveHandle, Convert.ToUInt64(percent_total), total_pr_max);
                            SetStatus(title, statistics, current_frame, percent_total);
                        }
                    }
                    else if (((System.Timers.Timer)source).Enabled)
                    {
                        //Кодирование только началось - прогресса еще нет (или его нет и не ожидается)
                        p.TrayIcon.Text = "Step: " + (step + 1).ToString() + " of " + steps.ToString() + " (0.00%)\r\n" + unknown;
                        Win7Taskbar.SetProgressValue(ActiveHandle, Convert.ToUInt64(percent_total), total_pr_max);
                        SetStatus("0.00% encoding to: " + busyfile, ((IsNoProgressStage) ? unknown : ""), current_frame, percent_total);
                    }
                }
            }
            catch (Exception)
            {
                //ErrorExeption(ex.Message);
            }
        }

        internal delegate void AbortFreezedTaskDelegate();
        private void AbortFreezedTask()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new AbortFreezedTaskDelegate(AbortFreezedTask));
            else
            {
                try
                {
                    SetLog("\r\n\r\n" + Languages.Translate("No progress for xx minutes, aborting encoding!").Replace("xx", last_update_threshold.ToString()));
                    AbortAction(true);

                    //Т.к. AbortAction не всегда может приводить к отмене зависшего кодирования, то нельзя
                    //расчитывать на запуск следующего задания в RunWorkerCompleted - запускаем его тут
                    p.EncodeNextTask();
                }
                catch (Exception ex)
                {
                    ErrorException(ex.Message);
                }
            }
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //запоминаем когда это началось
                start_time = old_time = DateTime.Now;

                //запускаем таймер
                timer = new System.Timers.Timer();
                timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                timer.Interval = 500;
                timer.Enabled = true;

                //пишем в файл свежий скрипт
                AviSynthScripting.WriteScriptToFile(m);

                //прописываем инфо в лог
                SetLog("PLATFORM");
                SetLog("------------------------------");
                SetLog("OS Code: " + Environment.OSVersion.ToString());
                SetLog("OS Name: " + SysInfo.GetOSNameFull());
                SetLog("Framework: " + Environment.Version + SysInfo.GetFrameworkVersion());
                SetLog("AviSynth: " + SysInfo.AVSVersionString);
                SetLog("CPU Info: " + SysInfo.GetCPUInfo() + ", " + Environment.ProcessorCount + " core(s)");
                SetLog("RAM Total: " + SysInfo.GetTotalRAM());
                SetLog("Language: " + CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName + " (" +
                    //CultureInfo.CurrentCulture.TextInfo.ANSICodePage + ", \"" +
                    System.Text.Encoding.Default.CodePage + ", \"" +
                    CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "\")");
                SetLog("SystemDrive: " + Environment.GetEnvironmentVariable("SystemDrive"));
                SetLog("");

                SetLog("XviD4PSP");
                SetLog("------------------------------");
                Assembly this_assembly = Assembly.GetExecutingAssembly();
                SetLog("Version: " + this_assembly.GetName().Version.ToString());
                SetLog("Created: " + File.GetLastWriteTime(this_assembly.GetModules()[0].FullyQualifiedName).ToString("dd.MM.yyyy HH:mm:ss"));
                SetLog("AppPath: " + Calculate.StartupPath);
                SetLog("TempPath: " + Settings.TempPath);
                SetLog("");

                SetLog("FILES");
                SetLog("------------------------------");
                //файлы
                foreach (string f in m.infileslist)
                {
                    SetLog(Path.GetFileName(f) + " >");
                }
                SetLog(Path.GetFileName(m.outfilepath));
                SetLog("");

                SetLog("TASK");
                SetLog("------------------------------");
                SetLog("Format: " + Format.EnumToString(m.format));
                SetLog("Duration: " + Calculate.GetTimeline(m.outduration.TotalSeconds) + " (" + m.outframes + ")");

                if (m.vencoding != "Disabled")
                {
                    if (m.outvcodec != "Copy")
                    {
                        SetLog("VideoDecoder: " + m.vdecoder);

                        if (m.inresw != m.outresw || m.inresh != m.outresh)
                            SetLog("Resolution: " + m.inresw + "x" + m.inresh + " > " + m.outresw + "x" + m.outresh);
                        else
                            SetLog("Resolution: " + m.inresw + "x" + m.inresh);

                        if (Math.Abs(m.inaspect - m.outaspect) > 0.0001)
                            SetLog("Aspect: " + Calculate.ConvertDoubleToPointString(m.inaspect, 4) + " > " + Calculate.ConvertDoubleToPointString(m.outaspect, 4));
                        else
                            SetLog("Aspect: " + Calculate.ConvertDoubleToPointString(m.outaspect, 4));

                        SetLog("VCodecPreset: " + m.vencoding);
                        SetLog("VEncodingMode: " + m.encodingmode);

                        string vcodec_info = (m.outvcodec == "x264") ? (((m.x264options.profile == x264.Profiles.High10) ? " 10-bit depth" : "") +
                            ((Settings.UseAVS4x264 && SysInfo.GetOSArchInt() == 64) ? " (x64)" : "")) :
                            (m.outvcodec == "XviD") ? (Settings.UseXviD_130) ? " (1.3.x)" : " (1.2.2)" : "";

                        if (m.invcodec != m.outvcodec)
                            SetLog("VideoCodec: " + m.invcodecshort + " > " + m.outvcodec + vcodec_info);
                        else
                            SetLog("VideoCodec: " + m.invcodecshort + vcodec_info);

                        if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                            m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                            m.encodingmode == Settings.EncodingModes.OnePassSize)
                        {
                            SetLog("Size: " + m.infilesize + " > " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " mb");
                        }
                        else
                        {
                            if (m.invbitrate != m.outvbitrate)
                            {
                                if (m.encodingmode == Settings.EncodingModes.Quality ||
                                    m.encodingmode == Settings.EncodingModes.Quantizer ||
                                    m.encodingmode == Settings.EncodingModes.ThreePassQuality ||
                                    m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                                    SetLog("VideoBitrate: " + m.invbitrate + " > Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1));
                                else
                                    SetLog("VideoBitrate: " + m.invbitrate + " > " + m.outvbitrate);
                            }
                            else
                                SetLog("VideoBitrate: " + m.invbitrate);
                        }

                        string inquality = Calculate.GetQualityIn(m);
                        string outquality = Calculate.GetQualityOut(m, true);
                        if (inquality != outquality && outquality != Languages.Translate("Unknown"))
                            SetLog("Quality: " + inquality + " > " + outquality);
                        if (m.inframerate != m.outframerate)
                        {
                            SetLog("Framerate: " + m.inframerate + " > " + m.outframerate);
                            SetLog("FramerateModifier: " + m.frameratemodifer);
                        }
                        else
                            SetLog("Framerate: " + m.inframerate);

                        SetLog("SourceType: " + m.interlace);
                        SetLog("FieldOrder: " + m.fieldOrder);
                        if (m.deinterlace != DeinterlaceType.Disabled)
                            SetLog("Deinterlacer: " + m.deinterlace);
                    }
                    else
                    {
                        SetLog("VCodecPreset: Copy");
                        SetLog("VideoCodec: " + m.invcodecshort);
                        SetLog("VideoBitrate: " + m.invbitrate);
                        SetLog("Resolution: " + m.inresw + "x" + m.inresh);
                        SetLog("Framerate: " + m.inframerate);
                    }
                }
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec != "Copy")
                    {
                        if (instream.decoder > 0)
                            SetLog("AudioDecoder: " + instream.decoder);

                        SetLog("AEncodingPreset: " + outstream.encoding);
                        if (instream.codecshort != outstream.codec)
                            SetLog("AudioCodec: " + instream.codecshort + " > " + outstream.codec);
                        else
                            SetLog("AudioCodec: " + instream.codecshort);

                        if (instream.bitrate != outstream.bitrate)
                            SetLog("AudioBitrate: " + instream.bitrate + " > " + ((outstream.bitrate == 0) ? "VBR" : outstream.bitrate.ToString()));
                        else
                            SetLog("AudioBitrate: " + instream.bitrate);

                        if (instream.samplerate != outstream.samplerate)
                        {
                            SetLog("Samplerate: " + instream.samplerate + " > " + outstream.samplerate);
                            SetLog("SamplerateModifier: " + m.sampleratemodifer);
                        }
                        else
                        {
                            SetLog("Samplerate: " + instream.samplerate);
                        }

                        if (instream.channels != outstream.channels)
                        {
                            SetLog("Channels: " + instream.channels + " > " + outstream.channels);
                            SetLog("UpDownMix: " + instream.channelconverter);
                        }
                        else
                        {
                            SetLog("Channels: " + instream.channels);
                        }

                        if (instream.gain != "0.0" && outstream.codec != "Disabled" && outstream.codec != "Copy")
                        {
                            SetLog("Normalize: " + m.volume);
                            SetLog("Accurate: " + m.volumeaccurate);
                            SetLog("Gain: " + instream.gain);
                        }
                        if (instream.delay != 0 || outstream.delay != 0)
                            SetLog("Delay: " + instream.delay + " > " + outstream.delay);
                    }
                    else
                    {
                        SetLog("AEncodingPreset: Copy");
                        SetLog("AudioCodec: " + instream.codecshort);
                        SetLog("AudioBitrate: " + instream.bitrate);
                        SetLog("Samplerate: " + instream.samplerate);
                        SetLog("Channels: " + instream.channels);
                        if (Settings.CopyDelay && (instream.delay != 0 || outstream.delay != 0))
                            SetLog("Delay: " + instream.delay + " > " + outstream.delay);
                    }

                    if (DontMuxStreams)
                    {
                        SetLog("V+A Muxing: Disabled");
                    }
                }

                SetLog("");

                //в расширенном режиме пишем в лог текст скрипта
                if (Settings.PrintAviSynth)
                {
                    SetLog("SCRIPT");
                    SetLog("------------------------------");
                    SetLog(m.script);
                    SetLog("");
                }

                //кодирование звука (до кодирования видео)
                if (AudioFirst)
                {
                    if (m.outaudiostreams.Count > 0)
                    {
                        AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                        AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        if (outstream.codec == "Copy" && !File.Exists(instream.audiopath) &&
                            (!IsDirectRemuxingPossible || DontMuxStreams))
                        {
                            SetLog("DEMUXING");
                            SetLog("------------------------------");
                        }
                        else if (outstream.codec != "Disabled" && outstream.codec != "Copy")
                        {
                            if (muxer != Format.Muxers.Disabled || DontMuxStreams || m.format == Format.ExportFormats.Audio)
                            {
                                SetLog("AUDIO ENCODING");
                                SetLog("------------------------------");
                            }
                        }

                        if (muxer != Format.Muxers.Disabled || muxer == Format.Muxers.Disabled && m.vencoding == "Disabled" || DontMuxStreams)
                            make_sound();
                    }

                    if (IsAborted || IsErrors) return;

                    //обнуляем прогресс
                    ResetProgress();
                }

                //кодирование видео
                if (m.outvcodec == "Copy" && (!IsDirectRemuxingPossible || DontMuxStreams))
                {
                    SetLog("DEMUXING");
                    SetLog("------------------------------");
                }
                else if (muxer == Format.Muxers.Disabled && m.format != Format.ExportFormats.Audio && !DontMuxStreams)
                {
                    SetLog((m.outaudiostreams.Count == 0) ? "VIDEO ENCODING" : "VIDEO & AUDIO ENCODING");
                    SetLog("------------------------------");
                }
                else if (m.outvcodec != "Disabled" && m.outvcodec != "Copy")
                {
                    SetLog("VIDEO ENCODING");
                    SetLog("------------------------------");
                }

                if (m.outvcodec == "x264" ||
                    m.outvcodec == "x262")
                    make_x264();
                if (m.outvcodec == "XviD")
                    make_XviD();
                if (m.outvcodec == "MPEG2" ||
                    m.outvcodec == "MPEG1" ||
                    m.outvcodec == "MPEG4" ||
                    m.outvcodec == "FFV1" ||
                    m.outvcodec == "DV" ||
                    m.outvcodec == "HUFF" ||
                    m.outvcodec == "MJPEG" ||
                    m.outvcodec == "FLV1")
                    make_ffmpeg();

                //копирование видео потока
                if (m.outvcodec == "Copy")
                {
                    if (!DontMuxStreams && IsDirectRemuxingPossible)
                    {
                        step++;
                        m.outvideofile = m.infilepath;
                    }
                    else
                    {
                        if (DontMuxStreams)
                        {
                            m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true) +
                                (m.format == Format.ExportFormats.BluRay ? "\\" + m.key : "") + //Кодирование в папку
                                 "." + Format.GetValidRAWVideoEXT(m);
                        }

                        Format.Demuxers dem = Format.GetDemuxer(m);
                        if (dem == Format.Demuxers.mkvextract) demux_mkv(Demuxer.DemuxerMode.ExtractVideo);
                        else if (dem == Format.Demuxers.pmpdemuxer) demux_pmp(Demuxer.DemuxerMode.ExtractVideo);
                        else if (dem == Format.Demuxers.mp4box) demux_mp4box(Demuxer.DemuxerMode.ExtractVideo);
                        else demux_ffmpeg(Demuxer.DemuxerMode.ExtractVideo);
                    }
                }

                if (IsAborted || IsErrors) return;

                //обнуляем прогресс
                ResetProgress();

                //кодирование звука (после кодирования видео)
                if (!AudioFirst)
                {
                    if (m.outaudiostreams.Count > 0)
                    {
                        AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                        AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        if (outstream.codec == "Copy" && !File.Exists(instream.audiopath) &&
                            (!IsDirectRemuxingPossible || DontMuxStreams))
                        {
                            SetLog("DEMUXING");
                            SetLog("------------------------------");
                        }
                        else if (outstream.codec != "Disabled" && outstream.codec != "Copy")
                        {
                            if (muxer != Format.Muxers.Disabled || DontMuxStreams || m.format == Format.ExportFormats.Audio)
                            {
                                SetLog("AUDIO ENCODING");
                                SetLog("------------------------------");
                            }
                        }

                        if (muxer != Format.Muxers.Disabled || muxer == Format.Muxers.Disabled && m.vencoding == "Disabled" || DontMuxStreams)
                            make_sound();
                    }

                    if (IsAborted || IsErrors) return;

                    //обнуляем прогресс
                    ResetProgress();
                }

                //pulldown
                if (m.format == Format.ExportFormats.Mpeg2PAL && m.outframerate != "25.000" ||
                    m.format == Format.ExportFormats.Mpeg2NTSC && m.outframerate != "29.970")
                {
                    SetLog("PULLDOWN");
                    SetLog("------------------------------");
                    make_pulldown();
                }

                if (IsAborted || IsErrors) return;

                //обнуляем прогресс
                ResetProgress();

                //муксинг
                if (muxer != 0 && muxer != Format.Muxers.Disabled && !DontMuxStreams)
                {
                    SetLog("MUXING");
                    SetLog("------------------------------");

                    if (muxer == Format.Muxers.pmpavc) make_pmp();
                    else if (muxer == Format.Muxers.mkvmerge) make_mkv();
                    else if (muxer == Format.Muxers.ffmpeg) make_ffmpeg_mux();
                    else if (muxer == Format.Muxers.virtualdubmod)
                    {
                        //делаем ави из avc
                        string vext = Path.GetExtension(m.outvideofile);
                        if (vext != ".avi")
                        {
                            make_avc2avi();
                            if (IsAborted || IsErrors) return;
                        }

                        make_vdubmod();
                    }
                    else if (muxer == Format.Muxers.mp4box) make_mp4();
                    else if (muxer == Format.Muxers.tsmuxer) make_tsmuxer();
                    else if (muxer == Format.Muxers.dpgmuxer) make_dpg();
                }

                if (IsAborted || IsErrors) return;

                //обнуляем прогресс
                ResetProgress();

                //Прогресса уже точно не будет
                IsNoProgressStage = true;

                //THM
                if (m.format == Format.ExportFormats.PmpAvc)
                    make_thm(144, 80, true, "png");
                else if (Formats.GetDefaults(m.format).IsEditable)
                {
                    string thm_def = Formats.GetDefaults(m.format).THM_Format;
                    string thm_format = Formats.GetSettings(m.format, "THM_Format", thm_def);
                    if (thm_format != "JPG" && thm_format != "PNG" && thm_format != "BMP" && thm_format != "None") thm_format = thm_def;
                    if (thm_format != "None")
                    {
                        bool fix_ar = Formats.GetSettings(m.format, "THM_FixAR", Formats.GetDefaults(m.format).THM_FixAR);
                        int thm_w = Formats.GetSettings(m.format, "THM_Width", Formats.GetDefaults(m.format).THM_Width);
                        int thm_h = Formats.GetSettings(m.format, "THM_Height", Formats.GetDefaults(m.format).THM_Height);
                        if (thm_w == 0) thm_w = m.outresw; if (thm_h == 0) thm_h = m.outresh;
                        make_thm(thm_w, thm_h, fix_ar, thm_format.ToLower());
                    }
                }

                //прописываем сколько это всё заняло у нас врмени
                TimeSpan enc_time = (DateTime.Now - start_time);
                string enc_time_s = "";
                if (enc_time.Days > 0) enc_time_s += enc_time.Days + " day ";
                if (enc_time.Hours > 0) enc_time_s += enc_time.Hours + " hour ";
                if (enc_time.Minutes > 0) enc_time_s += enc_time.Minutes + " min ";
                if (enc_time.Seconds > 0) enc_time_s += enc_time.Seconds + " sec";

                SetLog("TIME");
                SetLog("------------------------------");
                SetLog(Languages.Translate("Total encoding time:") + " " + enc_time_s);

                //Определение размера файла(ов)
                if (IsSplitting)
                {
                    string[] files = new string[0];
                    string pat1 = " ", pat2 = "";
                    if (muxer == Format.Muxers.mkvmerge)
                    {
                        pat1 = "-001";
                        pat2 = "*-*";
                    }
                    else if (muxer == Format.Muxers.mp4box)
                    {
                        pat1 = "_001";
                        pat2 = "*_*";
                    }
                    else if (muxer == Format.Muxers.tsmuxer)
                    {
                        pat1 = ".split.1";
                        pat2 = "*.split.*";
                    }

                    files = Directory.GetFiles(Path.GetDirectoryName(m.outfilepath),
                       Path.GetFileNameWithoutExtension(m.outfilepath).Replace(pat1, pat2) +
                       Path.GetExtension(m.outfilepath).ToLower());

                    //если получился только 1 файл
                    if (files.Length == 1)
                    {
                        File.Move(m.outfilepath, m.outfilepath.Replace(pat1, ""));
                        m.outfilepath = m.outfilepath.Replace(pat1, "");
                        files = new string[1];
                        files[0] = m.outfilepath;
                    }

                    long size = 0;
                    foreach (string f in files)
                    {
                        FileInfo finfo = new FileInfo(f);
                        size += finfo.Length;
                    }

                    SetLog(Languages.Translate("Out file size is:") + " " +
                        Calculate.ConvertDoubleToPointString((double)size / 1024.0 / 1024.0, 2) + " mb (" + files.Length + " files)");
                }
                else if (m.format == Format.ExportFormats.BluRay && !DontMuxStreams)
                {
                    long size = Calculate.GetFolderSize(m.outfilepath);
                    SetLog(Languages.Translate("Out file size is:") + " " +
                        Calculate.ConvertDoubleToPointString((double)size / 1024.0 / 1024.0, 2) + " mb");
                }
                else
                {
                    if (DontMuxStreams)
                    {
                        SetLog(Languages.Translate("Out file size is:") + " " +
                            Calculate.ConvertDoubleToPointString(new FileInfo(m.outvideofile).Length / 1024.0 / 1024.0, 2) + " mb (video track)");

                        m.outfilepath = m.outvideofile;

                        if (m.outaudiostreams.Count > 0)
                        {
                            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                            SetLog(Languages.Translate("Out file size is:") + " " +
                            Calculate.ConvertDoubleToPointString(new FileInfo(outstream.audiopath).Length / 1024.0 / 1024.0, 2) + " mb (audio track)");
                        }
                    }
                    else
                    {
                        SetLog(Languages.Translate("Out file size is:") + " " +
                        Calculate.ConvertDoubleToPointString(new FileInfo(m.outfilepath).Length / 1024.0 / 1024.0, 2) + " mb");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!IsAborted && !IsErrors)
                {
                    IsErrors = true;
                    ErrorException(ex.Message);
                }
            }
        }

       private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            try
            {
                //получаем текущий фрейм
                frame = e.ProgressPercentage;
            } 
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

       private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //закрываем таймер
            timer.Enabled = false;
            timer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);
            timer.Close();

            button_pause.Visibility = Visibility.Hidden;
            cbxPriority.Visibility = Visibility.Hidden;
            combo_ending.Visibility = Visibility.Hidden;
            prCurrent.Visibility = Visibility.Hidden;
            prTotal.Visibility = Visibility.Hidden;
            tbxProgress.Visibility = Visibility.Hidden;
            tbxLog.Margin = new Thickness(8, 8, 8, 8);
            prCurrent.Value = 0;
            prTotal.Value = 0;
            button_cancel.ToolTip = Languages.Translate("Close");
            button_cancel.Content = Languages.Translate("Close");
            p.TrayIcon.Text = "XviD4PSP";

            if (this.IsVisible)
                this.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(Encoder_IsVisibleChanged);

            if (!IsErrors && !IsAborted)
            {
                //Нет ошибок
                Finished = 0;
                button_info.Visibility = Visibility.Visible;
                button_play.Visibility = Visibility.Visible;

                //"Готово" в TrayIcon
                if (!Settings.TrayNoBalloons && (!p.IsVisible || !this.IsVisible || this.WindowState == WindowState.Minimized))
                {
                    p.TrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                    p.TrayIcon.BalloonTipTitle = Languages.Translate("Complete") + "!";
                    p.TrayIcon.BalloonTipText = Path.GetFileName(m.outfilepath);
                    p.TrayIcon.ShowBalloonTip(5000);
                }

                //"Готово" в Taskbar
                Win7Taskbar.SetProgressState(ActiveHandle, TBPF.NOPROGRESS);
            }
            else if (IsErrors && !IsAborted)
            {
                //Есть ошибки
                Finished = 1;

                //"Ошибка" в TrayIcon
                if (!Settings.TrayNoBalloons && (!p.IsVisible || !this.IsVisible || this.WindowState == WindowState.Minimized))
                {
                    p.TrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Error;
                    p.TrayIcon.BalloonTipTitle = Languages.Translate("Error") + "!";
                    p.TrayIcon.BalloonTipText = Path.GetFileName(m.outfilepath);
                    p.TrayIcon.ShowBalloonTip(5000);
                }

                //"Ошибка" в Taskbar
                Win7Taskbar.SetProgressTaskComplete(ActiveHandle, TBPF.ERROR);

                try
                {
                    //Сохранение лога при ошибке
                    string logfilename = m.outfilepath + ".error.log";
                    File.WriteAllText(logfilename, tbxLog.Text + Environment.NewLine, System.Text.Encoding.Default);

                    SetLog(Environment.NewLine + "This log was saved here: " + logfilename);
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message);
                }
            }

            try
            {
                //удаляем временные файлы
                if (!IsErrors)
                {
                    if (m.outvideofile != null && !DontMuxStreams &&
                        muxer != Format.Muxers.Disabled &&
                        m.outvideofile != m.outfilepath &&
                        Path.GetDirectoryName(m.outvideofile) == Settings.TempPath)
                    {
                        //Защита от удаления исходников
                        bool garbage = (m.outvideofile != m.infilepath);
                        foreach (string file in m.infileslist)
                        {
                            if (m.outvideofile == file)
                            {
                                garbage = false; break;
                            }
                        }
                        if (garbage) SafeDelete(m.outvideofile);
                    }

                    foreach (object s in m.inaudiostreams)
                    {
                        AudioStream a = (AudioStream)s;
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
                            if (garbage) p.deletefiles.Add(a.audiopath);
                        }
                    }

                    foreach (object s in m.outaudiostreams)
                    {
                        AudioStream a = (AudioStream)s;
                        if (a.audiopath != null && !DontMuxStreams &&
                            Path.GetDirectoryName(a.audiopath) == Settings.TempPath &&
                            a.audiopath != m.outfilepath) //Защита от удаления результата кодирования
                            p.deletefiles.Add(a.audiopath);

                        //Временный WAV-файл от 2pass AAC
                        if (!string.IsNullOrEmpty(a.nerotemp) && Path.GetDirectoryName(a.nerotemp) == Settings.TempPath)
                        {
                            //Защита от удаления исходников
                            bool garbage = (a.nerotemp != m.infilepath);
                            foreach (string file in m.infileslist)
                            {
                                if (a.nerotemp == file)
                                {
                                    garbage = false; break;
                                }
                            }
                            if (garbage) SafeDelete(a.nerotemp);
                        }
                    }
                }

                SafeDelete(m.scriptpath);
                if (m.outvideofile != null)
                {
                    SafeDelete(Calculate.RemoveExtention(m.outvideofile) + "log");
                    SafeDelete(Calculate.RemoveExtention(m.outvideofile) + "log.temp");
                }
                SafeDelete(Settings.TempPath + "\\" + m.key + ".vcf");

                //проверка на удачное завершение (заголовок окна)
                if (!IsAborted && !IsErrors)
                {
                    if (m.format == Format.ExportFormats.BluRay && !DontMuxStreams)
                    {
                        if (Directory.Exists(m.outfilepath) && Calculate.GetFolderSize(m.outfilepath) != 0)
                        {
                            p.outfiles.Remove(outpath_src);
                            Title = Path.GetFileName(m.outfilepath) + " " + Languages.Translate("ready") + "!";
                        }
                        else
                            Title = Languages.Translate("Error") + "!";
                    }
                    else
                    {
                        if (DontMuxStreams && File.Exists(m.outvideofile) && new FileInfo(m.outvideofile).Length != 0 &&
                            (m.outaudiostreams.Count == 0 || m.outaudiostreams.Count > 0 &&
                            File.Exists(((AudioStream)m.outaudiostreams[m.outaudiostream]).audiopath) &&
                            new FileInfo(((AudioStream)m.outaudiostreams[m.outaudiostream]).audiopath).Length != 0))
                        {
                            p.outfiles.Remove(outpath_src);
                            Title = Languages.Translate("Complete") + "!";
                        }
                        else if (!DontMuxStreams && File.Exists(m.outfilepath) && new FileInfo(m.outfilepath).Length != 0)
                        {
                            p.outfiles.Remove(outpath_src);
                            Title = Path.GetFileName(m.outfilepath) + " " + Languages.Translate("ready") + "!";
                        }
                        else
                            Title = Languages.Translate("Error") + "!";
                    }
                }

                //меняем статус кодирования
                if (IsAborted) p.UpdateTaskStatus(m.key, TaskStatus.Waiting);
                else if (IsErrors) p.UpdateTaskStatus(m.key, TaskStatus.Errors);
                else
                {
                    if (Settings.AutoDeleteTasks) p.RemoveTask(m.key);
                    else p.UpdateTaskStatus(m.key, TaskStatus.Encoded);
                }

                //Смотрим, есть ли что ещё скодировать
                if (!IsAborted && !IsAutoAborted && p.EncodeNextTask())
                {
                    //Если нет заданий со статусами Waiting, Encoding и Errors, то можно выходить
                    if (ending == Shutdown.ShutdownMode.Exit)
                    {
                        p.IsExiting = true;
                        p.Close();
                    }
                    else if (ending == Shutdown.ShutdownMode.Hibernate ||
                        ending == Shutdown.ShutdownMode.Shutdown ||
                        ending == Shutdown.ShutdownMode.Standby)
                    {
                        Shutdown shut = new Shutdown(this, ending);
                    }
                }

                //Закрываем окно
                if (Settings.AutoClose && !IsErrors) Close();
            }
            catch (Exception ex)
            {
                ErrorException(ex.Message);
            }
        }

        private void cbxPriority_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbxPriority.SelectedItem != null && cbxPriority.IsDropDownOpen)
            {
                Settings.ProcessPriority = cbxPriority.SelectedIndex;
                SetPriority(Settings.ProcessPriority);
            }
        }

        private void button_play_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start(m.outfilepath);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);
            }
        }

        private void button_info_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaInfo media = new MediaInfo(m.outfilepath, MediaInfo.InfoMode.MediaInfo, this);
        }

        private void SetPriority(int prioritet)
        {
            if (encoderProcess != null)
            {
                if (prioritet == 0)
                {
                    encoderProcess.PriorityClass = ProcessPriorityClass.Idle;
                    encoderProcess.PriorityBoostEnabled = false;
                }
                else if (prioritet == 1)
                {
                    encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                    encoderProcess.PriorityBoostEnabled = true;
                }
                else if (prioritet == 2)
                {
                    encoderProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
                    encoderProcess.PriorityBoostEnabled = true;
                }
            }
            if (avs != null && avs.IsBusy())
            {
                avs.SetPriority(prioritet);
            }
        }

        private void ErrorException(string message)
        {
            SetLog("");
            SetLog(Languages.Translate("Error") + ": " + Environment.NewLine + message);
            IsErrors = true;
            //ShowMessage(message);
        }

        internal delegate void MessageDelegate(string data);
        private void ShowMessage(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), data);
            else
            {
                Message mes = new Message(this.Owner);
                mes.ShowMessage(data, Languages.Translate("Error"));
            }
        }

        internal delegate void StatusDelegate(string title, string pr_text, double pr_c, double pr_t);
        private void SetStatus(string title, string pr_text, double pr_c, double pr_t)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new StatusDelegate(SetStatus), title, pr_text, pr_c, pr_t);
            else
            {
                this.Title = title;
                this.tbxProgress.Text = pr_text;
                this.prCurrent.Value = pr_c;
                this.prTotal.Value = pr_t;
            }
        }

        internal delegate void MaximumDelegate(double maximum);
        private void SetMaximum(double maximum)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MaximumDelegate(SetMaximum), maximum);
            else
            {
                prCurrent.Maximum = maximum;
                prTotal.Maximum = maximum * steps;
                total_pr_max = Convert.ToUInt64(prTotal.Maximum);
            }
        }

        internal delegate void LogDelegate(string data);
        private void SetLog(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new LogDelegate(SetLog), data);
            else
            {
                tbxLog.AppendText(data + Environment.NewLine);
                tbxLog.ScrollToEnd();

                //Лог в файл
                if (Settings.WriteLog)
                {
                    try
                    {
                        string logfilename = ((Settings.LogInTemp) ? Settings.TempPath + "\\" + Path.GetFileName(m.outfilepath) : m.outfilepath) + ".encoding.log";
                        File.AppendAllText(logfilename, data + Environment.NewLine, System.Text.Encoding.Default);
                    }
                    catch (Exception ex)
                    {
                        tbxLog.AppendText(ex.Message + Environment.NewLine);
                        tbxLog.ScrollToEnd();
                    }
                }
            }
        }

        private void SetFilteredLog(Regex reg, string text)
        {
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line) && !reg.IsMatch(line))
                    SetLog(line);
            }
        }

        private void combo_ending_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_ending.IsDropDownOpen || combo_ending.IsSelectionBoxHighlighted)
            {
                if (combo_ending.SelectedItem != null)
                {
                    if (combo_ending.SelectedItem.ToString() == Languages.Translate("Wait"))
                        ending = Shutdown.ShutdownMode.Wait;
                    if (combo_ending.SelectedItem.ToString() == Languages.Translate("Standby"))
                        ending = Shutdown.ShutdownMode.Standby;
                    if (combo_ending.SelectedItem.ToString() == Languages.Translate("Hibernate"))
                        ending = Shutdown.ShutdownMode.Hibernate;
                    if (combo_ending.SelectedItem.ToString() == Languages.Translate("Shutdown"))
                        ending = Shutdown.ShutdownMode.Shutdown;
                    if (combo_ending.SelectedItem.ToString() == Languages.Translate("Exit"))
                        ending = Shutdown.ShutdownMode.Exit;
                    Settings.FinalAction = ending;
                }
            }
        }

        //Сохранение лога по даблклику
        private void tbxLog_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string logfilename = m.outfilepath + ".encoding.log";
                File.WriteAllText(logfilename, tbxLog.Text + Environment.NewLine, System.Text.Encoding.Default);
                
                SetLog(Environment.NewLine + "This log was saved here: " + logfilename + Environment.NewLine);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message);
            }
        }
	}
}