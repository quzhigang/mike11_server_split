using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using DHI.PFS;
using System.Collections;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using System.Data;
using Kdbndp;
using Newtonsoft.Json;

namespace bjd_model.Mike11
{
    //测站实时水情数据
    [Serializable]
    public struct Water_Condition
    {
        public string Name;
        public string Reach;
        public string Reach_CnName;
        public double Chainage;
        public double Level;           //监测水位
        public double Discharge;       //监测流量
        public string Update_Date;    //监测数据更新时间
        public string Stcd;          //监测断面stcd  
        public string Datasource;   //监测数据来源
        public string Station_Type; //站点类型，主要还是次要
        public string Level1;  //警戒水位
        public string Level2;  //水位的防洪高水位
        public string Level3;  //保证水位
        public string More_info;  //更多的信息
        public double Control_Area; //控制流域面积
        public List<string> Model_instance; //水情所属的模型实例

        public static Water_Condition Get_NowWater(string name,string reach,string reach_cnname,
            double chainage, double level,double discharge, string date,string stcd, string datasource,
            string station_type, string level1, string level2, string level3,string more_info,double control_area, List<string> model_instances)
        {
            Water_Condition now_water;
            now_water.Name = name;
            now_water.Reach = reach;
            now_water.Reach_CnName = reach_cnname;
            now_water.Chainage = chainage;
            now_water.Level = level;
            now_water.Discharge = discharge;
            now_water.Update_Date = date;
            now_water.Stcd = stcd;
            now_water.Datasource = datasource;
            now_water.Station_Type = station_type;
            now_water.Level1 = level1;
            now_water.Level2 = level2;
            now_water.Level3 = level3;
            now_water.More_info = more_info;
            now_water.Control_Area = control_area;
            now_water.Model_instance = model_instances;

            return now_water;
        }

        public static string Get_Stcd(List<Water_Condition> water_condition,string name)
        {
            for (int i = 0; i < water_condition.Count; i++)
            {
                if (water_condition[i].Name == name) return water_condition[i].Stcd;
            }
            return "";
        }

        public static Water_Condition Get_WaterCondition(List<Water_Condition> water_condition, string name)
        {
            Water_Condition condition = water_condition[0];
            for (int i = 0; i < water_condition.Count; i++)
            {
                if (water_condition[i].Name == name)
                {
                    condition = water_condition[i];
                }
            }
            return condition;
        }

        public static Water_Condition Get_WaterCondition1(List<Water_Condition> water_condition, string stcd)
        {
            Water_Condition condition = water_condition[0];
            for (int i = 0; i < water_condition.Count; i++)
            {
                if (water_condition[i].Stcd == stcd)
                {
                    condition = water_condition[i];
                }
            }
            return condition;
        }
    }

    //热启动参数
    [Serializable]
    public struct Hotstart
    {
        public bool Use_Hotstart;
        public string Hotstart_file;
        public DateTime Hotstart_datetime;
        public static Hotstart Get_Hotstart(string hotstart_file,DateTime hotstart_time)
        {
            Hotstart hotstart;
            hotstart.Use_Hotstart = true;
            hotstart.Hotstart_file = hotstart_file;
            hotstart.Hotstart_datetime = hotstart_time;
            return hotstart;
        }
        public static Hotstart Get_Default_Hotstart()
        {
            Hotstart hotstart;
            hotstart.Use_Hotstart = false;
            hotstart.Hotstart_file = "";
            hotstart.Hotstart_datetime = DateTime.Now;
            return hotstart;
        }
    }

