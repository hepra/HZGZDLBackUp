using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CustomReport;
using CustomWPFColorPicker;
using GZDL_DEV.DEL;
using GZDL_DEV.model;
using Microsoft.Win32;
using Steema.TeeChart.WPF.Styles;
using Steema.TeeChart.WPF.Tools;

namespace GZDevicesMode {
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window {
		#region 变量
		//Tchart游标 字典 通过表名 +"1"or"2" 确定游标
		Dictionary<object, CursorTool> cursor_collection = new Dictionary<object, CursorTool>();
		Dictionary<object, CursorTool> cursor_collection_DC = new Dictionary<object, CursorTool>();
		/// <summary>
		/// 程序启动路径
		/// </summary>
		static string root_path = (System.AppDomain.CurrentDomain.BaseDirectory);
		/// <summary>
		/// 测试数据保存路径
		/// </summary>
		string test_data_path = root_path + "TestData\\";
		//分析表格 DataSource
		private ObservableCollection<class_AnalysisShow> DC_display_list = new ObservableCollection<class_AnalysisShow>();
		private ObservableCollection<class_AnalysisShow> AC_ThreePhase_display_list = new ObservableCollection<class_AnalysisShow>();
		private ObservableCollection<class_AnalysisShow> DC_display_list1 = new ObservableCollection<class_AnalysisShow>();
		private ObservableCollection<class_AnalysisShow> AC_ThreePhase_display_list1 = new ObservableCollection<class_AnalysisShow>();
		private ObservableCollection<class_AnalysisShow> DC_display_list2 = new ObservableCollection<class_AnalysisShow>();
		private ObservableCollection<class_AnalysisShow> AC_ThreePhase_display_list2 = new ObservableCollection<class_AnalysisShow>();
		class_AnalysisShow analysisshow_AC = new class_AnalysisShow();
		class_AnalysisShow analysisshow_DC = new class_AnalysisShow();
		/// <summary>
		/// 测试参数
		/// </summary>
		public class_TestParameter test_parameter = new class_TestParameter();
		/// <summary>
		/// 变压器参数
		/// </summary>
		public class_TransformerParameter transformer_parameter = new class_TransformerParameter();
		//下发命令
		class_Commander Commander = new class_Commander();
		private ManualResetEvent DrawDone = new ManualResetEvent(false);
		string HeaderA = "A相";
		string HeaderB = "B相";
		string HeaderC = "C相";
		//峰值初始化
		bool isPeakVale_initial = true;
		//记录被选中的checkbox index；
		List<int> checked_pos = new List<int>();
		//记录ABC相数据
		public static List<int> quxianA = new List<int>();
		public static List<int> quxianB = new List<int>();
		public static List<int> quxianC = new List<int>();
		int Page_Max_count = 150;
		Dictionary<string, double> TrangleDataGridSort = new Dictionary<string, double>();
		Queue<List<int>> quxian_Show_areaAphase = new Queue<List<int>>();
		Queue<List<int>> quxian_Show_areaBphase = new Queue<List<int>>();
		Queue<List<int>> quxian_Show_areaCphase = new Queue<List<int>>();
		/// <summary>
		/// 数据缓存队列 存的是来不及处理的数据
		/// </summary>
		Queue<byte[]> A_data = new Queue<byte[]>(20000);
		Queue<byte[]> B_data = new Queue<byte[]>(20000);
		Queue<byte[]> C_data = new Queue<byte[]>(20000);
		Queue<int[]> quxian_Show_Trig_Aphase = new Queue<int[]>(600);
		Queue<int[]> quxian_Show_Trig_Bphase = new Queue<int[]>(600);
		Queue<int[]> quxian_Show_Trig_Cphase = new Queue<int[]>(600);
		//FastLine 总共 9条 三个表 每个表三条  三相
		public static List<FastLine> line_forAnalysis_chart1 = new List<FastLine>();
		public static List<FastLine> line_forAnalysis_chart2 = new List<FastLine>();
		public static List<FastLine> line_forTest = new List<FastLine>();
		//TCP 连接窗口
		TCP_Connect_SettingWindow TCP连接窗口;
		//测试设置窗口
		TestSetupWindow 测试设置窗口;
		WindowTipsForSaveData 数据保存提示窗口 = new WindowTipsForSaveData();
		//变压器属性设置窗口
		TransForm_SettingWindow 变压器属性设置窗口;
		LoadingWindow load;
		waitForAnalysis wait_analysis;

		//轮询系统状态
		DispatcherTimer Get_StateTimer = new DispatcherTimer();
		DispatcherTimer Draw_line_Timer = new DispatcherTimer();
		/// <summary>
		/// 系统状态 枚举类型
		/// </summary>
		public enum State {
			/// <summary>
			/// 系统状态OK
			/// </summary>
			_0STAT_OK = 0,
			/// <summary>
			/// 电源板升压失败
			/// </summary>
			_1STAT_DSPPOWERUP_FAIL = 1,
			/// <summary>
			/// 获取峰值失败
			/// </summary>
			_2STAT_DSPFFGET_FAIL = 2,
			/// <summary>
			/// 电源板通信失败
			/// </summary>
			_3STAT_DSPCOM_FAIL = 3,
			/// <summary>
			/// 电源板降压失败
			/// </summary>
			_4STAT_DSPPOWERDOWN_FAIL = 4,
			_5STAT_DONTRUNAGAIN = 5,
			_6STAT_POWER_SHAKE = 6,
			_7STAT_READY_FOR_MEASURE = 7,
			_8STAT_POWER_STARTUP
		}
		Thread th_read;
		Thread th_tolist;
		Thread th_analysis;

		#endregion

		#region 构造函数
		public MainWindow() {
			InitializeComponent();
			tabTestInfo.DataContext = test_parameter;
			Binding binding = new Binding();
			binding.Source = test_parameter;
			binding.Path = new PropertyPath("_24IsACMeasurment");
			binding.Mode = BindingMode.OneWay;
			tabACInfo.SetBinding(TabItem.IsEnabledProperty, binding);
			tabACInfo.SetBinding(TabItem.IsSelectedProperty, binding);

			Binding binding_DC = new Binding();
			binding_DC.Source = test_parameter;
			binding_DC.Path = new PropertyPath("_25IsDCMeasurment");
			binding_DC.Mode = BindingMode.OneWay;
			tabDCInfo.SetBinding(TabItem.IsEnabledProperty, binding_DC);
			tabDCInfo.SetBinding(TabItem.IsSelectedProperty, binding_DC);

			//测试表格初始化
			Teechart_Initial("波形", Tchart1);
			//分析表格1初始化
			Teechart_Initial("波形", Tchart_1);
			//分析表格2初始化
			Teechart_Initial("波形", Tchart_2);
			//测试波形ABC 初始化
			fastLine_Initial("A相曲线", line_forTest, Tchart1);
			fastLine_Initial("B相曲线", line_forTest, Tchart1);
			fastLine_Initial("C相曲线", line_forTest, Tchart1);
			//分析波形ABC 初始化
			fastLine_Initial("A相分析波形", line_forAnalysis_chart1, Tchart_1);
			fastLine_Initial("B相分析波形", line_forAnalysis_chart1, Tchart_1);
			fastLine_Initial("C相分析波形", line_forAnalysis_chart1, Tchart_1);
			fastLine_Initial("A相分析波形", line_forAnalysis_chart2, Tchart_2);
			fastLine_Initial("B相分析波形", line_forAnalysis_chart2, Tchart_2);
			fastLine_Initial("C相分析波形", line_forAnalysis_chart2, Tchart_2);
		}
		#endregion

		#region 设置表格游标 位置和颜色

		void lablcursor_update(CursorTool cursor, Label lb) {
			try {
				switch (cursor.Tag.ToString()) {
					case "Tchart1A1":
					case "Tchart1A2":
					case "Tchart_1A1":
					case "Tchart_1A2":
					case "Tchart_2A1":
					case "Tchart_2A2":
						lb.Content = cursor.XValue.ToString("0.#");
						if (cursor.Tag.ToString().Contains("A1")) {
							lbCurorA.Content = cursor.XValue.ToString("0.#");
							lbCurorB.Content = cursor_collection[cursor.Tag.ToString().Replace("A1", "A2")].XValue.ToString("0.#");
						}
						else if (cursor.Tag.ToString().Contains("A2")) {
							lbCurorB.Content = cursor.XValue.ToString("0.#");
							lbCurorA.Content = cursor_collection[cursor.Tag.ToString().Replace("A2", "A1")].XValue.ToString("0.#");
						}
						lbGapA.Content = ((double.Parse(lbCurorB.Content.ToString()) - double.Parse(lbCurorA.Content.ToString()))).ToString("0.#");
						analysisshow_AC.Aphase_Value = lbGapA.Content.ToString();
						break;
					case "Tchart1B1":
					case "Tchart1B2":
					case "Tchart_1B1":
					case "Tchart_1B2":
					case "Tchart_2B1":
					case "Tchart_2B2":
						lb.Content = cursor.XValue.ToString("0.#");
						if (cursor.Tag.ToString().Contains("B1")) {
							lbCurorA.Content = cursor.XValue.ToString("0.#");
							lbCurorB.Content = cursor_collection[cursor.Tag.ToString().Replace("B1", "B2")].XValue.ToString("0.#");
						}
						else if (cursor.Tag.ToString().Contains("B2")) {
							lbCurorB.Content = cursor.XValue.ToString("0.#");
							lbCurorA.Content = cursor_collection[cursor.Tag.ToString().Replace("B2", "B1")].XValue.ToString("0.#");
						}
						lbGapB.Content = ((double.Parse(lbCurorB.Content.ToString()) - double.Parse(lbCurorA.Content.ToString()))).ToString("0.#");
						analysisshow_AC.Bphase_Value = lbGapB.Content.ToString();
						break;
					case "Tchart1C1":
					case "Tchart1C2":
					case "Tchart_1C1":
					case "Tchart_1C2":
					case "Tchart_2C1":
					case "Tchart_2C2":
						lb.Content = cursor.XValue.ToString("0.#");
						if (cursor.Tag.ToString().Contains("C1")) {
							lbCurorA.Content = cursor.XValue.ToString("0.#");
							lbCurorB.Content = cursor_collection[cursor.Tag.ToString().Replace("C1", "C2")].XValue.ToString("0.#");
						}
						else if (cursor.Tag.ToString().Contains("C2")) {
							lbCurorB.Content = cursor.XValue.ToString("0.#");
							lbCurorA.Content = cursor_collection[cursor.Tag.ToString().Replace("C2", "C1")].XValue.ToString("0.#");
						}
						lbGapC.Content = ((double.Parse(lbCurorB.Content.ToString()) - double.Parse(lbCurorA.Content.ToString()))).ToString("0.#");
						analysisshow_AC.Cphase_Value = lbGapC.Content.ToString();
						break;
					case "Tchart1A1_DC":
					case "Tchart1A2_DC":
					case "Tchart_1A1_DC":
					case "Tchart_1A2_DC":
					case "Tchart_2A1_DC":
					case "Tchart_2A2_DC":
						lb.Content = cursor.XValue.ToString("0.#");
						if (cursor.Tag.ToString().Contains("A1")) {
							lbCurorA.Content = cursor.XValue.ToString("0.#");
							lbCurorB.Content = cursor_collection_DC[cursor.Tag.ToString().Replace("A1", "A2")].XValue.ToString("0.#");
						}
						else if (cursor.Tag.ToString().Contains("A2")) {
							lbCurorB.Content = cursor.XValue.ToString("0.#");
							lbCurorA.Content = cursor_collection_DC[cursor.Tag.ToString().Replace("A2", "A1")].XValue.ToString("0.#");
						}
						lbGapA2.Content = ((double.Parse(lbCurorB.Content.ToString()) - double.Parse(lbCurorA.Content.ToString()))).ToString("0.#");
						break;
					case "Tchart1B1_DC":
					case "Tchart1B2_DC":
					case "Tchart_1B1_DC":
					case "Tchart_1B2_DC":
					case "Tchart_2B1_DC":
					case "Tchart_2B2_DC":
						lb.Content = cursor.XValue.ToString("0.#");
						if (cursor.Tag.ToString().Contains("B1")) {
							lbCurorA.Content = cursor.XValue.ToString("0.#");
							lbCurorB.Content = cursor_collection_DC[cursor.Tag.ToString().Replace("B1", "B2")].XValue.ToString("0.#");
						}
						else if (cursor.Tag.ToString().Contains("B2")) {
							lbCurorB.Content = cursor.XValue.ToString("0.#");
							lbCurorA.Content = cursor_collection_DC[cursor.Tag.ToString().Replace("B2", "B1")].XValue.ToString("0.#");
						}
						lbGapB2.Content = ((double.Parse(lbCurorB.Content.ToString()) - double.Parse(lbCurorA.Content.ToString()))).ToString("0.#");
						break;
					case "Tchart1C1_DC":
					case "Tchart1C2_DC":
					case "Tchart_1C1_DC":
					case "Tchart_1C2_DC":
					case "Tchart_2C1_DC":
					case "Tchart_2C2_DC":
						lb.Content = cursor.XValue.ToString("0.#");
						if (cursor.Tag.ToString().Contains("C1")) {
							lbCurorA.Content = cursor.XValue.ToString("0.#");
							lbCurorB.Content = cursor_collection_DC[cursor.Tag.ToString().Replace("C1", "C2")].XValue.ToString("0.#");
						}
						else if (cursor.Tag.ToString().Contains("C2")) {
							lbCurorB.Content = cursor.XValue.ToString("0.#");
							lbCurorA.Content = cursor_collection_DC[cursor.Tag.ToString().Replace("C2", "C1")].XValue.ToString("0.#");
						}
						lbGapC2.Content = ((double.Parse(lbCurorB.Content.ToString()) - double.Parse(lbCurorA.Content.ToString()))).ToString("0.#");
						break;
				}
			}
			catch {

			}
			DataGridUpdate(cursor.Tag.ToString());

		}
		//设置游标位置
		void set_cursor_position(CursorTool cursor, double Xvalue) {
			cursor.XValue = Xvalue;
			cursor.Active = true;
		}
		void set_cursor_position(CursorTool cursor, double Xvalue, bool isShow) {
			cursor.XValue = Xvalue;
			cursor.Active = isShow;
		}
		#endregion

		#region Tchart 游标 曲线 属性初始化
		void fastLine_Initial(string title, List<FastLine> line_list, Steema.TeeChart.WPF.TChart chart) {
			FastLine temp = new FastLine(chart.Chart);
			temp.Title = title;
			//MarksTip tooltip1 = new MarksTip(Tchart1.Chart);
			//tooltip1.Series = temp;
			//temp.AutoRepaint = true;
			//tooltip1.MouseAction = Steema.TeeChart.WPF.Tools.MarksTipMouseAction.Move;
			//tooltip1.Style = Steema.TeeChart.WPF.Styles.MarksStyles.SeriesTitle;
			try {
				Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
				chartPen.Width = double.Parse(tb_LineWidth.Text);
				temp.LinePen = chartPen;
			}
			catch (Exception error) {
				MessageBox.Show("请输入数字!\r\n" + error.Message, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			line_list.Add(temp);

		}
		void Cursor_Set(CursorTool cursor, string cursor_Tag_Name, Color cursor_color, Steema.TeeChart.WPF.TChart chart) {
			cursor.Tag = cursor_Tag_Name;
			cursor_collection.Add(cursor_Tag_Name, cursor);
			if (cursor.Tag.ToString().Replace(chart.Name, "").Contains("1")) {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorA);
				};
			}
			else {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorB);
				};
			}
			cursor.Active = true;
			cursor.Style = CursorToolStyles.Vertical;
			cursor.FollowMouse = false;
			cursor.ScopeSize = 1;
			cursor.ScopeStyle = ScopeCursorStyle.Empty;
			cursor.CursorClickTolerance = 3;
			cursor.OriginalCursor = Cursors.Hand;
			cursor.Pen = new Steema.TeeChart.WPF.Drawing.ChartPen(Tchart1.Chart, cursor_color);
			cursor.Pen.Width = 2;
			cursor.Pen.Style = DashStyles.Dash;
			chart.Tools.Add(cursor);
		}
		void Cursor_Set_xuxian(CursorTool cursor, string cursor_Tag_Name, Color cursor_color, Steema.TeeChart.WPF.TChart chart) {
			cursor.Tag = cursor_Tag_Name;
			cursor_collection.Add(cursor_Tag_Name, cursor);
			if (cursor.Tag.ToString().Replace(chart.Name, "").Contains("1")) {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorA);
				};
			}
			else {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorB);
				};
			}
			cursor.Active = true;
			cursor.Style = CursorToolStyles.Vertical;
			cursor.FollowMouse = false;
			cursor.ScopeSize = 1;
			cursor.ScopeStyle = ScopeCursorStyle.Empty;
			cursor.CursorClickTolerance = 3;
			cursor.Pen = new Steema.TeeChart.WPF.Drawing.ChartPen(Tchart1.Chart, cursor_color);
			cursor.Pen.Width = 2;
			chart.Tools.Add(cursor);
		}
		void Cursor_Set_DC(CursorTool cursor, string cursor_Tag_Name, Color cursor_color, Steema.TeeChart.WPF.TChart chart) {
			cursor.Tag = cursor_Tag_Name;
			cursor_collection_DC.Add(cursor_Tag_Name, cursor);
			if (cursor.Tag.ToString().Replace(chart.Name, "").Contains("1")) {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorA);
				};
			}
			else {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorB);
				};
			}
			cursor.Active = true;
			cursor.Style = CursorToolStyles.Vertical;
			cursor.FollowMouse = false;
			cursor.ScopeSize = 1;
			cursor.CursorClickTolerance = 3;

			cursor.Pen = new Steema.TeeChart.WPF.Drawing.ChartPen(Tchart1.Chart, cursor_color);
			cursor.Pen.Width = 2;
			cursor.Pen.Style = DashStyles.Dash;
			chart.Tools.Add(cursor);
		}
		void Cursor_Set_DC_xuxian(CursorTool cursor, string cursor_Tag_Name, Color cursor_color, Steema.TeeChart.WPF.TChart chart) {
			cursor.Tag = cursor_Tag_Name;
			cursor_collection_DC.Add(cursor_Tag_Name, cursor);
			if (cursor.Tag.ToString().Replace(chart.Name, "").Contains("1")) {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorA);
				};
			}
			else {
				cursor.Change += (CursorChangeEventHandler)delegate {
					lablcursor_update(cursor, lbCurorB);
				};
			}
			cursor.Active = true;
			cursor.Style = CursorToolStyles.Vertical;
			cursor.FollowMouse = false;
			cursor.ScopeSize = 1;
			cursor.CursorClickTolerance = 3;
			cursor.Pen = new Steema.TeeChart.WPF.Drawing.ChartPen(Tchart1.Chart, cursor_color);
			cursor.Pen.Width = 2;
			chart.Tools.Add(cursor);
		}

		void Teechart_Initial(string ChartName, Steema.TeeChart.WPF.TChart chart) {
			//3D关闭
			chart.Aspect.View3D = false;
			chart.Legend.Visible = false;
			Max = 0;
			chart.Chart.Walls.Bottom.Visible = true;
			chart.Chart.Walls.Left.Visible = true;
			//设置附加信息框显示的 信息 为序列 名称
			chart.Chart.Legend.LegendStyle = Steema.TeeChart.WPF.LegendStyles.Series;
			//坐标轴是否可见
			chart.Axes.Left.Visible = true;
			chart.Axes.Left.Grid.Style = DashStyles.Dash;
			chart.Axes.Bottom.Visible = true;
			chart.Axes.Bottom.Grid.Style = DashStyles.Dash;
			chart.Axes.Bottom.Increment = 0.01;
			chart.Axes.Left.Increment = 0.01;
			//设置坐标轴名称
			Steema.TeeChart.WPF.AxisTitle bottom_title = new Steema.TeeChart.WPF.AxisTitle(chart.Chart, chart.Chart.Axes.Bottom);
			bottom_title.Text = "时间(ms)";
			// bottom_title.Color = PickerLine.CurrentColor.Color;
			chart.Axes.Bottom.Title.Text = "时间(ms)";
			//chart.Axes.Bottom.Title.Color = PickerLine.CurrentColor.Color;
			chart.Axes.Left.Title.Text = "电流(mA)";
			//chart.Axes.Left.Title.Color = PickerLine.CurrentColor.Color;
			//设置游标
			chart.Cursor = System.Windows.Input.Cursors.Hand;
			// 设置表名
			chart.Chart.Header.Text = ChartName;
			chart.Chart.Axes.Bottom.SetMinMax(0, 600);
			chart.Chart.Axes.Left.SetMinMax(-20, 20);
			//设置 游标
			CursorTool cursor1 = new CursorTool(chart.Chart);
			CursorTool cursor2 = new CursorTool(chart.Chart);
			CursorTool cursor3 = new CursorTool(chart.Chart);
			CursorTool cursor4 = new CursorTool(chart.Chart);
			CursorTool cursor5 = new CursorTool(chart.Chart);
			CursorTool cursor6 = new CursorTool(chart.Chart);
			CursorTool cursor1_DC = new CursorTool(chart.Chart);
			CursorTool cursor2_DC = new CursorTool(chart.Chart);
			CursorTool cursor3_DC = new CursorTool(chart.Chart);
			CursorTool cursor4_DC = new CursorTool(chart.Chart);
			CursorTool cursor5_DC = new CursorTool(chart.Chart);
			CursorTool cursor6_DC = new CursorTool(chart.Chart);
			CursorTool cursorHorztion = new CursorTool(chart.Chart);
			cursorHorztion.FollowMouse = false;
			cursorHorztion.Active = true;
			cursorHorztion.Style = CursorToolStyles.Horizontal;
			cursorHorztion.Change += (CursorChangeEventHandler)delegate {
				cursorHorztion.YValue = 0;
			};
			cursorHorztion.ScopeSize = 2;
			cursorHorztion.ScopeStyle = ScopeCursorStyle.Empty;
			cursorHorztion.CursorClickTolerance = 3;
			cursorHorztion.Pen = new Steema.TeeChart.WPF.Drawing.ChartPen(Tchart1.Chart, Colors.Black);
			cursorHorztion.Pen.Style = DashStyles.Dash;
			cursorHorztion.Pen.Width = 2;
			cursor1.Tag = chart.Name + "A1";
			cursor2.Tag = chart.Name + "A2";
			cursor3.Tag = chart.Name + "B1";
			cursor4.Tag = chart.Name + "B2";
			cursor5.Tag = chart.Name + "C1";
			cursor6.Tag = chart.Name + "C2";
			cursor1_DC.Tag = chart.Name + "A1_DC";
			cursor2_DC.Tag = chart.Name + "A2_DC";
			cursor3_DC.Tag = chart.Name + "B1_DC";
			cursor4_DC.Tag = chart.Name + "B2_DC";
			cursor5_DC.Tag = chart.Name + "C1_DC";
			cursor6_DC.Tag = chart.Name + "C2_DC";
			Cursor_Set(cursor1, cursor1.Tag.ToString(), Color.FromRgb(0xFF, 0x83, 0x07), chart);
			Cursor_Set_xuxian(cursor2, cursor2.Tag.ToString(), Color.FromRgb(0xFF, 0x83, 0x07), chart);
			Cursor_Set(cursor3, cursor3.Tag.ToString(), Color.FromRgb(0x00, 0x80, 0x00), chart);
			Cursor_Set_xuxian(cursor4, cursor4.Tag.ToString(), Color.FromRgb(0x00, 0x80, 0x00), chart);
			Cursor_Set(cursor5, cursor5.Tag.ToString(), Color.FromRgb(0xFF, 0x00, 0x00), chart);
			Cursor_Set_xuxian(cursor6, cursor6.Tag.ToString(), Color.FromRgb(0xFF, 0x00, 0x00), chart);
			Cursor_Set_DC(cursor1_DC, cursor1_DC.Tag.ToString(), Color.FromRgb(0xff, 0x99, 0x00), chart);
			Cursor_Set_DC_xuxian(cursor2_DC, cursor2_DC.Tag.ToString(), Color.FromRgb(0xff, 0x99, 0x00), chart);
			Cursor_Set_DC(cursor3_DC, cursor3_DC.Tag.ToString(), Color.FromRgb(0x00, 0x33, 0x00), chart);
			Cursor_Set_DC_xuxian(cursor4_DC, cursor4_DC.Tag.ToString(), Color.FromRgb(0x00, 0x33, 0x00), chart);
			Cursor_Set_DC(cursor5_DC, cursor5_DC.Tag.ToString(), Color.FromRgb(0x99, 0x00, 0x00), chart);
			Cursor_Set_DC_xuxian(cursor6_DC, cursor6_DC.Tag.ToString(), Color.FromRgb(0x99, 0x00, 0x00), chart);
		}
		#endregion

		#region FastLine线条颜色控制
		public void ChangeColorA(SolidColorBrush color) {
			line_forTest[0].Color = color.Color;
		}
		public void ChangeColorB(SolidColorBrush color) {
			line_forTest[1].Color = color.Color;
		}
		public void ChangeColorC(SolidColorBrush color) {
			line_forTest[2].Color = color.Color;
		}
		#endregion

		#region 连接仪器
		public void UI_logic() {
			try {
				if (TCP连接窗口.is_Client_create_success == false) {
					this.Dispatcher.Invoke(new Action(delegate {
						btnDataAnalysis.IsEnabled = false;
						btnSystemSetting.IsEnabled = false;
						btnTransformerConfig.IsEnabled = false;
						btnPauseTest.IsEnabled = false;
						btnReTest.IsEnabled = false;
						btnSartTest.IsEnabled = false;
						btnStopTest.IsEnabled = false;
						btnSystemSetting.IsEnabled = false;
						btnTransformerConfig.IsEnabled = false;
						btnConnectDevice.IsEnabled = true;
						TCP连接窗口.btnConnect.IsEnabled = true;
						TCP连接窗口.btnCancel.IsEnabled = false;
						if (load != null) {
							load.Close();
						}
						Get_StateTimer.Stop();
						if (load != null) {
							load.Close();
						}
						fun_资源释放线程回收();
						TCP连接窗口.Show();
						return;
					}));
				}
				else if (TCP连接窗口.is_Sever_create_success == false) {
					this.Dispatcher.Invoke(new Action(delegate {
						btnSystemSetting.IsEnabled = false;
						btnTransformerConfig.IsEnabled = false;
						btnDataAnalysis.IsEnabled = false;
						btnPauseTest.IsEnabled = false;
						btnReTest.IsEnabled = false;
						btnSartTest.IsEnabled = false;
						btnStopTest.IsEnabled = false;
						btnSystemSetting.IsEnabled = false;
						btnTransformerConfig.IsEnabled = false;
						btnConnectDevice.IsEnabled = true;
						Get_StateTimer.Stop();
						TCP连接窗口.Show();
					}));
					return;
				}
				else {
					this.Dispatcher.Invoke(new Action(delegate {
						Get_StateTimer.Start();
						btnSystemSetting.IsEnabled = true;
						btnTransformerConfig.IsEnabled = true;
						btnDataAnalysis.IsEnabled = false;
						btnPauseTest.IsEnabled = false;
						btnReTest.IsEnabled = false;
						btnSartTest.IsEnabled = true;
						btnStopTest.IsEnabled = false;
						btnSystemSetting.IsEnabled = true;
						btnTransformerConfig.IsEnabled = true;
						btnConnectDevice.IsEnabled = false;
					}));
					return;
				}
			}
			catch (Exception error) {
				MessageBox.Show("未检测到已连接仪器,无法执行此操作\r\n" + error.Message, "错误", MessageBoxButton.OK,
					MessageBoxImage.Error);
				this.Dispatcher.Invoke(new Action(delegate {
							btnConnectDevice.IsEnabled = true;
							if (TCP连接窗口 == null) {
								TCP连接窗口 = new TCP_Connect_SettingWindow();
								TCP连接窗口.logic = UI_logic;
								TCP连接窗口.handshake = handshake;
								TCP连接窗口.Message_receive = Tcp_message_receive_As_Sever;
								TCP连接窗口.Client_msg_rec = Tcp_message_receive_As_Client;
								Get_StateTimer.Stop();
							}
							TCP连接窗口.Show();
						}));
			}

		}
		public void handshake() {
			CMD_Send(Commander._1_CMD_HANDSHAKE);
			CMD_Send(Commander._3_CMD_STOPMEASURE);
		}
		void connect_device() {
			if (TCP连接窗口 == null) {
				TCP连接窗口 = new TCP_Connect_SettingWindow();
				TCP连接窗口.logic = UI_logic;
				TCP连接窗口.handshake = handshake;
				TCP连接窗口.Message_receive = Tcp_message_receive_As_Sever;
				TCP连接窗口.Client_msg_rec = Tcp_message_receive_As_Client;
				load = new LoadingWindow();
				load.start = send;
				load.Owner = GZDLMainWindow;
			}
			if (!TCP连接窗口.is_Sever_create_success) {
				TCP连接窗口.Create_Sever();
			}
			if (!TCP连接窗口.is_Client_create_success) {
				if (TCP连接窗口.socket_Client != null) {
					if (TCP连接窗口.socket_Client.Connected) {
						CMD_Send(Commander._3_CMD_STOPMEASURE);
						Get_StateTimer.Start();
						TCP连接窗口.Hide();
					}
					else {
						TCP连接窗口.create_TCPClient();
					}
				}
				else {
					TCP连接窗口.create_TCPClient();
				}
				
			}
		}
		#endregion

		#region 变压器参数配置
		void transformer_configuration() {
			if (变压器属性设置窗口 == null) {
				变压器属性设置窗口 = new TransForm_SettingWindow(TreeViewTestItem, TestStage_Update);
				变压器属性设置窗口.DataContext = transformer_parameter;
			}
			//根据所选的变压器匹配最新一次的测试属性
			#region 已存在单位ComboxSelectionChange
			变压器属性设置窗口.cmbExistCompany.DropDownClosed += (EventHandler)delegate {
				if (变压器属性设置窗口.cmbExistCompany.SelectedItem != null) {
					transformer_parameter._1ItsUnitName = 变压器属性设置窗口.cmbExistCompany.SelectedItem.ToString();
					变压器属性设置窗口.cmbExistTransFormers.Items.Clear();
					DataSet ds = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name);
					foreach (DataRow row in ds.Tables[0].Rows) {
						if (row[1].Equals(transformer_parameter._1ItsUnitName)) {
							if (row[1].Equals(transformer_parameter._1ItsUnitName)) {
								bool add_trans = true;
								foreach (var item in 变压器属性设置窗口.cmbExistTransFormers.Items) {
									string switchproducter = row[(int)OleDbHelper.File_Name_e._2TransFormName + 1].ToString();
									if (item.Equals(switchproducter)) {
										add_trans = false;
										continue;
									}
								}
								if (add_trans) {
									变压器属性设置窗口.cmbExistTransFormers.Items.Add(row[2]);
									if (测试设置窗口 != null) {
										测试设置窗口.cmbTransformerSelect.Items.Add(row[2]);
										测试设置窗口.cmbTransformerSelect.SelectedItem = null;
									}
								}

							}
						}
					}
				}
			};
			#endregion
			#region 已存在变压器ComboxSelectionChange
			变压器属性设置窗口.cmbExistTransFormers.DropDownClosed += (EventHandler)delegate {
				if (变压器属性设置窗口.cmbExistTransFormers.SelectedItem != null && transformer_parameter._1ItsUnitName != null) {
					transformer_parameter._2TransformerName = 变压器属性设置窗口.cmbExistTransFormers.SelectedItem.ToString();
					get_access_info(transformer_parameter._2TransformerName);
					test_parameter._1curTransformerName = transformer_parameter._2TransformerName;
				}
				else {
					MessageBox.Show("请设置变压器所属单位");
					变压器属性设置窗口.cmbExistCompany.IsDropDownOpen = true;
					return;
				}
			};
			#endregion
			变压器属性设置窗口.cmbExistCompany.Items.Clear();
			变压器属性设置窗口.cmbSwitchProducerName.Items.Clear();
			变压器属性设置窗口.cmbSwitchModel.Items.Clear();
			#region 将数据库中单位,变压器型号,变压器制造厂家全部导入Combox
			DataSet ds1 = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name);
			foreach (DataRow row in ds1.Tables[0].Rows) {
				bool add = true;
				bool addSwitchP = true;
				bool addSwitchM = true;
				foreach (var item in 变压器属性设置窗口.cmbExistCompany.Items) {
					string companyname = row[1].ToString();
					if (item.Equals(companyname)) {
						add = false;
						continue;
					}
				}
				foreach (var item in 变压器属性设置窗口.cmbSwitchModel.Items) {
					string switchmodel = row[(int)OleDbHelper.File_Name_e._21SwitchModel + 1].ToString();
					if (item.Equals(switchmodel)) {
						addSwitchM = false;
						continue;
					}
				}
				foreach (var item in 变压器属性设置窗口.cmbSwitchProducerName.Items) {
					string switchproducter = row[(int)OleDbHelper.File_Name_e._20SwitchManufactorName + 1].ToString();
					if (item.Equals(switchproducter)) {
						addSwitchP = false;
						continue;
					}
				}
				if (add) {
					变压器属性设置窗口.cmbExistCompany.Items.Add(row[1]);
				}
				if (addSwitchP) {
					变压器属性设置窗口.cmbSwitchProducerName.Items.Add(row[(int)OleDbHelper.File_Name_e._20SwitchManufactorName + 1].ToString());
				}
				if (addSwitchM) {
					变压器属性设置窗口.cmbSwitchModel.Items.Add(row[(int)OleDbHelper.File_Name_e._21SwitchModel + 1].ToString());
				}
			}
			#endregion
			if (transformer_parameter._1ItsUnitName != null) {
				for (int i = 0; i < 变压器属性设置窗口.cmbExistCompany.Items.Count; i++) {
					if (变压器属性设置窗口.cmbExistCompany.Items[i].ToString() == transformer_parameter._1ItsUnitName) {
						变压器属性设置窗口.cmbExistCompany.SelectedIndex = i;
					}
				}
				变压器属性设置窗口.cmbExistTransFormers.Items.Clear();
				DataSet ds = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name);

				foreach (DataRow row in ds.Tables[0].Rows) {
					if (row[1].Equals(transformer_parameter._1ItsUnitName)) {
						bool add_trans = true;
						foreach (var item in 变压器属性设置窗口.cmbExistTransFormers.Items) {
							string switchproducter = row[(int)OleDbHelper.File_Name_e._2TransFormName + 1].ToString();
							if (item.Equals(switchproducter)) {
								add_trans = false;
								continue;
							}
						}
						if (add_trans) {
							变压器属性设置窗口.cmbExistTransFormers.Items.Add(row[2]);
							if (测试设置窗口 != null) {
								测试设置窗口.cmbTransformerSelect.Items.Add(row[2]);
								测试设置窗口.cmbTransformerSelect.SelectedItem = null;
							}
						}

					}
				}

				if (transformer_parameter._2TransformerName != null) {

					for (int i = 0; i < 变压器属性设置窗口.cmbExistTransFormers.Items.Count; i++) {
						if (变压器属性设置窗口.cmbExistTransFormers.Items[i].ToString() == transformer_parameter._2TransformerName) {
							变压器属性设置窗口.cmbExistTransFormers.SelectedIndex = i;
							get_access_info(transformer_parameter._2TransformerName);
						}
					}
					for (int i = 0; i < 变压器属性设置窗口.cmbSwitchModel.Items.Count; i++) {
						if (变压器属性设置窗口.cmbSwitchModel.Items[i].ToString() == transformer_parameter._21SwitchModel) {
							变压器属性设置窗口.cmbSwitchModel.SelectedIndex = i;
						}
					}
					for (int i = 0; i < 变压器属性设置窗口.cmbSwitchProducerName.Items.Count; i++) {
						if (变压器属性设置窗口.cmbSwitchProducerName.Items[i].ToString() == transformer_parameter._20SwitchManufactorName) {
							变压器属性设置窗口.cmbSwitchProducerName.SelectedIndex = i;
						}
					}
					if (测试设置窗口 != null) {
						for (int i = 0; i < 测试设置窗口.cmbTransformerSelect.Items.Count; i++) {
							if (测试设置窗口.cmbTransformerSelect.Items[i].ToString() == transformer_parameter._2TransformerName) {
								测试设置窗口.cmbTransformerSelect.SelectedIndex = i;
							}
						}
					}
				}
			}
			变压器属性设置窗口.Background = Brushes.AliceBlue;
			变压器属性设置窗口.Activate();
			变压器属性设置窗口.Show();
		}
		#endregion

		#region 测试参数配置
		void test_configuration() {
			if (测试设置窗口 == null) {
				测试设置窗口 = new TestSetupWindow(TestStage_Update);
				测试设置窗口.DataContext = test_parameter;
				测试设置窗口.useDefultPara();
				#region 每次加载时更新 测试分接位 的总数 并 选中上一次测试位置
				测试设置窗口.cmbOneCurTap.Items.Clear();
				if (transformer_parameter._24SwitchStartWorkingPosition != null && transformer_parameter._25SwitchStopWorkingPosition != null) {
					for (int i = int.Parse(transformer_parameter._24SwitchStartWorkingPosition); i <= int.Parse(transformer_parameter._25SwitchStopWorkingPosition); i++) {
						测试设置窗口.cmbOneCurTap.Items.Add(i);
						测试设置窗口.cmbOneCurTap.SelectedItem = null;
					}

				}
				#endregion
				test_parameter._0CompanyName = transformer_parameter._1ItsUnitName;
				测试设置窗口.btn_UseDefaultPara.Click += (RoutedEventHandler)delegate { 默认参数(); };
				return_default_Test_parameter();
				#region 测试分接位Combox变化
				测试设置窗口.cmbOneCurTap.SelectionChanged += (SelectionChangedEventHandler)delegate {
					if (测试设置窗口.cmbOneCurTap.SelectedItem != null) {
						测试设置窗口.rbBackSwitch.IsEnabled = true;
						测试设置窗口.rbForwardSwitch.IsEnabled = true;
						test_parameter._7SinglePointMeasurementCurTap = 测试设置窗口.cmbOneCurTap.SelectedItem.ToString();
						测试设置窗口.rbBackSwitch.Content = "向后切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + (int.Parse(测试设置窗口.cmbOneCurTap.SelectedItem.ToString()) + 1) + ")";

						if (测试设置窗口.cmbOneCurTap.SelectedItem.ToString() == transformer_parameter._25SwitchStopWorkingPosition) {
							测试设置窗口.rbBackSwitch.Content = "向前切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + "禁止" + ")";
							测试设置窗口.rbBackSwitch.IsChecked = false;
							测试设置窗口.rbForwardSwitch.IsChecked = true;

							测试设置窗口.rbBackSwitch.IsEnabled = false;
						}
						测试设置窗口.rbForwardSwitch.Content = "向前切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + (int.Parse(测试设置窗口.cmbOneCurTap.SelectedItem.ToString()) - 1) + ")";
						if (测试设置窗口.cmbOneCurTap.SelectedItem.ToString() == transformer_parameter._24SwitchStartWorkingPosition) {
							测试设置窗口.rbForwardSwitch.Content = "向前切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + "禁止" + ")";
							测试设置窗口.rbForwardSwitch.IsChecked = false;
							测试设置窗口.rbBackSwitch.IsChecked = true;

							测试设置窗口.rbForwardSwitch.IsEnabled = false;
						}
						if (测试设置窗口.cmbOneCurTap.SelectedItem != null) {
							test_parameter._7SinglePointMeasurementCurTap = 测试设置窗口.cmbOneCurTap.SelectedItem.ToString();
							#region 分接位记录
							if (test_parameter._24IsACMeasurment) {
								//交流连续自动测试
								if (test_parameter._14isAutoContinuousMearsurment) {
									if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
										test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
									}
									else {
										test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
									}

								}
								//交流单点测试
								else if (test_parameter._15isHandleSingleMearsurment) {
									if (test_parameter._8SinglePointMeasurementForwardSwitch) {
										test_parameter._48Access_position = "交流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) - 1).ToString() + "]";
									}
									else {
										test_parameter._48Access_position = "交流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) + 1).ToString() + "]";
									}
								}
							}
							else if (test_parameter._25IsDCMeasurment) {
								//直流连续自动测试
								if (test_parameter._14isAutoContinuousMearsurment) {
									if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
										test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
									}
									else {
										test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
									}

								}
								//直流单点测试
								else if (test_parameter._15isHandleSingleMearsurment) {
									if (test_parameter._8SinglePointMeasurementForwardSwitch) {
										test_parameter._48Access_position = "直流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) - 1).ToString() + "]";
									}
									else {
										test_parameter._48Access_position = "直流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) + 1).ToString() + "]";
									}
								}
							}
							#endregion
						}
					}
				};
				#endregion
				#region 所属单位 combox 变化
				测试设置窗口.cmbCompanySelect.DropDownClosed += (EventHandler)delegate {
					if (测试设置窗口.cmbCompanySelect.SelectedItem != null) {
						transformer_parameter._1ItsUnitName = 测试设置窗口.cmbCompanySelect.SelectedItem.ToString();
						test_parameter._0CompanyName = 测试设置窗口.cmbCompanySelect.SelectedItem.ToString();
						//将数据库中该单位所属的变压器全部导入Combox
						DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)transformer_parameter._1ItsUnitName);
						测试设置窗口.cmbTransformerSelect.Items.Clear();
						foreach (DataRow row in ds.Tables[0].Rows) {
							测试设置窗口.cmbTransformerSelect.Items.Add(row[2]);
							测试设置窗口.cmbTransformerSelect.SelectedItem = null;
						}
						//根据所选的变压器匹配最新一次的测试属性
						if (测试设置窗口.cmbTransformerSelect.Items.Count == 0) {
							MessageBox.Show("请先配置变压器信息!");
							transformer_configuration();
							测试设置窗口.Activate();
							测试设置窗口.Show();
							return;
						}
					}
				};
				测试设置窗口.btnDSP.Click += (RoutedEventHandler)delegate {
					byte[] data = new byte[8];
					byte[] temp;
					temp = BitConverter.GetBytes(0x8000000c);
					int index = 0;
					temp.CopyTo(data, index);
					index += temp.Length;
					temp = BitConverter.GetBytes(int.Parse(测试设置窗口.tbDSP.Text));
					temp.CopyTo(data, index);
					CMD_Send(data);
				};
				#endregion
				测试设置窗口.Background = Brushes.AliceBlue;
				测试设置窗口.Activate();
				测试设置窗口.Show();
			}
			测试设置窗口.btn_UseDefaultPara.Click += (RoutedEventHandler)delegate { 默认参数(); };
			#region 测试分接位Combox变化
			测试设置窗口.cmbOneCurTap.SelectionChanged += (SelectionChangedEventHandler)delegate {
				if (测试设置窗口.cmbOneCurTap.SelectedItem != null) {
					测试设置窗口.rbBackSwitch.IsEnabled = true;
					测试设置窗口.rbForwardSwitch.IsEnabled = true;
					test_parameter._7SinglePointMeasurementCurTap = 测试设置窗口.cmbOneCurTap.SelectedItem.ToString();
					测试设置窗口.rbBackSwitch.Content = "向后切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + (int.Parse(测试设置窗口.cmbOneCurTap.SelectedItem.ToString()) + 1) + ")";

					if (测试设置窗口.cmbOneCurTap.SelectedItem.ToString() == transformer_parameter._25SwitchStopWorkingPosition) {
						测试设置窗口.rbBackSwitch.Content = "向前切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + "禁止" + ")";
						测试设置窗口.rbBackSwitch.IsChecked = false;
						测试设置窗口.rbForwardSwitch.IsChecked = true;

						测试设置窗口.rbBackSwitch.IsEnabled = false;
					}
					测试设置窗口.rbForwardSwitch.Content = "向前切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + (int.Parse(测试设置窗口.cmbOneCurTap.SelectedItem.ToString()) - 1) + ")";
					if (测试设置窗口.cmbOneCurTap.SelectedItem.ToString() == transformer_parameter._24SwitchStartWorkingPosition) {
						测试设置窗口.rbForwardSwitch.Content = "向前切换(" + 测试设置窗口.cmbOneCurTap.SelectedItem.ToString() + "→" + "禁止" + ")";
						测试设置窗口.rbForwardSwitch.IsChecked = false;
						测试设置窗口.rbBackSwitch.IsChecked = true;

						测试设置窗口.rbForwardSwitch.IsEnabled = false;
					}
					if (测试设置窗口.cmbOneCurTap.SelectedItem != null) {
						test_parameter._7SinglePointMeasurementCurTap = 测试设置窗口.cmbOneCurTap.SelectedItem.ToString();
						#region 分接位记录
						if (test_parameter._24IsACMeasurment) {
							//交流连续自动测试
							if (test_parameter._14isAutoContinuousMearsurment) {
								if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
								}

							}
							//交流单点测试
							else if (test_parameter._15isHandleSingleMearsurment) {
								if (test_parameter._8SinglePointMeasurementForwardSwitch) {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) - 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) + 1).ToString() + "]";
								}
							}
						}
						else if (test_parameter._25IsDCMeasurment) {
							//直流连续自动测试
							if (test_parameter._14isAutoContinuousMearsurment) {
								if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
								}

							}
							//直流单点测试
							else if (test_parameter._15isHandleSingleMearsurment) {
								if (test_parameter._8SinglePointMeasurementForwardSwitch) {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) - 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) + 1).ToString() + "]";
								}
							}
						}
						#endregion
					}
				}
			};
			#endregion
			#region 所属单位 combox 变化
			测试设置窗口.cmbCompanySelect.DropDownClosed += (EventHandler)delegate {
				if (测试设置窗口.cmbCompanySelect.SelectedItem != null) {
					transformer_parameter._1ItsUnitName = 测试设置窗口.cmbCompanySelect.SelectedItem.ToString();
					test_parameter._0CompanyName = 测试设置窗口.cmbCompanySelect.SelectedItem.ToString();
					//将数据库中该单位所属的变压器全部导入Combox
					DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)transformer_parameter._1ItsUnitName);
					测试设置窗口.cmbTransformerSelect.Items.Clear();
					foreach (DataRow row in ds.Tables[0].Rows) {
						测试设置窗口.cmbTransformerSelect.Items.Add(row[2]);
						测试设置窗口.cmbTransformerSelect.SelectedItem = null;
					}
					//根据所选的变压器匹配最新一次的测试属性
					if (测试设置窗口.cmbTransformerSelect.Items.Count == 0) {
						MessageBox.Show("请先配置变压器信息!");
						transformer_configuration();
						return;
					}
				}
			};
			#endregion
			#region 交流输出电压
			if (测试设置窗口.cmbInnerSupplyVoltage.SelectedItem != null) {
				test_parameter._2OutputVolt = 测试设置窗口.cmbInnerSupplyVoltage.SelectedItem.ToString();
			}

			#endregion
			#region 如果已经配置了变压器信息 更新测试窗口 所属单位 下拉框
			if (transformer_parameter._1ItsUnitName != null) {
				test_parameter._0CompanyName = transformer_parameter._1ItsUnitName;
				测试设置窗口.cmbCompanySelect.Items.Clear();
				if (变压器属性设置窗口.cmbExistCompany.Items.Count == 0) {
					DataSet ds1 = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name);
					foreach (DataRow row in ds1.Tables[0].Rows) {
						bool add = true;
						foreach (var item in 测试设置窗口.cmbCompanySelect.Items) {
							string companyname = row[1].ToString();
							if (item.Equals(companyname)) {
								add = false;
								continue;
							}
						}
						if (add) {
							测试设置窗口.cmbCompanySelect.Items.Add(row[1]);
						}
					}
				}
				else {
					for (int i = 0; i < 变压器属性设置窗口.cmbExistCompany.Items.Count; i++) {
						测试设置窗口.cmbCompanySelect.Items.Add(变压器属性设置窗口.cmbExistCompany.Items[i].ToString());
					}
				}
				for (int i = 0; i < 测试设置窗口.cmbCompanySelect.Items.Count; i++) {
					if (测试设置窗口.cmbCompanySelect.Items[i].ToString() == transformer_parameter._1ItsUnitName) {
						测试设置窗口.cmbCompanySelect.SelectedIndex = i;
					}
				}
				#region 选择所属单位后 将所属单位 下所有变压器导入 变压器选择下拉框
				//将数据库中该单位所属的变压器全部导入Combox
				DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)transformer_parameter._1ItsUnitName);
				测试设置窗口.cmbTransformerSelect.Items.Clear();
				foreach (DataRow row in ds.Tables[0].Rows) {
					测试设置窗口.cmbTransformerSelect.Items.Add(row[2]);
					测试设置窗口.cmbTransformerSelect.SelectedItem = null;
				}
				//根据所选的变压器匹配最新一次的测试属性
				test_parameter._1curTransformerName = transformer_parameter._2TransformerName;
				if (测试设置窗口.cmbTransformerSelect.Items.Count == 0) {
					MessageBox.Show("请先配置变压器信息!");
					transformer_configuration();
					return;
				}
				for (int i = 0; i < 测试设置窗口.cmbTransformerSelect.Items.Count; i++) {
					if (测试设置窗口.cmbTransformerSelect.Items[i].ToString() == test_parameter._1curTransformerName) {
						测试设置窗口.cmbTransformerSelect.SelectedIndex = i;
					}
				}
				get_Test_information(test_parameter._1curTransformerName);
				#endregion
			}
			else {
				MessageBox.Show("请先配置变压器信息!");
				transformer_configuration();
				return;
			}
			#endregion
			#region 每次 根据测试采样率设置数据buffer大小
			if (测试设置窗口.cmbDeviceACSampleFrequency.SelectedItem != null) {
				test_parameter._4SampleFrequency = 测试设置窗口.cmbDeviceACSampleFrequency.SelectedItem.ToString();
				int 采样率 = int.Parse(test_parameter._4SampleFrequency);
				if (采样率 == 100) {
					采样率 = 50;
				}
				if (采样率 == 500) {
					采样率 = 200;
				}
				Page_Max_count = (int)(采样率 * 3);
				if (test_parameter._25IsDCMeasurment) {
					Page_Max_count = 60;
				}
			}
			#endregion
			测试设置窗口.rbAutoContinuousMeasurment.Checked += (RoutedEventHandler)delegate {
				测试设置窗口.tbContinuousTestCurTap.Text = transformer_parameter._24SwitchStartWorkingPosition;
				测试设置窗口.tbContinuousTestEndTap.Text = transformer_parameter._25SwitchStopWorkingPosition;
			};
			test_parameter._0CompanyName = transformer_parameter._1ItsUnitName;
			测试设置窗口.btnDSP.Click += (RoutedEventHandler)delegate {
				byte[] data = new byte[8];
				byte[] temp;
				temp = BitConverter.GetBytes(0x8000000c);
				int index = 0;
				temp.CopyTo(data, index);
				index += temp.Length;
				temp = BitConverter.GetBytes(int.Parse(测试设置窗口.tbDSP.Text));
				temp.CopyTo(data, index);
				CMD_Send(data);
			};
			测试设置窗口.Background = Brushes.AliceBlue;
			测试设置窗口.Activate();
			测试设置窗口.Show();
		}
		#endregion
		void 默认参数() {
			test_parameter._17SampleFrequency_DC = "20";
			test_parameter._26MutationRation_DC = "5";
			test_parameter._28ErrorRation_DC = "5";
			test_parameter._30MinChangeTime_DC = "0.1";
			test_parameter._32MaxConstantTime_DC = "1";
			test_parameter._34IgnoreTime_DC = "80";

			test_parameter._27MutationRation_AC = "5";
			test_parameter._29ErrorRation_AC = "5";
			test_parameter._31MinChangeTime_AC = "0.2";
			test_parameter._33MaxConstantTime_AC = "22.0";
			test_parameter._35IgnoreTime_AC = "80";
		//	test_parameter._2OutputVolt = "200";
			test_parameter._5AutoContinuousMeasurementCurTap = transformer_parameter._26SwitchMidPosition;
			test_parameter._7SinglePointMeasurementCurTap = transformer_parameter._26SwitchMidPosition;
			test_parameter._6AutoContinuousMeasurementEndTap = transformer_parameter._25SwitchStopWorkingPosition;
			test_parameter._14isAutoContinuousMearsurment = false;
			test_parameter._15isHandleSingleMearsurment = true;
			test_parameter._18isAutoContinuousMearsurment_DC = false;
			test_parameter._19isHandleSingleMearsurment_DC = true;
			test_parameter._20EnableDCfilter_DC = true;
			test_parameter._21DisableDCfilter_DC = false;
			test_parameter._22IsInnernalPower = true;
			test_parameter._23IsExternalPower = false;
			test_parameter._21DisableDCfilter_DC = false;
			test_parameter._36IsAutoAnalysisParameterSet_AC = true;
			test_parameter._37IsAutoAnalysisParameterSet_DC = true;
			test_parameter._38IsHandleAnalysisParameterSet_AC = false;
			test_parameter._39IsHandleAnalysisParameterSet_DC = false;
			if ((bool)测试设置窗口.rbBackSwitch.IsChecked) {
				test_parameter._9SinglePointMeasurementBackSwitch = true;
				test_parameter._8SinglePointMeasurementForwardSwitch = false;
			}
			if ((bool)测试设置窗口.rbForwardSwitch.IsChecked) {
				test_parameter._9SinglePointMeasurementBackSwitch = false;
				test_parameter._8SinglePointMeasurementForwardSwitch = true;
			}
			测试设置窗口.tbContinuousTestCurTap.Text = test_parameter._5AutoContinuousMeasurementCurTap;
			测试设置窗口.tbContinuousTestEndTap.Text = test_parameter._6AutoContinuousMeasurementEndTap;
			测试设置窗口.tbErrorRatioAuto_AC.Text = test_parameter._29ErrorRation_AC;
			测试设置窗口.tbErrorRatioAuto_DC.Text = test_parameter._28ErrorRation_DC;
			测试设置窗口.tbErrorRatioManual_AC.Text = test_parameter._29ErrorRation_AC;
			测试设置窗口.tbErrorRatioManual_DC.Text = test_parameter._28ErrorRation_DC;
			测试设置窗口.tbIgnoreTimeSpan_AC.Text = test_parameter._35IgnoreTime_AC;
			测试设置窗口.tbMaxConstantTime_AC.Text = test_parameter._33MaxConstantTime_AC;
			测试设置窗口.tbMinChangeTime_AC.Text = test_parameter._31MinChangeTime_AC;
			测试设置窗口.tbMinChangeTime_DC.Text = test_parameter._30MinChangeTime_DC;
			测试设置窗口.tbMinConstantTime_DC.Text = test_parameter._32MaxConstantTime_DC;
			测试设置窗口.tbMutationRatioAuto_AC.Text = test_parameter._27MutationRation_AC;
			测试设置窗口.tbMutationRatioAuto_DC.Text = test_parameter._26MutationRation_DC;
			测试设置窗口.tbMutationRatioManual_AC.Text = test_parameter._27MutationRation_AC;
			测试设置窗口.tbMutationRatioManual_DC.Text = test_parameter._26MutationRation_DC;
			//采样率
			for (int i = 0; i < 测试设置窗口.cmbDeviceACSampleFrequency.Items.Count; i++) {
				if (测试设置窗口.cmbDeviceACSampleFrequency.Items[i].ToString() == test_parameter._4SampleFrequency) {
					测试设置窗口.cmbDeviceACSampleFrequency.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbDeviceACSampleFrequency.Items.Count - 1) {
					测试设置窗口.cmbDeviceACSampleFrequency.SelectedIndex = 0;
				}
			}
			//分接位
			for (int i = 0; i < 测试设置窗口.cmbOneCurTap.Items.Count; i++) {
				if (测试设置窗口.cmbOneCurTap.Items[i].ToString() == test_parameter._7SinglePointMeasurementCurTap) {
					测试设置窗口.cmbOneCurTap.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbOneCurTap.Items.Count - 1) {
					测试设置窗口.cmbOneCurTap.SelectedIndex = 0;
				}
			}
			// 交流输出电压
			for (int i = 0; i < 测试设置窗口.cmbInnerSupplyVoltage.Items.Count; i++) {
				if (测试设置窗口.cmbInnerSupplyVoltage.Items[i].ToString() == test_parameter._2OutputVolt) {
					测试设置窗口.cmbInnerSupplyVoltage.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbInnerSupplyVoltage.Items.Count - 1) {
					测试设置窗口.cmbInnerSupplyVoltage.SelectedIndex = 0;
				}
			}
			//测试档位
			for (int i = 0; i < 测试设置窗口.cmbTestGear.Items.Count; i++) {
				if (测试设置窗口.cmbTestGear.Items[i].ToString() == test_parameter._16MeasureGear_DC) {
					测试设置窗口.cmbTestGear.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbTestGear.Items.Count - 1) {
					测试设置窗口.cmbTestGear.SelectedIndex = 0;
				}
			}
			测试设置窗口.rbAutoAnalysisParameterSet_AC.IsChecked = test_parameter._36IsAutoAnalysisParameterSet_AC;
			测试设置窗口.rbAutoAnalysisParameterSet_DC.IsChecked = test_parameter._37IsAutoAnalysisParameterSet_DC;
			测试设置窗口.rbAutoContinuousMeasurment.IsChecked = test_parameter._14isAutoContinuousMearsurment;
			测试设置窗口.rbSinglePiontMeasurment.IsChecked = test_parameter._15isHandleSingleMearsurment;
			测试设置窗口.rbInnernalPower.IsChecked = test_parameter._22IsInnernalPower;
			测试设置窗口.rbExternalPower.IsChecked = test_parameter._23IsExternalPower;
			测试设置窗口.rbForwardSwitch.IsChecked = test_parameter._8SinglePointMeasurementForwardSwitch;
			测试设置窗口.rbBackSwitch.IsChecked = test_parameter._9SinglePointMeasurementBackSwitch;
			测试设置窗口.rbDisableDCfilter.IsChecked = test_parameter._21DisableDCfilter_DC;
			测试设置窗口.rbEnableDCfilter.IsChecked = test_parameter._20EnableDCfilter_DC;
			测试设置窗口.rbHandleAnalysisParameterSet_AC.IsChecked = test_parameter._38IsHandleAnalysisParameterSet_AC;
			测试设置窗口.rbHandleAnalysisParameterSet_DC.IsChecked = test_parameter._39IsHandleAnalysisParameterSet_DC;
			测试设置窗口.rbEnableDCfilter.IsChecked = test_parameter._20EnableDCfilter_DC;
		}
		#region 从数据库加载 已存在信息
		//根据 当前变压器TreeView 选择 匹配配置信息
		void get_access_info(OleDbHelper.File_Name_e transformer_file_name, string transformer_value, OleDbHelper.Test_File_Name_e test_file_name, string test_value) {
			try {
				DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)transformer_parameter._1ItsUnitName);
				foreach (DataRow row in ds.Tables[0].Rows) {
					if (row[2].Equals(test_parameter._1curTransformerName)) {
						transformer_parameter._1ItsUnitName = row[(int)OleDbHelper.File_Name_e._1ItsUnitName + 1].ToString();
						transformer_parameter._3TransformerModel = row[(int)OleDbHelper.File_Name_e._3TransFormModel + 1].ToString();

						transformer_parameter._6Transformerphase = row[(int)OleDbHelper.File_Name_e._6Transformerphase + 1].ToString();
						switch (transformer_parameter._6Transformerphase) {
							case "单相":
								transformer_parameter._4Single_phase = true;
								transformer_parameter._5Thrid_phase = false;
								变压器属性设置窗口.cb1P.IsChecked = true;
								变压器属性设置窗口.cb3P.IsChecked = false;
								break;
							case "三相":
								transformer_parameter._5Thrid_phase = true;
								transformer_parameter._4Single_phase = false;
								变压器属性设置窗口.cb3P.IsChecked = true;
								变压器属性设置窗口.cb1P.IsChecked = false;
								break;
							default:
								transformer_parameter._5Thrid_phase = false;
								transformer_parameter._5Thrid_phase = false;
								break;
						}
						transformer_parameter._9TransformerWinding = row[(int)OleDbHelper.File_Name_e._9TransformerWinding + 1].ToString();
						switch (transformer_parameter._9TransformerWinding) {
							case "双绕组":
								transformer_parameter._7Double_Winding = true;
								变压器属性设置窗口.cb2RZ.IsChecked = true;
								transformer_parameter._8Three_Winding = false;
								变压器属性设置窗口.cb3RZ.IsChecked = false;
								break;
							case "三绕组":
								transformer_parameter._8Three_Winding = true;
								变压器属性设置窗口.cb3RZ.IsChecked = true;
								transformer_parameter._7Double_Winding = false;
								变压器属性设置窗口.cb2RZ.IsChecked = false;
								break;
							default:
								transformer_parameter._8Three_Winding = false;
								transformer_parameter._7Double_Winding = false;
								break;
						}
						transformer_parameter._13TransformerWindingConnMethod = row[(int)OleDbHelper.File_Name_e._13TransformerWindingConnMethod + 1].ToString();
						switch (transformer_parameter._13TransformerWindingConnMethod) {
							case "Y型接法": transformer_parameter._10Y_method = true; break;
							case "YN型接法": transformer_parameter._11YN_method = true; break;
							case "三角形接法": transformer_parameter._12Triangle_method = true; break;

							default: break;
						}
						transformer_parameter._20SwitchManufactorName = row[(int)OleDbHelper.File_Name_e._20SwitchManufactorName + 1].ToString();
						transformer_parameter._21SwitchModel = row[(int)OleDbHelper.File_Name_e._21SwitchModel + 1].ToString();
						transformer_parameter._22SwitchCode = row[(int)OleDbHelper.File_Name_e._22SwitchCode + 1].ToString();
						transformer_parameter._23SwitchColumnCount = row[(int)OleDbHelper.File_Name_e._23SwitchColumnCount + 1].ToString();
						switch (transformer_parameter._23SwitchColumnCount) {
							case "单列": transformer_parameter._27SwitchColumn_One_Count = true; break;
							case "双列": transformer_parameter._28SwitchColumn_Two_Count = true; break;
							case "三列": transformer_parameter._29SwitchColumn_Three_Count = true; break;

							default: break;
						}
						transformer_parameter._24SwitchStartWorkingPosition = row[(int)OleDbHelper.File_Name_e._24SwitchStartWorkingPosition + 1].ToString();
						transformer_parameter._25SwitchStopWorkingPosition = row[(int)OleDbHelper.File_Name_e._25SwitchStopWorkingPosition + 1].ToString();
						transformer_parameter._26SwitchMidPosition = row[(int)OleDbHelper.File_Name_e._26SwitchMidPosition + 1].ToString();
						BindingTransformer_Info();
						break;
					}
				}



				DataSet ds1 = OleDbHelper.Select(OleDbHelper.Test_File_Name_e._0curTransformerName, (object)test_value);

				foreach (DataRow row in ds1.Tables[0].Rows) {
					if (row[9].Equals(test_parameter._48Access_position)) {
						test_parameter._2OutputVolt = row[(int)OleDbHelper.Test_File_Name_e._1OutputVolt].ToString();
						test_parameter._3OutputVoltFrequency = row[(int)OleDbHelper.Test_File_Name_e._2OutputVoltFrequency].ToString();
						test_parameter._4SampleFrequency = row[(int)OleDbHelper.Test_File_Name_e._3SampleFrequency].ToString();

						test_parameter._5AutoContinuousMeasurementCurTap = row[(int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap].ToString();

						test_parameter._6AutoContinuousMeasurementEndTap = row[(int)OleDbHelper.Test_File_Name_e._5AutoContinuousMeasurementEndTap].ToString();

						test_parameter._7SinglePointMeasurementCurTap = row[(int)OleDbHelper.Test_File_Name_e._6SinglePointMeasurementCurTap].ToString();

						test_parameter._8SinglePointMeasurementForwardSwitch = (bool)row[(int)OleDbHelper.Test_File_Name_e._7SinglePointMeasurementForwardSwitch];

						test_parameter._9SinglePointMeasurementBackSwitch = (bool)row[(int)OleDbHelper.Test_File_Name_e._8SinglePointMeasurementBackSwitch];
						test_parameter._14isAutoContinuousMearsurment = (bool)row[(int)OleDbHelper.Test_File_Name_e._14isAutoContinuousMearsurment];
						test_parameter._15isHandleSingleMearsurment = (bool)row[(int)OleDbHelper.Test_File_Name_e._15isHandleSingleMearsurment];
						test_parameter._20EnableDCfilter_DC = (bool)row[(int)OleDbHelper.Test_File_Name_e._20EnableDCfilter_DC];
						test_parameter._21DisableDCfilter_DC = (bool)row[(int)OleDbHelper.Test_File_Name_e._21DisableDCfilter_DC];
						test_parameter._22IsInnernalPower = (bool)row[(int)OleDbHelper.Test_File_Name_e._22IsInnernalPower];
						test_parameter._23IsExternalPower = (bool)row[(int)OleDbHelper.Test_File_Name_e._23IsExternalPower];
						test_parameter._26MutationRation_DC = row[(int)OleDbHelper.Test_File_Name_e._26MutationRation_DC].ToString();
						test_parameter._27MutationRation_AC = row[(int)OleDbHelper.Test_File_Name_e._27MutationRation_AC].ToString();
						test_parameter._28ErrorRation_DC = row[(int)OleDbHelper.Test_File_Name_e._28ErrorRation_DC].ToString();
						test_parameter._29ErrorRation_AC = row[(int)OleDbHelper.Test_File_Name_e._29ErrorRation_AC].ToString();
						test_parameter._30MinChangeTime_DC = row[(int)OleDbHelper.Test_File_Name_e._30MinChangeTime_DC].ToString();
						test_parameter._31MinChangeTime_AC = row[(int)OleDbHelper.Test_File_Name_e._31MinChangeTime_AC].ToString();
						test_parameter._32MaxConstantTime_DC = row[(int)OleDbHelper.Test_File_Name_e._32MaxConstantTime_DC].ToString();
						test_parameter._33MaxConstantTime_AC = row[(int)OleDbHelper.Test_File_Name_e._33MaxConstantTime_AC].ToString();
						test_parameter._34IgnoreTime_DC = row[(int)OleDbHelper.Test_File_Name_e._34IgnoreTime_DC].ToString();
						test_parameter._35IgnoreTime_AC = row[(int)OleDbHelper.Test_File_Name_e._35IgnoreTime_AC].ToString();
						test_parameter._36IsAutoAnalysisParameterSet_AC = (bool)row[(int)OleDbHelper.Test_File_Name_e._36IsAutoAnalysisParameterSet_AC];
						test_parameter._38IsHandleAnalysisParameterSet_AC = (bool)row[(int)OleDbHelper.Test_File_Name_e._38IsHandleAnalysisParameterSet_AC];
						test_parameter.Measurment = row[(int)OleDbHelper.Test_File_Name_e.Measurment].ToString();
						test_parameter._40Cursor_A1 = row[(int)OleDbHelper.Test_File_Name_e._40Cursor_A1].ToString();
						test_parameter._41Cursor_A2 = row[(int)OleDbHelper.Test_File_Name_e._41Cursor_A2].ToString();
						test_parameter._42Cursor_B1 = row[(int)OleDbHelper.Test_File_Name_e._42Cursor_B1].ToString();
						test_parameter._43Cursor_B2 = row[(int)OleDbHelper.Test_File_Name_e._43Cursor_B2].ToString();
						test_parameter._44Cursor_C1 = row[(int)OleDbHelper.Test_File_Name_e._44Cursor_C1].ToString();
						test_parameter._45Cursor_C2 = row[(int)OleDbHelper.Test_File_Name_e._45Cursor_C2].ToString();
						test_parameter._46Peak_value = row[(int)OleDbHelper.Test_File_Name_e._46Peak_value].ToString();
						test_parameter._47Test_Date = row[(int)OleDbHelper.Test_File_Name_e._47Test_Date].ToString();
						test_parameter._48Access_position = row[(int)OleDbHelper.Test_File_Name_e._48Access_position].ToString();
						test_parameter._49Mesurent_Counts = row[(int)OleDbHelper.Test_File_Name_e._49Mesurent_Counts].ToString();
						test_parameter._50Cursor_A1_DC = row[(int)OleDbHelper.Test_File_Name_e._50Cursor_A1_DC].ToString();
						test_parameter._51Cursor_A2_DC = row[(int)OleDbHelper.Test_File_Name_e._51Cursor_A2_DC].ToString();
						test_parameter._52Cursor_B1_DC = row[(int)OleDbHelper.Test_File_Name_e._52Cursor_B1_DC].ToString();
						test_parameter._53Cursor_B2_DC = row[(int)OleDbHelper.Test_File_Name_e._53Cursor_B2_DC].ToString();
						test_parameter._54Cursor_C1_DC = row[(int)OleDbHelper.Test_File_Name_e._54Cursor_C1_DC].ToString();
						test_parameter._55Cursor_C2_DC = row[(int)OleDbHelper.Test_File_Name_e._55Cursor_C2_DC].ToString();
						switch (test_parameter.Measurment) {
							case "直流测试": test_parameter._25IsDCMeasurment = true; break;
							case "交流测试": test_parameter._24IsACMeasurment = true; break;
							default: break;
						}
						BindingTest_Info();
					}
				}
			}
			catch {

			}
		}
		void get_access_info(string test_value) {
			if (test_value == null) {
				return;
			}
			DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)transformer_parameter._1ItsUnitName);

			foreach (DataRow row in ds.Tables[0].Rows) {
				if (row[2].Equals(test_value)) {
					transformer_parameter._1ItsUnitName = row[(int)OleDbHelper.File_Name_e._1ItsUnitName + 1].ToString();
					transformer_parameter._3TransformerModel = row[(int)OleDbHelper.File_Name_e._3TransFormModel + 1].ToString();
					transformer_parameter._6Transformerphase = row[(int)OleDbHelper.File_Name_e._6Transformerphase + 1].ToString();
					switch (transformer_parameter._6Transformerphase) {
						case "单相":
							transformer_parameter._4Single_phase = true;
							transformer_parameter._5Thrid_phase = false;
							变压器属性设置窗口.cb1P.IsChecked = true;
							变压器属性设置窗口.cb3P.IsChecked = false;
							break;
						case "三相":
							transformer_parameter._5Thrid_phase = true;
							transformer_parameter._4Single_phase = false;
							变压器属性设置窗口.cb3P.IsChecked = true;
							变压器属性设置窗口.cb1P.IsChecked = false;
							break;
						default:
							transformer_parameter._5Thrid_phase = false;
							transformer_parameter._5Thrid_phase = false;
							break;
					}
					transformer_parameter._9TransformerWinding = row[(int)OleDbHelper.File_Name_e._9TransformerWinding + 1].ToString();
					switch (transformer_parameter._9TransformerWinding) {
						case "双绕组":
							transformer_parameter._7Double_Winding = true;
							变压器属性设置窗口.cb2RZ.IsChecked = true;
							transformer_parameter._8Three_Winding = false;
							变压器属性设置窗口.cb3RZ.IsChecked = false;
							break;
						case "三绕组":
							transformer_parameter._8Three_Winding = true;
							变压器属性设置窗口.cb3RZ.IsChecked = true;
							transformer_parameter._7Double_Winding = false;
							变压器属性设置窗口.cb2RZ.IsChecked = false;
							break;
						default:
							transformer_parameter._8Three_Winding = false;
							transformer_parameter._7Double_Winding = false;
							break;
					}
					transformer_parameter._13TransformerWindingConnMethod = row[(int)OleDbHelper.File_Name_e._13TransformerWindingConnMethod + 1].ToString();
					transformer_parameter._10Y_method = false;
					transformer_parameter._11YN_method = false;
					transformer_parameter._12Triangle_method = false;
					变压器属性设置窗口.cbTangle.IsChecked = false;
					变压器属性设置窗口.cbYO.IsChecked = false;
					变压器属性设置窗口.cbY.IsChecked = false;
					switch (transformer_parameter._13TransformerWindingConnMethod) {
						case "Y型接法": transformer_parameter._10Y_method = true; 变压器属性设置窗口.cbY.IsChecked = true; break;
						case "YN型接法": transformer_parameter._11YN_method = true; 变压器属性设置窗口.cbYO.IsChecked = true; break;
						case "三角形接法": transformer_parameter._12Triangle_method = true; 变压器属性设置窗口.cbTangle.IsChecked = true; break;

						default: break;
					}
					transformer_parameter._20SwitchManufactorName = row[(int)OleDbHelper.File_Name_e._20SwitchManufactorName + 1].ToString();
					transformer_parameter._21SwitchModel = row[(int)OleDbHelper.File_Name_e._21SwitchModel + 1].ToString();
					transformer_parameter._22SwitchCode = row[(int)OleDbHelper.File_Name_e._22SwitchCode + 1].ToString();
					transformer_parameter._23SwitchColumnCount = row[(int)OleDbHelper.File_Name_e._23SwitchColumnCount + 1].ToString();
					switch (transformer_parameter._23SwitchColumnCount) {
						case "单列": transformer_parameter._27SwitchColumn_One_Count = true; 变压器属性设置窗口.cbOne.IsChecked = true; break;
						case "双列": transformer_parameter._28SwitchColumn_Two_Count = true; 变压器属性设置窗口.cbTwo.IsChecked = true; break;
						case "三列": transformer_parameter._29SwitchColumn_Three_Count = true; 变压器属性设置窗口.cbThrid.IsChecked = true; break;

						default: break;
					}
					transformer_parameter._24SwitchStartWorkingPosition = row[(int)OleDbHelper.File_Name_e._24SwitchStartWorkingPosition + 1].ToString();
					transformer_parameter._25SwitchStopWorkingPosition = row[(int)OleDbHelper.File_Name_e._25SwitchStopWorkingPosition + 1].ToString();
					transformer_parameter._26SwitchMidPosition = row[(int)OleDbHelper.File_Name_e._26SwitchMidPosition + 1].ToString();
					break;
				}
			}
			BindingTransformer_Info();
		}
		void get_access_info(string CompanyName, string TransFormerName) {
			if (TransFormerName == null) {
				return;
			}
			DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)CompanyName);

			foreach (DataRow row in ds.Tables[0].Rows) {
				if (row[2].Equals(TransFormerName)) {
					transformer_parameter._1ItsUnitName = row[(int)OleDbHelper.File_Name_e._1ItsUnitName + 1].ToString();
					transformer_parameter._3TransformerModel = row[(int)OleDbHelper.File_Name_e._3TransFormModel + 1].ToString();
					transformer_parameter._6Transformerphase = row[(int)OleDbHelper.File_Name_e._6Transformerphase + 1].ToString();
					switch (transformer_parameter._6Transformerphase) {
						case "单相":
							transformer_parameter._4Single_phase = true;
							transformer_parameter._5Thrid_phase = false;
							变压器属性设置窗口.cb1P.IsChecked = true;
							变压器属性设置窗口.cb3P.IsChecked = false;
							break;
						case "三相":
							transformer_parameter._5Thrid_phase = true;
							transformer_parameter._4Single_phase = false;
							变压器属性设置窗口.cb3P.IsChecked = true;
							变压器属性设置窗口.cb1P.IsChecked = false;
							break;
						default:
							transformer_parameter._5Thrid_phase = false;
							transformer_parameter._5Thrid_phase = false;
							break;
					}
					transformer_parameter._9TransformerWinding = row[(int)OleDbHelper.File_Name_e._9TransformerWinding + 1].ToString();
					switch (transformer_parameter._9TransformerWinding) {
						case "双绕组":
							transformer_parameter._7Double_Winding = true;
							变压器属性设置窗口.cb2RZ.IsChecked = true;
							transformer_parameter._8Three_Winding = false;
							变压器属性设置窗口.cb3RZ.IsChecked = false;
							break;
						case "三绕组":
							transformer_parameter._8Three_Winding = true;
							变压器属性设置窗口.cb3RZ.IsChecked = true;
							transformer_parameter._7Double_Winding = false;
							变压器属性设置窗口.cb2RZ.IsChecked = false;
							break;
						default:
							transformer_parameter._8Three_Winding = false;
							transformer_parameter._7Double_Winding = false;
							break;
					}
					transformer_parameter._13TransformerWindingConnMethod = row[(int)OleDbHelper.File_Name_e._13TransformerWindingConnMethod + 1].ToString();
					switch (transformer_parameter._13TransformerWindingConnMethod) {
						case "Y型接法": transformer_parameter._10Y_method = true; 变压器属性设置窗口.cbY.IsChecked = true; break;
						case "YN型接法": transformer_parameter._11YN_method = true; 变压器属性设置窗口.cbYO.IsChecked = true; break;
						case "三角形接法": transformer_parameter._12Triangle_method = true; 变压器属性设置窗口.cbTangle.IsChecked = true; break;

						default: break;
					}
					transformer_parameter._20SwitchManufactorName = row[(int)OleDbHelper.File_Name_e._20SwitchManufactorName + 1].ToString();
					transformer_parameter._21SwitchModel = row[(int)OleDbHelper.File_Name_e._21SwitchModel + 1].ToString();
					transformer_parameter._22SwitchCode = row[(int)OleDbHelper.File_Name_e._22SwitchCode + 1].ToString();
					transformer_parameter._23SwitchColumnCount = row[(int)OleDbHelper.File_Name_e._23SwitchColumnCount + 1].ToString();
					switch (transformer_parameter._23SwitchColumnCount) {
						case "单列": transformer_parameter._27SwitchColumn_One_Count = true; 变压器属性设置窗口.cbOne.IsChecked = true; break;
						case "双列": transformer_parameter._28SwitchColumn_Two_Count = true; 变压器属性设置窗口.cbTwo.IsChecked = true; break;
						case "三列": transformer_parameter._29SwitchColumn_Three_Count = true; 变压器属性设置窗口.cbThrid.IsChecked = true; break;

						default: break;
					}
					transformer_parameter._24SwitchStartWorkingPosition = row[(int)OleDbHelper.File_Name_e._24SwitchStartWorkingPosition + 1].ToString();
					transformer_parameter._25SwitchStopWorkingPosition = row[(int)OleDbHelper.File_Name_e._25SwitchStopWorkingPosition + 1].ToString();
					transformer_parameter._26SwitchMidPosition = row[(int)OleDbHelper.File_Name_e._26SwitchMidPosition + 1].ToString();
					break;
				}
			}
			BindingTransformer_Info();
		}
		// 开机默认 加载最新配置信息
		void get_access_info() {
			if (变压器属性设置窗口 == null) {
				变压器属性设置窗口 = new TransForm_SettingWindow(TreeViewTestItem, TestStage_Update);
			}
			DataSet ds = OleDbHelper.Select(OleDbHelper.TransFormer_Table_Name);
			try {
				DataRow row = ds.Tables[0].Rows[ds.Tables[0].Rows.Count - 1];
				transformer_parameter._1ItsUnitName = row[(int)OleDbHelper.File_Name_e._1ItsUnitName + 1].ToString();
				transformer_parameter._2TransformerName = row[(int)OleDbHelper.File_Name_e._2TransFormName + 1].ToString();
				transformer_parameter._3TransformerModel = row[(int)OleDbHelper.File_Name_e._3TransFormModel + 1].ToString();
				transformer_parameter._6Transformerphase = row[(int)OleDbHelper.File_Name_e._6Transformerphase + 1].ToString();
				switch (transformer_parameter._6Transformerphase) {
					case "单相":
						transformer_parameter._4Single_phase = true;
						transformer_parameter._5Thrid_phase = false;
						变压器属性设置窗口.cb1P.IsChecked = true;
						变压器属性设置窗口.cb3P.IsChecked = false;
						break;
					case "三相":
						transformer_parameter._5Thrid_phase = true;
						transformer_parameter._4Single_phase = false;
						变压器属性设置窗口.cb3P.IsChecked = true;
						变压器属性设置窗口.cb1P.IsChecked = false;
						break;
					default:
						transformer_parameter._5Thrid_phase = false;
						transformer_parameter._5Thrid_phase = false;
						break;
				}
				transformer_parameter._9TransformerWinding = row[(int)OleDbHelper.File_Name_e._9TransformerWinding + 1].ToString();
				switch (transformer_parameter._9TransformerWinding) {
					case "双绕组":
						transformer_parameter._7Double_Winding = true;
						变压器属性设置窗口.cb2RZ.IsChecked = true;
						transformer_parameter._8Three_Winding = false;
						变压器属性设置窗口.cb3RZ.IsChecked = false;
						break;
					case "三绕组":
						transformer_parameter._8Three_Winding = true;
						变压器属性设置窗口.cb3RZ.IsChecked = true;
						transformer_parameter._7Double_Winding = false;
						变压器属性设置窗口.cb2RZ.IsChecked = false;
						break;
					default:
						transformer_parameter._8Three_Winding = false;
						transformer_parameter._7Double_Winding = false;
						break;
				}
				transformer_parameter._13TransformerWindingConnMethod = row[(int)OleDbHelper.File_Name_e._13TransformerWindingConnMethod + 1].ToString();
				switch (transformer_parameter._13TransformerWindingConnMethod) {
					case "Y型接法": transformer_parameter._10Y_method = true; 变压器属性设置窗口.cbY.IsChecked = true; break;
					case "YN型接法": transformer_parameter._11YN_method = true; 变压器属性设置窗口.cbYO.IsChecked = true; break;
					case "三角形接法": transformer_parameter._12Triangle_method = true; 变压器属性设置窗口.cbTangle.IsChecked = true; break;

					default: break;
				}
				transformer_parameter._20SwitchManufactorName = row[(int)OleDbHelper.File_Name_e._20SwitchManufactorName + 1].ToString();
				transformer_parameter._21SwitchModel = row[(int)OleDbHelper.File_Name_e._21SwitchModel + 1].ToString();
				transformer_parameter._22SwitchCode = row[(int)OleDbHelper.File_Name_e._22SwitchCode + 1].ToString();
				transformer_parameter._23SwitchColumnCount = row[(int)OleDbHelper.File_Name_e._23SwitchColumnCount + 1].ToString();
				switch (transformer_parameter._23SwitchColumnCount) {
					case "单列": transformer_parameter._27SwitchColumn_One_Count = true; 变压器属性设置窗口.cbOne.IsChecked = true; break;
					case "双列": transformer_parameter._28SwitchColumn_Two_Count = true; 变压器属性设置窗口.cbTwo.IsChecked = true; break;
					case "三列": transformer_parameter._29SwitchColumn_Three_Count = true; 变压器属性设置窗口.cbThrid.IsChecked = true; break;

					default: break;
				}
				transformer_parameter._24SwitchStartWorkingPosition = row[(int)OleDbHelper.File_Name_e._24SwitchStartWorkingPosition + 1].ToString();
				transformer_parameter._25SwitchStopWorkingPosition = row[(int)OleDbHelper.File_Name_e._25SwitchStopWorkingPosition + 1].ToString();
				transformer_parameter._26SwitchMidPosition = row[(int)OleDbHelper.File_Name_e._26SwitchMidPosition + 1].ToString();
				BindingTransformer_Info();
			}
			catch  {
				tbErrorExpection.Text = "配置信息消失了!\r\n请添加变压器!";
				//MessageBox.Show("没有找到可加载变压器配置信息:\r\n请添加变压器!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
		void get_Test_information(string transformer_name) {
			//根据 变压器配置信息加载 测试信息
			DataSet ds1 = OleDbHelper.Select(OleDbHelper.Test_Table_Name);
			DataRow row1 = null;
			foreach (DataRow 最新测试数据行 in ds1.Tables[0].Rows) {
				if (最新测试数据行[(int)OleDbHelper.Test_File_Name_e._0curTransformerName].ToString() == transformer_name && 最新测试数据行[(int)OleDbHelper.Test_File_Name_e._00CompanyName].ToString() == transformer_parameter._1ItsUnitName) {
					row1 = 最新测试数据行;
				}
			}
			//有匹配到数据
			if (row1 != null) {
				test_parameter._0CompanyName = row1[(int)OleDbHelper.Test_File_Name_e._00CompanyName].ToString();
				test_parameter._2OutputVolt = row1[(int)OleDbHelper.Test_File_Name_e._1OutputVolt].ToString();
				test_parameter._3OutputVoltFrequency = row1[(int)OleDbHelper.Test_File_Name_e._2OutputVoltFrequency].ToString();
				test_parameter._4SampleFrequency = row1[(int)OleDbHelper.Test_File_Name_e._3SampleFrequency].ToString();
				test_parameter._14isAutoContinuousMearsurment = (bool)row1[(int)OleDbHelper.Test_File_Name_e._14isAutoContinuousMearsurment];
				test_parameter._15isHandleSingleMearsurment = (bool)row1[(int)OleDbHelper.Test_File_Name_e._15isHandleSingleMearsurment];
				test_parameter._5AutoContinuousMeasurementCurTap = row1[(int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap].ToString() == "NULL" ? transformer_parameter._26SwitchMidPosition : row1[(int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap].ToString();
				test_parameter._6AutoContinuousMeasurementEndTap = row1[(int)OleDbHelper.Test_File_Name_e._5AutoContinuousMeasurementEndTap].ToString() == "NULL" ? (int.Parse(transformer_parameter._26SwitchMidPosition) + 10).ToString() : row1[(int)OleDbHelper.Test_File_Name_e._5AutoContinuousMeasurementEndTap].ToString();

				test_parameter._7SinglePointMeasurementCurTap = row1[(int)OleDbHelper.Test_File_Name_e._6SinglePointMeasurementCurTap].ToString() == "NULL" ? transformer_parameter._26SwitchMidPosition : row1[(int)OleDbHelper.Test_File_Name_e._6SinglePointMeasurementCurTap].ToString();

				test_parameter._8SinglePointMeasurementForwardSwitch = (bool)row1[(int)OleDbHelper.Test_File_Name_e._7SinglePointMeasurementForwardSwitch];

				test_parameter._9SinglePointMeasurementBackSwitch = (bool)row1[(int)OleDbHelper.Test_File_Name_e._8SinglePointMeasurementBackSwitch];
				test_parameter._20EnableDCfilter_DC = (bool)row1[(int)OleDbHelper.Test_File_Name_e._20EnableDCfilter_DC];
				test_parameter._21DisableDCfilter_DC = (bool)row1[(int)OleDbHelper.Test_File_Name_e._21DisableDCfilter_DC];
				test_parameter._22IsInnernalPower = (bool)row1[(int)OleDbHelper.Test_File_Name_e._22IsInnernalPower];
				test_parameter._23IsExternalPower = (bool)row1[(int)OleDbHelper.Test_File_Name_e._23IsExternalPower];
				//直流突变 默认10
				test_parameter._26MutationRation_DC = row1[(int)OleDbHelper.Test_File_Name_e._26MutationRation_DC].ToString() == null ? "10" : row1[(int)OleDbHelper.Test_File_Name_e._26MutationRation_DC].ToString();
				//交流突变 默认 10
				test_parameter._27MutationRation_AC = row1[(int)OleDbHelper.Test_File_Name_e._27MutationRation_AC].ToString() == null ? "10" : row1[(int)OleDbHelper.Test_File_Name_e._27MutationRation_AC].ToString();
				//直流误差 默认2
				test_parameter._28ErrorRation_DC = row1[(int)OleDbHelper.Test_File_Name_e._28ErrorRation_DC].ToString() == null ? "2" : row1[(int)OleDbHelper.Test_File_Name_e._28ErrorRation_DC].ToString();
				//交流误差 默认2
				test_parameter._29ErrorRation_AC = row1[(int)OleDbHelper.Test_File_Name_e._29ErrorRation_AC].ToString() == null ? "2" : row1[(int)OleDbHelper.Test_File_Name_e._29ErrorRation_AC].ToString();
				//直流最大变化时间 80
				test_parameter._30MinChangeTime_DC = row1[(int)OleDbHelper.Test_File_Name_e._30MinChangeTime_DC].ToString() == null ? "2" : row1[(int)OleDbHelper.Test_File_Name_e._30MinChangeTime_DC].ToString();
				//交流最小变化时间 默认 0.2
				test_parameter._31MinChangeTime_AC = row1[(int)OleDbHelper.Test_File_Name_e._31MinChangeTime_AC].ToString() == null ? "0.2" : row1[(int)OleDbHelper.Test_File_Name_e._31MinChangeTime_AC].ToString();
				//直流最大不变时间 默认 2
				test_parameter._32MaxConstantTime_DC = row1[(int)OleDbHelper.Test_File_Name_e._32MaxConstantTime_DC].ToString() == null ? "80" : row1[(int)OleDbHelper.Test_File_Name_e._32MaxConstantTime_DC].ToString();
				//交流最小持续不变时间 默认 1.5
				test_parameter._33MaxConstantTime_AC = row1[(int)OleDbHelper.Test_File_Name_e._33MaxConstantTime_AC].ToString() == null ? "1.5" : row1[(int)OleDbHelper.Test_File_Name_e._33MaxConstantTime_AC].ToString();
				test_parameter._34IgnoreTime_DC = row1[(int)OleDbHelper.Test_File_Name_e._34IgnoreTime_DC].ToString();
				test_parameter._35IgnoreTime_AC = row1[(int)OleDbHelper.Test_File_Name_e._35IgnoreTime_AC].ToString();
				test_parameter._36IsAutoAnalysisParameterSet_AC = (bool)row1[(int)OleDbHelper.Test_File_Name_e._36IsAutoAnalysisParameterSet_AC];
				test_parameter._38IsHandleAnalysisParameterSet_AC = (bool)row1[(int)OleDbHelper.Test_File_Name_e._38IsHandleAnalysisParameterSet_AC];
				test_parameter.Measurment = row1[(int)OleDbHelper.Test_File_Name_e.Measurment].ToString();
				test_parameter._40Cursor_A1 = row1[(int)OleDbHelper.Test_File_Name_e._40Cursor_A1].ToString();
				test_parameter._41Cursor_A2 = row1[(int)OleDbHelper.Test_File_Name_e._41Cursor_A2].ToString();
				test_parameter._42Cursor_B1 = row1[(int)OleDbHelper.Test_File_Name_e._42Cursor_B1].ToString();
				test_parameter._43Cursor_B2 = row1[(int)OleDbHelper.Test_File_Name_e._43Cursor_B2].ToString();
				test_parameter._44Cursor_C1 = row1[(int)OleDbHelper.Test_File_Name_e._44Cursor_C1].ToString();
				test_parameter._45Cursor_C2 = row1[(int)OleDbHelper.Test_File_Name_e._45Cursor_C2].ToString();
				test_parameter._46Peak_value = row1[(int)OleDbHelper.Test_File_Name_e._46Peak_value].ToString();
				test_parameter._47Test_Date = row1[(int)OleDbHelper.Test_File_Name_e._47Test_Date].ToString();
				test_parameter._48Access_position = row1[(int)OleDbHelper.Test_File_Name_e._48Access_position].ToString();
				test_parameter._49Mesurent_Counts = row1[(int)OleDbHelper.Test_File_Name_e._49Mesurent_Counts].ToString();
				test_parameter._50Cursor_A1_DC = row1[(int)OleDbHelper.Test_File_Name_e._50Cursor_A1_DC].ToString();
				test_parameter._51Cursor_A2_DC = row1[(int)OleDbHelper.Test_File_Name_e._51Cursor_A2_DC].ToString();
				test_parameter._52Cursor_B1_DC = row1[(int)OleDbHelper.Test_File_Name_e._52Cursor_B1_DC].ToString();
				test_parameter._53Cursor_B2_DC = row1[(int)OleDbHelper.Test_File_Name_e._53Cursor_B2_DC].ToString();
				test_parameter._54Cursor_C1_DC = row1[(int)OleDbHelper.Test_File_Name_e._54Cursor_C1_DC].ToString();
				test_parameter._55Cursor_C2_DC = row1[(int)OleDbHelper.Test_File_Name_e._55Cursor_C2_DC].ToString();

				switch (test_parameter.Measurment) {
					case "直流测试": test_parameter._25IsDCMeasurment = true; 测试设置窗口.rbDC.IsChecked = true; 测试设置窗口.tabDC.IsSelected = true; break;
					case "交流测试": test_parameter._24IsACMeasurment = true; 测试设置窗口.rbAC.IsChecked = true; 测试设置窗口.tabAC.IsSelected = true; break;
					default: break;
				}
				BindingTest_Info();
				return;
			}
			//没有匹配到数据
			else {
				return_default_Test_parameter();
				return;
			}
		}
		/// <summary>
		/// 恢复测试默认参数
		/// </summary>
		void return_default_Test_parameter() {
			默认参数();
			BindingTest_Info();
		}
		#endregion

		#region 手动数据绑定
		void BindingTransformer_Info() {
			if (变压器属性设置窗口 == null) {
				变压器属性设置窗口 = new TransForm_SettingWindow(TreeViewTestItem, TestStage_Update);
			}
			switch (transformer_parameter._6Transformerphase) {
				case "单相":
					transformer_parameter._4Single_phase = true;
					transformer_parameter._5Thrid_phase = false;
					变压器属性设置窗口.cb1P.IsChecked = true;
					变压器属性设置窗口.cb3P.IsChecked = false;
					break;
				case "三相":
					transformer_parameter._5Thrid_phase = true;
					transformer_parameter._4Single_phase = false;
					变压器属性设置窗口.cb3P.IsChecked = true;
					变压器属性设置窗口.cb1P.IsChecked = false;
					break;
				default:
					transformer_parameter._5Thrid_phase = false;
					transformer_parameter._5Thrid_phase = false;
					break;
			}
			switch (transformer_parameter._9TransformerWinding) {
				case "双绕组":
					transformer_parameter._7Double_Winding = true;
					变压器属性设置窗口.cb2RZ.IsChecked = true;
					transformer_parameter._8Three_Winding = false;
					变压器属性设置窗口.cb3RZ.IsChecked = false;
					break;
				case "三绕组":
					transformer_parameter._8Three_Winding = true;
					变压器属性设置窗口.cb3RZ.IsChecked = true;
					transformer_parameter._7Double_Winding = false;
					变压器属性设置窗口.cb2RZ.IsChecked = false;
					break;
				default:
					transformer_parameter._8Three_Winding = false;
					transformer_parameter._7Double_Winding = false;
					break;
			}
			switch (transformer_parameter._13TransformerWindingConnMethod) {
				case "Y型接法": transformer_parameter._10Y_method = true; 变压器属性设置窗口.cbY.IsChecked = true; break;
				case "YN型接法": transformer_parameter._11YN_method = true; 变压器属性设置窗口.cbYO.IsChecked = true; break;
				case "三角形接法": transformer_parameter._12Triangle_method = true; 变压器属性设置窗口.cbTangle.IsChecked = true; break;

				default: break;
			}
			switch (transformer_parameter._23SwitchColumnCount) {
				case "单列": transformer_parameter._27SwitchColumn_One_Count = true; 变压器属性设置窗口.cbOne.IsChecked = true; break;
				case "双列": transformer_parameter._28SwitchColumn_Two_Count = true; 变压器属性设置窗口.cbTwo.IsChecked = true; break;
				case "三列": transformer_parameter._29SwitchColumn_Three_Count = true; 变压器属性设置窗口.cbThrid.IsChecked = true; break;

				default: break;
			}
			变压器属性设置窗口.tbMidPosition.Text = transformer_parameter._26SwitchMidPosition;
			变压器属性设置窗口.tbStartWorkingPosition.Text = transformer_parameter._24SwitchStartWorkingPosition;
			变压器属性设置窗口.tbEndWorkingPosition.Text = transformer_parameter._25SwitchStopWorkingPosition;
			for (int i = 0; i < 变压器属性设置窗口.cmbSwitchModel.Items.Count; i++) {
				if (变压器属性设置窗口.cmbSwitchModel.Items[i].ToString() == transformer_parameter._21SwitchModel) {
					变压器属性设置窗口.cmbSwitchModel.SelectedIndex = i;
				}
			}
			for (int i = 0; i < 变压器属性设置窗口.cmbSwitchProducerName.Items.Count; i++) {
				if (变压器属性设置窗口.cmbSwitchProducerName.Items[i].ToString() == transformer_parameter._20SwitchManufactorName) {
					变压器属性设置窗口.cmbSwitchProducerName.SelectedIndex = i;
				}
			}
			变压器属性设置窗口.cmbSwitchModel.Text = transformer_parameter._21SwitchModel;
			变压器属性设置窗口.cmbSwitchProducerName.Text = transformer_parameter._20SwitchManufactorName;
			变压器属性设置窗口.tbSwitchProductionCode.Text = transformer_parameter._22SwitchCode;
			变压器属性设置窗口.tbTransFormModel.Text = transformer_parameter._3TransformerModel;
			变压器属性设置窗口.cmbExistCompany.Text = transformer_parameter._1ItsUnitName;
			变压器属性设置窗口.cmbExistTransFormers.Text = transformer_parameter._2TransformerName;
		}
		void BindingTest_Info() {
			if (测试设置窗口 == null) {
				测试设置窗口 = new TestSetupWindow(TestStage_Update);
				测试设置窗口.DataContext = test_parameter;
				#region 每次加载时更新 测试分接位 的总数 并 选中上一次测试位置
				测试设置窗口.cmbOneCurTap.Items.Clear();
				if (transformer_parameter._24SwitchStartWorkingPosition != null && transformer_parameter._25SwitchStopWorkingPosition != null) {
					for (int i = int.Parse(transformer_parameter._24SwitchStartWorkingPosition); i <= int.Parse(transformer_parameter._25SwitchStopWorkingPosition); i++) {
						测试设置窗口.cmbOneCurTap.Items.Add(i);
						测试设置窗口.cmbOneCurTap.SelectedItem = null;
					}
				}
				#endregion
			}

			#region 确定直流或者交流测试
			if ((bool)测试设置窗口.rbAC.IsChecked) {
				test_parameter._24IsACMeasurment = true;
				test_parameter._25IsDCMeasurment = false;
			}
			if ((bool)测试设置窗口.rbDC.IsChecked) {
				test_parameter._25IsDCMeasurment = true;
				test_parameter._24IsACMeasurment = false;
			}
			if ((bool)测试设置窗口.rbBackSwitch.IsChecked) {
				test_parameter._9SinglePointMeasurementBackSwitch = true;
				test_parameter._8SinglePointMeasurementForwardSwitch = false;
			}
			if ((bool)测试设置窗口.rbForwardSwitch.IsChecked) {
				test_parameter._9SinglePointMeasurementBackSwitch = false;
				test_parameter._8SinglePointMeasurementForwardSwitch = true;
			}
			#endregion
			测试设置窗口.cmbTestGear.SelectedItem = test_parameter._16MeasureGear_DC;
			测试设置窗口.tbContinuousTestCurTap.Text = test_parameter._5AutoContinuousMeasurementCurTap;
			测试设置窗口.tbContinuousTestEndTap.Text = test_parameter._6AutoContinuousMeasurementEndTap;
			测试设置窗口.tbErrorRatioAuto_AC.Text = test_parameter._29ErrorRation_AC;
			测试设置窗口.tbErrorRatioAuto_DC.Text = test_parameter._28ErrorRation_DC;
			测试设置窗口.tbErrorRatioManual_AC.Text = test_parameter._29ErrorRation_AC;
			测试设置窗口.tbErrorRatioManual_DC.Text = test_parameter._28ErrorRation_DC;
			测试设置窗口.tbIgnoreTimeSpan_AC.Text = test_parameter._35IgnoreTime_AC;
			测试设置窗口.tbMaxConstantTime_AC.Text = test_parameter._33MaxConstantTime_AC;
			测试设置窗口.tbMinChangeTime_AC.Text = test_parameter._31MinChangeTime_AC;
			测试设置窗口.tbMinChangeTime_DC.Text = test_parameter._30MinChangeTime_DC;
			测试设置窗口.tbMinConstantTime_DC.Text = test_parameter._32MaxConstantTime_DC;
			测试设置窗口.tbMutationRatioAuto_AC.Text = test_parameter._27MutationRation_AC;
			测试设置窗口.tbMutationRatioAuto_DC.Text = test_parameter._26MutationRation_DC;
			测试设置窗口.tbMutationRatioManual_AC.Text = test_parameter._27MutationRation_AC;
			测试设置窗口.tbMutationRatioManual_DC.Text = test_parameter._26MutationRation_DC;
			测试设置窗口.cmbCompanySelect.SelectedItem = test_parameter._0CompanyName;
			测试设置窗口.cmbTransformerSelect.SelectedItem = test_parameter._1curTransformerName;
			测试设置窗口.tbDeviceSampleFrequency_DC.Text = "20";
			//采样率
			for (int i = 0; i < 测试设置窗口.cmbDeviceACSampleFrequency.Items.Count; i++) {
				if (测试设置窗口.cmbDeviceACSampleFrequency.Items[i].ToString() == test_parameter._4SampleFrequency) {
					测试设置窗口.cmbDeviceACSampleFrequency.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbDeviceACSampleFrequency.Items.Count - 1) {
					测试设置窗口.cmbDeviceACSampleFrequency.SelectedIndex = 0;
				}
			}
			//分接位
			for (int i = 0; i < 测试设置窗口.cmbOneCurTap.Items.Count; i++) {
				if (测试设置窗口.cmbOneCurTap.Items[i].ToString() == test_parameter._7SinglePointMeasurementCurTap) {
					测试设置窗口.cmbOneCurTap.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbOneCurTap.Items.Count - 1) {
					测试设置窗口.cmbOneCurTap.SelectedIndex = 0;
				}
			}
			// 交流输出电压
			for (int i = 0; i < 测试设置窗口.cmbInnerSupplyVoltage.Items.Count; i++) {
				if (测试设置窗口.cmbInnerSupplyVoltage.Items[i].ToString() == test_parameter._2OutputVolt) {
					测试设置窗口.cmbInnerSupplyVoltage.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbInnerSupplyVoltage.Items.Count - 1) {
					测试设置窗口.cmbInnerSupplyVoltage.SelectedIndex = 0;
				}
			}
			//测试档位
			for (int i = 0; i < 测试设置窗口.cmbTestGear.Items.Count; i++) {
				if (测试设置窗口.cmbTestGear.Items[i].ToString() == test_parameter._16MeasureGear_DC) {
					测试设置窗口.cmbTestGear.SelectedIndex = i;
					break;
				}
				if (i == 测试设置窗口.cmbTestGear.Items.Count - 1) {
					测试设置窗口.cmbTestGear.SelectedIndex = 0;
				}
			}
			switch (test_parameter.Measurment) {
				case "直流测试":
					test_parameter._25IsDCMeasurment = true;
					测试设置窗口.rbAC.IsChecked = false;
					测试设置窗口.rbDC.IsChecked = true;
					测试设置窗口.tabDC.IsSelected = true; break;
				case "交流测试":
					test_parameter._24IsACMeasurment = true;
					测试设置窗口.rbAC.IsChecked = true;
					测试设置窗口.rbDC.IsChecked = false;
					测试设置窗口.tabAC.IsSelected = true; break;
				default: break;
			}
			测试设置窗口.rbAutoAnalysisParameterSet_AC.IsChecked = test_parameter._36IsAutoAnalysisParameterSet_AC;
			测试设置窗口.rbAutoAnalysisParameterSet_DC.IsChecked = test_parameter._37IsAutoAnalysisParameterSet_DC;
			测试设置窗口.rbAutoContinuousMeasurment.IsChecked = test_parameter._14isAutoContinuousMearsurment;
			测试设置窗口.rbSinglePiontMeasurment.IsChecked = test_parameter._15isHandleSingleMearsurment;
			测试设置窗口.rbInnernalPower.IsChecked = test_parameter._22IsInnernalPower;
			测试设置窗口.rbExternalPower.IsChecked = test_parameter._23IsExternalPower;
			测试设置窗口.rbForwardSwitch.IsChecked = test_parameter._8SinglePointMeasurementForwardSwitch;
			测试设置窗口.rbBackSwitch.IsChecked = test_parameter._9SinglePointMeasurementBackSwitch;
			测试设置窗口.rbDisableDCfilter.IsChecked = test_parameter._21DisableDCfilter_DC;
			测试设置窗口.rbEnableDCfilter.IsChecked = test_parameter._20EnableDCfilter_DC;
			测试设置窗口.rbHandleAnalysisParameterSet_AC.IsChecked = test_parameter._38IsHandleAnalysisParameterSet_AC;
			测试设置窗口.rbHandleAnalysisParameterSet_DC.IsChecked = test_parameter._39IsHandleAnalysisParameterSet_DC;
			测试设置窗口.rbEnableDCfilter.IsChecked = test_parameter._20EnableDCfilter_DC;
		}
		#endregion

		#region 测试信息更新

		int count_of_info_name = 11;
		class class_TestInfo {
			public string property_name { get; set; }
			public object property_value { get; set; }
		}
		class_TestInfo format_class_testinfo(string name, object value) {
			class_TestInfo temp = new class_TestInfo();
			temp.property_name = name;
			temp.property_value = value;
			return temp;
		}
		#endregion

		#region TreeView更新
		void TreeViewUpdate(int Level) {
			#region TreeView 更新
			bool isExist = false;
			bool isExist2 = false;
			bool isExist3 = false;
			bool isExist4 = false;
			//树根节点不存在时候 新建根节点
			if (TreeViewTestItem.Items.Count == 0) {
				//当变压器属性为空的时候 啥也不做 这属于错误操作！
				if (transformer_parameter._2TransformerName == "" || transformer_parameter._2TransformerName == null) {
					return;
				}
				//不为空时 第1层节点 为 变压器所属单位名称
				TreeViewIconsItem first_stage = new TreeViewIconsItem();
				first_stage.FontSize = 14;
				first_stage.HeaderText = transformer_parameter._1ItsUnitName;
				first_stage.TabIndex = 1;
				TreeViewTestItem.Items.Add(first_stage);
				//不为空时 第2层节点 为 变压器名称
				TreeViewIconsItem second_stage = new TreeViewIconsItem();
				second_stage.HeaderText = transformer_parameter._2TransformerName;
				second_stage.Icon = first_stage.iconSourceTransformer;
				second_stage.TabIndex = 2;
				second_stage.FontSize = 14;
				first_stage.Items.Add(second_stage);
				if (Level >= 5) {
					//不为空时 第3层节点 为 测试日期
					TreeViewIconsItem three_stage = new TreeViewIconsItem();
					three_stage.HeaderText = test_parameter._47Test_Date;
					three_stage.Icon = three_stage.iconSourceDate1;
					three_stage.TabIndex = 3;
					second_stage.Items.Add(three_stage);
					//不为空时 第4层节点 为 测试的分接位
					TreeViewIconsItem four_stage = new TreeViewIconsItem();
					four_stage.HeaderText = test_parameter._48Access_position;
					four_stage.TabIndex = 4;
					four_stage.Icon = four_stage.iconSourceNode;
					three_stage.Items.Add(four_stage);
					if (Level == 6) {
						//不为空时 第5层节点 为 测试次数
						TreeViewIconsItem five_stage = new TreeViewIconsItem();
						five_stage.HeaderText = test_parameter._49Mesurent_Counts;
						test_parameter._49Mesurent_Counts = five_stage.HeaderText;
						five_stage.Icon = five_stage.iconSourceTest_Count;
						five_stage.TabIndex = 5;
						four_stage.Items.Add(five_stage);
						Monunt_Phase(five_stage);
					}

				}

			}
			//树根节点存在时候 比较节点信息
			else {
				//错误操作时 啥也不做 返回
				if (transformer_parameter._2TransformerName == "" || transformer_parameter._2TransformerName == null) {
					return;
				}
				foreach (TreeViewIconsItem item in TreeViewTestItem.Items) {
					#region 如果 变压器所属单位已存在于TreeView中
					if (item.HeaderText == transformer_parameter._1ItsUnitName) {
						isExist = true;
						foreach (TreeViewIconsItem tvii in item.Items) {
							#region 如果变压器名称已存在于TreeView中

							if (tvii.HeaderText.ToString() == transformer_parameter._2TransformerName) {
								isExist2 = true;
								if (Level >= 5) {
									foreach (TreeViewIconsItem tviii in tvii.Items) {

										#region 如果测试日期已存在于TreeView中
										if (tviii.HeaderText.ToString() == test_parameter._47Test_Date) {
											isExist3 = true;
											foreach (TreeViewIconsItem tviiii in tviii.Items) {

												#region 如果测试分接位已存在于 TreeView中
												if (tviiii.HeaderText == test_parameter._48Access_position) {
													isExist4 = true;
													//不为空时 第5层节点 为 测试次数
													if (Level == 6) {
														TreeViewIconsItem five_stage = new TreeViewIconsItem();
														five_stage.HeaderText = test_parameter._49Mesurent_Counts;
														five_stage.Icon = five_stage.iconSourceTest_Count;
														five_stage.TabIndex = 5;
														tviiii.Items.Add(five_stage);

														Monunt_Phase(five_stage);
														tviiii.IsExpanded = true;
													}
												}
												#endregion
												#region 否则
												else if (!isExist3) {
													isExist4 = false;
												}
												#endregion
											}
											if (!isExist4) {
												//不为空时 第4层节点 为 测试的分接位
												TreeViewIconsItem four_stage = new TreeViewIconsItem();
												four_stage.HeaderText = test_parameter._48Access_position;
												four_stage.TabIndex = 4;
												four_stage.Icon = four_stage.iconSourceNode;
												tviii.Items.Add(four_stage);
												//不为空时 第5层节点 为 测试次数
												if (Level == 6) {
													TreeViewIconsItem five_stage = new TreeViewIconsItem();
													five_stage.HeaderText = test_parameter._49Mesurent_Counts;
													five_stage.Icon = five_stage.iconSourceTest_Count;
													five_stage.TabIndex = 5;
													four_stage.Items.Add(five_stage);
													Monunt_Phase(five_stage);
												}

											}
										}
										#endregion
										#region 否则
										else {
											isExist3 = false;
										}
										#endregion
									}
									if (!isExist3) {
										//不为空时 第3层节点 为 测试日期
										TreeViewIconsItem three_stage = new TreeViewIconsItem();
										three_stage.HeaderText = test_parameter._47Test_Date;
										three_stage.Icon = three_stage.iconSourceDate1;
										three_stage.TabIndex = 3;
										tvii.Items.Add(three_stage);
										//不为空时 第4层节点 为 测试的分接位
										TreeViewIconsItem four_stage = new TreeViewIconsItem();
										//如果是自动连续测量
										four_stage.HeaderText = test_parameter._48Access_position;
										four_stage.TabIndex = 4;
										four_stage.Icon = four_stage.iconSourceNode;
										three_stage.Items.Add(four_stage);
										if (Level == 6) {
											TreeViewIconsItem five_stage = new TreeViewIconsItem();
											five_stage.HeaderText = test_parameter._49Mesurent_Counts;
											five_stage.Icon = five_stage.iconSourceTest_Count;
											five_stage.TabIndex = 5;
											four_stage.Items.Add(five_stage);
											Monunt_Phase(five_stage);
										}
									}
								}

							}
							#endregion
							#region 否则
							//如果是最后一项
							else if (!isExist2) {
								isExist2 = false;
							}
							#endregion
						}
						if (!isExist2) {
							//不为空时 第2层节点 为 变压器名称
							TreeViewIconsItem second_stage = new TreeViewIconsItem();
							second_stage.HeaderText = transformer_parameter._2TransformerName;
							second_stage.Icon = item.iconSourceTransformer;
							second_stage.TabIndex = 2;
							second_stage.FontSize = 14;
							item.Items.Add(second_stage);
							if (Level >= 5) {
								//不为空时 第3层节点 为 测试日期
								TreeViewIconsItem three_stage = new TreeViewIconsItem();
								three_stage.HeaderText = test_parameter._47Test_Date;
								three_stage.Icon = three_stage.iconSourceDate1;
								three_stage.TabIndex = 3;
								second_stage.Items.Add(three_stage);
								//不为空时 第4层节点 为 测试的分接位
								TreeViewIconsItem four_stage = new TreeViewIconsItem();
								four_stage.HeaderText = test_parameter._48Access_position;
								four_stage.TabIndex = 4;
								four_stage.Icon = four_stage.iconSourceNode;
								three_stage.Items.Add(four_stage);
								if (Level == 6) {
									//不为空时 第5层节点 为 测试次数
									TreeViewIconsItem five_stage = new TreeViewIconsItem();
									five_stage.HeaderText = test_parameter._49Mesurent_Counts;
									test_parameter._49Mesurent_Counts = five_stage.HeaderText;
									five_stage.Icon = five_stage.iconSourceTest_Count;
									five_stage.TabIndex = 5;
									four_stage.Items.Add(five_stage);
									Monunt_Phase(five_stage);
								}

							}

						}
					}
					#endregion
					#region 否则
					else if (isExist != true) {
						isExist = false;
					}
					#endregion
				}
				if (!isExist) {
					//当变压器属性为空的时候 啥也不做 这属于错误操作！
					if (transformer_parameter._2TransformerName == "" || transformer_parameter._2TransformerName == null) {
						return;
					}
					//不为空时 第1层节点 为 变压器所属单位名称
					TreeViewIconsItem first_stage = new TreeViewIconsItem();
					first_stage.HeaderText = transformer_parameter._1ItsUnitName;
					first_stage.TabIndex = 1;
					first_stage.FontSize = 14;
					TreeViewTestItem.Items.Add(first_stage);
					//不为空时 第2层节点 为 变压器名称
					TreeViewIconsItem second_stage = new TreeViewIconsItem();
					second_stage.HeaderText = transformer_parameter._2TransformerName;
					second_stage.Icon = first_stage.iconSourceTransformer;
					second_stage.TabIndex = 2;
					second_stage.FontSize = 14;
					first_stage.Items.Add(second_stage);
					if (Level >= 5) {
						//不为空时 第3层节点 为 测试日期
						TreeViewIconsItem three_stage = new TreeViewIconsItem();
						three_stage.HeaderText = test_parameter._47Test_Date;
						three_stage.Icon = three_stage.iconSourceDate1;
						three_stage.TabIndex = 3;
						second_stage.Items.Add(three_stage);
						//不为空时 第4层节点 为 测试的分接位
						TreeViewIconsItem four_stage = new TreeViewIconsItem();
						four_stage.HeaderText = test_parameter._48Access_position;
						four_stage.TabIndex = 4;
						four_stage.Icon = four_stage.iconSourceNode;
						three_stage.Items.Add(four_stage);
						if (Level == 6) {
							//不为空时 第5层节点 为 测试次数
							TreeViewIconsItem five_stage = new TreeViewIconsItem();
							five_stage.HeaderText = test_parameter._49Mesurent_Counts;
							test_parameter._49Mesurent_Counts = five_stage.HeaderText;
							five_stage.Icon = five_stage.iconSourceTest_Count;
							five_stage.TabIndex = 5;
							four_stage.Items.Add(five_stage);
							Monunt_Phase(five_stage);
						}

					}
				}
			}
			#endregion
		}
		void TreeViewUpdate_for_OpenFile() {
			#region TreeView 更新
			bool isExist = false;
			bool isExist2 = false;
			bool isExist3 = false;
			bool isExist4 = false;
			bool isExist5 = false;
			//树根节点不存在时候 新建根节点
			if (TreeViewTestItem.Items.Count == 0) {
				//当变压器属性为空的时候 啥也不做 这属于错误操作！
				if (test_parameter._1curTransformerName == null) {
					return;
				}
				//不为空时 第1层节点 为 变压器所属单位名称
				TreeViewIconsItem first_stage = new TreeViewIconsItem();
				first_stage.HeaderText = transformer_parameter._1ItsUnitName;
				first_stage.TabIndex = 1;
				first_stage.FontSize = 14;
				TreeViewTestItem.Items.Add(first_stage);
				//不为空时 第2层节点 为 变压器名称
				TreeViewIconsItem second_stage = new TreeViewIconsItem();
				second_stage.HeaderText = test_parameter._1curTransformerName;
				second_stage.Icon = first_stage.iconSourceTransformer;
				second_stage.TabIndex = 2;
				second_stage.FontSize = 14;
				first_stage.Items.Add(second_stage);
				//不为空时 第3层节点 为 测试日期
				if (test_parameter._47Test_Date == null) {
					return;
				}
				TreeViewIconsItem three_stage = new TreeViewIconsItem();
				three_stage.HeaderText = test_parameter._47Test_Date;
				three_stage.Icon = first_stage.iconSourceDate1;
				three_stage.TabIndex = 3;
				second_stage.Items.Add(three_stage);
				//不为空时 第4层节点 为 测试的分接位
				if (test_parameter._48Access_position == null) {
					return;
				}
				TreeViewIconsItem four_stage = new TreeViewIconsItem();
				four_stage.HeaderText = test_parameter._48Access_position;
				four_stage.TabIndex = 4;
				four_stage.Icon = four_stage.iconSourceNode;
				three_stage.Items.Add(four_stage);
				//不为空时 第5层节点 为 测试次数
				if (test_parameter._49Mesurent_Counts != null) {
					TreeViewIconsItem five_stage = new TreeViewIconsItem();
					five_stage.HeaderText = test_parameter._49Mesurent_Counts;
					five_stage.Icon = five_stage.iconSourceTest_Count;
					five_stage.TabIndex = 5;
					four_stage.Items.Add(five_stage);
					Monunt_Phase(five_stage);
					isExist = true;
				}

			}
			//树根节点存在时候 比较节点信息
			else {
				//错误操作时 啥也不做 返回
				if (test_parameter._1curTransformerName == null) {
					return;
				}
				foreach (TreeViewIconsItem item in TreeViewTestItem.Items) {
					#region 如果 变压器所属单位已存在于TreeView中
					if (item.HeaderText == transformer_parameter._1ItsUnitName) {
						isExist = true;
						foreach (TreeViewIconsItem tvii in item.Items) {
							#region 如果变压器名称已存在于TreeView中

							if (tvii.HeaderText.ToString() == test_parameter._1curTransformerName) {
								isExist2 = true;
								foreach (TreeViewIconsItem tviii in tvii.Items) {

									#region 如果测试日期已存在于TreeView中
									if (tviii.HeaderText.ToString() == test_parameter._47Test_Date) {
										isExist3 = true;
										foreach (TreeViewIconsItem tviiii in tviii.Items) {

											#region 如果测试分接位已存在于 TreeView中
											if (tviiii.HeaderText == test_parameter._48Access_position) {
												isExist4 = true;
												if (test_parameter._49Mesurent_Counts == null) {
													isExist5 = true;
												}
												foreach (TreeViewIconsItem tviiiii in tviiii.Items) {

													if (tviiiii.HeaderText == test_parameter._49Mesurent_Counts) {
														isExist5 = true;
													}
													else if (!isExist5) {
														isExist5 = false;
													}
												}
												if (!isExist5) {
													if (test_parameter._49Mesurent_Counts != null) {
														TreeViewIconsItem five_stage = new TreeViewIconsItem();
														five_stage.HeaderText = test_parameter._49Mesurent_Counts;
														five_stage.Icon = five_stage.iconSourceTest_Count;
														five_stage.TabIndex = 5;
														tviiii.Items.Add(five_stage);
														Monunt_Phase(five_stage);
													}
												}
											}
											#endregion
											#region 否则
											else if (!isExist4) {
												isExist4 = false;
											}
											#endregion
										}
										if (!isExist4) {
											//不为空时 第4层节点 为 测试的分接位
											if (test_parameter._48Access_position == null) {
												return;
											}
											TreeViewIconsItem four_stage = new TreeViewIconsItem();
											four_stage.HeaderText = test_parameter._48Access_position;
											four_stage.TabIndex = 4;
											four_stage.Icon = four_stage.iconSourceNode;
											tviii.Items.Add(four_stage);
											if (test_parameter._49Mesurent_Counts != null) {
												//不为空时 第5层节点 为 测试次数
												TreeViewIconsItem five_stage = new TreeViewIconsItem();
												five_stage.HeaderText = test_parameter._49Mesurent_Counts;
												five_stage.Icon = five_stage.iconSourceTest_Count;
												five_stage.TabIndex = 5;
												four_stage.Items.Add(five_stage);
												Monunt_Phase(five_stage);
											}

										}
									}
									#endregion
									#region 否则
									else if (!isExist3) {
										isExist3 = false;
									}
									#endregion
								}
								if (!isExist3) {
									//不为空时 第3层节点 为 测试日期
									TreeViewIconsItem three_stage = new TreeViewIconsItem();
									three_stage.HeaderText = test_parameter._47Test_Date;
									three_stage.Icon = three_stage.iconSourceDate1;
									three_stage.TabIndex = 3;
									tvii.Items.Add(three_stage);
									//不为空时 第4层节点 为 测试的分接位
									TreeViewIconsItem four_stage = new TreeViewIconsItem();
									//如果是自动连续测量
									four_stage.HeaderText = test_parameter._48Access_position;
									four_stage.TabIndex = 4;
									four_stage.Icon = four_stage.iconSourceNode;
									three_stage.Items.Add(four_stage);
									//不为空时 第5层节点 为 测试次数
									if (test_parameter._49Mesurent_Counts != null) {
										TreeViewIconsItem five_stage = new TreeViewIconsItem();
										five_stage.HeaderText = test_parameter._49Mesurent_Counts;
										five_stage.Icon = five_stage.iconSourceTest_Count;
										five_stage.TabIndex = 5;
										four_stage.Items.Add(five_stage);
										Monunt_Phase(five_stage);
									}
								}
							}
							#endregion
							#region 否则
							//如果是最后一项
							else if (!isExist2) {
								isExist2 = false;
							}
							#endregion
						}
						if (!isExist2) {
							//不为空时 第2层节点 为 变压器名称
							if (test_parameter._1curTransformerName == null) {
								return;
							}
							TreeViewIconsItem second_stage = new TreeViewIconsItem();
							second_stage.HeaderText = test_parameter._1curTransformerName;
							second_stage.Icon = item.iconSourceTransformer;
							second_stage.TabIndex = 2;
							second_stage.FontSize = 14;
							item.Items.Add(second_stage);
							//不为空时 第3层节点 为 测试日期
							if (test_parameter._47Test_Date == null) {
								return;
							}
							TreeViewIconsItem three_stage = new TreeViewIconsItem();
							three_stage.HeaderText = test_parameter._47Test_Date;
							three_stage.Icon = three_stage.iconSourceDate1;
							three_stage.TabIndex = 3;
							second_stage.Items.Add(three_stage);
							//不为空时 第4层节点 为 测试的分接位
							if (test_parameter._48Access_position == null) {
								return;
							}
							TreeViewIconsItem four_stage = new TreeViewIconsItem();
							four_stage.HeaderText = test_parameter._48Access_position;
							four_stage.TabIndex = 4;
							four_stage.Icon = four_stage.iconSourceNode;
							three_stage.Items.Add(four_stage);
							//不为空时 第5层节点 为 测试次数
							if (test_parameter._49Mesurent_Counts != null) {
								TreeViewIconsItem five_stage = new TreeViewIconsItem();
								five_stage.HeaderText = test_parameter._49Mesurent_Counts;
								five_stage.Icon = five_stage.iconSourceTest_Count;
								five_stage.TabIndex = 5;
								four_stage.Items.Add(five_stage);
								Monunt_Phase(five_stage);
							}
						}
					}
					#endregion
					#region 否则
					else if (!isExist) {
						isExist = false;
					}
					#endregion
				}
				if (!isExist) {
					//当变压器属性为空的时候 啥也不做 这属于错误操作！
					if (test_parameter._1curTransformerName == null) {
						return;
					}
					//不为空时 第1层节点 为 变压器所属单位名称
					TreeViewIconsItem first_stage = new TreeViewIconsItem();
					first_stage.HeaderText = transformer_parameter._1ItsUnitName;
					first_stage.TabIndex = 1;
					first_stage.FontSize = 14;
					TreeViewTestItem.Items.Add(first_stage);
					//不为空时 第2层节点 为 变压器名称
					TreeViewIconsItem second_stage = new TreeViewIconsItem();
					second_stage.HeaderText = test_parameter._1curTransformerName;
					second_stage.Icon = first_stage.iconSourceTransformer;
					second_stage.TabIndex = 2;
					second_stage.FontSize = 14;
					first_stage.Items.Add(second_stage);
					//不为空时 第3层节点 为 测试日期
					if (test_parameter._47Test_Date == null) {
						return;
					}
					TreeViewIconsItem three_stage = new TreeViewIconsItem();
					three_stage.HeaderText = test_parameter._47Test_Date;
					three_stage.Icon = first_stage.iconSourceDate1;
					three_stage.TabIndex = 3;
					second_stage.Items.Add(three_stage);
					//不为空时 第4层节点 为 测试的分接位
					if (test_parameter._48Access_position == null) {
						return;
					}
					TreeViewIconsItem four_stage = new TreeViewIconsItem();
					four_stage.HeaderText = test_parameter._48Access_position;
					four_stage.TabIndex = 4;
					four_stage.Icon = four_stage.iconSourceNode;
					three_stage.Items.Add(four_stage);
					//不为空时 第5层节点 为 测试次数
					if (test_parameter._49Mesurent_Counts != null) {
						TreeViewIconsItem five_stage = new TreeViewIconsItem();
						five_stage.HeaderText = test_parameter._49Mesurent_Counts;
						five_stage.Icon = five_stage.iconSourceTest_Count;
						five_stage.TabIndex = 5;
						four_stage.Items.Add(five_stage);
						Monunt_Phase(five_stage);
					}
				}
			}
			#endregion
		}
		public int num(string t) {
			int last = t.Length - 1;
			int gewei = 0;
			int shiwei = 0;
			int shuzigeshu = 0;
			for (int k = 0; k < last; k++) {
				if (Char.IsDigit(t[k])) {
					shuzigeshu += 1;
					if (shuzigeshu == 1) {
						gewei = int.Parse(t[k].ToString());
					}
					if (shuzigeshu == 2) {
						shiwei = gewei;
						gewei = int.Parse(t[k].ToString());
					}
				}
			}
			if (shuzigeshu == 0) {
				return 0;
			}
			else {
				return shiwei * 10 + gewei;
			}
		}
		void Monunt_Phase(TreeViewIconsItem tviiiii) {
			TreeViewIconsItem C_phase = new TreeViewIconsItem();
			C_phase.HeaderText = "C相数据";
			C_phase.TabIndex = 33;
			C_phase.Foreground = new SolidColorBrush(Color.FromRgb(220, 20, 60));
			C_phase.Icon = C_phase.iconSourceData;
			tviiiii.Items.Add(C_phase);
			if (transformer_parameter._5Thrid_phase == true) {
				TreeViewIconsItem A_phase = new TreeViewIconsItem();
				A_phase.HeaderText = "A相数据";
				A_phase.Foreground = new SolidColorBrush(Color.FromRgb(255, 140, 0));
				A_phase.TabIndex = 11;
				A_phase.Icon = A_phase.iconSourceData;
				tviiiii.Items.Add(A_phase);
				TreeViewIconsItem B_phase = new TreeViewIconsItem();
				B_phase.HeaderText = "B相数据";
				B_phase.TabIndex = 22;
				B_phase.Icon = B_phase.iconSourceData;
				B_phase.Foreground = new SolidColorBrush(Color.FromRgb(34, 139, 34));
				tviiiii.Items.Add(B_phase);
			}
		}

		#endregion

		#region UI Update 数据更新 开始测试
		void ListViewUpdate() {
			#region ListView 更新
			class_TestInfo[] testinfos = new class_TestInfo[8];
			testinfos[0] = format_class_testinfo("单位:", (transformer_parameter._1ItsUnitName != null ? transformer_parameter._1ItsUnitName : "未设置"));
			testinfos[1] = format_class_testinfo("变压器:", (transformer_parameter._2TransformerName != null ? transformer_parameter._2TransformerName : "未设置"));
			testinfos[2] = format_class_testinfo("接线法:", (transformer_parameter._13TransformerWindingConnMethod != null ? transformer_parameter._13TransformerWindingConnMethod : "未设置"));
			testinfos[3] = format_class_testinfo("相数:", (transformer_parameter._6Transformerphase != null ? transformer_parameter._6Transformerphase : "未设置"));
			testinfos[4] = format_class_testinfo("交流/直流:", (test_parameter.Measurment != null ? test_parameter.Measurment : "未知"));
			if (test_parameter.Measurment == "直流测试") {
				testinfos[5] = format_class_testinfo("采样率:", 20 + "KHz");
			}
			else {
				testinfos[5] = format_class_testinfo("采样率:", (test_parameter._4SampleFrequency != null ? test_parameter._4SampleFrequency : "未设置") + "KHz");
			}
			if (test_parameter._24IsACMeasurment) {
				if (test_parameter._23IsExternalPower) {
					testinfos[6] = format_class_testinfo("输出电压:", "外接电源");
				}
				else {
					testinfos[6] = format_class_testinfo("输出电压:", (test_parameter._2OutputVolt != null ? test_parameter._2OutputVolt : "未设置") + "V");

				}
			}
			else {
				testinfos[6] = format_class_testinfo("测试档位:", (test_parameter._16MeasureGear_DC != null ? test_parameter._16MeasureGear_DC : "未设置"));
			}
			testinfos[7] = format_class_testinfo("开关分列数:", (transformer_parameter._23SwitchColumnCount != null ? transformer_parameter._23SwitchColumnCount : "未设置"));
			LVTestInfo.ItemsSource = testinfos;
			#endregion
		}
		void exhange() {
			#region exchange
			if (transformer_parameter._4Single_phase == true) {
				transformer_parameter._6Transformerphase = "单相";
			}
			if (transformer_parameter._5Thrid_phase == true) {
				transformer_parameter._6Transformerphase = "三相";
			}
			if (transformer_parameter._7Double_Winding == true) {
				transformer_parameter._9TransformerWinding = "双绕组";
			}
			if (transformer_parameter._8Three_Winding == true) {
				transformer_parameter._9TransformerWinding = "三绕组";
			}
			if (transformer_parameter._10Y_method == true) {
				transformer_parameter._13TransformerWindingConnMethod = "Y型接法";
			}
			if (transformer_parameter._11YN_method == true) {
				transformer_parameter._13TransformerWindingConnMethod = "YN型接法";
			}
			if (transformer_parameter._12Triangle_method == true) {
				transformer_parameter._13TransformerWindingConnMethod = "三角形接法";

			}
			if (transformer_parameter._27SwitchColumn_One_Count == true) {
				transformer_parameter._23SwitchColumnCount = "单列";
			}
			if (transformer_parameter._28SwitchColumn_Two_Count == true) {
				transformer_parameter._23SwitchColumnCount = "双列";
			}
			if (transformer_parameter._29SwitchColumn_Three_Count == true) {
				transformer_parameter._23SwitchColumnCount = "三列";
			}
			if (test_parameter._24IsACMeasurment) {
				test_parameter.Measurment = "交流测试";
			}
			if (test_parameter._25IsDCMeasurment) {
				test_parameter.Measurment = "直流测试";
			}
			#endregion

		}
		void Test_Condition_Initial(int Page_Count) {
			TCP连接窗口.is_end = false;
			btnSave.IsEnabled = false;
			is_开始接受 = true;
			is_init = false;
			isPeakVale_initial = true;
			need_juage = true;
			is_trig = true;
			page_count = 0;
			丢包序号 = 0;
			btnReAnalysis.IsEnabled = false;
			是否第一次收到数据 = true;
			quxian_Show_areaAphase.Clear();
			quxian_Show_areaBphase.Clear();
			quxian_Show_areaCphase.Clear();
			quxian_Show_Trig_Aphase.Clear();
			quxian_Show_Trig_Bphase.Clear();
			quxian_Show_Trig_Cphase.Clear();
			Max = 0;
			A_data.Clear();
			B_data.Clear();
			C_data.Clear();
			set_cursor_position(cursor_collection[Tchart1.Name + "A1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "A2"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "B1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "B2"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "C1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "C2"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], 0, false);
			for (int i = 0; i < 3; i++) {
				try {
					line_forTest[i].Active = true;
					line_forTest[i].XValues.Clear();
					line_forTest[i].YValues.Clear();
					Tchart1.Axes.Bottom.SetMinMax(0, 600);
					Tchart1.AutoRepaint = true;
				}
				catch {

				}
			}
			if (test_parameter._24IsACMeasurment) {
				Tchart1.Axes.Left.Inverted = false;
				Tchart_1.Axes.Left.Inverted = false;
				Tchart_2.Axes.Left.Inverted = false;
			}
			else {
				Tchart1.Axes.Left.Inverted = true;
				Tchart_1.Axes.Left.Inverted = true;
				Tchart_2.Axes.Left.Inverted = true;
				Tchart1.Axes.Left.Automatic = true;
				Tchart_1.Axes.Left.Automatic = true;
				Tchart_2.Axes.Left.Automatic = true;
			}
		}
		string 上一次测试的变压器 = null;
		string 上一次测试的变压器所属单位 = null;
		string 上一次测试分接位 = null;
		public void TestStage_Update(object sender) {
			exhange();
			#region TreeView响应
			if (sender.GetType() == typeof(TreeView)) {
				return;
			}
			#endregion

			#region 测试窗口新增按钮点击响应
			if (sender.GetType() == typeof(Button)) {
				Button btn = sender as Button;
				if (btn.Name == "btnAddNewTestTransFormer") {
					transformer_parameter._2TransformerName = test_parameter._1curTransformerName;
					transformer_configuration();
				}
			}
			#endregion

			#region 变压器信息设置

			if (sender.GetType() == typeof(Button)) {
				Button btn = sender as Button;
				if (btn.Name == "btnTransformerParaConfirm") {
					bool is_need_add_company = false;
					bool is_need_add_transformer = false;
					bool is_need_add_SwitchModel = false;
					bool is_need_add_SwitchProduectName = false;
					#region 去空
					if (变压器属性设置窗口.cmbExistTransFormers.Text == "") {
						MessageBox.Show("请选择或者输入变压器名称");
						变压器属性设置窗口.cmbExistTransFormers.Focus();
						变压器属性设置窗口.Activate();
						变压器属性设置窗口.Show();
						return;
					}
					if (变压器属性设置窗口.cmbExistCompany.Text == "") {
						MessageBox.Show("请选择或者输入变压器所属单位");
						变压器属性设置窗口.cmbExistCompany.Focus();
						变压器属性设置窗口.Activate();
						变压器属性设置窗口.Show();
						return;
					}
					if (变压器属性设置窗口.cmbSwitchModel.Text == "") {
						MessageBox.Show("请选择或者输入开关型号");
						变压器属性设置窗口.cmbSwitchModel.Focus();
						变压器属性设置窗口.Activate();
						变压器属性设置窗口.Show();
						return;
					}
					if (变压器属性设置窗口.cmbSwitchProducerName.Text == "") {
						MessageBox.Show("请选择或者输入开关生产厂家");
						变压器属性设置窗口.cmbSwitchProducerName.Focus();
						变压器属性设置窗口.Activate();
						变压器属性设置窗口.Show();
						return;
					}
					#endregion
					#region 增加所属单位  变压器 开关生产厂家
					if (变压器属性设置窗口.cmbExistCompany.Items.Count == 0) {
						is_need_add_company = true;
					}
					for (int i = 0; i < 变压器属性设置窗口.cmbExistCompany.Items.Count; i++) {
						if (变压器属性设置窗口.cmbExistCompany.Items[i].ToString() == 变压器属性设置窗口.cmbExistCompany.Text) {
							is_need_add_company = false;
						}
						else {
							is_need_add_company = true;
						}
					}
					if (is_need_add_company) {
						变压器属性设置窗口.cmbExistCompany.Items.Add(变压器属性设置窗口.cmbExistCompany.Text);
						transformer_parameter._1ItsUnitName = 变压器属性设置窗口.cmbExistCompany.Text;
					}
					///增加变压器
					if (变压器属性设置窗口.cmbExistTransFormers.Items.Count == 0) {
						is_need_add_transformer = true;
					}
					for (int i = 0; i < 变压器属性设置窗口.cmbExistTransFormers.Items.Count; i++) {
						if (变压器属性设置窗口.cmbExistTransFormers.Items[i].ToString() == 变压器属性设置窗口.cmbExistTransFormers.Text) {
							is_need_add_transformer = false;
						}
						else {
							is_need_add_transformer = true;
							break;
						}
					}
					if (is_need_add_transformer) {
						变压器属性设置窗口.cmbExistTransFormers.Items.Add(变压器属性设置窗口.cmbExistTransFormers.Text);
						for (int i = 0; i < 变压器属性设置窗口.cmbExistTransFormers.Items.Count; i++) {
							if (变压器属性设置窗口.cmbExistTransFormers.Items[i].ToString() == 变压器属性设置窗口.cmbExistTransFormers.Text) {
								变压器属性设置窗口.cmbExistTransFormers.SelectedIndex = i;
								变压器属性设置窗口.cmbExistTransFormers.Text = 变压器属性设置窗口.cmbExistTransFormers.SelectedItem.ToString();
							}
						}
						transformer_parameter._2TransformerName = 变压器属性设置窗口.cmbExistTransFormers.Text;
					}
					if (变压器属性设置窗口.cmbExistTransFormers.Text != "") {
						transformer_parameter._2TransformerName = 变压器属性设置窗口.cmbExistTransFormers.Text;
					}

					//增加开关型号
					if (变压器属性设置窗口.cmbSwitchModel.Items.Count == 0) {
						is_need_add_SwitchModel = true;
					}
					for (int i = 0; i < 变压器属性设置窗口.cmbSwitchModel.Items.Count; i++) {
						if (变压器属性设置窗口.cmbSwitchModel.Items[i].ToString() == 变压器属性设置窗口.cmbSwitchModel.Text) {
							is_need_add_SwitchModel = false;
						}
						else {
							is_need_add_SwitchModel = true;
							break;
						}
					}
					if (is_need_add_SwitchModel) {
						变压器属性设置窗口.cmbSwitchModel.Items.Add(变压器属性设置窗口.cmbSwitchModel.Text);
						for (int i = 0; i < 变压器属性设置窗口.cmbSwitchModel.Items.Count; i++) {
							if (变压器属性设置窗口.cmbSwitchModel.Items[i].ToString() == 变压器属性设置窗口.cmbSwitchModel.Text) {
								变压器属性设置窗口.cmbSwitchModel.SelectedIndex = i;
								变压器属性设置窗口.cmbSwitchModel.Text = 变压器属性设置窗口.cmbSwitchModel.SelectedItem.ToString();
							}
						}
						transformer_parameter._21SwitchModel = 变压器属性设置窗口.cmbSwitchModel.Text;
					}

					//增加开关生产商家
					if (变压器属性设置窗口.cmbSwitchProducerName.Items.Count == 0) {
						is_need_add_SwitchProduectName = true;
					}
					for (int i = 0; i < 变压器属性设置窗口.cmbSwitchProducerName.Items.Count; i++) {
						if (变压器属性设置窗口.cmbSwitchProducerName.Items[i].ToString() == 变压器属性设置窗口.cmbSwitchProducerName.Text) {
							is_need_add_SwitchProduectName = false;
						}
						else {
							is_need_add_SwitchProduectName = true;
							break;
						}
					}
					if (is_need_add_SwitchProduectName) {
						变压器属性设置窗口.cmbSwitchProducerName.Items.Add(变压器属性设置窗口.cmbSwitchProducerName.Text);
						for (int i = 0; i < 变压器属性设置窗口.cmbSwitchProducerName.Items.Count; i++) {
							if (变压器属性设置窗口.cmbSwitchProducerName.Items[i].ToString() == 变压器属性设置窗口.cmbSwitchProducerName.Text) {
								变压器属性设置窗口.cmbSwitchProducerName.SelectedIndex = i;
								变压器属性设置窗口.cmbSwitchProducerName.Text = 变压器属性设置窗口.cmbSwitchProducerName.SelectedItem.ToString();
							}
						}
						transformer_parameter._20SwitchManufactorName = 变压器属性设置窗口.cmbSwitchProducerName.Text;
					}
					#endregion
					transformer_parameter._10Y_method = (bool)变压器属性设置窗口.cbY.IsChecked;
					transformer_parameter._11YN_method = (bool)变压器属性设置窗口.cbYO.IsChecked;
					transformer_parameter._12Triangle_method = (bool)变压器属性设置窗口.cbTangle.IsChecked;
					transformer_parameter._20SwitchManufactorName = 变压器属性设置窗口.cmbSwitchProducerName.Text == "" ? "未知" : 变压器属性设置窗口.cmbSwitchProducerName.Text;
					transformer_parameter._21SwitchModel = 变压器属性设置窗口.cmbSwitchModel.Text == "" ? "未知" : 变压器属性设置窗口.cmbSwitchModel.Text;
					transformer_parameter._22SwitchCode = 变压器属性设置窗口.tbSwitchProductionCode.Text;
					transformer_parameter._24SwitchStartWorkingPosition = 变压器属性设置窗口.tbStartWorkingPosition.Text == "" ? "1" : 变压器属性设置窗口.tbStartWorkingPosition.Text;
					transformer_parameter._25SwitchStopWorkingPosition = 变压器属性设置窗口.tbEndWorkingPosition.Text;
					transformer_parameter._26SwitchMidPosition = 变压器属性设置窗口.tbMidPosition.Text;
					transformer_parameter._27SwitchColumn_One_Count = (bool)变压器属性设置窗口.cbOne.IsChecked;
					transformer_parameter._28SwitchColumn_Two_Count = (bool)变压器属性设置窗口.cbTwo.IsChecked;
					transformer_parameter._29SwitchColumn_Three_Count = (bool)变压器属性设置窗口.cbThrid.IsChecked;
					transformer_parameter._3TransformerModel = 变压器属性设置窗口.tbTransFormModel.Text;
					transformer_parameter._4Single_phase = (bool)变压器属性设置窗口.cb1P.IsChecked;
					transformer_parameter._5Thrid_phase = (bool)变压器属性设置窗口.cb3P.IsChecked;
					transformer_parameter._7Double_Winding = (bool)变压器属性设置窗口.cb2RZ.IsChecked;
					transformer_parameter._8Three_Winding = (bool)变压器属性设置窗口.cb3RZ.IsChecked;
					if (变压器属性设置窗口.tbStartWorkingPosition.Text == "" || 变压器属性设置窗口.tbEndWorkingPosition.Text == "" || 变压器属性设置窗口.tbMidPosition.Text == "") {
						MessageBox.Show("请设置开关工作位置!");
						变压器属性设置窗口.Activate();
						变压器属性设置窗口.Show();
						return;
					}
					#region exchange
					if (transformer_parameter._4Single_phase == true) {
						transformer_parameter._6Transformerphase = "单相";
					}
					if (transformer_parameter._5Thrid_phase == true) {
						transformer_parameter._6Transformerphase = "三相";
					}
					if (transformer_parameter._7Double_Winding == true) {
						transformer_parameter._9TransformerWinding = "双绕组";
					}
					if (transformer_parameter._8Three_Winding == true) {
						transformer_parameter._9TransformerWinding = "三绕组";
					}
					if (transformer_parameter._10Y_method == true) {
						transformer_parameter._13TransformerWindingConnMethod = "Y型接法";
					}
					if (transformer_parameter._11YN_method == true) {
						transformer_parameter._13TransformerWindingConnMethod = "YN型接法";
					}
					if (transformer_parameter._12Triangle_method == true) {
						transformer_parameter._13TransformerWindingConnMethod = "三角形接法";

					}
					if (transformer_parameter._27SwitchColumn_One_Count == true) {
						transformer_parameter._23SwitchColumnCount = "单列";
					}
					if (transformer_parameter._28SwitchColumn_Two_Count == true) {
						transformer_parameter._23SwitchColumnCount = "双列";
					}
					if (transformer_parameter._29SwitchColumn_Three_Count == true) {
						transformer_parameter._23SwitchColumnCount = "三列";
					}
					if (test_parameter._24IsACMeasurment) {
						test_parameter.Measurment = "交流测试";
					}
					if (test_parameter._25IsDCMeasurment) {
						test_parameter.Measurment = "直流测试";
					}
					#endregion
					#region 数据库变压器参数赋值
					// 变压器参数修改
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
						OleDbHelper.File_Name_e._1ItsUnitName, transformer_parameter._1ItsUnitName);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._2TransFormName, transformer_parameter._2TransformerName);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._3TransFormModel, transformer_parameter._3TransformerModel);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._6Transformerphase, transformer_parameter._6Transformerphase);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._9TransformerWinding, transformer_parameter._9TransformerWinding);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._13TransformerWindingConnMethod, transformer_parameter._13TransformerWindingConnMethod);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._20SwitchManufactorName, transformer_parameter._20SwitchManufactorName);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._21SwitchModel, transformer_parameter._21SwitchModel);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._22SwitchCode, transformer_parameter._22SwitchCode);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._23SwitchColumnCount, transformer_parameter._23SwitchColumnCount);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._24SwitchStartWorkingPosition, transformer_parameter._24SwitchStartWorkingPosition);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._25SwitchStopWorkingPosition, transformer_parameter._25SwitchStopWorkingPosition);
					OleDbHelper.TransForm_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.TransForm_Setting_Parameters, (int)
							 OleDbHelper.File_Name_e._26SwitchMidPosition, transformer_parameter._26SwitchMidPosition);
					if (!OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, transformer_parameter._1ItsUnitName)) {
						OleDbHelper.Insert(OleDbHelper.TransForm_Setting_Parameters, OleDbHelper.TransFormer_Table_Name);
					}
					else {
						bool update = true;
						if (!OleDbHelper.Select(transformer_parameter._1ItsUnitName, transformer_parameter._2TransformerName)) {
							OleDbHelper.Insert(OleDbHelper.TransForm_Setting_Parameters, OleDbHelper.TransFormer_Table_Name);
							update = false;
						}
						if (update) {
							OleDbHelper.Update(OleDbHelper.TransForm_Setting_Parameters, OleDbHelper.TransFormer_Table_Name, (int)OleDbHelper.File_Name_e._1ItsUnitName, (int)OleDbHelper.File_Name_e._2TransFormName);
						}
					}
					#endregion
					#region 每次加载时更新 测试分接位 的总数 并 选中上一次测试位置
					if (测试设置窗口 == null) {
						测试设置窗口 = new TestSetupWindow(TestStage_Update);
						测试设置窗口.DataContext = test_parameter;
						#region 每次加载时更新 测试分接位 的总数 并 选中上一次测试位置
						测试设置窗口.cmbOneCurTap.Items.Clear();
						if (transformer_parameter._24SwitchStartWorkingPosition != null && transformer_parameter._25SwitchStopWorkingPosition != null) {
							for (int i = int.Parse(transformer_parameter._24SwitchStartWorkingPosition); i <= int.Parse(transformer_parameter._25SwitchStopWorkingPosition); i++) {
								测试设置窗口.cmbOneCurTap.Items.Add(i);
								测试设置窗口.cmbOneCurTap.SelectedItem = null;
							}
						}

						#endregion
					}
					#endregion
					TreeViewUpdate(3);
				}
			}
			#endregion


			#region 测试设置窗口Combox SelectChange 响应
			if (sender.GetType() == typeof(ComboBox)) {
				if ((sender as ComboBox).Name == "cmbTransformerSelect") {
					if (测试设置窗口.cmbTransformerSelect.SelectedItem == null) {
						return;
					}
					test_parameter._1curTransformerName = 测试设置窗口.cmbTransformerSelect.SelectedItem.ToString();
					get_access_info(test_parameter._1curTransformerName);
					#region 每次加载时更新 测试分接位 的总数
					测试设置窗口.cmbOneCurTap.Items.Clear();
					if (transformer_parameter._24SwitchStartWorkingPosition != null && transformer_parameter._25SwitchStopWorkingPosition != null) {
						for (int i = int.Parse(transformer_parameter._24SwitchStartWorkingPosition); i <= int.Parse(transformer_parameter._25SwitchStopWorkingPosition); i++) {
							测试设置窗口.cmbOneCurTap.Items.Add(i);
							测试设置窗口.cmbOneCurTap.SelectedItem = null;
						}
					}
					#endregion
					get_Test_information(test_parameter._1curTransformerName);
					transformer_parameter._2TransformerName = test_parameter._1curTransformerName;
				}
			}
			#endregion

			ListViewUpdate();
			#region 数据入库 开始测试
			if (sender.GetType() == typeof(Button)) {
				Button btn = sender as Button;
				#region 按钮 保存并开始测试
				if (btn.Name == "btnTestParaConfirm") {
					if (TCP连接窗口 == null || !TCP连接窗口.is_Client_create_success) {
						UI_logic();
						Get_StateTimer.Stop();
						return;
					}
					test_parameter._5AutoContinuousMeasurementCurTap = 测试设置窗口.tbContinuousTestCurTap.Text;
					lb_switch_list.Items.Clear();
					test_parameter._47Test_Date = System.DateTime.Now.ToString("yyyy-MM-dd");
					//下发参数
					try {

						#region 线程 初始化
						if (测试设置窗口.cb_AutoPause.IsChecked == true) {
							th_read = new Thread(thread_Fun_从初始队列读取数据到新队列);
							th_analysis = new Thread(thread_Fun_List出队数据分析);
							th_tolist = new Thread(thread_Fun_新队列转数据为List);
							th_tolist.IsBackground = true;
							th_read.IsBackground = true;
							th_analysis.IsBackground = true;
						}
						else {
							th_analysis = new Thread(thread_Fun绘图);
							th_read = new Thread(thread_Fun_实时绘图);
							th_read.IsBackground = true;
							th_analysis.IsBackground = true;
						}

						#endregion

						#region 去空
						if (测试设置窗口.cmbDeviceACSampleFrequency.SelectedItem == null) {
							MessageBox.Show("请设置交流采样率");
							测试设置窗口.cmbDeviceACSampleFrequency.IsDropDownOpen = true;
							测试设置窗口.cmbDeviceACSampleFrequency.Focus();
							return;
						}
						if (测试设置窗口.cmbCompanySelect.SelectedItem == null) {
							MessageBox.Show("请选择变压器所属单位");
							测试设置窗口.cmbCompanySelect.IsDropDownOpen = true;
							测试设置窗口.cmbCompanySelect.Focus();
							return;
						}
						if (测试设置窗口.cmbTransformerSelect.SelectedItem == null) {

							MessageBox.Show("请选择变压器");
							测试设置窗口.cmbTransformerSelect.IsDropDownOpen = true;
							测试设置窗口.cmbTransformerSelect.Focus();
							return;
						}
						#endregion

						#region 确定直流或者交流测试
						if ((bool)测试设置窗口.rbAC.IsChecked) {
							test_parameter._24IsACMeasurment = true;
							test_parameter._25IsDCMeasurment = false;
						}
						if ((bool)测试设置窗口.rbDC.IsChecked) {
							test_parameter._25IsDCMeasurment = true;
							test_parameter._24IsACMeasurment = false;
						}
						if ((bool)测试设置窗口.rbBackSwitch.IsChecked) {
							test_parameter._9SinglePointMeasurementBackSwitch = true;
							test_parameter._8SinglePointMeasurementForwardSwitch = false;
						}
						if ((bool)测试设置窗口.rbForwardSwitch.IsChecked) {
							test_parameter._9SinglePointMeasurementBackSwitch = false;
							test_parameter._8SinglePointMeasurementForwardSwitch = true;
						}
						#endregion

						#region 数据结构初始化
						test_parameter._17SampleFrequency_DC = 测试设置窗口.tbDeviceSampleFrequency_DC.Text;
						test_parameter._4SampleFrequency = 测试设置窗口.cmbDeviceACSampleFrequency.SelectedItem.ToString();
						int 采样率 = int.Parse(test_parameter._4SampleFrequency);
						if (采样率 == 100) {
							采样率 = 50;
						}
						if (采样率 == 500) {
							采样率 = 200;
						}
						Page_Max_count = (int)(采样率 * 3);
						if (test_parameter._25IsDCMeasurment) {
							Page_Max_count = 60;
							test_parameter._17SampleFrequency_DC = "20";
						}
						Test_Condition_Initial(Page_Max_count);
						#endregion

						#region 分接位记录
						if (test_parameter._24IsACMeasurment) {
							//交流连续自动测试
							if (test_parameter._14isAutoContinuousMearsurment) {
								if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
								}

							}
							//交流单点测试
							else if (test_parameter._15isHandleSingleMearsurment) {
								if (test_parameter._8SinglePointMeasurementForwardSwitch) {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) - 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "交流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) + 1).ToString() + "]";
								}
							}
						}
						else if (test_parameter._25IsDCMeasurment) {
							//直流连续自动测试
							if (test_parameter._14isAutoContinuousMearsurment) {
								if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
								}

							}
							//直流单点测试
							else if (test_parameter._15isHandleSingleMearsurment) {
								if (test_parameter._8SinglePointMeasurementForwardSwitch) {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) - 1).ToString() + "]";
								}
								else {
									test_parameter._48Access_position = "直流_分接位[" + test_parameter._7SinglePointMeasurementCurTap + "]→[" + (int.Parse(test_parameter._7SinglePointMeasurementCurTap) + 1).ToString() + "]";
								}
							}
						}
						#endregion

						#region 缓存处理

						#endregion

						int Test_count = 0;
						//如果 上一次测试文件夹里只有原始数据 那么删除上次测试
						DirectoryInfo di = new DirectoryInfo(test_data_path + 上一次测试的变压器所属单位 + "\\" + 上一次测试的变压器 + "\\" + test_parameter._47Test_Date + "\\" + 上一次测试分接位);
						try {
							DirectoryInfo[] directory_list = di.GetDirectories();
							for (int j = 0; j < directory_list.Length; j++) {
								int digital = num(directory_list[j].ToString());
								if (digital > Test_count) {
									Test_count = digital;
								}
							}
							DirectoryInfo huancun = new DirectoryInfo(test_data_path + 上一次测试的变压器所属单位 + "\\" + 上一次测试的变压器 + "\\" + test_parameter._47Test_Date + "\\" + 上一次测试分接位 + "\\" + "第" + Test_count + "次测试");
							FileInfo[] huancun_list = huancun.GetFiles();
							//如果没有保存分析结果 那么就删除 测试结果
							if (huancun_list.Length <= 3 && huancun_list[0].ToString().Contains("原始")) {
								Directory.Delete(test_data_path + 上一次测试的变压器所属单位 + "\\" + 上一次测试的变压器 + "\\" + test_parameter._47Test_Date + "\\" + 上一次测试分接位 + "\\" + "第" + Test_count + "次测试", true);
							}
							else if (huancun_list.Length > 3) {
								Directory.Delete(test_data_path + 上一次测试的变压器所属单位 + "\\" + 上一次测试的变压器 + "\\" + test_parameter._47Test_Date + "\\" + 上一次测试分接位 + "\\" + "第" + Test_count + "次测试", true);
							}
						}
						catch {
							//tbErrorExpection.Text = "配置信息消失了!\r\n请添加变压器!";
							// MessageBox.Show("清空缓存错误:" + err.Message);
						}
						DirectoryInfo di1 = new DirectoryInfo(test_data_path + transformer_parameter._1ItsUnitName + "\\" + test_parameter._1curTransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position);
						try {
							Test_count = 0;
							DirectoryInfo[] directory_list = di1.GetDirectories();
							for (int j = 0; j < directory_list.Length; j++) {
								int digital = num(directory_list[j].ToString());
								if (digital > Test_count) {
									Test_count = digital;
								}
							}
							test_parameter._49Mesurent_Counts = "第" + (Test_count + 1) + "次测试";
						}
						catch {
							test_parameter._49Mesurent_Counts = "第" + (Test_count + 1) + "次测试";
						}
						#region 更新标题 和TreeView
						Tchart1.Header.Text = "当前测试波形:" + test_parameter._48Access_position;
						Tchart1.Header.Color = Colors.Blue;
						Tchart1.Header.Font.Bold = true;
						Tchart1.Header.Font.Size = 15;
						TreeViewUpdate(5);
						#endregion

						#region 初始化 CheckBox
						if (transformer_parameter._6Transformerphase == "单相") {
							cbAp.IsEnabled = false;
							cbBp.IsEnabled = false;
							cbAp_Chart1.IsEnabled = false;
							cbBp_Chart1.IsEnabled = false;
							cbAp_Chart2.IsEnabled = false;
							cbBp_Chart2.IsEnabled = false;
						}
						else {
							cbAp.IsEnabled = true;
							cbBp.IsEnabled = true;
							cbAp_Chart1.IsEnabled = true;
							cbBp_Chart1.IsEnabled = true;
							cbAp_Chart2.IsEnabled = true;
							cbBp_Chart2.IsEnabled = true;
						}
						#endregion


						#region 发送命令 线程执行 开始测试
						if (TCP连接窗口.is_Client_create_success) {
							load = new LoadingWindow();
							if (CMD_Send(GenerateCMD())) {
								测试设置窗口.Hide();
								load.start = send;
								load.Owner = GZDLMainWindow;
								if (test_parameter._25IsDCMeasurment) {
									load.lab_电压.Content = "";
									load.lab_单位.Content = "";
									load.lab_电压值.Content = "";
								}
								else {
									if (测试设置窗口.rbExternalPower.IsChecked == true) {
										load.lab_电压.Content = "";
										load.lab_单位.Content = "";
										load.lab_电压值.Content = "";
									}
								}
								load.Show();
								if (测试设置窗口.cb_AutoPause.IsChecked == true) {
									Draw_line_Timer.Start();
									th_read.Start();
									th_analysis.Start();
									th_tolist.Start();
									测试设置窗口.cmbInnerSupplyVoltage.IsEnabled = false;
								}
								else {
									th_analysis.Start();
									th_read.Start();

								}
								//cmbCurrentTestData.IsEnabled = false;
								btnPauseTest.IsEnabled = true;
								btnStopTest.IsEnabled = true;
								btnSartTest.IsEnabled = false;
								btnSystemSetting.IsEnabled = false;
								btnTransformerConfig.IsEnabled = false;
								上一次测试分接位 = test_parameter._48Access_position;
								gpTree.IsEnabled = false;

							}
						}
						#endregion
					}
					catch (Exception error) {
						// cmbCurrentTestData.IsEnabled = true;
						btnPauseTest.IsEnabled = false;
						btnSartTest.IsEnabled = false;
						btnSystemSetting.IsEnabled = true;
						btnTransformerConfig.IsEnabled = true;
						CMD_Send(Commander._3_CMD_STOPMEASURE);
						MessageBox.Show("开始测试错误:" + error.Message);
					}
					#region 测试参数更新至数据库
					try {

						#region 数据库测试参数赋值
						if (测试设置窗口.cmbInnerSupplyVoltage.SelectedItem != null) {
							test_parameter._2OutputVolt = 测试设置窗口.cmbInnerSupplyVoltage.SelectedItem.ToString();
						}
						test_parameter._3OutputVoltFrequency = "50Hz";
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										OleDbHelper.Test_File_Name_e._00CompanyName, transformer_parameter._1ItsUnitName);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										OleDbHelper.Test_File_Name_e._0curTransformerName, test_parameter._1curTransformerName);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
							OleDbHelper.Test_File_Name_e._1OutputVolt, test_parameter._2OutputVolt);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
							OleDbHelper.Test_File_Name_e._2OutputVoltFrequency, test_parameter._3OutputVoltFrequency);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
							OleDbHelper.Test_File_Name_e._3SampleFrequency, test_parameter._4SampleFrequency);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap, test_parameter._5AutoContinuousMeasurementCurTap);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._5AutoContinuousMeasurementEndTap, test_parameter._6AutoContinuousMeasurementEndTap);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
							OleDbHelper.Test_File_Name_e._6SinglePointMeasurementCurTap, test_parameter._7SinglePointMeasurementCurTap);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._7SinglePointMeasurementForwardSwitch, test_parameter._8SinglePointMeasurementForwardSwitch);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._8SinglePointMeasurementBackSwitch, test_parameter._9SinglePointMeasurementBackSwitch);

						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._14isAutoContinuousMearsurment, test_parameter._14isAutoContinuousMearsurment);

						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._15isHandleSingleMearsurment, test_parameter._15isHandleSingleMearsurment);

						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._20EnableDCfilter_DC, test_parameter._20EnableDCfilter_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
							OleDbHelper.Test_File_Name_e._21DisableDCfilter_DC, test_parameter._21DisableDCfilter_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._22IsInnernalPower, test_parameter._22IsInnernalPower);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._23IsExternalPower, test_parameter._23IsExternalPower);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._26MutationRation_DC, test_parameter._26MutationRation_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._27MutationRation_AC, test_parameter._27MutationRation_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._28ErrorRation_DC, test_parameter._28ErrorRation_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._29ErrorRation_AC, test_parameter._29ErrorRation_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._30MinChangeTime_DC, test_parameter._30MinChangeTime_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._31MinChangeTime_AC, test_parameter._31MinChangeTime_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._32MaxConstantTime_DC, test_parameter._32MaxConstantTime_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._33MaxConstantTime_AC, test_parameter._33MaxConstantTime_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._34IgnoreTime_DC, test_parameter._34IgnoreTime_DC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								  OleDbHelper.Test_File_Name_e._35IgnoreTime_AC, test_parameter._35IgnoreTime_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										OleDbHelper.Test_File_Name_e._36IsAutoAnalysisParameterSet_AC, test_parameter._36IsAutoAnalysisParameterSet_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										OleDbHelper.Test_File_Name_e._38IsHandleAnalysisParameterSet_AC, test_parameter._38IsHandleAnalysisParameterSet_AC);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e.Measurment, test_parameter.Measurment);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
									   OleDbHelper.Test_File_Name_e._40Cursor_A1, test_parameter._40Cursor_A1 == null ? "0" : test_parameter._40Cursor_A1);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											   OleDbHelper.Test_File_Name_e._41Cursor_A2, test_parameter._41Cursor_A2 == null ? "0" : test_parameter._41Cursor_A2);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											   OleDbHelper.Test_File_Name_e._42Cursor_B1, test_parameter._42Cursor_B1 == null ? "0" : test_parameter._42Cursor_B1);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											   OleDbHelper.Test_File_Name_e._43Cursor_B2, test_parameter._43Cursor_B2 == null ? "0" : test_parameter._43Cursor_B2);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											   OleDbHelper.Test_File_Name_e._44Cursor_C1, test_parameter._44Cursor_C1 == null ? "0" : test_parameter._44Cursor_C1);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											   OleDbHelper.Test_File_Name_e._45Cursor_C2, test_parameter._45Cursor_C2 == null ? "0" : test_parameter._45Cursor_C2);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											   OleDbHelper.Test_File_Name_e._46Peak_value, test_parameter._46Peak_value == null ? "0" : test_parameter._46Peak_value);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
									 OleDbHelper.Test_File_Name_e._47Test_Date, test_parameter._47Test_Date);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
													  OleDbHelper.Test_File_Name_e._48Access_position, test_parameter._48Access_position);
						OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
													  OleDbHelper.Test_File_Name_e._49Mesurent_Counts, test_parameter._49Mesurent_Counts);
						#endregion


						if (!OleDbHelper.Select(OleDbHelper.Test_File_Name_e._00CompanyName, transformer_parameter._1ItsUnitName)) {
							OleDbHelper.Insert(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name);
						}
						else {
							if (!OleDbHelper.Select(OleDbHelper.Test_File_Name_e._0curTransformerName, transformer_parameter._1ItsUnitName, test_parameter._1curTransformerName)) {
								OleDbHelper.Insert(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name);
							}
							else {
								OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
							}
						}
					}
					catch (Exception oldc_error) {
						tbErrorExpection.Text = "测试信息写入数据库出错!\r\n"+oldc_error.Message;
					}
					#endregion
					return;
				}
				#endregion
				else if (btn.Name == "btn_Analysis_Confirm") {
					return;
				}
			}
			#endregion
		}
		#endregion

		#region 生成命令包并发送命令
		//发送
		public bool CMD_Send(byte[] cmd) {
			int success_send_length = TCP连接窗口.client_message_send(cmd);
			if (success_send_length > 0) {
				return true;
			}
			else {
				UI_logic();
			}
			return false;
		}
		//加载窗口
		void send(LoadingWindow load) {
			GenerateCMD(load);
		}
		void send_repeat(LoadingWindow load) {

			for (int i = 1; i <= 100; i++) {
				load.bgMeet.ReportProgress(i, "");
				Thread.Sleep(50);
			}

		}
		void end_wiat(LoadingWindow load) {
			CMD_Send(Commander._7_CMD_RECALL);
			是否第一次收到数据 = true;
			need_juage = true;
			is_trig = true;
		}
		//加载窗口模式 
		void GenerateCMD(LoadingWindow load) {
			for (int i = 1; i <= 100; i++) {
				load.bgMeet.ReportProgress(i, "指令:测试方式 值:" + test_parameter._24IsACMeasurment);
				Thread.Sleep(50);
			}
		}
		//生成下发命令数据包
		byte[] GenerateCMD() {
			byte[] data = new byte[200];
			byte[] temp;
			temp = BitConverter.GetBytes(0x80000002);
			int index = 0;
			temp.CopyTo(data, index);

			//测量方式
			if (test_parameter._24IsACMeasurment) {
				index += temp.Length;
				temp = BitConverter.GetBytes(0);
				temp.CopyTo(data, index);

			}
			else {
				index += temp.Length;
				temp = BitConverter.GetBytes(1);
				temp.CopyTo(data, index);
			}

			//自动连续测量还是单点测量
			if (test_parameter._18isAutoContinuousMearsurment_DC) {
				index += temp.Length;
				temp = BitConverter.GetBytes(1);
				temp.CopyTo(data, index);
			}
			else {
				index += temp.Length;
				temp = BitConverter.GetBytes(0);
				temp.CopyTo(data, index);
			}

			//连续测量当前
			index += temp.Length;
			temp = BitConverter.GetBytes(int.Parse(test_parameter._5AutoContinuousMeasurementCurTap));
			temp.CopyTo(data, index);

			//连续测量结束
			index += temp.Length;
			temp = BitConverter.GetBytes(int.Parse(test_parameter._6AutoContinuousMeasurementEndTap));
			temp.CopyTo(data, index);

			//交流采样率
			index += temp.Length;
			int 采样率 = int.Parse(test_parameter._4SampleFrequency);
			if (采样率 == 100) {
				采样率 = 50;
			}
			if (采样率 == 500) {
				采样率 = 200;
			}
			temp = BitConverter.GetBytes(采样率 * 1000);
			temp.CopyTo(data, index);

			//输出频率
			index += temp.Length;
			temp = BitConverter.GetBytes(50);
			temp.CopyTo(data, index);

			//交流突变比例
			index += temp.Length;
			try {
				temp = BitConverter.GetBytes(float.Parse(test_parameter._27MutationRation_AC));
				temp.CopyTo(data, index);

				//交流误差比例
				index += temp.Length;
				temp = BitConverter.GetBytes(float.Parse(test_parameter._29ErrorRation_AC == null ? "2.0" : test_parameter._29ErrorRation_AC));
				temp.CopyTo(data, index);

				//交流变化区域忽略时间
				index += temp.Length;
				temp = BitConverter.GetBytes(80);
				temp.CopyTo(data, index);

				//交流最小持续变化时间
				index += temp.Length;
				temp = BitConverter.GetBytes(float.Parse(test_parameter._31MinChangeTime_AC));
				temp.CopyTo(data, index);

				//交流最小持续不变时间
				index += temp.Length;
				temp = BitConverter.GetBytes(float.Parse(test_parameter._33MaxConstantTime_AC));
				temp.CopyTo(data, index);

				//电源板交流输出电压
				index += temp.Length;
				temp = BitConverter.GetBytes(int.Parse(test_parameter._2OutputVolt));
				temp.CopyTo(data, index);

				//直流采样率
				index += temp.Length;
				temp = BitConverter.GetBytes(int.Parse(test_parameter._17SampleFrequency_DC));
				temp.CopyTo(data, index);

				//直流测试滤波开关
				if (test_parameter._20EnableDCfilter_DC) {
					index += temp.Length;
					temp = BitConverter.GetBytes(1);
					temp.CopyTo(data, index);
				}
				else {
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
				}

				//直流突变比例
				index += temp.Length;
				temp = BitConverter.GetBytes((int)float.Parse(test_parameter._26MutationRation_DC));
				temp.CopyTo(data, index);

				//直流误差比例
				index += temp.Length;
				temp = BitConverter.GetBytes((int)float.Parse(test_parameter._28ErrorRation_DC));
				temp.CopyTo(data, index);

				//直流最大持续变化时间
				index += temp.Length;
				temp = BitConverter.GetBytes((int)float.Parse(test_parameter._32MaxConstantTime_DC));
				temp.CopyTo(data, index);
				//直流最小持续不变时间
				index += temp.Length;
				temp = BitConverter.GetBytes((int)float.Parse(test_parameter._30MinChangeTime_DC));
				temp.CopyTo(data, index);
				//直流测试档位
				switch (test_parameter._16MeasureGear_DC) {
					case "0--20Ω":
						index += temp.Length;
						temp = BitConverter.GetBytes(0);
						temp.CopyTo(data, index);
						break;
					case "20--100Ω":
						index += temp.Length;
						temp = BitConverter.GetBytes(1);
						temp.CopyTo(data, index);
						break;
					default:
						index += temp.Length;
						temp = BitConverter.GetBytes(0);
						temp.CopyTo(data, index);
						break;
				}
			}
			catch (Exception error) {
				tbErrorExpection.Text = "命令生成错误:" + error.Message;
				//MessageBox.Show("命令生成错误:" + error.Message);
			}
			//变压器相数
			switch (transformer_parameter._6Transformerphase) {
				case "单相":
					index += temp.Length;
					temp = BitConverter.GetBytes(1);
					temp.CopyTo(data, index);
					break;
				case "三相":
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
					break;
				default:
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
					break;
			}
			//变压器绕组数
			switch (transformer_parameter._9TransformerWinding) {
				case "三绕组":
					index += temp.Length;
					temp = BitConverter.GetBytes(1);
					temp.CopyTo(data, index);
					break;
				case "双绕组":
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
					break;
				default:
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
					break;
			}
			//变压器绕组连接方式
			if (test_parameter._25IsDCMeasurment) {
				switch (transformer_parameter._13TransformerWindingConnMethod) {
					case "YN型接法":
						index += temp.Length;
						temp = BitConverter.GetBytes(1);
						temp.CopyTo(data, index);
						break;
					default:
						MessageBox.Show("注:直流测试只能使用 YN型接线法,系统已默认使用YN型接法");
						index += temp.Length;
						temp = BitConverter.GetBytes(1);
						temp.CopyTo(data, index);
						break;
				}
			}
			else {
				switch (transformer_parameter._13TransformerWindingConnMethod) {
					case "Y型接法":
						index += temp.Length;
						temp = BitConverter.GetBytes(0);
						temp.CopyTo(data, index);
						break;
					case "YN型接法":
						index += temp.Length;
						temp = BitConverter.GetBytes(1);
						temp.CopyTo(data, index);
						break;
					case "三角形接法":
						index += temp.Length;
						temp = BitConverter.GetBytes(2);
						temp.CopyTo(data, index);
						break;
					default:
						index += temp.Length;
						temp = BitConverter.GetBytes(0);
						temp.CopyTo(data, index);
						break;
				}
			}

			//变压器开关分裂数
			switch (transformer_parameter._23SwitchColumnCount) {
				case "单列":
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
					break;
				case "双列":
					index += temp.Length;
					temp = BitConverter.GetBytes(1);
					temp.CopyTo(data, index);
					break;
				case "三列":
					index += temp.Length;
					temp = BitConverter.GetBytes(2);
					temp.CopyTo(data, index);
					break;
				default:
					index += temp.Length;
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
					break;
			}
			//开关工作起始位置
			index += temp.Length;
			temp = BitConverter.GetBytes(int.Parse(transformer_parameter._24SwitchStartWorkingPosition));
			temp.CopyTo(data, index);
			//开关工作结束为止
			index += temp.Length;
			temp = BitConverter.GetBytes(int.Parse(transformer_parameter._25SwitchStopWorkingPosition));
			temp.CopyTo(data, index);
			//开关工作中间位置
			index += temp.Length;
			temp = BitConverter.GetBytes(int.Parse(transformer_parameter._26SwitchMidPosition));
			temp.CopyTo(data, index);
			//DSP采样点数
			index += temp.Length;
			temp = BitConverter.GetBytes(400);
			temp.CopyTo(data, index);
			//内部或者外部电源
			index += temp.Length;
			if (test_parameter._24IsACMeasurment) {
				if (test_parameter._22IsInnernalPower) {
					temp = BitConverter.GetBytes(0);
					temp.CopyTo(data, index);
				}
				if (test_parameter._23IsExternalPower) {
					temp = BitConverter.GetBytes(1);
					temp.CopyTo(data, index);
				}
			}
			else {
				temp = BitConverter.GetBytes(0);
				temp.CopyTo(data, index);
			}

			return data;
		}
		#endregion

		#region 接受数据 保存
		void save_data(string Header, string file_name, List<int>[] data) {
			StringBuilder sb = new StringBuilder();
			sb.Append(Header + "\r\n");
			sb.Append("Data:\r\n");
			int LastData = 0;
			for (int i = 0; i < data.Length; i++) {
				List<int> data_points = data[i];
				for (int j = 0; j < data_points.Count; j++) {

					#region 滤波
					//不是触发数据

					if (data_points[j] <= int.Parse(test_parameter._46Peak_value) + 15000 && data_points[j] >= -(int.Parse(test_parameter._46Peak_value) + 15000)) {
						//如果上次数据为0  那么赋值
						if (LastData == 0) {
							LastData = data_points[j];
							if (j % 10 == 0) {
								sb.Append(data_points[j] + ";\r\n");
							}
							else {
								sb.Append(data_points[j] + ";");
							}
						}
						//如果不为0 那么判断
						else {
							if (data_points[j] - LastData >= int.Parse(test_parameter._46Peak_value) / 20 || data_points[j] - LastData <= -int.Parse(test_parameter._46Peak_value) / 20) {
								data_points[j] = LastData;
								if (j < data_points.Count - 2) {
									LastData = data_points[j + 1];
									if (LastData - data_points[j] >= int.Parse(test_parameter._46Peak_value) / 20 || LastData - data_points[j] <= -int.Parse(test_parameter._46Peak_value) / 20) {
										LastData = data_points[j + 2];
									}
								}
								if (j % 10 == 0) {
									sb.Append(data_points[j] + ";\r\n");
								}
								else {
									sb.Append(data_points[j] + ";");
								}
							}
							else {
								LastData = data_points[j];
								if (j % 10 == 0) {
									sb.Append(data_points[j] + ";\r\n");
								}
								else {
									sb.Append(data_points[j] + ";");
								}
							}
						}
					}
					else {
						if (j < data_points.Count - 1) {
							if (data_points[j + 1] <= int.Parse(test_parameter._46Peak_value) + 15000 && data_points[j + 1] >= -(int.Parse(test_parameter._46Peak_value) + 15000)) {
								data_points[j] = data_points[j + 1];
							}
							else {
								LastData = 0;
								if (j % 10 == 0) {
									sb.Append(data_points[j] + ";\r\n");
								}
								else {
									sb.Append(data_points[j] + ";");
								}
							}
						}
						else {
							LastData = 0;
							if (j % 10 == 0) {
								sb.Append(data_points[j] + ";\r\n");
							}
							else {
								sb.Append(data_points[j] + ";");
							}
						}

					}
					#endregion
				}
			}
			string path1 = test_data_path + transformer_parameter._1ItsUnitName;
			string path2 = path1 + "\\" + test_parameter._1curTransformerName;
			string path3 = path2 + "\\" + test_parameter._47Test_Date;
			string path4 = path3 + "\\" + test_parameter._48Access_position;
			string path5 = path4 + "\\" + test_parameter._49Mesurent_Counts;
			上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
			上一次测试的变压器 = test_parameter._1curTransformerName;
			if (!Directory.Exists(path1)) {
				Directory.CreateDirectory(path1);
			}
			if (!Directory.Exists(path2)) {
				Directory.CreateDirectory(path2);
			}
			if (!Directory.Exists(path3)) {
				Directory.CreateDirectory(path3);
			}
			if (!Directory.Exists(path4)) {
				Directory.CreateDirectory(path4);
			}
			if (!Directory.Exists(path5)) {
				Directory.CreateDirectory(path5);
			}
			FileHelper.SaveFile_Create(path5 + "\\" + file_name + " ", sb.ToString(), sb.Length);
			Allow_Mouse_In = true;
			Mouse.OverrideCursor = Cursors.Hand;
		}
		//保存文件  随后进行分析
		void save_data(string file_name, byte[] data) {
			string path5 = test_data_path + transformer_parameter._1ItsUnitName + "\\" + test_parameter._1curTransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts;
			上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
			上一次测试的变压器 = test_parameter._1curTransformerName;
			// 上一次测试分接位 = test_parameter._48Access_position;
			if (!Directory.Exists(path5)) {
				Directory.CreateDirectory(path5);
			}
			FileHelper.SaveFile_Append(path5 + "\\" + file_name, data);
		}
		bool is_无效变化点 = false;

		void save_data(string Header, string file_name, List<int> data, int 开始保存位置) {
			if (开始保存位置 < 0) {
				开始保存位置 = 0;
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(Header + "\r\n");
			sb.Append("Data:\r\n");
			for (int i = 开始保存位置; i < data.Count; i++) {
				if (i % 10 == 0) {
					sb.Append(data[i] + ";\r\n");
				}
				else {
					sb.Append(data[i] + ";");
				}
			}
			string path1 = test_data_path + transformer_parameter._1ItsUnitName;
			string path2 = path1 + "\\" + test_parameter._1curTransformerName;
			string path3 = path2 + "\\" + test_parameter._47Test_Date;
			string path4 = path3 + "\\" + test_parameter._48Access_position;
			string path5 = path4 + "\\" + test_parameter._49Mesurent_Counts;
			上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
			上一次测试的变压器 = test_parameter._1curTransformerName;
			if (!Directory.Exists(path5)) {
				Directory.CreateDirectory(path5);
			}
			FileHelper.SaveFile_Create(path5 + "\\" + file_name, sb.ToString(), sb.Length);
			Allow_Mouse_In = true;
			Mouse.OverrideCursor = Cursors.Hand;
		}
		string create_save_header() {
			StringBuilder sb = new StringBuilder();
			sb.Append("Header:[TransFormerParameter]\r\n");
			sb.Append(transformer_parameter._1ItsUnitName + ";");//0
			sb.Append(transformer_parameter._2TransformerName + ";");
			sb.Append(transformer_parameter._3TransformerModel + ";");
			sb.Append(transformer_parameter._4Single_phase + ";");
			sb.Append(transformer_parameter._5Thrid_phase + ";");
			sb.Append(transformer_parameter._6Transformerphase + ";");
			sb.Append(transformer_parameter._7Double_Winding + ";");
			sb.Append(transformer_parameter._8Three_Winding + ";");
			sb.Append(transformer_parameter._9TransformerWinding + ";");
			sb.Append(transformer_parameter._10Y_method + ";");
			sb.Append(transformer_parameter._11YN_method + ";");
			sb.Append(transformer_parameter._12Triangle_method + ";");
			sb.Append(transformer_parameter._13TransformerWindingConnMethod + ";");
			sb.Append(transformer_parameter._20SwitchManufactorName + ";");
			sb.Append(transformer_parameter._21SwitchModel + ";");
			sb.Append(transformer_parameter._22SwitchCode + ";");
			sb.Append(transformer_parameter._23SwitchColumnCount + ";");
			sb.Append(transformer_parameter._24SwitchStartWorkingPosition + ";");
			sb.Append(transformer_parameter._25SwitchStopWorkingPosition + ";");
			sb.Append(transformer_parameter._26SwitchMidPosition + ";");
			sb.Append(transformer_parameter._27SwitchColumn_One_Count + ";");
			sb.Append(transformer_parameter._28SwitchColumn_Two_Count + ";");
			sb.Append(transformer_parameter._29SwitchColumn_Three_Count + ";");//22

			sb.Append("\r\nHeader:[TestParameter]\r\n");
			sb.Append(transformer_parameter._1ItsUnitName + ";");//23
			sb.Append(test_parameter._1curTransformerName + ";");
			sb.Append(test_parameter._2OutputVolt + ";");
			sb.Append(test_parameter._3OutputVoltFrequency + ";");
			sb.Append(test_parameter._4SampleFrequency + ";");
			sb.Append(test_parameter._5AutoContinuousMeasurementCurTap + ";");
			sb.Append(test_parameter._6AutoContinuousMeasurementEndTap + ";");
			sb.Append(test_parameter._7SinglePointMeasurementCurTap + ";");
			sb.Append(test_parameter._8SinglePointMeasurementForwardSwitch + ";");
			sb.Append(test_parameter._9SinglePointMeasurementBackSwitch + ";");
			sb.Append(test_parameter._14isAutoContinuousMearsurment + ";");
			sb.Append(test_parameter._15isHandleSingleMearsurment + ";");
			sb.Append(test_parameter._16MeasureGear_DC + ";");
			sb.Append(test_parameter._17SampleFrequency_DC + ";");
			sb.Append(test_parameter._18isAutoContinuousMearsurment_DC + ";");
			sb.Append(test_parameter._19isHandleSingleMearsurment_DC + ";");
			sb.Append(test_parameter._20EnableDCfilter_DC + ";");
			sb.Append(test_parameter._21DisableDCfilter_DC + ";");
			sb.Append(test_parameter._22IsInnernalPower + ";");
			sb.Append(test_parameter._23IsExternalPower + ";");
			sb.Append(test_parameter._24IsACMeasurment + ";");//43
			sb.Append(test_parameter._25IsDCMeasurment + ";");//44
			sb.Append(test_parameter._26MutationRation_DC + ";");
			sb.Append(test_parameter._27MutationRation_AC + ";");
			sb.Append(test_parameter._28ErrorRation_DC + ";");
			sb.Append(test_parameter._29ErrorRation_AC + ";");
			sb.Append(test_parameter._30MinChangeTime_DC + ";");
			sb.Append(test_parameter._31MinChangeTime_AC + ";");
			sb.Append(test_parameter._32MaxConstantTime_DC + ";");
			sb.Append(test_parameter._33MaxConstantTime_AC + ";");
			sb.Append(test_parameter._34IgnoreTime_DC + ";");
			sb.Append(test_parameter._35IgnoreTime_AC + ";");
			sb.Append(test_parameter._36IsAutoAnalysisParameterSet_AC + ";");
			sb.Append(test_parameter._37IsAutoAnalysisParameterSet_DC + ";");
			sb.Append(test_parameter._38IsHandleAnalysisParameterSet_AC + ";");
			sb.Append(test_parameter._39IsHandleAnalysisParameterSet_DC + ";");
			sb.Append(test_parameter._40Cursor_A1 + ";");
			sb.Append(test_parameter._41Cursor_A2 + ";");
			sb.Append(test_parameter._42Cursor_B1 + ";");
			sb.Append(test_parameter._43Cursor_B2 + ";");
			sb.Append(test_parameter._44Cursor_C1 + ";");
			sb.Append(test_parameter._45Cursor_C2 + ";");
			sb.Append(test_parameter._46Peak_value + ";");
			sb.Append(test_parameter._47Test_Date + ";");
			sb.Append(test_parameter._48Access_position + ";");
			sb.Append(test_parameter._49Mesurent_Counts + ";");
			sb.Append(test_parameter._50Cursor_A1_DC + ";");
			sb.Append(test_parameter._51Cursor_A2_DC + ";");
			sb.Append(test_parameter._52Cursor_B1_DC + ";");
			sb.Append(test_parameter._53Cursor_B2_DC + ";");
			sb.Append(test_parameter._54Cursor_C1_DC + ";");
			sb.Append(test_parameter._55Cursor_C2_DC + ";");
			return sb.ToString();
		}
		#region 接受数据变量

		int page_count = 0;
		bool is_trig = false;
		int 丢包序号 = 0;
		bool 是否第一次收到数据 = true;
		bool need_juage = true;
		bool Allow_Mouse_In = true;
		Int32 serial_num = 0;
		Int32 channel_index = 0;
		bool is_开始接受 = true;
		#endregion
		void Tcp_message_receive_As_Sever(byte[] arrMsgRec) {
			//lock (monitor) {
				
				if (is_开始接受) {

					#region 逻辑触发
					if (是否第一次收到数据) {
						this.Dispatcher.Invoke(new Action(delegate {
							btnStopTest.IsEnabled = true;
							btnPauseTest.IsEnabled = true;
							btnSartTest.IsEnabled = false;
							btnSystemSetting.IsEnabled = false;
							btnTransformerConfig.IsEnabled = false;
							btnReTest.IsEnabled = true;
							btnDataAnalysis.IsEnabled = true;
							// btn_SaveTestData.IsEnabled = false;
							是否第一次收到数据 = false;
							//Allow_Mouse_In = false;
							#region 设置 测试时鼠标不能移动到绘图区域
							////获取控件位置
							//GeneralTransform generalTransform1 = Tchart1.TransformToAncestor(this);
							//Point currentPoint = generalTransform1.Transform(new Point(0, 0));
							//int locationX = (int)currentPoint.X;
							//int locationY = (int)currentPoint.Y;
							////获取控件长宽度
							//int width = (int)Tchart1.ActualWidth;
							//int hight = (int)Tchart1.ActualHeight;
							//POINT mousePoint = new POINT();
							//Tchart1.MouseMove += (MouseEventHandler)delegate
							//{
							//    if (!Allow_Mouse_In)
							//    {
							//        GetCursorPos(out mousePoint);
							//        //左边区域
							//        if (mousePoint.Y > locationY && mousePoint.Y < locationY + hight && mousePoint.X < locationX + 100)
							//        {
							//            if (mousePoint.X > locationX && mousePoint.X < locationX + width)
							//            {
							//                SetCursorPos(locationX, mousePoint.Y);
							//            }
							//            return;
							//        }
							//        //上面区域
							//        if (mousePoint.X > locationX && mousePoint.X < locationX + width && mousePoint.Y < locationY + 100)
							//        {
							//            if (mousePoint.Y > locationY)
							//            {
							//                SetCursorPos(mousePoint.X, locationY);
							//            }
							//            return;
							//        }
							//        //右边区域
							//        if (mousePoint.Y > locationY && mousePoint.Y < locationY + hight && mousePoint.X > locationX + 600)
							//        {
							//            if (mousePoint.X < locationX + width && mousePoint.X > locationX)
							//            {
							//                SetCursorPos((int)(locationX + width), mousePoint.Y);
							//            }
							//            return;
							//        }
							//        //下面区域
							//        if (mousePoint.X > locationX && mousePoint.X < locationX + width && mousePoint.Y > locationY + 300)
							//        {
							//            if (mousePoint.Y > locationY && mousePoint.Y < locationY + hight)
							//            {
							//                SetCursorPos(mousePoint.X, (int)(locationY + hight));
							//            }
							//            return;
							//        }
							//    }
							//};
							#endregion
						}));
					}
					#endregion
					//   need_juage = true;
					channel_index = BitConverter.ToInt32(arrMsgRec, 4);
					serial_num = BitConverter.ToInt32(arrMsgRec, 8);
					if (serial_num >= 0) {
						byte[] wavedata = new byte[1200];
						for (int i = 0; i < wavedata.Length; i++) {
							wavedata[i] = arrMsgRec[i + 12];
						}
						if (transformer_parameter._6Transformerphase == "三相") {
							switch (channel_index) {
								//A相
								case 0:
									 save_data("A相原始数据", wavedata);
									A_data.Enqueue(wavedata);
									break;
								case 1:
									 save_data("B相原始数据", wavedata);
									B_data.Enqueue(wavedata);
									break;
								case 2:
									 save_data("C相原始数据", wavedata);
									C_data.Enqueue(wavedata);
									page_count++;

									break;
								default: break;
							}
						}
						if (transformer_parameter._6Transformerphase == "单相") {
							switch (channel_index) {
								//A相
								case 0:
									break;
								case 1:
									break;
								case 2:
									save_data("C相原始数据", wavedata);
									C_data.Enqueue(wavedata);
									break;
								default: break;
							}
						}
						//this.Dispatcher.Invoke(new Action(delegate {

						//	#region  触发后自动停止接受数据
						//	if (测试设置窗口.cb_AutoPause.IsChecked == true) {
						//		if (transformer_parameter._6Transformerphase == "三相") {
						//			switch (channel_index) {
						//				//A相
						//				case 0:
						//					A_data.Enqueue(wavedata);
						//					break;
						//				case 1:
						//					B_data.Enqueue(wavedata);
						//					break;
						//				case 2:
						//					C_data.Enqueue(wavedata);
						//					page_count++;
						//					if (is_trig) {
						//						丢包序号 = serial_num;
						//						is_trig = false;
						//					}
						//					else {
						//						丢包序号 += 1;
						//					}
						//					if (丢包序号 != serial_num && need_juage == true) {
						//						is_trig = true;
						//						need_juage = false;
						//						丢包序号 = serial_num;
						//						this.Dispatcher.Invoke(new Action(delegate {
						//							lb_switch_list.Items.Add("丢包序号:" + 丢包序号);
						//						}));

						//					}
						//					this.Dispatcher.Invoke(new Action(delegate {
						//						lbData_SerialNum.Content = serial_num;
						//					}));

						//					break;
						//				default: break;
						//			}
						//		}
						//		if (transformer_parameter._6Transformerphase == "单相") {
						//			switch (channel_index) {
						//				//A相
						//				case 0:
						//					break;
						//				case 1:
						//					break;
						//				case 2:
						//					C_data.Enqueue(wavedata);
						//					page_count++;
						//					if (is_trig) {
						//						丢包序号 = serial_num;
						//						is_trig = false;
						//					}
						//					else {
						//						丢包序号 += 1;
						//					}
						//					if (丢包序号 != serial_num && need_juage == true) {
						//						is_trig = true;
						//						need_juage = false;
						//						丢包序号 = serial_num;
						//						this.Dispatcher.Invoke(new Action(delegate {
						//							lb_switch_list.Items.Add("丢包序号:" + 丢包序号);
						//						}));
						//					}
						//					this.Dispatcher.Invoke(new Action(delegate {
						//						lbData_SerialNum.Content = serial_num;
						//					}));

						//					break;
						//				default: break;
						//			}
						//		}
						//		if (page_count == (Page_Max_count)) {
						//			Fun_丢掉开头混乱数据_计算变化率_开始测试();
						//			page_count++;
						//		}
						//	}
						//	#endregion
						//	#region  触发后手动停止接受数据
						//	if (测试设置窗口.cb_AutoPause.IsChecked != true) {

						//	}
						//	#endregion
						//}));
					}
				}
				//Monitor.PulseAll(monitor);
			//}
			
		}
		void Tcp_message_receive_As_Client(byte[] client_message) {
			string header = "0x" + Convert.ToString(BitConverter.ToInt32(client_message, 0), 16);
			switch (header) {
				//获取波峰
				case "0x80000082":
					this.tblock_State.Dispatcher.Invoke(new Action(delegate {
						switch (BitConverter.ToInt32(client_message, 4)) {

							case (int)State._0STAT_OK:
								tblock_State.Foreground = Brushes.Green;
								tblock_State.Text = "启动成功";
								break;
							case (int)State._1STAT_DSPPOWERUP_FAIL:
								tblock_State.Text = "电源板升压失败";
								tblock_State.Foreground = Brushes.Red;
								CMD_Send(Commander._3_CMD_STOPMEASURE);
								Get_StateTimer.Stop();
								MessageBox.Show(tblock_State.Text, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
								break;
							case (int)State._2STAT_DSPFFGET_FAIL:
								tblock_State.Text = "获取峰值失败";
								tblock_State.Foreground = Brushes.Red;
								CMD_Send(Commander._3_CMD_STOPMEASURE);
								Get_StateTimer.Stop();
								if (load != null) {
									load.Close();
								}
								MessageBox.Show(tblock_State.Text, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
								break;
							case (int)State._8STAT_POWER_STARTUP:
								load.lab_电压值.Content = BitConverter.ToInt32(client_message, 8).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 8).ToString();
								break;
							case (int)State._7STAT_READY_FOR_MEASURE:
								load.lab_电压值.Content = BitConverter.ToInt32(client_message, 8).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 8).ToString();
								if (isPeakVale_initial) {
									test_parameter._46Peak_value = BitConverter.ToInt32(client_message, 12).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 12).ToString();
									isPeakVale_initial = false;
								}
								break;

							default:
								break;
						}
					}));

					break;
				//触发波形参数
				case "0x80000087":
					double rate = 3.0 / Page_Max_count;
					test_parameter._40Cursor_A1 = BitConverter.ToInt32(client_message, 4).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 4).ToString();
					test_parameter._41Cursor_A2 = BitConverter.ToInt32(client_message, 8).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 8).ToString();
					test_parameter._42Cursor_B1 = BitConverter.ToInt32(client_message, 12).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 12).ToString();
					test_parameter._43Cursor_B2 = BitConverter.ToInt32(client_message, 16).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 16).ToString();
					test_parameter._44Cursor_C1 = BitConverter.ToInt32(client_message, 20).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 20).ToString();
					test_parameter._45Cursor_C2 = BitConverter.ToInt32(client_message, 24).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 24).ToString();

					if (transformer_parameter._12Triangle_method) {
						test_parameter._50Cursor_A1_DC = BitConverter.ToInt32(client_message, 28).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 28).ToString();
						test_parameter._51Cursor_A2_DC = BitConverter.ToInt32(client_message, 32).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 32).ToString();
						test_parameter._52Cursor_B1_DC = BitConverter.ToInt32(client_message, 36).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 36).ToString();
						test_parameter._53Cursor_B2_DC = BitConverter.ToInt32(client_message, 40).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 40).ToString();
						test_parameter._54Cursor_C1_DC = BitConverter.ToInt32(client_message, 44).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 44).ToString();
						test_parameter._55Cursor_C2_DC = BitConverter.ToInt32(client_message, 48).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 48).ToString();
					}

					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
									  OleDbHelper.Test_File_Name_e._40Cursor_A1, test_parameter._40Cursor_A1 == null ? "0" : test_parameter._40Cursor_A1);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e._41Cursor_A2, test_parameter._41Cursor_A2 == null ? "0" : test_parameter._41Cursor_A2);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e._42Cursor_B1, test_parameter._42Cursor_B1 == null ? "0" : test_parameter._42Cursor_B1);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e._43Cursor_B2, test_parameter._43Cursor_B2 == null ? "0" : test_parameter._43Cursor_B2);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e._44Cursor_C1, test_parameter._44Cursor_C1 == null ? "0" : test_parameter._44Cursor_C1);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e._45Cursor_C2, test_parameter._45Cursor_C2 == null ? "0" : test_parameter._45Cursor_C2);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											  OleDbHelper.Test_File_Name_e._46Peak_value, test_parameter._46Peak_value == null ? "0" : test_parameter._46Peak_value);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
					OleDbHelper.Test_File_Name_e._50Cursor_A1_DC, test_parameter._50Cursor_A1_DC == null ? "0" : test_parameter._50Cursor_A1_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											OleDbHelper.Test_File_Name_e._51Cursor_A2_DC, test_parameter._51Cursor_A2_DC == null ? "0" : test_parameter._51Cursor_A2_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											OleDbHelper.Test_File_Name_e._52Cursor_B1_DC, test_parameter._52Cursor_B1_DC == null ? "0" : test_parameter._52Cursor_B1_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											OleDbHelper.Test_File_Name_e._53Cursor_B2_DC, test_parameter._53Cursor_B2_DC == null ? "0" : test_parameter._53Cursor_B2_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											OleDbHelper.Test_File_Name_e._54Cursor_C1_DC, test_parameter._54Cursor_C1_DC == null ? "0" : test_parameter._54Cursor_C1_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
											OleDbHelper.Test_File_Name_e._55Cursor_C2_DC, test_parameter._55Cursor_C2_DC == null ? "0" : test_parameter._55Cursor_C2_DC);
					if (!OleDbHelper.Select(OleDbHelper.Test_File_Name_e._00CompanyName, transformer_parameter._1ItsUnitName)) {
						OleDbHelper.Insert(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name);
					}
					else {
						if (!OleDbHelper.Select(OleDbHelper.Test_File_Name_e._0curTransformerName, test_parameter._1curTransformerName)) {
							OleDbHelper.Insert(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name);
						}
						else {
							OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
						}
					}
					if (test_parameter._25IsDCMeasurment) {
						CMD_Send(Commander.CMD_GETDCTRIGPOS);
					}
					break;
				case "0x8000008A":
				case "0x8000008a":
					double rate1 = 3.0 / Page_Max_count;
					test_parameter._50Cursor_A1_DC = BitConverter.ToInt32(client_message, 4).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 4).ToString();
					test_parameter._51Cursor_A2_DC = BitConverter.ToInt32(client_message, 8).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 8).ToString();
					test_parameter._52Cursor_B1_DC = BitConverter.ToInt32(client_message, 12).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 12).ToString();
					test_parameter._53Cursor_B2_DC = BitConverter.ToInt32(client_message, 16).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 16).ToString();
					test_parameter._54Cursor_C1_DC = BitConverter.ToInt32(client_message, 20).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 20).ToString();
					test_parameter._55Cursor_C2_DC = BitConverter.ToInt32(client_message, 24).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 24).ToString();
					#region 测试参数赋值

					test_parameter._3OutputVoltFrequency = "50Hz";
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
								   OleDbHelper.Test_File_Name_e._50Cursor_A1_DC, test_parameter._50Cursor_A1_DC == null ? "0" : test_parameter._50Cursor_A1_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										   OleDbHelper.Test_File_Name_e._51Cursor_A2_DC, test_parameter._51Cursor_A2_DC == null ? "0" : test_parameter._51Cursor_A2_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										   OleDbHelper.Test_File_Name_e._52Cursor_B1_DC, test_parameter._52Cursor_B1_DC == null ? "0" : test_parameter._52Cursor_B1_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										   OleDbHelper.Test_File_Name_e._53Cursor_B2_DC, test_parameter._53Cursor_B2_DC == null ? "0" : test_parameter._53Cursor_B2_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										   OleDbHelper.Test_File_Name_e._54Cursor_C1_DC, test_parameter._54Cursor_C1_DC == null ? "0" : test_parameter._54Cursor_C1_DC);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)
										   OleDbHelper.Test_File_Name_e._55Cursor_C2_DC, test_parameter._55Cursor_C2_DC == null ? "0" : test_parameter._55Cursor_C2_DC);
					if (!OleDbHelper.Select(OleDbHelper.Test_File_Name_e._00CompanyName, transformer_parameter._1ItsUnitName)) {
						OleDbHelper.Insert(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name);
					}
					else {
						if (!OleDbHelper.Select(OleDbHelper.Test_File_Name_e._0curTransformerName, test_parameter._1curTransformerName)) {
							OleDbHelper.Insert(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name);
						}
						else {
							OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
						}
					}
					#endregion
					break;
				//轮询状态信息
				case "0x80000086":

					this.tblock_State.Dispatcher.Invoke(new Action(delegate {
						switch (BitConverter.ToInt32(client_message, 4)) {
							case (int)State._0STAT_OK:
								tblock_State.Foreground = Brushes.Green;
								tblock_State.Text = "系统运行正常";
								break;
							case (int)State._1STAT_DSPPOWERUP_FAIL:
								tblock_State.Text = "电源板升压失败";
								tblock_State.Foreground = Brushes.Red;
								CMD_Send(Commander._3_CMD_STOPMEASURE);
								Get_StateTimer.Stop();
								MessageBox.Show(tblock_State.Text, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
								break;
							case (int)State._2STAT_DSPFFGET_FAIL:
								tblock_State.Text = "获取峰值失败";
								tblock_State.Foreground = Brushes.Red;
								CMD_Send(Commander._3_CMD_STOPMEASURE);
								Get_StateTimer.Stop();
								MessageBox.Show(tblock_State.Text, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
								break;
							case (int)State._3STAT_DSPCOM_FAIL:
								tblock_State.Text = "电源板通信失败";
								tblock_State.Foreground = Brushes.Red;
								CMD_Send(Commander._3_CMD_STOPMEASURE);
								Get_StateTimer.Stop();
								MessageBox.Show(tblock_State.Text, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
								break;
							case (int)State._4STAT_DSPPOWERDOWN_FAIL:
								tblock_State.Text = "电源板降压失败";
								tblock_State.Foreground = Brushes.Red;
								CMD_Send(Commander._3_CMD_STOPMEASURE);
								Get_StateTimer.Stop();
								if (load != null) {
									load.Close();
								}
								MessageBox.Show(tblock_State.Text, "异常", MessageBoxButton.OK, MessageBoxImage.Error);
								break;
							case (int)State._8STAT_POWER_STARTUP:
								if (test_parameter._24IsACMeasurment) {
									if (test_parameter._23IsExternalPower) {
										load.lab_电压值.Content = "";
										break;
									}
									load.lab_电压值.Content = BitConverter.ToInt32(client_message, 8).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 8).ToString();
								}

								break;
							case (int)State._7STAT_READY_FOR_MEASURE:
								if (test_parameter._24IsACMeasurment) {
									if (test_parameter._23IsExternalPower) {
										load.lab_电压值.Content = "";
										break;
									}
									load.lab_电压值.Content = "设备升压完成!";
									load.lab_单位.Content = "";
								}
								if (isPeakVale_initial) {
									test_parameter._46Peak_value = BitConverter.ToInt32(client_message, 12).ToString() == null ? "0" : BitConverter.ToInt32(client_message, 12).ToString();
									isPeakVale_initial = false;
								}
								break;
							default:
								break;
						}
					}));
					break;
				//获取版本信息
				case "0x80000088":
					//TODO.....
					break;
				default: break;
			}
		}
		#endregion

		#region 分析缓存变量
		Queue<List<int>> var_A相触发波形缓存 = new Queue<List<int>>();
		Queue<List<int>> var_B相触发波形缓存 = new Queue<List<int>>();
		Queue<List<int>> var_C相触发波形缓存 = new Queue<List<int>>();
		Queue<byte[]> var_A数据 = new Queue<byte[]>();
		Queue<byte[]> var_B数据 = new Queue<byte[]>();
		Queue<byte[]> var_C数据 = new Queue<byte[]>();
		double var_A相变化率 = 0;
		double var_B相变化率 = 0;
		double var_C相变化率 = 0;
		double var_直流最大值A = 0;
		double var_直流最大值B = 0;
		double var_直流最大值C = 0;
		int[] var_A相变化点位置 = new int[10];
		int[] var_B相变化点位置 = new int[10];
		int[] var_C相变化点位置 = new int[10];
		Queue<int[]> var_绘图队列A = new Queue<int[]>();
		Queue<int[]> var_绘图队列B = new Queue<int[]>();
		Queue<int[]> var_绘图队列C = new Queue<int[]>();
		List<int> trig_wave_dataA = new List<int>();
		List<int> trig_wave_dataB = new List<int>();
		List<int> trig_wave_dataC = new List<int>();
		List<int> bdfore_trig_wave_dataA = new List<int>();
		List<int> bdfore_trig_wave_dataB = new List<int>();
		List<int> bdfore_trig_wave_dataC = new List<int>();
		//Dictionary<string, int> A相变化点集合_字典 = new Dictionary<string, int>();
		//Dictionary<string, int> B相变化点集合_字典 = new Dictionary<string, int>();
		Dictionary<string, int> C相变化点集合_字典 = new Dictionary<string, int>();
		int index = 0;
		object monitor = new object();
		bool is_init = false;
		#endregion

		#region threadFunction
		void Fun_丢掉开头混乱数据_计算变化率_开始测试() {
			for (int i = 0; i < Page_Max_count / 15; i++) {
				if (transformer_parameter._6Transformerphase == "三相") {
					A_data.Dequeue();
					B_data.Dequeue();
					C_data.Dequeue();
				}
				if (transformer_parameter._6Transformerphase == "单相") {
					C_data.Dequeue();
				}
			}
			try {
				if (test_parameter._24IsACMeasurment) {
					if (transformer_parameter._6Transformerphase == "三相") {
						var_A相变化率 = Fun_从缓存队列获得变化率_峰值(A_data, Page_Max_count * 300);
						var_B相变化率 = Fun_从缓存队列获得变化率_峰值(B_data, Page_Max_count * 300);
						var_C相变化率 = Fun_从缓存队列获得变化率_峰值(C_data, Page_Max_count * 300);
					}
					if (transformer_parameter._6Transformerphase == "单相") {
						var_C相变化率 = Fun_从缓存队列获得变化率_峰值(C_data, Page_Max_count * 300);
					}

				}
				if (test_parameter._25IsDCMeasurment) {
					if (transformer_parameter._6Transformerphase == "三相") {
						var_直流最大值A = Fun_找直流正常的值(A_data, Page_Max_count * 300, 1);
						var_直流最大值B = Fun_找直流正常的值(B_data, Page_Max_count * 300, 2);
						var_直流最大值C = Fun_找直流正常的值(C_data, Page_Max_count * 300, 3);
					}
					if (transformer_parameter._6Transformerphase == "单相") {
						var_直流最大值C = Fun_找直流正常的值(C_data, Page_Max_count * 300, 3);
					}

				}
				is_init = true;
			}
			catch {
				MessageBox.Show("下位机数据异常!请检查接线!");
				is_init = false;
				CMD_Send(Commander._3_CMD_STOPMEASURE);
				fun_资源释放线程回收();
				return;
			}
		}
		void thread_Fun_从初始队列读取数据到新队列() {
			int var_需要的数据个数 = Page_Max_count * 200;
			int flag = Page_Max_count / 3;
			while (true) {
				lock (monitor) {
					Monitor.Pulse(monitor);
					Monitor.Wait(monitor);
					if (is_init && C_data.Count > flag && C_data.Count % flag == 0) {
						for (int i = 0; i < var_需要的数据个数 / 600; i++) {
							if (transformer_parameter._6Transformerphase == "三相") {
								var_A数据.Enqueue(A_data.Dequeue());
								var_B数据.Enqueue(B_data.Dequeue());
								var_C数据.Enqueue(C_data.Dequeue());
							}
							if (transformer_parameter._6Transformerphase == "单相") {
								var_C数据.Enqueue(C_data.Dequeue());
							}
						}
					}
				}
			}
		}
		void thread_Fun_新队列转数据为List() {
			int count = Page_Max_count * 200;
			while (true) {
				lock (monitor) {
					Monitor.Pulse(monitor);
					Monitor.Wait(monitor);
					if (var_C数据.Count > 0 && var_C数据.Count % Page_Max_count / 3 == 0) {
						if (transformer_parameter._6Transformerphase == "三相") {
							Fun_新队列转数据为List(var_A数据, count, var_A相触发波形缓存);
							Fun_新队列转数据为List(var_B数据, count, var_B相触发波形缓存);
							Fun_新队列转数据为List(var_C数据, count, var_C相触发波形缓存);
						}
						if (transformer_parameter._6Transformerphase == "单相") {
							Fun_新队列转数据为List(var_C数据, count, var_C相触发波形缓存);
						}
					}
				}
			}
		}
		void thread_Fun_实时绘图() {
			int count = Page_Max_count * 40;
			while (true) {
					if (C_data.Count > count / 600) {
						if (transformer_parameter._6Transformerphase == "三相") {
							Fun_新队列转数据为List(A_data,  count, var_A相触发波形缓存);
							Fun_新队列转数据为List(B_data,  count, var_B相触发波形缓存);
							Fun_新队列转数据为List(C_data,  count, var_C相触发波形缓存);
						}
						if (transformer_parameter._6Transformerphase == "单相") {

							Fun_新队列转数据为List(C_data, count, var_C相触发波形缓存);
						}
					}
					else {
						Thread.Sleep(10);
					}
			}
		}
		object draw = new object();
		void thread_Fun绘图() {
			while (true) {
						if (var_C相触发波形缓存.Count > 1) {
							
							if (transformer_parameter._6Transformerphase == "三相") {
								int[] tempA = new int[Page_Max_count * 40];
								int[] tempB = new int[Page_Max_count * 40];
								int[] tempC = new int[Page_Max_count * 40];

								var_A相触发波形缓存.Dequeue().CopyTo(tempA, 0);
								var_B相触发波形缓存.Dequeue().CopyTo(tempB, 0);
								var_C相触发波形缓存.Dequeue().CopyTo(tempC, 0);
								this.Tchart1.Dispatcher.Invoke(new Action(delegate {
									Max = int.Parse(test_parameter._46Peak_value) * 2;
									Fun_绘制线条(tempA, Colors.Gold, line_forTest[0]);
									Fun_绘制线条(tempB, Colors.Green, line_forTest[1]);
									Fun_绘制线条(tempC, Colors.Red, line_forTest[2]);
									if (load != null) {
										load.Close();
									}
								}));
							}
							if (transformer_parameter._6Transformerphase == "单相") {
								int[] tempC = new int[Page_Max_count * 40];
								var_C相触发波形缓存.Dequeue().CopyTo(tempC, 0);
								this.Tchart1.Dispatcher.Invoke(new Action(delegate {
									Max = int.Parse(test_parameter._46Peak_value) * 2;
									Fun_绘制线条(tempC, Colors.Red, line_forTest[2]);
									if (load != null) {
										load.Close();
									}
								}));
							}
						}
						else {
							Thread.Sleep(20);
						}
			}
		}
		void thread_Fun_List出队数据分析() {
			index = 0;
			int count = Page_Max_count * 200;
			double 误差比例 = 0;
			int 最大持续不变时间对应的点 = 0;
			int 最小持续变化时间对应的点 = 0;
			if (test_parameter._24IsACMeasurment) {

				误差比例 = int.Parse(test_parameter._2OutputVolt) / 600.0 + 100 / Page_Max_count + (float.Parse(test_parameter._29ErrorRation_AC) / 4.0);
				if (Page_Max_count == 600) {
					误差比例 += 0.2;
				}
				最大持续不变时间对应的点 = (int)(float.Parse(test_parameter._33MaxConstantTime_AC) * Page_Max_count / 3);
				最小持续变化时间对应的点 = (int)(float.Parse(test_parameter._31MinChangeTime_AC) * Page_Max_count / 3);
			}
			else {
				误差比例 = (float.Parse(test_parameter._28ErrorRation_DC) * 2);
				最大持续不变时间对应的点 = (int)(float.Parse(test_parameter._32MaxConstantTime_DC) * Page_Max_count / 3);
				最小持续变化时间对应的点 = (int)(float.Parse(test_parameter._30MinChangeTime_DC) * Page_Max_count / 3);
			}

			while (true) {
				if (is_init && var_C相触发波形缓存.Count >= 3) {
					int peak = int.Parse(test_parameter._46Peak_value);
					#region 取数据 绘图

					#region 三相
					if (transformer_parameter._6Transformerphase == "三相") {
						//保存前一组备用
						bdfore_trig_wave_dataA.Clear();
						bdfore_trig_wave_dataB.Clear();
						bdfore_trig_wave_dataC.Clear();

						for (int i = 0; i < trig_wave_dataA.Count; i++) {
							bdfore_trig_wave_dataA.Add(trig_wave_dataA[i]);
							bdfore_trig_wave_dataB.Add(trig_wave_dataB[i]);
							bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
						}
						//清空后更新
						trig_wave_dataA.Clear();
						trig_wave_dataB.Clear();
						trig_wave_dataC.Clear();
						trig_wave_dataA = var_A相触发波形缓存.Dequeue();
						trig_wave_dataB = var_B相触发波形缓存.Dequeue();
						trig_wave_dataC = var_C相触发波形缓存.Dequeue();
						//绘图
						index += Page_Max_count / 15 * 25;
						if (index >= Page_Max_count / 15 * 75) {
							index = 0;
						}
						Fun_实时绘图(trig_wave_dataA, 0, index);
						Fun_实时绘图(trig_wave_dataB, 1, index);
						Fun_实时绘图(trig_wave_dataC, 2, index);
						#region 交流
						if (test_parameter._24IsACMeasurment) {
							if (Fun_抓触发(trig_wave_dataA, var_A相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak) ||
							Fun_抓触发(trig_wave_dataB, var_B相变化率, 误差比例, Page_Max_count / 5, 最大持续不变时间对应的点, peak) ||
							Fun_抓触发(trig_wave_dataC, var_C相变化率, 误差比例, Page_Max_count / 5, 最大持续不变时间对应的点, peak)) {
								#region 暂停测试和界面逻辑
								this.Dispatcher.Invoke(new Action(delegate {
									if (load != null) {
										load.Close();
									}
									// cmbCurrentTestData.IsEnabled = true;
									Allow_Mouse_In = true;
									CMD_Send(Commander._4_CMD_PAUSEMEASUER);
									TCP连接窗口.is_end = true;
									is_开始接受 = false;
									Draw_line_Timer.Stop();
									th_read.Abort();
									th_tolist.Abort();
								}));
								#endregion

								#region 粘合数据
								//将触发段和前一段粘合
								for (int i = 0; i < trig_wave_dataA.Count; i++) {
									bdfore_trig_wave_dataA.Add(trig_wave_dataA[i]);
									bdfore_trig_wave_dataB.Add(trig_wave_dataB[i]);
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								//在触发段之后 再粘合一段
								trig_wave_dataA.Clear();
								trig_wave_dataB.Clear();
								trig_wave_dataC.Clear();
								trig_wave_dataA = var_A相触发波形缓存.Dequeue();
								trig_wave_dataB = var_B相触发波形缓存.Dequeue();
								trig_wave_dataC = var_C相触发波形缓存.Dequeue();
								for (int i = 0; i < trig_wave_dataA.Count; i++) {
									bdfore_trig_wave_dataA.Add(trig_wave_dataA[i]);
									bdfore_trig_wave_dataB.Add(trig_wave_dataB[i]);
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								#endregion

								#region 找变化点位置
								Fun_找一段曲线里的变化点(bdfore_trig_wave_dataA, var_A相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_A相变化点位置);
								Fun_变化点位置第二次优化(bdfore_trig_wave_dataA, var_A相变化率, var_A相变化点位置);
								Fun_找一段曲线里的变化点(bdfore_trig_wave_dataB, var_B相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_B相变化点位置);
								Fun_变化点位置第二次优化(bdfore_trig_wave_dataB, var_B相变化率, var_B相变化点位置);
								Fun_找一段曲线里的变化点(bdfore_trig_wave_dataC, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_C相变化点位置);
								Fun_变化点位置第二次优化(bdfore_trig_wave_dataC, var_C相变化率, var_C相变化点位置);

								if (transformer_parameter._13TransformerWindingConnMethod != "三角形接法") {
									#region 修正结束位置

									int[] array = new int[3];
									array[0] = var_A相变化点位置[1];
									array[1] = var_B相变化点位置[1];
									array[2] = var_C相变化点位置[1];
									Array.Sort(array);
									var_A相变化点位置[1] = array[1];
									var_B相变化点位置[1] = array[1];
									var_C相变化点位置[1] = array[1];
									if (Math.Abs(var_A相变化点位置[0] - array[1]) > Page_Max_count * 20) {
										var_A相变化点位置[0] = var_B相变化点位置[0];
									}
									if (Math.Abs(var_B相变化点位置[0] - array[1]) > Page_Max_count * 20) {
										var_B相变化点位置[0] = var_A相变化点位置[0];
									}
									if (Math.Abs(var_C相变化点位置[0] - array[1]) > Page_Max_count * 20) {
										var_C相变化点位置[0] = var_B相变化点位置[0];
									}
									#endregion 修正结束位置
								}
								#endregion
								#region 游标位置赋值 绘制数据 保存数据
								int[] array2 = new int[3];
								int min = 0;
								array2[0] = var_A相变化点位置[0];
								array2[1] = var_B相变化点位置[0];
								array2[2] = var_C相变化点位置[0];
								Array.Sort(array2);
								if (array2[0] != 0) {
									min = array2[0];
								}
								else {
									min = array2[2];
								}
								if (var_A相变化点位置[0] == 0) {
									var_A相变化点位置[0] = min;
								}
								if (var_B相变化点位置[0] == 0) {
									var_B相变化点位置[0] = min;
								}
								if (var_C相变化点位置[0] == 0) {
									var_C相变化点位置[0] = min;
								}
								test_parameter._40Cursor_A1 = (var_A相变化点位置[0] - min) + "";
								test_parameter._41Cursor_A2 = (var_A相变化点位置[1] - min) + "";
								test_parameter._42Cursor_B1 = (var_B相变化点位置[0] - min) + "";
								test_parameter._43Cursor_B2 = (var_B相变化点位置[1] - min) + "";
								test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
								test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
								test_parameter._50Cursor_A1_DC = (var_A相变化点位置[2] - min) + "";
								test_parameter._51Cursor_A2_DC = (var_A相变化点位置[3] - min) + "";
								test_parameter._52Cursor_B1_DC = (var_B相变化点位置[2] - min) + "";
								test_parameter._53Cursor_B2_DC = (var_B相变化点位置[3] - min) + "";
								test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2] - min) + "";
								test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3] - min) + "";

								this.Dispatcher.Invoke(new Action(delegate {
									save_data(create_save_header(), "A相数据", bdfore_trig_wave_dataA, (min - Page_Max_count * 50));
									save_data(create_save_header(), "B相数据", bdfore_trig_wave_dataB, (min - Page_Max_count * 50));
									save_data(create_save_header(), "C相数据", bdfore_trig_wave_dataC, (min - Page_Max_count * 50));
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataA, Colors.Orange, line_forTest[0], min);
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataB, Colors.Green, line_forTest[1], min);
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataC, Colors.Red, line_forTest[2], min);
									Cursor_Postion_Set("ABC", true);
									Tchart_ShowArea_Set("ABC", Tchart1);
									// btn_SaveTestData.IsEnabled = true;
									//   Add_Data_MyCombox_item(test_parameter._48Access_position);
									上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
									上一次测试的变压器 = test_parameter._1curTransformerName;
									btnDataAnalysis.IsEnabled = true;
									btnPauseTest.IsEnabled = true;
									Fun_缓存清空();
									th_analysis.Abort();
								}));
								#endregion
							}
						}
						#endregion
						#region 直流
						if (test_parameter._25IsDCMeasurment) {
							if (Fun_抓直流触发(trig_wave_dataA, var_A相变化率, var_直流最大值A, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点) ||
							Fun_抓直流触发(trig_wave_dataB, var_B相变化率, var_直流最大值B, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点) ||
							Fun_抓直流触发(trig_wave_dataC, var_C相变化率, var_直流最大值C, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点)) {
								#region 触发后逻辑
								this.Dispatcher.Invoke(new Action(delegate {
									if (load != null) {
										load.Close();
									}
									// cmbCurrentTestData.IsEnabled = true;
									Allow_Mouse_In = true;
									CMD_Send(Commander._4_CMD_PAUSEMEASUER);
									TCP连接窗口.is_end = true;
									is_开始接受 = false;
									Draw_line_Timer.Stop();
									th_read.Abort();
									th_tolist.Abort();
								}));
								#endregion
								#region 数据粘合
								for (int i = 0; i < trig_wave_dataA.Count; i++) {
									bdfore_trig_wave_dataA.Add(trig_wave_dataA[i]);
									bdfore_trig_wave_dataB.Add(trig_wave_dataB[i]);
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								trig_wave_dataA.Clear();
								trig_wave_dataB.Clear();
								trig_wave_dataC.Clear();
								trig_wave_dataA = var_A相触发波形缓存.Dequeue();
								trig_wave_dataB = var_B相触发波形缓存.Dequeue();
								trig_wave_dataC = var_C相触发波形缓存.Dequeue();
								for (int i = 0; i < trig_wave_dataA.Count; i++) {
									bdfore_trig_wave_dataA.Add(trig_wave_dataA[i]);
									bdfore_trig_wave_dataB.Add(trig_wave_dataB[i]);
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								#endregion
								#region 找起始结束点
								Fun_找直流一段曲线里的变化起始点和结束点(bdfore_trig_wave_dataA, var_A相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_A相变化点位置);
								Fun_找直流一段曲线里的变化起始点和结束点(bdfore_trig_wave_dataB, var_B相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_B相变化点位置);
								Fun_找直流一段曲线里的变化起始点和结束点(bdfore_trig_wave_dataC, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
								int[] array = new int[3];
								array[0] = var_A相变化点位置[1];
								array[1] = var_B相变化点位置[1];
								array[2] = var_C相变化点位置[1];
								Array.Sort(array);
								var_A相变化点位置[1] = array[0];
								var_B相变化点位置[1] = array[0];
								var_C相变化点位置[1] = array[0];
								#endregion
								#region 桥接点
								List<int> A = new List<int>();
								List<int> B = new List<int>();
								List<int> C = new List<int>();
								for (int i = var_A相变化点位置[0]; i < var_A相变化点位置[1]; i++) {
									A.Add(bdfore_trig_wave_dataA[i]);
									B.Add(bdfore_trig_wave_dataB[i]);
									C.Add(bdfore_trig_wave_dataC[i]);

								}
								var_A相变化率 = Fun_找直流触发段变化率(A);
								var_B相变化率 = Fun_找直流触发段变化率(B);
								var_C相变化率 = Fun_找直流触发段变化率(C);
								Fun_找直流过渡和桥接点(A, var_A相变化率, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_A相变化点位置);
								Fun_找直流过渡和桥接点(B, var_B相变化率, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_B相变化点位置);
								Fun_找直流过渡和桥接点(C, var_C相变化率, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
								#endregion
								#region 游标赋值
								int min = var_A相变化点位置[0];
								if (min >= var_B相变化点位置[0]) {
									min = var_B相变化点位置[0];
								}
								if (min >= var_C相变化点位置[0]) {
									min = var_C相变化点位置[0];
								}
								test_parameter._40Cursor_A1 = (var_A相变化点位置[0] - min) + "";
								test_parameter._41Cursor_A2 = (var_A相变化点位置[1] - min) + "";
								test_parameter._42Cursor_B1 = (var_B相变化点位置[0] - min) + "";
								test_parameter._43Cursor_B2 = (var_B相变化点位置[1] - min) + "";
								test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
								test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
								test_parameter._50Cursor_A1_DC = (var_A相变化点位置[2]) + "";
								test_parameter._51Cursor_A2_DC = (var_A相变化点位置[3]) + "";
								test_parameter._52Cursor_B1_DC = (var_B相变化点位置[2]) + "";
								test_parameter._53Cursor_B2_DC = (var_B相变化点位置[3]) + "";
								test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2]) + "";
								test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3]) + "";
								#endregion
								this.Dispatcher.Invoke(new Action(delegate {
									save_data(create_save_header(), "A相数据", bdfore_trig_wave_dataA, (min - Page_Max_count * 50));
									save_data(create_save_header(), "B相数据", bdfore_trig_wave_dataB, (min - Page_Max_count * 50));
									save_data(create_save_header(), "C相数据", bdfore_trig_wave_dataC, (min - Page_Max_count * 50));
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataA, Colors.Orange, line_forTest[0], min);
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataB, Colors.Green, line_forTest[1], min);
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataC, Colors.Red, line_forTest[2], min);
									Cursor_Postion_Set("ABC", true);
									Tchart_ShowArea_Set("ABC", Tchart1);
									//btn_SaveTestData.IsEnabled = true;
									// Add_Data_MyCombox_item(test_parameter._48Access_position);
									上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
									上一次测试的变压器 = test_parameter._1curTransformerName;
									btnDataAnalysis.IsEnabled = true;
									btnPauseTest.IsEnabled = true;
									Fun_缓存清空();
									th_analysis.Abort();
								}));
							}
						}
						#endregion
					}
					#endregion

					#region 单相
					if (transformer_parameter._6Transformerphase == "单相") {
						//保存前一组备用
						bdfore_trig_wave_dataC.Clear();
						for (int i = 0; i < trig_wave_dataC.Count; i++) {
							bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
						}
						//清空后更新
						trig_wave_dataC.Clear();
						trig_wave_dataC = var_C相触发波形缓存.Dequeue();
						//绘图
						index += Page_Max_count / 15 * 25;
						if (index >= Page_Max_count / 15 * 100) {
							index = 0;
						}
						Fun_实时绘图(trig_wave_dataC, 2, index);
						#region 交流
						if (test_parameter._24IsACMeasurment) {
							if (
							Fun_抓触发(trig_wave_dataC, var_C相变化率, 误差比例, Page_Max_count / 5, 最大持续不变时间对应的点, peak)) {
								#region 停止测试和界面逻辑
								this.Dispatcher.Invoke(new Action(delegate {
									if (load != null) {
										load.Close();
									}
									// cmbCurrentTestData.IsEnabled = true;
									Allow_Mouse_In = true;
									CMD_Send(Commander._4_CMD_PAUSEMEASUER);
									TCP连接窗口.is_end = true;
									is_开始接受 = false;
									Draw_line_Timer.Stop();

									th_read.Abort();
									th_tolist.Abort();
								}));
								#endregion

								#region 粘合数据
								//将触发段和前一段粘合
								for (int i = 0; i < trig_wave_dataC.Count; i++) {
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								//在触发段之后 再粘合一段
								trig_wave_dataC.Clear();
								trig_wave_dataC = var_C相触发波形缓存.Dequeue();
								for (int i = 0; i < trig_wave_dataC.Count; i++) {
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								#endregion

								#region 找变化点位置
								Fun_找一段曲线里的变化点(bdfore_trig_wave_dataC, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_C相变化点位置);
								#endregion

								#region 游标位置赋值 绘制数据 保存数据
								int min = var_C相变化点位置[0];

								test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
								test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";

								test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2] - min) + "";
								test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3] - min) + "";

								this.Dispatcher.Invoke(new Action(delegate {
									save_data(create_save_header(), "C相数据", bdfore_trig_wave_dataC, (min - Page_Max_count * 50));
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataC, Colors.Red, line_forTest[2], min);
									Cursor_Postion_Set("C", true);
									Tchart_ShowArea_Set("C", Tchart1);
									// btn_SaveTestData.IsEnabled = true;
									//  Add_Data_MyCombox_item(test_parameter._48Access_position);
									上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
									上一次测试的变压器 = test_parameter._1curTransformerName;
									btnDataAnalysis.IsEnabled = true;
									btnPauseTest.IsEnabled = true;
									Fun_缓存清空();
									th_analysis.Abort();
								}));
								#endregion
							}
						}
						#endregion
						#region 直流
						if (test_parameter._25IsDCMeasurment) {
							if (
							Fun_抓直流触发(trig_wave_dataC, var_C相变化率, var_直流最大值C, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点)) {
								#region 触发后逻辑
								this.Dispatcher.Invoke(new Action(delegate {
									if (load != null) {
										load.Close();
									}
									//    cmbCurrentTestData.IsEnabled = true;
									Allow_Mouse_In = true;
									CMD_Send(Commander._4_CMD_PAUSEMEASUER);
									TCP连接窗口.is_end = true;
									is_开始接受 = false;
									Draw_line_Timer.Stop();

									th_read.Abort();
									th_tolist.Abort();
								}));
								#endregion
								#region 粘合数据
								for (int i = 0; i < trig_wave_dataC.Count; i++) {
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								trig_wave_dataC.Clear();
								trig_wave_dataC = var_C相触发波形缓存.Dequeue();
								for (int i = 0; i < trig_wave_dataC.Count; i++) {
									bdfore_trig_wave_dataC.Add(trig_wave_dataC[i]);
								}
								#endregion
								#region 找起始结束点
								Fun_找直流一段曲线里的变化起始点和结束点(bdfore_trig_wave_dataC, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
								#endregion
								#region 找桥接点
								List<int> C = new List<int>();
								for (int i = var_C相变化点位置[0]; i < var_C相变化点位置[1]; i++) {
									C.Add(bdfore_trig_wave_dataC[i]);

								}
								var_C相变化率 = Fun_找直流触发段变化率(C);
								Fun_找直流过渡和桥接点(C, var_C相变化率, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
								#endregion
								#region 游标赋值
								int min = var_C相变化点位置[0];
								test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
								test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
								test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2]) + "";
								test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3]) + "";
								#endregion
								this.Dispatcher.Invoke(new Action(delegate {
									save_data(create_save_header(), "C相数据", bdfore_trig_wave_dataC, (min - Page_Max_count * 50));
									Fun_绘制触发曲线_Trig_Line(bdfore_trig_wave_dataC, Colors.Red, line_forTest[2], min);
									Cursor_Postion_Set("C", true);
									Tchart_ShowArea_Set("C", Tchart1);
									// btn_SaveTestData.IsEnabled = true;
									// Add_Data_MyCombox_item(test_parameter._48Access_position);
									上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
									上一次测试的变压器 = test_parameter._1curTransformerName;
									btnDataAnalysis.IsEnabled = true;
									btnPauseTest.IsEnabled = true;
									Fun_缓存清空();
									th_analysis.Abort();
								}));
							}
						}
						#endregion
					}
					#endregion

					#endregion

				}
				else {
					Thread.Sleep(1);
				}
			}

		}
		int max_保存长度 = 0;
		void thread_Fun_List出队数据分析(List<int> A, List<int> B, List<int> C) {
			for (int i = 0; i < var_A相变化点位置.Length; i++) {
				var_A相变化点位置[i] = 0;
				var_B相变化点位置[i] = 0;
				var_C相变化点位置[i] = 0;
			}
			index = 0;
			int peak = 0;

			Max = 0;
			#region 三相
			if (transformer_parameter._6Transformerphase == "三相") {
				#region 交流
				if (test_parameter._24IsACMeasurment) {
					#region 找变化点位置
					Fun_找一段曲线里的变化点(A, var_A相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_A相变化点位置);
				    Fun_变化点位置第二次优化(A, var_A相变化率, var_A相变化点位置);
					Fun_找一段曲线里的变化点(B, var_B相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_B相变化点位置);
					Fun_变化点位置第二次优化(B, var_B相变化率, var_B相变化点位置);
					Fun_找一段曲线里的变化点(C, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_C相变化点位置);
					Fun_变化点位置第二次优化(C, var_C相变化率, var_C相变化点位置);

					if (transformer_parameter._13TransformerWindingConnMethod != "三角形接法") {
						#region 修正结束位置

						int[] array = new int[3];
						bool is_neccessary = false;
						if (Math.Abs((var_A相变化点位置[1] - var_C相变化点位置[1])) > Page_Max_count * 5) {
							is_neccessary = true;
						}
						if (Math.Abs((var_B相变化点位置[1] - var_C相变化点位置[1])) > Page_Max_count * 5) {
							is_neccessary = true;
						}
						array[0] = var_A相变化点位置[1];
						array[1] = var_B相变化点位置[1];
						array[2] = var_C相变化点位置[1];
						Array.Sort(array);
						if (is_neccessary) {
							var_A相变化点位置[1] = array[1];
							var_B相变化点位置[1] = array[1];
							var_C相变化点位置[1] = array[1];
						}
						if (Math.Abs(var_A相变化点位置[0] - array[1]) > Page_Max_count * 50) {
							var_A相变化点位置[0] = var_B相变化点位置[0];
						}
						if (Math.Abs(var_B相变化点位置[0] - array[1]) > Page_Max_count * 50) {
							var_B相变化点位置[0] = var_A相变化点位置[0];
						}
						if (Math.Abs(var_C相变化点位置[0] - array[1]) > Page_Max_count * 50) {
							var_C相变化点位置[0] = var_B相变化点位置[0];
						}
						#endregion 修正结束位置
					}
					#endregion
					#region 游标位置赋值 绘制数据 保存数据
					int[] array1 = new int[3];
					int min = 0;
					array1[0] = var_A相变化点位置[0];
					array1[1] = var_B相变化点位置[0];
					array1[2] = var_C相变化点位置[0];
					Array.Sort(array1);
					if (array1[0] != 0) {
						min = array1[0];
					}
					else {
						min = array1[2];
					}
					if (var_A相变化点位置[0] == 0) {
						var_A相变化点位置[0] = min;
					}
					if (var_B相变化点位置[0] == 0) {
						var_B相变化点位置[0] = min;
					}
					if (var_C相变化点位置[0] == 0) {
						var_C相变化点位置[0] = min;
					}
					test_parameter._40Cursor_A1 = (var_A相变化点位置[0] - min) + "";
					test_parameter._41Cursor_A2 = (var_A相变化点位置[1] - min) + "";
					test_parameter._42Cursor_B1 = (var_B相变化点位置[0] - min) + "";
					test_parameter._43Cursor_B2 = (var_B相变化点位置[1] - min) + "";
					test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
					test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
					test_parameter._50Cursor_A1_DC = (var_A相变化点位置[2] - min) + "";
					test_parameter._51Cursor_A2_DC = (var_A相变化点位置[3] - min) + "";
					test_parameter._52Cursor_B1_DC = (var_B相变化点位置[2] - min) + "";
					test_parameter._53Cursor_B2_DC = (var_B相变化点位置[3] - min) + "";
					test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2] - min) + "";
					test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3] - min) + "";
					List<int> 触发段A = new List<int>();
					List<int> 触发段B = new List<int>();
					List<int> 触发段C = new List<int>();
					if (min - (Page_Max_count * 50) < 0) {
						is_无效变化点 = true;
						return;
					}

					if (min + Page_Max_count * 150 > A.Count) {
						max_保存长度 = A.Count;
					}
					else {
						max_保存长度 = min + Page_Max_count * 150;
					}
					for (int i = min - Page_Max_count * 50; i < max_保存长度; i++) {
						触发段A.Add(A[i]);
						触发段B.Add(B[i]);
						触发段C.Add(C[i]);
					}
					this.Dispatcher.Invoke(new Action(delegate {
						Fun_绘制触发曲线_Trig_Line(触发段A, Colors.Orange, line_forTest[0]);
						Fun_绘制触发曲线_Trig_Line(触发段B, Colors.Green, line_forTest[1]);
						Fun_绘制触发曲线_Trig_Line(触发段C, Colors.Red, line_forTest[2]);
						save_data(create_save_header(), "A相数据", 触发段A, 0);
						save_data(create_save_header(), "B相数据", 触发段B, 0);
						save_data(create_save_header(), "C相数据", 触发段C, 0);
						Cursor_Postion_Set("ABC", true);
						Tchart_ShowArea_Set("ABC", Tchart1);
					}));
					#endregion
				}
				#endregion
				#region 直流
				if (test_parameter._25IsDCMeasurment) {
					#region 找起始结束点
					Fun_找直流一段曲线里的变化起始点和结束点(A, var_A相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_A相变化点位置);
					Fun_找直流一段曲线里的变化起始点和结束点(B, var_B相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_B相变化点位置);
					Fun_找直流一段曲线里的变化起始点和结束点(C, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
					int[] array = new int[3];
					array[0] = var_A相变化点位置[1];
					array[1] = var_B相变化点位置[1];
					array[2] = var_C相变化点位置[1];
					Array.Sort(array);
					var_A相变化点位置[1] = array[0];
					var_B相变化点位置[1] = array[0];
					var_C相变化点位置[1] = array[0];
					List<int> AA = new List<int>();
					List<int> BB = new List<int>();
					List<int> CC = new List<int>();
					List<int> AAA = new List<int>();
					List<int> BBB = new List<int>();
					List<int> CCC = new List<int>();
					if (var_A相变化点位置[1] - var_A相变化点位置[0] < Page_Max_count) {
						return;
					}
					for (int i = var_A相变化点位置[0]; i < var_A相变化点位置[1]; i++) {
						AA.Add(A[i]);
						BB.Add(B[i]);
						CC.Add(C[i]);
					}
					int len = 0;
					if ((var_A相变化点位置[1] + Page_Max_count * 50) >= A.Count) {
						len = A.Count;

					}
					else {
						len = var_A相变化点位置[1] + Page_Max_count * 50;
					}
					for (int i = var_C相变化点位置[0] - Page_Max_count * 50 < 0 ? 0 : (var_C相变化点位置[0] - Page_Max_count * 50); i < len; i++) {
						AAA.Add(A[i]);
						BBB.Add(B[i]);
						CCC.Add(C[i]);
					}
					#endregion
					#region 找桥接点

					double var_A相直流变化率 = Fun_找直流触发段变化率(AA);
					double var_B相直流变化率 = Fun_找直流触发段变化率(BB);
					double var_C相直流变化率 = Fun_找直流触发段变化率(CC);
					Fun_找直流过渡和桥接点(AA, var_A相直流变化率, Page_Max_count / 10, Page_Max_count / 3, var_A相变化点位置);
					Fun_找直流过渡和桥接点(BB, var_B相直流变化率, Page_Max_count / 10, Page_Max_count / 3, var_B相变化点位置);
					Fun_找直流过渡和桥接点(CC, var_C相直流变化率, Page_Max_count / 10, Page_Max_count / 3, var_C相变化点位置);
					int min = var_A相变化点位置[0];
					if (min >= var_B相变化点位置[0]) {
						min = var_B相变化点位置[0];
					}
					if (min >= var_C相变化点位置[0]) {
						min = var_C相变化点位置[0];
					}
					#endregion
					#region 游标赋值
					test_parameter._40Cursor_A1 = (var_A相变化点位置[0] - min) + "";
					test_parameter._41Cursor_A2 = (var_A相变化点位置[1] - min) + "";
					test_parameter._42Cursor_B1 = (var_B相变化点位置[0] - min) + "";
					test_parameter._43Cursor_B2 = (var_B相变化点位置[1] - min) + "";
					test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
					test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
					test_parameter._50Cursor_A1_DC = (var_A相变化点位置[2]) + "";
					test_parameter._51Cursor_A2_DC = (var_A相变化点位置[3]) + "";
					test_parameter._52Cursor_B1_DC = (var_B相变化点位置[2]) + "";
					test_parameter._53Cursor_B2_DC = (var_B相变化点位置[3]) + "";
					test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2]) + "";
					test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3]) + "";
					#endregion
					this.Dispatcher.Invoke(new Action(delegate {
						Fun_绘制触发曲线_Trig_Line(AAA, Colors.Orange, line_forTest[0]);
						Fun_绘制触发曲线_Trig_Line(BBB, Colors.Green, line_forTest[1]);
						Fun_绘制触发曲线_Trig_Line(CCC, Colors.Red, line_forTest[2]);
						save_data(create_save_header(), "A相数据", AAA, 0);
						save_data(create_save_header(), "B相数据", BBB, 0);
						save_data(create_save_header(), "C相数据", CCC, 0);
						Cursor_Postion_Set("ABC", true);
						Tchart_ShowArea_Set("ABC", Tchart1);
						上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
						上一次测试的变压器 = test_parameter._1curTransformerName;
						Fun_缓存清空();
					}));
				}
				#endregion
			}
			#endregion

			#region 单相
			if (transformer_parameter._6Transformerphase == "单相") {
				#region 交流
				if (test_parameter._24IsACMeasurment) {
					#region 找变化点位置
					Fun_找一段曲线里的变化点(C, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, peak, var_C相变化点位置);
					Fun_变化点位置第二次优化(C, var_C相变化率, var_C相变化点位置);
					#endregion
					#region 游标位置赋值 绘制数据 保存数据
					int min = var_C相变化点位置[0];
					test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
					test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
					test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2] - min) + "";
					test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3] - min) + "";
					List<int> 触发段C = new List<int>();
					if (min - (Page_Max_count * 50) < 0) {
						is_无效变化点 = true;
						return;
					}

					if (min + Page_Max_count * 150 > C.Count) {
						max_保存长度 = C.Count;
					}
					else {
						max_保存长度 = min + Page_Max_count * 150;
					}
					for (int i = min - Page_Max_count * 50; i < max_保存长度; i++) {
						触发段C.Add(C[i]);
					}
					this.Dispatcher.Invoke(new Action(delegate {
						Fun_绘制触发曲线_Trig_Line(触发段C, Colors.Red, line_forTest[2]);
						save_data(create_save_header(), "C相数据", 触发段C, 0);
						Cursor_Postion_Set("C", true);
						Tchart_ShowArea_Set("C", Tchart1);
						//btn_SaveTestData.IsEnabled = true;
					}));
					#endregion
				}
				#endregion
				#region 直流
				if (test_parameter._25IsDCMeasurment) {
					#region 找起始结束点
					Fun_找直流一段曲线里的变化起始点和结束点(C, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
					List<int> CC = new List<int>();
					List<int> CCC = new List<int>();

					for (int i = var_C相变化点位置[0]; i < var_C相变化点位置[1]; i++) {
						CC.Add(C[i]);
					}
					int len = 0;
					if ((var_C相变化点位置[1] + Page_Max_count * 50) >= C.Count) {
						len = C.Count;
					}
					else {
						len = var_C相变化点位置[1] + Page_Max_count * 50;

					}
					for (int i = (var_C相变化点位置[0] - Page_Max_count * 50 < 0 ? 0 : (var_C相变化点位置[0] - Page_Max_count * 50)); i < len; i++) {
						CCC.Add(C[i]);
					}
					#endregion
					#region 找桥接点
					double var_C相直流变化率 = Fun_找直流触发段变化率(CC);
					Fun_找直流过渡和桥接点(CC, var_C相直流变化率, Page_Max_count / 10, Page_Max_count / 3, var_C相变化点位置);
					#endregion
					#region 游标赋值
					int min = var_C相变化点位置[0];
					test_parameter._44Cursor_C1 = (var_C相变化点位置[0] - min) + "";
					test_parameter._45Cursor_C2 = (var_C相变化点位置[1] - min) + "";
					test_parameter._54Cursor_C1_DC = (var_C相变化点位置[2]) + "";
					test_parameter._55Cursor_C2_DC = (var_C相变化点位置[3]) + "";
					#endregion
					this.Dispatcher.Invoke(new Action(delegate {
						Fun_绘制触发曲线_Trig_Line(CCC, Colors.Red, line_forTest[2]);
						save_data(create_save_header(), "C相数据", CCC, 0);
						Cursor_Postion_Set("C", true);
						Tchart_ShowArea_Set("C", Tchart1);
						上一次测试的变压器所属单位 = transformer_parameter._1ItsUnitName;
						上一次测试的变压器 = test_parameter._1curTransformerName;
						Fun_缓存清空();
					}));
				}
				#endregion
			}
			#endregion
		}
	#endregion

		#region ProcessFunction
		void Fun_新队列转数据为List(Queue<byte[]> var_新队列数据, int var_需要的数据个数, Queue<List<int>> var_Lis队列) {
			try {
				List<int> temp = new List<int>();
				for (int i = 0; i < var_需要的数据个数 / 600; i++) {
					byte[] temp1 = var_新队列数据.Dequeue();
					for (int j = 0; j < temp1.Length; j += 2) {
						temp.Add(BitConverter.ToInt16(temp1, j));
					}
				}
				var_Lis队列.Enqueue(temp);
			}
			catch {
				//MessageBox.Show("数据接收失败!请重新测试");
			}

		}
		void Fun_新队列转数据为List(Queue<byte[]> var_新队列数据,string save_path, int var_需要的数据个数, Queue<List<int>> var_Lis队列) {
			try {
				List<int> temp = new List<int>();
				for (int i = 0; i < var_需要的数据个数 / 600; i++) {
					byte[] temp1 = var_新队列数据.Dequeue();
					save_data(save_path, temp1);
					for (int j = 0; j < temp1.Length; j += 2) {
						temp.Add(BitConverter.ToInt16(temp1, j));
					}
				}
				var_Lis队列.Enqueue(temp);
			}
			catch {
				//MessageBox.Show("数据接收失败!请重新测试");
			}

		}
		double Fun_从缓存队列获得变化率_峰值(Queue<byte[]> var_原始队列数据, int var_需要的数据个数) {
			List<int> var_存放数据的列表 = new List<int>();
			List<int[]> var_正常波形列表 = new List<int[]>();
			for (int i = 0; i < var_需要的数据个数 / 600; i++) {
				byte[] temp = var_原始队列数据.Dequeue();
				for (int j = 0; j < temp.Length; j += 2) {
					var_存放数据的列表.Add(BitConverter.ToInt16(temp, j));
				}
			}
			try {
				Fun_找正常波形并加入列表(var_存放数据的列表, var_正常波形列表);
			}
			catch {
				if (load != null) {
					load.Close();
				}
				CMD_Send(Commander._3_CMD_STOPMEASURE);
				fun_资源释放线程回收();
				btnStopTest.IsEnabled = false;
				btnSartTest.IsEnabled = true;
				btnSystemSetting.IsEnabled = true;
				btnTransformerConfig.IsEnabled = true;
				btnPauseTest.IsEnabled = false;
				// cmbCurrentTestData.IsEnabled = true;
				Allow_Mouse_In = true;
				Mouse.OverrideCursor = Cursors.Hand;
				tbErrorExpection.Text = "错误:测试数据异常!请重新测试!";
				//MessageBox.Show("测试数据异常!请重新测试!");
				return 0;
			}
			return Fun_计算周期两点之间最大差异(var_正常波形列表, var_正常波形列表[0].Length);
		}
		void Fun_缓存清空() {
			gpTree.IsEnabled = true;
			bdfore_trig_wave_dataA.Clear();
			bdfore_trig_wave_dataB.Clear();
			bdfore_trig_wave_dataC.Clear();
			A_data.Clear();
			B_data.Clear();
			C_data.Clear();
			var_A数据.Clear();
			var_B数据.Clear();
			var_C数据.Clear();
			var_A相触发波形缓存.Clear();
			var_B相触发波形缓存.Clear();
			var_C相触发波形缓存.Clear();
			trig_wave_dataA.Clear();
			trig_wave_dataB.Clear();
			trig_wave_dataC.Clear();
			var_绘图队列A.Clear();
			var_绘图队列B.Clear();
			var_绘图队列C.Clear();
		}
		bool end = true;
		void Fun_实时绘图(List<int> var_绘图数据, int Phase, int index) {
			int count = Page_Max_count * 500 / 15;
			int[] temp = new int[count];
			for (int j = 0; j < temp.Length; j++) {
				temp[j] = var_绘图数据[j + index];
			}
			if (Phase == 0) {
				if (var_绘图队列A.Count > 1) {
					var_绘图队列A.Dequeue();
				}
				var_绘图队列A.Enqueue(temp);

			}
			if (Phase == 1) {
				if (var_绘图队列B.Count > 1) {
					var_绘图队列B.Dequeue();
				}
				var_绘图队列B.Enqueue(temp);
			}
			if (Phase == 2) {
				if (var_绘图队列C.Count > 1) {
					var_绘图队列C.Dequeue();
				}
				var_绘图队列C.Enqueue(temp);
			}
		}
		List<int> Fun_读文件(string 文件路径, int 返回数据的个数, short 每次偏移量, short 文件数据占的字节数) {
			List<int> temp = new List<int>();
			for (int k = 0; k < 返回数据的个数; k += 200) {
				byte[] buffer = FileHelper.OpenFile(文件路径, 200 * 文件数据占的字节数, (返回数据的个数 * 文件数据占的字节数) * 每次偏移量 + 文件数据占的字节数 * k);
				int index = 0;
				for (int i = 0; i < buffer.Length; i += 文件数据占的字节数) {
					index = k + i / 文件数据占的字节数;
					if (index >= 返回数据的个数) {
						index = i - 1;
					}
					if (文件数据占的字节数 == 2) {
						temp.Add(BitConverter.ToInt16(buffer, i));
					}
					if (文件数据占的字节数 == 4) {
						temp.Add(BitConverter.ToInt32(buffer, i));
					}
				}
			}
			return temp;
		}
		List<int> Fun_从文件变化点获取触发段曲线(string 文件路径, int 开始位置,int 分析个数) {
			List<int> temp = new List<int>();
			byte[] buffer = FileHelper.OpenFile(文件路径, 分析个数, 开始位置);
			for (int i = 0; i < buffer.Length; i += 2) {
				temp.Add(BitConverter.ToInt16(buffer, i));
			}
			return temp;
		}
		DispatcherTimer proTimer = new DispatcherTimer();
		DispatcherTimer proTimer_Single = new DispatcherTimer();

		void Fun_将文件数据全部分析并将变化点加入字典() {
			path = test_data_path  + transformer_parameter._1ItsUnitName + "\\" + test_parameter._1curTransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts + "\\";
			if (!File.Exists(path + "C相原始数据")) {
				return;
			}
			filelength = 0;
			filelength = FileHelper.getLength(path + "C相原始数据");
			if (filelength == 0) {
				return;
			}
			if (wait_analysis == null) {
				wait_analysis = new waitForAnalysis(filelength);
			}
			wait_analysis.Show();
			index = 0;
			C相变化点集合_字典.Clear();
			if (test_parameter._24IsACMeasurment) {
				误差比例 = 0;
			误差比例 = int.Parse(test_parameter._2OutputVolt) / 500.0  + (float.Parse(test_parameter._29ErrorRation_AC) / 4.0);
				if (Page_Max_count == 600) {
					误差比例+=2+ 800.0/int.Parse(test_parameter._2OutputVolt);
				}
				最大持续不变时间对应的点 = (int)(float.Parse(test_parameter._33MaxConstantTime_AC) * Page_Max_count / 3);
				最小持续变化时间对应的点 = (int)(float.Parse(test_parameter._31MinChangeTime_AC) * Page_Max_count / 3);
				#region 三相
				if (transformer_parameter._6Transformerphase == "三相") {
					#region 计算变化率

					List<int> quxianA = Fun_读文件(path + "A相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianB = Fun_读文件(path + "B相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					List<int[]> noaml = new List<int[]>();//    
					try {
						Fun_找正常波形并加入列表(quxianA, noaml);
						var_A相变化率 = Fun_计算周期两点之间最大差异(noaml, noaml[0].Length);               //
						noaml.Clear();
						Fun_找正常波形并加入列表(quxianB, noaml);                                             //
						var_B相变化率 = Fun_计算周期两点之间最大差异(noaml, noaml[0].Length);                   //
						noaml.Clear();                                                                          ///找正常波形 并找到变化率;
						Fun_找正常波形并加入列表(quxianC, noaml);                                               ///          
						var_C相变化率 = Fun_计算周期两点之间最大差异(noaml, noaml[0].Length);                    ///   
						noaml.Clear();
					}
					catch {
						tbErrorExpection.Text = "没有找到变化点!";
					}
					//
					//误差比例 = int.Parse(test_parameter._46Peak_value) / 50 * var_C相变化率;
					tbErrorExpection.Text = 误差比例 + "||C:" + var_C相变化率.ToString("0.##") +"B:"+ var_B相变化率.ToString("0.##")+ "A:"+ var_A相变化率.ToString("0.##");
					#endregion
					offset = 0;
					proTimer.Start();
				}

				#endregion
				#region 单相
				if (transformer_parameter._6Transformerphase == "单相") {
					#region 计算变化率
					List<int> quxianCC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					List<int[]> noamlC = new List<int[]>();
					noamlC.Clear();                              ///找正常波形 并找到变化率;
					try {
						Fun_找正常波形并加入列表(quxianCC, noamlC);
						var_C相变化率 = Fun_计算周期两点之间最大差异(noamlC, noamlC[0].Length);
						noamlC.Clear();
					}
					catch {
						tbErrorExpection.Text = "没有找到变化点!";
					}
					#endregion
					offset = 0;
					proTimer_Single.Start();
				}

				#endregion
			}
			else {
				误差比例 = (float.Parse(test_parameter._28ErrorRation_DC) * 2);
				最大持续不变时间对应的点 = (int)(float.Parse(test_parameter._32MaxConstantTime_DC) * Page_Max_count / 3);
				最小持续变化时间对应的点 = (int)(float.Parse(test_parameter._30MinChangeTime_DC) * Page_Max_count / 3);
				#region 三相
				if (transformer_parameter._6Transformerphase == "三相") {
					#region 计算变化率

					List<int> quxianA = Fun_读文件(path + "A相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianB = Fun_读文件(path + "B相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					try {
						var_A相变化率 = Fun_找直流变化率(quxianA);
						var_B相变化率 = Fun_找直流变化率(quxianB);
						var_C相变化率 = Fun_找直流变化率(quxianC);
					}
					catch {

					}
					#endregion
					offset = 0;
					proTimer.Start();
				}

				#endregion
				#region 单相
				if (transformer_parameter._6Transformerphase == "单相") {
					#region 计算变化率
					List<int> quxianCC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					try {
						var_C相变化率 = Fun_找直流变化率(quxianC);

					}
					catch {

					}
					#endregion
					offset = 0;
					proTimer_Single.Start();
				}

				#endregion
			}




		}
		void Fun_重新分析(string path) {
			if (!File.Exists(path + "C相原始数据")) {
				return;
			}
			filelength = 0;
			filelength = FileHelper.getLength(path + "C相原始数据");
			if (filelength == 0) {
				return;
			}
			if (wait_analysis == null) {
				wait_analysis = new waitForAnalysis(filelength);
			}
			wait_analysis.Show();
			index = 0;
			C相变化点集合_字典.Clear();
			if (test_parameter._24IsACMeasurment) {
				误差比例 = 0;
				误差比例 = int.Parse(test_parameter._2OutputVolt) / 500.0 + (float.Parse(test_parameter._29ErrorRation_AC) / 4.0);
				if (Page_Max_count == 600) {
					误差比例 += 2 + 800.0 / int.Parse(test_parameter._2OutputVolt);
				}
				最大持续不变时间对应的点 = (int)(float.Parse(test_parameter._33MaxConstantTime_AC) * Page_Max_count / 3);
				最小持续变化时间对应的点 = (int)(float.Parse(test_parameter._31MinChangeTime_AC) * Page_Max_count / 3);
				#region 三相
				if (transformer_parameter._6Transformerphase == "三相") {
					#region 计算变化率

					List<int> quxianA = Fun_读文件(path + "A相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianB = Fun_读文件(path + "B相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					List<int[]> noaml = new List<int[]>();//    
					try {
						Fun_找正常波形并加入列表(quxianA, noaml);
						var_A相变化率 = Fun_计算周期两点之间最大差异(noaml, noaml[0].Length);               //
						noaml.Clear();
						Fun_找正常波形并加入列表(quxianB, noaml);                                             //
						var_B相变化率 = Fun_计算周期两点之间最大差异(noaml, noaml[0].Length);                   //
						noaml.Clear();                                                                          ///找正常波形 并找到变化率;
						Fun_找正常波形并加入列表(quxianC, noaml);                                               ///          
						var_C相变化率 = Fun_计算周期两点之间最大差异(noaml, noaml[0].Length);                    ///   
						noaml.Clear();
					}
					catch {
						tbErrorExpection.Text = "没有找到变化点!";
					}
					//
					//误差比例 = int.Parse(test_parameter._46Peak_value) / 50 * var_C相变化率;
					tbErrorExpection.Text = 误差比例 + "||C:" + var_C相变化率.ToString("0.##") + "B:" + var_B相变化率.ToString("0.##") + "A:" + var_A相变化率.ToString("0.##");
					#endregion
					offset = 0;
					proTimer.Start();
				}

				#endregion
				#region 单相
				if (transformer_parameter._6Transformerphase == "单相") {
					#region 计算变化率
					List<int> quxianCC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					List<int[]> noamlC = new List<int[]>();
					noamlC.Clear();                              ///找正常波形 并找到变化率;
					try {
						Fun_找正常波形并加入列表(quxianCC, noamlC);
						var_C相变化率 = Fun_计算周期两点之间最大差异(noamlC, noamlC[0].Length);
						noamlC.Clear();
					}
					catch {
						tbErrorExpection.Text = "没有找到变化点!";
					}
					#endregion
					offset = 0;
					proTimer_Single.Start();
				}

				#endregion
			}
			else {
				误差比例 = (float.Parse(test_parameter._28ErrorRation_DC) * 2);
				最大持续不变时间对应的点 = (int)(float.Parse(test_parameter._32MaxConstantTime_DC) * Page_Max_count / 3);
				最小持续变化时间对应的点 = (int)(float.Parse(test_parameter._30MinChangeTime_DC) * Page_Max_count / 3);
				#region 三相
				if (transformer_parameter._6Transformerphase == "三相") {
					#region 计算变化率

					List<int> quxianA = Fun_读文件(path + "A相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianB = Fun_读文件(path + "B相原始数据", Page_Max_count * 200, 0, 2);//
					List<int> quxianC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					try {
						var_A相变化率 = Fun_找直流变化率(quxianA);
						var_B相变化率 = Fun_找直流变化率(quxianB);
						var_C相变化率 = Fun_找直流变化率(quxianC);
					}
					catch {

					}
					#endregion
					offset = 0;
					proTimer.Start();
				}

				#endregion
				#region 单相
				if (transformer_parameter._6Transformerphase == "单相") {
					#region 计算变化率
					List<int> quxianCC = Fun_读文件(path + "C相原始数据", Page_Max_count * 200, 0, 2);//
					try {
						var_C相变化率 = Fun_找直流变化率(quxianC);

					}
					catch {

					}
					#endregion
					offset = 0;
					proTimer_Single.Start();
				}

				#endregion
			}
		}
		void Fun_将字典内容加添加到_Lb() {
			lb_switch_list.Items.Clear();
			for (int i = 0; i < C相变化点集合_字典.Count; i++) {
				lb_switch_list.Items.Add(C相变化点集合_字典.Keys.ElementAt(i));
			}
			if (lb_switch_list.Items.Count > 0) {
				lb_switch_list.SelectedIndex = 0;
			}
			//Fun_删除无效变化点(); 
		}

		List<int> Fun_读取触发波形文件(string path) {
			StringBuilder sb = new StringBuilder();
			sb.Append(Encoding.Default.GetString(FileHelper.OpenFile(path)));
			string ss = Encoding.Default.GetString(BitConverter.GetBytes(0));
			if (sb.ToString() == Encoding.Default.GetString(BitConverter.GetBytes(0))) {
				return null;
			}
			List<int> trig_wave_data = data_match_form_stringBulider(sb, "s");
			return trig_wave_data;
		}
		void Fun_找正常波形并加入列表(List<int> data, List<int[]> nomal_wave) {
			//存零点位置  即 一个正常周期的起始位置
			int[] lingdian1 = new int[80];
			//零点位置的标签
			int lingdian = 0;
			for (int i = 0; i < data.Count - 1; i++) {
				if (data[i] >= 0 && data[i + 1] <= 0 && Math.Abs(data[i + 1]) < 10000) {
					if (lingdian < lingdian1.Length) {
						lingdian1[lingdian] = i;
						lingdian++;
					}
					else {
						break;
					}
				}
			}
			int len = 0;
			//获取正常波形数据 并加入列表            
			for (int i = 0; i < lingdian1.Length - 2; i++) {
				for (int j = i + 1; j < lingdian1.Length - 1; j++) {
					if (Math.Abs(lingdian1[i] - lingdian1[j]) >= Page_Max_count / 3 * 18 && Math.Abs(lingdian1[i] - lingdian1[j]) <= Page_Max_count / 3 * 22) {
						len = Math.Abs(lingdian1[i] - lingdian1[j]);
						int[] temp = new int[len];
						for (int k = lingdian1[i]; k < lingdian1[i] + len; k++) {
							temp[k - lingdian1[i]] = data[k];
						}
						nomal_wave.Add(temp);
						i = j;
						break;
					}
				}
			}
		}
		double Fun_计算周期两点之间最大差异(List<int[]> sample, int lenth) {
			if (sample.Count == 0) {
				MessageBox.Show("数据异常");
				return 0;
			}
			double rate = 0;
			int[] buffer = new int[(lenth + 100) * (sample.Count + 1)];
			int sum = 0;
			for (int i = 0; i < sample.Count; i++) {
				for (int j = 0; j < sample[i].Length - 1; j++) {
					buffer[i * sample[i].Length + j] = Math.Abs(sample[i][j] - sample[i][j + 1]);
				}
			}
			Array.Sort(buffer);
			for (int i = buffer.Length - Page_Max_count*4; i < buffer.Length-10; i++) {
				sum += buffer[i];
			}
			rate = (sum+0.1) /(Page_Max_count*4-10);
			return rate;
		}


		void Fun_找一段曲线里的变化点(List<int> Var_原始曲线, double double_变化率, double double_误差比例, int int_最小持续变化时间对应的点, int int_最大持续不变时间对应的点, int int_峰值, int[] Var_存放变化点的位置信息) {
			bool find_start = false;
			//找到触发结束标记
			bool find_end = true;
			//变化点标签
			int index = 0;
			//变化点1差值
			double xxx = 0;
			//变化点后续差值
			double yyy = 0;
			double var_上升还是下降 = 0;
			double rate_阈值 = double_变化率 * (double_误差比例 + 1.0 / double_变化率);
			int 上下沿相隔个数 = 4;
			int 差值个数 = 1;
			if (Page_Max_count == 600) {
				上下沿相隔个数 = 12;
				差值个数 = 5;
				int_最小持续变化时间对应的点 = int_最小持续变化时间对应的点 / 2;
			}
			int _flag = 0;

			for (int i = 10; i < Var_原始曲线.Count - 100; i++) {
				yyy = 0;
				var_上升还是下降 = Var_原始曲线[i] - Var_原始曲线[i + 上下沿相隔个数];
				if (var_上升还是下降 == 0) {
					continue;
				}
				xxx = Var_原始曲线[i] - Var_原始曲线[i + 差值个数];
				//如果波形变化超过理论变化率开始分析是否是变化点
				if (Math.Abs(xxx) >= rate_阈值) {
					//开始
					#region 开始
					if (!find_start) {
						//分析后面的点 如果全部是变化点 那么 这个点是变化点
						for (int j = i + 差值个数; j < i + int_最小持续变化时间对应的点; j++) {
							if (var_上升还是下降 < 0) {
								yyy = Var_原始曲线[i] + (j - i - 差值个数) * (double_变化率) - Var_原始曲线[j];
							}
							else if (var_上升还是下降 > 0) {
								yyy = Var_原始曲线[i] - (j - i - 差值个数) * (double_变化率) - Var_原始曲线[j];
							}
							//
							if (Math.Abs(yyy) <= double_变化率) {
								i = j;
								break;
							}
							if (j >= i + int_最小持续变化时间对应的点 - 2) {
								if (index < 10) {
									Var_存放变化点的位置信息[index++] = i;
									i = j;
									find_start = true;
									find_end = false;
									break;
								}
							}
						}
					}
					#endregion
				}
				else {
					if (!find_end) {
						_flag = 0;
						#region 结束
						//分析后面的点 如果全部是变化点 那么 这个点是变化点
						int count = i + Page_Max_count * 100;
						if (Var_原始曲线.Count - count <= 差值个数*5) {
							count = Var_原始曲线.Count - 差值个数*5-1;
						}
						for (int j = i + 差值个数; j < count; j++) {
							for (int num = 0; num < 差值个数*4; num++) {
								if (Math.Abs(Var_原始曲线[j] - Var_原始曲线[j + num + 差值个数]) >= (rate_阈值 + num * rate_阈值)) {
									_flag++;
								}
							}//相邻的num个 点 来比较变化 

							if (_flag >= 差值个数*4-3) {
								i = j + 差值个数 * 4;
								break;
							}//这个点还在变化  此次分析终止 重新找

							if (j >= i + int_最大持续不变时间对应的点 - 差值个数*4) {
								if (index < 10) {
									int 变化区间 = i - Var_存放变化点的位置信息[index - 1];
									if (变化区间 >= Page_Max_count / 3/*大于1ms并且小于150ms*/ && 变化区间 <= Page_Max_count * 50) {
										Var_存放变化点的位置信息[index++] = i;
									}
									else {
										index--;
										Var_存放变化点的位置信息[index] = 0;
									}
									i = j;
									find_start = false;
									find_end = true;
									break;
								}
							}//如果在 最大持续不变区间里 都没有变化点了  那么这个点就是 变化结束点
						}
						#endregion
					}
				}
			}
		}
		int[] Fun_文件分析变化点(List<int> Var_原始曲线, double double_变化率, double double_误差比例, int int_最小持续变化时间对应的点, int int_最大持续不变时间对应的点, int int_峰值) {
			bool find_start = false;
			//找到触发结束标记
			bool find_end = true;
			//变化点标签
			int index = 0;
			//变化点1差值
			double xxx = 0;
			//变化点后续差值
			double yyy = 0;
			double var_上升还是下降 = 0;
			int[] Var_存放变化点的位置信息 = new int[10];
			double rate_阈值 = double_变化率 * (double_误差比例+1.0/double_变化率);
			int 上下沿相隔个数 = 4;
			int 差值个数 = 1;
			if (Page_Max_count == 600) {
				上下沿相隔个数 = 12;
				差值个数 = 5;
				int_最小持续变化时间对应的点 = int_最小持续变化时间对应的点 / 2;
			}
			int _flag = 0;


			for (int i = 10; i < Var_原始曲线.Count - 100; i++) {
				yyy = 0;
				var_上升还是下降 = Var_原始曲线[i] - Var_原始曲线[i + 上下沿相隔个数];
				if (var_上升还是下降 == 0) {
					//i++;
					continue;
				}
				xxx = Var_原始曲线[i] - Var_原始曲线[i + 差值个数];
				//如果波形变化超过理论变化率开始分析是否是变化点
				if (Math.Abs(xxx) >= rate_阈值) {
					//开始
					#region 开始
					if (!find_start) {
						//分析后面的点 如果全部是变化点 那么 这个点是变化点
						for (int j = i + 差值个数; j < i + int_最小持续变化时间对应的点; j++) {
							if (var_上升还是下降 < 0) {
								yyy = Var_原始曲线[i] + (j - i - 差值个数) * (double_变化率) - Var_原始曲线[j];
							}
							else if (var_上升还是下降 > 0) {
								yyy = Var_原始曲线[i] - (j - i - 差值个数) * (double_变化率) - Var_原始曲线[j];
							}
							//
							if (Math.Abs(yyy) <= double_变化率) {
								i = j;
								break;
							}
							if (j >= i + int_最小持续变化时间对应的点 - 2) {
								if (index < 10) {
									Var_存放变化点的位置信息[index++] = i;
									i = j;
									find_start = true;
									find_end = false;
									break;
								}
							}
							if (find_start) {
								break;
							}
						}
					}
					#endregion
				}
				else {
					if (!find_end) {
						#region 结束
						_flag = 0;
						//分析后面的点 如果全部是变化点 那么 这个点是变化点
						int count = i + Page_Max_count * 100;
						if (Var_原始曲线.Count - count <= 差值个数 * 5) {
							count = Var_原始曲线.Count - 差值个数 * 5 - 1;
						}
						for (int j = i + 差值个数; j < count; j++) {
							for (int num = 0; num < 差值个数 * 4; num++) {
								if (Math.Abs(Var_原始曲线[j] - Var_原始曲线[j + num + 差值个数]) >= (rate_阈值 + num * rate_阈值)) {
									_flag++;
								}
							}

							if (_flag >= 差值个数 * 4-2) {
								i = j + 差值个数 * 4;
								break;
							}
							if (j >= i + int_最大持续不变时间对应的点 - 差值个数*4) {
								if (index < 10) {
									int 变化区间 = i - Var_存放变化点的位置信息[index - 1];
									if (变化区间 >= Page_Max_count / 3 && 变化区间 <= Page_Max_count * 50) {
										Var_存放变化点的位置信息[index++] = i;
									}
									else {
										index--;
										Var_存放变化点的位置信息[index] = 0;
									}
									i = j;
									find_start = false;
									find_end = true;
									break;
								}
							}
						}
						#endregion
					}
				}
			}
			for (int i = 0; i < Var_存放变化点的位置信息.Length; i++) {
				if (Var_存放变化点的位置信息[i] == 0) {
					if ((i - 1) % 2 == 0) {
						Var_存放变化点的位置信息[i - 1] = 0;
						break;
					}
					break;
				}
			}
			return Var_存放变化点的位置信息;
		}


		void Fun_变化点位置第二次优化(List<int> Var_原始曲线, double 变化, int[] Var_存放变化点的位置信息) {

			int 前后范围 = Page_Max_count * 1;
			int 间隔的点 = Page_Max_count / 30;
			int 变化趋势 = int.Parse(test_parameter._46Peak_value) *2/ (6000 / Page_Max_count + 50);
			int 变化_flag0 = 0;
			int 变化_flag1 = 0;
			int 前段变化次数 = 0;
			int 后段变化次数 = 0;
			int len = 0;
			try {
				//判断有效值个数
				for (int s = 0; s < Var_存放变化点的位置信息.Length; s++) {
					if (Var_存放变化点的位置信息[s] == 0) {
						break;
					}
					len++;
				}
				for (int count = 0; count < len - 1; count += 2) {
					前段变化次数 = 0;
					后段变化次数 = 0;
					//前段变化
					for (int i = Var_存放变化点的位置信息[count]; i > Var_存放变化点的位置信息[count] - 前后范围; i--) {
						if ((i - 2 * 间隔的点) <= 0) {
							continue;
						}
						if ((Var_原始曲线[i] - Var_原始曲线[i - 间隔的点] > 变化趋势 && Var_原始曲线[i - 间隔的点] - Var_原始曲线[i - 2 * 间隔的点] < -变化趋势) || (Var_原始曲线[i] - Var_原始曲线[i - 间隔的点] < -变化趋势 && Var_原始曲线[i - 间隔的点] - Var_原始曲线[i - 2 * 间隔的点] > 变化趋势)) {
							前段变化次数++;
							变化_flag0 = i;
							for (int j = i - 1; j > i - 间隔的点; j--) {
								if (Math.Abs((Var_原始曲线[j] - Var_原始曲线[j - 1] + 0.0001) / (Var_原始曲线[j - 1] - Var_原始曲线[j - 2]) + 0.0001) > 4 || (Math.Abs((Var_原始曲线[j] - Var_原始曲线[j - 1] + 0.0001) / (Var_原始曲线[j - 1] - Var_原始曲线[j - 2] + 0.0001))) < 0.25) {
									变化_flag0 = j - 2;
									// break;
								}
							}
						}
					}
					//后段变化
					for (int i = Var_存放变化点的位置信息[count + 1]; i < Var_存放变化点的位置信息[count + 1] + 前后范围; i++) {
						if ((i + 2 * 间隔的点) >= Var_原始曲线.Count) {
							continue;
						}
						if ((Var_原始曲线[i] - Var_原始曲线[i + 间隔的点] > 变化趋势 && Var_原始曲线[i + 间隔的点] - Var_原始曲线[i + 2 * 间隔的点] < -变化趋势) || (Var_原始曲线[i] - Var_原始曲线[i + 间隔的点] < -变化趋势 && Var_原始曲线[i + 间隔的点] - Var_原始曲线[i + 2 * 间隔的点] > 变化趋势)) {
							后段变化次数++;
							变化_flag1 = i;
							for (int j = i + 1; j < i + 间隔的点; j++) {
								if (Math.Abs((Var_原始曲线[j] - Var_原始曲线[j + 1] + 0.0001) / (Var_原始曲线[j + 1] - Var_原始曲线[j + 2] + 0.0001)) > 4 || Math.Abs((Var_原始曲线[j] - Var_原始曲线[j + 1] + 0.0001) / (Var_原始曲线[j + 1] - Var_原始曲线[j + 2] + 0.0001)) < 0.25) {
									变化_flag1 = j + 2;
									// break;
								}
							}
						}
					}
					if (前段变化次数 >= 3) {
						Var_存放变化点的位置信息[count] = 变化_flag0;
					}
					if (后段变化次数 >= 3) {
						Var_存放变化点的位置信息[count + 1] = 变化_flag1;
					}
				}
			}
			catch (Exception e) {
				MessageBox.Show(e.Message);
			}

		}


		bool Fun_抓触发(List<int> Var_原始曲线, double double_变化率, double double_误差比例, int int_最小持续变化时间对应的点, int int_最大持续不变时间对应的点, int int_峰值) {
			double rate = double_变化率 * (double_误差比例);
			double yyy = 0;
			double xxx = 0;
			int var_上升还是下降 = 0;
			for (int i = 0; i < Var_原始曲线.Count - Page_Max_count / 3; i++) {
				//if (Math.Abs(Var_原始曲线[i]) >= (int_峰值 * 1.2))
				//{
				//    for (int j = i + 1; j < i + int_最小持续变化时间对应的点; j++)
				//    {
				//        if (Math.Abs(Var_原始曲线[j]) <= (int_峰值 * 1.2))
				//        {
				//            i = j;
				//            break;
				//        }
				//        if (j == i + int_最小持续变化时间对应的点 - 5)
				//        {
				//            return true;
				//        }
				//    }
				//}
				yyy = 0;
				var_上升还是下降 = Var_原始曲线[i] - Var_原始曲线[i + Page_Max_count / 30];
				xxx = Var_原始曲线[i] - Var_原始曲线[i + 1];
				//如果波形变化超过理论变化率开始分析是否是变化点
				if (Math.Abs(xxx) > rate) {
					//分析后面的点 如果全部是变化点 那么 这个点是变化点
					for (int j = i + 1; j < i + int_最小持续变化时间对应的点; j++) {
						if (var_上升还是下降 < 0) {
							yyy = Var_原始曲线[i] + (j - i) * (rate) - Var_原始曲线[j + 1];
						}
						else if (var_上升还是下降 > 0) {
							yyy = Var_原始曲线[i] - (j - i) * (rate) - Var_原始曲线[j + 1];
						}
						if (Math.Abs(yyy) < rate) {
							i = j;
							break;
						}
						if (j == i + int_最小持续变化时间对应的点 - 2) {
							return true;
						}
					}
				}
			}
			return false;
		}


	
		bool Fun_抓直流触发(List<int> Var_原始曲线, double double_变化率, double int_峰值, double double_误差比例, int int_最小持续变化时间对应的点, int int_最大持续不变时间对应的点) {
			double rate = double_变化率 * double_误差比例;
			double yyy = 0;
			double xxx = 0;
			for (int i = 0; i < Var_原始曲线.Count - 100; i++) {
				if (Math.Abs(Var_原始曲线[i]) >= (int_峰值 * 1.5)) {
					for (int j = i + 1; j < i + int_最小持续变化时间对应的点; j++) {
						if (Math.Abs(Var_原始曲线[j]) <= (int_峰值 * 1.5)) {
							i = j;
							break;
						}
						if (j == i + int_最小持续变化时间对应的点 - 4) {
							return true;
						}
					}
				}
				yyy = 0;
				xxx = Var_原始曲线[i] - Var_原始曲线[i + 1];
				//如果波形变化超过理论变化率开始分析是否是变化点
				if (Math.Abs(xxx) > rate) {
					//分析后面的点 如果全部是变化点 那么 这个点是变化点
					for (int j = i + 1; j < i + int_最小持续变化时间对应的点; j++) {
						yyy = Var_原始曲线[j] - Var_原始曲线[j + 1];
						if (Math.Abs(yyy) <= rate) {
							i = j;
							break;
						}
						if (j == i + int_最小持续变化时间对应的点 - 4) {
							return true;
						}
					}
				}
			}
			return false;
		}


		void Fun_找直流一段曲线里的变化起始点和结束点(List<int> Var_原始曲线, double double_变化率, double double_误差比例, int int_最小持续变化时间对应的点, int int_最大持续不变时间对应的点, int[] Var_存放变化点的位置信息) {
			bool find_start = false;
			//找到触发结束标记
			//变化点1差值
			double xxx = 0;
			//变化点后续差值
			double yyy = 0;
			double rate = double_变化率 * (double_误差比例);
			for (int i = 0; i < Var_原始曲线.Count - 100; i++) {
				yyy = 0;
				xxx = Var_原始曲线[i] - Var_原始曲线[i + 1];
				//如果波形变化超过理论变化率开始分析是否是变化点
				if (Math.Abs(xxx) >= rate) {
					//开始
					#region 开始
					if (!find_start) {
						//分析后面的点 如果全部是变化点 那么 这个点是变化点
						for (int j = i + 1; j < Var_原始曲线.Count - 100; j++) {
							yyy = Var_原始曲线[i] - Var_原始曲线[j];
							if (j > i + int_最小持续变化时间对应的点) {
								if (Math.Abs(yyy) <= rate) {
									Var_存放变化点的位置信息[0] = i;
									Var_存放变化点的位置信息[1] = j;
									return;
								}
							}
							if (Math.Abs(yyy) <= rate && j < i + int_最小持续变化时间对应的点) {
								i = j;
								break;
							}
						}
					}
					#endregion
				}
			}
		}


		void Fun_找直流过渡和桥接点(List<int> Var_原始曲线, double double_变化率, int int_最小持续变化时间对应的点, int int_最大持续不变时间对应的点, int[] Var_存放变化点的位置信息) {
			bool find_start = false;
			//找到触发结束标记
			bool find_end = false;
			//变化点标签
			int index = 2;
			//变化点1差值
			double xxx = 0;
			//变化点后续差值
			double yyy = 0;
			double rate = double_变化率;
			if (rate <= 3) {
				rate = 3;
			}
			#region 找桥接起始点
			for (int i = Page_Max_count / 3; i < Var_原始曲线.Count - Page_Max_count / 3; i++) {
				yyy = 0;
				xxx = Var_原始曲线[i] - Var_原始曲线[i + 2];
				//找到第一段最平的地方
				if (Math.Abs(xxx) <= rate && !find_start) {
					find_start = true;
				}
				//如果变化率 突然变大 并且后面一定的都变化 这个点是第一个位置
				if (find_start && Math.Abs(xxx) > rate * 5 && !find_end) {
					//分析后面的点 如果全部是变化点 那么 这个点是变化点
					for (int j = i + 2; j < i + int_最小持续变化时间对应的点; j++) {
						yyy = Var_原始曲线[i] - Var_原始曲线[j];
						if (Math.Abs(yyy) <= rate) {
							i = j;
							break;
						}
						if (j == i + int_最小持续变化时间对应的点 - 2) {
							if (index < 10) {
								Var_存放变化点的位置信息[2] = i;
								i = j;
								find_start = true;
								find_end = true;
								break;
							}
						}
					}
					if (find_end) {
						break;
					}
				}
			}
			#endregion
			#region 找桥接结束点
			find_start = false;
			find_end = false;
			for (int i = Var_原始曲线.Count - ((int)(Page_Max_count / 1.5)); i > Page_Max_count / 3; i--) {
				yyy = 0;
				xxx = Var_原始曲线[i] - Var_原始曲线[i - 3];
				//找到第一段最平的地方
				if (Math.Abs(xxx) <= rate && !find_start) {
					find_start = true;
				}
				//如果变化率 突然变大 并且后面一定的都变化 这个点是第一个位置
				if (find_start && Math.Abs(xxx) > rate * 5 && !find_end) {
					//分析后面的点 如果全部是变化点 那么 这个点是变化点
					for (int j = i - 2; j > i - int_最小持续变化时间对应的点; j--) {
						yyy = Var_原始曲线[i] - Var_原始曲线[j];
						if (Math.Abs(yyy) <= rate) {
							i = j;
							break;
						}
						if (j == i - int_最小持续变化时间对应的点 + 2) {
							if (index < 10) {
								Var_存放变化点的位置信息[3] = i;
								i = j;
								return;
								//find_start = true;
								//find_end = false;
								//break;
							}
						}
					}
				}
			}
			#endregion
		}


		double Fun_找直流正常的值(Queue<byte[]> var_原始队列数据, int var_需要的数据个数, int Phase) {
			List<int> var_存放数据的列表 = new List<int>();
			for (int i = 0; i < var_需要的数据个数 / 600; i++) {
				byte[] temp = var_原始队列数据.Dequeue();
				for (int j = 0; j < temp.Length; j += 2) {
					var_存放数据的列表.Add(BitConverter.ToInt16(temp, j));
				}
			}
			if (Phase == 1) {
				var_A相变化率 = Fun_找直流变化率(var_存放数据的列表);
			}
			if (Phase == 2) {
				var_B相变化率 = Fun_找直流变化率(var_存放数据的列表);
			}
			if (Phase == 3) {
				var_C相变化率 = Fun_找直流变化率(var_存放数据的列表);
			}
			return fun_找直流正常值(var_存放数据的列表);
		}


		double fun_找直流正常值(List<int> source) {
			int[] cache = new int[source.Count];
			source.CopyTo(cache, 0);
			Array.Sort(cache);
			int sum = 0;
			for (int i = cache.Length - 100; i < cache.Length; i++) {
				sum += cache[i];
			}
			return sum / 100;
		}


		double Fun_找直流变化率(List<int> var_存放数据的列表) {
			double rate = 0;
			int[] buffer = new int[var_存放数据的列表.Count / 2];
			int sum = 0;
			for (int i = 0; i < buffer.Length; i++) {
				buffer[i] = Math.Abs(var_存放数据的列表[i] - var_存放数据的列表[i + 1]);
			}
			Array.Sort(buffer);
			for (int i = buffer.Length - 320; i < buffer.Length - 20; i++) {
				sum += buffer[i];
			}
			rate = sum / 300.0;
			return rate;
		}


		double Fun_找直流触发段变化率(List<int> var_存放数据的列表) {
			double rate = 0;
			if (var_存放数据的列表.Count - 5 < 10) {
				return 1;
			}
			int[] buffer = new int[var_存放数据的列表.Count - 5];
			int sum = 0;
			for (int i = 0; i < buffer.Length; i++) {
				buffer[i] = Math.Abs(var_存放数据的列表[i] - var_存放数据的列表[i + 1]);
			}
			Array.Sort(buffer);
			for (int i = buffer.Length / 4; i < buffer.Length / 2; i++) {
				sum += buffer[i];
			}
			try {
				rate = sum / (buffer.Length / 2 - buffer.Length / 4);
			}
			catch {

			}
			return rate;
		}
		#endregion

		#region 绘制数据线
		int Max负值 = 0;
		double Max = 0;
		double Multiple = 50.0;
		private bool DrawNewLine(List<int> data, Color color, FastLine line, Steema.TeeChart.WPF.TChart Chart) {
			if (data == null) {
				return false;
			}
			Max = 0;
			#region AC
			if (test_parameter._24IsACMeasurment) {
				T_row1.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row2.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row3.Height = new GridLength(0.3 * T_grid.ActualHeight);
				T_row4.Height = new GridLength(0.3 * T_grid.ActualHeight);
				Chart.Axes.Left.Inverted = false;
				Chart.Axes.Left.Title.Text = "电流(mA)";
				double rate = 3.0 / Page_Max_count;
				line.YValues.Clear();
				line.XValues.Clear();
				Chart.Chart.Axes.Left.Automatic = false;


				StringBuilder sb1 = new StringBuilder();
				for (int i = 0; i < data.Count; i += Page_Max_count / 75) {
					if (Max < data[i] * 1.5) {
						Max = data[i] * 1.5;
					}
					line.Add((i - Page_Max_count * 50) * rate, data[i] / Multiple);
				}
				Tchart1.Chart.Axes.Left.SetMinMax(-(Max / Multiple), (Max / Multiple));
				try {
					Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
					chartPen.Width = double.Parse(tb_LineWidth.Text);
					line.LinePen = chartPen;
				}
				catch (Exception error) {
					MessageBox.Show("请输入数字!\r\n" + error.Message, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				Chart.Chart.Axes.Left.SetMinMax((-Max / Multiple) * 1.5, (Max / Multiple) * 1.5);

				line.Active = true;
				Chart.AutoRepaint = true;
				line.Color = color;
				return true;
			}
			#endregion
			#region DC
			else {
				T_row1.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row2.Height = new GridLength(0.1 * T_grid.ActualHeight);
				T_row3.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row4.Height = new GridLength(0.5 * T_grid.ActualHeight);
				Chart.Axes.Left.Inverted = true;
				Chart.Chart.Axes.Left.Automatic = true;
				Chart.Axes.Left.Title.Text = "电压(V)";
				double rate = 3.0 / 60.0;
				line.YValues.Clear();
				line.XValues.Clear();
				Max = 0;
				for (int i = 0; i < data.Count; i += 1) {
					if (Max < data[i] * 1.5) {
						Max = data[i] * 1.5;
					}
					double Yvalue = data[i] / 1000.0;

					line.Add((i - Page_Max_count * 50) * rate, Yvalue);
				}
				try {
					Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
					chartPen.Width = double.Parse(tb_LineWidth.Text);
					line.LinePen = chartPen;
				}
				catch (Exception error) {
					MessageBox.Show("请输入数字!\r\n" + error.Message, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				Chart.Chart.Axes.Left.SetMinMax(0, (Max / 1000.0));
				line.Active = true;
				line.Color = color;
				return true;
			}
			#endregion
		}
		private bool Fun_绘制触发曲线_Trig_Line(List<int> data, Color color, FastLine line, int 变化点位置) {
			if (变化点位置 < Page_Max_count * 50) {
				变化点位置 = Page_Max_count * 50;
			}
			Max = 0;
			if (test_parameter._24IsACMeasurment) {
				#region AC
				T_row1.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row2.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row3.Height = new GridLength(0.3 * T_grid.ActualHeight);
				T_row4.Height = new GridLength(0.3 * T_grid.ActualHeight);
				Tchart1.Axes.Left.Inverted = false;
				int data_length = data.Count;
				double rate = 3.0 / Page_Max_count;
				line.YValues.Clear();
				line.XValues.Clear();
				Tchart1.Axes.Left.Title.Text = "电流(mA)";
				line.Color = color;
				Max = 0;
				for (int i = 变化点位置 - Page_Max_count * 50; i < 变化点位置 + Page_Max_count * 200; i += Page_Max_count / 75) {
					if (Max <= data[i] * 1.5) {
						Max = data[i] * 1.5;
					}
					line.Add((i - 变化点位置) * rate, data[i] / Multiple);
				}
				Tchart1.Axes.Left.SetMinMax(-(Max / Multiple) * 1.5, (Max / Multiple) * 1.5);
				line.Active = true;
				return true;
				#endregion
			}
			else {
				#region DC
				T_row1.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row2.Height = new GridLength(0.1 * T_grid.ActualHeight);
				T_row3.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row4.Height = new GridLength(0.5 * T_grid.ActualHeight);
				Tchart1.Axes.Left.Inverted = true;
				Tchart1.Axes.Left.Title.Text = "电压(V)";
				double rate = 3.0 / 60.0;
				line.YValues.Clear();
				line.XValues.Clear();
				line.Color = color;
				Max = 0;
				for (int i = 变化点位置 - Page_Max_count * 50; i < 变化点位置 + Page_Max_count * 200; i += 1) {
					if (Max < data[i] * 1.5) {
						Max = data[i] * 1.5;
					}
					line.Add((i - 变化点位置) * rate, data[i] / 1000.0);
				}
				Tchart1.Axes.Left.SetMinMax(0, (Max / 1000));
				line.Active = true;
				return true;
				#endregion
			}
		}
		private bool Fun_绘制触发曲线_Trig_Line(List<int> data, Color color, FastLine line) {
			if (test_parameter._24IsACMeasurment) {
				#region AC
				T_row1.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row2.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row3.Height = new GridLength(0.3 * T_grid.ActualHeight);
				T_row4.Height = new GridLength(0.3 * T_grid.ActualHeight);
				Tchart1.Axes.Left.Inverted = false;
				int data_length = data.Count;
				double rate = 3.0 / Page_Max_count;
				line.YValues.Clear();
				line.XValues.Clear();
				Tchart1.Axes.Left.Title.Text = "电流(mA)";
				line.Color = color;
				for (int i = 0; i < data.Count; i += Page_Max_count / 75) {
					if (Max < data[i]) {
						Max = data[i] * 2;
					}
					line.Add((i - Page_Max_count * 50) * rate, data[i] / Multiple);
				}
				Tchart1.Chart.Axes.Left.SetMinMax(-(Max / Multiple), (Max / Multiple));
				line.Active = true;
				return true;
				#endregion
			}
			else {
				#region DC
				T_row1.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row2.Height = new GridLength(0.1 * T_grid.ActualHeight);
				T_row3.Height = new GridLength(0.2 * T_grid.ActualHeight);
				T_row4.Height = new GridLength(0.5 * T_grid.ActualHeight);
				Tchart1.Axes.Left.Inverted = true;
				int peak_value = 0;
				try {
					peak_value = int.Parse(test_parameter._46Peak_value);
				}
				catch {
					MessageBox.Show("峰值为空:" + test_parameter._46Peak_value, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				Tchart1.Chart.Axes.Left.Automatic = false;
				Tchart1.Axes.Left.Title.Text = "电压(V)";
				double rate = 3.0 / 60.0;
				line.YValues.Clear();
				line.XValues.Clear();
				line.Color = color;
				for (int i = 0; i < data.Count; i++) {
					if (Max < data[i] * 1.5) {
						Max = data[i] * 1.5;
					}
					line.Add((i - Page_Max_count * 50) * rate, data[i] / 1000.0);
				}
				Tchart1.Chart.Axes.Left.SetMinMax(0, (Max / 1000));
				line.Active = true;
				return true;
				#endregion
			}
		}

		private bool Fun_绘制线条(int[] data, Color color, FastLine line) {
			if (test_parameter._24IsACMeasurment) {
				#region AC
				Tchart1.Axes.Left.Inverted = false;
				double rate = 3.0 / Page_Max_count;
				Tchart1.Chart.Axes.Left.Automatic = false;
				line.YValues.Clear();
				line.XValues.Clear();
				line.Color = color;
				Tchart1.Axes.Left.Title.Text = "电流(mA)";
				List<double> tempX = new List<double>();
				List<double> tempY = new List<double>();
				for (int j = 0; j < data.Length; j += Page_Max_count / 15) {
					if (Max < data[j]) {
						Max = data[j] * 2;
					}
					tempX.Add(j * rate);
					tempY.Add(data[j] / Multiple);
				}
				line.Add(tempX.ToArray(), tempY.ToArray());
				Tchart1.Chart.Axes.Left.SetMinMax(-(Max / Multiple), (Max / Multiple));
				Tchart1.Chart.Axes.Bottom.SetMinMax(0, data.Length * rate);
				line.Active = true;
				Tchart1.AutoRepaint = true;
				return true;
				#endregion
			}
			if (test_parameter._25IsDCMeasurment) {
				#region DC
				Tchart1.Axes.Left.Inverted = true;
				double rate = 3.0 / Page_Max_count;
				Tchart1.Chart.Axes.Left.SetMinMax(0, 25);
				line.YValues.Clear();
				line.XValues.Clear();
				line.Color = color;
				Tchart1.Axes.Left.Title.Text = "电压(V)";
				for (int j = 0; j < data.Length; j += Page_Max_count / 30) {
					line.Add((j) * rate, data[j] / 1000.0);
				}
				Tchart1.Chart.Axes.Bottom.SetMinMax(0, data.Length * rate);
				line.Active = true;
				Tchart1.AutoRepaint = true;
				return true;
				#endregion
			}
			return true;
		}

		#endregion

		#region 鼠标坐标获取与设置
		/// <summary>   
		/// 设置鼠标的坐标   
		/// </summary>   
		/// <param name="x">横坐标</param>   
		/// <param name="y">纵坐标</param>   
		[System.Runtime.InteropServices.DllImport("User32")]
		public extern static void SetCursorPos(int x, int y);
		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		public static extern bool GetCursorPos(out POINT pt);
		public struct POINT {
			public int X;
			public int Y;
			public POINT(int x, int y) {
				this.X = x;
				this.Y = y;
			}
		}
		#endregion

		#region 游标位置设置
		void Cursor_Postion_Set(string Phase_Name, bool Show_or_Hide) {
			set_cursor_position(cursor_collection[Tchart1.Name + "A1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "A2"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "B1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "B2"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "C1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "C2"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], 0, false);
			double rate = 3.0 / Page_Max_count;
			if (test_parameter._25IsDCMeasurment) {
				switch (Phase_Name) {
					case "A":
						set_cursor_position(cursor_collection[Tchart1.Name + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
						break;
					case "B":
						set_cursor_position(cursor_collection[Tchart1.Name + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
						break;
					case "C":
						set_cursor_position(cursor_collection[Tchart1.Name + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						break;
					default:
						set_cursor_position(cursor_collection[Tchart1.Name + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[Tchart1.Name + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						break;
				}
			}
			else {
				switch (Phase_Name) {
					case "A":
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[Tchart1.Name + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[Tchart1.Name + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
						}

						break;
					case "B":
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[Tchart1.Name + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[Tchart1.Name + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
						}

						break;
					case "C":
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[Tchart1.Name + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[Tchart1.Name + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						}

						break;
					default:
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[Tchart1.Name + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[Tchart1.Name + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[Tchart1.Name + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						}

						break;
				}
				if (transformer_parameter._13TransformerWindingConnMethod != "三角形接法") {
					cursor_collection_DC[Tchart1.Name + "A1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "A2_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "B1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "B2_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "C1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "C2_DC"].Active = false;

				}
			}

		}
		void Cursor_Postion_Set(string Phase_Name, string TchartName, bool Show_or_Hide) {
			set_cursor_position(cursor_collection[TchartName + "A1"], 0, false);
			set_cursor_position(cursor_collection[TchartName + "A2"], 0, false);
			set_cursor_position(cursor_collection_DC[TchartName + "A1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[TchartName + "A2_DC"], 0, false);
			set_cursor_position(cursor_collection[TchartName + "B1"], 0, false);
			set_cursor_position(cursor_collection[TchartName + "B2"], 0, false);
			set_cursor_position(cursor_collection_DC[TchartName + "B1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[TchartName + "B2_DC"], 0, false);
			set_cursor_position(cursor_collection[TchartName + "C1"], 0, false);
			set_cursor_position(cursor_collection[TchartName + "C2"], 0, false);
			set_cursor_position(cursor_collection_DC[TchartName + "C1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[TchartName + "C2_DC"], 0, false);
			double rate = 3.0 / Page_Max_count;
			if (test_parameter._25IsDCMeasurment) {
				switch (Phase_Name) {
					case "A":
						set_cursor_position(cursor_collection[TchartName + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
						break;
					case "B":
						set_cursor_position(cursor_collection[TchartName + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
						break;
					case "C":
						set_cursor_position(cursor_collection[TchartName + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						break;
					default:
						set_cursor_position(cursor_collection[TchartName + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection[TchartName + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
						set_cursor_position(cursor_collection_DC[TchartName + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						break;
				}
			}
			else {
				switch (Phase_Name) {
					case "A":
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[TchartName + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[TchartName + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
						}

						break;
					case "B":
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[TchartName + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[TchartName + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
						}

						break;
					case "C":
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[TchartName + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[TchartName + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						}

						break;
					default:
						if (transformer_parameter._12Triangle_method) {
							set_cursor_position(cursor_collection[TchartName + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "A1_DC"], int.Parse(test_parameter._50Cursor_A1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "A2_DC"], int.Parse(test_parameter._51Cursor_A2_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "B1_DC"], int.Parse(test_parameter._52Cursor_B1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "B2_DC"], int.Parse(test_parameter._53Cursor_B2_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "C1_DC"], int.Parse(test_parameter._54Cursor_C1_DC) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection_DC[TchartName + "C2_DC"], int.Parse(test_parameter._55Cursor_C2_DC) * rate, Show_or_Hide);
						}
						else {
							set_cursor_position(cursor_collection[TchartName + "A1"], int.Parse(test_parameter._40Cursor_A1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "A2"], int.Parse(test_parameter._41Cursor_A2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "B1"], int.Parse(test_parameter._42Cursor_B1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "B2"], int.Parse(test_parameter._43Cursor_B2) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "C1"], int.Parse(test_parameter._44Cursor_C1) * rate, Show_or_Hide);
							set_cursor_position(cursor_collection[TchartName + "C2"], int.Parse(test_parameter._45Cursor_C2) * rate, Show_or_Hide);
						}

						break;
				}
				if (transformer_parameter._13TransformerWindingConnMethod != "三角形接法") {
					cursor_collection_DC[Tchart1.Name + "A1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "A2_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "B1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "B2_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "C1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "C2_DC"].Active = false;

				}
			}
		}
		void Tchart_ShowArea_Set(string Phase_Name) {
			try {
				Tchart1.Header.Text = "测试波形:" + test_parameter._48Access_position;
			}
			catch (Exception e) {
				MessageBox.Show(e.Message);
			}
			double rate = 3.0 / Page_Max_count;
			if (test_parameter._25IsDCMeasurment) {
				switch (Phase_Name) {
					case "A":
						double offset = int.Parse(test_parameter._41Cursor_A2) * rate - int.Parse(test_parameter._40Cursor_A1) * rate;
						Tchart1.Axes.Bottom.SetMinMax(int.Parse(test_parameter._40Cursor_A1) * rate - offset / 4, int.Parse(test_parameter._41Cursor_A2) * rate + offset / 4);
						break;
					case "B":
						double offsetb = int.Parse(test_parameter._43Cursor_B2) * rate - int.Parse(test_parameter._42Cursor_B1) * rate;
						Tchart1.Axes.Bottom.SetMinMax(int.Parse(test_parameter._42Cursor_B1) * rate - offsetb / 4, int.Parse(test_parameter._43Cursor_B2) * rate + offsetb / 4);
						break;
					case "C":
						double offsetc = int.Parse(test_parameter._45Cursor_C2) * rate - int.Parse(test_parameter._44Cursor_C1) * rate;
						Tchart1.Axes.Bottom.SetMinMax(int.Parse(test_parameter._44Cursor_C1) * rate - offsetc / 4, int.Parse(test_parameter._45Cursor_C2) * rate + offsetc / 4);
						break;
					default:
						double min = int.Parse(test_parameter._40Cursor_A1);
						if (min >= int.Parse(test_parameter._42Cursor_B1)) {
							min = int.Parse(test_parameter._42Cursor_B1);
						}
						else if (min >= int.Parse(test_parameter._44Cursor_C1)) {
							min = int.Parse(test_parameter._44Cursor_C1);
						}
						double max = int.Parse(test_parameter._41Cursor_A2);
						if (max <= int.Parse(test_parameter._43Cursor_B2)) {
							max = int.Parse(test_parameter._43Cursor_B2);
						}
						else if (max <= int.Parse(test_parameter._45Cursor_C2)) {
							max = int.Parse(test_parameter._45Cursor_C2);
						}

						double offsetABC = max * rate - min * rate;
						Tchart1.Axes.Bottom.SetMinMax(min * rate - offsetABC / 4, max * rate + offsetABC / 4);
						break;
				}

			}
			else {
				switch (Phase_Name) {
					case "A":

						if (transformer_parameter._12Triangle_method) {
							double min = int.Parse(test_parameter._40Cursor_A1);
							double max = int.Parse(test_parameter._51Cursor_A2_DC);
							Tchart1.Axes.Bottom.SetMinMax(min * rate - 20, max * rate + 20);
						}
						else {
							double min = int.Parse(test_parameter._40Cursor_A1);
							double max = int.Parse(test_parameter._41Cursor_A2);
							Tchart1.Axes.Bottom.SetMinMax(min * rate - 20, max * rate + 20);
						}
						break;
					case "B":
						if (transformer_parameter._12Triangle_method) {
							double minb = int.Parse(test_parameter._42Cursor_B1);
							double maxb = int.Parse(test_parameter._53Cursor_B2_DC);
							Tchart1.Axes.Bottom.SetMinMax(minb * rate - 20, maxb * rate + 20);
						}
						else {
							double minb = int.Parse(test_parameter._42Cursor_B1);
							double maxb = int.Parse(test_parameter._43Cursor_B2);
							Tchart1.Axes.Bottom.SetMinMax(minb * rate - 20, maxb * rate + 20);
						}

						break;
					case "C":
						if (transformer_parameter._12Triangle_method) {
							double minc = int.Parse(test_parameter._44Cursor_C1);
							double maxc = int.Parse(test_parameter._55Cursor_C2_DC);
							Tchart1.Axes.Bottom.SetMinMax(minc * rate - 20, maxc * rate + 20);
						}
						else {
							double minc = int.Parse(test_parameter._44Cursor_C1);
							double maxc = int.Parse(test_parameter._45Cursor_C2);
							Tchart1.Axes.Bottom.SetMinMax(minc * rate - 20, maxc * rate + 20);
						}

						break;
					default:
						if (transformer_parameter._12Triangle_method) {
							double minabc = int.Parse(test_parameter._40Cursor_A1);
							if (minabc >= int.Parse(test_parameter._42Cursor_B1)) {
								minabc = int.Parse(test_parameter._42Cursor_B1);
							}
							else if (minabc >= int.Parse(test_parameter._44Cursor_C1)) {
								minabc = int.Parse(test_parameter._44Cursor_C1);
							}
							double maxabc = int.Parse(test_parameter._51Cursor_A2_DC);
							if (maxabc <= int.Parse(test_parameter._53Cursor_B2_DC)) {
								maxabc = int.Parse(test_parameter._53Cursor_B2_DC);
							}
							else if (maxabc <= int.Parse(test_parameter._55Cursor_C2_DC)) {
								maxabc = int.Parse(test_parameter._55Cursor_C2_DC);
							}
							Tchart1.Axes.Bottom.SetMinMax(minabc * rate - 20, maxabc * rate + 20);
						}
						else {
							double minabc = int.Parse(test_parameter._40Cursor_A1);
							if (minabc >= int.Parse(test_parameter._42Cursor_B1)) {
								minabc = int.Parse(test_parameter._42Cursor_B1);
							}
							else if (minabc >= int.Parse(test_parameter._44Cursor_C1)) {
								minabc = int.Parse(test_parameter._44Cursor_C1);
							}
							double maxabc = int.Parse(test_parameter._41Cursor_A2);
							if (maxabc <= int.Parse(test_parameter._43Cursor_B2)) {
								maxabc = int.Parse(test_parameter._43Cursor_B2);
							}
							else if (maxabc <= int.Parse(test_parameter._45Cursor_C2)) {
								maxabc = int.Parse(test_parameter._45Cursor_C2);
							}
							Tchart1.Axes.Bottom.SetMinMax(minabc * rate - 20, maxabc * rate + 20);
						}
						Tchart1.AutoRepaint = true;
						break;
				}

			}
		}
		void Tchart_ShowArea_Set(string Phase_Name, Steema.TeeChart.WPF.TChart chart) {
			double rate = 3.0 / Page_Max_count;
			if (test_parameter._25IsDCMeasurment) {
				switch (Phase_Name) {
					case "A":
						double offset = int.Parse(test_parameter._41Cursor_A2) * rate - int.Parse(test_parameter._40Cursor_A1) * rate;
						chart.Axes.Bottom.SetMinMax(int.Parse(test_parameter._40Cursor_A1) * rate - offset / 4, int.Parse(test_parameter._41Cursor_A2) * rate + offset / 4);
						break;
					case "B":
						double offsetb = int.Parse(test_parameter._43Cursor_B2) * rate - int.Parse(test_parameter._42Cursor_B1) * rate;
						chart.Axes.Bottom.SetMinMax(int.Parse(test_parameter._42Cursor_B1) * rate - offsetb / 4, int.Parse(test_parameter._43Cursor_B2) * rate + offsetb / 4);
						break;
					case "C":
						double offsetc = int.Parse(test_parameter._45Cursor_C2) * rate - int.Parse(test_parameter._44Cursor_C1) * rate;
						chart.Axes.Bottom.SetMinMax(int.Parse(test_parameter._44Cursor_C1) * rate - offsetc / 4, int.Parse(test_parameter._45Cursor_C2) * rate + offsetc / 4);
						break;
					default:
						double min = int.Parse(test_parameter._40Cursor_A1);
						if (min >= int.Parse(test_parameter._42Cursor_B1)) {
							min = int.Parse(test_parameter._42Cursor_B1);
						}
						if (min >= int.Parse(test_parameter._44Cursor_C1)) {
							min = int.Parse(test_parameter._44Cursor_C1);
						}
						double max = int.Parse(test_parameter._41Cursor_A2);
						if (max <= int.Parse(test_parameter._43Cursor_B2)) {
							max = int.Parse(test_parameter._43Cursor_B2);
						}
						if (max <= int.Parse(test_parameter._45Cursor_C2)) {
							max = int.Parse(test_parameter._45Cursor_C2);
						}

						double offsetABC = max * rate - min * rate;
						chart.Axes.Bottom.SetMinMax(min * rate - offsetABC / 4, max * rate + offsetABC / 4);
						break;
				}

			}
			else {
				switch (Phase_Name) {
					case "A":

						if (transformer_parameter._12Triangle_method) {
							double min = int.Parse(test_parameter._40Cursor_A1);
							double max = int.Parse(test_parameter._51Cursor_A2_DC);
							chart.Axes.Bottom.SetMinMax(min * rate - 20, max * rate + 20);
						}
						else {
							double min = int.Parse(test_parameter._40Cursor_A1);
							double max = int.Parse(test_parameter._41Cursor_A2);
							chart.Axes.Bottom.SetMinMax(min * rate - 20, max * rate + 20);
						}
						break;
					case "B":
						if (transformer_parameter._12Triangle_method) {
							double minb = int.Parse(test_parameter._42Cursor_B1);
							double maxb = int.Parse(test_parameter._53Cursor_B2_DC);
							chart.Axes.Bottom.SetMinMax(minb * rate - 20, maxb * rate + 20);
						}
						else {
							double minb = int.Parse(test_parameter._42Cursor_B1);
							double maxb = int.Parse(test_parameter._43Cursor_B2);
							chart.Axes.Bottom.SetMinMax(minb * rate - 20, maxb * rate + 20);
						}

						break;
					case "C":
						if (transformer_parameter._12Triangle_method) {
							double minc = int.Parse(test_parameter._44Cursor_C1);
							double maxc = int.Parse(test_parameter._55Cursor_C2_DC);
							chart.Axes.Bottom.SetMinMax(minc * rate - 20, maxc * rate + 20);
						}
						else {
							double minc = int.Parse(test_parameter._44Cursor_C1);
							double maxc = int.Parse(test_parameter._45Cursor_C2);
							chart.Axes.Bottom.SetMinMax(minc * rate - 20, maxc * rate + 20);
						}

						break;
					default:
						if (transformer_parameter._12Triangle_method) {
							double minabc = int.Parse(test_parameter._40Cursor_A1);
							if (minabc >= int.Parse(test_parameter._42Cursor_B1)) {
								minabc = int.Parse(test_parameter._42Cursor_B1);
							}
							if (minabc >= int.Parse(test_parameter._44Cursor_C1)) {
								minabc = int.Parse(test_parameter._44Cursor_C1);
							}
							double maxabc = int.Parse(test_parameter._51Cursor_A2_DC);
							if (maxabc <= int.Parse(test_parameter._53Cursor_B2_DC)) {
								maxabc = int.Parse(test_parameter._53Cursor_B2_DC);
							}
							if (maxabc <= int.Parse(test_parameter._55Cursor_C2_DC)) {
								maxabc = int.Parse(test_parameter._55Cursor_C2_DC);
							}
							chart.Axes.Bottom.SetMinMax(minabc * rate - 20, maxabc * rate + 20);
						}
						else {
							double minabc = int.Parse(test_parameter._40Cursor_A1);
							if (minabc >= int.Parse(test_parameter._42Cursor_B1)) {
								minabc = int.Parse(test_parameter._42Cursor_B1);
							}
							if (minabc >= int.Parse(test_parameter._44Cursor_C1)) {
								minabc = int.Parse(test_parameter._44Cursor_C1);
							}
							double maxabc = int.Parse(test_parameter._41Cursor_A2);
							if (maxabc <= int.Parse(test_parameter._43Cursor_B2)) {
								maxabc = int.Parse(test_parameter._43Cursor_B2);
							}
							if (maxabc <= int.Parse(test_parameter._45Cursor_C2)) {
								maxabc = int.Parse(test_parameter._45Cursor_C2);
							}
							chart.Axes.Bottom.SetMinMax(minabc * rate - 20, maxabc * rate + 20);
						}

						break;
				}

			}
		}
		#endregion

		#region 保存图片
		public void save_image(string path) {
			Steema.TeeChart.WPF.Export.ImageExport image_export = new Steema.TeeChart.WPF.Export.ImageExport(Tchart1.Chart);
			image_export.JPEG.Save(path);
		}
		#endregion

		#region 遍历窗体控件
		private void SetNotEditable(DependencyObject element) {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) {
				var child = VisualTreeHelper.GetChild(element, i);
				if (child is Button) {
					Button btn = child as Button;
					btn.MouseEnter += (MouseEventHandler)delegate {
						// btn.Background = new SolidColorBrush(Color.FromRgb(255, 255, 210));
						btn.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
						btn.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 0));
					};
					btn.MouseLeave += (MouseEventHandler)delegate {
						// btn.Background = new SolidColorBrush(Color.FromRgb(0xc4, 0xf2, 0xff));
						btn.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
						// btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0xc4, 0xf2, 0xff));
					};
				}

				if (child == null) {
					continue;
				}
				else if (child is Grid) {
					this.SetNotEditable(child);
				}
				else if (child is StackPanel) {
					this.SetNotEditable(child);
				}
				else if (child is GroupBox) {
					this.SetNotEditable(child);
				}
				else if (child is DockPanel) {
					this.SetNotEditable(child);
				}
				else if (child is ScrollViewer) {
					this.SetNotEditable(child);
					//ScrollViewer不具有Children属性，无法对其进行遍历，但是具有Content属性，作为容器型控件，一般都可以通过这样的方法来解决。  
				}
			}
		}
		#endregion

		#region 文件打开 数据解析
		void read(LoadingWindow load) {
			for (int i = 1; i <= 100; i++) {
				load.bgMeet.ReportProgress(100, "");
				Thread.Sleep(50);
			}
		}

		List<int> data_match_form_stringBulider(StringBuilder sb) {
			try {
				sb.Replace("Header:[TransFormerParameter]", "");
				sb.Replace("Header:[TestParameter]", "");
				sb.Replace("\r\n", "");
				string[] all = sb.ToString().Split(new string[] { "Data:" }, StringSplitOptions.RemoveEmptyEntries);
				string Header;
				string Data;
				Header = all[0];
				string[] parameters = Header.Split(';');
				transformer_parameter._1ItsUnitName = parameters[0];
				transformer_parameter._2TransformerName = parameters[1];
				transformer_parameter._3TransformerModel = parameters[2];
				transformer_parameter._4Single_phase = bool.Parse(parameters[3]);
				transformer_parameter._5Thrid_phase = bool.Parse(parameters[4]);
				transformer_parameter._6Transformerphase = parameters[5];
				transformer_parameter._7Double_Winding = bool.Parse(parameters[6]);
				transformer_parameter._8Three_Winding = bool.Parse(parameters[7]);
				transformer_parameter._9TransformerWinding = parameters[8];
				transformer_parameter._10Y_method = bool.Parse(parameters[9]);
				transformer_parameter._11YN_method = bool.Parse(parameters[10]);
				transformer_parameter._12Triangle_method = bool.Parse(parameters[11]);
				transformer_parameter._13TransformerWindingConnMethod = parameters[12];
				transformer_parameter._20SwitchManufactorName = parameters[13];
				transformer_parameter._21SwitchModel = parameters[14];
				transformer_parameter._22SwitchCode = parameters[15];
				transformer_parameter._23SwitchColumnCount = parameters[16];
				transformer_parameter._24SwitchStartWorkingPosition = parameters[17];
				transformer_parameter._25SwitchStopWorkingPosition = parameters[18];
				transformer_parameter._26SwitchMidPosition = parameters[19];
				transformer_parameter._27SwitchColumn_One_Count = bool.Parse(parameters[20]);
				transformer_parameter._28SwitchColumn_Two_Count = bool.Parse(parameters[21]);
				transformer_parameter._29SwitchColumn_Three_Count = bool.Parse(parameters[22]);

				test_parameter._0CompanyName = parameters[23];
				test_parameter._1curTransformerName = parameters[24];
				test_parameter._2OutputVolt = parameters[25];
				test_parameter._3OutputVoltFrequency = parameters[26];
				test_parameter._4SampleFrequency = parameters[27];
				test_parameter._5AutoContinuousMeasurementCurTap = parameters[28];
				test_parameter._6AutoContinuousMeasurementEndTap = parameters[29];
				test_parameter._7SinglePointMeasurementCurTap = parameters[30];
				test_parameter._8SinglePointMeasurementForwardSwitch = bool.Parse(parameters[31]);
				test_parameter._9SinglePointMeasurementBackSwitch = bool.Parse(parameters[32]);
				test_parameter._14isAutoContinuousMearsurment = bool.Parse(parameters[33]);
				test_parameter._15isHandleSingleMearsurment = bool.Parse(parameters[34]);
				test_parameter._16MeasureGear_DC = parameters[35];
				test_parameter._17SampleFrequency_DC = parameters[36];
				test_parameter._18isAutoContinuousMearsurment_DC = bool.Parse(parameters[37]);
				test_parameter._19isHandleSingleMearsurment_DC = bool.Parse(parameters[38]);
				test_parameter._20EnableDCfilter_DC = bool.Parse(parameters[39]);
				test_parameter._21DisableDCfilter_DC = bool.Parse(parameters[40]);
				test_parameter._22IsInnernalPower = bool.Parse(parameters[41]);
				test_parameter._23IsExternalPower = bool.Parse(parameters[42]);
				test_parameter._24IsACMeasurment = bool.Parse(parameters[43]);
				test_parameter._25IsDCMeasurment = bool.Parse(parameters[44]);
				test_parameter._26MutationRation_DC = parameters[45];
				test_parameter._27MutationRation_AC = parameters[46];
				test_parameter._28ErrorRation_DC = parameters[47];
				test_parameter._29ErrorRation_AC = parameters[48];
				test_parameter._30MinChangeTime_DC = parameters[49];
				test_parameter._31MinChangeTime_AC = parameters[50];
				test_parameter._32MaxConstantTime_DC = parameters[51];
				test_parameter._33MaxConstantTime_AC = parameters[52];
				test_parameter._34IgnoreTime_DC = parameters[53];
				test_parameter._35IgnoreTime_AC = parameters[54];
				test_parameter._36IsAutoAnalysisParameterSet_AC = bool.Parse(parameters[55]);
				test_parameter._37IsAutoAnalysisParameterSet_DC = bool.Parse(parameters[56]);
				test_parameter._38IsHandleAnalysisParameterSet_AC = bool.Parse(parameters[57]);
				test_parameter._39IsHandleAnalysisParameterSet_DC = bool.Parse(parameters[58]);
				test_parameter._40Cursor_A1 = parameters[58] == null ? "0" : parameters[59];
				test_parameter._41Cursor_A2 = parameters[59] == null ? "0" : parameters[60];
				test_parameter._42Cursor_B1 = parameters[60] == null ? "0" : parameters[61];
				test_parameter._43Cursor_B2 = parameters[61] == null ? "0" : parameters[62];
				test_parameter._44Cursor_C1 = parameters[62] == null ? "0" : parameters[63];
				test_parameter._45Cursor_C2 = parameters[63] == null ? "0" : parameters[64];
				test_parameter._46Peak_value = parameters[64] == null ? "0" : parameters[65];
				test_parameter._47Test_Date = parameters[65] == null ? "0" : parameters[66];
				test_parameter._48Access_position = parameters[66] == null ? "0" : parameters[67];
				test_parameter._49Mesurent_Counts = parameters[67] == null ? "0" : parameters[68];
				test_parameter._50Cursor_A1_DC = parameters[68] == null ? "0" : parameters[69];
				test_parameter._51Cursor_A2_DC = parameters[69] == null ? "0" : parameters[70];
				test_parameter._52Cursor_B1_DC = parameters[70] == null ? "0" : parameters[71];
				test_parameter._53Cursor_B2_DC = parameters[71] == null ? "0" : parameters[72];
				test_parameter._54Cursor_C1_DC = parameters[72] == null ? "0" : parameters[73];
				test_parameter._55Cursor_C2_DC = parameters[73] == null ? "0" : parameters[74];
				Data = all[1];
				string[] data = Data.Split(';');
				List<int> temp = new List<int>();

				for (int i = 0; i < data.Length - 1; i++) {
					temp.Add(Convert.ToInt32(data[i]));
				}
				int 采样率 = int.Parse(test_parameter._4SampleFrequency);
				if (采样率 == 100) {
					采样率 = 50;
				}
				if (采样率 == 500) {
					采样率 = 200;
				}
				Page_Max_count = (int)(采样率 * 3);
				if (test_parameter._25IsDCMeasurment) {
					Page_Max_count = 60;
				}
				quxian_Show_areaAphase = new Queue<List<int>>(Page_Max_count);
				if (transformer_parameter._6Transformerphase == "单相") {
					cbAp.IsEnabled = false;
					cbBp.IsEnabled = false;
					cbAp_Chart1.IsEnabled = false;
					cbBp_Chart1.IsEnabled = false;
					cbAp_Chart2.IsEnabled = false;
					cbBp_Chart2.IsEnabled = false;
				}
				else {
					cbAp.IsEnabled = true;
					cbBp.IsEnabled = true;
					cbAp_Chart1.IsEnabled = true;
					cbBp_Chart1.IsEnabled = true;
					cbAp_Chart2.IsEnabled = true;
					cbBp_Chart2.IsEnabled = true;
				}
				return temp;
			}
			catch {
				MessageBox.Show("数据匹配失败", "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}

		}
		List<int> data_match_form_stringBulider(StringBuilder sb, string s) {
			try {
				sb.Replace("\r\n", "");
				string[] data = sb.ToString().Split(';');
				List<int> temp = new List<int>();
				for (int i = 0; i < data.Length - 2; i++) {
					temp.Add(int.Parse(data[i + 1]));
				}
				return temp;
			}
			catch (Exception e) {
				MessageBox.Show(e.Message, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}

		}

		//打开已有文件时 更新UI显示
		void show_File_Data_onChart() {
			exhange();
			#region ListView 更新
			ListViewUpdate();
			#endregion
		}
		//打开文件时 更新 TreeView
		void openfile_TreeView_update() {
			#region TreeView 更新
			TreeViewUpdate_for_OpenFile();
			#endregion
		}
		//递归打开目录内所有数据
		void recursion_get_directory(string directory, int tag) {
			if (quxianA == null) {
				quxianA = new List<int>();
			}
			if (quxianB == null) {
				quxianB = new List<int>();
			}
			if (quxianC == null) {
				quxianC = new List<int>();
			}
			if (directory == "") {
				return;
			}
			try {
				DirectoryInfo di = new DirectoryInfo(directory);
				DirectoryInfo[] directory_list = di.GetDirectories();
				StringBuilder sb = new StringBuilder();
				if (directory_list.Length <= 0) {
					FileInfo[] file_name_list = di.GetFiles();

					if (file_name_list.Length <= 2) {
						//单相
						for (int i = 0; i < file_name_list.Length; i++) {
							if (file_name_list[i].ToString().Contains("原始")) {
								continue;
							}
							sb.Clear();
							sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(directory + "\\" + file_name_list[i].ToString())));
							quxianC.Clear();
							quxianC = data_match_form_stringBulider(sb);
							openfile_TreeView_update();
							show_File_Data_onChart();
						}

					}
					else if (file_name_list.Length >= 3) {
						//三相
						for (int i = 0; i < file_name_list.Length; i++) {
							if (file_name_list[i].ToString().Contains("原始")) {
								continue;
							}
							sb.Clear();
							sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(directory + "\\" + file_name_list[i].ToString())));
							if (i == 0) {
								quxianA.Clear();
								quxianA = data_match_form_stringBulider(sb);
							}
							else if (i == 1) {
								quxianB.Clear();
								quxianB = data_match_form_stringBulider(sb);
							}
							else if (i == 2) {
								quxianC.Clear();
								quxianC = data_match_form_stringBulider(sb);
							}
						}
						openfile_TreeView_update();
						show_File_Data_onChart();
					}
					return;
				}
				else {
					tag++;
					for (int i = 0; i < directory_list.Length; i++) {
						string path = directory_list[i].ToString();
						recursion_get_directory(directory + "\\" + path, tag);
					}
				}
			}
			catch (Exception e) {
				MessageBox.Show("分析错误:" + e.Message);
			}

		}
		//目录打开
		void open_phase_source_data(TreeViewIconsItem item) {
			test_parameter._49Mesurent_Counts = ((TreeViewIconsItem)item.Parent).HeaderText;
			test_parameter._48Access_position = ((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).HeaderText;
			test_parameter._47Test_Date = ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).Parent).HeaderText;
			transformer_parameter._2TransformerName = ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).Parent).Parent).HeaderText;
			transformer_parameter._1ItsUnitName = ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).Parent).Parent).Parent).HeaderText;
			StringBuilder sb = new StringBuilder();
			sb.Clear();
			sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile_Dialog(test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts)));
			string s = sb.ToString();
			string ss = Encoding.UTF8.GetString(BitConverter.GetBytes(0));
			if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
				return;
			}
			quxianA.Clear();
			quxianB.Clear();
			quxianC.Clear();
			if (item.TabIndex == 11) {
				quxianA = data_match_form_stringBulider(sb);

				if (DrawNewLine(quxianA, Color.FromRgb(0xFF, 0x83, 0x07), line_forTest[0], Tchart1)) {
					line_forTest[1].Active = false;
					line_forTest[2].Active = false;
					double rate = 3.0 / Page_Max_count;
					Cursor_Postion_Set("A", true);
					Tchart_ShowArea_Set("A");
				}
			}
			else if (item.TabIndex == 22) {
				quxianB = data_match_form_stringBulider(sb);
				if (DrawNewLine(quxianB, Color.FromRgb(0x00, 0x80, 0x00), line_forTest[1], Tchart1)) {
					line_forTest[0].Active = false;
					line_forTest[2].Active = false;
					double rate = 3.0 / Page_Max_count;
					Cursor_Postion_Set("B", true);
					Tchart_ShowArea_Set("B");
				}
			}
			else if (item.TabIndex == 33) {
				quxianC = data_match_form_stringBulider(sb);
				if (DrawNewLine(quxianC, Color.FromRgb(0xFF, 0x00, 0x00), line_forTest[2], Tchart1)) {
					line_forTest[0].Active = false;
					line_forTest[1].Active = false;
					double rate = 3.0 / Page_Max_count;
					Cursor_Postion_Set("C", true);
					Tchart_ShowArea_Set("C");
				}
			}

			show_File_Data_onChart();
			openfile_TreeView_update();
		}
		//单击打开 for  Test
		void open_phase_source_data(string Phase, string root_path) {
			Max = 0;
			if (quxianA == null) {
				quxianA = new List<int>();
			}
			if (quxianB == null) {
				quxianB = new List<int>();
			}
			if (quxianC == null) {
				quxianC = new List<int>();
			}
			set_cursor_position(cursor_collection[Tchart1.Name + "A1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "A2"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "B1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "B2"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "C1"], 0, false);
			set_cursor_position(cursor_collection[Tchart1.Name + "C2"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "A1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "A2_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "B1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "B2_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "C1_DC"], 0, false);
			set_cursor_position(cursor_collection_DC[Tchart1.Name + "C2_DC"], 0, false);
			for (int i = 0; i < 3; i++) {
				try {
					line_forTest[i].Active = true;
					line_forTest[i].XValues.Clear();
					line_forTest[i].YValues.Clear();
					Tchart1.AutoRepaint = true;
				}
				catch {

				}
			}
			StringBuilder sb = new StringBuilder();
			switch (Phase) {
				case "A":
					sb.Clear();
					sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\A相数据 ")));
					if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
						return;
					}
					quxianA.Clear();
					quxianA = data_match_form_stringBulider(sb);
					if (DrawNewLine(quxianA, Color.FromRgb(0xFF, 0x83, 0x07), line_forTest[0], Tchart1)) {
						double rate = 3.0 / Page_Max_count;
						Cursor_Postion_Set("A", true);
						Tchart_ShowArea_Set("A");
					}
					break;
				case "B":
					sb.Clear();
					sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\B相数据 ")));
					if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
						return;
					}
					quxianB.Clear();
					quxianB = data_match_form_stringBulider(sb);
					if (DrawNewLine(quxianB, Color.FromRgb(0x00, 0x80, 0x00), line_forTest[1], Tchart1)) {
						double rate = 3.0 / Page_Max_count;
						Cursor_Postion_Set("B", true);
						Tchart_ShowArea_Set("B");
					}
					break;
				case "C":
					sb.Clear();
					sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\C相数据")));
					if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
						return;
					}
					quxianC.Clear();
					quxianC = data_match_form_stringBulider(sb);
					Fun_绘制触发曲线_Trig_Line(quxianC, Colors.Red, line_forTest[2]);
					cbCp.IsChecked = true;
					cbAp.IsEnabled = false;
					cbBp.IsEnabled = false;
					break;
				default:
					sb.Clear();
					sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\C相数据 ")));
					if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
						return;
					}
					quxianC.Clear();
					quxianC = data_match_form_stringBulider(sb);
					if (transformer_parameter._6Transformerphase == "三相") {
						sb.Clear();
						sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\A相数据 ")));
						if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
							return;
						}
						quxianA.Clear();
						quxianA = data_match_form_stringBulider(sb);
						sb.Clear();
						sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\B相数据 ")));
						if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
							return;
						}
						quxianB.Clear();
						quxianB = data_match_form_stringBulider(sb);
						Fun_绘制触发曲线_Trig_Line(quxianA, Colors.Gold, line_forTest[0]);
						Fun_绘制触发曲线_Trig_Line(quxianB, Colors.Green, line_forTest[1]);
						Fun_绘制触发曲线_Trig_Line(quxianC, Colors.Red, line_forTest[2]);
						Cursor_Postion_Set("ABC", true);
						Tchart_ShowArea_Set("ABC", Tchart1);
						cbAp.IsChecked = true;
						cbBp.IsChecked = true;
						cbCp.IsChecked = true;
						cbAp.IsEnabled = true;
						cbBp.IsEnabled = true;
					}
					else {
						Fun_绘制触发曲线_Trig_Line(quxianC, Colors.Red, line_forTest[2]);
						Cursor_Postion_Set("C", true);
						Tchart_ShowArea_Set("C", Tchart1);
						cbCp.IsChecked = true;
						cbAp.IsEnabled = false;
						cbBp.IsEnabled = false;
					}
					break;
			}
			show_File_Data_onChart();
			openfile_TreeView_update();
		}
		//单击打开  For Analysis
		void open_phase_source_data_for_Analysis(string Phase, string root_path, List<FastLine> lines, string ChartName) {
			Steema.TeeChart.WPF.TChart Chart = new Steema.TeeChart.WPF.TChart();
			if (ChartName == Tchart_1.Name) {
				Chart = Tchart_1;
			}
			if (ChartName == Tchart_2.Name) {
				Chart = Tchart_2;
			}
			for (int i = 0; i < 3; i++) {
				try {
					lines[i].Active = true;
					lines[i].XValues.Clear();
					lines[i].YValues.Clear();
				}
				catch {

				}
			}
			StringBuilder sb = new StringBuilder();
			switch (Phase) {
				default:
					sb.Clear();
					sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\C相数据 ")));
					if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
						return;
					}
					List<int> quxianc = new List<int>();
					quxianc = data_match_form_stringBulider(sb);
					DrawNewLine(quxianc, Color.FromRgb(0xFF, 0x00, 0x00), lines[2], Chart);
					if (transformer_parameter._6Transformerphase == "三相") {
						sb.Clear();
						sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\A相数据 ")));
						if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
							return;
						}
						List<int> quxiana = new List<int>();
						quxiana = data_match_form_stringBulider(sb);
						if (DrawNewLine(quxiana, Color.FromRgb(0xFF, 0x83, 0x07), lines[0], Chart)) {
						}
						sb.Clear();
						sb.Append(Encoding.UTF8.GetString(FileHelper.OpenFile(root_path + "\\B相数据 ")));
						if (sb.ToString() == Encoding.UTF8.GetString(BitConverter.GetBytes(0))) {
							return;
						}
						List<int> quxianb = new List<int>();
						quxianb = data_match_form_stringBulider(sb);
						DrawNewLine(quxianb, Color.FromRgb(0x00, 0x80, 0x00), lines[1], Chart);
					}
					break;
			}
			if (ChartName == Tchart_1.Name) {
				double rate1 = 3.0 / Page_Max_count;
				if (test_parameter._24IsACMeasurment) {
					tabChart1AC.IsSelected = true;
				}
				if (test_parameter._25IsDCMeasurment) {
					tabChart1DC.IsSelected = true;
				}
				Chart.Header.Text = "[" + test_parameter._47Test_Date + "]" + test_parameter._49Mesurent_Counts + "波形:" + test_parameter._48Access_position;
				Chart.Header.Color = Colors.Blue;
				Chart.Header.Font.Bold = true;
				Chart.Header.Font.Size = 13;
				if (transformer_parameter._6Transformerphase == "三相") {
					Cursor_Postion_Set("ABC", ChartName, true);
					Tchart_ShowArea_Set("ABC", Chart);
				}
				else {
					Cursor_Postion_Set("C", ChartName, true);
					Tchart_ShowArea_Set("C", Chart);
				}
			}
			if (ChartName == Tchart_2.Name) {
				double rate1 = 3.0 / Page_Max_count;
				if (test_parameter._24IsACMeasurment) {
					tabChart2AC.IsSelected = true;
				}
				if (test_parameter._25IsDCMeasurment) {
					tabChart2DC.IsSelected = true;
				}
				Chart.Header.Text = "[" + test_parameter._47Test_Date + "]" + test_parameter._49Mesurent_Counts + "波形:" + test_parameter._48Access_position;
				Chart.Header.Color = Colors.Blue;
				Chart.Header.Font.Bold = true;
				Chart.Header.Font.Size = 13;
				if (transformer_parameter._6Transformerphase == "三相") {
					Cursor_Postion_Set("ABC", ChartName, true);
					Tchart_ShowArea_Set("ABC", Chart);
				}
				else {
					Cursor_Postion_Set("C", ChartName, true);
					Tchart_ShowArea_Set("C", Chart);
				}
			}
		}

		#endregion

		#region 测试列表展开至ListBox

		TreeViewIconsItem need_select_item;
		ContextMenu phase_list = new ContextMenu();
		private void lb_switch_list_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (transformer_parameter._6Transformerphase == "三相") {
				cbAp.IsChecked = true;
				cbBp.IsChecked = true;
				cbCp.IsChecked = true;
				cbAp.IsEnabled = true;
				cbBp.IsEnabled = true;
				cbCp.IsEnabled = true;
			}
			else {
				cbAp.IsChecked = true;
				cbBp.IsChecked = true;
				cbBp.IsEnabled = false;
				cbCp.IsEnabled = false;
				cbCp.IsChecked = true;
			}
			fun_读取文件并分析();
		}
		void fun_读取文件并分析() {
			if (lb_switch_list.SelectedItem == null) {
				return;
			}
			else {
				 Max = 0;
				 btnSave.IsEnabled = true;
				 if (string.IsNullOrEmpty(上一次测试分接位)) {
					 上一次测试分接位 = test_parameter._48Access_position;
				 }
			     string path = test_data_path  + transformer_parameter._1ItsUnitName + "\\" + test_parameter._1curTransformerName + "\\" + test_parameter._47Test_Date + "\\" + 上一次测试分接位 + "\\" + test_parameter._49Mesurent_Counts + "\\";
				
				 int 变化点位置 = C相变化点集合_字典[lb_switch_list.SelectedItem.ToString()];
			    int 分析的个数 =  Page_Max_count*500; 
				quxianC.Clear();
				if (Math.Abs(变化点位置 - filelength) <= 分析的个数) {
					分析的个数 =(int)Math.Abs(变化点位置 - filelength);
				}
				int aimPosition =变化点位置 - Page_Max_count * 150;
				if (aimPosition <= 0) {
					aimPosition = 0;
				}
				if (transformer_parameter._6Transformerphase == "三相") {
					quxianA = Fun_从文件变化点获取触发段曲线(path + "A相原始数据", aimPosition, 分析的个数);
					quxianB = Fun_从文件变化点获取触发段曲线(path + "B相原始数据", aimPosition, 分析的个数);
				}
				quxianC = Fun_从文件变化点获取触发段曲线(path + "C相原始数据", aimPosition, 分析的个数);

				is_无效变化点 = false;
				thread_Fun_List出队数据分析(quxianA, quxianB, quxianC);
				if (is_无效变化点) {
					lb_switch_list.Items.Remove(lb_switch_list.SelectedItem);
				}
			}
		}
		private void lb_switch_list_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			fun_读取文件并分析();
		}
		void mi_click(object sender, RoutedEventArgs e) {
			MenuItem mi = sender as MenuItem;
			foreach (TreeViewIconsItem item in need_select_item.Items) {
				if (item.HeaderText == mi.Header.ToString()) {
					TreeViewIconsItem select_item = item;
					test_parameter._49Mesurent_Counts = item.HeaderText;
					test_parameter._48Access_position = need_select_item.HeaderText;
					test_parameter._47Test_Date = ((TreeViewIconsItem)need_select_item.Parent).HeaderText;
					transformer_parameter._2TransformerName = ((TreeViewIconsItem)((TreeViewIconsItem)need_select_item.Parent).Parent).HeaderText;
					transformer_parameter._1ItsUnitName = ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)need_select_item.Parent).Parent).Parent).HeaderText;
					if (select_item.TabIndex == 5) {
						if (tabAnalysis.IsSelected != true) {
							try {
								open_phase_source_data("ABC", test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts);
							}
							catch {
								MessageBox.Show("没有找到可加载的数据!", "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
							}
						}
						else if (isTchart_1Draw) {
							open_phase_source_data_for_Analysis("ABC", test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts, line_forAnalysis_chart1, Tchart_1.Name);
							isTchart_1Draw = false;
							isTchart_2Draw = true;
						}
						else if (isTchart_2Draw) {
							open_phase_source_data_for_Analysis("ABC", test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts, line_forAnalysis_chart2, Tchart_2.Name);
							isTchart_1Draw = true;
							isTchart_2Draw = false;
						}
					}
				}
			}
			get_access_info(OleDbHelper.File_Name_e._1ItsUnitName, transformer_parameter._1ItsUnitName, OleDbHelper.Test_File_Name_e._0curTransformerName, test_parameter._1curTransformerName);
		}
		private void lb_switch_list_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			//if (lb_switch_list.SelectedItem != null)
			//{
			//    lb_switch_list.ContextMenu.IsOpen = true;
			//}
		}

		#endregion

		#region DataGrid绑定与更新
		string Aphase_offset = "0";
		string Bphase_offset = "0";
		string Cphase_offset = "0";
		//获取控件中虚拟子控件
		public T FindFirstVisualChild<T>(DependencyObject obj, string childName) where T : DependencyObject {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
				DependencyObject child = VisualTreeHelper.GetChild(obj, i);
				if (child != null && child is T && child.GetValue(NameProperty).ToString() == childName) {
					return (T)child;
				}
				else {
					T childOfChild = FindFirstVisualChild<T>(child, childName);
					if (childOfChild != null) {
						return childOfChild;
					}
				}
			}
			return null;
		}
		Dictionary<string, double> DictorySort(Dictionary<string, double> source) {
			Dictionary<string, double> temp = new Dictionary<string, double>();
			var dicSort = from objDic in source orderby objDic.Value select objDic;
			foreach (KeyValuePair<string, double> kvp in dicSort)
				temp.Add(kvp.Key, kvp.Value);
			return temp;
		}

		void DataGridUpdate(string cursor_Name) {

			try {
				if (cursor_Name.Contains("A")) {
					Aphase_offset = cursor_collection[cursor_Name.Replace("A2", "A1")].XValue.ToString("0.#");
				}
				if (cursor_Name.Contains("B")) {
					Bphase_offset = cursor_collection[cursor_Name.Replace("B2", "B1")].XValue.ToString("0.#");
				}
				if (cursor_Name.Contains("C")) {
					Cphase_offset = cursor_collection[cursor_Name.Replace("C2", "C1")].XValue.ToString("0.#");
				}
			}
			catch {

			}
			if (cursor_Name.Contains(Tchart1.Name)) {
				#region 直流
				if (test_parameter._25IsDCMeasurment) {
					DC_display_list.Clear();
					try {
						double current = 1;
						double rate = 0.05;
						if (test_parameter._16MeasureGear_DC == "0--20Ω") {
							current = 1;
						}
						if (test_parameter._16MeasureGear_DC == "20--100Ω") {
							current = 0.2;
						}
						//过渡电压
						if (transformer_parameter._6Transformerphase == "三相") {
							int indexguoduA1 = (int)(cursor_collection[Tchart1.Name + "A1"].XValue / rate + 3000);
							double VolateguoduA1 = line_forTest[0].YValues.Value[indexguoduA1];
							int indexguoduA2 = (int)(cursor_collection[Tchart1.Name + "A2"].XValue / rate + 3000);
							double VolateguoduA2 = line_forTest[0].YValues.Value[indexguoduA2];

							int indexguoduB1 = (int)(cursor_collection[Tchart1.Name + "B1"].XValue / rate + 3000);
							double VolateguoduB1 = line_forTest[1].YValues.Value[indexguoduB1];
							int indexguoduB2 = (int)(cursor_collection[Tchart1.Name + "B2"].XValue / rate + 3000);
							double VolateguoduB2 = line_forTest[1].YValues.Value[indexguoduB2];

							int indexguoduC1 = (int)(cursor_collection[Tchart1.Name + "C1"].XValue / rate + 3000);
							double VolateguoduC1 = line_forTest[2].YValues.Value[indexguoduC1];
							int indexguoduC2 = (int)(cursor_collection[Tchart1.Name + "C2"].XValue / rate + 3000);
							double VolateguoduC2 = line_forTest[2].YValues.Value[indexguoduC2];
							string guodudianzuA1 = (VolateguoduA1 / current).ToString("0.##");
							string guodudianzuA2 = (VolateguoduA2 / current).ToString("0.##");
							string guodudianzuB1 = (VolateguoduB1 / current).ToString("0.##");
							string guodudianzuB2 = (VolateguoduB2 / current).ToString("0.##");
							string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
							string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");

							//桥接电压
							int indexA1 = (int)(cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue / rate + 3000);
							double VolateA1 = line_forTest[0].YValues.Value[indexA1];
							int indexA2 = (int)(cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue / rate + 3000);
							double VolateA2 = line_forTest[0].YValues.Value[indexA2];

							int indexB1 = (int)(cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue / rate + 3000);
							double VolateB1 = line_forTest[1].YValues.Value[indexB1];
							int indexB2 = (int)(cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue / rate + 3000);
							double VolateB2 = line_forTest[1].YValues.Value[indexB2];

							int indexC1 = (int)(cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue / rate + 3000);
							double VolateC1 = line_forTest[2].YValues.Value[indexC1];
							int indexC2 = (int)(cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue / rate + 3000);
							double VolateC2 = line_forTest[2].YValues.Value[indexC2];
							string qiaojiedianzuA1 = (VolateA1 / current).ToString("0.##");
							string qiaojiedianzuA2 = (VolateA2 / current).ToString("0.##");
							string qiaojiedianzuB1 = (VolateB1 / current).ToString("0.##");
							string qiaojiedianzuB2 = (VolateB2 / current).ToString("0.##");
							string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
							string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
							DC_display_list.Add(new class_AnalysisShow("过渡电阻1(Ω)", guodudianzuA1, guodudianzuB1, guodudianzuC1));
							DC_display_list.Add(new class_AnalysisShow("过渡电阻2(Ω)", guodudianzuA2, guodudianzuB2, guodudianzuC2));

							DC_display_list.Add(new class_AnalysisShow("桥接电阻1(Ω)", qiaojiedianzuA1, qiaojiedianzuB1, qiaojiedianzuC1));
							DC_display_list.Add(new class_AnalysisShow("桥接电阻2(Ω)", qiaojiedianzuA2, qiaojiedianzuB2, qiaojiedianzuC2));
							//过渡时间1
							string guodushijianA1 = (cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue - cursor_collection[Tchart1.Name + "A1"].XValue).ToString("0.##");
							string guodushijianB1 = (cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue - cursor_collection[Tchart1.Name + "B1"].XValue).ToString("0.##");
							string guodushijianC1 = (cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue - cursor_collection[Tchart1.Name + "C1"].XValue).ToString("0.##");
							//过渡时间2
							string guodushijianA2 = (cursor_collection[Tchart1.Name + "A2"].XValue - cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue).ToString("0.##");
							string guodushijianB2 = (cursor_collection[Tchart1.Name + "B2"].XValue - cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue).ToString("0.##");
							string guodushijianC2 = (cursor_collection[Tchart1.Name + "C2"].XValue - cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue).ToString("0.##");
							DC_display_list.Add(new class_AnalysisShow("过渡时间1(ms)", guodushijianA1, guodushijianB1, guodushijianC1));
							DC_display_list.Add(new class_AnalysisShow("过渡时间2(ms)", guodushijianA2, guodushijianB2, guodushijianC2));
							//桥接时间
							string qiaojieshijianA = (cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue).ToString("0.##");
							string qiaojieshijianB = (cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue).ToString("0.##");
							string qiaojieshijianC = (cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue).ToString("0.##");
							DC_display_list.Add(new class_AnalysisShow("桥接时间(ms)", qiaojieshijianA, qiaojieshijianB, qiaojieshijianC));
							DC_display_list.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							DC_display_list.Add(new class_AnalysisShow("不同步时间(ms)", "0", (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"), (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#")));
						}
						else {
							int indexguoduC1 = (int)(cursor_collection[Tchart1.Name + "C1"].XValue / rate + 3000);
							double VolateguoduC1 = line_forTest[2].YValues.Value[indexguoduC1];
							int indexguoduC2 = (int)(cursor_collection[Tchart1.Name + "C2"].XValue / rate + 3000);
							double VolateguoduC2 = line_forTest[2].YValues.Value[indexguoduC2];
							string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
							string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");

							//桥接电压
							int indexC1 = (int)(cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue / rate + 3000);
							double VolateC1 = line_forTest[2].YValues.Value[indexC1];
							int indexC2 = (int)(cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue / rate + 3000);
							double VolateC2 = line_forTest[2].YValues.Value[indexC2];
							string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
							string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
							DC_display_list.Add(new class_AnalysisShow("过渡电阻1(Ω)", "0", "0", guodudianzuC1));
							DC_display_list.Add(new class_AnalysisShow("过渡电阻2(Ω)", "0", "0", guodudianzuC2));

							DC_display_list.Add(new class_AnalysisShow("桥接电阻1(Ω)", "0", "0", qiaojiedianzuC1));
							DC_display_list.Add(new class_AnalysisShow("桥接电阻2(Ω)", "0", "0", qiaojiedianzuC2));
							//过渡时间1
							string guodushijianC1 = (cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue - cursor_collection[Tchart1.Name + "C1"].XValue).ToString("0.##");
							//过渡时间2
							string guodushijianC2 = (cursor_collection[Tchart1.Name + "C2"].XValue - cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue).ToString("0.##");
							DC_display_list.Add(new class_AnalysisShow("过渡时间1(ms)", "0", "0", guodushijianC1));
							DC_display_list.Add(new class_AnalysisShow("过渡时间2(ms)", "0", "0", guodushijianC2));
							//桥接时间
							string qiaojieshijianC = (cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue).ToString("0.##");
							DC_display_list.Add(new class_AnalysisShow("桥接时间(ms)", "0", "0", qiaojieshijianC));
							DC_display_list.Add(new class_AnalysisShow("切换时间(ms)", "0", "0", lbGapC.Content.ToString()));
							DC_display_list.Add(new class_AnalysisShow("不同步时间(ms)", "0", "0", "0"));
						}
					}
					catch {
					}
				}
				#endregion
				#region 交流
				if (test_parameter._24IsACMeasurment) {
					AC_ThreePhase_display_list.Clear();
					if (transformer_parameter._6Transformerphase == "三相") {
						if (transformer_parameter._13TransformerWindingConnMethod == "三角形接法") {
							TextBlock tbAphase = FindFirstVisualChild<TextBlock>(datagridAC, "tbAphaseVale");
							TextBlock tbBphase = FindFirstVisualChild<TextBlock>(datagridAC, "tbBphaseVale");
							TextBlock tbCphase = FindFirstVisualChild<TextBlock>(datagridAC, "tbCphaseVale");
							double a11 = cursor_collection[Tchart1.Name + "A1"].XValue;
							double a12 = cursor_collection[Tchart1.Name + "A2"].XValue;
							double b11 = cursor_collection[Tchart1.Name + "B1"].XValue;
							double b12 = cursor_collection[Tchart1.Name + "B2"].XValue;
							double c11 = cursor_collection[Tchart1.Name + "C1"].XValue;
							double c12 = cursor_collection[Tchart1.Name + "C2"].XValue;

							double a21 = cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue;
							double a22 = cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue;
							double b21 = cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue;
							double b22 = cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue;
							double c21 = cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue;
							double c22 = cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue;
							TrangleDataGridSort.Clear();
							TrangleDataGridSort.Add("A11", a11);
							TrangleDataGridSort.Add("A12", a12);
							TrangleDataGridSort.Add("B11", b11);
							TrangleDataGridSort.Add("B12", b12);
							TrangleDataGridSort.Add("C11", c11);
							TrangleDataGridSort.Add("C12", c12);
							TrangleDataGridSort.Add("A21", a21);
							TrangleDataGridSort.Add("A22", a22);
							TrangleDataGridSort.Add("B21", b21);
							TrangleDataGridSort.Add("B22", b22);
							TrangleDataGridSort.Add("C21", c21);
							TrangleDataGridSort.Add("C22", c22);
							TrangleDataGridSort = DictorySort(TrangleDataGridSort);


							if (TrangleDataGridSort.ElementAt(0).Key.Contains("A")) {
								//AB
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("B")) {
									HeaderA = "AB相值";
									tbAphase.Text = "AB相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";

										}
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
								}
								//AC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("C")) {
									HeaderA = "AC相";
									tbAphase.Text = "AC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("B")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";

										}
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										HeaderB = "BC相值";
										tbBphase.Text = "BC相值";
										HeaderC = "AB相值";
										tbCphase.Text = "AB相值";
									}
								}
							}
							else if (TrangleDataGridSort.ElementAt(0).Key.Contains("B")) {
								//BC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("C")) {
									HeaderA = "BC相值";
									tbAphase.Text = "BC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
								//AB
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("A")) {
									HeaderA = "AB相";
									tbAphase.Text = "AB相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}

								}
							}
							else {
								//BC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("B")) {
									HeaderA = "BC相值";
									tbAphase.Text = "BC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
								//AC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("A")) {
									HeaderA = "AC相";
									tbAphase.Text = "AC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
							}
							AC_ThreePhase_display_list.Add(new class_AnalysisShow("切换时间(ms)", (TrangleDataGridSort.ElementAt(3).Value - TrangleDataGridSort.ElementAt(1).Value).ToString("0.#"), (TrangleDataGridSort.ElementAt(6).Value - TrangleDataGridSort.ElementAt(4).Value).ToString("0.#"), (TrangleDataGridSort.ElementAt(10).Value - TrangleDataGridSort.ElementAt(8).Value).ToString("0.#")));
						}
						else {
							TextBlock tbAphase = FindFirstVisualChild<TextBlock>(datagridAC, "tbAphaseVale");
							TextBlock tbBphase = FindFirstVisualChild<TextBlock>(datagridAC, "tbBphaseVale");
							TextBlock tbCphase = FindFirstVisualChild<TextBlock>(datagridAC, "tbCphaseVale");
							HeaderA = "A相值";
							HeaderB = "B相值";
							HeaderC = "C相值";
							tbAphase.Text = "A相值";
							tbBphase.Text = "B相值";
							tbCphase.Text = "C相值";
							AC_ThreePhase_display_list.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							AC_ThreePhase_display_list.Add(new class_AnalysisShow("不同步时间(ms)", "0", (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"), (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#")));
						}

					}
					else {
						AC_ThreePhase_display_list.Add(new class_AnalysisShow("切换时间(ms)", "0", "0", lbGapC.Content.ToString()));
						if (transformer_parameter._13TransformerWindingConnMethod == "YN型接法") {
							AC_ThreePhase_display_list.Add(new class_AnalysisShow("不同步时间(ms)", "0", "0", "0"));
						}
					}
				}
				#endregion
			}
			if (cursor_Name.Contains(Tchart_1.Name)) {
				#region 直流
				if (test_parameter._25IsDCMeasurment) {
					DC_display_list1.Clear();
					try {
						double current = 1;
						double rate = 0.05;
						if (test_parameter._16MeasureGear_DC == "0--20Ω") {
							current = 1;
						}
						if (test_parameter._16MeasureGear_DC == "20--100Ω") {
							current = 0.2;
						}
						if (transformer_parameter._6Transformerphase == "三相") {
							//过渡电压
							int indexguoduA1 = (int)(cursor_collection[Tchart_1.Name + "A1"].XValue / rate + 3000);
							double VolateguoduA1 = line_forAnalysis_chart1[0].YValues.Value[indexguoduA1];
							int indexguoduA2 = (int)(cursor_collection[Tchart_1.Name + "A2"].XValue / rate + 3000);
							double VolateguoduA2 = line_forAnalysis_chart1[0].YValues.Value[indexguoduA2];

							int indexguoduB1 = (int)(cursor_collection[Tchart_1.Name + "B1"].XValue / rate + 3000);
							double VolateguoduB1 = line_forAnalysis_chart1[1].YValues.Value[indexguoduB1];
							int indexguoduB2 = (int)(cursor_collection[Tchart_1.Name + "B2"].XValue / rate + 3000);
							double VolateguoduB2 = line_forAnalysis_chart1[1].YValues.Value[indexguoduB2];

							int indexguoduC1 = (int)(cursor_collection[Tchart_1.Name + "C1"].XValue / rate + 3000);
							double VolateguoduC1 = line_forAnalysis_chart1[2].YValues.Value[indexguoduC1];
							int indexguoduC2 = (int)(cursor_collection[Tchart_1.Name + "C2"].XValue / rate + 3000);
							double VolateguoduC2 = line_forAnalysis_chart1[2].YValues.Value[indexguoduC2];
							string guodudianzuA1 = (VolateguoduA1 / current).ToString("0.##");
							string guodudianzuA2 = (VolateguoduA2 / current).ToString("0.##");
							string guodudianzuB1 = (VolateguoduB1 / current).ToString("0.##");
							string guodudianzuB2 = (VolateguoduB2 / current).ToString("0.##");
							string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
							string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");

							//桥接电压
							int indexA1 = (int)(cursor_collection_DC[Tchart_1.Name + "A1_DC"].XValue / rate + 3000);
							double VolateA1 = line_forAnalysis_chart1[0].YValues.Value[indexA1];
							int indexA2 = (int)(cursor_collection_DC[Tchart_1.Name + "A2_DC"].XValue / rate + 3000);
							double VolateA2 = line_forAnalysis_chart1[0].YValues.Value[indexA2];

							int indexB1 = (int)(cursor_collection_DC[Tchart_1.Name + "B1_DC"].XValue / rate + 3000);
							double VolateB1 = line_forAnalysis_chart1[1].YValues.Value[indexB1];
							int indexB2 = (int)(cursor_collection_DC[Tchart_1.Name + "B2_DC"].XValue / rate + 3000);
							double VolateB2 = line_forAnalysis_chart1[1].YValues.Value[indexB2];

							int indexC1 = (int)(cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue / rate + 3000);
							double VolateC1 = line_forAnalysis_chart1[2].YValues.Value[indexC1];
							int indexC2 = (int)(cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue / rate + 3000);
							double VolateC2 = line_forAnalysis_chart1[2].YValues.Value[indexC2];
							string qiaojiedianzuA1 = (VolateA1 / current).ToString("0.##");
							string qiaojiedianzuA2 = (VolateA2 / current).ToString("0.##");
							string qiaojiedianzuB1 = (VolateB1 / current).ToString("0.##");
							string qiaojiedianzuB2 = (VolateB2 / current).ToString("0.##");
							string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
							string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
							DC_display_list1.Add(new class_AnalysisShow("过渡电阻1(Ω)", guodudianzuA1, guodudianzuB1, guodudianzuC1));
							DC_display_list1.Add(new class_AnalysisShow("过渡电阻2(Ω)", guodudianzuA2, guodudianzuB2, guodudianzuC2));

							DC_display_list1.Add(new class_AnalysisShow("桥接电阻1(Ω)", qiaojiedianzuA1, qiaojiedianzuB1, qiaojiedianzuC1));
							DC_display_list1.Add(new class_AnalysisShow("桥接电阻2(Ω)", qiaojiedianzuA2, qiaojiedianzuB2, qiaojiedianzuC2));
							//过渡时间1
							string guodushijianA1 = (cursor_collection_DC[Tchart_1.Name + "A1_DC"].XValue - cursor_collection[Tchart_1.Name + "A1"].XValue).ToString("0.##");
							string guodushijianB1 = (cursor_collection_DC[Tchart_1.Name + "B1_DC"].XValue - cursor_collection[Tchart_1.Name + "B1"].XValue).ToString("0.##");
							string guodushijianC1 = (cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue - cursor_collection[Tchart_1.Name + "C1"].XValue).ToString("0.##");
							//过渡时间2
							string guodushijianA2 = (cursor_collection[Tchart_1.Name + "A2"].XValue - cursor_collection_DC[Tchart_1.Name + "A2_DC"].XValue).ToString("0.##");
							string guodushijianB2 = (cursor_collection[Tchart_1.Name + "B2"].XValue - cursor_collection_DC[Tchart_1.Name + "B2_DC"].XValue).ToString("0.##");
							string guodushijianC2 = (cursor_collection[Tchart_1.Name + "C2"].XValue - cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue).ToString("0.##");
							DC_display_list1.Add(new class_AnalysisShow("过渡时间1(ms)", guodushijianA1, guodushijianB1, guodushijianC1));
							DC_display_list1.Add(new class_AnalysisShow("过渡时间2(ms)", guodushijianA2, guodushijianB2, guodushijianC2));
							//桥接时间
							string qiaojieshijianA = (cursor_collection_DC[Tchart_1.Name + "A2_DC"].XValue - cursor_collection_DC[Tchart_1.Name + "A1_DC"].XValue).ToString("0.##");
							string qiaojieshijianB = (cursor_collection_DC[Tchart_1.Name + "B2_DC"].XValue - cursor_collection_DC[Tchart_1.Name + "B1_DC"].XValue).ToString("0.##");
							string qiaojieshijianC = (cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue).ToString("0.##");
							DC_display_list1.Add(new class_AnalysisShow("桥接时间(ms)", qiaojieshijianA, qiaojieshijianB, qiaojieshijianC));
							DC_display_list1.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							DC_display_list1.Add(new class_AnalysisShow("不同步时间(ms)", "0", (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"), (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#")));
						}
						else {
							//过渡电压
							int indexguoduC1 = (int)(cursor_collection[Tchart_1.Name + "C1"].XValue / rate + 3000);
							double VolateguoduC1 = line_forAnalysis_chart1[2].YValues.Value[indexguoduC1];
							int indexguoduC2 = (int)(cursor_collection[Tchart_1.Name + "C2"].XValue / rate + 3000);
							double VolateguoduC2 = line_forAnalysis_chart1[2].YValues.Value[indexguoduC2];
							string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
							string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");

							//桥接电压
							int indexC1 = (int)(cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue / rate + 3000);
							double VolateC1 = line_forAnalysis_chart1[2].YValues.Value[indexC1];
							int indexC2 = (int)(cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue / rate + 3000);
							double VolateC2 = line_forAnalysis_chart1[2].YValues.Value[indexC2];
							string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
							string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
							DC_display_list1.Add(new class_AnalysisShow("过渡电阻1(Ω)", "0", "0", guodudianzuC1));
							DC_display_list1.Add(new class_AnalysisShow("过渡电阻2(Ω)", "0", "0", guodudianzuC2));

							DC_display_list1.Add(new class_AnalysisShow("桥接电阻1(Ω)", "0", "0", qiaojiedianzuC1));
							DC_display_list1.Add(new class_AnalysisShow("桥接电阻2(Ω)", "0", "0", qiaojiedianzuC2));
							//过渡时间1
							string guodushijianC1 = (cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue - cursor_collection[Tchart_1.Name + "C1"].XValue).ToString("0.##");
							//过渡时间2
							string guodushijianC2 = (cursor_collection[Tchart_1.Name + "C2"].XValue - cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue).ToString("0.##");
							DC_display_list1.Add(new class_AnalysisShow("过渡时间1(ms)", "0", "0", guodushijianC1));
							DC_display_list1.Add(new class_AnalysisShow("过渡时间2(ms)", "0", "0", guodushijianC2));
							//桥接时间
							string qiaojieshijianC = (cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue).ToString("0.##");
							DC_display_list1.Add(new class_AnalysisShow("桥接时间(ms)", "0", "0", qiaojieshijianC));
							DC_display_list1.Add(new class_AnalysisShow("切换时间(ms)", "0", "0", lbGapC.Content.ToString()));
							DC_display_list1.Add(new class_AnalysisShow("不同步时间(ms)", "0", "0", "0"));
						}

					}
					catch {

					}
				}
				#endregion
				#region 交流
				if (test_parameter._24IsACMeasurment) {
					AC_ThreePhase_display_list1.Clear();
					if (transformer_parameter._6Transformerphase == "三相") {
						if (transformer_parameter._13TransformerWindingConnMethod == "三角形接法") {
							TextBlock tbAphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_1, "tbAphaseVale");
							TextBlock tbBphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_1, "tbBphaseVale");
							TextBlock tbCphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_1, "tbCphaseVale");
							double a11 = cursor_collection[Tchart_1.Name + "A1"].XValue;
							double a12 = cursor_collection[Tchart_1.Name + "A2"].XValue;
							double b11 = cursor_collection[Tchart_1.Name + "B1"].XValue;
							double b12 = cursor_collection[Tchart_1.Name + "B2"].XValue;
							double c11 = cursor_collection[Tchart_1.Name + "C1"].XValue;
							double c12 = cursor_collection[Tchart_1.Name + "C2"].XValue;

							double a21 = cursor_collection_DC[Tchart_1.Name + "A1_DC"].XValue;
							double a22 = cursor_collection_DC[Tchart_1.Name + "A2_DC"].XValue;
							double b21 = cursor_collection_DC[Tchart_1.Name + "B1_DC"].XValue;
							double b22 = cursor_collection_DC[Tchart_1.Name + "B2_DC"].XValue;
							double c21 = cursor_collection_DC[Tchart_1.Name + "C1_DC"].XValue;
							double c22 = cursor_collection_DC[Tchart_1.Name + "C2_DC"].XValue;
							TrangleDataGridSort.Clear();
							TrangleDataGridSort.Add("A11", a11);
							TrangleDataGridSort.Add("A12", a12);
							TrangleDataGridSort.Add("B11", b11);
							TrangleDataGridSort.Add("B12", b12);
							TrangleDataGridSort.Add("C11", c11);
							TrangleDataGridSort.Add("C12", c12);
							TrangleDataGridSort.Add("A21", a21);
							TrangleDataGridSort.Add("A22", a22);
							TrangleDataGridSort.Add("B21", b21);
							TrangleDataGridSort.Add("B22", b22);
							TrangleDataGridSort.Add("C21", c21);
							TrangleDataGridSort.Add("C22", c22);
							TrangleDataGridSort = DictorySort(TrangleDataGridSort);


							if (TrangleDataGridSort.ElementAt(0).Key.Contains("A")) {
								//AB
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("B")) {
									HeaderA = "AB相值";
									tbAphase.Text = "AB相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";

										}
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
								}
								//AC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("C")) {
									HeaderA = "AC相";
									tbAphase.Text = "AC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("B")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";

										}
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										HeaderB = "BC相值";
										tbBphase.Text = "BC相值";
										HeaderC = "AB相值";
										tbCphase.Text = "AB相值";
									}
								}
							}
							else if (TrangleDataGridSort.ElementAt(0).Key.Contains("B")) {
								//BC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("C")) {
									HeaderA = "BC相值";
									tbAphase.Text = "BC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
								//AB
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("A")) {
									HeaderA = "AB相";
									tbAphase.Text = "AB相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}

								}
							}
							else {
								//BC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("B")) {
									HeaderA = "BC相值";
									tbAphase.Text = "BC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
								//AC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("A")) {
									HeaderA = "AC相";
									tbAphase.Text = "AC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
							}
							AC_ThreePhase_display_list1.Add(new class_AnalysisShow("切换时间(ms)", (TrangleDataGridSort.ElementAt(3).Value - TrangleDataGridSort.ElementAt(1).Value).ToString("0.#"), (TrangleDataGridSort.ElementAt(6).Value - TrangleDataGridSort.ElementAt(4).Value).ToString("0.#"), (TrangleDataGridSort.ElementAt(10).Value - TrangleDataGridSort.ElementAt(8).Value).ToString("0.#")));
						}
						else {
							TextBlock tbAphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_1, "tbAphaseVale");
							TextBlock tbBphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_1, "tbBphaseVale");
							TextBlock tbCphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_1, "tbCphaseVale");
							HeaderA = "A相值";
							HeaderB = "B相值";
							HeaderC = "C相值";
							tbAphase.Text = "A相值";
							tbBphase.Text = "B相值";
							tbCphase.Text = "C相值";
							AC_ThreePhase_display_list1.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							AC_ThreePhase_display_list1.Add(new class_AnalysisShow("不同步时间(ms)", "0", (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"), (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#")));
						}

					}
					else {
						AC_ThreePhase_display_list1.Add(new class_AnalysisShow("切换时间(ms)", "0", "0", lbGapC.Content.ToString()));
						if (transformer_parameter._13TransformerWindingConnMethod == "YN型接法") {
							AC_ThreePhase_display_list1.Add(new class_AnalysisShow("不同步时间(ms)", "0", "0", "0"));
						}
					}
				}
				#endregion

			}
			if (cursor_Name.Contains(Tchart_2.Name)) {
				#region 直流
				if (test_parameter._25IsDCMeasurment) {
					DC_display_list2.Clear();
					try {
						double current = 1;
						double rate = 0.05;
						if (test_parameter._16MeasureGear_DC == "0--20Ω") {
							current = 1;
						}
						if (test_parameter._16MeasureGear_DC == "20--100Ω") {
							current = 0.2;
						}
						if (transformer_parameter._6Transformerphase == "三相") {
							//过渡电压
							int indexguoduA1 = (int)(cursor_collection[Tchart_2.Name + "A1"].XValue / rate + 3000);
							double VolateguoduA1 = line_forAnalysis_chart2[0].YValues.Value[indexguoduA1];
							int indexguoduA2 = (int)(cursor_collection[Tchart_2.Name + "A2"].XValue / rate + 3000);
							double VolateguoduA2 = line_forAnalysis_chart2[0].YValues.Value[indexguoduA2];

							int indexguoduB1 = (int)(cursor_collection[Tchart_2.Name + "B1"].XValue / rate + 3000);
							double VolateguoduB1 = line_forAnalysis_chart2[1].YValues.Value[indexguoduB1];
							int indexguoduB2 = (int)(cursor_collection[Tchart_2.Name + "B2"].XValue / rate + 3000);
							double VolateguoduB2 = line_forAnalysis_chart2[1].YValues.Value[indexguoduB2];

							int indexguoduC1 = (int)(cursor_collection[Tchart_2.Name + "C1"].XValue / rate + 3000);
							double VolateguoduC1 = line_forAnalysis_chart2[2].YValues.Value[indexguoduC1];
							int indexguoduC2 = (int)(cursor_collection[Tchart_2.Name + "C2"].XValue / rate + 3000);
							double VolateguoduC2 = line_forAnalysis_chart2[2].YValues.Value[indexguoduC2];
							string guodudianzuA1 = (VolateguoduA1 / current).ToString("0.##");
							string guodudianzuA2 = (VolateguoduA2 / current).ToString("0.##");
							string guodudianzuB1 = (VolateguoduB1 / current).ToString("0.##");
							string guodudianzuB2 = (VolateguoduB2 / current).ToString("0.##");
							string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
							string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");

							//桥接电压
							int indexA1 = (int)(cursor_collection_DC[Tchart_2.Name + "A1_DC"].XValue / rate + 3000);
							double VolateA1 = line_forAnalysis_chart2[0].YValues.Value[indexA1];
							int indexA2 = (int)(cursor_collection_DC[Tchart_2.Name + "A2_DC"].XValue / rate + 3000);
							double VolateA2 = line_forAnalysis_chart2[0].YValues.Value[indexA2];

							int indexB1 = (int)(cursor_collection_DC[Tchart_2.Name + "B1_DC"].XValue / rate + 3000);
							double VolateB1 = line_forAnalysis_chart2[1].YValues.Value[indexB1];
							int indexB2 = (int)(cursor_collection_DC[Tchart_2.Name + "B2_DC"].XValue / rate + 3000);
							double VolateB2 = line_forAnalysis_chart2[1].YValues.Value[indexB2];

							int indexC1 = (int)(cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue / rate + 3000);
							double VolateC1 = line_forAnalysis_chart2[2].YValues.Value[indexC1];
							int indexC2 = (int)(cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue / rate + 3000);
							double VolateC2 = line_forAnalysis_chart2[2].YValues.Value[indexC2];
							string qiaojiedianzuA1 = (VolateA1 / current).ToString("0.##");
							string qiaojiedianzuA2 = (VolateA2 / current).ToString("0.##");
							string qiaojiedianzuB1 = (VolateB1 / current).ToString("0.##");
							string qiaojiedianzuB2 = (VolateB2 / current).ToString("0.##");
							string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
							string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
							DC_display_list2.Add(new class_AnalysisShow("过渡电阻1(Ω)", guodudianzuA1, guodudianzuB1, guodudianzuC1));
							DC_display_list2.Add(new class_AnalysisShow("过渡电阻2(Ω)", guodudianzuA2, guodudianzuB2, guodudianzuC2));

							DC_display_list2.Add(new class_AnalysisShow("桥接电阻1(Ω)", qiaojiedianzuA1, qiaojiedianzuB1, qiaojiedianzuC1));
							DC_display_list2.Add(new class_AnalysisShow("桥接电阻2(Ω)", qiaojiedianzuA2, qiaojiedianzuB2, qiaojiedianzuC2));
							//过渡时间1
							string guodushijianA1 = (cursor_collection_DC[Tchart_2.Name + "A1_DC"].XValue - cursor_collection[Tchart_2.Name + "A1"].XValue).ToString("0.##");
							string guodushijianB1 = (cursor_collection_DC[Tchart_2.Name + "B1_DC"].XValue - cursor_collection[Tchart_2.Name + "B1"].XValue).ToString("0.##");
							string guodushijianC1 = (cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue - cursor_collection[Tchart_2.Name + "C1"].XValue).ToString("0.##");
							//过渡时间2
							string guodushijianA2 = (cursor_collection[Tchart_2.Name + "A2"].XValue - cursor_collection_DC[Tchart_2.Name + "A2_DC"].XValue).ToString("0.##");
							string guodushijianB2 = (cursor_collection[Tchart_2.Name + "B2"].XValue - cursor_collection_DC[Tchart_2.Name + "B2_DC"].XValue).ToString("0.##");
							string guodushijianC2 = (cursor_collection[Tchart_2.Name + "C2"].XValue - cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue).ToString("0.##");
							DC_display_list2.Add(new class_AnalysisShow("过渡时间1(ms)", guodushijianA1, guodushijianB1, guodushijianC1));
							DC_display_list2.Add(new class_AnalysisShow("过渡时间2(ms)", guodushijianA2, guodushijianB2, guodushijianC2));
							//桥接时间
							string qiaojieshijianA = (cursor_collection_DC[Tchart_2.Name + "A2_DC"].XValue - cursor_collection_DC[Tchart_2.Name + "A1_DC"].XValue).ToString("0.##");
							string qiaojieshijianB = (cursor_collection_DC[Tchart_2.Name + "B2_DC"].XValue - cursor_collection_DC[Tchart_2.Name + "B1_DC"].XValue).ToString("0.##");
							string qiaojieshijianC = (cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue).ToString("0.##");
							DC_display_list2.Add(new class_AnalysisShow("桥接时间(ms)", qiaojieshijianA, qiaojieshijianB, qiaojieshijianC));
							DC_display_list2.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							DC_display_list2.Add(new class_AnalysisShow("不同步时间(ms)", "0", (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"), (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#")));
						}
						else {

							//过渡电压
							int indexguoduC1 = (int)(cursor_collection[Tchart_2.Name + "C1"].XValue / rate + 3000);
							double VolateguoduC1 = line_forAnalysis_chart2[2].YValues.Value[indexguoduC1];
							int indexguoduC2 = (int)(cursor_collection[Tchart_2.Name + "C2"].XValue / rate + 3000);
							double VolateguoduC2 = line_forAnalysis_chart2[2].YValues.Value[indexguoduC2];
							string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
							string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");

							//桥接电压
							int indexC1 = (int)(cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue / rate + 3000);
							double VolateC1 = line_forAnalysis_chart2[2].YValues.Value[indexC1];
							int indexC2 = (int)(cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue / rate + 3000);
							double VolateC2 = line_forAnalysis_chart2[2].YValues.Value[indexC2];
							string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
							string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
							DC_display_list2.Add(new class_AnalysisShow("过渡电阻1(Ω)", "0", "0", guodudianzuC1));
							DC_display_list2.Add(new class_AnalysisShow("过渡电阻2(Ω)", "0", "0", guodudianzuC2));

							DC_display_list2.Add(new class_AnalysisShow("桥接电阻1(Ω)", "0", "0", qiaojiedianzuC1));
							DC_display_list2.Add(new class_AnalysisShow("桥接电阻2(Ω)", "0", "0", qiaojiedianzuC2));
							//过渡时间1
							string guodushijianC1 = (cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue - cursor_collection[Tchart_2.Name + "C1"].XValue).ToString("0.##");
							//过渡时间2
							string guodushijianC2 = (cursor_collection[Tchart_2.Name + "C2"].XValue - cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue).ToString("0.##");
							DC_display_list2.Add(new class_AnalysisShow("过渡时间1(ms)", "0", "0", guodushijianC1));
							DC_display_list2.Add(new class_AnalysisShow("过渡时间2(ms)", "0", "0", guodushijianC2));
							//桥接时间
							string qiaojieshijianC = (cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue).ToString("0.##");
							DC_display_list2.Add(new class_AnalysisShow("桥接时间(ms)", "0", "0", qiaojieshijianC));
							DC_display_list2.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							DC_display_list2.Add(new class_AnalysisShow("不同步时间(ms)", "0", "0", "0"));
						}

					}
					catch {

					}
				}
				#endregion
				#region 交流
				if (test_parameter._24IsACMeasurment) {
					AC_ThreePhase_display_list2.Clear();
					if (transformer_parameter._6Transformerphase == "三相") {
						if (transformer_parameter._13TransformerWindingConnMethod == "三角形接法") {
							TextBlock tbAphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_2, "tbAphaseVale");
							TextBlock tbBphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_2, "tbBphaseVale");
							TextBlock tbCphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_2, "tbCphaseVale");
							double a11 = cursor_collection[Tchart_2.Name + "A1"].XValue;
							double a12 = cursor_collection[Tchart_2.Name + "A2"].XValue;
							double b11 = cursor_collection[Tchart_2.Name + "B1"].XValue;
							double b12 = cursor_collection[Tchart_2.Name + "B2"].XValue;
							double c11 = cursor_collection[Tchart_2.Name + "C1"].XValue;
							double c12 = cursor_collection[Tchart_2.Name + "C2"].XValue;

							double a21 = cursor_collection_DC[Tchart_2.Name + "A1_DC"].XValue;
							double a22 = cursor_collection_DC[Tchart_2.Name + "A2_DC"].XValue;
							double b21 = cursor_collection_DC[Tchart_2.Name + "B1_DC"].XValue;
							double b22 = cursor_collection_DC[Tchart_2.Name + "B2_DC"].XValue;
							double c21 = cursor_collection_DC[Tchart_2.Name + "C1_DC"].XValue;
							double c22 = cursor_collection_DC[Tchart_2.Name + "C2_DC"].XValue;
							TrangleDataGridSort.Clear();
							TrangleDataGridSort.Add("A11", a11);
							TrangleDataGridSort.Add("A12", a12);
							TrangleDataGridSort.Add("B11", b11);
							TrangleDataGridSort.Add("B12", b12);
							TrangleDataGridSort.Add("C11", c11);
							TrangleDataGridSort.Add("C12", c12);
							TrangleDataGridSort.Add("A21", a21);
							TrangleDataGridSort.Add("A22", a22);
							TrangleDataGridSort.Add("B21", b21);
							TrangleDataGridSort.Add("B22", b22);
							TrangleDataGridSort.Add("C21", c21);
							TrangleDataGridSort.Add("C22", c22);
							TrangleDataGridSort = DictorySort(TrangleDataGridSort);


							if (TrangleDataGridSort.ElementAt(0).Key.Contains("A")) {
								//AB
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("B")) {
									HeaderA = "AB相值";
									tbAphase.Text = "AB相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";

										}
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
								}
								//AC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("C")) {
									HeaderA = "AC相";
									tbAphase.Text = "AC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("B")) {
										//BC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";

										}
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										HeaderB = "BC相值";
										tbBphase.Text = "BC相值";
										HeaderC = "AB相值";
										tbCphase.Text = "AB相值";
									}
								}
							}
							else if (TrangleDataGridSort.ElementAt(0).Key.Contains("B")) {
								//BC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("C")) {
									HeaderA = "BC相值";
									tbAphase.Text = "BC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
								//AB
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("A")) {
									HeaderA = "AB相";
									tbAphase.Text = "AB相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}

								}
							}
							else {
								//BC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("B")) {
									HeaderA = "BC相值";
									tbAphase.Text = "BC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "AC相值";
											tbCphase.Text = "AC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "AC相值";
											tbBphase.Text = "AC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
								//AC
								if (TrangleDataGridSort.ElementAt(1).Key.Contains("A")) {
									HeaderA = "AC相";
									tbAphase.Text = "AC相值";
									if (TrangleDataGridSort.ElementAt(4).Key.Contains("C")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("A")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
									else if (TrangleDataGridSort.ElementAt(4).Key.Contains("A")) {
										//AC
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("B")) {
											HeaderB = "AB相值";
											tbBphase.Text = "AB相值";
											HeaderC = "BC相值";
											tbCphase.Text = "BC相值";
										}
									}
									else {
										if (TrangleDataGridSort.ElementAt(5).Key.Contains("C")) {
											HeaderB = "BC相值";
											tbBphase.Text = "BC相值";
											HeaderC = "AB相值";
											tbCphase.Text = "AB相值";
										}
									}
								}
							}
							AC_ThreePhase_display_list2.Add(new class_AnalysisShow("切换时间(ms)", (TrangleDataGridSort.ElementAt(3).Value - TrangleDataGridSort.ElementAt(1).Value).ToString("0.#"), (TrangleDataGridSort.ElementAt(6).Value - TrangleDataGridSort.ElementAt(4).Value).ToString("0.#"), (TrangleDataGridSort.ElementAt(10).Value - TrangleDataGridSort.ElementAt(8).Value).ToString("0.#")));

						}
						else {
							TextBlock tbAphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_2, "tbAphaseVale");
							TextBlock tbBphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_2, "tbBphaseVale");
							TextBlock tbCphase = FindFirstVisualChild<TextBlock>(datagridAC_Tchart_2, "tbCphaseVale");
							HeaderA = "A相值";
							HeaderB = "B相值";
							HeaderC = "C相值";
							tbAphase.Text = "A相值";
							tbBphase.Text = "B相值";
							tbCphase.Text = "C相值";
							AC_ThreePhase_display_list2.Add(new class_AnalysisShow("切换时间(ms)", lbGapA.Content.ToString(), lbGapB.Content.ToString(), lbGapC.Content.ToString()));
							AC_ThreePhase_display_list2.Add(new class_AnalysisShow("不同步时间(ms)", "0", (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"), (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#")));
						}

					}
					else {
						AC_ThreePhase_display_list2.Add(new class_AnalysisShow("切换时间(ms)", "0", "0", lbGapC.Content.ToString()));
						if (transformer_parameter._13TransformerWindingConnMethod == "YN型接法") {
							AC_ThreePhase_display_list2.Add(new class_AnalysisShow("不同步时间(ms)", "0", "0", "0"));
						}
					}
				}
				#endregion
			}
			datagridDC.ItemsSource = DC_display_list;
			datagridAC.ItemsSource = AC_ThreePhase_display_list;
			Chart_1column22.Header = HeaderA;
			Chart_1column33.Header = HeaderB;
			Chart_1column44.Header = HeaderC;
			Chart_2column22.Header = HeaderA;
			Chart_2column33.Header = HeaderB;
			Chart_2column44.Header = HeaderC;
			datagridAC_Tchart_1.ItemsSource = AC_ThreePhase_display_list1;
			datagridAC_Tchart_2.ItemsSource = AC_ThreePhase_display_list2;
			datagridDC_Tchart_2.ItemsSource = DC_display_list2;
			datagridDC_Tchart_1.ItemsSource = DC_display_list1;
		}
		#endregion

		#region 测试文件缓存模块
		void Delete_FilesAndFolders(string path) {
			try {
				Directory.Delete(path, true);
			}
			catch {

			}
		}
		void Delete_TreeViewNodes(string path) {
			string[] Node_names = path.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (TreeViewIconsItem item1 in TreeViewTestItem.Items) {
				if (item1.HeaderText == Node_names[0])//所属单位
                {
					foreach (TreeViewIconsItem item2 in item1.Items) {
						if (item2.HeaderText == Node_names[1])//变压器
                        {
							foreach (TreeViewIconsItem item3 in item2.Items) {
								if (item3.HeaderText == Node_names[2])//日期
                                {
									foreach (TreeViewIconsItem item4 in item3.Items) {
										if (item4.HeaderText == Node_names[3])//分接位置
                                        {
											for (int i = 0; i < item4.Items.Count; i++) {
												if (((TreeViewIconsItem)item4.Items[i]).HeaderText == Node_names[4])//第几次测试
                                                {
													item4.Items.RemoveAt(i);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		#endregion

	/******控件触发事件*******************************/
		#region 窗口加载
		int offset = 0;
		long filelength = 0;
		string path = "";
		double 误差比例 = 0;
		int 最小持续变化时间对应的点 = 0;
		int 最大持续不变时间对应的点 = 0;
		int 字节数 = 2;
		private void GZDLMainWindow_Loaded(object sender, RoutedEventArgs e) {
			PickerLineA相.temp = ChangeColorA;
			PickerLineB相.temp = ChangeColorB;
			PickerLineC相.temp = ChangeColorC;
			Mouse.OverrideCursor = Cursors.Hand;
			Window window1 = Window.GetWindow(menuItem1);
			Point point1 = menuItem1.TransformToAncestor(window1).Transform(new Point(0, 0));
			double offset1 = point1.X;
			ChangeTheme.Margin = new Thickness(10, 0, offset1 - 40, 0);
			#region 数据库初始化
			try {
				//数据库初始化
				if (OleDbHelper.OleDbHelperInit()) {
					LogHelper.Log_Write(LogHelper.Log_Level_e._2_Info, "ACCESS数据库初始化成功");
				}
			}
			catch (Exception ex) {
				LogHelper.Log_Write(LogHelper.Log_Level_e._0_Error, ex);
				MessageBox.Show("数据库初始化失败:" + ex.Message, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				this.Close();
				Application.Current.Shutdown();
			}
			#endregion
			//加最新的测试数据
			get_access_info();
			if (transformer_parameter._1ItsUnitName != "") {
				recursion_get_directory(test_data_path + transformer_parameter._1ItsUnitName, 0);
			}
			LogHelper.Log_Write(LogHelper.Log_Level_e._2_Info, "窗口加载完毕");
			#region checkBox初始化
			cbAp.IsChecked = true;
			cbBp.IsChecked = true;
			cbCp.IsChecked = true;
			cbAp_Chart1.IsChecked = true;
			cbBp_Chart1.IsChecked = true;
			cbCp_Chart1.IsChecked = true;
			cbAp_Chart2.IsChecked = true;
			cbBp_Chart2.IsChecked = true;
			cbCp_Chart2.IsChecked = true;
			#endregion
			#region Timer绘图 初始化
			Get_StateTimer.Interval = TimeSpan.FromMilliseconds(1000);
			Get_StateTimer.Tick += (EventHandler)delegate {
				CMD_Send(Commander._6_CMD_GETSTATE);
			};
			Draw_line_Timer.Interval = TimeSpan.FromMilliseconds(1000);
			// Fun_主任绘图(Colors.Black, line_forTest[0]);
			Draw_line_Timer.Tick += (EventHandler)delegate {
				if (var_绘图队列C.Count > 0) {
					this.Tchart1.Dispatcher.Invoke(new Action(delegate {
						if (load != null) {
							load.Close();
						}
						if (transformer_parameter._6Transformerphase == "三相") {

							Fun_绘制线条(var_绘图队列A.Dequeue(), Colors.Orange, line_forTest[0]);
							Fun_绘制线条(var_绘图队列B.Dequeue(), Colors.Green, line_forTest[1]);
							Fun_绘制线条(var_绘图队列C.Dequeue(), Colors.Red, line_forTest[2]);
						}
						if (transformer_parameter._6Transformerphase == "单相") {
							Fun_绘制线条(var_绘图队列C.Dequeue(), Colors.Red, line_forTest[2]);
						}

					}), DispatcherPriority.Background);
				}
			};
			#endregion
			#region Timer分析三相文件 初始化
			proTimer.Tick += (EventHandler)delegate {
				int 每次取的数据个数 = Page_Max_count * 500;
				if (offset < filelength) {
					short value = (short)((offset / (每次取的数据个数 * 字节数)));
					wait_analysis.getPro(offset);
					List<int> temp_list = new List<int>();
					temp_list.Clear();
					temp_list = Fun_读文件(path + "C相原始数据", 每次取的数据个数, value, 2);
					for (int i = 0; i < var_C相变化点位置.Length; i++) {
						var_C相变化点位置[i] = 0;
					}
					#region 交流
					if (test_parameter._24IsACMeasurment) {
						int[] C变化点 = Fun_文件分析变化点(temp_list, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, 0);
						if (C变化点[0] != 0&&C变化点[1]!=0) {
							for (int i = 0; i < C变化点.Length; i += 2) {
								if (C变化点[i] == 0) {
									break;
								}
								if (Math.Abs(C变化点[i] - C变化点[i + 1]) >= Page_Max_count / 3 * 80) {
									continue;
								}
								C相变化点集合_字典.Add("变化位置[" + (C相变化点集合_字典.Count+1) + "]", C变化点[i] * 字节数 + offset);
							}
						}
					}
					#endregion
					#region 直流
					if (test_parameter._25IsDCMeasurment) {
						Fun_找直流一段曲线里的变化起始点和结束点(temp_list, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
						if (var_C相变化点位置[0] != 0 && (var_C相变化点位置[1] - var_C相变化点位置[0]) > Page_Max_count / 2) {
							//A相变化点集合_字典.Add("变化位置[" + (var_C相变化点位置[0] * 字节数 + offset) + "]", var_C相变化点位置[0] * 字节数 + offset);
							//B相变化点集合_字典.Add("变化位置[" + (var_C相变化点位置[0] * 字节数 + offset) + "]", var_C相变化点位置[0] * 字节数 + offset);
							C相变化点集合_字典.Add("变化位置[" + (var_C相变化点位置[0] * 字节数 + offset) + "]", var_C相变化点位置[0] * 字节数 + offset);
						}
					}
					#endregion
					offset += 每次取的数据个数 * 字节数;
				}
				else {
					Fun_将字典内容加添加到_Lb();
					wait_analysis.Hide();
					if (lb_switch_list.Items.Count < 1) {
						MessageBox.Show("没有找到变化点");
					}
					proTimer.Stop();
				}
			};
			#endregion
			#region Timer分析文件单相 初始化
			proTimer_Single.Tick += (EventHandler)delegate {
				int 每次取的数据个数 = Page_Max_count * 300;
				if (offset < filelength) {
					short value = (short)(1 + (offset / (每次取的数据个数 * 字节数)));
					wait_analysis.getPro(offset);
					List<int> temp_list = Fun_读文件(path + "C相原始数据", 每次取的数据个数, value, 2);
					#region 交流
					if (test_parameter._24IsACMeasurment) {
						int[] C变化点 = Fun_文件分析变化点(temp_list, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, 0);
						if (C变化点[0] != 0) {
							for (int i = 0; i < C变化点.Length; i += 2) {
								if (C变化点[i] == 0) {
									break;
								}
								C相变化点集合_字典.Add("变化位置[" + (C变化点[i] * 字节数 + offset) + "]", C变化点[i] * 字节数 + offset);
							}
						}
					}
					#endregion
					#region 直流
					if (test_parameter._25IsDCMeasurment) {
						Fun_找直流一段曲线里的变化起始点和结束点(temp_list, var_C相变化率, 误差比例, 最小持续变化时间对应的点, 最大持续不变时间对应的点, var_C相变化点位置);
						if (var_C相变化点位置[0] != 0 && (var_C相变化点位置[1] - var_C相变化点位置[0]) > Page_Max_count) {
							C相变化点集合_字典.Add("变化位置[" + (var_C相变化点位置[0] * 字节数 + offset) + "]", var_C相变化点位置[0] * 字节数 + offset);
							var_C相变化点位置[0] = 0;
							var_C相变化点位置[1] = 0;
						}
					}
					#endregion
					offset += 每次取的数据个数 * 字节数;
				}
				else {
					Fun_将字典内容加添加到_Lb();
					wait_analysis.Hide();
					if (lb_switch_list.Items.Count < 1) {
						MessageBox.Show("没有找到变化点");
					}
					proTimer_Single.Stop();
				}
			};
			#endregion

		}
		#endregion

		#region tabAnalysis 加载
		private void tabAnalysis_Loaded(object sender, RoutedEventArgs e) {

		}
		#endregion

		#region 表格加载
		private void Grid_Loaded(object sender, RoutedEventArgs e) {
		}

		#endregion

		#region 变压器参数设置
		private void menuSysSetting_Click(object sender, RoutedEventArgs e) {
			transformer_configuration();
		}
		private void btnTransformerConfig_Click(object sender, RoutedEventArgs e) {
			transformer_configuration();
		}
		#endregion

		#region 测试参数设置
		private void menuTestParaSetup_Click(object sender, RoutedEventArgs e) {
			test_configuration();
		}
		#endregion

		#region 系统参数设置
		private void menuSystemSetup_Click(object sender, RoutedEventArgs e) {
			test_configuration();
		}
		#endregion

		#region TreeView右键菜单----增加
		/// <summary>
		/// 添加项
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItemADD_Click(object sender, RoutedEventArgs e) {
			transformer_configuration();
		}
		#endregion

		#region TreeView右键菜单----删除
		/// <summary>
		/// 删除项
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Click(object sender, RoutedEventArgs e) {
			if (TreeViewTestItem.SelectedItem != null) {
				if (MessageBox.Show("是否删除:\r\n" + ((TreeViewIconsItem)TreeViewTestItem.SelectedItem).HeaderText + "?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {

					if (MessageBox.Show("是否删除本地数据以及变压器相关配置信息?\r\n删除后数据不可恢复!请谨慎选择!", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
						Delete_FilesAndFolders(test_data_path + ((TreeViewIconsItem)TreeViewTestItem.SelectedItem).HeaderText);
						
						Delete_FilesAndFolders(folder);
						DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)((TreeViewIconsItem)TreeViewTestItem.SelectedItem).HeaderText);
						for (int i = 0; i < ds.Tables[0].Rows.Count; i++) {
							OleDbHelper.Delete_TransFormer(ds.Tables[0].Rows[i][(int)OleDbHelper.File_Name_e._2TransFormName + 1].ToString());
						}
						OleDbHelper.Delete_Company(((TreeViewIconsItem)TreeViewTestItem.SelectedItem).HeaderText);

						TreeViewTestItem.Items.Remove(TreeViewTestItem.SelectedItem);
						lb_switch_list.Items.Clear();
						return;
					}
					else {
						TreeViewTestItem.Items.Remove(TreeViewTestItem.SelectedItem);
					}
				}
				else {
					return;
				}
			}
			else {
				return;
			}
		}
		#endregion

		#region TreeView 子项右键菜单----增加
		private void MenuItem_add_node_Click(object sender, RoutedEventArgs e) {
			transformer_configuration();
		}
		#endregion

		#region TreeView 子项右键菜单---删除
		private void MenuItem_delete_node_Click(object sender, RoutedEventArgs e) {
			if (TreeViewTestItem.SelectedItem != null) {
				TreeViewIconsItem item = TreeViewTestItem.SelectedItem as TreeViewIconsItem;
				TreeViewIconsItem item_parent = item.Parent as TreeViewIconsItem;
				if (MessageBox.Show("是否删除:\r\n" + ((TreeViewIconsItem)TreeViewTestItem.SelectedItem).HeaderText + "?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
					if (MessageBox.Show("是否删除本地数据以及变压器相关配置信息?\r\n   删除后数据不可恢复!请谨慎选择!", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
						switch (item.TabIndex) {
							case 1: Delete_FilesAndFolders(test_data_path + item.HeaderText);
								DataSet ds = OleDbHelper.Select(OleDbHelper.File_Name_e._1ItsUnitName, (object)item.HeaderText);
								for (int i = 0; i < ds.Tables[0].Rows.Count; i++) {
									OleDbHelper.Delete_TransFormer(ds.Tables[0].Rows[i][(int)OleDbHelper.File_Name_e._2TransFormName + 1].ToString());
								}
								OleDbHelper.Delete_Company(item.HeaderText);

								break;
							case 2: Delete_FilesAndFolders(test_data_path + ((TreeViewIconsItem)item.Parent).HeaderText + "\\" + item.HeaderText);
								OleDbHelper.Delete_TransFormer(((TreeViewIconsItem)item.Parent).HeaderText, item.HeaderText);
								break;
							case 3: Delete_FilesAndFolders(test_data_path + ((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).HeaderText + "\\" + ((TreeViewIconsItem)item.Parent).HeaderText + "\\" + item.HeaderText); break;
							case 4: Delete_FilesAndFolders(test_data_path + ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).Parent).HeaderText + "\\" + ((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).HeaderText + "\\" + ((TreeViewIconsItem)item.Parent).HeaderText + "\\" + item.HeaderText); break;
							case 5: Delete_FilesAndFolders(test_data_path + ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).Parent).Parent).HeaderText + "\\" + ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).Parent).HeaderText + "\\" + ((TreeViewIconsItem)((TreeViewIconsItem)item.Parent).Parent).HeaderText + "\\" + ((TreeViewIconsItem)item.Parent).HeaderText + "\\" + item.HeaderText); break;
							case 11: break;
							case 22: break;
							case 33: break;
						}
						if (item_parent != null) {
							item_parent.Items.Remove(item);
						}
						else {
							TreeViewTestItem.Items.Remove(item);
						}
						lb_switch_list.Items.Clear();
						return;
					}
					else {
						if (item_parent != null) {
							item_parent.Items.Remove(item);
						}
						else {
							TreeViewTestItem.Items.Remove(item);
						}
						lb_switch_list.Items.Clear();
					}
					return;
				}
				else {
					return;
				}
			}
			else {
				return;
			}
		}
		#endregion

		#region Tchart 放大 缩小
		bool is_Enlarge = true;
		bool is_Narrow = false;
		#region 放大
		private void btnExpandImage_Click(object sender, RoutedEventArgs e) {
			switch ((sender as Button).Name) {
				case "btnEnlargeImage": expand(Tchart1); break;
				case "btnEnlargeImage_Tchart_1": expand(Tchart_1); break;
				case "btnEnlargeImage_Tchart_2": expand(Tchart_2); break;
				default: break;
			}
		}
		void expand(Steema.TeeChart.WPF.TChart chart) {
			if (is_Enlarge) {
				is_Enlarge = false;
				is_Narrow = true;
			}
			chart.Chart.Axes.Bottom.SetMinMax(chart.Chart.Axes.Bottom.Minimum + chart.Chart.Axes.Bottom.Maximum * 0.2, chart.Chart.Axes.Bottom.Maximum * 0.8);
			if (chart.Chart.Axes.Bottom.Maximum <= chart.Chart.Axes.Bottom.Minimum) {
				chart.Chart.Axes.Bottom.SetMinMax(chart.Chart.Axes.Bottom.Maximum * 0.99, chart.Chart.Axes.Bottom.Maximum);
				return;
			}

		}
		#endregion

		#region 缩小
		private void btnNarrowImage_Click(object sender, RoutedEventArgs e) {
			switch ((sender as Button).Name) {
				case "btnNarrowImage": narrow(Tchart1); break;
				case "btnNarrowImage_Tchart_1": narrow(Tchart_1); break;
				case "btnNarrowImage_Tchart_2": narrow(Tchart_2); break;
				default: break;
			}
		}
		void narrow(Steema.TeeChart.WPF.TChart chart) {

			if (is_Narrow) {
				is_Narrow = false;
				is_Enlarge = true;
			}
			chart.Chart.Axes.Bottom.SetMinMax(chart.Chart.Axes.Bottom.Minimum - (chart.Chart.Axes.Bottom.Maximum * 0.2), chart.Chart.Axes.Bottom.Maximum * 1.2);
			if (chart.Chart.Axes.Bottom.Minimum <= -100) {
				chart.Chart.Axes.Bottom.SetMinMax(0, chart.Chart.Axes.Bottom.Maximum);
				if (chart.Chart.Axes.Bottom.Maximum >= 1100) {
					chart.Chart.Axes.Bottom.SetMinMax(-100, 1100);
				}
				return;
			}
			if (chart.Chart.Axes.Bottom.Maximum >= 1100) {
				chart.Chart.Axes.Bottom.SetMinMax(chart.Chart.Axes.Bottom.Minimum, 1100);
				return;
			}

		}
		#endregion
		#endregion

		#region Tchart 波形 右移

		private void btnRightMove_Click(object sender, RoutedEventArgs e) {
			switch ((sender as Button).Name) {
				case "btnRightMove": rightmshift(Tchart1, int.Parse(tbMoveStepLong.Text)); break;
				case "btnRightMove_Tchart_1": rightmshift(Tchart_1, int.Parse(tbMoveStepLong_Tchart_1.Text)); break;
				case "btnRightMove_Tchart_2": rightmshift(Tchart_2, int.Parse(tbMoveStepLong_Tchart_2.Text)); break;
				default: break;
			}
		}
		void rightmshift(Steema.TeeChart.WPF.TChart chart, int step) {
			if (chart.Chart.Axes.Bottom.Maximum >= 1100) {
				chart.Chart.Axes.Bottom.Maximum = 1100;
				return;
			}
			chart.Chart.Axes.Bottom.Minimum += step;
			chart.Chart.Axes.Bottom.Maximum += step;
		}
		#endregion

		#region Tchart 波形 左移
		private void btnLeftMove_Click(object sender, RoutedEventArgs e) {
			switch ((sender as Button).Name) {
				case "btnLeftMove": leftmshift(Tchart1, int.Parse(tbMoveStepLong.Text)); break;
				case "btnLeftMove_Tchart_1": leftmshift(Tchart_1, int.Parse(tbMoveStepLong_Tchart_1.Text)); break;
				case "btnLeftMove_Tchart_2": leftmshift(Tchart_2, int.Parse(tbMoveStepLong_Tchart_2.Text)); break;
				default: break;
			}

		}

		void leftmshift(Steema.TeeChart.WPF.TChart chart, int step) {
			if (chart.Chart.Axes.Bottom.Minimum <= -100) {
				chart.Chart.Axes.Bottom.Minimum = -100;
				return;
			}
			chart.Chart.Axes.Bottom.Minimum -= step;
			chart.Chart.Axes.Bottom.Maximum -= step;
		}
		#endregion

		#region 还原
		private void btnReset_Click(object sender, RoutedEventArgs e) {
			if (transformer_parameter._6Transformerphase == "三相") {
				Cursor_Postion_Set("ABC", true);
				Tchart_ShowArea_Set("ABC");
				cbAp.IsChecked = true;
				cbBp.IsChecked = true;
				cbCp.IsChecked = true;
			}
			else {
				Cursor_Postion_Set("C", true);
				Tchart_ShowArea_Set("C");
				cbCp.IsChecked = true;
			}

		}

		#endregion

		#region 生成word试验报告点击事件

		private void btnExportWord_Click(object sender, RoutedEventArgs e) {
			WordTips Word导出设置窗口 = new WordTips();
			Word导出设置窗口.btn_Export.Click += (RoutedEventHandler)delegate {

				string image_path = root_path + "\\image\\temp.jpg";
				string model_path = root_path + "\\WordModel\\Model.dotx";
				string export_path = FileHelper.OpenDirectory() + "\\" + test_parameter._1curTransformerName + "--" + test_parameter._48Access_position + "测试报告.doc";
				if (export_path == null) {
					export_path = root_path + "\\WordModel\\测试报告.doc";
				}
				WordExport report = new WordExport();
				report.CreateNewDocument(model_path);
				Steema.TeeChart.WPF.Export.ImageExport image_export = new Steema.TeeChart.WPF.Export.ImageExport(Tchart1.Chart);
				image_export.JPEG.Save(image_path);
				report.InsertPicture("图片", image_path, 450, 250);
				if (test_parameter._24IsACMeasurment) {

					if (transformer_parameter._13TransformerWindingConnMethod == "YN型接法") {
						Microsoft.Office.Interop.Word.Table table = report.InsertTable("表格", 3, 4, 32);
						report.InsertCell(table, 1, 1, "交流分析结果");
						report.InsertCell(table, 2, 1, "切换时间(ms)");
						report.InsertCell(table, 3, 1, "不同步时间(ms)");
						report.InsertCell(table, 1, 2, "A相");
						report.InsertCell(table, 1, 3, "B相");
						report.InsertCell(table, 1, 4, "C相");
						//切换时间
						report.InsertCell(table, 2, 2, lbGapA.Content.ToString());
						report.InsertCell(table, 2, 3, lbGapB.Content.ToString());
						report.InsertCell(table, 2, 4, lbGapC.Content.ToString());
						//不同步时间
						report.InsertCell(table, 3, 2, "0");
						report.InsertCell(table, 3, 3, (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"));
						report.InsertCell(table, 3, 4, (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#"));
						if (transformer_parameter._6Transformerphase == "单相") {
							report.InsertCell(table, 3, 2, "0");
							report.InsertCell(table, 3, 3, "0");
							report.InsertCell(table, 3, 4, "0");
						}
					}
					if (transformer_parameter._13TransformerWindingConnMethod == "三角形接法") {
						Microsoft.Office.Interop.Word.Table table = report.InsertTable("表格", 2, 4, 32);
						report.InsertCell(table, 2, 2, (TrangleDataGridSort.ElementAt(3).Value - TrangleDataGridSort.ElementAt(1).Value).ToString("0.#"));
						report.InsertCell(table, 2, 3, (TrangleDataGridSort.ElementAt(6).Value - TrangleDataGridSort.ElementAt(4).Value).ToString("0.#"));
						report.InsertCell(table, 2, 4, (TrangleDataGridSort.ElementAt(10).Value - TrangleDataGridSort.ElementAt(8).Value).ToString("0.#"));
						report.InsertCell(table, 1, 1, "交流分析结果");
						report.InsertCell(table, 2, 1, "切换时间(ms)");
						report.InsertCell(table, 1, 2, HeaderA);
						report.InsertCell(table, 1, 3, HeaderB);
						report.InsertCell(table, 1, 4, HeaderC);
					}

				}
				else {
					try {
						double current = 1;
						double rate = 0.05;
						if (test_parameter._16MeasureGear_DC == "0--20Ω") {
							current = 1;
						}
						if (test_parameter._16MeasureGear_DC == "20--100Ω") {
							current = 0.2;
						}
						//过渡电压
						int indexguoduA1 = (int)(cursor_collection[Tchart1.Name + "A1"].XValue / rate + 3000);
						double VolateguoduA1 = line_forTest[0].YValues.Value[indexguoduA1];
						int indexguoduA2 = (int)(cursor_collection[Tchart1.Name + "A2"].XValue / rate + 3000);
						double VolateguoduA2 = line_forTest[0].YValues.Value[indexguoduA2];

						int indexguoduB1 = (int)(cursor_collection[Tchart1.Name + "B1"].XValue / rate + 3000);
						double VolateguoduB1 = line_forTest[1].YValues.Value[indexguoduB1];
						int indexguoduB2 = (int)(cursor_collection[Tchart1.Name + "B2"].XValue / rate + 3000);
						double VolateguoduB2 = line_forTest[1].YValues.Value[indexguoduB2];

						int indexguoduC1 = (int)(cursor_collection[Tchart1.Name + "C1"].XValue / rate + 3000);
						double VolateguoduC1 = line_forTest[2].YValues.Value[indexguoduC1];
						int indexguoduC2 = (int)(cursor_collection[Tchart1.Name + "C2"].XValue / rate + 3000);
						double VolateguoduC2 = line_forTest[2].YValues.Value[indexguoduC2];
						string guodudianzuA1 = (VolateguoduA1 / current).ToString("0.##");
						string guodudianzuA2 = (VolateguoduA2 / current).ToString("0.##");
						string guodudianzuB1 = (VolateguoduB1 / current).ToString("0.##");
						string guodudianzuB2 = (VolateguoduB2 / current).ToString("0.##");
						string guodudianzuC1 = (VolateguoduC1 / current).ToString("0.##");
						string guodudianzuC2 = (VolateguoduC2 / current).ToString("0.##");
						//桥接电压
						int indexA1 = (int)(cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue / rate + 3000);
						double VolateA1 = line_forTest[0].YValues.Value[indexA1];
						int indexA2 = (int)(cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue / rate + 3000);
						double VolateA2 = line_forTest[0].YValues.Value[indexA2];

						int indexB1 = (int)(cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue / rate + 3000);
						double VolateB1 = line_forTest[1].YValues.Value[indexB1];
						int indexB2 = (int)(cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue / rate + 3000);
						double VolateB2 = line_forTest[1].YValues.Value[indexB2];

						int indexC1 = (int)(cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue / rate + 3000);
						double VolateC1 = line_forTest[2].YValues.Value[indexC1];
						int indexC2 = (int)(cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue / rate + 3000);
						double VolateC2 = line_forTest[2].YValues.Value[indexC2];
						string qiaojiedianzuA1 = (VolateA1 / current).ToString("0.##");
						string qiaojiedianzuA2 = (VolateA2 / current).ToString("0.##");
						string qiaojiedianzuB1 = (VolateB1 / current).ToString("0.##");
						string qiaojiedianzuB2 = (VolateB2 / current).ToString("0.##");
						string qiaojiedianzuC1 = (VolateC1 / current).ToString("0.##");
						string qiaojiedianzuC2 = (VolateC2 / current).ToString("0.##");
						//过渡时间1
						string guodushijianA1 = (cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue - cursor_collection[Tchart1.Name + "A1"].XValue).ToString("0.##");
						string guodushijianB1 = (cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue - cursor_collection[Tchart1.Name + "B1"].XValue).ToString("0.##");
						string guodushijianC1 = (cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue - cursor_collection[Tchart1.Name + "C1"].XValue).ToString("0.##");
						//过渡时间2
						string guodushijianA2 = (cursor_collection[Tchart1.Name + "A2"].XValue - cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue).ToString("0.##");
						string guodushijianB2 = (cursor_collection[Tchart1.Name + "B2"].XValue - cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue).ToString("0.##");
						string guodushijianC2 = (cursor_collection[Tchart1.Name + "C2"].XValue - cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue).ToString("0.##");
						//桥接时间
						string qiaojieshijianA = (cursor_collection_DC[Tchart1.Name + "A2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "A1_DC"].XValue).ToString("0.##");
						string qiaojieshijianB = (cursor_collection_DC[Tchart1.Name + "B2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "B1_DC"].XValue).ToString("0.##");
						string qiaojieshijianC = (cursor_collection_DC[Tchart1.Name + "C2_DC"].XValue - cursor_collection_DC[Tchart1.Name + "C1_DC"].XValue).ToString("0.##");
						Microsoft.Office.Interop.Word.Table table = report.InsertTable("表格", 10, 4, 32);
						report.InsertCell(table, 1, 1, "直流分析结果");
						report.InsertCell(table, 2, 1, "过渡电阻1(Ω)");
						report.InsertCell(table, 3, 1, "过渡电阻2(Ω)");
						report.InsertCell(table, 4, 1, "桥接电阻1(Ω)");
						report.InsertCell(table, 5, 1, "桥接电阻2(ms)");
						report.InsertCell(table, 6, 1, "过渡时间1(ms)");
						report.InsertCell(table, 7, 1, "过渡时间2(ms)");
						report.InsertCell(table, 8, 1, "桥接时间(ms)");
						report.InsertCell(table, 9, 1, "切换时间(ms)");
						report.InsertCell(table, 10, 1, "不同步时间(ms)");
						report.InsertCell(table, 1, 2, "A相");
						report.InsertCell(table, 1, 3, "B相");
						report.InsertCell(table, 1, 4, "C相");
						//过渡电阻1
						report.InsertCell(table, 2, 2, guodudianzuA1);
						report.InsertCell(table, 2, 3, guodudianzuB1);
						report.InsertCell(table, 2, 4, guodudianzuC1);
						//过渡电阻2
						report.InsertCell(table, 3, 2, guodudianzuA2);
						report.InsertCell(table, 3, 3, guodudianzuB2);
						report.InsertCell(table, 3, 4, guodudianzuC2);
						//桥接电阻1
						report.InsertCell(table, 4, 2, qiaojiedianzuA1);
						report.InsertCell(table, 4, 3, qiaojiedianzuB1);
						report.InsertCell(table, 4, 4, qiaojiedianzuC1);
						//桥接电阻2
						report.InsertCell(table, 5, 2, qiaojiedianzuA2);
						report.InsertCell(table, 5, 3, qiaojiedianzuB2);
						report.InsertCell(table, 5, 4, qiaojiedianzuC2);
						//过渡时间1
						report.InsertCell(table, 6, 2, guodushijianA1);
						report.InsertCell(table, 6, 3, guodushijianB1);
						report.InsertCell(table, 6, 4, guodushijianC1);
						//过渡时间2
						report.InsertCell(table, 7, 2, guodushijianA2);
						report.InsertCell(table, 7, 3, guodushijianB2);
						report.InsertCell(table, 7, 4, guodushijianC2);
						//桥接时间
						report.InsertCell(table, 8, 2, qiaojieshijianA);
						report.InsertCell(table, 8, 3, qiaojieshijianB);
						report.InsertCell(table, 8, 4, qiaojieshijianC);
						//切换时间
						report.InsertCell(table, 9, 2, lbGapA.Content.ToString());
						report.InsertCell(table, 9, 3, lbGapB.Content.ToString());
						report.InsertCell(table, 9, 4, lbGapC.Content.ToString());
						//不同步时间
						report.InsertCell(table, 10, 2, "0");
						report.InsertCell(table, 10, 3, (float.Parse(Aphase_offset) - float.Parse(Bphase_offset)).ToString("0.#"));
						report.InsertCell(table, 10, 4, (float.Parse(Aphase_offset) - float.Parse(Cphase_offset)).ToString("0.#"));
					}
					catch {

					}

				}
				File.Delete(image_path);
				report.InsertText("出厂序号", transformer_parameter._22SwitchCode);
				report.InsertText("制造厂家", transformer_parameter._20SwitchManufactorName);
				report.InsertText("绕组接线方式", transformer_parameter._13TransformerWindingConnMethod);
				if (test_parameter._25IsDCMeasurment) {
					report.InsertText("测量方式", "  直流测量  ");
				}
				if (test_parameter._24IsACMeasurment) {
					report.InsertText("测量方式", "  交流测量  ");
				}
				report.InsertText("变压器名称", transformer_parameter._2TransformerName);
				report.InsertText("变压器型号", transformer_parameter._3TransformerModel);
				report.InsertText("结论", Word导出设置窗口.tb_Test_Conclusion.Text);
				report.InsertText("测试人员", Word导出设置窗口.tb_Test_Conclusion.Text);
				report.InsertText("报告审核", Word导出设置窗口.tb_Checker.Text);
				report.InsertText("报告批准", Word导出设置窗口.tb_Examer.Text);
				report.InsertText("打印日期", System.DateTime.Now.ToString("yyyy-MM-dd"));

				report.SaveDocument(export_path);
				MessageBox.Show("保存成功!\r\n路径:" + export_path);
				System.Diagnostics.Process.Start(export_path);
				Word导出设置窗口.Close();
			};
			Word导出设置窗口.Show();

		}
		#endregion

		#region 主题切换点击事件
		private void menuBlackback_Click(object sender, RoutedEventArgs e) {
			Steema.TeeChart.WPF.Themes.BlackIsBackTheme black_theme =
			new Steema.TeeChart.WPF.Themes.BlackIsBackTheme(Tchart1.Chart);
			black_theme.Apply(Tchart1.Chart);
		}

		private void menuBlueSkyTheme_Click(object sender, RoutedEventArgs e) {
			Steema.TeeChart.WPF.Themes.BlueSkyTheme bule_theme =
		   new Steema.TeeChart.WPF.Themes.BlueSkyTheme(Tchart1.Chart);
			bule_theme.Apply(Tchart1.Chart);

		}

		private void menuBusinessTheme_Click(object sender, RoutedEventArgs e) {
			Steema.TeeChart.WPF.Themes.BusinessTheme bussiness_theme = new Steema.TeeChart.WPF.Themes.BusinessTheme(Tchart1.Chart);
			bussiness_theme.Apply(Tchart1.Chart);
		}

		private void menuExcelTheme_Click(object sender, RoutedEventArgs e) {
			Steema.TeeChart.WPF.Themes.ExcelTheme excel_theme = new Steema.TeeChart.WPF.Themes.ExcelTheme(Tchart1.Chart);
			excel_theme.Apply(Tchart1.Chart);
		}
		private void menuOperaTheme_Click(object sender, RoutedEventArgs e) {
			Steema.TeeChart.WPF.Themes.OperaTheme opera_theme = new Steema.TeeChart.WPF.Themes.OperaTheme();
			opera_theme.Apply(Tchart1.Chart);
		}
		#endregion

		#region 连接仪器 点击事件
		private void MIConnect_Click(object sender, RoutedEventArgs e) {
			connect_device();
		}
		private void btnConnectDevice_Click(object sender, RoutedEventArgs e) {
			connect_device();
		}
		#endregion

		#region 数据库设置 点击事件
		private void menuDataBaseSetting_Click(object sender, RoutedEventArgs e) {
			OldcWindow1 ow = new OldcWindow1();
			ow.Show();
		}
		#endregion

		#region 辅助设置 点击事件
		private void menuAssistanceSetting_Click(object sender, RoutedEventArgs e) {
		}

		#endregion

		#region  分析参数设置 点击事件
		private void menuAnalysisSetting_Click(object sender, RoutedEventArgs e) {
			test_configuration();
		}
		#endregion

		#region 数据分析按钮单击事件
		private void btnDataAnalysis_Click(object sender, RoutedEventArgs e) {
			//if (TCP连接窗口 == null || !TCP连接窗口.is_Client_create_success)
			//{
			//    MessageBox.Show("抱歉,未检测到已连接仪器,无法执行此操作", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
			//    UI_logic();
			//    return;
			//}
			//CMD_Send(Commander._5_CMD_GETTRIGPARAM);
		}
		#endregion

		#region 开始测试按钮单击事件
		private void btnSartTest_Click(object sender, RoutedEventArgs e) {
			test_configuration();
		}
		#endregion

		#region 打开文件按钮单击事件
		string folder = string.Empty;
		private void btnOpenFile_Click(object sender, RoutedEventArgs e) {
			folder = FileHelper.OpenDirectory();
			string[] temp = folder.Split('\\');
			transformer_parameter._1ItsUnitName = temp[temp.Length - 1];
			recursion_get_directory(folder, 0);
		}
		private void miOpenFile_Click(object sender, RoutedEventArgs e) {
			string folder = FileHelper.OpenDirectory();
			string[] temp = folder.Split('\\');
			transformer_parameter._1ItsUnitName = temp[temp.Length - 1];
			recursion_get_directory(folder, 0);
		}
		#endregion

		#region treeView 子项 单击触发事件
		bool isTchart_1Draw = true;
		bool isTchart_2Draw = false;
		string 重新分析路径 = string.Empty;
		private void TreeViewTestItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (TreeViewTestItem.SelectedItem == null) {
				return;
			}
			LoadingWindow wait = new LoadingWindow("读取数据", "数据读取中请等待");
			wait.start = read;
			wait.Owner = GZDLMainWindow;
			TreeViewIconsItem select_item = (TreeViewIconsItem)TreeViewTestItem.SelectedItem;
			if (select_item.TabIndex == 1) {
				try {
					select_item.ContextMenu.Items.Remove(mi_导入上层);
					select_item.ContextMenu.Items.Remove(mi_导入下层);
				}
				catch {

				}
				transformer_parameter._1ItsUnitName = select_item.HeaderText;
				BindingTransformer_Info();
				select_item.IsExpanded = true;
			}
			if (select_item.TabIndex == 2) {
				try {
					select_item.ContextMenu.Items.Remove(mi_导入上层);
					select_item.ContextMenu.Items.Remove(mi_导入下层);
				}
				catch {

				}
				transformer_parameter._2TransformerName = select_item.HeaderText;
				transformer_parameter._1ItsUnitName = ((TreeViewIconsItem)select_item.Parent).HeaderText;
				get_access_info(transformer_parameter._1ItsUnitName, transformer_parameter._2TransformerName);
				BindingTransformer_Info();
				BindingTest_Info();
				select_item.IsExpanded = true;
				ListViewUpdate();
			}
			if (select_item.TabIndex == 3) {
				try {
					select_item.ContextMenu.Items.Remove(mi_导入上层);
					select_item.ContextMenu.Items.Remove(mi_导入下层);
				}
				catch {

				}
				test_parameter._47Test_Date = select_item.HeaderText;
				BindingTransformer_Info();
				BindingTest_Info();
				select_item.IsExpanded = true;
				// export_to_ListBox(select_item);
			}
			if (select_item.TabIndex == 4) {
				try {
					select_item.ContextMenu.Items.Remove(mi_导入上层);
					select_item.ContextMenu.Items.Remove(mi_导入下层);
				}
				catch {

				}
				test_parameter._48Access_position = select_item.HeaderText;
				select_item.IsExpanded = true;
				BindingTransformer_Info();
				BindingTest_Info();
			}
			if (select_item.TabIndex == 5) {
				//select_item.IsExpanded = true;
				if (tabAnalysis.IsSelected) {
					mi_导入上层.Header = "数据导入上层";
					mi_导入上层.Click += mi_导入到上层_Click;
					mi_导入下层.Header = "数据导入下层";
					mi_导入下层.Click += mi_导入到下层_Click;
					try {
						select_item.ContextMenu.Items.Remove(mi_导入上层);
						select_item.ContextMenu.Items.Remove(mi_导入下层);
					}
					catch {

					}
					select_item.ContextMenu.Items.Add(mi_导入上层);
					select_item.ContextMenu.Items.Add(mi_导入下层);
				}
				wait.Show();
				test_parameter._49Mesurent_Counts = select_item.HeaderText;
				test_parameter._48Access_position = ((TreeViewIconsItem)select_item.Parent).HeaderText;
				test_parameter._47Test_Date = ((TreeViewIconsItem)((TreeViewIconsItem)select_item.Parent).Parent).HeaderText;
				transformer_parameter._2TransformerName = ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)select_item.Parent).Parent).Parent).HeaderText;
				transformer_parameter._1ItsUnitName = ((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)((TreeViewIconsItem)select_item.Parent).Parent).Parent).Parent).HeaderText;
				btnReAnalysis.IsEnabled = false;
				if (tabAnalysis.IsSelected != true) {
					try {
						string temp_path =  test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts;
						if (!File.Exists(temp_path)) {
							string header = folder.Split(new string[]{transformer_parameter._1ItsUnitName},StringSplitOptions.None)[0];
							temp_path = header + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts;
						}
							try {
								select_item.ContextMenu.Items.Remove(mi_重新分析);
							}
							catch {

							}
							if (File.Exists(temp_path + "\\C相原始数据")) {
								mi_重新分析.Header = "重新分析";
								mi_重新分析.Click += mi_重新分析_Click;
								select_item.ContextMenu.Items.Add(mi_重新分析);
								btnReAnalysis.IsEnabled = true;
							}
								open_phase_source_data("ABC", temp_path);
					}
					catch (Exception err) {
						MessageBox.Show("没有找到可加载的数据!" + err.Message, "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
						((TreeViewIconsItem)select_item.Parent).Items.Remove(select_item);
						lb_switch_list.Items.Clear();
					}
				}
				else {

				}
				BindingTransformer_Info();
				BindingTest_Info();
				wait.Close();
			}
			get_access_info(OleDbHelper.File_Name_e._1ItsUnitName, transformer_parameter._1ItsUnitName, OleDbHelper.Test_File_Name_e._0curTransformerName, test_parameter._1curTransformerName);
		}
		TreeViewIconsItem select_parent;
		void export_to_ListBox(TreeViewIconsItem parent_item) {
			select_parent = parent_item;
			lb_switch_list.Items.Clear();
			foreach (TreeViewIconsItem child_item in parent_item.Items) {
				lb_switch_list.Items.Add(child_item.HeaderText);
			}
		}
		#endregion

		#region TreeView 子项 双击触发事件
		private void TreeViewTestItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			TreeViewIconsItem select_item = (TreeViewIconsItem)TreeViewTestItem.SelectedItem;
			if (select_item == null) {
				return;
			}
			switch (select_item.TabIndex) {
				case 1:
				case 2:  //transformer_configuration(); break;
				case 3:
				case 4:
				case 5: break;
				case 11:
				case 22:
				case 33: open_phase_source_data(select_item); break;
			}
		}

		#endregion

		#region 鼠标悬停重写
		private void btnConnectDevice_MouseEnter(object sender, MouseEventArgs e) {
			Button btn = sender as Button;
			btn.Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0));
		}

		private void btnConnectDevice_MouseLeave(object sender, MouseEventArgs e) {
			Button btn = sender as Button;
			btn.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
		}

		#endregion

		#region 保存数据

		private void btnSave_Click(object sender, RoutedEventArgs e) {
			if (lb_switch_list.Items.Count >= 1) {
				gpTree.IsEnabled = true;
				string path5 = test_data_path + transformer_parameter._1ItsUnitName + "\\" + test_parameter._1curTransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts;
				File.Delete(path5 + "\\A相原始数据");
				File.Delete(path5 + "\\B相原始数据");
				File.Delete(path5 + "\\C相原始数据");
				//StringBuilder sb = new StringBuilder();
				//sb.Append("误差比例\t变化率 \t测试电压\t采样率\r\n");
				//sb.Append(误差比例+"\t"+var_C相变化率+"\t"+test_parameter._2OutputVolt+"\t"+test_parameter._4SampleFrequency+"\r\n");
				//FileHelper.SaveFile_Append(test_data_path + "参数", sb.ToString(), 1024);
				lb_switch_list.Items.Clear();
				TreeViewUpdate(6);
				MessageBox.Show("保存成功!\r\n路径:" + path5 + "\r\n原始数据已删除!");
			}
			btnSave.IsEnabled = false;
			btnReAnalysis.IsEnabled = false;
		}
		#endregion

		#region 重新分析
		ModfiyParaPage window_重新分析 = null;
		void Fun_ChongXinFenXi(float 误差比例, float 持续时间) {
			test_parameter._29ErrorRation_AC = 误差比例.ToString();
			test_parameter._33MaxConstantTime_AC = 持续时间.ToString();
			Fun_将文件数据全部分析并将变化点加入字典();
		}
		private void btnReAnalysis_Click(object sender, RoutedEventArgs e) {
			if (window_重新分析 == null) {
				window_重新分析 = new ModfiyParaPage(Fun_ChongXinFenXi);
			}
			window_重新分析.Show();
			//btnReAnalysis.IsEnabled = false;
		}
		#endregion

		#region 暂停测试
		void fun_资源释放线程回收() {

			TCP连接窗口.is_end = true;
			is_开始接受 = false;
			TCP连接窗口.Sever_Reastart();
			Draw_line_Timer.Stop();
			if (th_read != null) {
				th_read.Abort();
				if (测试设置窗口.cb_AutoPause.IsChecked == true) {
					th_tolist.Abort();
				}
				Fun_缓存清空();
				th_analysis.Abort();
			}
		}
		private void btnPauseTest_Click(object sender, RoutedEventArgs e) {
			if (TCP连接窗口 == null || !TCP连接窗口.is_Client_create_success) {
				MessageBox.Show("抱歉,未检测到已连接仪器,无法执行此操作", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
				UI_logic();
				Get_StateTimer.Stop();
				return;
			}
			if (load != null) {
				load.Close();
			}
			CMD_Send(Commander._4_CMD_PAUSEMEASUER);
			if (测试设置窗口.cb_AutoPause.IsChecked == true) {
				测试设置窗口.cmbInnerSupplyVoltage.IsEnabled = false;
				fun_资源释放线程回收();
			}
			else {
				测试设置窗口.cmbInnerSupplyVoltage.IsEnabled = false;
				fun_资源释放线程回收();
				Fun_将文件数据全部分析并将变化点加入字典();
			}
			(sender as Button).IsEnabled = false;
			btnSartTest.IsEnabled = true;
			btnSystemSetting.IsEnabled = true;
			btnTransformerConfig.IsEnabled = true;
			btnReAnalysis.IsEnabled = true;
			//  cmbCurrentTestData.IsEnabled = true;
			Allow_Mouse_In = true;
			Mouse.OverrideCursor = Cursors.Hand;
			if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) == int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) - 1) {
				if (test_parameter._24IsACMeasurment) {
					test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + test_parameter._6AutoContinuousMeasurementEndTap + "]";
				}
				if (test_parameter._25IsDCMeasurment) {
					test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + test_parameter._6AutoContinuousMeasurementEndTap + "]";
				}
				test_parameter._5AutoContinuousMeasurementCurTap = transformer_parameter._25SwitchStopWorkingPosition;
				test_parameter._6AutoContinuousMeasurementEndTap = transformer_parameter._24SwitchStartWorkingPosition;
				测试设置窗口.tbContinuousTestCurTap.Text = test_parameter._5AutoContinuousMeasurementCurTap;
				测试设置窗口.tbContinuousTestEndTap.Text = test_parameter._6AutoContinuousMeasurementEndTap;
				OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap, test_parameter._5AutoContinuousMeasurementCurTap);
				OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._48Access_position, test_parameter._48Access_position);
				OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._5AutoContinuousMeasurementEndTap, test_parameter._6AutoContinuousMeasurementEndTap);
				OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
				ListViewUpdate();
				return;
			}
			if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) == int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) + 1) {
				if (test_parameter._24IsACMeasurment) {
					test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + test_parameter._6AutoContinuousMeasurementEndTap + "]";
				}
				if (test_parameter._25IsDCMeasurment) {
					test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + test_parameter._6AutoContinuousMeasurementEndTap + "]";
				}
				test_parameter._5AutoContinuousMeasurementCurTap = transformer_parameter._24SwitchStartWorkingPosition;
				test_parameter._6AutoContinuousMeasurementEndTap = transformer_parameter._25SwitchStopWorkingPosition;
				测试设置窗口.tbContinuousTestCurTap.Text = test_parameter._5AutoContinuousMeasurementCurTap;
				测试设置窗口.tbContinuousTestEndTap.Text = test_parameter._6AutoContinuousMeasurementEndTap;
				OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap, test_parameter._5AutoContinuousMeasurementCurTap);
				OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._48Access_position, test_parameter._48Access_position);
				OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._5AutoContinuousMeasurementEndTap, test_parameter._6AutoContinuousMeasurementEndTap);
				OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
				ListViewUpdate();
				return;
			}

			#region 分接位记录
			//交流连续自动测试
			if (test_parameter._24IsACMeasurment) {
				//交流连续自动测试
				if (test_parameter._14isAutoContinuousMearsurment) {
					if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
						test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
						test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString();
						if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) >= int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) - 1) {
							test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) - 1).ToString();
						}
					}
					else if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) > int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
						test_parameter._48Access_position = "交流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
						test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString();
						if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) <= 2 && int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) < 2) {
							test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) + 1).ToString();
						}
					}

					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap, test_parameter._5AutoContinuousMeasurementCurTap);
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._48Access_position, test_parameter._48Access_position);
					OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
					ListViewUpdate();
				}
			}
			else if (test_parameter._25IsDCMeasurment) {
				//直流连续自动测试
				if (test_parameter._14isAutoContinuousMearsurment) {
					if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) < int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
						test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString() + "]";
						test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) + 1).ToString();
						if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) >= int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) - 1) {
							test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) - 1).ToString();
						}
					}
					else if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) > int.Parse(test_parameter._6AutoContinuousMeasurementEndTap)) {
						test_parameter._48Access_position = "直流_分接位[" + test_parameter._5AutoContinuousMeasurementCurTap + "]→[" + (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString() + "]";
						test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) - 1).ToString();
						if (int.Parse(test_parameter._5AutoContinuousMeasurementCurTap) <= 2 && int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) < 2) {
							test_parameter._5AutoContinuousMeasurementCurTap = (int.Parse(test_parameter._6AutoContinuousMeasurementEndTap) + 1).ToString();
						}
					}
					OleDbHelper.Test_Setting_Parameters = OleDbHelper.Parameter_Value_Modify(OleDbHelper.Test_Setting_Parameters, (int)OleDbHelper.Test_File_Name_e._4AutoContinuousMeasurementCurTap, test_parameter._5AutoContinuousMeasurementCurTap);
					OleDbHelper.Update(OleDbHelper.Test_Setting_Parameters, OleDbHelper.Test_Table_Name, (int)OleDbHelper.Test_File_Name_e._00CompanyName, (int)OleDbHelper.Test_File_Name_e._0curTransformerName);
					ListViewUpdate();
				}
			}


			#endregion
		}
		#endregion

		#region 停止测试
		private void btnStopTest_Click(object sender, RoutedEventArgs e) {
			if (TCP连接窗口 == null || !TCP连接窗口.is_Client_create_success) {
				MessageBox.Show("抱歉,未检测到已连接仪器,无法执行此操作", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
				UI_logic();
				Get_StateTimer.Stop();
				return;
			}
			if (load != null) {
				load.Close();
			}
			if (测试设置窗口.rbInnernalPower.IsChecked == true) {
				CMD_Send(Commander._3_CMD_STOPMEASURE);
			}
			else {
				CMD_Send(Commander._4_CMD_PAUSEMEASUER);
			}
			if (测试设置窗口.cb_AutoPause.IsChecked == true) {
				测试设置窗口.cmbInnerSupplyVoltage.IsEnabled = true;
				fun_资源释放线程回收();
			}
			else {
				if (btnPauseTest.IsEnabled) {
					测试设置窗口.cmbInnerSupplyVoltage.IsEnabled = true;
					fun_资源释放线程回收();
					Fun_将文件数据全部分析并将变化点加入字典();
				}
			}
			测试设置窗口.cmbInnerSupplyVoltage.IsEnabled = true;
			(sender as Button).IsEnabled = false;
			btnSartTest.IsEnabled = true;
			btnSystemSetting.IsEnabled = true;
			btnTransformerConfig.IsEnabled = true;
			btnPauseTest.IsEnabled = false;
			btnReAnalysis.IsEnabled = true;
			// cmbCurrentTestData.IsEnabled = true;
			Allow_Mouse_In = true;
			Mouse.OverrideCursor = Cursors.Hand;
		}
		#endregion

		#region 设置线宽 Enter刷新事件
		private void TextBox_KeyUp(object sender, KeyEventArgs e) {
			try {
				if (e.Key == Key.Enter) {

					foreach (FastLine line in line_forAnalysis_chart1) {
						Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
						chartPen.Width = float.Parse(tb_LineWidth.Text.Trim());
						chartPen.Color = line.Color;
						line.LinePen = chartPen;
					}
					foreach (FastLine line in line_forTest) {
						Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
						chartPen.Width = float.Parse(tb_LineWidth.Text.Trim());
						chartPen.Color = line.Color;
						line.LinePen = chartPen;
					}
					foreach (FastLine line in line_forAnalysis_chart2) {
						Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
						chartPen.Width = float.Parse(tb_LineWidth.Text.Trim());
						chartPen.Color = line.Color;
						line.LinePen = chartPen;
					}
					checked_pos.Clear();
					for (int i = 0; i < 3; i++) {
						if (line_forTest[i].Active) {
							Steema.TeeChart.WPF.Drawing.ChartPen chartPen = new Steema.TeeChart.WPF.Drawing.ChartPen();
							chartPen.Width = float.Parse(tb_LineWidth.Text.Trim());
							chartPen.Color = line_forTest[i].Color;
							line_forTest[i].LinePen = chartPen;
						}
					}
				}
			}
			catch (Exception error) {
				MessageBox.Show(error.Message + "\r\n 请输入数字!", "程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

		}
		#endregion

		#region 重启设备
		private void btnReTest_Click(object sender, RoutedEventArgs e) {

		}
		#endregion

		#region 相别isChecked
		private void cbAp_Checked(object sender, RoutedEventArgs e) {
			CheckBox temp = sender as CheckBox;
			switch (temp.Name) {
				case "cbAp":
					line_forTest[0].Active = true;
					cursor_collection[Tchart1.Name + "A1"].Active = true;
					cursor_collection[Tchart1.Name + "A2"].Active = true;
					cursor_collection_DC[Tchart1.Name + "A1_DC"].Active = true;
					cursor_collection_DC[Tchart1.Name + "A2_DC"].Active = true;
					break;
				case "cbBp":
					line_forTest[1].Active = true;
					cursor_collection[Tchart1.Name + "B1"].Active = true;
					cursor_collection[Tchart1.Name + "B2"].Active = true;
					cursor_collection_DC[Tchart1.Name + "B1_DC"].Active = true;
					cursor_collection_DC[Tchart1.Name + "B2_DC"].Active = true;
					break;
				case "cbCp":
					line_forTest[2].Active = true;
					cursor_collection[Tchart1.Name + "C1"].Active = true;
					cursor_collection[Tchart1.Name + "C2"].Active = true;
					cursor_collection_DC[Tchart1.Name + "C1_DC"].Active = true;
					cursor_collection_DC[Tchart1.Name + "C2_DC"].Active = true;
					break;
				case "cbAp_Chart1":
					line_forAnalysis_chart1[0].Active = true;
					cursor_collection[Tchart_1.Name + "A1"].Active = true;
					cursor_collection[Tchart_1.Name + "A2"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "A1_DC"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "A2_DC"].Active = true;
					break;
				case "cbBp_Chart1":
					line_forAnalysis_chart1[1].Active = true;
					cursor_collection[Tchart_1.Name + "B1"].Active = true;
					cursor_collection[Tchart_1.Name + "B2"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "B1_DC"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "B2_DC"].Active = true;
					break;
				case "cbCp_Chart1":
					line_forAnalysis_chart1[2].Active = true;
					cursor_collection[Tchart_1.Name + "C1"].Active = true;
					cursor_collection[Tchart_1.Name + "C2"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "C1_DC"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "C2_DC"].Active = true;
					break;
				case "cbAp_Chart2":
					line_forAnalysis_chart2[0].Active = true;
					cursor_collection[Tchart_1.Name + "A1"].Active = true;
					cursor_collection[Tchart_1.Name + "A2"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "A1_DC"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "A2_DC"].Active = true;
					break;
				case "cbBp_Chart2":
					line_forAnalysis_chart2[1].Active = true;
					cursor_collection[Tchart_1.Name + "B1"].Active = true;
					cursor_collection[Tchart_1.Name + "B2"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "B1_DC"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "B2_DC"].Active = true;
					break;
				case "cbCp_Chart2":
					line_forAnalysis_chart2[2].Active = true;
					cursor_collection[Tchart_1.Name + "C1"].Active = true;
					cursor_collection[Tchart_1.Name + "C2"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "C1_DC"].Active = true;
					cursor_collection_DC[Tchart_1.Name + "C2_DC"].Active = true;
					break;
				default: break;
			}
			if (test_parameter._24IsACMeasurment && transformer_parameter._13TransformerWindingConnMethod != "三角形接法") {
				foreach (var item in cursor_collection_DC) {
					item.Value.Active = false;
				}
			}
		}
		private void cbAp_Unchecked(object sender, RoutedEventArgs e) {
			CheckBox temp = sender as CheckBox;
			switch (temp.Name) {
				case "cbAp":
					line_forTest[0].Active = false;
					cursor_collection[Tchart1.Name + "A1"].Active = false;
					cursor_collection[Tchart1.Name + "A2"].Active = false;
					cursor_collection_DC[Tchart1.Name + "A1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "A2_DC"].Active = false;
					break;
				case "cbBp":
					line_forTest[1].Active = false;
					cursor_collection[Tchart1.Name + "B1"].Active = false;
					cursor_collection[Tchart1.Name + "B2"].Active = false;
					cursor_collection_DC[Tchart1.Name + "B1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "B2_DC"].Active = false;
					break;
				case "cbCp":
					line_forTest[2].Active = false;
					cursor_collection[Tchart1.Name + "C1"].Active = false;
					cursor_collection[Tchart1.Name + "C2"].Active = false;
					cursor_collection_DC[Tchart1.Name + "C1_DC"].Active = false;
					cursor_collection_DC[Tchart1.Name + "C2_DC"].Active = false;
					break;
				case "cbAp_Chart1":
					line_forAnalysis_chart1[0].Active = false;
					cursor_collection[Tchart_1.Name + "A1"].Active = false;
					cursor_collection[Tchart_1.Name + "A2"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "A1_DC"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "A2_DC"].Active = false;
					break;
				case "cbBp_Chart1":
					line_forAnalysis_chart1[1].Active = false;
					cursor_collection[Tchart_1.Name + "B1"].Active = false;
					cursor_collection[Tchart_1.Name + "B2"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "B1_DC"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "B2_DC"].Active = false;
					break;
				case "cbCp_Chart1":
					line_forAnalysis_chart1[2].Active = false;
					cursor_collection[Tchart_1.Name + "C1"].Active = false;
					cursor_collection[Tchart_1.Name + "C2"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "C1_DC"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "C2_DC"].Active = false;
					break;
				case "cbAp_Chart2":
					line_forAnalysis_chart2[0].Active = true;
					cursor_collection[Tchart_1.Name + "A1"].Active = false;
					cursor_collection[Tchart_1.Name + "A2"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "A1_DC"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "A2_DC"].Active = false;
					break;
				case "cbBp_Chart2":
					line_forAnalysis_chart2[1].Active = false;
					cursor_collection[Tchart_1.Name + "B1"].Active = false;
					cursor_collection[Tchart_1.Name + "B2"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "B1_DC"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "B2_DC"].Active = false;
					break;
				case "cbCp_Chart2":
					line_forAnalysis_chart2[2].Active = false;
					cursor_collection[Tchart_1.Name + "C1"].Active = false;
					cursor_collection[Tchart_1.Name + "C2"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "C1_DC"].Active = false;
					cursor_collection_DC[Tchart_1.Name + "C2_DC"].Active = false;
					break;
				default: break;
			}
		}
		#endregion

		#region 拟合函数
		//double Get_w(int[] data) {
		//	int n = 0;
		//	int flag = 0;
		//	for (int i = 0; i < data.Length; i++) {
		//		if (i < data.Length - 1) {
		//			if ((data[i] <= 0 && data[i + 1] >= 0) || (data[i] >= 0 && data[i + 1] <= 0)) {
		//				flag++;
		//			}
		//		}
		//		if (flag == 1) {
		//			n++;
		//		}
		//	}

		//	return Math.PI / n;
		//}
		//int Get_Peak(int[] data) {
		//	int max = 0;
		//	for (int i = 0; i < data.Length / 10; i++) {
		//		max = max > Math.Abs(data[i]) ? max : Math.Abs(data[i]);

		//	}
		//	if (max >= int.Parse(test_parameter._46Peak_value) + 20000) {
		//		max = int.Parse(test_parameter._46Peak_value);
		//	}
		//	return max;
		//}
		//double Get_ρ(int[] data) {
		//	int n = 0;
		//	int max = Get_Peak(data);
		//	for (int i = 0; i < data.Length; i++) {
		//		if (data[i] >= max) {
		//			n = i;
		//			return Math.PI / 2 - Get_w(data) * n;
		//		}
		//	}
		//	return Math.PI / 2 - Get_w(data) * n;
		//}
		//Point get_A_m(int[] data) {
		//	Point temp = new Point();
		//	int max = Get_Peak(data);
		//	temp.X = max / 1000.0;
		//	temp.Y = 0;
		//	return temp;
		//}
		////Y=ASin(w*X+ρ)+m
		//double Match_Sin_Data(double x, double w, double ρ, Point A_m) {
		//	return A_m.X * Math.Sin(w * x + ρ) + A_m.Y;
		//}
		#endregion

		#region MenuItem附加事件
		MenuItem mi_导入上层 = new MenuItem();
		MenuItem mi_导入下层 = new MenuItem();
		MenuItem mi_重新分析 = new MenuItem();
		private void mi_导入到上层_Click(object sender, RoutedEventArgs e) {
			open_phase_source_data_for_Analysis("ABC", test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts, line_forAnalysis_chart1, Tchart_1.Name);
			isTchart_1Draw = false;
			isTchart_2Draw = true;
		}
		private void mi_导入到下层_Click(object sender, RoutedEventArgs e) {
			open_phase_source_data_for_Analysis("ABC", test_data_path + transformer_parameter._1ItsUnitName + "\\" + transformer_parameter._2TransformerName + "\\" + test_parameter._47Test_Date + "\\" + test_parameter._48Access_position + "\\" + test_parameter._49Mesurent_Counts, line_forAnalysis_chart2, Tchart_2.Name);
			isTchart_1Draw = true;
			isTchart_2Draw = false;
		}
		private void mi_重新分析_Click(object sender, RoutedEventArgs e) {
			Fun_将文件数据全部分析并将变化点加入字典();
		}
		private void tabcontrol_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (tabAnalysis.IsSelected == false) {
				for (int i = 0; i < 3; i++) {
					line_forAnalysis_chart1[i].Clear();
					line_forAnalysis_chart1[i].XValues.Clear();
					line_forAnalysis_chart1[i].YValues.Clear();
					line_forAnalysis_chart2[i].Clear();
					line_forAnalysis_chart2[i].XValues.Clear();
					line_forAnalysis_chart2[i].YValues.Clear();
				}
			}
		}
		private void TreeViewTestItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			var treeViewItem = VisualUpwardSearch<TreeViewIconsItem>(e.OriginalSource as DependencyObject) as TreeViewIconsItem;
			if (treeViewItem != null) {
				treeViewItem.Focus();
				e.Handled = true;
			}
		}
		#endregion

		#region 窗口关闭 资源释放
		private void GZDLMainWindow_Closed(object sender, EventArgs e) {
			if (TCP连接窗口 != null) {
				if (TCP连接窗口.is_Client_create_success) {
					CMD_Send(Commander._3_CMD_STOPMEASURE);
				}
			}
			System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
			System.Windows.Application.Current.Shutdown(0);
			System.Environment.Exit(0);
		}
		private void GZDLMainWindow_Closing(object sender, CancelEventArgs e) {
			if (TCP连接窗口 != null) {
				if (TCP连接窗口.is_Client_create_success) {
					CMD_Send(Commander._3_CMD_STOPMEASURE);
				}
			}
			System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
			System.Windows.Application.Current.Shutdown(0);
			System.Environment.Exit(0);
			System.Environment.Exit(0);
		}
		private void GZDLMainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
		}

		private void btnCloseWindows_Click(object sender, RoutedEventArgs e) {
			if (TCP连接窗口 != null) {
				if (TCP连接窗口.is_Client_create_success) {
					CMD_Send(Commander._3_CMD_STOPMEASURE);
				}
			}
			System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
			System.Windows.Application.Current.Shutdown(0);
			System.Environment.Exit(0);
		}
		#endregion

		static DependencyObject VisualUpwardSearch<T>(DependencyObject source) {
			while (source != null && source.GetType() != typeof(T))
				source = VisualTreeHelper.GetParent(source);

			return source;
		}


	
		

	}
}


