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
using CustomWPFColorPicker;
namespace GZDevicesMode
{
    /// <summary>
    /// TransForm_SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TransForm_SettingWindow : Window
    {
        public TreeView new_tree;
        public delegate void DelegateMethod(object sender);
        public DelegateMethod temp;
        public TransForm_SettingWindow()
        {
            InitializeComponent();
        }
        public TransForm_SettingWindow(TreeView tree,DelegateMethod realMethod)
        {
            InitializeComponent();
            new_tree = tree;
            temp = realMethod;
        }

        private void T_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void btnTransformerParaConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            temp(sender);
        }

        private void btnTransformerParaCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void T_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void tbStartWorkingPosition_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox temp = sender as TextBox;
            int result = 0;
            if(temp.Text == "")
            {
                return;
            }
            if(int.TryParse(temp.Text,out result))
            {
                return;
            }
            else
            {
                MessageBox.Show("请输入整数", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                if(temp.Name == "tbEndWorkingPosition")
                {
                    temp.Text = "20";
                }
                else
                {
                    temp.Text = "0";
                }
            }
        }

        private void tbMidPosition_TextChanged(object sender, TextChangedEventArgs e)
        {
            string num = (sender as TextBox).Text;
            lb_9a.Content = num + "a";
            lb_9b.Content = num + "b";
            lb_9c.Content = num + "c";
        }

        private void lb_9a_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if(rb.IsChecked == true)
            {
                rb.Foreground = Brushes.Red;
            }
            else
            {
                rb.Foreground = Brushes.Gray;
            }
        }

    }
}
