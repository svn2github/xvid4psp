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
        public static extern IntPtr MediaInfo_New();
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern void MediaInfo_Delete(IntPtr Handle);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern void MediaInfo_Close(IntPtr Handle);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_State_Get(IntPtr Handle);
        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Count_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        public MediaInfoWrapper() { Handle = MediaInfo_New(); }
        ~MediaInfoWrapper() { MediaInfo_Delete(Handle); }
        public int Open(String FileName) { return (int)MediaInfo_Open(Handle, FileName); }
        public void Close() { MediaInfo_Close(Handle); }
        public String Inform() { return Marshal.PtrToStringUni(MediaInfo_Inform(Handle, (IntPtr)0)); }
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch) { return Marshal.PtrToStringUni(MediaInfo_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch)); } //туут
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo) { return Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo)); }
        public String Option(String Option, String Value) { return Marshal.PtrToStringUni(MediaInfo_Option(Handle, Option, Value)); }
        public int State_Get() { return (int)MediaInfo_State_Get(Handle); }
        public int Count_Get(StreamKind StreamKind, int StreamNumber) { return (int)MediaInfo_Count_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber); }
        private IntPtr Handle;

        //Default values, if you know how to set default values in C#, say me
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo) { return Get(StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); }
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text); }
        public String Option(String Option_) { return Option(Option_, ""); }
        public int Count_Get(StreamKind StreamKind) { return Count_Get(StreamKind, -1); }

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
           
       //Settings.Test = ACodecString(0);
            stream.audiopath = filepath;
            stream.audiofiles = new string[] { stream.audiopath };
            stream.codec = ACodecString(0);
            stream.codecshort = ACodecShort(0);
            stream.language = "Unknown"; //"English";
            stream.bitrate = AudioBitrate(0);
            stream.delay = 0;
            stream.samplerate = Samplerate(0);
            stream.channels = Channels(0);
            stream.bits = Bits(0);
          //stream.channels = Channels(0);
           
       //Settings.Test = ACodecShort(0);

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
            //m.inframerate = Fr ; //FrameRate; 
            Close();
            return stream;
        }

        //public Massive GetFrameRate(Massive m)
        //{
        //    Open(m.infilepath);

         //   m.inframerate = FrameRate;

        //    Close();
        //    return m;
        //}

        //public Massive GetAudioStreams(Massive m)
        //{
        //    Open(m.infilepath);
        //    m.inaudiostreams = CountAudioStreams;
        //    Close();
        //    return m;
        //}

        public int AudioID(int track)
        {
            string x = Get(StreamKind.Audio, track, "ID");
            if (x == "C0" ||
                x == "A0" ||
                x == "C0 / 45" ||
                x == "CO / 110" ||
                x == "81 / 45" ||
                x == "81 / 1100" ||
                x == "80" ||
                x == "81 / 1100" ||
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

        public int VideoID()
        {
            try
            {
                string s = Get(StreamKind.Video, 0, "ID");
                int z = Convert.ToInt32(s);
                return z;
            }
            catch
            {
                return 0;
            }
        }

        public string VCodec
        {
            get
            {
                string s = Get(StreamKind.Video, 0, "Codec");
                if (s == "")
                    s = Get(StreamKind.General, 0, "Codec");
                return s;
            }
        }

        public string VCodecString
        {
            get
            {
                string s = Get(StreamKind.Video, 0, "Codec/String");
                //string fcc = Get(StreamKind.Video, 0, "Codec/CC");
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

                if (s == "Sorenson H263")
                    s = "h263";

                if (s == "DivX 5" ||
                    s == "DivX 3 Low")
                    s = "DivX";

                if (s == "XVID / V_MS/VFW/FOURCC")
                    s = "XviD";

                if (s == "MPEG-1 Video" ||
                    s == "V_MPEG - 1")
                    s = "MPEG1";

                if (s == "MPEG-2 Video" ||
                    s == "V_MPEG-2")
                    s = "MPEG2";

                if (s == "WMV3")
                    s = "WMV";

                if (s == "V_MS/VFW/WVC1" ||
                    s == "V_MS/VFW/FOURCC" ||
                    s == "WVC1" ||
                    s == "VC-1")
                    s = "VC1";

                if (s == "H.264" ||
                    s == "MPEG-4 AVC" ||
                    s == "AVC" ||
                    s == "V_MPEG4/ISO/AVC")
                    s = "h264";

                if (s == "MPEG-4" ||
                    s == "MPEG-4 Visual")
                {
                    if (fcc == "20")
                        s = "MPEG4";
                    else if (fcc == "DX50" ||
                        fcc == "DIVX")
                        s = "DivX";
                    else if (fcc == "XVID")
                        s = "XviD";
                    else
                        s = "h264";
                }

                if (s == "Flash Video")
                    s = "FLV";
                return s;
            }
        }

        public string ACodec(int track)
        {
                return Get(StreamKind.Audio, track, "Codec");
        }

        public string ACodecString(int track)
        {
            return Get(StreamKind.Audio, track, "Codec/String");
        }

        public string ACodecShort(int track)
        {
            string s = Get(StreamKind.Audio, track, "Codec/String");
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
            if (s == "MPEG-1 Audio layer 2")
                s = "MP2";
            if (s == "Vorbis")
                s = "OGG";
            if (s == "AAC LC" || s == "AAC LC-SBR" || s == "AAC LC-SBR-PS" || s == "A_AAC")
                s = "AAC";
            if (s == "WMA2" ||
                s == "WMA3")
                s = "WMA";
            if (s == "A_AC3")
                s = "AC3";
            if (s == "A_DTS" ||
                s == "DTS-HD")
                s = "DTS";
            if (s == "A_AAC")
                s = "AAC";
            if (s == "A_LPCM")
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
                return Get(StreamKind.Video, 0, "AspectRatio/String").Replace("/", ":");
            }
        }

        public double Aspect
        {
            get
            {
                string s = Get(StreamKind.Video, 0, "AspectRatio");
                if (s == "")
                    return 1.333;
                return Calculate.ConvertStringToDouble(s);
            }
        }

        public double PixelAspect
        {
            get
            {
                string s = Get(StreamKind.Video, 0, "PixelAspectRatio");
                if (s == "")
                    return 1.0;
                return Calculate.ConvertStringToDouble(s);
            }
        }

        public string FrameRateString
        {
            get
            {
                return Get(StreamKind.Video, 0, "FrameRate/String");
            }
        }

        public string FrameRate
        {
            get
            {
                string x = Get(StreamKind.Video, 0, "FrameRate");
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
                if (x == "23.980")
                    x = "23.976"; //скорее всего это просто кривой файл
                return x;
            }
        }

        public int Width
        {
            get
            {
                int n = 0;
                string x = Get(StreamKind.Video, 0, "Width");
                if (x != "")
                    n = Convert.ToInt32(x);
                return n;
            }
        }

        public int Height
        {
            get
            {
                int n = 0;
                string x = Get(StreamKind.Video, 0, "Height");
                if (x != "")
                   n = Convert.ToInt32(x);
                return n;
            }
        }

        public long Milliseconds
        {
            get
            {
                long n = 0;
                string x = Get(StreamKind.General, 0, "PlayTime");
                if (x != "")
                {
                    try
                    {
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
            string sadelay = Get(StreamKind.Audio, track, "Delay");
            string svdelay = Get(StreamKind.Video, 0, "Delay");

            //подправляем
            if (sadelay.Contains("/"))
            {
                string[] separator = new string[] { " / " };
                string[] a = sadelay.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                sadelay = a[0];
            }
            if (svdelay.Contains("/"))
            {
                string[] separator = new string[] { " / " };
                string[] a = svdelay.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                svdelay = a[0];
            }

            //задержка звука
            int adelay = 0;
            Int32.TryParse(sadelay, NumberStyles.Integer, null, out adelay);

            //задержка видео
            int vdelay = 0;
            Int32.TryParse(svdelay, NumberStyles.Integer, null, out vdelay);

            //сверяем задержку видео
            if (vdelay == adelay)
                adelay = 0;

            //невероятно большая задержка
            if (adelay < -5000 || adelay > 5000)
                adelay = 0;

            return adelay;
        }

        public string Samplerate(int track)
        {
            string value = Get(StreamKind.Audio, track, "SamplingRate");
            if (value.Contains("/"))
            {
                string[] separator = new string[] { " / " };
                string[] a = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                value = a[0];
            }

            if (value == "")
                value = null;

            return value;
        }

        public string SamplerateString(int track)
        {
            return Get(StreamKind.Audio, track, "SamplingRate/String");
        }

        public int AudioBitrateBytes(int track)
        {
            int n = 0;

            string bitrates = Get(StreamKind.Audio, track, "BitRate");
            string nbitrates = Get(StreamKind.Audio, track, "BitRate_Nominal");

            double bitrate = Calculate.ConvertStringToDouble(bitrates);
            double nbitrate = Calculate.ConvertStringToDouble(nbitrates);

            if (nbitrate < bitrate &&
                nbitrate != 0.0)
                n = (int)nbitrate;
            else
                n = (int)bitrate;

            return n;
        }

        public int AudioBitrate(int track)
        {
            int n = 0;

            string bitrates = Get(StreamKind.Audio, track, "BitRate");
            string nbitrates = Get(StreamKind.Audio, track, "BitRate_Nominal");

            double bitrate = 0.0;
            Double.TryParse(bitrates, NumberStyles.Float, null, out bitrate);

            double nbitrate = 0.0;
            Double.TryParse(nbitrates, NumberStyles.Float, null, out nbitrate);

            if (nbitrate < bitrate &&
                nbitrate != 0.0)
                n = (int)(nbitrate / 1000.0);
            else
                n = (int)(bitrate / 1000.0);
            
            return n;
        }

        public long VideoBitrateBytes
        {
            get
            {
                int n = 0;

                string bitrates = Get(StreamKind.Video, 0, "BitRate");
                string nbitrates = Get(StreamKind.Video, 0, "BitRate_Nominal");

                if (bitrates == "")
                {
                    bitrates = Get(StreamKind.General, 0, "BitRate");
                    nbitrates = Get(StreamKind.General, 0, "BitRate_Nominal");

                    double bitrate = Calculate.ConvertStringToDouble(bitrates);
                    double nbitrate = Calculate.ConvertStringToDouble(nbitrates);

                    if (nbitrate < bitrate &&
                        nbitrate != 0.0)
                        n = (int)(nbitrate);
                    else
                        n = (int)(bitrate);
                }
                else
                {
                    double bitrate = Calculate.ConvertStringToDouble(bitrates);
                    double nbitrate = Calculate.ConvertStringToDouble(nbitrates);

                    if (nbitrate < bitrate &&
                        nbitrate != 0.0)
                        n = (int)(nbitrate);
                    else
                        n = (int)(bitrate);
                }

                return n;
            }
        }

        public int VideoBitrate
        {
            get
            {
                int n = 0;

                string bitrates = Get(StreamKind.Video, 0, "BitRate");
                string nbitrates = Get(StreamKind.Video, 0, "BitRate_Nominal");

                if (bitrates == "")
                {
                    bitrates = Get(StreamKind.General, 0, "BitRate");
                    nbitrates = Get(StreamKind.General, 0, "BitRate_Nominal");

                    double bitrate = Calculate.ConvertStringToDouble(bitrates);
                    double nbitrate = Calculate.ConvertStringToDouble(nbitrates);

                    if (nbitrate < bitrate &&
                        nbitrate != 0.0)
                        n = (int)(nbitrate / 1000.0);
                    else
                        n = (int)(bitrate / 1000.0);
                }
                else
                {
                    double bitrate = Calculate.ConvertStringToDouble(bitrates);
                    double nbitrate = Calculate.ConvertStringToDouble(nbitrates);
 
                    if (nbitrate < bitrate &&
                        nbitrate != 0.0)
                        n = (int)(nbitrate / 1000.0);
                    else
                        n = (int)(bitrate / 1000.0);
                }

                return n;
            }
        }

        public string AudioLanguage(int track)
        {
            string x = Get(StreamKind.Audio, track, "Language/String");
            if (x == "" || x.Contains(" "))
                x = "Unknown";
            if (x == "en-us")
                x = "English";
            return x;
        }

        public string SubLanguage(int track)
        {
            string x = Get(StreamKind.Text, track, "Language/String");
            if (x == "")
                x = "Unknown";
            return x;
        }

        public int Channels(int track)
        {
            int n = 0;
            string x = Get(StreamKind.Audio, track, "Channel(s)");
            if (x != "")
               n = Convert.ToInt32(x);
            return n;
        }

        public int Bits(int track)
        {
            int n = 0;
            string x = Get(StreamKind.Audio, track, "Resolution");
            if (x != "" && x != null)
               n = Convert.ToInt32(x);
            return n;
        }

        public string Standart
        {
            get
            {
                return Get(StreamKind.Video, 0, "Standard");
            }
        }

        public int FileSizeBytes
        {
            get
            {
                return Convert.ToInt32(Get(StreamKind.General, 0, "FileSize"));
            }
        }

        public string FileSizeString
        {
            get
            {
                return Get(StreamKind.General, 0, "FileSize/String4");
            }
        }

        public int Frames
        {
            get
            {
                int n = 0;
                string x = Get(StreamKind.Video, 0, "FrameCount");
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

        public SourceType Interlace
        {
            get
            {
                string interlace = Get(StreamKind.Video, 0, "Interlacement");
                SourceType ininterlace = SourceType.PROGRESSIVE;

                if (interlace == "TFF" ||
                    interlace == "Interlaced" ||
                    interlace == "BFF")
                    ininterlace = SourceType.INTERLACED;

                return ininterlace;
            }
        }

        public string ScanOrder
        {
            get
            {
                return Get(StreamKind.Video, 0, "ScanOrder");
            }
        }

    }
}
