using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace GZDL_DEV.DEL
{
   static public class FileHelper
    {
        /// <summary>
        /// 读取文件 以Show OpenFileDialog 方式
        /// </summary>
        /// <returns></returns>
       static public byte[] OpenFile()
        {
            OpenFileDialog openData = new OpenFileDialog();
            openData.Filter = "所有文件|*";
            openData.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            openData.ShowDialog();
            if (openData.FileName != "")
            {
                using (FileStream fsRead = new FileStream(openData.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fsRead.Seek(0, SeekOrigin.End);
                    fsRead.Seek(0, SeekOrigin.Begin);
                    byte[] buffer = new byte[len];
                    int count = fsRead.Read(buffer, 0, buffer.Length);
                    return buffer;
                }      
            }
            return BitConverter.GetBytes(0);    
        }
       static public byte[] OpenFile_Dialog(string initial_directory)
       {
          
           OpenFileDialog openData = new OpenFileDialog();
           openData.Filter = "所有文件|*";
           openData.InitialDirectory = initial_directory;
           openData.ShowDialog();
           if (openData.FileName != "")
           {
               using (FileStream fsRead = new FileStream(openData.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
               {
                  long len = fsRead.Seek(0, SeekOrigin.End);
                  fsRead.Seek(0, SeekOrigin.Begin);
                   byte[] buffer = new byte[len];
                   int count = fsRead.Read(buffer, 0, buffer.Length);
                   return buffer;
               }
           }
           else
           {
               return BitConverter.GetBytes(0);
           }
       }
       static public string OpenDirectory()
       {
           FolderBrowserDialog dialog = new FolderBrowserDialog();
           dialog.RootFolder = Environment.SpecialFolder.MyComputer;
           dialog.Description = "请选择[公司名称]作为根目录";
           dialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory +"TestData";
           dialog.ShowNewFolderButton = false;
 
           
           if (dialog.ShowDialog() == DialogResult.OK)
           {
               string foldPath = dialog.SelectedPath;
               return foldPath;
           }
           return "";
       }
       static public byte[] OpenFile(string file_path)
       {
           
           using (FileStream fsRead = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
           {
               try
               {
                   long len = fsRead.Seek(0,SeekOrigin.End);
                   fsRead.Seek(0, SeekOrigin.Begin);
                   byte[] buffer = new byte[len];
                   int count = fsRead.Read(buffer, 0, buffer.Length);
                   return buffer;
               }
               catch (Exception e)
               {
                   MessageBox.Show(e.Message);
               }
               finally
               {
                   fsRead.Close();
               }
           }
           return null;
       }
       static public byte[] OpenFile(string file_path, int buffer_size,int offset)
        {
            byte[] buffer = new byte[buffer_size];
           using( FileStream fsRead = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
           {
               try
               {
                   fsRead.Seek(offset, SeekOrigin.Begin);
                   int count = fsRead.Read(buffer,0, buffer.Length);
               }
               catch(Exception e)
               {
                   MessageBox.Show(e.Message);
               }
               finally
               {
                   fsRead.Close();
               }
           }
            return buffer;
        }
       static public long getLength(string file_path)
       {
           using (FileStream fsRead = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
           {
               try
               {
                   long len = fsRead.Seek(0, SeekOrigin.End);
                   fsRead.Seek(0, SeekOrigin.Begin);
                   return len;
               }
               catch (Exception e)
               {
                   MessageBox.Show(e.Message);
               }
               finally
               {
                   fsRead.Close();
               }
           }
           return 0;
       }
        /// <summary>
        /// Show SaveFileDialog 方式
        /// </summary>
        /// <param name="Event"></param>
       static public void SaveFile(CancelEventHandler Event)
        {
            SaveFileDialog openfile = new SaveFileDialog();
            openfile.Title = "选择保存的文件路径";
            string buffer_path = System.AppDomain.CurrentDomain.BaseDirectory;
            openfile.InitialDirectory = buffer_path;
            openfile.Filter = "Excel文件|*.xlsx |TXT文件|*.txt";
              //添加保存按钮触发事件
            openfile.FileOk += Event;

            openfile.ShowDialog(); 
        }
       static public void SaveFile(string file_name, FileMode mode, FileAccess access, string data, int size)
          {
              using (FileStream fsWrite = new FileStream(file_name, mode, access, FileShare.ReadWrite))
             {
                 byte[] buffer = new byte[size+10];
                 buffer = Encoding.UTF8.GetBytes(data + "\r\n");
                 fsWrite.Write(buffer, 0, buffer.Length);
             }
          }
       static public void SaveFile_Append(string file_name, string data, int size)
        {
            using (FileStream fsWrite = new FileStream(file_name, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                byte[] buffer = new byte[size];
                buffer = Encoding.UTF8.GetBytes(data );
                fsWrite.Write(buffer, 0, buffer.Length);
            }
        }

       static public void SaveFile_Append(string file_name, byte[] data)
       {
           using (FileStream fsWrite = new FileStream(file_name, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
           {
               try
               {
                   fsWrite.Write(data, 0, data.Length);
               }
               catch(Exception e)
               {
                   MessageBox.Show(e.Message);
               }
               finally
               {
                   fsWrite.Close();
               }
           }
       }
       static public void SaveFile_Create(string file_name, string data, int size)
        {
            using (FileStream fsWrite = new FileStream(file_name, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[size + 100];
                buffer = Encoding.UTF8.GetBytes(data);
                fsWrite.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
