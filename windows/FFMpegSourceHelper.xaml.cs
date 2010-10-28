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
    public partial class FFMpegSourceHelper
    {
        private BackgroundWorker worker = null;
        public Massive m;
        AviSynthReader reader;
        public bool IsErrors = false;
        public string error_message = null;

        public FFMpegSourceHelper(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = mass.owner;
            m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            text_info.Content = Languages.Translate("Please wait... Work in progress...");

            if (Settings.FFmpegSource2)
            {
                Title = "FFmpegSource2";
                text_info.ToolTip = Languages.Translate("Indexing") + "...";
            }
            else
            {
                Title = "FFmpegSource";
                text_info.ToolTip = Languages.Translate("FFmpegSource creates CACHE files. It can take long time and hard drive space.")/*+
                    Environment.NewLine + Languages.Translate("Use other decoders for more fast import:") + Environment.NewLine +
                    Languages.Translate("FFmpegSource - slow, but safe and codec independed import.") + Environment.NewLine +
                    Languages.Translate("AVISource and DirectShowSource - fast, but depend on system codecs import.")*/;
            }

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
            try
            {
                //проверяем насколько успешно декодируется видео
                string script = AviSynthScripting.GetFramerateScript(m);

                try
                {
                    reader = new AviSynthReader();
                    reader.ParseScript(script);
                }
                catch (Exception ex)
                {
                    //FFmpegSource: Audio decoding error                                       - FFMS1 A
                    //FFmpegSource: Audio codec not found                                      - FFMS1 A
                    //FFmpegSource: Video track is unseekable                                  - FFMS1 A
                    //FFmpegSource: Selected track is not audio                                - FFMS1 A
                    //FFmpegSource: Invalid audio track number                                 - FFMS1 
                    //FFmpegSource: Can't create decompressor: Unsupported compression method. - FFMS1
                    //FFmpegSource: Couldn't open                                              - FFMS1
                    //FFmpegSource: Video codec not found                                      - FFMS1
                    //FFVideoSource: Video codec not found                                     - FFMS1/2
                    //FFAudioSource: Out of bounds track index selected                        - FFMS2
                    //FFAudioSource: No audio track found                                      - FFMS2 A
                    //FFVideoSource: The index does not match the source file                  - FFMS2
                    //FFVideoSource: Could not open video codec                                - FFMS2
                    //FFVideoSource: No video track found                                      - FFMS2
                    //FFVideoSource: Can't open file                                           - FFMS2
                    //FFIndex: Can't open file                                                 - FFMS2
                    //FF...
                    error_message = ex.Message;
                    if (error_message.StartsWith("FFAudioSource:")) IsErrors = false;
                    else if (error_message.StartsWith("FFmpegSource: Audio")) IsErrors = false;
                    else if (error_message.StartsWith("FFmpegSource: Selected track is not audio")) IsErrors = false;
                    else if (error_message.StartsWith("FFmpegSource: Invalid audio track number")) IsErrors = false;
                    else if (error_message.StartsWith("FFmpegSource: Video codec not found")) IsErrors = false;
                    else IsErrors = true;
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
            catch (Exception ex)
            {
                ShowMessage(ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
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
    }
}