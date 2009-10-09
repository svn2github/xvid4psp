using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace XviD4PSP
{
    static class Languages
    {
        public static string Dictionary; //Словарь
        
        public static string Translate(string phrase)
        {
            try
            {
                string[] separator = new string[] { Environment.NewLine };
                string[] lines = Dictionary.Split(separator, StringSplitOptions.None);
                int n = 0;
                foreach (string line in lines)
                {
                    if (line == phrase)
                    {
                        return lines[n + 1];
                    }
                    n += 1;
                }
            }
            catch { }
            return phrase;

           /*using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\languages\\" + Settings.Language + ".txt", System.Text.Encoding.Default))
            {
                string line;
                while (sr.EndOfStream == false)
                {
                    line = sr.ReadLine();
                    if (line == phrase)
                    {
                        return sr.ReadLine();
                    }
                }
                return phrase;
            }*/
        }
    }
}
