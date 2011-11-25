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
using System.Collections;
using System.Globalization;
using System.Windows.Interop;

namespace XviD4PSP
{
    public partial class ScriptRunner
    {
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthReader reader = null;
        private IntPtr Handle = IntPtr.Zero;
        private bool IsAborted = false;
        private string script = null;
        private string label = null;
        private int num_closes = 0;

        public ScriptRunner(string script)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.script = script;

            label = Languages.Translate("Frame XX of YY");
            Title = Languages.Translate("Running the script") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");
            this.ContentRendered += new EventHandler(Window_ContentRendered);

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
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

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                reader = new AviSynthReader();
                reader.ParseScript(script);
                int total = reader.FrameCount;

                for (int i = 0; i < total && !IsAborted; i++)
                {
                    reader.ReadFrameDummy(i);
                    worker.ReportProgress(i + 1, total);
                }
            }
            catch (Exception ex)
            {
                if (!IsAborted && num_closes == 0)
                {
                    e.Result = ex;
                }
            }
            finally
            {
                CloseReader(true);
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (((BackgroundWorker)sender).WorkerReportsProgress)
            {
                if (progress_total.IsIndeterminate)
                {
                    progress_total.IsIndeterminate = false;
                    progress_total.Maximum = (int)e.UserState;
                }

                Title = "(" + ((e.ProgressPercentage * 100) / (int)e.UserState).ToString("0") + "%)";
                label_info.Content = label.Replace("XX", e.ProgressPercentage.ToString()).Replace("YY", e.UserState.ToString());
                progress_total.Value = e.ProgressPercentage;

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), Convert.ToUInt64(e.UserState));
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorException("ScriptRunner (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                //Добавляем скрипт в StackTrace
                string stacktrace = ((Exception)e.Result).StackTrace;
                if (!string.IsNullOrEmpty(script))
                    stacktrace += Calculate.WrapScript(script, 150);

                ErrorException("ScriptRunner: " + ((Exception)e.Result).Message, stacktrace);
            }

            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel_closing = false;

            if (worker != null)
            {
                if (worker.IsBusy && num_closes < 5)
                {
                    //Отмена
                    IsAborted = true;
                    cancel_closing = true;
                    worker.CancelAsync();
                    num_closes += 1;
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

                worker.WorkerReportsProgress = false;
                label_info.Content = Languages.Translate("Aborting... Please wait...");
                Win7Taskbar.SetProgressState(Handle, TBPF.INDETERMINATE);
                progress_total.IsIndeterminate = true;
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