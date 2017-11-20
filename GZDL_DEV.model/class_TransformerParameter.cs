using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZDL_DEV.model
{
    public class class_TransformerParameter
    {
        /// <summary>
        /// 所属单位
        /// </summary>
        public string _1ItsUnitName { get; set; }
        /// <summary>
        /// 变压器名字
        /// </summary>
        public string _2TransformerName { get; set; }
        /// <summary>
        /// 变压器型号
        /// </summary>
        public string _3TransformerModel { get; set; }
        /// <summary>
        /// 变压器相数
        /// </summary>
        public bool _4Single_phase { get; set; }
        public bool _5Thrid_phase { get; set; }
        public string _6Transformerphase { get; set; }
    
        /// <summary>
        /// 变压器绕组数
        /// </summary>
        public bool _7Double_Winding { get; set; }
        public bool _8Three_Winding { get; set; }
        public string _9TransformerWinding { get; set; }
        
        /// <summary>
        /// 变压器绕组连接方式
        /// </summary>
        public bool _10Y_method { get; set; }
        public bool _11YN_method { get; set; }
        public bool _12Triangle_method { get; set; }
        public string _13TransformerWindingConnMethod { get; set; }
        
        /// <summary>
        /// 最大分接数
        /// </summary>
        public int _14TransformerMaxTap { get; set; }
        /// <summary>
        /// 中间分接位置
        /// </summary>
        
        public string _15MidPositionOfTap { get; set; }
        /// <summary>
        /// 过渡电阻模式
        /// </summary>
        public bool _16unkonwn { get; set; }
        public bool _17Single { get; set; }
        public bool _18Many { get; set; }
        public string _19Transition_resistance_mode { get; set; }
    
     
        /// <summary>
        /// 开关生产厂家
        /// </summary>
        public string _20SwitchManufactorName { get; set; }
        /// <summary>
        /// 开关型号
        /// </summary>
        public string _21SwitchModel { get; set; }
        /// <summary>
        /// 开关序列号
        /// </summary>
        public string _22SwitchCode { get; set; }
        /// <summary>
        /// 开关分列数
        /// </summary>
        public string _23SwitchColumnCount { get; set; }
        public string _24SwitchStartWorkingPosition { get; set; }
        public string _25SwitchStopWorkingPosition { get; set; }
        public string _26SwitchMidPosition { get; set; }
        public bool _27SwitchColumn_One_Count { get; set; }
        public bool _28SwitchColumn_Two_Count { get; set; }
        public bool _29SwitchColumn_Three_Count { get; set; }
    }
}
