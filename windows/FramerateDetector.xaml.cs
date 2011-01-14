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
    public partial class FramerateDetector
    {
        private BackgroundWorker worker = null;
        public Massive m;
        AviSynthReader reader;

        public FramerateDetector(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //забиваем
            prCurrent.Maximum = 100;
            Title =  Languages.Translate("Detecting framerate") + "...";
            text_info.Content = Languages.Translate("Please wait... Work in progress...");

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
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {

        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                //если всё ещё не получилось используем DS
                if (m.inframerate == "")
                {
                   m.vdecoder = AviSynthScripting.Decoders.DirectShowSource;

                    string script = AviSynthScripting.GetFramerateScript(m);

                    try
                    {
                        reader = new AviSynthReader();
                        reader.ParseScript(script);

                        if (m != null)
                        {
                            if (reader.Framerate != Double.PositiveInfinity)
                            {
                                m.inframerate = Calculate.ConvertDoubleToPointString(reader.Framerate);
                                double milliseconds = Convert.ToInt32((double)reader.FrameCount / reader.Framerate) * 1000;
                                m.induration = TimeSpan.FromMilliseconds(milliseconds);
                                m.outduration = m.induration;
                                m.inframes = reader.FrameCount;
                                //m.outframerate = Calculate.ConvertDoubleToPointString(reader.Framerate);
                            }
                        }
                        reader.Close();
                        reader = null;
                    }
                    catch (Exception)
                    {
                        reader.Close();
                        reader = null;
                    }
                }

                //если всё ещё не получилось используем FF
                if (m.inframerate == "")
                {
                    m.vdecoder = AviSynthScripting.Decoders.FFmpegSource;

                    string script = AviSynthScripting.GetFramerateScript(m);

                    try
                    {
                        reader = new AviSynthReader();
                        reader.ParseScript(script);

                        if (m != null)
                        {
                            if (reader.Framerate != Double.PositiveInfinity)
                                m.inframerate = Calculate.ConvertDoubleToPointString(reader.Framerate);
                        }
                        reader.Close();
                        reader = null;
                    }
                    catch (Exception)
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

        internal delegate void StatusDelegate(string title, string pr_text, double pr_c);
        private void SetStatus(string title, string pr_text, double pr_c)
        {
            if (m != null)
            {
                try
                {
                    if (!Application.Current.Dispatcher.CheckAccess())
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new StatusDelegate(SetStatus), title, pr_text, pr_c);
                    else
                    {
                        //this.Title = title;
                        this.prCurrent.Value = pr_c;
                    }
                }
                catch
                {
                }
            }
        }

    }
}