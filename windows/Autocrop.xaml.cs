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
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthReader reader = null;
        private int num_closes = 0;
        private string script;
        public Massive m;

        public enum AutocropMode { AllFiles = 1, Disabled, MPEGOnly }

        public Autocrop()
        {
            this.InitializeComponent();
        }

        public Autocrop(Massive mass, System.Windows.Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Detecting black borders") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //получаем автокроп инфу
                script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Autocrop);

                reader = new AviSynthReader();
                reader.ParseScript(script);

                //Чтение и анализ лог-файла
                if (!worker.CancellationPending)
                {
                    int result1 = 0, result2 = 0, result3 = m.inresw, result4 = m.inresh;
                    using (StreamReader sr = new StreamReader(Settings.TempPath + "\\AutoCrop.log", System.Text.Encoding.Default))
                    {
                        Match mat;
                        Regex r = new Regex(@"Crop.(\d+),(\d+),(\d+),(\d+).", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        mat = r.Match(sr.ReadToEnd());
                        if (mat.Success)
                        {
                            result1 = Convert.ToInt32(mat.Groups[1].Value);
                            result2 = Convert.ToInt32(mat.Groups[2].Value);
                            result3 = Convert.ToInt32(mat.Groups[3].Value);
                            result4 = Convert.ToInt32(mat.Groups[4].Value);
                        }
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
                if (worker != null && !worker.CancellationPending && m != null && num_closes == 0)
                {
                    //Ошибка
                    ex.HelpLink = script;
                    e.Result = ex;

                    try
                    {
                        //записываем скрипт с ошибкой в файл
                        AviSynthScripting.WriteScriptToFile(script, "error");
                    }
                    catch { }
                }
            }
            finally
            {
                CloseReader(true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel_closing = false;

            if (worker != null)
            {
                if (worker.IsBusy && num_closes < 5)
                {
                    //Отмена
                    cancel_closing = true;
                    worker.CancelAsync();
                    num_closes += 1;
                    m = null;
                }
                else
                {
                    worker.Dispose();
                    worker = null;
                }
            }

            //Отменяем закрытие окна
            if (cancel_closing)
            {
                //CloseReader(false);

                label_info.Content = Languages.Translate("Aborting... Please wait...");
                e.Cancel = true;
            }
            else
                CloseReader(true);
        }

        private void CloseReader(bool _null)
        {
            lock (locker)
            {
                if (reader != null)
                {
                    reader.Close();
                    if (_null) reader = null;
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                m = null;
                ErrorException("AutoCrop (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                m = null;

                //Добавляем скрипт в StackTrace
                string stacktrace = ((Exception)e.Result).StackTrace;
                if (!string.IsNullOrEmpty(((Exception)e.Result).HelpLink))
                    stacktrace += Calculate.WrapScript(((Exception)e.Result).HelpLink, 150);

                ErrorException("AutoCrop: " + ((Exception)e.Result).Message, stacktrace);
            }

            Close();
        }

        internal delegate void ErrorExceptionDelegate(string data, string info);
        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ErrorExceptionDelegate(ErrorException), data, info);
            else
            {
                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}