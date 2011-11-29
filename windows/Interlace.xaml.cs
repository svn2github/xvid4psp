using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Input;

namespace XviD4PSP
{
	public partial class Interlace
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;

        public Interlace(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            if (mass != null)
            {
                m = mass.Clone();
                oldm = mass.Clone();
            }

            p = parent;
            Owner = p;

            //переводим
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_refresh.Content = Languages.Translate("Apply");
            button_refresh.ToolTip = Languages.Translate("Refresh preview");
            Title = Languages.Translate("Interlace") + "/" + Languages.Translate("Framerate");
            button_analyse.Content = Languages.Translate("Analyse");
            label_deinterlace.Content = Languages.Translate("Deinterlace") + ":";
            label_field_order.Content = Languages.Translate("Field order") + ":";
            label_source_info.Content = Languages.Translate("Detect source type") + ":";
            label_source_type.Content = Languages.Translate("Source type") + ":";
            label_outinterlace.Content = Languages.Translate("Target type") + ":";
            text_in_framerate.Content = Languages.Translate("Input framerate:");
            text_out_framerate.Content = Languages.Translate("Output framerate:");
            text_framerateconvertor.Content = Languages.Translate("Framerate converter:");
            if (m != null) text_in_framerate_value.Content = m.inframerate + " fps";
            //button_fullscreen.Content = Languages.Translate("Fullscreen");
            tab_main.Header = Languages.Translate("Main");
            tab_settings.Header = Languages.Translate("Settings");
            text_analyze_percent.Content = Languages.Translate("Analyze (% of the source lenght)") + ":";
            text_min_sections.Content = Languages.Translate("But no less than (sections)") + ":";
            text_hybrid_int.Content = Languages.Translate("Hybrid interlace threshold") + " (%):";
            text_hybrid_fo.Content = Languages.Translate("Hybrid field order threshold") + " (%):";
            text_fo_portions.Content = Languages.Translate("Enable selective field order analysis");
            check_original_fps.Content = Languages.Translate("Use the original fps of the stream (if available)");
            check_nonstandard_fps.Content = Languages.Translate("Allow non-standard fps on output");

            if (m != null)
            {
                //забиваем
                foreach (string stype in Enum.GetNames(typeof(SourceType)))
                {
                    if (stype != "NOT_ENOUGH_SECTIONS")
                        combo_sourcetype.Items.Add(GetFixedSTypeString(stype));
                }
                combo_sourcetype.SelectedItem = GetFixedSTypeString(m.interlace.ToString());

                foreach (string fieldt in Enum.GetNames(typeof(FieldOrder)))
                    combo_fieldorder.Items.Add(GetFixedFieldString(fieldt));
                combo_fieldorder.SelectedItem = GetFixedFieldString(m.fieldOrder.ToString());

                //Деинтерлейс и всплывающие подсказки к нему
                foreach (DeinterlaceType dtype in Enum.GetValues(typeof(DeinterlaceType)))
                {
                    ComboBoxItem item = new ComboBoxItem();
                    string cont = dtype.ToString(), tltp = "";
                    double frate = Calculate.ConvertStringToDouble(m.inframerate);

                    string regular = Languages.Translate("Regular deinterlacing using");
                    string doubling = Languages.Translate("Deinterlacing with doubling frame rate using");
                    string telecine = Languages.Translate("Inverse Telecine (remove 3:2 pulldown) using");
                    string decimate1 = Languages.Translate("Auto searching and removing duplicate frames, so frame rate will be reduced to");
                    string decimate2 = Languages.Translate("it must be the original frame rate of the video BEFORE duplicates was added!");

                    if (dtype == DeinterlaceType.TFM) tltp = Languages.Translate("FieldMatching (for videos with ShiftedFields) using TFM");
                    else if (dtype == DeinterlaceType.Yadif) tltp = regular + " Yadif";
                    else if (dtype == DeinterlaceType.YadifModEDI) tltp = regular + " YadifMod+NNEDI2 (slow)";
                    else if (dtype == DeinterlaceType.TDeint) tltp = regular + " TDeint";
                    else if (dtype == DeinterlaceType.TDeintEDI) tltp = regular + " TDeint+EEDI2 (very slow)";
                    else if (dtype == DeinterlaceType.TomsMoComp) tltp = regular + " TomsMoComp";
                    else if (dtype == DeinterlaceType.LeakKernelDeint) tltp = regular + " LeakKernelDeint";
                    else if (dtype == DeinterlaceType.FieldDeinterlace) tltp = regular + " FieldDeinterlace (blending)";
                    else if (dtype == DeinterlaceType.QTGMC) { tltp = Languages.Translate("Deinterlacing with denoising using QTGMC (see Settings)"); }
                    else if (dtype == DeinterlaceType.QTGMC_2) { cont = "QTGMC (x2)"; tltp = Languages.Translate("Deinterlacing with doubling frame rate and denoising using QTGMC (see Settings)"); }
                    else if (dtype == DeinterlaceType.MCBob) { cont = "MCBob (x2)"; tltp = doubling + " MCBob (very slow)"; }
                    else if (dtype == DeinterlaceType.NNEDI) { cont = "NNEDI (x2)"; tltp = doubling + " NNEDI2"; }
                    else if (dtype == DeinterlaceType.YadifModEDI2) { cont = "YadifModEDI (x2)"; tltp = doubling + " YadifMod+NNEDI2"; }
                    else if (dtype == DeinterlaceType.SmoothDeinterlace) { cont = "SmoothDeinterlace (x2)"; tltp = doubling + " SmoothDeinterlace"; }
                    else if (dtype == DeinterlaceType.TIVTC) tltp = telecine + " TIVTC\r\n29.970->23.976";
                    else if (dtype == DeinterlaceType.TIVTC_TDeintEDI) { cont = "TIVTC+TDeintEDI"; tltp = telecine + " TIVTC+TDeint+NNEDI2\r\n29.970->23.976"; }
                    else if (dtype == DeinterlaceType.TIVTC_YadifModEDI) { cont = "TIVTC+YadifModEDI"; tltp = telecine + " TIVTC+YadifMod+NNEDI2\r\n29.970->23.976"; }
                    else if (dtype == DeinterlaceType.TDecimate) { cont = "TDecimate 1-in-5"; tltp = Languages.Translate("Remove duplicate frames (remove 1 frame from every 5 frames) using TDecimate") + "\r\n29.970->23.976"; }
                    else if (dtype == DeinterlaceType.TDecimate_23)
                    {
                        cont = "TDecimate to 23.976"; tltp = decimate1 + " 23.976.\r\n23.976 - " + decimate2;
                        item.IsEnabled = (frate > 23.976);
                    }
                    else if (dtype == DeinterlaceType.TDecimate_24)
                    {
                        cont = "TDecimate to 24.000"; tltp = decimate1 + " 24.000.\r\n24.000 - " + decimate2;
                        item.IsEnabled = (frate > 24.000);
                    }
                    else if (dtype == DeinterlaceType.TDecimate_25)
                    {
                        cont = "TDecimate to 25.000"; tltp = decimate1 + " 25.000.\r\n25.000 - " + decimate2;
                        item.IsEnabled = (frate > 25.000);
                    }
                    else tltp = null;

                    item.Tag = dtype;
                    item.Content = cont;
                    item.ToolTip = tltp;
                    combo_deinterlace.Items.Add(item);
                }
                combo_deinterlace.SelectedValue = m.deinterlace;

                bool any = false, infr = false, outfr = false;
                foreach (string f in Format.GetValidFrameratesList(m))
                {
                    if (f != "0.000")
                    {
                        if (!infr && f == m.inframerate) infr = true;
                        if (!outfr && f == m.outframerate) outfr = true;
                        combo_framerate.Items.Add(f + " fps");
                    }
                    else
                        any = true;
                }
                if (any) combo_framerate.Items.Insert(0, ""); else check_nonstandard_fps.IsEnabled = false;
                if (!infr && any && m.inframerate != m.outframerate) combo_framerate.Items.Add(m.inframerate + " fps");
                if (!outfr) combo_framerate.Items.Add(m.outframerate + " fps");
                combo_framerate.SelectedItem = m.outframerate + " fps";

                //интерлейс режимы
                foreach (string interlace in Enum.GetNames(typeof(Massive.InterlaceModes)))
                    combo_outinterlace.Items.Add(interlace);
                combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

                foreach (AviSynthScripting.FramerateModifers ratechangers in Enum.GetValues(typeof(AviSynthScripting.FramerateModifers)))
                    combo_framerateconvertor.Items.Add(new ComboBoxItem() { Content = ratechangers });
                combo_framerateconvertor.SelectedValue = m.frameratemodifer;
                CheckFramerateModifierIsApplied();
            }
            else
            {
                tab_main.IsEnabled = false;
                tab_settings.IsSelected = true;
            }

            //Настройки
            num_analyze_percent.Value = (decimal)Settings.SD_Analyze;
            num_min_sections.Value = (decimal)Settings.SD_Min_Sections;
            num_hybrid_int.Value = (decimal)Settings.SD_Hybrid_Int;
            num_hybrid_fo.Value = (decimal)Settings.SD_Hybrid_FO;
            check_fo_portions.IsChecked = Settings.SD_Portions_FO;

            check_iscombed_mark.IsChecked = Settings.IsCombed_Mark;
            num_iscombed_cthresh.Value = Settings.IsCombed_CThresh;
            num_iscombed_mi.Value = Settings.IsCombed_MI;

            combo_qtgmc_preset.Items.Add("Draft");
            combo_qtgmc_preset.Items.Add("Ultra Fast");
            combo_qtgmc_preset.Items.Add("Super Fast");
            combo_qtgmc_preset.Items.Add("Very Fast");
            combo_qtgmc_preset.Items.Add("Faster");
            combo_qtgmc_preset.Items.Add("Fast");
            combo_qtgmc_preset.Items.Add("Medium");
            combo_qtgmc_preset.Items.Add("Slow");
            combo_qtgmc_preset.Items.Add("Slower");
            combo_qtgmc_preset.Items.Add("Very Slow");
            combo_qtgmc_preset.Items.Add("Placebo");
            combo_qtgmc_preset.SelectedItem = Settings.QTGMC_Preset;

            num_qtgmc_sharp.Value = (decimal)Settings.QTGMC_Sharpness;

            check_original_fps.IsChecked = Settings.MI_Original_fps;
            check_nonstandard_fps.IsChecked = Settings.Nonstandard_fps;

            SetTooltips();
            ShowDialog();
		}

