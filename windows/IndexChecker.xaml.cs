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
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthReader reader = null;
        private int num_closes = 0;
        private bool NV = false;
        private string script;
        public Massive m;

        public IndexChecker(Massive mass, bool NV)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();
            this.NV = NV;

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Checking index folder") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
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
            try
            {
                //забиваем необходимые параметры
                MediaInfoWrapper media = new MediaInfoWrapper();
                media.Open(m.infilepath);
                m.invcodec = media.VCodecString;
                m.invcodecshort = media.VCodecShort;
                //Добавляем инфу, по которой потом можно будет определить, применять ли ForceFilm
                m.inframerate = media.FrameRate;
                bool pulldown = media.ScanOrder.Contains("Pulldown");
                media.Close();

                //Выходим при отмене
                if (worker.CancellationPending) return;

                //проверка на невалидную индексацию
                if (!NV && m.invcodecshort != "MPEG2" && m.invcodecshort != "MPEG1")
                {
                    //Выходим отсюда, декодер будет выбран позже
                    m.indexfile = null;
                    m.vdecoder = 0;
                    return;
                }

                //получаем индекс файл
                m.indexfile = Calculate.GetBestIndexFile(m.infilepath, NV);

                //определяем видео декодер
                if (NV)
                {
                    m.vdecoder = AviSynthScripting.Decoders.DGSource;
                    m.dgdecnv_path = Calculate.StartupPath + "\\apps\\DGDecNV\\";
                }
                else
                    m.vdecoder = AviSynthScripting.Decoders.MPEG2Source;

                if (File.Exists(m.indexfile) && !worker.CancellationPending)
                {
                    //Определяем, использовался ли Force Film (#1)
                    //Для DGSource ForceFilm задается через скрипт, а не через правку индекс-файла,
                    //его в любой момент можно вкл\выкл, поэтому тут нужно определиться, использовать ли ForceFilm.
                    if (m.vdecoder == AviSynthScripting.Decoders.DGSource)
                    {
                        if (Indexing_DGIndexNV.CheckIndexAndForceFilm(m.indexfile, m.inframerate))
                        {
                            m.IsForcedFilm = true;
                            m.interlace = SourceType.UNKNOWN;
                        }
                    }

                    //проверяем папки
                    script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Info);
                    reader = new AviSynthReader();
                    reader.ParseScript(script);
                    m.induration = TimeSpan.FromSeconds((double)reader.FrameCount / reader.Framerate);

                    //Определяем, использовался ли Force Film (#2)
                    //Для MPEG2Source ForceFilm задается через правку индекс-файла,
                    //поэтому изменив его однажды (при индексации) ForceFilm всегда будет вкл.
                    if (m.vdecoder == AviSynthScripting.Decoders.MPEG2Source)
                    {
                        if ((pulldown && m.inframerate == "23.976" || m.inframerate == "29.970") && Math.Abs(reader.Framerate - 23.976) < 0.001)
                        {
                            m.IsForcedFilm = true;
                            m.interlace = SourceType.UNKNOWN;
                        }
                    }

                    //Закрываем ридер
                    CloseReader(true);

                    //проверка на устаревшую индекс папку
                    string ifopath = Calculate.GetIFO(m.infilepath);
                    if (File.Exists(ifopath) && !worker.CancellationPending)
                    {
                        VStripWrapper vs = new VStripWrapper();
                        vs.Open(ifopath);
                        TimeSpan duration = vs.Duration();
                        vs.Close();

                        //папка устарела (если разница между продолжительностью в скрипте и в IFO больше 10-ти секунд)
                        if (Math.Abs(m.induration.Duration().TotalSeconds - duration.TotalSeconds) > 10)
                        {
                            //Будем папку удалять..
                            throw new Exception("MPEG2Source");
                        }
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

                label_info.Content = Languages.Translate("Aborting... Please wait...");
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
                    if (_null) reader = null;
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                m = null;
                ErrorException("IndexChecker (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                Exception ex = (Exception)e.Result;

                //Проблемы при открытии существующего индекс-файла (возможно это кэш от другого файла, уже не существующего)
                if (ex.Message.StartsWith("MPEG2Source") || ex.Message.StartsWith("DGSource: Could not open one of the input files"))
                {
                    SafeDirDelete(Path.GetDirectoryName(m.indexfile));
                }
                else
                {
                    m = null;

                    //Добавляем в StackTrace текущий скрипт
                    string stacktrace = ex.StackTrace;
                    if (!string.IsNullOrEmpty(ex.HelpLink))
                        stacktrace += Calculate.WrapScript(ex.HelpLink, 150);

                    ErrorException("IndexChecker: " + ex.Message, stacktrace);
                }
            }

            Close();
        }

        private void SafeDirDelete(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;

                //Удаляем все файлы, после чего саму папку..
                foreach (string file in Directory.GetFiles(dir))
                    File.Delete(file);

                //..если в ней нет никаких подпапок
                if (Directory.GetDirectories(dir).Length == 0)
                    Directory.Delete(dir, false);
            }
            catch (Exception) //ex)
            {
                //ShowErrorMessage("SafeDirDelete: " + ex.Message, ex.StackTrace);
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