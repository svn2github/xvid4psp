using System;
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
	public partial class Interlace
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;

        public Interlace(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            m = mass.Clone();
            oldm = mass.Clone();
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
            text_in_framerate_value.Content = m.inframerate + " fps";
            //button_fullscreen.Content = Languages.Translate("Fullscreen");

            SetTooltips();

            //забиваем
            foreach (string fieldt in Enum.GetNames(typeof(FieldOrder)))
                combo_fieldorder.Items.Add(GetFixedFieldString(fieldt));
            combo_fieldorder.SelectedItem = GetFixedFieldString(m.fieldOrder.ToString());

            foreach (string stype in Enum.GetNames(typeof(SourceType)))
            {
                if (stype != "NOT_ENOUGH_SECTIONS")
                    combo_sourcetype.Items.Add(GetFixedSTypeString(stype));
            }
            combo_sourcetype.SelectedItem = GetFixedSTypeString(m.interlace.ToString());

            //Деинтерлейс и всплывающие подсказки к нему
            foreach (DeinterlaceType dtype in Enum.GetValues(typeof(DeinterlaceType)))
            {
                ComboBoxItem item = new ComboBoxItem();
                string cont = dtype.ToString(), tltp = "";
                double frate = Calculate.ConvertStringToDouble(m.inframerate);

                if (dtype == DeinterlaceType.TFM) tltp = "FieldMatching (for videos with ShiftedFields) using TFM";
                else if (dtype == DeinterlaceType.Yadif) tltp = "Regular deinterlacing using Yadif";
                else if (dtype == DeinterlaceType.YadifModEDI) tltp = "Regular deinterlacing using YadifMod+NNEDI2 (slow)";
                else if (dtype == DeinterlaceType.TDeint) tltp = "Regular deinterlacing using TDeint";
                else if (dtype == DeinterlaceType.TDeintEDI) tltp = "Regular deinterlacing using TDeint+EEDI2 (very slow)";
                else if (dtype == DeinterlaceType.LeakKernelDeint) tltp = "Regular deinterlacing using LeakKernelDeint";
                else if (dtype == DeinterlaceType.TomsMoComp) tltp = "Regular deinterlacing using TomsMoComp";
                else if (dtype == DeinterlaceType.FieldDeinterlace) tltp = "Regular deinterlacing using FieldDeinterlace (blending)";
                else if (dtype == DeinterlaceType.SmoothDeinterlace) { cont = "SmoothDeinterlace (x2)"; tltp = "Deinterlacing with doubling frame rate using SmoothDeinterlace"; }
                else if (dtype == DeinterlaceType.MCBob) { cont = "MCBob (x2)"; tltp = "Deinterlacing with doubling frame rate using MCBob (very slow)"; }
                else if (dtype == DeinterlaceType.QTGMC) { cont = "QTGMC (x2)"; tltp = "Deinterlacing with doubling frame rate and denoising using QTGMC (slow)"; }
                else if (dtype == DeinterlaceType.NNEDI) { cont = "NNEDI (x2)"; tltp = "Deinterlacing with doubling frame rate using NNEDI2"; }
                else if (dtype == DeinterlaceType.YadifModEDI2) { cont = "YadifModEDI (x2)"; tltp = "Deinterlacing with doubling frame rate using YadifMod+NNEDI2"; }
                else if (dtype == DeinterlaceType.TIVTC) tltp = "Inverse Telecine (remove 3:2 pulldown) using TIVTC\r\n29.970->23.976";
                else if (dtype == DeinterlaceType.TIVTC_TDeintEDI) { cont = "TIVTC+TDeintEDI"; tltp = "Inverse Telecine (remove 3:2 pulldown) using TIVTC+TDeint+NNEDI2\r\n29.970->23.976"; }
                else if (dtype == DeinterlaceType.TIVTC_YadifModEDI) { cont = "TIVTC+YadifModEDI"; tltp = "Inverse Telecine (remove 3:2 pulldown) using TIVTC+YadifMod+NNEDI2\r\n29.970->23.976"; }
                else if (dtype == DeinterlaceType.TDecimate) { cont = "TDecimate 1-in-5"; tltp = "Remove duplicate frames (remove 1 frame from every 5 frames) using TDecimate\r\n29.970->23.976"; }
                else if (dtype == DeinterlaceType.TDecimate_23)
                {
                    cont = "TDecimate to 23.976"; tltp = "Auto searching and removing duplicate frames, so frame rate will be reduced to 23.976.\r\n" +
                        "23.976 - it must be the original frame rate of the video BEFORE duplicates was added!";
                    item.IsEnabled = (frate > 23.976);
                }
                else if (dtype == DeinterlaceType.TDecimate_24)
                {
                    cont = "TDecimate to 24.000"; tltp = "Auto searching and removing duplicate frames, so frame rate will be reduced to 24.000.\r\n" +
                        "24.000 - it must be the original frame rate of the video BEFORE duplicates was added!";
                    item.IsEnabled = (frate > 24.000);
                }
                else if (dtype == DeinterlaceType.TDecimate_25)
                {
                    cont = "TDecimate to 25.000"; tltp = "Auto searching and removing duplicate frames, so frame rate will be reduced to 25.000.\r\n" +
                        "25.000 - it must be the original frame rate of the video BEFORE duplicates was added!";
                    item.IsEnabled = (frate > 25.000);
                }
                else tltp = null;

                item.Content = cont;
                item.ToolTip = tltp;
                combo_deinterlace.Items.Add(item);
            }
            SetDeintSelectedItem(m.deinterlace);

            //забиваем
            foreach (string f in Format.GetValidFrameratesList(m))
                combo_framerate.Items.Add(f + " fps");
            if (!combo_framerate.Items.Contains(m.outframerate + " fps"))
                combo_framerate.Items.Add(m.outframerate + " fps");
            combo_framerate.SelectedItem = m.outframerate + " fps";

            foreach (string ratechangers in Enum.GetNames(typeof(AviSynthScripting.FramerateModifers)))
                combo_framerateconvertor.Items.Add(ratechangers);
            combo_framerateconvertor.SelectedItem = m.frameratemodifer.ToString();

            //интерлейс режимы
            foreach (string interlace in Enum.GetNames(typeof(Massive.InterlaceModes)))
                combo_outinterlace.Items.Add(interlace);
            combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

            ShowDialog();
		}

        private void SetDeintSelectedItem(DeinterlaceType deint)
        {
            int index = 0;
            foreach (DeinterlaceType dtype in Enum.GetValues(typeof(DeinterlaceType)))
            {
                if (dtype == deint) break;
                else index += 1;
            }

            combo_deinterlace.SelectedIndex = index;
            combo_deinterlace.ToolTip = ((ComboBoxItem)combo_deinterlace.Items.GetItemAt(index)).ToolTip;
            //((ComboBoxItem)combo_deinterlace.SelectedItem).ToolTip;
        }

        private void SetComboBoxSelectedItem(ComboBox box, object value)
        {
            foreach (ComboBoxItem item in box.Items)
            {
                if (item.Content.ToString() == value.ToString())
                { 
                    box.SelectedItem = item;
                    box.ToolTip = item.ToolTip;
                    break; 
                }
            }
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
            m = oldm.Clone();
            Close();
        }

        private void Refresh()
        {
            m = AviSynthScripting.CreateAutoAviSynthScript(m);
            p.m = m.Clone();
            p.Refresh(m.script);
        }

        private void SetTooltips()
        {
            if (m.frameratemodifer == AviSynthScripting.FramerateModifers.AssumeFPS)
            {
                combo_framerateconvertor.ToolTip = "The AssumeFPS filter changes the frame rate without changing the frame count." +
                    Environment.NewLine + "(causing the video to play faster or slower)";
            }
            if (m.frameratemodifer == AviSynthScripting.FramerateModifers.ChangeFPS)
            {
                combo_framerateconvertor.ToolTip = "ChangeFPS changes the frame rate by deleting or duplicating frames.";
            }
            if (m.frameratemodifer == AviSynthScripting.FramerateModifers.ConvertFPS)
            {
                combo_framerateconvertor.ToolTip = "The filter attempts to convert the frame rate of clip to new_rate without" +
                             Environment.NewLine + "dropping or inserting frames, providing a smooth conversion with results similar" +
                             Environment.NewLine + "to those of standalone converter boxes. The output will have (almost) the same duration as clip," +
                             Environment.NewLine + "but the number of frames will change proportional to the ratio of target and source frame rates.";
            }
            if (m.frameratemodifer == AviSynthScripting.FramerateModifers.MSUFrameRate)
            {
                combo_framerateconvertor.ToolTip = "The filter is intended for video frame rate up-conversion. It increases the frame rate integer times.";
            }
        }

        private void button_analyse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
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
                    combo_framerate.SelectedItem = m.outframerate + " fps";

                    Refresh();

                    //обновляем форму
                    combo_fieldorder.SelectedItem = GetFixedFieldString(m.fieldOrder.ToString());
                    combo_sourcetype.SelectedItem = GetFixedSTypeString(m.interlace.ToString());

                    SetDeintSelectedItem(m.deinterlace);
                    combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

                    //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                    m = Calculate.UpdateOutFrames(m);
                    m.outfilesize = Calculate.GetEncodingSize(m);
                }
            }
        }

        private void combo_sourcetype_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_sourcetype.IsDropDownOpen || combo_sourcetype.IsSelectionBoxHighlighted)
            {
                m.interlace = GetEnumFromSTypeString(combo_sourcetype.SelectedItem.ToString());

                m = Format.GetOutInterlace(m);
                m = Calculate.UpdateOutFramerate(m);

                //обновляем форму
                SetDeintSelectedItem(m.deinterlace);
                combo_framerate.SelectedItem = m.outframerate + " fps";
                combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

                //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                Refresh();
            }
        }

        private void combo_fieldorder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_fieldorder.IsDropDownOpen || combo_fieldorder.IsSelectionBoxHighlighted)
            {
                m.fieldOrder = GetEnumFromFOrderString(combo_fieldorder.SelectedItem.ToString());
                Refresh();
            }
        }

        private void combo_deinterlace_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_deinterlace.IsDropDownOpen || combo_deinterlace.IsSelectionBoxHighlighted)
            {
                combo_deinterlace.ToolTip = ((ComboBoxItem)combo_deinterlace.SelectedItem).ToolTip;
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

                combo_framerate.SelectedItem = m.outframerate + " fps";
                combo_outinterlace.SelectedItem = Format.GetCodecOutInterlace(m);

                //обновляем конечное колличество фреймов, с учётом режима деинтерелейса
                m = Calculate.UpdateOutFrames(m);
                m.outfilesize = Calculate.GetEncodingSize(m);

                Refresh();
            }
        }

        private void combo_framerate_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_framerate.IsDropDownOpen || combo_framerate.IsSelectionBoxHighlighted)
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

                p.m = m.Clone();
                p.Refresh(m.script);
            }
        }

        private void combo_framerateconvertor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_framerateconvertor.IsDropDownOpen || combo_framerateconvertor.IsSelectionBoxHighlighted)
            {
                m.frameratemodifer = (AviSynthScripting.FramerateModifers)Enum.Parse(typeof(AviSynthScripting.FramerateModifers),
                    combo_framerateconvertor.SelectedItem.ToString());
                Settings.FramerateModifer = m.frameratemodifer;

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
                SetTooltips();
            }
        }

        private void combo_outinterlace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_outinterlace.IsDropDownOpen || combo_outinterlace.IsSelectionBoxHighlighted)
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
                SetDeintSelectedItem(m.deinterlace);
                combo_framerate.SelectedItem = m.outframerate + " fps";

                //обновляем скрипт
                m = AviSynthScripting.CreateAutoAviSynthScript(m);

                p.m = m.Clone();
                p.Refresh(m.script);
            }
        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            p.SwitchToFullScreen();
        }
	}
}