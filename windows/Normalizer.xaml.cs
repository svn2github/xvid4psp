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

namespace XviD4PSP
{
	public partial class Normalize
	{

        private BackgroundWorker worker = null;
        private AviSynthEncoder avs = null;
        private Process encoderProcess = null;
        public Massive m;
        private int vtrim;
        private int step = 0;
        private string norm_string;

        public Normalize(Massive mass)
        {
            this.InitializeComponent();

            this.Owner = mass.owner;
            m = mass.Clone();

            //колличество обрабатываемых фреймов
            int accuratepr = Convert.ToInt32(m.volumeaccurate.Replace("%", ""));
            vtrim = Calculate.GetProcentValue(m.inframes, accuratepr);
            if (vtrim < 10000)
            {
                vtrim = 10000;
                if (vtrim > m.inframes)
                    vtrim = m.inframes;
            }

           // vtrim = 1000;
            //забиваем
            prCurrent.Maximum = vtrim;
            prTotal.Maximum = vtrim * 2;
            norm_string = Languages.Translate("Normalizer");

            prCurrent.ToolTip = Languages.Translate("Current progress");
            prTotal.ToolTip = Languages.Translate("Total progress");
            Title = Languages.Translate("Normalizer");
            text_info.Content = Languages.Translate("Sound extracting...");

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

        private void ExtractSound()
        {
            AudioStream stream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Normalize);

            script += Environment.NewLine +"Trim(0, " + vtrim + ")";

            //AviSynthScripting.WriteScriptToFile(script, "test");

            try
            {
                //удаляем старый файл
                SafeDelete(stream.gainfile);

                //создаём кодер
                avs = new AviSynthEncoder(m, script, stream.gainfile);

                //запускаем кодер
                avs.start();

                //извлекаем кусок на ext_frames фреймов
                while (avs.IsBusy())
                {
                    worker.ReportProgress(avs.frame);
                    Thread.Sleep(100);
                }

                //чистим ресурсы
                avs = null;
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void GetGain()
        {
            string encodertext = "";
            step++;

            encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.WorkingDirectory = Calculate.StartupPath;
            info.FileName = Calculate.StartupPath + "\\apps\\normalize\\normalize.exe";
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            AudioStream stream = (AudioStream)m.inaudiostreams[m.inaudiostream];

            info.Arguments = "-m " + m.volume.Replace("%", "") + " -p \"" + stream.gainfile + "\"";

            encoderProcess.StartInfo = info;
            encoderProcess.Start();
            encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
            encoderProcess.PriorityBoostEnabled = true;

            encoderProcess.WaitForExit();

            encodertext += encoderProcess.StandardOutput.ReadToEnd();
            encodertext += encoderProcess.StandardError.ReadToEnd();
          
            //чистим ресурсы
            encoderProcess.Close();
            encoderProcess.Dispose();
            encoderProcess = null;

            //забиваем гейн
            string pat = @"(\d+.\d+)\DdB";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat = r.Match(encodertext);

            if (mat.Success == true)
            {
                stream.gain = mat.Groups[1].Value;
                stream.gaindetected = true;
            }
            else
            {
                stream.gain = "0.0";
                stream.gaindetected = false;
            }
        }

       private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            //получаем текущий фрейм
            int cf = e.ProgressPercentage;

            //вычисляем проценты прогресса
            double pr = ((double)cf / vtrim) * 100.0;
            Title = "(" + pr.ToString("##0.00") + "%)";

            prCurrent.Value = cf;
            prTotal.Value = cf + (step * vtrim);
        }

       private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                //извлекаем кусок звука
                //if (!File.Exists(instream.gainfile))
                    ExtractSound();

                //проверка на удачное завершение
                FileInfo finfo = new FileInfo(instream.gainfile);
                if (!File.Exists(instream.gainfile))
                {
                    //throw new Exception(Languages.Translate("Can`t create gain file!"));
                    return;
                }
                else
                {
                    if (finfo.Length == 0)
                    {
                        //throw new Exception(Languages.Translate("Can`t create gain file!"));
                        return;
                    }
                }

                //определяем его громкость
                SetInfo(Languages.Translate("Volume gain detecting..."));
                GetGain();
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

       private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //SafeDelete(gain_file);
            Close();
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

        private void ErrorExeption(string message)
        {
            ShowMessage(this, message);
        }

        internal delegate void MessageDelegate(object sender, string data);
        private void ShowMessage(object sender, string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), sender, data);
            else
            {
                Message mes = new Message(this);
                mes.ShowMessage(data, Languages.Translate("Error"));
            }
        }

        internal delegate void InfoDelegate(string data);
        private void SetInfo(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new InfoDelegate(SetInfo), data);
            else
            {
                text_info.Content = data;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
        }

	}
}