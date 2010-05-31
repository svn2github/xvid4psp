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
           StreamWriter sw = new StreamWriter(Settings.TempPath + "\\" + m.key + ".vcf", false, System.Text.Encoding.Default);

           sw.WriteLine("VirtualDub.RemoveInputStreams();");
           sw.WriteLine("VirtualDub.Open(\"" + Calculate.GetUTF8String(m.outvideofile) + "\", 0, 0);");

           if (m.outaudiostreams.Count > 0)
           {
               AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
               if (outstream.audiopath != null)
               {
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

                   //SRT 0x0301 
                   //Для правильного муксинга вбр и абр мп3-звука
                   if (outstream.codec == "MP3" && m.mp3_options.encodingmode != Settings.AudioEncodingModes.CBR || instream.codecshort == "MP3" && outstream.codec == "Copy")
                   {
                       //для VBR и ABR (1 - не переписывать заголовок файла)
                       sw.WriteLine("VirtualDub.stream[0].SetSource(\"" + Calculate.GetUTF8String(outstream.audiopath) + "\", 0x" + key + ", 1);");
                   }
                   else
                   {
                       //для CBR (0 - переписывать заголовок)
                       sw.WriteLine("VirtualDub.stream[0].SetSource(\"" + Calculate.GetUTF8String(outstream.audiopath) + "\", 0x" + key + ", 0);");
                   }

                   sw.WriteLine("VirtualDub.stream[0].DeleteComments(1);");
                   sw.WriteLine("VirtualDub.stream[0].AdjustChapters(1);");
                   sw.WriteLine("VirtualDub.stream[0].SetMode(0);");
                   sw.WriteLine("VirtualDub.stream[0].SetInterleave(1, 500, 1, 0, " + (CopyDelay ? outstream.delay.ToString() : "0") + ");");
                   sw.WriteLine("VirtualDub.stream[0].SetClipMode(1, 1);");
                   sw.WriteLine("VirtualDub.stream[0].SetConversion(0, 0, 0, 0, 0);");
                   sw.WriteLine("VirtualDub.stream[0].SetVolume();");
                   sw.WriteLine("VirtualDub.stream[0].SetCompression();");
                   sw.WriteLine("VirtualDub.stream[0].EnableFilterGraph(0);");
                   sw.WriteLine("VirtualDub.stream[0].filters.Clear();");
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
           //sw.WriteLine("VirtualDub.video.AddComment(0x00000003, \"INAM\", \"Title\");"); //Clip
           //sw.WriteLine("VirtualDub.video.AddComment(0x00000005, \"IART\", \"Author\");");
           //sw.WriteLine("VirtualDub.video.AddComment(0x00000006, \"ICOP\", \"Copyright\");");
           sw.WriteLine("VirtualDub.SaveAVI(\"" + Calculate.GetUTF8String(m.outfilepath) + "\");");
           sw.WriteLine("VirtualDub.Close();");

           sw.Close();
       }
    }
}
