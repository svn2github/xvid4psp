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
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text;

namespace XviD4PSP
{
    public partial class Demuxer
    {
        public enum DemuxerMode { DecodeToWAV = 1, NeroTempWAV, ExtractVideo, ExtractAudio, ExtractSub, ExtractTimecode, RepairMKV };
        private BackgroundWorker worker = null;
        private AviSynthEncoder avs = null;
        private Process encoderProcess = null;
        public Massive m;
        private DemuxerMode mode;
        private string outfile;

        private bool IsAborted = false;
        public bool IsErrors = false;
        public string error_message;
        private int exit_code;
        private string source_file;

        //Логи утилит
        private StringBuilder encodertext = new StringBuilder();
        private void AppendEncoderText(string text)
        {
            if (encodertext.Length > 0)
            {
                //Укорачиваем лог, если он слишком длинный
                if (encodertext.Length > 5000)
                {
                    int new_line_pos = encodertext.ToString().IndexOf(Environment.NewLine, 500);
                    if (new_line_pos <= 0) new_line_pos = 500;
                    encodertext.Remove(0, new_line_pos);
                    encodertext.Insert(0, ".....");
                }

                encodertext.Append(Environment.NewLine);
            }
            encodertext.Append(text);
        }

        public Demuxer(Massive mass, DemuxerMode mode, string outfile)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();
            this.outfile = outfile;
            this.mode = mode;

            //забиваем
            progress.Maximum = 100;

            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            if (mode == DemuxerMode.DecodeToWAV) Title = Languages.Translate("Decoding to Windows PCM") + "...";
            else if (mode == DemuxerMode.NeroTempWAV) Title = Languages.Translate("Creating Nero temp file") + "...";
            else if (mode == DemuxerMode.ExtractAudio) Title = Languages.Translate("Audio demuxing") + "...";
            else if (mode == DemuxerMode.ExtractVideo) Title = Languages.Translate("Video demuxing") + "...";
            else if (mode == DemuxerMode.RepairMKV) Title = Languages.Translate("Remuxing Matroska file") + "...";

            //Определяем исходный файл
            source_file = (m.infilepath_source != null) ? m.infilepath_source : m.infilepath;

            //фоновое кодирование
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            //Сворачиваем окно, если программа минимизирована или свернута в трей
            if (!Owner.IsVisible || Owner.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Minimized;
                this.StateChanged += new EventHandler(Window_StateChanged);
                this.Name = "Hidden";
            }

            ShowDialog();
        }

