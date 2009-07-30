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
using System.Collections;

namespace XviD4PSP
{
	public partial class MKVRebuilder
	{
        private BackgroundWorker worker = null;
        private ManualResetEvent locker = new ManualResetEvent(true);
        private Process encoderProcess = null;

        private string infile;
        private string outfile;

        public MKVRebuilder(System.Windows.Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;

            //переводим
            button_cancel.Content = Languages.Translate("Cancel");
            button_start.Content = Languages.Translate("Start");
            tab_main.Header = Languages.Translate("Main");
            tab_log.Header = Languages.Translate("Log");
            label_infile.Content = Languages.Translate("Input file path:");
            label_outfile.Content = Languages.Translate("Output file path:");
            progress.Maximum = 100;

            Show();
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

       private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progress.Value = e.ProgressPercentage;
            Title = "(" +  e.ProgressPercentage + "%)";
        }

       private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //удаляем старый файл
                SafeDelete(outfile);

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvmerge.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;

                info.Arguments = "-S \"" + infile + "\" -o \"" + outfile + "\"";

                SetLog(info.Arguments);

                encoderProcess.StartInfo = info;
                encoderProcess.Start();

                string line;
                string pat = @"progress:\D(\d+)%";
                Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                Match mat;

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardOutput.ReadLine();

                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        else
                            SetLog(line);
                    }
                }

                //чистим ресурсы
                encoderProcess.Close();
                encoderProcess.Dispose();
                encoderProcess = null;
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

       private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            ////проверка на удачное завершение
            //if (File.Exists(outfile))
            //{
            //    FileInfo outinfo = new FileInfo(outfile);
            //    if (outinfo.Length > 20000)
            //    {
            //        Message mes = new Message(this);
            //        mes.ShowMessage(Path.GetFileName(outfile) + " " + Languages.Translate("ready") + "!", Languages.Translate("Complete") + "!");
            //    }
            //}
            button_start.Content = Languages.Translate("Start");
            Title = "MKVRebuilder";
            progress.Value = 0;
            tabs.SelectedIndex = 0;
        }


       private void ErrorExeption(string message)
       {
           ShowMessage(message, Languages.Translate("Error"));
       }

       internal delegate void MessageDelegate(string mtext, string mtitle);
        private void ShowMessage(string mtext, string mtitle)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), mtext, mtitle);
            else
            {
                Message mes = new Message(this);
                mes.ShowMessage(mtext, mtitle);
            }
        }

        internal delegate void LogDelegate(string data);
        private void SetLog(string data)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new LogDelegate(SetLog), data);
            else
            {
                textbox_log.AppendText(data + Environment.NewLine);
                textbox_log.ScrollToEnd();
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
                SafeDelete(outfile);
            }    
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

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy)
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
            else if (worker == null || 
                !worker.IsBusy)
                Close();
        }

        private void button_start_Click(object sender, RoutedEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                if (button_start.Content.ToString() == Languages.Translate("Pause"))
                {
                    locker.Reset();
                    button_start.Content = Languages.Translate("Resume");   
                }
                else
                {
                    locker.Set();
                    button_start.Content = Languages.Translate("Pause");
                }
            }
            else
            {
                if (textbox_infile.Text != "" &&
                    File.Exists(textbox_infile.Text) &&
                    textbox_outfile.Text != "")
                {
                    button_start.Content = Languages.Translate("Pause");  
                    tabs.SelectedIndex = 1;

                    //запоминаем переменные
                    infile = textbox_infile.Text;
                    outfile = textbox_outfile.Text;

                    textbox_log.Clear();

                    //фоновое кодирование
                    CreateBackgoundWorker();
                    worker.RunWorkerAsync();
                }
            }
        }

        private void button_open_Click(object sender, RoutedEventArgs e)
        {
            if (worker == null || !worker.IsBusy)
            {
                ArrayList files = OpenDialogs.GetFilesFromConsole("mkv");
                if (files.Count > 0)
                {
                    textbox_infile.Text = files[0].ToString();
                    textbox_outfile.Text = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed.mkv";
                }
            }
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_infile.Text != "")
            {
                System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                s.AddExtension = true;
                s.SupportMultiDottedExtensions = true;
                s.Title = Languages.Translate("Select unique name for output file:");
                s.DefaultExt = ".mkv";
                s.FileName = textbox_outfile.Text;
                s.Filter = "MKV " + Languages.Translate("files") + "|*.mkv";

                if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string ext = Path.GetExtension(s.FileName).ToLower();

                    if (!s.DefaultExt.StartsWith("."))
                        s.DefaultExt = "." + s.DefaultExt;

                    if (ext != s.DefaultExt)
                        s.FileName += s.DefaultExt;
                    textbox_outfile.Text = s.FileName;
                }
            }
        }

	}
}