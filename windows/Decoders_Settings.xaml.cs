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

namespace XviD4PSP
{
	public partial class Decoders_Settings
	{
        public Massive m;
        private string old_raw_script;
        public bool NeedUpdate = false;
        
        public Decoders_Settings(Massive mass, System.Windows.Window owner, int set_focus_to)
		{
			this.InitializeComponent();

            Owner = owner;

            if (mass != null)
            {
                m = mass.Clone();

                //Скрипт для определения изменений
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
                old_raw_script = m.script;
            }

            //переводим
            Title = Languages.Translate("Decoding");
            button_ok.Content = Languages.Translate("OK");
            text_other.Content = Languages.Translate("Other files") + ":";
            check_dss_audio.ToolTip = Languages.Translate("Allow DirectShowSource to decode audio directly from the source-file (without demuxing).");
            check_force_film.Content = Languages.Translate("Auto force Film at") + " (%):";
            check_force_film.ToolTip = Languages.Translate("Auto force Film if Film percentage is more than selected value (for NTSC sources only)");
            check_ffms2.Content = Languages.Translate("Use new FFmpegSource2");
            check_ffms2.ToolTip = Languages.Translate("Choose what kind of FFmpegSource (old or new) will be used for decoding associated file types");
            check_ffms_force_fps.ToolTip = Languages.Translate("Force FPS");
            check_ffms_audio.ToolTip = check_dss_audio.ToolTip.ToString().Replace("DirectShowSource", "FFmpegSource") + "\r\n" +
                Languages.Translate("Note: FFmpegSource1 will decode audio to RAW-data file, so it can take a lot of space on your HDD.");
            check_new_delay.Content = Languages.Translate("Use new Delay calculation method");
            check_new_delay.ToolTip = Languages.Translate("A new method uses the difference between video and audio delays, while old method uses audio delay only."); //+
                //"\r\n" + Languages.Translate("This new method can be helpfull for the FFmpegSource decoders, but harmful for the DirectShowSource.");
            check_copy_delay.Content = Languages.Translate("Apply Delay in Copy mode");

            text_avi.ToolTip = "avi";
            text_mkv.ToolTip = "mkv";
            text_mp4.ToolTip = "mp4, m4v";
            text_mpg.ToolTip = "various mpeg-files";

            text_ac3.ToolTip = "ac3";
            text_wav.ToolTip = "wav, w64";
            text_mpa.ToolTip = "mp1, mp2, mpa";
            text_mp3.ToolTip = "mp3";

            //Video
            combo_avi_dec.Items.Add("AVISource");
            combo_avi_dec.Items.Add("DirectShowSource");
            combo_avi_dec.Items.Add("DirectShowSource2");
            combo_avi_dec.Items.Add("FFmpegSource");

            combo_mkv_dec.Items.Add("DirectShowSource");
            combo_mkv_dec.Items.Add("DirectShowSource2");
            combo_mkv_dec.Items.Add("FFmpegSource");

            combo_mp4_dec.Items.Add("DirectShowSource");
            combo_mp4_dec.Items.Add("DirectShowSource2");
            combo_mp4_dec.Items.Add("FFmpegSource");

            combo_mpg_dec.Items.Add("DirectShowSource");
            combo_mpg_dec.Items.Add("DirectShowSource2");
            combo_mpg_dec.Items.Add("FFmpegSource");
            combo_mpg_dec.Items.Add("Mpeg2Source");

            combo_oth_dec.Items.Add("DirectShowSource");
            combo_oth_dec.Items.Add("DirectShowSource2");
            combo_oth_dec.Items.Add("FFmpegSource");

            //Audio
            combo_ac3_dec.Items.Add("BassAudioSource");
            combo_ac3_dec.Items.Add("NicAC3Source");

            combo_mpa_dec.Items.Add("BassAudioSource");
            combo_mpa_dec.Items.Add("NicMPG123Source");

            combo_mp3_dec.Items.Add("BassAudioSource");
            combo_mp3_dec.Items.Add("NicMPG123Source");

            combo_wav_dec.Items.Add("BassAudioSource");
            combo_wav_dec.Items.Add("RaWavSource");
            combo_wav_dec.Items.Add("WavSource");

            //DirectShowSource
            check_dss_convert_fps.IsChecked = Settings.DSS_ConvertFPS;
            check_dss_audio.IsChecked = Settings.DSS_Enable_Audio;
            
            //Mpeg2Source
            check_force_film.IsChecked = Settings.DGForceFilm;
            num_force_film.Value = Settings.DGFilmPercent;

            //FFmpegSource
            check_ffms2.IsChecked = Settings.FFmpegSource2;
            check_ffms_force_fps.IsChecked = Settings.FFmpegAssumeFPS;
            check_ffms_audio.IsChecked = Settings.FFMS_Enable_Audio;

            //Audio
            check_new_delay.IsChecked = Settings.NewDelayMethod;
            check_copy_delay.IsChecked = Settings.CopyDelay;

            SetDecoders();
            
            //Тултипы для декодеров
            SetVDecoderToolTip(combo_avi_dec);
            SetVDecoderToolTip(combo_mkv_dec);
            SetVDecoderToolTip(combo_mp4_dec);
            SetVDecoderToolTip(combo_mpg_dec);
            SetVDecoderToolTip(combo_oth_dec);

            //Переводим фокус
            if (set_focus_to == 2) tab_audio.Focus();

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                //Прячем индекс-файл для переключения МПЕГ-декодера
                string ext = Path.GetExtension(m.infilepath).ToLower();
                if (m.isvideo && ext != ".d2v" && Calculate.IsMPEG(m.infilepath) && m.invcodecshort != "h264")
                {
                    if (Settings.MPEGDecoder == AviSynthScripting.Decoders.MPEG2Source && m.oldindexfile != null)
                    {
                        m.indexfile = m.oldindexfile;
                        m.oldindexfile = null;
                    }
                    else if (Settings.MPEGDecoder != AviSynthScripting.Decoders.MPEG2Source && m.indexfile != null)
                    {
                        m.oldindexfile = m.indexfile;
                        m.indexfile = null;
                    }
                }

                //Новый видео декодер
                m = Format.GetValidVDecoder(m);

                //Новый аудио декодер
                foreach (object o in m.inaudiostreams)
                {
                    AudioStream s = (AudioStream)o;
                    s = Format.GetValidADecoder(s);
                }

                //Новый скрипт
                m = AviSynthScripting.CreateAutoAviSynthScript(m);

                //Проверяем, изменился ли скрипт
                NeedUpdate = (old_raw_script != m.script);
            }
            
