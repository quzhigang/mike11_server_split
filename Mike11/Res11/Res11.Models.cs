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
    //洪水GIS结果类型
    [Serializable]
    public enum Mike11FloodRes_GisType
    {
        water_polygon,           //三维过程水面
        reach_centerline        //带属性三维线
    }

    //一维河道结果
    [Serializable]
    public enum mike11_restype
    {
        Discharge,           //流量
        Water_Level,         //水位
        Velocity,        //断面平均流速
        Volume,                //水量
        Concentration         //水质浓度
    }

    //一维结果数据节点类型
    [Serializable]
    public enum Grid_value
    {
        h_grid,      //仅水位点
        q_grid,      //仅流量点
        all_grid     //全部节点
    }

    //产汇流结果
    [Serializable]
    public enum RF_restype
    {
        RunOff,
        NetRainfall
    }

    //一维还是二维
    [Serializable]
    public enum Mike11_Mike21
    {
        mike11,
        mike21
    }

    //河道结果结构体
    [Serializable]
    public struct mike11_res
    {
        public double Water_Level;        //水位
        public double Water_H;           //最大渠道水深
        public double Average_Speed;    //断面平均流速
        public double Discharge;        //流量

        public static mike11_res Get_ReachRes(double water_Level, double water_H, double velocity, double discharge)
        {
            mike11_res res;
            res.Water_Level = water_Level;
            res.Water_H = water_H;
            res.Average_Speed = velocity;
            res.Discharge = discharge;
            return res;
        }
    }

    //水量结果统计 -- 一维或二维
    [Serializable]
    public struct water_volume_balance
    {
        //总量
        public double initial_volume;     //初始水量
        public double final_volume;       //最终水量

        public double total_inflow;       //总流入
        public double total_outflow;      //总流出


        public static water_volume_balance Get_Default_Res()
        {
            water_volume_balance result;
            result.final_volume = 0;
            result.initial_volume = 0;
            result.total_inflow = 0;
            result.total_outflow = 0;
            return result;
        }
    }

    //流域产汇流模型结果特征值统计
    [Serializable]
    public struct rainfallstatist
    {
        public double catchment_area;  //流域面积

        public double max_runoff;       //最大径流流量
        public DateTime max_runoff_time;  //最大径流流量出现的时间

        public double runnoff_volume;   //总径流量
        public double netrainfall_h;     //总净雨深
    }

    //桩号-值(如纵断面水位数据)
    [Serializable]
    public class Chainage_Value
    {
        public Chainage_Value(double chainage, double value)
        {
            this.chainage = (float)chainage;
            this.value = (float)value;
        }

        public Chainage_Value(float chainage, float value)
        {
            this.chainage = chainage;
            this.value = value;
        }

        public float chainage;
        public float value;
    }

    //纵断面最大最小数据
    [Serializable]
    public class Zdm_MaxMinValue
    {
        public Zdm_MaxMinValue(float max, float min)
        {
            Max = max;
            Min = min;
        }

        public Zdm_MaxMinValue(double max, double min)
        {
            Max = (float)max;
            Min = (float)min;
        }

        public float Max { get; set; }
        public float Min { get; set; }

    }

    //污染统计最大最小数据
    [Serializable]
    public class AD_MaxMinValue
    {
        public AD_MaxMinValue(double reach_max, double volume_max, double concentration_max)
        {
            this.reach_max = reach_max;
            this.volume_max = volume_max;
            this.concentration_max = concentration_max;
        }

        public double reach_max;
        public double volume_max;
        public double concentration_max;
    }

    //闸门状态结果
    [Serializable]
    public class Gate_State_Res
    {
        public Gate_State_Res(string gate_name, Gate_State gate_state)
        {
            this.Gate_name = gate_name;
            this.Gate_state = gate_state;
        }

        public string Gate_name;
        public Gate_State Gate_state;
    }

    //res11ad的GIS结果数据
    [Serializable]
    public class Res11AD_GisRes
    {
        public Res11AD_GisRes(List<PointXY> ad_ploygon, double max_con, double ave_level, PointXY Ad_Location)
        {
            this.Ad_Ploygon = ad_ploygon;
            this.Max_Con = max_con;
            this.Ave_Level = ave_level;
            this.Ad_Location = Ad_Location;
            //this.AdPloygon_GisRes = adPloygon;
            //this.AdLocation_GisRes = adLocation;
        }

        //污染物多边形
        public List<PointXY> Ad_Ploygon
        { get; set; }

        //污染物前端点
        public PointXY Ad_Location
        { get; set; }

        //最大浓度
        public double Max_Con
        { get; set; }

        //平均水位
        public double Ave_Level
        { get; set; }

        ////完整的水质多边形要素对象
        //public Gis_Polygon AdPloygon_GisRes
        //{ get; set; }

        ////完整的水质前锋位置点要素对象
        //public Gis_Point AdLocation_GisRes
        //{ get; set; }
    }

    //res11hd的GIS结果数据
    [Serializable]
    public class Res11HD_GisRes
    {
        public Res11HD_GisRes(List<Reach_Segment> segment_list, List<List<PointXY>> hd_ploygon_list, List<double> ave_level_list)
        {
            this.Segment_list = segment_list;
            this.Hd_Ploygon_list = hd_ploygon_list;
            this.Ave_Level_list = ave_level_list;
            //this.HdPloygon_GisRes = hdploygon;
        }

        //渠段起止桩号
        public List<Reach_Segment> Segment_list
        { get; set; }

        //渠段多边形
        public List<List<PointXY>> Hd_Ploygon_list
        { get; set; }

        //平均水位
        public List<double> Ave_Level_list
        { get; set; }

        //完整的水面多边形要素对象
        //public Gis_Polygon HdPloygon_GisRes
        //{ get; set; }
    }

    //水库的洪水模拟预报结果
    [Serializable]
    public class Reservoir_FloodRes
    {
        //构造函数
        public Reservoir_FloodRes()
        {
            Stcd = "";
            ResName = "";
            Max_Level = 0;
            Max_Volumn = 0;
            MaxLevel_Time = DateTime.Now;
            Max_InQ = 0;
            Max_OutQ = 0;
            MaxInQ_Time = DateTime.Now;
            MaxOutQ_Time = DateTime.Now;
            Total_InVolumn = 0;
            Total_OutVolumn = 0;
            YHDTotal_OutVolumn = 0;
            XHDTotal_OutVolumn = 0;
            EndTime_Level = 0;
            EndTime_Volumn = 0;
            InQ_Dic = new Dictionary<DateTime, double>();
            OutQ_Dic = new Dictionary<DateTime, double>();
            YHDOutQ_Dic = new Dictionary<DateTime, double>();
            XHDOutQ_Dic = new Dictionary<DateTime, double>();
            Level_Dic = new Dictionary<DateTime, double>();
            Volumn_Dic = new Dictionary<DateTime, double>();
        }

        public Reservoir_FloodRes(string stcd, string name, double max_Level, double max_Volumn, DateTime maxLevel_Time, double max_InQ, double max_OutQ,
            double total_InVolumn, double total_OutVolumn, double yHDTotal_OutVolumn, double xHDTotal_OutVolumn, double endTime_Level,
            double endTime_Volumn, Dictionary<DateTime, double> inQ_Dic, Dictionary<DateTime, double> outQ_Dic, DateTime maxinq_time, DateTime maxoutq_time,
            Dictionary<DateTime, double> yHDOutQ_Dic, Dictionary<DateTime, double> xHDOutQ_Dic, Dictionary<DateTime, double> level_Dic,
            Dictionary<DateTime, double> volumn_Dic)
        {
            Stcd = stcd;
            ResName = name;
            Max_Level = max_Level;
            Max_Volumn = max_Volumn;
            MaxLevel_Time = maxLevel_Time;
            Max_InQ = max_InQ;
            Max_OutQ = max_OutQ;
            MaxInQ_Time = maxinq_time;
            MaxOutQ_Time = maxoutq_time;
            Total_InVolumn = total_InVolumn;
            Total_OutVolumn = total_OutVolumn;
            YHDTotal_OutVolumn = yHDTotal_OutVolumn;
            XHDTotal_OutVolumn = xHDTotal_OutVolumn;
            EndTime_Level = endTime_Level;
            EndTime_Volumn = endTime_Volumn;
            InQ_Dic = inQ_Dic;
            OutQ_Dic = outQ_Dic;
            YHDOutQ_Dic = yHDOutQ_Dic;
            XHDOutQ_Dic = xHDOutQ_Dic;
            Level_Dic = level_Dic;
            Volumn_Dic = volumn_Dic;
        }

        //属性
        public string Stcd { get; set; }   //水库stcd
        public string ResName { get; set; }   //水库中文名称
        public double Max_Level { get; set; }  //最高水位
        public double Max_Volumn { get; set; }  //最大库容
        public DateTime MaxLevel_Time { get; set; }  //最高水位出现时间
        public double Max_InQ { get; set; }      //最大入库流量
        public double Max_OutQ { get; set; }     //最大出库流量
        public DateTime MaxInQ_Time { get; set; }      //最大入库流量出现时间
        public DateTime MaxOutQ_Time { get; set; }     //最大出库流量出现时间
        public double Total_InVolumn { get; set; }  //总入库量
        public double Total_OutVolumn { get; set; }  //总出库量
        public double YHDTotal_OutVolumn { get; set; }  //溢洪道总出库量
        public double XHDTotal_OutVolumn { get; set; }  //泄洪洞总出库量
        public double EndTime_Level { get; set; }     //最终水位
        public double EndTime_Volumn { get; set; }    //最终库容

        public Dictionary<DateTime, double> InQ_Dic { get; set; }  //入库流量过程
        public Dictionary<DateTime, double> OutQ_Dic { get; set; }  //出库流量过程
        public Dictionary<DateTime, double> YHDOutQ_Dic { get; set; }  //溢洪道出库流量过程
        public Dictionary<DateTime, double> XHDOutQ_Dic { get; set; }  //泄洪洞出库流量过程
        public Dictionary<DateTime, double> Level_Dic { get; set; }  //库水位过程
        public Dictionary<DateTime, double> Volumn_Dic { get; set; }  //库容过程(河段水量)
    }

    //河道特征断面的洪水模拟预报结果
    [Serializable]
    public class ReachSection_FloodRes
    {
        //构造函数
        public ReachSection_FloodRes()
        {
            Max_Qischarge = 0;
            Max_Level = 0;
            MaxQ_AtTime = DateTime.Now;
            Total_Flood = 0;
            Level_Dic = new Dictionary<DateTime, double>();
            Discharge_Dic = new Dictionary<DateTime, double>();
        }

        public ReachSection_FloodRes(string stcd, string name, double max_Qischarge, double max_Level, DateTime maxQ_AtTime, double total_Flood,
            Dictionary<DateTime, double> level_Dic, Dictionary<DateTime, double> discharge_Dic)
        {
            Stcd = stcd;
            SectionName = name;
            Max_Qischarge = max_Qischarge;
            Max_Level = max_Level;
            MaxQ_AtTime = maxQ_AtTime;
            Total_Flood = total_Flood;
            Level_Dic = level_Dic;
            Discharge_Dic = discharge_Dic;
        }

        //属性
        public string Stcd { get; set; }          //断面stcd
        public string SectionName { get; set; }   //断面名称
        public double Max_Qischarge { get; set; }  //洪峰流量
        public double Max_Level { get; set; }  //最高水位
        public DateTime MaxQ_AtTime { get; set; }  //洪峰流量到达时间
        public double Total_Flood { get; set; }  //断面总过洪量

        public Dictionary<DateTime, double> Level_Dic { get; set; }  //水位过程
        public Dictionary<DateTime, double> Discharge_Dic { get; set; }  //流量过程
    }

    //蓄滞洪区的洪水模拟结果
    [Serializable]
    public class Xzhq_FloodRes
    {
        //构造函数
        public Xzhq_FloodRes()
        {
            Stcd = "";
            Name = "";
            Xzhq_State = "未启用";
            Start_FloodTime = "";
            Max_Volumn = 0;
            Max_Level = "";
            Max_Area = 0;
            Max_FloodTime = 0;
            MaxVolumn_Time = "";
            Max_FhyInQ = new Dictionary<string, double>();
            InQ_Dic = new Dictionary<string, Dictionary<DateTime, double>>();
            Total_InQ_Dic = new Dictionary<DateTime, double>();
            OutQ_Dic = new Dictionary<string, Dictionary<DateTime, double>>();
            Total_OutQ_Dic = new Dictionary<DateTime, double>();
            Level_Dic = new Dictionary<DateTime, double>();
            Vomumn_Dic = new Dictionary<DateTime, double>();
            Area_Dic = new Dictionary<DateTime, double>();
        }

        public Xzhq_FloodRes(string stcd, string name, string xzhq_state,string start_FloodTime,
            double max_Volumn, string max_Level,double max_h, double max_Area, double max_FloodTime,
            string maxVolumn_Time, Dictionary<string, double> max_FhyInQ, Dictionary<string, Dictionary<DateTime, double>> inQ_Dic, 
            Dictionary<DateTime, double> total_InQ_Dic, Dictionary<string, Dictionary<DateTime, double>> outQ_Dic, 
            Dictionary<DateTime, double> total_OutQ_Dic, Dictionary<DateTime, double> level_Dic, 
            Dictionary<DateTime, double> volumn_Dic, Dictionary<DateTime, double> area_Dic, Dictionary<DateTime, string> xzhq_state_dic)
        {
            Stcd = stcd;
            Name = name;
            Xzhq_State = xzhq_state;
            Start_FloodTime = start_FloodTime;
            Max_Volumn = max_Volumn;
            Max_Level = max_Level;
            Max_Depth = max_h;
            Max_Area = max_Area;
            Max_FloodTime = max_FloodTime;
            MaxVolumn_Time = maxVolumn_Time;
            Max_FhyInQ = max_FhyInQ;
            InQ_Dic = inQ_Dic;
            Total_InQ_Dic = total_InQ_Dic;
            OutQ_Dic = outQ_Dic;
            Total_OutQ_Dic = total_OutQ_Dic;
            Level_Dic = level_Dic;
            Vomumn_Dic = volumn_Dic;
            Area_Dic = area_Dic;
            Xzhq_State_Dic = xzhq_state_dic;
        }

        //属性
        public string Stcd { get; set; }                 //蓄滞洪区ID
        public string Name { get; set; }                 //蓄滞洪区名称
        public string Xzhq_State { get; set; }       //蓄滞洪区启用状态
        public string Start_FloodTime { get; set; }    //启用时间
        public double Max_Volumn { get; set; }           //最大滞洪量
        public string Max_Level { get; set; }            //最高滞洪水位
        public double Max_Depth { get; set; }            //最大淹没水深
        public double Max_Area { get; set; }             //最大淹没面积
        public double Max_FloodTime { get; set; }        //最大淹没历时(小时)
        public string MaxVolumn_Time { get; set; }     //最大滞洪量出现时间
        public Dictionary<string, double> Max_FhyInQ { get; set; }              //分洪堰 最大分洪流量

        public Dictionary<string, Dictionary<DateTime, double>> InQ_Dic { get; set; }      //各进洪口 进洪流量过程
        public Dictionary<DateTime, double> Total_InQ_Dic { get; set; }                    //各进洪口 合计进洪流量过程
        public Dictionary<string, Dictionary<DateTime, double>> OutQ_Dic { get; set; }     //各退洪口 退洪流量过程
        public Dictionary<DateTime, double> Total_OutQ_Dic { get; set; }        //各退洪口 合计退洪流量过程
        public Dictionary<DateTime, double> Level_Dic { get; set; }             //滞蓄水位过程
        public Dictionary<DateTime, double> Vomumn_Dic { get; set; }           //滞蓄洪量过程
        public Dictionary<DateTime, double> Area_Dic { get; set; }           //淹没面积过程
        public Dictionary<DateTime, string> Xzhq_State_Dic { get; set; }       //蓄滞洪区启用状态过程

        //获取蓄滞洪区启用状态结果
        public static Dictionary<string, string> Get_Xzhq_State(Dictionary<DateTime, Dictionary<string, string>> xzhq_alltime_res)
        {
            List<string> xzhq_list = xzhq_alltime_res.ElementAt(0).Value.Keys.ToList();

            Dictionary<string, string> res = new Dictionary<string, string>();
            for (int i = 0; i < xzhq_list.Count; i++)
            {
                string xzhq_stcd = xzhq_list[i];
                bool xzhq_state = false;
                for (int j = 0; j < xzhq_alltime_res.Count; j++)
                {
                    if(xzhq_alltime_res.ElementAt(j).Value[xzhq_stcd] == "启用")
                    {
                        xzhq_state = true;
                        break;
                    }
                }
                string state = xzhq_state ? "启用" : "未启用";
                res.Add(xzhq_stcd, state);
            }

            return res;
        }


    }

    //漫堤风险河段结果
    [Serializable]
    public class Reach_MDRisk_Result
    {
        public Reach_MDRisk_Result()
        {
            Reach_Name = "";
            Reach_CNname = "";
            Start_Chainage = 0;
            End_Chainage = 0;
            Distance = 0;
            Risk_Alert = "漫堤";
        }

        public Reach_MDRisk_Result(string reach_Name, string reach_CNname,
            double start_Chainage, double end_Chainage, string risk_alert)
        {
            Reach_Name = reach_Name;
            Reach_CNname = reach_CNname;
            Start_Chainage = start_Chainage;
            End_Chainage = end_Chainage;
            Distance = Math.Round(end_Chainage - start_Chainage, 1);
            Risk_Alert = risk_alert;
        }

        //属性
        public string Reach_Name { get; set; }    //河名
        public string Reach_CNname { get; set; }  //河道中文名
        public double Start_Chainage { get; set; }  //起点桩号
        public double End_Chainage { get; set; }  //终点桩号
        public double Distance { get; set; }       //长度
        public string Risk_Alert { get; set; }    //风险告警
    }

    //冲刷风险河段结果
    [Serializable]
    public class Reach_CSRisk_Result
    {
        public Reach_CSRisk_Result()
        {
            Reach_Name = "";
            Reach_CNname = "";
            Start_Chainage = 0;
            End_Chainage = 0;
            Risk_Alert = "冲刷风险";
        }

        public Reach_CSRisk_Result(string reach_Name, string reach_CNname,
            double start_Chainage, double end_Chainage)
        {
            Reach_Name = reach_Name;
            Reach_CNname = reach_CNname;
            Start_Chainage = start_Chainage;
            End_Chainage = end_Chainage;
            Risk_Alert = "冲刷风险";
        }

        //属性
        public string Reach_Name { get; set; }    //河名
        public string Reach_CNname { get; set; }  //河道中文名
        public double Start_Chainage { get; set; }  //起点桩号
        public double End_Chainage { get; set; }  //终点桩号
        public string Risk_Alert { get; set; }    //风险告警
    }

    //倒灌风险河段结果
    [Serializable]
    public class Reach_DGRisk_Result
    {
        public Reach_DGRisk_Result()
        {
            Reach_Name = "";
            Reach_CNname = "";
            Start_Time = new DateTime();
            Max_Discharge = 0;
            Total_Dg_Volumn = 0;
            Total_Dg_Hours = 0;
            Risk_Alert = "倒灌风险";
        }

        public Reach_DGRisk_Result(string reach_Name, string reach_CNname, DateTime start_Time, 
            double max_Discharge, double total_Dg_Volumn, double total_Dg_Hours)
        {
            Reach_Name = reach_Name;
            Reach_CNname = reach_CNname;
            Start_Time = start_Time;
            Max_Discharge = max_Discharge;
            Total_Dg_Volumn = total_Dg_Volumn;
            Total_Dg_Hours = total_Dg_Hours;
            Risk_Alert = "倒灌风险";
        }


        //属性
        public string Reach_Name { get; set; }   //河名
        public string Reach_CNname { get; set; }  //河道中文名
        public DateTime Start_Time { get; set; }  //倒灌开始时间
        public double Max_Discharge { get; set; }  //倒灌最大流量
        public double Total_Dg_Volumn { get; set; } //累计倒灌洪量
        public double Total_Dg_Hours { get; set; }  //倒灌持续时间(小时)
        public string Risk_Alert { get; set; }    //风险告警
    }

    //缺口漫溢风险结果
    [Serializable]
    public class Reach_GapRisk_Result
    {
        public Reach_GapRisk_Result()
        {
            Reach_Name = "";
            Reach_CNname = "";
            Gap_Name = "";
            Gap_Chainage = 0;
            Max_Level = 0;
            Left_Gap_Datumn = 0;
            Right_Gap_Datumn = 0;
            Risk_Alert = "";
        }

        public Reach_GapRisk_Result(string reach_Name, string reach_CNname, string gap_Name,double gap_Chainage,
            double max_Level, double left_Gap_Datumn, double right_Gap_Datumn, string risk_Alert)
        {
            Reach_Name = reach_Name;
            Reach_CNname = reach_CNname;
            Gap_Name = gap_Name;
            Gap_Chainage = gap_Chainage;
            Max_Level = max_Level;
            Left_Gap_Datumn = left_Gap_Datumn;
            Right_Gap_Datumn = right_Gap_Datumn;
            Risk_Alert = risk_Alert;
        }

        //属性
        public string Reach_Name { get; set; }   //河名
        public string Reach_CNname { get; set; }  //河道中文名
        public string Gap_Name { get; set; }  //缺口中文名称
        public double Gap_Chainage { get; set; }  //缺口所在桩号位置
        public double Max_Level { get; set; }  //缺口处河道最高水位
        public double Left_Gap_Datumn { get; set; }  //左岸缺口底高程
        public double Right_Gap_Datumn { get; set; }  //右岸缺口底高程
        public string Risk_Alert { get; set; }    //风险告警
    }

    //闸门状态枚举
    [Serializable]
    public enum Gate_State
    {
        open = 1,
        close = 2,
        opening = 3,
        closing = 4
    }
}
