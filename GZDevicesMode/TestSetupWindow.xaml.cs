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
using GZDL_DEV.model;

namespace GZDevicesMode
{
    /// <summary>
    /// TestSetupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestSetupWindow : Window
    {
        public delegate void DelegateMethod(object sender);
        DelegateMethod temp;
       
       public TestSetupWindow(DelegateMethod realFouction)
        {
            InitializeComponent();
            temp = realFouction;
            
            cmbTestGear.Items.Add("0--20Ω");
            cmbTestGear.Items.Add("20--100Ω");
            cmbTestGear.SelectedIndex = 0;
            cmbInnerSupplyVoltage.Items.Add("100");
            cmbInnerSupplyVoltage.Items.Add("200");
            cmbInnerSupplyVoltage.Items.Add("500");
            cmbInnerSupplyVoltage.Items.Add("800");
            tbDeviceSampleFrequency_DC.Text = "20";
            cmbInnerSupplyVoltage.SelectedIndex = 1;
            cmbDeviceACSampleFrequency.Items.Add(50);
            cmbDeviceACSampleFrequency.Items.Add(100);
            cmbDeviceACSampleFrequency.Items.Add(200);
            cmbDeviceACSampleFrequency.Items.Add(500);
            rbInnernalPower.IsChecked = true;
            tabAC.IsSelected = true;
            rbAutoAnalysisParameterSet_AC.IsChecked = true;
            rbAutoAnalysisParameterSet_DC.IsChecked = true;
            rbEnableDCfilter.IsChecked = true;
            rbDisableDCfilter.IsChecked = false;
            rbSinglePiontMeasurment.IsChecked = true;
            rbBackSwitch.IsChecked = true;
            rbAutoContinuousMeasurment.IsChecked = false;
            rbInnernalPower.IsChecked = true;
            tabAC.IsSelected = true;
            rbAutoAnalysisParameterSet_AC.IsChecked = true;
            rbAutoContinuousMeasurment.IsChecked = false;
            rbSinglePiontMeasurment.IsChecked = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //rbInnernalPower.IsChecked = true;
        }
        public void useDefultPara()
        {
            tbDeviceSampleFrequency_DC.Text = "20";
            cmbTestGear.SelectedIndex = 0;
            cmbDeviceACSampleFrequency.SelectedIndex = 0;
            cmbOneCurTap.SelectedIndex = 0;
            cmbInnerSupplyVoltage.SelectedIndex = 1;
            tbErrorRatioAuto_AC.Text = "5";
            tbErrorRatioAuto_DC.Text = "5";
            tbErrorRatioManual_AC.Text = "5";
            tbErrorRatioManual_DC.Text = "5";
            tbIgnoreTimeSpan_AC.Text = "80";
            tbMaxConstantTime_AC.Text = "4";
            tbMinConstantTime_DC.Text = "1";
            tbMutationRatioAuto_AC.Text = "5";
            tbMutationRatioManual_AC.Text = "5";
            tbMutationRatioAuto_DC.Text = "5";
            tbMutationRatioManual_DC.Text = "5";
            tbMinChangeTime_AC.Text = "0.5";
            tbMinChangeTime_DC.Text = "1";
            rbInnernalPower.IsChecked = true;
            tabAC.IsSelected = true;
            rbAutoAnalysisParameterSet_AC.IsChecked = true;
            rbAutoAnalysisParameterSet_DC.IsChecked = true;
            rbEnableDCfilter.IsChecked = true;
            rbSinglePiontMeasurment.IsChecked = true;
            rbBackSwitch.IsChecked = true;
            rbAutoContinuousMeasurment.IsChecked = false;
            rbDisableDCfilter.IsChecked = false;
        }
        private void btnTestParaConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (tbDeviceSampleFrequency_DC.Text == "" || tbDeviceSampleFrequency_DC.Text == null||
                tbErrorRatioAuto_AC.Text==""||tbErrorRatioAuto_AC.Text==null||
                tbErrorRatioAuto_DC.Text == "" || tbErrorRatioAuto_DC.Text == null||
                 tbMaxConstantTime_AC.Text == "" || tbMaxConstantTime_AC.Text == null||
                tbMinChangeTime_AC.Text == ""||tbMinChangeTime_AC.Text == null||
                tbMinChangeTime_DC.Text == ""||tbMinChangeTime_DC.Text == null||tbContinuousTestCurTap.Text == ""||tbContinuousTestEndTap.Text == ""
                )
            {
               if( MessageBox.Show("部分参数未配置!\r\n  [是]使用系统默认参数\r\n  [否]手动配置","提示",MessageBoxButton.YesNo,MessageBoxImage.Information) == MessageBoxResult.Yes)
               {
                   tbDeviceSampleFrequency_DC.Text = "20";
                   cmbTestGear.SelectedIndex = 0;
                   cmbDeviceACSampleFrequency.SelectedIndex = 0;
                   cmbOneCurTap.SelectedIndex = 0;
                   cmbInnerSupplyVoltage.SelectedIndex = 1;
                   tbContinuousTestEndTap.Text = (int.Parse(tbContinuousTestCurTap.Text) + 1).ToString();
                   
                   tbErrorRatioAuto_AC.Text = "5";
                   tbErrorRatioAuto_DC.Text = "5";
                   tbErrorRatioManual_AC.Text = "5";
                   tbErrorRatioManual_DC.Text = "55";
                   tbIgnoreTimeSpan_AC.Text = "80";
                   tbMaxConstantTime_AC.Text = "0.5";
                   tbMinConstantTime_DC.Text = "1";
                   tbMutationRatioAuto_AC.Text = "5";
                   tbMutationRatioManual_AC.Text = "5";
                   tbMutationRatioAuto_DC.Text = "5";
                   tbMutationRatioManual_DC.Text = "5";
                   tbMinChangeTime_AC.Text = "0.5";
                   tbMinChangeTime_DC.Text = "1";
                   rbInnernalPower.IsChecked = true;
                   tabAC.IsSelected = true;
                   rbAutoAnalysisParameterSet_AC.IsChecked = true;
                   rbAutoAnalysisParameterSet_DC.IsChecked = true;
                   rbEnableDCfilter.IsChecked = true;
                   rbSinglePiontMeasurment.IsChecked = true;
                   rbBackSwitch.IsChecked = true;
                   rbAutoContinuousMeasurment.IsChecked = false;
               }
                else
               {
                   return;
               }
            }
            temp(sender); 
        }

