using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Timers;
using System.Text.RegularExpressions;
using System.Collections;

namespace XviD4PSP
{
	public partial class Informer
	{
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private FFInfo ff = null;
        private int num_closes = 0;
        public Massive m;

        public Informer(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Getting media info") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                string ext = Path.GetExtension((m.infilepath_source != null) ? m.infilepath_source : m.infilepath).ToLower();

                if (ext != ".avs" && ext != ".grf" && ext != ".d2v" && ext != ".dga" && ext != ".dgi")
                {
                    //забиваем максимум параметров файла
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(m.infilepath);

                    //Выходим при отмене
                    if (m == null || worker.CancellationPending) return;

                    m.invcodec = media.VCodecString;
                    m.invcodecshort = media.VCodecShort;
                    m.invbitrate = media.VideoBitrate;
                    m.inresw = media.Width;
                    m.inresh = media.Height;
                    m.inaspect = media.Aspect;
                    //m.pixelaspect = (m.inresw != 0 && m.inresh != 0) ? (m.inaspect / ((double)m.inresw / (double)m.inresh)) : 1.0;
                    m.pixelaspect = media.PixelAspect;
                    m.invideostream_mi_id = media.VTrackID();
                    m.invideostream_mi_order = media.VTrackOrder();
                    m.intextstreams = media.CountTextStreams;
                    m.inframerate = media.FrameRate;
                    m.induration = TimeSpan.FromMilliseconds(media.Milliseconds);
                    m.outduration = m.induration;
                    m.interlace = media.Interlace;
                    m.interlace_raw = media.ScanType;
                    m.fieldOrder_raw = media.ScanOrder;
                    m.inframes = media.Frames;
                    m.standart = media.Standart;

                    //Возвращаем 29фпс для видео с пуллдауном, т.к. MediaInfo выдает для него 23фпс, а MPEG2Source\DGSource
                    //без ForceFilm из-за пуллдауна будет декодировать с 29-ю. Продолжительность пересчитывается в Caching.
                    if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source || m.vdecoder == AviSynthScripting.Decoders.DGSource)
                    {
                        if (media.ScanOrder.Contains("Pulldown") && m.inframerate == "23.976" && !m.IsForcedFilm)
                        {
                            m.inframerate = "29.970";
                            m.interlace = SourceType.FILM;
                        }
                        else if (m.IsForcedFilm)
                        {
                            m.inframerate = "23.976";
                            m.interlace = SourceType.UNKNOWN;
                        }
                    }

                    //забиваем аудио потоки
                    if (ext == ".pmp" && Settings.EnableAudio)
                    {
                        AudioStream stream = new AudioStream();
                        stream.codec = "AAC";
                        stream.codecshort = "AAC";
                        stream.samplerate = "44100";
                        stream.bits = 16;
                        stream.channels = 2;
                        stream.language = "English";
                        m.inaudiostreams.Add(stream.Clone());
                        m.inaudiostream = 0;
                    }
                    else if (ext == ".dpg")
                    {
                        dpgmuxer dpg = new dpgmuxer();
                        dpgmuxer.DPGHeader header = dpg.ReadHeader(m.infilepath_source);
                        m.inframes = header.frames;
                        m.inframerate = Calculate.ConvertDoubleToPointString((double)header.fps);
                        m.induration = TimeSpan.FromSeconds((double)header.frames / (double)header.fps);
                        m.outduration = m.induration;

                        if (m.inaudiostreams.Count > 0 && Settings.EnableAudio)
                        {
                            AudioStream stream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                            //забиваем в список все найденные треки
                            MediaInfoWrapper med = new MediaInfoWrapper();
                            stream = med.GetAudioInfoFromAFile(stream.audiopath);
                            stream.samplerate = header.samplerate.ToString();
                            m.inaudiostreams[m.inaudiostream] = stream;
                        }
                    }
                    else if (ext == ".cda")
                    {
                        AudioStream stream = new AudioStream();
                        stream.codec = "CDDA";
                        stream.codecshort = "CDDA";
                        //stream.bitrate = media.AudioBitrate(snum);
                        //stream.mkvid = media.AudioID(snum);
                        //stream.samplerate = media.Samplerate(snum);
                        //stream.bits = media.Bits(snum);
                        //stream.channels = media.Channels(snum);
                        stream.language = "English";

                        m.isvideo = false;
                        m.inframerate = "25.000";
                        m.outframerate = m.inframerate;
                        //m.inframes = (int)(m.induration.TotalSeconds * 25);
                        //m.outframes = m.inframes;
                        m.vdecoder = AviSynthScripting.Decoders.BlankClip;
                        stream.audiopath = m.infilepath;
                        stream = Format.GetValidADecoder(stream);

                        m.inaudiostreams.Add(stream.Clone());
                    }
                    else if (m.indexfile != null && File.Exists(m.indexfile) && Settings.EnableAudio)
                    {
                        int n = 0;
                        ArrayList atracks = Indexing.GetTracks(m.indexfile);
                        foreach (string apath in atracks)
                        {
                            //определяем аудио потоки
                            AudioStream stream = new AudioStream();

                            //забиваем в список все найденные треки
                            MediaInfoWrapper med = new MediaInfoWrapper();
                            stream = med.GetAudioInfoFromAFile(apath);
                            stream.audiopath = apath;
                            stream.delay = Calculate.GetDelay(apath);
                            stream = Format.GetValidADecoder(stream);

                            stream.mi_id = media.ATrackID(n);
                            stream.mi_order = media.ATrackOrder(n);

                            m.inaudiostreams.Add(stream.Clone());
                            n++;
                        }
                        m.inaudiostream = 0;
                    }
                    else
                    {
                        if (Settings.EnableAudio || media.CountVideoStreams == 0)
                        {
                            for (int snum = 0; snum < media.CountAudioStreams; snum++)
                            {
                                AudioStream stream = new AudioStream();
                                stream.codec = media.ACodecString(snum);
                                stream.codecshort = media.ACodecShort(snum);
                                stream.bitrate = media.AudioBitrate(snum);
                                stream.mi_id = media.ATrackID(snum);
                                stream.mi_order = media.ATrackOrder(snum);
                                stream.samplerate = media.Samplerate(snum);
                                stream.bits = media.Bits(snum);
                                stream.channels = media.Channels(snum);
                                if (m.indexfile == null)
                                    stream.delay = media.Delay(snum);
                                stream.language = media.AudioLanguage(snum);

                                //вероятно звуковой файл
                                if (media.CountVideoStreams == 0)
                                {
                                    m.isvideo = false;
                                    m.inframerate = "25.000";
                                    m.outframerate = m.inframerate;
                                    m.inframes = (int)(m.induration.TotalSeconds * 25);
                                    m.outframes = m.inframes;
                                    m.vdecoder = AviSynthScripting.Decoders.BlankClip;
                                    stream.audiopath = m.infilepath;
                                    stream = Format.GetValidADecoder(stream);
                                }

                                m.inaudiostreams.Add(stream.Clone());
                            }
                            //делаем первый трек активным
                            m.inaudiostream = 0;
                        }

                        //довбиваем duration и frames для join заданий
                        if (m.infileslist.Length > 1)
                        {
                            TimeSpan ts = TimeSpan.Zero;
                            foreach (string file in m.infileslist)
                            {
                                MediaInfoWrapper med = new MediaInfoWrapper();
                                med.Open(file);
                                ts += med.Duration;
                                med.Close();
                            }
                            m.induration = ts;
                            m.outduration = m.induration;
                            m.inframes = (int)(m.induration.TotalSeconds * Calculate.ConvertStringToDouble(m.inframerate));
                        }
                    }

                    //довбиваем параметры из IFO
                    string ifo = Calculate.GetIFO(m.infilepath);
                    if (File.Exists(ifo))
                    {
                        //через MediaInfo
                        media.Open(ifo);
                        int n = 0;
                        foreach (object o in m.inaudiostreams)
                        {
                            AudioStream s = (AudioStream)o;
                            s.language = media.AudioLanguage(n);
                            n++;
                        }

                        //через VStrip
                        VStripWrapper vs = new VStripWrapper();
                        vs.Open(ifo);
                        m.induration = vs.Duration();
                        m.outduration = m.induration;
                        m.inframes = (int)(m.induration.TotalSeconds * Calculate.ConvertStringToDouble(m.inframerate));
                        vs.Close();
                    }

                    //закрываем MediaInfo
                    media.Close();
                }

