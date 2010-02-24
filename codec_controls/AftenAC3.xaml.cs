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
	public partial class AftenAC3
	{
        public Massive m;
        private AudioEncoding root_window;

        public AftenAC3(Massive mass, AudioEncoding AudioEncWindow)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = AudioEncWindow;

            for (int i = 1; i < 32; i++) combo_dnorm.Items.Add("-" + i + "dB");

            combo_bandwidth.Items.Add("Auto");
            for (int i = 10; i < 24; i += 1) combo_bandwidth.Items.Add(i + "kHz");

            combo_dnorm.ToolTip = "Dialog normalization level. -31dB means that decoder will leave audio level as is while play back." +
                "\r\nHigher values will produce more quiet sound.\r\nDefault: -31dB";
            
            combo_bandwidth.ToolTip = "High-frequency cutoff. In Auto mode encoder will auto-select this parameter depending\r\non bitrate, samplerate" +
                " and N of channels. But you can specify it manually.\r\nDefault: Auto (not optimal, very low cutoff frequency!)";

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            //прогружаем битрейты
            LoadBitrates();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //значение по умолчанию
            if (!combo_bitrate.Items.Contains(outstream.bitrate) || outstream.bitrate == 0)
                outstream.bitrate = 224;

            combo_bitrate.SelectedItem = outstream.bitrate;
            combo_dnorm.SelectedIndex = m.ac3_options.dnorm - 1;

            if (m.ac3_options.bandwidth == -1) 
                combo_bandwidth.SelectedItem = "Auto";
            else //В вычислениях samplerate принят равным 48000kHz
                combo_bandwidth.SelectedItem = Convert.ToInt32((((m.ac3_options.bandwidth * 3.0) + 73) * 0.09375)).ToString() + "kHz";
        }

        private void LoadBitrates()
        {
            try
            {
                combo_bitrate.Items.Clear();
                int n = 64;
                while (n <= 640)
                {
                    if (n != 288 && n !=352 && n !=416 && n != 480 && n != 544 && n!= 608) //исключаем не поддерживаемые кодеком битрейты
                    combo_bitrate.Items.Add(n);
                    n += 32;
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

            //создаём свежий массив параметров
            m.ac3_options = new ac3_arguments();

            //берём пока что за основу последнюю строку
            string line = outstream.passes;

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-b") outstream.bitrate = Convert.ToInt32(cli[n + 1]);
                else if (value == "-dnorm") m.ac3_options.dnorm = Convert.ToInt32(cli[n + 1]);
                else if (value == "-w") m.ac3_options.bandwidth = Convert.ToInt32(cli[n + 1]);

                n++;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            line += "-b " + outstream.bitrate;
            if (m.ac3_options.dnorm != 31) line += " -dnorm " + m.ac3_options.dnorm;
            if (m.ac3_options.bandwidth != -1) line += " -w " + m.ac3_options.bandwidth;

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_bitrate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bitrate.IsDropDownOpen || combo_bitrate.IsSelectionBoxHighlighted)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                outstream.bitrate = Convert.ToInt32(combo_bitrate.SelectedItem);

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_dnorm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_dnorm.IsDropDownOpen || combo_dnorm.IsSelectionBoxHighlighted)
            {
                m.ac3_options.dnorm = combo_dnorm.SelectedIndex + 1;
                root_window.UpdateManualProfile();
            }
        }

        private void combo_bandwidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_bandwidth.IsDropDownOpen || combo_bandwidth.IsSelectionBoxHighlighted)
            {
                if (combo_bandwidth.SelectedItem.ToString() == "Auto")
                    m.ac3_options.bandwidth = -1;
                else //В вычислениях samplerate принят равным 48000kHz
                    m.ac3_options.bandwidth = Convert.ToInt32(((Convert.ToDouble(combo_bandwidth.SelectedItem.ToString().Substring(0, 2)) / 0.09375) - 73) / 3);
                root_window.UpdateManualProfile();
            }
        }
	}
}