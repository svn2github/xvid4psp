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
        public Options_BluRay(Window parent)
        {
            this.InitializeComponent();
            this.Owner = parent;

            //переводим
            Title = "BluRay " + Languages.Translate("Options").ToLower() + ":";
            label_type.Content = "BluRay " + Languages.Translate("type") + ":";
            check_direct_remux.Content = Languages.Translate("Use direct remuxing if possible");
            check_direct_remux.ToolTip = Languages.Translate("If possible, copy the streams directly from the source file without demuxing them (Copy mode)");
            check_dont_mux.Content = Languages.Translate("Don`t multiplex video and audio streams");
            check_dont_mux.ToolTip = Languages.Translate("Encode video and audio streams in a separate files (without muxing)");
            check_interlace.Content = Languages.Translate("Interlace is allowed");
            check_interlace.ToolTip = Languages.Translate("Enable this option if you want to allow interlaced encoding for this format");
            button_ok.Content = Languages.Translate("OK");

            combo_type.Items.Add("UDF 2.50 DVD/BD");
            combo_type.Items.Add("FAT32 HDD/MS");
            combo_type.SelectedItem = Settings.BluRayType;

            string sdirect_remux = Settings.GetFormatPreset(Format.ExportFormats.BluRay, "direct_remux");
            if (sdirect_remux != null)
                check_direct_remux.IsChecked = Convert.ToBoolean(sdirect_remux);
            else
                check_direct_remux.IsChecked = true;

            check_dont_mux.IsChecked = Convert.ToBoolean(Settings.GetFormatPreset(Format.ExportFormats.BluRay, "dont_mux_streams"));
            check_interlace.IsChecked = Convert.ToBoolean(Settings.GetFormatPreset(Format.ExportFormats.BluRay, "interlaced"));

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void combo_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_type.IsDropDownOpen || combo_type.IsSelectionBoxHighlighted)
            {
                Settings.BluRayType = combo_type.SelectedItem.ToString();
            }
        }

        private void check_direct_remux_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetFormatPreset(Format.ExportFormats.BluRay, "direct_remux", check_direct_remux.IsChecked.Value.ToString());
        }

        private void check_dont_mux_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetFormatPreset(Format.ExportFormats.BluRay, "dont_mux_streams", check_dont_mux.IsChecked.Value.ToString());
        }

        private void check_interlace_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetFormatPreset(Format.ExportFormats.BluRay, "interlaced", check_interlace.IsChecked.Value.ToString());
        }
    }
}