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
using System.Text;

namespace XviD4PSP
{
	public partial class OpenDialog
	{

        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        public ArrayList files;
        private string arguments;

        public OpenDialog(string arguments, System.Windows.Window owner)
        {
            this.InitializeComponent();

            //this.Owner = owner;
            this.arguments = arguments;

            this.Top = 0.0;
            this.Left = 0.0;

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
            worker.WorkerSupportsCancellation = true;
        }

        private void GetFiles()
        {
            try
            {
                files = new ArrayList();

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.WorkingDirectory = Calculate.StartupPath;
                info.FileName = "SafeOpenDialog.exe";
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                info.CreateNoWindow = true;

                info.Arguments = arguments;

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                encoderProcess.PriorityClass = ProcessPriorityClass.Normal;

                encoderProcess.WaitForExit();

                string[] separator = new string[] { Environment.NewLine };
                string[] a = encoderProcess.StandardOutput.ReadToEnd().Split(separator, StringSplitOptions.RemoveEmptyEntries);

                foreach (string file in a)
                    files.Add(file);

                //while (!encoderProcess.StandardOutput.EndOfStream)
                //    files.Add(encoderProcess.StandardOutput.ReadLine());

                encoderProcess.Close();
                encoderProcess = null;
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

       private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            GetFiles();
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
                Message mes = new Message(this);
                mes.ShowMessage(data, Languages.Translate("Error"));
            }
        }

        //internal delegate void InfoDelegate(string data);
        //private void SetInfo(string data)
        //{
        //    if (!Application.Current.Dispatcher.CheckAccess())
        //        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new InfoDelegate(SetInfo), data);
        //    else
        //    {
        //        text_info.Content = data;
        //    }
        //}

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
        }

	}
}