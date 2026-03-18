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
using bjd_model.Mike11;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;

namespace bjd_model
{
    public partial class HydroModel
    {
        //删除模型 -- 根据模型名字从数据库中删除模型，同时删除服务器上的模型文件
        private static void Delete_Model(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //删除数据库中的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                Mysql.Execute_Command(del_sql);
            }

            

            //删除模型文件
            string dirname = Model_Files.Get_Modeldir(plan_code);
            if (Directory.Exists(dirname))
            {
                Directory.Delete(dirname, true);
            }

            Console.WriteLine("模型{0}已经成功删除！", plan_code);
        }

        //删除模型mike11结果 -- 根据模型名从数据库中删除模型结果
        private static void Delete_Model_Res11(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //删除数据库中的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                Mysql.Execute_Command(del_sql);
            }

            

            Console.WriteLine("模型{0}mike11结果已经成功删除！", plan_code);
        }

        //删除模型mike11GIS结果 -- 根据模型名从数据库中删除模型结果
        private static void Delete_Model_GisLineRes11(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISLineRES_TABLENAME;
            

            //删除数据库中的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                Mysql.Execute_Command(del_sql);
            }

            

            Console.WriteLine("模型{0}mike11GIS线结果已经成功删除！", plan_code);
        }

        //删除模型mike11GIS结果 -- 根据模型名从数据库中删除模型结果
        private static void Delete_Model_GisPolygonRes11(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISPOLYGONRES_TABLENAME;
            

            //删除数据库中的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                Mysql.Execute_Command(del_sql);
            }

            

            Console.WriteLine("模型{0}mike11GIS面结果已经成功删除！", plan_code);
        }

        //将模型名或模型方案名统一修改为模型名(影响效率，去掉)
        public static string Get_ModelName(string plan_code)
        {
            string res_modelname = plan_code;

            //获取模型的真名（自动生成的模型名，如fangan1）
            List<HydroModel> user_hydromodels = Load_AllModel();
            if (plan_code != Model_Const.DEFAULT_MODELNAME)
            {
                for (int i = 0; i < user_hydromodels.Count; i++)
                {
                    //如果用户给的是方案名，则换算成模型名
                    if (user_hydromodels[i].Model_Faname == plan_code)
                    {
                        res_modelname = user_hydromodels[i].Modelname;
                        break;
                    }
                }
            }

            return res_modelname;
        }

        //传递模型 -- 各用户之间传递模型（数据库里用户的模型增加）
        private static void Send_Model_ToOtherUser(string plan_code, string other_username)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //先判断该用户是否有该模型
            string select_sql = "select model_object from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + other_username + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                Console.WriteLine("用户{0}已经具有了相同的模型名:{1}!", other_username, plan_code);
                return;
            }

            //在数据库中找出模型
            string select_sql1 = "select plan_code,model_desc,model_object from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt1 = Mysql.query(select_sql1);
            if (dt1.Rows.Count == 0)
            {
                Console.WriteLine("未找到该模型{0]", plan_code);
                return;
            }

            //下载模型并修改模型文件路径
            HydroModel model = HydroModel.Load(plan_code);
            HydroModel.Modify_ModelFilePath(ref model, other_username);

            //将模型名、描述、模型对象 存入数据库
            MemoryStream ms = new MemoryStream();   //将模型流域集合类序列化后写入数据库
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            binFormat.Serialize(ms, model);  //将对象序列化
            byte[] buffer = ms.ToArray();  //转换成字节数组

            string insert_sql = "insert into " + tableName + " (model_instance,plan_code,model_desc,model_object) values(:model_instance,:plan_code,:model_desc,:model_object)";
            KdbndpParameter[] mysqlPara = { new KdbndpParameter(":model_instance", other_username),
                                             new KdbndpParameter(":plan_code", dt1.Rows[0][0].ToString()), 
                                             new KdbndpParameter(":model_desc", dt1.Rows[0][1].ToString()) ,
                                             new KdbndpParameter(":model_object", buffer) 
                                         };

            using (KdbndpCommand mysqlcmd = new KdbndpCommand(insert_sql))
            {
                mysqlcmd.Parameters.AddRange(mysqlPara);
                mysqlcmd.ExecuteNonQuery();
            }

            //将模型文件考一份到该用户下
            string source_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance + @"\" + plan_code;
            string dest_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + other_username + @"\" + plan_code;
            if (!Directory.Exists(dest_dir))
            {
                Directory.CreateDirectory(dest_dir);
            }

            File_Common.Copy_Directory_Allfile(source_dir, dest_dir);
            

            Console.WriteLine("模型传输完成！");
        }

        //修改模型文件路径到指定用户
        private static void Modify_ModelFilePath(ref HydroModel model, string model_instance)
        {
            Model_Files modelfiles = model.Modelfiles;

            //模型文件夹
            string model_dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + model.Modelname;

            modelfiles.Simulate_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Simulate_filename);
            modelfiles.Nwk11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Nwk11_filename);
            modelfiles.Xns11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Xns11_filename);
            modelfiles.Bnd11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Bnd11_filename);
            modelfiles.Rr11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Rr11_filename);
            modelfiles.Hd11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Hd11_filename);
            modelfiles.Ad11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Ad11_filename);

            //新建结果文件夹
            string model_result_dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + model.Modelname + @"\" + "results";
            modelfiles.Hdres11_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Hdres11_filename);
            modelfiles.Rrres11_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Rrres11_filename);
            modelfiles.XAJres_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.XAJres_filename);
            modelfiles.Hdtxt_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Hdtxt_filename);

            modelfiles.Progressinfo_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Progressinfo_filename);
        }
        
        //*******************************************************************************


        //新建模型-- 根据基础模型名字是否为null确定是否有基础模型
        private static HydroModel Create_NewModel(string fangan_name,string start_timestr, string end_timestr,
            string model_desc, string base_plan_code, string plan_code = "", int step_saveminutes = 60)
        {
            if(plan_code == "")
            {
                //给新模型命名 "model_" + 当前建模时间
                string name = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") +
                    DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00");
                plan_code = "model_" + name;
            }

            //模型开始时间：如果开始时间是现在或历史，则采用默认提前量，否则不提前
            DateTime start_time = SimulateTime.StrToTime(start_timestr);
            int ahead_hours = DateTime.Compare(start_time, DateTime.Now.AddHours(1.0)) < 0 ? Model_Const.AHEAD_HOURS : 0;

            //模拟时间
            SimulateTime simulatetime;
            simulatetime.Begin = start_time.AddHours(-1 * ahead_hours);//提前时间
            simulatetime.End = SimulateTime.StrToTime(end_timestr);

            //模型选择
            CalculateModel cal_model = CalculateModel.only_hd;

            //根据前端是否提交基础模型名，来判断基础模型名，基础模型名由方案名得到(前端提交方案名)
            HydroModel hydromodel = new HydroModel(base_plan_code, plan_code, fangan_name, simulatetime,cal_model,model_desc,ahead_hours);

            //修改保存时间步长
            SaveTimeStep save_timestep = (SaveTimeStep)Enum.Parse(typeof(SaveTimeStep), step_saveminutes.ToString());
            hydromodel.Set_Mike11_SaveTimeStep(save_timestep);

            //直接保存模型
            hydromodel.Save();   //请求数据库

            return hydromodel;
        }

        //以当前模型，修改继承的基础模型调度信息
        public static void Correct_DDInfo(HydroModel model, int calculate_type, ref Dictionary<string, List<string[]>> Model_Ddinfo)
        {
            DateTime start_time = model.ModelGlobalPars.Simulate_time.Begin;
            DateTime end_time = model.ModelGlobalPars.Simulate_time.End;

            //纯水动力学则去掉水质;不在事件范围内的按起止时间
            DateTime adsource_time = Model_Ddinfo.Last().Key == "wry_nd" ? SimulateTime.StrToTime(Model_Ddinfo.Last().Value[0][4]) : new DateTime(2000,1,1);
            if (calculate_type == 1)
            {
                Model_Ddinfo.Remove(Model_Ddinfo.Last().Key);
                Model_Ddinfo.Add("污染源拟定信息", new List<string[]>());
            }
            else
            {
                // 基础模型没有污染源拟定信息
                if (adsource_time == new DateTime(2000, 1, 1))
                {
                    List<string[]> ad_info_list = new List<string[]>();
                    string[] adinfo = new string[6];
                    adinfo[0] = "可溶性液体";
                    adinfo[1] = "215+531";
                    adinfo[2] = "0.0";
                    adinfo[3] = start_time.ToString(Model_Const.TIMEFORMAT);  //泄漏起始时间改为开始时间
                    adinfo[4] = end_time.ToString(Model_Const.TIMEFORMAT);  //泄漏结束时间改为结束时间
                    adinfo[5] = "缺少污染源拟定！";
                    ad_info_list.Add(adinfo);

                    Model_Ddinfo.Add("wry_nd", ad_info_list);
                }
                else if (adsource_time.Subtract(start_time).TotalHours <= 0 || adsource_time.Subtract(end_time).TotalHours >= 0)
                {
                    // 继承了基础模型的污染源拟定，但时间不在现在模型的时间范围内
                    string[] adinfo = Model_Ddinfo.Last().Value[0];
                    adinfo[2] = "0.0";
                    adinfo[3] = start_time.ToString(Model_Const.TIMEFORMAT);  //泄漏起始时间改为开始时间
                    adinfo[4] = end_time.ToString(Model_Const.TIMEFORMAT);  //泄漏结束时间改为结束时间
                    adinfo[5] = "缺少污染源拟定！";
                    Model_Ddinfo.Last().Value[0] = adinfo;
                }
            }

            //修改调度信息
            for (int i = 0; i < Model_Ddinfo.Count -1; i++)
            {
                //不在新模型时间范围内的去掉
                List<string[]> ddinfo = Model_Ddinfo.ElementAt(i).Value;
                for (int j = 0; j < ddinfo.Count; j++)
                {
                    if (ddinfo[j][2] == "规则调度") continue;
                    string[] ddzl_str = ddinfo[j][3].Split(new char[] { ';' });
                    string new_ddzl_str = "";
                    for (int k = 0; k < ddzl_str.Length; k++)
                    {
                        if (ddzl_str[k] == "") continue;  //最后一个分割出来的为""
                        DateTime zmdd_start_time = SimulateTime.StrToTime(ddzl_str[k].Substring(0,19));
                        if (zmdd_start_time.Subtract(start_time).TotalHours >= 0 && zmdd_start_time.Subtract(end_time).TotalHours <= 0)
                        {
                            new_ddzl_str += ddzl_str[k];
                            new_ddzl_str += ";";
                        }
                    }

                    //如果没有在模拟时间范围内的，则改为规则调度
                    if (new_ddzl_str == "")
                    {
                        ddinfo[j][2] = "规则调度";
                        GateType str_type = Item_Info.Get_Gate_Type(ddinfo[j][1]);
                        ddinfo[j][3] = Item_Info.Get_Gate_Rule(str_type);

                    }
                }
            }
        }

        //修改调度
        public static string[] Correct_DDInfo(HydroModel model, string strname, int ddfs, Dictionary<string, int> strddzl_list, ref Dictionary<string, List<string[]>> Model_Ddinfo)
        {
            string[] str_ddinfo = null;

            //找到这个闸门的调度信息
            for (int i = 0; i < Model_Ddinfo.Count -1; i++)
            {
                List<string[]> ddinfo = Model_Ddinfo.ElementAt(i).Value;
                for (int j = 0; j < ddinfo.Count; j++)
                {
                    if (Item_Info.Get_StrChinaName(strname) == ddinfo[j][1])
                    {
                        str_ddinfo = ddinfo[j];

                        string info2 = "";
                        string info3 = "";
                        //修改第2、3条调度信息
                        if(ddfs == 1)
                        {
                            info2 = "指令调度";
                            info3 = Get_Ddinfo3(strddzl_list);

                        }
                        else
                        {
                            info2 = "规则调度";
                            info3 = Item_Info.Get_Gate_Rule(Item_Info.Get_Gate_Type(strname));
                        }
                        ddinfo[j][2] = info2;
                        ddinfo[j][3] = info3;
                        break;
                    }

                }
            }

            //返回该闸门调度信息字符串数组
            return str_ddinfo;
        }

        //获取第3条（调度过程信息）,从指令
        private static string Get_Ddinfo3(Dictionary<string, int> strddzl_list)
        {
            string ddinfo3 = "";
            if (strddzl_list == null) return null;

            //Dictionary<string, int> strddzl_list = new Dictionary<string, int>();
            //strddzl_list.Add("2018/05/31 08:00:00", 2);  //闸门关闭
            //strddzl_list.Add("2018/05/31 15:00:00", 1); //闸门开启
            for (int i = 0; i < strddzl_list.Count; i++)
            {
                if (strddzl_list.ElementAt(i).Value == 2)
                {
                    ddinfo3 += strddzl_list.ElementAt(i).Key + "关闭闸门;";
                }
                else
                {
                    ddinfo3 += strddzl_list.ElementAt(i).Key + "开启闸门;";
                }
            }
            return ddinfo3;
        }

        //修改保存时间步长
        private void Change_Modelsavestep()
        {
            //改为默认保存时间
            this.Set_Mike11_SaveTimeStep(Model_Const.MIKE11_SAVESTEPTIME);

            //如果模拟时间太长，而保存步长又太短，则更正保存时间步长，以确保前端显示(如每秒2步)时不至于时间太长(小于10分钟)
            double simulatehours = this.ModelGlobalPars.Simulate_time.End.Subtract(this.ModelGlobalPars.Simulate_time.Begin).TotalHours;
            int save_steps = (int)this.Mike11Pars.Mike11_savetimestep;
            if (simulatehours > 400 && save_steps < 30)
            {
                this.Set_Mike11_SaveTimeStep(SaveTimeStep.step_30);
            }
            else if (simulatehours > 200 && save_steps < 10)
            {
                this.Set_Mike11_SaveTimeStep(SaveTimeStep.step_10);
            }
            else if (simulatehours > 100 && save_steps < 5)
            {
                this.Set_Mike11_SaveTimeStep(SaveTimeStep.step_5);
            }
        }
    }
}

