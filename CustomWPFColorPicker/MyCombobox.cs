using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CustomWPFColorPicker
{
    #region 自定义的ComboboxItem
    public class MyComboboxItem : ComboBoxItem
        {
            TextBlock textBlock;
            CheckBox cb;
            public object teeChart { get; set; }
            public ComboBox cmbox { get; set; }
            CustomWPFColorPicker.ColorPickerControlView pikerLiner;
            public delegate void do_reflush_combox(object Tchart, ComboBox cmb);
           public do_reflush_combox work_func;
            public MyComboboxItem()
            {
                StackPanel stack = new StackPanel();
                //　 设置StackPanel中的内容水平排列
                stack.Orientation = Orientation.Horizontal;
                stack.Height = 16;
                cb = new CheckBox();
                //　 向StackPanel对象中添加一个图标对象
                stack.Children.Add(cb);
                cb.Click += (System.Windows.RoutedEventHandler)delegate{
                       IsSelected = (bool)cb.IsChecked;
                       work_func(teeChart,cmbox);
                };
                
                //　 创建用于添加文本信息的TextBlock对象
                textBlock = new TextBlock();
                textBlock.Text = ComboBoxItem.NameProperty.Name;
                //　 向StackPanel对象中添加文本信息
                stack.Children.Add(textBlock);
                Content = stack;
            }
            //　 用于设置或获得节点中的文本信息
            public string itemName
            {
                set
                {
                    textBlock.Text = value;
                }
                get
                {
                    return textBlock.Text;
                }
            }
            public bool? isChecked
            {
                set
                {
                    cb.IsChecked = value;
                }
                get
                {
                    return cb.IsChecked;
                }
            }
        }

    public class MyComboboxItem_ForSaveData : ComboBoxItem
    {
        TextBlock textBlock;
        CheckBox checkbox;
        public object teeChart { get; set; }
        public ComboBox cmbox { get; set; }
        CustomWPFColorPicker.ColorPickerControlView pikerLiner;
        public delegate void do_reflush_combox(ComboBox cmb);
        public do_reflush_combox work_func;
        public MyComboboxItem_ForSaveData()
        {
            StackPanel stack = new StackPanel();
            //　 设置StackPanel中的内容水平排列
            stack.Orientation = Orientation.Horizontal;
            stack.Height = 16;
            checkbox = new CheckBox();
            //　 向StackPanel对象中添加一个图标对象
            stack.Children.Add(checkbox);
            checkbox.Click += (System.Windows.RoutedEventHandler)delegate
            {
                IsSelected = (bool)checkbox.IsChecked;
            };

            //　 创建用于添加文本信息的TextBlock对象
            textBlock = new TextBlock();
            textBlock.Text = ComboBoxItem.NameProperty.Name;
            //　 向StackPanel对象中添加文本信息
            stack.Children.Add(textBlock);
            Content = stack;
        }
        //　 用于设置或获得节点中的文本信息
        public string itemName
        {
            set
            {
                textBlock.Text = value;
            }
            get
            {
                return textBlock.Text;
            }
        }
        public bool? isChecked
        {
            set
            {
                checkbox.IsChecked = value;
            }
            get
            {
                return checkbox.IsChecked;
            }
        }
    }
        #endregion
}
