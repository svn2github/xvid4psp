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

        public AudioOptions()
		{
			this.InitializeComponent();
		}

        public AudioOptions(Massive mass, MainWindow parent, AudioOptionsModes mode)
        {
            this.InitializeComponent();

            this.mode = mode;
            p = parent;
            Owner = p;

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
            label_accurate.Content = Languages.Translate("Accurate") + ":";
            //label_mixing.Content = Languages.Translate("Mixing") + ":";
            combo_mixing.ToolTip = Languages.Translate("How to convert audio channels");
            button_fix_channels.ToolTip = Languages.Translate("Remember this selection");

            //путь к звуковой дорожке
            combo_atracks.Items.Clear();
            if (m.inaudiostreams.Count > 0)
            {
                //забиваем в список каналы звука
                int n = 1;
                foreach (object o in m.inaudiostreams)
                {
                    AudioStream s = (AudioStream)o;
                    combo_atracks.Items.Add(n.ToString("00") + ". " + s.language + " " + s.codecshort + " " + s.channels + "ch");
                    n++;
                }
                combo_atracks.SelectedIndex = m.inaudiostream;

                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (instream.audiopath == null &&
                    instream.badmixing)
                {
                    string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                    instream.audiopath = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + outext;
                    instream.audiofiles = new string[] { instream.audiopath };
                    instream = Format.GetValidADecoder(instream);
                }

                if (instream.audiopath != null)
                    textbox_apath.Text = instream.audiopath;
            }

            //только для all режима
            if (mode == AudioOptionsModes.AllOptions)
            {
                //прописываем samplerate
                combo_samplerate.Items.Clear();
                foreach (string f in Format.GetValidSampleratesList(m))
                    combo_samplerate.Items.Add(f);

                combo_converter.Items.Clear();
                foreach (string ratechangers in Enum.GetNames(typeof(AviSynthScripting.SamplerateModifers)))
                    combo_converter.Items.Add(ratechangers);

                //прописываем громкость
                combo_volume.Items.Clear();
                combo_volume.Items.Add("Disabled");
                for (int y = 90; y < 402; y += 2)
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

                string outacodec = null;
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    outacodec = outstream.codec;
                }

                //запрещаем формы если файл без звука
                if (m.outaudiostreams.Count == 0 ||
                    m.outaudiostreams.Count  > 0 && outacodec == "Copy")
                {
                    group_channels.IsEnabled = false;
                    group_delay.IsEnabled = false;
                    group_samplerate.IsEnabled = false;
                    group_volume.IsEnabled = false;
                }
                else
                {
                    group_channels.IsEnabled = true;
                    group_delay.IsEnabled = true;
                    group_samplerate.IsEnabled = true;
                    group_volume.IsEnabled = true;
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
                this.Height = 234;
            }

            if (m.inaudiostreams.Count > 0 &&
                m.outaudiostreams.Count > 0)
            {
                //прописываем в форму текущие настройки
                SetAudioOptions();

                //прописываем входную информацию
                SetInfo();
            }

            //прописываем тултипы
            SetTooltips();
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
            combo_converter.SelectedItem = m.sampleratemodifer.ToString();
            combo_volume.SelectedItem = m.volume;
            combo_accurate.SelectedItem = m.volumeaccurate;
            combo_mixing.SelectedItem = EnumMixingToString(instream.channelconverter);
        }

        private void SetInfo()
        {
            if (m.inaudiostreams.Count > 0 &&
                m.outaudiostreams.Count > 0)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                //таблички
                label_delayin.Content = Languages.Translate("Input") + ": " + instream.delay + " ms";
                label_insamplerate.Content = Languages.Translate("Input") + ": " + instream.samplerate + " Hz";
                label_volume.Content = Languages.Translate("Amplifying") + ": " + instream.gain + "dB";
                label_inchannels.Content = Languages.Translate("Source") + ": " + Calculate.ExplainChannels(instream.channels);

                //панель информации
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
                texbox_info.AppendText("Output:" + Environment.NewLine);
                texbox_info.AppendText("------------------" + Environment.NewLine);
                texbox_info.AppendText("Codec: " + outstream.codec + Environment.NewLine);
                texbox_info.AppendText("Bitrate: " + outstream.bitrate + " kbps" + Environment.NewLine);
                texbox_info.AppendText("Channels: " + outstream.channels + " ch" + Environment.NewLine);
                texbox_info.AppendText("Samplerate: " + outstream.samplerate + " Hz" + Environment.NewLine);
                texbox_info.AppendText("Delay: " + outstream.delay + " ms" + Environment.NewLine);
                texbox_info.AppendText("Amplifying: " + instream.gain + " dB" + Environment.NewLine);
            }
        }

        private void SetTooltips()
        {
            if (m.sampleratemodifer == AviSynthScripting.SamplerateModifers.AssumeSampleRate)
            {
                combo_converter.ToolTip = Languages.Translate("AssumeSampleRate changes the sample rate (playback speed) of the current sample.") + 
                    Environment.NewLine + Languages.Translate("If used without AssumeFPS, it will cause desync with the video.");
            }
            if (m.sampleratemodifer == AviSynthScripting.SamplerateModifers.SSRC)
            {
                combo_converter.ToolTip = Languages.Translate("SSRC resampling. Audio is always converted to float.") +
                    Environment.NewLine + Languages.Translate("This filter will result in better audio quality than ResampleAudio.") +
                    Environment.NewLine + Languages.Translate("It uses SSRC by Naoki Shibata, which offers the best resample quality available.");
            }
            if (m.sampleratemodifer == AviSynthScripting.SamplerateModifers.ResampleAudio)
            {
                combo_converter.ToolTip = Languages.Translate("ResampleAudio performs a high-quality change of audio sample rate.");
            }

            textbox_apath.ToolTip = Languages.Translate("Path for external audio file");
            button_openapath.ToolTip = Languages.Translate("Open external audio file");
            button_analysevolume.ToolTip = Languages.Translate("Analyse");
            button_play.ToolTip = Languages.Translate("Play selected track");
        }

        private void combo_atracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_atracks.IsDropDownOpen || combo_atracks.IsSelectionBoxHighlighted)
            {
                m.inaudiostream = combo_atracks.SelectedIndex;

                //передаём активный трек на выход
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (instream.audiopath == null &&
                    instream.decoder == 0 &&
                    m.inaudiostream > 0)
                {
                    string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                    instream.audiopath = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + outext;
                    instream.audiofiles = new string[] { instream.audiopath };
                    instream = Format.GetValidADecoder(instream);
                }

                textbox_apath.Text = instream.audiopath;

                //перезабиваем поток на выход
                AudioStream stream = new AudioStream();
                m.outaudiostreams.Clear();
                stream.gainfile = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + "_gain.wav";
                stream.delay = instream.delay;
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

                if (File.Exists(Settings.TempPath + "\\tracker.avs"))
                    File.Delete(Settings.TempPath + "\\tracker.avs");
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
           if (m.inaudiostreams.Count > 0 && !Settings.DontDemuxAudio)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

                if (instream.audiopath != null &&
                    !File.Exists(instream.audiopath))
                {
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                    if (dem.m != null)
                        m = dem.m.Clone();
                }

                if (m.volume != "Disabled" &&
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

                if (instream.audiopath != null &&
                    !File.Exists(instream.audiopath))
                {
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                    m = dem.m.Clone();
                }

                if (m.volume != "Disabled" &&
                    !instream.gaindetected &&
                    outacodec != "Copy")
                    AnalyseVolume();
            }

            Refresh();
            SetInfo();
        }

        private void button_play_Click(object sender, RoutedEventArgs e)
        {
            if (m.outaudiostreams.Count > 0 &&
                m.inaudiostreams.Count > 0)
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
                    if (instream.audiopath != null &&
                        !File.Exists(instream.audiopath))
                    {
                        Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                        m = dem.m.Clone();
                    }

                    string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.FastPreview);
                    AviSynthScripting.WriteScriptToFile(script, "tracker");
                    if (File.Exists(instream.audiopath) ||
                        instream.audiopath == null)
                    {
                        PlayInWPFPlayer(Settings.TempPath + "\\tracker.avs");
                    }
                }
            }
        }

        private void PlayInWPFPlayer(string filepath)
        {
            Process pr = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Calculate.StartupPath +
                "\\WPF_VideoPlayer.exe";
            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
            info.Arguments = filepath;
            pr.StartInfo = info;
            pr.Start();
        }

        private void button_openapath_Click(object sender, RoutedEventArgs e)
        {
            ArrayList files = OpenDialogs.GetFilesFromConsole("oa");
            if (files.Count > 0)
            {
                //разрешаем формы
                group_channels.IsEnabled = true;
                group_delay.IsEnabled = true;
                group_samplerate.IsEnabled = true;
                group_volume.IsEnabled = true;

                string infilepath = files[0].ToString();
                textbox_apath.Text = infilepath;

                //получаем медиа информацию
                MediaInfoWrapper mi = new MediaInfoWrapper();
                try
                {
                    AudioStream stream = mi.GetAudioInfoFromAFile(infilepath);
                    stream = Format.GetValidADecoder(stream);
                    //делаем трек активным
                    m.inaudiostream = m.inaudiostreams.Count;
                    stream.gainfile = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + "_gain.wav";
                    m.inaudiostreams.Add(stream);

                    //прописываем в список внешний трек
                    combo_atracks.Items.Add((combo_atracks.Items.Count + 1).ToString("00") + ". Unknown " + stream.codecshort + " " + stream.channels + "ch");
                    combo_atracks.SelectedIndex = combo_atracks.Items.Count - 1;
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
                finally
                {
                    mi.Close();
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
                instream.gaindetected = false;
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
                    combo_converter.SelectedItem = m.sampleratemodifer.ToString();
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

                m.sampleratemodifer = (AviSynthScripting.SamplerateModifers)Enum.Parse(typeof(AviSynthScripting.SamplerateModifers),
                                       combo_converter.SelectedItem.ToString());

                AviSynthScripting.SamplerateModifers oldс = m.sampleratemodifer;
                m = Format.GetValidSamplerateModifer(m);
                if (oldс != m.sampleratemodifer)
                {
                    Message message = new Message(this);
                    message.ShowMessage(Languages.Translate("SSRC can`t convert") + ": " + instream.samplerate + " > " + outstream.samplerate + "!",
                        Languages.Translate("Warning"), Message.MessageStyle.Ok);
                    combo_converter.SelectedItem = m.sampleratemodifer.ToString();
                }

                SetTooltips();
            }
        }

        private void button_analysevolume_Click(object sender, RoutedEventArgs e)
        {
            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

            if (instream.audiopath != null &&
                !File.Exists(instream.audiopath))
            {
                Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, instream.audiopath);
                if (dem.m != null)
                    m = dem.m.Clone();

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
                    m = norm.m.Clone();
                    instream.gaindetected = true;
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
                SafeDelete(instream.gainfile);
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
                ErrorExeption(ex.Message);
            }
        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
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

	}
}