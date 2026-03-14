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
        //获取初始水情条件中需要特殊固定初始水位的 初始条件段
        public static Dictionary<AtReach, double> Get_Custom_Section_InitialLevel()
        {
            Dictionary<AtReach, double> custom_section_level = new Dictionary<AtReach, double>();
            //custom_section_level.Add(AtReach.Get_Atreach("YH", 230000), 114);
            //custom_section_level.Add(AtReach.Get_Atreach("YH", 243505), 110);
            return custom_section_level;
        }

        //获取模型实例的所有监测站点stcd
        public static List<string> Get_StationStcd_FromModelInstance(string model_instance)
        {
            List<string> res = new List<string>();  

            //从数据库读取水情监测信息
            List<Water_Condition> now_water = Hd11.Read_NowWater();

            //获取对应关系
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Model_instance.Contains(model_instance)) res.Add(now_water[i].Stcd);
            }

            return res;
        }

        //从数据库获取与南水北调总干渠交叉断面的总干渠相关数据
        public static Dictionary<string, ZGQ_SectionInfo> Get_ZGQ_SectionInfo()
        {
            Dictionary<string, ZGQ_SectionInfo> zgq_sectioninfo = new Dictionary<string, ZGQ_SectionInfo>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.NSBD_INFO_TABLENAME;
            

            //先判断数据库中是否存在
            string sql = "select * from " + tableName;
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string name = dt.Rows[i][1].ToString();
                string stcd = dt.Rows[i][2].ToString();
                string reach = dt.Rows[i][3].ToString();
                string reach_cnname = dt.Rows[i][4].ToString();
                double chainage = double.Parse(dt.Rows[i][5].ToString());
                double sj_5n_level = double.Parse(dt.Rows[i][6].ToString());
                double sj_10n_level = double.Parse(dt.Rows[i][7].ToString());
                double sj_20n_level = double.Parse(dt.Rows[i][8].ToString());
                double sj_50n_level = double.Parse(dt.Rows[i][9].ToString());
                double sj_100n_level = double.Parse(dt.Rows[i][10].ToString());
                double sj_300n_level = double.Parse(dt.Rows[i][11].ToString());
                double sj_q = double.Parse(dt.Rows[i][12].ToString());
                double jh_q = double.Parse(dt.Rows[i][13].ToString());
                string dd_level_name = dt.Rows[i][14].ToString();
                double dd_level = double.Parse(dt.Rows[i][15].ToString());
                double hp_bottom_level = double.Parse(dt.Rows[i][16].ToString());
                string other_level_name = dt.Rows[i][17].ToString();
                double other_level = other_level_name ==""?0:double.Parse(dt.Rows[i][18].ToString());
                double jd = double.Parse(dt.Rows[i][19].ToString());
                double wd = double.Parse(dt.Rows[i][20].ToString());

                AtReach atreach = AtReach.Get_Atreach(reach, chainage);
                ZGQ_SectionInfo section_info = new ZGQ_SectionInfo(atreach,sj_5n_level,sj_10n_level,
                    sj_20n_level, sj_50n_level, sj_100n_level, sj_300n_level,sj_q,jh_q, dd_level, 
                    dd_level_name, hp_bottom_level,other_level, other_level_name,jd,wd);

                zgq_sectioninfo.Add(name, section_info);
            }

            
            return zgq_sectioninfo;
        }

        //获取山洪基础信息
        public static List<SH_INFO> Get_Mountain_Flood_BaseInfo()
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODEL_SH_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //山洪基础信息
            List<SH_INFO> sh_infos = new List<SH_INFO>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string region_name = dt.Rows[i][1].ToString();
                string region_code = dt.Rows[i][2].ToString();
                string village_name = dt.Rows[i][3].ToString();
                string village_code = dt.Rows[i][4].ToString();
                float village_x = float.Parse(dt.Rows[i][5].ToString());
                float village_y = float.Parse(dt.Rows[i][6].ToString());
                float village_z = float.Parse(dt.Rows[i][7].ToString());
                string catchment_code = dt.Rows[i][9].ToString();
                string risk_level = dt.Rows[i][10].ToString();

                SH_INFO sh_info = new SH_INFO(region_name, region_code, village_name,
                    village_code, village_x, village_y, village_z, catchment_code, risk_level);
                sh_infos.Add(sh_info);
            }

            return sh_infos;
        }

        //获取工程(水库河道蓄滞洪区) 所在地市和主管单位信息
        public static List<Struct_Region_Info> Get_Struct_LocationInfo(string struct_type = "")
        {
            if (Item_Info.Struct_Location_Info == null)
            {
                Item_Info.Struct_Location_Info = Struct_Region_Info.Get_Struct_RegionInfo();
            }

            if(struct_type == "")
            {
                return Item_Info.Struct_Location_Info;
            }
            else
            {
                List<Struct_Region_Info> all_strinfo = Item_Info.Struct_Location_Info;
                List<Struct_Region_Info> type_strinfo = new List<Struct_Region_Info>();
                for (int i = 0; i < all_strinfo.Count; i++)
                {
                    if(all_strinfo[i].type == struct_type) type_strinfo.Add(all_strinfo[i]);
                }
                return type_strinfo;
            }
        }

        //获取工程(水库河道蓄滞洪区) 所在地市和主管单位信息 --根据水库和蓄滞洪区中文名称
        public static Struct_Region_Info Get_SingleStruct_LocationInfo(string cn_name)
        {
            Struct_Region_Info res = new Struct_Region_Info();

            if (Item_Info.Struct_Location_Info == null)
            {
                Item_Info.Struct_Location_Info = Struct_Region_Info.Get_Struct_RegionInfo();
            }

            List<Struct_Region_Info> all_strinfo = Item_Info.Struct_Location_Info;
            for (int i = 0; i < all_strinfo.Count; i++)
            {
                if (all_strinfo[i].cn_name == cn_name)
                {
                    res = all_strinfo[i];
                    break;
                }
            }
            return res;
        }

        //获取工程(水库河道蓄滞洪区) 所在地市和主管单位信息 --根据河道位置
        public static Struct_Region_Info Get_SingleStruct_LocationInfo(AtReach atreach)
        {
            Struct_Region_Info res = new Struct_Region_Info();

            if (Item_Info.Struct_Location_Info == null)
            {
                Item_Info.Struct_Location_Info = Struct_Region_Info.Get_Struct_RegionInfo();
            }

            List<Struct_Region_Info> all_strinfo = Item_Info.Struct_Location_Info;
            for (int i = 0; i < all_strinfo.Count; i++)
            {
                if (all_strinfo[i].name == atreach.reachname && 
                    atreach.chainage >= all_strinfo[i].start_chainage && atreach.chainage <= all_strinfo[i].end_chainage)
                {
                    res = all_strinfo[i];
                    break;
                }
            }
            return res;
        }
    }
}