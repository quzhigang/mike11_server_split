using System;
using System.Collections.Generic;
using System.Linq;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike11;

namespace bjd_model.CatchMent
{
    //保存最新所有流域信息和其所采用产汇流模型信息的类
    [Serializable]
    public class CatchmentList
    {
        // 初始化流域列表及平均雨量蒸发模板
        public CatchmentList()
        {
            CatchmentList.AverageEvaRate = new Dictionary<int, double>();
            CatchmentList.AverageRainfallRate = new Dictionary<int, double>();
            CatchmentList.Stand24_Rfmodel = new Dictionary<DateTime, double>();
        }

        public List<CatchmentInfo> Catchment_infolist
        { get; set; }

        public int MaxCatchmentId
        { get; set; }

        public static Dictionary<int, double> AverageEvaRate
        { get; set; }

        public static Dictionary<int, double> AverageRainfallRate
        { get; set; }

        public static Dictionary<DateTime, double> Stand24_Rfmodel
        { get; set; }

        // 向集合中新增流域
        public void AddCatchment(CatchmentInfo newcatchment)
        {
            if (Catchment_infolist == null)
            {
                Catchment_infolist = new List<CatchmentInfo>();
            }

            if (!Catchment_infolist.Contains(newcatchment))
            {
                Catchment_infolist.Add(newcatchment);

                if (newcatchment.Id > MaxCatchmentId)
                {
                    MaxCatchmentId = newcatchment.Id;
                }

                RFService.Update_RFStation_CatchmentName(newcatchment.Name, newcatchment.Catchment_Pointlist);
            }
        }

        // 从集合中移除指定流域
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

        // 根据流域名称获取流域对象
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

        // 获取指定时段的平均日蒸发或降雨序列
        // 获取指定时段的平均日蒸发或降雨序列
        public static Dictionary<DateTime, double> Get_Average_DayEvaRfRate(DateTime starttime, DateTime endtime, Eva_RF eva_rf)
        {
            Dictionary<DateTime, double> Average_RateDic = new Dictionary<DateTime, double>();

            DateTime history_starttime = new DateTime(2001, 1, 1, 0, 0, 0);
            DateTime history_endtime = new DateTime(2012, 12, 31, 0, 0, 0);
            Dictionary<DateTime, double> source_dic;
            switch (eva_rf)
            {
                case Eva_RF.eva:
                    source_dic = RFService.GetHistory_Evadic(history_starttime, history_endtime);
                    break;
                default:
                    source_dic = RFService.GetHistory_RFdic(history_starttime, history_endtime);
                    break;
            }

            if (starttime.CompareTo(source_dic.Keys.ElementAt(0)) >= 0 && endtime.CompareTo(source_dic.Keys.ElementAt(source_dic.Count - 1)) <= 0)
            {
                Average_RateDic = Dfs0.Get_Subdic(source_dic, starttime, endtime);
                return Average_RateDic;
            }

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

            if (Average_RateDic.Keys.ElementAt(Average_RateDic.Count - 1) != endtime)
            {
                Average_RateDic.Add(endtime, endevarf);
            }
            return Average_RateDic;
        }

        // 获取指定时段的平均小时蒸发或降雨序列
        public static Dictionary<DateTime, double> Get_Average_HourEvaRfRate(DateTime starttime, DateTime endtime, Eva_RF eva_rf)
        {
            Dictionary<DateTime, double> dayevadic = Get_Average_DayEvaRfRate(starttime, endtime, eva_rf);
            Dictionary<DateTime, double> hourevarfdic = Dfs0.Insert_Accutedic(dayevadic, new TimeSpan(1, 0, 0));

            return hourevarfdic;
        }

        // 判断当前流域集合是否全部采用 XAJ 模型
        public bool AllisXAJ()
        {
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

    [Serializable]
    public class Nam : Rfmodel
    {
        // 使用默认参数构造 NAM 模型
        public Nam(CatchmentInfo catchmentinfo)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
        }

        // 使用指定参数构造 NAM 模型
        public Nam(CatchmentInfo catchmentinfo, NAMparameters pars)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.NAMpar = pars;
        }

