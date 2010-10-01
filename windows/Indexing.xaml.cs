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

namespace XviD4PSP
{
	public partial class Indexing
	{
        private BackgroundWorker worker = null;
        private Process encoderProcess = null;
        public Massive m;
        private int iHandle;
        private string filelistpath;
        private int FilmPercent = 0;
        private bool IsAborted;

		public Indexing()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
		}

        public Indexing(Massive mass)
        {
            this.InitializeComponent();

            this.Owner = mass.owner;

            m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Indexing");
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //фоновое кодирование
            CreateBackgoundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                IsAborted = true;
                KillIndexing();
                if (File.Exists(m.indexfile)) SafeDirDelete(Path.GetDirectoryName(m.indexfile));
                SafeFileDelete(filelistpath);
                m = null;
            }
        }

        private void KillIndexing()
        {
            //прибиваем dgindex
            if (encoderProcess != null)
            {
                if (!encoderProcess.HasExited)
                {
                    encoderProcess.Kill();
                    encoderProcess.WaitForExit();
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
                ErrorExeption(ex.Message);
            }
        }

        private void SafeDirDelete(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
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
            if (prCurrent.IsIndeterminate)
            {
                prCurrent.IsIndeterminate = false;
                label_info.Content = Languages.Translate("Indexing") + "...";
            }

            prCurrent.Value = e.ProgressPercentage;
            Title = "(" + e.ProgressPercentage.ToString("0") + "%)";
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try //Индексация
            {
                //создаём папку
                if (!Directory.Exists(Path.GetDirectoryName(m.indexfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(m.indexfile));

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\DGMPGDec\\DGIndex.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);

                //создаём список файлов
                filelistpath = Settings.TempPath + "\\" + m.key + ".lst";
                StreamWriter sw = new StreamWriter(filelistpath, false, System.Text.Encoding.Default);
                foreach (string _line in m.infileslist) sw.WriteLine(_line);
                sw.Close();

                //Извлекаем звук, только если он нам нужен
                string ademux = (Settings.EnableAudio) ? "-OM=2" : "-OM=0";

                info.Arguments = "-SD=\" -IA=6 -FO=0 " + ademux + " -BF=\"" + filelistpath + "\" -OF=\"" + Calculate.RemoveExtention(m.indexfile, true) + "\" -HIDE -EXIT";

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

                if (!IsAborted && !File.Exists(m.indexfile))
                    throw new Exception(Languages.Translate("Can`t find file") + ": " + m.indexfile);
            }
            catch (Exception ex)
            {
                ErrorExeption("Indexing (DGIndex): " + ex.Message);
                m = null;
                return;
            }

            //Auto ForceFilm (только для 23.976, 29.970, и если частота неизвестна)
            if (IsAborted || !Settings.DGForceFilm || !string.IsNullOrEmpty(m.inframerate) && m.inframerate != "23.976" && m.inframerate != "29.970")
                return;

            try
            {
                //Получение процента для ForceFilm
                Match mat; //FINISHED  94.57% FILM
                Regex r = new Regex(@"FINISHED\s+(\d+\.*\d*)%.FILM", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                    while (!sr.EndOfStream)
                    {
                        mat = r.Match(sr.ReadLine());
                        if (mat.Success) FilmPercent = Convert.ToInt32(Calculate.ConvertStringToDouble(mat.Groups[1].Value));
                    }

                //Выход, если процент Film недостаточен
                if (FilmPercent < Settings.DGFilmPercent) return;

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
                ErrorExeption("Auto ForceFilm: " + ex.Message);
                m = null;
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            SafeFileDelete(filelistpath);
            Close();
        }

        public static ArrayList GetTracks(string indexfile) //звук, получение списка звуковых треков
        {
            ArrayList tracklist = new ArrayList();
            foreach (string f in Directory.GetFiles(Path.GetDirectoryName(indexfile), "*"))
            {
                string ext = Path.GetExtension(f);
                string path = Path.GetFileNameWithoutExtension(indexfile);
                
                //Отрезаем .track_number (для файлов полученых после tsMuxeR`а)
                if (path.Length >= 12 && path.Contains(".track_"))
                {
                  //path = path.Substring(0, path.Length - 11);
                    Regex r = new Regex(@"(\.track_\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    Match mat;
                    mat = r.Match(path);
                    if (mat.Success == true)
                    {
                        path = path.Replace(mat.Groups[1].Value, "");
                    }
                }

                if (f.Contains(path))
                {
                    if (ext == ".mpa" || ext == ".ac3" || ext == ".wav" || ext == ".mp2" || ext == ".mp3" || ext == ".dts" || ext == ".m4a" || ext == ".aac"
                         || ext == ".flac" || ext == ".ape" || ext == ".aiff" || ext == ".aif" || ext == ".wv" || ext == ".ogg" || ext == ".wma")
                    {
                        tracklist.Add(f);
                    }
                }
            }
            return tracklist;
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
                Message mes = new Message(this.Owner);
                mes.ShowMessage(data, Languages.Translate("Error"));
            }
        }
	}
}