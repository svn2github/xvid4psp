using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Diagnostics;

namespace XviD4PSP
{
	public partial class Donate
	{
		public Donate(System.Windows.Window owner)
		{
			this.InitializeComponent();

            this.Owner = owner;

			// Insert code required on object creation below this point.
            text_donate.Text = Languages.Translate("If you think that my unofficial builds of XviD4PSP are not so bad, and if you can help me with something, you can always send me email to say all what you wanna say, or may be something else") + " :)";
            this.Title = Languages.Translate("Donate");

            Show();
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void button_email_Click(object sender, RoutedEventArgs e)
        {

            try
            {
               // Settings.WasDonate = true;
                Process.Start("mailto:forclip@gmail.com");
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

	}
}