using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace XviD4PSP
{
   public class FFInfo
    {
       public string info = null;
       Process encoderProcess;

        public void Open(string filepath)
        {
            encoderProcess = new Process();
            ProcessStartInfo pinfo = new ProcessStartInfo();

            pinfo.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            pinfo.WorkingDirectory = Path.GetDirectoryName(pinfo.FileName);
            pinfo.UseShellExecute = false;
            pinfo.RedirectStandardOutput = true;
            pinfo.RedirectStandardError = true;
            pinfo.CreateNoWindow = true;

            pinfo.Arguments = "-i \"" + filepath + "\"";

            encoderProcess.StartInfo = pinfo;
            encoderProcess.Start();

            int wseconds = 0;
            while (wseconds < 50 &&
                !encoderProcess.HasExited) //ждём не более пяти секунд
            {
                Thread.Sleep(100);
                wseconds++;
            }

            if (!encoderProcess.HasExited)
            {
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
                //info = null;
            }

            info = encoderProcess.StandardError.ReadToEnd();
        }

        public void Close()
        {
            if (encoderProcess != null &&
                !encoderProcess.HasExited)
            {
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
            }
            info = null;
        }

        private string SearchRegEx(string pattern, out int times)
        {
            //делим на строки
            string[] separator = new string[] { Environment.NewLine };
            string[] lines = info.Split(separator, StringSplitOptions.None);
            string result = null;
            times = 0;

            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

            foreach (string line in lines)
            {
                Match m = r.Match(line);
                if (m.Success)
                {
                    result = m.Groups[1].Value;
                    times++;
                }
            }

            return result;
        }

        public int StreamCount()
        {
            if (info != null)
            {
                int streams = 0;
                string[] separator = new string[] { Environment.NewLine };
                string[] lines = info.Split(separator, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    if (line.StartsWith("    Stream #0."))
                        streams += 1;
                }
                return streams;
            }
            else
                return 0;
        }

        public int AudioStreamCount()
        {
            if (info != null)
            {
                int streams = 0;
                string[] separator = new string[] { Environment.NewLine };
                string[] lines = info.Split(separator, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    if (line.StartsWith("    Stream #0.") && line.Contains(": Audio:"))
                        streams++;
                }
                        return streams;
            }
            else
                return 0;
        }

        public string StreamLanguage(int stream)
        {
            if (info != null)
            {
                int times;
                string lang = SearchRegEx(stream + @"\((\D+)\)", out times);
                if (times != 0)
                {
                    if (lang == "eng")
                        return "English";
                    else if (lang == "jpn")
                        return "Japanese";
                    else
                        return lang;
                }
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string StreamType(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+\s(\D+):", out times);
                if (times != 0)
                    return type;
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string StreamCodec(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+:\s(\w+),", out times);
                if (times != 0)
                    return type;
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string StreamCodecShort(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+:\s(\w+),", out times);
                if (times != 0)
                {
                    if (type == "liba52")
                        type = "AC3";
                    if (type == "mpeg4aac")
                        type = "AAC";
                    if (type == "pcm_s16le")
                        type = "PCM";
                    return type.ToUpper();
                }
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string StreamColor(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+\s(\w+),", out times);
                if (times != 0)
                    return type;
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string StreamSamplerate(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+,\s(\d+)\sHz", out times);
                if (times != 0)
                    return type;
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public int StreamChannels(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+Hz,.(\d).channel", out times); //2 channels, 3 channels, 
                if (times != 0)
                {
                    return Convert.ToInt32(type);
                }
                else
                {
                    type = SearchRegEx(@"Stream..0." + stream + @".+Hz,.(\d\.\d),", out times); //5.1, 2.1, 
                    if (times != 0)
                    {
                        string[] types;
                        string[] separator = new string[] { "." };
                        types = type.Split(separator, StringSplitOptions.None);
                        return Convert.ToInt32(types[0]) + Convert.ToInt32(types[1]);
                    }
                    else
                    {
                        type = SearchRegEx(@"Stream..0." + stream + @".+Hz,.(\w+)", out times); //mono, stereo
                        if (times != 0)
                        {
                            if (type == "mono")
                                return 1;
                            else if (type == "stereo")
                                return 2;
                            else
                                return 0;
                        }
                    }
                    return 0;
                }                       
            }
            else
                return 0;
        }

        public string StreamFramerate(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+,.(\d+.\d+).tb\(r\)", out times);
                if (times != 0)
                {
                    if (type == "23.98")
                        type = "23.976";
                    return Calculate.ConvertDoubleToPointString(Calculate.ConvertStringToDouble(type));
                }
                else
                    return "";
            }
            else
                return "";
        }

        public int StreamW(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+,.(\d+)x", out times);      
                if (times != 0)
                    return Convert.ToInt32(type);
                else
                    return 0;
            }
            else
                return 0;
        }

        public int StreamH(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+,.\d+x(\d+)", out times);
                if (times != 0)
                    return Convert.ToInt32(type);
                else
                    return 0;
            }
            else
                return 0;
        }

        public int StreamBitrate(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream.+0." + stream + @".+\s(\d+)\skb", out times);
                if (times != 0)
                    return Convert.ToInt32(type);
                else
                    return 0;
            }
            else
                return 0;
        }

        public int VideoBitrate(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream.+0." + stream + @".+Video.+\s(\d+)\skb", out times);
                if (times != 0)
                    return Convert.ToInt32(type);
                else
                {
                    //если битрейт не указан в потоке
                    int abitrate = 0;
                    for (int snum = AudioStream(); snum < StreamCount(); snum++)
                    {
                        abitrate += StreamBitrate(snum);
                    }

                    int vbitrate = TotalBitrate() - abitrate;
                    if (vbitrate > 0)
                        return vbitrate;
                    else
                        return 0;
                }
            }
            else
                return 0;
        }

        public int TotalBitrate()
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Duration.+\s(\d+)\skb", out times);
                if (times != 0)
                    return Convert.ToInt32(type);
                else
                    return 0;
            }
            else
                return 0;
        }

        public string StreamPAR(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+PAR\s(\d+:\d+)", out times);
                if (times != 0)
                    return type;
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string StreamDAR(int stream)
        {
            if (info != null)
            {
                int times;
                string type = SearchRegEx(@"Stream..0." + stream + @".+DAR\s(\d+:\d+)", out times);
                if (times != 0)
                    return type;
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public string Timeline()
        {
            if (info != null)
            {
                int times;
                string timeline = SearchRegEx(@"Duration:.(\S+),", out times);
                if (timeline != "")
                {
                    return timeline;
                }
                else
                    return "Unknown";
            }
            else
                return "Unknown";
        }

        public int AudioStream()
        {
            if (info != null)
            {
                //делим на строки
                string[] separator = new string[] { Environment.NewLine };
                string[] lines = info.Split(separator, StringSplitOptions.None);
                string result = null;
                int stream = 1;

                Regex r = new Regex(@"Stream.+0\.(\d).+Audio", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

                foreach (string line in lines)
                {
                    Match m = r.Match(line);
                    if (m.Success)
                    {
                        result = m.Groups[1].Value;
                        Int32.TryParse(result, NumberStyles.Integer, null, out stream);
                        return stream;
                    }
                }
                return stream;
                
                //int astreams = 0;
                //string astream = SearchRegEx(@"Stream.+0\.(\d).+Audio", out astreams);
                //Int32.TryParse(astream, NumberStyles.Integer, null, out stream);
                //return astreams - (stream - 1);
            }
            else
                return 0;
        }

        public int VideoStream()
        {
            if (info != null)
            {
                int stream = 0;
                string vstream = SearchRegEx(@"Stream.+0\.(\d).+Video", out stream);
                Int32.TryParse(vstream, NumberStyles.Integer, null, out stream);
                return stream;
            }
            else
                return 0;
        }

        public TimeSpan Duration()
        {
            //Input #0, matroska, from 'D:\HardFiles_Current\Burst_Angel_clip.mkv':
            //Duration: 00:00:12.5, start: 0.000000, bitrate: N/A
            //Stream #0.3(eng): Subtitle: 0x0000
            if (info != null)
            {
                int times;
                string timeline = SearchRegEx(@"Duration:.(\S+),", out times);

                string sh = SearchRegEx(@"Duration:.(\d+)", out times);
                string sm = SearchRegEx(@"Duration:....(\d+)", out times);
                string ss = SearchRegEx(@"Duration:.......(\d+)", out times);
                string sms = SearchRegEx(@"Duration:..........(\d+)", out times);

                if (times != 0)
                {
                    int ih = 0;
                    Int32.TryParse(sh, NumberStyles.Integer, null, out ih);
                    int im = 0;
                    Int32.TryParse(sm, NumberStyles.Integer, null, out im);
                    int isec = 0;
                    Int32.TryParse(ss, NumberStyles.Integer, null, out isec);
                    int ims = 0;
                    Int32.TryParse(sms, NumberStyles.Integer, null, out ims);

                    double totalms = (ims * 100) + (isec * 1000) + (im * 60 * 1000) + (ih * 60 * 60 * 1000);

                    return TimeSpan.FromMilliseconds(totalms);
                }
                else
                    return TimeSpan.Zero;
            }
            else
                return TimeSpan.Zero;
        }


    }
}
