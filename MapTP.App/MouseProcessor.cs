using System;
using System.Runtime.InteropServices;

namespace MapTP.App
{
    internal class MouseProcessor
    {
        #region ---- Win10 Touch Injection ----
        private const int TOUCH_FEEDBACK_DEFAULT = 0x1;
        private const int POINTER_FLAG_NONE = 0x0;
        private const int POINTER_FLAG_UPDATE = 0x002;
        private const int POINTER_FLAG_DOWN = 0x00010000;
        private const int POINTER_FLAG_UP = 0x00040000;
        private const int POINTER_FLAG_INRANGE = 0x0002;
        private const int POINTER_FLAG_INCONTACT = 0x0004;

        private enum POINTER_INPUT_TYPE
        {
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public uint touchFlags;
            public uint touchMask;
            public uint orientation;
            public uint pressure;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTER_INFO
        {
            public POINTER_INPUT_TYPE pointerType;
            public uint pointerId;
            public uint frameId;
            public IntPtr targetWindow;
            public POINT ptPixelLocation;
            public POINT ptPixelLocationRaw;
            public uint pointerFlags;
            public uint pointerData;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x, y;
            public POINT(int x, int y) { this.x = x; this.y = y; }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InitializeTouchInjection(uint maxCount, uint dwMode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InjectTouchInput(uint count, [MarshalAs(UnmanagedType.LPArray), In] POINTER_TOUCH_INFO[] contacts);

        private static readonly uint TOUCH_ID = 1;   // 我们只用单指
        private static uint frame = 0;
        private static bool _available;

        static MouseProcessor()
        {
            try
            {
                // Win10 1607+ 才支持
                var ver = Environment.OSVersion.Version;
                if (ver.Major == 10 && ver.Build >= 14393)
                    _available = InitializeTouchInjection(1, TOUCH_FEEDBACK_DEFAULT);
            }
            catch { /* 老系统直接放弃 */ }
        }
        #endregion

        #region ---- 旧 SendInput 备用 ----
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001,
                           MOUSEEVENTF_ABSOLUTE = 0x8000,
                           MOUSEEVENTF_VIRTUALDESK = 0x4000,
                           MOUSEEVENTF_LEFTDOWN = 0x0002,
                           MOUSEEVENTF_LEFTUP = 0x0004;
        #endregion

        /// <summary>
        /// 把绝对像素坐标注入成指针事件
        /// </summary>
        public void MoveCursor(int x, int y)
        {
            if (_available)
            {
                var ti = new POINTER_TOUCH_INFO();
                ti.pointerInfo.pointerType = POINTER_INPUT_TYPE.PT_TOUCH;
                ti.pointerInfo.pointerId = TOUCH_ID;
                ti.pointerInfo.frameId = ++frame;
                ti.pointerInfo.ptPixelLocation = new POINT(x, y);
                ti.pointerInfo.ptPixelLocationRaw = new POINT(x, y);
                ti.pointerInfo.pointerFlags = POINTER_FLAG_UPDATE |
                                              POINTER_FLAG_INRANGE |
                                              POINTER_FLAG_INCONTACT;
                ti.pressure = 320;   // 0-1024，随意
                ti.orientation = 90;
                ti.touchMask = 0x00000004; // TOUCH_MASK_PRESSURE
                InjectTouchInput(1, new[] { ti });
            }
            else
            {
                // 老系统回退
                var inp = new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = x,
                        dy = y,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                        time = 0
                    }
                };
                SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
            }
        }

        public void MouseDown()
        {
            if (_available)
            {
                var ti = new POINTER_TOUCH_INFO();
                ti.pointerInfo.pointerType = POINTER_INPUT_TYPE.PT_TOUCH;
                ti.pointerInfo.pointerId = TOUCH_ID;
                ti.pointerInfo.frameId = ++frame;
                ti.pointerInfo.pointerFlags = POINTER_FLAG_DOWN |
                                              POINTER_FLAG_INRANGE |
                                              POINTER_FLAG_INCONTACT;
                ti.pressure = 512;
                ti.orientation = 90;
                ti.touchMask = 0x00000004;
                InjectTouchInput(1, new[] { ti });
            }
            else
            {
                var inp = new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN }
                };
                SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
            }
        }

        public void MouseUp()
        {
            if (_available)
            {
                var ti = new POINTER_TOUCH_INFO();
                ti.pointerInfo.pointerType = POINTER_INPUT_TYPE.PT_TOUCH;
                ti.pointerInfo.pointerId = TOUCH_ID;
                ti.pointerInfo.frameId = ++frame;
                ti.pointerInfo.pointerFlags = POINTER_FLAG_UP;
                ti.pressure = 0;
                ti.orientation = 90;
                ti.touchMask = 0x00000004;
                InjectTouchInput(1, new[] { ti });
            }
            else
            {
                var inp = new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP }
                };
                SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
            }
        }
    }
}
