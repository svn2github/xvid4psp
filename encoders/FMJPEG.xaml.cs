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
	public partial class FMJPEG
	{
        public Massive m;
        private Settings.EncodingModes oldmode;
        private VideoEncoding root_window;
        private MainWindow p;
        public enum CodecPresets { Default = 1, Turbo, Ultra, Extreme, Custom }

        public FMJPEG(Massive mass, VideoEncoding VideoEncWindow, MainWindow parent)
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
            //combo_mode.Items.Add("2-Pass Bitrate");
            //combo_mode.Items.Add("3-Pass Bitrate");
            combo_mode.Items.Add("Constant Quality");
            //combo_mode.Items.Add("2-Pass Quality");
            combo_mode.Items.Add("1-Pass Size");
            //combo_mode.Items.Add("3-Pass Size");

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

            if (m.encodingmode == Settings.EncodingModes.Quality ||
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
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
            {
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                     m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }
            else
            {
                text_bitrate.Content = Languages.Translate("Quantizer") + ": (Q)";
                num_bitrate.Value = (decimal)m.outvbitrate;
            }
                  
            check_bitexact.IsChecked = m.ffmpeg_options.bitexact;

            num_gopsize.Value = m.ffmpeg_options.gopsize;
            num_minbitrate.Value = m.ffmpeg_options.minbitrate;
            num_maxbitrate.Value = m.ffmpeg_options.maxbitrate;
            num_buffsize.Value = m.ffmpeg_options.buffsize;
            num_bittolerance.Value = m.ffmpeg_options.bittolerance;

            SetToolTips();
        }

        private void SetToolTips()
        {
            combo_mode.ToolTip = "Encoding mode";

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                 m.encodingmode == Settings.EncodingModes.TwoPass ||
                 m.encodingmode == Settings.EncodingModes.ThreePass)
                num_bitrate.ToolTip = "Set bitrate (Default: Auto)";
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                      m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
                num_bitrate.ToolTip = "Set file size (Default: InFileSize)";
            else
                num_bitrate.ToolTip = "Set target quality (Default: 3)";
            check_bitexact.ToolTip = "Use only bitexact stuff, except dct (Default: Disabled)";
            num_gopsize.ToolTip = "Set the group of picture size (Default: Auto)";
            num_minbitrate.ToolTip = "Set min video bitrate tolerance in kbps (Default: Auto)";
            num_maxbitrate.ToolTip = "Set max video bitrate tolerance in kbps (Default: Auto)";
            num_bittolerance.ToolTip = "Set video bitrate tolerance in kb (Default: Auto)";
            num_buffsize.ToolTip = "Set ratecontrol buffer size (Default: Auto)";
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
                    else if (m.vpasses.Count == 1)
                        mode = Settings.EncodingModes.OnePassSize;
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

                //дешифруем флаги
                if (value == "-flags")
                {
                    string flags_string = cli[n + 1];
                    string[] separator2 = new string[] { "+" };
                    string[] flags = flags_string.Split(separator2, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string flag in flags)
                    {
                        if (flag == "bitexact")
                            m.ffmpeg_options.bitexact = true;
                    }
                }

                if (value == "-g")
                    m.ffmpeg_options.gopsize = Convert.ToInt32(cli[n + 1]);

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
            string line = "-vcodec mjpeg -an ";

            //битрейты
            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
                line += "-b " + m.outvbitrate * 1000;
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
                line += "-sizemode " + m.outvbitrate * 1000;
            else
                line += "-qscale " + Calculate.ConvertDoubleToPointString((double)m.outvbitrate, 1);

            //создаём пустой массив -flags
            string flags = " -flags ";

            //контроль битрейтов
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

            if (m.ffmpeg_options.gopsize != 0)
                line += " -g " + m.ffmpeg_options.gopsize;

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
                m.encodingmode == Settings.EncodingModes.Quantizer ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
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

                else if (XviDmode == "1-Pass Size")
                    m.encodingmode = Settings.EncodingModes.OnePassSize;

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
                    oldmode != Settings.EncodingModes.ThreePassSize &&
                    oldmode != Settings.EncodingModes.OnePassSize)
                {
                    if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                        m.encodingmode == Settings.EncodingModes.OnePassSize)
                    {
                        SetDefaultBitrates();
                    }
                }

                root_window.UpdateOutSize();
                root_window.UpdateManualProfile();
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

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                m.outvbitrate = Calculate.GetAutoBitrate(m);
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Bitrate") + ": (kbps)";
            }

            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                m.outvbitrate = m.infilesizeint;
                num_bitrate.Value = (decimal)m.outvbitrate;
                text_bitrate.Content = Languages.Translate("Size") + ": (MB)";
            }

            SetToolTips();
        }

        private void check_bitexact_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_bitexact.IsFocused)
            {
                m.ffmpeg_options.bitexact = check_bitexact.IsChecked.Value;
                root_window.UpdateManualProfile();
            }
        }

        private void check_bitexact_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (check_bitexact.IsFocused)
            {
                m.ffmpeg_options.bitexact = check_bitexact.IsChecked.Value;
                root_window.UpdateManualProfile();
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
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
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
            }
        }

        private void num_minbitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_minbitrate.IsAction)
            {
                m.ffmpeg_options.minbitrate = (int)num_minbitrate.Value;

                root_window.UpdateManualProfile();
            }
        }

        private void num_maxbitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_maxbitrate.IsAction)
            {
                m.ffmpeg_options.maxbitrate = (int)num_maxbitrate.Value;

                root_window.UpdateManualProfile();
            }
        }

        private void num_gopsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_gopsize.IsAction)
            {
                m.ffmpeg_options.gopsize = (int)num_gopsize.Value;

                root_window.UpdateManualProfile();
            }
        }

        private void num_buffsize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_buffsize.IsAction)
            {
                m.ffmpeg_options.buffsize = (int)num_buffsize.Value;

                root_window.UpdateManualProfile();
            }
        }

        private void num_bittolerance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_bittolerance.IsAction)
            {
                m.ffmpeg_options.bittolerance = (int)num_bittolerance.Value;

                root_window.UpdateManualProfile();
            }
        }

	}
}