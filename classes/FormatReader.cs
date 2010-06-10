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
    static class FormatReader
    {
        //Для одиночных значений
        public static string GetFormatInfo(string format, string key)
        {
            try
            {
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\formats\\" + format + ".ini", System.Text.Encoding.Default))
                {
                    while (!sr.EndOfStream)
                    {
                        if (sr.ReadLine() == "[" + key + "]" && !sr.EndOfStream)
                        {
                            return sr.ReadLine().Trim();
                        }
                    }
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        //Для одиночных значений int (с дефолтом)
        public static int GetFormatInfo(string format, string key, int default_value)
        {
            int result = default_value;
            string value = GetFormatInfo(format, key);
            if (!int.TryParse(value, out result)) return default_value;
            else return result;
        }

        //Для одиночных значений bool (с дефолтом)
        public static bool GetFormatInfo(string format, string key, bool default_value)
        {
            string value = GetFormatInfo(format, key);
            if (value != null && value.ToLower() == "true") return true;
            else return default_value;
        }

        //Для одиночных значений string (с дефолтом)
        public static string GetFormatInfo(string format, string key, string default_value)
        {
            string value = GetFormatInfo(format, key);
            return (string.IsNullOrEmpty(value)) ? default_value : value;
        }

        //Для множественных значений string (с дефолтом)
        public static string[] GetFormatInfo(string format, string key, string[] default_value)
        {          
            try
            {
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\formats\\" + format + ".ini", System.Text.Encoding.Default))
                {
                    string[] lines;
                    ArrayList temp_lines = new ArrayList();
                    while (!sr.EndOfStream)
                    {
                        if (sr.ReadLine() == "[" + key + "]" && !sr.EndOfStream)
                        {
                            //Делим строку на подстроки
                            lines = sr.ReadLine().Trim().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length == 0) return default_value;

                            //Избавляемся от пробелов в подстроках, и пересобираем строчку
                            foreach (string ss in lines) temp_lines.Add(ss.Trim());
                            return (String[])temp_lines.ToArray(typeof(string));
                        }
                    }
                    return default_value;
                }
            }
            catch (Exception)
            {
                return default_value;
            }
        }
    }
}
