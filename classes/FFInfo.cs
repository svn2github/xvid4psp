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
        private static object locker = new object();
        private Process encoderProcess = null;
        private string[] global_lines = null;
        private bool was_killed = false;
        public string info = null;

        public void Open(string filepath)
        {
            encoderProcess = new Process();
            ProcessStartInfo pinfo = new ProcessStartInfo();

            pinfo.FileName = Calculate.StartupPath + "\\apps\\ffmpeg\\ffmpeg.exe";
            pinfo.WorkingDirectory = Path.GetDirectoryName(pinfo.FileName);
            pinfo.UseShellExecute = false;
            pinfo.RedirectStandardOutput = false;
            pinfo.RedirectStandardError = true;
            pinfo.CreateNoWindow = true;

            pinfo.Arguments = "-i \"" + filepath + "\"";

            encoderProcess.StartInfo = pinfo;
            encoderProcess.Start();

            //Ждём не более 10-ти секунд (для скриптов ждем чуть дольше)
            int time = (Path.GetExtension(filepath).ToLower() == ".avs") ? 100000 : 10000;
            encoderProcess.WaitForExit(time);

            if (encoderProcess == null) return;
            else if (!encoderProcess.HasExited)
            {
                was_killed = true;
                encoderProcess.Kill();
                encoderProcess.WaitForExit();
            }

            //Читаем и сразу делим на строки (чтоб не делать одну и ту же работу по 10 раз)
            info = encoderProcess.StandardError.ReadToEnd();
            if (was_killed) info += "\r\nFFInfo: The waiting period has exceeded a given value of " + time + "ms. Aborted!";
            if (info != null) global_lines = info.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
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

            info = null;
            global_lines = null;
        }

        private bool SearchRegEx(string pattern, out string value)
        {
            value = "";
            if (info == null) return false;

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
            if (info == null) return _default;

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
            if (info == null) return _default;

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
            if (info != null)
            {
                Regex r = new Regex(@"^\s+Stream\s\#0\.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
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
            return SearchRegEx(@"^\s+Stream\s\#0\.(\d+).+Video:", 0);
        }

        //Список ID всех видео треков
        public ArrayList VideoStreams()
        {
            ArrayList v_streams = new ArrayList();
            if (info != null)
            {
                //Stream #0.0[0x1e0]: Video:
                Regex r = new Regex(@"^\s+Stream\s\#0\.(\d+).+Video:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
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
            return SearchRegEx(@"^\s+Stream\s\#0\.(\d+).+Audio:", 0);
        }

        //Список ID всех аудио треков
        public ArrayList AudioStreams()
        {
            ArrayList a_streams = new ArrayList();
            if (info != null)
            {
                //Stream #0.1[0x1c0]: Audio:
                Regex r = new Regex(@"^\s+Stream\s\#0\.(\d+).+Audio:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                foreach (string line in global_lines)
                {
                    Match m = r.Match(line);
                    if (m.Success) a_streams.Add(Convert.ToInt32(m.Groups[1].Value));
                }
            }
            return a_streams;
        }

        //Полностью вся строка для трека
        public string StreamFull(int stream)
        {
            return SearchRegEx(@"^\s+(Stream\s\#0\." + stream + @"\D.+)", "Unknown");
        }

        public string StreamLanguage(int stream)
        {
            string value = ""; //Stream #0.1[0x1100](HUN): Audio:
            if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @".*\((\D+)\):", out value))
            {
                value = value.ToLower();
                if (value == "ara") return "Arabic";
                else if (value == "arm" || value == "hye") return "Armenian";
                else if (value == "aus") return "Australian";
                else if (value == "bel") return "Belarusian";
                else if (value == "bul") return "Bulgarian";
                else if (value == "cze" || value == "ces") return "Czech";
                else if (value == "chi" || value == "zho") return "Chinese";
                else if (value == "dan") return "Danish";
                else if (value == "dut" || value == "nld") return "Dutch";
                else if (value == "ger" || value == "deu") return "German";
                else if (value == "eng" || value == "ang") return "English";
                else if (value == "est") return "Estonian";
                else if (value == "fin") return "Finnish";
                else if (value == "fre" || value == "fra") return "French";
                else if (value == "heb") return "Hebrew";
                else if (value == "hun") return "Hungarian";
                else if (value == "ita") return "Italian";
                else if (value == "jpn") return "Japanese";
                else if (value == "kor") return "Korean";
                else if (value == "lat") return "Latin";
                else if (value == "lav") return "Latvian";
                else if (value == "lit") return "Lithuanian";
                else if (value == "mul") return "Multiple";
                else if (value == "pol") return "Polish";
                else if (value == "por") return "Portuguese";
                else if (value == "rum" || value == "ron") return "Romanian";
                else if (value == "rus") return "Russian";
                else if (value == "spa") return "Spanish";
                else if (value == "swe") return "Swedish";
                else if (value == "tur") return "Turkish";
                else if (value == "ukr") return "Ukrainian";
                else if (value == "und") return "Unknown";
                else return value;
            }
            return "Unknown";
        }

        public string StreamCodec(int stream)
        {
            //Stream #0.0[0x1e0]: Video: mpeg2video, yuv420p,
            //Stream #0.1[0x1c0]: Audio: mp2, 48000 Hz,
            //Stream #0.0: Video: h264 (Constrained Baseline),
            //Stream #0.0: Video: SVQ3 / 0x33515653,
            return SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+:\s(\w+)[\s,]", "Unknown");
        }

        public string StreamCodecShort(int stream)
        {
            string value = ""; //Stream #0.0[0x1e0]: Video: mpeg2video, yuv420p,
            if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+:\s(\w+)[\s,]", out value))
            {
                if (value == "liba52") return "AC3";
                else if (value == "mpeg4aac") return "AAC";
                else if (value.Contains("pcm") || value.Contains("s16")) return "PCM";
                else if (value.Contains("wma")) return "WMA";
                else if (value == "dca") return "DTS";
                return value.ToUpper();
            }
            return "Unknown";
        }

        public string StreamColor(int stream)
        {
            //Stream #0.0[0x1e0]: Video: mpeg2video, yuv420p,
            //Stream #0.0: Video: h264 (Constrained Baseline), yuv420p,
            return SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+Video:\s.+?,\s(\w+),", "Unknown");
        }

        public string StreamSamplerate(int stream)
        {
            //Stream #0.1[0x1c0]: Audio: mp2, 48000 Hz, 2 channels,
            return SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s(\d+)\sHz", "Unknown");
        }

        public int StreamChannels(int stream)
        {
            if (info != null)
            {
                string value = "";
                //Stream #0.1[0x1c0]: Audio: mp2, 48000 Hz, 2 channels,
                if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+Hz,\s(\d)\schannel", out value)) //2 channels, 3 channels
                {
                    return Convert.ToInt32(value);
                }
                //Stream #0.1[0x80]: Audio: ac3, 48000 Hz, 5.1,
                if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+Hz,\s(\d\.\d)", out value)) //5.1, 2.1
                {
                    string[] values = value.Split(new string[] { "." }, StringSplitOptions.None);
                    return Convert.ToInt32(values[0]) + Convert.ToInt32(values[1]);
                }
                //Stream #0.1[0x80]: Audio: ac3, 48000 Hz, stereo,
                if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+Hz,\s(\w+)", out value)) //mono, stereo
                {
                    if (value == "mono") return 1;
                    if (value == "stereo") return 2;
                }
            }
            return 0;
        }

        public string StreamFramerate(int stream)
        {
            string value = ""; //Stream #0.0: Video: mpeg2video, yuv420p, 720x576 [PAR 16:15 DAR 4:3], 9500 kb/s, 25 fps, 25 tbr, 90k tbn, 50 tbc
            if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s(\d+.?\d*)\stbr", out value))
            {
                if (value == "23.98") return "23.976";
                return Calculate.ConvertDoubleToPointString(Calculate.ConvertStringToDouble(value));
            }
            return "";
        }

        public int StreamW(int stream)
        {
            //Stream #0.0[0x1e0]: Video: mpeg2video, yuv420p, 720x576 [PAR 16:15 DAR 4:3],
            return SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s(\d+)x", 0);
        }

        public int StreamH(int stream)
        {
            //Stream #0.0[0x1e0]: Video: mpeg2video, yuv420p, 720x576 [PAR 16:15 DAR 4:3],
            return SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s\d+x(\d+)", 0);
        }

        public int StreamBitrate(int stream)
        {
            //Stream #0.0[0x810]: Video: mpeg2video, yuv420p, 1280x720 [PAR 1:1 DAR 16:9], 18300 kb/s, 25 fps,
            return SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s(\d+)\skb", 0);
        }

        public int VideoBitrate(int stream)
        {
            if (info != null)
            {
                string value = "";
                if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+Video.+,\s(\d+)\skb", out value))
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

        public int TotalBitrate()
        {
            //Duration: 00:04:34.00, start: 0.335000, bitrate: 441 kb/s
            return SearchRegEx(@"Duration:.+,\sbitrate:\s(\d+)\skb", 0);
        }

        public string StreamPAR(int stream)
        {
            //Stream #0.0: Video: h264, yuv420p, 704x416 [PAR 963:907 DAR 21186:11791], PAR 26:33 DAR 4:3, 25 fps,
            string raw = SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\[PAR\s(\d+:\d+)\s", "");
            string main = SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+PAR\s(\d+:\d+)\s", "");

            if (raw != "" && main != "" && raw != main) return main + " (" + raw + " original)";
            else if (main != "") return main;
            else if (raw != "") return raw;
            else return "Unknown";
        }

        public double CalculatePAR(int stream)
        {
            if (info != null)
            {
                string value = "";
                if (Settings.MI_Original_AR && SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\[PAR\s(\d+:\d+)\s", out value)) //В скобках [] - значение из потока
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
                if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+PAR\s(\d+:\d+)\s", out value)) //Без скобок - значение из контейнера
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
            }
            return 0;
        }

        public string StreamDAR(int stream)
        {
            //Stream #0.0: Video: h264, yuv420p, 704x416 [PAR 963:907 DAR 21186:11791], PAR 26:33 DAR 4:3, 25 fps, 25 tbr, 1k tbn, 50 tbc
            string raw = SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\sDAR\s(\d+:\d+)\]", "");
            string main = SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\sDAR\s(\d+:\d+)", "");

            if (raw != "" && main != "" && raw != main) return main + " (" + raw + " original)";
            else if (main != "") return main;
            else if (raw != "") return raw;
            else return "Unknown";
        }

        public string StreamDARSelected(int stream)
        {
            //Stream #0.0: Video: h264, yuv420p, 704x416 [PAR 963:907 DAR 21186:11791], PAR 26:33 DAR 4:3, 25 fps, 25 tbr, 1k tbn, 50 tbc
            string dar = "";
            if (Settings.MI_Original_AR && SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\sDAR\s(\d+:\d+)\]", out dar)) return dar;
            if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\sDAR\s(\d+:\d+)", out dar)) return dar;
            return dar;
        }

        public double CalculateDAR(int stream)
        {
            if (info != null)
            {
                string value = "";
                if (Settings.MI_Original_AR && SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\sDAR\s(\d+:\d+)\]", out value)) //В скобках [] - значение из потока
                {
                    string[] results = value.Split(':');
                    return Convert.ToDouble(results[0]) / Convert.ToDouble(results[1]);
                }
                if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+\sDAR\s(\d+:\d+)", out value)) //Без скобок - значение из контейнера
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
            return SearchRegEx(@"Duration:\s(\S+),", "Unknown");
        }

        public TimeSpan Duration()
        {
            string value = ""; //Duration: 00:03:16.70, start: 0.440000,
            if (SearchRegEx(@"Duration:\s(\d+:\d+:\d+\.?\d*),", out value))
            {
                TimeSpan time;
                TimeSpan.TryParse(value, out time);
                return time;
            }
            return TimeSpan.Zero;
        }

        public int StreamBits(int stream)
        {
            string value = "";
            //Stream #0.1: Audio: mp3, 44100 Hz, 2 channels, s16,
            //Stream #0.0: Audio: wmapro, 44100 Hz, stereo, flt, - 24бит, какие еще буквы могут быть?
            if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s[su](\d{1,3})", out value)) return Convert.ToInt32(value);
            if (SearchRegEx(@"^\s+Stream\s\#0\." + stream + @"\D.+,\s(flt)", out value)) return 24;
            return 0;
        }
    }
}
