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
        #region 用户新建、保存、删除、加载、传递、下载模型、获取模型信息等操作
        //新建并计算自动预报模型
        public static void CreateRun_AutoForecastModel()
        {
            //采用jarray解析返回的json数组
            string plan_code = Model_Const.AUTO_MODELNAME;
            DateTime start_time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            DateTime end_time = start_time.AddHours(Model_Const.AUTOFORECAST_HOURS);
            string start_timestr = SimulateTime.TimetoStr(start_time);
            string end_timestr = SimulateTime.TimetoStr(end_time);
            string fangan_name = start_timestr + "自动预报";
            string fangan_desc = start_timestr + "开始的自动预报，预报时长72小时";

            //1、更新水情和工情数据库
            Item_Info.Update_LevelTable();
            Item_Info.Update_GateStateTable();

            //2、新建模型、修改初始条件、闸站调度和边界条件  --只保存模型实体
            HydroModel hydro_model = HydroModel.Create_Model(fangan_name, start_timestr, end_timestr, fangan_desc,Model_Const.DEFAULT_MODELNAME,plan_code,60);

            //3、新建模型时仅保存了模型实体和调度信息，模型基本方案信息需要单独保存
            string business_code = "flood_forecast_wg";
            string business_name = "卫共流域洪水预报应用模型";
            HydroModel.Save_ModelPlan_Info(hydro_model, business_code, business_name);

            //根据实时水位更新河道水库初始水位和河道基流
            hydro_model.Update_InitiallevelAndBaseIn();

            //根据数据库闸站状态，修改所有的建筑物闸站调度方式及数据
            hydro_model.Update_AllStr_DdInfo_FromGB();

            //请求各子流域产汇流流量过程，修改相应的边界条件
            Dictionary<string, Dictionary<DateTime, double>> catchment_q = hydro_model.Modify_BndID_ToDischargeDic();

            //更新模型参数数据库中mike11边界条件字段
            Dictionary<string, object> bnd_dic = new Dictionary<string, object>() { };
            bnd_dic.Add("bnd_type", "降雨计算洪水");
            bnd_dic.Add("bnd_value", File_Common.Serializer_Obj(catchment_q));
            Hd11.Update_ModelPara_DBInfo(hydro_model.Modelname, "mike11_bnd_info", File_Common.Serializer_Obj(bnd_dic));

            //请求生成降雨统计等值面
            string request_url = Mysql_GlobalVar.create_rftjgis_serverurl + "?planCode=" + hydro_model.Modelname;
            File_Common.Get_HttpReSponse(request_url);

            //3、计算模型并存储结果
            hydro_model.Simulate();
        }


        //新建模型 -- 根据基础模型名字(前端提供 模型方案名)是否为null确定是否有基础模型
        public static HydroModel Create_Model(string fangan_name, string start_timestr, string end_timestr,
                               string model_desc, string base_plan_code = Model_Const.DEFAULT_MODELNAME,string plan_code = "", int step_saveminutes = 20)
        {
            string dirname = Model_Files.Get_Modeldir(base_plan_code);
            if (base_plan_code == "" || base_plan_code == null || !Directory.Exists(dirname)) base_plan_code = Model_Const.DEFAULT_MODELNAME;

            return Create_NewModel(fangan_name, start_timestr, end_timestr, model_desc, base_plan_code,plan_code, step_saveminutes);
        }

        //在模型实体数据库里 修改模型方案名称和描述和保存时间步长
        public static void Change_ModelBaseinfo(string plan_code, string fangan_name, string model_desc, int step_save_minutes = -1)
        {
            //定义连接的数据库和表
            HydroModel hydromodel = HydroModel.Load(plan_code);
            hydromodel.Model_Faname = fangan_name;
            hydromodel.Modeldesc = model_desc;

            //如果需要修改，则保存时间步长修改
            if(step_save_minutes!= -1)
            {
                SaveTimeStep save_timestep = (SaveTimeStep)Enum.Parse(typeof(SaveTimeStep), step_save_minutes.ToString());
                hydromodel.Set_Mike11_SaveTimeStep(save_timestep);

                //修改模型方案表，更新模型状态
                hydromodel.Model_state = Model_State.Ready_Calting;
                Update_ModelStateInfo(hydromodel, "待计算",false);   
            }
            hydromodel.Save();

            Console.WriteLine("模型方案名称和描述和保存时间步长更新完成！");
        }

        //获取所有模型的信息(特定信息,不含默认模型)
        public static Dictionary<string, Dictionary<string, string>> Get_AllModel_Info()
        {
            //获取该用户所有的模型(不含默认模型)
            return Get_AllModel_Info_FromDB();
        }


        //获取 指定模型 的闸门调度信息(从数据库)
        public static string Get_ModelGatedd_Info(string plan_code)
        {
            return Item_Info.Get_Model_SingleParInfo(plan_code, "mike11_strdd_info");
        }

        //获取主要控制闸门的简短调度指令
        public static string Get_Model_DispatchPlan(string plan_code)
        {
            return Item_Info.Get_Model_DispatchPlan_Fromdb(plan_code);
        }

        //获取 指定模型 的初始条件信息(从数据库)
        public static string Get_Model_InitialLevel(string plan_code)
        {
            return Item_Info.Get_Model_SingleParInfo(plan_code, "mike11_initial_info");
        }

        //获取 指定模型 的边界条件信息(从数据库)
        public static string Get_Model_BndInfo(string plan_code)
        {
            return Item_Info.Get_Model_SingleParInfo(plan_code, "mike11_bnd_info");
        }

        //获取 指定模型 优化调度设置信息(从数据库)
        public static string Get_Model_DispatchTarget(string plan_code)
        {
            return Item_Info.Get_Model_SingleParInfo(plan_code, "mike11_dispatch_target_info");
        }

        //根据方案id获取已经集成的一维模型实例名称(有文件夹，基于一个业务模型只包含一个同类型模型实例)
        public static string Get_Model_Instance(string plan_code)
        {
            //从模型方案数据库获取该方案所属的业务模型ID
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sql = "select business_code from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0)
            {
                
                return "";
            }
            string business_code = dt.Rows[0][0].ToString();

            //从业务模型属性数据库中根据业务模型ID获取基础模型实例
            string tableName1 = Mysql_GlobalVar.MODELBUSINESS_TABLENAME;
            sql = "select instance_codes from " + tableName1 + " where code = '" + business_code + "'";
            dt = Mysql.query(sql);
            nLen = dt.Rows.Count;
            

            if (nLen == 0)
            {
                return "";
            }
            string instance_codes = dt.Rows[0][0].ToString();
            string[] instance_list = instance_codes.Split(new char[] { ',' });

            //如果有该模型实例的文件，表示该模型实例已经集成
            for (int i = 0; i < instance_list.Length; i++)
            {
                string model_instance = instance_list[i];

                //如果有该模型实例的文件，表示该模型实例已经集成
                string model_instancedir = Model_Files.Get_Itemdir(model_instance);
                if (Directory.Exists(model_instancedir))
                {
                    return model_instance;
                }
            }
            return "";
        }

        //根据方案id获取已经集成的模型实例名称(有文件夹，基于一个业务模型只包含一个同类型模型实例)
        public static List<string> Get_Model_Instance_list(string plan_code)
        {
            List<string> instance_list = new List<string>();

            //从业务模型属性数据库获取该方案包含的基础模型实例清单
            

            //从模型方案数据库获取该方案所属的业务模型ID
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sql = "select business_code from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0)
            {
                
                return instance_list;
            }
            string business_code = dt.Rows[0][0].ToString();

            //从业务模型属性数据库中根据业务模型ID获取基础模型实例
            string tableName1 = Mysql_GlobalVar.MODELBUSINESS_TABLENAME;
            sql = "select instance_codes from " + tableName1 + " where code = '" + business_code + "'";
            dt = Mysql.query(sql);
            nLen = dt.Rows.Count;
            

            if (nLen == 0)
            {
                return instance_list;
            }
            string instance_codes = dt.Rows[0][0].ToString();
            instance_list = instance_codes.Split(new char[] { ',' }).ToList();

            return instance_list;
        }

        //获取业务模型和模型实例的对应关系
        public static Dictionary<string, List<string>> Get_BusinessModel_ModelInstance_Relation()
        {
            Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();

            //从业务模型属性数据库获取该方案包含的基础模型实例清单
            

            //从业务模型属性数据库中根据业务模型ID获取基础模型实例
            string tableName1 = Mysql_GlobalVar.MODELBUSINESS_TABLENAME;
            string sql = "select code,instance_codes from " + tableName1;
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            

            if (nLen == 0) return null;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string business_codes = dt.Rows[i][0].ToString();
                string instance_codes = dt.Rows[i][1].ToString();
                string[] instance_list = instance_codes.Split(new char[] { ',' });
                List<string> model_instances = instance_list.ToList();
                res.Add(business_codes, model_instances);
            }

            return res;
        }

        //根据业务模型ID获取里面包括的第1个Mike11或Mike21模型实例
        public static string Get_Mike1121_ModelInstance(string business_code,string mike11_mike21)
        {
            if (business_code == "") return "";

            //获取该业务模型包含的二维模型实例
            Dictionary<string, List<string>> business_instance = HydroModel.Get_BusinessModel_ModelInstance_Relation();
            if (!business_instance.Keys.Contains(business_code)) return "";
            List<string> instances = business_instance[business_code];
            string mike1121_model_instance = "";
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].Contains("mike21"))
                {
                    mike1121_model_instance = instances[i];
                    break;
                }
            }

            return mike1121_model_instance;
        }

        //根据方案ID获取里面包括的第1个Mike11或Mike21模型实例
        public static string Get_Mike21_ModelInstance(string plan_code)
        {
            List<string> instance_list = Get_Model_Instance_list(plan_code);
            string mike21_model_instance = "";
            for (int i = 0; i < instance_list.Count; i++)
            {
                if (instance_list[i].Contains("mike21"))
                {
                    mike21_model_instance = instance_list[i];
                    break;
                }
            }

            return mike21_model_instance;
        }

        //保存模型 -- 名字和提炼出来的进数据库，模型参数分散保存在模型文件中
        //(**XAJ的几个降雨蒸发键值对集合属性未保存，其在更新边界时才赋值**)
        public void Save()
        {
            //将模型实体保存进模型实体数据库
            Save_ModelObject(this);
        }

        //加载模型 -- 根据模型名从数据库中获取(静态类)
        public static HydroModel Load(string plan_code)
        {
            return Load_Model(plan_code);
        }

        //得到默认模型 -- 将默认模型copy到本用户名下
        public void Get_DefaultModel()
        {
            //记载默认模型
            HydroModel Basemodel = HydroModel.Get_DefaultModel_AndCopytoself_FromDB();
            Basemodel.Save();
        }

        //删除模型 -- 根据模型名字从数据库中删除模型，同时删除服务器上的模型文件
        public static void Delete(string plan_code)
        {
            Delete_Model(plan_code);
            Delete_Model_Res11(plan_code);
            Delete_Model_GisLineRes11(plan_code);
            Delete_Model_GisPolygonRes11(plan_code);
        }

        //传递模型 -- 各用户之间传递模型（数据库里用户的模型增加）
        public static bool Send_Model(string plan_code, string other_username)
        {
            Send_Model_ToOtherUser(plan_code, other_username);
            return true;
        }

        //下载模型文件服务端部分 -- 从服务端得到模型文件字节数组，string为带扩展名的文件名
        public static Dictionary<string, byte[]> Get_ModelFileByte(string plan_code)
        {
            //得到模型
            HydroModel model = Load_Model(plan_code);

            //返回各模型文件字节数组
            string model_dir = Path.GetDirectoryName(model.Modelfiles.Simulate_filename);
            return File_Common.Get_ModelFile_Byte(model_dir);
        }

        //下载所有文件服务端部分 -- 从服务端得到模型文件和结果文件字节数组，
        public static Dictionary<string, byte[]> Get_AllFileByte(string plan_code)
        {
            //得到模型
            HydroModel model = Load_Model(plan_code);

            //返回各模型文件字节数组
            string model_dir = Path.GetDirectoryName(model.Modelfiles.Simulate_filename);
            return File_Common.Get_AllFile_Byte(model_dir);
        }
        #endregion

    }
}
