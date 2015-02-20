using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using System.Globalization;
using System.Text;

namespace XviD4PSP
{
	public partial class FLV1
	{
        public Massive m;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum CodecPresets { Default = 1, Turbo, Ultra, Custom }
        private ArrayList good_cli = null;

        public FLV1(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
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
            combo_me_method.Items.Add("Small"); //0..1
            combo_me_method.Items.Add("Sab"); //-x..-2
            combo_me_method.Items.Add("Funny"); //-1
            combo_me_method.Items.Add("Var"); //2..256
            combo_me_method.Items.Add("L2s"); //257..512
            combo_me_method.Items.Add("HEX"); //513..768
            combo_me_method.Items.Add("UMH"); //769..1024
            combo_me_method.Items.Add("Full"); //1025..x

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

            //прогружаем матрицы квантизации
            combo_qmatrix.Items.Add("H263");
            foreach (string matrix in PresetLoader.CustomMatrixes(MatrixTypes.TXT))
                combo_qmatrix.Items.Add(matrix);

            //прогружаем fourcc
            combo_fourcc.Items.Add("");
            combo_fourcc.Items.Add("Default");
            combo_fourcc.Items.Add("FLV1");

            //прогружаем mbd
            combo_mbd.Items.Add("Simple");
            combo_mbd.Items.Add("Fewest bits");
            combo_mbd.Items.Add("Rate distortion");

            //пресеты настроек и качества
            foreach (string preset in Enum.GetNames(typeof(CodecPresets)))
                combo_codec_preset.Items.Add(preset);

            Apply_CLI.Content = Languages.Translate("Apply");
            Reset_CLI.Content = Languages.Translate("Reset");
            Help_CLI.Content = Languages.Translate("Help");
            Reset_CLI.ToolTip = "Reset to last good CLI";
            Help_CLI.ToolTip = "Show ffmpeg.exe -help full screen";

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            SetMinMaxBitrate();

            string mode = Calculate.EncodingModeEnumToString(m.encodingmode);
            if (!string.IsNullOrEmpty(mode))
            {
                if (!combo_mode.Items.Contains(mode))
                    combo_mode.Items.Add(mode);
                combo_mode.SelectedItem = mode;
            }
            else
            {
                m.encodingmode = Settings.EncodingModes.TwoPass;
                combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
                SetMinMaxBitrate();
                SetDefaultBitrates();
            }

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
            {
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }
            else if (m.encodingmode == Settings.EncodingModes.OnePassSize||
                m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }
            else
            {
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }

            //Dia method & size
            if (m.ffmpeg_options.dia_size == -1) //Funny (-1)
            {
                combo_me_method.SelectedIndex = 2;
                num_dia_size.IsEnabled = false;
                num_dia_size.Value = 1;
            }
            else if (m.ffmpeg_options.dia_size < -1) //Sab (-x..-2)
            {
                combo_me_method.SelectedIndex = 1;
                num_dia_size.IsEnabled = true;
                num_dia_size.Value = Math.Abs(m.ffmpeg_options.dia_size) - 1;
            }
            else if (m.ffmpeg_options.dia_size < 2) //Small (0..1)
            {
                combo_me_method.SelectedIndex = 0;
                num_dia_size.IsEnabled = false;
                num_dia_size.Value = 1;
            }
            else if (m.ffmpeg_options.dia_size > 1024) //Full (1025..x)
            {
                combo_me_method.SelectedIndex = 7;
                num_dia_size.IsEnabled = true;
                num_dia_size.Value = m.ffmpeg_options.dia_size - 1024;
            }
            else if (m.ffmpeg_options.dia_size > 768) //UMH (769..1024)
            {
                combo_me_method.SelectedIndex = 6;
                num_dia_size.IsEnabled = true;
                num_dia_size.Value = m.ffmpeg_options.dia_size - 768;
            }
            else if (m.ffmpeg_options.dia_size > 512) //HEX (513..768)
            {
                combo_me_method.SelectedIndex = 5;
                num_dia_size.IsEnabled = true;
                num_dia_size.Value = m.ffmpeg_options.dia_size - 512;
            }
            else if (m.ffmpeg_options.dia_size > 256) //L2s (257..512)
            {
                combo_me_method.SelectedIndex = 4;
                num_dia_size.IsEnabled = true;
                num_dia_size.Value = m.ffmpeg_options.dia_size - 256;
            }
            else //Var (2..256)
            {
                combo_me_method.SelectedIndex = 3;
                num_dia_size.IsEnabled = true;
                num_dia_size.Value = m.ffmpeg_options.dia_size - 1;
            }

            combo_cmp.SelectedIndex = m.ffmpeg_options.cmp;
            check_trellis.IsChecked = m.ffmpeg_options.trellis;
            check_gmc.IsChecked = m.ffmpeg_options.gmc;
            check_aic.IsChecked = m.ffmpeg_options.aic;
            check_qprd.IsChecked = m.ffmpeg_options.qprd;
            check_cbp.IsChecked = m.ffmpeg_options.cbp;
            if (!combo_fourcc.Items.Contains(m.ffmpeg_options.fourcc))
                combo_fourcc.Items.Add(m.ffmpeg_options.fourcc);
            combo_fourcc.SelectedItem = m.ffmpeg_options.fourcc;
            check_bitexact.IsChecked = m.ffmpeg_options.bitexact;

            num_minbitrate.Value = m.ffmpeg_options.minbitrate;
            num_maxbitrate.Value = m.ffmpeg_options.maxbitrate;
            num_buffsize.Value = m.ffmpeg_options.buffsize;
            num_bittolerance.Value = m.ffmpeg_options.bittolerance;
            num_gopsize.Value = m.ffmpeg_options.gopsize;
            check_closed_gop.IsChecked = m.ffmpeg_options.closedgop;
            check_enforce_gop.IsChecked = m.ffmpeg_options.enforce_gopsize;

            if (m.ffmpeg_options.mbd == "simple") combo_mbd.SelectedIndex = 0;
            else if (m.ffmpeg_options.mbd == "bits") combo_mbd.SelectedIndex = 1;
            else if (m.ffmpeg_options.mbd == "rd") combo_mbd.SelectedIndex = 2;

            check_mv0.IsChecked = m.ffmpeg_options.mv0;
            check_mv4.IsChecked = m.ffmpeg_options.mv4;

            if (m.ffmpeg_options.intramatrix != null || m.ffmpeg_options.intermatrix != null)
            {
                string setmatrix = "H263";
                foreach (string matrix in combo_qmatrix.Items)
                {
                    if (m.ffmpeg_options.intermatrix == PresetLoader.GetInterMatrix(matrix) /*&&
                        m.ffmpeg_options.intramatrix == PresetLoader.GetIntraMatrix(matrix)*/)
                        setmatrix = matrix;
                }
                combo_qmatrix.SelectedItem = setmatrix;
            }
            else
                combo_qmatrix.SelectedItem = "H263";

            //Выключаем элементы
            combo_fourcc.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-vtag ");
            num_maxbitrate.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-maxrate ");
            num_minbitrate.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-minrate ");
            num_buffsize.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-bufsize ");
            num_bittolerance.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-bt ");
            combo_me_method.IsEnabled = num_dia_size.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-dia_size ");
            combo_mbd.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-mbd ");
            combo_qmatrix.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-intra_matrix ") && !m.ffmpeg_options.extra_cli.Contains("-inter_matrix ");
            check_trellis.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-trellis ");
            combo_cmp.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-cmp ");
            num_gopsize.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("-g ");
            check_mv0.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+mv0");
            check_mv4.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+mv4");
            check_bitexact.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+bitexact");
            check_gmc.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+gmc");
            check_aic.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+aic");
            check_closed_gop.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+cgop");
            check_qprd.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+qp_rd");
            check_cbp.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+cbp_rd");
            check_enforce_gop.IsEnabled = !m.ffmpeg_options.extra_cli.Contains("+strict_gop");

            SetToolTips();
            DetectCodecPreset();
        }

