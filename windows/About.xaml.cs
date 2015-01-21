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

            try
            {
                Assembly this_assembly = Assembly.GetExecutingAssembly();
                text_version.Text = "Version: " + this_assembly.GetName().Version.ToString();
                text_version.Text += " (" + File.GetLastWriteTime(this_assembly.GetModules()[0].FullyQualifiedName).ToString("dd.MM.yyyy") + ")";

                object[] attributes = this_assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length > 0)
                {
                    string copyright = ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
                    if (copyright.Length > 10) text_copyright.Text = copyright.Substring(10);
                }
            }
            catch { }

            text_about.ToolTip = SysInfo.AVSVersionString;

            Title = Languages.Translate("About");
            text_import.Text = Languages.Translate("Output codecs:");
            text_export.Text = Languages.Translate("Output containers:");
            button_changelog.Content = Languages.Translate("Changelog");

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

        private void button_changelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string file = "\\Changelog_eng.txt";
                if (Settings.Language == "Russian") file = "\\Changelog_rus.txt";
                using (StreamReader sr = new StreamReader(Calculate.StartupPath + file, System.Text.Encoding.Default))
                    new ShowWindow(this, "Changelog", sr.ReadToEnd(), new FontFamily("Lucida Console"));
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage(ex.Message, ex.StackTrace, Languages.Translate("Error"));
            }
        }
	}
}