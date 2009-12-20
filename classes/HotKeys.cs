using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace XviD4PSP
{
    static class HotKeys
    {
        public static string[] Data = new string[] { "" }; //Тут хранятся действия и ключи к ним

        public static void FillData()
        {
            try
            {
                Data = Settings.HotKeys.Split(new string[] { ";" }, StringSplitOptions.None);
            }
            catch { }
        }
        
        public static string GetAction(string PressedKeys)
        {
            try
            {
                foreach (string line in Data)
                {
                    if (line.Trim().EndsWith(PressedKeys))
                        return line.Trim().Replace(PressedKeys, "");
                }
            }
            catch { }
            return "";
        }

        public static string GetKeys(string Action)
        {
            try
            {
                foreach (string line in Data)
                {
                    if (line.Trim().StartsWith(Action))
                        return line.Trim().Replace(Action + "=", "");
                }
            }
            catch { }
            return "";
        }
    }
}

