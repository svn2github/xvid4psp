using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;

namespace XviD4PSP
{
    public class dpgmuxer
    {
        public struct DPGHeader
        {
            // frames per second
	        //header->fps = readint( dpg ) / 0x100;

            public version_id version;          // DPG0 DPG1 DPG2 DPG3
            public int frames;                  // total number of frames in video stream
            public int fps;                     // frames per second (in file * 0x100)
            public int samplerate;              // samplerate of audio stream
            public audio_id audio_id;           // MP2  GSM1(channel)  GSM2(channels)  OGG
            public int aoffset;                 // address of audio stream
            public int asize;                   // size of audio stream in bytes 
            public int vsize;                   // size of video stream in bytes     
            public int voffset;                 // address of video stream
            // version_id >= DPG2
            public int goffset;                 // address of GOP list
            public int gsize;                   // size of GOP list
            // version_id >= DPG1
            public pixel_format pixel_format;
            public bool complete;          	
	        // DPG2 and DPG3 use the same header
        }

        public enum version_id { DPG0, DPG1, DPG2, DPG3 };
        public enum audio_id { MP2, GSM1, GSM2, OGG };
        public enum pixel_format { RGB18, RGB21, RGB24 };

        public void MuxStreams(Massive m)
        {
            //создаём новый файл
            using (FileStream target = new FileStream(m.outfilepath, FileMode.Create))
            {
                //создаём заголовок
                DPGHeader header = new DPGHeader();
                header.version = version_id.DPG0;
                header.aoffset = 36;
                header.frames = m.outframes;
                header.fps = (int)Calculate.ConvertStringToDouble(m.outframerate);
                header.vsize = (int)new FileInfo(m.outvideofile).Length;
                header.pixel_format = pixel_format.RGB24;
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream a = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    header.samplerate = Convert.ToInt32(a.samplerate);
                    header.asize = (int)new FileInfo(a.audiopath).Length;
                    string aext = Path.GetExtension(a.audiopath).ToLower();

                    if (aext == ".mp2")
                        header.audio_id = audio_id.MP2;
                    if (aext == ".gsm" && a.channels == 1)
                        header.audio_id = audio_id.GSM1;
                    if (aext == ".gsm" && a.channels == 2)
                        header.audio_id = audio_id.GSM2;
                    if (aext == ".ogg")
                        header.audio_id = audio_id.OGG;
                }

                //пишем заголовок
                WriteHeader(target, header);

                //вычисляем процент
                long total = header.aoffset + header.asize + header.vsize;

                //пишем звук
                if (m.outaudiostreams.Count > 0)
                {
                    AudioStream a = (AudioStream)m.outaudiostreams[m.outaudiostream];
                    using (FileStream fs = File.OpenRead(a.audiopath))
                    {
                        byte[] buffer = new byte[16384];
                        int bytesRead;
                        while (true)
                        {
                            //читаем
                            bytesRead = fs.Read(buffer, 0, buffer.Length);

                            //прогресс
                            double progress = ((double)target.Position / (double)total) * 100.0;
                            ProgressChanged(progress);

                            //файл закончился
                            if (bytesRead == 0)
                                break;

                            //пишем в новый файл
                            target.Write(buffer, 0, bytesRead);
                            fs.Flush();
                            target.Flush();
                        }
                    }
                }

                //пишем видео
                using (FileStream fs = File.OpenRead(m.outvideofile))
                {
                    byte[] buffer = new byte[16384];
                    int bytesRead;
                    while (true)
                    {
                        //читаем
                        bytesRead = fs.Read(buffer, 0, buffer.Length);

                        //файл закончился
                        if (bytesRead == 0)
                            break;

                        //прогресс
                        double progress = ((double)target.Position / (double)total) * 100.0;
                        ProgressChanged(progress);

                        //пишем в файл
                        target.Write(buffer, 0, bytesRead);
                        fs.Flush();
                        target.Flush();
                    }
                }
            }
        }

        public void DemuxAudio(string infile, string outfile)
        {
            DPGHeader header = ReadHeader(infile);

            using (FileStream outstream = new FileStream(outfile, FileMode.Create))
            {
                using (FileStream instream = File.OpenRead(infile))
                {
                    CopyStream(instream, outstream, header.aoffset, header.asize);
                }
            }
        }

        public void DemuxVideo(string infile, string outfile)
        {
            DPGHeader header = ReadHeader(infile);

            using (FileStream outstream = new FileStream(outfile, FileMode.Create))
            {
                using (FileStream instream = File.OpenRead(infile))
                {
                    CopyStream(instream, outstream, header.voffset, header.vsize);
                }
            }
        }

