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
        //查询河道特征断面的洪水结果
        public static Dictionary<string, ReachSection_FloodRes> Get_ReachSection_FloodRes(HydroModel hydromodel, ResultData resdata, string model_instance)
        {
            Dictionary<string, ReachSection_FloodRes> reach_floodres = new Dictionary<string, ReachSection_FloodRes>();

            //获取所有河道特征断面信息
            List<Water_Condition> section_waterlevel = Get_ReachSection_Info(model_instance);

            //遍历
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                ReachSection_FloodRes section_res = Get_Section_FloodRes(hydromodel, section_waterlevel[i], resdata);
                reach_floodres.Add(section_waterlevel[i].Name, section_res);
            }

            return reach_floodres;
        }

        //获取所有河道特征断面信息
        public static List<Water_Condition> Get_ReachSection_Info(string model_instance)
        {
            //获取所有河道特征断面信息
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //遍历
            List<Water_Condition> section_waterlevel = new List<Water_Condition>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                if (now_waterlevel[i].Datasource != "水库水文站" && now_waterlevel[i].Station_Type == "主要" &&
                    now_waterlevel[i].Model_instance.Contains(model_instance))
                {
                    section_waterlevel.Add(now_waterlevel[i]);
                }
            }

            return section_waterlevel;
        }

        //查询某河道断面的所有洪水结果
        public static ReachSection_FloodRes Get_Section_FloodRes(HydroModel hydromodel, Water_Condition section, ResultData resdata)
        {
            List<double> sections = hydromodel.Mike11Pars.SectionList.Get_Reach_SecChainageList(section.Reach);
            int up_section_index = File_Common.Search_Value(sections, section.Chainage, true);

            //用最近的上一个水位断面数据
            double section_chainage = sections[up_section_index];

            //如果附近断面只是小数位差别，则认为是同一个断面，则采用
            if (Math.Abs(sections[up_section_index + 1] - section.Chainage) < 0.1) section_chainage = sections[up_section_index + 1];

            AtReach q_atreach = AtReach.Get_Atreach(section.Reach, section_chainage);
            AtReach h_atreach = AtReach.Get_Atreach(section.Reach, section_chainage);
            string stcd = section.Stcd;
            string name = section.Name;
            Dictionary<DateTime, double> discharge_Dic = Res11.Res11reader(hydromodel.Modelname, q_atreach, mike11_restype.Discharge, resdata);
            Dictionary<DateTime, double> level_Dic = Res11.Res11reader(hydromodel.Modelname, h_atreach, mike11_restype.Water_Level, resdata);

            //去掉前面的时间序列和保留1位小数
            if (discharge_Dic != null)
            {
                Remove_Ahead_Res(hydromodel, ref discharge_Dic);
                discharge_Dic = discharge_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 1));
            }

            if (level_Dic != null)
            {
                Remove_Ahead_Res(hydromodel, ref level_Dic);
                level_Dic = level_Dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));
            }

            //如果是自动预报，则对前面一段水位流量过程进行曲线修正，使其与实测匹配(采用S曲线修正，水位实测为0不修正)
            if (hydromodel.Modelname == Model_Const.AUTO_MODELNAME)
            {
                discharge_Dic = Dfs0.Modify_DicFront_WithSmoothCurve(discharge_Dic, section.Discharge, Model_Const.Modify_Front_Hours);
                if (level_Dic.ElementAt(0).Value != section.Level && section.Level != 0) level_Dic = Dfs0.Modify_DicFront_WithSmoothCurve(level_Dic, section.Level, Model_Const.Modify_Front_Hours);
            }

            double max_Qischarge = discharge_Dic.Values.Max();
            double max_Level = level_Dic.Values.Max();
            DateTime maxQ_AtTime = discharge_Dic.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            DateTime start_time = hydromodel.ModelGlobalPars.Simulate_time.Begin.AddHours(Model_Const.AHEAD_HOURS);
            if (DateTime.Compare(maxQ_AtTime, start_time) < 0) maxQ_AtTime = start_time;
            double total_Flood = Get_TotalVolume(discharge_Dic);
            ReachSection_FloodRes section_res = new ReachSection_FloodRes(stcd, name, max_Qischarge, max_Level, maxQ_AtTime, total_Flood, level_Dic, discharge_Dic);

            return section_res;
        }
    }
}
