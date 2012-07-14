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
        private DateTime start = DateTime.Now;
        private IntPtr Handle = IntPtr.Zero;
        private bool IsAborted = false;
        private string script = null;
        private string label = null;
        private string elapsed = null;
        private int num_closes = 0;
        private int total = 0;

        public ScriptRunner(string script)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.script = script;

            label = Languages.Translate("Frame XX of YY");
            elapsed = Languages.Translate("Elapsed") + ": ";
            Title = Languages.Translate("Running the script") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");
            label_fps.Content = elapsed + "00:00:00.00   avg fps: 0.00";
            check_autoclose.ToolTip = Languages.Translate("Close window when finished");
            check_autoclose.IsChecked = Settings.CloseScriptRunner;
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
                total = reader.FrameCount;
                start = DateTime.Now;

                for (int i = 0; i < total && !IsAborted; i++)
                {
                    reader.ReadFrameDummy(i);
                    worker.ReportProgress(i + 1, DateTime.Now);
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
                    label = label.Replace("YY", total.ToString());
                    progress_total.IsIndeterminate = false;
                    progress_total.Maximum = total;
                }

                Title = "(" + ((e.ProgressPercentage * 100) / total).ToString("0") + "%)";
                label_info.Content = label.Replace("XX", e.ProgressPercentage.ToString());
                progress_total.Value = e.ProgressPercentage;

                TimeSpan time = ((DateTime)e.UserState) - start;
                label_fps.Content = elapsed + (new DateTime(time.Ticks).ToString("HH:mm:ss.ff")) + "   avg fps: " +
                    (e.ProgressPercentage / time.TotalSeconds).ToString("0.00", CultureInfo.InvariantCulture);

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), Convert.ToUInt64(total));
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorException("ScriptRunner (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
                Close();
            }
            else if (e.Result != null)
            {
                //Добавляем скрипт в StackTrace
                string stacktrace = ((Exception)e.Result).StackTrace;
                if (!string.IsNullOrEmpty(script))
                    stacktrace += Calculate.WrapScript(script, 150);

                ErrorException("ScriptRunner: " + ((Exception)e.Result).Message, stacktrace);
                Close();
            }
            else if (IsAborted || Settings.CloseScriptRunner)
            {
                Close();
            }
            else
            {
                Title = Languages.Translate("Complete");
                if (progress_total.IsIndeterminate)
                {
                    //Прогресс еще не выводился
                    progress_total.IsIndeterminate = false;
                    progress_total.Value = progress_total.Maximum;
                    label_info.Content = label.Replace("XX", total.ToString()).Replace("YY", total.ToString());
                    Win7Taskbar.SetProgressTaskComplete(Handle, TBPF.NORMAL);
                }
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

        private void check_autoclose_Click(object sender, RoutedEventArgs e)
        {
            Settings.CloseScriptRunner = check_autoclose.IsChecked.Value;
        }
    }
}