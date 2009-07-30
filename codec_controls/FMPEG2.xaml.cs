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
using System.Globalization;


namespace XviD4PSP
{
	public partial class FMPEG2
	{
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum CodecPresets { Default = 1, Turbo, Ultra, Custom }

        public FMPEG2(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
		{
			this.InitializeComponent();

            this.num_bitrate.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_bitrate_ValueChanged);
            this.num_bittolerance.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_bittolerance_ValueChanged);
            this.num_buffsize.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_buffsize_ValueChanged);
            this.num_gopsize.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_gopsize_ValueChanged);
            this.num_maxbitrate.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_maxbitrate_ValueChanged);
            this.num_minbitrate.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_minbitrate_ValueChanged);

            this.m = mass.Clone();
            this.p = parent;
            this.root_window = VideoEncWindow;

            combo_mode.Items.Add("1-Pass Bitrate");
            combo_mode.Items.Add("2-Pass Bitrate");
            //combo_mode.Items.Add("3-Pass Bitrate");
            combo_mode.Items.Add("Constant Quality");
            combo_mode.Items.Add("2-Pass Quality");
            combo_mode.Items.Add("2-Pass Size");
            //combo_mode.Items.Add("3-Pass Size");

            //Прописываем ME метод
            //combo_me_method.Items.Add("ZERO");
            //combo_me_method.Items.Add("FULL");
            //combo_me_method.Items.Add("EPSZ");
            //combo_me_method.Items.Add("LOG");
            //combo_me_method.Items.Add("PHODS");
            //combo_me_method.Items.Add("X1");
            //combo_me_method.Items.Add("HEX");
            //combo_me_method.Items.Add("UMH");
            //combo_me_method.Items.Add("ITER");
            combo_me_method.Items.Add("Default Search"); //0
            combo_me_method.Items.Add("Sab Diamond Search"); //-2-
            combo_me_method.Items.Add("Funny Diamond Search"); //-1
            combo_me_method.Items.Add("Small Diamond Search"); //2+
            combo_me_method.Items.Add("L2s Diamond Search"); //257+
            combo_me_method.Items.Add("HEX Search"); //513+
            combo_me_method.Items.Add("UMH Search"); //769+
            combo_me_method.Items.Add("Full Search"); //1025+



            //прописываем cmp
            combo_cmp.Items.Add("SAD");
            combo_cmp.Items.Add("SSE");
            combo_cmp.Items.Add("SATD");
            combo_cmp.Items.Add("DCT");
            combo_cmp.Items.Add("PSNR");
            combo_cmp.Items.Add("BIT");
            combo_cmp.Items.Add("RD");
            combo_cmp.Items.Add("ZERO");
            combo_cmp.Items.Add("VSAD");
            combo_cmp.Items.Add("VSSE");
            combo_cmp.Items.Add("NSSE");
            //combo_cmp.Items.Add("W53");
            //combo_cmp.Items.Add("W97");

            //прогружаем матрицы квантизации
            combo_qmatrix.Items.Add("H263");
            foreach (string matrix in PresetLoader.CustomMatrixes(MatrixTypes.TXT))
                combo_qmatrix.Items.Add(matrix);

            //прогружаем fourcc
            combo_fourcc.Items.Add("MPEG");
            combo_fourcc.Items.Add("MPG2");

            //прогружаем векторы
            combo_mvectors.Items.Add("Disabled");
            combo_mvectors.Items.Add("MV0");
            combo_mvectors.Items.Add("MV4");
            combo_mvectors.Items.Add("Unlimited");

            //прогружаем mbd
            combo_mbd.Items.Add("Simple");
            combo_mbd.Items.Add("Fewest bits");
            combo_mbd.Items.Add("Rate distortion");

            //B фреймы
            for (int n = 0; n <= 16; n++)
                combo_bframes.Items.Add(n);

            combo_bdecision.Items.Add("Disabled");
            for (int n = 0; n <= 10; n++)
                combo_bdecision.Items.Add(n.ToString());

            combo_brefine.Items.Add("Disabled");
            for (int n = 0; n <= 4; n++)
                combo_brefine.Items.Add(n.ToString());

            //пресеты настроек и качества
            foreach (string preset in Enum.GetNames(typeof(CodecPresets)))
                combo_codec_preset.Items.Add(preset);

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            SetMinMaxBitrate();

            combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
            if (combo_mode.SelectedItem == null)
            {
                m.encodingmode = Settings.EncodingModes.OnePass;
                combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
                SetMinMaxBitrate();
                SetDefaultBitrates();
            }

            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
            else
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";

            //запоминаем первичные режим кодирования
            oldmode = m.encodingmode;

            //защита для гладкой смены кодеков
            if (m.outvbitrate > 90000)
                m.outvbitrate = 90000;
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                if (m.outvbitrate > 31)
                    m.outvbitrate = 31;
                if (m.outvbitrate == 0)
                    m.outvbitrate = 1;
            }

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.Value = (decimal)m.outvbitrate;
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                     m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.Value = (decimal)m.outvbitrate;
            else
                num_bitrate.Value = (decimal)m.outvbitrate;

            //combo_me_method.SelectedIndex = m.ffmpeg_options.memethod;
            if (m.ffmpeg_options.dia_size == 0)
                combo_me_method.SelectedItem = "Default Search";
            else if (m.ffmpeg_options.dia_size == -2)
                combo_me_method.SelectedItem = "Sab Diamond Search";
            else if (m.ffmpeg_options.dia_size == -1)
                combo_me_method.SelectedItem = "Funny Diamond Search";
            else if (m.ffmpeg_options.dia_size == 2)
                combo_me_method.SelectedItem = "Small Diamond Search";
            else if (m.ffmpeg_options.dia_size == 257)
                combo_me_method.SelectedItem = "L2s Diamond Search";
            else if (m.ffmpeg_options.dia_size == 513)
                combo_me_method.SelectedItem = "HEX Search";
            else if (m.ffmpeg_options.dia_size == 769)
                combo_me_method.SelectedItem = "UMH Search";
            else if (m.ffmpeg_options.dia_size == 1025)
                combo_me_method.SelectedItem = "Full Search";
            
            
            combo_cmp.SelectedIndex = m.ffmpeg_options.cmp;      
            check_trellis.IsChecked = m.ffmpeg_options.trellis;
            combo_bframes.SelectedItem = m.ffmpeg_options.bframes;
            check_qprd.IsChecked = m.ffmpeg_options.qprd;
            check_cbp.IsChecked = m.ffmpeg_options.cbp;
            combo_fourcc.SelectedItem = m.ffmpeg_options.fourcc_mpeg2;
            combo_bdecision.SelectedItem = m.ffmpeg_options.bdecision;
            combo_brefine.SelectedItem = m.ffmpeg_options.brefine;
            check_bitexact.IsChecked = m.ffmpeg_options.bitexact;
            check_intra.IsChecked = m.ffmpeg_options.intra;

            num_gopsize.Value = m.ffmpeg_options.gopsize;
            num_minbitrate.Value = m.ffmpeg_options.minbitrate;
            num_maxbitrate.Value = m.ffmpeg_options.maxbitrate;
            num_buffsize.Value = m.ffmpeg_options.buffsize;
            num_bittolerance.Value = m.ffmpeg_options.bittolerance;

            if (m.ffmpeg_options.mbd == "simple")
                combo_mbd.SelectedItem = "Simple";
            if (m.ffmpeg_options.mbd == "bits")
                combo_mbd.SelectedItem = "Fewest bits";
            if (m.ffmpeg_options.mbd == "rd")
                combo_mbd.SelectedItem = "Rate distortion";

            combo_mvectors.SelectedItem = m.ffmpeg_options.mvectors;

            if (m.ffmpeg_options.intramatrix != null || m.ffmpeg_options.intermatrix != null)
            {
                string setmatrix = "H263";
                foreach (string matrix in combo_qmatrix.Items)
                {
                    if (m.ffmpeg_options.intermatrix == PresetLoader.GetInterMatrix(matrix) &&
                        m.ffmpeg_options.intramatrix == PresetLoader.GetIntraMatrix(matrix))
                        setmatrix = matrix;
                }
                combo_qmatrix.SelectedItem = setmatrix;
            }
            else
                combo_qmatrix.SelectedItem = "H263";

            SetToolTips();
            DetectCodecPreset();
        }

        private void SetToolTips()
        {
            combo_mode.ToolTip = "Encoding mode";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                 m.encodingmode == Settings.EncodingModes.TwoPass ||
                 m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.ToolTip = "Set bitrate (Default: Auto)" + Environment.NewLine +
                    "For DVD must don`t be more than 9800 kbps video + audio bitrate\nalso max bitrate must be limited and buffer set to 1835 kbit";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                      m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else
                num_bitrate.ToolTip = "Set target quality (Default: 3)";

            combo_me_method.ToolTip = "Motion estimation method (Default: EPSZ)" + Environment.NewLine +
                "ZERO - zero motion estimation (fastest)" + Environment.NewLine +
                "FULL - full motion estimation (slowest)" + Environment.NewLine +
                "EPZS - epzs motion estimation (default)" + Environment.NewLine +
                "LOG - log motion estimation" + Environment.NewLine +
                "PHODS - phods motion estimation" + Environment.NewLine +
                "X1 - X1 motion estimation" + Environment.NewLine +
                "HEX - hex motion estimation" + Environment.NewLine +
                "UMH - umh motion estimation" + Environment.NewLine +
                "ITER - iter motion estimation";

            combo_qmatrix.ToolTip = "Use custom MPEG4 quantization matrix (Default: H263)";
            check_trellis.ToolTip = "Trellis quantization (Default: Disabled)";
            combo_mvectors.ToolTip = "0 - don`t use motion vectors (Default)" + Environment.NewLine + 
                                     "4 - use four motion vector by macroblock" + Environment.NewLine +
                                     "Unlimited - use unlimited motion vectors (Best)";
            check_qprd.ToolTip = "Use rate distortion optimization for qp selection (Default: Disabled)";
            check_cbp.ToolTip = "Use rate distortion optimization for cbp (Default: Disabled)";
            combo_fourcc.ToolTip = "Force video tag/fourcc (Default: MPEG)";
            check_bitexact.ToolTip = "Use only bitexact stuff, except dct (Default: Disabled)";
            combo_bframes.ToolTip = "Max bframes (Default: 0)";
            combo_bdecision.ToolTip = "Downscales frames for dynamic B-frame decision (Default: Disabled)";
            combo_brefine.ToolTip = "Refine the two motion vectors used in bidirectional macroblocks (Default: Disabled)";
            combo_mbd.ToolTip = "Macroblock decision algorithm (Default: Simple)" + Environment.NewLine + 
                                "Simple - Fast" + Environment.NewLine + 
                                "Fewest bits - Medium" + Environment.NewLine + 
                                "Rate distortion - Best Quality";
            num_gopsize.ToolTip = "Set the group of picture size (Default: Auto)";
            num_minbitrate.ToolTip = "Set min video bitrate tolerance in kbps (Default: Auto)";
            num_maxbitrate.ToolTip = "Set max video bitrate tolerance in kbps (Default: Auto)\nFor DVD max bitrate must be limited to 9000-9400 kbps";
            num_bittolerance.ToolTip = "Set video bitrate tolerance in kb (Default: Auto)";
            num_buffsize.ToolTip = "Set ratecontrol buffer size (Default: Auto)\nFor DVD buffer must be 1835 kbit";
            check_intra.ToolTip = "Use only intra frames - Highest Quality Encoding (Default: Disabled)";
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                combo_codec_preset.ToolTip = "Default - default codec settings to default" + Environment.NewLine +
                    "Turbo - fast encoding, big output file size" + Environment.NewLine +
                    "Ultra - high quality encoding, medium file size" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            else
            {
                combo_codec_preset.ToolTip = "Default - default codec settings to default" + Environment.NewLine +
                    "Turbo - fast encoding, bad quality" + Environment.NewLine +
                    "Ultra - high quality encoding, optimal speed-quality solution" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            combo_cmp.ToolTip = "Motion estimation compare function (Default: SAD)" + Environment.NewLine +
    "SAD - sum of absolute differences, fast" + Environment.NewLine +
    "SSE - sum of squared errors" + Environment.NewLine +
    "SATD - sum of absolute Hadamard transformed differences" + Environment.NewLine +
    "DCT - sum of absolute DCT transformed differences" + Environment.NewLine +
    "PSNR - sum of squared quantization errors (avoid, low quality)" + Environment.NewLine +
    "BIT - number of bits needed for the block" + Environment.NewLine +
    "RD - rate distortion optimal, slow" + Environment.NewLine +
    "ZERO - 0" + Environment.NewLine +
    "VSAD - sum of absolute vertical differences" + Environment.NewLine +
    "VSSE - sum of squared vertical differences" + Environment.NewLine +
    "NSSE - noise preserving sum of squared differences";
            //"W53 - 5/3 wavelet, only used in snow" + Environment.NewLine +
            //"W97 - 9/7 wavelet, only used in snow";
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров ffmpeg
            m.ffmpeg_options = new ffmpeg_arguments();

            //для начала определяем колличество проходов
            Settings.EncodingModes mode = Settings.EncodingModes.OnePass;
            if (m.vpasses.Count == 3)
                mode = Settings.EncodingModes.ThreePass;
            else if (m.vpasses.Count == 2)
                mode = Settings.EncodingModes.TwoPass;

            //берём пока что за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-b")
                    m.outvbitrate = Convert.ToInt32(cli[n + 1]) / 1000;

                if (value == "-sizemode")
                {
                    m.outvbitrate = Convert.ToInt32(cli[n + 1]) / 1000;
                    if (m.vpasses.Count == 3)
                        mode = Settings.EncodingModes.ThreePassSize;
                    else if (m.vpasses.Count == 2)
                        mode = Settings.EncodingModes.TwoPassSize;
                }

                if (m.vpasses.Count == 1)
                {
                    if (value == "-qscale")
                    {
                        mode = Settings.EncodingModes.Quality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (m.vpasses.Count == 2)
                {
                    if (value == "-qscale")
                    {
                        mode = Settings.EncodingModes.TwoPassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (m.vpasses.Count == 3)
                {
                    if (value == "-qscale")
                    {
                        mode = Settings.EncodingModes.ThreePassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (value == "-maxrate")
                    m.ffmpeg_options.maxbitrate = Convert.ToInt32(cli[n + 1]) / 1000;

                if (value == "-minrate")
                    m.ffmpeg_options.minbitrate = Convert.ToInt32(cli[n + 1]) / 1000;

                if (value == "-bt")
                    m.ffmpeg_options.bittolerance = Convert.ToInt32(cli[n + 1]) / 1000;

                if (value == "-bufsize")
                    m.ffmpeg_options.buffsize = Convert.ToInt32(cli[n + 1]) / 1000;

                if (value == "-trellis") //тут
                    m.ffmpeg_options.trellis = true;

                //дешифруем флаги
                if (value == "-flags")
                {
                    string flags_string = cli[n + 1];
                    string[] separator2 = new string[] { "+" };
                    string[] flags = flags_string.Split(separator2, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string flag in flags)
                    {
                       // if (flag == "trell") //тут
                       //     m.ffmpeg_options.trellis = true;

                        if (flag == "mv0")
                            m.ffmpeg_options.mvectors = "MV0";

                        if (flag == "mv4")
                            m.ffmpeg_options.mvectors = "MV4";

                        if (flag == "umv")
                            m.ffmpeg_options.mvectors = "Unlimited";

                        if (flag == "qprd")
                            m.ffmpeg_options.qprd = true;

                        if (flag == "cbp")
                            m.ffmpeg_options.cbp = true;

                        if (flag == "bitexact")
                            m.ffmpeg_options.bitexact = true;

                    }
                }

                if (value == "-mbd")
                    m.ffmpeg_options.mbd = cli[n + 1];

                if (value == "-inter_matrix")
                    m.ffmpeg_options.intermatrix = cli[n + 1];

                if (value == "-intra_matrix")
                    m.ffmpeg_options.intramatrix = cli[n + 1];

                if (value == "-intra")
                    m.ffmpeg_options.intra = true;

                //if (value == "-me_method")
                //   m.ffmpeg_options.memethod = Convert.ToInt32(cli[n + 1]);

                if (value == "-dia_size") //из файла в профиль
                    m.ffmpeg_options.dia_size = Convert.ToInt32(cli[n + 1]);

              
                if (value == "-cmp")
                    m.ffmpeg_options.cmp = Convert.ToInt32(cli[n + 1]);

                if (value == "-bf")
                    m.ffmpeg_options.bframes = Convert.ToInt32(cli[n + 1]);

                if (value == "-brd_scale")
                    m.ffmpeg_options.bdecision = cli[n + 1];

                if (value == "-bidir_refine")
                    m.ffmpeg_options.brefine = cli[n + 1];

                if (value == "-g")
                    m.ffmpeg_options.gopsize = Convert.ToInt32(cli[n + 1]);

                if (value == "-vtag")
                    m.ffmpeg_options.fourcc_mpeg2 = cli[n + 1];

                n++;
            }

            //прописываем вычисленное колличество проходов
            m.encodingmode = mode;

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //обнуляем старые строки
            m.vpasses.Clear();

            string line_turbo = "";
            string line = "-vcodec mpeg2video -an ";

            //битрейты
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line += "-b " + m.outvbitrate * 1000;
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                line += "-sizemode " + m.outvbitrate * 1000;
            else
                line += "-qscale " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1);

            //создаём пустой массив -flags
            string flags = " -flags ";

            if (m.ffmpeg_options.maxbitrate != 0)
                line += " -maxrate " + m.ffmpeg_options.maxbitrate * 1000;

            if (m.ffmpeg_options.minbitrate != 0)
                line += " -minrate " + m.ffmpeg_options.minbitrate * 1000;

            if (m.ffmpeg_options.bittolerance != 0)
                line += " -bt " + m.ffmpeg_options.bittolerance * 1000;

            if (m.ffmpeg_options.bitexact)
                flags += "+bitexact";

            if (m.ffmpeg_options.buffsize != 0)
                line += " -bufsize " + m.ffmpeg_options.buffsize * 1000;

           // if (m.ffmpeg_options.memethod != 2)
            //    line += " -me_method " + m.ffmpeg_options.memethod;

            if (m.ffmpeg_options.dia_size != 0)
                line += " -dia_size " + m.ffmpeg_options.dia_size; 
            
            if (m.ffmpeg_options.intramatrix != null)
                line += " -intra_matrix " + m.ffmpeg_options.intramatrix;

            if (m.ffmpeg_options.intermatrix != null)
                line += " -inter_matrix " + m.ffmpeg_options.intermatrix;

            if (m.ffmpeg_options.intra)
                line += " -intra";

            if (m.ffmpeg_options.trellis)
                //flags += "+trell"; //тут
                line += " -trellis 1 ";

            if (m.ffmpeg_options.mvectors == "Unlimited")///
                flags += "+umv";
            else if (m.ffmpeg_options.mvectors == "MV4")///
                flags += "+mv4";
            else if (m.ffmpeg_options.mvectors == "MV0")///
                flags += "+mv0";

            if (m.ffmpeg_options.qprd)
                flags += "+qprd";///

            if (m.ffmpeg_options.cbp)///
                flags += "+cbp";

            if (m.ffmpeg_options.bframes != 0)
                line += " -bf " + m.ffmpeg_options.bframes;

            if (m.ffmpeg_options.bdecision != "Disabled")
                line += " -brd_scale " + m.ffmpeg_options.bdecision;

            if (m.ffmpeg_options.brefine != "Disabled")
                line += " -bidir_refine " + m.ffmpeg_options.brefine;

            if (m.ffmpeg_options.mbd != "simple")
                line += " -mbd " + m.ffmpeg_options.mbd;

            //глобально прописываем cmp
            if (m.ffmpeg_options.cmp != 0)
                line += " -cmp " + m.ffmpeg_options.cmp +
                    " -subcmp " + m.ffmpeg_options.cmp +
                    " -mbcmp " + m.ffmpeg_options.cmp +
                    " -ildctcmp " + m.ffmpeg_options.cmp +
                    " -precmp " + m.ffmpeg_options.cmp +
                    " -skipcmp " + m.ffmpeg_options.cmp;

            if (m.ffmpeg_options.gopsize != 0)
                line += " -g " + m.ffmpeg_options.gopsize;

            line += " -vtag " + m.ffmpeg_options.fourcc_mpeg2;

            //передаём массив флагов
            if (flags != " -flags ")
                line += flags;

            //передаём параметры в турбо строку
            line_turbo += line;

            //удаляем пустоту в начале
            if (line.StartsWith(" "))
                line = line.Remove(0, 1);
            if (line_turbo.StartsWith(" "))
                line_turbo = line_turbo.Remove(0, 1);

            //забиваем данные в массив
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
m.encodingmode == Settings.EncodingModes.Quality ||
m.encodingmode == Settings.EncodingModes.Quantizer)
                m.vpasses.Add(line);

            if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize)
            {
                m.vpasses.Add(line_turbo);
                m.vpasses.Add(line);
            }

            if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.vpasses.Add(line_turbo);
                m.vpasses.Add(line);
                m.vpasses.Add(line);
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line);
            }

            if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line);
                m.vpasses.Add(line);
            }

            return m;
        }

        private void combo_codec_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_codec_preset.IsDropDownOpen || combo_codec_preset.IsSelectionBoxHighlighted)
            {
                CodecPresets preset = (CodecPresets)Enum.Parse(typeof(CodecPresets), combo_codec_preset.SelectedItem.ToString());

                if (preset == CodecPresets.Default)
                {
                    m.ffmpeg_options.bdecision = "Disabled";
                    m.ffmpeg_options.bframes = 0;
                    m.ffmpeg_options.bitexact = false;
                    m.ffmpeg_options.bittolerance = 0;
                    m.ffmpeg_options.brefine = "Disabled";
                    m.ffmpeg_options.buffsize = 0;
                    m.ffmpeg_options.cbp = false;
                    m.ffmpeg_options.fourcc_mpeg2 = "MPEG";
                    m.ffmpeg_options.gopsize = 0;
                    m.ffmpeg_options.intermatrix = null;
                    m.ffmpeg_options.intra = false;
                    m.ffmpeg_options.intramatrix = null;
                    m.ffmpeg_options.maxbitrate = 0;
                    m.ffmpeg_options.mbd = "simple";
                    m.ffmpeg_options.memethod = 2;
                    m.ffmpeg_options.minbitrate = 0;
                    m.ffmpeg_options.mvectors = "Disabled";
                    m.ffmpeg_options.qprd = false;
                    m.ffmpeg_options.trellis = false;
                    m.ffmpeg_options.cmp = 0;
                    //m.encodingmode = Settings.EncodingModes.OnePass;
                    SetDefaultBitrates();
                }

                if (preset == CodecPresets.Turbo)
                {
                    m.ffmpeg_options.bdecision = "Disabled";
                    m.ffmpeg_options.bframes = Format.GetMaxBFrames(m);
                    m.ffmpeg_options.brefine = "Disabled";
                    m.ffmpeg_options.cbp = false;
                    m.ffmpeg_options.mbd = "simple";
                    m.ffmpeg_options.memethod = 0;
                    m.ffmpeg_options.mvectors = "Disabled";
                    m.ffmpeg_options.qprd = false;
                    m.ffmpeg_options.trellis = false;
                    m.ffmpeg_options.cmp = 0;
                }

                if (preset == CodecPresets.Ultra)
                {
                    m.ffmpeg_options.bframes = Format.GetMaxBFrames(m);
                    m.ffmpeg_options.cbp = false;
                    m.ffmpeg_options.mbd = "bits";
                    m.ffmpeg_options.memethod = 2;
                    m.ffmpeg_options.mvectors = "Unlimited";
                    m.ffmpeg_options.qprd = false;
                    m.ffmpeg_options.trellis = true;
                    m.ffmpeg_options.cmp = 2;
                }

                if (preset != CodecPresets.Custom)
                {
                    LoadFromProfile();
                    root_window.UpdateOutSize();
                    root_window.UpdateManualProfile();
                }
            }
        }

        private void DetectCodecPreset()
        {
            CodecPresets preset = CodecPresets.Custom;

            //Default
            if (m.ffmpeg_options.bdecision == "Disabled" &&
             m.ffmpeg_options.bframes == 0 &&
             m.ffmpeg_options.bitexact == false &&
             m.ffmpeg_options.bittolerance == 0 &&
             m.ffmpeg_options.brefine == "Disabled" &&
             m.ffmpeg_options.buffsize == 0 &&
             m.ffmpeg_options.cbp == false &&
             m.ffmpeg_options.fourcc_mpeg2 == "MPEG" &&
             m.ffmpeg_options.gopsize == 0 &&
             m.ffmpeg_options.intermatrix == null &&
             m.ffmpeg_options.intra == false &&
             m.ffmpeg_options.intramatrix == null &&
             m.ffmpeg_options.maxbitrate == 0 &&
             m.ffmpeg_options.mbd == "simple" &&
             m.ffmpeg_options.memethod == 2 &&
             m.ffmpeg_options.minbitrate == 0 &&
             m.ffmpeg_options.mvectors == "Disabled" &&
             m.ffmpeg_options.qprd == false &&
             m.ffmpeg_options.trellis == false &&
                m.ffmpeg_options.cmp == 0)
                preset = CodecPresets.Default;

            //Turbo
            else if (m.ffmpeg_options.bdecision == "Disabled" &&
            m.ffmpeg_options.bframes == Format.GetMaxBFrames(m) &&
            m.ffmpeg_options.brefine == "Disabled" &&
            m.ffmpeg_options.cbp == false &&
            m.ffmpeg_options.mbd == "simple" &&
            m.ffmpeg_options.memethod == 0 &&
            m.ffmpeg_options.mvectors == "Disabled" &&
            m.ffmpeg_options.qprd == false &&
            m.ffmpeg_options.trellis == false &&
                m.ffmpeg_options.cmp == 0)
                preset = CodecPresets.Turbo;

            //Ultra
            else if (m.ffmpeg_options.bframes == Format.GetMaxBFrames(m) &&
            m.ffmpeg_options.cbp == false &&
            m.ffmpeg_options.mbd == "bits" &&
            m.ffmpeg_options.memethod == 2 &&
            m.ffmpeg_options.mvectors == "Unlimited" &&
            m.ffmpeg_options.qprd == false &&
            m.ffmpeg_options.trellis == true &&
                m.ffmpeg_options.cmp == 2)
                preset = CodecPresets.Ultra;

            combo_codec_preset.SelectedItem = preset.ToString();
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted)
            {
                //запоминаем старый режим
                oldmode = m.encodingmode;

                string XviDmode = combo_mode.SelectedItem.ToString();
                if (XviDmode == "1-Pass Bitrate")
                    m.encodingmode = Settings.EncodingModes.OnePass;

                else if (XviDmode == "2-Pass Bitrate")
                    m.encodingmode = Settings.EncodingModes.TwoPass;

                else if (XviDmode == "2-Pass Size")
                    m.encodingmode = Settings.EncodingModes.TwoPassSize;

                else if (XviDmode == "3-Pass Bitrate")
                    m.encodingmode = Settings.EncodingModes.ThreePass;

                else if (XviDmode == "3-Pass Size")
                    m.encodingmode = Settings.EncodingModes.ThreePassSize;

                else if (XviDmode == "Constant Quality")
                    m.encodingmode = Settings.EncodingModes.Quality;

                else if (XviDmode == "2-Pass Quality")
                    m.encodingmode = Settings.EncodingModes.TwoPassQuality;

                else if (XviDmode == "3-Pass Quality")
                    m.encodingmode = Settings.EncodingModes.ThreePassQuality;

                SetMinMaxBitrate();

                //сброс на квантайзер
                if (oldmode != Settings.EncodingModes.Quality &&
                    oldmode != Settings.EncodingModes.Quantizer &&
                    oldmode != Settings.EncodingModes.TwoPassQuality &&
                    oldmode != Settings.EncodingModes.ThreePassQuality)
                {
                    if (m.encodingmode == Settings.EncodingModes.Quality ||
                        m.encodingmode == Settings.EncodingModes.Quantizer ||
                        m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                        m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    {
                        SetDefaultBitrates();
                    }
                }

                //сброс на битрейт
                if (oldmode != Settings.EncodingModes.OnePass &&
                    oldmode != Settings.EncodingModes.TwoPass &&
                    oldmode != Settings.EncodingModes.ThreePass)
                {
                    if (m.encodingmode == Settings.EncodingModes.OnePass ||
                        m.encodingmode == Settings.EncodingModes.TwoPass ||
                        m.encodingmode == Settings.EncodingModes.ThreePass)
                    {
                        SetDefaultBitrates();
                    }
                }

                //сброс на размер
                if (oldmode != Settings.EncodingModes.TwoPassSize &&
                    oldmode != Settings.EncodingModes.ThreePassSize)
                {
                    if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassSize)
                    {
                        SetDefaultBitrates();
                    }
                }

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void SetDefaultBitrates()
        {
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.outvbitrate = 1;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
            }

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                m.outvbitrate = Calculate.GetAutoBitrate(m);
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.outvbitrate = m.infilesizeint;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
            }

            SetToolTips();
        }

        private void combo_me_method_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_me_method.IsDropDownOpen || combo_me_method.IsSelectionBoxHighlighted)
            {
                //m.ffmpeg_options.memethod = combo_me_method.SelectedIndex;
                if (Convert.ToString(combo_me_method.SelectedItem) == "Default Search")
                    m.ffmpeg_options.dia_size = 0;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "Sab Diamond Search")
                    m.ffmpeg_options.dia_size = -2;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "Funny Diamond Search")
                    m.ffmpeg_options.dia_size = -1;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "Small Diamond Search")
                    m.ffmpeg_options.dia_size = 2;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "L2s Diamond Search")
                    m.ffmpeg_options.dia_size = 257;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "HEX Search")
                    m.ffmpeg_options.dia_size = 513;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "UMH Search")
                    m.ffmpeg_options.dia_size = 769;
                else if (Convert.ToString(combo_me_method.SelectedItem) == "Full Search")
                    m.ffmpeg_options.dia_size = 1025;
                
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_qmatrix_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_qmatrix.IsDropDownOpen || combo_qmatrix.IsSelectionBoxHighlighted)
            {
                if (combo_qmatrix.SelectedItem.ToString() != "H263")
                {
                    m.ffmpeg_options.intermatrix = PresetLoader.GetInterMatrix(combo_qmatrix.SelectedItem.ToString());
                    m.ffmpeg_options.intramatrix = PresetLoader.GetIntraMatrix(combo_qmatrix.SelectedItem.ToString());
                }
                else
                {
                    m.ffmpeg_options.intermatrix = null;
                    m.ffmpeg_options.intramatrix = null;
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_trellis_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_trellis.IsFocused)
            {
                m.ffmpeg_options.trellis = check_trellis.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_trellis_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_trellis.IsFocused)
            {
                m.ffmpeg_options.trellis = check_trellis.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.bframes = Convert.ToInt32(combo_bframes.SelectedItem);
                if (m.ffmpeg_options.bframes == 0)
                {
                    combo_bdecision.SelectedItem = "Disabled";
                    m.ffmpeg_options.bdecision = "Disabled";
                    combo_brefine.SelectedItem = "Disabled";
                    m.ffmpeg_options.brefine = "Disabled";
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bdecision_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bdecision.IsDropDownOpen || combo_bdecision.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.bdecision = combo_bdecision.SelectedItem.ToString();
                if (m.ffmpeg_options.bframes == 0)
                {
                    m.ffmpeg_options.bframes = 1;
                    combo_bframes.SelectedItem = 1;
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_brefine_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_brefine.IsDropDownOpen || combo_brefine.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.brefine = combo_brefine.SelectedItem.ToString();
                if (m.ffmpeg_options.bframes == 0)
                {
                    m.ffmpeg_options.bframes = 1;
                    combo_bframes.SelectedItem = 1;
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_qprd_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_qprd.IsFocused)
            {
                m.ffmpeg_options.qprd = check_qprd.IsChecked.Value;

                combo_mbd.SelectedItem = "Rate distortion";
                m.ffmpeg_options.mbd = "rd";

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_qprd_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_qprd.IsFocused)
            {
                m.ffmpeg_options.qprd = check_qprd.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_cbp_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_cbp.IsFocused)
            {
                m.ffmpeg_options.cbp = check_cbp.IsChecked.Value;

                combo_mbd.SelectedItem = "Rate distortion";
                m.ffmpeg_options.mbd = "rd";

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_cbp_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_cbp.IsFocused)
            {
                m.ffmpeg_options.cbp = check_cbp.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_bitexact_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_bitexact.IsFocused)
            {
                m.ffmpeg_options.bitexact = check_bitexact.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_bitexact_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_bitexact.IsFocused)
            {
                m.ffmpeg_options.bitexact = check_bitexact.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_intra_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_intra.IsFocused)
            {
                m.ffmpeg_options.intra = check_intra.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_intra_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_intra.IsFocused)
            {
                m.ffmpeg_options.intra = check_intra.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_mbd_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mbd.IsDropDownOpen || combo_mbd.IsSelectionBoxHighlighted)
            {
                string mbd = combo_mbd.SelectedItem.ToString();
                if (mbd == "Simple")
                    m.ffmpeg_options.mbd = "simple";
                if (mbd == "Fewest bits")
                    m.ffmpeg_options.mbd = "bits";
                if (mbd == "Rate distortion")
                    m.ffmpeg_options.mbd = "rd";

                if (mbd != "Rate distortion")
                {
                    m.ffmpeg_options.cbp = false;
                    check_cbp.IsChecked = false;
                    m.ffmpeg_options.qprd = false;
                    check_qprd.IsChecked = false;
                }

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_fourcc_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_fourcc.IsDropDownOpen || combo_fourcc.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.fourcc_mpeg2 = combo_fourcc.SelectedItem.ToString();
                Settings.Mpeg2FOURCC = m.ffmpeg_options.fourcc_mpeg2;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_mvectors_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mvectors.IsDropDownOpen || combo_mvectors.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.mvectors = combo_mvectors.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void SetMinMaxBitrate()
        {
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                num_bitrate.DecimalPlaces = 0;
                num_bitrate.Change = 1;
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = Format.GetMaxVBitrate(m);
            }

            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                num_bitrate.DecimalPlaces = 1;
                num_bitrate.Change = 0.1m;
                num_bitrate.Minimum = 0;
                num_bitrate.Maximum = 31;
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                num_bitrate.DecimalPlaces = 0;
                num_bitrate.Change = 1;
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = 50000;
            }

            num_minbitrate.Minimum = 0;
            num_minbitrate.Maximum = Format.GetMaxVBitrate(m);

            num_maxbitrate.Minimum = 0;
            num_maxbitrate.Maximum = Format.GetMaxVBitrate(m);

            num_buffsize.Minimum = 0;
            num_buffsize.Maximum = 50000;

            num_bittolerance.Minimum = 0;
            num_bittolerance.Maximum = 50000;

            num_gopsize.Minimum = 0;
            num_gopsize.Maximum = 50000;
        }

        private void num_bitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bitrate.IsAction)
            {
                m.outvbitrate = num_bitrate.Value;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_minbitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_minbitrate.IsAction)
            {
                m.ffmpeg_options.minbitrate = (int)num_minbitrate.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_maxbitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_maxbitrate.IsAction)
            {
                m.ffmpeg_options.maxbitrate = (int)num_maxbitrate.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_gopsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_gopsize.IsAction)
            {
                m.ffmpeg_options.gopsize = (int)num_gopsize.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_buffsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_buffsize.IsAction)
            {
                m.ffmpeg_options.buffsize = (int)num_buffsize.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_bittolerance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bittolerance.IsAction)
            {
                m.ffmpeg_options.bittolerance = (int)num_bittolerance.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_cmp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_cmp.IsDropDownOpen || combo_cmp.IsSelectionBoxHighlighted)
            {
                m.ffmpeg_options.cmp = combo_cmp.SelectedIndex;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }
   

	}
}