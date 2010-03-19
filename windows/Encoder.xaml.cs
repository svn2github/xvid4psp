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
        private Massive m;
        private MainWindow p;
        private string encodertext;
        private bool IsAborted = false;
        private bool IsErrors = false;
        private string estimated = Languages.Translate("estimated");

        private System.Timers.Timer timer;
        private int of = 0;
        private int cf = 0;
        private DateTime ot;
        private double et = 0.0;
        private int step = 0;
        private int steps = 0;
        private double fps = 0.0;
        private double ofps = 0.0;
        private double encoder_fps = 0.0;
        //private string enc_fps = "";
        private string busyfile;
        private DateTime start_time;
        private Shutdown.ShutdownMode ending = Settings.FinalAction;

        private AviSynthEncoder avs;

		public Encoder()
		{
			this.InitializeComponent();
		}

        public Encoder(Massive mass, MainWindow parent)
        {
            this.InitializeComponent();

            Owner = mass.owner;

            button_info.Visibility = Visibility.Hidden;
            button_play.Visibility = Visibility.Hidden;

            button_play.Content = Languages.Translate("Play");
            button_info.Content = Languages.Translate("Info");

            Show();

            m = mass.Clone();
            p = parent;

            Format.Muxers muxer = Format.GetMuxer(m);

            //видео кодирование
            steps = m.vpasses.Count;

            //аудио кодирование
            if (m.inaudiostreams.Count > 0)
            {
                if (muxer != Format.Muxers.Disabled)
                    steps++;
                if (m.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass && m.outaudiostreams.Count > 0
                    && ((AudioStream)(m.outaudiostreams[m.outaudiostream])).codec == "AAC")
                    steps++;
            }

            if (m.vencoding == "Disabled")
                steps++;

            //муксинг
            if (muxer == Format.Muxers.pmpavc)
            {
                steps++;
                steps++;
                steps++;
                steps++;
            }
            if (muxer != Format.Muxers.Disabled &&
                muxer != 0 &&
                muxer != Format.Muxers.pmpavc)
                steps++;

            //if (Format.IsDirectRemuxingPossible(m))
            //    steps++;

            //точка отсчёта
            step--;

            //Определяем кол-во кадров (могло измениться из-за трима) 
            //AviSynthReader reader2 = new AviSynthReader();
           // reader2.ParseScript(m.script);
            //m.outframes = reader2.FrameCount;
            //reader2.Close();
           // reader2 = null;
                
            //забиваем
            SetMaximum(m.outframes);
            //SetMaximum(m.duration.TotalSeconds);

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

        private void CreateBackgoundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        //private void ResetCounter()
        //{
        //    of = 0;
        //    cf = 0;
        //}

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                //AbortAction();
                Close();
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }


        private void AbortAction()
        {
            IsAborted = true;
            locker.Set();

            if (encoderProcess != null)
            {
                if (!encoderProcess.HasExited)
                {
                    encoderProcess.Kill();
                    encoderProcess.WaitForExit();
                }
            }

            if (avs != null && avs.IsBusy())
                avs.stop();

            p.outfiles.Remove(m.outfilepath);

            //if (IsAborted)
            //    p.UpdateTaskStatus(m.key, "Waiting");

            //Close();
        }

        private void button_pause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (button_pause.Content.ToString() == Languages.Translate("Pause"))
            {
                locker.Reset();
                if (avs != null && avs.IsBusy())
                    avs.pause();
                button_pause.Content = Languages.Translate("Resume");
                button_pause.ToolTip = Languages.Translate("Resume encoding");
            }
            else
            {
                locker.Set();
                if (avs != null && avs.IsBusy())
                    avs.resume();
                button_pause.Content = Languages.Translate("Pause");
                button_pause.ToolTip = Languages.Translate("Pause encoding");
            }
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
                AbortAction();
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void make_thm()
        {
            string thmpath = Calculate.RemoveExtention(m.outfilepath) + "jpg";

            AviSynthReader reader = new AviSynthReader();
            reader.ParseScript(m.script);

            Bitmap bmp = new Bitmap(160, 120);
            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImage(reader.ReadFrameBitmap(m.thmframe), 0, 0, 160, 120);
            bmp.Save(thmpath, System.Drawing.Imaging.ImageFormat.Jpeg);

            //завершение
            g.Dispose();
            bmp.Dispose();
            reader.Close();
        }

        private void make_png()
        {
            string thmpath = Calculate.RemoveExtention(m.outfilepath) + "png";

            AviSynthReader reader = new AviSynthReader();
            reader.ParseScript(m.script);

            Bitmap bmp = new Bitmap(144, 80);
            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImage(reader.ReadFrameBitmap(m.thmframe), 0, 0, 144, 80);
            bmp.Save(thmpath, System.Drawing.Imaging.ImageFormat.Png);

            //завершение
            g.Dispose();
            bmp.Dispose();
            reader.Close();
        }

        private void make_x264()
        {
            //прописываем интерлейс флаги
            if (m.interlace == SourceType.FILM ||
                m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                m.interlace == SourceType.INTERLACED)
            {
                if (m.deinterlace == DeinterlaceType.Disabled)
                {
                    string forfer = " --tff";
                    if (m.fieldOrder == FieldOrder.BFF)
                        forfer = " --bff";

                    ArrayList newclis = new ArrayList();
                    foreach (string cli in m.vpasses)
                    {
                        string newclie = cli;
                        //newclie = newclie + " --interlaced";
                        newclie = newclie + forfer;
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
                    newclis.Add(cli.Replace("--size " + targetsize, "--bitrate " + bitrate));
                m.vpasses = (ArrayList)newclis.Clone();
            }

            step++;

            if (m.outvideofile == null)
                m.outvideofile = Settings.TempPath + "\\" + m.key + ".264";

            Format.Muxers muxer = Format.GetMuxer(m);
            if (muxer == Format.Muxers.ffmpeg) //ffmpeg криво муксит raw-avc, нужно кодировать в контейнер
            {
                if (Path.GetExtension(m.outfilepath).ToLower() != ".avi") //А в АВИ наоборот, криво муксит из контейнера.. :(
                    m.outvideofile = Calculate.RemoveExtention(m.outvideofile, true) + ".mp4";
            }
            if (muxer == Format.Muxers.Disabled) //Если можно кодировать сразу в контейнер
            {
                m.outvideofile = m.outfilepath;
                m.dontmuxstreams = true;
            }
            else if (File.Exists(m.outvideofile)) //подхватываем готовый файл
            {
                if (File.Exists(m.outvideofile) &&
                    new FileInfo(m.outvideofile).Length != 0)
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile +
                        Environment.NewLine);
                    if (m.vpasses.Count == 2)
                    {
                        step++;
                        step++;
                    }
                    if (m.vpasses.Count == 3)
                    {
                        step++;
                        step++;
                        step++;
                    }
                    return;
                }
            }

            busyfile = Path.GetFileName(m.outvideofile);

            string passlog = Calculate.RemoveExtention(m.outvideofile) + "log";

            //info строка
            SetLog("Encoding video to: " + m.outvideofile);
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                m.outvinfo = m.outvcodec + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " " + m.outresw + "x" + m.outresh + " " +
                    m.outframerate + "fps (" + m.outframes + " frames)";
            //else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
            //    m.encodingmode == Settings.EncodingModes.ThreePassSize)
            //    m.outvinfo = m.outvcodec + " " + m.outvbitrate + "mb " + m.outresw + "x" + m.outresh + " " +
            //        m.outframerate + "fps (" + m.outframes + " frames)";
            else
                m.outvinfo = m.outvcodec + " " + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " +
                    m.outframerate + "fps (" + m.outframes + " frames)";
            SetLog(m.outvinfo);
            if (m.vpasses.Count > 1) SetLog("\r\n...first pass...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            string arguments; //начало создания коммандной строчки для x264-го

            if (m.format == Format.ExportFormats.PmpAvc)
                info.FileName = Calculate.StartupPath + "\\apps\\x264_pmp\\x264.exe";
            else
                info.FileName = Calculate.StartupPath + "\\apps\\x264\\" + ((Settings.Use64x264) ? "avs4x264.exe" : "x264.exe");

            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string psnr = (Settings.x264_PSNR) ? " --psnr" : "";
            string ssim = (Settings.x264_SSIM) ? " --ssim" : "";

            arguments = m.vpasses[0] + psnr + ssim;

            //прописываем sar
            string sar = "";
            if (!m.vpasses[m.vpasses.Count - 1].ToString().Contains(" --sar "))
            {
                if (m.sar != null || m.IsAnamorphic) sar = " --sar " + m.sar;
                else sar = " --sar 1:1";
            }
            arguments += sar;

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

            //Вывод аргументов коммандной строки в лог, если эта опция включена
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog((Settings.Use64x264 ? "x264_64.exe: " : "x264.exe: ") + info.Arguments);
                SetLog(" ");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            string pat = @"(\d+)/(\d+).frames,.(\d+.\d+).fps"; //@"(\d+)/(\d+)"
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            //первый проход
            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardError.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success == true)
                    {
                        worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        encoder_fps = Calculate.ConvertStringToDouble(mat.Groups[3].Value);
                    }
                    else
                    {
                        if (line.StartsWith("encoded"))
                        {
                            SetLog("x264 [total]: " + line);
                        }
                        else
                        {
                            if (encodertext != null) encodertext += Environment.NewLine;
                            encodertext += line;
                            SetLog(line);
                        }
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            SetLog(encoderProcess.StandardError.ReadToEnd());

            //обнуляем прогресс
            encoder_fps = of = cf = 0;

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
            }

            encodertext = null;

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
                arguments = m.vpasses[1] + psnr + ssim + " --stats \"" + passlog + "\"";//

                //прописываем sar
                arguments += sar;

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

                //прописываем аргументы команндной строки
                SetLog(" ");
                if (Settings.ArgumentsToLog)
                {
                    SetLog((Settings.Use64x264 ? "x264_64.exe: " : "x264.exe: ") + info.Arguments);
                    SetLog(" ");
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                            encoder_fps = Calculate.ConvertStringToDouble(mat.Groups[3].Value);
                        }
                        else
                        {
                            if (line.StartsWith("encoded"))
                            {
                                SetLog("x264 [total]: " + line);
                            }
                            else
                            {
                                if (encodertext != null) encodertext += Environment.NewLine;
                                encodertext += line;
                                SetLog(line);
                            }
                        }
                    }
                }

                //Дочитываем остатки лога, если что-то не успело считаться
                SetLog(encoderProcess.StandardError.ReadToEnd());

                //обнуляем прогресс
                encoder_fps = of = cf = 0;

                //Отлавливаем ошибку по ErrorLevel
                if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
                {
                    IsErrors = true;
                    ErrorExeption(encodertext);
                }

                encodertext = null;
            }

            //третий проход
            if (m.vpasses.Count == 3 && !IsAborted && !IsErrors)
            {
                SetLog("...last pass...");

                step++;
                arguments = m.vpasses[2] + psnr + ssim + " --stats \"" + passlog + "\"";

                //прописываем sar
                arguments += sar;

                info.Arguments = arguments + " --output \"" + m.outvideofile + "\" \"" + m.scriptpath + "\"";

                //прописываем аргументы команндной строки
                SetLog(" ");
                if (Settings.ArgumentsToLog)
                {
                    SetLog((Settings.Use64x264 ? "x264_64.exe: " : "x264.exe: ") + info.Arguments);
                    SetLog(" ");
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                            encoder_fps = Calculate.ConvertStringToDouble(mat.Groups[3].Value);
                        }
                        else
                        {
                            if (line.StartsWith("encoded"))
                            {
                                SetLog("x264 [total]: " + line);
                            }
                            else
                            {
                                if (encodertext != null) encodertext += Environment.NewLine;
                                encodertext += line;
                                SetLog(line);
                            }
                        }
                    }
                }

                //Дочитываем остатки лога, если что-то не успело считаться
                SetLog(encoderProcess.StandardError.ReadToEnd());

                //обнуляем прогресс
                encoder_fps = of = cf = 0;

                //Отлавливаем ошибку по ErrorLevel
                if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
                {
                    IsErrors = true;
                    ErrorExeption(encodertext);
                }

                encodertext = null;
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
                ErrorExeption(Languages.Translate("Can`t find output video file!"));
            }

            SetLog("");

            SafeDelete(passlog);
            SafeDelete(passlog + ".mbtree");
        }

        private void make_ffmpeg()
        {
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
                if (m.dontmuxstreams)
                {
                    if (m.outvcodec == "MPEG2")
                        m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true) + ".m2v";
                    else if (m.outvcodec == "MPEG1")
                        m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true) + ".m1v";
                    else
                        m.outvideofile = Calculate.RemoveExtention(m.outfilepath, true) + ".avi";
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

            //подхватываем готовый файл
            if (File.Exists(m.outvideofile))
            {
                if (File.Exists(m.outvideofile) &&
                    new FileInfo(m.outvideofile).Length != 0)
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile +
                        Environment.NewLine);
                    if (m.vpasses.Count == 2)
                    {
                        step++;
                        step++;
                    }
                    if (m.vpasses.Count == 3)
                    {
                        step++;
                        step++;
                        step++;
                    }
                    return;
                }
            }

            busyfile = Path.GetFileName(m.outvideofile);

            string passlog1 = Calculate.RemoveExtention(m.outvideofile, true) + "_1";
            string passlog2 = Calculate.RemoveExtention(m.outvideofile, true) + "_2";

            //определяем какой это тип задания
            Format.Muxers muxer = Format.GetMuxer(m);

            //блок много процессорного кодирования
            int cpucount = Environment.ProcessorCount;

            //для кодеков без многопроцессорности
            if (m.outvcodec == "MJPEG" ||
                m.outvcodec == "FLV1")
                cpucount = 1;

            //info строка для кодирования с муксингом
            if (muxer != Format.Muxers.Disabled)
            {
                SetLog("Encoding video to: " + m.outvideofile);
                if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.Quantizer ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    m.outvinfo = m.outvcodec + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " " + m.outresw + "x" + m.outresh + " " +
                         m.outframerate + "fps (" + m.outframes + " frames)";
                else
                    m.outvinfo = m.outvcodec + " " + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " +
                        m.outframerate + "fps (" + m.outframes + " frames)";
                SetLog(m.outvinfo);
            }
            //блок для кодирования в один присест
            else
            {
                SetLog("Encoding to: " + m.outfilepath);
                if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.Quantizer ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    m.outvinfo = m.outvcodec + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " " + m.outresw + "x" + m.outresh + " " +
                         m.outframerate + "fps (" + m.outframes + " frames)";
                else
                    m.outvinfo = m.outvcodec + " " + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " +
                        m.outframerate + "fps (" + m.outframes + " frames)";
                SetLog(m.outvinfo);
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec == "MP3" && m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                        m.outainfo = outstream.codec + " Q" + m.mp3_options.quality + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";                    
                    else if (outstream.codec == "FLAC")
                        m.outainfo = outstream.codec + " Q" + m.flac_options.level + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";
                    else
                        m.outainfo = outstream.codec + " " + outstream.bitrate + "kbps " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";
                    SetLog(m.outainfo);
                }            
            }

            if (m.vpasses.Count > 1) SetLog("\r\n...first pass...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            string arguments;

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext = null;

            //фикс для частот которые не принимает ffmpeg
            if (m.outframerate == "23.976" ||
                m.outframerate == "29.970")
            {
                ArrayList newpass = new ArrayList();
                foreach (string p in m.vpasses)
                    newpass.Add("-r " + m.outframerate + " " + p);
                m.vpasses = (ArrayList)newpass.Clone();
            }

            string oldfilepath = m.outvideofile;
            //блок для кодирования в один присест
            if (muxer == Format.Muxers.Disabled)
            {
                m.outvideofile = m.outfilepath;
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    m.vpasses[m.vpasses.Count - 1] = m.vpasses[m.vpasses.Count - 1].ToString().Replace(" -an", " " + outstream.passes);
                }
            }

            arguments = "-threads " + cpucount.ToString() + " " + m.vpasses[0];

            //прописываем аспект
            if (m.sar != null || m.IsAnamorphic)
                arguments += " -aspect " + Calculate.ConvertDoubleToPointString(m.outaspect);

            if (m.vpasses.Count == 1)
                info.Arguments = " -y -i \"" + m.scriptpath + "\" " + arguments + " \"" + m.outvideofile + "\"";
            else
                info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 1 -passlogfile \"" + passlog1 + "\" " + arguments + " \"" + m.outvideofile + "\"";

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe:" + " " + info.Arguments);
                SetLog(" ");
            }

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
                    //SetLog(line);
                    mat = r.Match(line);
                    if (mat.Success == true)
                    {
                        worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
                    }
                }
            }

            //обнуляем прогресс
            of = cf = 0;

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited &&  encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorExeption(encodertext += encoderProcess.StandardError.ReadToEnd());
            }

            encodertext = null;

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
                if (m.sar != null || m.IsAnamorphic)
                    arguments += " -aspect " + Calculate.ConvertDoubleToPointString(m.outaspect);

                if (m.vpasses.Count == 2)
                    info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 2 -passlogfile \"" + passlog1 + "\" " + arguments + " \"" + m.outvideofile + "\"";
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

                //прописываем аргументы команндной строки
                SetLog(" ");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("ffmpeg.exe:" + " " + info.Arguments);
                    SetLog(" ");
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                        }
                        else
                        {
                            if (encodertext != null)
                                encodertext += Environment.NewLine;
                            encodertext += line;
                        }
                    }
                }

                //обнуляем прогресс
                of = cf = 0;

                //Отлавливаем ошибку по ErrorLevel
                if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
                {
                    IsErrors = true;
                    ErrorExeption(encodertext += encoderProcess.StandardError.ReadToEnd());
                }

                encodertext = null;
            }

            //третий проход
            if (m.vpasses.Count == 3 && !IsAborted && !IsErrors)
            {
                SetLog("...last pass...");

                step++;
                arguments = "-threads " + cpucount.ToString() + " " + m.vpasses[2];

                //прописываем аспект
                if (m.sar != null || m.IsAnamorphic)
                    arguments += " -aspect " + Calculate.ConvertDoubleToPointString(m.outaspect);

                info.Arguments = " -y -i \"" + m.scriptpath + "\" -pass 2 -passlogfile \"" + passlog1 + "\" " + arguments + " \"" + m.outvideofile + "\"";

                //прописываем аргументы команндной строки
                SetLog(" ");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("ffmpeg.exe:" + " " + info.Arguments);
                    SetLog(" ");
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                        }
                        else
                        {
                            if (encodertext != null)
                                encodertext += Environment.NewLine;
                            encodertext += line;
                        }
                    }
                }

                //обнуляем прогресс
                of = cf = 0;

                //Отлавливаем ошибку по ErrorLevel
                if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
                {
                    IsErrors = true;
                    ErrorExeption(encodertext += encoderProcess.StandardError.ReadToEnd());
                }

                encodertext = null;
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
                ErrorExeption(Languages.Translate("Can`t find output video file!"));
            }

            //возвращаем путь
            m.outvideofile = oldfilepath;

            SafeDelete(passlog1 + "-0.log");
            SafeDelete(passlog2 + "-0.log");

            SetLog(" ");
        }

        private void make_XviD()
        {
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
                        newclie = newclie + " -interlaced " + fieldorder;
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
                    newclis.Add(cli.Replace("-size " + targetsize + "000", "-bitrate " + bitrate + "000"));
                m.vpasses = (ArrayList)newclis.Clone();

                //SetLog("TargetSize: " + Calculate.ConvertDoubleToPointString(targetsize, 1) + " mb");
                //SetLog("VBitrate: " + bitrate + " kbps");
                //if (m.outaudiostreams.Count > 0)
                //{
                //    AudioStream s = (AudioStream)m.outaudiostreams[m.outaudiostream];
                //    SetLog("ABitrate: " + s.bitrate + " kbps"); 
                //}
            }

            //FOURCC
            if (m.XviD_options.fourcc != "XVID")
            {
                ArrayList newclis = new ArrayList();
                foreach (string cli in m.vpasses)
                {
                    newclis.Add(cli.Replace(" -fourcc " + m.XviD_options.fourcc, ""));
                }
                //передаём обновленные параметры
                m.vpasses = (ArrayList)newclis.Clone();
            }

            //Zones
            if (m.XviD_options.cartoon ||
                m.XviD_options.grey)
            {
                ArrayList newclis = new ArrayList();
                foreach (string cli in m.vpasses)
                {
                    string sline = cli.Replace("-5G/1000", "-5G/" + m.outframes);
                    sline = sline.Replace("-5C/1000", "-5C/" + m.outframes);
                    sline = sline.Replace("-5GC/1000", "-5GC/" + m.outframes);
                    newclis.Add(sline);
                }
                //передаём обновленные параметры
                m.vpasses = (ArrayList)newclis.Clone();
            }

            step++;

            if (m.outvideofile == null)
                m.outvideofile = Settings.TempPath + "\\" + m.key + ".avi";

            //подхватываем готовый файл
            if (File.Exists(m.outvideofile))
            {
                if (File.Exists(m.outvideofile) &&
                    new FileInfo(m.outvideofile).Length != 0)
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile +
                        Environment.NewLine);
                    if (m.vpasses.Count == 2)
                    {
                        step++;
                        step++;
                    }
                    if (m.vpasses.Count == 3)
                    {
                        step++;
                        step++;
                        step++;
                    }
                    return;
                }
            }

            busyfile = Path.GetFileName(m.outvideofile);

            string passlog1 = Calculate.RemoveExtention(m.outvideofile, true) + "_1.log";
            string passlog2 = Calculate.RemoveExtention(m.outvideofile, true) + "_2.log";

            //узнаём колличество процессоров
            int cpucount = Environment.ProcessorCount + 2; //для максимальной производительности запускаем на два потока больше

            //info строка
            SetLog("Encoding video to: " + m.outvideofile);
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                 m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                m.outvinfo = m.outvcodec + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + " " + m.outresw + "x" + m.outresh + " " +
                     m.outframerate + "fps (" + m.outframes + " frames)";
            //else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
            //     m.encodingmode == Settings.EncodingModes.ThreePassSize)
            //    m.outvinfo = m.outvcodec + " " + m.outvbitrate + "mb " + m.outresw + "x" + m.outresh + " " +
            //        m.outframerate + "fps (" + m.outframes + " frames)";
            else
                m.outvinfo = m.outvcodec + " " + m.outvbitrate + "kbps " + m.outresw + "x" + m.outresh + " " +
                    m.outframerate + "fps (" + m.outframes + " frames)";
            SetLog(m.outvinfo);
            if (m.vpasses.Count > 1) SetLog("\r\n...first pass...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            string arguments;

            info.FileName = Calculate.StartupPath + "\\apps\\xvid_encraw\\xvid_encraw.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            arguments = m.vpasses[0] + " -threads " + cpucount.ToString();//

            //прописываем sar
            if (m.sar != null || m.IsAnamorphic)
                arguments += " -par " + m.sar;
            //else
            //    arguments += " -par 1:1";

            if (m.vpasses.Count == 1)
                info.Arguments = arguments + " -avi \"" + m.outvideofile + "\" -i \"" + m.scriptpath + "\"";
            //-vbvsize 6291456 -vbvmax 9708.400 -vbvpeak 16000000 HDTV профиль, похожe что не влияет на quality mode
            //else if (m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
            //         m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            //{
            //    arguments = "-pass1 \"" + passlog1 + "\" " + arguments;
            //    info.Arguments = arguments + " -i \"" + m.scriptpath + "\" -o \"" + m.outvideofile + "\"";
            //}
            else if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                //arguments = "-pass1 \"" + passlog1 + "\" " + arguments;
                info.Arguments = arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";
            }
            else
            {
                arguments = "-pass1 \"" + passlog1 + "\" " + arguments;
                info.Arguments = arguments + " -i \"" + m.scriptpath + "\" -o NUL";
            }

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("xvid_encraw.exe:" + " " + info.Arguments);
                SetLog(" ");
            }
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            //-progress 10
            //961 frames( 96%) encoded,  39.45 fps, Average Bitrate =   516kbps
            //string pat = @"(\d+)\Dframes";

            string line;
            string pat = @"(\d+):\Dkey=";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            //первый проход
            while (!encoderProcess.HasExited)
            {
                locker.WaitOne();
                line = encoderProcess.StandardOutput.ReadLine();
                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success == true)
                        worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                    //SetLog(line);
                    //else
                    //{
                    //    if (encodertext != null)
                    //        encodertext += Environment.NewLine;
                    //    encodertext += line;
                    //    SetLog(line);
                    //}
                }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //проверка на завершение
            if (!encoderProcess.HasExited)
            {
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
            }

            //ResetCounter();
            if (IsAborted || IsErrors) return;

            //второй проход
            if (m.vpasses.Count > 1)
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
                    SetLog(Languages.Translate("Best bitrate for") + " Q" + Calculate.ConvertDoubleToPointString((double)m.outvbitrate , 1) + ": " + vbitrate + "kbps");
                    if (vbitrate > maxbitrate)
                    {
                        vbitrate = maxbitrate;
                        SetLog(Languages.Translate("But it`s more than maximum bitrate") + ": " + vbitrate + "kbps");
                    }
                }

                if (m.vpasses.Count == 2) SetLog("...last pass...");
                else if (m.vpasses.Count == 3) SetLog("...second pass...");

                step++;
                arguments = m.vpasses[1] + " -threads " + cpucount.ToString();//

                //прописываем sar
                if (m.sar != null || m.IsAnamorphic)
                    arguments += " -par " + m.sar;
                //else
                //    arguments += " -par 1:1";

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

                //прописываем аргументы команндной строки
                SetLog(" ");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("xvid_encraw.exe:" + " " + info.Arguments);
                    SetLog(" ");
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {

                        mat = r.Match(line);
                        if (mat.Success == true)
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        //else
                        //{
                        //    if (encodertext != null)
                        //        encodertext += Environment.NewLine;
                        //    encodertext += line;
                        //    SetLog(line);
                        //}
                    }
                }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //проверка на завершение
            if (!encoderProcess.HasExited)
            {
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
            }

            //ResetCounter();
            if (IsAborted || IsErrors) return;

            //третий проход
            if (m.vpasses.Count == 3)
            {
                SetLog("...last pass...");

                step++;
                arguments = m.vpasses[2] + " -threads " + cpucount.ToString();//

                //прописываем sar
                if (m.sar != null || m.IsAnamorphic)
                    arguments += " -par " + m.sar;
                //else
                //    arguments += " -par 1:1";

                if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    info.Arguments = "-pass2 \"" + passlog1 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";
                else
                    info.Arguments = "-pass2 \"" + passlog2 + "\" " + arguments + " -i \"" + m.scriptpath + "\" -avi \"" + m.outvideofile + "\"";

                //прописываем аргументы команндной строки
                SetLog(" ");
                if (Settings.ArgumentsToLog)
                {
                    SetLog("xvid_encraw.exe:" + " " + info.Arguments);
                    SetLog(" ");
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        //else
                        //{
                        //    if (encodertext != null)
                        //        encodertext += Environment.NewLine;
                        //    encodertext += line;
                        //    SetLog(line);
                        //}
                    }
                }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //проверка на завершение
            if (!encoderProcess.HasExited)
            {
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outvideofile) ||
                new FileInfo(m.outvideofile).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;

            SetLog("");

            SafeDelete(passlog1);
            SafeDelete(passlog2);

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
            if (Format.IsDirectRemuxingPossible(m) && outstream.codec == "Copy") return;

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

            //подхватываем готовый файл
            if (outstream.codec == "Copy" &&
                File.Exists(instream.audiopath))
            {
                //if (instream.audiopath.Contains(m.key))
                //    outstream.audiopath = instream.audiopath;
                //else
                //    File.Copy(instream.audiopath, outstream.audiopath);
                outstream.audiopath = instream.audiopath;
            }

            if (m.vencoding != "Disabled")
            {
                if (File.Exists(outstream.audiopath) &&
                    new FileInfo(outstream.audiopath).Length != 0)
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + outstream.audiopath +
                           Environment.NewLine);
                    return;
                }
            }

            //Извлекаем звук для Copy
            if (outstream.codec == "Copy" &&
                !File.Exists(instream.audiopath))
            {
                Format.Demuxers dem = Format.GetDemuxer(m);
                if (aext == ".wav") ExtractSound();
                else if (dem == Format.Demuxers.mkvextract) demux_mkv(Demuxer.DemuxerMode.ExtractAudio);
                else if (dem == Format.Demuxers.pmpdemuxer) demux_pmp(Demuxer.DemuxerMode.ExtractAudio);
                else if (dem == Format.Demuxers.mp4box) demux_mp4box(Demuxer.DemuxerMode.ExtractAudio);
                else demux_ffmpeg(Demuxer.DemuxerMode.ExtractAudio);
                return;
            }

            if (m.dontmuxstreams)
                outstream.audiopath = Calculate.RemoveExtention(m.outfilepath, true) + Path.GetExtension(outstream.audiopath);

            if (Path.GetExtension(outstream.audiopath) == ".aac")
                outstream.audiopath = Calculate.RemoveExtention(outstream.audiopath, true) + ".m4a"; //Подменяем расширение для кодирования ААС (NeroEncAac кодирует в .m4a)

            busyfile = Path.GetFileName(outstream.audiopath);

            bool TwoPassAAC = false;
            SetLog("Encoding audio to: " + outstream.audiopath);
            if (outstream.codec == "MP3" && m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                m.outainfo = outstream.codec + " Q" + m.mp3_options.quality + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";
            else if (outstream.codec == "FLAC")
                m.outainfo = outstream.codec + " Q" + m.flac_options.level + " " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";
            else if (outstream.codec == "AAC" && m.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass)
            {
                m.outainfo = outstream.codec + " " + outstream.bitrate + "kbps 2pass " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";
                TwoPassAAC = true;
            }
            else m.outainfo = outstream.codec + " " + outstream.bitrate + "kbps " + outstream.channels + "ch " + outstream.bits + "bit " + outstream.samplerate + "khz";
            SetLog(m.outainfo);

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

                if (outstream.codec == "MP3")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\lame\\lame.exe";
                else if (outstream.codec == "AAC")
                {
                    if (m.format == Format.ExportFormats.PmpAvc)
                        avs.encoderPath = Calculate.StartupPath + "\\apps\\neroAacEnc_pmp\\neroAacEnc.exe";
                    else avs.encoderPath = Calculate.StartupPath + "\\apps\\neroAacEnc\\neroAacEnc.exe";
                }
                else if (outstream.codec == "MP2" || outstream.codec == "PCM" || outstream.codec == "LPCM" || outstream.codec == "FLAC")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                else if (outstream.codec == "AC3")
                    avs.encoderPath = Calculate.StartupPath + "\\apps\\aften\\aften.exe";

                //запускаем кодер
                avs.start();

                //прописываем аргументы командной строки
                while (avs.args == null && !avs.IsErrors) Thread.Sleep(50);
                SetLog(" ");

                if (Settings.ArgumentsToLog)
                {
                    SetLog(Path.GetFileName(avs.encoderPath) + ": " + avs.args);
                    SetLog(" ");
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
                of = cf = 0;

                //чистим ресурсы
                avs = null;
            }

            SetLog("");

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outstream.audiopath) || new FileInfo(outstream.audiopath).Length == 0)
            {
                IsErrors = true;
                throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }

            if (m.format == Format.ExportFormats.M2TS && outstream.codec == "AAC" ||
                m.format == Format.ExportFormats.TS && outstream.codec == "AAC" ||
                m.format == Format.ExportFormats.BluRay && outstream.codec == "AAC")
                make_aac();
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
            encodertext = null;

            info.Arguments = outstream.passes + " -if \"" + outstream.nerotemp + "\" -of \"" + outstream.audiopath + "\"";

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            SetLog(" ");
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
                    if (mat.Success == true)
                    {
                        if (oldpass == 0 && mat.Groups[1].Value == "First")
                        {
                            SetLog("...first pass...");
                            oldpass = 1;
                        }
                        else if (oldpass == 1 && mat.Groups[1].Value == "Second")
                        {
                            SetLog("...last pass...");
                            of = cf = 0;
                            oldpass = 2;
                            step++;
                        }
                        worker.ReportProgress((int)(Convert.ToInt32(mat.Groups[2].Value) * fps));
                    }
                    else
                    {
                        if (encodertext != null) encodertext += Environment.NewLine;
                        encodertext += line;
                    }
                }
            }

            //обнуляем прогресс
            of = cf = 0;

            //Удаляем временный WAV-файл
            if (outstream.nerotemp != m.infilepath && Path.GetDirectoryName(outstream.nerotemp) == Settings.TempPath)
                SafeDelete(outstream.nerotemp);

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.ExitCode != 0 && !IsAborted)
            {
                ErrorExeption(encodertext + "\r\n" + encoderProcess.StandardError.ReadToEnd() + "\r\n" + encoderProcess.StandardOutput.ReadToEnd());
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            encodertext = null;
        }

        private void ExtractSound()
        {
            //определяем аудио потоки
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

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
                ErrorExeption(avs.error_text);                
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
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext = null;

            string format = "";

            string outfile = "";

            if (dmode == Demuxer.DemuxerMode.ExtractAudio)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                busyfile = Path.GetFileName(outstream.audiopath);
                step++;

                outfile = outstream.audiopath;

                string forceformat = "";
                string outext = Path.GetExtension(outfile);
                if (outext == ".lpcm" ||
                    instream.codec == "LPCM")
                    forceformat = " -f s16be";

                info.Arguments = "-map 0." + instream.ffid + " -i \"" + m.infilepath +
                    "\" -vn -acodec copy" + forceformat  + " \"" + outfile + "\"";

                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
                
                if (Settings.ArgumentsToLog)
                {
                    SetLog("ffmpeg.exe: " + info.Arguments + Environment.NewLine);
                }           
            }

            if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                if (m.outvideofile == null)
                    m.outvideofile = Settings.TempPath + "\\" + m.key + "." + Format.GetValidRAWVideoEXT(m);

                //подхватываем готовый файл
                if (File.Exists(m.outvideofile))
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile +
                    Environment.NewLine);
                    if (m.vpasses.Count >= 2)
                        step++;
                    if (m.vpasses.Count == 3)
                    {
                        step++;
                        step++;
                    }
                    return;
                }

                busyfile = Path.GetFileName(m.outvideofile);

                SetLog("Demuxing video stream to: " + m.outvideofile);
                SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                    Environment.NewLine);

                string outext = Path.GetExtension(m.outvideofile).ToLower();
                if (outext == ".264" ||
                    outext == ".h264")
                    format = " -f h264";

                //if (outext == ".m2v")
                //    format = " -f vob";

                outfile = m.outvideofile;
                info.Arguments = "-i \"" + m.infilepath +
                    "\" -vcodec copy -an" + format + " \"" + outfile + "\"";
                
                if (Settings.ArgumentsToLog)
                {
                    SetLog("ffmpeg.exe: " + info.Arguments + Environment.NewLine);
                }
            }

            //удаляем старый файл
            SafeDelete(outfile);

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"time=(\d+.\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            int procent = 0;

            int outframes = m.outframes;
            m.outframes = m.inframes;
            SetMaximum(m.outframes);
            //первый проход
            while (!encoderProcess.HasExited)
            {
                line = encoderProcess.StandardError.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success == true)
                    {
                        double ctime = Calculate.ConvertStringToDouble(mat.Groups[1].Value);
                        procent = (int)(ctime * Calculate.ConvertStringToDouble(m.inframerate));
                        worker.ReportProgress(procent);
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
                    }
                }
            }
            m.outframes = outframes;
            SetMaximum(m.outframes);

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorExeption(encodertext += encoderProcess.StandardError.ReadToEnd());
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
            }

            encodertext = null;
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
                step++;

                outfile = outstream.audiopath;
                info.Arguments = "-raw " + instream.mkvid + " \"" + m.infilepath + "\" -out \"" + outfile + "\"";

                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
                
                if (Settings.ArgumentsToLog)
                {
                    SetLog("mp4box.exe: " + info.Arguments + Environment.NewLine);
                }
            }

            if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                if (m.outvideofile == null)
                    m.outvideofile = Settings.TempPath + "\\" + m.key + "." + Format.GetValidRAWVideoEXT(m);

                //подхватываем готовый файл
                if (File.Exists(m.outvideofile))
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile +
                    Environment.NewLine);
                    if (m.vpasses.Count >= 2)
                        step++;
                    if (m.vpasses.Count == 3)
                    {
                        step++;
                        step++;
                    }
                    return;
                }

                busyfile = Path.GetFileName(m.outvideofile);

                SetLog("Demuxing video stream to: " + m.outvideofile);
                SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                    Environment.NewLine);

                outfile = m.outvideofile;
                info.Arguments = "-raw " + m.invideostream_mkvid + " \"" + m.infilepath + "\" -out \"" + outfile + "\"";

                if (Settings.ArgumentsToLog)
                {
                    SetLog("mp4box.exe: " + info.Arguments + Environment.NewLine);
                }
            }

            //удаляем старый файл
            SafeDelete(outfile);

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"(\d+)/(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            int procent = 0;

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
                    if (mat.Success == true)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
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

            //проверка на удачное завершение
            if (!File.Exists(outfile) ||
                new FileInfo(outfile).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
            }

            encodertext = null;
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

            busyfile = Path.GetFileName(m.infilepath);
            step++;
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
                    if (mat.Success == true)
                    {
                        worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
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
                ErrorExeption(encodertext);
            }

            if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                if (outstream.codec == "Copy")
                {
                    if (new FileInfo(outstream.audiopath).Length == 0)
                    {
                        IsErrors = true;
                        ErrorExeption(encodertext);
                    }
                }
                else
                    File.Delete(outstream.audiopath);
            }

            encodertext = null;

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
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string outfile = "";

            if (dmode == Demuxer.DemuxerMode.ExtractAudio)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                busyfile = Path.GetFileName(outstream.audiopath);
                step++;

                outfile = outstream.audiopath;
                info.Arguments = "tracks " + flist + instream.mkvid + ":" + "\"" + outfile + "\"";

                SetLog("Demuxing audio stream to: " + outstream.audiopath);
                SetLog(instream.codecshort + " " + instream.bitrate + "kbps " + instream.channels + "ch " + instream.bits + "bit " + instream.samplerate + "khz" +
                    Environment.NewLine);
                
                if (Settings.ArgumentsToLog)
                {
                    SetLog("mkvextract.exe: " + info.Arguments + Environment.NewLine);
                }
            }

            if (dmode == Demuxer.DemuxerMode.ExtractVideo)
            {
                if (m.outvideofile == null)
                    m.outvideofile = Settings.TempPath + "\\" + m.key + "." + Format.GetValidRAWVideoEXT(m);

                //подхватываем готовый файл
                if (File.Exists(m.outvideofile))
                {
                    SetLog(Languages.Translate("Using already created file") + ": " + m.outvideofile +
                    Environment.NewLine);
                    if (m.vpasses.Count >= 2)
                        step++;
                    if (m.vpasses.Count == 3)
                    {
                        step++;
                        step++;
                    }
                    return;
                }

                busyfile = Path.GetFileName(m.outvideofile);
                step++;

                SetLog("Demuxing video stream to: " + m.outvideofile);
                SetLog(m.inresw + "x" + m.inresh + " " + m.invcodecshort + " " + m.invbitrate + "kbps " + m.inframerate + "fps" +
                    Environment.NewLine);

                outfile = m.outvideofile;
                info.Arguments = "tracks " + flist + m.invideostream_mkvid + ":" + "\"" + outfile + "\"";
                
                if (Settings.ArgumentsToLog)
                {
                    SetLog("mkvextract.exe: " + info.Arguments + Environment.NewLine);
                }
            }

            //удаляем старый файл
            SafeDelete(outfile);

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"progress:\D(\d+)%";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            int procent = 0;

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
                    if (mat.Success == true)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
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

            //проверка на удачное завершение
            if (!File.Exists(outfile) ||
                new FileInfo(outfile).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
            }

            encodertext = null;
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
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            
            //запоминаем настоящее имя
            //не помню зачем такие сложности
            //string savefilepath = m.outfilepath;
            //m.outfilepath = Path.GetDirectoryName(m.outfilepath) + "\\" + m.key + Path.GetExtension(m.outfilepath);
            //SafeDelete(m.outfilepath);

            string rate = "-fps " + m.outframerate + " ";
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate != null)
                    rate = "-fps " + m.inframerate + " ";
                else
                    rate = "";
            }

            string ipod = ""; //пробный агрумент для ипода (для больших разрешений и 5.5G его все-равно не достаточно)
            if (!old_muxer && (m.format == Format.ExportFormats.Mp4iPod50G || m.format == Format.ExportFormats.Mp4iPod55G))
                ipod = "-ipod ";

            string addaudio = "";
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                addaudio = " -add \"" + outstream.audiopath + "\"";
            }
            
            info.Arguments = ipod + rate +
                " -add \"" + m.outvideofile + "\"" + addaudio + " -new \"" + m.outfilepath + "\" -tmp \"" + Settings.TempPath + "\"";

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog((old_muxer ? "NicMP4Box.exe: " : "MP4Box.exe: ") + info.Arguments);
                SetLog(" ");
            }
            
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
                    if (mat.Success == true)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
                    }
                }
            }

            //обнуляем прогресс
            of = cf = 0;

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
            }
            
            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;

            //переименовываем файл на настоящий и передаём переменную
            //ну не помню я зачем я это делал :( - пролема длинных имён - похоже исправлена
            //File.Move(m.outfilepath, savefilepath);
            //m.outfilepath = savefilepath;

            SetLog("");
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

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("mp4box.exe:" + " " + info.Arguments);
                SetLog(" ");
            }

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
                    if (mat.Success == true)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
                        //    SetLog(line);
                    }
                }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(aacfile) ||
                new FileInfo(aacfile).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }
            else
            {
                //передаём новый файл
                SafeDelete(outstream.audiopath);
                outstream.audiopath = aacfile;
            }

            encodertext = null;

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

            ////прописываем аргументы команндной строки
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
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            SetLog("");
        }

        private void muxer_ProgressChanged(double progress)
        {
            //получаем текущий фрейм 
            cf = (int)((progress / 100.0) * (double)m.outframes);
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
                    SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext = null;

            string rate = " -r " + m.outframerate;
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate != null)
                    rate = " -r " + m.inframerate;
                else
                    rate = "";
            }
    
            string aformat = "";
            string addaudio = "";
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (!File.Exists(outstream.audiopath))
                    addaudio = " -acodec copy";
                else
                    addaudio = " -i \"" + outstream.audiopath + "\"" + aformat + " -acodec copy";
            }

            string format = "";
            if (m.format == Format.ExportFormats.Mpeg2PS ||
                m.format == Format.ExportFormats.Mpeg2PAL ||
                m.format == Format.ExportFormats.Mpeg2NTSC)
                format = " -f vob";
            else if (m.format == Format.ExportFormats.Mp4iPod55G)
                format = " -f ipod"; //Добавляем ipod atom для 5.5G

            info.Arguments = "-i \"" + m.outvideofile + "\" -vcodec copy" + addaudio + format + rate + " \"" + m.outfilepath + "\"";

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe:" + " " + info.Arguments);
                SetLog(" ");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            double fps = Calculate.ConvertStringToDouble(m.outframerate);
            Regex r = new Regex(@"time=(\d+.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
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
                        worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                    }
                    else
                    {
                         if (encodertext != null)
                             encodertext += Environment.NewLine;
                         encodertext += line;
                    }
                }
            }

            //обнуляем прогресс
            of = cf = 0;
            
            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorExeption(encodertext += encoderProcess.StandardError.ReadToEnd());
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
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;

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
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext = null;

            info.Arguments = "-i \"" + m.scriptpath + "\" " + outstream.passes + " -vn \"" + outstream.audiopath + "\"";

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("ffmpeg.exe:" + " " + info.Arguments);
                SetLog(" ");
            }
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            double fps = Calculate.ConvertStringToDouble(m.outframerate);
            Regex r = new Regex(@"time=(\d+.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
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
                        worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                    }
                    else
                    {
                        if (encodertext != null)
                            encodertext += Environment.NewLine;
                        encodertext += line;
                    }
                }
            }

            //обнуляем прогресс
            of = cf = 0;

            //Отлавливаем ошибку по ErrorLevel
            if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && !IsAborted)
            {
                IsErrors = true;
                ErrorExeption(encodertext += encoderProcess.StandardError.ReadToEnd());
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outstream.audiopath) || new FileInfo(outstream.audiopath).Length == 0)
            {
                IsErrors = true;
                throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }

            encodertext = null;
        }

        private void make_mplex()
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
                    SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\mplex\\mplex.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string addaudio = "";
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                addaudio = " \"" + outstream.audiopath + "\"";
            }

            string format = " -f 3";
            if (m.format == Format.ExportFormats.Mpeg2PAL ||
                m.format == Format.ExportFormats.Mpeg2NTSC)
                format = " -f 9";

            info.Arguments = "-o \"" + m.outfilepath + "\" \"" + m.outvideofile + "\" " + addaudio + format;

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("mplex.exe:" + " " + info.Arguments);
                SetLog(" ");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            double fps = Calculate.ConvertStringToDouble(m.outframerate);
            Regex r = new Regex(@"time=(\d+.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
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
                        worker.ReportProgress((int)(Calculate.ConvertStringToDouble(mat.Groups[1].Value) * fps));
                    }
                    else
                    {
                        if (line.StartsWith("**ERROR"))
                        {
                            if (encodertext != null)
                                encodertext += Environment.NewLine;
                            encodertext += line;
                        }
                        if (Settings.ArgumentsToLog)
                            SetLog(line);
                    }
                }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            //проверка на ошибки в логе
            if (encodertext != null && encodertext.Contains("error,"))
                IsErrors = true;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;

            SetLog("");
        }

        private void make_tsmuxer()
        {
            //MUXOPT --no-pcr-on-video-pid --new-audio-pes --vbr 
            //V_MPEG4/ISO/AVC, D:\media\test\stream.h264, fps=25
            //A_AC3, D:\media\test\stream.ac3, timeshift=-10000ms

            //Наименования кодеков в meta файле:
            //V_MPEG4/ISO/AVC - H264
            //V_MS/VFW/WVC1   - VC1
            //V_MPEG-2        - MPEG2

            //A_AC3 - DD (AC3) / DD+ (E-AC3) / True HD (True HD только для дорожек с AC3 core внутри).
            //A_AAC - AAC
            //A_DTS - DTS / DTS-HD
            //A_MP3 - MPEG audio layer 1/2/3
            //A_LPCM - raw pcm data or PCM WAVE file

            //S_PGS - сабтитры в формате Presentation Graphic Stream.

            //Дополнительные параметы аудио/видео дорожек:
            //fps - для видеодорожки H264 можно явно задать значение fps (см. пример выше). Если fps не указан, он начитывается из потока.

            //level - позволяет перезаписать поле level в потоке H264. Например, можно изменить профиль High@5.1 на High@4.1.
            //Следует иметь ввиду, что обновляется только заголовок. H264 Поток может не удовлетворять требованиям 
            //более низкого level. 

            //insertSEI - параметр используется только для видео H.264. При его включении просиходит следующее: если оригинальный видеопоток не содержит 
            //информации SEI picture timing и SEI buffering period, то такая инфомация добавляется в поток. Этот параметр рекомендуется включать
            //для лучшей совместимости с приставкой Sony Playstation 3.

            //contSPS - параметр используется только для видео H.264. При включении параметра, если оригинальный видеопоток не содержит циклически повторяющихся 
            //элементов SPS/PPS (при импорте из MKV они могут быть записаны только один раз в начале файла), то SPS/PPS будут дополнительно вставляться в поток
            //перед каждым ключевым кадром. Рекомендуется всегда включать этот параметр. 
            //Примечание: для x264 потоков видеоплейер Dune HD не может декодировать повторный SPS элемент, видимо это ошибка в текущей прошивке плейера.


            //timeshift - Для аудио дорожек поддерживается параметр timeshift, может быть как больше, так и меньше нуля. 
            //Значение для timeshift задается в миллисекундах (в конце должно стоять ms) или в секундах (в конце буква s). Этот параметр 
            //позволяет сдвинуть аудиодорожку по времени вперед (положительное значение параметра) или назад.

            //down-to-dts - доступно только для дорожек DTS-HD. Делает преобразование DTS-HD в стандартный DTS.
            //down-to-ac3 - доступно только для дорожек TRUE-HD c ядром AC3 внутри (обычно такие пишут на Blu-ray диски).

            //track - начиная с версии 0.9.96 появилась возможность ссылаться на дорожки, лежащие в других контейнерах. В этом случае нужно указывать
            //номер дорожки внутри контейнера. 
            //Список поддерживаемых контейнеров:
            //- TS/M2TS
            //- EVO/VOB/MPG
            //- MKV

            /*Дополнительные параметры мьюксера в строке MUXOPT.

            Параметры этой группы влият на весь поток в целом, а не на отдельную дорожку. Параметры перечисляются через пробел.

            --pcr-on-video-pid - не выделять отдельный PID для PCR, а использовать существующий video PID.
            --new-audio-pes - использовать байт 0xfd вместо 0xbd для дорожек AC3, True-HD, DTS и DTS-HD. Это соответствует стандарту Blu-ray.
            -vbr - использовать переменный битрейт 
            --minbitrate=xxxx - задает нижнюю границу vbr битрейта. Если поток занимает меньшее количество байт, будут вставляться NULL пакеты
            для забивания потока до нужной полосы.
            --maxbitrate=xxxx - верхняя граница vbr битрейта. 
            --cbr - режим мьюксинга с фиксированным битрейтом. Опции --vbr и --cbr не должны использоваться совместно.
            --vbv-len - длина виртуального буфера в миллисекундах. Значение по умолчанию 500. Обычно этот параметр используется совместно с --cbr.
            Параметр аналогичен значению vbv-buffer-size в кодере x264, но задается не в килобитах, а в миллисекундах (при константном битрейте
            их можно пересчитать друг в друга). Если вы самостоятельно кодировали файл в x264 в режиме константного битрейта, для более
            плавного вещания файла в сеть рекомендуется выставлять такое же (или меньшее) значение этого параметра чем в x264. При переполнении виртуального
            буфера в лог будут выведены соответствующие ошибки.
            --bitrate=xxxx - битрейт для режима мьюксинга с фиксированным битрейтом.
            Значения --maxbitrate, --minbitrate и --bitrate указывается в килобитах в секунду. Можно использовать не целое число, разделитель
            между целой и дробной частью символ точка. Например: --maxbitrate=19423.432
            --no-asyncio - не создавать отдельный поток для записи выходных файлов. Включение этого режима также отменяет флаг FILE_FLAG_NO_BUFFERING.
            Это несколько снижает скорость записи, но позволяет видеть объем выходного файла во время работы.
            --auto-chapters=nn - вставлять главы каждые nn минут. Используется только в режиме blu-ray muxing.
            --custom-chapters=<строка параметров> - вставлять главы в указанных местах. Используется только в режиме blu-ray muxing.
            Строка параметров имеет следующий вид: hh:mm:ss;hh:mm:ss и т.д. Через точку с запятой перечисляются временные метки, в которых
            надо вставить новую главу. Строка не должна содержать пробелов.
            --demux - в этом режиме выбранные аудио/видео треки сохраняются как отдельные файлы. При обработке дорожек на них накладываются
              все выбранные эффекты, например, смена level для h.264. В режиме demux некоторые типы дорожек всегда подвераются изменениям при
              сохранении в файл:
               - Субтитры в формате Presentation graphic stream преобразуются в формат sup
               - PCM аудиодорожки сохраняются в виде WAV файлов. Также происходит автоматическое разбиение на несколько файлов,
                 если размер WAV файла превышает 4Gb.*/

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
                    SetLog("Audio file: " + outstream.audiopath);
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

            string addaudio = "";
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                addaudio = " \"" + outstream.audiopath + "\"";
            }

            //создаём мета файл
            string metapath = Settings.TempPath + "\\" + m.key + ".meta";

            string vcodec = m.outvcodec;
            if (m.outvcodec == "Copy")
                vcodec = m.invcodecshort;

            string vtag = "V_MPEG4/ISO/AVC";
            if (vcodec == "MPEG2")
                vtag = "V_MPEG-2";
            if (vcodec == "VC1")
                vtag = "V_MS/VFW/WVC1";

            string fps = ", fps=" + m.outframerate;
            if (m.outvcodec == "Copy")
                fps = ", fps=" + m.inframerate;
            if (fps == ", fps=")
                fps = "";

            string h264tweak = "";
            if (vtag == "V_MPEG4/ISO/AVC")
                h264tweak = ", level=4.1, insertSEI, contSPS";

            string split = "";
            if (m.format == Format.ExportFormats.BluRay)
            {
                if (m.bluray_type == "FAT32 HDD/MS")
                    split = " --split-size=4000MB";
            }
            else
            {
                if (m.split != null &&
                    m.split != "Disabled")
                {
                    int size = 0;

                    string svalue = Calculate.GetRegexValue(@"(\d+).mb", m.split);
                    if (svalue != null)
                        Int32.TryParse(svalue, NumberStyles.Integer, null, out size);

                    if (size != 0)
                        split = " --split-size=" + size + "MB";
                }
            }

            //контрольная проверка на FAT32
            if (split == "")
            {
                DriveInfo dinfo = new DriveInfo(Path.GetPathRoot(m.outfilepath));
                if (dinfo.DriveFormat == "FAT32")
                {
                    split = " --split-size=4000MB";
                    m.split = "4000 mb FAT32";
                    SetLog("FAT32 detected. Auto 4GB split activated.");
                }
            }

            string bluray = "";
            if (m.format == Format.ExportFormats.BluRay)
                bluray = " --blu-ray --auto-chapters=5";

            string meta = "MUXOPT --no-pcr-on-video-pid --new-audio-pes --vbr --vbv-len=500" + bluray + split + Environment.NewLine;

            string ext = Path.GetExtension(m.infilepath).ToLower();

            //video path
            string vpath = m.outvideofile;
            string vtrack = "";
            if (Format.IsDirectRemuxingPossible(m) &&
                m.outvcodec == "Copy")
            {
                vpath = m.infilepath;
                //if (ext == ".mkv")
                    vtrack = ", track=" + m.invideostream_mkvid + ", lang=eng";
                //else
                //    vtrack = ", track=" + m.invideostream_ffid + ", lang=eng";
            }

            meta += vtag + ", \"" + vpath + "\"" + fps + h264tweak + vtrack + Environment.NewLine;

            if (m.outaudiostreams.Count > 0)
            {
                AudioStream i = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream o = (AudioStream)m.outaudiostreams[m.outaudiostream];

                string acodec = o.codec;
                if (o.codec == "Copy")
                    acodec = i.codecshort;

                string atag = "A_AC3";
                if (acodec == "AAC")
                    atag = "A_AAC";
                if (acodec == "DTS")
                    atag = "A_DTS";
                if (acodec == "MP2" ||
                    acodec == "MP3")
                    atag = "A_MP3";
                if (acodec == "PCM" ||
                    acodec == "LPCM")
                    atag = "A_LPCM";

                //audio path
                string apath = o.audiopath;
                string atrack = "";

                if (File.Exists(o.audiopath))
                {
                    apath = o.audiopath;
                    atrack = ", track=0, lang=eng";
                }
                else
                {
                    if (Format.IsDirectRemuxingPossible(m) &&
                        o.codec == "Copy")
                    {
                        apath = m.infilepath;
                        //if (ext == ".mkv")
                        atrack = ", track=" + i.mkvid;// + ", lang=eng";
                        //else
                        //    atrack = ", track=" + i.ffid;// +", lang=eng";
                    }
                }

                meta += atag + ", \"" + apath + "\"" + atrack;
            }

            //пишем meta в файл
            StreamWriter sw = new StreamWriter(metapath, false, System.Text.Encoding.Default);
            string[] separator = new string[] { Environment.NewLine };
            string[] lines = meta.Split(separator, StringSplitOptions.None);
            foreach (string l in lines)
                sw.WriteLine(l);
            sw.Close();

            info.Arguments = "\"" + metapath + "\" \"" + m.outfilepath + "\"";

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("tsmuxer.exe:" + " " + info.Arguments);
                SetLog(meta);
                SetLog(" ");
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
                    if (mat.Success == true)
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
            of = 0;
            cf = 0;

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            //проверка на ошибки в логе
            if (encodertext != null && encodertext.Contains("error,"))
                IsErrors = true;

            if (IsAborted || IsErrors) return;

            //если включено деление
            if (split != "" && m.format != Format.ExportFormats.BluRay)
                m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + ".split.1" + Path.GetExtension(m.outfilepath).ToLower();

            //проверка на удачное завершение
            if (m.format == Format.ExportFormats.BluRay)
            {
                if (!Directory.Exists(m.outfilepath) ||
                    Calculate.GetFolderSize(m.outfilepath) == 0)
                {
                    IsErrors = true;
                    ErrorExeption(encodertext);
                }
            }
            else
            {
                if (!File.Exists(m.outfilepath) ||
                    new FileInfo(m.outfilepath).Length == 0)
                {
                    IsErrors = true;
                    ErrorExeption(encodertext);
                    //throw new Exception(Languages.Translate("Can`t find output video file!"));
                }
            }

            encodertext = null;

            //удаляем мета файл
            SafeDelete(metapath);

            //правим BluRay структуру для флешек и FAT32 дисков
            if (m.format == Format.ExportFormats.BluRay &&
                m.bluray_type == "FAT32 HDD/MS")
                Calculate.MakeFAT32BluRay(m.outfilepath);

            SetLog("");
        }

        private void make_avc2avi()
        {

            string aviout = Calculate.RemoveExtention(m.outvideofile, true) + ".avi";

            busyfile = Path.GetFileName(aviout);

            //info строка
            SetLog("Video file: " + m.outvideofile);
            SetLog("Rebuild to: " + aviout);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\avc2avi\\avc2avi.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

          //до переделки avc2avi  0.001
        //    string rate = " -f " + m.outframerate;
        //    if (m.outvcodec == "Copy")
        //    {
        //        if (m.inframerate != null)
        //            rate = " -f " + m.inframerate;
        //        else
        //            rate = "";
        //    }

        //    info.Arguments = "-i \"" + m.outvideofile + "\" -o \"" + aviout + "\"" + rate; //" " + "-f 23.977";//



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
                if (rate == "23.976")
                    rate = "23.9761";

                if (rate == "29.970")
                    rate = "29.9701";

                if (rate == "59.940")
                    rate = "59.9401";

                rate = " -f " + rate;
            }
            info.Arguments = "-i \"" + m.outvideofile + "\" -o \"" + aviout + "\"" + rate;      //" " + "-f 23.977";//
            
            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("avc2avi.exe:" + " " + info.Arguments);
                SetLog(" ");
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
                ErrorExeption(encodertext);
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
            busyfile = Path.GetFileName(m.outvideofile);

            //info строка
            if (m.format == Format.ExportFormats.Mpeg2PAL)
                SetLog("DGPulldown: " + m.outframerate + " > 25.000");
            if (m.format == Format.ExportFormats.Mpeg2NTSC)
                SetLog("DGPulldown: " + m.outframerate + " > 29.970");

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

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("dgpulldown.exe:" + " " + info.Arguments);
                SetLog(" ");
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

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("cfourcc.exe:" + " " + info.Arguments);
                SetLog(" ");
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

        private void make_divxmux()
        {

            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            //info строка
            SetLog("Video file: " + m.outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\DivXMux\\DivXMux.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            //info.RedirectStandardOutput = true;
            //info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string addaudio = "";
            if (m.inaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                addaudio = " -a \"" + outstream.audiopath + "\"";
            }

            info.Arguments = "-v \"" + m.outvideofile + "\"" + addaudio + " -o \"" + m.outfilepath + "\"";

            //узнаём какой примерно должен полчиться файл
            long asize = 0;
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                asize = new FileInfo(outstream.audiopath).Length;
            }
            long totalsize = new FileInfo(m.outvideofile).Length + asize;

            encoderProcess.StartInfo = info;

            //прописываем аргументы команндной строки
            if (Settings.ArgumentsToLog)
                SetLog(info.Arguments);

            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            encoderProcess.WaitForExit();
            //while (!encoderProcess.HasExited)
            //{
            //    Thread.Sleep(100);
            //    //locker.WaitOne();
            //    try
            //    {
            //        if (File.Exists(m.outfilepath))
            //        {
            //            FileInfo csize = new FileInfo(m.outfilepath);
            //            worker.ReportProgress(Convert.ToInt32((csize.Length / totalsize) * m.outframes));
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;

            SetLog("");
        }

        private void make_vdubmod()
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
                    SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);
            SetLog("Please wait...");

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\VirtualDubMod\\VirtualDubMod.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            info.Arguments = "/x /s\"" + Settings.TempPath + "\\" + m.key + ".vcf\"";

            //узнаём какой примерно должен полчиться файл
            //FileInfo vsize = new FileInfo(m.outvideofile);
            //FileInfo asize = new FileInfo(m.outaudiofile);
            //long totalsize = vsize.Length + asize.Length;

            //прописываем фикс для реестра
            VirtualDubModWrapper.SetStartUpInfo();

            //создаём скрипт
            VirtualDubModWrapper.CreateMuxingScript(m);

            
            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            //SetPriority(Settings.ProcessPriority);

            encoderProcess.WaitForExit();
            //while (!encoderProcess.HasExited)
            //{
            //    Thread.Sleep(100);
            //    //locker.WaitOne();
            //    try
            //    {
            //        if (File.Exists(m.outfilepath))
            //        {
            //            FileInfo csize = new FileInfo(m.outfilepath);
            //            worker.ReportProgress(Convert.ToInt32((csize.Length / totalsize) * m.outframes));
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;

            SetLog("");
        }

        private void make_mkv()
        {
            SafeDelete(m.outfilepath);

            busyfile = Path.GetFileName(m.outfilepath);
            step++;

            string outvideofile;
            if (Format.IsDirectRemuxingPossible(m) &&
                m.outvcodec == "Copy")
                outvideofile = m.infilepath;
            else
                outvideofile = m.outvideofile;

            //info строка
            SetLog("Video file: " + outvideofile);
            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.audiopath != null)
                    SetLog("Audio file: " + outstream.audiopath);
            }
            SetLog("Muxing to: " + m.outfilepath);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvmerge.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            string ext = Path.GetExtension(outvideofile);
            int vID = 0;
            if (ext == ".mp4")
                vID = 1;

            string rate = "--default-duration " + vID + ":" + m.outframerate + "fps ";
            if (m.outvcodec == "Copy")
            {
                if (m.inframerate == "")
                    rate = "";
                else
                    rate = "--default-duration " + vID + ":" + m.inframerate + "fps ";
            }

            //split
            string split = "";
            if (m.split != null &&
                m.split != "Disabled")
            {
                int size = 0;

                string svalue = Calculate.GetRegexValue(@"(\d+).mb", m.split);
                if (svalue != null)
                    Int32.TryParse(svalue, NumberStyles.Integer, null, out size);

                if (size != 0)
                    split = " --split size:" + size + "M ";
            }

            //video
            string video = "";
            if (Format.IsDirectRemuxingPossible(m) &&
                m.outvcodec == "Copy")
                video = "-d " + m.invideostream_mkvid + " -A -S \"" + m.infilepath + "\" ";
                //video = "-d " + m.invideostream_mkvid + " -S \"" + m.infilepath + "\" ";
            else
                video = rate + "-d " + vID + " -A -S \"" + m.outvideofile + "\" ";

            //audio
            string audio = "";
            if (m.outaudiostreams.Count > 0)
            {
                //video += "--sync 0:0 ";

                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                string aext = Path.GetExtension(outstream.audiopath);
                int aID = outstream.mkvid;
                if (aext == ".m4a" || aext == ".avi") //||aext == ".aac"
                    aID = 1;
                if (aext == ".aac") //Для aac всегда ноль, т.к. это RAW
                    aID = 0;
                string sbr = null;
                if (outstream.codec == "Copy" && instream.codec.Contains("AAC")) //Для правильного муксинга he-aac, aac+, или aac-sbr
                    if (instream.codec.Contains("HE") || instream.codec.Contains("AAC+") || instream.codec.Contains("SBR"))
                        sbr = " --aac-is-sbr 0:1";
                    else
                        sbr = " --aac-is-sbr 0:0";

                if (!File.Exists(outstream.audiopath) &&
                    Format.IsDirectRemuxingPossible(m) &&
                    outstream.codec == "Copy")
                {
                    audio = "-a " + instream.mkvid + " -D -S --no-chapters \"" + m.infilepath + "\" "; //звук из исходника (режим Copy без демукса)
                }
                else
                    audio = "-a " + aID + sbr + " -D -S --no-chapters \"" + outstream.audiopath + "\" ";
            }
 
            //"--aspect-ratio " + vID + ":" + Calculate.ConvertDoubleToPointString(m.outaspect) + " " +
            
            //Ввод полученых аргументов коммандной строки, + добавление строки введенной пользователем
            info.Arguments = "-o \"" + m.outfilepath + "\" " + video + audio + split + m.mkvstring;

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("mkvmerge.exe:" + " " + info.Arguments); //Вывод в лог коммандной строки для mkvmerge
                SetLog(" ");
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            SetPriority(Settings.ProcessPriority);

            string line;
            string pat = @"progress:\D(\d+)%";
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
                    if (mat.Success == true)
                    {
                        procent = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress(Convert.ToInt32((procent / 100.0) * m.outframes));
                    }
                    else
                    {
                        if (line.StartsWith("Error"))
                        {
                            IsErrors = true;
                            ErrorExeption(line.Replace("Error:","").Trim());
                        }
                        if (line.StartsWith("Warning"))
                        {
                            if (encodertext != null)
                                encodertext += Environment.NewLine;
                            encodertext += line;
                            SetLog(line);
                        }
                    }
                }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //если включено деление
            if (split != "")
                m.outfilepath = Calculate.RemoveExtention(m.outfilepath, true) + "-001" + Path.GetExtension(m.outfilepath).ToLower();

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
                //throw new Exception(Languages.Translate("Can`t find output video file!"));
            }

            encodertext = null;
           
            SetLog("");
        }

        private void make_pmp()
        {

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

            //прописываем аргументы команндной строки
            SetLog(" ");
            if (Settings.ArgumentsToLog)
            {
                SetLog("pmp_muxer_avc.exe:" + " " + info.Arguments);
                SetLog(" ");
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

                    if (mat.Success == true && IsNum)
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
                            if (encodertext != null)
                                encodertext += Environment.NewLine;
                            encodertext += line;
                            //SetLog(line);
                        }
                    }
            }

            //обнуляем прогресс
            of = 0;
            cf = 0;

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(m.outfilepath) ||
                new FileInfo(m.outfilepath).Length == 0)
            {
                IsErrors = true;
                ErrorExeption(encodertext);
            }

            encodertext = null;

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
                ErrorExeption(ex.Message);
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                if (worker != null && worker.IsBusy)
                {
                    //получаем текущее время и текущий фрейм
                    DateTime ct = DateTime.Now;
                    int cframe = cf;

                    double pr = 0.0;
                    double pr_t = 0.0;

                    //вычисляем проценты прогресса
                    pr = ((double)cframe / m.outframes) * 100.0;
                    pr_t = cframe + (step * m.outframes);

                    //вычисляем fps
                    if (of != cframe)
                    {
                        double tinterval = TimeSpan.FromTicks(ct.Ticks - ot.Ticks).TotalSeconds;
                        fps = (double)(cframe - of) / tinterval; //фэпээс
   
                        //запоминаем сравнительные значения
                        of = cframe;
                        ot = ct;
                        ofps = fps;

                        //берем значение fps от энкодеров, если они не равны нулю
                        if (encoder_fps != 0.0)
                            fps = encoder_fps;
                       
                        //вычисляем сколько времени осталось кодировать
                        if (cframe < m.outframes)
                            et = (double)(m.outframes - cframe) / fps;

                    }

                    string e_s = "";
                    string new_fps = "";
                   
                    //убираем десятые и сотые от фпс, если значения получены не от энкодеров
                    if (encoder_fps != 0.0)
                        new_fps = fps.ToString("####0.00");
                    else
                        new_fps = fps.ToString("####0");

                    if (fps > 0.0000001)
                    {
                        TimeSpan elapsed = TimeSpan.FromSeconds(et);
                        if (elapsed.Days > 0)
                            e_s += elapsed.Days + "day ";

                        if (elapsed.Hours > 0)
                            e_s += elapsed.Hours + "hour ";

                        if (elapsed.Minutes > 0)
                            e_s += elapsed.Minutes + "min ";

                        if (elapsed.Seconds > 0)
                            e_s += elapsed.Seconds + "sec";

                        //fps = 123.1234;
                        string pr_text = (cframe) + "frames " + new_fps + "fps " + e_s; //("####0.0")   PadLeft(12, ' ');
                        string title = pr.ToString("##0.00") + "% encoding to: " + busyfile;
                        SetStatus(title, pr_text, cframe, pr_t);
                    }
                }

            }
            catch (Exception)
            {
                //ErrorExeption(ex.Message);
            }
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //перепроверяем правильное колличество фреймов и фреймрейт
                //m = AviSynthScripting.UpdateOutFrames(m);

              //  AviSynthReader reader2 = new AviSynthReader();
               // reader2.ParseScript(m.script);
              //  m.outframes = reader2.FrameCount;
              //  reader2.Close();
              //  reader2 = null;



                //запоминаем когда это началось
                start_time = DateTime.Now;

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
                SetLog("OS: " + Environment.OSVersion.ToString());
                SetLog("OEMCodePage: " + CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                SetLog("Language: " + CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName);
                SetLog("DecimalSeparator: " + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                SetLog("Framework: " + Environment.Version);
                SetLog("Processors: " + Environment.ProcessorCount);
                SetLog("Machine: " + Environment.MachineName);
                SetLog("UserName: " + Environment.UserName);
                SetLog("SystemDrive: " + Environment.ExpandEnvironmentVariables("%SystemDrive%"));
                SetLog("");

                SetLog("XVID4PSP");
                SetLog("------------------------------");
                //Assembly ainfo = Assembly.GetExecutingAssembly();
                //AssemblyName aname = ainfo.GetName();
                //string ver = aname.Version.ToString(2) + aname.Version.Build + aname.Version.Revision;
                AssemblyInfoHelper asinfo = new AssemblyInfoHelper(); 
                string ver = asinfo.Version + " " + asinfo.Trademark;
                DateTime ct = File.GetLastWriteTime(Calculate.StartupPath + "\\XviD4PSP.exe");
                SetLog("Version: " + ver);
                SetLog("Created: " + ct.ToString());
                SetLog("TempPath: " + Settings.TempPath);
                SetLog("AppPath: " + Calculate.StartupPath);
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
                SetLog("Duration: " + Calculate.GetTimeline(m.outduration.TotalSeconds) + " (" + m.outframes  + ")");

                if (m.vencoding != "Disabled")
                {
                    if (m.outvcodec != "Copy")
                    {
                        if (m.vdecoder == AviSynthScripting.Decoders.FFmpegSource && Settings.FFmpegSource2)
                            SetLog("VideoDecoder: FFmpegSource2");
                        else
                            SetLog("VideoDecoder: " + m.vdecoder);

                        if (m.inresw != m.outresw ||
                            m.inresh != m.outresh)
                            SetLog("Resolution: " + m.inresw + "x" + m.inresh + " > " + m.outresw + "x" + m.outresh);
                        else
                            SetLog("Resolution: " + m.inresw + "x" + m.inresh);

                        SetLog("VCodecPreset: " + m.vencoding);
                        SetLog("VEncodingMode: " + m.encodingmode);
                        if (m.invcodec != m.outvcodec)
                            SetLog("VideoCodec: " + m.invcodecshort + " > " + m.outvcodec);
                        else
                            SetLog("VideoCodec: " + m.invcodecshort);

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
                            SetLog("FramerateModifer: " + m.frameratemodifer);
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
                        SetLog("Resolution: " + m.inresw + "x" + m.inresh);
                        SetLog("VideoCodec: " + m.invcodecshort);
                        SetLog("VideoBitrate: " + m.invbitrate);
                        SetLog("Framerate: " + m.inframerate);
                    }
                }
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec != "Copy")
                    {
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
                            SetLog("SamplerateModifer: " + m.sampleratemodifer);
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
                        if (instream.delay != 0 ||
                            outstream.delay != 0)
                            SetLog("Delay: " + instream.delay + " > " + outstream.delay);
                    }
                    else
                    {
                        SetLog("AudioCodec: " + instream.codecshort);
                        SetLog("AudioBitrate: " + instream.bitrate);
                        SetLog("Samplerate: " + instream.samplerate);
                        SetLog("Channels: " + instream.channels);
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

                Format.Muxers muxer = Format.GetMuxer(m);

                //кодирование видео
                if (m.outvcodec == "Copy" &&
                    !Format.IsDirectRemuxingPossible(m))
                {
                    SetLog("DEMUXING");
                    SetLog("------------------------------");
                }
                else if (muxer == Format.Muxers.Disabled &&
                    m.format != Format.ExportFormats.Audio)
                {
                    SetLog((m.outaudiostreams.Count == 0) ? "VIDEO ENCODING" : "VIDEO & AUDIO ENCODING");
                    SetLog("------------------------------");
                }
                else if (m.outvcodec != "Disabled" &&
                         m.outvcodec != "Copy")
                {
                    SetLog("VIDEO ENCODING");
                    SetLog("------------------------------");
                }

                if (m.outvcodec == "x264")
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
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(m.infilepath);
                    int astreams = media.CountAudioStreams;
                    media.Close();
                    if (Format.IsDirectRemuxingPossible(m))//astreams == 0)
                    {
                        m.outvideofile = m.infilepath;
                    }
                    else
                    {
                        Format.Demuxers dem = Format.GetDemuxer(m);
                        if (dem == Format.Demuxers.mkvextract)
                            demux_mkv(Demuxer.DemuxerMode.ExtractVideo);
                        else if (dem == Format.Demuxers.pmpdemuxer)
                            demux_pmp(Demuxer.DemuxerMode.ExtractVideo);
                        else if (dem == Format.Demuxers.mp4box)
                            demux_mp4box(Demuxer.DemuxerMode.ExtractVideo);
                        else
                            demux_ffmpeg(Demuxer.DemuxerMode.ExtractVideo);
                    }
                }

                if (IsAborted || IsErrors) return;

                //кодирование звука
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec == "Copy" && 
                        !File.Exists(instream.audiopath) &&
                        !Format.IsDirectRemuxingPossible(m))
                    {
                        SetLog("DEMUXING");
                        SetLog("------------------------------");
                    }
                    else if (outstream.codec != "Disabled" &&
                        outstream.codec != "Copy")
                    {
                        if (muxer != Format.Muxers.Disabled ||
                            m.format == Format.ExportFormats.Audio)
                        {
                            SetLog("AUDIO ENCODING");
                            SetLog("------------------------------");
                        }
                    }

                    if (muxer != Format.Muxers.Disabled)
                        make_sound();
                    if (muxer == Format.Muxers.Disabled &&
                        m.vencoding == "Disabled")
                        make_sound();
                }

                if (IsAborted || IsErrors) return;

                //pulldown
                if (m.format == Format.ExportFormats.Mpeg2PAL && m.outframerate != "25.000" ||
                    m.format == Format.ExportFormats.Mpeg2NTSC && m.outframerate != "29.970")
                {
                    SetLog("PULLDOWN");
                    SetLog("------------------------------");
                    make_pulldown();
                }

                if (IsAborted || IsErrors) return;

                if (m.dontmuxstreams)
                    muxer = Format.Muxers.Disabled;

                //муксинг
                if (muxer != 0 &&
                    muxer != Format.Muxers.Disabled)
                {
                    SetLog("MUXING");
                    SetLog("------------------------------");

                    if (muxer == Format.Muxers.pmpavc)
                        make_pmp();
                    if (muxer == Format.Muxers.mkvmerge)
                        make_mkv();
                    if (muxer == Format.Muxers.mplex)
                        make_mplex();
                    if (muxer == Format.Muxers.ffmpeg)
                        make_ffmpeg_mux();
                    if (muxer == Format.Muxers.virtualdubmod)
                    {
                        //делаем ави из avc
                        string vext = Path.GetExtension(m.outvideofile);
                        if (vext != ".avi")
                            make_avc2avi();

                        make_vdubmod();
                    }
                    if (muxer == Format.Muxers.mp4box)
                        make_mp4();
                    if (muxer == Format.Muxers.tsmuxer)
                        make_tsmuxer();
                    if (muxer == Format.Muxers.dpgmuxer)
                        make_dpg();
                }

                if (IsAborted || IsErrors) return;

                //картинки для PSP
                if (m.format == Format.ExportFormats.Mp4PSPAVC ||
                    m.format == Format.ExportFormats.Mp4PSPAVCTV)
                    make_thm();
                if (m.format == Format.ExportFormats.PmpAvc)
                    make_png();

                //SetLog("     ");
                //SetLog("Output format: " + Format.EnumToString(m.format));
                //if (m.outvinfo != null)
                //    SetLog("Video track: " + m.outvinfo);
                //if (m.outainfo != null)
                //    SetLog("Audio track: " + m.outainfo);

                //прописываем сколько это всё заняло у нас врмени
                TimeSpan enc_time = (DateTime.Now - start_time);
                string enc_time_s = "";
                if (enc_time.Days > 0)
                    enc_time_s += enc_time.Days + " day ";

                if (enc_time.Hours > 0)
                    enc_time_s += enc_time.Hours + " hour ";

                if (enc_time.Minutes > 0)
                    enc_time_s += enc_time.Minutes + " min ";

                if (enc_time.Seconds > 0)
                    enc_time_s += enc_time.Seconds + " sec";

                SetLog("TIME");
                SetLog("------------------------------");
                SetLog(Languages.Translate("Total encoding time:") + " " + enc_time_s);

                if (m.split != null &&
                    m.split != "Disabled")
                {
                    string[] files;
                    if (m.format == Format.ExportFormats.M2TS ||
                        m.format == Format.ExportFormats.TS)
                    {
                        files = Directory.GetFiles(Path.GetDirectoryName(m.outfilepath),
                            Path.GetFileNameWithoutExtension(m.outfilepath).Replace(".split.1", "") +
                            "*.split.*" + Path.GetExtension(m.outfilepath).ToLower());

                        //если получился только 1 файл
                        if (files.Length == 1)
                        {
                            File.Move(m.outfilepath, m.outfilepath.Replace(".split.1", ""));
                            m.outfilepath = m.outfilepath.Replace(".split.1", "");
                            files = new string[1];
                            files[0] = m.outfilepath;
                        }
                    }

                    else if (m.format == Format.ExportFormats.Mkv)
                    {
                        files = Directory.GetFiles(Path.GetDirectoryName(m.outfilepath),
                            Path.GetFileNameWithoutExtension(m.outfilepath).Replace("-001", "") +
                            "*-*" + Path.GetExtension(m.outfilepath).ToLower());

                        //если получился только 1 файл
                        if (files.Length == 1)
                        {
                            File.Move(m.outfilepath, m.outfilepath.Replace("-001", ""));
                            m.outfilepath = m.outfilepath.Replace("-001", "");
                            files = new string[1];
                            files[0] = m.outfilepath;
                        }
                    }
                    else
                        files = new string[0];

                    long size = 0;
                    foreach (string f in files)
                    {
                        FileInfo finfo = new FileInfo(f);
                        size += finfo.Length;
                    }

                    SetLog(Languages.Translate("Out file size is:") + " " + 
                        Calculate.ConvertDoubleToPointString((double)size / 1024.0 / 1024.0, 2) + " mb");
                }
                else if (m.format == Format.ExportFormats.BluRay)
                {
                    long size = Calculate.GetFolderSize(m.outfilepath);
                    SetLog(Languages.Translate("Out file size is:") + " " +
                        Calculate.ConvertDoubleToPointString((double)size / 1024.0 / 1024.0, 2) + " mb");
                }
                else
                {
                    long size = 0;
                    if (m.dontmuxstreams)
                    {
                        size += new FileInfo(m.outvideofile).Length;
                        if (m.outaudiostreams.Count > 0)
                        {
                            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                            size += new FileInfo(outstream.audiopath).Length;
                        }
                    }
                    else
                    {
                        FileInfo finfo = new FileInfo(m.outfilepath);
                        size = finfo.Length;
                    }
                  
                    SetLog(Languages.Translate("Out file size is:") + " " +
                        Calculate.ConvertDoubleToPointString((double)size / 1024.0 / 1024.0, 2) + " mb");
                }

            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

       private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            try
            {
                //получаем текущий фрейм
                cf = e.ProgressPercentage; 
            } 
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

       private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //закрываем таймер
            timer.Close();
            timer.Enabled = false;
            timer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);

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

            if (!IsErrors && !IsAborted)
            {
                button_info.Visibility = Visibility.Visible;
                button_play.Visibility = Visibility.Visible;
            }

            try
            {
                //удаляем временные файлы
                if (!IsErrors)
                {
                    if (m.infilepath != m.outvideofile &&
                        m.outvideofile != null &&
                        !m.dontmuxstreams)
                        SafeDelete(m.outvideofile);

                    foreach (object s in m.inaudiostreams)
                    {
                        AudioStream a = (AudioStream)s;
                        if (a.audiopath != null &&
                            Path.GetDirectoryName(a.audiopath) == Settings.TempPath &&
                            a.audiopath != m.infilepath) //Защита от удаления исходника
                            p.deletefiles.Add(a.audiopath);
                    }

                    foreach (object s in m.outaudiostreams)
                    {
                        AudioStream a = (AudioStream)s;
                        if (a.audiopath != null &&
                            Path.GetDirectoryName(a.audiopath) == Settings.TempPath &&
                            a.audiopath != m.outfilepath) //Защита от удаления результата кодирования
                            p.deletefiles.Add(a.audiopath);
                        if (!string.IsNullOrEmpty(a.nerotemp) && a.nerotemp != m.infilepath && Path.GetDirectoryName(a.nerotemp) == Settings.TempPath)
                            SafeDelete(a.nerotemp); //Временный WAV-файл от 2pass AAC
                    }
                }
                else
                {
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
                SafeDelete(m.scriptpath);
                if (m.outvideofile != null)
                {
                    SafeDelete(Calculate.RemoveExtention(m.outvideofile) + "log");
                    SafeDelete(Calculate.RemoveExtention(m.outvideofile) + "log.temp");
                }
                SafeDelete(Settings.TempPath + "\\" + m.key + ".vcf");

                //проверка на удачное завершение
                if (!IsAborted && !IsErrors)
                {
                    if (m.format == Format.ExportFormats.BluRay)
                    {
                        if (Directory.Exists(m.outfilepath) &&
                            Calculate.GetFolderSize(m.outfilepath) != 0)
                        {
                            p.outfiles.Remove(m.outfilepath);
                            Title = Path.GetFileName(m.outfilepath) + " " + Languages.Translate("ready") + "!";
                        }
                        else
                            Title = Languages.Translate("Error");
                    }
                    else
                    {
                        if (File.Exists(m.outfilepath) &&
                            new FileInfo(m.outfilepath).Length != 0)
                        {
                            p.outfiles.Remove(m.outfilepath);
                            Title = Path.GetFileName(m.outfilepath) + " " + Languages.Translate("ready") + "!";
                        }
                        else
                            Title = Languages.Translate("Error");
                    }
                }

                //меняем статус кодирования
                if (IsAborted)
                    p.UpdateTaskStatus(m.key, "Waiting");
                else if (IsErrors)
                    p.UpdateTaskStatus(m.key, "Errors");
                else
                {
                    if (Settings.AutoDeleteTasks)
                        p.RemoveTask(m.key);
                    else
                        p.UpdateTaskStatus(m.key, "Encoded");
                }

                //финальные действия
                if (p.outfiles.Count == 0 && !IsAborted)
                {
                    if (ending == Shutdown.ShutdownMode.Exit)
                        p.Close();
                    else if (ending == Shutdown.ShutdownMode.Hibernate ||
                        ending == Shutdown.ShutdownMode.Shutdown ||
                        ending == Shutdown.ShutdownMode.Standby)
                    {
                        Shutdown shut = new Shutdown(this, ending);
                    }
                }

                //смотрим есть ли что ещё скодировать
                if (!IsAborted)
                    p.EncodeNextTask();

                //выходим
                if (Settings.AutoClose && !IsErrors)
                    Close();
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
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

        private void ErrorExeption(string message)
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