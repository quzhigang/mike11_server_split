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
        #region ************************闸站状态结果*************************
        //根据闸站调度结果和闸站流量，推求闸站状态结果
        public static Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> Get_Gate_State(HydroModel hydromodel,
            Dictionary<string, List<string[]>> model_ddinfo, Dictionary<string, Dictionary<DateTime, double>> fsz_discharge)
        {
            Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> gate_state_res = new Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>>();

            List<DateTime> time_list = fsz_discharge.ElementAt(0).Value.Keys.ToList();

            //通过节制闸闸站调度信息 获取 节制闸、退水闸状态结果
            List<Dictionary<DateTime, Gate_State_Res>> jzz_dd_state = Get_Gate_StateDic(time_list, model_ddinfo, GateType.YLZ);
            List<Dictionary<DateTime, Gate_State_Res>> tsz_dd_state = Get_Gate_StateDic(time_list, model_ddinfo, GateType.PBZ);

            //通过获取分水闸调度信息、流量过程 获取 分水闸、泵站状态结果
            List<Dictionary<DateTime, Gate_State_Res>> fsz_dd_state = Get_Gate_StateDic(time_list, model_ddinfo, GateType.LLZ, fsz_discharge, hydromodel);

            //转换格式
            gate_state_res = Gate_State_Format(time_list, jzz_dd_state, tsz_dd_state, fsz_dd_state);

            return gate_state_res;
        }

        //通过闸站调度信息、流量 获取 节制闸状态结果
        public static List<Dictionary<DateTime, Gate_State_Res>> Get_Gate_StateDic(List<DateTime> time_list, Dictionary<string, List<string[]>> model_ddinfo,
            GateType gate_type, Dictionary<string, Dictionary<DateTime, double>> fsz_discharge = null, HydroModel hydromodel = null)
        {
            List<Dictionary<DateTime, Gate_State_Res>> typegate_state_diclist = new List<Dictionary<DateTime, Gate_State_Res>>();

            //获取节制闸调度信息
            string dd_type;
            Gate_State gate_gzdd_state;
            double operate_time;
            switch (gate_type)
            {
                case GateType.YLZ:
                    dd_type = "jzz_dd";
                    operate_time = Const_Global.Model_Const.JZZ_OCTIME;
                    gate_gzdd_state = Gate_State.open;
                    break;
                case GateType.PBZ:
                    dd_type = "tsz_dd";
                    operate_time = Const_Global.Model_Const.TSZ_OCTIME;
                    gate_gzdd_state = Gate_State.close;
                    break;
                default:
                    gate_gzdd_state = Gate_State.open;
                    operate_time = Const_Global.Model_Const.FSZ_OCTIME;
                    dd_type = "fsz_dd";
                    break;
            }

            //遍历一类闸门调度信息
            List<string[]> gate_ddinfo = model_ddinfo[dd_type];

            for (int i = 0; i < gate_ddinfo.Count; i++)
            {
                string gate_name = gate_ddinfo[i][1];
                string gate_ddfs = gate_ddinfo[i][2];
                string gate_ddgc = gate_ddinfo[i][3];
                Dictionary<DateTime, Gate_State_Res> typegate_state_res = new Dictionary<DateTime, Gate_State_Res>();

                //如果是规则调度，则按节制闸、退水闸的调度规则来
                if (gate_ddfs == "指令调度")
                {
                    typegate_state_res = Get_Gate_ZlddState(gate_name, time_list, gate_ddgc, operate_time);
                }
                else
                {
                    if (gate_type == GateType.YLZ || gate_type == GateType.PBZ)
                    {
                        typegate_state_res = Get_JZZTSZ_GZDDState(gate_name, time_list, gate_gzdd_state);
                    }
                    else
                    {
                        typegate_state_res = Get_FSZ_GZDDstate(gate_name, time_list, fsz_discharge, gate_type, hydromodel);
                    }
                }
                typegate_state_diclist.Add(typegate_state_res);
            }

            return typegate_state_diclist;
        }

        //从调度指令中获取闸门状态集合
        private static Dictionary<DateTime, Gate_State_Res> Get_Gate_ZlddState(string strname, List<DateTime> time_list, string gate_ddgc, double operate_time)
        {
            Dictionary<DateTime, Gate_State_Res> gate_state_res = new Dictionary<DateTime, Gate_State_Res>();

            //获取单个调度开关集合
            string[] dd_strinfo = gate_ddgc.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<DateTime, Gate_State> gate_ocdic = new Dictionary<DateTime, Gate_State>();
            for (int i = 0; i < dd_strinfo.Length; i++)
            {
                string datestr = dd_strinfo[i].Trim().Substring(0, dd_strinfo[i].Trim().Length - 4);
                DateTime dd_time = SimulateTime.StrToTime(datestr);
                string openorclose = dd_strinfo[i].Trim().Substring(dd_strinfo[i].Trim().Length - 4).Substring(0, 2);

                Gate_State gate_state;
                Gate_State reverse_state;
                if (openorclose == "开启")
                {
                    gate_state = Gate_State.open;
                    reverse_state = Gate_State.close;
                }
                else
                {
                    gate_state = Gate_State.close;
                    reverse_state = Gate_State.open;
                }

                //插入第一个状态(该状态为下一个调度的反向状态)
                if (i == 0 && dd_time != time_list[0]) gate_ocdic.Add(time_list[0], reverse_state);

                gate_ocdic.Add(dd_time, reverse_state);   //该命令开始时刻状态为反向的
                gate_ocdic.Add(dd_time.AddMinutes(operate_time), gate_state);  //该命令结束时刻状态才为该指令状态

                //插入最后一个状态，为集合之前的最后一个状态
                Gate_State last_gatestate = gate_ocdic.Last().Value;
                if (i == dd_strinfo.Length - 1 && dd_time != time_list.Last()) gate_ocdic.Add(time_list.Last(), last_gatestate);
            }

            //根据时间判断开关
            for (int i = 0; i < time_list.Count; i++)
            {
                DateTime now_time = time_list[i];
                Gate_State now_state = Gate_State.open;
                for (int j = 0; j < gate_ocdic.Count - 1; j++)
                {
                    DateTime start_time = gate_ocdic.ElementAt(j).Key;
                    DateTime end_time = gate_ocdic.ElementAt(j + 1).Key;
                    if (now_time.Subtract(start_time).TotalHours >= 0 && now_time.Subtract(end_time).TotalHours <= 0)
                    {
                        //如果时间等于开始时间
                        if (now_time == start_time)
                        {
                            now_state = gate_ocdic.ElementAt(j).Value;
                            break;
                        }

                        //如果时间等于结束时间
                        if (now_time == end_time)
                        {
                            now_state = gate_ocdic.ElementAt(j + 1).Value;
                            break;
                        }

                        //如果时间在调度状态之间
                        if (gate_ocdic.ElementAt(j).Value == gate_ocdic.ElementAt(j + 1).Value)
                        {
                            now_state = gate_ocdic.ElementAt(j).Value;
                        }
                        else if (gate_ocdic.ElementAt(j).Value == Gate_State.open && gate_ocdic.ElementAt(j + 1).Value == Gate_State.close)
                        {
                            now_state = Gate_State.closing;
                        }
                        else
                        {
                            now_state = Gate_State.opening;
                        }
                        break;
                    }
                }

                Gate_State_Res gaet_state_obj = new Gate_State_Res(strname, now_state);
                gate_state_res.Add(now_time, gaet_state_obj);
            }

            return gate_state_res;
        }

        //获取节制闸、退水闸闸门规则调度状态集合
        private static Dictionary<DateTime, Gate_State_Res> Get_JZZTSZ_GZDDState(string strname, List<DateTime> time_list, Gate_State gate_gzdd_state)
        {
            Dictionary<DateTime, Gate_State_Res> gate_state_res = new Dictionary<DateTime, Gate_State_Res>();
            for (int i = 0; i < time_list.Count; i++)
            {
                Gate_State_Res gaet_state_obj = new Gate_State_Res(strname, gate_gzdd_state);
                gate_state_res.Add(time_list[i], gaet_state_obj);
            }
            return gate_state_res;
        }

        //获取分水闸、泵站规则调度状态集合
        private static Dictionary<DateTime, Gate_State_Res> Get_FSZ_GZDDstate(string strname, List<DateTime> time_list,
            Dictionary<string, Dictionary<DateTime, double>> discharge_dic, GateType gate_type, HydroModel hydromodel)
        {
            Dictionary<DateTime, Gate_State_Res> gate_state_res = new Dictionary<DateTime, Gate_State_Res>();

            //获取建筑物英文名字
            string str_enname = Item_Info.Get_StrEnglishName(strname, "", "");

            //获取分水闸、泵站设计流量
            double design_discharge = 0;
            if (gate_type == GateType.LLZ)
            {
                Controlstr str = hydromodel.Mike11Pars.ControlstrList.GetGate(str_enname);
                design_discharge = str is Normalstr ? (str as Normalstr).Ddparams_double : 0;
            }
            else
            {
                design_discharge = 20.0;  //惠南庄泵站按20流量考虑为全开
            }

            for (int i = 0; i < time_list.Count - 1; i++)
            {
                //获取该时刻下建筑物流量
                double discharge = discharge_dic[strname][time_list[i]];

                //获取下一时刻建筑物流量
                double next_discharge = discharge_dic[strname][time_list[i + 1]];

                //如果当前时刻流量小于0.1m3/s，则认为关闭
                Gate_State gate_gzdd_state;
                if (discharge < 0.1)
                {
                    gate_gzdd_state = Gate_State.close;
                }
                else if (discharge >= design_discharge)
                {
                    gate_gzdd_state = Gate_State.open;
                }
                else
                {
                    gate_gzdd_state = Gate_State.opening;
                    if (next_discharge < discharge) gate_gzdd_state = Gate_State.closing;
                }

                Gate_State_Res gaet_state_obj = new Gate_State_Res(strname, gate_gzdd_state);
                gate_state_res.Add(time_list[i], gaet_state_obj);
            }
            gate_state_res.Add(time_list[time_list.Count - 1], gate_state_res.Last().Value);

            return gate_state_res;
        }

        //转换闸门状态格式
        private static Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> Gate_State_Format(List<DateTime> time_list, List<Dictionary<DateTime, Gate_State_Res>> jzz_dd_state,
            List<Dictionary<DateTime, Gate_State_Res>> tsz_dd_state, List<Dictionary<DateTime, Gate_State_Res>> fsz_dd_state)
        {
            Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> gate_state_res = new Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>>();

            for (int i = 0; i < time_list.Count; i++)
            {
                Dictionary<string, List<Gate_State_Res>> type_gate_statedic = new Dictionary<string, List<Gate_State_Res>>();

                List<Gate_State_Res> jzz_statelist = new List<Gate_State_Res>();
                for (int j = 0; j < jzz_dd_state.Count; j++)
                {
                    jzz_statelist.Add(jzz_dd_state[j][time_list[i]]);
                }
                type_gate_statedic.Add("jzz_state", jzz_statelist);

                List<Gate_State_Res> tsz_statelist = new List<Gate_State_Res>();
                for (int j = 0; j < tsz_dd_state.Count; j++)
                {
                    tsz_statelist.Add(tsz_dd_state[j][time_list[i]]);
                }
                type_gate_statedic.Add("tsz_state", tsz_statelist);

                List<Gate_State_Res> fsz_statelist = new List<Gate_State_Res>();
                for (int j = 0; j < fsz_dd_state.Count; j++)
                {
                    fsz_statelist.Add(fsz_dd_state[j][time_list[i]]);
                }
                type_gate_statedic.Add("fsz_state", fsz_statelist);

                gate_state_res.Add(time_list[i], type_gate_statedic);
            }

            return gate_state_res;
        }
        #endregion *********************************************************
    }
}
