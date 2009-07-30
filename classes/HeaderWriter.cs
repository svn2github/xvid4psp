using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace XviD4PSP
{
   public class HeaderWriter
    {

       private static void writeWaveHeader(Stream target, AviSynthClip a)
       {
           const uint FAAD_MAGIC_VALUE = 0xFFFFFF00;
           const uint WAV_HEADER_SIZE = 36;
           bool useFaadTrick = a.AudioSizeInBytes >= (uint.MaxValue - WAV_HEADER_SIZE);
           target.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
           target.Write(BitConverter.GetBytes(useFaadTrick ? FAAD_MAGIC_VALUE : (uint)(a.AudioSizeInBytes + WAV_HEADER_SIZE)), 0, 4);
           target.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "), 0, 8);
           target.Write(BitConverter.GetBytes((uint)0x10), 0, 4);
           target.Write(BitConverter.GetBytes((short)0x01), 0, 2);
           target.Write(BitConverter.GetBytes(a.ChannelsCount), 0, 2);
           target.Write(BitConverter.GetBytes(a.AudioSampleRate), 0, 4);

           //MEGUI
           target.Write(BitConverter.GetBytes(a.AvgBytesPerSec), 0, 4);
           target.Write(BitConverter.GetBytes(a.BytesPerSample * a.ChannelsCount), 0, 2);

           //BEHAPPY
           //target.Write(BitConverter.GetBytes(a.BitsPerSample * a.AudioSampleRate * a.ChannelsCount / 8), 0, 4);
           //target.Write(BitConverter.GetBytes(a.ChannelsCount * a.BitsPerSample / 8), 0, 2);

           target.Write(BitConverter.GetBytes(a.BitsPerSample), 0, 2);
           target.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
           target.Write(BitConverter.GetBytes(useFaadTrick ? (FAAD_MAGIC_VALUE - WAV_HEADER_SIZE) : (uint)a.AudioSizeInBytes), 0, 4);
       }

       public static void Test(Massive m)
       {
           try
           {
               using (AviSynthScriptEnvironment env = new AviSynthScriptEnvironment())
               {
                   using (AviSynthClip a = env.ParseScript(m.script, AviSynthColorspace.RGB24))
                   {
                       using (Stream target = new FileStream("d:\\test.wav", FileMode.Create))
                       {
                           //пишем заголовок
                           writeWaveHeader(target, a);

                           //сохраняем декодированный звук в WAVE
                           const int MAX_SAMPLES_PER_ONCE = 4096;
                           int frameSample = 0;
                           int frameBufferTotalSize = MAX_SAMPLES_PER_ONCE * a.ChannelsCount * a.BytesPerSample;
                           byte[] frameBuffer = new byte[frameBufferTotalSize];

                           GCHandle h = GCHandle.Alloc(frameBuffer, GCHandleType.Pinned);
                           IntPtr address = h.AddrOfPinnedObject();
                           try
                           {
                               while (frameSample < a.SamplesCount)
                               {
                                   int nHowMany = Math.Min((int)(a.SamplesCount - frameSample), MAX_SAMPLES_PER_ONCE);
                                   a.ReadAudio(address, frameSample, nHowMany);
                                   //frame = (int)(((double)frameSample / (double)a.SamplesCount) * (double)a.num_frames);

                                   target.Write(frameBuffer, 0, nHowMany * a.ChannelsCount * a.BytesPerSample);
                                   target.Flush(); //флюш немедленно пишет данные в файл и освобождает буффера???
                                   frameSample += nHowMany;
                               }
                           }
                           finally
                           {
                               h.Free();
                           }

                           //интересно зачем это ??
                           if (a.BytesPerSample % 2 == 1)
                               target.WriteByte(0);

                       }
                   }
               }
           }
           catch (Exception ex)
           {
               Message mes = new Message();
               mes.ShowMessage(ex.Message);
           }
       }



    }
}
