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
    public partial class x262
    {
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum Presets { Ultrafast = 0, Superfast, Veryfast, Faster, Fast, Medium, Slow, Slower, Veryslow, Placebo }
        public enum Tunes { None = 0, Film, Animation, Grain, StillImage, PSNR, SSIM, FastDecode }
        public enum Profiles { Auto = 0, Simple, Main, High }
        private ArrayList good_cli = null;

        public x262(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
        {
            this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = VideoEncWindow;
            this.p = parent;

            //Mode
            combo_mode.Items.Add("1-Pass Bitrate");
            combo_mode.Items.Add("2-Pass Bitrate");
            combo_mode.Items.Add("3-Pass Bitrate");
            combo_mode.Items.Add("Constant Quality");
            combo_mode.Items.Add("Constant Quantizer");
            combo_mode.Items.Add("2-Pass Quality");
            combo_mode.Items.Add("3-Pass Quality");
            combo_mode.Items.Add("2-Pass Size");
            combo_mode.Items.Add("3-Pass Size");

            //Profile
            combo_mpg_profile.Items.Add(new ComboBoxItem { Content = "Auto" });
            combo_mpg_profile.Items.Add("Simple Profile");
            combo_mpg_profile.Items.Add("Main Profile");
            combo_mpg_profile.Items.Add("High Profile");

            //Level
            combo_level.Items.Add("Unrestricted");
            combo_level.Items.Add("Low");
            combo_level.Items.Add("Main");
            combo_level.Items.Add("High");
            combo_level.Items.Add("High-1440");
            combo_level.Items.Add("HighP");

            //Tune
            combo_tune.Items.Add("None");
            combo_tune.Items.Add("Film");
            combo_tune.Items.Add("Animation");
            combo_tune.Items.Add("Grain");
            combo_tune.Items.Add("Still Image");
            combo_tune.Items.Add("PSNR");
            combo_tune.Items.Add("SSIM");
            combo_tune.Items.Add("Fast Decode");

            //Adaptive Quantization
            for (double n = 0.1; n <= 2.1; n += 0.1)
                combo_adapt_quant.Items.Add(Calculate.ConvertDoubleToPointString(n, 1));

            combo_adapt_quant_mode.Items.Add("None");
            combo_adapt_quant_mode.Items.Add("VAQ");
            combo_adapt_quant_mode.Items.Add("A-VAQ");

            //Прописываем subme
            combo_subme.Items.Add("0 - Fullpel only");
            combo_subme.Items.Add("1 - QPel SAD");
            combo_subme.Items.Add("2 - QPel SATD");
            combo_subme.Items.Add("3 - HPel on MB then QPel");
            combo_subme.Items.Add("4 - Always QPel");
            combo_subme.Items.Add("5 - QPel & Bidir ME");
            combo_subme.Items.Add("6 - RD on I/P frames");
            combo_subme.Items.Add("7 - RD on all frames");
            combo_subme.Items.Add("8 - RD refinement on I/P frames");
            combo_subme.Items.Add("9 - RD refinement on all frames");

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

            //DC precision
            for (int n = 8; n <= 11; n++)
                combo_dc_precision.Items.Add(n);

            combo_open_gop.Items.Add("No");
            combo_open_gop.Items.Add("Yes");

            //Кол-во потоков для lookahead
            combo_lookahead_threads.Items.Add("Auto");
            for (int n = 1; n <= 10; n++)
                combo_lookahead_threads.Items.Add(Convert.ToString(n));

            //Кол-во потоков для x262-го
            combo_threads_count.Items.Add("Auto");
            combo_threads_count.Items.Add("1");
            combo_threads_count.Items.Add("1+1"); //+ --thread-input
            for (int n = 2; n <= 32; n++)
                combo_threads_count.Items.Add(Convert.ToString(n));

            //-b-adapt
            combo_badapt_mode.Items.Add("Disabled");
            combo_badapt_mode.Items.Add("Fast");
            combo_badapt_mode.Items.Add("Optimal");

            combo_range_in.Items.Add("Auto");
            combo_range_in.Items.Add("TV");
            combo_range_in.Items.Add("PC");

            combo_range_out.Items.Add("Auto");
            combo_range_out.Items.Add("TV");
            combo_range_out.Items.Add("PC");

            combo_colorprim.Items.Add("Undefined");
            combo_colorprim.Items.Add("bt709");
            combo_colorprim.Items.Add("bt470m");
            combo_colorprim.Items.Add("bt470bg");
            combo_colorprim.Items.Add("smpte170m");
            combo_colorprim.Items.Add("smpte240m");
            combo_colorprim.Items.Add("film");

            combo_transfer.Items.Add("Undefined");
            combo_transfer.Items.Add("bt709");
            combo_transfer.Items.Add("bt470m");
            combo_transfer.Items.Add("bt470bg");
            combo_transfer.Items.Add("linear");
            combo_transfer.Items.Add("log100");
            combo_transfer.Items.Add("log316");
            combo_transfer.Items.Add("smpte170m");
            combo_transfer.Items.Add("smpte240m");

            combo_colormatrix.Items.Add("Undefined");
            combo_colormatrix.Items.Add("bt709");
            combo_colormatrix.Items.Add("fcc");
            combo_colormatrix.Items.Add("bt470bg");
            combo_colormatrix.Items.Add("smpte170m");
            combo_colormatrix.Items.Add("smpte240m");
            combo_colormatrix.Items.Add("GBR");
            combo_colormatrix.Items.Add("YCgCo");

            combo_colorspace.Items.Add("I420");
            combo_colorspace.Items.Add("I422");

            text_mode.Content = Languages.Translate("Encoding mode") + ":";
            Apply_CLI.Content = Languages.Translate("Apply");
            Reset_CLI.Content = Languages.Translate("Reset");
            x262_help.Content = Languages.Translate("Help");
            Reset_CLI.ToolTip = "Reset to last good CLI";
            x262_help.ToolTip = "Show x262.exe --fullhelp screen";

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
                if (m.encodingmode == Settings.EncodingModes.Quantizer)
                    text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                else
                    text_bitrate.Content = Languages.Translate("Quality") + ": (CRF)";

                num_bitrate.Value = (decimal)m.outvbitrate;
            }

            //Встроенный в x262 пресет
            text_preset_name.Content = m.x262options.preset.ToString();
            slider_preset.Value = (int)m.x262options.preset;

            //Level
            if (m.x262options.level == "low") combo_level.SelectedIndex = 1;
            else if (m.x262options.level == "main") combo_level.SelectedIndex = 2;
            else if (m.x262options.level == "high") combo_level.SelectedIndex = 3;
            else if (m.x262options.level == "high-1440") combo_level.SelectedIndex = 4;
            else if (m.x262options.level == "highp") combo_level.SelectedIndex = 5;
            else combo_level.SelectedIndex = 0;

            //Tune
            combo_tune.SelectedIndex = (int)m.x262options.tune;

            combo_adapt_quant_mode.SelectedIndex = m.x262options.aqmode;
            combo_adapt_quant.SelectedItem = m.x262options.aqstrength;
            
            num_psyrdo.Value = m.x262options.psyrdo;
            num_qcomp.Value = m.x262options.qcomp;
            num_vbv_max.Value = m.x262options.vbv_maxrate;
            num_vbv_buf.Value = m.x262options.vbv_bufsize;
            num_vbv_init.Value = m.x262options.vbv_init;

            //Прописываем subme
            combo_subme.SelectedIndex = m.x262options.subme;

            //прописываем me алгоритм
            if (m.x262options.me == "dia") combo_me.SelectedIndex = 0;
            else if (m.x262options.me == "hex") combo_me.SelectedIndex = 1;
            else if (m.x262options.me == "umh") combo_me.SelectedIndex = 2;
            else if (m.x262options.me == "esa") combo_me.SelectedIndex = 3;
            else if (m.x262options.me == "tesa") combo_me.SelectedIndex = 4;

            //прописываем me range
            combo_merange.SelectedItem = m.x262options.merange;

            //прописываем chroma me
            check_chroma.IsChecked = m.x262options.no_chroma;

            //B фреймы
            combo_bframes.SelectedItem = m.x262options.bframes;

            combo_dc_precision.SelectedItem = m.x262options.dc;

            if (!m.x262options.open_gop) combo_open_gop.SelectedIndex = 0;
            else combo_open_gop.SelectedIndex = 1;

            //altscan
            check_altscan.IsChecked = m.x262options.altscan;

            //fast p skip
            check_fast_pskip.IsChecked = m.x262options.no_fastpskip;

            //linear quantization
            check_linear_q.IsChecked = m.x262options.linear_q;

            //min-max quantizer
            num_min_quant.Value = m.x262options.min_quant;
            num_max_quant.Value = m.x262options.max_quant;
            num_step_quant.Value = m.x262options.step_quant;

            num_min_gop.Value = m.x262options.gop_min;
            num_max_gop.Value = m.x262options.gop_max;

            combo_badapt_mode.SelectedIndex = m.x262options.b_adapt;
            num_chroma_qp.Value = m.x262options.qp_offset;
            check_slow_first.IsChecked = m.x262options.slow_frstpass;
            check_nombtree.IsChecked = m.x262options.no_mbtree;
            num_lookahead.Value = m.x262options.lookahead;

            if (m.x262options.lookahead_threads == "auto") combo_lookahead_threads.SelectedIndex = 0;
            else combo_lookahead_threads.SelectedItem = m.x262options.lookahead_threads;

            check_enable_psy.IsChecked = !m.x262options.no_psy;
            num_ratio_ip.Value = m.x262options.ratio_ip;
            num_ratio_pb.Value = m.x262options.ratio_pb;
            num_slices.Value = m.x262options.slices;
            check_fake_int.IsChecked = m.x262options.fake_int;

            if (m.x262options.range_in == "auto") combo_range_in.SelectedIndex = 0;
            else if (m.x262options.range_in == "tv") combo_range_in.SelectedIndex = 1;
            else if (m.x262options.range_in == "pc") combo_range_in.SelectedIndex = 2;

            if (m.x262options.range_out == "auto") combo_range_out.SelectedIndex = 0;
            else if (m.x262options.range_out == "tv") combo_range_out.SelectedIndex = 1;
            else if (m.x262options.range_out == "pc") combo_range_out.SelectedIndex = 2;

            combo_colorprim.SelectedItem = m.x262options.colorprim;
            combo_transfer.SelectedItem = m.x262options.transfer;
            combo_colormatrix.SelectedItem = m.x262options.colormatrix;
            combo_colorspace.SelectedItem = m.x262options.colorspace;
            check_non_deterministic.IsChecked = m.x262options.non_deterministic;
            check_bluray.IsChecked = m.x262options.bluray;

            //Кол-во потоков для x262-го
            if (m.x262options.threads == "auto") combo_threads_count.SelectedIndex = 0;
            else if (m.x262options.threads == "1" && m.x262options.thread_input) combo_threads_count.SelectedIndex = 2;
            else combo_threads_count.SelectedItem = m.x262options.threads;

            //Включаем-выключаем элементы.
            //Сначала на основе --preset.
            if (m.x262options.preset == Presets.Ultrafast)
            {
                check_slow_first.IsEnabled = !m.x262options.extra_cli.Contains("--slow-firstpass");
                check_fast_pskip.IsEnabled = !m.x262options.extra_cli.Contains("--no-fast-pskip");
                check_nombtree.IsEnabled = false;
            }
            else if (m.x262options.preset == Presets.Superfast)
            {
                check_slow_first.IsEnabled = !m.x262options.extra_cli.Contains("--slow-firstpass");
                check_fast_pskip.IsEnabled = !m.x262options.extra_cli.Contains("--no-fast-pskip");
                check_nombtree.IsEnabled = false;
            }
            else if (m.x262options.preset == Presets.Veryfast || m.x262options.preset == Presets.Faster)
            {
                check_slow_first.IsEnabled = !m.x262options.extra_cli.Contains("--slow-firstpass");
                check_fast_pskip.IsEnabled = !m.x262options.extra_cli.Contains("--no-fast-pskip");
                check_nombtree.IsEnabled = !m.x262options.extra_cli.Contains("--no-mbtree");
            }
            else if (m.x262options.preset == Presets.Placebo)
            {
                check_slow_first.IsEnabled = false;
                check_fast_pskip.IsEnabled = false;
                check_nombtree.IsEnabled = !m.x262options.extra_cli.Contains("--no-mbtree");
            }
            else
            {
                //Для остальных
                check_slow_first.IsEnabled = !m.x262options.extra_cli.Contains("--slow-firstpass");
                check_fast_pskip.IsEnabled = !m.x262options.extra_cli.Contains("--no-fast-pskip");
                check_nombtree.IsEnabled = !m.x262options.extra_cli.Contains("--no-mbtree");
            }

            //Tune Grain
            /*if (m.x262options.tune == Tunes.Grain)
            {
            }
            else
            {
            }*/

            //Tune PSNR и SSIM
            if (m.x262options.tune == Tunes.PSNR || m.x262options.tune == Tunes.SSIM)
            {
                num_psyrdo.IsEnabled = check_enable_psy.IsEnabled = false;
            }
            else
            {
                check_enable_psy.IsEnabled = !m.x262options.extra_cli.Contains("--no-psy");
                num_psyrdo.IsEnabled = (!m.x262options.no_psy && !m.x262options.extra_cli.Contains("--psy-rd "));
            }

            //Tune FastDecode
            /*if (m.x262options.tune == Tunes.FastDecode)
            {
            }
            else
            {
            }*/

            //Теперь на основе содержимого extra_cli
            combo_level.IsEnabled = !m.x262options.extra_cli.Contains("--level ");
            combo_subme.IsEnabled = !m.x262options.extra_cli.Contains("--subme ");
            combo_me.IsEnabled = !m.x262options.extra_cli.Contains("--me ");
            combo_merange.IsEnabled = !m.x262options.extra_cli.Contains("--merange ");
            check_chroma.IsEnabled = !m.x262options.extra_cli.Contains("--no-chroma-me");
            combo_bframes.IsEnabled = !m.x262options.extra_cli.Contains("--bframes ");
            combo_dc_precision.IsEnabled = !m.x262options.extra_cli.Contains("--dc ");
            combo_badapt_mode.IsEnabled = !m.x262options.extra_cli.Contains("--b-adapt ");
            check_altscan.IsEnabled = !m.x262options.extra_cli.Contains("--altscan");
            num_lookahead.IsEnabled = !m.x262options.extra_cli.Contains("--rc-lookahead ");
            combo_lookahead_threads.IsEnabled = !m.x262options.extra_cli.Contains("--lookahead-threads ");
            combo_adapt_quant_mode.IsEnabled = !m.x262options.extra_cli.Contains("--aq-mode ");
            combo_adapt_quant.IsEnabled = !m.x262options.extra_cli.Contains("--ag-strength ");
            num_vbv_max.IsEnabled = !m.x262options.extra_cli.Contains("--vbv-maxrate ");
            num_vbv_buf.IsEnabled = !m.x262options.extra_cli.Contains("--vbv-bufsize ");
            num_vbv_init.IsEnabled = !m.x262options.extra_cli.Contains("--vbv-init ");
            num_min_gop.IsEnabled = !m.x262options.extra_cli.Contains("--min-keyint ");
            num_max_gop.IsEnabled = !m.x262options.extra_cli.Contains("--keyint ");
            num_min_quant.IsEnabled = !m.x262options.extra_cli.Contains("--qpmin ");
            num_max_quant.IsEnabled = !m.x262options.extra_cli.Contains("--qpmax ");
            num_step_quant.IsEnabled = !m.x262options.extra_cli.Contains("--qpstep ");
            num_qcomp.IsEnabled = !m.x262options.extra_cli.Contains("--qcomp ");
            num_chroma_qp.IsEnabled = !m.x262options.extra_cli.Contains("--chroma-qp-offset ");
            num_ratio_ip.IsEnabled = !m.x262options.extra_cli.Contains("--ipratio ");
            num_ratio_pb.IsEnabled = !m.x262options.extra_cli.Contains("--pbratio ");
            combo_open_gop.IsEnabled = !m.x262options.extra_cli.Contains("--open-gop");
            num_slices.IsEnabled = !m.x262options.extra_cli.Contains("--slices ");
            check_fake_int.IsEnabled = !m.x262options.extra_cli.Contains("--fake-interlaced");
            combo_range_in.IsEnabled = !m.x262options.extra_cli.Contains("--input-range ");
            combo_range_out.IsEnabled = !m.x262options.extra_cli.Contains("--range ");
            combo_colorprim.IsEnabled = !m.x262options.extra_cli.Contains("--colorprim ");
            combo_transfer.IsEnabled = !m.x262options.extra_cli.Contains("--transfer ");
            combo_colormatrix.IsEnabled = !m.x262options.extra_cli.Contains("--colormatrix ");
            combo_colorspace.IsEnabled = !m.x262options.extra_cli.Contains("--output-csp ");
            check_non_deterministic.IsEnabled = !m.x262options.extra_cli.Contains("--non-deterministic");
            check_bluray.IsEnabled = !m.x262options.extra_cli.Contains("--bluray-compat");

            SetMPEG2Profile();
            SetToolTips();
            UpdateCLI();
        }

        private void SetMPEG2Profile()
        {
            //x262.exe encoder/set.c
            /*if( param->b_mpeg2 )
            {
                if( param->i_intra_dc_precision > X264_INTRA_DC_10_BIT || param->b_high_profile )
                    sps->i_profile_idc = MPEG2_PROFILE_HIGH;
                else if( sps->i_chroma_format_idc == CHROMA_422 || param->b_422_profile )
                    sps->i_profile_idc = MPEG2_PROFILE_422;
                else if( param->i_bframe > 0 || param->b_interlaced || param->b_fake_interlaced || param->b_main_profile )
                    sps->i_profile_idc = MPEG2_PROFILE_MAIN;
                else
                    sps->i_profile_idc = MPEG2_PROFILE_SIMPLE;
            }*/

            string profile = "Simple";

            if (m.x262options.colorspace == "I422")
                profile = "4:2:2";
            else if (m.x262options.custommatrix != null) //Что тут должно быть?!
                profile = "High";
            else if (m.x262options.bframes > 0 || m.x262options.fake_int)
                profile = "Main";

            combo_mpg_profile.SelectedIndex = (int)m.x262options.profile;

            ((ComboBoxItem)combo_mpg_profile.Items.GetItemAt(0)).Content = "Auto (" + profile + ")";
        }

        private void SetToolTips()
        {
            image_warning.ToolTip = Languages.Translate("This encoder is highly experimental!") + "\r\n" + Languages.Translate("Use it at your own risk!");

            //Определяем дефолты (с учетом --preset и --tune)
            //x262_arguments def = new x262_arguments(m.x262options.preset, m.x262options.tune, m.x262options.profile);

            //Определяем дефолты без учета --preset и --tune, т.к. это именно дефолты самого энкодера
            x262_arguments def = new x262_arguments(Presets.Medium, Tunes.None, m.x262options.profile);

            CultureInfo cult_info = new CultureInfo("en-US");

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.ToolTip = "Set bitrate (Default: Auto)";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
                num_bitrate.ToolTip = "Set target quantizer (1 - 69). Lower - better quality but bigger filesize.\r\n(Default: 23)";
            else
                num_bitrate.ToolTip = "Set target quality (0 - 51). Lower - better quality but bigger filesize.\r\n(Default: 23)";

            combo_mode.ToolTip = "Encoding mode";
            combo_mpg_profile.ToolTip = "Specify profile (--profile, default: " + def.profile.ToString() + ")";
            combo_level.ToolTip = "Specify level (--level, default: Unrestricted)";
            combo_tune.ToolTip = "Tune the settings for a particular type of source or situation (--tune, default: " + def.tune.ToString() + ")";
            combo_subme.ToolTip = "Subpixel motion estimation and mode decision (--subme, default: " + def.subme + ")\r\n" +
                "1 - fast\r\n7 - medium\r\n9 - slow";
            combo_me.ToolTip = "Integer pixel motion estimation method (--me, default: --me " + def.me + ")\r\n" +
                "Diamond Search, fast (--me dia)\r\nHexagonal Search (--me hex)\r\nUneven Multi-Hexagon Search (--me umh)\r\n" +
                "Exhaustive Search (--me esa)\r\nSATD Exhaustive Search, slow (--me tesa)";
            combo_merange.ToolTip = "Maximum motion vector search range (--merange, default: " + def.merange + ")";
            check_chroma.ToolTip = "Ignore chroma in motion estimation (--no-chroma-me, default: unchecked)";
            combo_bframes.ToolTip = "Number of B-frames between I and P (--bframes, default: " + def.bframes + ")";
            combo_dc_precision.ToolTip = "Specify intra DC precision to use (--dc, default: " + def.dc + ")";
            check_altscan.ToolTip = "Use alternate MPEG-2 VLC scan order, not zigzag (--altscan, default: unchecked)";
            check_fast_pskip.ToolTip = "Disables early SKIP detection on P-frames (--no-fast-pskip, default: unchecked)";
            check_linear_q.ToolTip = "Use MPEG-2 linear quantization table (--linear-quant, default: unchecked)";
            num_min_quant.ToolTip = "Set min QP (--qpmin, default: " + def.min_quant + ")";
            num_max_quant.ToolTip = "Set max QP (--qpmax, default: " + def.max_quant + ")";
            num_step_quant.ToolTip = "Set max QP step (--qpstep, default: " + def.step_quant + ")";
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                  slider_preset.ToolTip = "Set encoding preset (--preset, default: Medium):" + Environment.NewLine +
                    "Ultrafast - fastest encoding, but biggest output file size" + Environment.NewLine +
                    "Medium - default, good speed and medium file size" + Environment.NewLine +
                    "Veryslow - high quality encoding, small file size" + Environment.NewLine +
                    "Placebo - super high quality encoding, smallest file size";
            }
            else
            {
                slider_preset.ToolTip = "Set encoding preset (--preset, default: Medium):" + Environment.NewLine +
                    "Ultrafast - fastest encoding, bad quality" + Environment.NewLine +
                    "Medium - default, optimal speed-quality solution" + Environment.NewLine +
                    "Veryslow - high quality encoding" + Environment.NewLine +
                    "Placebo - super high quality encoding";
            }
            combo_badapt_mode.ToolTip = "Adaptive B-frame decision method (--b-adapt, default: " + (def.b_adapt == 0 ? "Disabled)" : def.b_adapt == 1 ? "Fast)" : "Optimal");
            combo_adapt_quant_mode.ToolTip = "AQ mode (--aq-mode, default: " + (def.aqmode == 0 ? "None" : def.aqmode == 1 ? "VAQ" : "A-VAQ") + ")\r\n" +
                        "None - disabled, 0\r\nVAQ - variance AQ (complexity mask), 1\r\nA-VAQ - auto-variance AQ (experimental), 2";
            combo_adapt_quant.ToolTip = "AQ Strength (--ag-strength, default: " + def.aqstrength + ")\r\n" +
                        "Reduces blocking and blurring in flat and textured areas: 0.5 - weak AQ, 1.5 - strong AQ";
            num_psyrdo.ToolTip = "Strength of psychovisual RD optimization, requires Subpixel ME >= 6 (--psy-rd, default: " + def.psyrdo.ToString(cult_info) + ")";
            num_vbv_max.ToolTip = "Max local bitrate, kbit/s (--vbv-maxrate, default: " + def.vbv_maxrate + ")";
            num_vbv_buf.ToolTip = "Set size of the VBV buffer, kbit (--vbv-bufsize, default: " + def.vbv_bufsize + ")";
            num_vbv_init.ToolTip = "Initial VBV buffer occupancy (--vbv-init, default: " + def.vbv_init.ToString(cult_info) + ")";
            num_qcomp.ToolTip = "QP curve compression (--qcomp, default: " + def.qcomp.ToString(cult_info) + ")\r\n0.00 => CBR, 1.00 => CQP";
            num_chroma_qp.ToolTip = "QP difference between chroma and luma (--chroma-qp-offset, default: " + def.qp_offset + ")";
            combo_threads_count.ToolTip = "Set number of threads for encoding (--threads, default: Auto)\r\n" +
                "Auto = 1.5 * logical_processors\r\n1+1 = --threads 1 --thread-input";
            check_slow_first.ToolTip = "Enable slow 1-st pass for multipassing encoding (off by default)" + Environment.NewLine + "(--slow-firstpass if checked)";
            check_nombtree.ToolTip = "Disable mb-tree ratecontrol (off by default, --no-mbtree if checked)";
            num_lookahead.ToolTip = "Number of frames for mb-tree ratecontrol and VBV-lookahead (--rc-lookahead, default: " + def.lookahead + ")";
            combo_lookahead_threads.ToolTip = "Force a specific number of lookahead threads (--lookahead-threads, default: Auto)\r\nAuto = 1/6 of regular threads.";
            check_enable_psy.ToolTip = "If unchecked disable all visual optimizations that worsen both PSNR and SSIM" + Environment.NewLine + "(--no-psy if not checked)";
            num_min_gop.ToolTip = "Minimum GOP size (--min-keyint, default: " + def.gop_min + ")\r\n0 - Auto";
            num_max_gop.ToolTip = "Maximum GOP size (--keyint, default: " + def.gop_max + ")\r\n0 - \"infinite\"";
            num_ratio_ip.ToolTip = "QP factor between I and P (--ipratio, default: " + def.ratio_ip.ToString(cult_info) + ")";
            num_ratio_pb.ToolTip = "QP factor between P and B (--pbratio, default: " + def.ratio_pb.ToString(cult_info) + ")";
            combo_open_gop.ToolTip = "Use recovery points to close GOPs, requires B-frames (--open-gop, default: " + (!def.open_gop ? "No" : "Yes") + ")";
            num_slices.ToolTip = "Number of slices per frame (--slices, default: " + def.slices + ")";
            check_fake_int.ToolTip = "Flag stream as interlaced but encode progressive (--fake-interlaced, default: unchecked)";
            combo_range_in.ToolTip = "Forces the range of the input (--input-range, default: Auto)\r\nIf input and output ranges aren't the same, x262 will perform range conversion!";
            combo_range_out.ToolTip = "Sets the range of the output (--range, default: Auto)\r\nIf input and output ranges aren't the same, x262 will perform range conversion!";
            combo_colorprim.ToolTip = "Specify color primaries (--colorprim, default: " + def.colorprim + ")";
            combo_transfer.ToolTip = "Specify transfer characteristics (--transfer, default: " + def.transfer + ")";
            combo_colormatrix.ToolTip = "Specify color matrix setting (--colormatrix, default: " + def.colormatrix + ")";
            combo_colorspace.ToolTip = "Specify output colorspace (--output-csp, default: " + def.colorspace + ")\r\nDo not change it if you don't know what you`re doing!";
            check_non_deterministic.ToolTip = "Slightly improve quality when encoding with --threads > 1, at the cost of non-deterministic output encodes\r\n(--non-deterministic, default: unchecked)";
            check_bluray.ToolTip = "Enable compatibility hacks for Blu-ray support (--bluray-compat, default: unchecked)";
        }

        public static Massive DecodeLine(Massive m)
        {
            //x264_param_parse
            //https://github.com/kierank/x262/blob/master/common/common.c

            //Дефолты
            Presets preset = Presets.Medium;
            Tunes tune = Tunes.None;
            Profiles profile = Profiles.Auto;
            Settings.EncodingModes mode = new Settings.EncodingModes();

            //берём за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            //Определяем пресет, для этого ищем ключ --preset
            string preset_name = Calculate.GetRegexValue(@"\-\-preset\s+(\w+)", line);
            if (!string.IsNullOrEmpty(preset_name))
            {
                try
                {
                    //Определяем preset по его названию или номеру
                    preset = (Presets)Enum.Parse(typeof(Presets), preset_name, true);
                }
                catch { }
            }

            //Ищем --tune
            string tune_name = Calculate.GetRegexValue(@"\-\-tune\s+(\w+)", line);
            if (!string.IsNullOrEmpty(tune_name))
            {
                try
                {
                    //Определяем tune по его названию
                    tune = (Tunes)Enum.Parse(typeof(Tunes), tune_name, true);
                }
                catch { }
            }

            //Ищем --profile (определяем 10-битность)
            string profile_name = Calculate.GetRegexValue(@"\-\-profile\s+(\w+)", line);
            if (!string.IsNullOrEmpty(profile_name))
            {
                try
                {
                    //Определяем profile по его названию
                    profile = (Profiles)Enum.Parse(typeof(Profiles), profile_name, true);
                }
                catch { }
            }

            //Создаём свежий массив параметров x262 (изменяя дефолты с учетом --preset и --tune)
            m.x262options = new x262_arguments(preset, tune, profile);
            m.x262options.profile = profile;
            m.x262options.preset = preset;
            m.x262options.tune = tune;

            string value = "";
            string[] cli = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (int n = 0; n < cli.Length; n++)
            {
                value = cli[n];

                if (m.vpasses.Count == 1)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.Quality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                    else if (value == "--bitrate" || value == "-B")
                    {
                        mode = Settings.EncodingModes.OnePass;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                    else if (value == "--qp" || value == "-q")
                    {
                        mode = Settings.EncodingModes.Quantizer;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                }
                else if (m.vpasses.Count == 2)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.TwoPassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                    else if (value == "--bitrate" || value == "-B")
                    {
                        mode = Settings.EncodingModes.TwoPass;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                    else if (value == "--size")
                    {
                        mode = Settings.EncodingModes.TwoPassSize;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                }
                else if (m.vpasses.Count == 3)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.ThreePassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                    else if (value == "--bitrate" || value == "-B")
                    {
                        mode = Settings.EncodingModes.ThreePass;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                    else if (value == "--size")
                    {
                        mode = Settings.EncodingModes.ThreePassSize;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[++n]);
                    }
                }

                if (value == "--level")
                    m.x262options.level = cli[++n];

                else if (value == "--aq-strength")
                    m.x262options.aqstrength = cli[++n];

                else if (value == "--aq-mode")
                    m.x262options.aqmode = Convert.ToInt32(cli[++n]);

                else if (value == "--no-psy")
                    m.x262options.no_psy = true;

                else if (value == "--psy-rd")
                {
                    string[] psyvalues = cli[++n].Split(new string[] { ":" }, StringSplitOptions.None);
                    m.x262options.psyrdo = (decimal)Calculate.ConvertStringToDouble(psyvalues[0]);
                }

                else if (value == "--subme" || value == "-m")
                    m.x262options.subme = Convert.ToInt32(cli[++n]);

                else if (value == "--me")
                    m.x262options.me = cli[++n];

                else if (value == "--merange")
                    m.x262options.merange = Convert.ToInt32(cli[++n]);

                else if (value == "--no-chroma-me")
                    m.x262options.no_chroma = true;

                else if (value == "--bframes" || value == "-b")
                    m.x262options.bframes = Convert.ToInt32(cli[++n]);

                else if (value == "--dc")
                    m.x262options.dc = Convert.ToInt32(cli[++n]);

                else if (value == "--b-adapt")
                    m.x262options.b_adapt = Convert.ToInt32(cli[++n]);

                else if (value == "--altscan")
                    m.x262options.altscan = true;

                else if (value == "--no-fast-pskip")
                    m.x262options.no_fastpskip = true;

                else if (value == "--linear-quant")
                    m.x262options.linear_q = true;

                else if (value == "--cqm")
                    m.x262options.custommatrix = cli[++n];

                else if (value == "--qpmin")
                    m.x262options.min_quant = Convert.ToInt32(cli[++n]);

                else if (value == "--qpmax")
                    m.x262options.max_quant = Convert.ToInt32(cli[++n]);

                else if (value == "--qpstep")
                    m.x262options.step_quant = Convert.ToInt32(cli[++n]);

                else if (value == "--threads")
                    m.x262options.threads = cli[++n];

                else if (value == "--thread-input")
                    m.x262options.thread_input = true;

                else if (value == "--qcomp")
                    m.x262options.qcomp = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--vbv-maxrate")
                    m.x262options.vbv_maxrate = Convert.ToInt32(cli[++n]);

                else if (value == "--vbv-bufsize")
                    m.x262options.vbv_bufsize = Convert.ToInt32(cli[++n]);

                else if (value == "--vbv-init")
                    m.x262options.vbv_init = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--chroma-qp-offset")
                    m.x262options.qp_offset = Convert.ToInt32(cli[++n]);

                else if (value == "--slow-firstpass")
                    m.x262options.slow_frstpass = true;

                else if (value == "--no-mbtree")
                    m.x262options.no_mbtree = true;

                else if (value == "--rc-lookahead")
                    m.x262options.lookahead = Convert.ToInt32(cli[++n]);

                else if (value == "--lookahead-threads")
                    m.x262options.lookahead_threads = cli[++n];

                else if (value == "--min-keyint")
                    m.x262options.gop_min = Convert.ToInt32(cli[++n]);

                else if (value == "--keyint")
                {
                    int _value = 0;
                    Int32.TryParse(cli[++n], out _value);
                    m.x262options.gop_max = _value;
                }

                else if (value == "--ipratio")
                    m.x262options.ratio_ip = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--pbratio")
                    m.x262options.ratio_pb = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--open-gop")
                    m.x262options.open_gop = true;

                else if (value == "--slices")
                    m.x262options.slices = Convert.ToInt32(cli[++n]);

                else if (value == "--fake-interlaced")
                    m.x262options.fake_int = true;

                else if (value == "--input-range")
                    m.x262options.range_in = cli[++n];

                else if (value == "--range")
                    m.x262options.range_out = cli[++n];

                else if (value == "--colorprim")
                {
                    string _value = cli[++n].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x262options.colorprim = "Undefined";
                    else
                        m.x262options.colorprim = _value;
                }

                else if (value == "--transfer")
                {
                    string _value = cli[++n].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x262options.transfer = "Undefined";
                    else
                        m.x262options.transfer = _value;
                }

                else if (value == "--colormatrix")
                {
                    string _value = cli[++n].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x262options.colormatrix = "Undefined";
                    else
                        m.x262options.colormatrix = _value;
                }

                else if (value == "--output-csp")
                    m.x262options.colorspace = cli[++n].ToUpper();

                else if (value == "--non-deterministic")
                    m.x262options.non_deterministic = true;

                else if (value == "--bluray-compat")
                    m.x262options.bluray = true;

                else if (value == "--extra:")
                {
                    for (int i = n + 1; i < cli.Length; i++)
                        m.x262options.extra_cli += cli[i] + " ";

                    m.x262options.extra_cli = m.x262options.extra_cli.Trim();
                }
            }

            //Сброс на дефолт, если в CLI нет параметров кодирования
            if (mode == 0)
            {
                m.encodingmode = Settings.EncodingModes.Quality;
                m.outvbitrate = 23;
            }
            else
                m.encodingmode = mode;

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //Определяем дефолты (с учетом --preset и --tune)
            x262_arguments defaults = new x262_arguments(m.x262options.preset, m.x262options.tune, m.x262options.profile);

            //обнуляем старые строки
            m.vpasses.Clear();

            string line = "--mpeg2 ";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line += "--bitrate " + m.outvbitrate;
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                line += "--size " + m.outvbitrate;
            else if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                line += "--crf " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1);
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
                line += "--qp " + m.outvbitrate;

            //Пресет
            line += " --preset " + m.x262options.preset.ToString().ToLower();

            if (m.x262options.tune != defaults.tune && !m.x262options.extra_cli.Contains("--tune "))
                line += " --tune " + m.x262options.tune.ToString().ToLower();

            if (m.x262options.profile != defaults.profile && !m.x262options.extra_cli.Contains("--profile "))
                line += " --profile " + m.x262options.profile.ToString().ToLower();

            if (m.x262options.level != defaults.level && !m.x262options.extra_cli.Contains("--level "))
                line += " --level " + m.x262options.level;

            if (m.x262options.aqmode != defaults.aqmode && !m.x262options.extra_cli.Contains("--aq-mode "))
                line += " --aq-mode " + m.x262options.aqmode;

            if (m.x262options.aqstrength != defaults.aqstrength && m.x262options.aqmode != 0 && !m.x262options.extra_cli.Contains("--aq-strength "))
                line += " --aq-strength " + m.x262options.aqstrength;

            if (m.x262options.merange != defaults.merange && !m.x262options.extra_cli.Contains("--merange "))
                line += " --merange " + m.x262options.merange;

            if (m.x262options.no_chroma && !defaults.no_chroma && !m.x262options.extra_cli.Contains("--no-chroma-me"))
                line += " --no-chroma-me";

            if (m.x262options.bframes != defaults.bframes && !m.x262options.extra_cli.Contains("--bframes "))
                line += " --bframes " + m.x262options.bframes;

            if (m.x262options.dc != defaults.dc && !m.x262options.extra_cli.Contains("--dc "))
                line += " --dc " + m.x262options.dc;

            if (m.x262options.b_adapt != defaults.b_adapt && !m.x262options.extra_cli.Contains("--b-adapt "))
                line += " --b-adapt " + m.x262options.b_adapt;

            if (m.x262options.altscan && !defaults.altscan && !m.x262options.extra_cli.Contains("--altscan"))
                line += " --altscan";

            if (m.x262options.no_fastpskip && !defaults.no_fastpskip && !m.x262options.extra_cli.Contains("--no-fast-pskip"))
                line += " --no-fast-pskip";

            if (m.x262options.linear_q && !defaults.linear_q && !m.x262options.extra_cli.Contains("--linear-quant"))
                line += " --linear-quant";

            if (m.x262options.custommatrix != defaults.custommatrix && !m.x262options.extra_cli.Contains("--cqm "))
                line += " --cqm " + m.x262options.custommatrix;

            if (m.x262options.min_quant != defaults.min_quant && !m.x262options.extra_cli.Contains("--qpmin "))
                line += " --qpmin " + m.x262options.min_quant;

            if (m.x262options.max_quant != defaults.max_quant && !m.x262options.extra_cli.Contains("--qpmax "))
                line += " --qpmax " + m.x262options.max_quant;

            if (m.x262options.step_quant != defaults.step_quant && !m.x262options.extra_cli.Contains("--qpstep "))
                line += " --qpstep " + m.x262options.step_quant;

            if (m.x262options.no_psy && !defaults.no_psy && !m.x262options.extra_cli.Contains("--no-psy"))
                line += " --no-psy";

            if (!m.x262options.no_psy && m.x262options.psyrdo != defaults.psyrdo && !m.x262options.extra_cli.Contains("--psy-rd "))
                line += " --psy-rd " + Calculate.ConvertDoubleToPointString((double)m.x262options.psyrdo, 2) + ":0.00";

            if (m.x262options.threads != defaults.threads && !m.x262options.extra_cli.Contains("--threads "))
            {
                if (m.x262options.threads == "1" && m.x262options.thread_input)
                    line += " --threads 1 --thread-input";
                else
                    line += " --threads " + m.x262options.threads;
            }

            if (m.x262options.qcomp != defaults.qcomp && !m.x262options.extra_cli.Contains("--qcomp "))
                line += " --qcomp " + Calculate.ConvertDoubleToPointString((double)m.x262options.qcomp, 2);

            if (m.x262options.vbv_maxrate != defaults.vbv_maxrate && !m.x262options.extra_cli.Contains("--vbv-maxrate "))
                line += " --vbv-maxrate " + m.x262options.vbv_maxrate;

            if (m.x262options.vbv_bufsize != defaults.vbv_bufsize && !m.x262options.extra_cli.Contains("--vbv-bufsize "))
                line += " --vbv-bufsize " + m.x262options.vbv_bufsize;

            if (m.x262options.vbv_init != defaults.vbv_init && !m.x262options.extra_cli.Contains("--vbv-init "))
                line += " --vbv-init " + Calculate.ConvertDoubleToPointString((double)m.x262options.vbv_init, 2);

            if (m.x262options.qp_offset != defaults.qp_offset && !m.x262options.extra_cli.Contains("--chroma-qp-offset "))
                line += " --chroma-qp-offset " + m.x262options.qp_offset;

            if (m.x262options.subme != defaults.subme && !m.x262options.extra_cli.Contains("--subme "))
                line += " --subme " + m.x262options.subme;

            if (m.x262options.me != defaults.me && !m.x262options.extra_cli.Contains("--me "))
                line += " --me " + m.x262options.me;

            if (m.x262options.slow_frstpass && !defaults.slow_frstpass && !m.x262options.extra_cli.Contains("--slow-firstpass"))
                line += " --slow-firstpass";

            if (m.x262options.no_mbtree && !defaults.no_mbtree && !m.x262options.extra_cli.Contains("--no-mbtree"))
                line += " --no-mbtree";

            if (!m.x262options.no_mbtree && m.x262options.lookahead != defaults.lookahead && !m.x262options.extra_cli.Contains("--rc-lookahead "))
                line += " --rc-lookahead " + m.x262options.lookahead;

            if (m.x262options.lookahead_threads != defaults.lookahead_threads && !m.x262options.extra_cli.Contains("--lookahead-threads "))
                line += " --lookahead-threads " + m.x262options.lookahead_threads;

            if (m.x262options.gop_min > 0 && m.x262options.gop_min != defaults.gop_min && !m.x262options.extra_cli.Contains("--min-keyint "))
                line += " --min-keyint " + m.x262options.gop_min;

            if (m.x262options.gop_max != defaults.gop_max && !m.x262options.extra_cli.Contains("--keyint "))
                line += " --keyint " + (m.x262options.gop_max > 0 ? m.x262options.gop_max.ToString() : "infinite");

            if (m.x262options.ratio_ip != defaults.ratio_ip && !m.x262options.extra_cli.Contains("--ipratio "))
                line += " --ipratio " + Calculate.ConvertDoubleToPointString((double)m.x262options.ratio_ip, 2);

            if (m.x262options.ratio_pb != defaults.ratio_pb && !m.x262options.extra_cli.Contains("--pbratio "))
                line += " --pbratio " + Calculate.ConvertDoubleToPointString((double)m.x262options.ratio_pb, 2);

            if (m.x262options.open_gop && !defaults.open_gop && !m.x262options.extra_cli.Contains("--open-gop"))
                line += " --open-gop";

            if (m.x262options.slices != defaults.slices && !m.x262options.extra_cli.Contains("--slices "))
                line += " --slices " + m.x262options.slices;

            if (m.x262options.fake_int && !defaults.fake_int && !m.x262options.extra_cli.Contains("--fake-interlaced"))
                line += " --fake-interlaced";

            if (m.x262options.range_in != defaults.range_in && !m.x262options.extra_cli.Contains("--input-range "))
                line += " --input-range " + m.x262options.range_in;

            if (m.x262options.range_out != defaults.range_out && !m.x262options.extra_cli.Contains("--range "))
                line += " --range " + m.x262options.range_out;

            if (m.x262options.colorprim != defaults.colorprim && !m.x262options.extra_cli.Contains("--colorprim "))
                line += " --colorprim " + ((m.x262options.colorprim == "Undefined") ? "undef" : m.x262options.colorprim);

            if (m.x262options.transfer != defaults.transfer && !m.x262options.extra_cli.Contains("--transfer "))
                line += " --transfer " + ((m.x262options.transfer == "Undefined") ? "undef" : m.x262options.transfer);

            if (m.x262options.colormatrix != defaults.colormatrix && !m.x262options.extra_cli.Contains("--colormatrix "))
                line += " --colormatrix " + ((m.x262options.colormatrix == "Undefined") ? "undef" : m.x262options.colormatrix);

            if (m.x262options.colorspace != defaults.colorspace && !m.x262options.extra_cli.Contains("--output-csp "))
                line += " --output-csp " + m.x262options.colorspace.ToLower();

            if (m.x262options.non_deterministic && !defaults.non_deterministic && !m.x262options.extra_cli.Contains("--non-deterministic"))
                line += " --non-deterministic";

            if (m.x262options.bluray && !defaults.bluray && !m.x262options.extra_cli.Contains("--bluray-compat"))
                line += " --bluray-compat";

            line += " --extra:";
            if (m.x262options.extra_cli != defaults.extra_cli)
                line += " " + m.x262options.extra_cli;

            //удаляем пустоту в начале
            line = line.TrimStart(new char[] { ' ' });

            //забиваем данные в массив
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer)
                m.vpasses.Add(line);
            else if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
            {
                m.vpasses.Add("--pass 1 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }
            else if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.vpasses.Add("--pass 1 " + line);
                m.vpasses.Add("--pass 3 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.TwoPassSize)
            {
                m.vpasses.Add("--pass 1 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }
            else if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.vpasses.Add("--pass 1 " + line);
                m.vpasses.Add("--pass 3 " + line);
                m.vpasses.Add("--pass 2 " + line);
            }

            return m;
        }

        private void combo_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted) && combo_mode.SelectedItem != null)
            {
                try
                {
                    //запоминаем старый режим
                    oldmode = m.encodingmode;

                    string x262mode = combo_mode.SelectedItem.ToString();
                    if (x262mode == "1-Pass Bitrate") m.encodingmode = Settings.EncodingModes.OnePass;
                    else if (x262mode == "2-Pass Bitrate") m.encodingmode = Settings.EncodingModes.TwoPass;
                    else if (x262mode == "2-Pass Size") m.encodingmode = Settings.EncodingModes.TwoPassSize;
                    else if (x262mode == "3-Pass Bitrate") m.encodingmode = Settings.EncodingModes.ThreePass;
                    else if (x262mode == "3-Pass Size") m.encodingmode = Settings.EncodingModes.ThreePassSize;
                    else if (x262mode == "Constant Quality") m.encodingmode = Settings.EncodingModes.Quality;
                    else if (x262mode == "Constant Quantizer") m.encodingmode = Settings.EncodingModes.Quantizer;
                    else if (x262mode == "2-Pass Quality") m.encodingmode = Settings.EncodingModes.TwoPassQuality;
                    else if (x262mode == "3-Pass Quality") m.encodingmode = Settings.EncodingModes.ThreePassQuality;

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

                    SetMPEG2Profile();
                    root_window.UpdateOutSize();
                    root_window.UpdateManualProfile();
                    UpdateCLI();
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
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.outvbitrate = 23;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Quality") + ": (CRF)";
            }
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
            {
                m.outvbitrate = 23;
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
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
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
            if ((combo_level.IsDropDownOpen || combo_level.IsSelectionBoxHighlighted) && combo_level.SelectedItem != null)
            {
                m.x262options.level = combo_level.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_subme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_subme.IsDropDownOpen || combo_subme.IsSelectionBoxHighlighted) && combo_subme.SelectedIndex != -1)
            {
                m.x262options.subme = combo_subme.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }

            if (m.x262options.subme < 6)
                num_psyrdo.Tag = "Inactive";
            else
                num_psyrdo.Tag = null;
        }

        private void combo_me_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_me.IsDropDownOpen || combo_me.IsSelectionBoxHighlighted) && combo_me.SelectedItem != null)
            {
                string me = combo_me.SelectedItem.ToString();
                if (me == "Diamond") m.x262options.me = "dia";
                else if (me == "Hexagon") m.x262options.me = "hex";
                else if (me == "Multi Hexagon") m.x262options.me = "umh";
                else if (me == "Exhaustive") m.x262options.me = "esa";
                else if (me == "SATD Exhaustive") m.x262options.me = "tesa";

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_merange_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_merange.IsDropDownOpen || combo_merange.IsSelectionBoxHighlighted) && combo_merange.SelectedItem != null)
            {
                m.x262options.merange = Convert.ToInt32(combo_merange.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_chroma_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.no_chroma = check_chroma.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted) && combo_bframes.SelectedItem != null)
            {
                m.x262options.bframes = Convert.ToInt32(combo_bframes.SelectedItem);
                SetMPEG2Profile();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }

            if (m.x262options.bframes > 0)
            {
                combo_badapt_mode.Tag = null;
                combo_open_gop.Tag = null;
            }
            else
            {
                combo_badapt_mode.Tag = "Inactive";
                combo_open_gop.Tag = "Inactive";
            }
        }

        private void combo_dc_precision_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_dc_precision.IsDropDownOpen || combo_dc_precision.IsSelectionBoxHighlighted) && combo_dc_precision.SelectedItem != null)
            {
                m.x262options.dc = Convert.ToInt32(combo_dc_precision.SelectedItem);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_altscan_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.altscan = check_altscan.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void num_min_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_quant.IsAction)
            {
                m.x262options.min_quant = Convert.ToInt32(num_min_quant.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_max_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_quant.IsAction)
            {
                m.x262options.max_quant = Convert.ToInt32(num_max_quant.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_step_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_step_quant.IsAction)
            {
                m.x262options.step_quant = Convert.ToInt32(num_step_quant.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_fast_pskip_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.no_fastpskip = check_fast_pskip.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_linear_q_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.linear_q = check_linear_q.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_mpg_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_mpg_profile.IsDropDownOpen || combo_mpg_profile.IsSelectionBoxHighlighted) && combo_mpg_profile.SelectedIndex != -1)
            {
                m.x262options.profile = (Profiles)Enum.ToObject(typeof(Profiles), combo_mpg_profile.SelectedIndex);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        public void UpdateCLI()
        {
            textbox_cli.Clear();
            foreach (string line in m.vpasses)
                textbox_cli.Text += line + "\r\n\r\n";
            good_cli = (ArrayList)m.vpasses.Clone(); //Клонируем CLI, не вызывающую ошибок
        }

        private void slider_preset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_preset.IsFocused || slider_preset.IsMouseOver)
            {
                //Создаем новые параметры с учетом --preset, и берем от них только те, от которых зависит пресет
                m.x262options.preset = (Presets)Enum.ToObject(typeof(Presets), (int)slider_preset.Value);
                x262_arguments defaults = new x262_arguments(m.x262options.preset, m.x262options.tune, m.x262options.profile);
                m.x262options.aqmode = defaults.aqmode;
                m.x262options.b_adapt = defaults.b_adapt;
                m.x262options.bframes = defaults.bframes;
                m.x262options.lookahead = defaults.lookahead;
                m.x262options.me = defaults.me;
                m.x262options.merange = defaults.merange;
                m.x262options.no_fastpskip = defaults.no_fastpskip;
                m.x262options.no_mbtree = defaults.no_mbtree;
                m.x262options.psyrdo = defaults.psyrdo;//
                m.x262options.slow_frstpass = defaults.slow_frstpass;
                m.x262options.subme = defaults.subme;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.x262options (это нужно для "--extra:")
                LoadFromProfile(); //Выставляем значения из m.x262options в элементы управления
            }
        }

        private void combo_tune_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_tune.IsDropDownOpen || combo_tune.IsSelectionBoxHighlighted) && combo_tune.SelectedIndex != -1)
            {
                //Создаем новые параметры с учетом --tune, и берем от них только те, от которых зависит tune
                m.x262options.tune = (Tunes)Enum.ToObject(typeof(Tunes), combo_tune.SelectedIndex);
                x262_arguments defaults = new x262_arguments(m.x262options.preset, m.x262options.tune, m.x262options.profile);
                m.x262options.aqmode = defaults.aqmode;
                m.x262options.aqstrength = defaults.aqstrength;
                m.x262options.bframes = defaults.bframes;
                m.x262options.no_psy = defaults.no_psy;
                m.x262options.psyrdo = defaults.psyrdo;
                m.x262options.qcomp = defaults.qcomp;
                m.x262options.ratio_ip = defaults.ratio_ip;
                m.x262options.ratio_pb = defaults.ratio_pb;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.x262options (это нужно для "--extra:")
                LoadFromProfile(); //Выставляем значения из m.x262options в элементы управления
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
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = 50000;
            }
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
            {
                num_bitrate.Minimum = 1;
                num_bitrate.Maximum = 69;
            }
            else
            {
                num_bitrate.Minimum = 0;
                num_bitrate.Maximum = 51;
            }
        }

        private void num_bitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bitrate.IsAction)
            {
                m.outvbitrate = num_bitrate.Value;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_psyrdo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_psyrdo.IsAction)
            {
                m.x262options.psyrdo = num_psyrdo.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_adapt_quant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_adapt_quant.IsDropDownOpen || combo_adapt_quant.IsSelectionBoxHighlighted) && combo_adapt_quant.SelectedItem != null)
            {
                m.x262options.aqstrength = combo_adapt_quant.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_adapt_quant_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_adapt_quant_mode.IsDropDownOpen || combo_adapt_quant_mode.IsSelectionBoxHighlighted) && combo_adapt_quant_mode.SelectedIndex != -1)
            {
                m.x262options.aqmode = combo_adapt_quant_mode.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_threads_count_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_threads_count.IsDropDownOpen || combo_threads_count.IsSelectionBoxHighlighted) && combo_threads_count.SelectedIndex != -1)
            {
                if (combo_threads_count.SelectedIndex == 2)
                {
                    m.x262options.threads = "1";
                    m.x262options.thread_input = true;
                }
                else
                {
                    m.x262options.threads = combo_threads_count.SelectedItem.ToString().ToLower();
                    m.x262options.thread_input = false;
                }
                
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_badapt_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_badapt_mode.IsDropDownOpen || combo_badapt_mode.IsSelectionBoxHighlighted) && combo_badapt_mode.SelectedIndex != -1)
            {
                m.x262options.b_adapt = combo_badapt_mode.SelectedIndex;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_qcomp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_qcomp.IsAction)
            {
                m.x262options.qcomp = num_qcomp.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_max.IsAction)
            {
                m.x262options.vbv_maxrate = Convert.ToInt32(num_vbv_max.Value);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_buf_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_buf.IsAction)
            {
                m.x262options.vbv_bufsize = Convert.ToInt32(num_vbv_buf.Value);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_init_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_init.IsAction)
            {
                m.x262options.vbv_init = num_vbv_init.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }
        
        private void num_chroma_qp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_chroma_qp.IsAction)
            {
                m.x262options.qp_offset = (int)num_chroma_qp.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_slow_first_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.slow_frstpass = check_slow_first.IsChecked.Value;

            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_nombtree_Clicked(object sender, RoutedEventArgs e)
        {
            m.x262options.no_mbtree = check_nombtree.IsChecked.Value;

            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void num_lookahead_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_lookahead.IsAction)
            {
                m.x262options.lookahead = Convert.ToInt32(num_lookahead.Value);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_lookahead_threads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_lookahead_threads.IsDropDownOpen || combo_lookahead_threads.IsSelectionBoxHighlighted) && combo_lookahead_threads.SelectedItem != null)
            {
                m.x262options.lookahead_threads = combo_lookahead_threads.SelectedItem.ToString().ToLower();

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_enable_psy_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.no_psy = !check_enable_psy.IsChecked.Value;
            num_psyrdo.IsEnabled = (!m.x262options.no_psy && !m.x262options.extra_cli.Contains("--psy-rd "));

            root_window.UpdateManualProfile();
            UpdateCLI();
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
                if (m.vpasses.Count == 0) m.vpasses.Add(" ");

                DecodeLine(m);                       //- Загружаем в массив m.x262 значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                   //- Загружаем в форму значения, на основе значений массива m.x262
                m.vencoding = "Custom x262 CLI";     //- Изменяем название пресета
                PresetLoader.CreateVProfile(m);      //- Перезаписываем файл пресета (m.vpasses[x])
                root_window.m = this.m.Clone();      //- Передаем массив в основное окно
                root_window.LoadProfiles();          //- Обновляем название выбранного пресета в основном окне (Custom x262 CLI)
            }
            catch (Exception)
            {
                Message mm = new Message(root_window);
                mm.ShowMessage(Languages.Translate("Attention! Seems like CLI line contains errors!") + "\r\n" + Languages.Translate("Check all keys and theirs values and try again!") + "\r\n\r\n" + 
                    Languages.Translate("OK - restore line (recommended)") + "\r\n" + Languages.Translate("Cancel - ignore (not recommended)"),Languages.Translate("Error"),Message.MessageStyle.OkCancel);
                if (mm.result == Message.Result.Ok)
                    button_Reset_CLI_Click(null, null);
            }
        }

        private void button_Reset_CLI_Click(object sender, RoutedEventArgs e)
        {
            if (good_cli != null)
            {
                m.vpasses = (ArrayList)good_cli.Clone(); //- Восстанавливаем CLI до версии, не вызывавшей ошибок
                DecodeLine(m);                           //- Загружаем в массив m.x262 значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                       //- Загружаем в форму значения, на основе значений массива m.x262
                root_window.m = this.m.Clone();          //- Передаем массив в основное окно
            }
            else
            {
                new Message(root_window).ShowMessage("Can`t find good CLI...", Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void button_x262_help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                help.FileName = Calculate.StartupPath + "\\apps\\x262\\x262.exe";
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = " --fullhelp";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                string title = "x262 --fullhelp";
                new ShowWindow(root_window, title, p.StandardOutput.ReadToEnd().Replace("\n", "\r\n"), new FontFamily("Lucida Console"));
            }
            catch (Exception ex)
            {
                new Message(root_window).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void num_min_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_gop.IsAction)
            {
                m.x262options.gop_min = Convert.ToInt32(num_min_gop.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_max_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_gop.IsAction)
            {
                m.x262options.gop_max = Convert.ToInt32(num_max_gop.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_ratio_ip_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ratio_ip.IsAction)
            {
                m.x262options.ratio_ip = num_ratio_ip.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_ratio_pb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ratio_pb.IsAction)
            {
                m.x262options.ratio_pb = num_ratio_pb.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_open_gop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_open_gop.IsDropDownOpen || combo_open_gop.IsSelectionBoxHighlighted) && combo_open_gop.SelectedIndex != -1)
            {
                m.x262options.open_gop = (combo_open_gop.SelectedIndex == 1);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_slices_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_slices.IsAction)
            {
                m.x262options.slices = Convert.ToInt32(num_slices.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_fake_int_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.fake_int = check_fake_int.IsChecked.Value;
            SetMPEG2Profile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_range_in_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_range_in.IsDropDownOpen || combo_range_in.IsSelectionBoxHighlighted) && combo_range_in.SelectedItem != null)
            {
                m.x262options.range_in = combo_range_in.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_range_out_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_range_out.IsDropDownOpen || combo_range_out.IsSelectionBoxHighlighted) && combo_range_out.SelectedItem != null)
            {
                m.x262options.range_out = combo_range_out.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colorprim_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_colorprim.IsDropDownOpen || combo_colorprim.IsSelectionBoxHighlighted) && combo_colorprim.SelectedItem != null)
            {
                m.x262options.colorprim = combo_colorprim.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_transfer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_transfer.IsDropDownOpen || combo_transfer.IsSelectionBoxHighlighted) && combo_transfer.SelectedItem != null)
            {
                m.x262options.transfer = combo_transfer.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colormatrix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_colormatrix.IsDropDownOpen || combo_colormatrix.IsSelectionBoxHighlighted) && combo_colormatrix.SelectedItem != null)
            {
                m.x262options.colormatrix = combo_colormatrix.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colorspace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_colorspace.IsDropDownOpen || combo_colorspace.IsSelectionBoxHighlighted) && combo_colorspace.SelectedItem != null)
            {
                m.x262options.colorspace = combo_colorspace.SelectedItem.ToString();
                SetMPEG2Profile();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_non_deterministic_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.non_deterministic = check_non_deterministic.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_bluray_Click(object sender, RoutedEventArgs e)
        {
            m.x262options.bluray = check_bluray.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }
    }
}