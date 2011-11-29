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
using System.Text.RegularExpressions;

namespace XviD4PSP
{
    public partial class Caching
    {
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthReader reader = null;
        private int num_closes = 0;
        private int counter = 0;
        public Massive m;

        public Caching(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title =  Languages.Translate("Caсhing") + "...";
            text_info.Content = Languages.Translate("Please wait... Work in progress...");

            //Caching
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
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //Выходим при отмене
            if (m == null || worker.CancellationPending) return;

            string script = "";
            try
            {
                string ext = Path.GetExtension(m.infilepath).ToLower();

                //получаем инфу из простого avs
                script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Info);

                reader = new AviSynthReader();
                reader.ParseScript(script);

                //Выходим при отмене
                if (m == null || worker.CancellationPending) return;

                AudioStream instream = (m.inaudiostreams.Count > 0) ? (AudioStream)m.inaudiostreams[m.inaudiostream] : new AudioStream();

                if (reader.Framerate != Double.PositiveInfinity && reader.Framerate != 0.0)
                {
                    m.induration = TimeSpan.FromSeconds((double)reader.FrameCount / reader.Framerate);
                    m.outduration = m.induration;
                    m.inframes = reader.FrameCount;
                    if (string.IsNullOrEmpty(m.inframerate))
                        m.inframerate = Calculate.ConvertDoubleToPointString(reader.Framerate);
                }

                if (m.isvideo && ext != ".avs" && (reader.Width == 0 || reader.Height == 0))
                    throw new Exception(m.vdecoder.ToString() + " can`t decode video (zero-size image was returned)!");
                else
                {
                    m.inresw = reader.Width;
                    m.inresh = reader.Height;

                    if (m.inaspect == 0 || double.IsNaN(m.inaspect))
                        m.inaspect = (double)m.inresw / (double)m.inresh;

                    if (ext == ".avs")
                    {
                        //Считываем SAR из скрипта
                        m.pixelaspect = (double)reader.GetIntVariable("OUT_SAR_X", 1) / (double)reader.GetIntVariable("OUT_SAR_Y", 1);
                    }
                }

                int samplerate = reader.Samplerate;
                if (samplerate == 0 && m.inaudiostreams.Count > 0 && Settings.EnableAudio)
                {
                    //похоже что звук не декодируется этим декодером
                    throw new Exception("Script doesn't contain audio");
                }

                if (samplerate != 0 && (m.inaudiostreams.Count > 0 || instream.samplerate == null))
                {
                    if (instream.channels > 0)
                    {
                        //вероятно аудио декодер меняет количество каналов
                        if (instream.channels != reader.Channels)
                            instream.badmixing = true;
                    }
                    else
                        instream.channels = reader.Channels;

                    instream.samplerate = samplerate.ToString();
                    instream.bits = reader.BitsPerSample;

                    //Определение продолжительности и числа кадров только для звука (например, если исходник - RAW ААС)
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
                        instream.codec = instream.codecshort = "PCM";
                        instream.language = "Unknown";
                        m.inaudiostreams.Add(instream.Clone());
                    }
                }
            }
            catch (Exception ex)
            {
                if (worker != null && !worker.CancellationPending && m != null && num_closes == 0)
                {
                    //Ошибка
                    ex.HelpLink = script;
                    e.Result = ex;

                    try
                    {
                        //записываем скрипт с ошибкой в файл
                        AviSynthScripting.WriteScriptToFile(script, "error");
                    }
                    catch (Exception) { }
                }
            }
            finally
            {
                CloseReader(true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel_closing = false;

            if (worker != null)
            {
                if (worker.IsBusy && num_closes < 5)
                {
                    //Отмена
                    cancel_closing = true;
                    worker.CancelAsync();
                    num_closes += 1;
                    m = null;
                }
                else
                {
                    worker.Dispose();
                    worker = null;
                }
            }

            //Отменяем закрытие окна
            if (cancel_closing)
            {
                //CloseReader(false);

                text_info.Content = Languages.Translate("Aborting... Please wait...");
                e.Cancel = true;
            }
            else
                CloseReader(true);
        }

        private void CloseReader(bool _null)
        {
            lock (locker)
            {
                if (reader != null)
                {
                    reader.Close();
                    if (_null) 
                        reader = null;
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //Выходим при отмене
            if (m == null || worker == null || num_closes > 0)
            {
                Close();
                return;
            }

            if (e.Error != null)
            {
                ErrorException("Caching (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
                m = null;
                Close();
                return;
            }

            //Всё OK - выходим
            if (e.Result == null)
            {
                Close();
                return;
            }

            //Извлекаем текст ошибки
            string error = ((Exception)e.Result).Message.Trim();
            string stacktrace = ((Exception)e.Result).StackTrace;
            if (!string.IsNullOrEmpty(((Exception)e.Result).HelpLink))
            {
                //Добавляем скрипт в StackTrace
                stacktrace += Calculate.WrapScript(((Exception)e.Result).HelpLink, 150);
            }

            string ext = Path.GetExtension(m.infilepath).ToLower();
            AudioStream instream = (m.inaudiostreams.Count > 0) ? (AudioStream)m.inaudiostreams[m.inaudiostream] : new AudioStream();

            //Ошибка в пользовательском скрипте или в графе
            if (ext == ".avs" || ext == ".grf")
            {
                ErrorException("Caching: " + error, stacktrace);
                m = null;
                Close();
                return;
            }

            //Начался разбор ошибок
            if ((error == "Script doesn't contain audio" || error.StartsWith("DirectShowSource:") ||
                error.StartsWith("FFAudioSource:") || error.Contains(" audio track")) && m.isvideo)
            {
                bool demux_audio = true;

                //Переключение на FFmpegSource2 при проблемах с DirectShowSource (кроме проблем со звуком)
                if (error.StartsWith("DirectShowSource:") && !error.Contains("unable to determine the duration of the audio.") &&
                    !error.Contains("Timeout waiting for audio."))
                {
                    demux_audio = !Settings.FFMS_Enable_Audio;
                    m.vdecoder = AviSynthScripting.Decoders.FFmpegSource2;

                    //И тут-же индексируем (чтоб не делать это через Ависинт)
                    Indexing_FFMS ffindex = new Indexing_FFMS(m);
                    if (ffindex.m == null)
                    {
                        m = null;
                        Close();
                        return;
                    }
                }

                //Извлечение звука
                if (m.inaudiostreams.Count > 0 && demux_audio && !File.Exists(instream.audiopath))
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
                            ErrorException("Decode to WAV: " + dec.error_message, null);
                            m = null;
                            Close();
                            return;
                        }
                    }
                    else
                    {
                        Demuxer dem = new Demuxer(m, Demuxer.DemuxerMode.ExtractAudio, outpath);
                        if (dem.IsErrors)
                        {
                            //Вместо вывода сообщения об ошибке тут можно назначить декодирование в WAV, но тогда в режиме Copy будет копироваться WAV..
                            ErrorException(dem.error_message, null);
                            m = null;
                            Close();
                            return;
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
                else if (m.inaudiostreams.Count > 0 && (!demux_audio || File.Exists(instream.audiopath)) && counter > 0)
                {
                    //Мы тут уже были - пора выходить с ошибкой..
                    ErrorException("Caching: " + error, stacktrace);
                    m = null;
                    Close();
                    return;
                }

                counter += 1;
                worker.RunWorkerAsync();
                return;
            }
            else if (error == "File could not be opened!" && instream.decoder == AviSynthScripting.Decoders.bassAudioSource)
            {
                instream.decoder = AviSynthScripting.Decoders.FFAudioSource;
                worker.RunWorkerAsync();
                return;
            }
            else if (error.Contains("convertfps") && m.vdecoder == AviSynthScripting.Decoders.DirectShowSource)
            {
                m.isconvertfps = false;
                worker.RunWorkerAsync();
                return;
            }
            else if (error == "Cannot load avisynth.dll")
            {
                string mess = Languages.Translate("AviSynth is not found!") + Environment.NewLine +
                    Languages.Translate("Please install AviSynth 2.5.7 MT or higher.");
                ErrorException(mess, null);
                m = null;
            }
            else
            {
                ErrorException("Caching: " + error, stacktrace);
                m = null;
            }

            Close();
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
                ErrorException("SafeFileDelete: " + ex.Message, ex.StackTrace);
            }
        }

        internal delegate void ErrorExceptionDelegate(string data, string info);
        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ErrorExceptionDelegate(ErrorException), data, info);
            else
            {
                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}