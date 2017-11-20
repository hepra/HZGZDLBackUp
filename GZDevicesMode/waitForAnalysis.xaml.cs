using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GZDevicesMode
{
    /// <summary>
    /// waitForAnalysis.xaml 的交互逻辑
    /// </summary>
    public partial class waitForAnalysis : Window
    {
        public waitForAnalysis()
        {
            InitializeComponent();
        }
        public waitForAnalysis(long max)
        {
            InitializeComponent();
            pro.Maximum = max;
            pro.Minimum = 0;
            pro.Value = 0;
        }
        public void getPro(long val)
        {
            pro.Value = val;
        }
    }
}