            Close();
        }

        private void SetDecoders()
        {
            string dec = Settings.AVIDecoder.ToString();
            if (dec == "AVISource") combo_avi_dec.SelectedIndex = 0;
            else if (dec == "DirectShowSource") combo_avi_dec.SelectedIndex = 1;
            else if (dec == "DSS2") combo_avi_dec.SelectedIndex = 2;
            else combo_avi_dec.SelectedIndex = 3;

            dec = Settings.MPEGDecoder.ToString();
            if (dec == "DirectShowSource") combo_mpg_dec.SelectedIndex = 0;
            else if (dec == "DSS2") combo_mpg_dec.SelectedIndex = 1;
            else if (dec == "FFmpegSource") combo_mpg_dec.SelectedIndex = 2;
            else combo_mpg_dec.SelectedIndex = 3;

            dec = Settings.MP4Decoder.ToString();
            if (dec == "DirectShowSource") combo_mp4_dec.SelectedIndex = 0;
            else if (dec == "DSS2") combo_mp4_dec.SelectedIndex = 1;
            else combo_mp4_dec.SelectedIndex = 2;

            dec = Settings.MKVDecoder.ToString();
            if (dec == "DirectShowSource") combo_mkv_dec.SelectedIndex = 0;
            else if (dec == "DSS2") combo_mkv_dec.SelectedIndex = 1;
            else combo_mkv_dec.SelectedIndex = 2;

            dec = Settings.OtherDecoder.ToString();
            if (dec == "DirectShowSource") combo_oth_dec.SelectedIndex = 0;
            else if (dec == "DSS2") combo_oth_dec.SelectedIndex = 1;
            else combo_oth_dec.SelectedIndex = 2;

            dec = Settings.AC3Decoder.ToString();
            if (dec == "bassAudioSource") combo_ac3_dec.SelectedIndex = 0;
            else combo_ac3_dec.SelectedIndex = 1;

            dec = Settings.WAVDecoder.ToString();
            if (dec == "bassAudioSource") combo_wav_dec.SelectedIndex = 0;
            else if (dec == "RaWavSource") combo_wav_dec.SelectedIndex = 1;
            else combo_wav_dec.SelectedIndex = 2;
            
            dec = Settings.MPADecoder.ToString();
            if (dec == "bassAudioSource") combo_mpa_dec.SelectedIndex = 0;
            else combo_mpa_dec.SelectedIndex = 1;

            dec = Settings.MP3Decoder.ToString();
            if (dec == "bassAudioSource") combo_mp3_dec.SelectedIndex = 0;
            else combo_mp3_dec.SelectedIndex = 1;
        }