        public static string GetFixedFieldString(string FOrder)
        {
            FieldOrder eFOrder = (FieldOrder)Enum.Parse(typeof(FieldOrder), FOrder);
            if (eFOrder == FieldOrder.UNKNOWN) return "Unknown";
            else if (eFOrder == FieldOrder.VARIABLE) return "Variable";
            else return FOrder;
        }

        public static FieldOrder GetEnumFromFOrderString(string FOrder)
        {
            if (FOrder == "TFF") return FieldOrder.TFF;
            else if (FOrder == "BFF") return FieldOrder.BFF;
            else if (FOrder == "Variable") return FieldOrder.VARIABLE;
            else return FieldOrder.UNKNOWN;
        }

        public static string GetFixedSTypeString(string SType)
        {
            SourceType eSType = (SourceType)Enum.Parse(typeof(SourceType), SType);
            if (eSType == SourceType.UNKNOWN) return "Unknown";
            else if (eSType == SourceType.DECIMATING) return "Decimating";
            else if (eSType == SourceType.FILM) return "Film";
            else if (eSType == SourceType.HYBRID_FILM_INTERLACED) return "Hybrid Film Interlaced";
            else if (eSType == SourceType.HYBRID_PROGRESSIVE_FILM) return "Hybrid Film Progressive";
            else if (eSType == SourceType.HYBRID_PROGRESSIVE_INTERLACED) return "Hybrid Progressive Interlaced";
            else if (eSType == SourceType.INTERLACED) return "Interlaced";
            else if (eSType == SourceType.PROGRESSIVE) return "Progressive";
            else return SType;
        }

