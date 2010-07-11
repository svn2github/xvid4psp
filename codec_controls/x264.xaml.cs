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
using System.Globalization;

namespace XviD4PSP
{
    public partial class x264
    {
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum CodecPresets { Custom, Ultrafast, Superfast, Veryfast, Faster, Fast, Medium, Slow, Slower, Veryslow, Placebo }
        private ArrayList good_cli = null;

        public x264(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
        {
            this.InitializeComponent();

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
            for (double n = 0.1; n <= 2.1; n += 0.1)
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
            combo_subme.Items.Add("10 - QP-RD");

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

            //пресеты настроек и качества
            foreach (string preset in Enum.GetNames(typeof(CodecPresets)))
                combo_codec_preset.Items.Add(preset);

            //Кол-во потоков для x264-го
            combo_threads_count.Items.Add("Auto");
            for (int n = 1; n <= 12; n++)
                combo_threads_count.Items.Add(Convert.ToString(n));

            //-b-adapt
            combo_badapt_mode.Items.Add("Disabled");
            combo_badapt_mode.Items.Add("Fast");
            combo_badapt_mode.Items.Add("Optimal");

            for (int n = -16; n <= 16; n++)
                combo_chroma_qp.Items.Add(Convert.ToString(n));

            //--b-pyramid
            combo_bpyramid_mode.Items.Add("None");
            combo_bpyramid_mode.Items.Add("Strict");
            combo_bpyramid_mode.Items.Add("Normal");

            //--weightp
            combo_weightp_mode.Items.Add("Disabled");
            combo_weightp_mode.Items.Add("Blind offset");
            combo_weightp_mode.Items.Add("Smart analysis");

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
            if (m.x264options.me == "dia") combo_me.SelectedItem = "Diamond";
            else if (m.x264options.me == "hex") combo_me.SelectedItem = "Hexagon";
            else if (m.x264options.me == "umh") combo_me.SelectedItem = "Multi Hexagon";
            else if (m.x264options.me == "esa") combo_me.SelectedItem = "Exhaustive";
            else if (m.x264options.me == "tesa") combo_me.SelectedItem = "SATD Exhaustive";

            //прописываем me range
            combo_merange.SelectedItem = m.x264options.merange;

            //прописываем chroma me
            check_chroma.IsChecked = m.x264options.no_chroma;

            //B фреймы
            combo_bframes.SelectedItem = m.x264options.bframes;

            //режим B фреймов
            if (m.x264options.direct == "none") combo_bframe_mode.SelectedItem = "Disabled";
            else if (m.x264options.direct == "spatial") combo_bframe_mode.SelectedItem = "Spatial";
            else if (m.x264options.direct == "temporal") combo_bframe_mode.SelectedItem = "Temporal";
            else if (m.x264options.direct == "auto") combo_bframe_mode.SelectedItem = "Auto";

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

            //Прогружаем список AVC profile
            SetAVCProfile();

            //min-max quantizer
            num_min_quant.Value = m.x264options.min_quant;
            num_max_quant.Value = m.x264options.max_quant;
            num_step_quant.Value = m.x264options.step_quant;

            num_min_gop.Value = m.x264options.gop_min;
            num_max_gop.Value = m.x264options.gop_max;

            //Кол-во потоков для x264-го
            if (m.x264options.threads == "auto") combo_threads_count.SelectedItem = "Auto";
            else combo_threads_count.SelectedItem = m.x264options.threads;

            //-b-adapt            
            combo_badapt_mode.SelectedIndex = m.x264options.b_adapt;

            combo_chroma_qp.SelectedItem = m.x264options.qp_offset;

            check_slow_first.IsChecked = m.x264options.slow_frstpass;

            check_nombtree.IsChecked = m.x264options.no_mbtree;

            if (m.x264options.no_mbtree == true) num_lookahead.IsEnabled = false;
            else num_lookahead.IsEnabled = true;

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

            check_nal_hrd.IsChecked = m.x264options.nal_hrd;

            check_aud.IsChecked = m.x264options.aud;

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
            /* x264.exe
               if( sps->b_qpprime_y_zero_transform_bypass )
                   sps->i_profile_idc  = PROFILE_HIGH444_PREDICTIVE;
               else if( param->analyse.b_transform_8x8 || param->i_cqm_preset != X264_CQM_FLAT )
                   sps->i_profile_idc  = PROFILE_HIGH;
               else if( param->b_cabac || param->i_bframe > 0 || param->b_interlaced || param->analyse.i_weighted_pred > 0 )
                   sps->i_profile_idc  = PROFILE_MAIN;
               else
                   sps->i_profile_idc  = PROFILE_BASELINE;
            */

            string avcprofile = "Baseline Profile";

            if (m.x264options.adaptivedct ||
                m.x264options.adaptivedct && m.x264options.cabac ||
                m.outvbitrate == 0 ||
                m.x264options.custommatrix != null)
                avcprofile = "High Profile";
            else if (m.x264options.cabac ||
                     m.x264options.bframes > 0 ||
                     //m.x264options.weightb ||
                     m.x264options.weightp > 0)
                avcprofile = "Main Profile";

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
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
                num_bitrate.ToolTip = "Set target quantizer. Lower - better quality but bigger filesize.\r\n(Default: 23)";
            else
                num_bitrate.ToolTip = "Set target quality. Lower - better quality but bigger filesize.\r\n(Default: 23)";

            check_lossless.ToolTip = "Lossless encoding mode. High AVC profile only";
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
            combo_subme.ToolTip = "Subpixel motion estimation and mode decision (--subme, default: 7):" + Environment.NewLine + 
                "1: fast\r\n7: default\r\n10: slow, requires trellis=2 and aq-mode>0";
            combo_me.ToolTip = "Integer pixel motion estimation method (--me, default: --me hex)" + Environment.NewLine +
                "Diamond Search, fast (--me dia)" + Environment.NewLine +
                "Hexagonal Search (--me hex)" + Environment.NewLine +
                "Uneven Multi-Hexagon Search (--me umh)" + Environment.NewLine +
                "Exhaustive Search (--me esa)" + Environment.NewLine +
                "SATD Exhaustive Search, slow (--me tesa)";
            combo_merange.ToolTip = "Maximum motion vector search range (--merange, default: 16)";
            check_chroma.ToolTip = "Ignore chroma in motion estimation (--no-chroma-me, default: unchecked)";
            combo_bframes.ToolTip = "Number of B-frames between I and P (--bframes, default: 3)";
            combo_bframe_mode.ToolTip = "Direct MV prediction mode (--direct, default: Spatial)";
            combo_bpyramid_mode.ToolTip = "Keep some B-frames as references (--b-pyramid, default: Normal)\r\nNone: disabled \r\nStrict: strictly hierarchical pyramid (Blu-ray compatible)\r\nNormal: non-strict (not Blu-ray compatible)";
            check_weightedb.ToolTip = "Weighted prediction for B-frames (--no-weightb if not checked)";
            combo_weightp_mode.ToolTip = "Weighted prediction for P-frames (--weightp, default: Smart)";
            combo_trellis.ToolTip = "Trellis RD quantization, requires CABAC (--trellis, default: 1)" + Environment.NewLine +
                "0: disabled" + Environment.NewLine +
                "1: enabled only on the final encode of a MB" + Environment.NewLine +
                "2: enabled on all mode decisions";
            combo_ref.ToolTip = "Number of reference frames (--ref, default: 3)";
            check_mixed_ref.ToolTip = "Decide references on a per partition basis (--no-mixed-refs if not checked)";
            check_cabac.ToolTip = "Enable CABAC (--no-cabac if not checked)";
            check_fast_pskip.ToolTip = "Disables early SKIP detection on P-frames (--no-fast-pskip, default: unchecked)";
            check_dct_decimate.ToolTip = "Disables coefficient thresholding on P-frames (--no-dct-decimate, default: unchecked)";
            combo_avc_profile.ToolTip = "AVC profile";
            num_min_quant.ToolTip = "Set min QP (--qpmin, default: 10)";
            num_max_quant.ToolTip = "Set max QP (--qpmax, default: 51)";
            num_step_quant.ToolTip = "Set max QP step (--qpstep, default: 4)";
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                combo_codec_preset.ToolTip = "Set encoding preset:" + Environment.NewLine +
                    "Ultrafast - fastest encoding, but biggest output file size" + Environment.NewLine +
                    "Medium - default, good speed and medium file size" + Environment.NewLine +
                    "Veryslow - high quality encoding, small file size" + Environment.NewLine +
                    "Placebo - super high quality encoding, smallest file size" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            else
            {
                combo_codec_preset.ToolTip = "Set encoding preset:" + Environment.NewLine +
                    "Ultrafast - fastest encoding, bad quality" + Environment.NewLine +
                    "Medium - default, optimal speed-quality solution" + Environment.NewLine +
                    "Veryslow - high quality encoding" + Environment.NewLine +
                    "Placebo - super high quality encoding" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            combo_badapt_mode.ToolTip = "Adaptive B-frame decision method (--b-adapt, default: Fast)";
            combo_adapt_quant_mode.ToolTip = "AQ mode (--aq-mode, default: 1)" + Environment.NewLine +
                        "0: Disabled" + Environment.NewLine +
                        "1: Variance AQ (complexity mask)" + Environment.NewLine +
                        "2: Auto-variance AQ (experimental)";
            combo_adapt_quant.ToolTip = "AQ Strength (--ag-strength, default: 1.0)" + Environment.NewLine +
                        "Reduces blocking and blurring in flat and textured areas" + Environment.NewLine +
                        "0.5: weak AQ, 1.5: strong AQ";
            num_psyrdo.ToolTip = "Strength of psychovisual RD optimization (--psy-rd, default: 1.0)";
            num_psytrellis.ToolTip = "Strength of psychovisual Trellis optimization (--psy-rd, default: 0.0)";
            num_vbv_buf.ToolTip = "Set size of the VBV buffer, kbit (--vbv-bufsize, default: 0)";
            num_vbv_max.ToolTip = "Max local bitrate, kbit/s (--vbv-maxrate, default: 0)";
            num_qcomp.ToolTip = "QP curve compression (--qcomp, default: 0.60)" + Environment.NewLine +
                        "0.00 => CBR, 1.00 => CQP";
            combo_chroma_qp.ToolTip = "QP difference between chroma and luma (--qp-chroma-offset Default: 0)";
            combo_threads_count.ToolTip = "Set number of threads for encoding (--threads, default: Auto)";
            check_slow_first.ToolTip = "Enable slow 1-st pass for multipassing encoding (off by default)" + Environment.NewLine + "(--slow-firstpass if checked)";
            check_nombtree.ToolTip = "Disable mb-tree ratecontrol (off by default, --no-mbtree if checked)";
            num_lookahead.ToolTip = "Number of frames for frametype lookahead (--rc-lookahead, default: 40)";
            check_enable_psy.ToolTip = "If unchecked disable all visual optimizations that worsen both PSNR and SSIM" + Environment.NewLine + "(--no-psy if not checked)";
            num_min_gop.ToolTip = "Minimum GOP size (--min-keyint, default: 25)";
            num_max_gop.ToolTip = "Maximum GOP size (--keyint, default: 250)";
            check_nal_hrd.ToolTip = "Signal HRD information, requires VBV parameters (--nal-hrd vbr, default: unchecked)";
            check_aud.ToolTip = "Use Access Unit Delimiters (--aud, default: unchecked)";
        }

        public static Massive DecodeLine(Massive m)
        {
            //создаём свежий массив параметров x264
            m.x264options = new x264_arguments();

            Settings.EncodingModes mode = new Settings.EncodingModes();

            //берём пока что за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

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
                        m.outvbitrate = Convert.ToInt32(cli[n + 1]);
                    }
                    else if (value == "--qp" || value == "-q")
                    {
                        mode = Settings.EncodingModes.Quantizer;
                        m.outvbitrate = Convert.ToInt32(cli[n + 1]);
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
                        m.outvbitrate = Convert.ToInt32(cli[n + 1]);
                    }
                    else if (value == "--size")
                    {
                        mode = Settings.EncodingModes.TwoPassSize;
                        m.outvbitrate = Convert.ToInt32(cli[n + 1]);
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
                        m.outvbitrate = Convert.ToInt32(cli[n + 1]);
                    }
                    else if (value == "--size")
                    {
                        mode = Settings.EncodingModes.ThreePassSize;
                        m.outvbitrate = Convert.ToInt32(cli[n + 1]);
                    }
                }

                if (value == "--level")
                    m.x264options.level = cli[n + 1];

                else if (value == "--ref" || value == "-r")
                    m.x264options.reference = Convert.ToInt32(cli[n + 1]);

                else if (value == "--aq-strength")
                {
                    string aqvalue = cli[n + 1];
                    m.x264options.aqstrength = aqvalue;
                }

                else if (value == "--aq-mode")
                {
                    if (cli[n + 1] == "0")
                        m.x264options.aqmode = "Disabled";
                    else
                        m.x264options.aqmode = cli[n + 1];
                }

                else if (value == "--no-psy")
                    m.x264options.no_psy = true;

                else if (value == "--psy-rd")
                {
                    string psy = cli[n + 1];
                    string[] psyseparator = new string[] { ":" };
                    string[] psyvalues = psy.Split(psyseparator, StringSplitOptions.None);
                    m.x264options.psyrdo = (decimal)Calculate.ConvertStringToDouble(psyvalues[0]);
                    m.x264options.psytrellis = (decimal)Calculate.ConvertStringToDouble(psyvalues[1]);
                }

                else if (value == "--partitions" || value == "--analyse" || value == "-A")
                    m.x264options.analyse = cli[n + 1];

                else if (value == "--deblock" || value == "--filter" || value == "-f")
                {
                    string filtervalues = cli[n + 1];
                    string[] fseparator = new string[] { ":" };
                    string[] fvalues = filtervalues.Split(fseparator, StringSplitOptions.None);
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

                else if (value == "--qcomp")
                    m.x264options.qcomp = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);

                else if (value == "--vbv-bufsize")
                    m.x264options.vbv_bufsize = Convert.ToInt32(cli[n + 1]);

                else if (value == "--vbv-maxrate")
                    m.x264options.vbv_maxrate = Convert.ToInt32(cli[n + 1]);

                else if (value == "--chroma-qp-offset")
                    m.x264options.qp_offset = cli[n + 1];

                else if (value == "--slow-firstpass")
                    m.x264options.slow_frstpass = true;

                else if (value == "--no-mbtree")
                    m.x264options.no_mbtree = true;

                else if (value == "--rc-lookahead")
                    m.x264options.lookahead = Convert.ToInt32(cli[n + 1]);

                else if (value == "--nal-hrd")
                    m.x264options.nal_hrd = true;

                else if (value == "--min-keyint")
                    m.x264options.gop_min = Convert.ToInt32(cli[n + 1]);

                else if (value == "--keyint")
                    m.x264options.gop_max = Convert.ToInt32(cli[n + 1]);

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

            if (m.x264options.no_chroma)
                line += " --no-chroma-me";

            if (m.x264options.bframes != 3)
                line += " --bframes " + m.x264options.bframes;

            if (m.x264options.direct != "spatial")
                line += " --direct " + m.x264options.direct;

            if (m.x264options.b_adapt != 1)
                line += " --b-adapt " + m.x264options.b_adapt;

            if (m.x264options.bpyramid != 2)
            {
                if (m.x264options.bpyramid == 1)
                    line += " --b-pyramid strict";
                else
                    line += " --b-pyramid none";
            }

            if (m.x264options.weightb == false)
                line += " --no-weightb";

            if (m.x264options.weightp != 2)
                line += " --weightp " + m.x264options.weightp;

            if (m.x264options.trellis != 1)
                line += " --trellis " + m.x264options.trellis;

            if (m.x264options.no_fastpskip)
                line += " --no-fast-pskip";

            if (m.x264options.no_dctdecimate)
                line += " --no-dct-decimate";

            if (m.x264options.custommatrix != null)
                line += " --cqm " + m.x264options.custommatrix;

            if (m.x264options.min_quant != 10)
                line += " --qpmin " + m.x264options.min_quant;

            if (m.x264options.max_quant != 51)
                line += " --qpmax " + m.x264options.max_quant;

            if (m.x264options.step_quant != 4)
                line += " --qpstep " + m.x264options.step_quant;

            if (m.x264options.aud == true)
                line += " --aud";

            if (m.x264options.nal_hrd)
                line += " --nal-hrd vbr";

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
                line += " --qcomp " + Calculate.ConvertDoubleToPointString((double)m.x264options.qcomp, 2);

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

            if (m.x264options.gop_min != 25)
                line += " --min-keyint " + m.x264options.gop_min;

            if (m.x264options.gop_max != 250)
                line += " --keyint " + m.x264options.gop_max;

            //удаляем пустоту в начале
            if (line.StartsWith(" "))
                line = line.Remove(0, 1);          

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

                    check_lossless.IsChecked = false;

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

        private void check_deblocking_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.deblocking = check_deblocking.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
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
                if (me == "Diamond") m.x264options.me = "dia";
                else if (me == "Hexagon") m.x264options.me = "hex";
                else if (me == "Multi Hexagon") m.x264options.me = "umh";
                else if (me == "Exhaustive") m.x264options.me = "esa";
                else if (me == "SATD Exhaustive") m.x264options.me = "tesa";

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

        private void check_chroma_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_chroma = check_chroma.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
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
                if (bmode == "Disabled") m.x264options.direct = "none";
                else if (bmode == "Spatial") m.x264options.direct = "spatial";
                else if (bmode == "Temporal") m.x264options.direct = "temporal";
                else if (bmode == "Auto") m.x264options.direct = "auto";

                if (bmode != "Disabled" &&
                    m.x264options.bframes == 0)
                {
                    combo_bframes.SelectedItem = 1;
                    m.x264options.bframes = 1;
                }

                if (bmode == "Disabled")
                {
                    combo_bframes.SelectedItem = 0;
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
                    combo_bframes.SelectedItem = 1;
                    m.x264options.bframes = 1;
                }

                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_weightedb_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.weightb = check_weightedb.IsChecked.Value;

            if (m.x264options.weightb && m.x264options.bframes == 0)
            {
                combo_bframes.SelectedItem = 1;
                m.x264options.bframes = 1;
            }

            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_weightp_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_weightp_mode.IsDropDownOpen || combo_weightp_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.weightp = combo_weightp_mode.SelectedIndex;
                SetAVCProfile();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_8x8dct_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.adaptivedct = check_8x8dct.IsChecked.Value;
            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_i4x4_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_p4x4_Click(object sender, RoutedEventArgs e)
        {
            if (check_p4x4.IsChecked.Value && !check_p8x8.IsChecked.Value)
                check_p8x8.IsChecked = true;
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
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
            DetectCodecPreset();
        }

        private void check_p8x8_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_b8x8_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.analyse = GetMackroblocks();
            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void combo_trellis_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_trellis.IsDropDownOpen || combo_trellis.IsSelectionBoxHighlighted)
            {
                m.x264options.trellis = combo_trellis.SelectedIndex;
               
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

        private void num_min_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_quant.IsAction)
            {
                m.x264options.min_quant = Convert.ToInt32(num_min_quant.Value);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_max_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_quant.IsAction)
            {
                m.x264options.max_quant = Convert.ToInt32(num_max_quant.Value);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_step_quant_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_step_quant.IsAction)
            {
                m.x264options.step_quant = Convert.ToInt32(num_step_quant.Value);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_mixed_ref_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.mixedrefs = check_mixed_ref.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_cabac_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.cabac = check_cabac.IsChecked.Value;
            SetAVCProfile();
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_fast_pskip_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_fastpskip = check_fast_pskip.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_dct_decimate_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.no_dctdecimate = check_dct_decimate.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
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
                    combo_badapt_mode.SelectedValue = "Disabled";
                    m.x264options.b_adapt = 0;
                    combo_bpyramid_mode.SelectedIndex = 0;
                    m.x264options.bpyramid = 0;
                    check_weightedb.IsChecked = false;
                    m.x264options.weightb = false;
                    combo_weightp_mode.SelectedIndex = 0;
                    m.x264options.weightp = 0;
                    m.x264options.direct = "none";
                    combo_bframe_mode.SelectedItem = "Disabled";
                    m.x264options.adaptivedct = false;
                    check_8x8dct.IsChecked = false;
                    check_i8x8.IsChecked = false;
                    check_i4x4.IsChecked = false;
                    m.x264options.analyse = GetMackroblocks();
                }
                else if (avcprofile == "Main Profile")
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
                    combo_weightp_mode.SelectedIndex = 2;
                    m.x264options.weightp = 2;
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
                else if (avcprofile == "High Profile")
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
                    combo_weightp_mode.SelectedIndex = 2;
                    m.x264options.weightp = 2;
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

#region "Detecting Codec Preset"
            //Medium 
            if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.lookahead == 40 &&
                    m.x264options.me == "hex" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.no_mbtree == false &&
                    m.x264options.reference == 3 &&
                    m.x264options.subme == 7 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 2 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Medium;
            
