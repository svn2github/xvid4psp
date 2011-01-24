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
	public partial class LameMP3
	{
        public Massive m;
        private AudioEncoding root_window;

        public LameMP3(Massive mass, AudioEncoding AudioEncWindow)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = AudioEncWindow;

            combo_mode.Items.Add("CBR");
            combo_mode.Items.Add("VBR");
            combo_mode.Items.Add("ABR");

            combo_channels_mode.Items.Add("Auto");                //default is (j) or (s) depending on bitrate
            combo_channels_mode.Items.Add("Stereo");              //"-m s" (s)imple = force LR stereo on all frames
            combo_channels_mode.Items.Add("Joint Stereo");        //"-m j" (j)oint  = joins the best possible of MS and LR stereo
            combo_channels_mode.Items.Add("Forced Joint Stereo"); //"-m f" (f)orce  = force MS stereo on all frames.
            combo_channels_mode.Items.Add("Mono");                //"-m m" (d)ual-mono, (m)ono
            combo_channels_mode.ToolTip = "Auto - auto select (depending on bitrate), default\r\n" +
                "Stereo - force LR stereo on all frames\r\n" +
                "Joint Stereo - joins the best possible of MS and LR stereo\r\n" +
                "Forced Joint Stereo - force MS stereo on all frames\r\n" +
                "Mono - encode as mono";

            combo_quality.Items.Add("0 - Best Quality");
            combo_quality.Items.Add("1");
            combo_quality.Items.Add("2 - Recommended");
            combo_quality.Items.Add("3");
            combo_quality.Items.Add("4");
            combo_quality.Items.Add("5 - Good Speed");
            combo_quality.Items.Add("6");
            combo_quality.Items.Add("7 - Very Fast");
            combo_quality.Items.Add("8");
            combo_quality.Items.Add("9 - Poor Quality");
            combo_quality.ToolTip = "Noise shaping & psycho acoustic algorithms\r\n" +
                "0 - highest quality, very slow\r\n" +
                "2 - recommended, default\r\n" +
                "9 - poor quality, but fast";

            combo_gain.Items.Add("None");
            combo_gain.Items.Add("Fast");
            combo_gain.Items.Add("Accurate");
            combo_gain.ToolTip = "None - do not compute RG (slightly faster encoding)\r\n" +
                "Fast - compute RG fast and slightly inaccurately, default\r\n" +
                "Accurate - compute RG more accurately, but slower";

            //прогружаем битрейты
            LoadBitrates();

            LoadFromProfile();
		}

        private void LoadBitrates()
        {
            try
            {
                combo_bitrate.Items.Clear();
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                {
                    combo_bitrate.ToolTip = "0 - high quality, bigger files\r\n4 - default\r\n9 - poor quality, smaller files";

                    for (int n = 0; n <= 9; n++)
                        combo_bitrate.Items.Add(n);

                    //Битрейт для VBR
                    outstream.bitrate = 0;
                }
                else
                {
                    combo_bitrate.ToolTip = null;

                    if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.CBR)
                    {
                        int n = 8;
                        while (n <= 160)
                        {
                            if (n != 72 && n != 88 && n != 104 && n != 120 && n != 136 && n != 152)
                                combo_bitrate.Items.Add(n);
                            n += 8;
                        }
                        combo_bitrate.Items.Add(192);
                        combo_bitrate.Items.Add(224);
                        combo_bitrate.Items.Add(256);
                        combo_bitrate.Items.Add(320);

                    }
                    else
                    {
                        int n = 8;
                        while (n <= 320)
                        {
                            combo_bitrate.Items.Add(n);
                            n += 8;
                        }
                    }
                    //Битрейт по умолчанию
                    if (!combo_bitrate.Items.Contains(outstream.bitrate) || outstream.bitrate == 0)
                        outstream.bitrate = 192;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void LoadFromProfile()
        {
            //забиваем режимы кодирования
            combo_mode.SelectedItem = m.mp3_options.encodingmode.ToString();

            //прогружаем битрейты
            LoadBitrates();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                combo_bitrate.SelectedItem = m.mp3_options.quality;
            else
                combo_bitrate.SelectedItem = outstream.bitrate;

            combo_channels_mode.SelectedItem = m.mp3_options.channelsmode;
            combo_quality.SelectedIndex = m.mp3_options.encquality;
            combo_gain.SelectedIndex = m.mp3_options.replay_gain;

            check_force_samplearte.IsChecked = (m.mp3_options.forcesamplerate);
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров Lame
            m.mp3_options = new mp3_arguments();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            int n = 0;
            int b = -1;
            int B = -1;
            int V = -1;
            int abr = -1;

            //берём пока что за основу последнюю строку
            string line = outstream.passes;
            string[] cli = line.Split(new string[] { " " }, StringSplitOptions.None);

            foreach (string value in cli)
            {
                if (value == "-m")
                {
                    string cmode = cli[n + 1];
                    if (cmode == "s") m.mp3_options.channelsmode = "Stereo";
                    else if (cmode == "j") m.mp3_options.channelsmode = "Joint Stereo";
                    else if (cmode == "f") m.mp3_options.channelsmode = "Forced Joint Stereo";
                    else if (cmode == "m") m.mp3_options.channelsmode = "Mono";
                    else m.mp3_options.channelsmode = "Auto";
                }

                else if (value == "-q")
                {
                    int num = 0;
                    int.TryParse(cli[n + 1], out num);
                    m.mp3_options.encquality = num;
                }

                else if (value == "-b")
                    b = Convert.ToInt32(cli[n + 1]);

                else if (value == "-B")
                    B = Convert.ToInt32(cli[n + 1]);

                else if (value == "-V")
                    V = Convert.ToInt32(cli[n + 1]);

                else if (value == "--abr")
                    abr = Convert.ToInt32(cli[n + 1]);

                else if (value == "--noreplaygain")
                    m.mp3_options.replay_gain = 0;

                else if (value == "--replaygain-fast")
                    m.mp3_options.replay_gain = 1;

                else if (value == "--replaygain-accurate")
                    m.mp3_options.replay_gain = 2;

                else if (value == "--resample")
                    m.mp3_options.forcesamplerate = true;

                n++;
            }

            //вычисляем какой всё таки это был режим
            if (V >= 0)
            {
                m.mp3_options.encodingmode = Settings.AudioEncodingModes.VBR;
                outstream.bitrate = 0;
                m.mp3_options.quality = V;
                m.mp3_options.minb = (b >= 0) ? b : 32;
                m.mp3_options.maxb = (B >= 0) ? B : 320;
            }
            else if (abr >= 0)
            {
                m.mp3_options.encodingmode = Settings.AudioEncodingModes.ABR;
                outstream.bitrate = abr;
                m.mp3_options.minb = (b >= 0) ? b : 32;
                m.mp3_options.maxb = (B >= 0) ? B : 320;
            }
            else
            {
                m.mp3_options.encodingmode = Settings.AudioEncodingModes.CBR;
                outstream.bitrate = (b >= 0) ? b : 128;
                m.mp3_options.minb = 32;
                m.mp3_options.maxb = 320;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.mp3_options.channelsmode == "Stereo")
                line += "-m s ";
            else if (m.mp3_options.channelsmode == "Joint Stereo")
                line += "-m j ";
            else if (m.mp3_options.channelsmode == "Forced Joint Stereo")
                line += "-m f ";
            else if (m.mp3_options.channelsmode == "Mono")
                line += "-m m ";

            if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.ABR)
            {
                line += "--abr " + outstream.bitrate;

                if (m.mp3_options.minb != 32)
                    line += " -b " + m.mp3_options.minb;
                if (m.mp3_options.maxb != 320)
                    line += " -B " + m.mp3_options.maxb;
            }
            else if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
            {
                line += "-V " + m.mp3_options.quality;

                if (m.mp3_options.minb != 32)
                    line += " -b " + m.mp3_options.minb;
                if (m.mp3_options.maxb != 320)
                    line += " -B " + m.mp3_options.maxb;
            }
            else
                line += "-b " + outstream.bitrate;

            line += " -q " + m.mp3_options.encquality;

            if (m.mp3_options.replay_gain == 0)
                line += " --noreplaygain";
            else if (m.mp3_options.replay_gain == 2)
                line += " --replaygain-accurate";

            if (m.mp3_options.forcesamplerate)
                line += " --resample ";

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted)
            {
                string mode = combo_mode.SelectedItem.ToString();
                m.mp3_options.encodingmode = (Settings.AudioEncodingModes)Enum.Parse(typeof(Settings.AudioEncodingModes), mode);
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                LoadBitrates();

                if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                {
                    combo_bitrate.SelectedItem = m.mp3_options.quality;
                    m.mp3_options.minb = 32;
                    m.mp3_options.maxb = 320;
                }
                else if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.ABR)
                {
                    combo_bitrate.SelectedItem = outstream.bitrate;
                    m.mp3_options.minb = 32;
                    m.mp3_options.maxb = 320;
                }
                else
                {
                    combo_bitrate.SelectedItem = 192;
                    outstream.bitrate = 192;
                    //combo_bitrate.SelectedItem = outstream.bitrate;
                }

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_bitrate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bitrate.IsDropDownOpen || combo_bitrate.IsSelectionBoxHighlighted)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    m.mp3_options.quality = Convert.ToInt32(combo_bitrate.SelectedItem);
                else
                    outstream.bitrate = Convert.ToInt32(combo_bitrate.SelectedItem);

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_channels_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_channels_mode.IsDropDownOpen || combo_channels_mode.IsSelectionBoxHighlighted)
            {
                m.mp3_options.channelsmode = combo_channels_mode.SelectedItem.ToString();

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_quality_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_quality.IsDropDownOpen || combo_quality.IsSelectionBoxHighlighted)
            {
                m.mp3_options.encquality = combo_quality.SelectedIndex;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void check_force_samplearte_Click(object sender, RoutedEventArgs e)
        {
            m.mp3_options.forcesamplerate = check_force_samplearte.IsChecked.Value;

            root_window.UpdateOutSize();
            root_window.UpdateManualProfile();
        }

        private void combo_gain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_gain.IsDropDownOpen || combo_gain.IsSelectionBoxHighlighted)
            {
                m.mp3_options.replay_gain = combo_gain.SelectedIndex;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }
	}
}