        private void CopyStream(FileStream instream, FileStream outstream, long offset, long streamsize)
        {
            double progress;
            byte[] buffer = new byte[16384];
            int bytesRead;

            long chunk_count = streamsize / buffer.Length;
            instream.Seek(offset, SeekOrigin.Begin);

            for (int c = 0; c < chunk_count; c++)
            {
                //читаем
                bytesRead = instream.Read(buffer, 0, buffer.Length);

                //файл закончился
                if (bytesRead == 0)
                    break;

                //пишем
                outstream.Write(buffer, 0, bytesRead);
                instream.Flush();
                outstream.Flush();

                //прогресс
                progress = ((double)(outstream.Position) / (double)streamsize) * 100.0;
                ProgressChanged(progress);
            }

            //последняя партия
            bytesRead = instream.Read(buffer, 0, (int)(streamsize % (long)buffer.Length));

            //файл закончился
            if (bytesRead == 0)
                return;

            //пишем остаток
            outstream.Write(buffer, 0, bytesRead);
            instream.Flush();
            outstream.Flush();
        }

        private void WriteHeader(FileStream target, DPGHeader header)
        {
            //revision (4 bytes)
            WriteText(target, header.version.ToString());
            //frames (4 bytes integer)
            target.Write(BitConverter.GetBytes(header.frames), 0, 4);
            //pad (1 byte)
            target.WriteByte(0);
            //fps (1 byte)
            target.Write(BitConverter.GetBytes(header.fps), 0, 1);
            //pad (2 byte)
            target.WriteByte(0);
            target.WriteByte(0);
            //sample rate (4 bytes integer)
            target.Write(BitConverter.GetBytes(header.samplerate), 0, 4);
            //channels (4 bytes integer)
            target.Write(BitConverter.GetBytes((int)header.audio_id), 0, 4);
            //audio start offset (4 bytes integer)
            target.Write(BitConverter.GetBytes(header.aoffset), 0, 4);
            //audio size (4 bytes integer):
            target.Write(BitConverter.GetBytes(header.asize), 0, 4);
            //video start offset (4 bytes integer)
            target.Write(BitConverter.GetBytes(header.aoffset + header.asize), 0, 4);
            //video size (4 bytes integer)
            target.Write(BitConverter.GetBytes(header.vsize), 0, 4);

            if (header.version == version_id.DPG2 ||
                header.version == version_id.DPG3)
            {
                //gop offset (4 bytes integer)
                target.Write(BitConverter.GetBytes(header.goffset), 0, 4);
                //gop size (4 bytes integer)
                target.Write(BitConverter.GetBytes(header.gsize), 0, 4);
            }
        }

        public DPGHeader ReadHeader(string filepath)
        {
            DPGHeader header = new DPGHeader();
            ASCIIEncoding encoder = new ASCIIEncoding();

            //пробуем прочитать header
            using (FileStream fs = File.OpenRead(filepath))
            {
                //revision (4 bytes)
                byte[] buffer = new byte[4];
                fs.Read(buffer, 0, buffer.Length);
                header.version = (version_id)Enum.Parse(typeof(version_id), encoder.GetString(buffer));

                //frames (4 bytes integer)
                fs.Read(buffer, 0, buffer.Length);
                header.frames = BitConverter.ToInt32(buffer, 0);

                //pad (1 byte)
                fs.ReadByte();

                //fps (1 byte)
                buffer = new byte[1];
                fs.Read(buffer, 0, buffer.Length);
                header.fps = buffer[0];

                //pad (2 byte)
                fs.ReadByte();
                fs.ReadByte();

                //sample rate (4 bytes integer)
                buffer = new byte[4];
                fs.Read(buffer, 0, buffer.Length);
                header.samplerate = BitConverter.ToInt32(buffer, 0);

                //audio_id (4 bytes integer)
                fs.Read(buffer, 0, buffer.Length);
                header.audio_id = (audio_id)Enum.Parse(typeof(audio_id), BitConverter.ToInt32(buffer, 0).ToString());

                //audio start offset (4 bytes integer)
                fs.Read(buffer, 0, buffer.Length);
                header.aoffset = BitConverter.ToInt32(buffer, 0);

                //audio size (4 bytes integer):
                fs.Read(buffer, 0, buffer.Length);
                header.asize = BitConverter.ToInt32(buffer, 0);

                //video start offset (4 bytes integer)
                fs.Read(buffer, 0, buffer.Length);
                header.voffset = BitConverter.ToInt32(buffer, 0);

                //video size (4 bytes integer)
                fs.Read(buffer, 0, buffer.Length);
                header.vsize = BitConverter.ToInt32(buffer, 0);

                if (header.version == version_id.DPG2 ||
                    header.version == version_id.DPG3)
                {
                    //gop offset
                    fs.Read(buffer, 0, buffer.Length);
                    header.goffset = BitConverter.ToInt32(buffer, 0);

                    //gop size
                    fs.Read(buffer, 0, buffer.Length);
                    header.gsize = BitConverter.ToInt32(buffer, 0);
                }         

                //проверка на целостность
                if ((fs.Length - (header.aoffset + header.asize + header.vsize)) == 0)
                    header.complete = true;
                else
                    header.complete = false;
            }

            return header;
        }

        private void WriteText(FileStream fs, string value)
        {
            byte[] info = new ASCIIEncoding().GetBytes(value);
            fs.Write(info, 0, info.Length);
        }

        public delegate void ProgressChangedDelegate(double progress);
        public event ProgressChangedDelegate ProgressChanged;

    }
}
