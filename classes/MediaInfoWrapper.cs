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
        ~MediaInfoWrapper() { if (Handle != IntPtr.Zero) MediaInfo_Delete(Handle); }

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

        public AudioStream GetAudioInfoFromAFile(string filepath, bool and_close_mi)
        {
            AudioStream stream = new AudioStream();
            Open(filepath);

            stream.audiopath = filepath;
            stream.audiofiles = new string[] { stream.audiopath };
            stream.codec = ACodecString(0);
            stream.codecshort = ACodecShort(0);
            stream.language = AudioLanguage(0);
            stream.bitrate = AudioBitrate(0);
            stream.samplerate = Samplerate(0);
            stream.channels = Channels(0);
            stream.bits = Bits(0);
            stream.delay = 0;

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

            if (and_close_mi)
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
            if (!string.IsNullOrEmpty(s))
            {
                int index = s.IndexOf("-"); //0-1 (TS)
                if (index >= 0) s = s.Substring(index + 1);
                if (Int32.TryParse(s, NumberStyles.Integer, null, out index))
                    return index;
            }
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
            string s = Get(StreamKind.Video, 0, "StreamOrder");
            if (!string.IsNullOrEmpty(s))
            {
                int index = s.IndexOf("-"); //0-1 (TS)
                if (index >= 0) s = s.Substring(index + 1);
                if (Int32.TryParse(s, NumberStyles.Integer, null, out index))
                    return index;
            }
            return -1;
        }

        public string VCodecString
        {
            get
            {
                //(Codec fields are DEPRECATED)
                string s = Get(StreamKind.Video, 0, "Codec/String");
                //string fcc = Get(StreamKind.Video, 0, "Codec/CC");
                //string a = Get(StreamKind.Video, 0, "Format");
                //string b = Get(StreamKind.Video, 0, "CodecID");
                //string c = Get(StreamKind.Video, 0, "CodecID/Hint");

                if (s == "")
                    s = Get(StreamKind.General, 0, "Codec/String");

                if (s.ToLower().Contains("avc"))
                    return "h264";

                return s;
            }
        }

        public string VCodecShort
        {
            get
            {
                //(Codec fields are DEPRECATED)
                string s = Get(StreamKind.Video, 0, "Codec/String");
                string fcc = Get(StreamKind.Video, 0, "Codec/CC").ToLower();

                if (s == "")
                    s = Get(StreamKind.General, 0, "Codec/String");

                string lower = s.ToLower();

                if (lower == "sorenson spark")
                    return "h263";
                if (lower.Contains("divx"))
                    return "DivX";
                if (lower.Contains("xvid")) //XVID / V_MS/VFW/FOURCC
                    return "XviD";
                if (lower.Contains("avc") || lower.Contains("264"))
                    return "h264";
                if (lower.Contains("mpeg"))
                {
                    if (lower.Contains("1"))
                        return "MPEG1";
                    if (lower.Contains("2") || lower.Contains("PS"))
                        return "MPEG2";
                    if (lower.Contains("4"))
                    {
                        if (fcc == "20")
                            return "MPEG4";
                        if (fcc == "dx50" || fcc == "divx")
                            return "DivX";
                        if (fcc == "xvid")
                            return "XviD";
                        return "h264";
                    }
                }
                if (lower.Contains("wmv"))
                    return "WMV";
                if (lower.Contains("vc1") || lower.Contains("vc-1") ||
                    lower == "v_ms/vfw/fourcc") //V_MS/VFW/FOURCC
                    return "VC1";
                if (lower.Contains("flash"))
                    return "FLV";
                if (lower.Contains("vp6"))
                    return "VP6";
                if (lower.Contains("vp7"))
                    return "VP7";
                if (lower.Contains("vp8"))
                    return "VP8";
                if (lower.Contains("vp9"))
                    return "VP9";

                return s;
            }
        }

        public string ACodecString(int track)
        {
            //(Codec fields are DEPRECATED)
            return Get(StreamKind.Audio, track, "Codec/String");
        }

        public string ACodecShort(int track)
        {
            //(Codec fields are DEPRECATED)
            string s = Get(StreamKind.Audio, track, "Codec/String");
            //string fcc = Get(StreamKind.Audio, track, "Codec/CC");
            //string a = Get(StreamKind.Audio, track, "Format");
            //string b = Get(StreamKind.Audio, track, "CodecID");
            //string c = Get(StreamKind.Audio, track, "CodecID/Hint");

            string lower = s.ToLower();

            if (lower == "lossless")
            {
                s = Get(StreamKind.Audio, track, "Format");
                lower = s.ToLower();
            }

            if (lower.Contains("mpeg"))
            {
                if (lower.Contains("layer 2") || lower.Contains("l2"))
                    return "MP2";
                if (lower.Contains("layer 3") || lower.Contains("l3") ||
                    lower == "mpeg-1 audio")
                    return "MP3";
            }
            if (lower == "mpa1l3" ||
                lower == "mpa2l3" ||
                lower == "a_mp3")
                return "MP3";
            if (lower == "vorbis")
                return "OGG";
            if (lower.Contains("aac"))
                return "AAC";
            if (lower.Contains("alac"))
                return "ALAC";
            if (lower.Contains("wma"))
                return "WMA";
            if (lower == "ac3+" ||
                lower == "e-ac-3")
                return "AC3+";
            if (lower.Contains("ac3"))
                return "AC3";
            if (lower.Contains("dts"))
                return "DTS";
            if (lower == "a_lpcm")
                return "LPCM";

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
