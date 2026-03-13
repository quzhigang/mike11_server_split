using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using DHI.Generic.MikeZero;
using Kdbndp;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model.Mike21;
using Newtonsoft.Json;

namespace bjd_model.CatchMent
{

    //降雨文件和蒸发文件
    [Serializable]
    public struct Rfeva_FileItem
    {
        public string rain_filename;  //降雨文件地址
        public string rain_itemname;  //降雨项目名
        public string eva_filename;  //蒸发文件地址
        public string eva_itemname;  //蒸发项目名
    }

    //设计降雨信息
    [Serializable]
    public struct DesignRF_Info
    {
        public DesignRFLevel designrf_level;
        public DesignRF_TimeSpan designrf_timespan;
    }

    //设计降雨量级
    [Serializable]
    public enum DesignRFLevel
    {
        level_5nian = 5,
        level_10nian = 10,
        level_20nian = 20,
        level_50nian = 50,
        level_100nian = 100
    }

    //设计降雨历时
    [Serializable]
    public enum DesignRF_TimeSpan
    {
        level_1day = 1,
        level_3day = 3,
        level_7day = 7,
        level_15day = 15
    }

    // 水文模型枚举
    [Serializable]
    public enum RFModelType
    {
        NAM,
        UHM,
        XAJ
    }

    //蒸发还是降雨
    [Serializable]
    public enum Eva_RF
    {
        eva,
        rainfall
    }

    // NAM模型9个参数
    [Serializable]
    public struct NAMparameters
    {
        public double U_Max;      //地表蓄水能力 10~20mm
        public double L_Max;      //根区蓄水能力 50~250mm
        public double CQOF;       //径流系数  0.01~0.9
        public double CKIF;       //壤中流汇流时间常量，500~1000hour
        public double CK1;        //地表径流汇流时间常量 决定洪峰尖锐和毯化的程度的时间常量,越小越尖锐 3~46hour
        public double TOF;        //产生地表径流的根区临界土壤含水率  0~0.7
        public double TIF;        //产生壤中流的根区临界土壤含水率  0~0.7

        public double TG;         //产生基流的根区临界土壤含水率  0~0.7
        public double CKBF;       //基流汇流时间常量  500~5000hour
    }

    // NAM模型初始条件
    [Serializable]
    public struct Nam_InitialCondition
    {
        public double U_Ini;     //初始地表储水量占地表总可蓄水量的比值
        public double L_Ini;     //初始根区储水量占根区总可蓄水量的比值
        public double OF_Ini;    //坡面流初始流量  m3/s
        public double IF_Ini;    //壤中流初始流量  m3/s
        public double BFlow;     //基流初始流量   m3/s
    }

    // UHM模型参数
    [Serializable]
    public struct UHMparameters
    {
        public double Area_RF;       //面积调整系数
        public double Baseflow;     //基流  m3/s
        public double InitLoss;     //初损  mm
        public double ConstLoss;    //沿损  mm/hour 
        public double Tlag;         //时间延后 hour 
    }

    // 新安江模型初始条件
    [Serializable]
    public struct XajInitialConditional
    {
        public double ua_day;// 上层土壤初始含水量（一般以4月1日的土壤含水量为初始土壤含水量）
        public double la_day;// 下层土壤初始含水量
        public double da_day;// 深层土壤初始含水量

        public double baseFlow;  //基流
    }

    // 新安江模型参数
    [Serializable]
    public struct Xajparameters
    {
        public double Sm;     //张力水 蓄水容量
        public double Wm;     //土壤蓄水容量
        public double Um;     //上层蓄水容量 
        public double Lm;     //下层蓄水容量  
        public double B;      //张力水蓄水容量曲线的方次 
        public double Im;     //不透水的面积比例      0.01~0.02
        public double K;      //蒸发能力折算系数
        public double C;      //深层蒸发系数
        public double Ki;     //自由水蓄水水库对壤中流的出流系数
        public double Ex;     //表土自由水蓄水容量曲线的方次
        public double Ci;     //壤中流消退系数
        public double Cg;     //地下径流消退系数                          值越小  峰值越大   对退水的影响较小
        public double Cs;     //地表径流消退系数                   降低洪量比较敏感  值越大 消退越慢 
        public double L;      // 滞后时段数,增大使峰值向后，并有降低洪峰的作用。
        public double Cr;     //河网蓄水消退系数

        public static Xajparameters Get_Default_XAJpars()
        {
            Xajparameters paras;
            paras.Sm = 30;
            paras.Wm = 130;
            paras.Um = 15;
            paras.Lm = 50;
            paras.B = 0.25;
            paras.Im = 0.02;
            paras.K = 0.9;
            paras.C = 0.11;
            paras.Ki = 0.29;
            paras.Ex = 1.5;
            paras.Ci = 0.9;
            paras.Cg = 0.9;
            paras.Cs = 0.5;
            paras.L = 3.5;
            paras.Cr = 0.15;
            return paras;
        }
    }


    public class RR11
    {
        #region ****************************从默认rr11文件中获取集水区信息*********************************
        //从默认rr11文件中获取集水区详细信息
        public static void GetCatchmentInfo(string sourcefilename, ref HydroModel hydromodel)
        {
            CatchmentList Catchment_List = hydromodel.RfPars.Catchmentlist;
            string modelname = hydromodel.Modelname;
            SimulateTime simulate_time = hydromodel.ModelGlobalPars.Simulate_time;

            PFSFile pfsfile = new PFSFile(sourcefilename);   //读取文件
            PFSSection target = pfsfile.GetTarget("MIKE_RR", 1);   //最外面的节
            PFSSection CatchList = target.GetSection("CatchList", 1);
            PFSSection Parameterlist = target.GetSection("ParameterList", 1);
            PFSSection Timeserieslist = target.GetSection("TimeseriesList", 1);

            List<CatchmentInfo> catchmentlist = new List<CatchmentInfo>();
            for (int i = 0; i < CatchList.GetSectionsCount(); i++)
            {
                PFSSection catchment = CatchList.GetSection("Catchment", i + 1);
                if (catchment.GetKeyword("Catchment_Model").GetParameter(1).ToString() == "NAM")
                {
                    //获取流域基本情况--名称、面积、ID、模型，并根据模型类别新建相应水文模型对象
                    string catchment_name = catchment.GetKeyword("Catchment_Name").GetParameter(1).ToString();
                    double catchment_area = catchment.GetKeyword("Catchment_Area").GetParameter(1).ToDouble();
                    int catchment_id = catchment.GetKeyword("Number_ID").GetParameter(1).ToInt();

                    //先获取流域的边界坐标集合
                    List<PointXY> catchmentpointlist = RFService.GetCatchment_Pointdic(catchment_name);

                    //构建流域
                    CatchmentInfo catchmentinfo = new CatchmentInfo(catchment_id, catchment_name, catchmentpointlist);
                    catchmentinfo.Now_RfmodelType = RFModelType.NAM;

                    Nam nammodel = new Nam(catchmentinfo);
                    //获取模型参数
                    PFSSection parametersec = Parameterlist.GetSection(i + 1).GetSection("SurfaceRootzone", 1);
                    NAMparameters parameters;
                    parameters.U_Max = parametersec.GetKeyword("U_Max").GetParameter(1).ToDouble();
                    parameters.L_Max = parametersec.GetKeyword("L_Max").GetParameter(1).ToDouble();
                    parameters.CQOF = parametersec.GetKeyword("CQOF").GetParameter(1).ToDouble();
                    parameters.CKIF = parametersec.GetKeyword("CKIF").GetParameter(1).ToDouble();
                    parameters.CK1 = parametersec.GetKeyword("CK1").GetParameter(1).ToDouble();
                    parameters.TOF = parametersec.GetKeyword("TOF").GetParameter(1).ToDouble();
                    parameters.TIF = parametersec.GetKeyword("TIF").GetParameter(1).ToDouble();
                    PFSSection parametersec1 = Parameterlist.GetSection(i + 1).GetSection("GroundWater", 1);
                    parameters.TG = parametersec1.GetKeyword("TG").GetParameter(1).ToDouble();
                    parameters.CKBF = parametersec1.GetKeyword("CKBF").GetParameter(1).ToDouble();

                    nammodel.NAMpar = parameters;

                    //获取模型初始条件
                    PFSSection parametersec2 = Parameterlist.GetSection(i + 1).GetSection("InitialCondition", 1);
                    Nam_InitialCondition initial;
                    initial.U_Ini = parametersec2.GetKeyword("U_Ini").GetParameter(1).ToDouble();
                    initial.L_Ini = parametersec2.GetKeyword("L_Ini").GetParameter(1).ToDouble();
                    initial.OF_Ini = parametersec2.GetKeyword("OF_Ini").GetParameter(1).ToDouble();
                    initial.IF_Ini = parametersec2.GetKeyword("IF_Ini").GetParameter(1).ToDouble();
                    initial.BFlow = parametersec2.GetKeyword("GWL_Ini").GetParameter(1).ToDouble();
                    nammodel.NAMInitial = initial;

                    //重新给流域的产汇流模型赋值
                    catchmentinfo.Now_Rfmodel = nammodel;

                    //流域的产汇流集合要更新
                    catchmentinfo.Update_Rfmodellist();

                    //加入流域集合
                    catchmentlist.Add(catchmentinfo);
                }
                else if (catchment.GetKeyword("Catchment_Model").GetParameter(1).ToString() == "UHM")
                {
                    //获取流域基本情况--名称、面积、ID、模型，并根据模型类别新建相应水文模型对象
                    string catchment_name = catchment.GetKeyword("Catchment_Name").GetParameter(1).ToString();
                    double catchment_area = catchment.GetKeyword("Catchment_Area").GetParameter(1).ToDouble();
                    int catchment_id = catchment.GetKeyword("Number_ID").GetParameter(1).ToInt();

                    //先获取流域的边界坐标集合
                    List<PointXY> catchmentpointlist = RFService.GetCatchment_Pointdic(catchment_name);

                    //获取降雨时间序列文件路径
                    Rfeva_FileItem rfeva_fileitem = Get_RfEvafilename(Timeserieslist, catchment_name);

                    //构建流域 
                    CatchmentInfo catchmentinfo = new CatchmentInfo(catchment_id, catchment_name, catchmentpointlist);
                    catchmentinfo.Now_RfmodelType = RFModelType.UHM;

                    Uhm uhmmodel = new Uhm(catchmentinfo);
                    //获取模型参数
                    PFSSection parametersec = Parameterlist.GetSection(i + 1);
                    UHMparameters parameters;
                    parameters.Area_RF = parametersec.GetKeyword("Area_RF").GetParameter(1).ToDouble();
                    parameters.Baseflow = parametersec.GetKeyword("Baseflow").GetParameter(1).ToDouble();
                    parameters.InitLoss = parametersec.GetKeyword("InitLoss").GetParameter(1).ToDouble();
                    parameters.ConstLoss = parametersec.GetKeyword("ConstLoss").GetParameter(1).ToDouble();
                    parameters.Tlag = parametersec.GetKeyword("Tlag").GetParameter(1).ToDouble();
                    uhmmodel.UHMpar = parameters;

                    //重新给流域的产汇流模型赋值
                    catchmentinfo.Now_Rfmodel = uhmmodel;

                    //流域的产汇流集合要更新
                    catchmentinfo.Update_Rfmodellist();

                    //加入流域集合
                    catchmentlist.Add(catchmentinfo);
                }
            }

            Catchment_List.Catchment_infolist = catchmentlist;
            Catchment_List.MaxCatchmentId = Catchment_List.Catchment_infolist.Count;
            Console.WriteLine("流域信息初始化成功!");
            pfsfile.Close();

            //初始化流域的控制点集合
            InitialDefault_CatchmentPointxy(ref  Catchment_List);
            Console.WriteLine("流域控制点集合初始化成功!");
        }

