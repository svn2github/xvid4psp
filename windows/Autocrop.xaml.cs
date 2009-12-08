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

        public Autocrop(Massive mass, System.Windows.Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;

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
                    int result1 = 0, result2 = 0, result3 = m.inresw, result4 = m.inresh;

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

                    //в массив //дубликаты
                    m.cropl = m.cropl_copy = result1;                       //слева
                    m.cropr = m.cropr_copy = m.inresw - result1 - result3;  //справа
                    m.cropb = m.cropb_copy = m.inresh - result2 - result4;  //низ
                    m.cropt = m.cropt_copy = result2;                       //верх
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
                        Languages.Translate("Please install AviSynth 2.5.7 MT or higher.");
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