using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Globalization;

namespace XviD4PSP
{
    static class Calculate
    {
        public static bool ContainsInStringArray(string[] list, string value) //Не используется
        {
            foreach (string v in list)
            {
                if (v == value)
                    return true;
            }
            return false;
        }

        public static int MakeFAT32BluRay(string path)
        {
            int fixedfiles = 0;

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (Path.GetFileName(file) == "MovieObject.bdmv")
                {
                    File.Move(file, Path.GetDirectoryName(file) + "\\MOVIEOBJ.BDM");
                    fixedfiles++;
                }
                else if (Path.GetExtension(file) == ".bdmv")
                {
                    File.Move(file, Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file).ToUpper() + ".BDM");
                    fixedfiles++;
                }
                else if (Path.GetExtension(file) == ".mpls")
                {
                    File.Move(file, Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file).ToUpper() + ".MPL");
                    fixedfiles++;
                }
                else if (Path.GetExtension(file) == ".clpi")
                {
                    File.Move(file, Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file).ToUpper() + ".CPI");
                    fixedfiles++;
                }
                else if (Path.GetExtension(file) == ".m2ts")
                {
                    File.Move(file, Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file).ToUpper() + ".MTS");
                    fixedfiles++;
                }
            }
            return fixedfiles;
        }

        public static long GetFolderSize(string path)
        {
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            long size = 0;
            foreach (string f in files)
            {
                FileInfo finfo = new FileInfo(f);
                size += finfo.Length;
            }
            return size;
        }

        public static string GetRegexValue(string pattern, string value)
        {
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(value);
            if (m.Success)
                return m.Groups[1].Value;
            else
                return null;
        }

        public static string EncodingModeEnumToString(Settings.EncodingModes mode)
        {
            if (mode == Settings.EncodingModes.OnePass) return "1-Pass Bitrate";
            else if (mode == Settings.EncodingModes.TwoPass) return "2-Pass Bitrate";
            else if (mode == Settings.EncodingModes.ThreePass) return "3-Pass Bitrate";
            else if (mode == Settings.EncodingModes.OnePassSize) return "1-Pass Size";
            else if (mode == Settings.EncodingModes.TwoPassSize) return "2-Pass Size";
            else if (mode == Settings.EncodingModes.ThreePassSize) return "3-Pass Size";
            else if (mode == Settings.EncodingModes.Quality) return "Constant Quality";
            else if (mode == Settings.EncodingModes.Quantizer) return "Constant Quantizer";
            else if (mode == Settings.EncodingModes.TwoPassQuality) return "2-Pass Quality";
            else if (mode == Settings.EncodingModes.ThreePassQuality) return "3-Pass Quality";
            return null; //null, чтоб сработала защита от пустого профиля
        }

        public static Settings.EncodingModes EncodingModeStringToEnum(string mode) //Не используется
        {
            if (mode == "1-Pass Bitrate") return Settings.EncodingModes.OnePass;
            else if (mode == "2-Pass Bitrate") return Settings.EncodingModes.TwoPass;
            else if (mode == "3-Pass Bitrate") return Settings.EncodingModes.ThreePass;
            else if (mode == "1-Pass Size") return Settings.EncodingModes.OnePassSize;
            else if (mode == "2-Pass Size") return Settings.EncodingModes.TwoPassSize;
            else if (mode == "3-Pass Size") return Settings.EncodingModes.ThreePassSize;
            else if (mode == "Constant Quality") return Settings.EncodingModes.Quality;
            else if (mode == "Constant Quantizer") return Settings.EncodingModes.Quantizer;
            else if (mode == "2-Pass Quality") return Settings.EncodingModes.TwoPassQuality;
            else if (mode == "3-Pass Quality") return Settings.EncodingModes.ThreePassQuality;
            return Settings.EncodingModes.OnePass;
        }

        public static int GetGCD(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a = a % b;
                else
                    b = b % a;
            }
            if (a == 0)
                return b;
            else
                return a;
        }

        public static string ExplainChannels(int channels)
        {
            if (channels == 0) return "Silence";
            else if (channels == 1) return "Mono";
            else if (channels == 2) return "Stereo";
            else if (channels == 3) return "Stereo LFE";
            else if (channels == 4) return "Quadro";
            else if (channels == 5) return "5 channels";
            else if (channels == 6) return "6 channels";
            else if (channels == 7) return "7 channels";
            else return "Unknown";
        }

        public static Massive UpdateOutFrames(Massive m)
        {
            if (m.frameratemodifer == AviSynthScripting.FramerateModifers.AssumeFPS)
            {
                //AssumeFPS не меняет число кадров, но его меняют деинтерлейсеры
                if (m.deinterlace == DeinterlaceType.TIVTC || m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI ||
                    m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI)
                {
                    //Для "деинтерлейсеров", делящих fps на 1.25
                    m.outframes = Convert.ToInt32((Calculate.ConvertStringToDouble(m.inframerate) / 1.25) * m.induration.TotalSeconds);
                }
                else if (m.deinterlace == DeinterlaceType.TDecimate ||
                    m.deinterlace == DeinterlaceType.TDecimate_23)
                {
                    //Для "деинтерлейсеров", возвращающих 23.976fps
                    m.outframes = Convert.ToInt32(23.976 * m.induration.TotalSeconds);
                }
                else if (m.deinterlace == DeinterlaceType.TDecimate_24)
                {
                    //TDecimate_24 возвращает 24.000fps
                    m.outframes = Convert.ToInt32(24.000 * m.induration.TotalSeconds);
                }
                else if (m.deinterlace == DeinterlaceType.TDecimate_25)
                {
                    //TDecimate_25 возвращает 25.000fps
                    m.outframes = Convert.ToInt32(25.000 * m.induration.TotalSeconds);
                }
                else if (m.deinterlace == DeinterlaceType.SmoothDeinterlace || m.deinterlace == DeinterlaceType.MCBob ||
                    m.deinterlace == DeinterlaceType.NNEDI || m.deinterlace == DeinterlaceType.YadifModEDI2 ||
                    m.deinterlace == DeinterlaceType.QTGMC_2)
                {
                    //Для деинтерлейсеров, удваивающих fps
                    m.outframes = Convert.ToInt32(Calculate.ConvertStringToDouble(m.inframerate) * 2.0 * m.induration.TotalSeconds);
                }
                else
                {
                    //Во всех остальных случаях
                    m.outframes = m.inframes;
                }
            }
            else
            {
                m.outframes = Convert.ToInt32(Calculate.ConvertStringToDouble(m.outframerate) * m.induration.TotalSeconds);
            }

            //Учитываем обрезку
            if (m.trims.Count > 0 && m.trim_is_on)
            {
                int total = m.outframes;
                m.outframes = 0;

                for (int i = 0; i < m.trims.Count; i++)
                {
                    int trim_start = Math.Max(((Trim)m.trims[i]).start, 0);
                    int trim_end = Math.Max(((Trim)m.trims[i]).end, 0);

                    trim_start = Math.Min(trim_start, total);
                    trim_end = Math.Min((trim_end == 0 ? total - 1 : trim_end), total - 1);

                    m.outframes += 1;
                    if (trim_end - trim_start > 0)
                        m.outframes += (trim_end - trim_start);
                }
            }

            //С тест-скриптом тоже что-то надо делать..
            if (m.testscript && m.outframes > 2555) m.outframes = 2555;

            //Пересчитываем duration
            m.outduration = TimeSpan.FromSeconds((double)m.outframes / Calculate.ConvertStringToDouble(m.outframerate));

            //Пересчитываем кадр для THM
            m.thmframe = m.outframes / 2;

            return m;
        }

        //Выходная частота кадров с учетом деинтерлейса, но без учета каких-либо ограничений
        public static string GetRawOutFramerate(Massive m)
        {
            if (m.deinterlace == DeinterlaceType.TIVTC || m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI ||
                m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI)
            {
                //Для "деинтерлейсеров", делящих fps на 1.25
                double inframerate = Calculate.ConvertStringToDouble(m.inframerate);
                return Calculate.ConvertDoubleToPointString(inframerate / 1.25);
            }
            else if (m.deinterlace == DeinterlaceType.TDecimate ||
                m.deinterlace == DeinterlaceType.TDecimate_23)
            {
                //Для "деинтерлейсеров", возвращающих 23.976fps
                return "23.976";
            }
            else if (m.deinterlace == DeinterlaceType.TDecimate_24)
            {
                //TDecimate_24 возвращает 24.000fps
                return "24.000";
            }
            else if (m.deinterlace == DeinterlaceType.TDecimate_25)
            {
                //TDecimate_25 возвращает 25.000fps
                return "25.000";
            }
             else if (m.deinterlace == DeinterlaceType.SmoothDeinterlace || m.deinterlace == DeinterlaceType.MCBob ||
                m.deinterlace == DeinterlaceType.NNEDI || m.deinterlace == DeinterlaceType.YadifModEDI2 ||
                m.deinterlace == DeinterlaceType.QTGMC_2)
            {
                //Для деинтерлейсеров, удваивающих fps
                double inframerate = Calculate.ConvertStringToDouble(m.inframerate);
                return Calculate.ConvertDoubleToPointString(inframerate * 2);
            }
            else
            {
                //Во всех остальных случаях
                return m.inframerate;
            }
        }

        //Выходная частота кадров с учетом деинтерлейса и ограничений форматов
        public static Massive UpdateOutFramerate(Massive m)
        {
            string rate = Calculate.GetRawOutFramerate(m);
            string[] rates = Format.GetValidFrameratesList(m);
            m.outframerate = GetClosePointDoubleFPS(rate, rates);
            return m;
        }

        public static string SplitCapString(string source) //Не используется
        {
            string newstring = "";
            int n = 0;
            char[] _chars = source.ToCharArray();
            foreach (char _char in source.ToCharArray())
            {
                if (n != 0 &&
                    Char.IsUpper(_char) &&
                    n + 1 < _chars.Length &&
                    !Char.IsUpper(_chars[n - 1]))
                {
                    newstring += " ";
                }
                newstring += _char;
                n++;
            }

            return newstring;
        }

        public static double GetProcent(int total, int current) //Не используется
        {
            return ((double)current / (double)total) * 100.0;
        }

        public static int GetProcentValue(int total, int procent)
        {
            return (int)(((double)procent / 100.0) * (double)total);
        }

        public static string GetUTF8String(string ustring)
        {
            string str = "";
            Byte[] bmas = Encoding.UTF8.GetBytes(ustring);
            foreach (Byte b in bmas)
            { str = str + String.Format("\\x{0:x}", b); }
            return str;
        }

        public static string GetQualityIn(Massive m)
        {
            int pixels = m.inresw * m.inresh;
            double framerate = ConvertStringToDouble(m.inframerate);
            int bitrate = m.invbitrate * 1000;
            return ConvertDoubleToPointString((double)bitrate / (double)pixels / framerate);
        }

        public static string GetQualityOut(Massive m, bool FromSize)
        {
            string quality = Languages.Translate("Unknown");

            int pixels = m.outresw * m.outresh;
            double framerate = Calculate.ConvertStringToDouble(m.outframerate);
            int bitrate = (int)m.outvbitrate * 1000;

            if (m.encodingmode == Settings.EncodingModes.OnePass ||
                m.encodingmode == Settings.EncodingModes.TwoPass ||
                m.encodingmode == Settings.EncodingModes.ThreePass)
            {
                quality = ConvertDoubleToPointString((double)bitrate / (double)pixels / framerate);
            }
            else if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                int abitrate = 0;
                string apath = "";
                if (m.outaudiostreams.Count > 0)
                {
                    //Мы не можем посчитать качество, т.к. не можем посчитать видео-битрет, т.к. не знаем аудио-битрейта
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.codec == "Copy" && File.Exists(instream.audiopath))
                        apath = instream.audiopath;
                    else if (outstream.bitrate == 0 && outstream.codec != "Disabled")
                        return quality;
                    else
                        abitrate = outstream.bitrate;
                }

                if (FromSize)
                {
                    if (apath != "")
                        bitrate = Calculate.GetBitrateForSize((double)m.outvbitrate, apath, (int)m.outduration.TotalSeconds, m.outvcodec, m.format) * 1000;
                    else
                        bitrate = Calculate.GetBitrateForSize((double)m.outvbitrate, abitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format) * 1000;
                }
                else
                    bitrate = (int)m.outvbitrate * 1000;

                quality = ConvertDoubleToPointString((double)bitrate / (double)pixels / framerate);
            }

            return quality;
        }

        public static int GetAutoBitrate(Massive m)
        {
            //поправка на сжимаемость кодека
            double quality = 1.0;
            if (m.outvcodec == "x264")
                quality = 0.245;
            else if (m.outvcodec == "MPEG4" ||
                m.outvcodec == "XviD")
                quality = 0.42;
            else if (m.outvcodec == "MPEG2")
                quality = 0.579;
            else if (m.outvcodec == "MPEG1")
                quality = 0.700;

            int pixels = m.outresw * m.outresh;

            double framerate;
            if (m.frameratemodifer == AviSynthScripting.FramerateModifers.AssumeFPS)
                framerate = ConvertStringToDouble(m.inframerate);
            else
                framerate = Calculate.ConvertStringToDouble(m.outframerate);

            int autobitrate = (int)((quality * (double)pixels * framerate) / 1000.0);
            int maxbitrate = Format.GetMaxVBitrate(m);

            if (autobitrate > maxbitrate)
                return maxbitrate;
            else
                return autobitrate;
        }

        public static Massive CalculateSAR(Massive m)
        {
            m.sar = CalculateSAR(m.outresw, m.outresh, m.outaspect);
            return m;
        }

        public static string CalculateSAR(int outresw, int outresh, double outaspect)
        {
            double dsar = outresw / outaspect / outresh;
            double diff, dsarX, min_diff = 100.0;
            int sarX = 1, sarY = 1, i = 1;
            while (i < 1000)
            {
                dsarX = dsar * i;
                diff = Math.Abs(dsar - Math.Round(dsarX) / i);
                if (diff < min_diff)
                {
                    min_diff = diff;
                    sarX = Convert.ToInt32(dsarX);
                    sarY = i;
                }
                i += 1;
            }
            return sarY.ToString() + ":" + sarX.ToString();
        }

        public static string[] InsertAspect(string[] aspects, string aspect)
        {
            ArrayList aspectslist = new ArrayList();
            aspectslist.AddRange(aspects);

            string closeaspect = GetClosePointDouble(aspect, aspects);
            int closeaspectindex = aspectslist.IndexOf(closeaspect);
            if (closeaspect.Contains(" "))
                closeaspect = closeaspect.Split(new string[] { " " }, StringSplitOptions.None)[0];

            double _aspect = ConvertStringToDouble(aspect);
            double _closeaspect = ConvertStringToDouble(closeaspect);
            if (_aspect < _closeaspect)
            {
                aspectslist.Insert(closeaspectindex, aspect);
            }
            else if (_aspect > _closeaspect)
            {
                if (closeaspectindex == aspectslist.Count)
                    aspectslist.Insert(closeaspectindex, aspect);
                else
                    aspectslist.Insert(closeaspectindex + 1, aspect);
            }

            return Calculate.ConvertArrayListToStringArray(aspectslist);
        }

        public static string GetClosePointDouble(string CompareValue, string[] ValuesList)
        {
            double min = double.MaxValue;
            string closest = ValuesList[0];
            double compare = ConvertStringToDouble(CompareValue);
            foreach (string value in ValuesList)
            {
                double diff = Math.Abs(compare - ConvertStringToDouble(value));
                if (diff < min)
                {
                    min = diff;
                    closest = value;
                }
            }

            return closest;
        }

        public static string GetClosePointDoubleFPS(string CompareValue, string[] ValuesList)
        {
            //Фильтруем
            bool any = false;
            ArrayList values = new ArrayList();
            foreach (string value in ValuesList)
            {
                if (value == "0.000") any = true;
                else values.Add(value);
            }

            //Если разрешены любые значения fps
            if (any && Settings.Nonstandard_fps)
            {
                if (!string.IsNullOrEmpty(CompareValue))
                {
                    //На всякий случай приводим к нужному формату
                    return Calculate.ConvertDoubleToPointString(ConvertStringToDouble(CompareValue), 3);
                }
                else if (values.Count > 1)
                    return values[0].ToString();
                else
                    return "25.000";
            }

            //Защита от пустого списка (если в нем было только "0.000")
            if (values.Count == 0)
            {
                if (!string.IsNullOrEmpty(CompareValue))
                {
                    //На всякий случай приводим к нужному формату
                    return Calculate.ConvertDoubleToPointString(ConvertStringToDouble(CompareValue), 3);
                }
                else
                    return "25.000";
            }

            double min = double.MaxValue;
            string closest = values[0].ToString();
            double compare = ConvertStringToDouble(CompareValue);
            foreach (string value in values)
            {
                double diff = Math.Abs(compare - ConvertStringToDouble(value));
                if (diff < min)
                {
                    min = diff;
                    closest = value;
                }
            }

            return closest;
        }

        public static double GetCloseDouble(double CompareValue, string[] ValuesList)
        {
            double min = double.MaxValue;
            double closest = ConvertStringToDouble(ValuesList[0]);
            foreach (string value in ValuesList)
            {
                double dvalue = ConvertStringToDouble(value);
                double diff = Math.Abs(CompareValue - dvalue);
                if (diff < min)
                {
                    min = diff;
                    closest = dvalue;
                }
            }

            return closest;
        }

        public static string GetCloseInteger(string CompareValue, string[] ValuesList)
        {
            int min = int.MaxValue;
            string closest = ValuesList[0];
            int compare = Convert.ToInt32(CompareValue);
            foreach (string value in ValuesList)
            {
                int diff = Math.Abs(compare - Convert.ToInt32(value));
                if (diff < min)
                {
                    min = diff;
                    closest = value;
                }
            }

            return closest;
        }

        public static int GetCloseIntegerAL(int CompareValue, ArrayList ValuesList)
        {
            int min = int.MaxValue;
            int closest = Convert.ToInt32(ValuesList[0]);
            foreach (int value in ValuesList)
            {
                int diff = Math.Abs(CompareValue - value);
                if (diff < min)
                {
                    min = diff;
                    closest = value;
                }
            }

            return closest;
        }

        public static string GetEncodingSize(Massive m)
        {
            string ssize = Languages.Translate("Unknown");

            if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
                m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                m.encodingmode == Settings.EncodingModes.OnePassSize)
            {
                if (m.outaudiostreams.Count > 0)
                {
                    //Мы не можем знать размер если звук = VBR или Copy, но файл еще не извлечен и битрейт неизвестен
                    AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                    AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    if (outstream.bitrate == 0 && outstream.codec != "Disabled")
                    {
                        //Если звук будет кодироваться первым, то размер звукового файла уже будет известен к моменту начала
                        //кодирования видео (кроме случая DirectRemux для Copy), и его можно будет учесть
                        if (outstream.codec == "Copy" && !(Settings.EncodeAudioFirst && !Format.IsDirectRemuxingPossible(m))
                            && !File.Exists(instream.audiopath))
                        {
                            //Copy
                            return ssize;
                        }
                        else if (outstream.codec != "Copy" && !Settings.EncodeAudioFirst)
                        {
                            //VBR
                            return ssize;
                        }
                    }
                }
                return m.outvbitrate + " mb";
            }
            else if (m.format == Format.ExportFormats.Audio && m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                if (outstream.bitrate == 0) return ssize; //"Unknown" для VBR
                double outsize = (0.125261 * (double)outstream.bitrate * (double)m.outduration.TotalSeconds) / 1052.0 / 0.994;
                ssize = Calculate.ConvertDoubleToPointString(outsize, 1) + " mb";
            }
            else
            {
                if (m.encodingmode == Settings.EncodingModes.OnePass ||
                    m.encodingmode == Settings.EncodingModes.TwoPass ||
                    m.encodingmode == Settings.EncodingModes.ThreePass)
                {
                    double outsize = (0.1258 * (double)m.outvbitrate * (double)m.outduration.TotalSeconds) / 1052.0 / 0.994;
                    if (m.outaudiostreams.Count > 0)
                    {
                        //Мы не можем знать размер если звук = VBR или Copy, но файл еще не извлечен и битрейт неизвестен
                        AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                        AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                        if (outstream.codec == "Copy" && File.Exists(instream.audiopath))
                            outsize += (new FileInfo(instream.audiopath).Length / 1000) / 1052.0 / 0.994;
                        else if (outstream.bitrate == 0 && outstream.codec != "Disabled")
                            return ssize;
                        else
                            outsize += (0.125261 * (double)outstream.bitrate * (double)m.outduration.TotalSeconds) / 1052.0 / 0.994;
                    }

                    //TS M2TS BluRay packet size
                    if (m.format == Format.ExportFormats.M2TS ||
                        m.format == Format.ExportFormats.TS)
                        outsize *= 1.03;
                    if (m.format == Format.ExportFormats.BluRay)
                        outsize *= 1.05;

                    ssize = Calculate.ConvertDoubleToPointString(outsize, 1) + " mb";

                    if (Format.Is4GBlimitedFormat(m) && outsize > 4000)
                    {
                        Message mess = new Message(App.Current.MainWindow);
                        mess.ShowMessage(ssize + " - " + Languages.Translate("exceed maximum file size for format") + " " + Format.EnumToString(m.format) + "!",
                            Languages.Translate("Warning"), Message.MessageStyle.Ok);
                    }
                }
            }
            return ssize;
        }

        public static int GetBitrateForSize(double targetsize, int abitrate, int seconds, string outvcodec, Format.ExportFormats outformat)
        {
            //TS M2TS BluRay packet size
            if (outformat == Format.ExportFormats.M2TS ||
                outformat == Format.ExportFormats.TS)
                targetsize /= 1.03;
            if (outformat == Format.ExportFormats.BluRay)
                targetsize /= 1.05;

            int bitrate = 0;
            double kbSizeV = (double)targetsize * 1052.0;
            double kbSizeA = 0.1258 * (double)abitrate * (double)seconds;
            kbSizeV = (kbSizeV - kbSizeA) * 0.994;
            bitrate = Convert.ToInt32(kbSizeV / 0.125261 / (double)seconds);

            ////TS M2TS BluRay packet size
            //if (outformat == Format.ExportFormats.M2TS ||
            //    outformat == Format.ExportFormats.TS ||
            //    outformat == Format.ExportFormats.BluRay)
            //    bitrate = Convert.ToInt32((double)bitrate / 1.05);

            return bitrate;
        }

        public static int GetBitrateForSize(double targetsize, string audiopath, int seconds, string outvcodec, Format.ExportFormats outformat)
        {
            //TS M2TS BluRay packet size
            if (outformat == Format.ExportFormats.M2TS ||
                outformat == Format.ExportFormats.TS)
                targetsize /= 1.03;
            if (outformat == Format.ExportFormats.BluRay)
                targetsize /= 1.07;

            int bitrate = 0;
            double kbSizeV = (double)targetsize * 1052.0;

            FileInfo info = new FileInfo(audiopath);
            double kbSizeA = info.Length / 1000.0;

            kbSizeV = (kbSizeV - kbSizeA) * 0.994;
            bitrate = Convert.ToInt32(kbSizeV / 0.125261 / (double)seconds);

            ////TS M2TS BluRay packet size
            //if (outformat == Format.ExportFormats.M2TS ||
            //    outformat == Format.ExportFormats.TS ||
            //    outformat == Format.ExportFormats.BluRay)
            //    bitrate = Convert.ToInt32((double)bitrate / 1.05);

            return bitrate;
        }

        public static bool IsMPEG(string FilePath)
        {
            string[] x = new string[] { ".vob", ".ts", ".mpg", ".mpe", ".mpeg", ".m2p", ".m2t", ".m2v", ".mod", ".d2v", ".m2ts", ".vro" };
            foreach (string ext in x)
            {
                if (ext == Path.GetExtension(FilePath).ToLower())
                    return true;
            }
            return false;
        }

        public static bool IsValidVOBName(string vobpath)
        {
            //это точно не воб
            if (Path.GetExtension(vobpath).ToLower() != ".vob") return false;

            string pat = @"^VTS_(\d\d)_(\d\d?)$"; //Не более 2-х цифр в конце
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(Path.GetFileNameWithoutExtension(vobpath));
            if (m.Success)
                return true;
            else if (Path.GetFileName(vobpath).ToUpper() == "VIDEO_TS.VOB")
                return true;
            else
                return false;
        }

        public static string GetIFO(string filepath)
        {
            string ifo = filepath.Substring(0, filepath.Length - 5) + "0.IFO";
            if (File.Exists(ifo))
                return ifo;
            else
                return null;
        }

        public static string GetBestIndexFile(string infilepath)
        {
            string title = "";
            if (IsValidVOBName(infilepath))
            {
                title = GetTitleNum(infilepath);
                if (!string.IsNullOrEmpty(title))
                    title = "_T" + title;
            }

            string indexpath;
            string dvdname = GetDVDName(infilepath);

            //Куда помещать индекс-папку
            if (IsReadOnly(infilepath) || Settings.DGIndexInTemp)
            {
                //В Temp-папку
                indexpath = Settings.TempPath + "\\" + dvdname + ".index\\" + dvdname + title + ".d2v";
            }
            else
            {
                //Рядом с исходником (если это ДВД, то имеет смысл изменить имя папки на более короткое)
                indexpath = Path.GetDirectoryName(infilepath) + "\\" + ((title.Length > 0 && dvdname.Length > 10) ? "DGIndex" : dvdname) +
                    ".index\\" + dvdname + title + ".d2v";
            }

            //Проверяем длину пути, если есть превышение - вылезет Exception с красивым сообщением :)
            if (Settings.EnableAudio)
                title = Path.GetDirectoryName(indexpath + "_extra_characters_for_audio_info"); //+32
            else
                title = Path.GetDirectoryName(indexpath);

            return indexpath;
        }

        public static bool IsReadOnly(string filepath)
        {
            DirectoryInfo di = new DirectoryInfo(filepath);
            if (di.Attributes == (di.Attributes | FileAttributes.ReadOnly))
                return true;
            else
                return false;
        }

        public static string GetDVDName(string infilepath)
        {
            if (IsValidVOBName(infilepath))
            {
                string dvdpath = Path.GetDirectoryName(infilepath);
                string dvdname = Path.GetFileName(dvdpath);
                if (dvdname.Equals("VIDEO_TS", StringComparison.InvariantCultureIgnoreCase))
                {
                    dvdpath = Path.GetDirectoryName(dvdpath);
                    dvdname = Path.GetFileName(dvdpath);
                }

                //проверка на задание в корне
                if (dvdpath == Path.GetPathRoot(infilepath))
                {
                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                        if (drive.DriveType == DriveType.CDRom &&
                            drive.IsReady &&
                            drive.Name == dvdpath)
                            dvdname = drive.VolumeLabel;
                }

                //проверка на безымянный диск
                if (dvdname == "")
                    dvdname = "DVD";

                return dvdname;
            }
            else
                return Path.GetFileNameWithoutExtension(infilepath);
        }

        public static string GetTitleNum(string infilepath)
        {
            string pat = @"VTS_(\d\d)_(\d)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(Path.GetFileNameWithoutExtension(infilepath));
            if (m.Success == true)
                return m.Groups[1].Value;
            else
                return null;
        }

        public static string GetTimeline(double duration)
        {
            TimeSpan dts = TimeSpan.FromSeconds(duration);
            return dts.Hours.ToString("00") + ":" +
                dts.Minutes.ToString("00") + ":" +
                dts.Seconds.ToString("00") + ":" +
                dts.Milliseconds.ToString("000");
        }

        public static string StartupPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);
            }
        }

        public static string DecimalSeparator
        {
            get
            {
                return System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            }
        }

        public static int GetDelay(string audiopath)
        {
            if (audiopath == null)
                return 0;

            int delay = 0;
            string pat = @"DELAY\s(-?\d+)ms";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(Path.GetFileNameWithoutExtension(audiopath));
            if (m.Success)
            {
                delay = Convert.ToInt32(m.Groups[1].Value);
            }
            return delay;
        }

        public static int GetIntFromString(string value) //Не используется
        {
            string pat = @"(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(value);
            if (m.Success == true)
                return Convert.ToInt32(m.Groups[1].Value);
            return 0;
        }

        public static bool IsValid(int value, int validation) //Не используется
        {
            string testS = Convert.ToString(Convert.ToDouble(value) / validation);
            if (testS.Contains(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) == true)
                return false;
            else
                return true;
        }

        public static int GetValid(int value, int validation)
        {
            double n = (double)value / (double)validation;
            int z = Convert.ToInt32(n) * validation;
            return z;
        }

        public static int GetSplittedValue(string value, int position) //Не используется
        {
            //string[] separator = new string[] { "x" };
            string[] separator;
            if (value.Contains(":") == true)
                separator = new string[] { ":" };
            else if (value.Contains("x") == true)
                separator = new string[] { "x" };
            else
                separator = new string[] { "x" };
            string[] a = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            return Convert.ToInt32(a[position - 1]);
        }

        public static string GetSplittedString(string value, int position)
        {
            //string[] separator = new string[] { "x" };
            string[] separator;
            if (value.Contains(":"))
                separator = new string[] { ":" };
            else if (value.Contains("x"))
                separator = new string[] { "x" };
            else
                separator = new string[] { " " };
            string[] a = value.Split(separator, StringSplitOptions.None);
            return a[position];
        }

        public static string CheckScriptErrors(Massive m)
        {
            string er = null;

            AviSynthReader reader = new AviSynthReader();

            try
            {
                reader.ParseScript(m.script);
            }
            catch (Exception ex)
            {
                er = ex.Message;
            }

            reader.Close();
            reader = null;

            return er;
        }

        public static string ConvertDoubleToPointString(double value)
        {
            string svalue = value.ToString("0.000");
            if (svalue.Contains(","))
                svalue = svalue.Replace(",", ".");
            return svalue;
        }

        public static string ConvertDoubleToPointString(double value, int decimalplaces)
        {
            string aspect = value.ToString("0.0");
            if (decimalplaces == 2) aspect = value.ToString("0.00");
            else if (decimalplaces == 3) aspect = value.ToString("0.000");
            else if (decimalplaces == 4) aspect = value.ToString("0.0000");
            if (aspect.Contains(",")) aspect = aspect.Replace(",", ".");
            return aspect;
        }

        public static double ConvertStringToDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0.0;

            if (value.Contains(".") && "." != DecimalSeparator)
                value = value.Replace(".", DecimalSeparator);
            if (value.Contains(",") && "," != DecimalSeparator)
                value = value.Replace(",", DecimalSeparator);
            if (value.Contains(" "))
                value = value.Split(new string[] { " " }, StringSplitOptions.None)[0];

            double dvalue = 0.0;
            Double.TryParse(value, NumberStyles.Float, null, out dvalue);
            return dvalue;
        }

        public static string[] ConvertArrayListToStringArray(ArrayList collection)
        {
            return (string[])collection.ToArray(typeof(string));
        }

        public static ArrayList ConvertStringArrayToArrayList(string[] collection)
        {
            ArrayList newArray = new ArrayList();
            newArray.AddRange(collection);
            return newArray;
        }

        public static string RemoveExtention(string filepath)
        {
            string ext = Path.GetExtension(filepath);
            return filepath.Substring(0, filepath.Length - ext.Length + 1);
        }

        public static string RemoveExtention(string filepath, bool removedot)
        {
            int n = 0;
            if (removedot == true)
                n = -1;
            string ext = Path.GetExtension(filepath);
            return filepath.Substring(0, filepath.Length - ext.Length + 1 + n);
        }

        public static string GetShortPath(string path)
        {
            if (Path.GetDirectoryName(path).Length > 10)
                return Path.GetPathRoot(path) + "...\\" + Path.GetFileName(path);
            return path;
        }

        public static bool ValidatePath(string path, bool throw_if_illegal)
        {
            if (Settings.ValidatePathes)
            {
                //Наша дефолтная кодировка
                Encoding encoding = Encoding.Default;

                //Чтоб определить наличие "нехороших" символов в пути, прогоняем его через нашу кодировку
                string reencoded = encoding.GetString(encoding.GetBytes(path));
                if (path != reencoded)
                {
                    if (throw_if_illegal)
                    {
                        //Выделяем "нехорошие" символы
                        string characters = ":\r\n\r\n";
                        ArrayList chars = new ArrayList();
                        if (path.Length == reencoded.Length)
                        {
                            char[] _in = path.ToCharArray();
                            char[] _out = reencoded.ToCharArray();

                            for (int i = 0; i < _in.Length; i++)
                            {
                                if (_in[i] != _out[i] && !chars.Contains(_in[i])) chars.Add(_in[i]);
                            }

                            chars.Sort();
                            foreach (char ch in chars)
                            {
                                characters += Char.ConvertFromUtf32(ch) + " ";
                            }
                        }
                        else
                            characters = ".";

                        throw new Exception(Languages.Translate("The path contains characters not supported by the current code page") +
                            " (" + encoding.CodePage + ")" + characters);
                    }
                    return false;
                }
            }
            return true;
        }

        public static string WrapScript(string script, int max_length)
        {
            //Перенос длинных строчек
            string result = "\r\n\r\n   -------\r\n";
            string[] lines = (script.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            foreach (string line in lines)
            {
                int index = 0;
                while (index <= line.Length)
                {
                    int length = Math.Min(max_length, line.Length - index);
                    result += "\r\n   " + line.Substring(index, length);
                    index += max_length;
                }
            }
            return result;
        }
    }
}
