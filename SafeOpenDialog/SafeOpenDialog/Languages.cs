using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Reflection;

namespace SafeOpenDialog
{
   static class Languages
    {

       public static ArrayList Translate(ArrayList phrases)
        {
            //определяем рабочую папку
            string StartupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);

            //узнаём язык программы
            string lang;
            using (RegistryKey myHive = Registry.CurrentUser.OpenSubKey("Software\\Winnydows\\XviD4PSP5", true))
            {
                if (myHive != null)
                {
                    object l = myHive.GetValue("Language");
                    if (l != null)
                        lang = l.ToString();
                    else
                        lang = "English"; 
                }
                else
                    lang = "English"; 
            }

            //получаем фразу
            using (StreamReader sr = new StreamReader(StartupPath + "\\languages\\" + lang + ".txt", System.Text.Encoding.Default))
                {
                    string line;
                    while (sr.EndOfStream == false)
                    {
                        line = sr.ReadLine();

                        int n = 0;
                        foreach (string phrase in phrases)
                        {
                            if (line == phrase)
                            {
                                phrases[n] = sr.ReadLine();
                                break;
                            }
                            n++;
                        }
                    }
                }

            return phrases;
        }


    }
}
