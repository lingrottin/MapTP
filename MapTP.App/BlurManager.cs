using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MapTP.App
{
    internal class BlurManager
    {
        // Completely from Walterlv

            private readonly Window _window;
            private bool _isEnabled;
            private int _blurColor;

            /// <summary>
            /// 创建 <see cref="BlurManager"/> 的一个新实例。
            /// </summary>
            /// <param name="window">要创建模糊特效的窗口实例。</param>
            public BlurManager(Window window) => _window = window ?? throw new ArgumentNullException(nameof(window));

            /// <summary>
            /// 获取或设置此窗口模糊特效是否生效的一个状态。
            /// 默认为 false，即不生效。
            /// </summary>
            [DefaultValue(false)]
            public bool IsEnabled
            {
                get => _isEnabled;
                set
                {
                    _isEnabled = value;
                    OnIsEnabledChanged(value);
                }
            }

            /// <summary>
            /// 获取或设置此窗口模糊特效叠加的颜色。
            /// </summary>
            public Color Color
            {
                get => Color.FromArgb(
                    // 取出红色分量。
                    (byte)((_blurColor & 0x000000ff) >> 0),
                    // 取出绿色分量。
                    (byte)((_blurColor & 0x0000ff00) >> 8),
                    // 取出蓝色分量。
                    (byte)((_blurColor & 0x00ff0000) >> 16),
                    // 取出透明分量。
                    (byte)((_blurColor & 0xff000000) >> 24));
                set => _blurColor =
                    // 组装红色分量。
                    value.R << 0 |
                    // 组装绿色分量。
                    value.G << 8 |
                    // 组装蓝色分量。
                    value.B << 16 |
                    // 组装透明分量。
                    value.A << 24;
            }

            private void OnIsEnabledChanged(bool isEnabled)
            {
                Window window = _window;
                var handle = new WindowInteropHelper(window).EnsureHandle();
                Composite(handle, isEnabled);
            }

            private void Composite(IntPtr handle, bool isEnabled)
            {
                // 操作系统版本判定。
                var osVersion = Environment.OSVersion.Version;
                var windows10_1809 = new Version(10, 0, 17763);
                var windows10 = new Version(10, 0);
            var windows11_buildver = 22000;

                // 创建 AccentPolicy 对象。
                var accent = new AccentPolicy();

                // 设置特效。
                if (!isEnabled)
                {
                    accent.AccentState = AccentState.ACCENT_DISABLED;
                }
                else if(Environment.OSVersion.Version.Build >= windows11_buildver)
            {
                accent.AccentState = AccentState.ACCENT_INVALID_STATE; // disable Acrylic and do nothing
            }
            else if (osVersion > windows10_1809)
                {
                    // 如果系统在 Windows 10 (1809) 以上，则启用亚克力效果，并组合已设置的叠加颜色和透明度。
                    //  请参见《在 WPF 程序中应用 Windows 10 真•亚克力效果》
                    //  https://blog.walterlv.com/post/using-acrylic-in-wpf-application.html
                    accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                    accent.GradientColor = _blurColor;
                }
                else if (osVersion > windows10)
                {
                    // 如果系统在 Windows 10 以上，则启用 Windows 10 早期的模糊特效。
                    //  请参见《在 Windows 10 上为 WPF 窗口添加模糊特效》
                    //  https://blog.walterlv.com/post/win10/2017/10/02/wpf-transparent-blur-in-windows-10.html
                    accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                }
                else
                {
                    // 暂时不处理其他操作系统：
                    //  - Windows 8/8.1 不支持任何模糊特效
                    //  - Windows Vista/7 支持 Aero 毛玻璃效果
                    return;
                }

                // 将托管结构转换为非托管对象。
                var accentPolicySize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentPolicySize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                // 设置窗口组合特性。
                try
                {
                    // 设置模糊特效。
                    var data = new WindowCompositionAttributeData
                    {
                        Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                        SizeOfData = accentPolicySize,
                        Data = accentPtr,
                    };
                    SetWindowCompositionAttribute(handle, ref data);
                }
                finally
                {
                    // 释放非托管对象。
                    Marshal.FreeHGlobal(accentPtr);
                }
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

            private enum AccentState
            {
                /// <summary>
                /// 完全禁用 DWM 的叠加特效。
                /// </summary>
                ACCENT_DISABLED = 0,

                /// <summary>
                /// 
                /// </summary>
                ACCENT_ENABLE_GRADIENT = 1,
                ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
                ACCENT_ENABLE_BLURBEHIND = 3,
                ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
                ACCENT_INVALID_STATE = 5,
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct AccentPolicy
            {
                public AccentState AccentState;
                public int AccentFlags;
                public int GradientColor;
                public int AnimationId;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct WindowCompositionAttributeData
            {
                public WindowCompositionAttribute Attribute;
                public IntPtr Data;
                public int SizeOfData;
            }

            private enum WindowCompositionAttribute
            {
                // 省略其他未使用的字段
                WCA_ACCENT_POLICY = 19,
                // 省略其他未使用的字段
            }

            /// <summary>
            /// 当前操作系统支持的透明模糊特效级别。
            /// </summary>
            public enum BlurSupportedLevel
            {
                /// <summary>
                /// 
                /// </summary>
                NotSupported,
                Aero,
                Blur,
                Acrylic,
            }
        }
    
    /// <summary>
    /// enables Mica for Windows 11 users
    /// </summary>
    internal class Mica
    {
        //this is from Difegue/Mica-WPF-Sample
        #region Win32
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [Flags]
        public enum DwmWindowAttribute : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_MICA_EFFECT = 1029,
            DWMA_SYSTEMBACKDROP_TYPE=38,
        }
        #endregion

        // Enable Mica on the given HWND.
        public static void EnableMica(HwndSource source, bool darkThemeEnabled)
        {
            int trueValue = 0x01;
            int falseValue = 0x00;
            int systemBackdropType_Main = 3;

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (darkThemeEnabled)
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));

            DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMA_SYSTEMBACKDROP_TYPE, ref systemBackdropType_Main, Marshal.SizeOf(typeof(int)));
        }

        private HwndSource hwndSource;

        private bool _enable;
        public bool enable
        {
            get => _enable;
            set
            {
                _enable = value;
                if (enable)
                {
                    EnableMica(hwndSource, false);
                }
            }
        }

        public Mica(HwndSource _hwndSource)
        {
            this.hwndSource = _hwndSource;
        }
    }
}

