using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MapTP.App
{
    internal class MouseProcessor
    {

        #region Win32
        // from https://zhuanlan.zhihu.com/p/626326773
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        // Useful constants for readability
        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000; // May be ignored on older Windows
        const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        #endregion

        // Simple smoothing to improve perceived pointer stability
        private int _lastRawX = -1;
        private int _lastRawY = -1;
        private int _lastOutX = -1;
        private int _lastOutY = -1;

        // Public knobs (can be wired to settings later if needed)
        public bool EnableSmoothing { get; set; } = true;

        // range: 0..1. closer to 0 -> smoother/slower, closer to 1 -> snappier
        public double SmoothFactor { get; set; } = 0.35;

        // Deadzone in normalized absolute units (0..65535). Small deltas are ignored.
        public int Deadzone { get; set; } = 1;

        public void MoveCursor(int x, int y)
        {
            // Remember raw (requested) position
            int rawX = x;
            int rawY = y;

            // Initialize last values on first call
            if (_lastOutX < 0 || _lastOutY < 0)
            {
                _lastRawX = rawX;
                _lastRawY = rawY;
                _lastOutX = rawX;
                _lastOutY = rawY;
            }

            // Early out if movement is within deadzone (reduces jitter)
            if (Math.Abs(rawX - _lastRawX) <= Deadzone && Math.Abs(rawY - _lastRawY) <= Deadzone)
            {
                _lastRawX = rawX;
                _lastRawY = rawY;
                return;
            }

            int outX = rawX;
            int outY = rawY;

            if (EnableSmoothing)
            {
                // Exponential moving average
                double a = Math.Min(1.0, Math.Max(0.0, SmoothFactor));
                outX = (int)Math.Round(_lastOutX + (rawX - _lastOutX) * a);
                outY = (int)Math.Round(_lastOutY + (rawY - _lastOutY) * a);
            }

            INPUT[] _input = new INPUT[1];
            _input[0] = new INPUT
            {
                type = INPUT_MOUSE,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        dx = outX,
                        dy = outY,
                        mouseData = 0,
                        // Absolute movement across the virtual desktop; avoid OS coalescing if possible
                        dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE | MOUSEEVENTF_VIRTUALDESK | MOUSEEVENTF_MOVE_NOCOALESCE,
                        time = 0 // Windows will provide this
                    }
                }
            };
            SendInput((uint)1, _input, Marshal.SizeOf(typeof(INPUT)));

            _lastRawX = rawX;
            _lastRawY = rawY;
            _lastOutX = outX;
            _lastOutY = outY;
            return;

        }

        public void MouseDown()
        {
            INPUT[] _input = new INPUT[1];
            _input[0] = new INPUT
            {
                type = INPUT_MOUSE, // INPUT_MOUSE
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_LEFTDOWN, // MOUSEEVENTF_LEFTDOWN
                        time = 0 // Windows will provide this
                    }
                }
            };
            SendInput((uint)1, _input, Marshal.SizeOf(typeof(INPUT)));
            return;
        }

        public void MouseUp()
        {
            INPUT[] _input = new INPUT[1];
            _input[0] = new INPUT
            {
                type = INPUT_MOUSE, // INPUT_MOUSE
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_LEFTUP, // MOUSEEVENTF_LEFTUP
                        time = 0 // Windows will provide this
                    }
                }
            };
            SendInput((uint)1, _input, Marshal.SizeOf(typeof(INPUT)));
            return;
        }

        public MouseProcessor()
        {
        }
    }

}
