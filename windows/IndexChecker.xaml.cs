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
	public partial class IndexChecker
	{
        private BackgroundWorker worker = null;
        public Massive m;
        AviSynthReader reader;

        public IndexChecker(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = mass.owner;

            m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;

            Title = Languages.Translate("Checking index folder") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //фоновое кодирование
            CreateBackgoundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void CreateBackgoundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //забиваем необходимые параметры
                MediaInfoWrapper media = new MediaInfoWrapper();
                media.Open(m.infilepath);
                m.invcodec = media.VCodecString;
                m.invcodecshort = media.VCodecShort;
                //Добавляем инфу, по которой потом можно будет определить, применять ли ForceFilm
                m.inframerate = media.FrameRate;
                if (media.ScanOrder.Contains("Pulldown")) m.IsPullDown = true;
                media.Close();

                //проверка на невалидную индексацию
                if (m.invcodecshort != "MPEG2" && m.invcodecshort != "MPEG1")
                {
                    //m.vdecoder = AviSynthScripting.Decoders.DirectShowSource;
                    return; //Просто выходим отсюда, декодер будет выбран позже (Settings.OtherDecoder)
                }

                //получаем индекс файл
                m.indexfile = Calculate.GetBestIndexFile(m.infilepath);

                //определяем видео декодер
                m = Format.GetValidVDecoder(m);

                if (File.Exists(m.indexfile))
                {
                    //проверяем папки
                    string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Info);
                    reader = new AviSynthReader();
                    reader.ParseScript(script);
                    m.induration = TimeSpan.FromSeconds((double)reader.FrameCount / reader.Framerate);

                    //проверка на устаревшую индекс папку
                    string ifopath = Calculate.GetIFO(m.infilepath);
                    if (File.Exists(ifopath))
                    {
                        VStripWrapper vs = new VStripWrapper();
                        vs.Open(ifopath);
                        TimeSpan duration = vs.Duration();
                        vs.Close();

                        //папка устарела
                        if ((long)m.induration.Duration().TotalSeconds != (long)duration.TotalSeconds)
                        {
                            try
                            {
                                Directory.Delete(Path.GetDirectoryName(m.indexfile), true);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Проблемы при открытии существующего d2v-файла (возможно это кэш от другого файла, уже не существующего)
                if (ex.Message.StartsWith("MPEG2Source"))
                {
                    try
                    {
                        Directory.Delete(Path.GetDirectoryName(m.indexfile), true);
                    }
                    catch { }
                }
                else
                {
                    m = null;
                    ShowMessage(ex.Message, Languages.Translate("Error"), Message.MessageStyle.Ok);
                }
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
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