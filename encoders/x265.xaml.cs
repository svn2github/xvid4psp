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
    public partial class x265
    {
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum Presets { Ultrafast = 0, Superfast, Veryfast, Faster, Fast, Medium, Slow, Slower, Veryslow, Placebo }
        public enum Tunes { None = 0, Grain, PSNR, SSIM, FastDecode }
        public enum Profiles { Auto = 0, Main, Main444, Main_10, Main422_10, Main444_10 }
        private ArrayList good_cli = null;

        public x265(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
        {
            this.InitializeComponent();

            this.m = mass.Clone();
            this.root_window = VideoEncWindow;
            this.p = parent;

            //https://github.com/videolan/x265/blob/master/source/x265cli.h - ключи CLI
            //https://github.com/videolan/x265/blob/master/source/common/param.cpp - парсинг ключей, Presets, Tunes

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

            //HEVC profile
            combo_hevc_profile.Items.Add("Auto");
            combo_hevc_profile.Items.Add("Main");
            combo_hevc_profile.Items.Add("Main 444");
            combo_hevc_profile.Items.Add("Main 10b");
            combo_hevc_profile.Items.Add("Main 422 10b");
            combo_hevc_profile.Items.Add("Main 444 10b");

            //HEVC level
            combo_level.Items.Add("Unrestricted");
            combo_level.Items.Add("1.0");
            combo_level.Items.Add("2.0");
            combo_level.Items.Add("2.1");
            combo_level.Items.Add("3.0");
            combo_level.Items.Add("3.1");
            combo_level.Items.Add("4.0");
            combo_level.Items.Add("4.1");
            combo_level.Items.Add("5.0");
            combo_level.Items.Add("5.1");
            combo_level.Items.Add("5.2");
            combo_level.Items.Add("6.0");
            combo_level.Items.Add("6.1");
            combo_level.Items.Add("6.2");

            //Tune  psnr, ssim, grain, zero-latency, fast-decode
            combo_tune.Items.Add("None");
            combo_tune.Items.Add("Grain");
            combo_tune.Items.Add("PSNR");
            combo_tune.Items.Add("SSIM");
            combo_tune.Items.Add("Fast Decode");

            //Adaptive Quantization
            for (double n = 0.0; n <= 3.1; n += 0.1)
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
            combo_subme.Items.Add("0 - least");
            combo_subme.Items.Add("1");
            combo_subme.Items.Add("2");
            combo_subme.Items.Add("3");
            combo_subme.Items.Add("4");
            combo_subme.Items.Add("5");
            combo_subme.Items.Add("6");
            combo_subme.Items.Add("7");
            combo_subme.Items.Add("8");
            combo_subme.Items.Add("9");
            combo_subme.Items.Add("10");
            combo_subme.Items.Add("11 - most");

            //прописываем me алгоритм
            combo_me.Items.Add("Diamond");
            combo_me.Items.Add("Hexagon");
            combo_me.Items.Add("Multi Hexagon");
            combo_me.Items.Add("Star");
            combo_me.Items.Add("Full");

            //прописываем me range
            for (int n = 4; n <= 512; n++)
                combo_merange.Items.Add(n);

            for (int i = 1; i < 6; i++)
                combo_max_merge.Items.Add(i);

            combo_rd.Items.Add("0 - least");
            for (int i = 1; i < 6; i++)
                combo_rd.Items.Add(i);
            combo_rd.Items.Add("6 - full RDO");

            combo_ctu.Items.Add(16);
            combo_ctu.Items.Add(32);
            combo_ctu.Items.Add(64);

            //B фреймы
            for (int n = 0; n <= 16; n++)
                combo_bframes.Items.Add(n);

            combo_bpyramid_mode.Items.Add("Disabled");
            combo_bpyramid_mode.Items.Add("Enabled");

            //refernce frames
            for (int n = 1; n <= 16; n++)
                combo_ref.Items.Add(n);

            combo_open_gop.Items.Add("No");
            combo_open_gop.Items.Add("Yes");

            //Кол-во потоков для x265-го
            combo_threads_count.Items.Add("Auto");
            combo_threads_frames.Items.Add("Auto");
            for (int n = 1; n <= 32; n++)
            {
                combo_threads_count.Items.Add(n);
                combo_threads_frames.Items.Add(n);
            }

            //-b-adapt
            combo_badapt_mode.Items.Add("None");
            combo_badapt_mode.Items.Add("Fast");
            combo_badapt_mode.Items.Add("Full");

            combo_range_out.Items.Add("Auto");
            combo_range_out.Items.Add("Limited");
            combo_range_out.Items.Add("Full");

            combo_colorprim.Items.Add("Undefined");
            combo_colorprim.Items.Add("bt709");
            combo_colorprim.Items.Add("bt470m");
            combo_colorprim.Items.Add("bt470bg");
            combo_colorprim.Items.Add("smpte170m");
            combo_colorprim.Items.Add("smpte240m");
            combo_colorprim.Items.Add("film");
            combo_colorprim.Items.Add("bt2020");

            combo_transfer.Items.Add("Undefined");
            combo_transfer.Items.Add("bt709");
            combo_transfer.Items.Add("bt470m");
            combo_transfer.Items.Add("bt470bg");
            combo_transfer.Items.Add("smpte170m");
            combo_transfer.Items.Add("smpte240m");
            combo_transfer.Items.Add("linear");
            combo_transfer.Items.Add("log100");
            combo_transfer.Items.Add("log316");
            combo_transfer.Items.Add("iec61966-2-4");
            combo_transfer.Items.Add("bt1361e");
            combo_transfer.Items.Add("iec61966-2-1");
            combo_transfer.Items.Add("bt2020-10");
            combo_transfer.Items.Add("bt2020-12");

            combo_colormatrix.Items.Add("Undefined"); 
            combo_colormatrix.Items.Add("bt709");
            combo_colormatrix.Items.Add("fcc");
            combo_colormatrix.Items.Add("bt470bg");
            combo_colormatrix.Items.Add("smpte170m");
            combo_colormatrix.Items.Add("smpte240m");
            combo_colormatrix.Items.Add("GBR");
            combo_colormatrix.Items.Add("YCgCo");
            combo_colormatrix.Items.Add("bt2020nc");
            combo_colormatrix.Items.Add("bt2020c");

            combo_hash.Items.Add("None");
            combo_hash.Items.Add("MD5");
            combo_hash.Items.Add("CRC");
            combo_hash.Items.Add("Checksum");

            text_mode.Content = Languages.Translate("Encoding mode") + ":";
            Apply_CLI.Content = Languages.Translate("Apply");
            Reset_CLI.Content = Languages.Translate("Reset");
            x265_help.Content = Languages.Translate("Help");
            Reset_CLI.ToolTip = "Reset to last good CLI";
            x265_help.ToolTip = "Show x265.exe --fullhelp screen";

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

            combo_hevc_profile.SelectedIndex = (int)m.x265options.profile;

            //HEVC level
            if (m.x265options.level == "unrestricted") combo_level.SelectedIndex = 0;
            else combo_level.SelectedItem = m.x265options.level;

            //Встроенный в x265 пресет
            text_preset_name.Content = m.x265options.preset.ToString();
            slider_preset.Value = (int)m.x265options.preset;

            //Tune
            combo_tune.SelectedIndex = (int)m.x265options.tune;

            check_lossless.IsChecked = m.x265options.lossless;
            check_slow_first.IsChecked = m.x265options.slow_firstpass;

            num_qcomp.Value = m.x265options.qcomp;
            num_ratio_ip.Value = m.x265options.ratio_ip;
            num_ratio_pb.Value = m.x265options.ratio_pb;
            num_chroma_qpb.Value = m.x265options.chroma_offset_cb;
            num_chroma_qpr.Value = m.x265options.chroma_offset_cr;
            num_vbv_max.Value = m.x265options.vbv_maxrate;
            num_vbv_buf.Value = m.x265options.vbv_bufsize;
            num_vbv_init.Value = m.x265options.vbv_init;
            combo_adapt_quant_mode.SelectedIndex = m.x265options.aqmode;
            combo_adapt_quant.SelectedItem = m.x265options.aqstrength;
            check_cutree.IsChecked = m.x265options.cutree;

            num_psyrdo.Value = m.x265options.psyrdo;
            num_psyrdoq.Value = m.x265options.psyrdoq;

            //прогружаем деблокинг
            combo_dstrength.SelectedItem = m.x265options.deblockTC;
            combo_dthreshold.SelectedItem = m.x265options.deblockBeta;
            check_deblocking.IsChecked = m.x265options.deblocking;

            check_sao.IsChecked = m.x265options.sao;

            //Прописываем subme
            combo_subme.SelectedIndex = m.x265options.subme;

            //прописываем me алгоритм
            if (m.x265options.me == "dia" || m.x265options.me == "0") combo_me.SelectedIndex = 0;
            else if (m.x265options.me == "hex" || m.x265options.me == "1") combo_me.SelectedIndex = 1;
            else if (m.x265options.me == "umh" || m.x265options.me == "2") combo_me.SelectedIndex = 2;
            else if (m.x265options.me == "star" || m.x265options.me == "3") combo_me.SelectedIndex = 3;
            else if (m.x265options.me == "full" || m.x265options.me == "4") combo_me.SelectedIndex = 4;

            //прописываем me range
            combo_merange.SelectedItem = m.x265options.merange;

            combo_max_merge.SelectedIndex = m.x265options.max_merge - 1;
            combo_rd.SelectedIndex = m.x265options.rd;
            combo_ctu.SelectedItem = m.x265options.ctu;
            check_cu_lossless.IsChecked = m.x265options.cu_lossless;
            check_early_skip.IsChecked = m.x265options.early_skip;
            check_rect.IsChecked = m.x265options.rect;
            check_amp.IsChecked = m.x265options.amp;
            check_constr_intra.IsChecked = m.x265options.constr_intra;
            check_b_intra.IsChecked = m.x265options.b_intra;

            //B фреймы
            combo_bframes.SelectedIndex = m.x265options.bframes;

            combo_bpyramid_mode.SelectedIndex = (m.x265options.bpyramid) ? 1 : 0;

            if (!m.x265options.open_gop) combo_open_gop.SelectedIndex = 0;
            else combo_open_gop.SelectedIndex = 1;

            //weightp
            check_weightedp.IsChecked = m.x265options.weightp;

            //weightb
            check_weightedb.IsChecked = m.x265options.weightb;

            //refernce frames
            combo_ref.SelectedItem = m.x265options.reference;

            num_min_gop.Value = m.x265options.gop_min;
            num_max_gop.Value = m.x265options.gop_max;

            combo_badapt_mode.SelectedIndex = m.x265options.b_adapt;
            
            num_lookahead.Value = m.x265options.lookahead;

            if (m.x265options.range_out == "auto") combo_range_out.SelectedIndex = 0;
            else if (m.x265options.range_out == "limited") combo_range_out.SelectedIndex = 1;
            else if (m.x265options.range_out == "full") combo_range_out.SelectedIndex = 2;

            combo_colorprim.SelectedItem = m.x265options.colorprim;
            combo_transfer.SelectedItem = m.x265options.transfer;
            combo_colormatrix.SelectedItem = m.x265options.colormatrix;
            combo_hash.SelectedIndex = m.x265options.hash;

            check_info.IsChecked = m.x265options.info;
            check_aud.IsChecked = m.x265options.aud;
            check_hrd.IsChecked = m.x265options.hrd;
            check_headers.IsChecked = m.x265options.headers_repeat;

            check_wpp.IsChecked = m.x265options.wpp;
            check_pmode.IsChecked = m.x265options.pmode;
            check_pme.IsChecked = m.x265options.pme;

            //Кол-во потоков для x265-го
            combo_threads_count.SelectedIndex = m.x265options.threads;
            combo_threads_frames.SelectedIndex = m.x265options.threads_frames;

            //Включаем-выключаем элементы на основе содержимого extra_cli
            combo_hevc_profile.IsEnabled = !m.x265options.extra_cli.Contains("--profile ");
            combo_level.IsEnabled = !m.x265options.extra_cli.Contains("--level-idc ") && !m.x265options.extra_cli.Contains("--level ");
            check_high_tier.IsEnabled = !m.x265options.extra_cli.Contains("--high-tier") && !m.x265options.extra_cli.Contains("--no-high-tier");
            combo_tune.IsEnabled = !m.x265options.extra_cli.Contains("--tune ");
            check_lossless.IsEnabled = !m.x265options.extra_cli.Contains("--lossless") && !m.x265options.extra_cli.Contains("--no-lossless");
            check_slow_first.IsEnabled = !m.x265options.extra_cli.Contains("--slow-firstpass") && !m.x265options.extra_cli.Contains("--no-slow-firstpass");

            combo_subme.IsEnabled = !m.x265options.extra_cli.Contains("--subme ");
            combo_me.IsEnabled = !m.x265options.extra_cli.Contains("--me ");
            combo_merange.IsEnabled = !m.x265options.extra_cli.Contains("--merange ");
            combo_max_merge.IsEnabled = !m.x265options.extra_cli.Contains("--max-merge ");
            combo_rd.IsEnabled = !m.x265options.extra_cli.Contains("--rd ");
            combo_ctu.IsEnabled = !m.x265options.extra_cli.Contains("--ctu ");
            check_weightedp.IsEnabled = !m.x265options.extra_cli.Contains("--weightp") && !m.x265options.extra_cli.Contains("--no-weightp");
            check_weightedb.IsEnabled = !m.x265options.extra_cli.Contains("--weightb") && !m.x265options.extra_cli.Contains("--no-weightb");
            check_cu_lossless.IsEnabled = !m.x265options.extra_cli.Contains("--cu-lossless") && !m.x265options.extra_cli.Contains("--no-cu-lossless");
            check_early_skip.IsEnabled = !m.x265options.extra_cli.Contains("--early-skip") && !m.x265options.extra_cli.Contains("--no-early-skip");
            check_rect.IsEnabled = !m.x265options.extra_cli.Contains("--rect") && !m.x265options.extra_cli.Contains("--no-rect");
            check_amp.IsEnabled = !m.x265options.extra_cli.Contains("--amp") && !m.x265options.extra_cli.Contains("--no-amp");
            check_constr_intra.IsEnabled = !m.x265options.extra_cli.Contains("--constrained-intra") && !m.x265options.extra_cli.Contains("--no-constrained-intra");
            check_b_intra.IsEnabled = !m.x265options.extra_cli.Contains("--b-intra") && !m.x265options.extra_cli.Contains("--no-b-intra");

            combo_bframes.IsEnabled = !m.x265options.extra_cli.Contains("--bframes ");
            combo_badapt_mode.IsEnabled = !m.x265options.extra_cli.Contains("--b-adapt ") && !m.x265options.extra_cli.Contains("--no-b-adapt");
            combo_bpyramid_mode.IsEnabled = !m.x265options.extra_cli.Contains("--b-pyramid") && !m.x265options.extra_cli.Contains("--no-b-pyramid");
            combo_ref.IsEnabled = !m.x265options.extra_cli.Contains("--ref ");
            combo_open_gop.IsEnabled = !m.x265options.extra_cli.Contains("--open-gop") && !m.x265options.extra_cli.Contains("--no-open-gop");
            num_min_gop.IsEnabled = !m.x265options.extra_cli.Contains("--min-keyint ");
            num_max_gop.IsEnabled = !m.x265options.extra_cli.Contains("--keyint ");
            num_lookahead.IsEnabled = !m.x265options.extra_cli.Contains("--rc-lookahead ");
            check_deblocking.IsEnabled = !m.x265options.extra_cli.Contains("--deblock ") && !m.x265options.extra_cli.Contains("--no-deblock");
            check_sao.IsEnabled = !m.x265options.extra_cli.Contains("--sao") && !m.x265options.extra_cli.Contains("--no-sao");

            num_qcomp.IsEnabled = !m.x265options.extra_cli.Contains("--qcomp ");
            num_ratio_ip.IsEnabled = !m.x265options.extra_cli.Contains("--ipratio ");
            num_ratio_pb.IsEnabled = !m.x265options.extra_cli.Contains("--pbratio ");
            num_chroma_qpb.IsEnabled = !m.x265options.extra_cli.Contains("--cbqpoffs ");
            num_chroma_qpr.IsEnabled = !m.x265options.extra_cli.Contains("--crqpoffs ");
            num_vbv_max.IsEnabled = !m.x265options.extra_cli.Contains("--vbv-maxrate ");
            num_vbv_buf.IsEnabled = !m.x265options.extra_cli.Contains("--vbv-bufsize ");
            num_vbv_init.IsEnabled = !m.x265options.extra_cli.Contains("--vbv-init ");
            combo_adapt_quant_mode.IsEnabled = !m.x265options.extra_cli.Contains("--aq-mode ");
            combo_adapt_quant.IsEnabled = !m.x265options.extra_cli.Contains("--aq-strength ");
            check_cutree.IsEnabled = !m.x265options.extra_cli.Contains("--cutree") && !m.x265options.extra_cli.Contains("--no-cutree");

            num_psyrdo.IsEnabled = !m.x265options.extra_cli.Contains("--psy-rd ");
            num_psyrdoq.IsEnabled = !m.x265options.extra_cli.Contains("--psy-rdoq ");
            combo_range_out.IsEnabled = !m.x265options.extra_cli.Contains("--range ");
            combo_colorprim.IsEnabled = !m.x265options.extra_cli.Contains("--colorprim ");
            combo_transfer.IsEnabled = !m.x265options.extra_cli.Contains("--transfer ");
            combo_colormatrix.IsEnabled = !m.x265options.extra_cli.Contains("--colormatrix ");

            combo_hash.IsEnabled = !m.x265options.extra_cli.Contains("--hash ");
            check_info.IsEnabled = !m.x265options.extra_cli.Contains("--info") && !m.x265options.extra_cli.Contains("--no-info");
            check_aud.IsEnabled = !m.x265options.extra_cli.Contains("--aud") && !m.x265options.extra_cli.Contains("--no-aud");
            check_hrd.IsEnabled = !m.x265options.extra_cli.Contains("--hrd") && !m.x265options.extra_cli.Contains("--no-hrd");
            check_headers.IsEnabled = !m.x265options.extra_cli.Contains("--repeat-headers") && !m.x265options.extra_cli.Contains("--repeat-headers");
            check_wpp.IsEnabled = !m.x265options.extra_cli.Contains("--wpp") && !m.x265options.extra_cli.Contains("--no-wpp");
            check_pmode.IsEnabled = !m.x265options.extra_cli.Contains("--pmode") && !m.x265options.extra_cli.Contains("--no-pmode");
            check_pme.IsEnabled = !m.x265options.extra_cli.Contains("--pme") && !m.x265options.extra_cli.Contains("--no-pme");
            combo_threads_count.IsEnabled = !m.x265options.extra_cli.Contains("--threads ");
            combo_threads_frames.IsEnabled = !m.x265options.extra_cli.Contains("--frame-threads ");

            SetToolTips();
            UpdateCLI();
        }

        private void SetToolTips()
        {
            //Определяем дефолты (с учетом --preset и --tune)
            //x265_arguments def = new x265_arguments(m.x265options.preset, m.x265options.tune, m.x265options.profile);

            //Определяем дефолты без учета --preset и --tune, т.к. это именно дефолты самого энкодера
            x265_arguments def = new x265_arguments(Presets.Medium, Tunes.None, m.x265options.profile);

            CultureInfo cult_info = new CultureInfo("en-US");
            string _en = "checked", _dis = "unchecked";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.ToolTip = "Set bitrate (Default: Auto)";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
                num_bitrate.ToolTip = "Set target quantizer (" + ((m.x265options.profile.ToString().EndsWith("10")) ? "-12" : "0") + " - 51). Lower - better quality but bigger filesize.\r\n(Default: 28)";
            else
                num_bitrate.ToolTip = "Set target quality (" + ((m.x265options.profile.ToString().EndsWith("10")) ? "-12" : "0") + " - 51). Lower - better quality but bigger filesize.\r\n(Default: 28)";

            combo_mode.ToolTip = "Encoding mode";
            combo_hevc_profile.ToolTip = "Limit HEVC profile (--profile, default: " + def.profile.ToString() + ")\r\n" +
                "Auto - don't set the --profile key\r\nMain, Main 444 - 410, 444 (8 bit depth)\r\nMain 10b, Main 422 10b, Main 444 10b - 410, 422, 444 (10 bit depth)";
            combo_level.ToolTip = "Force a minumum required decoder level (--level-idc, default: Unrestricted)";
            check_high_tier.ToolTip = "If a decoder level is specified, this modifier selects High tier of that level (--[no-]high-tier, default: " + ((def.high_tier) ? _en : _dis) + ")";
            combo_tune.ToolTip = "Tune the settings for a particular type of source or situation (--tune, default: " + def.tune.ToString() + ")";
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

            check_lossless.ToolTip = "Enable lossless: bypass transform, quant and loop filters globally (--[no-]lossless, default: " + ((def.lossless) ? _en : _dis) + ")";
            check_slow_first.ToolTip = "Enable a slow first pass in a multipass rate control mode (--[no-]slow-firstpass, default: " + ((def.slow_firstpass) ? _en : _dis) + ")";
            combo_subme.ToolTip = "Amount of subpel refinement to perform (--subme, default " + def.subme + ")";
            combo_me.ToolTip = "Motion search method (--me, default: --me " + def.me + ")\r\n" +
                "Diamond Search, fast (--me dia)\r\nHexagonal Search (--me hex)\r\nUneven Multi-Hexagon Search (--me umh)\r\n" +
                "Star Search (--me star)\r\nFull Search, slow (--me full)";
            combo_merange.ToolTip = "Motion search range (--merange, default: " + def.merange + ")";
            combo_max_merge.ToolTip = "Maximum number of merge candidates (--max-merge, default: " + def.max_merge + ")";
            combo_rd.ToolTip = "Level of RD in mode decision (--rd, default: " + def.rd + ")";
            combo_ctu.ToolTip = "Maximum CU size, WxH (--ctu, default: " + def.ctu + ")";
            check_weightedp.ToolTip = "Enable weighted prediction in P slices (--[no-]weightp, default: " + ((def.weightp) ? _en : _dis) + ")";
            check_weightedb.ToolTip = "Enable weighted prediction in B slices (--[no-]weightb, default: " + ((def.weightb) ? _en : _dis) + ")";
            check_cu_lossless.ToolTip = "Consider lossless mode in CU RDO decisions (--[no-]cu-lossless, default: " + ((def.cu_lossless) ? _en : _dis) + ")";
            check_early_skip.ToolTip = "Enable early SKIP detection (--[no-]early-skip, default: " + ((def.early_skip) ? _en : _dis) + ")";
            check_rect.ToolTip = "Enable rectangular motion partitions Nx2N and 2NxN (--[no-]rect, default: " + ((def.rect) ? _en : _dis) + ")";
            check_amp.ToolTip = "Enable asymmetric motion partitions, requires rectangular MP (--[no-]amp, default: " + ((def.amp) ? _en : _dis) + ")";
            check_constr_intra.ToolTip = "Constrained intra prediction; use only intra coded reference pixels (--[no-]constrained-intra, default: " + ((def.constr_intra) ? _en : _dis) + ")";
            check_b_intra.ToolTip = "Enable intra in B frames (--[no-]b-intra, default: " + ((def.b_intra) ? _en : _dis) + ")";

            combo_bframes.ToolTip = "Maximum number of consecutive b-frames; now it only enables B GOP structure (--bframes, default: " + def.bframes + ")";
            combo_badapt_mode.ToolTip = "Adaptive B frame scheduling (--b-adapt, default: " + (def.b_adapt == 0 ? "Disabled)" : def.b_adapt == 1 ? "Fast)" : "Full)");
            combo_bpyramid_mode.ToolTip = "Use B-frames as references (--[no-]b-pyramid, default: " + ((def.bpyramid)? "Enabled)" : "Disabled)");
            combo_ref.ToolTip = "Max. number of L0 references to be allowed (--ref, default: " + def.reference + ")";
            combo_open_gop.ToolTip = "Enable open-GOP, allows I slices to be non-IDR (--[no-]open-gop, default: " + ((def.open_gop) ? "Yes)" : "No)");
            num_min_gop.ToolTip = "Scenecuts closer together than this are coded as I, not IDR (--min-keyint, default: " + def.gop_min + ")\r\n0 - Auto";
            num_max_gop.ToolTip = "Max IDR period in frames (--keyint, default: " + def.gop_max + ")";
            num_lookahead.ToolTip = "Number of frames for frame-type lookahead; determines encoder latency (--rc-lookahead, default: " + def.lookahead + ")";
            check_deblocking.ToolTip = "Enable deblocking loop filter (--[no-]deblock, default: " + ((def.deblocking) ? _en : _dis) + ")";
            combo_dstrength.ToolTip = "Deblocking filter tC offset (--deblock, default: " + def.deblockTC + ":x)";
            combo_dthreshold.ToolTip = "Deblocking filter Beta offset (--deblock, default: x:" + def.deblockBeta + ")";
            check_sao.ToolTip = "Enable Sample Adaptive Offset (--[no-]sao, default: " + ((def.sao) ? _en : _dis) + ")";
            num_qcomp.ToolTip = "Weight given to predicted complexity (--qcomp, default: " + def.qcomp.ToString(cult_info) + ")";
            num_ratio_ip.ToolTip = "QP factor between I and P (--ipratio, default: " + def.ratio_ip.ToString(cult_info) + ")";
            num_ratio_pb.ToolTip = "QP factor between P and B (--pbratio, default: " + def.ratio_pb.ToString(cult_info) + ")";
            num_chroma_qpb.ToolTip = "Chroma Cb QP offset (--cbqpoffs, , default: " + def.chroma_offset_cb + ")";
            num_chroma_qpr.ToolTip = "Chroma Cr QP offset (--crqpoffs, , default: " + def.chroma_offset_cr + ")";
            num_vbv_max.ToolTip = "Max local bitrate, kbit/s (--vbv-maxrate, default: " + def.vbv_maxrate + ")";
            num_vbv_buf.ToolTip = "Set size of the VBV buffer, kbit (--vbv-bufsize, default: " + def.vbv_bufsize + ")";
            num_vbv_init.ToolTip = "Initial VBV buffer occupancy (--vbv-init, default: " + def.vbv_init.ToString(cult_info) + ")";
            combo_adapt_quant_mode.ToolTip = "Mode for Adaptive Quantization (--aq-mode, default: " + ((def.aqmode == 0) ? "None" : def.aqmode == 1 ? "VAQ" : "A-VAQ") + ")\r\n" +
                        "None - disabled, 0\r\nVAQ - variance AQ (uniform), 1\r\nA-VAQ - auto-variance AQ, 2";
            combo_adapt_quant.ToolTip = "AQ Strength (--ag-strength, default: " + def.aqstrength + ")\r\n" +
                        "Reduces blocking and blurring in flat and textured areas: 0.5 - weak AQ, 1.5 - strong AQ";
            check_cutree.ToolTip = "Enable CUTtree for Adaptive Quantization (--[no-]cutree, default: " + ((def.cutree) ? _en : _dis) + ")";
            num_psyrdo.ToolTip = "Strength of psycho-visual rate distortion optimization (--psy-rd, default: " + def.psyrdo.ToString(cult_info) + ")";
            num_psyrdoq.ToolTip = "Strength of psycho-visual optimization in quantization (--psy-rdoq, default: " + def.psyrdoq.ToString(cult_info) + ")";
            combo_range_out.ToolTip = "Specify black level and range of luma and chroma signals (--range, default: Auto)";
            combo_colorprim.ToolTip = "Specify color primaries (--colorprim, default: " + def.colorprim + ")";
            combo_transfer.ToolTip = "Specify transfer characteristics (--transfer, default: " + def.transfer + ")";
            combo_colormatrix.ToolTip = "Specify color matrix setting (--colormatrix, default: " + def.colormatrix + ")";
            combo_hash.ToolTip = "Decoded Picture Hash SEI (--hash, default: " + ((def.hash == 0) ? "None)" : (def.hash == 1) ? "MD5)" : (def.hash == 2) ? "CRC)" : "Checksum)");
            check_info.ToolTip = "Emit SEI identifying encoder and parameters (--[no-]info, default: " + ((def.info) ? _en : _dis) + ")";
            check_aud.ToolTip = "Emit Access Unit Delimiters at the start of each access unit (--[no-]aud, default: " + ((def.aud) ? _en : _dis) + ")";
            check_hrd.ToolTip = "Enable HRD parameters signaling (--[no-]hrd, default: " + ((def.hrd) ? _en : _dis) + ")";
            check_headers.ToolTip = "Emit SPS and PPS headers at each keyframe (--[no-]repeat-headers, default: " + ((def.headers_repeat) ? _en : _dis) + ")";
            check_wpp.ToolTip = "Enable Wavefront Parallel Processing (--[no-]wpp, default: " + ((def.wpp) ? _en : _dis) + ")";
            check_pmode.ToolTip = "Enable parallel mode analysis (--[no-]pmode, default: " + ((def.pmode) ? _en : _dis) + ")";
            check_pme.ToolTip = "Enable parallel motion estimation (--[no-]pme, default: " + ((def.pme) ? _en : _dis) + ")";
            combo_threads_count.ToolTip = "Number of threads for thread pool (--threads, default: Auto)";
            combo_threads_frames.ToolTip = "Number of concurrently encoded frames (--frame-threads, default: Auto)";
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
            string profile_name = Calculate.GetRegexValue(@"\-\-profile\s+(\w+-?\w*)", line);
            if (!string.IsNullOrEmpty(profile_name))
            {
                //Определяем profile по его названию
                if (profile_name.Equals("main", StringComparison.InvariantCultureIgnoreCase)) profile = Profiles.Main;
                else if (profile_name.Equals("main444-8", StringComparison.InvariantCultureIgnoreCase)) profile = Profiles.Main444;
                else if (profile_name.Equals("main10", StringComparison.InvariantCultureIgnoreCase)) profile = Profiles.Main_10;
                else if (profile_name.Equals("main422-10", StringComparison.InvariantCultureIgnoreCase)) profile = Profiles.Main422_10;
                else if (profile_name.Equals("main444-10", StringComparison.InvariantCultureIgnoreCase)) profile = Profiles.Main444_10;
            }

            //Создаём свежий массив параметров x265 (изменяя дефолты с учетом --preset и --tune)
            m.x265options = new x265_arguments(preset, tune, profile);
            m.x265options.profile = profile;
            m.x265options.preset = preset;
            m.x265options.tune = tune;

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
                    else if (value == "--bitrate")
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
                    else if (value == "--bitrate")
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
                    else if (value == "--bitrate")
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

                if (value == "--level-idc" || value == "--level")
                    m.x265options.level = cli[++n];

                else if (value == "--high-tier")
                    m.x265options.high_tier = true;
                else if (value == "--no-high-tier")
                    m.x265options.high_tier = false;

                else if (value == "--lossless")
                    m.x265options.lossless = true;
                else if (value == "--no-lossless")
                    m.x265options.lossless = false;

                else if (value == "--slow-firstpass")
                    m.x265options.slow_firstpass = true;
                else if (value == "--no-slow-firstpass")
                    m.x265options.slow_firstpass = false;

                else if (value == "--ref")
                    m.x265options.reference = Convert.ToInt32(cli[++n]);

                else if (value == "--aq-mode")
                    m.x265options.aqmode = Convert.ToInt32(cli[++n]);

                else if (value == "--aq-strength")
                    m.x265options.aqstrength = cli[++n];

                else if (value == "--cutree")
                    m.x265options.cutree = true;
                else if (value == "--no-cutree")
                    m.x265options.cutree = false;

                else if (value == "--psy-rd")
                    m.x265options.psyrdo = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--psy-rdoq")
                    m.x265options.psyrdoq = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--deblock")
                {
                    m.x265options.deblocking = true;
                    if (n + 1 < cli.Length && (cli[n + 1].Contains(":") || cli[n + 1].Contains(",")))
                    {
                        string[] dvalues = cli[n].Split(new string[] { ":" }, StringSplitOptions.None);
                        if (dvalues.Length < 2) dvalues = cli[++n].Split(new string[] { "," }, StringSplitOptions.None);
                        if (dvalues.Length > 1)
                        {
                            m.x265options.deblockTC = Convert.ToInt32(dvalues[0]);
                            m.x265options.deblockBeta = Convert.ToInt32(dvalues[1]);
                        }
                    }
                }
                else if (value == "--no-deblock")
                {
                    m.x265options.deblocking = false;
                }

                else if (value == "--sao")
                    m.x265options.sao = true;
                else if (value == "--no-sao")
                    m.x265options.sao = false;

                else if (value == "--subme" || value == "-m")
                    m.x265options.subme = Convert.ToInt32(cli[++n]);

                else if (value == "--me")
                    m.x265options.me = cli[++n];

                else if (value == "--merange")
                    m.x265options.merange = Convert.ToInt32(cli[++n]);

                else if (value == "--max-merge")
                    m.x265options.max_merge = Convert.ToInt32(cli[++n]);

                else if (value == "--rd")
                    m.x265options.rd = Convert.ToInt32(cli[++n]);

                else if (value == "--ctu" || value == "-s")
                    m.x265options.ctu = Convert.ToInt32(cli[++n]);

                else if (value == "--cu-lossless")
                    m.x265options.cu_lossless = true;
                else if (value == "--no-cu-lossless")
                    m.x265options.cu_lossless = false;

                else if (value == "--early-skip")
                    m.x265options.early_skip = true;
                else if (value == "--no-early-skip")
                    m.x265options.early_skip = false;

                else if (value == "--rect")
                    m.x265options.rect = true;
                else if (value == "--no-rect")
                    m.x265options.rect = false;

                else if (value == "--amp")
                    m.x265options.amp = true;
                else if (value == "--no-amp")
                    m.x265options.amp = false;

                else if (value == "--constrained-intra")
                    m.x265options.constr_intra = true;
                else if (value == "--no-constrained-intra")
                    m.x265options.constr_intra = false;

                else if (value == "--b-intra")
                    m.x265options.b_intra = true;
                else if (value == "--no-b-intra")
                    m.x265options.b_intra = false;

                else if (value == "--bframes" || value == "-b")
                    m.x265options.bframes = Convert.ToInt32(cli[++n]);

                else if (value == "--b-adapt")
                    m.x265options.b_adapt = Convert.ToInt32(cli[++n]);
                else if (value == "--no-b-adapt")
                    m.x265options.b_adapt = 0;

                else if (value == "--b-pyramid")
                    m.x265options.bpyramid = true;
                else if (value == "--no-b-pyramid")
                    m.x265options.bpyramid = false;

                else if (value == "--weightb")
                    m.x265options.weightb = true;
                else if (value == "--no-weightb")
                    m.x265options.weightb = false;

                else if (value == "--weightp" || value == "-w")
                    m.x265options.weightp = true;
                else if (value == "--no-weightp")
                    m.x265options.weightp = false;

                else if (value == "--threads")
                    m.x265options.threads = Convert.ToInt32(cli[++n]);

                else if (value == "--frame-threads" || value == "-F")
                    m.x265options.threads_frames = Convert.ToInt32(cli[++n]);

                else if (value == "--qcomp")
                    m.x265options.qcomp = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--ipratio")
                    m.x265options.ratio_ip = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--pbratio")
                    m.x265options.ratio_pb = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--cbqpoffs")
                    m.x265options.chroma_offset_cb = Convert.ToInt32(cli[++n]);

                else if (value == "--crqpoffs")
                    m.x265options.chroma_offset_cr = Convert.ToInt32(cli[++n]);

                else if (value == "--vbv-maxrate")
                    m.x265options.vbv_maxrate = Convert.ToInt32(cli[++n]);

                else if (value == "--vbv-bufsize")
                    m.x265options.vbv_bufsize = Convert.ToInt32(cli[++n]);

                else if (value == "--vbv-init")
                    m.x265options.vbv_init = (decimal)Calculate.ConvertStringToDouble(cli[++n]);

                else if (value == "--rc-lookahead")
                    m.x265options.lookahead = Convert.ToInt32(cli[++n]);

                else if (value == "--min-keyint" || value == "-i")
                    m.x265options.gop_min = Convert.ToInt32(cli[++n]);

                else if (value == "--keyint" || value == "-I")
                    m.x265options.gop_max = Convert.ToInt32(cli[++n]);

                else if (value == "--open-gop")
                    m.x265options.open_gop = true;
                else if (value == "--no-open-gop")
                    m.x265options.open_gop = false;

                else if (value == "--range")
                    m.x265options.range_out = cli[++n];

                else if (value == "--colorprim")
                {
                    string _value = cli[++n].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x265options.colorprim = "Undefined";
                    else
                        m.x265options.colorprim = _value;
                }

                else if (value == "--transfer")
                {
                    string _value = cli[++n].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x265options.transfer = "Undefined";
                    else
                        m.x265options.transfer = _value;
                }

                else if (value == "--colormatrix")
                {
                    string _value = cli[++n].Trim(new char[] { '"' });
                    if (_value == "undef")
                        m.x265options.colormatrix = "Undefined";
                    else
                        m.x265options.colormatrix = _value;
                }

                else if (value == "--hash")
                    m.x265options.hash = Convert.ToInt32(cli[++n]);

                else if (value == "--info")
                    m.x265options.info = true;
                else if (value == "--no-info")
                    m.x265options.info = false;

                else if (value == "--aud")
                    m.x265options.aud = true;
                else if (value == "--no-aud")
                    m.x265options.aud = false;

                else if (value == "--hrd")
                    m.x265options.hrd = true;
                else if (value == "--no-hrd")
                    m.x265options.hrd = false;

                else if (value == "--repeat-headers")
                    m.x265options.headers_repeat = true;
                else if (value == "--no-repeat-headers")
                    m.x265options.headers_repeat = false;

                else if (value == "--wpp")
                    m.x265options.wpp = true;
                else if (value == "--no-wpp")
                    m.x265options.wpp = false;

                else if (value == "--pmode")
                    m.x265options.pmode = true;
                else if (value == "--no-pmode")
                    m.x265options.pmode = false;

                else if (value == "--pme")
                    m.x265options.pme = true;
                else if (value == "--no-pme")
                    m.x265options.pme = false;

                else if (value == "--extra:")
                {
                    for (int i = n + 1; i < cli.Length; i++)
                        m.x265options.extra_cli += cli[i] + " ";

                    m.x265options.extra_cli = m.x265options.extra_cli.Trim();
                }
            }

            //Сброс на дефолт, если в CLI нет параметров кодирования
            if (mode == 0)
            {
                m.encodingmode = Settings.EncodingModes.Quality;
                m.outvbitrate = 28;
            }
            else
                m.encodingmode = mode;

            return m;
        }

        public static Massive EncodeLine(Massive m)
        {
            //Определяем дефолты (с учетом --preset и --tune)
            x265_arguments defaults = new x265_arguments(m.x265options.preset, m.x265options.tune, m.x265options.profile);

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
            line += " --preset " + m.x265options.preset.ToString().ToLower();

            if (m.x265options.tune != defaults.tune && !m.x265options.extra_cli.Contains("--tune "))
                line += " --tune " + m.x265options.tune.ToString().ToLower();

            if (m.x265options.profile != defaults.profile && !m.x265options.extra_cli.Contains("--profile "))
            {
                if (m.x265options.profile == Profiles.Main) line += " --profile main";
                else if (m.x265options.profile == Profiles.Main444) line += " --profile main444-8";
                else if (m.x265options.profile == Profiles.Main_10) line += " --profile main10";
                else if (m.x265options.profile == Profiles.Main422_10) line += " --profile main422-10";
                else if (m.x265options.profile == Profiles.Main444_10) line += " --profile main444-10";
            }

            if (m.x265options.level != defaults.level && !m.x265options.extra_cli.Contains("--level-idc ") && !m.x265options.extra_cli.Contains("--level "))
                line += " --level-idc " + m.x265options.level;

            if (m.x265options.high_tier != defaults.high_tier && !m.x265options.extra_cli.Contains("--high-tier") && !m.x265options.extra_cli.Contains("--no-high-tier"))
                line += (m.x265options.high_tier) ? " --high-tier" : " --no-high-tier";

            if (m.x265options.lossless != defaults.lossless && !m.x265options.extra_cli.Contains("--lossless") && !m.x265options.extra_cli.Contains("--no-lossless"))
                line += (m.x265options.lossless) ? " --lossless" : " --no-lossless";

            if (m.x265options.reference != defaults.reference && !m.x265options.extra_cli.Contains("--ref "))
                line += " --ref " + m.x265options.reference;

            if (m.x265options.aqmode != defaults.aqmode && !m.x265options.extra_cli.Contains("--aq-mode "))
                line += " --aq-mode " + m.x265options.aqmode;

            if (m.x265options.aqstrength != defaults.aqstrength && m.x265options.aqmode != 0 && !m.x265options.extra_cli.Contains("--aq-strength "))
                line += " --aq-strength " + m.x265options.aqstrength;

            if (m.x265options.cutree != defaults.cutree && !m.x265options.extra_cli.Contains("--cutree") && !m.x265options.extra_cli.Contains("--no-cutree"))
                line += (m.x265options.cutree) ? " --cutree" : " --no-cutree";

            if (!m.x265options.deblocking && defaults.deblocking && !m.x265options.extra_cli.Contains("--no-deblock"))
                line += " --no-deblock";

            if (m.x265options.deblocking && (m.x265options.deblockTC != defaults.deblockTC || m.x265options.deblockBeta != defaults.deblockBeta) &&
                !m.x265options.extra_cli.Contains("--deblock "))
                line += " --deblock " + m.x265options.deblockTC + ":" + m.x265options.deblockBeta;

            if (m.x265options.sao != defaults.sao && !m.x265options.extra_cli.Contains("--sao") && !m.x265options.extra_cli.Contains("--no-sao"))
                line += (m.x265options.sao) ? " --sao" : " --no-sao";

            if (m.x265options.merange != defaults.merange && !m.x265options.extra_cli.Contains("--merange "))
                line += " --merange " + m.x265options.merange;

            if (m.x265options.max_merge != defaults.max_merge && !m.x265options.extra_cli.Contains("--max-merge "))
                line += " --max-merge " + m.x265options.max_merge;

            if (m.x265options.rd != defaults.rd && !m.x265options.extra_cli.Contains("--rd "))
                line += " --rd " + m.x265options.rd;

            if (m.x265options.ctu != defaults.ctu && !m.x265options.extra_cli.Contains("--ctu ") && !m.x265options.extra_cli.Contains("-s "))
                line += " --ctu " + m.x265options.ctu;

            if (m.x265options.cu_lossless != defaults.cu_lossless && !m.x265options.extra_cli.Contains("--cu-lossless") && !m.x265options.extra_cli.Contains("--no-cu-lossless"))
                line += (m.x265options.cu_lossless) ? " --cu-lossless" : " --no-cu-lossless";

            if (m.x265options.early_skip != defaults.early_skip && !m.x265options.extra_cli.Contains("--early-skip") && !m.x265options.extra_cli.Contains("--no-early-skip"))
                line += (m.x265options.early_skip) ? " --early-skip" : " --no-early-skip";

            if (m.x265options.rect != defaults.rect && !m.x265options.extra_cli.Contains("--rect") && !m.x265options.extra_cli.Contains("--no-rect"))
                line += (m.x265options.rect) ? " --rect" : " --no-rect";

            if (m.x265options.amp != defaults.amp && !m.x265options.extra_cli.Contains("--amp") && !m.x265options.extra_cli.Contains("--no-amp"))
                line += (m.x265options.amp) ? " --amp" : " --no-amp";

            if (m.x265options.constr_intra != defaults.constr_intra && !m.x265options.extra_cli.Contains("--constrained-intra") && !m.x265options.extra_cli.Contains("--no-constrained-intra"))
                line += (m.x265options.constr_intra) ? " --constrained-intra" : " --no-constrained-intra";

            if (m.x265options.b_intra != defaults.b_intra && !m.x265options.extra_cli.Contains("--b-intra") && !m.x265options.extra_cli.Contains("--no-b-intra"))
                line += (m.x265options.b_intra) ? " --b-intra" : " --no-b-intra";

            if (m.x265options.bframes != defaults.bframes && !m.x265options.extra_cli.Contains("--bframes "))
                line += " --bframes " + m.x265options.bframes;

            if (m.x265options.b_adapt != defaults.b_adapt && !m.x265options.extra_cli.Contains("--b-adapt ") && !m.x265options.extra_cli.Contains("--no-b-adapt"))
                line += " --b-adapt " + m.x265options.b_adapt;

            if (m.x265options.bpyramid != defaults.bpyramid && !m.x265options.extra_cli.Contains("--b-pyramid") && !m.x265options.extra_cli.Contains("--no-b-pyramid"))
                line += (m.x265options.bpyramid) ? " --b-pyramid" : " --no-b-pyramid";

            if (m.x265options.weightb != defaults.weightb && !m.x265options.extra_cli.Contains("--weightb") && !m.x265options.extra_cli.Contains("--no-weightb"))
                line += (m.x265options.weightb) ? " --weightb" : " --no-weightb";

            if (m.x265options.weightp != defaults.weightp && !m.x265options.extra_cli.Contains("--weightp") && !m.x265options.extra_cli.Contains("--no-weightp"))
                line += (m.x265options.weightp) ? " --weightp" : " --no-weightp";

            if (m.x265options.psyrdo != defaults.psyrdo && !m.x265options.extra_cli.Contains("--psy-rd "))
                line += " --psy-rd " + Calculate.ConvertDoubleToPointString((double)m.x265options.psyrdo, 2);

            if (m.x265options.psyrdoq != defaults.psyrdoq && !m.x265options.extra_cli.Contains("--psy-rdoq "))
                line += " --psy-rdoq " + Calculate.ConvertDoubleToPointString((double)m.x265options.psyrdoq, 2);

            if (m.x265options.threads != defaults.threads && !m.x265options.extra_cli.Contains("--threads "))
                line += " --threads " + m.x265options.threads;

            if (m.x265options.threads_frames != defaults.threads_frames && !m.x265options.extra_cli.Contains("--frame-threads "))
                line += " --frame-threads " + m.x265options.threads_frames;

            if (m.x265options.qcomp != defaults.qcomp && !m.x265options.extra_cli.Contains("--qcomp "))
                line += " --qcomp " + Calculate.ConvertDoubleToPointString((double)m.x265options.qcomp, 2);

            if (m.x265options.ratio_ip != defaults.ratio_ip && !m.x265options.extra_cli.Contains("--ipratio "))
                line += " --ipratio " + Calculate.ConvertDoubleToPointString((double)m.x265options.ratio_ip, 2);

            if (m.x265options.ratio_pb != defaults.ratio_pb && !m.x265options.extra_cli.Contains("--pbratio "))
                line += " --pbratio " + Calculate.ConvertDoubleToPointString((double)m.x265options.ratio_pb, 2);

            if (m.x265options.chroma_offset_cb != defaults.chroma_offset_cb && !m.x265options.extra_cli.Contains("--cbqpoffs "))
                line += " --cbqpoffs " + m.x265options.chroma_offset_cb;

            if (m.x265options.chroma_offset_cr != defaults.chroma_offset_cr && !m.x265options.extra_cli.Contains("--crqpoffs "))
                line += " --crqpoffs " + m.x265options.chroma_offset_cr;

            if (m.x265options.vbv_maxrate != defaults.vbv_maxrate && !m.x265options.extra_cli.Contains("--vbv-maxrate "))
                line += " --vbv-maxrate " + m.x265options.vbv_maxrate;

            if (m.x265options.vbv_bufsize != defaults.vbv_bufsize && !m.x265options.extra_cli.Contains("--vbv-bufsize "))
                line += " --vbv-bufsize " + m.x265options.vbv_bufsize;

            if (m.x265options.vbv_init != defaults.vbv_init && !m.x265options.extra_cli.Contains("--vbv-init "))
                line += " --vbv-init " + Calculate.ConvertDoubleToPointString((double)m.x265options.vbv_init, 2);

            if (m.x265options.subme != defaults.subme && !m.x265options.extra_cli.Contains("--subme "))
                line += " --subme " + m.x265options.subme;

            if (m.x265options.me != defaults.me && !m.x265options.extra_cli.Contains("--me "))
                line += " --me " + m.x265options.me;

            if (m.x265options.slow_firstpass != defaults.slow_firstpass && !m.x265options.extra_cli.Contains("--slow-firstpass") && !m.x265options.extra_cli.Contains("--no-slow-firstpass"))
                line += (m.x265options.slow_firstpass) ? " --slow-firstpass" : " --no-slow-firstpass";

            if (m.x265options.lookahead != defaults.lookahead && !m.x265options.extra_cli.Contains("--rc-lookahead "))
                line += " --rc-lookahead " + m.x265options.lookahead;

            if (m.x265options.gop_min != defaults.gop_min && !m.x265options.extra_cli.Contains("--min-keyint "))
                line += " --min-keyint " + m.x265options.gop_min;

            if (m.x265options.gop_max != defaults.gop_max && !m.x265options.extra_cli.Contains("--keyint "))
                line += " --keyint " + m.x265options.gop_max;

            if (m.x265options.open_gop != defaults.open_gop && !m.x265options.extra_cli.Contains("--open-gop") && !m.x265options.extra_cli.Contains("--no-open-gop"))
                line += (m.x265options.open_gop) ? " --open-gop" : " --no-open-gop";

            if (m.x265options.range_out != defaults.range_out && !m.x265options.extra_cli.Contains("--range "))
                line += " --range " + m.x265options.range_out;

            if (m.x265options.colorprim != defaults.colorprim && !m.x265options.extra_cli.Contains("--colorprim "))
                line += " --colorprim " + ((m.x265options.colorprim == "Undefined") ? "undef" : m.x265options.colorprim);

            if (m.x265options.transfer != defaults.transfer && !m.x265options.extra_cli.Contains("--transfer "))
                line += " --transfer " + ((m.x265options.transfer == "Undefined") ? "undef" : m.x265options.transfer);

            if (m.x265options.colormatrix != defaults.colormatrix && !m.x265options.extra_cli.Contains("--colormatrix "))
                line += " --colormatrix " + ((m.x265options.colormatrix == "Undefined") ? "undef" : m.x265options.colormatrix);

            if (m.x265options.hash != defaults.hash && !m.x265options.extra_cli.Contains("--hash "))
                line += " --hash " + m.x265options.hash;

            if (m.x265options.info != defaults.info && !m.x265options.extra_cli.Contains("--info") && !m.x265options.extra_cli.Contains("--no-info"))
                line += (m.x265options.info) ? " --info" : " --no-info";

            if (m.x265options.aud != defaults.aud && !m.x265options.extra_cli.Contains("--aud") && !m.x265options.extra_cli.Contains("--no-aud"))
                line += (m.x265options.aud) ? " --aud" : " --no-aud";

            if (m.x265options.hrd != defaults.hrd && !m.x265options.extra_cli.Contains("--hrd") && !m.x265options.extra_cli.Contains("--no-hrd"))
                line += (m.x265options.hrd) ? " --hrd" : " --no-hrd";

            if (m.x265options.headers_repeat != defaults.headers_repeat && !m.x265options.extra_cli.Contains("--repeat-headers") && !m.x265options.extra_cli.Contains("--no-repeat-headers"))
                line += (m.x265options.headers_repeat) ? " --repeat-headers" : " --no-repeat-headers";

            if (m.x265options.wpp != defaults.wpp && !m.x265options.extra_cli.Contains("--wpp") && !m.x265options.extra_cli.Contains("--no-wpp"))
                line += (m.x265options.wpp) ? " --wpp" : " --no-wpp";

            if (m.x265options.pmode != defaults.pmode && !m.x265options.extra_cli.Contains("--pmode") && !m.x265options.extra_cli.Contains("--no-pmode"))
                line += (m.x265options.pmode) ? " --pmode" : " --no-pmode";

            if (m.x265options.pme != defaults.pme && !m.x265options.extra_cli.Contains("--pme") && !m.x265options.extra_cli.Contains("--no-pme"))
                line += (m.x265options.pme) ? " --pme" : " --no-pme";

            line += " --extra:";
            if (m.x265options.extra_cli != defaults.extra_cli)
                line += " " + m.x265options.extra_cli;

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

                    string x265mode = combo_mode.SelectedItem.ToString();
                    if (x265mode == "1-Pass Bitrate") m.encodingmode = Settings.EncodingModes.OnePass;
                    else if (x265mode == "2-Pass Bitrate") m.encodingmode = Settings.EncodingModes.TwoPass;
                    else if (x265mode == "2-Pass Size") m.encodingmode = Settings.EncodingModes.TwoPassSize;
                    else if (x265mode == "3-Pass Bitrate") m.encodingmode = Settings.EncodingModes.ThreePass;
                    else if (x265mode == "3-Pass Size") m.encodingmode = Settings.EncodingModes.ThreePassSize;
                    else if (x265mode == "Constant Quality") m.encodingmode = Settings.EncodingModes.Quality;
                    else if (x265mode == "Constant Quantizer") m.encodingmode = Settings.EncodingModes.Quantizer;
                    else if (x265mode == "2-Pass Quality") m.encodingmode = Settings.EncodingModes.TwoPassQuality;
                    else if (x265mode == "3-Pass Quality") m.encodingmode = Settings.EncodingModes.ThreePassQuality;

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
                m.outvbitrate = 28;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Quality") + ": (CRF)";
            }
            else if (m.encodingmode == Settings.EncodingModes.Quantizer)
            {
                m.outvbitrate = 28;
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
                m.x265options.level = combo_level.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_high_tier_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.high_tier = check_high_tier.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_lossless_Click(object sender, RoutedEventArgs e)
        {
            if (check_lossless.IsChecked.Value)
            {
                m.x265options.lossless = true;
                combo_mode.SelectedItem = "Constant Quantizer";
                m.encodingmode = Settings.EncodingModes.Quantizer;
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                num_bitrate.Value = m.outvbitrate = 28;
                /*if (m.x265options.profile != Profiles.Auto && m.x265options.profile != Profiles.High10)
                {
                    combo_avc_profile.SelectedIndex = 0;
                    m.x265options.profile = Profiles.Auto;
                }*/

                SetToolTips();
                SetMinMaxBitrate();
            }
            else
                m.x265options.lossless = false;

            root_window.UpdateOutSize();
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_deblocking_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.deblocking = check_deblocking.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_dstrength_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_dstrength.IsDropDownOpen || combo_dstrength.IsSelectionBoxHighlighted) && combo_dstrength.SelectedItem != null)
            {
                m.x265options.deblockTC = Convert.ToInt32(combo_dstrength.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_dthreshold_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_dthreshold.IsDropDownOpen || combo_dthreshold.IsSelectionBoxHighlighted) && combo_dthreshold.SelectedItem != null)
            {
                m.x265options.deblockBeta = Convert.ToInt32(combo_dthreshold.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_sao_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.sao = check_sao.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_subme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_subme.IsDropDownOpen || combo_subme.IsSelectionBoxHighlighted) && combo_subme.SelectedIndex != -1)
            {
                m.x265options.subme = combo_subme.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_me_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_me.IsDropDownOpen || combo_me.IsSelectionBoxHighlighted) && combo_me.SelectedItem != null)
            {
                string me = combo_me.SelectedItem.ToString();
                if (me == "Diamond") m.x265options.me = "dia";
                else if (me == "Hexagon") m.x265options.me = "hex";
                else if (me == "Multi Hexagon") m.x265options.me = "umh";
                else if (me == "Star") m.x265options.me = "star";
                else if (me == "Full") m.x265options.me = "full";

                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_merange_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_merange.IsDropDownOpen || combo_merange.IsSelectionBoxHighlighted) && combo_merange.SelectedItem != null)
            {
                m.x265options.merange = Convert.ToInt32(combo_merange.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted) && combo_bframes.SelectedIndex != -1)
            {
                m.x265options.bframes = combo_bframes.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_weightedp_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.weightp = check_weightedp.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_weightedb_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.weightb = check_weightedb.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_ref_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_ref.IsDropDownOpen || combo_ref.IsSelectionBoxHighlighted) && combo_ref.SelectedItem != null)
            {
                m.x265options.reference = Convert.ToInt32(combo_ref.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_hevc_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_hevc_profile.IsDropDownOpen || combo_hevc_profile.IsSelectionBoxHighlighted) && combo_hevc_profile.SelectedIndex != -1)
            {
                if (combo_hevc_profile.SelectedIndex == 1) m.x265options.profile = Profiles.Main;
                else if (combo_hevc_profile.SelectedIndex == 2) m.x265options.profile = Profiles.Main444;
                else if (combo_hevc_profile.SelectedIndex == 3) m.x265options.profile = Profiles.Main_10;
                else if (combo_hevc_profile.SelectedIndex == 4) m.x265options.profile = Profiles.Main422_10;
                else if (combo_hevc_profile.SelectedIndex == 5) m.x265options.profile = Profiles.Main444_10;
                else m.x265options.profile = Profiles.Auto;

                //Тултипы и дефолты под 8\10-bit
                SetToolTips();
                SetMinMaxBitrate();

                //Проверяем выход за лимиты 8-ми и 10-ти битных версий
                if (m.encodingmode == Settings.EncodingModes.Quality ||
                    m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    m.encodingmode == Settings.EncodingModes.ThreePassQuality ||
                    m.encodingmode == Settings.EncodingModes.Quantizer)
                {
                    if (!m.x265options.profile.ToString().EndsWith("10") && m.outvbitrate < 0)
                    {
                        num_bitrate.Value = m.outvbitrate = 1;
                    }
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
                m.x265options.preset = (Presets)Enum.ToObject(typeof(Presets), (int)slider_preset.Value);
                x265_arguments defaults = new x265_arguments(m.x265options.preset, m.x265options.tune, m.x265options.profile);

                m.x265options.lookahead = defaults.lookahead;
                m.x265options.ctu = defaults.ctu;
                m.x265options.merange = defaults.merange;
                m.x265options.b_adapt = defaults.b_adapt;
                m.x265options.subme = defaults.subme;
                m.x265options.me = defaults.me;
                m.x265options.early_skip = defaults.early_skip;
                m.x265options.sao = defaults.sao;
                m.x265options.weightp = defaults.weightp;
                m.x265options.rd = defaults.rd;
                m.x265options.reference = defaults.reference;
                m.x265options.deblocking = defaults.deblocking;
                m.x265options.aqmode = defaults.aqmode;
                m.x265options.aqstrength = defaults.aqstrength;
                m.x265options.cutree = defaults.cutree;
                m.x265options.amp = defaults.amp;
                m.x265options.rect = defaults.rect;
                m.x265options.max_merge = defaults.max_merge;
                m.x265options.bframes = defaults.bframes;
                m.x265options.weightb = defaults.weightb;
                m.x265options.slow_firstpass = defaults.slow_firstpass;
                m.x265options.b_intra = defaults.b_intra;

                /*
                scenecutThreshold     scenecut
                bEnableSignHiding     signhide
                bEnableFastIntra      fast-intra
                tuQTMaxInterDepth     tu-inter-depth
                tuQTMaxIntraDepth     tu-intra-depth
                bEnableTransformSkip  tskip
                */

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.x265options (это нужно для "--extra:")
                LoadFromProfile(); //Выставляем значения из m.x265options в элементы управления
            }
        }

        private void combo_tune_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_tune.IsDropDownOpen || combo_tune.IsSelectionBoxHighlighted) && combo_tune.SelectedIndex != -1)
            {
                //Создаем новые параметры с учетом --tune, и берем от них только те, от которых зависит tune
                m.x265options.tune = (Tunes)Enum.ToObject(typeof(Tunes), combo_tune.SelectedIndex);
                x265_arguments defaults = new x265_arguments(m.x265options.preset, m.x265options.tune, m.x265options.profile);

                m.x265options.deblockBeta = defaults.deblockBeta;
                m.x265options.deblockTC = defaults.deblockTC;
                m.x265options.b_intra = defaults.b_intra;
                m.x265options.psyrdoq = defaults.psyrdoq;
                m.x265options.psyrdo = defaults.psyrdo;
                m.x265options.ratio_ip = defaults.ratio_ip;
                m.x265options.ratio_pb = defaults.ratio_pb;
                m.x265options.aqmode = defaults.aqmode;
                m.x265options.aqstrength = defaults.aqstrength;
                m.x265options.qcomp = defaults.qcomp;
                m.x265options.deblocking = defaults.deblocking;
                m.x265options.sao = defaults.sao;
                m.x265options.weightp = defaults.weightp;
                m.x265options.weightb = defaults.weightb;
                m.x265options.b_intra = defaults.b_intra;

                /* zerolatency
                m.x265options.b_adapt = defaults.b_adapt;
                m.x265options.bframes = defaults.bframes;
                m.x265options.lookahead = defaults.lookahead;
                m.x265options.cutree = defaults.cutree;
                m.x265options.threads_frames = defaults.threads_frames;
                */

                /*
                scenecutThreshold     scenecut
                */

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
                m = DecodeLine(m); //Пересчитываем параметры из CLI в m.x265options (это нужно для "--extra:")
                LoadFromProfile(); //Выставляем значения из m.x265options в элементы управления
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
            else
            {
                num_bitrate.Minimum = (m.x265options.profile.ToString().EndsWith("10")) ? -12 : 0;
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
                m.x265options.psyrdo = num_psyrdo.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_psyrdoq_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_psyrdoq.IsAction)
            {
                m.x265options.psyrdoq = num_psyrdoq.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_adapt_quant_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_adapt_quant_mode.IsDropDownOpen || combo_adapt_quant_mode.IsSelectionBoxHighlighted) && combo_adapt_quant_mode.SelectedIndex != -1)
            {
                m.x265options.aqmode = combo_adapt_quant_mode.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_adapt_quant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_adapt_quant.IsDropDownOpen || combo_adapt_quant.IsSelectionBoxHighlighted) && combo_adapt_quant.SelectedItem != null)
            {
                m.x265options.aqstrength = combo_adapt_quant.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_wpp_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.wpp = check_wpp.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_pmode_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.pmode = check_pmode.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_pme_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.pme = check_pme.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_threads_count_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_threads_count.IsDropDownOpen || combo_threads_count.IsSelectionBoxHighlighted) && combo_threads_count.SelectedIndex != -1)
            {
                m.x265options.threads = combo_threads_count.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_threads_frames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_threads_frames.IsDropDownOpen || combo_threads_frames.IsSelectionBoxHighlighted) && combo_threads_frames.SelectedIndex != -1)
            {
                m.x265options.threads_frames = combo_threads_frames.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_badapt_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_badapt_mode.IsDropDownOpen || combo_badapt_mode.IsSelectionBoxHighlighted)
            {
                m.x265options.b_adapt = combo_badapt_mode.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_bpyramid_mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_bpyramid_mode.IsDropDownOpen || combo_bpyramid_mode.IsSelectionBoxHighlighted) && combo_bpyramid_mode.SelectedIndex != -1)
            {
                m.x265options.bpyramid = (combo_bpyramid_mode.SelectedIndex == 1);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_qcomp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_qcomp.IsAction)
            {
                m.x265options.qcomp = num_qcomp.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_max.IsAction)
            {
                m.x265options.vbv_maxrate = Convert.ToInt32(num_vbv_max.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_buf_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_buf.IsAction)
            {
                m.x265options.vbv_bufsize = Convert.ToInt32(num_vbv_buf.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_vbv_init_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_vbv_init.IsAction)
            {
                m.x265options.vbv_init = num_vbv_init.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }
        
        private void check_slow_first_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.slow_firstpass = check_slow_first.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void num_lookahead_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_lookahead.IsAction)
            {
                m.x265options.lookahead = Convert.ToInt32(num_lookahead.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
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

                DecodeLine(m);                       //- Загружаем в массив m.x265 значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                   //- Загружаем в форму значения, на основе значений массива m.x265
                m.vencoding = "Custom x265 CLI";     //- Изменяем название пресета
                PresetLoader.CreateVProfile(m);      //- Перезаписываем файл пресета (m.vpasses[x])
                root_window.m = this.m.Clone();      //- Передаем массив в основное окно
                root_window.LoadProfiles();          //- Обновляем название выбранного пресета в основном окне (Custom x265 CLI)
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
                DecodeLine(m);                           //- Загружаем в массив m.x265 значения, на основе текущего содержимого m.vpasses[x]
                LoadFromProfile();                       //- Загружаем в форму значения, на основе значений массива m.x265
                root_window.m = this.m.Clone();          //- Передаем массив в основное окно
            }
            else
            {
                new Message(root_window).ShowMessage("Can`t find good CLI...", Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void button_x265_help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool is_10bit = m.x265options.profile.ToString().EndsWith("10");
                bool x64 = (Settings.UseAVS4x265 && SysInfo.GetOSArchInt() == 64);
                System.Diagnostics.ProcessStartInfo help = new System.Diagnostics.ProcessStartInfo();
                help.FileName = Calculate.StartupPath + "\\apps\\x265\\" + ((x64) ? "x265_64" : "x265") + ((is_10bit) ? "_10b.exe" : ".exe");
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = "--log-level full --help";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                help.RedirectStandardError = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                string title = "x265 " + ((is_10bit) ? "10" : "8") + "-bit depth " + ((x64) ? "(x64) " : "") + "--fullhelp";
                string info = p.StandardOutput.ReadToEnd();
                string ver = p.StandardError.ReadToEnd();
                new ShowWindow(root_window, title, ver + info, new FontFamily("Lucida Console"));
            }
            catch (Exception ex)
            {
                new Message(root_window).ShowMessage(ex.Message, Languages.Translate("Error"));
            }
        }

        private void check_info_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.info = check_info.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_aud_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.aud = check_aud.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_hrd_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.hrd = check_hrd.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_headers_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.headers_repeat = check_headers.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void num_min_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_gop.IsAction)
            {
                m.x265options.gop_min = Convert.ToInt32(num_min_gop.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_max_gop_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_max_gop.IsAction)
            {
                m.x265options.gop_max = Convert.ToInt32(num_max_gop.Value);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_ratio_ip_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ratio_ip.IsAction)
            {
                m.x265options.ratio_ip = num_ratio_ip.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_ratio_pb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_ratio_pb.IsAction)
            {
                m.x265options.ratio_pb = num_ratio_pb.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_open_gop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_open_gop.IsDropDownOpen || combo_open_gop.IsSelectionBoxHighlighted) && combo_open_gop.SelectedIndex != -1)
            {
                m.x265options.open_gop = (combo_open_gop.SelectedIndex == 1);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_range_out_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_range_out.IsDropDownOpen || combo_range_out.IsSelectionBoxHighlighted) && combo_range_out.SelectedItem != null)
            {
                m.x265options.range_out = combo_range_out.SelectedItem.ToString().ToLower();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colorprim_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_colorprim.IsDropDownOpen || combo_colorprim.IsSelectionBoxHighlighted) && combo_colorprim.SelectedItem != null)
            {
                m.x265options.colorprim = combo_colorprim.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_transfer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_transfer.IsDropDownOpen || combo_transfer.IsSelectionBoxHighlighted) && combo_transfer.SelectedItem != null)
            {
                m.x265options.transfer = combo_transfer.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_colormatrix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_colormatrix.IsDropDownOpen || combo_colormatrix.IsSelectionBoxHighlighted) && combo_colormatrix.SelectedItem != null)
            {
                m.x265options.colormatrix = combo_colormatrix.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_hash_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_hash.IsDropDownOpen || combo_hash.IsSelectionBoxHighlighted) && combo_hash.SelectedIndex != -1)
            {
                m.x265options.hash = combo_hash.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_chroma_qpb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_chroma_qpb.IsAction)
            {
                m.x265options.chroma_offset_cb = (int)num_chroma_qpb.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void num_chroma_qpr_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_chroma_qpr.IsAction)
            {
                m.x265options.chroma_offset_cr = (int)num_chroma_qpr.Value;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_cutree_Clicked(object sender, RoutedEventArgs e)
        {
            m.x265options.cutree = check_cutree.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void combo_max_merge_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_max_merge.IsDropDownOpen || combo_max_merge.IsSelectionBoxHighlighted) && combo_max_merge.SelectedIndex != -1)
            {
                m.x265options.max_merge = combo_max_merge.SelectedIndex + 1;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_rd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_rd.IsDropDownOpen || combo_rd.IsSelectionBoxHighlighted) && combo_rd.SelectedIndex != -1)
            {
                m.x265options.rd = combo_rd.SelectedIndex;
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void combo_ctu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_ctu.IsDropDownOpen || combo_ctu.IsSelectionBoxHighlighted) && combo_ctu.SelectedItem != null)
            {
                m.x265options.ctu = Convert.ToInt32(combo_ctu.SelectedItem);
                root_window.UpdateManualProfile();
                UpdateCLI();
            }
        }

        private void check_cu_lossless_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.cu_lossless = check_cu_lossless.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_early_skip_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.early_skip = check_early_skip.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_rect_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.rect = check_rect.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_amp_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.amp = check_amp.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_constr_intra_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.constr_intra = check_constr_intra.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }

        private void check_b_intra_Click(object sender, RoutedEventArgs e)
        {
            m.x265options.b_intra = check_b_intra.IsChecked.Value;
            root_window.UpdateManualProfile();
            UpdateCLI();
        }
    }
}