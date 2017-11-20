using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace GZDL_DEV.DEL
{
   

    public static class LogHelper
    {
        #region private variable
        static string root_path = System.AppDomain.CurrentDomain.BaseDirectory;
        const string formatTimeWithMilliSecond = "yyyy/MM/dd HH:mm:ss fff";
        public static string Last_Log_Name = "";
        static string path_name = root_path  + ConfigurationManager.AppSettings.GetValues("Log_Path")[0]+"\\" + DateTime.Now.ToString("yy-MM-dd")+".txt";
        static Log_Level_e output_level = (Log_Level_e)int.Parse(ConfigurationManager.AppSettings.GetValues("Output_Level")[0]);


        //const string formatWithMethodInfo = "UI|{0} | {1} | {2}";
        //const string formatWithTwoParms = "UI|{0} | {1} ";
        //const string formatWithOneParm = "UI|{0} ";
        //const string formatErrorWithMethodInfo = "UIErr: {0} | {1} | {2}";
        //const string formatWithMethodResult = "Method:{0} --> Result:{1}";
        //const string formaWithMethodResultAndParms = "Method:{0} --> Result:{1} -- Parms:{2}";
        //static bool isLog = false;
        static StringBuilder sb = new StringBuilder();
       public enum Log_Level_e
        {
            _0_Error = 0,
            _1_Warn,
            _2_Info,
            _3_Debug
        };
        static bool save_log_txt(string path_name,string log_message)
        {
			if (!Directory.Exists(root_path + ConfigurationManager.AppSettings.GetValues("Log_Path")[0])) {
				Directory.CreateDirectory(root_path + ConfigurationManager.AppSettings.GetValues("Log_Path")[0]);
			}
			//if (!File.Exists(path_name)) {
			//	File.Create(path_name);
			//}
             using (FileStream fsWrite = new FileStream(path_name, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                byte[] buffer = new byte[1024 * 1024];
                buffer = Encoding.UTF8.GetBytes(log_message);
                fsWrite.Write(buffer, 0, buffer.Length);
                return true;
            }  
        }
        static public void Log_Write(Log_Level_e log_level, Exception ex)
        {
            string exception_message = "母鸡呀" ;
            if (ex.InnerException != null)
            {
                exception_message = ex.InnerException.ToString();
            }
            else
            {
                exception_message =  ex.Message;
            }
             #region
                sb.Clear();
                string position = Get_Fun_Info();

                switch (output_level)
                {
                    case Log_Level_e._0_Error:
                        {
                            switch (log_level)
                            {
                                case Log_Level_e._0_Error:
                                    {
                                        sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString()); break;
                                    }
                            }
                            break;
                        }
                    case Log_Level_e._1_Warn:
                        {
                            switch (log_level)
                            {
                                case Log_Level_e._0_Error:
                                    {
                                        sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString()); break;
                                    }
                                case Log_Level_e._1_Warn:
                                    {
                                        sb.Append("[Warning]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString());
                                        break;
                                    }
                            }
                            break;
                        }
                    case Log_Level_e._2_Info:
                        {
                            switch (log_level)
                            {
                                case Log_Level_e._0_Error:
                                    {
                                        sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString()); break;
                                    }
                                case Log_Level_e._1_Warn:
                                    {
                                        sb.Append("[Warning]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString());
                                        break;
                                    }
                                case Log_Level_e._2_Info:
                                    {
                                        sb.Append("[Info]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString());
                                        break;
                                    }
                            }

                            break;

                        }
                    case Log_Level_e._3_Debug:
                        {

                            switch (log_level)
                            {
                                case Log_Level_e._0_Error:
                                    {
                                        sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString()); break;
                                    }
                                case Log_Level_e._1_Warn:
                                    {
                                        sb.Append("[Warning]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString());
                                        break;
                                    }
                                case Log_Level_e._2_Info:
                                    {
                                        sb.Append("[Info]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString());
                                        break;
                                    }
                                case Log_Level_e._3_Debug:
                                    {
                                        sb.Append("[Debug]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + exception_message + "\r\n");
                                        save_log_txt(path_name, sb.ToString());
                                        break;
                                    }
                            }
                            break;
                        }
                    default: break;
                }

                #endregion

        }

        public static void Log_Write(Log_Level_e log_level,string describe)
       {
           sb.Clear();
           string position = Get_Fun_Info();

           switch (output_level)
           {
               case Log_Level_e._1_Warn:
                   {
                       switch (log_level)
                       {
                           case Log_Level_e._0_Error:
                               {
                                   sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + describe+"\r\n");
                                   save_log_txt(path_name, sb.ToString()); break;
                               }
                           case Log_Level_e._1_Warn:
                               {
                                   sb.Append("[Warning]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString());
                                   break;
                               }
                       }
                       break;
                   }
               case Log_Level_e._2_Info:
                   {
                       switch (log_level)
                       {
                           case Log_Level_e._0_Error:
                               {
                                   sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString()); break;
                               }
                           case Log_Level_e._1_Warn:
                               {
                                   sb.Append("[Warning]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString());
                                   break;
                               }
                           case Log_Level_e._2_Info:
                               {
                                   sb.Append("[Info]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString());
                                   break;
                               }
                       }

                       break;

                   }
               case Log_Level_e._3_Debug:
                   {

                       switch (log_level)
                       {
                           case Log_Level_e._0_Error:
                               {
                                   sb.Append("[Error]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t错误原因:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString()); break;
                               }
                           case Log_Level_e._1_Warn:
                               {
                                   sb.Append("[Warning]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString());
                                   break;
                               }
                           case Log_Level_e._2_Info:
                               {
                                   sb.Append("[Info]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString());
                                   break;
                               }
                           case Log_Level_e._3_Debug:
                               {
                                   sb.Append("[Debug]\t" + DateTime.Now.ToString(formatTimeWithMilliSecond) + "\r\n\t位置:" + position + "\r\n\t描述:" + describe + "\r\n");
                                   save_log_txt(path_name, sb.ToString());
                                   break;
                               }
                       }
                       break;
                   }
               default: break;
           }
       }


       static string  Get_Fun_Info()
        {
            string result = "Unknown";
            StringBuilder sbInfo = new StringBuilder();
            StackTrace trace = new StackTrace(true);

            for (int index = 0; index < trace.FrameCount; ++index)
            {
                StackFrame frame = trace.GetFrame(index);
                MethodBase method = frame.GetMethod();
                Type declaringType = method.DeclaringType;
                if (declaringType != typeof(LogHelper))
                {
                    sbInfo.AppendFormat("运行路径:[{0}]", frame.GetFileName());
                    sbInfo.AppendFormat("\r\n\t行号:[{0}] ", frame.GetFileLineNumber());
                    sbInfo.AppendFormat("\r\n\t调用函数--> [{0}.{1}]", method.DeclaringType.FullName, method.Name);
                    result = sbInfo.ToString();
                    break;
                }
            }
            return result;
        }
       
        #endregion

        #region public methods

        //#region init
        ///// <summary>
        ///// init by configuration
        ///// </summary>
        //public static void Init()
        //{
        //    FileStream stream;
        //    FileMode fileMode;
           
        //    #region define
        //    string logPathKey = "logPath", logDestinationKey = "logDestination", autoFlushKey = "autoFlush";
        //    string logPath = string.Empty, logDestination = string.Empty;
            
        //    #endregion

        //    #region get destination
        //    //no destination,return
        //    if (!ConfigurationManager.AppSettings.AllKeys.Contains(logDestinationKey)) return;
        //    logDestination = ConfigurationManager.AppSettings[logDestinationKey].Trim();

        //    int logType = 0;
        //    Int32.TryParse(logDestination, out logType);
        //    //if logType == 0 , don't record log
        //    isLog = (logType != 0);
        //    #endregion

        //    #region auto flush
        //    Trace.AutoFlush = (ConfigurationManager.AppSettings.AllKeys.Contains(autoFlushKey)
        //        && ConfigurationManager.AppSettings[autoFlushKey].Trim() == "1");
        //    #endregion

        //    #region switch log type
        //    switch (logType)
        //    {
        //        case 0:
        //            {
        //                //0 - No output
        //                Trace.Listeners.Clear();
        //                return;
        //            }
        //        case 1:
        //            {
        //                //1 - Debugger (Such as DbgView)
        //                //use default listener
        //                return;
        //            }
        //        case 2:
        //            {
        //                //2 - File (Overwrite the old file and doesn't close it until application exits)
        //                Trace.Listeners.Clear();
        //                fileMode = FileMode.Create; break;
        //            }
        //        case 3:
        //            {
        //                //3 - File (Append the log at the end of file and close it after each log output)
        //                Trace.Listeners.Clear();
        //                fileMode = FileMode.Append; break;
        //            }
        //        case 4:
        //            {
        //                //4 - Debugger&File (Append the log at the end of file and close it after each log output)
        //                fileMode = FileMode.Append; break;
        //            }

        //        default: return;
        //    }
        //    #endregion

        //    #region check path
        //    //path is null
        //    logPath = ConfigurationManager.AppSettings[logPathKey].Trim();
        //    if (string.IsNullOrEmpty(logPath)) return;

        //    //path has invalid char
        //    var pathCharArray = logPath.ToCharArray();
        //    if (pathCharArray.Any(o => Path.GetInvalidPathChars().Contains(o)))
        //        return;

        //    //FileName has invalid char
        //    //note : invalid file name chars count is 41, 
        //    //invalid path  chars count  is 36
        //    //and , the top 36 of invalid file name chars  are  same as invalid path chars
        //    //so,first check path invalid chars,second check filename,only filename
        //    var filenameCharArray = Path.GetFileName(logPath).ToCharArray();
        //    if (filenameCharArray.Any(o => Path.GetInvalidFileNameChars().Contains(o)))
        //        return;

        //    //EnvironmentVariables Path
        //    if (logPath.Contains('%'))
        //        logPath = Environment.ExpandEnvironmentVariables(logPath);

        //    //cheng relative path to absolute path.
        //    if (String.IsNullOrEmpty(Path.GetPathRoot(logPath)))
        //        logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath);
        //    #endregion

        //    #region file log
        //    //risk:directory readonly;need administrator right to createfile;and so on
        //    //use try-catch
        //    try
        //    {
        //        if (!Directory.Exists(Path.GetDirectoryName(logPath)))
        //            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

        //        stream = File.Open(logPath, fileMode, FileAccess.Write, FileShare.ReadWrite);
        //        TextWriterTraceListener text = new TextWriterTraceListener(stream);
        //        //text.TraceOutputOptions = TraceOptions.DateTime;
        //        Trace.Listeners.Add(text);
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.Write(ex);
        //    }
        //    #endregion

        //}
        //#endregion

        //#region write override

        ///// <summary>
        ///// output info with time,the time has milliseccond
        ///// example as :[UI|2011/12/20 14:57:39 630 | Test Write Method  ]
        ///// </summary>
        ///// <param name="info"></param>
        //public static void Write(string info)
        //{
        //    Trace.WriteLine(string.Format(formatWithTwoParms, DateTime.Now.ToString(formatTimeWithMilliSecond), info));
        //    //TextWriterTraceListener text = new TextWriterTraceListener(stream);
        //    ////text.TraceOutputOptions = TraceOptions.DateTime;
        //    //Trace.Listeners.Add(text);
        //}

        ///// <summary>
        ///// output info with time,the time has milliseccond,but no date.
        ///// example as :       
        ///// UI|2011/12/20 14:57:48 277 | False 
        ///// UI|2011/12/20 14:57:48 279 | 0.2256
        ///// </summary>
        ///// <param name="info"></param>
        //public static void Write(object info)
        //{
        //    if (info == null)
        //        info = "NULL";
        //    Trace.WriteLine(string.Format(formatWithTwoParms, DateTime.Now.ToString(formatTimeWithMilliSecond), info));
        //}

        ///// <summary>
        ///// output info with time,the time has milliseccond,but no date.
        ///// receive format params
        ///// example as
        ///// LogHelper.Write("current ID:{0} Name:{1}", 115001, "Jackon");
        ///// UI|2011/12/20 15:01:28 801 | current ID:115001 Name:Jackon 
        ///// </summary>
        ///// <param name="format"></param>
        ///// <param name="args"></param>
        ///// <returns></returns>
        ///// <exception >
        ///// string.format fail,only output format 
        ///// </exception>
        //public static void Write(string format, params object[] args)
        //{
        //    try
        //    {
        //        CheckNullParms(args);
        //        Write(string.Format(format, args));
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.Write(ex);
        //    }
        //    finally
        //    {

        //    }
        //}

        //#endregion

        //#region write variable
        ///// <summary>
        ///// write object name and value to log
        ///// </summary>
        ///// <param name="variableName"></param>
        ///// <param name="variableValue"></param>
        ///// <example>
        /////UI|2011/12/20 16:00:18 580 | currentTemperature = 37.5 
        /////UI|2011/12/20 16:00:18 581 | IsSupportRotate = False 
        ///// </example>
        ///// <exception >
        ///// variableName or variableValue is null,return
        ///// </exception>
        //public static void WriteVariable(string variableName, object variableValue)
        //{
        //    if (string.IsNullOrEmpty(variableName)) return;
        //    Write(string.Format("{0} = {1}", variableName, variableValue == null ? "Null" : variableValue));
        //}

        //#endregion

        //#region writh with method info
        ///// <summary>
        ///// output info with method and exec result
        ///// example as
        ///// UI|2011/12/20 15:17:04 732 | Method:DoTestIsOK --> Result:Null 
        ///// </summary>
        ///// <param name="method">method name</param>
        ///// <param name="ret">result ,1 or 0</param>
        //public static void WriteMethod(string method, object ret)
        //{
        //    Write(String.Format(formatWithMethodResult, method, ret == null ? "Null" : ret));
        //}

        ///// <summary>
        ///// output info with method \ exec result \ parms
        ///// example as
        ///// UI|2011/12/20 15:17:05 316 | Method:DoTestAdd --> Result:4 -- Parms:a:1 b:3 
        ///// </summary>
        ///// <param name="method">method name</param>
        ///// <param name="ret">result ,1 or 0</param>
        //public static void WriteMethod(string method, object ret, string format, params object[] args)
        //{
        //    CheckNullParms(args);
        //    Write(String.Format(formaWithMethodResultAndParms, method, ret == null ? "Null" : ret, string.Format(format, args)));
        //}
        //#endregion

        //#region with with stack information
        ///// <summary>
        ///// example:
        ///// UI|2011/12/20 15:31:23 220 | CSharpLogDemo.TestClass.TestDo 
        ///// </summary>
        ///// <param name="info"></param>
        //public static void WriteStack()
        //{
        //    Write(GetExecutingMethodName());
        //}

        ///// <summary>
        ///// example
        ///// UI|2011/12/20 15:31:23 954 | E:\ToshibaCommon\TosWpfCommonLib\CSharpLogDemo\MainWindow.xaml.cs(116)  --> CSharpLogDemo.TestClass.TestDoDetail 
        ///// must has pdb file,otherse ,no file info
        ///// </summary>
        ///// <param name="info">custom info</param>
        //public static void WriteStackDetail()
        //{
        //    Write(GetExecutingInfo());
        //}
        //#endregion

        //#region Write Exception
        ///// <summary>
        ///// output datetime and exception.message
        ///// UI|2011/12/20 15:39:48 088 | DoTest 
        /////System.Exception: Test Exception
        /////at CSharpLogDemo.TestClass.TestException() in E:\ToshibaCommon\TosWpfCommonLib\CSharpLogDemo\MainWindow.xaml.cs:line 153
        /////at CSharpLogDemo.MainWindow.btnException_Click(Object sender, RoutedEventArgs e) in E:\ToshibaCommon\TosWpfCommonLib\CSharpLogDemo\MainWindow.xaml.cs:line 119
        ///// </summary>
        ///// <param name="ex">current exception.</param>
        ///// <param name="caption">custom Caption</param>
        //public static void WriteException(Exception ex, string caption)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendFormat(formatWithTwoParms, DateTime.Now.ToString(formatTimeWithMilliSecond), caption);
        //    sb.AppendLine();
        //    sb.Append(ex.ToString());
        //    if (ex.InnerException != null)
        //    {
        //        sb.AppendLine("Inner Exception");
        //        sb.Append(exception_message);
        //    }
        //    Trace.WriteLine(sb.ToString());
        //}

        ///// <summary>
        ///// output datetime and exception.message
        ///// UI|2011/12/20 15:39:45 405 
        /////System.Exception: Test Exception
        /////at CSharpLogDemo.TestClass.TestException() in E:\ToshibaCommon\TosWpfCommonLib\CSharpLogDemo\MainWindow.xaml.cs:line 153
        /////at CSharpLogDemo.MainWindow.btnException_Click(Object sender, RoutedEventArgs e) in E:\ToshibaCommon\TosWpfCommonLib\CSharpLogDemo\MainWindow.xaml.cs:line 119
        ///// </summary>
        ///// <param name="ex">current exception.</param>
        ///// <param name="caption">custom Caption</param>
        //public static void WriteException(Exception ex)
        //{
        //    if (ex == null) return;
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendFormat(formatWithOneParm, DateTime.Now.ToString(formatTimeWithMilliSecond));
        //    sb.AppendLine();
        //    sb.Append(ex.ToString());
        //    if (ex.InnerException != null)
        //    {
        //        sb.AppendLine("Inner Exception");
        //        sb.Append(exception_message);
        //    }
        //    Trace.WriteLine(sb.ToString());
        //}
        //#endregion

        //#endregion

        //#region private method
        ///// <summary>
        ///// get current call method name
        ///// </summary>
        ///// <returns>FullName.Name,example as [WpfTraceSolution.MainWindow.button1_Click]</returns>
        //static string GetExecutingMethodName()
        //{
        //    if (!isLog) return string.Empty;
        //    string result = "Unknown";
        //    StackTrace trace = new StackTrace(false);

        //    for (int index = 0; index < trace.FrameCount; ++index)
        //    {
        //        StackFrame frame = trace.GetFrame(index);
        //        MethodBase method = frame.GetMethod();
        //        Type declaringType = method.DeclaringType;
        //        if (declaringType != typeof(LogHelper))
        //        {
        //            result = string.Concat(method.DeclaringType.FullName, ".", method.Name);
        //            break;
        //        }
        //    }

        //    return result;
        //}

        ///// <summary>
        ///// get Execute details ,output example as [F:\WpfTraceSolution\MainWindow.xaml.cs(79) | WpfTraceSolution.MainWindow.btnWriteWithFileInfo_Click]
        ///// </summary>
        ///// <returns></returns>
        //static string GetExecutingInfo()
        //{
        //    if (!isLog) return string.Empty;
        //    string result = "Unknown";
        //    StringBuilder sbInfo = new StringBuilder();
        //    StackTrace trace = new StackTrace(true);

        //    for (int index = 0; index < trace.FrameCount; ++index)
        //    {
        //        StackFrame frame = trace.GetFrame(index);
        //        MethodBase method = frame.GetMethod();
        //        Type declaringType = method.DeclaringType;
        //        if (declaringType != typeof(LogHelper))
        //        {
        //            sbInfo.AppendFormat("{0}", frame.GetFileName());
        //            sbInfo.AppendFormat("({0}) ", frame.GetFileLineNumber());
        //            sbInfo.AppendFormat(" --> {0}.{1}", method.DeclaringType.FullName, method.Name);
        //            result = sbInfo.ToString();
        //            break;
        //        }
        //    }

        //    return result;
        //}

        //static void CheckNullParms(object[] args)
        //{
        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        if (args[i] == null)
        //            args[i] = "Null";
        //    }
        //}
        #endregion
    }
}
