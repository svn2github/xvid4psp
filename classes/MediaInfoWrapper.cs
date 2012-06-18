// MediaInfoDLL - All info about media files, for DLL
// Copyright (C) 2002-2006 Jerome Martinez, Zen@MediaArea.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// MediaInfoDLL - All info about media files, for DLL
// Copyright (C) 2002-2006 Jerome Martinez, Zen@MediaArea.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Microsoft Visual C# wrapper for MediaInfo Library
// See MediaInfo.h for help
//
// To make it working, you must put MediaInfo.Dll
// in the executable folder
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

namespace XviD4PSP
{
    public enum StreamKind
    {
        General,
        Video,
        Audio,
        Text,
        Chapters,
        Image
    }

    public enum InfoKind
    {
        Name,
        Text,
        Measure,
        Options,
        NameText,
        MeasureText,
        Info,
        HowTo
    }

    public enum InfoOptions
    {
        ShowInInform,
        Support,
        ShowInSupported,
        TypeOfValue
    }

    public class MediaInfoWrapper
    {        
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)  
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_New();
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern void MediaInfo_Delete(IntPtr Handle);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern void MediaInfo_Close(IntPtr Handle);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_State_Get(IntPtr Handle);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Count_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        private IntPtr Handle;
        public MediaInfoWrapper() { Handle = MediaInfo_New(); }
        public int Open(String FileName) { return (int)MediaInfo_Open(Handle, FileName); }
        public void Close() { MediaInfo_Close(Handle); }
        ~MediaInfoWrapper() { MediaInfo_Delete(Handle); }

