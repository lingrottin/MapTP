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

        //[DllImport("user32.dll")]
        //private static extern int SetCursorPos(int x, int y);
        #endregion
        public void MoveCursor(int x, int y)
        {
                //SetCursorPos(x, y);
                INPUT[] _input = new INPUT[1];
                _input[0] = new INPUT
                {
                    type = 0, // INPUT_MOUSE
                    mkhi = new MOUSEKEYBDHARDWAREINPUT
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = x,
                            dy = y,
                            mouseData = 0,
                            dwFlags = 0x8000 | 0x0001 | 0x4000, // MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVEMENT | MOUSEEVENTF_VIRTUALDESK
                            time = 0 // Windows will provide this
                        }
                    }
                };
                SendInput((uint)1, _input, Marshal.SizeOf(typeof(INPUT)));
                return;
            
        }

        public void MouseDown()
        {
            INPUT[] _input = new INPUT[1];
            _input[0] = new INPUT
            {
                type = 0, // INPUT_MOUSE
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = 0x0002, // MOUSEEVENTF_LEFTDOWN
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
                type = 0, // INPUT_MOUSE
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = 0x0004, // MOUSEEVENTF_LEFTUP
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
