using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniversalLib.Common;
using UniversalLib.Core.Stylus;
using static UniversalLib.Core.Stylus.User32;
namespace UniversalLib.Core.Mouse
{

    internal class RawInputMouseReceiverWindow
    {
        private nint _hwnd;
        public Action<Rawmouse> Input;
        public RawInputMouseReceiverWindow()
        {
            InitCreateWindow();
        }
        public const int WM_INPUT = 0x00FF;
        private IntPtr wndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_INPUT:
                    ProcessRawInput(lParam);
                    return IntPtr.Zero;
                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        [StructLayout(LayoutKind.Explicit,CharSet=CharSet.Ansi,Pack =1)]
        internal struct Rawmouse
        {
            [FieldOffset(0)]
            public ushort usFlags;
            [FieldOffset(4)]
            public uint ulButtons;
            [FieldOffset(4)]
            public ushort usButtonFlags;
            [FieldOffset(6)]
            public short usButtonData;
            [FieldOffset(8)]
            public uint ulRawButtons;
            [FieldOffset(12)]
            public int lLastX;
            [FieldOffset(16)]
            public int lLastY;
            [FieldOffset(20)]
            public uint ulExtraInformation;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct Rawinput
        {
            public RAWINPUTHEADER header;
            public RAWINPUTDATA data;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }


        [StructLayout(LayoutKind.Explicit)]
        internal struct RAWINPUTDATA
        {
            [FieldOffset(0)]
            public Rawmouse mouse;

            [FieldOffset(0)]
            public Rawkeyboard keyboard;

            [FieldOffset(0)]
            public Rawhid hid;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct Rawkeyboard
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct Rawhid
        {
            public uint dwSizeHid;
            public uint dwCount;
            // 这里根据需要添加 HID 设备的数据解析
        }
        private const uint RID_INPUT = 0x10000003;
        internal enum RawInputType : uint
        {
            MOUSE = 0,
            KEYBOARD = 1,
            HID = 2,
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
    IntPtr hRawInput,
    uint uiCommand,
    IntPtr pData,
    ref uint pcbSize,
    uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
    IntPtr hRawInput,
    uint uiCommand,
    [Out] byte[] pData,
    ref uint pcbSize,
    uint cbSizeHeader);
        private void ProcessRawInput(IntPtr hdevice)
        {
            uint dwSize = 0;
            uint headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();

            // 第一次调用，获取数据大小
            if (GetRawInputData(hdevice, RID_INPUT, IntPtr.Zero, ref dwSize, headerSize) != 0)
            {
                // 处理错误情况
                return;
            }
            // 分配内存来接收数据
            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);

            try
            {
                // 第二次调用，获取实际数据
                if (GetRawInputData(hdevice, RID_INPUT, buffer, ref dwSize, headerSize) != dwSize)
                {
                    // 处理错误情况
                    return;
                }

                // 将未托管内存中的数据转换为托管结构体
                Rawinput raw = Marshal.PtrToStructure<Rawinput>(buffer);

                if (raw.header.dwType == (uint)RawInputType.MOUSE)
                {
                    var rawMouse = raw.data.mouse;
                    Input?.Invoke(rawMouse);
                }
            }
            finally
            {
                // 释放分配的内存
                Marshal.FreeHGlobal(buffer);
            }


        }
        private WndProc _wndProcDelegate;
        private void InitCreateWindow()
        {
            // 注册窗口类
            IntPtr hModule = System.Diagnostics.Process.GetCurrentProcess().Handle;
            WNDCLASSEX wcx = new WNDCLASSEX();
            wcx.cbSize = Marshal.SizeOf(wcx);
            wcx.style = CS_HREDRAW | CS_VREDRAW;
            _wndProcDelegate=new WndProc(wndProc);
            wcx.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            wcx.cbClsExtra = 0;
            wcx.cbWndExtra = 0;
            wcx.hInstance = hModule;
            wcx.lpszClassName = "MainWClass";
            wcx.hIcon = IntPtr.Zero;
            wcx.hCursor = IntPtr.Zero;
            wcx.hbrBackground = IntPtr.Zero;
            wcx.lpszMenuName = null;
            wcx.hIconSm = IntPtr.Zero;

            if (!RegisterClassEx(ref wcx))
            {
                var error = Marshal.GetLastWin32Error();
                LogsHelper.Instance.DebugWrite($"[RawInputMouseReceiverWindow][InitCreateWindow] Window created RegisterClassEx error :{error}");
                return;
            }

            IntPtr hWnd = CreateWindowEx(
                0,
                wcx.lpszClassName,
                "MouseWin32Window",
                WS_OVERLAPPEDWINDOW,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                 wcx.hInstance,
                IntPtr.Zero);
            if (hWnd == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                LogsHelper.Instance.DebugWrite($"[RawInputMouseReceiverWindow][InitCreateWindow] Window created CreateWindowEx error :{error}");
                return;
            }
            LogsHelper.Instance.DebugWrite("[InitCreateWindow] Window created successfully");
            _hwnd = hWnd; // 保存窗口句柄
            #region RegisterPenDetector

            RawInputDevice[] rid =
            {
                new RawInputDevice
                {
                    UsagePage = HidUsagePage.GENERIC,
                    Usage = HidUsage.Mouse,
                    Flags= RawInputDeviceFlags.INPUTSINK,
                    Target =hWnd
                },
            };
            bool success = RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RawInputDevice>());
            if (!success)
            {
                uint error = GetLastError();
                LogsHelper.Instance.DebugWrite($"[InitCreateWindow] RegisterRawInputDevices failed with error code: {error}");
            }

            #endregion

        }
        public enum DataCommand : uint
        {
            RID_HEADER = 0x10000005, // Get the header information from the RAWINPUT structure.
            RID_INPUT = 0x10000003   // Get the raw data from the RAWINPUT structure.
        }

        #region create window
        public nint Hwnd => _hwnd;


        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();



        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const int CS_HREDRAW = 0x0002;
        private const int CS_VREDRAW = 0x0001;
        private const int WM_NCCREATE = 0x0081;

        public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterClassEx")]
        private static extern bool RegisterClassEx(ref WNDCLASSEX lpwcx);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx")]
        public static extern IntPtr CreateWindowEx(
           int dwExStyle,
           //UInt16 regResult,
           [MarshalAs(UnmanagedType.LPStr)]
       string lpClassName,
           [MarshalAs(UnmanagedType.LPStr)]
       string lpWindowName,
           UInt32 dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion









    }
}
