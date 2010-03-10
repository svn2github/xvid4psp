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
        public Massive m;
        private int vtrim;

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

            //забиваем
            prCurrent.Maximum = vtrim;

            prCurrent.ToolTip = Languages.Translate("Current progress");
            Title = Languages.Translate("Normalizer");
            text_info.Content = Languages.Translate("Volume gain detecting...");

            //фоновое кодирование
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
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

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            //получаем текущий фрейм
            int cf = e.ProgressPercentage;

            //вычисляем проценты прогресса
            double pr = ((double)cf / vtrim) * 100.0;
            Title = "(" + pr.ToString("##0.00") + "%)";

            prCurrent.Value = cf;
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Normalize);
                script += Environment.NewLine + "Trim(0, " + vtrim + ")";

                avs = new AviSynthEncoder(m, script);

                //запускаем кодер
                avs.start();

                //Выводим прогресс
                while (avs.IsBusy())
                {
                    worker.ReportProgress(avs.frame);
                    Thread.Sleep(100);
                }

                if (!avs.IsErrors)
                {
                    instream.gain = avs.gain.ToString("##0.000").Replace(",", ".");
                    if (instream.gain == "0.000") instream.gain = "0.0";
                    instream.gaindetected = true;
                }
                else
                {
                    instream.gain = "0.0";
                    instream.gaindetected = false;
                    ErrorExeption(avs.error_text);
                }
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
            finally
            {
                //чистим ресурсы
                if (avs != null) avs = null;
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Close();
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
                Message mes = new Message(Owner);
                mes.ShowMessage(data, Languages.Translate("Error"));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (avs != null && avs.IsBusy())
                avs.stop();
        }
    }
}