using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace XviD4PSP
{
    public partial class VisualCrop
    {
        private static object locker = new object();
        private AviSynthReader reader = null;
        private Massive oldm;
        public Massive m;

        private int width;
        private int height;
        private int left = 0;
        private int top = 0;
        private int right = 0;
        private int bottom = 0;
        private double fps = 0;

        private string script = "";
        private string frame_of = "";
        private string cropped_s = "";
        private bool WindowLoaded = false;
        private bool IgnoreMouse = false;
        private bool OldSeeking = false; //Settings.OldSeeking;

        private byte R = 255;
        private byte G = 255;
        private byte B = 255;
        private byte A = 200;

        private bool IsError = false;
        private bool HasVideo = false;
        private IntPtr buffer = IntPtr.Zero;
        private PixelFormat format = PixelFormats.Bgr24;
        private int bpp = PixelFormats.Bgr24.BitsPerPixel / 8;
        private int stride = 0;

        public VisualCrop(Massive mass, Window owner)
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

            Color color = Settings.VCropBrush;
            slider_R.Value = R = color.R;
            slider_G.Value = G = color.G;
            slider_B.Value = B = color.B;
            slider_A.Value = A = color.A;
            FinalColor.Color = Color.FromArgb(255, R, G, B);

            numl.ToolTip = Languages.Translate("Left");
            numt.ToolTip = Languages.Translate("Top");
            numr.ToolTip = Languages.Translate("Right");
            numb.ToolTip = Languages.Translate("Bottom");
            button_autocrop.Content = Languages.Translate("Analyse");
            button_autocrop.ToolTip = Languages.Translate("Autocrop black borders");
            button_autocrop_current.ToolTip = Languages.Translate("Autocrop on current frame");
            button_uncrop.ToolTip = Languages.Translate("Remove crop");
            button_settings.ToolTip = Languages.Translate("Settings");
            slider_A.ToolTip = Languages.Translate("Transparency of the mask");
            slider_R.ToolTip = slider_G.ToolTip = slider_B.ToolTip = Languages.Translate("Brightness of the mask");

            button_fullscreen.ToolTip = Languages.Translate("Fullscreen mode");
            button_cancel.Content = Languages.Translate("Cancel");
            frame_of = Languages.Translate("Frame XX of YY").ToLower();
            cropped_s = Languages.Translate("cropped size");

            try
            {
                reader = new AviSynthReader(AviSynthColorspace.RGB24, AudioSampleType.Undefined);
                reader.ParseScript(script);

                if (reader.Clip.HasVideo && reader.FrameCount > 0)
                {
                    slider_pos.Maximum = reader.FrameCount;
                    slider_pos.Value = (Settings.VCropFrame == "THM-frame") ? m.thmframe : 0;
                    numl.Maximum = numr.Maximum = width = reader.Width;
                    numt.Maximum = numb.Maximum = height = reader.Height;
                    fps = reader.Framerate;

                    stride = width * bpp;
                    buffer = Marshal.AllocHGlobal(stride * height);
                    HasVideo = true;

                    SetFrame((int)slider_pos.Value);
                    ShowCroppedFrame();
                }
                else
                {
                    PreviewError("NO VIDEO", Brushes.Gainsboro);
                }
            }
            catch (Exception ex)
            {
                SetPreviewError(ex);
            }

            if (IsError)
            {
                CloseReader();
                Close();
            }
            else
            {
                WindowLoaded = true;
                ShowDialog();
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            this.SizeToContent = SizeToContent.Manual;
            Calculate.CheckWindowPos(this, true);
            this.MaxWidth = double.PositiveInfinity;
            this.MaxHeight = double.PositiveInfinity;
            if (Settings.VCropFullscreen && !IsError && HasVideo)
                button_fullscreen_Click(null, null);
        }

        private void SetFrame(int frame)
        {
            try
            {
                if (reader != null && !IsError && HasVideo)
                {
                    reader.Clip.ReadFrame(buffer, stride, frame);
                }
            }
            catch (Exception ex)
            {
                SetPreviewError(ex);
            }
        }

        private void ShowCroppedFrame()
        {
            try
            {
                if (reader == null || IsError || !HasVideo)
                    return;

                #region
                //В buffer`е содержится исходная картинка без бордюров. Тут из неё делается
                //копия, на которую будут наложены бордюры с учетом их яркости и прозрачности.
                //Можно было бы сделать две картинки (как раньше): исходную - с изображением,
                //вторую поверх неё - только с бордюрами (центр - прозрачный). Это было бы
                //быстрее в работе, т.к. при позиционировании не пришлось бы изменять вторую
                //картинку с бордюрами, а при изменении бордюров не требовалось бы делать
                //копию исходной картинки. Но WPF очень коряво ресайзит границы перехода
                //от бордюра к прозрачной области. Если же изменить опции рендеринга для
                //картинки на NearestNeighbor, то эта проблема пропадает, но вместо неё
                //появляется доп. погрешность. Есть еще вариант отображать бордюры самим
                //WPF (4 Rectangle по краям), но скорее всего проблем будет еще больше..
                #endregion

                byte[] pixels = new byte[stride * height];
                Marshal.Copy(buffer, pixels, 0, pixels.Length);

                byte Ainv = (byte)(255 - A); //Инверсия прозрачности
                byte RA = (byte)((R * A) / 255); //R с учетом прозрачности
                byte GA = (byte)((G * A) / 255); //G с учетом прозрачности
                byte BA = (byte)((B * A) / 255); //B с учетом прозрачности

                unsafe
                {
                    fixed (byte* pointer = pixels)
                    {
                        byte* pixel = pointer;
                        int topPixels = width * top;
                        int bottomPixels = width * bottom;
                        int centerHeight = height - top - bottom;
                        int centerJump = (width - left - right) * bpp;

                        for (int j = 0; j < bottomPixels; j++) //Снизу
                        {
                            *pixel = (byte)(((*pixel * Ainv) / 255) + BA); pixel++;
                            *pixel = (byte)(((*pixel * Ainv) / 255) + GA); pixel++;
                            *pixel = (byte)(((*pixel * Ainv) / 255) + RA); pixel++;
                            //*pixel = 255; pixel++; //Для RGB32
                        }
                        for (int j = 0; j < centerHeight; j++) //Середина
                        {
                            for (int i = 0; i < left; i++) //Слева
                            {
                                *pixel = (byte)(((*pixel * Ainv) / 255) + BA); pixel++;
                                *pixel = (byte)(((*pixel * Ainv) / 255) + GA); pixel++;
                                *pixel = (byte)(((*pixel * Ainv) / 255) + RA); pixel++;
                                //*pixel = 255; pixel++;
                            }
                            pixel += centerJump;
                            for (int i = 0; i < right; i++) //Справа
                            {
                                *pixel = (byte)(((*pixel * Ainv) / 255) + BA); pixel++;
                                *pixel = (byte)(((*pixel * Ainv) / 255) + GA); pixel++;
                                *pixel = (byte)(((*pixel * Ainv) / 255) + RA); pixel++;
                                //*pixel = 255; pixel++;
                            }
                        }
                        for (int j = 0; j < topPixels; j++) //Сверху
                        {
                            *pixel = (byte)(((*pixel * Ainv) / 255) + BA); pixel++;
                            *pixel = (byte)(((*pixel * Ainv) / 255) + GA); pixel++;
                            *pixel = (byte)(((*pixel * Ainv) / 255) + RA); pixel++;
                            //*pixel = 255; pixel++;
                        }
                    }
                }

                if (reader != null && !IsError)
                {
                    Pic.Source = BitmapSource.Create(width, height, 0, 0, format, null, pixels, stride);
                    pixels = null;
                    ShowTitle();
                }
            }
            catch (Exception ex)
            {
                if (ex is AccessViolationException)
                    throw;

                if (Pic.Source != null)
                    ErrorException("VisualCrop (ShowCroppedFrame): " + ex.Message, ex.StackTrace);
                else
                    SetPreviewError(ex);
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            //Проверка на четность
            if (left % 2 != 0 || right % 2 != 0 || top % 2 != 0 || bottom % 2 != 0)
            {
                ErrorException("VisualCrop: " + Languages.Translate("Resolution must be mod2 on each side!"), null);
                return;
            }

            m.cropl = m.cropl_copy = left;
            m.cropr = m.cropr_copy = right;
            m.cropt = m.cropt_copy = top;
            m.cropb = m.cropb_copy = bottom;

            Close();
        }

        private void SetPreviewError(Exception ex)
        {
            if (!IsError)
            {
                IsError = true;
                Pic.Source = null;
                Pic.Visibility = Visibility.Collapsed;

                //Добавляем скрипт в StackTrace
                string stack = ex.StackTrace;
                if (!string.IsNullOrEmpty(script))
                    stack += Calculate.WrapScript(script, 150);

                ErrorException("VisualCrop: " + ex.Message.Trim(), stack);
                PreviewError(Languages.Translate("Error") + "...", Brushes.Red);
            }
        }

        private void PreviewError(string text, Brush foreground)
        {
            ErrBox.Child = new TextBlock() { Text = text, Background = Brushes.Black, Foreground = foreground, TextAlignment = TextAlignment.Center, FontFamily = new FontFamily("Arial") };
            ErrBox.Visibility = Visibility.Visible;
        }

        private void ErrorException(string message, string info)
        {
            new Message((this.IsLoaded) ? this : Owner).ShowMessage(message, info, Languages.Translate("Error"));
        }

        private void changedl(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded && !IsError)
            {
                if (Convert.ToInt32(numl.Value) % 2 == 0)
                {
                    left = (int)numl.Value;
                    if (left + right < width) ShowCroppedFrame();
                    else numl.Value = width - right - 1;
                }
                else
                    numl.Value -= 1;
            }
        }

        private void changedr(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded && !IsError)
            {
                if (Convert.ToInt32(numr.Value) % 2 == 0)
                {
                    right = (int)numr.Value;
                    if (left + right < width) ShowCroppedFrame();
                    else numr.Value = width - left - 1;
                }
                else
                    numr.Value -= 1;
            }
        }

        private void changedt(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded && !IsError)
            {
                if (Convert.ToInt32(numt.Value) % 2 == 0)
                {
                    top = (int)numt.Value;
                    if (top + bottom < height) ShowCroppedFrame();
                    else numt.Value = height - bottom - 1;
                }
                else
                    numt.Value -= 1;
            }
        }

        private void changedb(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (WindowLoaded && !IsError)
            {
                if (Convert.ToInt32(numb.Value) % 2 == 0)
                {
                    bottom = (int)numb.Value;
                    if (top + bottom < height) ShowCroppedFrame();
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
            if (!IsError && HasVideo && slider_pos.IsMouseOver && slider_pos.Tag == null)
            {
                if (OldSeeking)
                {
                    SetFrame((int)slider_pos.Value);
                    ShowCroppedFrame();
                }
                else
                    ShowTitle(); //Пересчет номера кадра
            }
        }

        private void slider_pos_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsError && HasVideo && slider_pos.IsMouseOver && !OldSeeking)
            {
                SetFrame((int)slider_pos.Value);
                ShowCroppedFrame();
            }
        }

        private void slider_pos_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*Settings.OldSeeking =*/ OldSeeking = !OldSeeking;
            if (OldSeeking) Title = "Old Seeking"; else Title = "New Seeking";
            //((MainWindow)(Owner.Owner)).check_old_seeking.IsChecked = OldSeeking;
        }

        private void MouseClick(object sender, MouseButtonEventArgs e)
        {  
            ChangeZones(e.GetPosition(Pic), (e.ChangedButton == MouseButton.Left), (e.ClickCount == 2));
        }

        private void MoveMouse(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !IgnoreMouse)
                ChangeZones(e.GetPosition(Pic), true, false);
        }

        private void ChangeZones(Point point, bool left, bool twice)
        {
            if (!IsError && HasVideo && Pic.ActualHeight > 0)
            {
                IgnoreMouse = false;
                if (point.Y > Pic.ActualHeight / 1.5)
                    if (left) numt.Value = Convert.ToInt32(height - ((double)height * point.Y) / Pic.ActualHeight); //Сверху
                    else numt.Value = 0;
                else if (point.Y < Pic.ActualHeight / 3)
                    if (left) numb.Value = Convert.ToInt32(((double)height * point.Y) / Pic.ActualHeight);          //Снизу
                    else numb.Value = 0;
                else if (point.X < Pic.ActualWidth / 3)
                    if (left) numl.Value = Convert.ToInt32(((double)width * point.X) / Pic.ActualWidth);            //Слева
                    else numl.Value = 0;
                else if (point.X > Pic.ActualWidth / 1.5)
                    if (left) numr.Value = Convert.ToInt32(width - ((double)width * point.X) / Pic.ActualWidth);    //Справа
                    else numr.Value = 0;
                else if (left && twice) button_fullscreen_Click(null, null);                                        //Центр - Фуллскрин
                else if (!left) cmn_settings.IsOpen = true;                                                         //Центр - настройки
            }
            else if (ErrBox.Visibility == Visibility.Visible && left && twice)
            {
                button_fullscreen_Click(null, null);
            }
        }

        private void WheelMouse(object sender, MouseWheelEventArgs e)
        {
            if (!IsError && HasVideo && Pic.ActualHeight > 0)
            {
                int d = (e.Delta < 0) ? -2 : 2;
                Point point = e.GetPosition(Pic);
                if (point.Y > Pic.ActualHeight / 1.5) numt.Value = numt.Value + d;       //Сверху
                else if (point.Y < Pic.ActualHeight / 3) numb.Value = numb.Value + d;    //Снизу
                else if (point.X < Pic.ActualWidth / 3) numl.Value = numl.Value + d;     //Слева
                else if (point.X > Pic.ActualWidth / 1.5) numr.Value = numr.Value + d;   //Справа
            }
        }

        private void button_AutoCrop_Click(object sender, RoutedEventArgs e)
        {
            if (!IsError && HasVideo)
            {
                Autocrop acrop = new Autocrop(m, this, -1);
                if (acrop.m != null)
                {
                    m = acrop.m.Clone();
                    SetValues(m.cropl, m.cropt, m.cropr, m.cropb);
                    ShowCroppedFrame();
                }
            }
        }

        private void button_AutoCrop_current_Click(object sender, RoutedEventArgs e)
        {
            if (!IsError && HasVideo)
            {
                Autocrop acrop = new Autocrop(m, this, (int)slider_pos.Value);
                if (acrop.m != null)
                {
                    m = acrop.m.Clone();
                    SetValues(m.cropl, m.cropt, m.cropr, m.cropb);
                    ShowCroppedFrame();
                }
            }
        }

        private void button_uncrop_Click(object sender, RoutedEventArgs e)
        {
            if (!IsError && HasVideo)
            {
                SetValues(0, 0, 0, 0);
                ShowCroppedFrame();
            }
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
            IgnoreMouse = true; //Чтоб кроп не срабатывал при включении Фуллскрина
        }

        private void ShowTitle()
        {
            string modw, modh;
            int cw = width - left - right, ch = height - top - bottom;

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

            Title = width + "x" + height + " -> " + cw + "x" + ch + " (" + modw + "x" + modh + ", " + cropped_s + ") | " +
                frame_of.Replace("xx", ((int)slider_pos.Value).ToString()).Replace("yy", ((int)slider_pos.Maximum).ToString());
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
            if (!IsError && HasVideo)
            {
                int new_frame = (int)slider_pos.Value + step;
                new_frame = (new_frame < 0) ? 0 : (new_frame > (int)slider_pos.Maximum) ? (int)slider_pos.Maximum : new_frame;
                if (new_frame != (int)slider_pos.Value)
                {
                    slider_pos.Tag = new object();
                    slider_pos.Value = new_frame;
                    slider_pos.Tag = null;
                    SetFrame(new_frame);
                    ShowCroppedFrame();
                }
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
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                    buffer = IntPtr.Zero;
                }
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
        }

        private void slider_color_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (((Slider)sender).IsFocused)
            {
                R = (byte)slider_R.Value;
                G = (byte)slider_G.Value;
                B = (byte)slider_B.Value;
                A = (byte)slider_A.Value;

                FinalColor.Color = Color.FromArgb(255, R, G, B);
                Settings.VCropBrush = Color.FromArgb(A, R, G, B);

                if (left != 0 || right != 0 || top != 0 || bottom != 0)
                    ShowCroppedFrame();
            }
        }

        private void button_settings_Click(object sender, RoutedEventArgs e)
        {
            if (!IsError && HasVideo && !cmn_settings.IsOpen)
            {
                //Чтоб через кнопку открывалось в центре
                cmn_settings.Placement = PlacementMode.Center;
                cmn_settings.PlacementTarget = Picture;
                cmn_settings.IsOpen = true;
            }
            else
            {
                cmn_settings.IsOpen = false;
            }
        }

        private void cmn_settings_Closed(object sender, RoutedEventArgs e)
        {
            //Возвращаем обратно открытие в точке клика
            cmn_settings.Placement = PlacementMode.MousePoint;
        }
    }
}