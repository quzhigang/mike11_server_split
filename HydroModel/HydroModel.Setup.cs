using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.Diagnostics;
using System.IO;
using DHI.PFS;
using Kdbndp;

using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model.Mike21;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;

namespace bjd_model
{
    public partial class HydroModel
    {
        #region 构造函数的方法
        //以基础模型为基础，设置模型参数
        private void SetPars_Basemodel(HydroModel Basemodel, string modelname, string model_faname, SimulateTime simulatetime, CalculateModel cal_model,string model_desc,int ahead_hours =0)
        {
            //模型名和基础模型
            this.Modelname = modelname;
            this.Model_Faname = model_faname;
            this.Modeldesc = model_desc;
            this.Model_state = Model_State.Ready_Calting;
            this.BaseModel = Basemodel;

            //模型全局参数和模型文件地址参数
            this.ModelGlobalPars = new Model_GlobalPars(simulatetime, cal_model,
                 this.BaseModel.ModelGlobalPars.Simulate_timestep, this.BaseModel.ModelGlobalPars.Coordinate_type, ahead_hours);
            this.Modelfiles = new Model_Files(modelname);

            //其他参数 均采用 基础模型参数
            this.RfPars = this.BaseModel.RfPars;
            this.Mike11Pars = this.BaseModel.Mike11Pars;

            //纠正可控建筑物文件夹路径为当前文件夹、基础模型建筑物为时间类型的，如果基础模型模拟时间不在现模型模拟时间范围内，则改为规则调度
            Modify_Timefile(modelname);

            this.Mike21Pars = this.BaseModel.Mike21Pars;
            this.CoupleLinklist = this.BaseModel.CoupleLinklist;

            //如果基础模型不是default,则将基础模型的dfs0、dfs1拷贝过来
            //如果有一个边界条件dfs0不存在，则拷贝基础模型的
            bool dfs0_allexist = Bnd_Dfs0_AllExist();
            if (this.BaseModel.Modelname != Model_Const.DEFAULT_MODELNAME && !dfs0_allexist)
            {
                Dfs0.Rewrite_Dfs0copy_UpdateFile(this);
            }
        }

        //纠正可控建筑物时间序列文件夹路径为当前文件夹
        private void Modify_Timefile(string modelname)
        {
            //获取系统路径
            string sys_path = Model_Const.Get_Sysdir();

            //如果基础模型模拟时间不在现模型模拟时间范围内，则改为规则调度
            bool time_include = true;
            //if (this.ModelGlobalPars.Simulate_time.Begin.Subtract(this.BaseModel.ModelGlobalPars.Simulate_time.End).TotalHours >= 0 ||
            //    this.ModelGlobalPars.Simulate_time.End.Subtract(this.BaseModel.ModelGlobalPars.Simulate_time.Begin).TotalHours <= 0)
            //{
            //    time_include = false;
            //}

            //纠正文件夹路径为当前文件夹路径(**注意，这样做基本模型的参数也会被改,不过在前端基本模型会被存储在数据库，需要用时重新给属性赋值**)
            List<Controlstr> constrol_list = this.BaseModel.Mike11Pars.ControlstrList.GateListInfo;
            string dirname = Model_Files.Get_Modeldir(modelname);
            for (int i = 0; i < constrol_list.Count; i++)
            {
                string new_time_filepath = null;
                string time_filename = null;
                if (constrol_list[i] is Normalstr)
                {
                    Normalstr normal_str = constrol_list[i] as Normalstr;
                    time_filename = Path.GetFileName(normal_str.Ddparams_filename);
                    if (time_filename == "") continue;

                    new_time_filepath = dirname + @"\" + time_filename;
                    normal_str.Ddparams_filename = new_time_filepath; //由于是引用类型，可以直接改

                    //如果基础模型模拟时间不在现模型模拟时间范围内，则改为规则调度
                    if (!time_include) normal_str.Strddgz = CtrDdtype.GZDU;
                }
                else if (constrol_list[i] is Fhkstr)
                {
                    Fhkstr fhk_str = constrol_list[i] as Fhkstr;
                    time_filename = Path.GetFileName(fhk_str.time_filename);
                    if (time_filename == "") continue;

                    new_time_filepath = dirname + @"\" + time_filename;
                    fhk_str.time_filename = new_time_filepath;

                    //如果基础模型模拟时间不在现模型模拟时间范围内，则改为规则调度
                    if (!time_include) fhk_str.Strddgz = CtrDdtype.GZDU;
                }
            }
        }

