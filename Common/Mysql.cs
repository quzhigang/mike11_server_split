using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kdbndp;
using System.Data;
using bjd_model.Const_Global;
using System.Collections;
using System.Diagnostics;

namespace bjd_model.Common
{
    [Serializable]
    public class Mysql
    {
        //无参构造
        public Mysql()
        {
            this.Dbname = Mysql_GlobalVar.NOWITEM_DBNAME;
            this.Ip = Mysql_GlobalVar.now_modeldb_serip;
            this.Port = Mysql_GlobalVar.now_port;
            this.User = Mysql_GlobalVar.now_modeldb_user;
            this.Password = Mysql_GlobalVar.now_modeldb_password;
            this.Charset = Mysql_GlobalVar.now_character;
        }

        //有参构造
        public Mysql(string dbname, string ip, int port, string user, string password, string charset)
        {
            this.Dbname = dbname;
            this.Ip = ip;
            this.Port = port;
            this.User = user;
            this.Password = password;
            this.Charset = charset;
        }

        //属性
        public string Dbname
        { get; set; }
        public string Ip
        { get; set; }
        public int Port
        { get; set; }
        public string User
        { get; set; }
        public string Password
        { get; set; }
        public string Charset
        { get; set; }


        //方法
        //组合连接信息字符串
        public string Getconnectinfo()
        {
            //string dbname = "Database =" + this.Dbname;
            //string ip = "Data Source =" + this.Ip;
            //string user = "User Id=" + this.User;
            //string password = "Password =" + this.Password;
            //string p = "pooling= false";
            //string gchar = "CharSet = " + this.Charset;
            //string port1 = "port =" + this.Port.ToString();
            //return string.Join(";", dbname, ip, user, password, p, gchar, port1);

            string connect_info = $"Server={this.Ip};User Id={this.User};Password = {this.Password}; Database = {this.Dbname}; Port = {this.Port.ToString()}";
            return connect_info;
        }

        //连接数据库,返回连接对象
        public KdbndpConnection Getconnect()
        {
            //使用成员方法处理连接信息的字符串
            string connectinfo = Getconnectinfo();

            //尝试连接
            KdbndpConnection mysqlcon;
            try
            {
                mysqlcon = new KdbndpConnection(connectinfo);
            }
            catch (Exception er)  //捕获错误信息
            {
                Console.WriteLine("连接失败: {0}", er.Message);
                return null;  // 结束程序，如果这里不加return则后面的mysqlcon不可用，因为出错后没有初始化
            }

            //返回连接
            return mysqlcon;
        }



