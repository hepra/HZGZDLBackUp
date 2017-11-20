using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace CustomWPFColorPicker
{
    /// <summary>
    /// ��ɫѡ����
    /// </summary>
   
    public partial class ColorPickerControlView : UserControl
    {
         public delegate void DelegateMethod(SolidColorBrush para);
         public DelegateMethod temp;
        /// <summary>
        /// ��ǰ��ɫ,ʵ�廭ˢ��
        /// </summary>
        public SolidColorBrush CurrentColor
        {
            get { return (SolidColorBrush)GetValue(CurrentColorProperty); }
            set { SetValue(CurrentColorProperty, value); }
        }
        /// <summary>
        /// ע������,������:CurrentColor,��������:SolidColorBrush �����ؼ�����:ColorPickerControlView Ĭ��ֵ:System.Windows.Media.Brushes.Black
        /// </summary>
        public static DependencyProperty CurrentColorProperty =
            DependencyProperty.Register("CurrentColor", typeof(SolidColorBrush), typeof(ColorPickerControlView), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x33, 0xcc, 0x00))));
        /// <summary>
        /// �����п���UI���������:SelectColorCommand,����:ColorPickerControlView(��ǰ���������)
        /// </summary>
        public static RoutedUICommand SelectColorCommand = new RoutedUICommand("SelectColorCommand","SelectColorCommand", typeof(ColorPickerControlView));
        /// <summary>
        /// �ؼ���ģ�ߴ���
        /// </summary>
        private Window _advancedPickerWindow;
        /// <summary>
        /// ���캯��
        /// </summary>
        public ColorPickerControlView()
        {
            DataContext = this;
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(SelectColorCommand, SelectColorCommandExecute));
        }
        /// <summary>
        /// ��ѡ����ɫ����ת���ɵ�ǰ��ɫ��Ӧ��Color����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectColorCommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(e.Parameter.ToString()));
            temp(CurrentColor);
        }
        /// <summary>
        /// ʵ����ģ�ߴ��ڵ���ͼ����
        /// </summary>
        /// <param name="advancedColorWindow"></param>
        public static void ShowModal(Window advancedColorWindow)
        {
            advancedColorWindow.Owner = Application.Current.MainWindow;
            advancedColorWindow.ShowDialog();
        }
        /// <summary>
        /// �ر�ģ�ߴ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AdvancedPickerPopUpKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                _advancedPickerWindow.Close();
        }
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;
            e.Handled = false;
        }
        /// <summary>
        /// ���Moreʱ ���� ��ɫѡ�����
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