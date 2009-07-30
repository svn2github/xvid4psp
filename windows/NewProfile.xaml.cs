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
	public partial class NewProfile
	{
       public string profile;
       private string format;
       private ProfileType ptype;
       public enum ProfileType { SBC = 1, VEncoding, AEncoding }

		public NewProfile(string auto_name, string format, ProfileType ptype, System.Windows.Window owner)
		{
			this.InitializeComponent();

            Owner = owner;

            textbox_profile.Text = auto_name;
            this.format = format;
            this.ptype = ptype;

            Title = Languages.Translate("New profile");
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            text_profile.Content = Languages.Translate("Name of new profile:");

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string profile_path = "";
            if (ptype == ProfileType.VEncoding)
                profile_path = Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\video\\" + textbox_profile.Text + ".txt";
            else if (ptype == ProfileType.AEncoding)
                profile_path = Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\audio\\" + textbox_profile.Text + ".txt";
            else if (ptype == ProfileType.SBC)
                profile_path = Calculate.StartupPath + "\\presets\\sbc\\" + textbox_profile.Text + ".avs";

            if (File.Exists(profile_path))
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Profile with same name already exists.") +
                Environment.NewLine + Languages.Translate("Replace profile?"), 
                Languages.Translate("Question"), Message.MessageStyle.YesNo);
                if (mess.result == Message.Result.No)
                    return;
            }

            if (textbox_profile.Text != "")
                profile = textbox_profile.Text;
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

	}
}