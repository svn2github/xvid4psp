using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

namespace XviD4PSP
{
    public enum MatrixTypes { TXT, CQM };

    public class PresetLoader
    {
        public static ArrayList CustomMatrixes(MatrixTypes mtype)
        {
            ArrayList list = new ArrayList();
            string type = mtype.ToString().ToLower();
            foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\matrix\\" + type, ("*." + type)))
            {
                list.Add(Path.GetFileNameWithoutExtension(file));
            }
            return list;
        }

        public static string GetInterMatrix(string name)
        {
            string inter = null;
            string mpath = Calculate.StartupPath + "\\presets\\matrix\\txt\\" + name + ".txt";

            if (File.Exists(mpath))
            {
                using (StreamReader sr = new StreamReader(mpath, System.Text.Encoding.Default))
                {
                    string line;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.StartsWith("inter_matrix="))
                        {
                            string[] separator = new string[] { "=" };
                            string[] a = line.Split(separator, StringSplitOptions.None);
                            return a[1];
                        }
                    }
                }
            }
            return inter;
        }

        public static string GetIntraMatrix(string name)
        {
            string intra = null;
            string mpath = Calculate.StartupPath + "\\presets\\matrix\\txt\\" + name + ".txt";

            if (File.Exists(mpath))
            {
                using (StreamReader sr = new StreamReader(mpath, System.Text.Encoding.Default))
                {
                    string line;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line.StartsWith("intra_matrix="))
                        {
                            string[] separator = new string[] { "=" };
                            string[] a = line.Split(separator, StringSplitOptions.None);
                            return a[1];
                        }
                    }
                }
            }
            return intra;
        }

        public static ArrayList GetVCodecPasses(Massive m)
        {
            if (m.vencoding == "Copy" ||
                m.vencoding == "Disabled")
            {
                ArrayList passlist = new ArrayList();
                passlist.Add("stream copy or disable");
                return passlist;
            }
            else
            {
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\video\\" + m.vencoding + ".txt", System.Text.Encoding.Default))
                {
                    string line;
                    ArrayList passlist = new ArrayList();
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line == "video cli:")
                        {
                            while (line != "" && !sr.EndOfStream)
                            {
                                line = sr.ReadLine();
                                if (line == "")
                                    break;
                                passlist.Add(line);
                            }
                            break;
                        }
                    }
                    return passlist;
                }
            }
        }

        public static string GetVCodec(Massive m)
        {
            if (m.vencoding == "Copy" ||
                m.vencoding == "Disabled")
                return m.vencoding;
            else
            {
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\video\\" + m.vencoding + ".txt", System.Text.Encoding.Default))
                {
                    string line;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line == "video codec:")
                        {
                            return sr.ReadLine();
                        }
                    }
                    return null;
                }
            }
        }

        public static void CreateVProfile(Massive m)
        {
            using (StreamWriter sw = new StreamWriter(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\video\\" + m.vencoding + ".txt", false, System.Text.Encoding.Default))
            {
                sw.WriteLine("video codec:");
                sw.WriteLine(m.outvcodec);

                sw.WriteLine();

                sw.WriteLine("video cli:");
                foreach (string line in m.vpasses)
                    sw.WriteLine(line);

                sw.Close();
            }
        }

        public static string GetACodec(Format.ExportFormats format, string encodingpreset)
        {
            if (encodingpreset == "Copy" ||
                encodingpreset == "Disabled")
                return encodingpreset;
            else
            {
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(format) + "\\audio\\" + encodingpreset + ".txt", System.Text.Encoding.Default))
                {
                    string line;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line == "audio codec:" && !sr.EndOfStream)
                        {
                            return sr.ReadLine();
                        }
                    }
                    return null;
                }
            }
        }

        public static string GetACodecPasses(Massive m)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            if (outstream.encoding == "Copy" ||
                outstream.encoding == "Disabled")
                return "stream copy or disable";
            else
            {
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\audio\\" + outstream.encoding + ".txt", System.Text.Encoding.Default))
                {
                    string line;
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        if (line == "audio cli:" && !sr.EndOfStream)
                        {
                            return sr.ReadLine();
                        }
                    }
                    return null;
                }
            }
        }

        public static void CreateAProfile(Massive m)
        {
            AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

            using (StreamWriter sw = new StreamWriter(Calculate.StartupPath + "\\presets\\encoding\\" + Format.EnumToString(m.format) + "\\audio\\" + outstream.encoding + ".txt", false, System.Text.Encoding.Default))
            {
                sw.WriteLine("audio codec:");
                sw.WriteLine(outstream.codec);

                sw.WriteLine();

                sw.WriteLine("audio cli:");
                sw.WriteLine(outstream.passes);

                sw.Close();
            }
        }

        public static Massive DecodePresets(Massive m)
        {
            //расшифровываем видео параметры
            if (m.outvcodec == "x265") m = x265.DecodeLine(m);
            else if (m.outvcodec == "x264") m = x264.DecodeLine(m);
            else if (m.outvcodec == "x262") m = x262.DecodeLine(m);
            else if (m.outvcodec == "XviD") m = XviD.DecodeLine(m);
            else if (m.outvcodec == "MPEG2") m = FMPEG2.DecodeLine(m);
            else if (m.outvcodec == "MPEG1") m = FMPEG1.DecodeLine(m);
            else if (m.outvcodec == "MPEG4") m = FMPEG4.DecodeLine(m);
            else if (m.outvcodec == "DV") m = FDV.DecodeLine(m);
            else if (m.outvcodec == "HUFF") m = FFHUFF.DecodeLine(m);
            else if (m.outvcodec == "MJPEG") m = FMJPEG.DecodeLine(m);
            else if (m.outvcodec == "FFV1") m = FFV1.DecodeLine(m);
            else if (m.outvcodec == "FLV1") m = FLV1.DecodeLine(m);

            if (m.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                //расшифровываем audio параметры
                if (outstream.codec == "AAC") m = NeroAAC.DecodeLine(m);
                else if (outstream.codec == "QAAC") m = QuickTimeAAC.DecodeLine(m);
                else if (outstream.codec == "MP3") m = LameMP3.DecodeLine(m);
                else if (outstream.codec == "AC3") m = AftenAC3.DecodeLine(m);
                else if (outstream.codec == "MP2") m = FMP2.DecodeLine(m);
                else if (outstream.codec == "PCM") m = FPCM.DecodeLine(m);
                else if (outstream.codec == "LPCM") m = FLPCM.DecodeLine(m);
                else if (outstream.codec == "FLAC") m = FFLAC.DecodeLine(m);
            }

            return m;
        }
    }
}
