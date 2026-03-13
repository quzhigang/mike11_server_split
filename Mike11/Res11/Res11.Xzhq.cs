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
        //获取蓄滞洪区启用状态，判断是否有超设计启用的
        public static bool Get_Xzhq_Csj_Result(HydroModel hydromodel,out Dictionary<string, List<string>> xzhq_main_res)
        {
            xzhq_main_res = new Dictionary<string, List<string>>();
            bool no_csj = true;

            if (hydromodel == null || !File.Exists(hydromodel.Modelfiles.Hdres11_filename)) return no_csj;

            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);
            IDataItem dataitem = resdata.Reaches.ElementAt(0).DataItems.ElementAt(0);
            if (dataitem == null) return no_csj;

            Dictionary<string, Dictionary<DateTime, string>> sub_xzhq_res;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = Res11.Get_Xzhq_FloodRes(hydromodel, resdata, out sub_xzhq_res);

            for (int i = 0; i < xzhq_result.Count; i++)
            {
                string name = xzhq_result.ElementAt(i).Key;
                Xzhq_FloodRes xzhq_res = xzhq_result[name];
                if (xzhq_res.Stcd == "GRP_ZHQ" || xzhq_res.Stcd == "CJQ_ZHQ" || xzhq_res.Stcd == "GQX_ZHQ") continue;

                if (xzhq_res.Xzhq_State == "超设计")
                {
                    no_csj = false;
                    break;
                }
            }

            for (int i = 0; i < xzhq_result.Count; i++)
            {
                Xzhq_FloodRes xzhq_res = xzhq_result.ElementAt(i).Value;
                List<string> res = new List<string>() { xzhq_res.Xzhq_State, xzhq_res.Max_Level.ToString(),
                    xzhq_res.Max_Volumn.ToString(), xzhq_res .Max_Area.ToString()};
                xzhq_main_res.Add(xzhq_res.Name, res);
            }

            return no_csj;
        }

        //获取蓄滞洪区洪水结果
        public static Dictionary<string, Xzhq_FloodRes> Get_Xzhq_FloodRes(HydroModel hydromodel,
            ResultData resdata, out Dictionary<string, Dictionary<DateTime, string>> sub_xzhq_res)
        {
            Dictionary<string, Xzhq_FloodRes> xzhq_floodres = new Dictionary<string, Xzhq_FloodRes>();
            sub_xzhq_res = new Dictionary<string, Dictionary<DateTime, string>>();

            List<Xzhq_Info> xzhq_list = WG_INFO.Get_XzhqInfo();
            for (int i = 0; i < xzhq_list.Count; i++)
            {
                string stcd = xzhq_list[i].Stcd;
                string name = xzhq_list[i].Name;
                Dictionary<double, double> l_v = xzhq_list[i].Level_Volume;
                Dictionary<double, double> l_a = xzhq_list[i].Level_Area;

                Dictionary<string, AtReach> inq_list = xzhq_list[i].InQ_Section_List;
                Dictionary<string, AtReach> fhy_inq_list = new Dictionary<string, AtReach>();
                for (int j = 0; j < inq_list.Count; j++)
                {
                    string inq_name = inq_list.ElementAt(j).Key;
                    if (inq_name.Contains("分洪")) fhy_inq_list.Add(inq_name, inq_list.ElementAt(j).Value);
                }

                Dictionary<string, Dictionary<DateTime, double>> inq_dic_list = new Dictionary<string, Dictionary<DateTime, double>>();
                for (int j = 0; j < inq_list.Count; j++)
                {
                    AtReach atreach = inq_list.ElementAt(j).Value;
                    Dictionary<DateTime, double> inq_dic = Res11.Res11reader(hydromodel.Modelname, atreach, mike11_restype.Discharge, resdata);
                    Modify_Dic_Res(ref inq_dic, hydromodel.ModelGlobalPars.Ahead_hours, 1);
                    inq_dic_list.Add(inq_list.ElementAt(j).Key, inq_dic);
                }
                List<Dictionary<DateTime, double>> inq_dic_list1 = inq_dic_list.Values.ToList();
                Dictionary<DateTime, double> total_inq_dic = Dfs0.Combine_Dic(inq_dic_list1);

                Dictionary<string, double> max_fhyinq = new Dictionary<string, double>();
                Dictionary<string, Dictionary<DateTime, double>> fhy_inq_dic_list = new Dictionary<string, Dictionary<DateTime, double>>();
                for (int j = 0; j < fhy_inq_list.Count; j++)
                {
                    string fhy_name = fhy_inq_list.ElementAt(j).Key;
                    Dictionary<DateTime, double> fhy_inq_dic = inq_dic_list[fhy_name];
                    fhy_inq_dic_list.Add(fhy_name, fhy_inq_dic);
                    double max_fh_q = Math.Round(fhy_inq_dic.Values.Max(), 1);
                    max_fhyinq.Add(fhy_name, max_fh_q);
                }
                List<Dictionary<DateTime, double>> fhy_inq_dic_list1 = fhy_inq_dic_list.Values.ToList();
                Dictionary<DateTime, double> total_fhy_inq_dic = Dfs0.Combine_Dic(fhy_inq_dic_list1);

                Dictionary<string, AtReach> outq_list = xzhq_list[i].OutQ_Section_List;
                Dictionary<string, Dictionary<DateTime, double>> outq_dic_list = new Dictionary<string, Dictionary<DateTime, double>>();
                for (int j = 0; j < outq_list.Count; j++)
                {
                    AtReach atreach = outq_list.ElementAt(j).Value;
                    Dictionary<DateTime, double> outq_dic = Res11.Res11reader(hydromodel.Modelname, atreach, mike11_restype.Discharge, resdata);
                    Modify_Dic_Res(ref outq_dic, hydromodel.ModelGlobalPars.Ahead_hours, 1);
                    outq_dic_list.Add(outq_list.ElementAt(j).Key, outq_dic);
                }
                List<Dictionary<DateTime, double>> outq_dic_list1 = outq_dic_list.Values.ToList();
                Dictionary<DateTime, double> total_outq_dic = Dfs0.Combine_Dic(outq_dic_list1);

                Dictionary<DateTime, double> level_dic = Res11.Res11reader(hydromodel.Modelname, xzhq_list[i].Level_Atreach, mike11_restype.Water_Level);
                Modify_Dic_Res(ref level_dic, hydromodel.ModelGlobalPars.Ahead_hours, 1);
                Dictionary<DateTime, double> volumn_Dic = l_v.Count == 0 ? new Dictionary<DateTime, double>() : File_Common.Insert_ResVolumn(level_dic, xzhq_list[i].Level_Volume);
                Dictionary<DateTime, double> area_Dic = l_a.Count == 0 ? new Dictionary<DateTime, double>() : File_Common.Insert_ResVolumn(level_dic, xzhq_list[i].Level_Area);

                double xzhq_datumn = l_v.Count == 0 ?10000: xzhq_list[i].Level_Volume.First().Key;
                double max_level = level_dic.Values.Max();
                double max_h = Math.Round(Math.Max(max_level - xzhq_datumn,0),2);
                string max_level_str = max_level.ToString();
                double max_volumn = volumn_Dic.Count == 0 ? 0 : volumn_Dic.Values.Max();
                double max_area = area_Dic.Count == 0 ? 0 : area_Dic.Values.Max();
                string maxvolumn_time = level_dic == null ? "" : SimulateTime.TimetoStr(level_dic.OrderByDescending(entry => entry.Value).First().Key);
                if (maxvolumn_time == SimulateTime.TimetoStr(level_dic.ElementAt(0).Key)) maxvolumn_time = "";

                string state = "未启用";string state_q_time = "";
                string design_level_str = xzhq_list[i].Design_Level.Contains("/") ? xzhq_list[i].Design_Level.Split('/')[0] : xzhq_list[i].Design_Level;
                double design_level = double.Parse(design_level_str);
                if (xzhq_list[i].Only_FhyPd_State)
                {
                    if (total_fhy_inq_dic.Values.Max() > 20)
                    {
                        state = "启用";
                        if (max_level > design_level + 0.2) state = "超设计";
                    }
                    else
                    {
                        state = "未启用";
                    }
                }
                else
                {
                    Dictionary<DateTime, double> state_atreach_q = Res11.Res11reader(hydromodel.Modelname, xzhq_list[i].State_Pd_AtReachQ.ElementAt(0).Key, mike11_restype.Discharge);
                    double state_atreach_maxq = state_atreach_q.Values.Max();
                    double atreach_q = xzhq_list[i].State_Pd_AtReachQ.ElementAt(0).Value;
                    if (state_atreach_maxq > atreach_q && max_volumn > 100)
                    {
                        state_q_time = SimulateTime.TimetoStr(state_atreach_q.FirstOrDefault(x => x.Value >= atreach_q).Key);
                        state = max_level > design_level + 0.2 ? "超设计" : "启用";
                    }
                }

                string start_time_str = ""; double max_floodtime = 0;
                if (state != "未启用")
                {
                    DateTime start_time1 = total_fhy_inq_dic == null ? level_dic.Last().Key : total_fhy_inq_dic.FirstOrDefault(x => x.Value != 0).Key;
                    DateTime start_time2 = level_dic.FirstOrDefault(x => x.Value >= xzhq_list[i].Xzhq_IsFlood_Level).Key;
                    DateTime start_time = DateTime.Compare(start_time1, start_time2) < 0 ? start_time1 : start_time2;
                    if (start_time.Year == 1) start_time = start_time2;
                    start_time_str = start_time == level_dic.Last().Key ? "" : SimulateTime.TimetoStr(start_time);
                    if (start_time_str == "0001/01/01 00:00:00") start_time_str = "";
                    if (start_time_str == "" && state_q_time != "")
                    {
                        start_time_str = state_q_time;
                        start_time = SimulateTime.StrToTime(start_time_str);
                    }

                    DateTime end_time = level_dic.LastOrDefault(x => x.Value > xzhq_list[i].Xzhq_IsFlood_Level).Key;
                    max_floodtime = Math.Round(Math.Max(end_time.Subtract(start_time).TotalHours, 0), 1);
                }
                else
                {
                    max_area = 0; max_volumn = 0; max_level_str = "";
                }

                Dictionary<DateTime, string> xzhq_state_list = new Dictionary<DateTime, string>();
                if (state == "未启用")
                {
                    for (int j = 0; j < level_dic.Count; j++)
                    {
                        xzhq_state_list.Add(level_dic.ElementAt(j).Key, state);
                    }
                }
                else
                {
                    DateTime start_time = SimulateTime.StrToTime(start_time_str);
                    for (int j = 0; j < level_dic.Count; j++)
                    {
                        DateTime now_time = level_dic.ElementAt(j).Key;
                        string now_state = now_time.CompareTo(start_time) < 0 ? "未启用" : "启用";
                        xzhq_state_list.Add(level_dic.ElementAt(j).Key, now_state);
                    }
                }
                sub_xzhq_res.Add(stcd, xzhq_state_list);

                Xzhq_FloodRes fhblq_res = new Xzhq_FloodRes(stcd, name, state, start_time_str, max_volumn, max_level_str,max_h, max_area,
                    max_floodtime, maxvolumn_time, max_fhyinq, inq_dic_list, total_inq_dic, outq_dic_list,
                    total_outq_dic, level_dic, volumn_Dic, area_Dic, xzhq_state_list);
                xzhq_floodres.Add(name, fhblq_res);
            }

            Dictionary<string, Xzhq_FloodRes> xzhq_res = Modify_Xzhq_FloodRes(xzhq_floodres);
            return xzhq_res;
        }

        //去掉前面的时间序列和保留小数
        public static void Modify_Dic_Res(ref Dictionary<DateTime, double> old_dic, int ahead_hours,int ws)
        {
            if (old_dic != null)
            {
                Remove_Ahead_Res(ref old_dic,ahead_hours);
                old_dic = old_dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, ws));
            }
        }

        //修改将15个蓄滞洪区结果合并成9个
        public static Dictionary<string, Xzhq_FloodRes> Modify_Xzhq_FloodRes(Dictionary<string, Xzhq_FloodRes> xzhq_floodres)
        {
            Dictionary<string, Xzhq_FloodRes> xzhq_res = new Dictionary<string, Xzhq_FloodRes>();
            List<Xzhq_Info> xzhq_list = WG_INFO.Get_XzhqInfo();
            List<Xzhq_Info> father_xzhq_list = Xzhq_Info.Get_Father_List(xzhq_list);
            Dictionary<string, List<string>> son_xzhq = Xzhq_Info.Get_Son_List(xzhq_list);

            for (int i = 0; i < father_xzhq_list.Count; i++)
            {
                string name = father_xzhq_list[i].Name;
                string stcd = father_xzhq_list[i].Stcd;
                Xzhq_FloodRes res = xzhq_floodres[name];
                Xzhq_Info xzhq_info = father_xzhq_list[i];

                if (son_xzhq.Keys.Contains(stcd))
                {
                    List<string> son_list = son_xzhq[stcd];
                    Combine_XzhqRes(ref res, xzhq_info, xzhq_floodres, son_list);
                }

                xzhq_res.Add(name, res);
            }

            return xzhq_res;
        }

        //合并蓄滞洪区结果
        public static void Combine_XzhqRes(ref Xzhq_FloodRes xzhq_res, Xzhq_Info xzhq_info, Dictionary<string, Xzhq_FloodRes> xzhq_floodres, List<string> son_list)
        {
            Xzhq_FloodRes first_son_res = xzhq_floodres[son_list[0]];
            string start_time = xzhq_floodres[son_list[0]].Start_FloodTime;
            if ((start_time == "" || start_time == "0001/01/01 00:00:00") && son_list.Count > 1) start_time = xzhq_floodres[son_list[1]].Start_FloodTime;
            double max_volumn = 0; string max_level = ""; double max_h = 0; double max_area = 0; double max_floodtime = 0;
            string maxvolumn_time = ""; string state = "未启用";
            List<string> son_statelist = new List<string>();
            Dictionary<DateTime, string> xzhq_state_dic = new Dictionary<DateTime, string>();
            Dictionary<string, double> max_fhyq = new Dictionary<string, double>();
            Dictionary<DateTime, double> volumn_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> area_dic = new Dictionary<DateTime, double>();
            Dictionary<string, double> design_volumn = new Dictionary<string, double>();
            for (int i = 0; i < son_list.Count; i++)
            {
                Xzhq_FloodRes son_res = xzhq_floodres[son_list[i]];
                if (son_res.Max_FloodTime > max_floodtime) max_floodtime = son_res.Max_FloodTime;

                son_statelist.Add(son_res.Xzhq_State);

                Dictionary<DateTime, string> son_state_dic = son_res.Xzhq_State_Dic;
                Modify_XzhqState_Dic(ref xzhq_state_dic, son_state_dic);

                string son_maxvolumn_time = son_res.MaxVolumn_Time;
                if (son_maxvolumn_time != "")
                {
                    if (maxvolumn_time == "")
                    {
                        maxvolumn_time = son_maxvolumn_time;
                    }
                    else
                    {
                        DateTime son_max_time = SimulateTime.StrToTime(son_maxvolumn_time);
                        DateTime now_max_time = SimulateTime.StrToTime(maxvolumn_time);
                        if (son_max_time.CompareTo(now_max_time) > 0) maxvolumn_time = son_maxvolumn_time;
                    }
                }

                max_volumn += son_res.Max_Volumn;
                max_level = max_level == "" ? son_res.Max_Level : max_level + "/" + son_res.Max_Level;
                max_area += son_res.Max_Area;
                if (son_res.Max_Depth >= max_h) max_h = son_res.Max_Depth;

                Dictionary<string, double> fh_maxq = son_res.Max_FhyInQ;
                for (int j = 0; j < fh_maxq.Count; j++)
                {
                    string fhy_name = fh_maxq.ElementAt(j).Key;
                    if (!max_fhyq.Keys.Contains(fhy_name)) max_fhyq.Add(fhy_name, fh_maxq[fhy_name]);
                }

                Dictionary<DateTime, double> son_volumn_dic = son_res.Vomumn_Dic;
                if (volumn_dic.Count == 0)
                {
                    volumn_dic = son_volumn_dic;
                }
                else
                {
                    volumn_dic = Dfs0.Combine_Dic(volumn_dic, son_volumn_dic);
                }

                Dictionary<DateTime, double> son_area_dic = son_res.Area_Dic;
                if (area_dic.Count == 0)
                {
                    area_dic = son_area_dic;
                }
                else
                {
                    area_dic = Dfs0.Combine_Dic(area_dic, son_area_dic);
                }
            }

            bool all_is_csj = true;
            for (int i = 0; i < son_statelist.Count; i++)
            {
                if(son_statelist[i] != "超设计")
                {
                    all_is_csj = false;
                    break;
                }
            }
            if (all_is_csj)
            {
                state = "超设计";
            }
            else if(son_statelist.Contains("超设计") || son_statelist.Contains("启用"))
            {
                state = "启用";
            }
            else
            {
                state = "未启用";
            }

            xzhq_res.Start_FloodTime = start_time;
            xzhq_res.Xzhq_State = state;
            xzhq_res.Max_Volumn = max_volumn;
            xzhq_res.Max_Level = max_level;
            xzhq_res.Max_Depth = max_h;
            xzhq_res.Max_Area = Math.Round(max_area, 2);
            xzhq_res.Max_FloodTime = max_floodtime;
            xzhq_res.MaxVolumn_Time = maxvolumn_time;
            xzhq_res.Max_FhyInQ = max_fhyq;
            xzhq_res.Vomumn_Dic = volumn_dic;
            xzhq_res.Area_Dic = area_dic;
            xzhq_res.Xzhq_State_Dic = xzhq_state_dic;
        }

        private static void Modify_XzhqState_Dic(ref Dictionary<DateTime, string> xzhq_state_dic, Dictionary<DateTime, string> son_state_dic)
        {
            if(xzhq_state_dic.Count == 0)
            {
                xzhq_state_dic = son_state_dic;
            }
            else
            {
                for (int i = 0; i < xzhq_state_dic.Count; i++)
                {
                    DateTime time = xzhq_state_dic.ElementAt(i).Key;
                    string state = Combine_XzhqState(xzhq_state_dic.ElementAt(i).Value, son_state_dic[time]);
                    xzhq_state_dic[time] = state;
                }
            }
        }

        private static string Combine_XzhqState(string state, string son_state)
        {
            string res = state;

            if (state == "未启用")
            {
                if (son_state == "超设计")
                {
                    res = "超设计";
                }
                else if (son_state == "启用")
                {
                    res = "启用";
                }
            }
            else if (state == "启用")
            {
                if (son_state == "超设计") res = "超设计";
            }

            return res;
        }
    }
}
