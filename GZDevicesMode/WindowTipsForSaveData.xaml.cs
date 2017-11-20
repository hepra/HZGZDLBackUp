using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GZDevicesMode
{
    /// <summary>
    /// WindowTipsForSaveData.xaml 的交互逻辑
    /// </summary>
    public partial class WindowTipsForSaveData : Window
    {
        public bool is_Save_data = true;
        public bool is_Show_InTheFuture = true;
        public WindowTipsForSaveData()
        {
            InitializeComponent();
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            if(rb_NotSvave.IsChecked == true)
            {
                is_Save_data = false;
            }
            else if(rb_Save.IsChecked == true)
            {
                is_Save_data = true;
            }
            if(cb_DonotShowAnyMore.IsChecked == true)
            {
                is_Show_InTheFuture = false;
            }
            else
            {
                is_Show_InTheFuture = true;
            }
            this.Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (rb_NotSvave.IsChecked == true)
            {
                is_Save_data = false;
            }
            else if (rb_Save.IsChecked == true)
            {
                is_Save_data = true;
            }
            if (cb_DonotShowAnyMore.IsChecked == true)
            {
                is_Show_InTheFuture = false;
            }
            else
            {
                is_Show_InTheFuture = true;
            }
            e.Cancel = true;
            this.Hide();
        }
    }
}
