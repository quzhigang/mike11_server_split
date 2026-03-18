using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using System.Globalization;
using System.Data;
using Kdbndp;
using System.IO;
using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;

using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace bjd_model.Mike11
{
    public partial class Nwk11
    {        
        #region ******************************新增产汇流流域连接*************************************
        //新增RR产汇流流域连接,不分NAM模型还是XAJ模型，先加入连接再说
        public static void Add_CatchmentConnect(ref HydroModel hydromodel, string catchmentname, Reach_Segment connect_reachseg)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            Catchment_ConnectList connectlist = hydromodel.Mike11Pars.Catchment_Connectlist;

            //获取集水区产汇流模型信息
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);

            //新建集水区连接
            Catchment_Connectinfo catchment_connectinfo;

            catchment_connectinfo.catchment_name = catchment.Name;
            catchment_connectinfo.catchment_area = catchment.Area;

            catchment_connectinfo.connect_reach = connect_reachseg;

            //在全局集水区连接集合中加入该集水区连接
            connectlist.AddCatchmentConnect(catchment_connectinfo);
        }

        //断开指定集水区和河道的耦合连接
        public static void Substract_catchmentconncet(ref HydroModel hydromodel, string catchmentname)
        {
            Catchment_ConnectList connectlist = hydromodel.Mike11Pars.Catchment_Connectlist;

            //先判断是否有该连接
            Catchment_Connectinfo catchment_connectinfo = connectlist.Get_CatchmentConnect(catchmentname);
            if (catchment_connectinfo.catchment_name == "")
            {
                Console.WriteLine("该集水区并未与河道耦合连接！");
                return;
            }

            //断开该连接
            connectlist.RemoveCatchmentConnect(catchmentname);
        }
        #endregion **************************************************************************************************
    }
}
