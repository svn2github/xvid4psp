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
	public partial class AudioEncoding
	{
        public Massive m;
        private Massive oldm;

        LameMP3 mp3;
        NeroAAC aac;
        AftenAC3 ac3;
        FMP2 fmp2;
        FPCM fpcm;
        FLPCM flpcm;
        FFLAC fflac;
        CopyOrDisabled copyordisabled;

        public AudioEncoding(Massive mass)
		{
			this.InitializeComponent();

            m = mass.Clone();
            oldm = mass.Clone();
            Owner = m.owner;

            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //загружаем список кодеков соответвующий формату
            foreach (string codec in Format.GetACodecsList(m.format)) combo_codec.Items.Add(codec);
            if (!combo_codec.Items.Contains(outstream.codec)) combo_codec.Items.Add(outstream.codec);
            combo_codec.SelectedItem = outstream.codec;
            text_incodec_value.Content = instream.codecshort;

            text_insize_value.Content = m.infilesize;
            text_outsize_value.Content = m.outfilesize = Calculate.GetEncodingSize(m);

            //загружаем правильную страницу
            LoadCodecWindow();

            //переводим
            Title = Languages.Translate("Audio encoding settings");
            text_size.Content = Languages.Translate("Size") + ":";
            text_codec.Content = Languages.Translate("Codec") + ":";
            text_profile.Content = Languages.Translate("Profile:");
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_add.ToolTip = Languages.Translate("Add profile");
            button_remove.ToolTip = Languages.Translate("Remove profile");

            LoadProfiles();

            ShowDialog();
		}

        private void LoadCodecWindow()
        {
            //определяем аудио потоки
            AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //загрузка
            if (outstream.codec == "AAC")
            {
                aac = new NeroAAC(m, this);
                grid_codec.Children.Add(aac);
            }
            else if (outstream.codec == "MP3")
            {
                mp3 = new LameMP3(m, this);
                grid_codec.Children.Add(mp3);
            }
            else if (outstream.codec == "AC3")
            {
                ac3 = new AftenAC3(m, this);
                grid_codec.Children.Add(ac3);
            }
            else if (outstream.codec == "MP2")
            {
                fmp2 = new FMP2(m, this);
                grid_codec.Children.Add(fmp2);
            }
            else if (outstream.codec == "PCM")
            {
                fpcm = new FPCM(m, this);
                grid_codec.Children.Add(fpcm);
            }
            else if (outstream.codec == "LPCM")
            {
                flpcm = new FLPCM(m, this);
                grid_codec.Children.Add(flpcm);
            }
            else if (outstream.codec == "FLAC")
            {
                fflac = new FFLAC(m, this);
                grid_codec.Children.Add(fflac);
            }
            else if (outstream.codec == "Copy" ||
                outstream.codec == "Disabled")
            {
                copyordisabled = new CopyOrDisabled();
                if (outstream.codec == "Disabled")
                    copyordisabled.text_info.Content = Languages.Translate("Output file will be created without sound.");
                else
                {
                    copyordisabled.text_info.Content = "Codec: " + instream.codecshort + Environment.NewLine;
                    copyordisabled.text_info.Content += "Bitrate: " + instream.bitrate  + " kbps" + Environment.NewLine;
                    copyordisabled.text_info.Content += "Channels: " + instream.channels +  " ch" + Environment.NewLine;
                    copyordisabled.text_info.Content += "Samplerate: " + instream.samplerate + " Hz" + Environment.NewLine;
                    copyordisabled.text_info.Content += "Bits: " + instream.bits + " bit";
                }
                grid_codec.Children.Add(copyordisabled);
            }
        }

        private void UnLoadCodecWindow()
        {
            //очистка
            if (aac != null)
            {
                grid_codec.Children.Remove(aac);
                aac = null;
            }
            else if (mp3 != null)
            {
                grid_codec.Children.Remove(mp3);
                mp3 = null;
            }
            else if (ac3 != null)
            {
                grid_codec.Children.Remove(ac3);
                ac3 = null;
            }
            else if (fmp2 != null)
            {
                grid_codec.Children.Remove(fmp2);
                fmp2 = null;
            }
            else if (fpcm != null)
            {
                grid_codec.Children.Remove(fpcm);
                fpcm = null;
            }
            else if (flpcm != null)
            {
                grid_codec.Children.Remove(flpcm);
                flpcm = null;
            }
            else if (fflac != null)
            {
                grid_codec.Children.Remove(fflac);
                fflac = null;
            }
            else if (copyordisabled != null)
            {
                grid_codec.Children.Remove(copyordisabled);
                copyordisabled = null;
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateMassive();
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m = oldm.Clone();
            Close();
        }

        private void combo_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_profile.IsDropDownOpen || combo_profile.IsSelectionBoxHighlighted)
            {
                RefreshCodecProfileWindow();

                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                //правим выходной битрейт
                if (outstream.codec == "Copy")
                    outstream.bitrate = instream.bitrate;
                if (outstream.codec == "Disabled")
                    outstream.bitrate = 0;

                UpdateOutSize();

                //проверяем можно ли копировать данный формат
                if (outstream.codec == "Copy")
                {
                    string CopyProblems = Format.ValidateCopyAudio(m);
                    if (CopyProblems != null)
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because audio encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }
            }
        }

        private void combo_codec_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_codec.IsDropDownOpen || combo_codec.IsSelectionBoxHighlighted)
            {
                //определяем аудио потоки
                AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                UnLoadCodecWindow();
                outstream.codec = combo_codec.SelectedItem.ToString();

                m.mp3_options = new mp3_arguments();
                m.aac_options = new aac_arguments();
                m.ac3_options = new ac3_arguments();
                m.flac_options = new flac_arguments();

                LoadCodecWindow();
                if (outstream.codec == "Disabled")
                {
                    combo_profile.SelectedItem = outstream.codec;
                    outstream.encoding = outstream.codec;
                }
                else if (outstream.codec == "Copy")
                {
                    combo_profile.SelectedItem = outstream.codec;
                    outstream.encoding = outstream.codec;
                    outstream.bitrate = instream.bitrate;
                    m = Format.GetValidSamplerate(m);
                    m = Format.GetValidBits(m);
                    m = Format.GetValidChannelsConverter(m);
                    m = Format.GetValidChannels(m);
                }
                else
                {
                    m = Format.GetValidSamplerate(m);
                    m = Format.GetValidBits(m);
                    m = Format.GetValidChannelsConverter(m);
                    m = Format.GetValidChannels(m);
                    UpdateManualProfile();
                }

                //правим выходной битрейт
                if (outstream.codec == "Disabled")
                    outstream.bitrate = 0;

                //проверяем можно ли копировать данный формат
                if (outstream.codec == "Copy")
                {
                    string CopyProblems = Format.ValidateCopyAudio(m);
                    if (CopyProblems != null)
                    {
                        Message mess = new Message(this);
                        mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because audio encoder = Copy)"), Languages.Translate("Warning"));
                    }
                }

                UpdateOutSize();
            }
        }

        private void RefreshCodecProfileWindow()
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (combo_profile.SelectedItem.ToString() == "Copy" ||
                combo_profile.SelectedItem.ToString() == "Disabled")
                combo_codec.SelectedItem = combo_profile.SelectedItem.ToString();

            if (outstream.codec != "Copy" &&
                combo_profile.SelectedItem.ToString() == "Copy" ||
                outstream.codec != "Disabled" &&
                combo_profile.SelectedItem.ToString() == "Disabled")
            {
                UnLoadCodecWindow();
                outstream.codec = combo_profile.SelectedItem.ToString();
                combo_codec.SelectedItem = outstream.codec;
                outstream.encoding = outstream.codec;
                LoadCodecWindow();
            }
            else
            {

                if (outstream.codec == "Copy" ||
                    outstream.codec == "Disabled")
                {
                    UnLoadCodecWindow();
                    outstream.codec = Format.GetACodec(m.format, m.outvcodec);
                    LoadCodecWindow();
                }
                else
                {
                    string codec = outstream.codec;
                    outstream.encoding = combo_profile.SelectedItem.ToString();
                    string newcodec = PresetLoader.GetACodec(m.format, outstream.encoding);
                    if (codec != newcodec)
                    {
                        UnLoadCodecWindow();
                        outstream.codec = newcodec;
                        LoadCodecWindow();
                    }
                }
                LoadProfileToCodec();
                combo_codec.SelectedItem = outstream.codec;
            }
        }

        private void UpdateMassive()
        {
            if (aac != null)
            {
                m = aac.m.Clone();
                m = NeroAAC.EncodeLine(m);
            }
            else if (mp3 != null)
            {
                m = mp3.m.Clone();
                m = LameMP3.EncodeLine(m);
            }
            else if (ac3 != null)
            {
                m = ac3.m.Clone();
                m = AftenAC3.EncodeLine(m);
            }
            else if (fmp2 != null)
            {
                m = fmp2.m.Clone();
                m = FMP2.EncodeLine(m);
            }
            else if (fpcm != null)
            {
                m = fpcm.m.Clone();
                m = FPCM.EncodeLine(m);
            }
            else if (flpcm != null)
            {
                m = flpcm.m.Clone();
                m = FLPCM.EncodeLine(m);
            }
            else if (fflac != null)
            {
                m = fflac.m.Clone();
                m = FFLAC.EncodeLine(m);
            }
        }

        private void UpdateCodecMassive()
        {
            if (aac != null) aac.m = m.Clone();
            else if (mp3 != null) mp3.m = m.Clone();
            else if (ac3 != null) ac3.m = m.Clone();
            else if (fmp2 != null) fmp2.m = m.Clone();
            else if (fpcm != null) fpcm.m = m.Clone();
            else if (flpcm != null) flpcm.m = m.Clone();
            else if (fflac != null) fflac.m = m.Clone();
        }

        private void LoadProfiles()
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            //загружаем список фильтров
            combo_profile.Items.Clear();
            foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\audio"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                combo_profile.Items.Add(name);
            }
            combo_profile.Items.Add("Disabled");
            combo_profile.Items.Add("Copy");
            //прописываем текущий пресет кодирования
            combo_profile.SelectedItem = outstream.encoding;

            combo_profile.UpdateLayout();
        }

        private void LoadProfileToCodec()
        {
            //записываем профиль в реестр
            Settings.SetAEncodingPreset(m.format, combo_profile.SelectedItem.ToString());

            m = Format.GetValidChannelsConverter(m);
            m = Format.GetValidChannels(m);
            m = Format.GetValidSamplerate(m);
            m = Format.GetValidBits(m);

            if (aac != null)
            {
                AudioStream outstream = (AudioStream)aac.m.outaudiostreams[aac.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(aac.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(aac.m);
                aac.m = PresetLoader.DecodePresets(aac.m);
                aac.LoadFromProfile();
            }
            else if (mp3 != null)
            {
                AudioStream outstream = (AudioStream)mp3.m.outaudiostreams[mp3.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(mp3.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(mp3.m);
                mp3.m = PresetLoader.DecodePresets(mp3.m);
                mp3.LoadFromProfile();
            }
            else if (ac3 != null)
            {
                AudioStream outstream = (AudioStream)ac3.m.outaudiostreams[ac3.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(ac3.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(ac3.m);
                ac3.m = PresetLoader.DecodePresets(ac3.m);
                ac3.LoadFromProfile();
            }
            else if (fmp2 != null)
            {
                AudioStream outstream = (AudioStream)fmp2.m.outaudiostreams[fmp2.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(fmp2.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(fmp2.m);
                fmp2.m = PresetLoader.DecodePresets(fmp2.m);
                fmp2.LoadFromProfile();
            }
            else if (fpcm != null)
            {
                AudioStream outstream = (AudioStream)fpcm.m.outaudiostreams[fpcm.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(fpcm.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(fpcm.m);
                fpcm.m = PresetLoader.DecodePresets(fpcm.m);
                fpcm.LoadFromProfile();
            }
            else if (flpcm != null)
            {
                AudioStream outstream = (AudioStream)flpcm.m.outaudiostreams[flpcm.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(flpcm.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(flpcm.m);
                flpcm.m = PresetLoader.DecodePresets(flpcm.m);
                flpcm.LoadFromProfile();
            }
            else if (fflac != null)
            {
                AudioStream outstream = (AudioStream)fflac.m.outaudiostreams[fflac.m.outaudiostream];
                //забиваем настройки из профиля
                outstream.encoding = combo_profile.SelectedItem.ToString();
                outstream.codec = PresetLoader.GetACodec(fflac.m.format, outstream.encoding);
                outstream.passes = PresetLoader.GetACodecPasses(fflac.m);
                fflac.m = PresetLoader.DecodePresets(fflac.m);
                fflac.LoadFromProfile();
            }
        }

        private void button_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (outstream.codec == "Copy" ||
                outstream.codec == "Disabled")
                return;

            UpdateMassive();

            string auto_name = outstream.codec;

            if (outstream.codec == "AAC")
            {
                if (m.aac_options.aacprofile == "AAC-LC")
                    auto_name += "-LC";
                if (m.aac_options.aacprofile == "AAC-HE")
                    auto_name += "-HE";
                if (m.aac_options.aacprofile == "AAC-HEv2")
                    auto_name += "-HEv2";
                auto_name += " " + m.aac_options.encodingmode.ToString();
            }

            if (outstream.codec == "MP3")
            {
                auto_name += " " + m.mp3_options.encodingmode.ToString().ToUpper();
                if (m.mp3_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    auto_name += " Q" + m.mp3_options.quality;
                else
                    auto_name += " " + outstream.bitrate + "k";
            }

            if (outstream.codec == "AAC")
            {
                if (m.aac_options.encodingmode == Settings.AudioEncodingModes.VBR)
                    auto_name += " Q" + m.aac_options.quality;
                else
                    auto_name += " " + outstream.bitrate + "k";
            }

            if (outstream.codec == "PCM" ||
                outstream.codec == "LPCM")
            {
                auto_name += " " + outstream.bits + "bit";
            }

            if (outstream.codec == "AC3" ||
                outstream.codec == "MP2")
                    auto_name += " " + outstream.bitrate + "k";

            auto_name += " Custom";  

            NewProfile newp = new NewProfile(auto_name, Format.EnumToString(m.format), NewProfile.ProfileType.AEncoding, this);

            if (newp.profile != null)
            {
                outstream.encoding = newp.profile;
                PresetLoader.CreateAProfile(m);
                LoadProfiles();
            }

            LoadProfileToCodec();
            UpdateOutSize();
            UpdateCodecMassive();
        }

        private void button_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (outstream.codec == "Copy" ||
                outstream.codec == "Disabled")
                return;

            if (combo_profile.Items.Count > 1)
            {
                UpdateMassive();

                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Do you realy want to remove profile") + " " + outstream.encoding + "?",
                    Languages.Translate("Question"),
                    Message.MessageStyle.YesNo);

                if (mess.result == Message.Result.Yes)
                {
                    int last_num = combo_profile.SelectedIndex;
                    string profile_path = Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\audio\\" + outstream.encoding + ".txt";
                    File.Delete(profile_path);

                    //загружаем список фильтров
                    combo_profile.Items.Clear();
                    foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\audio"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        combo_profile.Items.Add(name);
                    }
                    combo_profile.Items.Add("Disabled");
                    combo_profile.Items.Add("Copy");

                    //прописываем текущий пресет кодирования
                    if (last_num == 0)
                        outstream.encoding = combo_profile.Items[0].ToString();
                    else
                        outstream.encoding = combo_profile.Items[last_num - 1].ToString();
                    combo_profile.SelectedItem = outstream.encoding;

                    combo_profile.UpdateLayout();

                    RefreshCodecProfileWindow();

                    UpdateOutSize();
                    UpdateCodecMassive();
                    LoadProfileToCodec();

                    //проверяем можно ли копировать данный формат
                    if (outstream.codec == "Copy")
                    {
                        string CopyProblems = Format.ValidateCopyAudio(m);
                        if (CopyProblems != null)
                        {
                            Message messa = new Message(this);
                            mess.ShowMessage(Languages.Translate("The stream contains parameters incompatible with this format") +
                                " " + Format.EnumToString(m.format) + ": " + CopyProblems + "." + Environment.NewLine + Languages.Translate("(You see this message because audio encoder = Copy)"), Languages.Translate("Warning"));
                        }
                    }
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

        public void UpdateOutSize()
        {
            UpdateMassive();
            text_outsize_value.Content = Calculate.GetEncodingSize(m);
            m.outfilesize = text_outsize_value.Content.ToString();
            UpdateCodecMassive();
        }

        public void UpdateManualProfile()
        {
            try
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                UpdateMassive();

                outstream.encoding = "Custom";
                PresetLoader.CreateAProfile(m);

                LoadProfiles();
                UpdateCodecMassive();
            }
            catch (Exception ex)
            {
                Message mes = new Message(this);
                mes.ShowMessage(ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
	}
}