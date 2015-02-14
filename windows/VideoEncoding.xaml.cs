﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace XviD4PSP
{
	public partial class VideoEncoding
	{
        public Massive m;
        private Massive oldm;
        private bool profile_was_changed = false;
        private string old_vencoding;

        x265 x265c;
        x264 x264c;
        x262 x262c;
        XviD xvid;
        FMPEG4 mpeg4;
        FMPEG2 mpeg2;
        FMPEG1 mpeg1;
        FDV dv;
        FFHUFF huff;
        FFV1 ffv1;
        FLV1 flv;
        FMJPEG mjpeg;
        CopyOrDisabled copyordisabled;

        MainWindow p;

        public VideoEncoding(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            if (mass != null)
            {
                this.m = mass.Clone();
                this.oldm = mass.Clone();
            }
            else
            {
                //Заполняем массив
                m = new Massive();
                m.format = Settings.FormatOut;
                m.vencoding = old_vencoding = Settings.GetVEncodingPreset(m.format);
                m.outvcodec = PresetLoader.GetVCodec(m);
                m.vpasses = PresetLoader.GetVCodecPasses(m);
                m = PresetLoader.DecodePresets(m);
                m.infilepath = "C:\\file.mkv";
                m.invcodecshort = "NOINPUT";
                m.inresw = m.outresw = 640;
                m.inresh = m.outresh = 480;
                m.induration = m.outduration = TimeSpan.FromMinutes(1); 
                m.inframerate = m.outframerate = "25.000";
                m.inframes = m.outframes = 1000;
                m.infilesizeint = 1000;
                m.invbitrate = 1000;

                //Убираем лишнее, т.к. всё-равно показывать там нечего
                text_insize_value.Visibility = text_outsize_value.Visibility = Visibility.Collapsed;
                text_inquality_value.Visibility = text_outquality_value.Visibility = Visibility.Collapsed;
                text_codec.Margin = text_incodec_value.Margin = combo_codec.Margin = new Thickness(16, 8, 16, 8);
                row2.Height = row3.Height = new GridLength(0);
            }

            this.Owner = this.p = parent;

            //загружаем список кодеков соответвующий формату
            foreach (string codec in Format.GetVCodecsList(m.format)) combo_codec.Items.Add(codec);
            combo_codec.Items.Add("Copy");
            if (!combo_codec.Items.Contains(m.outvcodec)) combo_codec.Items.Add(m.outvcodec);
            combo_codec.SelectedItem = m.outvcodec;
            text_incodec_value.Content = m.invcodecshort;
            text_insize_value.Content = m.infilesize;
            text_outsize_value.Content = m.outfilesize = Calculate.GetEncodingSize(m);

            //загружаем правильную страницу
            LoadCodecWindow();

            //переводим
            Title = Languages.Translate("Video encoding settings");
            text_size.Content = Languages.Translate("Size") + ":";
            text_codec.Content = Languages.Translate("Codec") + ":";
            text_quality.Content = Languages.Translate("Quality") + ":";
            text_profile.Content = Languages.Translate("Profile:");
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_add.ToolTip = Languages.Translate("Add profile");
            button_remove.ToolTip = Languages.Translate("Remove profile");

            //bit-pixels calculation
            text_inquality_value.Content = Calculate.GetQualityIn(m);
            text_outquality_value.Content = Calculate.GetQualityOut(m, true);

            LoadProfiles();

            ShowDialog();
		}

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        private void LoadCodecWindow()
        {
            //загрузка
            if (m.outvcodec == "x265")
            {
                x265c = new x265(m, this, p);
                grid_codec.Children.Add(x265c);
            }
            else if (m.outvcodec == "x264")
            {
                x264c = new x264(m, this, p);
                grid_codec.Children.Add(x264c);
            }
            else if (m.outvcodec == "x262")
            {
                x262c = new x262(m, this, p);
                grid_codec.Children.Add(x262c);
            }
            else if (m.outvcodec == "XviD")
            {
                xvid = new XviD(m, this, p);
                grid_codec.Children.Add(xvid);
            }
            else if (m.outvcodec == "MPEG1")
            {
                mpeg1 = new FMPEG1(m, this, p);
                grid_codec.Children.Add(mpeg1);
            }
            else if (m.outvcodec == "MPEG2")
            {
                mpeg2 = new FMPEG2(m, this, p);
                grid_codec.Children.Add(mpeg2);
            }
            else if (m.outvcodec == "MPEG4")
            {
                mpeg4 = new FMPEG4(m, this, p);
                grid_codec.Children.Add(mpeg4);
            }
            else if (m.outvcodec == "DV")
            {
                dv = new FDV(m, this, p);
                grid_codec.Children.Add(dv);
            }
            else if (m.outvcodec == "HUFF")
            {
                huff = new FFHUFF(m, this, p);
                grid_codec.Children.Add(huff);
            }
            else if (m.outvcodec == "FFV1")
            {
                ffv1 = new FFV1(m, this, p);
                grid_codec.Children.Add(ffv1);
            }
            else if (m.outvcodec == "FLV1")
            {
                flv = new FLV1(m, this, p);
                grid_codec.Children.Add(flv);
            }
            else if (m.outvcodec == "MJPEG")
            {
                mjpeg = new FMJPEG(m, this, p);
                grid_codec.Children.Add(mjpeg);
            }
            else if (m.outvcodec == "Copy")
            {
                copyordisabled = new CopyOrDisabled();
                copyordisabled.text_info.Content = "Codec: " + m.invcodecshort + Environment.NewLine;
                copyordisabled.text_info.Content += "Bitrate: " + m.invbitrate + " kbps" + Environment.NewLine;
                copyordisabled.text_info.Content += "Resolution: " + m.inresw + "x" + m.inresh + Environment.NewLine;
                copyordisabled.text_info.Content += "Framerate: " + m.inframerate + " fps";
                grid_codec.Children.Add(copyordisabled);
            }
        }

        private void UnLoadCodecWindow()
        {
            //очистка
            if (x265c != null)
            {
                grid_codec.Children.Remove(x265c);
                x265c = null;
            }
            else if (x264c != null)
            {
                grid_codec.Children.Remove(x264c);
                x264c = null;
            }
            else if (x262c != null)
            {
                grid_codec.Children.Remove(x262c);
                x262c = null;
            }
            else if (xvid != null)
            {
                grid_codec.Children.Remove(xvid);
                xvid = null;
            }
            else if (mpeg1 != null)
            {
                grid_codec.Children.Remove(mpeg1);
                mpeg1 = null;
            }
            else if (mpeg2 != null)
            {
                grid_codec.Children.Remove(mpeg2);
                mpeg2 = null;
            }
            else if (mpeg4 != null)
            {
                grid_codec.Children.Remove(mpeg4);
                mpeg4 = null;
            }
            else if (dv != null)
            {
                grid_codec.Children.Remove(dv);
                dv = null;
            }
            else if (huff != null)
            {
                grid_codec.Children.Remove(huff);
                huff = null;
            }
            else if (ffv1 != null)
            {
                grid_codec.Children.Remove(ffv1);
                ffv1 = null;
            }
            else if (flv != null)
            {
                grid_codec.Children.Remove(flv);
                flv = null;
            }
            else if (mjpeg != null)
            {
                grid_codec.Children.Remove(mjpeg);
                mjpeg = null;
            }
            else if (copyordisabled != null)
            {
                grid_codec.Children.Remove(copyordisabled);
                copyordisabled = null;
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (oldm != null && x265c == null && x264c == null && x262c == null && xvid == null && mpeg4 == null) UpdateMassive(); //CustomCLI
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (oldm != null)
                m = oldm.Clone();
            else
                m.vencoding = old_vencoding;

            Close();
        }

        private void combo_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_profile.IsDropDownOpen || combo_profile.IsSelectionBoxHighlighted) && combo_profile.SelectedItem != null)
            {
                RefreshCodecProfileWindow();

                //правим выходной битрейт
                if (m.outvcodec == "Copy")
                    m.outvbitrate = m.invbitrate;
                
                profile_was_changed = true;
                UpdateOutSize();
                profile_was_changed = false;
                
                //проверяем можно ли копировать данный формат
                if (m.outvcodec == "Copy")
                {
                    string CopyProblems = Format.ValidateCopyVideo(m);
                    if (CopyProblems != null)
                    {
                        new Message(this).ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                            " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine +
                            Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }
            }
        }

        private void combo_codec_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_codec.IsDropDownOpen || combo_codec.IsSelectionBoxHighlighted) && combo_codec.SelectedItem != null)
            {
                UnLoadCodecWindow();

                m.x265options = new x265_arguments();
                m.x264options = new x264_arguments();
                m.x262options = new x262_arguments();
                m.XviD_options = new XviD_arguments();
                m.ffmpeg_options = new ffmpeg_arguments();

                m.outvcodec = combo_codec.SelectedItem.ToString();
                m = Format.GetValidVEncodingMode(m);

                LoadCodecWindow();
                if (m.outvcodec == "Copy")
                {
                    combo_profile.SelectedItem = m.outvcodec;
                    m.vencoding = m.outvcodec;
                }
                else
                    UpdateManualProfile();

                //проверяем можно ли копировать данный формат
                if (m.outvcodec == "Copy")
                {
                    string CopyProblems = Format.ValidateCopyVideo(m);
                    if (CopyProblems != null)
                    {
                        new Message(this).ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                            " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine +
                            Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }

                //правим выходной битрейт
                if (m.outvcodec == "Copy") m.outvbitrate = m.invbitrate;

                UpdateOutSize();

                if (x265c != null) x265c.UpdateCLI();
                if (x264c != null) x264c.UpdateCLI();
                if (x262c != null) x262c.UpdateCLI();
                if (xvid != null) xvid.UpdateCLI();
                if (mpeg4 != null) mpeg4.UpdateCLI();
            }
        }

        private void RefreshCodecProfileWindow()
        {
            if (combo_profile.SelectedItem.ToString() == "Copy")
                combo_codec.SelectedItem = combo_profile.SelectedItem.ToString();

            if (m.outvcodec != "Copy" && combo_profile.SelectedItem.ToString() == "Copy")
            {
                UnLoadCodecWindow();
                m.outvcodec = combo_profile.SelectedItem.ToString();
                combo_codec.SelectedItem = m.outvcodec;
                m.vencoding = m.outvcodec;
                LoadCodecWindow();
            }
            else
            {
                string codec = m.outvcodec;
                m.vencoding = combo_profile.SelectedItem.ToString();
                string newcodec = PresetLoader.GetVCodec(m);
                if (codec != newcodec)
                {
                    UnLoadCodecWindow();
                    m.outvcodec = newcodec;
                    LoadCodecWindow();
                }

                LoadProfileToCodec();
                combo_codec.SelectedItem = m.outvcodec;
            }
        }

        private void UpdateMassive()
        {
            if (x265c != null)
            {
                m = x265c.m.Clone();
                m = x265.EncodeLine(m);
            }
            else if (x264c != null)
            {
                m = x264c.m.Clone();
                m = x264.EncodeLine(m);
            }
            else if (x262c != null)
            {
                m = x262c.m.Clone();
                m = x262.EncodeLine(m);
            }
            else if (xvid != null)
            {
                m = xvid.m.Clone();
                m = XviD.EncodeLine(m);
            }
            else if (mpeg1 != null)
            {
                m = mpeg1.m.Clone();
                m = FMPEG1.EncodeLine(m);
            }
            else if (mpeg2 != null)
            {
                m = mpeg2.m.Clone();
                m = FMPEG2.EncodeLine(m);
            }
            else if (mpeg4 != null)
            {
                m = mpeg4.m.Clone();
                m = FMPEG4.EncodeLine(m);
            }
            else if (dv != null)
            {
                m = dv.m.Clone();
                m = FDV.EncodeLine(m);
            }
            else if (huff != null)
            {
                m = huff.m.Clone();
                m = FFHUFF.EncodeLine(m);
            }
            else if (ffv1 != null)
            {
                m = ffv1.m.Clone();
                m = FFV1.EncodeLine(m);
            }
            else if (flv != null)
            {
                m = flv.m.Clone();
                m = FLV1.EncodeLine(m);
            }
            else if (mjpeg != null)
            {
                m = mjpeg.m.Clone();
                m = FMJPEG.EncodeLine(m);
            }
        }

        private void UpdateCodecMassive()
        {
            if (x265c != null) x265c.m = m.Clone();
            else if (x264c != null) x264c.m = m.Clone();
            else if (x262c != null) x262c.m = m.Clone();
            else if (xvid != null) xvid.m = m.Clone();
            else if (mpeg1 != null) mpeg1.m = m.Clone();
            else if (mpeg2 != null) mpeg2.m = m.Clone();
            else if (mpeg4 != null) mpeg4.m = m.Clone();
            else if (dv != null) dv.m = m.Clone();
            else if (huff != null) huff.m = m.Clone();
            else if (ffv1 != null) ffv1.m = m.Clone();
            else if (flv != null) flv.m = m.Clone();
            else if (mjpeg != null) mjpeg.m = m.Clone();
        }

        public void LoadProfiles()
        {
            //загружаем список пресетов
            combo_profile.Items.Clear();
            try
            {
                foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\video", "*.txt"))
                    combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }
            combo_profile.Items.Add("Copy");

            //прописываем текущий пресет кодирования
            combo_profile.SelectedItem = m.vencoding;
        }

        private void LoadProfileToCodec()
        {
            //записываем профиль в реестр
            Settings.SetVEncodingPreset(m.format, combo_profile.SelectedItem.ToString());

            if (x265c != null)
            {
                //забиваем настройки из профиля
                x265c.m.vencoding = combo_profile.SelectedItem.ToString();
                x265c.m.outvcodec = PresetLoader.GetVCodec(x265c.m);
                x265c.m.vpasses = PresetLoader.GetVCodecPasses(x265c.m);
                x265c.m = PresetLoader.DecodePresets(x265c.m);
                x265c.LoadFromProfile();
            }
            else if (x264c != null)
            {
                //забиваем настройки из профиля
                x264c.m.vencoding = combo_profile.SelectedItem.ToString();
                x264c.m.outvcodec = PresetLoader.GetVCodec(x264c.m);
                x264c.m.vpasses = PresetLoader.GetVCodecPasses(x264c.m);
                x264c.m = PresetLoader.DecodePresets(x264c.m);
                x264c.LoadFromProfile();
            }
            else if (x262c != null)
            {
                //забиваем настройки из профиля
                x262c.m.vencoding = combo_profile.SelectedItem.ToString();
                x262c.m.outvcodec = PresetLoader.GetVCodec(x262c.m);
                x262c.m.vpasses = PresetLoader.GetVCodecPasses(x262c.m);
                x262c.m = PresetLoader.DecodePresets(x262c.m);
                x262c.LoadFromProfile();
            }
            else if (xvid != null)
            {
                //забиваем настройки из профиля
                xvid.m.vencoding = combo_profile.SelectedItem.ToString();
                xvid.m.outvcodec = PresetLoader.GetVCodec(xvid.m);
                xvid.m.vpasses = PresetLoader.GetVCodecPasses(xvid.m);
                xvid.m = PresetLoader.DecodePresets(xvid.m);
                xvid.LoadFromProfile();
            }
            else if (mpeg1 != null)
            {
                //забиваем настройки из профиля
                mpeg1.m.vencoding = combo_profile.SelectedItem.ToString();
                mpeg1.m.outvcodec = PresetLoader.GetVCodec(mpeg1.m);
                mpeg1.m.vpasses = PresetLoader.GetVCodecPasses(mpeg1.m);
                mpeg1.m = PresetLoader.DecodePresets(mpeg1.m);
                mpeg1.LoadFromProfile();
            }
            else if (mpeg2 != null)
            {
                //забиваем настройки из профиля
                mpeg2.m.vencoding = combo_profile.SelectedItem.ToString();
                mpeg2.m.outvcodec = PresetLoader.GetVCodec(mpeg2.m);
                mpeg2.m.vpasses = PresetLoader.GetVCodecPasses(mpeg2.m);
                mpeg2.m = PresetLoader.DecodePresets(mpeg2.m);
                mpeg2.LoadFromProfile();
            }
            else if (mpeg4 != null)
            {
                //забиваем настройки из профиля
                mpeg4.m.vencoding = combo_profile.SelectedItem.ToString();
                mpeg4.m.outvcodec = PresetLoader.GetVCodec(mpeg4.m);
                mpeg4.m.vpasses = PresetLoader.GetVCodecPasses(mpeg4.m);
                mpeg4.m = PresetLoader.DecodePresets(mpeg4.m);
                mpeg4.LoadFromProfile();
            }
            else if (dv != null)
            {
                //забиваем настройки из профиля
                dv.m.vencoding = combo_profile.SelectedItem.ToString();
                dv.m.outvcodec = PresetLoader.GetVCodec(dv.m);
                dv.m.vpasses = PresetLoader.GetVCodecPasses(dv.m);
                dv.m = PresetLoader.DecodePresets(dv.m);
                dv.LoadFromProfile();
            }
            else if (huff != null)
            {
                //забиваем настройки из профиля
                huff.m.vencoding = combo_profile.SelectedItem.ToString();
                huff.m.outvcodec = PresetLoader.GetVCodec(huff.m);
                huff.m.vpasses = PresetLoader.GetVCodecPasses(huff.m);
                huff.m = PresetLoader.DecodePresets(huff.m);
                huff.LoadFromProfile();
            }
            else if (ffv1 != null)
            {
                //забиваем настройки из профиля
                ffv1.m.vencoding = combo_profile.SelectedItem.ToString();
                ffv1.m.outvcodec = PresetLoader.GetVCodec(ffv1.m);
                ffv1.m.vpasses = PresetLoader.GetVCodecPasses(ffv1.m);
                ffv1.m = PresetLoader.DecodePresets(ffv1.m);
                ffv1.LoadFromProfile();
            }
            else if (flv != null)
            {
                //забиваем настройки из профиля
                flv.m.vencoding = combo_profile.SelectedItem.ToString();
                flv.m.outvcodec = PresetLoader.GetVCodec(flv.m);
                flv.m.vpasses = PresetLoader.GetVCodecPasses(flv.m);
                flv.m = PresetLoader.DecodePresets(flv.m);
                flv.LoadFromProfile();
            }
            else if (mjpeg != null)
            {
                //забиваем настройки из профиля
                mjpeg.m.vencoding = combo_profile.SelectedItem.ToString();
                mjpeg.m.outvcodec = PresetLoader.GetVCodec(mjpeg.m);
                mjpeg.m.vpasses = PresetLoader.GetVCodecPasses(mjpeg.m);
                mjpeg.m = PresetLoader.DecodePresets(mjpeg.m);
                mjpeg.LoadFromProfile();
            }
        }

        private void button_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m.outvcodec == "Copy") return;

            if (x265c == null && x264c == null && x262c == null && xvid == null && mpeg4 == null)
                UpdateMassive(); //CustomCLI

            string auto_name = m.outvcodec + " ";
            if (m.outvcodec == "HUFF" || m.outvcodec == "FFV1")
            {
                auto_name += "LossLess";
            }
            else if (m.outvcodec == "DV")
            {
                auto_name += "Custom";
            }
            else
            {
                if (m.outvbitrate == 0 || m.outvcodec == "x265" && m.x265options.lossless)
                    auto_name += "LL";
                else
                {
                    if (m.encodingmode == Settings.EncodingModes.Quality ||
                        m.encodingmode == Settings.EncodingModes.Quantizer ||
                        m.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                        m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                        auto_name += "Q" + m.outvbitrate;
                    else if (m.encodingmode == Settings.EncodingModes.OnePass ||
                        m.encodingmode == Settings.EncodingModes.TwoPass ||
                        m.encodingmode == Settings.EncodingModes.ThreePass)
                        auto_name += m.outvbitrate + "k";
                    else
                        auto_name += m.outvbitrate + "MB";
                }

                if (m.outvcodec != "MJPEG")
                {
                    if (m.encodingmode == Settings.EncodingModes.TwoPass ||
                        m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                        m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                        auto_name += " 2P";
                    else if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                        m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                        m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                        auto_name = " 3P";
                }
                auto_name += " Custom";
            }

            NewProfile newp = new NewProfile(auto_name, Format.EnumToString(m.format), NewProfile.ProfileType.VEncoding, this);
            if (newp.profile != null)
            {
                string old_encoding = m.vencoding;
                m.vencoding = newp.profile;         //Название для нового пресета
                try
                {
                    PresetLoader.CreateVProfile(m); //Пресет записывается в файл
                    LoadProfiles();                 //Обновляется содержимое комбобокса с пресетами, выбирается текущий пресет
                }
                catch (Exception ex)
                {
                    new Message(this).ShowMessage(Languages.Translate("Can`t save profile") + ": " + ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                    m.vencoding = old_encoding;
                }
            }

            //Не совсем понятно, зачем нужно перезагружаться с пресета, который мы только что сохранили..
            if (x265c == null && x264c == null && x262c == null && xvid == null && mpeg4 == null) //CustomCLI
            {
                LoadProfileToCodec();
                UpdateOutSize();
                UpdateCodecMassive();
            }
        }

        private void button_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m.outvcodec == "Copy") return;

            if (combo_profile.Items.Count > 1)
            {
                UpdateMassive();

                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Do you realy want to remove profile") + " \"" + m.vencoding + "\"?",
                    Languages.Translate("Question"),
                    Message.MessageStyle.YesNo);

                if (mess.result == Message.Result.Yes)
                {
                    int last_num = combo_profile.SelectedIndex;
                    string profile_path = Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\video\\" + m.vencoding + ".txt";

                    try
                    {
                        File.Delete(profile_path);
                    }
                    catch (Exception ex)
                    {
                        new Message(this).ShowMessage(Languages.Translate("Can`t delete profile") + ": " + ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                        return;
                    }

                    //загружаем список пресетов
                    combo_profile.Items.Clear();
                    try
                    {
                        foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\video", "*.txt"))
                            combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
                    }
                    catch { }
                    combo_profile.Items.Add("Copy");

                    //прописываем текущий пресет кодирования
                    if (last_num == 0)
                    {
                        //Самый первый пресет
                        m.vencoding = combo_profile.Items[0].ToString();
                    }
                    else
                    {
                        //Предыдущий (перед удалённым) пресет
                        m.vencoding = combo_profile.Items[last_num - 1].ToString();
                    }
                    combo_profile.SelectedItem = m.vencoding;
                    combo_profile.UpdateLayout();

                    RefreshCodecProfileWindow();

                    UpdateOutSize();
                    UpdateCodecMassive();
                    LoadProfileToCodec();

                    //проверяем можно ли копировать данный формат
                    if (m.outvcodec == "Copy")
                    {
                        string CopyProblems = Format.ValidateCopyVideo(m);
                        if (CopyProblems != null)
                        {
                            new Message(this).ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                " " + Format.EnumToString(m.format) + ": " + CopyProblems + "."
                                + Environment.NewLine + Languages.Translate("(You see this message because video encoder = Copy)"), Languages.Translate("Warning"));
                        }
                    }
                }
            }
            else
            {
                new Message(this).ShowMessage(Languages.Translate("Not allowed removing the last profile!"), Languages.Translate("Warning"), Message.MessageStyle.Ok);
            }
        }

        public void UpdateOutSize()
        {
            if (profile_was_changed)
            {
                if (x265c != null) m = x265c.m.Clone();
                else if (x264c != null) m = x264c.m.Clone();
                else if (x262c != null) m = x262c.m.Clone();
                else if (xvid != null) m = xvid.m.Clone();
                else if (mpeg4 != null) m = mpeg4.m.Clone();
            }
            else UpdateMassive();

            //Финальный размер
            text_outsize_value.Content = m.outfilesize = Calculate.GetEncodingSize(m);

            //bit-pixels calculation
            text_inquality_value.Content = Calculate.GetQualityIn(m);
            text_outquality_value.Content = Calculate.GetQualityOut(m, true);

            UpdateCodecMassive();
        }

        public void UpdateManualProfile()
        {
            try
            {
                UpdateMassive(); //Клонирует массивы и пересоздает vpasses[x]

                m.vencoding = "Custom";
                PresetLoader.CreateVProfile(m);

                LoadProfiles(); //Пересоздание списка пресетов, выбор текущего
                UpdateCodecMassive(); //Копирует m отсюда в m.кодека
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
	}
}