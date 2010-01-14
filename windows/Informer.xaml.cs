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
        private BackgroundWorker worker = null;
        public Massive m;
        private int progress = 0;
        AviSynthReader reader;
        FFInfo ff;

        public Informer(Massive mass)
        {
            this.InitializeComponent();

            this.Owner = mass.owner;

            m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;

            Title = Languages.Translate("Getting media info") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //фоновое кодирование
            CreateBackgoundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void CreateBackgoundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (m != null)
            {
                string tmp_title = "(" + progress.ToString("##0") + "%)";
                SetStatus(tmp_title, "", progress);
                progress++;
            }
        }

        void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {

        }

        void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                string ext = Path.GetExtension((m.infilepath_source != null) ? m.infilepath_source : m.infilepath).ToLower();

                if (ext != ".d2v" && ext != ".avs" && ext!= ".dga")
                {
                    //забиваем максимум параметров файла
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(m.infilepath);

                    m.invcodec = media.VCodecString;
                    m.invcodecshort = media.VCodecShort;
                    m.invbitrate = media.VideoBitrate;
                    m.inresw = media.Width;
                    m.inresh = media.Height;
                    m.inaspect = media.Aspect;
                    //m.pixelaspect = (m.inresw != 0 && m.inresh != 0) ? (m.inaspect / ((double)m.inresw / (double)m.inresh)) : 1.0;
                    m.pixelaspect = media.PixelAspect;
                    m.invideostream_mkvid = media.VideoID();
                    m.intextstreams = media.CountTextStreams;
                    m.inframerate = media.FrameRate;
                    m.induration = TimeSpan.FromMilliseconds(media.Milliseconds);
                    m.outduration = m.induration;
                    m.interlace = media.Interlace;
                    m.inframes = media.Frames;
                    m.standart = media.Standart;
                                 
                    //Возвращаем 29фпс для мпег2 видео с пуллдауном, т.к. MediaInfo выдает для него 23фпс, а MPEG2Source из-за пуллдауна декодирует с 29-ю..
                    if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && media.ScanOrder.Contains("Pulldown") && m.inframerate == "23.976" && !Settings.DGForceFilm)
                    { 
                        m.inframerate = "29.970";
                        m.interlace = SourceType.FILM;
                    }

                    //Это сообщение больше не нужно, т.к. ForceFilm будет только при индексации видео с PullDown и 23.976фпс
                    //if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && !media.ScanOrder.Contains("Pulldown") && m.inframerate != "23.976" && Settings.DGForceFilm)
                    //{
                    //    ShowMessage(Languages.Translate("This video was indexing with the option ForceFilm on, but for this video it was not needed. If you forgot to turn it off,") + Environment.NewLine + Languages.Translate("go to menu Video->Decoding->MPEGfiles and uncheck it, then delete indexing folder and try again."), Languages.Translate("Error"), Message.MessageStyle.Ok);
                    //}

                    //забиваем аудио потоки
                    if (ext == ".pmp")
                    {
                        AudioStream stream = new AudioStream();
                        stream.codec = "AAC";
                        stream.codecshort = "AAC";
                        stream.samplerate = "44100";
                        stream.bits = 16;
                        stream.channels = 2;
                        stream.gainfile = Settings.TempPath + "\\" + m.key + "_0_gain.wav";
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

                        if (m.inaudiostreams.Count > 0)
                        {
                            AudioStream stream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                            //забиваем в список все найденные треки
                            MediaInfoWrapper med = new MediaInfoWrapper();
                            stream = med.GetAudioInfoFromAFile(stream.audiopath);
                            stream.gainfile = Settings.TempPath + "\\" + m.key + "_0_gain.wav";
                            stream.samplerate = header.samplerate.ToString();
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
                        stream.gainfile = Settings.TempPath + "\\" + m.key + "_0_gain.wav";
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
                    else if (m.indexfile != null &&
                             File.Exists(m.indexfile))
                    {
                        ArrayList atracks = Indexing.GetTracks(m.indexfile);
                        int n = 0;
                        foreach (string apath in atracks)
                        {
                            //определяем аудио потоки
                            AudioStream stream = new AudioStream();

                            //забиваем в список все найденные треки
                            MediaInfoWrapper med = new MediaInfoWrapper();
                            stream = med.GetAudioInfoFromAFile(apath);
                            stream.audiopath = apath;
                            stream.delay = Calculate.GetDelay(apath);
                            stream.gainfile = Settings.TempPath + "\\" + m.key + "_" + n + "_gain.wav";
                            stream = Format.GetValidADecoder(stream);

                            stream.mkvid = media.AudioID(n);

                            m.inaudiostreams.Add(stream);
                            n++;
                        }
                        m.inaudiostream = 0;
                    }
                    else
                    {
                        for (int snum = 0; snum < media.CountAudioStreams; snum++)
                        {
                            AudioStream stream = new AudioStream();
                            stream.codec = media.ACodecString(snum);
                            stream.codecshort = media.ACodecShort(snum);
                            stream.bitrate = media.AudioBitrate(snum);
                            stream.mkvid = media.AudioID(snum);
                            stream.samplerate = media.Samplerate(snum);
                            stream.bits = media.Bits(snum);
                            stream.channels = media.Channels(snum);
                            if (m.indexfile == null)
                                stream.delay = media.Delay(snum);
                            stream.gainfile = Settings.TempPath + "\\" + m.key + "_" + snum + "_gain.wav";
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

                //довбиваем параметры с помощью ffmpeg
                ff = new FFInfo();
                ff.Open(m.infilepath);

                if (m.inframerate == "")
                {
                    m.inframerate = ff.StreamFramerate(ff.VideoStream());
                    if (m.inframerate != "")
                    {
                        m.induration = ff.Duration();
                        m.outduration = m.induration;
                        m.inframes = (int)(m.induration.TotalSeconds * Calculate.ConvertStringToDouble(m.inframerate));
                    }
                }
                m.invideostream_ffid = ff.VideoStream();

                if (ext == ".avs")
                {
                    //m.inframerate = ff.StreamFramerate(ff.VideoStream()); 23.976 неверно округляются до 23.98 
                    //m.induration = ff.Duration();
                    //m.outduration = m.induration;
                    //m.inframes = (int)(m.induration.TotalSeconds * Calculate.ConvertStringToDouble(m.inframerate));

                    m.invideostream_ffid = ff.VideoStream();
                    m.invcodec = ff.StreamCodec(m.invideostream_ffid);
                    m.invcodecshort = ff.StreamCodecShort(m.invideostream_ffid);
                    m.invbitrate = ff.VideoBitrate(m.invideostream_ffid);
                    m.inresw = ff.StreamW(m.invideostream_ffid);
                    m.inresh = ff.StreamH(m.invideostream_ffid);
                    m.inaspect = (double)m.inresw / (double)m.inresh;
                }
                else if (ext == ".d2v")
                {
                    //Читаем d2v-файл
                    int n = 0;
                    string line = "";
                    Match mat1;
                    Match mat2;
                    Regex r1 = new Regex(@"Picture_Size=(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    Regex r2 = new Regex(@"Aspect_Ratio=(\d+):(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
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
                                result3 = Convert.ToDouble(mat2.Groups[1].Value);
                                result4 = Convert.ToDouble(mat2.Groups[2].Value);
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
                    string path = Path.GetDirectoryName(m.infilepath).ToLower();
                    string name = Path.GetFileNameWithoutExtension(m.infilepath).ToLower();
                    if (!File.Exists(path + "\\" + name + ".log"))
                    {
                        ShowMessage(Languages.Translate("Can`t find DGAVCIndex log-file:") + " " + path + "\\" + name + ".log" + Environment.NewLine + Environment.NewLine +
                        Languages.Translate("AR will be set as 16/9, you can change it manually later."), Languages.Translate("Error"), Message.MessageStyle.Ok);
                        m.inaspect = 16.0 / 9.0;
                    }
                    else
                    {
                        //Читаем log-файл
                        string line = "";
                        Match mat1;
                        Match mat2;
                        Regex r1 = new Regex(@"Frame.Size:.(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Regex r2 = new Regex(@"SAR:.(\d+):(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        int result1 = 1280; int result2 = 720; double result3 = 1; double result4 = 1; //Значения по-умолчанию
                        using (StreamReader sr = new StreamReader(path + "\\" + name + ".log", System.Text.Encoding.Default))
                            while (!sr.EndOfStream)
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
                                    result3 = Convert.ToDouble(mat2.Groups[1].Value);
                                    result4 = Convert.ToDouble(mat2.Groups[2].Value);
                                }
                            }
                        m.inresw = result1;
                        m.inresh = result2;
                        m.inaspect = (result3 / result4) * ((double)m.inresw / (double)m.inresh);
                        m.pixelaspect = m.inaspect / ((double)m.inresw / (double)m.inresh);
                        //можно еще определить тут фпс, но всё-равно это будет сделано позже через ависинт-скрипт (class Caching). 
                    }
                    m.invcodecshort = "h264";
                }
                else if (m.isvideo && Settings.UseFFmpegAR) 
                {
                    double par = ff.CalculatePAR(m.invideostream_ffid);
                    if (par != 0) m.pixelaspect = par;
                    double dar = ff.CalculateDAR(m.invideostream_ffid);
                    if (dar != 0) m.inaspect = dar;
                }

                //подправляем кодек, ffID, язык
                int astream = ff.AudioStream();
                foreach (object o in m.inaudiostreams)
                {  
                    AudioStream s = (AudioStream)o;
                    if (s.samplerate == null)
                        s.samplerate = ff.StreamSamplerate(astream);
                    if (s.channels == 0)
                        s.channels = ff.StreamChannels(astream);
                    if (s.bitrate == 0)
                        s.bitrate = ff.StreamBitrate(astream);
                    if (s.codec == "A_MS/ACM")
                    {
                        s.codec = ff.StreamCodec(astream);
                        s.codecshort = ff.StreamCodecShort(astream);
                    }
                    if (s.language == "Unknown")
                        s.language = ff.StreamLanguage(astream);
                    s.ffid = astream;
                    astream++;
                }

                //забиваем аудио, если они ещё не забиты
                if (m.indexfile == null && m.inaudiostreams.Count < ff.AudioStreamCount() ||
                    ext == ".avs" && m.inaudiostreams.Count < ff.AudioStreamCount())
                {
                    for (int snum = ff.AudioStream(); snum <= ff.AudioStreamCount(); snum++)
                    {
                        AudioStream stream = new AudioStream();
                        stream.codec = ff.StreamCodec(snum);
                        stream.codecshort = ff.StreamCodecShort(snum);
                        stream.bitrate = ff.StreamBitrate(snum);
                        stream.samplerate = ff.StreamSamplerate(snum);
                        //stream.bits = media.Bits(snum);
                        stream.channels = ff.StreamChannels(snum);
                        //if (m.indexfile == null)
                        //    stream.delay = media.Delay(snum);
                        stream.gainfile = Settings.TempPath + "\\" + m.key + "_" + snum + "_gain.wav";
                        stream.language = ff.StreamLanguage(snum);
                        stream.ffid = snum;
                        m.inaudiostreams.Add(stream.Clone());
                    }
                    m.inaudiostream = 0;
                }

                ff.Close();

                //подсчитываем размер
                long sizeb = 0;
                foreach (string f in m.infileslist)
                {
                    FileInfo info = new FileInfo(f);
                    sizeb += info.Length;
                }
                m.infilesize = Calculate.ConvertDoubleToPointString((double)sizeb / 1049511 , 1) + " mb";
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
                m = null;
                ShowMessage(ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ff != null)
                ff.Close();

            if (worker.IsBusy)
            {
                worker.CancelAsync();
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Close();
        }

        internal delegate void MessageDelegate(string data, string title, Message.MessageStyle style);
        private void ShowMessage(string data, string title, Message.MessageStyle style)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), data, title, style);
            else
            {
                Message mes = new Message(this.Owner);
                mes.ShowMessage(data, title, style);
            }
        }

        internal delegate void StatusDelegate(string title, string pr_text, double pr_c);
        private void SetStatus(string title, string pr_text, double pr_c)
        {
            if (m != null)
            {
                if (!Application.Current.Dispatcher.CheckAccess())
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new StatusDelegate(SetStatus), title, pr_text, pr_c);
                else
                {
                    //this.Title = title;
                    //this.tbxProgress.Text = pr_text;
                    this.prCurrent.Value = pr_c;
                }
            }
        }

	}
}