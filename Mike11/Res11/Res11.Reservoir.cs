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
        //查询全部水库的洪水结果
        public static Dictionary<string, Reservoir_FloodRes> Get_AllReservoir_FloodRes(HydroModel hydromodel, ResultData resdata, string model_instance)
        {
            Dictionary<string, Reservoir_FloodRes> res_floodres = new Dictionary<string, Reservoir_FloodRes>();

            //获取所有水库信息
            List<Reservoir> res_info = Item_Info.Get_ResInfo(model_instance);

            //从数据库获取一维模型水库的初始水情条件
            Dictionary<string, double> res_initial_levels = Hd11.Get_Res_InitialLevel(hydromodel.Modelname);

            //遍历水库
            for (int i = 0; i < res_info.Count; i++)
            {
                double res_initiallevel = res_initial_levels == null ? 0 : res_initial_levels[res_info[i].Name];
                Reservoir_FloodRes floodres = Get_Reservoir_FloodRes(hydromodel, res_info[i], resdata, res_initiallevel);
                res_floodres.Add(res_info[i].Name, floodres);
            }

            return res_floodres;
        }

        //查询某水库的洪水结果
        public static Reservoir_FloodRes Get_Reservoir_FloodRes(HydroModel hydromodel, Reservoir res, ResultData resdata,double res_initiallevel)
        {
            string stcd = res.Stcd;
            string name = res.Name;

            //求水库水位过程、最高水位和出现的时间,用最近的上一个水位断面数据
            List<double> sections = hydromodel.Mike11Pars.SectionList.Get_Reach_SecChainageList(res.Atreach_Segment.reachname);
            int up_section_index = File_Common.Search_Value(sections, res.Atreach_Segment.end_chainage, true);
            double section_chainage = sections[up_section_index];
            AtReach res_level_atreach = AtReach.Get_Atreach(res.Atreach_Segment.reachname, section_chainage);

            //水库 水位过程
            Dictionary<DateTime, double> level_Dic = Res11.Res11reader(hydromodel.Modelname, res_level_atreach, mike11_restype.Water_Level, resdata);
            if (level_Dic != null) Remove_Ahead_Res(hydromodel, ref level_Dic);
            double res_level_xz = (res_initiallevel == 0 || level_Dic == null) ? 0 : level_Dic.First().Value - res_initiallevel;
            if (level_Dic != null) level_Dic = level_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value - res_level_xz, 2));

            //最高水位和出现的时间
            double max_Level = level_Dic == null ? 0 : level_Dic.Values.Max();
            DateTime maxLevel_Time = level_Dic.OrderByDescending(entry => entry.Value).First().Key;

            //库容
            Dictionary<DateTime, double> volumn_Dic = File_Common.Insert_ResVolumn(level_Dic, res.Level_Volume);
            if (volumn_Dic != null) volumn_Dic = volumn_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 3));
            double max_Volumn = volumn_Dic.Values.Max();

            //出库流量过程
            Dictionary<string, AtReach[]> res_outatreach = Item_Info.GetRes_OutQAtreach(Mysql_GlobalVar.now_instance);
            AtReach[] this_resout = res_outatreach[res.Name];
            Dictionary<DateTime, double> yHDOutQ_Dic = Res11.Res11reader(hydromodel.Modelname, this_resout[0], mike11_restype.Discharge, resdata);
            if (yHDOutQ_Dic != null) Remove_Ahead_Res(hydromodel, ref yHDOutQ_Dic);
            if (yHDOutQ_Dic != null) yHDOutQ_Dic = yHDOutQ_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));

            Dictionary<DateTime, double> xHDOutQ_Dic = this_resout.Length == 1 ? null : Res11.Res11reader(hydromodel.Modelname, this_resout[1], mike11_restype.Discharge, resdata);
            if (xHDOutQ_Dic != null) Remove_Ahead_Res(hydromodel, ref xHDOutQ_Dic);
            if (xHDOutQ_Dic != null) xHDOutQ_Dic = xHDOutQ_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));

            Dictionary<DateTime, double> outQ_Dic = this_resout.Length == 1 ? yHDOutQ_Dic : Dfs0.Combine_Dic(yHDOutQ_Dic, xHDOutQ_Dic);
            double max_OutQ = outQ_Dic.Values.Max();
            DateTime maxoutq_Time = outQ_Dic.OrderByDescending(entry => entry.Value).First().Key;

            //入库流量过程
            Dictionary<string, Res_InSection_BndID> res_inflows = Item_Info.Get_ResInFlow_SectionBndID(Mysql_GlobalVar.now_instance);
            Res_InSection_BndID res_inflow = res_inflows[res.Name];
            Dictionary<DateTime, double> inQ_Dic = Calculate_ResInflow(hydromodel, res, res_inflow);
            if (inQ_Dic != null) Remove_Ahead_Res(hydromodel, ref inQ_Dic);
            if (inQ_Dic != null) inQ_Dic = inQ_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));
            double max_InQ = (inQ_Dic == null || inQ_Dic.Count == 0) ? 0 : inQ_Dic.Values.Max();
            DateTime maxinq_Time = (inQ_Dic == null || inQ_Dic.Count == 0) ?
                hydromodel.ModelGlobalPars.Simulate_time.End : inQ_Dic.OrderByDescending(entry => entry.Value).First().Key;

            //出入库洪量统计
            double total_InVolumn = inQ_Dic == null ? 0 : Math.Round(Get_TotalVolume(inQ_Dic), 3);
            double total_OutVolumn = Math.Round(Get_TotalVolume(outQ_Dic), 3);
            double YHDTotal_OutVolumn = Math.Round(Get_TotalVolume(yHDOutQ_Dic), 3);
            double XHDTotal_OutVolumn = this_resout.Length == 1 ? 0 : Math.Round(Get_TotalVolume(xHDOutQ_Dic), 3);

            //最终水位库容
            double endTime_Level = level_Dic.Values.Last();
            double endTime_Volumn = volumn_Dic.Values.Last();

            //对象实例化
            Reservoir_FloodRes flood_res = new Reservoir_FloodRes(stcd, name, max_Level, max_Volumn, maxLevel_Time, max_InQ, max_OutQ,
             total_InVolumn, total_OutVolumn, YHDTotal_OutVolumn, XHDTotal_OutVolumn, endTime_Level, endTime_Volumn, inQ_Dic, outQ_Dic,
             maxinq_Time, maxoutq_Time, yHDOutQ_Dic, xHDOutQ_Dic, level_Dic, volumn_Dic);

            return flood_res;
        }

        //根据水库的入库边界条件和河道断面，汇总入库流量过程
        public static Dictionary<DateTime, double> Calculate_ResInflow(HydroModel hydromodel, Reservoir res, Res_InSection_BndID res_inflow)
        {
            // 入库流量
            Dictionary<DateTime, double> inq_res = new Dictionary<DateTime, double>();

            //获取边界条件入流过程
            List<Dictionary<DateTime, double>> bnd_inqs = Get_Res_BndInQ(hydromodel, res, res_inflow);

            //边界条件入流合并后，内插为保存时间
            Dictionary<DateTime, double> bnd_inqres = new Dictionary<DateTime, double>();
            if (bnd_inqs.Count != 0)
            {
                Dictionary<DateTime, double> bnd_inqcombine = Dfs0.Combine_Dic(bnd_inqs);
                TimeSpan timespan = new TimeSpan(0, 0, (int)hydromodel.Mike11Pars.Mike11_savetimestep * 60);
                bnd_inqres = bnd_inqcombine == null ? null : Dfs0.Insert_Instantdic(bnd_inqcombine, timespan);
            }

            //上游断面入流
            List<Dictionary<DateTime, double>> upsection_inqs = new List<Dictionary<DateTime, double>>();
            if (res_inflow.Up_Insection.Count != 0)
            {
                for (int i = 0; i < res_inflow.Up_Insection.Count; i++)
                {
                    AtReach section = res_inflow.Up_Insection[i];
                    Dictionary<DateTime, double> upsection_inq = Res11.Res11reader(hydromodel.Modelname, section, mike11_restype.Discharge);

                    //增加几个小时
                    double section_res_distance = Get_Section_ToResDistance(hydromodel, res, section);
                    double add_hours = Math.Round(section_res_distance / res_inflow.Inflow_Speed / 3600.0);
                    Dictionary<DateTime, double> upsection_inq_xz = Dfs0.Modify_Dic_ChangeHours(upsection_inq, add_hours);

                    upsection_inqs.Add(upsection_inq_xz);
                }
            }
            Dictionary<DateTime, double> up_inqres;
            if (upsection_inqs.Count == 0)
            {
                up_inqres = null;
            }
            else if (upsection_inqs[0] == null)
            {
                up_inqres = null;
            }
            else
            {
                up_inqres = Dfs0.Combine_Dic(upsection_inqs);
            }

            //全部入流
            Dictionary<DateTime, double> combine_res = Dfs0.Combine_Dic(up_inqres, bnd_inqres);
            inq_res = up_inqres == null ? bnd_inqres : combine_res;
            if (inq_res.Count == 0 && up_inqres != null) inq_res = up_inqres;

            //去掉模型初始时刻以前的
            inq_res = inq_res.Where(x => x.Key >= hydromodel.ModelGlobalPars.Simulate_time.Begin && x.Key <= hydromodel.ModelGlobalPars.Simulate_time.End).ToDictionary(x => x.Key, x => x.Value);
            return inq_res;
        }

        //获取干流或支流断面到干流水库大坝的间距
        public static double Get_Section_ToResDistance(HydroModel hydromodel, Reservoir res, AtReach section)
        {
            double distance = 0;

            //水库大坝桩号
            Dictionary<string, List<AtReach>> res_atreachs = Item_Info.GetRes_ControlInitialAtreach();
            List<AtReach> res_control_initialatreach = res_atreachs[res.Name];
            List<AtReach> main_reachs = new List<AtReach>();
            for (int i = 0; i < res_control_initialatreach.Count; i++)
            {
                if (res_control_initialatreach[i].reachname == res.Atreach_Segment.reachname) main_reachs.Add(res_control_initialatreach[i]);
            }
            double res_dam_chainage = main_reachs.Count == 0 ? 0 : main_reachs.Max(r => r.chainage);

            //干流或支流断面距水库大坝距离
            if (section.reachname == res.Atreach_Segment.reachname)
            {
                //断面就在干流上
                distance = Math.Max(0, res_dam_chainage - section.chainage);
            }
            else   //断面在支流上
            {
                //支流与断面的连接桩号
                ReachInfo reach_info = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(section.reachname);
                AtReach down_connect_atreach = reach_info.downstream_connect;
                if (down_connect_atreach.reachname == res.Atreach_Segment.reachname)
                {
                    distance = reach_info.end_chainage - section.chainage + Math.Max(0, res_dam_chainage - down_connect_atreach.chainage);
                }
            }

            return distance;
        }

        //获取边界条件入流过程
        public static List<Dictionary<DateTime, double>> Get_Res_BndInQ(HydroModel hydromodel, Reservoir res, Res_InSection_BndID res_inflow)
        {
            //边界条件入流
            List<Dictionary<DateTime, double>> bnd_inqs = new List<Dictionary<DateTime, double>>();
            if (res_inflow.In_BndID.Count != 0)
            {
                for (int i = 0; i < res_inflow.In_BndID.Count; i++)
                {
                    //获取该边界条件的时间序列文件
                    string bndid = res_inflow.In_BndID[i];
                    Reach_Bd bnd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(bndid);
                    Dictionary<DateTime, double> upbnd_inq;
                    if (bnd.Valuetype == BdValueType.Constant)
                    {
                        upbnd_inq = Dfs0.Get_ConstValue_Dic(hydromodel.ModelGlobalPars.Simulate_time.Begin,
                                    hydromodel.ModelGlobalPars.Simulate_time.End, new TimeSpan(1, 0, 0), bnd.Bd_value);
                        bnd_inqs.Add(upbnd_inq);
                    }
                    else
                    {
                        upbnd_inq = Dfs0.Dfs0_Reader_GetItemDic(bnd.Bd_filename, 1);

                        //增加几个小时
                        double bnd_res_distance = Get_Bnd_ToResDistance(res, bnd);
                        double add_hours = Math.Round(bnd_res_distance / res_inflow.Inflow_Speed / 3600.0);
                        Dictionary<DateTime, double> upbnd_inq_xz = Dfs0.Modify_Dic_ChangeHours(upbnd_inq, add_hours);
                        bnd_inqs.Add(upbnd_inq_xz);
                    }
                }
            }

            return bnd_inqs;
        }

        //获取入流边界条件到水库大坝的间距
        public static double Get_Bnd_ToResDistance(Reservoir res, Reach_Bd bnd)
        {
            //边界条件所在河道和位置
            string reach_name = bnd.Reachinfo.reachname;
            double start_chainage = bnd.Reachinfo.chainage;
            double end_chainage = bnd.Endchainage;
            double bnd_chainage = end_chainage == 0 ? start_chainage : (start_chainage + end_chainage) * 0.5;

            //水库大坝桩号
            Dictionary<string, List<AtReach>> res_atreachs = Item_Info.GetRes_ControlInitialAtreach();
            List<AtReach> res_control_initialatreach = res_atreachs[res.Name];
            List<AtReach> main_reachs = new List<AtReach>();
            for (int i = 0; i < res_control_initialatreach.Count; i++)
            {
                if (res_control_initialatreach[i].reachname == res.Atreach_Segment.reachname) main_reachs.Add(res_control_initialatreach[i]);
            }
            double res_dam_chainage = main_reachs.Count == 0 ? 0 : main_reachs.Max(r => r.chainage);

            //返回间距
            double distance = reach_name == res.Atreach_Segment.reachname ? Math.Max(0, res_dam_chainage - bnd_chainage) : 0;
            return distance;
        }

        //获取边界条件入流过程
        public static List<Dictionary<DateTime, double>> Get_Res_BndInQ(HydroModel hydromodel, List<string> res_inflowbnds)
        {
            //边界条件入流
            List<Dictionary<DateTime, double>> bnd_inqs = new List<Dictionary<DateTime, double>>();
            if (res_inflowbnds.Count != 0)
            {
                for (int i = 0; i < res_inflowbnds.Count; i++)
                {
                    //获取该边界条件的时间序列文件
                    string bndid = res_inflowbnds[i];
                    Reach_Bd bnd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(bndid);
                    Dictionary<DateTime, double> upbnd_inq;
                    if (bnd.Valuetype == BdValueType.Constant)
                    {
                        upbnd_inq = Dfs0.Get_ConstValue_Dic(hydromodel.ModelGlobalPars.Simulate_time.Begin,
                                    hydromodel.ModelGlobalPars.Simulate_time.End, new TimeSpan(1, 0, 0), bnd.Bd_value);
                    }
                    else
                    {
                        upbnd_inq = Dfs0.Dfs0_Reader_GetItemDic(bnd.Bd_filename, 1);
                    }

                    //减去前面的小时
                    if(upbnd_inq != null)
                    {
                        Remove_Ahead_Res(hydromodel, ref upbnd_inq);
                    }

                    bnd_inqs.Add(upbnd_inq);
                }
            }

            return bnd_inqs;
        }
    }
}
