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

namespace XviD4PSP
{
    public partial class Decoder
    {
        public enum DecoderModes { DecodeVideo, DecodeAudio, DecodeAV };
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        public Massive m;
        private DecoderModes mode;
        private string outfile;
        private FFInfo ff;

        private bool IsAborted = false;
        public bool IsErrors = false;
        public string error_message;
        private int exit_code;
        private string source_file;

        //Лог ffmpeg
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
                }

                encodertext.Append(Environment.NewLine);
            }
            encodertext.Append(text);
        }

        public Decoder(Massive mass, DecoderModes mode, string outfile)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();
            this.outfile = outfile;
            this.mode = mode;

            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            if (mode == DecoderModes.DecodeAudio) Title = Languages.Translate("Audio decoding") + "...";
            else if (mode == DecoderModes.DecodeVideo) Title = Languages.Translate("Video decoding") + "...";
            else if (mode == DecoderModes.DecodeAV) Title = Languages.Translate("LossLess decoding") + "...";

            //Определяем исходный файл
            source_file = (m.infilepath_source != null) ? m.infilepath_source : m.infilepath;

            //фоновое кодирование
            CreateBackgoundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
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

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (progress.IsIndeterminate)
            {
                progress.IsIndeterminate = false;

                if (mode == DecoderModes.DecodeAudio)
                    label_info.Content = Languages.Translate("Audio decoding") + "...";
                else if (mode == DecoderModes.DecodeVideo)
                    label_info.Content = Languages.Translate("Video decoding") + "...";
                else if (mode == DecoderModes.DecodeAV)
                    label_info.Content = Languages.Translate("LossLess decoding") + "...";
            }

            progress.Value = e.ProgressPercentage;
            Title = "(" + e.ProgressPercentage + "%)";
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                if (m.infileslist.Length > 1)
                    ShowMessage(Languages.Translate("Sorry, but stream will be extracted only from first file! :("),
                        Languages.Translate("Warning"));

                //удаляем старый файл
                SafeDelete(outfile);

                //получаем колличество секунд
                ff = new FFInfo();
                ff.Open(source_file);
                int seconds = (int)ff.Duration().TotalSeconds;

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;

                string _format = "";
                string _vcodec = "-vn";
                string _acodec = "-an";
                string yv12 = "";
                string _framerate = "";
                string aspect = "";

                if (mode == DecoderModes.DecodeVideo)
                {
                    _vcodec = "-vcodec ffvhuff";//ffvhuff, ffv1
                    yv12 = " -pix_fmt yuv420p";
                    string dar = ff.StreamDARSelected(ff.FirstVideoStreamID());
                    aspect = (dar != "") ? " -aspect " + dar : "";
                }
                else if (mode == DecoderModes.DecodeAudio)
                {
                    _acodec = "-acodec pcm_s16le";
                }
                else
                {
                    _vcodec = "-vcodec ffvhuff";//ffvhuff, ffv1
                    yv12 = " -pix_fmt yuv420p";
                    _acodec = "-acodec pcm_s16le";
                    m.inframerate = ff.StreamFramerate(ff.FirstVideoStreamID());
                    m.format = Settings.FormatOut;
                    _framerate = " -r " + Format.GetValidFramerate(m).outframerate;
                    string dar = ff.StreamDARSelected(ff.FirstVideoStreamID());
                    aspect = (dar != "") ? " -aspect " + dar : "";
                }

                //закрываем фф
                ff.Close();
                ff = null;

                info.Arguments = "-i \"" + source_file +
                    "\" " + _vcodec + " " + _acodec + _format + yv12 + _framerate + aspect + " \"" + outfile + "\"";

                encoderProcess.StartInfo = info;
                encoderProcess.Start();

                string line, pat = @"time=(\d+.\d+)";
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
                            double ctime = Calculate.ConvertStringToDouble(mat.Groups[1].Value);
                            double pr = ((double)ctime / (double)seconds) * 100.0;
                            worker.ReportProgress((int)pr);
                        }
                        else
                        {
                            AppendEncoderText(line);
                        }
                    }
                }

                //чистим ресурсы
                exit_code = encoderProcess.ExitCode;
                AppendEncoderText(encoderProcess.StandardError.ReadToEnd());
                encoderProcess.Close();
                encoderProcess.Dispose();
                encoderProcess = null;

                if (IsAborted) return;

                //проверка на удачное завершение
                if (exit_code != 0 && encodertext.Length > 0)
                {
                    //Оставляем только последнюю строчку из всего лога
                    string[] log = encodertext.ToString().Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    throw new Exception(log[log.Length - 1]);
                }
                if (!File.Exists(outfile) || new FileInfo(outfile).Length == 0)
                {
                    if (mode == DecoderModes.DecodeVideo) throw new Exception(Languages.Translate("Can`t find output video file!"));
                    if (mode == DecoderModes.DecodeAudio) throw new Exception(Languages.Translate("Can`t find output audio file!"));
                }
            }
            catch (Exception ex)
            {
                IsErrors = true;
                error_message = ex.Message;
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

        internal delegate void InfoDelegate(string data);
        private void SetInfo(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new InfoDelegate(SetInfo), data);
            else
            {
                label_info.Content = data;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ff != null)
            {
                ff.Close();
                ff = null;
            }

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