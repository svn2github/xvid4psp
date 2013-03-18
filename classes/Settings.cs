using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media;

namespace XviD4PSP
{
    public static class Settings
    {
        public enum AutoVolumeModes { Disabled = 1, OnImport, OnExport }
        public enum AutoJoinModes { Disabled = 1, Enabled, DVDonly }
        public enum EncodingModes { OnePass = 1, TwoPass, ThreePass, Quality, Quantizer, OnePassSize, TwoPassSize, ThreePassSize, TwoPassQuality, ThreePassQuality }
        public enum PlayerEngines { DirectShow = 1, MediaBridge, PictureView }
        public enum VRenderers { Auto = 0, Overlay, VMR7, VMR9, EVR }
        public enum AfterImportActions { Nothing = 1, Middle, Play }
        public enum AudioEncodingModes { CBR = 1, VBR, ABR, TwoPass, CVBR, ALAC }
        public enum AutoDeinterlaceModes { AllFiles = 1, MPEGs, Disabled }
        public enum ATrackModes { Manual = 0, Language, Number }

        private static void SetString(string Key, string Value)
        {
            RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");
            myHive.SetValue(Key, Value, RegistryValueKind.String);
            myHive.Close();
        }

        private static void SetBool(string Key, bool Value)
        {
            RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");
            myHive.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            myHive.Close();
        }

        private static void SetInt(string Key, int Value)
        {
            RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");
            myHive.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            myHive.Close();
        }

        private static void SetDouble(string Key, double Value)
        {
            RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5");
            myHive.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            myHive.Close();
        }

        private static double GetDouble(string key, double _default)
        {
            object value = GetValue(key);
            if (value == null)
            {
                return _default;
            }
            else
            {
                double dvalue;
                string sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                string dstring = value.ToString().Replace(".", sep).Replace(",", sep);
                if (Double.TryParse(dstring, out dvalue)) return dvalue;
                else return _default;
            }
        }

        private static object GetValue(string Key)
        {
            using (RegistryKey myHive = Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5", true))
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
                RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5\\" + format);
                myHive.SetValue(key, value, RegistryValueKind.String);
                myHive.Close();
            }
        }

        public static string GetFormatPreset(Format.ExportFormats format, string key)
        {
            object value;
            using (RegistryKey myHive = Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5\\" + format, true))
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
                RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5\\videopreset");
                myHive.SetValue(format.ToString(), value, RegistryValueKind.String);
                myHive.Close();
            }
        }

        public static void SetAEncodingPreset(Format.ExportFormats format, string value)
        {
            {
                RegistryKey myHive = Registry.CurrentUser.CreateSubKey("Software\\Winnydows\\XviD4PSP5\\audiopreset");
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
                    return (PlayerEngines)Enum.Parse(typeof(PlayerEngines), value.ToString(), true);
            }
            set
            {
                SetString("PlayerEngine", value.ToString());
            }
        }

