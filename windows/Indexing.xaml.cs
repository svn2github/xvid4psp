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
                KillIndexing();
                if (File.Exists(m.indexfile))
                    SafeDirDelete(Path.GetDirectoryName(m.indexfile));
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
            try
            {
                //создаём папку
                if (!Directory.Exists(Path.GetDirectoryName(m.indexfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(m.indexfile));

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.WorkingDirectory = Calculate.StartupPath + "\\DGMPGDec";
                info.FileName = Calculate.StartupPath + "\\apps\\DGMPGDec\\DGIndex.exe";

                //info.CreateNoWindow = true;
                //info.WindowStyle = ProcessWindowStyle.Hidden;

                //создаём список файлов
                filelistpath = Settings.TempPath + "\\" + m.key + ".lst";
                StreamWriter sw = new StreamWriter(filelistpath, false, System.Text.Encoding.Default);
                foreach (string _line in m.infileslist)
                    sw.WriteLine(_line);
                sw.Close();

                info.Arguments = "-SD=\" -IA=6 -FO=0 -OM=2 -BF=\"" + filelistpath +
                                 "\" -OF=\"" + Calculate.RemoveExtention(m.indexfile, true) + "\" -HIDE -EXIT";
               
                encoderProcess.StartInfo = info;
                encoderProcess.Start();
                encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                encoderProcess.PriorityBoostEnabled = true;

                int trynum = 0;
                while (iHandle == 0 && trynum < 20)
                {
                    iHandle = Win32.GetWindowHandle("DGIndex[");
                    Thread.Sleep(100);
                    trynum++;
                }

                if (trynum < 20)
                {
                    StringBuilder st = new StringBuilder(256);
                    string line;
                    int iReturn;
                    string pat = @"(\d+)%";
                    Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    Match mat;
                    while (!encoderProcess.HasExited)
                    {
                        iReturn = Win32.GetWindowText(iHandle, st, 256);
                        line = st.ToString();
                        mat = r.Match(line);
                        if (line != null)
                        {
                            if (mat.Success == true)
                            {
                                worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                            }
                        }
                        Thread.Sleep(50);
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            SafeFileDelete(filelistpath);
            Close();
        }

        public static string GetTrack(Massive mass)
        {
            foreach (string f in Directory.GetFiles(Path.GetDirectoryName(mass.indexfile), "*"))
            {
                string ext = Path.GetExtension(f);
                if (f.Contains(Path.GetFileNameWithoutExtension(mass.indexfile)))
                {
                    if (ext != ".d2v" && ext != ".txt" && ext != ".bad")
                        return f;
                }
            }
            return null;
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
                    if (ext != ".d2v" && ext != ".txt" && ext != ".bad" && ext != ".log" && ext != ".dga" && ext != ".d2a" && ext != ".m2ts" && ext != ".h264" && ext != ".264" && ext != ".avc")// AVC, .bad") добавил другие расширения
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