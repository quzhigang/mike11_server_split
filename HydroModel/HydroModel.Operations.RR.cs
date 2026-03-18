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
    public partial class HydroModel
    {
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
    }
}
