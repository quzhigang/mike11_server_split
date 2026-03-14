using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike21;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kdbndp;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace bjd_model.Mike11
{
    public partial class Item_Info
    {        
        //获取蓄滞洪区基本信息
        public static List<Xzhq_Info> Get_Xzhq_BaseInfo()
        {
            List<Xzhq_Info> Xzhq_Info = new List<Xzhq_Info>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.XZHQ_BASEINFO;

            //先判断数据库中是否存在
            string sqlstr = $"select name,code,des_fl_sta,pcode from {tableName} order by sort_num" ;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string cnname = dt.Rows[i][0].ToString();
                string stcd = dt.Rows[i][1].ToString();
                string design_level = dt.Rows[i][2].ToString();
                string father = dt.Rows[i][3].ToString();

                Xzhq_Info xzhq = new Xzhq_Info();
                xzhq.Stcd = stcd;
                xzhq.Name = cnname;
                xzhq.Father = father;
                xzhq.Design_Level = design_level;
                Xzhq_Info.Add(xzhq);
            }
            return Xzhq_Info;
        }

        //读取蓄滞洪区的水位-库容关系和水位-面积关系曲线(读取数据库)
        public static void Modify_Xzhq_Relation(ref List<Xzhq_Info> xzhq_Info)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.RES_HVRELATION;
            

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string stcd = dt.Rows[i][1].ToString();
                string res_cnname = dt.Rows[i][2].ToString();
                string hv_relationstr = dt.Rows[i][3].ToString();
                string ha_relationstr = dt.Rows[i][4].ToString();

                //更新蓄滞洪区(子类)水位-库容曲线和水位-面积曲线
                for (int j = 0; j < xzhq_Info.Count; j++)
                {
                    if(xzhq_Info[j].Stcd == stcd)
                    {
                        List<double[]> h_v = JsonConvert.DeserializeObject<List<double[]>>(hv_relationstr);
                        List<double[]> h_a = JsonConvert.DeserializeObject<List<double[]>>(ha_relationstr);

                        //水位-库容关系曲线
                        Dictionary<double, double> hv_list = new Dictionary<double, double>();
                        for (int k = 0; k < h_v.Count; k++)
                        {
                            hv_list.Add(h_v[k][0], h_v[k][1]);
                        }

                        //水位-面积关系曲线
                        Dictionary<double, double> ha_list = new Dictionary<double, double>();
                        for (int k = 0; k < h_a.Count; k++)
                        {
                            ha_list.Add(h_a[k][0], h_a[k][1]);
                        }

                        xzhq_Info[j].Level_Volume = hv_list;
                        xzhq_Info[j].Level_Area = ha_list;
                        if (hv_list.Count != 0) xzhq_Info[j].Xzhq_IsFlood_Level = hv_list.ElementAt(0).Key + 0.2;
                        break;
                    }
                }

            }
            
        }

        //获取所有蓄滞洪区区基本信息
        public static List<Xzhq_Info> Get_XzhqInfo()
        {
            if (Item_Info.Xzhq_Info == null) Initial_Xzhq_Info();
            List<Xzhq_Info> Fhblq_Info = Item_Info.Xzhq_Info;
            return Fhblq_Info;
        }

        //一维模型三维面要素中的判断显示要素 及与蓄滞洪区对应关系 
        public static Dictionary<int, string> Get_Mike11_TxPolygonFeatures()
        {
            Dictionary<int, string> polygon_fid = new Dictionary<int, string>();
            polygon_fid.Add(58, "");
            polygon_fid.Add(59, "LXP_UP_ZHQ");
            polygon_fid.Add(60, "LXP_DOWN_ZHQ");
            polygon_fid.Add(61, "GQX_UP_ZHQ");
            polygon_fid.Add(62, "GQX_DOWN_ZHQ");
            polygon_fid.Add(63, "BSP_ZHQ");
            polygon_fid.Add(64, "CHQ_ZHQ");
            polygon_fid.Add(65, "LWP_ZHQ");
            polygon_fid.Add(66, "XTP_ZHQ");
            polygon_fid.Add(67, "RGP_ZHQ");
            polygon_fid.Add(68, "GRP1_ZHQ");
            polygon_fid.Add(69, "GRP2_ZHQ");
            for (int i = 70; i < 84; i++){ polygon_fid.Add(i, ""); }
            polygon_fid.Add(84, "CJQ_ZHQ");
            polygon_fid.Add(85, "");
            polygon_fid.Add(86, "");
            polygon_fid.Add(87, "LXP_UP_ZHQ");
            polygon_fid.Add(88, "LXP_DOWN_ZHQ");
            polygon_fid.Add(89, "LXP_DOWN_ZHQ");
            return polygon_fid;
        }
    }
}