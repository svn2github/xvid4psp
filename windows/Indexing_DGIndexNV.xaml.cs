using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;

namespace XviD4PSP
{
	public partial class Indexing_DGIndexNV
	{
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        private IntPtr Handle = IntPtr.Zero;
        private bool IsError = false;
        private int num_closes = 0;
        private string indexfile;
        public Massive m;

		public Indexing_DGIndexNV()
		{
			this.InitializeComponent();
			// Insert code required on object creation below this point.
		}

        public Indexing_DGIndexNV(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Indexing");
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, ref Handle, false);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (!IsError)
                Win7Taskbar.SetProgressIndeterminate(this, ref Handle);
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
                label_info.Content = Languages.Translate("Aborting... Please wait...");
                Win7Taskbar.SetProgressState(Handle, TBPF.INDETERMINATE);
                prCurrent.IsIndeterminate = true;
                e.Cancel = true;
            }
            else
            {
                //Удаление индекс-папки при отмене или ошибке
                if ((num_closes > 0 || m == null) && File.Exists(indexfile))
                    SafeDirDelete(Path.GetDirectoryName(indexfile));
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

        private void SafeDirDelete(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;

                //Удаляем все файлы, после чего саму папку..
                foreach (string file in Directory.GetFiles(dir))
                    File.Delete(file);

                //..если в ней нет никаких подпапок
                if (Directory.GetDirectories(dir).Length == 0)
                    Directory.Delete(dir, false);
            }
            catch (Exception ex)
            {
                ErrorException("SafeDirDelete: " + ex.Message, ex.StackTrace);
            }
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
                    label_info.Content = Languages.Translate("Indexing") + " (DGIndexNV)...";
                }

                prCurrent.Value = e.ProgressPercentage;
                Title = "(" + e.ProgressPercentage.ToString("0") + "%)";

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), 100);
            }
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try //Индексация
            {
                indexfile = m.indexfile;

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\DGDecNV\\DGIndexNV.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);

                //DGIndexNV.exe отсутствует
                if (!File.Exists(info.FileName))
                {
                    throw new Exception("DGIndexNV <- " + Languages.Translate("You need to obtain your licensed copy of the DGDecNV package from the author (Donald Graft) in order to use it!") +
                    "\r\n" + Languages.Translate("Home page") + ": http://rationalqm.us/dgdecnv/dgdecnv.html");
                }

                //создаём папку
                if (!Directory.Exists(Path.GetDirectoryName(m.indexfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(m.indexfile));

                //создаём список файлов
                string files_list = "";
                for (int i = 0; i < m.infileslist.Length; i++)
                {
                    files_list += "\"" + m.infileslist[i] + "\"";
                    if (i < m.infileslist.Length - 1) files_list += ",";
                }

                //Извлекаем звук, только если он нам нужен
                string ademux = (Settings.EnableAudio) ? " -a" : "";

                //Выходим при отмене
                if (worker.CancellationPending)
                    return;

                //DGIndexNV -i "d:\files\my wedding day.264","d:\files\my divorce day.264" -o "d:\my wedding day.dgi" -e
                info.Arguments = "-i " + files_list + " -o \"" + Calculate.RemoveExtention(m.indexfile, true) + ".dgi\" -h" + ademux;

                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = false;
                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                encoderProcess.PriorityBoostEnabled = true;

                Match mat;
                Regex r = new Regex(@"(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                while (!encoderProcess.HasExited)
                {
                    string line = encoderProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success)
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                    }
                }

                encoderProcess.WaitForExit();

                //Индекс-файл не найден
                if (!worker.CancellationPending && !File.Exists(m.indexfile))
                    throw new Exception(Languages.Translate("Can`t find file") + ": " + m.indexfile);

                if (worker.CancellationPending)
                    return;
            }
            catch (Exception ex)
            {
                if (worker != null && !worker.CancellationPending && m != null && num_closes == 0)
                {
                    //Ошибка
                    e.Result = ex;
                }

                m = null;
                return;
            }

            try //Перепроверка индекса и ForceFilm
            {
                m.IsForcedFilm = CheckIndexAndForceFilm(m.indexfile, m.inframerate);
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

        public static bool CheckIndexAndForceFilm(string indexfile, string inframerate)
        {
            int FilmPercent = 0;
            int size_w = 0, size_h = 0;
            int fps_n = 0, fps_d = 0;
            string line;

            #region
            /*
             * FPS 25000 / 1000
             * CODED 30791
             * PLAYBACK 30791
             * 0.00% FILM
             * ORDER 1
             * -------------
             * SIZ 720 x 480 
             * FPS 30000 / 1001
             * CODED 679
             * PLAYBACK 849
             * 100.00% FILM
             * ORDER 1
             * -------------
             * Файл не проиндексировался:
             * SIZ 0 x 0 
             * FPS 0 / 0
             * CODED 0
             * PLAYBACK 0
             * 0.00% FILM
             * ORDER -1
             */
            #endregion

            //Проверка индекса и получение процента для ForceFilm
            Match mat; //100.00% FILM
            Regex film = new Regex(@"^(\d+\.*\d*)%\sFILM", RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Regex size = new Regex(@"^SIZ\s(\d+)\sx\s(\d+)", RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Regex fps = new Regex(@"^FPS\s(\d+)\s/\s(\d+)", RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            using (StreamReader sr = new StreamReader(indexfile, Encoding.Default))
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();

                    mat = film.Match(line);
                    if (mat.Success)
                    {
                        FilmPercent = Convert.ToInt32(Calculate.ConvertStringToDouble(mat.Groups[1].Value));
                        continue;
                    }

                    mat = size.Match(line);
                    if (mat.Success)
                    {
                        size_w = Convert.ToInt32(mat.Groups[1].Value);
                        size_h = Convert.ToInt32(mat.Groups[2].Value);
                        continue;
                    }

                    mat = fps.Match(line);
                    if (mat.Success)
                    {
                        fps_n = Convert.ToInt32(mat.Groups[1].Value);
                        fps_d = Convert.ToInt32(mat.Groups[2].Value);
                        continue;
                    }
                }

            //Кривой индекс (произошла ошибка)
            if (size_w == 0 || size_h == 0 || fps_n == 0 || fps_d == 0)
            {
                throw new Exception(Languages.Translate("Broken or incomplete index file detected!") + "\r\n" +
                    Languages.Translate("Unsupported by DGDIndexNV container or codec - one of the reasons for this."));
            }

            if (!Settings.DGForceFilm || FilmPercent < Settings.DGFilmPercent)
                return false;

            if (!string.IsNullOrEmpty(inframerate) && (inframerate == "23.976" || inframerate == "29.970"))
                return true;

            return false;
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
                ErrorException("Indexing (DGIndexNV): " + ((Exception)e.Result).Message, ((Exception)e.Result).StackTrace);
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
                IsError = true;
                if (worker != null) worker.WorkerReportsProgress = false;
                if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressTaskComplete(Handle, TBPF.ERROR);

                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
	}
}