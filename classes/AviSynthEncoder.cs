using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace XviD4PSP
{
   public class AviSynthEncoder
    {
        private Process _encoderProcess;

        public string encoderPath = null;
        public int frame;
        public string args = null, error_text = "";
        public bool IsAborted = false, IsErrors = false, IsGainDetecting = false;
        public double gain = 0;

        private Massive m;
        private string script;
        private string outfilepath;

        private ManualResetEvent locker = new ManualResetEvent(true);
        private Thread _encoderThread = null;
        private Thread _readFromStdOutThread = null;
        private Thread _readFromStdErrThread = null;
        private string _encoderStdErr = null;
        private string _encoderStdOut = null;
        private static readonly Regex _cleanUpStringRegex = new Regex(@"\n[^\n]+\r", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        public AviSynthEncoder(Massive mass, string script, string outfilepath)
        {
            m = mass.Clone();
            this.script = script;
            this.outfilepath = outfilepath;
        }

        public AviSynthEncoder(Massive mass)
        {
            m = mass.Clone();
            this.script = m.script;
            AudioStream stream = (AudioStream)m.outaudiostreams[0];
            this.outfilepath = stream.audiopath;
        }

        //Только для определения громкости
        public AviSynthEncoder(Massive mass, string script)
        {
            m = mass.Clone();
            this.script = script;
            IsGainDetecting = true;
        }

        private void readStdOut()
        {
            readStdStream(true);
        }

        private void readStdErr()
        {
            readStdStream(false);
        }

        private string cleanUpString(string s)
        {
            return _cleanUpStringRegex.Replace(s.Replace(Environment.NewLine, "\n"), Environment.NewLine);
        }

        private void readStdStream(bool bStdOut)
        {
            try
            {
                using (StreamReader r = bStdOut ? _encoderProcess.StandardOutput : _encoderProcess.StandardError)
                {
                    while (!_encoderProcess.HasExited)
                    {
                        Thread.Sleep(0);
                        string text = r.ReadToEnd();
                        if (text != null && text.Length > 0)
                        {
                            if (bStdOut)
                                _encoderStdOut = cleanUpString(text);
                            else
                                _encoderStdErr = cleanUpString(text);
                        }
                        Thread.Sleep(0);
                    }
                }
            }
            catch { }
        }

        private Stream getOutputStream()
        {
            return (encoderPath == null) ? (new FileStream(outfilepath, FileMode.Create)) : _encoderProcess.StandardInput.BaseStream;
        }

        private void StartEncoding()
        {
            try
            {
                using (AviSynthScriptEnvironment env = new AviSynthScriptEnvironment())
                {
                    using (AviSynthClip a = env.ParseScript(script, AviSynthColorspace.RGB24))
                    //using (AviSynthClip a = env.OpenScriptFile(inFilePath, AviSynthColorspace.RGB24))
                    {
                        if (a.ChannelsCount == 0) throw new Exception("Can't find audio stream");

                        const int MAX_SAMPLES_PER_ONCE = 4096;
                        int frameSample = 0;

                        //MeGUI
                        int frameBufferTotalSize = MAX_SAMPLES_PER_ONCE * a.ChannelsCount * a.BytesPerSample;

                        byte[] frameBuffer = new byte[frameBufferTotalSize];

                        if (encoderPath != null) createEncoderProcess();

                        if (!IsGainDetecting)
                        {
                            //Обычное кодирование/извлечение звука
                            using (Stream target = getOutputStream())
                            {
                                //let's write WAV Header
                                writeHeader(target, a);

                                GCHandle h = GCHandle.Alloc(frameBuffer, GCHandleType.Pinned);
                                IntPtr address = h.AddrOfPinnedObject();
                                try
                                {
                                    while (frameSample < a.SamplesCount)
                                    {
                                        locker.WaitOne();
                                        int nHowMany = Math.Min((int)(a.SamplesCount - frameSample), MAX_SAMPLES_PER_ONCE);
                                        a.ReadAudio(address, frameSample, nHowMany);

                                        locker.WaitOne();
                                        frame = (int)(((double)frameSample / (double)a.SamplesCount) * (double)a.num_frames);

                                        target.Write(frameBuffer, 0, nHowMany * a.ChannelsCount * a.BytesPerSample);

                                        target.Flush();
                                        frameSample += nHowMany;
                                        Thread.Sleep(0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (_encoderProcess != null && _encoderProcess.HasExited)
                                        throw new Exception("Abnormal encoder termination (exit code = " + _encoderProcess.ExitCode.ToString() + ")");
                                    else throw new Exception(ex.Message, ex);
                                }
                                finally
                                {
                                    h.Free();
                                }

                                if (a.BytesPerSample % 2 == 1)
                                    target.WriteByte(0);
                            }
                        }
                        else
                        {
                            //Определяем peak level
                            int max = 0, e = 0, ch = a.ChannelsCount;
                            GCHandle h = GCHandle.Alloc(frameBuffer, GCHandleType.Pinned);
                            IntPtr address = h.AddrOfPinnedObject();
                            try
                            {
                                while (frameSample < a.SamplesCount)
                                {
                                    locker.WaitOne();
                                    int nHowMany = Math.Min((int)(a.SamplesCount - frameSample), MAX_SAMPLES_PER_ONCE);
                                    frame = (int)(((double)frameSample / (double)a.SamplesCount) * (double)a.num_frames);
                                    a.ReadAudio(address, frameSample, nHowMany);

                                    locker.WaitOne();
                                    int n = 0, pos = 0;
                                    while (n < nHowMany)
                                    {
                                        //Ищем максимум для каждого канала
                                        for (int i = 0; i < ch; i += 1)
                                        {
                                            //Это годится только для 16-ти битного звука!
                                            e = BitConverter.ToInt16(frameBuffer, pos);
                                            max = Math.Max(max, Math.Abs(e));
                                            pos += 2;
                                        }

                                        n += 1;
                                    }

                                    frameSample += nHowMany;
                                    Thread.Sleep(0);
                                }

                                if (max != 0)
                                    gain = 20.0 * Math.Log10((32767.0 * Convert.ToDouble(m.volume.Replace("%", ""))) / ((double)max * 100.0));
                            }
                            finally
                            {
                                h.Free();
                            }
                        }
                        if (_encoderProcess != null)
                        {
                            _encoderProcess.WaitForExit();
                            _readFromStdErrThread.Join();
                            _readFromStdOutThread.Join();
                            if (_encoderProcess.ExitCode != 0)
                                throw new Exception("Abnormal encoder termination (exit code = " + _encoderProcess.ExitCode.ToString() + ")");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!IsAborted)
                {
                    IsErrors = true;
                    error_text = ((IsGainDetecting) ? "Gain Detector Error: " : "AviSynth Encoder Error: ") + ex.Message;
                    try
                    {
                        if (_encoderProcess != null && _encoderProcess.HasExited && _encoderProcess.ExitCode != 0)
                            error_text += ("\r\n" + (!string.IsNullOrEmpty(_encoderStdErr) ? "\r\n" + _encoderStdErr : "") +
                                (!string.IsNullOrEmpty(_encoderStdOut) ? "\r\n" + _encoderStdOut : ""));
                    }
                    catch (Exception exc)
                    {
                        error_text += "\r\n\r\nThere was Exception while trying to get error info:\r\n" + exc.Message;
                    }
                }
            }
            finally
            {
                try
                {
                    if (_encoderProcess != null && !_encoderProcess.HasExited)
                    {
                        _encoderProcess.Kill();
                        _encoderProcess.WaitForExit();
                        _readFromStdErrThread.Join();
                        _readFromStdOutThread.Join();
                    }
                }
                catch { }
                _encoderProcess = null;
                _readFromStdErrThread = null;
                _readFromStdOutThread = null;
                _encoderThread = null;
            }
        }

        private void createEncoderProcess()
        {
            try
            {
                _encoderProcess = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                AudioStream stream = (AudioStream)m.outaudiostreams[m.outaudiostream];

                if (encoderPath.Contains("neroAacEnc.exe"))
                {
                    info.Arguments = "-ignorelength " + stream.passes + " -if - -of \"" + outfilepath + "\"";
                }
                else if (encoderPath.Contains("lame.exe"))
                {
                    string resample = "";
                    if (m.mp3_options.forcesamplerate)
                        resample = Calculate.ConvertDoubleToPointString(Convert.ToDouble(stream.samplerate) / 1000.0, 1);
                    info.Arguments = stream.passes + resample + " - \"" + outfilepath + "\"";
                    if (stream.channels == 1)
                        info.Arguments = info.Arguments.Replace(" -m s", " -m m").Replace(" -m j", " -m m").Replace(" -m f", " -m m");
                }
                else if (encoderPath.Contains("ffmpeg.exe"))
                    info.Arguments = "-i - " + stream.passes + " -vn \"" + outfilepath + "\"";
                else if (encoderPath.Contains("aften.exe"))
                    info.Arguments = stream.passes + " - \"" + outfilepath + "\"";

                //запоминаем аргументы для лога
                args = info.Arguments;

                info.FileName = encoderPath;
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.UseShellExecute = false;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;
                _encoderProcess.StartInfo = info;
                _encoderProcess.Start();
                SetPriority(Settings.ProcessPriority);
                _readFromStdOutThread = new Thread(new ThreadStart(readStdOut));
                _readFromStdErrThread = new Thread(new ThreadStart(readStdErr));
                _readFromStdOutThread.Start();
                _readFromStdOutThread.Priority = ThreadPriority.Normal;
                _readFromStdErrThread.Start();
                _readFromStdErrThread.Priority = ThreadPriority.Normal;
            }
            catch (Exception e)
            {
                if (_encoderProcess != null)
                {
                    try
                    {
                        _encoderProcess.Kill();
                        _encoderProcess.WaitForExit();
                        _readFromStdErrThread.Join();
                        _readFromStdOutThread.Join();
                    }
                    catch { }
                    finally { _encoderProcess = null; }
                }

                throw new Exception("Can't start encoder: " + e.Message, e);
            }
        }

        private void writeHeader(Stream target, AviSynthClip a)
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

            target.Write(BitConverter.GetBytes(a.BitsPerSample), 0, 2);
            target.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
            target.Write(BitConverter.GetBytes(useFaadTrick ? (FAAD_MAGIC_VALUE - WAV_HEADER_SIZE) : (uint)a.AudioSizeInBytes), 0, 4);
        }

        private void Start()
        {
            _encoderThread = new Thread(new ThreadStart(this.StartEncoding));
            _encoderThread.Priority = ThreadPriority.Lowest;
            _encoderThread.Start();
        }

        private void Abort()
        {
            IsAborted = true;
            locker.Set();
            if (_encoderThread != null)
             _encoderThread.Abort();
             _encoderThread = null;

            if (_encoderProcess != null && !_encoderProcess.HasExited)
            {
                _encoderProcess.Kill();
                _encoderProcess.WaitForExit();
            }
        }

        public bool IsBusy()
        {
            if (_encoderThread != null)
                return true;
            else
                return false;
        }

        public void start()
        {
                this.Start();
        }

        public void stop()
        {
            this.Abort();
        }

        public void pause()
        {
            if (_encoderThread != null)
                locker.Reset();
        }

        public void resume()
        {
            if (_encoderThread != null)
                locker.Set();
        }

        public void SetPriority(int prioritet)
        {
            if (this._encoderProcess != null && !_encoderProcess.HasExited)
            {
                if (prioritet == 0)
                {
                    _encoderProcess.PriorityClass = ProcessPriorityClass.Idle;
                    _encoderProcess.PriorityBoostEnabled = false;
                }
                else if (prioritet == 1)
                {
                    _encoderProcess.PriorityClass = ProcessPriorityClass.Normal;
                    _encoderProcess.PriorityBoostEnabled = true;
                }
                else if (prioritet == 2)
                {
                    _encoderProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
                    _encoderProcess.PriorityBoostEnabled = true;
                }
            }
        }
    }
}
