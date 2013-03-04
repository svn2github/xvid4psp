using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace XviD4PSP
{
    public partial class Message
    {
        public enum MessageStyle { Ok = 1, YesNo, OkCancel };
        public enum Result { Yes = 1, No, Ok, Cancel };

        public Result result = Result.No;
        private MessageStyle mstyle = MessageStyle.Ok;

        public Message()
        {
            this.InitializeComponent();
            // Insert code required on object creation below this point.
        }

        public Message(System.Windows.Window owner)
        {
            this.InitializeComponent();
            if (owner != null && owner.IsVisible) this.Owner = owner;
            else if (App.Current.MainWindow.IsVisible) this.Owner = App.Current.MainWindow;
            else this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, true);
        }

        public void ShowMessage(string text)
        { SetUpWindow(text, null, "Message", MessageStyle.Ok); }

        public void ShowMessage(string text, string title)
        { SetUpWindow(text, null, title, MessageStyle.Ok); }

        public void ShowMessage(string text, string title, MessageStyle style)
        { SetUpWindow(text, null, title, style); }

        public void ShowMessage(string text, MessageStyle style)
        { SetUpWindow(text, null, "Message", style); }

        public void ShowMessage(string text, string info, string title)
        { SetUpWindow(text, info, title, MessageStyle.Ok); }

        public void ShowMessage(string text, string info, string title, MessageStyle style)
        { SetUpWindow(text, info, title, style); }

        //Выводим инфу в окно
        private void SetUpWindow(string text, string info, string title, MessageStyle style)
        {
            this.Title = title;
            text_message.Text = text;
            if (!string.IsNullOrEmpty(info))
            {
                text_info.Text = info;
                btInfo.Visibility = Visibility.Visible;
            }

            mstyle = style;
            if (mstyle == MessageStyle.Ok)
            {
                btYes.Visibility = Visibility.Hidden;
                btNo.Content = Languages.Translate("OK");
            }
            else if (mstyle == MessageStyle.YesNo)
            {
                btYes.Content = Languages.Translate("Yes");
                btNo.Content = Languages.Translate("No");
            }
            else if (mstyle == MessageStyle.OkCancel)
            {
                btYes.Content = Languages.Translate("OK");
                btNo.Content = Languages.Translate("Cancel");
            }
            else
            {
                btYes.Visibility = Visibility.Hidden;
                btNo.Content = Languages.Translate("OK");
            }

            cm_copy.Header = Languages.Translate("Copy");
            ShowDialog();
        }

        private void btYes_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mstyle == MessageStyle.OkCancel)
                result = Result.Ok;
            else if (mstyle == MessageStyle.YesNo)
                result = Result.Yes;
            else
                result = Result.Ok;
            this.Close();
        }

        private void btNo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mstyle == MessageStyle.OkCancel)
                result = Result.Cancel;
            else if (mstyle == MessageStyle.YesNo)
                result = Result.No;
            else
                result = Result.Ok;
            this.Close();
        }

        private void btInfo_Click(object sender, RoutedEventArgs e)
        {
            if (text_info.Visibility == Visibility.Collapsed)
            {
                text_info.Visibility = Visibility.Visible;
                Calculate.CheckWindowPos(this, true);
            }
            else
                text_info.Visibility = Visibility.Collapsed;
        }

        private void cm_copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Win32.CopyToClipboard(text_message.Text + ((btInfo.Visibility == Visibility.Visible) ? "\r\n\r\n" + text_info.Text : ""));
            }
            catch (Exception) { }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                cm_copy_Click(null, null);
        }

        private void LayoutRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}