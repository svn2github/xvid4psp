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

namespace XviD4PSP
{
    public enum SourceType
    {
        UNKNOWN, NOT_ENOUGH_SECTIONS,
        PROGRESSIVE, INTERLACED, FILM, DECIMATING,
        HYBRID_FILM_INTERLACED, HYBRID_PROGRESSIVE_INTERLACED, HYBRID_PROGRESSIVE_FILM
    };

    public enum FieldOrder
    {
        UNKNOWN, VARIABLE, TFF, BFF
    };

    public enum DeinterlaceType
    {
        Disabled, Yadif, YadifModEDI, TDeint, TDeintEDI, LeakKernelDeint, TomsMoComp, FieldDeinterlace, SmoothDeinterlace, NNEDI, MCBob, TIVTC, TDecimate
    };

	public partial class SourceDetector
	{

        private BackgroundWorker worker = null;
        public Massive m;
        private Process encoderProcess = null;
        private bool IsErrors = false;
        private bool IsAborted = false;

		public SourceDetector()
		{
			this.InitializeComponent();
		}

        public SourceDetector(Massive mass)
        {
            this.InitializeComponent();

            this.Owner = mass.owner;

            m = mass.Clone();

            Title = Languages.Translate("Detecting interlace") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");
            progress_total.Maximum = 100;

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
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (progress_total.IsIndeterminate)
            {
                progress_total.IsIndeterminate = false;
                label_info.Content = Languages.Translate("Detecting interlace") + "...";
            }

            progress_total.Value = e.ProgressPercentage;
            Title = "(" + e.ProgressPercentage.ToString("0") + "%)";
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsErrors && !IsAborted)
                ErrorExeption(Languages.Translate("You don`t have YV12 decoder! It`s needed for source interlace detection."));

            if (IsErrors)
                m = null;

            Close();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //создаём скрипт для деинтерлейса
                string dscript = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Interlace);
                AviSynthScripting.WriteScriptToFile(dscript, "interlace");

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.WorkingDirectory = Calculate.StartupPath;
                info.FileName = Calculate.StartupPath + "\\apps\\bautodeint\\bautodeint.exe";
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;

                info.Arguments = "--input \"" + Settings.TempPath + "\\interlace.avs\"";

                encoderProcess.StartInfo = info;

                string line;
                string pat = @"(\d+)%\Dcompleted";
                Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                Match mat;

                encoderProcess.Start();
                encoderProcess.PriorityClass = ProcessPriorityClass.Normal;

                string stdout = "";

