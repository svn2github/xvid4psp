using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Timers;
using System.Text.RegularExpressions;

namespace XviD4PSP
{
    public partial class Caching
    {
        private BackgroundWorker worker = null;
        public Massive m;
        AviSynthReader reader;
        public string error;
        private int count = 0;

        public Caching(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = mass.owner;
            m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title =  Languages.Translate("Caсhing") + "...";
            text_info.Content = Languages.Translate("Please wait... Work in progress...");

            //фоновое кодирование
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (m != null)
            {
                string script = "";
                try
                {
                    string ext = Path.GetExtension(m.infilepath).ToLower();

                    //получаем инфу из простого avs
                    script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Info);

                    reader = new AviSynthReader();
                    reader.ParseScript(script);

                    //выходим при отмене операции
                    if (reader == null) return;

                    AudioStream instream = (m.inaudiostreams.Count > 0) ? (AudioStream)m.inaudiostreams[m.inaudiostream] : new AudioStream();

                    if (reader.Framerate != Double.PositiveInfinity &&
                        reader.Framerate != 0.0)// &&m.duration == TimeSpan.Zero
                    {
                        m.induration = TimeSpan.FromSeconds((double)reader.FrameCount / reader.Framerate);
                        m.outduration = m.induration;
                        m.inframes = reader.FrameCount;
                        if (m.inframerate == null ||
                            m.inframerate == "")
                            m.inframerate = Calculate.ConvertDoubleToPointString(reader.Framerate);
                    }

                    if (m.isvideo && ext != ".avs" && (reader.Width == 0 || reader.Height == 0))
                        throw new Exception(m.vdecoder.ToString() + " can`t decode video (zero-size image was returned)!");
                    else
                    {
                        m.inresw = reader.Width;
                        m.inresh = reader.Height;
                    }

                    if (ext == ".avs")
                    {
                        //Считываем аспект из скрипта
                        m.pixelaspect = (double)reader.GetIntVariable("OUT_SAR_X", 1) / (double)reader.GetIntVariable("OUT_SAR_Y", 1);
                    }

                    int samplerate = reader.Samplerate;
                    if (samplerate == 0 && m.inaudiostreams.Count > 0 && Settings.EnableAudio)
                    {
                        //похоже что звук не декодируется этим декодером
                        throw new Exception("Script doesn't contain audio");
                    }

                    if (samplerate != 0 && (m.inaudiostreams.Count > 0 || instream.samplerate == null))
                    {
                        //вероятно аудио декодер меняет количество каналов
                        if (instream.channels != reader.Channels && instream.channels > 0)
                            instream.badmixing = true;

                        instream.samplerate = samplerate.ToString();
                        instream.bits = reader.BitsPerSample;

                        //Определение продолжительности и числа кадров только для звука (например если исходник - RAW ААС)
                        if (!m.isvideo && m.inframes == 0 && m.induration == TimeSpan.Zero)
                        {
                            m.induration = m.outduration = TimeSpan.FromSeconds(reader.SamplesCount / (double)reader.Samplerate);
                            m.inframes = (int)(m.induration.TotalSeconds * 25);
                        }

                        if (m.inaudiostreams.Count > 0 && instream.bitrate == 0 && (instream.codecshort == "PCM" || instream.codecshort == "LPCM"))
                            instream.bitrate = (reader.BitsPerSample * reader.Samplerate * reader.Channels) / 1000; //kbps

                        //если звук всё ещё не забит
                        if (m.inaudiostreams.Count == 0 && ext == ".avs" && samplerate != 0)
                        {
                            instream.bitrate = (reader.BitsPerSample * reader.Samplerate * reader.Channels) / 1000; //kbps
                            instream.codec = "PCM";
                            instream.codecshort = "PCM";
                            instream.language = "English";
                            m.inaudiostreams.Add(instream.Clone());
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    AviSynthScripting.WriteScriptToFile(script, "error");
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader = null;
                    }
                }
            }                
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                //ShowMessage(Languages.Translate("Don`t stop caching please. It can brake file import!"),
                //    Languages.Translate("Warning"), Message.MessageStyle.Ok);
                //e.Cancel = true;

                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
                m = null;
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
           //выходим при отмене
            if (m == null) return;

            string ext = Path.GetExtension(m.infilepath).ToLower();
            AudioStream instream = (m.inaudiostreams.Count > 0) ? (AudioStream)m.inaudiostreams[m.inaudiostream] : new AudioStream();

            if (error == null)
            {
                Close();
            }
            else if (error == "Script doesn't contain video" || error == "Script doesn't contain audio" || error.StartsWith("DirectShowSource:") ||
                error.Contains("FFmpegSource: Audio decoding error") || error.StartsWith("FFAudioSource:") || error.Contains(" audio track"))
            {
                //Переключение на FFmpegSource при проблемах с DirectShowSource (кроме проблем со звуком)
                //"Script doesn't contain video" - это из AviSynthReader.OpenScript, тут этого не будет, т.к. у нас ParseScript
                if (error.StartsWith("DirectShowSource:") && !error.Contains("unable to determine the duration of the audio.") &&
                    !error.Contains("Timeout waiting for audio.") || error == "Script doesn't contain video")
                    m.vdecoder = AviSynthScripting.Decoders.FFmpegSource;

                if (m.inaudiostreams.Count > 0 && !File.Exists(instream.audiopath))
                {
                    string outext = Format.GetValidRAWAudioEXT(instream.codecshort);
                    string outpath = Settings.TempPath + "\\" + m.key + "_" + m.inaudiostream + outext;

                    //удаляем старый файл
                    SafeDelete(outpath);

                    //извлекаем новый файл
                    if (outext == ".wav")
                    {
                        Decoder dec = new Decoder(m, Decoder.DecoderModes.DecodeAudio, outpath);
                        if (dec.IsErrors)
                        {
                            ShowMessage("Decode to WAV: " + dec.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                            m = null;
                            Close();
                        }
                    }
                    else
                    {
                        Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, outpath);
                        if (dem.IsErrors)
                        {
                            //Вместо вывода сообщения об ошибке тут можно назначить декодирование в WAV, но тогда в режиме Copy будет копироваться WAV..
                            ShowMessage(dem.error_message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                            m = null;
                            Close();
                        }
                    }

                    //проверка на удачное завершение
                    if (File.Exists(outpath) && new FileInfo(outpath).Length != 0)
                    {
                        instream.audiopath = outpath;
                        instream.audiofiles = new string[] { outpath };
                        instream = Format.GetValidADecoder(instream);
                        ((MainWindow)Owner).deletefiles.Add(outpath);
                    }
                    else
                    {
                        instream.audiopath = null;
                        instream.decoder = 0;
                    }
                }
                else if (m.inaudiostreams.Count > 0 && File.Exists(instream.audiopath) && count > 0)
                {
                    //Мы тут дважды, и звуковой файл уже извлечен - пора выходить с ошибкой..
                    ShowMessage("Caching: " + error, Languages.Translate("Error"), Message.MessageStyle.Ok);
                    m = null;
                    Close();
                }

                count += 1;
                error = null;
                worker.RunWorkerAsync();
                return;
            }
            else if (error == "FFmpegSource: Audio codec not found")
            {
                //ситуация когда стоит попробовать декодировать аудио в wav
                Close(); //Позже будет декодирование в WAV
            }
            else if (error == "File could not be opened!" && instream.decoder == AviSynthScripting.Decoders.bassAudioSource)
            {
                instream.decoder = AviSynthScripting.Decoders.FFAudioSource;
                error = null;
                worker.RunWorkerAsync();
                return;
            }
            else if (error.Contains("convertfps") && m.vdecoder == AviSynthScripting.Decoders.DirectShowSource)
            {
                error = null;
                m.isconvertfps = false;
                worker.RunWorkerAsync();
                return;
            }
            else if (error == "Cannot load avisynth.dll")
            {
                string mess = Languages.Translate("AviSynth is not found!") + Environment.NewLine +
                    Languages.Translate("Please install AviSynth 2.5.7 MT or higher.");
                ShowMessage(mess, Languages.Translate("Error"), Message.MessageStyle.Ok);
                error = null;
                m = null;
            }
            //файл плохой надо пересобирать
            else if (error.StartsWith("FFmpegSource: Can't parse Matroska file"))
            {
                Message message = new Message(this);
                message.ShowMessage(Languages.Translate("Matroska file is corrupted! Try repair file?"),
                    Languages.Translate("Question"), Message.MessageStyle.YesNo);

                if (message.result == Message.Result.Yes)
                {
                    m.vdecoder = AviSynthScripting.Decoders.DirectShowSource;
                    instream.decoder = AviSynthScripting.Decoders.DirectShowSource;
                    error = null;

                    //тут необходимо запустить всплывающий процесс пересборки
                    string outpath = Path.GetDirectoryName(m.infilepath) + "\\" +
                        Path.GetFileNameWithoutExtension(m.infilepath) + ".repaired.mkv";
                    Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.RepairMKV, outpath);

                    if (File.Exists(outpath))
                    {
                        string oldpath = Path.GetDirectoryName(m.infilepath) + "\\" +
                        Path.GetFileNameWithoutExtension(m.infilepath) + ".bad.mkv";

                        File.Move(m.infilepath, oldpath);
                        File.Move(outpath, m.infilepath);

                        worker.RunWorkerAsync();
                        return;
                    }
                    else
                    {
                        error = null;
                        m = null;
                    }
                }
                else
                {
                    error = null;
                    m = null;
                }
            }
            else
            {
                ShowMessage("Caching: " + error, Languages.Translate("Error"), Message.MessageStyle.Ok);
                error = null;
                m = null;
            }
            Close();
        }

        internal delegate void MessageDelegate(string data, string title, Message.MessageStyle style);
        private void ShowMessage(string data, string title, Message.MessageStyle style)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(ShowMessage), data, title, style);
            else
            {
                Message mes = new Message(this.Owner);
                mes.ShowMessage(data, title, style);
            }
        }

        private void SafeDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}