        //Список видео декодеров и расширений
        public static string VDecoders
        {
            get
            {
                object value = GetValue("VDecoders");
                if (value == null || value.ToString().Length == 0)
                {
                    return "mpeg_ps/ts=MPEG2Source; avi=DirectShowSource; mp4=DirectShowSource; mkv=DirectShowSource; evo=FFmpegSource2; *=DirectShowSource";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("VDecoders", value);
            }
        }

        //Список аудио декодеров и расширений
        public static string ADecoders
        {
            get
            {
                object value = GetValue("ADecoders");
                if (value == null || value.ToString().Length == 0)
                {
                    return "ac3=NicAC3Source; mpa=NicMPG123Source; mp1=NicMPG123Source; mp2=NicMPG123Source; mp3=bassAudioSource; wav=RaWavSource; w64=RaWavSource; dts=NicDTSSource; wma=bassAudioSource; *=bassAudioSource";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("ADecoders", value);
            }
        }

        public static bool DSS_ConvertFPS
        {
            get
            {
                object value = GetValue("DSS_ConvertFPS");
                if (value == null) return true;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("DSS_ConvertFPS", value);
            }
        }

        //Разрешить декодирование звука через DirectShowSource
        public static bool DSS_Enable_Audio
        {
            get
            {
                object value = GetValue("DSS_Enable_Audio");
                if (value == null) return true;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("DSS_Enable_Audio", value);
            }
        }

        //DSS2: использовать LAV Splitter
        public static bool DSS2_LAVSplitter
        {
            get
            {
                object value = GetValue("DSS2_LAVSplitter");
                if (value == null) return true;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("DSS2_LAVSplitter", value);
            }
        }

        //Настройки LAVSplitter`а
        public static string DSS2_LAVS_Settings
        {
            get
            {
                object value = GetValue("DSS2_LAVS_Settings");
                if (value == null) return "L3";
                else return Convert.ToString(value);
            }
            set
            {
                SetString("DSS2_LAVS_Settings", value);
            }
        }

        //DSS2: использовать LAV Video Decoder
        public static bool DSS2_LAVDecoder
        {
            get
            {
                object value = GetValue("DSS2_LAVDecoder");
                if (value == null) return true;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("DSS2_LAVDecoder", value);
            }
        }

        //Настройки LAV Video Decoder`а
        public static string DSS2_LAVV_Settings
        {
            get
            {
                object value = GetValue("DSS2_LAVV_Settings");
                if (value == null) return "L3";
                else return Convert.ToString(value);
            }
            set
            {
                SetString("DSS2_LAVV_Settings", value);
            }
        }

        //DSS2: режим субтитров
        public static int DSS2_SubsMode
        {
            get
            {
                object value = GetValue("DSS2_SubsMode");
                if (value == null) return 0;
                else return Convert.ToInt32(value);
            }
            set
            {
                SetInt("DSS2_SubsMode", value);
            }
        }

        //DSS2: отразить по вертикали
        public static bool DSS2_FlipV
        {
            get
            {
                object value = GetValue("DSS2_FlipV");
                if (value == null) return false;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("DSS2_FlipV", value);
            }
        }

        //DSS2: отразить по горизонтали
        public static bool DSS2_FlipH
        {
            get
            {
                object value = GetValue("DSS2_FlipH");
                if (value == null) return false;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("DSS2_FlipH", value);
            }
        }

        //DSS2: предзагрузка ("недосикинг")
        public static int DSS2_Preroll
        {
            get
            {
                object value = GetValue("DSS2_Preroll");
                if (value == null) return 15;
                else return Convert.ToInt32(value);
            }
            set
            {
                SetInt("DSS2_Preroll", value);
            }
        }

        //Разрешить декодирование звука через FFmpegSource
        public static bool FFMS_Enable_Audio
        {
            get
            {
                object value = GetValue("FFMS_Enable_Audio");
                if (value == null) return false;
                else return Convert.ToBoolean(value);
            }
            set
            {
                SetBool("FFMS_Enable_Audio", value);
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

        public static DeinterlaceType Deint_Film
        {
            get
            {
                object value = GetValue("Deint_Film");
                if (value == null)
                    return DeinterlaceType.TIVTC;
                else
                    return (DeinterlaceType)Enum.Parse(typeof(DeinterlaceType), value.ToString());
            }
            set
            {
                SetString("Deint_Film", value.ToString());
            }
        }

        public static DeinterlaceType Deint_Interlaced
        {
            get
            {
                object value = GetValue("Deint_Interlaced");
                if (value == null)
                    return DeinterlaceType.Yadif;
                else
                    return (DeinterlaceType)Enum.Parse(typeof(DeinterlaceType), value.ToString());
            }
            set
            {
                SetString("Deint_Interlaced", value.ToString());
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

        public static bool FFMS_IndexInTemp
        {
            get
            {
                object value = GetValue("FFMS_IndexInTemp");
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
                SetBool("FFMS_IndexInTemp", value);
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

        public static bool x262_PSNR
        {
            get
            {
                object value = GetValue("x262_PSNR");
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
                SetBool("x262_PSNR", value);
            }
        }

        public static bool x262_SSIM
        {
            get
            {
                object value = GetValue("x262_SSIM");
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
                SetBool("x262_SSIM", value);
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
                return GetDouble("VolumeLevel", 1.0);
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

        //Всплывающие подсказки к фильтрам при раскрытом списке
        public static bool ShowFToolTips
        {
            get
            {
                object value = GetValue("ShowFToolTips");
                if (value == null)
                {
                    return (Language == "Russian");
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("ShowFToolTips", value);
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

        //Пересчет значений при ручном кропе
        public static AspectResolution.CropRecalculate CropRecalculate
        {
            get
            {
                object value = GetValue("CropRecalculate");
                if (value == null)
                {
                    return AspectResolution.CropRecalculate.Aspect;
                }
                else
                {
                    return (AspectResolution.CropRecalculate)Enum.Parse(typeof(AspectResolution.CropRecalculate), value.ToString(), true);
                }
            }
            set
            {
                SetString("CropRecalculate", value.ToString());
            }
        }

        //Переиндексация для FFmpegSource2
        public static bool FFMS_Reindex
        {
            get
            {
                object value = GetValue("FFMS_Reindex");
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
                SetBool("FFMS_Reindex", value);
            }
        }

        //Извлекать таймкоды при индексации (FFmpegSource2)
        public static bool FFMS_TimeCodes
        {
            get
            {
                object value = GetValue("FFMS_TimeCodes");
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
                SetBool("FFMS_TimeCodes", value);
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
                    return "avi/divx/wmv/mpg/mpeg/mod/asf/mkv/mka/mov/qt/3gp/mp4/ogm/avs/vob/ts/m2t/m2v/d2v/m2ts/flv/pmp/h264/264/evo/vdr/dpg/wav/ac3/dts/mpa/mp3/mp2/wma/m4a/aac/ogg/aiff/aif/flac/ape";
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

        //Авто ForceFilm при индексации DGIndex`ом
        public static bool DGForceFilm
        {
            get
            {
                object value = GetValue("DGForceFilm");
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
                SetBool("DGForceFilm", value);
            }
        }

        //Процент Film для Auto ForceFilm
        public static int DGFilmPercent
        {
            get
            {
                object value = GetValue("DGFilmPercent");
                if (value == null) return 95;
                else return Convert.ToInt32(value);
            }
            set
            {
                SetInt("DGFilmPercent", value);
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

        public static bool VCropFullscreen
        {
            get
            {
                object value = GetValue("VCropFullscreen");
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
                SetBool("VCropFullscreen", value);
            }
        }

        public static Color VCropBrush
        {
            get
            {
                object value = GetValue("VCropBrush");
                if (value != null)
                {
                    try { return (Color)ColorConverter.ConvertFromString(Convert.ToString(value)); }
                    catch (FormatException) { }
                }

                return new Color() { A = 200, R = 255, G = 255, B = 255 }; //#C8FFFFFF
            }
            set
            {
                SetString("VCropBrush", Convert.ToString(value));
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
                object value = GetValue("BatchPause");
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
                SetBool("BatchPause", value);
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

        //Новый способ определения задержки (относительно видео-трека, эквивалент "Video delay" в MediaInfo).
        public static bool NewDelayMethod
        {
            get
            {
                object value = GetValue("NewDelayMethod");
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
                SetBool("NewDelayMethod", value);
            }
        }

        public static bool CopyDelay
        {
            get
            {
                object value = GetValue("CopyDelay");
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
                SetBool("CopyDelay", value);
            }
        }

        public static bool UseAVS4x264
        {
            get
            {
                object value = GetValue("UseAVS4x264");
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
                SetBool("UseAVS4x264", value);
            }
        }

        public static string HotKeys
        {
            get
            {
                object value = GetValue("HotKeys");
                if (value == null || Convert.ToString(value) == "")
                {
                    return "Open file(s)=Ctrl+O; Open folder=Ctrl+Alt+O; Open DVD folder=Ctrl+D; Decode file=Ctrl+Alt+D; Join file=Ctrl+J; Close file=Ctrl+C; Save task=Ctrl+S; Save frame=Ctrl+F; Save THM frame=Ctrl+Alt+F; Refresh preview=Shift+R; VDemux=Shift+V; " +
                        "Decoding=D; Detect black borders=Shift+B; Detect interlace=Shift+I; Color correction=C; Resolution/Aspect=R; Interlace/Framerate=I; VEncoding settings=V; ADemux=Shift+A; Save to WAV=W; Editing options=Ctrl+A; AEncoding settings=A; Add subtitles=Insert; " +
                        "Remove subtitles=Delete; AvsP editor=E; Edit filtering script=S; Apply test script=Ctrl+T; Save script=Shift+S; Run script=Ctrl+R; MT settings=Shift+T; Windows Media Player=Shift+M; Media Player Classic=M; WPF Video Player=Ctrl+M; Global settings=G; Media Info=F1; " +
                        "FFRebuilder=F2; DGIndex=F4; DGPulldown=F5; DGAVCIndex=F6; VirtualDubMod=F7; AVI-Mux=F8; tsMuxeR=F9; MKVExtract=F10; MKVMerge=F11; Yamb=F12; Frame forward=Right; Frame back=Left; 10 frames forward=Ctrl+Right; 10 frames backward=Ctrl+Left; 100 frames forward=Ctrl+Up; " +
                        "100 frames backward=Ctrl+Down; 30 sec. forward=Shift+Right; 30 sec. backward=Shift+Left; 3 min. forward=Shift+Up; 3 min. backward=Shift+Down; Play-Pause=Space; Fullscreen=Esc; Volume+=Up; Volume-=Down; Set Start=Home; Set End=End; Next/New region=PageUp; " +
                        "Previous region=Next; Apply Trim=T; Add/Remove bookmark=Ctrl+B; Edit format=F;";
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

        public static bool FFMS_AssumeFPS
        {
            get
            {
                object value = GetValue("FFMS_AssumeFPS");
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
                SetBool("FFMS_AssumeFPS", value);
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

        public static int RecentFilesCount
        {
            get
            {
                object value = GetValue("RecentFilesCount");
                if (value == null)
                {
                    return 5;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("RecentFilesCount", value);
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

        //Разрешаем иконку в трее
        public static bool TrayIconIsEnabled
        {
            get
            {
                object value = GetValue("TrayIconIsEnabled");
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
                SetBool("TrayIconIsEnabled", value);
            }
        }

        //В трей при закрытии программы
        public static bool TrayClose
        {
            get
            {
                object value = GetValue("TrayClose");
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
                SetBool("TrayClose", value);
            }
        }
        
        //В трей при сворачивании окна
        public static bool TrayMinimize
        {
            get
            {
                object value = GetValue("TrayMinimize");
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
                SetBool("TrayMinimize", value);
            }
        }

        //Одинарный клик по иконке для восстановления окна
        public static bool TrayClickOnce
        {
            get
            {
                object value = GetValue("TrayClickOnce");
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
                SetBool("TrayClickOnce", value);
            }
        }

        //Запретить всплывающие сообщения в трее
        public static bool TrayNoBalloons
        {
            get
            {
                object value = GetValue("TrayNoBalloons");
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
                SetBool("TrayNoBalloons", value);
            }
        }

        //Рендерер для DirectShow движка превью
        public static VRenderers VideoRenderer
        {
            get
            {
                object value = GetValue("VRenderer");
                if (value == null)
                {
                    return VRenderers.Auto;
                }
                else
                {
                    return (VRenderers)Enum.Parse(typeof(VRenderers), value.ToString(), true);
                }
            }
            set
            {
                SetString("VRenderer", value.ToString());
            }
        }

        //Кодировать сначала звук, потом видео
        public static bool EncodeAudioFirst
        {
            get
            {
                object value = GetValue("EncodeAudioFirst");
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
                SetBool("EncodeAudioFirst", value);
            }
        }

        public static int XviD_Threads
        {
            get
            {
                object value = GetValue("XviD_Threads");
                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("XviD_Threads", value);
            }
        }

        //Charset для сообщений mkvmerge, mkvextract, mkvinfo
        public static string MKVToolnix_Charset
        {
            get
            {
                object value = GetValue("MKVToolnix_Charset");
                if (value == null)
                {
                    SetString("MKVToolnix_Charset", "");
                    return "";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("MKVToolnix_Charset", value);
            }
        }

        //Обрабатывать звук при открытии файлов
        public static bool EnableAudio
        {
            get
            {
                object value = GetValue("EnableAudio");
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
                SetBool("EnableAudio", value);
            }
        }

        //Анализировать % (SourceDetector)
        public static double SD_Analyze
        {
            get
            {
                return GetDouble("SD_Analyze", 1.0);
            }
            set
            {
                SetDouble("SD_Analyze", value);
            }
        }

        //Минимум секций (блоков по 5 кадров) для анализа (SourceDetector)
        public static int SD_Min_Sections
        {
            get
            {
                object value = GetValue("SD_Min_Sections");
                if (value == null)
                {
                    return 150;
                }
                return Convert.ToInt32(value);
            }
            set
            {
                SetInt("SD_Min_Sections", value);
            }
        }

        //Порог для Hybrid Interlace % (SourceDetector)
        public static int SD_Hybrid_Int
        {
            get
            {
                object value = GetValue("SD_Hybrid_Int");
                if (value == null)
                {
                    return 5;
                }
                return Convert.ToInt32(value);
            }
            set
            {
                SetInt("SD_Hybrid_Int", value);
            }
        }

        //Порог для Hybrid FieldOrder % (SourceDetector)
        public static int SD_Hybrid_FO
        {
            get
            {
                object value = GetValue("SD_Hybrid_FO");
                if (value == null)
                {
                    return 10;
                }
                return Convert.ToInt32(value);
            }
            set
            {
                SetInt("SD_Hybrid_FO", value);
            }
        }

        //Выборочный анализ полей (SourceDetector)
        public static bool SD_Portions_FO
        {
            get
            {
                object value = GetValue("SD_Portions_FO");
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
                SetBool("SD_Portions_FO", value);
            }
        }

        //Пресет для FFRebuilder`а
        public static string FFRebuilder_Profile
        {
            get
            {
                object value = GetValue("FFRebuilder_Profile");
                if (value == null)
                {
                    return "Default";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("FFRebuilder_Profile", value);
            }
        }

        //Путь до Windows Media Player
        public static string WMP_Path
        {
            get
            {
                object value = GetValue("WMP_Path");
                if (value == null || value.ToString() == "")
                {
                    return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Windows Media Player\\wmplayer.exe";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("WMP_Path", value);
            }
        }

        //Путь до Media Player Classic
        public static string MPC_Path
        {
            get
            {
                object value = GetValue("MPC_Path");
                if (value == null || value.ToString() == "")
                {
                    string path = "";

                    //Сначала пробуем найти путь к плейеру через реестр
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Gabest\Media Player Classic", false))
                    {
                        if (key != null)
                        {
                            path = key.GetValue("ExePath", "").ToString();
                            if (File.Exists(path) && !Path.GetFileName(path).Contains("64")) goto done;
                        }
                    }

                    //Если не вышло, ищем во всех возможных местах
                    if (!File.Exists(path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\K-Lite Codec Pack\\Media Player Classic\\mpc-hc.exe"))
                        if (!File.Exists(path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Media Player Classic - Home Cinema\\mpc-hc.exe"))
                            if (!File.Exists(path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\MPC HomeCinema\\mpc-hc.exe"))
                                if (!File.Exists(path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\K-Lite Codec Pack\\Media Player Classic\\mplayerc.exe"))
                                    if (!File.Exists(path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Media Player Classic\\mplayerc.exe"))
                                        return "";

                    done:
                    SetString("MPC_Path", path);
                    return path;
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("MPC_Path", value);
            }
        }

        //Путь до WPF_VideoPlayer
        public static string WPF_Path
        {
            get
            {
                object value = GetValue("WPF_Path");
                if (value == null || value.ToString() == "")
                {
                    return Calculate.StartupPath + "\\WPF_VideoPlayer.exe";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("WPF_Path", value);
            }
        }

        //Выбор версии XviD (true = новая 1.3.0)
        public static bool UseXviD_130
        {
            get
            {
                object value = GetValue("UseXviD_130");
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
                SetBool("UseXviD_130", value);
            }
        }

        //Поддержка таскбара в Windows 7
        public static bool Win7TaskbarIsEnabled
        {
            get
            {
                object value = GetValue("Win7TaskbarIsEnabled");
                if (value == null)
                {
                    OperatingSystem osInfo = Environment.OSVersion;
                    return ((osInfo.Version.Major == 6 && osInfo.Version.Minor >= 1) || (osInfo.Version.Major > 6));
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("Win7TaskbarIsEnabled", value);
            }
        }

        //Создавать резервную копию списка заданий
        public static bool EnableBackup
        {
            get
            {
                object value = GetValue("EnableBackup");
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
                SetBool("EnableBackup", value);
            }
        }

        //Проверять пути на "нехорошие" символы
        public static bool ValidatePathes
        {
            get
            {
                object value = GetValue("ValidatePathes");
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
                SetBool("ValidatePathes", value);
            }
        }

        //DRC для NicAC3Source
        public static bool NicAC3_DRC
        {
            get
            {
                object value = GetValue("NicAC3_DRC");
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
                SetBool("NicAC3_DRC", value);
            }
        }

        //DRC для NicDTSSource
        public static bool NicDTS_DRC
        {
            get
            {
                object value = GetValue("NicDTS_DRC");
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
                SetBool("NicDTS_DRC", value);
            }
        }

        //QTGMC Preset
        public static string QTGMC_Preset
        {
            get
            {
                object value = GetValue("QTGMC_Preset");
                if (value == null)
                {
                    return "Slow";
                }
                else
                {
                    return Convert.ToString(value);
                }
            }
            set
            {
                SetString("QTGMC_Preset", value);
            }
        }

        //QTGMC Sharpness
        public static double QTGMC_Sharpness
        {
            get
            {
                return GetDouble("QTGMC_Sharpness", 1.0);
            }
            set
            {
                SetDouble("QTGMC_Sharpness", value);
            }
        }

        //Помечать интерлейсные кадры (Hybrid Progressive Interlaced)
        public static bool IsCombed_Mark
        {
            get
            {
                object value = GetValue("IsCombed_Mark");
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
                SetBool("IsCombed_Mark", value);
            }
        }

        //IsCombed CThresh (Hybrid Progressive Interlaced)
        public static int IsCombed_CThresh
        {
            get
            {
                object value = GetValue("IsCombed_CThresh");
                if (value == null)
                {
                    return 7;
                }
                return Convert.ToInt32(value);
            }
            set
            {
                SetInt("IsCombed_CThresh", value);
            }
        }

        //IsCombed MI (Hybrid Progressive Interlaced)
        public static int IsCombed_MI
        {
            get
            {
                object value = GetValue("IsCombed_MI");
                if (value == null)
                {
                    return 40;
                }
                return Convert.ToInt32(value);
            }
            set
            {
                SetInt("IsCombed_MI", value);
            }
        }

        //Кол-во потоков для FFmpegSource2
        public static int FFMS_Threads
        {
            get
            {
                object value = GetValue("FFMS_Threads");
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
                SetInt("FFMS_Threads", value);
            }
        }

        //Использовать AR из потока (Original AR в MediaInfo)
        public static bool MI_Original_AR
        {
            get
            {
                object value = GetValue("MI_Original_AR");
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
                SetBool("MI_Original_AR", value);
            }
        }

        //Использовать fps из потока (Original fps в MediaInfo)
        public static bool MI_Original_fps
        {
            get
            {
                object value = GetValue("MI_Original_fps");
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
                SetBool("MI_Original_fps", value);
            }
        }

        //Разрешить нестандартные fps на выход (отключает автоподгонку fps до ближайшего стандартного значения)
        public static bool Nonstandard_fps
        {
            get
            {
                object value = GetValue("Nonstandard_fps");
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
                SetBool("Nonstandard_fps", value);
            }
        }

        //Переключает автокроп из режима обрезки по наименьшим значениям в режим
        //обрезки по наиболее часто встречающимся значениям (при анализе от 5-ти кадров)
        public static bool AutocropMostCommon
        {
            get
            {
                object value = GetValue("AutocropMostCommon");
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
                SetBool("AutocropMostCommon", value);
            }
        }

        //Перенос строк в окне MediaInfo
        public static bool MI_WrapText
        {
            get
            {
                object value = GetValue("MI_WrapText");
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
                SetBool("MI_WrapText", value);
            }
        }

        //Отмена кодирования зависших заданий
        public static bool AutoAbortEncoding
        {
            get
            {
                object value = GetValue("AutoAbortEncoding");
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
                SetBool("AutoAbortEncoding", value);
            }
        }

        //Режим автовыбора звуковой дорожки
        public static ATrackModes DefaultATrackMode
        {
            get
            {
                object value = GetValue("DefaultATrackMode");
                if (value == null)
                    return ATrackModes.Manual;
                else
                    return (ATrackModes)Enum.Parse(typeof(ATrackModes), value.ToString(), true);
            }
            set
            {
                SetString("DefaultATrackMode", value.ToString());
            }
        }

        //Язык дорожки для автовыбора
        public static string DefaultATrackLang
        {
            get
            {
                object value = GetValue("DefaultATrackLang");
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
                SetString("DefaultATrackLang", value);
            }
        }

        //Номер дорожки для автовыбора (отсчет с 1)
        public static int DefaultATrackNum
        {
            get
            {
                object value = GetValue("DefaultATrackNum");
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
                SetInt("DefaultATrackNum", value);
            }
        }

        //Ширина первой (левой) колонки в логе MediaInfo
        public static string MI_ColumnSize
        {
            get
            {
                object value = GetValue("MI_ColumnSize");
                if (value == null)
                {
                    SetString("MI_ColumnSize", "35");
                    return "35";
                }
                else
                {
                    return value.ToString();
                }
            }
            set
            {
                SetString("MI_ColumnSize", value);
            }
        }

        //Закрывать ScriptRunner при завершении прогона скрипта
        public static bool CloseScriptRunner
        {
            get
            {
                object value = GetValue("CloseScriptRunner");
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
                SetBool("CloseScriptRunner", value);
            }
        }

        //Предупреждение в окне MT-настроек
        public static bool MTSettings_Warning
        {
            get
            {
                object value = GetValue("MTSettings_Warning");
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
                SetBool("MTSettings_Warning", value);
            }
        }

        //Значение для SetMemoryMax
        public static int SetMemoryMax
        {
            get
            {
                object value = GetValue("SetMemoryMax");
                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("SetMemoryMax", value);
            }
        }

        //MTMode перед импортом видео
        public static int SetMTMode_1
        {
            get
            {
                object value = GetValue("SetMTMode_1");
                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("SetMTMode_1", value);
            }
        }

        //MTMode после импорта видео
        public static int SetMTMode_2
        {
            get
            {
                object value = GetValue("SetMTMode_2");
                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("SetMTMode_2", value);
            }
        }

        //Кол-во потоков для SetMTMode
        public static int SetMTMode_Threads
        {
            get
            {
                object value = GetValue("SetMTMode_Threads");
                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("SetMTMode_Threads", value);
            }
        }

        //Режим внутренней обработки многопоточности
        public static MTMode MTMode_Internal
        {
            get
            {
                object value = GetValue("MTMode_Internal");
                if (value == null)
                {
                    return MTMode.Undefined;
                }
                else
                {
                    return (MTMode)Enum.Parse(typeof(MTMode), value.ToString(), true);
                }
            }
            set
            {
                SetString("MTMode_Internal", value.ToString());
            }
        }

        //Сортировка пресетов по времени изменений
        public static bool SortPresetsByTime
        {
            get
            {
                object value = GetValue("SortPresetsByTime");
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
                SetBool("SortPresetsByTime", value);
            }
        }

        //Пропуск кадров для PictureView
        public static bool PictureViewDropFrames
        {
            get
            {
                object value = GetValue("PictureViewDropFrames");
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
                SetBool("PictureViewDropFrames", value);
            }
        }

        //Звук для PictureView
        public static bool PictureViewAudio
        {
            get
            {
                object value = GetValue("PictureViewAudio");
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
                SetBool("PictureViewAudio", value);
            }
        }

        //Ограничение размеров и позиции окон
        public static bool CheckWindowsPos
        {
            get
            {
                object value = GetValue("CheckWindowsPos");
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
                SetBool("CheckWindowsPos", value);
            }
        }

        //Принудительно фиксировать fps для LSMASH
        public static bool LSMASH_AssumeFPS
        {
            get
            {
                object value = GetValue("LSMASH_AssumeFPS");
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
                SetBool("LSMASH_AssumeFPS", value);
            }
        }

        //Разрешить декодирование звука через LSMASH
        public static bool LSMASH_Enable_Audio
        {
            get
            {
                object value = GetValue("LSMASH_Enable_Audio");
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
                SetBool("LSMASH_Enable_Audio", value);
            }
        }

        //Кол-во потоков для LSMASH
        public static int LSMASH_Threads
        {
            get
            {
                object value = GetValue("LSMASH_Threads");
                if (value == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            set
            {
                SetInt("LSMASH_Threads", value);
            }
        }

        //Удалять индекс-файлы LSMASH (LWLibav)
        public static bool DeleteLSMASHCache
        {
            get
            {
                object value = GetValue("DeleteLSMASHCache");
                if (value == null)
                {
                    return false; //*.lwi вроде как еще и файл некоего LightWorks
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }
            set
            {
                SetBool("DeleteLSMASHCache", value);
            }
        }
    }
}
