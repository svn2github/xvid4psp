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
	public partial class NeroAAC
	{
        public Massive m;
        private AudioEncoding root_window;

        public NeroAAC(Massive mass, AudioEncoding AudioEncWindow)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = AudioEncWindow;

            combo_aac_profile.Items.Add("AAC-LC");
            combo_aac_profile.Items.Add("AAC-HE");
            combo_aac_profile.Items.Add("AAC-HEv2");

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            combo_mode.Items.Clear();
            //забиваем режимы кодирования
            foreach (string mode in Enum.GetNames(typeof(Settings.AudioEncodingModes)))
                combo_mode.Items.Add(mode);
            combo_mode.SelectedItem = m.aac_options.encodingmode.ToString();

            //прогружаем битрейты
            LoadBitrates();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //значение по умолчанию
            if (!combo_bitrate.Items.Contains(outstream.bitrate) ||
                outstream.bitrate == 0)
                outstream.bitrate = 128;

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                combo_bitrate.SelectedItem = m.aac_options.quality.ToString("0.00").Replace(",", ".");
            else
                combo_bitrate.SelectedItem = outstream.bitrate;

            combo_aac_profile.SelectedItem = m.aac_options.aacprofile;
        }

        private void LoadBitrates()
        {
            try
            {
                combo_bitrate.Items.Clear();
                if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                {
                    double n = 0;
                    while (n <= 1.01)
                    {
                        combo_bitrate.Items.Add(n.ToString("0.00").Replace(",", "."));
                        n += 0.01;
                    }
                }
                else
                {
                    int n = 16;
                    int maximum = Format.GetMaxAACBitrate(m);
                    while (n <= maximum)
                    {
                        combo_bitrate.Items.Add(n);
                        n += 4;
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

            //создаём свежий массив параметров Nero AAC
            m.aac_options = new aac_arguments();

            //берём пока что за основу последнюю строку
            string line = outstream.passes;

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-q")
                {
                    m.aac_options.encodingmode = Settings.AudioEncodingModes.VBR;
                    m.aac_options.quality = Calculate.ConvertStringToDouble(cli[n + 1]);
                }

                if (value == "-br")
                {
                    m.aac_options.encodingmode = Settings.AudioEncodingModes.ABR;
                    outstream.bitrate = Convert.ToInt32(cli[n + 1].Replace("000", ""));
                }

                if (value == "-cbr")
                {
                    m.aac_options.encodingmode = Settings.AudioEncodingModes.CBR;
                    outstream.bitrate = Convert.ToInt32(cli[n + 1].Replace("000", ""));
                }

                if (value == "-lc")
                    m.aac_options.aacprofile = "AAC-LC";

                if (value == "-he")
                    m.aac_options.aacprofile = "AAC-HE";

                if (value == "-hev2")
                    m.aac_options.aacprofile = "AAC-HEv2";

                n++;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.ABR)
                line += "-br " + outstream.bitrate + "000";

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.CBR)
                line += "-cbr " + outstream.bitrate + "000";

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                line += "-q " + m.aac_options.quality.ToString("0.00").Replace(",", ".");

            if (m.aac_options.aacprofile == "AAC-LC")
                line += " -lc";

            if (m.aac_options.aacprofile == "AAC-HE")
                line += " -he";

            if (m.aac_options.aacprofile == "AAC-HEv2")
                line += " -hev2";

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted)
            {
                string mode = combo_mode.SelectedItem.ToString();
                m.aac_options.encodingmode = (Settings.AudioEncodingModes)Enum.Parse(typeof(Settings.AudioEncodingModes), mode);
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                LoadBitrates();

                if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    combo_bitrate.SelectedItem = m.aac_options.quality.ToString("0.00").Replace(",", ".");
                else
                    combo_bitrate.SelectedItem = outstream.bitrate;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_bitrate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bitrate.IsDropDownOpen || combo_bitrate.IsSelectionBoxHighlighted)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    m.aac_options.quality = Calculate.ConvertStringToDouble(combo_bitrate.SelectedItem.ToString());
                else
                    outstream.bitrate = Convert.ToInt32(combo_bitrate.SelectedItem);

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_aac_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_aac_profile.IsDropDownOpen || combo_aac_profile.IsSelectionBoxHighlighted)
            {
                //TODO: прописать тут ограничение и смену битрейтов для HEv2
                m.aac_options.aacprofile = combo_aac_profile.SelectedItem.ToString();

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }


	}
}