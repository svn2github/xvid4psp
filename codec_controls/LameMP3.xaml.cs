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

            combo_channels_mode.Items.Add("Stereo");
            combo_channels_mode.Items.Add("Joint Stereo");
            combo_channels_mode.Items.Add("Forced Join Stereo");
            combo_channels_mode.Items.Add("Mono");

            combo_quality.Items.Add("0 - Best Quality");
            combo_quality.Items.Add("1");
            combo_quality.Items.Add("2 - Recomended");
            combo_quality.Items.Add("3");
            combo_quality.Items.Add("4");
            combo_quality.Items.Add("5 - Good Speed");
            combo_quality.Items.Add("6");
            combo_quality.Items.Add("7 - Very Fast");
            combo_quality.Items.Add("8");
            combo_quality.Items.Add("9 - Poor Quality");         

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
                    for (int n = 0; n <= 8; n++) combo_bitrate.Items.Add(n);
                    //Битрейт для VBR
                    outstream.bitrate = 0;
                }
                else
                {
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
            combo_quality.SelectedItem = m.mp3_options.encquality;

            check_force_samplearte.IsChecked = (m.mp3_options.forcesamplerate);
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров Lame
            m.mp3_options = new mp3_arguments();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //берём пока что за основу последнюю строку
            string line = outstream.passes;

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            int b = 0;
            int B = 0;
            int V = 0;
            int abr = 0;

            foreach (string value in cli)
            {
                if (value == "-m")
                {
                    string cmode = cli[n + 1];
                    if (cmode == "s")
                        m.mp3_options.channelsmode = "Stereo";
                    if (cmode == "j")
                        m.mp3_options.channelsmode = "Joint Stereo";
                    if (cmode == "f")
                        m.mp3_options.channelsmode = "Forced Joint Stereo";
                    if (cmode == "m")
                        m.mp3_options.channelsmode = "Mono";
                }

                if (value == "-q")
                {
                    string qlevel = cli[n + 1];
                    if (qlevel == "0")
                        m.mp3_options.encquality = "0 - Best Quality";
                    if (qlevel == "1")
                        m.mp3_options.encquality = "1";
                    if (qlevel == "2")
                        m.mp3_options.encquality = "2 - Recomended";
                    if (qlevel == "3")
                        m.mp3_options.encquality = "3";
                    if (qlevel == "4")
                        m.mp3_options.encquality = "4";
                    if (qlevel == "5")
                        m.mp3_options.encquality = "5 - Good Speed";
                    if (qlevel == "6")
                        m.mp3_options.encquality = "6";
                    if (qlevel == "7")
                        m.mp3_options.encquality = "7 - Very Fast";
                    if (qlevel == "8")
                        m.mp3_options.encquality = "8";
                    if (qlevel == "9")
                        m.mp3_options.encquality = "9 - Poor Quality";
                }

                if (value == "--resample")
                    m.mp3_options.forcesamplerate = true;

                if (value == "-b")
                    b = Convert.ToInt32(cli[n + 1]);
                if (value == "-B")
                    B = Convert.ToInt32(cli[n + 1]);
                if (value == "-V")
                    V = Convert.ToInt32(cli[n + 1]);
                if (value == "--abr")
                    abr = Convert.ToInt32(cli[n + 1]);

                n++;
            }

            //вычисляем какой всё таки это был режим
            if (V != 0)
            {
                m.mp3_options.encodingmode = Settings.AudioEncodingModes.VBR;
                m.mp3_options.quality = V;
                m.mp3_options.minb = b;
                m.mp3_options.maxb = B;
            }
            else if (abr != 0)
            {
                m.mp3_options.encodingmode = Settings.AudioEncodingModes.ABR;
                outstream.bitrate = abr;
                m.mp3_options.minb = b;
                m.mp3_options.maxb = B;
            }
            else
            {
                m.mp3_options.encodingmode = Settings.AudioEncodingModes.CBR;
                outstream.bitrate = b;
                m.mp3_options.minb = 0;
                m.mp3_options.maxb = 0;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.mp3_options.channelsmode == "Stereo")
                line += "-m s";
            if (m.mp3_options.channelsmode == "Joint Stereo")
                line += "-m j";
            if (m.mp3_options.channelsmode == "Forced Joint Stereo")
                line += "-m f";
            if (m.mp3_options.channelsmode == "Mono")
                line += "-m m";

            if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.ABR)
                line += " -b " + m.mp3_options.minb +
                    " --abr " + outstream.bitrate +
                    " -B " + m.mp3_options.maxb;
            else if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                line += " -b " + m.mp3_options.minb +
                    " -V " + m.mp3_options.quality +
                    " -B " + m.mp3_options.maxb;
            else
                line += " -b " + outstream.bitrate;

            line += " -q " + m.mp3_options.encquality.Substring(0, 1);

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
                  //  combo_bitrate.SelectedItem = outstream.bitrate;
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
                m.mp3_options.encquality = combo_quality.SelectedItem.ToString();

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void check_force_samplearte_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_force_samplearte.IsFocused)
            {
                m.mp3_options.forcesamplerate = check_force_samplearte.IsChecked.Value;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void check_force_samplearte_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_force_samplearte.IsFocused)
            {
                m.mp3_options.forcesamplerate = check_force_samplearte.IsChecked.Value;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }
	}
}