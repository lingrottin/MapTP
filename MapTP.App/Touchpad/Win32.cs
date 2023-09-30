using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MapTP.App.Touchpad
{
    internal static class Win32
    {
        // this is completely from https://github.com/emoacht/RawInput.Touchpad

        #region Win32

        [DllImport("User32", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(
            [Out] RAWINPUTDEVICELIST[] pRawInputDeviceList,
            ref uint puiNumDevices,
            uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
        }

        public const uint RIM_TYPEMOUSE = 0;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEHID = 2;

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags; // RIDEV_INPUTSINK
            public IntPtr hwndTarget;
        }

        public const uint RIDEV_INPUTSINK = 0x00000100;

        [DllImport("User32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(
            IntPtr hRawInput, // lParam in WM_INPUT
            uint uiCommand, // RID_HEADER
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        public const uint RID_INPUT = 0x10000003;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER Header;
            public RAWHID Hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType; // RIM_TYPEMOUSE or RIM_TYPEKEYBOARD or RIM_TYPEHID
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam; // wParam in WM_INPUT
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
            public IntPtr bRawData; // This is not for use.
        }

        [DllImport("User32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice, // hDevice by RAWINPUTHEADER
            uint uiCommand, // RIDI_PREPARSEDDATA
            IntPtr pData,
            ref uint pcbSize);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice, // hDevice by RAWINPUTDEVICELIST
            uint uiCommand, // RIDI_DEVICEINFO
            ref RID_DEVICE_INFO pData,
            ref uint pcbSize);

        public const uint RIDI_PREPARSEDDATA = 0x20000005;
        public const uint RIDI_DEVICEINFO = 0x2000000b;

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO
        {
            public uint cbSize; // This is determined to accommodate RID_DEVICE_INFO_KEYBOARD.
            public uint dwType;
            public RID_DEVICE_INFO_HID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_HID
        {
            public uint dwVendorId;
            public uint dwProductId;
            public uint dwVersionNumber;
            public ushort usUsagePage;
            public ushort usUsage;
        }

        [DllImport("Hid.dll", SetLastError = true)]
        public static extern uint HidP_GetCaps(
            IntPtr PreparsedData,
            out HIDP_CAPS Capabilities);

        public const uint HIDP_STATUS_SUCCESS = 0x00110000;

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;

            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("Hid.dll", CharSet = CharSet.Auto)]
        public static extern uint HidP_GetValueCaps(
            HIDP_REPORT_TYPE ReportType,
            [Out] HIDP_VALUE_CAPS[] ValueCaps,
            ref ushort ValueCapsLength,
            IntPtr PreparsedData);

        public enum HIDP_REPORT_TYPE
        {
            HidP_Input,
            HidP_Output,
            HidP_Feature
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_VALUE_CAPS
        {
            public ushort UsagePage;
            public byte ReportID;

            [MarshalAs(UnmanagedType.U1)]
            public bool IsAlias;

            public ushort BitField;
            public ushort LinkCollection;
            public ushort LinkUsage;
            public ushort LinkUsagePage;

            [MarshalAs(UnmanagedType.U1)]
            public bool IsRange;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsStringRange;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsDesignatorRange;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsAbsolute;
            [MarshalAs(UnmanagedType.U1)]
            public bool HasNull;

            public byte Reserved;
            public ushort BitSize;
            public ushort ReportCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public ushort[] Reserved2;

            public uint UnitsExp;
            public uint Units;
            public int LogicalMin;
            public int LogicalMax;
            public int PhysicalMin;
            public int PhysicalMax;

            // Range
            public ushort UsageMin;
            public ushort UsageMax;
            public ushort StringMin;
            public ushort StringMax;
            public ushort DesignatorMin;
            public ushort DesignatorMax;
            public ushort DataIndexMin;
            public ushort DataIndexMax;

            // NotRange
            public ushort Usage => UsageMin;
            // ushort Reserved1;
            public ushort StringIndex => StringMin;
            // ushort Reserved2;
            public ushort DesignatorIndex => DesignatorMin;
            // ushort Reserved3;
            public ushort DataIndex => DataIndexMin;
            // ushort Reserved4;
        }

        [DllImport("Hid.dll", CharSet = CharSet.Auto)]
        public static extern uint HidP_GetUsageValue(
            HIDP_REPORT_TYPE ReportType,
            ushort UsagePage,
            ushort LinkCollection,
            ushort Usage,
            out uint UsageValue,
            IntPtr PreparsedData,
            IntPtr Report,
            uint ReportLength);

        #endregion
    }
}
