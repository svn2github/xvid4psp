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
using System.Windows.Interop;
using System.Collections;
using System.Text;

namespace XviD4PSP
{
    public partial class Indexing_FFMS
    {
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        private IntPtr Handle = IntPtr.Zero;
        private int num_closes = 0;
        public Massive m;

        private ArrayList index_files = new ArrayList();
        private int current = 0;
        private int total = 0;

        //Лог ffmsindex
        private StringBuilder encodertext = new StringBuilder();
        private void AppendEncoderText(string text)
        {
            if (encodertext.Length > 0)
            {
                //Укорачиваем лог, если он слишком длинный
                if (encodertext.Length > 5000)
                {
                    int new_line_pos = encodertext.ToString().IndexOf(Environment.NewLine, 500);
                    if (new_line_pos <= 0) new_line_pos = 500;
                    encodertext.Remove(0, new_line_pos);
                    encodertext.Insert(0, ".....");
                }

                encodertext.Append(Environment.NewLine);
            }
            encodertext.Append(text);
        }

        public Indexing_FFMS(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            total = m.infileslist.Length;
            Title = Languages.Translate("Indexing");
            text_info.Content = Languages.Translate("Please wait... Work in progress...");
            this.ContentRendered += new EventHandler(Window_ContentRendered);

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
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

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //Параметры индексации
                string reindex = (Settings.FFMS_Reindex) ? "-f " : "";
                string timecodes = (Settings.FFMS_TimeCodes) ? "-c " : "";
                string audio = (Settings.EnableAudio && Settings.FFMS_Enable_Audio) ? "-t -1 " : "-t 0 ";

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\ffmsindex.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = false;
                info.CreateNoWindow = true;

                //Будем поочерёдно индексировать все имеющиеся файлы
                for (current = 0; current < m.infileslist.Length && !worker.CancellationPending && m != null; current ++)
                {
                    //Определяем индекс-файл и строчку с аргументами для ffmsindex.exe
                    index_files.Add(((m.ffms_indexintemp) ? Settings.TempPath + "\\" + Path.GetFileName(m.infileslist[current]) : m.infileslist[current]) + ".ffindex");
                    info.Arguments = reindex + timecodes + audio + "\"" + m.infileslist[current] + "\" \"" + index_files[current] + "\"";

                    encoderProcess.StartInfo = info;
                    encoderProcess.Start();
                    encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                    encoderProcess.PriorityBoostEnabled = true;
                    encodertext.Length = 0;

                    string line, pat = @"\.\.\.\s(\d+)%";
                    Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    Match mat;

                    while (!encoderProcess.HasExited)
                    {
                        line = encoderProcess.StandardOutput.ReadLine();
                        if (line != null)
                        {
                            mat = r.Match(line);
                            if (mat.Success)
                                worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                            else
                                AppendEncoderText(line);
                        }
                    }

                    //Ловим ошибки
                    AppendEncoderText(encoderProcess.StandardOutput.ReadToEnd());
                    if (encoderProcess.HasExited && encoderProcess.ExitCode != 0 && encodertext.Length > 0)
                    {
                        //"index file already exists" - это не ошибка, игнорируем
                        if (!encodertext.ToString().Contains("index file already exists"))
                        {
                            //Отрезаем мусор
                            int start = 0;
                            if ((start = encodertext.ToString().IndexOf("Indexing error")) >= 0)
                                throw new Exception("FFIndex: " + encodertext.ToString().Substring(start).Trim());
                            else
                                throw new Exception("FFIndex: " + encodertext.ToString().Trim());
                        }
                    }

                    if (total > current + 1)
                    {
                        encoderProcess.Close();
                        worker.ReportProgress(0);
                    }
                }
            }
            catch (Exception ex)
            {
                if (worker != null && !worker.CancellationPending && m != null && num_closes == 0)
                {
                    //Ошибка
                    e.Result = ex;
                }

                m = null;
            }
        }

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (((BackgroundWorker)sender).WorkerReportsProgress)
            {
                if (prCurrent.IsIndeterminate)
                {
                    prCurrent.IsIndeterminate = false;
                    text_info.Content = Languages.Translate("Indexing") + " (FFMS2)...";
                }

                prCurrent.Value = e.ProgressPercentage;
                if (total > 1)
                    Title = e.ProgressPercentage.ToString("0") + "% (" + (current + 1) + " of " + total + ")";
                else
                    Title = "(" + e.ProgressPercentage.ToString("0") + "%)";

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), 100);
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                m = null;
                ErrorException("Indexing (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                m = null;
                ErrorException("Indexing (FFMS2): " + ((Exception)e.Result).Message, ((Exception)e.Result).StackTrace);
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

            CloseIndexer();

            //Отменяем закрытие окна
            if (cancel_closing)
            {
                worker.WorkerReportsProgress = false;
                text_info.Content = Languages.Translate("Aborting... Please wait...");
                Win7Taskbar.SetProgressState(Handle, TBPF.INDETERMINATE);
                prCurrent.IsIndeterminate = true;
                e.Cancel = true;
            }
            else
            {
                //Удаление индекс-файлов при отмене или ошибке
                if (num_closes > 0 || m == null)
                {
                    foreach (string file in index_files)
                        SafeFileDelete(file);
                }
            }
        }

        private void CloseIndexer()
        {
            lock (locker)
            {
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
                    catch (Exception) { }
                    finally
                    {
                        encoderProcess.Close();
                        encoderProcess.Dispose();
                        encoderProcess = null;
                    }
                }
            }
        }

        private void SafeFileDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                ErrorException("SafeFileDelete: " + ex.Message, ex.StackTrace);
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