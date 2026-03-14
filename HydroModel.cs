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
    //模型文件地址参数
    [Serializable]
    public class Model_Files
    {
        #region 构造函数
        public Model_Files()
        {
            SetDefault();
        }

        //根据方案名创建新文件夹和各文件地址
        public Model_Files(string modelname)
        {
            Create_ModelFilePath(modelname);
        }
        #endregion

        #region 属性
        //耦合模拟文件地址
        public string Couple_filename { get; set; }

        //*********** Mike11各模型文件地址************//
        //mike11模型文件地址
        public string Simulate_filename { get; set; }
        public string Nwk11_filename { get; set; }
        public string Xns11_filename { get; set; }
        public string Bnd11_filename { get; set; }
        public string Rr11_filename { get; set; }
        public string Hd11_filename { get; set; }
        public string Ad11_filename { get; set; }

        //mike11和rr ad结果文件地址
        public string Hdres11_filename { get; set; }
        public string Hd_addres11_filename { get; set; }
        public string Rrres11_filename { get; set; }
        public string Adres11_filename { get; set; }
        public string Hdtxt_filename { get; set; }
        public string XAJres_filename { get; set; }

        //进度信息文件地址
        public string Progressinfo_filename { get; set; }
        //***********************************************//


        //******* Mike21各模型文件最新地址和参数*********//
        public string M21fm_filename { get; set; }
        public string Mesh_filename { get; set; }
        public string M21caolv_filename { get; set; }

        //mike21结果文件地址
        public string Dfsu1_gc_filename { get; set; }
        public string Dfsu2_tj_filenane { get; set; }
        //***********************************************//
        #endregion

        #region 方法
        //获取默认模型的模型文件地址类
        public static Model_Files Get_Modelfilepath(string model_dirname)
        {
            Model_Files modelfiles = new Model_Files();

            //先判断是否存在online_models\default\username的文件夹
            //Console.WriteLine(System.Environment.CurrentDirectory);
            if (!Directory.Exists(model_dirname))
            {
                Console.WriteLine("未找到默认模型文件夹!!");
                return null;
            }

            //搜索文件夹，逐个加载默认模型文件
            Get_ModelFile_FromDir(ref modelfiles, model_dirname);

            return modelfiles;
        }

        //搜索文件夹，从中获取默认模型各文件
        public static void Get_ModelFile_FromDir(ref Model_Files modelfiles, string dirname)
        {
            DirectoryInfo modeldirinfo = new DirectoryInfo(dirname);

            //先从文件中判断 默认模型是那种模型
            modelfiles.Couple_filename = Directory.GetFiles(dirname, "*.couple").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.couple")[0]) : "";

            modelfiles.Simulate_filename = Directory.GetFiles(dirname, "*.sim11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.sim11")[0]) : "";
            modelfiles.Nwk11_filename = Directory.GetFiles(dirname, "*.nwk11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.nwk11")[0]) : "";
            modelfiles.Xns11_filename = Directory.GetFiles(dirname, "*.xns11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.xns11")[0]) : "";
            modelfiles.Bnd11_filename = Directory.GetFiles(dirname, "*.bnd11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.bnd11")[0]) : "";
            modelfiles.Rr11_filename = Directory.GetFiles(dirname, "*.rr11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.rr11")[0]) : "";
            modelfiles.Hd11_filename = Directory.GetFiles(dirname, "*.hd11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.hd11")[0]) : "";
            modelfiles.Ad11_filename = Directory.GetFiles(dirname, "*.ad11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.ad11")[0]) : "";

            modelfiles.M21fm_filename = Directory.GetFiles(dirname, "*.m21fm").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.m21fm")[0]) : "";
            modelfiles.Mesh_filename = Directory.GetFiles(dirname, "*.mesh").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.mesh")[0]) : "";
            modelfiles.M21caolv_filename = Directory.GetFiles(dirname, "*.dfsu").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.dfsu")[0]) : "";

            //新建结果文件夹
            if (Directory.Exists(modeldirinfo.FullName + @"\" + "results")) Directory.Delete(modeldirinfo.FullName + @"\" + "results", true);
            DirectoryInfo modelresultdirinfo = Directory.CreateDirectory(modeldirinfo.FullName + @"\" + "results");
            modelfiles.Hdres11_filename = modelresultdirinfo.FullName + @"\hd_result.res1d";  //默认模型的这个结果文件被重写了，故没问题
            modelfiles.Hd_addres11_filename = modelresultdirinfo.FullName + @"\hd_result.ADDOUT.res1d";
            modelfiles.Adres11_filename = modelresultdirinfo.FullName + @"\ad_result.res1d";

            modelfiles.Dfsu1_gc_filename = modelresultdirinfo.FullName + @"\m21_gc.dfsu";     //这个可能有问题,没重写
            modelfiles.Progressinfo_filename = modelresultdirinfo.FullName + @"\progress.txt"; //这个也没问题
        }

        //设置模型属性 -- 全部为空
        public void SetDefault()
        {
            this.Couple_filename = "";

            this.Simulate_filename = "";
            this.Nwk11_filename = "";
            this.Xns11_filename = "";
            this.Bnd11_filename = "";
            this.Rr11_filename = "";
            this.Hd11_filename = "";
            this.Ad11_filename = "";

            this.M21fm_filename = "";
            this.Mesh_filename = "";
            this.M21caolv_filename = "";

            this.Hdres11_filename = "";
            this.Hd_addres11_filename = "";
            this.Rrres11_filename = "";
            this.Adres11_filename = "";
            this.XAJres_filename = "";
            this.Hdtxt_filename = "";
            this.Dfsu1_gc_filename = "";
            this.Dfsu2_tj_filenane = "";
            this.Progressinfo_filename = "";
        }

        //设置默认属性 -- 根据模型名(模拟方案名)自动构建各模型文件所在路径
        public void Create_ModelFilePath(string modelname)
        {
            //先判断是否存在online_models的文件夹
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance;
            if (!Directory.Exists(dirname))
            {
                Directory.CreateDirectory(dirname);
            }

            //新建该模型方案的文件夹
            DirectoryInfo modeldirinfo = Directory.CreateDirectory(dirname + @"\" + modelname);
            this.Couple_filename = modeldirinfo.FullName + @"\ouhe.couple";

            this.Simulate_filename = modeldirinfo.FullName + @"\simulate11.sim11";
            this.Nwk11_filename = modeldirinfo.FullName + @"\hd.nwk11";
            this.Xns11_filename = modeldirinfo.FullName + @"\hddm.xns11";
            this.Bnd11_filename = modeldirinfo.FullName + @"\bjtj.bnd11";
            this.Rr11_filename = modeldirinfo.FullName + @"\rainfall.rr11";
            this.Hd11_filename = modeldirinfo.FullName + @"\cs.hd11";
            this.Ad11_filename = modeldirinfo.FullName + @"\cs.ad11";

            this.M21fm_filename = modeldirinfo.FullName + @"\simulate21.m21fm";
            this.Mesh_filename = modeldirinfo.FullName + @"\dixing.mesh";
            this.M21caolv_filename = modeldirinfo.FullName + @"\caolv.dfsu";

            //新建结果文件夹
            DirectoryInfo modelresultdirinfo = Directory.CreateDirectory(modeldirinfo.FullName + @"\" + "results");
            this.Hdres11_filename = modelresultdirinfo.FullName + @"\hd_result.res1d";
            this.Hd_addres11_filename = modelresultdirinfo.FullName + @"\hd_result.ADDOUT.res1d";
           
            this.Rrres11_filename = modelresultdirinfo.FullName + @"\rr_result.res1d";
            this.Adres11_filename = modelresultdirinfo.FullName + @"\ad_result.res1d";
            this.XAJres_filename = modelresultdirinfo.FullName + @"\XAJ_result.dfs0";
            this.Hdtxt_filename = modelresultdirinfo.FullName + @"\jg.txt";

            this.Dfsu1_gc_filename = modelresultdirinfo.FullName + @"\m21_gc.dfsu";
            this.Dfsu2_tj_filenane = modelresultdirinfo.FullName + @"\m21_tj.dfsu";

            this.Progressinfo_filename = modelresultdirinfo.FullName + @"\progress.txt";
        }

        //获取项目文件夹路径
        public static string Get_Itemdir()
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance;
            return dirname;
        }

        //获取项目文件夹路径
        public static string Get_Itemdir(string model_instance)
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance;
            return dirname;
        }

        //获取项目文件夹路径
        public static string Get_Item_DefaultModel_Dir(string model_instance)
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + Model_Const.DEFAULT_MODELNAME;
            return dirname;
        }

        //获取模型文件夹路径
        public static string Get_Modeldir(string plan_code)
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance + @"\" + plan_code;
            return dirname;
        }

        //获取模型结果文件夹路径
        public static string Get_ModelResdir(string plan_code,string model_instance = "")
        {
            if(model_instance== "") model_instance = Mysql_GlobalVar.now_instance;
            string model_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + plan_code;
            string res_dir = File_Common.Get_SubDir(model_dir, ".dfsu");
            if (res_dir == "") res_dir = File_Common.Get_SubDir(model_dir, ".res1d");
            if (res_dir == "") res_dir = File_Common.Get_SubDir(model_dir, ".res11");
            string result_direct = model_dir + @"\" + "results";
            if (res_dir == "")
            {
                if (Directory.Exists(result_direct)) res_dir = result_direct;
            }
            return res_dir;
        }
       
        #endregion

    }

    //模型全局参数
    [Serializable]
    public class Model_GlobalPars
    {
        #region 构造函数
        public Model_GlobalPars()
        {
            SetDefault();
        }

        //模拟时间
        public Model_GlobalPars(SimulateTime simulate_time)
        {
            SetDefault();
            this.Simulate_time = simulate_time;
        }

        //模拟时间，模型选择，洪水类型，模拟时间步长
        public Model_GlobalPars(SimulateTime simulate_time, CalculateModel Select_model, SimTimeStep Simulate_timestep, string corodinate,int ahead_hours)
        {
            SetDefault();
            this.Simulate_time = simulate_time;
            this.Select_model = Select_model;
            this.Simulate_timestep = Simulate_timestep;
            this.Coordinate_type = corodinate;
            this.Ahead_hours = ahead_hours;
        }
        #endregion

        #region 属性
        //计算模型选择
        public CalculateModel Select_model { get; set; }

        //洪水类型 -- 历史洪水模拟、设计洪水模拟、实时预报洪水模拟
        public SimulateFloodType Simulate_floodtype { get; set; }

        //模型起止时间
        public SimulateTime Simulate_time { get; set; }

        //提取模拟小时数
        public int Ahead_hours { get; set; }

        //模拟时间步长(一、二维统一)
        public SimTimeStep Simulate_timestep { get; set; }

        //坐标投影
        public string Coordinate_type { get; set; }
        #endregion

        #region 方法
        public void SetDefault()
        {
            //默认模型选择为一维水动力学模型
            this.Select_model = CalculateModel.only_hd;

            //默认洪水类型为实时预报洪水
            this.Simulate_floodtype = SimulateFloodType.Realtime_Forecast;

            //默认模拟时间为当前小时至后2天，总共2天
            SimulateTime simulate_datetime;
            simulate_datetime.Begin = SimulateTime.Get_NowTime();
            simulate_datetime.End = simulate_datetime.Begin.AddDays(2);
            this.Simulate_time = simulate_datetime;

            //默认提前小时数
            this.Ahead_hours = 0;

            //默认模拟步长为30秒
            this.Simulate_timestep = SimTimeStep.step_30;

            this.Coordinate_type = Model_Const.DEFAULT_COOR;
        }
        #endregion

    }

    //产汇流参数
    [Serializable]
    public class RF_Pars
    {
        #region 构造函数
        public RF_Pars()
        {
            SetDefault();
        }

        //设计洪水构造函数
        public RF_Pars(DesignRF_Info Designrf)
        {
            SetDefault();
            this.Designrf_info = Designrf;
        }

        //实时预报降雨构造函数
        public RF_Pars(double future_rf, TimeSpan future_timespan)
        {
            SetDefault();
            this.Forecast_future_rfaccumulated = future_rf;
            this.Forecast_future_rftimespan = future_timespan;
        }

        #endregion

        #region 属性
        //Rainfall产汇流模型选择
        public RFModelType Rainfall_selectmodel { get; set; }

        //设计降雨信息
        public DesignRF_Info Designrf_info { get; set; }

        //预报未来降雨量
        public double Forecast_future_rfaccumulated { get; set; }

        //预报未来降雨时长
        public TimeSpan Forecast_future_rftimespan { get; set; }

        //预报未来降雨过程模板套用类型
        public forecast_rfmodeltype Forecast_rfmodel_type { get; set; }

        //自定义的未来降雨过程序列
        public Dictionary<DateTime, double> Forecast_typedef_rfmodeldic { get; set; }


        //***** 以下属性为模型数据的集合 *****
        //产汇流流域集合
        public CatchmentList Catchmentlist { get; set; }
        #endregion

        #region 方法
        public void SetDefault()
        {
            //默认 产汇流模型 为NAM模型
            this.Rainfall_selectmodel = RFModelType.NAM;

            //默认设计降雨为5年一遇24小时降雨
            DesignRF_Info defaultdesignrf;
            defaultdesignrf.designrf_level = DesignRFLevel.level_5nian;
            defaultdesignrf.designrf_timespan = DesignRF_TimeSpan.level_1day;
            this.Designrf_info = defaultdesignrf;

            //默认预报未来降雨量和时长、过程模板、自定义未来过程序列
            this.Forecast_future_rfaccumulated = 0;                             //预报未来降雨量
            this.Forecast_future_rftimespan = new TimeSpan(24, 0, 0);              //预报未来降雨时长24小时
            this.Forecast_rfmodel_type = forecast_rfmodeltype.default_model;     //预报未来降雨过程模板套用类型

            //默认标准24小时模板和自定义的模板过程
            Dictionary<DateTime, double> forecast_typedefdic = new Dictionary<DateTime, double>();
            this.Forecast_typedef_rfmodeldic = forecast_typedefdic;            //自定义的未来降雨过程序列初始化

            //产汇流流域数据
            CatchmentList Catchmentlist = new CatchmentList();
            this.Catchmentlist = Catchmentlist;
        }
        #endregion
    }

    //一维参数
    [Serializable]
    public class Mike11_Pars
    {
        #region 构造函数
        public Mike11_Pars()
        {
            SetDefault();
        }
        #endregion

        #region 属性
        //水位安全超高，用于计算保证水位，默认超过保证水位就溃堤
        public double Protectheight_leveltodd { get; set; }

        //mike11结果保存模拟时间步倍数
        public SaveTimeStep Mike11_savetimestep { get; set; }

        //水动力学初始条件热启动设置
        public Hotstart Mike11_HD_Hotstart { get; set; }

        //AD水质初始条件热启动设置
        public Hotstart Mike11_AD_Hotstart { get; set; }

        //竖直初始条件是否采用热启动

        //***** 以下属性为模型数据的集合 *****
        //一维流域连接集合
        public Catchment_ConnectList Catchment_Connectlist { get; set; }

        //一维河道集合
        public ReachList ReachList { get; set; }

        //一维断面集合
        public SectionList SectionList { get; set; }

        //一维可控建筑物集合
        public ControlList ControlstrList { get; set; }

        //一维边界条件集合
        public BoundaryList BoundaryList { get; set; }

        //一维水动力学参数集合
        public Hd11_ParametersList Hd_Pars { get; set; }

        //一维水质（AD）参数集合
        public Ad11_ParametersList Ad_Pars { get; set; }
        #endregion

        #region 方法
        public void SetDefault()
        {
            //注意：一些参数的元素如果是对象的集合，则必须初始化，否则建立全新模型时会出错
            this.Protectheight_leveltodd = 0.5;  //超高  

            this.Mike11_savetimestep = SaveTimeStep.step_30;
            this.Mike11_HD_Hotstart = Hotstart.Get_Default_Hotstart();
            this.Mike11_AD_Hotstart = Hotstart.Get_Default_Hotstart();

            Catchment_ConnectList Catchment_Connectlist = new Catchment_ConnectList();
            Catchment_Connectlist.CatchmentConnect_Infolist = new List<Catchment_Connectinfo>();
            this.Catchment_Connectlist = Catchment_Connectlist;

            ReachList ReachList = new ReachList();
            ReachList.Reach_infolist = new List<ReachInfo>();
            ReachList.Reach_baseinfolist = new List<Reach_Segment>();
            ReachList.Reach_gridchainagelist = new Dictionary<string, List<double>>();
            ReachList.Maxpointnumber = 0;
            this.ReachList = ReachList;

            SectionList SectionList = new SectionList();
            SectionList.ReachSectionList = new Dictionary<AtReach, List<PointXZS>>();
            SectionList.AddStorageAreaList = new Dictionary<AtReach, Dictionary<double, double>>();
            SectionList.Reach_LR_Highflow = new Dictionary<string, double>();
            this.SectionList = SectionList;

            ControlList ControlstrList = new ControlList();
            ControlstrList.Default_GateList = new List<Controlstr>();
            ControlstrList.Default_GateNameList = new List<string>();
            ControlstrList.Gatebaseinfo = new Dictionary<string, int>();
            ControlstrList.GateListInfo = new List<Controlstr>();
            ControlstrList.NewAdd_GateList = new List<Controlstr>();
            this.ControlstrList = ControlstrList;

            BoundaryList BoundaryList = new BoundaryList();
            BoundaryList.Boundary_infolist = new List<Reach_Bd>();
            this.BoundaryList = BoundaryList;

            Hd11_ParametersList Hd_Pars = new Hd11_ParametersList();
            Hd_Pars.Bed_Resist = new Dictionary<Reach_Segment, double>();
            Hd_Pars.InitialWater = new Dictionary<AtReach, double>();
            Hd_Pars.Reach_OutputGrid = new List<Reach_Segment>();
            Hd_Pars.Global_Resist = 0.033;
            this.Hd_Pars = Hd_Pars;

            Ad11_ParametersList Ad_Pars = new Ad11_ParametersList();
            Ad_Pars.Component_list = new List<Component>();
            Ad_Pars.ComponetInit_list = new List<Component_Init>();
            Ad_Pars.Decay_list = new List<Decay>();
            Ad_Pars.Dispersion = Dispersion.Get_Dispersion();
            this.Ad_Pars = Ad_Pars;

        }
        #endregion

    }

    //二维参数
    [Serializable]
    public class Mike21_Pars
    {
        #region 构造函数
        public Mike21_Pars()
        {
            SetDefault();
        }
        #endregion

        #region 属性
        //mike21结果保存模拟时间步倍数
        public SaveTimeStep Mike21_savetimestepbs { get; set; }

        //干湿边界
        public Dry_Wet Dry_wet { get; set; }

        //糙率设置
        public Bed_Resistance Resistance_set { get; set; }

        //二维里的降雨设置
        public Pre_Eva Pre_set { get; set; }

        //二维里的蒸发设置
        public Pre_Eva Eva_set { get; set; }

        //二维里的初始条件设置
        public Initial_Contition Initial_set { get; set; }

        //***** 以下属性为模型数据的集合 *****
        //二维堤防集合
        public DikeList DikeList { get; set; }

        //二维堰（涵洞）集合
        public WeirList WeirList { get; set; }

        //二维网格参数集合,含网格边长
        public MeshPars MeshParsList { get; set; }
        #endregion

        #region 方法
        public void SetDefault()
        {
            //注意：一些参数的元素如果是对象的集合，则必须初始化，否则建立全新模型时会出错
            this.Mike21_savetimestepbs = SaveTimeStep.step_60;

            Dry_Wet Dry_wet;
            Dry_wet.dry = 0.005;
            Dry_wet.flood = 0.05;
            Dry_wet.wet = 0.1;
            this.Dry_wet = Dry_wet;

            Bed_Resistance resistance = Bed_Resistance.Get_Default_Resistance();
            this.Resistance_set = resistance;

            Pre_Eva Pre_set;
            Pre_set.type_pre_eva = Type_Pre_Eva.NoPreEva;
            Pre_set.format_pre_eva = Format_Pre_Eva.constant;
            Pre_set.constantEvaporation = 0;
            Pre_set.fileName = "";
            Pre_set.itemName = "";
            this.Pre_set = Pre_set;

            this.Eva_set = Pre_set;

            Initial_Contition Initial_set;
            Initial_set.initial_type = 1;
            Initial_set.const_surface_elevation = 0;
            Initial_set.fileName = "";
            Initial_set.itemName = "";
            this.Initial_set = Initial_set;

            DikeList DikeList = new DikeList();
            DikeList.Dike_List = new List<Dike>();
            DikeList.MaxDikeId = 0;
            this.DikeList = DikeList;

            WeirList WeirList = new WeirList();
            WeirList.Weir_List = new List<Weir>();
            WeirList.MaxWeirId = 0;
            this.WeirList = WeirList;

            MeshPars MeshParsList = new MeshPars();
            MeshParsList.Mesh_Sidelength = Model_Const.MESHSIDE_LENGTH;
            this.MeshParsList = MeshParsList;
        }

        #endregion
    }


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

        //如果业务模型包含二维，则请求二维模型服务参数设置和计算服务
        public static void Request_Mike21_Server(HydroModel model, List<string> instance_list,string model_instance,string business_code)
        {
            try
            {
                //同步请求二维模型来修改边界条件(二维模型会请求一维模型特定结果来修改其边界条件)
                string http_url = Mysql_GlobalVar.mike21_serverurl;
                string changebnd_url = http_url + "?request_type=change_reachflood&request_pars=[\"" + model.Modelname + "\",\"" + model.Modelname + "\"]";
                File_Common.Get_HttpReSponse(changebnd_url);
                Thread.Sleep(2000);

                //如果是溃堤模型，则请求二维设置溃口
                if (business_code == "embank_break_gq" || business_code == "embank_break_wh")
                {
                    string post_request_pars = Get_Modify_ReachBreak_RequestPars(model, model_instance);
                    if (post_request_pars != null) File_Common.Post_HttpReSponse(http_url, post_request_pars);
                }

                //如果是闸门故障模型，则请求二维设置故障闸门
                if (business_code.Contains("fault"))
                {
                    string post_request_pars = Get_Modify_FaultGate_RequestPars(model,model_instance);
                    if (post_request_pars != null) File_Common.Post_HttpReSponse(http_url, post_request_pars);
                }

                //请求修改初始水情
                //Dictionary<string, object> post_request_pars = Get_Modify_InitialLevel_RequestPars(model, instance_list);
                //if (post_request_pars != null) File_Common.Post_HttpReSponse(http_url, File_Common.Serializer_Obj(post_request_pars));

                //请求二维模型开始计算
                Thread.Sleep(2000);
                string run_url = http_url + "?request_type=run_model&request_pars=" + model.Modelname;
                File_Common.Get_HttpReSponse(run_url);
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                throw;
            }
        }

        //获取二维设置故障闸门设置请求参数
        private static string Get_Modify_FaultGate_RequestPars(HydroModel model, string model_instance)
        {
            //组合成二维需要的对象
            List<GateNow> gate_state_list = new List<GateNow>();

            //获取建筑物各闸门基础信息
            string mike21_modelinstance = HydroModel.Get_Mike21_ModelInstance(model.Modelname);
            Dictionary<string, object> fault_baseinfo = FaultGate_BaseInfo.Read_StrGate_Info(mike21_modelinstance);
            List<FaultGate_BaseInfo> gate_info = fault_baseinfo["gate_info"] as List<FaultGate_BaseInfo>;

            //获取闸门故障设置信息
            string fault_info_desc = FaultGate_BaseInfo.Get_FaultGate_SetInfo(model.Modelname, model_instance);
            Dictionary<string, object> fault_info = JsonConvert.DeserializeObject<Dictionary<string, object>>(fault_info_desc);
            List<object[]> gate_h_list = JsonConvert.DeserializeObject<List<object[]>>(fault_info["gate_h"].ToString());

            //各个闸门信息
            for (int i = 0; i < gate_info.Count; i++)
            {
                string open_state = "半开";
                double open_h = double.Parse(gate_h_list[i][2].ToString());
                if (open_h == gate_info[i].gate_h) open_state = "全开";
                if (open_h == 0) open_state = "全关";

                GateNow gate_state = new GateNow(gate_info[i].gate_code, gate_info[i].gate_name,
                    gate_info[i].gate_h, gate_info[i].gate_bottom, open_h, open_state);

                gate_state_list.Add(gate_state);
            }

            //组合二维模型要设置故障闸门的信息
            List<object> request_pars = new List<object>();
            request_pars.Add(model.Modelname);
            request_pars.Add(gate_state_list);
            Dictionary<string, object> post_request_pars = new Dictionary<string, object>();
            post_request_pars.Add("request_type", "set_fault_gate");
            post_request_pars.Add("request_pars", request_pars);

            string post_string = File_Common.Serializer_Obj(post_request_pars);
            return post_string;
        }

        //获取二维模型设置溃口请求参数
        private static string Get_Modify_ReachBreak_RequestPars(HydroModel model,string model_instance)
        {
            //从数据库获取一维模型的溃口信息
            string break_info = Reach_Break_BaseInfo.Get_Reach_BreakInfo(model.Modelname, model_instance);
            if (break_info == null || break_info == "") return null;
            Reach_Break_BaseInfo break_baseinfo = JsonConvert.DeserializeObject<Reach_Break_BaseInfo>(break_info);
            string break_reachname = "SSS_BRANCH_" + model.Mike11Pars.ControlstrList.GateListInfo.Last().Strname + "_GZDU";
            AtReach atreach = AtReach.Get_Atreach(break_reachname,50);
            Dictionary<DateTime, double> dischage_dic = Res11.Res11reader(model.Modelname, atreach, mike11_restype.Discharge);
            DateTime fh_starttime = dischage_dic.FirstOrDefault(x => x.Value > 1.0).Key;
            DateTime fh_endtime = fh_starttime.AddMinutes(break_baseinfo.fh_minutes);
            break_baseinfo.fh_start_time = SimulateTime.TimetoStr(fh_starttime);
            break_baseinfo.fh_finish_time = SimulateTime.TimetoStr(fh_endtime);

            //更新模型参数数据库中的溃口设置信息
            Reach_Break_BaseInfo.Update_Reach_BreakInfo(model.Modelname, model_instance, break_baseinfo);

            //组合成二维需要的对象
            Mike21_Reach_BreakInfo mike21_breakinfo = new Mike21_Reach_BreakInfo(break_baseinfo.location, break_baseinfo.fh_width, break_baseinfo.fh_datumn,
                break_baseinfo.fh_start_time, break_baseinfo.fh_finish_time);

            //组合二维模型要修改溃口的信息
            List<object> request_pars = new List<object>();
            request_pars.Add(model.Modelname);
            request_pars.Add(mike21_breakinfo);
            Dictionary<string, object> post_request_pars = new Dictionary<string, object>();
            post_request_pars.Add("request_type", "change_reach_break");
            post_request_pars.Add("request_pars", request_pars);

            string post_string = File_Common.Serializer_Obj(post_request_pars);
            return post_string;
        }

        //请求二维模型来修改初始条件(请求参数包含水库水位)
        private static Dictionary<string, object> Get_Modify_InitialLevel_RequestPars(HydroModel model, List<string> instance_list)
        {
            //从数据库获取一维模型的初始水情条件
            string intial_water = Item_Info.Get_Model_SingleParInfo(model.Modelname, "mike11_initial_info"); 
            if (intial_water == null || intial_water == "") return null;
            Dictionary<string, List<List<object>>> intial_water_obj = JsonConvert.DeserializeObject<Dictionary<string, List<List<object>>>>(intial_water);

            //获取松耦合的二维模型实例
            List<string> mike21_instancs = new List<string>();
            for (int i = 0; i < instance_list.Count; i++)
            {
                if (instance_list[i].Contains("mike21")) mike21_instancs.Add(instance_list[i]);
            }

            //从数据库获取该模型实例的初始水位设置区域信息(可有多个) 
            string mike21_instance = mike21_instancs[0];  //后期再考虑多个二维模型松耦合的情况
            string changebnd_url = Mysql_GlobalVar.mike21_serverurl + "?request_type=get_initial_regionname&request_pars=" + mike21_instance;
            string mike21_initialregion_names = File_Common.Get_HttpReSponse(changebnd_url);
            Dictionary<string,string> region_names = JsonConvert.DeserializeObject<Dictionary<string, string>>(mike21_initialregion_names);

            //组合二维模型要修改初始水情的信息
            List<object> request_pars = new List<object>();
            List<Dictionary<string, object>> initial_levels = new List<Dictionary<string, object>>();
            for (int i = 0; i < region_names.Count; i++)
            {
                string resion_id = region_names.ElementAt(i).Key;
                string resion_name = region_names.ElementAt(i).Value;
                Dictionary<string, object> initial_level = new Dictionary<string, object>();

                for (int j = 0; j < intial_water_obj.Count; j++)
                {
                    List<List<object>> intial_water_array = intial_water_obj.ElementAt(j).Value;
                    for (int k = 0; k < intial_water_array.Count; k++)
                    {
                        if(intial_water_array[k][1].ToString() == resion_name)
                        {
                            double level = Double.Parse(intial_water_array[k][2].ToString());
                            initial_level.Add("res_id", resion_id);
                            initial_level.Add("res_name", resion_name);
                            initial_level.Add("initial_level_name", "指定水位: " + intial_water_array[k][2].ToString());
                            initial_level.Add("initial_level", level);

                            initial_levels.Add(initial_level);
                            break;
                        }
                    }
                }
            }
            request_pars.Add(model.Modelname);
            request_pars.Add(initial_levels);

            Dictionary<string, object> post_request_pars = new Dictionary<string, object>();
            post_request_pars.Add("request_type", "change_res_initiallevel");
            post_request_pars.Add("request_pars", request_pars);

            return post_request_pars;
        }

        //从模型方案表里 更新模型状态和开始计算时间、需要花费的计算时间
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

        //加载模型 -- 根据模型名从数据库中获取(静态类)(可用模型名或方案名)
        private static HydroModel Load_Model(string plan_code)
        {
            //产汇流的一些静态属性要重新初始化
            //if (CatchmentList.Stand24_Rfmodel == null )
            //{
            //    RR11.GetDefault_AverageRF();             //流域各月平均降雨
            //    RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
            //    RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
            //}

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            
            //先判断该模型在数据库中是否存在
            string sql = "select model_object from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", plan_code);
                return null;
            }

            //流域信息最后从数据库中读取，并给模型流域参数赋值
            byte[] blob = Mysql.Get_Blob(sql);
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            HydroModel model = binFormat.Deserialize(ms) as HydroModel;
            ms.Close();
            ms.Dispose();

            //产汇流的一些静态属性要重新初始化
            if (model.RfPars.Catchmentlist.Catchment_infolist != null &&
                CatchmentList.Stand24_Rfmodel == null &&
                (model.ModelGlobalPars.Select_model == CalculateModel.only_rr ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_and_hd ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood))
            {
                RR11.GetDefault_AverageRF();             //流域各月平均降雨
                RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
                RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
            }

            //如果是一维模型且断面数为空，且非默认模型，则从全局中获取当前项目的断面
            if (model.ModelGlobalPars.Select_model.ToString().Contains("hd") && 
                model.Mike11Pars.SectionList == null && plan_code != Model_Const.DEFAULT_MODELNAME)
            {
                SectionList nowitem_sectiondata = Item_Info.Get_Item_SectionDatas(Mysql_GlobalVar.now_instance);
                model.Mike11Pars.SectionList = nowitem_sectiondata;
            }

            return model;
        }


        //加载该用户的所有模型 -- (静态类)
        public static List<HydroModel> Load_AllModel()
        {
            List<HydroModel> model_list = new List<HydroModel>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select * from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("模型实例：{0}没有模型!", Mysql_GlobalVar.now_instance);
                return null;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string plan_code = dt.Rows[i][2].ToString();
                sql = "select model_object from " + tableName + " where plan_code='" + plan_code + "' and user = '" + Mysql_GlobalVar.now_modeldb_user + "'";

                byte[] blob = Mysql.Get_Blob(sql);

                MemoryStream ms = new MemoryStream(blob);
                BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器

                ms.Position = 0;
                ms.Seek(0, SeekOrigin.Begin);

                HydroModel model = binFormat.Deserialize(ms) as HydroModel;

                ms.Close();
                ms.Dispose();

                model_list.Add(model);
            }

            
            return model_list;
        }

        //查询该实例所有模型方案信息 -- (静态类)
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

        //用户加载默认模型 -- 根据模型名从数据库中获取(静态类)
        public static HydroModel Get_DefaultModel_AndCopytoself_FromDB()
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select model_object from " + tableName + " where plan_code='" + Model_Const.DEFAULT_MODELNAME + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", Model_Const.DEFAULT_MODELNAME);
                return null;
            }

            //流域信息最后从数据库中读取，并给模型流域参数赋值
            byte[] blob = Mysql.Get_Blob(sql);
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            HydroModel model = binFormat.Deserialize(ms) as HydroModel;
            ms.Close();
            ms.Dispose();

            //修改模型文件路径
            HydroModel.Modify_ModelFilePath(ref model, Mysql_GlobalVar.now_instance);

            //产汇流的一些静态属性要重新初始化
            if (model.RfPars.Catchmentlist.Catchment_infolist != null &&
                CatchmentList.Stand24_Rfmodel == null &&
                (model.ModelGlobalPars.Select_model == CalculateModel.only_rr ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_and_hd ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood))
            {
                RR11.GetDefault_AverageRF();             //流域各月平均降雨
                RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
                RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
            }

            //上传模型
            model.Save();

            //将默认模型文件拷贝一份到现基础模型实例文件夹下
            string source_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Model_Const.DEFAULT_MODELNAME;
            string dest_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance + @"\" + Model_Const.DEFAULT_MODELNAME;
            if (!Directory.Exists(dest_dir))
            {
                Directory.CreateDirectory(dest_dir);
                File_Common.Copy_Directory_Allfile(source_dir, dest_dir);
            }

            //更新默认模型的河网文件，只保留规则调度
            //Nwk11.Rewrite_Nkw11_UpdateFile(model);

            return model;
        }

        //从文件中获取默认模型后保存
        public static void Load_DefaultModel_ToDB()
        {
            //从默认模型文件中获取模型
            HydroModel model = HydroModel.Get_Default_Model();

            //设置当前项目的结果保存时间
            Set_NowItem_SaveMinutes(model);

            //保存至数据库
            HydroModel.Save_ModelObject(model);
        }

        public static void Set_NowItem_SaveMinutes(HydroModel default_model)
        {
            if (default_model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood)
            {
                //藕合模型也用二维模型
                Model_Const.Now_Model_SaveTime = (int)default_model.Mike21Pars.Mike21_savetimestepbs * (int)default_model.ModelGlobalPars.Simulate_timestep;
            }
            else if (default_model.ModelGlobalPars.Select_model == CalculateModel.only_m21 || default_model.ModelGlobalPars.Select_model == CalculateModel.ad_hd_m21)
            {
                //二维模型
                Model_Const.Now_Model_SaveTime = (int)default_model.Mike21Pars.Mike21_savetimestepbs * (int)default_model.ModelGlobalPars.Simulate_timestep;
            }
            else if (default_model.ModelGlobalPars.Select_model == CalculateModel.only_hd || default_model.ModelGlobalPars.Select_model == CalculateModel.ad_and_hd)
            {
                //一维模型(注意，需要保存的是分钟)
                Model_Const.Now_Model_SaveTime = (int)default_model.Mike11Pars.Mike11_savetimestep;
            }
            else
            {
                //产汇流模型
                Model_Const.Now_Model_SaveTime = 60;
            }
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
            modelfiles.Couple_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Couple_filename);

            modelfiles.Simulate_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Simulate_filename);
            modelfiles.Nwk11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Nwk11_filename);
            modelfiles.Xns11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Xns11_filename);
            modelfiles.Bnd11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Bnd11_filename);
            modelfiles.Rr11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Rr11_filename);
            modelfiles.Hd11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Hd11_filename);
            modelfiles.Ad11_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Ad11_filename);

            modelfiles.M21fm_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.M21fm_filename);
            modelfiles.Mesh_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.Mesh_filename);
            modelfiles.M21caolv_filename = model_dirname + @"\" + Path.GetFileName(modelfiles.M21caolv_filename);

            //新建结果文件夹
            string model_result_dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + model.Modelname + @"\" + "results";
            modelfiles.Hdres11_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Hdres11_filename);
            modelfiles.Rrres11_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Rrres11_filename);
            modelfiles.XAJres_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.XAJres_filename);
            modelfiles.Hdtxt_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Hdtxt_filename);

            modelfiles.Dfsu1_gc_filename = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Dfsu1_gc_filename);
            modelfiles.Dfsu2_tj_filenane = model_result_dirname + @"\" + Path.GetFileName(modelfiles.Dfsu2_tj_filenane);

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
        #endregion
    }



    //类的后半部分 -- 各种操作方法
    public partial class HydroModel
    {
        #region 用户新建、保存、删除、加载、传递、下载模型等操作

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


        #region RR流域操作 -- 新增流域和其产汇流模型，改变各流域产汇流模型类型、修改模型参数
        // 新增流域和其产汇流模型 -- 模型参数采用默认值 
        public void Add_NewCatchment(string CatchmentName, List<PointXY> catchment_pointlist, RFModelType select_model)
        {
            HydroModel hydromodel = this;
            RR11.Add_NewCatchment(ref hydromodel, CatchmentName, catchment_pointlist, select_model);
        }

        //改变所有流域采用的产汇流模型类型
        public void Change_AllCatchment_RFmodel(RFModelType now_rfmodeltype)
        {
            HydroModel hydromodel = this;
            RR11.Change_AllCatchment_RFmodel(ref  hydromodel, now_rfmodeltype);
        }

        // 修改指定流域的 NAM模型初始条件
        public void Modify_NAM_Initial(string catchmentname, Nam_InitialCondition new_initial)
        {
            HydroModel hydromodel = this;
            RR11.Modify_Nam_Initial(ref hydromodel, catchmentname, new_initial);
        }

        // 修改指定流域 NAM模型的参数
        public void Modify_NAM_Parameter(string catchmentname, NAMparameters new_nampara)
        {
            HydroModel hydromodel = this;
            RR11.Modify_NAM_Parameter(ref hydromodel, catchmentname, new_nampara);
        }

        // 修改指定流域 UHM模型的参数
        public void Modify_UHM_Parameter(string catchmentname, UHMparameters uhmpara)
        {
            HydroModel hydromodel = this;
            RR11.Modify_UHM_Parameter(ref hydromodel, catchmentname, uhmpara);
        }

        // 修改指定流域 XAJ模型的参数
        public void Modify_XAJ_Parameter(string catchmentname, Xajparameters xajpara)
        {
            HydroModel hydromodel = this;
            RR11.Modify_XAJ_Parameter(ref hydromodel, catchmentname, xajpara);
        }

        // 修改指定流域 XAJ模型的初始条件
        public void Modify_XAJ_Initial(string catchmentname, XajInitialConditional xajInitial)
        {
            HydroModel hydromodel = this;
            RR11.Modify_XAJ_Initial(ref hydromodel, catchmentname, xajInitial);
        }

        //上传流域UHM单位线 -- 如果有则删除原来的重新更新
        public static void Write_UhmGraph_ToDB(string catchmentname, Dictionary<int, double> Catchment_UHMdic)
        {
            RR11.Write_NewUHMGraph_ToDb(catchmentname, Catchment_UHMdic);
        }

        #endregion


        #region 一维建筑物操作 -- 新增 、更新分洪口(溃口)和普通建筑物、泵站
        //新建分洪口 -- 默认调度规则
        public string Add_Fhkstr(PointXY p, double fhk_width)
        {
            HydroModel hydromodel = this;
            FhkstrInfo fhkstrinfo = Nwk11.Get_Default_Fhkstrinfo(hydromodel, p, fhk_width);
            string fhkstrname = Nwk11.Add_Fhkstr(ref hydromodel, ref fhkstrinfo);
            return fhkstrname;
        }

        //新建分洪口 -- 默认调度规则
        public void Add_Fhkstr(ref Reach_Break_BaseInfo break_info)
        {
            HydroModel hydromodel = this;

            //坐标和桩号转换
            PointXY p_jwd = PointXY.Get_Pointxy(break_info.location[0], break_info.location[1]);
            PointXY p_pro = PointXY.CoordTranfrom(p_jwd, 4326, 4547, 3);

            string business_code = HydroModel.Get_BusinessCode_FromDB(hydromodel.Modelname);
            string reachname = "";
            if(business_code == "embank_break_gq")
            {
                reachname = "GQ";
            }
            else if(business_code == "embank_break_wh")
            {
                reachname = "WH";
            }

            AtReach at_reach = reachname==""? Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList,p_pro): Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, reachname, p_pro);

            //删除已有的溃口建筑
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            List<Controlstr> contro_list = controllist.GateListInfo;
            for (int i = 0; i < contro_list.Count; i++)
            {
                string str_name = contro_list[i].Strname;
                if (str_name.Contains("_KB")) controllist.RemoveGate(str_name);
            }

            //确定分洪水位，如果按最高水位分洪，则需要先试算一次
            double fh_level = break_info.break_level;
            if (break_info.break_condition == "max_level") fh_level = Reach_Break_BaseInfo.Get_Section_MaxLevel(hydromodel, at_reach);

            //新增溃堤建筑物
            FhkstrInfo fhk_str = Nwk11.Get_Default_Fhkstrinfo(hydromodel, at_reach, p_pro, break_info.fh_width,fh_level,break_info.fh_minutes * 60);
            break_info.str_name = Nwk11.Add_Fhkstr(ref hydromodel, ref fhk_str);
            break_info.fh_datumn = fhk_str.dm_level;
            break_info.break_level = fh_level;

           Nwk11.Add_Fhkstr(ref hydromodel, ref fhk_str);
        }

        //新建分洪口 -- 默认调度规则，并耦合到二维
        public string Add_Fhkstr_AndLink(PointXY p, double fhk_width)
        {
            HydroModel hydromodel = this;
            FhkstrInfo fhkstrinfo = Nwk11.Get_Default_Fhkstrinfo(hydromodel, p, fhk_width);
            string fhkstrname = Nwk11.Add_Fhkstr(ref hydromodel, ref fhkstrinfo);

            //添加侧向建筑物连接
            Add_SideStructure_Link(fhkstrname);

            return fhkstrname;
        }

        //新建其他普通建筑物 -- 默认调度规则(全开)，正向
        public string Add_Normalstr(string gatename, PointXY p, Necessary_Attrs neces_attrs)
        {
            HydroModel hydromodel = this;
            return Nwk11.Add_Normalstr(ref hydromodel, gatename, p, neces_attrs);
        }

        //新建泵站-- 默认正向，按设计流量全开
        public string Add_Bumpstr_Normal(string gatename, PointXY p, double design_discharge)
        {
            HydroModel hydromodel = this;
            return Nwk11.Add_Bumpstr(ref hydromodel, gatename, p, design_discharge);
        }

        //新建泵站-- 默认侧向，按设计流量全开
        public string Add_Bumpstr_Side(string gatename, PointXY p, double design_discharge)
        {
            HydroModel hydromodel = this;
            string strname = Nwk11.Add_Bumpstr(ref hydromodel, gatename, p, design_discharge);

            //改为侧向
            Normalstr str = this.Mike11Pars.ControlstrList.GetGate(strname) as Normalstr;
            str.Str_Sidetype = Str_SideType.sidestr;

            return strname;
        }

        //因为溃口设置， 从数据库中更新初始视角
        public static void Update_ModelPlan_InitialView(HydroModel model, Reach_Break_BaseInfo break_baseinfo)
        {
            //转换前端经纬度到大地坐标，并找到所在附近河道和桩号
            PointXY p = PointXY.Get_Pointxy(break_baseinfo.location[0], break_baseinfo.location[1]);
            if (p.X < 200)
            {
                p = PointXY.CoordTranfrom(p, 4326, 4547, 3);
            }
            AtReach atreach = Nwk11.Get_NearReach(model.Mike11Pars.ReachList, p);
            int heading;   //需要更新的那个heading角度
            PointXY view_point = model.Mike11Pars.ReachList.Get_ReachPointxy(atreach.reachname, atreach.chainage - 1000, out heading);//从上游500m俯瞰
            if (heading < 0) heading = heading + 360;
            view_point = PointXY.CoordTranfrom(view_point, 4547, 4326, 6);

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //获取最初视角
            string sql = "select view_point from " + tableName + " where plan_code = '" + model.Modelname + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return;
            string view_jsonstr = dt.Rows[0][0].ToString();

            // 解析JSON对象
            JObject jsonObject = JObject.Parse(view_jsonstr);
            JObject cameraObject = (JObject)jsonObject["camera"];

            // 修改x、y和heading属性
            cameraObject["position"]["x"] = view_point.X;
            cameraObject["position"]["y"] = view_point.Y;
            cameraObject["heading"] = heading;
            cameraObject["position"]["spatialReference"]["latestWkid"] = 4326;
            cameraObject["position"]["spatialReference"]["wkid"] = 4326;

            // 将修改后的JSON对象转换回字符串
            string new_view = jsonObject.ToString();

            //更新数据库的污染源信息
            sql = $"UPDATE {tableName} SET view_point=:new_view WHERE plan_code=:planCode";

            KdbndpParameter[] mysqlPara =
            {
                    new KdbndpParameter(":new_view", new_view),
                    new KdbndpParameter(":planCode", model.Modelname)
             };
            Mysql.Execute_Command(sql, mysqlPara);

            Console.WriteLine("视角修改完成！");
        }

        #endregion


        #region  一维建筑物操作 -- 改变建筑物位置、参数
        //改变闸门位置 -- 根据给定的点坐标
        public void Change_GateLocation(string strname, PointXY newloacal_p)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateLocation(ref hydromodel, strname, newloacal_p);
        }

        //改变闸门位置 -- 根据给定河道位置
        public void Change_GateLocation(string strname, AtReach atreach)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateLocation(ref hydromodel, strname, atreach);
        }

        //改变闸门基本属性 -- 根据必须的5个参数(闸门类型、数量、单宽、底高、最大开启高程)
        public void Change_GateAttributes(string strname, Necessary_Attrs new_nes_attributes)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateAttributes(ref hydromodel, strname, new_nes_attributes);
        }

        //改变弧形闸门参数 -- 按弧形闸门高根据规范推算其他参数
        public void Change_GateRadiapars(string strname, double radia_height)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateRadiapars(ref hydromodel, strname, radia_height);
        }

        //改变公式闸门参数 - 根据因子相应扩大和缩小
        public void Change_GateFormulapars(string strname, double factor_a, double factor_b)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateFormulapars(ref hydromodel, strname, factor_a, factor_b);
        }

        //改变分洪口分洪水位 -- 为堤顶高程
        public void Change_FhkWaterLevel_ToMax(string strname)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_FhkWaterLevel_ToMax(ref hydromodel, strname);
        }

        //改变分洪口分洪水位 -- 为指定水位
        public void Change_FhkWaterLevel_ToValue(string strname, double new_waterlevel)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_FhkWaterLevel_ToValue(ref hydromodel, strname, new_waterlevel);
        }

        //改变分洪口溃堤时间 -- 为指定时间
        public void Change_FhkBreakTime(string strname, TimeSpan new_breaktime)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_FhkBreakTime(ref hydromodel, strname, new_breaktime);
        }
        #endregion


        #region 一维建筑物操作 -- 改变建筑物调度规则
        //通过迭代计算，获得所有闸门优化的闸站调度方式，并完整计算提取结果
        public void Start_IterCal()
        {
            HydroModel hydromodel = this;

            WgOptdd.Get_OptimizeDd_CalResult(hydromodel);
        }

        //将全部闸门改为规则调度
        public void Changeddgz_AllToGZDU()
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_AllToGZDU(ref hydromodel);
        }

        //将全部闸门改为全开
        public void Changeddgz_AllToZMDU_FullOpen()
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_AllToZMDU_FullOpen(ref hydromodel);
        }

        //将指定闸门 改为规则调度 -- 闸门类型的全开   
        public void Changeddgz_ToGZDU(string strname)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToGZDU(ref hydromodel, strname);
        }

        //将指定闸门 改为控泄调度 -- 指定流量   
        public void Changeddgz_ToKXDU(string strname, double ddparams_double)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, ddparams_double);
        }

        //将指定闸门 改为控泄调度 -- 以水位流量关系确定流量   
        public void Changeddgz_ToKXDU(string strname, double ddparams_double, List<double[]> str_relation)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, ddparams_double, str_relation);
        }

        //将指定闸门 改为控泄时间调度 -- 指定控泄流量过程
        public void Changeddgz_ToKXDU_TIME(string strname, Dictionary<DateTime, double> dischargedic)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToKXDU_TIME(ref hydromodel, strname, dischargedic);
        }

        //将指定闸门 改为闸门调度 
        public void Changeddgz_ToZMDU(string strname, ZMDUinfo ddparams_zmdu)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        //将指定闸门 改为闸门时间调度 -- 指定闸门开启高度过程
        public void Changeddgz_ToZMDU_TIME(string strname, Dictionary<DateTime, double> gateheight_dic)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToZMDU_TIME(ref hydromodel, strname, gateheight_dic);
        }

        //根据数据库闸站状态，修改所有的建筑物闸站调度方式及数据
        public void Update_AllStr_DdInfo_FromGB()
        {
            HydroModel hydromodel = this;
            Item_Info.Update_AllStr_DdInfo_FromGB(ref hydromodel);

            this.Model_state = Model_State.Ready_Calting;

            //重新保存模型 --保存模型实体和调度信息，但未更新状态
            this.Save();

            //更新模型状态为待计算  --不再保存模型实体
            string[] model_info = Item_Info.Get_Model_Info(this, "", false);
            Update_ModelStateInfo(this, model_info[4],false);

            //单独更新闸站状态信息
            Update_Model_Ddinfo(hydromodel, model_info[6]);
        }

        //对比和更新所有建筑物的调度方式(每个建筑物每次指令调度有4个参数 -- 时间、调度方式、开闸数、调度量)
        public void Update_AllStr_DdInfo(Dictionary<string, List<DdInfo>> new_ddinfos)
        {
            HydroModel hydromodel = this;
            Item_Info.Update_AllStr_DdInfo(ref hydromodel, new_ddinfos);

            this.Model_state = Model_State.Ready_Calting;

            //重新保存模型 --保存模型实体，但未更新状态
            this.Save();

            //更新模型状态信息为待计算
            string[] model_info = Item_Info.Get_Model_Info(this, "", false);

            //更新模型状态为待计算 --不再保存模型实体
            Update_ModelStateInfo(this, model_info[4],false);

            //重新与给定的需要修改的闸门调度信息匹配下
            Match_Gatedd_Infos(ref model_info, new_ddinfos);

            //单独更新调度信息
            Update_Model_Ddinfo(hydromodel, model_info[6]);
        }

        //重新与给定的需要修改的闸门调度信息匹配
        public static void Match_Gatedd_Infos(ref string[] model_info, Dictionary<string, List<DdInfo>> new_ddinfos)
        {
            string gate_ddinfo = model_info[6];
            List<string[]> dd_list = JsonConvert.DeserializeObject<List<string[]>>(gate_ddinfo);

            //重新匹配
            for (int i = 0; i < dd_list.Count; i++)
            {
                string firstString = dd_list[i][0];
                for (int j = 0; j < new_ddinfos.Count; j++)
                {
                    if (firstString == new_ddinfos.ElementAt(j).Key)
                    {
                        List<DdInfo> dd_type = new_ddinfos.ElementAt(j).Value;
                        List<string[]> dd_info = new List<string[]>();
                        for (int k = 0; k < dd_type.Count; k++)
                        {
                            string[] gate_dd = new string[] { dd_type[k].dd_time, dd_type[k].dd_type, dd_type[k].open_n.ToString(), dd_type[k].dd_value.ToString() };
                            dd_info.Add(gate_dd);
                        }

                        string dd_str = File_Common.Serializer_Obj(dd_info);
                        dd_list[i][5] = dd_str;
                    }
                }
            }

            model_info[6] = JsonConvert.SerializeObject(dd_list);
        }

        //对比和更新单个建筑物的调度方式(3个参数 -- 英文名、调度方式、调度量)
        public void Update_Str_DdInfo(string[] new_ddinfo)
        {
            HydroModel hydromodel = this;
            Item_Info.Update_Str_DdInfo(ref hydromodel, new_ddinfo);

            //重新保存模型
            this.Save();

            //更新模型状态信息为待计算
            this.Model_state = Model_State.Ready_Calting;
            string[] model_info = Item_Info.Get_Model_Info(this, "", false);
            Update_ModelStateInfo(this, model_info[4]);
        }

        //判断采用哪一种闸门时间调度方式(开度过程\还是闸门数过程\还是两者都是)
        public void Changeddgz_ToZMDU_TIME_Select(string strname, Dictionary<DateTime, double> gateheight_dic, Dictionary<DateTime, double> gatenumber_dic)
        {
            HydroModel hydromodel = this;
            Nwk11.ZMDU_TIME_Select(ref hydromodel, strname, gateheight_dic, gatenumber_dic);
        }

        //判断各闸门调度的变化，修改变化闸门的调度方式
        public void Update_GateDd(string strname, ZMDUinfo ddparams_zmdu)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        #endregion


        #region 一维河道操作 -- 增加新河道、增加减少河道和产汇流的连接
        //增加一条新河道 -- 根据河名和河道控制点
        public void Add_NewReach(string newreachname, List<PointXY> reachpoints)
        {
            HydroModel hydromodel = this;

            //第1步 -- 增加新河道，更新河道集合
            Nwk11.Add_NewReach_Network(ref hydromodel, ref newreachname, ref reachpoints);

            //第2步 -- 调用GIS服务剖切断面，更新河道断面集合和数据库
            Xns11.Add_NewReach_Section(ref hydromodel, newreachname);

            //第3步 -- 判定连接关系，生成边界条件，更新边界条件集合
            Bnd11.Add_NewReach_Bnd(ref hydromodel, newreachname);
        }

        //新增RR产汇流流域连接 - 不分产汇流模型类型
        public void Add_CatchmentConnect(string catchmentname, Reach_Segment connect_reachseg)
        {
            HydroModel hydromodel = this;
            Nwk11.Add_CatchmentConnect(ref hydromodel, catchmentname, connect_reachseg);
        }

        //断开指定RR产汇流流域连接 - 不分产汇流模型类型
        public void Substract_catchment_conncet(string catchmentname)
        {
            HydroModel hydromodel = this;
            Nwk11.Substract_catchmentconncet(ref hydromodel, catchmentname);
        }

        //断开所有RR产汇流流域连接 - 不分产汇流模型类型
        public void Substract_allcatchment_conncet()
        {
            HydroModel hydromodel = this;
            int n = hydromodel.Mike11Pars.Catchment_Connectlist.CatchmentConnect_Infolist.Count;
            for (int i = 0; i < n; i++)
            {
                string catchmentname = hydromodel.Mike11Pars.Catchment_Connectlist.CatchmentConnect_Infolist[0].catchment_name;
                Nwk11.Substract_catchmentconncet(ref hydromodel, catchmentname);
            }
        }
        #endregion


        #region 一维断面操作 -- 更新某河段断面,断面数据获取（一般可通过sectionlist类的成员方法获取）
        //更新已有河段 断面数据 -- 原桩号保持不变，重新从DEM上剖切,该操作不会改变河网、边界等数据
        public void Update_ReachSection_OldeSecChainage(Reach_Segment reach_segment)
        {
            HydroModel hydromodel = this;
            Xns11.Update_ReachSection_FromDEM(ref hydromodel, reach_segment);
        }

        //更新已有河段 断面数据 -- 重新读入新的断面桩号和数据
        public void Update_ReachSection_NewSecChainage(Reach_Segment reachsegment, ref Dictionary<AtReach, List<PointXZS>> reachsectiondata)
        {
            HydroModel hydromodel = this;
            Xns11.Update_ReachSection(ref hydromodel, reachsegment, ref  reachsectiondata);
        }
        #endregion


        #region 一维断面与水库连接操作 -- 将水库库容挂在断面上以发挥水库的调蓄作用
        //将水库与一维河道连接 -- 1个连接点,substract_sectionvolume(是否核减断面占用库容)
        public void Connect_Reservoir_OnSection(Dictionary<double, double> Level_Volume, PointXY p, bool substract_sectionvolume)
        {
            HydroModel hydromodel = this;
            Xns11.Connect_Reservoir_OnSection(hydromodel, Level_Volume, p, substract_sectionvolume);
        }

        //将水库与一维河道连接 -- 2个连接点
        public void Connect_Reservoir_OnSection(Dictionary<double, double> Level_Volume, PointXY p1, PointXY p2, bool substract_sectionvolume)
        {
            HydroModel hydromodel = this;
            Xns11.Connect_Reservoir_OnSection(hydromodel, Level_Volume, p1, p2, substract_sectionvolume);
        }
        #endregion


        #region 一维边界条件操作 -- 修改边界条件，新增旁侧入流
        //请求NAM产汇流模型计算的各子流域洪水流量过程
        public Dictionary<string, Dictionary<DateTime, double>> Modify_BndID_ToDischargeDic_FromNAM()
        {
            HydroModel hydromodel = this;

            //通过网络请求降雨过程
            string request_rain_url = Mysql_GlobalVar.catchmentrain_serverurl; 
            request_rain_url += "?planCode=" + hydromodel.Modelname;
            Dictionary<string, Dictionary<DateTime, double>> catchment_rain = Item_Info.Get_Catchment_RainGC_FromHttpRequest(request_rain_url);
            
            //请求产汇流模型计算 网络地址
            string request_flood_url = Mysql_GlobalVar.nam_serverurl;

            //请求参数组合
            string plan_code = hydromodel.Modelname;
            List<object> request_pars = new List<object>();
            request_pars.Add(plan_code);
            request_pars.Add(catchment_rain);
            request_pars.Add(85.0);
            request_pars.Add(0);
            //request_pars.Add(HydroModel.Get_BusinessCode_FromDB(plan_code));
            string request_pars_str = File_Common.Serializer_Obj(request_pars);

            Dictionary<string, object> allrequest_pars = new Dictionary<string, object>();
            allrequest_pars.Add("request_type", "catchment_cal");
            allrequest_pars.Add("request_pars", request_pars_str);
            string allrequest_pars_str = File_Common.Serializer_Obj(allrequest_pars);

            //请求产汇流模型计算所有子流域洪水过程
            Dictionary<string, Dictionary<DateTime, double>> catchment_q = Item_Info.Get_Catchment_FloodGC_FromHttpRequest(hydromodel, request_flood_url, allrequest_pars_str);
            Bnd11.Modify_Bnds_ToDischargeDic(ref hydromodel, catchment_q);
            return catchment_q;
        }

        //请求产汇流模型，修改所有相同边界条件ID的流量过程
        public Dictionary<string, Dictionary<DateTime, double>> Modify_BndID_ToDischargeDic()
        {
            HydroModel hydromodel = this;

            //请求产汇流模型计算 网络地址
            string request_url = Mysql_GlobalVar.catchmentq_serverurl;

            //请求参数
            string plan_code = hydromodel.Modelname;
            request_url += "?planCode=" + plan_code;

            //请求产汇流模型计算所有子流域洪水过程
            Dictionary<string, Dictionary<DateTime, double>> catchment_q = Item_Info.Get_CatchmentQ_FromHttpRequest(hydromodel, request_url);
            Bnd11.Modify_Bnds_ToDischargeDic(ref hydromodel, catchment_q);
            return catchment_q;
        }

        //综合判断修改边界条件
        public void Change_Reach_Boundry(string bnd_type, string bnd_value)
        {
            HydroModel hydromodel = this;
            Bnd11.Change_Reach_Boundry(hydromodel,bnd_type,bnd_value);
        }

        //将全部子流域入流边界条件改为0
        public void Modify_BndID_ToNoInflow()
        {
            //获取各子流域ID和默认的产汇流模型
            Dictionary<string, string> rf_models = RR11.Get_Default_RFmodel();

            for (int i = 0; i < rf_models.Count; i++)
            {
                Modify_BndID_ToConstDischarge(rf_models.ElementAt(i).Key,0.01);
            }
        }

        //修改指定边界条件 为常量值入流
        public void Modify_BndID_ToConstDischarge(string bnd_id,double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_BndID_ToConstDischarge(ref hydromodel, bnd_id, const_discharge);
        }

        //修改指定河道上边界为 常量值入流
        public void Modify_ReachUpBnd_ToConstDischarge(string reachname, double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachUpBnd_ToConstDischarge(ref hydromodel, reachname, const_discharge);
        }

        //修改指定河道上边界为 常量水位
        public void Modify_ReachUpBnd_ToConstwl(string reachname, double const_waterlevel)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachUpBnd_ToConstwl(ref hydromodel, reachname, const_waterlevel);
        }

        //修改指定河道上边界为 时间过程入流
        public void Modify_ReachUpBnd_ToDischargeDic(string reachname, Dictionary<DateTime, double> discharge_dic)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachUpBnd_ToDischargeDic(ref hydromodel, reachname, discharge_dic);
        }

        //修改指定河道下边界为 水位流量关系
        public void Modify_ReachDownBnd_ToQH(string reachname)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachDownBnd_ToQH(ref hydromodel, reachname);
        }

        //修改指定河道下边界为 常量水位
        public void Modify_ReachDownBnd_ToConstwl(string reachname, double const_waterlevel)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachDownBnd_ToConstwl(ref hydromodel, reachname, const_waterlevel);
        }

        //修改指定河道下边界为 水位过程
        public void Modify_ReachDownBnd_Towldic(string reachname, Dictionary<DateTime, double> waterleveldic)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachDownBnd_Towldic(ref hydromodel, reachname, waterleveldic);
        }

        //新增 随时间变化 的旁侧入流  --  点源(HD)
        public void Add_NewInflowBd(AtReach atreach, Dictionary<DateTime, double> discharge)
        {
            HydroModel hydromodel = this;
            string new_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + atreach.reachname + atreach.chainage + "_newrl.dfs0";

            //创建入流过程的dfs0文件
            Dfs0.Dfs0_Creat(new_in_filename, discharge, dfs0type.discharge);

            //加入边界集合
            Bnd11.Add_NewInflowBd(ref hydromodel, atreach, new_in_filename, "discharge");
        }

        //新增的 随时间变化 旁侧入流  --  分布源(HD)
        public void Add_NewInflowBd(Reach_Segment seg_reach, Dictionary<DateTime, double> discharge)
        {
            HydroModel hydromodel = this;
            string new_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + seg_reach.reachname + seg_reach.start_chainage + "_" + seg_reach.end_chainage + "_newrl.dfs0";

            //创建入流过程的dfs0文件
            Dfs0.Dfs0_Creat(new_in_filename, discharge, dfs0type.discharge);

            //加入边界集合
            Bnd11.Add_NewInflowBd(ref hydromodel, seg_reach, new_in_filename, "discharge");
        }

        //新增 常量 旁侧入流  --  点源(HD)
        public void Add_NewInflowBd(AtReach atreach, double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Add_NewInflowBd(ref hydromodel, atreach, const_discharge);
        }

        //新增 常量 旁侧入流  --  分布源(HD)
        public void Add_NewInflowBd(Reach_Segment seg_reach, double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Add_NewInflowBd(ref hydromodel, seg_reach, const_discharge);
        }

        #endregion


        #region 一维河道参数操作 -- 修改水库和河道初始水位，修改河道糙率，输出点

        //根据数据库实时水位更新全部河道水库初始水位和河道基流流量(贾鲁河流域)
        public void Update_InitiallevelAndBaseIn()
        {
            //如果基础模型是默认模型，则从数据库读取实时水位修改初始条件,修改各河道边界条件基流
            if (this.BaseModel.Modelname == "" || this.BaseModel.Modelname == Model_Const.DEFAULT_MODELNAME)
            {
                //用流量推算控制断面水位，修改河道沿线初始水位
                this.Update_InitialWaterlevel();

                //通过一定算法，获得几个主要河道额外基流流量(也可能是负值，表示取水)
                Dictionary<string, double> base_in_q = Item_Info.Get_MainReach_Base_Discharge();

                //修改河道与初始水位对应的边界条件基流流量
                if (base_in_q == null) return;
                for (int i = 0; i < base_in_q.Count; i++)
                {
                    this.Modify_BndID_ToConstDischarge(base_in_q.ElementAt(i).Key, base_in_q.ElementAt(i).Value);
                }
            }
        }

        //根据数据库实时水位更新全部河道水库初始水位
        public void Update_InitialWaterlevel()
        {
            HydroModel hydromodel = this;
            Hd11.Update_InitialWaterlevel(ref hydromodel);
        }

        //根据给定水位更新水库河道初始水位
        public void Update_InitialWaterlevel(Dictionary<string, double> inital_level)
        {
            HydroModel hydromodel = this;
            Hd11.Update_InitialWaterlevel(ref hydromodel, inital_level);
        }

        //更新边界条件的基流
        public void Update_Base_Discharge()
        {
            //获取主要河道额外 基流流量
            Dictionary<string, double> base_in_q = Item_Info.Get_MainReach_Base_Discharge();

            //修改河道与初始水位对应的边界条件基流流量
            if (Item_Info.Reach_NowDischarge != null)
            {
                for (int i = 0; i < base_in_q.Count; i++)
                {
                    this.Modify_BndID_ToConstDischarge(base_in_q.ElementAt(i).Key, base_in_q.ElementAt(i).Value);
                }
            }
        }

        //修改某位置的初始水位值
        public void Modify_ReachSection_InitialWaterLevel(AtReach atreach, double initialWaterLevel)
        {
            HydroModel hydromodel = this;
            Hd11.Modify_InitialWaterLevel(ref hydromodel, atreach, initialWaterLevel);
        }

        // 重新设置某河的糙率
        public void Modify_Reach_Resistance(string reachname, double resistance)
        {
            HydroModel hydromodel = this;
            Hd11.Modify_BedResistance(ref hydromodel, reachname, resistance);
        }

        // 添加新的河道的初始水位
        public void Add_NewInitial_WaterLevel(AtReach atreach, double initial_waterlevel)
        {
            HydroModel hydromodel = this;
            Hd11.Add_InitialWaterLevel(ref hydromodel, atreach, initial_waterlevel);
        }

        // 添加河道点的时间序列结果输出
        public void Add_OutputGridPoint(AtReach reachGrid)
        {
            HydroModel hydromodel = this;
            Hd11.Add_OutputGridPoint(ref hydromodel, reachGrid);
        }

        // 添加水质新的组分
        public void Add_NewComponent(string new_component)
        {
            HydroModel hydromodel = this;
            Ad11.Add_Component(ref hydromodel, new_component);
        }

        // 修改全局扩散系数 -- 用一个值
        public void Modify_Dispersion(double dispersion)
        {
            HydroModel hydromodel = this;
            Ad11.Modify_Dispersion(ref hydromodel, dispersion);
        }

        // 修改所有组分的衰减系数 -- 用一个值
        public void Modify_Decay(double decay)
        {
            HydroModel hydromodel = this;
            Ad11.Modify_Decay(ref hydromodel, decay);
        }
        #endregion


        #region 二维堤防操作 -- 正常挡水 、完全挡水、取消堤防（不挡水）、新建堤防
        //堤防操作 -- 正常挡水 、完全挡水（+100m)、完全不挡水 ,dike编号从1开始
        public void Dike_Opereate(string dikename, Dike_Operate operate)
        {
            HydroModel hydromodel = this;
            M21fm.Dike_Opereate(ref hydromodel, dikename, operate);
        }

        //新建堤防1 -- 仅给走向控制点，其他默认
        public void Add_Dike(string newdikename, List<PointXY> pointlist)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, newdikename);
        }

        //新建堤防2 -- 给走向控制点和堤防高度
        public void Add_Dike(string dikename, List<PointXY> pointlist, double dike_aveheight)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, dikename, dike_aveheight);
        }

        //新建堤防3 -- 给走向控制点和堤防高程常量
        public void Add_Dike(string dikename, List<PointXY> pointlist, double dike_crest, bool isconst)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, dikename, dike_crest, true);
        }

        //新建堤防4 -- 给走向和高程控制点
        public void Add_Dike(string dikename, List<PointXYZ> pointlist)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, dikename);
        }
        #endregion


        #region 二维堰(涵洞)操作 -- 正常过水、堵死不过水、新增单独涵洞、新增堤防上的涵洞并将堤防一分为二
        //涵洞操作 -- 正常启用过水、堵死不过水
        public void Weir_Opereate(string weirname, Weir_Operate operate)
        {
            HydroModel hydromodel = this;
            M21fm.Weir_Opereate(ref hydromodel, weirname, operate);
        }

        //新增涵洞1 -- 在已有dike上增加涵洞(堰底取地面高程)，并将原来的dike一分为二
        public void AddWeirAtDike(string new_weirname, PointXY point, double weir_width)
        {
            HydroModel hydromodel = this;
            M21fm.AddWeirAtDike(ref hydromodel, point, new_weirname, weir_width);
        }

        //新增涵洞2 -- 在已有dike上增加涵洞(给定堰底高程)，并将原来的dike一分为二
        public void AddWeirAtDike(string weir_name, PointXY point, double weir_width, double datum)
        {
            HydroModel hydromodel = this;
            M21fm.AddWeirAtDike(ref hydromodel, point, weir_name, weir_width, datum);
        }
        #endregion


        #region 二维其他参数操作
        //设置干湿边界
        public void Set_Mike21_DryWet(Dry_Wet new_drywet)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            m21pars.Dry_wet = new_drywet;
        }

        //设置保存时间步长
        public void Set_Mike21_Savetimestep(SaveTimeStep savetimestep)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            m21pars.Mike21_savetimestepbs = savetimestep;
        }

        //设置常量蒸发(mm/day)
        public void Set_Mike21_ConstEva(double const_eva)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            Pre_Eva pre_eva = m21pars.Eva_set;
            pre_eva.type_pre_eva = Type_Pre_Eva.Pre_Eva;
            pre_eva.constantEvaporation = const_eva;
            m21pars.Eva_set = pre_eva;
        }

        //按该月多年平均蒸发速率设置常量蒸发
        public void Set_Mike21_AverageConstEva()
        {
            //获取该月常量蒸发速率(mm/day)
            DateTime start_time = this.ModelGlobalPars.Simulate_time.Begin;
            DateTime end_time = this.ModelGlobalPars.Simulate_time.End;
            Dictionary<DateTime, double> Average_DayEvaRfRate = CatchmentList.Get_Average_DayEvaRfRate(start_time, end_time, Eva_RF.eva);
            double const_eva = Average_DayEvaRfRate.Values.ElementAt(Average_DayEvaRfRate.Count - 1);

            //设置常量蒸发
            Set_Mike21_ConstEva(const_eva);
        }

        //设置常量初始水位
        public void Set_Mike21_ConstInitial_WaterLevel(double const_waterlevel)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            Initial_Contition initial = m21pars.Initial_set;
            initial.initial_type = 1;
            initial.const_surface_elevation = const_waterlevel;
            initial.fileName = "";
            initial.itemName = "";

            m21pars.Initial_set = initial;
        }

        #endregion


        #region 二维地面操作1 -- 改变地形高程、查询指定点高程
        //改变多边形区域内地形高程 -- 增加或减少一定值(value为负即为减)
        public void EditMesh_Addvalue(List<PointXY> polygon_points, double change_value)
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Addvalue(ref hydromodel, polygon_points, change_value);
        }

        //改变多边形区域内地形高程 -- 设为常量值
        public void EditMesh_Setvalue(List<PointXY> polygon_points, double const_value)
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Setvalue(ref hydromodel, polygon_points, const_value);
        }

        //改变多边形区域内地形高程 -- 重新根据DEM散点插值
        public void EditMesh_Interpolate_InPolygon(List<PointXY> polygon_points)
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Interpolate_InPolygon(ref hydromodel, polygon_points);
        }

        //重新内插所有mesh节点高程值
        public void EditMesh_Interpolate_Allnodes()
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Interpolate_Allnodes(ref hydromodel);
        }

        //从文件中读取多边形控制点数据   ** 静态方法 **
        public static List<PointXY> ReadXYFile(string sourcefile)
        {
            return Mesh.ReadXYFile(sourcefile);
        }

        //从文件中读取DEM高程散点数据   ** 静态方法 **
        public static List<PointXYZ> ReadXYZFile(string sourcefile)
        {
            return Mesh.ReadXYZFile(sourcefile);
        }

        //获取指定点地面高程 -- 从mesh文件中
        public double Get_Point_Z(PointXY point)
        {
            HydroModel hydromodel = this;
            return Mesh.Get_Point_Z(hydromodel, point);
        }
        #endregion


        #region 二维地面操作2 -- 重新生成mesh以替换原mesh、新增加mesh与原mesh结合
        //根据前端给定的边界，创建新mesh -- 原来的mesh将被替换
        public void Create_New_MdfMesh(List<List<PointXY>> boundaryPoint)
        {
            HydroModel hydromodel = this;

            Mdf.Create_New_MdfMesh(ref hydromodel, boundaryPoint);
        }

        //根据前端给定的边界，创建新mesh并与原mesh合并 -- 原来的mesh范围将增大
        public void Create_NewMesh_AndMerge(List<List<PointXY>> boundaryPoint)
        {
            HydroModel hydromodel = this;

            Mdf.Create_NewMesh_AndMerge(ref hydromodel, boundaryPoint);
        }

        #endregion


        #region 耦合连接操作 -- 新增侧向耦合连接、侧向建筑物耦合连接(**缺少普通侧向建筑物的耦合连接**)
        //新增侧向连接 -- 给定连接河段和河道哪一侧，可以两侧都选
        public void Add_Lateral_Link(Reach_Segment reachsegment, ReachLR reachlr)
        {
            HydroModel hydromodel = this;
            Couple.Add_Lateral_Link(ref hydromodel, reachsegment, reachlr);
        }

        //新增侧向连接 -- 给定两点和河道哪一侧，可以两侧都选
        public void Add_Lateral_Link(PointXY start_p, PointXY end_p, ReachLR reachlr)
        {
            HydroModel hydromodel = this;
            Couple.Add_Lateral_Link(ref hydromodel, start_p, end_p, reachlr);
        }

        //新增侧向建筑物(如分洪口)连接 -- 给定分洪口建筑物对象
        public void Add_SideStructure_Link(string sidestr_name)
        {
            HydroModel hydromodel = this;
            Controlstr str = this.Mike11Pars.ControlstrList.GetGate(sidestr_name);
            if (str == null) return;
            Couple.Add_SideStructure_Link(ref hydromodel, str as Fhkstr);
        }

        //去除指定河道的所有侧向连接
        public void Remove_ReachALL_LateralLink(string reachname)
        {
            CoupleLinkList couplelinklist = this.CoupleLinklist;
            couplelinklist.Remove_ReachALL_LateralLink(reachname);
        }

        //去除指定侧向建筑物连接
        public void Remove_Sidestr_Link(string sidestr_name)
        {
            CoupleLinkList couplelinklist = this.CoupleLinklist;
            Controlstr str = this.Mike11Pars.ControlstrList.GetGate(sidestr_name);
            if (str == null) return;
            couplelinklist.Remove_Sidestr_Link(str as Fhkstr);
        }
        #endregion


        #region 一维断面数据查询操作 -- 从Xns11文件和数据库中查询
        //从数据库中获取指定河道的左右堤顶高程、地面高程和河底高程   ** 静态方法 **
        public static ReachSection_Altitude Get_Altitude_FromDB(AtReach atreach)
        {
            ReachSection_Altitude altitude = Xns11.Get_Altitude(atreach);
            return altitude;
        }

        //获取指定河道的左右堤顶高程、地面高程和河底高程 -- 从模型参数中获取
        public ReachSection_Altitude Get_Altitude(AtReach atreach)
        {
            ReachSection_Altitude altitude = this.Mike11Pars.SectionList.Get_Altitude(atreach);
            return altitude;
        }

        //获取河道所有断面桩号集合 -- 从模型参数中获取
        public List<double> Get_Reach_SecChainageList(string reachname)
        {
            return this.Mike11Pars.SectionList.Get_Reach_SecChainageList(reachname);
        }

        //获取河段内的断面桩号集合 -- 从模型参数中获取
        public List<double> Get_SegReach_SecChainageList(Reach_Segment reach_segment)
        {
            return this.Mike11Pars.SectionList.Get_SegReach_SecChainageList(reach_segment);
        }

        //获取河道指定断面的断面数据 -- 从模型参数中获取
        public List<PointXZS> Get_Sectiondata(AtReach atreach)
        {
            return this.Mike11Pars.SectionList.Get_Sectiondata(atreach);
        }

        //获取河道指定断面的断面数据 -- 根据点从模型断面数据库中获取
        public static Dictionary<string,object> Get_Sectiondata_FromPoint(PointXY p)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            //转为投影坐标
            PointXY pro_p = PointXY.CoordTranfrom(p, 4326, 4547, 3);

            //从默认模型文件中初始化河道数据
            ReachList reachlist = new ReachList();
            string default_modeldir = Model_Files.Get_Modeldir(Model_Const.DEFAULT_MODELNAME);
            string nwk11_file = File_Common.Get_MaxFile(default_modeldir, ".nwk11");

            Nwk11.GetDefault_ReachInfo(nwk11_file, ref reachlist);
            AtReach atreach = Nwk11.Get_NearReach(reachlist, pro_p);
            if (atreach.chainage == -1) return null;

            //断面如果不在断面上，则获取最近的断面
            List<PointXZS> sectiondata = Xns11.Get_SectionData(atreach.reachname, atreach.chainage);

            //转换格式
            List<double[]> sectiondata1 = new List<double[]>();
            for (int i = 0; i < sectiondata.Count; i++)
            {
                sectiondata1.Add(new double[] { sectiondata[i].X, sectiondata[i].Z });
            }

            //断面数据排序
            List<double[]> sectiondata2 = sectiondata1.OrderBy(arr => arr[0]).ToList();

            //获取河道中文名
            string reach_cnname = Item_Info.Get_ReachChinaName(atreach.reachname);
            double chainage = atreach.chainage;

            res.Add("reach_name", reach_cnname);
            res.Add("chainage", chainage);
            res.Add("section_data", sectiondata2);
            return res;
        }

        //获取河道出口的水位流量关系 -- 从断面文件中计算
        public Dictionary<double, double> Get_ReachOut_QH(string reachname)
        {
            string Xns11_sourcefilename = this.Modelfiles.Xns11_filename;
            return Xns11.Get_ReachEnd_QH(Xns11_sourcefilename, reachname, reachname + Model_Const.REACHID_HZ);
        }
        #endregion


        #region 一维结果查询操作 -- 一维河道结果、产汇流结果、水量平衡结果等
        //查询河道指定点 全部时间结果数据（水位、水深、流速、流量）
        public static Dictionary<DateTime, mike11_res> Get_Mike11Point_AllRes(string plan_code,PointXY p)
        {
            return Res11.Get_Mike11PointRes(plan_code, p);
        }

        //查询河道指定点的某时刻 结果数据（水位、水深、流速、流量）
        public static mike11_res Get_Mike11Point_Res(string plan_code, PointXY p,DateTime time)
        {
            Dictionary<DateTime, mike11_res> mike11_res_dic = HydroModel.Get_Mike11Point_AllRes(plan_code, p);

            mike11_res res = mike11_res_dic.Keys.Contains(time)? mike11_res_dic[time]:new mike11_res();
            return res;
        }

        //查询指定点的指定项结果数据
        public Dictionary<DateTime, double> Get_Mike11Point_SingleRes(PointXY p, mike11_restype itemtype)
        {
            HydroModel hydromodel = this;
            return Res11.Res11reader(hydromodel.Modelname, p, itemtype);
        }

        //查询指定河道断面桩号的指定项
        public Dictionary<DateTime, double> Get_Mike11Section_SingleRes(AtReach atreach, mike11_restype itemtype)
        {
            HydroModel hydromodel = this;
            return Res11.Res11reader(hydromodel.Modelname, atreach, itemtype);
        }

        //查询指定流域的指定结果项(径流流量或净雨)
        public Dictionary<DateTime, double> Get_RRCatchment_SingleRes(string catchmentname, RF_restype restype)
        {
            HydroModel hydromodel = this;
            if (this.RfPars.Rainfall_selectmodel == RFModelType.XAJ)
            {
                if (restype == RF_restype.NetRainfall)
                {
                    Console.WriteLine("新安江模拟没有净雨深结果！");
                    return null;
                }
                return Res11.Get_XAJresdic(hydromodel, catchmentname);
            }
            else
            {
                return Res11.Res11reader(hydromodel, catchmentname, restype);
            }
        }

        //查询指定流域的所有统计值 -- 最大径流量，出现的时间，总径流量，总净雨深
        public rainfallstatist Get_RRCatchment_StatistRes(string catchmentname)
        {
            HydroModel hydromodel = this;
            return Res11.Res11reader(hydromodel, catchmentname);
        }

        //获取模型水量平衡结果 -- 包括一维河道或二维网格水量
        public water_volume_balance Get_WaterVolume_BalanceRes(Mike11_Mike21 mike11_mike21)
        {
            HydroModel hydromodel = this;
            return Res11.Htmlreader(hydromodel, mike11_mike21);
        }

        //将特征点一维结果写入数据库
        public void Write_Mike11TxtRes_ToDB()
        {
            HydroModel hydromodel = this;
            Res11.Writejgtxt_ToMysql(hydromodel);
        }


        //*****************模型结果后处理**************************
        //获取格网预报降雨风险
        public static Warning_Info Get_Rain_WarningInfo(string plan_code)
        {
            //获取模型信息
            Dictionary<string, string> model_info = HydroModel.Get_Model_Info_FromDB(plan_code);
            string start_time = model_info["start_time"];
            string end_time = SimulateTime.TimetoStr(SimulateTime.StrToTime(start_time).AddHours(24));

            //未来24小时格网预报降雨和雨强
            string request_url = Mysql_GlobalVar.forecast_rain_serverurl + "?st=" + start_time+ "&ed=" + end_time;
            string response_res = File_Common.Get_HttpReSponse(request_url);
            dynamic jsonObject = JsonConvert.DeserializeObject(response_res);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());

            //遍历其属性和值
            Dictionary<string, object> res_list = new Dictionary<string, object>();
            foreach (var property in json_obj.Properties())
            {
                string item = property.Name;
                object value = property.Value;
                res_list.Add(item, value);
            }

            //降雨过程值
            JArray rainArray = res_list["v"] as JArray;
            List<double> rain_value = rainArray.ToObject<List<double>>();
            double total_rain = Math.Round(rain_value.Sum(),2);
            double max_rain = Math.Round(rain_value.Max(),2);

            //预警分级和预警信息
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "";
            if (total_rain > 200)
            {
                warning_level = Warning_Level.crimson_warining;

                warning_desc = $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }
            else if (total_rain > 100)
            {
                warning_level = Warning_Level.red_warining;

                warning_desc = $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }
            else if (total_rain > 50)
            {
                warning_level = Warning_Level.yellow_warining;
                warning_desc = $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }
            else
            {
                warning_level = Warning_Level.blue_warining;
                
                warning_desc = total_rain == 0? $"预报未来24小时无降雨":
                    $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }

            List<Warning_Info> rain_station_warning = new List<Warning_Info>();
            Warning_Info warning_info = new Warning_Info("降雨预警", warning_level, warning_desc, rain_station_warning);

            return warning_info;
        }

        //从预报方案结果中，获取预警风险信息，包括降雨、水库、河道、蓄滞洪区进洪、南水北调、山洪灾害预警信息 
        public static List<Warning_Info> Get_WarningInfo(string plan_code)
        {
            List<Warning_Info> res = new List<Warning_Info>();

            //获取一维模型结果
            Dictionary<string, object> mike11_res = HydroModel.Get_Mike11_TjResult(plan_code);
            if (mike11_res == null) return res;
            Dictionary<string, Reservoir_FloodRes> res_results = mike11_res["reservoir_result"] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, ReachSection_FloodRes> reach_result = mike11_res["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = mike11_res["floodblq_result"] as Dictionary<string, Xzhq_FloodRes>;
            Dictionary<string, object> reach_risk = mike11_res["reach_risk"] as Dictionary<string, object>;
            string result_desc = mike11_res["result_desc"] as string;
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //1、获取水库预警信息
            Warning_Info res_warninginfo = Res11.Get_AllRes_Warninginfo(plan_code, res_results,now_waterlevel);
            res.Add(res_warninginfo);

            //2、获取河道预警信息
            Warning_Info reach_warninginfo = Res11.Get_Reach_Warninginfo(plan_code, reach_result, reach_risk, now_waterlevel);
            res.Add(reach_warninginfo);

            //3、获取蓄滞洪区进洪预警信息
            Warning_Info xzhq_warninginfo = Res11.Get_Xzhq_Warninginfo(plan_code, xzhq_result);
            res.Add(xzhq_warninginfo);

            //4、获取南水北调预警信息
            Warning_Info nsbd_warninginfo = Res11.Get_Nsbd_Warninginfo(plan_code, reach_risk);
            res.Add(nsbd_warninginfo);

            //5、获取山洪预警信息
            Warning_Info shzh_warninginfo = Res11.Get_Sh_Warninginfo(plan_code, reach_result);
            res.Add(shzh_warninginfo);



            return res;
        }

        //从方案结果中，获取水库、河道、蓄滞洪区存在高风险的重点巡查部位
        public static Dictionary<string, List<Important_Inspect_UnitInfo>> Get_Important_Inspect_Parts(HydroModel model)
        {
            Dictionary<string, List<Important_Inspect_UnitInfo>> inspect_list = new Dictionary<string, List<Important_Inspect_UnitInfo>>();

            //获取一维模型结果
            Dictionary<string, object> mike11_res = HydroModel.Get_Mike11_TjResult(model.Modelname);
            if (mike11_res == null) return inspect_list;
            Dictionary<string, Reservoir_FloodRes> res_results = mike11_res["reservoir_result"] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, ReachSection_FloodRes> reach_result = mike11_res["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = mike11_res["floodblq_result"] as Dictionary<string, Xzhq_FloodRes>;
            Dictionary<string, object> reach_risk = mike11_res["reach_risk"] as Dictionary<string, object>;
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //1、获取水库类型 重点巡查的大中型水库
            List<Important_Inspect_UnitInfo> res_inspect_list = Res11.Get_AllRes_Inspectinfo(model, res_results, now_waterlevel);
            inspect_list.Add("水库", res_inspect_list);

            //2、获取河道类型 重点巡查的河段清单
            List<Important_Inspect_UnitInfo> reach_inspect_list = Res11.Get_Reach_Inspectinfo(model, reach_result, reach_risk, now_waterlevel);
            inspect_list.Add("河道", reach_inspect_list);

            //3、获取蓄滞洪区类型 重点巡查的蓄滞洪区
            List<Important_Inspect_UnitInfo> xzhq_inspect_list = Res11.Get_Xzhq_Inspectinfo(model, xzhq_result);
            inspect_list.Add("蓄滞洪区", xzhq_inspect_list);

            return inspect_list;
        }

        //获取模型的所有mike11结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_Result(string plan_code)
        {
            return Res11.Load_Res11(plan_code);
        }

        //获取模型的mike11全过程结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_GcResult(string plan_code)
        {
            Dictionary<string, object> res = Res11.Load_Res11_GC(plan_code);
            return res;
        }

        //获取模型mike11 统计结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_TjResult(string plan_code)
        {
            Dictionary<string, object> res = Res11.Load_Res11_TJ(plan_code);
            return res;
        }

        //获取模型指定类别的mike11纵剖面结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_ZPResult(string plan_code, string res_type)
        {
            Dictionary<string, object> res = Res11.Load_Res11_Part3(plan_code, res_type);
            return res;
        }

        //获取模型指定时刻的mike11纵剖面结果详表 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_XbResult(string plan_code)
        {
            Dictionary<string, object> res = Res11.Load_Res11_Part5(plan_code);
            return res;
        }

        //获取河道水位断面详表 -- 从数据库，用于前端获取
        public static Dictionary<string, List<float>> Get_ReachSections(string plan_code)
        {
            Dictionary<string, List<float>> res = Res11.Load_ReachSections(plan_code);
            return res;
        }

        //获取单一河道断面的水位流量过程(优先数据库，没有就读结果文件)
        public static Dictionary<string, Dictionary<DateTime, float>> Get_SectionRes(string plan_code, AtReach atreach)
        {
            Dictionary<string, Dictionary<DateTime, float>> res = Res11.Load_SectionRes_FromDB(plan_code, atreach);
            return res;
        }

        //获取模型指定时刻的 mike11 GIS结果信息 -- 从数据库，用于前端获取
        public static string Get_Mike11Gis_Result(string plan_code,string model_instance,Mike11FloodRes_GisType res_gistype, string time = "")
        {
            if(res_gistype == Mike11FloodRes_GisType.reach_centerline)
            {
                string field_name = "line_sample_result";
                return Res11.Load_Sample_GisRes(field_name, model_instance);
            }
            else
            {
                return Res11.Load_Reach_GisPolygon_Res(plan_code, model_instance, time);
            }
        }

        //获取模型 mike11 GIS统计结果 -- 从数据库，用于前端获取
        public static string Get_Mike11GisTJ_Result(string plan_code)
        {
            return Res11.Load_Res11Gis(plan_code);
        }

        //获取模型的所有mike11结果信息 -- 从结果文件,用于模型结果保存,在模型计算结果出来后调用
        public List<object> Get_Mike11_AllResult(string model_instance, bool is_quick = false)
        {
            List<object> model_allresult = new List<object>();
            Stopwatch sw = new Stopwatch();
            HydroModel model = this;

            //判断
            if (model == null || !File.Exists(model.Modelfiles.Hdres11_filename)) return null;

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(model.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);
            IDataItem dataitem = resdata.Reaches.ElementAt(0).DataItems.ElementAt(0);
            if (dataitem == null) return null;

            //如果是自动预报模型，临时修改模型提前模拟时间，从而减少时间裁剪
            //int model_ahead_hours = model.ModelGlobalPars.Ahead_hours;
            //if(model.Modelname == Model_Const.AUTO_MODELNAME) model.ModelGlobalPars.Ahead_hours = Model_Const.AUTOFORECAST_AHEAD_HOURS;

            //水库洪水结果
            sw.Start();
            Dictionary<string, Reservoir_FloodRes> res_result = Res11.Get_AllReservoir_FloodRes(model, resdata,model_instance);
            model_allresult.Add(res_result);
            sw.Stop();
            Console.WriteLine("0水库洪水结果查询完毕！耗时：{0}", sw.Elapsed);

            //河道特征断面洪水结果
            sw.Restart();
            Dictionary<string, ReachSection_FloodRes> reach_result = Res11.Get_ReachSection_FloodRes(model, resdata, model_instance);
            model_allresult.Add(reach_result);
            sw.Stop();
            Console.WriteLine("1河道特征断面洪水结果查询完毕！耗时：{0}", sw.Elapsed);

            //蓄滞洪区洪水结果
            sw.Restart();
            Dictionary<string, Dictionary<DateTime, string>> sub_xzhq_res;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = Res11.Get_Xzhq_FloodRes(model, resdata,out sub_xzhq_res);
            model_allresult.Add(xzhq_result);
            sw.Stop();
            Console.WriteLine("2蓄滞洪区洪水结果查询完毕！耗时：{0}", sw.Elapsed);

            //如果是快速模拟，则这些结果全部为空
            if (is_quick)
            {
                Add_Null_ZDres(ref model_allresult);
            }
            else
            {
                //水位纵断(H点)
                sw.Restart();
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> LevelZdmHD_Data = model.Get_LevelZdm_Data(resdata, model_instance);
                Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float1 = Res11.Get_FloatDicAll(LevelZdmHD_Data); //轻量化一下再存，会快点
                model_allresult.Add(ZdmHD_Data_Float1);
                sw.Stop();
                Console.WriteLine("3水位纵断查询完毕！耗时：{0}", sw.Elapsed);

                //流量纵断(Q点插值为H点)
                sw.Restart();
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> DischargeZdmHD_Data = model.Get_DischargeZdm_Data(resdata, model_instance);
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> New_DischargeZdmHD_Data = Res11.Insert_Qzd_FromHzd(LevelZdmHD_Data, DischargeZdmHD_Data);
                Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float2 = Res11.Get_FloatDicAll(New_DischargeZdmHD_Data); //轻量化一下再存，会快点
                model_allresult.Add(ZdmHD_Data_Float2);
                sw.Stop();
                Console.WriteLine("4流量纵断查询完毕！耗时：{0}", sw.Elapsed);

                //流速纵断(H点)
                sw.Restart();
                ResultData resdata1 = new ResultData();
                resdata1.Connection = Connection.Create(model.Modelfiles.Hd_addres11_filename);
                Diagnostics diagn1 = new Diagnostics();
                resdata1.Load(diagn1);
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> SpeedZdmHD_Data = model.Get_SpeedZdm_Data(resdata1, model_instance);
                Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float3 = Res11.Get_FloatDicAll(SpeedZdmHD_Data); //轻量化一下再存，会快点
                model_allresult.Add(ZdmHD_Data_Float3);
                sw.Stop();
                Console.WriteLine("5流速纵断查询完毕！耗时：{0}", sw.Elapsed);

                //堤顶和渠底纵断(H点)
                sw.Restart();
                DateTime[] timearray = LevelZdmHD_Data.First().Value.Keys.ToArray();
                Dictionary<string, double[]> grid = Res11.Get_Reach_GridChainages(LevelZdmHD_Data);
                Dictionary<string, List<double>> Zdm_dd_Data = model.Get_Zdmdd_Data(grid);
                Dictionary<string, List<double>> Zdm_qd_Data = model.Get_Zdmqd_Data(grid);
                Dictionary<string, List<double>> Zdm_td_Data = model.Get_Zdmtd_Data(grid);

                Dictionary<string, List<float>> Zdmdd_Data = Res11.Get_FloatDic(Zdm_dd_Data);
                Dictionary<string, List<float>> Zdmqd_Data = Res11.Get_FloatDic(Zdm_qd_Data);
                Dictionary<string, List<float>> Zdmtd_Data = Res11.Get_FloatDic(Zdm_td_Data);

                model_allresult.Add(Zdmdd_Data);
                model_allresult.Add(Zdmqd_Data);
                sw.Stop();
                Console.WriteLine("67堤顶渠底纵断查询完毕！耗时：{0}", sw.Elapsed);

                //H点桩号序列
                sw.Restart();
                Dictionary<string, List<double>> H_Chainage = Res11.Get_Reach_GridChainages1(LevelZdmHD_Data);
                Dictionary<string, List<float>> H_Chainage_Float = Res11.Get_FloatDic(H_Chainage); //轻量化一下再存，会快点
                model_allresult.Add(H_Chainage_Float);
                sw.Stop();
                Console.WriteLine("8桩号序列查询完毕！耗时：{0}", sw.Elapsed);

                //语音结果
                sw.Restart();
                Dictionary<DateTime, string> ResultSoundFiles = new Dictionary<DateTime, string>(); //暂缓 model.Get_ResultSoundFiles(gate_totaldischarge, tsz_accumulatedV, LevelZdmHD_Data);
                model_allresult.Add(ResultSoundFiles);
                sw.Stop();
                Console.WriteLine("9语音结果生成完毕！耗时：{0}", sw.Elapsed);

                //河道风险统计结果
                sw.Restart();
                Dictionary<string, object> reach_risk = Res11.Get_Reach_RiskResult(model, ZdmHD_Data_Float1, ZdmHD_Data_Float2, ZdmHD_Data_Float3,
                    Zdmdd_Data, Zdmtd_Data, H_Chainage_Float, reach_result, res_result, model_instance);
                model_allresult.Add(reach_risk);
                sw.Stop();
                Console.WriteLine("10河道风险结果统计完毕！耗时:{0}", sw.Elapsed);

                //结果综述
                Dictionary<string, List<float>> level_max = Res11.Get_ZdRes_TimeMaxValue(ZdmHD_Data_Float1);
                string res_desc = Get_Result_Desc(res_result, xzhq_result, level_max, Zdmdd_Data, reach_result);
                model_allresult.Add(res_desc);
                Console.WriteLine("11结果综述生成完毕！耗时:{0}", sw.Elapsed);

                //滩地纵剖
                model_allresult.Add(Zdmtd_Data);
                Console.WriteLine("12滩地纵剖生成完毕！耗时:{0}", sw.Elapsed);

                //子蓄滞洪区启用结果
                Dictionary<DateTime, Dictionary<string, string>> sub_xzhq_res1 = Res11.Get_Dic_Change(sub_xzhq_res);
                model_allresult.Add(sub_xzhq_res1);
                Console.WriteLine("13子蓄滞洪区启用结果！耗时:{0}", sw.Elapsed);
            }

            return model_allresult;
        }

        //空的各纵断结果
        private static void Add_Null_ZDres(ref List<object> model_allresult)
        {
            //水位纵断(H点)
            Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float1 = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            model_allresult.Add(ZdmHD_Data_Float1);

            //流量纵断(Q点插值为H点)
            Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float2 = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            model_allresult.Add(ZdmHD_Data_Float2);

            //流速纵断(H点)
            Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float3 = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            model_allresult.Add(ZdmHD_Data_Float3);

            //堤顶和渠底纵断(H点)
            Dictionary<string, List<float>> Zdmdd_Data = new Dictionary<string, List<float>>();
            Dictionary<string, List<float>> Zdmqd_Data = new Dictionary<string, List<float>>();
            model_allresult.Add(Zdmdd_Data);
            model_allresult.Add(Zdmqd_Data);

            //H点桩号序列
            Dictionary<string, List<float>> H_Chainage_Float = new Dictionary<string, List<float>>();
            model_allresult.Add(H_Chainage_Float);

            //语音结果
            Dictionary<DateTime, string> ResultSoundFiles = new Dictionary<DateTime, string>();
            model_allresult.Add(ResultSoundFiles);

            //河道风险统计结果
            Dictionary<string, object> reach_risk = new Dictionary<string, object>();
            model_allresult.Add(reach_risk);

            //结果综述
            model_allresult.Add("");

            //滩地纵剖
            Dictionary<string, List<float>> Zdmtd_Data = new Dictionary<string, List<float>>();
            model_allresult.Add(Zdmtd_Data);

            //子蓄滞洪区启用结果
            Dictionary<DateTime, Dictionary<string, string>> sub_xzhq_res1 = new Dictionary<DateTime, Dictionary<string, string>>();
            model_allresult.Add(sub_xzhq_res1);
        }

        //获取结果综述
        public string Get_Result_Desc(Dictionary<string, Reservoir_FloodRes> res_result, Dictionary<string, Xzhq_FloodRes> fhblq_result,
            Dictionary<string, List<float>> level_max, Dictionary<string, List<float>> Zdmdd_Data, Dictionary<string, ReachSection_FloodRes> reach_result)
        {
            //水库风险总结
            string res_resultdesc = Get_Res_ResultDesc(res_result);

            //河道漫堤风险
            string reach_resultdesc = Get_Reach_ResultDesc(level_max, Zdmdd_Data);

            //蓄滞洪区风险总结
            string fhblq_resultdesc = Get_Fhblq_ResultDesc(fhblq_result);

            //出省洪峰流量
            double ycz_outq = reach_result.Keys.Contains("元村")? reach_result["元村"].Max_Qischarge:0;
            string outq_desc = $"4、下游(元村站)出省洪峰流量{Math.Round(ycz_outq)}m³/s。";

            return res_resultdesc + "; " + reach_resultdesc + "; " + fhblq_resultdesc + "; " + outq_desc;
        }

        //保留区风险综述
        public static string Get_Fhblq_ResultDesc(Dictionary<string, Xzhq_FloodRes> fhblq_result)
        {
            Dictionary<string, int> blq_fxres = new Dictionary<string, int>();
            string first_blq_name = ""; int run_blq = 0; int overrun_blq = 0;
            for (int i = 0; i < fhblq_result.Count; i++)
            {
                string blq_cnname = fhblq_result.ElementAt(i).Value.Name;
                Xzhq_FloodRes blq_res = fhblq_result.ElementAt(i).Value;
                if(blq_res.Xzhq_State == "启用" || blq_res.Xzhq_State == "超设计")
                {
                    if (first_blq_name == "") first_blq_name = blq_cnname;
                    run_blq++;
                    if(blq_res.Xzhq_State == "超设计") overrun_blq++;
                }
            }

            string blq_fx_str = $"3、{run_blq}个蓄滞洪区启用，其中{overrun_blq}个超设计启用";

            return blq_fx_str;
        }

        //南水北调交叉断面风险
        private static string Get_Reach_ResultDesc(HydroModel model, Dictionary<string, ReachSection_FloodRes> reach_result,string model_instance)
        {
            //获取河道特征断面  --河道南水北调交叉断面
            List<Water_Condition> section_waterlevel = Res11.Get_ReachSection_Info(model_instance);

            //换一下格式
            Dictionary<string, Water_Condition> section_info = new Dictionary<string, Water_Condition>();
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                section_info.Add(section_waterlevel[i].Reach_CnName, section_waterlevel[i]);
            }

            //获取各断面堤顶高程
            Dictionary<string, double> dd_level = new Dictionary<string, double>();
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                AtReach section_atreach = AtReach.Get_Atreach(section_waterlevel[i].Reach, section_waterlevel[i].Chainage);
                double section_dd = model.Mike11Pars.SectionList.GetReach_Section_MaxminZ(section_atreach).max_z;
                dd_level.Add(section_waterlevel[i].Reach_CnName, section_dd);
            }

            //交叉断面 洪水风险总结
            List<string> secton_level_jj = new List<string>();
            List<string> secton_level_bz = new List<string>();
            List<string> secton_level_md = new List<string>();
            for (int i = 0; i < reach_result.Count; i++)
            {
                string section_name = reach_result.ElementAt(i).Key;   //中文
                double max_level = reach_result.ElementAt(i).Value.Max_Level;
                double level1 = section_info.Keys.Contains(section_name) ? double.Parse(section_info[section_name].Level1):10000;
                double level3 = section_info.Keys.Contains(section_name) ? double.Parse(section_info[section_name].Level3):10000;
                double dd = dd_level.Keys.Contains(section_name) ? dd_level[section_name] : 10000;
                if (max_level > dd)
                {
                    //超堤顶水位
                    secton_level_md.Add(section_name);
                }
                else if (max_level > level3) 
                {
                    //超保证水位
                    secton_level_bz.Add(section_name);
                }
                else if(max_level > level1)
                {
                    //超警戒水位
                    secton_level_jj.Add(section_name);
                }
            }

            string reach_jj_info = ""; 
            for (int i = 0; i < secton_level_jj.Count; i++)
            {
                reach_jj_info += secton_level_jj[i];
                if (i != secton_level_jj.Count - 1) reach_jj_info += "、";
            }
            reach_jj_info = secton_level_jj.Count == 0 ? "无交叉断面河道水位超警戒" : reach_jj_info + "河道水位超警戒";

            string reach_bz_info = ""; 
            for (int i = 0; i < secton_level_bz.Count; i++)
            {
                reach_bz_info += secton_level_bz[i];
                if (i != secton_level_bz.Count - 1) reach_bz_info += "、";
            }
            reach_bz_info = secton_level_bz.Count == 0 ? "无交叉断面河道水位超保证" : reach_bz_info + "河道水位超保证";

            string reach_md_info = "";
            for (int i = 0; i < secton_level_md.Count; i++)
            {
                reach_md_info += secton_level_md[i];
                if (i != secton_level_md.Count - 1) reach_md_info += "、";
            }
            reach_md_info = secton_level_md.Count == 0? "无交叉断面河道水位漫堤" : reach_md_info +"河道水位漫堤";

            string res_desc = "2、南水北调示范段" + reach_jj_info + "，" + reach_bz_info + "，" + reach_md_info;
            return res_desc;
        }

        //河道漫堤风险
        private static string Get_Reach_ResultDesc(Dictionary<string, List<float>> level_max, Dictionary<string, List<float>> Zdmdd_Data)
        {
            //河堤风险总结
            List<string> reach_md = new List<string>();
            for (int i = 0; i < level_max.Count; i++)
            {
                string reach_name = level_max.ElementAt(i).Key;
                List<float> reach_level_max = level_max.ElementAt(i).Value;
                List<float> reach_dd_level = Zdmdd_Data.ElementAt(i).Value;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                if (reach_cnname.Contains("连接河") || reach_cnname.Contains("分洪堰") || reach_cnname.Contains("泄洪洞") || reach_cnname.Contains("溢洪") || reach_cnname.Contains("保留区")) continue;
                for (int j = 0; j < reach_level_max.Count; j++)
                {
                    if (reach_level_max[j] >= reach_dd_level[j] - 0.1)
                    {
                        reach_md.Add(reach_cnname);
                        break;
                    }
                }
            }

            string mdreach = "2、";
            for (int i = 0; i < reach_md.Count; i++)
            {
                mdreach += reach_md[i];
                if (i > 2) break;  //只放3条河道
                if (i != reach_md.Count - 1) mdreach += "、";
            }
            if (reach_md.Count > 3) mdreach += "等河道";

            string reach_md_str = reach_md.Count == 0 ? "2、流域各主要河道均不存在漫堤风险" : mdreach + "局部河段存在漫堤风险";

            return reach_md_str;
        }


        //获取水库的结果描述
        public static string Get_Res_ResultDesc(Dictionary<string, Reservoir_FloodRes> res_result)
        {
            //各水库的坝顶高程
            Dictionary<string, Dictionary<double, double>> res_qhrelation = Item_Info.Get_ResHVrelation();
            Dictionary<string, double> res_damlevel = new Dictionary<string, double>();
            for (int i = 0; i < res_qhrelation.Count; i++)
            {
                res_damlevel.Add(res_qhrelation.ElementAt(i).Key, res_qhrelation.ElementAt(i).Value.Keys.Max());
            }

            //各水库的设计水位
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();
            Dictionary<string, double> res_sjlevel = new Dictionary<string, double>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                if (now_waterlevel[i].Datasource == "水库水文站" && now_waterlevel[i].Level3 != "")
                {
                    res_sjlevel.Add(now_waterlevel[i].Name, double.Parse(now_waterlevel[i].Level3));
                }
            }

            //各水库的堰底高程
            Dictionary<string, double> res_yhd_datumn = new Dictionary<string, double>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                if (now_waterlevel[i].Datasource == "水库水文站" && now_waterlevel[i].Level2 != "")
                {
                    res_yhd_datumn.Add(now_waterlevel[i].Name, double.Parse(now_waterlevel[i].Level2));
                }
            }

            //水库风险总结
            int res_md = 0; int res_yh = 0; int res_sj = 0;
            for (int i = 0; i < res_result.Count; i++)
            {
                //漫坝水库数量
                Reservoir_FloodRes res = res_result.ElementAt(i).Value;
                if (res_damlevel.Keys.Contains(res.ResName))
                {
                    if (res.Max_Level > res_damlevel[res.ResName]) res_md++;
                }

                //溢洪水库数量
                if (res_yhd_datumn.Keys.Contains(res.ResName))
                {
                    if (res.Max_Level > res_yhd_datumn[res.ResName]) res_yh++;
                }

                //超设计水位数量
                if (res_sjlevel.Keys.Contains(res.ResName))
                {
                    if (res.Max_Level > res_sjlevel[res.ResName]) res_sj++;
                }
            }
            res_sj = res_sj - res_md;
            res_yh = res_yh - res_sj;

            string res_md_str = res_md == 0 ? "各水库无漫坝风险" : res_md.ToString() + "座水库有漫坝风险";
            string res_yh_str = res_yh == 0 ? "各水库无溢流泄洪" : res_yh.ToString() + "座水库溢流泄洪";
            string res_sj_str = res_sj == 0 ? "各水库未超设计水位" : res_sj.ToString() + "座水库超设计水位";
            string res_str = "1、" + res_md_str + "，" + res_yh_str + "，" + res_sj_str;
            return res_str;
        }

        //获取结果语音播报文件
        public Dictionary<DateTime, string> Get_ResultSoundFiles(Dictionary<string, Dictionary<DateTime, double>> tsz_discharge,
            Dictionary<string, Dictionary<DateTime, double>> tsz_accumulatedV, Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data)
        {
            HydroModel hydromodel = this;
            return Res11.Get_ResultSoundFiles(hydromodel, tsz_discharge, tsz_accumulatedV, ZdmHD_Data);
        }

        //查询渠道总水量
        public Dictionary<string, Dictionary<DateTime, double>> Get_Reach_Volume()
        {
            HydroModel hydromodel = this;
            return Res11.Get_Reach_Volume(hydromodel);
        }

        //统计引水、退水、分水3类的流量过程
        public Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_AllGate_Discharge()
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllGate_Discharge(this);
        }


        //统计引水、退水、分水3类的累积水量过程(根据流量过程统计)
        public Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_AllGate_AccumulatedVolume(Dictionary<string, 
            Dictionary<string, Dictionary<DateTime, double>>> gate_discharges)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllGate_AccumulatedVolume(gate_discharges);
        }

        //查询水位纵断面数据过程(H点)
        public Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_LevelZdm_Data(ResultData resdata,string model_instance)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Grid_ZdmData(hydromodel, mike11_restype.Water_Level, resdata, model_instance);
        }

        //查询流量纵断面数据过程(Q点)
        public Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_DischargeZdm_Data(ResultData resdata,string model_instance)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Grid_ZdmData(hydromodel,  mike11_restype.Discharge, resdata, model_instance);
        }

        //查询流速纵断面数据过程(H点)
        public Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_SpeedZdm_Data(ResultData resdata,string model_instance)
        {
            HydroModel hydromodel = this;

            return Res11.Get_AllReach_Grid_ZdmData(hydromodel, mike11_restype.Velocity, resdata, model_instance);
        }

        //查询堤顶纵断面数据(H点)
        public Dictionary<string,List<double>> Get_Zdmdd_Data(Dictionary<string, double[]> grid)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Dd_ZdmData(hydromodel,grid);
        }

        //查询渠底纵断面数据(H点)
        public Dictionary<string, List<double>> Get_Zdmqd_Data(Dictionary<string, double[]> grid)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Qd_ZdmData(hydromodel, grid);
        }

        //查询滩地纵断面数据(H点)
        public Dictionary<string, List<double>> Get_Zdmtd_Data(Dictionary<string, double[]> grid)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Td_ZdmData(hydromodel, grid);
        }

        //结果统计信息报表
        public Dictionary<string, List<object[]>> Get_ResStatistics_Table(Dictionary<string, List<float>> H_Chainage_Float,Dictionary<string, Dictionary<DateTime, List<float>>> DischargeZdmHD_Data,
            Dictionary<string, Dictionary<DateTime, List<float>>> LevelZdmHD_Data, Dictionary<string, Dictionary<DateTime, List<float>>> SpeedZdmHD_Data,
            Dictionary<string, List<float>> Zdm_dd_Data, Dictionary<string, List<float>> Zdm_qd_Data)
        {
            HydroModel hydromodel = this;
            return Res11.Get_ResStatistics_Table(hydromodel, H_Chainage_Float, DischargeZdmHD_Data, LevelZdmHD_Data, SpeedZdmHD_Data, Zdm_dd_Data, Zdm_qd_Data);
        }

        #endregion


        #region 二维结果查询操作 -- 获取指定点的指定项目或全部项目数据、洪水风险特征数据
        //查询指定点指定项目的数据 -- 返回一个数据时间序列
        public Dictionary<DateTime, double> Get_Mike21Point_SingleRes(PointXY inputp, Mike21Res_Itemtype res_item)
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_DfsuRes_SingleItem(hydromodel, inputp, res_item);
        }

        //查询指定点所有项目的数据 -- 返回一个数据时间序列
        public Dictionary<DateTime, Dfsu_ItemValue> Get_Mike21Point_AllRes(PointXY inputp)
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_DfsuRes_AllItem(hydromodel, inputp);
        }

        //获取指定位置的洪水风险特征值 -- 最大水深、流速、到达时间(datetime)、淹没历时(timespan)
        public Flood_StaticsValue Get_Mike21Point_StaticsResult(PointXY inputp)
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_Flood_StaticsValue(hydromodel, inputp);
        }
        #endregion


        #region 二维结果切割抽取操作 -- 切割出指定矩形区域dfsu文件、抽取湿区域dfsu文件、抽取指定点项目数据生成dfs0文件
        //从dfsu结果中切割出指定矩形区域的dfsu -- 返回子区域dfsu路径
        public string Extract_Subarea_Dfsu(SubArea subarea)
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_Subarea_Dfsu(hydromodel, subarea);
        }

        //从dfsu结果中抽取出大于水深阀值湿区域的dfsu -- 返回湿区域dfsu路径
        public string Extract_Wetarea_Dfsu()
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_Wetarea_Dfsu(hydromodel);
        }

        //从dfsu结果中抽取指定项生成dfs0 -- 返回dfs0文件路径
        public string Extract_Dfs0_SingleItem(PointXY inputp, Mike21Res_Itemtype res_itemtype)
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_Dfs0_SingleItem(hydromodel, inputp, res_itemtype);
        }

        //从dfsu结果中抽取指定项生成dfs0 -- 返回dfs0文件路径
        public string Extract_Dfs0_AllItem(PointXY inputp)
        {
            HydroModel hydromodel = this;
            return Dfsu.Extract_Dfs0_AllItem(hydromodel, inputp);
        }
        #endregion


        #region 二维结果动态演进数据处理操作 -- 调用新进程和GIS服务，共同处理二维结果数据，生成时态数据GIS图层
        //调用新进程处理dfsu数据，生成时态数据GIS图层
        public void Create_NewProcess_DfsuToGis()
        {
            HydroModel hydromodel = this;
            Dfsu.Create_NewProcess_DfsuToGis(hydromodel);
        }
        #endregion

    }

}