            //Ultrafast
            else if (m.x264options.adaptivedct == false &&
                    m.x264options.analyse == "none" &&
                    m.x264options.aqmode == "Disabled" &&
                    m.x264options.b_adapt == 0 &&
                    m.x264options.bframes == 0 &&
                    m.x264options.cabac == false &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == false &&
                    m.x264options.direct == "none" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "dia" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == false &&
                    m.x264options.no_mbtree == true &&
                    m.x264options.reference == 1 &&
                    m.x264options.subme == 1 &&
                    m.x264options.trellis == 0 &&
                    m.x264options.weightb == false &&
                    m.x264options.weightp == 0 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Ultrafast;

            //Superfast
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "i8x8,i4x4" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "dia" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == false &&
                    m.x264options.no_mbtree == true &&
                    m.x264options.reference == 1 &&
                    m.x264options.subme == 1 &&
                    m.x264options.trellis == 0 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 0 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Superfast;

            //Veryfast
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "hex" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == false &&
                    m.x264options.no_mbtree == true &&
                    m.x264options.reference == 1 &&
                    m.x264options.subme == 2 &&
                    m.x264options.trellis == 0 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 0 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Veryfast;

            //Faster
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.me == "hex" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == false &&
                    m.x264options.lookahead == 20 &&
                    m.x264options.reference == 2 &&
                    m.x264options.subme == 4 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 1 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Faster;

            //Fast
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 1 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "spatial" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.lookahead == 30 &&
                    m.x264options.me == "hex" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.no_mbtree == false &&
                    m.x264options.reference == 2 &&
                    m.x264options.subme == 6 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 2 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Fast;

            //Slow
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "p8x8,b8x8,i8x8,i4x4" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.lookahead == 50 &&
                    m.x264options.me == "umh" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.no_mbtree == false &&
                    m.x264options.reference == 5 &&
                    m.x264options.subme == 8 &&
                    m.x264options.trellis == 1 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 2 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Slow;

            //Slower
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "all" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 3 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.lookahead == 60 &&
                    m.x264options.me == "umh" &&
                    m.x264options.merange == 16 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.no_mbtree == false &&
                    m.x264options.reference == 8 &&
                    m.x264options.subme == 9 &&
                    m.x264options.trellis == 2 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 2 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Slower;

            //Veryslow
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "all" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 8 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.no_fastpskip == false &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.lookahead == 60 &&
                    m.x264options.me == "umh" &&
                    m.x264options.merange == 24 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.no_mbtree == false &&
                    m.x264options.reference == 16 &&
                    m.x264options.subme == 10 &&
                    m.x264options.trellis == 2 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 2 &&
                    m.x264options.slow_frstpass == false)
                preset = CodecPresets.Veryslow;

