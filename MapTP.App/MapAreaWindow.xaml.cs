using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace MapTP.App
{
    /// <summary>
    /// MapAreaWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MapAreaWindow : Window
    {
        

        public delegate void SendArea(int scsx, int scsy, int scex, int scey);
        public SendArea sendArea;

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            return;
        }

        public MapAreaWindow()
        {
            InitializeComponent();
            this.LocationChanged += ReportLocation;
            this.SizeChanged += ReportLocation;
        }

        private void CloseGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseGrid.Background = Brushes.Crimson;
        }

        private void CloseGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseGrid.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x11, 0x22, 0x55));
        }

        private void CloseGrid_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void ReportLocation(object sender, EventArgs e)
        {
            var BorderPos = MainBorder.PointToScreen(new Point(0, 0));
            var source = PresentationSource.FromVisual(MainBorder);
            Matrix transformToDevice = source.CompositionTarget.TransformToDevice;
            var BorderSize = (Size)transformToDevice.Transform((Vector)MainBorder.RenderSize);

            int scsx = (int)Math.Floor(BorderPos.X);
            int scsy = (int)Math.Floor(BorderPos.Y);
            int scex = scsx + (int)Math.Floor(BorderSize.Width);
            int scey = scsy + (int)Math.Floor(BorderSize.Height);
            sendArea(scsx, scsy, scex, scey);
        }
    }
}
