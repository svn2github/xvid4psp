using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace XviD4PSP
{
  public static class Format
    {

       public enum ExportFormats
       {
           Audio,
           Avi,
           AviHardware,
           AviHardwareHD,
           AviiRiverClix2,
           AviMeizuM6,
           AviDVNTSC,
           AviDVPAL,
           Flv,
           Mov,
           ThreeGP,
           Mkv,
           Mp4,
           Mp4Archos5G,
           Mp4ToshibaG900,
           Mp4BlackBerry8100,
           Mp4BlackBerry8800,
           Mp4BlackBerry8830,
           Mp4SonyEricssonK610,
           Mp4SonyEricssonK800,
           Mp4MotorolaK1,
           Mp4Nokia5700,
           Mp4iPod50G,
           Mp4iPod55G,
           Mp4iPhone,
           Mp4AppleTV,
           Mp4Prada,
           Mp4PS3,
           Mp4PSPAVC,
           Mp4PSPAVCTV,
           Mp4PSPASP,
           Mpeg1PS,
           Mpeg2PS,
           Mpeg2NTSC,
           Mpeg2PAL,
           TS,
           M2TS,
           PmpAvc,
           DpgNintendoDS,
           WMV,
           BluRay,
           Custom
       }

       public enum Muxers { ffmpeg = 1, mplex, mp4box, divxmux, mkvmerge, pmpavc, virtualdubmod, tsmuxer, dpgmuxer, Disabled };
       public enum Demuxers { ffmpeg, mp4box, pmpdemuxer, mkvextract, tsmuxer, dpgmuxer, h264tsto };

       public static string EnumToString(ExportFormats formatenum)
       {
           if (formatenum == ExportFormats.Audio)
               return "Audio";
           if (formatenum == ExportFormats.Avi)
               return "AVI";
           if (formatenum == ExportFormats.AviHardware)
               return "AVI Hardware";
           if (formatenum == ExportFormats.AviHardwareHD)
               return "AVI Hardware HD";
           if (formatenum == ExportFormats.AviiRiverClix2)
               return "AVI iRiver Clix 2";
           if (formatenum == ExportFormats.AviMeizuM6)
               return "AVI Meizu M6";
           if (formatenum == ExportFormats.AviDVNTSC)
               return "AVI DV NTSC";
           if (formatenum == ExportFormats.AviDVPAL)
               return "AVI DV PAL";
           if (formatenum == ExportFormats.Flv)
               return "FLV";
           if (formatenum == ExportFormats.Mkv)
               return "MKV";
           if (formatenum == ExportFormats.Mp4)
               return "MP4";
           if (formatenum == ExportFormats.ThreeGP)
               return "3GP";
           if (formatenum == ExportFormats.Mov)
               return "MOV";
           if (formatenum == ExportFormats.Mp4Archos5G)
               return "MP4 Archos 5G";
           if (formatenum == ExportFormats.Mp4ToshibaG900)
               return "MP4 Toshiba G900";
           if (formatenum == ExportFormats.Mp4BlackBerry8100)
               return "MP4 BlackBerry 8100";
           if (formatenum == ExportFormats.Mp4BlackBerry8800)
               return "MP4 BlackBerry 8800";
           if (formatenum == ExportFormats.Mp4BlackBerry8830)
               return "MP4 BlackBerry 8830";
           if (formatenum == ExportFormats.Mp4SonyEricssonK610)
               return "MP4 SonyEricsson K610";
           if (formatenum == ExportFormats.Mp4SonyEricssonK800)
               return "MP4 SonyEricsson K800";
           if (formatenum == ExportFormats.Mp4MotorolaK1)
               return "MP4 Motorola K1";
           if (formatenum == ExportFormats.Mp4Nokia5700)
               return "MP4 Nokia 5700";
           if (formatenum == ExportFormats.Mp4iPod50G)
               return "MP4 iPod 5.0G";
           if (formatenum == ExportFormats.Mp4iPod55G)
               return "MP4 iPod 5.5G";
           if (formatenum == ExportFormats.Mp4iPhone)
               return "MP4 iPhone or Touch";
           if (formatenum == ExportFormats.Mp4AppleTV)
               return "MP4 Apple TV";
           if (formatenum == ExportFormats.Mp4Prada)
               return "MP4 Prada";
           if (formatenum == ExportFormats.Mp4PS3)
               return "MP4 PS3 or XBOX360";
           if (formatenum == ExportFormats.Mp4PSPAVC)
               return "MP4 PSP AVC";
           if (formatenum == ExportFormats.Mp4PSPAVCTV)
               return "MP4 PSP AVC TV";
           if (formatenum == ExportFormats.Mp4PSPASP)
               return "MP4 PSP ASP";
           if (formatenum == ExportFormats.Mpeg2PS)
               return "MPEG2 PS";
           if (formatenum == ExportFormats.Mpeg1PS)
               return "MPEG1 PS";
           if (formatenum == ExportFormats.Mpeg2NTSC)
               return "MPEG2 NTSC";
           if (formatenum == ExportFormats.Mpeg2PAL)
               return "MPEG2 PAL";
           if (formatenum == ExportFormats.PmpAvc)
               return "PMP AVC";
           if (formatenum == ExportFormats.M2TS)
               return "M2TS";
           if (formatenum == ExportFormats.TS)
               return "TS";
           if (formatenum == ExportFormats.DpgNintendoDS)
               return "DPG Nintendo DS";
           if (formatenum == ExportFormats.WMV)
               return "WMV";
           if (formatenum == ExportFormats.BluRay)
               return "BluRay";
           if (formatenum == ExportFormats.Custom)
               return "Custom";
           
           return null;
       }

       public static ExportFormats StringToEnum(string stringformat)
       {
           if (stringformat == "Audio")
               return ExportFormats.Audio;
           if (stringformat == "AVI")
               return ExportFormats.Avi;
           if (stringformat == "AVI Hardware")
               return ExportFormats.AviHardware;
           if (stringformat == "AVI Hardware HD")
               return ExportFormats.AviHardwareHD;
           if (stringformat == "AVI iRiver Clix 2")
               return ExportFormats.AviiRiverClix2;
           if (stringformat == "AVI Meizu M6")
               return ExportFormats.AviMeizuM6;
           if (stringformat == "AVI DV NTSC")
               return ExportFormats.AviDVNTSC;
           if (stringformat == "AVI DV PAL")
               return ExportFormats.AviDVPAL;
           if (stringformat == "FLV")
               return ExportFormats.Flv;
           if (stringformat == "MKV")
               return ExportFormats.Mkv;
           if (stringformat == "MP4")
               return ExportFormats.Mp4;
           if (stringformat == "3GP")
               return ExportFormats.ThreeGP;
           if (stringformat == "MOV")
               return ExportFormats.Mov;
           if (stringformat == "MP4 Archos 5G")
               return ExportFormats.Mp4Archos5G;
           if (stringformat == "MP4 Toshiba G900")
               return ExportFormats.Mp4ToshibaG900;
           if (stringformat == "MP4 BlackBerry 8100")
               return ExportFormats.Mp4BlackBerry8100;
           if (stringformat == "MP4 BlackBerry 8800")
               return ExportFormats.Mp4BlackBerry8800;
           if (stringformat == "MP4 BlackBerry 8830")
               return ExportFormats.Mp4BlackBerry8830;
           if (stringformat == "MP4 SonyEricsson K800")
               return ExportFormats.Mp4SonyEricssonK800;
           if (stringformat == "MP4 SonyEricsson K610")
               return ExportFormats.Mp4SonyEricssonK610;
           if (stringformat == "MP4 Motorola K1")
               return ExportFormats.Mp4MotorolaK1;
           if (stringformat == "MP4 Nokia 5700")
               return ExportFormats.Mp4Nokia5700;
           if (stringformat == "MP4 iPod 5.0G")
               return ExportFormats.Mp4iPod50G;
           if (stringformat == "MP4 iPod 5.5G")
               return ExportFormats.Mp4iPod55G;
           if (stringformat == "MP4 iPhone or Touch")
               return ExportFormats.Mp4iPhone;
           if (stringformat == "MP4 Apple TV")
               return ExportFormats.Mp4AppleTV;
           if (stringformat == "MP4 Prada")
               return ExportFormats.Mp4Prada;
           if (stringformat == "MP4 PS3 or XBOX360")
               return ExportFormats.Mp4PS3;
           if (stringformat == "MP4 PSP AVC")
               return ExportFormats.Mp4PSPAVC;
           if (stringformat == "MP4 PSP AVC TV")
               return ExportFormats.Mp4PSPAVCTV;
           if (stringformat == "MP4 PSP ASP")
               return ExportFormats.Mp4PSPASP;
           if (stringformat == "MPEG2 PS")
               return ExportFormats.Mpeg2PS;
           if (stringformat == "MPEG1 PS")
               return ExportFormats.Mpeg1PS;
           if (stringformat == "MPEG2 NTSC")
               return ExportFormats.Mpeg2NTSC;
           if (stringformat == "MPEG2 PAL")
               return ExportFormats.Mpeg2PAL;
           if (stringformat == "PMP AVC")
               return ExportFormats.PmpAvc;
           if (stringformat == "M2TS")
               return ExportFormats.M2TS;
           if (stringformat == "TS")
               return ExportFormats.TS;
           if (stringformat == "DPG Nintendo DS")
               return ExportFormats.DpgNintendoDS;
           if (stringformat == "WMV")
               return ExportFormats.WMV;
           if (stringformat == "BluRay")
               return ExportFormats.BluRay;
           if (stringformat == "Custom")
               return ExportFormats.Custom;

           return ExportFormats.Mp4PSPAVC;
       }

       public static string[] GetFormatList()
       {
           ArrayList formatlist = new ArrayList();
           foreach (string cformats in Enum.GetNames(typeof(ExportFormats)))
           {
               ExportFormats cformat = (ExportFormats)Enum.Parse(typeof(ExportFormats), cformats);
               if (cformat != ExportFormats.WMV)
                   formatlist.Add(EnumToString(cformat));
           }
           return Calculate.ConvertArrayListToStringArray(formatlist);
       }

       public static string[] GetVCodecsList(ExportFormats format)
       {
           //XviD x264 MPEG4 MPEG2 HUFF DV FLV1 FFV1

           switch (format)
           {
               case ExportFormats.PmpAvc:
               case ExportFormats.Mp4iPod50G:
               case ExportFormats.Mp4iPod55G:
               case ExportFormats.Mp4iPhone:
               case ExportFormats.Mp4AppleTV:
               case ExportFormats.Mkv:
               case ExportFormats.Mp4:
               case ExportFormats.Mov:
               case ExportFormats.ThreeGP:
               case ExportFormats.Mp4Archos5G:
               case ExportFormats.Mp4ToshibaG900:
               case ExportFormats.Mp4Nokia5700:
                   return new string[] { "x264", "MPEG4", "XviD", "Copy" };

               case ExportFormats.Mp4PSPAVC:
               case ExportFormats.Mp4PSPAVCTV:
               case ExportFormats.Mp4PS3:
                   return new string[] { "x264", "Copy" };

               case ExportFormats.Mp4PSPASP:
                   return new string[] { "MPEG4", "XviD", "Copy" };

               case ExportFormats.Avi:
                   return new string[] { "x264", "MPEG4", "FLV1", "MJPEG", "HUFF", "FFV1", "XviD", "Copy" };

               case ExportFormats.AviHardware:
               case ExportFormats.AviHardwareHD:
                   return new string[] { "MPEG4", "XviD", "Copy" };

               case ExportFormats.AviDVPAL:
               case ExportFormats.AviDVNTSC:
                   return new string[] { "DV", "Copy" };

               case ExportFormats.Flv:
                   return new string[] { "FLV1", "x264", "Copy" };

               case ExportFormats.AviiRiverClix2:
               case ExportFormats.Mp4Prada:
               case ExportFormats.Mp4BlackBerry8100:
               case ExportFormats.Mp4BlackBerry8800:
               case ExportFormats.Mp4BlackBerry8830:
               case ExportFormats.Mp4MotorolaK1:
               case ExportFormats.Mp4SonyEricssonK800:
               case ExportFormats.Mp4SonyEricssonK610:
               case ExportFormats.AviMeizuM6:
                   return new string[] { "MPEG4", "XviD", "Copy" };

               case ExportFormats.Mpeg2PAL:
               case ExportFormats.Mpeg2NTSC:
                   return new string[] { "MPEG2", "Copy" };

               case ExportFormats.Mpeg2PS:
                   return new string[] { "MPEG2", "Copy" };

               case ExportFormats.DpgNintendoDS:
               case ExportFormats.Mpeg1PS:
                   return new string[] { "MPEG1", "Copy" };

               case ExportFormats.M2TS:
               case ExportFormats.TS:
               case ExportFormats.BluRay:
                   return new string[] { "MPEG2", "x264", "Copy" };

               case ExportFormats.WMV:
                   return new string[] { "WMV3", "Copy" };

               case ExportFormats.Custom:
                   return FormatReader.GetFormatInfo2("Custom", "GetVCodecsList");

               default:
                   return null;
           }
       }

       public static string GetVCodec(ExportFormats format)
       {
           switch (format)
           {
               case ExportFormats.PmpAvc:
               case ExportFormats.Mkv:
               case ExportFormats.Mp4:
               case ExportFormats.Mov:
               case ExportFormats.ThreeGP:
               case ExportFormats.Mp4PSPAVC:
               case ExportFormats.Mp4PSPAVCTV:
               case ExportFormats.Mp4PS3:
               case ExportFormats.Mp4iPod50G:
               case ExportFormats.Mp4iPod55G:
               case ExportFormats.Mp4iPhone:
               case ExportFormats.Mp4AppleTV:
               case ExportFormats.Mp4Archos5G:
               case ExportFormats.Mp4Nokia5700:
               case ExportFormats.M2TS:
               case ExportFormats.TS:
               case ExportFormats.BluRay:
                   return "x264";

               case ExportFormats.AviiRiverClix2:
               case ExportFormats.Mp4Prada:
               case ExportFormats.Mp4BlackBerry8100:
               case ExportFormats.Mp4BlackBerry8800:
               case ExportFormats.Mp4BlackBerry8830:
               case ExportFormats.Mp4MotorolaK1:
               case ExportFormats.Mp4PSPASP:
               case ExportFormats.Avi:
               case ExportFormats.AviHardware:
               case ExportFormats.AviHardwareHD:
               case ExportFormats.Mp4SonyEricssonK800:
               case ExportFormats.Mp4SonyEricssonK610:
                   return "XviD";

               case ExportFormats.AviDVPAL:
               case ExportFormats.AviDVNTSC:
                   return "DV";

               case ExportFormats.Mpeg2PS:
               case ExportFormats.Mpeg2PAL:
               case ExportFormats.Mpeg2NTSC:
                   return "MPEG2";

               case ExportFormats.DpgNintendoDS:
               case ExportFormats.Mpeg1PS:
                   return "MPEG1";

               case ExportFormats.Flv:
                   return "FLV1";

               case ExportFormats.Mp4ToshibaG900:
               case ExportFormats.AviMeizuM6:
                   return "MPEG4";

               case ExportFormats.WMV:
                   return "WMV3";

               case ExportFormats.Custom:
                   return FormatReader.GetFormatInfo("Custom","GetVCodec");

               default:
                   return null;
           }
       }

       public static string[] GetACodecsList(ExportFormats format)
       {
           switch (format)
           {
               case ExportFormats.Audio:
                   return new string[] { "PCM", "FLAC", "AC3", "MP3", "MP2", "AAC" };

               case ExportFormats.PmpAvc:
               case ExportFormats.Mp4Archos5G:
               case ExportFormats.Mp4BlackBerry8100:
               case ExportFormats.Mp4BlackBerry8800:
               case ExportFormats.Mp4BlackBerry8830:
               case ExportFormats.Mp4MotorolaK1:
               case ExportFormats.Mp4ToshibaG900:
               case ExportFormats.Mp4SonyEricssonK800:
               case ExportFormats.Mp4SonyEricssonK610:
                   return new string[] { "MP3", "AAC", "Disabled", "Copy" };

               case ExportFormats.Flv:
                   return new string[] { "MP3", "AAC", "Disabled", "Copy" };

               case ExportFormats.Mp4iPod50G:
               case ExportFormats.Mp4iPod55G:
               case ExportFormats.Mp4iPhone:
               case ExportFormats.Mp4Prada:
               case ExportFormats.Mp4Nokia5700:
                   return new string[] { "AAC", "Disabled", "Copy" };

               case ExportFormats.AviiRiverClix2:
               case ExportFormats.AviMeizuM6:
                   return new string[] { "MP3", "MP2", "Disabled", "Copy" };

               case ExportFormats.Mp4PSPAVC:
               case ExportFormats.Mp4PSPAVCTV:
               case ExportFormats.Mp4PSPASP:
               case ExportFormats.Mp4PS3:
               case ExportFormats.Mp4AppleTV:
                   return new string[] { "AAC", "Disabled", "Copy" };

               case ExportFormats.Mp4:
               case ExportFormats.Mov:
                   return new string[] { "MP3", "MP2", "AC3", "AAC", "Disabled", "Copy" };

               case ExportFormats.ThreeGP:
                   return new string[] { "MP3", "MP2", "AAC", "Disabled", "Copy" };

               case ExportFormats.Mkv:
                   return new string[] { "PCM", "FLAC", "AC3", "MP3", "MP2", "AAC", "Disabled", "Copy" };

               case ExportFormats.DpgNintendoDS:
               case ExportFormats.Mpeg1PS:
                   return new string[] { "MP2", "Disabled", "Copy" };

               case ExportFormats.Mpeg2PS:
                   return new string[] { "MP2", "AC3", "Disabled", "Copy" };

               case ExportFormats.M2TS:
               case ExportFormats.TS:
                   return new string[] { "PCM", "AAC", "MP2", "MP3", "AC3", "Disabled", "Copy" };

               case ExportFormats.BluRay:
                   return new string[] { "PCM", "AC3", "Disabled", "Copy" };

               case ExportFormats.Mpeg2PAL:
               case ExportFormats.Mpeg2NTSC:
                   return new string[] { "MP2", "AC3", "Disabled", "Copy" };

               case ExportFormats.AviDVPAL:
               case ExportFormats.AviDVNTSC:
                   return new string[] { "PCM", "Disabled", "Copy" };

               case ExportFormats.AviHardware:
               case ExportFormats.AviHardwareHD:
               case ExportFormats.Avi:
                   return new string[] { "PCM", "AC3", "MP3", "MP2", "Disabled", "Copy" }; //, "AAC" 

               case ExportFormats.WMV:
                   return new string[] { "WMA3", "Disabled", "Copy" };

               case ExportFormats.Custom:
                   return FormatReader.GetFormatInfo2("Custom","GetACodecsList");
               
               default:
                   return null;
           }
       }

       public static string GetACodec(ExportFormats format, string vcodec)
       {
           switch (format)
           {
               case ExportFormats.PmpAvc:
               case ExportFormats.Mkv:
               case ExportFormats.Mp4:
               case ExportFormats.Mov:
               case ExportFormats.ThreeGP:
               case ExportFormats.Mp4PSPAVC:
               case ExportFormats.Mp4PSPAVCTV:
               case ExportFormats.Mp4PSPASP:
               case ExportFormats.Mp4PS3:
               case ExportFormats.Mp4iPod50G:
               case ExportFormats.Mp4iPod55G:
               case ExportFormats.Mp4iPhone:
               case ExportFormats.Mp4Prada:
               case ExportFormats.Mp4AppleTV:
               case ExportFormats.Mp4Archos5G:
               case ExportFormats.Mp4BlackBerry8100:
               case ExportFormats.Mp4BlackBerry8800:
               case ExportFormats.Mp4BlackBerry8830:
               case ExportFormats.Mp4MotorolaK1:
               case ExportFormats.Mp4SonyEricssonK800:
               case ExportFormats.Mp4SonyEricssonK610:
               case ExportFormats.Mp4Nokia5700:
                   return "AAC";

               case ExportFormats.AviiRiverClix2:
               case ExportFormats.Flv:
               case ExportFormats.Mp4ToshibaG900:
               case ExportFormats.Audio:
               case ExportFormats.AviMeizuM6:
                   return "MP3";

               case ExportFormats.AviHardware:
               case ExportFormats.AviHardwareHD:
               case ExportFormats.Avi:
                   switch (vcodec)
                   {
                       case "DivX":
                       case "XviD":
                       case "FLV1":
                       case "Copy":
                           return "MP3";

                       case "HUFF":
                       case "FFV1":
                       case "MJPEG":
                           return "PCM";

                       case "x264":
                           return "MP3";

                       default:
                           return "MP3";
                   }

               case ExportFormats.AviDVPAL:
               case ExportFormats.AviDVNTSC:
                   return "PCM";

               case ExportFormats.Mpeg2PAL:
               case ExportFormats.Mpeg2NTSC:
               case ExportFormats.Mpeg2PS:
               case ExportFormats.M2TS:
               case ExportFormats.TS:
               case ExportFormats.BluRay:
                   return "AC3";

               case ExportFormats.DpgNintendoDS:
               case ExportFormats.Mpeg1PS:
                   return "MP2";

               case ExportFormats.WMV:
                   return "WMA3";

               case ExportFormats.Custom:
                   return FormatReader.GetFormatInfo("Custom","GetACodec");
               
               default:
                   return null;
           }
       }

       public static Massive GetValidVDecoder(Massive m)
       {
           if (!m.isvideo)
           {
               m.vdecoder = 0;
               return m;
           }
           string ext = Path.GetExtension(m.infilepath).ToLower();
           if (Calculate.IsMPEG(m.infilepath) && (m.invcodecshort == "MPEG1" || m.invcodecshort == "MPEG2")) //m.invcodecshort != "h264")
           {
               if (m.indexfile != null)
                   m.vdecoder = AviSynthScripting.Decoders.MPEG2Source;
               else
               {
                   if (Settings.MPEGDecoder == AviSynthScripting.Decoders.MPEG2Source && m.indexfile == null)
                       m.vdecoder = Settings.OtherDecoder; //FFmpegSource
                   else
                       m.vdecoder = Settings.MPEGDecoder;
               }
           }
           else
           {
               if (ext == ".avi") m.vdecoder = Settings.AVIDecoder;
               else if (ext == ".evo") m.vdecoder = AviSynthScripting.Decoders.FFmpegSource;
               else if (ext == ".pmp") m.vdecoder = AviSynthScripting.Decoders.DirectShowSource;
               else if (ext == ".vdr") m.vdecoder = AviSynthScripting.Decoders.AVISource;
               else if (ext == ".avs") m.vdecoder = AviSynthScripting.Decoders.Import;
               else if (ext == ".dga") m.vdecoder = AviSynthScripting.Decoders.AVCSource;
               else if (ext == ".dgi") m.vdecoder = AviSynthScripting.Decoders.DGMultiSource;
               else m.vdecoder = Settings.OtherDecoder;
           }
           return m;
       }

       public static AudioStream GetValidADecoder(AudioStream instream)
       {
           if (instream.audiopath != null)
           {
               string aext = Path.GetExtension(instream.audiopath).ToLower();
               if (aext == ".ac3") instream.decoder = AviSynthScripting.Decoders.NicAC3Source;
               else if (aext == ".mpa") instream.decoder = AviSynthScripting.Decoders.NicMPG123Source;
               else if (aext == ".dts") instream.decoder = AviSynthScripting.Decoders.NicDTSSource;
               else if (aext == ".wav") instream.decoder = AviSynthScripting.Decoders.WAVSource;
               else if (aext == ".wma") instream.decoder = AviSynthScripting.Decoders.bassAudioSource;
               else instream.decoder = AviSynthScripting.Decoders.bassAudioSource;
           }
           else instream.decoder = 0;
           return instream;
       }

       public static Massive GetOutInterlace(Massive m)
       {
           if (m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
               m.interlace == SourceType.INTERLACED)
           {
               if (m.outvcodec == "MPEG2" ||
                   m.outvcodec == "MPEG1" && m.format != ExportFormats.DpgNintendoDS ||
                   m.outvcodec == "DV" ||
                   m.outvcodec == "HUFF" ||
                   m.outvcodec == "FFV1" ||
                   m.outvcodec == "x264" && m.format == ExportFormats.Avi ||
                   m.outvcodec == "x264" && m.format == ExportFormats.AviHardware ||
                   m.outvcodec == "x264" && m.format == ExportFormats.AviHardwareHD ||
                   m.outvcodec == "x264" && m.format == ExportFormats.BluRay ||
                   m.outvcodec == "x264" && m.format == ExportFormats.M2TS ||
                   m.outvcodec == "x264" && m.format == ExportFormats.Mkv ||
                   m.outvcodec == "x264" && m.format == ExportFormats.TS ||
                   m.outvcodec == "x264" && m.format == ExportFormats.Custom) 
               {
                   if (Settings.AlwaysProgressive)
                       m.deinterlace = Settings.Deinterlace;
                   else
                       m.deinterlace = DeinterlaceType.Disabled;
               }
               else
               {
                   if (m.interlace == SourceType.INTERLACED ||
                            m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED)
                       m.deinterlace = Settings.Deinterlace;
                   else
                       m.deinterlace = DeinterlaceType.Disabled;
               }
           }
           else if (m.interlace == SourceType.FILM ||
                    m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                    m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM)
               m.deinterlace = Settings.TIVTC;
           else if (m.interlace == SourceType.DECIMATING)
           {
               m.deinterlace = DeinterlaceType.TDecimate;
           }
           else
               m.deinterlace = DeinterlaceType.Disabled;

           m = GetValidFramerate(m);
           m = Calculate.UpdateOutFrames(m);

           return m;
       }

       public static string GetCodecOutInterlace(Massive m)
       {
           if (m.interlace == SourceType.DECIMATING ||
               m.interlace == SourceType.FILM ||
               m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
               m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
               m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
               m.interlace == SourceType.INTERLACED)
           {
               if (m.deinterlace == DeinterlaceType.Disabled)
                   return Massive.InterlaceModes.Interlaced.ToString();
               else
                   return Massive.InterlaceModes.Progressive.ToString();
           }
           else
               return Massive.InterlaceModes.Progressive.ToString();
       }

       public static Massive GetValidChannels(Massive m)
       {
           if (m.inaudiostreams.Count > 0 &&
               m.inaudiostreams.Count > 0)
           {
               AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

               if (instream.channelconverter == AudioOptions.ChannelConverters.ConvertToMono)
                   outstream.channels = 1;
               else if (instream.channelconverter == AudioOptions.ChannelConverters.ConvertToStereo ||
                instream.channelconverter == AudioOptions.ChannelConverters.ConvertToDolbyProLogic ||
                instream.channelconverter == AudioOptions.ChannelConverters.ConvertToDolbyProLogicII ||
                   instream.channelconverter == AudioOptions.ChannelConverters.ConvertToDolbyProLogicIILFE)
                   outstream.channels = 2;
               else if (instream.channelconverter == AudioOptions.ChannelConverters.ConvertToUpmixAction ||
                   instream.channelconverter == AudioOptions.ChannelConverters.ConvertToUpmixDialog ||
                   instream.channelconverter == AudioOptions.ChannelConverters.ConvertToUpmixFarina ||
                   instream.channelconverter == AudioOptions.ChannelConverters.ConvertToUpmixGerzen ||
                   instream.channelconverter == AudioOptions.ChannelConverters.ConvertToUpmixMultisonic ||
                   instream.channelconverter == AudioOptions.ChannelConverters.ConvertToUpmixSoundOnSound)
                   outstream.channels = 6;
               else
                   outstream.channels = instream.channels;
           }
           return m;
       }

       public static int GetSettingsChannels()
       {
           string ch = Settings.ChannelsConverter;
           if (ch == "KeepOriginalChannels")
               return 0;
           else if (ch == "ConvertToMono")
               return 1;
           else if (ch == "ConvertToStereo" || ch == "ConvertToDolbyProLogic" || ch == "ConvertToDolbyProLogicII" || ch == "ConvertToDolbyProLogicIILFE")
               return 2;
           else return 6;
       }

       public static Massive GetValidChannelsConverter(Massive m)
       {
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
           {
               AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

               instream.channelconverter = (AudioOptions.ChannelConverters)Enum.Parse(typeof(AudioOptions.ChannelConverters), Settings.ChannelsConverter, true); //AudioOptions.ChannelConverters.KeepOriginalChannels;
               int n = GetSettingsChannels();

               if (m.format == ExportFormats.PmpAvc)
               {
                   if (instream.channels != 2 && n != 2)
                       instream.channelconverter = AudioOptions.ChannelConverters.ConvertToDolbyProLogicII;
                   if (instream.channels == 2 && n != 0)
                       instream.channelconverter = AudioOptions.ChannelConverters.KeepOriginalChannels;
               }
               else if (m.format == ExportFormats.AviDVNTSC ||
                       m.format == ExportFormats.AviDVPAL ||
                       m.format == ExportFormats.AviiRiverClix2 ||
                       m.format == ExportFormats.Flv ||
                       m.format == ExportFormats.Mp4BlackBerry8100 ||
                       m.format == ExportFormats.Mp4BlackBerry8800 ||
                       m.format == ExportFormats.Mp4BlackBerry8830 ||
                       m.format == ExportFormats.Mp4SonyEricssonK800 ||
                       m.format == ExportFormats.Mp4SonyEricssonK610 ||
                       m.format == ExportFormats.Mp4MotorolaK1 ||
                       m.format == ExportFormats.Mp4iPhone ||
                       m.format == ExportFormats.Mp4iPod50G ||
                       m.format == ExportFormats.Mp4iPod55G ||
                       m.format == ExportFormats.Mp4Prada ||
                       m.format == ExportFormats.ThreeGP ||
                       m.format == ExportFormats.Mp4PSPAVC ||
                       m.format == ExportFormats.Mp4PSPAVCTV ||
                       m.format == ExportFormats.Mp4PSPASP ||
                       m.format == ExportFormats.Mp4Archos5G ||
                       m.format == ExportFormats.Mp4ToshibaG900 ||
                       m.format == ExportFormats.Mp4Nokia5700 ||
                       m.format == ExportFormats.AviMeizuM6 ||
                       outstream.codec == "MP2" ||
                       outstream.codec == "MP3" ||
                       m.format == ExportFormats.Custom && FormatReader.GetFormatInfo("Custom", "GetValidChannelsConverter") == "yes")
               {
                   if (instream.channels == 1 && n == 6 || instream.channels == 1 && n == 1)
                       instream.channelconverter = AudioOptions.ChannelConverters.KeepOriginalChannels;
                   if (instream.channels == 2 && n == 6 || instream.channels == 2 && n == 2)
                       instream.channelconverter = AudioOptions.ChannelConverters.KeepOriginalChannels;
                   if (instream.channels > 2 && n == 6 || instream.channels > 2 && n == 0)
                       instream.channelconverter = AudioOptions.ChannelConverters.ConvertToDolbyProLogicII;
               }
               else if (instream.channels == 1 && n == 1 || instream.channels == 2 && n == 2 || instream.channels == 6 && n == 6)
                   instream.channelconverter = AudioOptions.ChannelConverters.KeepOriginalChannels;
           }
           return m;
       }

       public static Massive GetValidBits(Massive m)
       {
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
           {
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
               outstream.bits = 16;
           }
           return m;
       }

       public static Massive GetValidSamplerate(Massive m)
       {
           if (m.inaudiostreams.Count > 0 &&
               m.inaudiostreams.Count > 0)
           {
               AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

               if (m.format == ExportFormats.DpgNintendoDS)
               {
                   outstream.samplerate = "32000";
               }
               else
               {
                   if (instream.samplerate != null)
                   {
                       string[] rates = GetValidSampleratesList(m);
                       outstream.samplerate = Calculate.GetCloseInteger(instream.samplerate, rates);
                   }
               }
           }

           //получаем правильный samplerate конвертер
           m = GetValidSamplerateModifer(m);

           return m;
       }

       public static Massive GetValidSamplerateModifer(Massive m)
       {
           if (m.inaudiostreams.Count > 0 &&
               m.inaudiostreams.Count > 0 &&
               m.sampleratemodifer == AviSynthScripting.SamplerateModifers.SSRC)
           {
               AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

               int sfrq = Convert.ToInt32(instream.samplerate);
               int dfrq = Convert.ToInt32(outstream.samplerate);
               int frqgcd = Calculate.GetGCD(sfrq, dfrq);
               int fs1 = dfrq * sfrq / frqgcd;

               if (fs1 / dfrq != 1 && 
                   fs1 / dfrq % 2 != 0 && 
                   fs1 / dfrq % 3 != 0)
                   m.sampleratemodifer = AviSynthScripting.SamplerateModifers.ResampleAudio;          
           }

           return m;
       }

       public static string[] GetValidSampleratesList(Massive m)
       {
           if (m.format == ExportFormats.PmpAvc)
               return new string[] { "44100" };
           else if (m.format == ExportFormats.Flv)
               return new string[] { "44100" };
           else if (m.format == ExportFormats.AviDVPAL ||
               m.format == ExportFormats.AviDVNTSC)
               return new string[] { "32000", "48000" };
           else if (m.format == ExportFormats.DpgNintendoDS)
               return new string[] { "32000", "48000" }; //"32768"
           else if (m.format == ExportFormats.Custom)
               return FormatReader.GetFormatInfo2("Custom", "GetValidSampleratesList");
           else
           {
               AudioStream outstream;
               if (m.outaudiostreams.Count > 0)
                   outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
               else
               {
                   outstream = new AudioStream();
                   outstream.encoding = Settings.GetAEncodingPreset(Settings.FormatOut);
                   outstream.codec = PresetLoader.GetACodec(m.format, outstream.encoding);
               }

               if (outstream.codec == "MP3" ||
                   outstream.codec == "MP2")
                   return new string[] { "32000", "44100", "48000" };
               else if (m.format == ExportFormats.TS && outstream.codec == "PCM" ||
                   m.format == ExportFormats.M2TS && outstream.codec == "PCM" ||
                   m.format == ExportFormats.BluRay && outstream.codec == "PCM")
                   return new string[] { "48000", "96000", "192000" };
               else if (outstream.codec == "PCM" ||
                        outstream.codec == "LPCM")
                   return new string[] { "22050", "32000", "44100", "48000", "96000", "192000" };
               else if (outstream.codec == "AC3")
                   return new string[] { "48000" };
               else if (outstream.codec == "WMA3")
                   return new string[] { "22050", "32000", "44100", "48000" };
               else
                   return new string[] { "22050", "32000", "44100", "48000" };
           }
       }

       public static Massive GetValidFramerate(Massive m)
       {
           string[] rates = GetValidFrameratesList(m);

           if (m.format == ExportFormats.DpgNintendoDS)
           {
               if ((double)m.outresw / (double)m.outresh  < 1.5)
                   m.outframerate = "20.000";
               else
                   m.outframerate = "22.000";
           }
           else if (m.deinterlace == DeinterlaceType.TIVTC ||
               m.deinterlace == DeinterlaceType.TDecimate)
               m.outframerate = Calculate.GetClosePointDouble("23.976", rates);
           else if (m.deinterlace == DeinterlaceType.SmoothDeinterlace ||
                        m.deinterlace == DeinterlaceType.NNEDI ||
                        m.deinterlace == DeinterlaceType.MCBob)
           {
               double inframerate = Calculate.ConvertStringToDouble(m.inframerate);
               string rate = Calculate.ConvertDoubleToPointString(inframerate * 2.0);
               m.outframerate = Calculate.GetClosePointDouble(rate, rates);
           }
           else
               m.outframerate = Calculate.GetClosePointDouble(m.inframerate, rates);

           return m;
       }

       public static string[] GetValidFrameratesList(Massive m)
       {
           if (m.format == ExportFormats.AviDVPAL)
               return new string[] { "25.000" };

           else if (m.format == ExportFormats.AviDVNTSC)
               return new string[] { "29.970" };

           else if (m.format == ExportFormats.Avi ||
               m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.WMV)
               return new string[] { "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940", "60.000", "120.000" };

           else if (m.format == ExportFormats.AviHardware ||
               m.format == ExportFormats.AviHardwareHD)
               return new string[] { "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };

           else if (m.format == ExportFormats.Mp4SonyEricssonK610 ||
               m.format == ExportFormats.AviMeizuM6)
               return new string[] { "15.000", "18.000", "20.000" };

           else if (m.format == ExportFormats.Mp4Nokia5700)
               return new string[] { "15.000" };

           else if (m.format == ExportFormats.Mp4SonyEricssonK800)
               return new string[] { "15.000", "18.000", "20.000", "23.976", "24.000", "25.000" };

           else if (m.format == ExportFormats.Mpeg2PAL)
               return new string[] { "23.976", "24.000", "25.000" };

           else if (m.format == ExportFormats.Mpeg2NTSC)
               return new string[] { "23.976", "24.000", "25.000", "29.970" };

           else if (m.format == ExportFormats.Mp4PS3)
               return new string[] { "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };

           else if (m.format == ExportFormats.DpgNintendoDS)
               return new string[] { "20.000", "22.000", "24.000", "25.000" };

           else if (m.format == ExportFormats.M2TS ||
           m.format == ExportFormats.TS ||
           m.format == ExportFormats.BluRay ||
           m.format == ExportFormats.Mpeg2PS ||
           m.format == ExportFormats.Mpeg1PS)
               return new string[] { "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };

           else if (m.format == ExportFormats.Custom)
               return FormatReader.GetFormatInfo2("Custom", "GetValidFrameratesList");

           else
               return new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
       }

       public static string GetValidExtension(Massive m)
       {
           switch (m.format)
           {
               default:
               case ExportFormats.Mp4:
               case ExportFormats.Mp4PSPAVC:
               case ExportFormats.Mp4PSPAVCTV:
               case ExportFormats.Mp4PSPASP:
               case ExportFormats.Mp4PS3:
               case ExportFormats.Mp4iPod50G:
               case ExportFormats.Mp4iPod55G:
               case ExportFormats.Mp4iPhone:
               case ExportFormats.Mp4Prada:
               case ExportFormats.Mp4BlackBerry8100:
               case ExportFormats.Mp4BlackBerry8800:
               case ExportFormats.Mp4BlackBerry8830:
               case ExportFormats.Mp4SonyEricssonK800:
               case ExportFormats.Mp4SonyEricssonK610:
               case ExportFormats.Mp4MotorolaK1:
               case ExportFormats.Mp4AppleTV:
               case ExportFormats.Mp4Archos5G:
               case ExportFormats.Mp4ToshibaG900:
               case ExportFormats.Mp4Nokia5700:
                   return ".mp4";

               case ExportFormats.Mkv:
                   return ".mkv";

               case ExportFormats.ThreeGP:
                   return ".3gp";

               case ExportFormats.Mov:
                   return ".mov";

               case ExportFormats.PmpAvc:
                   return ".pmp";

               case ExportFormats.Flv:
                   return ".flv";

               case ExportFormats.Avi:
               case ExportFormats.AviHardware:
               case ExportFormats.AviHardwareHD:
               case ExportFormats.AviDVPAL:
               case ExportFormats.AviDVNTSC:
               case ExportFormats.AviiRiverClix2:
               case ExportFormats.AviMeizuM6:
                   return ".avi";

               case ExportFormats.Mpeg2PAL:
               case ExportFormats.Mpeg2NTSC:
               case ExportFormats.Mpeg2PS:
               case ExportFormats.Mpeg1PS:
                   return ".mpg";

               case ExportFormats.TS:
                   return ".ts";

               case ExportFormats.M2TS:
               case ExportFormats.BluRay:
                   return ".m2ts";

               case ExportFormats.DpgNintendoDS:
                   return ".dpg";

               case ExportFormats.WMV:  //Custom
                   return ".wmv";

               case ExportFormats.Custom:  //Custom
                   return FormatReader.GetFormatInfo("Custom","GetValidExtension");
               
               case ExportFormats.Audio:
                   {
                       AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                       if (outstream.codec == "AAC")
                           return ".m4a";
                       else if (outstream.codec == "PCM")
                           return ".wav";
                       else
                           return "." + outstream.codec.ToLower();
                   }
           }
       }

       public static Massive GetValidResolution(Massive m)
       {
           ArrayList wlist = GetResWList(m);
           ArrayList hlist = GetResHList(m);

           //блок дл€ форматов с анаморфом
           if (Settings.SaveAnamorph || m.aspectfix == AspectResolution.AspectFixes.SAR)
           {
               if (m.format == ExportFormats.TS ||
                   m.format == ExportFormats.M2TS ||
                   m.format == ExportFormats.Mkv ||
                   m.format == ExportFormats.Mp4 ||
                   m.format == ExportFormats.Mp4PS3 ||
                   m.format == ExportFormats.Mov ||
                   m.format == ExportFormats.Avi ||
                   m.format == ExportFormats.AviHardware ||
                   m.format == ExportFormats.AviHardwareHD ||
                   m.format == ExportFormats.Custom)
               //m.format == ExportFormats.BluRay)
               {
                   if (m.blackh == 0 && m.blackw == 0)
                   {
                       if (wlist.Contains(m.inresw) && hlist.Contains(m.inresh))
                       {
                           m.outresw = m.inresw;
                           m.outresh = m.inresh;
                           //m.inaspect = m.outaspect;
                           //≈сли у нас анаморф на входе
                           if ((double)m.inresw / (double)m.inresh != m.inaspect)
                           {
                               m.aspectfix = AspectResolution.AspectFixes.SAR;
                               m.outresw = Calculate.GetCloseIntegerAL(m.outresw - m.cropl - m.cropr, wlist); //ѕересчет ширины с учетом откропленного
                               m.outresh = Calculate.GetCloseIntegerAL(m.inresh - m.cropb - m.cropt, hlist); //ѕересчет высоты с учЄтом откропленного
                           }
                           return m;
                       }
                   }
               }
           }

           int MaxW = 0;
           int MaxH = 0;

           if (m.format == ExportFormats.AviHardware)
           {
               MaxW = 640;
               MaxH = 480;
           }
           else if (m.format == ExportFormats.Mp4Archos5G)
           {
               MaxW = 720;
               MaxH = 480;
           }
           else if (m.format == ExportFormats.Mp4ToshibaG900)
           {
               MaxW = 640;
               MaxH = 480;
           }
           else if (m.format == ExportFormats.Mp4iPhone)
           {
               MaxW = 480;
               MaxH = 320;
           }
           else if (m.format == ExportFormats.Custom)
           {
               MaxW = Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "MaxResolutionW"));
               MaxH = Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "MaxResolutionH"));
           }

           else
           {
               MaxW = (int)wlist[wlist.Count - 1];
               MaxH = (int)hlist[hlist.Count - 1];
           }

           //ограничение W*H
           int limit = (int)wlist[wlist.Count - 1] * (int)hlist[hlist.Count - 1];
           if (m.format == ExportFormats.Mp4PSPASP)
               limit = 100100;

           //первичное получение разрешений
           m.outresw = Calculate.GetCloseIntegerAL(m.inresw, wlist);
           m.outresh = (int)((double)m.outresw / m.inaspect); //¬ысота

           if (m.outresh > MaxH)
           {
               m.outresh = Calculate.GetCloseIntegerAL(m.inresh, hlist);
               m.outresw = Calculate.GetCloseIntegerAL((int)(m.outresh * m.inaspect), wlist);
           }
           else
           {
               //m.outresw = Calculate.GetCloseIntegerAL(m.inresw, wlist);
               m.outresh = Calculate.GetCloseIntegerAL(m.outresh, hlist); //(int)(m.outresw / m.inaspect), hlist);
           }

           //выбираем по какой стороне подбирать
           if (m.outresh > MaxH)
           {
               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit || m.outresw > MaxW || m.outresh > MaxH)
               {
                   m.outresh = Calculate.GetCloseIntegerAL(m.outresh - GetValidModH(m), hlist);
                   m.outresw = Calculate.GetCloseIntegerAL((int)(m.outresh * m.inaspect), wlist);
               }
           }
           else
           {
               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit || m.outresw > MaxW || m.outresh > MaxH)
               {
                   m.outresw = Calculate.GetCloseIntegerAL(m.outresw - GetValidModW(m), wlist);
                   m.outresh = Calculate.GetCloseIntegerAL((int)(m.outresw / m.inaspect), hlist);
               }
           }

           if (m.format == ExportFormats.BluRay)
           {
               if (m.outresw == 720 && m.outresh != 480 ||
                   m.outresw == 720 && m.outresh != 576)
                   m.outresh = 576;
               if (m.outresw == 1280 && m.outresh != 720)
                   m.outresh = 720;
               if (m.outresw == 1920 && m.outresh != 1080 ||
                   m.outresw == 1440 && m.outresh != 1080)
                   m.outresh = 1080;
           }

           return m;
       }

       public static Massive GetValidResolution(Massive m, int w)
       {
           ArrayList wlist = GetResWList(m);
           ArrayList hlist = GetResHList(m);

           //ѕри кодировании с сохранением анаморфа, высота равна исходной высоте минус всЄ что откроплено. 
           if (m.aspectfix == AspectResolution.AspectFixes.SAR)
           {
               m.outresh = Calculate.GetCloseIntegerAL(m.inresh - m.cropb - m.cropt, hlist);
               return m;
           }
           
           //ограничение W*H
           int limit = (int)wlist[wlist.Count - 1] * (int)hlist[hlist.Count - 1];
           if (m.format == ExportFormats.Mp4PSPASP)
               limit = 100100;

           //первичное получение разрешений
           m.outresh = (int)((double)w / m.inaspect);

           //выбираем по какой стороне подбирать
           if (m.outresh > (int)hlist[hlist.Count - 1])
           {
               m.outresh = Calculate.GetCloseIntegerAL(m.outresh, hlist);
               m.outresw = Calculate.GetCloseIntegerAL((int)(m.outresh * m.inaspect), wlist);

               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit)
               {
                   m.outresh = Calculate.GetCloseIntegerAL(m.inresh - GetValidModH(m), hlist);
                   m.outresw = Calculate.GetCloseIntegerAL((int)(m.outresh * m.inaspect), wlist);
               }
           }
           else
           {
               m.outresh = Calculate.GetCloseIntegerAL((int)(m.outresw / m.inaspect), hlist);

               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit)
               {
                   m.outresw = Calculate.GetCloseIntegerAL(m.outresw - GetValidModW(m), wlist);
                   m.outresh = Calculate.GetCloseIntegerAL((int)(m.outresw / m.inaspect), hlist);
               }
           }

           return m;
       }

       public static int GetValidModW(Massive m)
       {
           int modw = 16;
           if (m.format == ExportFormats.Avi || m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mov || m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Custom)
               modw = Settings.LimitModW;
           return modw;
       }

       public static int GetValidModH(Massive m)
       {
           int modh = 8;
           if (m.format == ExportFormats.Avi || m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mov || m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Custom)
               modh = Settings.LimitModH;
           return modh;
       }

       public static ArrayList GetResWList(Massive m)
       {
           ArrayList reswlist = new ArrayList();
           int n = 16;
           int step = 16;

           if (m.format == ExportFormats.AviHardware ||
               m.format == ExportFormats.AviHardwareHD ||
               m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.Mpeg2PS ||
               m.format == ExportFormats.Mpeg1PS ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.TS ||
               m.format == ExportFormats.WMV ||
               m.format == ExportFormats.Flv)
                
           {
               while (n < 1920 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Avi ||
               m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Mov)
           {
               step = Settings.LimitModW;
               while (n < 1920 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.BluRay)
           {
               reswlist.Add(720);
               reswlist.Add(1280);
               reswlist.Add(1440);
               reswlist.Add(1920);
           }
           else if (m.format == ExportFormats.Mpeg2PAL ||
               m.format == ExportFormats.AviDVPAL)
           {
                   reswlist.Add(720);
           }
           else if (m.format == ExportFormats.Mpeg2NTSC ||
               m.format == ExportFormats.AviDVNTSC ||
               m.format == ExportFormats.Mp4PSPAVCTV)
           {
                   reswlist.Add(720);
           }
           else if (m.format == ExportFormats.Mp4Archos5G)
           {
               while (n < 720 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4ToshibaG900)
           {
               while (n < 800 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4PSPAVC)
           {
               while (n < 480 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.PmpAvc)
           {
               while (n < 480 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4PSPASP)
           {
               while (n < 480 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.DpgNintendoDS)
           {
               while (n < 256 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4BlackBerry8100)
           {
               while (n < 240 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4BlackBerry8800 ||
               m.format == ExportFormats.Mp4BlackBerry8830 ||
               m.format == ExportFormats.Mp4SonyEricssonK800 ||
               m.format == ExportFormats.Mp4Nokia5700 ||
               m.format == ExportFormats.AviMeizuM6)
           {
               while (n < 320 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4SonyEricssonK610)
           {
               while (n < 176 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4MotorolaK1)
           {
               while (n < 352 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.AviiRiverClix2 ||
               m.format == ExportFormats.ThreeGP)
           {
               while (n < 320 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4iPod55G)
           {
               while (n < 640 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4Prada)
           {
               while (n < 400 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4iPhone)
           {
               while (n < 720 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4AppleTV)
           {
               while (n < 1280 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Custom)
           {
               step = Settings.LimitModW;
               n = Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "MinResolutionW"));
               while (n < Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "MaxResolutionW")) + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }

           return reswlist;
       }

       public static ArrayList GetResHList(Massive m)
       {
           ArrayList reshlist = new ArrayList();
           int n = 16;
           int step = 8;

           if (m.format == ExportFormats.AviHardware ||
               m.format == ExportFormats.AviHardwareHD ||
               m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.Mpeg2PS ||
               m.format == ExportFormats.Mpeg1PS ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.TS ||
               m.format == ExportFormats.WMV ||
               m.format == ExportFormats.Flv)
           {
               while (n < 1088 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else  if (m.format == ExportFormats.Avi ||
               m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Mov)
           {
               step = Settings.LimitModH;
               while (n < 1088 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.BluRay)
           {
               reshlist.Add(480);
               reshlist.Add(576);
               reshlist.Add(720);
               reshlist.Add(1080);
           }
           else if (m.format == ExportFormats.Mpeg2PAL ||
               m.format == ExportFormats.AviDVPAL)
           {
               reshlist.Add(576);
           }
           else if (m.format == ExportFormats.Mpeg2NTSC ||
               m.format == ExportFormats.AviDVNTSC ||
               m.format == ExportFormats.Mp4PSPAVCTV)
           {
               reshlist.Add(480);
           }
           else if (m.format == ExportFormats.Mp4Archos5G)
           {
               while (n < 576 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4ToshibaG900)
           {
               while (n < 480 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4PSPAVC)
           {
               while (n < 272 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.PmpAvc)
           {
               step = 16;
               while (n < 272 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4PSPASP)
           {
               while (n < 272 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.DpgNintendoDS)
           {
               while (n < 192 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4BlackBerry8100)
           {
               while (n < 180 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4BlackBerry8800)
           {
               while (n < 180 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4BlackBerry8830 ||
               m.format == ExportFormats.Mp4SonyEricssonK800 ||
               m.format == ExportFormats.Mp4Nokia5700 ||
               m.format == ExportFormats.AviMeizuM6)
           {
               while (n < 240 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4MotorolaK1)
           {
               while (n < 288 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4SonyEricssonK610)
           {
               while (n < 144 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.AviiRiverClix2 ||
               m.format == ExportFormats.ThreeGP)
           {
               while (n < 240 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4iPod55G)
           {
               while (n < 480 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4Prada)
           {
               while (n < 240 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4iPhone)
           {
               while (n < 576 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Mp4AppleTV)
           {
               while (n < 720 + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
           else if (m.format == ExportFormats.Custom)
           {
               step = Settings.LimitModH;
               n = Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "MinResolutionH"));
               while (n < Convert.ToInt32(FormatReader.GetFormatInfo("Custom", "MaxResolutionH")) + step)
               {
                   reshlist.Add(n);
                   n = n + step;
               }
           }
                              
           return reshlist;
       }

       public static int GetMaxVBitrate(Massive m)
       {

           if (m.format == ExportFormats.Mp4iPod55G ||
               m.format == ExportFormats.Mp4iPhone)
               return 1500;

           else if (m.format == ExportFormats.Mp4BlackBerry8100 ||
               m.format == ExportFormats.Mp4BlackBerry8800 ||
               m.format == ExportFormats.Mp4BlackBerry8830 ||
               m.format == ExportFormats.Mp4MotorolaK1 ||
               m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.Mp4Prada ||
               m.format == ExportFormats.ThreeGP ||
               m.format == ExportFormats.Mp4SonyEricssonK800 ||
               m.format == ExportFormats.Mp4SonyEricssonK610 ||
               m.format == ExportFormats.Mp4Nokia5700 ||
               m.format == ExportFormats.AviiRiverClix2 ||
               m.format == ExportFormats.AviMeizuM6)  //Custom
               return 800;

           else if (m.format == ExportFormats.DpgNintendoDS)
               return 512;

           else if (m.format == ExportFormats.Mpeg2PAL ||
               m.format == ExportFormats.Mpeg2NTSC)
           {
               int abitrate = 0;
               if (m.outaudiostreams.Count > 0)
               {
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                   abitrate = outstream.bitrate;
               }
               return 9800 - abitrate;
           }

           else if (m.format == ExportFormats.AviHardware)
               return 4854;

           else
           {
               if (m.outvcodec == "x264")
                   return 90000;//16384;
               else if (m.outvcodec == "MPEG2" ||
                        m.outvcodec == "MPEG1")
                   return 90000;
               else if (m.outvcodec == "MJPEG")
                   return 90000;
               else
                   return 10000;
           }
       }

       public static int GetMaxAACBitrate(Massive m)
       {
           if (m.format == ExportFormats.Mp4BlackBerry8100 ||
               m.format == ExportFormats.Mp4BlackBerry8800 ||
               m.format == ExportFormats.Mp4BlackBerry8830 ||
               m.format == ExportFormats.Mp4MotorolaK1 ||
               m.format == ExportFormats.Mp4iPhone ||
               m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.Mp4Prada ||
               m.format == ExportFormats.Mp4iPod55G ||
               m.format == ExportFormats.ThreeGP ||
               m.format == ExportFormats.Mp4SonyEricssonK800 ||
               m.format == ExportFormats.Mp4SonyEricssonK610 ||
               m.format == ExportFormats.Mp4Nokia5700)
               return 192;

           else
               return 640;
       }

       public static bool Is4GBlimitedFormat(Massive m)
       {
           if (m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.Mp4PSPAVC ||
               m.format == ExportFormats.Mp4PSPAVCTV ||
               m.format == ExportFormats.Mp4BlackBerry8100 ||
               m.format == ExportFormats.Mp4BlackBerry8800 ||
               m.format == ExportFormats.Mp4BlackBerry8830 ||
               m.format == ExportFormats.Mp4MotorolaK1 ||
               m.format == ExportFormats.AviiRiverClix2 ||
               m.format == ExportFormats.Mp4iPhone ||
               m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.Mp4iPod55G ||
               m.format == ExportFormats.Mp4Prada ||
               m.format == ExportFormats.ThreeGP ||
               m.format == ExportFormats.Mp4Archos5G ||
               m.format == ExportFormats.Mp4ToshibaG900 ||
               m.format == ExportFormats.Mp4SonyEricssonK800 ||
               m.format == ExportFormats.Mp4SonyEricssonK610 ||
               m.format == ExportFormats.Mp4Nokia5700 ||
               m.format == ExportFormats.AviMeizuM6 ||
               m.format == ExportFormats.DpgNintendoDS) 
               return true;
           else if (m.format == ExportFormats.Custom && FormatReader.GetFormatInfo("Custom", "Is4GBlimitedFormat") == "yes")
               return true;
           else
           return false;
       }

       public static string ValidateCopyAudio(Massive m)
       {
           AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
           string ext = Path.GetExtension(m.infilepath).ToLower();

           if (ext == ".avs" && instream.audiopath == null) return "Source - AVS-script";
           else if (m.format == ExportFormats.PmpAvc)
           {
               if (instream.codecshort != "AAC" && instream.codecshort != "MP3")
                   return "Codec - " + instream.codecshort;
               else if (instream.samplerate != "44100")
                   return "Samplerate - " + instream.samplerate;
               else return null;
           }
           else if (m.format == ExportFormats.Mp4PSPAVC || m.format == ExportFormats.Mp4PSPASP || m.format == ExportFormats.Mp4PSPAVCTV)
           {
               if (instream.codecshort != "AAC")
                   return "Codec - " + instream.codecshort;
               else return null;
           }
           else if (m.format == ExportFormats.DpgNintendoDS || m.format == ExportFormats.Mpeg1PS)
           {
               if (instream.codecshort != "MP2")
                   return "Codec - " + instream.codecshort;
               else return null;
           }
           else if (m.format == ExportFormats.AviiRiverClix2 || m.format == ExportFormats.AviMeizuM6)
           {
               if (instream.codecshort != "MP3" && instream.codecshort != "MP2")
                   return "Codec - " + instream.codecshort;
               else return null;
           }
           else if (m.format == ExportFormats.Mp4 && instream.codecshort != "AC3" ||
                    m.format == ExportFormats.Mp4AppleTV ||
                    m.format == ExportFormats.Mp4BlackBerry8100 ||
                    m.format == ExportFormats.Mp4BlackBerry8800 ||
                    m.format == ExportFormats.Mp4BlackBerry8830 ||
                    m.format == ExportFormats.Mp4SonyEricssonK800 ||
                    m.format == ExportFormats.Mp4SonyEricssonK610 ||
                    m.format == ExportFormats.Mp4Nokia5700 ||
                    m.format == ExportFormats.Mp4MotorolaK1 ||
                    m.format == ExportFormats.Mp4iPhone ||
                    m.format == ExportFormats.Mp4iPod50G ||
                    m.format == ExportFormats.Mp4iPod55G ||
                    m.format == ExportFormats.Mp4Prada ||
                    m.format == ExportFormats.Mp4PS3 ||
                    m.format == ExportFormats.ThreeGP)
           {
               if (instream.codecshort != "AAC" &&
                   instream.codecshort != "MP3" &&
                   instream.codecshort != "MP2")
                   return "Codec - " + instream.codecshort;
               else return null;
           }
           else if (m.format == ExportFormats.Mpeg2PS || m.format == ExportFormats.Mpeg2PAL || m.format == ExportFormats.Mpeg2NTSC)
           {
               if (instream.codecshort != "AC3" && instream.codecshort != "WAV" && instream.codecshort != "MP2")
                   return "Codec - " + instream.codecshort;
               else return null;
           }
           //else if (m.format == ExportFormats.TS ||
           //    m.format == ExportFormats.M2TS)
           //{
           //    if (instream.codecshort != "AC3" &&
           //        instream.codecshort != "WAV" &&
           //        instream.codecshort != "MP2")
           //        return "PS3 can`t play " + instream.codecshort;
           //    else
           //        return null;
           //}
           else if (m.format == ExportFormats.BluRay)
           {
               if (instream.codecshort != "AC3" && instream.codecshort != "DTS" &&
                   instream.codecshort != "WAV" && instream.codecshort != "PCM")
                   return "Codec - " + instream.codecshort;
               else return null;
           }
           else if (m.format == ExportFormats.Flv)
           {
               if (instream.codecshort != "AAC" && instream.codecshort != "MP3")
                   return "Codec - " + instream.codecshort;
               else if (instream.samplerate != "11025" && instream.samplerate != "22050" && instream.samplerate != "44100")
                   return "Samplerate - " + instream.samplerate;
               else if (instream.channels > 2)
                   return instream.channels + " Channels";
               else return null;
           }
           else return null;
       }

       public static string ValidateCopyVideo(Massive m)
       {
           string ext = Path.GetExtension(m.infilepath).ToLower();

           if (ext == ".avs") return "Source - AVS-script";
           else if (ext == ".d2v") return "Source - DGIndex-project";
           else if (ext == ".dga") return "Source - DGAVCIndex-project";
           else if (ext == ".dgi") return "Source - DGIndexNV-project";
           else if (m.format == ExportFormats.PmpAvc ||
                    m.format == ExportFormats.Mp4PSPAVC ||
                    m.format == ExportFormats.Mp4PSPASP)
           {
               if (m.inresw > 480 || m.inresh > 272)
                   return m.inresw + "x" + m.inresh;
           }
           else if (m.format == ExportFormats.Mp4PSPAVCTV)
           {
               if (m.inresw != 720 || m.inresh != 480)
                   return m.inresw + "x" + m.inresh;
           }
           else if (m.format == ExportFormats.AviiRiverClix2 || m.format == ExportFormats.AviMeizuM6)
           {
               if (m.invcodecshort != "XviD" && m.invcodecshort != "DivX")
                   return "Codec - " + m.invcodecshort;
           }
           else if (m.format == ExportFormats.BluRay)
           {
               if (m.inresw == 1920 && m.inresh == 1080 ||
                   m.inresw == 1280 && m.inresh == 720 ||
                   m.inresw == 720 && m.inresh == 480 ||
                   m.inresw == 720 && m.inresh == 576)
                   return null;
               else
                   return m.inresw + "x" + m.inresh;
           }
           if (m.format.ToString().StartsWith("Mp4"))
           {
               if (m.invcodecshort != "MPEG2" &&
                   m.invcodecshort != "MPEG4" &&
                   m.invcodecshort != "MPEG1" &&
                   m.invcodecshort != "h264" &&
                   m.invcodecshort != "h263" &&
                   m.invcodecshort != "XviD" &&
                   m.invcodecshort != "DivX")
                   return "Codec - " + m.invcodecshort;
           }
           return null;
       }

       public static bool IsDirectRemuxingPossible(Massive m)
       {
           if (Settings.GetFormatPreset(m.format, "direct_remux") == "False")
               return false;

           string ext = Path.GetExtension(m.infilepath).ToLower();

           if (m.format == ExportFormats.Mkv &&
               m.outvcodec == "Copy")
           {
               if (m.outaudiostreams.Count > 0)
               {
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                   if (outstream.codec == "Copy")
                       return false;
               }
           }

           if (m.format == ExportFormats.TS ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.BluRay)
           {          
               if (ext == ".mkv" ||
                   ext == ".vob" ||
                   ext == ".ts" ||
                   ext == ".m2ts" ||
                   ext == ".evo" ||
                   ext == ".mts") //ext == ".mpg" не знаю как получить правильный ID
                   return true;
               else
                   return false;
           }
           else if (m.format == ExportFormats.Mkv)
           {
               if (ext == ".mkv" ||
    ext == ".vob" ||
    ext == ".mp4" ||
    ext == ".mpg" ||
    ext == ".rm" ||
    ext == ".avi" ||
    ext == ".ogm" ||
                   ext == ".mov")
                   return true;
               else
                   return false;
           }
           else if (m.format == ExportFormats.Flv)
           {
               if (ext == ".flv")
               {
                   if (m.outaudiostreams.Count == 0)
                       return true;
                   else
                   {
                       AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                       if (outstream.codec == "Copy" &&
                           !File.Exists(outstream.audiopath))
                           return true;
                       else
                           return false;
                   }
               }
               else
                   return false;
           }
           else
           {
               return false;
           }
       }

       public static string GetValidRAWVideoEXT(Massive m)
       {
           string ext = m.invcodecshort.ToLower();
           string fext = Path.GetExtension(m.infilepath).ToLower();

           if (m.invcodecshort == "DivX" || m.invcodecshort == "XviD") ext = "avi";
           else if (ext.Contains("vp5") || ext.Contains("vp6")) ext = "flv";
           else if (m.invcodecshort == "MPEG1") ext = "m1v";
           else if (m.invcodecshort == "MPEG2") ext = "m2v";
           else if (m.invcodecshort == "h264") ext = "h264";
           else if (m.invcodecshort == "MPEG4") ext = "avi";
           else if (ext.Contains("vc1")) ext = "avi";
           else if (ext.Contains("dv")) ext = "avi";
           else if (ext.Contains("m") && ext.Contains("jp")) ext = "avi"; //M-JPEG
           else if (ext == "huffman" || ext == "hfyu" || ext == "ffvh") ext = "avi";
           else if (ext == "ffv1") ext = "avi";
           else if (fext == "avi") ext = "avi";

           Demuxers dem = GetDemuxer(m);
           Muxers mux = GetMuxer(m);
           if (dem == Demuxers.mp4box && ext == "avi") ext = "m4v";
           if (mux == Muxers.ffmpeg && ext == "h263") ext = "flv";

           return ext;
       }

       public static string GetValidRAWAudioEXT(string codec)
       {
           if (codec == "PCM" || codec == "LPCM") return ".wav";
           else if (codec == "TrueHD") return ".ac3";
           else return "." + codec.ToLower();
       }

       public static string GetValidVPreset(ExportFormats format)
       {
           if (format == ExportFormats.Avi ||
               format == ExportFormats.AviHardware ||
               format == ExportFormats.AviHardwareHD ||
               format == ExportFormats.AviiRiverClix2 ||
               format == ExportFormats.Mp4PSPASP ||
               format == ExportFormats.Mp4Prada ||
               format == ExportFormats.Mp4BlackBerry8100 ||
               format == ExportFormats.Mp4BlackBerry8800 ||
               format == ExportFormats.Mp4BlackBerry8830 ||
               format == ExportFormats.Mp4MotorolaK1 ||
               format == ExportFormats.Mp4SonyEricssonK800 ||
               format == ExportFormats.Mp4SonyEricssonK610)
               return "XviD HQ Ultra";
           else if (format == ExportFormats.AviDVNTSC ||
               format == ExportFormats.AviDVPAL)
               return "DV Video";
           else if (format == ExportFormats.Mpeg2NTSC ||
               format == ExportFormats.Mpeg2PAL)
               return "MPEG2 HQ Ultra";
           else if (format == ExportFormats.Mpeg2PS)
               return "MPEG2 HQ Ultra";
           else if (format == ExportFormats.Mpeg1PS ||
               format == ExportFormats.DpgNintendoDS)
               return "MPEG1 HQ Ultra";
           else if (format == ExportFormats.Mp4ToshibaG900 ||
               format == ExportFormats.AviMeizuM6)
               return "MPEG4 HQ Ultra";
           else if (format == ExportFormats.Flv)
               return "FLV1 HQ Ultra";
           else if (format == ExportFormats.WMV)
               return "WMV3 HQ Ultra";
           else if (format == ExportFormats.BluRay ||
               format == ExportFormats.M2TS ||
               format == ExportFormats.TS ||
               format == ExportFormats.Mkv ||
               format == ExportFormats.Mp4 ||
               format == ExportFormats.Mp4PSPAVC ||
               format == ExportFormats.Mp4PSPAVCTV)
               return "x264 Q21 HQ Film";
           else if (format == ExportFormats.Mp4iPod55G ||
               format == ExportFormats.Mp4iPhone ||
               format == ExportFormats.Mp4PS3)
               return "x264 Q21 HQ";
           else
               return "x264 HQ Ultra";
       }

       public static string GetValidAPreset(ExportFormats format)
       {
           if (format == ExportFormats.Avi ||
               format == ExportFormats.AviHardware ||
               format == ExportFormats.AviHardwareHD ||
               format == ExportFormats.AviiRiverClix2 ||
               format == ExportFormats.Mp4ToshibaG900 ||
               format == ExportFormats.Audio ||
               format == ExportFormats.AviMeizuM6)
               return "MP3 CBR 128k";
           else if (format == ExportFormats.Mp4SonyEricssonK800 ||
               format == ExportFormats.Mp4SonyEricssonK610 ||
                              format == ExportFormats.Flv)
               return "MP3 CBR 96k";
           else if (format == ExportFormats.AviDVNTSC ||
               format == ExportFormats.AviDVPAL)
               return "PCM 16bit";
           else if (format == ExportFormats.Mpeg2NTSC ||
               format == ExportFormats.Mpeg2PAL ||
               format == ExportFormats.Mpeg2PS ||
               format == ExportFormats.M2TS ||
               format == ExportFormats.TS ||
               format == ExportFormats.BluRay)
               return "AC3 384k";
           else if (format == ExportFormats.Mpeg1PS)
               return "MP2 192k";
           else if (format == ExportFormats.DpgNintendoDS)
               return "MP2 128k";
           else if (format == ExportFormats.Mp4Nokia5700)
               return "AAC-LC ABR 96k";

           else if (format == ExportFormats.Mp4BlackBerry8830)
               return "AAC-HE CBR 64k";

           else if (format == ExportFormats.WMV)
               return "WMA3 CBR 128k";

           else
               return "AAC-LC ABR 128k";
       }

       public static Muxers GetMuxer(Massive m)
       {
           if (m.format == Format.ExportFormats.PmpAvc)
               return Muxers.pmpavc;
           else if (m.format == Format.ExportFormats.Mkv)
           {
               // одирование сразу в MKV
               if (m.outaudiostreams.Count == 0 && m.outvcodec == "x264") return Muxers.Disabled;
               else return Muxers.mkvmerge;
           }
           else if (m.format == Format.ExportFormats.Mpeg2PS ||
                    m.format == ExportFormats.Mpeg1PS)
               return Muxers.ffmpeg;
           else if (m.format == Format.ExportFormats.Flv)
           {
               // одирование сразу в FLV
               if (m.outaudiostreams.Count == 0 && m.outvcodec == "x264") return Muxers.Disabled;
               else return Muxers.ffmpeg;
           }
           else if (m.format == ExportFormats.M2TS ||
                    m.format == ExportFormats.TS ||
                    m.format == ExportFormats.BluRay)
               return Muxers.tsmuxer;
           else if (m.format == Format.ExportFormats.Mpeg2NTSC ||
                    m.format == Format.ExportFormats.Mpeg2PAL)
           {
               return Muxers.ffmpeg;
           }
           else if (m.format == ExportFormats.DpgNintendoDS)
               return Muxers.dpgmuxer;
           else if (m.format == Format.ExportFormats.Avi ||
                    m.format == Format.ExportFormats.AviHardware ||
                    m.format == ExportFormats.AviHardwareHD ||
                    m.format == ExportFormats.AviiRiverClix2 ||
                    m.format == ExportFormats.AviMeizuM6)
           {
               if (m.outaudiostreams.Count == 0)
               {
                   if (m.outvcodec == "HUFF" ||
                       m.outvcodec == "FFV1" ||
                       m.outvcodec == "MJPEG" ||
                       m.outvcodec == "MPEG4" ||
                       m.outvcodec == "MPEG2" ||
                       m.outvcodec == "MPEG1" ||
                       m.outvcodec == "FLV1")
                       return Muxers.Disabled;
                   else return Muxers.ffmpeg;
               }
               else
               {
                   AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                   if (m.outvcodec == "HUFF" ||
                       m.outvcodec == "FFV1" ||
                       m.outvcodec == "MJPEG" ||
                       m.outvcodec == "MPEG4" ||
                       m.outvcodec == "MPEG2" ||
                       m.outvcodec == "MPEG1" ||
                       m.outvcodec == "FLV1")
                   {
                       if (outstream.codec == "PCM" || outstream.codec == "LPCM")
                           return Muxers.Disabled;
                       else
                       {
                           if (outstream.codec == "Copy" && instream.codecshort == "PCM" ||
                               outstream.codec == "Copy" && instream.codecshort == "LPCM")
                               return Muxers.ffmpeg;
                           else
                               return Muxers.virtualdubmod;
                       }
                   }
                   //outstream.codec == "MP3" ||
                   //outstream.codec == "PCM" ||
                   //outstream.codec == "LPCM" ||
                   else if (outstream.codec == "Copy" && instream.codecshort == "PCM" ||
                            outstream.codec == "Copy" && instream.codecshort == "LPCM")
                       return Muxers.ffmpeg;
                   else
                       return Muxers.virtualdubmod;
               }
           }
           else if (m.format == Format.ExportFormats.AviDVNTSC ||
                    m.format == Format.ExportFormats.AviDVPAL)
               return Muxers.Disabled;
           else if (m.format == ExportFormats.Audio)
               return Muxers.Disabled;
           else if (m.format == ExportFormats.Custom)
           {
               string CustomMuxer = FormatReader.GetFormatInfo("Custom", "GetMuxer");
               //return (Muxers)Enum.Parse(typeof(Muxers), FormatReader.GetFormatInfo("Custom", "GetMuxer"), true);
               if (CustomMuxer == "pmpavc") return Muxers.pmpavc;
               else if (CustomMuxer == "mkvmerge") return Muxers.mkvmerge;
               else if (CustomMuxer == "ffmpeg") return Muxers.ffmpeg;
               else if (CustomMuxer == "tsmuxer") return Muxers.tsmuxer;
               else if (CustomMuxer == "dpgmuxer") return Muxers.dpgmuxer;
               else if (CustomMuxer == "virtualdubmod") return Muxers.virtualdubmod;
               else if (CustomMuxer == "disabled") return Muxers.Disabled;
               else if (CustomMuxer == "mp4box") return Muxers.mp4box;
               else return Muxers.ffmpeg;
           }
           else if (m.format == Format.ExportFormats.Mp4)
           {
               // одирование сразу в MP4
               if (m.outaudiostreams.Count == 0 && m.outvcodec == "x264") return Muxers.Disabled;
               else return Muxers.mp4box;
           }
           else return Muxers.mp4box;
       }

      public static Demuxers GetDemuxer(Massive m)
      {
          //Muxers mux = GetMuxer(m);
          string ext = Path.GetExtension(m.infilepath);

          if (ext == ".mkv")
              return Demuxers.mkvextract;
          else if (ext == ".dpg")
              return Demuxers.dpgmuxer;
          else if (ext == ".pmp")
              return Demuxers.pmpdemuxer;
          else if (ext == ".mp4" ||
                   ext == ".mov" ||
                   ext == ".3gp")
          {
              if (m.invcodecshort == "DivX" ||
                  m.invcodecshort == "XviD" ||
                  m.invcodecshort == "MPEG4")
                  return Demuxers.ffmpeg;
              else
                  return Demuxers.mp4box;
          }
          else
              return Demuxers.ffmpeg;
      }

       public static Massive GetValidOutAspect(Massive m)
       {
           //метод дл€ форматов с фиксированным аспектом
           if (m.format == ExportFormats.BluRay)
           {
               m.outaspect = (double)m.outresw / (double)m.outresh;
               m.aspectfix = AspectResolution.AspectFixes.Black;
               m.sar = null;
           }
           else if (IsLockedOutAspect(m))
           {
               string[] outaspects = Format.GetValidOutAspects(m);
               m.outaspect = Calculate.GetCloseDouble(m.inaspect, outaspects);

               m.sar = Calculate.ConvertDoubleToPointString(m.outaspect);
               if (m.format == ExportFormats.Mp4PSPAVCTV ||
                   m.format == ExportFormats.BluRay ||
                   m.format == ExportFormats.Custom) //тут
                   m.sar = null;

               m.aspectfix = AspectResolution.AspectFixes.Black;
           }
           //метод дл€ остальных форматов
           else
           {
               if ((double)m.inresw / (double)m.inresh != m.inaspect && 
                   m.aspectfix == AspectResolution.AspectFixes.SAR)
               {
                   m.outaspect = m.inaspect;
                   m = Calculate.CalculateSAR(m);//тут
               }
               else
               {
                   m.outaspect = (double)m.outresw / (double)m.outresh;
                   m.sar = null;
                   m.aspectfix = AspectResolution.AspectFixes.Disabled;
               }
           }
           return m;
       }

       public static bool IsLockedOutAspect(Massive m)
       {
           if (m.format == ExportFormats.AviDVNTSC ||
               m.format == ExportFormats.AviDVPAL ||
               m.format == ExportFormats.Mpeg2NTSC ||
               m.format == ExportFormats.Mpeg2PAL ||
               m.format == ExportFormats.Mp4PSPAVCTV ||
               m.format == ExportFormats.BluRay)
               return true;
           else if (m.format == ExportFormats.Custom && FormatReader.GetFormatInfo("Custom", "IsLockedOutAspect") == "yes")
               return true;
           else
               return false;
       }

       public static string[] GetValidOutAspects(Massive m)
       {
           if (m.format == ExportFormats.AviDVNTSC ||
               m.format == ExportFormats.AviDVPAL)
               return new string[] { "1.3333 (4:3)" };

           else if (m.format == ExportFormats.Mpeg2NTSC ||
               m.format == ExportFormats.Mpeg2PAL)
               return new string[] { "1.3333 (4:3)", "1.7778 (16:9)" };

           else if (m.format == ExportFormats.Mp4PSPAVCTV)
               return new string[] { "1.3333 (4:3)" };

           else if (m.format == ExportFormats.BluRay)
               return new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };

           else if (m.format == ExportFormats.Mp4PSPAVC ||
               m.format == ExportFormats.Mp4PSPASP ||
               m.format == ExportFormats.PmpAvc)
               return new string[] { "1.3333 (4:3)", "1.7647 (16:9)", "1.8500", "2.3529" };

           else if (m.format == ExportFormats.Mp4iPhone)
               return new string[] { "1.3333 (4:3)", "1.5000", "1.7778 (16:9)", "1.8500", "2.3529" };

           else if (m.format == ExportFormats.Mp4ToshibaG900)
               return new string[] { "1.3333 (4:3)", "1.6667", "1.7778 (16:9)", "1.8500", "2.3529" };

           else if (m.format == ExportFormats.Custom)
               return FormatReader.GetFormatInfo2("Custom", "GetValidOutAspects");

           else
               return new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
       }

       public static int GetMaxBFrames(Massive m)
       {
           if (m.outvcodec == "MPEG4")
               return 0;

           else if (m.format == ExportFormats.Avi ||
               m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mov ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Mp4AppleTV)
               return 3;

           else if (m.format == ExportFormats.Mp4PSPAVC ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.TS ||
               m.format == ExportFormats.BluRay)
               return 3;

           else if (m.format == ExportFormats.Mp4PS3)
           {
               if (m.outvcodec == "x264")
                   return 3;
               else
                   return 1; //XviD
           }

           else if (m.format == ExportFormats.AviHardware ||
               m.format == ExportFormats.AviHardwareHD ||
               m.format == ExportFormats.WMV)
               return 1;

           else if (m.format == ExportFormats.Mpeg2NTSC ||
               m.format == ExportFormats.Mpeg2PAL ||
               m.format == ExportFormats.Mpeg2PS ||
               m.format == ExportFormats.Mpeg1PS ||
               m.format == ExportFormats.DpgNintendoDS)
               return 2;

           else if (m.format == ExportFormats.Mp4Archos5G)
           {
               if (m.outvcodec == "x264")
                   return 0;
               else
                   return 3;
           }

           else
               return 0;
       }

       public static bool GetValidPackedMode(Massive m)
       {
           if (m.format == ExportFormats.AviHardware ||
               m.format == ExportFormats.AviHardwareHD  ||
               m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.Mp4ToshibaG900  ||
               m.format == ExportFormats.Mp4SonyEricssonK800 ||
               m.format == ExportFormats.Mp4SonyEricssonK610 ||
               m.format == ExportFormats.Mp4BlackBerry8100 ||
               m.format == ExportFormats.Mp4BlackBerry8800 ||
               m.format == ExportFormats.Mp4BlackBerry8830 ||
               m.format == ExportFormats.Mp4MotorolaK1 ||
               m.format == ExportFormats.Mp4Prada ||
               m.format == ExportFormats.Mp4Nokia5700 ||
               m.format == ExportFormats.AviiRiverClix2 ||
               m.format == ExportFormats.AviMeizuM6)
               return false;
           else
           {
               if (m.encodingmode == Settings.EncodingModes.OnePass ||
                   m.encodingmode == Settings.EncodingModes.Quality ||
                   m.encodingmode == Settings.EncodingModes.Quantizer)
                   return true;
               else
                   return false;
           }
       }

       public static bool GetValidGMC(Massive m)
       {
           if (m.format == ExportFormats.Avi ||
               m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mov ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Mp4AppleTV ||
               m.format == ExportFormats.Flv)
               return true;
           else
               return false;
       }

       public static bool GetValidQPEL(Massive m)
       {
           if (m.format == ExportFormats.Avi ||
               m.format == ExportFormats.Mkv ||
               m.format == ExportFormats.Mov ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.Mp4AppleTV ||
               m.format == ExportFormats.Flv)
               return true;
           else
               return false;
       }

       public static bool GetValidCabac(Massive m)
       {
           if (m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.Mp4iPod55G ||
               m.format == ExportFormats.ThreeGP ||
               m.format == ExportFormats.Mp4Archos5G ||
               m.format == ExportFormats.Mp4ToshibaG900 ||
               m.format == ExportFormats.Mp4Nokia5700)
               return false;
           else
               return true;
       }

       public static bool GetValidBPyramid(Massive m)
       {
           if (m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.TS ||
               m.format == ExportFormats.BluRay)
               return true;
           else
               return false;
       }

       public static bool GetValidBiValue(Massive m)
       {
           if (m.outvcodec == "x264")
           {
               if (m.x264options.bframes > 0)
                   return true;
               else
                   return false;
           }
           else if (m.outvcodec == "XviD")
           {
               if (m.XviD_options.bframes > 0)
                   return true;
               else
                   return false;
           }
           else if (m.outvcodec == "WMV3")
           {
               if (m.wmv_options.bframes > 0)
                   return true;
               else
                   return false;
           }
           else
           {
               if (m.ffmpeg_options.bframes > 0)
                   return true;
               else
                   return false;
           }
       }

       public static int GetValidRefs(Massive m)
       {
           if (m.format == ExportFormats.Mp4PSPAVC ||
               m.format == ExportFormats.Mp4PSPAVCTV ||
               m.format == ExportFormats.PmpAvc ||
               m.format == ExportFormats.Mp4AppleTV ||
               m.format == ExportFormats.Mov ||
               m.format == ExportFormats.Mp4Archos5G ||
               m.format == ExportFormats.Mp4ToshibaG900)
               return 2;
           else if (m.format == ExportFormats.Mp4iPhone ||
               m.format == ExportFormats.Mp4iPod55G)
               return 6;
           else
               return 3;
       }

       public static string GetValidAVCLevel(Massive m)
       {
           if (m.format == ExportFormats.Mp4AppleTV ||
               m.format == ExportFormats.Mp4iPhone ||
               m.format == ExportFormats.Mp4iPod55G ||
               m.format == ExportFormats.Mp4PSPAVC ||
               m.format == ExportFormats.Mp4PSPAVCTV ||
               m.format == ExportFormats.PmpAvc ||
               m.format == ExportFormats.Mov)
               return "3.0";
           else if (m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.TS ||
               m.format == ExportFormats.BluRay)
               return "4.1";
           else if (m.format == ExportFormats.Mp4Archos5G)
               return "4.0";
           else if (m.format == ExportFormats.Mp4iPod50G ||
               m.format == ExportFormats.ThreeGP ||
               m.format == ExportFormats.Mp4ToshibaG900 ||
               m.format == ExportFormats.Mp4Nokia5700)
               return "1.3";
           else
               return "unrestricted";
       }

       public static string GetValidMacroblocks(Massive m)
       {
           if (m.format == ExportFormats.Mp4PS3 ||
               m.format == ExportFormats.Mp4 ||
               m.format == ExportFormats.M2TS ||
               m.format == ExportFormats.TS ||
               m.format == ExportFormats.BluRay)
               return "all"; //"all", "p8x8,b8x8,i4x4,i8x8"
           else
               return "p8x8,b8x8,i4x4,p4x4";
       }

       public static bool GetValidDCT(Massive m)
       {
           string macro = GetValidMacroblocks(m);
           if (macro == "all") //"all", "p8x8,b8x8,i4x4,i8x8"
               return true;
           else
               return false;
       }

       public static int GetValidTrellis(Massive m, int trellis)
       {
           if (m.format == ExportFormats.Mp4Archos5G)
               return 0;
           else
               return trellis;
       }

       public static Massive GetValidVEncodingMode(Massive m)
       {
           //XviD x264 MPEG4 MPEG2 HUFF DV FLV1 FFV1
           if (m.outvcodec == "XviD")
           {
               if (m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                   m.encodingmode = Settings.EncodingModes.ThreePassQuality;
               if (m.encodingmode == Settings.EncodingModes.Quantizer)
                   m.encodingmode = Settings.EncodingModes.Quality;
               if (m.encodingmode == Settings.EncodingModes.OnePassSize)
                   m.encodingmode = Settings.EncodingModes.TwoPassSize;
           }

           if (m.outvcodec == "x264")
           {
               if (m.encodingmode == Settings.EncodingModes.OnePassSize)
                   m.encodingmode = Settings.EncodingModes.TwoPassSize;
           }

           if (m.outvcodec == "MPEG4" ||
               m.outvcodec == "MPEG2" ||
               m.outvcodec == "FLV1" ||
               m.outvcodec == "MPEG1")
           {
               if (m.encodingmode == Settings.EncodingModes.ThreePass)
                   m.encodingmode = Settings.EncodingModes.TwoPass;
               if (m.encodingmode == Settings.EncodingModes.Quantizer)
                   m.encodingmode = Settings.EncodingModes.Quality;
               if (m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                   m.encodingmode == Settings.EncodingModes.OnePassSize)
                   m.encodingmode = Settings.EncodingModes.TwoPassSize;
               if (m.encodingmode == Settings.EncodingModes.ThreePassQuality)
                   m.encodingmode = Settings.EncodingModes.TwoPassQuality;
           }

           if (m.outvcodec == "MJPEG")
           {
               if (m.encodingmode == Settings.EncodingModes.ThreePass ||
                   m.encodingmode == Settings.EncodingModes.TwoPass)
                   m.encodingmode = Settings.EncodingModes.OnePass;
               if (m.encodingmode == Settings.EncodingModes.Quantizer ||
                   m.encodingmode == Settings.EncodingModes.ThreePassQuality ||
                   m.encodingmode == Settings.EncodingModes.TwoPassQuality)
                   m.encodingmode = Settings.EncodingModes.Quality;
               if (m.encodingmode == Settings.EncodingModes.ThreePassSize ||
                   m.encodingmode == Settings.EncodingModes.TwoPassSize)
                   m.encodingmode = Settings.EncodingModes.OnePassSize;
           }

           if (m.outvcodec == "HUFF" ||
               m.outvcodec == "DV" ||
               m.outvcodec == "FFV1")
           {
               if (m.encodingmode != Settings.EncodingModes.OnePass)
                   m.encodingmode = Settings.EncodingModes.OnePass;
           }
           //m.encodingmode = Settings.EncodingModes.OnePass;
           return m;
       }

    }
}
