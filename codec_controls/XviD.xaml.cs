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

            //прогружаем матрицы квантизации
            combo_qmatrix.Items.Add("H263");
            combo_qmatrix.Items.Add("MPEG");
            foreach (string matrix in PresetLoader.CustomMatrixes(MatrixTypes.CQM))
                combo_qmatrix.Items.Add(matrix);

            //прогружаем fourcc
            combo_fourcc.Items.Add("XVID");
            combo_fourcc.Items.Add("FFDS");
            combo_fourcc.Items.Add("FVFW");
            combo_fourcc.Items.Add("DX50");
            combo_fourcc.Items.Add("DIVX");
            combo_fourcc.Items.Add("MP4V");

            //B фреймы
            for (int n = 0; n <= 4; n++)
                combo_bframes.Items.Add(n);

            //пресеты настроек и качества
            foreach (string preset in Enum.GetNames(typeof(CodecPresets)))
                combo_codec_preset.Items.Add(preset);

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
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
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
            check_chroma.IsChecked = m.XviD_options.chromame;
            combo_qmatrix.SelectedItem = m.XviD_options.qmatrix;
            check_trellis.IsChecked = m.XviD_options.trellis;
            check_grayscale.IsChecked = m.XviD_options.grey;
            check_cartoon.IsChecked = m.XviD_options.cartoon;
            check_packedmode.IsChecked = m.XviD_options.packedmode;
            check_gmc.IsChecked = m.XviD_options.gmc;
            check_qpel.IsChecked = m.XviD_options.qpel;
            check_bvhq.IsChecked = m.XviD_options.bvhq;
            check_closedgop.IsChecked = m.XviD_options.closedgop;
            combo_bframes.SelectedItem = m.XviD_options.bframes;
            check_lumimasking.IsChecked = m.XviD_options.limimasking;
            combo_fourcc.SelectedItem = m.XviD_options.fourcc;

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
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else
                num_bitrate.ToolTip = "Set target quality (Default: 3)";

            combo_quality.ToolTip = "Quality (Default: 6)";
            combo_vhqmode.ToolTip = "Level of R-D optimizations (Default: 1)";
            check_chroma.ToolTip = "Chroma motion estimation (Default: Enabled)";
            combo_qmatrix.ToolTip = "Use custom MPEG4 quantization matrix (Default: H263)";
            check_trellis.ToolTip = "Trellis quantization (Default: Enabled)";
            check_grayscale.ToolTip = "Greyscale mode (Default: Disabled)";
            check_cartoon.ToolTip = "Cartoon mode (Default: Disabled)";
            check_packedmode.ToolTip = "Packed mode (Default: Enabled). Don`t work in multi-pass modes.";
            check_gmc.ToolTip = "Use global motion compensation (Default: Disabled)";
            check_qpel.ToolTip = "Use quarter pixel ME (Default: Disabled)";
            check_bvhq.ToolTip = "Use R-D optimizations for B-frames (Default: Disabled)";
            check_closedgop.ToolTip = "Closed GOP mode (Default: Enabled)";
            combo_bframes.ToolTip = "Max bframes (Default: 2)";
            check_lumimasking.ToolTip = "Use lumimasking algorithm (Default: Disabled)";
            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                combo_codec_preset.ToolTip = "Default - default codec settings to default" + Environment.NewLine +
                    "Turbo - fast encoding, big output file size" + Environment.NewLine +
                    "Ultra - high quality encoding, medium file size" + Environment.NewLine +
                    "Extreme - super high quality encoding, smallest file size" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            else
            {
                combo_codec_preset.ToolTip = "Default - default codec settings to default" + Environment.NewLine +
                    "Turbo - fast encoding, bad quality" + Environment.NewLine +
                    "Ultra - high quality encoding, optimal speed-quality solution" + Environment.NewLine +
                    "Extreme - super high quality encoding, very slow" + Environment.NewLine +
                    "Custom - custom codec settings";
            }
            combo_fourcc.ToolTip = "Force video tag/fourcc (Default: XVID)";
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

            //берём пока что за основу последнюю строку
            string line = m.vpasses[m.vpasses.Count - 1].ToString();

            string[] separator = new string[] { " " };
            string[] cli = line.Split(separator, StringSplitOptions.None);
            int n = 0;

            foreach (string value in cli)
            {
                if (value == "-bitrate")
                    m.outvbitrate = Convert.ToInt32(cli[n + 1]);

                if (value == "-size")
                {
                    m.outvbitrate = Convert.ToInt32(cli[n + 1]) / 1000;
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
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (m.vpasses.Count == 2)
                {
                    if (value == "-cq")
                    {
                        mode = Settings.EncodingModes.TwoPassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (m.vpasses.Count == 3)
                {
                    if (value == "-cq")
                    {
                        mode = Settings.EncodingModes.ThreePassQuality;
                        m.outvbitrate = (decimal)Calculate.ConvertStringToDouble(cli[n + 1]);
                    }
                }

                if (value == "-quality")
                    m.XviD_options.quality = Convert.ToInt32(cli[n + 1]);

                else if (value == "-nochromame")
                    m.XviD_options.chromame = false;

                else if (value == "-qtype")
                {
                    int qtype = Convert.ToInt32(cli[n + 1]);
                    if (qtype == 0)
                        m.XviD_options.qmatrix = "H263";
                    if (qtype == 1)
                        m.XviD_options.qmatrix = "MPEG";
                }

                else if (value == "-qmatrix")
                {
                    string mpath = cli[n + 1].Replace("\"", "");
                    if (File.Exists(mpath))
                        m.XviD_options.qmatrix = Path.GetFileNameWithoutExtension(mpath);
                }

                else if (value == "-notrellis")
                    m.XviD_options.trellis = false;

                else if (value == "-vhqmode")
                    m.XviD_options.vhqmode = Convert.ToInt32(cli[n + 1]);

                else if (value == "-zones")
                {
                    string zone = cli[n + 1];
                    if (zone == "0,w,1.0,-5G/1000,q,4")
                        m.XviD_options.grey = true;
                    if (zone == "0,w,1.0,-5C/1000,q,4")
                        m.XviD_options.cartoon = true;
                    if (zone == "0,w,1.0,-5GC/1000,q,4")
                    {
                        m.XviD_options.grey = true;
                        m.XviD_options.cartoon = true;
                    }
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

                //-max_bframes
                else if (value == "-max_bframes")
                    m.XviD_options.bframes = Convert.ToInt32(cli[n + 1]);

                else if (value == "-lumimasking")
                    m.XviD_options.limimasking = true;

                else if (value == "-fourcc")
                    m.XviD_options.fourcc = cli[n + 1];

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

            //if (m.XviD_options.cartoon)
            //    line += " -cartoon";

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

            if (m.XviD_options.grey &&
                !m.XviD_options.cartoon)
                line += " -zones 0,w,1.0,-5G/1000,q,4";

            if (!m.XviD_options.grey &&
                m.XviD_options.cartoon)
                line += " -zones 0,w,1.0,-5C/1000,q,4";

            if (m.XviD_options.grey &&
                m.XviD_options.cartoon)
                line += " -zones 0,w,1.0,-5GC/1000,q,4";

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

            if (m.XviD_options.limimasking)
                line += " -lumimasking";

            if (m.XviD_options.fourcc != "XVID")
                line += " -fourcc " + m.XviD_options.fourcc;

            //передаём параметры в турбо строку
            line_turbo = line;// +" -turbo";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line = "-bitrate " + m.outvbitrate + line;
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
                line = "-size " + m.outvbitrate * 1000 + line;
            else
                line = "-cq " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1) + line;

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
                m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }

            if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize)
            {
                m.vpasses.Add(line_turbo);
                m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
                m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }

            if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
            {
                m.vpasses.Add(line);
                m.vpasses.Add(line);
                m.vpasses.Add(line + " -imin 1 -bmin 1 -pmin 1");
            }

            return m;
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
                else if (oldmode != Settings.EncodingModes.OnePass &&
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
                else if (oldmode != Settings.EncodingModes.TwoPassSize &&
                         oldmode != Settings.EncodingModes.ThreePassSize)
                {
                    if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassSize)
                    {
                        SetDefaultBitrates();
                    }
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
                    if (check_packedmode.IsChecked.Value)
                        check_packedmode.IsChecked = false;
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
                m.outvbitrate = 3.0m;
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

        private void combo_quality_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_quality.IsDropDownOpen || combo_quality.IsSelectionBoxHighlighted)
            {
                m.XviD_options.quality = Convert.ToInt32(combo_quality.SelectedItem.ToString().Substring(0, 1));
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }


        private void check_chroma_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_chroma.IsFocused)
            {
                m.XviD_options.chromame = check_chroma.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_chroma_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_chroma.IsFocused)
            {
                m.XviD_options.chromame = check_chroma.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_qmatrix_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_qmatrix.IsDropDownOpen || combo_qmatrix.IsSelectionBoxHighlighted)
            {
                m.XviD_options.qmatrix = combo_qmatrix.SelectedItem.ToString();
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_trellis_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_trellis.IsFocused)
            {
                m.XviD_options.trellis = check_trellis.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_trellis_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_trellis.IsFocused)
            {
                m.XviD_options.trellis = check_trellis.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_vhqmode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_vhqmode.IsDropDownOpen || combo_vhqmode.IsSelectionBoxHighlighted)
            {
                m.XviD_options.vhqmode = Convert.ToInt32(combo_vhqmode.SelectedItem.ToString().Substring(0, 1));
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_grayscale_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_grayscale.IsFocused)
            {
                m.XviD_options.grey = check_grayscale.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_grayscale_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_grayscale.IsFocused)
            {
                m.XviD_options.grey = check_grayscale.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_cartoon_Checked(object sender, RoutedEventArgs e)
        {
            if (check_cartoon.IsFocused)
            {
                m.XviD_options.cartoon = check_cartoon.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_cartoon_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_cartoon.IsFocused)
            {
                m.XviD_options.cartoon = check_cartoon.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_packedmode_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_packedmode.IsFocused)
            {
                m.XviD_options.packedmode = check_packedmode.IsChecked.Value;
                if (m.XviD_options.bframes == 0)
                {
                    m.XviD_options.bframes = 1;
                    combo_bframes.SelectedItem = 1;
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_packedmode_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_packedmode.IsFocused)
            {
                m.XviD_options.packedmode = check_packedmode.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_gmc_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_gmc.IsFocused)
            {
                m.XviD_options.gmc = check_gmc.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_gmc_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_gmc.IsFocused)
            {
                m.XviD_options.gmc = check_gmc.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_qpel_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_qpel.IsFocused)
            {
                m.XviD_options.qpel = check_qpel.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_qpel_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_qpel.IsFocused)
            {
                m.XviD_options.qpel = check_qpel.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_bvhq_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_bvhq.IsFocused)
            {
                m.XviD_options.bvhq = check_bvhq.IsChecked.Value;
                if (m.XviD_options.bframes == 0)
                {
                    m.XviD_options.bframes = 1;
                    combo_bframes.SelectedItem = 1;
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_bvhq_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_bvhq.IsFocused)
            {
                m.XviD_options.bvhq = check_bvhq.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_closedgop_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_closedgop.IsFocused)
            {
                m.XviD_options.closedgop = check_closedgop.IsChecked.Value;
                if (m.XviD_options.bframes == 0)
                {
                    m.XviD_options.bframes = 1;
                    combo_bframes.SelectedItem = 1;
                }
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_closedgop_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_closedgop.IsFocused)
            {
                m.XviD_options.closedgop = check_closedgop.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_bframes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_bframes.IsDropDownOpen || combo_bframes.IsSelectionBoxHighlighted)
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

        private void check_lumimasking_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_lumimasking.IsFocused)
            {
                m.XviD_options.limimasking = check_lumimasking.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void check_lumimasking_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_lumimasking.IsFocused)
            {
                m.XviD_options.limimasking = check_lumimasking.IsChecked.Value;
                root_window.UpdateManualProfile();
                DetectCodecPreset();
            }
        }

        private void combo_codec_preset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_codec_preset.IsDropDownOpen || combo_codec_preset.IsSelectionBoxHighlighted)
            {
                CodecPresets preset = (CodecPresets)Enum.Parse(typeof(CodecPresets), combo_codec_preset.SelectedItem.ToString());

                if (preset == CodecPresets.Default)
                {
                    m.XviD_options.bframes = 2;
                    m.XviD_options.bvhq = false;
                    m.XviD_options.chromame = true;
                    m.XviD_options.closedgop = true;
                    m.XviD_options.gmc = false;
                    m.XviD_options.limimasking = false;
                    m.XviD_options.packedmode = true;
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = false;
                    m.XviD_options.quality = 6;
                    m.XviD_options.trellis = true;
                    m.XviD_options.vhqmode = 1;
                    m.XviD_options.fourcc = "XVID";
                    SetDefaultBitrates();
                }
                else if (preset == CodecPresets.Turbo)
                {
                    m.XviD_options.bframes = Format.GetMaxBFrames(m);
                    m.XviD_options.bvhq = false;
                    m.XviD_options.chromame = false;
                    m.XviD_options.closedgop = Format.GetValidBiValue(m);
                    m.XviD_options.gmc = false;
                    m.XviD_options.limimasking = false;
                    m.XviD_options.packedmode = false;
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = false;
                    m.XviD_options.quality = 1;
                    m.XviD_options.trellis = false;
                    m.XviD_options.vhqmode = 0;
                }
                else if (preset == CodecPresets.Ultra)
                {
                    m.XviD_options.bframes = Format.GetMaxBFrames(m);
                    m.XviD_options.bvhq = Format.GetValidBiValue(m);
                    m.XviD_options.chromame = true;
                    m.XviD_options.closedgop = Format.GetValidBiValue(m);
                    m.XviD_options.gmc = Format.GetValidGMC(m);
                    m.XviD_options.limimasking = false;
                    m.XviD_options.packedmode = Format.GetValidPackedMode(m);
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = Format.GetValidQPEL(m);
                    m.XviD_options.quality = 6;
                    m.XviD_options.trellis = true;
                    m.XviD_options.vhqmode = 1;
                }
                else if (preset == CodecPresets.Extreme)
                {
                    m.XviD_options.bframes = Format.GetMaxBFrames(m);
                    m.XviD_options.bvhq = Format.GetValidBiValue(m);
                    m.XviD_options.chromame = true;
                    m.XviD_options.closedgop = Format.GetValidBiValue(m);
                    m.XviD_options.gmc = Format.GetValidGMC(m);
                    m.XviD_options.limimasking = true;
                    m.XviD_options.packedmode = Format.GetValidPackedMode(m);
                    m.XviD_options.qmatrix = "H263";
                    m.XviD_options.qpel = Format.GetValidQPEL(m);
                    m.XviD_options.quality = 6;
                    m.XviD_options.trellis = true;
                    m.XviD_options.vhqmode = 4;
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
                m.XviD_options.limimasking == false &&
                m.XviD_options.packedmode == true &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == false &&
                m.XviD_options.quality == 6 &&
                m.XviD_options.trellis == true &&
                m.XviD_options.vhqmode == 1 &&
                m.XviD_options.fourcc == "XVID")
                preset = CodecPresets.Default;

            //Turbo
            else if (m.XviD_options.bframes == Format.GetMaxBFrames(m) &&
                m.XviD_options.bvhq == false &&
                m.XviD_options.chromame == false &&
                m.XviD_options.closedgop == Format.GetValidBiValue(m) &&
                m.XviD_options.gmc == false &&
                m.XviD_options.limimasking == false &&
                m.XviD_options.packedmode == false &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == false &&
                m.XviD_options.quality == 1 &&
                m.XviD_options.trellis == false &&
                m.XviD_options.vhqmode == 0)
                preset = CodecPresets.Turbo;

            //Ultra
            else if (m.XviD_options.bframes == Format.GetMaxBFrames(m) &&
                m.XviD_options.bvhq == Format.GetValidBiValue(m) &&
                m.XviD_options.chromame == true &&
                m.XviD_options.closedgop == Format.GetValidBiValue(m) &&
                m.XviD_options.gmc == Format.GetValidGMC(m) &&
                m.XviD_options.limimasking == false &&
                m.XviD_options.packedmode == Format.GetValidPackedMode(m) &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == Format.GetValidQPEL(m) &&
                m.XviD_options.quality == 6 &&
                m.XviD_options.trellis == true &&
                m.XviD_options.vhqmode == 1)
                preset = CodecPresets.Ultra;

            //Extreme
            else if (m.XviD_options.bframes == Format.GetMaxBFrames(m) &&
                m.XviD_options.bvhq == Format.GetValidBiValue(m) &&
                m.XviD_options.chromame == true &&
                m.XviD_options.closedgop == Format.GetValidBiValue(m) &&
                m.XviD_options.gmc == Format.GetValidGMC(m) &&
                m.XviD_options.limimasking == true &&
                m.XviD_options.packedmode == Format.GetValidPackedMode(m) &&
                m.XviD_options.qmatrix == "H263" &&
                m.XviD_options.qpel == Format.GetValidQPEL(m) &&
                m.XviD_options.quality == 6 &&
                m.XviD_options.trellis == true &&
                m.XviD_options.vhqmode == 4)
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
            if (combo_fourcc.IsDropDownOpen || combo_fourcc.IsSelectionBoxHighlighted)
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
                help.FileName = Calculate.StartupPath + "\\apps\\xvid_encraw\\xvid_encraw.exe";
                help.WorkingDirectory = Path.GetDirectoryName(help.FileName);
                help.Arguments = " -help";
                help.UseShellExecute = false;
                help.CreateNoWindow = true;
                help.RedirectStandardOutput = true;
                help.RedirectStandardError = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(help);
                new ShowWindow(root_window, "XviD help", p.StandardError.ReadToEnd(), new FontFamily("Lucida Console"));
            }
            catch (Exception) { }
        }
	}
}