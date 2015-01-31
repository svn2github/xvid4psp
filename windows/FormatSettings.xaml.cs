using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections;

namespace XviD4PSP
{
    public partial class FormatSettings
    {
        public bool update_massive = false;
        public bool update_audio = false;
        public bool update_resolution = false;
        public bool update_framerate = false;
        private Format.ExportFormats format;
        private Formats def;

        //Undo
        private string undo_vcodecs;
        private string undo_acodecs;
        private string undo_aspects;
        private string undo_framerates;
        private string undo_samplerates;
        private string undo_extension;
        private string undo_split;
        private string undo_mux_v;
        private string undo_mux_a;
        private string undo_mux_o;

        public FormatSettings(Format.ExportFormats format, MainWindow parent)
        {
            this.InitializeComponent();
            this.Owner = parent;

            //Дефолты
            this.format = format;
            this.def = Formats.GetDefaults(format);

            //Включаем\выключаем редактирование
            //Video
            textbox_vcodecs.IsEnabled = def.VCodecs_IsEditable;
            textbox_framerates.IsEnabled = def.Framerates_IsEditable;
            combo_MinResolutionW.IsEnabled = combo_MinResolutionH.IsEnabled =
                combo_MidResolutionW.IsEnabled = combo_MidResolutionH.IsEnabled =
                combo_MaxResolutionW.IsEnabled = combo_MaxResolutionH.IsEnabled =
                combo_ValidModW.IsEnabled = combo_ValidModH.IsEnabled = def.Resolution_IsEditable;
            textbox_aspects.IsEnabled = def.Aspects_IsEditable;
            combo_fix_ar_method.IsEnabled = (def.LockedAR_Methods.Length > 1);
            check_anamorphic.IsEnabled = def.Anamorphic_IsEditable;
            check_interlaced.IsEnabled = def.Interlaced_IsEditable;

            //Audio
            textbox_acodecs.IsEnabled = def.ACodecs_IsEditable;
            textbox_samplerates.IsEnabled = def.Samplerates_IsEditable;
            check_stereo.IsEnabled = def.LimitedToStereo_IsEditable;

            //Muxing
            combo_Muxer.IsEnabled = (def.Muxers.Length > 1);
            combo_Extension.IsEnabled = (def.Extensions.Length > 1);
            combo_split.IsEnabled = (def.Splitting != "None");
            check_dont_mux.IsEnabled = def.DontMuxStreams_IsEditable;
            check_direct_encoding.IsEnabled = def.DirectEncoding_IsEditable;
            check_direct_remux.IsEnabled = def.DirectRemuxing_IsEditable;
            check_4gb_only.IsEnabled = def.LimitedTo4Gb_IsEditable;

            //Переводим фокус
            int active_tab = 0;
            if (int.TryParse(Settings.GetFormatPreset(format, "ActiveTab"), out active_tab))
            {
                if (active_tab == 2) tab_audio.IsSelected = true;
                else if (active_tab == 3) tab_muxing.IsSelected = true;
            }

            LoadSettings();
            TranslateItems();
            SetTooltips();

            //Предупреждение (один раз для каждого формата)
            if (format != Format.ExportFormats.Custom && !Convert.ToBoolean(Settings.GetFormatPreset(format, "was_warned")))
            {
                Message mes = new Message(parent);
                mes.ShowMessage(Languages.Translate("Some options or their combinations may not work as you expect!") + "\r\n" +
                    Languages.Translate("After making any changes the format may become completely broken!"), Languages.Translate("Warning"), Message.MessageStyle.Ok);
                Settings.SetFormatPreset(format, "was_warned", bool.TrueString);
            }

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        //Блокировка изменения высоты окна вручную..
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!double.IsNaN(this.Height))
            {
                this.Height = e.PreviousSize.Height;
                this.Height = double.NaN;
            }
        }