        public String Inform() { return Marshal.PtrToStringUni(MediaInfo_Inform(Handle, (IntPtr)0)); }
        public String Option(String Option, String Value) { return Marshal.PtrToStringUni(MediaInfo_Option(Handle, Option, Value)); }
        private String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch) { return Marshal.PtrToStringUni(MediaInfo_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch)); }
        private String Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo) { return Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo)); }
        private int Count_Get(StreamKind StreamKind, int StreamNumber) { return (int)MediaInfo_Count_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber); }
        private int State_Get() { return (int)MediaInfo_State_Get(Handle); }

        //Default values, if you know how to set default values in C#, say me
        private int Count_Get(StreamKind StreamKind) { return Count_Get(StreamKind, -1); }
        private String Get(StreamKind StreamKind, int StreamNumber, int Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text); }
        private String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo) { return Get(StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }
        private String Get(StreamKind StreamKind, int StreamNumber, String Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); }
        private String Get_Splitted(StreamKind StreamKind, int StreamNumber, String Parameter)
        {
            string value = Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name);
            if (value.Length > 4)
            {
                //Берём только первую часть из двойных значений (1280 / 1280).
                //Сами такие значения - вероятно очень редки, но было дело..
                int index = value.IndexOf(" / ");
                if (index >= 0) return value.Substring(0, index);
            }

            return value;
        }

        //public AudioStream GetAudioInfo(Massive m)
        //{
        //    //выбираем трек
        //    int n = m.inaudiostream;
        //    AudioStream stream = new AudioStream();

        //    Open(m.infilepath);

        //    stream.codec = ACodecString(n);
        //    stream.codecshort = ACodecShort(n);
        //    m.inabitrate = AudioBitrate(n);
        //    stream.id = AudioID(n);
        //    stream.delay = Delay(n);
        //    stream.samplerate = Samplerate(n);
        //    m.inbits = Bits(n);
        //    m.inachannels = Channels(n);

        //    Close();
        //    return stream;
        //}

        public AudioStream GetAudioInfoFromAFile(string filepath)
        {
            AudioStream stream = new AudioStream();
            Open(filepath);

            stream.audiopath = filepath;
            stream.audiofiles = new string[] { stream.audiopath };
            stream.codec = ACodecString(0);
            stream.codecshort = ACodecShort(0);
            stream.language = AudioLanguage(0); //"Unknown";
            stream.bitrate = AudioBitrate(0);
            stream.delay = 0;
            stream.samplerate = Samplerate(0);
            stream.channels = Channels(0);
            stream.bits = Bits(0);

            //определяем битрейт
            if (stream.bitrate == 0)
            {
                if (!File.Exists(filepath) || Duration.TotalSeconds == 0)
                    stream.bitrate = (stream.bits * Convert.ToInt32(stream.samplerate) * stream.channels) / 1000; //kbps
                else
                {
                    FileInfo info = new FileInfo(filepath);
                    stream.bitrate = (int)(((info.Length / Duration.TotalSeconds) * stream.bits) / stream.channels) / 1000; //kbps
                }
            }

            Close();
            return stream;
        }

        public int ATrackID(int track)
        {
            string x = Get(StreamKind.Audio, track, "ID");
            if (x == "C0" ||
                x == "A0" ||
                x == "C0 / 45" ||
                x == "CO / 110" ||
                x == "81 / 45" ||
                x == "81 / 1100" ||
                x == "80" ||
                x == "1100")
                return 1;
            else if (x == "81 / 1101" ||
                     x == "1101" ||
                     x == "A1")
                return 2;
            else
            {
                Int32.TryParse(x, NumberStyles.Integer, null, out track);
                return track;
            }
        }

        public int ATrackOrder(int track)
        {
            string s = Get(StreamKind.Audio, track, "StreamOrder");
            if (Int32.TryParse(s, NumberStyles.Integer, null, out track))
                return track;
            else
                return -1;
        }

        public int VTrackID()
        {
            int z = 0;
            string s = Get(StreamKind.Video, 0, "ID");
            Int32.TryParse(s, NumberStyles.Integer, null, out z);
            return z;
        }

        public int VTrackOrder()
        {
            int z = 0;
            string s = Get(StreamKind.Video, 0, "StreamOrder");
            if (Int32.TryParse(s, NumberStyles.Integer, null, out z))
                return z;
            else
                return -1;
        }

        public string VCodecString
        {
            get
            {
                string s = Get(StreamKind.Video, 0, "Codec/String");
                //string fcc = Get(StreamKind.Video, 0, "Codec/CC");
                //string a = Get(StreamKind.Video, 0, "Format");
                //string b = Get(StreamKind.Video, 0, "CodecID");
                //string c = Get(StreamKind.Video, 0, "CodecID/Hint");

                if (s == "")
                    s = Get(StreamKind.General, 0, "Codec/String");
                if (s == "MPEG-4 AVC" ||
                    s == "AVC")
                    s = "h264";
                return s;
            }
        }

        public string VCodecShort
        {
            get
            {
                string s = Get(StreamKind.Video, 0, "Codec/String");
                string fcc = Get(StreamKind.Video, 0, "Codec/CC");

                if (s == "")
                    s = Get(StreamKind.General, 0, "Codec/String");

                if (s == "Sorenson Spark")
                    s = "h263";
                else if (s == "DivX 5" || s == "DivX 3 Low")
                    s = "DivX";
                else if (s == "XVID / V_MS/VFW/FOURCC")
                    s = "XviD";
                else if (s == "MPEG-1 Video" || s == "V_MPEG - 1")
                    s = "MPEG1";
                else if (s == "MPEG-2 Video" || s == "V_MPEG-2" || s == "MPEG-PS")
                    s = "MPEG2";
                else if (s == "WMV3")
                    s = "WMV";
                else if (s == "V_MS/VFW/WVC1" || s == "V_MS/VFW/FOURCC" || s == "WVC1" || s == "VC-1")
                    s = "VC1";
                else if (s == "H.264" || s == "MPEG-4 AVC" || s == "AVC" || s == "V_MPEG4/ISO/AVC")
                    s = "h264";
                else if (s == "MPEG-4" || s == "MPEG-4 Visual")
                {
                    if (fcc == "20")
                        s = "MPEG4";
                    else if (fcc == "DX50" || fcc == "DIVX")
                        s = "DivX";
                    else if (fcc == "XVID")
                        s = "XviD";
                    else
                        s = "h264";
                }
                else if (s == "Flash Video")
                    s = "FLV";
                else if (s == "V_VP8")
                    s = "VP8";
                return s;
            }
        }

        public string ACodecString(int track)
        {
            return Get(StreamKind.Audio, track, "Codec/String");
        }

        public string ACodecShort(int track)
        {
            string s = Get(StreamKind.Audio, track, "Codec/String");
            //string fcc = Get(StreamKind.Audio, track, "Codec/CC");
            //string a = Get(StreamKind.Audio, track, "Format");
            //string b = Get(StreamKind.Audio, track, "CodecID");
            //string c = Get(StreamKind.Audio, track, "CodecID/Hint");

            if (s == "MPEG-1 Audio layer 3" ||
                s == "MPEG-1/2 L3" ||
                s == "MPEG-1L3" ||
                s == "MPEG1/2 L3" ||
                s == "MPEG-1 Audio Layer 3" ||
                s == "MPA1L3" ||
                s == "MPA2L3" ||
                s == "MPEG-2 Audio layer 3" ||
                s == "A_MP3" ||
                s == "MPEG-1 Audio")
                s = "MP3";
            else if (s == "MPEG-1 Audio layer 2")
                s = "MP2";
            else if (s == "Vorbis")
                s = "OGG";
            else if (s.Contains("AAC"))
                s = "AAC";
            else if (s == "WMA2" ||
                s == "WMA3")
                s = "WMA";
            else if (s == "A_AC3")
                s = "AC3";
            else if (s == "A_DTS" ||
                s == "DTS-HD")
                s = "DTS";
            else if (s == "A_LPCM")
                s = "LPCM";
            return s;
        }

        public int CountVideoStreams
        {
            get
            {
                return Count_Get(StreamKind.Video);
            }
        }

        public int CountAudioStreams
        {
            get
            {
                return Count_Get(StreamKind.Audio);
            }
        }

        public int CountTextStreams
        {
            get
            {
                return Count_Get(StreamKind.Text);
            }
        }

        public string AspectString
        {
            get
            {
                return Get(StreamKind.Video, 0, "DisplayAspectRatio/String").Replace("/", ":");
            }
        }

        public double Aspect
        {
            get
            {
                string s = "";
                if (Settings.MI_Original_AR)
                {
                    s = Get_Splitted(StreamKind.Video, 0, "DisplayAspectRatio_Original");     //Из потока (если доступно)
                    if (s == "") s = Get_Splitted(StreamKind.Video, 0, "DisplayAspectRatio"); //Из контейнера или общее
                }
                else
                    s = Get_Splitted(StreamKind.Video, 0, "DisplayAspectRatio");

                //Подправляем DAR
                if (s == "" || s == "1.333") return 4.0 / 3.0;
                else if (s == "1.778") return 16.0 / 9.0;
                return Calculate.ConvertStringToDouble(s);
            }
        }

        public double PixelAspect
        {
            get
            {
                string s = "";
                if (Settings.MI_Original_AR)
                {
                    s = Get_Splitted(StreamKind.Video, 0, "PixelAspectRatio_Original");     //Из потока (если доступно)
                    if (s == "") s = Get_Splitted(StreamKind.Video, 0, "PixelAspectRatio"); //Из контейнера или общее
                }
                else
                    s = Get_Splitted(StreamKind.Video, 0, "PixelAspectRatio");

                if (s == "") return 1.0;

                double ar = Aspect;
                double we_get = Calculate.ConvertStringToDouble(s);
                if (we_get != 1.0 && (ar == 16.0 / 9.0 || ar == 4.0 / 3.0 || ar == 2.0 || ar == 2.210))
                {
                    int w = 0, h = 0;
                    if (Settings.MI_Original_AR)
                    {
                        //Из потока (если доступно)
                        string ow = Get_Splitted(StreamKind.Video, 0, "Width_Original");
                        Int32.TryParse(ow, NumberStyles.Integer, null, out w);
                        string oh = Get_Splitted(StreamKind.Video, 0, "Height_Original");
                        Int32.TryParse(oh, NumberStyles.Integer, null, out  h);
                    }
                    if (w == 0) w = Width;
                    if (h == 0) h = Height;

                    //Подправляем PAR (в дополнение к DAR)
                    double we_calc = Aspect / ((double)w / (double)h);
                    if (Math.Abs(we_get - we_calc) < 0.0006) return we_calc;
                }

                return we_get;
            }
        }

        public string FrameRate
        {
            get
            {
                string x = "";
                if (Settings.MI_Original_fps)
                {
                    x = Get_Splitted(StreamKind.Video, 0, "FrameRate_Original");     //Из потока (если доступно)
                    if (x == "") x = Get_Splitted(StreamKind.Video, 0, "FrameRate"); //Из контейнера или общее
                }
                else
                    x = Get_Splitted(StreamKind.Video, 0, "FrameRate");

                if (x == "")
                {
                    string s = Standart;
                    if (s != "")
                    {
                        if (s == "PAL")
                            x = "25.000";
                        else
                            x = "29.970";
                    }
                }

                return x;
            }
        }

        public int Width
        {
            get
            {
                int n = 0;
                string x = Get_Splitted(StreamKind.Video, 0, "Width");
                Int32.TryParse(x, NumberStyles.Integer, null, out n);
                return n;
            }
        }

        public int Height
        {
            get
            {
                int n = 0;
                string x = Get_Splitted(StreamKind.Video, 0, "Height");
                Int32.TryParse(x, NumberStyles.Integer, null, out n);
                return n;
            }
        }

        public long Milliseconds
        {
            get
            {
                long n = 0;
                string x = Get_Splitted(StreamKind.General, 0, "Duration");
                if (x != "")
                {
                    try
                    {
                        //Тут try-catch обязателен, т.к. MediaInfo иногда
                        //выдает такое, что даже Int64 переполняется.
                        n = Convert.ToInt32(Calculate.ConvertStringToDouble(x));
                    }
                    catch { }
                }
                return n;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromMilliseconds(Milliseconds);
            }
        }

        public int Delay (int track)
        {
            string vvdelay = Get_Splitted(StreamKind.Audio, track, "Video_Delay");
            string sadelay = Get_Splitted(StreamKind.Audio, track, "Delay");
            string svdelay = Get_Splitted(StreamKind.Video, 0, "Delay");

            //Используем параметр "Video delay", похоже МедиаИнфо уже всё посчитала
            if (vvdelay != "" && Settings.NewDelayMethod)
            {
                int delay;
                if (Int32.TryParse(vvdelay, NumberStyles.Integer, null, out delay)) return delay;
            }

            //Подправляем (mpeg, ts файлы)
            if (Settings.NewDelayMethod)
            {
                while (sadelay.Length > 2 && sadelay.Contains(".")) sadelay = sadelay.Remove(sadelay.Length - 1, 1);
                while (svdelay.Length > 2 && svdelay.Contains(".")) svdelay = svdelay.Remove(svdelay.Length - 1, 1);
            }

            //задержка звука
            int adelay = 0;
            Int32.TryParse(sadelay, NumberStyles.Integer, null, out adelay);

            //задержка видео
            int vdelay = 0;
            Int32.TryParse(svdelay, NumberStyles.Integer, null, out vdelay);

            //сверяем задержку видео
            if (vdelay == adelay)
                return 0;

            //Если задержка сразу на обоих треках (mpeg, ts файлы)
            if (vdelay != 0 && adelay != 0 && Settings.NewDelayMethod)
                adelay = adelay - vdelay;

            //невероятно большая задержка
            if (adelay < -5000 || adelay > 5000)
                adelay = 0;

            return adelay;
        }

        public string Samplerate(int track)
        {
            string value = Get_Splitted(StreamKind.Audio, track, "SamplingRate");
            if (value == "") value = null;

            return value;
        }

        public int AudioBitrate(int track)
        {
            int n = 0;

            string bitrates = Get_Splitted(StreamKind.Audio, track, "BitRate");
            string nbitrates = Get_Splitted(StreamKind.Audio, track, "BitRate_Nominal");

            double bitrate = 0.0;
            Double.TryParse(bitrates, NumberStyles.Float, null, out bitrate);

            double nbitrate = 0.0;
            Double.TryParse(nbitrates, NumberStyles.Float, null, out nbitrate);

            if (nbitrate < bitrate && nbitrate != 0.0)
                n = (int)(nbitrate / 1000.0);
            else
                n = (int)(bitrate / 1000.0);

            return n;
        }

        public int VideoBitrate
        {
            get
            {
                int n = 0;

                string bitrates = Get_Splitted(StreamKind.Video, 0, "BitRate");
                string nbitrates = Get_Splitted(StreamKind.Video, 0, "BitRate_Nominal");

                if (bitrates == "")
                {
                    bitrates = Get_Splitted(StreamKind.General, 0, "OverallBitRate");
                    nbitrates = Get_Splitted(StreamKind.General, 0, "OverallBitRate_Nominal");
                }

                double bitrate = Calculate.ConvertStringToDouble(bitrates);
                double nbitrate = Calculate.ConvertStringToDouble(nbitrates);

                if (nbitrate < bitrate && nbitrate != 0.0)
                    n = (int)(nbitrate / 1000.0);
                else
                    n = (int)(bitrate / 1000.0);

                return n;
            }
        }

        public string AudioLanguage(int track)
        {
            string x = Get_Splitted(StreamKind.Audio, track, "Language/String");
            if (x == "" || x.Contains(" "))
                x = "Unknown";
            if (x == "en-us")
                x = "English";
            return x;
        }

        public string SubLanguage(int track)
        {
            string x = Get_Splitted(StreamKind.Text, track, "Language/String");
            if (x == "") x = "Unknown";

            return x;
        }

        public int Channels(int track)
        {
            int n = 0;
            string x = Get_Splitted(StreamKind.Audio, track, "Channel(s)");
            Int32.TryParse(x, NumberStyles.Integer, null, out n);
            return n;
        }

        public int Bits(int track)
        {
            int n = 0;
            string x = Get_Splitted(StreamKind.Audio, track, "BitDepth");
            if (x == "") x = Get_Splitted(StreamKind.Audio, track, "Resolution");
            Int32.TryParse(x, NumberStyles.Integer, null, out n);
            return n;
        }

        public string Standart
        {
            get
            {
                return Get_Splitted(StreamKind.Video, 0, "Standard");
            }
        }

        public int Frames
        {
            get
            {
                int n = 0;
                string x = Get_Splitted(StreamKind.Video, 0, "FrameCount");
                if (x != "")
                {
                    try
                    {
                        n = Convert.ToInt32(x);
                    }
                    catch { }
                }
                return n;
            }
        }

        public string ScanType
        {
            get
            {
                return Get_Splitted(StreamKind.Video, 0, "ScanType");
            }
        }

        public SourceType Interlace
        {
            get
            {
                string interlace = Get_Splitted(StreamKind.Video, 0, "ScanType");
                SourceType ininterlace = SourceType.PROGRESSIVE;

                if (interlace == "Interlaced")
                    ininterlace = SourceType.INTERLACED;

                return ininterlace;
            }
        }

        public string ScanOrder
        {
            get
            {
                return Get_Splitted(StreamKind.Video, 0, "ScanOrder");
            }
        }
    }
}
