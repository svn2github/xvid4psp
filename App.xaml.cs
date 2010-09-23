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

namespace XviD4PSP
{
	public partial class App: System.Windows.Application
	{
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
