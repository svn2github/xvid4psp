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
        public enum InfoMode { MediaInfo = 1, MediaInfoFull, MP4BoxInfo, MKVInfo, FFmpegInfo };

        private static object lock_exe = new object();
        private static object lock_ff = new object();
        private Process encoderProcess = null;
        private FFInfo ff = null;
        private InfoMode infomode;
        private string infilepath;

        public MediaInfo(string infilepath, InfoMode infomode, System.Windows.Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.infomode = infomode;
            this.infilepath = infilepath;

            DDHelper ddh = new DDHelper(this);
            ddh.GotFiles += new DDEventHandler(DD_GotFiles);

            Title = "Info (" + infomode.ToString() + ")";
            button_open.Content = Languages.Translate("Open");
            button_save.Content = Languages.Translate("Save");
            button_close.Content = Languages.Translate("Close");
            tbxInfo.ToolTip = Languages.Translate("Drag and Drop your files here");
            check_wrap.Content = Languages.Translate("Wrap text");

            if (Settings.MI_WrapText)
            {
                check_wrap.IsChecked = true;
                tbxInfo.TextWrapping = TextWrapping.Wrap;
            }
            else
            {
                check_wrap.IsChecked = false;
                tbxInfo.TextWrapping = TextWrapping.NoWrap;
            }

            foreach (string info in Enum.GetNames(typeof(InfoMode)))
                combo_info.Items.Add(info);
            combo_info.SelectedItem = infomode.ToString();

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
                tbxInfo.ToolTip = null; //Чтоб не мешался

                if (infomode == InfoMode.MediaInfo)
                {
                    //краткая инфа
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(infilepath);
                    media.Option("Complete", "");
                    media.Option("Language", "  Config_Text_ColumnSize;" + Settings.MI_ColumnSize);
                    tbxInfo.Text = media.Inform();
                    media.Close();
                }
                else if (infomode == InfoMode.MediaInfoFull)
                {
                    //полная инфа
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(infilepath);
                    media.Option("Complete", "1");
                    media.Option("Language", "  Config_Text_ColumnSize;" + Settings.MI_ColumnSize);
                    tbxInfo.Text = media.Inform();
                    media.Close();
                }
                else if (infomode == InfoMode.MP4BoxInfo)
                {
                    encoderProcess = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    info.FileName = Calculate.StartupPath + "\\apps\\MP4Box\\MP4Box.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = false;
                    info.CreateNoWindow = true;

                    info.Arguments = "-info \"" + infilepath + "\"";

                    encoderProcess.StartInfo = info;
                    encoderProcess.Start();

                    tbxInfo.Text = encoderProcess.StandardOutput.ReadToEnd();
                }
                else if (infomode == InfoMode.MKVInfo)
                {
                    encoderProcess = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    info.FileName = Calculate.StartupPath + "\\apps\\MKVtoolnix\\mkvinfo.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.UseShellExecute = false;
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = false;
                    info.CreateNoWindow = true;

                    string charset = "";
                    string _charset = Settings.MKVToolnix_Charset;

                    try
                    {
                        if (_charset == "")
                        {
                            info.StandardOutputEncoding = System.Text.Encoding.UTF8;
                            charset = " --output-charset UTF-8";
                        }
                        else if (_charset.ToLower() == "auto")
                        {
                            info.StandardOutputEncoding = System.Text.Encoding.Default;
                            charset = " --output-charset " + System.Text.Encoding.Default.HeaderName;
                        }
                        else
                        {
                            int page = 0;
                            if (int.TryParse(_charset, out page))
                                info.StandardOutputEncoding = System.Text.Encoding.GetEncoding(page);
                            else
                                info.StandardOutputEncoding = System.Text.Encoding.GetEncoding(_charset);
                            charset = " --output-charset " + _charset;
                        }
                    }
                    catch (Exception) { }

                    info.Arguments = "\"" + infilepath + "\"" + charset;

                    encoderProcess.StartInfo = info;
                    encoderProcess.Start();

                    tbxInfo.Text = encoderProcess.StandardOutput.ReadToEnd().Replace("\r\r\n", "\r\n");
                }
                else if (infomode == InfoMode.FFmpegInfo)
                {
                    ff = new FFInfo();
                    ff.Open(infilepath);

                    if (ff.info.Length > 0)
                    {
                        string sortedinfo = "";
                        string[] lines = ff.info.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        foreach (string line in lines)
                        {
                            if (!line.StartsWith("  configuration:") &&
                                !line.StartsWith("  lib") &&
                                !line.StartsWith("  built on") &&
                                !line.StartsWith("At least one output") &&
                                !line.StartsWith("This program is not") &&
                                line != "")
                                sortedinfo += line + Environment.NewLine;
                        }

                        tbxInfo.Text = sortedinfo + "\r\n\r\n";
                    }
                    else
                        tbxInfo.Clear();

                    //Размер файла
                    string size_s = "";
                    double size = (new FileInfo(infilepath).Length) / 1048576.0;
                    if (size > 1024) size_s = (size / 1024.0).ToString("0.##", new System.Globalization.CultureInfo("en-US")) + " Gb\r\n";
                    else size_s = size.ToString("0.##", new System.Globalization.CultureInfo("en-US")) + " Mb\r\n";

                    //Общая инфа
                    tbxInfo.Text += "General:\r\n";
                    tbxInfo.Text += "Total streams       : " + ff.StreamsCount() + " (Video: " + ff.VideoStreams().Count + ", Audio: " + ff.AudioStreams().Count + ")\r\n";
                    tbxInfo.Text += "Total duration      : " + ff.Timeline() + " (" + ff.Duration().TotalSeconds.ToString("0.##", new System.Globalization.CultureInfo("en-US")) + " seconds)\r\n";
                    tbxInfo.Text += "Total bitrate       : " + ff.TotalBitrate() + " Kbps\r\n";
                    tbxInfo.Text += "Total size          : " + size_s;

                    //Видео треки
                    int v_count = ff.VideoStreams().Count, v_num = 1;
                    foreach (int num in ff.VideoStreams())
                    {
                        tbxInfo.Text += "\r\nVideo stream #" + v_num + ":\r\n";
                        tbxInfo.Text += "ID                  : 0." + num + Environment.NewLine;
                        tbxInfo.Text += "Codec               : " + ff.StreamCodec(num) + Environment.NewLine;
                        tbxInfo.Text += "Width               : " + ff.StreamW(num) + " pixels\r\n";
                        tbxInfo.Text += "Height              : " + ff.StreamH(num) + " pixels\r\n";
                        tbxInfo.Text += "Aspect (DAR)        : " + ff.StreamDAR(num) + Environment.NewLine;
                        tbxInfo.Text += "Aspect (PAR)        : " + ff.StreamPAR(num) + Environment.NewLine;
                        tbxInfo.Text += "Colorspace          : " + ff.StreamColor(num) + Environment.NewLine;
                        tbxInfo.Text += "Framerate           : " + ff.StreamFramerate(num) + " fps\r\n";
                        tbxInfo.Text += "Bitrate             : " + ff.VideoBitrate(num) + " Kbps\r\n";
                        v_num += 1;
                    }

                    //Аудио треки
                    int a_count = ff.AudioStreams().Count, a_num = 1;
                    foreach (int num in ff.AudioStreams())
                    {
                        tbxInfo.Text += "\r\nAudio stream #" + a_num + ":\r\n";
                        tbxInfo.Text += "ID                  : 0." + num + Environment.NewLine;
                        tbxInfo.Text += "Codec               : " + ff.StreamCodec(num) + Environment.NewLine;
                        tbxInfo.Text += "Channels            : " + ff.StreamChannels(num) + Environment.NewLine;
                        tbxInfo.Text += "Samplerate          : " + ff.StreamSamplerate(num) + " Hz\r\n";
                        tbxInfo.Text += "Language            : " + ff.StreamLanguage(num) + Environment.NewLine;
                        tbxInfo.Text += "Bitrate             : " + ff.StreamBitrate(num) + " Kbps\r\n";
                        tbxInfo.Text += "Bits                : " + ff.StreamBits(num) + Environment.NewLine;
                        a_num += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex);
            }
            finally
            {
                CloseEXE();
                CloseFFInfo();
            }
        }

        private void DD_GotFiles(object sender, string[] files)
        {
            tbxInfo.Clear();
            tbxInfo.ScrollToHome();
            infilepath = files[0];
            GetInfo();
            Activate();
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (infilepath != null)
            {
                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.SupportMultiDottedExtensions = true;
                    s.DefaultExt = ".log";
                    s.AddExtension = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    s.Filter = "LOG " + Languages.Translate("files") + "|*.log" +
                        "|TXT " + Languages.Translate("files") + "|*.txt";

                    s.InitialDirectory = Path.GetDirectoryName(infilepath);
                    s.FileName = Path.GetFileName(infilepath) + " - " + infomode.ToString(); //.ToLower();

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.WriteAllText(s.FileName, tbxInfo.Text); //, System.Text.Encoding.Default);
                    }
                }
                catch (Exception ex)
                {
                    ErrorException(ex);
                }
            }
        }

        private void ErrorException(Exception ex)
        {
            tbxInfo.Text = (Languages.Translate("Error") + ":\r\n   " + ex.Message + "\r\n\r\nStackTrace:\r\n" + ex.StackTrace);
        }

        private void combo_info_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_info.IsDropDownOpen || combo_info.IsSelectionBoxHighlighted) && combo_info.SelectedItem != null)
            {
                infomode = (InfoMode)Enum.Parse(typeof(InfoMode), combo_info.SelectedItem.ToString(), true);
                Title = "Info (" + infomode.ToString() + ")";

                if (infilepath != null)
                {
                    tbxInfo.Clear();
                    tbxInfo.ScrollToHome();
                    GetInfo();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseEXE();
            CloseFFInfo();
        }

        private void CloseEXE()
        {
            lock (lock_exe)
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

        private void CloseFFInfo()
        {
            lock (lock_ff)
            {
                if (ff != null)
                {
                    ff.Close();
                    ff = null;
                }
            }
        }

        private void check_wrap_Click(object sender, RoutedEventArgs e)
        {
            if ((Settings.MI_WrapText = check_wrap.IsChecked.Value))
            {
                tbxInfo.TextWrapping = TextWrapping.Wrap;
            }
            else
                tbxInfo.TextWrapping = TextWrapping.NoWrap;
        }
	}
}