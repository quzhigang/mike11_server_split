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
using MySqlConnector;
using System.Threading;
using Kdbndp;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike11;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace bjd_model
{
    public class Program
    {
        [STAThread]   //在mdf类里做外挂时需要的声明
        static void Main(string[] args)
        {
            //设置模型文件路径为系统当前路径
            Model_Const.Set_Sysdir(System.Environment.CurrentDirectory);

            //初始化服务器IP、登录名和密码
            Mysql_GlobalVar.Intial_DBGlobalVar();

            //上传断面文件
            //Xns11.Readtxt_Write_sectiondata_intomysql();
            //Xns11.Readxns11_Write_Sectionobject_IntoDB();

            //上传默认模型(系统初期上传数据库时用)
            //HydroModel.Load_DefaultModel_ToDB();

            //上传指定模型
            //Load_Model_ToDB();

            //批量修改模型参数
            //Modify_Model_Pars();

            //对样例多边形进行处理(保留6位数)、建立多边形折点和断面对应关系(注意每次建筑物增减都需要重新生成!)、生成样例线(需有已计算结果模型)
            //Res11.Gis_SamplePolygonLine_Process();

            //更新河道水位流量关系(没有的全部重新计算，有且有stcd的不更新)
            //Program.Update_ReachQH_RelationTable();

            //***********贾鲁河MIKE11测试**********
            //更新水情和工情数据库
            //Update_LevelGate();

            //1、新建模型1
            //Test1_hydromodel();

            //2、新建模型2
            //Test2_hydromodel();

            //3、开始计算并显示模拟进度
            //Test3_hydromodel();

            //4、查询已有方案信息
            //Test4_hydromodel();

            //5、查询某点的数据
            //Test5_hydromodel();

            //6、删除模型
            //Test6_hydromodel();

            //7、获取模型结果信息
            //Test7_hydromodel();

            //8、获取模型GIS结果
            //Test8_hydromodel();

            //9、查询模型的调度信息
            //Test9_hydromodel();

            //10、查询纵剖面图数据
            //Test10_hydromodel();

            //11、查询河道信息
            //Test11_hydromodel();


            //12、查询结果详表
            //Test12_hydromodel();

            Console.WriteLine("运行结束!");
            Console.Read();
        }


        //上传指定溃堤模型(可控建筑物不要加溃堤建筑物，先删除溃堤建筑物只能上传一次，否则溃堤建筑物重复了)
        public static void Load_Model_ToDB()
        {
            HydroModel model = HydroModel.Get_Model_FromFiles("model_20250512100000", "1111", "");
            //Nwk11.Changeddgz_ToZMDU_FullClose(ref model, "JLH_WHGFHY");
            //Nwk11.Changeddgz_ToZMDU_FullOpen(ref model, "JLH_HCZ");
            //Nwk11.Changeddgz_ToZMDU_FullOpen(ref model, "SJH_PDZ");

            //新增溃堤建筑物
            //所在河道信息由前端坐标 判断求得
            //PointXY p = PointXY.Get_Pointxy(114.326936, 34.15863);
            //PointXY p_pro = PointXY.CoordTranfrom(p, 4326, 4547, 3);
            //double fhk_width = 150;
            //double fh_level = 62.3;
            //double break_timesecond = 60;

            ////添加分洪口，默认规则调度
            //string new_fhkname = model.Add_Fhkstr(p_pro, fhk_width, fh_level, break_timesecond);

            HydroModel.Save_ModelObject(model);
        }

        //批量修改模型参数
        public static void Modify_Model_Pars()
        {
            HydroModel default_model = HydroModel.Load("default");
            //HydroModel old_default_model = HydroModel.Get_Model_FromFiles("default_old", "50年瞬溃堤", "该方案模拟溃堤...");
            Dictionary<string, Dictionary<string, string>> model_infos = HydroModel.Get_AllModel_Info();
            for (int i = 0; i < model_infos.Count; i++)
            {
                string plan_code = model_infos.ElementAt(i).Key;
                if (plan_code != "default")
                {
                    HydroModel model = HydroModel.Load(plan_code);
                    //model.Mike11Pars.Mike11_savetimestep = SaveTimeStep.step_60;

                    //断面批量修改
                    //model.Mike11Pars.SectionList = default_model.Mike11Pars.SectionList;

                    //糙率批量修改
                    //Hd11_ParametersList hdpar = model.Mike11Pars.Hd_Pars;
                    //hdpar.Bed_Resist = default_model.Mike11Pars.Hd_Pars.Bed_Resist;

                    //可控建筑物批量修改或指定某个可控建筑物修改
                    ControlList control_list = default_model.Mike11Pars.ControlstrList;
                    model.Mike11Pars.ControlstrList = control_list;
                    //for (int j = 0; j < control_list.Count; j++)
                    //{
                    //    control_list[j] = default_control_list[j];
                    //    //if (control_list[j].Strname == "SJH_FRGSKYHD")
                    //    //{
                    //    //    Controlstr str = control_list[j];
                    //    //    Attributes atts = str.Stratts;
                    //    //    atts.sill_level = 92.5;
                    //    //    atts.initial_value = 92.5;
                    //    //}
                    //}

                    //边界条件批量修改
                    //model.Mike11Pars.BoundaryList = default_model.Mike11Pars.BoundaryList;

                    //河道修改
                    //model.Mike11Pars.SectionList = default_model.Mike11Pars.SectionList;
                    model.Save();
                }
            }
        }

        //批量修改模型参数1
        private static void Modify_ModelPars1()
        {
            //已经构建的模型，全部更改模型文件路径
            Mysql_GlobalVar.now_instance = "wg_mike11";
            Dictionary<string, Dictionary<string, string>> model_infos = HydroModel.Get_AllModel_Info();
            for (int i = 0; i < model_infos.Count; i++)
            {
                string plan_code = model_infos.ElementAt(i).Key;
                HydroModel model = HydroModel.Load(plan_code);
                Model_Files model_files = model.Modelfiles;

                HydroModel base_model = model.BaseModel;
                Model_Files base_model_files = base_model.Modelfiles;

                // 获取 Model_Files 类型对象和所有公开属性信息，用反射方式遍历类的所有属性并修改
                Type type = typeof(Model_Files);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // 遍历所有公开属性并修改属性值
                foreach (PropertyInfo property in properties)
                {
                    string source_path = property.GetValue(base_model_files).ToString();
                    string new_path = source_path.Replace(".res11", ".res1d").Replace("hd_resultHDAdd.res1d", "hd_result.ADDOUT.res1d");
                    property.SetValue(base_model_files, new_path);

                    string source_path1 = property.GetValue(model_files).ToString();
                    string new_path1 = source_path1.Replace(".res11", ".res1d").Replace("hd_resultHDAdd.res1d", "hd_result.ADDOUT.res1d");
                    property.SetValue(model_files, new_path1);
                }

                model.Save();
            }
        }

        //更新初始条件数据库
        public static void Update_LevelGate()
        {
            Item_Info.Update_LevelTable();
            //HH_INFO.Update_GateStateTable();
        }

        //更新河道水位流量关系(没有的全部重新计算，有且有stcd的不更新)
        public static void Update_ReachQH_RelationTable()
        {
            //从默认模型文件中获取模型
            Item_Info.Update_ReachQH_RelationTable();
        }

        //1、新建模型1(创建自动预报模型并计算)
        public static void Test1_hydromodel()
        {
            HydroModel.CreateRun_AutoForecastModel();
        }

        //2、新建模型2(根据水情数据更新初始水位、更新闸站调度、接入各子流域产汇流结果)
        public static void Test2_hydromodel()
        {
            // 新建模型(请求一次)
            string start_timestr = (new DateTime(2021, 7, 19, 1, 0, 0)).ToString(Model_Const.TIMEFORMAT);
            string end_timestr = (new DateTime(2021, 7, 22, 0, 0, 0)).ToString(Model_Const.TIMEFORMAT);
            string fangan_name = "方案2：预演7.20洪水";
            string fangan_desc = "本方案用最新工程模拟2021年7月河南省郑州7.20暴雨洪水";
            HydroModel hydro_model = HydroModel.Create_Model(fangan_name, start_timestr, end_timestr, fangan_desc);

            //根据实时水位更新河道水库初始水位和河道基流(贾鲁河流域)
            hydro_model.Update_InitiallevelAndBaseIn();

            //根据数据库闸站状态，修改所有的建筑物闸站调度方式及数据
            hydro_model.Update_AllStr_DdInfo_FromGB();

            //请求各子流域产汇流流量过程，修改相应的边界条件
            hydro_model.Modify_BndID_ToDischargeDic();

            //保存模型
            hydro_model.Save();

            //计算模型并存储结果
            hydro_model.Simulate();
        }

        //3、开始计算并显示模拟进度
        public static void Test3_hydromodel()
        {
            //开始模拟运算
            string plan_code = "model_20240513100000";
            double elasped_second = HydroModel.Run(plan_code);  //返回计算消耗的时间，方便前端做伪进度条

            //获取并显示模拟进度
            HydroModel hydromodel = HydroModel.Load(plan_code);
            hydromodel.Show_Progress();
        }

        //4、查询已有方案信息
        public static void Test4_hydromodel()
        {
            //获取用户所有的模型信息(不含默认模型)
            Dictionary<string, Dictionary<string, string>> model_infos = HydroModel.Get_AllModel_Info();
            for (int i = 0; i < model_infos.Count; i++)
            {
                Dictionary<string, string> infos = model_infos.ElementAt(i).Value;
                for (int j = 0; j < infos.Count; j++)
			    {
                    Console.WriteLine(infos.ElementAt(j).Key +":  " + infos.ElementAt(j).Value);
			    }
                Console.WriteLine("");
            }
        }

        //5、查询某点信息
        public static void Test5_hydromodel()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            string plan_code = "model_20230401152358";
            DateTime now_time = SimulateTime.StrToTime("2021/07/20 20:00:00");
            PointXY p = PointXY.Get_Pointxy(114.146620, 34.637008);

            //获取模型结果数据(从结果中提取)
            mike11_res point_result = HydroModel.Get_Mike11Point_Res(plan_code, p, now_time);
            Console.WriteLine("水位:{0:.00} 水深：{1:0.00} 流速:{2:0.00} 流量:{3:0.00}", point_result.Water_Level, point_result.Water_H, point_result.Average_Speed, point_result.Discharge);

            sw.Stop();

            Console.WriteLine(sw.Elapsed.ToString());
        }


        //6、删除模型
        public static void Test6_hydromodel()
        {
            string plan_code = "model_20201219151843";
            HydroModel.Delete(plan_code);
        }

        //7、获取模型计算结果
        public static void Test7_hydromodel()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            string plan_code = "model_20201221181143";
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_Result(plan_code);
            string json = File_Common.Serializer_Obj(model_result);
            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());
        }

        //8、获取模型GIS结果
        public static void Test8_hydromodel()
        {
            Stopwatch sw = new Stopwatch();
            string plan_code = "model_20201222213617"; 
            string now_time = "2020/07/01 09:00:00";

            sw.Start();
            for (int i = 0; i < 20; i++)
            {
                sw.Start();
                string model_result = HydroModel.Get_Mike11Gis_Result(plan_code, Mysql_GlobalVar.now_instance, Mike11FloodRes_GisType.water_polygon, now_time);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.ToString());
                Console.WriteLine("");
            }
        }

        //9、获取模型调度信息
        public static void Test9_hydromodel()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string plan_code = "model_20201221181143";

            //获取所有闸门的调度结果
            string model_ddinfo = HydroModel.Get_ModelGatedd_Info(plan_code);
            Console.WriteLine(model_ddinfo);

            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());
            Console.WriteLine("");
        }


        //11、查询河道信息
        public static void Test11_hydromodel()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //获取结果
            List<Reach_BasePars> reach_info = Item_Info.Get_MainReach_Info(Mysql_GlobalVar.now_instance);

            sw.Stop();
            Console.WriteLine(File_Common.Serializer_Obj(reach_info));
            Console.WriteLine("");
        }

        //12、查询结果详表
        public static void Test12_hydromodel()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //获取结果
            string plan_code = "model_20201222213617";
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_XbResult(plan_code);

            sw.Stop();
            Console.WriteLine(File_Common.Serializer_Obj(model_result));
            Console.WriteLine("");
        }

        //其他 --替换结果
        public static void Test13_Replace()
        {
            Dictionary<string, object> model_result_ks = HydroModel.Get_Mike11_GcResult("model_20240829100000");
            Mysql_GlobalVar.now_instance = "wg_mike_flood";
            Dictionary<string, object> model_result_gj = HydroModel.Get_Mike11_GcResult("model_20240829111000");

            Dictionary<string, ReachSection_FloodRes> res_ks = model_result_ks["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, ReachSection_FloodRes> res_gj = model_result_gj["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            res_gj["合河(共)"] = res_ks["合河(共)"];
            res_gj["盐土庄闸"] = res_ks["盐土庄闸"];

            //将模型流域集合类序列化后写入数据库
            MemoryStream ms = new MemoryStream();
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            binFormat.Serialize(ms, res_gj);  //将对象序列化
            byte[] buffer = ms.ToArray();  //转换成字节数组

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;

            string sql = $"UPDATE {tableName} SET reachsection_result=:Reachsection_result WHERE plan_code=:planCode AND model_instance=:modelInstance";
            KdbndpParameter[] mysqlPara =
            {
                new KdbndpParameter(":Reachsection_result", buffer),
                new KdbndpParameter(":planCode","model_20240829111000"),
                new KdbndpParameter(":modelInstance", "wg_mike_flood")
            };
            Mysql.Execute_Command(sql, mysqlPara);
            ms.Close();
            ms.Dispose();
        }

    }
}