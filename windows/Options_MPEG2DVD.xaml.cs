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
	public partial class Options_MPEG2DVD
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;

        public Options_MPEG2DVD(Massive mass, MainWindow parent)
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

            check_multiplex.Content = Languages.Translate("Don`t multiplex video and audio");

            check_multiplex.IsChecked = Settings.Mpeg2MultiplexDisabled;

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

        private void check_multiplex_Checked(object sender, RoutedEventArgs e)
        {
            if (check_multiplex.IsFocused)
            {
                Settings.Mpeg2MultiplexDisabled = check_multiplex.IsChecked.Value;
                m.dontmuxstreams = check_multiplex.IsChecked.Value;
            }
        }

        private void check_multiplex_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_multiplex.IsFocused)
            {
                Settings.Mpeg2MultiplexDisabled = check_multiplex.IsChecked.Value;
                m.dontmuxstreams = check_multiplex.IsChecked.Value;
            }
        }

	}
}