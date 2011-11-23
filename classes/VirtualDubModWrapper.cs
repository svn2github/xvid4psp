using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace XviD4PSP
{
   public class VirtualDubModWrapper
    {
       public static void SetStartUpInfo()
       {
           RegistryKey myHive = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Freeware\\VirtualDubMod", true);
           if (myHive == null)
           {
               myHive = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
               myHive.CreateSubKey("Freeware\\VirtualDubMod");
               myHive = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Freeware\\VirtualDubMod", true);
               myHive.SetValue("VirtualDub", "00000001", RegistryValueKind.DWord);
               myHive.SetValue("SeenWelcome", "00000001", RegistryValueKind.DWord);
               myHive.SetValue("SeenMatroskaDeprecated 1.5.10.2", "00000001", RegistryValueKind.DWord);
           }
           myHive.Close();
       }

       public static void CreateMuxingScript(Massive m, bool CopyDelay)
       {
           string muxer_cli = "", language_a = "";
           if (Formats.GetDefaults(m.format).IsEditable)
           {
               //Получение CLI и расшифровка подставных значений
               muxer_cli = Calculate.ExpandVariables(m, Formats.GetSettings(m.format, "CLI_virtualdubmod", Formats.GetDefaults(m.format).CLI_virtualdubmod));
           }

           using (StreamWriter sw = new StreamWriter(Settings.TempPath + "\\" + m.key + ".vcf", false, System.Text.Encoding.Default))
           {
               sw.WriteLine("VirtualDub.RemoveInputStreams();");
               sw.WriteLine("VirtualDub.Open(\"" + Calculate.GetUTF8String(m.outvideofile) + "\", 0, 0);");

               if (m.outaudiostreams.Count > 0)
               {
                   AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                   if (outstream.audiopath != null)
                   {
                       string interleave = "", title_a = "";
                       if (!string.IsNullOrEmpty(muxer_cli))
                       {
                           string mux_o = Calculate.GetRegexValue(@"\[o\](.*?)\[/o\]", muxer_cli);
                           if (!string.IsNullOrEmpty(mux_o))
                           {
                               interleave = Calculate.GetRegexValue("interleave=\"(\\d*,\\s?\\d*,\\s?\\d*,\\s?\\d*)\"", mux_o);
                           }

                           string mux_a = Calculate.GetRegexValue(@"\[a\](.*?)\[/a\]", muxer_cli);
                           if (!string.IsNullOrEmpty(mux_a))
                           {
                               language_a = Calculate.GetRegexValue("language=\"(.*?)\"", mux_a);
                               title_a = Calculate.GetRegexValue("title=\"(.*?)\"", mux_a);
                           }
                       }

                       string aext = Path.GetExtension(outstream.audiopath).ToLower();
                       string key = "00000201";
                       if (aext == ".mp3" ||
                           aext == ".mp2" ||
                           aext == ".mpa")
                           key = "00000202";
                       else if (aext == ".ac3")
                           key = "00000203";
                       else if (aext == ".ogg")
                           key = "00000204";
                       else if (aext == ".dts")
                           key = "00000205";

                       //Для правильного муксинга вбр и абр мп3-звука
                       string vbr = "0"; //0 - переписывать заголовок (CBR), 1 - не переписывать (VBR и ABR)
                       if (outstream.codec == "MP3" && m.mp3_options.encodingmode != Settings.AudioEncodingModes.CBR ||
                           instream.codecshort == "MP3" && outstream.codec == "Copy")
                       {
                           vbr = "1";
                       }

                       sw.WriteLine("VirtualDub.stream[0].SetSource(\"" + Calculate.GetUTF8String(outstream.audiopath) + "\", 0x" + key + ", " + vbr + ");");
                       sw.WriteLine("VirtualDub.stream[0].DeleteComments(1);");
                       sw.WriteLine("VirtualDub.stream[0].AdjustChapters(1);");
                       sw.WriteLine("VirtualDub.stream[0].SetMode(0);");
                       sw.WriteLine("VirtualDub.stream[0].SetClipMode(1, 1);");
                       sw.WriteLine("VirtualDub.stream[0].SetConversion(0, 0, 0, 0, 0);");
                       sw.WriteLine("VirtualDub.stream[0].SetVolume();");
                       sw.WriteLine("VirtualDub.stream[0].SetCompression();");
                       sw.WriteLine("VirtualDub.stream[0].EnableFilterGraph(0);");
                       sw.WriteLine("VirtualDub.stream[0].filters.Clear();");

                       //Interleaving
                       if (!string.IsNullOrEmpty(interleave))
                           sw.WriteLine("VirtualDub.stream[0].SetInterleave(" + interleave + ", " + (CopyDelay ? outstream.delay.ToString() : "0") + ");");

                       //Title (audio)
                       if (!string.IsNullOrEmpty(title_a))
                           sw.WriteLine("VirtualDub.stream[0].AddComment(0x00000003, \"INAM\", \"" + title_a + "\");");
                   }
               }

               sw.WriteLine("VirtualDub.video.DeleteComments(1);");
               sw.WriteLine("VirtualDub.video.AdjustChapters(1);");
               sw.WriteLine("VirtualDub.video.SetDepth(24, 24);");
               sw.WriteLine("VirtualDub.video.SetMode(0);");
               sw.WriteLine("VirtualDub.video.SetFrameRate(0, 1);");
               sw.WriteLine("VirtualDub.video.SetIVTC(0, 0, -1, 0);");
               sw.WriteLine("VirtualDub.video.SetCompression();");
               sw.WriteLine("VirtualDub.video.filters.Clear();");

               string title = "", author = "", copyright = "";
               if (!string.IsNullOrEmpty(muxer_cli))
               {
                   string mux_v = Calculate.GetRegexValue(@"\[v\](.*?)\[/v\]", muxer_cli);
                   if (!string.IsNullOrEmpty(mux_v))
                   {
                       title = Calculate.GetRegexValue("title=\"(.*?)\"", mux_v);
                       author = Calculate.GetRegexValue("author=\"(.*?)\"", mux_v);
                       copyright = Calculate.GetRegexValue("copyright=\"(.*?)\"", mux_v);
                   }
               }

               //Language (audio)
               if (!string.IsNullOrEmpty(language_a))
                   sw.WriteLine("VirtualDub.video.AddComment(0x00000000, \"IAS1\", \"" + language_a + "\");");

               //Title
               if (!string.IsNullOrEmpty(title))
                   sw.WriteLine("VirtualDub.video.AddComment(0x00000003, \"INAM\", \"" + title + "\");");

               //Author
               if (!string.IsNullOrEmpty(author))
                   sw.WriteLine("VirtualDub.video.AddComment(0x00000005, \"IART\", \"" + author + "\");");

               //Copyright
               if (!string.IsNullOrEmpty(copyright))
                   sw.WriteLine("VirtualDub.video.AddComment(0x00000006, \"ICOP\", \"" + copyright + "\");");

               sw.WriteLine("VirtualDub.SaveAVI(\"" + Calculate.GetUTF8String(m.outfilepath) + "\");");
               sw.WriteLine("VirtualDub.Close();");

               sw.Close();
           }
       }
    }
}