        private void btnTestParaCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void btnAddNewTestTransFormer_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            temp(sender);   
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            temp(sender);
            tabTest.IsSelected = true;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void TabControl_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if(tabAC.IsSelected)
            {
                tabAC.FontSize = 14;
                tabAnalysisDC.Foreground = Brushes.Gray;
                tabAnalysisAC.Foreground = Brushes.Blue;
                tabAC.FontWeight = FontWeights.Bold;
                tabDC.FontWeight = FontWeights.Light;
            }
            else
            {
                tabDC.FontSize = 14;
                tabAnalysisDC.Foreground = Brushes.Blue;
                tabAnalysisAC.Foreground = Brushes.Gray;
                tabAC.FontWeight = FontWeights.Light;
                tabDC.FontWeight = FontWeights.Bold;
            }
        }

        private void tbMutationRatioAuto_AC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
            if (tb.Text == "0")
            {
                return;
            }
            if (tb.Text == ".")
            {
                return;
            }
            for (int i = 0; i < 51; i++)
            {
                if (tb.Text == i.ToString() + ".")
                {
                    return;
                }
            }
            if (tb.Text == "")
            {
                return;
            }
            if (double.TryParse(tb.Text, out result))
            {
                if(result>50||result<1)
                {
                    if(tb.Text!="")
                    {
                         MessageBox.Show("超过设置范围:允许范围为%1~50%");
                    }
                    if(result>50)
                    {
                        tb.Text = "50";
                    }
                    else if(result<1)
                    {
                        tb.Text = "1";
                    }
                }
            }
            else
            {
                tb.Text = "10.0";
            }
        }

        private void tbErrorRatioAuto_AC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
            string name = tb.Name;
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
            if (tb.Text == "")
            {
                return;
            }
            if (double.TryParse(tb.Text, out result))
            {
                if (result > 20 || result < 0)
                {
                    if (tb.Text != "")
                    {
                        MessageBox.Show("超过设置范围:允许范围为0~20");
                    }
                    
                    if (result > 20)
                    {
                        tb.Text = "20";
                    }
                    else if (result < 0)
                    {
                        tb.Text = "0";
                    }
                }
            }
            else
            {
                tb.Text = "5";
            }
        }

        private void tbMinChangeTime_AC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
            if(tb.Text == "0")
            {
                return;
            }
            if (tb.Text == ".")
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                if (tb.Text == i.ToString() + ".")
                {
                    return;
                }
            }
            if (tb.Text == "")
            {
                return;
            }
            if (double.TryParse(tb.Text, out result))
            {
                if (result > 1 || result < 0.1)
                {
                    if (tb.Text != "")
                    {
                        MessageBox.Show("超过设置范围:允许范围为0.1~1ms");
                    }
                    
                    if (result > 1)
                    {
                        tb.Text = "1";
                    }
                    else if (result < 0.1)
                    {
                        tb.Text = "0.1";
                    }
                }
            }
            else
            {
                tb.Text = "0.5";
            }
        }

        private void tbMaxConstantTime_AC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
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
            if (tb.Text == "")
            {
                return;
            }
            if (double.TryParse(tb.Text, out result))
            {
                if (result > 200 || result < 1)
                {
                    if (tb.Text != "")
                    {
                        MessageBox.Show("超过设置范围:允许范围为1~200ms");
                    }
                    
                    if (result > 200)
                    {
                        tb.Text = "200";
                    }
                    else if (result < 1)
                    {
                        tb.Text = "1";
                    }
                }
            }
            else
            {
                tb.Text = "5";
            }
        }

        private void tbIgnoreTimeSpan_AC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
            if (tb.Text == "0")
            {
                return;
            }
            if (tb.Text == ".")
            {
                return;
            }
            for (int i = 0; i < 256;i++ )
            {
                if(tb.Text == i.ToString()+".")
                {
                    return;
                }
            }
                if (tb.Text == "")
                {
                    return;
                }
            if (double.TryParse(tb.Text, out result))
            {
                if (result > 255 || result < 0)
                {
                    if (tb.Text != "")
                    {
                        MessageBox.Show("超过设置范围:允许范围为0~255ms");
                    }
                    
                    if (result > 255)
                    {
                        tb.Text = "255";
                    }
                    else if (result < 0)
                    {
                        tb.Text = "0";
                    }
                }
            }
            else
            {
                tb.Text = "80";
            }
        }

        private void tbMinChangeTime_DC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
            if (tb.Text == "0")
            {
                return;
            }
            if (tb.Text == ".")
            {
                return;
            }
            for (int i = 0; i < 256; i++)
            {
                if (tb.Text == i.ToString() + ".")
                {
                    return;
                }
            }
            if (tb.Text == "")
            {
                return;
            }
            if (double.TryParse(tb.Text, out result))
            {
                if (result > 20 || result < 0.05)
                {
                    if (tb.Text != "")
                    {
                        MessageBox.Show("直流最小持续变化时间允许范围为0.05~20ms");
                    }

                    if (result > 20)
                    {
                        tb.Text = "20";
                    }
                    else if (result < 0.05)
                    {
                        tb.Text = "0.05";
                    }
                }
            }
            else
            {
                tb.Text = "2";
            }
        }
        private void tbMinConstantTime_DC_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result = 0;
            TextBox tb = sender as TextBox;
            if (tb.Text == "0")
            {
                return;
            }
            if (tb.Text == ".")
            {
                return;
            }
            for (int i = 0; i < 256; i++)
            {
                if (tb.Text == i.ToString() + ".")
                {
                    return;
                }
            }
            if (tb.Text == "")
            {
                return;
            }
            if (double.TryParse(tb.Text, out result))
            {
                if (result > 2 || result < 0.1)
                {
                    if (tb.Text != "")
                    {
                        MessageBox.Show("直流最大持续不变时间允许范围为0.1-2ms");
                    }
                    if (result > 2)
                    {
                        tb.Text = "2.0";
                    }
                    else if (result < 0.1)
                    {
                        tb.Text = "0.1";
                    }
                }
            }
            else
            {
                tb.Text = "1";
            }
        }

        private void rbDC_Checked(object sender, RoutedEventArgs e)
        {
            //rbInnernalPower.IsChecked = true;
        }
        private void cmbTransformerSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gbTransSelect.BorderBrush = Brushes.Green;
            temp(sender);
        }

        private void rbExternalPower_Checked(object sender, RoutedEventArgs e)
        {
            cmbInnerSupplyVoltage.IsEnabled = false;
        }

        private void rbInnernalPower_Checked(object sender, RoutedEventArgs e)
        {
            cmbInnerSupplyVoltage.IsEnabled = true;
        }
    }
}
