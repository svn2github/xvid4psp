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
using System.Windows.Interop;
using System.Collections;

namespace XviD4PSP
{
    public partial class Autocrop
    {
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthReader reader = null;
        private IntPtr Handle = IntPtr.Zero;
        private bool IsError = false;
        private int num_closes = 0;
        private int frame = -1;
        private string script;
        public Massive m;

        public enum AutocropMode { AllFiles = 1, Disabled, MPEGOnly }

        public Autocrop()
        {
            this.InitializeComponent();
        }

        public Autocrop(Massive mass, System.Windows.Window owner, int frame)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.m = mass.Clone();
            this.frame = frame;

            //забиваем
            prCurrent.Maximum = 100;
            Title = Languages.Translate("Detecting black borders") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, ref Handle, false);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (!IsError)
                Win7Taskbar.SetProgressIndeterminate(this, ref Handle);
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (((BackgroundWorker)sender).WorkerReportsProgress)
            {
                if (prCurrent.IsIndeterminate)
                {
                    prCurrent.IsIndeterminate = false;
                    label_info.Content = Languages.Translate("Detecting black borders") + "...";
                }

                prCurrent.Value = e.ProgressPercentage;
                Title = "(" + e.ProgressPercentage.ToString("0") + "%)";

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), 100);
            }
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //Готовим скрипт
                script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Autocrop);
                script = AviSynthScripting.TuneAutoCropScript(script, frame);

                //Открываем скрипт
                reader = new AviSynthReader();
                reader.ParseScript(script);

                int width = m.inresw;
                int height = m.inresh;
                int frames = reader.FrameCount;

                ArrayList ll = new ArrayList();
                ArrayList tt = new ArrayList();
                ArrayList rr = new ArrayList();
                ArrayList bb = new ArrayList();

                //Проигрываем все кадры и считываем значения кропа
                for (int i = 0; i < frames && !worker.CancellationPending; i++)
                {
                    reader.ReadFrameDummy(i);
                    worker.ReportProgress(((i + 1) * 100) / frames);

                    //Фильтрация возможных(?) недопустимых значений
                    ll.Add(Math.Min(Math.Max(reader.GetVarInteger("crop_left", 0), 0), width));
                    tt.Add(Math.Min(Math.Max(reader.GetVarInteger("crop_top", 0), 0), height));
                    rr.Add(Math.Min(Math.Max(reader.GetVarInteger("crop_right", 0), 0), width));
                    bb.Add(Math.Min(Math.Max(reader.GetVarInteger("crop_bottom", 0), 0), height));
                }

                //Анализ полученного
                if (!worker.CancellationPending)
                {
                    if (ll.Count > 0)
                    {
                        bool new_mode = Settings.AutocropMostCommon;

                        //Ищем наиболее часто встречающиеся значения ("усредняем") или берём минимальные значения
                        m.cropl = m.cropl_copy = (ll.Count > 4 && new_mode) ? FindMostCommon(ll) : FindMinimum(ll);  //Слева
                        m.cropt = m.cropt_copy = (tt.Count > 4 && new_mode) ? FindMostCommon(tt) : FindMinimum(tt);  //Сверху
                        m.cropr = m.cropr_copy = (rr.Count > 4 && new_mode) ? FindMostCommon(rr) : FindMinimum(rr);  //Справа
                        m.cropb = m.cropb_copy = (bb.Count > 4 && new_mode) ? FindMostCommon(bb) : FindMinimum(bb);  //Снизу
                    }
                    else
                    {
                        m.cropl = m.cropl_copy = 0;  //Слева
                        m.cropt = m.cropt_copy = 0;  //Сверху
                        m.cropr = m.cropr_copy = 0;  //Справа
                        m.cropb = m.cropb_copy = 0;  //Снизу
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

        private int FindMostCommon(ArrayList values)
        {
            //Перебираем все значения в ArrayList..
            int[] counts = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                //..и считаем,..
                int count = 0;
                foreach (int value in values)
                {
                    //..сколько раз каждое из них там встречается
                    if (value == (int)values[i])
                        count += 1;
                }
                counts[i] = count;
            }

            //Определяем индекс значения, которое встречается чаще всего
            int index = 0, max = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > max)
                {
                    max = counts[i];
                    index = i;
                }
            }

            //Если этих значений больше 50% от общего числа..
            if ((max * 100) / values.Count > 50)
            {
                //..то выдаем само это значение
                return (int)values[index];
            }
            else
            {
                //..иначе выдаем наименьшее значение (т.к. видимо много мусора)
                return FindMinimum(values);
            }
        }

        private int FindMinimum(ArrayList list)
        {
            int min = int.MaxValue;
            foreach (int value in list)
            {
                if (value < min)
                    min = value;
            }
            return min;
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

                worker.WorkerReportsProgress = false;
                label_info.Content = Languages.Translate("Aborting... Please wait...");
                Win7Taskbar.SetProgressState(Handle, TBPF.INDETERMINATE);
                prCurrent.IsIndeterminate = true;
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
                ErrorException("AutoCrop (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
            }
            else if (e.Result != null)
            {
                m = null;

                //Добавляем скрипт в StackTrace
                string stacktrace = ((Exception)e.Result).StackTrace;
                if (!string.IsNullOrEmpty(((Exception)e.Result).HelpLink))
                    stacktrace += Calculate.WrapScript(((Exception)e.Result).HelpLink, 150);

                ErrorException("AutoCrop: " + ((Exception)e.Result).Message, stacktrace);
            }

            Close();
        }

        internal delegate void ErrorExceptionDelegate(string data, string info);
        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ErrorExceptionDelegate(ErrorException), data, info);
            else
            {
                IsError = true;
                if (worker != null) worker.WorkerReportsProgress = false;
                if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressTaskComplete(Handle, TBPF.ERROR);

                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}