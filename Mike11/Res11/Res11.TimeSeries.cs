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
        //去掉前面提前小时数的 各时刻数据1
        public static void Remove_Ahead_Res(HydroModel hydromodel, ref Dictionary<DateTime, double> result_dic)
        {
            if (result_dic.Count == 0) return;

            int remove_steps = (hydromodel.ModelGlobalPars.Ahead_hours * 60) / (int)hydromodel.Mike11Pars.Mike11_savetimestep;
            if(remove_steps ==0 && result_dic.ElementAt(0).Key.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin).TotalHours != 0)
            {
                remove_steps = result_dic.Keys.ToList().IndexOf(hydromodel.ModelGlobalPars.Simulate_time.Begin);
            }

            for (int i = 0; i < remove_steps; i++)
            {
                DateTime now_time = result_dic.ElementAt(0).Key;
                result_dic.Remove(now_time);
            }
        }

        //去掉前面提前小时数的 各时刻数据2
        public static void Remove_Ahead_Res(HydroModel hydromodel, ref Dictionary<DateTime, Dictionary<double, double>> result_dic)
        {
            int remove_steps = (hydromodel.ModelGlobalPars.Ahead_hours * 60) / (int)hydromodel.Mike11Pars.Mike11_savetimestep;

            for (int i = 0; i < remove_steps; i++)
            {
                DateTime now_time = result_dic.ElementAt(0).Key;
                result_dic.Remove(now_time);
            }
        }

        //去掉前面提前小时数的 各时刻数据3
        public static void Remove_Ahead_Res(ref Dictionary<DateTime, double> result_dic,int ahead_hours)
        {
            if (result_dic.Count == 0) return;

            int res_step_saveminutes = result_dic.Count >= 2 ? (int)(result_dic.ElementAt(1).Key.Subtract(result_dic.ElementAt(0).Key).TotalMinutes) : 10;
            int remove_steps = (ahead_hours * 60) / res_step_saveminutes;

            for (int i = 0; i < remove_steps; i++)
            {
                DateTime now_time = result_dic.ElementAt(0).Key;
                result_dic.Remove(now_time);
            }
        }

        //统计所有类型闸门的总水量(万m3,根据流量过程统计)
        public static Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_AllGate_AccumulatedVolume(Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> gate_discharges)
        {
            Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> gate_volumedics = new Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>>();

            for (int i = 0; i < gate_discharges.Count; i++)
            {
                Dictionary<string, Dictionary<DateTime, double>> reach_volumes = new Dictionary<string, Dictionary<DateTime, double>>();

                string reach_name = gate_discharges.ElementAt(i).Key;
                Dictionary<string, Dictionary<DateTime, double>> gate_discharge = gate_discharges.ElementAt(i).Value;
                if (gate_discharge.Count != 3) break;

                Dictionary<DateTime, double> ysl_dic = Get_AccumulatedVolume(gate_discharge.ElementAt(0).Value);
                Dictionary<DateTime, double> fsl_dic = Get_AccumulatedVolume(gate_discharge.ElementAt(1).Value);
                Dictionary<DateTime, double> tsl_dic = Get_AccumulatedVolume(gate_discharge.ElementAt(2).Value);

                reach_volumes.Add("引水量", ysl_dic);
                reach_volumes.Add("分水量", fsl_dic);
                reach_volumes.Add("退水量", tsl_dic);

                gate_volumedics.Add(reach_name, reach_volumes);
            }

            return gate_volumedics;
        }

        //根据流量过程统计水量(万m3)
        public static Dictionary<DateTime, double> Get_AccumulatedVolume(Dictionary<DateTime, double> str_discharge)
        {
            Dictionary<DateTime, double> accumulated_volume = new Dictionary<DateTime, double>();
            accumulated_volume.Add(str_discharge.ElementAt(0).Key, 0);
            double sum_volume = 0;
            for (int j = 1; j < str_discharge.Count; j++)
            {
                double average_q = (str_discharge.ElementAt(j - 1).Value + str_discharge.ElementAt(j).Value) / 2;
                double seconds = str_discharge.ElementAt(j).Key.Subtract(str_discharge.ElementAt(j - 1).Key).TotalSeconds;
                sum_volume += average_q * seconds / 10000.0;
                accumulated_volume.Add(str_discharge.ElementAt(j).Key, Math.Round(sum_volume, 1));
            }

            return accumulated_volume;
        }

        //根据流量过程统计水量(万m3)
        public static double Get_TotalVolume(Dictionary<DateTime, double> str_discharge)
        {
            double sum_volume = 0;
            for (int j = 1; j < str_discharge.Count; j++)
            {
                double average_q = (str_discharge.ElementAt(j - 1).Value + str_discharge.ElementAt(j).Value) / 2;
                double seconds = str_discharge.ElementAt(j).Key.Subtract(str_discharge.ElementAt(j - 1).Key).TotalSeconds;
                sum_volume += average_q * seconds / 10000.0;
            }

            return Math.Round(sum_volume, 3);
        }

        //根据流量过程统计水量(万m3)
        public static double Get_TotalVolume(Dictionary<DateTime, float> str_discharge)
        {
            double sum_volume = 0;
            for (int j = 1; j < str_discharge.Count; j++)
            {
                double average_q = (str_discharge.ElementAt(j - 1).Value + str_discharge.ElementAt(j).Value) / 2;
                double seconds = str_discharge.ElementAt(j).Key.Subtract(str_discharge.ElementAt(j - 1).Key).TotalSeconds;
                sum_volume += average_q * seconds / 10000.0;
            }

            return Math.Round(sum_volume, 3);
        }

        //根据出入库流量过程统计水库水量变化过程(万m3)
        public static Dictionary<DateTime, double> Cal_ResVolumnDic(Dictionary<DateTime, double> inqDic, Dictionary<DateTime, double> outqDic, double initialStorage, int step_minutes = 60)
        {
            Dictionary<DateTime, double> storageDic = new Dictionary<DateTime, double>();
            List<DateTime> timeList = new List<DateTime>(inqDic.Keys);

            double storage = initialStorage;
            for (int i = 0; i < timeList.Count; i++)
            {
                DateTime currentTime = timeList[i];
                double inq = inqDic[currentTime];
                double outq = outqDic[currentTime];
                double netq = inq - outq;
                double deltaStorage = netq * step_minutes * 60 / 10000.0;

                storage += deltaStorage;
                storageDic.Add(currentTime, storage);
            }

            return storageDic;
        }
    }
}
