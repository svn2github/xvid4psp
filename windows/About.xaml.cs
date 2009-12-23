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
using System.Windows.Input;
using System.Reflection;

namespace XviD4PSP
{
	public partial class About
	{
        public About(System.Windows.Window owner)
		{
            this.Owner = owner;
            this.InitializeComponent();
            
            AssemblyInfoHelper asinfo = new AssemblyInfoHelper();
            text_version.Text = "Version " + asinfo.Version + ", SVN revision " + asinfo.Trademark.Replace("rev", "");
            asinfo = null;

            Title = Languages.Translate("About");
            text_import.Text = Languages.Translate("Output codecs:");
            text_export.Text = Languages.Translate("Output containers:");
            button_changelog.Content = Languages.Translate("Changelog");

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void button_changelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string file = "\\Changelog_eng.txt";
                if (Settings.Language == "Russian") file = "\\Changelog_rus.txt";
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + file, System.Text.Encoding.Default))
                    new ShowWindow(this, "Changelog", sr.ReadToEnd(), new FontFamily("Tahoma"));
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(ex.Message, "Error");
            }
        }
	}
}