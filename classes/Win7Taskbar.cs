using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XviD4PSP
{
    public enum TBPF
    {
        NOPROGRESS = 0,
        INDETERMINATE = 0x1,
        NORMAL = 0x2,
        ERROR = 0x4,
        PAUSED = 0x8
    }

    public enum TBATF
    {
        USEMDITHUMBNAIL = 0x1,
        USEMDILIVEPREVIEW = 0x2
    }

    public enum THB : uint
    {
        BITMAP = 0x1,
        ICON = 0x2,
        TOOLTIP = 0x4,
        FLAGS = 0x8
    }

    public enum THBF : uint
    {
        ENABLED = 0,
        DISABLED = 0x1,
        DISMISSONCLICK = 0x2,
        NOBACKGROUND = 0x4,
        HIDDEN = 0x8
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct THUMBBUTTON
    {
        public THB dwMask;
        public uint iId;
        public uint iBitmap;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szTip;
        public THBF dwFlags;
    }

    [ComImport]
    [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList3
    {
        //ITaskbarList
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);

        //ITaskbarList2
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        //ITaskbarList3
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TBPF tbpFlags);
        void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        void UnregisterTab(IntPtr hwndTab);
        void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, TBATF tbatFlags);
        void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);
        void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButtons);
        void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
        void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
        void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
        void SetThumbnailClip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] System.Drawing.Rectangle prcClip);
    }

    [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterfaceAttribute(ClassInterfaceType.None)]
    [ComImportAttribute()]
    internal class CTaskbarList { }
///////////////////////////////////
    public static class Win7Taskbar
    {
        private static ITaskbarList3 TaskbarList;

        private static bool _IsInitialized = false;
        public static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public static bool InitializeWin7Taskbar()
        {
            if (TaskbarList == null)
            {
                try
                {
                    TaskbarList = (ITaskbarList3)new CTaskbarList();
                    TaskbarList.HrInit();
                }
                catch
                {
                    if (TaskbarList != null)
                    {
                        try { Marshal.ReleaseComObject(TaskbarList); }
                        catch { }
                        finally { TaskbarList = null; }
                    }
                }
            }

            return (_IsInitialized = (TaskbarList != null));
        }

        public static void UninitializeWin7Taskbar()
        {
            //Отключаем вывод прогресса
            _IsInitialized = false;

            if (TaskbarList != null)
            {
                try
                {
                    //Сбрасываем прогресс везде где он мог быть (т.е. во всех активных окнах)
                    foreach (System.Windows.Window wnd in System.Windows.Application.Current.Windows)
                        TaskbarList.SetProgressState(new System.Windows.Interop.WindowInteropHelper(wnd).Handle, TBPF.NOPROGRESS);
                }
                catch { }

                try { Marshal.ReleaseComObject(TaskbarList); }
                catch { }
                finally { TaskbarList = null; }
            }
        }

        public static void SetProgressState(IntPtr hwnd, TBPF state)
        {
            try
            {
                if (IsInitialized)
                    TaskbarList.SetProgressState(hwnd, state);
            }
            catch { }
        }

        public static void SetProgressValue(IntPtr hwnd, ulong current, ulong maximum)
        {
            try
            {
                if (IsInitialized)
                    TaskbarList.SetProgressValue(hwnd, current, maximum);
            }
            catch { }
        }

        //Шкала, полностью закрашенная соответствующим цветом
        public static void SetProgressTaskComplete(IntPtr hwnd, TBPF state)
        {
            try
            {
                if (IsInitialized)
                {
                    TaskbarList.SetProgressState(hwnd, state);
                    TaskbarList.SetProgressValue(hwnd, 100, 100);
                }
            }
            catch { }
        }

        //"Неопределенный" прогресс для окна
        public static void SetProgressIndeterminate(System.Windows.Window wnd)
        {
            try
            {
                if (IsInitialized)
                    SetProgressState(new System.Windows.Interop.WindowInteropHelper(wnd).Handle, TBPF.INDETERMINATE);
            }
            catch { }
        }

        //"Неопределенный" прогресс для окна, так-же возвращает Handle этого окна
        public static void SetProgressIndeterminate(System.Windows.Window wnd, ref IntPtr Handle)
        {
            try
            {
                if (IsInitialized)
                {
                    Handle = new System.Windows.Interop.WindowInteropHelper(wnd).Handle;
                    SetProgressState(Handle, TBPF.INDETERMINATE);
                }
            }
            catch { }
        }
    }
}
