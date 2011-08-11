using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Security;

namespace WPF_VideoPlayer
{
    public class VideoHwndHost : HwndHost
    {
        public VideoHwndHost()
        {
        }

        private IntPtr Parent_Handle;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int HOST_ID = 0x00000002;
        public event EventHandler RepaintRequired;

        [DllImport("user32.dll")]
        internal static extern int GetDoubleClickTime();
        private int interval = GetDoubleClickTime();
        private DateTime last_click = DateTime.Now;

        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateWindowEx(int dwExStyle, string lpszClassName, string lpszWindowName, int style, int x, int y,
            int width, int height, IntPtr hwndParent, IntPtr hMenu, IntPtr hInst, [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        internal static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern int SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        protected override HandleRef BuildWindowCore(HandleRef hwndParent) //Static MDIClient Message
        {
            Parent_Handle = hwndParent.Handle;
            IntPtr hwndHost = CreateWindowEx(0, "Message", "", WS_CHILD | WS_VISIBLE,
                0, 0, 0, 0, hwndParent.Handle, (IntPtr)HOST_ID, IntPtr.Zero, 0);

            if (hwndHost == IntPtr.Zero)
                throw new Exception("CreateVideoWindow: CreateWindowEx() failed to create a new window!");

            return new HandleRef(this, hwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0201: //0x0201 WM_LBUTTONDOWN (0x0202 WM_LBUTTONUP, 0x0203 WM_LBUTTONDBLCLK)
                    {
                        //Определяем даблклик для Fullscreen`а (по-хорошему надо было создавать свой 
                        //класс окна, в нём указать CS_DBLCLKS и ловить уже готовый WM_LBUTTONDBLCLK)
                        double diff = (DateTime.Now - last_click).TotalMilliseconds;
                        if (diff < interval)
                        {
                            SendMessage(Parent_Handle, 0x0203, wParam, lParam);
                            last_click = DateTime.MinValue;
                        }
                        else
                            last_click = DateTime.Now;

                        handled = true;
                        break;
                    }
                case 0x0205: //0x0205 WM_RBUTTONUP (0x0204 WM_RBUTTONDOWN, 0x0206 WM_RBUTTONDBLCLK)
                    {
                        //Правый клик
                        SendMessage(Parent_Handle, msg, wParam, lParam);
                        handled = true;
                        break;
                    }
                case 0x0F: //0x0F WM_PAINT
                    {
                        //Необходимо перерисовать окно
                        if (RepaintRequired != null)
                            RepaintRequired(this, new EventArgs());

                        handled = false;
                        break;
                    }
            }
            return IntPtr.Zero;
        }
    }

    //Вырезано из "Media Foundation .NET" http://sourceforge.net/projects/mfnet/ (GNU tarball от 05.Aug.2011).
    //Из всей библиотеки оставлено только то, что нужно для запуска EVR в простейшем его виде и для обработки кодов ошибок.
    #region Media Foundation .NET
    #region Original license
    /*MediaFoundationLib - Provide access to MediaFoundation interfaces via .NET
    Copyright (C) 2007
    http://mfnet.sourceforge.net

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA*/
    #endregion

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Class)]
    public class UnmanagedNameAttribute : System.Attribute
    {
        private string m_Name;

        public UnmanagedNameAttribute(string s)
        {
            m_Name = s;
        }

        public override string ToString()
        {
            return m_Name;
        }
    }

