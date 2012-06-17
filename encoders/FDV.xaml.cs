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
	public partial class FDV
	{
        public Massive m;
        private VideoEncoding root_window;
        private MainWindow p;

        public FDV(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
		{
			this.InitializeComponent();

            this.m = mass.Clone();
            this.p = parent;
            this.root_window = VideoEncWindow;

            //прогружаем fourcc
            combo_fourcc.Items.Add("dvsd");
            combo_fourcc.Items.Add("DVSD");
            combo_fourcc.Items.Add("dv25");
            combo_fourcc.Items.Add("DV25");
            combo_fourcc.Items.Add("dv50");
            combo_fourcc.Items.Add("DV50");

            //прогружаем пресеты
            combo_preset.Items.Add("DVCAM");
            combo_preset.Items.Add("DVCPRO25");
            combo_preset.Items.Add("DVCPRO50");

            LoadFromProfile();
            SetToolTips();
		}

        private void SetToolTips()
        {
            combo_fourcc.ToolTip = "Force video tag/fourcc (Default: DVSD)";
            combo_preset.ToolTip = "DV standarts (Default: DVCAM)\r\nDVCAM - colorspace yuv420p (only for PAL!)\r\n" +
                "DVCPRO25 - colorspace yuv411p\r\nDVCPRO50 - colorspace yuv422p\r\n";
        }

        public void LoadFromProfile()
        {
            combo_fourcc.SelectedItem = m.ffmpeg_options.fourcc_dv;
            combo_preset.SelectedItem = m.ffmpeg_options.dvpreset;
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
                    m.ffmpeg_options.fourcc_dv = cli[n + 1];

                if (value == "-pix_fmt")
                {
                    string dvstandart = cli[n + 1];
                    if (dvstandart == "yuv420p") m.ffmpeg_options.dvpreset = "DVCAM";
                    else if (dvstandart == "yuv411p") m.ffmpeg_options.dvpreset = "DVCPRO25";
                    else if (dvstandart == "yuv422p") m.ffmpeg_options.dvpreset = "DVCPRO50";

                    //dvcam 
                    //-f mov -vcodec dvvideo -r 25 -pix_fmt yuv420p -vtag dvcp -acodec pcm_s16be -ac 2

                    //dvcpro25 
                    //-f mov -vcodec dvvideo -r 25 -pix_fmt yuv411p -vtag dvpp -acodec pcm_s16be -ac 2 

                    //dvcpro50 
                    //-f mov -vcodec dvvideo -r 25 -pix_fmt yuv422p -vtag dv5p -acodec pcm_s16be -ac 2 
                }

                n++;
            }

            //битрейт
            if (m.format == Format.ExportFormats.AviDVPAL)
            {
                if (m.ffmpeg_options.dvpreset == "DVCPRO50")
                    m.outvbitrate = 57600;
                else
                    m.outvbitrate = 28800;
            }
            if (m.format == Format.ExportFormats.AviDVNTSC)
            {
                if (m.ffmpeg_options.dvpreset == "DVCPRO50")
                    m.outvbitrate = 57543;
                else
                    m.outvbitrate = 28771;
            }

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //обнуляем старые строки
            m.vpasses.Clear();

            string line = "-vcodec dvvideo -an";

            if (m.ffmpeg_options.dvpreset == "DVCAM") line += " -pix_fmt yuv420p";
            else if (m.ffmpeg_options.dvpreset == "DVCPRO25") line += " -pix_fmt yuv411p";
            else if (m.ffmpeg_options.dvpreset == "DVCPRO50") line += " -pix_fmt yuv422p";

            //fourcc
            line += " -vtag " + m.ffmpeg_options.fourcc_dv;

            m.vpasses.Add(line);
            return m;
        }

        private void combo_fourcc_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_fourcc.IsDropDownOpen || combo_fourcc.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.fourcc_dv = combo_fourcc.SelectedItem.ToString();
                Settings.DVFOURCC = m.ffmpeg_options.fourcc_dv;
                root_window.UpdateManualProfile();
            }
        }

        private void combo_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_preset.IsDropDownOpen || combo_preset.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.dvpreset = combo_preset.SelectedItem.ToString();
                //битрейт
                if (m.format == Format.ExportFormats.AviDVPAL)
                {
                    if (m.ffmpeg_options.dvpreset == "DVCPRO50")
                        m.outvbitrate = 57600;
                    else
                        m.outvbitrate = 28800;
                }
                if (m.format == Format.ExportFormats.AviDVNTSC)
                {
                    if (m.ffmpeg_options.dvpreset == "DVCPRO50")
                        m.outvbitrate = 57543;
                    else
                        m.outvbitrate = 28771;
                }
                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
            }
        }
	}
}