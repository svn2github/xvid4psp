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
	public partial class FPCM
	{
        public Massive m;
        private AudioEncoding root_window;

        public FPCM(Massive mass, AudioEncoding AudioEncWindow)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = AudioEncWindow;

            combo_bits.Items.Add("16-bit");
            combo_bits.Items.Add("24-bit");
            combo_bits.Items.Add("32-bit");

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            //декодируем настройки из профиля
            m = DecodeLine(m);

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
            combo_bits.SelectedItem = outstream.bits + "-bit";
        }

        public static Massive DecodeLine(Massive m)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //берём пока что за основу последнюю строку
            string line = outstream.passes;

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-acodec")
                {
                    string bit = cli[n + 1];

                    if (bit == "pcm_s16le")
                        outstream.bits = 16;
                    if (bit == "pcm_s24le")
                        outstream.bits = 24;
                    if (bit == "pcm_s32le")
                        outstream.bits = 32;
                }
            }

            outstream.bitrate = (int)(0.016 * outstream.channels * (double)Convert.ToInt32(outstream.samplerate) * (double)outstream.bits / 16.0);

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (outstream.bits == 16)
                line = "-acodec pcm_s16le";
            if (outstream.bits == 24)
                line = "-acodec pcm_s24le";
            if (outstream.bits == 32)
                line = "-acodec pcm_s32le";

            //line += " -ab " + m.outabitrate + "000";

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_bits_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bits.IsDropDownOpen || combo_bits.IsSelectionBoxHighlighted)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                outstream.bits = Convert.ToInt32(combo_bits.SelectedItem.ToString().Substring(0, 2));
                outstream.bitrate = (int)(0.016 * outstream.channels * (double)Convert.ToInt32(outstream.samplerate) * (double)outstream.bits / 16.0);
                root_window.UpdateManualProfile();
                root_window.UpdateOutSize();
            }
        }
	}
}