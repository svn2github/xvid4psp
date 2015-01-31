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
	public partial class XviD
	{
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum CodecPresets { Default = 1, Turbo, Ultra, Extreme, Custom }
        private ArrayList good_cli = null;

        public XviD(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
		{
			this.InitializeComponent();

            this.num_bitrate.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_bitrate_ValueChanged);

            this.m = mass.Clone();
            this.p = parent;
            this.root_window = VideoEncWindow;

            combo_mode.Items.Add("1-Pass Bitrate");
            combo_mode.Items.Add("2-Pass Bitrate");
            combo_mode.Items.Add("3-Pass Bitrate");
            combo_mode.Items.Add("Constant Quality");
            //combo_mode.Items.Add("2-Pass Quality");
            combo_mode.Items.Add("3-Pass Quality");
            combo_mode.Items.Add("2-Pass Size");
            combo_mode.Items.Add("3-Pass Size");

            //Прописываем Motion Search Precision
            combo_quality.Items.Add("0 - Disabled");
            combo_quality.Items.Add("1 - Very Low");
            combo_quality.Items.Add("2 - Low");
            combo_quality.Items.Add("3 - Medium");
            combo_quality.Items.Add("4 - High");
            combo_quality.Items.Add("5 - Very High");
            combo_quality.Items.Add("6 - Ultra High");

            combo_vhqmode.Items.Add("0 - Disabled");
            combo_vhqmode.Items.Add("1 - Mode Decision");
            combo_vhqmode.Items.Add("2 - Limited Search");
            combo_vhqmode.Items.Add("3 - Medium Search");
            combo_vhqmode.Items.Add("4 - Wide Search");

            combo_metric.Items.Add("0 - PSNR");
            combo_metric.Items.Add("1 - PSNR_HVSM");

            //прогружаем матрицы квантизации
            combo_qmatrix.Items.Add("H263");
            combo_qmatrix.Items.Add("MPEG");
            foreach (string matrix in PresetLoader.CustomMatrixes(MatrixTypes.CQM))
                combo_qmatrix.Items.Add(matrix);

            for (int i = 1; i < 32; i++)
            {
                combo_imin.Items.Add(i);
                combo_imax.Items.Add(i);
                combo_pmin.Items.Add(i);
                combo_pmax.Items.Add(i);
                combo_bmin.Items.Add(i);
                combo_bmax.Items.Add(i);
            }

            //прогружаем fourcc
            combo_fourcc.Items.Add("XVID");
            combo_fourcc.Items.Add("FFDS");
            combo_fourcc.Items.Add("FVFW");
            combo_fourcc.Items.Add("DX50");
            combo_fourcc.Items.Add("DIVX");
            combo_fourcc.Items.Add("MP4V");

            combo_masking.Items.Add("None");
            combo_masking.Items.Add("Lumi");
            combo_masking.Items.Add("Variance");

            //B фреймы
            for (int n = 0; n <= 4; n++) combo_bframes.Items.Add(n);

            combo_threads.Items.Add("Auto");
            for (int i = 1; i < 13; i++) combo_threads.Items.Add(i);

            //пресеты настроек и качества
            foreach (string preset in Enum.GetNames(typeof(CodecPresets)))
                combo_codec_preset.Items.Add(preset);

            text_mode.Content = Languages.Translate("Encoding mode") + ":";
            Apply_CLI.Content = Languages.Translate("Apply");
            Reset_CLI.Content = Languages.Translate("Reset");
            xvid_help.Content = Languages.Translate("Help");
            Reset_CLI.ToolTip = "Reset to last good CLI";
            xvid_help.ToolTip = "Show xvid_encraw.exe -help screen";

            LoadFromProfile();
		}

        public void LoadFromProfile()
        {
            SetMinMaxBitrate();

            combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
            if (combo_mode.SelectedItem == null)
            {
                m.encodingmode = Settings.EncodingModes.TwoPass;
                combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
                SetMinMaxBitrate();
                SetDefaultBitrates();
            }

            //запоминаем первичные режим кодирования
            oldmode = m.encodingmode;

            //защита для гладкой смены кодеков
            if (m.outvbitrate > 10000)
                m.outvbitrate = 10000;
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
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }
            else
            {
                if (m.encodingmode == Settings.EncodingModes.Quantizer)
                    text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                else
                    text_bitrate.Content = Languages.Translate("Quality") + ": (Q)";

                num_bitrate.Value = (decimal)m.outvbitrate;
            }

            if (m.XviD_options.bframes == 0)
            {
                check_bvhq.IsEnabled = false;
                check_bvhq.IsChecked = false;
                m.XviD_options.bvhq = false;
            }
            else
                check_bvhq.IsEnabled = true;

            combo_quality.SelectedIndex = m.XviD_options.quality;
            combo_vhqmode.SelectedIndex = m.XviD_options.vhqmode;
            combo_metric.SelectedIndex = m.XviD_options.metric;
            check_chroma.IsChecked = m.XviD_options.chromame;
            combo_qmatrix.SelectedItem = m.XviD_options.qmatrix;
            check_trellis.IsChecked = m.XviD_options.trellis;
            check_fullpass.IsChecked = m.XviD_options.full_first_pass;
            check_grayscale.IsChecked = m.XviD_options.gray;
            check_cartoon.IsChecked = m.XviD_options.cartoon;
            check_chroma_opt.IsChecked = m.XviD_options.chroma_opt;
            check_packedmode.IsChecked = m.XviD_options.packedmode;
            check_gmc.IsChecked = m.XviD_options.gmc;
            check_qpel.IsChecked = m.XviD_options.qpel;
            check_bvhq.IsChecked = m.XviD_options.bvhq;
            check_closedgop.IsChecked = m.XviD_options.closedgop;
            combo_bframes.SelectedItem = m.XviD_options.bframes;
            num_bquant_ratio.Value = m.XviD_options.b_ratio;
            num_bquant_offset.Value = m.XviD_options.b_offset;
            num_keyint.Value = m.XviD_options.keyint;
            combo_masking.SelectedIndex = m.XviD_options.masking;
            combo_fourcc.SelectedItem = m.XviD_options.fourcc;
            combo_imin.SelectedItem = m.XviD_options.imin;
            combo_imax.SelectedItem = m.XviD_options.imax;
            combo_pmin.SelectedItem = m.XviD_options.pmin;
            combo_pmax.SelectedItem = m.XviD_options.pmax;
            combo_bmin.SelectedItem = m.XviD_options.bmin;
            combo_bmax.SelectedItem = m.XviD_options.bmax;
            check_xvid_new.IsChecked = Settings.UseXviD_130;
            combo_threads.SelectedIndex = Settings.XviD_Threads;
            num_kboost.Value = m.XviD_options.kboost;
            num_chigh.Value = m.XviD_options.chigh;
            num_clow.Value = m.XviD_options.clow;
            num_ostrength.Value = m.XviD_options.ostrength;
            num_oimprove.Value = m.XviD_options.oimprove;
            num_odegrade.Value = m.XviD_options.odegrade;
            num_reaction.Value = m.XviD_options.reaction;
            num_averaging.Value = m.XviD_options.averaging;
            num_smoother.Value = m.XviD_options.smoother;
            num_vbvmax.Value = m.XviD_options.vbvmax;
            num_vbvsize.Value = m.XviD_options.vbvsize;
            num_vbvpeak.Value = m.XviD_options.vbvpeak;
            num_firstpass_q.Value = m.XviD_options.firstpass_q;

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
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                     m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InputFileSize)";
            else
                num_bitrate.ToolTip = "Set target quality (Default: 3)";

            combo_quality.ToolTip = "Motion search quality (-quality, default: 6)";
            combo_vhqmode.ToolTip = "Level of R-D optimizations (-vhqmode, default: 1)";
            combo_metric.ToolTip = "Distortion metric for R-D optimizations (-metric, default: 0)\r\nOnly for XviD 1.3.x";
            check_chroma.ToolTip = "Chroma motion estimation (-nochromame if unchecked, default: checked)";
            combo_qmatrix.ToolTip = "Use custom MPEG4 quantization matrix (-qmatrix, default: H263)";
            check_trellis.ToolTip = "Trellis quantization (-notrellis if unchecked, default: checked)";
            check_grayscale.ToolTip = "Grayscale encoding (Default: Disabled)";
            check_cartoon.ToolTip = "Cartoon mode (Default: Disabled)";
            check_chroma_opt.ToolTip = "Chroma optimizer (Default: Disabled)";
            check_packedmode.ToolTip = "Packed mode (-nopacked if unchecked, default: checked).\r\nNot compatible with multi-passes encoding!";
            check_gmc.ToolTip = "Use global motion compensation (-gmc if checked, default: unchecked)";
            check_qpel.ToolTip = "Use quarter pixel ME (-qpel if checked, default: unchecked)";
            check_bvhq.ToolTip = "Use R-D optimizations for B-frames (-bvhq if checked, default: unchecked)";
            check_closedgop.ToolTip = "Closed GOP mode (-noclosed_gop if unchecked, default: checked)";
            combo_bframes.ToolTip = "Max bframes (-max_bframes, default: 2)";
            num_bquant_ratio.ToolTip = "B-frames quantizer ratio (-bquant_ratio, default: 150)";
            num_bquant_offset.ToolTip = "B-frames quantizer offset (-bquant_offset, default: 100)";
            num_keyint.ToolTip = "Maximum keyframe interval (-max_key_interval, default: 300)";
            combo_masking.ToolTip = "HVS masking mode\r\n0 - None (default)\r\n1 - Lumi (-lumimasking for XviD 1.2.2," +
                " -masking 1 for XviD 1.3.x)\r\n2 - Variance (-lumimasking for XviD 1.2.2, -masking 2 for XviD 1.3.x)";
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                combo_codec_preset.ToolTip = "Default - set codec settings to defaults" + Environment.NewLine +
                    "Turbo - fast encoding, big output file size" + Environment.NewLine +
                    "Ultra - high quality encoding, medium file size" + Environment.NewLine +
                    "Extreme - super high quality encoding, smallest file size" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            else
            {
                combo_codec_preset.ToolTip = "Default - set codec settings to defaults" + Environment.NewLine +
                    "Turbo - fast encoding, bad quality" + Environment.NewLine +
                    "Ultra - high quality encoding, optimal speed-quality solution" + Environment.NewLine +
                    "Extreme - super high quality encoding, very slow" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            combo_fourcc.ToolTip = "Force video tag/fourcc (Default: XVID)";
            combo_imin.ToolTip = "Minimum quantizer for I-frames (-imin, default: 2)";
            combo_imax.ToolTip = "Maximum quantizer for I-frames (-imax, default: 31)";
            combo_pmin.ToolTip = "Minimum quantizer for P-frames (-pmin, default: 2)";
            combo_pmax.ToolTip = "Maximum quantizer for P-frames (-pmax, default: 31)";
            combo_bmin.ToolTip = "Minimum quantizer for B-frames (-bmin, default: 2)";
            combo_bmax.ToolTip = "Maximum quantizer for B-frames (-bmax, default: 31)";
            combo_threads.ToolTip = "Set number of threads for encoding (-threads, Auto (default) = CPU count + 2).\r\nThis is a global option.";
            check_fullpass.ToolTip = "Perform full (i.e. slow) first pass (-full1pass if checked, default: unchecked)";
            num_kboost.ToolTip = "I frame boost (-kboost, default: 10)";
            num_chigh.ToolTip = "High bitrate scenes degradation (-chigh, default: 0)";
            num_clow.ToolTip = "Low bitrate scenes improvement (-clow, default: 0)";
            num_ostrength.ToolTip = "Overflow control strength (-ostrength, default: 5)";
            num_oimprove.ToolTip = "Max overflow improvement (-oimprove, default: 5)";
            num_odegrade.ToolTip = "Max overflow degradation (-odegrade, default: 5)";
            num_reaction.ToolTip = "Reaction delay factor (-reaction, default: 16)";
            num_averaging.ToolTip = "Averaging period (-averaging, default: 100)";
            num_smoother.ToolTip = "Smoothing buffer (-smoother, default: 100)";
            num_vbvsize.ToolTip = "Use VBV buffer size (-vbvsize, default: 0)";
            num_vbvmax.ToolTip = "VBV max bitrate (-vbvmax, default: 0)";
            num_vbvpeak.ToolTip = "VBV peak bitrate over 1 second (-vbvpeak, default: 0)";
            check_xvid_new.ToolTip = "Enable this option if you want to use version 1.3.x for encoding. By default, version 1.2.2 is used.\r\nThis is a global option.";
            num_firstpass_q.ToolTip = "Redefine quantizer for the 1-st pass of multi-passes encoding, default: 2.0\r\nChange it only if you know what you're doing.";
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров XviD
            m.XviD_options = new XviD_arguments();

            //для начала определяем колличество проходов
            Settings.EncodingModes mode = Settings.EncodingModes.OnePass;
            if (m.vpasses.Count == 3)
                mode = Settings.EncodingModes.ThreePass;
            else if (m.vpasses.Count == 2)
                mode = Settings.EncodingModes.TwoPass;

            //Определяем нестандартный первый проход
            if (m.vpasses.Count > 1)
            {
                decimal res_q = 0;
                string zonesq = Calculate.GetRegexValue(@"\-zones\s0,q,(\d{1,2}\.?\d?)", m.vpasses[0].ToString());
                if (decimal.TryParse(zonesq, NumberStyles.Any, new CultureInfo("en-US"), out res_q))
                    m.XviD_options.firstpass_q = res_q;
            }

            //берём пока что за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            string value = "";
            string[] cli = line.Split(new string[] { " " }, StringSplitOptions.None);
            for (int n = 0; n < cli.Length; n++)
            {
                value = cli[n];

                if (value == "-bitrate")
                    m.outvbitrate = Convert.ToInt32(cli[++n]);

                if (value == "-size")
                {
                    m.outvbitrate = Convert.ToInt32(cli[++n]) / 1000;
                    if (m.vpasses.Count == 3)
                        mode = Settings.EncodingModes.ThreePassSize;
                    else if (m.vpasses.Count == 2)
                        mode = Settings.EncodingModes.TwoPassSize;
                }

                if (m.vpasses.Count == 1)
                {
                    if (value == "-cq")
                    {
                        mode = Settings.EncodingModes.Quality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                }

                if (m.vpasses.Count == 2)
                {
                    if (value == "-cq")
                    {
                        mode = Settings.EncodingModes.TwoPassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                }

                if (m.vpasses.Count == 3)
                {
                    if (value == "-cq")
                    {
                        mode = Settings.EncodingModes.ThreePassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                }

                if (value == "-quality")
                    m.XviD_options.quality = Convert.ToInt32(cli[++n]);

                else if (value == "-nochromame")
                    m.XviD_options.chromame = false;

                else if (value == "-qtype")
                {
                    int qtype = Convert.ToInt32(cli[++n]);
                    if (qtype == 0)
                        m.XviD_options.qmatrix = "H263";
                    if (qtype == 1)
                        m.XviD_options.qmatrix = "MPEG";
                }

                else if (value == "-qmatrix")
                {
                    string qm_path = Calculate.GetRegexValue(@"\-qmatrix\s""(.+)""", line);
                    if (qm_path != null)
                    {
                        string q_matrix = Path.GetFileNameWithoutExtension(qm_path);
                        if (File.Exists(Calculate.StartupPath + "\\presets\\matrix\\cqm\\" + q_matrix + ".cqm"))
                            m.XviD_options.qmatrix = q_matrix;
                    }
                }

                else if (value == "-notrellis")
                    m.XviD_options.trellis = false;

                else if (value == "-vhqmode")
                    m.XviD_options.vhqmode = Convert.ToInt32(cli[++n]);

                else if (value == "-metric")
                    m.XviD_options.metric = Convert.ToInt32(cli[++n]);

                else if (value == "-zones")
                {
                    string zone = cli[++n];
                    m.XviD_options.gray = zone.Contains("G");
                    m.XviD_options.cartoon = zone.Contains("C");
                    m.XviD_options.chroma_opt = zone.Contains("O");
                }

                else if (value == "-nopacked")
                    m.XviD_options.packedmode = false;

                else if (value == "-gmc")
                    m.XviD_options.gmc = true;

                else if (value == "-qpel")
                    m.XviD_options.qpel = true;

                else if (value == "-bvhq")
                    m.XviD_options.bvhq = true;

                else if (value == "-noclosed_gop")
                    m.XviD_options.closedgop = false;

                else if (value == "-max_bframes")
                    m.XviD_options.bframes = Convert.ToInt32(cli[++n]);

                else if (value == "-lumimasking")
                    m.XviD_options.masking = 2;

                else if (value == "-masking")
                    m.XviD_options.masking = Convert.ToInt32(cli[++n]);

                else if (value == "-fourcc")
                    m.XviD_options.fourcc = cli[++n];

                else if (value == "-max_key_interval")
                    m.XviD_options.keyint = Convert.ToInt32(cli[++n]);

                else if (value == "-bquant_ratio")
                    m.XviD_options.b_ratio = Convert.ToInt32(cli[++n]);

                else if (value == "-bquant_offset")
                    m.XviD_options.b_offset = Convert.ToInt32(cli[++n]);

                else if (value == "-reaction")
                    m.XviD_options.reaction = Convert.ToInt32(cli[++n]);

                else if (value == "-averaging")
                    m.XviD_options.averaging = Convert.ToInt32(cli[++n]);

                else if (value == "-smoother")
                    m.XviD_options.smoother = Convert.ToInt32(cli[++n]);

                else if (value == "-kboost")
                    m.XviD_options.kboost = Convert.ToInt32(cli[++n]);

                else if (value == "-ostrength")
                    m.XviD_options.ostrength = Convert.ToInt32(cli[++n]);

                else if (value == "-oimprove")
                    m.XviD_options.oimprove = Convert.ToInt32(cli[++n]);

                else if (value == "-odegrade")
                    m.XviD_options.odegrade = Convert.ToInt32(cli[++n]);

                else if (value == "-chigh")
                    m.XviD_options.chigh = Convert.ToInt32(cli[++n]);

                else if (value == "-clow")
                    m.XviD_options.clow = Convert.ToInt32(cli[++n]);

                else if (value == "-overhead")
                    m.XviD_options.overhead = Convert.ToInt32(cli[++n]);

                else if (value == "-vbvmax")
                    m.XviD_options.vbvmax = Convert.ToInt32(cli[++n]);

                else if (value == "-vbvsize")
                    m.XviD_options.vbvsize = Convert.ToInt32(cli[++n]);

                else if (value == "-vbvpeak")
                    m.XviD_options.vbvpeak = Convert.ToInt32(cli[++n]);

                else if (value == "-imin")
                {
                    m.XviD_options.mins += 1;
                    m.XviD_options.imin = Convert.ToInt32(cli[++n]);
                }

                else if (value == "-pmin")
                {
                    m.XviD_options.mins += 1;
                    m.XviD_options.pmin = Convert.ToInt32(cli[++n]);
                }

                else if (value == "-bmin")
                {
                    m.XviD_options.mins += 1;
                    m.XviD_options.bmin = Convert.ToInt32(cli[++n]);
                }

                else if (value == "-imax")
                    m.XviD_options.imax = Convert.ToInt32(cli[++n]);

                else if (value == "-pmax")
                    m.XviD_options.pmax = Convert.ToInt32(cli[++n]);

                else if (value == "-bmax")
                    m.XviD_options.bmax = Convert.ToInt32(cli[++n]);

                else if (value == "-full1pass")
                    m.XviD_options.full_first_pass = true;
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
            string line = "";

            if (m.XviD_options.quality != 6)
                line += " -quality " + m.XviD_options.quality;

            if (!m.XviD_options.chromame)
                line += " -nochromame";

            if (m.XviD_options.qmatrix == "H263")
                line += " -qtype 0";
            else if (m.XviD_options.qmatrix == "MPEG")
                line += " -qtype 1";
            else
                line += " -qmatrix \"" + Calculate.StartupPath + "\\presets\\matrix\\cqm\\" + m.XviD_options.qmatrix + ".cqm\"";

            if (!m.XviD_options.trellis)
                line += " -notrellis";

            if (m.XviD_options.vhqmode != 1)
                line += " -vhqmode " + m.XviD_options.vhqmode;

            if (m.XviD_options.metric != 0)
                line += " -metric " + m.XviD_options.metric;

            if (m.XviD_options.gray || m.XviD_options.cartoon || m.XviD_options.chroma_opt)
            {
                line += " -zones 0,w,1.0,";
                if (m.XviD_options.gray) line += "G";
                if (m.XviD_options.cartoon) line += "C";
                if (m.XviD_options.chroma_opt) line += "O";
            }

            if (!m.XviD_options.packedmode)
                line += " -nopacked";

            if (m.XviD_options.gmc)
                line += " -gmc";

            if (m.XviD_options.qpel)
                line += " -qpel";

            if (m.XviD_options.bvhq)
                line += " -bvhq";

            if (!m.XviD_options.closedgop)
                line += " -noclosed_gop";

            if (m.XviD_options.bframes != 2)
                line += " -max_bframes " + m.XviD_options.bframes;

            if (m.XviD_options.b_ratio != 150)
                line += " -bquant_ratio " + m.XviD_options.b_ratio;

            if (m.XviD_options.b_offset != 100)
                line += " -bquant_offset " + m.XviD_options.b_offset;

            if (m.XviD_options.masking == 1)
                line += Settings.UseXviD_130 ? " -masking 1" : " -lumimasking";
            else if (m.XviD_options.masking == 2)
                line += Settings.UseXviD_130 ? " -masking 2" : " -lumimasking";

            if (m.XviD_options.fourcc != "XVID")
                line += " -fourcc " + m.XviD_options.fourcc;

            if (m.XviD_options.keyint != 300)
                line += " -max_key_interval " + m.XviD_options.keyint;

            if (m.XviD_options.reaction != 16)
                line += " -reaction " + m.XviD_options.reaction;

            if (m.XviD_options.averaging != 100)
                line += " -averaging " + m.XviD_options.averaging;

            if (m.XviD_options.smoother != 100)
                line += " -smoother " + m.XviD_options.smoother;

            if (m.XviD_options.full_first_pass)
                line += " -full1pass";

            //передаём параметры в турбо строку
            line_turbo = line;

            //Восстанавливаем нестандартный первый проход
            if (m.XviD_options.firstpass_q > 0 && m.XviD_options.firstpass_q != 2)
            {
                //Меняем\добавляем ключ -zones 0,q
                if (m.XviD_options.gray || m.XviD_options.cartoon || m.XviD_options.chroma_opt)
                    line_turbo = line_turbo.Replace("-zones 0,w,1.0", "-zones 0,q," + m.XviD_options.firstpass_q.ToString("F1", new CultureInfo("en-US")));
                else
                    line_turbo += " -zones 0,q," + m.XviD_options.firstpass_q.ToString("F1", new CultureInfo("en-US"));

                //-zones 0,q сбивает настройки первого прохода на full 1-st pass (-zones 0,w этого не делает)
                //xvidcore\src\plugins\plugin_2pass1.c #ifdef FAST1PASS
                if (!m.XviD_options.full_first_pass)
                {
                    //-quality 5 (6)
                    if (m.XviD_options.quality != 6)
                        line_turbo = line_turbo.Replace("-quality " + m.XviD_options.quality, "-quality " + Math.Max(0, m.XviD_options.quality - 1));
                    else
                        line_turbo += " -quality " + Math.Max(0, m.XviD_options.quality - 1);

                    //-vhqmode 1 (4)
                    if (m.XviD_options.vhqmode != 1)
                        line_turbo = line_turbo.Replace("-vhqmode " + m.XviD_options.vhqmode, "-vhqmode " + (m.XviD_options.vhqmode >= 1 ? "1" : "0"));
                    else
                        line_turbo += " -vhqmode " + (m.XviD_options.vhqmode >= 1 ? "1" : "0");

                    //-bvhq
                    if (m.XviD_options.bvhq)
                        line_turbo = line_turbo.Replace(" -bvhq", "");

                    //-notrellis
                    line_turbo += (m.XviD_options.trellis) ? " -notrellis" : "";

                    //-nochromame
                    line_turbo += (m.XviD_options.chromame) ? " -nochromame" : "";

                    //-turbo
                    line_turbo += " -turbo";
                }
            }

            //Битрейт\Квантизер\Размер
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line = "-bitrate " + m.outvbitrate + line;
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                line = "-size " + (int)m.outvbitrate * 1000 + line;
            else
                line = "-cq " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + line;

            //Опции многопроходного кодирования
            if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize)
            {
                if (m.XviD_options.kboost != 10)
                    line += " -kboost " + m.XviD_options.kboost;

                if (m.XviD_options.ostrength != 5)
                    line += " -ostrength " + m.XviD_options.ostrength;

                if (m.XviD_options.oimprove != 5)
                    line += " -oimprove " + m.XviD_options.oimprove;

                if (m.XviD_options.odegrade != 5)
                    line += " -odegrade " + m.XviD_options.odegrade;

                if (m.XviD_options.chigh != 0)
                    line += " -chigh " + m.XviD_options.chigh;

                if (m.XviD_options.clow != 0)
                    line += " -clow " + m.XviD_options.clow;

                if (m.XviD_options.overhead != 24)
                    line += " -overhead " + m.XviD_options.overhead;

                if (m.XviD_options.vbvmax != 0)
                    line += " -vbvmax " + m.XviD_options.vbvmax;

                if (m.XviD_options.vbvsize != 0)
                    line += " -vbvsize " + m.XviD_options.vbvsize;

                if (m.XviD_options.vbvpeak != 0)
                    line += " -vbvpeak " + m.XviD_options.vbvpeak;
            }

            //Ограничения q
            string q_limits = "";

            if (m.XviD_options.imin != 2 || m.XviD_options.mins > 0)
                q_limits += " -imin " + m.XviD_options.imin;

            if (m.XviD_options.imax != 31)
                q_limits += " -imax " + m.XviD_options.imax;

            if (m.XviD_options.pmin != 2 || m.XviD_options.mins > 0)
                q_limits += " -pmin " + m.XviD_options.pmin;

            if (m.XviD_options.pmax != 31)
                q_limits += " -pmax " + m.XviD_options.pmax;

            if (m.XviD_options.bmin != 2 || m.XviD_options.mins > 0)
                q_limits += " -bmin " + m.XviD_options.bmin;

            if (m.XviD_options.bmax != 31)
                q_limits += " -bmax " + m.XviD_options.bmax;

            //удаляем пустоту в начале
            line = line.TrimStart(new char[] { ' ' });
            line_turbo = line_turbo.TrimStart(new char[] { ' ' });

            //забиваем данные в массив
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer)
            {
                m.vpasses.Add(line + q_limits);
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize)
            {
                m.vpasses.Add(line_turbo);
                if (q_limits.Length > 0)
                    m.vpasses.Add(line + q_limits);
                else
                    m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }
            else if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.vpasses.Add(line_turbo);
                if (q_limits.Length > 0)
                {
                    m.vpasses.Add(line + q_limits);
                    m.vpasses.Add(line + q_limits);
                }
                else
                {
                    m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
                    m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
                }
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
            {
                m.vpasses.Add(line);
                if (q_limits.Length > 0)
                    m.vpasses.Add(line + q_limits);
                else
                    m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }
            else if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line);
                if (q_limits.Length > 0)
                    m.vpasses.Add(line + q_limits);
                else
                    m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }

            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted) && combo_mode.SelectedItem != null)
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

                if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                    m.encodingmode == Settings.EncodingModes.ThreePass ||
                    m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                    m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    //отключаем пакет бит стрим так как он не работает в многопроходном режиме
                    m.XviD_options.packedmode = false;
                    check_packedmode.IsChecked = false;
                }

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void SetDefaultBitrates()
        {
            if (m.encodingmode == Settings.EncodingModes.Quantizer)
            {
                m.outvbitrate = 3.0m;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
            }
            else if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.outvbitrate = 3.0m;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Quality") + ": (Q)";
            }
            else if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                m.outvbitrate = Calculate.GetAutoBitrate(m);
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.outvbitrate = m.infilesizeint;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
            }

            SetToolTips();
        }

        private void combo_quality_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_quality.IsDropDownOpen || combo_quality.IsSelectionBoxHighlighted) && combo_quality.SelectedItem != null)
            {
                m.XviD_options.quality = Convert.ToInt32(combo_quality.SelectedItem.ToString().Substring(0, 1));
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_chroma_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.chromame = check_chroma.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_qmatrix_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_qmatrix.IsDropDownOpen || combo_qmatrix.IsSelectionBoxHighlighted) && combo_qmatrix.SelectedItem != null)
            {
                m.XviD_options.qmatrix = combo_qmatrix.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_trellis_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.trellis = check_trellis.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_vhqmode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_vhqmode.IsDropDownOpen || combo_vhqmode.IsSelectionBoxHighlighted) && combo_vhqmode.SelectedItem != null)
            {
                m.XviD_options.vhqmode = Convert.ToInt32(combo_vhqmode.SelectedItem.ToString().Substring(0, 1));
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_metric_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_metric.IsDropDownOpen || combo_metric.IsSelectionBoxHighlighted) && combo_metric.SelectedItem != null)
            {
                m.XviD_options.metric = Convert.ToInt32(combo_metric.SelectedItem.ToString().Substring(0, 1));
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_grayscale_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.gray = check_grayscale.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_cartoon_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.cartoon = check_cartoon.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_chroma_opt_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.chroma_opt = check_chroma_opt.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_packedmode_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.packedmode = check_packedmode.IsChecked.Value;
            if (m.XviD_options.packedmode && m.XviD_options.bframes == 0)
            {
                m.XviD_options.bframes = 1;
                combo_bframes.SelectedItem = 1;
            }
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_gmc_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.gmc = check_gmc.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_qpel_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.qpel = check_qpel.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_bvhq_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.bvhq = check_bvhq.IsChecked.Value;
            if (m.XviD_options.bvhq && m.XviD_options.bframes == 0)
            {
                m.XviD_options.bframes = 1;
                combo_bframes.SelectedItem = 1;
            }
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_closedgop_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.closedgop = check_closedgop.IsChecked.Value;
            if (m.XviD_options.closedgop && m.XviD_options.bframes == 0)
            {
                m.XviD_options.bframes = 1;
                combo_bframes.SelectedItem = 1;
            }
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted) && combo_bframes.SelectedItem != null)
            {
                m.XviD_options.bframes = Convert.ToInt32(combo_bframes.SelectedItem);

                if (m.XviD_options.bframes == 0)
                {
                    check_bvhq.IsChecked = false;
                    m.XviD_options.bvhq = false;

                    check_packedmode.IsChecked = false;
                    m.XviD_options.packedmode = false;

                    check_closedgop.IsChecked = false;
                    m.XviD_options.closedgop = false;
                }

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_masking_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_masking.IsDropDownOpen || combo_masking.IsSelectionBoxHighlighted) && combo_masking.SelectedIndex != -1)
            {
                m.XviD_options.masking = combo_masking.SelectedIndex;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_codec_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_codec_preset.IsDropDownOpen || combo_codec_preset.IsSelectionBoxHighlighted) && combo_codec_preset.SelectedItem != null)
            {
                CodecPresets preset = (CodecPresets)Enum.Parse(typeof(CodecPresets), combo_codec_preset.SelectedItem.ToString());

                if (preset == CodecPresets.Default)
                {
                    m.XviD_options.bframes = 2;
                    m.XviD_options.bvhq = false;
                    m.XviD_options.chromame = true;
                    m.XviD_options.closedgop = true;
                    m.XviD_options.gmc = false;
                    m.XviD_options.masking = 0;
                    m.XviD_options.packedmode = true;
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = false;
                    m.XviD_options.quality = 6;
                    m.XviD_options.trellis = true;
                    m.XviD_options.vhqmode = 1;
                    m.XviD_options.metric = 0;
                    m.XviD_options.fourcc = "XVID";
                    m.XviD_options.keyint = 300;
                    m.XviD_options.imin = 2;
                    m.XviD_options.imax = 31;
                    m.XviD_options.pmin = 2;
                    m.XviD_options.pmax = 31;
                    m.XviD_options.bmin = 2;
                    m.XviD_options.bmax = 31;
                    m.XviD_options.mins = 0;
                    m.XviD_options.full_first_pass = false;
                    m.XviD_options.b_ratio = 150;
                    m.XviD_options.b_offset = 100;
                    m.XviD_options.kboost = 10;
                    m.XviD_options.ostrength = 5;
                    m.XviD_options.oimprove = 5;
                    m.XviD_options.odegrade = 5;
                    m.XviD_options.chigh = 0;
                    m.XviD_options.clow = 0;
                    m.XviD_options.reaction = 16;
                    m.XviD_options.averaging = 100;
                    m.XviD_options.smoother = 100;
                    m.XviD_options.vbvmax = 0;
                    m.XviD_options.vbvsize = 0;
                    m.XviD_options.vbvpeak = 0;
                    m.XviD_options.firstpass_q = 2.0M;

                    SetDefaultBitrates();
                }
                else if (preset == CodecPresets.Turbo)
                {
                    m.XviD_options.bframes = Format.GetMaxBFrames(m);
                    m.XviD_options.bvhq = false;
                    m.XviD_options.chromame = false;
                    m.XviD_options.closedgop = Format.GetValidBiValue(m);
                    m.XviD_options.gmc = false;
                    m.XviD_options.masking = 0;
                    m.XviD_options.packedmode = false;
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = false;
                    m.XviD_options.quality = 1;
                    m.XviD_options.trellis = false;
                    m.XviD_options.vhqmode = 0;
                    m.XviD_options.metric = 0;
                    m.XviD_options.firstpass_q = 2.0M;
                }
                else if (preset == CodecPresets.Ultra)
                {
                    m.XviD_options.bframes = Format.GetMaxBFrames(m);
                    m.XviD_options.bvhq = Format.GetValidBiValue(m);
                    m.XviD_options.chromame = true;
                    m.XviD_options.closedgop = Format.GetValidBiValue(m);
                    m.XviD_options.gmc = Format.GetValidGMC(m);
                    m.XviD_options.masking = 0;
                    m.XviD_options.packedmode = Format.GetValidPackedMode(m);
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = Format.GetValidQPEL(m);
                    m.XviD_options.quality = 6;
                    m.XviD_options.trellis = true;
                    m.XviD_options.vhqmode = 1;
                    m.XviD_options.metric = 0;
                    m.XviD_options.firstpass_q = 2.0M;
                }
                else if (preset == CodecPresets.Extreme)
                {
                    m.XviD_options.bframes = Format.GetMaxBFrames(m);
                    m.XviD_options.bvhq = Format.GetValidBiValue(m);
                    m.XviD_options.chromame = true;
                    m.XviD_options.closedgop = Format.GetValidBiValue(m);
                    m.XviD_options.gmc = Format.GetValidGMC(m);
                    m.XviD_options.masking = 2;
                    m.XviD_options.packedmode = Format.GetValidPackedMode(m);
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = Format.GetValidQPEL(m);
                    m.XviD_options.quality = 6;
                    m.XviD_options.trellis = true;
                    m.XviD_options.vhqmode = 4;
                    m.XviD_options.metric = 0;
                    m.XviD_options.firstpass_q = 2.0M;
                }

                if (preset != CodecPresets.Custom)
                {
                    LoadFromProfile();
                    root_window.UpdateOutSize();
                    root_window.UpdateManualProfile();
                    UpdateCLI();
                }
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
            if (m.XviD_options.bframes == 2 &&
                m.XviD_options.bvhq == false &&
                m.XviD_options.chromame == true &&
                m.XviD_options.closedgop == true &&
                m.XviD_options.gmc == false &&
                m.XviD_options.masking == 0 &&
                m.XviD_options.packedmode == true &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == false &&
                m.XviD_options.quality == 6 &&
                m.XviD_options.trellis == true &&
                m.XviD_options.vhqmode == 1 &&
                m.XviD_options.metric == 0 &&
                m.XviD_options.fourcc == "XVID" &&
                m.XviD_options.keyint == 300 &&
                m.XviD_options.imin == 2 &&
                m.XviD_options.imax == 31 &&
                m.XviD_options.pmin == 2 &&
                m.XviD_options.pmax == 31 &&
                m.XviD_options.bmin == 2 &&
                m.XviD_options.bmax == 31 &&
                m.XviD_options.full_first_pass == false &&
                m.XviD_options.b_ratio == 150 &&
                m.XviD_options.b_offset == 100 &&
                m.XviD_options.kboost == 10 &&
                m.XviD_options.ostrength == 5 &&
                m.XviD_options.oimprove == 5 &&
                m.XviD_options.odegrade == 5 &&
                m.XviD_options.chigh == 0 &&
                m.XviD_options.clow == 0 &&
                m.XviD_options.reaction == 16 &&
                m.XviD_options.averaging == 100 &&
                m.XviD_options.smoother == 100 &&
                m.XviD_options.vbvmax == 0 &&
                m.XviD_options.vbvsize == 0 &&
                m.XviD_options.vbvpeak == 0 &&
                m.XviD_options.firstpass_q == 2)
                preset = CodecPresets.Default;

            //Turbo
            else if (m.XviD_options.bframes == Format.GetMaxBFrames(m) &&
                m.XviD_options.bvhq == false &&
                m.XviD_options.chromame == false &&
                m.XviD_options.closedgop == Format.GetValidBiValue(m) &&
                m.XviD_options.gmc == false &&
                m.XviD_options.masking == 0 &&
                m.XviD_options.packedmode == false &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == false &&
                m.XviD_options.quality == 1 &&
                m.XviD_options.trellis == false &&
                m.XviD_options.vhqmode == 0 &&
                m.XviD_options.metric == 0 &&
                m.XviD_options.firstpass_q == 2)
                preset = CodecPresets.Turbo;

            //Ultra
            else if (m.XviD_options.bframes == Format.GetMaxBFrames(m) &&
                m.XviD_options.bvhq == Format.GetValidBiValue(m) &&
                m.XviD_options.chromame == true &&
                m.XviD_options.closedgop == Format.GetValidBiValue(m) &&
                m.XviD_options.gmc == Format.GetValidGMC(m) &&
                m.XviD_options.masking == 0 &&
                m.XviD_options.packedmode == Format.GetValidPackedMode(m) &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == Format.GetValidQPEL(m) &&
                m.XviD_options.quality == 6 &&
                m.XviD_options.trellis == true &&
                m.XviD_options.vhqmode == 1 &&
                m.XviD_options.metric == 0 &&
                m.XviD_options.firstpass_q == 2)
                preset = CodecPresets.Ultra;

            //Extreme
            else if (m.XviD_options.bframes == Format.GetMaxBFrames(m) &&
                m.XviD_options.bvhq == Format.GetValidBiValue(m) &&
                m.XviD_options.chromame == true &&
                m.XviD_options.closedgop == Format.GetValidBiValue(m) &&
                m.XviD_options.gmc == Format.GetValidGMC(m) &&
                m.XviD_options.masking > 0 &&
                m.XviD_options.packedmode == Format.GetValidPackedMode(m) &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == Format.GetValidQPEL(m) &&
                m.XviD_options.quality == 6 &&
                m.XviD_options.trellis == true &&
                m.XviD_options.vhqmode == 4 &&
                m.XviD_options.metric == 0 &&
                m.XviD_options.firstpass_q == 2)
                preset = CodecPresets.Extreme;

            combo_codec_preset.SelectedItem = preset.ToString();
            UpdateCLI();
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
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = 31;
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                     m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                num_bitrate.DecimalPlaces = 0;
                num_bitrate.Change = 1;
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = 50000;
            }
        }

        private void combo_fourcc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_fourcc.IsDropDownOpen || combo_fourcc.IsSelectionBoxHighlighted) && combo_fourcc.SelectedItem != null)
            {
                m.XviD_options.fourcc = combo_fourcc.SelectedItem.ToString();
                Settings.XviDFOURCC = m.XviD_options.fourcc;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
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
                DecodeLine(m);                       //- Загружаем в массив m.xvid значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                   //- Загружаем в форму значения, на основе значений массива m.xvid
                m.vencoding = "Custom XviD CLI";     //- Изменяем название пресета           
                PresetLoader.CreateVProfile(m);      //- Перезаписываем файл пресета (m.vpasses[x])
                root_window.m = this.m.Clone();      //- Передаем массив в основное окно
                root_window.LoadProfiles();          //- Обновляем название выбранного пресета в основном окне (Custom XviD CLI)
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

        private void button_Reset_CLI_Click(object sender, RoutedEventArgs e)
        {
            if (good_cli != null)
            {
                m.vpasses = (ArrayList)good_cli.Clone(); //- Восстанавливаем CLI до версии, не вызывавшей ошибок
                DecodeLine(m);                           //- Загружаем в массив m.xvid значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                       //- Загружаем в форму значения, на основе значений массива m.xvid
                root_window.m = this.m.Clone();          //- Передаем массив в основное окно
            }
            else
            {
                new Message(root_window).ShowMessage("Can`t find good CLI...", Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void button_xvid_help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                help.FileName = Calculate.StartupPath + "\\apps\\xvid_encraw" + (!Settings.UseXviD_130 ? "\\1.2.2" : "") + "\\xvid_encraw.exe";
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = " -help";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                help.RedirectStandardError = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                new ShowWindow(root_window, "XviD help", p.StandardError.ReadToEnd(), new FontFamily("Lucida Console"));
            }
            catch (Exception ex) 
            {
                new Message(root_window).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void combo_imin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_imin.IsDropDownOpen || combo_imin.IsSelectionBoxHighlighted) && combo_imin.SelectedItem != null)
            {
                m.XviD_options.imin = Convert.ToInt32(combo_imin.SelectedItem);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_imax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_imax.IsDropDownOpen || combo_imax.IsSelectionBoxHighlighted) && combo_imax.SelectedItem != null)
            {
                m.XviD_options.imax = Convert.ToInt32(combo_imax.SelectedItem);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_pmin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_pmin.IsDropDownOpen || combo_pmin.IsSelectionBoxHighlighted) && combo_pmin.SelectedItem != null)
            {
                m.XviD_options.pmin = Convert.ToInt32(combo_pmin.SelectedItem);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_pmax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_pmax.IsDropDownOpen || combo_pmax.IsSelectionBoxHighlighted) && combo_pmax.SelectedItem != null)
            {
                m.XviD_options.pmax = Convert.ToInt32(combo_pmax.SelectedItem);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bmin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_bmin.IsDropDownOpen || combo_bmin.IsSelectionBoxHighlighted) && combo_bmin.SelectedItem != null)
            {
                m.XviD_options.bmin = Convert.ToInt32(combo_bmin.SelectedItem);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bmax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_bmax.IsDropDownOpen || combo_bmax.IsSelectionBoxHighlighted) && combo_bmax.SelectedItem != null)
            {
                m.XviD_options.bmax = Convert.ToInt32(combo_bmax.SelectedItem);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_keyint_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_keyint.IsAction)
            {
                m.XviD_options.keyint = Convert.ToInt32(num_keyint.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_threads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_threads.IsDropDownOpen || combo_threads.IsSelectionBoxHighlighted) && combo_threads.SelectedIndex != -1)
            {
                Settings.XviD_Threads = combo_threads.SelectedIndex;

                root_window.UpdateManualProfile();
                //DetectCodecPreset();
            }
        }

        private void check_fullpass_Clicked(object sender, RoutedEventArgs e)
        {
            m.XviD_options.full_first_pass = check_fullpass.IsChecked.Value;
                
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void num_bquant_ratio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bquant_ratio.IsAction)
            {
                m.XviD_options.b_ratio = Convert.ToInt32(num_bquant_ratio.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_bquant_offset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bquant_offset.IsAction)
            {
                m.XviD_options.b_offset = Convert.ToInt32(num_bquant_offset.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_kboost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_kboost.IsAction)
            {
                m.XviD_options.kboost = Convert.ToInt32(num_kboost.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_chigh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_chigh.IsAction)
            {
                m.XviD_options.chigh = Convert.ToInt32(num_chigh.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_clow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_clow.IsAction)
            {
                m.XviD_options.clow = Convert.ToInt32(num_clow.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_ostrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ostrength.IsAction)
            {
                m.XviD_options.ostrength = Convert.ToInt32(num_ostrength.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_oimprove_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_oimprove.IsAction)
            {
                m.XviD_options.oimprove = Convert.ToInt32(num_oimprove.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_odegrade_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_odegrade.IsAction)
            {
                m.XviD_options.odegrade = Convert.ToInt32(num_odegrade.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_reaction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_reaction.IsAction)
            {
                m.XviD_options.reaction = Convert.ToInt32(num_reaction.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_averaging_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_averaging.IsAction)
            {
                m.XviD_options.averaging = Convert.ToInt32(num_averaging.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_smoother_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_smoother.IsAction)
            {
                m.XviD_options.smoother = Convert.ToInt32(num_smoother.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_vbvmax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbvmax.IsAction)
            {
                m.XviD_options.vbvmax = Convert.ToInt32(num_vbvmax.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_vbvsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbvsize.IsAction)
            {
                m.XviD_options.vbvsize = Convert.ToInt32(num_vbvsize.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_vbvpeak_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbvpeak.IsAction)
            {
                m.XviD_options.vbvpeak = Convert.ToInt32(num_vbvpeak.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_xvid_new_Clicked(object sender, RoutedEventArgs e)
        {
            Settings.UseXviD_130 = check_xvid_new.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void num_firstpass_q_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_firstpass_q.IsAction)
            {
                m.XviD_options.firstpass_q = num_firstpass_q.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }
	}
}