        //获取降雨时间序列文件路径
        public static Rfeva_FileItem Get_RfEvafilename(PFSSection TimeseriesList, string catchmentname)
        {
            Rfeva_FileItem rfeva_fileitem;
            rfeva_fileitem.rain_filename = "";
            rfeva_fileitem.rain_itemname = "";
            rfeva_fileitem.eva_filename = "";
            rfeva_fileitem.eva_itemname = "";

            for (int i = 0; i < TimeseriesList.GetSectionsCount(); i++)
            {
                PFSSection Condition = TimeseriesList.GetSection("Condition", i + 1);
                if (Condition.GetKeyword("Catchment_Name").GetParameter(1).ToString() == catchmentname)
                {
                    if (Condition.GetKeyword("DataType").GetParameter(1).ToInt() == 0)   //降雨
                    {
                        rfeva_fileitem.rain_filename = Condition.GetKeyword("Timeseries").GetParameter(1).ToString();
                        rfeva_fileitem.rain_itemname = Condition.GetSection("TimeSeriesItems", 1).GetKeyword("Item").GetParameter(1).ToString();
                    }
                    else if (Condition.GetKeyword("DataType").GetParameter(1).ToInt() == 1)  //蒸发
                    {
                        rfeva_fileitem.eva_filename = Condition.GetKeyword("Timeseries").GetParameter(1).ToString();
                        rfeva_fileitem.eva_itemname = Condition.GetSection("TimeSeriesItems", 1).GetKeyword("Item").GetParameter(1).ToString();
                    }
                }
            }
            return rfeva_fileitem;
        }

        //从基本资料数据库的默认蒸发站点中读取蒸发数据,用于初始化日平均蒸发量
        public static void GetDefault_AverageEvp()
        {
            Dictionary<int, double> AverageEvaRate = new Dictionary<int, double>();

            //从数据库中获取历史蒸发数据
            DateTime starttime = new DateTime(2001, 1, 1, 0, 0, 0);
            DateTime endtime = new DateTime(2012, 12, 31, 0, 0, 0);
            Dictionary<DateTime, double> evpdic = RFService.GetHistory_Evadic(starttime, endtime);

            //各月平均日蒸发统计
            DateTime[] datetime_array = evpdic.Keys.ToArray();
            double[] evp_array = evpdic.Values.ToArray();

            double sum_evp = 0;
            int sum_day = 0;
            double average_evp = 0;
            for (int i = 0; i < 12; i++)
            {
                int month_int = i + 1;
                for (int j = 0; j < evpdic.Count; j++)
                {
                    if (datetime_array[j].Month == month_int)
                    {
                        sum_evp += evp_array[j];
                        sum_day++;
                    }
                }

                average_evp = sum_evp / sum_day;
                AverageEvaRate.Add(month_int, average_evp);
            }

            //流域日平均蒸发速率初始化
            CatchmentList.AverageEvaRate = AverageEvaRate;

            Console.WriteLine("日平均蒸发量初始化成功!");
        }

        //从基本资料数据库的默认降雨站点中读取降雨数据,用于初始化日平均降雨量
        public static void GetDefault_AverageRF()
        {
            Dictionary<int, double> AverageRFRate = new Dictionary<int, double>();

            //从数据库中获取历史降雨数据
            DateTime starttime = new DateTime(2001, 1, 1, 0, 0, 0);
            DateTime endtime = new DateTime(2012, 12, 31, 0, 0, 0);
            Dictionary<DateTime, double> rfdic = RFService.GetHistory_RFdic(starttime, endtime);

            //各月平均日降雨量统计
            DateTime[] datetime_array = rfdic.Keys.ToArray();
            double[] rf_array = rfdic.Values.ToArray();

            double sum_rf = 0;
            int sum_day = 0;
            double average_rf = 0;
            for (int i = 0; i < 12; i++)
            {
                int month_int = i + 1;
                for (int j = 0; j < rfdic.Count; j++)
                {
                    if (datetime_array[j].Month == month_int)
                    {
                        sum_rf += rf_array[j];
                        sum_day++;
                    }
                }

                average_rf = sum_rf / sum_day;
                AverageRFRate.Add(month_int, average_rf);
            }

            //流域日平均降雨速率初始化
            CatchmentList.AverageRainfallRate = AverageRFRate;

            Console.WriteLine("日平均降雨量初始化成功!");
        }

        //从基本资料数据库的标准模板雨形表中读取数据,用于初始化24小时标准雨形dic
        public static void GetDefault_Stand24_Rfmodeldic()
        {
            //获取标准24小时模板降雨序列
            Dictionary<DateTime, double> rfdic = RFService.GetStand24_RFmodeldic();

            //流域日平均降雨速率初始化
            CatchmentList.Stand24_Rfmodel = rfdic;

            Console.WriteLine("标准24小时雨形模板初始化成功!");
        }

        //从默认流域控制点文件中读取流域控制点坐标，将其坐标集合赋值给默认流域
        public static void InitialDefault_CatchmentPointxy(ref CatchmentList Catchment_List)
        {
            //循环判断流域名称，给各流域添加控制点集合
            for (int i = 0; i < Catchment_List.Catchment_infolist.Count; i++)
            {
                CatchmentInfo catchment = Catchment_List.Catchment_infolist[i];

                List<PointXY> catchmentpointlist = RFService.GetCatchment_Pointdic(catchment.Name);
                catchment.Catchment_Pointlist = catchmentpointlist;
            }
        }
        #endregion ****************************************************************************************


        #region **********************************更新rr11文件*********************************************
        // 提取最新的流域信息和参数、初始条件、降雨时间序列文件，更新RR11文件
        public static void Rewrite_RR11_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Rr11_filename;
            string outputfilename = hydromodel.Modelfiles.Rr11_filename;
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            if (catchmentlist.Catchment_infolist == null) return;

            //打开PFS文件，开始编辑
            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection MIKE_RR = pfsfile.GetTarget("MIKE_RR", 1);   //目标

            //更新流域区域信息节
            Update_CatchListSec(hydromodel, ref MIKE_RR);
            Console.WriteLine("流域基本信息节更新成功!");

            //更新流域参数节
            Update_ParameterListSec(hydromodel, ref MIKE_RR);
            Console.WriteLine("流域参数节更新成功!");

            //更新时间序列节
            Update_TimeseriesSec(hydromodel, ref MIKE_RR);
            Console.WriteLine("时间序列节更新成功!");

            //重新生成rr11文件
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("RR11产汇流参数文件更新成功!");
            Console.WriteLine("");
        }

        //更新流域区域信息节
        public static void Update_CatchListSec(HydroModel hydromodel, ref PFSSection MIKE_RR)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            //清空原默认rr11文件中的流域集合
            MIKE_RR.DeleteSection("CatchList", 1);  //删除的节也是单独排
            PFSSection CatchList = MIKE_RR.InsertNewSection("CatchList", 1);  //重新添加流域集合的节

            //重新逐个添加集水区
            List<CatchmentInfo> catchmentinfolist = catchmentlist.Catchment_infolist;
            if (catchmentinfolist == null) return;