    public class Hd11
    {
        #region ***************************从hd11文件中获取参数信息*********************************
        //从默认hd11参数文件中获取已有参数信息
        public static void GetDefault_Hd11Info(string sourcefilename, ref Hd11_ParametersList Hd11_Pars)
        {
            //读取PFS文件
            PFSFile pfsfile = new PFSFile(sourcefilename);   //读取文件
            PFSSection target = pfsfile.GetTarget("MIKE0_HD", 1);   //最外面的节

            //获取初始水位信息
            PFSSection InitList = target.GetSection("InitList", 1);  //第2层节：河道局部初始条件
            Dictionary<AtReach, double> InitialWater = new Dictionary<AtReach, double>();
            for (int i = 0; i < InitList.GetKeywordsCount(); i++)
            {
                AtReach atreach;
                atreach.reachname = InitList.GetKeyword("DATA", i + 1).GetParameter(1).ToString();
                atreach.reachid = atreach.reachname + Model_Const.REACHID_HZ;
                atreach.chainage = InitList.GetKeyword("DATA", i + 1).GetParameter(2).ToDouble();

                double initlevel = InitList.GetKeyword("DATA", i + 1).GetParameter(3).ToDouble();
                InitialWater.Add(atreach, initlevel);
            }
            Hd11_Pars.InitialWater = InitialWater;

            //获取糙率信息
            PFSSection BedList = target.GetSection("BedList", 1);  //第2层节：河道局部初始条件
            Dictionary<Reach_Segment, double> Bed_Resist = new Dictionary<Reach_Segment, double>();
            for (int i = 0; i < BedList.GetKeywordsCount() / 2; i++)
            {
                Reach_Segment reach_seg;
                reach_seg.reachname = BedList.GetKeyword("DATA", 2 * i + 1).GetParameter(1).ToString();
                reach_seg.reachtopoid = reach_seg.reachname + Model_Const.REACHID_HZ;
                reach_seg.start_chainage = BedList.GetKeyword("DATA", 2 * i + 1).GetParameter(2).ToDouble();
                reach_seg.end_chainage = BedList.GetKeyword("DATA", 2 * i + 2).GetParameter(2).ToDouble();

                double bedresist = BedList.GetKeyword("DATA", 2 * i + 2).GetParameter(3).ToDouble();
                Bed_Resist.Add(reach_seg, bedresist);
            }
            Hd11_Pars.Bed_Resist = Bed_Resist;

            //获取结果输出文件路径信息
            PFSSection Global_Variables = target.GetSection("Global_Variables", 1);  //第2层节：全局变量
            PFSSection TextOutput = Global_Variables.GetSection("TextOutput", 1);  //第3层节：文件输出
            PFSSection OutputFiles = TextOutput.GetSection("OutputFiles", 1);  //第4层节：输出路径
            if(OutputFiles.GetKeywordsCount() != 0)
            {
                PFSKeyword data = OutputFiles.GetKeyword("data", 1);
                string TxtFilename = data.GetParameter(3).ToFileNamePath();
                //补全路径
                if (TxtFilename.StartsWith("."))
                {
                    TxtFilename = Path.GetDirectoryName(sourcefilename) + TxtFilename.Substring(1);
                }
                Hd11_Pars.TxtFilename = TxtFilename;
            }
            else
            {
                Hd11_Pars.TxtFilename = "";
            }

            //获取全局河道糙率
            PFSSection Global_Values = Global_Variables.GetSection("Global_Values", 1);
            double globlesistance = Global_Values.GetKeyword("G_resistance", 1).GetParameter(1).ToDouble();
            Hd11_Pars.Global_Resist = globlesistance;

            //获取河道结果输出点信息
            PFSSection OutputGridPoints = target.GetSection("OutputGridPoints", 1);  //第2层节：输出节点
            List<Reach_Segment> outputgrid = new List<Reach_Segment>();
            for (int i = 0; i < OutputGridPoints.GetKeywordsCount(); i++)
            {
                Reach_Segment reach_seg;
                reach_seg.reachname = OutputGridPoints.GetKeyword("DATA", i + 1).GetParameter(1).ToString();
                reach_seg.reachtopoid = reach_seg.reachname + Model_Const.REACHID_HZ;
                reach_seg.start_chainage = OutputGridPoints.GetKeyword("DATA", i + 1).GetParameter(2).ToDouble();
                reach_seg.end_chainage = OutputGridPoints.GetKeyword("DATA", i + 1).GetParameter(3).ToDouble();

                outputgrid.Add(reach_seg);
            }
            Hd11_Pars.Reach_OutputGrid = outputgrid;

            Console.WriteLine("hd11参数信息初始化成功!");
            pfsfile.Close();
        }
        #endregion *******************************************************************************************


        #region ************************************** 更新hd11文件 *****************************************
        // 提取最新的参数信息，更新hd11文件
        public static void Rewrite_Hd11_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Hd11_filename;
            string outputfilename = hydromodel.Modelfiles.Hd11_filename;
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;
            if (hd11pars == null) return;

            //打开PFS文件，开始编辑
            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection MIKE0_HD = pfsfile.GetTarget("MIKE0_HD", 1);   //目标

            //更新全局变量节
            Update_GlobalVarSec(hd11pars, ref MIKE0_HD);
            Console.WriteLine("全局变量更新成功!");

            //更新初始条件节
            Update_InitListSec(hd11pars, ref MIKE0_HD);
            Console.WriteLine("初始条件更新成功!");

            //更新河道糙率节
            Update_BedListSec(hd11pars, ref MIKE0_HD);
            Console.WriteLine("河道糙率更新成功!");

            //更新输出河道节点
            Update_OutputGridSec(hd11pars, ref MIKE0_HD);
            Console.WriteLine("河道输出节点更新成功!");

            //重新生成hd11文件
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("Hd11一维参数文件更新成功!");
            Console.WriteLine("");
        }

        //更新全局变量节 - 修改全局糙率、输出txt文件路径
        public static void Update_GlobalVarSec(Hd11_ParametersList hd11pars, ref PFSSection MIKE0_HD)
        {
            PFSSection Global_Variables = MIKE0_HD.GetSection("Global_Variables", 1);  //第2层节：全局变量

            //修改输出文件路径
            PFSSection TextOutput = Global_Variables.GetSection("TextOutput", 1);  //第3层节：文件输出
            PFSSection OutputFiles = TextOutput.GetSection("OutputFiles", 1);  //第4层节：输出路径
            if(OutputFiles.GetKeywordsCount() !=0)
            {
                PFSKeyword data = OutputFiles.GetKeyword("data", 1);
                if (hd11pars.TxtFilename != null)
                {
                    data.GetParameter(3).ModifyFileNameParameter(hd11pars.TxtFilename);
                }
            }

            //修改全局糙率
            PFSSection Global_Values = Global_Variables.GetSection("Global_Values", 1);
            Global_Values.GetKeyword("G_resistance", 1).DeleteParameter(1);
            Global_Values.GetKeyword("G_resistance", 1).InsertNewParameterDouble(hd11pars.Global_Resist, 1);
        }