                int n = 0;
                while (!encoderProcess.HasExited)
                {
                    line = encoderProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            n++;
                            worker.ReportProgress(Convert.ToInt32(mat.Groups[1].Value));
                        }
                        else
                        {
                            if (line.StartsWith("BAutoDeint [info]: Number of sections of type"))
                                SetFieldPhase();
                            stdout += line + Environment.NewLine;
                        }
                    }
                }

                //чистим ресурсы
                encoderProcess.Close();
                encoderProcess.Dispose();
                encoderProcess = null;

                //значения по умолчанию
                m.interlace = SourceType.UNKNOWN;
                m.fieldOrder = FieldOrder.UNKNOWN;

                //если ничего не получили, значит есть косяк
                if (stdout == "" && n == 0)
                    IsErrors = true;
                else //обробатываем полученую информацию
                {
                    IsErrors = false;

                    if (stdout != "")
                    {
                        //int m0 = GetIntInfo(stdout, @"(\d+)\Dsections\Dwith\D0\Dframes\Dmoving");
                        //int m1 = GetIntInfo(stdout, @"(\d+)\Dsections\Dwith\D1\Dframes\Dmoving");
                        //int m2 = GetIntInfo(stdout, @"(\d+)\Dsections\Dwith\D2\Dframes\Dmoving");
                        //int m3 = GetIntInfo(stdout, @"(\d+)\Dsections\Dwith\D3\Dframes\Dmoving");
                        //int m4 = GetIntInfo(stdout, @"(\d+)\Dsections\Dwith\D4\Dframes\Dmoving");
                        //int m5 = GetIntInfo(stdout, @"(\d+)\Dsections\Dwith\D5\Dframes\Dmoving");

                        string type = GetStringInfo(stdout, @"determined\Dto\Dbe\D(\D+).");
                        if (type == "decimating")
                            m.interlace = SourceType.DECIMATING;
                        else if (type == "progressive")
                            m.interlace = SourceType.PROGRESSIVE;
                        else if (type == "partly film")
                            m.interlace = SourceType.HYBRID_FILM_INTERLACED;
                        else if (type == "interlaced")
                            m.interlace = SourceType.INTERLACED;
                        else if (type == "partly interlaced")
                            m.interlace = SourceType.HYBRID_PROGRESSIVE_INTERLACED;
                        else
                        {
                            //int unknown = GetIntInfo(stdout, @"`unknown':\D(\d+)");
                            int progressive = GetIntInfo(stdout, @"`progressive':\D(\d+)");
                            int interlaced = GetIntInfo(stdout, @"`interlaced':\D(\d+)");
                            int film = GetIntInfo(stdout, @"`film':\D(\d+)");
                            
                            //гадание
                            if (progressive > interlaced &&
                                progressive > film)
                                m.interlace = SourceType.PROGRESSIVE;
                            else if (interlaced > progressive &&
                                interlaced > film)
                                m.interlace = SourceType.INTERLACED;
                            else if (film > interlaced &&
                                film > progressive)
                                m.interlace = SourceType.FILM;
                        }

                        //int bff = GetIntInfo(stdout, @"`bff':\D(\d+)");
                        //int tff = GetIntInfo(stdout, @"`tff':\D(\d+)");
                        //int funknown = 0;
                        //if (stdout.Length > 56)
                        //    funknown = GetIntInfo(stdout.Substring(56, stdout.Length - 56), @"`unknown':\D(\d+)");

                        string field = GetStringInfo(stdout, @"Field\Dorder\Dis\D(\D+).");
                        if (field == "variable") m.fieldOrder = FieldOrder.VARIABLE;
                        else if (field == "tff") m.fieldOrder = FieldOrder.TFF;
                        else if (field == "bff") m.fieldOrder = FieldOrder.BFF;

                        //MessageBox.Show(stdout + Environment.NewLine + Environment.NewLine +
                        //    m.interlace.ToString() + Environment.NewLine +
                        //    m.fieldOrder.ToString());

                        //"BAutoDeint [info]: Number of sections of type `unknown': 1\r\n
                        //BAutoDeint [info]: Number of sections of type `progressive': 49\r\
                        //nBAutoDeint [info]: There are 0 sections with 0 frames moving.\r\n
                        //BAutoDeint [info]: There are 0 sections with 1 frames moving.\r\n
                        //BAutoDeint [info]: There are 0 sections with 2 frames moving.\r\n
                        //BAutoDeint [info]: There are 0 sections with 3 frames moving.\r\n
                        //BAutoDeint [info]: There are 1 sections with 4 frames moving.\r\n
                        //BAutoDeint [info]: There are 49 sections with 5 frames moving.\r\n\r\n
                        //Processing completed. Type is determined to be progressive.\r\n"

                        ////правим возможные погрешности
                        //if (m.inframerate == "25.000")
                        //{
                        //    if (m.interlace == SourceType.FILM ||
                        //        m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                        //        m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM)
                        //        m.interlace = SourceType.INTERLACED;
                        //}

                        m = Format.GetOutInterlace(m);
                        m = Calculate.UpdateOutFrames(m);
                    }

                    //удаляем мусор
                    SafeDelete(Settings.TempPath + "\\interlace.avs");
                    SafeDelete(Settings.TempPath + "\\interlace.avs.bautodeint_temp.avs");
                    SafeDelete(Settings.TempPath + "\\interlace.avs.bautodeint_temp.data");
                    SafeDelete(Settings.TempPath + "\\interlace.avs.bautodeint_temp.data.fieldorder");
                }
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
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

        private string GetStringInfo(string stdout, string pattern)
        {
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            mat = r.Match(stdout);
            if (mat.Success)
            {
                string x = mat.Groups[1].Value;
                if (x.Contains("."))
                {
                    string[] separator = new string[] { "." };
                    string[] a = x.Split(separator, StringSplitOptions.None);
                    return a[0];
                }
                else
                    return x;
            }
            else
                return null;
        }

        private int GetIntInfo(string stdout, string pattern)
        {
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match mat;
            mat = r.Match(stdout);
            if (mat.Success)
                return Convert.ToInt32(mat.Groups[1].Value);
            else
                return 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (encoderProcess != null)
            {
                IsAborted = true;
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
                e.Cancel = true;
            }
        }

        internal delegate void FieldPhaseDelegate();
        private void SetFieldPhase()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FieldPhaseDelegate(SetFieldPhase));
            else
                label_info.Content = Languages.Translate("Detecting fields order") + "...";
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
	}
}