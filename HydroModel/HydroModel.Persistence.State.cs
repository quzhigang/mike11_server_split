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
        public static void Update_ModelStateInfo(HydroModel model, string[] model_info, bool update_modelobject = true)
        {
            //从模型中获取模型信息
            string model_instance = Mysql_GlobalVar.now_instance;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;
            

            //先判断和删除旧的信息
            string select_sql = "select * from " + tableName + " where plan_code = '" + model.Modelname + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            string modelstate = model_info[4];
            string start_simulate_time = model_info[5];

            //模拟所需要的时间(秒)
            if (nLen != 0 && modelstate != null)
            {
                //更新模型状态和其他
                string sql = "update " + tableName + " set state = '" + modelstate + "' where plan_code = '" + model.Modelname + "'";
                Mysql.Execute_Command(sql);

                sql = "update " + tableName + " set start_simulate_time = '" + start_simulate_time + "' where plan_code = '" + model.Modelname + "'";
                Mysql.Execute_Command(sql);

                switch (modelstate)
                {
                    case "已完成":
                        model.Model_state = Model_State.Finished;
                        break;
                    case "模型错误":
                        model.Model_state = Model_State.Error;
                        break;
                    case "待计算":
                        model.Model_state = Model_State.Ready_Calting;
                        break;
                    default:
                        model.Model_state = Model_State.Iscalting;
                        break;
                }

                //更新模型实体
                if (update_modelobject) Update_ModelObject(model);
            }

            
        }

        //从模型方案表里 更新模型状态
        public static void Update_ModelStateInfo(HydroModel model, string modelstate,bool update_modelobject = true)
        {
            //从模型中获取模型信息
            string model_instance = Mysql_GlobalVar.now_instance;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;
            

            //先判断和删除旧的信息
            string select_sql = "select * from " + tableName + " where plan_code = '" + model.Modelname + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0 && modelstate != null)
            {
                //更新模型状态
                string sql = "update " + tableName + " set state = '" + modelstate + "' where plan_code = '" + model.Modelname + "'";
                Mysql.Execute_Command(sql);

                switch (modelstate)
                {
                    case "已完成":
                        model.Model_state = Model_State.Finished;
                        break;
                    case "模型错误":
                        model.Model_state = Model_State.Error;
                        break;
                    case "待计算":
                        model.Model_state = Model_State.Ready_Calting;
                        break;
                    default:
                        model.Model_state = Model_State.Iscalting;
                        break;
                }

                //更新模型实体
                if(update_modelobject) Update_ModelObject(model);
            }

            
        }

        //更新模型调度信息 从数据库中
        public static void Update_Model_Ddinfo(HydroModel model, string Gatedd_Info)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //更新数据库该闸的调度信息
            string sql = $"UPDATE {tableName} SET mike11_strdd_info=:Gatedd_Info WHERE plan_code=:planCode AND model_instance=:modelInstance";

            KdbndpParameter[] mysqlPara =
            {
                new KdbndpParameter(":Gatedd_Info", Gatedd_Info),
                new KdbndpParameter(":planCode", model.Modelname),
                new KdbndpParameter(":modelInstance", Mysql_GlobalVar.now_instance)
            };
            Mysql.Execute_Command(sql, mysqlPara);

            Console.WriteLine("调度信息存储完成！");
        }

        //保存模型基本信息进模型方案数据库
        public static void Save_ModelPlan_Info(HydroModel model,string business_code,string business_name)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;
            

            string model_instance = Mysql_GlobalVar.now_instance;

            //先判断和删除旧的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + model.Modelname + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + model.Modelname  + "'";
                Mysql.Execute_Command(del_sql);
            }

            //向模型参数数据库中插入新的模型信息
            DateTime start_time = model.ModelGlobalPars.Simulate_time.Begin.AddHours(model.ModelGlobalPars.Ahead_hours);
            double time_hours = model.ModelGlobalPars.Simulate_time.End.Subtract(model.ModelGlobalPars.Simulate_time.Begin).TotalHours;
            int expect_seconds = (int)Item_Info.Get_ModelRun_ElispedTime(time_hours);
            int step_save_minutes = (int)model.Mike11Pars.Mike11_savetimestep;

            //获取模型状态
            string state = "";
            switch (model.Model_state)
            {
                case Model_State.Finished:
                    state = "已完成";
                    break;
                case Model_State.Error:
                    state = "模型错误";
                    break;
                case Model_State.Ready_Calting:
                    state = "待计算";
                    break;
                default:
                    state = "计算中";
                    break;
            }

            //插入记录
            string sql = "insert into " + tableName + " (plan_code,plan_name,plan_desc,business_code,business_name,start_time,end_time,state,step_save_minutes,expect_seconds,start_simulate_time,progress_info) " +
                "values(:plan_code,:plan_name,:plan_desc,:business_code,:business_name,:start_time,:end_time,:state,:step_save_minutes,:expect_seconds,:start_simulate_time,:progress_info)";
            KdbndpParameter[] mysqlPara = {
                                            new KdbndpParameter(":plan_code", model.Modelname),
                                            new KdbndpParameter(":plan_name", model.Model_Faname),
                                            new KdbndpParameter(":plan_desc", model.Modeldesc),

                                            new KdbndpParameter(":business_code", business_code),
                                            new KdbndpParameter(":business_name", business_name),

                                            new KdbndpParameter(":start_time", SimulateTime.TimetoStr(start_time)),
                                            new KdbndpParameter(":end_time", SimulateTime.TimetoStr(model.ModelGlobalPars.Simulate_time.End)),
                                            new KdbndpParameter(":state", state),
                                            new KdbndpParameter(":step_save_minutes",step_save_minutes),
                                            new KdbndpParameter(":expect_seconds",expect_seconds),
                                            new KdbndpParameter(":start_simulate_time", ""),
                                            new KdbndpParameter(":progress_info", "")
                                         };

            Mysql.Execute_Command(sql, mysqlPara);

            Console.WriteLine("模型存储完成！");
        }

        //保存模型名字和信息进模型实体数据库
        public static void Save_ModelObject(HydroModel model)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            string model_instance = Mysql_GlobalVar.now_instance;

            //如果中间加载了断面数据，则清除
            if (model.Mike11Pars.SectionList != null) model.Mike11Pars.SectionList = null;

            //先判断和删除旧的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + model.Modelname + "' and model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)  //仅更新模型实体
            {
                Update_ModelObject(model);
            }
            else   //插入新的记录
            {
                //将模型对象、调度信息 存入数据库
                string dd_info = Item_Info.Get_Model_DdInfo(model);

                //从模型中获取初始水情信息
                string initial_info = Item_Info.Get_Model_InitialInfo(model);

                //向模型参数数据库中插入新的模型信息
                Inser_ModelInfo(model, model_instance, tableName, dd_info, initial_info);
            }

            

            Console.WriteLine("模型存储完成！");
        }

        //向模型参数数据库中插入新的模型信息
        public static void Inser_ModelInfo(HydroModel model, string model_instance, string tableName, string dd_info,string initial_info)
        {
            MemoryStream ms = new MemoryStream();   //将模型流域集合类序列化后写入数据库
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            binFormat.Serialize(ms, model);  //将对象序列化
            byte[] buffer = ms.ToArray();  //转换成字节数组

            string sql = "insert into " + tableName + " (plan_code,model_instance,model_object,mike11_strdd_info,mike11_initial_info) values(:plan_code,:model_instance,:model_object,:mike11_strdd_info,:mike11_initial_info)";
            KdbndpParameter[] mysqlPara = { new KdbndpParameter(":plan_code", model.Modelname),
                                             new KdbndpParameter(":model_instance", model_instance),
                                             new KdbndpParameter(":model_object", buffer),
                                             new KdbndpParameter(":mike11_strdd_info", dd_info),
                                             new KdbndpParameter(":mike11_initial_info", initial_info),
                                         };

            Mysql.Execute_Command(sql, mysqlPara);
            ms.Close();
            ms.Dispose();
        }

        //更新模型实体
        public static void Update_ModelObject(HydroModel model)
        {
            if (model.Mike11Pars.SectionList != null) model.Mike11Pars.SectionList = null;

            //将模型流域集合类序列化后写入数据库
            MemoryStream ms = new MemoryStream();   
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            binFormat.Serialize(ms, model);  //将对象序列化
            byte[] buffer = ms.ToArray();  //转换成字节数组

            string model_instance = Mysql_GlobalVar.now_instance;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //先判断和删除旧的信息
            string select_sql = "select plan_code from " + tableName + " where plan_code = '" + model.Modelname + "' and model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0 && model != null)
            {
                string sql = $"UPDATE {tableName} SET model_object=:ModelObject WHERE plan_code=:planCode AND model_instance=:modelInstance";
                KdbndpParameter[] mysqlPara =
                {
                    new KdbndpParameter(":ModelObject", buffer),
                    new KdbndpParameter(":planCode", model.Modelname),
                    new KdbndpParameter(":modelInstance", model_instance)
                };
                Mysql.Execute_Command(sql, mysqlPara);
            }
            ms.Close();
            ms.Dispose();
            
        }


        //从数据库下载模型文件模板
        public static void Load_ModelFile_Template(string destfilepath)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELFILETEMPLATE_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string modelfile_extensionname = Path.GetExtension(destfilepath);
            string sql = "select object from " + tableName + " where name = '" + modelfile_extensionname + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型文件模板:{0}!", modelfile_extensionname);
                
                return;
            }

            byte[] result = (byte[])dt.Rows[0][0];
            Dictionary<string, byte[]> modelfile_byte = new Dictionary<string, byte[]>();
            modelfile_byte.Add(destfilepath, result);

            string dest_dir = Path.GetDirectoryName(destfilepath);
            File_Common.Down_Files(modelfile_byte, dest_dir);
            
        }

        //返回模型描述 -- 采用格式化的描述方式

        //请求 停止 执行AD_GP服务
        public static Dictionary<string, Dictionary<string, string>> HTTP_RequestCancel_GP(string plan_code)
        {
            string run_info = "";
            string gpwork_id = "";

            //获取用户模型信息
            Dictionary<string, Dictionary<string, string>> model_infos = HydroModel.Get_AllModel_Info();
            if(model_infos.Keys.Contains(plan_code))
            {
                Dictionary<string, string> model_info = model_infos[plan_code];
                gpwork_id = model_info["ad_gpserverid"];
            }

            if (gpwork_id != "" && gpwork_id != null)
            {
                string url = Mysql_GlobalVar.gp_cancleserverurl + "/" + gpwork_id + "/" + "cancel";

                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";                            //请求方法
                request.ProtocolVersion = new Version(1, 1);   //Http/1.1版本

                //发送请求
                try
                {
                    request.GetResponse();
                }
                finally
                {
                    run_info = "";
                }
            }

            return model_infos;
        }

        public static Dictionary<string,Dictionary<string,string>> Get_AllModel_Info_FromDB()
        {
            Dictionary<string, Dictionary<string, string>> modelinfo_list = new Dictionary<string, Dictionary<string, string>>();

            //定义连接的数据库和表
            
            string modelpar_table = Mysql_GlobalVar.MODELPAR_TABLENAME;
            string plan_table = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sql = "select plan_code from " + modelpar_table + " where model_instance = '" + Mysql_GlobalVar.now_instance + "' and plan_code != '" + Model_Const.DEFAULT_MODELNAME + "'";
            DataTable dt = Mysql.query(sql);
            if (dt == null) return null;
            int nLen = dt.Rows.Count;

            List<string> plan_codes = new List<string>();
            if (nLen == 0)
            {
                Console.WriteLine("模型实例：{0}没有模型方案!", Mysql_GlobalVar.now_instance);
                return null;
            }
            else
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    plan_codes.Add(dt.Rows[i][0].ToString());
                }
            }

            //从业务方案表中查询该模型实例的方案信息
            sql = "select * from " + plan_table;
            dt = Mysql.query(sql);
            nLen = dt.Rows.Count;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string plan_code = dt.Rows[i][0].ToString();
                if (plan_codes.Contains(plan_code))
                {
                    Dictionary<string, string> model_info = new Dictionary<string, string>();
                    for (int j = 1; j < dt.Columns.Count-2; j++)
                    {
                        model_info.Add(dt.Columns[j].ColumnName, dt.Rows[i][j].ToString());
                    }
                    modelinfo_list.Add(plan_code, model_info);
                }
            }

            
            return modelinfo_list;
        }

        //查询场次洪水的所有模型方案信息
        public static Dictionary<string, Dictionary<string, string>> Get_Flood_ModelInfo_FromDB(string flood_id)
        {
            Dictionary<string, Dictionary<string, string>> modelinfo_list = new Dictionary<string, Dictionary<string, string>>();

            //定义连接的数据库和表
            string plan_table = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //从业务方案表中查询该模型实例的方案信息
            string sql = $"select * from {plan_table} where flood_id = '{flood_id}'" ;
            DataTable dt = Mysql.query(sql);
            if (dt == null) return null;
            int nLen = dt.Rows.Count;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string plan_code = dt.Rows[i][0].ToString();
                Dictionary<string, string> model_info = new Dictionary<string, string>();
                for (int j = 1; j < dt.Columns.Count - 2; j++)
                {
                    model_info.Add(dt.Columns[j].ColumnName, dt.Rows[i][j].ToString());
                }
                modelinfo_list.Add(plan_code, model_info);
            }

            return modelinfo_list;
        }


        //推求进度时间信息
        private static Progress_Time Get_Progress(string start_time,string end_time,string start_simulate_time)
        {
            if (start_simulate_time == "") return Progress_Time.Get_Progress_Time(0.0, 0.0);
            DateTime s_time = SimulateTime.StrToTime(start_time);
            DateTime e_time = SimulateTime.StrToTime(end_time);

            DateTime s_simulate_time = SimulateTime.StrToTime(start_simulate_time);

            double time_hours = e_time.Subtract(s_time).TotalHours;
            double total_second = Item_Info.Get_ModelRun_ElispedTime(time_hours);
            double remain_second = Math.Max(total_second - Math.Round(DateTime.Now.Subtract(s_simulate_time).TotalSeconds), 0);

            return Progress_Time.Get_Progress_Time(total_second, remain_second);
        }

        //查询该用户指定模型信息 -- (静态类)
        public static Dictionary<string, string> Get_Model_Info_FromDB(string plan_code)
        {
            Dictionary<string, string> modelinfo_list = new Dictionary<string, string>();

            //定义连接的数据库和表
            
            string plan_table = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sql = "select * from " + plan_table + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            if (dt == null) return null;
            int nLen = dt.Rows.Count;

            for (int j = 1; j < dt.Columns.Count - 2; j++)
            {
                modelinfo_list.Add(dt.Columns[j].ColumnName, dt.Rows[0][j].ToString());
            }

            
            return modelinfo_list;
        }

        //查询指定模型状态 -- (静态类)
        public static string Get_ModelState_FromDB(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select state from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("项目：{0}没有模型!", Mysql_GlobalVar.now_instance);
                return null;
            }

            
            return dt.Rows[0][0].ToString(); 
        }

        //查询指定模型所属业务模型 -- (静态类)
        public static string Get_BusinessCode_FromDB(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select business_code from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("不存在该模型方案!");
                return null;
            }

            
            return dt.Rows[0][0].ToString();
        }

        //查询业务模型的初始视角
        public static string Get_BusinessView_FromDB(string business_model)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELBUSINESS_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select view_point from " + tableName + " where code = '" + business_model + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("不存在该模型方案!");
                return null;
            }

            
            return dt.Rows[0][0].ToString();
        }
    }
}

