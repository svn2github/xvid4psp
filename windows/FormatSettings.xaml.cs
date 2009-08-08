using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace XviD4PSP
{
	public partial class FormatSettings
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;
        public string fstream;
        private string format;
        
        public FormatSettings(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            m = mass.Clone();
            oldm = mass.Clone();
            p = parent;
            Owner = p;

            format = m.format.ToString();

            textbox_vcodec_list.Text = FormatReader.GetFormatInfo(format, "GetVCodecsList");
            textbox_acodec_list.Text = FormatReader.GetFormatInfo(format, "GetACodecsList");

            combo_VCodec.Items.Add("x264");
            combo_VCodec.Items.Add("MPEG1");
            combo_VCodec.Items.Add("MPEG2");
            combo_VCodec.Items.Add("MPEG4");
            combo_VCodec.Items.Add("XviD");
            combo_VCodec.Items.Add("FLV1");
            combo_VCodec.Items.Add("DV");
            combo_VCodec.SelectedItem = FormatReader.GetFormatInfo(format, "GetVCodec");

            combo_ACodec.Items.Add("PCM");
            combo_ACodec.Items.Add("AAC");
            combo_ACodec.Items.Add("AC3");
            combo_ACodec.Items.Add("MP2");
            combo_ACodec.Items.Add("MP3");
            combo_ACodec.SelectedItem = FormatReader.GetFormatInfo(format, "GetACodec");

            textbox_framerates_list.Text = FormatReader.GetFormatInfo(format, "GetValidFrameratesList");
            textbox_samplerates_list.Text = FormatReader.GetFormatInfo(format, "GetValidSampleratesList");

            combo_Muxer.Items.Add("pmpavc");
            combo_Muxer.Items.Add("mkvmerge");
            combo_Muxer.Items.Add("ffmpeg");
            combo_Muxer.Items.Add("tsmuxer");
            combo_Muxer.Items.Add("dpgmuxer");
            combo_Muxer.Items.Add("mp4box");
            combo_Muxer.Items.Add("virtualdubmod");
            combo_Muxer.Items.Add("disabled");
            combo_Muxer.SelectedItem = FormatReader.GetFormatInfo(format, "GetMuxer");

            combo_Extension.Items.Add(".mp4");
            combo_Extension.Items.Add(".mkv");
            combo_Extension.Items.Add(".3gp");
            combo_Extension.Items.Add(".mov");
            combo_Extension.Items.Add(".pmp");
            combo_Extension.Items.Add(".flv");
            combo_Extension.Items.Add(".avi");
            combo_Extension.Items.Add(".mpg");
            combo_Extension.Items.Add(".ts");
            combo_Extension.Items.Add(".m2ts");
            combo_Extension.Items.Add(".dpg");

            combo_Extension.SelectedItem = FormatReader.GetFormatInfo(format, "GetValidExtension");

            int n = 16;
            int step = 16;
            while (n < 1920 + step)
            {
                combo_MaxResolutionW.Items.Add(n.ToString());
                n = n + step;
            }
            combo_MaxResolutionW.SelectedItem = FormatReader.GetFormatInfo(format, "MaxResolutionW");

            n = 16;
            step = Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "GetResolutionHMod"));
            while (n < 1088 + step)
            {
                combo_MaxResolutionH.Items.Add(n.ToString());
                n = n + step;
            }
            combo_MaxResolutionH.SelectedItem = FormatReader.GetFormatInfo(format, "MaxResolutionH");

            n = 16;
            step = 16;
            while (n< 1920 + step)
            {
                combo_MinResolutionW.Items.Add(n.ToString());
                n = n + step;
            }
            combo_MinResolutionW.SelectedItem = FormatReader.GetFormatInfo(format, "MinResolutionW");

            n = 16;
            step = Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "GetResolutionHMod"));
            while (n < 1088 + step)
            {
                combo_MinResolutionH.Items.Add(n.ToString());
                n = n + step;
            }
            combo_MinResolutionH.SelectedItem = FormatReader.GetFormatInfo(format, "MinResolutionH");
           
            if (FormatReader.GetFormatInfo(format, "IsLockedOutAspect") == "yes")
                check_fixed_ar.IsChecked = true;
            else
                check_fixed_ar.IsChecked = false;

            textbox_aspects.Text = FormatReader.GetFormatInfo(format, "GetValidOutAspects");

            if (FormatReader.GetFormatInfo(format, "Is4GBlimitedFormat") == "yes")
                check_4gb_only.IsChecked = true;
            else
                check_4gb_only.IsChecked = false;


            TranslateItems();
            SetTooltips();

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //Начинаем переписывать ини-файл o_O
            try
            {
                string line;
                using (StreamReader reader = new StreamReader(Calculate.StartupPath + "\\FormatSettings.ini", System.Text.Encoding.Default))
                {
                    while (!reader.EndOfStream)
                    {
                        //Считываем построчно, ищем в текущей строчке совпадение с одним из параметров, если находим - меняем его значение на текущее, и пересобираем текст в новую переменную o_O
                        line = reader.ReadLine();
                        if (line.StartsWith("\\" + format + "\\GetVCodecsList\\"))
                            line = "\\" + format + "\\GetVCodecsList\\" + textbox_vcodec_list.Text.Trim() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetVCodec\\"))
                            line = "\\" + format + "\\GetVCodec\\" + combo_VCodec.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetACodecsList\\"))
                            line = "\\" + format + "\\GetACodecsList\\" + textbox_acodec_list.Text.Trim() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetACodec\\"))
                            line = "\\" + format + "\\GetACodec\\" + combo_ACodec.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetValidSampleratesList\\"))
                            line = "\\" + format + "\\GetValidSampleratesList\\" + textbox_samplerates_list.Text.Trim() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetValidFrameratesList\\"))
                            line = "\\" + format + "\\GetValidFrameratesList\\" + textbox_framerates_list.Text.Trim() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetValidExtension\\"))
                            line = "\\" + format + "\\GetValidExtension\\" + combo_Extension.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\GetMuxer\\"))
                            line = "\\" + format + "\\GetMuxer\\" + combo_Muxer.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\MinResolutionW\\"))
                            line = "\\" + format + "\\MinResolutionW\\" + combo_MinResolutionW.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\MinResolutionH\\"))
                            line = "\\" + format + "\\MinResolutionH\\" + combo_MinResolutionH.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\MaxResolutionW\\"))
                            line = "\\" + format + "\\MaxResolutionW\\" + combo_MaxResolutionW.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\MaxResolutionH\\"))
                            line = "\\" + format + "\\MaxResolutionH\\" + combo_MaxResolutionH.SelectedItem.ToString() + Environment.NewLine;
                        else if (line.StartsWith("\\" + format + "\\IsLockedOutAspect\\"))
                        {
                            string logval = "no";
                            if (check_fixed_ar.IsChecked == true) logval = "yes";
                            line = "\\" + format + "\\IsLockedOutAspect\\" + logval + Environment.NewLine;
                        }
                        else if (line.StartsWith("\\" + m.format + "\\GetValidOutAspects\\"))
                            line = "\\" + format + "\\GetValidOutAspects\\" + textbox_aspects.Text.Trim() + Environment.NewLine;
                        else if (line.StartsWith("\\" + m.format + "\\Is4GBlimitedFormat\\"))
                        {
                            string logval = "no";
                            if (check_4gb_only.IsChecked == true) logval = "yes";
                            line = "\\" + format + "\\Is4GBlimitedFormat\\" + logval + Environment.NewLine;
                        }
                        else
                            if (!reader.EndOfStream)
                                line = line + Environment.NewLine;

                        fstream += line;
                    }
                    reader.Dispose();
                    reader.Close();
                }
                //Пишем в файл..
                FileStream strm = new FileStream(Calculate.StartupPath + "\\FormatSettings.ini", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter writer = new StreamWriter(strm, System.Text.Encoding.Default);
                writer.WriteLine(fstream);
                writer.Flush();
                //writer.Close();
                //strm.Close();
                writer.Dispose();
                strm.Dispose();
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }

            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //m = oldm.Clone();
            Close();
        }

        private void TranslateItems()
        {

            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            Title = Languages.Translate ("Edit format");
            group_format.Header = Title + ": " + Convert.ToString(m.format);

            vcodeclist.Content = Languages.Translate("Valid video codecs:");
            getvcodec.Content = Languages.Translate("Usualy used:");
            framerateslist.Content = Languages.Translate("Valid framerates:");
            acodeclist.Content = Languages.Translate("Valid audio codecs:");
            getacodec.Content = Languages.Translate("Usualy used:");
            samplerateslist.Content = Languages.Translate("Valid samplerates:");
            validresolution.Content = Languages.Translate("Resolution:");
            check_fixed_ar.Content = "fix AR:"; //Languages.Translate("Use only AR`s, specified below:");
            validmuxer.Content = Languages.Translate("Muxer fot this format:");
            validextension.Content = Languages.Translate("File extension:");
            check_4gb_only.Content = Languages.Translate("Maximum filesize is 4Gb");

        }
        private void SetTooltips()
        {
            textbox_vcodec_list.ToolTip = Languages.Translate("Codecs that will be selectable in the video-codecs-setting window.") + Environment.NewLine + Languages.Translate("Valid values: x264, MPEG1, MPEG2, MPEG4, FLV1, MJPEG, HUFF, FFV1, XviD, DV, Copy");
            combo_VCodec.ToolTip = Languages.Translate("Codec, that usualy used in this format (or maybe i`m wrong)");
            textbox_framerates_list.ToolTip = Languages.Translate("Framerates, that can be set for this format.") + Environment.NewLine + Languages.Translate("valid values: 15.000, 18.000, 20.000, 23.976, 24.000, 25.000, 29.970, 30.000, 50.000, 59.940, 60.000, 120.000");
            textbox_acodec_list.ToolTip = Languages.Translate("Codecs that will be selectable in the audio-codecs-setting window.") + Environment.NewLine + Languages.Translate("valid values: PCM, AAC, MP2, MP3, AC3, Disabled, Copy");
            combo_ACodec.ToolTip = Languages.Translate("Codec, that usualy used in this format (or maybe i`m wrong)");
            textbox_samplerates_list.ToolTip = Languages.Translate("Samplerates, that can be set for this format.") + Environment.NewLine + Languages.Translate("valid values: 22050, 32000, 44100, 48000, 96000, 192000");
            validresolution.ToolTip = Languages.Translate("Resolution:");
            check_fixed_ar.ToolTip = Languages.Translate("DO NOT USE THIS OPTION!") + Environment.NewLine +  Languages.Translate("If this format can have only specified (by next setting) AR - then yes, if it can have any AR - then no");
            combo_Muxer.ToolTip = Languages.Translate("Muxer, that will be used for this format.");
            combo_Extension.ToolTip = Languages.Translate("File extension:");
            check_4gb_only.ToolTip = Languages.Translate("Maximum filesize is 4Gb");
            textbox_aspects.ToolTip = Languages.Translate("valid values: 1.333 (4:3), 1.500, 1.666, 1.765 (16:9), 1.778 (16:9), 1.850, 2.353, or any...");

        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void combo_VCodec_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {

        }

        private void combo_ACodec_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {

        }

        private void combo_Muxer_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {

        }

        private void combo_Extension_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {

        }

        private void combo_MaxResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MaxResolutionW.IsDropDownOpen)
            {
                if (Convert.ToInt32(combo_MaxResolutionW.SelectedItem) < Convert.ToInt32(combo_MinResolutionW.SelectedItem))
                    combo_MaxResolutionW.SelectedItem = combo_MinResolutionW.SelectedItem;
            }
        }
        private void combo_MaxResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MaxResolutionH.IsDropDownOpen)
            {
                if (Convert.ToInt32(combo_MaxResolutionH.SelectedItem) < Convert.ToInt32(combo_MinResolutionH.SelectedItem))
                    combo_MaxResolutionH.SelectedItem = combo_MinResolutionH.SelectedItem;
            }
        }

        private void combo_MinResolutionW_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MinResolutionW.IsDropDownOpen)
            {
                if (Convert.ToInt32(combo_MinResolutionW.SelectedItem) > Convert.ToInt32(combo_MaxResolutionW.SelectedItem))
                    combo_MinResolutionW.SelectedItem = combo_MaxResolutionW.SelectedItem;
            }
        }
        private void combo_MinResolutionH_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (combo_MinResolutionH.IsDropDownOpen)
            {
                if (Convert.ToInt32(combo_MinResolutionH.SelectedItem) > Convert.ToInt32(combo_MaxResolutionH.SelectedItem))
                    combo_MinResolutionH.SelectedItem = combo_MaxResolutionH.SelectedItem;
            }
        }
      
        private void check_Fixed_AR_Clicked(object sender, RoutedEventArgs e)
        {

        }

        private void check_4Gb_Only_Clicked(object sender, RoutedEventArgs e)
        {

        }

        private void check_Anamorph_IsPosible_Clicked(object sender, RoutedEventArgs e)
        {

        }

        private void check_Interlace_IsPosible_Clicked(object sender, RoutedEventArgs e)
        {

        }

	}
}