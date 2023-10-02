using System.Drawing;
using System.Windows;
using System.Windows.Interop;


namespace MapTP.App
{
    // from https://huchengv5.gitee.io//post/WPF-%E5%A6%82%E4%BD%95%E6%AD%A3%E7%A1%AE%E8%8E%B7%E5%8F%96%E5%B1%8F%E5%B9%95%E5%88%86%E8%BE%A8%E7%8E%87.html
    public class ScreenManager
    {
        /// <summary>
        /// 获取DPI百分比
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static double GetDpiRatio(Window window)
        {
            var currentGraphics = Graphics.FromHwnd(new WindowInteropHelper(window).Handle);
            return currentGraphics.DpiX / 96;
        }

        public static double GetDpiRatio()
        {
            return GetDpiRatio(Application.Current.MainWindow);
        }

        public static double GetScreenHeight()
        {
            return SystemParameters.PrimaryScreenHeight * GetDpiRatio();
        }

        public static double GetScreenWidth()
        {
            return SystemParameters.PrimaryScreenWidth * GetDpiRatio();
        }
    }
}