        //更新初始条件节
        public static void Update_InitListSec(Hd11_ParametersList hd11pars, ref PFSSection MIKE0_HD)
        {
            //清空原默认hd11文件中的参数集合
            MIKE0_HD.DeleteSection("InitList", 1);  //删除的节也是单独排
            PFSSection InitList = MIKE0_HD.InsertNewSection("InitList", 1);  //重新添加边界数组的节

            //重新逐个添加初始条件关键字
            Dictionary<AtReach, double> InitialWater = hd11pars.InitialWater;
            if (InitialWater == null) return;

            AtReach last_atreach = AtReach.Get_Atreach("", 0);
            for (int i = 0; i < InitialWater.Count; i++)
            {
                AtReach atreach = InitialWater.Keys.ElementAt(i);
                double initiallevel = InitialWater.Values.ElementAt(i);

                //逐个添加最新的初始水位
                if (!atreach.Equals(last_atreach))
                {
                    PFSKeyword data1 = InitList.InsertNewKeyword("DATA", i + 1);
                    object[] data1_array = { atreach.reachname, atreach.chainage, initiallevel, 0.1 };
                    Nwk11.InsertKeyPars(ref data1, data1_array);

                    last_atreach = atreach;
                }
            }

            //初始条件类型修改为水位
            if (InitialWater.Count != 0)
            {
                PFSSection Global_Variables = MIKE0_HD.GetSection("Global_Variables", 1);  //第2层节：全局变量
                PFSSection Global_Values = Global_Variables.GetSection("Global_Values", 1);
                PFSKeyword iLevelDepth = Global_Values.GetKeyword("iLevelDepth", 1);
                iLevelDepth.GetParameter(1).ModifyIntParameter(0);
            }
        }

        //更新河道糙率节
        public static void Update_BedListSec(Hd11_ParametersList hd11pars, ref PFSSection MIKE0_HD)
        {
            //清空原默认hd11文件中的参数集合
            MIKE0_HD.DeleteSection("BedList", 1);  //删除的节也是单独排
            PFSSection BedList = MIKE0_HD.InsertNewSection("BedList", 1);  //重新添加边界数组的节

            //重新逐个添加河道糙率关键字
            Dictionary<Reach_Segment, double> Bed_Resist = hd11pars.Bed_Resist;
            if (Bed_Resist == null) return;

            for (int i = 0; i < Bed_Resist.Count; i++)
            {
                Reach_Segment reach_segment = Bed_Resist.Keys.ElementAt(i);
                double bedresist = Bed_Resist.Values.ElementAt(i);

                //逐个添加最新的河道糙率
                PFSKeyword data1 = BedList.InsertNewKeyword("DATA", 2 * i + 1);
                object[] data1_array = { reach_segment.reachname, reach_segment.start_chainage, bedresist, 0.03, 0.04, 0.05 };
                Nwk11.InsertKeyPars(ref data1, data1_array);

                PFSKeyword data2 = BedList.InsertNewKeyword("DATA", 2 * i + 2);
                object[] data2_array = { reach_segment.reachname, reach_segment.end_chainage, bedresist, 0.03, 0.04, 0.05 };
                Nwk11.InsertKeyPars(ref data2, data2_array);
            }
        }

        //更新输出河道节点
        public static void Update_OutputGridSec(Hd11_ParametersList hd11pars, ref PFSSection MIKE0_HD)
        {
            //清空原默认hd11文件中的参数集合
            MIKE0_HD.DeleteSection("OutputGridPoints", 1);  //删除的节也是单独排
            PFSSection OutputGrid = MIKE0_HD.InsertNewSection("OutputGridPoints", 1);  //重新添加边界数组的节

            //重新逐个添加河道输出节点关键字
            List<Reach_Segment> Bed_OutGrid = hd11pars.Reach_OutputGrid;
            if (Bed_OutGrid == null) return;

            for (int i = 0; i < Bed_OutGrid.Count; i++)
            {
                Reach_Segment reach_segment = Bed_OutGrid[i];

                //逐个添加最新的河道输出节点
                PFSKeyword data1 = OutputGrid.InsertNewKeyword("DATA", i);
                object[] data1_array = { reach_segment.reachname, reach_segment.start_chainage, reach_segment.end_chainage };
                Nwk11.InsertKeyPars(ref data1, data1_array);
            }
        }

        #endregion ********************************************************************************************


