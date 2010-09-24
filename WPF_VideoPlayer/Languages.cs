using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace WPF_VideoPlayer
{
    internal static class Languages
    {
        public static string Translate(string phrase)
        {
            try
            {
                using (StreamReader reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + @"\languages\" + Settings.Language + ".txt", Encoding.Default))
                {
                    while (!reader.EndOfStream)
                    {
                        if (reader.ReadLine() == phrase)
                        {
                            return reader.ReadLine();
                        }
                    }
                    return phrase;
                }
            }
            catch (Exception)
            {
                return phrase;
            }
        }
    }
}

