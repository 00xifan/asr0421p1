using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Protection.PlayReady;
using static UniversalLib.Common.Win32Helper;

namespace UniversalLib.Common
{
    public class WindowManagerHelper
    {
        private static readonly Lazy<WindowManagerHelper> _instanceLock = new Lazy<WindowManagerHelper>(() => new WindowManagerHelper());
        public static WindowManagerHelper Instance
        {
            get
            {
                return _instanceLock.Value;
            }
        }

        public void SetNoActivateWindow(IntPtr currentHWnd)
        {
            int exStyle = GetWindowLong(currentHWnd, GWL_EXSTYLE);
           int ffg= SetWindowLong(currentHWnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }
        /// <summary>
        /// temp set window topmost  cancelling topmost
        /// </summary>
        /// <param name="hWnd"></param>
        public async void SetNonRealTimeTopMost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            await Task.Delay(10);
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        public void SetLongTopmost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
        public void SetNotTopmost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        public void SetWindowCusStyle(IntPtr hWnd)
        {
            int extendedStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED);
        }
        /// <summary>
        /// set Borderless, no title bar, no resizing window
        /// </summary>
        /// <param name="hWnd"></param>
        public void SewtNotThickRame(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME);
            SetWindowLong(hWnd, GWL_STYLE, style);
        }

        public bool IsMouseInsideWindow(IntPtr hWnd, Windows.Foundation.Point mousePosition)
        {
            if (GetWindowRect(hWnd, out Rect windowRect))
            {
                var xoff = mousePosition.X >= windowRect.Left && mousePosition.X <= windowRect.Right;
                var yoff = mousePosition.Y >= windowRect.Top && mousePosition.Y <= windowRect.Bottom;
                if (xoff && yoff)
                {
                    return true;
                }
            }
            return false;
        }

        public bool SetForeGroundWindowInternal(IntPtr hwnd)
        {
            uint lockTimeOut = 0;
            IntPtr foregrondHwnd = GetForegroundWindow();

            var dwThisTID = GetCurrentThreadId();
            uint pid = 0;
            var id = GetWindowThreadProcessId(foregrondHwnd, out pid);
            if (pid != dwThisTID)
            {
                AttachThreadInput(dwThisTID, pid, true);
                bool b = SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, lockTimeOut, 0);
                bool br = SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, 0, SPIF_SENDWININICHANGE | SPIF_UPDATEINIFILE);
                AllowSetForegroundWindow(dwThisTID);
            }
            if (pid != dwThisTID)
            {
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, lockTimeOut, SPIF_SENDWININICHANGE | SPIF_UPDATEINIFILE);
                AttachThreadInput(dwThisTID, pid, false);
            }
            return true;
        }



        #region  windows api
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_EX_LAYERED = 0x00080000;


        private const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
        private const uint SPIF_SENDWININICHANGE = 0x0002;
        private const uint SPIF_UPDATEINIFILE = 0x0001;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
         
        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern bool AllowSetForegroundWindow(uint dwProcessId);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SystemParametersInfo(uint action, uint param, uint vparam, uint init);

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "SetForegroundWindow", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);


        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);
        #endregion
    }
}
