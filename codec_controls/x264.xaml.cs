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
    public partial class x264
    {
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum CodecPresets { Default = 1, Fast, Slow, Slower, Placebo, Custom }

        public x264(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
        {
            this.InitializeComponent();

            this.num_bitrate.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_bitrate_ValueChanged);
            this.num_psyrdo.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_psyrdo_ValueChanged);
            this.num_qcomp.ValueChanged +=new RoutedPropertyChangedEventHandler<decimal>(num_qcomp_ValueChanged);
            this.num_psytrellis.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_psytrellis_ValueChanged);
            this.num_vbv_buf.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_vbv_buf_ValueChanged);
            this.num_vbv_max.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_vbv_max_ValueChanged);
            this.num_lookahead.ValueChanged +=new RoutedPropertyChangedEventHandler<decimal>(num_lookahead_ValueChanged);
            this.textbox_string1.TextChanged +=new TextChangedEventHandler(textbox_string1_TextChanged);
            this.textbox_string2.TextChanged += new TextChangedEventHandler(textbox_string2_TextChanged);
            this.textbox_string3.TextChanged += new TextChangedEventHandler(textbox_string3_TextChanged);
            

            this.m = mass.Clone();
            this.root_window = VideoEncWindow;
            this.p = parent;
            
            combo_mode.Items.Add("1-Pass Bitrate");
            combo_mode.Items.Add("2-Pass Bitrate");
            combo_mode.Items.Add("3-Pass Bitrate");
            combo_mode.Items.Add("Constant Quality");
            combo_mode.Items.Add("Constant Quantizer");
            combo_mode.Items.Add("2-Pass Quality");
            combo_mode.Items.Add("3-Pass Quality");
            combo_mode.Items.Add("2-Pass Size");
            combo_mode.Items.Add("3-Pass Size");

            //прогружаем список AVC level
            combo_level.Items.Add("unrestricted");
            combo_level.Items.Add("1.0");
            combo_level.Items.Add("1.1");
            combo_level.Items.Add("1.2");
            combo_level.Items.Add("1.3");
            combo_level.Items.Add("2.0");
            combo_level.Items.Add("2.1");
            combo_level.Items.Add("2.2");
            combo_level.Items.Add("3.0");
            combo_level.Items.Add("3.1");
            combo_level.Items.Add("3.2");
            combo_level.Items.Add("4.0");
            combo_level.Items.Add("4.1");
            combo_level.Items.Add("4.2");
            combo_level.Items.Add("5.0");
            combo_level.Items.Add("5.1");

            //Adaptive Quantization
           // combo_adapt_quant.Items.Add("Disabled");
          //  combo_adapt_quant.Items.Add("Auto");
            for (double n = 0.1; n <= 2.1; n+=0.1)
                combo_adapt_quant.Items.Add(Calculate.ConvertDoubleToPointString(n, 1));

            combo_adapt_quant_mode.Items.Add("Disabled");
            combo_adapt_quant_mode.Items.Add("1");
            combo_adapt_quant_mode.Items.Add("2");
            
            
            //прогружаем деблокинг
            for (int n = -6; n <= 6; n++)
            {
                combo_dstrength.Items.Add(n);
                combo_dthreshold.Items.Add(n);
            }

            //Прописываем subme
            combo_subme.Items.Add("1 - QPel SAD");
            combo_subme.Items.Add("2 - QPel SATD");
            combo_subme.Items.Add("3 - HPel on MB then QPel");
            combo_subme.Items.Add("4 - Always QPel");
            combo_subme.Items.Add("5 - QPel & Bidir ME");
            combo_subme.Items.Add("6 - RD on I/P frames");
            combo_subme.Items.Add("7 - RD on all frames");
            combo_subme.Items.Add("8 - RD refinement on I/P frames");
            combo_subme.Items.Add("9 - RD refinement on all frames");
            combo_subme.Items.Add("10 - QP-RD reg. trellis=2,AQ-mode>0");  

            //прописываем me алгоритм
            combo_me.Items.Add("Diamond");
            combo_me.Items.Add("Hexagon");
            combo_me.Items.Add("Multi Hexagon");
            combo_me.Items.Add("Exhaustive");
            combo_me.Items.Add("SATD Exhaustive");

            //прописываем me range
            for (int n = 4; n <= 64; n++)
                combo_merange.Items.Add(n);

            //B фреймы
            for (int n = 0; n <= 16; n++)
                combo_bframes.Items.Add(n);

            //режим B фреймов
            combo_bframe_mode.Items.Add("Disabled");
            combo_bframe_mode.Items.Add("Spatial");
            combo_bframe_mode.Items.Add("Temporal");
            combo_bframe_mode.Items.Add("Auto");

            //trellis
            combo_trellis.Items.Add("0 - Disabled");
            combo_trellis.Items.Add("1 - Final MB");
            combo_trellis.Items.Add("2 - Always");

            //refernce frames
            for (int n = 1; n <= 16; n++)
                combo_ref.Items.Add(n);

            //Прогружаем список AVC profile
            combo_avc_profile.Items.Add("Baseline Profile");
            combo_avc_profile.Items.Add("Main Profile");
            combo_avc_profile.Items.Add("High Profile");

            //minimum quantizer
            for (int n = 1; n <= 51; n++)
                combo_min_quant.Items.Add(n);

            //пресеты настроек и качества
            foreach (string preset in Enum.GetNames(typeof(CodecPresets)))
                combo_codec_preset.Items.Add(preset);

            //введенные пользователем параметры х264-го
            textbox_string1.Text = m.userstring1;
            textbox_string2.Text = m.userstring2;
            textbox_string3.Text = m.userstring3;

            //Кол-во потоков для x264-го
            combo_threads_count.Items.Add("Auto");
            for (int n = 1; n <= 8; n++)
                combo_threads_count.Items.Add(Convert.ToString(n));

            //-b-adapt
            combo_badapt_mode.Items.Add("Disabled");
            combo_badapt_mode.Items.Add("Fast");
            combo_badapt_mode.Items.Add("Optimal");
            
            for (int n = -16; n<=16; n++)
                combo_chroma_qp.Items.Add(Convert.ToString(n));
                       
            //--b-pyramid
            combo_bpyramid_mode.Items.Add("None");
            combo_bpyramid_mode.Items.Add("Strict");
            combo_bpyramid_mode.Items.Add("Normal");
            
            LoadFromProfile();
        }

        public void LoadFromProfile()
        {
            SetMinMaxBitrate();

            combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
            if (combo_mode.SelectedItem == null)
            {
                m.encodingmode = Settings.EncodingModes.Quality;
                combo_mode.SelectedItem = Calculate.EncodingModeEnumToString(m.encodingmode);
                SetMinMaxBitrate();
                SetDefaultBitrates();
            }

            //запоминаем первичные режим кодирования
            oldmode = m.encodingmode;

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
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }

            //lossless
            if (m.outvbitrate == 0)
                check_lossless.IsChecked = true;
            else
                check_lossless.IsChecked = false;

            //прогружаем список AVC level
            combo_level.SelectedItem = m.x264options.level;

            combo_adapt_quant.SelectedItem = m.x264options.aqstrength;
            combo_adapt_quant_mode.SelectedItem = m.x264options.aqmode;
            
            num_psyrdo.Value = m.x264options.psyrdo;
            num_psytrellis.Value = m.x264options.psytrellis;
            num_qcomp.Value = m.x264options.qcomp;
            num_vbv_max.Value = m.x264options.vbv_maxrate;
            num_vbv_buf.Value = m.x264options.vbv_bufsize;

            //прописываем макроблоки
            if (m.x264options.analyse.Contains("p8x8"))
                check_p8x8.IsChecked = true;
            else
                check_p8x8.IsChecked = false;

            if (m.x264options.analyse.Contains("i8x8"))
                check_i8x8.IsChecked = true;
            else
                check_i8x8.IsChecked = false;

            if (m.x264options.analyse.Contains("b8x8"))
                check_b8x8.IsChecked = true;
            else
                check_b8x8.IsChecked = false;

            if (m.x264options.analyse.Contains("i4x4"))
                check_i4x4.IsChecked = true;
            else
                check_i4x4.IsChecked = false;

            if (m.x264options.analyse.Contains("p4x4"))
                check_p4x4.IsChecked = true;
            else
                check_p4x4.IsChecked = false;

            if (m.x264options.analyse == "none")
            {
                check_p8x8.IsChecked = false;
                check_i8x8.IsChecked = false;
                check_b8x8.IsChecked = false;
                check_i4x4.IsChecked = false;
                check_p4x4.IsChecked = false;
            }

            if (m.x264options.analyse == "all")
            {
                check_p8x8.IsChecked = true;
                check_i8x8.IsChecked = true;
                check_b8x8.IsChecked = true;
                check_i4x4.IsChecked = true;
                check_p4x4.IsChecked = true;
            }

            //adaptive dct
            check_8x8dct.IsChecked = m.x264options.adaptivedct;

            //прогружаем деблокинг
            combo_dstrength.SelectedItem = m.x264options.deblocks;
            combo_dthreshold.SelectedItem = m.x264options.deblockt;
            check_deblocking.IsChecked = m.x264options.deblocking;

            //Прописываем subme
            combo_subme.SelectedItem = combo_subme.Items[m.x264options.subme - 1];

            //прописываем me алгоритм
            if (m.x264options.me == "dia")
                combo_me.SelectedItem = "Diamond";
            if (m.x264options.me == "hex")
                combo_me.SelectedItem = "Hexagon";
            if (m.x264options.me == "umh")
                combo_me.SelectedItem = "Multi Hexagon";
            if (m.x264options.me == "esa")
                combo_me.SelectedItem = "Exhaustive";
            if (m.x264options.me == "tesa")
                combo_me.SelectedItem = "SATD Exhaustive";

            //прописываем me range
            combo_merange.SelectedItem = m.x264options.merange;

            //прописываем chroma me
            check_chroma.IsChecked = m.x264options.chroma;

            //B фреймы
            combo_bframes.SelectedItem = m.x264options.bframes;

            //режим B фреймов
            if (m.x264options.direct == "none")
                combo_bframe_mode.SelectedItem = "Disabled";
            if (m.x264options.direct == "spatial")
                combo_bframe_mode.SelectedItem = "Spatial";
            if (m.x264options.direct == "temporal")
                combo_bframe_mode.SelectedItem = "Temporal";
            if (m.x264options.direct == "auto")
                combo_bframe_mode.SelectedItem = "Auto";

            //b-pyramid
            combo_bpyramid_mode.SelectedIndex = m.x264options.bpyramid;

            //weightb
            check_weightedb.IsChecked = m.x264options.weightb;

            //trellis
            combo_trellis.SelectedIndex = m.x264options.trellis;

            //refernce frames
            combo_ref.SelectedItem = m.x264options.reference;

            //mixed reference
            check_mixed_ref.IsChecked = m.x264options.mixedrefs;

            //cabac
            check_cabac.IsChecked = m.x264options.cabac;

            //fast p skip
            check_fast_pskip.IsChecked = m.x264options.fastpskip;

            //dct decimate
            check_dct_decimate.IsChecked = m.x264options.dctdecimate;

            //Прогружаем список AVC profile
            SetAVCProfile();

            //minimum quantizer
            combo_min_quant.SelectedItem = m.x264options.minquant;

            //Кол-во потоков для x264-го
            if (m.x264options.threads == "auto")
                combo_threads_count.SelectedItem = "Auto";
            else
                combo_threads_count.SelectedItem = m.x264options.threads;

            //-b-adapt            
            combo_badapt_mode.SelectedIndex = m.x264options.b_adapt;

            combo_chroma_qp.SelectedItem = m.x264options.qp_offset;

            check_slow_first.IsChecked = m.x264options.slow_frstpass;

            check_nombtree.IsChecked = m.x264options.no_mbtree;
            
            if (m.x264options.no_mbtree == true)
                num_lookahead.IsEnabled = false;
            else
                num_lookahead.IsEnabled = true;
            
            num_lookahead.Value = m.x264options.lookahead;

            check_enable_psy.IsChecked = !m.x264options.no_psy;

            if (m.x264options.no_psy == true)
            {
                num_psyrdo.IsEnabled = false;
                num_psytrellis.IsEnabled = false;
            }
            else
            {
                num_psyrdo.IsEnabled = true;
                num_psytrellis.IsEnabled = true;
            }

            SetToolTips();
            DetectCodecPreset();
        }

        private string GetMackroblocks()
        {
            if (check_p8x8.IsChecked.Value &&
                check_i8x8.IsChecked.Value &&
                check_b8x8.IsChecked.Value &&
                check_i4x4.IsChecked.Value &&
                check_p4x4.IsChecked.Value)
                return "all";

            if (!check_p8x8.IsChecked.Value &&
    !check_i8x8.IsChecked.Value &&
    !check_b8x8.IsChecked.Value &&
    !check_i4x4.IsChecked.Value &&
    !check_p4x4.IsChecked.Value)
                return "none";

            ArrayList macroblocks = new ArrayList();
            if (check_p8x8.IsChecked.Value)
                macroblocks.Add("p8x8");

            if (check_i8x8.IsChecked.Value)
                macroblocks.Add("i8x8");

            if (check_b8x8.IsChecked.Value)
                macroblocks.Add("b8x8");

            if (check_i4x4.IsChecked.Value)
                macroblocks.Add("i4x4");

            if (check_p4x4.IsChecked.Value)
                macroblocks.Add("p4x4");

            string s_macroblocks = "";
            int n = 0;
            foreach (string b in macroblocks)
            {
                s_macroblocks += b;
                if (b != macroblocks[macroblocks.Count - 1].ToString())
                    s_macroblocks += ",";
                n++;
            }

            return s_macroblocks;

        }

        private void SetAVCProfile()
        {
            string avcprofile = "Main Profile";

            if (!m.x264options.cabac ||
                m.x264options.bframes == 0)
                avcprofile = "Baseline Profile";

            if (m.x264options.analyse.Contains("i8x8") ||
                m.x264options.analyse == "all" ||
                m.x264options.adaptivedct ||
                m.outvbitrate == 0 ||
                m.x264options.custommatrix != null)
                avcprofile = "High Profile";

            combo_avc_profile.SelectedItem = avcprofile;
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
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else
                num_bitrate.ToolTip = "Set target quality (Default: 21)";

            check_lossless.ToolTip = "LossLess encoding mode. High AVC profile only";
            combo_level.ToolTip = "Specify level (--level)";
            check_p8x8.ToolTip = "Partitions to consider";
            check_i8x8.ToolTip = "Partitions to consider";
            check_b8x8.ToolTip = "Partitions to consider";
            check_i4x4.ToolTip = "Partitions to consider";
            check_p4x4.ToolTip = "Partitions to consider";
            check_8x8dct.ToolTip = "Adaptive spatial transform size (--no-8x8dct if not checked)";
            combo_dstrength.ToolTip = "Deblocking filter strength (--deblock, default: 0:0)";
            combo_dthreshold.ToolTip = "Deblocking filter treshold (--deblock, default: 0:0)";
            check_deblocking.ToolTip = "Deblocking filter (Default: Enabled)";
            combo_subme.ToolTip = "Subpixel motion estimation and partition decision quality: 1=fast, 10=best. (--subme, default: 7)";
            combo_me.ToolTip = "Subpixel motion estimation and mode decision (Default: --hex)" + Environment.NewLine +
                "Diamond Search (--dia)" + Environment.NewLine +
                "Hexagonal Search (--hex)" + Environment.NewLine +
                "Uneven Multi-Hexagon Search (--umh)" + Environment.NewLine +
                "Exhaustive Search (--esa)" + Environment.NewLine +
                "SATD Exhaustive Search (--tesa)";
            combo_merange.ToolTip = "Maximum motion vector search range (--merange, default: 16)";
            check_chroma.ToolTip = "Chroma motion estimation (--no-chroma-me if not checked)";
            combo_bframes.ToolTip = "Number of B-frames between I and P (--bframes, default: 3)";
            combo_bframe_mode.ToolTip = "B-frame mode (--direct, default: Spatial)";
            combo_bpyramid_mode.ToolTip = "Keep some B-frames as references. \r\n -none: disabled \r\n -strict: strictly hierarchical pyramid (Blu-ray compatible)\r\n -normal: non-strict (not Blu-ray compatible) \r\n(--b-pyramid value, default: --b-pyramid none)";
            check_weightedb.ToolTip = "Weighted prediction for B-frames (--no-weightb if not checked)";
            combo_trellis.ToolTip = "Trellis RD quantization. Requires CABAC." + Environment.NewLine +
                "0: disabled" + Environment.NewLine +
                "1: enabled only on the final encode of a MB (Default)" + Environment.NewLine +
                "2: enabled on all mode decisions";
            combo_ref.ToolTip = "Number of reference frames (--ref, default: 3)";
            check_mixed_ref.ToolTip = "Decide references on a per partition basis (--no-mixed-refs if not checked)";
            check_cabac.ToolTip = "Enable CABAC (--no-cabac if not checked)";
            check_fast_pskip.ToolTip = "Enable early SKIP detection on P-frames (--no-fast-pskip if not checked)";
            check_dct_decimate.ToolTip = "Enable coefficient thresholding on P-frames (--no-dct-decimate if not checked)";
            combo_avc_profile.ToolTip = "AVC profile";
            combo_min_quant.ToolTip = "Set min QP (Default: 10)";
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                combo_codec_preset.ToolTip = "Default - restore default settings" + Environment.NewLine +
                    "Fast - fast encoding, big output file size" + Environment.NewLine +
                    "Slow - good quality encoding, medium file size" + Environment.NewLine +
                    "Slower - high quality encoding, small file size" + Environment.NewLine +
                    "Placebo - super high quality encoding, smallest file size" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            else
            {
                combo_codec_preset.ToolTip = "Default - restore default settings" + Environment.NewLine +
                    "Fast - fast encoding, bad quality" + Environment.NewLine +
                    "Slow - good quality encoding, optimal speed-quality solution" + Environment.NewLine +
                    "Slower - high quality encoding" + Environment.NewLine +
                    "Placebo - high quality encoding, very slow" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
        combo_badapt_mode.ToolTip = "Adaptive B-frame decision method (--b-adapt, default: Fast)";
        combo_adapt_quant_mode.ToolTip = "AQ method (--aq-mode, default: 1)" + Environment.NewLine +
                    "0: Disabled" + Environment.NewLine +
                    "1: Variance AQ (Default)" + Environment.NewLine +
                    "2: Variance-Hadamard AQ (AutoVAQ patch)";
        combo_adapt_quant.ToolTip = "AQ Strength (--ag-strength, default: 1.0)" + Environment.NewLine +
                    "Reduces blocking and blurring in flat and textured areas" + Environment.NewLine +
                    "0.5: weak AQ, 1.5: strong AQ";
        num_psyrdo.ToolTip = "Strength of psychovisual RD optimization (--psy-rd, default: 1.0:0.0)";
        num_psytrellis.ToolTip = "Strength of psychovisual Trellis optimization (--psy-rd, default: 0.0)";
        num_vbv_buf.ToolTip = "Enable CBR and set VBV buffer size (--vbv-bufsize, default: 0)";
        num_vbv_max.ToolTip = "Set maximum local bitrate (--vbv-maxrate, default: 0)";
        num_qcomp.ToolTip = "QP curve compression (--qcomp, default: 0.6)" + Environment.NewLine +
                    "0.0 => CBR, 1.0 => CQP";
        combo_chroma_qp.ToolTip = "QP difference between chroma and luma (--qp-chroma-offset Default: 0)";
        combo_threads_count.ToolTip = "Set number of threads for encoding (--threads, default: Auto)";
        check_slow_first.ToolTip = "Enable slow 1-st pass for multipassing encoding (off by default)" + Environment.NewLine + "(--slow-firstpass if checked)";
        check_nombtree.ToolTip = "Disable mb-tree ratecontrol (off by default, --no-mbtree if checked)";
        num_lookahead.ToolTip = "Number of frames for frametype lookahead (--rc-lookahead, default: 40)";
        check_enable_psy.ToolTip = "If unchecked disable all visual optimizations that worsen both PSNR and SSIM" + Environment.NewLine + "(--no-psy if not checked)";

        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров x264
            m.x264options = new x264_arguments();

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
                if (value == "--size")
                {
                    m.outvbitrate = Convert.ToInt32(cli[n + 1]);
                    if (m.vpasses.Count == 3)
                        mode = Settings.EncodingModes.ThreePassSize;
                    else if (m.vpasses.Count == 2)
                        mode = Settings.EncodingModes.TwoPassSize;
                }

                if (value == "--bitrate" || value == "-B")
                    m.outvbitrate = Convert.ToInt32(cli[n + 1]);

                if (value == "--level")
                    m.x264options.level = cli[n + 1];

                if (m.vpasses.Count == 1)
                {
                    if (m.vpasses.Count == 1)
                    {
                        if (value == "--crf")
                        {
                            mode = Settings.EncodingModes.Quality;
                            m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                        }
                        if (value == "--qp" || value == "-q")
                        {
                            mode = Settings.EncodingModes.Quantizer;
                            m.outvbitrate = Convert.ToInt32(cli[n + 1]);
                        }
                    }
                    else
                    {
                        if (value == "--crf")
                        {
                            mode = Settings.EncodingModes.TwoPassQuality;
                            m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                        }
                    }
                }
                if (m.vpasses.Count == 2)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.TwoPassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }
                if (m.vpasses.Count == 3)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.ThreePassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (value == "--ref" || value == "-r")
                    m.x264options.reference = Convert.ToInt32(cli[n + 1]);

                if (value == "--aq-strength")
                {
                    string aqvalue = cli[n + 1];
                    //if (aqvalue == "0.0")
                     //   m.x264options.aqstrength = "Disabled";
                    //else
                        m.x264options.aqstrength = aqvalue;
                }

                if (value == "--aq-mode")
                {
                    if (cli[n + 1] == "0")
                        m.x264options.aqmode = "Disabled";
                    else
                        m.x264options.aqmode = cli[n + 1];
                }

                if (value == "--no-psy")
                    m.x264options.no_psy = true;
                
                if (value == "--psy-rd")
                {
                    string psy = cli[n + 1];
                    string[] psyseparator = new string[] { ":" };
                    string[] psyvalues = psy.Split(psyseparator, StringSplitOptions.None);
                    m.x264options.psyrdo = (decimal)Calculate.ConvertStringToDouble(psyvalues[0]);
                    m.x264options.psytrellis = (decimal)Calculate.ConvertStringToDouble(psyvalues[1]);
                }

                if (value == "--partitions" || value == "--analyse" || value == "-A") //--analyse
                    m.x264options.analyse = cli[n + 1];

                if (value == "--deblock" || value == "--filter" || value == "-f") //--filter
                {
                    string filtervalues = cli[n + 1];
                    string[] fseparator = new string[] { ":" };
                    string[] fvalues = filtervalues.Split(fseparator, StringSplitOptions.None);
                    m.x264options.deblocks = Convert.ToInt32(fvalues[0]);
                    m.x264options.deblockt = Convert.ToInt32(fvalues[1]);
                }

                if (value == "--subme" || value == "-m")
                    m.x264options.subme = Convert.ToInt32(cli[n + 1]);

                if (value == "--me")
                    m.x264options.me = cli[n + 1];

                if (value == "--merange")
                    m.x264options.merange = Convert.ToInt32(cli[n + 1]);

                if (value == "--no-chroma-me")
                    m.x264options.chroma = false;

                if (value == "--bframes" || value == "-b")
                    m.x264options.bframes = Convert.ToInt32(cli[n + 1]);

                if (value == "--direct")
                    m.x264options.direct = cli[n + 1];

                if (value == "--b-adapt")
                    m.x264options.b_adapt = Convert.ToInt32(cli[n + 1]);

                if (value == "--b-pyramid")
                {
                    if ((cli[n + 1]) == "strict")
                        m.x264options.bpyramid = 1;
                    else if ((cli[n + 1]) == "normal")
                        m.x264options.bpyramid = 2;
                }

                if (value == "--no-weightb")
                    m.x264options.weightb = false;

                if (value == "--no-8x8dct")
                    m.x264options.adaptivedct = false;

                if (value == "--trellis" || value == "-t")
                    m.x264options.trellis = Convert.ToInt32(cli[n + 1]);

                if (value == "--no-mixed-refs")
                    m.x264options.mixedrefs = false;

                if (value == "--no-cabac")
                    m.x264options.cabac = false;

                if (value == "--no-fast-pskip")
                    m.x264options.fastpskip = false;

                if (value == "--no-dct-decimate")
                    m.x264options.dctdecimate = false;

                if (value == "--cqm")
                    m.x264options.custommatrix = cli[n + 1];

                if (value == "--no-deblock" || value == "--nf")//--nf
                    m.x264options.deblocking = false;

                if (value == "--qpmin")
                    m.x264options.minquant = Convert.ToInt32(cli[n + 1]);

                if (value == "--aud")
                    m.x264options.aud = true;

                if (value == "--pictiming")
                    m.x264options.pictiming = true;

                if (value == "--threads")
                    m.x264options.threads = cli[n + 1];

                if (value == "--qcomp")
                    m.x264options.qcomp = (decimal)Calculate.ConvertStringToDouble(cli[n+1]);

                if (value == "--vbv-bufsize")
                    m.x264options.vbv_bufsize = Convert.ToInt32(cli[n + 1]);
                
                if (value == "--vbv-maxrate")
                    m.x264options.vbv_maxrate = Convert.ToInt32(cli[n + 1]);

                if (value == "--chroma-qp-offset")
                    m.x264options.qp_offset = cli[n + 1];

                if (value == "--slow-firstpass")
                    m.x264options.slow_frstpass = true;

                if (value == "--no-mbtree")
                    m.x264options.no_mbtree = true;

                if (value == "--rc-lookahead")
                    m.x264options.lookahead = Convert.ToInt32(cli[n + 1]);
                
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
            string line = "";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line += "--bitrate " + m.outvbitrate;
            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                line += "--size " + m.outvbitrate;

            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                line += "--crf " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1);

            if (m.encodingmode == Settings.EncodingModes.Quantizer)
                line += "--qp " + m.outvbitrate;

            if (m.x264options.level != "unrestricted")
                line += " --level " + m.x264options.level;

            if (m.x264options.reference != 3)
                line += " --ref " + m.x264options.reference;

            if (m.x264options.aqmode != "1")
            {
                if (m.x264options.aqmode == "Disabled")
                    line += " --aq-mode 0";
                else
                    line += " --aq-mode " + m.x264options.aqmode;
            }
            
            //if (m.x264options.aqstrength == "Disabled")
             //   line += " --aq-strength 0.0";
            //else if (m.x264options.aqstrength != "Auto")
              //  line += " --aq-strength " + m.x264options.aqstrength;
            if (m.x264options.aqstrength != "1.0" && m.x264options.aqmode != "Disabled")
                line += " --aq-strength " + m.x264options.aqstrength;

            if (!m.x264options.cabac)
                line += " --no-cabac";

            if (m.x264options.mixedrefs == false)
                line += " --no-mixed-refs";

            if (m.x264options.deblocking &&
                m.x264options.deblocks != 0 ||
                m.x264options.deblocking &&
                m.x264options.deblockt != 0)
                line += " --deblock " + m.x264options.deblocks + ":" + m.x264options.deblockt; //--filter

            if (!m.x264options.deblocking)
                line += " --no-deblock"; //-nf

            if (m.x264options.merange != 16)
                line += " --merange " + m.x264options.merange;

            if (!m.x264options.chroma)
                line += " --no-chroma-me";

            if (m.x264options.bframes != 3)
                line += " --bframes " + m.x264options.bframes;

            if (m.x264options.direct != "spatial")
                line += " --direct " + m.x264options.direct;

            if (m.x264options.b_adapt != 1)
                line += " --b-adapt " + m.x264options.b_adapt;

            if (m.x264options.bpyramid != 0)
            {
                if (m.x264options.bpyramid == 1)
                    line += " --b-pyramid strict";
                else
                    line += " --b-pyramid normal";
            }

            if (m.x264options.weightb == false)
                line += " --no-weightb";

            if (m.x264options.trellis != 1)
                line += " --trellis " + m.x264options.trellis;

            if (!m.x264options.fastpskip)
                line += " --no-fast-pskip";

            if (!m.x264options.dctdecimate)
                line += " --no-dct-decimate";

            if (m.x264options.custommatrix != null)
                line += " --cqm " + m.x264options.custommatrix;

            if (m.x264options.minquant != 10)
                line += " --qpmin " + m.x264options.minquant;

            if (m.x264options.aud == true)
                line += " --aud";

            if (m.x264options.pictiming == true)
                line += " --pictiming";

            if (m.x264options.no_psy == true)
                line += " --no-psy";

            if (!m.x264options.no_psy && (m.x264options.psyrdo != 1 ||
                m.x264options.psytrellis != 0))
                line += " --psy-rd " + Calculate.ConvertDoubleToPointString((double)m.x264options.psyrdo, 1) + ":" +
                    Calculate.ConvertDoubleToPointString((double)m.x264options.psytrellis, 1);

            if (m.x264options.threads != "auto")
                line += " --threads " + m.x264options.threads;

            if (m.x264options.qcomp != 0.6m)
                line += " --qcomp " + Calculate.ConvertDoubleToPointString((double)m.x264options.qcomp, 1);

            if (m.x264options.vbv_bufsize != 0)
                line += " --vbv-bufsize " + m.x264options.vbv_bufsize;

            if (m.x264options.vbv_maxrate != 0)
                line += " --vbv-maxrate " + m.x264options.vbv_maxrate;

            if (m.x264options.qp_offset != "0")
                line += " --chroma-qp-offset " + m.x264options.qp_offset;
           
            if (m.x264options.analyse != null)
                line += " --partitions " + m.x264options.analyse;

            if (m.x264options.adaptivedct == false)
                line += " --no-8x8dct";

            if (m.x264options.subme != 7)
                line += " --subme " + m.x264options.subme;

            if (m.x264options.me != "hex")
                line += " --me " + m.x264options.me;
   
            if (m.x264options.slow_frstpass == true)
                line += " --slow-firstpass";

            if (m.x264options.no_mbtree == true)
                line += " --no-mbtree";

            if (!m.x264options.no_mbtree && m.x264options.lookahead != 40)
                line += " --rc-lookahead " + m.x264options.lookahead;
                        
            line_turbo = line;

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

            if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
            {
                m.vpasses.Add("--pass 1 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }

            if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.vpasses.Add("--pass 1 " + line);
                m.vpasses.Add("--pass 3 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize)
            {
                m.vpasses.Add("--pass 1 " + line_turbo);
                m.vpasses.Add("--pass 2 " + line);
            }

            if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.vpasses.Add("--pass 1 " + line_turbo);
                m.vpasses.Add("--pass 3 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }
            
            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted)
            {
                try
                {
                    //запоминаем старый режим
                    oldmode = m.encodingmode;

                    string x264mode = combo_mode.SelectedItem.ToString();
                    if (x264mode == "1-Pass Bitrate")
                        m.encodingmode = Settings.EncodingModes.OnePass;

                    else if (x264mode == "2-Pass Bitrate")
                        m.encodingmode = Settings.EncodingModes.TwoPass;

                    else if (x264mode == "2-Pass Size")
                        m.encodingmode = Settings.EncodingModes.TwoPassSize;

                    else if (x264mode == "3-Pass Bitrate")
                        m.encodingmode = Settings.EncodingModes.ThreePass;

                    else if (x264mode == "3-Pass Size")
                        m.encodingmode = Settings.EncodingModes.ThreePassSize;

                    else if (x264mode == "Constant Quality")
                        m.encodingmode = Settings.EncodingModes.Quality;

                    else if (x264mode == "Constant Quantizer")
                        m.encodingmode = Settings.EncodingModes.Quantizer;

                    else if (x264mode == "2-Pass Quality")
                        m.encodingmode = Settings.EncodingModes.TwoPassQuality;

                    else if (x264mode == "3-Pass Quality")
                        m.encodingmode = Settings.EncodingModes.ThreePassQuality;

                    check_lossless.IsChecked = false;

                    SetMinMaxBitrate();

                    //сброс на квантайзер
                    if (oldmode == Settings.EncodingModes.OnePass ||
                        oldmode == Settings.EncodingModes.TwoPass ||
                        oldmode == Settings.EncodingModes.ThreePass ||
                        oldmode == Settings.EncodingModes.TwoPassSize ||
                        oldmode == Settings.EncodingModes.ThreePassSize)
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

                    if (m.encodingmode == Settings.EncodingModes.OnePass ||
                        m.encodingmode == Settings.EncodingModes.Quality ||
                        m.encodingmode == Settings.EncodingModes.Quantizer)
                    {
                        if (m.x264options.bframes > 1)
                        {
                            combo_bframe_mode.SelectedItem = "Spatial";
                            m.x264options.direct = "spatial";
                        }
                        else
                        {
                            combo_bframe_mode.SelectedItem = "Disabled";
                            m.x264options.direct = "none";
                        }
                    }
                    else
                    {
                        if (m.x264options.bframes > 1)
                        {
                            combo_bframe_mode.SelectedItem = "Auto";
                            m.x264options.direct = "auto";
                        }
                        else
                        {
                            combo_bframe_mode.SelectedItem = "Disabled";
                            m.x264options.direct = "none";
                        }
                    }

                    SetAVCProfile();
                    root_window.UpdateOutSize();
                    root_window.UpdateManualProfile();
                    DetectCodecPreset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            
        }

        private void SetDefaultBitrates()
        {
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.outvbitrate = 23;
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

        private void combo_level_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_level.IsDropDownOpen || combo_level.IsSelectionBoxHighlighted)
            {
                m.x264options.level = combo_level.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_lossless_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_lossless.IsFocused)
            {
                combo_mode.SelectedItem = "Constant Quantizer";
                m.encodingmode = Settings.EncodingModes.Quantizer;
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";

                //прогружаем битрейты
                m.outvbitrate = 0;
                num_bitrate.Value = (decimal)m.outvbitrate;

                SetAVCProfile();
                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_lossless_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_lossless.IsFocused)
            {
                m.outvbitrate = 23;
                num_bitrate.Value = (decimal)m.outvbitrate;

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_deblocking_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_deblocking.IsFocused)
            {
                m.x264options.deblocking = check_deblocking.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_deblocking_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_deblocking.IsFocused)
            {
                m.x264options.deblocking = check_deblocking.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_dstrength_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_dstrength.IsDropDownOpen || combo_dstrength.IsSelectionBoxHighlighted)
            {
                m.x264options.deblocks = Convert.ToInt32(combo_dstrength.SelectedItem);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_dthreshold_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_dthreshold.IsDropDownOpen || combo_dthreshold.IsSelectionBoxHighlighted)
            {
                m.x264options.deblockt = Convert.ToInt32(combo_dthreshold.SelectedItem);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_subme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_subme.IsDropDownOpen || combo_subme.IsSelectionBoxHighlighted)
            {
                m.x264options.subme = combo_subme.SelectedIndex + 1;

                //subme10
               // if (combo_subme.SelectedIndex == 9)
               // {
               //     //Включаем trellis=2
               //    if (m.x264options.trellis != 2)
               //     {
               //         m.x264options.trellis = 2;
               //         combo_trellis.SelectedItem = "2 - Always";
               //         SetAVCProfile();
               //     }
               //     //Включаем AQ=1
               //     if (m.x264options.aqmode == "Disabled")
               //     {
               //         m.x264options.aqmode = "1";
               //         combo_adapt_quant_mode.SelectedItem = "1";
               //     }
               //}               
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_me_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_me.IsDropDownOpen || combo_me.IsSelectionBoxHighlighted)
            {
                string me = combo_me.SelectedItem.ToString();
                if (me == "Diamond")
                    m.x264options.me = "dia";
                if (me == "Hexagon")
                    m.x264options.me = "hex";
                if (me == "Multi Hexagon")
                    m.x264options.me = "umh";
                if (me == "Exhaustive")
                    m.x264options.me = "esa";
                if (me == "SATD Exhaustive")
                    m.x264options.me = "tesa";

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_merange_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_merange.IsDropDownOpen || combo_merange.IsSelectionBoxHighlighted)
            {
                m.x264options.merange = Convert.ToInt32(combo_merange.SelectedItem);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_chroma_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_chroma.IsFocused)
            {
                m.x264options.chroma = check_chroma.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_chroma_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_chroma.IsFocused)
            {
                m.x264options.chroma = check_chroma.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted)
            {
                m.x264options.bframes = Convert.ToInt32(combo_bframes.SelectedItem);

                if (m.x264options.bframes == 0)
                {
                    //check_adaptb.IsChecked = false;
                    //m.x264options.badapt = false;
                    combo_bpyramid_mode.SelectedIndex = 0;
                    m.x264options.bpyramid = 0;
                    check_weightedb.IsChecked = false;
                    m.x264options.weightb = false;
                    combo_bframe_mode.SelectedItem = "Disabled";
                    m.x264options.direct = "none";
                }

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bframe_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bframe_mode.IsDropDownOpen || combo_bframe_mode.IsSelectionBoxHighlighted)
            {
                string bmode = combo_bframe_mode.SelectedItem.ToString();
                if (bmode == "Disabled")
                    m.x264options.direct = "none";
                if (bmode == "Spatial")
                    m.x264options.direct = "spatial";
                if (bmode == "Temporal")
                    m.x264options.direct = "temporal";
                if (bmode == "Auto")
                    m.x264options.direct = "auto";

                if (bmode != "Disabled" &&
                    m.x264options.bframes == 0)
                {
                    combo_bframes.SelectedItem = "1";
                    m.x264options.bframes = 1;
                }

                if (bmode == "Disabled")
                {
                    combo_bframes.SelectedItem = "0";
                    m.x264options.bframes = 0;
                }

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bpyramid_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_bpyramid_mode.IsDropDownOpen || combo_bpyramid_mode.IsSelectionBoxHighlighted)
            {
                if (combo_bpyramid_mode.SelectedItem.ToString() == "Strict")
                    m.x264options.bpyramid = 1;
                else if (combo_bpyramid_mode.SelectedItem.ToString() == "Normal")
                    m.x264options.bpyramid = 2;
                else
                    m.x264options.bpyramid = 0;

                if (m.x264options.bframes == 0)
                {
                    combo_bframes.SelectedItem = "1";
                    m.x264options.bframes = 1;
                }

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_weightedb_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_weightedb.IsFocused)
            {
                m.x264options.weightb = check_weightedb.IsChecked.Value;

                if (m.x264options.bframes == 0)
                {
                    combo_bframes.SelectedItem = "1";
                    m.x264options.bframes = 1;
                }

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_weightedb_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_weightedb.IsFocused)
            {
                m.x264options.weightb = check_weightedb.IsChecked.Value;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_8x8dct_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_8x8dct.IsFocused)
            {
                m.x264options.adaptivedct = check_8x8dct.IsChecked.Value;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_8x8dct_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_8x8dct.IsFocused)
            {
                m.x264options.adaptivedct = check_8x8dct.IsChecked.Value;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_i4x4_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_i4x4.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_i4x4_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_i4x4.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_p4x4_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_p4x4.IsFocused)
            {
                if (!check_p8x8.IsChecked.Value)
                    check_p8x8.IsChecked = true;
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_p4x4_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_p4x4.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_i8x8_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_i8x8.IsFocused)
            {
                if (!check_8x8dct.IsChecked.Value)
                    check_8x8dct.IsChecked = true;
                m.x264options.adaptivedct = true;
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_i8x8_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_i8x8.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_p8x8_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_p8x8.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_p8x8_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_p8x8.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_b8x8_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_b8x8.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_b8x8_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_b8x8.IsFocused)
            {
                m.x264options.analyse = GetMackroblocks();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_trellis_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_trellis.IsDropDownOpen || combo_trellis.IsSelectionBoxHighlighted)
            {
                string trellis = combo_trellis.SelectedItem.ToString();
                if (trellis == "0 - Disabled")
                    m.x264options.trellis = 0;
                if (trellis == "1 - Final MB")
                    m.x264options.trellis = 1;
                if (trellis == "2 - Always")
                    m.x264options.trellis = 2;

                //subme10
               // if (m.x264options.trellis != 2 && m.x264options.subme == 10)
               // {
               //     m.x264options.subme = 9;
               //     combo_subme.SelectedIndex = 8;
               // }

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_ref_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_ref.IsDropDownOpen || combo_ref.IsSelectionBoxHighlighted)
            {
                m.x264options.reference = Convert.ToInt32(combo_ref.SelectedItem);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_min_quant_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_min_quant.IsDropDownOpen || combo_min_quant.IsSelectionBoxHighlighted)
            {
                m.x264options.minquant = Convert.ToInt32(combo_min_quant.SelectedItem);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_mixed_ref_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_mixed_ref.IsFocused)
            {
                m.x264options.mixedrefs = check_mixed_ref.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_mixed_ref_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_mixed_ref.IsFocused)
            {
                m.x264options.mixedrefs = check_mixed_ref.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_cabac_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_cabac.IsFocused)
            {
                m.x264options.cabac = check_cabac.IsChecked.Value;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_cabac_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_cabac.IsFocused)
            {
                m.x264options.cabac = check_cabac.IsChecked.Value;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_fast_pskip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_fast_pskip.IsFocused)
            {
                m.x264options.fastpskip = check_fast_pskip.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_fast_pskip_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_fast_pskip.IsFocused)
            {
                m.x264options.fastpskip = check_fast_pskip.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_dct_decimate_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_dct_decimate.IsFocused)
            {
                m.x264options.dctdecimate = check_dct_decimate.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_dct_decimate_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_dct_decimate.IsFocused)
            {
                m.x264options.dctdecimate = check_dct_decimate.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_avc_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_avc_profile.IsDropDownOpen || combo_avc_profile.IsSelectionBoxHighlighted)
            {
                string avcprofile = combo_avc_profile.SelectedItem.ToString();
                if (avcprofile == "Baseline Profile")
                {
                    m.x264options.cabac = false;
                    check_cabac.IsChecked = false;
                    m.x264options.bframes = 0;
                    combo_bframes.SelectedItem = 0;
                    m.x264options.trellis = 0;
                    combo_trellis.SelectedItem = "Disabled";
                    //check_adaptb.IsChecked = false;
                    //m.x264options.badapt = false;
                    combo_badapt_mode.SelectedValue = "Disabled";
                    m.x264options.b_adapt = 0;
                    combo_bpyramid_mode.SelectedIndex = 0;
                    m.x264options.bpyramid = 0;
                    check_weightedb.IsChecked = false;
                    m.x264options.weightb = false;
                    m.x264options.direct = "none";
                    combo_bframe_mode.SelectedItem = "Disabled";
                    m.x264options.adaptivedct = false;
                    check_8x8dct.IsChecked = false;
                    check_i8x8.IsChecked = false;
                    check_i4x4.IsChecked = false;
                    m.x264options.analyse = GetMackroblocks();
                }

                if (avcprofile == "Main Profile")
                {
                    m.x264options.cabac = true;
                    check_cabac.IsChecked = true;
                    if (m.x264options.bframes == 0)
                    {
                        m.x264options.bframes = 3;
                        combo_bframes.SelectedItem = 3;
                    }
                    m.x264options.trellis = 2;
                    combo_trellis.SelectedItem = "2 - Always";
                    combo_badapt_mode.SelectedValue = "Fast";
                    m.x264options.b_adapt = 1;
                    check_weightedb.IsChecked = true;
                    m.x264options.weightb = true;
                    if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                        m.encodingmode == Settings.EncodingModes.ThreePass ||
                        m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                        m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                        m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    {
                        m.x264options.direct = "auto";
                        combo_bframe_mode.SelectedItem = "Auto";
                    }
                    else
                    {
                        m.x264options.direct = "spatial";
                        combo_bframe_mode.SelectedItem = "Spatial";
                    }
                    m.x264options.adaptivedct = false;
                    check_8x8dct.IsChecked = true;
                    check_i8x8.IsChecked = false;
                    check_i4x4.IsChecked = false;
                    m.x264options.analyse = GetMackroblocks();
                }

                if (avcprofile == "High Profile")
                {
                    m.x264options.cabac = true;
                    check_cabac.IsChecked = true;
                    if (m.x264options.bframes == 0)
                    {
                        m.x264options.bframes = 3;
                        combo_bframes.SelectedItem = 3;
                    }
                    m.x264options.trellis = 2;
                    combo_trellis.SelectedItem = "2 - Always";
                    combo_badapt_mode.SelectedValue = "Fast";
                    m.x264options.b_adapt = 1;
                    check_weightedb.IsChecked = true;
                    m.x264options.weightb = true;
                    if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                        m.encodingmode == Settings.EncodingModes.ThreePass ||
                        m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                        m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    {
                        m.x264options.direct = "auto";
                        combo_bframe_mode.SelectedItem = "Auto";
                    }
                    else
                    {
                        m.x264options.direct = "spatial";
                        combo_bframe_mode.SelectedItem = "Spatial";
                    }
                    m.x264options.adaptivedct = true;
                    check_8x8dct.IsChecked = true;
                    check_i8x8.IsChecked = true;
                    check_i4x4.IsChecked = true;
                    check_p4x4.IsChecked = true;
                    check_p8x8.IsChecked = true;
                    check_b8x8.IsChecked = true;
                    m.x264options.analyse = GetMackroblocks();
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void DetectCodecPreset()
        {
            CodecPresets preset = CodecPresets.Custom;

            //Default
            if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 0 &&
                    m.x264options.cabac == true &&
                    m.x264options.chroma == true &&
                    m.x264options.custommatrix == null &&
                    m.x264options.dctdecimate == true &&
                    m.x264options.deblocking == true &&
                    m.x264options.deblocks == 0 &&
                    m.x264options.deblockt == 0 &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.fastpskip == true &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "hex" &&
                    m.x264options.merange == 16 &&
                    m.x264options.minquant == 10 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.reference == 3 &&
                    m.x264options.subme == 7 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true &&
                    m.x264options.slow_frstpass == false)

                preset = CodecPresets.Default;

                  
            //Fast
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 0 &&
                    m.x264options.cabac == true &&
                    m.x264options.custommatrix == null &&
                    m.x264options.dctdecimate == true &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.fastpskip == true &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "hex" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == false &&
                    m.x264options.reference == 2 &&
                    m.x264options.subme == 5 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true)
                preset = CodecPresets.Fast;

            //Slow
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 0 &&
                    m.x264options.cabac == true &&
                    m.x264options.chroma == true &&
                    m.x264options.custommatrix == null &&
                    m.x264options.dctdecimate == true &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.fastpskip == true &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "umh" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.reference == 5 &&
                    m.x264options.subme == 8 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true)
                preset = CodecPresets.Slow;

            //Slower
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "all" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 0 &&
                    m.x264options.cabac == true &&
                    m.x264options.chroma == true &&
                    m.x264options.custommatrix == null &&
                    m.x264options.dctdecimate == true &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.fastpskip == true &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "umh" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.reference == 8 &&
                    m.x264options.subme == 9 &&
                    m.x264options.trellis == 2 &&
                    m.x264options.weightb == true)
                preset = CodecPresets.Slower;

            //Placebo
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "all" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 16 &&
                    m.x264options.bpyramid == 0 &&
                    m.x264options.cabac == true &&
                    m.x264options.chroma == true &&
                    m.x264options.custommatrix == null &&
                    m.x264options.dctdecimate == true &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "tesa" &&
                    m.x264options.merange == 24 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.reference == 16 &&
                    m.x264options.subme == 9 &&
                    m.x264options.trellis == 2 &&
                    m.x264options.weightb == true)
                preset = CodecPresets.Placebo;

            combo_codec_preset.SelectedItem = preset.ToString();

        }

        private void combo_codec_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_codec_preset.IsDropDownOpen || combo_codec_preset.IsSelectionBoxHighlighted)
            {
                CodecPresets preset = (CodecPresets)Enum.Parse(typeof(CodecPresets), combo_codec_preset.SelectedItem.ToString());

                if (preset == CodecPresets.Default) //туууут
                {
                    m.x264options.adaptivedct = true;
                    m.x264options.analyse = "p8x8,b8x8,i8x8,i4x4";
                    m.x264options.b_adapt = 1;
                    m.x264options.bframes = 3;
                    m.x264options.bpyramid = 0;
                    m.x264options.cabac = true;
                    m.x264options.chroma = true;
                    m.x264options.custommatrix = null;
                    m.x264options.dctdecimate = true;
                    m.x264options.deblocking = true;
                    m.x264options.deblocks = 0;
                    m.x264options.deblockt = 0;
                    m.x264options.direct = "spatial";
                    m.x264options.fastpskip = true;
                    m.x264options.level = Format.GetValidAVCLevel(m);
                    m.x264options.me = "hex";
                    m.x264options.merange = 16;
                    m.x264options.minquant = 10;
                    m.x264options.mixedrefs = true;
                    m.x264options.reference = 3;
                    m.x264options.subme = 7;
                    m.x264options.trellis = 1;
                    m.x264options.weightb = true;

                    m.x264options.aqmode = "1";
                    m.x264options.aqstrength = "1.0";
                    m.x264options.psytrellis = 0;
                    m.x264options.psyrdo = 1;
                    m.x264options.vbv_bufsize = 0;
                    m.x264options.vbv_maxrate = 0;
                    m.x264options.qcomp = 0.6m;
                    m.x264options.qp_offset = "0";
                    m.x264options.threads = "auto";
                    m.x264options.slow_frstpass = false;


                    SetDefaultBitrates();
                }

                if (preset == CodecPresets.Fast)
                {
                    m.x264options.adaptivedct = true;
                    m.x264options.analyse = "p8x8,b8x8,i8x8,i4x4";
                    m.x264options.b_adapt = 1;
                    m.x264options.bframes = 3;
                    m.x264options.bpyramid = 0;
                    m.x264options.cabac = true;
                    m.x264options.chroma = true;
                    m.x264options.custommatrix = null;
                    m.x264options.dctdecimate = true;
                    m.x264options.deblocking = true;
                    m.x264options.direct = "spatial";
                    m.x264options.fastpskip = true;
                    m.x264options.level = Format.GetValidAVCLevel(m);
                    m.x264options.me = "hex";
                    m.x264options.merange = 16;
                    m.x264options.mixedrefs = false;
                    m.x264options.reference = 2;
                    m.x264options.subme = 5;
                    m.x264options.trellis = 1;
                    m.x264options.weightb = true;
                }

                if (preset == CodecPresets.Slow)
                {
                    m.x264options.adaptivedct = true;
                    m.x264options.analyse = "p8x8,b8x8,i8x8,i4x4";
                    m.x264options.b_adapt = 2;
                    m.x264options.bframes = 3;
                    m.x264options.bpyramid = 0;
                    m.x264options.cabac = true;
                    m.x264options.chroma = true;
                    m.x264options.custommatrix = null;
                    m.x264options.dctdecimate = true;
                    m.x264options.deblocking = true;
                    m.x264options.direct = "auto";
                    m.x264options.fastpskip = true;
                    m.x264options.level = Format.GetValidAVCLevel(m);
                    m.x264options.me = "umh";
                    m.x264options.merange = 16;
                    m.x264options.mixedrefs = true;
                    m.x264options.reference = 5;
                    m.x264options.subme = 8;
                    m.x264options.trellis = 1;
                    m.x264options.weightb = true;
                }

                if (preset == CodecPresets.Slower)
                {
                    m.x264options.adaptivedct = true;
                    m.x264options.analyse = "all";
                    m.x264options.b_adapt = 2;
                    m.x264options.bframes = 3;
                    m.x264options.bpyramid = 0;
                    m.x264options.cabac = true;
                    m.x264options.chroma = true;
                    m.x264options.custommatrix = null;
                    m.x264options.dctdecimate = true;
                    m.x264options.deblocking = true;
                    m.x264options.direct = "auto";
                    m.x264options.fastpskip = true;
                    m.x264options.level = Format.GetValidAVCLevel(m);
                    m.x264options.me = "umh";
                    m.x264options.merange = 16;
                    m.x264options.mixedrefs = true;
                    m.x264options.reference = 8;
                    m.x264options.subme = 9;
                    m.x264options.trellis = 2;
                    m.x264options.weightb = true;
                }

                if (preset == CodecPresets.Placebo)
                {
                    m.x264options.adaptivedct = true;
                    m.x264options.analyse = "all";
                    m.x264options.b_adapt = 2;
                    m.x264options.bframes = 16;
                    m.x264options.bpyramid = 0;
                    m.x264options.cabac = true;
                    m.x264options.chroma = true;
                    m.x264options.custommatrix = null;
                    m.x264options.dctdecimate = true;
                    m.x264options.deblocking = true;
                    m.x264options.direct = "auto";
                    m.x264options.fastpskip = false;
                    m.x264options.level = Format.GetValidAVCLevel(m);
                    m.x264options.me = "tesa";
                    m.x264options.merange = 24;
                    m.x264options.mixedrefs = true;
                    m.x264options.reference = 16;
                    m.x264options.subme = 9;
                    m.x264options.trellis = 2;
                    m.x264options.weightb = true;
                }

                if (preset != CodecPresets.Custom)
                {
                    LoadFromProfile();
                    root_window.UpdateOutSize();
                    root_window.UpdateManualProfile();
                }
            }
        }

        private void SetMinMaxBitrate()
        {
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                num_bitrate.DecimalPlaces = 1;
                num_bitrate.Change = 0.1m;
            }
            else
            {
                num_bitrate.DecimalPlaces = 0;
                num_bitrate.Change = 1;
                m.outvbitrate = (int)m.outvbitrate;
            }

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = Format.GetMaxVBitrate(m);
            }

            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                num_bitrate.Minimum = 0;
                num_bitrate.Maximum = 51;
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = 50000;
            }
        }

        private void num_bitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bitrate.IsAction)
            {
                m.outvbitrate = num_bitrate.Value;

                if (m.encodingmode == Settings.EncodingModes.Quantizer)
                {
                    if (m.outvbitrate == 0)
                    {
                        if (!check_lossless.IsChecked.Value)
                            check_lossless.IsChecked = true;
                    }
                    else
                    {
                        if (check_lossless.IsChecked.Value)
                            check_lossless.IsChecked = false;
                    }
                }

                SetAVCProfile();
                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_psyrdo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_psyrdo.IsAction)
            {
                m.x264options.psyrdo = num_psyrdo.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_psytrellis_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_psytrellis.IsAction)
            {
                m.x264options.psytrellis = num_psytrellis.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_adapt_quant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_adapt_quant.IsDropDownOpen || combo_adapt_quant.IsSelectionBoxHighlighted)
            {
                m.x264options.aqstrength = combo_adapt_quant.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_adapt_quant_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_adapt_quant_mode.IsDropDownOpen || combo_adapt_quant_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.aqmode = combo_adapt_quant_mode.SelectedItem.ToString();

                //subme10
                //if (m.x264options.aqmode != "2" && m.x264options.subme == 10)
                //{
                //    m.x264options.subme = 9;
                //    combo_subme.SelectedIndex = 8;
                //}
                
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void textbox_string1_TextChanged(object sender, TextChangedEventArgs e)//прес
        {
            m.userstring1 = textbox_string1.Text;
        }

        private void textbox_string2_TextChanged(object sender, TextChangedEventArgs e)
        {
            m.userstring2 = textbox_string2.Text;
        }

        private void textbox_string3_TextChanged(object sender, TextChangedEventArgs e)
        {
            m.userstring3 = textbox_string3.Text;
        }

        private void combo_threads_count_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_threads_count.IsDropDownOpen || combo_threads_count.IsSelectionBoxHighlighted)
            {
                if (combo_threads_count.SelectedItem.ToString() == "Auto")
                    m.x264options.threads = "auto";
                else
                    m.x264options.threads = combo_threads_count.SelectedItem.ToString();

                root_window.UpdateManualProfile();
                DetectCodecPreset();

            }
                
                
                //Settings.ThreadsX264 = combo_threads_count.SelectedItem.ToString();
        }

        private void combo_badapt_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_badapt_mode.IsDropDownOpen || combo_badapt_mode.IsSelectionBoxHighlighted)
            {
                string b_adapt = combo_badapt_mode.SelectedItem.ToString();
                if (b_adapt == "Disabled")
                    m.x264options.b_adapt = 0;
                if (b_adapt == "Fast")
                    m.x264options.b_adapt = 1;
                if (b_adapt == "Optimal")
                    m.x264options.b_adapt = 2;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }

        }

        private void num_qcomp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_qcomp.IsAction)
            {
                m.x264options.qcomp = num_qcomp.Value;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_vbv_buf_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_buf.IsAction)
            {
                m.x264options.vbv_bufsize = Convert.ToInt32(num_vbv_buf.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_vbv_max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_max.IsAction)
            {
                m.x264options.vbv_maxrate = Convert.ToInt32(num_vbv_max.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_chroma_qp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_chroma_qp.IsDropDownOpen || combo_chroma_qp.IsSelectionBoxHighlighted)
            {
                m.x264options.qp_offset = combo_chroma_qp.SelectedItem.ToString();

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_slow_first_Click(object sender, RoutedEventArgs e)
        {
            if (check_slow_first.IsFocused)
            {
                m.x264options.slow_frstpass = check_slow_first.IsChecked.Value;
                
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_nombtree_Clicked(object sender, RoutedEventArgs e)
        {
            if (check_nombtree.IsFocused)
            {
                m.x264options.no_mbtree = check_nombtree.IsChecked.Value;
                if (m.x264options.no_mbtree == true)
                    num_lookahead.IsEnabled = false;
                else
                    num_lookahead.IsEnabled = true;

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_lookahead_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_lookahead.IsAction)
            {
                m.x264options.lookahead = Convert.ToInt32(num_lookahead.Value);

                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_enable_psy_Checked(object sender, RoutedEventArgs e)
        {
            if (check_enable_psy.IsFocused)
            {
                m.x264options.no_psy = !check_enable_psy.IsChecked.Value;

                if (m.x264options.no_psy == true)
                {
                    num_psyrdo.IsEnabled = false;
                    num_psytrellis.IsEnabled = false;
                }
                else
                {
                    num_psyrdo.IsEnabled = true;
                    num_psytrellis.IsEnabled = true;
                }
                
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

     }
}