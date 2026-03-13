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
        #region **********************结果过程统计报表**********************
        //获取最后时刻 各水位点和建筑物位置的序号，位置名称、桩号、流量、水位、水深、流速、渠底高、堤顶高
        public static Dictionary<string, List<object[]>> Get_ResStatistics_Table(HydroModel model, Dictionary<string, List<float>> H_Chainage_Float, Dictionary<string, Dictionary<DateTime, List<float>>> DischargeZdmHD_Data,
            Dictionary<string, Dictionary<DateTime, List<float>>> LevelZdmHD_Data, Dictionary<string, Dictionary<DateTime, List<float>>> SpeedZdmHD_Data,
            Dictionary<string, List<float>> Zdm_dd_Data, Dictionary<string, List<float>> Zdm_qd_Data)
        {
            Dictionary<string, List<object[]>> statistics_res = new Dictionary<string, List<object[]>>();

            //遍历河道
            for (int i = 0; i < DischargeZdmHD_Data.Count; i++)
            {
                string reach_name = DischargeZdmHD_Data.ElementAt(i).Key;

                //纵断面桩号(水位点)
                List<float> section = new List<float>();
                for (int j = 0; j < H_Chainage_Float[reach_name].Count; j++)
                {
                    section.Add(H_Chainage_Float[reach_name][j]);
                }

                //河道内所有建筑物桩号
                List<float> str_chainage = WG_INFO.Get_ReachStrInfo(reach_name);

                //先获取断面名称
                Dictionary<float, string> section_names = Get_Section_Names(model, reach_name, str_chainage);

                //该河道最后时刻的流量纵段
                Dictionary<float, float> q_zd = Get_ZD_Dic(H_Chainage_Float[reach_name], DischargeZdmHD_Data[reach_name].Last().Value);

                //该河道最后时刻的水位纵段
                Dictionary<float, float> level_zd = Get_ZD_Dic(H_Chainage_Float[reach_name], LevelZdmHD_Data[reach_name].Last().Value);

                //该河道最后时刻的流速纵段
                Dictionary<float, float> v_zd = Get_ZD_Dic(H_Chainage_Float[reach_name], SpeedZdmHD_Data[reach_name].Last().Value);

                //该河道堤顶高程和渠底高程纵段
                Dictionary<float, float> dd_zd = Get_ZD_Dic(H_Chainage_Float[reach_name], Zdm_dd_Data[reach_name]);
                Dictionary<float, float> qd_zd = Get_ZD_Dic(H_Chainage_Float[reach_name], Zdm_qd_Data[reach_name]);

                //遍历建筑物
                List<object[]> strlocation_infos = new List<object[]>();
                for (int j = 0; j < str_chainage.Count; j++)
                {
                    int xh = j + 1;
                    float chainage = str_chainage[j];
                    string name = section_names[chainage];
                    string chainage_end = "000" + Math.Round((chainage - Math.Floor(chainage / 1000) * 1000.0), 0).ToString();
                    string chainage_str = reach_name + Math.Floor(chainage / 1000).ToString() + "+" + chainage_end.Substring(chainage_end.Length - 3);
                    float discharge = (float)Math.Round(File_Common.Insert_Zd_Value(q_zd, chainage), 1);
                    float level = (float)Math.Round(File_Common.Insert_Zd_Value(level_zd, chainage), 2);
                    float speed = (float)Math.Round(File_Common.Insert_Zd_Value(v_zd, chainage), 2);
                    float qd = (float)Math.Round(File_Common.Insert_Zd_Value(qd_zd, chainage), 2);
                    float dd = (float)Math.Round(File_Common.Insert_Zd_Value(dd_zd, chainage), 2);
                    float h = (float)Math.Round(level - qd, 2);

                    //加入集合
                    object[] section_info = { xh, name, chainage_str, discharge, level, h, speed, qd, dd };
                    strlocation_infos.Add(section_info);
                }
                statistics_res.Add(reach_name, strlocation_infos);
            }

            return statistics_res;
        }

        //组合成键值对
        public static Dictionary<float, float> Get_ZD_Dic(List<float> chainages, List<float> values)
        {
            Dictionary<float, float> res = new Dictionary<float, float>();
            if (chainages.Count != values.Count) return null;
            for (int i = 0; i < chainages.Count; i++)
            {
                res.Add(chainages[i], values[i]);
            }

            return res;
        }

        //获取断面名称
        public static Dictionary<float, string> Get_Section_Names(HydroModel model, string reachname, List<float> section)
        {
            Dictionary<float, string> section_name = new Dictionary<float, string>();
            for (int i = 0; i < section.Count; i++)
            {
                float section_chainage = section[i];
                string name = "";

                //获取
                for (int j = 0; j < WG_INFO.Str_BaseInfo.Count; j++)
                {
                    //先判断河道
                    string reach_name = WG_INFO.Str_BaseInfo.ElementAt(j).Value.reach_name;
                    if (reachname != reach_name) continue;

                    //断面桩号与建筑物相近 则认为是建筑物
                    double chainage = WG_INFO.Str_BaseInfo.ElementAt(j).Value.chainage;
                    if (Math.Abs(chainage - section_chainage) < 0.1)
                    {
                        name = WG_INFO.Str_BaseInfo.ElementAt(j).Value.cn_name;
                        break;
                    }
                }
                if (!section_name.Keys.Contains(section_chainage)) section_name.Add(section_chainage, name);
            }

            return section_name;
        }

        #endregion **********************************************************
    }
}
