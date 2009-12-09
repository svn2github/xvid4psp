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
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace XviD4PSP
{
    public partial class VisualCrop
    {
        private int width;
        private int height;

        private int left = 0;
        private int top = 0;
        private int right = 0;
        private int bottom = 0;

        private string script = "";
        private bool WindowLoaded = false;

        private Bitmap bmp;
        private ImageSource picture;
        private System.Drawing.Image image;

        public Massive m;
        private Massive oldm;

        private bool OldSeeking = false;// Settings.OldSeeking;

        public VisualCrop(Massive mass, System.Windows.Window owner)
        {
            m = mass.Clone();
            oldm = mass.Clone();
            this.InitializeComponent();
            this.Owner = owner;

            //Создаем скрипт (т.к. текущий с кропом и ресайзом не годится)
            script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Info);

            numl.Value = left = m.cropl;
            numr.Value = right = m.cropr;
            numt.Value = top = m.cropt;
            numb.Value = bottom = m.cropb;

            Title = m.inresw + "x" + m.inresh + " -> " + (m.inresw - left - right) + "x" + (m.inresh - top - bottom) + " (cropped size)";
            button_autocrop.Content = Languages.Translate("Analyse");
            button_cancel.Content = Languages.Translate("Cancel");

            slider_pos.Maximum = m.outframes;
            slider_pos.Value = m.thmframe;

            ShowFrame(m.thmframe);
            if (left != 0 || right != 0 || top != 0 || bottom != 0) PutBorders();

            WindowLoaded = true;
            ShowDialog();

            GC.Collect();
        }

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        private void ShowFrame(int frame)
        {
            AviSynthReader reader = new AviSynthReader();
            reader.ParseScript(script); //Читаем скрипт
            image = reader.ReadFrameBitmap(frame); //Получаем картинку
            bmp = new System.Drawing.Bitmap(image);
            IntPtr hObject = bmp.GetHbitmap();
            picture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hObject, IntPtr.Zero, Int32Rect.Empty,
               System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()); //Переводим картинку в Source

            //Определяем ограничения
            numl.Maximum = numr.Maximum = width = bmp.Width;
            numt.Maximum = numb.Maximum = height = bmp.Height;

            Pic.Source = PicBack.Source = picture;
          
            bmp.Dispose();
            reader.Close();
            DeleteObject(hObject);
            GC.Collect();
        }

        private void PutBorders()
        {
            bmp = new System.Drawing.Bitmap(image);
            CropImage(ref bmp);
            IntPtr hObject = bmp.GetHbitmap();
            ImageSource picture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hObject, IntPtr.Zero, Int32Rect.Empty,
               System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            Pic.Source = picture;
            Title = m.inresw + "x" + m.inresh + " -> " + (m.inresw - left - right) + "x" + (m.inresh - top - bottom) + " (cropped size)";

            DeleteObject(hObject);
            bmp.Dispose();
            GC.Collect();
        }

        //Закрашивание пикселей (позаимствовано из MeGUI`я)
        private unsafe void CropImage(ref Bitmap b)
        {
            BitmapData image = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte* pointer = (byte*)image.Scan0.ToPointer();
            byte* pixel;
            int stride = image.Stride;
            byte white = (byte)255;
           
            pixel = pointer;
            int width = b.Width;
            int height = b.Height;
            int width3 = 3 * width;
            int left3 = 3 * left;
            int right3 = 3 * right;

            int lineGap = stride - width3;
            int centerJump = width3 - left3 - right3;
            for (int j = 0; j < top; j++)
            {
                for (int i = 0; i < width3; i++)
                {
                    *pixel = white;
                    pixel++;
                }
                pixel += lineGap;
            }
            int heightb = height - bottom;
            for (int j = top; j < heightb; j++)
            {
                for (int i = 0; i < left3; i++)
                {
                    *pixel = white;
                    pixel++;
                }
                pixel += centerJump;
                for (int i = 0; i < right3; i++)
                {
                    *pixel = white;
                    pixel++;
                }
                pixel += lineGap;
            }
            for (int j = b.Height - bottom; j < height; j++)
            {
                for (int i = 0; i < width3; i++)
                {
                    *pixel = white;
                    pixel++;
                }
                pixel += lineGap;
            }
            b.UnlockBits(image);
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            //Проверка на четность
            if (left % 2 != 0 || right % 2 != 0 || top % 2 != 0 || bottom % 2 != 0)
            {
                ErrorExeption("Resolution must be mod2 on each side!");
                return;
            }
            m.cropl = m.cropl_copy = left;
            m.cropr = m.cropr_copy = right;
            m.cropt = m.cropt_copy = top;
            m.cropb = m.cropb_copy = bottom;
           /* m = AspectResolution.FixInputAspect(m);
            m = Format.GetValidResolution(m);
            m = Format.GetValidOutAspect(m);
            m = AspectResolution.FixAspectDifference(m);
            m = AviSynthScripting.CreateAutoAviSynthScript(m); */
            Close();
        }

        private void ErrorExeption(string message)
        {
            new Message(this).ShowMessage(message, Languages.Translate("Error"));
        }

        private void changedl(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded)
            {
                if (Convert.ToInt32(numl.Value) % 2 == 0)
                {
                    left = (int)numl.Value;
                    if (left + right < width) PutBorders();
                    else numl.Value = width - right - 1;
                }
                else
                    numl.Value -= 1;
            }
        }

        private void changedr(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded)
            {
                if (Convert.ToInt32(numr.Value) % 2 == 0)
                {
                    right = (int)numr.Value;
                    if (left + right < width) PutBorders();
                    else numr.Value = width - left - 1;
                }
                else
                    numr.Value -= 1;
            }
        }

        private void changedt(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded)
            {
                if (Convert.ToInt32(numt.Value) % 2 == 0)
                {
                    top = (int)numt.Value;
                    if (top + bottom < height) PutBorders();
                    else numt.Value = height - bottom - 1;
                }
                else
                    numt.Value -= 1;
            }
        }

        private void changedb(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded)
            {
                if (Convert.ToInt32(numb.Value) % 2 == 0)
                {
                    bottom = (int)numb.Value;
                    if (top + bottom < height) PutBorders();
                    else numb.Value = height - top - 1;
                }
                else
                    numb.Value -= 1;
            }
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            m = oldm.Clone();
            Close();
        }

        private void slider_pos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (slider_pos.IsMouseOver && OldSeeking)
            {
                ShowFrame((int)(slider_pos.Value));
                PutBorders();
            }
        }

        private void slider_pos_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (slider_pos.IsMouseOver && !OldSeeking)
            {
                ShowFrame((int)(slider_pos.Value));
                PutBorders();
            }
        }

        private void slider_pos_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            /*Settings.OldSeeking =*/ OldSeeking = !OldSeeking;
            if (OldSeeking) Title = "Old Seeking"; else Title = "New Seeking";
            //((MainWindow)(Owner.Owner)).check_old_seeking.IsChecked = OldSeeking;
        }

        private void MouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.GetPosition(Pic).Y < Pic.ActualHeight / 3)
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    numt.Value = Convert.ToInt32(((double)height * e.GetPosition(Pic).Y) / Pic.ActualHeight);          //Сверху
                else numt.Value = 0;
            else if (e.GetPosition(Pic).Y > Pic.ActualHeight / 1.5)
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    numb.Value = Convert.ToInt32(height - ((double)height * e.GetPosition(Pic).Y) / Pic.ActualHeight); //Снизу
                else numb.Value = 0;
            else if (e.GetPosition(Pic).X < Pic.ActualWidth / 3)
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    numl.Value = Convert.ToInt32(((double)width * e.GetPosition(Pic).X) / Pic.ActualWidth);            //Слева
                else numl.Value = 0;
            else if (e.GetPosition(Pic).X > Pic.ActualWidth / 1.5)
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    numr.Value = Convert.ToInt32(width - ((double)width * e.GetPosition(Pic).X) / Pic.ActualWidth);    //Справа
                else numr.Value = 0;
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Left && e.ClickCount == 2) button_fullscreen_Click(null, null); //Центр - Фуллскрин
        }

        private void WheelMouse(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int d = 2;
            if (e.Delta < 0) d = -2;
            if (e.GetPosition(Pic).Y < Pic.ActualHeight / 3) numt.Value = numt.Value + d;         //Сверху
            else if (e.GetPosition(Pic).Y > Pic.ActualHeight / 1.5) numb.Value = numb.Value + d;  //Снизу
            else if (e.GetPosition(Pic).X < Pic.ActualWidth / 3) numl.Value = numl.Value + d;     //Слева
            else if (e.GetPosition(Pic).X > Pic.ActualWidth / 1.5) numr.Value = numr.Value + d;   //Справа
        }

        private void button_AutoCrop_Click(object sender, RoutedEventArgs e)
        {
            Autocrop acrop = new Autocrop(m, this);
            m = acrop.m.Clone();
            numl.Value = left = m.cropl;
            numr.Value = right = m.cropr;
            numt.Value = top = m.cropt;
            numb.Value = bottom = m.cropb;
            PutBorders();
        }

        private void button_fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                this.SizeToContent = SizeToContent.Manual;
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                txt_info.Visibility = Visibility.Visible;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                txt_info.Visibility = Visibility.Collapsed;
            }
        }
    }
}