            for (int i = 0; i < catchmentinfolist.Count; i++)
            {
                //逐个添加最新的集水区
                if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM || catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                {
                    PFSSection catchmentsec = CatchList.InsertNewSection("Catchment", i + 1);  //新加集水区的节
                    Insert_CatchMentKey(catchmentinfolist[i], ref catchmentsec);
                }
            }
        }

        //添加流域Catchhment基本信息节
        public static void Insert_CatchMentKey(CatchmentInfo catchment, ref PFSSection catchmentsec)
        {

            PFSKeyword Catchment_Name = catchmentsec.InsertNewKeyword("Catchment_Name", 1);  //声明流域名称
            Catchment_Name.InsertNewParameterString(catchment.Name, 1);  //给流域命名
            PFSKeyword Catchment_Model = catchmentsec.InsertNewKeyword("Catchment_Model", 2);  //声明流域水文模型
            if (catchment.Now_RfmodelType == RFModelType.NAM)
            {
                Catchment_Model.InsertNewParameterString("NAM", 1);  //选择水文模型：NAM或者UHM
            }
            else if (catchment.Now_RfmodelType == RFModelType.UHM)
            {
                Catchment_Model.InsertNewParameterString("UHM", 1);
            }

            PFSKeyword Catchment_Area = catchmentsec.InsertNewKeyword("Catchment_Area", 3);  //声明流域面积
            Catchment_Area.InsertNewParameterDouble(catchment.Area, 1);  //给流域面积赋值
            PFSKeyword Number_ID = catchmentsec.InsertNewKeyword("Number_ID", 4);  //声明添加流域的排序
            Number_ID.InsertNewParameterInt(catchment.Id, 1);  //确定添加流域的序号
            PFSKeyword Additional_output = catchmentsec.InsertNewKeyword("Additional_output", 5);
            Additional_output.InsertNewParameterBool(true, 1);
            PFSKeyword Calibration_plot = catchmentsec.InsertNewKeyword("Calibration_plot", 6);
            Calibration_plot.InsertNewParameterBool(false, 1);
        }

        //更新流域参数集合节
        public static void Update_ParameterListSec(HydroModel hydromodel, ref PFSSection MIKE_RR)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            //清空原默认rr11文件中的流域参数集合
            MIKE_RR.DeleteSection("ParameterList", 1);  //删除的节也是单独排
            PFSSection ParameterList = MIKE_RR.InsertNewSection("ParameterList", 1);  //重新添加流域参数集合的节

            //重新逐个添加集水区参数
            List<CatchmentInfo> catchmentinfolist = catchmentlist.Catchment_infolist;
            if (catchmentinfolist == null) return;

            for (int i = 0; i < catchmentinfolist.Count; i++)
            {
                //逐个添加最新的集水区参数
                if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM || catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                {
                    if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM)
                    {
                        PFSSection NAM_Parameters = ParameterList.InsertNewSection("NAM_Parameters", i + 1);  //新加NAM集水区参数的节
                        Insert_NamparameterSec(ref NAM_Parameters, catchmentinfolist[i].Now_Rfmodel as Nam);
                    }
                    else if (catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                    {
                        PFSSection UHMParameters = ParameterList.InsertNewSection("UHMParameters", i + 1);  //新加UHM集水区参数的节
                        string modeldir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);
                        Insert_UhmparameterSec(ref UHMParameters, modeldir, catchmentinfolist[i].Now_Rfmodel as Uhm);
                    }
                }
            }
        }

        //插入NAM模型参数节 关键字和参数
        public static void Insert_NamparameterSec(ref PFSSection namparsec, Nam Nammodel)
        {
            //地表层参数节
            PFSSection SurfaceRootzone = namparsec.InsertNewSection("SurfaceRootzone", 1); //第4层节：地面径流参数选项卡
            PFSKeyword U_Max = SurfaceRootzone.InsertNewKeyword("U_Max", 1);  //声明地表蓄水容量
            U_Max.InsertNewParameterDouble(Nammodel.NAMpar.U_Max, 1);  //地表蓄水容量，初始值为10
            PFSKeyword L_Max = SurfaceRootzone.InsertNewKeyword("L_Max", 2);  //声明地表蓄水容量
            L_Max.InsertNewParameterDouble(Nammodel.NAMpar.L_Max, 1);  //根区蓄水容量，初始值为100
            PFSKeyword CQOF = SurfaceRootzone.InsertNewKeyword("CQOF", 3);
            CQOF.InsertNewParameterDouble(Nammodel.NAMpar.CQOF, 1);
            PFSKeyword CKIF = SurfaceRootzone.InsertNewKeyword("CKIF", 4);
            CKIF.InsertNewParameterDouble(Nammodel.NAMpar.CKIF, 1);
            PFSKeyword CK1 = SurfaceRootzone.InsertNewKeyword("CK1", 5);
            CK1.InsertNewParameterDouble(Nammodel.NAMpar.CK1, 1);
            PFSKeyword CK12_DIF = SurfaceRootzone.InsertNewKeyword("CK12_DIF", 6);
            CK12_DIF.InsertNewParameterBool(false, 1);
            PFSKeyword CK2 = SurfaceRootzone.InsertNewKeyword("CK2", 7);
            CK2.InsertNewParameterDouble(Nammodel.NAMpar.CK1, 1);
            PFSKeyword TOF = SurfaceRootzone.InsertNewKeyword("TOF", 8);
            TOF.InsertNewParameterDouble(Nammodel.NAMpar.TOF, 1);
            PFSKeyword TIF = SurfaceRootzone.InsertNewKeyword("TIF", 9);
            TIF.InsertNewParameterDouble(Nammodel.NAMpar.TIF, 1);

            //地下水参数节
            PFSSection GroundWater = namparsec.InsertNewSection("GroundWater", 2);//第4层节：地下径流参数选项卡
            PFSKeyword IS_CAREA = GroundWater.InsertNewKeyword("IS_CAREA", 1);
            IS_CAREA.InsertNewParameterBool(false, 1);
            PFSKeyword CAREA = GroundWater.InsertNewKeyword("CAREA", 2);
            CAREA.InsertNewParameterDouble(1, 1);
            PFSKeyword TG = GroundWater.InsertNewKeyword("TG", 3);
            TG.InsertNewParameterDouble(Nammodel.NAMpar.TG, 1);
            PFSKeyword IS_SY = GroundWater.InsertNewKeyword("IS_SY", 4);
            IS_SY.InsertNewParameterBool(false, 1);
            PFSKeyword S_Y = GroundWater.InsertNewKeyword("S_Y", 5);
            S_Y.InsertNewParameterDouble(0.1, 1);
            PFSKeyword CKBF = GroundWater.InsertNewKeyword("CKBF", 6);
            CKBF.InsertNewParameterDouble(Nammodel.NAMpar.CKBF, 1);
            PFSKeyword IS_GWLBF0 = GroundWater.InsertNewKeyword("IS_GWLBF0", 7);
            IS_GWLBF0.InsertNewParameterBool(false, 1);
            PFSKeyword GWLBF0 = GroundWater.InsertNewKeyword("GWLBF0", 8);
            GWLBF0.InsertNewParameterDouble(10, 1);
            PFSKeyword GWLBF0_Season = GroundWater.InsertNewKeyword("GWLBF0_Season", 9);
            GWLBF0_Season.InsertNewParameterBool(false, 1);
            PFSKeyword GWLBF_min = GroundWater.InsertNewKeyword("GWLBF_min", 10);
            GWLBF_min.InsertNewParameterDouble(0, 1);
            PFSKeyword GWLBF = GroundWater.InsertNewKeyword("GWLBF", 11);
            object[] GWLBF_array = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            Nwk11.InsertKeyPars(ref GWLBF, GWLBF_array);
            PFSKeyword Is_GWLBF1 = GroundWater.InsertNewKeyword("Is_GWLBF1", 12);
            Is_GWLBF1.InsertNewParameterBool(false, 1);
            PFSKeyword GWLBF1 = GroundWater.InsertNewKeyword("GWLBF1", 13);
            GWLBF1.InsertNewParameterDouble(0, 1);
            PFSKeyword GWPUMP_Season = GroundWater.InsertNewKeyword("GWPUMP_Season", 14);
            GWPUMP_Season.InsertNewParameterBool(false, 1);
            PFSKeyword GWPUMP_File = GroundWater.InsertNewKeyword("GWPUMP_File", 15);
            GWPUMP_File.InsertNewParameterBool(false, 1);
            PFSKeyword GWPUMP = GroundWater.InsertNewKeyword("GWPUMP", 16);
            object[] GWPUMP_array = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Nwk11.InsertKeyPars(ref GWPUMP, GWPUMP_array);
            PFSKeyword GW_LOWER = GroundWater.InsertNewKeyword("GW_LOWER", 17);
            GW_LOWER.InsertNewParameterBool(false, 1);
            PFSKeyword CQLOW = GroundWater.InsertNewKeyword("CQLOW", 18);
            CQLOW.InsertNewParameterDouble(0, 1);
            PFSKeyword CKLOW = GroundWater.InsertNewKeyword("CKLOW", 19);
            CKLOW.InsertNewParameterDouble(10000, 1);

            //融雪选项卡节
            PFSSection SnowMelt = namparsec.InsertNewSection("SnowMelt", 3);
            PFSKeyword SNOWused = SnowMelt.InsertNewKeyword("SNOWused", 1);
            SNOWused.InsertNewParameterBool(false, 1);
            PFSKeyword C1_SNOW = SnowMelt.InsertNewKeyword("C1_SNOW", 2);
            C1_SNOW.InsertNewParameterDouble(2, 1);
            PFSKeyword T0_SNOW = SnowMelt.InsertNewKeyword("T0_SNOW", 3);
            T0_SNOW.InsertNewParameterDouble(0, 1);
            PFSKeyword C2_Season = SnowMelt.InsertNewKeyword("C2_Season", 4);
            C2_Season.InsertNewParameterBool(false, 1);
            PFSKeyword C2_File = SnowMelt.InsertNewKeyword("C2_File", 5);
            C2_File.InsertNewParameterBool(false, 1);
            PFSKeyword C2_SNOW = SnowMelt.InsertNewKeyword("C2_SNOW", 6);
            object[] C2_SNOW_array = { 1, 1.5, 2, 3, 4, 4.5, 4.5, 4, 3, 2, 1.5, 1 };
            Nwk11.InsertKeyPars(ref C2_SNOW, C2_SNOW_array);

            PFSKeyword C_RAIN_USED = SnowMelt.InsertNewKeyword("C_RAIN_USED", 7);
            C_RAIN_USED.InsertNewParameterBool(false, 1);
            PFSKeyword C_RAIN = SnowMelt.InsertNewKeyword("C_RAIN ", 8);
            C_RAIN.InsertNewParameterDouble(0, 1);
            PFSKeyword C_RADIATION_Used = SnowMelt.InsertNewKeyword("C_RADIATION_Used", 9);
            C_RADIATION_Used.InsertNewParameterBool(false, 1);
            PFSKeyword C_RADIATION = SnowMelt.InsertNewKeyword("C_RADIATION", 10);
            C_RADIATION.InsertNewParameterDouble(0, 1);
            PFSKeyword SNOW_Zones = SnowMelt.InsertNewKeyword("SNOW_Zones", 11);
            SNOW_Zones.InsertNewParameterBool(false, 1);
            PFSSection ZoneList = SnowMelt.InsertNewSection("Zonelist", 1);
            PFSKeyword NO_ZONES = ZoneList.InsertNewKeyword("NO_ZONES", 1);
            NO_ZONES.InsertNewParameterInt(10, 1);
            PFSKeyword T_ELEVREF = ZoneList.InsertNewKeyword("T_ELEVREF", 2);
            T_ELEVREF.InsertNewParameterDouble(0, 1);
            PFSKeyword T_DRYCHECK = ZoneList.InsertNewKeyword("T_DRYCHECK", 3);
            T_DRYCHECK.InsertNewParameterBool(false, 1);
            PFSKeyword T_LAPSEDRY = ZoneList.InsertNewKeyword("T_LAPSEDRY", 4);
            T_LAPSEDRY.InsertNewParameterDouble(-0.6, 1);
            PFSKeyword T_WETCHECK = ZoneList.InsertNewKeyword("T_WETCHECK", 5);
            T_WETCHECK.InsertNewParameterBool(false, 1);
            PFSKeyword T_LAPSEWET = ZoneList.InsertNewKeyword("T_LAPSEWET", 6);
            T_LAPSEWET.InsertNewParameterDouble(-0.4, 1);
            PFSKeyword P_CHECK = ZoneList.InsertNewKeyword("P_CHECK", 7);
            P_CHECK.InsertNewParameterBool(false, 1);
            PFSKeyword P_ELEVREF = ZoneList.InsertNewKeyword("P_ELEVREF", 8);
            P_ELEVREF.InsertNewParameterDouble(0, 1);
            PFSKeyword P_LAPSE = ZoneList.InsertNewKeyword("P_LAPSE", 9);
            P_LAPSE.InsertNewParameterDouble(2, 1);
            for (int i = 1; i < 11; i++)
            {
                PFSSection Zone = ZoneList.InsertNewSection("Zone", i);  //第6层节,第i个zone
                PFSKeyword ZONE_ELEVATION = Zone.InsertNewKeyword("ZONE_ELEVATION", 1);
                ZONE_ELEVATION.InsertNewParameterDouble(100 * i, 1);
                PFSKeyword ZONE_AREA = Zone.InsertNewKeyword("ZONE_AREA", 2);
                ZONE_AREA.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_MINSNOW = Zone.InsertNewKeyword("ZONE_MINSNOW", 3);
                ZONE_MINSNOW.InsertNewParameterDouble(100, 1);
                PFSKeyword ZONE_MAXSNOW = Zone.InsertNewKeyword("ZONE_MAXSNOW", 4);
                ZONE_MAXSNOW.InsertNewParameterDouble(10000, 1);
                PFSKeyword ZONE_MAXWATER = Zone.InsertNewKeyword("ZONE_MAXWATER", 5);
                ZONE_MAXWATER.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_TDRYCOR = Zone.InsertNewKeyword("ZONE_TDRYCOR", 6);
                ZONE_TDRYCOR.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_TWETCOR = Zone.InsertNewKeyword("ZONE_TWETCOR", 7);
                ZONE_TWETCOR.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_PCOR = Zone.InsertNewKeyword("ZONE_PCOR", 8);
                ZONE_PCOR.InsertNewParameterDouble(0, 1);
            }

            //灌溉节
            PFSSection Irrigation = namparsec.InsertNewSection("Irrigation", 4);
            PFSKeyword IRRIGATION = Irrigation.InsertNewKeyword("IRRIGATION", 1);
            IRRIGATION.InsertNewParameterBool(false, 1);
            PFSKeyword K0INF = Irrigation.InsertNewKeyword("K0INF", 2);
            K0INF.InsertNewParameterDouble(1, 1);
            PFSKeyword PCT_LGW = Irrigation.InsertNewKeyword("PCT_LGW", 3);
            PCT_LGW.InsertNewParameterDouble(50, 1);
            PFSKeyword PCT_LRI = Irrigation.InsertNewKeyword("PCT_LRI", 4);
            PCT_LRI.InsertNewParameterDouble(50, 1);
            PFSKeyword PCT_EXT = Irrigation.InsertNewKeyword("PCT_EXT", 5);
            PCT_EXT.InsertNewParameterDouble(0, 1);
            PFSKeyword EXT_RIVERNAME = Irrigation.InsertNewKeyword("EXT_RIVERNAME", 6);
            EXT_RIVERNAME.InsertNewParameterString("", 1);
            PFSKeyword EXT_RIVERCHAIN = Irrigation.InsertNewKeyword("EXT_RIVERCHAIN", 7);
            EXT_RIVERCHAIN.InsertNewParameterDouble(0, 1);
            PFSKeyword IRR_CROPLOSSES = Irrigation.InsertNewKeyword("IRR_CROPLOSSES", 8);
            IRR_CROPLOSSES.InsertNewParameterBool(true, 1);
            PFSKeyword CROP_COEF = Irrigation.InsertNewKeyword("CROP_COEF", 9);
            for (int i = 1; i < 13; i++)
            {
                CROP_COEF.InsertNewParameterDouble(1, i);
            }
            PFSKeyword LOSS_GRW = Irrigation.InsertNewKeyword("LOSS_GRW", 10);
            for (int i = 1; i < 13; i++)
            {
                LOSS_GRW.InsertNewParameterDouble(0, i);
            }
            PFSKeyword LOSS_OF = Irrigation.InsertNewKeyword("LOSS_OF", 11);
            for (int i = 1; i < 13; i++)
            {
                LOSS_OF.InsertNewParameterDouble(0, i);
            }
            PFSKeyword LOSS_EVAP = Irrigation.InsertNewKeyword("LOSS_EVAP", 12);
            for (int i = 1; i < 13; i++)
            {
                LOSS_EVAP.InsertNewParameterDouble(0, i);
            }

            //初始条件节
            PFSSection InitialCondition = namparsec.InsertNewSection("InitialCondition", 5);
            PFSKeyword U_Ini = InitialCondition.InsertNewKeyword("U_Ini", 1);
            U_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.U_Ini, 1);
            PFSKeyword L_Ini = InitialCondition.InsertNewKeyword("L_Ini", 2);
            L_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.L_Ini, 1);
            PFSKeyword OF_Ini = InitialCondition.InsertNewKeyword("OF_Ini", 3);
            OF_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.OF_Ini, 1);
            PFSKeyword IF_Ini = InitialCondition.InsertNewKeyword("IF_Ini", 4);
            IF_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.IF_Ini, 1);
            PFSKeyword GWL_Ini = InitialCondition.InsertNewKeyword("GWL_Ini", 5);
            GWL_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.BFlow, 1);
            PFSKeyword BFlow = InitialCondition.InsertNewKeyword("BFlow", 6);
            BFlow.InsertNewParameterDouble(0, 1);
            PFSKeyword Snow_Ini = InitialCondition.InsertNewKeyword("Snow_Ini", 7);
            Snow_Ini.InsertNewParameterDouble(0, 1);
            PFSSection Zonelist = InitialCondition.InsertNewSection("Zonelist", 1);  //第5层节
            for (int i = 1; i < 11; i++)
            {
                PFSSection Zone = Zonelist.InsertNewSection("Zone", i);
                PFSKeyword snow_Ini = Zone.InsertNewKeyword("Snow_Ini", 1);
                snow_Ini.InsertNewParameterDouble(0, 1);
                PFSKeyword Water_Ini = Zone.InsertNewKeyword("Water_Ini", 2);
                Water_Ini.InsertNewParameterDouble(0, 1);
            }

            //自动率定及参数范围节
            PFSSection AutoCal = namparsec.InsertNewSection("AutoCal", 6);
            PFSKeyword AUTOCAL = AutoCal.InsertNewKeyword("AUTOCAL", 1);
            AUTOCAL.InsertNewParameterBool(false, 1);
            PFSKeyword U_MAX_fit = AutoCal.InsertNewKeyword("U_MAX_fit ", 2);
            U_MAX_fit.InsertNewParameterBool(true, 1);
            PFSKeyword U_Max_upper = AutoCal.InsertNewKeyword("U_Max_upper", 3);
            U_Max_upper.InsertNewParameterDouble(20, 1);
            PFSKeyword U_Max_lower = AutoCal.InsertNewKeyword("U_Max_lower", 4);
            U_Max_lower.InsertNewParameterDouble(10, 1);
            PFSKeyword L_MAX_fit = AutoCal.InsertNewKeyword("L_MAX_fit", 5);
            L_MAX_fit.InsertNewParameterBool(true, 1);
            PFSKeyword L_Max_upper = AutoCal.InsertNewKeyword("L_Max_upper", 6);
            L_Max_upper.InsertNewParameterDouble(300, 1);
            PFSKeyword L_Max_lower = AutoCal.InsertNewKeyword("L_Max_lower", 7);
            L_Max_lower.InsertNewParameterDouble(100, 1);
            PFSKeyword CQOF_fit = AutoCal.InsertNewKeyword("CQOF_fit", 8);
            CQOF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CQOF_upper = AutoCal.InsertNewKeyword("CQOF_upper", 9);
            CQOF_upper.InsertNewParameterDouble(1, 1);
            PFSKeyword CQOF_lower = AutoCal.InsertNewKeyword("CQOF_lower", 10);
            CQOF_lower.InsertNewParameterDouble(0.1, 1);
            PFSKeyword CKIF_fit = AutoCal.InsertNewKeyword("CKIF_fit", 11);
            CKIF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CKIF_upper = AutoCal.InsertNewKeyword("CKIF_upper", 12);
            CKIF_upper.InsertNewParameterDouble(1000, 1);
            PFSKeyword CKIF_lower = AutoCal.InsertNewKeyword("CKIF_lower", 13);
            CKIF_lower.InsertNewParameterDouble(200, 1);
            PFSKeyword CK1_fit = AutoCal.InsertNewKeyword("CK1_fit", 14);
            CK1_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CK1_upper = AutoCal.InsertNewKeyword("CK1_upper", 15);
            CK1_upper.InsertNewParameterDouble(50, 1);
            PFSKeyword CK1_lower = AutoCal.InsertNewKeyword("CK1_lower", 16);
            CK1_lower.InsertNewParameterDouble(10, 1);
            PFSKeyword TOF_fit = AutoCal.InsertNewKeyword("TOF_fit", 17);
            TOF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword TOF_upper = AutoCal.InsertNewKeyword("TOF_upper", 18);
            TOF_upper.InsertNewParameterDouble(0.99, 1);
            PFSKeyword TOF_lower = AutoCal.InsertNewKeyword("TOF_lower", 19);
            TOF_lower.InsertNewParameterDouble(0, 1);
            PFSKeyword TIF_fit = AutoCal.InsertNewKeyword("TIF_fit", 20);
            TIF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword TIF_upper = AutoCal.InsertNewKeyword("TIF_upper", 21);
            TIF_upper.InsertNewParameterDouble(0.99, 1);
            PFSKeyword TIF_lower = AutoCal.InsertNewKeyword("TIF_lower", 22);
            TIF_lower.InsertNewParameterDouble(0, 1);
            PFSKeyword TG_fit = AutoCal.InsertNewKeyword("TG_fit", 23);
            TG_fit.InsertNewParameterBool(true, 1);
            PFSKeyword TG_upper = AutoCal.InsertNewKeyword("TG_upper", 24);
            TG_upper.InsertNewParameterDouble(0.99, 1);
            PFSKeyword TG_lower = AutoCal.InsertNewKeyword("TG_lower", 25);
            TG_lower.InsertNewParameterDouble(0, 1);
            PFSKeyword CKBF_fit = AutoCal.InsertNewKeyword("CKBF_fit", 26);
            CKBF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CKBF_upper = AutoCal.InsertNewKeyword("CKBF_upper", 27);
            CKBF_upper.InsertNewParameterDouble(4000, 1);
            PFSKeyword CKBF_lower = AutoCal.InsertNewKeyword("CKBF_lower", 28);
            CKBF_lower.InsertNewParameterDouble(1000, 1);
            PFSKeyword CK2_fit = AutoCal.InsertNewKeyword("CK2_fit", 29);
            CK2_fit.InsertNewParameterBool(false, 1);
            PFSKeyword CK2_upper = AutoCal.InsertNewKeyword("CK2_upper", 30);
            CK2_upper.InsertNewParameterDouble(50, 1);
            PFSKeyword CK2_lower = AutoCal.InsertNewKeyword("CK2_lower", 31);
            CK2_lower.InsertNewParameterDouble(10, 1);
            PFSKeyword CQLOW_fit = AutoCal.InsertNewKeyword("CQLOW_fit", 32);
            CQLOW_fit.InsertNewParameterBool(false, 1);
            PFSKeyword CQLOW_upper = AutoCal.InsertNewKeyword("CQLOW_upper", 33);
            CQLOW_upper.InsertNewParameterDouble(100, 1);
            PFSKeyword CQLOW_lower = AutoCal.InsertNewKeyword("CQLOW_lower", 34);
            CQLOW_lower.InsertNewParameterDouble(1, 1);
            PFSKeyword CKLOW_fit = AutoCal.InsertNewKeyword("CKLOW_fit", 35);
            CKLOW_fit.InsertNewParameterBool(false, 1);
            PFSKeyword CKLOW_upper = AutoCal.InsertNewKeyword("CKLOW_upper", 36);
            CKLOW_upper.InsertNewParameterDouble(30000, 1);
            PFSKeyword CKLOW_lower = AutoCal.InsertNewKeyword("CKLOW_lower", 37);
            CKLOW_lower.InsertNewParameterDouble(1000, 1);
            PFSKeyword WBL = AutoCal.InsertNewKeyword("WBL", 38);
            WBL.InsertNewParameterBool(true, 1);
            PFSKeyword RMSE = AutoCal.InsertNewKeyword("RMSE", 39);
            RMSE.InsertNewParameterBool(true, 1);
            PFSKeyword Peak_flow_RMSE = AutoCal.InsertNewKeyword("Peak_flow_RMSE", 40);
            Peak_flow_RMSE.InsertNewParameterBool(false, 1);
            PFSKeyword Low_flow_RMSE = AutoCal.InsertNewKeyword("Low_flow_RMSE", 41);
            Low_flow_RMSE.InsertNewParameterBool(false, 1);
            PFSKeyword Peak_flow_min = AutoCal.InsertNewKeyword("Peak_flow_min", 42);
            Peak_flow_min.InsertNewParameterDouble(0, 1);
            PFSKeyword Low_flow_max = AutoCal.InsertNewKeyword("Low_flow_max", 43);
            Low_flow_max.InsertNewParameterDouble(0, 1);
            PFSKeyword Maximum_evaluation = AutoCal.InsertNewKeyword("Maximum_evaluation", 44);
            Maximum_evaluation.InsertNewParameterDouble(2000, 1);
            PFSKeyword Initial_excluded = AutoCal.InsertNewKeyword("Initial_excluded", 45);
            Initial_excluded.InsertNewParameterDouble(0, 1);
        }

        //插入UHM参数节 关键字和参数
        public static void Insert_UhmparameterSec(ref PFSSection UHMParameters, string modeldir, Uhm uhmmodel)
        {
            PFSKeyword Area_RF = UHMParameters.InsertNewKeyword("Area_RF", 1);
            Area_RF.InsertNewParameterDouble(uhmmodel.UHMpar.Area_RF, 1);
            PFSKeyword Baseflow = UHMParameters.InsertNewKeyword("Baseflow", 2);
            Baseflow.InsertNewParameterDouble(uhmmodel.UHMpar.Baseflow, 1);
            PFSKeyword LossModel = UHMParameters.InsertNewKeyword("LossModel", 3);
            LossModel.InsertNewParameterDouble(0, 1);
            PFSKeyword InitLoss = UHMParameters.InsertNewKeyword("InitLoss", 4);
            InitLoss.InsertNewParameterDouble(uhmmodel.UHMpar.InitLoss, 1);
            PFSKeyword ConstLoss = UHMParameters.InsertNewKeyword("ConstLoss", 5);
            ConstLoss.InsertNewParameterDouble(uhmmodel.UHMpar.ConstLoss, 1);
            PFSKeyword RunoffCoef = UHMParameters.InsertNewKeyword("RunoffCoef", 6);
            RunoffCoef.InsertNewParameterDouble(0.75, 1);
            PFSKeyword LossCurveNumber = UHMParameters.InsertNewKeyword("LossCurveNumber", 7);
            LossCurveNumber.InsertNewParameterDouble(75, 1);
            PFSKeyword InitialAMC = UHMParameters.InsertNewKeyword("InitialAMC", 8);
            InitialAMC.InsertNewParameterDouble(2, 1);
            PFSKeyword Hydrograph = UHMParameters.InsertNewKeyword("Hydrograph", 9);
            Hydrograph.InsertNewParameterDouble(2, 1);
            PFSKeyword Filename = UHMParameters.InsertNewKeyword("Filename", 10);

            //各流域的单位线文件命名规则相同
            string uhmfilename = modeldir + @"\" + uhmmodel.Catchment_Name + "_uhm.dfs0";

            Filename.InsertNewParameterFileName(uhmfilename, 1);   //******输入单位水文线文件路径
            PFSKeyword Item = UHMParameters.InsertNewKeyword("Item", 11);
            Item.InsertNewParameterString("hydrograph", 1);  //******根据输入单位水文线项目名，统一为 hydrograph，与dfs0类中的常用时间序列文件相同
            Item.InsertNewParameterDouble(0, 2);
            PFSKeyword LagTime = UHMParameters.InsertNewKeyword("LagTime", 12);
            LagTime.InsertNewParameterDouble(0, 1);
            PFSKeyword Tlag = UHMParameters.InsertNewKeyword("Tlag", 13);
            Tlag.InsertNewParameterDouble(uhmmodel.UHMpar.Tlag, 1);
            PFSKeyword HLength = UHMParameters.InsertNewKeyword("HLength", 14);
            HLength.InsertNewParameterDouble(10, 1);
            PFSKeyword Slope = UHMParameters.InsertNewKeyword("Slope", 15);
            Slope.InsertNewParameterDouble(5, 1);
            PFSKeyword LagCurveNumber = UHMParameters.InsertNewKeyword("LagCurveNumber", 16);
            LagCurveNumber.InsertNewParameterDouble(75, 1);
            PFSKeyword InitialAbstractionDepth = UHMParameters.InsertNewKeyword("InitialAbstractionDepth", 17);
            InitialAbstractionDepth.InsertNewParameterDouble(1, 1);
            PFSSection UHM_EffectiveRainfall_Parameters = UHMParameters.InsertNewSection("UHM_EffectiveRainfall_Parameters", 1);  //第4层节
            PFSKeyword RainfallEnlargementNumber = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("RainfallEnlargementNumber", 1);
            RainfallEnlargementNumber.InsertNewParameterDouble(0, 1);
            PFSKeyword EffectiveRainfallNumber = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("EffectiveRainfallNumber", 2);
            EffectiveRainfallNumber.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantK = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantK", 3);
            BasinConstantK.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantP = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantP", 4);
            BasinConstantP.InsertNewParameterDouble(0, 1);
            PFSKeyword TimeOfDelay = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("TimeOfDelay", 5);
            TimeOfDelay.InsertNewParameterDouble(0, 1);
            PFSKeyword f1 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("f1", 6);
            f1.InsertNewParameterDouble(0, 1);
            PFSKeyword Rsa = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("Rsa", 7);
            Rsa.InsertNewParameterDouble(0, 1);
            PFSKeyword f2 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("f2", 8);
            f2.InsertNewParameterDouble(0, 1);
            PFSKeyword QLSFLossMethod = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("QLSFLossMethod", 9);
            QLSFLossMethod.InsertNewParameterDouble(0, 1);
            PFSKeyword LandUseArea = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("LandUseArea", 10);
            LandUseArea.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantT1 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantT1", 11);
            BasinConstantT1.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantT03 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantT03", 12);
            BasinConstantT03.InsertNewParameterDouble(0, 1);
            PFSKeyword TimeConcentration = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("TimeConcentration", 13);
            TimeConcentration.InsertNewParameterDouble(0, 1);
            PFSKeyword RationalRunoffCoef = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("RationalRunoffCoef", 14);
            RationalRunoffCoef.InsertNewParameterDouble(0, 1);
            PFSSection UHM_KinematicWave_Parameters = UHMParameters.InsertNewSection("UHM_KinematicWave_Parameters", 2);  //第4层节
            PFSKeyword ANS1 = UHM_KinematicWave_Parameters.InsertNewKeyword("ANS1", 1);
            ANS1.InsertNewParameterDouble(0, 1);
            PFSKeyword AL = UHM_KinematicWave_Parameters.InsertNewKeyword("AL", 2);
            AL.InsertNewParameterDouble(0, 1);
            PFSKeyword ALR = UHM_KinematicWave_Parameters.InsertNewKeyword("ALR", 3);
            ALR.InsertNewParameterDouble(0, 1);
            PFSKeyword SI = UHM_KinematicWave_Parameters.InsertNewKeyword("SI", 4);
            SI.InsertNewParameterDouble(0, 1);
            PFSKeyword ANS2 = UHM_KinematicWave_Parameters.InsertNewKeyword("ANS2", 5);
            ANS2.InsertNewParameterDouble(0, 1);
            PFSKeyword SLO = UHM_KinematicWave_Parameters.InsertNewKeyword("SLO", 6);
            SLO.InsertNewParameterDouble(0, 1);
        }

        //更新时间序列节
        public static void Update_TimeseriesSec(HydroModel hydromodel, ref PFSSection MIKE_RR)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            //清空原默认rr11文件中的时间序列
            MIKE_RR.DeleteSection("TimeseriesList", 1);  //删除的节也是单独排
            PFSSection TimeseriesList = MIKE_RR.InsertNewSection("TimeseriesList", 1);  //重新添加时间序列集合的节
            TimeseriesList.InsertNewKeyword("Max_Comb_Number", 1).InsertNewParameterInt(8, 1);

            //重新逐个添加时间序列条件节
            List<CatchmentInfo> catchmentinfolist = catchmentlist.Catchment_infolist;
            if (catchmentinfolist == null) return;

            int conditionnumber = 1;
            string modeldir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);
            for (int i = 0; i < catchmentinfolist.Count; i++)
            {
                //逐个添加最新集水区的时间序列节
                if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM || catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                {
                    if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM)
                    {
                        //添加降雨时间序列条件
                        PFSSection Condition1 = TimeseriesList.InsertNewSection("Condition", conditionnumber);  //新加集水区时间序列的节
                        Insert_RainConditionKey(ref Condition1, catchmentinfolist[i].Name, modeldir);
                        conditionnumber++;

                        //添加蒸发时间序列条件
                        PFSSection Condition2 = TimeseriesList.InsertNewSection("Condition", conditionnumber);
                        Insert_EvpConditionKey(ref Condition2, catchmentinfolist[i].Name, modeldir);
                        conditionnumber++;
                    }
                    else if (catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                    {
                        //只添加降雨时间序列条件
                        PFSSection Condition = TimeseriesList.InsertNewSection("Condition", conditionnumber);
                        Insert_RainConditionKey(ref Condition, catchmentinfolist[i].Name, modeldir);
                        conditionnumber++;
                    }
                }
            }
        }

        //插入降雨时间序列节关键字和参数
        public static void Insert_RainConditionKey(ref PFSSection Condition, string catchmentname, string modeldir)
        {
            PFSKeyword catchmentNameRain = Condition.InsertNewKeyword("Catchment_Name", 1);
            catchmentNameRain.InsertNewParameterString(catchmentname, 1);
            PFSKeyword MAWCalculationRain = Condition.InsertNewKeyword("MAWCalculation", 2);
            MAWCalculationRain.InsertNewParameterBool(false, 1);
            PFSKeyword dataTypeRain = Condition.InsertNewKeyword("DataType", 3);
            dataTypeRain.InsertNewParameterInt(0, 1);
            PFSKeyword timeSeriesRain = Condition.InsertNewKeyword("Timeseries", 4);

            //各方案统一的流域降雨时间序列路径
            string catchment_rf_filename = modeldir + @"\" + catchmentname + "_rf.dfs0";

            timeSeriesRain.InsertNewParameterFileName(catchment_rf_filename, 1);
            PFSSection timeSeriesItemsRain = Condition.InsertNewSection("TimeSeriesItems", 1);
            PFSKeyword itemRain = timeSeriesItemsRain.InsertNewKeyword("Item", 1);
            itemRain.InsertNewParameterString("rainfall", 1);   //项目名称统一用rainfall，这个在dfs0类中常见时间序列定义的
            itemRain.InsertNewParameterInt(1, 2);
            PFSSection weightedAverageTimeseriesRain = Condition.InsertNewSection("Weighted_average_timeseries", 2);
            PFSSection weightedAverageCombinationsRain = Condition.InsertNewSection("Weighted_average_combinations", 3);
            PFSSection temporalDistributionTimeseriesRain = Condition.InsertNewSection("Temporal_distribution_timeseries", 4);
            PFSSection temporalDistributionCombinationsRain = Condition.InsertNewSection("Temporal_distribution_combinations", 5);
        }

        //插入蒸发时间序列节关键字和参数
        public static void Insert_EvpConditionKey(ref PFSSection Condition, string catchmentname, string modeldir)
        {
            PFSKeyword catchmentNameRain = Condition.InsertNewKeyword("Catchment_Name", 1);
            catchmentNameRain.InsertNewParameterString(catchmentname, 1);
            PFSKeyword MAWCalculationRain = Condition.InsertNewKeyword("MAWCalculation", 2);
            MAWCalculationRain.InsertNewParameterBool(false, 1);
            PFSKeyword dataTypeRain = Condition.InsertNewKeyword("DataType", 3);
            dataTypeRain.InsertNewParameterInt(1, 1);
            PFSKeyword timeSeriesRain = Condition.InsertNewKeyword("Timeseries", 4);

            //各方案各流域统一的蒸发时间序列路径
            string evp_filename = modeldir + @"\" + "evp.dfs0";

            timeSeriesRain.InsertNewParameterFileName(evp_filename, 1);
            PFSSection timeSeriesItemsRain = Condition.InsertNewSection("TimeSeriesItems", 1);
            PFSKeyword itemRain = timeSeriesItemsRain.InsertNewKeyword("Item", 1);
            itemRain.InsertNewParameterString("evaporation", 1);   //项目名称统一用evaporation，这个在dfs0类中常见时间序列定义的
            itemRain.InsertNewParameterInt(1, 2);
            PFSSection weightedAverageTimeseriesRain = Condition.InsertNewSection("Weighted_average_timeseries", 2);
            PFSSection weightedAverageCombinationsRain = Condition.InsertNewSection("Weighted_average_combinations", 3);
            PFSSection temporalDistributionTimeseriesRain = Condition.InsertNewSection("Temporal_distribution_timeseries", 4);
            PFSSection temporalDistributionCombinationsRain = Condition.InsertNewSection("Temporal_distribution_combinations", 5);
        }
        #endregion ******************************************************************************************


        #region ************ 流域操作 -- 新增流域和其产汇流模型，改变各流域产汇流模型类型******************
        // 新增流域和其产汇流模型 -- 模型参数采用默认值 
        public static void Add_NewCatchment(ref HydroModel hydromodel, string CatchmentName, List<PointXY> catchment_pointlist, RFModelType select_model)
        {
            //新建流域对象 -- 此时会初始化3个产汇流模型
            int catchmentid;
            if (hydromodel.RfPars.Catchmentlist.Catchment_infolist == null)
            {
                catchmentid = 1;
            }
            else
            {
                catchmentid = hydromodel.RfPars.Catchmentlist.Catchment_infolist.Count + 1;
            }
            string modelname = hydromodel.Modelname;
            SimulateTime simulate_time = hydromodel.ModelGlobalPars.Simulate_time;
            CatchmentInfo newcatchmentinfo = new CatchmentInfo(catchmentid, CatchmentName, catchment_pointlist);

            //将流域对象加入集合 -- 此时会更新雨量站和流域关系数据库
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            catchmentlist.AddCatchment(newcatchmentinfo);

            //根据RF模型选择，重新选择当前的产汇流模型
            switch (select_model)
            {
                case RFModelType.NAM:
                    //给新流域所采用的RF模型重新赋值
                    newcatchmentinfo.Now_RfmodelType = RFModelType.NAM;
                    for (int i = 0; i < newcatchmentinfo.Rfmodel_List.Count; i++)
                    {
                        if (newcatchmentinfo.Rfmodel_List[i] is Nam)
                        {
                            newcatchmentinfo.Now_Rfmodel = newcatchmentinfo.Rfmodel_List[i];
                            break;
                        }
                    }
                    break;
                case RFModelType.UHM:
                    //给新流域所采用的RF模型重新赋值
                    newcatchmentinfo.Now_RfmodelType = RFModelType.UHM;
                    for (int i = 0; i < newcatchmentinfo.Rfmodel_List.Count; i++)
                    {
                        if (newcatchmentinfo.Rfmodel_List[i] is Uhm)
                        {
                            newcatchmentinfo.Now_Rfmodel = newcatchmentinfo.Rfmodel_List[i];
                            break;
                        }
                    }
                    break;
                case RFModelType.XAJ:
                    //给新流域所采用的RF模型重新赋值
                    newcatchmentinfo.Now_RfmodelType = RFModelType.XAJ;
                    for (int i = 0; i < newcatchmentinfo.Rfmodel_List.Count; i++)
                    {
                        if (newcatchmentinfo.Rfmodel_List[i] is Xaj)
                        {
                            newcatchmentinfo.Now_Rfmodel = newcatchmentinfo.Rfmodel_List[i];
                            break;
                        }
                    }
                    break;
            }

            Console.WriteLine("新流域构建成功!");
        }

        // 改变指定流域当前采用的产汇流模型为指定类型  -- 可单独改变某流域的产汇流模型，不过设定为一改全改
        public static void Change_Catchment_RFmodel(ref HydroModel hydromodel, string catchmentname, RFModelType rfmodeltype)
        {
            //获取流域
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);

            //改变流域当前采用产汇流模型
            if (catchment.Now_RfmodelType != rfmodeltype)
            {
                catchment.Now_RfmodelType = rfmodeltype;
                //从3个产汇流集合中找出相应的产汇流模型
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    switch (rfmodeltype)
                    {
                        case RFModelType.NAM:
                            if (catchment.Rfmodel_List[i] is Nam)
                            {
                                catchment.Now_Rfmodel = catchment.Rfmodel_List[i];
                            }
                            break;
                        case RFModelType.UHM:
                            if (catchment.Rfmodel_List[i] is Uhm)
                            {
                                catchment.Now_Rfmodel = catchment.Rfmodel_List[i];
                            }
                            break;
                        default:
                            if (catchment.Rfmodel_List[i] is Xaj)
                            {
                                catchment.Now_Rfmodel = catchment.Rfmodel_List[i];
                            }
                            break;
                    }
                }
            }

        }

        //改变所有流域当前采用的产汇流模型
        public static void Change_AllCatchment_RFmodel(ref HydroModel hydromodel, RFModelType now_rfmodeltype)
        {
            //改变当前所有流域产汇流
            RF_Pars Rfpars = hydromodel.RfPars;
            Rfpars.Rainfall_selectmodel = now_rfmodeltype;
            CatchmentList Catchmentlist = hydromodel.RfPars.Catchmentlist;

            for (int i = 0; i < Catchmentlist.Catchment_infolist.Count; i++)
            {
                Change_Catchment_RFmodel(ref hydromodel, Catchmentlist.Catchment_infolist[i].Name, now_rfmodeltype);
            }
        }

        //改变模型所有子流域采用的产汇流模型类型(贾鲁河流域,不改模型直接改数据库)
        public static void Change_Catchment_RFmodel(string plan_code, object rf_model)
        {
            Dictionary<string, string> rf_models = new Dictionary<string, string> ();

            //更新或写入该模型的产汇流模型类型
            string tableName = Mysql_GlobalVar.CATCHMENT_RFMODEL;

            //如果为空则采用默认模型
            if (rf_model == null || rf_model.ToString() == "")
            {
                rf_models = Get_Default_RFmodel();

                //先判断和删除旧的模型
                string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
                DataTable dt = Mysql.query(select_sql);
                if(dt != null)
                {
                    int nLen = dt.Rows.Count;
                    if (nLen != 0)
                    {
                        string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "'";
                        Mysql.Execute_Command(del_sql);
                    }
                }

                //向数据库中插入新的模型信息
                string fieldname_withoutid = "plan_code,code,rf_model";
                for (int i = 0; i < rf_models.Count; i++)
                {
                    object[] value = new object[] {plan_code, rf_models.ElementAt(i).Key, rf_models.ElementAt(i).Value};
                    
                    //执行插入记录的sql语句,不写第1个字段id字段
                    string sqlstr = Mysql.sql_insert(tableName, value, fieldname_withoutid);
                    Mysql.Execute_Command(sqlstr);
                }
            }
            else
            {
                rf_models = JsonConvert.DeserializeObject<Dictionary<string, string>>(rf_model.ToString());

                for (int i = 0; i < rf_models.Count; i++)
                {
                    object[] value = new object[] { plan_code, rf_models.ElementAt(i).Key, rf_models.ElementAt(i).Value };

                    //更新数据库
                    string sql = "update " + tableName + " set rf_model = '" + rf_models.ElementAt(i).Value + "' where plan_code = '" + plan_code + "' and code = '" + rf_models.ElementAt(i).Key + "'";
                    Mysql.Execute_Command(sql);
                }
            }

            Console.WriteLine("产汇流模型类型更新完成！");
        }

        //获取默认的产汇流模型类型
        public static Dictionary<string,string> Get_Default_RFmodel()
        {
            Dictionary<string, string> rf_models = new Dictionary<string, string>();
            string tablename = Mysql_GlobalVar.CATCHMENT_BASEINFO;
            

            //从数据库获取默认的产汇流模型
            string select = "select * from " + tablename;

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);
            for (int i = 0; i < rows.Count; i++)
            {
                //读取一条记录上的数据
                string code = rows[i][0].ToString();
                string modeltype = rows[i][16].ToString();

                if (!rf_models.Keys.Contains(code))
                {
                    rf_models.Add(code, modeltype);
                }
            }
            
            return rf_models;
        }

        //获取指定模型所有子流域采用的产汇流模型类型
        public static Dictionary<string, string> Get_Catchment_RFmodel(string plan_code)
        {
            Dictionary<string, string> rf_models = new Dictionary<string, string>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.CATCHMENT_RFMODEL;
            

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string code = dt.Rows[i][2].ToString();
                string model_type = dt.Rows[i][3].ToString();
                rf_models.Add(code, model_type);
            }

            

            return rf_models;
        }

        //将各子流域洪水结果写入数据库
        public static void Write_CatchmentDischarge_toDB(string plan_code, Dictionary<string, Dictionary<DateTime, double>> inflow_dic)
        {
            if (inflow_dic == null) return;
            if (inflow_dic.Count == 0) return;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_CATCHMENT_SETDISCHARGE;
            

            //先判断和删除旧的模型结果
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "'";
                Mysql.Execute_Command(del_sql);
            }

            //存入数据库
            string sql;
            for (int i = 0; i < inflow_dic.Count; i++)
            {
                string inflow = File_Common.Serializer_Obj(inflow_dic.ElementAt(i).Value , "yyyy-MM-dd HH:mm:ss");
                sql = "insert into " + tableName + "(plan_code,bsn_code,outflow_json)values(:plan_code,:bsn_code,:outflow_json)";
                KdbndpParameter[] mysqlPara = { 
                                                new KdbndpParameter(":plan_code", plan_code),
                                                new KdbndpParameter(":bsn_code",inflow_dic.ElementAt(i).Key),
                                                new KdbndpParameter(":outflow_json", inflow)
                                              };
                Mysql.Execute_Command(sql, mysqlPara);
            }
        }

        //删除降雨数据库
        public static void Del_RainFall_Res(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.CATCHMENT_RAINFALLRES_TABLENAME;
            

            //删除旧的降雨结果
            string sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "'";
                Mysql.Execute_Command(del_sql);
            }

            
        }

        #endregion ***************************************************************************************


        #region *********************** 流域操作 -- 修改流域产汇流模型参数 ******************************
        // 修改指定流域的 NAM模型初始条件
        public static void Modify_Nam_Initial(ref HydroModel hydromodel, string catchmentname, Nam_InitialCondition new_initial)
        {
            //修改该流域当前模型参数
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;
            if (nowrfmodel is Nam)
            {
                Nam nammodel = nowrfmodel as Nam;
                nammodel.NAMInitial = new_initial;

                //更新模型集合对象
                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Nam)
                    {
                        Nam nammodel = nowrfmodel as Nam;
                        nammodel.NAMInitial = new_initial;
                        break;
                    }
                }
            }
        }

        // 修改指定流域 NAM模型的参数
        public static void Modify_NAM_Parameter(ref HydroModel hydromodel, string catchmentname, NAMparameters new_nampara)
        {
            //修改该流域当前模型参数
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;
            if (nowrfmodel is Nam)
            {
                Nam nammodel = nowrfmodel as Nam;
                nammodel.NAMpar = new_nampara;

                //更新模型集合对象
                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Nam)
                    {
                        Nam nammodel = nowrfmodel as Nam;
                        nammodel.NAMpar = new_nampara;
                        break;
                    }
                }
            }
        }

        // 修改指定流域 UHM模型的参数
        public static void Modify_UHM_Parameter(ref HydroModel hydromodel, string catchmentname, UHMparameters uhmpara)
        {
            //修改该流域当前模型参数
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;

            if (nowrfmodel is Uhm)
            {
                Uhm uhmmodel = nowrfmodel as Uhm;
                uhmmodel.UHMpar = uhmpara;

                //更新模型集合对象
                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Uhm)
                    {
                        Uhm uhmmodel = nowrfmodel as Uhm;
                        uhmmodel.UHMpar = uhmpara;
                        break;
                    }
                }
            }
        }

        // 修改指定流域 XAJ模型的参数
        public static void Modify_XAJ_Parameter(ref HydroModel hydromodel, string catchmentname, Xajparameters xajpara)
        {
            //修改该流域当前模型参数
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;

            if (nowrfmodel is Xaj)
            {
                Xaj xajmodel = nowrfmodel as Xaj;
                xajmodel.XajPar = xajpara;

                //更新模型集合对象
                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Xaj)
                    {
                        Xaj xajmodel = rfmodel as Xaj;
                        xajmodel.XajPar = xajpara;
                        break;
                    }
                }
            }

        }

        // 修改指定流域 XAJ模型的初始条件
        public static void Modify_XAJ_Initial(ref HydroModel hydromodel, string catchmentname, XajInitialConditional xajInitial)
        {
            //修改该流域当前模型参数
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;

            if (nowrfmodel is Xaj)
            {
                Xaj xajmodel = nowrfmodel as Xaj;
                xajmodel.XajInitial = xajInitial;

                //更新模型集合对象
                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Xaj)
                    {
                        Xaj xajmodel = rfmodel as Xaj;
                        xajmodel.XajInitial = xajInitial;
                        break;
                    }
                }
            }

        }

        #endregion ******************************************************************************************


        #region *********************** 其他方法  -- 各流域开始新安江模拟并返回结果 *************************
        //各流域XAJ模型开始模拟，并制作结果dfs0文件
        public static void Get_Catchments_XAJSimulateRes(HydroModel hydromodel)
        {
            Dictionary<string, Dictionary<DateTime, double>> Catchment_RFreslist = new Dictionary<string, Dictionary<DateTime, double>>();

            //如果只是产汇流计算且采用的是XAJ模型，则直接调用流域的参数进行计算
            if (hydromodel.ModelGlobalPars.Select_model == CalculateModel.only_rr && hydromodel.RfPars.Rainfall_selectmodel == RFModelType.XAJ)
            {
                CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
                for (int i = 0; i < catchmentlist.Catchment_infolist.Count; i++)
                {
                    Xaj catchment_xajmodel = catchmentlist.Catchment_infolist[i].Now_Rfmodel as Xaj;
                    Dictionary<DateTime, double> rfresult = catchment_xajmodel.Forecast(hydromodel);
                    Catchment_RFreslist.Add(catchment_xajmodel.Catchment_Name, rfresult);
                }
            }
            else
            {
                Console.WriteLine("各流域产汇流模型非新安江模拟，请调整为新安江模拟后再模拟！");
            }

            //制作计算dfs0结果
            Dfs0.Creat_XAJres_Dfs0(hydromodel, Catchment_RFreslist);
        }

        // 新建一个空白模型文件RR11
        public static void Build_BlankRR11(string newFilePath)
        {
            PFSBuilder builder = new PFSBuilder();
            builder.AddTarget("MIKE_RR");  //建立最外面的节
            builder.AddKeyword("version_no");  //建立关键词
            builder.AddDouble(101);  //给关键词赋值

            builder.AddSection("CatchList");  //新建第2层节：流域列表
            builder.EndSection(); //CatchList

            builder.AddSection("CombinedList");
            builder.EndSection();  //CombinedList

            builder.AddSection("ParameterList");  //第2层节：参数列表
            builder.EndSection();  //ParameterList

            builder.AddSection("TimeseriesList");  //第3层节：时间序列
            builder.AddKeyword("Max_Comb_Number");
            builder.AddDouble(8);
            builder.EndSection();  //TimeseriesList

            builder.AddSection("BasinStationList");
            builder.AddKeyword("BasinListCount");
            builder.AddDouble(0);
            builder.EndSection();  //BasinStationList

            builder.AddSection("UHM_Common_Parameters");
            builder.AddSection("EnlargeRatio_ParameterList");
            builder.EndSection();  //EnlargeRatio_ParameterList
            builder.AddSection("NakayasuLoss_ParameterList");
            builder.EndSection();  //NakayasuLoss_ParameterList
            builder.AddSection("F1RsaLoss_ParameterList");
            builder.EndSection();  //F1RsaLoss_ParameterList
            builder.AddSection("QLSF_ParameterList");
            builder.EndSection();  //QLSF_ParameterList
            builder.AddSection("StartEvent");
            builder.AddKeyword("ApplyStart");
            builder.AddBool(false);
            builder.AddKeyword("Start");
            builder.AddDateTime(DateTime.Now);
            builder.EndSection();  //StartEvent
            builder.AddSection("SpecVal");
            builder.AddKeywordValues("ALO", 600);
            builder.AddKeywordValues("Divi", 1);
            builder.AddKeywordValues("Error1", 0.01);
            builder.AddKeywordValues("IR", 100);
            builder.AddKeywordValues("ISW", 0);
            builder.AddKeywordValues("JS", 200);
            builder.AddKeywordValues("JTO", 9999);
            builder.AddKeywordValues("KK1", 60);
            builder.AddKeywordValues("KK2", 1);
            builder.AddKeywordValues("LS", 50);
            builder.AddKeywordValues("P", 0.6);
            builder.AddKeywordValues("SI", 0.3);
            builder.EndSection();  //SpecVal
            builder.EndSection();  //UHM_Common_Parameters

            builder.AddSection("UserDefinedCombinationPeriods");
            builder.AddKeyword("ApplyCombination");
            builder.AddBool(false);
            builder.AddKeyword("PeriodCount");
            builder.AddDouble(0);
            builder.EndSection();  //UserDefinedCombinationPeriods

            builder.AddSection("MAWOutputOneFile");
            builder.AddKeyword("OutputOnlyOne");
            builder.AddBool(false);
            builder.AddKeyword("MAWFile");
            builder.AddFileName("");
            builder.EndSection();  //MAWOutputOneFile

            builder.AddSection("MAWCombNoFile");
            builder.AddKeyword("Apply");
            builder.AddBool(false);
            builder.AddKeyword("CombFile");
            builder.AddFileName("");
            builder.EndSection();  //MAWCombNoFile

            builder.AddSection("BasinAreaDefinition");
            builder.AddKeyword("OriginX");
            builder.AddDouble(0);
            builder.AddKeyword("OriginY");
            builder.AddDouble(0);
            builder.AddKeyword("Width");
            builder.AddDouble(500);
            builder.AddKeyword("Height");
            builder.AddDouble(350);
            builder.EndSection();  //BasinAreaDefinition

            builder.AddSection("LAYERS");
            builder.EndSection();  //LAYERS

            builder.EndSection();  //MIKE_RR

            builder.Write(newFilePath);
            Console.WriteLine("新建模型成功！");
        }

        // 将模型参数存入数据库（NAM和UHM要分别存储在不同的数据库表格）
        public static void ParameterToMysql(string filePath, int basinNo, KdbndpConnection mysqlconn, string tableName)
        {
            PFSFile basin = new PFSFile(filePath, false);
            PFSSection MIKE_RR = basin.GetTarget("MIKE_RR", 1);  //最外面的节

            PFSSection CatchList = MIKE_RR.GetSection("CatchList", 1);   //第2层节：流域列表
            PFSSection Catchment = CatchList.GetSection("Catchment", basinNo);  //第3层节：具体流域
            string Catchment_Name = Catchment.GetKeyword("Catchment_Name", 1).GetParameter(1).ToString();  //流域名字
            string Catchment_Model = Catchment.GetKeyword("Catchment_Model", 1).GetParameter(1).ToString();  //流域水文模型

            PFSSection ParameterList = MIKE_RR.GetSection("ParameterList", 1);   //第2层节：参数列表

            if (Catchment_Model == "NAM")
            {
                PFSSection NAM_Parameters = ParameterList.GetSection(basinNo);  //第3层节：某流域参数
                PFSSection SurfaceRootzone = NAM_Parameters.GetSection("SurfaceRootzone", 1);  //第4层节：地表径流参数
                double U_Max = SurfaceRootzone.GetKeyword("U_Max", 1).GetParameter(1).ToDouble();
                double L_Max = SurfaceRootzone.GetKeyword("L_Max", 1).GetParameter(1).ToDouble();
                double CQOF = SurfaceRootzone.GetKeyword("CQOF", 1).GetParameter(1).ToDouble();
                double CKIF = SurfaceRootzone.GetKeyword("CKIF", 1).GetParameter(1).ToDouble();
                double CK1 = SurfaceRootzone.GetKeyword("CK1", 1).GetParameter(1).ToDouble();
                double TOF = SurfaceRootzone.GetKeyword("TOF", 1).GetParameter(1).ToDouble();
                double TIF = SurfaceRootzone.GetKeyword("TIF", 1).GetParameter(1).ToDouble();
                PFSSection GroundWater = NAM_Parameters.GetSection("GroundWater", 1);  //地下径流参数
                double TG = GroundWater.GetKeyword("TG", 1).GetParameter(1).ToDouble();
                double CKBF = GroundWater.GetKeyword("CKBF", 1).GetParameter(1).ToDouble();

                string sql = "insert into " + tableName + "(Catchment_Name,U_Max,L_Max,CQOF,CKIF,CK1,TOF,TIF,TG,CKBF) values(:Catchment_Name,:U_Max,:L_Max,:CQOF,:CKIF,:CK1,:TOF,:TIF,:TG,:CKBF)";
                KdbndpParameter[] mysqlpara ={new KdbndpParameter(":Catchment_Name", Catchment_Name),
                                         new KdbndpParameter(":U_max", U_Max), 
                                         new KdbndpParameter(":L_max", L_Max), 
                                         new KdbndpParameter(":CQOF", CQOF),
                                         new KdbndpParameter(":CKIF", CKIF),
                                         new KdbndpParameter(":CK1", CK1),
                                         new KdbndpParameter(":TOF", TOF),
                                         new KdbndpParameter(":TIF", TIF),
                                         new KdbndpParameter(":TG", TG),
                                         new KdbndpParameter(":CKBF", CKBF),
                                       };
                Mysql.Execute_Command(sql, mysqlpara);
            }
            if (Catchment_Model == "UHM")
            {
                PFSSection UHMParameters = ParameterList.GetSection(basinNo);
                double Area_RF = UHMParameters.GetKeyword("Area_RF", 1).GetParameter(1).ToDouble();
                double Baseflow = UHMParameters.GetKeyword("Baseflow", 1).GetParameter(1).ToDouble();
                double InitLoss = UHMParameters.GetKeyword("InitLoss", 1).GetParameter(1).ToDouble();
                double ConstLoss = UHMParameters.GetKeyword("ConstLoss", 1).GetParameter(1).ToDouble();
                double Tlag = UHMParameters.GetKeyword("Tlag", 1).GetParameter(1).ToDouble();
                string sql = "insert into " + tableName + "(Catchment_Name,Area_RF,Baseflow,InitLoss,ConstLoss,Tlag) values(:Catchment_Name,:Area_RF,:Baseflow,:InitLoss,:ConstLoss,:Tlag)";
                KdbndpParameter[] mysqlpara ={
                                               new KdbndpParameter(":Catchment_Name",Catchment_Name),
                                               new KdbndpParameter(":Area_RF",Area_RF),
                                               new KdbndpParameter(":Baseflow",Baseflow),
                                               new KdbndpParameter(":InitLoss",InitLoss),
                                               new KdbndpParameter(":ConstLoss",ConstLoss),
                                               new KdbndpParameter(":Tlag",Tlag),
                                           };
                Mysql.Execute_Command(sql, mysqlpara);
            }
            Console.WriteLine("存入数据库成功！");
        }

        //将新的流域单位线上传到数据库 -- 如果有则删除原来的重新更新
        public static void Write_NewUHMGraph_ToDb(string catchmentname, Dictionary<int, double> Catchment_UHMdic)
        {
            //连接基础资料数据库
            Mysql mysql = new Mysql(Mysql_GlobalVar.NOWITEM_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port,
                                       Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            
            string tablename = Mysql_GlobalVar.HYDROGRAPH_TABLENAME;

            //打开数据库连接
            

            //如果这个流域的单位线存在了，则先删除
            string select_sql = "select * from " + tablename + " where catchment_name = '" + catchmentname + "' ";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tablename + " where catchment_name = '" + catchmentname + "' ";
                Mysql.Execute_Command(del_sql);
            }

            //获取字段名
            string select = "select * from " + tablename + " where id = 1 ";
            List<string> strlist = Mysql.Get_FieldsName(select);
            string fieldname_withoutid = null;
            for (int i = 1; i < strlist.Count - 1; i++)
            {
                fieldname_withoutid = fieldname_withoutid + strlist[i] + " , ";
            }
            fieldname_withoutid = fieldname_withoutid + strlist[strlist.Count - 1];

            //新建OBJECT类型的数组
            object[] value = new object[strlist.Count - 1];

            //遍历单位线数据
            int uhmcount = Catchment_UHMdic.Count;
            for (int i = 0; i < uhmcount; i++)
            {
                //每条记录的每个字段赋值
                value[0] = catchmentname;
                value[1] = Catchment_UHMdic.Keys.ElementAt(i);
                value[2] = Catchment_UHMdic.Values.ElementAt(i);

                //执行插入记录的sql语句,不写第1个字段id字段
                Mysql.Execute_Command(Mysql.sql_insert(tablename, value, fieldname_withoutid));

                Console.WriteLine("正在写入第{0}个数据，总数据:{1}", i, uhmcount);
            }

            //关闭连接
            
        }
        #endregion *******************************************************************************************
    }


    //保存最新所有流域信息和其所采用产汇流模型信息的类
    [Serializable]
    public class CatchmentList
    {
        public CatchmentList()
        {
            //流域的各月的 日平均蒸发速率
            CatchmentList.AverageEvaRate = new Dictionary<int, double> ();

            //流域的各月的 日平均降雨速率
            CatchmentList.AverageRainfallRate = new Dictionary<int, double> ();

            //标准24小时雨形模板
            CatchmentList.Stand24_Rfmodel = new Dictionary<DateTime, double>();
        }

        //**********************属性************************
        //产汇流流域和水文模型信息
        public List<CatchmentInfo> Catchment_infolist
        { get; set; }

        //流域ID的最大值
        public int MaxCatchmentId
        { get; set; }

        //流域的各月的 日平均蒸发速率
        public static Dictionary<int, double> AverageEvaRate
        { get; set; }

        //流域的各月的 日平均降雨速率
        public static Dictionary<int, double> AverageRainfallRate
        { get; set; }

        //标准24小时雨形模板
        public static Dictionary<DateTime, double> Stand24_Rfmodel
        { get; set; }

        //**********************方法************************
        //增加新流域方法
        public void AddCatchment(CatchmentInfo newcatchment)
        {
            if (Catchment_infolist == null)
            {
                Catchment_infolist = new List<CatchmentInfo>();
            }

            if (!Catchment_infolist.Contains(newcatchment))
            {
                //往集合中增加新流域
                Catchment_infolist.Add(newcatchment);

                //流域ID的最大值
                if (newcatchment.Id > MaxCatchmentId)
                {
                    MaxCatchmentId = newcatchment.Id;
                }

                //更新雨量站和流域对应关系数据库表
                RFService.Update_RFStation_CatchmentName(newcatchment.Name, newcatchment.Catchment_Pointlist);
            }
        }

        //减少流域方法
        public void RemoveCatchment(string catchmentname)
        {
            for (int i = 0; i < Catchment_infolist.Count; i++)
            {
                if (Catchment_infolist[i].Name == catchmentname)
                {
                    Catchment_infolist.RemoveAt(i);
                    if (MaxCatchmentId == Catchment_infolist[i].Id)
                    {
                        MaxCatchmentId--;
                    }
                    break;
                }
            }
        }

        //根据流域名获取该流域详细信息方法
        public CatchmentInfo Get_Catchmentinfo(string catchmentname)
        {
            CatchmentInfo Catchment = Catchment_infolist[0];

            for (int i = 0; i < Catchment_infolist.Count; i++)
            {
                if (Catchment_infolist[i].Name == catchmentname)
                {
                    Catchment = Catchment_infolist[i];
                    break;
                }
            }
            return Catchment;
        }

        //获取指定时期的平均日蒸发、降雨量键值对集合 -- 降雨只会用于新安江模型的前期降雨，蒸发都会用
        public static Dictionary<DateTime, double> Get_Average_DayEvaRfRate(DateTime starttime, DateTime endtime, Eva_RF eva_rf)
        {
            Dictionary<DateTime, double> Average_RateDic = new Dictionary<DateTime, double>();

            //如果指定时期就在求均价蒸发序列里面，则直接挑选出该区间的蒸发序列
            DateTime history_starttime = new DateTime(2001, 1, 1, 0, 0, 0);
            DateTime history_endtime = new DateTime(2012, 12, 31, 0, 0, 0);
            Dictionary<DateTime, double> source_dic;
            switch (eva_rf)
            {
                case Eva_RF.eva:  //求平均蒸发
                    source_dic = RFService.GetHistory_Evadic(history_starttime, history_endtime);
                    break;
                default:          //求平均降雨
                    source_dic = RFService.GetHistory_RFdic(history_starttime, history_endtime);
                    break;
            }

            if (starttime.CompareTo(source_dic.Keys.ElementAt(0)) >= 0 && endtime.CompareTo(source_dic.Keys.ElementAt(source_dic.Count - 1)) <= 0)
            {
                //截取该段时间序列
                Average_RateDic = Dfs0.Get_Subdic(source_dic, starttime, endtime);
                return Average_RateDic;
            }

            //如果不完全在，则按平均考虑
            int sum_day = endtime.Subtract(starttime).Days;
            double endevarf = 0;
            for (int i = 0; i < sum_day; i++)
            {
                DateTime nowdatetime = starttime.AddDays(i);
                double evarf = 0;
                if (eva_rf == Eva_RF.eva)
                {
                    for (int j = 0; j < CatchmentList.AverageEvaRate.Count; j++)
                    {
                        if (nowdatetime.Month == CatchmentList.AverageEvaRate.Keys.ElementAt(j))
                        {
                            evarf = CatchmentList.AverageEvaRate[nowdatetime.Month];
                            break;
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < CatchmentList.AverageRainfallRate.Count; j++)
                    {
                        if (nowdatetime.Month == CatchmentList.AverageRainfallRate.Keys.ElementAt(j))
                        {
                            evarf = CatchmentList.AverageRainfallRate[nowdatetime.Month];
                            break;
                        }
                    }
                }
                if (i == 0) evarf = 0;
                Average_RateDic.Add(nowdatetime, evarf);

                if (i == sum_day - 1)
                {
                    endevarf = evarf;
                }
            }

            //如果按天细分的最后一个日期不等于结束时间,则增加最后日期
            if (Average_RateDic.Keys.ElementAt(Average_RateDic.Count - 1) != endtime)
            {
                Average_RateDic.Add(endtime, endevarf);
            }
            return Average_RateDic;
        }

        //获取指定时期的平均小时蒸发、降雨量 -- 降雨只会用于新安江模型的前期降雨，蒸发都会用
        public static Dictionary<DateTime, double> Get_Average_HourEvaRfRate(DateTime starttime, DateTime endtime, Eva_RF eva_rf)
        {
            //先获取日平均蒸发降雨速率
            Dictionary<DateTime, double> dayevadic = Get_Average_DayEvaRfRate(starttime, endtime, eva_rf);

            //再内插为1小时步长的集合
            Dictionary<DateTime, double> hourevarfdic = Dfs0.Insert_Accutedic(dayevadic, new TimeSpan(1, 0, 0));

            return hourevarfdic;
        }

        //判断是否全部为XAJ模型
        public bool AllisXAJ()
        {
            //如果全部采用XAJ模型，则不能加载rr文件
            bool Allcatchment_RFmodel_IsXAJ = true;
            if (Catchment_infolist == null) return false;

            for (int i = 0; i < Catchment_infolist.Count; i++)
            {
                if (Catchment_infolist[i].Now_RfmodelType != RFModelType.XAJ)
                {
                    Allcatchment_RFmodel_IsXAJ = false;
                    break;
                }
            }
            return Allcatchment_RFmodel_IsXAJ;
        }
    }

    //专用于构建Nam模型的类
    [Serializable]
    public class Nam : Rfmodel
    {
        //构造函数

        //参数默认构造函数
        public Nam(CatchmentInfo catchmentinfo)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
        }

        //有参数构造函数
        public Nam(CatchmentInfo catchmentinfo, NAMparameters pars)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.NAMpar = pars;
        }

        //有初始条件构造函数
        public Nam(CatchmentInfo catchmentinfo, Nam_InitialCondition initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.NAMInitial = initial;
        }

        //有参数和初始条件构造函数
        public Nam(CatchmentInfo catchmentinfo, NAMparameters pars, Nam_InitialCondition initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.NAMpar = pars;
            this.NAMInitial = initial;
        }


        //属性
        public NAMparameters NAMpar   //nam模型参数
        { get; set; }

        public Nam_InitialCondition NAMInitial  //nam模型初始条件
        { get; set; }

        //方法
        //设置默认属性
        public void SetDefault(CatchmentInfo catchmentinfo)
        {
            //设置基本参数
            NAMparameters pars;
            pars.U_Max = 13;
            pars.L_Max = 150;
            pars.CQOF = 0.5;
            pars.CKIF = 700;
            pars.CK1 = 18;
            pars.TOF = 0.4;
            pars.TIF = 0.8;
            pars.TG = 0.9;
            pars.CKBF = 3500;
            this.NAMpar = pars;

            Nam_InitialCondition Initial;
            Initial.U_Ini = 0.5;
            Initial.L_Ini = 0.5;
            Initial.OF_Ini = 0.2;
            Initial.IF_Ini = 0.2;
            Initial.BFlow = 1;
            this.NAMInitial = Initial;
        }
    }

    //专用于构建UHM模型(水文单位线)的类
    [Serializable]
    public class Uhm : Rfmodel
    {
        //构造函数

        //参数默认构造函数
        public Uhm(CatchmentInfo catchmentinfo)
            : base(catchmentinfo.Name)
        {
            SetDefault();
        }

        //有参数构造函数
        public Uhm(CatchmentInfo catchmentinfo, UHMparameters pars)
            : base(catchmentinfo.Name)
        {
            SetDefault();
            this.UHMpar = pars;
        }

        //属性
        public UHMparameters UHMpar   //UHM模型参数
        { get; set; }

        //方法
        //设置默认属性
        public void SetDefault()
        {
            UHMparameters pars;
            pars.Area_RF = 1.0;
            pars.Baseflow = 0;
            pars.InitLoss = 10;
            pars.ConstLoss = 5;
            pars.Tlag = 2;
            this.UHMpar = pars;
        }
    }

    //专用于构建新安江模型的类
    [Serializable]
    public class Xaj : Rfmodel
    {
        //构造函数

        //只有所在流域的构造函数
        public Xaj(CatchmentInfo catchmentinfo)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
        }

        //只有初始条件的构造函数
        public Xaj(CatchmentInfo catchmentinfo, XajInitialConditional initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
            this.XajInitial = initial;
        }

        //只有新安江参数的构造函数
        public Xaj(CatchmentInfo catchmentinfo, Xajparameters paras)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
            this.XajPar = paras;
        }

        //有新安江参数和初始条件的构造函数
        public Xaj(CatchmentInfo catchmentinfo, Xajparameters paras, XajInitialConditional initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
            this.XajPar = paras;
            this.XajInitial = initial;
        }

        //属性
        public double Catchment_Area    //所在流域面积
        { get; set; }

        public Xajparameters XajPar  //新安江参数
        { get; set; }

        public XajInitialConditional XajInitial  //初始条件
        { get; set; }

        public Dictionary<DateTime, double> Day_P  //前期日降雨资料
        { get; set; }

        public Dictionary<DateTime, double> Step_P  //时段降雨
        { get; set; }

        public Dictionary<DateTime, double> Day_em //前期日蒸发资料
        { get; set; }

        //设置默认值方法
        public void SetDefault(CatchmentInfo catchmentinfo)
        {
            Xajparameters paras;
            paras.Sm = 30;
            paras.Wm = 130;
            paras.Um = 15;
            paras.Lm = 50;
            paras.B = 0.25;
            paras.Im = 0.02;
            paras.K = 0.9;
            paras.C = 0.11;
            paras.Ki = 0.29;
            paras.Ex = 1.5;
            paras.Ci = 0.9;
            paras.Cg = 0.9;
            paras.Cs = 0.5;
            paras.L = 3.5;
            paras.Cr = 0.15;
            this.XajPar = paras;  //设置默认参数

            XajInitialConditional initial;
            initial.ua_day = 10;
            initial.la_day = 30;
            initial.da_day = 60;
            initial.baseFlow = 1;
            this.XajInitial = initial;  //设置默认初始条件

            //前期日降雨、前期日蒸发、时段降雨均先采用空值，在模拟时再重新赋值
            Dictionary<DateTime, double> rf_eva_dic = new Dictionary<DateTime, double>();
            this.Day_P = rf_eva_dic;
            this.Step_P = rf_eva_dic;
            this.Day_em = rf_eva_dic;
        }

        //调用新安江模型开始运算
        public Dictionary<DateTime, double> Forecast(HydroModel hydromodel)
        {
            Dictionary<DateTime, double> Forecast_result = new Dictionary<DateTime, double>();

            //根据模拟时间，重新给新安江模型的降雨蒸发数据赋值
            SetModel_RfEvaDic(hydromodel);

            //调用模拟引擎开始计算
            Forecast_result = FloodForecast.Forecast(this);

            //返回结果数据
            return Forecast_result;
        }

        //根据模拟时间，重新给新安江模型的降雨蒸发数据赋值
        public void SetModel_RfEvaDic(HydroModel hydromodel)
        {
            //前期日降雨 时间序列 默认从水文数据库中读取(有可能数据读不全 -- 历史降雨、设计降雨、实时预报降雨)
            SimulateTime simulatetime = hydromodel.ModelGlobalPars.Simulate_time;
            DateTime starttime = simulatetime.Begin.AddDays(-1 * Model_Const.EVP_BEFORE_DAYCOUNT);
            DateTime endtime = simulatetime.End;
            Dictionary<DateTime, double> agorfdic = RFService.Get_CatchmentRainfall(this.Catchment_Name, starttime, endtime, 24);

            //通过判断获取的降雨序列起止点是否为模拟起止点，如果不完全是，则按多年平均考虑
            if (agorfdic.Keys.ElementAt(0) != starttime || agorfdic.Keys.ElementAt(agorfdic.Count - 1) != endtime)
            {
                this.Day_P = CatchmentList.Get_Average_DayEvaRfRate(starttime, endtime, Eva_RF.rainfall);
            }
            else
            {
                this.Day_P = agorfdic;
            }

            //时段降雨 时间序列 
            Dictionary<DateTime, double> resdic = Dfs0.GetRFdic_FromDb(hydromodel, this.Catchment_Name);

            //将以小时计的时段降雨内插成XAJ合适的时间步长(12分钟)
            TimeSpan new_timespan = new TimeSpan(0, (int)(Model_Const.XAJ_TIMESTEP_HOUR * 60), 0);
            Dictionary<DateTime, double> new_resdic = Dfs0.Insert_Accutedic(resdic, new_timespan);
            this.Step_P = new_resdic;

            //前期日蒸发 时间序列 默认采用历史各月的日平均值
            Dictionary<DateTime, double> Day_em = CatchmentList.Get_Average_DayEvaRfRate(starttime, endtime, Eva_RF.eva);
            this.Day_em = Day_em;
        }
    }

    //产汇流模型基类
    [Serializable]
    public class Rfmodel
    {
        //构造函数

        //参数默认构造函数
        public Rfmodel(string catchmentname)
        {
            this.Catchment_Name = catchmentname;
        }

        //属性
        public string Catchment_Name   //所在流域名字
        { get; set; }

    }

    //流域类 -- 一旦构造3个产汇流模型就都有了，参数为默认，当前模型定为NAM模型
    [Serializable]
    public class CatchmentInfo
    {
        //构造函数 -- 所需参数：流域名、流域边界点结合、模型方案名、模拟起止时间、
        public CatchmentInfo(int catchmentID, string name, List<PointXY> catchment_pointlist)
        {
            CO_SetDefault();
            this.Id = catchmentID;
            this.Name = name;
            double area = Math.Round(PointXY.Get_PolygonArea(catchment_pointlist) / 1000000, 3);  //以平方公里为单位的面积
            this.Area = area;
            this.Catchment_Pointlist = catchment_pointlist;

            //设置3个产汇流模型
            Set_RFmodelList();
        }

        //属性
        public string Name       //流域名称
        { get; set; }

        public double Area    //流域面积, km2
        { get; set; }

        public int Id    //流域id
        { get; set; }

        public List<PointXY> Catchment_Pointlist  //流域控制点集合
        { get; set; }

        public List<Rfmodel> Rfmodel_List  //3个产汇流模型的集合
        { get; set; }

        public Rfmodel Now_Rfmodel     //流域现在采用的降雨径流模型
        { get; set; }

        public RFModelType Now_RfmodelType  //流域现在采用的产汇流模型类型
        { get; set; }

        //方法
        //设置默认属性
        public void CO_SetDefault()
        {
            this.Name = "";
            this.Area = 0;

            List<PointXY> Catchment_Pointlist = new List<PointXY>();
            this.Catchment_Pointlist = Catchment_Pointlist;
        }

        //设置3个产汇流模型集合和当前产汇流模型 -- 创建nam模型时会创建蒸发dfs0文件
        public void Set_RFmodelList()
        {
            List<Rfmodel> Rfmodel_List = new List<Rfmodel>();
            Nam nam_model1 = new Nam(this);
            Uhm uhm_model1 = new Uhm(this);
            Xaj xaj_model1 = new Xaj(this);

            Rfmodel_List.Add(nam_model1);
            Rfmodel_List.Add(uhm_model1);
            Rfmodel_List.Add(xaj_model1);

            this.Rfmodel_List = Rfmodel_List;

            //更新当前采用的产汇流模型和类型
            this.Now_RfmodelType = RFModelType.NAM;
            this.Now_Rfmodel = Rfmodel_List[0];
        }

        //更新产汇流集合
        public void Update_Rfmodellist()
        {
            for (int i = 0; i < this.Rfmodel_List.Count; i++)
            {
                Rfmodel rfmodel = this.Rfmodel_List[i];
                RFModelType rfmodeltype;
                if (rfmodel is Nam)
                {
                    rfmodeltype = RFModelType.NAM;
                }
                else if (rfmodel is Uhm)
                {
                    rfmodeltype = RFModelType.UHM;
                }
                else
                {
                    rfmodeltype = RFModelType.XAJ;
                }

                //如果类型相同，但对象不同，则移除后重新添加
                if (rfmodeltype == this.Now_RfmodelType && this.Rfmodel_List[i] != this.Now_Rfmodel)
                {
                    this.Rfmodel_List.RemoveAt(i);
                    this.Rfmodel_List.Add(this.Now_Rfmodel);
                    break;
                }
            }
        }


    }

    //流域产汇流请求参数
    [Serializable]
    public class RfModel_RequestPars
    {
        public string plan_code;
        public string model_planname;
        public string start_time;
        public string end_time;
        public Dictionary<string, string> rf_model;

        public RfModel_RequestPars()
        {
            this.plan_code = "";
            this.model_planname = "";
            this.start_time = "";
            this.end_time = "";
            this.rf_model = new Dictionary<string, string>();
        }

        public RfModel_RequestPars(string plan_code, string model_planname, string start_time, string end_time, Dictionary<string, string> rf_model)
        {
            this.plan_code = plan_code;
            this.model_planname = model_planname;
            this.start_time = start_time;
            this.end_time = end_time;
            this.rf_model = rf_model;
        }
    }


}