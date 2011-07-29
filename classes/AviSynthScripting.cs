using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
   public static class AviSynthScripting
    {
       public enum Decoders
       {
           Import = 1,        //AviSynth
           BlankClip,         //AviSynth
           AVISource,         //AviSynth
           AVIFileSource,     //AviSynth
           OpenDMLSource,     //AviSynth
           MPEG2Source,       //DGDecode.dll
           AVCSource,         //DGAVCDecode.dll
           DGMultiSource,     //DGDecodeNV.dll
           DirectShowSource,  //DirectShowSource.dll
           DirectShowSource2, //avss.dll+VideoFunctions.avs
           FFmpegSource2,     //ffms2.dll+FFMS2.avsi
           FFAudioSource,     //ffms2.dll
           WAVSource,         //AviSynth
           RaWavSource,       //NicAudio.dll
           NicAC3Source,      //NicAudio.dll
           NicDTSSource,      //NicAudio.dll
           NicMPG123Source,   //NicAudio.dll
           bassAudioSource,   //bassAudio.dll
           RawSource,         //RawSource.dll
           QTInput            //QTSource.dll + QuickTime
       }

       public enum Resizers
       {
           BicubicResize = 1,
           BicubicResizePlus,
           BilinearResize,
           LanczosResize,
           Lanczos4Resize,
           BlackmanResize,
           SplineResize,     //SplineResize.dll
           Spline16Resize,
           Spline36Resize,
           Spline64Resize,
           Spline100Resize,  //SplineResize.dll
           Spline144Resize,  //SplineResize.dll
           GaussResize,
           PointResize
       }

       public enum ScriptMode
       {
           Info = 1,
           Autocrop,
           AudioOnly,
           Interlace,
           FastPreview,
           Normalize
       }

       public enum FramerateModifers { AssumeFPS = 1, ChangeFPS, ConvertFPS, ConvertMFlowFPS }
       public enum SamplerateModifers { SSRC = 1, ResampleAudio, AssumeSampleRate }

       public static Massive CreateAutoAviSynthScript(Massive m)
       {
           string old_filtering = null;
           string startup_path = Calculate.StartupPath;

           //Ищем и сохраняем старую фильтрацию
           if (m.script != null && !m.filtering_changed)
           {
               bool ok = false;
               string temp_filtering = null;
               string[] strings = m.script.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
               foreach (string line in strings)
               {
                   if (!ok && line.StartsWith("###[FILTERING]###"))
                   {
                       ok = true;
                       continue;
                   }
                   else if (ok && line.StartsWith("###[FILTERING]###"))
                   {
                       old_filtering = temp_filtering;
                       break;
                   }

                   if (ok) temp_filtering += line + Environment.NewLine;
               }
           }

           //определяем расширения
           string ext = Path.GetExtension(m.infilepath).ToLower();

           //определяем аудио потоки
           AudioStream instream = (m.inaudiostreams.Count > 0) ? (AudioStream)m.inaudiostreams[m.inaudiostream] : new AudioStream();

           //начинаем писать скрипт
           m.script = "";

           //загружаем доп функции
           m.script += "import(\"" + startup_path + "\\dlls\\AviSynth\\functions\\AudioFunctions.avs\")" + Environment.NewLine;
           m.script += "import(\"" + startup_path + "\\dlls\\AviSynth\\functions\\VideoFunctions.avs\")" + Environment.NewLine;

           //загружаем необходимые плагины импорта
           if (m.vdecoder == Decoders.MPEG2Source)
               m.script += "loadplugin(\"" + startup_path + "\\apps\\DGMPGDec\\DGDecode.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.AVCSource)
               m.script += "loadplugin(\"" + startup_path + "\\apps\\DGAVCDec\\DGAVCDecode.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DGMultiSource)
               m.script += "loadplugin(\"" + m.dgdecnv_path + "DGDecodeNV.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DirectShowSource2)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\avss.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.RawSource)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\rawsource.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.QTInput)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\QTSource.dll\")" + Environment.NewLine;

           if (m.vdecoder == Decoders.FFmpegSource2 || instream.decoder == Decoders.FFAudioSource)
           {
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\FFMS2.dll\")" + Environment.NewLine;
               m.script += "import(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\FFMS2.avsi\")" + Environment.NewLine;
           }

           if (instream.decoder == Decoders.NicAC3Source || instream.decoder == Decoders.NicMPG123Source ||
               instream.decoder == Decoders.NicDTSSource || instream.decoder == Decoders.RaWavSource)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\NicAudio.dll\")" + Environment.NewLine;
           else if (instream.decoder == Decoders.bassAudioSource)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\bass\\bassAudio.dll\")" + Environment.NewLine;

           if (m.deinterlace == DeinterlaceType.TIVTC || m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI ||
               m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI || m.deinterlace == DeinterlaceType.TDecimate ||
               m.deinterlace == DeinterlaceType.TDecimate_23 || m.deinterlace == DeinterlaceType.TDecimate_24 ||
               m.deinterlace == DeinterlaceType.TDecimate_25 || m.deinterlace == DeinterlaceType.TFM ||
               m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TIVTC.dll\")" + Environment.NewLine;
           if (m.deinterlace == DeinterlaceType.TomsMoComp)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TomsMoComp.dll\")" + Environment.NewLine;
           else if (m.deinterlace == DeinterlaceType.TDeint)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TDeint.dll\")" + Environment.NewLine;
           else if (m.deinterlace == DeinterlaceType.TDeintEDI)
           {
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TDeint.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\EEDI2.dll\")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI)
           {
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TDeint.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\nnedi3.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TMM.dll\")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.YadifModEDI || m.deinterlace == DeinterlaceType.YadifModEDI2 ||
               m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI)
           {
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\yadifmod.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\nnedi3.dll\")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.LeakKernelDeint)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\LeakKernelDeint.dll\")" + Environment.NewLine;
           else if (m.deinterlace == DeinterlaceType.Yadif)
               m.script += "loadcplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\yadif.dll\")" + Environment.NewLine;
           else if (m.deinterlace == DeinterlaceType.SmoothDeinterlace)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\SmoothDeinterlacer.dll\")" + Environment.NewLine;
           else if (m.deinterlace == DeinterlaceType.NNEDI)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\nnedi3.dll\")" + Environment.NewLine;
           else if (m.deinterlace == DeinterlaceType.MCBob)
           {
               m.script += "import(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\MCBob_mod.avs\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\EEDI2.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\mt_masktools-25.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\mvtools.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\Repair.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\RemoveGrain.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\nnedi3.dll\")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.QTGMC)
           {
               m.script += "import(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\QTGMC.avs\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\mvtools2.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\RemoveGrainSSE2.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\RepairSSE2.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\mt_masktools-25.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\fft3dfilter.dll\")" + Environment.NewLine;
               m.script += "#loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\VerticalCleaner.dll\")" + Environment.NewLine;
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\nnedi3.dll\")" + Environment.NewLine;
               m.script += "#loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\EEDI3.dll\")" + Environment.NewLine;
               m.script += "#loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\EEDI2.dll\")" + Environment.NewLine;
               m.script += "loadcplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\yadif.dll\")" + Environment.NewLine;
               m.script += "#loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\TDeint.dll\")" + Environment.NewLine;
               m.script += "#loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\AddGrainC.dll\")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.FieldDeinterlace)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\Decomb.dll\")" + Environment.NewLine;

           if (m.subtitlepath != null)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\VSFilter.dll\")" + Environment.NewLine;

           if (m.iscolormatrix)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\ColorMatrix.dll\")" + Environment.NewLine;

           if (instream.channelconverter != AudioOptions.ChannelConverters.KeepOriginalChannels)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\soxfilter.dll\")" + Environment.NewLine;

           if (m.frameratemodifer == FramerateModifers.ConvertMFlowFPS)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\mvtools2.dll\")" + Environment.NewLine;

           if (m.resizefilter == Resizers.SplineResize || m.resizefilter == Resizers.Spline100Resize || m.resizefilter == Resizers.Spline144Resize)
               m.script += "loadplugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\SplineResize.dll\")" + Environment.NewLine;

           m.script += Environment.NewLine;

           //прописываем импорт видео

           //принудительная установка fps
           string fps = "";
           if ((m.vdecoder == Decoders.DirectShowSource || m.vdecoder == Decoders.DirectShowSource2) && m.inframerate != "")
               fps = ", fps=" + m.inframerate;

           //принудительная конвертация частоты
           string convertfps = "";
           if (m.vdecoder == Decoders.DirectShowSource && m.isconvertfps && Settings.DSS_ConvertFPS)
               convertfps = ", convertfps=true";

           //выбор аудио трека
           string audio = "";
           if (m.vdecoder == Decoders.DirectShowSource && (instream.audiopath != null || m.outaudiostreams.Count == 0 || !Settings.DSS_Enable_Audio || !Settings.EnableAudio))
               audio = ", audio=false";
           else if (m.vdecoder == Decoders.AVISource && (instream.audiopath != null || m.outaudiostreams.Count == 0 || !Settings.EnableAudio))
               audio = ", audio=false";
           else if (m.vdecoder == Decoders.FFmpegSource2 && m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0 && instream.audiopath == null && Settings.FFMS_Enable_Audio && Settings.EnableAudio)
               audio = ", atrack=" + (instream.ffid) + ", adjustdelay=-3";

           //ипортируем видео
           string invideostring = "";

           if (m.vdecoder == Decoders.BlankClip) //пустой клип (черный экран)
           {
               invideostring = m.vdecoder.ToString() + "(length=" + m.inframes + ", width=128, height=96, fps=" + m.inframerate + ", color=$000000)";
           }
           else if (ext == ".d2v") //d2v и MPEG2Source
           {
               int n = 0;
               foreach (string file in m.infileslist)
               {
                   n++;
                   invideostring += m.vdecoder.ToString() + "(\"" + file + "\", cpu=0, info=3)";
                   if (n < m.infileslist.Length) invideostring += "++";
               }
           }
           else if (ext != ".d2v" && m.vdecoder == Decoders.MPEG2Source) //мпег2 и MPEG2Source
           {
               invideostring = m.vdecoder.ToString() + "(\"" + m.indexfile + "\", cpu=0, info=3)";
           }
           else if (ext == ".dga" || ext == ".dgi")
           {
               int n = 0;
               foreach (string file in m.infileslist)
               {
                   n++;
                   invideostring += m.vdecoder.ToString() + "(\"" + file + "\")";
                   if (n < m.infileslist.Length) invideostring += "++";
               }
           }
           else //другое
           {
               int n = 0;
               foreach (string file in m.infileslist)
               {
                   //Изменения под FFmpegSource2
                   string cache_path = "";
                   string assume_fps = "";
                   if (m.vdecoder == Decoders.FFmpegSource2)
                   {
                       //Корректировка кривого fps от FFmpegSource2 - не самое лучшее решение, и обязательно приведет к новым проблемам..
                       //Так-же как и принудительная установка частоты для DirectShowSource (1 и 2) - не самое лучшее решение, и тоже приводит к другим проблемам..
                       //Лучшее решение - писать скрипт вручную! Внимательно анализируя исходник, и разбираясь, отчего-же декодер выдает не тот фпс!
                       //С другой стороны, при муксинге все-равно муксер получит фпс, с которым должен будет муксить - получится тот-же самый AssumeFPS, только после кодирования.
                       //Ну а если исходная частота, и частота, с которой декодирует декодер, совпадают - то AssumeFPS вообще ни на что не повлияет..
                       assume_fps = (!string.IsNullOrEmpty(m.inframerate) && Settings.FFMS_AssumeFPS) ? ".AssumeFPS(" + m.inframerate + ")" : "";
                       cache_path = ", rffmode=0" + ((Settings.FFMS_Threads > 0) ? ", threads=" + Settings.FFMS_Threads : "") +
                           ((m.ffms_indexintemp) ? ", cachefile=\"" + Settings.TempPath + "\\" + Path.GetFileName(file) + ".ffindex\"" : "");
                   }
                   n++;
                   invideostring += m.vdecoder.ToString() + "(\"" + file + "\"" + audio + fps + convertfps + cache_path + ")" + assume_fps;
                   if (n < m.infileslist.Length) invideostring += "++";
               }
           }

           //теперь звук!
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0 && instream.audiopath != null)
           {
               //прописываем импорт видео
               m.script += "video = " + invideostring + Environment.NewLine;
               
               //пришиваем звук
               string ffindex = "";
               string inaudiostring = "";
               string no_video = (instream.decoder == Decoders.DirectShowSource) ? ", video=false" : "";
               string drc = (instream.decoder == Decoders.NicAC3Source && Settings.NicAC3_DRC ||
                   instream.decoder == Decoders.NicDTSSource && Settings.NicDTS_DRC) ? ", drc=1" : "";
               if (instream.audiofiles != null && instream.audiofiles.Length > 0)
               {
                   int n = 0;
                   foreach (string file in instream.audiofiles)
                   {
                       n++;
                       ffindex += "FFIndex(" + "\"" + file + "\")\r\n";
                       inaudiostring += instream.decoder.ToString() + "(\"" + file + "\"" + no_video + drc + ")";
                       if (n < instream.audiofiles.Length) inaudiostring += "++";
                   }
               }
               else
               {
                   ffindex += "FFIndex(" + "\"" + instream.audiopath + "\")\r\n";
                   inaudiostring += instream.decoder.ToString() + "(\"" + instream.audiopath + "\"" + no_video + drc + ")";
               }
               if (instream.decoder == Decoders.FFAudioSource) m.script += ffindex;
               m.script += "audio = " + inaudiostring + Environment.NewLine;

               //объединение
               m.script += "AudioDub(video, audio)" + Environment.NewLine;
           }
           else
           {
               //прописываем импорт всего клипа
               m.script += invideostring + Environment.NewLine;
           }

           m.script += Environment.NewLine;

           //Определяем необходимость смены частоты кадров (AssumeFPS, ChangeFPS, ConvertFPS, ConvertMFlowFPS)
           bool ApplyFramerateModifier = false, IsAssumeFramerateConvertion = false;
           if (m.deinterlace == DeinterlaceType.TIVTC || m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI ||
               m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI || m.deinterlace == DeinterlaceType.TDecimate ||
               m.deinterlace == DeinterlaceType.TDecimate_23)
           {
               if (m.outframerate != "23.976")
               {
                   ApplyFramerateModifier = true;
                   IsAssumeFramerateConvertion = (m.frameratemodifer == FramerateModifers.AssumeFPS);
               }
           }
           else if (m.deinterlace == DeinterlaceType.TDecimate_24)
           {
               if (m.outframerate != "24.000")
               {
                   ApplyFramerateModifier = true;
                   IsAssumeFramerateConvertion = (m.frameratemodifer == FramerateModifers.AssumeFPS);
               }
           }
           else if (m.deinterlace == DeinterlaceType.TDecimate_25)
           {
               if (m.outframerate != "25.000")
               {
                   ApplyFramerateModifier = true;
                   IsAssumeFramerateConvertion = (m.frameratemodifer == FramerateModifers.AssumeFPS);
               }
           }
           else if (m.deinterlace == DeinterlaceType.MCBob || m.deinterlace == DeinterlaceType.NNEDI ||
           m.deinterlace == DeinterlaceType.YadifModEDI2 || m.deinterlace == DeinterlaceType.QTGMC ||
           m.deinterlace == DeinterlaceType.SmoothDeinterlace)
           {
               double outframerate = Calculate.ConvertStringToDouble(m.outframerate);
               if (outframerate != Calculate.ConvertStringToDouble(m.inframerate) * 2.0)
               {
                   ApplyFramerateModifier = true;
                   IsAssumeFramerateConvertion = (m.frameratemodifer == FramerateModifers.AssumeFPS);
               }
           }
           else if (m.inframerate != m.outframerate)
           {
               ApplyFramerateModifier = true;
               IsAssumeFramerateConvertion = (m.frameratemodifer == FramerateModifers.AssumeFPS);
           }

           //блок обработки звука
           if (m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
           {
               AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

               //задержка
               if (outstream.delay != 0)
                   m.script += "DelayAudio(" + Calculate.ConvertDoubleToPointString(Convert.ToDouble(outstream.delay) / 1000) + ")" + Environment.NewLine;

               //меняем канальность
               if (instream.channelconverter != AudioOptions.ChannelConverters.KeepOriginalChannels)
                   m.script += instream.channelconverter.ToString() + "()" + Environment.NewLine;

               //меняем битность
               if (instream.bits != outstream.bits)
                   m.script += "ConvertAudioTo16bit()" + Environment.NewLine;

               //прописываем смену частоты
               if (instream.samplerate != outstream.samplerate && !IsAssumeFramerateConvertion && outstream.samplerate != null)
                   m.script += m.sampleratemodifer + "(" + outstream.samplerate + ")" + Environment.NewLine;

               //нормализация звука
               if (instream.gain != "0.0")
                   m.script += "AmplifydB(" + instream.gain + ")" + Environment.NewLine;
           }

           ///////////////////////
           //блок работы с видео//
           ///////////////////////

           //прописываем цветовое пространство
           m.script += "ConvertToYV12(" + ((m.interlace != SourceType.UNKNOWN && m.interlace != SourceType.PROGRESSIVE &&
               m.interlace != SourceType.DECIMATING) ? "interlaced = true" : "") + ")" + Environment.NewLine;

           //Mod protection
           string mod2 = null;
           //Для высоты можно и mod8 (когда в настройках стоит mod16)
           int modw = Format.GetValidModW(m), modh = Math.Min(Format.GetValidModH(m), 8);
           if (m.inresw % modw != 0 || m.inresh % modh != 0)
           {
               //Для интерлейсных исходников в этом месте будет проблема!
               m.script += "#Mod" + modw + "xMod" + modh + " protection" + Environment.NewLine;
               if (m.resizefilter == Resizers.BicubicResizePlus)
                   mod2 = "BicubicResize(" + Calculate.GetValid(m.inresw, modw) + ", " + Calculate.GetValid(m.inresh, modh) + ", 0, 0.75)";
               else
                   mod2 = m.resizefilter + "(" + Calculate.GetValid(m.inresw, modw) + ", " + Calculate.GetValid(m.inresh, modh) + ")";
               m.script += mod2 + Environment.NewLine;
           }

           //colormatrix
           if (m.iscolormatrix)
           {
               string colormatrix = "ColorMatrix(";
               if (m.vdecoder == Decoders.MPEG2Source || m.vdecoder == Decoders.DGMultiSource)
               {
                   if (m.interlace == SourceType.FILM ||
                       m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                       m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                       m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                       m.interlace == SourceType.INTERLACED)
                       colormatrix += "hints=true, interlaced=true)";
                   else
                       colormatrix += "hints=true)";
               }
               else
               {
                   if (m.interlace == SourceType.FILM ||
                       m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                       m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                       m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                       m.interlace == SourceType.INTERLACED)
                       colormatrix += "interlaced=true)";
                   else
                       colormatrix += ")";
               }

               m.script += colormatrix + Environment.NewLine;
           }

           if (m.saturation != 1.0)
               m.script += "Tweak(sat=" + Calculate.ConvertDoubleToPointString(m.saturation, 1) + ")" + Environment.NewLine;
           if (m.brightness != 0)
               m.script += "Tweak(bright=" + m.brightness + ")" + Environment.NewLine;
           if (m.contrast != 1.00)
               m.script += "Tweak(cont=" + Calculate.ConvertDoubleToPointString(m.contrast, 2) + ")" + Environment.NewLine;
           if (m.hue != 0)
               m.script += "Tweak(hue=" + m.hue + ")" + Environment.NewLine;         

           //разделяем при необходимости на поля
           if (m.interlace != SourceType.UNKNOWN && m.interlace != SourceType.PROGRESSIVE && m.interlace != SourceType.DECIMATING)
           {
               if (m.deinterlace == DeinterlaceType.Disabled)
               {
                   m.script += Environment.NewLine;
                   m.script += "SeparateFields()" + Environment.NewLine;
               }
           }

           //Деинтерлейсинг
           int order = (m.fieldOrder == FieldOrder.TFF) ? 1 : (m.fieldOrder == FieldOrder.BFF) ? 0 : -1;
           string mark = (Settings.IsCombed_Mark ? "" : "#") + ".Subtitle(\"deinterlaced frame\", align=5)";
           if (m.deinterlace == DeinterlaceType.TomsMoComp)
           {
               string deinterlacer = "TomsMoComp(" + order + ", 5, 1)";
               m.script += ((m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) ? "deint = " + deinterlacer + mark + Environment.NewLine +
                   "ScriptClip(last, \"IsCombedTIVTC(last, cthresh=" + Settings.IsCombed_CThresh + ", MI=" + Settings.IsCombed_MI + ") ? deint : last\")" : deinterlacer) + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.Yadif)
           {
               string deinterlacer = "Yadif(order=" + order + ")";
               m.script += ((m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) ? "deint = " + deinterlacer + mark + Environment.NewLine +
                   "ScriptClip(last, \"IsCombedTIVTC(last, cthresh=" + Settings.IsCombed_CThresh + ", MI=" + Settings.IsCombed_MI + ") ? deint : last\")" : deinterlacer) + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.YadifModEDI)
           {
               string deinterlacer = "YadifMod(order=" + order + ", edeint=nnedi3(field=" + order + "))";
               m.script += ((m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) ? "deint = " + deinterlacer + mark + Environment.NewLine +
                   "ScriptClip(last, \"IsCombedTIVTC(last, cthresh=" + Settings.IsCombed_CThresh + ", MI=" + Settings.IsCombed_MI + ") ? deint : last\")" : deinterlacer) + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.LeakKernelDeint)
           {
               string deinterlacer = "LeakKernelDeint(order=" + ((order < 0) ? "((GetParity) ? 1 : 0)" : order.ToString()) + ", sharp=true)";
               m.script += ((m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) ? "deint = " + deinterlacer + mark + Environment.NewLine +
               "ScriptClip(last, \"IsCombedTIVTC(last, cthresh=" + Settings.IsCombed_CThresh + ", MI=" + Settings.IsCombed_MI + ") ? deint : last\")" : deinterlacer) + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TFM)
           {
               m.script += "TFM(order=" + order + ", mode=1, pp=6, slow=1, cthresh=6, MI=35)" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.FieldDeinterlace)
           {
               m.script += ((order == 1) ? "AssumeTFF()." : (order == 0) ? "AssumeBFF()." : "") + "FieldDeinterlace(" + ((m.interlace ==
                   SourceType.HYBRID_PROGRESSIVE_INTERLACED) ? "full=false, threshold=20" : "") + ")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TDeint)
           {
               m.script += "TDeint(order=" + order + ((m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) ?
                   ", full=false, cthresh=" + Settings.IsCombed_CThresh + ", MI=" + Settings.IsCombed_MI : "") + ")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TDeintEDI)
           {
               string assume = ((order == 1) ? "AssumeTFF()." : (order == 0) ? "AssumeBFF()." : "");
               m.script += "edeintted = last." + assume + "SeparateFields().SelectEven().EEDI2(field=-1)" + Environment.NewLine;
               m.script += "TDeint(order=" + order + ", edeint=edeintted" + ((m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED) ?
                   ", full=false, cthresh=" + Settings.IsCombed_CThresh + ", MI=" + Settings.IsCombed_MI : "") + ")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TIVTC)
           {
               m.script += "TFM(order=" + order + ").TDecimate(hybrid=1)" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TIVTC_TDeintEDI)
           {
               m.script += "interp = nnedi3(field=" + order + ", qual=2)" + Environment.NewLine;
               m.script += "tmmask = TMM(order=" + order + ", field=" + order + ")" + Environment.NewLine;
               m.script += "deint = TDeint(order=" + order + ", field=" + order + ", edeint=interp, slow=2, emask=tmmask)" + Environment.NewLine;
               m.script += "TFM(order=" + order + ", mode=3, clip2=deint, slow=2).TDecimate(hybrid=1)" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TIVTC_YadifModEDI)
           {
               m.script += "interp = nnedi3(field=" + order + ", qual=2)" + Environment.NewLine;
               m.script += "deint = YadifMod(order=" + order + ", edeint=interp)" + Environment.NewLine;
               m.script += "TFM(order=" + order + ", mode=3, clip2=deint, slow=2).TDecimate(hybrid=1)" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TDecimate)
           {
               //TDecimate(Mode=7,Rate=23.976) dupthresh vidthresh
               //TDecimate(Mode=2, Rate=24)    maxndl m2PA
               //TDecimate(mode=0,cycleR=9,cycle=15) 59.940->23.976
               //TDecimate(Mode=1,CycleR=166,Cycle=1001) 29.970->25

               if (m.inframerate != "29.970") m.script += "ChangeFPS(29.97)" + Environment.NewLine;
               m.script += "TDecimate(cycleR=1, cycle=5) #remove 1 frame from every 5 frames" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TDecimate_23)
           {
               m.script += "TDecimate(mode=7, rate=23.976) #or try \"mode=2\"" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TDecimate_24)
           {
               m.script += "TDecimate(mode=7, rate=24.000) #or try \"mode=2\"" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.TDecimate_25)
           {
               m.script += "TDecimate(mode=7, rate=25.000) #or try \"mode=2\"" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.SmoothDeinterlace)
           {
               m.script += "SmoothDeinterlace(" + ((order == 1) ? "tff=true, " : (order == 0) ? "tff=false, " : "") + "doublerate=true)" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.MCBob)
           {
               m.script += ((order == 1) ? "AssumeTFF()\r\n" : (order == 0) ? "AssumeBFF()\r\n" : "");
               m.script += "MCBobmod()" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.NNEDI)
           {
               m.script += "nnedi3(field=" + ((order == 1) ? "3" : (order == 0) ? "2" : "-2") + ")" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.YadifModEDI2)
           {
               int field = ((order == 1) ? 3 : (order == 0) ? 2 : -2);
               m.script += "YadifMod(order=" + order + ", mode=1, edeint=nnedi3(field=" + field + "))" + Environment.NewLine;
           }
           else if (m.deinterlace == DeinterlaceType.QTGMC)
           {
               m.script += "QTGMC(Preset=\"" + Settings.QTGMC_Preset + "\", Sharpness=" + Calculate.ConvertDoubleToPointString(Settings.QTGMC_Sharpness, 1) + ")\r\n";
           }

           //Фильтрация до ресайза
           if (!Settings.ResizeFirst)
           {
               m.script += "\r\n###[FILTERING]###\r\n";
               if (old_filtering != null) m.script += old_filtering;
               else if (m.filtering != "Disabled") m.script += LoadScript(startup_path + "\\presets\\filtering\\" + m.filtering + ".avs");
               m.script += "###[FILTERING]###\r\n\r\n";
           }

           //блок применяется только если разрешение поменялось
           string newres = null;
           bool check_mod2 = false;
           if (m.inresw != m.outresw || m.inresh != m.outresh || m.cropl != 0 || m.cropr != 0 ||
               m.cropt != 0 || m.cropb != 0 || m.blackw != 0 || m.blackh != 0)
           {
               int cropl = m.cropl;
               int cropr = m.cropr;
               int cropt = m.cropt;
               int cropb = m.cropb;
               int blackh = m.blackh;
               int blackw = m.blackw;
               int outresw = m.outresw;
               int outresh = m.outresh;

               //пересчитываем размеры для раздельных полей
               if (m.interlace == SourceType.FILM ||
                   m.interlace == SourceType.HYBRID_FILM_INTERLACED ||
                   m.interlace == SourceType.HYBRID_PROGRESSIVE_FILM ||
                   m.interlace == SourceType.HYBRID_PROGRESSIVE_INTERLACED ||
                   m.interlace == SourceType.INTERLACED)
               {
                   if (m.deinterlace == DeinterlaceType.Disabled)
                   {
                       outresh /= 2;
                       if (cropt != 0)
                           cropt /= 2;
                       if (cropb != 0)
                           cropb /= 2;
                       if (blackh != 0)
                           blackh /= 2;
                       cropt = Calculate.GetValid(cropt, 2);
                       cropb = Calculate.GetValid(cropb, 2);
                       blackh = Calculate.GetValid(blackh, 2);
                   }
               }

               if (cropl != 0 || cropr != 0 || cropt != 0 || cropb != 0)
                   m.script += "Crop(" + cropl + ", " + cropt + ", -" + cropr + ", -" + cropb + ")" + Environment.NewLine;

               if (m.resizefilter == Resizers.BicubicResizePlus)
                   newres = "BicubicResize(" + (outresw - (blackw * 2)) + ", " + (outresh - (blackh * 2)) + ", 0, 0.75)";
               else
                   newres = m.resizefilter + "(" + (outresw - (blackw * 2)) + ", " + (outresh - (blackh * 2)) + ")";

               //Вписываем ресайз
               if (cropl == 0 && cropr == 0 && cropt == 0 && cropb == 0 && newres != mod2)
               {
                   //если mod2 защита отличается от нужного разрешения
                   m.script += newres + Environment.NewLine;
                   check_mod2 = (mod2 != null);
               }
               else if (cropl != 0 || cropr != 0 || cropt != 0 || cropb != 0)
               {
                   //если mod2 защита равна нужному разрешению, но есть кроп
                   m.script += newres + Environment.NewLine;
                   check_mod2 = (mod2 != null);
               }

               if (blackw != 0 || blackh != 0)
                   m.script += "AddBorders(" + blackw + ", " + blackh + ", " + blackw + ", " + blackh + ")" + Environment.NewLine;
           }

           //Фильтрация после ресайза
           if (Settings.ResizeFirst)
           {
               m.script += "\r\n###[FILTERING]###\r\n";
               if (old_filtering != null) m.script += old_filtering;
               else if (m.filtering != "Disabled") m.script += LoadScript(startup_path + "\\presets\\filtering\\" + m.filtering + ".avs");
               m.script += "###[FILTERING]###\r\n\r\n";
           }

           //объединяем поля при необходимости
           if (m.interlace != SourceType.UNKNOWN && m.interlace != SourceType.PROGRESSIVE && m.interlace != SourceType.DECIMATING)
           {
               if (m.deinterlace == DeinterlaceType.Disabled)
                   m.script += "Weave()" + Environment.NewLine;
           }

           //вписываем параметры для гистограммы
           if (m.levels != "Disabled")
               m.script += "Histogram(\"" + m.levels + "\")" + Environment.NewLine;
           
           //добавляем субтитры
           if (m.subtitlepath != null)
           {
               string subext = Path.GetExtension(m.subtitlepath).ToLower();
               if (subext == ".idx")
                    m.script += "VobSub(\"" + m.subtitlepath + "\")";
               else
                    m.script += "TextSub(\"" + m.subtitlepath + "\")";
           }

           //Смена частоты кадров (AssumeFPS, ChangeFPS, ConvertFPS, ConvertMFlowFPS)
           if (ApplyFramerateModifier && m.frameratemodifer == FramerateModifers.ConvertMFlowFPS)
           {
               m.script += "ConvertMFlowFPS(" + m.outframerate.Replace(".", "") + ", 1000)\r\n";
           }
           else if (ApplyFramerateModifier)
           {
               //Подгонка звука для AssumeFPS
               if (IsAssumeFramerateConvertion && m.inaudiostreams.Count > 0 && m.outaudiostreams.Count > 0)
               {
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];
                   m.script += m.frameratemodifer + "(" + m.outframerate + ", true)" + Environment.NewLine;
                   //Должен вписываться ресемплер из настроек (sampleratemodifer) но тогда надо отлавливать ошибки SSRC
                   //из-за несовместимых частот (могут получиться после AssumeFPS).
                   if (outstream.samplerate != null) m.script += "ResampleAudio(" + outstream.samplerate + ")" + Environment.NewLine;
               }
               else
                   m.script += m.frameratemodifer + "(" + m.outframerate + ")" + Environment.NewLine;
           }

           //Трим
           if (m.trim_is_on)
           {
               for (int i = 0; i < m.trims.Count; i++)
               {
                   m.script += "Trim(" + Math.Max(((Trim)m.trims[i]).start, 0) + ", " + Math.Max(((Trim)m.trims[i]).end, 0) +
                       (i < m.trims.Count - 1 ? ((i + 1) % 5 == 0) ? ")++\\\r\n" : ")++" : ")\r\n");
               }
           }

           //Тестовая нарезка
           if (m.testscript)
               m.script += "SelectRangeEvery(FrameCount()/50, 50) #2500 frames test-script\r\n\r\n";

           //Убираем лишнюю mod2 защиту
           if (check_mod2)
           {
               string[] lines = m.script.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
               System.Collections.ArrayList sorted = new System.Collections.ArrayList();
               bool res_found = false, mod2_done = false;

               //Перебираем скрипт с конца в поисках текущего ресайза, если
               //между ресайзом и mod2 только строки с "" и # - mod2 не нужен
               for (int i = lines.Length - 1; i >= 0; i--)
               {
                   string line = lines[i];
                   if (mod2_done)
                   {
                       sorted.Add(line);
                   }
                   else if (res_found && line == mod2)
                   {
                       if (lines[i - 1].StartsWith("#Mod")) i--;
                       mod2_done = true;
                   }
                   else
                   {
                       if (!res_found && line == newres) res_found = true;
                       else if (res_found && line != "" && !line.StartsWith("#")) mod2_done = true;
                       sorted.Add(line);
                   }
               }

               //Переворачиваем скрипт обратно
               m.script = "";
               for (int i = sorted.Count - 1; i >= 0; i--)
                   m.script += sorted[i] + Environment.NewLine;
           }

           return m;
       }

       public static Massive SetGain(Massive m)
       {
           //определяем аудио потоки
           AudioStream instream = (AudioStream)m.inaudiostreams[m.inaudiostream];

           bool ok = false;
           string new_script = "";
           string[] lines = m.script.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

           //замена
           foreach (string line in lines)
           {
               if (line.ToLower().StartsWith("amplifydb("))
               {
                   if (instream.gain != "0.0") new_script += "AmplifydB(" + instream.gain + ")\r\n";
                   ok = true;
               }
               else
                   new_script += line + Environment.NewLine;
           }

           //добавление
           if (!ok && instream.gain != "0.0") new_script += "AmplifydB(" + instream.gain + ")\r\n";

           m.script = new_script;
           return m;
       }

       public static string GetInfoScript(Massive m, ScriptMode mode)
       {
           //определяем расширения
           string ext = Path.GetExtension(m.infilepath).ToLower();

           AudioStream instream = (m.inaudiostreams.Count > 0) ? (AudioStream)m.inaudiostreams[m.inaudiostream] : new AudioStream();

           //начинаем писать скрипт
           string script = "";

           // загружаем доп функции
           script += "import(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\functions\\AudioFunctions.avs\")" + Environment.NewLine;
           script += "import(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\functions\\VideoFunctions.avs\")" + Environment.NewLine;

           //загружаем необходимые плагины импорта
           if (m.vdecoder == Decoders.AVCSource)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\apps\\DGAVCDec\\DGAVCDecode.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.MPEG2Source)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\apps\\DGMPGDec\\DGDecode.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DGMultiSource)
               script += "loadplugin(\"" + m.dgdecnv_path + "DGDecodeNV.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DirectShowSource2)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\avss.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.RawSource)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\rawsource.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.QTInput)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\QTSource.dll\")" + Environment.NewLine;

           if (m.vdecoder == Decoders.FFmpegSource2 || instream.decoder == Decoders.FFAudioSource)
           {
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\FFMS2.dll\")" + Environment.NewLine;
               script += "import(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\FFMS2.avsi\")" + Environment.NewLine;
           }

           if (instream.decoder == Decoders.NicAC3Source || instream.decoder == Decoders.NicMPG123Source ||
               instream.decoder == Decoders.NicDTSSource || instream.decoder == Decoders.RaWavSource)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\NicAudio.dll\")" + Environment.NewLine;
           else if (instream.decoder == Decoders.bassAudioSource)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\bass\\bassAudio.dll\")" + Environment.NewLine;

           if (mode == ScriptMode.Autocrop)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\AutoCrop.dll\")" + Environment.NewLine;
           if (mode == ScriptMode.Interlace)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\TIVTC.dll\")" + Environment.NewLine;

           if (instream.channelconverter != AudioOptions.ChannelConverters.KeepOriginalChannels)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\soxfilter.dll\")" + Environment.NewLine;

           script += Environment.NewLine;

           //прописываем импорт видео

           //принудительная установка fps
           string fps = "";
           if ((m.vdecoder == Decoders.DirectShowSource || m.vdecoder == Decoders.DirectShowSource2) && m.inframerate != "")
               fps = ", fps=" + m.inframerate;

           //принудительная конвертация частоты
           string convertfps = "";
           if (m.vdecoder == Decoders.DirectShowSource && m.isconvertfps && Settings.DSS_ConvertFPS)
               convertfps = ", convertfps=true";

           //выбор аудио трека
           string audio = "";
           if (m.vdecoder == Decoders.DirectShowSource && (mode == ScriptMode.Autocrop || mode == ScriptMode.Interlace || instream.audiopath != null ||
               !Settings.DSS_Enable_Audio || !Settings.EnableAudio))
               audio = ", audio=false";
           else if (m.vdecoder == Decoders.AVISource && (mode == ScriptMode.Autocrop || mode == ScriptMode.Interlace || instream.audiopath != null ||
               !Settings.EnableAudio))
               audio = ", audio=false";
           else if (m.vdecoder == Decoders.FFmpegSource2 && m.inaudiostreams.Count > 0 && instream.audiopath == null && Settings.FFMS_Enable_Audio &&
               Settings.EnableAudio && mode != ScriptMode.Autocrop && mode != ScriptMode.Interlace)
               audio = ", atrack=" + (instream.ffid) + ", adjustdelay=-3";

           //ипортируем видео
           string invideostring = "";
           if (m.vdecoder == Decoders.BlankClip)
           {
               invideostring = m.vdecoder.ToString() + "(length=" + m.inframes + ", width=128, height=96, fps=" + m.inframerate + ", color=$000000)";
           }
           else if (ext == ".d2v")
           {
               int n = 0;
               foreach (string file in m.infileslist)
               {
                   n++;
                   invideostring += m.vdecoder.ToString() + "(\"" + file + "\", cpu=0, info=3)";
                   if (n < m.infileslist.Length) invideostring += "++";
               }
           }
           else if (ext != ".d2v" && m.vdecoder == Decoders.MPEG2Source)
           {
               invideostring = m.vdecoder.ToString() + "(\"" + m.indexfile + "\", cpu=0, info=3)";
           }
           else if (ext == ".dga" || ext == ".dgi")
           {
               int n = 0;
               foreach (string file in m.infileslist)
               {
                   n++;
                   invideostring += m.vdecoder.ToString() + "(\"" + file + "\")";
                   if (n < m.infileslist.Length) invideostring += "++";
               }
           }
           else
           {
               int n = 0;
               foreach (string file in m.infileslist)
               {
                   //Изменения под FFmpegSource2
                   string cache_path = "";
                   string assume_fps = "";
                   if (m.vdecoder == Decoders.FFmpegSource2)
                   {
                       //Стоит-ли повторяться? :) 
                       assume_fps = (!string.IsNullOrEmpty(m.inframerate) && Settings.FFMS_AssumeFPS) ? ".AssumeFPS(" + m.inframerate + ")" : "";
                       cache_path = ", rffmode=0" + ((Settings.FFMS_Threads > 0) ? ", threads=" + Settings.FFMS_Threads : "") +
                           ((m.ffms_indexintemp) ? ", cachefile=\"" + Settings.TempPath + "\\" + Path.GetFileName(file) + ".ffindex\"" : "");
                   }
                   n += 1;
                   invideostring += m.vdecoder.ToString() + "(\"" + file + "\"" + audio + fps + convertfps + cache_path + ")" + assume_fps;
                   if (n < m.infileslist.Length) invideostring += "++";
               }
           }

           //импорт звука и объединение
           if (m.inaudiostreams.Count > 0 && instream.audiopath != null && mode != ScriptMode.Autocrop && mode != ScriptMode.Interlace)
           {
               //прописываем импорт видео
               script += "video = " + invideostring + Environment.NewLine;

               //пришиваем звук
               string ffindex = "";
               string inaudiostring = "";
               string no_video = (instream.decoder == Decoders.DirectShowSource) ? ", video=false" : "";
               string drc = (instream.decoder == Decoders.NicAC3Source && Settings.NicAC3_DRC ||
                   instream.decoder == Decoders.NicDTSSource && Settings.NicDTS_DRC) ? ", drc=1" : "";
               if (instream.audiofiles != null && instream.audiofiles.Length > 0)
               {
                   int n = 0;
                   foreach (string file in instream.audiofiles)
                   {
                       n++;
                       ffindex += "FFIndex(" + "\"" + file + "\")\r\n";
                       inaudiostring += instream.decoder.ToString() + "(\"" + file + "\"" + no_video + drc + ")";
                       if (n < instream.audiofiles.Length) inaudiostring += "++";
                   }
               }
               else
               {
                   ffindex += "FFIndex(" + "\"" + instream.audiopath + "\")\r\n";
                   inaudiostring += instream.decoder.ToString() + "(\"" + instream.audiopath + "\"" + no_video + drc + ")";
               }
               if (instream.decoder == Decoders.FFAudioSource) script += ffindex;
               script += "audio = " + inaudiostring + Environment.NewLine;

               //объединение
               script += "AudioDub(video, audio)" + Environment.NewLine;
           }
           else
           {
               //прописываем импорт всего клипа
               script += invideostring + Environment.NewLine;
           }

           if (mode == ScriptMode.FastPreview || mode == ScriptMode.Normalize)
           {
               //блок обработки звука
               if (m.outaudiostreams.Count > 0)
               {
                   AudioStream outstream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                   //задержка
                   if (outstream.delay != 0)
                       script += "DelayAudio(" + Calculate.ConvertDoubleToPointString(Convert.ToDouble(outstream.delay) / 1000) + ")" + Environment.NewLine;

                   //меняем канальность
                   if (instream.channelconverter != AudioOptions.ChannelConverters.KeepOriginalChannels)
                       script += instream.channelconverter.ToString() + "()" + Environment.NewLine;

                   //меняем битность
                   if (instream.bits != outstream.bits)
                       script += "ConvertAudioTo16bit()" + Environment.NewLine;

                   //прописываем смену частоты
                   if (instream.samplerate != outstream.samplerate && outstream.samplerate != null)
                       script += m.sampleratemodifer + "(" + outstream.samplerate + ")" + Environment.NewLine;

                   //нормализация звука
                   if (mode != ScriptMode.Normalize && instream.gain != "0.0")
                       script += "AmplifydB(" + instream.gain + ")" + Environment.NewLine;
               }
           }

           //автокроп
           if (mode == ScriptMode.Autocrop)
           {
               script += "ConvertToYV12()" + Environment.NewLine + Environment.NewLine;
               script += "AutoCrop(mode=2, wMultOf=4, hMultOf=4, samples=" + Settings.AutocropFrames + ", threshold=" + Settings.AutocropSensivity +
                   ", file=\"" + Settings.TempPath + "\\AutoCrop.log\")" + Environment.NewLine;
           }

           if (mode == ScriptMode.Interlace)
           {
               script += "ConvertToYV12()" + Environment.NewLine;
           }

           return script;
       }

       public static string GetFramerateScript(Massive m)
       {
           //определяем расширения
           string ext = Path.GetExtension(m.infilepath).ToLower();

           //начинаем писать скрипт
           string script = "";

           //загружаем доп функции
           script += "import(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\functions\\VideoFunctions.avs\")" + Environment.NewLine;

           //загружаем необходимые плагины импорта
           if(m.vdecoder == Decoders.AVCSource)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\apps\\DGAVCDec\\DGAVCDecode.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.MPEG2Source)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\apps\\DGMPGDec\\DGDecode.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DGMultiSource)
               script += "loadplugin(\"" + m.dgdecnv_path + "DGDecodeNV.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DirectShowSource2)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\avss.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.RawSource)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\rawsource.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.QTInput)
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\QTSource.dll\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.FFmpegSource2)
           {
               script += "loadplugin(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\FFMS2.dll\")" + Environment.NewLine;
               script += "import(\"" + Calculate.StartupPath + "\\dlls\\AviSynth\\plugins\\FFMS2.avsi\")" + Environment.NewLine;
           }

           script += Environment.NewLine;

           if (m.vdecoder == Decoders.MPEG2Source)
               script += m.vdecoder.ToString() + "(\"" + m.indexfile + "\", cpu=0, info=3)" + Environment.NewLine;
           else if (m.vdecoder == Decoders.AVCSource || m.vdecoder == Decoders.DGMultiSource)
               script += m.vdecoder.ToString() + "(\"" + m.indexfile + "\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DirectShowSource || m.vdecoder == Decoders.AVISource)
               script += m.vdecoder.ToString() + "(\"" + m.infilepath + "\", audio=false)" + Environment.NewLine;
           else if (m.vdecoder == Decoders.DirectShowSource2)
               script += m.vdecoder.ToString() + "(\"" + m.infilepath + "\")" + Environment.NewLine;
           else if (m.vdecoder == Decoders.FFmpegSource2)
           {
               //Если FFMS_Enable_Audio, то разрешаем звук, чтоб файл сразу проиндексировался вместе с ним (иначе позже произойдет переиндексация)
               string atrack = (m.inaudiostreams.Count > 0 && Settings.FFMS_Enable_Audio) ? ", atrack=-1, adjustdelay=-3" : ", atrack=-2";
               string cache_path = atrack + ", rffmode=0" + ((Settings.FFMS_Threads > 0) ? ", threads=" + Settings.FFMS_Threads : "") +
                   ((m.ffms_indexintemp) ? ", cachefile=\"" + Settings.TempPath + "\\" + Path.GetFileName(m.infilepath) + ".ffindex\"" : "");
               script += m.vdecoder.ToString() + "(\"" + m.infilepath + "\"" + cache_path + ")" + Environment.NewLine;
           }

           script += "ConvertToYUY2()" + Environment.NewLine;//тут
           script += "Trim(0, 100)" + Environment.NewLine;

           return script;
       }

       public static string LoadScript(string scriptpath)
       {
           string line;
           string x = "";
           string startup_path = Calculate.StartupPath;
           using (StreamReader sr = new StreamReader(scriptpath, System.Text.Encoding.Default))
           {
               while (!sr.EndOfStream)
               {
                   line = sr.ReadLine();
                   if (line.StartsWith("#"))
                   {
                       if (line.EndsWith(".dll")) x += "LoadPlugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\" + line.Replace("#", "") + "\")\r\n";
                       else if (line.EndsWith(".avs")) x += "Import(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\" + line.Replace("#", "") + "\")\r\n";
                       else if (line.EndsWith(".avsi")) x += "Import(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\" + line.Replace("#", "") + "\")\r\n";
                       else if (line.EndsWith(".vdf")) x += "LoadVirtualDubPlugin(\"" + startup_path + "\\dlls\\AviSynth\\plugins\\" + line.Replace("#", "") + "\", ";
                       else if (line.StartsWith("#vdf_arguments:"))
                       {
                           string[] v = line.Split(new string[] { ":" }, StringSplitOptions.None);
                           x += "\"" + v[1] + "\", " + v[2] + ")" + Environment.NewLine;
                       }
                       else if (!Settings.HideComments) x += line + Environment.NewLine;
                   }
                   else
                       x += line + Environment.NewLine;
               }
           }
           return x;
       }

       public static Massive WriteScriptToFile(Massive m)
       {
           StreamWriter sw = new StreamWriter(Settings.TempPath + "\\" + m.key + ".avs", false, System.Text.Encoding.Default);
           string[] separator = new string[] { Environment.NewLine };
           string[] lines = m.script.Split(separator, StringSplitOptions.None);
           foreach (string line in lines)
                   sw.WriteLine(line);
           sw.Close();

           m.scriptpath = Settings.TempPath + "\\" + m.key + ".avs";

           return m;
       }

       public static void WriteScriptToFile(string script, string scriptname)
       {
           StreamWriter sw = new StreamWriter(Settings.TempPath + "\\" + scriptname + ".avs", false, System.Text.Encoding.Default);
           string[] separator = new string[] { Environment.NewLine };
           string[] lines = script.Split(separator, StringSplitOptions.None);
           foreach (string line in lines)
               sw.WriteLine(line);
           sw.Close();
       }

       private const string InterlaceScript =
@"{0}
{1}
file=""{2}""
global sep=""-""
c = last
global clip = c
c = WriteFile(c, file, ""a"", ""sep"", ""b"")
c = FrameEvaluate(c, ""global a = IsCombedTIVTC(clip, cthresh=9)"")
c = FrameEvaluate(c, ""global b = ((0.50*YDifference{5}(clip) + 0.25*UDifference{5}(clip) + 0.25*VDifference{5}(clip)) < 1.0) ? false : true"")
SelectRangeEvery(c, {3}, {4}, 0)
crop(0, 0, 16, 16)";

       private const string FieldOrderScript =
@"{0}
{1}
file=""{2}""
global sep=""-""
d = last
global abff = d.assumebff.separatefields
global atff = d.assumetff.separatefields
c = abff
c = WriteFile(c, file, ""diffa"", ""sep"", ""diffb"")
c = FrameEvaluate(c,""global diffa = 0.50*YDifference{5}(abff) + 0.25*UDifference{5}(abff) + 0.25*VDifference{5}(abff)"")
c = FrameEvaluate(c,""global diffb = 0.50*YDifference{5}(atff) + 0.25*UDifference{5}(atff) + 0.25*VDifference{5}(atff)"")
SelectRangeEvery(c, {3}, {4}, 0)
crop(0, 0, 16, 16)";

       public static string GetSourceDetectionScript(Detecting det, string originalScript, string trimLine, string logFileName, int selectEvery, int selectLength)
       {
           //Скрипты для анализа работают намного быстрее, если вместо DifferenceFromPrevious использовать DifferenceToNext.
           //Не должно сильно сказаться на погрешности, т.к. всё-равно для достоверного определения движения в текущем кадре
           //нужны оба, и предыдущий, и последующий. Используя только два кадра нельзя определить, какой из них с движением,
           //а какой статичен. Так-что с FromPrevious всегда будет сколько-то неверно определенных кадров, для которых лучше
           //было бы использовать ToNext. И наоборот. Зато прибавка в скорости существенная! :)
           if (det == Detecting.Interlace)   //detection
               return string.Format(InterlaceScript, originalScript, trimLine, logFileName, selectEvery, selectLength, "ToNext");
           else if (det == Detecting.Fields) //field order
               return string.Format(FieldOrderScript, originalScript, trimLine, logFileName, selectEvery, selectLength, "ToNext");
           else
               return null;
       }
    }
}
