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
        //获取河道全部控制断面水位流量关系
        public static Dictionary<AtReach, List<double[]>> Get_SectionQHrelation()
        {
            if (Item_Info.Section_QHrelation == null) Initial_ReachSectionQHrelation();

            return Item_Info.Section_QHrelation;
        }

        //获取河道指定断面水位流量关系
        public static List<double[]> Get_SectionQHrelation(AtReach atreach)
        {
            List<double[]> res = new List<double[]>();

            if (Item_Info.Section_QHrelation == null) Initial_ReachSectionQHrelation();
            Dictionary<AtReach, List<double[]>> section_qh = Item_Info.Section_QHrelation;
            for (int i = 0; i < section_qh.Count; i++)
            {
                if (atreach.reachname == section_qh.ElementAt(i).Key.reachname && atreach.chainage == section_qh.ElementAt(i).Key.chainage)
                {
                    res = section_qh.ElementAt(i).Value;
                }
            }

            return res;
        }

        //更新河道全部控制断面水位流量关系(没有的全部重新计算，有且有stcd的不更新)
        public static void Update_ReachQH_RelationTable()
        {
            //加载默认模型
            HydroModel hydromodel = HydroModel.Load(Model_Const.DEFAULT_MODELNAME);

            //获取河道全部控制断面水位流量关系
            Dictionary<AtReach, string> section_stcd = Initial_ReachSectionQHrelation();
            Dictionary<AtReach, List<double[]>> section_qhrelation = Item_Info.Section_QHrelation;

            //更新或补全水位流量关系
            for (int i = 0; i < section_qhrelation.Count; i++)
            {
                AtReach atreach = section_qhrelation.ElementAt(i).Key;
                List<double[]> qh_relation = section_qhrelation.ElementAt(i).Value;
                string stcd = section_stcd.Keys.Contains(atreach) ? section_stcd[atreach] : "";
                if (qh_relation == null || stcd == "")
                {
                    List<double[]> new_qh_relation = Xns11.Get_ReachSection_QH(hydromodel, atreach);
                    section_qhrelation[atreach] = new_qh_relation;
                }
            }

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.SECTION_QHRELATION;
            

            //更新数据库水位
            for (int i = 0; i < section_qhrelation.Count; i++)
            {
                AtReach atreach = section_qhrelation.ElementAt(i).Key;
                List<double[]> qh_relation = section_qhrelation.ElementAt(i).Value;
                string qh_str = File_Common.Serializer_Obj(qh_relation);

                //处理
                string sql = "update " + tableName + " set qh_relation = '" + qh_str
                    + "' where reach = '" + atreach.reachname + "' and chainage = " + atreach.chainage + " and model_instance = '" + Mysql_GlobalVar.now_instance + "'";

                Mysql.Execute_Command(sql);
            }
            

            Console.WriteLine("河道断面水位流量关系更新完成！");
        }

        //获取指定建筑物闸前水位和流量关系
        public static List<double[]> Get_StrQHrelation(string str_name)
        {
            if (Item_Info.Struct_QHrelation == null) Initial_Struct_QHrelation();

            List<double[]> str_qhrelation = new List<double[]>();
            if (Item_Info.Struct_QHrelation.Keys.Contains(str_name))
            {
                str_qhrelation = Item_Info.Struct_QHrelation[str_name];
            }

            return str_qhrelation;
        }

        //根据监测水位得到重点河道断面流量
        public static Dictionary<AtReach, double> Get_MainStatin_Discharge(List<Water_Condition> now_water, Dictionary<AtReach, List<double[]>> section_qhrelations)
        {
            Dictionary<AtReach, double> res = new Dictionary<AtReach, double>();

            //河道重点监测站点
            List<string> reach_station = new List<string>() { "修武", "合河(共)", "刘庄", "淇门", "新村", "五陵", "安阳", "元村","横水"};

            //各河道重点监测站点最新流量
            for (int i = 0; i < reach_station.Count; i++)
            {
                string station_name = reach_station[i];
                Water_Condition station_condition = Water_Condition.Get_WaterCondition(now_water, station_name);
                AtReach station_atreach = AtReach.Get_Atreach(station_condition.Reach, station_condition.Chainage);
                double station_q = station_condition.Discharge;

                //如果没有实测流量，则用水位根据水位-流量关系内插得到流量
                if (station_q == 0)
                {
                    station_q = Get_SectionQ_FromLevelandQH(now_water, section_qhrelations, station_condition.Stcd, station_atreach);
                }

                if(!res.Keys.Contains(station_atreach)) res.Add(station_atreach, station_q);
            }

            //给主要断面流量赋值
            Item_Info.Reach_NowDischarge = res;

            return res;
        }

        //获取主要河道额外 基流流量
        public static Dictionary<string, double> Get_MainReach_Base_Discharge()
        {
            Dictionary<string, double> res = new Dictionary<string, double>();

            //主要水文站点流量
            Dictionary<AtReach, double> main_station_q = Item_Info.Reach_NowDischarge;
            if (main_station_q == null) return null;

            //各主要站点流量 ("修武", "合河(共)", "刘庄", "淇门", "新村", "五陵", "安阳", "元村","横水")
            double xw_q = main_station_q.Values.ElementAt(0);
            double hh_q = main_station_q.Values.ElementAt(1);
            double lz_q = main_station_q.Values.ElementAt(2);
            double qm_q = main_station_q.Values.ElementAt(3);
            double xc_q = main_station_q.Values.ElementAt(4);
            double wl_q = main_station_q.Values.ElementAt(5);
            double ay_q = main_station_q.Values.ElementAt(6);
            double yc_q = main_station_q.Values.ElementAt(7);
            double hs_q = main_station_q.Values.ElementAt(8);

            //获取当前水情
            List<Water_Condition> now_water = Hd11.Read_NowWater();

            //修武以上大沙河基流,剔除洪水后
            res.Add("xw_base_in", Math.Min(xw_q, 8));

            //合河(共)站前大沙河额外基流,剔除洪水后
            res.Add("hh_base_in", Math.Min(Math.Max(hh_q - xw_q, 0.01),10));

            //共渠合河至刘庄额外基流
            double add_q = Math.Min(lz_q + qm_q - hh_q - xc_q,0.01);
            res.Add("lz_base_in", Math.Min(hh_q + add_q * 0.5, 10));

            //卫河合河至淇门额外基流
            res.Add("qm_base_in", Math.Min(qm_q + add_q * 0.5, 15));

            //淇河新村以上额外基流
            Water_Condition pstsk_water = Water_Condition.Get_WaterCondition(now_water, "盘石头水库");
            res.Add("xc_base_in", Math.Min(Math.Max(xc_q - pstsk_water.Discharge,0), 20));

            //卫河老观嘴基于五陵站额外基流
            res.Add("wl_base_in", Math.Min(Math.Max(wl_q - lz_q - qm_q, 0.01), 30));

            //安阳河安阳以上额外基流
            Water_Condition zwsk_water = Water_Condition.Get_WaterCondition(now_water, "彰武水库");
            res.Add("ay_base_in", Math.Min(Math.Max(ay_q - zwsk_water.Discharge, 0), 20));

            //卫河元村以上基于元村站额外基流
            res.Add("yc_base_in", Math.Min(Math.Max(yc_q - wl_q - ay_q, 0.01),50));

            //小南海水库入库基流
            res.Add("xnhsk_base_in", Math.Min(hs_q, 10));

            //盘石头水库入库基流
            List<Water_Info> now_waterinfo = Item_Info.Read_Now_WaterInfo();
            double sjksk_outq = now_waterinfo.FirstOrDefault(x => x.stcd == "31005580").q;
            double gssk_outq = now_waterinfo.FirstOrDefault(x => x.stcd == "31006000").q;
            double aysk_outq = now_waterinfo.FirstOrDefault(x => x.stcd == "31006050").q;
            res.Add("pstsk_base_in", Math.Min(Math.Max((sjksk_outq + gssk_outq + aysk_outq),1.74), 10));
            return res;
        }

        //根据站点监测水位内插流量
        private static double Get_SectionQ_FromLevelandQH(List<Water_Condition> now_water, Dictionary<AtReach, List<double[]>> section_qhrelations, string stcd, AtReach section)
        {
            //监测水位
            double station_level = 0;
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Stcd == stcd && now_water[i].Level != 0) station_level = now_water[i].Level;
            }

            //水位流量关系
            List<double[]> qh = new List<double[]>();
            for (int i = 0; i < section_qhrelations.Count; i++)
            {
                if (section_qhrelations.ElementAt(i).Key.reachname == section.reachname &&
                    section_qhrelations.ElementAt(i).Key.chainage == section.chainage) qh = section_qhrelations.ElementAt(i).Value;
            }

            //内插实测流量
            double station_q = Insert_q(qh, station_level);

            return station_q;
        }

        //根据站点监测水位内插流量
        private static double Get_SectionQ_FromLevelandQH(double inital_level, Dictionary<AtReach, List<double[]>> section_qhrelations, AtReach section)
        {
            //水位流量关系
            List<double[]> qh = new List<double[]>();
            for (int i = 0; i < section_qhrelations.Count; i++)
            {
                if (section_qhrelations.ElementAt(i).Key.reachname == section.reachname &&
                    section_qhrelations.ElementAt(i).Key.chainage == section.chainage) qh = section_qhrelations.ElementAt(i).Value;
            }

            //内插实测流量
            double station_q = Insert_q(qh, inital_level);

            return station_q;
        }

        //根据主要河道流量，内插这河道所有控制断面水位
        public static Dictionary<AtReach, double> Insert_SectionLevel_FromQH(Dictionary<AtReach, List<double[]>> section_qhrelations, Dictionary<AtReach, double> reach_q)
        {
            Dictionary<AtReach, double> section_level = new Dictionary<AtReach, double>();

            //分河道处理
            List<string> reach_list = new List<string>();
            for (int i = 0; i < reach_q.Count; i++)
            {
                if (!reach_list.Contains(reach_q.ElementAt(i).Key.reachname)) reach_list.Add(reach_q.ElementAt(i).Key.reachname);
            }

            Dictionary<string, Dictionary<double, double>> reach_q1 = new Dictionary<string, Dictionary<double, double>>();
            string last_reach = reach_q.ElementAt(0).Key.reachname;
            for (int i = 0; i < reach_list.Count; i++)
            {
                string reach = reach_list[i];
                Dictionary<double, double> reach_chainages = new Dictionary<double, double>();
                for (int j = 0; j < reach_q.Count; j++)
                {
                    double chainage = reach_q.ElementAt(j).Key.chainage;
                    double discharge = reach_q.ElementAt(j).Value;
                    if (reach_q.ElementAt(j).Key.reachname == reach)
                    {
                        if (!reach_chainages.Keys.Contains(chainage)) reach_chainages.Add(chainage, discharge);
                    }
                }
                reach_chainages = reach_chainages.OrderBy(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
                reach_q1.Add(reach, reach_chainages);
            }

            for (int i = 0; i < section_qhrelations.Count; i++)
            {
                AtReach at_reach = section_qhrelations.ElementAt(i).Key;
                string reachname = at_reach.reachname;

                //河道流量
                double q = 0;
                for (int j = 0; j < reach_q1.Count; j++)
                {
                    if (reach_q1.ElementAt(j).Key != reachname) continue;

                    Dictionary<double, double> chainage_discharge = reach_q1.ElementAt(j).Value;
                    if(chainage_discharge.Count == 1)
                    {
                        q = chainage_discharge.ElementAt(0).Value;
                    }
                    else
                    {
                        //根据桩号内插流量
                        q = File_Common.Insert_Zd_Value1(chainage_discharge, at_reach.chainage);
                    }
                }

                //河道水位
                List<double[]> qh = section_qhrelations.ElementAt(i).Value;
                double level = Insert_h(qh, q);
                section_level.Add(section_qhrelations.ElementAt(i).Key, level);
            }
            return section_level;
        }

        //根据流量水位关系 由流量内插水位
        public static double Insert_h(List<double[]> qh_relation, double q)
        {
            for (int i = 0; i < qh_relation.Count - 1; i++)
            {
                if (q >= qh_relation[i][0] && q < qh_relation[i + 1][0])
                {
                    double data = qh_relation[i][1] + (qh_relation[i + 1][1] - qh_relation[i][1])
                        * (q - qh_relation[i][0]) / (qh_relation[i + 1][0] - qh_relation[i][0]);
                    return data;
                }
            }
            return 0;
        }

        //根据流量水位关系 由水位内插流量
        public static double Insert_q(List<double[]> qh_relation, double h)
        {
            for (int i = 0; i < qh_relation.Count - 1; i++)
            {
                if (h >= qh_relation[i][1] && h < qh_relation[i + 1][1])
                {
                    double data = qh_relation[i][0] + (qh_relation[i + 1][0] - qh_relation[i][0])
                        * (h - qh_relation[i][1]) / (qh_relation[i + 1][1] - qh_relation[i][1]);
                    return data;
                }
            }
            return 0;
        }

        //从数据库获取水库溢洪道、泄洪洞泄流曲线(如果没有的话，则为空)
        public static Dictionary<double, double> Get_Res_Strlq_Relation(string res_name,string str_type)
        {
            Dictionary<double, double> res = new Dictionary<double, double>();

            //获取该建筑物信息
            Struct_BasePars str_info = Get_Res_YHDXHD_StrInfo(res_name, str_type);
            if (str_info == null) return res;

            //获取该建筑的泄流曲线
            List<double[]> str_qh = Item_Info.Get_StrQHrelation(str_info.str_name);
            if(str_qh.Count != 0)
            {
                res = Change_Relation_Format(str_qh);
            }

            return res;
        }

        //改关系格式
        public static Dictionary<double, double> Change_Relation_Format(List<double[]> source_list)
        {
            if (source_list.First().Length != 2) return null;
            Dictionary<double, double> res = new Dictionary<double, double>();
            for (int i = 0; i < source_list.Count; i++)
            {
                res.Add(source_list[i][1], source_list[i][0]);
            }

            return res;
        }

        //水库的水位-库容关系曲线(读取数据库)
        public static Dictionary<string, Dictionary<double, double>> Get_ResHVrelation()
        {
            Dictionary<string, Dictionary<double, double>> hvrelation = new Dictionary<string, Dictionary<double, double>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.RES_HVRELATION;
            

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string stcd = dt.Rows[i][1].ToString();
                string res_cnname = dt.Rows[i][2].ToString();
                string hv_relationstr = dt.Rows[i][3].ToString();
                List<double[]> h_v = JsonConvert.DeserializeObject<List<double[]>>(hv_relationstr);
                Dictionary<double, double> res_hv = new Dictionary<double, double>();
                for (int j = 0; j < h_v.Count; j++)
                {
                    res_hv.Add(h_v[j][0], h_v[j][1]);
                }
                hvrelation.Add(stcd, res_hv);
            }

            

            return hvrelation;
        }
    }
}