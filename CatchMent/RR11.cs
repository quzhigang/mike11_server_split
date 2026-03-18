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

using bjd_model.Mike11;
using Newtonsoft.Json;

namespace bjd_model.CatchMent
{
    // 降雨蒸发时间序列文件及条目名称
    [Serializable]
    public struct Rfeva_FileItem
    {
        public string rain_filename;
        public string rain_itemname;
        public string eva_filename;
        public string eva_itemname;
    }

    // 设计暴雨信息
    [Serializable]
    public struct DesignRF_Info
    {
        public DesignRFLevel designrf_level;
        public DesignRF_TimeSpan designrf_timespan;
    }

    // 设计暴雨重现期等级
    [Serializable]
    public enum DesignRFLevel
    {
        level_5nian = 5,
        level_10nian = 10,
        level_20nian = 20,
        level_50nian = 50,
        level_100nian = 100
    }

    // 设计暴雨历时等级
    [Serializable]
    public enum DesignRF_TimeSpan
    {
        level_1day = 1,
        level_3day = 3,
        level_7day = 7,
        level_15day = 15
    }

    // 产汇流模型类型
    [Serializable]
    public enum RFModelType
    {
        NAM,
        UHM,
        XAJ
    }

    // 蒸发或降雨类型标识
    [Serializable]
    public enum Eva_RF
    {
        eva,
        rainfall
    }

    // NAM 模型参数
    [Serializable]
    public struct NAMparameters
    {
        public double U_Max;
        public double L_Max;
        public double CQOF;
        public double CKIF;
        public double CK1;
        public double TOF;
        public double TIF;
        public double TG;
        public double CKBF;
    }

    // NAM 模型初始条件
    [Serializable]
    public struct Nam_InitialCondition
    {
        public double U_Ini;
        public double L_Ini;
        public double OF_Ini;
        public double IF_Ini;
        public double BFlow;
    }

    // UHM 模型参数
    [Serializable]
    public struct UHMparameters
    {
        public double Area_RF;
        public double Baseflow;
        public double InitLoss;
        public double ConstLoss;
        public double Tlag;
    }

    // XAJ 模型初始条件
    [Serializable]
    public struct XajInitialConditional
    {
        public double ua_day;
        public double la_day;
        public double da_day;
        public double baseFlow;
    }

    // XAJ 模型参数
    [Serializable]
    public struct Xajparameters
    {
        public double Sm;
        public double Wm;
        public double Um;
        public double Lm;
        public double B;
        public double Im;
        public double K;
        public double C;
        public double Ki;
        public double Ex;
        public double Ci;
        public double Cg;
        public double Cs;
        public double L;
        public double Cr;

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

