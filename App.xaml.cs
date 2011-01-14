using System;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace XviD4PSP
{
	public partial class App: System.Windows.Application
	{
        private bool tasks_saved = false;

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
            string path = "";
            string txt = "An unhandled exception has occurred in XviD4PSP";
            bool log_saved = false;

            try
            {
                path = Settings.TempPath + "\\exception_info";
                path += "_(" + DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss") + ").log";
                txt += " v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch { }

            Exception ex = (obj as Exception);
            if (ex != null)
            {
                txt += "!\r\n\r\nSource:\r\n  " + ex.Source + "\r\n\r\nException:\r\n  " + ex.Message + "\r\n\r\nStack trace:\r\n" + ex.StackTrace +
                    (ex.InnerException != null ? "\r\n\r\nInner Exception:\r\n  " + ex.InnerException : "");
            }
            else
                txt += "!\r\n\r\n" + obj.ToString();

            try
            {
                File.WriteAllText(path, txt, System.Text.Encoding.Default);
                log_saved = true;
            }
            catch { }

            if (!tasks_saved)
            {
                //Это не для того, чтобы знать, сохранили мы задания или нет.
                //Это чтобы ограничиться одним разом (при множественных срабатываниях).
                tasks_saved = true;
                try
                {
                    if (App.Current.MainWindow.IsLoaded)
                    {
                        //Сохраняем (обновляем) файл с заданиями
                        ((MainWindow)App.Current.MainWindow).UpdateTasksBackup();
                    }
                }
                catch { }
            }

            MessageBox.Show(txt + (log_saved ? "\r\n\r\nThis log was saved here:\r\n  " + path + "\r\n" : ""),
                "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        private bool CopyToClipboard(string text)
        {
            bool ok = false;
            if (OpenClipboard(IntPtr.Zero))
            {
                IntPtr pText = IntPtr.Zero;
                try
                {
                    if (EmptyClipboard())
                    {
                        pText = Marshal.StringToHGlobalUni(text);           //StringToHGlobalAnsi
                        ok = (SetClipboardData(13, pText) != IntPtr.Zero ); //1 - CF_TEXT, 13 - CF_UNICODETEXT
                    }
                }
                finally
                {
                    CloseClipboard();
                    if (!ok && pText != IntPtr.Zero)
                        Marshal.FreeHGlobal(pText);
                }
            }
            return ok;
        }

        private void Copy_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            try
            {
                //Выходим на TextBox
                TextBox MyTextBox;
                if ((MyTextBox = sender as TextBox) == null)
                {
                    ContextMenu cm = (ContextMenu)((MenuItem)sender).Parent;
                    MyTextBox = (TextBox)cm.PlacementTarget;
                    cm.IsOpen = false;
                    e.Handled = true;
                }

                if (MyTextBox.IsFocused && !string.IsNullOrEmpty(MyTextBox.SelectedText))
                {
                    CopyToClipboard(MyTextBox.SelectedText);
                }
            }
            catch { }
        }

        private void Cut_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            try
            {
                //Выходим на TextBox
                TextBox MyTextBox;
                if ((MyTextBox = sender as TextBox) == null)
                {
                    ContextMenu cm = (ContextMenu)((MenuItem)sender).Parent;
                    MyTextBox = (TextBox)cm.PlacementTarget;
                    cm.IsOpen = false;
                    e.Handled = true;
                }

                if (MyTextBox.IsFocused && !string.IsNullOrEmpty(MyTextBox.SelectedText))
                {
                    if (CopyToClipboard(MyTextBox.SelectedText))
                    {
                        //Вырезаем текст
                        int index = MyTextBox.CaretIndex; //MyTextBox.SelectionStart
                        MyTextBox.Text = MyTextBox.Text.Remove(index, MyTextBox.SelectionLength);
                        MyTextBox.CaretIndex = index;
                    }
                }
            }
            catch { }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.C)
                {
                    e.Handled = true;
                    Copy_PreviewMouseUp(sender, null);
                }
                else if (e.Key == Key.X)
                {
                    e.Handled = true;
                    Cut_PreviewMouseUp(sender, null);
                }
            }
        }
    }
}
