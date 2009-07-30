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
using System.Timers;
using System.Text.RegularExpressions;

namespace XviD4PSP
{
    public partial class Autocrop
    {
        private BackgroundWorker worker = null;
        public Massive m;
        private int progress = 0;
        AviSynthReader reader;
        private bool IsAborted = false;

        public enum AutocropMode { AllFiles = 1, Disabled, MPEGOnly }

        public Autocrop()
        {
            this.InitializeComponent();
        }

        public Autocrop(Massive mass)
        {
            this.InitializeComponent();

            this.Owner = mass.owner;

            m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Detecting black borders") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

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

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (m != null)
            {
                string tmp_title = "(" + progress.ToString("##0") + "%)";
                SetStatus(tmp_title, "", progress);
                progress++;
            }
        }

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {

        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //запускаем таймер
                //timer = new System.Timers.Timer();
                //timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                //timer.Interval = 10;
                //timer.Enabled = true;

                //получаем автокроп инфу
                string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Autocrop);

                reader = new AviSynthReader();
                reader.ParseScript(script);
               
             
                
            //отсюда начинается чтение и анализ лог-файла
            if (!IsAborted)
            {   
                string croplog;
                using (StreamReader sr = new StreamReader(Settings.TempPath + "\\AutoCrop.log", System.Text.Encoding.Default))
                          croplog = sr.ReadToEnd();
                int result1; int result2; int result3; int result4;

                Regex r = new Regex(@"Crop.(\d+),(\d+),(\d+),(\d+).", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                Match mat;
                mat = r.Match(croplog);
                if (mat.Success == true)
                {
                    result1 = Convert.ToInt32(mat.Groups[1].Value);
                    result2 = Convert.ToInt32(mat.Groups[2].Value);
                    result3 = Convert.ToInt32(mat.Groups[3].Value);
                    result4 = Convert.ToInt32(mat.Groups[4].Value);
                }
                else
                {
                    result1 = 0;
                    result2 = 0;
                    result3 = m.inresw;
                    result4 = m.inresh;
                }

                int cropl = result1; //слева
                int cropr = m.inresw - result1 - result3;  //справа
                int cropb = m.inresh - result2 - result4; //низ
                int cropt = result2;  //верх
                
                //в массив
                m.cropl = cropl;
                m.cropr = cropr;
                m.cropb = cropb;
                m.cropt = cropt;

                //дубликаты
                m.cropl_copy = cropl;
                m.cropr_copy = cropr;
                m.cropb_copy = cropb;
                m.cropt_copy = cropt;

            //  Settings.Test = result1;
                
             
                
          //остатки старого автокропа (по разнице разрешений)      
          //      if (!IsAborted)
          //      {
           //         int cropw = m.inresw - reader.Width;
           //         if (cropw > 0)
           //             cropw /= 2;
           //         int croph = m.inresh - reader.Height;
           //         if (croph > 0)
           //             croph /= 2;

  //                  cropw = Calculate.GetValid(cropw, 2);
 //                   croph = Calculate.GetValid(croph, 2);

//                    m.cropl = cropw;
  //                  m.cropr = cropw;
    //                m.cropb = croph;
      //              m.cropt = croph;

                    //дубликаты
        //            m.cropl_copy = cropw;
        //            m.cropr_copy = cropw;
         //           m.cropb_copy = croph;
         //           m.cropt_copy = croph;
                }
            }
            catch (Exception ex)
            {
                //записываем скрипт с ошибкой в файл
                AviSynthScripting.WriteScriptToFile(AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Autocrop), "error");

                string mess = ex.Message;
                if (ex.Message == "Cannot load avisynth.dll")
                {
                    m = null;
                    mess = Languages.Translate("AviSynth is not found!") + Environment.NewLine +
                        Languages.Translate("Please install AviSynth 2.5.7 or higher.");
                }
                ShowMessage(mess, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
            finally
            {
                reader.Close();
                reader = null;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                IsAborted = true;
                worker.CancelAsync();
                if (reader != null)
                    reader.Close();
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //timer.Close();
            //timer.Enabled = false;
            //timer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);

            Close();
        }

        internal delegate void MessageDelegate(string data, string title, Message.MessageStyle style);
        private void ShowMessage(string data, string title, Message.MessageStyle style)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), data, title, style);
            else
            {
                Message mes = new Message(this.Owner);
                mes.ShowMessage(data, title, style);
            }
        }

        internal delegate void StatusDelegate(string title, string pr_text, double pr_c);
        private void SetStatus(string title, string pr_text, double pr_c)
        {
            if (m != null)
            {
                if (!Application.Current.Dispatcher.CheckAccess())
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new StatusDelegate(SetStatus), title, pr_text, pr_c);
                else
                {
                    //this.Title = title;
                    //this.tbxProgress.Text = pr_text;
                    this.prCurrent.Value = pr_c;
                }
            }
        }

    }
}