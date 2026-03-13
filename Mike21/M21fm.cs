using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS.mesh;
using DHI.Generic.MikeZero.DFS.dfsu;
using System.IO;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model;

namespace bjd_model.Mike21
{
    //堤防的操作枚举
    [Serializable]
    public enum Dike_Operate
    {
        normal,       //正常挡水
        disable,     //禁用堤防，堤防不再挡水
        full         //加高堤防完全挡水，使得水永远漫不过堤防
    }

    //堰的操作枚举
    [Serializable]
    public enum Weir_Operate
    {
        enable,      //正常启用
        disable      //禁用，封闭涵洞
    }

    //干湿边界
    [Serializable]
    public struct Dry_Wet
    {
        public double dry;
        public double flood;
        public double wet;

        public static Dry_Wet Get_Drywet(double dry, double flood, double wet)
        {
            Dry_Wet drywet;
            drywet.dry = dry;
            drywet.flood = flood;
            drywet.wet = wet;
            return drywet;
        }
    }

    //m21fm糙率结构体
    [Serializable]
    public struct Bed_Resistance
    {
        public int format;                      //0为常量，2为随空间变化
        public double constant_value;           //高程糙率值
        public string fileName;                  //随空间变化的量 --dfsu，项目类型为Manning's M
        public string itemName;                  //选用文件中的item名

        public static Bed_Resistance Get_Default_Resistance()
        {
            Bed_Resistance resistance;
            resistance.format = 0;
            resistance.constant_value = 32;
            resistance.fileName = "";
            resistance.itemName = "";
            return resistance;
        }
    }

    // 选择是否考虑面上的降雨蒸发
    [Serializable]
    public enum Type_Pre_Eva
    {
        NoPreEva = 0,  //不考虑降雨蒸发
        Pre_Eva = 1  //考虑降雨蒸发
    }

    // 降雨蒸发的形式
    [Serializable]
    public enum Format_Pre_Eva
    {
        constant = 0,  //降雨、蒸发为常量
        varyInTime = 1  //降雨、蒸发随时间改变，不随空间改变
    }

    //m21fm降雨蒸发数据结构体
    [Serializable]
    public struct Pre_Eva
    {
        public Type_Pre_Eva type_pre_eva;        //是否考虑降雨蒸发
        public Format_Pre_Eva format_pre_eva;    //常量还是随时间变化的序列
        public double constantEvaporation;       //常量值
        public string fileName;                  //随时间变化的时间序列
        public string itemName;                  //选用文件中的item名
    }

    //m21fm初始条件结构体
    [Serializable]
    public struct Initial_Contition
    {
        public int initial_type;                //1为常量，2为随空间变化的水深，3为随空间变化的水深和流速
        public double const_surface_elevation;  //表面高程常量
        public string fileName;                  //随空间变化的量 --dfsu，项目类型为surface elevation
        public string itemName;                  //选用文件中的item名
    }

    //所有设计m21fm文件的参数、堤防和涵洞操作的类
    public class M21fm
    {
        #region ************************从默认m21fm文件中获取Dike、Weir信息********************************
        //m21fm 相关参数初始化--从默认M21FM文件中获取
        public static void GetDefault_M21fmParameters(ref HydroModel hydromodel)
        {
            //获取模型各相关参数
            string sourcefilename = hydromodel.Modelfiles.M21fm_filename;
            Mike21_Pars Mike21pars = hydromodel.Mike21Pars;
            DikeList Dikelist = hydromodel.Mike21Pars.DikeList;
            WeirList Weirlist = hydromodel.Mike21Pars.WeirList;
            Model_GlobalPars GlobalPars = hydromodel.ModelGlobalPars;

            //找到相应的节
            PFSFile pfsfile = new PFSFile(sourcefilename);   //读取文件
            PFSSection FemEngineHD = pfsfile.GetTarget("FemEngineHD", 1);   //最外面的节
            PFSSection DOMAIN = FemEngineHD.GetSection("DOMAIN", 1);
            PFSSection HYDRODYNAMIC_MODULE = FemEngineHD.GetSection("HYDRODYNAMIC_MODULE", 1);

            //干湿边界
            PFSSection FLOOD_AND_DRY = HYDRODYNAMIC_MODULE.GetSection("FLOOD_AND_DRY", 1);
            FLOOD_AND_DRY.GetKeyword("drying_depth", 1).GetParameter(1).ToDouble();
            Dry_Wet drywet;
            drywet.dry = FLOOD_AND_DRY.GetKeyword("drying_depth", 1).GetParameter(1).ToDouble();
            drywet.flood = FLOOD_AND_DRY.GetKeyword("flooding_depth", 1).GetParameter(1).ToDouble();
            drywet.wet = FLOOD_AND_DRY.GetKeyword("mass_depth", 1).GetParameter(1).ToDouble();
            Mike21pars.Dry_wet = drywet;

            //糙率
            Bed_Resistance resistance;
            PFSSection BED_RESISTANCE = HYDRODYNAMIC_MODULE.GetSection("BED_RESISTANCE", 1);  //3层节
            PFSSection MANNING_NUMBER = BED_RESISTANCE.GetSection("MANNING_NUMBER", 1);  //4层节
            PFSKeyword format = MANNING_NUMBER.GetKeyword("format", 1);
            resistance.format = format.GetParameter(1).ToInt();
            PFSKeyword constant_value = MANNING_NUMBER.GetKeyword("constant_value", 1);
            resistance.constant_value = constant_value.GetParameter(1).ToDouble();
            PFSKeyword resistance_file_name = MANNING_NUMBER.GetKeyword("file_name", 1);
            resistance.fileName = resistance_file_name.GetParameter(1).ToFileName();  //糙率文件路径
            PFSKeyword item_name = MANNING_NUMBER.GetKeyword("item_name", 1);
            resistance.itemName = item_name.GetParameter(1).ToString();
            Mike21pars.Resistance_set = resistance;

            //降雨
            Pre_Eva precipitation;
            precipitation.type_pre_eva = Type_Pre_Eva.NoPreEva;
            precipitation.constantEvaporation = 0;
            precipitation.format_pre_eva = Format_Pre_Eva.constant;
            precipitation.fileName = "";
            precipitation.itemName = "";

            //蒸发
            Pre_Eva evaporation;
            evaporation.type_pre_eva = Type_Pre_Eva.NoPreEva;
            evaporation.constantEvaporation = 0;
            evaporation.format_pre_eva = Format_Pre_Eva.constant;
            evaporation.fileName = "";
            evaporation.itemName = "";
            Mike21pars.Pre_set = precipitation;
            Mike21pars.Eva_set = evaporation;

            //初始条件
            Initial_Contition initial;
            PFSSection INITIAL_CONDITIONS = HYDRODYNAMIC_MODULE.GetSection("INITIAL_CONDITIONS", 1);
            initial.initial_type = INITIAL_CONDITIONS.GetKeyword("type", 1).GetParameter(1).ToInt();
            initial.const_surface_elevation = INITIAL_CONDITIONS.GetKeyword("surface_elevation_constant", 1).GetParameter(1).ToDouble();

            initial.fileName = "";
            initial.itemName = "";
            try
            {
                initial.fileName = INITIAL_CONDITIONS.GetKeyword("file_name_2D", 1).GetParameter(1).ToString();
                initial.itemName = INITIAL_CONDITIONS.GetKeyword("item_name_for_surface_elevation", 1).GetParameter(1).ToString();
            }
            catch (Exception)
            {
            }

            Mike21pars.Initial_set = initial;

            //坐标投影
            PFSKeyword coordinate_type = DOMAIN.GetKeyword("coordinate_type", 1);
            GlobalPars.Coordinate_type = coordinate_type.GetParameter(1).ToString();

            //dike和weir初始化
            GetDefault_Dike_Weirlist(HYDRODYNAMIC_MODULE, ref hydromodel);

            pfsfile.Close();
        }

        //dike和weir初始化
        public static void GetDefault_Dike_Weirlist(PFSSection HYDRODYNAMIC_MODULE, ref HydroModel hydromodel)
        {
            DikeList Dikelist = hydromodel.Mike21Pars.DikeList;
            WeirList Weirlist = hydromodel.Mike21Pars.WeirList;

            //初始化集合
            List<Dike> dikelist = new List<Dike>();
            List<Weir> weirlist = new List<Weir>();

            Dikelist.MaxDikeId = 0;
            Weirlist.MaxWeirId = 0;

            //找到相应的节
            PFSSection STRUCTURE_MODULE = HYDRODYNAMIC_MODULE.GetSection("STRUCTURE_MODULE", 1);
            PFSSection WEIR = STRUCTURE_MODULE.GetSection("WEIR", 1);
            PFSSection DIKES = HYDRODYNAMIC_MODULE.GetSection("STRUCTURES", 1).GetSection("DIKES", 1);

            int weircount = WEIR.GetSectionsCount();
            for (int i = 0; i < weircount; i++)
            {
                //堰的名称、编号、过流水深阀值
                PFSSection weirsec = WEIR.GetSection("weir_data", i + 1);
                Weir weir = new Weir(i + 1, weirsec.GetKeyword("Location").GetParameter(3).ToString());
                weir.Operate = Weir_Operate.enable;
                weir.Delhs = weirsec.GetKeyword("delhs").GetParameter(1).ToDouble();

                //堰的控制点集合
                List<PointXY> pointlist = new List<PointXY>();
                int pointcount = weirsec.GetKeyword("number_of_points").GetParameter(1).ToInt();
                PFSKeyword keyx = weirsec.GetKeyword("x");
                PFSKeyword keyy = weirsec.GetKeyword("y");
                for (int j = 0; j < pointcount; j++)
                {
                    PointXY point;
                    point.X = keyx.GetParameter(j + 1).ToDouble();
                    point.Y = keyy.GetParameter(j + 1).ToDouble();
                    pointlist.Add(point);
                }
                weir.Pointlist = pointlist;

                //堰的类型、几何形状
                Dictionary<double, double> level_width = new Dictionary<double, double>();
                PFSSection Geometry = weirsec.GetSection("Geometry", 1);
                weir.Type = Geometry.GetKeyword("Attributes").GetParameter(1).ToInt();
                weir.Datum = Geometry.GetKeyword("Attributes").GetParameter(2).ToDouble();
                PFSSection Level_Width = Geometry.GetSection("Level_Width", 1);
                for (int j = 0; j < Level_Width.GetKeywordsCount(); j++)
                {
                    PFSKeyword LW_Data = Level_Width.GetKeyword(j + 1);
                    level_width.Add(LW_Data.GetParameter(1).ToDouble(), LW_Data.GetParameter(2).ToDouble());
                }
                weir.Level_Width = level_width;

                //将获取到的堰加入集合
                weirlist.Add(weir);
            }

            int dikecount = DIKES.GetSectionsCount();
            for (int i = 0; i < dikecount; i++)
            {
                //Dike的名称、类型、编号、过流水深阀值
                PFSSection dikesec = DIKES.GetSection(i + 1);
                Dike dike = new Dike(hydromodel.Modelfiles.Mesh_filename, i + 1, dikesec.GetKeyword("name").GetParameter(1).ToString());
                dike.Operate = Dike_Operate.normal;
                if (dikesec.GetKeyword("include").GetParameter(1).ToInt() != 1)
                {
                    dike.Operate = Dike_Operate.disable;
                }
                dike.Type = dikesec.GetKeyword("type").GetParameter(1).ToInt();
                dike.Delhs = dikesec.GetKeyword("critical_level_difference").GetParameter(1).ToDouble();

                //Dike的控制点集合
                List<PointXYZ> pointlist = new List<PointXYZ>();
                int pointcount = dikesec.GetKeyword("number_of_points").GetParameter(1).ToInt();
                for (int j = 0; j < pointcount; j++)
                {

                    PFSKeyword keyx = dikesec.GetSection(j + 1).GetKeyword("x");
                    PFSKeyword keyy = dikesec.GetSection(j + 1).GetKeyword("y");
                    PFSKeyword keyz = dikesec.GetSection(j + 1).GetKeyword("z");

                    PointXYZ point;
                    point.X = keyx.GetParameter(1).ToDouble();
                    point.Y = keyy.GetParameter(1).ToDouble();
                    point.Z = keyz.GetParameter(1).ToDouble();
                    pointlist.Add(point);
                }
                dike.Pointlist = pointlist;

                //将获取到的Dike加入集合
                dikelist.Add(dike);
            }

            //初始化dike和weir集合
            Dikelist.Dike_List = dikelist;
            Dikelist.MaxDikeId = dikelist.Count;

            Weirlist.Weir_List = weirlist;
            Weirlist.MaxWeirId = weirlist.Count;
            Console.WriteLine("dike和weir初始化成功!");
        }
        #endregion ****************************************************************************************


