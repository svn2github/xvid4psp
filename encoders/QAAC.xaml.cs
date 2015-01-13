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

namespace XviD4PSP
{
    public partial class QuickTimeAAC
    {
        public Massive m;
        private AudioEncoding root_window;
        private string str_bitrate = Languages.Translate("Bitrate") + ":";
        private string str_quality = Languages.Translate("Quality") + ":";

        public QuickTimeAAC(Massive mass, AudioEncoding AudioEncWindow)
        {
            this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = AudioEncWindow;

            //--check    Show library versions and exit
            //--formats  Show available AAC formats and exit

            combo_mode.Items.Add("CBR");             //-c, --cbr <bitrate>   AAC CBR mode / bitrate
            combo_mode.Items.Add("ABR");             //-a, --abr <bitrate>   AAC ABR mode / bitrate
            combo_mode.Items.Add("Constrained VBR"); //-v, --cvbr <bitrate>  AAC Constrained VBR mode / bitrate
            combo_mode.Items.Add("True VBR");        //-V, --tvbr <quality>  AAC True VBR mode / quality [0-127] (AAC-HE не поддерживается)
            combo_mode.Items.Add("Lossless (ALAC)"); //-A, --alac            ALAC encoding mode

            combo_accuracy.Items.Add("0 - Fast");
            combo_accuracy.Items.Add("1");
            combo_accuracy.Items.Add("2 - Slow");

            combo_aac_profile.Items.Add("AAC-LC");
            combo_aac_profile.Items.Add("AAC-HE");

            combo_gapless_mode.Items.Add("iTunSMPB");
            combo_gapless_mode.Items.Add("ISO");
            combo_gapless_mode.Items.Add("iTunSMPB + ISO");

            //Предупреждение о неточности битрейта
            combo_bitrate.Tag = Languages.Translate("Do not expect that selected bitrate will be strictly observed by the encoder!") + "\r\n" +
                Languages.Translate("The actual value will varies with encoding mode (ABR/CBR/CVBR), profile (LC/HE), sample rate and number of channels.") + "\r\n" +
                Languages.Translate("Click on \"Bitrate\" label to get detailed information about all supported combinations.");

            text_mode.Content = Languages.Translate("Encoding mode") + ":";
            text_accuracy.Content = Languages.Translate("Accuracy") + ":";
            text_gapless_mode.Content = Languages.Translate("Delay signaling") + ":";
            check_no_delay.Content = Languages.Translate("Compensate encoder delay");

            LoadFromProfile();
        }

        public void LoadFromProfile()
        {
            //забиваем режимы кодирования
            string mode = m.qaac_options.encodingmode.ToString();
            if (mode == "CBR") combo_mode.SelectedItem = "CBR";
            else if (mode == "ABR") combo_mode.SelectedItem = "ABR";
            else if (mode == "CVBR") combo_mode.SelectedItem = "Constrained VBR";
            else if (mode == "ALAC") combo_mode.SelectedItem = "Lossless (ALAC)";
            else combo_mode.SelectedItem = "True VBR";

            //прогружаем битрейты
            LoadBitrates();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                combo_bitrate.SelectedIndex = m.qaac_options.quality;
            else if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.ALAC)
                combo_bitrate.SelectedIndex = combo_bitrate.Items.Count - 1;
            else
                combo_bitrate.SelectedItem = outstream.bitrate;

            combo_accuracy.SelectedIndex = m.qaac_options.accuracy;
            combo_aac_profile.SelectedItem = m.qaac_options.aacprofile;
            combo_gapless_mode.SelectedIndex = m.qaac_options.gapless_mode;
            check_no_delay.IsChecked = m.qaac_options.no_delay;
        }