        //执行语句,包括插入，删除，更新等
        public static void Execute_Command(string comstr)
        {
            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            try
            {
                KdbndpCommand sqlcom = new KdbndpCommand(comstr, mysqlconn);
                sqlcom.ExecuteNonQuery();
            }
            catch (Exception er)
            {
                Console.WriteLine("执行错误 {0}", er.Message);
            }
            finally
            {
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
        }

        //执行语句,包括插入，删除，更新等
        public static void Execute_Command(string sql, KdbndpParameter[] para)
        {
            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            try
            {
                KdbndpCommand sqlcom = new KdbndpCommand(sql, mysqlconn);
                sqlcom.Parameters.AddRange(para);
                sqlcom.ExecuteNonQuery();
            }
            catch (Exception er)
            {
                Console.WriteLine("执行错误 {0}", er.Message);
            }
            finally
            {
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
        }

        //根据select语句获取记录和数量
        public static void Get_Rows(string select, out List<List<Object>> rows)
        {
            rows = new List<List<Object>>();

            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            KdbndpCommand sqlcom = new KdbndpCommand(select, mysqlconn);
            KdbndpDataReader sqlreader = sqlcom.ExecuteReader();

            try
            {
                int n = 0;
                while (sqlreader.Read())
                {
                    if (sqlreader.HasRows)  //HasRows属性用于判断是否有数据
                    {
                        rows.Add(new List<Object>());         //增加新的元素，每一个元素都是一个集合,相当于一个架子
                        for (int i = 0; i < sqlreader.FieldCount; i++)
                        {
                            rows[n].Add(sqlreader.GetValue(i));  //对指定集合元素增加该元素的元素,相当于往指定架子上放书
                        }
                        n++;
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("读取数据出错，{0}", error.Message);
            }
            finally
            {
                sqlreader.Close();  //记得关闭用于读取的对象
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
        }

        //根据select语句获取字段名和类型
        public static void Get_Fields(string selectstr, out List<string> fieldname, out List<Object> fieldtype)
        {
            fieldname = new List<string>();
            fieldtype = new List<Object>();

            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            KdbndpCommand sqlcom = new KdbndpCommand(selectstr, mysqlconn);
            KdbndpDataReader sqlreader = sqlcom.ExecuteReader();

            try
            {
                for (int i = 0; i < sqlreader.FieldCount; i++)
                {
                    fieldname.Add(sqlreader.GetName(i));
                    fieldtype.Add(sqlreader.GetFieldType(i));
                }

            }
            catch (Exception er)
            {
                Console.WriteLine("读取错误{0}", er);
            }
            finally
            {
                sqlreader.Close();    //记得关闭用于读取的对象
                Con_Pool.getPool().closeConnect(mysqlconn);
            }

        }

        //根据select语句获取字段名
        public static List<string> Get_FieldsName(string selectstr)
        {
            List<string> fieldname = new List<string>();

            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            KdbndpCommand sqlcom = new KdbndpCommand(selectstr, mysqlconn);
            KdbndpDataReader sqlreader = sqlcom.ExecuteReader();

            try
            {
                for (int i = 0; i < sqlreader.FieldCount; i++)
                {
                    fieldname.Add(sqlreader.GetName(i));
                }

            }
            catch (Exception er)
            {
                Console.WriteLine("读取错误{0}", er);
            }
            finally
            {
                sqlreader.Close();    //记得关闭用于读取的对象
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
            return fieldname;
        }

        //根据select语句获取字段数量
        public static int Get_FieldsCount(string selectstr)
        {
            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            KdbndpCommand sqlcom = new KdbndpCommand(selectstr, mysqlconn);
            KdbndpDataReader sqlreader = sqlcom.ExecuteReader();

            int FieldsCount = 0;
            try
            {
                FieldsCount = sqlreader.FieldCount;

            }
            catch (Exception er)
            {
                Console.WriteLine("读取错误{0}", er);
            }
            finally
            {
                sqlreader.Close();    //记得关闭用于读取的对象
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
            return FieldsCount;
        }

        //根据select语句获取字段数量
        public static byte[] Get_Blob(string sql)
        {
            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            KdbndpCommand sqlcom = new KdbndpCommand(sql, mysqlconn);
            KdbndpDataReader sqlreader = sqlcom.ExecuteReader();

            try
            {
                if (sqlreader.Read())
                {
                    byte[] blob = new byte[sqlreader.GetBytes(0, 0, null, 0, int.MaxValue)];
                    sqlreader.GetBytes(0, 0, blob, 0, blob.Length);
                    return blob;
                }
            }
            catch (Exception er)
            {
                Console.WriteLine("读取错误{0}", er);
            }
            finally
            {
                sqlreader.Close();    //记得关闭用于读取的对象
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
            return null;
        }

        //组建插入记录sql语句
        public static string sql_insert(string tablename, object[] value)
        {
            //组建sql语句
            StringBuilder strbuilder = new StringBuilder();
            strbuilder.Append("insert into ");
            strbuilder.Append(tablename);
            strbuilder.Append(" values (");

            for (int i = 0; i < value.Length; i++)
            {
                Type type = value[i].GetType();
                if (type == typeof(string) || type == typeof(char) || type == typeof(DateTime))
                {
                    strbuilder.Append("'");
                    strbuilder.Append(value[i]);
                    strbuilder.Append("'");
                }
                else
                {
                    strbuilder.Append(value[i]);
                }

                if (i != value.Length - 1)
                {
                    strbuilder.Append(",");
                }
            }

            strbuilder.Append(")");
            return (strbuilder.ToString());
        }

        //组建插入记录sql语句without id,且ID为第一个字段
        public static string sql_insert(string tablename, object[] value, string fieldname_withoutid)
        {
            //组建sql语句
            StringBuilder strbuilder = new StringBuilder();
            strbuilder.Append("insert into ");
            strbuilder.Append(tablename);

            strbuilder.Append(" (" + fieldname_withoutid);

            strbuilder.Append(") values (");

            for (int i = 0; i < value.Length; i++)
            {
                Type type = value[i].GetType();
                if (type == typeof(string) || type == typeof(char) || type == typeof(DateTime))
                {
                    strbuilder.Append("'");
                    strbuilder.Append(value[i]);
                    strbuilder.Append("'");
                }
                else
                {
                    strbuilder.Append(value[i]);
                }

                if (i != value.Length - 1)
                {
                    strbuilder.Append(",");
                }
            }

            strbuilder.Append(")");
            return (strbuilder.ToString());
        }

        // 查询数据,返回格式为表(DataTable)
        public static DataTable query(string sql)
        {
            if (string.Equals(sql, null)) return null;

            KdbndpConnection mysqlconn = Con_Pool.getPool().getConnect();
            KdbndpCommand cmd = new KdbndpCommand(sql, mysqlconn);

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
            finally
            {
                Con_Pool.getPool().closeConnect(mysqlconn);
            }
        }


        //常用where语句
        public static string Get_WhereStr(string plan_code = "")
        {
            string username = Mysql_GlobalVar.now_modeldb_user;
            string model_instance = Mysql_GlobalVar.now_instance;

            //先判断和删除旧的信息
            string where_sql1 = " where user = '" + username + "' and model_instance = '" + model_instance + "'";
            string where_sql2 = " where user = '" + username + "' and model_instance = '" + model_instance + "' and plan_code = '" + plan_code + "'";
            string where_sql = plan_code == "" ? where_sql1 : where_sql2;
            return where_sql;
        }

        //清空表
        public static void Clear_Table(string table)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();

            //先判断该模型在数据库中是否存在
            string sql = "TRUNCATE TABLE " + table;
            Execute_Command(sql);

            sw.Stop();
            Console.WriteLine("清除表{0} 花费:{1}s ", table, sw.Elapsed.ToString());
        }

    }


    //数据库连接池
    public class Con_Pool
    {
        private static Con_Pool cpool = null;//池管理对象
        private static Object objlock = typeof(Con_Pool);//池管理对象实例
        private int size = 5;//池中连接数
        private int useCount = 0;//已经使用的连接数
        private ArrayList pool = null;//连接保存的集合
        private String ConnectionStr = "";//连接字符串

        public Con_Pool()
        {
            //数据库连接字符串
            this.ConnectionStr = new Mysql().Getconnectinfo();

            //创建可用连接的集合
            this.pool = new ArrayList();
        }

        // 创建获取连接池对象
        public static Con_Pool getPool()
        {
            lock (objlock)
            {
                if (cpool == null)
                {
                    cpool = new Con_Pool();
                }
                return cpool;
            }
        }

        // 获取池中的连接
        public KdbndpConnection getConnect()
        {
            lock (pool)
            {
                KdbndpConnection tmp = null;
                //可用连接数量大于0
                if (pool.Count > 0)
                {
                    //取第一个可用连接
                    tmp = (KdbndpConnection)pool[0];
                    //在可用连接中移除此链接
                    pool.RemoveAt(0);
                    //不成功
                    if (!isUserful(tmp))
                    {
                        //可用的连接数据已去掉一个
                        useCount--;
                        tmp = getConnect();
                    }
                }
                else
                {
                    //可使用的连接小于连接数量
                    if (useCount <= size)
                    {
                        try
                        {
                            //创建连接
                            tmp = CreateConnect(tmp);
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }

                //连接为null
                if (tmp == null)
                {
                    //达到最大连接数递归调用获取连接否则创建新连接
                    if (useCount <= size)
                    {
                        tmp = getConnect();
                    }
                    else
                    {
                        tmp = CreateConnect(tmp);
                    }
                }
                return tmp;
            }
        }

        // 创建连接
        private KdbndpConnection CreateConnect(KdbndpConnection tmp)
        {
            try
            {
                //创建连接
                KdbndpConnection conn = new KdbndpConnection(ConnectionStr);
                conn.Open();
                //可用的连接数加上一个
                useCount++;
                tmp = conn;
                return tmp;
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                throw;
            }
        }

        // 关闭连接,加连接回到池中
        public void closeConnect(KdbndpConnection con)
        {
            lock (pool)
            {
                if (con != null)
                {
                    //将连接添加在连接池中
                    pool.Add(con);
                }
            }
        }

        // 目的保证所创连接成功,测试池中连接
        private bool isUserful(KdbndpConnection con)
        {
            //主要用于不同用户
            bool result = true;
            if (con != null)
            {
                string sql = "select 1";//随便执行对数据库操作
                KdbndpCommand cmd = new KdbndpCommand(sql, con);
                try
                {
                    cmd.ExecuteScalar().ToString();
                }
                catch
                {
                    result = false;
                }

            }
            return result;
        }

    }

}