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
      
        public About()
		{
            
            this.InitializeComponent();
            
            Assembly ainfo = Assembly.GetExecutingAssembly();
            AssemblyName aname = ainfo.GetName();
            //SampleTitle.Text = aname.Name + " " + aname.Version + " beta " + DateTime.Now;
            Title = Languages.Translate("About");
            text_import.Text = Languages.Translate("Import formats:");
            text_export.Text = Languages.Translate("Export formats:");
            button_changelog.Content = Languages.Translate("Changelog") + "*";
            //text_version.Text = aname.Version.ToString(2) + aname.Version.Build + aname.Version.Revision;
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void button_changelog_Click(object sender, RoutedEventArgs e)
        {
            Changelog a = new Changelog(this);
            a.Owner = this;
        }


	}
}