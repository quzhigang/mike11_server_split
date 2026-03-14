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
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
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
        //查询所有河道的洪水风险结果(漫堤风险河段、漫滩风险河段、冲刷风险河段、倒灌支流河道、总干渠风险)
        public static Dictionary<string, object> Get_Reach_RiskResult(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, List<float>>> level_zd,
            Dictionary<string, Dictionary<DateTime, List<float>>> discharge_zd, Dictionary<string, Dictionary<DateTime, List<float>>> speed_zd,
            Dictionary<string, List<float>> dd_zd, Dictionary<string, List<float>> td_zd, Dictionary<string, List<float>> section_chainage,
            Dictionary<string, ReachSection_FloodRes> reach_result, Dictionary<string, Reservoir_FloodRes> res_result, string model_instance)
        {
            Dictionary<string, object> reach_risk = new Dictionary<string, object>();

            Dictionary<string, List<float>> level_max = Get_ZdRes_TimeMaxValue(level_zd);
            Dictionary<string, List<float>> discharge_max = Get_ZdRes_TimeMaxValue(discharge_zd);
            Dictionary<string, List<float>> speed_max = Get_ZdRes_TimeMaxValue(speed_zd);

            List<string> main_reach = Item_Info.Get_MainReach_Names(model_instance, true);
            Dictionary<string, List<Reach_MDRisk_Result>> reach_md_segment = Static_MDMT_ReachSeg(level_max, dd_zd, section_chainage, "漫堤", main_reach);
            Dictionary<string, List<Reach_MDRisk_Result>> reach_mt_segment = Static_MDMT_ReachSeg(level_max, td_zd, section_chainage, "漫滩", main_reach);
            Dictionary<string, List<Reach_CSRisk_Result>> reach_cs_segment = Static_CS_ReachSeg(speed_max, section_chainage);
            Dictionary<string, Reach_DGRisk_Result> reach_dg_infos = Static_DG_ReachInfo(discharge_zd);
            Dictionary<string, Dictionary<string, string>> zgq_risk = Static_ZGQ_Risk(reach_result, reach_cs_segment);

            reach_risk.Add("漫堤风险", reach_md_segment);
            reach_risk.Add("漫滩风险", reach_mt_segment);
            reach_risk.Add("冲刷风险", reach_cs_segment);
            reach_risk.Add("倒灌风险", reach_dg_infos);
            reach_risk.Add("总干渠风险", zgq_risk);

            return reach_risk;
        }

        private static Dictionary<string, Dictionary<string, string>> Static_ZGQ_Risk(Dictionary<string, ReachSection_FloodRes> reach_result,
            Dictionary<string, List<Reach_CSRisk_Result>> reach_cs_segment)
        {
            Dictionary<string, Dictionary<string, string>> zgq_risk = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, ZGQ_SectionInfo> zgq_section_info = Item_Info.Get_ZGQ_SectionInfo();

            for (int i = 0; i < reach_result.Count; i++)
            {
                Dictionary<string, string> risk = new Dictionary<string, string>();
                string section_name = reach_result.ElementAt(i).Key;
                ReachSection_FloodRes section_floodres = reach_result.ElementAt(i).Value;
                ZGQ_SectionInfo section_info = zgq_section_info.Keys.Contains(section_name) ? zgq_section_info[section_name] : null;
                if (section_info == null) continue;

                risk.Add("风险等级", Get_ZGQ_RiskLevel(section_name, section_floodres, section_info));
                risk.Add("漫堤风险", Get_ZGQ_Md_Risk(section_name, section_floodres, section_info));
                risk.Add("冲刷风险", Get_ZGQ_Cs_Risk(section_name, section_floodres, section_info, reach_cs_segment));
                risk.Add("浸泡风险", Get_ZGQ_Jp_Risk(section_name, section_floodres, section_info));

                zgq_risk.Add(section_name, risk);
            }

            return zgq_risk;
        }

        private static string Get_ZGQ_RiskLevel(string section_name, ReachSection_FloodRes section_res, ZGQ_SectionInfo section_info)
        {
            string risk_level = "无等级";
            double max_level = section_res.Max_Level;
            if (max_level >= section_info.sj_100n_level) risk_level = "Ⅰ级(特别重大)";
            else if (max_level >= section_info.sj_50n_level) risk_level = "Ⅱ级(重大)";
            else if (max_level >= section_info.sj_20n_level) risk_level = "Ⅲ级(较大)";
            else if (max_level >= section_info.sj_10n_level) risk_level = "Ⅳ级(一般)";

            if (max_level >= section_info.dd_level) risk_level = "Ⅰ级(特别重大)";
            return risk_level;
        }

        private static string Get_ZGQ_Md_Risk(string section_name, ReachSection_FloodRes section_res, ZGQ_SectionInfo section_info)
        {
            string desc1 = section_res.Max_Level < section_info.dd_level ? "低于" : "高于";
            string desc2 = section_res.Max_Level < section_info.dd_level ? "不" : "";
            return $"进口最高水位{section_res.Max_Level}m，{desc1}{section_info.dd_level_name}高程{section_info.dd_level}m，{desc2}存在漫堤风险；";
        }

        private static string Get_ZGQ_Cs_Risk(string section_name, ReachSection_FloodRes section_res, ZGQ_SectionInfo section_info,
            Dictionary<string, List<Reach_CSRisk_Result>> reach_cs_segment)
        {
            string risk_cs = "局部区域存在冲刷风险。";
            double up_chainage = Math.Max(section_info.section_atreach.chainage - 300, 0);
            double down_chainage = section_info.section_atreach.chainage + 300;
            if (reach_cs_segment.Keys.Contains(section_info.section_atreach.reachname))
            {
                List<Reach_CSRisk_Result> cs_segment = reach_cs_segment[section_info.section_atreach.reachname];
                for (int i = 0; i < cs_segment.Count; i++)
                {
                    bool start_in = (cs_segment[i].Start_Chainage >= up_chainage) && (cs_segment[i].Start_Chainage <= section_info.section_atreach.chainage);
                    bool end_in = (cs_segment[i].End_Chainage >= up_chainage) && (cs_segment[i].End_Chainage <= section_info.section_atreach.chainage);
                    bool start_in1 = (cs_segment[i].Start_Chainage <= down_chainage) && (cs_segment[i].Start_Chainage >= section_info.section_atreach.chainage);
                    bool end_in1 = (cs_segment[i].End_Chainage <= down_chainage) && (cs_segment[i].End_Chainage >= section_info.section_atreach.chainage);

                    if (start_in || end_in) risk_cs = "上游附近河道平均流速达到2.0m/s以上，存在大范围河床冲刷风险；";
                    if (start_in1 || end_in1) risk_cs = "下游附近河道平均流速达到2.0m/s以上，存在大范围河床冲刷风险；";
                }
            }

            return risk_cs;
        }

        private static string Get_ZGQ_Jp_Risk(string section_name, ReachSection_FloodRes section_res, ZGQ_SectionInfo section_info)
        {
            string jp_desc = $"最高水位高于渠坡底高程{section_info.hp_bottom_level}m，渠坡存在浸泡风险";
            return section_res.Max_Level >= section_info.hp_bottom_level ? jp_desc : "不存在渠坡浸泡风险";
        }

        private static Dictionary<string, Reach_DGRisk_Result> Static_DG_ReachInfo(Dictionary<string, Dictionary<DateTime, List<float>>> discharge_zd)
        {
            Dictionary<string, Reach_DGRisk_Result> res = new Dictionary<string, Reach_DGRisk_Result>();
            List<string> dg_reach = Item_Info.Get_DG_Reach();
            Dictionary<string, Dictionary<DateTime, List<float>>> dg_reach_discharge = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            for (int i = 0; i < discharge_zd.Count; i++)
            {
                string reach_name = discharge_zd.ElementAt(i).Key;
                if (dg_reach.Contains(reach_name))
                {
                    dg_reach_discharge.Add(reach_name, discharge_zd[reach_name]);
                }
            }

            for (int i = 0; i < dg_reach_discharge.Count; i++)
            {
                string reach_name = dg_reach_discharge.ElementAt(i).Key;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                Dictionary<DateTime, List<float>> reach_discharge = dg_reach_discharge.ElementAt(i).Value;
                Dictionary<DateTime, float> lastsection_discharge = GetLastValue(reach_discharge);
                float min_discharge = lastsection_discharge.Values.Min();

                if (min_discharge < Model_Const.REACH_DG_DISCHARGE)
                {
                    DateTime dg_start_time = lastsection_discharge.Keys.First();
                    int dg_start_index = 0;
                    for (int j = 0; j < lastsection_discharge.Count; j++)
                    {
                        if (lastsection_discharge.ElementAt(j).Value < 0)
                        {
                            dg_start_time = lastsection_discharge.ElementAt(j).Key;
                            dg_start_index = j;
                            break;
                        }
                    }

                    DateTime dg_end_time = lastsection_discharge.Keys.Last();
                    int dg_end_index = lastsection_discharge.Count - 1;
                    for (int j = lastsection_discharge.Count - 1; j >= 0; j--)
                    {
                        if (lastsection_discharge.ElementAt(j).Value > 0)
                        {
                            dg_end_time = lastsection_discharge.ElementAt(j).Key;
                            dg_end_index = j;
                            break;
                        }
                    }

                    double total_dg_hours = Math.Round(Math.Abs(dg_end_time.Subtract(dg_start_time).TotalHours), 1);
                    Dictionary<DateTime, float> dg_flood_gc = lastsection_discharge.Skip(dg_start_index).
                        Take(dg_end_index - dg_start_index + 1).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    double total_dg_volumn = dg_flood_gc == null ? 0 : Math.Abs(Math.Round(Get_TotalVolume(dg_flood_gc), 3));

                    Reach_DGRisk_Result dg_object = new Reach_DGRisk_Result(reach_name, reach_cnname, dg_start_time, Math.Round(Math.Abs(min_discharge)), total_dg_volumn, total_dg_hours);
                    res.Add(reach_name, dg_object);
                }
            }

            return res;
        }

        public static Dictionary<DateTime, float> GetLastValue(Dictionary<DateTime, List<float>> reach_discharge)
        {
            Dictionary<DateTime, float> last_dic = new Dictionary<DateTime, float>();
            foreach (var entry in reach_discharge)
            {
                DateTime key = entry.Key;
                List<float> values = entry.Value;
                if (values.Count > 0)
                {
                    float lastValue = values[values.Count - 1];
                    last_dic[key] = lastValue;
                }
            }

            return last_dic;
        }

        private static Dictionary<string, List<Reach_CSRisk_Result>> Static_CS_ReachSeg(Dictionary<string, List<float>> speed_max,
            Dictionary<string, List<float>> section_chainage)
        {
            Dictionary<string, List<Reach_CSRisk_Result>> res = new Dictionary<string, List<Reach_CSRisk_Result>>();
            Dictionary<string, List<float>> reach_bc = Item_Info.Get_Reach_NoDestorySpeed(section_chainage);
            Dictionary<string, List<float>> reach_cs = new Dictionary<string, List<float>>();
            for (int i = 0; i < speed_max.Count; i++)
            {
                string reach_name = speed_max.ElementAt(i).Key;
                List<float> reach_speed_max = speed_max.ElementAt(i).Value;
                List<float> reach_bc_speed = reach_bc[reach_name];
                List<float> chainage = section_chainage[reach_name];
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                if (reach_cnname.Contains("连接河") || reach_cnname.Contains("分洪堰") ||
                    reach_cnname.Contains("泄洪洞") || reach_cnname.Contains("溢洪") ||
                    reach_cnname.Contains("保留区")) continue;

                List<float> cs_section_list = new List<float>();
                for (int j = 0; j < reach_speed_max.Count; j++)
                {
                    if (reach_speed_max[j] > reach_bc_speed[j]) cs_section_list.Add(chainage[j]);
                }
                if (cs_section_list.Count >= 2) reach_cs.Add(reach_name, cs_section_list);
            }

            Dictionary<string, Dictionary<float, float>> reach_hb = Merge_ReachSegments(reach_cs);

            for (int i = 0; i < reach_hb.Count; i++)
            {
                string reach_name = reach_hb.ElementAt(i).Key;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                Dictionary<float, float> reach_segment = reach_hb.ElementAt(i).Value;
                List<Reach_CSRisk_Result> reach_cs_list = new List<Reach_CSRisk_Result>();
                for (int j = 0; j < reach_segment.Count; j++)
                {
                    double start_chainage = Item_Info.Get_Design_Chainage(reach_name, reach_segment.ElementAt(j).Key);
                    double end_chainage = Item_Info.Get_Design_Chainage(reach_name, reach_segment.ElementAt(j).Value);

                    double tempt = 0;
                    if (end_chainage < start_chainage)
                    {
                        tempt = start_chainage;
                        start_chainage = end_chainage;
                        end_chainage = tempt;
                    }

                    Reach_CSRisk_Result cs_object = new Reach_CSRisk_Result(reach_name, reach_cnname, start_chainage, end_chainage);
                    if (cs_object.End_Chainage - cs_object.Start_Chainage > 300) reach_cs_list.Add(cs_object);
                }
                res.Add(reach_name, reach_cs_list);
            }

            return res;
        }

        private static Dictionary<string, List<Reach_MDRisk_Result>> Static_MDMT_ReachSeg(Dictionary<string, List<float>> level_max,
            Dictionary<string, List<float>> dd_zd, Dictionary<string, List<float>> section_chainage, string risk_type, List<string> reach_list = null)
        {
            Dictionary<string, List<Reach_MDRisk_Result>> res = new Dictionary<string, List<Reach_MDRisk_Result>>();
            Dictionary<string, List<float>> reach_md = new Dictionary<string, List<float>>();
            List<string> reachs = reach_list == null ? level_max.Keys.ToList() : reach_list;
            for (int i = 0; i < reachs.Count; i++)
            {
                string reach_name = reachs[i];
                List<float> reach_level_max = level_max[reach_name];
                List<float> reach_dd_level = dd_zd[reach_name];
                List<float> chainage = section_chainage[reach_name];
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                if (reach_cnname.Contains("连接河") || reach_cnname.Contains("分洪堰") ||
                    reach_cnname.Contains("泄洪洞") || reach_cnname.Contains("溢洪") || reach_cnname.Contains("溃口支流")) continue;

                List<float> md_section_list = new List<float>();
                for (int j = 0; j < reach_level_max.Count; j++)
                {
                    if (reach_level_max[j] >= reach_dd_level[j] - 0.1) md_section_list.Add(chainage[j]);
                }
                if (md_section_list.Count >= 2) reach_md.Add(reach_name, md_section_list);
            }

            Dictionary<string, Dictionary<float, float>> reach_hb = Merge_ReachSegments(reach_md);
            for (int i = 0; i < reach_hb.Count; i++)
            {
                string reach_name = reach_hb.ElementAt(i).Key;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                Dictionary<float, float> reach_segment = reach_hb.ElementAt(i).Value;
                List<Reach_MDRisk_Result> reach_md_list = new List<Reach_MDRisk_Result>();
                for (int j = 0; j < reach_segment.Count; j++)
                {
                    double start_chainage = Item_Info.Get_Design_Chainage(reach_name, reach_segment.ElementAt(j).Key);
                    double end_chainage = Item_Info.Get_Design_Chainage(reach_name, reach_segment.ElementAt(j).Value);

                    double tempt = 0;
                    if (end_chainage < start_chainage)
                    {
                        tempt = start_chainage;
                        start_chainage = end_chainage;
                        end_chainage = tempt;
                    }

                    Reach_MDRisk_Result md_object = new Reach_MDRisk_Result(reach_name, reach_cnname, start_chainage, end_chainage, risk_type);
                    reach_md_list.Add(md_object);
                }
                res.Add(reach_name, reach_md_list);
            }

            return res;
        }

        public static Dictionary<string, Dictionary<float, float>> Merge_ReachSegments(Dictionary<string, List<float>> reach_md)
        {
            Dictionary<string, Dictionary<float, float>> reach_hb = new Dictionary<string, Dictionary<float, float>>();
            float section_bc = (float)Model_Const.MIKE11_DEFAULTDX;
            Dictionary<string, List<float>> reach_md_xz = new Dictionary<string, List<float>>();
            for (int i = 0; i < reach_md.Count; i++)
            {
                string reach_name = reach_md.ElementAt(i).Key;
                List<float> sections = reach_md.ElementAt(i).Value;
                List<float> new_sections = new List<float>();

                float pre_section = sections[0];
                for (int j = 0; j < sections.Count; j++)
                {
                    if (j == 0 || sections[j] - pre_section > section_bc * 1.5)
                    {
                        if (!new_sections.Contains(sections[j] - section_bc)) new_sections.Add(Math.Max(0, sections[j] - section_bc));
                    }

                    if (!new_sections.Contains(sections[j])) new_sections.Add(sections[j]);
                    pre_section = sections[j];
                }
                reach_md_xz.Add(reach_name, new_sections);
            }

            for (int i = 0; i < reach_md_xz.Count; i++)
            {
                string reach_name = reach_md_xz.ElementAt(i).Key;
                List<float> sections = reach_md_xz.ElementAt(i).Value;
                Dictionary<float, float> segments = new Dictionary<float, float>();

                float start_section = sections[0];
                float pre_section = sections[0];

                for (int j = 1; j < sections.Count; j++)
                {
                    float current_section = sections[j];
                    if (current_section - pre_section > section_bc * 1.5)
                    {
                        segments.Add(start_section, pre_section);
                        start_section = current_section;
                    }

                    pre_section = current_section;
                }

                segments.Add(start_section, pre_section);
                reach_hb.Add(reach_name, segments);
            }

            return reach_hb;
        }
    }
}
