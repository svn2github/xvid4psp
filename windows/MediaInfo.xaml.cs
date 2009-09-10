using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace XviD4PSP
{
	public partial class MediaInfo
	{
        public enum InfoMode { MediaInfo = 1, MP4BoxInfo, MKVInfo, FFMPEGInfo };

        private InfoMode infomode;
        private string infilepath;

        public MediaInfo(string infilepath, InfoMode infomode, System.Windows.Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.infomode = infomode;
            this.infilepath = infilepath;

            button_open.Content = Languages.Translate("Open");
            button_close.Content = Languages.Translate("Close");
            button_save.Content = Languages.Translate("Save");

            combo_info.Items.Add("MediaInfo");
            combo_info.Items.Add("MP4BoxInfo");
            combo_info.Items.Add("MKVInfo");
            combo_info.Items.Add("FFMPEGInfo");
            combo_info.SelectedItem = infomode.ToString();

            Title = "Info (" + infomode.ToString() +")";
            
            if (infilepath != null)
                GetInfo();

            Show();
        }

        private void button_open_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ArrayList files = OpenDialogs.GetFilesFromConsole("ov");
            if (files.Count > 0)
                infilepath = files[0].ToString();

            if (infilepath != null)
                GetInfo();
        }

        private void button_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void GetInfo()
        {
            try
            {
                if (infomode == InfoMode.MediaInfo)
                {
                    MediaInfoWrapper media = new MediaInfoWrapper();

                    string ToDisplay = "";

                    media.Open(infilepath);

                    //ToDisplay += media.Option("Info_Parameters");//список ключей

                    media.Option("Complete", "1");//полная инфа
                    //media.Option("Complete");//краткая инфа      
                    ToDisplay += media.Inform();

                    //прицелная инфа
                    //ToDisplay += "VideoBitrate: " + media.VideoBitrate + " kbps" + Environment.NewLine;
                    //ToDisplay += "AudioBitrate: " + media.AudioBitrate(0) + " kbps" + Environment.NewLine;
                    //ToDisplay += "AudioLanguage: " + media.AudioLanguage(0) + Environment.NewLine;

                    tbxInfo.Text = ToDisplay;
                    tbxInfo.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                    media.Close();
                }

                if (infomode == InfoMode.MP4BoxInfo)
                {
                    Process encoderProcess = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\MP4Box.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
                    info.CreateNoWindow = true;

                    info.Arguments = "-info \"" + infilepath + "\"";

                    encoderProcess.StartInfo = info;
                    encoderProcess.Start();

                    encoderProcess.WaitForExit();

                    tbxInfo.Text = encoderProcess.StandardOutput.ReadToEnd();
                    tbxInfo.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                }

                if (infomode == InfoMode.MKVInfo)
                {
                    Process encoderProcess = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvinfo.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
                    info.CreateNoWindow = true;

                    info.Arguments = "\"" + infilepath + "\"";

                    encoderProcess.StartInfo = info;
                    encoderProcess.Start();

                    encoderProcess.WaitForExit();

                    tbxInfo.Text = encoderProcess.StandardOutput.ReadToEnd().Replace("\r", "");
                    tbxInfo.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                }

                if (infomode == InfoMode.FFMPEGInfo)
                {
                    Process encoderProcess = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    info.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;
                    info.CreateNoWindow = true;

                    info.Arguments = "-i \"" + infilepath + "\"";

                    encoderProcess.StartInfo = info;
                    encoderProcess.Start();

                    int wseconds = 0;
                    while (wseconds < 50 &&
                        !encoderProcess.HasExited) //ждём не более пяти секунд
                    {
                        Thread.Sleep(100);
                        wseconds++;
                    }

                    string _info = encoderProcess.StandardError.ReadToEnd();
                    string sortedinfo = "";
                    string[] separator = new string[] { Environment.NewLine };
                    string[] lines = _info.Split(separator, StringSplitOptions.None);
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("fFFmpeg version") &&
                            !line.StartsWith("  configuration:") &&
                            !line.StartsWith("  libavutil") &&
                            !line.StartsWith("  libavcodec") &&
                            !line.StartsWith("  libavformat") &&
                            !line.StartsWith("  libavdevice") &&
                            !line.StartsWith("  libsws") &&
                            !line.StartsWith("  built on") &&
                            !line.StartsWith("At least one output") &&
                            line != "")
                            sortedinfo += line + Environment.NewLine;
                    }

                    tbxInfo.Text = sortedinfo;
                    tbxInfo.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                    FFInfo ff = new FFInfo();
                    ff.Open(infilepath);
                    tbxInfo.Text += Environment.NewLine;
                    tbxInfo.Text += "Streams: " + ff.StreamCount() + Environment.NewLine;
                    tbxInfo.Text += "AudioStream: " + ff.AudioStream() + Environment.NewLine;
                    tbxInfo.Text += "VideoStream: " + ff.VideoStream() + Environment.NewLine;
                    tbxInfo.Text += "Timeline: " + ff.Timeline() + Environment.NewLine;
                    tbxInfo.Text += "Seconds: " + ff.Duration().TotalSeconds + Environment.NewLine;
                    tbxInfo.Text += "W: " + ff.StreamW(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "H: " + ff.StreamH(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "DAR: " + ff.StreamDAR(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "PAR: " + ff.StreamPAR(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "Color: " + ff.StreamColor(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "Framerate: " + ff.StreamFramerate(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "TotalBitrate: " + ff.TotalBitrate() + Environment.NewLine;
                    tbxInfo.Text += "VideoBitrate: " + ff.VideoBitrate(ff.VideoStream()) + Environment.NewLine;
                    tbxInfo.Text += "AudioBitrate: " + ff.StreamBitrate(ff.AudioStream()) + Environment.NewLine;
                    tbxInfo.Text += "Type: " + ff.StreamType(ff.AudioStream()) + Environment.NewLine;
                    tbxInfo.Text += "Codec: " + ff.StreamCodec(ff.AudioStream()) + Environment.NewLine;
                    tbxInfo.Text += "Channels: " + ff.StreamChannels(ff.AudioStream()) + Environment.NewLine;
                    tbxInfo.Text += "Samplerate: " + ff.StreamSamplerate(ff.AudioStream()) + Environment.NewLine;
                    tbxInfo.Text += "Language: " + ff.StreamLanguage(ff.AudioStream()) + Environment.NewLine;
                    ff.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void grid_main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void grid_main_Drop(object sender, DragEventArgs e)
        {
            foreach (string dropfile in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                tbxInfo.Clear();
                tbxInfo.ScrollToHome();
                infilepath = dropfile;
                GetInfo();
                return;
            }
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (tbxInfo.Text != null)
            {
                try
                {
                    string logfilename = infilepath + " - " + infomode.ToString().ToLower() + ".log";
                    FileStream strm = new FileStream(logfilename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    StreamWriter writer = new StreamWriter(strm);

                    writer.WriteLine(tbxInfo.Text);
                    writer.Flush();
                    writer.Dispose();
                    strm.Dispose();
                }
                catch (Exception ex)
                {
                       ErrorExeption(ex.Message);
                }
            }
        }

        private void ErrorExeption(string message)
        {
            tbxInfo.Text = (Languages.Translate("Error") + ": " + Environment.NewLine + message);            
        }

        private void combo_info_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_info.IsDropDownOpen || combo_info.IsSelectionBoxHighlighted)
            {
                if (combo_info.SelectedItem.ToString() == "MediaInfo")
                    infomode = InfoMode.MediaInfo;
                else if (combo_info.SelectedItem.ToString() == "MP4BoxInfo")
                    infomode = InfoMode.MP4BoxInfo;
                else if (combo_info.SelectedItem.ToString() == "MKVInfo")
                    infomode = InfoMode.MKVInfo;
                else
                    infomode = InfoMode.FFMPEGInfo;
                
                Title = "Info (" + infomode.ToString() + ")";

                tbxInfo.Clear();
                tbxInfo.ScrollToHome();
                if (infilepath != null)
                    GetInfo();
            }
        }
	}
}