            //Placebo
            else if (m.x264options.adaptivedct == true &&
                    m.x264options.analyse == "all" &&
                    m.x264options.aqmode == "1" &&
                    m.x264options.b_adapt == 2 &&
                    m.x264options.bframes == 16 &&
                    m.x264options.bpyramid == 2 &&
                    m.x264options.cabac == true &&
                    m.x264options.no_chroma == false &&
                    m.x264options.no_dctdecimate == false &&
                    m.x264options.deblocking == true &&
                    m.x264options.direct == "auto" &&
                    m.x264options.no_fastpskip == true &&
                    m.x264options.level == Format.GetValidAVCLevel(m) &&
                    m.x264options.lookahead == 60 &&
                    m.x264options.me == "tesa" &&
                    m.x264options.merange == 24 &&
                    m.x264options.mixedrefs == true &&
                    m.x264options.no_mbtree == false &&
                    m.x264options.reference == 16 &&
                    m.x264options.subme == 10 &&
                    m.x264options.trellis == 2 &&
                    m.x264options.weightb == true &&
                    m.x264options.weightp == 2 &&
                    m.x264options.slow_frstpass == true)
                preset = CodecPresets.Placebo;
#endregion

            combo_codec_preset.SelectedItem = preset.ToString();
            UpdateCLI();
        }

