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
using System.Runtime.InteropServices;

namespace XviD4PSP
{
    public partial class OpenDialog
    {
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        public ArrayList files = new ArrayList();
        private string arguments;
        private string title;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public OpenDialog(string arguments, System.Windows.Window owner)
        {
            this.InitializeComponent();
            //this.Owner = owner;
            this.arguments = arguments;

            this.Top = 0.0;
            this.Left = 0.0;

            //Когда XviD4PSP станет активным приложением, нужно будет вывести
            //окно SafeOpenDialog на первый план, т.к. само оно этого не сделает
            Application.Current.Activated += new EventHandler(Application_Activated);

            //Определяем текст заголовка, по которому будем искать окно
            if (arguments.StartsWith("oa")) title = Languages.Translate("Select audio files") + ":";
            else if (arguments.StartsWith("sub")) title = Languages.Translate("Select subtitles file") + ":";
            else title = Languages.Translate("Select video files") + ":";

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Application_Activated(object sender, EventArgs e)
        {
            try
            {
                Thread.Sleep(150);

                //Ищем наше окно и выводим его на первый план
                if (encoderProcess != null && !encoderProcess.HasExited)
                {
                    IntPtr w_handle = FindWindow("#32770", title);
                    if (w_handle != IntPtr.Zero) SetForegroundWindow(w_handle);
                }
            }
            catch { }
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.WorkingDirectory = Calculate.StartupPath;
                info.FileName = "SafeOpenDialog.exe";
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = false;
                info.CreateNoWindow = true;
                info.Arguments = arguments;

                try
                {
                    info.StandardOutputEncoding = Encoding.UTF8; //Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                }
                catch
                {
                    info.StandardOutputEncoding = Encoding.Default;
                }

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                encoderProcess.WaitForExit();

                string[] a = encoderProcess.StandardOutput.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string file in a)
                {
                    //Проверка файла на его наличие (мы могли получить мусор из консоли) и на "нехорошие" символы в путях к нему
                    if (File.Exists(file) && Calculate.ValidatePath(file, (a.Length == 1))) files.Add(file);
                    else if (a.Length == 1) throw new FileNotFoundException(Languages.Translate("Can`t find file") + ": " + file);
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
                ShowMessage("SafeOpenDialog: " + ((Exception)e.Result).Message, ((Exception)e.Result).StackTrace);

            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Отпускаем EventHandler
            Application.Current.Activated -= new EventHandler(Application_Activated);

            if (worker != null)
            {
                worker.Dispose();
                worker = null;
            }

            if (encoderProcess != null)
            {
                try
                {
                    if (!encoderProcess.HasExited)
                    {
                        encoderProcess.Kill();
                        encoderProcess.WaitForExit();
                    }
                }
                catch { }
                encoderProcess.Close();
                encoderProcess.Dispose();
                encoderProcess = null;
            }
        }

        internal delegate void MessageDelegate(string data, string info);
        private void ShowMessage(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), data, info);
            else
            {
                Message mes = new Message(/*this.Owner != null ? this.Owner : */Application.Current.MainWindow);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}