        #region ************************************* Hd11参数修改操作 *****************************************
        //根据数据库实时水位更新全部河道水库初始水位
        public static void Update_InitialWaterlevel(ref HydroModel hydromodel)
        {
            Dictionary<AtReach, double> initial_water = hydromodel.Mike11Pars.Hd_Pars.InitialWater;

            //从数据库中读取全部实时水位数据
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //修改水库的水位初始条件
            Modify_ResInitial(ref initial_water, now_waterlevel);

            //修改河道控制断面的水位初始条件
            //Dictionary<AtReach, double> custom_section_level = HH_INFO.Get_Custom_Section_InitialLevel();
            Modify_ReachInitial(ref initial_water, now_waterlevel);

            //更新数据库初始水情信息
            string initial_water_info = WG_INFO.Get_Model_InitialInfo(hydromodel); 
            Update_ModelPara_DBInfo(hydromodel.Modelname, "mike11_initial_info", initial_water_info);
        }

        //根据给定水位更新全部河道水库初始水位
        public static void Update_InitialWaterlevel(ref HydroModel hydromodel, Dictionary<string, double> inital_level)
        {
            Dictionary<AtReach, double> initial_water = hydromodel.Mike11Pars.Hd_Pars.InitialWater;

            //从数据库中读取全部实时水位数据
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //修改实时水位为指定水位
            List<Water_Condition> new_waterlevel = new List<Water_Condition>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                Water_Condition wt = now_waterlevel[i];
                if (inital_level.Keys.Contains(wt.Stcd))
                {
                    double level = inital_level[wt.Stcd];
                    if (level != 0)
                    {
                        wt.Level = level;
                        new_waterlevel.Add(wt);
                    }
                }
            }

            //修改水库的水位初始条件
            Modify_ResInitial(ref initial_water, new_waterlevel);

            //修改河道控制断面的水位初始条件
            Modify_ReachInitial(ref initial_water, new_waterlevel);

