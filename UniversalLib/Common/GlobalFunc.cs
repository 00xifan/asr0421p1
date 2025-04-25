// Copyright (c) 2025-present Lenovo.  All rights reserverd
// Confidential and Restricted
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace UniversalLib.Common
{
    public class GlobalFunc
    {
        public static IntPtr CreateWindow(string wndClassName, string wndWindowName, Win32Helper.WndProc wndProc)
        {
            IntPtr hModule = System.Diagnostics.Process.GetCurrentProcess().Handle;
            Win32Helper.WNDCLASSEX wcx = new Win32Helper.WNDCLASSEX();
            wcx.cbSize = Marshal.SizeOf(wcx);
            wcx.style = Win32Helper.CS_HREDRAW | Win32Helper.CS_VREDRAW;
            IntPtr address = Marshal.GetFunctionPointerForDelegate((Delegate)wndProc);
            wcx.lpfnWndProc = address;
            wcx.cbClsExtra = 0;
            wcx.cbWndExtra = 0;
            wcx.hInstance = hModule;
            wcx.lpszClassName = wndClassName;
            if (!Win32Helper.RegisterClassEx(ref wcx))
            {
                var error = Marshal.GetLastWin32Error();
                LogsHelper.Instance.DebugWrite($"LockScreenStatusHelper CreateWindow Window created RegisterClassEx error :{error}");
                return IntPtr.Zero;
            }

            IntPtr _hWnd = Win32Helper.CreateWindowEx(
                0,
                wcx.lpszClassName,
                wndWindowName,
                Win32Helper.WS_OVERLAPPEDWINDOW,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                 wcx.hInstance,
                IntPtr.Zero);
            if (_hWnd == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                LogsHelper.Instance.DebugWrite($"LockScreenStatusHelper CreateWindow Window created CreateWindowEx error :{error}");
                return IntPtr.Zero;
            }
            LogsHelper.Instance.DebugWrite("LockScreenStatusHelper Window created successfully");
            return _hWnd;
        }

        private static bool IsLockSession()
        {
            uint dwSessionID = Win32Helper.WTSGetActiveConsoleSessionId();
            uint dwBytesReturned = 0;
            int dwFlags = 0;
            IntPtr pInfo = IntPtr.Zero;
            Win32Helper.WTSQuerySessionInformationW(IntPtr.Zero, dwSessionID, Win32Helper.WTS_INFO_CLASS.WTSSessionInfoEx, ref pInfo, ref dwBytesReturned);
            var shit = Marshal.PtrToStructure<Win32Helper.WTSINFOEXW>(pInfo);
            if (shit.Level == 1)
            {
                dwFlags = shit.Data.WTSInfoExLevel1.SessionFlags;
            }
            if (dwFlags == 1)
            {
                return false;
            }
            return true;
        }
        private static bool IsIdleState()
        {
            try
            {
                int tempPid = 0;
                IntPtr hwnd = Win32Helper.GetForegroundWindow();
                Win32Helper.GetWindowThreadProcessId(hwnd, out tempPid);
                IntPtr h_Process = Win32Helper.OpenProcess(Win32Helper.PROCESS_QUERY_INFORMATION | Win32Helper.PROCESS_VM_READ, false, tempPid);//process name
                try
                {
                    if (hwnd == IntPtr.Zero)
                    {
                        return false;
                    }
                    char[] buf = new char[65535];
                    UInt32 len = 65535;
                    Win32Helper.QueryFullProcessImageName(h_Process, 0, buf, ref len);
                    string path = new string(buf, 0, (int)len);
                    string exeName = Path.GetFileNameWithoutExtension(path);

                    //CheckMarx: Cannot detect the existence of a process solely by its name
                    if (exeName.ToLower() == "LockApp".ToLower())
                    {
                        return true;
                    }
                }
                finally
                {
                    Win32Helper.CloseHandle(h_Process);
                }
            }
            catch (Exception ex)
            {
                LogsHelper.Instance.ErrorWrite($"IsIdleState error {ex.Message}");
            }
            return false;
        }

        private static bool IsLogonUI()
        {
            return Process.GetProcessesByName("LogonUI")?.Length != 0;
        }

        public static bool IsLockSessionState()
        {
            if(IsLockSession() || IsIdleState() || IsLogonUI())
            {
                LogsHelper.Instance.DebugWrite("IsLockSessionState lock");
                return true;
            }
            LogsHelper.Instance.DebugWrite("IsLockSessionState unlock");
            return false;
        }
    }
}
