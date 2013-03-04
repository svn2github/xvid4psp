using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace XviD4PSP
{
    public class Win32
    {
        //Private Declare Function EnumChildWindows Lib "USER32.DLL" (ByVal hWndParent As System.IntPtr, ByVal lpEnumFunc As EnumWindowsDelegate, ByVal lParam As Integer) As Integer

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern int GetWindowTextLength(HandleRef hWnd);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern int GetWindowText(HandleRef hWnd, StringBuilder lpString, int nMaxCount);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern bool SetWindowText(int hwnd, string str);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern bool IsWindow(int hwnd);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern bool IsWindowVisible(int hwnd);

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern bool EnumWindows(Wnd Parent, int lParam);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern int GetWindow(int hwnd, uint lParam);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern int FindWindow(string className, string windowName);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern int ShowWindow(int hwnd, int command);
        ////[DllImport("user32.dll", CharSet = CharSet.Auto)]
        ////public static extern int SendMessage(int hwnd, uint msg, int wparam, int lparam);
        ////[DllImport("user32.dll", CharSet = CharSet.Auto)]
        ////public static extern int GetSystemMetrics(int Index);
        //public delegate bool Wnd(int hwnd, int lParam);

        // The WM_COMMAND message is sent when the user selects a command item from a menu, 
        // when a control sends a notification message to its parent window, or when an 
        // accelerator keystroke is translated.
        public const int WM_COMMAND = 0x111;

        public const int GW_HWNDNEXT = 2;
        public const int GW_HWNDPREV = 3;
        public const int GW_CHILD = 5;
        public const int MF_BYPOSITION = 0x400;

        [DllImport("User32.dll")]
        public static extern int GetDesktopWindow();
        [DllImport("User32.dll")]
        public static extern int GetTopWindow(int hwndParent);
        [DllImport("User32.dll")]
        public static extern int GetWindow(int hwndSibling, int wFlag);
        [DllImport("User32.dll")]
        public static extern int GetWindowText(int hWnd, System.Text.StringBuilder text, int count);
        [DllImport("User32.dll")]
        public static extern UInt32 RealGetWindowClass(int hWnd, System.Text.StringBuilder text, UInt32 count);
        [DllImport("User32.dll")]
        public static extern int SetParent(int hWndChild, int hWndNewParent);
        [DllImport("User32.dll")]
        public static extern int GetMenu(int hWnd);
        [DllImport("User32.dll")]
        public static extern int GetSubMenu(int hMenu, int nPos);
        [DllImport("User32.dll")]
        public static extern uint GetMenuItemID(int hMenu, int nPos);
        [DllImport("User32.dll")]
        public static extern int GetMenuItemCount(int hMenu);
        [DllImport("User32.dll")]
        public static extern int GetMenuString(int hMenu, uint uIDItem, StringBuilder lpString, int nMaxCount, uint uFlag);

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // The FindWindow function retrieves a handle to the top-level window whose class name
        // and window name match the specified strings. This function does not search child windows.
        // This function does not perform a case-sensitive search.
        [DllImport("User32.dll")]
        public static extern int FindWindow(string strClassName, string strWindowName);

        // The FindWindowEx function retrieves a handle to a window whose class name 
        // and window name match the specified strings. The function searches child windows, beginning
        // with the one following the specified child window. This function does not perform a case-sensitive search.
        [DllImport("User32.dll")]
        public static extern int FindWindowEx(int hwndParent, int hwndChildAfter, string strClassName, string strWindowName);

        // The SendMessage function sends the specified message to a 
        // window or windows. It calls the window procedure for the specified 
        // window and does not return until the window procedure has processed the message. 
        [DllImport("User32.dll")]
        public static extern Int32 SendMessage(
            int hWnd,               // handle to destination window
            int Msg,                // message
            int wParam,             // first message parameter
            [MarshalAs(UnmanagedType.LPStr)] string lParam); // second message parameter

        [DllImport("User32.dll")]
        public static extern Int32 SendMessage(
            int hWnd,               // handle to destination window
            int Msg,                // message
            int wParam,             // first message parameter
            int lParam);			// second message parameter

        public static int GetWindowHandle(string windowtext)
        {
            // start with the top window on the Desktop	
            int window_handle = GetTopWindow(GetDesktopWindow());
            StringBuilder text_stringBuilder = new StringBuilder(0x20);
            StringBuilder class_stringBuilder = new StringBuilder(0x20);
            //string Class_string = "WMPlayerApp";
            try
            {
                while (true)
                {
                    //look at each Desktop window...
                    GetWindowText(window_handle, text_stringBuilder, 0x20);
                    if (text_stringBuilder.ToString().StartsWith(windowtext))
                    {
                        return window_handle;
                    }
                    if ((window_handle = GetWindow(window_handle, GW_HWNDNEXT)) == 0)
                    {
                        return 0;
                    }
                }
            }
            catch (Exception) { return 0; }
        }

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        public static bool CopyToClipboard(string text)
        {
            bool ok = false;
            if (OpenClipboard(IntPtr.Zero))
            {
                IntPtr pText = IntPtr.Zero;
                try
                {
                    if (EmptyClipboard())
                    {
                        pText = Marshal.StringToHGlobalUni(text);          //StringToHGlobalAnsi
                        ok = (SetClipboardData(13, pText) != IntPtr.Zero); //1 - CF_TEXT, 13 - CF_UNICODETEXT
                    }
                }
                finally
                {
                    CloseClipboard();
                    if (!ok && pText != IntPtr.Zero)
                        Marshal.FreeHGlobal(pText);
                }
            }
            return ok;
        }
    }
}
