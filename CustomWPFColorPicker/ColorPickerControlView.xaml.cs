using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace CustomWPFColorPicker
{
    /// <summary>
    /// 颜色选择器
    /// </summary>
   
    public partial class ColorPickerControlView : UserControl
    {
         public delegate void DelegateMethod(SolidColorBrush para);
         public DelegateMethod temp;
        /// <summary>
        /// 当前颜色,实体画刷类
        /// </summary>
        public SolidColorBrush CurrentColor
        {
            get { return (SolidColorBrush)GetValue(CurrentColorProperty); }
            set { SetValue(CurrentColorProperty, value); }
        }
        /// <summary>
        /// 注册属性,属性名:CurrentColor,属性类型:SolidColorBrush 所属控件类型:ColorPickerControlView 默认值:System.Windows.Media.Brushes.Black
        /// </summary>
        public static DependencyProperty CurrentColorProperty =
            DependencyProperty.Register("CurrentColor", typeof(SolidColorBrush), typeof(ColorPickerControlView), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x33, 0xcc, 0x00))));
        /// <summary>
        /// 代码中控制UI界面的命令:SelectColorCommand,类型:ColorPickerControlView(当前定义的类型)
        /// </summary>
        public static RoutedUICommand SelectColorCommand = new RoutedUICommand("SelectColorCommand","SelectColorCommand", typeof(ColorPickerControlView));
        /// <summary>
        /// 控件的模具窗口
        /// </summary>
        private Window _advancedPickerWindow;
        /// <summary>
        /// 构造函数
        /// </summary>
        public ColorPickerControlView()
        {
            DataContext = this;
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(SelectColorCommand, SelectColorCommandExecute));
        }
        /// <summary>
        /// 将选择颜色参数转换成当前颜色对应的Color类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectColorCommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(e.Parameter.ToString()));
            temp(CurrentColor);
        }
        /// <summary>
        /// 实例化模具窗口到绘图窗口
        /// </summary>
        /// <param name="advancedColorWindow"></param>
        public static void ShowModal(Window advancedColorWindow)
        {
            advancedColorWindow.Owner = Application.Current.MainWindow;
            advancedColorWindow.ShowDialog();
        }
        /// <summary>
        /// 关闭模具窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AdvancedPickerPopUpKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                _advancedPickerWindow.Close();
        }
        /// <summary>
        /// 不懂
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
            e.Handled = false;
        }
        /// <summary>
        /// 点击More时 弹出 颜色选择界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoreColorsClicked(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
            var advancedColorPickerDialog = new AdvancedColorPickerDialog();

            _advancedPickerWindow = new Window
                                        {
                                            AllowsTransparency = true,
                                            Content = advancedColorPickerDialog,
                                            WindowStyle = WindowStyle.None,
                                            ShowInTaskbar = false,
                                            Background = new SolidColorBrush(Colors.Transparent),
                                            Padding = new Thickness(0),
                                            Margin = new Thickness(0),
                                            WindowState = WindowState.Normal,
                                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                            SizeToContent = SizeToContent.WidthAndHeight
                                        };
            advancedColorPickerDialog.method = delegate(SolidColorBrush color) { temp(color); };
            _advancedPickerWindow.DragMove();
            _advancedPickerWindow.KeyDown += AdvancedPickerPopUpKeyDown;
            advancedColorPickerDialog.DialogResultEvent += AdvancedColorPickerDialogDialogResultEvent;
            advancedColorPickerDialog.Drag += AdvancedColorPickerDialogDrag;
            ShowModal(_advancedPickerWindow);
        }

        void AdvancedColorPickerDialogDrag(object sender, DragDeltaEventArgs e)
        {
            _advancedPickerWindow.DragMove();
        }

        void AdvancedColorPickerDialogDialogResultEvent(object sender, EventArgs e)
        {
            _advancedPickerWindow.Close();
            var dialogEventArgs = (DialogEventArgs)e;
            if (dialogEventArgs.DialogResult == DialogResult.Cancel)
                return;
            CurrentColor = dialogEventArgs.SelectedColor;
            temp(CurrentColor);
        }
    }
}