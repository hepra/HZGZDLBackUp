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
using CustomReport;
using GZDL_DEV.DEL;
using GZDL_DEV.model;

namespace GZDevicesMode
{
    /// <summary>
    /// OldcWindow1.xaml 的交互逻辑
    /// </summary>
    public partial class OldcWindow1 : Window
    {
        string[] header = OleDbHelper.Setting_Items_Name;
        public OldcWindow1()
        {
            InitializeComponent();
            cmOldcTableName.Items.Add("变压器信息表");
            cmOldcTableName.Items.Add("测试信息表");
            cmOldcTableName.SelectedIndex = 0;
            dataGrid1.ItemsSource = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name).Tables[0].DefaultView;
        }

        private void cmOldcTableName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cmOldcTableName.SelectedItem == null)
            {
                return;
            }
            if(cmOldcTableName.SelectedItem.ToString()=="变压器信息表")
            {
                    dataGrid1.ItemsSource = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name).Tables[0].DefaultView; 
            }
            else
            {
                dataGrid1.ItemsSource = OleDbHelper.Select(OleDbHelper.Test_Table_Name).Tables[0].DefaultView; 

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