        // 使用指定初始条件构造 NAM 模型
        public Nam(CatchmentInfo catchmentinfo, Nam_InitialCondition initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.NAMInitial = initial;
        }

        // 使用指定参数和初始条件构造 NAM 模型
        public Nam(CatchmentInfo catchmentinfo, NAMparameters pars, Nam_InitialCondition initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.NAMpar = pars;
            this.NAMInitial = initial;
        }

        public NAMparameters NAMpar
        { get; set; }

        public Nam_InitialCondition NAMInitial
        { get; set; }

        // 设置 NAM 模型默认参数和初始条件
        // 设置 NAM 模型默认参数和初始条件
        public void SetDefault(CatchmentInfo catchmentinfo)
        {
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

    [Serializable]
    public class Uhm : Rfmodel
    {
        // 使用默认参数构造 UHM 模型
        public Uhm(CatchmentInfo catchmentinfo)
            : base(catchmentinfo.Name)
        {
            SetDefault();
        }

        // 使用指定参数构造 UHM 模型
        public Uhm(CatchmentInfo catchmentinfo, UHMparameters pars)
            : base(catchmentinfo.Name)
        {
            SetDefault();
            this.UHMpar = pars;
        }

        public UHMparameters UHMpar
        { get; set; }

        // 设置 UHM 模型默认参数
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

    [Serializable]
    public class Xaj : Rfmodel
    {
        // 使用默认参数构造 XAJ 模型
        public Xaj(CatchmentInfo catchmentinfo)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
        }

        // 使用指定初始条件构造 XAJ 模型
        public Xaj(CatchmentInfo catchmentinfo, XajInitialConditional initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
            this.XajInitial = initial;
        }

        // 使用指定参数构造 XAJ 模型
        public Xaj(CatchmentInfo catchmentinfo, Xajparameters paras)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
            this.XajPar = paras;
        }

        // 使用指定参数和初始条件构造 XAJ 模型
        public Xaj(CatchmentInfo catchmentinfo, Xajparameters paras, XajInitialConditional initial)
            : base(catchmentinfo.Name)
        {
            SetDefault(catchmentinfo);
            this.Catchment_Area = catchmentinfo.Area;
            this.XajPar = paras;
            this.XajInitial = initial;
        }

        public double Catchment_Area
        { get; set; }

        public Xajparameters XajPar
        { get; set; }

        public XajInitialConditional XajInitial
        { get; set; }

        public Dictionary<DateTime, double> Day_P
        { get; set; }

        public Dictionary<DateTime, double> Step_P
        { get; set; }

        public Dictionary<DateTime, double> Day_em
        { get; set; }

        // 设置 XAJ 模型默认参数和初始条件
        // 设置 XAJ 模型默认参数和初始条件
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
            this.XajPar = paras;

            XajInitialConditional initial;
            initial.ua_day = 10;
            initial.la_day = 30;
            initial.da_day = 60;
            initial.baseFlow = 1;
            this.XajInitial = initial;

            Dictionary<DateTime, double> rf_eva_dic = new Dictionary<DateTime, double>();
            this.Day_P = rf_eva_dic;
            this.Step_P = rf_eva_dic;
            this.Day_em = rf_eva_dic;
        }

        // 执行 XAJ 模型计算并返回结果序列
        // 执行 XAJ 模型计算并返回结果序列
        public Dictionary<DateTime, double> Forecast(HydroModel hydromodel)
        {
            Dictionary<DateTime, double> Forecast_result = new Dictionary<DateTime, double>();

            SetModel_RfEvaDic(hydromodel);
            Forecast_result = FloodForecast.Forecast(this);

            return Forecast_result;
        }

