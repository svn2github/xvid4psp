﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Text.RegularExpressions;

namespace XviD4PSP
{
    public partial class ColorCorrection
    {
        public Massive m;
        private Massive oldm;
        private MainWindow p;
        private string old_sbc;
        private bool slider_moved = false;

        public ColorCorrection(Massive mass, MainWindow parent)
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
                m.sbc = old_sbc = Settings.SBC;
                m = DecodeProfile(m);

                //Настройки гистограммы не хранятся в пресете
                combo_histogram.IsEnabled = false;
            }

            this.Owner = this.p = parent;

            //переводим
            Title = Languages.Translate("Color correction");
            text_profile.Content = Languages.Translate("Profile:");
            button_apply.Content = Languages.Translate("Apply");
            button_apply.ToolTip = Languages.Translate("Refresh preview");
            button_cancel.Content = Languages.Translate("Cancel");
            //button_fullscreen.Content = Languages.Translate("Fullscreen");
            button_ok.Content = Languages.Translate("OK");
            button_add.ToolTip = Languages.Translate("Add profile");
            button_remove.ToolTip = Languages.Translate("Remove profile");
            text_brightness.Content = Languages.Translate("Brightness") + ":";
            text_saturation.Content = Languages.Translate("Saturation") + ":";
            text_contrast.Content = Languages.Translate("Contrast") + ":";
            text_hue.Content = Languages.Translate("Hue") + ":";
            text_histogram.Content = Languages.Translate("Histogram") + ":";

            //AviSynth 2.6+
            if (SysInfo.AVSVersionFloat >= 2.6f)
            {
                check_dithering.IsEnabled = true;
                check_dithering.ToolTip = Languages.Translate("Apply dithering to prevent banding");
            }
            else
            {
                check_dithering.IsEnabled = false;
                check_dithering.ToolTip = Languages.Translate("AviSynth 2.6+ is required");
            }

            check_fullrange.ToolTip = Languages.Translate("Do not clip luma and chroma to TV levels");

            combo_brightness.ToolTip = Languages.Translate("Is used to change the brightness of the image.") + Environment.NewLine +
               Languages.Translate("Positive values increase the brightness.") + Environment.NewLine +
               Languages.Translate("Negative values decrease the brightness.");

            combo_hue.ToolTip = Languages.Translate("Is used to adjust the color hue of the image.") + Environment.NewLine +
                Languages.Translate("Positive values shift the image towards red.") + Environment.NewLine +
                Languages.Translate("Negative values shift it towards green.");

            combo_contrast.ToolTip = Languages.Translate("Is used to change the contrast of the image.") + Environment.NewLine +
                Languages.Translate("Values above 1.0 increase the contrast.") + Environment.NewLine +
                Languages.Translate("Values below 1.0 decrease the contrast.");

            combo_saturation.ToolTip = Languages.Translate("Is used to adjust the color saturation of the image.") + Environment.NewLine +
                Languages.Translate("Values above 1.0 increase the saturation.") + Environment.NewLine +
                Languages.Translate("Values below 1.0 reduce the saturation.");

            //забиваем параметры
            //Цветность
            for (double n = 0.0; n <= 10.0; n += 0.1)
                combo_saturation.Items.Add(n.ToString("0.0").Replace(",", "."));
            slider_saturation.Minimum = 0.0;
            slider_saturation.Maximum = 10.0;
            slider_saturation.SmallChange = 0.1;

            //Контрастность
            for (double n = 0.0; n <= 5.0; n += 0.01)
                combo_contrast.Items.Add(n.ToString("0.00").Replace(",", "."));
            slider_contrast.Minimum = 0.0;
            slider_contrast.Maximum = 5.0;
            slider_contrast.SmallChange = 0.01;

            //Оттенок
            for (int n = -180; n <= 180; n++)
                combo_hue.Items.Add(n);
            slider_hue.Minimum = -180;
            slider_hue.Maximum = 180;
            slider_hue.SmallChange = 1;

            //Яркость
            for (int n = -255; n <= 255; n++)
                combo_brightness.Items.Add(n);
            slider_brightness.Minimum = -255;
            slider_brightness.Maximum = 255;
            slider_brightness.SmallChange = 1;

