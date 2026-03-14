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
    public partial class HydroModel
    {
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