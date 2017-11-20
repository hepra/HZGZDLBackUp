using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZDL_DEV.model
{
    public class class_TestParameter
    {
        /// <summary>
        /// 所属单位名称
        /// </summary>
        public string _0CompanyName { get; set; }
        /// <summary>
        /// 测试变压器名
        /// </summary>
        public string _1curTransformerName { get; set; }
        /// <summary>
        /// 内部电源电压
        /// </summary>
        public string _2OutputVolt { get; set; }
        /// <summary>
        /// 测试输出电压频率
        /// </summary>
        public string _3OutputVoltFrequency { get; set; }
        /// <summary>
        /// 测试采样频率
        /// </summary>
        public string _4SampleFrequency { get; set; }
        /// <summary>
        /// 自动连续测量的当前分接位置
        /// </summary>
        public string _5AutoContinuousMeasurementCurTap { get; set; }
        /// <summary>
        /// 自动连续测量的结束分接位置
        /// </summary>
        public string _6AutoContinuousMeasurementEndTap { get; set; }
        /// <summary>
        /// 单点分接位测试的当前分接位置
        /// </summary>
        public string _7SinglePointMeasurementCurTap { get; set; }
        /// <summary>
        /// 单点分接位测试 往前切换
        /// </summary>
        public bool _8SinglePointMeasurementForwardSwitch{ get; set; }
        /// <summary>
        /// 单点分接位测试 往后切换
        /// </summary>
        public bool _9SinglePointMeasurementBackSwitch { get; set; }
        public bool _14isAutoContinuousMearsurment { get; set; }
        public bool _15isHandleSingleMearsurment { get; set; }
        public string _16MeasureGear_DC { get; set; }
        public string _17SampleFrequency_DC{ get; set; }
        public bool _18isAutoContinuousMearsurment_DC { get; set; }
        public bool _19isHandleSingleMearsurment_DC { get; set; }
        public bool _20EnableDCfilter_DC { get; set; }
        public bool _21DisableDCfilter_DC { get; set; }
        public bool _22IsInnernalPower { get; set; }
        public bool _23IsExternalPower { get; set; }
        public bool _24IsACMeasurment { get; set; }
        public bool _25IsDCMeasurment { get; set; }
        public string Measurment { get; set; }
        public string _26MutationRation_DC { get; set; }
        public string _27MutationRation_AC { get; set; }
        public string _28ErrorRation_DC { get; set; }
        public string _29ErrorRation_AC { get; set; }
        public string _30MinChangeTime_DC { get; set; }
        public string _31MinChangeTime_AC { get; set; }
        public string _32MaxConstantTime_DC { get; set; }
        public string _33MaxConstantTime_AC { get; set; }
        public string _34IgnoreTime_DC { get; set; }
        public string _35IgnoreTime_AC { get; set; }
        public bool _36IsAutoAnalysisParameterSet_AC { get; set; }
        public bool _37IsAutoAnalysisParameterSet_DC { get; set; }
        public bool _38IsHandleAnalysisParameterSet_AC { get; set; }
        public bool _39IsHandleAnalysisParameterSet_DC { get; set; }
        public string _40Cursor_A1 { get; set; }
        public string _41Cursor_A2 { get; set; }
        public string _42Cursor_B1 { get; set; }
        public string _43Cursor_B2 { get; set; }
        public string _44Cursor_C1 { get; set; }
        public string _45Cursor_C2 { get; set; }
        public string _46Peak_value { get; set; }
        public string _47Test_Date {get;set;}
        public string _48Access_position { get; set; }
        public string _49Mesurent_Counts { get; set; }
        public string _50Cursor_A1_DC { get; set; }
        public string _51Cursor_A2_DC { get; set; }
        public string _52Cursor_B1_DC { get; set; }
        public string _53Cursor_B2_DC { get; set; }
        public string _54Cursor_C1_DC { get; set; }
        public string _55Cursor_C2_DC { get; set; }
    }
}
