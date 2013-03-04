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
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using System.Windows.Interop;

namespace XviD4PSP
{
	public partial class Indexing
	{
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        private IntPtr Handle = IntPtr.Zero;
        private bool IsError = false;
        private string filelistpath;
        private string indexfile;
        private int FilmPercent = -1;
        private int num_closes = 0;
        private int iHandle;
        public Massive m;

		public Indexing()
		{
			this.InitializeComponent();
			// Insert code required on object creation below this point.
		}

        public Indexing(Massive mass)
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
                //Удаляем мусор
                SafeFileDelete(filelistpath);

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
                    label_info.Content = Languages.Translate("Indexing") + " (DGIndex)...";
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

                //создаём папку
                if (!Directory.Exists(Path.GetDirectoryName(m.indexfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(m.indexfile));

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\DGMPGDec\\DGIndex.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);

                //создаём список файлов
                string files_list = "";
                foreach (string _line in m.infileslist) files_list += _line + "\r\n";
                File.WriteAllText((filelistpath = Settings.TempPath + "\\" + m.key + ".lst"), files_list, System.Text.Encoding.Default);

                //Выходим при отмене
                if (worker.CancellationPending) return;

                //Извлекаем звук, только если он нам нужен
                string ademux = (Settings.EnableAudio) ? "-OM=2" : "-OM=0";

                info.Arguments = "-SD=\" -FO=0 " + ademux + " -BF=\"" + filelistpath + "\" -OF=\"" + Calculate.RemoveExtention(m.indexfile, true) + "\" -HIDE -EXIT";

                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                encoderProcess.PriorityBoostEnabled = true;

                for (int i = 0; iHandle == 0 && i < 20; i++)
                {
                    iHandle = Win32.GetWindowHandle("DGIndex[");
                    Thread.Sleep(100);
                }

                if (iHandle != 0)
                {
                    Match mat;
                    StringBuilder st = new StringBuilder(256);
                    Regex r = new Regex(@"(\d+)%", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    while (!encoderProcess.HasExited)
                    {
                        if (Win32.GetWindowText(iHandle, st, 256) > 0)
                        {
                            mat = r.Match(st.ToString());
                            if (mat.Success) worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        }
                        Thread.Sleep(100);
                    }
                }

                encoderProcess.WaitForExit();

                //Индекс-файл не найден
                if (!worker.CancellationPending && !File.Exists(m.indexfile))
                    throw new Exception(Languages.Translate("Can`t find file") + ": " + m.indexfile);
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

            try
            {
                //Auto ForceFilm (только для 23.976, 29.970, и если частота неизвестна)
                if (worker.CancellationPending || !Settings.DGForceFilm || !string.IsNullOrEmpty(m.inframerate) && m.inframerate != "23.976" && m.inframerate != "29.970")
                    return;
            }
            catch (Exception)
            {
                //worker или m == null
                return;
            }

            try //Теперь ForceFilm
            {
                FilmPercent = 0;

                //Получение процента для ForceFilm
                Match mat; //FINISHED  94.57% FILM
                Regex r = new Regex(@"FINISHED\s+(\d+\.*\d*)%.FILM", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                    while (!sr.EndOfStream)
                    {
                        mat = r.Match(sr.ReadLine());
                        if (mat.Success) FilmPercent = Convert.ToInt32(Calculate.ConvertStringToDouble(mat.Groups[1].Value));
                    }

                //Выход при отмене, или если процент Film недостаточен
                if (worker.CancellationPending || FilmPercent < Settings.DGFilmPercent) return;

                //Перезапись d2v-файла
                string file = "", line = "";
                using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.StartsWith("Field_Operation")) file += "Field_Operation=1\r\n";
                        else if (line.StartsWith("Frame_Rate")) file += "Frame_Rate=23976 (24000/1001)\r\n";
                        else file += line + Environment.NewLine;
                    }

                using (StreamWriter sw = new StreamWriter(m.indexfile, false, System.Text.Encoding.Default))
                    sw.Write(file);

                m.IsForcedFilm = true;
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
                if (FilmPercent < 0)
                    ErrorException("Indexing (DGIndex): " + ((Exception)e.Result).Message, ((Exception)e.Result).StackTrace);
                else
                    ErrorException("Auto ForceFilm: " + ((Exception)e.Result).Message, ((Exception)e.Result).StackTrace);
            }

            Close();
        }

        public static ArrayList GetTracks(string indexfile) //звук, получение списка звуковых треков
        {
            ArrayList tracklist = new ArrayList();
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(indexfile), "*"))
            {
                string ext = Path.GetExtension(file).ToLower();
                string path = Path.GetFileNameWithoutExtension(indexfile);

                //Отрезаем .track_number (для файлов, полученных после tsMuxeR`а)
                if (path.Length >= 12 && path.Contains(".track_"))
                    path = Regex.Replace(path, @"\.track_\d+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

                if (Path.GetFileNameWithoutExtension(file).StartsWith(path))
                {
                    if (ext == ".mpa" || ext == ".ac3" || ext == ".wav" || ext == ".mp2" || ext == ".mp3" || ext == ".dts" || ext == ".m4a" || ext == ".aac"
                         || ext == ".flac" || ext == ".ape" || ext == ".aiff" || ext == ".aif" || ext == ".wv" || ext == ".ogg" || ext == ".wma")
                    {
                        tracklist.Add(file);
                    }
                }
            }
            return tracklist;
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