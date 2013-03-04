using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace WPF_VideoPlayer
{
	public partial class MediaInfo
	{
        private static object locker = new object();
        private IntPtr MI_Handle = IntPtr.Zero;
        private string infilepath;

        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_New();

        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);

        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);

        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);

        [DllImport("dlls//MediaInfo//MediaInfo.dll")]
        private static extern void MediaInfo_Delete(IntPtr Handle);

        public MediaInfo(string infilepath, System.Windows.Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.infilepath = infilepath;

            DDHelper ddh = new DDHelper(this);
            ddh.GotFiles += new DDEventHandler(ddh_GotFiles);

            if (Settings.MI_WrapText)
            {
                check_wrap.IsChecked = true;
                tbxInfo.TextWrapping = TextWrapping.Wrap;
            }
            else
            {
                check_wrap.IsChecked = false;
                tbxInfo.TextWrapping = TextWrapping.NoWrap;
            }

            check_mi_full.IsChecked = Settings.MI_Full;
            check_mi_full.Content = Languages.Translate("Full info");
            check_wrap.Content = Languages.Translate("Wrap text");
            button_open.Content = Languages.Translate("Open");
            button_save.Content = Languages.Translate("Save");
            button_close.Content = Languages.Translate("Close");
            tbxInfo.ToolTip = Languages.Translate("Drag and Drop your files here");

            if (!string.IsNullOrEmpty(infilepath))
                GetInfo();

            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            if (Settings.CheckWindowsPos)
            {
                IntPtr Handle = new WindowInteropHelper(this).Handle;
                ((MainWindow)Owner).CheckWindowPos(this, Handle, false);
            }
        }

        private void button_open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.Filter = Languages.Translate("All files") + " (*.*)|*.*";
                dialog.Multiselect = false;
                dialog.Title = Languages.Translate("Select media file") + ":";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    infilepath = dialog.FileName;
                    GetInfo();
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex);
            }
        }

        private void button_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void GetInfo()
        {
            try
            {
                tbxInfo.ToolTip = null; //Чтоб не мешался

                //MediaInfo не покажет ничего интересного для Ависинт-скриптов
                if (Path.GetExtension(infilepath).ToLower() == ".avs")
                {
                    using (StreamReader sr = new StreamReader(infilepath, System.Text.Encoding.Default))
                        tbxInfo.Text = sr.ReadToEnd();
                }
                else
                {
                    MI_Handle = MediaInfo_New();
                    MediaInfo_Open(MI_Handle, infilepath);
                    MediaInfo_Option(MI_Handle, "Complete", (Settings.MI_Full ? "1" : ""));
                    MediaInfo_Option(MI_Handle, "Language", "  Config_Text_ColumnSize;" + Settings.MI_ColumnSize);
                    tbxInfo.Text = Marshal.PtrToStringUni(MediaInfo_Inform(MI_Handle, IntPtr.Zero));
                }
            }
            catch (Exception ex)
            {
                ErrorException(ex);
            }
            finally
            {
                CloseMI();
            }
        }

        private void ddh_GotFiles(object sender, string[] files)
        {
            tbxInfo.Clear();
            tbxInfo.ScrollToHome();
            infilepath = files[0];
            GetInfo();
            Activate();
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(infilepath))
            {
                try
                {
                    System.Windows.Forms.SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
                    s.SupportMultiDottedExtensions = true;
                    s.DefaultExt = ".log";
                    s.AddExtension = true;
                    s.Title = Languages.Translate("Select unique name for output file:");
                    s.Filter = "LOG " + Languages.Translate("files") + "|*.log" +
                        "|TXT " + Languages.Translate("files") + "|*.txt";

                    s.InitialDirectory = Path.GetDirectoryName(infilepath);
                    s.FileName = Path.GetFileName(infilepath) + (Settings.MI_Full ? " - MediaInfoFull" : " - MediaInfo");

                    if (s.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.WriteAllText(s.FileName, tbxInfo.Text); //, System.Text.Encoding.Default);
                    }
                }
                catch (Exception ex)
                {
                    ErrorException(ex);
                }
            }
        }

        private void ErrorException(Exception ex)
        {
            tbxInfo.Text = (Languages.Translate("Error") + ":\r\n   " + ex.Message + "\r\n\r\nStackTrace:\r\n" + ex.StackTrace);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseMI();
        }

        private void CloseMI()
        {
            lock (locker)
            {
                if (MI_Handle != IntPtr.Zero)
                {
                    MediaInfo_Delete(MI_Handle);
                    MI_Handle = IntPtr.Zero;
                }
            }
        }

        private void check_mi_full_Click(object sender, RoutedEventArgs e)
        {
            Settings.MI_Full = check_mi_full.IsChecked.Value;

            if (!string.IsNullOrEmpty(infilepath))
                GetInfo();
        }

        private void check_wrap_Click(object sender, RoutedEventArgs e)
        {
            if ((Settings.MI_WrapText = check_wrap.IsChecked.Value))
            {
                tbxInfo.TextWrapping = TextWrapping.Wrap;
            }
            else
                tbxInfo.TextWrapping = TextWrapping.NoWrap;
        }
	}
}