        private void SetVDecoderToolTip(ComboBox box)
        {
            if (box.SelectedItem == null) return;

            string dec = box.SelectedItem.ToString();
            if (dec == "DirectShowSource")
                box.ToolTip = Languages.Translate("This decoder uses installed on your system DirecShow filters-decoders (and theirs settings!) for audio and video decoding.");
            else if (dec == "DirectShowSource2")
                box.ToolTip = Languages.Translate("Mostly the same as DirectShowSource, but from Haali. It provides frame-accuracy seeking and don`t use your system decoders for audio.") +
                    Environment.NewLine + Languages.Translate("Path to the source file must not contains cyrillic and some other symbols!");
            else if (dec == "FFmpegSource")
                box.ToolTip = Languages.Translate("This decoder (old or new) is fully independent from your system decoders and theirs settings, but needs some time for indexing video (especially new FFmpegSource2).");
            else if (dec == "Mpeg2Source")
                box.ToolTip = Languages.Translate("Probably the best decoder for MPEG-files. Fully independent and frame-accurate.");
            else
                box.ToolTip = null;
        }

        private void combo_avi_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_avi_dec.IsDropDownOpen || combo_avi_dec.IsSelectionBoxHighlighted)
            {
                if (combo_avi_dec.SelectedIndex == 0) Settings.AVIDecoder = AviSynthScripting.Decoders.AVISource;
                else if (combo_avi_dec.SelectedIndex == 1) Settings.AVIDecoder = AviSynthScripting.Decoders.DirectShowSource;
                else if (combo_avi_dec.SelectedIndex == 2) Settings.AVIDecoder = AviSynthScripting.Decoders.DSS2;
                else Settings.AVIDecoder = AviSynthScripting.Decoders.FFmpegSource;

                SetVDecoderToolTip(combo_avi_dec);
            }
        }

        private void combo_mkv_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mkv_dec.IsDropDownOpen || combo_mkv_dec.IsSelectionBoxHighlighted)
            {
                if (combo_mkv_dec.SelectedIndex == 0) Settings.MKVDecoder = AviSynthScripting.Decoders.DirectShowSource;
                else if (combo_mkv_dec.SelectedIndex == 1) Settings.MKVDecoder = AviSynthScripting.Decoders.DSS2;
                else Settings.MKVDecoder = AviSynthScripting.Decoders.FFmpegSource;

                SetVDecoderToolTip(combo_mkv_dec);
            }
        }

        private void combo_mp4_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mp4_dec.IsDropDownOpen || combo_mp4_dec.IsSelectionBoxHighlighted)
            {
                if (combo_mp4_dec.SelectedIndex == 0) Settings.MP4Decoder = AviSynthScripting.Decoders.DirectShowSource;
                else if (combo_mp4_dec.SelectedIndex == 1) Settings.MP4Decoder = AviSynthScripting.Decoders.DSS2;
                else Settings.MP4Decoder = AviSynthScripting.Decoders.FFmpegSource;

                SetVDecoderToolTip(combo_mp4_dec);
            }
        }

        private void combo_mpg_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mpg_dec.IsDropDownOpen || combo_mpg_dec.IsSelectionBoxHighlighted)
            {
                if (combo_mpg_dec.SelectedIndex == 0) Settings.MPEGDecoder = AviSynthScripting.Decoders.DirectShowSource;
                else if (combo_mpg_dec.SelectedIndex == 1) Settings.MPEGDecoder = AviSynthScripting.Decoders.DSS2;
                else if (combo_mpg_dec.SelectedIndex == 2) Settings.MPEGDecoder = AviSynthScripting.Decoders.FFmpegSource;
                else Settings.MPEGDecoder = AviSynthScripting.Decoders.MPEG2Source;

                SetVDecoderToolTip(combo_mpg_dec);
            }
        }

        private void combo_oth_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_oth_dec.IsDropDownOpen || combo_oth_dec.IsSelectionBoxHighlighted)
            {
                if (combo_oth_dec.SelectedIndex == 0) Settings.OtherDecoder = AviSynthScripting.Decoders.DirectShowSource;
                else if (combo_oth_dec.SelectedIndex == 1) Settings.OtherDecoder = AviSynthScripting.Decoders.DSS2;
                else Settings.OtherDecoder = AviSynthScripting.Decoders.FFmpegSource;

                SetVDecoderToolTip(combo_oth_dec);
            }
        }

        private void combo_ac3_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_ac3_dec.IsDropDownOpen || combo_ac3_dec.IsSelectionBoxHighlighted)
            {
                if (combo_ac3_dec.SelectedIndex == 0) Settings.AC3Decoder = AviSynthScripting.Decoders.bassAudioSource;
                else Settings.AC3Decoder = AviSynthScripting.Decoders.NicAC3Source;
            }
        }

        private void combo_wav_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_wav_dec.IsDropDownOpen || combo_wav_dec.IsSelectionBoxHighlighted)
            {
                if (combo_wav_dec.SelectedIndex == 0) Settings.WAVDecoder = AviSynthScripting.Decoders.bassAudioSource;
                else if (combo_wav_dec.SelectedIndex == 1) Settings.WAVDecoder = AviSynthScripting.Decoders.RaWavSource;
                else Settings.WAVDecoder = AviSynthScripting.Decoders.WAVSource;
            }
        }

        private void combo_mpa_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mpa_dec.IsDropDownOpen || combo_mpa_dec.IsSelectionBoxHighlighted)
            {
                if (combo_mpa_dec.SelectedIndex == 0) Settings.MPADecoder = AviSynthScripting.Decoders.bassAudioSource;
                else Settings.MPADecoder = AviSynthScripting.Decoders.NicMPG123Source;
            }
        }   

        private void combo_mp3_dec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mp3_dec.IsDropDownOpen || combo_mp3_dec.IsSelectionBoxHighlighted)
            {
                if (combo_mp3_dec.SelectedIndex == 0) Settings.MP3Decoder = AviSynthScripting.Decoders.bassAudioSource;
                else Settings.MP3Decoder = AviSynthScripting.Decoders.NicMPG123Source;
            }
        }
       
        private void num_force_film_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_force_film.IsAction)
            {
                Settings.DGFilmPercent = Convert.ToInt32(num_force_film.Value);
            }
        }

        private void check_dss_convert_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.DSS_ConvertFPS = check_dss_convert_fps.IsChecked.Value;
        }

        private void check_dss_audio_Click(object sender, RoutedEventArgs e)
        {
            Settings.DSS_Enable_Audio = check_dss_audio.IsChecked.Value;
        }

        private void check_force_film_Click(object sender, RoutedEventArgs e)
        {
            Settings.DGForceFilm = check_force_film.IsChecked.Value;
        }

        private void check_ffms2_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFmpegSource2 = check_ffms2.IsChecked.Value;
        }

        private void check_ffms_force_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFmpegAssumeFPS = check_ffms_force_fps.IsChecked.Value;
        }

        private void check_ffms_audio_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFMS_Enable_Audio = check_ffms_audio.IsChecked.Value;
        }

        private void check_new_delay_Click(object sender, RoutedEventArgs e)
        {
            Settings.NewDelayMethod = check_new_delay.IsChecked.Value;
        }
        
        private void check_copy_delay_Click(object sender, RoutedEventArgs e)
        {
            Settings.CopyDelay = check_copy_delay.IsChecked.Value;
        }
	}
}