        //从模型文件中获取模型
        public static HydroModel Get_Model_FromFiles(string plan_code, string plan_name, string plan_desc)
        {
            //先构造基础模型(默认模型)
            HydroModel model = new HydroModel();

            //默认模型的参数先初始化
            HydroModel Basemodel = new HydroModel();// HydroModel.Load(Model_Const.DEFAULT_MODELNAME);
            model.BaseModel = Basemodel;  

            //定位模型的文件夹
            string dirname = Model_Files.Get_Modeldir(plan_code);

            //给模型各属性赋值
            model.Modelname = plan_code;
            model.Model_Faname = plan_name;
            model.Modeldesc = plan_desc;
            model.Modelfiles = Model_Files.Get_Modelfilepath(dirname); //获取模型文件参数类
            model.ModelGlobalPars = new Model_GlobalPars();
            model.RfPars = new RF_Pars();
            model.Mike11Pars = new Mike11_Pars();
            model.Mike21Pars = new Mike21_Pars();

            //从默认模型文件中 更新 默认模型的各参数
            UpdatePars_FromModelFiles(ref model);

            //模型断面数据还是采用基础模型
            model.BaseModel = model;
            //model.Mike11Pars.SectionList = Basemodel.Mike11Pars.SectionList;

            return model;
        }

        //从默认模型文件夹中获取默认模型
        public static HydroModel Get_Default_Model()
        {
            //先构造基础模型(默认模型)
            HydroModel model = new HydroModel();

            //默认模型的参数先初始化
            model.BaseModel = new HydroModel();   //新建一个空模型作为默认模型的基础模型

            //定位模型的文件夹
            string dirname = Model_Files.Get_Modeldir(Model_Const.DEFAULT_MODELNAME);

            //给模型各属性赋值
            model.Modelname = Model_Const.DEFAULT_MODELNAME;
            model.Model_Faname = Model_Const.DEFAULT_FANGANNAME;
            model.Modelfiles = Model_Files.Get_Modelfilepath(dirname); //获取默认模型文件参数类
            model.ModelGlobalPars = new Model_GlobalPars();
            model.RfPars = new RF_Pars();
            model.Mike11Pars = new Mike11_Pars();
            model.Mike21Pars = new Mike21_Pars();

            //从默认模型文件中 更新 默认模型的各参数
            UpdatePars_FromModelFiles(ref model);
            model.Modeldesc = model.Get_ModelDesc();

            //基础模型为自身
            model.BaseModel = model;
            return model;
        }