        // 按当前方案时间为 XAJ 模型装载降雨蒸发序列
        public void SetModel_RfEvaDic(HydroModel hydromodel)
        {
            SimulateTime simulatetime = hydromodel.ModelGlobalPars.Simulate_time;
            DateTime starttime = simulatetime.Begin.AddDays(-1 * Model_Const.EVP_BEFORE_DAYCOUNT);
            DateTime endtime = simulatetime.End;
            Dictionary<DateTime, double> agorfdic = RFService.Get_CatchmentRainfall(this.Catchment_Name, starttime, endtime, 24);

            if (agorfdic.Keys.ElementAt(0) != starttime || agorfdic.Keys.ElementAt(agorfdic.Count - 1) != endtime)
            {
                this.Day_P = CatchmentList.Get_Average_DayEvaRfRate(starttime, endtime, Eva_RF.rainfall);
            }
            else
            {
                this.Day_P = agorfdic;
            }

            Dictionary<DateTime, double> resdic = Dfs0.GetRFdic_FromDb(hydromodel, this.Catchment_Name);
            TimeSpan new_timespan = new TimeSpan(0, (int)(Model_Const.XAJ_TIMESTEP_HOUR * 60), 0);
            Dictionary<DateTime, double> new_resdic = Dfs0.Insert_Accutedic(resdic, new_timespan);
            this.Step_P = new_resdic;

            Dictionary<DateTime, double> Day_em = CatchmentList.Get_Average_DayEvaRfRate(starttime, endtime, Eva_RF.eva);
            this.Day_em = Day_em;
        }
    }

    [Serializable]
    public class Rfmodel
    {
        // 构造产汇流模型基类
        public Rfmodel(string catchmentname)
        {
            this.Catchment_Name = catchmentname;
        }

        public string Catchment_Name
        { get; set; }
    }

    [Serializable]
    public class CatchmentInfo
    {
        // 根据流域基础信息构造流域对象
        // 根据流域基础信息构造流域对象
        public CatchmentInfo(int catchmentID, string name, List<PointXY> catchment_pointlist)
        {
            CO_SetDefault();
            this.Id = catchmentID;
            this.Name = name;
            double area = Math.Round(PointXY.Get_PolygonArea(catchment_pointlist) / 1000000, 3);
            this.Area = area;
            this.Catchment_Pointlist = catchment_pointlist;

            Set_RFmodelList();
        }

        public string Name
        { get; set; }

        public double Area
        { get; set; }

        public int Id
        { get; set; }

        public List<PointXY> Catchment_Pointlist
        { get; set; }

        public List<Rfmodel> Rfmodel_List
        { get; set; }

        public Rfmodel Now_Rfmodel
        { get; set; }

        public RFModelType Now_RfmodelType
        { get; set; }

        // 初始化流域对象默认值
        // 初始化流域对象默认值
        public void CO_SetDefault()
        {
            this.Name = "";
            this.Area = 0;

            List<PointXY> Catchment_Pointlist = new List<PointXY>();
            this.Catchment_Pointlist = Catchment_Pointlist;
        }

        // 创建流域对应的全部产汇流模型集合
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
            this.Now_RfmodelType = RFModelType.NAM;
            this.Now_Rfmodel = Rfmodel_List[0];
        }

        // 将当前采用模型同步回模型集合
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

                if (rfmodeltype == this.Now_RfmodelType && this.Rfmodel_List[i] != this.Now_Rfmodel)
                {
                    this.Rfmodel_List.RemoveAt(i);
                    this.Rfmodel_List.Add(this.Now_Rfmodel);
                    break;
                }
            }
        }
    }

    [Serializable]
    public class RfModel_RequestPars
    {
        public string plan_code;
        public string model_planname;
        public string start_time;
        public string end_time;
        public Dictionary<string, string> rf_model;

        // 构造默认的流域产汇流请求参数
        // 构造默认的流域产汇流请求参数
        public RfModel_RequestPars()
        {
            this.plan_code = "";
            this.model_planname = "";
            this.start_time = "";
            this.end_time = "";
            this.rf_model = new Dictionary<string, string>();
        }

        // 构造带完整字段的流域产汇流请求参数
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