        //Разворачиваем главное окно при разворачивании этого окна
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != System.Windows.WindowState.Minimized)
            {
                this.Name = "Window";
                Calculate.CheckWindowPos(this, false);

                if (!Owner.IsVisible) Owner.Show();
                if (Owner.WindowState == System.Windows.WindowState.Minimized)
                    Owner.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
                Calculate.CheckWindowPos(this, false);
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

        private void ExtractSound()
        {
            //удаляем старый файл
            SafeDelete(outfile);

            //создаём кодер
            avs = new AviSynthEncoder(m, m.script, outfile);

            //запускаем кодер
            avs.start();

            //Выводим прогресс
            double total_frames = (m.outframes != 0) ? m.outframes : m.inframes; //Определяем количество кадров
            while (avs.IsBusy())
            {
                double p = ((double)(avs.frame) / total_frames) * 100.0;
                worker.ReportProgress((int)p);
                Thread.Sleep(100);
            }

            //Ловим ошибки, если они были
            if (avs.IsErrors)
            {
                AppendEncoderText(avs.error_text);
                avs = null;
                throw new Exception(encodertext.ToString());
            }

            //чистим ресурсы
            avs = null;
        }

        private void demux_ffmpeg()
        {
            if (m.infileslist.Length > 1)
                ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
                    Languages.Translate("Warning"));

            //удаляем старый файл
            SafeDelete(outfile);

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.StandardErrorEncoding = Encoding.UTF8;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            if (mode == DemuxerMode.ExtractAudio)
            {
                AudioStream s = (AudioStream)m.inaudiostreams[m.inaudiostream];

                string acodec = "copy", forceformat = "";
                string outext = Path.GetExtension(outfile).ToLower();
                if (outext == ".lpcm" || s.codec == "LPCM") forceformat = " -f s16be";
                else if (outext == ".wav") { acodec = "pcm_s16le"; forceformat = " -f wav"; }
                else if (outext == ".truehd") forceformat = " -f truehd";

                info.Arguments = "-hide_banner -nostdin -i \"" + source_file + "\" -map 0:" + s.ff_order + " -sn -vn -acodec " + acodec + forceformat + " \"" + outfile + "\"";
            }
            else if (mode == DemuxerMode.ExtractVideo)
            {
                string forceformat = "";
                //string outext = Path.GetExtension(outfile);
                //if (outext == ".m2v")
                //    forceformat = " -f vob";
                info.Arguments = "-hide_banner -nostdin -i \"" + source_file + "\" -map 0:v:0 -sn -an -vcodec copy" + forceformat + " \"" + outfile + "\"";
            }

            double percentage_k = m.induration.TotalSeconds / 100.0;
            TimeSpan current_sec = TimeSpan.Zero;

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"time=(\d+:\d+:\d+\.?\d*)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            //первый проход
            while (!encoderProcess.HasExited)
            {
                line = encoderProcess.StandardError.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success && TimeSpan.TryParse(mat.Groups[1].Value, out current_sec))
                    {
                        worker.ReportProgress((int)(current_sec.TotalSeconds / percentage_k));
                    }
                    else
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) AppendEncoderText(Calculate.FilterLogMessage(r, line));

            //чистим ресурсы
            exit_code = encoderProcess.ExitCode;
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted) return;

            //проверка на удачное завершение
            if (exit_code != 0 && encodertext.Length > 0)
            {
                //Оставляем только последнюю строчку из всего лога
                string[] log = encodertext.ToString().Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                throw new Exception(log[log.Length - 1]);
            }
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                if (mode == DemuxerMode.ExtractVideo) throw new Exception(Languages.Translate("Can`t find output video file!"));
                if (mode == DemuxerMode.ExtractAudio) throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }
            encodertext.Length = 0;
        }

        private void demux_mp4box()
        {
            //if (m.infileslist.Length > 1)
            //    ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
            //        Languages.Translate("Warning"));

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\MP4Box.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            //удаляем старый файл
            SafeDelete(outfile);

            if (mode == Demuxer.DemuxerMode.ExtractVideo)
                info.Arguments = "-raw " + m.invideostream_mi_id + " \"" + source_file + "\" -out \"" + outfile + "\"";

            if (mode == Demuxer.DemuxerMode.ExtractAudio)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                info.Arguments = "-raw " + instream.mi_id + " \"" + source_file + "\" -out \"" + outfile + "\"";
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"(\d+)/(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            //первый проход
            while (!encoderProcess.HasExited)
            {
                line = encoderProcess.StandardError.ReadLine();

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

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) AppendEncoderText(Calculate.FilterLogMessage(r, line));

            //чистим ресурсы
            exit_code = encoderProcess.ExitCode;
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted) return;

            //проверка на удачное завершение
            if (exit_code != 0)
            {
                throw new Exception(encodertext.ToString());
            }
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                if (mode == DemuxerMode.ExtractVideo) throw new Exception(Languages.Translate("Can`t find output video file!"));
                if (mode == DemuxerMode.ExtractAudio) throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }
            encodertext.Length = 0;
        }

        private void demux_dpg()
        {
            //удаляем старый файл
            SafeDelete(outfile);

            dpgmuxer muxer = new dpgmuxer();
            muxer.ProgressChanged += new dpgmuxer.ProgressChangedDelegate(muxer_ProgressChanged);

            try
            {
                if (mode == Demuxer.DemuxerMode.ExtractVideo)
                    muxer.DemuxVideo(source_file, outfile);

                if (mode == Demuxer.DemuxerMode.ExtractAudio)
                    muxer.DemuxAudio(source_file, outfile);
            }
            catch (Exception ex)
            {
                IsErrors = true;
                ErrorExeption(ex.Message);
            }

            muxer.ProgressChanged -= new dpgmuxer.ProgressChangedDelegate(muxer_ProgressChanged);

            if (IsAborted || IsErrors) return;

            //проверка на удачное завершение
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                if (mode == DemuxerMode.ExtractVideo)
                    throw new Exception(Languages.Translate("Can`t find output video file!"));
                if (mode == DemuxerMode.ExtractAudio)
                    throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }
        }

        private void muxer_ProgressChanged(double progress_value)
        {
            SetProgress(progress_value);
        }

        private void demux_pmp()
        {
            //if (m.infileslist.Length > 1)
            //    ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
            //        Languages.Translate("Warning"));

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\pmp_muxer_avc\\pmp_demuxer.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = false;
            info.CreateNoWindow = true;

            info.Arguments = "\"" + source_file + "\"";

            string vfile = source_file + ".avi";
            string afile = source_file + ".1.aac";

            //удаляем старый файл
            SafeDelete(outfile);

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"(\d+)\D/\D(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

            //первый проход
            while (!encoderProcess.HasExited)
            {
                line = encoderProcess.StandardOutput.ReadLine();

                if (line != null)
                {
                    mat = r.Match(line);
                    if (mat.Success)
                    {
                        int frame = Convert.ToInt32(mat.Groups[1].Value);
                        worker.ReportProgress((int)(((double)(frame) / (double)m.inframes) * 100.0));
                    }
                    else
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted || IsErrors) return;

            if (mode == DemuxerMode.ExtractVideo)
            {
                //проверка на удачное завершение
                if (!File.Exists(vfile) || new FileInfo(vfile).Length == 0)
                    throw new Exception(Languages.Translate("Can`t find output video file!"));

                SafeDelete(afile);

                //вытягиваем RAW h264 из AVI
                string old_infilepath = source_file;
                source_file = vfile;
                demux_ffmpeg();
                SafeDelete(vfile);
                source_file = old_infilepath;
            }

            if (mode == DemuxerMode.ExtractAudio)
            {
                //проверка на удачное завершение
                if (!File.Exists(afile) || new FileInfo(afile).Length == 0)
                    throw new Exception(Languages.Translate("Can`t find output audio file!"));

                SafeDelete(vfile);
                File.Move(afile, outfile);
            }

            //перемещаем файл в правильное место
            encodertext.Length = 0;

            SafeDelete(source_file + ".log");
        }

        private void demux_mkv()
        {
            //удаляем старый файл
            SafeDelete(outfile);

            //список файлов
            string flist = "";
            foreach (string _file in m.infileslist)
                flist += "\"" + _file + "\" ";

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvextract.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.StandardOutputEncoding = System.Text.Encoding.UTF8;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = false;
            info.CreateNoWindow = true;
            encodertext.Length = 0;

            if (mode == DemuxerMode.ExtractAudio)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                info.Arguments = "tracks " + flist + instream.mi_order + ":" + "\"" + outfile + "\" --output-charset UTF-8";
            }
            else if (mode == DemuxerMode.ExtractVideo)
                info.Arguments = "tracks " + flist + m.invideostream_mi_order + ":" + "\"" + outfile + "\" --output-charset UTF-8";
            else if (mode == DemuxerMode.RepairMKV)
            {
                info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvmerge.exe";
                info.Arguments = "-S \"" + source_file + "\" -o \"" + outfile + "\" --output-charset UTF-8";
            }

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            string line;
            string pat = @"^[^\+].+:\s(\d+)%";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;

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
                    else if (line != "")
                    {
                        AppendEncoderText(line);
                    }
                }
            }

            //Дочитываем остатки лога, если что-то не успело считаться
            line = encoderProcess.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(line)) AppendEncoderText(Calculate.FilterLogMessage(r, line.Replace("\r\r\n", "\r\n")));

            //чистим ресурсы
            exit_code = encoderProcess.ExitCode;
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            if (IsAborted) return;

            //проверка на удачное завершение
            if (exit_code < 0 || exit_code > 1) //1 - Warning, 2 - Error
            {
                throw new Exception(encodertext.ToString());
            }
            if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
            {
                if (mode == DemuxerMode.ExtractVideo) throw new Exception(Languages.Translate("Can`t find output video file!"));
                if (mode == DemuxerMode.ExtractAudio) throw new Exception(Languages.Translate("Can`t find output audio file!"));
            }
            encodertext.Length = 0;
        }

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            SetProgress((double)e.ProgressPercentage);
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string demuxer = "";
            try
            {
                if (mode == DemuxerMode.DecodeToWAV || mode == DemuxerMode.NeroTempWAV)
                {
                    //Декодируем звук из скрипта
                    demuxer = " (Extract Sound):\r\n";
                    ExtractSound();
                }
                else
                {
                    //Извлекаем аудио или видео из исходника
                    Format.Demuxers dem = Format.GetDemuxer(m);
                    if (dem == Format.Demuxers.mkvextract)
                    {
                        demuxer = " (MKVExtract.exe):\r\n";
                        demux_mkv();
                    }
                    else if (dem == Format.Demuxers.pmpdemuxer)
                    {
                        demuxer = " (PMP_Demuxer.exe):\r\n";
                        demux_pmp();
                    }
                    else if (dem == Format.Demuxers.mp4box)
                    {
                        demuxer = " (MP4Box.exe):\r\n";
                        demux_mp4box();
                    }
                    else if (dem == Format.Demuxers.dpgmuxer)
                    {
                        demuxer = ":\r\n";
                        demux_dpg();
                    }
                    else
                    {
                        demuxer = " (FFmpeg.exe):\r\n";
                        demux_ffmpeg();
                    }
                }
            }
            catch (Exception ex)
            {
                IsErrors = true;
                error_message = "Demuxer" + demuxer + ex.Message;
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Close();
        }

        private void ErrorExeption(string message)
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
                Message mes = new Message(Owner);
                mes.ShowMessage(mtext, mtitle);
            }
        }

        internal delegate void ProgressDelegate(double progress_value);
        private void SetProgress(double progress_value)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ProgressDelegate(SetProgress), progress_value);
            else
            {
                if (progress.IsIndeterminate)
                {
                    progress.IsIndeterminate = false;

                    if (mode == DemuxerMode.DecodeToWAV) label_info.Content = Languages.Translate("Decoding to Windows PCM") + "...";
                    else if (mode == DemuxerMode.NeroTempWAV) label_info.Content = Languages.Translate("Creating Nero temp file") + "...";
                    else if (mode == DemuxerMode.ExtractAudio) label_info.Content = Languages.Translate("Audio demuxing") + "...";
                    else if (mode == DemuxerMode.ExtractVideo) label_info.Content = Languages.Translate("Video demuxing") + "...";
                    else if (mode == DemuxerMode.RepairMKV) label_info.Content = Languages.Translate("Remuxing Matroska file") + "...";
                }

                Title = progress_value.ToString("##0.00") + "%";
                progress.Value = progress_value;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (encoderProcess != null)
            {
                try
                {
                    IsAborted = true;
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
                IsAborted = false;
                e.Cancel = true;
            }

            if (IsAborted || IsErrors)
                SafeDelete(outfile);
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
    }
}