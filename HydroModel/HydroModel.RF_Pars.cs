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

}
