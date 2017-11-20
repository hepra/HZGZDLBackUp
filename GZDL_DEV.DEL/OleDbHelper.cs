
using System;
using System.Collections.Generic;
using System.Text;

using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Collections;
using System.IO;
using ADOX;
using System.Windows;
using GZDL_DEV.DEL;

namespace CustomReport
{
    /**//// <summary>
    /// The OleDbHelper class is intended to encapsulate high performance, 
    /// scalable best practices for common uses of OleDb.
    /// </summary>
    public abstract class OleDbHelper
    {

        //Database connection strings
		static string root_path = (System.AppDomain.CurrentDomain.BaseDirectory);
        static string mdb_path = root_path + "Fra.mdb";
        public static string[] Setting_Items_Name = { 
                                                "所属单位名称",
                                                "变压器名称",
                                                "变压器型号",
                                                "变压器相数",
                                                "变压器绕组数",
                                                "变压器绕组连接方式",
                                                "开关生产厂家",
                                                "开关型号",
                                                "开关出厂日期",
                                                "开关分列数",
                                                "开关起始工作位置",
                                                "开关结束工作位置",
                                                "开关中间工作位置",
                                             };
        public enum File_Name_e
        {
            _1ItsUnitName = 0,
            _2TransFormName,
            _3TransFormModel,
            _6Transformerphase,
            _9TransformerWinding,
            _13TransformerWindingConnMethod,
            _20SwitchManufactorName,
            _21SwitchModel,
            _22SwitchCode,
            _23SwitchColumnCount,
            _24SwitchStartWorkingPosition,
            _25SwitchStopWorkingPosition,
            _26SwitchMidPosition
        }
        public static string[] Test_Items_Name = { 
                                                "所属单位名称",
                                                "变压器名称",
                                                "输出电压",
                                                "交流测试频率",
                                                "交流采样率",
                                                "自动连续测量当前位置",
                                                "自动连续测量结束位置",
                                                "单点测量当前位置",
                                                "向前切换",
                                                "向后切换",
                                                "启用直流过滤",
                                                "不器用直流过滤",
                                                "内置电源",
                                                "外接电源",
                                                "直流突变比例",
                                                "交流突变比例",
                                                "直流误差比例",
                                                "交流误差比例",
                                                "直流最大变化时间",
                                                "交流最小变化时间",
                                                "直流最小不变时间",
                                                "交流最大不变时间",
                                                "直流忽略时间",
                                                "交流忽略时间",
                                                "自动分析",
                                                "手动分析",
                                                "Measurment",
                                                "游标A1坐标",
                                                "游标A2坐标",
                                                "游标B1坐标",
                                                "游标B2坐标",
                                                "游标C1坐标",
                                                "游标C2坐标",
                                                "峰峰值",
                                                "测试日期",
                                                "测试分接位置",
                                                "测试次数",
                                                "Cursor_A1_DC",
                                                "Cursor_A2_DC",
                                                "Cursor_B1_DC",
                                                "Cursor_B2_DC",
                                                "Cursor_C1_DC",
                                                "Cursor_C2_DC",
                                                "自动连续测量",
                                                "单点测量"
                                             };
        public enum Test_File_Name_e
        {
            _00CompanyName = 0,
            _0curTransformerName,
            _1OutputVolt,
            _2OutputVoltFrequency,
            _3SampleFrequency,
            _4AutoContinuousMeasurementCurTap,
            _5AutoContinuousMeasurementEndTap,
            _6SinglePointMeasurementCurTap,
           _7SinglePointMeasurementForwardSwitch,
            _8SinglePointMeasurementBackSwitch,
            _20EnableDCfilter_DC,
            _21DisableDCfilter_DC,
            _22IsInnernalPower,
            _23IsExternalPower,              
            _26MutationRation_DC,              
            _27MutationRation_AC,                                               
            _28ErrorRation_DC,
            _29ErrorRation_AC,
            _30MinChangeTime_DC,
            _31MinChangeTime_AC,
            _32MaxConstantTime_DC,
            _33MaxConstantTime_AC,
            _34IgnoreTime_DC,
            _35IgnoreTime_AC,
            _36IsAutoAnalysisParameterSet_AC,
            _38IsHandleAnalysisParameterSet_AC,
            Measurment,
            _40Cursor_A1,
            _41Cursor_A2,
            _42Cursor_B1,
            _43Cursor_B2,
            _44Cursor_C1, 
            _45Cursor_C2,
            _46Peak_value,
            _47Test_Date,
            _48Access_position,
            _49Mesurent_Counts,
             _50Cursor_A1_DC,
             _51Cursor_A2_DC,
             _52Cursor_B1_DC,
             _53Cursor_B2_DC,
             _54Cursor_C1_DC,
             _55Cursor_C2_DC,
             _14isAutoContinuousMearsurment,
             _15isHandleSingleMearsurment
        }
        public class  Parameters
        {
            public Parameters(string filed_name,OleDbType filed_type, object value)
            {
                FieldName = filed_name;
                FieldType = filed_type;
                Value = value;
            }
            public string FieldName { get; set; }
            public OleDbType FieldType { get; set; } 
            public object Value { get; set; }
        }
       static public List<Parameters> TransForm_Setting_Parameters = new List<Parameters>();
       static public List<Parameters> Test_Setting_Parameters = new List<Parameters>();
       private static StringBuilder strCmd_Select = new StringBuilder();
       private static StringBuilder strCmd_Insert = new StringBuilder();
       private static StringBuilder strCmd_Update = new StringBuilder();
       private static StringBuilder strCmd_Delete = new StringBuilder();
	   private static readonly string strConnection = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + mdb_path;
       public static string TransFormer_Table_Name = "GZDL_Devices_TransInfo_Table";
       public static string Test_Table_Name = "GZDL_Devices_TestInfo_Table";


