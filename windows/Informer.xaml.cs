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
                string ext;
                if (m.infilepath_source != null)
                    ext = Path.GetExtension(m.infilepath_source).ToLower();
                else
                    ext = Path.GetExtension(m.infilepath).ToLower();

                if (ext != ".d2v" && ext != ".avs" && ext!= ".dga") //AVC
                {
                    //забиваем максимум параметров файла
                    MediaInfoWrapper media = new MediaInfoWrapper();
                    media.Open(m.infilepath);

                    m.invcodec = media.VCodecString;
                    m.invcodecshort = media.VCodecShort;
                    m.invbitrate = media.VideoBitrate;
                    m.inaspect = media.Aspect;
                    m.pixelaspect = media.PixelAspect;
                    m.inaspectstring = media.AspectString;
                    m.invideostream_mkvid = media.VideoID();
                    m.intextstreams = media.CountTextStreams;
                    m.inframerate = media.FrameRate;
                    m.inresw = media.Width;
                    m.inresh = media.Height;
                    m.induration = TimeSpan.FromMilliseconds(media.Milliseconds);
                    m.outduration = m.induration;
                    m.interlace = media.Interlace;
                    m.inframes = media.Frames;
                    m.standart = media.Standart;

                    //Возвращаем 29фпс для мпег2 видео с пуллдауном, т.к. MediaInfo выдает для него 23фпс, а MPEG2Source из-за пуллдауна декодирует с 29-ю..
                    if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && media.ScanOrder.Contains("Pulldown") && m.inframerate == "23.976" && !Settings.DGForceFilm)
                    { 
                        m.inframerate = "29.970";
                        //m.interlace = SourceType.FILM;
                    }

                    if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source && !media.ScanOrder.Contains("Pulldown") && m.inframerate != "23.976" && Settings.DGForceFilm)
                    {
                        ShowMessage(Languages.Translate("This video was indexing with turned on option ForceFilm, but for this video it was not needed. If you forgot to turn it off,") + Environment.NewLine + Languages.Translate("go to menu Video->Decoding->MPEGfiles and turn it off, then delete indexing folder and try again."), Languages.Translate("Error"), Message.MessageStyle.Ok);
                    }

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

                if (ext == ".d2v")
                {
                    m.inaspect = 1.3333;
                }
                
                
                
                //AVC
                if(ext == ".dga")
                {    
                    //все параметры будут браться из log-файла от DGAVCDec
                    string logg;
                    string path = Path.GetDirectoryName(m.infilepath).ToLower(); //определяем путь к log-файлу
                    string name = Path.GetFileNameWithoutExtension(m.infilepath).ToLower(); //определяем имя файла без расширения
                    if (!File.Exists(path + "\\" + name + ".log"))//проверяем, на месте ли сам файл, и если его нет, то Ошибка!
                    {
                        ShowMessage(Languages.Translate("Can`t find DGAVCIndex log-file:") + " " + path + "\\" + name + ".log" + Environment.NewLine + Environment.NewLine +
                        Languages.Translate("AR will be set as 16/9, you can change it manualy later."), Languages.Translate("Error"), Message.MessageStyle.Ok);
                        m.invcodecshort = "h264";
                        m.inaspect = 1.7777;
                    }
                    else
                    {
                        //а если есть, то
                        using (StreamReader sr = new StreamReader(path + "\\" + name + ".log", System.Text.Encoding.Default)) //читаем log-файл
                            logg = sr.ReadToEnd();
                        //делим на строки
                        string[] separator = new string[] { Environment.NewLine };
                        string[] lines = logg.Split(separator, StringSplitOptions.None);
                        string result1 = "1280"; string result2 = "720"; string result3 = "1"; string result4 = "1"; //дефолтные значения

                        Regex r1 = new Regex(@"Frame.Size:.(\d+)x(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Match mat1;
                        foreach (string line in lines)
                        {

                            mat1 = r1.Match(line);
                            if (mat1.Success)
                            {
                                result1 = mat1.Groups[1].Value;
                                result2 = mat1.Groups[2].Value;
                            }                        
                        }
                        m.inresw = Convert.ToInt32(result1);
                        m.inresh = Convert.ToInt32(result2);

                        //ShowMessage(Convert.ToString(result1) + Convert.ToString(result2), Languages.Translate("Error"), Message.MessageStyle.Ok); 

                        Regex r2 = new Regex(@"SAR:.(\d+):(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                        Match mat2;
                        foreach (string line in lines)
                        {

                            mat2 = r2.Match(line);
                            if (mat2.Success)
                            {
                                result3 = mat2.Groups[1].Value;
                                result4 = mat2.Groups[2].Value;
                            }
                        }
                        double sar1 = Convert.ToInt32(result3);
                        double sar2 = Convert.ToInt32(result4);

                        //ShowMessage(Convert.ToString(result3) + Convert.ToString(result4), Languages.Translate("Error"), Message.MessageStyle.Ok); 

                        //вычисляем аспект..
                        m.inaspect = (sar1 / sar2) * ((double)m.inresw / (double)m.inresh);
                        //можно еще определить тут фпс, но всё-равно это будет сделано позже через ависинт-скрипт (class Caching).
                    }
                    m.invcodecshort = "h264";
                   // m.invbitrate = 9000;
                   // sizeb += new FileInfo(file).Length;
                   // m.infilesize = Calculate.ConvertDoubleToPointString((double)sizeb / 1049511, 1) + " mb"; 
                }//AVC
                

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
                if (m.inaudiostreams.Count < ff.AudioStreamCount() && m.indexfile == null ||
                    m.inaudiostreams.Count < ff.AudioStreamCount() && ext == ".avs")
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