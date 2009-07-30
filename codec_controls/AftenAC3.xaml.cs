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

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            //прогружаем битрейты
            LoadBitrates();

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //значение по умолчанию
            if (!combo_bitrate.Items.Contains(outstream.bitrate) ||
                outstream.bitrate == 0)
                outstream.bitrate = 224;

            combo_bitrate.SelectedItem = outstream.bitrate;
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
                if (value == "-b")
                    outstream.bitrate = Convert.ToInt32(cli[n + 1]);

                n++;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            line += "-b " + outstream.bitrate;

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


	}
}