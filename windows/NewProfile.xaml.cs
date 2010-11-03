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
       private string format;
       private ProfileType ptype;
       public string profile = null;
       public enum ProfileType { SBC = 1, VEncoding, AEncoding, FFRebuilder }

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
            //Имя нового профиля
            string name = textbox_profile.Text.Trim();

            //Проверка на недопустимые имена
            if (name == "") return;
            if (name.ToLower() == "disabled" && (ptype == ProfileType.SBC || ptype == ProfileType.AEncoding || ptype == ProfileType.VEncoding) ||
                name.ToLower() == "default" && ptype == ProfileType.FFRebuilder)
            {
                new Message(this).ShowMessage(Languages.Translate("Profile with same name already exists."), Languages.Translate("Error"));
                return;
            }

            string profile_path = "";
            if (ptype == ProfileType.VEncoding)
                profile_path = Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\video\\" + name + ".txt";
            else if (ptype == ProfileType.AEncoding)
                profile_path = Calculate.StartupPath + "\\presets\\encoding\\" + format + "\\audio\\" + name + ".txt";
            else if (ptype == ProfileType.SBC)
                profile_path = Calculate.StartupPath + "\\presets\\sbc\\" + name + ".avs";
            else if (ptype == ProfileType.FFRebuilder)
                profile_path = Calculate.StartupPath + "\\presets\\ffrebuilder\\" + name + ".txt";

            if (File.Exists(profile_path))
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Profile with same name already exists.") +
                Environment.NewLine + Languages.Translate("Replace profile?"),
                Languages.Translate("Question"), Message.MessageStyle.YesNo);
                if (mess.result == Message.Result.No)
                    return;
            }

            profile = name;
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
	}
}