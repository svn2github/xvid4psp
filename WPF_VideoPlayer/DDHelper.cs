using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Security;
using System.Windows;

namespace WPF_VideoPlayer
{
    public delegate void DDEventHandler(object sender, string[] files);

    public class DDHelper
    {
        private IntPtr Handle = IntPtr.Zero;
        private bool Processing = false;
        private HwndSource HSource = null;
        public event DDEventHandler GotFiles;

        [DllImport("user32.dll", EntryPoint = "ChangeWindowMessageFilter")]
        private static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);
        [DllImport("ole32.dll", EntryPoint = "RevokeDragDrop")]
        private static extern int RevokeDragDrop(IntPtr hwnd);
        [DllImport("shell32.dll", EntryPoint = "DragAcceptFiles")]
        private static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);
        [DllImport("shell32.dll", EntryPoint = "DragFinish")]
        private static extern void DragFinish(IntPtr hDrop);
        [DllImport("shell32.dll", EntryPoint = "DragQueryFile", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] StringBuilder lpszFile, uint cch);

        public DDHelper(Window owner)
        {
            owner.Loaded += new RoutedEventHandler(Window_Loaded);
            owner.Closed += new EventHandler(Window_Closed);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper((Window)sender).Handle;

            if (Handle != IntPtr.Zero)
            {
                HSource = HwndSource.FromHwnd(Handle);
                HSource.AddHook(new HwndSourceHook(WndProc));

                if (Environment.OSVersion.Version.Major >= 6)
                {
                    //Обходим UAC-фильтр
                    ChangeWindowMessageFilter(0x0233, 1); //WM_DROPFILES
                    ChangeWindowMessageFilter(0x004A, 1); //WM_COPYDATA
                    ChangeWindowMessageFilter(0x0049, 1); //WM_COPYGLOBALDATA
                }

                RevokeDragDrop(Handle);        //Выключаем OLE D&D (ВАЖНО!!!)
                DragAcceptFiles(Handle, true); //Включаем Win32 D&D
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (Handle != IntPtr.Zero)
            {
                DragAcceptFiles(Handle, false);
                if (HSource != null)
                {
                    HSource.RemoveHook(new HwndSourceHook(WndProc));
                    HSource = null;
                }
                Handle = IntPtr.Zero;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0233: //WM_DROPFILES
                    {
                        if (!Processing)
                        {
                            Processing = true;

                            try
                            {
                                //Кол-во файлов
                                uint count = DragQueryFile(wParam, 0xFFFFFFFF, null, 0);
                                string[] files = new string[count];

                                for (uint i = 0; i < count; i++)
                                {
                                    //Длина строки
                                    uint size = DragQueryFile(wParam, i, null, 0);
                                    StringBuilder sb = new StringBuilder((int)size);

                                    //Сама строка
                                    DragQueryFile(wParam, i, sb, size + 1);
                                    files[i] = sb.ToString();
                                }

                                if (files.Length > 0 && GotFiles != null)
                                {
                                    GotFiles(this, files);
                                }
                            }
                            catch (Exception) { }

                            Processing = false;
                        }

                        DragFinish(wParam);
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }
    }
}

