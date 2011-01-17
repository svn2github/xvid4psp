using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

namespace SafeOpenDialog
{
    class Program
    {
        [STAThreadAttribute]
        static void Main(string[] args)
        {
            try
            {
                try
                {
                    //Кодировка stdout
                    if (args.Length == 2 && args[0].ToLowerInvariant() != "ff")
                    {
                        int page;
                        Console.OutputEncoding = (Int32.TryParse(args[1], out page) ? Encoding.GetEncoding(page) : Encoding.GetEncoding(args[1]));
                    }
                    else if (args.Length == 3 && args[0].ToLowerInvariant() == "ff")
                    {
                        int page;
                        Console.OutputEncoding = (Int32.TryParse(args[2], out page) ? Encoding.GetEncoding(page) : Encoding.GetEncoding(args[2]));
                    }
                    else
                    {
                        //Дефолтный вариант
                        Console.OutputEncoding = Encoding.UTF8; //Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                    }
                }
                catch
                {
                    Console.OutputEncoding = Encoding.Default;
                }

                if (args.Length > 0)
                {
                    string type = args[0].ToLowerInvariant();

                    if (type.StartsWith("ov")) //диалог выбора видео файла(ов)
                    {
                        //быстро переводим фразы
                        ArrayList phrases = new ArrayList();
                        phrases.Add("All video files"); //0
                        phrases.Add("files"); //1
                        phrases.Add("All files"); //2
                        phrases.Add("Select video files"); //3
                        phrases.Add("All audio files"); //4
                        phrases = Languages.Translate(phrases);

                        OpenFileDialog o = new OpenFileDialog();
                        o.Filter = phrases[0] + "|*.avi;*.divx;*.wmv;*.mpg;*.mpe;*.mpeg;*.mod;*.asf;*.mkv;*.mov;*.qt;*.3gp;*.hdmov;*.mp4;*.ogm;*.avs;*.vob;*.dvr-ms;*.ts;*.m2p;*.m2t;*.m2v;*.d2v;*.m2ts;*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil;*.flv;*.pmp;*.h264;*.264;*.vro;*.dga;*.dgi|" +
                            phrases[4] + "|*.wav;*.ac3;*.dts;*.mpa;*.mp3;*.mp2;*.wma;*.m4a;*.aac;*.ogg;*.aiff;*.aif;*.flac;*.wv;*.spx;*.mpc;*.mpp;*.ape;*.mo3;*.xm;*.it;*.mod;*.s3m;*.mtm;*.umx;*.cda|" +
                            "AVI " + phrases[1] + " (AVI DIVX ASF)|*.avi;*.divx;*.asf|" +
                            "RAW AVC " + phrases[1] + " (H264 264)|*.h264;*.264|" +
                            "MPEG " + phrases[1] + " (MPG MPE MPEG VOB MOD TS M2P M2T M2TS VRO)|*.mpg;*.mpe;*.mpeg;*.vob;*.ts;*.m2p;*.m2t;*.mod;*.m2ts;*.vro|" +
                            "DGIndex " + phrases[1] + " (D2V)|*.d2v|" +
                            "DGIndexNV " + phrases[1] + " (DGI)|*.dgi|" +
                            "DGAVCIndex " + phrases[1] + " (DGA)|*.dga|" +
                            "QuickTime " + phrases[1] + " (MOV QT 3GP HDMOV)|*.mov;*.qt;*.3gp;*.hdmov|" +
                            "RealVideo " + phrases[1] + " (RM RAM RMVB RPX SMI SMIL)|*.rm;*.ram;*.rmvb;*.rpx;*.smi;*.smil|" +
                            "Matroska " + phrases[1] + " (MKV)|*.mkv|" +
                            "OGG Media (Vorbis) " + phrases[1] + " (OGM)|*.ogm|" +
                            "Windows Media " + phrases[1] + " (WMV)|*.wmv|" +
                            "Media Center " + phrases[1] + " (DVR-MS)|*.dvr-ms|" +
                            "PMP " + phrases[1] + " (PMP)|*.pmp|" +
                            "Flash Video " + phrases[1] + " (FLV)|*.flv|" +
                            "Windows PCM " + phrases[1] + " (WAV)|*.wav|" +
                            "AC3 " + phrases[1] + " |*.ac3|" +
                            "DTS " + phrases[1] + " |*.dts|" +
                            "MPA " + phrases[1] + " |*.mpa|" +
                            "MP2 " + phrases[1] + " |*.mp2|" +
                            "MP3 " + phrases[1] + " |*.mp3|" +
                            "WMA " + phrases[1] + " |*.wma|" +
                            "AAC " + phrases[1] + " |*.aac;*.m4a|" +
                            "OGG " + phrases[1] + " |*.ogg|" +
                            "AIFF " + phrases[1] + " |*.aiff;*.aif|" +
                            "FLAC " + phrases[1] + " |*.flac|" +
                            "WavPack " + phrases[1] + " |*.wv|" +
                            "Speex " + phrases[1] + " |*.spx|" +
                            "Musepack " + phrases[1] + " |*.mpc;*.mpp|" +
                            "APE " + phrases[1] + " |*.ape|" +
                            "CDAudio " + phrases[1] + " |*.cda|" +
                            "Tracker " + phrases[1] + " |*.mo3;*.xm;*.it;*.mod;*.s3m;*.mtm;*.umx|" +
                            "Tasks " + phrases[1] + " |*.tsks|" +
                            phrases[2] + " (*.*)|*.*";
                        o.Multiselect = (type == "ovm");
                        o.Title = phrases[3] + ":";

                        if (o.ShowDialog() == DialogResult.OK)
                        {
                            if (o.Multiselect)
                            {
                                foreach (string file in o.FileNames)
                                    Console.WriteLine(file);
                            }
                            else
                                Console.WriteLine(o.FileName);
                        }
                    }
                    else if (type.StartsWith("oa")) //диалог выбора аудио файла(ов)
                    {
                        //быстро переводим фразы
                        ArrayList phrases = new ArrayList();
                        phrases.Add("All audio files"); //0
                        phrases.Add("files"); //1
                        phrases.Add("All files"); //2
                        phrases.Add("Select audio files"); //3
                        phrases = Languages.Translate(phrases);

                        OpenFileDialog o = new OpenFileDialog();
                        o.Filter = phrases[0] + "|*.wav;*.ac3;*.dts;*.mpa;*.mp3;*.mp2;*.wma;*.m4a;*.aac;*.ogg;*.aiff;*.aif;*.flac;*.wv;*.spx;*.mpc;*.mpp;*.ape;*.mo3;*.xm;*.it;*.mod;*.s3m;*.mtm;*.umx;*.cda|" +
                            "Windows PCM " + phrases[1] + " (WAV)|*.wav|" +
                            "AC3 " + phrases[1] + " |*.ac3|" +
                            "DTS " + phrases[1] + " |*.dts|" +
                            "MPA " + phrases[1] + " |*.mpa|" +
                            "MP2 " + phrases[1] + " |*.mp2|" +
                            "MP3 " + phrases[1] + " |*.mp3|" +
                            "WMA " + phrases[1] + " |*.wma|" +
                            "AAC " + phrases[1] + " |*.aac;*.m4a|" +
                            "OGG " + phrases[1] + " |*.ogg|" +
                            "AIFF " + phrases[1] + " |*.aiff;*.aif|" +
                            "FLAC " + phrases[1] + " |*.flac|" +
                            "WavPack " + phrases[1] + " |*.wv|" +
                            "Speex " + phrases[1] + " |*.spx|" +
                            "Musepack " + phrases[1] + " |*.mpc;*.mpp|" +
                            "APE " + phrases[1] + " |*.ape|" +
                            "CDAudio " + phrases[1] + " |*.cda|" +
                            "Tracker " + phrases[1] + " |*.mo3;*.xm;*.it;*.mod;*.s3m;*.mtm;*.umx|" +
                            phrases[2] + " (*.*)|*.*";
                        o.Multiselect = (type == "oam");
                        o.Title = phrases[3] + ":";

                        if (o.ShowDialog() == DialogResult.OK)
                        {
                            if (o.Multiselect)
                            {
                                foreach (string file in o.FileNames)
                                    Console.WriteLine(file);
                            }
                            else
                                Console.WriteLine(o.FileName);
                        }
                    }
                    else if (type == "ff") //диалог выбора соседних файлов
                    {
                        //быстро переводим фразы
                        ArrayList phrases = new ArrayList();
                        phrases.Add("Friend files"); //0
                        phrases.Add("All files"); //1
                        phrases.Add("Select video files"); //2
                        phrases = Languages.Translate(phrases);

                        OpenFileDialog o = new OpenFileDialog();
                        o.Filter = phrases[0] + "|*" + args[1] + "|" + phrases[1] + " (*.*)|*.*";
                        o.Multiselect = true;
                        o.Title = phrases[2] + ":";

                        if (o.ShowDialog() == DialogResult.OK)
                        {
                            foreach (string file in o.FileNames)
                                Console.WriteLine(file);
                        }
                    }
                    else if (type == "mkv") //диалог выбора mkv файла
                    {
                        //быстро переводим фразы
                        ArrayList phrases = new ArrayList();
                        phrases.Add("All video files"); //0
                        phrases.Add("files"); //1
                        phrases.Add("All files"); //2
                        phrases.Add("Select video files"); //3
                        phrases = Languages.Translate(phrases);

                        OpenFileDialog o = new OpenFileDialog();
                        o.Filter = "Matroska " + phrases[1] + " (MKV)|*.mkv|" + phrases[2] + " (*.*)|*.*";
                        o.Multiselect = false;
                        o.Title = phrases[3] + ":";

                        if (o.ShowDialog() == DialogResult.OK)
                            Console.WriteLine(o.FileName);
                    }
                    else if (type == "sub") //диалог выбора файла субтитров
                    {
                        //быстро переводим фразы
                        ArrayList phrases = new ArrayList();
                        phrases.Add("Subtitles"); //0
                        phrases.Add("All files"); //1
                        phrases.Add("Select subtitles file"); //2
                        phrases = Languages.Translate(phrases);

                        OpenFileDialog o = new OpenFileDialog();
                        o.Filter = phrases[0] + "|*.srt;*.sub;*.idx;*.ssa;*.ass;*.psb;*.smi|" + phrases[1] + " (*.*)|*.*";
                        o.Multiselect = false;
                        o.Title = phrases[2] + ":";

                        if (o.ShowDialog() == DialogResult.OK)
                            Console.WriteLine(o.FileName);
                    }
                }
                else
                {
                    Console.WriteLine("");
                    Console.WriteLine("SafeOpenDialog ov|ovm|oa|oam|ff|mkv|sub ff_extension [output_encoding]");
                    Console.WriteLine("");
                    Console.WriteLine("ov  - select video file");
                    Console.WriteLine("ovm - select video files (multiselect)");
                    Console.WriteLine("oa  - select audio file");
                    Console.WriteLine("oam - select audio files (multiselect)");
                    Console.WriteLine("ff  - select friend files (multiselect)");
                    Console.WriteLine("mkv - select matroska file");
                    Console.WriteLine("sub - select subtitles file");
                    Console.WriteLine("");
                    Console.WriteLine("ff_extension - extension for friend files (ff)");
                    Console.WriteLine("");
                    Console.WriteLine("output_encoding - code page name (or number) for stdout, default: UTF-8");
                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
