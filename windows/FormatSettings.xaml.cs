using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace XviD4PSP
{
    public partial class FormatSettings
    {
        public Massive m;
        private MainWindow p;
        public bool update_massive = false;
        public bool update_audio = false;
        public bool update_resolution = false;
        public bool update_framerate = false;
        private string format;

        //Undo
        private string undo_extension;
        private string undo_frc_format;
        private string undo_split;
        private string undo_vcodecs;
        private string undo_acodecs;
        private string undo_aspects;
        private string undo_framerates;
        private string undo_samplerates;

        //Дефолты (а так-же в Format.cs)
        private string default_vcodecs = "x264, MPEG1, MPEG2, MPEG4, FLV1, MJPEG, HUFF, FFV1, XviD, DV, Copy";
        private string default_acodecs = "PCM, FLAC, AAC, MP2, MP3, AC3, Disabled, Copy";
        private string default_aspects = "1.3333 (4:3), 1.6667, 1.7778 (16:9), 1.8500, 2.0000, 2.2100, 2.3529";
        private string default_framerates = "20.000, 23.976, 24.000, 25.000, 29.970, 30.000, 50.000, 59.940, 60.000";
        private string default_samplerates = "22050, 32000, 44100, 48000";

        public FormatSettings(Massive mass, MainWindow parent)
        {
            this.InitializeComponent();

            p = parent;
            Owner = p;

            if (mass == null)
            {
                m = new Massive();
                m.format = Format.ExportFormats.Custom;
            }
            else m = mass.Clone();

            format = m.format.ToString();

            LoadSettings();
            TranslateItems();
            SetTooltips();

            //Переводим фокус
            int set_focus_to = FormatReader.GetFormatInfo(format, "ActiveTab", 1);
            if (set_focus_to == 2) tab_audio.Focus();
            else if (set_focus_to == 3) tab_muxing.Focus();

            ShowDialog();
        }

        private void LoadSettings()
        {
            textbox_vcodecs.Text = undo_vcodecs = FormatReader.GetFormatInfo(format, "GetVCodecs", default_vcodecs);
            textbox_acodecs.Text = undo_acodecs = FormatReader.GetFormatInfo(format, "GetACodecs", default_acodecs);
            textbox_aspects.Text = undo_aspects = FormatReader.GetFormatInfo(format, "GetValidOutAspects", default_aspects);
            textbox_framerates.Text = undo_framerates = FormatReader.GetFormatInfo(format, "GetValidFramerates", default_framerates);
            textbox_samplerates.Text = undo_samplerates = FormatReader.GetFormatInfo(format, "GetValidSamplerates", default_samplerates);

            combo_fix_ar_method.Items.Clear();
            combo_fix_ar_method.Items.Add("Black");
            combo_fix_ar_method.Items.Add("Crop");
            string fix_ar = FormatReader.GetFormatInfo(format, "AspectAdjustingMethod", "Black");
            combo_fix_ar_method.SelectedItem = (combo_fix_ar_method.Items.Contains(fix_ar)) ? fix_ar : "Black";

            combo_Muxer.Items.Clear();
            combo_Muxer.Items.Add("virtualdubmod");
            combo_Muxer.Items.Add("mkvmerge");
            combo_Muxer.Items.Add("ffmpeg");
            combo_Muxer.Items.Add("mp4box");
            combo_Muxer.Items.Add("tsmuxer");
            combo_Muxer.Items.Add("pmpavc");
            combo_Muxer.Items.Add("dpgmuxer");
            combo_Muxer.Items.Add("disabled");
            string muxer = FormatReader.GetFormatInfo(format, "GetMuxer", "mkvmerge").ToLower();
            combo_Muxer.SelectedItem = (combo_Muxer.Items.Contains(muxer)) ? muxer : "ffmpeg";

            combo_Extension.Items.Clear();
            combo_Extension.Items.Add("");
            combo_Extension.Items.Add("3gp");
            combo_Extension.Items.Add("avi");
            combo_Extension.Items.Add("dpg");
            combo_Extension.Items.Add("flv");
            combo_Extension.Items.Add("mp4");
            combo_Extension.Items.Add("m4v");
            combo_Extension.Items.Add("mov");
            combo_Extension.Items.Add("mkv");
            combo_Extension.Items.Add("mpg");
            combo_Extension.Items.Add("pmp");
            combo_Extension.Items.Add("ts");
            combo_Extension.Items.Add("m2ts");
            undo_extension = FormatReader.GetFormatInfo(format, "GetValidExtension", "mkv");
            if (!combo_Extension.Items.Contains(undo_extension)) combo_Extension.Items.Add(undo_extension);
            combo_Extension.SelectedItem = undo_extension;

            combo_force_format.Items.Clear();
            combo_force_format.Items.Add("");
            combo_force_format.Items.Add("None");
            combo_force_format.Items.Add("iPod");
            combo_force_format.Items.Add("PSP");
            combo_force_format.Items.Add("VOB");
            undo_frc_format = FormatReader.GetFormatInfo(format, "ForceFormat", "None");
            if (!combo_force_format.Items.Contains(undo_frc_format)) combo_force_format.Items.Add(undo_frc_format);
            combo_force_format.SelectedItem = undo_frc_format;

            combo_split.Items.Clear();
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
            undo_split = FormatReader.GetFormatInfo(format, "SplitOutputFile", "Disabled");
            if (!combo_split.Items.Contains(undo_split)) combo_split.Items.Add(undo_split);
            combo_split.SelectedItem = undo_split;

            combo_thm_format.Items.Clear();
            combo_thm_format.Items.Add("None");
            combo_thm_format.Items.Add("JPG");
            combo_thm_format.Items.Add("PNG");
            string thm_format = FormatReader.GetFormatInfo(format, "THMFormat", "None");
            if (!combo_thm_format.Items.Contains(thm_format)) combo_thm_format.SelectedItem = "None";
            else combo_thm_format.SelectedItem = thm_format;

            combo_thm_W.Items.Clear();
            combo_thm_W.Items.Add(0);
            for (int i = 120; i < 2000; i += 4) combo_thm_W.Items.Add(i);
            combo_thm_W.SelectedItem = FormatReader.GetFormatInfo(format, "THMWidth", 160);

            combo_thm_H.Items.Clear();
            combo_thm_H.Items.Add(0);
            for (int i = 60; i < 2000; i += 2) combo_thm_H.Items.Add(i);
            combo_thm_H.SelectedItem = FormatReader.GetFormatInfo(format, "THMHeight", 120);

            LoadResolutions();
            combo_ValidModW.Items.Clear();
            combo_ValidModH.Items.Clear();
            for (int w = 4; w <= 16; w *= 2) combo_ValidModW.Items.Add(w);
            combo_ValidModW.SelectedItem = Format.GetValidModW(m);
            for (int h = 2; h <= 16; h *= 2) combo_ValidModH.Items.Add(h);
            combo_ValidModH.SelectedItem = Format.GetValidModH(m);

            check_fixed_ar.IsChecked = FormatReader.GetFormatInfo(format, "IsLockedOutAspect", false);
            check_anamorph.IsChecked = FormatReader.GetFormatInfo(format, "CanBeAnamorphic", false);
            check_stereo.IsChecked = FormatReader.GetFormatInfo(format, "IsLimitedToStereo", false);
            //check_thm_fix_ar.IsChecked = FormatReader.GetFormatInfo(format, "THMFixAR", true);
            check_dont_mux.IsChecked = FormatReader.GetFormatInfo(format, "DontMuxStreams", false);
            check_direct_remux.IsChecked = FormatReader.GetFormatInfo(format, "UseDirectRemux", false);
            check_4gb_only.IsChecked = FormatReader.GetFormatInfo(format, "Is4GBlimitedFormat", false);
        }

        private void LoadResolutions()
        {
            //Ширина
            int n = 16;
            int step = Format.GetValidModW(m);
            string val = "";
            combo_MinResolutionW.Items.Clear();
            combo_MaxResolutionW.Items.Clear();
            while (n < 1920 + step)
            {
                combo_MinResolutionW.Items.Add(n.ToString());
                combo_MaxResolutionW.Items.Add(n.ToString());
                n = n + step;
            }
            val = FormatReader.GetFormatInfo(format, "MinResolutionW", "16");
            if (!combo_MinResolutionW.Items.Contains(val)) combo_MinResolutionW.Items.Add(val);
            combo_MinResolutionW.SelectedItem = val;
            val = FormatReader.GetFormatInfo(format, "MaxResolutionW", "1920");
            if (!combo_MaxResolutionW.Items.Contains(val)) combo_MaxResolutionW.Items.Add(val);
            combo_MaxResolutionW.SelectedItem = val;

            //Высота
            n = 16;
            step = Format.GetValidModH(m);
            combo_MinResolutionH.Items.Clear();
            combo_MaxResolutionH.Items.Clear();
            while (n < 1088 + step)
            {
                combo_MinResolutionH.Items.Add(n.ToString());
                combo_MaxResolutionH.Items.Add(n.ToString());
                n = n + step;
            }
            val = FormatReader.GetFormatInfo(format, "MinResolutionH", "16");
            if (!combo_MinResolutionH.Items.Contains(val)) combo_MinResolutionH.Items.Add(val);
            combo_MinResolutionH.SelectedItem = val;
            val = FormatReader.GetFormatInfo(format, "MaxResolutionH", "1088");
            if (!combo_MaxResolutionH.Items.Contains(val)) combo_MaxResolutionH.Items.Add(val);
            combo_MaxResolutionH.SelectedItem = val;
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (tab_video.IsSelected) StoreValue(format, "ActiveTab", "1");
            else if (tab_audio.IsSelected) StoreValue(format, "ActiveTab", "2");
            else if (tab_muxing.IsSelected) StoreValue(format, "ActiveTab", "3");

            Close();
        }

        private void StoreValue(string format, string key, string value)
        {
            try
            {
                bool ok = false;
                string line = "";
                string output = "";
                string path = Calculate.StartupPath + "\\presets\\formats\\" + format + ".ini";

                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path, System.Text.Encoding.Default))
                    {
                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine();
                            if (line == "[" + key + "]")
                            {
                                output += line + Environment.NewLine + value + Environment.NewLine + Environment.NewLine;
                                //Строка со старым значением, и пустая строка после неё
                                if (!reader.EndOfStream) reader.ReadLine();
                                if (!reader.EndOfStream) if ((line = reader.ReadLine()) != "") output += line + Environment.NewLine;
                                ok = true;
                            }
                            else
                                output += line + Environment.NewLine;
                        }
                    }

                    //Если дошли до конца, а строки так и не было - вставляем её
                    if (!ok) output += "[" + key + "]\r\n" + value + "\r\n\r\n";
                    File.WriteAllText(path, output, System.Text.Encoding.Default);
                }
                else
                {
                    string text = "[FormatName]\r\n" + format + "\r\n\r\n[" + key + "]\r\n" + value + "\r\n\r\n";
                    File.WriteAllText(path, text, System.Text.Encoding.Default);
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                //Если папки нет, создаем её и пробуем снова
                if (!Directory.Exists(Calculate.StartupPath + "\\presets\\formats"))
                {
                    try
                    {
                        Directory.CreateDirectory(Calculate.StartupPath + "\\presets\\formats");
                    }
                    catch (Exception exc)
                    {
                        ErrorException("Can`t create directory: " + exc.Message);
                        return;
                    }
                    StoreValue(format, key, value);
                }
                else
                    ErrorException("StoreValue: " + ex.Message);
            }
            catch (Exception ex)
            {
                ErrorException("StoreValue: " + ex.Message);
            }
        }

        private void TranslateItems()
        {
            button_reset.Content = Languages.Translate("Reset");
            button_ok.Content = Languages.Translate("OK");
            Title = Languages.Translate("Edit format") + " (" + Convert.ToString(m.format) + ")";
            group_format.Header = group_audio.Header = group_muxing.Header = Languages.Translate("Edit format");

            vcodecslist.Content = Languages.Translate("Valid video codecs:");
            framerateslist.Content = Languages.Translate("Valid framerates:");
            check_fixed_ar.Content = Languages.Translate("Fixed AR");
            check_anamorph.Content = Languages.Translate("Can be anamorphic");
            acodecslist.Content = Languages.Translate("Valid audio codecs:");
            check_stereo.Content = Languages.Translate("Limited to Stereo");
            samplerateslist.Content = Languages.Translate("Valid samplerates:");
            validmuxer.Content = Languages.Translate("Muxer for this format:");
            validextension.Content = Languages.Translate("File extension:");
            force_format.Content = Languages.Translate("Force format:");
            split.Content = Languages.Translate("Split output file:");
            thm.Content = Languages.Translate("Create THM:");
            thm_size.Content = Languages.Translate("Resolution:");
            check_dont_mux.Content = Languages.Translate("Don`t multiplex video and audio");
            check_direct_remux.Content = Languages.Translate("Use direct remuxing if possible");
            check_4gb_only.Content = Languages.Translate("Maximum filesize is 4Gb");
        }

        private void SetTooltips()
        {
            button_reset.ToolTip = Languages.Translate("Reset all settings");
            textbox_vcodecs.ToolTip = Languages.Translate("Codecs, that will be selectable in the video-codecs settings window.") + Environment.NewLine + Languages.Translate("Valid values:") + " x264, MPEG1, MPEG2, MPEG4, FLV1, MJPEG, HUFF, FFV1, XviD, DV, Copy\r\n" + Languages.Translate("Separate by comma.");
            textbox_framerates.ToolTip = Languages.Translate("Framerates, that can be set for this format.") + Environment.NewLine + Languages.Translate("Valid values:") + " 15.000, 18.000, 20.000, 23.976, 24.000, 25.000, 29.970, 30.000, 50.000, 59.940, 60.000, 120.000, ...\r\n" + Languages.Translate("Separate by comma.");
            combo_MinResolutionH.ToolTip = combo_MaxResolutionH.ToolTip = combo_thm_H.ToolTip = Languages.Translate("Height");
            combo_MinResolutionW.ToolTip = combo_MaxResolutionW.ToolTip = combo_thm_W.ToolTip = Languages.Translate("Width");
            combo_ValidModW.ToolTip = Languages.Translate("Width") + "\r\n" + Languages.Translate("Values XX are strongly NOT recommended!").Replace("XX", "4, 8");
            combo_ValidModH.ToolTip = Languages.Translate("Height") + "\r\n" + Languages.Translate("Values XX are strongly NOT recommended!").Replace("XX", "2, 4");
            textbox_aspects.ToolTip = Languages.Translate("Aspect ratios.") + Environment.NewLine + Languages.Translate("Valid values:") + " 1.3333 (4:3), 1.5000, 1.6667, 1.7647 (16:9), 1.7778 (16:9), 1.8500, 2.3529, ...\r\n" + Languages.Translate("Separate by comma.");
            check_fixed_ar.ToolTip = Languages.Translate("Use this option if you want to limit AR by values, specified above");
            combo_fix_ar_method.ToolTip = Languages.Translate("Aspect adjusting method:") + " " + combo_fix_ar_method.SelectedItem.ToString();
            textbox_acodecs.ToolTip = Languages.Translate("Codecs, that will be selectable in the audio-codecs settings window.") + Environment.NewLine + Languages.Translate("Valid values:") + " PCM, FLAC, AAC, MP2, MP3, AC3, Disabled, Copy\r\n" + Languages.Translate("Separate by comma.");
            textbox_samplerates.ToolTip = Languages.Translate("Samplerates, that can be set for this format.") + Environment.NewLine + Languages.Translate("Valid values:") + " 22050, 32000, 44100, 48000, 96000, 192000, ...\r\n" + Languages.Translate("Separate by comma.");
            check_stereo.ToolTip = Languages.Translate("Maximum numbers of audio channels for this format is 2");
            combo_Muxer.ToolTip = Languages.Translate("Muxer for this format:") + "\r\nvirtualdubmod - avi\r\nmkvmerge - mkv, webm\r\nffmpeg - all types\r\nmp4box - mp4, m4v, mov, 3gp\r\ntsmuxer - ts, m2ts\r\ndisabled - direct encoding";
            combo_Extension.ToolTip = Languages.Translate("File extension:") + " " + undo_extension;
            combo_force_format.ToolTip = Languages.Translate("Only for this muxers:") + " mp4box (-ipod, -psp), ffmpeg (-f value)";
            combo_split.ToolTip = Languages.Translate("Only for this muxers:") + " mkvmerge, mp4box, tsmuxer";
            combo_thm_format.ToolTip = "THM format";
            combo_thm_W.ToolTip += "\r\n" + Languages.Translate("0 - encoding size");
            combo_thm_H.ToolTip += "\r\n" + Languages.Translate("0 - encoding size");
        }

        private void ErrorException(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void combo_Muxer_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_Muxer.IsDropDownOpen || combo_Muxer.IsSelectionBoxHighlighted)
            {
                StoreValue(format, "GetMuxer", combo_Muxer.SelectedItem.ToString());
            }
        }

        private void combo_Extension_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_Extension.IsDropDownOpen || combo_Extension.IsSelectionBoxHighlighted || combo_Extension.IsEditable) && combo_Extension.SelectedItem != null)
            {
                if (EnableEditing(combo_Extension)) return;
                else DisableEditing(combo_Extension);

                undo_extension = combo_Extension.SelectedItem.ToString();
                combo_Extension.ToolTip = Languages.Translate("File extension:") + " " + undo_extension;
                StoreValue(format, "GetValidExtension", undo_extension);
                update_massive = true;
            }
        }

        private bool EnableEditing(ComboBox box)
        {
            //Включаем редактирование
            if (!box.IsEditable && box.SelectedIndex == 0)
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
                if (box == combo_force_format) box.ToolTip = Languages.Translate("Only for this muxers:") + " mp4box (-ipod, -psp), ffmpeg (-f value)";
                else if (box == combo_split) box.ToolTip = Languages.Translate("Only for this muxers:") + " mkvmerge, mp4box, tsmuxer";
            }
        }

        private void UndoEdit(ComboBox box)
        {
            //Возвращаем исходное значение
            if (box == combo_Extension) box.SelectedItem = undo_extension;
            else if (box == combo_force_format) box.SelectedItem = undo_frc_format;
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
                else if (box == combo_force_format && Calculate.GetRegexValue(@"^([\w\-]+)$", text) == null) { UndoEdit(box); return; }
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

        private void combo_MaxResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MaxResolutionW.IsDropDownOpen || combo_MaxResolutionW.IsSelectionBoxHighlighted)
            {
                if (Convert.ToInt32(combo_MaxResolutionW.SelectedItem) < Convert.ToInt32(combo_MinResolutionW.SelectedItem))
                    combo_MaxResolutionW.SelectedItem = combo_MinResolutionW.SelectedItem;

                StoreValue(format, "MaxResolutionW", combo_MaxResolutionW.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MaxResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MaxResolutionH.IsDropDownOpen || combo_MaxResolutionH.IsSelectionBoxHighlighted)
            {
                if (Convert.ToInt32(combo_MaxResolutionH.SelectedItem) < Convert.ToInt32(combo_MinResolutionH.SelectedItem))
                    combo_MaxResolutionH.SelectedItem = combo_MinResolutionH.SelectedItem;

                StoreValue(format, "MaxResolutionH", combo_MaxResolutionH.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MinResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MinResolutionW.IsDropDownOpen || combo_MinResolutionW.IsSelectionBoxHighlighted)
            {
                if (Convert.ToInt32(combo_MinResolutionW.SelectedItem) > Convert.ToInt32(combo_MaxResolutionW.SelectedItem))
                    combo_MinResolutionW.SelectedItem = combo_MaxResolutionW.SelectedItem;

                StoreValue(format, "MinResolutionW", combo_MinResolutionW.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_MinResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MinResolutionH.IsDropDownOpen || combo_MinResolutionH.IsSelectionBoxHighlighted)
            {
                if (Convert.ToInt32(combo_MinResolutionH.SelectedItem) > Convert.ToInt32(combo_MaxResolutionH.SelectedItem))
                    combo_MinResolutionH.SelectedItem = combo_MaxResolutionH.SelectedItem;

                StoreValue(format, "MinResolutionH", combo_MinResolutionH.SelectedItem.ToString());
                update_resolution = true;
            }
        }

        private void combo_ValidModW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_ValidModW.IsDropDownOpen || combo_ValidModW.IsSelectionBoxHighlighted)
            {
                StoreValue(format, "LimitModW", combo_ValidModW.SelectedItem.ToString());
                LoadResolutions();
                update_resolution = true;
            }
        }

        private void combo_ValidModH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_ValidModH.IsDropDownOpen || combo_ValidModH.IsSelectionBoxHighlighted)
            {
                StoreValue(format, "LimitModH", combo_ValidModH.SelectedItem.ToString());
                LoadResolutions();
                update_resolution = true;
            }
        }

        private void combo_fix_ar_method_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_fix_ar_method.IsDropDownOpen || combo_fix_ar_method.IsSelectionBoxHighlighted)
            {
                string value = combo_fix_ar_method.SelectedItem.ToString();
                StoreValue(format, "AspectAdjustingMethod", value);
                combo_fix_ar_method.ToolTip = Languages.Translate("Aspect adjusting method:") + " " + value;
                update_resolution = true;
            }
        }

        private void check_Fixed_AR_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "IsLockedOutAspect", check_fixed_ar.IsChecked.Value.ToString());
            update_resolution = true;
        }

        private void check_anamorph_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "CanBeAnamorphic", check_anamorph.IsChecked.Value.ToString());
            update_resolution = true;
        }

        private void check_4Gb_Only_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "Is4GBlimitedFormat", check_4gb_only.IsChecked.Value.ToString());
            update_massive = true;
        }

        private void check_Stereo(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "IsLimitedToStereo", check_stereo.IsChecked.Value.ToString());
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
        }

        private void vcodecs_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            string[] values = textbox_vcodecs.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                string ss = value.Trim().ToUpper();
                if (ss == "X264") ss = "x264, ";
                else if (ss == "MPEG1") ss = "MPEG1, ";
                else if (ss == "MPEG2") ss = "MPEG2, ";
                else if (ss == "MPEG4") ss = "MPEG4, ";
                else if (ss == "FLV1") ss = "FLV1, ";
                else if (ss == "MJPEG") ss = "MJPEG, ";
                else if (ss == "HUFF") ss = "HUFF, ";
                else if (ss == "FFV1") ss = "FFV1, ";
                else if (ss == "XVID") ss = "XviD, ";
                else if (ss == "DV") ss = "DV, ";
                else if (ss == "COPY") ss = "Copy, ";
                else continue;

                if (!output.Contains(ss)) output += ss;
            }
            if (output.Length == 0) output = default_vcodecs;
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_vcodecs.Text = undo_vcodecs = output;
            StoreValue(format, "GetVCodecs", output);
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
                else if (ss == "AAC") ss = "AAC, ";
                else if (ss == "MP2") ss = "MP2, ";
                else if (ss == "MP3") ss = "MP3, ";
                else if (ss == "AC3") ss = "AC3, ";
                else if (ss == "DISABLED") ss = "Disabled, ";
                else if (ss == "COPY") ss = "Copy, ";
                else continue;

                if (!output.Contains(ss)) output += ss;
            }
            if (output.Length == 0) output = default_acodecs;
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_acodecs.Text = undo_acodecs = output;
            StoreValue(format, "GetACodecs", output);
        }

        private void fps_ok_Click(object sender, RoutedEventArgs e)
        {
            string output = "";
            string[] values = textbox_framerates.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
            {
                double dd;
                string ss = value.Trim().Replace(".", Calculate.DecimalSeparator);
                if (double.TryParse(ss, out dd))
                {
                    ss = Calculate.ConvertDoubleToPointString(dd, 3);
                    if (dd > 5 && dd < 200 && !output.Contains(ss)) output += ss + ", ";
                }
            }
            if (output.Length == 0) output = default_framerates;
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_framerates.Text = undo_framerates = output;
            StoreValue(format, "GetValidFramerates", output);
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
            }
            if (output.Length == 0) output = default_samplerates;
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_samplerates.Text = undo_samplerates = output;
            StoreValue(format, "GetValidSamplerates", output);
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
                    if (dd > 0.5 && dd < 3 && !output.Contains(ss))
                    {
                        if (Math.Abs(dd - 1.33) <= 0.01) ss += " (4:3)";
                        else if (Math.Abs(dd - 1.77) <= 0.01) ss += " (16:9)";
                        output += ss + ", ";
                    }
                }
            }
            if (output.Length == 0) output = default_aspects;
            else output = output.TrimEnd(new char[] { ',', ' ' });

            textbox_aspects.Text = undo_aspects = output;
            StoreValue(format, "GetValidOutAspects", output);
            update_resolution = true;
        }

        private void combo_force_format_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_force_format.IsDropDownOpen || combo_force_format.IsSelectionBoxHighlighted || combo_force_format.IsEditable) && combo_force_format.SelectedItem != null)
            {
                if (EnableEditing(combo_force_format)) return;
                else DisableEditing(combo_force_format);

                undo_frc_format = combo_force_format.SelectedItem.ToString();
                StoreValue(format, "ForceFormat", undo_frc_format);
            }
        }

        private void combo_split_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_split.IsDropDownOpen || combo_split.IsSelectionBoxHighlighted || combo_split.IsEditable) && combo_split.SelectedItem != null)
            {
                if (EnableEditing(combo_split)) return;
                else DisableEditing(combo_split);

                undo_split = combo_split.SelectedItem.ToString();
                StoreValue(format, "SplitOutputFile", undo_split);
                update_massive = true;
            }
        }

        private void combo_thm_format_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_thm_format.IsDropDownOpen || combo_thm_format.IsSelectionBoxHighlighted)
            {
                StoreValue(format, "THMFormat", combo_thm_format.SelectedItem.ToString());
            }
        }

        private void combo_thm_W_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_thm_W.IsDropDownOpen || combo_thm_W.IsSelectionBoxHighlighted)
            {
                StoreValue(format, "THMWidth", combo_thm_W.SelectedItem.ToString());
            }
        }

        private void combo_thm_H_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_thm_H.IsDropDownOpen || combo_thm_H.IsSelectionBoxHighlighted)
            {
                StoreValue(format, "THMHeight", combo_thm_H.SelectedItem.ToString());
            }
        }

        private void check_thm_fix_ar_Clicked(object sender, RoutedEventArgs e)
        {
            //StoreValue(format, "THMFixAR", check_thm_fix_ar.IsChecked.Value.ToString());
        }

        private void check_direct_remux_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "UseDirectRemux", check_direct_remux.IsChecked.Value.ToString());
        }

        private void check_dont_mux_Clicked(object sender, RoutedEventArgs e)
        {
            StoreValue(format, "DontMuxStreams", check_dont_mux.IsChecked.Value.ToString());
            update_massive = true;
        }

        private void button_reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Просто удаляем всё из ini-файла, пусть используются дефолты
                File.WriteAllText(Calculate.StartupPath + "\\presets\\formats\\" + format + ".ini", "");
                LoadSettings();
                SetTooltips();
                update_audio = update_framerate = update_resolution = true;
            }
            catch (Exception ex)
            {
                ErrorException(Languages.Translate("Can`t delete profile") + " \"" + format + "\": " + ex.Message);
            }
        }
    }
}