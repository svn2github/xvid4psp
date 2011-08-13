using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace WPF_VideoPlayer
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //Ловим необработанные исключения
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Current_DispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ShowExceptionInfo(e.Exception);

            //e.Handled = true;
            //App.Current.Shutdown(1);
            AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowExceptionInfo(e.ExceptionObject);
        }

        private void ShowExceptionInfo(object obj)
        {
            string txt = "An unhandled exception has occurred in WPF Video Player";

            Exception ex = (obj as Exception);
            if (ex != null)
            {
                txt += "!\r\n\r\nSource:\r\n  " + ex.Source + "\r\n\r\nException:\r\n  " + ex.Message + "\r\n\r\nStack trace:\r\n" + ex.StackTrace +
                    (ex.InnerException != null ? "\r\n\r\nInner Exception:\r\n  " + ex.InnerException : "");
            }
            else
                txt += "!\r\n\r\n" + obj.ToString();

            MessageBox.Show(txt, "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
    }
}