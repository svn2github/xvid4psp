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

            combo_mode.Items.Add("CBR");
            combo_mode.Items.Add("VBR");
            combo_mode.Items.Add("ABR");
            combo_mode.Items.Add("ABR 2-Pass");

            combo_aac_profile.Items.Add("Auto");
            combo_aac_profile.Items.Add("AAC-LC");
            combo_aac_profile.Items.Add("AAC-HE");
            combo_aac_profile.Items.Add("AAC-HEv2");

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            //забиваем режимы кодирования
            string mode = m.aac_options.encodingmode.ToString();
            if (mode == "CBR") combo_mode.SelectedItem = "CBR";
            else if (mode == "VBR") combo_mode.SelectedItem = "VBR";
            else if (mode == "ABR") combo_mode.SelectedItem = "ABR";
            else combo_mode.SelectedItem = "ABR 2-Pass";

            //прогружаем битрейты
            LoadBitrates();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //значение по умолчанию
            if (!combo_bitrate.Items.Contains(outstream.bitrate) || outstream.bitrate == 0)
                outstream.bitrate = 128;

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                combo_bitrate.SelectedItem = m.aac_options.quality.ToString("0.00").Replace(",", ".");
            else combo_bitrate.SelectedItem = outstream.bitrate;

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
                    while (n <= Format.GetMaxAACBitrate(m))
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
            int n = 0; bool auto = true;

            foreach (string value in cli)
            {
                if (value == "-2pass") m.aac_options.encodingmode = Settings.AudioEncodingModes.TwoPass;
                if (value == "-q")
                {
                    m.aac_options.encodingmode = Settings.AudioEncodingModes.VBR;
                    m.aac_options.quality = Calculate.ConvertStringToDouble(cli[n + 1]);
                }

                if (value == "-br" || value == "-cbr")
                {
                    if (m.aac_options.encodingmode != Settings.AudioEncodingModes.TwoPass)
                        if (value == "-br") m.aac_options.encodingmode = Settings.AudioEncodingModes.ABR;
                        else m.aac_options.encodingmode = Settings.AudioEncodingModes.CBR;
                    outstream.bitrate = Convert.ToInt32(cli[n + 1].Replace("000", ""));
                }

                if (value == "-lc" || value == "-he" || value == "-hev2")
                {
                    auto = false;
                    if (value == "-lc") m.aac_options.aacprofile = "AAC-LC";
                    else if (value == "-he") m.aac_options.aacprofile = "AAC-HE";
                    else m.aac_options.aacprofile = "AAC-HEv2";
                }
                if (auto) m.aac_options.aacprofile = "Auto";

                n++;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.TwoPass)
                line += "-2pass -br " + outstream.bitrate + "000";

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.ABR)
                line += "-br " + outstream.bitrate + "000";

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.CBR)
                line += "-cbr " + outstream.bitrate + "000";

            if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                line += "-q " + m.aac_options.quality.ToString("0.00").Replace(",", ".");

            if (m.aac_options.aacprofile == "AAC-LC") line += " -lc";
            if (m.aac_options.aacprofile == "AAC-HE") line += " -he";
            if (m.aac_options.aacprofile == "AAC-HEv2") line += " -hev2";

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted)
            {
                string mode = combo_mode.SelectedItem.ToString();
                if (mode == "CBR") m.aac_options.encodingmode = Settings.AudioEncodingModes.CBR;
                else if (mode == "VBR") m.aac_options.encodingmode = Settings.AudioEncodingModes.VBR;
                else if (mode == "ABR") m.aac_options.encodingmode = Settings.AudioEncodingModes.ABR;
                else m.aac_options.encodingmode = Settings.AudioEncodingModes.TwoPass;
                
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                LoadBitrates();

                if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    combo_bitrate.SelectedItem = m.aac_options.quality.ToString("0.00").Replace(",", ".");
                else combo_bitrate.SelectedItem = outstream.bitrate;

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
                else outstream.bitrate = Convert.ToInt32(combo_bitrate.SelectedItem);

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