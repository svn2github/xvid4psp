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
	public partial class FFV1
	{
        public Massive m;
        private VideoEncoding root_window;
        private MainWindow p;

        public FFV1(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.p = parent;
            this.root_window = VideoEncWindow;

            //прогружаем colorspace
            combo_color.Items.Add("YV12");
            combo_color.Items.Add("YUY2");
            combo_color.Items.Add("RGB32");
            combo_color.Items.Add("YUV410P");
            combo_color.Items.Add("YUV411P");
            combo_color.Items.Add("YUV444P");

            //codertype
            combo_codertype.Items.Add("VLC");
            combo_codertype.Items.Add("AC");

            //context model
            combo_contextmodel.Items.Add("Small");
            combo_contextmodel.Items.Add("Large");

            LoadFromProfile();
            SetToolTips();
		}

        private void SetToolTips()
        {
            combo_contextmodel.ToolTip = "Context model (Default: Small)";
            combo_codertype.ToolTip = "Coder type (Default: VLC)";
            combo_color.ToolTip = "Output colorspace (Default: YV12)";
        }

        public void LoadFromProfile()
        {
            //битрейт
            m.outvbitrate = 1; //(int)((4.19 * (double)m.outresw * (double)m.outresh * Calculate.ConvertStringToDouble(m.outframerate)) / 1000.0);
            //А поскольку битрейт все-равно нельзя расчитать, то вот
            m.encodingmode = Settings.EncodingModes.Quantizer;

            combo_contextmodel.SelectedItem = m.ffmpeg_options.contextmodel;
            combo_color.SelectedItem = m.ffmpeg_options.colorspace;
            combo_codertype.SelectedItem = m.ffmpeg_options.codertype;
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров ffmpeg
            m.ffmpeg_options = new ffmpeg_arguments();

            m.encodingmode = Settings.EncodingModes.OnePass;

            //берём пока что за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-vtag")
                    m.ffmpeg_options.fourcc_huff = cli[n + 1];

                if (value == "-pix_fmt")
                {
                    //YUV422 - YUY2
                    //YUV420P - YV12 & I420

                    string color = cli[n + 1];
                    if (color == "yuv420p") m.ffmpeg_options.colorspace = "YV12";
                    else if (color == "yuv422p") m.ffmpeg_options.colorspace = "YUY2";
                    else m.ffmpeg_options.colorspace = color.ToUpper();
                }

                if (value == "-coder")
                    m.ffmpeg_options.codertype = cli[n + 1].ToUpper();

                if (value == "-context")
                {
                    if (Convert.ToInt32(cli[n + 1]) == 0) 
                        m.ffmpeg_options.contextmodel = "Small";
                    else m.ffmpeg_options.contextmodel = "Large";
                }

                n++;
            }

            m.outvbitrate = 1; //(int)((4.19 * (double)m.outresw * (double)m.outresh * Calculate.ConvertStringToDouble(m.outframerate)) / 1000.0);
            m.encodingmode = Settings.EncodingModes.Quantizer;

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //обнуляем старые строки
            m.vpasses.Clear();

            string line = "-vcodec ffv1 -an";

            if (m.ffmpeg_options.colorspace == "YV12") line += " -pix_fmt yuv420p";
            else if (m.ffmpeg_options.colorspace == "YUY2") line += " -pix_fmt yuv422p";
            else line += " -pix_fmt " + m.ffmpeg_options.colorspace.ToLower();

            if (m.ffmpeg_options.codertype != "VLC")
                line += " -coder " + m.ffmpeg_options.codertype.ToLower();

            if (m.ffmpeg_options.contextmodel == "Large")
                line += " -context 1";

            m.vpasses.Add(line);

            return m;
        }

        private void combo_contextmodel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_contextmodel.IsDropDownOpen || combo_contextmodel.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.contextmodel = combo_contextmodel.SelectedItem.ToString();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_color.IsDropDownOpen || combo_color.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.colorspace = combo_color.SelectedItem.ToString();

                m.outvbitrate = 1; //(int)((4.19 * (double)m.outresw * (double)m.outresh * Calculate.ConvertStringToDouble(m.outframerate)) / 1000.0);
                m.encodingmode = Settings.EncodingModes.Quantizer;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_codertype_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_codertype.IsDropDownOpen || combo_codertype.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.codertype = combo_codertype.SelectedItem.ToString();
                root_window.UpdateManualProfile();
            }
        }
	}
}