            //更新初始水情信息
            string initial_water_info = WG_INFO.Get_Model_InitialInfo(hydromodel, inital_level);
            Update_ModelPara_DBInfo(hydromodel.Modelname, "mike11_initial_info", initial_water_info);
        }

        //通过迭代修改模拟开始时刻 水库初始水位值，以还原方案开始时刻水位
        public static void Update_ResInitiallevel_FromIter(ref HydroModel hydromodel, List<object> model_res)
        {
            Dictionary<AtReach, double> initial_water = hydromodel.Mike11Pars.Hd_Pars.InitialWater;

            //模型结果
            Dictionary<string, Reservoir_FloodRes> res_floodres = model_res[0] as Dictionary<string, Reservoir_FloodRes>;

            //从数据库中读取全部实时水位数据
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //获取所有水库基本信息
            List<Reservoir> res_info = WG_INFO.Get_ResInfo(Mysql_GlobalVar.now_instance);

            //从数据库获取一维模型水库的初始水情条件，如果并没有设置过初始水位(数据库无记录)，则直接返回不再修改
            Dictionary<string, double> res_initial_levels = Get_Res_InitialLevel(hydromodel.Modelname);
            if (res_initial_levels == null) return;

            //逐一修改模型初始时刻 水库水位
            List<Water_Condition> model_waterlevel = new List<Water_Condition>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                Water_Condition wt = now_waterlevel[i];
                if (res_floodres.Keys.Contains(wt.Name))
                {
                    //水库基本信息、模型结果
                    Reservoir_FloodRes floodres = res_floodres[wt.Name];
                    Reservoir resinfo = res_info.FirstOrDefault(s => s.Name == wt.Name);
                    double res_initiallevel = res_initial_levels.Keys.Contains(wt.Name) ? res_initial_levels[wt.Name] : floodres.Level_Dic.First().Value;

                    //通过迭代计算，获取模型初始水位
                    double level = Get_Model_ResInitialLevel(resinfo, floodres, res_initiallevel);
                    wt.Level = level;
                    model_waterlevel.Add(wt);
                }
            }

            //修改水库的水位初始条件
            Modify_ResInitial(ref initial_water, model_waterlevel);
        }

        //从数据库获取水库初始水位
        public static Dictionary<string, double> Get_Res_InitialLevel(string model_name)
        {
            Dictionary<string, double> res_initial_levels = new Dictionary<string, double>();
            string intial_water = WG_INFO.Get_Model_SingleParInfo(model_name, "mike11_initial_info");
            if (intial_water == null || intial_water == "") return null;

            Dictionary<string, List<List<object>>> intial_water_obj = JsonConvert.DeserializeObject<Dictionary<string, List<List<object>>>>(intial_water);
            List<List<object>> intial_water_array = intial_water_obj["水库"];
            for (int i = 0; i < intial_water_array.Count; i++)
            {
                string res_name = intial_water_array[i][1].ToString();
                double res_level = Double.Parse(intial_water_array[i][2].ToString());
                res_initial_levels.Add(res_name, res_level);
            }

            return res_initial_levels;
        }

        //通过迭代计算，获取模型初始水位
        public static double Get_Model_ResInitialLevel(Reservoir resinfo, Reservoir_FloodRes floodres, double des_level)
        {
            //数据准备
            Dictionary<double, double> res_lv = resinfo.Level_Volume;
            Dictionary<DateTime, double> inq_dic = floodres.InQ_Dic;
            Dictionary<DateTime, double> outq_dic = floodres.OutQ_Dic;

            //通过迭代计算水库消峰流量
            double min_level = des_level - 10;
            double max_level = des_level + 10;
            double initial_level = (min_level + max_level) / 2;

            //进行水库迭代调蓄计算
            int n = 0;
            Dictionary<double, double> iter_levels = new Dictionary<double, double>();
            while (true)
            {
                //进行水库调蓄计算，返回最后时刻库水位
                double last_level = Get_Res_LastLevel(res_lv, inq_dic, outq_dic, initial_level);

                //当试算最后时刻库水位与目标水位之间误差小于0.01m 
                double wc = Math.Abs(last_level - des_level);
                if (!iter_levels.Keys.Contains(initial_level)) iter_levels.Add(initial_level, wc);
                if (wc < 0.01 || n >= 20)
                {
                    if (n == 20) initial_level = iter_levels.OrderBy(kvp => kvp.Value).First().Key;
                    break;
                }

                //继续迭代
                if (last_level > des_level)
                {
                    //当计算的最终库水位大于目标水位，表示初始水位设置偏大，应减小
                    max_level = initial_level;
                }
                else
                {
                    min_level = initial_level;
                }

                initial_level = (min_level + max_level) / 2;
                n++;
            }

            return initial_level;
        }

        //进行水库调蓄计算，返回最后时刻库水位
        public static double Get_Res_LastLevel(Dictionary<double, double> res_lv, Dictionary<DateTime, double> inq_dic,
            Dictionary<DateTime, double> outq_dic, double initial_level)
        {
            //逐时间步进行水库调洪演算，计算水库水位
            double last_level = initial_level;
            double last_res_volumn = File_Common.Insert_Zd_Value(res_lv, last_level);
            double last_in_discharge = inq_dic.First().Value;
            double step_minutes = inq_dic.ElementAt(1).Key.Subtract(inq_dic.ElementAt(0).Key).TotalMinutes;
            for (int i = 0; i < inq_dic.Count; i++)
            {
                //当前入库和出库流量
                double now_in_discharge = inq_dic.ElementAt(i).Value;
                double now_out_discharge = outq_dic.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_discharge + now_in_discharge) / 2.0) * (step_minutes * 60);

                //该时段出库洪量(m3)
                double out_volumn = now_out_discharge * (step_minutes * 60);

                //当前水库库容(万m3)
                double now_volumn = last_res_volumn + (in_volumn - out_volumn) / 10000.0;

                //反向内插当前水库水位
                double now_level = File_Common.Insert_Zd_Key(res_lv, now_volumn);

                last_level = now_level;
                last_res_volumn = now_volumn;
                last_in_discharge = now_in_discharge;
            }

            return last_level;
        }


        //更新数据库里初始水情、边界条件等信息
        public static void Update_ModelPara_DBInfo(string model_name,string field_name,string info)
        {
            string model_instance = Mysql_GlobalVar.now_instance;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //先判断旧的信息
            string select_sql = "select plan_code from " + tableName + " where plan_code = '" + model_name + "' and model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0 )
            {
                string sql = "update " + tableName + " set "+ field_name + " = '" + info + "' where plan_code = '" + model_name + "' and model_instance = '" + model_instance + "'";
                Mysql.Execute_Command(sql);
            }
            
        }

        //修改水库的水位初始条件
        public static void Modify_ResInitial(ref Dictionary<AtReach, double> initial_water, List<Water_Condition> now_water)
        {
            //从中刷选出有监测数据的水库水位
            Dictionary<string, double> res_levels = new Dictionary<string, double>();
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Datasource == "水库水文站" && now_water[i].Level != 0 && now_water[i].Update_Date != null)
                {
                    res_levels.Add(now_water[i].Name, now_water[i].Level);
                }
            }

            //获取 有实测水位的水库水位站 控制的一位河道HD11中初始条件断面(键为水库水情监测断面名称，值为控制的河段集合)
            Dictionary<string, List<AtReach>> res_initial_atreach = WG_INFO.GetRes_ControlInitialAtreach(res_levels.Keys.ToList());

            //修正水库初始水位
            for (int i = 0; i < res_initial_atreach.Count; i++)
            {
                string res_name = res_initial_atreach.ElementAt(i).Key;
                double res_level = res_levels[res_name];
                if (res_name == "双泉水库" && res_level > 150) res_level = res_level - 77.2;
                if (res_name == "马鞍石水库" && res_level < 300) res_level = res_level + 263.2;
                List<AtReach> res_atreach = res_initial_atreach.ElementAt(i).Value;
                for (int j = 0; j < res_atreach.Count; j++)
                {
                    AtReach atreach = res_atreach[j];

                    //修正这些水库控制断面的 初始条件水位
                    if(initial_water.Keys.Contains(atreach)) initial_water[atreach] = res_level;
                }
            }
        }


        //修改河道控制断面的水位初始条件
        private static void Modify_ReachInitial(ref Dictionary<AtReach, double> initial_water, List<Water_Condition> now_watercondition)
        {
            //从数据库获取河道 控制断面 水位流量关系
            Dictionary<AtReach, List<double[]>> section_qhrelations = WG_INFO.Get_SectionQHrelation();

            //根据特定点水位推算河道流量(如果只有监测水位的话)
            Dictionary<AtReach, double> reach_mainq = WG_INFO.Get_MainStatin_Discharge(now_watercondition, section_qhrelations);

            //根据控制断面水位流量关系推算沿线控制断面水位(这些控制断面与初始条件对应，是需要修改的断面)
            Dictionary<AtReach, double> reach_level = WG_INFO.Insert_SectionLevel_FromQH(section_qhrelations, reach_mainq);

            //修改河道控制断面水位值
            for (int i = 0; i < initial_water.Count; i++)
            {
                AtReach atreach = initial_water.ElementAt(i).Key;
                if (Contain_ThisAtreach(reach_level, atreach))
                {
                   if(initial_water.Keys.Contains(atreach)) initial_water[atreach] = reach_level[atreach];
                }
            }
        }

        //包含河道断面位置
        public static bool Contain_ThisAtreach(Dictionary<AtReach, double> reach_level, AtReach atreach)
        {
            bool res = false;
            for (int i = 0; i < reach_level.Count; i++)
            {
                if(reach_level.ElementAt(i).Key.reachname == atreach.reachname && 
                    reach_level.ElementAt(i).Key.chainage == atreach.chainage)
                {
                    res = true;
                }
            }
            return res;
        }


        //更新数据库里惠农渠入渠流量 -- 第一个
        public static void Update_InputQ(int type,double value)
        {
            //从模型中获取模型信息
            string username = Mysql_GlobalVar.now_modeldb_user;
            string password = Mysql_GlobalVar.now_modeldb_password;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.WATER_CONDITION;
            

            //更新数据库入渠流量或水位
            string sql = "";
            if (type == 0)  //修改渠首水位边界
            {
                value -= 0.3;
                sql = "update " + tableName + " set level = '" + value  + "' where name = '龙门桥'" ;
            }
            else    //修改渠首流量边界
            {
                sql = "update " + tableName + " set discharge = '" + value + "' where name = '龙门桥'";
            }
            Mysql.Execute_Command(sql);

            

            Console.WriteLine("渠首边界条件信息存储完成！");
        }

        //内插补全河道初始水位
        private static Dictionary<string,Dictionary<AtReach,double>> Insert_ReachLevel(HydroModel hydromodel,List<Water_Condition> now_water)
        {
            Dictionary<string, Dictionary<AtReach, double>> reach_initial_level = new Dictionary<string, Dictionary<AtReach, double>>();
            List<ReachInfo> reachlist = hydromodel.Mike11Pars.ReachList.Reach_infolist;
            for (int i = 0; i < reachlist.Count; i++)
            {
                string reach = reachlist[i].reachname;
                AtReach start = AtReach.Get_Atreach(reach, reachlist[i].start_chainage);
                AtReach end = AtReach.Get_Atreach(reach, reachlist[i].end_chainage);

                //单条河道水位集合
                Dictionary<AtReach, double> reach_levels = new Dictionary<AtReach, double>();
                for (int j = 0; j < now_water.Count; j++)
                {
                    if (now_water[j].Reach == reach)
                    {
                        AtReach atreach = AtReach.Get_Atreach(reach, now_water[j].Chainage);
                        reach_levels.Add(atreach, now_water[j].Level);
                    }
                }

                //求河道起止点渠底高程
                ReachSection_Altitude start_section_altitude = hydromodel.Mike11Pars.SectionList.Get_Altitude(start);
                double start_qd = start_section_altitude.section_lowest;
                ReachSection_Altitude end_section_altitude = hydromodel.Mike11Pars.SectionList.Get_Altitude(end);
                double end_qd = end_section_altitude.section_lowest;

                //补全起止点水位
                if (!reach_levels.Keys.Contains(start))  //起点
                {
                    double level = 0;

                    string upconnect_reach = reachlist[i].upstream_connect.reachname;
                    if (upconnect_reach != null && reach_initial_level.Keys.Contains(upconnect_reach))
                    {
                        level = Insert_SectionLevel(reach_initial_level[upconnect_reach], reachlist[i].upstream_connect);
                    }
                    else if (reach_levels.Count != 0)
                    {
                        ReachSection_Altitude first_section_altitude = hydromodel.Mike11Pars.SectionList.Get_Altitude(reach_levels.First().Key);
                        double first_qd = first_section_altitude.section_lowest;

                        level = start_qd + Math.Max(reach_levels.First().Value - first_qd, 0.1);

                        //惠农区特列
                        if (reach == "HNQ") level = reach_levels.First().Value + 0.3;
                    }
                    reach_levels.Add(start, level);
                }
                reach_levels = reach_levels.OrderBy(p => p.Key.chainage).ToDictionary(p => p.Key, o => o.Value);

                if (!reach_levels.Keys.Contains(end))  //终点
                {
                    double level = 0;

                    string downconnect_reach = reachlist[i].downstream_connect.reachname;
                    if (downconnect_reach != null && reach_initial_level.Keys.Contains(downconnect_reach))
                    {
                        level = Insert_SectionLevel(reach_initial_level[downconnect_reach], reachlist[i].downstream_connect);
                    }
                    else if (reach_levels.Count != 0)
                    {
                        ReachSection_Altitude last_section_altitude = hydromodel.Mike11Pars.SectionList.Get_Altitude(reach_levels.Last().Key);
                        double last_qd = last_section_altitude.section_lowest;

                        level = end_qd + Math.Max(reach_levels.Last().Value - last_qd, 0.1);
                    }
                    reach_levels.Add(end, level);
                }

                //排序
                reach_levels = reach_levels.OrderBy(p => p.Key.chainage).ToDictionary(p => p.Key, o => o.Value);

                reach_initial_level.Add(reach, reach_levels);
            }

            return reach_initial_level;
        }

        //内插渠道水位
        private static double Insert_SectionLevel(Dictionary<AtReach, double> reach_level,AtReach atreach)
        {
            double depth = 0;
            if (reach_level == null) return depth;

            if (atreach.chainage <= reach_level.First().Key.chainage) return reach_level.First().Value;
            if (atreach.chainage >= reach_level.Last().Key.chainage) return reach_level.Last().Value;

            for (int i = 0; i < reach_level.Count - 1; i++)
            {
                double start_chainage = reach_level.ElementAt(i).Key.chainage;
                double end_chainage = reach_level.ElementAt(i+1).Key.chainage;

                double start_h = reach_level.ElementAt(i).Value;
                double end_h = reach_level.ElementAt(i + 1).Value;

                if (atreach.chainage >= start_chainage && atreach.chainage <= end_chainage)
                {
                    depth = start_h + (end_h - start_h) * (atreach.chainage - start_chainage) / (end_chainage - start_chainage);
                    return depth;
                }
            }
            return depth;
        }

        //从数据库中读取最新实时水位数据
        public static List<Water_Condition> Read_NowWater(string model_instance = "")
        {
            List<Water_Condition> now_water = new List<Water_Condition>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.WATER_CONDITION;

            //先判断数据库中是否存在
            string sql1 = "select * from " + tableName + " order by id";
            string sql2 = "select * from " + tableName + " where model_instance like '%" + model_instance + "%'" + " order by id";
            
            string sqlstr = model_instance == ""? sql1:sql2;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string name = dt.Rows[i][1].ToString();
                string stcd = dt.Rows[i][2].ToString();
                string reach = dt.Rows[i][3].ToString();
                string reach_cnname = dt.Rows[i][4].ToString();
                double chainage = double.Parse(dt.Rows[i][5].ToString());
                double level = double.Parse(dt.Rows[i][6].ToString());
                double discharge = double.Parse(dt.Rows[i][7].ToString());
                string date = dt.Rows[i][8].ToString();
                string datesource = dt.Rows[i][9].ToString();
                string station_type = dt.Rows[i][10].ToString();
                string level1 = dt.Rows[i][11].ToString();
                string level2 = dt.Rows[i][12].ToString();
                string level3 = dt.Rows[i][13].ToString();
                string more_info = dt.Rows[i][14].ToString();
                string area_str = dt.Rows[i][15].ToString();
                double control_area = (area_str == null || area_str == "")? 0: double.Parse(dt.Rows[i][15].ToString());
                List<string> model_instances =  dt.Rows[i][16].ToString().Split(',').ToList();

                Water_Condition water_condition = Water_Condition.Get_NowWater(name,reach,reach_cnname,
                    chainage,level,discharge,date, stcd, datesource, station_type, level1, level2,level3,more_info, control_area, model_instances);
                now_water.Add(water_condition);
            }

            
            return now_water;
        }

        //获取各水库汛限水位
        public static Dictionary<string, double> Read_Res_XXLevels(string model_instance = "", string stcd = "")
        {
            Dictionary<string, double> res_levels = new Dictionary<string, double>();

            //定义连接的数据库和表
            string tableName1 = Mysql_GlobalVar.WATER_CONDITION;

            //先判断数据库中是否存在
            string sql1 = $"select name,level_1 from {tableName1} where data_source = '水库水文站'";
            string sql2 = sql1 + " and model_instance like '%" + model_instance + "%'";

            string sqlstr = model_instance == "" ? sql1 : sql2;
            if (stcd != "") sqlstr = sqlstr + $" and stcd = '{stcd}'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string name = dt.Rows[i][0].ToString();

                double xx_level = double.Parse(dt.Rows[i][1].ToString());
                res_levels.Add(name, xx_level);
            }

            return res_levels;
        }

        //从数据库中通过水情断面stcd获取水情断面桩号
        public static AtReach Read_Sectioninfo_FromSTCD(string stcd)
        {
            List<Water_Condition> now_water = new List<Water_Condition>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.WATER_CONDITION;
            

            //先判断数据库中是否存在
            string sqlstr = "select reach,chainage from " + tableName + " where stcd = '" + stcd + "'"; 
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return AtReach.Get_Atreach("", 0);

            //遍历记录
            string reach = dt.Rows[0][0].ToString();
            double chainage = double.Parse(dt.Rows[0][1].ToString());
            AtReach atreach = AtReach.Get_Atreach(reach, chainage);

            
            return atreach;
        }

        // 修改初始水位
        public static void Modify_InitialWaterLevel(ref HydroModel hydromodel, AtReach newatreach, double newlevel)
        {
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;

            for (int i = 0; i < hd11pars.InitialWater.Count; i++)
            {
                AtReach atreach = hd11pars.InitialWater.Keys.ElementAt(i);
                if (newatreach.Equals(atreach)) hd11pars.InitialWater[atreach] = newlevel;
            }
        }

        // 重新设置某河的糙率，只能整条设置，原来有就改值，没有就重新赋值
        public static void Modify_BedResistance(ref HydroModel hydromodel, string reachname, double resistance)
        {
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //删除该河道原来的糙率
            int number = hd11pars.Bed_Resist.Count;
            for (int i = 0; i < number; i++)
            {
                Reach_Segment reach_segment = hd11pars.Bed_Resist.Keys.ElementAt(i);
                if (reach_segment.reachname == reachname)
                {
                    hd11pars.Bed_Resist.Remove(reach_segment);
                    number--;
                    i--;
                }
            }

            //获取河道起止点桩号信息
            Reach_Segment reachseg;
            reachseg.reachname = reachname;
            reachseg.reachtopoid = reachseg.reachname + Model_Const.REACHID_HZ;
            ReachInfo reachinfo = reachlist.Get_Reachinfo(reachname);
            reachseg.start_chainage = reachinfo.start_chainage;
            reachseg.end_chainage = reachinfo.end_chainage;

            //重新添加
            hd11pars.Bed_Resist.Add(reachseg, resistance);
        }

        // 添加河道的初始水位
        public static void Add_InitialWaterLevel(ref HydroModel hydromodel, AtReach atreach, double initialwaterlevel)
        {
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;

            //原来完全相同的初始水位删除
            for (int i = 0; i < hd11pars.InitialWater.Count; i++)
            {
                AtReach nowatreach = hd11pars.InitialWater.Keys.ElementAt(i);
                if (nowatreach.Equals(atreach)) hd11pars.InitialWater.Remove(atreach);
            }

            //添加新的初始水位
            hd11pars.InitialWater.Add(atreach, initialwaterlevel);
        }

        // 修改时间序列结果txt输出路径
        public static void Modify_OutputTxtPath(ref HydroModel hydromodel, string newoutput_filename)
        {
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;

            hd11pars.TxtFilename = newoutput_filename;
        }

        // 添加河道点的时间序列结果输出
        public static void Add_OutputGridPoint(ref HydroModel hydromodel, AtReach reachGrid)
        {
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;

            Reach_Segment reach_segment;
            reach_segment.reachname = reachGrid.reachname;
            reach_segment.reachtopoid = reachGrid.reachid;
            reach_segment.start_chainage = reachGrid.chainage;
            reach_segment.end_chainage = reachGrid.chainage;

            if (!hd11pars.Reach_OutputGrid.Contains(reach_segment))
            {
                hd11pars.Reach_OutputGrid.Add(reach_segment);
            }
        }

        // 添加河道点的时间序列结果输出
        public static void Add_OutputGridPoint(ref HydroModel hydromodel, Reach_Segment reachGrid)
        {
            Hd11_ParametersList hd11pars = hydromodel.Mike11Pars.Hd_Pars;

            if (!hd11pars.Reach_OutputGrid.Contains(reachGrid))
            {
                hd11pars.Reach_OutputGrid.Add(reachGrid);
            }
        }

        #endregion ******************************************************************************************

    }

    //保存最新的hd11参数
    [Serializable]
    public class Hd11_ParametersList
    {
        //**********************属性************************
        //全局糙率值
        public double Global_Resist
        { get; set; }

        //水库和河道的初始水位集合
        public Dictionary<AtReach, double> InitialWater
        { get; set; }

        //水库和河道的糙率键值对集合
        public Dictionary<Reach_Segment, double> Bed_Resist
        { get; set; }

        //结果时间序列输出txt文件路径
        public string TxtFilename
        { get; set; }

        //河道结果输出点和河段
        public List<Reach_Segment> Reach_OutputGrid
        { get; set; }

        public Hd11_ParametersList(double global_Resist, Dictionary<AtReach, double> initialWater, 
            Dictionary<Reach_Segment, double> bed_Resist, string txtFilename, List<Reach_Segment> reach_OutputGrid)
        {
            Global_Resist = global_Resist;
            InitialWater = initialWater;
            Bed_Resist = bed_Resist;
            TxtFilename = txtFilename;
            Reach_OutputGrid = reach_OutputGrid;
        }

        public Hd11_ParametersList()
        {
            Global_Resist = 0.025;
            InitialWater = new Dictionary<AtReach, double>();
            Bed_Resist = new Dictionary<Reach_Segment, double>();
            TxtFilename = "";
            Reach_OutputGrid = new List<Reach_Segment>();
        }

        //**********************方法************************

    }

}