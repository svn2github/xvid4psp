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
	public partial class Options_M2TS
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;

        public Options_M2TS(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            m = mass.Clone();
            oldm = mass.Clone();
            p = parent;
            Owner = p;

            //переводим
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            Title = Format.EnumToString(m.format) + " " + Languages.Translate("Options").ToLower() + ":";
            label_split.Content = Languages.Translate("Split output file:");
            check_direct_remux.Content = Languages.Translate("Use direct remuxing if possible");

            combo_split.Items.Add("Disabled");
            combo_split.Items.Add("700 mb CD");
            combo_split.Items.Add("800 mb CD");
            combo_split.Items.Add("1000 mb ISO");
            combo_split.Items.Add("4000 mb FAT32");
            combo_split.Items.Add("4700 mb DVD5");
            combo_split.Items.Add("8500 mb DVD9");
            if (m.split != null)
                combo_split.SelectedItem = m.split;
            else
                combo_split.SelectedItem = "Disabled";

            string sdirect_remux = Settings.GetFormatPreset(m.format, "direct_remux");
            if (sdirect_remux != null)
                check_direct_remux.IsChecked = Convert.ToBoolean(sdirect_remux);
            else
                check_direct_remux.IsChecked = true;

            //Включаем текстбокс для ввода мкв-параметров, если формат = мкв
            textbox_mkv_string.Visibility = Visibility.Collapsed;
            mkv_string_enter.Visibility = Visibility.Collapsed;
            mkv_string_warning.Visibility = Visibility.Collapsed;
            if (m.format == Format.ExportFormats.Mkv)
            {
                textbox_mkv_string.Text = m.mkvstring; //введенные пользователем аргументы коммандной строки для mkvmerge 
                textbox_mkv_string.Visibility = Visibility.Visible;
                mkv_string_enter.Visibility = Visibility.Visible;
                mkv_string_warning.Visibility = Visibility.Visible;
                this.Height = 280;
            }


            SetTooltips();

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m.mkvstring = textbox_mkv_string.Text;
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m = oldm.Clone();
            Close();
        }

        private void SetTooltips()
        {
      
        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void combo_split_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_split.IsDropDownOpen || combo_split.IsSelectionBoxHighlighted)
            {
                m.split = combo_split.SelectedItem.ToString();
                Settings.SetFormatPreset(m.format, "split", m.split);
            }
        }

        private void check_direct_remux_Checked(object sender, RoutedEventArgs e)
        {
            if (check_direct_remux.IsFocused)
            {
                Settings.SetFormatPreset(m.format, "direct_remux", check_direct_remux.IsChecked.Value.ToString());
            }
        }

        private void check_direct_remux_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_direct_remux.IsFocused)
            {
                Settings.SetFormatPreset(m.format, "direct_remux", check_direct_remux.IsChecked.Value.ToString());
            }
        }

	}
}