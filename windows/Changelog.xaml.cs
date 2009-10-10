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
	public partial class Changelog
	{
        public Changelog(System.Windows.Window owner)
        {
            this.Owner = owner;
            this.InitializeComponent();

            try
            {
                if (Settings.Language == "Russian")
                {
                    using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\Changelog_rus.txt", System.Text.Encoding.Default))
                        changelog_text.Text = sr.ReadToEnd();
                }
                else
                {
                    using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\Changelog_eng.txt", System.Text.Encoding.Default))
                        changelog_text.Text = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Message mes = new Message(this);
                mes.ShowMessage(ex.Message, "Error");
                Close();
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

    }
}