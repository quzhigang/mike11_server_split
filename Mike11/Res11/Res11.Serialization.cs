using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;
using System.Runtime.Serialization;
using System.IO;
using DHI.Mike1D.CrossSectionModule;
using System.Threading;
using System.Web.Script.Serialization;
using Kdbndp;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;

using bjd_model;
using System.Diagnostics;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json.Linq;


namespace bjd_model.Mike11
{
    public partial class Res11
    {
        #region  ***************结果序列化**************
        private static List<Dictionary<int, AtReach>> Relation_Deserialize(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            List<Dictionary<int, AtReach>> res_dic = binFormat.Deserialize(ms) as List<Dictionary<int, AtReach>>;

            ms.Close();
            ms.Dispose();

            return res_dic;
        }

        private static Dictionary<string, Reservoir_FloodRes> Res11_Deserialize1(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            Dictionary<string, Reservoir_FloodRes> res_dic = binFormat.Deserialize(ms) as Dictionary<string, Reservoir_FloodRes>;

            ms.Close();
            ms.Dispose();

            return res_dic;
        }


        private static Dictionary<string, ReachSection_FloodRes> Res11_Deserialize0(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            Dictionary<string, ReachSection_FloodRes> res_dic = binFormat.Deserialize(ms) as Dictionary<string, ReachSection_FloodRes>;

            ms.Close();
            ms.Dispose();

            return res_dic;
        }

        private static Dictionary<string, Xzhq_FloodRes> Res11_Deserialize2(byte[] blob)
        {
            if (blob.Length == 0) return new Dictionary<string, Xzhq_FloodRes>();

            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            Dictionary<string, Xzhq_FloodRes> res_dic = binFormat.Deserialize(ms) as Dictionary<string, Xzhq_FloodRes>;

            ms.Close();
            ms.Dispose();

            return res_dic;
        }

        private static string Res11_Deserialize33(byte[] blob)
        {
            if (blob.Length == 0) return "";

            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            string res = binFormat.Deserialize(ms) as string;

            ms.Close();
            ms.Dispose();

            return res;
        }

        private static Dictionary<string, object> Res11_Deserialize44(byte[] blob)
        {
            Dictionary<string, object> res_dic = new Dictionary<string, object>();
            if (blob.Length == 0) return res_dic;

           //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            res_dic = binFormat.Deserialize(ms) as Dictionary<string, object>;

            ms.Close();
            ms.Dispose();

            return res_dic;
        }


        private static Dictionary<string, Dictionary<DateTime, List<float>>> Res11_Deserialize22(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            Dictionary<string, Dictionary<DateTime, List<float>>> res_dic = binFormat.Deserialize(ms) as Dictionary<string, Dictionary<DateTime, List<float>>>;

            ms.Close();
            ms.Dispose();

            return res_dic;
        }

