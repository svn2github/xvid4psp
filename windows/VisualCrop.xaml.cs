using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace XviD4PSP
{
    public partial class VisualCrop
    {
        private static object locker = new object();
        private AviSynthReader reader = null;
        private ImageSource picture = null;
        private Image image = null;
        private Bitmap bmp = null;
        private Massive oldm;
        public Massive m;

        private int width;
        private int height;
        private int left = 0;
        private int top = 0;
        private int right = 0;
        private int bottom = 0;
        private int brightness = 0;
        private double fps = 0;

        private string script = "";
        private bool WindowLoaded = false;
        private bool GreenLight = false;
        private bool OldSeeking = false; //Settings.OldSeeking;

        public VisualCrop(Massive mass, System.Windows.Window owner)
        {
            m = mass.Clone();
            oldm = mass.Clone();
            this.InitializeComponent();
            this.Owner = owner;

            //Создаем скрипт (т.к. текущий с кропом и ресайзом не годится)
            script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.VCrop);

            numl.Value = left = m.cropl;
            numr.Value = right = m.cropr;
            numt.Value = top = m.cropt;
            numb.Value = bottom = m.cropb;

            PicBack.Opacity = Settings.VCropOpacity / 10.0;
            brightness = Settings.VCropBrightness * 10;

            numl.ToolTip = Languages.Translate("Left");
            numt.ToolTip = Languages.Translate("Top");
            numr.ToolTip = Languages.Translate("Right");
            numb.ToolTip = Languages.Translate("Bottom");
            button_autocrop.Content = Languages.Translate("Analyse");
            button_autocrop.ToolTip = Languages.Translate("Autocrop black borders");
            button_autocrop_current.ToolTip = Languages.Translate("Autocrop on current frame");
            button_uncrop.ToolTip = Languages.Translate("Remove crop");
            button_fullscreen.ToolTip = Languages.Translate("Fullscreen mode");
            button_cancel.Content = Languages.Translate("Cancel");

            slider_pos.Maximum = m.inframes;
            slider_pos.Value = (Settings.VCropFrame == "THM-frame") ? m.thmframe : 0;

            ShowFrame((int)slider_pos.Value);
            if (left != 0 || right != 0 || top != 0 || bottom != 0) PutBorders();
            else if (image != null) ShowTitle();

            WindowLoaded = true;

            ShowDialog();
            GC.Collect();
        }

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        private void ShowFrame(int frame)
        {
            IntPtr hObject = IntPtr.Zero;
            try
            {
                if (reader == null)
                {
                    //Открываем скрипт
                    reader = new AviSynthReader();
                    reader.ParseScript(script);
                    fps = reader.Framerate;
                }

                //Получаем картинку
                image = reader.ReadFrameBitmap(frame);
                bmp = new System.Drawing.Bitmap(image);
                hObject = bmp.GetHbitmap();
                picture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hObject, IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()); //Переводим картинку в Source

                //Определяем ограничения
                numl.Maximum = numr.Maximum = width = bmp.Width;
                numt.Maximum = numb.Maximum = height = bmp.Height;

                Pic.Source = PicBack.Source = picture;
            }
            catch (Exception ex)
            {
                ErrorException("VisualCrop (ShowFrame): " + ex.Message, ex.StackTrace);
                CloseReader();
            }
            finally
            {
                if (bmp != null) bmp.Dispose();
                DeleteObject(hObject);
                GC.Collect();
            }
        }

        private void PutBorders()
        {
            if (image == null) return;
            IntPtr hObject = IntPtr.Zero;
            try
            {
                bmp = new System.Drawing.Bitmap(image);
                CropImage(ref bmp);
                hObject = bmp.GetHbitmap();
                ImageSource picture = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hObject, IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                Pic.Source = picture;
                ShowTitle();
            }
            catch (Exception ex)
            {
                ErrorException("VisualCrop (PutBorders): " + ex.Message, ex.StackTrace);
            }
            finally
            {
                if (bmp != null) bmp.Dispose();
                DeleteObject(hObject);
                GC.Collect();
            }
        }

        //Закрашивание пикселей (позаимствовано из MeGUI`я)
        private unsafe void CropImage(ref Bitmap b)
        {
            BitmapData image = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte* pointer = (byte*)image.Scan0.ToPointer();
            byte* pixel;
            int stride = image.Stride;
            byte white = (byte)brightness;
           
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
                ErrorException("VisualCrop: Resolution must be mod2 on each side!", null);
                return;
            }

            m.cropl = m.cropl_copy = left;
            m.cropr = m.cropr_copy = right;
            m.cropt = m.cropt_copy = top;
            m.cropb = m.cropb_copy = bottom;

            Close();
        }

        private void ErrorException(string message, string info)
        {
            //Добавляем скрипт в StackTrace
            if (!string.IsNullOrEmpty(script))
                info += Calculate.WrapScript(script, 150);

            new Message((this.IsLoaded) ? this : Owner).ShowMessage(message, info, Languages.Translate("Error"));
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
            if (slider_pos.IsMouseOver && slider_pos.Tag == null)
            {
                if (OldSeeking)
                {
                    ShowFrame((int)(slider_pos.Value));
                    PutBorders();
                }
                else
                    ShowTitle(); //Пересчет номера кадра
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
            ChangeZones(e.GetPosition(Pic), (e.ChangedButton == System.Windows.Input.MouseButton.Left), (e.ClickCount == 2));
        }

        private void MoveMouse(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && GreenLight)
                ChangeZones(e.GetPosition(Pic), true, false);
        }

        private void ChangeZones(System.Windows.Point point, bool left, bool twice)
        {
            GreenLight = true;
            if (point.Y < Pic.ActualHeight / 3)
                if (left) numt.Value = Convert.ToInt32(((double)height * point.Y) / Pic.ActualHeight);          //Сверху
                else numt.Value = 0;
            else if (point.Y > Pic.ActualHeight / 1.5)
                if (left) numb.Value = Convert.ToInt32(height - ((double)height * point.Y) / Pic.ActualHeight); //Снизу
                else numb.Value = 0;
            else if (point.X < Pic.ActualWidth / 3)
                if (left) numl.Value = Convert.ToInt32(((double)width * point.X) / Pic.ActualWidth);            //Слева
                else numl.Value = 0;
            else if (point.X > Pic.ActualWidth / 1.5)
                if (left) numr.Value = Convert.ToInt32(width - ((double)width * point.X) / Pic.ActualWidth);    //Справа
                else numr.Value = 0;
            else if (left && twice) button_fullscreen_Click(null, null);                                        //Центр - Фуллскрин
        }
        
        private void WheelMouse(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int d = (e.Delta < 0) ? -2 : 2;
            if (e.GetPosition(Pic).Y < Pic.ActualHeight / 3) numt.Value = numt.Value + d;         //Сверху
            else if (e.GetPosition(Pic).Y > Pic.ActualHeight / 1.5) numb.Value = numb.Value + d;  //Снизу
            else if (e.GetPosition(Pic).X < Pic.ActualWidth / 3) numl.Value = numl.Value + d;     //Слева
            else if (e.GetPosition(Pic).X > Pic.ActualWidth / 1.5) numr.Value = numr.Value + d;   //Справа
        }

        private void button_AutoCrop_Click(object sender, RoutedEventArgs e)
        {
            Autocrop acrop = new Autocrop(m, this, -1);
            if (acrop.m != null)
            {
                m = acrop.m.Clone();
                SetValues(m.cropl, m.cropt, m.cropr, m.cropb);
                PutBorders();
            }
        }

        private void button_AutoCrop_current_Click(object sender, RoutedEventArgs e)
        {
            Autocrop acrop = new Autocrop(m, this, (int)slider_pos.Value);
            if (acrop.m != null)
            {
                m = acrop.m.Clone();
                SetValues(m.cropl, m.cropt, m.cropr, m.cropb);
                PutBorders();
            }
        }

        private void button_uncrop_Click(object sender, RoutedEventArgs e)
        {
            SetValues(0, 0, 0, 0);
            PutBorders();
        }

        private void SetValues(int _left, int _top, int _right, int _bottom)
        {
            WindowLoaded = false;
            numl.Value = left = m.cropl = _left;
            numt.Value = top = m.cropt = _top;
            numr.Value = right = m.cropr = _right;
            numb.Value = bottom = m.cropb = _bottom;
            WindowLoaded = true;
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

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            GreenLight = false; //Чтоб кроп не срабатывал при включении Фуллскрина
        }

        private void ShowTitle()
        {
            string modw, modh;
            int cw = m.inresw - left - right, ch = m.inresh - top - bottom;
            
            if (cw % 16 == 0) modw = "16";
            else if (cw % 8 == 0) modw = "8";
            else if (cw % 4 == 0) modw = "4";
            else if (cw % 2 == 0) modw = "2";
            else modw = "1";

            if (ch % 16 == 0) modh = "16";
            else if (ch % 8 == 0) modh = "8";
            else if (ch % 4 == 0) modh = "4";
            else if (ch % 2 == 0) modh = "2";
            else modh = "1";

            Title = m.inresw + "x" + m.inresh + " -> " + cw + "x" + ch + " (" + modw + "x" + modh + ", cropped size) | frame " +
                (int)slider_pos.Value + " of " + (int)slider_pos.Maximum;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (numl.txt_box.IsFocused || numt.txt_box.IsFocused || numr.txt_box.IsFocused || numb.txt_box.IsFocused) return;
            string key = new System.Windows.Input.KeyConverter().ConvertToString(e.Key);
            string mod = new System.Windows.Input.ModifierKeysConverter().ConvertToString(System.Windows.Input.Keyboard.Modifiers);
            string PressedKeys = "=" + ((mod.Length > 0) ? mod + "+" : "") + key;

            string Action = HotKeys.GetAction(PressedKeys);
            e.Handled = (Action.Length > 0);

            switch (Action)
            {
                case ("Frame forward"): Frame_Shift(1); break;
                case ("Frame back"): Frame_Shift(-1); break;
                case ("10 frames forward"): Frame_Shift(10); break;
                case ("10 frames backward"): Frame_Shift(-10); break;
                case ("100 frames forward"): Frame_Shift(100); break;
                case ("100 frames backward"): Frame_Shift(-100); break;
                case ("30 sec. forward"): Frame_Shift(Convert.ToInt32(fps * 30)); break;
                case ("30 sec. backward"): Frame_Shift(-Convert.ToInt32(fps * 30)); break;
                case ("3 min. forward"): Frame_Shift(Convert.ToInt32(fps * 180)); break;
                case ("3 min. backward"): Frame_Shift(-Convert.ToInt32(fps * 180)); break;
                case ("Fullscreen"): button_fullscreen_Click(null, null); break;
            }
        }

        private void Frame_Shift(int step)
        {
            int new_frame = (int)slider_pos.Value + step;
            new_frame = (new_frame < 0) ? 0 : (new_frame > (int)slider_pos.Maximum) ? (int)slider_pos.Maximum : new_frame;
            if (new_frame != (int)slider_pos.Value)
            {
                slider_pos.Tag = new object();
                slider_pos.Value = new_frame;
                slider_pos.Tag = null;
                ShowFrame(new_frame);
                PutBorders();
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseReader();
        }

        private void CloseReader()
        {
            lock (locker)
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
        }
    }
}