            //Возможные типы гистограммы
            combo_histogram.Items.Add("Disabled");
            combo_histogram.Items.Add("Classic");
            combo_histogram.Items.Add("Levels");
            combo_histogram.Items.Add("Color");
            combo_histogram.Items.Add("Color2");
            combo_histogram.Items.Add("Luma");
            combo_histogram.Items.Add("Stereo");
            combo_histogram.Items.Add("StereoOverlay");
            combo_histogram.Items.Add("AudioLevels");
            combo_histogram.SelectedItem = (oldm != null) ? oldm.histogram : "Disabled";

            LoadProfiles();    //загружает список профилей в форму, название текущего профиля выбирается = m.sbc
            LoadFromProfile(); //загружает параметры в форму (из массива m)

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (oldm != null)
                m = oldm.Clone();
            else
                m.sbc = old_sbc;

            Close();
        }

        private void combo_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_profile.IsDropDownOpen || combo_profile.IsSelectionBoxHighlighted)
            {
                m.sbc = combo_profile.SelectedItem.ToString();

                DecodeProfile(m);//читает и передает массиву mass значения параметров из файла профиля = m.sbc
                LoadFromProfile();//загружает значения параметров в форму (из массива m)
                Refresh();
            }
        }

        private void LoadProfiles() //загружает список профилей в форму, текущий профиль выбирается = m.sbc
        {
            //загружаем списки профилей цвето коррекции
            combo_profile.Items.Clear();
            combo_profile.Items.Add("Disabled");
            try
            {
                foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\sbc", "*.avs"))
                    combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch { }

            //прописываем текущий профиль
            combo_profile.SelectedItem = m.sbc;
        }

        public static Massive DecodeProfile(Massive mass)//читает и передает массиву mass значения параметров из файла профиля = m.sbc
        {
            //обнуляем параметры на параметры по умолчанию
            mass.iscolormatrix = false;
            mass.tweak_nocoring = false;
            mass.tweak_dither = false;
            mass.saturation = 1.0;
            mass.brightness = 0;
            mass.contrast = 1.00;
            mass.hue = 0;

            if (mass.sbc == "Disabled")
                return mass;

            try
            {
                string line;
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\sbc\\" + mass.sbc + ".avs", System.Text.Encoding.Default))
                {
                    while (!sr.EndOfStream)
                    {
                        //дешифровка яркости контраста ...
                        line = sr.ReadLine().ToLower();
                        if (line.StartsWith("tweak"))
                        {
                            Regex r;
                            Match mat;

                            //получаем hue - оттенок
                            r = new Regex(@"hue=(-?\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                            mat = r.Match(line);
                            if (mat.Success)
                                mass.hue = Convert.ToInt32(mat.Groups[1].Value);

                            //получаем насыщенность
                            r = new Regex(@"sat=(\d+.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                            mat = r.Match(line);
                            if (mat.Success)
                                mass.saturation = Calculate.ConvertStringToDouble(mat.Groups[1].Value);

                            //получаем яркость
                            r = new Regex(@"bright=(-?\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                            mat = r.Match(line);
                            if (mat.Success)
                                mass.brightness = Convert.ToInt32(mat.Groups[1].Value);

                            //получаем контраст
                            r = new Regex(@"cont=(\d+.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                            mat = r.Match(line);
                            if (mat.Success)
                                mass.contrast = Calculate.ConvertStringToDouble(mat.Groups[1].Value);

                            //Full range
                            r = new Regex(@"coring=(\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                            mat = r.Match(line);
                            if (mat.Success)
                                mass.tweak_nocoring = !Convert.ToBoolean(mat.Groups[1].Value);

                            //Dithering
                            r = new Regex(@"dither=(\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                            mat = r.Match(line);
                            if (mat.Success)
                                mass.tweak_dither = Convert.ToBoolean(mat.Groups[1].Value);
                        }
                        else if (line == "colormatrix()")
                        {
                            //дешифровка ColorMatrix
                            mass.iscolormatrix = true;
                        }
                    }
                }
            }
            catch { }

            return mass;
        }

        private void LoadFromProfile() //загружает значения параметров в форму (из массива m)
        {
            combo_saturation.SelectedItem = m.saturation.ToString("0.0").Replace(",", ".");
            combo_hue.SelectedItem = m.hue;
            combo_brightness.SelectedItem = m.brightness;
            combo_contrast.SelectedItem = m.contrast.ToString("0.00").Replace(",", ".");

            slider_saturation.Value = m.saturation;
            slider_hue.Value = m.hue;
            slider_brightness.Value = m.brightness;
            slider_contrast.Value = m.contrast;

            check_colormatrix.IsChecked = m.iscolormatrix;
            check_fullrange.IsChecked = m.tweak_nocoring;
            check_dithering.IsChecked = m.tweak_dither;
        }

        private void button_add_Click(object sender, System.Windows.RoutedEventArgs e) //кнопка "добавить новый профиль"
        {
            string auto_name = "Custom";
            if (m.iscolormatrix) auto_name += " DVD";

            NewProfile newp = new NewProfile(auto_name, Format.EnumToString(m.format), NewProfile.ProfileType.SBC, this); //создается новый профиль
            if (newp.profile != null)
            {
                CreateSBCProfile(newp.profile);
                LoadProfiles();//загружает список профилей в форму, название текущего профиля выбирается = m.sbc
            }
        }

        private void button_remove_Click(object sender, System.Windows.RoutedEventArgs e) //кнопка "удалить профиль"
        {
            if (m.sbc == "Disabled") return;
            
            if (combo_profile.Items.Count > 1)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Do you realy want to remove profile") + " \"" + m.sbc + "\"?",
                    Languages.Translate("Question"),
                    Message.MessageStyle.YesNo);

                if (mess.result == Message.Result.Yes)
                {
                    int last_num = combo_profile.SelectedIndex;
                    string profile_path = Calculate.StartupPath + "\\presets\\sbc\\" + m.sbc + ".avs";

                    try
                    {
                        File.Delete(profile_path);
                    }
                    catch (Exception ex)
                    {
                        new Message(this).ShowMessage(Languages.Translate("Can`t delete profile") + ": " + ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                        return;
                    }

                    //загружаем список фильтров
                    combo_profile.Items.Clear();
                    combo_profile.Items.Add("Disabled");
                    try
                    {
                        foreach (string file in Calculate.GetSortedFiles(Calculate.StartupPath + "\\presets\\sbc", "*.avs"))
                            combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
                    }
                    catch { }

                    //прописываем текущий пресет кодирования
                    if (last_num == 0)
                        m.sbc = combo_profile.Items[0].ToString();
                    else
                        m.sbc = combo_profile.Items[last_num - 1].ToString();
                    combo_profile.SelectedItem = m.sbc;
                    combo_profile.UpdateLayout();

                    DecodeProfile(m);
                    LoadFromProfile();
                    Refresh();
                }
            }
            else
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Not allowed removing the last profile!"),
                    Languages.Translate("Warning"),
                    Message.MessageStyle.Ok);
            }
        }

        private void UpdateManualProfile() //изменяет название текущего профиля на Custom и сохраняет его в виде файла
        {
            CreateSBCProfile("Custom");
            LoadProfiles();
        }

        private void CreateSBCProfile(string profile) //создает новый пресет-файл
        {
            try
            {
                string text = "";
                if (m.iscolormatrix) text += "ColorMatrix()" + Environment.NewLine;
                if (m.hue != 0) text += "Tweak(hue=" + m.hue + ")" + Environment.NewLine;
                if (m.saturation != 1.0) text += "Tweak(sat=" + m.saturation.ToString("0.0").Replace(",", ".") + ")" + Environment.NewLine;
                if (m.brightness != 0) text += "Tweak(bright=" + m.brightness + ")" + Environment.NewLine;
                if (m.contrast != 1.0) text += "Tweak(cont=" + m.contrast.ToString("0.00").Replace(",", ".") + ")" + Environment.NewLine;
                if (m.tweak_nocoring) text += "Tweak(coring=false)" + Environment.NewLine;
                if (m.tweak_dither) text += "Tweak(dither=true)" + Environment.NewLine;

                string path = Calculate.StartupPath + "\\presets\\sbc\\" + profile + ".avs";
                File.WriteAllText(path, text, System.Text.Encoding.Default);
                m.sbc = profile;
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(Languages.Translate("Can`t save profile") + ": " + ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void slider_saturation_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_saturation.IsFocused)
            {
                combo_saturation.SelectedItem = slider_saturation.Value.ToString("0.0").Replace(",", ".");
                m.saturation = Calculate.ConvertStringToDouble(combo_saturation.SelectedItem.ToString());
                UpdateManualProfile();
                slider_moved = true;
            }
        }

        private void combo_saturation_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_saturation.IsDropDownOpen || combo_saturation.IsSelectionBoxHighlighted)
            {
                m.saturation = Calculate.ConvertStringToDouble(combo_saturation.SelectedItem.ToString());
                slider_saturation.Value = m.saturation;
                UpdateManualProfile();
                Refresh();
            }
        }

        private void slider_hue_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_hue.IsFocused)
            {
                m.hue = Convert.ToInt32(slider_hue.Value);
                combo_hue.SelectedItem = m.hue;
                UpdateManualProfile();
                slider_moved = true;
            }
        }

        private void combo_hue_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_hue.IsDropDownOpen || combo_hue.IsSelectionBoxHighlighted)
            {
                m.hue = Convert.ToInt32(combo_hue.SelectedItem);
                slider_hue.Value = m.hue;
                UpdateManualProfile();
                Refresh();
            }
        }

        private void slider_contrast_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_contrast.IsFocused)
            {
                combo_contrast.SelectedItem = slider_contrast.Value.ToString("0.00").Replace(",", ".");
                m.contrast = Calculate.ConvertStringToDouble(combo_contrast.SelectedItem.ToString());
                UpdateManualProfile();
                slider_moved = true;
            }
        }

        private void combo_contrast_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_contrast.IsDropDownOpen || combo_contrast.IsSelectionBoxHighlighted)
            {
                m.contrast = Calculate.ConvertStringToDouble(combo_contrast.SelectedItem.ToString());
                slider_contrast.Value = m.contrast;
                UpdateManualProfile();
                Refresh();
            }
        }

        private void slider_brightness_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_brightness.IsFocused)
            {
                m.brightness = Convert.ToInt32(slider_brightness.Value);
                combo_brightness.SelectedItem = m.brightness;
                UpdateManualProfile();
                slider_moved = true;
            }
        }

        private void combo_brightness_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_brightness.IsDropDownOpen || combo_brightness.IsSelectionBoxHighlighted)
            {
                m.brightness = Convert.ToInt32(combo_brightness.SelectedItem);
                slider_brightness.Value = m.brightness;
                UpdateManualProfile();
                Refresh();
            }
        }

        private void check_colormatrix_Clicked(object sender, RoutedEventArgs e)
        {
            m.iscolormatrix = check_colormatrix.IsChecked.Value;
            UpdateManualProfile();
            Refresh();
        }

        private void check_fullrange_Clicked(object sender, RoutedEventArgs e)
        {
            m.tweak_nocoring = check_fullrange.IsChecked.Value;
            UpdateManualProfile();
            Refresh();
        }

        private void check_dithering_Clicked(object sender, RoutedEventArgs e)
        {
            m.tweak_dither = check_dithering.IsChecked.Value;
            UpdateManualProfile();
            Refresh();
        }

        private void Refresh()
        {
            if (oldm != null)
            {
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
                p.m = m.Clone();
                p.Refresh(m.script);
                this.Focus();
            }
        }

        private void button_apply_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Refresh();
        }

        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (oldm != null)
            {
                p.SwitchToFullScreen();
                this.Focus();
            }
        }

        //Обработка выбора режима отображения гистограммы
        private void combo_histogram_SelectionChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (combo_histogram.IsDropDownOpen || combo_histogram.IsSelectionBoxHighlighted)
            {
                m.histogram = Convert.ToString(combo_histogram.SelectedItem);
                Refresh();
            }
        }

        private void SliderUp(object sender, object e)
        {
            if (slider_moved)
            {
                //Обновление превью после перемещения слайдеров
                slider_moved = false;
                Refresh();
            }
        }
    }
}