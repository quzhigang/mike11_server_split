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
        #region **********************生成风险信息*****************
        //获取山洪灾害预警信息
        public static Warning_Info Get_Sh_Warninginfo(string plan_code, Dictionary<string, ReachSection_FloodRes> reach_result)
        {
            //初始化预警信息
            string warning_name = "山洪预警";
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "卫共流域无山洪灾害预警";

            //遍历各山洪风险
            List<Warning_Info> sh_warning_list = new List<Warning_Info>();


            //生成对象
            Warning_Info sh_warninginfo = new Warning_Info(warning_name, warning_level, warning_desc, sh_warning_list);

            return sh_warninginfo;
        }

        //获取南水北调预警信息
        public static Warning_Info Get_Nsbd_Warninginfo(string plan_code, Dictionary<string, object> reach_risk)
        {
            //初始化预警信息
            string warning_name = "南水北调";
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "南水北调各交叉河道无洪水风险";

            //总干渠风险
            Dictionary<string, Dictionary<string, string>> zgq_risk = reach_risk["总干渠风险"] as Dictionary<string, Dictionary<string, string>>;

            //遍历各总干渠风险
            List<Warning_Info> nsbd_warning_list = new List<Warning_Info>();
            int nsbd_count = 0; string nsbd_reach_name = "";
            for (int i = 0; i < zgq_risk.Count; i++)
            {
                string reach_name = zgq_risk.ElementAt(i).Key;

                //获取南水北调风险结果
                Dictionary<string, string> risk_res = zgq_risk.ElementAt(i).Value;
                if (risk_res == null) continue;

                //获取单个交叉断面的预警信息
                string risk_level = risk_res["风险等级"];
                Warning_Level level = Warning_Level.blue_warining;
                if (risk_level.Contains("Ⅰ") || risk_level.Contains("Ⅱ"))
                {
                    level = Warning_Level.red_warining;
                }
                else if (risk_level.Contains("Ⅲ"))
                {
                    level = Warning_Level.yellow_warining;
                }
                if (level == Warning_Level.blue_warining) continue;

                string md_risk = risk_res["漫堤风险"];
                string cs_risk = risk_res["冲刷风险"];

                string warning = "";
                if (md_risk.Contains("不") && cs_risk.Contains("不"))
                {
                    warning = $"{reach_name}预报不存在漫堤和冲刷风险";
                }
                else if (md_risk.Contains("不"))
                {
                    warning = $"{reach_name}预报存在冲刷风险";
                }
                else
                {
                    warning = $"{reach_name}预报存在漫堤风险";
                }
                Warning_Info reach_warning = new Warning_Info(reach_name, level, warning);

                //整体预警级别根据各对象最高预警级别
                if ((int)reach_warning.Level < (int)warning_level) warning_level = reach_warning.Level;

                //各预警级别数量
                if (warning_level == Warning_Level.red_warining)
                {
                    nsbd_reach_name = reach_name; nsbd_count++;
                }

                nsbd_warning_list.Add(reach_warning);
            }

            //组织风险预警概述文字
            if (warning_level != Warning_Level.blue_warining)
            {
                warning_desc = nsbd_count == 0 ? "南水北调各交叉河道无洪水风险" : $"{nsbd_reach_name}等{nsbd_count}个交叉断面预报存在洪水风险";
            }

            //生成对象
            Warning_Info nsbd_warninginfo = new Warning_Info(warning_name, warning_level, warning_desc, nsbd_warning_list);

            return nsbd_warninginfo;
        }

        //获取蓄滞洪区预警信息
        public static Warning_Info Get_Xzhq_Warninginfo(string plan_code, Dictionary<string, Xzhq_FloodRes> xzhq_result)
        {
            //初始化预警信息
            string warning_name = "进洪预警";
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "各蓄滞洪区预报无进洪风险";

            //遍历各蓄滞洪区
            List<Warning_Info> xzhq_warning_list = new List<Warning_Info>();
            int xzhq_on_count = 0; string xzhq_on_name = "";
            for (int i = 0; i < xzhq_result.Count; i++)
            {
                string xzhq_name = xzhq_result.ElementAt(i).Key;

                //获取蓄滞洪区结果
                Xzhq_FloodRes xzhq_res = xzhq_result.ElementAt(i).Value;
                if (xzhq_res == null ) continue;
                if (xzhq_res.Xzhq_State == "未启用") continue;

                //获取单个蓄滞洪区的预警信息
                Warning_Level level = Warning_Level.red_warining;
                DateTime start_time = SimulateTime.StrToTime(xzhq_res.Start_FloodTime);
                string warning = $"{start_time.Month}月{start_time.Day}日{start_time.Hour}时，预报{xzhq_name}开始进洪";
                Warning_Info reach_warning = new Warning_Info(xzhq_name, level, warning);

                //整体预警级别根据各对象最高预警级别
                if ((int)reach_warning.Level < (int)warning_level) warning_level = reach_warning.Level;

                //各预警级别数量
                if (warning_level == Warning_Level.red_warining)
                {
                    xzhq_on_name = xzhq_name; xzhq_on_count++;
                }

                xzhq_warning_list.Add(reach_warning);
            }
            
            //组织风险预警概述文字
            if (warning_level != Warning_Level.blue_warining)
            {
                warning_desc = xzhq_on_count == 0 ? "" : $"{xzhq_on_name}等{xzhq_on_count}个蓄滞洪区预报存在进洪风险";
            }

            //生成对象
            Warning_Info xzhq_warninginfo = new Warning_Info(warning_name, warning_level, warning_desc, xzhq_warning_list);

            return xzhq_warninginfo;
        }

        //获取河道预警信息
        public static Warning_Info Get_Reach_Warninginfo(string plan_code, Dictionary<string, ReachSection_FloodRes> reach_result,
            Dictionary<string, object> reach_risk ,List<Water_Condition> now_waterlevel)
        {
            //初始化预警信息
            string warning_name = "河道预警";
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "各主干河道无风险预警";

            //遍历河道主要站点
            List<Warning_Info> reach_warning_list = new List<Warning_Info>();
            int bz_count = 0; string bz_station_name = "";
            for (int i = 0; i < reach_result.Count; i++)
            {
                string station_name = reach_result.ElementAt(i).Key;
                string station_stcd = reach_result.ElementAt(i).Value.Stcd;
                if (!station_stcd.StartsWith("31")) continue;  //不是以31开头的不算水文站点

                //获取站点结果
                ReachSection_FloodRes reach_sectionres = reach_result.ElementAt(i).Value;
                if (reach_sectionres == null) continue;

                //获取单个河道站点的预警信息
                Warning_Info reach_warning = Get_Station_Warninginfo(station_name, reach_sectionres, now_waterlevel);
                if (reach_warning.Level == Warning_Level.blue_warining) continue;

                //整体预警级别根据各对象最高预警级别
                if ((int)reach_warning.Level < (int)warning_level) warning_level = reach_warning.Level;

                //各预警级别数量
                if (warning_level == Warning_Level.red_warining)
                {
                    bz_station_name = station_name; bz_count++;
                }

                reach_warning_list.Add(reach_warning);
            }

            //遍历漫滩河道(只统计大沙河、共渠、卫河)
            Dictionary<string, List<Reach_MDRisk_Result>> mt_risk_reach = reach_risk["漫滩风险"] as Dictionary<string, List<Reach_MDRisk_Result>>;
            List<string> mt_tj_reachlist = new List<string>() { "DSH", "GQ", "WH" };
            double mt_length = 0; string mt_reach = "";
            for (int i = 0; i < mt_risk_reach.Count; i++)
            {
                string reach_name = mt_risk_reach.ElementAt(i).Key;
                if (!mt_tj_reachlist.Contains(reach_name)) continue;

                //遍历各河段
                List<Reach_MDRisk_Result> reach_mt = mt_risk_reach.ElementAt(i).Value;
                for (int j = 0; j < reach_mt.Count; j++)
                {
                    string object_name = reach_mt[j].Reach_CNname + reach_mt[j].Start_Chainage + "-" + reach_mt[j].End_Chainage;
                    Warning_Level level = Warning_Level.yellow_warining;
                    string desc = $"{object_name}，总长度{reach_mt[j].Distance}m,洪水出槽漫滩";
                    Warning_Info warning_info = new Warning_Info(object_name, level, desc);

                    //整体预警级别根据各对象最高预警级别
                    if ((int)level < (int)warning_level) warning_level = level;

                    //累积
                    mt_reach = reach_mt[j].Reach_CNname;
                    mt_length += reach_mt[j].Distance;

                    reach_warning_list.Add(warning_info);
                }
            }

            //遍历漫堤河道(只统计大沙河、共渠、卫河、淇河、安阳河)
            Dictionary<string, List<Reach_MDRisk_Result>> md_risk_reach = reach_risk["漫堤风险"] as Dictionary<string, List<Reach_MDRisk_Result>>;
            List<string> md_tj_reachlist = new List<string>() { "DSH", "GQ", "WH","AYH","QH" };
            int md_count = 0; string md_reach = "";
            for (int i = 0; i < md_risk_reach.Count; i++)
            {
                string reach_name = md_risk_reach.ElementAt(i).Key;
                if (!md_tj_reachlist.Contains(reach_name)) continue;

                //遍历各河段
                List<Reach_MDRisk_Result> reach_md = md_risk_reach.ElementAt(i).Value;
                for (int j = 0; j < reach_md.Count; j++)
                {
                    string object_name = reach_md[j].Reach_CNname + Math.Round((reach_md[j].Start_Chainage + reach_md[j].End_Chainage)/2);
                    Warning_Level level = Warning_Level.red_warining;
                    string desc = $"{object_name}附近，预报洪水存在漫堤风险";
                    Warning_Info warning_info = new Warning_Info(object_name, level, desc);

                    //整体预警级别根据各对象最高预警级别
                    if ((int)level < (int)warning_level) warning_level = level;

                    //累积
                    md_reach = reach_md[j].Reach_CNname;
                    md_count++;

                    reach_warning_list.Add(warning_info);
                }
            }

            //组织风险预警概述文字
            if (warning_level != Warning_Level.blue_warining)
            {
                string bz_info = bz_count == 0 ? "" : $"{bz_station_name}等{bz_count}座水文站预报水位超保证;";
                string mt_info = mt_length == 0 ? "" : $"{mt_reach}等总计{mt_length}m河段预报存在漫滩风险;";
                string md_info = md_count == 0 ? "" : $"{md_reach}等{md_count}处河道预报存在漫堤风险";
                warning_desc = bz_info + mt_info + md_info;
            }

            //生成对象
            Warning_Info reach_warninginfo = new Warning_Info(warning_name, warning_level, warning_desc, reach_warning_list);

            return reach_warninginfo;
        }

        //获取单个河道站点的预警信息
        private static Warning_Info Get_Station_Warninginfo(string station_name, ReachSection_FloodRes reach_sectionres,
            List<Water_Condition> now_waterlevel)
        {
            //水位预警
            double max_level = reach_sectionres.Max_Level;
            double max_q = reach_sectionres.Max_Qischarge;
            DateTime max_level_time = reach_sectionres.MaxQ_AtTime;

            Water_Condition res_watercondition = Water_Condition.Get_WaterCondition(now_waterlevel, station_name);
            double level1 = double.Parse(res_watercondition.Level1);  //警戒水位
            double level3 = double.Parse(res_watercondition.Level3); //保证水位
            Warning_Level warning_level = Warning_Level.blue_warining;
            string level_name = "";
            if (max_level >= level3)
            {
                warning_level = Warning_Level.red_warining;
                level_name = "超过保证水位";
            }
            else if (max_level >= level1)
            {
                warning_level = Warning_Level.yellow_warining;
                level_name = "超过警戒水位";
            }
            else
            {
                warning_level = Warning_Level.blue_warining;
                level_name = "在警戒水位以下";
            }
            string warning_desc = $"{max_level_time.Month}月{max_level_time.Day}日{max_level_time.Hour}时，{station_name}预报洪峰流量{max_q}m³/s，最高水位{max_level}m，{level_name}";

            Warning_Info warning_info = new Warning_Info(station_name, warning_level, warning_desc);

            return warning_info;
        }

        //获取水库预警信息
        public static Warning_Info Get_AllRes_Warninginfo(string plan_code, Dictionary<string, Reservoir_FloodRes> res_results,
            List<Water_Condition> now_waterlevel)
        {
            //初始化预警信息
            string warning_name = "水库预警";
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "各大中型水库无风险预警";

            //遍历水库
            List<Warning_Info> res_warning_list = new List<Warning_Info>();
            int xx_count = 0; string xx_res_name = "";
            int fhg_count = 0; string fhg_res_name = "";
            int xh_count = 0;string xh_res_name = "";
            for (int i = 0; i < res_results.Count; i++)
            {
                string res_name = res_results.ElementAt(i).Key;

                //获取水库结果
                Reservoir_FloodRes res_result = res_results.Keys.Contains(res_name)? res_results[res_name]: null;
                if (res_result == null) continue;

                //获取高水位和泄洪预警信息
                Warning_Info res_warning = Get_ResLevelXh_Warninginfo(res_name, res_result, now_waterlevel);
                if (res_warning.Level == Warning_Level.blue_warining) continue;

                //整体预警级别根据各对象最高预警级别
                if ((int)res_warning.Level < (int)warning_level) warning_level = res_warning.Level;

                //各预警级别数量
                if(warning_level == Warning_Level.red_warining)
                {
                    if (res_warning.Desc.Contains("发生洪水溢流泄洪"))
                    {
                        xh_res_name = res_name; xh_count++;
                    }
                    else
                    {
                        fhg_res_name = res_name; fhg_count++;
                    }
                }
                else if(warning_level == Warning_Level.yellow_warining || warning_level == Warning_Level.orange_warining)
                {
                    xx_res_name = res_name; xx_count++;
                }

                res_warning_list.Add(res_warning);
            }

            //组织风险预警概述文字
            if(warning_level != Warning_Level.blue_warining)
            {
                string xh_info = xh_count == 0 ? "": $"{xh_res_name}等{xh_count}座水库预报存在溢流泄洪风险;";
                string fhg_info = fhg_count == 0 ? "" : $"{fhg_res_name}等{fhg_count}座水库预报最高水位超防洪高水位;";
                string xx_info = xx_count == 0 ? "" : $"{xx_res_name}等{xx_count}座水库预报最高水位超汛限水位";
                warning_desc = xh_info + fhg_info + xx_info;
            }

            //生成对象
            Warning_Info res_warninginfo = new Warning_Info(warning_name, warning_level, warning_desc, res_warning_list);

            return res_warninginfo;
        }

        //获取单个水库高水位和泄洪预警信息
        private static Warning_Info Get_ResLevelXh_Warninginfo(string res_name, Reservoir_FloodRes res_result,
            List<Water_Condition> now_waterlevel)
        {
            //水位预警
            double max_level = res_result.Max_Level;
            DateTime max_level_time = res_result.MaxLevel_Time;
            Water_Condition res_watercondition = Water_Condition.Get_WaterCondition(now_waterlevel, res_name);
            double level1 = double.Parse(res_watercondition.Level1);  //汛限水位
            double level2 = double.Parse(res_watercondition.Level2); //防护高水位
            double level3 = double.Parse(res_watercondition.Level3); //设计水位
            Warning_Level warning_level = Warning_Level.blue_warining;
            string level_name = "";
            Struct_BasePars res_yhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");
            bool yhd_xh;
            if (res_yhd == null)
            {
                yhd_xh = false;
            }
            else
            {
                yhd_xh = (res_yhd.gate_type == GateType.NOGATE && max_level >= res_yhd.datumn) ? true : false;
            }
            if (yhd_xh) warning_level = Warning_Level.red_warining;

            if (max_level >= level3)
            {
                warning_level = Warning_Level.red_warining;
                level_name = "超过设计水位";
            }
            else if (max_level >= level2)
            {
                warning_level = Warning_Level.orange_warining;
                level_name = "超过防洪高高程";
            }
            else if (max_level >= level1)
            {
                warning_level = Warning_Level.yellow_warining;
                level_name = "超过汛限水位";
            }
            else
            {
                warning_level = Warning_Level.blue_warining;
                level_name = "在汛限水位以下";
            }

            string warning_desc = $"1、{max_level_time.Month}月{max_level_time.Day}日{max_level_time.Hour}时，{res_name}预报最高水位{max_level}m，{level_name}";

            //泄洪预警
            if(yhd_xh) warning_desc = warning_desc + "; 2、库水位超过溢洪道堰顶高程，发生洪水溢流泄洪";

            Warning_Info warning_info = new Warning_Info(res_name, warning_level, warning_desc);

            return warning_info;
        }

        //获取水库重点巡查信息
        public static List<Important_Inspect_UnitInfo> Get_AllRes_Inspectinfo(HydroModel model,Dictionary<string, Reservoir_FloodRes> res_results,
            List<Water_Condition> now_waterlevel)
        {
            List<Important_Inspect_UnitInfo> res_inspect_list = new List<Important_Inspect_UnitInfo>();
            string plan_code = model.Modelname;
            List<Reservoir> res_info = Item_Info.Get_ResInfo(Mysql_GlobalVar.now_instance);
            ReachList reach_list = model.Mike11Pars.ReachList;

            //遍历水库
            for (int i = 0; i < res_results.Count; i++)
            {
                string res_name = res_results.ElementAt(i).Key;

                //获取水库结果
                Reservoir_FloodRes res_result = res_results.Keys.Contains(res_name) ? res_results[res_name] : null;
                if (res_result == null) continue;

                //水位判断
                double max_level = res_result.Max_Level;
                Water_Condition res_watercondition = Water_Condition.Get_WaterCondition(now_waterlevel, res_name);
                double level1 = double.Parse(res_watercondition.Level1);  //汛限水位
                double level2 = double.Parse(res_watercondition.Level2); //防洪高水位
                double level3 = double.Parse(res_watercondition.Level3); //设计水位
                if (max_level < level1) continue;
                string inspect_reason = ""; string inspect_point = "";
                if (max_level >= level3)
                {
                    inspect_reason = "超过设计水位"; inspect_point = "大坝";
                }
                else if (max_level >= level2)
                {
                    inspect_reason = "超过防洪高水位"; inspect_point = "大坝";
                }
                else if (max_level >= level1)
                {
                    inspect_reason = "超过汛限水位"; inspect_point = "大坝";
                }

                //溢洪道溢流泄洪判断
                Struct_BasePars res_yhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");
                bool yhd_xh;
                if (res_yhd == null)
                {
                    yhd_xh = false;
                }
                else
                {
                    yhd_xh = (res_yhd.gate_type == GateType.NOGATE && max_level >= res_yhd.datumn) ? true : false;
                }

                if (yhd_xh)
                {
                    inspect_reason = inspect_reason + "，溢洪道溢流泄洪";
                    inspect_point = "大坝、溢洪道";
                }

                string at_city = Item_Info.Get_SingleStruct_LocationInfo(res_name).city;
                string admin_uint = Item_Info.Get_SingleStruct_LocationInfo(res_name).admin_unit;

                //水库的位置
                List<PointXY> location = new List<PointXY>();
                Reservoir resinfo = res_info.FirstOrDefault(s => s.Name == res_name);
                AtReach res_atreach = AtReach.Get_Atreach(resinfo.Atreach_Segment.reachname, resinfo.Atreach_Segment.end_chainage);
                PointXY res_p = reach_list.Get_ReachPointxy(res_atreach.reachname, res_atreach.chainage);
                PointXY res_p1 = PointXY.CoordTranfrom(res_p, 4547, 4326, 6);
                location.Add(res_p1);

                Important_Inspect_UnitInfo inspect_info = new Important_Inspect_UnitInfo(res_name, 
                    "水库巡查",$"{res_name}{inspect_point}", inspect_reason, at_city, admin_uint, res_watercondition.Stcd, location);
                res_inspect_list.Add(inspect_info);
            }

            return res_inspect_list;
        }

        //获取蓄滞洪区重点巡查信息
        public static List<Important_Inspect_UnitInfo> Get_Xzhq_Inspectinfo(HydroModel model, Dictionary<string, Xzhq_FloodRes> xzhq_result)
        {
            List<Important_Inspect_UnitInfo> xzhq_inspect_list = new List<Important_Inspect_UnitInfo>();
            List<Xzhq_Info> xzhq_list = Item_Info.Get_XzhqInfo();
            ReachList reach_list = model.Mike11Pars.ReachList;
            string plan_code = model.Modelname;

            //遍历各蓄滞洪区
            for (int i = 0; i < xzhq_result.Count; i++)
            {
                string xzhq_name = xzhq_result.ElementAt(i).Key;

                //获取蓄滞洪区结果
                Xzhq_FloodRes xzhq_res = xzhq_result.ElementAt(i).Value;
                if (xzhq_res == null) continue;
                if (xzhq_res.Xzhq_State == "未启用") continue;

                //各蓄滞洪区的分洪闸堰信息
                Xzhq_Info xzhq_info = xzhq_list.FirstOrDefault(x => x.Name == xzhq_name);
                Dictionary<string, AtReach> inq_list = xzhq_info.InQ_Section_List;
                List<string> fhy_inqstr_name = new List<string>();
                for (int j = 0; j < inq_list.Count; j++)
                {
                    string inq_name = inq_list.ElementAt(j).Key;
                    if (inq_name.Contains("分洪")) fhy_inqstr_name.Add(inq_name);
                }

                //获取单个蓄滞洪区的巡查信息
                string inspect_reason = "";
                string inspect_region = "";

                Dictionary<string, double> fhy_maxq = xzhq_res.Max_FhyInQ;
                bool hasNonZeroValue = fhy_maxq.Values.Any(value => value != 0);
                if (hasNonZeroValue)      
                {
                    inspect_region = "蓄滞洪区";

                    //有分洪堰进洪
                    for (int j = 0; j < fhy_maxq.Count; j++)
                    {
                        inspect_region += $"、{fhy_maxq.ElementAt(j).Key}";
                    }
                    inspect_reason = xzhq_info.Only_FhyPd_State? "分洪(闸)堰进洪，蓄滞洪区启用":"河水漫溢+分洪(闸)堰进洪，蓄滞洪区启用";
                }
                else
                {
                    //河道漫溢进洪
                    inspect_reason = "河水漫溢，蓄滞洪区启用";
                    inspect_region = "蓄滞洪区";
                }

                string at_city = Item_Info.Get_SingleStruct_LocationInfo(xzhq_name).city;
                string admin_uint = Item_Info.Get_SingleStruct_LocationInfo(xzhq_name).admin_unit;

                //所在位置
                List<PointXY> location = new List<PointXY>();
                AtReach xzhq_atreach = xzhq_info.Level_Atreach;
                PointXY xzhq_p = reach_list.Get_ReachPointxy(xzhq_atreach.reachname, xzhq_atreach.chainage);
                PointXY xzhq_p1 = PointXY.CoordTranfrom(xzhq_p, 4547, 4326, 6);
                location.Add(xzhq_p1);

                Important_Inspect_UnitInfo inspect_info = new Important_Inspect_UnitInfo(xzhq_name+"蓄滞洪区",
                    "蓄滞洪区巡查", inspect_region, inspect_reason, at_city, admin_uint,xzhq_res.Stcd, location);
                xzhq_inspect_list.Add(inspect_info);
            }

            return xzhq_inspect_list;
        }

        //获取河道重点巡查河段信息
        public static List<Important_Inspect_UnitInfo> Get_Reach_Inspectinfo(HydroModel model, Dictionary<string, ReachSection_FloodRes> reach_result,
            Dictionary<string, object> reach_risk, List<Water_Condition> now_waterlevel)
        {
            List<Important_Inspect_UnitInfo> reach_inspect_list = new List<Important_Inspect_UnitInfo>();

            //遍历漫滩河道(只统计大沙河、共渠、卫河)
            Dictionary<string, List<Reach_MDRisk_Result>> mt_risk_reach = reach_risk["漫滩风险"] as Dictionary<string, List<Reach_MDRisk_Result>>;
            List<string> mt_tj_reachlist = new List<string>() { "DSH", "GQ", "WH" };
            for (int i = 0; i < mt_risk_reach.Count; i++)
            {
                string reach_name = mt_risk_reach.ElementAt(i).Key;
                if (!mt_tj_reachlist.Contains(reach_name)) continue;

                //遍历各河段
                List<Reach_MDRisk_Result> reach_mt = mt_risk_reach.ElementAt(i).Value;
                for (int j = 0; j < reach_mt.Count; j++)
                {
                    AtReach atreach = AtReach.Get_Atreach(reach_mt[j].Reach_Name, (reach_mt[j].Start_Chainage + reach_mt[j].End_Chainage)/2);
                    string reach_cnname = reach_mt[j].Reach_CNname;

                    //巡查信息
                    Struct_Region_Info region_info = Item_Info.Get_SingleStruct_LocationInfo(atreach);
                    if (region_info.city == "") continue;
                    string at_city = region_info.city;
                    string admin_uint = region_info.admin_unit;

                    string start_chainage_str = $"{Math.Floor(reach_mt[j].Start_Chainage / 1000.0)}+{(reach_mt[j].Start_Chainage % 1000).ToString("000")}";
                    string end_chainage_str = $"{Math.Floor(reach_mt[j].End_Chainage / 1000.0)}+{(reach_mt[j].End_Chainage % 1000).ToString("000")}";
                    string region = reach_cnname + "左岸滩地，桩号:" + start_chainage_str + "~" + end_chainage_str;

                    //获取巡查河段坐标信息
                    ReachList reach_list = model.Mike11Pars.ReachList;
                    List<PointXY> segment_jwd = reach_list.Get_Segment_PointjwdList(reach_mt[j].Reach_Name, reach_mt[j].Start_Chainage, reach_mt[j].End_Chainage);

                    Important_Inspect_UnitInfo inspect_info = new Important_Inspect_UnitInfo(reach_cnname,
                        "河道巡查", region, "左岸洪水漫滩", at_city, admin_uint,"", segment_jwd);
                    reach_inspect_list.Add(inspect_info);
                }
            }

            //遍历漫堤河道(只统计大沙河、共渠、卫河、淇河、安阳河)
            Dictionary<string, List<Reach_MDRisk_Result>> md_risk_reach = reach_risk["漫堤风险"] as Dictionary<string, List<Reach_MDRisk_Result>>;
            List<string> md_tj_reachlist = new List<string>() { "DSH", "GQ", "WH", "AYH", "QH", "TYH" };
            for (int i = 0; i < md_risk_reach.Count; i++)
            {
                string reach_name = md_risk_reach.ElementAt(i).Key;
                if (!md_tj_reachlist.Contains(reach_name)) continue;

                //遍历各河段
                List<Reach_MDRisk_Result> reach_md = md_risk_reach.ElementAt(i).Value;
                for (int j = 0; j < reach_md.Count; j++)
                {
                    string reach_cnname = reach_md[j].Reach_CNname;
                    AtReach atreach = AtReach.Get_Atreach(reach_md[j].Reach_Name, (reach_md[j].Start_Chainage + reach_md[j].End_Chainage) / 2);

                    //巡查信息
                    Struct_Region_Info region_info = Item_Info.Get_SingleStruct_LocationInfo(atreach);
                    if (region_info.city == "") continue;
                    string at_city = region_info.city;
                    string admin_uint = region_info.admin_unit;

                    string start_chainage_str = $"{Math.Floor(reach_md[j].Start_Chainage / 1000.0)}+{(reach_md[j].Start_Chainage % 1000).ToString("000")}";
                    string end_chainage_str = $"{Math.Floor(reach_md[j].End_Chainage / 1000.0)}+{(reach_md[j].End_Chainage % 1000).ToString("000")}";
                    string region = reach_cnname + "左右岸堤防，桩号:" + start_chainage_str + "~" + end_chainage_str;

                    //获取巡查河段坐标信息
                    ReachList reach_list = model.Mike11Pars.ReachList;
                    List<PointXY> segment_jwd = reach_list.Get_Segment_PointjwdList(reach_md[j].Reach_Name, reach_md[j].Start_Chainage, reach_md[j].End_Chainage);

                    Important_Inspect_UnitInfo inspect_info = new Important_Inspect_UnitInfo(reach_cnname, 
                        "河道巡查", region,"存在高水位漫堤风险", at_city, admin_uint,"", segment_jwd);
                    reach_inspect_list.Add(inspect_info);
                }
            }

            return reach_inspect_list;
        }
        #endregion **************************************************
    }
}
