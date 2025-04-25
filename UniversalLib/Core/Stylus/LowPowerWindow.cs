using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniversalLib.Common;
using static UniversalLib.Core.Stylus.User32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static UniversalLib.Core.Stylus.User32.PenInputEvent;

namespace UniversalLib.Core.Stylus
{
    public class LowPowerWindow
    {
        private nint _hwnd;
        public Action<RawPenInputEventArg> PenInputRecived;
        public LowPowerWindow()
        {
            InitCreateWindow();
        }
        private WndProc _wndProcDelegate;
        private void InitCreateWindow()
        {
            // 注册窗口类
            IntPtr hModule = System.Diagnostics.Process.GetCurrentProcess().Handle;
            WNDCLASSEX wcx = new WNDCLASSEX();
            wcx.cbSize = Marshal.SizeOf(wcx);
            wcx.style = CS_HREDRAW | CS_VREDRAW;
            _wndProcDelegate = new WndProc(wndProc);
            wcx.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

            wcx.cbClsExtra = 0;
            wcx.cbWndExtra = 0;
            wcx.hInstance = hModule;
            wcx.lpszClassName = "StylusMainLowWClass";
            wcx.hIcon = IntPtr.Zero;
            wcx.hCursor = IntPtr.Zero;
            wcx.hbrBackground = IntPtr.Zero;
            wcx.lpszMenuName = null;
            wcx.hIconSm = IntPtr.Zero;
            if (!RegisterClassEx(ref wcx))
            {
                var error = Marshal.GetLastWin32Error();
                LogsHelper.Instance.DebugWrite($"[InitCreateWindow] Window created RegisterClassEx error :{error}");
                return;
            }

            IntPtr hWnd = CreateWindowEx(
                0,
                wcx.lpszClassName,
                "StylusWin32Window",
                WS_OVERLAPPEDWINDOW,
                0, 0, 0, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                 wcx.hInstance,
                IntPtr.Zero);
            if (hWnd == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                LogsHelper.Instance.DebugWrite($"[InitCreateWindow] Window created CreateWindowEx error :{error}");
                return;
            }
            LogsHelper.Instance.DebugWrite("[InitCreateWindow] Window created successfully");
            _hwnd = hWnd;
            #region RegisterPenDetector

            RawInputDevice[] rid =
            {
                new RawInputDevice
                {
                    UsagePage = HidUsagePage.DIGITIZERS,
                    Usage = HidUsage.Pen,
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

        #region stylus
        private void ProcessRawInput(IntPtr hdevice)
        {
            var hid = GetDeviceHid(hdevice);
            uint dwSize = 0; // 50  
            uint headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
            uint result = GetRawInputData(hdevice, RawInputGetBehavior.Input, null, ref dwSize, headerSize);
            if (result == 0)
            {
                //分配缓冲区来存储输入数据
                byte[] rawData = new byte[dwSize];
                //获取原始输入数据
                result = GetRawInputData(hdevice, RawInputGetBehavior.Input, rawData, ref dwSize, headerSize);
                if (result == dwSize)
                {
                    GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
                    try
                    {
                        IntPtr bufferPtr = handle.AddrOfPinnedObject();
                        //将指针转换为结构体
                        InputData rawBuffer = Marshal.PtrToStructure<InputData>(bufferPtr);
                        PenInputRecived?.Invoke(new RawPenInputEventArg(PenInputEvent.Create(rawBuffer.data.pen.data, hid.hid)));
                    }
                    finally
                    {
                        handle.Free();
                    }

                }
            }
        }
        #endregion







    }



}