        #region ***************************** 更新模拟文件********************************
        // 提取最新的mesh文件、糙率文件、结果文件、模拟时间、步长、保存频率、降雨蒸发设置等参数，更新M21fm文件
        public static void Rewrite_M21fm_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.M21fm_filename;
            string outputfilename = hydromodel.Modelfiles.M21fm_filename;
            Mike21_Pars mike21pars = hydromodel.Mike21Pars;
            if (hydromodel.ModelGlobalPars.Select_model != CalculateModel.ad_hd_m21 &&
                hydromodel.ModelGlobalPars.Select_model != CalculateModel.only_m21 &&
                hydromodel.ModelGlobalPars.Select_model != CalculateModel.rr_hd_flood ) return;

            Model_GlobalPars globalpars = hydromodel.ModelGlobalPars;

            //打开PFS文件，开始编辑
            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection FemEngineHD = pfsfile.GetTarget("FemEngineHD", 1);   //目标

            //更新计算区域节
            Update_DomainSec(hydromodel, ref FemEngineHD);

            //更新模拟时间节
            Update_TimeSec(globalpars, ref FemEngineHD);

            //得到水动力学模拟节
            PFSSection HYDRODYNAMIC_MODULE = FemEngineHD.GetSection("HYDRODYNAMIC_MODULE", 1);

            //更新可变时间步长节--只改最大值
            Update_SolutionSec(globalpars, ref HYDRODYNAMIC_MODULE);

            //更新干湿边界
            Update_DryWetSec(mike21pars, ref HYDRODYNAMIC_MODULE);

            //更新糙率文件路径
            Update_BedsistanceSec(hydromodel, ref HYDRODYNAMIC_MODULE);

            //更新降雨蒸发节的设置
            Update_PreEvaSec(mike21pars, ref HYDRODYNAMIC_MODULE);

            //更新weir节的设置
            Update_Weir(hydromodel, ref HYDRODYNAMIC_MODULE);

            //更新dike节的设置
            Update_Dike(hydromodel, ref HYDRODYNAMIC_MODULE);

            //更新初始条件节的设置
            Update_InitialSec(mike21pars, ref HYDRODYNAMIC_MODULE);

            //更新输出节的设置,包含输出文件路径、坐标系、范围、计算步数、输出频率
            Update_OutputSec(hydromodel, ref HYDRODYNAMIC_MODULE);

            //重新生成m21fm文件
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("M21fm二维模拟文件更新成功!");
            Console.WriteLine("");
        }

        //更新计算区域节设置
        public static void Update_DomainSec(HydroModel hydromodel, ref PFSSection FemEngineHD)
        {
            string mesh_filename = hydromodel.Modelfiles.Mesh_filename;

            //更新mesh文件路径
            PFSSection DOMAIN = FemEngineHD.GetSection("DOMAIN", 1);
            PFSKeyword file_name = DOMAIN.GetKeyword("file_name", 1);
            file_name.GetParameter(1).ModifyFileNameParameter(mesh_filename);  //输入mesh文件路径

            //更新Mesh文件的坐标系
            MeshFile meshfile = new MeshFile();  //mesh文件
            meshfile.Read(mesh_filename);
            string Projection = hydromodel.ModelGlobalPars.Coordinate_type;
            PFSKeyword coordinate_type = DOMAIN.GetKeyword("coordinate_type", 1);
            coordinate_type.GetParameter(1).ModifyStringParameter(Projection);

            //更新最大Z值
            double max_z = meshfile.Z.Max();
            PFSKeyword minimum_depth = DOMAIN.GetKeyword("minimum_depth", 1);
            minimum_depth.DeleteParameter(1);
            minimum_depth.InsertNewParameterDouble(max_z, 1);

            Console.WriteLine("Domain节更新成功!");
        }

