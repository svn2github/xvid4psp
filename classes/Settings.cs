using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace XviD4PSP
{
    public static class Settings
    {

        public enum AutoVolumeModes { Disabled = 1, OnImport, OnExport }
        public enum AutoJoinModes { Disabled = 1, Enabled, DVDonly }
        public enum EncodingModes { OnePass = 1, TwoPass, ThreePass, Quality, Quantizer, OnePassSize, TwoPassSize, ThreePassSize, TwoPassQuality, ThreePassQuality }
        public enum PlayerEngines { DirectShow = 1, MediaBridge }
        public enum AfterImportActions { Nothing = 1, Middle, Play }
        public enum AudioEncodingModes { CBR = 1, VBR, ABR, TwoPass }
        public enum AutoDeinterlaceModes { AllFiles = 1, MPEGs, Disabled }

        private static void SetString(string Key, string Value)
        {
            RegistryKey myHive =
                Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");

            myHive.SetValue(Key, Value, RegistryValueKind.String);
            myHive.Close();

        }

        private static void SetBool(string Key, bool Value)
        {
            RegistryKey myHive =
                Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");

            myHive.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            myHive.Close();

        }

        private static void SetInt(string Key, int Value)
        {
            RegistryKey myHive =
                Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");

            myHive.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            myHive.Close();

        }

        private static void SetDouble(string Key, double Value)
        {
            RegistryKey myHive =
                Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");

            myHive.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            myHive.Close();

        }

        private static object GetValue(string Key)
        {
            using (RegistryKey
         myHive = Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5", true))
            {
                if (myHive != null)
                {
                    return myHive.GetValue(Key);
                }
                else
                {
                    return null;
                }
            }
        }

        public static string Key
        {
            get
            {
                object value = GetValue("key");
                if (value == null)
                {
                    return "0000";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("key", value);
            }
        }

        public static string Language
        {
            get
            {
                object value = GetValue("Language");
                if (value == null)
                {
                    return "English";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Language", value);
            }
        }

        public static string VolumeAccurate
        {
            get
            {
                object value = GetValue("VolumeAccurate");
                if (value == null)
                {
                    return "10%";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("VolumeAccurate", value);
            }
        }

        public static string DVDPath
        {
            get
            {
                object value = GetValue("DVDPath");
                if (value == null)
                {
                    return null;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("DVDPath", value);
            }
        }

        public static string BluRayPath
        {
            get
            {
                object value = GetValue("BluRayPath");
                if (value == null)
                {
                    return null;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("BluRayPath", value);
            }
        }

        public static bool AutoClose
        {
            get
            {
                object value = GetValue("AutoClose");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("AutoClose", value);
            }
        }

        public static bool AutoColorMatrix
        {
            get
            {
                object value = GetValue("AutoColorMatrix");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("AutoColorMatrix", value);
            }
        }

        public static bool AlwaysProgressive
        {
            get
            {
                object value = GetValue("AlwaysProgressive");
                if (value == null)
                {
                    SetBool("AlwaysProgressive", true);
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("AlwaysProgressive", value);
            }
        }

        public static bool WasDonate
        {
            get
            {
                object value = GetValue("WasDonate");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("WasDonate", value);
            }
        }

        public static bool AutoDeleteTasks
        {
            get
            {
                object value = GetValue("AutoDeleteTasks");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("AutoDeleteTasks", value);
            }
        }

        public static int ProcessPriority
        {
            get
            {
                object value = GetValue("EncProcessPriority");
                if (value == null)
                {
                    return 1;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("EncProcessPriority", value);
            }
        }

        public static string TempPath
        {
            get
            {
                object value = GetValue("TempPath");
                if (value == null)
                {
                    string temp = Environment.ExpandEnvironmentVariables("%SystemDrive%") + "\\Temp";
                    if (!Directory.Exists(temp))
                        Directory.CreateDirectory(temp);
                    return temp;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("TempPath", value);
            }
        }

        public static Format.ExportFormats FormatOut
        {
            get
            {
                object value = GetValue("Format");
                if (value == null)
                {
                    return Format.ExportFormats.Mkv;
                }
                else
                {
                    return (Format.ExportFormats)Enum.Parse(typeof(Format.ExportFormats), value.ToString());
                }
            }
            set
            {
                SetString("Format", value.ToString());
            }
        }

        public static string Filtering
        {
            get
            {
                object value = GetValue("Filtering");
                if (value == null)
                {
                    //значение по умолчанию
                    return "Disabled";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Filtering", value);
            }
        }

        public static string SBC
        {
            get
            {
                object value = GetValue("SBC");
                if (value == null)
                {
                    //значение по умолчанию
                    return "Disabled";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("SBC", value);
            }
        }

        public static bool ArgumentsToLog
        {
            get
            {
                object value = GetValue("ArgumentsToLog");
                if (value == null)
                    return true;
                else
                    return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("ArgumentsToLog", value);
            }
        }

        public static string GetVEncodingPreset(Format.ExportFormats format)
        {
            object value;

            using (RegistryKey myHive =
                Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5\\videopreset", true))
            {
                if (myHive != null)
                    value = myHive.GetValue(format.ToString());
                else
                    value = null;
            }

            if (value == null)
            {
                //значение по умолчанию
                return Format.GetValidVPreset(format);
            }
            else
            {
                return Convert.ToString(value);
            }
        }

        public static string GetAEncodingPreset(Format.ExportFormats format)
        {
            object value;
            using (RegistryKey myHive =
                Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5\\audiopreset", true))
            {
                if (myHive != null)
                    value = myHive.GetValue(format.ToString());
                else
                    value = null;
            }
            if (value == null)
            {
                //значение по умолчанию
                return Format.GetValidAPreset(format);
            }
            else
            {
                return Convert.ToString(value);
            }
        }

        public static void SetFormatPreset(Format.ExportFormats format, string key, string value)
        {
            {
                RegistryKey myHive =
     Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5\\" + format);
                myHive.SetValue(key, value, RegistryValueKind.String);
                myHive.Close();
            }
        }

        public static string GetFormatPreset(Format.ExportFormats format, string key)
        {
            object value;
            using (RegistryKey myHive =
                Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5\\" + format, true))
            {
                if (myHive != null)
                    value = myHive.GetValue(key);
                else
                    value = null;
            }
            if (value == null)
                return null;
            else
                return Convert.ToString(value);
        }

        public static void SetVEncodingPreset(Format.ExportFormats format, string value)
        {
            {
                RegistryKey myHive =
     Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5\\videopreset");
                myHive.SetValue(format.ToString(), value, RegistryValueKind.String);
                myHive.Close();
            }
        }

        public static void SetAEncodingPreset(Format.ExportFormats format, string value)
        {
            {
                RegistryKey myHive =
     Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5\\audiopreset");
                myHive.SetValue(format.ToString(), value, RegistryValueKind.String);
                myHive.Close();
            }
        }

        public static AviSynthScripting.Resizers ResizeFilter
        {
            get
            {
                object value = GetValue("ResizeFilter");
                if (value == null)
                    return AviSynthScripting.Resizers.Lanczos4Resize;
                else
                    return (AviSynthScripting.Resizers)Enum.Parse(typeof(AviSynthScripting.Resizers), value.ToString());
            }
            set
            {
                SetString("ResizeFilter", value.ToString());
            }
        }

        public static AviSynthScripting.FramerateModifers FramerateModifer
        {
            get
            {
                object value = GetValue("FramerateModifer");
                if (value == null)
                    return AviSynthScripting.FramerateModifers.ChangeFPS;
                else
                    return (AviSynthScripting.FramerateModifers)Enum.Parse(typeof(AviSynthScripting.FramerateModifers), value.ToString());
            }
            set
            {
                SetString("FramerateModifer", value.ToString());
            }
        }

        public static AviSynthScripting.SamplerateModifers SamplerateModifer
        {
            get
            {
                object value = GetValue("SamplerateModifer");
                if (value == null)
                    return AviSynthScripting.SamplerateModifers.SSRC;
                else
                    return (AviSynthScripting.SamplerateModifers)Enum.Parse(typeof(AviSynthScripting.SamplerateModifers), value.ToString());
            }
            set
            {
                SetString("SamplerateModifer", value.ToString());
            }
        }

        public static AfterImportActions AfterImportAction
        {
            get
            {
                object value = GetValue("AfterImportAction");
                if (value == null)
                    return AfterImportActions.Nothing;
                else
                    return (AfterImportActions)Enum.Parse(typeof(AfterImportActions), value.ToString());
            }
            set
            {
                SetString("AfterImportAction", value.ToString());
            }
        }

        public static string Volume
        {
            get
            {
                object value = GetValue("Volume");
                if (value == null)
                {
                    return "100%";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Volume", value);
            }
        }

        public static PlayerEngines PlayerEngine
        {
            get
            {
                object value = GetValue("PlayerEngine");
                if (value == null)
                    return PlayerEngines.DirectShow;
                else
                    return (PlayerEngines)Enum.Parse(typeof(PlayerEngines), value.ToString());
            }
            set
            {
                SetString("PlayerEngine", value.ToString());
            }
        }

        public static AviSynthScripting.Decoders AVIDecoder
        {
            get
            {
                object value = GetValue("AVIDecoder");
                if (value == null)
                    return AviSynthScripting.Decoders.DirectShowSource;
                else
                    return (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), value.ToString());
            }
            set
            {
                SetString("AVIDecoder", value.ToString());
            }
        }

        public static AviSynthScripting.Decoders MPEGDecoder
        {
            get
            {
                object value = GetValue("MPEGDecoder");
                if (value == null)
                    return AviSynthScripting.Decoders.MPEG2Source;
                else
                    return (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), value.ToString());
            }
            set
            {
                SetString("MPEGDecoder", value.ToString());
            }
        }

        public static AviSynthScripting.Decoders OtherDecoder
        {
            get
            {
                object value = GetValue("OtherDecoder");
                if (value == null)
                    return AviSynthScripting.Decoders.DirectShowSource;
                else
                    return (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), value.ToString());
            }
            set
            {
                SetString("OtherDecoder", value.ToString());
            }
        }

        public static Autocrop.AutocropMode AutocropMode
        {
            get
            {
                object value = GetValue("AutocropMode");
                if (value == null)
                    return Autocrop.AutocropMode.MPEGOnly;
                else
                    return (Autocrop.AutocropMode)Enum.Parse(typeof(Autocrop.AutocropMode), value.ToString());
            }
            set
            {
                SetString("AutocropMode", value.ToString());
            }
        }

        public static AutoDeinterlaceModes AutoDeinterlaceMode
        {
            get
            {
                object value = GetValue("AutoDeinterlaceMode");
                if (value == null)
                    return AutoDeinterlaceModes.MPEGs;
                else
                    return (AutoDeinterlaceModes)Enum.Parse(typeof(AutoDeinterlaceModes), value.ToString());
            }
            set
            {
                SetString("AutoDeinterlaceMode", value.ToString());
            }
        }

        public static AutoJoinModes AutoJoinMode
        {
            get
            {
                object value = GetValue("AutoJoinMode");
                if (value == null)
                    return AutoJoinModes.DVDonly;
                else
                    return (AutoJoinModes)Enum.Parse(typeof(AutoJoinModes), value.ToString());
            }
            set
            {
                SetString("AutoJoinMode", value.ToString());
            }
        }

        public static DeinterlaceType TIVTC
        {
            get
            {
                object value = GetValue("TIVTC");
                if (value == null)
                    return DeinterlaceType.TIVTC;
                else
                    return (DeinterlaceType)Enum.Parse(typeof(DeinterlaceType), value.ToString());
            }
            set
            {
                SetString("TIVTC", value.ToString());
            }
        }

        public static DeinterlaceType Deinterlace
        {
            get
            {
                object value = GetValue("Deinterlace");
                if (value == null)
                    return DeinterlaceType.Yadif;
                else
                    return (DeinterlaceType)Enum.Parse(typeof(DeinterlaceType), value.ToString());
            }
            set
            {
                SetString("Deinterlace", value.ToString());
            }
        }

        public static AutoVolumeModes AutoVolumeMode
        {
            get
            {
                object value = GetValue("AutoVolumeMode");
                if (value == null)
                    return AutoVolumeModes.OnExport;
                else
                    return (AutoVolumeModes)Enum.Parse(typeof(AutoVolumeModes), value.ToString());
            }
            set
            {
                SetString("AutoVolumeMode", value.ToString());
            }
        }

        public static string Mpeg1FOURCC
        {
            get
            {
                object value = GetValue("Mpeg1FOURCC");
                if (value == null)
                {
                    return "MPEG";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Mpeg1FOURCC", value);
            }
        }

        public static string Mpeg2FOURCC
        {
            get
            {
                object value = GetValue("Mpeg2FOURCC");
                if (value == null)
                {
                    return "MPEG";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Mpeg2FOURCC", value);
            }
        }

        public static string Mpeg4FOURCC
        {
            get
            {
                object value = GetValue("Mpeg4FOURCC");
                if (value == null)
                {
                    return "DIVX";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Mpeg4FOURCC", value);
            }
        }

        public static string HUFFFOURCC
        {
            get
            {
                object value = GetValue("HUFFFOURCC");
                if (value == null)
                {
                    return "HFYU";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("HUFFFOURCC", value);
            }
        }

        public static string XviDFOURCC
        {
            get
            {
                object value = GetValue("XviDFOURCC");
                if (value == null)
                {
                    return "XVID";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("XviDFOURCC", value);
            }
        }

        public static string DVFOURCC
        {
            get
            {
                object value = GetValue("DVFOURCC");
                if (value == null)
                {
                    return "dvsd";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("DVFOURCC", value);
            }
        }

        public static string AVCHD_PATH
        {
            get
            {
                object value = GetValue("AVCHD_PATH");
                if (value == null)
                {
                    return null;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("AVCHD_PATH", value);
            }
        }

        public static string BluRayType
        {
            get
            {
                object value = GetValue("bluray_type");
                if (value == null)
                {
                    return "UDF 2.50 DVD/BD";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("bluray_type", value);
            }
        }

        public static bool DontDemuxAudio
        {
            get
            {
                object value = GetValue("DontDemuxAudio");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DontDemuxAudio", value);
            }
        }

        public static bool SaveAnamorph
        {
            get
            {
                object value = GetValue("SaveAnamorph");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("SaveAnamorph", value);
            }
        }

        public static int AutocropSensivity
        {
            get
            {
                object value = GetValue("AutocropSensivity");
                if (value == null)
                {
                    return 27;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("AutocropSensivity", value);
            }
        }

        public static bool DeleteFFCache
        {
            get
            {
                object value = GetValue("DeleteFFCache");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DeleteFFCache", value);
            }
        }

        public static bool DeleteDGIndexCache
        {
            get
            {
                object value = GetValue("DeleteDGIndexCache");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DeleteDGIndexCache", value);
            }
        }

        public static bool SearchTempPath
        {
            get
            {
                object value = GetValue("SearchTempPath");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("SearchTempPath", value);
            }
        }

        public static bool x264_PSNR
        {
            get
            {
                object value = GetValue("x264_PSNR");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("x264_PSNR", value);
            }
        }

        public static bool x264_SSIM
        {
            get
            {
                object value = GetValue("x264_SSIM");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("x264_SSIM", value);
            }
        }

        public static bool PrintAviSynth
        {
            get
            {
                object value = GetValue("PrintAviSynth");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("PrintAviSynth", value);
            }
        }

        public static bool ffmpeg_pipe
        {
            get
            {
                object value = GetValue("ffmpeg_pipe");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("ffmpeg_pipe", value);
            }
        }

        public static bool Mpeg2MultiplexDisabled
        {
            get
            {
                object value = GetValue("Mpeg2Multiplex");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("Mpeg2Multiplex", value);
            }
        }

        public static void ResetAllSettings(System.Windows.Window owner)
        {
            RegistryKey myHive = Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5", true);
            if (myHive != null)
            {
                myHive = Registry.CurrentUser.OpenSubKey("Software\\Winnydows", true);
                myHive.DeleteSubKeyTree("XviD4PSP5");
                myHive.Close();
            }
        }

        //Значение для ползунка регулятора громкости 
        public static double VolumeLevel
        {
            get
            {
                object value = GetValue("VolumeLevel");
                if (value == null)
                {
                    SetDouble("VolumeLevel", 1.0);
                    return 1.0;
                }
                return Convert.ToDouble(value);
            }
            set
            {
                SetDouble("VolumeLevel", value);
            }
        }

        //Разрешает/запрещает изменять размер основного окна при запуске
        public static bool WindowResize
        {
            get
            {
                object value = GetValue("WindowResize");
                if (value == null)
                {
                    SetBool("WindowResize", true);
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("WindowResize", value);
            }
        }

        //Положение окна
        public static string WindowLocation
        {
            get
            {
                object value = GetValue("WindowLocation");
                if (value == null)
                {
                    return "747/577/100/100";
                }
                return Convert.ToString(value);
            }

            set
            {
                SetString("WindowLocation", value);
            }
        }

        //Размер области для заданий
        public static string TasksRows
        {
            get
            {
                object value = GetValue("TasksRows");
                if (value == null)
                {
                    return "128*/400*";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("TasksRows", value);
            }
        }

        //Разрешает или запрещает удалять из текста скрипта комментарии (#)
        public static bool HideComments
        {
            get
            {
                object value = GetValue("HideComments");
                if (value == null)
                {
                    SetBool("HideComments", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("HideComments", value);
            }
        }

        public static string Test
        {
            get
            {
                object value = GetValue("Test");
                if (value == null)
                {
                    return "UDF 2.50 DVD/BD";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("Test", value);
            }
        }

        //Кроп/ресайз до или после фильтрации
        public static bool ResizeFirst
        {
            get
            {
                object value = GetValue("ResizeFirst");
                if (value == null)
                {
                    SetBool("ResizeFirst", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("ResizeFirst", value);
            }
        }

        //Кол-во кадров для анализа автокропа
        public static int AutocropFrames
        {
            get
            {
                object value = GetValue("AutocropFrames");
                if (value == null)
                {
                    SetInt("AutocropFrames", 11);
                    return 11;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("AutocropFrames", value);
            }
        }

        //Пересчет аспекта при ручном кропе
        public static bool RecalculateAspect
        {
            get
            {
                object value = GetValue("RecalculateAspect");
                if (value == null)
                {
                    SetBool("RecalculateAspect", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("RecalculateAspect", value);
            }
        }

        //Использовать старый или новый FFmpegSource
        public static bool FFmpegSource2
        {
            get
            {
                object value = GetValue("FFmpegSource2");
                if (value == null)
                {
                    SetBool("FFmpegSource2", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("FFmpegSource2", value);
            }
        }

        //Перечитывать или нет параметры видео из скрипта, при сохранении задания
        public static bool ReadScript
        {
            get
            {
                object value = GetValue("ReadScript");
                if (value == null)
                {
                    SetBool("ReadScript", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("ReadScript", value);
            }
        }

        //Записывать лог кодирования в файл
        public static bool WriteLog
        {
            get
            {
                object value = GetValue("WriteLog");
                if (value == null)
                {
                    SetBool("WriteLog", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("WriteLog", value);
            }
        }

        //Сохранять файл лога кодирования во временную папку
        public static bool LogInTemp
        {
            get
            {
                object value = GetValue("LogInTemp");
                if (value == null)
                {
                    SetBool("LogInTemp", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("LogInTemp", value);
            }
        }

        public static string GoodFilesExtensions
        {
            get
            {
                object value = GetValue("GoodFilesExtensions");
                if (value == null)
                {
                    return "avi/divx/wmv/mpg/mpeg/asf/mkv/mov/qt/3gp/mp4/ogm/avs/vob/ts/m2t/m2v/d2v/m2ts/flv/pmp/h264/264/evo/vdr/dpg/wav/ac3/dts/mpa/mp3/mp2/wma/m4a/aac/ogg/aiff/aif/flac/ape";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("GoodFilesExtensions", value);
            }
        }

        //Автозапуск кодирования после открытия всех файлов (при пакетной обработке)
        public static bool AutoBatchEncoding
        {
            get
            {
                object value = GetValue("AutoBatchEncoding");
                if (value == null)
                {
                    SetBool("AutoBatchEncoding", false);
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("AutoBatchEncoding", value);
            }
        }

        //Включить опцию ForcedFilm при индексации DGIndex`ом
        public static bool DGForceFilm
        {
            get
            {
                object value = GetValue("DGForceFilm");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DGForceFilm", value);
            }
        }

        //DGIndex-кэш в Темп-папку
        public static bool DGIndexInTemp
        {
            get
            {
                object value = GetValue("DGIndexInTemp");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DGIndexInTemp", value);
            }
        }

        //Папка для batch-encoding исходников
        public static string BatchPath
        {
            get
            {
                object value = GetValue("BatchPath");
                if (value == null)
                {
                    return null;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("BatchPath", value);
            }
        }
            
        //Папка для batch-encoding перекодированного
        public static string BatchEncodedPath
        {
            get
            {
                object value = GetValue("BatchEncodedPath");
                if (value == null)
                {
                    return null;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("BatchEncodedPath", value);
            }
        }

        //OldSeeking - непрерывное позиционирование
        public static bool OldSeeking
        {
            get
            {
                object value = GetValue("OldSeeking");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("OldSeeking", value);
            }
        }

        public static string ChannelsConverter
        {
            get
            {
                object value = GetValue("ChannelsConverter");
                if (value == null)
                {
                    return "KeepOriginalChannels";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("ChannelsConverter", value);
            }
        }

        //Использовать FFmpeg-инфу об AR открываемого файла
        public static bool UseFFmpegAR
        {
            get
            {
                object value = GetValue("UseFFmpegAR");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("UseFFmpegAR", value);
            }
        }

        public static int VCropOpacity
        {
            get
            {
                object value = GetValue("VCropOpacity");
                if (value == null)
                {
                    return 2;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("VCropOpacity", value);
            }
        }

        public static int VCropBrightness
        {
            get
            {
                object value = GetValue("VCropBrightness");
                if (value == null)
                {
                    return 25;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("VCropBrightness", value);
            }
        }

        public static string VCropFrame
        {
            get
            {
                object value = GetValue("VCropFrame");
                if (value == null)
                {
                    return "THM-frame";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("VCropFrame", value);
            }
        }

        public static bool BatchCloneAR
        {
            get
            {
                object value = GetValue("BatchCloneAR");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("BatchCloneAR", value);
            }
        }

        public static bool BatchCloneTrim
        {
            get
            {
                object value = GetValue("BatchCloneTrim");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("BatchCloneTrim", value);
            }
        }

        public static bool BatchCloneDeint
        {
            get
            {
                object value = GetValue("BatchCloneDeint");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("BatchCloneDeint", value);
            }
        }
        
        public static bool BatchCloneFPS
        {
            get
            {
                object value = GetValue("BatchCloneFPS");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("BatchCloneFPS", value);
            }
        }
        
        public static bool BatchCloneAudio
        {
            get
            {
                object value = GetValue("BatchCloneAudio");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("BatchCloneAudio", value);
            }
        }

        public static bool BatchPause
        {
            get
            {
                object value = GetValue("BatchClonePath");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("BatchClonePath", value);
            }
        }

        public static bool ApplyDelay
        {
            get
            {
                object value = GetValue("ApplyDelay");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("ApplyDelay", value);
            }
        }

        public static bool Use64x264
        {
            get
            {
                object value = GetValue("Use64x264");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("Use64x264", value);
            }
        }

        public static string HotKeys
        {
            get
            {
                object value = GetValue("HotKeys");
                if (value == null || Convert.ToString(value) == "")
                {
                    return "Open file(s)=Ctrl+O; Open folder=Ctrl+Alt+O; Open DVD folder=Ctrl+D; Decode file=Ctrl+Alt+D; Join file=Ctrl+J; Close file=Ctrl+C; Save task=Ctrl+S; Save frame=Ctrl+F; Save THM frame=Ctrl+Alt+F; Refresh preview=Shift+R; VDemux=Shift+V;" +
                        "Detect black borders=Shift+B; Detect interlace=Shift+I; Color correction=C; Resolution/Aspect=R; Interlace/Framerate=I; VEncoding settings=V; ADemux=Shift+A; Save to WAV=W; Editing options=Ctrl+A; AEncoding settings=A; Add subtitles=Insert;" +
                        "Remove subtitles=Delete; Edit filtering script=S; Test script=Ctrl+T; Save script=Shift+S; Windows Media Player=Shift+M; Media Player Classic=M; WPF Video Player=Ctrl+M; Media Info=F1; FFRebuilder=F2; MKVRebuilder=F3; DGIndex=F4; DGPulldown=F5;" + 
                        "DGAVCIndex=F6; VirtualDubMod=F7; AVI-Mux=F8; tsMuxeR=F9; MKVExtract=F10; MKVMerge=F11; Yamb=F12; Frame forward=Right; Frame back=Left; 10 frames forward=Ctrl+Right; 10 frames backward=Ctrl+Left; Play-Pause=Space; Fullscreen=Esc; Volume+=Up;" + 
                        "Volume-=Down; Set Start=Home; Set End=End; Apply Trim=T;";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("HotKeys", value);
            }
        }

        public static Shutdown.ShutdownMode FinalAction
        {
            get
            {
                object value = GetValue("FinalAction");
                if (value == null)
                    return Shutdown.ShutdownMode.Wait;
                else
                    return (Shutdown.ShutdownMode)Enum.Parse(typeof(Shutdown.ShutdownMode), value.ToString());
            }
            set
            {
                SetString("FinalAction", value.ToString());
            }
        }

        public static bool FFmpegAssumeFPS
        {
            get
            {
                object value = GetValue("FFmpegAssumeFPS");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("FFmpegAssumeFPS", value);
            }
        }

        public static int LimitModW
        {
            get
            {
                object value = GetValue("LimitModW");
                if (value == null)
                {
                    return 16;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("LimitModW", value);
            }
        }

        public static int LimitModH
        {
            get
            {
                object value = GetValue("LimitModH");
                if (value == null)
                {
                    return 8;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("LimitModH", value);
            }
        }

        public static bool DeleteTempFiles
        {
            get
            {
                object value = GetValue("DeleteTempFiles");
                if (value == null)
                {
                    return true;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DeleteTempFiles", value);
            }
        }

        public static string RecentFiles
        {
            get
            {
                object value = GetValue("RecentFiles");
                if (value == null)
                {
                    return "";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("RecentFiles", value);
            }
        }

        public static bool ScriptView
        {
            get
            {
                object value = GetValue("ScriptView");
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("ScriptView", value);
            }
        }

        //Цвет фона:текста для окна ScriptView
        public static string ScriptView_Brushes
        {           
            get
            {
                object value = GetValue("ScriptView_Brushes");
                if (value == null)
                {
                    SetString("ScriptView_Brushes", "#FFFFFFFF:#FF000000");
                    return "#FFFFFFFF:#FF000000";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("ScriptView_Brushes", value);
            }
        }
    }
}
