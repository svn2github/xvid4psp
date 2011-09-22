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
        public enum Presets { Ultrafast = 0, Superfast, Veryfast, Faster, Fast, Medium, Slow, Slower, Veryslow, Placebo }
        public enum Tunes { None = 0, Film, Animation, Grain, StillImage, PSNR, SSIM, FastDecode }
        public enum Profiles { Auto = 0, Baseline, Main, High, High10 }
        private ArrayList good_cli = null;

        public x264(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
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

            //AVC profile
            combo_avc_profile.Items.Add(new ComboBoxItem { Content = "Auto" });
            combo_avc_profile.Items.Add("Baseline Profile");
            combo_avc_profile.Items.Add("Main Profile");
            combo_avc_profile.Items.Add("High Profile");
            combo_avc_profile.Items.Add("High 10 Profile");

            //AVC level
            combo_level.Items.Add("Unrestricted");
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

            //прогружаем деблокинг
            for (int n = -6; n <= 6; n++)
            {
                combo_dstrength.Items.Add(n);
                combo_dthreshold.Items.Add(n);
            }

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
            combo_subme.Items.Add("10 - QP-RD");
            combo_subme.Items.Add("11 - Full RD");

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

            combo_open_gop.Items.Add("No");
            combo_open_gop.Items.Add("Yes");

            //Кол-во потоков для x264-го
            combo_threads_count.Items.Add("Auto");
            combo_threads_count.Items.Add("1");
            combo_threads_count.Items.Add("1+1"); //+ --thread-input
            for (int n = 2; n <= 16; n++)
                combo_threads_count.Items.Add(Convert.ToString(n));

            //-b-adapt
            combo_badapt_mode.Items.Add("Disabled");
            combo_badapt_mode.Items.Add("Fast");
            combo_badapt_mode.Items.Add("Optimal");

            //--b-pyramid
            combo_bpyramid_mode.Items.Add("None");
            combo_bpyramid_mode.Items.Add("Strict");
            combo_bpyramid_mode.Items.Add("Normal");

            //--weightp
            combo_weightp_mode.Items.Add("Disabled");
            combo_weightp_mode.Items.Add("Blind offset");
            combo_weightp_mode.Items.Add("Smart analysis");

            combo_nal_hrd.Items.Add("None");
            combo_nal_hrd.Items.Add("VBR");
            combo_nal_hrd.Items.Add("CBR");

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
            combo_colorspace.Items.Add("I444");
            combo_colorspace.Items.Add("RGB");

            Apply_CLI.Content = Languages.Translate("Apply");
            Reset_CLI.Content = Languages.Translate("Reset");
            x264_help.Content = Languages.Translate("Help");
            Reset_CLI.ToolTip = "Reset to last good CLI";
            x264_help.ToolTip = "Show x264.exe --fullhelp screen";

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

            //lossless
            check_lossless.IsChecked = IsLossless(m);

            //Встроенный в x264 пресет
            text_preset_name.Content = m.x264options.preset.ToString();
            slider_preset.Value = (int)m.x264options.preset;

            //AVC level
            if (m.x264options.level == "unrestricted") combo_level.SelectedIndex = 0;
            else combo_level.SelectedItem = m.x264options.level;

            //Tune
            combo_tune.SelectedIndex = (int)m.x264options.tune;

            combo_adapt_quant_mode.SelectedIndex = m.x264options.aqmode;
            combo_adapt_quant.SelectedItem = m.x264options.aqstrength;
            
            num_psyrdo.Value = m.x264options.psyrdo;
            num_psytrellis.Value = m.x264options.psytrellis;
            num_qcomp.Value = m.x264options.qcomp;
            num_vbv_max.Value = m.x264options.vbv_maxrate;
            num_vbv_buf.Value = m.x264options.vbv_bufsize;
            num_vbv_init.Value = m.x264options.vbv_init;

            //прописываем макроблоки
            if (m.x264options.analyse == "none")
            {
                check_p8x8.IsChecked = false;
                check_i8x8.IsChecked = false;
                check_b8x8.IsChecked = false;
                check_i4x4.IsChecked = false;
                check_p4x4.IsChecked = false;
            }
            else if (m.x264options.analyse == "all")
            {
                check_p8x8.IsChecked = true;
                check_i8x8.IsChecked = true;
                check_b8x8.IsChecked = true;
                check_i4x4.IsChecked = true;
                check_p4x4.IsChecked = true;
            }
            else
            {
                check_p8x8.IsChecked = m.x264options.analyse.Contains("p8x8");
                check_i8x8.IsChecked = m.x264options.analyse.Contains("i8x8");
                check_b8x8.IsChecked = m.x264options.analyse.Contains("b8x8");
                check_i4x4.IsChecked = m.x264options.analyse.Contains("i4x4");
                check_p4x4.IsChecked = m.x264options.analyse.Contains("p4x4");
            }

            //adaptive dct
            check_8x8dct.IsChecked = m.x264options.adaptivedct;

            //прогружаем деблокинг
            combo_dstrength.SelectedItem = m.x264options.deblocks;
            combo_dthreshold.SelectedItem = m.x264options.deblockt;
            check_deblocking.IsChecked = m.x264options.deblocking;

            //Прописываем subme
            combo_subme.SelectedIndex = m.x264options.subme;

            //прописываем me алгоритм
            if (m.x264options.me == "dia") combo_me.SelectedIndex = 0;
            else if (m.x264options.me == "hex") combo_me.SelectedIndex = 1;
            else if (m.x264options.me == "umh") combo_me.SelectedIndex = 2;
            else if (m.x264options.me == "esa") combo_me.SelectedIndex = 3;
            else if (m.x264options.me == "tesa") combo_me.SelectedIndex = 4;

            //прописываем me range
            combo_merange.SelectedItem = m.x264options.merange;

            //прописываем chroma me
            check_chroma.IsChecked = m.x264options.no_chroma;

            //B фреймы
            combo_bframes.SelectedItem = m.x264options.bframes;

            //режим B фреймов
            if (m.x264options.direct == "none") combo_bframe_mode.SelectedIndex = 0;
            else if (m.x264options.direct == "spatial") combo_bframe_mode.SelectedIndex = 1;
            else if (m.x264options.direct == "temporal") combo_bframe_mode.SelectedIndex = 2;
            else if (m.x264options.direct == "auto") combo_bframe_mode.SelectedIndex = 3;

            if (!m.x264options.open_gop) combo_open_gop.SelectedIndex = 0;
            else combo_open_gop.SelectedIndex = 1;

            if (m.x264options.nal_hrd == "none") combo_nal_hrd.SelectedIndex = 0;
            else if (m.x264options.nal_hrd == "vbr") combo_nal_hrd.SelectedIndex = 1;
            else if (m.x264options.nal_hrd == "cbr") combo_nal_hrd.SelectedIndex = 2;

            //b-pyramid
            combo_bpyramid_mode.SelectedIndex = m.x264options.bpyramid;

            //weightb
            check_weightedb.IsChecked = m.x264options.weightb;

            //weightp
            combo_weightp_mode.SelectedIndex = m.x264options.weightp;

            //trellis
            combo_trellis.SelectedIndex = m.x264options.trellis;

            //refernce frames
            combo_ref.SelectedItem = m.x264options.reference;

            //mixed reference
            check_mixed_ref.IsChecked = m.x264options.mixedrefs;

            //cabac
            check_cabac.IsChecked = m.x264options.cabac;

            //fast p skip
            check_fast_pskip.IsChecked = m.x264options.no_fastpskip;

            //dct decimate
            check_dct_decimate.IsChecked = m.x264options.no_dctdecimate;

            //min-max quantizer
            num_min_quant.Value = m.x264options.min_quant;
            num_max_quant.Value = m.x264options.max_quant;
            num_step_quant.Value = m.x264options.step_quant;

            num_min_gop.Value = m.x264options.gop_min;
            num_max_gop.Value = m.x264options.gop_max;

            combo_badapt_mode.SelectedIndex = m.x264options.b_adapt;
            num_chroma_qp.Value = m.x264options.qp_offset;
            check_slow_first.IsChecked = m.x264options.slow_frstpass;
            check_nombtree.IsChecked = m.x264options.no_mbtree;
            num_lookahead.Value = m.x264options.lookahead;
            check_enable_psy.IsChecked = !m.x264options.no_psy;
            check_aud.IsChecked = m.x264options.aud;
            num_ratio_ip.Value = m.x264options.ratio_ip;
            num_ratio_pb.Value = m.x264options.ratio_pb;
            num_slices.Value = m.x264options.slices;
            check_pic_struct.IsChecked = m.x264options.pic_struct;
            check_fake_int.IsChecked = m.x264options.fake_int;
            check_full_range.IsChecked = m.x264options.full_range;
            combo_colorprim.SelectedItem = m.x264options.colorprim;
            combo_transfer.SelectedItem = m.x264options.transfer;
            combo_colormatrix.SelectedItem = m.x264options.colormatrix;
            combo_colorspace.SelectedItem = m.x264options.colorspace;
            check_non_deterministic.IsChecked = m.x264options.non_deterministic;
            check_bluray.IsChecked = m.x264options.bluray;

            //Кол-во потоков для x264-го
            if (m.x264options.threads == "auto") combo_threads_count.SelectedIndex = 0;
            else if (m.x264options.threads == "1" && m.x264options.thread_input) combo_threads_count.SelectedIndex = 2;
            else combo_threads_count.SelectedItem = m.x264options.threads;

            //Включаем-выключаем элементы.
            //Сначала на основе --preset.
            if (m.x264options.preset == Presets.Ultrafast)
            {
                check_slow_first.IsEnabled = !m.x264options.extra_cli.Contains("--slow-firstpass");
                check_cabac.IsEnabled = false;
                check_deblocking.IsEnabled = false;
                check_mixed_ref.IsEnabled = false;
                check_fast_pskip.IsEnabled = !m.x264options.extra_cli.Contains("--no-fast-pskip");
                check_8x8dct.IsEnabled = false;
                check_nombtree.IsEnabled = false;
                check_weightedb.IsEnabled = false;
            }
            else if (m.x264options.preset == Presets.Superfast)
            {
                check_slow_first.IsEnabled = !m.x264options.extra_cli.Contains("--slow-firstpass");
                check_mixed_ref.IsEnabled = false;
                check_fast_pskip.IsEnabled = !m.x264options.extra_cli.Contains("--no-fast-pskip");
                check_8x8dct.IsEnabled = !m.x264options.extra_cli.Contains("--no-8x8dct");
                check_nombtree.IsEnabled = false;
            }
            else if (m.x264options.preset == Presets.Veryfast || m.x264options.preset == Presets.Faster)
            {
                check_slow_first.IsEnabled = !m.x264options.extra_cli.Contains("--slow-firstpass");
                check_mixed_ref.IsEnabled = false;
                check_fast_pskip.IsEnabled = !m.x264options.extra_cli.Contains("--no-fast-pskip");
                check_8x8dct.IsEnabled = !m.x264options.extra_cli.Contains("--no-8x8dct");
                check_nombtree.IsEnabled = !m.x264options.extra_cli.Contains("--no-mbtree");
            }
            else if (m.x264options.preset == Presets.Placebo)
            {
                check_slow_first.IsEnabled = false;
                check_mixed_ref.IsEnabled = !m.x264options.extra_cli.Contains("--no-mixed-refs");
                check_fast_pskip.IsEnabled = false;
                check_8x8dct.IsEnabled = !m.x264options.extra_cli.Contains("--no-8x8dct");
                check_nombtree.IsEnabled = !m.x264options.extra_cli.Contains("--no-mbtree");
            }
            else
            {
                //Для остальных
                check_slow_first.IsEnabled = !m.x264options.extra_cli.Contains("--slow-firstpass");
                check_mixed_ref.IsEnabled = !m.x264options.extra_cli.Contains("--no-mixed-refs");
                check_fast_pskip.IsEnabled = !m.x264options.extra_cli.Contains("--no-fast-pskip");
                check_8x8dct.IsEnabled = !m.x264options.extra_cli.Contains("--no-8x8dct");
                check_nombtree.IsEnabled = !m.x264options.extra_cli.Contains("--no-mbtree");
            }

            //Tune Grain
            if (m.x264options.tune == Tunes.Grain)
            {
                check_dct_decimate.IsEnabled = false;
            }
            else
                check_dct_decimate.IsEnabled = !m.x264options.extra_cli.Contains("--no-dct-decimate");

            //Tune PSNR и SSIM
            if (m.x264options.tune == Tunes.PSNR || m.x264options.tune == Tunes.SSIM)
            {
                num_psyrdo.IsEnabled = num_psytrellis.IsEnabled = check_enable_psy.IsEnabled = false;
            }
            else
            {
                check_enable_psy.IsEnabled = !m.x264options.extra_cli.Contains("--no-psy");
                num_psyrdo.IsEnabled = num_psytrellis.IsEnabled = (!m.x264options.no_psy && !m.x264options.extra_cli.Contains("--psy-rd "));
            }

            //Tune FastDecode
            if (m.x264options.tune == Tunes.FastDecode)
            {
                check_cabac.IsEnabled = false;
                check_deblocking.IsEnabled = false;
                check_weightedb.IsEnabled = false;
            }
            else
            {
                check_cabac.IsEnabled = ((int)m.x264options.preset > 0) ? !m.x264options.extra_cli.Contains("--no-cabac") : false;
                check_deblocking.IsEnabled = ((int)m.x264options.preset > 0) ? !m.x264options.extra_cli.Contains("--no-deblock") : false;
                check_weightedb.IsEnabled = ((int)m.x264options.preset > 0) ? !m.x264options.extra_cli.Contains("--no-weightb") : false;
            }

            //Теперь на основе содержимого extra_cli
            combo_level.IsEnabled = !m.x264options.extra_cli.Contains("--level ");
            combo_subme.IsEnabled = !m.x264options.extra_cli.Contains("--subme ");
            combo_me.IsEnabled = !m.x264options.extra_cli.Contains("--me ");
            combo_merange.IsEnabled = !m.x264options.extra_cli.Contains("--merange ");
            combo_ref.IsEnabled = !m.x264options.extra_cli.Contains("--ref ");
            check_chroma.IsEnabled = !m.x264options.extra_cli.Contains("--no-chroma-me");
            check_p8x8.IsEnabled = check_p4x4.IsEnabled = check_i8x8.IsEnabled =
                check_b8x8.IsEnabled = check_i4x4.IsEnabled = !m.x264options.extra_cli.Contains("--analyse ");
            combo_bframes.IsEnabled = !m.x264options.extra_cli.Contains("--bframes ");
            combo_bframe_mode.IsEnabled = !m.x264options.extra_cli.Contains("--direct ");
            combo_badapt_mode.IsEnabled = !m.x264options.extra_cli.Contains("--b-adapt ");
            combo_bpyramid_mode.IsEnabled = !m.x264options.extra_cli.Contains("--b-pyramid ");
            combo_weightp_mode.IsEnabled = !m.x264options.extra_cli.Contains("--weightp ");
            num_lookahead.IsEnabled = (!m.x264options.extra_cli.Contains("--rc-lookahead ") && !m.x264options.no_mbtree);
            combo_trellis.IsEnabled = !m.x264options.extra_cli.Contains("--trellis ");
            combo_adapt_quant_mode.IsEnabled = !m.x264options.extra_cli.Contains("--aq-mode ");
            combo_adapt_quant.IsEnabled = !m.x264options.extra_cli.Contains("--ag-strength ");
            num_vbv_max.IsEnabled = !m.x264options.extra_cli.Contains("--vbv-maxrate ");
            num_vbv_buf.IsEnabled = !m.x264options.extra_cli.Contains("--vbv-bufsize ");
            num_vbv_init.IsEnabled = !m.x264options.extra_cli.Contains("--vbv-init ");
            num_min_gop.IsEnabled = !m.x264options.extra_cli.Contains("--min-keyint ");
            num_max_gop.IsEnabled = !m.x264options.extra_cli.Contains("--keyint ");
            num_min_quant.IsEnabled = !m.x264options.extra_cli.Contains("--qpmin ");
            num_max_quant.IsEnabled = !m.x264options.extra_cli.Contains("--qpmax ");
            num_step_quant.IsEnabled = !m.x264options.extra_cli.Contains("--qpstep ");
            num_qcomp.IsEnabled = !m.x264options.extra_cli.Contains("--qcomp ");
            num_chroma_qp.IsEnabled = !m.x264options.extra_cli.Contains("--chroma-qp-offset ");
            combo_nal_hrd.IsEnabled = !m.x264options.extra_cli.Contains("--nal-hrd ");
            check_aud.IsEnabled = !m.x264options.extra_cli.Contains("--aud");
            num_ratio_ip.IsEnabled = !m.x264options.extra_cli.Contains("--ipratio ");
            num_ratio_pb.IsEnabled = !m.x264options.extra_cli.Contains("--pbratio ");
            combo_open_gop.IsEnabled = !m.x264options.extra_cli.Contains("--open-gop");
            num_slices.IsEnabled = !m.x264options.extra_cli.Contains("--slices ");
            check_pic_struct.IsEnabled = !m.x264options.extra_cli.Contains("--pic-struct");
            check_fake_int.IsEnabled = !m.x264options.extra_cli.Contains("--fake-interlaced");
            check_full_range.IsEnabled = !m.x264options.extra_cli.Contains("--fullrange ");
            combo_colorprim.IsEnabled = !m.x264options.extra_cli.Contains("--colorprim ");
            combo_transfer.IsEnabled = !m.x264options.extra_cli.Contains("--transfer ");
            combo_colormatrix.IsEnabled = !m.x264options.extra_cli.Contains("--colormatrix ");
            combo_colorspace.IsEnabled = !m.x264options.extra_cli.Contains("--output-csp ");
            check_non_deterministic.IsEnabled = !m.x264options.extra_cli.Contains("--non-deterministic");
            check_bluray.IsEnabled = !m.x264options.extra_cli.Contains("--bluray-compat");

            //И дополнительно на основе --profile
            /*if (m.x264options.profile == Profiles.Baseline)
            {
                check_8x8dct.IsEnabled = false;
                combo_bframes.IsEnabled = false;
                check_cabac.IsEnabled = false;
                combo_weightp_mode.IsEnabled = false;
            }
            else if (m.x264options.profile == Profiles.Main)
            {
                check_8x8dct.IsEnabled = false;
            }*/

            SetAVCProfile();
            SetToolTips();
            UpdateCLI();
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

            string macroblocks = "";
            if (check_p8x8.IsChecked.Value) macroblocks += "p8x8,";
            if (check_b8x8.IsChecked.Value) macroblocks += "b8x8,";
            if (check_i8x8.IsChecked.Value) macroblocks += "i8x8,";
            if (check_i4x4.IsChecked.Value) macroblocks += "i4x4,";
            if (check_p4x4.IsChecked.Value) macroblocks += "p4x4,";

            return macroblocks.TrimEnd(new char[] { ',' });
        }

        private void SetAVCProfile()
        {
            /* x264.exe encoder/set.c
            sps->b_qpprime_y_zero_transform_bypass = param->rc.i_rc_method == X264_RC_CQP && param->rc.i_qp_constant == 0;
            if (sps->b_qpprime_y_zero_transform_bypass || sps->i_chroma_format_idc == CHROMA_444)
                sps->i_profile_idc = PROFILE_HIGH444_PREDICTIVE;
            else if (sps->i_chroma_format_idc == CHROMA_422)
                sps->i_profile_idc = PROFILE_HIGH422;
            else if (BIT_DEPTH > 8)
                sps->i_profile_idc = PROFILE_HIGH10;
            else if (param->analyse.b_transform_8x8 || param->i_cqm_preset != X264_CQM_FLAT)
                sps->i_profile_idc = PROFILE_HIGH;
            else if (param->b_cabac || param->i_bframe > 0 || param->b_interlaced || param->b_fake_interlaced || param->analyse.i_weighted_pred > 0)
                sps->i_profile_idc = PROFILE_MAIN;
            else
                sps->i_profile_idc = PROFILE_BASELINE;
            */

            string avcprofile = "Baseline";

            if (IsLossless(m) || m.x264options.colorspace == "I444" || m.x264options.colorspace == "RGB")
                avcprofile = "High 4:4:4";
            else if (m.x264options.colorspace == "I422")
                avcprofile = "High 4:2:2";
            else if (m.x264options.adaptivedct ||
                m.x264options.custommatrix != null)
                avcprofile = "High";
            else if (m.x264options.cabac ||
                m.x264options.bframes > 0 ||
                m.x264options.weightp > 0 ||
                m.x264options.fake_int)
                avcprofile = "Main";

            combo_avc_profile.SelectedIndex = (int)m.x264options.profile;

            ((ComboBoxItem)combo_avc_profile.Items.GetItemAt(0)).Content = "Auto (" + avcprofile + ")";
        }

        private void SetToolTips()
        {
            //Определяем дефолты (с учетом --preset и --tune)
            //x264_arguments def = new x264_arguments(m.x264options.preset, m.x264options.tune, m.x264options.profile);

            //Определяем дефолты без учета --preset и --tune, т.к. это именно дефолты самого энкодера
            x264_arguments def = new x264_arguments(Presets.Medium, Tunes.None, m.x264options.profile);

            CultureInfo cult_info = new CultureInfo("en-US");

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.ToolTip = "Set bitrate (Default: Auto)";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
                num_bitrate.ToolTip = "Set target quantizer (0 - " + ((m.x264options.profile == Profiles.High10) ? "81" : "69") + "). Lower - better quality but bigger filesize.\r\n(Default: 23)";
            else
                num_bitrate.ToolTip = "Set target quality (" + ((m.x264options.profile == Profiles.High10) ? "-12" : "0") + " - 51). Lower - better quality but bigger filesize.\r\n(Default: 23)";

            combo_mode.ToolTip = "Encoding mode";
            check_lossless.ToolTip = "Lossless encoding mode. High 4:4:4 AVC profile only!";
            combo_avc_profile.ToolTip = "Limit AVC profile (--profile, default: " + def.profile.ToString() + ")\r\n" +
                "Auto - don`t set the --profile key\r\nBaseline - forced --no-8x8dct --bframes 0 --no-cabac --weightp 0\r\nMain - forced --no-8x8dct\r\nHigh - no restrictions\r\n" +
                "High 10 - switching to 10-bit depth encoding!";
            combo_level.ToolTip = "Specify level (--level, default: Unrestricted)";
            combo_tune.ToolTip = "Tune the settings for a particular type of source or situation (--tune, default: " + def.tune.ToString() + ")";
            check_p8x8.ToolTip = "Enables partitions to consider: p8x8 (and also p16x8, p8x16)";
            check_p4x4.ToolTip = "Enables partitions to consider: p4x4 (and also p8x4, p4x8), requires p8x8";
            check_b8x8.ToolTip = "Enables partitions to consider: b8x8 (and also b16x8, b8x16)";
            check_i8x8.ToolTip = "Enables partitions to consider: i8x8, requires Adaptive DCT";
            check_i4x4.ToolTip = "Enables partitions to consider: i4x4";
            check_8x8dct.ToolTip = "Adaptive spatial transform size (--no-8x8dct if not checked)";
            combo_dstrength.ToolTip = "Deblocking filter strength (--deblock, default: " + def.deblocks + ":0)";
            combo_dthreshold.ToolTip = "Deblocking filter treshold (--deblock, default: 0:" + def.deblockt + ")";
            check_deblocking.ToolTip = "Deblocking filter (default: enabled)";
            combo_subme.ToolTip = "Subpixel motion estimation and mode decision (--subme, default: " + def.subme + ")\r\n" +
                "1 - fast\r\n7 - medium\r\n10 & 11 - slow, requires Trellis = 2 and AQ mode > 0";
            combo_me.ToolTip = "Integer pixel motion estimation method (--me, default: --me " + def.me + ")\r\n" +
                "Diamond Search, fast (--me dia)\r\nHexagonal Search (--me hex)\r\nUneven Multi-Hexagon Search (--me umh)\r\n" +
                "Exhaustive Search (--me esa)\r\nSATD Exhaustive Search, slow (--me tesa)";
            combo_merange.ToolTip = "Maximum motion vector search range (--merange, default: " + def.merange + ")";
            check_chroma.ToolTip = "Ignore chroma in motion estimation (--no-chroma-me, default: unchecked)";
            combo_bframes.ToolTip = "Number of B-frames between I and P (--bframes, default: " + def.bframes + ")";
            combo_bframe_mode.ToolTip = "Direct MV prediction mode, requires B-frames (--direct, default: " + def.direct + ")";
            combo_bpyramid_mode.ToolTip = "Keep some B-frames as references (--b-pyramid, default: " + (def.bpyramid == 0 ? "None)" : def.bpyramid == 1 ? "Strict)" : "Normal)") +
                "\r\nNone - disabled \r\nStrict - strictly hierarchical pyramid (Blu-ray compatible)\r\nNormal - non-strict (not Blu-ray compatible)";
            check_weightedb.ToolTip = "Weighted prediction for B-frames (--no-weightb if not checked)";
            combo_weightp_mode.ToolTip = "Weighted prediction for P-frames (--weightp, default: " + (def.weightp == 0 ? "Disabled)" : def.weightp == 1 ? "Blind offset)" : "Smart analysis)");
            combo_trellis.ToolTip = "Trellis RD quantization (--trellis, default: " + def.trellis + ")\r\n" +
                "0 - disabled\r\n1 - enabled only on the final encode of a MB\r\n2 - enabled on all mode decisions";
            combo_ref.ToolTip = "Number of reference frames (--ref, default: " + def.reference + ")";
            check_mixed_ref.ToolTip = "Decide references on a per partition basis (--no-mixed-refs if not checked)";
            check_cabac.ToolTip = "Enable CABAC (--no-cabac if not checked)";
            check_fast_pskip.ToolTip = "Disables early SKIP detection on P-frames (--no-fast-pskip, default: unchecked)";
            check_dct_decimate.ToolTip = "Disables coefficient thresholding on P-frames (--no-dct-decimate, default: unchecked)";
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
            num_psytrellis.ToolTip = "Strength of psychovisual Trellis optimization, requires Trellis >= 1 (--psy-rd, default: " + def.psytrellis.ToString(cult_info) + ")";
            num_vbv_max.ToolTip = "Max local bitrate, kbit/s (--vbv-maxrate, default: " + def.vbv_maxrate + ")";
            num_vbv_buf.ToolTip = "Set size of the VBV buffer, kbit (--vbv-bufsize, default: " + def.vbv_bufsize + ")";
            num_vbv_init.ToolTip = "Initial VBV buffer occupancy (--vbv-init, default: " + def.vbv_init.ToString(cult_info) + ")";
            num_qcomp.ToolTip = "QP curve compression (--qcomp, default: " + def.qcomp.ToString(cult_info) + ")\r\n0.00 => CBR, 1.00 => CQP";
            num_chroma_qp.ToolTip = "QP difference between chroma and luma (--chroma-qp-offset, default: " + def.qp_offset + ")";
            combo_threads_count.ToolTip = "Set number of threads for encoding (--threads, default: Auto)\r\n" +
                "Auto = 1.5 * logical_processors\r\n1+1 = --threads 1 --thread-input";
            check_slow_first.ToolTip = "Enable slow 1-st pass for multipassing encoding (off by default)" + Environment.NewLine + "(--slow-firstpass if checked)";
            check_nombtree.ToolTip = "Disable mb-tree ratecontrol (off by default, --no-mbtree if checked)";
            num_lookahead.ToolTip = "Number of frames for frametype lookahead (--rc-lookahead, default: " + def.lookahead + ")";
            check_enable_psy.ToolTip = "If unchecked disable all visual optimizations that worsen both PSNR and SSIM" + Environment.NewLine + "(--no-psy if not checked)";
            num_min_gop.ToolTip = "Minimum GOP size (--min-keyint, default: " + def.gop_min + ")\r\n0 - Auto";
            num_max_gop.ToolTip = "Maximum GOP size (--keyint, default: " + def.gop_max + ")\r\n0 - \"infinite\"";
            combo_nal_hrd.ToolTip = "Signal HRD information, requires VBV parameters (--nal-hrd, default: None)\r\nCBR not allowed in .mp4";
            check_aud.ToolTip = "Use Access Unit Delimiters (--aud, default: unchecked)";
            num_ratio_ip.ToolTip = "QP factor between I and P (--ipratio, default: " + def.ratio_ip.ToString(cult_info) + ")";
            num_ratio_pb.ToolTip = "QP factor between P and B (--pbratio, default: " + def.ratio_pb.ToString(cult_info) + ")";
            combo_open_gop.ToolTip = "Use recovery points to close GOPs, requires B-frames (--open-gop, default: " + (!def.open_gop ? "No" : "Yes") + ")";
            num_slices.ToolTip = "Number of slices per frame (--slices, default: " + def.slices + ")";
            check_pic_struct.ToolTip = "Force pic_struct in Picture Timing SEI (--pic-struct, default: unchecked)";
            check_fake_int.ToolTip = "Flag stream as interlaced but encode progressive (--fake-interlaced, default: unchecked)";
            check_full_range.ToolTip = "Specify full range samples setting (--fullrange on, default: unchecked)";
            combo_colorprim.ToolTip = "Specify color primaries (--colorprim, default: " + def.colorprim + ")";
            combo_transfer.ToolTip = "Specify transfer characteristics (--transfer, default: " + def.transfer + ")";
            combo_colormatrix.ToolTip = "Specify color matrix setting (--colormatrix, default: " + def.colormatrix + ")";
            combo_colorspace.ToolTip = "Specify output colorspace (--output-csp, default: " + def.colorspace + ")\r\nDo not change it if you don't know what you`re doing!";
            check_non_deterministic.ToolTip = "Slightly improve quality when encoding with --threads > 1, at the cost of non-deterministic output encodes\r\n(--non-deterministic, default: unchecked)";
            check_bluray.ToolTip = "Enable compatibility hacks for Blu-ray support (--bluray-compat, default: unchecked)";
        }

        public static Massive DecodeLine(Massive m)
        {
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

            //Создаём свежий массив параметров x264 (изменяя дефолты с учетом --preset и --tune)
            m.x264options = new x264_arguments(preset, tune, profile);
            m.x264options.profile = profile;
            m.x264options.preset = preset;
            m.x264options.tune = tune;

            int n = 0;
            string[] cli = line.Split(new string[] { " " }, StringSplitOptions.None);
            foreach (string value in cli)
            {
                if (m.vpasses.Count == 1)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.Quality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                    else if (value == "--bitrate" || value == "-B")
                    {
                        mode = Settings.EncodingModes.OnePass;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                    else if (value == "--qp" || value == "-q")
                    {
                        mode = Settings.EncodingModes.Quantizer;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }
                else if (m.vpasses.Count == 2)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.TwoPassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                    else if (value == "--bitrate" || value == "-B")
                    {
                        mode = Settings.EncodingModes.TwoPass;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                    else if (value == "--size")
                    {
                        mode = Settings.EncodingModes.TwoPassSize;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }
                else if (m.vpasses.Count == 3)
                {
                    if (value == "--crf")
                    {
                        mode = Settings.EncodingModes.ThreePassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                    else if (value == "--bitrate" || value == "-B")
                    {
                        mode = Settings.EncodingModes.ThreePass;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                    else if (value == "--size")
                    {
                        mode = Settings.EncodingModes.ThreePassSize;
                        m.outvbitrate = (int)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (value == "--level")
                    m.x264options.level = cli[n + 1];

                else if (value == "--ref" || value == "-r")
                    m.x264options.reference = Convert.ToInt32(cli[n + 1]);

                else if (value == "--aq-strength")
                    m.x264options.aqstrength = cli[n + 1];

                else if (value == "--aq-mode")
                    m.x264options.aqmode = Convert.ToInt32(cli[n + 1]);

                else if (value == "--no-psy")
                    m.x264options.no_psy = true;

                else if (value == "--psy-rd")
                {
                    string[] psyvalues = cli[n + 1].Split(new string[] { ":" }, StringSplitOptions.None);
                    m.x264options.psyrdo = (decimal)Calculate.ConvertStringToDouble(psyvalues[0]);
                    m.x264options.psytrellis = (decimal)Calculate.ConvertStringToDouble(psyvalues[1]);
                }

                else if (value == "--partitions" || value == "--analyse" || value == "-A")
                    m.x264options.analyse = cli[n + 1];

                else if (value == "--deblock" || value == "--filter" || value == "-f")
                {
                    string[] fvalues = cli[n + 1].Split(new string[] { ":" }, StringSplitOptions.None);
                    m.x264options.deblocks = Convert.ToInt32(fvalues[0]);
                    m.x264options.deblockt = Convert.ToInt32(fvalues[1]);
                }

                else if (value == "--subme" || value == "-m")
                    m.x264options.subme = Convert.ToInt32(cli[n + 1]);

                else if (value == "--me")
                    m.x264options.me = cli[n + 1];

                else if (value == "--merange")
                    m.x264options.merange = Convert.ToInt32(cli[n + 1]);

                else if (value == "--no-chroma-me")
                    m.x264options.no_chroma = true;

                else if (value == "--bframes" || value == "-b")
                    m.x264options.bframes = Convert.ToInt32(cli[n + 1]);

                else if (value == "--direct")
                    m.x264options.direct = cli[n + 1];

                else if (value == "--b-adapt")
                    m.x264options.b_adapt = Convert.ToInt32(cli[n + 1]);

                else if (value == "--b-pyramid")
                {
                    if ((cli[n + 1]) == "none" || (cli[n + 1]) == "0")
                        m.x264options.bpyramid = 0;
                    else if ((cli[n + 1]) == "strict" || (cli[n + 1]) == "1")
                        m.x264options.bpyramid = 1;
                    else if ((cli[n + 1]) == "normal" || (cli[n + 1]) == "2")
                        m.x264options.bpyramid = 2;
                }

                else if (value == "--no-weightb")
                    m.x264options.weightb = false;

                else if (value == "--weightp")
                    m.x264options.weightp = Convert.ToInt32(cli[n + 1]);

                else if (value == "--no-8x8dct")
                    m.x264options.adaptivedct = false;

                else if (value == "--trellis" || value == "-t")
                    m.x264options.trellis = Convert.ToInt32(cli[n + 1]);

                else if (value == "--no-mixed-refs")
                    m.x264options.mixedrefs = false;

                else if (value == "--no-cabac")
                    m.x264options.cabac = false;

                else if (value == "--no-fast-pskip")
                    m.x264options.no_fastpskip = true;

                else if (value == "--no-dct-decimate")
                    m.x264options.no_dctdecimate = true;

                else if (value == "--cqm")
                    m.x264options.custommatrix = cli[n + 1];

                else if (value == "--no-deblock" || value == "--nf")
                    m.x264options.deblocking = false;

                else if (value == "--qpmin")
                    m.x264options.min_quant = Convert.ToInt32(cli[n + 1]);

                else if (value == "--qpmax")
                    m.x264options.max_quant = Convert.ToInt32(cli[n + 1]);

                else if (value == "--qpstep")
                    m.x264options.step_quant = Convert.ToInt32(cli[n + 1]);

                else if (value == "--aud")
                    m.x264options.aud = true;

                else if (value == "--pictiming")
                    m.x264options.pictiming = true;

                else if (value == "--threads")
                    m.x264options.threads = cli[n + 1];

                else if (value == "--thread-input")
                    m.x264options.thread_input = true;

                else if (value == "--qcomp")
                    m.x264options.qcomp = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);

                else if (value == "--vbv-maxrate")
                    m.x264options.vbv_maxrate = Convert.ToInt32(cli[n + 1]);

                else if (value == "--vbv-bufsize")
                    m.x264options.vbv_bufsize = Convert.ToInt32(cli[n + 1]);

                else if (value == "--vbv-init")
                    m.x264options.vbv_init = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);

                else if (value == "--chroma-qp-offset")
                    m.x264options.qp_offset = Convert.ToInt32(cli[n + 1]);

                else if (value == "--slow-firstpass")
                    m.x264options.slow_frstpass = true;

                else if (value == "--no-mbtree")
                    m.x264options.no_mbtree = true;

                else if (value == "--rc-lookahead")
                    m.x264options.lookahead = Convert.ToInt32(cli[n + 1]);

                else if (value == "--nal-hrd")
                    m.x264options.nal_hrd = cli[n + 1];

                else if (value == "--min-keyint")
                    m.x264options.gop_min = Convert.ToInt32(cli[n + 1]);

                else if (value == "--keyint")
                {
                    int _value = 0;
                    Int32.TryParse(cli[n + 1], out _value);
                    m.x264options.gop_max = _value;
                }

                else if (value == "--ipratio")
                    m.x264options.ratio_ip = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);

                else if (value == "--pbratio")
                    m.x264options.ratio_pb = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);

                else if (value == "--open-gop")
                    m.x264options.open_gop = true;

                else if (value == "--slices")
                    m.x264options.slices = Convert.ToInt32(cli[n + 1]);

                else if (value == "--pic-struct")
                    m.x264options.pic_struct = true;

                else if (value == "--fake-interlaced")
                    m.x264options.fake_int = true;

                else if (value == "--fullrange")
                    m.x264options.full_range = (cli[n + 1] == "on");

                else if (value == "--colorprim")
                {
                    string _value = cli[n + 1].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x264options.colorprim = "Undefined";
                    else
                        m.x264options.colorprim = _value;
                }

                else if (value == "--transfer")
                {
                    string _value = cli[n + 1].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x264options.transfer = "Undefined";
                    else
                        m.x264options.transfer = _value;
                }

                else if (value == "--colormatrix")
                {
                    string _value = cli[n + 1].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x264options.colormatrix = "Undefined";
                    else
                        m.x264options.colormatrix = _value;
                }

                else if (value == "--output-csp")
                    m.x264options.colorspace = cli[n + 1].ToUpper();

                else if (value == "--non-deterministic")
                    m.x264options.non_deterministic = true;

                else if (value == "--bluray-compat")
                    m.x264options.bluray = true;

                else if (value == "--extra:")
                {
                    for (int i = n + 1; i < cli.Length; i++)
                        m.x264options.extra_cli += cli[i] + " ";

                    m.x264options.extra_cli = m.x264options.extra_cli.Trim();
                }

                n++;
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
            x264_arguments defaults = new x264_arguments(m.x264options.preset, m.x264options.tune, m.x264options.profile);

            //обнуляем старые строки
            m.vpasses.Clear();

            string line = "";

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
            line += " --preset " + m.x264options.preset.ToString().ToLower();

            if (m.x264options.tune != defaults.tune && !m.x264options.extra_cli.Contains("--tune "))
                line += " --tune " + m.x264options.tune.ToString().ToLower();

            if (m.x264options.profile != defaults.profile && !m.x264options.extra_cli.Contains("--profile "))
                line += " --profile " + m.x264options.profile.ToString().ToLower();

            if (m.x264options.level != defaults.level && !m.x264options.extra_cli.Contains("--level "))
                line += " --level " + m.x264options.level;

            if (m.x264options.reference != defaults.reference && !m.x264options.extra_cli.Contains("--ref "))
                line += " --ref " + m.x264options.reference;

            if (m.x264options.aqmode != defaults.aqmode && !m.x264options.extra_cli.Contains("--aq-mode "))
                line += " --aq-mode " + m.x264options.aqmode;

            if (m.x264options.aqstrength != defaults.aqstrength && m.x264options.aqmode != 0 && !m.x264options.extra_cli.Contains("--aq-strength "))
                line += " --aq-strength " + m.x264options.aqstrength;

            if (!m.x264options.cabac && defaults.cabac && !m.x264options.extra_cli.Contains("--no-cabac"))
                line += " --no-cabac";

            if (!m.x264options.mixedrefs && defaults.mixedrefs && !m.x264options.extra_cli.Contains("--no-mixed-refs"))
                line += " --no-mixed-refs";

            if (!m.x264options.deblocking && defaults.deblocking && !m.x264options.extra_cli.Contains("--no-deblock"))
                line += " --no-deblock";

            if (m.x264options.deblocking && (m.x264options.deblocks != defaults.deblocks || m.x264options.deblockt != defaults.deblockt) &&
                !m.x264options.extra_cli.Contains("--deblock "))
                line += " --deblock " + m.x264options.deblocks + ":" + m.x264options.deblockt;

            if (m.x264options.merange != defaults.merange && !m.x264options.extra_cli.Contains("--merange "))
                line += " --merange " + m.x264options.merange;

            if (m.x264options.no_chroma && !defaults.no_chroma && !m.x264options.extra_cli.Contains("--no-chroma-me"))
                line += " --no-chroma-me";

            if (m.x264options.bframes != defaults.bframes && !m.x264options.extra_cli.Contains("--bframes "))
                line += " --bframes " + m.x264options.bframes;

            if (m.x264options.direct != defaults.direct && !m.x264options.extra_cli.Contains("--direct "))
                line += " --direct " + m.x264options.direct;

            if (m.x264options.b_adapt != defaults.b_adapt && !m.x264options.extra_cli.Contains("--b-adapt "))
                line += " --b-adapt " + m.x264options.b_adapt;

            if (m.x264options.bpyramid != defaults.bpyramid && !m.x264options.extra_cli.Contains("--b-pyramid "))
            {
                if (m.x264options.bpyramid == 0)
                    line += " --b-pyramid none";
                else if (m.x264options.bpyramid == 1)
                    line += " --b-pyramid strict";
                else
                    line += " --b-pyramid normal";
            }

            if (!m.x264options.weightb && defaults.weightb && !m.x264options.extra_cli.Contains("--no-weightb"))
                line += " --no-weightb";

            if (m.x264options.weightp != defaults.weightp && !m.x264options.extra_cli.Contains("--weightp "))
                line += " --weightp " + m.x264options.weightp;

            if (m.x264options.trellis != defaults.trellis && !m.x264options.extra_cli.Contains("--trellis "))
                line += " --trellis " + m.x264options.trellis;

            if (m.x264options.no_fastpskip && !defaults.no_fastpskip && !m.x264options.extra_cli.Contains("--no-fast-pskip"))
                line += " --no-fast-pskip";

            if (m.x264options.no_dctdecimate && !defaults.no_dctdecimate && !m.x264options.extra_cli.Contains("--no-dct-decimate"))
                line += " --no-dct-decimate";

            if (m.x264options.custommatrix != defaults.custommatrix && !m.x264options.extra_cli.Contains("--cqm "))
                line += " --cqm " + m.x264options.custommatrix;

            if (m.x264options.min_quant != defaults.min_quant && !m.x264options.extra_cli.Contains("--qpmin "))
                line += " --qpmin " + m.x264options.min_quant;

            if (m.x264options.max_quant != defaults.max_quant && !m.x264options.extra_cli.Contains("--qpmax "))
                line += " --qpmax " + m.x264options.max_quant;

            if (m.x264options.step_quant != defaults.step_quant && !m.x264options.extra_cli.Contains("--qpstep "))
                line += " --qpstep " + m.x264options.step_quant;

            if (m.x264options.aud && !defaults.aud && !m.x264options.extra_cli.Contains("--aud"))
                line += " --aud";

            if (m.x264options.nal_hrd != defaults.nal_hrd && !m.x264options.extra_cli.Contains("--nal-hrd "))
                line += " --nal-hrd " + m.x264options.nal_hrd;

            if (m.x264options.pictiming && !defaults.pictiming && !m.x264options.extra_cli.Contains("--pictiming"))
                line += " --pictiming";

            if (m.x264options.no_psy && !defaults.no_psy && !m.x264options.extra_cli.Contains("--no-psy"))
                line += " --no-psy";

            if (!m.x264options.no_psy && (m.x264options.psyrdo != defaults.psyrdo || m.x264options.psytrellis != defaults.psytrellis) &&
                !m.x264options.extra_cli.Contains("--psy-rd "))
                line += " --psy-rd " + Calculate.ConvertDoubleToPointString((double)m.x264options.psyrdo, 2) + ":" +
                    Calculate.ConvertDoubleToPointString((double)m.x264options.psytrellis, 2);

            if (m.x264options.threads != defaults.threads && !m.x264options.extra_cli.Contains("--threads "))
            {
                if (m.x264options.threads == "1" && m.x264options.thread_input)
                    line += " --threads 1 --thread-input";
                else
                    line += " --threads " + m.x264options.threads;
            }

            if (m.x264options.qcomp != defaults.qcomp && !m.x264options.extra_cli.Contains("--qcomp "))
                line += " --qcomp " + Calculate.ConvertDoubleToPointString((double)m.x264options.qcomp, 2);

            if (m.x264options.vbv_maxrate != defaults.vbv_maxrate && !m.x264options.extra_cli.Contains("--vbv-maxrate "))
                line += " --vbv-maxrate " + m.x264options.vbv_maxrate;

            if (m.x264options.vbv_bufsize != defaults.vbv_bufsize && !m.x264options.extra_cli.Contains("--vbv-bufsize "))
                line += " --vbv-bufsize " + m.x264options.vbv_bufsize;

            if (m.x264options.vbv_init != defaults.vbv_init && !m.x264options.extra_cli.Contains("--vbv-init "))
                line += " --vbv-init " + Calculate.ConvertDoubleToPointString((double)m.x264options.vbv_init, 2);

            if (m.x264options.qp_offset != defaults.qp_offset && !m.x264options.extra_cli.Contains("--chroma-qp-offset "))
                line += " --chroma-qp-offset " + m.x264options.qp_offset;

            if (m.x264options.analyse != defaults.analyse && !m.x264options.extra_cli.Contains("--partitions "))
                line += " --partitions " + m.x264options.analyse;

            if (!m.x264options.adaptivedct && defaults.adaptivedct && !m.x264options.extra_cli.Contains("--no-8x8dct"))
                line += " --no-8x8dct";

            if (m.x264options.subme != defaults.subme && !m.x264options.extra_cli.Contains("--subme "))
                line += " --subme " + m.x264options.subme;

            if (m.x264options.me != defaults.me && !m.x264options.extra_cli.Contains("--me "))
                line += " --me " + m.x264options.me;

            if (m.x264options.slow_frstpass && !defaults.slow_frstpass && !m.x264options.extra_cli.Contains("--slow-firstpass"))
                line += " --slow-firstpass";

            if (m.x264options.no_mbtree && !defaults.no_mbtree && !m.x264options.extra_cli.Contains("--no-mbtree"))
                line += " --no-mbtree";

            if (!m.x264options.no_mbtree && m.x264options.lookahead != defaults.lookahead && !m.x264options.extra_cli.Contains("--rc-lookahead "))
                line += " --rc-lookahead " + m.x264options.lookahead;

            if (m.x264options.gop_min > 0 && m.x264options.gop_min != defaults.gop_min && !m.x264options.extra_cli.Contains("--min-keyint "))
                line += " --min-keyint " + m.x264options.gop_min;

            if (m.x264options.gop_max != defaults.gop_max && !m.x264options.extra_cli.Contains("--keyint "))
                line += " --keyint " + (m.x264options.gop_max > 0 ? m.x264options.gop_max.ToString() : "infinite");

            if (m.x264options.ratio_ip != defaults.ratio_ip && !m.x264options.extra_cli.Contains("--ipratio "))
                line += " --ipratio " + Calculate.ConvertDoubleToPointString((double)m.x264options.ratio_ip, 2);

            if (m.x264options.ratio_pb != defaults.ratio_pb && !m.x264options.extra_cli.Contains("--pbratio "))
                line += " --pbratio " + Calculate.ConvertDoubleToPointString((double)m.x264options.ratio_pb, 2);

            if (m.x264options.open_gop && !defaults.open_gop && !m.x264options.extra_cli.Contains("--open-gop"))
                line += " --open-gop";

            if (m.x264options.slices != defaults.slices && !m.x264options.extra_cli.Contains("--slices "))
                line += " --slices " + m.x264options.slices;

            if (m.x264options.pic_struct && !defaults.pic_struct && !m.x264options.extra_cli.Contains("--pic-struct"))
                line += " --pic-struct";

            if (m.x264options.fake_int && !defaults.fake_int && !m.x264options.extra_cli.Contains("--fake-interlaced"))
                line += " --fake-interlaced";

            if (m.x264options.full_range && !defaults.full_range && !m.x264options.extra_cli.Contains("--fullrange on"))
                line += " --fullrange on";

            if (m.x264options.colorprim != defaults.colorprim && !m.x264options.extra_cli.Contains("--colorprim "))
                line += " --colorprim " + ((m.x264options.colorprim == "Undefined") ? "undef" : m.x264options.colorprim);

            if (m.x264options.transfer != defaults.transfer && !m.x264options.extra_cli.Contains("--transfer "))
                line += " --transfer " + ((m.x264options.transfer == "Undefined") ? "undef" : m.x264options.transfer);

            if (m.x264options.colormatrix != defaults.colormatrix && !m.x264options.extra_cli.Contains("--colormatrix "))
                line += " --colormatrix " + ((m.x264options.colormatrix == "Undefined") ? "undef" : m.x264options.colormatrix);

            if (m.x264options.colorspace != defaults.colorspace && !m.x264options.extra_cli.Contains("--output-csp "))
                line += " --output-csp " + m.x264options.colorspace.ToLower();

            if (m.x264options.non_deterministic && !defaults.non_deterministic && !m.x264options.extra_cli.Contains("--non-deterministic"))
                line += " --non-deterministic";

            if (m.x264options.bluray && !defaults.bluray && !m.x264options.extra_cli.Contains("--bluray-compat"))
                line += " --bluray-compat";

            line += " --extra:";
            if (m.x264options.extra_cli != defaults.extra_cli)
                line += " " + m.x264options.extra_cli;

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
            if (combo_mode.IsDropDownOpen || combo_mode.IsSelectionBoxHighlighted)
            {
                try
                {
                    //запоминаем старый режим
                    oldmode = m.encodingmode;

                    string x264mode = combo_mode.SelectedItem.ToString();
                    if (x264mode == "1-Pass Bitrate") m.encodingmode = Settings.EncodingModes.OnePass;
                    else if (x264mode == "2-Pass Bitrate") m.encodingmode = Settings.EncodingModes.TwoPass;
                    else if (x264mode == "2-Pass Size") m.encodingmode = Settings.EncodingModes.TwoPassSize;
                    else if (x264mode == "3-Pass Bitrate") m.encodingmode = Settings.EncodingModes.ThreePass;
                    else if (x264mode == "3-Pass Size") m.encodingmode = Settings.EncodingModes.ThreePassSize;
                    else if (x264mode == "Constant Quality") m.encodingmode = Settings.EncodingModes.Quality;
                    else if (x264mode == "Constant Quantizer") m.encodingmode = Settings.EncodingModes.Quantizer;
                    else if (x264mode == "2-Pass Quality") m.encodingmode = Settings.EncodingModes.TwoPassQuality;
                    else if (x264mode == "3-Pass Quality") m.encodingmode = Settings.EncodingModes.ThreePassQuality;

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

                    check_lossless.IsChecked = IsLossless(m);

                    SetAVCProfile();
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
            if (combo_level.IsDropDownOpen || combo_level.IsSelectionBoxHighlighted)
            {
                m.x264options.level = combo_level.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_lossless_Click(object sender, RoutedEventArgs e)
        {
            if (check_lossless.IsChecked.Value)
            {
                combo_mode.SelectedItem = "Constant Quantizer";
                m.encodingmode = Settings.EncodingModes.Quantizer;
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                num_bitrate.Value = m.outvbitrate = 0;
                if (m.x264options.profile != Profiles.Auto && m.x264options.profile != Profiles.High10)
                {
                    combo_avc_profile.SelectedIndex = 0;
                    m.x264options.profile = Profiles.Auto;
                }

                SetToolTips();
                SetMinMaxBitrate();
            }
            else
                num_bitrate.Value = m.outvbitrate = 23;

            SetAVCProfile();
            root_window.UpdateOutSize();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_deblocking_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.deblocking = check_deblocking.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_dstrength_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_dstrength.IsDropDownOpen || combo_dstrength.IsSelectionBoxHighlighted)
            {
                m.x264options.deblocks = Convert.ToInt32(combo_dstrength.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_dthreshold_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_dthreshold.IsDropDownOpen || combo_dthreshold.IsSelectionBoxHighlighted)
            {
                m.x264options.deblockt = Convert.ToInt32(combo_dthreshold.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_subme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_subme.IsDropDownOpen || combo_subme.IsSelectionBoxHighlighted)
            {
                m.x264options.subme = combo_subme.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }

            if (m.x264options.subme < 6)
                num_psyrdo.Tag = "Inactive";
            else
                num_psyrdo.Tag = null;
        }

        private void combo_me_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_me.IsDropDownOpen || combo_me.IsSelectionBoxHighlighted)
            {
                string me = combo_me.SelectedItem.ToString();
                if (me == "Diamond") m.x264options.me = "dia";
                else if (me == "Hexagon") m.x264options.me = "hex";
                else if (me == "Multi Hexagon") m.x264options.me = "umh";
                else if (me == "Exhaustive") m.x264options.me = "esa";
                else if (me == "SATD Exhaustive") m.x264options.me = "tesa";

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_merange_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_merange.IsDropDownOpen || combo_merange.IsSelectionBoxHighlighted)
            {
                m.x264options.merange = Convert.ToInt32(combo_merange.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_chroma_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_chroma = check_chroma.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted)
            {
                m.x264options.bframes = Convert.ToInt32(combo_bframes.SelectedItem);
                SetAVCProfile();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }

            if (m.x264options.bframes > 0)
            {
                combo_bpyramid_mode.Tag = (m.x264options.bframes > 1) ? null : "Inactive";
                combo_bframe_mode.Tag = null;
                combo_badapt_mode.Tag = null;
                combo_open_gop.Tag = null;
                check_weightedb.Tag = null;
            }
            else
            {
                combo_bpyramid_mode.Tag = "Inactive";
                combo_bframe_mode.Tag = "Inactive";
                combo_badapt_mode.Tag = "Inactive";
                combo_open_gop.Tag = "Inactive";
                check_weightedb.Tag = "Inactive";
            }
        }

        private void combo_bframe_mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bframe_mode.IsDropDownOpen || combo_bframe_mode.IsSelectionBoxHighlighted)
            {
                string bmode = combo_bframe_mode.SelectedItem.ToString();
                if (bmode == "Disabled") m.x264options.direct = "none";
                else if (bmode == "Spatial") m.x264options.direct = "spatial";
                else if (bmode == "Temporal") m.x264options.direct = "temporal";
                else if (bmode == "Auto") m.x264options.direct = "auto";

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_bpyramid_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_bpyramid_mode.IsDropDownOpen || combo_bpyramid_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.bpyramid = combo_bpyramid_mode.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_weightedb_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.weightb = check_weightedb.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_weightp_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_weightp_mode.IsDropDownOpen || combo_weightp_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.weightp = combo_weightp_mode.SelectedIndex;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_8x8dct_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.adaptivedct = check_8x8dct.IsChecked.Value;
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_i4x4_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_p4x4_Click(object sender, RoutedEventArgs e)
        {
            if (check_p4x4.IsChecked.Value && !check_p8x8.IsChecked.Value)
                check_p8x8.IsChecked = true;
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_i8x8_Click(object sender, RoutedEventArgs e)
        {
            if (check_i8x8.IsChecked.Value && !check_8x8dct.IsChecked.Value)
            {
                check_8x8dct.IsChecked = true;
                m.x264options.adaptivedct = true;
            }
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_p8x8_Click(object sender, RoutedEventArgs e)
        {
            if (!check_p8x8.IsChecked.Value && check_p4x4.IsChecked.Value)
                check_p4x4.IsChecked = false;
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_b8x8_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_trellis_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_trellis.IsDropDownOpen || combo_trellis.IsSelectionBoxHighlighted)
            {
                m.x264options.trellis = combo_trellis.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }

            if (m.x264options.trellis > 0)
                num_psytrellis.Tag = null;
            else
                num_psytrellis.Tag = "Inactive";
        }

        private void combo_ref_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_ref.IsDropDownOpen || combo_ref.IsSelectionBoxHighlighted)
            {
                m.x264options.reference = Convert.ToInt32(combo_ref.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }

            if (m.x264options.reference < 2)
                check_mixed_ref.Tag = "Inactive";
            else
                check_mixed_ref.Tag = null;
        }

        private void num_min_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_quant.IsAction)
            {
                m.x264options.min_quant = Convert.ToInt32(num_min_quant.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_max_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_quant.IsAction)
            {
                m.x264options.max_quant = Convert.ToInt32(num_max_quant.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_step_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_step_quant.IsAction)
            {
                m.x264options.step_quant = Convert.ToInt32(num_step_quant.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_mixed_ref_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.mixedrefs = check_mixed_ref.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_cabac_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.cabac = check_cabac.IsChecked.Value;
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_fast_pskip_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_fastpskip = check_fast_pskip.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_dct_decimate_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_dctdecimate = check_dct_decimate.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_avc_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_avc_profile.IsDropDownOpen || combo_avc_profile.IsSelectionBoxHighlighted)
            {
                /*Overrides all settings.
                  - baseline: --no-8x8dct --bframes 0 --no-cabac --weightp 0 --cqm flat No interlaced. No lossless.
                  - main:     --no-8x8dct --cqm flat No lossless.
                  - high:     No lossless.
                  - high10:   No lossless. Support for bit depth 8-10.*/

                m.x264options.profile = (Profiles)Enum.ToObject(typeof(Profiles), combo_avc_profile.SelectedIndex);

                //Тултипы и дефолты под 8\10-bit
                SetToolTips();
                SetMinMaxBitrate();

                //Проверяем выход за лимиты 8-ми и 10-ти битных версий
                if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                {
                    //Не 0, потому-что (0..1)=Lossless
                    if (m.x264options.profile != Profiles.High10 && m.outvbitrate < 1)
                    {
                        check_lossless.IsChecked = false;
                        num_bitrate.Value = m.outvbitrate = 1;
                    }
                }
                else if (m.encodingmode == Settings.EncodingModes.Quantizer)
                {
                    if (m.x264options.profile != Profiles.High10 && m.outvbitrate > 69)
                        num_bitrate.Value = m.outvbitrate = 69;
                }

                if (m.x264options.profile != Profiles.High10)
                {
                    if (m.x264options.max_quant > 69)
                        num_max_quant.Value = m.x264options.max_quant = 69;
                }
                else
                {
                    if (m.x264options.max_quant == 69)
                        num_max_quant.Value = m.x264options.max_quant = 81;
                }

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
                m.x264options.preset = (Presets)Enum.ToObject(typeof(Presets), (int)slider_preset.Value);
                x264_arguments defaults = new x264_arguments(m.x264options.preset, m.x264options.tune, m.x264options.profile);
                m.x264options.adaptivedct = defaults.adaptivedct;
                m.x264options.analyse = defaults.analyse;
                m.x264options.aqmode = defaults.aqmode;
                m.x264options.b_adapt = defaults.b_adapt;
                m.x264options.bframes = defaults.bframes;
                m.x264options.bpyramid = defaults.bpyramid;
                m.x264options.cabac = defaults.cabac;
                m.x264options.deblocking = defaults.deblocking;
                m.x264options.direct = defaults.direct;//
                m.x264options.lookahead = defaults.lookahead;
                m.x264options.me = defaults.me;
                m.x264options.merange = defaults.merange;
                m.x264options.mixedrefs = defaults.mixedrefs;
                m.x264options.no_fastpskip = defaults.no_fastpskip;
                m.x264options.no_mbtree = defaults.no_mbtree;
                m.x264options.psyrdo = defaults.psyrdo;//
                m.x264options.reference = defaults.reference;
                m.x264options.slow_frstpass = defaults.slow_frstpass;
                m.x264options.subme = defaults.subme;
                m.x264options.trellis = defaults.trellis;
                m.x264options.weightb = defaults.weightb;
                m.x264options.weightp = defaults.weightp;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.x264options (это нужно для "--extra:")
                LoadFromProfile(); //Выставляем значения из m.x264options в элементы управления
            }
        }

        private void combo_tune_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_tune.IsDropDownOpen || combo_tune.IsSelectionBoxHighlighted)
            {
                //Создаем новые параметры с учетом --tune, и берем от них только те, от которых зависит tune
                m.x264options.tune = (Tunes)Enum.ToObject(typeof(Tunes), combo_tune.SelectedIndex);
                x264_arguments defaults = new x264_arguments(m.x264options.preset, m.x264options.tune, m.x264options.profile);
                m.x264options.aqmode = defaults.aqmode;
                m.x264options.aqstrength = defaults.aqstrength;
                m.x264options.bframes = defaults.bframes;
                m.x264options.cabac = defaults.cabac;
                m.x264options.deblocking = defaults.deblocking;
                m.x264options.deblocks = defaults.deblocks;
                m.x264options.deblockt = defaults.deblockt;
                m.x264options.no_dctdecimate = defaults.no_dctdecimate;
                m.x264options.no_psy = defaults.no_psy;
                m.x264options.psyrdo = defaults.psyrdo;
                m.x264options.psytrellis = defaults.psytrellis;
                m.x264options.qcomp = defaults.qcomp;
                m.x264options.ratio_ip = defaults.ratio_ip;
                m.x264options.ratio_pb = defaults.ratio_pb;
                m.x264options.reference = defaults.reference;
                m.x264options.weightb = defaults.weightb;
                m.x264options.weightp = defaults.weightp;

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.x264options (это нужно для "--extra:")
                LoadFromProfile(); //Выставляем значения из m.x264options в элементы управления
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
                num_bitrate.Minimum = 0;
                num_bitrate.Maximum = (m.x264options.profile == Profiles.High10) ? 81 : 69;
            }
            else
            {
                num_bitrate.Minimum = (m.x264options.profile == Profiles.High10) ? -12 : 0;
                num_bitrate.Maximum = 51;
            }

            //Сюда же
            num_max_quant.Maximum = (m.x264options.profile == Profiles.High10) ? 81 : 69;
        }

        private void num_bitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bitrate.IsAction)
            {
                m.outvbitrate = num_bitrate.Value;
                check_lossless.IsChecked = IsLossless(m);

                SetAVCProfile();
                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_psyrdo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_psyrdo.IsAction)
            {
                m.x264options.psyrdo = num_psyrdo.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_psytrellis_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_psytrellis.IsAction)
            {
                m.x264options.psytrellis = num_psytrellis.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_adapt_quant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_adapt_quant.IsDropDownOpen || combo_adapt_quant.IsSelectionBoxHighlighted)
            {
                m.x264options.aqstrength = combo_adapt_quant.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_adapt_quant_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_adapt_quant_mode.IsDropDownOpen || combo_adapt_quant_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.aqmode = combo_adapt_quant_mode.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_threads_count_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_threads_count.IsDropDownOpen || combo_threads_count.IsSelectionBoxHighlighted)
            {
                if (combo_threads_count.SelectedIndex == 2)
                {
                    m.x264options.threads = "1";
                    m.x264options.thread_input = true;
                }
                else
                {
                    m.x264options.threads = combo_threads_count.SelectedItem.ToString().ToLower();
                    m.x264options.thread_input = false;
                }
                
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_badapt_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_badapt_mode.IsDropDownOpen || combo_badapt_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.b_adapt = combo_badapt_mode.SelectedIndex;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_qcomp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_qcomp.IsAction)
            {
                m.x264options.qcomp = num_qcomp.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_max.IsAction)
            {
                m.x264options.vbv_maxrate = Convert.ToInt32(num_vbv_max.Value);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_buf_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_buf.IsAction)
            {
                m.x264options.vbv_bufsize = Convert.ToInt32(num_vbv_buf.Value);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_init_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_init.IsAction)
            {
                m.x264options.vbv_init = num_vbv_init.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }
        
        private void num_chroma_qp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_chroma_qp.IsAction)
            {
                m.x264options.qp_offset = (int)num_chroma_qp.Value;

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_slow_first_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.slow_frstpass = check_slow_first.IsChecked.Value;

            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_nombtree_Clicked(object sender, RoutedEventArgs e)
        {
            m.x264options.no_mbtree = check_nombtree.IsChecked.Value;
            num_lookahead.IsEnabled = (!m.x264options.extra_cli.Contains("--rc-lookahead ") && !m.x264options.no_mbtree);

            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void num_lookahead_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_lookahead.IsAction)
            {
                m.x264options.lookahead = Convert.ToInt32(num_lookahead.Value);

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_enable_psy_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_psy = !check_enable_psy.IsChecked.Value;
            num_psyrdo.IsEnabled = num_psytrellis.IsEnabled = (!m.x264options.no_psy && !m.x264options.extra_cli.Contains("--psy-rd "));

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

                DecodeLine(m);                       //- Загружаем в массив m.x264 значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                   //- Загружаем в форму значения, на основе значений массива m.x264
                m.vencoding = "Custom x264 CLI";     //- Изменяем название пресета           
                PresetLoader.CreateVProfile(m);      //- Перезаписываем файл пресета (m.vpasses[x])
                root_window.m = this.m.Clone();      //- Передаем массив в основное окно
                root_window.LoadProfiles();          //- Обновляем название выбранного пресета в основном окне (Custom x264 CLI)
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
                DecodeLine(m);                           //- Загружаем в массив m.x264 значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                       //- Загружаем в форму значения, на основе значений массива m.x264
                root_window.m = this.m.Clone();          //- Передаем массив в основное окно
            }
            else
            {
                new Message(root_window).ShowMessage("Can`t find good CLI...", Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void button_x264_help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                help.FileName = Calculate.StartupPath + "\\apps\\" + (m.x264options.profile == x264.Profiles.High10 ? "x264_10b\\" : "x264\\") + ((Settings.Use64x264) ? "x264_64.exe" : "x264.exe");
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = " --fullhelp";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                string title = "x264 " + (m.x264options.profile == x264.Profiles.High10 ? "10" : "8") + "-bit depth " + ((Settings.Use64x264) ? "(64-bit) " : "") + "--fullhelp";
                new ShowWindow(root_window, title, p.StandardOutput.ReadToEnd().Replace("\n", "\r\n"), new FontFamily("Lucida Console"));
            }
            catch (Exception ex)
            {
                new Message(root_window).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void combo_nal_hrd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_nal_hrd.IsDropDownOpen || combo_nal_hrd.IsSelectionBoxHighlighted)
            {
                m.x264options.nal_hrd = combo_nal_hrd.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_aud_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.aud = check_aud.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void num_min_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_gop.IsAction)
            {
                m.x264options.gop_min = Convert.ToInt32(num_min_gop.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_max_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_gop.IsAction)
            {
                m.x264options.gop_max = Convert.ToInt32(num_max_gop.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_ratio_ip_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ratio_ip.IsAction)
            {
                m.x264options.ratio_ip = num_ratio_ip.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_ratio_pb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ratio_pb.IsAction)
            {
                m.x264options.ratio_pb = num_ratio_pb.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_open_gop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_open_gop.IsDropDownOpen || combo_open_gop.IsSelectionBoxHighlighted)
            {
                m.x264options.open_gop = (combo_open_gop.SelectedIndex == 1);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_slices_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_slices.IsAction)
            {
                m.x264options.slices = Convert.ToInt32(num_slices.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_pic_struct_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.pic_struct = check_pic_struct.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_fake_int_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.fake_int = check_fake_int.IsChecked.Value;
            SetAVCProfile();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_full_range_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.full_range = check_full_range.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_colorprim_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_colorprim.IsDropDownOpen || combo_colorprim.IsSelectionBoxHighlighted)
            {
                m.x264options.colorprim = combo_colorprim.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_transfer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_transfer.IsDropDownOpen || combo_transfer.IsSelectionBoxHighlighted)
            {
                m.x264options.transfer = combo_transfer.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colormatrix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_colormatrix.IsDropDownOpen || combo_colormatrix.IsSelectionBoxHighlighted)
            {
                m.x264options.colormatrix = combo_colormatrix.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colorspace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_colorspace.IsDropDownOpen || combo_colorspace.IsSelectionBoxHighlighted)
            {
                m.x264options.colorspace = combo_colorspace.SelectedItem.ToString();
                SetAVCProfile();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_non_deterministic_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.non_deterministic = check_non_deterministic.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_bluray_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.bluray = check_bluray.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        public static bool IsLossless(Massive m)
        {
            if (m.encodingmode == Settings.EncodingModes.Quantizer)
                return (m.outvbitrate == 0);
            else if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                if (m.x264options.profile == Profiles.High10)
                    return (m.outvbitrate < -11);
                else
                    return (m.outvbitrate < 1);
            }
            else
                return false;
        }
    }
}