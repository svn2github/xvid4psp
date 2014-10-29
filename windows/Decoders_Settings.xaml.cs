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
        private string dec_lwlv = AviSynthScripting.Decoders.LWLibavVideoSource.ToString();
        private string dec_lsmv = AviSynthScripting.Decoders.LSMASHVideoSource.ToString();
        private string dec_qts = AviSynthScripting.Decoders.QTInput.ToString();
        private string dec_dgnv = AviSynthScripting.Decoders.DGSource.ToString();

        //Список аудио декодеров
        private string dec_ac3 = AviSynthScripting.Decoders.NicAC3Source.ToString();
        private string dec_mp3 = AviSynthScripting.Decoders.NicMPG123Source.ToString();
        private string dec_dts = AviSynthScripting.Decoders.NicDTSSource.ToString();
        private string dec_rawav = AviSynthScripting.Decoders.RaWavSource.ToString();
        private string dec_wav = AviSynthScripting.Decoders.WAVSource.ToString();
        private string dec_bass = AviSynthScripting.Decoders.bassAudioSource.ToString();
        private string dec_ffas = AviSynthScripting.Decoders.FFAudioSource.ToString();
        private string dec_lwla = AviSynthScripting.Decoders.LWLibavAudioSource.ToString();
        private string dec_lsma = AviSynthScripting.Decoders.LSMASHAudioSource.ToString();

        //Подсказки к видео декодерам
        private string tltp_avi = Languages.Translate("This decoder uses VFW AVIFile interface, or AviSynth`s built-in OpenDML code (taken from VirtualDub).") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": avi, vdr";
        private string tltp_m2s = "(DGIndex, DGMPGDec)\r\n" + Languages.Translate("Probably the best choice for MPEG-files. It provides frame accurate seeking and uses internal MPEG1/2 decoder.") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": MPEG2-PS/TS (mpg, vob, mod,..)";
        private string tltp_dss = Languages.Translate("This decoder uses installed on your system DirecShow filters (splitters and decoders, including their settings!) for decoding.") +
            "\r\n" + Languages.Translate("Frame accurate seeking is not guaranteed!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_dss2 = Languages.Translate("Mostly the same as DirectShowSource, but from Haali. It provides frame accurate seeking when it`s possible.") +
            "\r\n" + Languages.Translate("Current modded version also allow you to use LAV Filters in portable mode, without interference from any other DirectShow filters.") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_ffms = Languages.Translate("This decoder (based on FFmpeg) uses their own splitters and decoders, but requires some extra time for indexing your file.") +
            "\r\n" + Languages.Translate("However, Haali Media Splitter is still required if you want to open MPEG-PS/TS or OGM files with this decoder.") +
            "\r\n" + Languages.Translate("Decoding of interlaced H.264 may be broken (due to limitations of FFmpeg)!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_lwlv = Languages.Translate("This decoder (based on FFmpeg) uses their own splitters and decoders, but requires some extra time for indexing your file.").Replace("FFmpeg", "Libav") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_lsmv = Languages.Translate("This decoder (based on L-SMASH) uses their own splitters and decoders.") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": MPEG-4 (mp4, mov, 3gp)";
        private string tltp_qts = Languages.Translate("This decoder uses QuickTime environment for decoding, so QuickTime is required!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": mov";
        private string tltp_dgnv = "(DGIndexNV, DGDecNV)\r\n" + Languages.Translate("This decoder uses an NVIDIA video card for decoding, it provides frame accurate seeking.") +
            "\r\n" + Languages.Translate("You need to obtain your licensed copy of the DGDecNV package from the author (Donald Graft) in order to use it!") +
            "\r\n\r\n" + Languages.Translate("Supported formats") + ": MPEG-PS/TS, MP4, MKV; MPEG2, H264, VC1";

        //Подсказки к аудио декодерам
        private string tltp_ac3 = Languages.Translate("Supported formats") + ": ac3";
        private string tltp_mp3 = Languages.Translate("Supported formats") + ": mpa, mp1, mp2, mp3";
        private string tltp_dts = Languages.Translate("Supported formats") + ": dts, dtswav";
        private string tltp_rawav = Languages.Translate("Supported formats") + ": wav, w64, lpcm";
        private string tltp_wav = Languages.Translate("Supported formats") + ": wav (2Gb max!)";
        private string tltp_bass = Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_ffas = Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_lwla = Languages.Translate("Supported formats") + ": " + Languages.Translate("various (multi-format decoder)");
        private string tltp_lsma = Languages.Translate("Supported formats") + ": MPEG-4 (m4a, mp4, mov, 3gp)";

        //Дефолты
        string on = Languages.Translate("On");
        string off = Languages.Translate("Off");
        string _def = Languages.Translate("Default") + ": ";

        public Massive m;
        private string old_raw_script;
        public bool NeedUpdate = false;
        private bool vdecoders_loaded = false;
        private bool adecoders_loaded = false;
        private bool text_editing = false;
        private int default_index = 0;

        private string lavs_language = "";
        private string lavs_advanced = "";

        public Decoders_Settings(Massive mass, System.Windows.Window owner, int set_focus_to)
        {
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
            check_dss_convert_fps.ToolTip = _def + on;
            check_dss_audio.Content = check_ffms_audio.Content = check_lsmash_audio.Content = Languages.Translate("Enable Audio");
            check_dss_audio.ToolTip = Languages.Translate("Allow DirectShowSource to decode audio directly from the source-file (without demuxing)");
            check_force_film.Content = Languages.Translate("Auto force Film at") + " (%):";
            check_force_film.ToolTip = Languages.Translate("Auto force Film if Film percentage is more than selected value (for NTSC sources only)") + "\r\n\r\n" + _def + on;
            num_force_film.ToolTip = _def + "95";
            check_ffms_force_fps.Content = check_lsmash_force_fps.Content = Languages.Translate("Add AssumeFPS()");
            check_ffms_force_fps.ToolTip = check_lsmash_force_fps.ToolTip = Languages.Translate("Force FPS using AssumeFPS()") + "\r\n\r\n" + _def + on;
            check_ffms_audio.ToolTip = check_dss_audio.ToolTip.ToString().Replace("DirectShowSource", "FFmpegSource2") + "\r\n\r\n" + _def + off;
            check_lsmash_audio.ToolTip = check_dss_audio.ToolTip.ToString().Replace("DirectShowSource", "LSMASH/LWLibav") + "\r\n\r\n" + _def + on;
            check_dss_audio.ToolTip += "\r\n\r\n" + _def + on;
            check_ffms_reindex.Content = Languages.Translate("Overwrite existing index files");
            check_ffms_reindex.ToolTip = Languages.Translate("Always re-index the source file and overwrite existing index file, even if it was valid") + "\r\n\r\n" + _def + off;
            check_ffms_timecodes.Content = Languages.Translate("Timecodes");
            check_ffms_timecodes.ToolTip = Languages.Translate("Extract timecodes to a file") + "\r\n\r\n" + _def + off;
            combo_ffms_threads.ToolTip = label_ffms_threads.ToolTip = "1 = " + Languages.Translate("disable multithreading (recommended)") + "\r\nAuto = " +
                Languages.Translate("logical CPU's count") + "\r\n\r\n" + Languages.Translate("Attention! Multithreaded decoding can be unstable!") + "\r\n\r\n" + _def + "1";
            label_ffms_threads.Content = label_lsmash_threads.Content = "- " + Languages.Translate("Decoding threads").ToLower();
            combo_lsmash_threads.ToolTip = label_lsmash_threads.ToolTip = _def + "Auto";

            check_drc_ac3.ToolTip = check_drc_dts.ToolTip = Languages.Translate("Apply DRC (Dynamic Range Compression) for this decoder") + "\r\n\r\n" + _def + off;
            group_misc.Header = Languages.Translate("Misc");
            check_enable_audio.Content = Languages.Translate("Enable audio in input files");
            check_enable_audio.ToolTip = Languages.Translate("If checked, input files will be opened with audio, otherwise they will be opened WITHOUT audio!")
                + "\r\n" + Languages.Translate("Audio files - exception, they always will be opened.") + "\r\n\r\n" + _def + on;
            check_new_delay.Content = Languages.Translate("Use new Delay calculation method");
            check_new_delay.ToolTip = Languages.Translate("A new method uses the difference between video and audio delays, while old method uses audio delay only.") + "\r\n\r\n" + _def + off;
            check_copy_delay.Content = Languages.Translate("Apply Delay in Copy mode");
            check_copy_delay.ToolTip = _def + off;
            button_vdec_add.Content = button_adec_add.Content = Languages.Translate("Add");
            button_vdec_delete.Content = button_adec_delete.Content = Languages.Translate("Remove");
            button_vdec_reset.Content = button_adec_reset.Content = Languages.Translate("Reset");
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

            //DirectShowSource2
            check_dss2_lavs.ToolTip = Languages.Translate("Use LAV Splitter to separate streams.");
            check_dss2_lavd.ToolTip = Languages.Translate("Use LAV Video Decoder to decode video stream.") + "\r\n" +
                Languages.Translate("If enabled, any other installed DirectShow filters will not be used and they will not affect the output!");
            button_lavs.ToolTip = check_dss2_lavs.ToolTip + "\r\n\r\n" + Languages.Translate("Press this button to open LAV Splitter settings tab.");
            button_lavd.ToolTip = check_dss2_lavd.ToolTip + "\r\n\r\n" + Languages.Translate("Press this button to open LAV Video Decoder settings tab.");
            check_dss2_lavs.ToolTip += "\r\n\r\n" + _def + on;
            check_dss2_lavd.ToolTip += "\r\n\r\n" + _def + on;

            check_dss2_lavs.IsChecked = Settings.DSS2_LAVSplitter;
            check_dss2_lavd.IsChecked = Settings.DSS2_LAVDecoder;

            string txt_no = Languages.Translate("No");
            label_dss2_subsm.ToolTip = Languages.Translate("Subtitles mode") + "\r\n\r\n" + _def + txt_no;
            combo_dss2_subsm.Items.Add(new ComboBoxItem() { Content = txt_no, ToolTip = Languages.Translate("Do not load subtitles") });
            combo_dss2_subsm.Items.Add(new ComboBoxItem() { Content = "1", ToolTip = Languages.Translate("Try to load subtitles (with LAV Decoder = \"No\")") });
            combo_dss2_subsm.Items.Add(new ComboBoxItem() { Content = "2", ToolTip = Languages.Translate("Force loading DirectVobSub and then try to load subtitles") });
            combo_dss2_subsm.Tag = _def + txt_no;
            combo_dss2_subsm.SelectedIndex = Settings.DSS2_SubsMode;

            check_dss2_flipv.ToolTip = Languages.Translate("Flip vertical") + "\r\n\r\n" + _def + off;
            check_dss2_fliph.ToolTip = Languages.Translate("Flip horizontal") + "\r\n\r\n" + _def + off;

            check_dss2_flipv.IsChecked = Settings.DSS2_FlipV;
            check_dss2_fliph.IsChecked = Settings.DSS2_FlipH;

            label_dss2_preroll.ToolTip = num_dss2_preroll.ToolTip = Languages.Translate("Preroll - it's a number of frames that will be minused from the requested frame No. when seeking to desired position.") +
                "\r\n" + Languages.Translate("All \"extra-frames\" will be read frame-by-frame till we get what we want (slower, but more precise way).") +
                "\r\n" + Languages.Translate("If after the seeking you can see artifacts or frozen frames for some time - you may want to increase this value.") +
                "\r\n\r\n" + _def + "15";
            num_dss2_preroll.Value = Settings.DSS2_Preroll;

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

            check_lsmash_force_fps.IsChecked = Settings.LSMASH_AssumeFPS;
            check_lsmash_audio.IsChecked = Settings.LSMASH_Enable_Audio;

            combo_lsmash_threads.Items.Add("Auto");
            for (int i = 1; i < 21; i++)
                combo_lsmash_threads.Items.Add(i);
            combo_lsmash_threads.SelectedIndex = Settings.LSMASH_Threads;

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

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);

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

            listview.UpdateLayout();
            this.InvalidateVisual();
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
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_lwlv, ToolTip = tltp_lwlv });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_lsmv, ToolTip = tltp_lsmv });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_qts, ToolTip = tltp_qts });
            vcombo.Items.Add(new ComboBoxItem() { Content = dec_dgnv, ToolTip = tltp_dgnv });
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
            acombo.Items.Add(new ComboBoxItem() { Content = dec_lwla, ToolTip = tltp_lwla });
            acombo.Items.Add(new ComboBoxItem() { Content = dec_lsma, ToolTip = tltp_lsma });
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
                    else if (dec.Equals(dec_lwlv, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_lwlv; index = 5; }
                    else if (dec.Equals(dec_lsmv, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_lsmv; index = 6; }
                    else if (dec.Equals(dec_qts, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_qts; index = 7; }
                    else if (dec.Equals(dec_dgnv, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_dgnv; index = 8; }

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
                    else if (dec.Equals(dec_lwla, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_lwla; index = 8; }
                    else if (dec.Equals(dec_lsma, StringComparison.InvariantCultureIgnoreCase)) { dec = dec_lsma; index = 9; }

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

        private void listview_Loaded(object sender, RoutedEventArgs e)
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

            //Мы всегда оказываемся здесь при переключении табов
            tab_lav_splitter.Visibility = Visibility.Collapsed;
            tab_lav_decoder.Visibility = Visibility.Collapsed;
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
            if ((combo_ffms_threads.IsDropDownOpen || combo_ffms_threads.IsSelectionBoxHighlighted) && combo_ffms_threads.SelectedIndex != -1)
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
            if ((combo_tracks_number.IsDropDownOpen || combo_tracks_number.IsSelectionBoxHighlighted) && combo_tracks_number.SelectedIndex != -1)
            {
                Settings.DefaultATrackNum = combo_tracks_number.SelectedIndex + 1;
            }
        }

        private void check_dss2_lavs_Click(object sender, RoutedEventArgs e)
        {
            Settings.DSS2_LAVSplitter = check_dss2_lavs.IsChecked.Value;
        }

        private void check_dss2_lavd_Click(object sender, RoutedEventArgs e)
        {
            Settings.DSS2_LAVDecoder = check_dss2_lavd.IsChecked.Value;
        }

        private void combo_dss2_subsm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_dss2_subsm.SelectedIndex != -1)
            {
                if (combo_dss2_subsm.IsDropDownOpen || combo_dss2_subsm.IsSelectionBoxHighlighted)
                    Settings.DSS2_SubsMode = combo_dss2_subsm.SelectedIndex;

                combo_dss2_subsm.ToolTip = ((ComboBoxItem)combo_dss2_subsm.SelectedItem).ToolTip + "\r\n\r\n" + combo_dss2_subsm.Tag;
            }
        }

        private void check_dss2_flipv_Click(object sender, RoutedEventArgs e)
        {
            Settings.DSS2_FlipV = check_dss2_flipv.IsChecked.Value;
        }

        private void check_dss2_fliph_Click(object sender, RoutedEventArgs e)
        {
            Settings.DSS2_FlipH = check_dss2_fliph.IsChecked.Value;
        }

        private void num_dss2_preroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_dss2_preroll.IsAction)
            {
                Settings.DSS2_Preroll = Convert.ToInt32(num_dss2_preroll.Value);
            }
        }

        private void button_lavs_Click(object sender, RoutedEventArgs e)
        {
            if (tab_lav_splitter.Tag == null) LoadLAVSSettings();
            tab_lav_splitter.Visibility = Visibility.Visible;
            tab_lav_splitter.IsSelected = true;
        }

        private void button_lavd_Click(object sender, RoutedEventArgs e)
        {
            if (tab_lav_decoder.Tag == null) LoadLAVVSettings();
            tab_lav_decoder.Visibility = Visibility.Visible;
            tab_lav_decoder.IsSelected = true;
        }

        private void LoadLAVSSettings()
        {
            label_lavs_loading.Content = Languages.Translate("Loading mode") + ":";
            combo_lavs_loading.Items.Add(new ComboBoxItem() { Content = "System", ToolTip = Languages.Translate("Load installed version with its settings, every other options on this tab will be ignored!") });
            combo_lavs_loading.Items.Add(new ComboBoxItem() { Content = "Portable", ToolTip = Languages.Translate("Load portable version, that not depend on what you have installed and how it tuned.") });
            combo_lavs_loading.Tag = _def + "Portable";
            combo_lavs_loading.SelectedIndex = 1;

            label_lavs_vc1.Content = Languages.Translate("VC-1 correction") + ":";
            combo_lavs_vc1.Items.Add(new ComboBoxItem() { Content = "Disabled", ToolTip = Languages.Translate("No timestamps correction") });
            combo_lavs_vc1.Items.Add(new ComboBoxItem() { Content = "Always", ToolTip = Languages.Translate("Always timestamps correction") });
            combo_lavs_vc1.Items.Add(new ComboBoxItem() { Content = "Auto", ToolTip = Languages.Translate("Timestamps correction only for decoders, that need it") });
            combo_lavs_vc1.Tag = _def + "Auto";
            combo_lavs_vc1.SelectedIndex = 2;

            label_lavs_subsm.Content = Languages.Translate("Subtitles mode") + ":";
            combo_lavs_subsm.Items.Add(new ComboBoxItem() { Content = "Disabled", ToolTip = Languages.Translate("Disable subtitles") });
            combo_lavs_subsm.Items.Add(new ComboBoxItem() { Content = "Forced", ToolTip = Languages.Translate("Forced only") });
            combo_lavs_subsm.Items.Add(new ComboBoxItem() { Content = "Default", ToolTip = Languages.Translate("Default mode") });
            combo_lavs_subsm.Items.Add(new ComboBoxItem() { Content = "Advanced", ToolTip = Languages.Translate("Advanced mode") });
            combo_lavs_subsm.Tag = _def + "Default";
            combo_lavs_subsm.SelectedIndex = 2;

            label_lavs_subsl.Content = Languages.Translate("Preferred subtitles languages") + ":";
            label_lavs_subsa.Content = Languages.Translate("Advanced subtitles settings") + ":";
            textbox_lavs_subsl.ToolTip = Languages.Translate("As ISO 639-2 language codes.") + "\r\n" + Languages.Translate("Separate by comma.") + "\r\n\r\n" + _def + Languages.Translate("(empty)");
            textbox_lavs_subsa.ToolTip = Languages.Translate("Please refer to MUXER documentation for more info").Replace("MUXER", "LAV Filters") + ".\r\n" + Languages.Translate("Separate by comma.") +
                "\r\n\r\n" + _def + Languages.Translate("(empty)");

            //"l3 vc2 sm2 sl[] sa[] ti0"
            string s = Settings.DSS2_LAVS_Settings.ToLower();
            bool l_args = false, a_args = false;

            for (int i = 0; i < s.Length; i++)
            {
                if (!l_args && !a_args)
                {
                    char next_ch = (i + 1 < s.Length) ? s[i + 1] : '\0';
                    char nnext_ch = (i + 2 < s.Length) ? s[i + 2] : '\0';

                    //48 = 0, 49 = 1, .. 57 = 9
                    int next_i = (next_ch >= 48 && next_ch <= 57) ? next_ch - 48 : -1;
                    int nnext_i = (nnext_ch >= 48 && nnext_ch <= 57) ? nnext_ch - 48 : -1;

                    if (s[i] == 'l' && next_i >= 0) //l - Loading (от 0 до 3)
                    {
                        combo_lavs_loading.SelectedIndex = (next_i == 3) ? 1 : 0;
                        i += 1;
                    }
                    else if (s[i] == 'v' && next_ch == 'c' && nnext_i >= 0) //vc - VC1Fix (от 0 до 2)
                    {
                        combo_lavs_vc1.SelectedIndex = Math.Min(nnext_i, combo_lavs_vc1.Items.Count - 1);
                        i += 2;
                    }
                    else if (s[i] == 's')
                    {
                        if (next_ch == 'm' && nnext_i >= 0) //sm - SMode (от 0 до 3)
                        {
                            combo_lavs_subsm.SelectedIndex = Math.Min(nnext_i, combo_lavs_subsm.Items.Count - 1);
                            i += 2;
                        }
                        else if (next_ch == 'l' && nnext_ch == '[') //sl - SLanguage (sl[...])
                        {
                            //Скобка открывается - всё идущее после неё будем сохранять в language
                            l_args = true;
                            lavs_language = "";
                            i += 2;
                        }
                        else if ((next_ch == 'a' || next_ch == 'A') && nnext_ch == '[') //sa - SAdvanced (sa[...])
                        {
                            //Скобка открывается - всё идущее после неё будем сохранять в advanced
                            a_args = true;
                            lavs_advanced = "";
                            i += 2;
                        }
                    }
                }
                else
                {
                    if (s[i] == ']')
                    {
                        //Скобка закрывается - переключаемся обратно на парсинг
                        l_args = a_args = false;
                    }
                    else if (l_args && s[i] != '[')
                    {
                        //Language args
                        lavs_language += s[i];
                    }
                    else if (a_args && s[i] != '[')
                    {
                        //Advanced args
                        lavs_advanced += s[i];
                    }
                }
            }

            if (lavs_language.Length > 0 && !l_args)
                textbox_lavs_subsl.Text = lavs_language;
            else
                lavs_language = "";

            if (lavs_advanced.Length > 0 && !a_args)
                textbox_lavs_subsa.Text = lavs_advanced;
            else
                lavs_advanced = "";

            tab_lav_splitter.Tag = true;
        }

        private void LoadLAVVSettings()
        {
            label_lavd_loading.Content = Languages.Translate("Loading mode") + ":";
            combo_lavd_loading.Items.Add(new ComboBoxItem() { Content = "System", ToolTip = Languages.Translate("Load installed version with its settings, every other options on this tab will be ignored!") });
            combo_lavd_loading.Items.Add(new ComboBoxItem() { Content = "Portable", ToolTip = Languages.Translate("Load portable version, that not depend on what you have installed and how it tuned.") });
            combo_lavd_loading.Tag = _def + "Portable";
            combo_lavd_loading.SelectedIndex = 1;

            label_lavd_threads.Content = Languages.Translate("Decoding threads") + ":";
            combo_lavd_threads.Items.Add("Auto");
            for (int i = 1; i < 21; i++)
                combo_lavd_threads.Items.Add(i);
            combo_lavd_threads.Tag = _def + "Auto";
            combo_lavd_threads.SelectedIndex = 0;

            label_lavd_range.Content = Languages.Translate("YUV->RGB range") + ":";
            combo_lavd_range.Items.Add(new ComboBoxItem() { Content = "Auto", ToolTip = Languages.Translate("Same as input") });
            combo_lavd_range.Items.Add(new ComboBoxItem() { Content = "Limited", ToolTip = Languages.Translate("Limited range (TV, 16-235)") });
            combo_lavd_range.Items.Add(new ComboBoxItem() { Content = "Full", ToolTip = Languages.Translate("Full range (PC, 0-255)") });
            combo_lavd_range.Tag = _def + "Auto";
            combo_lavd_range.SelectedIndex = 0;

            label_lavd_dither.Content = Languages.Translate("Dithering mode") + ":";
            combo_lavd_dither.Items.Add(new ComboBoxItem() { Content = "Ordered", ToolTip = Languages.Translate("Ordered (static) pattern - sometimes can be visible") });
            combo_lavd_dither.Items.Add(new ComboBoxItem() { Content = "Random", ToolTip = Languages.Translate("Random pattern - invisible, but increases the noise floor slightly") });
            combo_lavd_dither.Tag = _def + "Random";
            combo_lavd_dither.SelectedIndex = 1;

            label_lavd_vc1.Content = Languages.Translate("Use WMV9 DMO for VC-1") + ":";
            combo_lavd_vc1.Items.Add(Languages.Translate("No"));
            combo_lavd_vc1.Items.Add(Languages.Translate("Yes"));
            combo_lavd_vc1.Tag = _def + Languages.Translate("Yes");
            combo_lavd_vc1.SelectedIndex = 1;

            label_lavd_hw.Content = Languages.Translate("Hardware decoding") + ":";
            combo_lavd_hw.Items.Add("Disabled");
            combo_lavd_hw.Items.Add("CUDA");
            combo_lavd_hw.Items.Add("QuickSync");
            combo_lavd_hw.Items.Add("DXVA2");
            combo_lavd_hw.Tag = _def + "Disabled";
            combo_lavd_hw.SelectedIndex = 0;

            string txt = Languages.Translate("Enable hardware decoding for this codec") + "\r\n\r\n";
            check_dss2_hw_h264.ToolTip = txt + _def + on;
            check_dss2_hw_vc1.ToolTip = txt + _def + on;
            check_dss2_hw_mpeg2.ToolTip = txt + _def + on;
            check_dss2_hw_mpeg4.ToolTip = txt + _def + off;
            check_dss2_hw_hevc.ToolTip = txt + _def + off;

            check_dss2_hw_h264.IsChecked = true;
            check_dss2_hw_vc1.IsChecked = true;
            check_dss2_hw_mpeg2.IsChecked = true;
            check_dss2_hw_mpeg4.IsChecked = false;
            check_dss2_hw_hevc.IsChecked = false;

            txt = Languages.Translate("Enable hardware decoding for this resolution");
            check_dss2_hw_sd.ToolTip = txt + " ( <= 1024x576)\r\n\r\n" + _def + on;
            check_dss2_hw_hd.ToolTip = txt + " ( <= 1980x1200)\r\n\r\n" + _def + on;
            check_dss2_hw_uhd.ToolTip = txt + " ( > 1980x1200)\r\n\r\n" + _def + off;

            check_dss2_hw_sd.IsChecked = true;
            check_dss2_hw_hd.IsChecked = true;
            check_dss2_hw_uhd.IsChecked = false;

            //"l3 t0 r0 d1 dm0 fo0 sd0 vc1 hm0 hc7 hr3 hd0 hq0 ti0"
            string s = Settings.DSS2_LAVV_Settings.ToLower();

            for (int i = 0; i < s.Length; i++)
            {
                char next_ch = (i + 1 < s.Length) ? s[i + 1] : '\0';
                char nnext_ch = (i + 2 < s.Length) ? s[i + 2] : '\0';

                //48 = 0, 49 = 1, .. 57 = 9
                int next_i = (next_ch >= 48 && next_ch <= 57) ? next_ch - 48 : -1;
                int nnext_i = (nnext_ch >= 48 && nnext_ch <= 57) ? nnext_ch - 48 : -1;

                if (s[i] == 'l' && next_i >= 0) //l - Loading (от 0 до 3)
                {
                    combo_lavd_loading.SelectedIndex = (next_i == 3) ? 1 : 0;
                    i += 1;
                }
                else if (s[i] == 't' && next_i >= 0) //t - Threads (от 0 до xx)
                {
                    int val = next_i;
                    if (nnext_i >= 0)
                    {
                        val *= 10;
                        val += nnext_i;
                        i += 1;
                    }
                    combo_lavd_threads.SelectedIndex = Math.Min(val, combo_lavd_threads.Items.Count - 1);
                    i += 1;
                }
                else if (s[i] == 'r' && next_i >= 0) //r - Range (от 0 до 2)
                {
                    combo_lavd_range.SelectedIndex = Math.Min(next_i, combo_lavd_range.Items.Count - 1);
                    i += 1;
                }
                else if (s[i] == 'd' && next_i >= 0) //d - Dither (от 0 до 1)
                {
                    combo_lavd_dither.SelectedIndex = Math.Min(next_i, combo_lavd_dither.Items.Count - 1);
                    i += 1;
                }
                else if (s[i] == 'v' && next_ch == 'c' && nnext_i >= 0) //vc - WMV9 DMO (0 = false, 1+ = true)
                {
                    combo_lavd_vc1.SelectedIndex = (nnext_i > 0) ? 1 : 0;
                    i += 2;
                }
                else if (((s[i] == 'd' && next_ch == 'm') || (s[i] == 'f' && next_ch == 'o') || (s[i] == 's' && next_ch == 'd')) && nnext_i >= 0)
                {
                    //dm - DeintMode (от 0 до 3), fo - FieldOrder (от 0 до 2), sd - SWDeint (от 0 до 2)
                    i += 2;
                }
                else if (s[i] == 'h' && nnext_i >= 0)
                {
                    if (next_ch == 'm') //hm - HW Mode (от 0 до 3)
                    {
                        combo_lavd_hw.SelectedIndex = Math.Min(nnext_i, combo_lavd_hw.Items.Count - 1);
                        i += 2;
                    }
                    else if (next_ch == 'c') //hc - HW Codecs (от 0 до 31)
                    {
                        int val = nnext_i;
                        if (i + 3 < s.Length && s[i + 3] >= 48 && s[i + 3] <= 57)
                        {
                            val *= 10;
                            val += s[i + 3] - 48;
                            if (val > 31) val = 31;
                            i += 1;
                        }
                        check_dss2_hw_h264.IsChecked = ((val & 1) > 0);
                        check_dss2_hw_vc1.IsChecked = ((val & 2) > 0);
                        check_dss2_hw_mpeg2.IsChecked = ((val & 4) > 0);
                        check_dss2_hw_mpeg4.IsChecked = ((val & 8) > 0);
                        check_dss2_hw_hevc.IsChecked = ((val & 16) > 0);
                        i += 2;
                    }
                    else if (next_ch == 'r') //hr - HW Resolutions (от 0 до 7)
                    {
                        int val = Math.Min(nnext_i, 7);
                        check_dss2_hw_sd.IsChecked = ((val & 1) > 0);
                        check_dss2_hw_hd.IsChecked = ((val & 2) > 0);
                        check_dss2_hw_uhd.IsChecked = ((val & 4) > 0);
                        i += 2;
                    }
                    else if (next_ch == 'd' || next_ch == 'q') //hd - HW Deint (от 0 до 2), hq - HW Deint HQ (0 = false, 1+ = true)
                    {
                        i += 2;
                    }
                }
            }

            tab_lav_decoder.Tag = true;
        }

        private void combo_lav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            if (combo.SelectedItem != null)
            {
                if (combo.IsDropDownOpen || combo.IsSelectionBoxHighlighted)
                {
                    if (tab_lav_splitter.IsSelected) StoreLAVSSettings();
                    if (tab_lav_decoder.IsSelected) StoreLAVVSettings();
                }

                combo.ToolTip = null;
                ComboBoxItem item = combo.SelectedItem as ComboBoxItem;
                if (item != null) combo.ToolTip = item.ToolTip;
                if (combo.Tag != null) combo.ToolTip += ((combo.ToolTip != null) ? "\r\n\r\n" : "") + combo.Tag;
            }
        }

        private void textbox_lavs_sl_LostFocus(object sender, RoutedEventArgs e)
        {
            if (textbox_lavs_subsl.Text != lavs_language)
            {
                lavs_language = textbox_lavs_subsl.Text;
                StoreLAVSSettings();
            }
        }

        private void textbox_lavs_sa_LostFocus(object sender, RoutedEventArgs e)
        {
            if (textbox_lavs_subsa.Text != lavs_advanced)
            {
                lavs_advanced = textbox_lavs_subsa.Text;
                StoreLAVSSettings();
            }
        }

        private void check_dss2_hwc_Click(object sender, RoutedEventArgs e)
        {
            StoreLAVVSettings();
        }

        private void StoreLAVSSettings()
        {
            string val = (combo_lavs_loading.SelectedIndex == 1) ? "L3" : "L0";
            if (combo_lavs_vc1.SelectedIndex != 2) val += "vc" + combo_lavs_vc1.SelectedIndex;
            if (combo_lavs_subsm.SelectedIndex != 2) val += "sm" + combo_lavs_subsm.SelectedIndex;
            if (textbox_lavs_subsl.Text.Length > 0) val += "sl[" + textbox_lavs_subsl.Text + "]";
            if (textbox_lavs_subsa.Text.Length > 0) val += "sa[" + textbox_lavs_subsa.Text + "]";
            Settings.DSS2_LAVS_Settings = val;
        }

        private void StoreLAVVSettings()
        {
            int cod = 0, res = 0;
            string val = (combo_lavd_loading.SelectedIndex == 1) ? "L3" : "L0";
            if (combo_lavd_threads.SelectedIndex > 0) val += "t" + combo_lavd_threads.SelectedIndex;
            if (combo_lavd_range.SelectedIndex > 0) val += "r" + combo_lavd_range.SelectedIndex;
            if (combo_lavd_dither.SelectedIndex != 1) val += "d" + combo_lavd_dither.SelectedIndex;
            if (combo_lavd_vc1.SelectedIndex != 1) val += "vc" + combo_lavd_vc1.SelectedIndex;
            if (combo_lavd_hw.SelectedIndex > 0) val += "hm" + combo_lavd_hw.SelectedIndex;
            if (check_dss2_hw_h264.IsChecked.Value) cod += 1;
            if (check_dss2_hw_vc1.IsChecked.Value) cod += 2;
            if (check_dss2_hw_mpeg2.IsChecked.Value) cod += 4;
            if (check_dss2_hw_mpeg4.IsChecked.Value) cod += 8;
            if (check_dss2_hw_hevc.IsChecked.Value) cod += 16;
            if (check_dss2_hw_sd.IsChecked.Value) res += 1;
            if (check_dss2_hw_hd.IsChecked.Value) res += 2;
            if (check_dss2_hw_uhd.IsChecked.Value) res += 4;
            if (cod != 7) val += "hc" + cod;
            if (res != 3) val += "hr" + res;
            Settings.DSS2_LAVV_Settings = val;
        }

        private void check_lsmash_force_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.LSMASH_AssumeFPS = check_lsmash_force_fps.IsChecked.Value;
        }

        private void check_lsmash_audio_Click(object sender, RoutedEventArgs e)
        {
            Settings.LSMASH_Enable_Audio = check_lsmash_audio.IsChecked.Value;
        }

        private void combo_lsmash_threads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_lsmash_threads.IsDropDownOpen || combo_lsmash_threads.IsSelectionBoxHighlighted) && combo_lsmash_threads.SelectedIndex != -1)
            {
                Settings.LSMASH_Threads = combo_lsmash_threads.SelectedIndex;
            }
        }
    }
}