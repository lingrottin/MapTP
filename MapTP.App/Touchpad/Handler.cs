using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MapTP.App.Touchpad
{
    internal class Handler
    {

        public static bool Exists()
        {
            uint deviceListCount = 0;
            uint rawInputDeviceListSize = (uint)Marshal.SizeOf<Win32.RAWINPUTDEVICELIST>();

            if (Win32.GetRawInputDeviceList(
                null,
                ref deviceListCount,
                rawInputDeviceListSize) != 0)
            {
                return false;
            }

            var devices = new Win32.RAWINPUTDEVICELIST[deviceListCount];

            if (Win32.GetRawInputDeviceList(
                devices,
                ref deviceListCount,
                rawInputDeviceListSize) != deviceListCount)
            {
                return false;
            }

            foreach (var device in devices.Where(x => x.dwType == Win32.RIM_TYPEHID))
            {
                uint deviceInfoSize = 0;

                if (Win32.GetRawInputDeviceInfo(
                    device.hDevice,
                    Win32.RIDI_DEVICEINFO,
                    IntPtr.Zero,
                    ref deviceInfoSize) != 0)
                {
                    continue;
                }

                var deviceInfo = new Win32.RID_DEVICE_INFO { cbSize = deviceInfoSize };

                if (Win32.GetRawInputDeviceInfo(
                    device.hDevice,
                    Win32.RIDI_DEVICEINFO,
                    ref deviceInfo,
                    ref deviceInfoSize) == unchecked((uint)-1))
                {
                    continue;
                }

                if ((deviceInfo.hid.usUsagePage == 0x000D) &&
                    (deviceInfo.hid.usUsage == 0x0005))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool RegisterInput(IntPtr windowHandle)
        {
            // Precision Touchpad (PTP) in HID Clients Supported in Windows
            // https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/hid-architecture#hid-clients-supported-in-windows
            var device = new Win32.RAWINPUTDEVICE
            {
                usUsagePage = 0x000D,
                usUsage = 0x0005,
                dwFlags = Win32.RIDEV_INPUTSINK, // if set to 0, WM_INPUT messages come only when the window is in the foreground.
                hwndTarget = windowHandle
            };

            return Win32.RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<Win32.RAWINPUTDEVICE>());
        }

        public const int WM_INPUT = 0x00FF;
        public const int RIM_INPUT = 0;
        public const int RIM_INPUTSINK = 1;

        public static TouchpadContact[] ParseInput(IntPtr lParam)
        {
            // Get RAWINPUT.
            uint rawInputSize = 0;
            uint rawInputHeaderSize = (uint)Marshal.SizeOf<Win32.RAWINPUTHEADER>();

            if (Win32.GetRawInputData(
                lParam,
                Win32.RID_INPUT,
                IntPtr.Zero,
                ref rawInputSize,
                rawInputHeaderSize) != 0)
            {
                return null;
            }

            Win32.RAWINPUT rawInput;
            byte[] rawHidRawData;

            IntPtr rawInputPointer = IntPtr.Zero;
            try
            {
                rawInputPointer = Marshal.AllocHGlobal((int)rawInputSize);

                if (Win32.GetRawInputData(
                    lParam,
                    Win32.RID_INPUT,
                    rawInputPointer,
                    ref rawInputSize,
                    rawInputHeaderSize) != rawInputSize)
                {
                    return null;
                }

                rawInput = Marshal.PtrToStructure<Win32.RAWINPUT>(rawInputPointer);

                var rawInputData = new byte[rawInputSize];
                Marshal.Copy(rawInputPointer, rawInputData, 0, rawInputData.Length);

                rawHidRawData = new byte[rawInput.Hid.dwSizeHid * rawInput.Hid.dwCount];
                int rawInputOffset = (int)rawInputSize - rawHidRawData.Length;
                Buffer.BlockCopy(rawInputData, rawInputOffset, rawHidRawData, 0, rawHidRawData.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(rawInputPointer);
            }

            // Parse RAWINPUT.
            IntPtr rawHidRawDataPointer = Marshal.AllocHGlobal(rawHidRawData.Length);
            Marshal.Copy(rawHidRawData, 0, rawHidRawDataPointer, rawHidRawData.Length);

            IntPtr preparsedDataPointer = IntPtr.Zero;
            try
            {
                uint preparsedDataSize = 0;

                if (Win32.GetRawInputDeviceInfo(
                    rawInput.Header.hDevice,
                    Win32.RIDI_PREPARSEDDATA,
                    IntPtr.Zero,
                    ref preparsedDataSize) != 0)
                {
                    return null;
                }

                preparsedDataPointer = Marshal.AllocHGlobal((int)preparsedDataSize);

                if (Win32.GetRawInputDeviceInfo(
                    rawInput.Header.hDevice,
                    Win32.RIDI_PREPARSEDDATA,
                    preparsedDataPointer,
                    ref preparsedDataSize) != preparsedDataSize)
                {
                    return null;
                }

                if (Win32.HidP_GetCaps(
                    preparsedDataPointer,
                    out Win32.HIDP_CAPS caps) != Win32.HIDP_STATUS_SUCCESS)
                {
                    return null;
                }

                ushort valueCapsLength = caps.NumberInputValueCaps;
                var valueCaps = new Win32.HIDP_VALUE_CAPS[valueCapsLength];

                if (Win32.HidP_GetValueCaps(
                    Win32.HIDP_REPORT_TYPE.HidP_Input,
                    valueCaps,
                    ref valueCapsLength,
                    preparsedDataPointer) != Win32.HIDP_STATUS_SUCCESS)
                {
                    return null;
                }

                uint scanTime = 0;
                uint contactCount = 0;
                TouchpadContactCreator creator = new TouchpadContactCreator();
                List<TouchpadContact> contacts = new List<TouchpadContact>();

                foreach (var valueCap in valueCaps.OrderBy(x => x.LinkCollection))
                {
                    if (Win32.HidP_GetUsageValue(
                        Win32.HIDP_REPORT_TYPE.HidP_Input,
                        valueCap.UsagePage,
                        valueCap.LinkCollection,
                        valueCap.Usage,
                        out uint value,
                        preparsedDataPointer,
                        rawHidRawDataPointer,
                        (uint)rawHidRawData.Length) != Win32.HIDP_STATUS_SUCCESS)
                    {
                        continue;
                    }

                    // Usage Page and ID in Windows Precision Touchpad input reports
                    // https://docs.microsoft.com/en-us/windows-hardware/design/component-guidelines/windows-precision-touchpad-required-hid-top-level-collections#windows-precision-touchpad-input-reports
                    switch (valueCap.LinkCollection)
                    {
                        case 0:
                            if (valueCap.UsagePage == 0x0D)
                            {
                                switch (valueCap.Usage)
                                {
                                    case 0x56:
                                        // Scan Time
                                        scanTime = value;
                                        break;

                                    case 0x54: //Contact Count
                                        contactCount = value;
                                        break;
                                }
                            }

                            break;

                        default:
                            if (valueCap.UsagePage == 0x0D)
                            {
                                if (valueCap.Usage == 0x51)
                                {
                                    creator.ContactId = (int)value;
                                }
                            }
                            else if (valueCap.UsagePage == 0x01)
                            {
                                switch (valueCap.Usage)
                                {
                                    case 0x30: // X
                                        creator.X = (int)value;
                                        break;
                                    case 0x31: // Y
                                        creator.Y = (int)value;
                                        break;
                                }
                            }else if(valueCap.UsagePage == 0x09 && valueCap.Usage == 0x42)
                            {
                                creator.TipSwitch = (int)value;
                            } 
                            break;
                    }

                    if (creator.TryCreate(out TouchpadContact contact))
                    {
                        contacts.Add(contact);
                        if (contacts.Count >= contactCount)
                            break;

                        creator.Clear();
                    }
                }

                return contacts.ToArray();
            }
            finally
            {
                Marshal.FreeHGlobal(rawHidRawDataPointer);
                Marshal.FreeHGlobal(preparsedDataPointer);
            }
        }
    }
}
