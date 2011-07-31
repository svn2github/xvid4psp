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
using System.Windows.Interop;

namespace XviD4PSP
{
    public partial class Normalize
    {
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthEncoder avs = null;
        private IntPtr Handle = IntPtr.Zero;
        private int num_closes = 0;
        private string script;
        private int vtrim;
        public Massive m;

        public Normalize(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //колличество обрабатываемых фреймов
            int accuratepr = Convert.ToInt32(m.volumeaccurate.Replace("%", ""));
            vtrim = Calculate.GetProcentValue(m.inframes, accuratepr);
            if (vtrim < 10000) vtrim = Math.Min(10000, m.inframes);

            //забиваем
            prCurrent.Maximum = vtrim;
            prCurrent.ToolTip = Languages.Translate("Current progress");
            Title = Languages.Translate("Normalizer");
            text_info.Content = Languages.Translate("Please wait... Work in progress...");
            this.ContentRendered += new EventHandler(Window_ContentRendered);

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            //Сворачиваем окно, если программа минимизирована или свернута в трей
            if (!Owner.IsVisible || Owner.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Minimized;
                this.SizeToContent = System.Windows.SizeToContent.Manual;
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
                this.SizeToContent = System.Windows.SizeToContent.Width;
                if (!Owner.IsVisible) Owner.Show();
                if (Owner.WindowState == System.Windows.WindowState.Minimized)
                    Owner.WindowState = System.Windows.WindowState.Normal;
            }
        }

        void Window_ContentRendered(object sender, EventArgs e)
        {
            if (Handle == IntPtr.Zero)
                Win7Taskbar.SetProgressIndeterminate(this, ref Handle);
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
            if (((BackgroundWorker)sender).WorkerReportsProgress)
            {
                if (prCurrent.IsIndeterminate)
                {
                    prCurrent.IsIndeterminate = false;
                    text_info.Content = Languages.Translate("Volume gain detecting...");
                }

                //получаем текущий фрейм
                double cf = prCurrent.Value = e.ProgressPercentage;

                //вычисляем проценты прогресса
                double pr = (cf / vtrim) * 100.0;
                Title = "(" + pr.ToString("0") + "%)";

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), Convert.ToUInt64(vtrim));
            }
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Normalize);
                script += Environment.NewLine + "Trim(0, " + vtrim + ")";

                avs = new AviSynthEncoder(m, script);

                //Запускаем анализ
                avs.start();

                //Выводим прогресс
                while (avs.IsBusy())
                {
                    if (worker.CancellationPending) avs.stop();
                    else if (avs.frame > 0) worker.ReportProgress(avs.frame);
                    Thread.Sleep(100);
                }

                //Результаты
                if (!worker.CancellationPending)
                {
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
                        throw new Exception(avs.error_text, avs.exception_raw);
                    }
                }
            }
            catch (Exception ex)
            {
                if (worker != null && !worker.CancellationPending && m != null && num_closes == 0)
                {
                    //Ошибка
                    ex.HelpLink = script;
                    e.Result = ex;
                }
            }
            finally
            {
                CloseEncoder(true);
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
                //CloseEncoder(false);

                worker.WorkerReportsProgress = false;
                text_info.Content = Languages.Translate("Aborting... Please wait...");
                Win7Taskbar.SetProgressState(Handle, TBPF.INDETERMINATE);
                prCurrent.IsIndeterminate = true;
                e.Cancel = true;
            }
            else
                CloseEncoder(true);
        }

        private void CloseEncoder(bool _null)
        {
            lock (locker)
            {
                if (avs != null)
                {
                    if (avs.IsBusy()) avs.stop();
                    if (_null) avs = null;
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                m = null;
                ErrorException("Normalizer (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                m = null;

                Exception ex = (Exception)e.Result;
                string stacktrace = ex.StackTrace;

                //Добавляем StackTrace из AviSynthEncoder
                if (ex.InnerException != null)
                    stacktrace += "\r\n" + ex.InnerException.StackTrace;

                //Добавляем в StackTrace текущий скрипт
                if (!string.IsNullOrEmpty(ex.HelpLink))
                    stacktrace += Calculate.WrapScript(ex.HelpLink, 150);

                ErrorException("Normalizer: " + ex.Message, stacktrace);
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
                if (worker != null) worker.WorkerReportsProgress = false;
                if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressTaskComplete(Handle, TBPF.ERROR);

                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}