        //更新模拟时间节设置
        public static void Update_TimeSec(Model_GlobalPars globalpars, ref PFSSection FemEngineHD)
        {
            SimulateTime begin_endDateTime = globalpars.Simulate_time;
            SimTimeStep simulate_timestep = globalpars.Simulate_timestep;

            //计算总步数
            TimeSpan delta_time = begin_endDateTime.End.Subtract(begin_endDateTime.Begin);  //起始时间和结束时间的间隔            
            double delta_timeToSecond = delta_time.TotalSeconds;  //将间隔时间换算成秒
            int simulate_steps = (int)Math.Ceiling(delta_timeToSecond / (int)simulate_timestep); //计算时间步数，除不尽时入1

            PFSSection TIME = FemEngineHD.GetSection("TIME", 1);
            PFSKeyword start_time = TIME.GetKeyword("start_time", 1);  //计算的起始时间
            object[] start_time_array ={ 
                                         begin_endDateTime.Begin.Year,begin_endDateTime.Begin.Month,begin_endDateTime.Begin.Day,
                                         begin_endDateTime.Begin.Hour,begin_endDateTime.Begin.Minute,begin_endDateTime.Begin.Second
                                       };
            Nwk11.InsertKeyPars(ref start_time, start_time_array);
            PFSKeyword time_step_interval = TIME.GetKeyword("time_step_interval", 1);
            time_step_interval.GetParameter(1).ModifyIntParameter((int)simulate_timestep);
            PFSKeyword number_of_time_steps = TIME.GetKeyword("number_of_time_steps", 1);
            number_of_time_steps.GetParameter(1).ModifyIntParameter(simulate_steps);

            Console.WriteLine("Time节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 可变时间步长节的设置
        public static void Update_SolutionSec(Model_GlobalPars globalpars, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            double timeStepLongth = (double)globalpars.Simulate_timestep;  //以秒计的计算时间步长
            double maximumTime = timeStepLongth / 2;          //可变时间步长最大值设置为计算步长的一半
            PFSSection SOLUTION_TECHNIQUE = HYDRODYNAMIC_MODULE.GetSection("SOLUTION_TECHNIQUE", 1);
            PFSKeyword dt_max_HD = SOLUTION_TECHNIQUE.GetKeyword("dt_max_HD", 1);
            PFSKeyword dt_max_AD = SOLUTION_TECHNIQUE.GetKeyword("dt_max_AD", 1);
            dt_max_HD.DeleteParameter(1);
            dt_max_HD.InsertNewParameterDouble(maximumTime, 1);

            dt_max_AD.DeleteParameter(1);
            dt_max_AD.InsertNewParameterDouble(maximumTime, 1);

            Console.WriteLine("Hydrodynamic节 Solution节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 干湿边界节的设置
        public static void Update_DryWetSec(Mike21_Pars mike21pars, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            PFSSection FLOOD_AND_DRY = HYDRODYNAMIC_MODULE.GetSection("FLOOD_AND_DRY", 1);
            PFSKeyword drying_depth = FLOOD_AND_DRY.GetKeyword("drying_depth", 1);  //设置干边界
            drying_depth.DeleteParameter(1);
            drying_depth.InsertNewParameterDouble(mike21pars.Dry_wet.dry, 1);
            PFSKeyword flooding_depth = FLOOD_AND_DRY.GetKeyword("flooding_depth", 1);
            flooding_depth.DeleteParameter(1);
            flooding_depth.InsertNewParameterDouble(mike21pars.Dry_wet.flood, 1);
            PFSKeyword mass_depth = FLOOD_AND_DRY.GetKeyword("mass_depth", 1);  //湿边界
            mass_depth.DeleteParameter(1);
            mass_depth.InsertNewParameterDouble(mike21pars.Dry_wet.wet, 1);

            Console.WriteLine("Hydrodynamic节 Flood and Dry节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 糙率文件节的设置
        public static void Update_BedsistanceSec(HydroModel hydromodel, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            string meshfilename = hydromodel.BaseModel.Modelfiles.Mesh_filename;
            string basesourcefilename = hydromodel.BaseModel.Modelfiles.M21caolv_filename;
            string outputfilename = hydromodel.Modelfiles.M21caolv_filename;
            Mike21_Pars mike21pars = hydromodel.Mike21Pars;

            //判断现在和基础模型的网格是否一致
            if (basesourcefilename != outputfilename)
            {
                if (MeshPars.Get_NodeCount(meshfilename) == hydromodel.Mike21Pars.MeshParsList.Nodecount)
                {
                    if (File.Exists(outputfilename))
                    {
                        File.Delete(outputfilename);
                    }
                    File.Copy(basesourcefilename, outputfilename);
                }
            }

            Bed_Resistance now_resistance = mike21pars.Resistance_set;
            now_resistance.fileName = outputfilename;
            mike21pars.Resistance_set = now_resistance;

            Bed_Resistance resistance = hydromodel.Mike21Pars.Resistance_set;
            PFSSection BED_RESISTANCE = HYDRODYNAMIC_MODULE.GetSection("BED_RESISTANCE", 1);  //3层节
            PFSSection MANNING_NUMBER = BED_RESISTANCE.GetSection("MANNING_NUMBER", 1);  //4层节

            PFSKeyword format = MANNING_NUMBER.GetKeyword("format", 1);
            format.GetParameter(1).ModifyIntParameter(resistance.format);

            PFSKeyword constant_value = MANNING_NUMBER.GetKeyword("constant_value", 1);
            constant_value.DeleteParameter(1);
            constant_value.InsertNewParameterDouble(resistance.constant_value, 1);

            PFSKeyword resistance_file_name = MANNING_NUMBER.GetKeyword("file_name", 1);
            resistance_file_name.GetParameter(1).ModifyFileNameParameter(resistance.fileName);  //输入糙率文件
            PFSKeyword item_name = MANNING_NUMBER.GetKeyword("item_name", 1);
            item_name.GetParameter(1).ModifyStringParameter(resistance.itemName);

            Console.WriteLine("Hydrodynamic节 Bed sistance节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 降雨蒸发节的设置
        public static void Update_PreEvaSec(Mike21_Pars mike21pars, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            Pre_Eva precipitation = mike21pars.Pre_set;
            Pre_Eva evaporation = mike21pars.Eva_set;
            PFSSection PRECIPITATION_EVAPORATION = HYDRODYNAMIC_MODULE.GetSection("PRECIPITATION_EVAPORATION", 1);  //3层节

            PFSKeyword type_of_precipitation = PRECIPITATION_EVAPORATION.GetKeyword("type_of_precipitation", 1);
            type_of_precipitation.GetParameter(1).ModifyIntParameter(0);  //设置是否需要降雨

            PFSKeyword type_of_evaporation = PRECIPITATION_EVAPORATION.GetKeyword("type_of_evaporation", 1);
            type_of_evaporation.GetParameter(1).ModifyIntParameter(0);  //设置是否需要蒸发

            if ((int)precipitation.type_pre_eva == 1)  //如果考虑降雨
            {
                type_of_precipitation.GetParameter(1).ModifyIntParameter(1);  //设置是否需要降雨

                PFSSection PRECIPITATION = PRECIPITATION_EVAPORATION.GetSection("PRECIPITATION", 1);
                PFSKeyword format = PRECIPITATION.GetKeyword("format", 1);
                format.GetParameter(1).ModifyIntParameter((int)precipitation.format_pre_eva);
                if ((int)precipitation.format_pre_eva == 0)  //降雨为常量
                {
                    PFSKeyword constant_value = PRECIPITATION.GetKeyword("constant_value", 1);
                    constant_value.DeleteParameter(1);
                    constant_value.InsertNewParameterDouble(precipitation.constantEvaporation, 1);  //设置降雨常量
                }
                else if ((int)precipitation.format_pre_eva == 1)  //降雨随时间变化
                {
                    PFSKeyword eva_file_name = PRECIPITATION.GetKeyword("file_name", 1);
                    eva_file_name.GetParameter(1).ModifyFileNameParameter(precipitation.fileName);  //输入降雨时间序列文件
                    PFSKeyword item_number = PRECIPITATION.GetKeyword("item_number", 1);
                    item_number.GetParameter(1).ModifyIntParameter(1);     //降雨文件所选用的item序号，默认为1
                    PFSKeyword eva_item_name = PRECIPITATION.GetKeyword("item_name", 1);
                    eva_item_name.GetParameter(1).ModifyStringParameter(precipitation.itemName);
                }
            }

            if ((int)evaporation.type_pre_eva == 1)  //如果考虑蒸发
            {
                type_of_evaporation.GetParameter(1).ModifyIntParameter(1);  //设置是否需要蒸发

                PFSSection EVAPORATION = PRECIPITATION_EVAPORATION.GetSection("EVAPORATION", 1);
                PFSKeyword format = EVAPORATION.GetKeyword("format", 1);
                format.GetParameter(1).ModifyIntParameter((int)evaporation.format_pre_eva);
                if ((int)evaporation.format_pre_eva == 0)  //蒸发为常量
                {
                    PFSKeyword constant_value = EVAPORATION.GetKeyword("constant_value", 1);
                    constant_value.DeleteParameter(1);
                    constant_value.InsertNewParameterDouble(evaporation.constantEvaporation, 1);  //设置蒸发常量
                }
                else if ((int)evaporation.format_pre_eva == 1)  //蒸发随时间变化
                {
                    PFSKeyword eva_file_name = EVAPORATION.GetKeyword("file_name", 1);
                    eva_file_name.GetParameter(1).ModifyFileNameParameter(evaporation.fileName);  //输入时间蒸发序列文件
                    PFSKeyword item_number = EVAPORATION.GetKeyword("item_number", 1);
                    item_number.GetParameter(1).ModifyIntParameter(1);     //蒸发文件所选用的item序号，默认为1
                    PFSKeyword eva_item_name = EVAPORATION.GetKeyword("item_name", 1);
                    eva_item_name.GetParameter(1).ModifyStringParameter(evaporation.itemName);
                }
            }

            Console.WriteLine("Hydrodynamic节 Pre - Eva节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 建筑物节 weir的设置,重新插入所有的最新的weir
        public static void Update_Weir(HydroModel hydromodel, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            PFSSection STRUCTURES = HYDRODYNAMIC_MODULE.GetSection("STRUCTURE_MODULE", 1);  //3层节

            STRUCTURES.DeleteSection("WEIR", 1);  //删除的节也是单独排
            PFSSection WEIR = STRUCTURES.InsertNewSection("WEIR", 2);  //新加堰的节

            //重新逐个添加堰
            if (hydromodel.Mike21Pars.WeirList.Weir_List != null)
            {
                List<Weir> weirlist = hydromodel.Mike21Pars.WeirList.Weir_List;
                int weircount = weirlist.Count;
                for (int i = 0; i < weircount; i++)
                {
                    //逐个添加最新的weir和其设置
                    PFSSection weir_data = WEIR.InsertNewSection("weir_data", i + 1);  //新加堰的节

                    //插入weir节
                    Insert_WeirSec(hydromodel, ref weir_data, weirlist[i]);
                }
            }
            Console.WriteLine("Hydrodynamic节 Weir节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 建筑物节 dike的设置,重新插入所有的最新的dike
        public static void Update_Dike(HydroModel hydromodel, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            PFSSection STRUCTURES = HYDRODYNAMIC_MODULE.GetSection("STRUCTURES", 1);  //3层节

            STRUCTURES.DeleteSection("DIKES", 1);
            PFSSection DIKES = STRUCTURES.InsertNewSection("DIKES", 4);  //新加DIke的节

            //插入前面的参数
            if (hydromodel.Mike21Pars.DikeList.Dike_List != null)
            {
                List<Dike> dikelist = hydromodel.Mike21Pars.DikeList.Dike_List;
                int dikecount = dikelist.Count;
                DIKES.InsertNewKeyword("Touched", 1).InsertNewParameterInt(1, 1);
                DIKES.InsertNewKeyword("MzSEPfsListItemCount", 1).InsertNewParameterInt(dikecount, 1);
                DIKES.InsertNewKeyword("output_of_link_data", 1).InsertNewParameterInt(0, 1);
                DIKES.InsertNewKeyword("file_name_section", 1).InsertNewParameterString("line_section.xyz", 1);
                DIKES.InsertNewKeyword("number_of_dikes", 1).InsertNewParameterInt(dikecount, 1);

                for (int i = 0; i < dikecount; i++)
                {
                    //逐个添加最新的dike和其设置
                    PFSSection DIKE_I = DIKES.InsertNewSection("DIKE_" + (i + 1).ToString(), i + 1);  //新加堤防的节

                    //插入dike节
                    Insert_DikeSec(hydromodel, ref DIKE_I, dikelist[i]);
                }
            }
            else
            {
                DIKES.InsertNewKeyword("Touched", 1).InsertNewParameterInt(1, 1);
                DIKES.InsertNewKeyword("MzSEPfsListItemCount", 1).InsertNewParameterInt(0, 1);
                DIKES.InsertNewKeyword("output_of_link_data", 1).InsertNewParameterInt(0, 1);
                DIKES.InsertNewKeyword("file_name_section", 1).InsertNewParameterString("line_section.xyz", 1);
                DIKES.InsertNewKeyword("number_of_dikes", 1).InsertNewParameterInt(0, 1);
            }

            Console.WriteLine("Hydrodynamic节 Dike节更新成功!");
        }

        //插入dike节
        public static void Insert_DikeSec(HydroModel hydromodel, ref PFSSection DIKE_I, Dike dike)
        {
            PFSKeyword name = DIKE_I.InsertNewKeyword("name", 1);
            name.InsertNewParameterString(dike.Name, 1);  //堤防的名称
            PFSKeyword include = DIKE_I.InsertNewKeyword("include", 2);
            if (dike.Operate != Dike_Operate.disable)
            {
                include.InsertNewParameterInt(1, 1);  //堤防是否在用，参数为1在用，参数为0弃用
            }
            else
            {
                include.InsertNewParameterInt(0, 1);  //堤防是否在用，参数为1在用，参数为0弃用
            }

            //类型等其他参数
            PFSKeyword type = DIKE_I.InsertNewKeyword("type", 3);
            type.InsertNewParameterInt(dike.Type, 1);
            PFSKeyword formula = DIKE_I.InsertNewKeyword("formula", 4);
            formula.InsertNewParameterInt(1, 1);
            PFSKeyword critical_level_difference = DIKE_I.InsertNewKeyword("critical_level_difference", 5);
            critical_level_difference.InsertNewParameterDouble(dike.Delhs, 1);
            PFSKeyword coefficient = DIKE_I.InsertNewKeyword("coefficient", 6);
            coefficient.InsertNewParameterDouble(1.838, 1);
            PFSKeyword exponent = DIKE_I.InsertNewKeyword("exponent", 7);
            exponent.InsertNewParameterDouble(1.5, 1);
            PFSKeyword format = DIKE_I.InsertNewKeyword("format", 8);
            format.InsertNewParameterInt(0, 1);
            PFSKeyword constant_value = DIKE_I.InsertNewKeyword("constant_value", 9);
            constant_value.InsertNewParameterDouble(0, 1);
            PFSKeyword file_name = DIKE_I.InsertNewKeyword("file_name", 10);
            file_name.InsertNewParameterFileName("", 1);
            PFSKeyword item_number = DIKE_I.InsertNewKeyword("item_number", 11);
            item_number.InsertNewParameterInt(1, 1);

            //获得计算的起止时间
            DateTime start = hydromodel.ModelGlobalPars.Simulate_time.Begin;
            DateTime end = hydromodel.ModelGlobalPars.Simulate_time.End;
            PFSKeyword _start_time = DIKE_I.InsertNewKeyword("start_time", 12);
            object[] start_time_array = { start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second };
            Nwk11.InsertKeyPars(ref _start_time, start_time_array);
            PFSKeyword _end_time = DIKE_I.InsertNewKeyword("end_time", 13);
            object[] end_time_array = { end.Year, end.Month, end.Day, end.Hour, end.Minute, end.Second };
            Nwk11.InsertKeyPars(ref _end_time, end_time_array);

            PFSKeyword table_file_name = DIKE_I.InsertNewKeyword("table_file_name", 14);
            table_file_name.InsertNewParameterFileName("", 1);
            PFSKeyword specification_of_crest_level = DIKE_I.InsertNewKeyword("specification_of_crest_level", 15);
            specification_of_crest_level.InsertNewParameterInt(2, 1);
            PFSKeyword crest_level = DIKE_I.InsertNewKeyword("crest_level", 16);
            crest_level.InsertNewParameterDouble(0, 1);
            PFSKeyword input_format = DIKE_I.InsertNewKeyword("input_format", 17);
            input_format.InsertNewParameterInt(1, 1);

            //获取工程坐标
            string Projection = hydromodel.ModelGlobalPars.Coordinate_type;
            PFSKeyword coordinate_type = DIKE_I.InsertNewKeyword("coordinate_type", 18);
            coordinate_type.InsertNewParameterString(Projection, 1);

            //修改控制点数关键字和参数
            int point_count = dike.Pointlist.Count;  //集合元素的个数（点的个数）
            PFSKeyword number_of_points = DIKE_I.InsertNewKeyword("number_of_points", 19);
            number_of_points.InsertNewParameterInt(point_count, 1);  //设置堤防中点的个数

            //逐个添加控制点节
            for (int i = 0; i < point_count; i++)  //添加堤防的点
            {
                string point_i = (i + 1).ToString();
                PFSSection POINT_I = DIKE_I.InsertNewSection("POINT_" + point_i, i + 1);
                PFSKeyword x = POINT_I.InsertNewKeyword("x", 1);
                x.InsertNewParameterDouble(dike.Pointlist[i].X, 1);
                PFSKeyword y = POINT_I.InsertNewKeyword("y", 2);
                y.InsertNewParameterDouble(dike.Pointlist[i].Y, 1);
                PFSKeyword z = POINT_I.InsertNewKeyword("z", 3);
                z.InsertNewParameterDouble(dike.Pointlist[i].Z, 1);
            }

            //dike高程随时间改变节
            PFSSection CREST_LEVEL_CHANGE = DIKE_I.InsertNewSection("CREST_LEVEL_CHANGE", point_count + 1);
            PFSKeyword _format = CREST_LEVEL_CHANGE.InsertNewKeyword("format", 1);
            _format.InsertNewParameterInt(0, 1);
            PFSKeyword _constant_value = CREST_LEVEL_CHANGE.InsertNewKeyword("constant_value", 2);
            _constant_value.InsertNewParameterDouble(0, 1);
            PFSKeyword _file_name = CREST_LEVEL_CHANGE.InsertNewKeyword("file_name", 3);
            _file_name.InsertNewParameterFileName("", 1);
            PFSKeyword _item_number = CREST_LEVEL_CHANGE.InsertNewKeyword("item_number", 4);
            _item_number.InsertNewParameterInt(1, 1);
        }

        //插入weir节
        public static void Insert_WeirSec(HydroModel hydromodel, ref PFSSection weir_data, Weir weir)
        {
            //名称
            PFSKeyword Location = weir_data.InsertNewKeyword("Location", 1);
            Nwk11.InsertKeyPars(ref Location, "", -1 * Math.Exp(-155), weir.Name, "TopoID");  //给涵洞命名

            PFSKeyword delhs = weir_data.InsertNewKeyword("delhs", 2);
            delhs.InsertNewParameterDouble(weir.Delhs, 1);

            //工程坐标
            string Projection = hydromodel.ModelGlobalPars.Coordinate_type;
            PFSKeyword coordinate_type = weir_data.InsertNewKeyword("coordinate_type", 3);
            coordinate_type.InsertNewParameterString(Projection, 1);

            //控制点
            PFSKeyword number_of_points = weir_data.InsertNewKeyword("number_of_points", 4);
            number_of_points.InsertNewParameterInt(weir.Pointlist.Count, 1);
            PFSKeyword x = weir_data.InsertNewKeyword("x", 5);  //涵洞控制点
            PFSKeyword y = weir_data.InsertNewKeyword("y", 6);
            for (int i = 0; i < weir.Pointlist.Count; i++)
            {
                Nwk11.InsertKeyPars(ref x, weir.Pointlist[i].X);
                Nwk11.InsertKeyPars(ref y, weir.Pointlist[i].Y);
            }

            PFSKeyword HorizOffset = weir_data.InsertNewKeyword("HorizOffset", 7);
            HorizOffset.InsertNewParameterDouble(0, 1);

            //堰的操作
            PFSKeyword Attributes = weir_data.InsertNewKeyword("Attributes", 8);
            Attributes.InsertNewParameterInt(weir.Type, 1);
            if (weir.Operate == Weir_Operate.enable)
            {
                Attributes.InsertNewParameterInt(0, 2);
            }
            else
            {
                Attributes.InsertNewParameterInt(3, 2);   //封堵，不再流
            }

            //各类水头损失
            PFSKeyword HeadLossFactors = weir_data.InsertNewKeyword("HeadLossFactors", 9);
            Nwk11.InsertKeyPars(ref HeadLossFactors, 0.5, 1, 1, 0.5, 1, 1);
            PFSKeyword WeirFormulaParam = weir_data.InsertNewKeyword("WeirFormulaParam", 10);
            Nwk11.InsertKeyPars(ref WeirFormulaParam, 1, 1, 1.838, 1.5, 1);
            PFSKeyword WeirFormula2Param = weir_data.InsertNewKeyword("WeirFormula2Param", 11);
            Nwk11.InsertKeyPars(ref WeirFormula2Param, 1, 1, 1.838);
            PFSKeyword WeirFormula3Param = weir_data.InsertNewKeyword("WeirFormula3Param", 12);
            Nwk11.InsertKeyPars(ref WeirFormula3Param, 0, 0, 0, 0.6, 1.02, 1.37, 1, 0.03, 1.018, 1, 0, 2.6, 1, 0.7);

            //插入几何节
            PFSSection Geometry = weir_data.InsertNewSection("Geometry", 1);
            PFSKeyword Attributes2 = Geometry.InsertNewKeyword("Attributes", 1);
            Nwk11.InsertKeyPars(ref Attributes2, weir.Type, weir.Datum);

            PFSSection Level_Width = Geometry.InsertNewSection("Level_Width", 1);  //水位与涵洞宽度关系
            //添加水位与涵洞宽度关系
            for (int i = 0; i < weir.Level_Width.Count; i++)
            {
                PFSKeyword Data = Level_Width.InsertNewKeyword("Data", i + 1);
                Nwk11.InsertKeyPars(ref Data, weir.Level_Width.Keys.ElementAt(i), weir.Level_Width.Values.ElementAt(i));
            }
        }

        //更新Hydrodynamic(水动力学)节中 初始条件节的设置
        public static void Update_InitialSec(Mike21_Pars mike21pars, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            Initial_Contition initial = mike21pars.Initial_set;
            PFSSection INITIAL_CONDITIONS = HYDRODYNAMIC_MODULE.GetSection("INITIAL_CONDITIONS", 1);  //3层节
            for (int i = 0; i < INITIAL_CONDITIONS.GetSectionsCount(); i++)
            {
                INITIAL_CONDITIONS.DeleteKeyword(i + 1);
            }

            PFSKeyword Touched = INITIAL_CONDITIONS.InsertNewKeyword("Touched", 1);
            Touched.InsertNewParameterInt(1, 1);

            PFSKeyword type = INITIAL_CONDITIONS.InsertNewKeyword("type", 2);
            type.InsertNewParameterInt(initial.initial_type, 1);

            PFSKeyword surface_elevation_constant = INITIAL_CONDITIONS.InsertNewKeyword("surface_elevation_constant", 3);
            surface_elevation_constant.InsertNewParameterDouble(initial.const_surface_elevation, 1);

            PFSKeyword u_velocity_constant = INITIAL_CONDITIONS.InsertNewKeyword("u_velocity_constant", 4);
            u_velocity_constant.InsertNewParameterDouble(0, 1);

            PFSKeyword v_velocity_constant = INITIAL_CONDITIONS.InsertNewKeyword("v_velocity_constant", 5);
            v_velocity_constant.InsertNewParameterDouble(0, 1);

            PFSKeyword w_velocity_constant = INITIAL_CONDITIONS.InsertNewKeyword("w_velocity_constant", 6);
            w_velocity_constant.InsertNewParameterDouble(0, 1);

            if (initial.initial_type == 2 || initial.initial_type == 3)  //随空间变化
            {
                PFSKeyword file_name_2D = INITIAL_CONDITIONS.InsertNewKeyword("file_name_2D", 7);
                file_name_2D.InsertNewParameterFileName(initial.fileName, 1);

                PFSKeyword surface_elevation_item_no = INITIAL_CONDITIONS.InsertNewKeyword("surface_elevation_item_no", 8);
                surface_elevation_item_no.InsertNewParameterInt(1, 1);

                PFSKeyword item_name_for_surface_elevation = INITIAL_CONDITIONS.InsertNewKeyword("item_name_for_surface_elevation", 9);
                item_name_for_surface_elevation.InsertNewParameterFileName(initial.itemName, 1);
            }

            Console.WriteLine("Hydrodynamic节 Initial_Contition节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 输出节的设置
        public static void Update_OutputSec(HydroModel hydromodel, ref PFSSection HYDRODYNAMIC_MODULE)
        {
            string Projection = hydromodel.ModelGlobalPars.Coordinate_type;
            string out1_dfsugc_filename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            string out2_dfsutj_filename = hydromodel.Modelfiles.Dfsu2_tj_filenane;

            //计算总步数
            SimulateTime begin_endDateTime = hydromodel.ModelGlobalPars.Simulate_time;
            TimeSpan delta_time = begin_endDateTime.End.Subtract(begin_endDateTime.Begin);  //起始时间和结束时间的间隔            
            double delta_timeToSecond = delta_time.TotalSeconds;  //将间隔时间换算成秒
            int simulate_steps = (int)Math.Ceiling(delta_timeToSecond / (int)hydromodel.ModelGlobalPars.Simulate_timestep); //计算时间步数，除不尽时入1

            //更新输出文件路径和输出坐标范围
            PFSSection OUTPUTS = HYDRODYNAMIC_MODULE.GetSection("OUTPUTS", 1);

            //更新输出文件1节的内容，包含输出文件路径、坐标系、范围、计算步数、输出频率
            PFSSection OUTPUT_1 = OUTPUTS.GetSection("OUTPUT_1", 1);
            PFSKeyword coordinate_type1 = OUTPUT_1.GetKeyword("coordinate_type", 1);
            coordinate_type1.GetParameter(1).ModifyStringParameter(Projection);

            PFSKeyword file_name1 = OUTPUT_1.GetKeyword("file_name", 1);
            file_name1.GetParameter(1).ModifyFileNameParameter(out1_dfsugc_filename);  //结果文件输出路径,文件格式为 .dfsu

            PFSKeyword Title1 = OUTPUT_1.GetKeyword("title", 1);
            Title1.GetParameter(1).ModifyStringParameter(Path.GetFileNameWithoutExtension(out1_dfsugc_filename));  //标题

            PFSKeyword last_time_step1 = OUTPUT_1.GetKeyword("last_time_step", 1);  //保存的最后一步，默认为总步数
            last_time_step1.GetParameter(1).ModifyIntParameter(simulate_steps);

            PFSKeyword time_step_frequency1 = OUTPUT_1.GetKeyword("time_step_frequency", 1);    //保存频率
            time_step_frequency1.GetParameter(1).ModifyIntParameter((int)hydromodel.Mike21Pars.Mike21_savetimestepbs);

            PFSSection AREA1 = OUTPUT_1.GetSection("AREA", 1);   //更新输出区域范围
            Update_OutputAreaSec(hydromodel.Modelfiles, ref AREA1);

            //更新输出文件2节的内容，包含输出文件路径、坐标系、范围、计算步数、输出频率
            PFSSection OUTPUT_2 = OUTPUTS.GetSection("OUTPUT_2", 1);
            PFSKeyword coordinate_type2 = OUTPUT_2.GetKeyword("coordinate_type", 1);
            coordinate_type2.GetParameter(1).ModifyStringParameter(Projection);

            PFSKeyword file_name2 = OUTPUT_2.GetKeyword("file_name", 1);
            file_name2.GetParameter(1).ModifyFileNameParameter(out2_dfsutj_filename);  //统计结果输出路径,文件格式为 .dfsu

            PFSKeyword Title2 = OUTPUT_2.GetKeyword("title", 1);
            Title2.GetParameter(1).ModifyStringParameter(Path.GetFileNameWithoutExtension(out2_dfsutj_filename));  //标题

            PFSKeyword last_time_step2 = OUTPUT_2.GetKeyword("last_time_step", 1);
            last_time_step2.GetParameter(1).ModifyIntParameter(simulate_steps);

            PFSSection AREA2 = OUTPUT_2.GetSection("AREA", 1);
            Update_OutputAreaSec(hydromodel.Modelfiles, ref AREA2);

            Console.WriteLine("Hydrodynamic节 Outputs节更新成功!");
        }

        //更新Hydrodynamic(水动力学)节中 输出节中 输出区域坐标节设置
        public static void Update_OutputAreaSec(Model_Files modelfiles, ref PFSSection area_sec)
        {
            MeshFile mesh = MeshFile.ReadMesh(modelfiles.Mesh_filename);

            //获取mesh文件的坐标范围
            double min_x = mesh.X.Min();
            double max_x = mesh.X.Max();
            double min_y = mesh.Y.Min();
            double max_y = mesh.Y.Max();

            PFSSection AREA_POINT_1 = area_sec.GetSection(1);
            PFSKeyword AREA_POINT1_x = AREA_POINT_1.GetKeyword("x", 1);
            AREA_POINT1_x.DeleteParameter(1);
            AREA_POINT1_x.InsertNewParameterDouble(min_x, 1);
            PFSKeyword AREA_POINT1_y = AREA_POINT_1.GetKeyword("y", 1);
            AREA_POINT1_y.DeleteParameter(1);
            AREA_POINT1_y.InsertNewParameterDouble(min_y, 1);

            PFSSection AREA_POINT_2 = area_sec.GetSection(2);
            PFSKeyword AREA_POINT2_x = AREA_POINT_2.GetKeyword("x", 1);
            AREA_POINT2_x.DeleteParameter(1);
            AREA_POINT2_x.InsertNewParameterDouble(min_x, 1);
            PFSKeyword AREA_POINT2_y = AREA_POINT_2.GetKeyword("y", 1);
            AREA_POINT2_y.DeleteParameter(1);
            AREA_POINT2_y.InsertNewParameterDouble(max_y, 1);

            PFSSection AREA_POINT_3 = area_sec.GetSection(3);
            PFSKeyword AREA_POINT3_x = AREA_POINT_3.GetKeyword("x", 1);
            AREA_POINT3_x.DeleteParameter(1);
            AREA_POINT3_x.InsertNewParameterDouble(max_x, 1);
            PFSKeyword AREA_POINT3_y = AREA_POINT_3.GetKeyword("y", 1);
            AREA_POINT3_y.DeleteParameter(1);
            AREA_POINT3_y.InsertNewParameterDouble(max_y, 1);

            PFSSection AREA_POINT_4 = area_sec.GetSection(4);
            PFSKeyword AREA_POINT4_x = AREA_POINT_4.GetKeyword("x", 1);
            AREA_POINT4_x.DeleteParameter(1);
            AREA_POINT4_x.InsertNewParameterDouble(max_x, 1);
            PFSKeyword AREA_POINT4_y = AREA_POINT_4.GetKeyword("y", 1);
            AREA_POINT4_y.DeleteParameter(1);
            AREA_POINT4_y.InsertNewParameterDouble(min_y, 1);
        }
        #endregion ****************************************************************************


        #region *******************************堤防操作****************************************
        //堤防操作--正常挡水 、完全挡水（+100m)、完全不挡水 ,dike编号从1开始
        public static void Dike_Opereate(ref HydroModel hydromodel, string dikename, Dike_Operate operate)
        {
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;
            Dike dike = dikelist.Get_Dikeinfo(dikename);
            switch (operate)
            {
                case Dike_Operate.normal:
                    if (dike.Operate == Dike_Operate.normal)
                    {
                        break;
                    }

                    //如果之前是完全挡水，则需要先恢复到原挡水高度
                    if (dike.Operate == Dike_Operate.full)
                    {
                        int pointnumber = dike.Pointlist.Count;
                        List<PointXYZ> newpointlist = new List<PointXYZ>();
                        for (int i = 0; i < pointnumber; i++)
                        {
                            PointXYZ pointxyz;
                            pointxyz.X = dike.Pointlist[i].X;
                            pointxyz.Y = dike.Pointlist[i].Y;
                            pointxyz.Z = dike.Pointlist[i].Z - 100;
                            newpointlist.Add(pointxyz);
                        }
                        dike.Pointlist = newpointlist;
                    }

                    dike.Operate = Dike_Operate.normal;
                    break;
                case Dike_Operate.disable:
                    if (dike.Operate == Dike_Operate.disable)
                    {
                        break;
                    }
                    //如果之前是完全挡水，则需要先恢复到原挡水高度
                    if (dike.Operate == Dike_Operate.full)
                    {
                        int pointnumber = dike.Pointlist.Count;
                        List<PointXYZ> newpointlist = new List<PointXYZ>();
                        for (int i = 0; i < pointnumber; i++)
                        {
                            PointXYZ pointxyz;
                            pointxyz.X = dike.Pointlist[i].X;
                            pointxyz.Y = dike.Pointlist[i].Y;
                            pointxyz.Z = dike.Pointlist[i].Z - 100;
                            newpointlist.Add(pointxyz);
                        }
                        dike.Pointlist = newpointlist;
                    }

                    dike.Operate = Dike_Operate.disable;
                    break;
                case Dike_Operate.full:
                    if (dike.Operate == Dike_Operate.full)
                    {
                        break;
                    }

                    dike.Operate = Dike_Operate.full;
                    int pointnumber1 = dike.Pointlist.Count;
                    List<PointXYZ> newpointlist1 = new List<PointXYZ>();
                    for (int i = 0; i < pointnumber1; i++)
                    {
                        PointXYZ pointxyz;
                        pointxyz.X = dike.Pointlist[i].X;
                        pointxyz.Y = dike.Pointlist[i].Y;
                        pointxyz.Z = dike.Pointlist[i].Z + 100;
                        newpointlist1.Add(pointxyz);
                    }
                    dike.Pointlist = newpointlist1;
                    break;
            }
        }

        //堤防操作--根据前端给定的堤防线路控制点添加堤防1,最终的dike控制点通过与该区域的mesh节点比较确定
        public static void Add_Dike(ref HydroModel hydromodel, List<PointXY> pointlist, string dikename)
        {
            //提取模型参数
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;
            int ID = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;

            //新建dike对象
            Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, ID, dikename, pointlist);

            //将新dike加入集合
            dikelist.AddDike(newdike);
        }

        //堤防操作--根据前端给定的堤防线路控制点添加堤防2,最终的dike控制点通过与该区域的mesh节点比较确定,高程为地面高程+平均高度
        public static void Add_Dike(ref HydroModel hydromodel, List<PointXY> pointlist, string dikename, double dike_aveheight)
        {
            //提取模型参数
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;
            int ID = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;

            //新建dike对象
            Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, ID, dikename, pointlist, dike_aveheight);

            //将新dike加入集合
            dikelist.AddDike(newdike);
        }

        //堤防操作--根据前端给定的堤防线路控制点添加堤防3,最终的dike控制点通过与该区域的mesh节点比较确定,高程为给定dike常量高程
        public static void Add_Dike(ref HydroModel hydromodel, List<PointXY> pointlist, string dikename, double dike_crest, bool is_const)
        {
            //提取模型参数
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;
            int ID = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;

            //新建dike对象
            Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, ID, dikename, pointlist, dike_crest, is_const);

            //将新dike加入集合
            dikelist.AddDike(newdike);
        }

        //堤防操作--根据前端给定的堤防线路控制点添加堤防4,通过与mesh节点比较，确定最终dike点集合，采用控制点本身的高程构造Dike
        public static void Add_Dike(ref HydroModel hydromodel, List<PointXYZ> pointlist, string dikename)
        {
            //提取模型参数
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;
            int ID = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;

            //新建dike对象
            Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, ID, dikename, pointlist);

            //将新dike加入集合
            dikelist.AddDike(newdike);
        }


        //通过与mesh节点比较，获取堤防控制点坐标
        public static List<PointXYZ> GetDike_ControlPoints(MeshFile meshfile, List<PointXY> pointlist)
        {
            List<PointXYZ> node_pointlist = Extract_SubArea_Nodes(meshfile, pointlist);  //子区域中的节点
            List<PointXYZ> closestPoint = new List<PointXYZ>();  //子区域中离用户给定点的最近点集合,即挡水建筑物控制点

            double side_length = MeshPars.Get_MeshSide_AveLength(meshfile) / 4;
            for (int i = 0; i < pointlist.Count; i++)  //遍历用户给定点
            {
                PointXYZ closestPoint_i = SearchClosestPoint(pointlist[i], node_pointlist);  //寻找用户给定第i个点的最近点
                if (!closestPoint.Contains(closestPoint_i))
                {
                    closestPoint.Add(closestPoint_i);
                }

                if (i < pointlist.Count - 1)  //判定不是最后一个点，需要在该点与后面一个点之间进行插值
                {
                    //计算第i个点与第i+1个点之间的距离
                    double length = Math.Sqrt(Math.Pow(pointlist[i].X - pointlist[i + 1].X, 2) + Math.Pow(pointlist[i].Y - pointlist[i + 1].Y, 2));
                    int number = (int)(length / side_length);  //以网格1/4的长度插值，插入点的个数

                    for (int j = 0; j < number; j++)  //循环每一个插值点
                    {
                        PointXY points_j;  //第j个插值点
                        points_j.X = ((j + 1) * side_length) / length * (pointlist[i + 1].X - pointlist[i].X) + pointlist[i].X;
                        points_j.Y = ((j + 1) * side_length) / length * (pointlist[i + 1].Y - pointlist[i].Y) + pointlist[i].Y;

                        PointXYZ closestPoint_j = SearchClosestPoint(points_j, node_pointlist);  //寻找第j个插值点的最近点
                        if (!closestPoint.Contains(closestPoint_j))
                        {
                            closestPoint.Add(closestPoint_j);
                        }
                    }
                }
            }
            return closestPoint;
        }

        //提取在子区域内的节点
        public static List<PointXYZ> Extract_SubArea_Nodes(MeshFile meshfile, List<PointXY> pointlist)
        {
            double meshside_length = MeshPars.Get_MeshSide_AveLength(meshfile);
            SubArea subArea = Get_Sub_Area(pointlist, meshside_length);  //获取子区域

            //节点坐标
            double[] X = meshfile.X;
            double[] Y = meshfile.Y;
            double[] Z = meshfile.Z;

            List<PointXYZ> nodes = new List<PointXYZ>(); //声明一个集合，用来存储在子区域内的节点

            //遍历所有节点，判断节点是否在子区域内
            for (int i = 0; i < meshfile.NumberOfNodes; i++)
            {
                if ((X[i] >= subArea.x1) && (X[i] <= subArea.x2) && (Y[i] >= subArea.y1) && (Y[i] <= subArea.y2))
                {
                    PointXYZ point;
                    point.X = X[i]; point.Y = Y[i]; point.Z = Z[i];
                    nodes.Add(point);
                }
            }
            return nodes;
        }

        //根据用户给定的点集合，适当扩大2倍网格距离，得到子区域
        public static SubArea Get_Sub_Area(List<PointXY> pointlist, double meshside_length)
        {
            SubArea subArea;
            subArea.x1 = 0; subArea.x2 = 0; subArea.y1 = 0; subArea.y2 = 0;  //初始化边界坐标
            for (int i = 0; i < pointlist.Count; i++)  //遍历集合中每一个点，最小值赋值给左下角边界，最大值赋值给右上角边界
            {
                if (i == 0)
                {
                    subArea.x1 = pointlist[i].X;
                    subArea.x2 = pointlist[i].X;
                    subArea.y1 = pointlist[i].Y;
                    subArea.y2 = pointlist[i].Y;
                }
                else
                {
                    if (pointlist[i].X < subArea.x1)
                    {
                        subArea.x1 = pointlist[i].X;
                    }
                    if (pointlist[i].X > subArea.x2)
                    {
                        subArea.x2 = pointlist[i].X;
                    }
                    if (pointlist[i].Y < subArea.y1)
                    {
                        subArea.y1 = pointlist[i].Y;
                    }
                    if (pointlist[i].Y > subArea.y2)
                    {
                        subArea.y2 = pointlist[i].Y;
                    }
                }
            }

            //将子区域的每个边界向外扩2个最大网格边长，保证用户给定点所在元素都在子区域内
            subArea.x1 -= meshside_length * 2;
            subArea.y1 -= meshside_length * 2;
            subArea.x2 += meshside_length * 2;
            subArea.y2 += meshside_length * 2;

            return subArea;
        }

        // 寻找集合中离指定点最近的点
        public static PointXYZ SearchClosestPoint(PointXY points, List<PointXYZ> point)
        {
            double length = 0;  //初始化最小距离

            PointXYZ ClosestPoint;  //定义并初始化最近的点
            ClosestPoint.X = points.X;
            ClosestPoint.Y = points.Y;
            ClosestPoint.Z = 0;

            for (int i = 0; i < point.Count; i++)  //遍历集合中的点
            {
                double _length = Math.Sqrt(Math.Pow(points.X - point[i].X, 2) + Math.Pow(points.Y - point[i].Y, 2));  //计算给定点与第i个点的距离
                if (i == 0)
                {
                    length = _length;
                    ClosestPoint = point[i];
                }
                else
                {
                    if (_length < length)  //如果距离小于最小距离，则把值赋给最小距离，并记录最近点
                    {
                        length = _length;
                        ClosestPoint = point[i];
                    }
                }
            }
            return ClosestPoint;
        }

        #endregion *****************************************************************************


        #region *******************************涵洞操作*****************************************
        //涵洞操作--正常启用过水、堵死不过水（+100m）
        public static void Weir_Opereate(ref HydroModel hydromodel, string weirname, Weir_Operate operate)
        {
            WeirList weirlist = hydromodel.Mike21Pars.WeirList;

            Weir weir = weirlist.Get_Weir(weirname);
            switch (operate)
            {
                case Weir_Operate.enable:
                    weir.Operate = Weir_Operate.enable;
                    break;
                case Weir_Operate.disable:
                    weir.Operate = Weir_Operate.disable;
                    break;
            }
        }

        //涵洞操作--在已有dike上增加涵洞1(堰底高程取地面高程)，并将原来的dike一分为二，dike和weir分别更新
        public static void AddWeirAtDike(ref HydroModel hydromodel, PointXY point, string weir_name, double weir_width)
        {
            WeirList weirlist = hydromodel.Mike21Pars.WeirList;
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;

            //先判断是否在dike上
            PointXYZ ClosestPoint;
            PointXYZ SecondPoint;
            int dike_i;
            int YN = SearchClosestDikePoint(dikelist, point, out ClosestPoint, out SecondPoint, out dike_i);
            if (YN == -1)  //不在dike上，添加失败
            {
                Console.WriteLine("添加涵洞失败，未找到公路或堤防！");
                return;
            }

            //新建weir对象
            Weir newweir = new Weir(dikelist, weirlist.MaxWeirId + 1, weir_name, point, weir_width);
            weirlist.AddWeir(newweir);

            //将原来的dike一分为二
            List<PointXYZ> Points1;
            List<PointXYZ> Points2;
            DivideDike(dikelist.Dike_List[dike_i], newweir, out Points1, out Points2);

            //删除原来的dike
            string olddikename = dikelist.Dike_List[dike_i].Name;
            dikelist.RemoveDike(olddikename);

            //新建dike对象，并加入集合
            int dike1id = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;
            if (Points1 != null)
            {
                string newdikename = olddikename + "_1";
                Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, dike1id, newdikename, Points1);
                dikelist.AddDike(newdike);
            }

            int dike2id = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;
            if (Points2 != null)
            {
                string newdikename = olddikename + "_2";
                Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, dike2id, newdikename, Points2);
                dikelist.AddDike(newdike);
            }
        }

        //涵洞操作--在已有dike上增加涵洞2(给定堰底高程)，并将原来的dike一分为二，dike和weir分别更新
        public static void AddWeirAtDike(ref HydroModel hydromodel, PointXY point, string weir_name, double weir_width, double datum)
        {
            WeirList weirlist = hydromodel.Mike21Pars.WeirList;
            DikeList dikelist = hydromodel.Mike21Pars.DikeList;

            //先判断是否在dike上
            PointXYZ ClosestPoint;
            PointXYZ SecondPoint;
            int dike_i;
            int YN = SearchClosestDikePoint(dikelist, point, out ClosestPoint, out SecondPoint, out dike_i);
            if (YN == -1)  //不在dike上，添加失败
            {
                Console.WriteLine("添加堰失败，距离超高阀值！");
                return;
            }

            //新建weir对象
            Weir newweir = new Weir(dikelist, weirlist.MaxWeirId + 1, weir_name, point, weir_width, datum);
            weirlist.AddWeir(newweir);

            //将原来的dike一分为二
            List<PointXYZ> Points1;
            List<PointXYZ> Points2;
            DivideDike(dikelist.Dike_List[dike_i], newweir, out Points1, out Points2);

            //删除原来的dike
            string olddikename = dikelist.Dike_List[dike_i].Name;
            dikelist.RemoveDike(olddikename);

            //新建dike对象，并加入集合
            int dike1id = hydromodel.Mike21Pars.DikeList.MaxDikeId + 1;
            if (Points1 != null)
            {
                string newdikename = olddikename + "_1";
                Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, dike1id, newdikename, Points1);
                dikelist.AddDike(newdike);
            }

            if (Points2 != null)
            {
                string newdikename = olddikename + "_2";
                Dike newdike = new Dike(hydromodel.Modelfiles.Mesh_filename, dike1id, newdikename, Points2);
                dikelist.AddDike(newdike);
            }
        }

        // 寻找指定点最近的dike和其上最近的点、第二近的点
        public static int SearchClosestDikePoint(DikeList dikelist, PointXY point, out PointXYZ ClosestPoint, out PointXYZ SecondPoint, out int dike_i)
        {
            double minlength = 1000;

            //初始化最近的点
            ClosestPoint.X = point.X;
            ClosestPoint.Y = point.Y;
            ClosestPoint.Z = 0;

            //初始化涵洞的另一个点
            SecondPoint.X = point.X;
            SecondPoint.Y = point.Y;
            SecondPoint.Z = 0;

            //初始化最近堤防的序号
            dike_i = 0;

            int weir_pointj = 0;  //定义并初始化堤防上最近点的排列序号

            //寻找最近点
            for (int i = 0; i < dikelist.Dike_List.Count; i++)  //遍历堤防，寻找最近的堤防
            {
                Dike dike = dikelist.Dike_List[i];
                for (int j = 0; j < dike.Pointlist.Count; j++)  //遍历堤防的控制点，寻找最近的点
                {
                    PointXYZ pointj = dike.Pointlist[j];
                    double x = pointj.X;
                    double y = pointj.Y;
                    double z = pointj.Z;
                    double _length = Math.Sqrt(Math.Pow(x - point.X, 2) + Math.Pow(y - point.Y, 2));  //第i个堤防的第j个点与给定点的距离

                    if (_length < minlength)  //第i个堤防的第j个点与给定点的距离小于最小永许距离
                    {
                        minlength = _length;
                        ClosestPoint.X = x;
                        ClosestPoint.Y = y;
                        ClosestPoint.Z = z;
                        dike_i = i;
                        weir_pointj = j;
                    }
                }
            }

            //如果找到的最近的dike点也超出允许值，直接返回
            if (minlength > Model_Const.INSERT_WEIR_MINDISTANCE)
            {
                Console.WriteLine("插入的点超出允许距离!{0}", Model_Const.INSERT_WEIR_MINDISTANCE);
                return -1;
            }

            //找到第二近的dike点
            if (weir_pointj != 0 && weir_pointj != dikelist.Dike_List[dike_i].Pointlist.Count - 1)
            {
                double length1 = Math.Sqrt(Math.Pow(dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].X - point.X, 2) + Math.Pow(dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].Y - point.Y, 2));  //前一个点与指定点的距离
                double length2 = Math.Sqrt(Math.Pow(dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].X - point.X, 2) + Math.Pow(dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].Y - point.Y, 2));  //后一个点与指定点的距离

                if (length1 <= length2)  //前一个点近
                {
                    SecondPoint.X = dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].X;
                    SecondPoint.Y = dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].Y;
                    SecondPoint.Z = dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].Z;
                }
                else  //后一个点近
                {
                    SecondPoint.X = dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].X;
                    SecondPoint.Y = dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].Y;
                    SecondPoint.Z = dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].Z;
                }
            }
            else if (weir_pointj == 0)
            {
                SecondPoint.X = dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].X;
                SecondPoint.Y = dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].Y;
                SecondPoint.Z = dikelist.Dike_List[dike_i].Pointlist[weir_pointj + 1].Z;
            }
            else
            {
                SecondPoint.X = dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].X;
                SecondPoint.Y = dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].Y;
                SecondPoint.Z = dikelist.Dike_List[dike_i].Pointlist[weir_pointj - 1].Z;
            }

            return 0;
        }

        // 根据涵洞的控制点将堤防的控制点分成两部分
        public static void DivideDike(Dike olddike, Weir newweir, out List<PointXYZ> Points1, out List<PointXYZ> Points2)
        {
            PointXY start_point = newweir.Pointlist[0];
            PointXY end_point = newweir.Pointlist[newweir.Pointlist.Count - 1];

            int number_of_points = olddike.Pointlist.Count;  //堤防的控制点数

            Points1 = new List<PointXYZ>();  //存储涵洞前面的堤防控制点的集合
            Points2 = new List<PointXYZ>();  //存储涵洞后面的堤防控制点的集合

            int weir_i = 0;  //定义并初始化涵洞第一个点在堤防控制点中的排列序号
            PointXYZ point;  //定义一个点，用来存储堤防某控制点的坐标

            //遍历堤防的控制点，寻找涵洞前的堤防控制点，存入集合中
            for (int i = 0; i < number_of_points; i++)
            {
                double x = olddike.Pointlist[i].X;
                double y = olddike.Pointlist[i].Y;
                double z = olddike.Pointlist[i].Z;

                if (i == 0)  //第一个点，判断是否为涵洞的控制点
                {
                    if ((x == start_point.X && y == start_point.Y) || (x == end_point.X && y == end_point.Y))  //若为涵洞控制点，此时堤防没有被分为两部分
                    {
                        weir_i = i;  //记录涵洞的第一个点在堤防控制点中的排列序号
                        break;  //堤防没有被分为两部分，涵洞前面没有堤防，不必再循环，此时Points1是空集合
                    }
                    else  //第一个点不是涵洞控制点
                    {
                        point.X = x;
                        point.Y = y;
                        point.Z = z;
                        Points1.Add(point);
                    }
                }
                else  //i!=0，程序能运行到这一步，说明第一个点不是涵洞控制点
                {
                    //若为涵洞控制点，则此点为涵洞前面堤防的最后一个控制点；若不是涵洞控制点，则此点为涵洞前面堤防中间的某个控制点；无论哪种情况，都需要存入到集合Points1中
                    point.X = x;
                    point.Y = y;
                    point.Z = z;
                    Points1.Add(point);

                    if ((x == start_point.X && y == start_point.Y) || (x == end_point.X && y == end_point.Y))  //此点为涵洞控制点
                    {
                        weir_i = i;  //记录涵洞的第一个点在堤防控制点中的排列序号
                        break;  //此点为涵洞前面堤防的最后一个控制点
                    }
                }
            }

            //将涵洞后面堤防的控制点存入到集合中
            for (int j = weir_i + 1; j < number_of_points; j++)  //从涵洞的第二个控制点开始循环
            {
                if (weir_i + 1 == number_of_points - 1)  //最后一个点，即涵洞的第二点是堤防的终点
                {
                    break;  //不必再循环，涵洞后面没有堤防，Points2是空集合
                }
                else  //涵洞的第二个点不是堤防的终点，即涵洞后面有堤防
                {
                    double x = olddike.Pointlist[j].X;
                    double y = olddike.Pointlist[j].Y;
                    double z = olddike.Pointlist[j].Z;

                    point.X = x;
                    point.Y = y;
                    point.Z = z;
                    Points2.Add(point);
                }
            }
        }
        #endregion ******************************************************************************

    }


    //保存最新所有Dike的类
    [Serializable]
    public class DikeList
    {
        //**********************属性************************
        //模型中的Dike信息
        public List<Dike> Dike_List
        { get; set; }

        //Dike编号的最大值,也代表的是Dike数量
        public int MaxDikeId
        { get; set; }

        //**********************方法************************

        //增加新Dike静态方法
        public void AddDike(Dike newdike)
        {
            if (!Dike_List.Contains(newdike))
            {
                //往集合中增加新dike
                Dike_List.Add(newdike);

                //产汇流流域ID的最大值
                MaxDikeId++;
            }
        }

        //减少dike静态方法
        public void RemoveDike(string dikename)
        {
            for (int i = 0; i < Dike_List.Count; i++)
            {
                if (Dike_List[i].Name == dikename)
                {
                    Dike_List.RemoveAt(i);
                    MaxDikeId--;
                    break;
                }
            }
        }

        //根据dike名获取该dike详细信息方法
        public Dike Get_Dikeinfo(string dikename)
        {
            Dike dike = Dike_List[0];

            for (int i = 0; i < Dike_List.Count; i++)
            {
                if (Dike_List[i].Name == dikename)
                {
                    dike = Dike_List[i];
                    break;
                }
            }
            return dike;
        }
    }

    //保存最新所有Weir的类
    [Serializable]
    public class WeirList
    {
        //**********************属性************************
        //模型中的Weir信息
        public List<Weir> Weir_List
        { get; set; }

        //Weir编号的最大值,也代表的是Weir数量
        public int MaxWeirId
        { get; set; }

        //**********************方法************************

        //增加新Weir方法
        public void AddWeir(Weir newweir)
        {
            if (!Weir_List.Contains(newweir))
            {
                //往集合中增加新dike
                Weir_List.Add(newweir);

                //产汇流流域ID的最大值
                if (newweir.ID > MaxWeirId)
                {
                    MaxWeirId = newweir.ID;
                }
            }
        }

        //减少Weir方法
        public void RemoveDike(string weirname)
        {
            for (int i = 0; i < Weir_List.Count; i++)
            {
                if (Weir_List[i].Name == weirname)
                {
                    Weir_List.RemoveAt(i);
                    if (MaxWeirId == Weir_List[i].ID)
                    {
                        MaxWeirId--;
                    }
                    break;
                }
            }
        }

        //根据Weir名获取该Weir详细信息方法
        public Weir Get_Weir(string weirname)
        {
            Weir weir = Weir_List[0];

            for (int i = 0; i < Weir_List.Count; i++)
            {
                if (Weir_List[i].Name == weirname)
                {
                    weir = Weir_List[i];
                    break;
                }
            }
            return weir;
        }
    }

    //堤防Dike类
    [Serializable]
    public class Dike
    {
        //构造函数

        //参数默认构造函数
        public Dike(string Mesh_filename, int ID, string name)
        {
            SetDefault();
            this.Mesh_filename = Mesh_filename;
            this.Name = name;
            this.ID = ID;
        }

        //根据前端给定的线路控制点，通过与mesh节点比较，确定最终dike点集合，采用默认高度构造Dike
        public Dike(string Mesh_filename, int ID, string name, List<PointXY> Pointlist)
        {
            SetDefault();
            this.Mesh_filename = Mesh_filename;
            this.Name = name;
            this.ID = ID;

            //得到的dike上的点高程为地面高程
            MeshFile mesh = MeshFile.ReadMesh(Mesh_filename);
            List<PointXYZ> dem_pointlist = M21fm.GetDike_ControlPoints(mesh, Pointlist);

            //重新加高
            List<PointXYZ> dike_pointlist = new List<PointXYZ>();
            PointXYZ dike_point;
            for (int i = 0; i < dem_pointlist.Count; i++)
            {
                dike_point.X = dem_pointlist[i].X;
                dike_point.Y = dem_pointlist[i].Y;
                dike_point.Z = dem_pointlist[i].Z + this.AverageHeight;
                dike_pointlist.Add(dike_point);
            }
            this.Pointlist = dike_pointlist;
        }

        //根据前端给定的线路控制点，通过与mesh节点比较，确定最终dike点集合，采用给定高度构造Dike
        public Dike(string Mesh_filename, int ID, string name, List<PointXY> Pointlist, double dikeheight)
        {
            SetDefault();
            this.Mesh_filename = Mesh_filename;
            this.Name = name;
            this.ID = ID;
            this.AverageHeight = dikeheight;

            //得到的dike上的点高程为地面高程
            MeshFile mesh = MeshFile.ReadMesh(Mesh_filename);
            List<PointXYZ> dem_pointlist = M21fm.GetDike_ControlPoints(mesh, Pointlist);

            //重新加高
            List<PointXYZ> dike_pointlist = new List<PointXYZ>();
            PointXYZ dike_point;
            for (int i = 0; i < dem_pointlist.Count; i++)
            {
                dike_point.X = dem_pointlist[i].X;
                dike_point.Y = dem_pointlist[i].Y;
                dike_point.Z = dem_pointlist[i].Z + dikeheight;
                dike_pointlist.Add(dike_point);
            }
            this.Pointlist = dike_pointlist;
        }

        //根据前端给定的线路控制点，通过与mesh节点比较，确定最终dike点集合，采用给定常量高程构造Dike
        public Dike(string Mesh_filename, int ID, string name, List<PointXY> Pointlist, double dike_crest, bool is_const)
        {
            SetDefault();
            this.Mesh_filename = Mesh_filename;
            this.Name = name;
            this.ID = ID;

            //得到的dike上的点高程为地面高程
            MeshFile mesh = MeshFile.ReadMesh(Mesh_filename);
            List<PointXYZ> dem_pointlist = M21fm.GetDike_ControlPoints(mesh, Pointlist);

            //重新加高
            List<PointXYZ> dike_pointlist = new List<PointXYZ>();
            PointXYZ dike_point;
            for (int i = 0; i < dem_pointlist.Count; i++)
            {
                dike_point.X = dem_pointlist[i].X;
                dike_point.Y = dem_pointlist[i].Y;
                dike_point.Z = dike_crest;
                dike_pointlist.Add(dike_point);
            }
            this.Pointlist = dike_pointlist;
        }

        //根据前端给定的线路控制点，通过与mesh节点比较，确定最终dike点集合，采用控制点本身的高程构造Dike
        public Dike(string Mesh_filename, int ID, string name, List<PointXYZ> Pointlist)
        {
            SetDefault();
            this.Mesh_filename = Mesh_filename;
            this.Name = name;
            this.ID = ID;

            //提取前端控制点的PointXY集合
            List<PointXY> pointxylist = new List<PointXY>();
            for (int i = 0; i < Pointlist.Count; i++)
            {
                PointXY pointxy;
                pointxy.X = Pointlist[i].X;
                pointxy.Y = Pointlist[i].Y;
                pointxylist.Add(pointxy);
            }

            //得到的dike上的点和其高程为地面高程
            MeshFile mesh = MeshFile.ReadMesh(Mesh_filename);
            List<PointXYZ> dem_pointlist = M21fm.GetDike_ControlPoints(mesh, pointxylist);

            List<PointXY> pointxylist1 = new List<PointXY>();
            for (int i = 0; i < Pointlist.Count; i++)
            {
                PointXY pointxy;
                pointxy.X = dem_pointlist[i].X;
                pointxy.Y = dem_pointlist[i].Y;
                pointxylist1.Add(pointxy);
            }

            //重新给dike控制点的高程内插值
            List<PointXYZ> dike_pointxyz_list = Mesh.Get_Interpolatexyz(Pointlist, pointxylist1);

            this.Pointlist = dike_pointxyz_list;
        }

        //属性
        public string Mesh_filename  //所在mesh文件路径
        { get; set; }

        public string Name    //名称
        { get; set; }

        public int Type   //Dike的类型，0，1，2 ,默认为1 --Empirical formula
        { get; set; }

        public int ID   //堤防序号
        { get; set; }

        public Dike_Operate Operate  //启用状态
        { get; set; }

        public List<PointXYZ> Pointlist  //堤防上的点集合
        { get; set; }

        public double AverageHeight    //平均高度
        { get; set; }

        public double Delhs  //过流水头差阀值
        { get; set; }

        //方法
        //设置默认属性
        public void SetDefault()
        {
            this.Mesh_filename = "";
            this.Name = "";
            this.Type = 1;
            this.ID = 1;
            this.Operate = Dike_Operate.normal;

            List<PointXYZ> Pointlist = new List<PointXYZ>();
            this.Pointlist = Pointlist;

            this.AverageHeight = 3.5;
            this.Delhs = 0.01;
        }

    }

    //Weir类
    [Serializable]
    public class Weir
    {
        //构造函数

        //参数默认构造函数
        public Weir(int ID, string name)
        {
            SetDefault();
            this.Name = name;
            this.ID = ID;
        }

        //根据前端给定点集合，构造Weir,不一定在dike上
        public Weir(int ID, string name, List<PointXY> Pointlist, double width, double datum)
        {
            SetDefault();
            this.Name = name;
            this.ID = ID;

            this.Pointlist = Pointlist;

            Dictionary<double, double> level_width = new Dictionary<double, double>();
            level_width.Add(0, width);
            level_width.Add(4, width);
            this.Level_Width = level_width;

            this.Datum = datum;
        }

        //构建在dike上的堰 -- 根据前端给定的一个点，通过判断最近的dike
        public Weir(DikeList dikelist, int ID, string name, PointXY point, double width)
        {
            SetDefault();
            this.Name = name;
            this.Dikelist = dikelist;
            this.ID = ID;

            // 寻找指定点最近的dike和其上最近的点、第二近的点
            PointXYZ ClosestPoint;
            PointXYZ SecondPoint;
            int dike_i;
            M21fm.SearchClosestDikePoint(dikelist, point, out ClosestPoint, out SecondPoint, out dike_i);

            //堰的控制点集合赋值
            List<PointXY> pointlist = new List<PointXY>();
            PointXY point1;
            point1.X = ClosestPoint.X;
            point1.Y = ClosestPoint.Y;
            pointlist.Add(point1);
            PointXY point2;
            point2.X = SecondPoint.X;
            point2.Y = SecondPoint.Y;
            pointlist.Add(point2);
            this.Pointlist = pointlist;

            //堰底高程
            MeshFile mesh = MeshFile.ReadMesh(this.Dikelist.Dike_List[0].Mesh_filename);
            double z1 = Mesh.Get_Point_Z(mesh, point1);
            double z2 = Mesh.Get_Point_Z(mesh, point2);
            this.Datum = z1;
            if (z2 < z1)
            {
                this.Datum = z2;
            }

            //堰的水位宽度关系
            Dictionary<double, double> level_width = new Dictionary<double, double>();
            level_width.Add(0, width);
            level_width.Add(Math.Abs(ClosestPoint.Z - this.Datum), width);
            this.Level_Width = level_width;
        }

        //根据前端给定的一个点，通过判断最近的dike，构建在dike上的堰
        public Weir(DikeList dikelist, int ID, string name, PointXY point, double width, double datum)
        {
            SetDefault();
            this.Name = name;
            this.Dikelist = dikelist;
            this.ID = ID;

            // 寻找指定点最近的dike和其上最近的点、第二近的点
            PointXYZ ClosestPoint;
            PointXYZ SecondPoint;
            int dike_i;
            M21fm.SearchClosestDikePoint(dikelist, point, out ClosestPoint, out SecondPoint, out dike_i);

            //堰的控制点集合赋值
            List<PointXY> pointlist = new List<PointXY>();
            PointXY point1;
            point1.X = ClosestPoint.X;
            point1.Y = ClosestPoint.Y;
            pointlist.Add(point1);
            PointXY point2;
            point2.X = SecondPoint.X;
            point2.Y = SecondPoint.Y;
            pointlist.Add(point2);
            this.Pointlist = pointlist;

            //堰底高程
            this.Datum = datum;

            //堰的水位宽度关系
            Dictionary<double, double> level_width = new Dictionary<double, double>();
            level_width.Add(0, width);
            level_width.Add(Math.Abs(ClosestPoint.Z - this.Datum), width);
            this.Level_Width = level_width;

            this.Datum = datum;
        }

        //属性
        public DikeList Dikelist   //所在的dike集合
        { get; set; }

        public string Name    //名称
        { get; set; }

        public int Type   //堰的类型，0为宽顶堰，1为堰流公式1，2为堰流公式2
        { get; set; }

        public int ID   //堰的序号
        { get; set; }

        public Weir_Operate Operate  //是否启用
        { get; set; }

        public List<PointXY> Pointlist  //堰上的点集合
        { get; set; }

        public double Datum   //堰底高程
        { get; set; }

        public double Delhs  //堰的过流水头差阀值
        { get; set; }

        public Dictionary<double, double> Level_Width //水位与涵洞宽度的关系,水位是键
        { get; set; }

        //方法

        //设置默认属性
        public void SetDefault()
        {
            this.Dikelist = new DikeList();
            this.Name = "";
            this.Type = 0;   //宽顶堰
            this.ID = 1;
            this.Operate = Weir_Operate.enable;

            List<PointXY> Pointlist = new List<PointXY>();
            this.Pointlist = Pointlist;

            this.Datum = 0;
            this.Delhs = 0.01;

            Dictionary<double, double> level_width = new Dictionary<double, double>();
            level_width.Add(0, 6);
            level_width.Add(4, 6);

            this.Level_Width = level_width;
        }

    }

}