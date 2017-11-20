using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// LoadingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }
        public LoadingWindow(string title,string content)
        {
            InitializeComponent();
            this.Title = title;
            lab_tag.Content = content + ":";
            lab_单位.Content = "";
            lab_电压.Content = "";
            lab_电压值.Content = "";
        }
        public BackgroundWorker bgMeet;
        public delegate void GetProgress(LoadingWindow load);
        public GetProgress start;
        public GetProgress complete;
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            bgMeet = new BackgroundWorker();
            //能否报告进度更新  
            bgMeet.WorkerReportsProgress = true;
            //要执行的后台任务  
            bgMeet.DoWork += new DoWorkEventHandler(bgMeet_DoWork);
            //进度报告方法  
            bgMeet.ProgressChanged += new ProgressChangedEventHandler(bgMeet_ProgressChanged);
            //后台任务执行完成时调用的方法  
            bgMeet.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgMeet_RunWorkerCompleted);
            bgMeet.RunWorkerAsync(); //任务启动  
        }
        //执行任务  
        void bgMeet_DoWork(object sender, DoWorkEventArgs e)
        {
            //开始播放等待动画  
            this.Dispatcher.Invoke(new Action(() =>
            {
                loading.Visibility = System.Windows.Visibility.Visible;
            }));
            //开始后台任务  
           GetData();
        }
        //报告任务进度  
        void bgMeet_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.lab_pro.Content = e.ProgressPercentage + "%";
            }));
        }
        //任务执行完成后更新状态  
        void bgMeet_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.lab_pro.Content = "请等待......";
                if (lab_tag.Content.ToString() == "正在准备重传:")
                {
                    complete(this);
                }
            }));
        }
        //模拟耗时任务  
        public void GetData()
        {
            start(this);
        }
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

            bgMeet = new BackgroundWorker();
            //能否报告进度更新  
            bgMeet.WorkerReportsProgress = true;
            //要执行的后台任务  
            bgMeet.DoWork += new DoWorkEventHandler(bgMeet_DoWork);
            //进度报告方法  
            bgMeet.ProgressChanged += new ProgressChangedEventHandler(bgMeet_ProgressChanged);
            //后台任务执行完成时调用的方法  
            bgMeet.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgMeet_RunWorkerCompleted);
            bgMeet.RunWorkerAsync(); //任务启动  
        }  
    }
}