        private void SetToolTips()
        {
            combo_mode.ToolTip = "Encoding mode";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.ToolTip = "Set bitrate (Default: Auto)";
            else if (m.encodingmode == Settings.EncodingModes.OnePassSize ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else
                num_bitrate.ToolTip = "Set target quality (Default: 3)";

            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                combo_codec_preset.ToolTip = "Default - reset codec settings to defaults" + Environment.NewLine +
                    "Turbo - fast encoding, big output file size" + Environment.NewLine +
                    "Ultra - high quality encoding, medium file size" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            else
            {
                combo_codec_preset.ToolTip = "Default - reset codec settings to defaults" + Environment.NewLine +
                    "Turbo - fast encoding, bad quality" + Environment.NewLine +
                    "Ultra - high quality encoding, optimal speed-quality solution" + Environment.NewLine +
                    "Custom - custom codec settings";
            }

            combo_qmatrix.ToolTip = "Use custom quantization matrix (-inter_matrix -intra_matrix, default: H263)";
            check_trellis.ToolTip = "Trellis quantization (-trellis, default: unchecked)";
            check_gmc.ToolTip = "Use global motion compensation (-flags +gmc, default: unchecked)";
            check_mv0.ToolTip = "Always try a macroblock with mv=<0,0> (-flags +mv0, default: unchecked)";
            check_mv4.ToolTip = "Use four motion vectors per macroblock (-flags +mv4, default: unchecked)";
            check_aic.ToolTip = "MPEG4 ac prediction (-flags +aic, default: unchecked)";
            check_qprd.ToolTip = "Use rate distortion optimization for qp selection (-mpv_flags +qp_rd, default: unchecked)";
            check_cbp.ToolTip = "Use rate distortion optimization for CBP (-mpv_flags +cbp_rd, default: unchecked)";
            combo_fourcc.ToolTip = combo_fourcc.Tag = "Force video tag/fourcc (-vtag, default: Default)\r\nWarning: incompatible values may break encoding!";
            check_bitexact.ToolTip = "Use only bitexact stuff, except dct (-flags +bitexact, default: unchecked)";
            combo_mbd.ToolTip = "Macroblock decision algorithm (-mbd, default: Simple)\r\nSimple - Fast\r\nFewest bits - Medium\r\nRate distortion - Best Quality";
            num_gopsize.ToolTip = "Set the group of picture size (-g, default: 12)\r\n1 = use only intra frames (highest quality encoding)";
            check_enforce_gop.ToolTip = "Strictly enforce gop size (-mpv_flags +strict_gop, default: unchecked)";
            check_closed_gop.ToolTip = "Closed GOP (-flags +cgop -sc_threshold 1000000000, default: unchecked)";
            num_minbitrate.ToolTip = "Set min video bitrate in kbps (-minrate, default: 0)";
            num_maxbitrate.ToolTip = "Set max video bitrate in kbps (-maxrate, default: 0)";
            num_buffsize.ToolTip = "Set ratecontrol buffer size (-bufsize, default: 0)";
            num_bittolerance.ToolTip = "Set video bitrate tolerance in kbps (-bt, default: 0)";
            combo_me_method.ToolTip = num_dia_size.ToolTip = "Set motion estimation method and search range (-pre_dia_size -dia_size, default Small)";
            combo_cmp.ToolTip = "Motion estimation compare function (-cmp -subcmp -mbcmp -ildctcmp -precmp -skipcmp, default: 0)" + Environment.NewLine +
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
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров ffmpeg
            m.ffmpeg_options = new ffmpeg_arguments();

            Settings.EncodingModes mode = new Settings.EncodingModes();

            //берём пока что за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            string value = "";
            string[] cli = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (int n = 0; n < cli.Length; n++)
            {
                value = cli[n];

                if (value == "-vcodec" || value == "-c:v")
                    m.ffmpeg_options.vcodec = cli[++n];

                else if (value == "-vtag" || value == "-tag:v")
                    m.ffmpeg_options.fourcc = cli[++n];

                else if (value == "-b:v" || value == "-b")
                {
                    m.outvbitrate = Convert.ToInt32(cli[++n]) / 1000;

                    if (m.vpasses.Count == 1) mode = Settings.EncodingModes.OnePass;
                    else if (m.vpasses.Count == 2) mode = Settings.EncodingModes.TwoPass;
                    else if (m.vpasses.Count == 3) mode = Settings.EncodingModes.ThreePass;
                }
                else if (value == "-sizemode")
                {
                    m.outvbitrate = Convert.ToInt32(cli[++n]) / 1000;

                    if (m.vpasses.Count == 1) mode = Settings.EncodingModes.OnePassSize;
                    else if (m.vpasses.Count == 2) mode = Settings.EncodingModes.TwoPassSize;
                    else if (m.vpasses.Count == 3) mode = Settings.EncodingModes.ThreePassSize;
                }
                else if (value == "-q:v" || value == "-qscale:v" || value == "-qscale")
                {
                    m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                    if (m.vpasses.Count == 1) mode = Settings.EncodingModes.Quality;
                    else if (m.vpasses.Count == 2) mode = Settings.EncodingModes.TwoPassQuality;
                    else if (m.vpasses.Count == 3) mode = Settings.EncodingModes.ThreePassQuality;
                }

                else if (value == "-maxrate")
                    m.ffmpeg_options.maxbitrate = Convert.ToInt32(cli[++n]) / 1000;

                else if (value == "-minrate")
                    m.ffmpeg_options.minbitrate = Convert.ToInt32(cli[++n]) / 1000;

                else if (value == "-bufsize")
                    m.ffmpeg_options.buffsize = Convert.ToInt32(cli[++n]) / 1000;

                else if (value == "-bt")
                    m.ffmpeg_options.bittolerance = Convert.ToInt32(cli[++n]) / 1000;

                else if (value == "-trellis")
                    m.ffmpeg_options.trellis = (Convert.ToInt32(cli[++n]) > 0);

                else if (value == "-mbd")
                {
                    string next = cli[++n];
                    if (next == "0") m.ffmpeg_options.mbd = "simple";
                    else if (next == "1") m.ffmpeg_options.mbd = "bits";
                    else if (next == "2") m.ffmpeg_options.mbd = "rd";
                    else m.ffmpeg_options.mbd = next;
                }

                else if (value == "-cmp")
                {
                    int val = 0;
                    string next = cli[++n];
                    if (Int32.TryParse(next, out val))
                    {
                        m.ffmpeg_options.cmp = val;
                    }
                    else
                    {
                        if (next == "sad") m.ffmpeg_options.cmp = 0;
                        else if (next == "sse") m.ffmpeg_options.cmp = 1;
                        else if (next == "satd") m.ffmpeg_options.cmp = 2;
                        else if (next == "dct") m.ffmpeg_options.cmp = 3;
                        else if (next == "psnr") m.ffmpeg_options.cmp = 4;
                        else if (next == "bit") m.ffmpeg_options.cmp = 5;
                        else if (next == "rd") m.ffmpeg_options.cmp = 6;
                        else if (next == "zero") m.ffmpeg_options.cmp = 7;
                        else if (next == "vsad") m.ffmpeg_options.cmp = 8;
                        else if (next == "vsse") m.ffmpeg_options.cmp = 9;
                        else if (next == "nsse") m.ffmpeg_options.cmp = 10;
                    }
                }

                else if (value == "-inter_matrix")
                    m.ffmpeg_options.intermatrix = cli[++n];

                else if (value == "-intra_matrix")
                    m.ffmpeg_options.intramatrix = cli[++n];

                else if (value == "-dia_size")
                    m.ffmpeg_options.dia_size = Convert.ToInt32(cli[++n]);

                else if (value == "-g")
                    m.ffmpeg_options.gopsize = Convert.ToInt32(cli[++n]);

                //дешифруем флаги
                else if (value == "-flags")
                {
                    string[] flags = cli[++n].Split(new string[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string flag in flags)
                    {
                        if (flag == "gmc")
                            m.ffmpeg_options.gmc = true;

                        else if (flag == "aic")
                            m.ffmpeg_options.aic = true;

                        else if (flag == "mv0")
                            m.ffmpeg_options.mv0 = true;

                        else if (flag == "mv4")
                            m.ffmpeg_options.mv4 = true;

                        else if (flag == "bitexact")
                            m.ffmpeg_options.bitexact = true;

                        else if (flag == "cgop")
                            m.ffmpeg_options.closedgop = true;
                    }
                }

                else if (value == "-mpv_flags")
                {
                    string[] flags = cli[++n].Split(new string[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string flag in flags)
                    {
                        if (flag == "qp_rd")
                            m.ffmpeg_options.qprd = true;

                        else if (flag == "cbp_rd")
                            m.ffmpeg_options.cbp = true;

                        else if (flag == "strict_gop")
                            m.ffmpeg_options.enforce_gopsize = true;
                    }
                }

                else if (value == "-extra:")
                {
                    for (int i = n + 1; i < cli.Length; i++)
                        m.ffmpeg_options.extra_cli += cli[i] + " ";

                    m.ffmpeg_options.extra_cli = m.ffmpeg_options.extra_cli.Trim();
                }
            }

            //Сброс на дефолт, если в CLI нет параметров кодирования
            if (mode == 0)
            {
                m.encodingmode = Settings.EncodingModes.OnePass;
                m.outvbitrate = 200;
            }
            else
                m.encodingmode = mode;

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //Определяем дефолты
            ffmpeg_arguments defaults = new ffmpeg_arguments();

            //обнуляем старые строки
            m.vpasses.Clear();

            string line = "-vcodec ", flags = "", flags2 = "";
            if (!string.IsNullOrEmpty(m.ffmpeg_options.vcodec))
                line += m.ffmpeg_options.vcodec;
            else
                line += "flv";

            if (m.ffmpeg_options.fourcc != defaults.fourcc && m.ffmpeg_options.fourcc != "Default" && !m.ffmpeg_options.extra_cli.Contains("-vtag "))
                line += " -vtag " + m.ffmpeg_options.fourcc;

            //битрейты
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line += " -b:v " + m.outvbitrate * 1000;
            else if (m.encodingmode == Settings.EncodingModes.OnePassSize ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                line += " -sizemode " + m.outvbitrate * 1000;
            else
                line += " -q:v " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1);

            if (m.ffmpeg_options.maxbitrate != defaults.maxbitrate && !m.ffmpeg_options.extra_cli.Contains("-maxrate "))
                line += " -maxrate " + m.ffmpeg_options.maxbitrate * 1000;

            if (m.ffmpeg_options.minbitrate != defaults.minbitrate && !m.ffmpeg_options.extra_cli.Contains("-minrate "))
                line += " -minrate " + m.ffmpeg_options.minbitrate * 1000;

            if (m.ffmpeg_options.buffsize != defaults.buffsize && !m.ffmpeg_options.extra_cli.Contains("-bufsize "))
                line += " -bufsize " + m.ffmpeg_options.buffsize * 1000;

            if (m.ffmpeg_options.bittolerance != defaults.bittolerance && !m.ffmpeg_options.extra_cli.Contains("-bt "))
                line += " -bt " + m.ffmpeg_options.bittolerance * 1000;

            if (m.ffmpeg_options.dia_size != defaults.dia_size && !m.ffmpeg_options.extra_cli.Contains("-dia_size "))
            {
                line += " -pre_dia_size " + m.ffmpeg_options.dia_size;
                line += " -dia_size " + m.ffmpeg_options.dia_size;
            }

            if (m.ffmpeg_options.mbd != defaults.mbd && !m.ffmpeg_options.extra_cli.Contains("-mbd "))
                line += " -mbd " + m.ffmpeg_options.mbd;

            if (m.ffmpeg_options.intramatrix != defaults.intramatrix && !string.IsNullOrEmpty(m.ffmpeg_options.intramatrix) && !m.ffmpeg_options.extra_cli.Contains("-intra_matrix "))
                line += " -intra_matrix " + m.ffmpeg_options.intramatrix;

            if (m.ffmpeg_options.intermatrix != defaults.intermatrix && !string.IsNullOrEmpty(m.ffmpeg_options.intermatrix) && !m.ffmpeg_options.extra_cli.Contains("-inter_matrix "))
                line += " -inter_matrix " + m.ffmpeg_options.intermatrix;

            if (m.ffmpeg_options.trellis != defaults.trellis && !m.ffmpeg_options.extra_cli.Contains("-trellis "))
                line += " -trellis " + ((m.ffmpeg_options.trellis) ? "1" : "0");

            //глобально прописываем cmp
            if (m.ffmpeg_options.cmp != defaults.cmp && !m.ffmpeg_options.extra_cli.Contains("-cmp "))
                line += " -cmp " + m.ffmpeg_options.cmp +
                    " -subcmp " + m.ffmpeg_options.cmp +
                    " -mbcmp " + m.ffmpeg_options.cmp +
                    " -ildctcmp " + m.ffmpeg_options.cmp +
                    " -precmp " + m.ffmpeg_options.cmp +
                    " -skipcmp " + m.ffmpeg_options.cmp;

            if (m.ffmpeg_options.gopsize != defaults.gopsize && !m.ffmpeg_options.extra_cli.Contains("-g "))
                line += " -g " + m.ffmpeg_options.gopsize;

            if (m.ffmpeg_options.mv0 && !defaults.mv0 && !m.ffmpeg_options.extra_cli.Contains("+mv0"))
                flags += "+mv0";

            if (m.ffmpeg_options.mv4 && !defaults.mv4 && !m.ffmpeg_options.extra_cli.Contains("+mv4"))
                flags += "+mv4";

            if (m.ffmpeg_options.bitexact && !defaults.bitexact && !m.ffmpeg_options.extra_cli.Contains("+bitexact"))
                flags += "+bitexact";

            if (m.ffmpeg_options.gmc && !defaults.gmc && !m.ffmpeg_options.extra_cli.Contains("+gmc"))
                flags += "+gmc";

            if (m.ffmpeg_options.aic && !defaults.aic && !m.ffmpeg_options.extra_cli.Contains("+aic"))
                flags += "+aic";

            if (m.ffmpeg_options.closedgop && !defaults.closedgop && !m.ffmpeg_options.extra_cli.Contains("+cgop"))
            {
                flags += "+cgop";
                line += " -sc_threshold 1000000000";
            }

            if (m.ffmpeg_options.qprd && !defaults.qprd && !m.ffmpeg_options.extra_cli.Contains("+qp_rd"))
                flags2 += "+qp_rd";

            if (m.ffmpeg_options.cbp && !defaults.cbp && !m.ffmpeg_options.extra_cli.Contains("+cbp_rd"))
                flags2 += "+cbp_rd";

            if (m.ffmpeg_options.enforce_gopsize && !defaults.enforce_gopsize && !m.ffmpeg_options.extra_cli.Contains("+strict_gop"))
                flags2 += "+strict_gop";

            //передаём массив флагов
            if (flags.Length > 0)
                line += " -flags " + flags;

            if (flags2.Length > 0)
                line += " -mpv_flags " + flags2;

            line += " -extra:";
            if (m.ffmpeg_options.extra_cli != defaults.extra_cli)
                line += " " + m.ffmpeg_options.extra_cli;

            //удаляем пустоту в начале
            line = line.TrimStart(new char[] { ' ' });

            //забиваем данные в массив
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                m.vpasses.Add(line);
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line);
            }
            else if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line);
                m.vpasses.Add(line);
            }

