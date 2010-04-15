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

       public static bool ContainsInStringArray(string[] list, string value)
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

       public static Settings.EncodingModes EncodingModeStringToEnum(string mode)
       {
           if (mode == "1-Pass Bitrate")
               return Settings.EncodingModes.OnePass;
           if (mode == "2-Pass Bitrate")
               return Settings.EncodingModes.TwoPass;
           if (mode == "3-Pass Bitrate")
               return Settings.EncodingModes.ThreePass;

           if (mode == "1-Pass Size")
               return Settings.EncodingModes.OnePassSize;
           if (mode == "2-Pass Size")
               return Settings.EncodingModes.TwoPassSize;
           if (mode == "3-Pass Size")
               return Settings.EncodingModes.ThreePassSize;

           if (mode == "Constant Quality")
               return Settings.EncodingModes.Quality;
           if (mode == "Constant Quantizer")
               return Settings.EncodingModes.Quantizer;

           if (mode == "2-Pass Quality")
               return Settings.EncodingModes.TwoPassQuality;
           if (mode == "3-Pass Quality")
               return Settings.EncodingModes.ThreePassQuality;

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
           if (channels == 0)
               return "Silence";
           else if (channels == 1)
               return "Mono";
           else if (channels == 2)
               return "Stereo";
           else if (channels == 3)
               return "Stereo LFE";
           else if (channels == 4)
               return "Quadro";
           else if (channels == 5)
               return "5 channels";
           else if (channels == 6)
               return "6 channels";
           else if (channels == 7)
               return "7 channels";
           else
               return "Unknown";
       }

       public static Massive UpdateOutFrames(Massive m)
       {
           if (m.frameratemodifer == AviSynthScripting.FramerateModifers.AssumeFPS)
           {
               //AssumeFPS не меняет число кадров, но его меняют деинтерлейсеры
               if (m.deinterlace == DeinterlaceType.TIVTC || m.deinterlace == DeinterlaceType.TDecimate)
               {
                   //Для деинтерлейсеров, возвращающих 23.976fps
                   m.outframes = (int)(23.976 * (double)m.induration.TotalSeconds);
               }
               else if (m.deinterlace == DeinterlaceType.SmoothDeinterlace ||
                   m.deinterlace == DeinterlaceType.MCBob || m.deinterlace == DeinterlaceType.NNEDI)
               {
                   //Для деинтерлейсеров, удваивающих fps
                   m.outframes = (int)(Calculate.ConvertStringToDouble(m.inframerate) * 2.0 * (double)m.induration.TotalSeconds);
               }
               else
               {
                   //Во всех остальных случаях
                   m.outframes = m.inframes;
               }
           }
           else
           {
               m.outframes = (int)(Calculate.ConvertStringToDouble(m.outframerate) * (double)m.induration.TotalSeconds);
           }

           //Учитываем обрезку     
           m.outframes = ((m.trim_end == 0 || m.trim_end > m.outframes) ? m.outframes : m.trim_end) - m.trim_start;

           //С тест-скриптом тоже что-то надо делать..
           if (m.testscript && m.outframes > 2555) m.outframes = 2555;

           //Пересчитываем duration
           m.outduration = TimeSpan.FromSeconds((double)m.outframes / Calculate.ConvertStringToDouble(m.outframerate));

           return m;
       }

       public static string SplitCapString (string source)
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

       public static double GetProcent(int total, int current)
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
           string quality = Languages.Translate("Unknown");
           int pixels = m.inresw * m.inresh;

           //вычитаем чёрные поля
           int black = m.cropl_copy * m.cropb_copy * m.cropr_copy * m.cropt_copy;
           pixels = pixels - black;

           double framerate = ConvertStringToDouble(m.inframerate);
           int bitrate = m.invbitrate * 1000;
           quality = ConvertDoubleToPointString((double)bitrate / (double)pixels / framerate);
           return quality;
       }

       public static string GetQualityOut(Massive m, bool FromSize)
       {
           string quality = Languages.Translate("Unknown");
           if (m.outaudiostreams.Count > 0 && ((AudioStream)m.outaudiostreams[m.outaudiostream]).bitrate == 0)
               return quality; //Мы не можем посчитать качество, т.к. не знаем финальный размер, потому-что звук = VBR
           
           int pixels = m.outresw * m.outresh;

           //добавляем чёрные поля
           int black = m.blackh * m.blackw;
           pixels = pixels + black;

           double framerate;
           if (m.frameratemodifer == AviSynthScripting.FramerateModifers.AssumeFPS)
               framerate = ConvertStringToDouble(m.inframerate);
           else
               framerate = Calculate.ConvertStringToDouble(m.outframerate);

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
               if (m.outaudiostreams.Count > 0)
               {
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                   abitrate = outstream.bitrate;
               }

               if (FromSize)
                   bitrate = Calculate.GetBitrateForSize((double)m.outvbitrate, abitrate, (int)m.outduration.TotalSeconds, m.outvcodec, m.format) * 1000;
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

       public static double CompareOutAspects(Massive m)
       {
           double diff = 0.0;

           double outaspect = m.outaspect;
           double resaspect = (double)m.outresw / (double)m.outresh;

           if (outaspect > resaspect)
               diff = outaspect - resaspect;
           else if (resaspect > outaspect)
               diff = resaspect - outaspect;

           return diff;
       }

       public static long CompareLongs(long longone, long longtwo)
       {
           long diff = 0;

           if (longone > longtwo)
               diff = longone - longtwo;
           else if (longtwo > longone)
               diff = longtwo - longone;

           return diff;
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
           {
               string[] separator = new string[] { " " };
               string[] a = closeaspect.Split(separator, StringSplitOptions.None);
               closeaspect = a[0];
           }

           double _closeaspect = ConvertStringToDouble(closeaspect);
           double _aspect = ConvertStringToDouble(aspect);

           if (_aspect < _closeaspect)
           {
               //if (closeaspectindex == 0)
                   aspectslist.Insert(closeaspectindex, aspect); // + " (1:1)");
               //else
               //    aspectslist.Insert(closeaspectindex - 1, aspect + " (1:1)");
           }
           if (_aspect > _closeaspect)
           {
               if (closeaspectindex == aspectslist.Count)
                   aspectslist.Insert(closeaspectindex, aspect); // + " (1:1)");
               else
                   aspectslist.Insert(closeaspectindex + 1, aspect); // + " (1:1)");
           }

           return Calculate.ConvertArrayListToStringArray(aspectslist);
       }

       public static string GetClosePointDouble(string CompareValue, string[] ValuesList)
       {
          string closevalue = ValuesList[0];
           double closedouble = ConvertStringToDouble(ValuesList[0]);
           double comparedouble = ConvertStringToDouble(CompareValue);
           double bestdiff;
           if (comparedouble > closedouble)
               bestdiff = comparedouble - closedouble;
           else
               bestdiff = closedouble - comparedouble;

           ArrayList values = new ArrayList();
           values.AddRange(ValuesList);

           foreach (string value in values)
           {
               double currentvalue = ConvertStringToDouble(value);
               double diff;
               if (comparedouble > currentvalue)
                   diff = comparedouble - currentvalue;
               else
                   diff = currentvalue - comparedouble;
               if (diff < bestdiff)
               {
                   bestdiff = diff;
                   int index = values.IndexOf(value);
                   closevalue = values[index].ToString();
               }
           }
           return closevalue;
       }

       public static double GetCloseDouble(double CompareValue, string[] ValuesList)
       {
           double closedouble = ConvertStringToDouble(ValuesList[0]);
           double comparedouble = CompareValue;
           double bestdiff;
           if (comparedouble > closedouble)
               bestdiff = comparedouble - closedouble;
           else
               bestdiff = closedouble - comparedouble;

           foreach (string value in ValuesList)
           {
               double currentvalue = ConvertStringToDouble(value);
               double diff;
               if (comparedouble > currentvalue)
                   diff = comparedouble - currentvalue;
               else
                   diff = currentvalue - comparedouble;
               if (diff < bestdiff)
               {
                   bestdiff = diff;
                   closedouble = currentvalue;
               }
           }
           return closedouble;
       }

       public static string GetCloseInteger(string CompareValue, string[] ValuesList)
       {
           string closevalue = ValuesList[0];
           int closeint = Convert.ToInt32(ValuesList[0]);
           int compareint = Convert.ToInt32(CompareValue);
           int bestdiff;
           if (compareint > closeint)
               bestdiff = compareint - closeint;
           else
               bestdiff = closeint - compareint;

           foreach (string value in ValuesList)
           {
               int currentvalue = Convert.ToInt32(value);
               int diff;
               if (compareint > currentvalue)
                   diff = compareint - currentvalue;
               else
                   diff = currentvalue - compareint;
               if (diff < bestdiff)
               {
                   bestdiff = diff;
                   closevalue = currentvalue.ToString();
               }
           }
           return closevalue;
       }

       public static int GetCloseIntegerAL(int CompareValue, ArrayList ValuesList)
       {
           int closevalue = Convert.ToInt32(ValuesList[0]);
           int closeint = Convert.ToInt32(ValuesList[0]);
           int compareint = Convert.ToInt32(CompareValue);
           int bestdiff;
           if (compareint > closeint)
               bestdiff = compareint - closeint;
           else
               bestdiff = closeint - compareint;

           foreach (int value in ValuesList)
           {
               int diff;
               if (compareint > value)
                   diff = compareint - value;
               else
                   diff = value - compareint;
               if (diff < bestdiff)
               {
                   bestdiff = diff;
                   closevalue = value;
               }
           }
           return closevalue;
       }

       public static string GetEncodingSize(Massive m)
       {
           string ssize = Languages.Translate("Unknown");
           if (m.outaudiostreams.Count > 0 && ((AudioStream)m.outaudiostreams[m.outaudiostream]).bitrate == 0)
               return ssize; //Мы не можем знать размер, если звук = VBR
           if (m.encodingmode == Settings.EncodingModes.TwoPassSize ||
               m.encodingmode == Settings.EncodingModes.ThreePassSize ||
               m.encodingmode == Settings.EncodingModes.OnePassSize)
           {
               ssize = m.outvbitrate + " mb";
           }
           else if (m.format == Format.ExportFormats.Audio && m.outaudiostreams.Count > 0)
           {
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
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
                       AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                       outsize += (0.125261 * (double)outstream.bitrate * (double)m.outduration.TotalSeconds) / 1052.0 / 0.994;
                   }

                   //TS M2TS BluRay packet size
                   if (m.format == Format.ExportFormats.M2TS ||
                       m.format == Format.ExportFormats.TS)
                       outsize *= 1.03;
                   if (m.format == Format.ExportFormats.BluRay)
                       outsize *= 1.05;

                   ssize = Calculate.ConvertDoubleToPointString(outsize, 1) + " mb";

                   if (Format.Is4GBlimitedFormat(m) &&
                       outsize > 4000)
                   {
                       Message mess = new Message(m.owner);
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

       public static string GetIntraMatrix(string matrix)
       {
           if (matrix == "MPEG")
               return "8,16,19,22,26,27,29,34,16,16,22,24,27,29,34,37,19,22,26,27,29,34,34,38,22,22,26,27,29,34,37,40,22,26,27,29,32,35,40,48,26,27,29,32,35,40,48,58,26,27,29,34,38,46,56,69,27,29,35,38,46,56,69,83";
           else if (matrix == "KVCD Notch")
               return "8,9,12,22,26,27,29,34,9,10,14,26,27,29,34,37,12,14,18,27,29,34,37,38,22,26,27,31,36,37,38,40,26,27,29,36,39,38,40,48,27,29,34,37,38,40,48,58,29,34,37,38,40,48,58,69,34,37,38,40,48,58,69,79";
           else
               return "";
       }

       public static string GetInterMatrix(string matrix)
       {
           if (matrix == "MPEG")
               return "16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16";
           else if (matrix == "KVCD Notch")
               return "16,18,20,22,24,26,28,30,18,20,22,24,26,28,30,32,20,22,24,26,28,30,32,34,22,24,26,30,32,32,34,36,24,26,28,32,34,34,36,38,26,28,30,32,34,36,38,40,28,30,32,34,36,38,42,42,30,32,34,36,38,40,42,44";
           else
               return "";
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
           if (Path.GetExtension(vobpath).ToLower() != ".vob")
               return false;

           string pat = @"VTS_(\d\d)_(\d)";
           Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
           Match m = r.Match(Path.GetFileNameWithoutExtension(vobpath));
           if (m.Success == true)
               return true;
           else
           {
               if (Path.GetFileName(vobpath) == "VIDEO_TS.VOB")
                   return true;
               else
                   return false;
           }
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
           if (Path.GetExtension(infilepath).ToLower() == ".vob" && IsValidVOBName(infilepath))
           {
               title = GetTitleNum(infilepath);
               if (title != "")
                   title = "_T" + title;
           }

           string dvdname = GetDVDName(infilepath);

           string indexpath = Path.GetDirectoryName(infilepath) + "\\" + dvdname + ".index\\" + dvdname + title + ".d2v";

           //если файл ReadOnly или если в настройках выбрано создавать DGIndex-кэш в Темп-папке
           if (IsReadOnly(infilepath) || Settings.DGIndexInTemp)
               indexpath = Settings.TempPath + "\\" + dvdname + ".index\\" + dvdname + title + ".d2v";

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
           if (Path.GetExtension(infilepath).ToLower() == ".vob" && IsValidVOBName(infilepath))
           {
               string dvdname = "";
               string dvdpath = Path.GetDirectoryName(infilepath);
               dvdname = Path.GetFileName(dvdpath);
               if (dvdname == "VIDEO_TS")
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
            string s = "";
            string pat = @"DELAY\D+(\d+)ms";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(audiopath);
            if (m.Success == true)
            {
                if (m.Groups[0].Value.Contains("-"))
                    s = "-";
                s += m.Groups[1].Value;
                delay = Convert.ToInt32(s);
            }
            return delay;
        }

        public static int GetIntFromString(string value)
        {
            string pat = @"(\d+)";
            Regex r = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match m = r.Match(value);
            if (m.Success == true)
                return Convert.ToInt32(m.Groups[1].Value);
            return 0;
        }

        public static bool IsValid(int value, int validation)
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

        public static int GetSplittedValue(string value, int position)
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
           if (value == null || value == "") return 0.0;

           if (value.Contains(".") && "." != DecimalSeparator)
               value = value.Replace(".", DecimalSeparator);
           if (value.Contains(",") && "," != DecimalSeparator)
               value = value.Replace(",", DecimalSeparator);
           if (value.Contains(" "))
           {
               string[] separator = new string[] { " " };
               string[] a = value.Split(separator, StringSplitOptions.None);
               if (value == "Anamorphic (4:3)")
                   return 1.333;
               else if (value == "Anamorphic (16:9)")
                   return 1.778;
               else if (value == "Anamorphic (2.353)" ||
                        value == "Anamorphic (2,353)")
                   return 2.353;
               else
               {
                   double dvalue = 0.0;
                   Double.TryParse(a[0], NumberStyles.Float, null, out dvalue);
                   return dvalue;
               }
           }
           else
           {
               double dvalue = 0.0;
               Double.TryParse(value, NumberStyles.Float, null, out dvalue);
               return dvalue;
           }
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

       public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
       {
           BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
        bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
           return bitmapSource;
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

       public static string GetExtension(string filepath)
       {
           string ext = filepath.Substring(filepath.LastIndexOf("\\") + 1);
           ext = "." + ext.Substring(ext.LastIndexOf(".") + 1).ToLower();
           return ext;
       }

       public static string GetShortPath(string path)
       {
           if (Path.GetDirectoryName(path).Length > 10)
               return Path.GetPathRoot(path) + "...\\" + Path.GetFileName(path);
           return path;
       }
    }
}
