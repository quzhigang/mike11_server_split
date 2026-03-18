using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using bjd_model.Mike11;
using bjd_model.Const_Global;
using bjd_model.Mike21;
using bjd_model.Common;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using bjd_model.CatchMent;
using System.Dynamic;
using System.Runtime.Serialization.Formatters.Binary;
using Kdbndp;

namespace bjd_model
{
    /// <summary>
    /// Model_Ser 的摘要说明
    /// </summary>
    public class Model_Ser : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            //容许跨域访问
            Cross_Domain_Set(ref context);
            context.Response.ContentType = "text/plain";

            //获取前端传过来的参数(方案名)  --适用于来自web段的get和post请求
            string request_type = context.Request["request_type"] == null ? context.Request.Form["request_type"]:context.Request["request_type"];
            string request_pars = context.Request["request_type"] == null ? context.Request.Form["request_pars"]: context.Request["request_pars"];

            //适用于来自后端的请求，从请求体中获取请求数据
            if(request_type == null)
            {
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    string requestBody = reader.ReadToEnd();
                    Dictionary<string, Object> request_info = JsonConvert.DeserializeObject<Dictionary<string, Object>>(requestBody);
                    if (request_info.Keys.Contains("request_type")) request_type = request_info["request_type"].ToString();
                    if (request_info.Keys.Contains("request_pars")) request_pars = request_info["request_pars"].ToString();
                }
            }

            //设置系统当前路径(即该.ashx网页所在的路径)
            Model_Const.Set_Sysdir(context.Request.MapPath("./"));
            string _data = "";

            //*********************** 系统初始化 **********************   
            if (request_type == "model_init") //模型初始化
            {
                _data = Default_Model_Init();
            }
            //***********************************************************   


            //*********************** 模型新建、删除及修改 **********************   
            if (request_type == "auto_forcast")  //创建自动预报模型并计算
            {
                _data = Auto_Forcast(request_pars);
            }
            else if (request_type == "create_model")  //手工创建模型,返回最新的模型信息集合
            {
                _data = Create_Model(request_pars);
            }
            else if (request_type == "change_model_baseinfo") //修改模型名称和描述和保存时间步长
            {
                _data = Change_ModelBaseinfo(request_pars);
            }
            else if (request_type == "del_model")  //删除模型,返回剩下的模型集合
            {
                _data = Del_Model(request_pars);
            }
            else if (request_type == "run_model")  //计算模型,返回所需的计算时间(second)
            {
                _data = Run_Model(request_pars);
            }
            else if (request_type == "run_model_quick")  //一维快速计算模型,返回所需的计算时间(second)
            {
                _data = Run_Model_Quick(request_pars);
            }
            else if (request_type == "stop_model")  //停止模型计算，返回成功信息
            {
                _data = Stop_Model(request_pars);
            }
            else if (request_type == "modify_initial")  //修改河道初始条件
            {
                _data = Change_Reach_Initial(request_pars);
            }
            else if (request_type == "get_rfmodel")  //获取产汇流模型类型
            {
                _data = Get_Rfmodel(request_pars);
            }
            else if (request_type == "change_rfmodel")  //修改产汇流模型类型
            {
                _data = Change_Rfmodel(request_pars);
            }
            else if (request_type == "change_boundry")  //修改洪水入流边界条件
            {
                _data = Change_Reach_Boundry(request_pars);
            }
            else if (request_type == "modify_gatestate")  //修改闸站调度，返回成功
            {
                _data = Model_ZZDU(request_pars);
            }
            else if (request_type == "change_reach_break")  //修改河堤溃口设置
            {
                _data = Change_ReachBreak(request_pars);
            }
            else if (request_type == "get_reach_break")  //获取河堤溃口设置
            {
                _data = Get_ReachBreak(request_pars);
            }
            else if (request_type == "set_dispatch_target")  //设置优化调度目标参数，返回成功
            {
                _data = Set_Dispatch_Target(request_pars);
            }
            else if (request_type == "iter_cal")  //开始迭代计算，获取库群优化调度信息
            {
                _data = Iter_CalModel(request_pars);
            }
            else if (request_type == "backcal_resdd")  //根据已完成的预报预演方案，反向推演单水库调度方案
            {
                _data = Backcal_Resdd(request_pars);
            }
            else if (request_type == "get_faultgate_baseinfo")  //获取故障闸门基本信息
            {
                _data = Get_FaultGate_BaseInfo(request_pars);
            }
            else if (request_type == "set_fault_gate")  //设置故障闸门
            {
                _data = Set_Fault_Gate(request_pars);
            }
            else if (request_type == "get_fault_gate")  //获取故障闸门信息
            {
                _data = Get_Fault_Gate(request_pars);
            }

            //***********************************************************   


            //*********************** 获取模型及结果信息 **********************   
            if (request_type == "get_models") //获取已有 所有模型信息
            {
                _data = Get_AllModels(request_pars);
            }
            else if (request_type == "get_ddinfo")  //获取模型所有闸门的调度信息
            {
                _data = Get_Model_Ddinfo(request_pars);
            }
            else if (request_type == "get_dispatch_plan")  //获取方案主要控制闸站的 简短调度指令
            {
                _data = Get_Model_DispatchPlan(request_pars);
            }
            else if (request_type == "get_initial_waterlevel")  //获取模型初始水情信息
            {
                _data = Get_Model_InitialLevel(request_pars);
            }
            else if (request_type == "get_bndinfo")  //获取模型边界条件信息
            {
                _data = Get_Model_BndInfo(request_pars);
            }
            else if (request_type == "get_dispatch_target")  //获取优化调度目标设置信息
            {
                _data = Get_Model_DispatchTarget(request_pars);
            }
            else if (request_type == "get_tjdata_result") //获取模型统计data结果
            {
                _data = Get_Mike11_TjResult(request_pars);
            }
            else if (request_type == "get_gcdata_result") //获取模型过程data结果
            {
                _data = Get_Mike11_GcResult(request_pars);
            }
            else if (request_type == "get_gisgc_polygon_result") //获取某时刻gis面结果
            {
                _data = Get_Model_GISGC_PolygonResult(request_pars);
            }
            else if (request_type == "get_sampleline") //获取gis样板线
            {
                _data = Get_Sample_GisLine(request_pars);
            }
            else if (request_type == "get_sampleline_data_result") //获取模型GIS过程线的全过程属性结果
            {
                _data = Get_LineData_Attrs(request_pars);
            }
            else if (request_type == "get_gistj_result") //获取gis统计线结果
            {
                _data = Get_Model_GISTJResult(request_pars);
            }
            else if (request_type == "get_gistj_polygon_result") //获取gis统计面结果(淹没面)
            {
                _data = Get_Model_GISTJ_PolygonResult(request_pars);
            }

            else if (request_type == "get_sound_result")  //获取语音
            {
                _data = Get_Model_SoundResult(request_pars);
            }
            else if (request_type == "get_point_result")  //查询模型某点的结果(时间为""，则返回序列)
            {
                _data = Get_Point_Result(request_pars);
            }
            else if (request_type == "get_zp_result")  //获取某类纵剖面数据
            {
                _data = Get_Mike11_ZpResult(request_pars);
            }
            else if (request_type == "get_xb_result")  //获取最后结果时刻详表
            {
                _data = Get_Mike11_XbResult(request_pars);
            }
            else if (request_type == "get_reachinfo")  //获取模型的河道信息，所有模型一样
            {
                _data = Get_Main_ReachInfo(request_pars);
            }
            else if (request_type == "get_reachsections")  //获取模型的河道H点断面清单
            {
                _data = Get_ReachSections(request_pars);
            }
            else if (request_type == "get_sectionres")  //获取单一河道断面的水位流量过程
            {
                _data = Get_SectionRes(request_pars);
            }
            else if (request_type == "get_sectionlist_res")  //获取多个河道断面的水位流量过程
            {
                _data = Get_SectionList_Res(request_pars);
            }
            else if (request_type == "get_section_discharges")  //获取多个河道断面的流量过程
            {
                _data = Get_Section_Discharges(request_pars);
            }
            else if (request_type == "get_catchment_discharges")  //获取多个子流域的流量过程
            {
                _data = Get_Catchment_Discharges(request_pars);
            }

            else if (request_type == "get_gateres")  //获取单一建筑物的水位流量结果过程
            {
                _data = Get_GateRes(request_pars);
            }
            else if (request_type == "get_sectiondata") //获取单一河道断面的原始断面数据(根据河名和桩号)
            {
                _data = Get_SectionDatas(request_pars);
            }
            else if (request_type == "get_sectiondata_frompoint") //获取单一河道断面的原始断面数据(根据河名和桩号)
            {
                _data = Get_SectionDatas_FromPoint(request_pars);
            }
            else if (request_type == "get_atreach") //根据方案ID获取需要进行流量统计的特征河道断面
            {
                _data = Get_AtReach(request_pars);
            }
            else if (request_type == "get_reachsection_location") //根据方案ID获取需要进行流量统计的特征河道断面
            {
                _data = Get_ReachSection_Location(request_pars);
            }
            else if (request_type == "get_station_info") //获取站点信息，包括各种警戒水位和保证水位
            {
                _data = Get_StationInfo();
            }
            else if (request_type == "get_strddrule_info") //获取建筑物规则调度信息
            {
                _data = Get_Str_DdRuleInfo(request_pars);
            }
            else if (request_type == "get_control_strs") //获取水库、河道闸站、蓄滞洪区的可控建筑物
            {
                _data = Get_Control_Strs(request_pars);
            }
            else if (request_type == "get_gatestate") //获取最新闸站工情
            {
                _data = Get_GateState(request_pars);
            }
            else if (request_type == "get_now_waterinfo") //获取最新水情信息
            {
                _data = Get_NowWaterInfo(request_pars);
            }
            else if (request_type == "get_design_flood") //获取设计洪水信息
            {
                _data = Get_Design_Flood(request_pars);
            }
            else if (request_type == "get_risk_warning") //获取流域全部预警信息
            {
                _data = Get_Risk_Warning(request_pars);
            }
            else if (request_type == "get_nsbd_sectioninfo") //获取南水北调交叉断面信息
            {
                _data = Get_Nsbd_SectionInfo();
            }
            else if (request_type == "get_history_autoforcast_list") //获取历史自动预报清单
            {
                _data = Get_History_AutoForcast_List();
            }
            else if (request_type == "del_history_autoforcast") //删除历史自动预报洪水结果
            {
                _data = Del_History_AutoForcast(request_pars);
            }
            else if (request_type == "get_history_autoforcast_res") //获取历史自动预报结果
            {
                _data = Get_History_AutoForcast_Res(request_pars);
            }
            else if (request_type == "get_business_view") //获取业务模型初始视角
            {
                _data = Get_Business_View(request_pars);
            }
            else if (request_type == "get_mountain_forecast_flood") //获取山区小流域预报洪水
            {
                _data = Get_Mountain_Forecast_Flood(request_pars);
            }
            else if (request_type == "get_rain_flood_list") //获取场次降雨洪水信息清单
            {
                _data = Get_Flood_List();
            }
            else if (request_type == "get_rainflood_plan_list") //获取场次降雨洪水 方案信息清单
            {
                _data = Get_FloodPlan_List(request_pars);
            }
            else if (request_type == "change_rainflood_recomplan") //修改场次洪水推荐方案
            {
                _data = Change_Flood_RecomPlan(request_pars);
            }
            else if (request_type == "important_inspect") //获取工程重点巡查位置
            {
                _data = Get_Important_Inspect_Parts(request_pars);
            }

            else if (request_type == "model_business") //模型相关业务
            {
                _data = Model_Business();
            }
            //***********************************************************   


            //响应
            context.Response.Clear();
            context.Response.Write(_data);
            context.Response.End();
        }

        //系统默认模型初始化
        public string Default_Model_Init()
        {
            //获取模型结果
            HydroModel.Load_DefaultModel_ToDB();

            return "默认模型初始化成功!";
        }

        //用户初始化
        public string User_Init()
        {
            //获取模型结果
            HydroModel.Get_DefaultModel_AndCopytoself_FromDB();

            return "用户初始化成功!";
        }

        #region *******************模型新建、删除及修改************************
        //新建模型
        public string Create_Model(string pars)
        {
            //var pars = ['model_20230513101926','测试方案1111', '2021/07/20 00:00:00', '2021/07/21 00:00:00','1日模拟',20, base_plan_code]; 
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 7 || jar[0].ToString() == "") return null;

            string plan_code = jar[0].ToString();
            string fangan_name = jar[1].ToString();
            string start_timestr = jar[2].ToString();
            string end_timestr = jar[3].ToString();
            string fangan_desc = jar[4].ToString();

            int step_saveminutes;
            if (jar[5].ToString() == "" || jar[5].ToString() == null)
            {
                step_saveminutes = Model_Const.Now_Model_SaveTime;
            }
            else
            {
                step_saveminutes = int.Parse(jar[5].ToString());
            }
            string base_plan_code = jar[6].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            if (model_instance == "")
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案无模型实例！");
                return obj.ToString();
            }

            Mysql_GlobalVar.now_instance = model_instance;

            //更新水情和工情数据库
            Item_Info.Update_LevelTable();
            //HH_INFO.Update_GateStateTable();

            //新建模型(请求一次)
            int expect_seconds = HydroModel.Create_Model_AndGetExpectSeconds(fangan_name, start_timestr, end_timestr, fangan_desc, base_plan_code, plan_code, step_saveminutes);

            //返回信息
            JObject obj1 = new JObject();
            obj1.Add("expect_seconds", expect_seconds);

            return obj1.ToString(); ;
        }

        //创建自动预报模型并计算
        public string Auto_Forcast(string pars)
        {
            //设置当前项目名
            Mysql_GlobalVar.now_instance = "wg_mike11";

            //获取自动模型信息
            //string modelstate = HydroModel.Get_ModelState_FromDB(Model_Const.AUTO_MODELNAME);
            //if(modelstate == "计算中")
            //{
            //    //返回信息
            //    JObject obj1 = new JObject();
            //    obj1.Add("error", "模型计算中，请稍后再试！");
            //    return obj1.ToString();
            //}

            //修改更新各子流域产汇流类型
            RR11.Change_Catchment_RFmodel(Model_Const.AUTO_MODELNAME, "");

            //创建自动预报模型并计算
            HydroModel.CreateRun_AutoForecastModel();

            //返回信息
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }


        //删除模型
        public string Del_Model(string plan_code)
        {
            if (plan_code == "" || plan_code == null)
            {
                JObject obj = new JObject();
                obj.Add("success", true);
                return obj.ToString();
            }

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            if (model_instance == "")
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案无模型实例！");
                return obj.ToString();
            }

            Mysql_GlobalVar.now_instance = model_instance;

            //删除模型
            HydroModel.Delete_ModelPlan(plan_code);

            //获取最新的模型集合
            string _data = Get_AllModels(model_instance);

            return _data;
        }

        //计算模型
        public string Run_Model(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            if (model_instance == "")
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案无模型实例！");
                return obj.ToString();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //返回计算所需的时间(s)
            string _data = HydroModel.Run(plan_code).ToString();

            return _data;
        }

        //快速计算模型  --仅限于纯一维模型，不进行GIS后处理，不提取GIS相关data结果
        public string Run_Model_Quick(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            if (model_instance == "")
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案无模型实例！");
                return obj.ToString();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //返回计算所需的时间(s)
            string _data = HydroModel.Run(plan_code,true).ToString();

            return _data;
        }

        //停止计算
        public string Stop_Model(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return Get_SuccessObj();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            if (model_instance == "")
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案无模型实例！");
                return obj.ToString();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //停止模型计算 -- 如果有PYTHON处理进程则关闭
            HydroModel.Stop(plan_code);

            return Get_SuccessObj(); 
        }


        //改变河道的初始水位条件
        public string Change_Reach_Initial(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //更新水情和工情数据库
            Item_Info.Update_LevelTable();

            //修改初始条件
            HydroModel hydromodel = HydroModel.Load(plan_code);
            hydromodel.Change_Reach_Initial(jar[1].ToString());

            //返回
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //获取各子流域产汇流模型类型
        public string Get_Rfmodel(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;
            Dictionary<string, string> catchment_rfmodel = RR11.Get_Catchment_RFmodel(plan_code);

            string data = File_Common.Serializer_Obj(catchment_rfmodel);

            //返回
            return data;
        }

        //改变各子流域产汇流模型类型
        public string Change_Rfmodel(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;

            string plan_code = jar[0].ToString();
            object rf_model = jar[1];

            Dictionary<string, string> catchment_rfmodel = RR11.Get_Catchment_RFmodel(plan_code);
            if(catchment_rfmodel == null)
            {
                //初始化各子流域产汇流类型
                RR11.Change_Catchment_RFmodel(plan_code, "");
            }

            //修改各子流域产汇流类型
            RR11.Change_Catchment_RFmodel(plan_code, rf_model);

            //返回
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //改变河道的洪水入流边界条件
        public string Change_Reach_Boundry(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count < 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string bnd_type = jar[1].ToString();
            string bnd_value = jar.Count == 3 ? jar[2].ToString() : "";

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //修改边界条件
            HydroModel hydromodel = HydroModel.Load(plan_code);
            hydromodel.Change_Reach_Boundry(bnd_type, bnd_value);

            //更新模型状态信息为待计算，重新保存模型
            hydromodel.Model_state = Model_State.Ready_Calting;
            hydromodel.Save();

            //返回
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //修改闸站调度
        public string Model_ZZDU(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;

            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //修改调度方式
            HydroModel hydromodel = HydroModel.Load(plan_code);

            if (jar[1].ToString() == "monitor")  //全部采用闸站现状状态调度
            {
                hydromodel.Apply_GateDispatch_Monitor();
            }
            else if(jar[1].ToString() == "gaterule")  //全部采用规则调度
            {
                hydromodel.Apply_GateDispatch_Rule();
            }
            else   //指令调度
            {
                hydromodel.Apply_GateDispatch_Command(jar[1] as JArray);
            }

            //返回该模型最新的调度信息，必须返回json格式的字符串，否则前端回调函数将不执行
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //修改河堤溃口设置
        public string Change_ReachBreak(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike21模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);

            //溃口基本设置信息
            Reach_Break_BaseInfo break_baseinfo = JsonConvert.DeserializeObject<Reach_Break_BaseInfo>(jar[1].ToString());

            //根据参数修改溃口设置(先删除已有溃口，再增加)
            hydromodel.Change_ReachBreak(plan_code, model_instance, break_baseinfo);

            //返回
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //获取河堤溃口设置
        public string Get_ReachBreak(string pars)
        {
            string plan_code = pars;
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike21模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取该模型 河道溃口信息 
            string break_info = Reach_Break_BaseInfo.Get_Reach_BreakInfo(plan_code, model_instance);

            return break_info;
        }

        //根据桩号和河道ID求坐标
        public string Get_ReachSection_Location(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;

            //获取参数
            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型
            HydroModel model = HydroModel.Load(plan_code);

            JArray jar1 = JArray.Parse(jar[1].ToString());
            List<PointXY> jwd_list = model.Get_ReachSection_Locations(jar1);

            return File_Common.Serializer_Obj(jwd_list);
        }

        //设置优化调度目标约束等参数 --不做任何计算，仅修改数据库
        public string Set_Dispatch_Target(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 4 || jar[0].ToString() == "") return null;

            //获取参数
            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //更新数据库调度目标设置信息
            Hd11.Update_ModelPara_DBInfo(plan_code, "mike11_dispatch_target_info", pars);

            //返回该模型最新的调度信息，必须返回json格式的字符串，否则前端回调函数将不执行
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //开始迭代计算，获取库群优化调度信息
        public string Iter_CalModel(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            if (model_instance == "")
            {
                JObject obj1 = new JObject();
                obj1.Add("info", "该方案无模型实例！");
                return obj1.ToString();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //加载模型
            HydroModel model = HydroModel.Load(plan_code);

            //开始迭代计算
            string _data = model.Iter_Simulate().ToString();

            return _data;
        }

        //根据已完成的预报预演方案，反向推演单水库调度方案
        public string Backcal_Resdd(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 3 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string res_name = jar[1].ToString();
            double max_level = double.Parse(jar[2].ToString());

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            if (model_instance == "")
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案无模型实例！");
                return obj.ToString();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //加载模型
            HydroModel model = HydroModel.Load(plan_code);

            //如果模型状态未完成，则返回
            if (model.Model_state != Model_State.Finished)
            {
                JObject obj = new JObject();
                obj.Add("info", "该方案未完成计算！");
                return obj.ToString();
            }

            //开始迭代计算
            Dictionary<string, object> res = Resoptdd.Get_SingelResGate_OptimizeDd(model, model_instance, res_name, max_level);
            string _data = File_Common.Serializer_Obj(res);

            return _data;
        }


        //获取故障闸门基础信息
        public string Get_FaultGate_BaseInfo(string business_code)
        {
            //获取该业务模型包含的二维模型实例
            string mike21_modelinstance = HydroModel.Get_Mike1121_ModelInstance(business_code, "mike21");
            if (mike21_modelinstance == "") return null;

            //从数据库获取故障闸门基础信息
            Dictionary<string, object> faultgate = FaultGate_BaseInfo.Read_StrGate_Info(mike21_modelinstance);

            //序列化
            string _data = File_Common.Serializer_Obj(faultgate);

            return _data;
        }

        //故障闸门设置
        public string Set_Fault_Gate(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string faultgate_info = jar[1].ToString();

            //各闸门调度信息
            JArray jar1 = JArray.Parse(faultgate_info);
            if (jar1.Count != 4 ) return null;
            string str_name = jar1[0].ToString();
            string fault_type = jar1[1].ToString();
            List<string> fault_gates = JsonConvert.DeserializeObject<List<string>>(jar1[2].ToString());
            List<string> gate_h = JsonConvert.DeserializeObject<List<string>>(jar1[3].ToString());
            List<double> gate_h_list = new List<double>();
            for (int i = 0; i < gate_h.Count; i++)
            {
                gate_h_list.Add(double.Parse(gate_h[i]));
            }

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //修改调度方式
            HydroModel hydromodel = HydroModel.Load(plan_code);

            hydromodel.Set_Fault_Gate(plan_code, str_name, fault_type, fault_gates, gate_h_list);

            //返回该模型最新的调度信息，必须返回json格式的字符串，否则前端回调函数将不执行
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        //故障闸门回显
        public string Get_Fault_Gate(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            if (model_instance == "")
            {
                JObject obj1 = new JObject();
                obj1.Add("info", "该方案无模型实例！");
                return obj1.ToString();
            }

            Mysql_GlobalVar.now_instance = model_instance;

            string res = HydroModel.Get_Fault_Gate(plan_code, model_instance);
            return res;
        }

        //修改模型名称和描述
        public string Change_ModelBaseinfo(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 4 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string fangan_name = jar[1].ToString();
            string model_desc = jar[2].ToString();
            int step_save_minutes = jar[3].ToString() ==""?-1: int.Parse(jar[3].ToString());

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            if (model_instance == "")
            {
                JObject obj1 = new JObject();
                obj1.Add("info", "该方案无模型实例！");
                return obj1.ToString();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //修改模型名称和描述和保存时间步长
            HydroModel.Change_ModelBaseinfo(plan_code, fangan_name, model_desc, step_save_minutes);

            //返回
            JObject obj = new JObject();
            obj.Add("success", true);
            return obj.ToString();
        }

        #endregion ***********************************************************


        #region *******************获取模型及结果信息************************
        //获取实例已有模型方案信息
        public string Get_AllModels(string model_instance)
        {
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型信息
            Dictionary<string, Dictionary<string, string>> model_infos = HydroModel.Get_AllModel_Info();

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(model_infos);

            return _data;
        }

        //获取模型调度信息
        public string Get_Model_Ddinfo(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //获取结果
            string model_ddinfo = HydroModel.Get_ModelGatedd_Info(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(model_ddinfo);

            return _data;
        }

        //获取该方案主要控制闸站的 简短调度指令
        public string Get_Model_DispatchPlan(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //简短调度指令
            string model_ddplan = HydroModel.Get_Model_DispatchPlan(plan_code);

            return model_ddplan;
        }

        //获取初始水情信息
        public string Get_Model_InitialLevel(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取结果
            string initial_info = HydroModel.Get_Model_InitialLevel(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(initial_info);

            return _data;
        }

        //获取边界条件信息
        public string Get_Model_BndInfo(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取结果
            string bnd_info = HydroModel.Get_Model_BndInfo(plan_code);

            return bnd_info;
        }

        //获取优化调度目标设置信息
        public string Get_Model_DispatchTarget(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取结果
            string bnd_info = HydroModel.Get_Model_DispatchTarget(plan_code);

            return bnd_info;
        }

        //获取模型统计data结果数据
        public string Get_Mike11_TjResult(string pars)
        {
            string plan_code = pars;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型统计data结果
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_TjResult(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(model_result);

            return _data;
        }

        //获取模型过程data结果数据
        public string Get_Mike11_GcResult(string pars)
        {
            string plan_code = pars;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //全部的水库(水位、库容、入库流量、出库流量)和河道特征断面(水位和流量)以及保留区(分洪流量、滞洪量))
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_GcResult(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(model_result);

            return _data;
        }

        //获取模型某时刻 GIS过程面结果数据
        public string Get_Model_GISGC_PolygonResult(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "" ) return null;
            string plan_code = jar[0].ToString();
            string time_str = jar[1].ToString();
            DateTime time = SimulateTime.StrToTime(time_str);
            time_str = SimulateTime.TimetoStr(time);

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型GIS过程面结果
            string gis_result = HydroModel.Get_Mike11Gis_Result(plan_code, model_instance, Mike11FloodRes_GisType.water_polygon, time_str);

            return gis_result;
        }

        //获取模型 GIS样板线
        public string Get_Sample_GisLine(string plan_code)
        {
            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型GIS过程线结果
            string gis_result = HydroModel.Get_Mike11Gis_Result(plan_code, model_instance, Mike11FloodRes_GisType.reach_centerline);

            return gis_result;
        }

        //获取模型GIS过程线的全过程属性结果
        public string Get_LineData_Attrs(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string res_type = jar[1].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型GIS过程线的全过程属性结果
            string gis_data_result = Res11.Load_Res11Gis_LineData_Attrs(plan_code, res_type);

            return gis_data_result;
        }

        //获取模型 GIS统计线结果数据
        public string Get_Model_GISTJResult(string pars)
        {
            string plan_code = pars;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型GIS统计结果
            string gis_result = HydroModel.Get_Mike11GisTJ_Result(plan_code);

            return gis_result;
        }

        //获取模型 GIS统计面结果数据 -- 淹没水深分布
        public string Get_Model_GISTJ_PolygonResult(string pars)
        {
            string plan_code = pars;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型GIS统计结果
            string gis_result = HydroModel.Get_Mike11Gis_Result(plan_code, model_instance, Mike11FloodRes_GisType.water_polygon,"all_time");

            return gis_result;
        }


        //获取语音
        public string Get_Model_SoundResult(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取文件字节流
            string soundfile_dir = Model_Files.Get_ModelResdir(plan_code) + @"\" + "sound";
            Dictionary<string, byte[]> file_bytes = File_Common.Get_AllFile_Byte(soundfile_dir);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(file_bytes);

            return _data;
        }

        //直接从结果中获取模型结果数据(某点 某时刻/无时刻)
        public string Get_Point_Result(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 4 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string now_timestr = jar[1].ToString(); //如果时间为""，则范围时间序列
            string jd = jar[2].ToString();
            string wd = jar[3].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            PointXY p = PointXY.Get_Pointxy(double.Parse(jd), double.Parse(wd));
            object obj = HydroModel.Get_Point_Result(plan_code, now_timestr, p);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(obj);

            return _data;
        }

        //获取模型纵剖面图结果数据
        public string Get_Mike11_ZpResult(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string res_type = jar[1].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取结果
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_ZPResult(plan_code, res_type);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(model_result);

            return _data;
        }

        //获取模型纵剖面图结果详表
        public string Get_Mike11_XbResult(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型结果
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_XbResult(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(model_result);

            return _data;
        }

        //获取模型河道信息，所有模型一样，无参
        public string Get_Main_ReachInfo(string model_instance)
        {
            //获取模型结果
            List<Reach_BasePars> reach_info = Item_Info.Get_MainReach_Info(model_instance);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(reach_info);

            return _data;
        }

        //获取河道及断面清单
        public string Get_ReachSections(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            Dictionary<string, object> reach_section_infos = new Dictionary<string, object>();

            //获取河道基础信息
            Dictionary<string, Reach_BasePars> reach_info = Item_Info.Get_MainReach_Infodic(model_instance);

            //获取模型结果
            Dictionary<string, List<float>> reach_sections = HydroModel.Get_ReachSections(plan_code);


            reach_section_infos.Add("reach_baseinfo", reach_info);
            reach_section_infos.Add("reach_sections", reach_sections);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(reach_section_infos);

            return _data;
        }

        //获取单一河道断面的水位流量结果过程
        public string Get_SectionRes(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 3 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string reach_name = jar[1].ToString();
            double chainage = double.Parse(jar[2].ToString());

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取结果
            AtReach atreach = AtReach.Get_Atreach(reach_name, chainage);

            //先从数据库直接获取结果
            Dictionary<string, Dictionary<DateTime, float>> section_result = HydroModel.Get_SectionRes(plan_code, atreach);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(section_result);

            //如果是空，则直接读取模型结果文件
            if (section_result == null)
            {
                Dictionary<string, Dictionary<DateTime, double>> section_result1 = Res11.Load_SectionRes_FromResFile(plan_code, atreach);
                _data = File_Common.Serializer_Obj(section_result1);
            }

            return _data;
        }

        //获取多个河道断面的水位流量结果过程
        public string Get_SectionList_Res(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            List<Reach_Sections> atreach_list = JsonConvert.DeserializeObject<List<Reach_Sections>>(jar[1].ToString());

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //直接从模型结果文件里获取
            Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> res = Res11.Get_SectionList_Res_FromResFile(plan_code, atreach_list);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(res);

            return _data;
        }


        //获取多个 河道断面的流量过程
        public string Get_Section_Discharges(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;
            Dictionary<string, AtReach1> atreachs = JsonConvert.DeserializeObject<Dictionary<string, AtReach1>>(jar[1].ToString());
            if (atreachs == null) return "";

            Dictionary<string, object> discharge_res = HydroModel.Get_Section_Discharges(plan_code, atreachs);

            string _data = File_Common.Serializer_Obj(discharge_res);

            return _data;
        }

        //获取多个子流域 的流量过程
        public string Get_Catchment_Discharges(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;
            Dictionary<string, string> catchments = JsonConvert.DeserializeObject<Dictionary<string, string>>(jar[1].ToString());
            if (catchments == null) return "";

            //加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);
            Dictionary<string, Dictionary<DateTime, double>> discharge_res = hydromodel.Get_Catchment_Discharges(catchments);

            string _data = File_Common.Serializer_Obj(discharge_res);

            return _data;
        }

        //获取建筑物的水位流量结果过程
        public string Get_GateRes(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string plan_code = jar[0].ToString();
            string gate_name = jar[1].ToString();

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //直接读取模型结果文件
            Dictionary<string,object> gate_result = Res11.Load_SectionRes_FromResFile(plan_code, gate_name);
            string _data = File_Common.Serializer_Obj(gate_result);

            return _data;
        }

        //获取单一河道断面的原始断面数据(根据河名和桩号)
        public string Get_SectionDatas(string pars)
        {
            JArray jar = JArray.Parse(pars);
            if (jar[0].ToString() == "" ) return null;
            Mysql_GlobalVar.now_instance = "wg_mike11";

            List<double[]> sectiondata2 = HydroModel.Get_SectionDatas(jar[0].ToString(), jar[0].ToString(), jar[1].ToString());

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(sectiondata2);

            return _data;
        }

        //获取单一河道断面的原始断面数据(根据点)
        public string Get_SectionDatas_FromPoint(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string jd = jar[0].ToString();
            string wd = jar[1].ToString();
            PointXY p = PointXY.Get_Pointxy(double.Parse(jd), double.Parse(wd));

            Mysql_GlobalVar.now_instance = "wg_mike11";

            //获取断面数据(根据模型断面文件，读取默认模型河网文件初始化河道，求河道名称和桩号，再从数据库获取断面原始数据)
            Dictionary<string, object> sectiondata = HydroModel.Get_Sectiondata_FromPoint(p);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(sectiondata);
             
            return _data;
        }

        //根据方案ID获取需要进行流量统计的特征河道断面
        public string Get_AtReach(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return Get_SuccessObj();

            //根据模型方案id和业务模型类型获取需要统计流量的特征断面(用于分洪、溃堤、闸站故障流量统计)
            AtReach atreach = Item_Info.Get_ReachSection_From_PlanCode(plan_code);

            //序列化
            string _data = File_Common.Serializer_Obj(atreach);

            return _data;
        }

        //获取水库、河道水文站、拦河闸警戒水位、保证水位
        public string Get_StationInfo()
        {
            //根据模型方案id和业务模型类型获取需要统计流量的特征断面(用于分洪、溃堤、闸站故障流量统计)
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //序列化
            string _data = File_Common.Serializer_Obj(now_waterlevel);

            return _data;
        }

        //获取建筑物 规则调度 信息
        public string Get_Str_DdRuleInfo(string business_code)
        {
            //获取闸门基本信息，其中包括规则调度信息
            if (Item_Info.Str_BaseInfo == null) Item_Info.Initial_StrBaseInfo();

            //根据业务模型获取模型实例(参数为空字符串，则为全部，否则为指定业务模型的建筑物)
            Dictionary<string, Struct_BasePars> str_info = business_code == ""? Item_Info.Str_BaseInfo:Item_Info.Get_StrBaseInfo_ByModelBusiness(business_code);

            //刷选一下没有闸门的
            Dictionary<string, Struct_BasePars> controstr_info = new Dictionary<string, Struct_BasePars>();
            for (int i = 0; i < str_info.Count; i++)
            {
                if(str_info.ElementAt(i).Value.gate_type != GateType.NOGATE)
                {
                    controstr_info.Add(str_info.ElementAt(i).Key, str_info.ElementAt(i).Value);
                }
            }

            //序列化
            string _data = File_Common.Serializer_Obj(controstr_info);

            return _data;
        }

        //获取水库、河道闸站、蓄滞洪区的可控建筑物
        public string Get_Control_Strs(string pars)
        {
            //采用jarray解析返回的json数组
            JArray jar = JArray.Parse(pars);
            if (jar.Count != 2 || jar[0].ToString() == "") return null;
            string business_code = jar[0].ToString();
            string obj_stcd = jar[1].ToString();

            //获取闸门基本信息，其中包括规则调度信息
            if (Item_Info.Str_BaseInfo == null) Item_Info.Initial_StrBaseInfo();

            //根据业务模型获取模型实例(参数为空字符串，则为全部，否则为指定业务模型的建筑物)
            Dictionary<string, Struct_BasePars> str_info = business_code == "" ? Item_Info.Str_BaseInfo : Item_Info.Get_StrBaseInfo_ByModelBusiness(business_code);

            //刷选一下没有闸门的
            Dictionary<string, Struct_BasePars> controstr_info = new Dictionary<string, Struct_BasePars>();
            for (int i = 0; i < str_info.Count; i++)
            {
                if (str_info.ElementAt(i).Value.gate_type != GateType.NOGATE)
                {
                    controstr_info.Add(str_info.ElementAt(i).Key, str_info.ElementAt(i).Value);
                }
            }

            //找到涉及的建筑物id
            List<string> str_list = controstr_info.Where(kv => kv.Value.struct_stcd == obj_stcd).Select(kv => kv.Key).ToList();

            //序列化
            string _data = File_Common.Serializer_Obj(str_list);

            return _data;
        }


        //获取成功信息对象
        private static string Get_SuccessObj(string info = "success")
        {
            JObject obj = new JObject();
            obj.Add(info, true);
            return obj.ToString();
        }

        //获取最新闸站工情
        public string Get_GateState(string business_code)
        {
            //从数据库获取当前闸门状态
            List<Gate_StateInfo> gate_state =(business_code == "" || business_code == "undefined")? Item_Info.Read_NowGateState(): Item_Info.Read_NowGateState(business_code);

            //序列化
            string _data = File_Common.Serializer_Obj(gate_state);

            return _data;
        }

        //获取最新水情信息
        public string Get_NowWaterInfo(string business_code)
        {
            //从数据库获取当前最新水情
            List<Water_Info> now_waterinfo = Item_Info.Read_Now_WaterInfo(business_code);

            //组合对象属性
            Dictionary<string, object> res = new Dictionary<string, object>();
            res.Add("success", true);
            res.Add("code", "200");
            res.Add("message", "请求成功");
            res.Add("data", now_waterinfo);

            //序列化
            string _data = File_Common.Serializer_Obj(res);

            return _data;
        }

        //获取河道水库设计洪水信息 
        public string Get_Design_Flood(string business_code)
        {
            List<Reach_Design_Flood> res = HydroModel.Get_Design_Flood(business_code);
            if (res == null) return null;
            return File_Common.Serializer_Obj(res);
        }

        //获取流域 预警信息
        public string Get_Risk_Warning(string plan_code)
        {
            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = model_instance;

            //获取该方案下的降雨预警信息
            List<Warning_Info> warning_infos = new List<Warning_Info>();
            Warning_Info rain_warning_info = HydroModel.Get_Rain_WarningInfo(plan_code);
            warning_infos.Add(rain_warning_info);

            //获取该方案下降雨、水库、河道、蓄滞洪区进洪、南水北调、山洪灾害预警信息 
            List<Warning_Info> warning_info = HydroModel.Get_WarningInfo(plan_code);
            warning_infos.AddRange(warning_info);

            //根据预警级别进行排序
            List<Warning_Info> warning_infos1 = warning_infos.OrderBy(p => p.Level).ToList();

            return File_Common.Serializer_Obj(warning_infos1);
        }

        //获取南水北调交叉断面信息
        public string Get_Nsbd_SectionInfo()
        {
            //从数据库获取南水北调交叉断面信息
            Dictionary<string, ZGQ_SectionInfo> nsbd_sectioninfo = Item_Info.Get_ZGQ_SectionInfo();

            return File_Common.Serializer_Obj(nsbd_sectioninfo);
        }

        //获取历史自动预报清单
        public string Get_History_AutoForcast_List()
        {
            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = "wg_mike11";

            //获取结果
            Dictionary<string, string[]> history_autoforcast_list = Res11.Get_History_AutoForcast_List();

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(history_autoforcast_list);

            return _data;
        }

        //删除历史自动预报
        public string Del_History_AutoForcast(string plan_code)
        {
            if (plan_code == "" || plan_code == null)
            {
                return Get_SuccessObj();
            }

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = "wg_mike11";

            //删除模型
            Res11.Del_History_AutoForcast(plan_code);

            JObject obj1 = new JObject();
            obj1.Add("success", true);
            string _data = obj1.ToString();
            return _data;
        }

        //获取历史自动预报结果
        public string Get_History_AutoForcast_Res(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = "wg_mike11";

            //获取结果
            Dictionary<string, object> res = Res11.Get_History_AutoForcast_List(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(res);

            return _data;

        }

        //获取业务模型视角
        public string Get_Business_View(string business_model)
        {
            if (business_model == "" || business_model == null) return null;

            //获取结果
            string res = HydroModel.Get_BusinessView_FromDB(business_model);

            return res;
        }

        //获取山丘区预报洪水
        public string Get_Mountain_Forecast_Flood(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //设置当前项目名(模型实例名)
            Mysql_GlobalVar.now_instance = "wg_mike11";

            //获取结果
            List<Dictionary<string, object>> res = Res11.Get_Mountain_Forecast_Flood(plan_code);

            //将集合序列化为json格式的字符串
            string _data = File_Common.Serializer_Obj(res);

            return _data;
        }

        //获取场次降雨洪水清单
        public string Get_Flood_List()
        {
            //查询数据库获取场次洪水信息
            List<Rain_Flood_Info> rain_flood_info = Rain_Flood_Info.Get_ReachFloodInfo_FromDB();

            string res = File_Common.Serializer_Obj(rain_flood_info);
            return res;
        }

        //获取场次降雨洪水的方案清单
        public string Get_FloodPlan_List(string flood_id)
        {
            if (flood_id == "" || flood_id == null) return null;

            //查询数据库获取场次洪水的方案清单信息
            Dictionary<string, Dictionary<string, string>> flood_plan_info = HydroModel.Get_Flood_ModelInfo_FromDB(flood_id);

            string res = File_Common.Serializer_Obj(flood_plan_info);
            return res;
        }

        //修改某场次降雨洪水的推荐方案
        public string Change_Flood_RecomPlan(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //修改场次洪水推荐方案
            Rain_Flood_Info.Update_Rain_RecomPlan(plan_code);

            JObject obj = new JObject();
            obj.Add("success", true);
            string _data = obj.ToString();

            return _data;
        }

        //获取工程重点巡查位置
        public string Get_Important_Inspect_Parts(string plan_code)
        {
            if (plan_code == "" || plan_code == null) return null;

            //根据模型方案id获取已经集成的mike11模型实例名称(有文件夹)
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            Mysql_GlobalVar.now_instance = model_instance;

            //获取模型
            HydroModel model = HydroModel.Load(plan_code);

            //分类获取工程重点巡查位置
            Dictionary<string, List<Important_Inspect_UnitInfo>> import_inspect = HydroModel.Get_Important_Inspect_Parts(model);

            string res = File_Common.Serializer_Obj(import_inspect);
            return res;
        }


        //模型业务
        public string Model_Business()
        {
            //上传断面文件
            Mysql_GlobalVar.now_instance = "wg_mike11";
            //Xns11.Readtxt_Write_sectiondata_intomysql();
            //Xns11.Readxns11_Write_Sectionobject_IntoDB();

            //获取一维结果
            HydroModel hydromodel = HydroModel.Load("model_20241105184059");
            List<object> mike11_datares = hydromodel.Get_Mike11_AllResult("wg_mike11");
            Dictionary<string, object> res_desc_obj = new Dictionary<string, object>();
            //res_desc_obj.Add("res_desc", res_desc);
            //res_desc_obj.Add("main_strdd", main_strdd);
            //res_desc_obj.Add("flood_res", flood_res);
            if (mike11_datares != null) mike11_datares[11] = res_desc_obj;

            //保存结果
            Res11.Write_Mike11res_IntoDB(hydromodel, mike11_datares, "wg_mike11");


            //上传默认模型(系统初期上传数据库时用)
            //HydroModel.Load_DefaultModel_ToDB();

            //上传指定模型
            //Program.Load_Model_ToDB();

            //批量修改模型参数
            //Program.Modify_Model_Pars();

            //对样例多边形进行处理(保留6位数)、建立多边形折点和断面对应关系(注意每次建筑物增减都需要重新生成!)、生成样例线(需有已计算结果模型)
            //Res11.Gis_SamplePolygonLine_Process();

            //更新河道水位流量关系(没有的全部重新计算，有且有stcd的不更新)
            //Program.Update_ReachQH_RelationTable();

            //更新水情和工情数据库
            //Program.Update_LevelGate();

            //1、新建模型1
            //Program.Test1_hydromodel();

            //2、新建模型2
            //Program.Test2_hydromodel();

            //3、开始计算并显示模拟进度
            //Program.Test3_hydromodel();

            return Get_SuccessObj();
        }
        
        #endregion ***********************************************************


        //设置允许跨域访问
        public void Cross_Domain_Set(ref HttpContext context)
        {
            //容许跨域访问
            context.Response.ClearHeaders();
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            string requestHeaders = context.Request.Headers["Access-Control-Request-Headers"];
            context.Response.AppendHeader("Access-Control-Allow-Headers",
            string.IsNullOrEmpty(requestHeaders) ? "*" : requestHeaders);
            context.Response.AppendHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}
