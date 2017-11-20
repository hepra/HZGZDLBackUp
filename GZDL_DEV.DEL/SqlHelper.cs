using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZDL_DEV
{
    public class SqlHelper
    {
        static string ConnectString = ConfigurationManager.ConnectionStrings["dbConnStr"].ConnectionString;
        /// <summary>
        //执行查询
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sqlcommnd)
        {
            using(SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using(SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    return cmd.ExecuteNonQuery();
                }
            }
        } 
        /// <summary>
        /// 参数筛选
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sqlcommnd, params SqlParameter[] Parameters)
        {
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    cmd.Parameters.AddRange(Parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// 执行查询
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sqlcommnd)
        {
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    return cmd.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// 参数化筛选
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sqlcommnd, params SqlParameter[] Parameters)
        {
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    cmd.Parameters.AddRange(Parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// 获取数据库内容
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <returns></returns>
        public string DataReader(string sqlcommnd,int ColumnNo)
        {
            string rt = "";
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    SqlDataReader read =   cmd.ExecuteReader();
                    while(read.Read() !=false)
                    {
                        rt +="data:"+ read.GetString(ColumnNo) ;
                    }
                }
            }
            return rt;
        }
       
        /// <summary>
        /// 参数筛选的Read
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public string DataReader(string sqlcommnd, int ColumnNo, params SqlParameter[] Parameters)
        {
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                string rt = "";
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    cmd.Parameters.AddRange(Parameters);
                    SqlDataReader read = cmd.ExecuteReader();
                    while (read.Read() != false)
                    {
                        rt += "data:" + read.GetString(ColumnNo);
                    }
                    return rt;
                }
            }
        }
        /// <summary>
        /// 获取数据库内容
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <returns></returns>
        public DataTable SqlDataAdapt(string sqlcommnd)
        {
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    DataSet ds = new DataSet();
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    sda.Fill(ds);
                    return ds.Tables[0];
                }
            }
        }
        /// <summary>
        /// 参数匹配 获取数据
        /// </summary>
        /// <param name="sqlcommnd"></param>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public DataTable SqlDataAdapt(string sqlcommnd,params SqlParameter[] Parameters)
        {
            using (SqlConnection conn = new SqlConnection(ConnectString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlcommnd;
                    cmd.Parameters.AddRange(Parameters);
                    DataSet ds = new DataSet();
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    sda.Fill(ds);
                    return ds.Tables[0];
                }
            }
        }
        /// <summary>
        /// 批量插入 table 行名列名 与数据库行名列名 相同
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="table"></param>
        /// <param name="colNames"></param>
        public  void BulkCopy(string tableName,DataTable table,params string[] colNames)
        {
            SqlBulkCopy sqlbulkcopy = new SqlBulkCopy(ConnectString);
            sqlbulkcopy.DestinationTableName = tableName;
            for(int i=0;i<colNames.Length;i++)
            {
                sqlbulkcopy.ColumnMappings.Add(colNames[i],colNames[i]);
            }
            sqlbulkcopy.WriteToServer(table);
        }
    }
}
