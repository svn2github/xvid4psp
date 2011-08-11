using Microsoft.Win32;
using System;
using System.IO;

namespace WPF_VideoPlayer
{
    public static class Settings
    {
        public enum PlayerEngines { DirectShow = 1, MediaBridge = 2 }
        public enum VRenderers { Auto = 0, Overlay, VMR7, VMR9, EVR } 

        private static object GetValue(string Key)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Winnydows\XviD4PSP5", true))
            {
                if (key != null)
                {
                    return key.GetValue(Key);
                }
                return null;
            }
        }

        private static void SetBool(string Key, bool Value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Winnydows\XviD4PSP5");
            key.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            key.Close();
        }

        private static void SetDouble(string Key, double Value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Winnydows\XviD4PSP5");
            key.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            key.Close();
        }

        private static void SetInt(string Key, int Value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Winnydows\XviD4PSP5");
            key.SetValue(Key, Convert.ToString(Value), RegistryValueKind.String);
            key.Close();
        }

        private static void SetString(string Key, string Value)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Winnydows\XviD4PSP5");
            key.SetValue(Key, Value, RegistryValueKind.String);
            key.Close();
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

        public static string Language
        {
            get
            {
                object value = GetValue("Language");
                if (value == null)
                {
                    return "English";
                }
                return Convert.ToString(value);
            }
            set
            {
                SetString("Language", value);
            }
        }

        //Положение окна
        public static string WindowLocation
        {
            get
            {
                object value = GetValue("WPFPlayer_WindowLocation");
                if (value == null)
                {
                    return "600/480/100/100";
                }
                return Convert.ToString(value);
            }

            set
            {
                SetString("WPFPlayer_WindowLocation", value);
            }
        }

        //Движок плейера
        public static PlayerEngines PlayerEngine
        {
            get
            {
                try
                {
                    object value = GetValue("WPFPlayer_Engine");
                    if (value == null)
                    {
                        return PlayerEngines.DirectShow;
                    }
                    return (PlayerEngines)Enum.Parse(typeof(PlayerEngines), value.ToString(), true);
                }
                catch
                {
                    return PlayerEngines.DirectShow;
                }
            }
            set
            {
                SetString("WPFPlayer_Engine", value.ToString());
            }
        }

        //Рендерер для DirectShow превью
        public static VRenderers VideoRenderer
        {
            get
            {
                object value = GetValue("WPFPlayer_VRenderer");
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
                SetString("WPFPlayer_VRenderer", value.ToString());
            }
        }

        //Громкость
        public static double VolumeLevel
        {
            get
            {
                return GetDouble("WPFPlayer_VolumeLevel", 1.0);
            }
            set
            {
                SetDouble("WPFPlayer_VolumeLevel", value);
            }
        }

        //OldSeeking - непрерывное позиционирование
        public static bool OldSeeking
        {
            get
            {
                object value = GetValue("WPFPlayer_OldSeeking");
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
                SetBool("WPFPlayer_OldSeeking", value);
            }
        }
    }
}

