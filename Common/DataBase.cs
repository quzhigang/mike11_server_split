using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using Kdbndp;

namespace bjd_model.Common
{
    //数据库类(亮亮编)
    public class DataBase
    {
        private KdbndpConnection m_pCon = null;
        public DataBase(string sConnection)
        {
            //string sConnection = ConfigurationManager.ConnectionStrings["mysql"].ToString();
            if (m_pCon == null)
            {
                m_pCon = new KdbndpConnection(sConnection);
                KdbndpConnection.ClearAllPools();
                m_pCon.Open();
            }
        }


        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable query(string sql)
        {
            if (string.Equals(sql, null)) return null;
            KdbndpCommand cmd = new KdbndpCommand(sql, m_pCon);
            try
            {
                DataSet ds = new DataSet();
                KdbndpDataAdapter da = new KdbndpDataAdapter(cmd);
                da.Fill(ds);
                DataTable dt = ds.Tables[0];
                return dt;
            }
            catch (KdbndpException e)
            {
                Console.WriteLine(sql + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns></returns>
        public bool excute(string sql)
        {
            if (string.Equals(sql, null)) return false;
            KdbndpCommand cmd = new KdbndpCommand(sql, m_pCon);
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (KdbndpException e)
            {
                Console.WriteLine(sql + e.Message);
                return false;
            }
        }

    }
}