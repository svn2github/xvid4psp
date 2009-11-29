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
	public partial class ShowWindow
	{
        public ShowWindow(Window owner, string Title, string Text, FontFamily Font)
        {
            this.Owner = owner;
            this.InitializeComponent();
            this.group_box.Header = this.Title = Title;
            this.text_box.FontFamily = Font;
            this.text_box.Text = Text;
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}