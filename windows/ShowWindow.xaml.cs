using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Input;

namespace XviD4PSP
{
    public partial class ShowWindow
    {
        private string last_search = "";
        private int last_search_index = 0;

        public ShowWindow(Window owner, string Title, string Text, FontFamily Font)
        {
            this.Owner = owner;
            this.InitializeComponent();
            this.group_box.Header = this.Title = Title;
            this.text_box.FontFamily = Font;
            this.text_box.Text = Text;
            this.button_search.Content = Languages.Translate("Search");
            this.textbox_search.Text = button_search.Content + "...";
            this.text_box.Focus();

            Show();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            this.SizeToContent = SizeToContent.Manual;
            Calculate.CheckWindowPos(this, true);
            this.MaxWidth = double.PositiveInfinity;
            this.MaxHeight = double.PositiveInfinity;
        }

        private void button_search_Click(object sender, RoutedEventArgs e)
        {
            if (textbox_search.Text == "" || textbox_search.Foreground != Brushes.Black)
            {
                textbox_search.Focus();
                return;
            }

            string search = textbox_search.Text;
            if (last_search_index >= text_box.Text.Length) last_search_index = 0;
            int search_index = (search.ToLower() == last_search.ToLower()) ? last_search_index : 0;
            int index = text_box.Text.IndexOf(search, search_index, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                if (last_search_index > 0)
                {
                    last_search_index = 0;
                    button_search_Click(null, null);
                }
                return;
            }
            last_search = search;
            last_search_index = index + search.Length;

            text_box.Focus();
            text_box.SelectionStart = index;
            text_box.SelectionLength = search.Length;
        }

        private void textbox_search_GotFocus(object sender, RoutedEventArgs e)
        {
            if (textbox_search.Foreground != Brushes.Black)
            {
                textbox_search.Text = "";
                textbox_search.Foreground = Brushes.Black;
                textbox_search.FontStyle = FontStyles.Normal;
            }
        }

        private void textbox_search_LostFocus(object sender, RoutedEventArgs e)
        {
            if (textbox_search.Text.Length == 0)
            {
                textbox_search.Text = button_search.Content + "...";
                textbox_search.Foreground = Brushes.Gray;
                textbox_search.FontStyle = FontStyles.Oblique;
            }
        }

        private void textbox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (textbox_search.IsFocused || text_box.IsFocused && textbox_search.Text.Length > 0 && textbox_search.Foreground == Brushes.Black)
                    button_search_Click(null, null);
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                textbox_search.Focus();
                textbox_search.SelectAll();
            }
            else if (e.Key == Key.F3)
            {
                if (textbox_search.Text == "" || textbox_search.Foreground != Brushes.Black)
                    textbox_search.Focus();
                else
                    button_search_Click(null, null);
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}