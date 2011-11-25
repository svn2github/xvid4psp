using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Runtime.InteropServices;
using System.IO;

namespace XviD4PSP
{
    public class Formats
    {
        #region GetSettings
        //Считанные настройки форматов
        private static object locker_set = new object();
        private static List<string> settings_list = null;
        private static string GetRawSettings(Format.ExportFormats format)
        {
            //"Инициализируем"
            if (settings_list == null)
            {
                lock (locker_set)
                {
                    settings_list = new List<string>();
                    for (int i = 0; i < Enum.GetValues(typeof(Format.ExportFormats)).Length; i++)
                        settings_list.Add(null);
                }
            }

            //Считываем настройки для требуемого формата
            if (settings_list[(int)format] == null)
            {
                lock (locker_set)
                {
                    try
                    {
                        string file = Calculate.StartupPath + "\\presets\\formats\\" + format.ToString() + ".ini";
                        if (File.Exists(file))
                        {
                            using (StreamReader sr = new StreamReader(file, System.Text.Encoding.Default))
                                settings_list[(int)format] = sr.ReadToEnd();
                        }
                        else
                        {
                            settings_list[(int)format] = "";
                        }
                    }
                    catch (Exception)
                    {
                        settings_list[(int)format] = "";
                    }
                }
            }

            //Выдаём настройки по индексу
            return settings_list[(int)format];
        }

