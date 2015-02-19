﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace XviD4PSP
{
	public partial class AudioOptions
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;
        private AudioOptionsModes mode;
        public enum AudioOptionsModes { AllOptions, TracksOnly };

        public enum ChannelConverters { 
            KeepOriginalChannels,
            ConvertToMono,
            ConvertToStereo,
            ConvertToDolbyProLogic,
            ConvertToDolbyProLogicII,
            ConvertToDolbyProLogicIILFE,
            ConvertToUpmixDialog,
            ConvertToUpmixAction,
            ConvertToUpmixGerzen,
            ConvertToUpmixFarina,
            ConvertToUpmixMultisonic,
            ConvertToUpmixSoundOnSound
        };

        public AudioOptions(Massive mass, MainWindow parent, AudioOptionsModes mode)
        {
            this.InitializeComponent();

            this.mode = mode;
            p = parent;
            Owner = p;

            DDHelper ddh = new DDHelper(this);
            ddh.GotFiles += new DDEventHandler(DD_GotFiles);

            //загружаем фарш в форму
            Reload(mass);

            //прописываем события
            this.num_delay.ValueChanged += new RoutedPropertyChangedEventHandler<decimal>(num_delay_ValueChanged);

            if (mode == AudioOptionsModes.AllOptions)
                Show();
            else
                ShowDialog();
        }

        public void Reload(Massive mass)
        {
            m = mass.Clone();
            oldm = mass.Clone();

            ////забиваем необходимые декодеры
            //if (m.vdecoder == 0)
            //    m = Format.GetValidVDecoder(m);
            //if (instream.decoder == 0)
            //    instream = Format.GetValidADecoder(instream);

            //перевод
            group_audio.Header = Languages.Translate("Source");
            label_apath.Content = Languages.Translate("Path") + ":";
            label_atrack.Content = Languages.Translate("Track") + ":";
            this.Title = Languages.Translate("Editing audio options") + ":";
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_apply.Content = Languages.Translate("Apply");
            group_delay.Header = Languages.Translate("Delay");
            group_info.Header = Languages.Translate("Info");
            group_samplerate.Header = Languages.Translate("Samplerate");
            group_volume.Header = Languages.Translate("Volume");
            group_channels.Header = Languages.Translate("Channels");
            label_delayout.Content = Languages.Translate("Output") + ":";
            label_outsamplerate.Content = Languages.Translate("Output") + ":";
            label_converter.Content = Languages.Translate("Converter") + ":";
            label_accurate.Content = Languages.Translate("Analyse") + ":";
            //label_mixing.Content = Languages.Translate("Mixing") + ":";

            //путь к звуковой дорожке
            combo_atracks.Items.Clear();
            if (m.inaudiostreams.Count > 0)
            {
                //забиваем в список каналы звука
                int n = 1;
                foreach (object o in m.inaudiostreams)
                {
                    AudioStream s = (AudioStream)o;
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = n.ToString("00") + ". " + s.language + " " + s.codecshort + " " + s.channels + "ch";
                    item.ToolTip = item.Content + " " + s.samplerate + "Hz " + s.bitrate + "kbps " + s.delay + "ms";
                    combo_atracks.Items.Add(item);
                    n++;
                }
                combo_atracks.SelectedIndex = m.inaudiostream;

                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (instream.audiopath == null && instream.badmixing)
                {
                    string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                    instream.audiopath = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + outext;
                    instream.audiofiles = new string[] { instream.audiopath };
                    instream = Format.GetValidADecoder(instream);
                }

                if (instream.audiopath != null)
                    textbox_apath.Text = instream.audiopath;
            }

            check_apply_delay.IsChecked = Settings.ApplyDelay;

            //только для all режима
            if (mode == AudioOptionsModes.AllOptions)
            {
                //прописываем samplerate
                combo_samplerate.Items.Clear();
                foreach (string f in Format.GetValidSampleratesList(m))
                    combo_samplerate.Items.Add(f);

                combo_converter.Items.Clear();
                foreach (AviSynthScripting.SamplerateModifers ratechangers in Enum.GetValues(typeof(AviSynthScripting.SamplerateModifers)))
                    combo_converter.Items.Add(new ComboBoxItem() { Content = ratechangers });

                //прописываем громкость
                combo_volume.Items.Clear();
                combo_volume.Items.Add("Disabled");
                for (int y = 30; y < 401; y += 10)
                    combo_volume.Items.Add(y + "%");

                //прописываем аккуратность определения громкости
                combo_accurate.Items.Clear();
                for (int y = 1; y <= 9; y++)
                    combo_accurate.Items.Add(y + "%");
                for (int y = 10; y <= 100; y += 10)
                    combo_accurate.Items.Add(y + "%");

                //прогружаем работу с каналами
                combo_mixing.Items.Clear();
                foreach (string channelschanger in Enum.GetNames(typeof(ChannelConverters)))
                    combo_mixing.Items.Add(EnumMixingToString((ChannelConverters)Enum.Parse(typeof(ChannelConverters), channelschanger)));

                if (m.outaudiostreams.Count > 0)
                {
                    if (((AudioStream)m.outaudiostreams[m.outaudiostream]).codec == "Copy")
                    {
                        group_delay.IsEnabled = Settings.CopyDelay;
                        group_channels.IsEnabled = group_samplerate.IsEnabled = group_volume.IsEnabled = false;
                    }
                    else
                    {
                        group_delay.IsEnabled = group_channels.IsEnabled = group_samplerate.IsEnabled = group_volume.IsEnabled = true;
                    }
                }
                else
                {
                    group_delay.IsEnabled = group_channels.IsEnabled = group_samplerate.IsEnabled = group_volume.IsEnabled = false;
                }
            }
            //закончился режим all
            else
            {
                //режим только для выбора треков
                this.Title = Languages.Translate("Select audio track") + ":";

                group_channels.Visibility = Visibility.Hidden;
                group_info.Visibility = Visibility.Hidden;
                group_samplerate.Visibility = Visibility.Hidden;
                group_volume.Visibility = Visibility.Hidden;

                group_audio.Margin = new Thickness(8, 8, 8, 0);
                group_delay.Margin = new Thickness(8, 100, 8, 0);

                button_apply.Visibility = Visibility.Hidden;
                button_cancel.Visibility = Visibility.Hidden;
                button_ok.Margin = new Thickness(0, 5, 8, 5);

                this.Width = 500;
                this.Height = 240;
            }

            //прописываем в форму текущие настройки
            if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0) SetAudioOptions();
            
            //прописываем информацию
            SetInfo();

            //прописываем тултипы
            SetTooltips();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        private void SetAudioOptions()
        {
            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (m.volume == null)
                m.volume = Settings.Volume;

            num_delay.Value = (decimal)outstream.delay;
            combo_samplerate.SelectedItem = outstream.samplerate;
            combo_converter.SelectedValue = m.sampleratemodifer;
            combo_volume.SelectedItem = m.volume;
            combo_accurate.SelectedItem = m.volumeaccurate;
            combo_mixing.SelectedItem = EnumMixingToString(instream.channelconverter);
        }

        private void SetInfo()
        {
            if (m.inaudiostreams.Count > 0)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                //таблички
                label_delayin.Content = Languages.Translate("Input") + ": " + instream.delay + " ms";
                label_insamplerate.Content = Languages.Translate("Input") + ": " + instream.samplerate + " Hz";
                label_volume.Content = Languages.Translate("Amplifying") + ": " + instream.gain + " dB";
                label_inchannels.Content = Languages.Translate("Source") + ": " + Calculate.ExplainChannels(instream.channels);

                //Параметры на вход
                texbox_info.Clear();
                texbox_info.AppendText("Input:" + Environment.NewLine);
                texbox_info.AppendText("------------------" + Environment.NewLine);
                texbox_info.AppendText("Codec: " + instream.codec + Environment.NewLine);
                texbox_info.AppendText("Codec ID: " + instream.codecshort + Environment.NewLine);
                texbox_info.AppendText("Bitrate: " + instream.bitrate + " kbps" + Environment.NewLine);
                texbox_info.AppendText("Channels: " + instream.channels + " ch" + Environment.NewLine);
                texbox_info.AppendText("Samplerate: " + instream.samplerate + " Hz" + Environment.NewLine);
                texbox_info.AppendText("Delay: " + instream.delay + " ms" + Environment.NewLine);
                texbox_info.AppendText(Environment.NewLine);

                if (m.outaudiostreams.Count == 0) return;
                
                //Параметры на выход
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                texbox_info.AppendText("Output:" + Environment.NewLine);
                texbox_info.AppendText("------------------" + Environment.NewLine);
                texbox_info.AppendText("Codec: " + outstream.codec + Environment.NewLine);
                texbox_info.AppendText("Bitrate: " + ((outstream.bitrate > 0) ? outstream.bitrate + " kbps" : "VBR") + Environment.NewLine);
                texbox_info.AppendText("Channels: " + outstream.channels + " ch" + Environment.NewLine);
                texbox_info.AppendText("Samplerate: " + outstream.samplerate + " Hz" + Environment.NewLine);
                texbox_info.AppendText("Delay: " + outstream.delay + " ms" + Environment.NewLine);
                texbox_info.AppendText("Amplifying: " + instream.gain + " dB" + Environment.NewLine);
            }
        }

        private void SetTooltips()
        {
            foreach (ComboBoxItem item in combo_converter.Items)
            {
                if (((AviSynthScripting.SamplerateModifers)item.Content) == AviSynthScripting.SamplerateModifers.AssumeSampleRate)
                {
                    item.ToolTip = Languages.Translate("AssumeSampleRate changes the sample rate (playback speed) of the current sample.") +
                    Environment.NewLine + Languages.Translate("If used without AssumeFPS, it will cause desync with the video.");
                }
                else if (((AviSynthScripting.SamplerateModifers)item.Content) == AviSynthScripting.SamplerateModifers.SSRC)
                {
                    item.ToolTip = Languages.Translate("SSRC resampling. Audio is always converted to float.") +
                    Environment.NewLine + Languages.Translate("This filter will result in better audio quality than ResampleAudio.") +
                    Environment.NewLine + Languages.Translate("It uses SSRC by Naoki Shibata, which offers the best resample quality available.");
                }
                else if (((AviSynthScripting.SamplerateModifers)item.Content) == AviSynthScripting.SamplerateModifers.ResampleAudio)
                {
                    item.ToolTip = Languages.Translate("ResampleAudio performs a high-quality change of audio sample rate.");
                }
            }

            textbox_apath.ToolTip = Languages.Translate("Path for external audio file");
            button_openapath.ToolTip = Languages.Translate("Open external audio file");
            button_analysevolume.ToolTip = Languages.Translate("Analyse");
            button_play.ToolTip = Languages.Translate("Play selected track");
            combo_mixing.ToolTip = Languages.Translate("How to convert audio channels");
            button_fix_channels.ToolTip = Languages.Translate("Remember this selection");
            check_apply_delay.ToolTip = Languages.Translate("Auto apply to output");
            combo_volume.ToolTip = Languages.Translate("Normalize to this (peak) level:") + "\r\n30% = -10.5dB\r\n40% = -8dB\r\n50% = -6dB\r\n60% = -4.5dB\r\n70% = -3dB\r\n80% = -1.9dB\r\n" + 
                "90% = -0.9dB\r\n100% = 0dB (Nominal level)\r\n150% = 3.5dB\r\n200% = 6dB\r\n250% = 8dB\r\n300% = 9.5dB\r\n350% = 11dB\r\n400% = 12dB";
            combo_accurate.ToolTip = Languages.Translate("How many frames to analyze");
        }

        private void combo_atracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_atracks.IsDropDownOpen || combo_atracks.IsSelectionBoxHighlighted)
            {
                m.inaudiostream = combo_atracks.SelectedIndex;

                //передаём активный трек на выход
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                //Требуется извлечение звука
                if (instream.audiopath == null && instream.decoder == 0 && m.inaudiostream > 0)
                {
                    //FFMS2 и LSMASH умеют переключать треки, для них их можно не извлекать
                    if (!(m.vdecoder == AviSynthScripting.Decoders.FFmpegSource2 && Settings.FFMS_Enable_Audio ||
                        ((m.vdecoder == AviSynthScripting.Decoders.LSMASHVideoSource || m.vdecoder == AviSynthScripting.Decoders.LWLibavVideoSource) &&
                        Settings.LSMASH_Enable_Audio)))
                    {
                        string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                        instream.audiopath = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + outext;
                        instream.audiofiles = new string[] { instream.audiopath };
                        instream = Format.GetValidADecoder(instream);
                    }
                }

                textbox_apath.Text = instream.audiopath;

                //перезабиваем поток на выход
                AudioStream stream = new AudioStream();
                m.outaudiostreams.Clear();
                if (Settings.ApplyDelay) stream.delay = instream.delay;
                m.outaudiostreams.Add(stream);

                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                //забиваем аудио настройки
                outstream.encoding = Settings.GetAEncodingPreset(Settings.FormatOut);
                outstream.codec = PresetLoader.GetACodec(m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(m);

                m = Format.GetValidSamplerate(m);

                //определяем битность
                m = Format.GetValidBits(m);

                //определяем колличество каналов
                m = Format.GetValidChannelsConverter(m);
                m = Format.GetValidChannels(m);

                //проверяем можно ли копировать данный формат
                if (outstream.codec == "Copy")
                {
                    //AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                    outstream.audiopath = instream.audiopath;
                    outstream.bitrate = instream.bitrate;

                    string CopyProblems = Format.ValidateCopyAudio(m);
                    if (CopyProblems != null)
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                            " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because audio encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }
                else
                {
                    string aext = Format.GetValidRAWAudioEXT(outstream.codec);
                    outstream.audiopath = Settings.TempPath + "\\" + m.key + aext;
                }

                SetAudioOptions();

                SetInfo();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m.inaudiostreams.Count > 0)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (textbox_apath.Text != instream.audiopath)
                {
                    if (textbox_apath.Text == "")
                    {
                        instream.audiopath = null;
                        instream.audiofiles = null;
                    }
                    else
                    {
                        instream.audiopath = textbox_apath.Text;
                        instream.audiofiles = new string[] { instream.audiopath };
                    }
                }

                try
                {
                    if (File.Exists(Settings.TempPath + "\\tracker.avs"))
                        File.Delete(Settings.TempPath + "\\tracker.avs");
                }
                catch { }
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
           if (m.inaudiostreams.Count > 0)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (instream.audiopath != null && !File.Exists(instream.audiopath))
                {
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                    if (dem.IsErrors)
                    {
                        ErrorException(dem.error_message, null);
                    }
                }

                if ((m.volume != "Disabled" && Settings.AutoVolumeMode != Settings.AutoVolumeModes.Disabled) &&
                    !instream.gaindetected)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec != "Copy")
                        AnalyseVolume();
                }
            }

            if (mode == AudioOptionsModes.AllOptions)
            {
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
                if (p.m.script != m.script)
                    Refresh();
            }

            Close();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            if (mode == AudioOptionsModes.AllOptions)
            {
                if (p.m.script != oldm.script)
                {
                    m = oldm.Clone();
                    Refresh();
                }
            }

            Close();
        }

        private void button_apply_Click(object sender, RoutedEventArgs e)
        {
            if (m.inaudiostreams.Count > 0)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                string outacodec = null;
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    outacodec = outstream.codec;
                }

                if (instream.audiopath != null && !File.Exists(instream.audiopath))
                {
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                    if (dem.IsErrors)
                    {
                        ErrorException(dem.error_message, null);

                        //Обходим анализ громкости
                        Refresh();
                        SetInfo();
                        return;
                    }
                }

                if ((m.volume != "Disabled" && Settings.AutoVolumeMode != Settings.AutoVolumeModes.Disabled) &&
                    !instream.gaindetected && outacodec != "Copy")
                    AnalyseVolume();
            }

            Refresh();
            SetInfo();
        }

        private void button_play_Click(object sender, RoutedEventArgs e)
        {
            if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                if (textbox_apath.Text != instream.audiopath)
                {
                    if (textbox_apath.Text == "")
                    {
                        instream.audiopath = null;
                        instream.audiofiles = null;
                    }
                    else
                    {
                        instream.audiopath = textbox_apath.Text;
                        instream.audiofiles = new string[] { instream.audiopath };
                    }
                }

               /* if (m.vdecoder == AviSynthScripting.Decoders.FFmpegSource)
                {
                    ////Получаем информацию через AviSynth и кешируем аудио для FFmpegSource
                    //Caching cach = new Caching(m);
                    //m = cach.m.Clone();
                        
                    string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.FastPreview);
                    AviSynthScripting.WriteScriptToFile(script, "tracker");
                    PlayInWPFPlayer(Settings.TempPath + "\\tracker.avs");
                }
                else   */
                {
                    if (instream.audiopath != null && !File.Exists(instream.audiopath))
                    {
                        Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                        if (dem.IsErrors)
                        {
                            ErrorException(dem.error_message, null);
                            return;
                        }
                    }

                    string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.FastPreview);
                    AviSynthScripting.WriteScriptToFile(script, "tracker");
                    if (instream.audiopath == null || File.Exists(instream.audiopath))
                    {
                        try
                        {
                            Process.Start(Calculate.StartupPath + "\\WPF_VideoPlayer.exe", Settings.TempPath + "\\tracker.avs");
                        }
                        catch (Exception ex)
                        {
                            ErrorException(ex.Message, ex.StackTrace);
                        }
                    }
                }
            }
        }

        private void button_openapath_Click(object sender, RoutedEventArgs e)
        {
            ArrayList files = OpenDialogs.GetFilesFromConsole("oa");
            if (files.Count > 0) AddExternalTrack(files[0].ToString());
        }

        private void AddExternalTrack(string infilepath)
        {
            //получаем медиа информацию
            MediaInfoWrapper mi = null;
            FFInfo ff = null;

            try
            {
                AudioStream stream = null;
                int old_stream = 0;

                string ext = Path.GetExtension(infilepath).ToLower();
                if (ext != ".avs" && ext != ".grf")
                {
                    mi = new MediaInfoWrapper();
                    stream = mi.GetAudioInfoFromAFile(infilepath, false);
                    stream.mi_order = mi.ATrackOrder(0);
                    stream.mi_id = mi.ATrackID(0);

                    ff = new FFInfo();
                    ff.Open(infilepath);

                    //Аналогично тому, как сделано в Informer'е
                    if (ff.AudioStreams().Count > 0)
                    {
                        stream.ff_order = ff.FirstAudioStreamID();
                        stream.ff_order_filtered = ff.FilteredStreamOrder(stream.ff_order);
                        if (stream.mi_order < 0) stream.mi_order = stream.ff_order;
                        if (stream.bitrate == 0) stream.bitrate = ff.AudioBitrate(stream.ff_order);
                        if (stream.channels == 0) stream.channels = ff.StreamChannels(stream.ff_order);
                        if (stream.samplerate == null) stream.samplerate = ff.StreamSamplerate(stream.ff_order);
                        if (stream.language == "Unknown") stream.language = ff.StreamLanguage(stream.ff_order);
                        stream.ff_bits = ff.StreamBits(stream.ff_order);
                        //if (stream.bits == 0) stream.bits = stream.ff_bits;
                        stream.ff_codec = ff.StreamCodec(stream.ff_order);
                        if (stream.codec == "A_MS/ACM" || stream.codec == "")
                        {
                            stream.codec = stream.ff_codec;
                            stream.codecshort = ff.StreamCodecShort(stream.ff_order);
                        }
                    }
                }
                else
                {
                    stream = new AudioStream();
                    stream.audiopath = infilepath;
                    stream.audiofiles = new string[] { stream.audiopath };
                    stream.codec = stream.codecshort = "PCM";
                    stream.language = "Unknown";
                }

                //Добавляем этот трек
                old_stream = m.inaudiostream;
                stream = Format.GetValidADecoder(stream);
                m.inaudiostream = m.inaudiostreams.Count;
                m.inaudiostreams.Add(stream.Clone());

                //Оставшаяся инфа + ошибки
                Caching cach = new Caching(m, true);
                if (cach.m == null)
                {
                    //Удаляем этот трек
                    m.inaudiostream = old_stream;
                    m.inaudiostreams.RemoveAt(m.inaudiostreams.Count - 1);
                    return;
                }
                m = cach.m.Clone();

                textbox_apath.Text = infilepath;

                //разрешаем формы
                group_channels.IsEnabled = true;
                group_delay.IsEnabled = true;
                group_samplerate.IsEnabled = true;
                group_volume.IsEnabled = true;

                //прописываем в список внешний трек
                ComboBoxItem item = new ComboBoxItem();
                stream = (AudioStream)m.inaudiostreams[m.inaudiostream]; //Переопределяем с новыми параметрами
                item.Content = (combo_atracks.Items.Count + 1).ToString("00") + ". " + stream.language + " " + stream.codecshort + " " + stream.channels + "ch";
                item.ToolTip = item.Content + " " + stream.samplerate + "Hz " + stream.bitrate + "kbps " + stream.delay + "ms";
                combo_atracks.Items.Add(item);
                combo_atracks.SelectedIndex = combo_atracks.Items.Count - 1;
            }
            catch (Exception ex)
            {
                ErrorException("AddExternalTrack: " + ex.Message, ex.StackTrace);
                return;
            }
            finally
            {
                if (mi != null)
                {
                    mi.Close();
                    mi = null;
                }

                if (ff != null)
                {
                    ff.Close();
                    ff = null;
                }
            }

            AudioStream newstream = new AudioStream();
            m.outaudiostreams.Clear();
            m.outaudiostreams.Add(newstream);
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //забиваем аудио настройки
            outstream.encoding = Settings.GetAEncodingPreset(Settings.FormatOut);
            outstream.codec = PresetLoader.GetACodec(m.format, outstream.encoding);
            outstream.passes = PresetLoader.GetACodecPasses(m);

            m = Format.GetValidSamplerate(m);

            //определяем битность
            m = Format.GetValidBits(m);

            //определяем колличество каналов
            m = Format.GetValidChannelsConverter(m);
            m = Format.GetValidChannels(m);

            //проверяем можно ли копировать данный формат
            if (outstream.codec == "Copy")
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                outstream.audiopath = instream.audiopath;
                outstream.bitrate = instream.bitrate;

                string CopyProblems = Format.ValidateCopyAudio(m);
                if (CopyProblems != null)
                {
                    Message mess = new Message(this);
                    mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                        " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because audio encoder = Copy)"), Languages.Translate("Warning"));
                }
            }
            else
            {
                string aext = Format.GetValidRAWAudioEXT(outstream.codec);
                outstream.audiopath = Settings.TempPath + "\\" + m.key + aext;
            }

            SetAudioOptions();
            SetInfo();
        }

        private void DD_GotFiles(object sender, string[] files)
        {
            try
            {
                if (Calculate.ValidatePath(files[0], true))
                    AddExternalTrack(files[0]);
            }
            catch (Exception ex)
            {
                ErrorException("DragOpen: " + ex.Message, ex.StackTrace);
            }
        }

        private void Refresh()
        {
            m = AviSynthScripting.CreateAutoAviSynthScript(m);
            p.m = m.Clone();
            p.Refresh(m.script);

            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (p.combo_aencoding.SelectedItem.ToString() != outstream.encoding)
                    p.combo_aencoding.SelectedItem = outstream.encoding;
            }
        }

        private void combo_volume_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_volume.IsDropDownOpen || combo_volume.IsSelectionBoxHighlighted)
            {
                m.volume = combo_volume.SelectedItem.ToString();
                Settings.Volume = m.volume;
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                instream.gain = "0.0";
                instream.gaindetected = false;

                SetInfo();
            }
        }

        private void num_delay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_delay.IsAction)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                outstream.delay = (int)num_delay.Value;
                SetInfo();
            }
        }

        private void combo_samplerate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_samplerate.IsDropDownOpen || combo_samplerate.IsSelectionBoxHighlighted)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                outstream.samplerate = Calculate.GetSplittedString(combo_samplerate.SelectedItem.ToString(), 0);

                AviSynthScripting.SamplerateModifers oldс = m.sampleratemodifer;
                m = Format.GetValidSamplerateModifer(m);
                if (oldс != m.sampleratemodifer)
                {
                    Message message = new Message(this);
                    message.ShowMessage(Languages.Translate("SSRC can`t convert") + ": " + instream.samplerate + " > " + outstream.samplerate + "!",
                        Languages.Translate("Warning"), Message.MessageStyle.Ok);
                    combo_converter.SelectedValue = m.sampleratemodifer;
                }

                SetInfo();
            }
        }

        private void combo_converter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_converter.IsDropDownOpen || combo_converter.IsSelectionBoxHighlighted)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                m.sampleratemodifer = (AviSynthScripting.SamplerateModifers)((ComboBoxItem)combo_converter.SelectedItem).Content;

                AviSynthScripting.SamplerateModifers oldс = m.sampleratemodifer;
                m = Format.GetValidSamplerateModifer(m);
                if (oldс != m.sampleratemodifer)
                {
                    Message message = new Message(this);
                    message.ShowMessage(Languages.Translate("SSRC can`t convert") + ": " + instream.samplerate + " > " + outstream.samplerate + "!",
                        Languages.Translate("Warning"), Message.MessageStyle.Ok);
                    combo_converter.SelectedValue = m.sampleratemodifer;
                }
            }
        }

        private void button_analysevolume_Click(object sender, RoutedEventArgs e)
        {
            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

            if (instream.audiopath != null && !File.Exists(instream.audiopath))
            {
                Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                if (dem.IsErrors)
                {
                    ErrorException(dem.error_message, null);
                    return;
                }

                //обновляем скрипт
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
            }

            AnalyseVolume();
            SetInfo();
        }

        private void AnalyseVolume()
        {
            if (combo_volume.SelectedItem != null)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                if (combo_volume.SelectedItem.ToString() == "Disabled")
                {
                    instream.gain = "0.0";
                    instream.gaindetected = false;
                }
                else
                {
                    Normalize norm = new Normalize(m);
                    if (norm.m != null)
                    {
                        m = norm.m.Clone();
                        instream.gaindetected = true;
                    }
                    else
                    {
                        instream.gain = "0.0";
                        instream.gaindetected = false;
                    }
                }
            }
        }

        private void combo_accurate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_accurate.IsDropDownOpen || combo_accurate.IsSelectionBoxHighlighted)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                Settings.VolumeAccurate = combo_accurate.SelectedItem.ToString();
                m.volumeaccurate = combo_accurate.SelectedItem.ToString();
                instream.gain = "0.0";
                instream.gaindetected = false;

                SetInfo();
            }
        }

        private void SafeDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                ErrorException("SafeFileDelete: " + ex.Message, ex.StackTrace);
            }
        }

        private void ErrorException(string message, string info)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, info, Languages.Translate("Error"));
        }

        private void combo_mixing_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mixing.IsDropDownOpen || combo_mixing.IsSelectionBoxHighlighted)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                instream.channelconverter = StringMixingToEnum(combo_mixing.SelectedItem.ToString());

                //получаем колличество каналов на выход
                m = Format.GetValidChannels(m);

                SetInfo(); 
            }
        }

        private string EnumMixingToString(ChannelConverters mixing)
        {
            if (mixing == ChannelConverters.KeepOriginalChannels)
                return "Keep Original Channels";
            if (mixing == ChannelConverters.ConvertToMono)
                return "Convert to 1ch Mono";
            if (mixing == ChannelConverters.ConvertToStereo)
                return "Convert to 2ch Stereo";
            if (mixing == ChannelConverters.ConvertToDolbyProLogic)
                return "Convert to 2ch Dolby Pro Logic";
            if (mixing == ChannelConverters.ConvertToDolbyProLogicII)
                return "Convert to 2ch Dolby Pro Logic II";
            if (mixing == ChannelConverters.ConvertToDolbyProLogicIILFE)
                return "Convert to 2ch Dolby Pro Logic II (LFE)";
            if (mixing == ChannelConverters.ConvertToUpmixAction)
                return "Convert to 5.1 channels (Action)";
            if (mixing == ChannelConverters.ConvertToUpmixDialog)
                return "Convert to 5.1 channels (Dialog)";
            if (mixing == ChannelConverters.ConvertToUpmixFarina)
                return "Convert to 5.1 channels (Farina)";
            if (mixing == ChannelConverters.ConvertToUpmixGerzen)
                return "Convert to 5.1 channels (Gerzen)";
            if (mixing == ChannelConverters.ConvertToUpmixMultisonic)
                return "Convert to 5.1 channels (Multisonic)";
            if (mixing == ChannelConverters.ConvertToUpmixSoundOnSound)
                return "Convert to 5.1 channels (SoundOnSound)";

            return mixing.ToString();
        }

        private ChannelConverters StringMixingToEnum(string mixing)
        {
            if (mixing == "Keep Original Channels")
                return ChannelConverters.KeepOriginalChannels;
            if (mixing == "Convert to 1ch Mono")
                return ChannelConverters.ConvertToMono;
            if (mixing == "Convert to 2ch Stereo")
                return ChannelConverters.ConvertToStereo;
            if (mixing == "Convert to 2ch Dolby Pro Logic")
                return ChannelConverters.ConvertToDolbyProLogic;
            if (mixing == "Convert to 2ch Dolby Pro Logic II")
                return ChannelConverters.ConvertToDolbyProLogicII;
            if (mixing == "Convert to 2ch Dolby Pro Logic II (LFE)")
                return ChannelConverters.ConvertToDolbyProLogicIILFE;
            if (mixing == "Convert to 5.1 channels (Action)")
                return ChannelConverters.ConvertToUpmixAction;
            if (mixing == "Convert to 5.1 channels (Dialog)")
                return ChannelConverters.ConvertToUpmixDialog;
            if (mixing == "Convert to 5.1 channels (Farina)")
                return ChannelConverters.ConvertToUpmixFarina;
            if (mixing == "Convert to 5.1 channels (Gerzen)")
                return ChannelConverters.ConvertToUpmixGerzen;
            if (mixing == "Convert to 5.1 channels (Multisonic)")
                return ChannelConverters.ConvertToUpmixMultisonic;
            if (mixing == "Convert to 5.1 channels (SoundOnSound)")
                return ChannelConverters.ConvertToUpmixSoundOnSound;

            return ChannelConverters.KeepOriginalChannels;
        }

        private void button_fix_channels_Click(object sender, RoutedEventArgs e)
        {
            Settings.ChannelsConverter = StringMixingToEnum(combo_mixing.SelectedItem.ToString()).ToString();
        }

        private void check_apply_delay_Click(object sender, RoutedEventArgs e)
        {
            Settings.ApplyDelay = check_apply_delay.IsChecked.Value;
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
            if (check_apply_delay.IsChecked.Value)
            {
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                num_delay.Value = outstream.delay = instream.delay;
            }
            else num_delay.Value = outstream.delay = 0;
            SetInfo();
        }
	}
}