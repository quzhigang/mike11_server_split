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

}