        public static SourceType GetEnumFromSTypeString(string SType)
        {
            if (SType == "Unknown") return SourceType.UNKNOWN;
            else if (SType == "Decimating") return SourceType.DECIMATING;
            else if (SType == "Film") return SourceType.FILM;
            else if (SType == "Hybrid Film Interlaced") return SourceType.HYBRID_FILM_INTERLACED;
            else if (SType == "Hybrid Film Progressive") return SourceType.HYBRID_PROGRESSIVE_FILM;
            else if (SType == "Hybrid Progressive Interlaced") return SourceType.HYBRID_PROGRESSIVE_INTERLACED;
            else if (SType == "Interlaced") return SourceType.INTERLACED;
            else if (SType == "Progressive") return SourceType.PROGRESSIVE;
            else return SourceType.UNKNOWN;
        }

        private void button_refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Refresh();
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null) m = oldm.Clone();
            Close();
        }

        private void Refresh()
        {
            if (m != null)
            {
                CheckFramerateModifierIsApplied();

                m = AviSynthScripting.CreateAutoAviSynthScript(m);
                p.m = m.Clone();
                p.Refresh(m.script);
                this.Focus();
            }
        }

        private void SetTooltips()
        {
            foreach (ComboBoxItem item in combo_framerateconvertor.Items)
            {
                if (((AviSynthScripting.FramerateModifers)item.Content) == AviSynthScripting.FramerateModifers.AssumeFPS)
                {
                    item.ToolTip = Languages.Translate("AssumeFPS changes the frame rate without changing the frame count.") + "\r\n" +
                        Languages.Translate("The video will play faster or slower.");
                }
                else if (((AviSynthScripting.FramerateModifers)item.Content) == AviSynthScripting.FramerateModifers.ChangeFPS)
                {
                    item.ToolTip = Languages.Translate("ChangeFPS changes the frame rate by deleting or duplicating frames.") + "\r\n" +
                        Languages.Translate("The motion smoothness may be lost.");
                }
                else if (((AviSynthScripting.FramerateModifers)item.Content) == AviSynthScripting.FramerateModifers.ConvertFPS)
                {
                    item.ToolTip = Languages.Translate("ConvertFPS attempts to convert the frame rate using \"smart blending\" without deleting or duplicating frames.") + "\r\n" +
                        Languages.Translate("The video may lose sharpness or may appear blending artifacts.");
                }
                else if (((AviSynthScripting.FramerateModifers)item.Content) == AviSynthScripting.FramerateModifers.ConvertMFlowFPS)
                {
                    item.ToolTip = Languages.Translate("Creates new frames using motion vectors between existing frames.") + "\r\n" +
                        Languages.Translate("Based on functions MAnalyse and MFlowFps from the MVTools2 plugin.");
                }
            }

            string on = Languages.Translate("On");
            string off = Languages.Translate("Off");
            string _def = Languages.Translate("Default") + ": ";

            num_analyze_percent.ToolTip = _def + "1";
            num_min_sections.ToolTip = _def +  "150 (1 section = 5 frames, 150 sections = 750 frames)";
            num_hybrid_int.ToolTip = _def + "5";
            num_hybrid_fo.ToolTip = _def + "10";
            check_fo_portions.ToolTip = _def + on;
            check_iscombed_mark.ToolTip = Languages.Translate("Print \"deinterlaced frame\" on each frame that was detected as Combed.") + "\r\n" +
                Languages.Translate("Use this option for tuning CThresh and MI, uncheck it when done!") + "\r\n\r\n" + _def + off;
            num_iscombed_cthresh.ToolTip = Languages.Translate("How strong or visible combing must be to be detected (lower values = higher sensitivity).") + "\r\n\r\n" + _def + "7";
            num_iscombed_mi.ToolTip = Languages.Translate("How many combed areas must be found to detect whole frame as Сombed.") + "\r\n\r\n" + _def + "40";
            combo_qtgmc_preset.ToolTip = _def + "Slow";
            num_qtgmc_sharp.ToolTip = _def + "1.0";
            check_original_fps.ToolTip = Languages.Translate("If checked, use the framerate of the raw video stream instead of the framerate of the container.") + "\r\n" +
                Languages.Translate("This option is meaningful only when a file is opening.") + "\r\n\r\n" + _def + off;
            check_nonstandard_fps.ToolTip = Languages.Translate("If checked, all non-standard fps will be passed on output without rounding to the nearest standard value") + ".\r\n\r\n" + _def + off;
        }

        private void button_analyse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            button_analyse.ToolTip = null;
            SourceDetector sd = new SourceDetector(m);

            if (sd.m != null)
            {
                DeinterlaceType olddeint = m.deinterlace;
                FieldOrder oldfo = m.fieldOrder;
                SourceType olditype = m.interlace;

                m = sd.m.Clone();

                if (m.deinterlace != olddeint ||
                    m.fieldOrder != oldfo ||
                    m.interlace != olditype)
                {
                    m = Format.GetOutInterlace(m);
                    m = Calculate.UpdateOutFramerate(m);
                    SetFramerateCombo(m.outframerate);

                    //обновляем форму
                    combo_fieldorder.SelectedItem = GetFixedFieldString(m.fieldOrder.ToString());
                    combo_sourcetype.SelectedItem = GetFixedSTypeString(m.interlace.ToString());

                    combo_deinterlace.SelectedValue = m.deinterlace;
                    combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

                    //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                    m = Calculate.UpdateOutFrames(m);
                    m.outfilesize = Calculate.GetEncodingSize(m);

                    Refresh();
                }

                //Выводим результаты
                if (sd.results != null)
                {
                    ToolTipService.SetShowDuration(button_analyse, 100000);
                    button_analyse.ToolTip = sd.results;
                }
            }
        }

        private void combo_sourcetype_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_sourcetype.IsDropDownOpen || combo_sourcetype.IsSelectionBoxHighlighted) && combo_sourcetype.SelectedItem != null)
            {
                m.interlace = GetEnumFromSTypeString(combo_sourcetype.SelectedItem.ToString());

                m = Format.GetOutInterlace(m);
                m = Calculate.UpdateOutFramerate(m);

                //обновляем форму
                combo_deinterlace.SelectedValue = m.deinterlace;
                combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);
                SetFramerateCombo(m.outframerate);

                //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                Refresh();
            }
        }

        private void combo_fieldorder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_fieldorder.IsDropDownOpen || combo_fieldorder.IsSelectionBoxHighlighted) && combo_fieldorder.SelectedItem != null)
            {
                m.fieldOrder = GetEnumFromFOrderString(combo_fieldorder.SelectedItem.ToString());
                Refresh();
            }
        }

        private void combo_deinterlace_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_deinterlace.IsDropDownOpen || combo_deinterlace.IsSelectionBoxHighlighted) && combo_deinterlace.SelectedItem != null)
            {
                m.deinterlace = (DeinterlaceType)Enum.GetValues(typeof(DeinterlaceType)).GetValue(combo_deinterlace.SelectedIndex);

                //запоминаем настройки
                if (m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                    m.interlace == SourceType.INTERLACED)
                {
                    if (m.deinterlace == DeinterlaceType.FieldDeinterlace ||
                        m.deinterlace == DeinterlaceType.LeakKernelDeint ||
                        m.deinterlace == DeinterlaceType.TDeint ||
                        m.deinterlace == DeinterlaceType.TDeintEDI ||
                        m.deinterlace == DeinterlaceType.TomsMoComp ||
                        m.deinterlace == DeinterlaceType.Yadif ||
                        m.deinterlace == DeinterlaceType.YadifModEDI ||
                        m.deinterlace == DeinterlaceType.YadifModEDI2 ||
                        m.deinterlace == DeinterlaceType.SmoothDeinterlace ||
                        m.deinterlace == DeinterlaceType.NNEDI ||
                        m.deinterlace == DeinterlaceType.MCBob ||
                        m.deinterlace == DeinterlaceType.QTGMC ||
                        m.deinterlace == DeinterlaceType.QTGMC_2 ||
                        m.deinterlace == DeinterlaceType.TFM)
                        Settings.Deint_Interlaced = m.deinterlace;
                }
                else if (m.interlace == SourceType.FILM || m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                    m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM)
                {
                    if (m.deinterlace == DeinterlaceType.TIVTC ||
                        m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI ||
                        m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI)
                        Settings.Deint_Film = m.deinterlace;
                }

                m = Calculate.UpdateOutFramerate(m);

                SetFramerateCombo(m.outframerate);
                combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

                //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                Refresh();
            }
        }

        private void combo_framerate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_framerate.IsDropDownOpen || combo_framerate.IsSelectionBoxHighlighted || combo_framerate.IsEditable) && combo_framerate.SelectedItem != null)
            {
                if (combo_framerate.SelectedItem.ToString().Length == 0)
                {
                    //Включаем редактирование
                    combo_framerate.IsEditable = true;
                    combo_framerate.ToolTip = Languages.Translate("Enter - apply, Esc - cancel.");
                    combo_framerate.ApplyTemplate();
                    return;
                }
                else
                {
                    m.outframerate = Calculate.GetSplittedString(combo_framerate.SelectedItem.ToString(), 0);

                    m.sampleratemodifer = Settings.SamplerateModifer;
                    m = AviSynthScripting.CreateAutoAviSynthScript(m);

                    //механизм обхода ошибок SSRC
                    if (m.sampleratemodifer == AviSynthScripting.SamplerateModifers.SSRC &&
                        m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
                    {
                        AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                        AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        if (instream.samplerate != outstream.samplerate && outstream.samplerate != null &&
                            Calculate.CheckScriptErrors(m) == "SSRC: could not resample between the two samplerates.")
                        {
                            m.sampleratemodifer = AviSynthScripting.SamplerateModifers.ResampleAudio;
                            m = AviSynthScripting.CreateAutoAviSynthScript(m);
                        }
                    }

                    //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                    m = Calculate.UpdateOutFrames(m);
                    m.outfilesize = Calculate.GetEncodingSize(m);

                    CheckFramerateModifierIsApplied();

                    p.m = m.Clone();
                    p.Refresh(m.script);
                    this.Focus();
                }

                if (combo_framerate.IsEditable)
                {
                    //Выключаем редактирование
                    combo_framerate.IsEditable = false;
                    combo_framerate.ToolTip = null;
                }
            }
        }

        private void combo_framerateconvertor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((combo_framerateconvertor.IsDropDownOpen || combo_framerateconvertor.IsSelectionBoxHighlighted) && combo_framerateconvertor.SelectedItem != null)
            {
                Settings.FramerateModifer = m.frameratemodifer = (AviSynthScripting.FramerateModifers)((ComboBoxItem)combo_framerateconvertor.SelectedItem).Content;

                m.sampleratemodifer = Settings.SamplerateModifer;
                m = AviSynthScripting.CreateAutoAviSynthScript(m);

                //механизм обхода ошибок SSRC
                if (m.sampleratemodifer == AviSynthScripting.SamplerateModifers.SSRC &&
                    m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
                {
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (instream.samplerate != outstream.samplerate && outstream.samplerate != null &&
                        Calculate.CheckScriptErrors(m) == "SSRC: could not resample between the two samplerates.")
                    {
                        m.sampleratemodifer = AviSynthScripting.SamplerateModifers.ResampleAudio;
                        m = AviSynthScripting.CreateAutoAviSynthScript(m);
                    }
                }

                //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                p.m = m.Clone();
                p.Refresh(m.script);
                this.Focus();
            }
        }

        private void combo_outinterlace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_outinterlace.IsDropDownOpen || combo_outinterlace.IsSelectionBoxHighlighted) && combo_outinterlace.SelectedItem != null)
            {
                Massive.InterlaceModes outinterlace = (Massive.InterlaceModes)Enum.Parse(typeof(Massive.InterlaceModes),
                    combo_outinterlace.SelectedItem.ToString());

                if (outinterlace == Massive.InterlaceModes.Interlaced)
                    m.deinterlace = DeinterlaceType.Disabled;
                else
                {
                    if (m.interlace == SourceType.FILM || m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                        m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM)
                        m.deinterlace = Settings.Deint_Film;
                    else if (m.interlace == SourceType.DECIMATING)
                        m.deinterlace = DeinterlaceType.TDecimate;
                    else if (m.interlace == SourceType.INTERLACED || m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED)
                        m.deinterlace = Settings.Deint_Interlaced;
                    else
                        m.deinterlace = DeinterlaceType.Disabled;
                }

                //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                m = Calculate.UpdateOutFramerate(m);
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                //обновляем форму
                combo_deinterlace.SelectedValue = m.deinterlace;
                SetFramerateCombo(m.outframerate);

                Refresh();
            }
        }

        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                p.SwitchToFullScreen();
                this.Focus();
            }
        }

        private void num_analyze_percent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_analyze_percent.IsAction)
                Settings.SD_Analyze = (double)num_analyze_percent.Value;
        }

        private void num_min_sections_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_min_sections.IsAction)
                Settings.SD_Min_Sections = (int)num_min_sections.Value;
        }

        private void num_hybrid_int_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_hybrid_int.IsAction)
                Settings.SD_Hybrid_Int = (int)num_hybrid_int.Value;
        }

        private void num_hybrid_fo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_hybrid_fo.IsAction)
                Settings.SD_Hybrid_FO = (int)num_hybrid_fo.Value;
        }

        private void check_fo_portions_Click(object sender, RoutedEventArgs e)
        {
            Settings.SD_Portions_FO = check_fo_portions.IsChecked.Value;
        }

        private void check_iscombed_mark_Click(object sender, RoutedEventArgs e)
        {
            Settings.IsCombed_Mark = check_iscombed_mark.IsChecked.Value;
            if (m != null && m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) Refresh();
        }

        private void num_iscombed_cthresh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_iscombed_cthresh.IsAction)
            {
                Settings.IsCombed_CThresh = (int)num_iscombed_cthresh.Value;
                if (m != null && m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) Refresh();
            }
        }

        private void num_iscombed_mi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_iscombed_mi.IsAction)
            {
                Settings.IsCombed_MI = (int)num_iscombed_mi.Value;
                if (m != null && m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) Refresh();
            }
        }

        private void combo_qtgmc_preset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_qtgmc_preset.IsDropDownOpen || combo_qtgmc_preset.IsSelectionBoxHighlighted) && combo_qtgmc_preset.SelectedItem != null)
            {
                Settings.QTGMC_Preset = combo_qtgmc_preset.SelectedItem.ToString();
                if (m != null && (m.deinterlace == DeinterlaceType.QTGMC || m.deinterlace == DeinterlaceType.QTGMC_2))
                    Refresh();
            }
        }

        private void num_qtgmc_sharp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_qtgmc_sharp.IsAction)
            {
                Settings.QTGMC_Sharpness = (double)num_qtgmc_sharp.Value;
                if (m != null && (m.deinterlace == DeinterlaceType.QTGMC || m.deinterlace == DeinterlaceType.QTGMC_2))
                    Refresh();
            }
        }

        private void check_original_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.MI_Original_fps = check_original_fps.IsChecked.Value;
        }

        private void check_nonstandard_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.Nonstandard_fps = check_nonstandard_fps.IsChecked.Value;

            if (m != null)
            {
                m = Format.GetOutInterlace(m);
                m = Calculate.UpdateOutFramerate(m);
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                combo_deinterlace.SelectedValue = m.deinterlace;
                SetFramerateCombo(m.outframerate);

                Refresh();
            }
        }

        private void combo_framerate_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                //Проверяем введённый текст
                double d_value = 0;
                string s_value = combo_framerate.Text.Trim().Replace(".", Calculate.DecimalSeparator);
                if (s_value.Contains(" ")) s_value = s_value.Split(new string[] { " " }, StringSplitOptions.None)[0]; //" fps"
                if (!double.TryParse(s_value, out d_value) || d_value < 5 || d_value > 200)
                {
                    //Возвращаем исходное значение
                    SetFramerateCombo(m.outframerate);
                }
                else
                {
                    //Добавляем и выбираем Item
                    SetFramerateCombo(Calculate.ConvertDoubleToPointString(d_value, 3));
                }
            }
            else if (e.Key == Key.Escape)
            {
                //Возвращаем исходное значение
                SetFramerateCombo(m.outframerate);
            }
        }

        private void combo_framerate_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            if (box.IsEditable && box.SelectedItem != null && !box.IsDropDownOpen && !box.IsMouseCaptured)
                combo_framerate_KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter));
        }

        private void SetFramerateCombo(string framerate)
        {
            framerate = framerate + " fps";
            if (!combo_framerate.Items.Contains(framerate))
                combo_framerate.Items.Add(framerate);
            combo_framerate.SelectedItem = framerate;
        }

        private void CheckFramerateModifierIsApplied()
        {
            if (m.outframerate != Calculate.GetRawOutFramerate(m))
            {
                combo_framerateconvertor.Tag = null;
            }
            else
            {
                combo_framerateconvertor.Tag = "Inactive";
            }
        }
	}
}