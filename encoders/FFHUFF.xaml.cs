﻿using System;
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
	public partial class FFHUFF
	{
        public Massive m;
        private VideoEncoding root_window;
        private MainWindow p;

        public FFHUFF(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.p = parent;
            this.root_window = VideoEncWindow;

            //прогружаем fourcc
            combo_fourcc.Items.Add("HFYU");
            combo_fourcc.Items.Add("FFVH");

            //прогружаем colorspace
            combo_color.Items.Add("YV12");
            combo_color.Items.Add("YUY2");
            combo_color.Items.Add("RGB32");

            //предиктор
            combo_predictor.Items.Add("Left");
            combo_predictor.Items.Add("Plane");
            combo_predictor.Items.Add("Median");

            LoadFromProfile();
            SetToolTips();
		}

        private void SetToolTips()
        {
            combo_fourcc.ToolTip = "Force video tag/fourcc (Default: HFYU)";
            combo_predictor.ToolTip = "Prediction method (Default: Plane)";
            combo_color.ToolTip = "Output colorspace (Default: YV12)";
        }

        public void LoadFromProfile()
        {
            //битрейт
            m.outvbitrate = 1; //(int)((6.70495523 * (double)m.outresw * (double)m.outresh * Calculate.ConvertStringToDouble(m.outframerate)) / 1000.0);
            //А поскольку битрейт все-равно нельзя расчитать, то вот
            m.encodingmode = Settings.EncodingModes.Quantizer;

            combo_fourcc.SelectedItem = m.ffmpeg_options.fourcc_huff;
            combo_color.SelectedItem = m.ffmpeg_options.colorspace;
            combo_predictor.SelectedItem = m.ffmpeg_options.predictor;
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
                    //RGBA 32

                    string dvstandart = cli[n + 1];
                    if (dvstandart == "yuv420p") m.ffmpeg_options.colorspace = "YV12";
                    else if (dvstandart == "yuv422p") m.ffmpeg_options.colorspace = "YUY2";
                    else m.ffmpeg_options.colorspace = dvstandart.ToUpper();
                }

                if (value == "-pred")
                {
                    string pred = cli[n + 1];
                    if (pred == "left") m.ffmpeg_options.predictor = "Left";
                    else if (pred == "plane") m.ffmpeg_options.predictor = "Plane";
                    else m.ffmpeg_options.predictor = "Median";
                }

                n++;
            }

            m.outvbitrate = 1; //(int)((6.70495523 * (double)m.outresw * (double)m.outresh * Calculate.ConvertStringToDouble(m.outframerate)) / 1000.0);
            m.encodingmode = Settings.EncodingModes.Quantizer;

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //обнуляем старые строки
            m.vpasses.Clear();

            string line = "-vcodec ffvhuff -an";

            if (m.ffmpeg_options.colorspace == "YV12") line += " -pix_fmt yuv420p";
            else if (m.ffmpeg_options.colorspace == "YUY2") line += " -pix_fmt yuv422p";
            else line += " -pix_fmt " + m.ffmpeg_options.colorspace.ToLower();

            if (m.ffmpeg_options.predictor != "Plane")
                line += " -pred " + m.ffmpeg_options.predictor.ToLower();

            //fourcc
            line += " -vtag " + m.ffmpeg_options.fourcc_huff;

            m.vpasses.Add(line);

            return m;
        }

        private void combo_fourcc_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_fourcc.IsDropDownOpen || combo_fourcc.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.fourcc_huff = combo_fourcc.SelectedItem.ToString();
                Settings.HUFFFOURCC = m.ffmpeg_options.fourcc_huff;
                root_window.UpdateManualProfile();
            }
        }

        private void combo_color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_color.IsDropDownOpen || combo_color.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.colorspace = combo_color.SelectedItem.ToString();

                m.outvbitrate = 1; //(int)((6.70495523 * (double)m.outresw * (double)m.outresh * Calculate.ConvertStringToDouble(m.outframerate)) / 1000.0);
                m.encodingmode = Settings.EncodingModes.Quantizer;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }

        private void combo_predictor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_predictor.IsDropDownOpen || combo_predictor.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.predictor = combo_predictor.SelectedItem.ToString();
                root_window.UpdateManualProfile();
            }
        }
	}
}