        private void LoadBitrates()
        {
            try
            {
                combo_bitrate.Items.Clear();
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.VBR) //True VBR
                {
                    //Задано энкодеру vs. использовано энкодером (проверено с CoreAudioToolbox 7.9.7.9)
                    //0 4 5 10 13 15 20 25 30 35 40 45 50 55 60 65 70 75 80 85 90 95 100 105 110 115 120 125 127
                    //0 0 9  9  9 18 18 27 27 36 36 45 54 54 63 63-73 73 82 82 91 91 100 109 109 118 118 127 127
                    //Т.е. фактически используются только следующие значения:
                    //0 9 18 27 36 45 54 63-73 82 91 100 109 118 127 (0-63, 63-73, 73-127)
                    //Всего 15-ть, но когда-то давно их было вообще лишь 11-ть.

                    for (int i = 0; i < 15; i++)
                        combo_bitrate.Items.Add(i);

                    //Битрейт для VBR
                    outstream.bitrate = 0;
                }
                else
                {
                    for (int i = 8; i <= Format.GetMaxAACBitrate(m); i += 4)
                        combo_bitrate.Items.Add(i);

                    if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.ALAC)
                    {
                        //Это тоже VBR
                        outstream.bitrate = 0;
                    }
                    else if (!combo_bitrate.Items.Contains(outstream.bitrate) || outstream.bitrate == 0)
                    {
                        //Битрейт по умолчанию
                        outstream.bitrate = 128;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static Massive DecodeLine(Massive m)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //создаём свежий массив параметров QuickTime AAC
            m.qaac_options = new qaac_arguments();

            //берём пока что за основу последнюю строку
            string line = outstream.passes;

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-q")
                {
                    m.qaac_options.accuracy = Convert.ToInt32(cli[n + 1]);
                }
                else if (value == "--tvbr")
                {
                    m.qaac_options.encodingmode = Settings.AudioEncodingModes.VBR;
                    m.qaac_options.quality = Convert.ToInt32(cli[n + 1]) / 9;
                }
                else if (value == "--cbr" || value == "--abr" || value == "--cvbr")
                {
                    if (value == "--cbr") m.qaac_options.encodingmode = Settings.AudioEncodingModes.CBR;
                    else if (value == "--abr") m.qaac_options.encodingmode = Settings.AudioEncodingModes.ABR;
                    else m.qaac_options.encodingmode = Settings.AudioEncodingModes.CVBR;

                    outstream.bitrate = Convert.ToInt32(cli[n + 1]);
                }
                else if (value == "--alac")
                {
                    m.qaac_options.encodingmode = Settings.AudioEncodingModes.ALAC;
                }
                else if (value == "--he")
                {
                    m.qaac_options.aacprofile = "AAC-HE";
                }
                else if (value == "--no-delay")
                {
                    m.qaac_options.no_delay = true;
                }
                else if (value == "--gapless-mode")
                {
                    m.qaac_options.gapless_mode = Convert.ToInt32(cli[n + 1]);
                }

                n++;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.ALAC)
            {
                //Lossless
                line += "--alac";
            }
            else
            {
                //Точность кодирования
                line += "-q " + m.qaac_options.accuracy;

                //Битрейт\Качество
                if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.ABR)
                    line += " --abr " + outstream.bitrate;
                else if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.CBR)
                    line += " --cbr " + outstream.bitrate;
                else if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.CVBR)
                    line += " --cvbr " + outstream.bitrate;
                else //if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                {
                    int value = (m.qaac_options.quality * 9);
                    line += " --tvbr " + ((value > 63) ? value + 1 : value);
                }

                //AAC-HE
                if (m.qaac_options.aacprofile == "AAC-HE") line += " --he";
            }

            if (m.qaac_options.gapless_mode != 0)
                line += " --gapless-mode " + m.qaac_options.gapless_mode;

            if (m.qaac_options.no_delay)
                line += " --no-delay";

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted) && combo_mode.SelectedItem != null)
            {
                string mode = combo_mode.SelectedItem.ToString();
                if (mode == "CBR") m.qaac_options.encodingmode = Settings.AudioEncodingModes.CBR;
                else if (mode == "ABR") m.qaac_options.encodingmode = Settings.AudioEncodingModes.ABR;
                else if (mode == "Constrained VBR") m.qaac_options.encodingmode = Settings.AudioEncodingModes.CVBR;
                else if (mode == "Lossless (ALAC)") m.qaac_options.encodingmode = Settings.AudioEncodingModes.ALAC;
                else
                {
                    m.qaac_options.encodingmode = Settings.AudioEncodingModes.VBR;
                    if (combo_aac_profile.SelectedIndex == 1)
                    {
                        //AAC-HE запрещен при True VBR
                        combo_aac_profile.SelectedIndex = 0;
                        m.qaac_options.aacprofile = "AAC-LC";
                    }
                }

                LoadBitrates();

                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    combo_bitrate.SelectedIndex = m.qaac_options.quality;
                else if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.ALAC)
                    combo_bitrate.SelectedIndex = combo_bitrate.Items.Count - 1;
                else
                    combo_bitrate.SelectedItem = outstream.bitrate;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }

            if (combo_mode.SelectedIndex != -1)
            {
                if (combo_mode.SelectedIndex < 3)
                {
                    combo_bitrate.ToolTip = combo_bitrate.Tag;
                    combo_aac_profile.IsEnabled = true;
                    text_bitrate.Content = str_bitrate + " (?)";
                }
                else
                {
                    combo_bitrate.ToolTip = null;
                    combo_aac_profile.IsEnabled = false;
                    text_bitrate.Content = (combo_mode.SelectedIndex == 3) ? str_quality : str_bitrate;
                }

                if (combo_aac_profile.SelectedIndex == 1 ||  //AAC-HE
                    combo_mode.SelectedIndex == 4)           //ALAC
                {
                    m.qaac_options.no_delay = false;
                    check_no_delay.IsChecked = false;
                    check_no_delay.IsEnabled = false;
                }
                else
                {
                    check_no_delay.IsEnabled = true;
                }

                combo_bitrate.IsEnabled = combo_accuracy.IsEnabled = (combo_mode.SelectedIndex != 4);
            }
        }

        private void combo_bitrate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_bitrate.IsDropDownOpen || combo_bitrate.IsSelectionBoxHighlighted) && combo_bitrate.SelectedItem != null)
            {
                if (m.qaac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                {
                    m.qaac_options.quality = combo_bitrate.SelectedIndex;
                }
                else
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    outstream.bitrate = Convert.ToInt32(combo_bitrate.SelectedItem);
                }

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_accuracy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_accuracy.IsDropDownOpen || combo_accuracy.IsSelectionBoxHighlighted) && combo_accuracy.SelectedIndex != -1)
            {
                m.qaac_options.accuracy = combo_accuracy.SelectedIndex;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_aac_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_aac_profile.IsDropDownOpen || combo_aac_profile.IsSelectionBoxHighlighted) && combo_aac_profile.SelectedItem != null)
            {
                m.qaac_options.aacprofile = combo_aac_profile.SelectedItem.ToString();

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }

            if (combo_aac_profile.SelectedIndex != -1)
            {
                if (combo_aac_profile.SelectedIndex == 1 ||  //AAC-HE
                    combo_mode.SelectedIndex == 4)           //ALAC
                {
                    m.qaac_options.no_delay = false;
                    check_no_delay.IsChecked = false;
                    check_no_delay.IsEnabled = false;
                }
                else
                {
                    check_no_delay.IsEnabled = true;
                }
            }
        }

        private void text_bitrate_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (combo_mode.SelectedIndex >= 0 && combo_mode.SelectedIndex < 3)
            {
                try
                {
                    System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                    help.FileName = Calculate.StartupPath + "\\apps\\qaac\\qaac.exe";
                    help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                    help.Arguments = " --formats";
                    help.UseShellExecute = false;
                    help.CreateNoWindow = true;

                    //Начиная с версии 1.26 qaac.exe работает со stdout\stderr используя UTF-8
                    help.StandardOutputEncoding = System.Text.Encoding.UTF8;
                    help.StandardErrorEncoding = System.Text.Encoding.UTF8;
                    help.RedirectStandardOutput = true;
                    help.RedirectStandardError = true;

                    System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);

                    //Именно в таком порядке (а по хорошему надо в отдельных потоках)
                    string std_out = p.StandardOutput.ReadToEnd();
                    string std_err = p.StandardError.ReadToEnd();

                    new ShowWindow(root_window, "qaac --formats", ((std_err.Length > 0) ? std_err + "\r\n" : "") + std_out, new FontFamily("Lucida Console"));
                }
                catch (Exception ex)
                {
                    new Message(root_window).ShowMessage(ex.Message, Languages.Translate("Error"));
                }
            }
        }

        private void check_no_delay_Click(object sender, RoutedEventArgs e)
        {
            m.qaac_options.no_delay = check_no_delay.IsChecked.Value;

            root_window.UpdateOutSize();
            root_window.UpdateManualProfile();
        }

        private void combo_gapless_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_gapless_mode.IsDropDownOpen || combo_gapless_mode.IsSelectionBoxHighlighted) && combo_gapless_mode.SelectedIndex != -1)
            {
                m.qaac_options.gapless_mode = combo_gapless_mode.SelectedIndex;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }
    }
}