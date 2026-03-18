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
}