    [UnmanagedName("CLSID_EnhancedVideoRenderer"),
    ComImport,
    Guid("FA10746C-9B63-4b6c-BC49-FC300EA5F256")]
    public class EnhancedVideoRenderer
    {
    }

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("FA993888-4383-415A-A930-DD472A8CF6F7")]
    public interface IMFGetService
    {
        [PreserveSig]
        int GetService(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidService,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppvObject
            );
    }

    public static class MFServices
    {
        public static readonly Guid MR_VIDEO_RENDER_SERVICE = new Guid(0x1092a86c, 0xab1a, 0x459a, 0xa3, 0x36, 0x83, 0x1f, 0xbc, 0x4d, 0x11, 0xff);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4), UnmanagedName("MFVideoNormalizedRect")]
    public class MFVideoNormalizedRect
    {
        public float left;
        public float top;
        public float right;
        public float bottom;

        public MFVideoNormalizedRect()
        {
        }

        public MFVideoNormalizedRect(float l, float t, float r, float b)
        {
            left = l;
            top = t;
            right = r;
            bottom = b;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class MFRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public MFRect()
        {
        }

        public MFRect(int l, int t, int r, int b)
        {
            left = l;
            top = t;
            right = r;
            bottom = b;
        }

        public MFRect(Rectangle rectangle)
        {
            left = rectangle.Left;
            top = rectangle.Top;
            right = rectangle.Right;
            bottom = rectangle.Bottom;
        }
    }

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("A490B1E4-AB84-4D31-A1B2-181E03B1077A"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMFVideoDisplayControl
    {
        [PreserveSig]
        int GetNativeVideoSize(
            /*[Out]*/ out Size pszVideo,
            /*[Out]*/ out Size pszARVideo
            );

        [PreserveSig]
        int GetIdealVideoSize(
            /*[Out]*/ out Size pszVideo,
            /*[Out]*/ out Size pszARVideo
            );

        [PreserveSig]
        int SetVideoPosition(
            [In] MFVideoNormalizedRect pnrcSource,
            [In] MFRect prcDest
            );

        [PreserveSig]
        int GetVideoPosition(
            /*[Out]*/ out MFVideoNormalizedRect pnrcSource,
            /*[Out]*/ out MFRect prcDest
            );

        [PreserveSig]
        int SetAspectRatioMode(
            [In] MFVideoAspectRatioMode dwAspectRatioMode
            );

        [PreserveSig]
        int GetAspectRatioMode(
            out MFVideoAspectRatioMode pdwAspectRatioMode
            );

        [PreserveSig]
        int SetVideoWindow(
            [In] IntPtr hwndVideo
            );

        [PreserveSig]
        int GetVideoWindow(
            out IntPtr phwndVideo
            );

        [PreserveSig]
        int RepaintVideo();

        [PreserveSig]
        int SetBorderColor(
            [In] int Clr
            );

        [PreserveSig]
        int GetBorderColor(
            out int pClr
            );

        [PreserveSig]
        int SetRenderingPrefs(
            [In] MFVideoRenderPrefs dwRenderFlags
            );

        [PreserveSig]
        int GetRenderingPrefs(
            out MFVideoRenderPrefs pdwRenderFlags
            );

        [PreserveSig]
        int SetFullscreen(
            [In, MarshalAs(UnmanagedType.Bool)] bool fFullscreen
            );

        [PreserveSig]
        int GetFullscreen(
            [MarshalAs(UnmanagedType.Bool)] out bool pfFullscreen
            );
    }

    [Flags, UnmanagedName("MFVideoAspectRatioMode")]
    public enum MFVideoAspectRatioMode
    {
        None = 0x00000000,
        PreservePicture = 0x00000001,
        PreservePixel = 0x00000002,
        NonLinearStretch = 0x00000004,
        Mask = 0x00000007
    }

    [Flags, UnmanagedName("MFVideoRenderPrefs")]
    public enum MFVideoRenderPrefs
    {
        None = 0,
        DoNotRenderBorder = 0x00000001,
        DoNotClipToDevice = 0x00000002,
        AllowOutputThrottling = 0x00000004,
        ForceOutputThrottling = 0x00000008,
        ForceBatching = 0x00000010,
        AllowBatching = 0x00000020,
        ForceScaling = 0x00000040,
        AllowScaling = 0x00000080,
        DoNotRepaintOnStop = 0x00000100,
        Mask = 0x000001ff,
    }

    public static class MFError
    {
        #region externs

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern int FormatMessage(FormatMessageFlags dwFlags, IntPtr lpSource,
            int dwMessageId, int dwLanguageId, out IntPtr lpBuffer, int nSize, IntPtr[] Arguments);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW", ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, LoadLibraryExFlags dwFlags);

        [DllImport("kernel32.dll", ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hFile);

        [DllImport("kernel32.dll", ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LocalFree(IntPtr hMem);

        #endregion

        #region Declarations

        [Flags, UnmanagedName("#defines in WinBase.h")]
        private enum LoadLibraryExFlags
        {
            DontResolveDllReferences = 0x00000001,
            LoadLibraryAsDataFile = 0x00000002,
            LoadWithAlteredSearchPath = 0x00000008,
            LoadIgnoreCodeAuthzLevel = 0x00000010
        }

        [Flags, UnmanagedName("FORMAT_MESSAGE_* defines")]
        private enum FormatMessageFlags
        {
            AllocateBuffer = 0x00000100,
            IgnoreInserts = 0x00000200,
            FromString = 0x00000400,
            FromHmodule = 0x00000800,
            FromSystem = 0x00001000,
            ArgumentArray = 0x00002000,
            MaxWidthMask = 0x000000FF
        }

        #endregion

        private static IntPtr s_hModule = IntPtr.Zero;
        private const string MESSAGEFILE = "mferror.dll";

        public static string GetErrorText(int hr)
        {
            string sRet = null;
            int dwBufferLength;
            IntPtr ip = IntPtr.Zero;

            FormatMessageFlags dwFormatFlags =
                FormatMessageFlags.AllocateBuffer |
                FormatMessageFlags.IgnoreInserts |
                FormatMessageFlags.FromSystem |
                FormatMessageFlags.MaxWidthMask;

            // Scan both the Windows Media library, and the system library looking for the message
            dwBufferLength = FormatMessage(
                dwFormatFlags,
                s_hModule, // module to get message from (NULL == system)
                hr, // error number to get message for
                0, // default language
                out ip,
                0,
                null
                );

            // Not a system message.  In theory, you should be able to get both with one call.  In practice (at
            // least on my 64bit box), you need to make 2 calls.
            if (dwBufferLength == 0)
            {
                if (s_hModule == IntPtr.Zero)
                {
                    // Load the Media Foundation error message dll
                    s_hModule = LoadLibraryEx(MESSAGEFILE, IntPtr.Zero, LoadLibraryExFlags.LoadLibraryAsDataFile);
                }

                if (s_hModule != IntPtr.Zero)
                {
                    // If the load succeeds, make sure we look in it
                    dwFormatFlags |= FormatMessageFlags.FromHmodule;

                    // Scan both the Windows Media library, and the system library looking for the message
                    dwBufferLength = FormatMessage(
                        dwFormatFlags,
                        s_hModule, // module to get message from (NULL == system)
                        hr, // error number to get message for
                        0, // default language
                        out ip,
                        0,
                        null
                        );
                }
            }

            try
            {
                // Convert the returned buffer to a string.  If ip is null (due to not finding
                // the message), no exception is thrown.  sRet just stays null.  The
                // try/finally is for the (remote) possibility that we run out of memory
                // creating the string.
                sRet = Marshal.PtrToStringUni(ip);
            }
            finally
            {
                // Cleanup
                if (ip != IntPtr.Zero)
                {
                    LocalFree(ip);
                }
            }

            return sRet;
        }

        public static void ThrowExceptionForHR(int hr)
        {
            // If a severe error has occurred
            if (hr < 0)
            {
                string s = GetErrorText(hr);

                // If a string is returned, build a COM error from it
                if (s != null)
                {
                    throw new COMException(s, hr);
                }
                else
                {
                    // No string, just use standard com error
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }
    }
    #endregion
}