        public void UpdateCLI()
        {
            textbox_cli.Clear();
            foreach (string aa in m.vpasses)
                textbox_cli.Text += aa + "\r\n\r\n";
            good_cli = (ArrayList)m.vpasses.Clone(); //Клонируем CLI, не вызывающую ошибок
        }

        private void combo_codec_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_codec_preset.IsDropDownOpen || combo_codec_preset.IsSelectionBoxHighlighted)
            {
                CodecPresets preset = (CodecPresets)Enum.Parse(typeof(CodecPresets), combo_codec_preset.SelectedItem.ToString());

                //Сбрасываем на дефолтные параметры
                if (preset != CodecPresets.Custom)
                {
                    m.x264options = new x264_arguments().Clone();
                    m.x264options.level = Format.GetValidAVCLevel(m);
                }
                
                if (preset == CodecPresets.Ultrafast)
                {
                    m.x264options.adaptivedct = false;
                    m.x264options.analyse = "none";
                    m.x264options.b_adapt = 0;
                    m.x264options.bframes = 0;
                    m.x264options.bpyramid = 0;
                    m.x264options.cabac = false;
                    m.x264options.deblocking = false;
                    m.x264options.direct = "none";
                    m.x264options.me = "dia";
                    m.x264options.mixedrefs = false;
                    m.x264options.reference = 1;
                    m.x264options.subme = 1;
                    m.x264options.trellis = 0;
                    m.x264options.weightb = false;
                    m.x264options.weightp = 0;
                    m.x264options.aqmode = "Disabled";
                    m.x264options.psyrdo = 0;
                    m.x264options.no_mbtree = true;
                }
                else if (preset == CodecPresets.Superfast)
                {
                    m.x264options.analyse = "i8x8,i4x4";
                    m.x264options.me = "dia";
                    m.x264options.mixedrefs = false;
                    m.x264options.reference = 1;
                    m.x264options.subme = 1;
                    m.x264options.trellis = 0;
                    m.x264options.weightp = 0;
                    m.x264options.no_mbtree = true;
                }
                else if (preset == CodecPresets.Veryfast)
                {
                    m.x264options.no_mbtree = true;
                    m.x264options.mixedrefs = false;
                    m.x264options.reference = 1;
                    m.x264options.subme = 2;
                    m.x264options.trellis = 0;
                    m.x264options.weightp = 0;
                }
                else if (preset == CodecPresets.Faster)
                {
                    m.x264options.mixedrefs = false;
                    m.x264options.reference = 2;
                    m.x264options.subme = 4;
                    m.x264options.weightp = 1;
                    m.x264options.lookahead = 20;
                }
                else if (preset == CodecPresets.Fast)
                {
                    m.x264options.reference = 2;
                    m.x264options.subme = 6;
                    m.x264options.lookahead = 30;
                }
                else if (preset == CodecPresets.Slow)
                {
                    m.x264options.b_adapt = 2;
                    m.x264options.direct = "auto";
                    m.x264options.me = "umh";
                    m.x264options.lookahead = 50;
                    m.x264options.reference = 5;
                    m.x264options.subme = 8;
                }
                else if (preset == CodecPresets.Slower)
                {
                    m.x264options.b_adapt = 2;
                    m.x264options.direct = "auto";
                    m.x264options.me = "umh";
                    m.x264options.analyse = "all";
                    m.x264options.lookahead = 60;
                    m.x264options.reference = 8;
                    m.x264options.subme = 9;
                    m.x264options.trellis = 2;
                }
                else if (preset == CodecPresets.Veryslow)
                {
                    m.x264options.b_adapt = 2;
                    m.x264options.bframes = 8;
                    m.x264options.direct = "auto";
                    m.x264options.me = "umh";
                    m.x264options.merange = 24;
                    m.x264options.analyse = "all";
                    m.x264options.reference = 16;
                    m.x264options.lookahead = 60;
                    m.x264options.subme = 10;
                    m.x264options.trellis = 2;
                }
                else if (preset == CodecPresets.Placebo)
                {
                    m.x264options.b_adapt = 2;
                    m.x264options.bframes = 16;
                    m.x264options.direct = "auto";
                    m.x264options.no_fastpskip = true;
                    m.x264options.me = "tesa";
                    m.x264options.merange = 24;
                    m.x264options.analyse = "all";
                    m.x264options.reference = 16;
                    m.x264options.lookahead = 60;
                    m.x264options.subme = 10;
                    m.x264options.trellis = 2;
                    m.x264options.slow_frstpass = true;
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
            else if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                num_bitrate.Minimum = 0;
                num_bitrate.Maximum = 51;
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
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

        private void combo_threads_count_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_threads_count.IsDropDownOpen || combo_threads_count.IsSelectionBoxHighlighted)
            {
                m.x264options.threads = combo_threads_count.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_badapt_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_badapt_mode.IsDropDownOpen || combo_badapt_mode.IsSelectionBoxHighlighted)
            {
                m.x264options.b_adapt = combo_badapt_mode.SelectedIndex;

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
            m.x264options.slow_frstpass = check_slow_first.IsChecked.Value;

            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_nombtree_Clicked(object sender, RoutedEventArgs e)
        {
            m.x264options.no_mbtree = check_nombtree.IsChecked.Value;
            num_lookahead.IsEnabled = !m.x264options.no_mbtree;

            root_window.UpdateManualProfile();
            DetectCodecPreset();
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
                help.FileName = Calculate.StartupPath + "\\apps\\x264\\x264.exe";
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = " --fullhelp";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                new ShowWindow(root_window, "x264 help", p.StandardOutput.ReadToEnd(), new FontFamily("Lucida Console"));
            }
            catch (Exception) { }
        }

        private void check_nal_hrd_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.nal_hrd = check_nal_hrd.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void check_aud_Click(object sender, RoutedEventArgs e)
        {
            m.x264options.aud = check_aud.IsChecked.Value;
            root_window.UpdateManualProfile();
            DetectCodecPreset();
        }

        private void num_min_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_gop.IsAction)
            {
                m.x264options.gop_min = Convert.ToInt32(num_min_gop.Value);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void num_max_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_gop.IsAction)
            {
                m.x264options.gop_max = Convert.ToInt32(num_max_gop.Value);
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }
    }
}