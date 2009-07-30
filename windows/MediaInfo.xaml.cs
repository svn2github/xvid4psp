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
        public enum InfoMode { MediaInfo = 1, MP4BOX, FFMPEG, MKVINFO };

        private InfoMode infomode;
        private string infilepath;

		public MediaInfo(string infilepath, InfoMode infomode, System.Windows.Window owner)
		{
			this.InitializeComponent();
            this.Owner = owner;
            this.infomode = infomode;
            this.infilepath = infilepath;

            GetInfo();

            button_open.Content = Languages.Translate("Open");
            button_close.Content = Languages.Translate("Close");

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

            if (infomode == InfoMode.MP4BOX)
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

            if (infomode == InfoMode.MKVINFO)
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

                tbxInfo.Text = encoderProcess.StandardOutput.ReadToEnd();
                tbxInfo.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }

            if (infomode == InfoMode.FFMPEG)
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
                    if (!line.StartsWith("FFmpeg version") &&
                        !line.StartsWith("  configuration:") &&
                        !line.StartsWith("  libavutil version:") &&
                        !line.StartsWith("  libavcodec version:") &&
                        !line.StartsWith("  libavformat version:") &&
                        !line.StartsWith("  libavdevice version:") &&
                        !line.StartsWith("  built on") &&
                        !line.StartsWith("Must supply at least") &&
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

        private void grid_main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
        }

        private void grid_main_Drop(object sender, DragEventArgs e)
        {
            foreach (string dropfile in (string[])e.Data.GetData(DataFormats.FileDrop))
            {

            }
        }

	}
}