        private static Dictionary<string, List<object[]>> Res11_Deserialize3(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<string, List<object[]>> res_dic = binFormat.Deserialize(ms) as Dictionary<string, List<object[]>>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        private static Dictionary<DateTime, string> Res11_Deserialize4(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<DateTime, string> res_dic = binFormat.Deserialize(ms) as Dictionary<DateTime, string>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }


        private static Dictionary<string, List<float>> Res11_Deserialize5(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<string, List<float>> res_dic = binFormat.Deserialize(ms) as Dictionary<string, List<float>>;
            return res_dic;
        }

        private static Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> Res11_Deserialize6(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> res_dic = binFormat.Deserialize(ms) as Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        private static Dictionary<DateTime, Res11AD_GisRes> Res11_Deserialize7(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<DateTime, Res11AD_GisRes> res_dic = binFormat.Deserialize(ms) as Dictionary<DateTime, Res11AD_GisRes>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        private static Dictionary<DateTime, List<Res11HD_GisRes>> Res11_Deserialize8(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<DateTime, List<Res11HD_GisRes>> res_dic = binFormat.Deserialize(ms) as Dictionary<DateTime, List<Res11HD_GisRes>>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        private static GeoJson_Polygon Res11_Deserialize9(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            bool isno = binFormat.Deserialize(ms) is GeoJson_Polygon ? true:false;
            GeoJson_Polygon res_dic = binFormat.Deserialize(ms) as GeoJson_Polygon;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        private static Dictionary<DateTime, GeoJson_Point> Res11_Deserialize10(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<DateTime, GeoJson_Point> res_dic = binFormat.Deserialize(ms) as Dictionary<DateTime, GeoJson_Point>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        private static Dictionary<DateTime, GeoJson_Polygon> Res11_Deserialize11(byte[] blob)
        {
            //创建内存流对象
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);

            Dictionary<DateTime, GeoJson_Polygon> res_dic = binFormat.Deserialize(ms) as Dictionary<DateTime, GeoJson_Polygon>;

            ms.Close();
            ms.Dispose();
            return res_dic;
        }

        // 读取txt文件并存入mysql数据库
        public static void Writejgtxt_ToMysql(HydroModel hydromodel)
        {
            /*
            string sourcefilename = hydromodel.Modelfiles.Hdtxt_filename;

            Mysql mysql = new Mysql(Mysql_GlobalVar.NOWITEM_DBNAME, Mysql_GlobalVar.now_modeldb_serip,
                Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string tablename = Mysql_GlobalVar.MIKE11RES_TABLENAME;

            
            
            string[] str_row = File.ReadAllLines(sourcefilename);  //读取txt文件
            string[] str_ZH = str_row[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);   //对桩号所在行进行字符串分隔
            string[] str_reachname = str_row[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 8; i < str_row.Length - 6; i++)  //循环每一行数据
            {
                string[] str_column = str_row[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);   //用' '分隔每一行的值 
                string time = string.Join(" ", str_column[0], str_column[1]);  //连接日期和具体时间，如：2020-07-10 13:00:00
                int column_WaterLevel;  //声明水位所在列数
                int column_Discharge;   //声明流量所在列数
                for (int j = 1; j < str_ZH.Length; j++)  //循环桩号
                {
                    column_WaterLevel = j;   //水位所在列数
                    for (int l = j + 1; l < str_ZH.Length; l++)  //循环“j”后面的桩号，若有相同的桩号，则为流量序列
                    {
                        if (str_ZH[l] == str_ZH[j])
                        {
                            column_Discharge = l;   //流量所在列数
                            string sql = "insert into " + tablename + " (reach_name,chainage,datetime,waterlevel,discharge) values(?reach_name,?chainage,?datetime,?waterlevel,?discharge)";  //存入数据库SQL语句
                            //定义参数
                            KdbndpParameter[] mysqlpara = { 
                                                           new KdbndpParameter(":reach_name", str_reachname[j+1]) ,
                                                           new KdbndpParameter(":chainage", Math.Round(Convert.ToDouble(str_ZH[j]), 3)) ,
                                                           new KdbndpParameter(":datetime", time), 
                                                           new KdbndpParameter(":waterlevel", Math.Round(Convert.ToDouble(str_column[column_WaterLevel + 1]),2)), 
                                                           new KdbndpParameter(":discharge", Math.Round(Convert.ToDouble(str_column[column_Discharge + 1]),1))
                                                           };

                            using (KdbndpCommand mysqlcmd = new KdbndpCommand(sql, connect))
                            {

                                mysqlcmd.Parameters.AddRange(mysqlpara);
                                mysqlcmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine("存储完成！");
           */
        }

        public static void Xmlreader()
        {
            //FilePath filepath = new FilePath();
            //filepath.Path ="d:\\simulateVolumeBalance.HTML";
            //ResultDataXmlBridge bri = new ResultDataXmlBridge ();
            //bri.FilePath = filepath;
        }
        #endregion ***************************************************************
    }
}