        //从模型文件中 获取模型各参数后 更新模型参数
        private static void UpdatePars_FromModelFiles(ref HydroModel hydromodel)
        {
            //nwk11河道、可控建筑物、计算点、集水区连接等信息获取
            Mike11_Pars Mike11Pars = hydromodel.Mike11Pars;
            if (hydromodel.Modelfiles.Nwk11_filename != "")
            {
                ReachList reachlist = new ReachList();
                Nwk11.GetDefault_ReachInfo(hydromodel.Modelfiles.Nwk11_filename, ref reachlist);
                Nwk11.GetDefault_ComputeChainageList(hydromodel.Modelfiles.Nwk11_filename, ref reachlist);
                Mike11Pars.ReachList = reachlist;

                ControlList controlstrlist = new ControlList();
                Nwk11.GetDefault_GateInfo(hydromodel.Modelfiles.Nwk11_filename, ref controlstrlist);
                Mike11Pars.ControlstrList = controlstrlist;

                Catchment_ConnectList Catchment_Connectlist = new Catchment_ConnectList();
                Nwk11.GetDefault_CatchmentConnectInfo(hydromodel.Modelfiles.Nwk11_filename, ref Catchment_Connectlist);
                Mike11Pars.Catchment_Connectlist = Catchment_Connectlist;
            }

            //Xns11断面数据获取 --不保持模型断面，在加载模型的时候从全局参数中获取
            if (hydromodel.Modelfiles.Xns11_filename != "")
            {
                SectionList Sectionlist =  new SectionList();
                Xns11.GetDefault_SectionInfo(hydromodel.Modelfiles.Xns11_filename, ref Sectionlist);
                Mike11Pars.SectionList = Sectionlist;
            }

            //从mesh中获取mesh参数
            if (hydromodel.Modelfiles.Mesh_filename != "")
            {
                MeshPars Meshparslist = hydromodel.Mike21Pars.MeshParsList;
                Mesh.GetDefault_MeshInfo(hydromodel.Modelfiles.Mesh_filename, ref Meshparslist);
            }

            //m21fm相关二维通用参数、dike、weir 和 部分全局参数(坐标投影)获取
            Model_GlobalPars GlobalPars = hydromodel.ModelGlobalPars;
            if (hydromodel.Modelfiles.M21fm_filename != "")
            {
                M21fm.GetDefault_M21fmParameters(ref hydromodel);
            }

            //Bnd11一维边界条件获取
            if (hydromodel.Modelfiles.Bnd11_filename != "")
            {
                BoundaryList Boundarylist = new BoundaryList();
                Bnd11.GetDefault_Bnd11Info(hydromodel.Modelfiles.Bnd11_filename, ref Boundarylist);
                Mike11Pars.BoundaryList = Boundarylist;
            }

            //Hd11一维HD参数获取
            if (hydromodel.Modelfiles.Hd11_filename != "")
            {
                Hd11_ParametersList Hd11_Pars = new Hd11_ParametersList();
                Hd11.GetDefault_Hd11Info(hydromodel.Modelfiles.Hd11_filename, ref Hd11_Pars);
                Mike11Pars.Hd_Pars = Hd11_Pars;
            }

            //Hd11一维AD参数获取
            if (hydromodel.Modelfiles.Ad11_filename != "")
            {
                Ad11_ParametersList Ad11_Pars = new Ad11_ParametersList();
                Ad11.GetDefault_Ad11Info(hydromodel.Modelfiles.Ad11_filename, ref Ad11_Pars);
                Mike11Pars.Ad_Pars = Ad11_Pars;
            }

            //Simulate全局参数和部分一维参数获取
            if (hydromodel.Modelfiles.Simulate_filename != "")
            {
                Sim11.GetDefault_M11SimulatePars(hydromodel.Modelfiles.Simulate_filename, ref GlobalPars, ref Mike11Pars);
            }

            //RR11流域获取
            if (hydromodel.ModelGlobalPars.Select_model == CalculateModel.only_rr ||
                 hydromodel.ModelGlobalPars.Select_model == CalculateModel.rr_and_hd ||
                  hydromodel.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood)
            {
                RR11.GetDefault_AverageRF();             //流域各月平均降雨
                RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
                RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
                RR11.GetCatchmentInfo(hydromodel.Modelfiles.Rr11_filename, ref hydromodel);   //流域详细信息
            }

            //couple耦合连接获取
            if (hydromodel.Modelfiles.Couple_filename != "")
            {
                CoupleLinkList Couplelinklist = new CoupleLinkList();
                Couple.GetCoupleInfo(hydromodel.Modelfiles.Couple_filename, ref Couplelinklist);
                hydromodel.CoupleLinklist = Couplelinklist;
            }
        }

        //无参初始化
        private void SetDefault()
        {
            this.Modelname = null;
            this.Model_Faname = null;
            this.Modeldesc = null;
            this.Model_state = Model_State.Ready_Calting;
            this.BaseModel = null;
            this.Modelfiles = null;
            this.Mike11Pars = null;
            this.Mike21Pars = null;
            this.ModelGlobalPars = null;
            this.RfPars = null;
            this.CoupleLinklist = null;
        }

        //根据设计洪水选择计算模拟时间
        private SimulateTime Getsimulatetime_FromDesinFlood(DesignRF_Info Designrf_info)
        {
            SimulateTime simulatetime;
            simulatetime.Begin = new DateTime(2020, 7, 1, 0, 0, 0);

            DesignRF_TimeSpan floodtimespan = Designrf_info.designrf_timespan;

            //降雨时间+3天为模拟结束时间
            int days = (int)floodtimespan + 3;
            simulatetime.End = simulatetime.Begin.AddDays(days);

            return simulatetime;
        }
        #endregion
    }
}

