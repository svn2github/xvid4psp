using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace XviD4PSP
{
    public class AviSynthReader
    {
        private AviSynthScriptEnvironment enviroment = null;
        private AviSynthClip clip = null;
        private int width, height;
        private double frameRate;

        public AviSynthClip Clip
        {
            get
            {
                return this.clip;
            }
        }

        public AviSynthReader()
        {
        }

        public void ParseScript(string script)
        {
            try
            {
                this.enviroment = new AviSynthScriptEnvironment();
                this.clip = enviroment.ParseScript(script, AviSynthColorspace.RGB24);
                if (this.clip.HasVideo)
                {
                    //throw new ArgumentException("Script doesn't contain video");
                    this.height = this.clip.VideoHeight;
                    this.width = this.clip.VideoWidth;
                    this.frameRate = ((double)clip.raten) / ((double)clip.rated);
                }
            }
            catch (Exception)
            {
                cleanup();
                throw;
            }
        }

        public void OpenScript(string script)
        {
            try
            {
                this.enviroment = new AviSynthScriptEnvironment();
                this.clip = enviroment.OpenScriptFile(script, AviSynthColorspace.RGB24);
                if (!this.clip.HasVideo)
                    throw new ArgumentException("Script doesn't contain video");
                this.height = this.clip.VideoHeight;
                this.width = this.clip.VideoWidth;
                this.frameRate = ((double)clip.raten) / ((double)clip.rated);
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
            if (this.enviroment != null)
            {
                (this.enviroment as IDisposable).Dispose();
                this.enviroment = null;
            }
        }

        public void Close()
        {
            cleanup();
        }

        public int Width
        {
            get { return this.width; }
        }

        public int Height
        {
            get { return this.height; }
        }

        public double Framerate
        {
            get { return this.frameRate; }
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

        public int GetIntVariable(string variable_name, int default_value)
        {
            return clip.GetIntVariable(variable_name, default_value);
        }

        public Bitmap ReadFrameBitmap(int position)
        {
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
    }
}
