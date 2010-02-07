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
    public partial class FFLAC
	{
        public Massive m;
        private AudioEncoding root_window;

        public FFLAC(Massive mass, AudioEncoding AudioEncWindow)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = AudioEncWindow;

            for (int n = 0; n < 13; n++) combo_level.Items.Add(n);
            for (int n = 0; n < 11; n++) combo_use_lpc.Items.Add(n);
            for (int n = 0; n < 16; n++) combo_precision.Items.Add(n);

            combo_level.ToolTip = "Set compression level:\r\n0 - fast, but bigger filesize\r\n5 - default\r\n12 - slow, but smaller filesize";
            combo_use_lpc.ToolTip = "LPC method for determining coefficients:\r\n0 - LPC with fixed pre-defined coeffs (fast)\r\n" +                 "1 - LPC with coeffs determined by Levinson-Durbin recursion (default)\r\n2+ - LPC with coeffs determined by Cholesky factorization using (Use LPC - 1) passes (10 - veeery slow)";
            combo_precision.ToolTip = "LPC coefficient precision (15 - default)";

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            combo_level.SelectedItem = m.flac_options.level;
            combo_use_lpc.SelectedItem = m.flac_options.use_lpc;
            combo_precision.SelectedItem = m.flac_options.lpc_precision;
        }

        public static Massive DecodeLine(Massive m)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
            
            //создаём свежий массив параметров FFmpeg FLAC
            m.flac_options = new flac_arguments();

            //берём пока что за основу последнюю строку
            string line = outstream.passes;

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-compression_level") m.flac_options.level = Convert.ToInt32(cli[n + 1]);
                else if (value == "-use_lpc") m.flac_options.use_lpc = Convert.ToInt32(cli[n + 1]);
                else if (value == "-lpc_coeff_precision ") m.flac_options.lpc_precision = Convert.ToInt32(cli[n + 1]);
                
                n++;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            string line = "-acodec flac -f flac";

            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.flac_options.level != 5) line += " -compression_level " + m.flac_options.level;
            if (m.flac_options.use_lpc != 1) line += " -use_lpc " + m.flac_options.use_lpc;
            if (m.flac_options.lpc_precision != 15) line += " -lpc_coeff_precision " + m.flac_options.lpc_precision;

            //забиваем данные в массив
            outstream.passes = line;

            return m;
        }

        private void combo_level_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_level.IsDropDownOpen || combo_level.IsSelectionBoxHighlighted)
            {
                m.flac_options.level = Convert.ToInt32(combo_level.SelectedItem);
                root_window.UpdateManualProfile();
            }
        }

        private void combo_use_lpc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_use_lpc.IsDropDownOpen || combo_use_lpc.IsSelectionBoxHighlighted)
            {
                m.flac_options.use_lpc = Convert.ToInt32(combo_use_lpc.SelectedItem);
                root_window.UpdateManualProfile();
            }
        }
        
        private void combo_precision_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_precision.IsDropDownOpen || combo_precision.IsSelectionBoxHighlighted)
            {
                m.flac_options.lpc_precision = Convert.ToInt32(combo_precision.SelectedItem);
                root_window.UpdateManualProfile();
            }
        }
	}
}