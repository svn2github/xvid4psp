using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace XviD4PSP
{
    public partial class Decoders_Settings
    {
        private class MyObj
        {
            public string Extension { get; set; }
            public string Decoder { get; set; }
            public string ToolTip { get; set; }
            public int Index { get; set; }
        }

        //"MPEG2-PS/TS" и "Другие файлы"
        public const string mpeg_psts = "mpeg_ps/ts";
        public const string other_files = "*";

        //Список видео декодеров
        private string dec_avi = AviSynthScripting.Decoders.AVISource.ToString();
        private string dec_m2s = AviSynthScripting.Decoders.MPEG2Source.ToString();
        private string dec_dss = AviSynthScripting.Decoders.DirectShowSource.ToString();
        private string dec_dss2 = AviSynthScripting.Decoders.DirectShowSource2.ToString();
        private string dec_ffms = AviSynthScripting.Decoders.FFmpegSource2.ToString();
        private string dec_qts = AviSynthScripting.Decoders.QTInput.ToString();

        //Список аудио декодеров
        private string dec_ac3 = AviSynthScripting.Decoders.NicAC3Source.ToString();
        private string dec_mp3 = AviSynthScripting.Decoders.NicMPG123Source.ToString();
        private string dec_dts = AviSynthScripting.Decoders.NicDTSSource.ToString();
        private string dec_rawav = AviSynthScripting.Decoders.RaWavSource.ToString();
        private string dec_wav = AviSynthScripting.Decoders.WAVSource.ToString();
        private string dec_bass = AviSynthScripting.Decoders.bassAudioSource.ToString();
        private string dec_ffas = AviSynthScripting.Decoders.FFAudioSource.ToString();

        //Подсказки к видео декодерам
        private string tltp_avi = Languages.Translate("This decoder uses VFW AVIFile interface, or AviSynth`s built-in OpenDML code (taken from VirtualDub).") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": avi, vdr";
        private string tltp_m2s = Languages.Translate("Probably the best choice for MPEG-files. It provides frame accurate seeking and uses internal MPEG1/2 decoder.") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": MPEG2-PS/TS (mpg, vob, mod,..)";
        private string tltp_dss = Languages.Translate("This decoder uses installed on your system DirecShow filters (splitters and decoders, including their settings!) for decoding.") +
            "\r\n" + Languages.Translate("Frame accurate seeking is not guaranteed!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_dss2 = Languages.Translate("Mostly the same as DirectShowSource, but from Haali. It provides frame accurate seeking when it`s possible.") +
            "\r\n" + Languages.Translate("Haali Media Splitter must be installed.") +
            "\r\n" + Languages.Translate("May hang when processing the last frames!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_ffms = Languages.Translate("This decoder (based on FFmpeg) uses their own splitters and decoders, but requires some extra time for indexing your file.") +
            "\r\n" + Languages.Translate("However, Haali Media Splitter is still required if you want to open MPEG-PS/TS or OGM files with this decoder.") +
            "\r\n" + Languages.Translate("Decoding of interlaced H.264 may be broken (due to limitations of FFmpeg)!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_qts = Languages.Translate("This decoder uses QuickTime environment for decoding, so QuickTime is required!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": mov";

        //Подсказки к аудио декодерам
        private string tltp_ac3 = Languages.Translate("Supported formats") + ": ac3";
        private string tltp_mp3 = Languages.Translate("Supported formats") + ": mpa, mp1, mp2, mp3";
        private string tltp_dts = Languages.Translate("Supported formats") + ": dts, dtswav";
        private string tltp_rawav = Languages.Translate("Supported formats") + ": wav, w64, lpcm";
        private string tltp_wav = Languages.Translate("Supported formats") + ": wav (2Gb max!)";
        private string tltp_bass = Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_ffas = Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");

        public Massive m;
        private string old_raw_script;
        public bool NeedUpdate = false;
        private bool vdecoders_loaded = false;
        private bool adecoders_loaded = false;
        private bool text_editing = false;
        private int default_index = 0;

        public Decoders_Settings(Massive mass, System.Windows.Window owner, int set_focus_to)
        {
            this.Opacity = 0;
            this.InitializeComponent();
            this.Owner = owner;

            if (mass != null)
            {
                default_index = -1;
                m = mass.Clone();

                //Скрипт для определения изменений
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
                old_raw_script = m.script;
            }

            //переводим
            Title = Languages.Translate("Decoding");
            button_ok.Content = Languages.Translate("OK");
            check_dss_audio.Content = check_ffms_audio.Content = Languages.Translate("Enable Audio");
            check_dss_audio.ToolTip = Languages.Translate("Allow DirectShowSource to decode audio directly from the source-file (without demuxing)");
            check_force_film.Content = Languages.Translate("Auto force Film at") + " (%):";
            check_force_film.ToolTip = Languages.Translate("Auto force Film if Film percentage is more than selected value (for NTSC sources only)");
            check_ffms_force_fps.Content = Languages.Translate("Add AssumeFPS()");
            check_ffms_force_fps.ToolTip = Languages.Translate("Force FPS using AssumeFPS()");
            check_ffms_audio.ToolTip = check_dss_audio.ToolTip.ToString().Replace("DirectShowSource", "FFmpegSource2");
            check_ffms_reindex.Content = Languages.Translate("Overwrite existing index files");
            check_ffms_reindex.ToolTip = Languages.Translate("Always re-index the source file and overwrite existing index file, even if it was valid");
            check_ffms_timecodes.Content = Languages.Translate("Timecodes");
            check_ffms_timecodes.ToolTip = Languages.Translate("Extract timecodes to a file");
            check_drc_ac3.ToolTip = check_drc_dts.ToolTip = Languages.Translate("Apply DRC (Dynamic Range Compression) for this decoder");
            group_misc.Header = Languages.Translate("Misc");
            check_enable_audio.Content = Languages.Translate("Enable audio in input files");
            check_enable_audio.ToolTip = Languages.Translate("If checked, input files will be opened with audio, otherwise they will be opened WITHOUT audio!")
                + "\r\n" + Languages.Translate("Audio files - exception, they always will be opened.");
            check_new_delay.Content = Languages.Translate("Use new Delay calculation method");
            check_new_delay.ToolTip = Languages.Translate("A new method uses the difference between video and audio delays, while old method uses audio delay only."); //+
            //"\r\n" + Languages.Translate("This new method can be helpfull for the FFmpegSource decoders, but harmful for the DirectShowSource.");
            check_copy_delay.Content = Languages.Translate("Apply Delay in Copy mode");
            button_vdec_add.Content = button_adec_add.Content = Languages.Translate("Add");
            button_vdec_delete.Content = button_adec_delete.Content = Languages.Translate("Remove");
            button_vdec_reset.Content = button_adec_reset.Content = Languages.Translate("Reset");
            combo_ffms_threads.ToolTip = label_ffms_threads.ToolTip = "1 = " + Languages.Translate("disable multithreading (recommended)") + "\r\nAuto = " +
                Languages.Translate("logical CPU's count") + "\r\n\r\n" + Languages.Translate("Attention! Multithreaded decoding can be unstable!");
            label_ffms_threads.Content = "- " + Languages.Translate("decoding threads");
            group_tracks.Header = Languages.Translate("Track selection");
            check_tracks_manual.Content = Languages.Translate("Manual");
            check_tracks_manual.ToolTip = Languages.Translate("During the opening of the file you will be prompted to select the track.");
            check_tracks_language.ToolTip = combo_tracks_language.ToolTip = Languages.Translate("The first track in this language will be automatically selected during the opening of the file.") +
                "\r\n" + Languages.Translate("If none of the tracks are matches, the very first track will be choosen.");
            check_tracks_number.ToolTip = combo_tracks_number.ToolTip = Languages.Translate("The track with this ordinal number will be automatically selected during the opening of the file.") +
                "\r\n" + Languages.Translate("If specified value exceeds the amount of the tracks in the file, the very first track will be choosen.");

            //DirectShowSource
            check_dss_convert_fps.IsChecked = Settings.DSS_ConvertFPS;
            check_dss_audio.IsChecked = Settings.DSS_Enable_Audio;

            //MPEG2Source
            check_force_film.IsChecked = Settings.DGForceFilm;
            num_force_film.Value = Settings.DGFilmPercent;

            //FFmpegSource2
            check_ffms_force_fps.IsChecked = Settings.FFMS_AssumeFPS;
            check_ffms_audio.IsChecked = Settings.FFMS_Enable_Audio;
            check_ffms_reindex.IsChecked = Settings.FFMS_Reindex;
            check_ffms_timecodes.IsChecked = Settings.FFMS_TimeCodes;

            combo_ffms_threads.Items.Add("Auto");
            for (int i = 1; i < 21; i++)
                combo_ffms_threads.Items.Add(i);
            combo_ffms_threads.SelectedIndex = Settings.FFMS_Threads;

            //Режим автовыбора треков
            if (Settings.DefaultATrackMode == Settings.ATrackModes.Language)
                check_tracks_language.IsChecked = true;
            else if (Settings.DefaultATrackMode == Settings.ATrackModes.Number)
                check_tracks_number.IsChecked = true;
            else
                check_tracks_manual.IsChecked = true;

            //Языки треков
            ArrayList languages = new ArrayList();
            languages.Add(Settings.DefaultATrackLang);
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                string name = ci.EnglishName.Split(new char[] { ' ' })[0];
                if (!languages.Contains(name))
                    languages.Add(name);
            }
            languages.Sort();
            combo_tracks_language.Items.Add("");
            combo_tracks_language.Items.Add("Unknown");
            foreach (string name in languages) combo_tracks_language.Items.Add(name);
            combo_tracks_language.SelectedItem = Settings.DefaultATrackLang;

            //Номера треков
            for (int i = 1; i < 21; i++)
                combo_tracks_number.Items.Add(i);
            combo_tracks_number.SelectedIndex = Settings.DefaultATrackNum - 1;

            //NicAudio
            check_drc_ac3.IsChecked = Settings.NicAC3_DRC;
            check_drc_dts.IsChecked = Settings.NicDTS_DRC;

            //Audio
            check_enable_audio.IsChecked = Settings.EnableAudio;
            check_new_delay.IsChecked = Settings.NewDelayMethod;
            check_copy_delay.IsChecked = Settings.CopyDelay;

            //Выбираем вкладку
            if (set_focus_to == 2) tab_audio.IsSelected = true;

            //Загружаем Видео\Аудио декодеры
            LoadVDecodersListView(default_index);
            LoadADecodersListView(default_index);

            //Выбираем декодеры
            SelectDecoders();

            ShowDialog();
        }

        void Window_ContentRendered(object sender, EventArgs e)
        {
            //Подгоняем высоту ListView
            double new_height = 136;
            ListView listview = (listview_vdecoders.IsVisible) ? listview_vdecoders : listview_adecoders;
            if (listview.Items.Count > 0)
            {
                ListViewItem item = (listview.ItemContainerGenerator.ContainerFromItem(listview.Items[0]) as ListViewItem);
                if (item != null)
                {
                    int show_items = 6; //Должно уместиться 6 штук
                    double item_height = item.ActualHeight;
                    double thickness = item.BorderThickness.Top;
                    if (!double.IsNaN(item_height) && item_height > 0)
                        new_height = (item_height * show_items) + ((!double.IsNaN(thickness)) ? thickness * show_items : 0) + show_items;
                }
            }

            listview_vdecoders.Height = listview_adecoders.Height = new_height;
            this.Opacity = 1;
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m != null)
            {
                //Новый видео декодер
                m.vdecoder = Format.GetValidVDecoder(m);

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

        private void textbox_Initialized(object sender, EventArgs e)
        {
            //Редактировать можно не всё!
            TextBox text = (TextBox)sender;
            if (text.Text == mpeg_psts || text.Text == other_files)
            {
                text.IsReadOnly = true;
                text.IsHitTestVisible = false;
            }
        }

        private void vcombo_Initialized(object sender, EventArgs e)
        {
            vdecoders_loaded = false;
            ComboBox vcombo = (ComboBox)sender;
            vcombo.Items.Clear();

            //Из-за глюков с IemsSource+ComboBoxItem+Binding приходится вот так вот
            //извращаться с заполнением комбобокса и выбором нужного элемента из списка..
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_avi, ToolTip = tltp_avi });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_m2s, ToolTip = tltp_m2s });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_dss, ToolTip = tltp_dss });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_dss2, ToolTip = tltp_dss2 });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_ffms, ToolTip = tltp_ffms });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_qts, ToolTip = tltp_qts });
            vcombo.SelectedIndex = (int)vcombo.Tag;
            vdecoders_loaded = true;
        }

        private void acombo_Initialized(object sender, EventArgs e)
        {
            adecoders_loaded = false;
            ComboBox acombo = (ComboBox)sender;
            acombo.Items.Clear();

            //Из-за глюков с IemsSource+ComboBoxItem+Binding приходится вот так вот
            //извращаться с заполнением комбобокса и выбором нужного элемента из списка..
            acombo.Items.Add(new ComboBoxItem() { Content = dec_ac3, ToolTip = tltp_ac3 });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_mp3, ToolTip = tltp_mp3 });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_dts, ToolTip = tltp_dts });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_rawav, ToolTip = tltp_rawav });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_wav, ToolTip = tltp_wav });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_bass, ToolTip = tltp_bass });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_ffas, ToolTip = tltp_ffas });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_dss, ToolTip = tltp_dss });
            acombo.SelectedIndex = (int)acombo.Tag;
            adecoders_loaded = true;
        }

        private void LoadVDecodersListView(int item_index)
        {
            vdecoders_loaded = false;
            listview_vdecoders.Items.Clear();

            int mpeg_index = -1, other_index = 2;
            string mpeg_dec = "", other_dec = dec_dss; //Дефолты
            foreach (string line in (Settings.VDecoders.ToLower().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)))
            {
                string[] extension_and_decoder = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (extension_and_decoder.Length == 2)
                {
                    int index = -1;
                    string ext = extension_and_decoder[0].Trim();
                    string dec = extension_and_decoder[1].Trim();

                    //Определяем декодер (и его индекс в комбобоксе) без учета регистра
                    if (dec.Equals(dec_avi, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_avi; index = 0; }
                    else if (dec.Equals(dec_m2s, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_m2s; index = 1; }
                    else if (dec.Equals(dec_dss, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_dss; index = 2; }
                    else if (dec.Equals(dec_dss2, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_dss2; index = 3; }
                    else if (dec.Equals(dec_ffms, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_ffms; index = 4; }
                    else if (dec.Equals(dec_qts, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_qts; index = 5; }

                    //Сортировка начала\конца списка
                    if (ext == mpeg_psts) { mpeg_dec = dec; mpeg_index = index; } //Это в начало
                    else if (ext == other_files) { other_dec = dec; other_index = index; } //Это в конец
                    else listview_vdecoders.Items.Add(new MyObj() { Extension = ext, Decoder = dec, Index = index });
                }
            }

            //Начало списка
            if (mpeg_dec.Length > 0)
                listview_vdecoders.Items.Insert(0, new MyObj() { Extension = mpeg_psts, Decoder = mpeg_dec, ToolTip = "MPEG2-PS/TS " + Languages.Translate("files"), Index = mpeg_index });

            //Конец списка
            if (other_dec.Length > 0)
                listview_vdecoders.Items.Add(new MyObj() { Extension = other_files, Decoder = other_dec, ToolTip = Languages.Translate("Other files"), Index = other_index });

            listview_vdecoders.SelectedIndex = (item_index >= listview_vdecoders.Items.Count) ? listview_vdecoders.Items.Count - 1 : item_index;
            vdecoders_loaded = true;
        }

        private void LoadADecodersListView(int item_index)
        {
            adecoders_loaded = false;
            listview_adecoders.Items.Clear();

            string other_dec = dec_bass; int other_index = 5; //Дефолты
            foreach (string line in (Settings.ADecoders.ToLower().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)))
            {
                string[] extension_and_decoder = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (extension_and_decoder.Length == 2)
                {
                    int index = -1;
                    string ext = extension_and_decoder[0].Trim();
                    string dec = extension_and_decoder[1].Trim();

                    //Определяем декодер (и его индекс в комбобоксе) без учета регистра
                    if (dec.Equals(dec_ac3, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_ac3; index = 0; }
                    else if (dec.Equals(dec_mp3, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_mp3; index = 1; }
                    else if (dec.Equals(dec_dts, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_dts; index = 2; }
                    else if (dec.Equals(dec_rawav, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_rawav; index = 3; }
                    else if (dec.Equals(dec_wav, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_wav; index = 4; }
                    else if (dec.Equals(dec_bass, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_bass; index = 5; }
                    else if (dec.Equals(dec_ffas, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_ffas; index = 6; }
                    else if (dec.Equals(dec_dss, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_dss; index = 7; }

                    //Сортировка начала\конца списка
                    if (ext == other_files) { other_dec = dec; other_index = index; } //Это в конец
                    else listview_adecoders.Items.Add(new MyObj() { Extension = ext, Decoder = dec, Index = index });
                }
            }

            //Конец списка
            if (other_dec.Length > 0)
                listview_adecoders.Items.Add(new MyObj() { Extension = other_files, Decoder = other_dec, ToolTip = Languages.Translate("Other files"), Index = other_index });

            listview_adecoders.SelectedIndex = (item_index >= listview_adecoders.Items.Count) ? listview_adecoders.Items.Count - 1 : item_index;
            adecoders_loaded = true;
        }

        private void SelectDecoders()
        {
            if (m == null) return;

            //Выбираем видео декодер
            if (m.vdecoder > 0)
            {
                int vid = -1, vnum = 0;
                string vext = Path.GetExtension(m.infilepath).ToLower().TrimStart(new char[] { '.' });
                tab_video_Header.ToolTip = Languages.Translate("File extension:") + " " + vext + "\r\n" + Languages.Translate("Current decoder") + ": " + m.vdecoder.ToString();
                if (m.isvideo && vext != "d2v" && vext != "dga" && vext != "dgi" && vext != "grf" && vext != "avs")
                {
                    foreach (MyObj obj in listview_vdecoders.Items)
                    {
                        if (obj.Extension == vext)
                        {
                            vid = vnum; break;
                        }
                        vnum += 1;
                    }

                    if (vid < 0 && vext != "pmp" && vext != "vdr" && vext != "y4m" && vext != "yuv")
                    {
                        if (Calculate.IsMPEG(m.infilepath) && (m.invcodecshort == "MPEG1" || m.invcodecshort == "MPEG2"))
                        { if (((MyObj)listview_vdecoders.Items[0]).Extension == mpeg_psts) vid = 0; }
                        else vid = listview_vdecoders.Items.Count - 1;
                    }

                    if (vid >= 0)
                    {
                        listview_vdecoders.SelectedIndex = vid;
                        if (listview_vdecoders.IsVisible)
                            listview_vdecoders.ScrollIntoView(listview_vdecoders.SelectedItem);
                    }
                }
            }

            //Выбираем аудио декодер
            if (m.inaudiostreams.Count > 0)
            {
                AudioStream stream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                if (stream.decoder > 0)
                {
                    int aud = -1, anum = 0;
                    string aext = Path.GetExtension(stream.audiopath).ToLower().TrimStart(new char[] { '.' });
                    tab_audio_Header.ToolTip = Languages.Translate("File extension:") + " " + aext + "\r\n" + Languages.Translate("Current decoder") + ": " + stream.decoder.ToString();
                    if (aext != "avs" && aext != "grf")
                    {
                        foreach (MyObj obj in listview_adecoders.Items)
                        {
                            if (obj.Extension == aext)
                            {
                                aud = anum; break;
                            }
                            anum += 1;
                        }

                        if (aud < 0)
                            listview_adecoders.SelectedIndex = listview_adecoders.Items.Count - 1;

                        if (aud >= 0)
                        {
                            listview_adecoders.SelectedIndex = aud;
                            if (listview_adecoders.IsVisible)
                                listview_adecoders.ScrollIntoView(listview_adecoders.SelectedItem);
                        }
                    }
                }
            }
        }

        void listview_Loaded(object sender, RoutedEventArgs e)
        {
            //Делаем ScrollIntoView для выбранной строки, т.к.
            //на автомате оно иногда делается через одно место
            ListView listview = (ListView)sender;
            if (listview.SelectedItem != null)
            {
                //Просто Loaded или Initialized не достаточно, поэтому с задержкой, хоть это и не правильно..
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                timer.Tick += (a, b) =>
                {
                    if (listview.SelectedItem != null)
                        listview.ScrollIntoView(listview.SelectedItem);
                    timer.Stop();
                    timer = null;
                };
                timer.Start();
            }
        }

        private void combo_decoder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!vdecoders_loaded || !adecoders_loaded) return;

            ComboBox combo = (ComboBox)sender;
            ListView listview = (combo.IsDescendantOf(listview_vdecoders)) ? listview_vdecoders : listview_adecoders;
            if ((combo.IsDropDownOpen || combo.IsSelectionBoxHighlighted) && combo.SelectedItem != null && listview.SelectedItem != null)
            {
                string result = "";
                int index = listview.SelectedIndex;
                for (int i = 0; i < listview.Items.Count; i++)
                {
                    if (i == index)
                    {
                        result += ((MyObj)listview.Items[i]).Extension + "=" + ((ComboBoxItem)combo.SelectedItem).Content.ToString() + "; ";
                    }
                    else
                    {
                        MyObj obj = (MyObj)listview.Items[i];
                        result += obj.Extension + "=" + obj.Decoder + "; ";
                    }
                }

                if (combo.IsDescendantOf(listview_vdecoders))
                {
                    Settings.VDecoders = result;
                    LoadVDecodersListView(index);
                }
                else
                {
                    Settings.ADecoders = result;
                    LoadADecodersListView(index);
                }
            }
        }

        private void textbox_ext_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!vdecoders_loaded || !adecoders_loaded || text_editing) return;

            TextBox textbox = (TextBox)sender;
            ListView listview = (textbox.IsDescendantOf(listview_vdecoders)) ? listview_vdecoders : listview_adecoders;
            if (textbox.IsFocused && listview.SelectedItem != null)
            {
                text_editing = true;

                //Проверяем, что введено
                string new_ext = "";
                string old_ext = ((MyObj)listview.SelectedItem).Extension;
                if (old_ext == mpeg_psts || old_ext == other_files || (new_ext = Calculate.GetRegexValue(@"^([0-9a-z]{1,6})$", textbox.Text.ToLower())) == null)
                {
                    textbox.Text = old_ext;
                    text_editing = false;
                    return;
                }

                //Проверяем, что такого расширения еще нет в списке
                foreach (MyObj obj in listview.Items)
                {
                    if (new_ext == obj.Extension)
                    {
                        textbox.Text = old_ext;
                        text_editing = false;
                        return;
                    }
                }

                string result = "";
                int index = listview.SelectedIndex;
                for (int i = 0; i < listview.Items.Count; i++)
                {
                    if (i == index)
                    {
                        result += new_ext + "=" + ((MyObj)listview.Items[i]).Decoder + "; ";
                    }
                    else
                    {
                        MyObj obj = (MyObj)listview.Items[i];
                        result += obj.Extension + "=" + obj.Decoder + "; ";
                    }
                }

                if (textbox.IsDescendantOf(listview_vdecoders))
                    Settings.VDecoders = result;
                else
                    Settings.ADecoders = result;

                ((MyObj)listview.SelectedItem).Extension = new_ext;
                text_editing = false;
            }
        }

        private void button_decoder_delete_Click(object sender, RoutedEventArgs e)
        {
            ListView listview = (sender == button_vdec_delete) ? listview_vdecoders : listview_adecoders;
            if (vdecoders_loaded && adecoders_loaded && listview.SelectedItem != null)
            {
                //Удалять можно не всё!
                string old_ext = ((MyObj)listview.SelectedItem).Extension;
                if (old_ext == mpeg_psts || old_ext == other_files) return;

                string result = "";
                int index = listview.SelectedIndex;
                for (int i = 0; i < listview.Items.Count; i++)
                {
                    if (i != index)
                    {
                        MyObj obj = (MyObj)listview.Items[i];
                        result += obj.Extension + "=" + obj.Decoder + "; ";
                    }
                }

                if (sender == button_vdec_delete)
                {
                    Settings.VDecoders = result;
                    LoadVDecodersListView(index);
                }
                else
                {
                    Settings.ADecoders = result;
                    LoadADecodersListView(index);
                }
            }
        }

        private void button_decoder_add_Click(object sender, RoutedEventArgs e)
        {
            ListView listview = (sender == button_vdec_add) ? listview_vdecoders : listview_adecoders;

            //Проверяем, что такого расширения еще нет в списке
            for (int i = 0; i < listview.Items.Count; i++)
            {
                if (((MyObj)listview.Items[i]).Extension == "edit")
                {
                    //А если есть, то переводим на него фокус
                    listview.SelectedIndex = i;
                    listview.ScrollIntoView(listview.Items[i]);
                    return;
                }
            }

            //Добавляем
            if (sender == button_vdec_add)
            {
                string result = Settings.VDecoders.Trim();
                if (result.EndsWith(";")) result += " edit=" + dec_dss;
                else result += "; edit=" + dec_dss;

                Settings.VDecoders = result;
                LoadVDecodersListView(listview_vdecoders.Items.Count - 1);
            }
            else
            {
                string result = Settings.ADecoders.Trim();
                if (result.EndsWith(";")) result += " edit=" + dec_bass;
                else result += "; edit=" + dec_bass;

                Settings.ADecoders = result;
                LoadADecodersListView(listview_adecoders.Items.Count - 1);
            }

            listview.ScrollIntoView(listview.SelectedItem);
        }

        private void button_decoders_reset_Click(object sender, RoutedEventArgs e)
        {
            if (sender == button_vdec_reset)
            {
                Settings.VDecoders = "";
                LoadVDecodersListView(default_index);
                if (m != null) SelectDecoders();
                else listview_Loaded(listview_vdecoders, null);
            }
            else
            {
                Settings.ADecoders = "";
                LoadADecodersListView(default_index);
                if (m != null) SelectDecoders();
                else listview_Loaded(listview_adecoders, null);
            }
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //Подгоняем ширину второй колонки ListView
            ListView listview = (ListView)sender;
            if (listview.IsVisible && e.ViewportHeightChange > 0 && e.ViewportWidthChange > 0)
            {
                //С учётом скроллбара
                double scroll_width = 0;
                Decorator border = VisualTreeHelper.GetChild(listview, 0) as Decorator;
                if (border != null)
                {
                    ScrollViewer scroll = border.Child as ScrollViewer;
                    scroll_width = (scroll != null && scroll.ComputedVerticalScrollBarVisibility == Visibility.Visible) ? SystemParameters.VerticalScrollBarWidth : 0;
                }

                if (listview == listview_vdecoders)
                    vdecoder.Width = listview.Width - vextension.Width - scroll_width - 5;
                else
                    adecoder.Width = listview.Width - aextension.Width - scroll_width - 5;
            }
        }

        private void combo_Closed(object sender, EventArgs e)
        {
            //Уводим фокус с комбобокса, чтоб случайно не изменить
            //выбор после его закрытия (при попытке скроллить)
            if (((ComboBox)sender).IsDescendantOf(listview_vdecoders))
                listview_vdecoders.Focus();
            else
                listview_adecoders.Focus();
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

        private void check_ffms_force_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFMS_AssumeFPS = check_ffms_force_fps.IsChecked.Value;
        }

        private void check_ffms_audio_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFMS_Enable_Audio = check_ffms_audio.IsChecked.Value;
        }

        private void check_ffms_reindex_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFMS_Reindex = check_ffms_reindex.IsChecked.Value;
        }

        private void check_ffms_timecodes_Click(object sender, RoutedEventArgs e)
        {
            Settings.FFMS_TimeCodes = check_ffms_timecodes.IsChecked.Value;
        }

        private void check_drc_ac3_Click(object sender, RoutedEventArgs e)
        {
            Settings.NicAC3_DRC = check_drc_ac3.IsChecked.Value;
        }

        private void check_drc_dts_Click(object sender, RoutedEventArgs e)
        {
            Settings.NicDTS_DRC = check_drc_dts.IsChecked.Value;
        }

        private void check_new_delay_Click(object sender, RoutedEventArgs e)
        {
            Settings.NewDelayMethod = check_new_delay.IsChecked.Value;
        }

        private void check_copy_delay_Click(object sender, RoutedEventArgs e)
        {
            Settings.CopyDelay = check_copy_delay.IsChecked.Value;
        }

        private void check_enable_audio_Click(object sender, RoutedEventArgs e)
        {
            Settings.EnableAudio = check_enable_audio.IsChecked.Value;
        }

        private void combo_ffms_threads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_ffms_threads.IsDropDownOpen || combo_ffms_threads.IsSelectionBoxHighlighted)
            {
                Settings.FFMS_Threads = combo_ffms_threads.SelectedIndex;
            }
        }

        private void check_tracks_Click(object sender, RoutedEventArgs e)
        {
            if (sender == check_tracks_manual)
            {
                check_tracks_language.IsChecked = check_tracks_number.IsChecked = false;
                Settings.DefaultATrackMode = Settings.ATrackModes.Manual;
            }
            else if (sender == check_tracks_language)
            {
                check_tracks_manual.IsChecked = check_tracks_number.IsChecked = false;
                Settings.DefaultATrackMode = Settings.ATrackModes.Language;
            }
            else if (sender == check_tracks_number)
            {
                check_tracks_manual.IsChecked = check_tracks_language.IsChecked = false;
                Settings.DefaultATrackMode = Settings.ATrackModes.Number;
            }
        }

        private void combo_tracks_language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_tracks_language.IsDropDownOpen || combo_tracks_language.IsSelectionBoxHighlighted || combo_tracks_language.IsEditable) &&
                combo_tracks_language.SelectedItem != null)
            {
                if (combo_tracks_language.SelectedIndex == 0)
                {
                    //Включаем редактирование
                    combo_tracks_language.IsEditable = true;
                    combo_tracks_language.ToolTip = Languages.Translate("Enter - apply, Esc - cancel.");
                    combo_tracks_language.ApplyTemplate();
                    return;
                }
                else
                {
                    Settings.DefaultATrackLang = combo_tracks_language.SelectedItem.ToString();
                }

                if (combo_tracks_language.IsEditable)
                {
                    //Выключаем редактирование
                    combo_tracks_language.IsEditable = false;
                    combo_tracks_language.ToolTip = null;
                }
            }
        }

        private void combo_tracks_language_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                //Проверяем введённый текст
                string text = combo_tracks_language.Text.Trim();
                if (text.Length < 4 || Calculate.GetRegexValue(@"^([a-zA-Z\s]+)$", text) == null)
                {
                    //Возвращаем исходное значение
                    combo_tracks_language.SelectedItem = Settings.DefaultATrackLang;
                }
                else
                {
                    //Форматируем текст
                    text = text.Substring(0, 1).ToUpper() + text.Substring(1, text.Length - 1).ToLower();

                    //Добавляем и выбираем Item
                    if (!combo_tracks_language.Items.Contains(text))
                        combo_tracks_language.Items.Add(text);
                    combo_tracks_language.SelectedItem = text;
                }
            }
            else if (e.Key == Key.Escape)
            {
                //Возвращаем исходное значение
                combo_tracks_language.SelectedItem = Settings.DefaultATrackLang;
            }
        }

        private void combo_tracks_language_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            if (box.IsEditable && box.SelectedItem != null && !box.IsDropDownOpen && !box.IsMouseCaptured)
                combo_tracks_language_KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter));
        }

        private void combo_tracks_number_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_tracks_number.IsDropDownOpen || combo_tracks_number.IsSelectionBoxHighlighted)
            {
                Settings.DefaultATrackNum = combo_tracks_number.SelectedIndex + 1;
            }
        }
    }
}