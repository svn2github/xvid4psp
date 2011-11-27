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
           Audio = 0,
           Avi,
           AviHardware,
           AviHardwareHD,
           AviiRiverClix2,
           AviMeizuM6,
           AviDVNTSC,
           AviDVPAL,
           Flv,
           Mkv,
           ThreeGP,
           Mov,
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
           Mp4PSPAVCTV,
           Mp4PSPAVC,
           Mp4PSPASP,
           Mpeg1PS,
           Mpeg2PS,
           Mpeg2NTSC,
           Mpeg2PAL,
           TS,
           M2TS,
           BluRay,
           PmpAvc,
           DpgNintendoDS,
           Custom
       }

       public enum Muxers { ffmpeg = 1, mp4box, mkvmerge, pmpavc, virtualdubmod, tsmuxer, dpgmuxer, Disabled };
       public enum Demuxers { ffmpeg, mp4box, pmpdemuxer, mkvextract, tsmuxer, dpgmuxer };

       public static string EnumToString(ExportFormats formatenum)
       {
           if (formatenum == ExportFormats.Audio) return "Audio";
           if (formatenum == ExportFormats.Avi) return "AVI";
           if (formatenum == ExportFormats.AviHardware) return "AVI Hardware";
           if (formatenum == ExportFormats.AviHardwareHD) return "AVI Hardware HD";
           if (formatenum == ExportFormats.AviiRiverClix2) return "AVI iRiver Clix 2";
           if (formatenum == ExportFormats.AviMeizuM6) return "AVI Meizu M6";
           if (formatenum == ExportFormats.AviDVNTSC) return "AVI DV NTSC";
           if (formatenum == ExportFormats.AviDVPAL) return "AVI DV PAL";
           if (formatenum == ExportFormats.Flv) return "FLV";
           if (formatenum == ExportFormats.Mkv) return "MKV";
           if (formatenum == ExportFormats.Mp4) return "MP4";
           if (formatenum == ExportFormats.ThreeGP) return "3GP";
           if (formatenum == ExportFormats.Mov) return "MOV";
           if (formatenum == ExportFormats.Mp4Archos5G) return "MP4 Archos 5G";
           if (formatenum == ExportFormats.Mp4ToshibaG900) return "MP4 Toshiba G900";
           if (formatenum == ExportFormats.Mp4BlackBerry8100) return "MP4 BlackBerry 8100";
           if (formatenum == ExportFormats.Mp4BlackBerry8800) return "MP4 BlackBerry 8800";
           if (formatenum == ExportFormats.Mp4BlackBerry8830) return "MP4 BlackBerry 8830";
           if (formatenum == ExportFormats.Mp4SonyEricssonK610) return "MP4 SonyEricsson K610";
           if (formatenum == ExportFormats.Mp4SonyEricssonK800) return "MP4 SonyEricsson K800";
           if (formatenum == ExportFormats.Mp4MotorolaK1) return "MP4 Motorola K1";
           if (formatenum == ExportFormats.Mp4Nokia5700) return "MP4 Nokia 5700";
           if (formatenum == ExportFormats.Mp4iPod50G) return "MP4 iPod 5.0G";
           if (formatenum == ExportFormats.Mp4iPod55G) return "MP4 iPod 5.5G";
           if (formatenum == ExportFormats.Mp4iPhone) return "MP4 iPhone or Touch";
           if (formatenum == ExportFormats.Mp4AppleTV) return "MP4 Apple TV";
           if (formatenum == ExportFormats.Mp4Prada) return "MP4 Prada";
           if (formatenum == ExportFormats.Mp4PS3) return "MP4 PS3 or XBOX360";
           if (formatenum == ExportFormats.Mp4PSPAVC) return "MP4 PSP AVC";
           if (formatenum == ExportFormats.Mp4PSPAVCTV) return "MP4 PSP AVC TV";
           if (formatenum == ExportFormats.Mp4PSPASP) return "MP4 PSP ASP";
           if (formatenum == ExportFormats.Mpeg2PS) return "MPEG2 PS";
           if (formatenum == ExportFormats.Mpeg1PS) return "MPEG1 PS";
           if (formatenum == ExportFormats.Mpeg2NTSC) return "MPEG2 NTSC";
           if (formatenum == ExportFormats.Mpeg2PAL) return "MPEG2 PAL";
           if (formatenum == ExportFormats.PmpAvc) return "PMP AVC";
           if (formatenum == ExportFormats.M2TS) return "M2TS";
           if (formatenum == ExportFormats.TS) return "TS";
           if (formatenum == ExportFormats.DpgNintendoDS) return "DPG Nintendo DS";
           if (formatenum == ExportFormats.BluRay) return "BluRay";
           if (formatenum == ExportFormats.Custom) return "Custom";

           return null;
       }

       public static ExportFormats StringToEnum(string stringformat)
       {
           if (stringformat == "Audio") return ExportFormats.Audio;
           if (stringformat == "AVI") return ExportFormats.Avi;
           if (stringformat == "AVI Hardware") return ExportFormats.AviHardware;
           if (stringformat == "AVI Hardware HD") return ExportFormats.AviHardwareHD;
           if (stringformat == "AVI iRiver Clix 2") return ExportFormats.AviiRiverClix2;
           if (stringformat == "AVI Meizu M6") return ExportFormats.AviMeizuM6;
           if (stringformat == "AVI DV NTSC") return ExportFormats.AviDVNTSC;
           if (stringformat == "AVI DV PAL") return ExportFormats.AviDVPAL;
           if (stringformat == "FLV") return ExportFormats.Flv;
           if (stringformat == "MKV") return ExportFormats.Mkv;
           if (stringformat == "MP4") return ExportFormats.Mp4;
           if (stringformat == "3GP") return ExportFormats.ThreeGP;
           if (stringformat == "MOV") return ExportFormats.Mov;
           if (stringformat == "MP4 Archos 5G") return ExportFormats.Mp4Archos5G;
           if (stringformat == "MP4 Toshiba G900") return ExportFormats.Mp4ToshibaG900;
           if (stringformat == "MP4 BlackBerry 8100") return ExportFormats.Mp4BlackBerry8100;
           if (stringformat == "MP4 BlackBerry 8800") return ExportFormats.Mp4BlackBerry8800;
           if (stringformat == "MP4 BlackBerry 8830") return ExportFormats.Mp4BlackBerry8830;
           if (stringformat == "MP4 SonyEricsson K800") return ExportFormats.Mp4SonyEricssonK800;
           if (stringformat == "MP4 SonyEricsson K610") return ExportFormats.Mp4SonyEricssonK610;
           if (stringformat == "MP4 Motorola K1") return ExportFormats.Mp4MotorolaK1;
           if (stringformat == "MP4 Nokia 5700") return ExportFormats.Mp4Nokia5700;
           if (stringformat == "MP4 iPod 5.0G") return ExportFormats.Mp4iPod50G;
           if (stringformat == "MP4 iPod 5.5G") return ExportFormats.Mp4iPod55G;
           if (stringformat == "MP4 iPhone or Touch") return ExportFormats.Mp4iPhone;
           if (stringformat == "MP4 Apple TV") return ExportFormats.Mp4AppleTV;
           if (stringformat == "MP4 Prada") return ExportFormats.Mp4Prada;
           if (stringformat == "MP4 PS3 or XBOX360") return ExportFormats.Mp4PS3;
           if (stringformat == "MP4 PSP AVC") return ExportFormats.Mp4PSPAVC;
           if (stringformat == "MP4 PSP AVC TV") return ExportFormats.Mp4PSPAVCTV;
           if (stringformat == "MP4 PSP ASP") return ExportFormats.Mp4PSPASP;
           if (stringformat == "MPEG2 PS") return ExportFormats.Mpeg2PS;
           if (stringformat == "MPEG1 PS") return ExportFormats.Mpeg1PS;
           if (stringformat == "MPEG2 NTSC") return ExportFormats.Mpeg2NTSC;
           if (stringformat == "MPEG2 PAL") return ExportFormats.Mpeg2PAL;
           if (stringformat == "PMP AVC") return ExportFormats.PmpAvc;
           if (stringformat == "M2TS") return ExportFormats.M2TS;
           if (stringformat == "TS") return ExportFormats.TS;
           if (stringformat == "DPG Nintendo DS") return ExportFormats.DpgNintendoDS;
           if (stringformat == "BluRay") return ExportFormats.BluRay;
           if (stringformat == "Custom") return ExportFormats.Custom;

           return ExportFormats.Mp4PSPAVC;
       }

       public static string[] GetFormatList()
       {
           ArrayList formatlist = new ArrayList();
           foreach (ExportFormats cformat in Enum.GetValues(typeof(ExportFormats)))
           {
               formatlist.Add(EnumToString(cformat));
           }
           return Calculate.ConvertArrayListToStringArray(formatlist);
       }

       public static string[] GetVCodecsList(ExportFormats format)
       {
           //XviD x264 MPEG4 MPEG2 HUFF DV FLV1 FFV1

           switch (format)
           {
               case ExportFormats.Mpeg1PS:
               case ExportFormats.DpgNintendoDS:
                   return new string[] { "MPEG1" };

               case ExportFormats.BluRay:
                   return new string[] { "MPEG2", "x264" };

               case ExportFormats.PmpAvc:
                   return new string[] { "x264", "MPEG4", "XviD" };
           }

           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).VCodecs_IsEditable)
               {
                   return Formats.GetSettings(format, "VCodecs", Formats.GetDefaults(format).VCodecs);
               }

               return Formats.GetDefaults(format).VCodecs;
           }

           return new string[] { "x264", "MPEG4", "FLV1", "MJPEG", "HUFF", "FFV1", "XviD" };
       }

       public static string[] GetACodecsList(ExportFormats format)
       {
           switch (format)
           {
               case ExportFormats.Audio:
                   return new string[] { "PCM", "FLAC", "AC3", "MP3", "MP2", "AAC" };

               case ExportFormats.Mpeg1PS:
               case ExportFormats.DpgNintendoDS:
                   return new string[] { "MP2" };

               case ExportFormats.BluRay:
                   return new string[] { "PCM", "AC3" };

               case ExportFormats.PmpAvc:
                   return new string[] { "MP3", "AAC" };
           }

           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).ACodecs_IsEditable)
               {
                   return Formats.GetSettings(format, "ACodecs", Formats.GetDefaults(format).ACodecs);
               }

               return Formats.GetDefaults(format).ACodecs;
           }

           return new string[] { "PCM", "FLAC", "AC3", "MP3", "MP2", "AAC" };
       }

       public static AviSynthScripting.Decoders GetValidVDecoder(Massive m)
       {
           if (!m.isvideo)
           {
               return AviSynthScripting.Decoders.BlankClip;
           }

           string ext = Path.GetExtension(m.infilepath).ToLower().TrimStart(new char[] { '.' });
           if (ext == "avs") return AviSynthScripting.Decoders.Import;

           if (m.indexfile != null)
           {
               if (ext == "d2v") return AviSynthScripting.Decoders.MPEG2Source;
               else if (ext == "dga") return AviSynthScripting.Decoders.AVCSource;
               else if (ext == "dgi") return AviSynthScripting.Decoders.DGMultiSource;
           }

           string mpeg_dec = "", other_dec = AviSynthScripting.Decoders.DirectShowSource.ToString(); //Дефолты
           foreach (string line in (Settings.VDecoders.ToLower().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)))
           {
               string[] extension_and_decoder = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
               if (extension_and_decoder.Length == 2)
               {
                   string extension = extension_and_decoder[0].Trim();
                   string decoder = extension_and_decoder[1].Trim();

                   if (extension == Decoders_Settings.mpeg_psts) mpeg_dec = decoder;
                   else if (extension == Decoders_Settings.other_files) other_dec = decoder;
                   else if (extension == ext && decoder.Length > 0)
                   {
                       //Мы нашли декодер для этого расширения
                       return (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), decoder, true);
                   }
               }
           }

           //mpeg_ps/ts
           if (mpeg_dec.Length > 0 && Calculate.IsMPEG(m.infilepath) && (string.IsNullOrEmpty(m.invcodecshort) || (m.invcodecshort == "MPEG1" || m.invcodecshort == "MPEG2")))
           {
               return (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), mpeg_dec, true);
           }

           if (ext == "pmp") return AviSynthScripting.Decoders.DirectShowSource;
           else if (ext == "vdr") return AviSynthScripting.Decoders.AVISource;
           else if (ext == "y4m" || ext == "yuv") return AviSynthScripting.Decoders.RawSource;
           else if (other_dec.Length > 0) return (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), other_dec, true);
           else return AviSynthScripting.Decoders.DirectShowSource;
       }

       public static AudioStream GetValidADecoder(AudioStream instream)
       {
           if (instream.audiopath != null)
           {
               string ext = Path.GetExtension(instream.audiopath).ToLower().TrimStart(new char[] { '.' });
               string other_dec = AviSynthScripting.Decoders.bassAudioSource.ToString(); //Дефолт
               foreach (string line in (Settings.ADecoders.ToLower().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)))
               {
                   string[] extension_and_decoder = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                   if (extension_and_decoder.Length == 2)
                   {
                       string extension = extension_and_decoder[0].Trim();
                       string decoder = extension_and_decoder[1].Trim();

                       if (extension == Decoders_Settings.other_files) other_dec = decoder;
                       else if (extension == ext && decoder.Length > 0)
                       {
                           //Мы нашли декодер для этого расширения
                           instream.decoder = (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), decoder, true);
                           return instream;
                       }
                   }
               }

               if (other_dec.Length > 0) instream.decoder = (AviSynthScripting.Decoders)Enum.Parse(typeof(AviSynthScripting.Decoders), other_dec, true);
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
               if (Formats.GetDefaults(m.format).IsEditable)
               {
                   bool interlace_allowed = false;
                   if (Formats.GetDefaults(m.format).Interlaced_IsEditable)
                       interlace_allowed = Formats.GetSettings(m.format, "Interlaced", Formats.GetDefaults(m.format).Interlaced);
                   else
                       interlace_allowed = Formats.GetDefaults(m.format).Interlaced;

                   if (m.outvcodec == "MPEG2" ||
                       m.outvcodec == "DV" ||
                       m.outvcodec == "HUFF" ||
                       m.outvcodec == "FFV1" ||
                       m.outvcodec == "x264" ||
                       m.outvcodec == "XviD")
                   {
                       if (interlace_allowed)
                           m.deinterlace = DeinterlaceType.Disabled;
                       else
                           m.deinterlace = Settings.Deint_Interlaced;
                   }
                   else
                       m.deinterlace = Settings.Deint_Interlaced;
               }
               else if (m.format == ExportFormats.BluRay)
               {
                   if (!Convert.ToBoolean(Settings.GetFormatPreset(Format.ExportFormats.BluRay, "interlaced")))
                       m.deinterlace = Settings.Deint_Interlaced;
                   else
                       m.deinterlace = DeinterlaceType.Disabled;
               }
               else
                   m.deinterlace = Settings.Deint_Interlaced;
           }
           else if (m.interlace == SourceType.FILM || m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                    m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM)
           {
               m.deinterlace = Settings.Deint_Film;
           }
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
           else
               return 6;
       }

       public static Massive GetValidChannelsConverter(Massive m)
       {
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
           {
               AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

               instream.channelconverter = (AudioOptions.ChannelConverters)Enum.Parse(typeof(AudioOptions.ChannelConverters), Settings.ChannelsConverter, true); //AudioOptions.ChannelConverters.KeepOriginalChannels;
               int n = GetSettingsChannels();

               bool limited_to_stereo = false;
               if (Formats.GetDefaults(m.format).IsEditable)
               {
                   if (Formats.GetDefaults(m.format).LimitedToStereo_IsEditable)
                       limited_to_stereo = Formats.GetSettings(m.format, "LimitedToStereo", Formats.GetDefaults(m.format).LimitedToStereo);
                   else
                       limited_to_stereo = Formats.GetDefaults(m.format).LimitedToStereo;
               }

               if (m.format == ExportFormats.PmpAvc)
               {
                   if (instream.channels != 2 && n != 2)
                       instream.channelconverter = AudioOptions.ChannelConverters.ConvertToDolbyProLogicII;
                   if (instream.channels == 2 && n != 0)
                       instream.channelconverter = AudioOptions.ChannelConverters.KeepOriginalChannels;
               }
               else if (outstream.codec == "MP2" || outstream.codec == "MP3" || limited_to_stereo)
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
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
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

       //Хоть это и из инструкции, но некоторые недопустимые частоты не отлавливаются (42240->32000)
       public static Massive GetValidSamplerateModifer(Massive m)
       {
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0 &&
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
           else if (m.format == ExportFormats.DpgNintendoDS)
               return new string[] { "32000", "48000" }; //"32768"
           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               string[] rates;
               if (Formats.GetDefaults(m.format).Samplerates_IsEditable)
               {
                   rates = Formats.GetSettings(m.format, "Samplerates", Formats.GetDefaults(m.format).Samplerates);
               }
               else
                   rates = Formats.GetDefaults(m.format).Samplerates;

               bool auto = false;
               foreach (string rate in rates)
               {
                   if (rate.ToLower() == "auto") auto = true;
               }
               if (!auto) return rates;
           }

           AudioStream outstream;
           if (m.outaudiostreams.Count > 0)
               outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
           else
           {
               outstream = new AudioStream();
               outstream.encoding = Settings.GetAEncodingPreset(Settings.FormatOut);
               outstream.codec = PresetLoader.GetACodec(m.format, outstream.encoding);
           }

           if (m.format == ExportFormats.Flv)
               return new string[] { "44100" };
           else if (m.format == ExportFormats.AviDVPAL || m.format == ExportFormats.AviDVNTSC)
               return new string[] { "32000", "48000" };
           else if (outstream.codec == "MP3" || outstream.codec == "MP2")
               return new string[] { "32000", "44100", "48000" };
           else if (outstream.codec == "AC3")
           {
               if (m.format == ExportFormats.AviHardware || m.format == ExportFormats.AviHardwareHD ||
                   m.format == ExportFormats.Mpeg2PS || m.format == ExportFormats.Mpeg2PAL ||
                   m.format == ExportFormats.Mpeg2NTSC || m.format == ExportFormats.TS ||
                   m.format == ExportFormats.M2TS || m.format == ExportFormats.BluRay) //Audio mp4 mov mkv
                   return new string[] { "48000" };
               else
                   return new string[] { "32000", "44100", "48000" };
           }
           else if (outstream.codec == "PCM" || outstream.codec == "LPCM")
           {
               if (m.format == ExportFormats.TS || m.format == ExportFormats.M2TS || m.format == ExportFormats.BluRay)
                   return new string[] { "48000", "96000", "192000" };
               else if (m.format == ExportFormats.Mpeg2PS || m.format == ExportFormats.Mpeg2PAL || m.format == ExportFormats.Mpeg2NTSC)
                   return new string[] { "48000" };
               else
                   return new string[] { "22050", "32000", "44100", "48000", "96000", "192000" };
           }
           else
               return new string[] { "22050", "32000", "44100", "48000" };
       }

       public static Massive GetValidFramerate(Massive m)
       {
           if (m.format == ExportFormats.DpgNintendoDS)
           {
               if ((double)m.outresw / (double)m.outresh < 1.5)
                   m.outframerate = "20.000";
               else
                   m.outframerate = "22.000";
           }
           else
           {
               //Пересчет fps с учетом деинтерлейсера и ограничений форматов
               m = Calculate.UpdateOutFramerate(m);
           }

           return m;
       }

       public static string[] GetValidFrameratesList(Massive m)
       {
           if (m.format == ExportFormats.DpgNintendoDS)
               return new string[] { "20.000", "22.000", "24.000", "25.000" };

           else if (m.format == ExportFormats.Mpeg1PS || m.format == ExportFormats.BluRay)
               return new string[] { "23.976", "24.000", "25.000", "29.970", "30.000", "50.000", "59.940" };

           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               if (Formats.GetDefaults(m.format).Framerates_IsEditable)
               {
                   return Formats.GetSettings(m.format, "Framerates", Formats.GetDefaults(m.format).Framerates);
               }

               return Formats.GetDefaults(m.format).Framerates;
           }

           return new string[] { "11.000", "15.000", "18.000", "20.000", "23.976", "24.000", "25.000", "29.970" };
       }

       public static string GetValidExtension(Massive m)
       {
           switch (m.format)
           {
               case ExportFormats.PmpAvc:
                   return ".pmp";

               case ExportFormats.Mpeg1PS:
                   return ".mpg";

               case ExportFormats.BluRay:
                   return ".m2ts";

               case ExportFormats.DpgNintendoDS:
                   return ".dpg";

               case ExportFormats.Audio:
                   {
                       if (m.outaudiostreams.Count > 0)
                       {
                           AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                           if (outstream.codec == "AAC") return ".m4a";
                           else if (outstream.codec == "PCM") return ".wav";
                           else return "." + outstream.codec.ToLower();
                       }
                       return ".nope";
                   }
           }

           if (Formats.GetDefaults(m.format).IsEditable)
           {
               if (Formats.GetDefaults(m.format).Extensions.Length > 1)
               {
                   string ext = Formats.GetSettings(m.format, "Extension", Formats.GetDefaults(m.format).Extension).ToLower();
                   foreach (string exts in Formats.GetDefaults(m.format).Extensions)
                   {
                       if (exts == "*" || exts == ext) return "." + ext;
                   }
               }

               return "." + Formats.GetDefaults(m.format).Extension;
           }

           return ".mp4";
       }

       public static void GetLimitedRes(Format.ExportFormats format, ref int MaxW, ref int MaxH)
       {
           //Максимальное разрешение, устанавливаемое в Авто-режиме
           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).Resolution_IsEditable)
               {
                   MaxW = Formats.GetSettings(format, "MidW", Formats.GetDefaults(format).MidW);
                   MaxH = Formats.GetSettings(format, "MidH", Formats.GetDefaults(format).MidH);
               }
               else
               {
                   MaxW = Formats.GetDefaults(format).MidW;
                   MaxH = Formats.GetDefaults(format).MidH;
               }
           }
       }

       public static Massive GetValidResolution(Massive m)
       {
           ArrayList wlist = GetResWList(m);
           ArrayList hlist = GetResHList(m);

           //Определяем лимиты
           int MaxW = (int)wlist[wlist.Count - 1];
           int MaxH = (int)hlist[hlist.Count - 1];
           GetLimitedRes(m.format, ref MaxW, ref MaxH);

           bool anamorph_allowed = false;
           if (Formats.GetDefaults(m.format).IsEditable)
           {
               if (Formats.GetDefaults(m.format).Anamorphic_IsEditable)
                   anamorph_allowed = Formats.GetSettings(m.format, "Anamorphic", Formats.GetDefaults(m.format).Anamorphic);
               else
                   anamorph_allowed = Formats.GetDefaults(m.format).Anamorphic;
           }

           //Обработка анаморфа
           if (anamorph_allowed || m.aspectfix == AspectResolution.AspectFixes.SAR)
           {
               if (m.blackh == 0 && m.blackw == 0)
               {
                   //Накладываем лимиты
                   int w = Math.Min(m.inresw, MaxW);
                   int h = Math.Min(m.inresh, MaxH);

                   //Если у нас анаморф на входе или сработали лимиты
                   if ((double)m.inresw / (double)m.inresh != m.inaspect ||
                       m.inresw != w || m.inresh != h)
                   {
                       //Пересчет разрешения с учетом откропленного и лимитов
                       w = Math.Min(m.inresw - m.cropl - m.cropr, MaxW);
                       h = Math.Min(m.inresh - m.cropt - m.cropb, MaxH);
                       m.aspectfix = AspectResolution.AspectFixes.SAR;
                   }

                   //Еще раз перепроверяем
                   m.outresw = Calculate.GetCloseIntegerAL(w, wlist);
                   m.outresh = Calculate.GetCloseIntegerAL(h, hlist);
                   return m;
               }
           }

           //ограничение W*H
           int limit = (int)wlist[wlist.Count - 1] * (int)hlist[hlist.Count - 1];
           if (m.format == ExportFormats.Mp4PSPASP) limit = 100100;

           //первичное получение разрешений
           m.outresw = Calculate.GetCloseIntegerAL(m.inresw, wlist);
           m.outresh = Convert.ToInt32(m.outresw / m.inaspect); //Высота

           if (m.outresh > MaxH)
           {
               m.outresh = Calculate.GetCloseIntegerAL(m.inresh, hlist);
               m.outresw = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresh * m.inaspect), wlist);
           }
           else
           {
               //m.outresw = Calculate.GetCloseIntegerAL(m.inresw, wlist);
               m.outresh = Calculate.GetCloseIntegerAL(m.outresh, hlist); //Convert.ToInt32(m.outresw / m.inaspect), hlist);
           }

           //выбираем по какой стороне подбирать
           if (m.outresh > MaxH)
           {
               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit || m.outresw > MaxW || m.outresh > MaxH)
               {
                   m.outresh = Calculate.GetCloseIntegerAL(m.outresh - GetValidModH(m.format), hlist);
                   m.outresw = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresh * m.inaspect), wlist);
               }
           }
           else
           {
               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit || m.outresw > MaxW || m.outresh > MaxH)
               {
                   m.outresw = Calculate.GetCloseIntegerAL(m.outresw - GetValidModW(m.format), wlist);
                   m.outresh = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresw / m.inaspect), hlist);
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

           //При кодировании с сохранением анаморфа, высота равна исходной высоте минус всё что откроплено. 
           if (m.aspectfix == AspectResolution.AspectFixes.SAR)
           {
               //Определяем лимит
               int MaxW = 0;
               int MaxH = (int)hlist[hlist.Count - 1];
               GetLimitedRes(m.format, ref MaxW, ref MaxH);

               //Пересчет высоты с учетом откропленного и лимита
               m.outresh = Calculate.GetCloseIntegerAL(Math.Min(m.inresh - m.cropt - m.cropb, MaxH), hlist);
               return m;
           }

           //ограничение W*H
           int limit = (int)wlist[wlist.Count - 1] * (int)hlist[hlist.Count - 1];
           if (m.format == ExportFormats.Mp4PSPASP) limit = 100100;

           //первичное получение разрешений
           m.outresh = Convert.ToInt32(w / m.inaspect);

           //выбираем по какой стороне подбирать
           if (m.outresh > (int)hlist[hlist.Count - 1])
           {
               m.outresh = Calculate.GetCloseIntegerAL(m.outresh, hlist);
               m.outresw = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresh * m.inaspect), wlist);

               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit)
               {
                   m.outresh = Calculate.GetCloseIntegerAL(m.inresh - GetValidModH(m.format), hlist);
                   m.outresw = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresh * m.inaspect), wlist);
               }
           }
           else
           {
               m.outresh = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresw / m.inaspect), hlist);

               //перебираем пока разрешение не будет в норме
               while ((m.outresw * m.outresh) > limit)
               {
                   m.outresw = Calculate.GetCloseIntegerAL(m.outresw - GetValidModW(m.format), wlist);
                   m.outresh = Calculate.GetCloseIntegerAL(Convert.ToInt32(m.outresw / m.inaspect), hlist);
               }
           }

           return m;
       }

       public static int GetValidModW(ExportFormats format)
       {
           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).Resolution_IsEditable)
                   return Formats.GetSettings(format, "ModW", Formats.GetDefaults(format).ModW);
               else
                   return Formats.GetDefaults(format).ModW;
           }

           return 16;
       }

       public static int GetValidModH(ExportFormats format)
       {
           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).Resolution_IsEditable)
                   return Formats.GetSettings(format, "ModH", Formats.GetDefaults(format).ModH);
               else
                   return Formats.GetDefaults(format).ModH;
           }

           return 8;
       }

       public static ArrayList GetResWList(Massive m)
       {
           int n = 16, step = 16;
           ArrayList reswlist = new ArrayList();

           if (m.format == ExportFormats.Mpeg1PS)
           {
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
           else if (m.format == ExportFormats.PmpAvc)
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
           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               step = GetValidModW(m.format);
               int min = Formats.GetDefaults(m.format).MinW;
               int max = Formats.GetDefaults(m.format).MaxW;
               if (Formats.GetDefaults(m.format).Resolution_IsEditable)
               {
                   min = Formats.GetSettings(m.format, "MinW", min);
                   max = Formats.GetSettings(m.format, "MaxW", max);
               }

               while (min < max + step)
               {
                   reswlist.Add(min);
                   min += step;
               }
           }
           else
           {
               while (n < 1920 + step)
               {
                   reswlist.Add(n);
                   n = n + step;
               }
           }

           return reswlist;
       }

       public static ArrayList GetResHList(Massive m)
       {
           int n = 16, step = 8;
           ArrayList reshlist = new ArrayList();

           if (m.format == ExportFormats.Mpeg1PS)
           {
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
           else if (m.format == ExportFormats.PmpAvc)
           {
               step = 16;
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
           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               step = GetValidModH(m.format);
               int min = Formats.GetDefaults(m.format).MinH;
               int max = Formats.GetDefaults(m.format).MaxH;
               if (Formats.GetDefaults(m.format).Resolution_IsEditable)
               {
                   min = Formats.GetSettings(m.format, "MinH", min);
                   max = Formats.GetSettings(m.format, "MaxH", max);
               }

               while (min < max + step)
               {
                   reshlist.Add(min);
                   min += step;
               }
           }
           else
           {
               while (n < 1088 + step)
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
               m.format == ExportFormats.AviMeizuM6)
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
           //Просто выводит предупреждение, если ожидаемый размер файла > 4Gb
           if (m.format == ExportFormats.DpgNintendoDS)
               return true;
           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               if (Formats.GetDefaults(m.format).LimitedTo4Gb_IsEditable)
                   return Formats.GetSettings(m.format, "LimitedTo4Gb", Formats.GetDefaults(m.format).LimitedTo4Gb);
               else
                   return Formats.GetDefaults(m.format).LimitedTo4Gb;
           }
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
           else if (m.format == Format.ExportFormats.Avi ||
               m.format == Format.ExportFormats.AviHardware ||
               m.format == Format.ExportFormats.AviDVPAL ||
               m.format == ExportFormats.AviDVNTSC ||
               m.format == ExportFormats.AviHardwareHD)
           {
               if (instream.codecshort == "AAC")
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
                m.format == ExportFormats.Mp4PS3 && instream.codecshort != "AC3" ||
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
           string ext = Path.GetExtension(m.infilepath).ToLower();

           if (Formats.GetDefaults(m.format).IsEditable)
           {
               //Точно выкл.
               if (Formats.GetDefaults(m.format).DirectRemuxing_IsEditable)
               {
                   if (!Formats.GetSettings(m.format, "DirectRemuxing", Formats.GetDefaults(m.format).DirectRemuxing))
                       return false;
               }
               else if (!Formats.GetDefaults(m.format).DirectRemuxing)
                   return false;

               Muxers muxer = GetMuxer(m);
               if (muxer == Muxers.mkvmerge)
               {
                   if (m.outvcodec == "Copy" && m.outaudiostreams.Count > 0 && ((AudioStream)m.outaudiostreams[m.outaudiostream]).codec == "Copy")
                       return false;
                   if (ext == ".mkv" || ext == ".webm" || ext == ".mpg" || ext == ".vob" || ext == ".mp4" || ext == ".mov" || ext == ".avi" ||
                       ext == ".rm" || ext == ".ogm" || ext == ".ts" || ext == ".m2ts" || ext == ".m2t" || ext == ".mts")
                       return true;
                   else
                       return false;
               }
               else if (muxer == Muxers.tsmuxer)
               {
                   if (ext == ".mkv" || ext == ".mpg" || ext == ".vob" || ext == ".mp4" || ext == ".mov" || ext == ".ts" ||
                       ext == ".m2ts" || ext == ".m2t" || ext == ".mts" || ext == ".evo")
                       return true;
                   else
                       return false;
               }
               else if (muxer == Muxers.ffmpeg)
               {
                   if (m.format == ExportFormats.Flv)
                   {
                       if (ext == ".flv")
                       {
                           if (m.outaudiostreams.Count == 0)
                               return true;
                           else
                           {
                               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                               if (outstream.codec == "Copy" && !File.Exists(outstream.audiopath))
                                   return true;
                               else
                                   return false;
                           }
                       }
                       else
                           return false;
                   }
                   else if (m.inaudiostreams.Count > 1 && m.outaudiostreams.Count > 0 && ((AudioStream)m.outaudiostreams[m.outaudiostream]).codec == "Copy")
                       return false;
                   else
                       return true;
               }
               else
                   return false;
           }
           else if (m.format == ExportFormats.BluRay)
           {
               if (Settings.GetFormatPreset(m.format, "direct_remux") == "False")
                   return false;
               if (ext == ".mkv" || ext == ".mpg" || ext == ".vob" || ext == ".mp4" || ext == ".mov" || ext == ".ts" ||
                   ext == ".m2ts" || ext == ".m2t" || ext == ".mts" || ext == ".evo")
                   return true;
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
           else if (ext.Contains("vp5") || ext.Contains("vp6") || ext == "h263") ext = "avi";
           else if (ext.Contains("vp8")) ext = "ivf"; //vp8
           else if (m.invcodecshort == "MPEG1") ext = "m1v";
           else if (m.invcodecshort == "MPEG2") ext = "m2v";
           else if (m.invcodecshort == "h264") ext = "h264";
           else if (m.invcodecshort == "MPEG4") ext = "avi";
           else if (ext.Contains("sorenson")) ext = "avi";
           else if (ext.Contains("vc1")) ext = "avi";
           else if (ext.Contains("wmv")) ext = "wmv";
           else if (ext.Contains("dv")) ext = "avi";
           else if (ext.Contains("m") && ext.Contains("jp")) ext = "avi"; //M-JPEG
           else if (ext == "huffman" || ext == "hfyu" || ext == "ffvh") ext = "avi";
           else if (ext == "ffv1") ext = "avi";
           else if (fext == ".avi") ext = "avi";

           Demuxers dem = GetDemuxer(m);
           if (dem == Demuxers.mp4box && ext == "avi") ext = "m4v";
           //ffmpeg извлекает кривой raw-h264 (из mkv, mp4, mov, flv - точно, но из avi, mpg, ts, m2ts вроде нормальный)
           else if (dem == Demuxers.ffmpeg && ext == "h264" && fext != ".avi" && fext != ".mpg" && fext != ".ts" && fext != ".m2ts") ext = "mp4";

           //Muxers mux = GetMuxer(m);
           //if (mux == Muxers.ffmpeg && ext == "h263") ext = "flv";

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
           else if (format == ExportFormats.Flv ||
               format == ExportFormats.Mp4SonyEricssonK800 ||
               format == ExportFormats.Mp4SonyEricssonK610)
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
           else if (format == ExportFormats.Mp4Archos5G ||
               format == ExportFormats.Mp4BlackBerry8100 ||
               format == ExportFormats.Mp4BlackBerry8800 ||
               format == ExportFormats.Mp4MotorolaK1 ||
               format == ExportFormats.Mp4Prada ||
               format == ExportFormats.PmpAvc)
               return "AAC-LC ABR 128k";
           else
               return "AAC-LC VBR 0.45";
       }

       public static Muxers GetMuxer(Massive m)
       {
           if (m.format == ExportFormats.Audio)
               return Muxers.Disabled;
           else if (m.format == Format.ExportFormats.PmpAvc)
               return Muxers.pmpavc;
           else if (m.format == ExportFormats.Mpeg1PS)
               return Muxers.ffmpeg;
           else if (m.format == ExportFormats.BluRay)
               return Muxers.tsmuxer;
           else if (m.format == ExportFormats.DpgNintendoDS)
               return Muxers.dpgmuxer;
           else if (m.format == ExportFormats.Mp4iPod50G)
               return Muxers.mp4box;
           else if (m.format == Format.ExportFormats.Mp4iPod55G)
           {
               if (m.outvcodec == "x264") return Muxers.ffmpeg; //ipod atom
               else return Muxers.mp4box;
           }
           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               //Кодирование сразу в контейнер
               bool direct_encoding = false;
               if (Formats.GetDefaults(m.format).DirectEncoding_IsEditable)
               {
                   direct_encoding = Formats.GetSettings(m.format, "DirectEncoding", Formats.GetDefaults(m.format).DirectEncoding);
               }
               else
                   direct_encoding = Formats.GetDefaults(m.format).DirectEncoding;

               if (direct_encoding)
               {
                   string ext = GetValidExtension(m);
                   if (m.outaudiostreams.Count == 0)
                   {
                       //Звука нет и видеокодер может кодировать сразу в нужный контейнер
                       if (m.outvcodec == "x264" && (ext == ".mkv" || ext == ".mp4" || ext == ".flv" || ext == ".264" || ext == ".h264") ||
                           m.outvcodec == "XviD" && (ext == ".avi") ||
                           m.outvcodec == "HUFF" ||
                           m.outvcodec == "FFV1" ||
                           m.outvcodec == "MJPEG" ||
                           m.outvcodec == "MPEG4" ||
                           m.outvcodec == "MPEG2" ||
                           m.outvcodec == "MPEG1" ||
                           m.outvcodec == "FLV1" ||
                           m.outvcodec == "DV")
                           return Muxers.Disabled;
                   }
                   else
                   {
                       //Видео и звук кодируются через FFmpeg
                       if (m.outvcodec == "HUFF" ||
                           m.outvcodec == "FFV1" ||
                           m.outvcodec == "MJPEG" ||
                           m.outvcodec == "MPEG4" ||
                           m.outvcodec == "MPEG2" ||
                           m.outvcodec == "MPEG1" ||
                           m.outvcodec == "FLV1" ||
                           m.outvcodec == "DV")
                       {
                           AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                           if (outstream.codec == "PCM" ||
                               outstream.codec == "LPCM" ||
                               outstream.codec == "FLAC" ||
                               outstream.codec == "MP2")
                               return Muxers.Disabled;
                       }
                   }
               }

               if (Formats.GetDefaults(m.format).Muxers.Length > 1)
               {
                   string muxer = Formats.GetSettings(m.format, "Muxer", Formats.GetDefaults(m.format).Muxer).ToLower();
                   foreach (string muxers in Formats.GetDefaults(m.format).Muxers)
                   {
                       if (muxers == muxer)
                           return (Muxers)Enum.Parse(typeof(Muxers), muxer, true);
                   }
               }

               return (Muxers)Enum.Parse(typeof(Muxers), Formats.GetDefaults(m.format).Muxer, true);
           }
           else
               return Muxers.mp4box;
       }

      public static Demuxers GetDemuxer(Massive m)
      {
          //Muxers mux = GetMuxer(m);
          string ext = Path.GetExtension(m.infilepath).ToLower();

          if (ext == ".mkv" || ext == ".webm")
          {
              if (m.invcodecshort == "VC1" || m.invcodecshort.Contains("WMV"))
                  return Demuxers.ffmpeg;
              else
                  return Demuxers.mkvextract;
          }
          else if (ext == ".dpg")
              return Demuxers.dpgmuxer;
          else if (ext == ".pmp")
              return Demuxers.pmpdemuxer;
          else if (ext == ".mp4" ||
                   ext == ".m4v" ||
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
           //методы для форматов с фиксированным аспектом
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
               m.aspectfix = AspectResolution.AspectFixes.Black;

               if (Formats.GetDefaults(m.format).IsEditable)
               {
                   //Анаморф
                   bool anamorphic = false;
                   if (Formats.GetDefaults(m.format).Anamorphic_IsEditable)
                       anamorphic = Formats.GetSettings(m.format, "Anamorphic", Formats.GetDefaults(m.format).Anamorphic);
                   else
                       anamorphic = Formats.GetDefaults(m.format).Anamorphic;

                   if (!anamorphic) m.sar = null;
                   else m = Calculate.CalculateSAR(m);

                   //Метод изменения аспекта
                   if (Formats.GetDefaults(m.format).LockedAR_Methods.Length > 1)
                   {
                       string method = Formats.GetSettings(m.format, "LockedAR_Method", Formats.GetDefaults(m.format).LockedAR_Method);
                       foreach (string methods in Formats.GetDefaults(m.format).LockedAR_Methods)
                       {
                           if (methods == method)
                           {
                               m.aspectfix = (AspectResolution.AspectFixes)Enum.Parse(typeof(AspectResolution.AspectFixes), method);
                               if (m.aspectfix == AspectResolution.AspectFixes.SAR && !anamorphic)
                               {
                                   //Но анаморф не был разрешен..
                                   m.aspectfix = AspectResolution.AspectFixes.Disabled;
                                   m.outaspect = (double)m.outresw / (double)m.outresh;
                               }
                           }
                       }
                   }
                   else
                       m.aspectfix = (AspectResolution.AspectFixes)Enum.Parse(typeof(AspectResolution.AspectFixes), Formats.GetDefaults(m.format).LockedAR_Method);
               }
               else
                   m = Calculate.CalculateSAR(m);
           }
           else
           {
               //методы для остальных форматов
               if (m.aspectfix == AspectResolution.AspectFixes.SAR)
               {
                   m.outaspect = m.inaspect;
                   m = Calculate.CalculateSAR(m);
               }
               else
               {
                   m.outaspect = (double)m.outresw / (double)m.outresh;
                   m.aspectfix = AspectResolution.AspectFixes.Disabled;
                   m.sar = null;
               }
           }
           return m;
       }

       public static bool IsLockedOutAspect(Massive m)
       {
           if (m.format == ExportFormats.BluRay)
               return true;
           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               if (Formats.GetDefaults(m.format).LockedAR_Methods.Length > 1)
               {
                   string method = Formats.GetSettings(m.format, "LockedAR_Method", Formats.GetDefaults(m.format).LockedAR_Method);
                   foreach (string methods in Formats.GetDefaults(m.format).LockedAR_Methods)
                   {
                       if (methods == method)
                           return (method != "Disabled");
                   }
               }

               return (Formats.GetDefaults(m.format).LockedAR_Method != "Disabled");
           }

           return false;
       }

       public static string[] GetValidOutAspects(Massive m)
       {
           if (m.format == ExportFormats.BluRay)
               return new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };

           else if (m.format == ExportFormats.PmpAvc)
               return new string[] { "1.3333 (4:3)", "1.7647 (16:9)", "1.8500", "2.3529" };

           else if (Formats.GetDefaults(m.format).IsEditable)
           {
               if (Formats.GetDefaults(m.format).Aspects_IsEditable)
               {
                   return Formats.GetSettings(m.format, "Aspects", Formats.GetDefaults(m.format).Aspects);
               }

               return Formats.GetDefaults(m.format).Aspects;
           }

           return new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" };
       }

       public static bool GetMultiplexing(ExportFormats format)
       {
           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).DontMuxStreams_IsEditable)
                   return Formats.GetSettings(format, "DontMuxStreams", Formats.GetDefaults(format).DontMuxStreams);
               else
                   return Formats.GetDefaults(format).DontMuxStreams;
           }
           else if (format == ExportFormats.BluRay)
               return Convert.ToBoolean(Settings.GetFormatPreset(Format.ExportFormats.BluRay, "dont_mux_streams"));

           return false; //DontMuxStreams = false
       }

       public static string GetSplitting(ExportFormats format)
       {
           if (Formats.GetDefaults(format).IsEditable)
           {
               if (Formats.GetDefaults(format).Splitting != "None")
                   return Formats.GetSettings(format, "Splitting", Formats.GetDefaults(format).Splitting);
               else
                   return "Disabled";
           }
           else if (format == ExportFormats.BluRay)
               return Settings.GetFormatPreset(format, "split");

           return "Disabled";
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
               m.format == ExportFormats.AviHardwareHD)
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