                //довбиваем параметры с помощью FFmpeg
                ff = new FFInfo();
                ff.Open(m.infilepath);

                //Выходим при отмене
                if (m == null || worker.CancellationPending) return;

                m.invideostream_ff_order = ff.FirstVideoStreamID();
                if (m.invideostream_mi_order < 0) m.invideostream_mi_order = m.invideostream_ff_order;

                //null - MediaInfo для этого файла не запускалась (avs, grf..)
                //"" - MediaInfo запускалась, но нужной инфы получить не удалось
                if (m.inframerate == "")
                {
                    m.inframerate = ff.StreamFramerate(m.invideostream_ff_order);
                    if (m.inframerate != "")
                    {
                        m.induration = ff.Duration();
                        m.outduration = m.induration;
                        m.inframes = (int)(m.induration.TotalSeconds * Calculate.ConvertStringToDouble(m.inframerate));
                    }
                }

                if (ext == ".avs")
                {
                    //m.invideostream_ffid = 0;
                    m.invcodec = ff.StreamCodec(m.invideostream_ff_order);
                    m.invcodecshort = ff.StreamCodecShort(m.invideostream_ff_order);
                    m.invbitrate = ff.VideoBitrate(m.invideostream_ff_order);
                    m.inresw = ff.StreamW(m.invideostream_ff_order);
                    m.inresh = ff.StreamH(m.invideostream_ff_order);
                    m.inaspect = (double)m.inresw / (double)m.inresh;
                }
                else if (ext == ".grf")
                {
                    string infile = Path.GetFileNameWithoutExtension(m.infilepath).ToLower();
                    if (infile.StartsWith("audio"))
                    {
                        //Это аудио-граф
                        m.isvideo = false;
                        m.inframerate = m.outframerate = "25.000";
                        m.vdecoder = AviSynthScripting.Decoders.BlankClip;

                        AudioStream stream = new AudioStream();
                        stream.audiopath = m.infilepath;
                        stream.audiofiles = new string[] { stream.audiopath };
                        stream.codec = stream.codecshort = "PCM";
                        stream.language = "Unknown";
                        stream = Format.GetValidADecoder(stream);
                        m.inaudiostreams.Add(stream.Clone());
                    }
                    else
                    {
                        //Это видео-граф
                        m.invcodec = "RAWVIDEO";
                        m.invcodecshort = "RAWVIDEO";

                        //Если DirectShowSource не сможет определить fps, то требуемое значение можно будет указать в имени файла..
                        Regex r = new Regex(@"(fps\s?=\s?([\d\.\,]*))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Match mat = r.Match(infile);
                        if (mat.Success)
                        {
                            double fps = Calculate.ConvertStringToDouble(mat.Groups[2].Value);
                            if (fps > 0) m.inframerate = Calculate.ConvertDoubleToPointString(fps);

                            //"Очищаем" имя файла
                            infile = infile.Replace(mat.Groups[1].Value, "").Trim(new char[] { ' ', '_', '-', '(', ')', '.', ',' });
                        }

                        //Ищем звук к нему
                        if (Settings.EnableAudio)
                        {
                            //Почему на шаблон "audio*.grf" находятся и файлы типа "Копия audio file.grf"?!
                            string[] afiles = Directory.GetFiles(Path.GetDirectoryName(m.infilepath), "audio*.grf");
                            foreach (string afile in afiles)
                            {
                                string aname = Path.GetFileNameWithoutExtension(afile).ToLower();
                                if (aname.StartsWith("audio") && aname.Contains(infile))
                                {
                                    AudioStream stream = new AudioStream();
                                    stream.audiopath = afile;
                                    stream.audiofiles = new string[] { stream.audiopath };
                                    stream.codec = stream.codecshort = "PCM";
                                    stream.language = "Unknown";
                                    stream = Format.GetValidADecoder(stream);
                                    m.inaudiostreams.Add(stream.Clone());
                                    break; //Только один трек
                                }
                            }
                        }
                    }
                }
                else if (ext == ".d2v")
                {
                    //Читаем d2v-файл
                    int n = 0;
                    string line = "";
                    Match mat1;
                    Match mat2;
                    Regex r1 = new Regex(@"Picture_Size=(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    Regex r2 = new Regex(@"Aspect_Ratio=(\d+\.*\d*):(\d+\.*\d*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    int result1 = 720; int result2 = 576; double result3 = 4; double result4 = 3; //Значения по-умолчанию
                    using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                        while (!sr.EndOfStream && n < 15) //Ограничиваемся первыми 15-ю строчками
                        {
                            line = sr.ReadLine();
                            mat1 = r1.Match(line);
                            mat2 = r2.Match(line);
                            if (mat1.Success)
                            {
                                result1 = Convert.ToInt32(mat1.Groups[1].Value);
                                result2 = Convert.ToInt32(mat1.Groups[2].Value);
                            }
                            if (mat2.Success)
                            {
                                result3 = Calculate.ConvertStringToDouble(mat2.Groups[1].Value);
                                result4 = Calculate.ConvertStringToDouble(mat2.Groups[2].Value);
                            }
                            n += 1;
                        }
                    m.inresw = result1;
                    m.inresh = result2;
                    m.inaspect = result3 / result4;
                    m.pixelaspect = m.inaspect / ((double)m.inresw / (double)m.inresh);
                    m.invcodecshort = "MPEG2";
                }
                else if (ext == ".dga")
                {
                    //Смотрим, на месте ли log-файл
                    string log_file = Calculate.RemoveExtention(m.infilepath) + "log";
                    if (File.Exists(log_file))
                    {
                        //Читаем log-файл
                        string text_log = "";
                        Match mat;
                        Regex r1 = new Regex(@"Frame.Size:.(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Regex r2 = new Regex(@"SAR:.(\d+):(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        int result1 = 1280; int result2 = 720; double result3 = 1; double result4 = 1; //Значения по-умолчанию
                        using (StreamReader sr = new StreamReader(log_file, System.Text.Encoding.Default))
                            text_log = sr.ReadToEnd();

                        mat = r1.Match(text_log);
                        if (mat.Success)
                        {
                            result1 = Convert.ToInt32(mat.Groups[1].Value);
                            result2 = Convert.ToInt32(mat.Groups[2].Value);
                        }
                        mat = r2.Match(text_log);
                        if (mat.Success)
                        {
                            result3 = Convert.ToDouble(mat.Groups[1].Value);
                            result4 = Convert.ToDouble(mat.Groups[2].Value);
                        }

                        m.inresw = result1;
                        m.inresh = result2;
                        m.inaspect = (result3 / result4) * ((double)m.inresw / (double)m.inresh);
                        m.pixelaspect = m.inaspect / ((double)m.inresw / (double)m.inresh);
                        //можно еще определить тут фпс, но всё-равно это будет сделано позже через ависинт-скрипт (class Caching). 
                        m.invcodecshort = "h264";
                    }
                    else
                    {
                        //Если нет log-файла, ищем исходный файл и берем инфу из него
                        Match mat;
                        string source_file = "";
                        Regex r = new Regex(@"(\D:\\.*\..*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                            for (int i = 0; i < 3 && !sr.EndOfStream; i += 1)
                            {
                                source_file = sr.ReadLine();
                            }
                        mat = r.Match(source_file);
                        if (mat.Success && File.Exists(source_file = mat.Groups[1].Value))
                        {
                            MediaInfoWrapper media = new MediaInfoWrapper();
                            media.Open(source_file);
                            m.invcodecshort = media.VCodecShort;
                            m.inresw = media.Width;
                            m.inresh = media.Height;
                            m.inaspect = media.Aspect;
                            m.pixelaspect = media.PixelAspect;
                            media.Close();
                        }
                        else
                        {
                            throw new Exception(Languages.Translate("Can`t find DGAVCIndex log-file:") + " " + log_file +
                                "\r\n" + Languages.Translate("And can`t determine the source-file."));
                        }
                    }
                }
                else if (ext == ".dgi")
                {
                    //Путь к декодеру 
                    using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                        for (int i = 0; i < 2 && !sr.EndOfStream; i += 1)
                        {
                            m.dgdecnv_path = sr.ReadLine();
                        }
                    if (!File.Exists(m.dgdecnv_path + "DGDecodeNV.dll"))
                    {
                        throw new Exception(Languages.Translate("Can`t find file") + ": " + m.dgdecnv_path + "DGDecodeNV.dll");
                    }

                    //Смотрим, на месте ли log-файл
                    string log_file = Calculate.RemoveExtention(m.infilepath) + "log";
                    if (File.Exists(log_file))
                    {
                        //Читаем log-файл
                        Match mat;
                        string text_log = "";
                        Regex r1 = new Regex(@"Coded.Size:.(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Regex r2 = new Regex(@"SAR:.(\d+):(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Regex r3 = new Regex(@"Aspect.Ratio:.(\d+\.*\d*):(\d+\.*\d*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Regex r4 = new Regex(@"Video.Type:.(.*).", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        double result1, result2;
                        text_log = File.ReadAllText(log_file, System.Text.Encoding.Default);

                        //Разрешение (Coded Size:)
                        mat = r1.Match(text_log);
                        if (mat.Success)
                        {
                            m.inresw = Convert.ToInt32(mat.Groups[1].Value);
                            m.inresh = Convert.ToInt32(mat.Groups[2].Value);

                            //Аспект (SAR:)
                            mat = r2.Match(text_log);
                            if (mat.Success)
                            {
                                result1 = Convert.ToDouble(mat.Groups[1].Value);
                                result2 = Convert.ToDouble(mat.Groups[2].Value);

                                m.inaspect = (result1 / result2) * ((double)m.inresw / (double)m.inresh);
                                m.pixelaspect = result1 / result2;
                            }
                            else
                            {
                                //Аспект (Aspect Ratio:)
                                mat = r3.Match(text_log);
                                if (mat.Success)
                                {
                                    result1 = Calculate.ConvertStringToDouble(mat.Groups[1].Value);
                                    result2 = Calculate.ConvertStringToDouble(mat.Groups[2].Value);

                                    m.inaspect = result1 / result2;
                                    m.pixelaspect = m.inaspect / ((double)m.inresw / (double)m.inresh);
                                }
                                else
                                {
                                    m.inaspect = (double)m.inresw / (double)m.inresh;
                                    m.pixelaspect = 1.0;
                                }
                            }
                        }
                        else
                        {
                            m.inaspect = 16.0 / 9.0;
                            m.pixelaspect = 1.0;
                        }

                        //Кодек
                        mat = r4.Match(text_log);
                        if (mat.Success)
                        {
                            string codec = mat.Groups[1].Value;
                            if (codec == "AVC") codec = "h264";
                            m.invcodecshort = codec;
                        }
                    }
                    else
                    {
                        //Если нет log-файла, ищем исходный файл и берем инфу из него
                        string source_file = "";
                        Regex r = new Regex(@"(\D:\\.*\..*)\s\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        using (StreamReader sr = new StreamReader(m.indexfile, System.Text.Encoding.Default))
                            for (int i = 0; i < 4 && !sr.EndOfStream; i += 1)
                            {
                                source_file = sr.ReadLine();
                            }
                        Match mat = r.Match(source_file);
                        if (mat.Success && File.Exists(source_file = mat.Groups[1].Value))
                        {
                            MediaInfoWrapper media = new MediaInfoWrapper();
                            media.Open(source_file);
                            m.invcodecshort = media.VCodecShort;
                            m.inresw = media.Width;
                            m.inresh = media.Height;
                            m.inaspect = media.Aspect;
                            m.pixelaspect = media.PixelAspect;
                            media.Close();
                        }
                        else
                        {
                            throw new Exception(Languages.Translate("Can`t find DGIndexNV log-file:") + " " + log_file +
                               "\r\n" + Languages.Translate("And can`t determine the source-file."));
                        }
                    }
                }
                else if (m.isvideo && Settings.UseFFmpegAR)
                {
                    double par = ff.CalculatePAR(m.invideostream_ff_order);
                    if (par != 0) m.pixelaspect = par;
                    double dar = ff.CalculateDAR(m.invideostream_ff_order);
                    if (dar != 0) m.inaspect = dar;
                }

                //Аудио
                if (ff.AudioStreams().Count > 0)
                {
                    ArrayList AStreams = ff.AudioStreams();

                    //подправляем кодек, ffID, язык
                    //Это будет работать как надо, только если очерёдность наших треков
                    //совпадает с их очерёдностью в FFmpeg, иначе инфа о треках перепутается!
                    int astream = 0;
                    foreach (object o in m.inaudiostreams)
                    {
                        AudioStream s = (AudioStream)o;
                        s.ff_order = (int)AStreams[astream]; //ID трека для FFmpeg
                        if (s.mi_order < 0) s.mi_order = s.ff_order;
                        if (s.bitrate == 0) s.bitrate = ff.StreamBitrate(s.ff_order);
                        if (s.channels == 0) s.channels = ff.StreamChannels(s.ff_order);
                        if (s.samplerate == null) s.samplerate = ff.StreamSamplerate(s.ff_order);
                        if (s.language == "Unknown") s.language = ff.StreamLanguage(s.ff_order);
                        if (s.codec == "A_MS/ACM")
                        {
                            s.codec = ff.StreamCodec(s.ff_order);
                            s.codecshort = ff.StreamCodecShort(s.ff_order);
                        }

                        astream++;
                        if (astream >= AStreams.Count) break;
                    }

                    if ((m.indexfile == null && Settings.EnableAudio || ext == ".avs") && m.inaudiostreams.Count < AStreams.Count)
                    {
                        //забиваем аудио, если они ещё не забиты (если у FFmpeg треков больше, чем у нас)
                        //Все треки от FFmpeg добавляются к тем, что у нас уже есть. И если у нас уже что-то
                        //есть, то мы можем получить дубли каких-то треков. Тут тоже нужно как-то сопоставлять
                        //треки, и объединить это всё с кодом, который выше!
                        //m.inaudiostreams.Clear(); //Может просто обнулить всё что уже есть? Тогда потеряем инфу о Delay.
                        foreach (int stream_num in AStreams)
                        {
                            AudioStream stream = new AudioStream();
                            stream.codec = ff.StreamCodec(stream_num);
                            stream.codecshort = ff.StreamCodecShort(stream_num);
                            stream.bitrate = ff.StreamBitrate(stream_num);
                            stream.samplerate = ff.StreamSamplerate(stream_num);
                            stream.bits = ff.StreamBits(stream_num);
                            stream.channels = ff.StreamChannels(stream_num);
                            stream.language = ff.StreamLanguage(stream_num);
                            stream.mi_order = stream.ff_order = stream_num;
                            m.inaudiostreams.Add(stream.Clone());
                        }

                        m.inaudiostream = 0;
                    }
                }

                //Закрываем FFInfo
                CloseFFInfo();

                //подсчитываем размер
                long sizeb = 0;
                foreach (string f in m.infileslist)
                {
                    FileInfo info = new FileInfo(f);
                    sizeb += info.Length;
                }
                m.infilesize = Calculate.ConvertDoubleToPointString((double)sizeb / 1049511, 1) + " mb";
                m.infilesizeint = (int)((double)sizeb / 1049511);

                //определяем аудио декодер
                foreach (object o in m.inaudiostreams)
                {
                    AudioStream s = (AudioStream)o;
                    if (s.decoder == 0)
                        s = Format.GetValidADecoder(s);
                }
            }
            catch (Exception ex)
            {
                if (worker != null && !worker.CancellationPending && m != null && num_closes == 0)
                {
                    //Ошибка
                    e.Result = ex;
                }

                m = null;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel_closing = false;

            if (worker != null)
            {
                if (worker.IsBusy && num_closes < 5)
                {
                    //Отмена
                    cancel_closing = true;
                    worker.CancelAsync();
                    num_closes += 1;
                    m = null;
                }
                else
                {
                    worker.Dispose();
                    worker = null;
                }
            }

            //Закрываем FFInfo
            CloseFFInfo();

            //Отменяем закрытие окна
            if (cancel_closing)
            {
                label_info.Content = Languages.Translate("Aborting... Please wait...");
                e.Cancel = true;
            }
        }

        private void CloseFFInfo()
        {
            lock (locker)
            {
                if (ff != null)
                {
                    ff.Close();
                    ff = null;
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                m = null;
                ErrorException("Informer (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                m = null;
                ErrorException("Informer: " + ((Exception)e.Result).Message, ((Exception)e.Result).StackTrace);
            }

            Close();
        }

        internal delegate void ErrorExceptionDelegate(string data, string info);
        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ErrorExceptionDelegate(ErrorException), data, info);
            else
            {
                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
	}
}