﻿using System;
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
        private static object locker = new object();
        private Process encoderProcess = null;
        private string[] global_lines = null;
        public StringBuilder info = new StringBuilder();

        //https://msdn.microsoft.com/ru-ru/library/3206d374(v=vs.110).aspx
        //https://msdn.microsoft.com/ru-ru/library/4edbef7e(v=vs.90).aspx
        //Все знаки, кроме следующих, соответствуют сами себе:
        //. $ ^ { [ ( | ) * + ? \ (и еще #)

        public void Open(string filepath)
        {
            encoderProcess = new Process();
            ProcessStartInfo pinfo = new ProcessStartInfo();

            pinfo.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            pinfo.WorkingDirectory = Path.GetDirectoryName(pinfo.FileName);
            pinfo.UseShellExecute = false;
            pinfo.RedirectStandardOutput = false;
            pinfo.RedirectStandardError = true;
            pinfo.StandardErrorEncoding = Encoding.UTF8;
            pinfo.CreateNoWindow = true;

            pinfo.Arguments = "-hide_banner -i \"" + filepath + "\"";

            encoderProcess.StartInfo = pinfo;
            encoderProcess.Start();

            //Читаем лог
            while (encoderProcess != null && !encoderProcess.HasExited)
            {
                info.AppendLine(encoderProcess.StandardError.ReadLine());
            }

            //Дочитываем и сразу делим на строки
            if (encoderProcess != null)
            {
                info.Append(encoderProcess.StandardError.ReadToEnd());
                if (info.Length > 0) global_lines = info.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        public void Close()
        {
            lock (locker)
            {
                if (encoderProcess != null)
                {
                    try
                    {
                        if (!encoderProcess.HasExited)
                        {
                            encoderProcess.Kill();
                            encoderProcess.WaitForExit();
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        encoderProcess.Close();
                        encoderProcess.Dispose();
                        encoderProcess = null;
                    }
                }
            }

            info.Length = 0;
            global_lines = null;
        }

        private bool SearchRegEx(string pattern, out string value)
        {
            value = "";
            if (info.Length == 0) return false;

            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            foreach (string line in global_lines)
            {
                Match m = r.Match(line);
                if (m.Success)
                {
                    value = m.Groups[1].Value;
                    return true;
                }
            }
            return false;
        }

        private string SearchRegEx(string pattern, string _default)
        {
            if (info.Length == 0) return _default;

            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            foreach (string line in global_lines)
            {
                Match m = r.Match(line);
                if (m.Success) return m.Groups[1].Value;
            }
            return _default;
        }

        private int SearchRegEx(string pattern, int _default)
        {
            if (info.Length == 0) return _default;

            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            foreach (string line in global_lines)
            {
                Match m = r.Match(line);
                if (m.Success) return Convert.ToInt32(m.Groups[1].Value);
            }
            return _default;
        }

        //Общее кол-во треков (всех)
        public int StreamsCount()
        {
            int streams = 0;
            if (info.Length > 0)
            {
                Regex r = new Regex(@"^\s+Stream\s\#0:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                foreach (string line in global_lines)
                {
                    Match m = r.Match(line);
                    if (m.Success) streams += 1;
                }
            }
            return streams;
        }

        //ID первого видео трека
        public int FirstVideoStreamID()
        {
            return SearchRegEx(@"^\s+Stream\s\#0:(\d+).+Video:", 0);
        }

        //Список ID всех видео треков
        public ArrayList VideoStreams()
        {
            ArrayList v_streams = new ArrayList();
            if (info.Length > 0)
            {
                //Stream #0:0[0x1e0]: Video:
                Regex r = new Regex(@"^\s+Stream\s\#0:(\d+).+Video:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                foreach (string line in global_lines)
                {
                    Match m = r.Match(line);
                    if (m.Success) v_streams.Add(Convert.ToInt32(m.Groups[1].Value));
                }
            }
            return v_streams;
        }

        //ID первого аудио трека 
        public int FirstAudioStreamID()
        {
            return SearchRegEx(@"^\s+Stream\s\#0:(\d+).+Audio:", 0);
        }

        //Список ID всех аудио треков
        public ArrayList AudioStreams()
        {
            ArrayList a_streams = new ArrayList();
            if (info.Length > 0)
            {
                //Stream #0:1[0x1c0]: Audio:
                Regex r = new Regex(@"^\s+Stream\s\#0:(\d+).+Audio:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                foreach (string line in global_lines)
                {
                    Match m = r.Match(line);
                    if (m.Success) a_streams.Add(Convert.ToInt32(m.Groups[1].Value));
                }
            }
            return a_streams;
        }

        //ID трека для mkvmerge (mpeg\ts), без "мусора"
        public int FilteredStreamOrder(int stream)
        {
            if (stream > 0)
            {
                //Stream #0:0[0x1bf]: Data: dvd_nav_packet
                //Stream #0:2[0x20]: Subtitle: dvd_subtitle (эти mkvmerge не видит)
                //Stream #0:2[0x1200]: Subtitle: hdmv_pgs_subtitle ([144][0][0][0] / 0x0090), 1920x1080 (эти mkvmerge видит)
                //Stream #0:3[0x811]: Unknown: none ([161][0][0][0] / 0x00A1)
                int data = SearchRegEx(@"^\s+Stream\s\#0:(\d+)\D.+Data:\s\w+", -1);
                int subs = SearchRegEx(@"^\s+Stream\s\#0:(\d+)\D.+Subtitle:\sdvd_subtitle", -1);
                int unkn = SearchRegEx(@"^\s+Stream\s\#0:(\d+)\D.+Unknown:\s\w+", -1);
                
                int new_index = stream;
                if (data >= 0 && stream > data) new_index -= 1;
                if (subs >= 0 && stream > subs) new_index -= 1;
                if (unkn >= 0 && stream > unkn) new_index -= 1;
                return new_index;
            }
            return stream;
        }

        //Полностью вся строка для трека
        public string StreamFull(int stream)
        {
            return SearchRegEx(@"^\s+(Stream\s\#0:" + stream + @"\D.+)", "Unknown");
        }

        public string StreamLanguage(int stream)
        {
            string value = "";
            //Stream #0:1(und): Audio:
            //Stream #0:1[0x1100](HUN): Audio:
            if (!SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\((\D+)\):", out value))
                if (!SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\[.*?\((\D+)\):", out value))
                    return "Unknown";

            value = value.ToLower();
            if (value == "mul") return "Multiple";
            if (value == "und") return "Unknown";

            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (value.Length == 2 && ci.TwoLetterISOLanguageName == value ||
                    value.Length == 3 && ci.ThreeLetterISOLanguageName == value)
                    return ci.EnglishName.Split(new char[] { ' ' })[0];
            }

            return "Unknown";
        }

        public string StreamCodec(int stream)
        {
            //Stream #0:0[0x1e0]: Video: mpeg2video, yuv420p,
            //Stream #0:1[0x1c0]: Audio: mp2, 48000 Hz,
            //Stream #0:0: Video: h264 (Constrained Baseline),
            //Stream #0:0: Video: SVQ3 / 0x33515653,
            return SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+:\s(\w+)[\s,]", "Unknown");
        }

        public string StreamCodecShort(int stream)
        {
            string value = ""; //Stream #0:0[0x1e0]: Video: mpeg2video, yuv420p,
            if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+:\s(\w+)[\s,]", out value))
            {
                if (value == "liba52") return "AC3";
                if (value.Contains("aac")) return "AAC";
                if (value.Contains("pcm") || value.Contains("s16")) return "PCM";
                if (value.Contains("wma")) return "WMA";
                if (value.Contains("dsd")) return "DSD";
                if (value == "dca") return "DTS";
                return value.ToUpper();
            }
            return "Unknown";
        }

        public string StreamColor(int stream)
        {
            //Stream #0:0[0x1e0]: Video: mpeg2video, yuv420p,
            //Stream #0:0: Video: h264 (Constrained Baseline), yuv420p,
            return SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+Video:\s.+?,\s(\w+)[\(,]", "Unknown");
        }

        public string StreamSamplerate(int stream)
        {
            //Stream #0:1[0x1c0]: Audio: mp2, 48000 Hz, 2 channels,
            return SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(\d+)\sHz", "Unknown");
        }

        public int StreamChannels(int stream)
        {
            if (info.Length > 0)
            {
                string value = "";
                //Stream #0:1[0x1c0]: Audio: mp2, 48000 Hz, 2 channels,
                if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+Hz,\s(\d)\schannel", out value)) //2 channels, 3 channels
                {
                    return Convert.ToInt32(value);
                }
                //Stream #0:1[0x80]: Audio: ac3, 48000 Hz, 5.1,
                if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+Hz,\s(\d\.\d)", out value)) //5.1, 2.1
                {
                    string[] values = value.Split(new string[] { "." }, StringSplitOptions.None);
                    return Convert.ToInt32(values[0]) + Convert.ToInt32(values[1]);
                }
                //Stream #0:1[0x80]: Audio: ac3, 48000 Hz, stereo,
                if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+Hz,\s(\w+)", out value)) //mono, stereo
                {
                    if (value == "mono") return 1;
                    if (value == "stereo") return 2;
                    if (value == "quad") return 4;
                    if (value == "hexagonal") return 6;
                    if (value == "octagonal") return 8;
                    if (value == "downmix") return 2;
                }
            }
            return 0;
        }

        public string StreamFramerate(int stream)
        {
            /* dump.c
            int fps = st->avg_frame_rate.den && st->avg_frame_rate.num;
            int tbr = st->r_frame_rate.den && st->r_frame_rate.num;
            int tbn = st->time_base.den && st->time_base.num;
            int tbc = st->codec->time_base.den && st->codec->time_base.num;
            */

            string value = ""; //Stream #0:0: Video: mpeg2video, yuv420p, 720x576 [PAR 16:15 DAR 4:3], 9500 kb/s, 25 fps, 25 tbr, 90k tbn, 50 tbc
            if (!SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(\d+\.?\d*)\stbr", out value))
                if (!SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(\d+\.?\d*)\sfps", out value))
                    return "";

            if (value == "23.98") return "23.976";
            if (value == "47.95") return "47.952";
            return Calculate.ConvertDoubleToPointString(Calculate.ConvertStringToDouble(value));
        }

        public int StreamW(int stream)
        {
            //Stream #0:0[0x1e0]: Video: mpeg2video, yuv420p, 720x576 [PAR 16:15 DAR 4:3],
            return SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(\d+)x\d", 0);
        }

        public int StreamH(int stream)
        {
            //Stream #0:0[0x1e0]: Video: mpeg2video, yuv420p, 720x576 [PAR 16:15 DAR 4:3],
            return SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s\d+x(\d+)", 0);
        }

        public int TotalBitrate()
        {
            //Duration: 00:04:34.00, start: 0.335000, bitrate: 441 kb/s
            return SearchRegEx(@"^\s+Duration:.+,\sbitrate:\s(\d+)\skb", 0);
        }

        public int StreamBitrate(int stream)
        {
            //Stream #0:0[0x810]: Video: mpeg2video, yuv420p, 1280x720 [PAR 1:1 DAR 16:9], 18300 kb/s, 25 fps,
            return SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(\d+)\skb", 0);
        }

        public int VideoBitrate(int stream)
        {
            if (info.Length > 0)
            {
                string value = "";
                if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+Video.+,\s(\d+)\skb", out value))
                    return Convert.ToInt32(value);
                else
                {
                    //если битрейт не указан в потоке
                    int abitrate = 0, count = StreamsCount();
                    for (int snum = 0; snum < count; snum++)
                    {
                        if (snum != stream)
                            abitrate += StreamBitrate(snum);
                    }
                    return Math.Max(0, TotalBitrate() - abitrate);
                }
            }
            return 0;
        }

        public int AudioBitrate(int stream)
        {
            int value = StreamBitrate(stream);
            if (value > 0) return value;

            if (VideoStreams().Count == 0 && AudioStreams().Count == 1)
                return TotalBitrate();

            return 0;
        }

        public string StreamSAR(int stream)
        {
            //Stream #0:0: Video: h264, yuv420p, 704x416 [SAR 963:907 DAR 21186:11791], SAR 26:33 DAR 4:3, 25 fps,
            string raw = SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\[SAR\s(\d+:\d+)\s", "");
            string main = SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+SAR\s(\d+:\d+)\s", "");

            if (raw != "" && main != "" && raw != main) return main + " (" + raw + " original)";
            else if (main != "") return main;
            else if (raw != "") return raw;
            else return "Unknown";
        }

        public double CalculateSAR(int stream)
        {
            if (info.Length > 0)
            {
                string value = "";
                if (Settings.MI_Original_AR && SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\[SAR\s(\d+:\d+)\s", out value)) //В скобках [] - значение из потока
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
                if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+SAR\s(\d+:\d+)\s", out value)) //Без скобок - значение из контейнера
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
            }
            return 0;
        }

        public string StreamDAR(int stream)
        {
            //Stream #0:0: Video: h264, yuv420p, 704x416 [SAR 963:907 DAR 21186:11791], SAR 26:33 DAR 4:3, 25 fps, 25 tbr, 1k tbn, 50 tbc
            string raw = SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\sDAR\s(\d+:\d+)\]", "");
            string main = SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\sDAR\s(\d+:\d+)", "");

            if (raw != "" && main != "" && raw != main) return main + " (" + raw + " original)";
            else if (main != "") return main;
            else if (raw != "") return raw;
            else return "Unknown";
        }

        public string StreamDARSelected(int stream)
        {
            //Stream #0:0: Video: h264, yuv420p, 704x416 [SAR 963:907 DAR 21186:11791], SAR 26:33 DAR 4:3, 25 fps, 25 tbr, 1k tbn, 50 tbc
            string dar = "";
            if (Settings.MI_Original_AR && SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\sDAR\s(\d+:\d+)\]", out dar)) return dar;
            if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\sDAR\s(\d+:\d+)", out dar)) return dar;
            return dar;
        }

        public double CalculateDAR(int stream)
        {
            if (info.Length > 0)
            {
                string value = "";
                if (Settings.MI_Original_AR && SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\sDAR\s(\d+:\d+)\]", out value)) //В скобках [] - значение из потока
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
                if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\sDAR\s(\d+:\d+)", out value)) //Без скобок - значение из контейнера
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
            }
            return 0;
        }

        public string Timeline()
        {
            //Duration: 00:03:16.70, start: 0.440000,
            return SearchRegEx(@"^\s+Duration:\s(\S+),", "Unknown");
        }

        public TimeSpan Duration()
        {
            string value = ""; //Duration: 00:03:16.70, start: 0.440000,
            if (SearchRegEx(@"^\s+Duration:\s(\d+:\d+:\d+\.?\d*),", out value))
            {
                TimeSpan time;
                TimeSpan.TryParse(value, out time);
                return time;
            }
            return TimeSpan.Zero;
        }

        //Разрядность на выходе FFmpeg-декодера (а не в исходнике!)
        public int StreamBits(int stream)
        {
            string value = "";
            //Stream #0:1: Audio: mp3, 44100 Hz, 2 channels, s16(p),
            //Stream #0:0: Audio: wmapro, 44100 Hz, stereo, flt, (flt=32, dbl=64)
            //Stream #0:1(und): Audio: pcm_s24le (lpcm / 0x6D63706C), 48000 Hz, 5.1(side), s32 (24 bit), 6912 kb/s (default)
            if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+\((\d{1,2})\sbit\)", out value)) return Convert.ToInt32(value);
            if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s[su](\d{1,2})([\spf,]|$)", out value)) return Convert.ToInt32(value);
            if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(flt)", out value)) return 32;
            if (SearchRegEx(@"^\s+Stream\s\#0:" + stream + @"\D.+,\s(dbl)", out value)) return 64;
            return 0;
        }
    }
}
