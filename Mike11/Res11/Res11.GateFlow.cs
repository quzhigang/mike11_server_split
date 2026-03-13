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
        //查询各类闸门的流量过程
        public static Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_AllGate_Discharge(HydroModel hydromodel)
        {
            Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> gate_discharges = new Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>>();
            List<ReachInfo> main_reach = WG_INFO.Get_MainReachInfo(Mysql_GlobalVar.now_instance);
            List<DateTime> time_list = new List<DateTime>();

            //遍历各主河道
            for (int i = 0; i < main_reach.Count; i++)
            {
                Dictionary<string, Dictionary<DateTime, double>> gate_discharge = new Dictionary<string, Dictionary<DateTime, double>>();

                //该渠道引水流量--取起点流量
                AtReach atreach = AtReach.Get_Atreach(main_reach[i].reachname, main_reach[i].start_chainage);
                Dictionary<DateTime, double> ys_discharge_dic = hydromodel.Get_Mike11Section_SingleRes(atreach, mike11_restype.Discharge);
                Dictionary<DateTime, double> ys_discharge_dic1 = Get_ZZ_Dic(ys_discharge_dic, 2);  //转正

                if (time_list.Count == 0) time_list = ys_discharge_dic1.Keys.ToList();

                //该渠道分水流量--取所有分水闸和泵站之和
                Dictionary<string, Dictionary<DateTime, double>> fsz_discharge = Get_TypeGate_Discharge(hydromodel, time_list, GateType.LLZ);
                Dictionary<DateTime, double> fs1_discharge_dic = Total_TypeGate_Discharge(fsz_discharge);
                Dictionary<string, Dictionary<DateTime, double>> bz_discharge = Get_TypeGate_Discharge(hydromodel, time_list, GateType.BZ);
                Dictionary<DateTime, double> fs2_discharge_dic = Total_TypeGate_Discharge(bz_discharge);
                Dictionary<DateTime, double> fs_discharge_dic = Add_Gate_Discharge(fs1_discharge_dic, fs2_discharge_dic);

                //该渠道退水流量--取所有退水闸流量之和
                Dictionary<string, Dictionary<DateTime, double>> tsz_discharge = Get_TypeGate_Discharge(hydromodel, time_list, GateType.PBZ);
                Dictionary<DateTime, double> ts_discharge_dic = Total_TypeGate_Discharge(tsz_discharge);

                gate_discharge.Add("引水流量", ys_discharge_dic1);
                gate_discharge.Add("分水流量", fs_discharge_dic);
                gate_discharge.Add("退水流量", ts_discharge_dic);

                //加入集合
                gate_discharges.Add(main_reach[i].reachname, gate_discharge);
            }

            return gate_discharges;
        }

        //汇总同一类闸门流量过程
        public static Dictionary<DateTime, double> Total_TypeGate_Discharge(Dictionary<string, Dictionary<DateTime, double>> gate_discharge)
        {
            Dictionary<DateTime, double> discharge_dic = new Dictionary<DateTime, double>();
            if (gate_discharge == null || gate_discharge.Count == 0) return discharge_dic;
            Dictionary<DateTime, double> first_str = gate_discharge.First().Value;
            for (int i = 0; i < first_str.Count; i++)
            {
                DateTime time = first_str.ElementAt(i).Key;
                double discharge = 0;
                for (int j = 0; j < gate_discharge.Count; j++)
                {
                    discharge += gate_discharge.ElementAt(j).Value[time];
                }
                discharge_dic.Add(time, discharge);
            }

            return discharge_dic;
        }

        //把结果转正
        public static Dictionary<DateTime, double> Get_ZZ_Dic(Dictionary<DateTime, double> sourcedic, int dec)
        {
            Dictionary<DateTime, double> newdic = new Dictionary<DateTime, double>();
            for (int i = 0; i < sourcedic.Count; i++)
            {
                newdic.Add(sourcedic.ElementAt(i).Key, Math.Round(Math.Abs(sourcedic.ElementAt(i).Value), dec));
            }
            return newdic;
        }

        //合并2类闸门流量过程
        public static Dictionary<DateTime, double> Add_Gate_Discharge(Dictionary<DateTime, double> gate_discharge1, Dictionary<DateTime, double> gate_discharge2)
        {
            if (gate_discharge1.Count != gate_discharge2.Count) return null;
            Dictionary<DateTime, double> new_dic = new Dictionary<DateTime, double>();

            for (int i = 0; i < gate_discharge1.Count; i++)
            {
                DateTime time = gate_discharge1.ElementAt(i).Key;
                double new_value = gate_discharge1.ElementAt(i).Value + gate_discharge2.ElementAt(i).Value;

                new_dic.Add(time, new_value);
            }

            return new_dic;
        }

        //查询某类闸门流量过程(节制闸、退水闸根据河道位置，分水闸、泵站根据边界条件)
        public static Dictionary<string, Dictionary<DateTime, double>> Get_TypeGate_Discharge(HydroModel hydromodel, List<DateTime> time_list, GateType gate_type)
        {
            Dictionary<string, Dictionary<DateTime, double>> gate_dischargedic = new Dictionary<string, Dictionary<DateTime, double>>();

            //获取闸门数量
            Dictionary<string, AtReach> gate_dic = WG_INFO.Get_TypeGate_FromStrBaseInfo(hydromodel, gate_type);

            //如果是分水闸、泵站
            if (gate_type == GateType.LLZ || gate_type == GateType.BZ)
            {
                for (int i = 0; i < gate_dic.Count; i++)
                {
                    double discharge = -1 * hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(gate_dic.ElementAt(i).Key).Bd_value;
                    Dictionary<DateTime, double> gate_discharge = Dfs0.GetDic_SingleValueDic(time_list, discharge);
                    gate_dischargedic.Add(gate_dic.ElementAt(i).Key, gate_discharge);
                }
            }
            else  //退水闸(根据基本属性得到的所在河道为干渠，非退水闸支流，故流量过程直接采用 退水闸支流起点流量)
            {
                ControlList model_controls = hydromodel.Mike11Pars.ControlstrList;
                Dictionary<string, AtReach> open_gate = new Dictionary<string, AtReach>();

                //退水闸先判断开启的，免得浪费时间
                for (int i = 0; i < gate_dic.Count; i++)
                {
                    Normalstr str = model_controls.GetGate(gate_dic.ElementAt(i).Key) as Normalstr;
                    if (str.Ddparams_zmdu.opengatenumber != 0 && str.Ddparams_zmdu.opengateheight != str.Stratts.sill_level)
                    {
                        open_gate.Add(gate_dic.ElementAt(i).Key, gate_dic.ElementAt(i).Value);
                    }
                }

                //增加一个，免得出错
                if (open_gate.Count == 0) open_gate.Add(gate_dic.First().Key, gate_dic.First().Value);

                //获取各建筑物流量过程
                for (int i = 0; i < open_gate.Count; i++)
                {
                    Dictionary<DateTime, double> gate_discharge = Res11reader_Ongird(hydromodel, open_gate.ElementAt(i).Value, mike11_restype.Discharge);
                    gate_dischargedic.Add(open_gate.ElementAt(i).Key, gate_discharge);
                }
            }

            return gate_dischargedic;
        }

        //查询多个指定河道桩号的指定项（仅水位或流量），返回该项的时间序列(适用于指定河道桩号一定在网格点上的情况，加快效率)
        public static Dictionary<DateTime, double> Res11reader_Ongird(HydroModel hydromodel, AtReach atreach, mike11_restype itemtype)
        {
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();

            //源文件和项目类型
            string sourcefilename = hydromodel.Modelfiles.Hdres11_filename;

            //河名
            string reachname = atreach.reachname;
            double chainage = atreach.chainage;

            //项目名
            string itemname = null;
            if (itemtype == mike11_restype.Water_Level)
            {
                itemname = itemtype.ToString().Replace("_", " ");
            }
            else
            {
                itemname = itemtype.ToString();
            }

            //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(sourcefilename);

            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //获取时间数组
            DateTime[] timearray = resdata.TimesList.ToArray();

            //找到指定的河道,注意有的河道分段了，要找到正确的河道河段
            IRes1DReach reach = resdata.Reaches.ElementAt(0);
            int reach_id = 0; //知道该河道名字的河道序号
            int grid_id = 0;
            for (int i = 0; i < resdata.Reaches.Count; i++)
            {
                reach = resdata.Reaches.ElementAt(i);
                if (reach.Name != reachname) continue;

                reach = resdata.Reaches.ElementAt(i);
                double reachupchainage = reach.GetChainages(0)[0];
                double reachdownchainage = reach.GetChainages(0)[reach.GetChainages(0).Length - 1];

                //如果河名和桩号都能对上
                if (chainage >= reachupchainage && chainage <= reachdownchainage)
                {
                    reach_id = i;
                    for (int j = 0; j < reach.GridPoints.Count; j++)
                    {
                        double sec_chainage = reach.GridPoints.ElementAt(j).Chainage;
                        if (sec_chainage != chainage) continue;
                        grid_id = j;
                        break;
                    }
                    break;
                }
            }

            //找到指定的项目
            int itemnumber = 0;
            IDataItem dataitem = reach.DataItems.ElementAt(0);
            for (int i = 0; i < reach.DataItems.Count; i++)
            {
                dataitem = reach.DataItems.ElementAt(i);
                if (string.Equals(dataitem.Quantity.Description, itemname, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                itemnumber++;
            }

            //各同名河道遍历
            reach = resdata.Reaches.ElementAt(reach_id);
            IDataItem data_item = reach.DataItems.ElementAt(itemnumber);

            //直接获取河道结果的二维数组，其中行为时间数量，列为计算点数量
            double[] reach_res = data_item.CreateTimeSeriesData(grid_id);
            for (int i = 0; i < reach_res.Length; i++)
            {
                result.Add(timearray[i], Math.Round(reach_res[i], 1));//保留1位小数
            }

            //移除前面提前的小时数
            Remove_Ahead_Res(hydromodel, ref result);

            return result;
        }

    }
}
