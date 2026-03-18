using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Kdbndp;
using Newtonsoft.Json;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike11;
using bjd_model.Mike21;

namespace bjd_model.CatchMent
{
    public partial class RR11
    {
        #region 流域操作
        // 新增流域并选择当前采用的产汇流模型
        public static void Add_NewCatchment(ref HydroModel hydromodel, string CatchmentName, List<PointXY> catchment_pointlist, RFModelType select_model)
        {
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

            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            catchmentlist.AddCatchment(newcatchmentinfo);

            switch (select_model)
            {
                case RFModelType.NAM:
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

        // 修改单个流域当前采用的产汇流模型类型
        public static void Change_Catchment_RFmodel(ref HydroModel hydromodel, string catchmentname, RFModelType rfmodeltype)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);

            if (catchment.Now_RfmodelType != rfmodeltype)
            {
                catchment.Now_RfmodelType = rfmodeltype;
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

        // 统一修改所有流域当前采用的产汇流模型类型
        public static void Change_AllCatchment_RFmodel(ref HydroModel hydromodel, RFModelType now_rfmodeltype)
        {
            RF_Pars Rfpars = hydromodel.RfPars;
            Rfpars.Rainfall_selectmodel = now_rfmodeltype;
            CatchmentList Catchmentlist = hydromodel.RfPars.Catchmentlist;

            for (int i = 0; i < Catchmentlist.Catchment_infolist.Count; i++)
            {
                Change_Catchment_RFmodel(ref hydromodel, Catchmentlist.Catchment_infolist[i].Name, now_rfmodeltype);
            }
        }

        // 按方案编码写入或更新数据库中的流域产汇流模型类型
        public static void Change_Catchment_RFmodel(string plan_code, object rf_model)
        {
            Dictionary<string, string> rf_models = new Dictionary<string, string>();
            string tableName = Mysql_GlobalVar.CATCHMENT_RFMODEL;

            if (rf_model == null || rf_model.ToString() == "")
            {
                rf_models = Get_Default_RFmodel();

                string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
                DataTable dt = Mysql.query(select_sql);
                if (dt != null)
                {
                    int nLen = dt.Rows.Count;
                    if (nLen != 0)
                    {
                        string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "'";
                        Mysql.Execute_Command(del_sql);
                    }
                }

                string fieldname_withoutid = "plan_code,code,rf_model";
                for (int i = 0; i < rf_models.Count; i++)
                {
                    object[] value = new object[] { plan_code, rf_models.ElementAt(i).Key, rf_models.ElementAt(i).Value };
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

                    string sql = "update " + tableName + " set rf_model = '" + rf_models.ElementAt(i).Value + "' where plan_code = '" + plan_code + "' and code = '" + rf_models.ElementAt(i).Key + "'";
                    Mysql.Execute_Command(sql);
                }
            }

            Console.WriteLine("产汇流模型类型更新完成！");
        }

        // 获取默认流域产汇流模型类型配置
        public static Dictionary<string, string> Get_Default_RFmodel()
        {
            Dictionary<string, string> rf_models = new Dictionary<string, string>();
            string tablename = Mysql_GlobalVar.CATCHMENT_BASEINFO;

            string select = "select * from " + tablename;

            List<List<Object>> rows;
            Mysql.Get_Rows(select, out rows);
            for (int i = 0; i < rows.Count; i++)
            {
                string code = rows[i][0].ToString();
                string modeltype = rows[i][16].ToString();

                if (!rf_models.Keys.Contains(code))
                {
                    rf_models.Add(code, modeltype);
                }
            }

            return rf_models;
        }

        // 获取指定方案的流域产汇流模型类型配置
        public static Dictionary<string, string> Get_Catchment_RFmodel(string plan_code)
        {
            Dictionary<string, string> rf_models = new Dictionary<string, string>();
            string tableName = Mysql_GlobalVar.CATCHMENT_RFMODEL;

            string sqlstr = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            for (int i = 0; i < nLen; i++)
            {
                string code = dt.Rows[i][2].ToString();
                string model_type = dt.Rows[i][3].ToString();
                rf_models.Add(code, model_type);
            }

            return rf_models;
        }

        // 将子流域出流结果写入数据库
        public static void Write_CatchmentDischarge_toDB(string plan_code, Dictionary<string, Dictionary<DateTime, double>> inflow_dic)
        {
            if (inflow_dic == null) return;
            if (inflow_dic.Count == 0) return;

            string tableName = Mysql_GlobalVar.MIKE11_CATCHMENT_SETDISCHARGE;

            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "'";
                Mysql.Execute_Command(del_sql);
            }

            string sql;
            for (int i = 0; i < inflow_dic.Count; i++)
            {
                string inflow = File_Common.Serializer_Obj(inflow_dic.ElementAt(i).Value, "yyyy-MM-dd HH:mm:ss");
                sql = "insert into " + tableName + "(plan_code,bsn_code,outflow_json)values(:plan_code,:bsn_code,:outflow_json)";
                KdbndpParameter[] mysqlPara = {
                                                new KdbndpParameter(":plan_code", plan_code),
                                                new KdbndpParameter(":bsn_code",inflow_dic.ElementAt(i).Key),
                                                new KdbndpParameter(":outflow_json", inflow)
                                              };
                Mysql.Execute_Command(sql, mysqlPara);
            }
        }

        // 删除指定方案的降雨结果记录
        public static void Del_RainFall_Res(string plan_code)
        {
            string tableName = Mysql_GlobalVar.CATCHMENT_RAINFALLRES_TABLENAME;

            string sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "'";
                Mysql.Execute_Command(del_sql);
            }
        }
        #endregion
    }
}
