using System;
using System.IO;
using System.Net;
using System.Windows;
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

		public Message()
        {
            this.InitializeComponent();
            // Insert code required on object creation below this point.
        }

        public Message(System.Windows.Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
        }

        public void ShowMessage(string text)
        {
           text_message.Content = text;
            mstyle = MessageStyle.Ok;
            btYes.Visibility = Visibility.Hidden;
            btNo.Content = Languages.Translate("OK");
            ShowDialog();
        }

        public void ShowMessage(string text, string title)
        {
            text_message.Content = text;
            this.Title = title;
            mstyle = MessageStyle.Ok;
            SetButtons();
            ShowDialog();
        }

        public void ShowMessage(string text, string title, MessageStyle style)
        {
            text_message.Content = text;
            this.Title = title;
            mstyle = style;
            SetButtons();
            ShowDialog();
        }

        public void ShowMessage(string text, MessageStyle style)
        {
            text_message.Content = text;
            mstyle = style;
            SetButtons();
            ShowDialog();
        }

        public void SetButtons()
        {
            if (mstyle == MessageStyle.YesNo)
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
        }

        private Result _result = Result.No;
        public Result result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
            }
        }

        private MessageStyle _mstyle = MessageStyle.Ok;
        public MessageStyle mstyle
        {
            get
            {
                return _mstyle;
            }
            set
            {
                _mstyle = value;
            }
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

	}
}