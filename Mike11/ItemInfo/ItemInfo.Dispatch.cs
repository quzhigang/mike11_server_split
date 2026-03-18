using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using bjd_model.Common;
using bjd_model.Const_Global;
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
        #region *********************获取和更新闸站调度*********************
        //从模型中获取建筑物调度信息(初始化默认模型时使用)
        public static string Get_Model_DdInfo(HydroModel hydromodel)
        {
            List<string[]> modeldd_infostr = Get_Model_DdInfoArray(hydromodel);

            string model_ddinfo = File_Common.Serializer_Obj(modeldd_infostr);
            return model_ddinfo;
        }

        //从模型中获取建筑物调度信息
        public static List<string[]> Get_Model_DdInfoArray(HydroModel hydromodel)
        {
            List<string[]> modeldd_infostr = new List<string[]>();

            //从默认模型中获取建筑物调度信息(初始化默认模型时使用)
            List<Str_DdInfo> gatedd_info = Get_ModelGatedd_Info(hydromodel);

            //分类排序 -- 按河道和建筑物(节制闸、退水闸、泵站、分水闸)分类
            List<ReachInfo> reach_info = hydromodel.Mike11Pars.ReachList.Reach_infolist;
            List<Str_DdInfo> new_gatedd_info = Get_Order_Str(gatedd_info);

            //调度信息
            for (int i = 0; i < new_gatedd_info.Count; i++)
            {
                string str_name = new_gatedd_info[i].str_name;
                Struct_BasePars str_basepar = Item_Info.Get_StrBaseInfo(str_name);

                //建筑物中文名
                string str_cnname = str_basepar.cn_name;

                //类型中文名
                string str_type = str_basepar.str_type;

                //所在河道中文名
                string reach_cn_name = str_basepar.reach_cnname;

                //获取调度方式中文名、闸门及值
                string strdd_cn_name = new_gatedd_info[i].Get_DdType_CNName();
                int open_n = new_gatedd_info[i].open_n;
                double value = Math.Round(new_gatedd_info[i].dd_value, 3); //保留3位小数

                //调度
                List<string[]> dd_info = new List<string[]>();
                string[] gate_dd = new string[] { "", strdd_cn_name.ToString(), open_n.ToString(), value.ToString() };
                dd_info.Add(gate_dd);

                string dd_str = File_Common.Serializer_Obj(dd_info);
                string[] dd_strs = { str_name, (i + 1).ToString(), str_cnname, str_type, reach_cn_name, dd_str };
                modeldd_infostr.Add(dd_strs);
            }

            return modeldd_infostr;
        }

        //从模型中获取建筑物调度信息
        public static List<Str_DdInfo> Get_ModelGatedd_Info(HydroModel hydromodel)
        {
            List<Str_DdInfo> modeldd_info = new List<Str_DdInfo>();

            //从建筑物集合中获取模型模型调度信息
            List<Normalstr> gate_list = Normalstr.Get_Model_Normalstr(hydromodel);
            for (int i = 0; i < gate_list.Count; i++)
            {
                //获取建筑物调度方式(全开、全关、指定开度、指定流量)、开闸数量和调度值
                int open_n = gate_list[i].Ddparams_zmdu.opengatenumber;        //开闸数量
                double dd_value = 0;   //调度值

                //判断建筑物调度方式
                Str_DdType str_ddtype = gate_list[i].Strddgz == CtrDdtype.GZDU ? Str_DdType.Rule : Get_Str_DdType(gate_list[i], ref dd_value);

                //建筑物调度信息
                Str_DdInfo str_ddinfo = new Str_DdInfo(gate_list[i].Strname, gate_list[i].Reachinfo, str_ddtype, open_n, dd_value);

                if (str_ddtype == Str_DdType.Open)
                {
                    str_ddinfo.open_n = gate_list[i].Stratts.gate_count;
                    str_ddinfo.dd_value = gate_list[i].Stratts.gate_height;
                }
                else if (str_ddtype == Str_DdType.Close)
                {
                    str_ddinfo.open_n = 0;
                    str_ddinfo.dd_value = 0;
                }
                else if (str_ddtype == Str_DdType.Set_Q)
                {
                    str_ddinfo.open_n = 0;
                }

                modeldd_info.Add(str_ddinfo);
            }
            return modeldd_info;
        }

        //判断建筑物调度方式
        public static Str_DdType Get_Str_DdType(Normalstr str, ref double dd_value)
        {
            Str_DdType str_ddtype;
            if (str.Stratts.gate_type == 2)   //设置流量
            {
                str_ddtype = Str_DdType.Set_Q;
                dd_value = Math.Max(str.Ddparams_double, str.Stratts.max_value);
            }
            else   //其他闸门类型
            {
                if (str.Ddparams_zmdu.opengatenumber == 0)
                {
                    str_ddtype = Str_DdType.Close;
                }
                else if (str.Ddparams_zmdu.fullyopen == true)
                {
                    str_ddtype = Str_DdType.Open;
                }
                else
                {
                    str_ddtype = Str_DdType.Set_H;
                    dd_value = str.Ddparams_zmdu.opengateheight - str.Stratts.sill_level;
                }
            }

            return str_ddtype;
        }

        //建筑物按桩号排序
        public static List<Str_DdInfo> Get_Order_Str(List<Str_DdInfo> gatedd_info)
        {
            List<Str_DdInfo> new_gatedd_info = new List<Str_DdInfo>();

            //分类
            List<Str_DdInfo> jzz_gate = new List<Str_DdInfo>();
            List<Str_DdInfo> tsz_gate = new List<Str_DdInfo>();
            List<Str_DdInfo> bz_gate = new List<Str_DdInfo>();
            List<Str_DdInfo> fsk_gate = new List<Str_DdInfo>();
            for (int i = 0; i < gatedd_info.Count; i++)
            {
                Struct_BasePars str_par = Item_Info.Get_StrBaseInfo(gatedd_info[i].str_name);
                if (str_par.gate_type == GateType.YLZ) jzz_gate.Add(gatedd_info[i]);
                if (str_par.gate_type == GateType.PBZ) tsz_gate.Add(gatedd_info[i]);
                if (str_par.gate_type == GateType.BZ) bz_gate.Add(gatedd_info[i]);
                if (str_par.gate_type == GateType.LLZ) fsk_gate.Add(gatedd_info[i]);
            }

            //按桩号排序
            //jzz_gate = jzz_gate.OrderBy(p => p.atreach.reachchainage).ToList();  //节制闸不排序了
            tsz_gate = tsz_gate.OrderBy(p => p.atreach.chainage).ToList();
            bz_gate = bz_gate.OrderBy(p => p.atreach.chainage).ToList();
            fsk_gate = fsk_gate.OrderBy(p => p.atreach.chainage).ToList();

            //集合在一起
            new_gatedd_info.AddRange(jzz_gate);
            new_gatedd_info.AddRange(tsz_gate);
            new_gatedd_info.AddRange(bz_gate);
            new_gatedd_info.AddRange(fsk_gate);

            return new_gatedd_info;
        }

        //获取该方案水库、河道、蓄滞洪区 简短调度指令
        public static string Get_Model_DispatchPlan_Fromdb(string plan_code)
        {
            //定义连接的数据库和表
            string moel_instance = Mysql_GlobalVar.now_instance;
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select mike11_strdd_info from " + tableName +
                " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //从数据库获取该方案 建筑物调度信息
            string strddinfo_str = dt.Rows[0][0].ToString();
            List<string[]> dd_list = JsonConvert.DeserializeObject<List<string[]>>(strddinfo_str);

            //获取建筑物基本信息
            Dictionary<string, Struct_BasePars> str_baseinfo = Item_Info.Get_StrBaseInfo();

            //获取模型统计data结果
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_TjResult(plan_code);

            //获取水库调度指令
            Dictionary<string, Reservoir_FloodRes> reservoir_result = model_result["reservoir_result"] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, Dispatch_Info> reservoir_ddplan = Get_ResGate_DispatchPlan(str_baseinfo, dd_list,reservoir_result);

            //获取河道节点控制闸站调度指令
            Dictionary<string, ReachSection_FloodRes> reach_station_res = model_result["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Dispatch_Info> reachgate_ddplan = Get_ReachGate_DispatchPlan(str_baseinfo,dd_list, reach_station_res);

            //获取蓄滞洪区调度指令
            Dictionary<string, Xzhq_FloodRes> xzhq_res = model_result["floodblq_result"] as Dictionary<string, Xzhq_FloodRes>;
            Dictionary<string, Dispatch_Info> xzhq_ddplan = Get_Xzhq_DispatchPlan(xzhq_res);

            //各类型调度组合
            Dictionary<string, object> model_ddplan = new Dictionary<string, object>();
            model_ddplan.Add("reservoir_dispatch", reservoir_ddplan);
            model_ddplan.Add("reach_dispatch", reachgate_ddplan);
            model_ddplan.Add("xzhq_dispatch", xzhq_ddplan);

            return File_Common.Serializer_Obj(model_ddplan);
        }

        //获取水库的调度方案指令
        public static Dictionary<string, Dispatch_Info> Get_ResGate_DispatchPlan(Dictionary<string, Struct_BasePars> str_baseinfo,
            List<string[]> dd_list, Dictionary<string, Reservoir_FloodRes> reservoir_result)
        {
            Dictionary<string, Dispatch_Info> res_dd_plan = new Dictionary<string, Dispatch_Info>();
            for (int i = 0; i < reservoir_result.Count; i++)
            {
                string res_name = reservoir_result.ElementAt(i).Key;
                Reservoir_FloodRes reservoir_res = reservoir_result.ElementAt(i).Value;
                DateTime start_time = reservoir_res.Level_Dic.ElementAt(0).Key;

                //获取该水库泄洪建筑集合
                List<Struct_BasePars> res_strlist = Item_Info.Get_Res_All_StrInfo(res_name);

                //只保留溢洪道可以控制的水库
                if (!Struct_BasePars.Have_GateYhd(res_strlist)) continue;

                //判断水库的是否为规程调度(各建筑均为规程调度)
                bool res_isgzdd = Res_IsGzdd(dd_list, res_strlist);
                string res_dispatch_type = res_isgzdd ? "规程调度": "指令调度";

                //水库调度方案和 水库各泄洪建筑调度指令
                string res_dispatch_plan;
                Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();
                double max_outq = Math.Round(reservoir_res.Max_OutQ);
                DateTime flood_outtime = reservoir_res.OutQ_Dic.Values.Max() == 0? start_time:reservoir_res.OutQ_Dic.First(kvp => kvp.Value != 0).Key;
                if (max_outq < 10)
                {
                    //水库工程调度方案
                    res_dispatch_plan = $"{res_dispatch_type}，水库各建筑不泄洪";

                    //水库工程的 各建筑物调度指令
                    for (int j = 0; j < res_strlist.Count; j++)
                    {
                        gate_dispatch.Add(res_strlist[j].cn_name, new List<string>() { $"{start_time}开始，全程关闭不泄洪" });
                    }
                }
                else
                {
                    //水库工程调度方案
                    double max_outq1 = Math.Round(max_outq / 10) * 10;
                    res_dispatch_plan = $"{res_dispatch_type}，控制水库下泄流量不超{max_outq1}m³/s";

                    //水库泄洪洞设计过流能力
                    double res_xhq_q = 0;
                    for (int j = 0; j < res_strlist.Count; j++)
                    {
                        if (res_strlist[j].str_type == "泄洪洞") res_xhq_q += res_strlist[j].design_q;
                    }

                    //获取各水库建筑的调度指令
                    gate_dispatch = Get_Res_StructDDinfo(res_strlist, max_outq, res_xhq_q, start_time, flood_outtime);
                }

                //工程和闸门调度指令
                Dispatch_Info dispatch_info_obj = new Dispatch_Info(res_dispatch_plan, gate_dispatch, reservoir_res.Stcd);
                res_dd_plan.Add(res_name, dispatch_info_obj);
            }

            return res_dd_plan;
        }

        //判断水库的是否为规程调度(各建筑均为规程调度)
        private static bool Res_IsGzdd(List<string[]> dd_list, List<Struct_BasePars> res_strlist)
        {
            //判断是规程调度还是指令调度
            bool res_isgzdd = true;
            for (int j = 0; j < res_strlist.Count; j++)
            {
                for (int k = 0; k < dd_list.Count; k++)
                {
                    string dd_strname = dd_list[k][0];
                    List<string[]> str_dd_list = JsonConvert.DeserializeObject<List<string[]>>(dd_list[k][5]);
                    string dd_type = str_dd_list[0][1];
                    if (dd_type != "规则")
                    {
                        res_isgzdd = false; break;
                    }
                }
            }
            return res_isgzdd;
        }

        //获取各水库建筑的调度指令
        public static Dictionary<string,List<string>> Get_Res_StructDDinfo(List<Struct_BasePars> res_strlist,
            double max_outq,double res_xhq_q, DateTime start_time,DateTime flood_outtime)
        {
            Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();

            //水库工程的 各建筑物调度指令
            double last_design_q = 0;
            for (int j = 0; j < res_strlist.Count; j++)
            {
                double design_q = res_strlist[j].design_q;
                DateTime str_ddtime = start_time;

                string gate_dd_info = "";
                if (res_strlist[j].str_type == "泄洪洞")
                {
                    double min_q = Math.Round(Math.Min(design_q, max_outq) / 10) * 10;
                    if (last_design_q == 0)
                    {
                        gate_dd_info = $"控制下泄流量不超{min_q}m³/s";
                    }
                    else
                    {
                        double design_last_q = Math.Round((design_q - last_design_q) / 10) * 10;
                        gate_dd_info = last_design_q >= max_outq ? "泄洪洞全程关闭": $"控制下泄流量不超{design_last_q}m³/s";
                    }

                    if (gate_dd_info.Contains("m³/s")) str_ddtime = flood_outtime;
                    last_design_q = design_q;
                }
                else if (res_strlist[j].str_type == "溢洪道")
                {
                    if (res_strlist[j].gate_type != GateType.NOGATE)
                    {
                        if (res_xhq_q - max_outq >= -1)
                        {
                            gate_dd_info = $"溢洪道全程关闭";
                        }
                        else
                        {
                            double yhd_q = Math.Round((max_outq - res_xhq_q) / 10) * 10;
                            gate_dd_info = $"控制下泄流量不超{yhd_q}m³/s";
                        }
                    }
                    else
                    {
                        if (res_xhq_q - max_outq >= -1)
                        {
                            gate_dd_info = $"溢洪道全程不溢流";
                        }
                        else
                        {
                            double yhd_q = Math.Round((max_outq - res_xhq_q) / 10) * 10;
                            gate_dd_info = $"溢洪道自由溢流最大流量{yhd_q}m³/s";
                        }
                    }

                    if (gate_dd_info.Contains("m³/s")) str_ddtime = flood_outtime;
                }

                gate_dispatch.Add(res_strlist[j].cn_name, new List<string>() { $"{str_ddtime}开始, {gate_dd_info}" });
            }

            return gate_dispatch;
        }

        //获取河道节点控制闸站调度方案指令
        public static Dictionary<string, Dispatch_Info> Get_ReachGate_DispatchPlan(Dictionary<string, Struct_BasePars> str_baseinfo,
            List<string[]> dd_list, Dictionary<string, ReachSection_FloodRes> reach_station_res)
        {
            Dictionary<string, Dispatch_Info> reachgate_dd_plan = new Dictionary<string, Dispatch_Info>();

            //获取河道主要节点控制建筑
            Dictionary<string, List<string>> main_str = Get_Main_ControlStr();
            List<string> reach_main_gate = main_str["reach_main_gate"];
            DateTime start_time = reach_station_res.ElementAt(0).Value.Discharge_Dic.ElementAt(0).Key;

            //各个节点河道闸站调度方案指令
            for (int i = 0; i < reach_main_gate.Count; i++)
            {
                Struct_BasePars gate_baseinfo = str_baseinfo[reach_main_gate[i]];
                double max_q = reach_station_res.Keys.Contains(gate_baseinfo.cn_name) ? reach_station_res[gate_baseinfo.cn_name].Max_Qischarge : 0;
                max_q = Math.Round(max_q/10)*10;
                Dictionary<DateTime,double> gate_qdic = reach_station_res.Keys.Contains(gate_baseinfo.cn_name) ? reach_station_res[gate_baseinfo.cn_name].Discharge_Dic: null;
                string str_stcd = reach_station_res.Keys.Contains(gate_baseinfo.cn_name) ? reach_station_res[gate_baseinfo.cn_name].Stcd:"";

                //获取该建筑物的调度方式
                Dispatch_Info dispatch_info_obj = new Dispatch_Info();
                dispatch_info_obj.stcd = str_stcd;

                //调度方案
                string dispatch_plan = "";

                //建筑物调度详情
                Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();
                for (int j = 0; j < dd_list.Count; j++)
                {
                    string dd_strname = dd_list[j][0];

                    if(dd_strname == reach_main_gate[i])
                    {
                        List<string[]> str_dd_list = JsonConvert.DeserializeObject<List<string[]>>(dd_list[j][5]);
                        string str_dispatch_type = str_dd_list[0][1] == "规则"? "规程调度" : "指令调度";
                        if (max_q < 10)
                        {
                            dispatch_plan = $"{str_dispatch_type}，闸门全程关闭";
                        }
                        else
                        {
                            dispatch_plan = $"{str_dispatch_type}，控制过闸流量不超{max_q}m³/s";
                        }

                        //获取单个河道控制建筑物调度指令
                        List<string> str_dispatch = Get_Gate_DispatchPlan(dd_list[j], gate_baseinfo, max_q, gate_qdic, dd_strname, start_time);

                        gate_dispatch.Add(gate_baseinfo.cn_name, str_dispatch);
                        dispatch_info_obj.gate_dispatch = gate_dispatch;
                        break;
                    }
                }

                dispatch_info_obj.dispatch_plan = dispatch_plan;
                reachgate_dd_plan.Add(gate_baseinfo.cn_name, dispatch_info_obj);
            }

            return reachgate_dd_plan;
        }

        //获取单个河道控制建筑物调度指令
        private static List<string> Get_Gate_DispatchPlan(string[] dd_list, Struct_BasePars gate_baseinfo, double max_q,
            Dictionary<DateTime, double> gate_qdic, string dd_strname,DateTime start_time)
        {
            List<string[]> str_dd_list = JsonConvert.DeserializeObject<List<string[]>>(dd_list[5]);
            string dd_type = str_dd_list[0][1];
            string dd_n = str_dd_list[0][2];
            string dd_value = str_dd_list[0][3];

            //建筑物调度方案指令
            List<string> str_dispatch = new List<string>();
            if (dd_type == "规则")
            {
                if (max_q < 10)
                {
                    string str_ddmessage = $"{start_time}开始，闸门全程关闭";
                    str_dispatch.Add(str_ddmessage);
                }
                else
                {
                    if (dd_strname == "QHNZ_XHKJZZ")  //小河口闸
                    {
                        string cx_start_time = gate_qdic == null ? "" : SimulateTime.TimetoStr(gate_qdic.FirstOrDefault(x => x.Value > 400).Key);
                        string close_start_time = gate_qdic == null ? "" : SimulateTime.TimetoStr(gate_qdic.LastOrDefault(x => x.Value > 10).Key);
                        string str_ddmessage1 = $"1、{start_time}开始，闸门控泄流量不超400m³/s";
                        string str_ddmessage2 = $"2、{cx_start_time}开始,闸门全开敞泄";
                        string str_ddmessage3 = $"3、{close_start_time}开始,闸门全闭";
                        str_dispatch.Add(str_ddmessage1);
                        str_dispatch.Add(str_ddmessage2);
                        str_dispatch.Add(str_ddmessage3);
                    }
                    else if (dd_strname == "GQ_YTZJZZ")  //盐土庄闸
                    {
                        string str_ddmessage = $"{start_time}开始，控制下泄流量不超{gate_baseinfo.design_q}m³/s";
                        str_dispatch.Add(str_ddmessage);
                    }
                    else
                    {
                        string str_ddmessage = gate_baseinfo.ddrule_info;
                        str_dispatch.Add(str_ddmessage);
                    }
                }
            }
            else if (dd_type == "全开")
            {
                string str_ddmessage = $"{start_time}开始，全程{gate_baseinfo.gate_h}孔闸门全开";
                str_dispatch.Add(str_ddmessage);
            }
            else if (dd_type == "全关")
            {
                string str_ddmessage = $"{start_time}开始，闸门全程关闭";
                str_dispatch.Add(str_ddmessage);
            }
            else if (dd_type == "控泄")
            {
                string str_ddmessage = $"{start_time}开始，全程控制下泄流量不超{dd_value}m³/s";
                str_dispatch.Add(str_ddmessage);
            }
            else if (dd_type == "时间")
            {
                for (int k = 0; k < str_dd_list.Count; k++)
                {
                    string ddtime = str_dd_list[k][0];
                    string ddtype = str_dd_list[k][1];
                    string ddn = str_dd_list[k][2];
                    string ddvalue = str_dd_list[k][3];

                    string str_ddmessage = $"{k + 1}、{ddtime},调度方式:{ddtype},调度值:{ddvalue}";
                    str_dispatch.Add(str_ddmessage);
                }
            }
            else if (dd_type == "半开")  //全程半开
            {
                string str_ddmessage = $"{start_time}开始，全程{dd_n}孔闸门半开,平均开度{dd_value}m";
                str_dispatch.Add(str_ddmessage);
            }

            return str_dispatch;
        }

        //获取蓄滞洪区调度指令
        public static Dictionary<string, Dispatch_Info> Get_Xzhq_DispatchPlan(Dictionary<string, Xzhq_FloodRes> xzhq_res)
        {
            Dictionary<string, Dispatch_Info> xzhq_dd_plan = new Dictionary<string, Dispatch_Info>();
            for (int i = 0; i < xzhq_res.Count; i++)
            {
                string xzhq_name = xzhq_res.ElementAt(i).Key;
                Xzhq_FloodRes res = xzhq_res.ElementAt(i).Value;

                //蓄滞洪区调度指令和各建筑物调度指令
                string dispatch_plan;
                Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();
                List<string> fhy_list = res.Max_FhyInQ.Keys.ToList();
                if (res.Xzhq_State == "未启用")
                {
                    //蓄滞洪区调度方案
                    dispatch_plan = "蓄滞洪区不启用";

                    //蓄滞洪区工程的 各建筑物调度指令
                    for (int j = 0; j < fhy_list.Count; j++)
                    {
                        gate_dispatch.Add(fhy_list[j], new List<string>() {"不启用"});
                    }
                }
                else
                {
                    //蓄滞洪区工程调度指令
                    double fhy_q_sum = res.Max_FhyInQ.Values.Sum();
                    string jh_type = fhy_q_sum == 0 ? "河道漫溢": "分洪闸堰";
                    dispatch_plan = $"{res.Start_FloodTime},通过{jh_type}启用蓄滞洪区";

                    //蓄滞洪区工程的 各建筑物调度指令
                    Dictionary<string, Dictionary<DateTime, double>> allstr_inq = res.InQ_Dic;
                    for (int j = 0; j < fhy_list.Count; j++)
                    {
                        List<string> fhydd_list = new List<string>();

                        string fhy_ddinfo;
                        double max_q = res.Max_FhyInQ[fhy_list[j]];
                        if(max_q == 0)
                        {
                            fhy_ddinfo = $"全过程不分洪";
                        }
                        else
                        {
                            Dictionary<DateTime, double> q_list = allstr_inq.Keys.Contains(fhy_list[j]) ? allstr_inq[fhy_list[j]] : null;
                            string start_time = q_list == null ? res.Start_FloodTime : SimulateTime.TimetoStr(q_list.FirstOrDefault(x => x.Value > 0).Key);
                            fhy_ddinfo = $"{start_time},启用分洪";
                        }
                        fhydd_list.Add(fhy_ddinfo);
                        gate_dispatch.Add(fhy_list[j], fhydd_list);
                    }
                }

                //工程和闸门调度指令
                Dispatch_Info dispatch_info_obj = new Dispatch_Info(dispatch_plan, gate_dispatch, res.Stcd);
                xzhq_dd_plan.Add(xzhq_name, dispatch_info_obj);
            }

            return xzhq_dd_plan;
        }

        //分类获取主要控制建筑
        public static Dictionary<string, List<string>> Get_Main_ControlStr()
        {
            Dictionary<string, List<string>> main_str = new Dictionary<string, List<string>>();

            //获取建筑物基本信息
            Dictionary<string, Struct_BasePars> str_info = Item_Info.Get_StrBaseInfo();

            //分类获取建筑物，键为水库、河道、蓄滞洪区
            List<string> res_str = new List<string>();
            List<string> reach_str = new List<string>();
            List<string> xzhq_str = new List<string>();
            List<string> other_str = new List<string>();
            for (int i = 0; i < str_info.Count; i++)
            {
                string str_name = str_info.ElementAt(i).Key;
                Struct_BasePars str = str_info.ElementAt(i).Value;
                if (str.control_str != "")
                {
                    //主要控制闸门 -- 水库
                    if (str.str_type == "溢洪道" || str.str_type == "泄洪洞") res_str.Add(str_name);

                    //主要控制闸门 -- 河道闸站
                    if (str.str_type == "拦河闸" || str.str_type == "退水闸") reach_str.Add(str_name);

                    //主要控制闸门 -- 蓄滞洪区
                    if (str.str_type == "分洪闸" || str.str_type == "分洪堰") xzhq_str.Add(str_name);
                }
                else
                {
                    //其他非控制闸门
                    other_str.Add(str_name);
                }
            }

            main_str.Add("main_res_str", res_str);
            main_str.Add("reach_main_gate", reach_str);
            main_str.Add("main_xzhq_str", xzhq_str);
            main_str.Add("other_str", other_str);

            return main_str;
        }

        //更新所有闸门的调度(每个建筑物4个参数 -- 英文名、调度方式、开闸数量、调度量)
        public static void Update_AllStr_DdInfo(ref HydroModel hydromodel, Dictionary<string, List<DdInfo>> new_ddinfos)
        {
            //逐一修改闸门调度方式
            for (int i = 0; i < new_ddinfos.Count; i++)
            {
                string strname = new_ddinfos.ElementAt(i).Key;
                List<DdInfo> gate_dds = new_ddinfos.ElementAt(i).Value;

                //修改该闸门调度方式
                if(gate_dds.Count == 1 && gate_dds[0].dd_time == "")   
                {
                    //只有一个调度方式，从头到尾全过程一样
                    Change_Str_Zldd(ref hydromodel, strname, gate_dds[0].dd_type, gate_dds[0].open_n, gate_dds[0].dd_value);
                }
                else
                {
                    //按时间调度闸门 (开闸关闸所需时间按默认)
                    Nwk11.Changeddgz_ToZLDD_TIME(ref hydromodel, strname, gate_dds);
                }
            }
        }

        //刷选出需要修改的这些闸站原始调度信息
        public static List<string[]> FilterData(List<string[]> source_ddinfo, List<string[]> new_ddinfos)
        {
            List<string[]> filteredData = new List<string[]>();
            for (int i = 0; i < source_ddinfo.Count; i++)
            {
                string firstString = source_ddinfo[i][0];
                for (int j = 0; j < new_ddinfos.Count; j++)
                {
                    if (firstString == new_ddinfos[j][0])
                    {
                        filteredData.Add(source_ddinfo[i]);
                        break;
                    }
                }
            }
            return filteredData;
        }

        //更新单个闸门的调度(4个参数 -- 英文名、调度方式、开闸数量、调度量)
        public static void Update_Str_DdInfo(ref HydroModel hydromodel, string[] new_ddinfo)
        {
            if (new_ddinfo.Length != 3) return;

            int open_n = 0;
            if (new_ddinfo[2] != "" && new_ddinfo[2] != null) open_n = int.Parse(new_ddinfo[2]);

            double dd_value = 0;
            if (new_ddinfo[3] != "" && new_ddinfo[3] != null) dd_value = double.Parse(new_ddinfo[3]);

            //修改该闸门调度方式
            Change_Str_Zldd(ref hydromodel, new_ddinfo[0], new_ddinfo[1], open_n, dd_value);
        }

        //根据数据库闸站状态，修改所有的建筑物闸站调度方式及数据
        public static void Update_AllStr_DdInfo_FromGB(ref HydroModel hydromodel)
        {
            //从数据库获取当前闸门状态
            List<Gate_StateInfo> gate_state = Item_Info.Read_NowGateState();

            //逐一修改修改闸门调度方式
            Dictionary<string, int> str_baseinfo = hydromodel.Mike11Pars.ControlstrList.Gatebaseinfo;
            for (int i = 0; i < gate_state.Count; i++)
            {
                string strname = gate_state[i].str_name;
                if (!str_baseinfo.Keys.Contains(strname)) continue;  //建筑物基本信息表里有mike11可控建筑物没有的建筑物
                string cn_ddfs = gate_state[i].nowState;
                double dd_value = gate_state[i].openHQ;
                int open_n = gate_state[i].openN;
                Change_Str_Zldd(ref hydromodel, strname, cn_ddfs, open_n, dd_value);
            }
        }

        //修改某闸门的调度方式(闸门状态全过程一样)
        public static void Change_Str_Zldd(ref HydroModel hydromodel, string strname, string str_ddtype, int open_n = 0, double dd_value = 0)
        {
            //获取该闸门的基本信息
            Struct_BasePars str_basepar = Get_StrBaseInfo(strname);

            //判断
            Str_DdType dd_type = Str_DdInfo.Get_DdType(str_ddtype);
            switch (dd_type)
            {
                case Str_DdType.Open:
                    if(str_basepar.gate_type == GateType.LLZ || str_basepar.gate_type == GateType.BZ)
                    {
                        //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
                        Change_Tokxdd(ref hydromodel, strname, str_basepar.design_q);
                    }
                    else
                    {
                        Nwk11.Changeddgz_ToZMDU_FullOpen(ref hydromodel, strname);
                    }
                    break;
                case Str_DdType.Close:
                    if (str_basepar.gate_type == GateType.LLZ || str_basepar.gate_type == GateType.BZ)
                    {
                        //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
                        Change_Tokxdd(ref hydromodel, strname, 0);
                    }
                    else
                    {
                        Nwk11.Changeddgz_ToZMDU_FullClose(ref hydromodel, strname);
                    }
                    break;
                case Str_DdType.Set_H:
                    Nwk11.Changeddgz_ToZMDU_SetH(ref hydromodel, strname, str_basepar.gate_type, dd_value, open_n);

                    //在原溢流闸门处增加一个新的溢流闸门，闸门数量为未打开闸门数,状态为全关(溢流类型的闸门全关后水流依然能从上面溢流)
                    if (str_basepar.gate_type == GateType.YLZ && hydromodel.Mike11Pars.ControlstrList.GetGate(strname).Stratts.gate_count != open_n)
                    {
                        Nwk11.Add_WeirGate_SyGaten(ref hydromodel, strname, open_n);
                    }
                    break;
                case Str_DdType.Set_Q:
                    //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
                    Change_Tokxdd(ref hydromodel, strname,dd_value);
                    break;
                case Str_DdType.Rule:
                    //按规则调度
                    Nwk11.Changeddgz_ToGZDU(ref hydromodel, strname);
                    break;
                default:
                    break;
            }
        }

        //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
        private static void Change_Tokxdd(ref HydroModel hydromodel, string strname, double dd_value)
        {
            //获取水位流量关系
            List<double[]> str_qh1 = Item_Info.Get_StrQHrelation(strname);

            if (str_qh1.Count != 0 && dd_value != 0)
            {
                Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, dd_value, str_qh1);
            }
            else
            {
                Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, dd_value);
            }
        }

        #endregion ***********************************************************


        #region ********************建筑物名称 类型 调度规则**********************
        //获取下一个最近的 指定类型 建筑物名称及其桩号
        public static void Get_NextNearStr(HydroModel hydromodel, AtReach atreach, GateType gatetype, out string near_strname, out double near_strchainage)
        {
            double neardistance = 1000000;
            near_strname = "";
            near_strchainage = 0.0;
            List<Controlstr> controlstr_list = hydromodel.Mike11Pars.ControlstrList.GateListInfo;

            //遍历所有可控建筑物，找到与给定类型相同的，且位于下游最近的一个
            for (int i = 0; i < controlstr_list.Count; i++)
            {
                if (controlstr_list[i].Strname.EndsWith(gatetype.ToString()))
                {
                    double str_chainage = 0.0;
                    string reachname = controlstr_list[i].Reachinfo.reachname;
                    ReachInfo reach_info = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);

                    //如果该建筑物不在给定河道上，但连接到该河道，则按连接桩号
                    if (reachname == atreach.reachname)
                    {
                        str_chainage = controlstr_list[i].Reachinfo.chainage;
                    }
                    else if (reach_info.upstream_connect.reachname == atreach.reachname)  //如果上游连接河道与给定河道相同也行
                    {
                        str_chainage = reach_info.upstream_connect.chainage;
                    }
                    else if (reach_info.downstream_connect.reachname == atreach.reachname)  //如果下游连接河道与给定河道相同也行
                    {
                        str_chainage = reach_info.downstream_connect.chainage;
                    }

                    if (str_chainage >= atreach.chainage)
                    {
                        double str_distance = str_chainage - atreach.chainage;
                        if (str_distance < neardistance)
                        {
                            near_strname = controlstr_list[i].Strname;
                            near_strchainage = str_chainage;
                            neardistance = str_distance;
                        }
                    }
                }
            }

        }

        //获取所有指定类型的建筑物，如节制闸、分水闸、退水闸
        public static Dictionary<string, AtReach> Get_TypeGate_FromModel(HydroModel model, GateType gate_type)
        {
            ControlList controllist = model.Mike11Pars.ControlstrList;
            Dictionary<string, AtReach> reachdic = new Dictionary<string, AtReach>();

            //获取指定类型建筑物(根据名称最后字符判断)
            for (int i = 0; i < controllist.GateListInfo.Count; i++)
            {
                if (controllist.GateListInfo[i].Strname.EndsWith(gate_type.ToString()))
                {
                    reachdic.Add(controllist.GateListInfo[i].Strname, controllist.GateListInfo[i].Reachinfo);
                }
            }
            return reachdic;
        }

        //从建筑物基本参数里 获取所有指定类型的建筑物，如节制闸、分水闸、退水闸
        public static Dictionary<string, AtReach> Get_TypeGate_FromStrBaseInfo(HydroModel model, GateType gate_type)
        {
            Dictionary<string, AtReach> reachdic = new Dictionary<string, AtReach>();
            if (gate_type == GateType.PBZ)
            {
                reachdic = Get_TypeGate_FromModel(model, GateType.PBZ);
                return reachdic;
            }

            //获取指定类型建筑物(根据名称最后字符判断)
            for (int i = 0; i < Item_Info.Str_BaseInfo.Count; i++)
            {
                if (Item_Info.Str_BaseInfo.ElementAt(i).Value.gate_type == gate_type)
                {
                    //获取名
                    string strname = Item_Info.Str_BaseInfo.ElementAt(i).Key;
                    AtReach atreach = AtReach.Get_Atreach(Item_Info.Str_BaseInfo.ElementAt(i).Value.reach_name, Item_Info.Str_BaseInfo.ElementAt(i).Value.chainage);
                    if (!reachdic.Keys.Contains(strname)) reachdic.Add(strname, atreach);
                }
            }
            return reachdic;
        }

        //从建筑物基本参数里 获取节制闸
        public static Dictionary<string, Dictionary<string, double>> Get_JZZ_FromStrBaseInfo()
        {
            Dictionary<string, Dictionary<string, double>> res = new Dictionary<string, Dictionary<string, double>>();

            //获取JZZ
            Dictionary<string, AtReach> reach_jzzdic = new Dictionary<string, AtReach>();
            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();
            for (int i = 0; i < Item_Info.Str_BaseInfo.Count; i++)
            {
                if (Item_Info.Str_BaseInfo.ElementAt(i).Value.gate_type == GateType.YLZ)
                {
                    //获取名
                    string strname = Item_Info.Str_BaseInfo.ElementAt(i).Key;
                    string str_cnname = Item_Info.Str_BaseInfo.ElementAt(i).Value.cn_name;
                    AtReach atreach = AtReach.Get_Atreach(Item_Info.Str_BaseInfo.ElementAt(i).Value.reach_name, Item_Info.Str_BaseInfo.ElementAt(i).Value.chainage);
                    if (!reach_jzzdic.Keys.Contains(strname)) reach_jzzdic.Add(str_cnname, atreach);
                }
            }

            //按河道分类

            List<Reach_BasePars> main_reach = Item_Info.Get_MainReach_Info(Mysql_GlobalVar.now_instance);
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reach_name = main_reach[i].name;
                Dictionary<string, double> jzz = new Dictionary<string, double>();
                for (int j = 0; j < reach_jzzdic.Count; j++)
                {
                    if (reach_jzzdic.ElementAt(j).Value.reachname == reach_name)
                    {
                        jzz.Add(reach_jzzdic.ElementAt(j).Key, reach_jzzdic.ElementAt(j).Value.chainage);
                    }
                }
                res.Add(reach_name, jzz);
            }

            return res;
        }

        //获取各类型闸门规则调度方式
        public static string Get_Gate_Rule(GateType gate_type)
        {
            switch (gate_type)
            {
                case GateType.YLZ:
                    return "闸门全程开启";
                case GateType.PBZ:
                    return "闸门全程关闭";
                case GateType.LLZ:
                    return "闸前水位大于最低运行水位时开启，否则关闭";
                case GateType.BZ:
                    return "闸前水位大于最低运行水位时开启，否则关闭";
                default:
                    return "";
            }
        }

        //获取闸门类型
        public static GateType Get_Gate_Type(string name)
        {
            Struct_BasePars str_par = Item_Info.Get_StrBaseInfo(name);

            return str_par.gate_type;
        }

        //获取模型模拟所需时间(秒)
        public static double Get_ModelRun_ElispedTime(double time_hours)
        {
            //计算时间步长按固定考虑
            double elasp_second = 10;
            if (time_hours <= 24)
            {
                elasp_second = 15;
            }
            else if (time_hours <= 48)
            {
                elasp_second = 20;
            }
            else if (time_hours <= 72)   //3天
            {
                elasp_second = 25;
            }
            else if (time_hours <= 96)   //4天
            {
                elasp_second = 35;
            }
            else if (time_hours <= 120)   //5天
            {
                elasp_second = 50;
            }
            else if (time_hours <= 168)   //7天
            {
                elasp_second = 60;
            }
            else
            {
                elasp_second = Math.Round(time_hours * 1.1 * 80 / 168);
            }

            return elasp_second;  
        }

        //获取模型信息(包括调度信息)
        public static string[] Get_Model_Info(HydroModel model, string start_simulatetime = "", bool iscalting = false)
        {
            //定义信息数组
            string[] model_info = new string[8];
            model_info[0] = model.Model_Faname;

            model_info[1] = model.Modeldesc;

            model_info[2] = model.ModelGlobalPars.Simulate_time.Begin.AddHours(model.ModelGlobalPars.Ahead_hours).ToString(Model_Const.TIMEFORMAT);
            model_info[3] = model.ModelGlobalPars.Simulate_time.End.ToString(Model_Const.TIMEFORMAT);

            switch (model.Model_state)
            {
                case Model_State.Finished:
                    model_info[4] = "已完成";
                    break;
                case Model_State.Error:
                    model_info[4] = "模型错误";
                    break;
                case Model_State.Ready_Calting:
                    model_info[4] = "待计算";
                    break;
                default:
                    model_info[4] = "计算中";
                    break;
            }

            model_info[5] = start_simulatetime;

            //获取模型调度信息
            model_info[6] = Item_Info.Get_Model_DdInfo(model);
            return model_info;
        }

        //获取新建模型信息
        public static Dictionary<string, Dictionary<string, string>> Get_NewModel_Info(HydroModel model)
        {
            //获取模型信息
            string[] model_info = Item_Info.Get_Model_Info(model);

            //与数据库对应的字段，并修改了最后一个以返回给前端
            string[] item = new string[] { "fangan_name", "model_desc", "start_time", "end_time", "model_state", "progress", "zzdd_info" };

            //将模拟开始时间信息 改为进度信息
            Dictionary<string, Dictionary<string, string>> new_modelinfo = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> model_info_dic = new Dictionary<string, string>();
            for (int i = 0; i < item.Length; i++)
            {
                if (i == item.Length - 2)
                {
                    model_info_dic.Add("progress", "0$0");
                    continue;
                }
                model_info_dic.Add(item[i], model_info[i]);
            }
            new_modelinfo.Add(model.Modelname, model_info_dic);
            return new_modelinfo;
        }
        #endregion ************************************************************************
    }
}
