using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.Windows.Threading;
using System.ComponentModel;

namespace XviD4PSP
{
    public partial class Shutdown
    {
        //[DllImport("Powrprof.dll")]
        //public static extern bool SetSuspendState(bool Hibernate, bool ForceCritical, bool DisableWakeEvent);

        public enum ShutdownMode { Wait = 1, Standby, Hibernate, Shutdown, Exit }
        private int seconds = 20;
        private ShutdownMode mode;
        private string warning_message;
        private BackgroundWorker worker = null;
        private bool IsCanceled = false;

        public Shutdown(System.Windows.Window owner, ShutdownMode mode)
        {
            this.InitializeComponent();
            if (owner.IsVisible) this.Owner = owner;
            else if (App.Current.MainWindow.IsVisible) this.Owner = App.Current.MainWindow;
            else this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.mode = mode;

            button_cancel.Content = Languages.Translate("Cancel");
            warning_message = Languages.Translate("System will be shutdown! 20 seconds left!");
            text_message.Content = warning_message;
            Title = Languages.Translate("Shutdown");

            //фоновые процессы
            CreateBackgoundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        private void CreateBackgoundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (!IsCanceled && seconds != 0)
            {
                SetMessage();
                Thread.Sleep(1000);
                seconds--;
            }
        }

        private void worker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (!IsCanceled)
            {
                if (mode == ShutdownMode.Shutdown)
                {
                    ((MainWindow)App.Current.MainWindow).IsExiting = true;
                    PowerManager powerManager = new PowerManager();
                    powerManager.PowerOffComputer(false);
                }
                else if (mode == ShutdownMode.Hibernate)
                {
                    PowerManager powerManager = new PowerManager();
                    powerManager.HibernateComputer(false);
                }
                else if (mode == ShutdownMode.Standby)
                {
                    PowerManager powerManager = new PowerManager();
                    powerManager.StandbyComputer(true, false);
                }
            }

            Close();
        }

        internal delegate void MessageDelegate();
        private void SetMessage()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new MessageDelegate(SetMessage));
            else
            {
                string _warning_message = warning_message.Replace("20", seconds.ToString());
                text_message.Content = _warning_message;
            }
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsCanceled = true;
        }
    }
}