    public partial class RR11
    {
        #region 从默认 rr11 文件读取流域信息
        public static void GetCatchmentInfo(string sourcefilename, ref HydroModel hydromodel)
        {
            CatchmentList Catchment_List = hydromodel.RfPars.Catchmentlist;
            string modelname = hydromodel.Modelname;
            SimulateTime simulate_time = hydromodel.ModelGlobalPars.Simulate_time;

            PFSFile pfsfile = new PFSFile(sourcefilename);
            PFSSection target = pfsfile.GetTarget("MIKE_RR", 1);
            PFSSection CatchList = target.GetSection("CatchList", 1);
            PFSSection Parameterlist = target.GetSection("ParameterList", 1);
            PFSSection Timeserieslist = target.GetSection("TimeseriesList", 1);

            List<CatchmentInfo> catchmentlist = new List<CatchmentInfo>();
            for (int i = 0; i < CatchList.GetSectionsCount(); i++)
            {
                PFSSection catchment = CatchList.GetSection("Catchment", i + 1);
                if (catchment.GetKeyword("Catchment_Model").GetParameter(1).ToString() == "NAM")
                {
                    string catchment_name = catchment.GetKeyword("Catchment_Name").GetParameter(1).ToString();
                    double catchment_area = catchment.GetKeyword("Catchment_Area").GetParameter(1).ToDouble();
                    int catchment_id = catchment.GetKeyword("Number_ID").GetParameter(1).ToInt();

                    List<PointXY> catchmentpointlist = RFService.GetCatchment_Pointdic(catchment_name);

                    CatchmentInfo catchmentinfo = new CatchmentInfo(catchment_id, catchment_name, catchmentpointlist);
                    catchmentinfo.Now_RfmodelType = RFModelType.NAM;

                    Nam nammodel = new Nam(catchmentinfo);
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

                    PFSSection parametersec2 = Parameterlist.GetSection(i + 1).GetSection("InitialCondition", 1);
                    Nam_InitialCondition initial;
                    initial.U_Ini = parametersec2.GetKeyword("U_Ini").GetParameter(1).ToDouble();
                    initial.L_Ini = parametersec2.GetKeyword("L_Ini").GetParameter(1).ToDouble();
                    initial.OF_Ini = parametersec2.GetKeyword("OF_Ini").GetParameter(1).ToDouble();
                    initial.IF_Ini = parametersec2.GetKeyword("IF_Ini").GetParameter(1).ToDouble();
                    initial.BFlow = parametersec2.GetKeyword("GWL_Ini").GetParameter(1).ToDouble();
                    nammodel.NAMInitial = initial;

                    catchmentinfo.Now_Rfmodel = nammodel;
                    catchmentinfo.Update_Rfmodellist();
                    catchmentlist.Add(catchmentinfo);
                }
                else if (catchment.GetKeyword("Catchment_Model").GetParameter(1).ToString() == "UHM")
                {
                    string catchment_name = catchment.GetKeyword("Catchment_Name").GetParameter(1).ToString();
                    double catchment_area = catchment.GetKeyword("Catchment_Area").GetParameter(1).ToDouble();
                    int catchment_id = catchment.GetKeyword("Number_ID").GetParameter(1).ToInt();

                    List<PointXY> catchmentpointlist = RFService.GetCatchment_Pointdic(catchment_name);
                    Rfeva_FileItem rfeva_fileitem = Get_RfEvafilename(Timeserieslist, catchment_name);

                    CatchmentInfo catchmentinfo = new CatchmentInfo(catchment_id, catchment_name, catchmentpointlist);
                    catchmentinfo.Now_RfmodelType = RFModelType.UHM;

                    Uhm uhmmodel = new Uhm(catchmentinfo);
                    PFSSection parametersec = Parameterlist.GetSection(i + 1);
                    UHMparameters parameters;
                    parameters.Area_RF = parametersec.GetKeyword("Area_RF").GetParameter(1).ToDouble();
                    parameters.Baseflow = parametersec.GetKeyword("Baseflow").GetParameter(1).ToDouble();
                    parameters.InitLoss = parametersec.GetKeyword("InitLoss").GetParameter(1).ToDouble();
                    parameters.ConstLoss = parametersec.GetKeyword("ConstLoss").GetParameter(1).ToDouble();
                    parameters.Tlag = parametersec.GetKeyword("Tlag").GetParameter(1).ToDouble();
                    uhmmodel.UHMpar = parameters;

                    catchmentinfo.Now_Rfmodel = uhmmodel;
                    catchmentinfo.Update_Rfmodellist();
                    catchmentlist.Add(catchmentinfo);
                }
            }

            Catchment_List.Catchment_infolist = catchmentlist;
            Catchment_List.MaxCatchmentId = Catchment_List.Catchment_infolist.Count;
            Console.WriteLine("流域信息初始化成功!");
            pfsfile.Close();

            InitialDefault_CatchmentPointxy(ref Catchment_List);
            Console.WriteLine("流域控制点集合初始化成功!");
        }

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
                    if (Condition.GetKeyword("DataType").GetParameter(1).ToInt() == 0)
                    {
                        rfeva_fileitem.rain_filename = Condition.GetKeyword("Timeseries").GetParameter(1).ToString();
                        rfeva_fileitem.rain_itemname = Condition.GetSection("TimeSeriesItems", 1).GetKeyword("Item").GetParameter(1).ToString();
                    }
                    else if (Condition.GetKeyword("DataType").GetParameter(1).ToInt() == 1)
                    {
                        rfeva_fileitem.eva_filename = Condition.GetKeyword("Timeseries").GetParameter(1).ToString();
                        rfeva_fileitem.eva_itemname = Condition.GetSection("TimeSeriesItems", 1).GetKeyword("Item").GetParameter(1).ToString();
                    }
                }
            }
            return rfeva_fileitem;
        }

        public static void GetDefault_AverageEvp()
        {
            Dictionary<int, double> AverageEvaRate = new Dictionary<int, double>();

            DateTime starttime = new DateTime(2001, 1, 1, 0, 0, 0);
            DateTime endtime = new DateTime(2012, 12, 31, 0, 0, 0);
            Dictionary<DateTime, double> evpdic = RFService.GetHistory_Evadic(starttime, endtime);

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

            CatchmentList.AverageEvaRate = AverageEvaRate;
            Console.WriteLine("日平均蒸发量初始化成功!");
        }

        public static void GetDefault_AverageRF()
        {
            Dictionary<int, double> AverageRFRate = new Dictionary<int, double>();

            DateTime starttime = new DateTime(2001, 1, 1, 0, 0, 0);
            DateTime endtime = new DateTime(2012, 12, 31, 0, 0, 0);
            Dictionary<DateTime, double> rfdic = RFService.GetHistory_RFdic(starttime, endtime);

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

            CatchmentList.AverageRainfallRate = AverageRFRate;
            Console.WriteLine("日平均降雨量初始化成功!");
        }

        public static void GetDefault_Stand24_Rfmodeldic()
        {
            Dictionary<DateTime, double> rfdic = RFService.GetStand24_RFmodeldic();
            CatchmentList.Stand24_Rfmodel = rfdic;
            Console.WriteLine("标准24小时雨形模板初始化成功!");
        }

        public static void InitialDefault_CatchmentPointxy(ref CatchmentList Catchment_List)
        {
            for (int i = 0; i < Catchment_List.Catchment_infolist.Count; i++)
            {
                CatchmentInfo catchment = Catchment_List.Catchment_infolist[i];
                List<PointXY> catchmentpointlist = RFService.GetCatchment_Pointdic(catchment.Name);
                catchment.Catchment_Pointlist = catchmentpointlist;
            }
        }
        #endregion
    }
}
