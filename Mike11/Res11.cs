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
        private static object reach_result;

        #region ****************获取水动力结果 ***************************
        //获取模型优化调度判断相关的结果 --调度目标断面的mike11结果(从结果文件)
        public static double Get_TargetSection_Result(HydroModel hydromodel,Dispatch_Target dd_target,List<Water_Condition> now_waterlevel)
        {
            Dictionary<string, ReachSection_FloodRes> reach_floodres = new Dictionary<string, ReachSection_FloodRes>();

            //判断
            if (hydromodel == null || !File.Exists(hydromodel.Modelfiles.Hdres11_filename)) return 0;

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);
            IDataItem dataitem = resdata.Reaches.ElementAt(0).DataItems.ElementAt(0);
            if (dataitem == null) return 0;

            //模型实例
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            Water_Condition targetsection_water_condition = Water_Condition.Get_WaterCondition1(now_waterlevel, dd_target.stcd);

            //调度目标的 河道特征断面洪水结果
            ReachSection_FloodRes section_res = Res11.Get_Section_FloodRes(hydromodel, targetsection_water_condition, resdata);

            return section_res.Max_Qischarge;
        }

        //获取模型水库和河道断面结果
        public static Dictionary<string, ReachSection_FloodRes> Get_Mike11_ResReach_Result(HydroModel hydromodel, out Dictionary<string, Reservoir_FloodRes> res_result)
        {
            Dictionary<string, ReachSection_FloodRes> reach_floodres = new Dictionary<string, ReachSection_FloodRes>();
            res_result = new Dictionary<string, Reservoir_FloodRes>();

            //判断
            if (hydromodel == null || !File.Exists(hydromodel.Modelfiles.Hdres11_filename)) return null;

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);
            IDataItem dataitem = resdata.Reaches.ElementAt(0).DataItems.ElementAt(0);
            if (dataitem == null) return null;

            //水库洪水结果
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            res_result = Res11.Get_AllReservoir_FloodRes(hydromodel, resdata, model_instance);

            //河道洪水结果
            Dictionary<string, ReachSection_FloodRes> reach_result = Res11.Get_ReachSection_FloodRes(hydromodel, resdata, model_instance);

            return reach_result;
        }

        //获取模型水库洪水结果
        public static Dictionary<string, Dictionary<DateTime, double>> Get_Mike11_ResFloodIn_Result(HydroModel hydromodel)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_floodin = new Dictionary<string, Dictionary<DateTime, double>>();

            //判断
            if (hydromodel == null || !File.Exists(hydromodel.Modelfiles.Hdres11_filename)) return null;

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);
            IDataItem dataitem = resdata.Reaches.ElementAt(0).DataItems.ElementAt(0);
            if (dataitem == null) return null;

            //水库洪水结果
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            Dictionary<string, Reservoir_FloodRes> res_result = Res11.Get_AllReservoir_FloodRes(hydromodel, resdata, model_instance);

            //获取水库洪水入流结果
            for (int i = 0; i < res_result.Count; i++)
            {
                Dictionary<DateTime, double> floodin = res_result.ElementAt(i).Value.InQ_Dic;
                res_floodin.Add(res_result.ElementAt(i).Key, floodin);
            }

            return res_floodin;
        }

        //获取水动力结果的时间序列
        public static DateTime[] Get_ResTime_List(HydroModel hydro_model)
        {
            //源文件和项目类型
            string sourcefilename = hydro_model.Modelfiles.Hdres11_filename;

            //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(sourcefilename);

            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //获取时间数组
            DateTime[] timearray = resdata.TimesList.ToArray();

            return timearray;
        }

        #endregion*********************************************************
    }
}
