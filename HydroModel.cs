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
    //前端一个计算方案对应一个HydroModel对象
    //类的前半部分 -- 构造函数和属性
    [Serializable]
    public partial class HydroModel
    {
        #region 构造函数
        //空模型
        public HydroModel()
        {
            SetDefault();
        }

        //无基础模型的构造函数
        public HydroModel(string modelname, string model_faname, SimulateTime simulatetime, CalculateModel cal_model, string model_desc)
        {
            //将基础模型设置为默认模型
            HydroModel Basemodel = HydroModel.Load(Model_Const.DEFAULT_MODELNAME);

            //设置模型参数
            SetPars_Basemodel(Basemodel, modelname, model_faname, simulatetime, cal_model, model_desc);

            //其他参数重新初始化
            this.RfPars = new RF_Pars();
            this.Mike11Pars = new Mike11_Pars();
            this.Mike21Pars = new Mike21_Pars();
            this.CoupleLinklist = new CoupleLinkList();
        }

        //有基础模型的构造函数
        public HydroModel(string Base_modelname, string modelname, string model_faname, SimulateTime simulatetime, CalculateModel cal_model,string model_desc,int ahead_hours)
        {
            //从数据库加载模型
            HydroModel Basemodel = HydroModel.Load(Base_modelname);
            if (Basemodel == null) return;

            //设置模型参数
            SetPars_Basemodel(Basemodel, modelname, model_faname, simulatetime, cal_model, model_desc, ahead_hours);
        }
        #endregion


        #region 属性
        //模型名称(相当于模型ID，除默认模型外，其他自动生成)
        public string Modelname { get; set; }

        //模型方案名(显示在前端和存储在数据库用)
        public string Model_Faname { get; set; }

        //模型描述
        public string Modeldesc { get; set; }

        //模型状态
        public Model_State Model_state { get; set; }

        //基础模型 -- 每个模型都必须有个基础模型
        public HydroModel BaseModel { get; set; }

        //模型文件地址
        public Model_Files Modelfiles { get; set; }

        //模型全局参数
        public Model_GlobalPars ModelGlobalPars { get; set; }

        //产汇流参数
        public RF_Pars RfPars { get; set; }

        //一维参数
        public Mike11_Pars Mike11Pars { get; set; }

        //二维参数
        public Mike21_Pars Mike21Pars { get; set; }

        //耦合参数
        public CoupleLinkList CoupleLinklist { get; set; }  //一二维耦合连接结合
        #endregion

        public string Get_ModelDesc()
        {
            return Get_ModelDesc(this);
        }

        //返回模型描述 -- 采用格式化的描述方式
        public static string Get_ModelDesc(HydroModel model)
        {
            List<string> model_desc = new List<string>();

            //模拟时间
            string begintime = model.ModelGlobalPars.Simulate_time.Begin.ToString();
            string endtime = model.ModelGlobalPars.Simulate_time.End.ToString();
            model_desc.Add("模拟时间：开始:" + begintime + "  结束:" + endtime);

            //计算步长
            model_desc.Add("计算步长：" + (int)model.ModelGlobalPars.Simulate_timestep + " 秒");

            //计算洪水类型
            string flood_type = "";
            switch (model.ModelGlobalPars.Simulate_floodtype)
            {
                case SimulateFloodType.Design:
                    flood_type = "设计洪水";
                    break;
                case SimulateFloodType.History:
                    flood_type = "历史洪水";
                    break;
                case SimulateFloodType.Realtime_Forecast:
                    flood_type = "实时预报洪水";
                    break;
            }
            if (model.ModelGlobalPars.Select_model == CalculateModel.ad_and_hd)
            {
                flood_type = "无";
            }
            model_desc.Add("洪水类型：" + flood_type);

            //采用的计算模型
            string model_type = "";
            switch (model.ModelGlobalPars.Select_model)
            {
                case CalculateModel.only_rr:
                    model_type = "产汇流模型";
                    break;
                case CalculateModel.only_hd:
                    model_type = "水动力一维模型";
                    break;
                case CalculateModel.rr_and_hd:
                    model_type = "产汇流、水动力一维耦合模型";
                    break;
                case CalculateModel.ad_and_hd:
                    model_type = "水质、水动力一维耦合模型";
                    break;
                case CalculateModel.only_m21:
                    model_type = "水动力二维模型";
                    break;
                case CalculateModel.rr_hd_flood:
                    model_type = "产汇流、水动力一维、二维耦合模型";
                    break;
            }
            model_desc.Add("模型组合：" + model_type);

            //采用的产汇流模型
            string RFmodel = "无";
            if (model.RfPars.Catchmentlist.Catchment_infolist != null &&
                (model.ModelGlobalPars.Select_model == CalculateModel.only_rr ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_and_hd ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood))
            {
                switch (model.RfPars.Catchmentlist.Catchment_infolist[0].Now_RfmodelType)
                {
                    case RFModelType.NAM:
                        RFmodel = "NAM";
                        break;
                    case RFModelType.UHM:
                        RFmodel = "单位线";
                        break;
                    case RFModelType.XAJ:
                        RFmodel = "新安江";
                        break;
                }
            }
            model_desc.Add("产汇流模型：" + RFmodel);

            string resultstr = "";
            for (int i = 0; i < model_desc.Count; i++)
            {
                resultstr += model_desc[i] + " ";
            }
            return resultstr;
        }


        #region 模型文件操作 -- 更新各模型文件
        //更新各模型文件
        public void Write_ModelFiles()
        {
            ////如果有一个边界条件dfs0不存在，则拷贝基础模型的(如果中间改了产汇流、溃堤等边界条件，这样做会导致新边界条件被老的覆盖！)
            //bool dfs0_allexist = Bnd_Dfs0_AllExist();
            //if (this.BaseModel.Modelname != Model_Const.DEFAULT_MODELNAME && !dfs0_allexist)
            //{
            //    Dfs0.Rewrite_Dfs0copy_UpdateFile(this);
            //}

            RR11.Rewrite_RR11_UpdateFile(this);

            Nwk11.Rewrite_Nkw11_UpdateFile(this);
            Xns11.Rewrite_Xns11copy_UpdateFile(this);

            Mesh.Rewrite_Mesh_UpdateFile(this);
            M21fm.Rewrite_M21fm_UpdateFile(this);

            Bnd11.Rewrite_Bnd11_UpdateFile(this);
            Hd11.Rewrite_Hd11_UpdateFile(this);
            Ad11.Rewrite_Ad11_UpdateFile(this);
            Sim11.RewriteSimulate_SelectModel(this);

            Couple.Rewrite_Couple_UpdateFile(this);

            Console.WriteLine("模型构建成功！");
            Console.WriteLine("");
        }

        //判断模型的边界条件dfs0是否都存在
        public bool Bnd_Dfs0_AllExist()
        {
            bool dfs0_allexist = true;
            List<Reach_Bd> boundary_infolist = this.Mike11Pars.BoundaryList.Boundary_infolist;
            for (int i = 0; i < boundary_infolist.Count; i++)
            {
                if(boundary_infolist[i].Valuetype == BdValueType.TS_File && !File.Exists(boundary_infolist[i].Bd_filename))
                {
                    dfs0_allexist = false;
                    return dfs0_allexist;
                }
            }
            return dfs0_allexist;
        }

        //更新调度、污染源指定相关资源
        public void Write_Model_PartFiles()
        {
            Dfs0.Rewrite_Dfs0copy_UpdateFile(this);
            Nwk11.Rewrite_Nkw11_UpdateFile(this);
            Bnd11.Rewrite_Bnd11_UpdateFile(this);

            Console.WriteLine("部分模型文件生成成功！");
        }
        #endregion


        #region 模型全局参数操作 -- 修改模拟时间、步长、一维保存步数
        //开始模拟(正式) -- 静态方法，返回模拟所需要的大概时间
        public static double Run(string plan_code,bool is_quick = false)
        {
            HydroModel hydromodel = HydroModel.Load(plan_code);

            //开始模拟运算，会先更新模型文件
            hydromodel.Simulate(is_quick);

            //返回模拟所需要的时间(秒)
            double time_hours = hydromodel.ModelGlobalPars.Simulate_time.End.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin).TotalHours;
            double elisp_time = Item_Info.Get_ModelRun_ElispedTime(time_hours);

            //等待模型状态改变后才返回(最多20秒)
            int n = 0;
            while (true)
            {
                Thread.Sleep(1000);
                if (Model_Const.Now_Model_State == Model_State.Iscalting )
                {
                    break;
                }
                else if (n > 20)
                {
                    Console.WriteLine("模型未正常开始计算！");
                    break;
                }
                n++;
            }

            return elisp_time;
        }
        
        //开始模拟 -- 对外接口
        public void Simulate(bool is_quick = false)
        {
            //Start_Simulate();
            //创建子线程开始模拟
            Thread newth = new Thread(() => Start_Simulate(is_quick));
            newth.IsBackground = true;
            newth.Start();
            Console.WriteLine("开始模拟计算........");
        }

        //开始迭代模拟 -- 对外接口
        public double Iter_Simulate()
        {
            //创建子线程开始模拟
            Thread newth = new Thread(Start_IterCal);
            newth.IsBackground = true;
            newth.Start();
            Console.WriteLine("开始迭代计算........");

            //返回模拟所需要的时间(秒)
            double time_hours = this.ModelGlobalPars.Simulate_time.End.Subtract(this.ModelGlobalPars.Simulate_time.Begin).TotalHours;
            double elisp_time = Item_Info.Get_ModelRun_ElispedTime(time_hours) * 3;

            //等待模型状态改变后才返回(最多20秒)
            int n = 0;
            while (true)
            {
                Thread.Sleep(1000);
                if (Model_Const.Now_Model_State == Model_State.Iscalting)
                {
                    break;
                }
                else if (n > 20)
                {
                    Console.WriteLine("模型未正常开始计算！");
                    break;
                }
                n++;
            }

            return elisp_time;
        }

        //通过快速同步模拟，用于优化调度的迭代计算
        public void Quick_Simulate()
        {
            //更新河网文件(每次都会改定闸站调度，所以每次都需要更新)
            if (this.Modelname != Model_Const.DEFAULT_MODELNAME) this.Write_ModelFiles();

            //其他则使用DOS程序调用相应引擎进行计算
            if (Model_Const.Stop_Geting_Mike11Res) return;
            Run_MikeEngine_UseDos();
            Sim11.Write_ProgressInfo(this);

            Console.WriteLine("开始快速模拟计算........");
        }

        //快速模拟提前时间段，并获取主要结果(水库和河道)
        public List<object> Quick_SimulateAhead_GetMainres()
        {
            List<object> res = new List<object>();
            if (this.ModelGlobalPars.Ahead_hours == 0) return null;

            //模型原参数
            int model_ahead_hours = this.ModelGlobalPars.Ahead_hours;
            SimulateTime old_simulatetime = this.ModelGlobalPars.Simulate_time;

            //修改模型模拟结束时间
            SimulateTime new_simulatetime;
            new_simulatetime.Begin = old_simulatetime.Begin;
            new_simulatetime.End = new_simulatetime.Begin.AddHours(model_ahead_hours);
            this.ModelGlobalPars.Simulate_time = new_simulatetime;

            //更新模拟文件,默认模型不更新
            if (this.Modelname != Model_Const.DEFAULT_MODELNAME)
            {
                Write_ModelFiles();
            }

            //使用DOS程序调用相应引擎进行计算
            if (Model_Const.Stop_Geting_Mike11Res) return null;
            Run_MikeEngine_UseDos();
            Sim11.Write_ProgressInfo(this);

            //获取模型实例
            string model_instance = HydroModel.Get_Model_Instance(this.Modelname);
            if (this.Mike11Pars.SectionList == null) this.Mike11Pars.SectionList = Item_Info.Get_Item_SectionDatas(model_instance);
            if (this.Mike11Pars.SectionList.ReachSectionList == null) this.Mike11Pars.SectionList = Item_Info.Get_Item_SectionDatas(model_instance);

            //计算完成后，获取水库和河道断面结果
            this.ModelGlobalPars.Ahead_hours = 0;
            Dictionary<string, Reservoir_FloodRes> res_floodres;
            Dictionary<string, ReachSection_FloodRes> reachsection_res = Res11.Get_Mike11_ResReach_Result(this, out res_floodres);
            res.Add(res_floodres); res.Add(reachsection_res);

            //还原模型全局参数
            this.ModelGlobalPars.Ahead_hours = model_ahead_hours;
            this.ModelGlobalPars.Simulate_time = old_simulatetime;

            return res;
        }

        //停止模拟 -- 直接停止dos程序，也相应停止引擎
        public static void Stop(string plan_code)
        {
            HydroModel hydromodel = HydroModel.Load(plan_code);
            Model_Const.Stop_Geting_Mike11Res = true;
            hydromodel.Stop();
        }

        //停止模拟 -- 直接停止dos程序，也相应停止引擎
        public void Stop()
        {
            //获取引擎名
            string engine_name;
            if (this.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood)
            {
                engine_name = EngineName.FemEngineMFGPUSP.ToString();  //适合Flood的单精度GPU计算
            }
            else if (this.ModelGlobalPars.Select_model == CalculateModel.only_m21)
            {
                engine_name = EngineName.FemEngineHDGPUSP.ToString(); //适合纯mike21fm单精度GPU计算
            }
            else
            {
                engine_name = EngineName.Application.ToString();   //单独非XAJ产汇流、一维河道以及两者耦合 1D模拟
            }

            //获取当前所有正在运行的进程,返回进程类的数组
            Process[] pros = Process.GetProcesses();
            for (int i = 0; i < pros.Length; i++)
            {
                if (pros[i].ProcessName == "cmd")
                {
                    pros[i].Kill();
                }

                if (pros[i].ProcessName == engine_name)
                {
                    pros[i].Kill();
                }
            }

            //更新数据库里的模型状态为待计算
            this.Model_state = Model_State.Ready_Calting;
            string[] model_info = Item_Info.Get_Model_Info(this, "", true);
            Update_ModelStateInfo(this, model_info[4]);
        }

        //获取进度 -- 从进度文本中获取(**应用程序必须按照一定频率不停的调用这个服务接口以获取实时进度**)
        public ProgressInfo GetProgress()
        {
            HydroModel hydromodel = this;
            return Sim11.Get_NowProgress_Realtime(hydromodel);
        }

        //显示进度--在屏幕上实时显示模拟进度(直到模拟结束才停止)
        public void Show_Progress()
        {
            HydroModel hydromodel = this;
            Sim11.Show_Simulate_ProgressInfo(hydromodel);
        }

        // 修改计算时间步长
        public void Modify_SimulationTimeStep(SimTimeStep new_simtimestep)
        {
            HydroModel hydromodel = this;
            Sim11.Modify_SimulationTimeStep(ref hydromodel, new_simtimestep);
        }

        // 修改一维存储时间步长
        public void Set_Mike11_SaveTimeStep(SaveTimeStep new_savetimestep)
        {
            HydroModel hydromodel = this;
            Sim11.Modify_SaveTimeStep(ref hydromodel, new_savetimestep);
        }

        //修改计算模型类型 -- 默认三者耦合
        public void Modify_ModelType(CalculateModel new_Selectmodel)
        {
            Model_GlobalPars globalpars = this.ModelGlobalPars;
            globalpars.Select_model = new_Selectmodel;
        }
        #endregion



        #region 其他辅助方法
        [DllImport("shell32.dll")]
        private static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, Int32 nShowCmd);

        //利用win32接口方法ShellExecute调用Mike计算引擎 -- 根据模型类型选择引擎
        public void Run_MikeEngine_UseDos()
        {
            //其他则使用DOS程序调用相应引擎进行计算
            string simulate_file;
            string engine_name;
            if (this.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood)
            {
                simulate_file = this.Modelfiles.Couple_filename;
                engine_name = EngineName.FemEngineMFGPUSP + ".exe";  //适合Flood的单精度GPU计算
            }
            else if (this.ModelGlobalPars.Select_model == CalculateModel.only_m21)
            {
                simulate_file = this.Modelfiles.M21fm_filename;
                engine_name = EngineName.FemEngineHDGPUSP + ".exe"; //适合m21fm的单精度GPU计算
            }
            else 
            {
                simulate_file =  this.Modelfiles.Simulate_filename;  //"-s " + 静音方式，mike11.exe计算过程中不弹出对话框，"DHI.Mike1D.Application.exe"没有这个标识
                engine_name = "DHI.Mike1D.Application.exe";   //不知道为什么，用全路径才识别 单独非XAJ产汇流、一维河道以及两者耦合
            }

            ShellExecute(IntPtr.Zero, "open", engine_name, simulate_file, null, 0);  //最后一个参数为0，表示不显示cmd对话框
        }

        //开始模拟 -- 如果是RR_hd_flood则同步处理二维结果数据
        private void Start_Simulate(bool is_quick = false)
        {
            Model_Const.Stop_Geting_Mike11Res = false;
            string start_simulate_time = DateTime.Now.ToString(Const_Global.Model_Const.TIMEFORMAT);

            //更新数据库里的模型状态信息和模型实体
            this.Model_state = Model_State.Iscalting;
            string[] model_info = Item_Info.Get_Model_Info(this, start_simulate_time, true);
            Update_ModelStateInfo(this, model_info, true);
            Model_Const.Now_Model_State = Model_State.Iscalting;

            //先通过一次试算修改水库河道流以还原初始水情
            HydroModel model = this;
            if (this.ModelGlobalPars.Ahead_hours != 0)
            {
                List<object> quick_simulate = Quick_SimulateAhead_GetMainres();
                if (quick_simulate != null) Hd11.Update_ResInitiallevel_FromIter(ref model, quick_simulate);
            }

            //重新更新模拟文件,默认模型不更新
            if (this.Modelname != Model_Const.DEFAULT_MODELNAME)
            {
                Write_ModelFiles();
            }

            //如果是产汇流计算且采用的是XAJ模型，则直接返回结果
            if (this.ModelGlobalPars.Select_model == CalculateModel.only_rr && this.RfPars.Rainfall_selectmodel == RFModelType.XAJ)
            {
                //各流域XAJ模型开始模拟，并制作结果dfs0文件
                RR11.Get_Catchments_XAJSimulateRes(this);
            }
            else
            {
                //其他则使用DOS程序调用相应引擎进行计算
                Run_MikeEngine_UseDos();

                //将进度信息写入文本
                if (this.ModelGlobalPars.Select_model != CalculateModel.only_rr)
                {
                    Sim11.Write_ProgressInfo(this);
                }
            }

            //等2秒
            Thread.Sleep(2000);

            //更新模型状态
            if (File.Exists(this.Modelfiles.Progressinfo_filename))
            {
                if (this.GetProgress().now_progress_value == 100)
                {
                    //进度文件存在，且进度为100，则认为完成
                    this.Model_state = Model_State.Finished;
                }
                else
                {
                    //进度文件存在，但未能达到100则认为模型错误
                    this.Model_state = Model_State.Error;
                }
            }

            //全局变量重置
            if (Model_Const.Stop_Geting_Mike11Res) return;

            //获取模型实例
            string model_instance = HydroModel.Get_Model_Instance(this.Modelname);
            if (this.Mike11Pars.SectionList == null) this.Mike11Pars.SectionList = Item_Info.Get_Item_SectionDatas(model_instance);
            if (this.Mike11Pars.SectionList.ReachSectionList == null) this.Mike11Pars.SectionList = Item_Info.Get_Item_SectionDatas(model_instance);

            //计算完成后，获取一维模型结果
            List<object> mike11_datares = Get_Mike11_AllResult(model_instance, is_quick);

            //将一维模型结果写入数据库
            Res11.Write_Mike11res_IntoDB(this, mike11_datares, model_instance);

            //将GIS过程线的结果和统计GIS线结果 写入该方案数据库
            if(!is_quick) Res11.Write_GCGisLine_TJGisLine_toDB(this, mike11_datares, model_instance);

            //更新模型信息到数据库
            model_info = Item_Info.Get_Model_Info(this, start_simulate_time, false);
            Update_ModelStateInfo(this, model_info[4], true);

            //如果是快速模拟，则到此结束
            if (is_quick) return;

            //写河道过程三维水面geojson文件，并将文件路径结果写入数据库
            Res11.Write_GCPolygonFile_toDB(this, mike11_datares, model_instance);

            //写最大淹没水深分布面要素geojson文件，并将文件路径结果写入数据库(调用python进程)
            string business_code = HydroModel.Get_BusinessCode_FromDB(this.Modelname);
            if (business_code == "flood_dispatch_wg" || business_code == "flood_forecast_wg" || business_code == "optimize_dispatch_wg")
            {
                Res11.Write_TJPolygonFile_toDB(this, mike11_datares, model_instance);
            }

            //如果是自动预报，通过判断，对超过降雨量阈值的自动预报主要结果进行保存
            if (this.Modelname == Model_Const.AUTO_MODELNAME) Res11.Save_History_AutoForcastRes();

            //如果业务模型包含二维，则请求二维模型服务参数设置和计算服务
            List<string> instance_list = Get_Model_Instance_list(this.Modelname);
            if (instance_list.Any(s => s.Contains("mike21"))) Request_Mike21_Server(this, instance_list, model_instance, business_code);
        }
    }
}

