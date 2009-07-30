using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace XviD4PSP
{
   static class Languages
    {

        public static string Translate(string phrase)
        {
            using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\languages\\" + Settings.Language + ".txt", System.Text.Encoding.Default))
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
                }

        }


    }
}
