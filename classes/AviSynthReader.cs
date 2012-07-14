using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace XviD4PSP
{
    public class AviSynthReader
    {
        private AviSynthScriptEnvironment environment = null;
        private AviSynthColorspace forced_colorspace;
        private AudioSampleType forced_sampletype;

        private AviSynthClip clip = null;
        public AviSynthClip Clip
        { 
            get { return this.clip; }
        }

        public AviSynthReader()
        {
            //По умолчанию форматы не меняются
            forced_colorspace = AviSynthColorspace.Undefined;
            forced_sampletype = AudioSampleType.Undefined;
        }

        public AviSynthReader(AviSynthColorspace forceColorSpace, AudioSampleType forceSampleType)
        {
            //Если надо - меняем форматы
            forced_colorspace = forceColorSpace;
            forced_sampletype = forceSampleType;
        }

        //Скрипт в виде string
        public void ParseScript(string script)
        {
            try
            {
                this.environment = new AviSynthScriptEnvironment();
                this.clip = environment.ParseScript(script, forced_colorspace, forced_sampletype);
                //if (!this.clip.HasVideo) throw new ArgumentException("Script doesn't contain video");
            }
            catch (Exception)
            {
                cleanup();
                throw;
            }
        }

        //Скрипт из файла
        public void OpenScript(string script)
        {
            try
            {
                this.environment = new AviSynthScriptEnvironment();
                this.clip = environment.OpenScriptFile(script, forced_colorspace, forced_sampletype);
                if (!this.clip.HasVideo) throw new ArgumentException("Script doesn't contain video");
            }
            catch (Exception)
            {
                cleanup();
                throw;
            }
        }

        private void cleanup()
        {
            if (this.clip != null)
            {
                (this.clip as IDisposable).Dispose();
                this.clip = null;
            }
            if (this.environment != null)
            {
                (this.environment as IDisposable).Dispose();
                this.environment = null;
            }
        }

        public void Close()
        {
            cleanup();
        }

        public int Width
        {
            get { return (clip.HasVideo) ? clip.VideoWidth : 0; }
        }

        public int Height
        {
            get { return (clip.HasVideo) ? clip.VideoHeight : 0; }
        }

        public double Framerate
        {
            get { return (clip.HasVideo) ? ((double)clip.raten) / ((double)clip.rated) : 0; }
        }

        public int FrameCount
        {
            get { return clip.num_frames; }
        }

        public int BitsPerSample
        {
            get { return clip.BitsPerSample; }
        }

        public int Samplerate
        {
            get { return clip.AudioSampleRate; }
        }

        public long SamplesCount
        {
            get { return clip.SamplesCount; }
        }

        public int Channels
        {
            get { return clip.ChannelsCount; }
        }

        public bool GetVarBoolean(string variable_name, bool default_value)
        {
            return clip.GetVarBoolean(variable_name, default_value);
        }

        public int GetVarInteger(string variable_name, int default_value)
        {
            return clip.GetVarInteger(variable_name, default_value);
        }

        public float GetVarFloat(string variable_name, float default_value)
        {
            return clip.GetVarFloat(variable_name, default_value);
        }

        public string GetVarString(string variable_name, string default_value)
        {
            return clip.GetVarString(variable_name, default_value);
        }

        public Bitmap ReadFrameBitmap(int position)
        {
            if (clip.PixelType != AviSynthColorspace.RGB24)
                throw new Exception("ReadFrameBitmap: Invalid PixelType (only RGB24 is supported)");

            Bitmap bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                // Lock the bitmap's bits.  
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                try
                {
                    // Get the address of the first line.
                    IntPtr ptr = bmpData.Scan0;
                    // Read data
                    clip.ReadFrame(ptr, bmpData.Stride, position);
                }
                finally
                {
                    // Unlock the bits.
                    bmp.UnlockBits(bmpData);
                }
                bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
                return bmp;
            }
            catch (Exception)
            {
                bmp.Dispose();
                throw;
            }
        }

        public void ReadFrameDummy(int position)
        {
            clip.ReadFrame(IntPtr.Zero, 0, position);
        }
    }
}