            return m;
        }

        private void combo_codec_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_codec_preset.IsDropDownOpen || combo_codec_preset.IsSelectionBoxHighlighted) && combo_codec_preset.SelectedItem != null)
            {
                CodecPresets preset = (CodecPresets)Enum.Parse(typeof(CodecPresets), combo_codec_preset.SelectedItem.ToString());

                if (preset == CodecPresets.Default)
                {
                    m.ffmpeg_options.vcodec = "flv";
                    m.ffmpeg_options.fourcc = "Default";
                    m.ffmpeg_options.aic = false;
                    m.ffmpeg_options.bitexact = false;
                    m.ffmpeg_options.bittolerance = 0;
                    m.ffmpeg_options.buffsize = 0;
                    m.ffmpeg_options.cbp = false;
                    m.ffmpeg_options.gmc = false;
                    m.ffmpeg_options.gopsize = 12;
                    m.ffmpeg_options.enforce_gopsize = false;
                    m.ffmpeg_options.closedgop = false;
                    m.ffmpeg_options.intermatrix = null;
                    m.ffmpeg_options.intramatrix = null;
                    m.ffmpeg_options.maxbitrate = 0;
                    m.ffmpeg_options.mbd = "simple";
                    m.ffmpeg_options.dia_size = 0;
                    m.ffmpeg_options.minbitrate = 0;
                    m.ffmpeg_options.mv0 = false;
                    m.ffmpeg_options.mv4 = false;
                    m.ffmpeg_options.qprd = false;
                    m.ffmpeg_options.trellis = false;
                    m.ffmpeg_options.cmp = 0;
                    //m.encodingmode = Settings.EncodingModes.OnePass;
                    SetDefaultBitrates();
                }
                else if (preset == CodecPresets.Turbo)
                {
                    m.ffmpeg_options.aic = false;
                    m.ffmpeg_options.cbp = false;
                    m.ffmpeg_options.gmc = false;
                    m.ffmpeg_options.mbd = "simple";
                    m.ffmpeg_options.dia_size = 0;
                    m.ffmpeg_options.mv0 = false;
                    m.ffmpeg_options.mv4 = false;
                    m.ffmpeg_options.qprd = false;
                    m.ffmpeg_options.trellis = false;
                    m.ffmpeg_options.cmp = 0;
                }
                else if (preset == CodecPresets.Ultra)
                {
                    m.ffmpeg_options.aic = true;
                    m.ffmpeg_options.cbp = false;
                    m.ffmpeg_options.gmc = Format.GetValidGMC(m);
                    m.ffmpeg_options.mbd = "bits";
                    m.ffmpeg_options.dia_size = 1028;
                    m.ffmpeg_options.mv0 = false;
                    m.ffmpeg_options.mv4 = true;
                    m.ffmpeg_options.qprd = false;
                    m.ffmpeg_options.trellis = true;
                    m.ffmpeg_options.cmp = 2;
                }
                else if (preset == CodecPresets.Custom)
                {
                    IList items = e.RemovedItems;
                    if (items != null && items.Count > 0)
                        combo_codec_preset.SelectedItem = items[0];
                    return;
                }

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.ffmpeg_options (это нужно для "-extra:")
                LoadFromProfile(); //Выставляем значения из m.ffmpeg_options в элементы управления
            }
        }

        public void UpdateCLI()
        {
            textbox_cli.Clear();
            foreach (string aa in m.vpasses)
                textbox_cli.Text += aa + "\r\n\r\n";
            good_cli = (ArrayList)m.vpasses.Clone(); //Клонируем CLI, не вызывающую ошибок
        }

        private void DetectCodecPreset()
        {
            CodecPresets preset = CodecPresets.Custom;

            //Default
            if ((string.IsNullOrEmpty(m.ffmpeg_options.vcodec) || m.ffmpeg_options.vcodec == "flv") &&
                m.ffmpeg_options.fourcc == "Default" &&
                m.ffmpeg_options.aic == false &&
                m.ffmpeg_options.bitexact == false &&
                m.ffmpeg_options.bittolerance == 0 &&
                m.ffmpeg_options.buffsize == 0 &&
                m.ffmpeg_options.cbp == false &&
                m.ffmpeg_options.gmc == false &&
                m.ffmpeg_options.gopsize == 12 &&
                m.ffmpeg_options.enforce_gopsize == false &&
                m.ffmpeg_options.closedgop == false &&
                m.ffmpeg_options.intermatrix == null &&
                m.ffmpeg_options.intramatrix == null &&
                m.ffmpeg_options.maxbitrate == 0 &&
                m.ffmpeg_options.mbd == "simple" &&
                m.ffmpeg_options.dia_size == 0 &&
                m.ffmpeg_options.minbitrate == 0 &&
                m.ffmpeg_options.mv0 == false &&
                m.ffmpeg_options.mv4 == false &&
                m.ffmpeg_options.qprd == false &&
                m.ffmpeg_options.trellis == false &&
                m.ffmpeg_options.cmp == 0)
                preset = CodecPresets.Default;

            //Turbo
            else if (m.ffmpeg_options.aic == false &&
                m.ffmpeg_options.cbp == false &&
                m.ffmpeg_options.gmc == false &&
                m.ffmpeg_options.mbd == "simple" &&
                m.ffmpeg_options.dia_size == 0 &&
                m.ffmpeg_options.mv0 == false &&
                m.ffmpeg_options.mv4 == false &&
                m.ffmpeg_options.qprd == false &&
                m.ffmpeg_options.trellis == false &&
                m.ffmpeg_options.cmp == 0)
                preset = CodecPresets.Turbo;

            //Ultra
            else if (m.ffmpeg_options.aic == true &&
                m.ffmpeg_options.cbp == false &&
                m.ffmpeg_options.gmc == Format.GetValidGMC(m) &&
                m.ffmpeg_options.mbd == "bits" &&
                m.ffmpeg_options.dia_size == 1028 &&
                m.ffmpeg_options.mv0 == false &&
                m.ffmpeg_options.mv4 == true &&
                m.ffmpeg_options.qprd == false &&
                m.ffmpeg_options.trellis == true &&
                m.ffmpeg_options.cmp == 2)
                preset = CodecPresets.Ultra;

            combo_codec_preset.SelectedItem = preset.ToString();
            UpdateCLI();
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted) && combo_mode.SelectedItem != null)
            {
                //запоминаем старый режим
                Settings.EncodingModes oldmode = m.encodingmode;

                m.encodingmode = Calculate.EncodingModeStringToEnum(combo_mode.SelectedItem.ToString());

                SetMinMaxBitrate();

                //Устанавливаем дефолтный битрейт (при необходимости)
                if (oldmode == Settings.EncodingModes.OnePass ||
                    oldmode == Settings.EncodingModes.TwoPass ||
                    oldmode == Settings.EncodingModes.ThreePass)
                {
                    if (m.encodingmode != Settings.EncodingModes.OnePass &&
                        m.encodingmode != Settings.EncodingModes.TwoPass &&
                        m.encodingmode != Settings.EncodingModes.ThreePass)
                        SetDefaultBitrates();
                }
                else if (oldmode.ToString().Contains("Size") && !m.encodingmode.ToString().Contains("Size"))
                {
                    SetDefaultBitrates();
                }
                else if (oldmode.ToString().Contains("Quality") && !m.encodingmode.ToString().Contains("Quality"))
                {
                    SetDefaultBitrates();
                }
                else if (oldmode == Settings.EncodingModes.Quantizer)
                {
                    SetDefaultBitrates();
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
                m.outvbitrate = 3;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
            }
            else if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                m.outvbitrate = Calculate.GetAutoBitrate(m);
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";
            }
            else if (m.encodingmode == Settings.EncodingModes.OnePassSize ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize ||
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
            if ((combo_me_method.IsDropDownOpen || combo_me_method.IsSelectionBoxHighlighted) & combo_me_method.SelectedIndex != -1)
            {
                if (combo_me_method.SelectedIndex == 0) //Small (0..1)
                {
                    m.ffmpeg_options.dia_size = 0;
                    num_dia_size.IsEnabled = false;
                    num_dia_size.Value = 1;
                }
                else if (combo_me_method.SelectedIndex == 1) //Sab (-x..-2)
                {
                    if (!num_dia_size.IsEnabled)
                        num_dia_size.Value = 1;

                    m.ffmpeg_options.dia_size = -1 - Convert.ToInt32(num_dia_size.Value);
                    num_dia_size.IsEnabled = true;
                }
                else if (combo_me_method.SelectedIndex == 2) //Funny (-1)
                {
                    m.ffmpeg_options.dia_size = -1;
                    num_dia_size.IsEnabled = false;
                    num_dia_size.Value = 1;
                }
                else if (combo_me_method.SelectedIndex == 3) //Var (2..256)
                {
                    if (!num_dia_size.IsEnabled)
                        num_dia_size.Value = 1;

                    m.ffmpeg_options.dia_size = 1 + Convert.ToInt32(num_dia_size.Value);
                    num_dia_size.IsEnabled = true;
                }
                else if (combo_me_method.SelectedIndex == 4) //L2s (257..512)
                {
                    if (!num_dia_size.IsEnabled)
                        num_dia_size.Value = 1;

                    m.ffmpeg_options.dia_size = 256 + Convert.ToInt32(num_dia_size.Value);
                    num_dia_size.IsEnabled = true;
                }
                else if (combo_me_method.SelectedIndex == 5) //HEX (513..768)
                {
                    if (!num_dia_size.IsEnabled)
                        num_dia_size.Value = 1;

                    m.ffmpeg_options.dia_size = 512 + Convert.ToInt32(num_dia_size.Value);
                    num_dia_size.IsEnabled = true;
                }
                else if (combo_me_method.SelectedIndex == 6) //UMH (769..1024)
                {
                    if (!num_dia_size.IsEnabled)
                        num_dia_size.Value = 1;

                    m.ffmpeg_options.dia_size = 768 + Convert.ToInt32(num_dia_size.Value);
                    num_dia_size.IsEnabled = true;
                }
                else if (combo_me_method.SelectedIndex == 7) //Full (1025..x)
                {
                    if (!num_dia_size.IsEnabled)
                        num_dia_size.Value = 1;

                    m.ffmpeg_options.dia_size = 1024 + Convert.ToInt32(num_dia_size.Value);
                    num_dia_size.IsEnabled = true;
                }

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_dia_size_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_dia_size.IsAction)
            {
                int value = Convert.ToInt32(e.OldValue - e.NewValue);

                if (m.ffmpeg_options.dia_size < 0)
                {
                    m.ffmpeg_options.dia_size += value;
                }
                else
                {
                    m.ffmpeg_options.dia_size -= value;
                }

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_qmatrix_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_qmatrix.IsDropDownOpen || combo_qmatrix.IsSelectionBoxHighlighted) && combo_qmatrix.SelectedItem != null)
            {
                if (combo_qmatrix.SelectedItem.ToString() != "H263")
                {
                    m.ffmpeg_options.intermatrix = PresetLoader.GetInterMatrix(combo_qmatrix.SelectedItem.ToString());
                    //использование intra вызывает артефакты в FLV
                    //m.ffmpeg_options.intramatrix = PresetLoader.GetIntraMatrix(combo_qmatrix.SelectedItem.ToString());
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

        private void check_trellis_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.trellis = check_trellis.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_gmc_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.gmc = check_gmc.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_aic_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.aic = check_aic.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_qprd_Click(object sender, RoutedEventArgs e)
        {
            if ((m.ffmpeg_options.qprd = check_qprd.IsChecked.Value))
            {
                combo_mbd.SelectedItem = "Rate distortion";
                m.ffmpeg_options.mbd = "rd";
            }
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_cbp_Click(object sender, RoutedEventArgs e)
        {
            if ((m.ffmpeg_options.cbp = check_cbp.IsChecked.Value))
            {
                combo_mbd.SelectedItem = "Rate distortion";
                m.ffmpeg_options.mbd = "rd";
            }
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_bitexact_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.bitexact = check_bitexact.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_mbd_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_mbd.IsDropDownOpen || combo_mbd.IsSelectionBoxHighlighted) && combo_mbd.SelectedItem != null)
            {
                string mbd = combo_mbd.SelectedItem.ToString();

                if (mbd == "Simple") m.ffmpeg_options.mbd = "simple";
                else if (mbd == "Fewest bits") m.ffmpeg_options.mbd = "bits";
                else if (mbd == "Rate distortion") m.ffmpeg_options.mbd = "rd";

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
            if ((combo_fourcc.IsDropDownOpen || combo_fourcc.IsSelectionBoxHighlighted || combo_fourcc.IsEditable) && combo_fourcc.SelectedItem != null)
            {
                if (combo_fourcc.SelectedIndex == 0)
                {
                    //Включаем редактирование
                    combo_fourcc.IsEditable = true;
                    combo_fourcc.ToolTip = Languages.Translate("Enter - apply, Esc - cancel.");
                    combo_fourcc.ApplyTemplate();
                    return;
                }
                else
                {
                    m.ffmpeg_options.fourcc = combo_fourcc.SelectedItem.ToString();
                    root_window.UpdateManualProfile();
                    DetectCodecPreset();
                }
            }

            if (combo_fourcc.IsEditable)
            {
                //Выключаем редактирование
                combo_fourcc.IsEditable = false;
                combo_fourcc.ToolTip = combo_fourcc.Tag;
            }
        }

        private void combo_fourcc_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            if (box.IsEditable && box.SelectedItem != null && !box.IsDropDownOpen && !box.IsMouseCaptured)
                combo_fourcc_KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter));
        }

        private void combo_fourcc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                //Проверяем введённый текст
                string text = combo_fourcc.Text.Trim();
                if (text.Length == 0 || Calculate.GetRegexValue(@"^([\x20-\x7E]{3,4})$", text) == null) //ASCII printable
                { combo_fourcc.SelectedItem = m.ffmpeg_options.fourcc; return; }

                //Добавляем и выбираем Item
                if (!combo_fourcc.Items.Contains(text))
                    combo_fourcc.Items.Add(text);
                combo_fourcc.SelectedItem = text;
            }
            else if (e.Key == Key.Escape)
            {
                //Возвращаем исходное значение
                combo_fourcc.SelectedItem = m.ffmpeg_options.fourcc;
            }
        }

        private void check_mv0_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.mv0 = check_mv0.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_mv4_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.mv4 = check_mv4.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
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
            else if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                num_bitrate.DecimalPlaces = 1;
                num_bitrate.Change = 0.1m;
                num_bitrate.Minimum = 0;
                num_bitrate.Maximum = 31;
            }
            else if (m.encodingmode == Settings.EncodingModes.OnePassSize ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize ||
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

        private void num_gopsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_gopsize.IsAction)
            {
                m.ffmpeg_options.gopsize = (int)num_gopsize.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_enforce_gop_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.enforce_gopsize = check_enforce_gop.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_closed_gop_Click(object sender, RoutedEventArgs e)
        {
            m.ffmpeg_options.closedgop = check_closed_gop.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_cmp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_cmp.IsDropDownOpen || combo_cmp.IsSelectionBoxHighlighted) && combo_cmp.SelectedIndex != -1)
            {
                m.ffmpeg_options.cmp = combo_cmp.SelectedIndex;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void button_Help_CLI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                help.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = " -help full";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                help.RedirectStandardError = true;
                help.StandardOutputEncoding = Encoding.UTF8;
                help.StandardErrorEncoding = Encoding.UTF8;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                //Именно в таком порядке (а по хорошему надо в отдельных потоках)
                string std_out = p.StandardOutput.ReadToEnd();
                string std_err = p.StandardError.ReadToEnd();
                new ShowWindow(root_window, "FFmpeg -help full", std_err + "\r\n" + std_out, new FontFamily("Lucida Console"));
            }
            catch (Exception ex)
            {
                new Message(root_window).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void button_Reset_CLI_Click(object sender, RoutedEventArgs e)
        {
            if (good_cli != null)
            {
                m.vpasses = (ArrayList)good_cli.Clone(); //- Восстанавливаем CLI до версии, не вызывавшей ошибок
                DecodeLine(m);                           //- Загружаем в массив m.ffmpeg значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                       //- Загружаем в форму значения, на основе значений массива m.ffmpeg
                root_window.m = this.m.Clone();          //- Передаем массив в основное окно
            }
            else
            {
                new Message(root_window).ShowMessage("Can't find good CLI...", Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void button_Apply_CLI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringReader sr = new StringReader(textbox_cli.Text);
                m.vpasses.Clear();
                string line = "";
                while (true)
                {
                    line = sr.ReadLine();
                    if (line == null) break;
                    if (line != "") m.vpasses.Add(line);
                }
                DecodeLine(m);                       //- Загружаем в массив m.ffmpeg значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                   //- Загружаем в форму значения, на основе значений массива m.xvid
                m.vencoding = "Custom FLV1 CLI";    //- Изменяем название пресета           
                PresetLoader.CreateVProfile(m);      //- Перезаписываем файл пресета (m.vpasses[x])
                root_window.m = this.m.Clone();      //- Передаем массив в основное окно
                root_window.LoadProfiles();          //- Обновляем название выбранного пресета в основном окне (Custom FLV1 CLI)
            }
            catch (Exception)
            {
                Message mm = new Message(root_window);
                mm.ShowMessage(Languages.Translate("Attention! Seems like CLI line contains errors!") + "\r\n" + Languages.Translate("Check all keys and theirs values and try again!") + "\r\n\r\n" +
                    Languages.Translate("OK - restore line (recommended)") + "\r\n" + Languages.Translate("Cancel - ignore (not recommended)"), Languages.Translate("Error"), Message.MessageStyle.OkCancel);
                if (mm.result == Message.Result.Ok)
                    button_Reset_CLI_Click(null, null);
            }
        }
	}
}