        //Для одиночных значений
        public static string GetSettings(Format.ExportFormats format, string key)
        {
            try
            {
                using (StringReader str = new StringReader(GetRawSettings(format)))
                {
                    while (true)
                    {
                        string line = str.ReadLine();
                        if (line == null) return "";
                        if (line == "[" + key + "]")
                        {
                            line = str.ReadLine();
                            if (line == null) return "";
                            else return line.Trim();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        //Для одиночных значений int (с дефолтом)
        public static int GetSettings(Format.ExportFormats format, string key, int default_value)
        {
            int result = default_value;
            string value = GetSettings(format, key);
            if (int.TryParse(value, out result)) return result;
            else return default_value;
        }

        //Для одиночных значений bool (с дефолтом)
        public static bool GetSettings(Format.ExportFormats format, string key, bool default_value)
        {
            bool result = default_value;
            string value = GetSettings(format, key);
            if (bool.TryParse(value, out result)) return result;
            else return default_value;
        }

        //Для одиночных значений string (с дефолтом)
        public static string GetSettings(Format.ExportFormats format, string key, string default_value)
        {
            string value = GetSettings(format, key);
            return (string.IsNullOrEmpty(value)) ? default_value : value;
        }

        //Для множественных значений string (со множественным дефолтом)
        public static string[] GetSettings(Format.ExportFormats format, string key, string[] default_value)
        {
            try
            {
                using (StringReader str = new StringReader(GetRawSettings(format)))
                {
                    while (true)
                    {
                        string line = str.ReadLine();
                        if (line == null) return default_value;
                        if (line == "[" + key + "]")
                        {
                            line = str.ReadLine();
                            if (line == null) return default_value;
                            else
                            {
                                //Делим строку на подстроки
                                string[] lines = line.Trim().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                if (lines.Length == 0) return default_value;

                                //Избавляемся от пробелов и пересобираем строчку
                                ArrayList temp_lines = new ArrayList();
                                foreach (string ss in lines) temp_lines.Add(ss.Trim());
                                return (String[])temp_lines.ToArray(typeof(string));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return default_value;
            }
        }
        #endregion

        #region SetSettings
        //Изменение настроек формата
        public static void SetSettings(Format.ExportFormats format, string key, string value)
        {
            string output = "";
            string input = GetRawSettings(format);
            string path = Calculate.StartupPath + "\\presets\\formats\\" + format.ToString() + ".ini";

            if (key != null && input.Length > 0)
            {
                bool ok = false;
                using (StringReader str = new StringReader(input))
                {
                    while (true)
                    {
                        string line = str.ReadLine();
                        if (line == null) break;
                        if (!ok && line == "[" + key + "]") //Ключ
                        {
                            ok = true;
                            output += line + "\r\n" + value + "\r\n\r\n";
                            line = str.ReadLine(); //Старое значение (пропускаем)
                            if (line == null) break;
                            line = str.ReadLine(); //Пустая строка после него (пропускаем..)
                            if (line == null) break;
                            if (line.Length > 0)   //(..если она действительно пустая)
                                output += line + "\r\n";
                        }
                        else
                            output += line + "\r\n";
                    }
                }

                //Если дошли до конца, а строки так и не было - вставляем её
                if (!ok) output += "[" + key + "]\r\n" + value + "\r\n\r\n";
            }
            else if (key != null)
            {
                //Новый файл
                output = "!!!DO NOT MODIFY THIS FILE!!!\r\n\r\n[FormatName]\r\n" + Format.EnumToString(format) + "\r\n\r\n[" + key + "]\r\n" + value + "\r\n\r\n";
            }
            else
            {
                //"Сброс"
                output = "!!!DO NOT MODIFY THIS FILE!!!\r\n\r\n[FormatName]\r\n" + Format.EnumToString(format) + "\r\n\r\n";
            }

            try
            {
                //Сохраняем
                File.WriteAllText(path, output, System.Text.Encoding.Default);
                settings_list[(int)format] = output;
            }
            catch (DirectoryNotFoundException ex)
            {
                //Если папки нет, создаем её и пробуем снова
                if (!Directory.Exists(Calculate.StartupPath + "\\presets\\formats"))
                {
                    try
                    {
                        Directory.CreateDirectory(Calculate.StartupPath + "\\presets\\formats");
                    }
                    catch (Exception ex2)
                    {
                        throw new Exception("Can`t create directory: " + ex2.Message, ex2);
                    }

                    File.WriteAllText(path, output, System.Text.Encoding.Default);
                    settings_list[(int)format] = output;
                }
                else
                    throw new Exception("SetSettings: " + ex.Message, ex);
            }
        }
        #endregion

        #region GetDefaults
        //Список всех форматов с ихними дефолтами
        private static object locker_def = new object();
        private static List<Formats> defaults_list = null;
        public static Formats GetDefaults(Format.ExportFormats format)
        {
            //"Инициализируем"
            if (defaults_list == null)
            {
                lock (locker_def)
                {
                    defaults_list = new List<Formats>();
                    for (int i = 0; i < Enum.GetValues(typeof(Format.ExportFormats)).Length; i++)
                        defaults_list.Add(null);
                }
            }

            //Определяем дефолты для требуемого формата
            if (defaults_list[(int)format] == null)
            {
                lock (locker_def)
                {
                    defaults_list[(int)format] = new Formats(format);
                }
            }

            //Выдаём дефолты по индексу
            return defaults_list[(int)format];
        }
        #endregion

        #region SetDefaults (Constructor)
        //Устанавливаем дефолты форматов
        private Formats(Format.ExportFormats format)
        {
            //Общее
            this.THM_Format = "None";
            this.THM_FixAR = true;
            this.THM_Width = 0;
            this.THM_Height = 0;

            if (format == Format.ExportFormats.Avi)
            {
                #region AVI
                this.VCodecs = new string[] { "x264", "MPEG4", "FLV1", "MJPEG", "HUFF", "FFV1", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "0.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940", "60.000", "120.000" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "PCM", "AC3", "MP3", "MP2" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "virtualdubmod", "ffmpeg" };
                this.Muxer = "virtualdubmod";
                this.CLI_ffmpeg = "";
                this.CLI_virtualdubmod = "[v][/v][a][/a][o]interleave=\"1, 500, 1, 0\"[/o]";

                this.Extensions = new string[] { "avi" };
                this.Extension = "avi";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.AviHardware || format == Format.ExportFormats.AviHardwareHD)
            {
                #region AVI Hardware/HD
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                if (format == Format.ExportFormats.AviHardware)
                {
                    this.MidW = 640; this.MidH = 480;
                    this.MaxW = 1920; this.MaxH = 1088;
                }
                else
                {
                    this.MidW = 1920; this.MidH = 1088;
                    this.MaxW = 1920; this.MaxH = 1088;
                }
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "PCM", "AC3", "MP3", "MP2" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "virtualdubmod", "ffmpeg" };
                this.Muxer = "virtualdubmod";
                this.CLI_ffmpeg = "";
                this.CLI_virtualdubmod = "[v][/v][a][/a][o]interleave=\"1, 500, 1, 0\"[/o]";

                this.Extensions = new string[] { "avi" };
                this.Extension = "avi";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.AviiRiverClix2 || format == Format.ExportFormats.AviMeizuM6)
            {
                #region AVI iRiver Clix 2 / Meizu M6
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                if (format == Format.ExportFormats.AviiRiverClix2)
                {
                    this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                }
                else
                {
                    this.Framerates = new string[] { "15.000", "18.000", "20.000" };
                }
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 320; this.MidH = 240;
                this.MaxW = 320; this.MaxH = 240;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "MP2" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "virtualdubmod", "ffmpeg" };
                this.Muxer = "virtualdubmod";
                this.CLI_ffmpeg = "";
                this.CLI_virtualdubmod = "[v][/v][a][/a][o]interleave=\"1, 500, 1, 0\"[/o]";

                this.Extensions = new string[] { "avi" };
                this.Extension = "avi";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.AviDVPAL || format == Format.ExportFormats.AviDVNTSC)
            {
                #region AVI DV PAL/NTSC
                this.VCodecs = new string[] { "DV" };
                this.VCodecs_IsEditable = false;
                this.Framerates_IsEditable = false;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)" };
                this.Aspects_IsEditable = false;
                if (format == Format.ExportFormats.AviDVPAL)
                {
                    this.Framerates = new string[] { "25.000" };
                    this.MinW = 720; this.MinH = 576;
                    this.MidW = 720; this.MidH = 576;
                    this.MaxW = 720; this.MaxH = 576;
                }
                else
                {
                    this.Framerates = new string[] { "29.970" };
                    this.MinW = 720; this.MinH = 480;
                    this.MidW = 720; this.MidH = 480;
                    this.MaxW = 720; this.MaxH = 480;
                }
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Black" };
                this.LockedAR_Method = "Black";
                this.Anamorphic = true;
                this.Anamorphic_IsEditable = false;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "PCM" };
                this.ACodecs_IsEditable = false;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "ffmpeg" };
                this.Muxer = "ffmpeg";
                this.CLI_ffmpeg = "";

                this.Extensions = new string[] { "avi" };
                this.Extension = "avi";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Flv)
            {
                #region FLV
                this.VCodecs = new string[] { "FLV1", "x264" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "ffmpeg" };
                this.Muxer = "ffmpeg";
                this.CLI_ffmpeg = "";

                this.Extensions = new string[] { "flv" };
                this.Extension = "flv";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mkv)
            {
                #region MKV
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "0.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940", "60.000", "120.000" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "PCM", "FLAC", "AC3", "MP3", "MP2", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mkvmerge", "ffmpeg" };
                this.Muxer = "mkvmerge";
                this.CLI_ffmpeg = "";
                this.CLI_mkvmerge = "[v]--compression -1:none[/v][a]--compression -1:none[/a][o][/o]";

                this.Extensions = new string[] { "mkv" };
                this.Extension = "mkv";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.ThreeGP)
            {
                #region 3GP
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 320; this.MidH = 240;
                this.MaxW = 320; this.MaxH = 240;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "MP2", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "3gp" };
                this.Extension = "3gp";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mov)
            {
                #region MOV
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "0.000", "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "MP2", "AC3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mov" };
                this.Extension = "mov";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4)
            {
                #region MP4
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "0.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940", "60.000", "120.000" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "MP2", "AC3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4Archos5G || format == Format.ExportFormats.Mp4ToshibaG900)
            {
                #region MP4 Archos 5G / Toshiba G900
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                if (format == Format.ExportFormats.Mp4Archos5G)
                {
                    this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                    this.MinW = 16; this.MinH = 16;
                    this.MidW = 720; this.MidH = 480;
                    this.MaxW = 720; this.MaxH = 576;
                }
                else
                {
                    this.Aspects = new string[] { "1.3333 (4:3)", "1.6667", "1.7778 (16:9)", "1.8500", "2.3529" };
                    this.MinW = 16; this.MinH = 16;
                    this.MidW = 640; this.MidH = 480;
                    this.MaxW = 800; this.MaxH = 480;
                }
                this.ModW = 16; this.ModH = 8;
                this.Aspects_IsEditable = true;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4BlackBerry8100 || format == Format.ExportFormats.Mp4BlackBerry8800 || format == Format.ExportFormats.Mp4BlackBerry8830)
            {
                #region MP4 BlackBerry 8100/8800/8830
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                if (format == Format.ExportFormats.Mp4BlackBerry8100)
                {
                    this.MidW = 240; this.MidH = 180;
                    this.MaxW = 240; this.MaxH = 180;
                }
                else if (format == Format.ExportFormats.Mp4BlackBerry8800)
                {
                    this.MidW = 320; this.MidH = 180;
                    this.MaxW = 320; this.MaxH = 180;
                }
                else
                {
                    this.MidW = 320; this.MidH = 240;
                    this.MaxW = 320; this.MaxH = 240;
                }
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4SonyEricssonK610 || format == Format.ExportFormats.Mp4SonyEricssonK800)
            {
                #region MP4 SonyEricsson K610/K800
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                if (format == Format.ExportFormats.Mp4SonyEricssonK610)
                {
                    this.Framerates = new string[] { "15.000", "18.000", "20.000" };
                    this.MidW = 176; this.MidH = 144;
                    this.MaxW = 176; this.MaxH = 144;
                }
                else
                {
                    this.Framerates = new string[] { "15.000", "18.000", "20.000", "23.976", "24.000", "25.000" };
                    this.MidW = 320; this.MidH = 240;
                    this.MaxW = 320; this.MaxH = 240;
                }
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4MotorolaK1)
            {
                #region MP4 Motorola K1
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 352; this.MidH = 288;
                this.MaxW = 352; this.MaxH = 288;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP3", "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4Nokia5700)
            {
                #region MP4 Nokia 5700
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "15.000" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 320; this.MidH = 240;
                this.MaxW = 320; this.MaxH = 240;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4iPod50G || format == Format.ExportFormats.Mp4iPod55G)
            {
                #region MP4 iPhone 5.0G/5.5G
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                if (format == Format.ExportFormats.Mp4iPod50G)
                {
                    this.MidW = 320; this.MidH = 240;
                    this.MaxW = 320; this.MaxH = 240;
                }
                else
                {
                    this.MidW = 640; this.MidH = 480;
                    this.MaxW = 640; this.MaxH = 480;
                }
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                //MP4Box или NicMP4box
                this.Muxers = new string[] { "auto" };
                this.Muxer = "auto";
                //this.CLI_ffmpeg = "[v][/v][a][/a][o]-f ipod[/o]";
                //this.CLI_mp4box = "[v][/v][a][/a][o]-ipod[/o]";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4iPhone)
            {
                #region MP4 iPhone or Touch
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.5000", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 480; this.MidH = 320;
                this.MaxW = 720; this.MaxH = 576;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4AppleTV)
            {
                #region MP4 Apple TV
                this.VCodecs = new string[] { "x264", "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1280; this.MidH = 720;
                this.MaxW = 1280; this.MaxH = 720;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4Prada)
            {
                #region MP4 Prada
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 400; this.MidH = 240;
                this.MaxW = 400; this.MaxH = 240;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4PS3)
            {
                #region MP4 PS3 or XBOX360
                this.VCodecs = new string[] { "x264" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4PSPAVCTV)
            {
                #region MP4 PSP AVC TV
                this.VCodecs = new string[] { "x264" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)" };
                this.Aspects_IsEditable = true;
                this.MinW = 720; this.MinH = 480;
                this.MidW = 720; this.MidH = 480;
                this.MaxW = 720; this.MaxH = 480;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Black";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.THM_Format = "JPG";
                this.THM_FixAR = true;
                this.THM_Width = 160;
                this.THM_Height = 120;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4PSPAVC)
            {
                #region MP4 PSP AVC
                this.VCodecs = new string[] { "x264" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7647 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 480; this.MidH = 272;
                this.MaxW = 480; this.MaxH = 272;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.THM_Format = "JPG";
                this.THM_FixAR = true;
                this.THM_Width = 160;
                this.THM_Height = 120;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mp4PSPASP)
            {
                #region MP4 PSP ASP
                this.VCodecs = new string[] { "MPEG4", "XviD" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7647 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 480; this.MidH = 272;
                this.MaxW = 480; this.MaxH = 272;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = false;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "AAC" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = true;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "mp4box", "ffmpeg" };
                this.Muxer = "mp4box";
                this.CLI_ffmpeg = "";
                this.CLI_mp4box = "";

                this.Extensions = new string[] { "mp4", "m4v" };
                this.Extension = "mp4";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.THM_Format = "JPG";
                this.THM_FixAR = true;
                this.THM_Width = 160;
                this.THM_Height = 120;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mpeg2PS)
            {
                #region MPEG2 PS
                this.VCodecs = new string[] { "MPEG2" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP2", "AC3" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "ffmpeg" };
                this.Muxer = "ffmpeg";
                this.CLI_ffmpeg = "[v][/v][a][/a][o]-f vob[/o]";

                this.Extensions = new string[] { "mpg", "vob" };
                this.Extension = "mpg";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Mpeg2PAL || format == Format.ExportFormats.Mpeg2NTSC)
            {
                #region MPEG2 PAL/NTSC
                this.VCodecs = new string[] { "MPEG2" };
                this.VCodecs_IsEditable = false;
                this.Framerates_IsEditable = false;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)" };
                this.Aspects_IsEditable = false;
                if (format == Format.ExportFormats.Mpeg2PAL)
                {
                    this.Framerates = new string[] { "23.976", "24.000", "25.000" };
                    this.MinW = 720; this.MinH = 576;
                    this.MidW = 720; this.MidH = 576;
                    this.MaxW = 720; this.MaxH = 576;
                }
                else
                {
                    this.Framerates = new string[] { "23.976", "24.000", "25.000", "29.970" };
                    this.MinW = 720; this.MinH = 480;
                    this.MidW = 720; this.MidH = 480;
                    this.MaxW = 720; this.MaxH = 480;
                }
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Black" };
                this.LockedAR_Method = "Black";
                this.Anamorphic = true;
                this.Anamorphic_IsEditable = false;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "MP2", "AC3" };
                this.ACodecs_IsEditable = false;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "ffmpeg" };
                this.Muxer = "ffmpeg";
                this.CLI_ffmpeg = "[v][/v][a][/a][o]-f vob[/o]";

                this.Extensions = new string[] { "mpg", "vob" };
                this.Extension = "mpg";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = false;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.TS || format == Format.ExportFormats.M2TS)
            {
                #region TS/M2TS
                this.VCodecs = new string[] { "MPEG2", "x264" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 16; this.MinH = 16;
                this.MidW = 1920; this.MidH = 1088;
                this.MaxW = 1920; this.MaxH = 1088;
                this.ModW = 16; this.ModH = 8;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = false;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = false;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "PCM", "AAC", "MP2", "MP3", "AC3" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "Auto" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "tsmuxer", "ffmpeg" };
                this.Muxer = "tsmuxer";
                this.CLI_ffmpeg = "";
                this.CLI_tsmuxer = "[v]lang=English[/v][a]lang=%lang%[/a][o]--no-pcr-on-video-pid --new-audio-pes --vbr --vbv-len=500[/o]";

                if (format == Format.ExportFormats.TS)
                {
                    this.Extensions = new string[] { "ts" };
                    this.Extension = "ts";
                }
                else
                {
                    this.Extensions = new string[] { "m2ts" };
                    this.Extension = "m2ts";
                }
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = false;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else if (format == Format.ExportFormats.Custom)
            {
                #region Custom
                this.VCodecs = new string[] { "x264", "MPEG1", "MPEG2", "MPEG4", "FLV1", "MJPEG", "HUFF", "FFV1", "XviD", "DV" };
                this.VCodecs_IsEditable = true;
                this.Framerates = new string[] { "0.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940", "60.000", "120.000" };
                this.Framerates_IsEditable = true;
                this.Aspects = new string[] { "1.3333 (4:3)", "1.6667", "1.7778 (16:9)", "1.8500", "2.0000", "2.1000", "2.3529" };
                this.Aspects_IsEditable = true;
                this.MinW = 320; this.MinH = 240;
                this.MidW = 720; this.MidH = 576;
                this.MaxW = 1024; this.MaxH = 576;
                this.ModW = 4; this.ModH = 4;
                this.Resolution_IsEditable = true;
                this.LockedAR_Methods = new string[] { "Disabled", "SAR", "Crop", "Black" };
                this.LockedAR_Method = "Disabled";
                this.Anamorphic = true;
                this.Anamorphic_IsEditable = true;
                this.Interlaced = true;
                this.Interlaced_IsEditable = true;

                this.ACodecs = new string[] { "PCM", "FLAC", "AAC", "MP2", "MP3", "AC3" };
                this.ACodecs_IsEditable = true;
                this.Samplerates = new string[] { "22050", "32000", "44100", "48000" };
                this.Samplerates_IsEditable = true;
                this.LimitedToStereo = false;
                this.LimitedToStereo_IsEditable = true;

                this.Muxers = new string[] { "virtualdubmod", "ffmpeg", "mkvmerge", "mp4box", "tsmuxer", "pmpavc", "dpgmuxer" };
                this.Muxer = "mkvmerge";

                this.CLI_ffmpeg = "";
                this.CLI_mkvmerge = "[v]--compression -1:none[/v][a]--compression -1:none --language %a_id%:\"%lang%\"[/a][o]--title \"%out_name%\"[/o]";
                this.CLI_mp4box = "[v]:name=\"VIDEO\"[/v][a]:lang=\"%lang%\":name=\"AUDIO\"[/a][o]-itags name=\"%out_name%\"[/o]";
                this.CLI_tsmuxer = "[v]lang=English[/v][a]lang=%lang%[/a][o]--no-pcr-on-video-pid --new-audio-pes --vbr --vbv-len=500[/o]";
                this.CLI_virtualdubmod = "[v]title=\"%out_name%\"[/v][a]title=\"Audio stream #1\" language=\"%lang%\"[/a][o]interleave=\"1, 500, 1, 0\"[/o]";

                this.Extensions = new string[] { "*", "avi", "flv", "mkv", "3gp", "mp4", "m4v", "mov", "mpg", "ts", "m2ts", "pmp", "dpg" };
                this.Extension = "mkv";
                this.Splitting = "Disabled";
                this.DontMuxStreams = false;
                this.DontMuxStreams_IsEditable = true;
                this.DirectEncoding = true;
                this.DirectEncoding_IsEditable = true;
                this.DirectRemuxing = true;
                this.DirectRemuxing_IsEditable = true;
                this.LimitedTo4Gb = true;
                this.LimitedTo4Gb_IsEditable = true;

                this.IsEditable = true;
                #endregion
            }
            else
            {
                IsEditable = false;
            }
        }
        #endregion

        #region Internal variables
        //Video
        public string[] VCodecs { get; private set; } //GetVCodecsList
        public bool VCodecs_IsEditable { get; private set; }
        public string[] Framerates { get; private set; } //GetValidFrameratesList
        public bool Framerates_IsEditable { get; private set; }
        public string[] Aspects { get; private set; } //GetValidOutAspects
        public bool Aspects_IsEditable { get; private set; }
        public int MinW { get; private set; } //GetResWList
        public int MinH { get; private set; } //GetResHList
        public int MidW { get; private set; } //GetLimitedRes
        public int MidH { get; private set; } //GetLimitedRes
        public int MaxW { get; private set; } //GetResWList
        public int MaxH { get; private set; } //GetResHList
        public int ModW { get; private set; } //GetValidModW
        public int ModH { get; private set; } //GetValidModH
        public bool Resolution_IsEditable { get; private set; }
        public string[] LockedAR_Methods { get; private set; } //>1 = IsEditable IsLockedOutAspect GetValidOutAspect
        public string LockedAR_Method { get; private set; }
        public bool Anamorphic { get; private set; } //GetValidOutAspect GetValidResolution
        public bool Anamorphic_IsEditable { get; private set; }
        public bool Interlaced { get; private set; } //GetOutInterlace
        public bool Interlaced_IsEditable { get; private set; }

        //Audio
        public string[] ACodecs { get; private set; } //GetACodecsList
        public bool ACodecs_IsEditable { get; private set; }
        public string[] Samplerates { get; private set; } //GetValidSamplerates
        public bool Samplerates_IsEditable { get; private set; }
        public bool LimitedToStereo { get; private set; } //GetValidChannelsConverter
        public bool LimitedToStereo_IsEditable { get; private set; }

        //Muxing
        public string[] Muxers { get; private set; } //>1 = IsEditable GetMuxer
        public string Muxer { get; private set; }

        public string CLI_ffmpeg { get; private set; }
        public string CLI_mkvmerge { get; private set; }
        public string CLI_mp4box { get; private set; }
        public string CLI_tsmuxer { get; private set; }
        public string CLI_virtualdubmod { get; private set; }

        public string[] Extensions { get; private set; } //>1 = IsEditable GetValidExtension
        public string Extension { get; private set; }
        public string Splitting { get; private set; } //!None = IsEditable GetSplitting 
        public bool DontMuxStreams { get; private set; } //GetMultiplexing
        public bool DontMuxStreams_IsEditable { get; private set; }
        public bool DirectEncoding { get; private set; } //GetMuxer
        public bool DirectEncoding_IsEditable { get; private set; }
        public bool DirectRemuxing { get; private set; } //IsDirectRemuxingPossible 
        public bool DirectRemuxing_IsEditable { get; private set; }
        public bool LimitedTo4Gb { get; private set; } //Is4GBlimitedFormat
        public bool LimitedTo4Gb_IsEditable { get; private set; }

        //THM
        public string THM_Format { get; private set; }
        public bool THM_FixAR { get; private set; }
        public int THM_Width { get; private set; }
        public int THM_Height { get; private set; }

        public bool IsEditable { get; private set; }
        #endregion
    }
}
