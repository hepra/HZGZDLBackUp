using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// ModfiyParaPage.xaml 的交互逻辑
    /// </summary>
    public partial class ModfiyParaPage : Window
    {
        public delegate void Fun_修改参数(float WuChaBiLi,float ChiXuShiJian);
        public Fun_修改参数 执行函数;
        public ModfiyParaPage(Fun_修改参数 fun )
        {
            InitializeComponent();
            执行函数 = fun;
        }

        private void btnChongXinFenXi_Click(object sender, RoutedEventArgs e)
        {
            执行函数(float.Parse(tbWuChaBiLi.Text),float.Parse(tbChiXuShiJian.Text));
            this.Hide();
        }

        private void btnMoRenCanShu_Click(object sender, RoutedEventArgs e)
        {
            tbChiXuShiJian.Text = "20.0";
            tbWuChaBiLi.Text = "5.0";
        }

        private void tbWuChaBiLi_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string name = tb.Name;
            float value = 0;
             if (tb.Text == "0")
            {
                return;
            }
            if (tb.Text == ".")
            {
                return;
            }
            for (int i = 0; i < 11; i++)
            {
                if (tb.Text == i.ToString() + ".")
                {
                    return;
                }
            }
            if (tb.Text.Trim() == "")
            {
                return;
            }
            if(float.TryParse(tb.Text, out value))
            {
                return;
            }
            else
            {
                tb.Text = "";
            }
            //MessageBox.Show("请输入数字");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
