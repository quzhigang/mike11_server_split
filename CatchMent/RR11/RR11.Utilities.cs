using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.PFS;
using Kdbndp;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike11;

namespace bjd_model.CatchMent
{
    public partial class RR11
    {
        #region *********************** 其他方法  -- 各流域开始新安江模拟并返回结果 *************************
        //各流域XAJ模型开始模拟，并制作结果dfs0文件
        public static void Get_Catchments_XAJSimulateRes(HydroModel hydromodel)
        {
            Dictionary<string, Dictionary<DateTime, double>> Catchment_RFreslist = new Dictionary<string, Dictionary<DateTime, double>>();

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

            Dfs0.Creat_XAJres_Dfs0(hydromodel, Catchment_RFreslist);
        }

        // 新建一个空白模型文件RR11
        public static void Build_BlankRR11(string newFilePath)
        {
            PFSBuilder builder = new PFSBuilder();
            builder.AddTarget("MIKE_RR");
            builder.AddKeyword("version_no");
            builder.AddDouble(101);

            builder.AddSection("CatchList");
            builder.EndSection();

            builder.AddSection("CombinedList");
            builder.EndSection();

            builder.AddSection("ParameterList");
            builder.EndSection();

            builder.AddSection("TimeseriesList");
            builder.AddKeyword("Max_Comb_Number");
            builder.AddDouble(8);
            builder.EndSection();

            builder.AddSection("BasinStationList");
            builder.AddKeyword("BasinListCount");
            builder.AddDouble(0);
            builder.EndSection();

            builder.AddSection("UHM_Common_Parameters");
            builder.AddSection("EnlargeRatio_ParameterList");
            builder.EndSection();
            builder.AddSection("NakayasuLoss_ParameterList");
            builder.EndSection();
            builder.AddSection("F1RsaLoss_ParameterList");
            builder.EndSection();
            builder.AddSection("QLSF_ParameterList");
            builder.EndSection();
            builder.AddSection("StartEvent");
            builder.AddKeyword("ApplyStart");
            builder.AddBool(false);
            builder.AddKeyword("Start");
            builder.AddDateTime(DateTime.Now);
            builder.EndSection();
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
            builder.EndSection();
            builder.EndSection();

            builder.AddSection("UserDefinedCombinationPeriods");
            builder.AddKeyword("ApplyCombination");
            builder.AddBool(false);
            builder.AddKeyword("PeriodCount");
            builder.AddDouble(0);
            builder.EndSection();

            builder.AddSection("MAWOutputOneFile");
            builder.AddKeyword("OutputOnlyOne");
            builder.AddBool(false);
            builder.AddKeyword("MAWFile");
            builder.AddFileName("");
            builder.EndSection();

            builder.AddSection("MAWCombNoFile");
            builder.AddKeyword("Apply");
            builder.AddBool(false);
            builder.AddKeyword("CombFile");
            builder.AddFileName("");
            builder.EndSection();

            builder.AddSection("BasinAreaDefinition");
            builder.AddKeyword("OriginX");
            builder.AddDouble(0);
            builder.AddKeyword("OriginY");
            builder.AddDouble(0);
            builder.AddKeyword("Width");
            builder.AddDouble(500);
            builder.AddKeyword("Height");
            builder.AddDouble(350);
            builder.EndSection();

            builder.AddSection("LAYERS");
            builder.EndSection();

            builder.EndSection();

            builder.Write(newFilePath);
            Console.WriteLine("新建模型成功！");
        }

        // 将流域模型参数写入数据库表
        public static void ParameterToMysql(string filePath, int basinNo, KdbndpConnection mysqlconn, string tableName)
        {
            PFSFile basin = new PFSFile(filePath, false);
            PFSSection MIKE_RR = basin.GetTarget("MIKE_RR", 1);

            PFSSection CatchList = MIKE_RR.GetSection("CatchList", 1);
            PFSSection Catchment = CatchList.GetSection("Catchment", basinNo);
            string Catchment_Name = Catchment.GetKeyword("Catchment_Name", 1).GetParameter(1).ToString();
            string Catchment_Model = Catchment.GetKeyword("Catchment_Model", 1).GetParameter(1).ToString();

            PFSSection ParameterList = MIKE_RR.GetSection("ParameterList", 1);

            if (Catchment_Model == "NAM")
            {
                PFSSection NAM_Parameters = ParameterList.GetSection(basinNo);
                PFSSection SurfaceRootzone = NAM_Parameters.GetSection("SurfaceRootzone", 1);
                double U_Max = SurfaceRootzone.GetKeyword("U_Max", 1).GetParameter(1).ToDouble();
                double L_Max = SurfaceRootzone.GetKeyword("L_Max", 1).GetParameter(1).ToDouble();
                double CQOF = SurfaceRootzone.GetKeyword("CQOF", 1).GetParameter(1).ToDouble();
                double CKIF = SurfaceRootzone.GetKeyword("CKIF", 1).GetParameter(1).ToDouble();
                double CK1 = SurfaceRootzone.GetKeyword("CK1", 1).GetParameter(1).ToDouble();
                double TOF = SurfaceRootzone.GetKeyword("TOF", 1).GetParameter(1).ToDouble();
                double TIF = SurfaceRootzone.GetKeyword("TIF", 1).GetParameter(1).ToDouble();
                PFSSection GroundWater = NAM_Parameters.GetSection("GroundWater", 1);
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
            Mysql mysql = new Mysql(Mysql_GlobalVar.NOWITEM_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port,
                                       Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);

            string tablename = Mysql_GlobalVar.HYDROGRAPH_TABLENAME;

            string select_sql = "select * from " + tablename + " where catchment_name = '" + catchmentname + "' ";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tablename + " where catchment_name = '" + catchmentname + "' ";
                Mysql.Execute_Command(del_sql);
            }

            string select = "select * from " + tablename + " where id = 1 ";
            List<string> strlist = Mysql.Get_FieldsName(select);
            string fieldname_withoutid = null;
            for (int i = 1; i < strlist.Count - 1; i++)
            {
                fieldname_withoutid = fieldname_withoutid + strlist[i] + " , ";
            }
            fieldname_withoutid = fieldname_withoutid + strlist[strlist.Count - 1];

            object[] value = new object[strlist.Count - 1];

            int uhmcount = Catchment_UHMdic.Count;
            for (int i = 0; i < uhmcount; i++)
            {
                value[0] = catchmentname;
                value[1] = Catchment_UHMdic.Keys.ElementAt(i);
                value[2] = Catchment_UHMdic.Values.ElementAt(i);

                Mysql.Execute_Command(Mysql.sql_insert(tablename, value, fieldname_withoutid));

                Console.WriteLine("正在写入第{0}个数据，总数据:{1}", i, uhmcount);
            }
        }
        #endregion *******************************************************************************************
    }
}
