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
	public partial class Options_BluRay
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;

        public Options_BluRay(Massive mass, MainWindow parent)
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
            label_type.Content = "BluRay " + Languages.Translate("type") + ":";

            check_direct_remux.Content = Languages.Translate("Use direct remuxing when it possible");

            combo_type.Items.Add("UDF 2.50 DVD/BD");
            combo_type.Items.Add("FAT32 HDD/MS");

            combo_type.SelectedItem = m.bluray_type;

            string sdirect_remux = Settings.GetFormatPreset(m.format, "direct_remux");
            if (sdirect_remux != null)
                check_direct_remux.IsChecked = Convert.ToBoolean(sdirect_remux);
            else
                check_direct_remux.IsChecked = true;

            SetTooltips();

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
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

        private void combo_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_type.IsDropDownOpen || combo_type.IsSelectionBoxHighlighted)
            {
                m.bluray_type = combo_type.SelectedItem.ToString();
                Settings.BluRayType = m.bluray_type;
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