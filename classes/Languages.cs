using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace XviD4PSP
{
    static class Languages
    {
        public static string Dictionary = ""; //Словарь

        public static string Translate(string phrase)
        {
            try
            {
                using (StringReader sr = new StringReader(Dictionary))
                {
                    string line;
                    while (true)
                    {
                        line = sr.ReadLine();
                        if (line == null) return phrase;
                        if (line == phrase) return sr.ReadLine();
                    }
                }
            }
            catch (Exception)
            {
                return phrase;
            }
        }
    }
}
