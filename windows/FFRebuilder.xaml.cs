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
    public partial class FFRebuilder
    {
        private BackgroundWorker worker = null;
        private ManualResetEvent locker = new ManualResetEvent(true);
        private Process encoderProcess = null;
        FFInfo ff;

        private enum acodecs { COPY, PCM, DISABLED }
        private enum vcodecs { COPY, FFV1, FFVHUFF, DISABLED }
        private enum formats { AUTO, AVI, MP4, MKV, MPG, h264, M4V, MP3, MP2, AC3, WAV }

        private string infile;
        private string outfile;
        private vcodecs vcodec = vcodecs.COPY;
        private acodecs acodec = acodecs.COPY;
        private formats format = formats.AUTO;
        private string framerate = "AUTO";

        public FFRebuilder(System.Windows.Window owner)
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
            group_options.Header = Languages.Translate("Options");
            group_files.Header = Languages.Translate("Files");
            group_info.Header = Languages.Translate("Info");
            label_acodec.Content = Languages.Translate("Audio codec") + ":";
            label_vcodec.Content = Languages.Translate("Video codec") + ":";
            label_framerate.Content = Languages.Translate("Framerate") + ":";
            label_format.Content = Languages.Translate("Format") + ":";
            button_play.Content = Languages.Translate("Play");
            progress.Maximum = 100;

            foreach (string codec in Enum.GetNames(typeof(vcodecs)))
                combo_vcodec.Items.Add(codec);
            combo_vcodec.SelectedItem = vcodecs.COPY.ToString();

            foreach (string codec in Enum.GetNames(typeof(acodecs)))
                combo_acodec.Items.Add(codec);
            combo_acodec.SelectedItem = acodecs.COPY.ToString();

            foreach (string format in Enum.GetNames(typeof(formats)))
                combo_format.Items.Add(format);
            combo_format.SelectedItem = formats.AUTO.ToString();

            combo_framerate.Items.Add("AUTO");
            combo_framerate.Items.Add("15.000");
            combo_framerate.Items.Add("18.000");
            combo_framerate.Items.Add("20.000");
            combo_framerate.Items.Add("23.976");
            combo_framerate.Items.Add("23.980");
            combo_framerate.Items.Add("24.000");
            combo_framerate.Items.Add("25.000");
            combo_framerate.Items.Add("29.970");
            combo_framerate.Items.Add("30.000");
            combo_framerate.SelectedItem = "AUTO";

            button_play.Visibility = Visibility.Hidden;

            Show();
        }

        private void SetFFInfo(string filepath)
        {
            Process encoderProcess = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            info.Arguments = "-i \"" + filepath + "\"";

            encoderProcess.StartInfo = info;
            encoderProcess.Start();

            encoderProcess.WaitForExit();

            string _info = encoderProcess.StandardError.ReadToEnd();
            string sortedinfo = "";
            string[] separator = new string[] { Environment.NewLine };
            string[] lines = _info.Split(separator, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (!line.StartsWith("  configuration:") &&
                    !line.StartsWith("  libavutil") &&
                    !line.StartsWith("  libavcodec") &&
                    !line.StartsWith("  libavformat") &&
                    !line.StartsWith("  libavdevice") &&
                    !line.StartsWith("  libswscale") &&
                    !line.StartsWith("  built on") &&
                    !line.StartsWith("At least one output file") &&
                    line != "")
                    sortedinfo += line + Environment.NewLine;
            }

            text_info.Text = sortedinfo;
            text_info.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
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
            Title = "(" + e.ProgressPercentage + "%)";
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //удаляем старый файл
                SafeDelete(outfile);

                //получаем колличество секунд
                ff = new FFInfo();
                ff.Open(infile);
                int seconds = (int)ff.Duration().TotalSeconds;
                ff.Close();

                encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;

                string _format = "";
                if (format == formats.MPG)
                    _format = " -f vob";

                string _vcodec = "-vcodec copy";
                if (vcodec != vcodecs.COPY)
                    _vcodec = "-vcodec " + vcodec.ToString().ToLower();

                string yv12 = "";
                if (vcodec == vcodecs.FFV1 ||
                    vcodec == vcodecs.FFVHUFF)
                    yv12 = " -pix_fmt yuv420p";

                string _framerate = "";
                if (framerate != "AUTO")
                    _framerate = " -r " + framerate;

                string _acodec = "-acodec copy";
                if (acodec == acodecs.PCM)
                    _acodec = "-acodec pcm_s16le";

                if (vcodec == vcodecs.DISABLED)
                    _vcodec = "-vn";
                if (acodec == acodecs.DISABLED)
                    _acodec = "-an";

                info.Arguments = "-i \"" + infile +
                    "\" " + _vcodec + " " + _acodec + _format + yv12 + _framerate + " \"" + outfile + "\"";

                SetLog(info.Arguments);

                encoderProcess.StartInfo = info;
                encoderProcess.Start();


                string line;
                string pat = @"time=(\d+.\d+)";
                Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                Match mat;

                while (!encoderProcess.HasExited)
                {
                    locker.WaitOne();
                    line = encoderProcess.StandardError.ReadLine();

                    if (line != null)
                    {
                        mat = r.Match(line);
                        if (mat.Success == true)
                        {
                            double ctime = Calculate.ConvertStringToDouble(mat.Groups[1].Value);
                            double pr = ((double)ctime / (double)seconds) * 100.0;
                            worker.ReportProgress((int)pr);
                        }
                        else
                        {
                            SetLog(line);
                        }
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
            //проверка на удачное завершение
            if (File.Exists(outfile))
            {
                FileInfo outinfo = new FileInfo(outfile);
                if (outinfo.Length > 20000)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Path.GetFileName(outfile) + " " + Languages.Translate("ready") + "!", Languages.Translate("Complete") + "!");
                    button_play.Visibility = Visibility.Visible;
                }
            }

            try
            {
                FileInfo finfo = new FileInfo(outfile);
                SetLog(Languages.Translate("Out file size is:") + " " +
                    Calculate.ConvertDoubleToPointString((double)finfo.Length / 1024.0 / 1024.0, 2) + " mb");
            }
            catch (Exception ex)
            {
                SetLog(ex.Message);
            }

            button_start.Content = Languages.Translate("Start");
            Title = "FFRebuilder";
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
            if (ff != null)
                ff.Close();

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
                    vcodec = (vcodecs)Enum.Parse(typeof(vcodecs), this.combo_vcodec.SelectedItem.ToString());
                    acodec = (acodecs)Enum.Parse(typeof(acodecs), this.combo_acodec.SelectedItem.ToString());
                    format = (formats)Enum.Parse(typeof(formats), this.combo_format.SelectedItem.ToString());
                    framerate = combo_framerate.SelectedItem.ToString();

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
                ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
                if (files.Count > 0)
                {
                    textbox_infile.Text = files[0].ToString();
                    formats _format = (formats)Enum.Parse(typeof(formats), this.combo_format.SelectedItem.ToString());

                    string opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + Path.GetExtension(textbox_infile.Text);

                    if (_format != formats.AUTO)
                        opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed." + _format.ToString().ToLower();


                    textbox_outfile.Text = opath;

                    SetFFInfo(textbox_infile.Text);
                }
            }
        }

        private void combo_format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_format.IsDropDownOpen || combo_format.IsSelectionBoxHighlighted)
            {
                if (textbox_outfile.Text != "")
                {
                    formats _format = (formats)Enum.Parse(typeof(formats), this.combo_format.SelectedItem.ToString());

                    string opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + Path.GetExtension(textbox_infile.Text);

                    if (_format != formats.AUTO)
                        opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed." + _format.ToString().ToLower();

                    if (textbox_outfile.Text == "")
                        textbox_outfile.Text = opath;
                    else
                        textbox_outfile.Text = Path.GetDirectoryName(textbox_outfile.Text) + "\\" + Path.GetFileName(opath);
                }
            }
        }

        private void combo_vcodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_vcodec.IsDropDownOpen || combo_vcodec.IsSelectionBoxHighlighted)
            {
                vcodecs _vcodec = (vcodecs)Enum.Parse(typeof(vcodecs), this.combo_vcodec.SelectedItem.ToString());
                formats _format = (formats)Enum.Parse(typeof(formats), this.combo_format.SelectedItem.ToString());

                //if (_vcodec == vcodecs.FFV1 ||
                //    _vcodec == vcodecs.FFVHUFF)
                //{
                //    combo_format.SelectedItem = formats.AVI.ToString();
                //    _format = formats.AVI;
                //    combo_acodec.SelectedItem = acodecs.PCM.ToString();
                //}
                //else if (_vcodec == vcodecs.DISABLED)
                //{
                //    combo_format.SelectedItem = formats.AVI.ToString();
                //    _format = formats.;
                //}

                string opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + Path.GetExtension(textbox_infile.Text);

                if (_format != formats.AUTO)
                    opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed." + _format.ToString().ToLower();

                textbox_outfile.Text = opath;
            }
        }

        private void combo_acodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_acodec.IsDropDownOpen || combo_acodec.IsSelectionBoxHighlighted)
            {
                acodecs _acodec = (acodecs)Enum.Parse(typeof(acodecs), this.combo_acodec.SelectedItem.ToString());
                formats _format = (formats)Enum.Parse(typeof(formats), this.combo_format.SelectedItem.ToString());

                //if (_acodec != acodecs.COPY)
                //{
                //    combo_format.SelectedItem = formats.AVI.ToString();
                //    _format = formats.AVI;
                //    combo_vcodec.SelectedItem = vcodecs.FFVHUFF.ToString();
                //}

                string opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + Path.GetExtension(textbox_infile.Text);

                if (_format != formats.AUTO)
                    opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed." + _format.ToString().ToLower();

                textbox_outfile.Text = opath;
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
                s.FileName = textbox_outfile.Text;
                s.Filter = Languages.Translate("All files") + "|*.*";

                if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textbox_outfile.Text = s.FileName;
                }
            }
        }

        private void button_play_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_outfile.Text != "" &&
                File.Exists(textbox_outfile.Text))
            {
                Process.Start(textbox_outfile.Text);
            }
        }

        private void LayoutRoot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void LayoutRoot_Drop(object sender, DragEventArgs e)
        {
            foreach (string dropfile in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                textbox_infile.Text = dropfile.ToString();
                formats _format = (formats)Enum.Parse(typeof(formats), this.combo_format.SelectedItem.ToString());
                string opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed" + Path.GetExtension(textbox_infile.Text);
                if (_format != formats.AUTO)
                    opath = Calculate.RemoveExtention(textbox_infile.Text, true) + ".remuxed." + _format.ToString().ToLower();
                textbox_outfile.Text = opath;
                SetFFInfo(textbox_infile.Text);
                return;
            }
        }
    }
}