        //.. с сохранением возможности её автоустановки
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded && e.Source == tabs)
            {
                this.Height = double.NaN;
                this.SizeToContent = SizeToContent.Height;
            }
        }

        private void LoadSettings()
        {
            textbox_vcodecs.Text = undo_vcodecs = StringArrayToString((textbox_vcodecs.IsEnabled) ? Formats.GetSettings(format, "VCodecs", def.VCodecs) : def.VCodecs);
            textbox_acodecs.Text = undo_acodecs = StringArrayToString((textbox_acodecs.IsEnabled) ? Formats.GetSettings(format, "ACodecs", def.ACodecs) : def.ACodecs);
            textbox_aspects.Text = undo_aspects = StringArrayToString((textbox_aspects.IsEnabled) ? Formats.GetSettings(format, "Aspects", def.Aspects) : def.Aspects);
            textbox_framerates.Text = undo_framerates = StringArrayToString((textbox_framerates.IsEnabled) ? Formats.GetSettings(format, "Framerates", def.Framerates) : def.Framerates);
            textbox_samplerates.Text = undo_samplerates = StringArrayToString((textbox_samplerates.IsEnabled) ? Formats.GetSettings(format, "Samplerates", def.Samplerates) : def.Samplerates);

            //Фиксированный аспект
            combo_fix_ar_method.Items.Clear();
            if (combo_fix_ar_method.IsEnabled)
            {
                foreach (string fixes in def.LockedAR_Methods)
                    combo_fix_ar_method.Items.Add(fixes);

                string fix_ar = Formats.GetSettings(format, "LockedAR_Method", def.LockedAR_Method);
                combo_fix_ar_method.SelectedItem = (combo_fix_ar_method.Items.Contains(fix_ar)) ? fix_ar : def.LockedAR_Method;
            }
            else
            {
                combo_fix_ar_method.Items.Add(def.LockedAR_Method);
                combo_fix_ar_method.SelectedIndex = 0;
            }

            //Муксер
            combo_Muxer.Items.Clear();
            if (combo_Muxer.IsEnabled)
            {
                foreach (string muxers in def.Muxers)
                    combo_Muxer.Items.Add(muxers);

                string muxer = Formats.GetSettings(format, "Muxer", def.Muxer).ToLower();
                combo_Muxer.SelectedItem = (combo_Muxer.Items.Contains(muxer)) ? muxer : def.Muxer;
            }
            else
            {
                combo_Muxer.Items.Add(def.Muxer);
                combo_Muxer.SelectedIndex = 0;
            }

            //Параметры муксинга
            LoadMuxCLI();

            //Расширение
            combo_Extension.Items.Clear();
            if (combo_Extension.IsEnabled)
            {
                bool any = false;
                foreach (string exts in def.Extensions)
                {
                    if (!any && exts == "*") { any = true; combo_Extension.Items.Add(""); }
                    else combo_Extension.Items.Add(exts);
                }

                undo_extension = Formats.GetSettings(format, "Extension", def.Extension).ToLower();
                if (!combo_Extension.Items.Contains(undo_extension))
                {
                    if (any) combo_Extension.Items.Add(undo_extension);
                    else undo_extension = def.Extension;
                }
                combo_Extension.SelectedItem = undo_extension;
            }
            else
            {
                combo_Extension.Items.Add(def.Extension);
                combo_Extension.SelectedIndex = 0;
            }

            //Деление файла
            combo_split.Items.Clear();
            if (def.Splitting != "None")
            {
                combo_split.Items.Add("");
                combo_split.Items.Add("Disabled");
                combo_split.Items.Add("210Mb");
                combo_split.Items.Add("350Mb");
                combo_split.Items.Add("650Mb CD");
                combo_split.Items.Add("700Mb CD");
                combo_split.Items.Add("800Mb CD");
                combo_split.Items.Add("1000Mb ISO");
                combo_split.Items.Add("4000Mb FAT32");
                combo_split.Items.Add("4483Mb DVD5");
                combo_split.Items.Add("4700Mb DVD5");
                combo_split.Items.Add("8142Mb DVD9");
                combo_split.Items.Add("8500Mb DVD9");
                undo_split = Formats.GetSettings(format, "Splitting", def.Splitting);
                if (!combo_split.Items.Contains(undo_split)) combo_split.Items.Add(undo_split);
                combo_split.SelectedItem = undo_split;
            }
            else
            {
                combo_split.Items.Add("Disabled");
                combo_split.SelectedIndex = 0;
            }

            //Создание картинок
            combo_thm_format.Items.Clear();
            combo_thm_format.Items.Add("None");
            combo_thm_format.Items.Add("JPG");
            combo_thm_format.Items.Add("PNG");
            combo_thm_format.Items.Add("BMP");
            string thm_format = Formats.GetSettings(format, "THM_Format", def.THM_Format);
            if (!combo_thm_format.Items.Contains(thm_format)) combo_thm_format.SelectedItem = def.THM_Format;
            else combo_thm_format.SelectedItem = thm_format;

            //Ширина картинки
            combo_thm_W.Items.Clear();
            combo_thm_W.Items.Add(0);
            for (int i = 120; i < 2000; i += 4) combo_thm_W.Items.Add(i);
            combo_thm_W.SelectedItem = Formats.GetSettings(format, "THM_Width", def.THM_Width);

            //Высота картинки
            combo_thm_H.Items.Clear();
            combo_thm_H.Items.Add(0);
            for (int i = 60; i < 2000; i += 2) combo_thm_H.Items.Add(i);
            combo_thm_H.SelectedItem = Formats.GetSettings(format, "THM_Height", def.THM_Height);

            //Разрешение
            LoadResolutions();

            //Кратность сторон
            combo_ValidModW.Items.Clear();
            combo_ValidModH.Items.Clear();
            if (def.Resolution_IsEditable)
            {
                //Кратность для ширины
                for (int w = 2; w <= 16; w *= 2)
                    combo_ValidModW.Items.Add(w);
                int modw = Format.GetValidModW(format);
                if (!combo_ValidModW.Items.Contains(modw))
                    combo_ValidModW.Items.Add(modw);
                combo_ValidModW.SelectedItem = modw;

                //Кратность для высоты
                for (int h = 2; h <= 16; h *= 2)
                    combo_ValidModH.Items.Add(h);
                int modh = Format.GetValidModH(format);
                if (!combo_ValidModH.Items.Contains(modh))
                    combo_ValidModH.Items.Add(modh);
                combo_ValidModH.SelectedItem = modh;
            }
            else
            {
                combo_ValidModW.Items.Add(def.ModW);
                combo_ValidModH.Items.Add(def.ModH);
                combo_ValidModW.SelectedIndex = 0;
                combo_ValidModH.SelectedIndex = 0;
            }

            check_anamorphic.IsChecked = (check_anamorphic.IsEnabled) ? Formats.GetSettings(format, "Anamorphic", def.Anamorphic) : def.Anamorphic;
            check_interlaced.IsChecked = (check_interlaced.IsEnabled) ? Formats.GetSettings(format, "Interlaced", def.Interlaced) : def.Interlaced;
            check_stereo.IsChecked = (check_stereo.IsEnabled) ? Formats.GetSettings(format, "LimitedToStereo", def.LimitedToStereo) : def.LimitedToStereo;
            check_thm_fix_ar.IsChecked = (check_thm_fix_ar.IsEnabled) ? Formats.GetSettings(format, "THM_FixAR", def.THM_FixAR) : def.THM_FixAR;
            check_dont_mux.IsChecked = (check_dont_mux.IsEnabled) ? Formats.GetSettings(format, "DontMuxStreams", def.DontMuxStreams) : def.DontMuxStreams;
            check_direct_encoding.IsChecked = (check_direct_encoding.IsEnabled) ? Formats.GetSettings(format, "DirectEncoding", def.DirectEncoding) : def.DirectEncoding;
            check_direct_remux.IsChecked = (check_direct_remux.IsEnabled) ? Formats.GetSettings(format, "DirectRemuxing", def.DirectRemuxing) : def.DirectRemuxing;
            check_4gb_only.IsChecked = (check_4gb_only.IsEnabled) ? Formats.GetSettings(format, "LimitedTo4Gb", def.LimitedTo4Gb) : def.LimitedTo4Gb;
        }

        private string StringArrayToString(string[] input)
        {
            int pos = 0;
            string output = "";
            foreach (string line in input)
            {
                pos += 1;
                output += line;
                if (pos < input.Length) output += ", ";
            }

            return output;
        }

        private void LoadMuxCLI()
        {
            textbox_mux_v.Clear();
            textbox_mux_a.Clear();
            textbox_mux_o.Clear();
            undo_mux_v = undo_mux_a = undo_mux_o = "";
            combo_split.IsEnabled = false;

            string temp = null;
            string muxer_cli = "";
            string empty = Languages.Translate("(empty)");
            string _def = Languages.Translate("Default") + ": ";
            string info = Languages.Translate("Please refer to MUXER documentation for more info") + ".\r\n";
            string wcards = Languages.Translate("You can use a wildcards") + ":\r\n\r\n";
            string common = "%task% - " + Languages.Translate("task number") +
                    "\r\n%temp% - " + Languages.Translate("temp folder path (like \"C:\\Temp\")") +
                    "\r\n%in_path% %out_path% - " + Languages.Translate("input/output file folder (like \"C:\\Video\")") +
                    "\r\n%in_name% %out_name% - " + Languages.Translate("input/output file name (like \"my_video\")") +
                    "\r\n%in_ext% %out_ext% - " + Languages.Translate("input/output file extension (like \".mp4\")") +
                    "\r\n%lang% - " + Languages.Translate("audio stream language (\"Undetermined\" if not available)") + "\r\n\r\n";

            if (combo_Muxer.SelectedItem.ToString() == "ffmpeg")
            {
                muxer_cli = Formats.GetSettings(format, "CLI_ffmpeg", def.CLI_ffmpeg);
                textbox_mux_v.IsEnabled = textbox_mux_a.IsEnabled = textbox_mux_o.IsEnabled = true;

                info = info.Replace("MUXER", "FFmpeg");
                textbox_mux_v.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", def.CLI_ffmpeg))) ? empty : temp);
                textbox_mux_a.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", def.CLI_ffmpeg))) ? empty : temp);
                textbox_mux_o.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", def.CLI_ffmpeg))) ? empty : temp);
            }
            else if (combo_Muxer.SelectedItem.ToString() == "mkvmerge")
            {
                muxer_cli = Formats.GetSettings(format, "CLI_mkvmerge", def.CLI_mkvmerge);
                textbox_mux_v.IsEnabled = textbox_mux_a.IsEnabled = textbox_mux_o.IsEnabled = true;
                combo_split.IsEnabled = true;

                info = info.Replace("MUXER", "MKVMerge");
                string video = "%v_id% - " + Languages.Translate("video track ID (TID, track number)") + "\r\n";
                string audio = "%a_id% - " + Languages.Translate("audio track ID (TID, track number)") + "\r\n";

                textbox_mux_v.ToolTip = info + wcards + video + "\r\n" + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", def.CLI_mkvmerge))) ? empty : temp);
                textbox_mux_a.ToolTip = info + wcards + audio + "\r\n" + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", def.CLI_mkvmerge))) ? empty : temp);
                textbox_mux_o.ToolTip = info + wcards + video + audio + "\r\n" + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", def.CLI_mkvmerge))) ? empty : temp);
            }
            else if (combo_Muxer.SelectedItem.ToString() == "mp4box")
            {
                muxer_cli = Formats.GetSettings(format, "CLI_mp4box", def.CLI_mp4box);
                textbox_mux_v.IsEnabled = textbox_mux_a.IsEnabled = textbox_mux_o.IsEnabled = true;
                combo_split.IsEnabled = true;

                info = info.Replace("MUXER", "MP4Box");
                textbox_mux_v.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", def.CLI_mp4box))) ? empty : temp);
                textbox_mux_a.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", def.CLI_mp4box))) ? empty : temp);
                textbox_mux_o.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", def.CLI_mp4box))) ? empty : temp);
            }
            else if (combo_Muxer.SelectedItem.ToString() == "tsmuxer")
            {
                muxer_cli = Formats.GetSettings(format, "CLI_tsmuxer", def.CLI_tsmuxer);
                textbox_mux_v.IsEnabled = textbox_mux_a.IsEnabled = textbox_mux_o.IsEnabled = true;
                combo_split.IsEnabled = true;

                info = info.Replace("MUXER", "tsMuxeR");
                textbox_mux_v.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", def.CLI_tsmuxer))) ? empty : temp);
                textbox_mux_a.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", def.CLI_tsmuxer))) ? empty : temp);
                textbox_mux_o.ToolTip = info + wcards + common + _def + ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", def.CLI_tsmuxer))) ? empty : temp);
            }
            else if (combo_Muxer.SelectedItem.ToString() == "virtualdubmod")
            {
                muxer_cli = Formats.GetSettings(format, "CLI_virtualdubmod", def.CLI_virtualdubmod);
                textbox_mux_v.IsEnabled = textbox_mux_a.IsEnabled = textbox_mux_o.IsEnabled = true;

                string opts = Languages.Translate("Available options") + ":\r\n\r\n";
                string text = Languages.Translate("some text");

                textbox_mux_v.ToolTip = opts + "title=\"" + text + "\"\r\nauthor=\"" + text + "\"\r\ncopyright=\"" + text + "\"\r\n\r\n" + wcards + common + _def +
                    ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", def.CLI_virtualdubmod))) ? empty : temp);
                textbox_mux_a.ToolTip = opts + "title=\"" + text + "\"\r\nlanguage=\"" + text + "\"\r\n\r\n" + wcards + common + _def +
                    ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", def.CLI_virtualdubmod))) ? empty : temp);
                textbox_mux_o.ToolTip = opts + "interleave=\"1, 500, 1, 0\"\r\n" + Languages.Translate("1 - interleaving is enabled, 0 - disabled") + "\r\n" +
                    Languages.Translate("500 - preload (ms)") + "\r\n" + Languages.Translate("1 - interleaving interval (ms or frames, see below)") + "\r\n" +
                    Languages.Translate("0 - interval is in frames, 1 - in ms") + "\r\n\r\n" + _def +
                    ((string.IsNullOrEmpty(temp = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", def.CLI_virtualdubmod))) ? empty : temp);
            }
            else
            {
                textbox_mux_v.IsEnabled = false;
                textbox_mux_a.IsEnabled = false;
                textbox_mux_o.IsEnabled = false;
                return;
            }

            if (!string.IsNullOrEmpty(muxer_cli))
            {
                textbox_mux_v.Text = undo_mux_v = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", muxer_cli);
                textbox_mux_a.Text = undo_mux_a = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", muxer_cli);
                textbox_mux_o.Text = undo_mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);
            }
        }

        private void LoadResolutions()
        {
            combo_MinResolutionW.Items.Clear();
            combo_MinResolutionH.Items.Clear();
            combo_MidResolutionW.Items.Clear();
            combo_MidResolutionH.Items.Clear();
            combo_MaxResolutionW.Items.Clear();
            combo_MaxResolutionH.Items.Clear();

            if (def.Resolution_IsEditable)
            {
                //Ширина
                int n = 16, value;
                int step = Format.GetValidModW(format);
                while (n < 7680 + step)
                {
                    combo_MinResolutionW.Items.Add(n);
                    combo_MidResolutionW.Items.Add(n);
                    combo_MaxResolutionW.Items.Add(n);
                    n = n + step;
                }
                value = Formats.GetSettings(format, "MinW", def.MinW);
                if (!combo_MinResolutionW.Items.Contains(value)) combo_MinResolutionW.Items.Add(value);
                combo_MinResolutionW.SelectedItem = value;
                value = Formats.GetSettings(format, "MidW", def.MidW);
                if (!combo_MidResolutionW.Items.Contains(value)) combo_MidResolutionW.Items.Add(value);
                combo_MidResolutionW.SelectedItem = value;
                value = Formats.GetSettings(format, "MaxW", def.MaxW);
                if (!combo_MaxResolutionW.Items.Contains(value)) combo_MaxResolutionW.Items.Add(value);
                combo_MaxResolutionW.SelectedItem = value;

                //Высота
                n = 16;
                step = Format.GetValidModH(format);
                while (n < 4320 + step)
                {
                    combo_MinResolutionH.Items.Add(n);
                    combo_MidResolutionH.Items.Add(n);
                    combo_MaxResolutionH.Items.Add(n);
                    n = n + step;
                }
                value = Formats.GetSettings(format, "MinH", def.MinH);
                if (!combo_MinResolutionH.Items.Contains(value)) combo_MinResolutionH.Items.Add(value);
                combo_MinResolutionH.SelectedItem = value;
                value = Formats.GetSettings(format, "MidH", def.MidH);
                if (!combo_MidResolutionH.Items.Contains(value)) combo_MidResolutionH.Items.Add(value);
                combo_MidResolutionH.SelectedItem = value;
                value = Formats.GetSettings(format, "MaxH", def.MaxH);
                if (!combo_MaxResolutionH.Items.Contains(value)) combo_MaxResolutionH.Items.Add(value);
                combo_MaxResolutionH.SelectedItem = value;
            }
            else
            {
                combo_MinResolutionW.Items.Add(def.MinW);
                combo_MinResolutionW.SelectedIndex = 0;
                combo_MinResolutionH.Items.Add(def.MinH);
                combo_MinResolutionH.SelectedIndex = 0;
                combo_MidResolutionW.Items.Add(def.MidW);
                combo_MidResolutionW.SelectedIndex = 0;
                combo_MidResolutionH.Items.Add(def.MidH);
                combo_MidResolutionH.SelectedIndex = 0;
                combo_MaxResolutionW.Items.Add(def.MaxW);
                combo_MaxResolutionW.SelectedIndex = 0;
                combo_MaxResolutionH.Items.Add(def.MaxH);
                combo_MaxResolutionH.SelectedIndex = 0;
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Сохраняем активную вкладку (в реестре..)
            if (tab_audio.IsSelected) Settings.SetFormatPreset(format, "ActiveTab", "2");
            else if (tab_muxing.IsSelected) Settings.SetFormatPreset(format, "ActiveTab", "3");
            else Settings.SetFormatPreset(format, "ActiveTab", "1");
        }

        private void StoreValue(Format.ExportFormats format, string key, string value)
        {
            try
            {
                Formats.SetSettings(format, key, value);
            }
            catch (Exception ex)
            {
                ErrorException("StoreValue: " + ex.Message, ex.StackTrace);
            }
        }

        private void TranslateItems()
        {
            button_reset.Content = Languages.Translate("Reset");
            button_ok.Content = Languages.Translate("OK");
            Title = Languages.Translate("Edit format") + " (" + Format.EnumToString(format) + ")";
            group_video.Header = Languages.Translate("Video options");
            group_audio.Header = Languages.Translate("Audio options");
            group_muxing.Header = Languages.Translate("Multiplexing");

            vcodecslist.Content = Languages.Translate("Valid video codecs:");
            framerateslist.Content = Languages.Translate("Valid framerates:");
            fixed_ar.Content = Languages.Translate("Fix AR") + ":";
            check_anamorphic.Content = Languages.Translate("Anamorph is allowed");
            check_interlaced.Content = Languages.Translate("Interlace is allowed");
            acodecslist.Content = Languages.Translate("Valid audio codecs:");
            check_stereo.Content = Languages.Translate("Limited to Stereo");
            samplerateslist.Content = Languages.Translate("Valid samplerates:");
            validmuxer.Content = Languages.Translate("Muxer for this format:");
            validextension.Content = Languages.Translate("File extension:");
            mux_v.Content = Languages.Translate("Video options") + ":";
            mux_a.Content = Languages.Translate("Audio options") + ":";
            mux_o.Content = Languages.Translate("Global options") + ":";
            split.Content = Languages.Translate("Split output file:");
            thm.Content = Languages.Translate("Create THM:");
            thm_size.Content = Languages.Translate("Resolution:");
            check_dont_mux.Content = Languages.Translate("Don`t multiplex video and audio streams");
            check_direct_encoding.Content = Languages.Translate("Use direct encoding if possible");
            check_direct_remux.Content = Languages.Translate("Use direct remuxing if possible");
            check_4gb_only.Content = Languages.Translate("Maximum file size is 4Gb");
        }

        private void SetTooltips()
        {
            string _on = Languages.Translate("On");
            string _off = Languages.Translate("Off");
            string _def = "\r\n\r\n" + Languages.Translate("Default") + ": ";

            button_reset.ToolTip = Languages.Translate("Reset all settings");
            if (textbox_vcodecs.IsEnabled) textbox_vcodecs.ToolTip = Languages.Translate("Codecs, that will be selectable in the video-codecs settings window.") + "\r\n" + Languages.Translate("Valid values:") +
                " x265, x264, x262, MPEG1, MPEG2, MPEG4, FLV1, MJPEG, HUFF, FFV1, XviD, DV\r\n" + Languages.Translate("Separate by comma.") + _def + StringArrayToString(def.VCodecs);
            if (textbox_framerates.IsEnabled) textbox_framerates.ToolTip = Languages.Translate("Framerates, that can be set for this format.") + "\r\n" + Languages.Translate("Valid values:") +
                " 0.000 (" + Languages.Translate("means \"any\"") + "), 15.000, 18.000, 20.000, 23.976, 24.000, 25.000, 29.970, 30.000, 50.000, 59.940, 60.000, 120.000, ...\r\n" + Languages.Translate("Separate by comma.") +
                _def + StringArrayToString(def.Framerates);
            combo_thm_W.ToolTip = Languages.Translate("Width");
            combo_thm_H.ToolTip = Languages.Translate("Height");
            if (def.Resolution_IsEditable)
            {
                combo_MinResolutionW.ToolTip = combo_thm_W.ToolTip + " (" + Languages.Translate("Minimum").ToLower() + ")" + _def + def.MinW;
                combo_MinResolutionH.ToolTip = combo_thm_H.ToolTip + " (" + Languages.Translate("Minimum").ToLower() + ")" + _def + def.MinH;
                combo_MidResolutionW.ToolTip = combo_thm_W.ToolTip + " (" + Languages.Translate("limited maximum, for auto selection") + ")" + _def + def.MidW;
                combo_MidResolutionH.ToolTip = combo_thm_H.ToolTip + " (" + Languages.Translate("limited maximum, for auto selection") + ")" + _def + def.MidH;
                combo_MaxResolutionW.ToolTip = combo_thm_W.ToolTip + " (" + Languages.Translate("Maximum").ToLower() + ")" + _def + def.MaxW;
                combo_MaxResolutionH.ToolTip = combo_thm_H.ToolTip + " (" + Languages.Translate("Maximum").ToLower() + ")" + _def + def.MaxH;
                combo_ValidModW.ToolTip = combo_thm_W.ToolTip + " (" + Languages.Translate("multiplier") + ")\r\n" + Languages.Translate("Values XX are strongly NOT recommended!").Replace("XX", "2, 4, 8") + _def + def.ModW;
                combo_ValidModH.ToolTip = combo_thm_H.ToolTip + " (" + Languages.Translate("multiplier") + ")\r\n" + Languages.Translate("Values XX are strongly NOT recommended!").Replace("XX", "2, 4") + _def + def.ModH;
            }
            if (textbox_aspects.IsEnabled) textbox_aspects.ToolTip = Languages.Translate("Aspect ratios.") + "\r\n" + Languages.Translate("Valid values:") + " 1.3333 (4:3), 1.5000, 1.6667, 1.7647 (16:9), 1.7778 (16:9), 1.8500, 2.3529, ...\r\n" +
                Languages.Translate("Separate by comma.") + _def + StringArrayToString(def.Aspects);
            if (combo_fix_ar_method.IsEnabled) combo_fix_ar_method.ToolTip = Languages.Translate("Use this option if you want to limit AR by values, specified above") + ":\r\n\r\n" +
                "Disabled - " + Languages.Translate("do not fix") + "\r\nSAR - " + Languages.Translate("fix AR using anamorphic encoding") + " " + Languages.Translate("(which must be allowed!)") + "\r\nCrop - " +
                Languages.Translate("fix AR by cropping the picture") + "\r\nBlack - " + Languages.Translate("fix AR by adding a black borders") + "\r\n\r\n" +
                Languages.Translate("Note: for non-anamorphic encoding you must also limit the resolution (min, limit and max) with a single value,") + "\r\n" +
                Languages.Translate("the quotient from which is as close as possible to the desired AR (for example, 640x480 for 1.3333).") + _def + def.LockedAR_Method;
            if (check_anamorphic.IsEnabled) check_anamorphic.ToolTip = Languages.Translate("Enable this option if you want to allow anamorphic encoding for this format") + _def + " " + (def.Anamorphic ? _on : _off);
            if (check_interlaced.IsEnabled) check_interlaced.ToolTip = Languages.Translate("Enable this option if you want to allow interlaced encoding for this format") + _def + " " + (def.Interlaced ? _on : _off);
            if (textbox_acodecs.IsEnabled) textbox_acodecs.ToolTip = Languages.Translate("Codecs, that will be selectable in the audio-codecs settings window.") + "\r\n" + Languages.Translate("Valid values:") +
                " PCM, FLAC, AAC, QAAC, MP2, MP3, AC3\r\n" + Languages.Translate("Separate by comma.") + _def + StringArrayToString(def.ACodecs);
            if (textbox_samplerates.IsEnabled) textbox_samplerates.ToolTip = Languages.Translate("Samplerates, that can be set for this format.") + "\r\n" + Languages.Translate("Valid values:") +
                " Auto | 22050, 32000, 44100, 48000, 96000, 192000, ...\r\n" + Languages.Translate("Separate by comma.") + _def + StringArrayToString(def.Samplerates);
            if (check_stereo.IsEnabled) check_stereo.ToolTip = Languages.Translate("Maximum numbers of audio channels for this format is 2") + _def + " " + (def.LimitedToStereo ? _on : _off);
            if (combo_Muxer.IsEnabled) combo_Muxer.ToolTip = Languages.Translate("Use this muxer for multiplexing the streams") + ((format == Format.ExportFormats.Custom) ? ".\r\n" + Languages.Translate("Supported formats") +
                ":\r\n\r\nvirtualdubmod - avi\r\nffmpeg - all formats\r\nmkvmerge - mkv, webm\r\nmp4box - 3gp, mp4, m4v, mov\r\ntsmuxer - ts, m2ts\r\npmpavc - pmp\r\ndpgmuxer - dpg" : "") + _def + def.Muxer;
            if (combo_Extension.IsEnabled) combo_Extension.ToolTip = combo_Extension.Tag = Languages.Translate("Use this extension") + _def + def.Extension;
            if (def.Splitting != "None") combo_split.ToolTip = combo_split.Tag = Languages.Translate("Only for this muxers:") + " mkvmerge, mp4box, tsmuxer" + _def + def.Splitting;
            if (combo_thm_format.IsEnabled)
            {
                combo_thm_format.ToolTip = Languages.Translate("Save THM (thumbnail picture) in this format") + _def + def.THM_Format;
                check_thm_fix_ar.ToolTip = Languages.Translate("Preserve AR by cropping the image") + _def + " " + (def.THM_FixAR ? _on : _off);
                combo_thm_W.ToolTip += " (" + Languages.Translate("0 - encoding size") + ")" + _def + def.THM_Width;
                combo_thm_H.ToolTip += " (" + Languages.Translate("0 - encoding size") + ")" + _def + def.THM_Height;
            }
            if (check_dont_mux.IsEnabled) check_dont_mux.ToolTip = Languages.Translate("Encode video and audio streams in a separate files (without muxing)") + _def + " " + (def.DontMuxStreams ? _on : _off);
            if (check_direct_encoding.IsEnabled) check_direct_encoding.ToolTip = Languages.Translate("If possible, encode the streams directly to the output file without any intermediate files") + _def + " " + (def.DirectEncoding ? _on : _off);
            if (check_direct_remux.IsEnabled) check_direct_remux.ToolTip = Languages.Translate("If possible, copy the streams directly from the source file without demuxing them (Copy mode)") + _def + " " + (def.DirectRemuxing ? _on : _off);
            if (check_4gb_only.IsEnabled) check_4gb_only.ToolTip = Languages.Translate("This option will warn you if estimated file size exceeds 4Gb") + _def + " " + (def.LimitedTo4Gb ? _on : _off);
        }

        private void ErrorException(string message, string info)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, info, Languages.Translate("Error"));
        }

        private void combo_Muxer_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_Muxer.IsDropDownOpen || combo_Muxer.IsSelectionBoxHighlighted) && combo_Muxer.SelectedItem != null)
            {
                LoadMuxCLI();
                StoreValue(format, "Muxer", combo_Muxer.SelectedItem.ToString());
            }
        }

        private void combo_Extension_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_Extension.IsDropDownOpen || combo_Extension.IsSelectionBoxHighlighted || combo_Extension.IsEditable) && combo_Extension.SelectedItem != null)
            {
                if (EnableEditing(combo_Extension)) return;
                else DisableEditing(combo_Extension);

                undo_extension = combo_Extension.SelectedItem.ToString();
                StoreValue(format, "Extension", undo_extension);
                update_massive = true;
            }
        }

        private bool EnableEditing(ComboBox box)
        {
            //Включаем редактирование
            if (!box.IsEditable && box.SelectedItem != null && box.SelectedItem.ToString().Length == 0)
            {
                box.IsEditable = true;
                box.ToolTip = Languages.Translate("Enter - apply, Esc - cancel.");
                box.ApplyTemplate();
                return true;
            }
            return false;
        }

        private void DisableEditing(ComboBox box)
        {
            //Выключаем редактирование
            if (box.IsEditable)
            {
                box.IsEditable = false;
                box.ToolTip = box.Tag;
            }
        }

        private void UndoEdit(ComboBox box)
        {
            //Возвращаем исходное значение
            if (box == combo_Extension) box.SelectedItem = undo_extension;
            else if (box == combo_split) box.SelectedItem = undo_split;
        }

        private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            if (box.IsEditable && box.SelectedItem != null && !box.IsDropDownOpen && !box.IsMouseCaptured)
                ComboBox_KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter));
        }

        private void ComboBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                //Проверяем введённый текст
                ComboBox box = (ComboBox)sender;
                string text = box.Text.Trim();
                if (text == "") { UndoEdit(box); return; }
                else if (box == combo_split && Calculate.GetRegexValue(@"^(\d+)\D*", text) == null) { UndoEdit(box); return; }
                else if (box == combo_Extension && Calculate.GetRegexValue(@"^(\w+)$", (text = text.ToLower())) == null) { UndoEdit(box); return; }

                //Добавляем и выбираем Item
                if (!box.Items.Contains(text)) box.Items.Add(text);
                box.SelectedItem = text;
            }
            else if (e.Key == Key.Escape)
            {
                //Возвращаем исходное значение
                UndoEdit((ComboBox)sender);
            }
        }

        private void combo_MinResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_MinResolutionW.IsDropDownOpen || combo_MinResolutionW.IsSelectionBoxHighlighted) &&
                combo_MinResolutionW.SelectedItem != null && combo_MidResolutionW.SelectedItem != null && combo_MaxResolutionW.SelectedItem != null)
            {
                if (Convert.ToInt32(combo_MinResolutionW.SelectedItem) > Convert.ToInt32(combo_MidResolutionW.SelectedItem))
                    combo_MinResolutionW.SelectedItem = combo_MidResolutionW.SelectedItem;
                else if (Convert.ToInt32(combo_MinResolutionW.SelectedItem) > Convert.ToInt32(combo_MaxResolutionW.SelectedItem))
                    combo_MinResolutionW.SelectedItem = combo_MaxResolutionW.SelectedItem;

                StoreValue(format, "MinW", combo_MinResolutionW.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MinResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_MinResolutionH.IsDropDownOpen || combo_MinResolutionH.IsSelectionBoxHighlighted) &&
                combo_MinResolutionH.SelectedItem != null && combo_MidResolutionH.SelectedItem != null && combo_MaxResolutionH.SelectedItem != null)
            {
                if (Convert.ToInt32(combo_MinResolutionH.SelectedItem) > Convert.ToInt32(combo_MidResolutionH.SelectedItem))
                    combo_MinResolutionH.SelectedItem = combo_MidResolutionH.SelectedItem;
                else if (Convert.ToInt32(combo_MinResolutionH.SelectedItem) > Convert.ToInt32(combo_MaxResolutionH.SelectedItem))
                    combo_MinResolutionH.SelectedItem = combo_MaxResolutionH.SelectedItem;

                StoreValue(format, "MinH", combo_MinResolutionH.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MidResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_MidResolutionW.IsDropDownOpen || combo_MidResolutionW.IsSelectionBoxHighlighted) &&
                combo_MidResolutionW.SelectedItem != null && combo_MinResolutionW.SelectedItem != null && combo_MaxResolutionW.SelectedItem != null)
            {
                if (Convert.ToInt32(combo_MidResolutionW.SelectedItem) < Convert.ToInt32(combo_MinResolutionW.SelectedItem))
                    combo_MidResolutionW.SelectedItem = combo_MinResolutionW.SelectedItem;
                else if (Convert.ToInt32(combo_MidResolutionW.SelectedItem) > Convert.ToInt32(combo_MaxResolutionW.SelectedItem))
                    combo_MidResolutionW.SelectedItem = combo_MaxResolutionW.SelectedItem;

                StoreValue(format, "MidW", combo_MidResolutionW.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MidResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_MidResolutionH.IsDropDownOpen || combo_MidResolutionH.IsSelectionBoxHighlighted) &&
                combo_MidResolutionH.SelectedItem != null && combo_MinResolutionH.SelectedItem != null && combo_MaxResolutionH.SelectedItem != null)
            {
                if (Convert.ToInt32(combo_MidResolutionH.SelectedItem) < Convert.ToInt32(combo_MinResolutionH.SelectedItem))
                    combo_MidResolutionH.SelectedItem = combo_MinResolutionH.SelectedItem;
                else if (Convert.ToInt32(combo_MidResolutionH.SelectedItem) > Convert.ToInt32(combo_MaxResolutionH.SelectedItem))
                    combo_MidResolutionH.SelectedItem = combo_MaxResolutionH.SelectedItem;

                StoreValue(format, "MidH", combo_MidResolutionH.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MaxResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_MaxResolutionW.IsDropDownOpen || combo_MaxResolutionW.IsSelectionBoxHighlighted) &&
                combo_MaxResolutionW.SelectedItem != null && combo_MidResolutionW.SelectedItem != null && combo_MinResolutionW.SelectedItem != null)
            {
                if (Convert.ToInt32(combo_MaxResolutionW.SelectedItem) < Convert.ToInt32(combo_MidResolutionW.SelectedItem))
                    combo_MaxResolutionW.SelectedItem = combo_MidResolutionW.SelectedItem;
                else if (Convert.ToInt32(combo_MaxResolutionW.SelectedItem) < Convert.ToInt32(combo_MinResolutionW.SelectedItem))
                    combo_MaxResolutionW.SelectedItem = combo_MinResolutionW.SelectedItem;

                StoreValue(format, "MaxW", combo_MaxResolutionW.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MaxResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_MaxResolutionH.IsDropDownOpen || combo_MaxResolutionH.IsSelectionBoxHighlighted) &&
                combo_MaxResolutionH.SelectedItem != null && combo_MidResolutionH.SelectedItem != null && combo_MinResolutionH.SelectedItem != null)
            {
                if (Convert.ToInt32(combo_MaxResolutionH.SelectedItem) < Convert.ToInt32(combo_MidResolutionH.SelectedItem))
                    combo_MaxResolutionH.SelectedItem = combo_MidResolutionH.SelectedItem;
                else if (Convert.ToInt32(combo_MaxResolutionH.SelectedItem) < Convert.ToInt32(combo_MinResolutionH.SelectedItem))
                    combo_MaxResolutionH.SelectedItem = combo_MinResolutionH.SelectedItem;

                StoreValue(format, "MaxH", combo_MaxResolutionH.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_ValidModW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_ValidModW.IsDropDownOpen || combo_ValidModW.IsSelectionBoxHighlighted) && combo_ValidModW.SelectedItem != null)
            {
                StoreValue(format, "ModW", combo_ValidModW.SelectedItem.ToString());
                LoadResolutions();
                update_resolution = true;
            }
        }

        private void combo_ValidModH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_ValidModH.IsDropDownOpen || combo_ValidModH.IsSelectionBoxHighlighted) && combo_ValidModH.SelectedItem != null)
            {
                StoreValue(format, "ModH", combo_ValidModH.SelectedItem.ToString());
                LoadResolutions();
                update_resolution = true;
            }
        }

        private void combo_fix_ar_method_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_fix_ar_method.IsDropDownOpen || combo_fix_ar_method.IsSelectionBoxHighlighted) && combo_fix_ar_method.SelectedItem != null)
            {
                string value = combo_fix_ar_method.SelectedItem.ToString();
                StoreValue(format, "LockedAR_Method", value);
                update_resolution = true;
            }
        }

        private void check_anamorphic_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "Anamorphic", check_anamorphic.IsChecked.Value.ToString());
            update_resolution = true;
        }

        private void check_interlaced_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "Interlaced", check_interlaced.IsChecked.Value.ToString());
            update_framerate = true;
        }

        private void check_4Gb_Only_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "LimitedTo4Gb", check_4gb_only.IsChecked.Value.ToString());
            update_massive = true;
        }

        private void check_Stereo(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "LimitedToStereo", check_stereo.IsChecked.Value.ToString());
            update_audio = true;
        }

        private void textbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                //Сохраняем изменения
                TextBox tbox = (TextBox)sender;
                if (tbox == textbox_vcodecs) vcodecs_ok_Click(null, null);
                else if (tbox == textbox_framerates) fps_ok_Click(null, null);
                else if (tbox == textbox_aspects) aspects_ok_Click(null, null);
                else if (tbox == textbox_acodecs) acodecs_ok_Click(null, null);
                else if (tbox == textbox_samplerates) samplerates_ok_Click(null, null);
                else if (tbox == textbox_mux_v || tbox == textbox_mux_a || tbox == textbox_mux_o) mux_cli_ok_Click(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                //Отменяем изменения
                TextBox tbox = (TextBox)sender;
                if (tbox == textbox_vcodecs) textbox_vcodecs.Text = undo_vcodecs;
                else if (tbox == textbox_framerates) textbox_framerates.Text = undo_framerates;
                else if (tbox == textbox_aspects) textbox_aspects.Text = undo_aspects;
                else if (tbox == textbox_acodecs) textbox_acodecs.Text = undo_acodecs;
                else if (tbox == textbox_samplerates) textbox_samplerates.Text = undo_samplerates;
                else if (tbox == textbox_mux_v) textbox_mux_v.Text = undo_mux_v;
                else if (tbox == textbox_mux_a) textbox_mux_a.Text = undo_mux_a;
                else if (tbox == textbox_mux_o) textbox_mux_o.Text = undo_mux_o;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //При потере фокуса сначала проверяем, есть ли что сохранять
            TextBox tbox = (TextBox)sender;
            if (tbox == textbox_vcodecs) { if (textbox_vcodecs.Text != undo_vcodecs) vcodecs_ok_Click(null, null); }
            else if (tbox == textbox_framerates) { if (textbox_framerates.Text != undo_framerates) fps_ok_Click(null, null); }
            else if (tbox == textbox_aspects) { if (textbox_aspects.Text != undo_aspects) aspects_ok_Click(null, null); }
            else if (tbox == textbox_acodecs) { if (textbox_acodecs.Text != undo_acodecs) acodecs_ok_Click(null, null); }
            else if (tbox == textbox_samplerates) { if (textbox_samplerates.Text != undo_samplerates) samplerates_ok_Click(null, null); }
            else if (tbox == textbox_mux_v) { if (textbox_mux_v.Text != undo_mux_v) mux_cli_ok_Click(null, null); }
            else if (tbox == textbox_mux_a) { if (textbox_mux_a.Text != undo_mux_a) mux_cli_ok_Click(null, null); }
            else if (tbox == textbox_mux_o) { if (textbox_mux_o.Text != undo_mux_o) mux_cli_ok_Click(null, null); }
        }

        private void vcodecs_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            string[] values = textbox_vcodecs.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                string ss = value.Trim().ToUpper();
                if (ss == "X265") ss = "x265, ";
                else if (ss == "X264") ss = "x264, ";
                else if (ss == "X262") ss = "x262, ";
                else if (ss == "MPEG1") ss = "MPEG1, ";
                else if (ss == "MPEG2") ss = "MPEG2, ";
                else if (ss == "MPEG4") ss = "MPEG4, ";
                else if (ss == "FLV1") ss = "FLV1, ";
                else if (ss == "MJPEG") ss = "MJPEG, ";
                else if (ss == "HUFF") ss = "HUFF, ";
                else if (ss == "FFV1") ss = "FFV1, ";
                else if (ss == "XVID") ss = "XviD, ";
                else if (ss == "DV") ss = "DV, ";
                else continue;

                if (!output.Contains(ss)) output += ss;
            }
            if (output.Length == 0) output = StringArrayToString(def.VCodecs);
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_vcodecs.Text = undo_vcodecs = output;
            StoreValue(format, "VCodecs", output);
        }

        private void acodecs_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            string[] values = textbox_acodecs.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                string ss = value.Trim().ToUpper();
                if (ss == "PCM") ss = "PCM, ";
                else if (ss == "FLAC") ss = "FLAC, ";
                else if (ss == "QAAC") ss = "QAAC, ";
                else if (ss == "AAC") ss = "AAC, ";
                else if (ss == "MP2") ss = "MP2, ";
                else if (ss == "MP3") ss = "MP3, ";
                else if (ss == "AC3") ss = "AC3, ";
                else continue;

                if (!output.Contains(ss)) output += ss;
            }
            if (output.Length == 0) output = StringArrayToString(def.ACodecs);
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_acodecs.Text = undo_acodecs = output;
            StoreValue(format, "ACodecs", output);
        }

        private void fps_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            ArrayList _output = new ArrayList();
            string[] values = textbox_framerates.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                double dd;
                string ss = value.Trim().Replace(".", Calculate.DecimalSeparator);
                if (double.TryParse(ss, out dd))
                {
                    ss = Calculate.ConvertDoubleToPointString(dd, 3);
                    if ((dd == 0 || dd > 5 && dd < 200) && !_output.Contains(ss))
                    {
                        _output.Add(ss);
                        output += ss + ", ";
                    }
                }
            }
            if (output.Length == 0) output = StringArrayToString(def.Framerates);
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_framerates.Text = undo_framerates = output;
            StoreValue(format, "Framerates", output);
            update_framerate = true;
        }

        private void samplerates_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            string[] values = textbox_samplerates.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                int dd;
                string ss = value.Trim().Replace(".", "");
                if (int.TryParse(ss, out dd) && dd >= 8000 && dd < 200000 && !output.Contains(ss)) output += ss + ", ";
                else if (ss.ToLower() == "auto") { output = "Auto"; break; }
            }
            if (output.Length == 0) output = StringArrayToString(def.Samplerates);
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_samplerates.Text = undo_samplerates = output;
            StoreValue(format, "Samplerates", output);
            update_audio = true;
        }

        private void aspects_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            string[] values = textbox_aspects.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                double dd;
                string ss = value.Trim().Replace(".", Calculate.DecimalSeparator);
                if (ss.Length > 7) ss = ss.Substring(0, 7); //Удаляем лишнее
                if (double.TryParse(ss, out dd))
                {
                    ss = Calculate.ConvertDoubleToPointString(dd, 4);
                    if (dd > 0.5 && dd <= 5 && !output.Contains(ss))
                    {
                        if (Math.Abs(dd - 1.33) <= 0.01) ss += " (4:3)";
                        else if (Math.Abs(dd - 1.77) <= 0.01) ss += " (16:9)";
                        output += ss + ", ";
                    }
                }
            }
            if (output.Length == 0) output = StringArrayToString(def.Aspects);
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_aspects.Text = undo_aspects = output;
            StoreValue(format, "Aspects", output);
            update_resolution = true;
        }

        private void mux_cli_ok_Click(object sender, RoutedEventArgs e)
        {
            undo_mux_v = textbox_mux_v.Text;
            undo_mux_a = textbox_mux_a.Text;
            undo_mux_o = textbox_mux_o.Text;

            string output = "[v]" + undo_mux_v + "[/v][a]" + undo_mux_a + "[/a][o]" + undo_mux_o + "[/o]";
            StoreValue(format, "CLI_" + combo_Muxer.SelectedItem.ToString(), output);
        }

        private void combo_split_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_split.IsDropDownOpen || combo_split.IsSelectionBoxHighlighted || combo_split.IsEditable) && combo_split.SelectedItem != null)
            {
                if (EnableEditing(combo_split)) return;
                else DisableEditing(combo_split);

                undo_split = combo_split.SelectedItem.ToString();
                StoreValue(format, "Splitting", undo_split);
            }
        }

        private void combo_thm_format_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_thm_format.IsDropDownOpen || combo_thm_format.IsSelectionBoxHighlighted) && combo_thm_format.SelectedItem != null)
            {
                StoreValue(format, "THM_Format", combo_thm_format.SelectedItem.ToString());
            }
        }

        private void check_thm_fix_ar_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "THM_FixAR", check_thm_fix_ar.IsChecked.Value.ToString());
        }

        private void combo_thm_W_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_thm_W.IsDropDownOpen || combo_thm_W.IsSelectionBoxHighlighted) && combo_thm_W.SelectedItem != null)
            {
                StoreValue(format, "THM_Width", combo_thm_W.SelectedItem.ToString());
            }
        }

        private void combo_thm_H_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_thm_H.IsDropDownOpen || combo_thm_H.IsSelectionBoxHighlighted) && combo_thm_H.SelectedItem != null)
            {
                StoreValue(format, "THM_Height", combo_thm_H.SelectedItem.ToString());
            }
        }

        private void check_dont_mux_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "DontMuxStreams", check_dont_mux.IsChecked.Value.ToString());
        }

        private void check_direct_encoding_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "DirectEncoding", check_direct_encoding.IsChecked.Value.ToString());
        }

        private void check_direct_remux_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "DirectRemuxing", check_direct_remux.IsChecked.Value.ToString());
        }

        private void button_reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Calculate.StartupPath + "\\presets\\formats\\" + format.ToString() + ".ini";
                if (File.Exists(path))
                {
                    //Удаляем всё из ini-файла, пусть используются дефолты
                    Formats.SetSettings(format, null, null);

                    LoadSettings();
                    update_audio = update_framerate = update_resolution = true;
                }
            }
            catch (Exception ex)
            {
                ErrorException(Languages.Translate("Can`t delete profile") + " \"" + format.ToString() + "\": " + ex.Message, ex.StackTrace);
            }
        }
    }
}