        // Hashtable to store cached parameters
        private static Hashtable parmCache = Hashtable.Synchronized(new Hashtable());
        //插入命令创建
        static void Create_CMD_Insert(List<Parameters> parameters,string table_name)
        {
            strCmd_Insert.Clear();
            strCmd_Insert.Append("insert into " + table_name + "(");
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i == parameters.Count - 1)
                {
                    strCmd_Insert.Append(parameters[i].FieldName + ")");
                    break;
                }
                strCmd_Insert.Append(parameters[i].FieldName + ",");
            }
            strCmd_Insert.Append(" values(");
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i == parameters.Count - 1)
                {
                    strCmd_Insert.Append("@" + parameters[i].FieldName + ")");
                    break;
                }
                strCmd_Insert.Append("@" + parameters[i].FieldName + ",");
            }
        }
       
        //Update命令创建
        static void Create_CMD_Update(List<Parameters> parameters,string table_name,params int []specific_location)
        {
            strCmd_Update.Clear();
            strCmd_Update.Append("update " + table_name + " set ");
            int flag = 100;
            for (int i = 0; i < parameters.Count; i++)
            {
                foreach (int j in specific_location)
                {
                    if (i == j)
                    {
                        flag = i;
                        continue;
                    }
                }
                if (flag != 100 && flag == i)
                {
                    continue;
                }
                if (i == parameters.Count - 1)
                {
                    strCmd_Update.Append(parameters[i].FieldName + "=@" + parameters[i].FieldName);
                    break;
                }
                strCmd_Update.Append(parameters[i].FieldName + "=@" + parameters[i].FieldName + ",");
            }
            strCmd_Update.Append(" where ");
            for (int j = 0; j < specific_location.Length;j++ )
            {
                if (j == specific_location.Length - 1)
                {
                    strCmd_Update.Append(parameters[specific_location[j]].FieldName + "='" + parameters[specific_location[j]].Value + "'");
                    break;
                }
                strCmd_Update.Append(parameters[specific_location[j]].FieldName + "='" + parameters[specific_location[j]].Value + "' and ");
            }
        }

        //Delete删除命令创建
        static void Create_CMD_Delete(List<Parameters> parameters, string table_name, params int[] specific_location)
        {
            strCmd_Delete.Clear();
            strCmd_Delete.Append("delete from " + table_name);
            strCmd_Delete.Append(" where ");
            for (int j = 0; j < specific_location.Length; j++)
            {
                if (j == specific_location.Length - 1)
                {
                    strCmd_Delete.Append(parameters[specific_location[j]].FieldName + "='" + parameters[specific_location[j]].Value + "'");
                    break;
                }
                strCmd_Delete.Append(parameters[specific_location[j]].FieldName + "='" + parameters[specific_location[j]].Value + "' and ");
            }
        }
        /// <summary>
        /// 调用 OleDbHelper 必须先初始化
        /// </summary>
        /// <returns></returns>
        public static bool OleDbHelperInit()
        {
            //TransFormer 参数初始化
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._1ItsUnitName], OleDbType.LongVarWChar, "unkonwn"));//0
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._2TransFormName], OleDbType.LongVarWChar, "unknown"));//1
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._3TransFormModel], OleDbType.LongVarWChar, DateTime.Now.ToString("yyyy-MM-dd")));//2
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._6Transformerphase], OleDbType.LongVarWChar, "1"));//3
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._9TransformerWinding], OleDbType.LongVarWChar, "unknown"));//4
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._13TransformerWindingConnMethod], OleDbType.LongVarWChar, "unknown"));//5
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._20SwitchManufactorName], OleDbType.LongVarWChar, "unknown"));//6
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._21SwitchModel], OleDbType.LongVarWChar, "unknown"));//7
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._22SwitchCode], OleDbType.LongVarWChar, "unknown"));//8
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._23SwitchColumnCount], OleDbType.LongVarWChar, "unknown"));//9
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._24SwitchStartWorkingPosition], OleDbType.LongVarWChar, "unknown"));//10
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._25SwitchStopWorkingPosition], OleDbType.LongVarWChar, "unkonwn"));//11
            TransForm_Setting_Parameters.Add(new Parameters(Setting_Items_Name[(int)File_Name_e._26SwitchMidPosition], OleDbType.LongVarWChar, "unkonwn"));//12

            // Test参数初始化
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._00CompanyName], OleDbType.LongVarChar, "无"));//0
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._0curTransformerName], OleDbType.LongVarChar, "无"));//0
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._1OutputVolt], OleDbType.LongVarChar, "无"));//1
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._2OutputVoltFrequency], OleDbType.LongVarChar, "无"));//2
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._3SampleFrequency], OleDbType.LongVarChar, "无"));//3
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._4AutoContinuousMeasurementCurTap], OleDbType.LongVarChar, "无"));//4
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._5AutoContinuousMeasurementEndTap], OleDbType.LongVarChar, "无"));//5
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._6SinglePointMeasurementCurTap], OleDbType.LongVarChar, "无"));//6
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._7SinglePointMeasurementForwardSwitch], OleDbType.Boolean, false));//7
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._8SinglePointMeasurementBackSwitch], OleDbType.Boolean, false));//7
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._20EnableDCfilter_DC], OleDbType.Boolean, true));//13
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._21DisableDCfilter_DC], OleDbType.Boolean,false));//14
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._22IsInnernalPower], OleDbType.Boolean, true));//15
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._23IsExternalPower], OleDbType.Boolean, false));//16
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._26MutationRation_DC], OleDbType.LongVarChar, "10.0"));//17
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._27MutationRation_AC], OleDbType.LongVarChar, "10.0"));//18
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._28ErrorRation_DC], OleDbType.LongVarChar, "2.0"));//19
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._29ErrorRation_AC], OleDbType.LongVarChar, "2.0"));//20
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._30MinChangeTime_DC], OleDbType.LongVarChar, "0.2"));//21
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._31MinChangeTime_AC], OleDbType.LongVarChar, "0.2"));//22
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._32MaxConstantTime_DC], OleDbType.LongVarChar, "1.5"));//23
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._33MaxConstantTime_AC], OleDbType.LongVarChar, "1.5"));//24
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._34IgnoreTime_DC], OleDbType.LongVarChar, "80"));//25
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._35IgnoreTime_AC], OleDbType.LongVarChar, "80"));//26
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._36IsAutoAnalysisParameterSet_AC], OleDbType.Boolean, true));//27
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._38IsHandleAnalysisParameterSet_AC], OleDbType.Boolean, false));//28
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e.Measurment], OleDbType.LongVarChar, "交流"));//29
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._40Cursor_A1], OleDbType.LongVarChar, "0"));//30
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._41Cursor_A2], OleDbType.LongVarChar, "0"));//31
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._42Cursor_B1], OleDbType.LongVarChar, "0"));//32
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._43Cursor_B2], OleDbType.LongVarChar, "0"));//33
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._44Cursor_C1], OleDbType.LongVarChar, "0"));//34
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._45Cursor_C2], OleDbType.LongVarChar, "0"));//35
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._46Peak_value], OleDbType.LongVarChar, "0"));//36
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._47Test_Date], OleDbType.LongVarChar, "2017-1-1"));//37
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._48Access_position], OleDbType.LongVarChar, "未知"));//38
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._49Mesurent_Counts], OleDbType.LongVarChar, "第一次测试"));//39
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._50Cursor_A1_DC], OleDbType.LongVarChar, "0"));//40
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._51Cursor_A2_DC], OleDbType.LongVarChar, "0"));//41
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._52Cursor_B1_DC], OleDbType.LongVarChar, "0"));//42
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._53Cursor_B2_DC], OleDbType.LongVarChar, "0"));//43
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._54Cursor_C1_DC], OleDbType.LongVarChar, "0"));//44
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._55Cursor_C2_DC], OleDbType.LongVarChar, "0"));//45
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._14isAutoContinuousMearsurment], OleDbType.Boolean, false));//45
            Test_Setting_Parameters.Add(new Parameters(Test_Items_Name[(int)Test_File_Name_e._15isHandleSingleMearsurment], OleDbType.Boolean, true));//45

            ////////////////////////////////////////////////////////////////////////////////////////////
            Catalog catalog = new Catalog();
            //数据库不存在则创建
            if (!File.Exists(mdb_path))
            {
                try
                {
                    catalog.Create(strConnection);
                }
                catch (System.Exception ex)
                {
                    LogHelper.Log_Write(LogHelper.Log_Level_e._1_Warn, ex);
                    return false;
                }
            }
            //创建表格
            try
            {
                //测试信息表格
                create_Test_Setting_Table(catalog, Test_Table_Name);
                //变压器信息表格
                create_TransForm_Setting_Table(catalog, TransFormer_Table_Name);
               // create_Sys_Log_Table(catalog);
            }
           //若异常 则表格已存在
            catch (Exception ex)
            {
                LogHelper.Log_Write(LogHelper.Log_Level_e._1_Warn, ex);
                return false;
            }
            return true;
        }
        #region 创建变压器配置参数的存放表格
        private static  void create_TransForm_Setting_Table( Catalog catalog,string TableName)
        {
            Create_CMD_Insert(TransForm_Setting_Parameters, TableName);
            ADODB.Connection cn = new ADODB.Connection();
            cn.Open(strConnection, null, null, -1);
            catalog.ActiveConnection = cn;
            Table table = new Table();
            table.Name = TableName;
            table.Columns.Append("Seting_ID", DataTypeEnum.adInteger, 9);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._1ItsUnitName].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._2TransFormName].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._3TransFormModel].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._6Transformerphase].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._9TransformerWinding].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._13TransformerWindingConnMethod].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._20SwitchManufactorName].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._21SwitchModel].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._22SwitchCode].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._23SwitchColumnCount].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._24SwitchStartWorkingPosition].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._25SwitchStopWorkingPosition].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(TransForm_Setting_Parameters[(int)File_Name_e._26SwitchMidPosition].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns["Seting_ID"].ParentCatalog = catalog;
            table.Columns["Seting_ID"].Properties["AutoIncrement"].Value = true;//设置自动增长
            table.Keys.Append("FirstTablePrimaryKey", KeyTypeEnum.adKeyPrimary, table.Columns["Seting_ID"], null, null); //定义主键
            try
            {
                catalog.Tables.Append(table);  
            }
            catch(Exception ex)
            {
              //  LogHelper.Log_Write(LogHelper.Log_Level_e._1_Warn, ex);
            }
            cn.Close();
        }
        #endregion
        #region 创建 测试参数的存放表格
        private static void create_Test_Setting_Table(Catalog catalog, string TableName)
        {
            Create_CMD_Insert(Test_Setting_Parameters,TableName);
            ADODB.Connection cn = new ADODB.Connection();
            cn.Open(strConnection, null, null, -1);
            catalog.ActiveConnection = cn;
            Table table = new Table();
            table.Name = TableName;
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._00CompanyName].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._0curTransformerName].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._1OutputVolt].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._2OutputVoltFrequency].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._3SampleFrequency].FieldName, DataTypeEnum.adLongVarWChar, 9);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._4AutoContinuousMeasurementCurTap].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._5AutoContinuousMeasurementEndTap].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._6SinglePointMeasurementCurTap].FieldName, DataTypeEnum.adInteger, 9);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._7SinglePointMeasurementForwardSwitch].FieldName, DataTypeEnum.adBoolean, 9);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._8SinglePointMeasurementBackSwitch].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._20EnableDCfilter_DC].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._21DisableDCfilter_DC].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._22IsInnernalPower].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._23IsExternalPower].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._26MutationRation_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._27MutationRation_AC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._28ErrorRation_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._29ErrorRation_AC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._30MinChangeTime_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._31MinChangeTime_AC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._32MaxConstantTime_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._33MaxConstantTime_AC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._34IgnoreTime_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._35IgnoreTime_AC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._36IsAutoAnalysisParameterSet_AC].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._38IsHandleAnalysisParameterSet_AC].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e.Measurment].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._40Cursor_A1].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._41Cursor_A2].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._42Cursor_B1].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._43Cursor_B2].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._44Cursor_C1].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._45Cursor_C2].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._46Peak_value].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._47Test_Date].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._48Access_position].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._49Mesurent_Counts].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._50Cursor_A1_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._51Cursor_A2_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._52Cursor_B1_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._53Cursor_B2_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._54Cursor_C1_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._55Cursor_C2_DC].FieldName, DataTypeEnum.adLongVarWChar, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._14isAutoContinuousMearsurment].FieldName, DataTypeEnum.adBoolean, 20);
            table.Columns.Append(Test_Setting_Parameters[(int)Test_File_Name_e._15isHandleSingleMearsurment].FieldName, DataTypeEnum.adBoolean, 20);
            try
            {
                catalog.Tables.Append(table);
            }
            catch (Exception ex)
            {
              //  LogHelper.Log_Write(LogHelper.Log_Level_e._1_Warn, ex);
            }
			finally {
				cn.Close();
			}
        }


        #endregion
        #region 插入
        static public void Insert(List<Parameters> Setting_parameters,string table_name)
        {
            Create_CMD_Insert(Setting_parameters, table_name);
          
            OleDbParameter[] parameters = new OleDbParameter[Setting_parameters.Count];
            //参数赋值
            for (int i = 0; i < Setting_parameters.Count; i++)
            {
                parameters[i] = new OleDbParameter(Setting_parameters[i].FieldName, Setting_parameters[i].FieldType);
                if (Setting_parameters[i].Value == null)
                {
                    parameters[i].Value = "NULL";
                }
                else
                {
                    parameters[i].Value = Setting_parameters[i].Value;
                }
            }
            //执行数据库操作      
            string temp = strCmd_Insert.ToString();
            ExecuteNonQuery(strCmd_Insert.ToString(), parameters);
        }
        #endregion
        #region 查询数据  重载
        /// <summary>
        /// 查询变压器参数
        /// </summary>
        /// <param name="Filed_Name"></param>
        /// <param name="Filed_Value"></param>
        /// <returns></returns>
        public static DataSet Select( File_Name_e Filed_Name,object Filed_Value )
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  [" + TransFormer_Table_Name + " ] where " + Setting_Items_Name[(int)Filed_Name] + "=@" + Setting_Items_Name[(int)Filed_Name]);
            OleDbParameter[] parameters = new OleDbParameter[1];
            //参数赋值
            parameters[0] = new OleDbParameter(Setting_Items_Name[(int)Filed_Name], TransForm_Setting_Parameters[(int)Filed_Name].FieldType);
            parameters[0].Value = Filed_Value;
            return ExecuteDataSet(strCmd_Select.ToString(), parameters);
        }
        public static DataSet Select(string table_name)
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  [" + table_name + " ] ");
            return ExecuteDataSet(strCmd_Select.ToString());
        }


       /// <summary>
       /// 查询 测试参数
       /// </summary>
       /// <param name="Filed_Name"></param>
       /// <param name="Filed_Value"></param>
       /// <returns></returns>
        public static DataSet Select(Test_File_Name_e Filed_Name, object Filed_Value)
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  [" + Test_Table_Name + " ] where " + Test_Items_Name[(int)Filed_Name] + "=@" + Test_Items_Name[(int)Filed_Name]);
            OleDbParameter[] parameters = new OleDbParameter[1];
            //参数赋值
            parameters[0] = new OleDbParameter(Test_Items_Name[(int)Filed_Name], Test_Setting_Parameters[(int)Filed_Name].FieldType);
            parameters[0].Value = Filed_Value;
            return ExecuteDataSet(strCmd_Select.ToString(), parameters);
        }



        /// <summary>
        /// 查询指定值是否存在  若存在则返回 True
        /// </summary>
        /// <param name="Filed_Name"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool Select(File_Name_e Filed_Name,string Value)
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  ["+TransFormer_Table_Name+"]");
            DataSet ds = ExecuteDataSet(strCmd_Select.ToString());
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                object temp = ds.Tables[0].Rows[i][(int)Filed_Name + 1];
                if (temp.ToString() == Value)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool Select(string companyName, string transName)
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  [" + TransFormer_Table_Name + "]");
            DataSet ds = ExecuteDataSet(strCmd_Select.ToString());
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                object temp = ds.Tables[0].Rows[i][(int)File_Name_e._1ItsUnitName + 1];
                object temp1 = ds.Tables[0].Rows[i][(int)File_Name_e._2TransFormName + 1];
                if (temp.ToString() == companyName&&temp1.ToString()==transName)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Select(Test_File_Name_e Filed_Name, string Value)
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  [" + Test_Table_Name + "]");
            DataSet ds = ExecuteDataSet(strCmd_Select.ToString());
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                object temp = ds.Tables[0].Rows[i][(int)Filed_Name];
                if (temp.ToString() == Value)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool Select(Test_File_Name_e Filed_Name,string companyName , string transName)
        {
            strCmd_Select.Clear();
            strCmd_Select.Append("select * from  [" + Test_Table_Name + "]");
            DataSet ds = ExecuteDataSet(strCmd_Select.ToString());
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                object temp = ds.Tables[0].Rows[i][(int)Test_File_Name_e._00CompanyName];
                object temp1 = ds.Tables[0].Rows[i][(int)Test_File_Name_e._0curTransformerName];
                if (temp.ToString() == companyName&&temp1.ToString() == transName)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 修改数据
       public static void Update(List<Parameters> Setting_parameters,string table_name,params int[] specified_location)
       {
           Create_CMD_Update(Setting_parameters, table_name, specified_location);
           int length = Setting_parameters.Count - specified_location.Length;
           OleDbParameter[] parameters = new OleDbParameter[length];
           int flag = 100;
           int index = 0;
           for (int i = 0; i < Setting_parameters.Count; i++)
           {
               foreach (int j in specified_location)
               {
                   if (i == j)
                   {
                       flag = i;
                       continue;
                   }
               }
               if (flag != 100 && flag == i)
               {
                   continue;
               }
               parameters[index] = new OleDbParameter(Setting_parameters[i].FieldName, Setting_parameters[i].FieldType);
               parameters[index].Value = Setting_parameters[i].Value;
               index++;
           }
           try
           {
               ExecuteNonQuery(strCmd_Update.ToString(), parameters);
           }
           catch (Exception error)
           {
               MessageBox.Show(error.Message);
           }
       }
        #endregion

       #region 删除数据
       public static void Delete_Company( string CompanyName)
       {
           try
           {
               ExecuteNonQuery("Delete FROM [" + TransFormer_Table_Name + "] where [" + Setting_Items_Name[(int)File_Name_e._1ItsUnitName] + "]='" + CompanyName + "'");
           }
           catch(Exception error)
           {
               MessageBox.Show(error.Message);
           }
       }
       public static void Delete_TransFormer(string TransFormerName)
       {
           try
           {
               ExecuteNonQuery("Delete FROM [" + Test_Table_Name + "] where [" + "变压器名称" + "]='" + TransFormerName + "'");
           }
           catch (Exception error)
           {
               MessageBox.Show(error.Message);
           }
       }
       public static void Delete_TransFormer(string CompanyName, string TransFormerName)
       {
           try
           {
               ExecuteNonQuery("Delete FROM [" + TransFormer_Table_Name + "] where [" + Setting_Items_Name[(int)File_Name_e._2TransFormName] + "]='" + TransFormerName + "'" +" and " +"["+Setting_Items_Name[(int)File_Name_e._1ItsUnitName]+"]='" + CompanyName + "'" );
               ExecuteNonQuery("Delete FROM [" + Test_Table_Name + "] where [" + "变压器名称" + "]='" + TransFormerName + "'");
           }
           catch (Exception error)
           {
               MessageBox.Show(error.Message);
           }
       }
       #endregion


        #region 储存日志名称的表
       private static void create_Sys_Log_Table(Catalog catalog)
       {
           ADODB.Connection cn = new ADODB.Connection();
           cn.Open(strConnection, null, null, -1);
           catalog.ActiveConnection = cn;
           Table table = new Table();
           table.Name = "SystemLogNameTable";
           table.Columns.Append("Name_ID", DataTypeEnum.adInteger, 9);
           table.Columns.Append("历史日志名称", DataTypeEnum.adLongVarWChar, 9);
           table.Columns["Name_ID"].ParentCatalog = catalog;
           table.Columns["Name_ID"].Properties["AutoIncrement"].Value = true;//设置自动增长
           table.Keys.Append("FirstTablePrimaryKey", KeyTypeEnum.adKeyPrimary, table.Columns["Name_ID"], null, null); //定义主键
           try
           {
               catalog.Tables.Append(table);
           }
           catch (Exception ex)
           {
               LogHelper.Log_Write(LogHelper.Log_Level_e._1_Warn, ex);
           }
           cn.Close();
       }
       static public void Sys_Log_Table_Init()
       {
           OleDbParameter[] parameters = new OleDbParameter[1];
           //参数赋值
           parameters[0] = new OleDbParameter("历史日志名称",OleDbType.LongVarWChar);
           parameters[0].Value = DateTime.Now.ToString("yy-MM-dd");
           //执行数据库操作      
           ExecuteNonQuery(strCmd_Insert.ToString(), parameters);
       }
       public static bool Sys_Log_Table_Select(string log_name)
       {
           string cmdstr= "select * from  [SystemLogNameTable]";
           for(int i=0;i<ExecuteDataSet(cmdstr).Tables[0].Rows.Count;i++)
           {
                object temp = ExecuteDataSet(cmdstr).Tables[0].Rows[i][1];
               if(temp.ToString() == log_name)
               {
                   return true;
               }
           }
           return false;
       }
       static public void Sys_Log_Table_Insert(string log_name)
       {
           string cmdstr = "insert into [SystemLogNameTable](历史日志名称) values(@历史日志名称)";
           OleDbParameter[] parameters = new OleDbParameter[1];
           //参数赋值
           parameters[0] = new OleDbParameter("历史日志名称", OleDbType.LongVarWChar);
           parameters[0].Value = log_name;
           ExecuteNonQuery(cmdstr, parameters);
       }
       #endregion 
        #region 配置项内容修改
       static public List<Parameters> Parameter_Value_Modify(List<Parameters> list_parameter, int specificed_location, object value)
        {
            foreach (var item in list_parameter)
            {
                if (item.FieldName == list_parameter[specificed_location].FieldName)
                {
                    item.Value = value;
                    return list_parameter;
                }
                else
                {
                    continue;
                }
            }
            MessageBox.Show("[字段名匹配Error]未找到相符合的字段名！请输入正确字段名！");
            return list_parameter;
        }
        #endregion


        #region ExecuteNonQuery
        /**/
        /// <summary>
        /// Execute a OleDbCommand (that returns no resultset) against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-OleDb command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>an int representing the number of rows affected by the command</returns>
        public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {

            OleDbCommand cmd = new OleDbCommand();

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        /**//// <summary>
        /// 使用默认连接
        /// </summary>
        /// <param name="cmdType">命令文本类型</param>
        /// <param name="cmdText">命令文本</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>int</returns>
        public static int ExecuteNonQuery(CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();

            using (OleDbConnection conn = new OleDbConnection(strConnection))
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }

        /**//// <summary>
        /// 使用默认连接,CommandType默认为StoredProcedure
        /// </summary>
        /// <param name="cmdText">存储过程名</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>int</returns>
        private static int ExecuteNonQuery(string cmdText, params OleDbParameter[] commandParameters)
        {

            OleDbCommand cmd = new OleDbCommand();

            using (OleDbConnection conn = new OleDbConnection(strConnection))
            {
                PrepareCommand(cmd, conn, null, CommandType.Text,cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }
        /**//// <summary>
        /// Execute a OleDbCommand (that returns no resultset) against an existing database connection 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="conn">an existing database connection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-OleDb command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>an int representing the number of rows affected by the command</returns>
        public static int ExecuteNonQuery(OleDbConnection connection, CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {

            OleDbCommand cmd = new OleDbCommand();

            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }

        /**//// <summary>
        /// Execute a OleDbCommand (that returns no resultset) using an existing OleDb Transaction 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="trans">an existing OleDb transaction</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-OleDb command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>an int representing the number of rows affected by the command</returns>
        public static int ExecuteNonQuery(OleDbTransaction trans, CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }
        public static int ExecuteNonQuery(string cmdText)
        {
            using (OleDbConnection conn = new OleDbConnection(strConnection))
            {
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.Text;
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                cmd.Connection = conn;
               return  cmd.ExecuteNonQuery();
            }
        }

        /**//// <summary>
        /// Execute a OleDbCommand that returns a resultset against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  OleDbDataReader r = ExecuteReader(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-OleDb command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>A OleDbDataReader containing the results</returns>

        #endregion

        #region ExecuteReader
        public static OleDbDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();
            OleDbConnection conn = new OleDbConnection(connectionString);

            // we use a try/catch here because if the method throws an exception we want to 
            // close the connection throw code, because no datareader will exist, hence the 
            // commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                OleDbDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        /**//// <summary>
        /// 使用默认连接
        /// </summary>
        /// <param name="cmdType">命令文本类型</param>
        /// <param name="cmdText">命令文本</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>OleDbDataReader</returns>
        public static OleDbDataReader ExecuteReader(CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();
            OleDbConnection conn = new OleDbConnection(strConnection);

            // we use a try/catch here because if the method throws an exception we want to 
            // close the connection throw code, because no datareader will exist, hence the 
            // commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                OleDbDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        /**//// <summary>
        /// 使用默认连接,CommandType默认为StoredProcedure
        /// </summary>
        /// <param name="cmdText">存储过程名</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>OleDbDataReader</returns>
        public static OleDbDataReader ExecuteReader(string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();
            OleDbConnection conn = new OleDbConnection(strConnection);

            // we use a try/catch here because if the method throws an exception we want to 
            // close the connection throw code, because no datareader will exist, hence the 
            // commandBehaviour.CloseConnection will not work
            try
            {
                PrepareCommand(cmd, conn, null, CommandType.StoredProcedure, cmdText, commandParameters);
                OleDbDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return rdr;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }    
        #endregion

        #region ExecuteScalar
        /**//// <summary>
        /// Execute a OleDbCommand that returns the first column of the first record against the database specified in the connection string 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a OleDbConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-OleDb command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }
        /**//// <summary>
        /// 使用定义好的连接字符串
        /// </summary>
        /// <param name="cmdType">命令文本类型</param>
        /// <param name="cmdText">命令文本</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>object</returns>
        public static object ExecuteScalar(CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();

            using (OleDbConnection connection = new OleDbConnection(strConnection))
            {
                PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }
        /**//// <summary>
        /// 使用定义好的连接字符串,CommandType默认为StoredProcedure
        /// </summary>
        /// <param name="cmdText">存储过程名</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>object</returns>
        public static object ExecuteScalar(string cmdText, params OleDbParameter[] commandParameters)
        {
            OleDbCommand cmd = new OleDbCommand();

            using (OleDbConnection connection = new OleDbConnection(strConnection))
            {
                PrepareCommand(cmd, connection, null, CommandType.StoredProcedure, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
        }
        /**//// <summary>
        /// Execute a OleDbCommand that returns the first column of the first record against an existing database connection 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new OleDbParameter("@prodid", 24));
        /// </remarks>
        /// <param name="conn">an existing database connection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-OleDb command</param>
        /// <param name="commandParameters">an array of OleDbParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(OleDbConnection connection, CommandType cmdType, string cmdText, params OleDbParameter[] commandParameters)
        {

            OleDbCommand cmd = new OleDbCommand();

            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }

        #endregion

        #region ExecuteDataSet
        /**//// <summary>
        /// 返加dataset
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="cmdType">命令类型，如StoredProcedure,Text</param>
        /// <param name="cmdText">the stored procedure name or T-OleDb command</param>
        /// <returns>DataSet</returns>
        public static DataSet ExecuteDataSet(string connectionString, CommandType cmdType, string cmdText)
        {
            OleDbConnection OleDbDataConn = new OleDbConnection(connectionString);
            OleDbCommand OleDbComm = new OleDbCommand(cmdText, OleDbDataConn);
            OleDbComm.CommandType = cmdType;
            OleDbDataAdapter OleDbDA = new OleDbDataAdapter(OleDbComm);
            DataSet DS = new DataSet();
            OleDbDA.Fill(DS);
            return DS;
        }

        /**//// <summary>
        /// 使用定义好的连接字符串
        /// </summary>
        /// <param name="cmdType">命令文本类型</param>
        /// <param name="cmdText">命令文本</param>
        /// <returns>DataSet</returns>
        public static DataSet ExecuteDataSet(CommandType cmdType, string cmdText)
        {
            OleDbConnection OleDbDataConn = new OleDbConnection(strConnection);
            OleDbCommand OleDbComm = new OleDbCommand(cmdText, OleDbDataConn);
            OleDbComm.CommandType = cmdType;
            OleDbDataAdapter OleDbDA = new OleDbDataAdapter(OleDbComm);
            DataSet DS = new DataSet();
            OleDbDA.Fill(DS);
            return DS;
        }
        /**//// <summary>
        /// 使用定义好的连接字符串,CommandType默认为StoredProcedure
        /// </summary>
        /// <param name="cmdText">存储过程名</param>
        /// <returns>object</returns>
        public static DataSet ExecuteDataSet(string cmdText)
        {
            OleDbConnection OleDbDataConn = new OleDbConnection(strConnection);
            OleDbCommand OleDbComm = new OleDbCommand(cmdText, OleDbDataConn);
            OleDbComm.CommandType = CommandType.Text;
            OleDbDataAdapter OleDbDA = new OleDbDataAdapter(OleDbComm);
            DataSet DS = new DataSet();
            OleDbDA.Fill(DS);
            return DS;
        }
        /**//// <summary>
        /// 返加dataset
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="cmdType">命令类型，如StoredProcedure,Text</param>
        /// <param name="cmdText">the stored procedure name or T-OleDb command</param>
        /// <param name="OleDbparams">参数集</param>
        /// <returns>DataSet</returns>
        public static DataSet ExecuteDataSet(string connectionString, CommandType cmdType, string cmdText, params OleDbParameter[] OleDbparams)
        {
            OleDbConnection OleDbDataConn = new OleDbConnection(connectionString);
            OleDbCommand OleDbComm = AddOleDbParas(OleDbparams, cmdText, cmdType, OleDbDataConn);
            OleDbDataAdapter OleDbDA = new OleDbDataAdapter(OleDbComm);
            DataSet DS = new DataSet();
            OleDbDA.Fill(DS);
            return DS;
        }

        public static DataSet ExecuteDataSet( CommandType cmdType, string cmdText, params OleDbParameter[] OleDbparams)
        {
            OleDbConnection OleDbDataConn = new OleDbConnection(strConnection);
            OleDbCommand OleDbComm = AddOleDbParas(OleDbparams, cmdText, cmdType, OleDbDataConn);
            OleDbDataAdapter OleDbDA = new OleDbDataAdapter(OleDbComm);
            DataSet DS = new DataSet();
            OleDbDA.Fill(DS);
            return DS;
        }
        /**//// <summary>
        /// 使用定义好的连接字符串,CommandType默认为StoredProcedure
        /// </summary>
        /// <param name="cmdText">存储过程名</param>
        /// <param name="commandParameters">参数集</param>
        /// <returns>DataSet</returns>
        public static DataSet ExecuteDataSet(string cmdText, params OleDbParameter[] OleDbparams)
        {
            OleDbConnection OleDbDataConn = new OleDbConnection(strConnection);
            OleDbCommand OleDbComm = AddOleDbParas(OleDbparams,cmdText,CommandType.Text, OleDbDataConn);
            OleDbDataAdapter OleDbDA = new OleDbDataAdapter(OleDbComm);
            DataSet DS = new DataSet();
            OleDbDA.Fill(DS);
            return DS;
        }
        #endregion

        #region CacheParameters
        /**//// <summary>
        /// add parameter array to the cache
        /// </summary>
        /// <param name="cacheKey">Key to the parameter cache</param>
        /// <param name="cmdParms">an array of OleDbParamters to be cached</param>
        public static void CacheParameters(string cacheKey, params OleDbParameter[] commandParameters)
        {
            parmCache[cacheKey] = commandParameters;
        }

        #endregion

        #region GetCachedParameters
        /**//// <summary>
        /// Retrieve cached parameters
        /// </summary>
        /// <param name="cacheKey">key used to lookup parameters</param>
        /// <returns>Cached OleDbParamters array</returns>
        public static OleDbParameter[] GetCachedParameters(string cacheKey)
        {
            OleDbParameter[] cachedParms = (OleDbParameter[])parmCache[cacheKey];

            if (cachedParms == null)
                return null;

            OleDbParameter[] clonedParms = new OleDbParameter[cachedParms.Length];

            for (int i = 0, j = cachedParms.Length; i < j; i++)
                clonedParms[i] = (OleDbParameter)((ICloneable)cachedParms[i]).Clone();

            return clonedParms;
        }

        #endregion

        #region PrepareCommand
        /**//// <summary>
        /// Prepare a command for execution
        /// </summary>
        /// <param name="cmd">OleDbCommand object</param>
        /// <param name="conn">OleDbConnection object</param>
        /// <param name="trans">OleDbTransaction object</param>
        /// <param name="cmdType">Cmd type e.g. stored procedure or text</param>
        /// <param name="cmdText">Command text, e.g. Select * from Products</param>
        /// <param name="cmdParms">OleDbParameters to use in the command</param>
        private static void PrepareCommand(OleDbCommand cmd, OleDbConnection conn, OleDbTransaction trans, CommandType cmdType, string cmdText, OleDbParameter[] cmdParms)
        {

            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            if (cmdParms != null)
            {
                foreach (OleDbParameter parm in cmdParms)
                    cmd.Parameters.Add(parm);
            }
        }

        #endregion

        #region AddOleDbParas
        /**//// <summary>
        /// 获得一个完整的Command
        /// </summary>
        /// <param name="OleDbParas">OleDb的参数数组</param>
        /// <param name="CommandText">命令文本</param>
        /// <param name="IsStoredProcedure">命令文本是否是存储过程</param>
        /// <param name="OleDbDataConn">数据连接</param>
        /// <returns></returns>
        private static OleDbCommand AddOleDbParas(OleDbParameter[] OleDbParas, string cmdText, CommandType cmdType, OleDbConnection OleDbDataConn)
        {
            OleDbCommand OleDbComm = new OleDbCommand(cmdText, OleDbDataConn);
            OleDbComm.CommandType = cmdType;
            if (OleDbParas != null)
            {
                foreach (OleDbParameter p in OleDbParas)
                {
                    OleDbComm.Parameters.Add(p);
                }
            }
            return OleDbComm;
        }
        #endregion
    }
}