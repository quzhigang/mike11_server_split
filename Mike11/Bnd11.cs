using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.Collections;
using DHI.Mike1D.CrossSectionModule;
using System.IO;
using System.Globalization;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bjd_model.Mike11
{
    // 流量类型（时间序列还是常流量）
    [Serializable]
    public enum BdValueType
    {
        TS_File,
        Constant
    }

    //水质类型
    [Serializable]
    public enum AdSourceType
    {
        Solubility = 1,
        Dissolubility = 2
    }

    //优化方向 --库群优化调度
    [Serializable]
    public enum Optimize_Direction
    {
        min_target_discharge = 1,
        min_res_level = 2
    }

    //3种出流约束 --库群优化调度
    [Serializable]
    public enum Outflow_Constraint_Type
    {
        no = 1,
        no_outflow = 2,
        no_overflow = 3
    }

    //调度目标 --库群优化调度
    [Serializable]
    public class Dispatch_Target
    {
        public string name;
        public string stcd;
        public double max_discharge;

        public Dispatch_Target(string name, string stcd, double max_discharge)
        {
            this.name = name;
            this.stcd = stcd;
            this.max_discharge = max_discharge;
        }
    }

    //水库出流约束 --库群优化调度
    [Serializable]
    public class Res_Outflow_Constraint
    {
        public string name;
        public string stcd;
        public Outflow_Constraint_Type outflow_constraint;

        public Res_Outflow_Constraint(string name, string stcd, Outflow_Constraint_Type outflow_constraint)
        {
            this.name = name;
            this.stcd = stcd;
            this.outflow_constraint = outflow_constraint;
        }

        //获取相同stcd的水库
        public static Res_Outflow_Constraint Get_Res_Outflow_Constraint(List<Res_Outflow_Constraint> res_out_conslist,string stcd)
        {
            for (int i = 0; i < res_out_conslist.Count; i++)
            {
                if (res_out_conslist[i].stcd == stcd) return res_out_conslist[i];
            }
            return null;
        }
    }

    //水库水位约束 --库群优化调度
    [Serializable]
    public class Res_Level_Constraint
    {
        public string name;
        public string stcd;
        public string level_name;
        public double level_value;

        public Res_Level_Constraint(string name, string stcd, string level_name, double level_value)
        {
            this.name = name;
            this.stcd = stcd;
            this.level_name = level_name;
            this.level_value = level_value;
        }
    }


    //水库2种调度目标选择
    [Serializable]
    public enum Res_Target_Type
    {
        max_level,    //允许最高水位 max_level
        max_outq      //允许最大出库流量 max_outq
    }

    //调度目标 --单库调度目标
    [Serializable]
    public class Res_Dispatch_Target
    {
        public string name;
        public string stcd;
        public Res_Target_Type target;      //调度目标类型-- 最高允许水位max_level 或 最大允许出库流量max_outq
        public double value;               //调度目标最大允许值

        public Res_Dispatch_Target(string name, string stcd, Res_Target_Type target, double value)
        {
            this.name = name;
            this.stcd = stcd;
            this.target = target;
            this.value = value;
        }
    }

    public class Bnd11
    {
        #region ***************************从默认bnd11文件中获取已有边界信息*********************************
        //从默认bnd11文件中获取已有边界信息
        public static void GetDefault_Bnd11Info(string sourcefilename, ref BoundaryList Boundarylist)
        {
            //读取PFS文件
            PFSFile pfsfile = new PFSFile(sourcefilename);   //读取文件
            PFSSection target = pfsfile.GetTarget("BndCondition", 1);   //最外面的节
            PFSSection BndCndArray = target.GetSection("BndCndArray", 1);

            //从边界文件中找到所有边界条件
            List<Reach_Bd> boundary_list = new List<Reach_Bd>();
            for (int i = 0; i < BndCndArray.GetSectionsCount(); i++)
            {
                //先定义一个默认的边界条件对象
                Reach_Bd reachbd = new Reach_Bd();

                //边界类型、名称等边界定义
                PFSKeyword DescType = BndCndArray.GetSection(i + 1).GetKeyword("DescType");
                reachbd.Bd_description = DescType.GetParameter(1).ToInt();
                reachbd.Bd_type = DescType.GetParameter(2).ToInt();

                AtReach reachinfo;
                reachinfo.reachname = DescType.GetParameter(3).ToString();
                reachinfo.reachid = reachinfo.reachname + Model_Const.REACHID_HZ;
                reachinfo.chainage = DescType.GetParameter(4).ToDouble();
                reachbd.Reachinfo = reachinfo;

                reachbd.Endchainage = DescType.GetParameter(5).ToDouble();

                //是否包含AD模块
                PFSKeyword Inflow = BndCndArray.GetSection(i + 1).GetKeyword("Inflow");
                reachbd.IncludeAD = Inflow.GetParameter(2).ToBoolean();

                //水动力学边界值
                PFSParameter inflowpar = BndCndArray.GetSection(i + 1).GetSection("InflowArray", 1).GetKeyword("Inflow", 1).GetParameter(3);
                PFSParameter inflowpar1 = BndCndArray.GetSection(i + 1).GetSection("InflowArray", 1).GetKeyword("Inflow", 1).GetParameter(4);
                int ad_bdtype = BndCndArray.GetSection(i + 1).GetSection("InflowArray", 1).GetKeyword("Inflow", 1).GetParameter(8).ToInt(); //水质边界类型
                if (inflowpar.ToInt() == 0 && inflowpar1.ToFileNamePath() != "")  //边界值为dfs0文件
                {
                    reachbd.Valuetype = BdValueType.TS_File;
                    reachbd.Bd_filename = BndCndArray.GetSection(i + 1).GetSection("InflowArray", 1).GetKeyword("Inflow", 1).GetParameter(4).ToFileName();

                    //默认的边界文件用的是相对路径名，会导致求项目号出错，需要补全路径
                    if (reachbd.Bd_filename.StartsWith("."))
                    {
                        reachbd.Bd_filename = Path.GetDirectoryName(sourcefilename) + reachbd.Bd_filename.Substring(1);
                    }
                    reachbd.Bd_itemname = BndCndArray.GetSection(i + 1).GetSection("InflowArray", 1).GetKeyword("Inflow", 1).GetParameter(7).ToString();
                    reachbd.Bd_value = 0;
                }
                else   //边界值为常量
                {
                    reachbd.Valuetype = BdValueType.Constant;
                    reachbd.Bd_value = BndCndArray.GetSection(i + 1).GetSection("InflowArray", 1).GetKeyword("Inflow", 1).GetParameter(5).ToDouble();
                    reachbd.Bd_filename = "";
                    reachbd.Bd_itemname = "";
                }

                //水质边界值
                PFSSection comsec = BndCndArray.GetSection(i + 1).GetSection("ComponentArray", 1);
                if (reachbd.IncludeAD)
                {
                    int com_count = comsec.GetKeywordsCount();
                    List<BdValueType> Ad_valuetype = new List<BdValueType>();
                    List<int> Ad_comnumber = new List<int>();
                    List<int> Ad_bdtype = new List<int>();

                    List<double> Ad_value = new List<double>();
                    List<string> Ad_filename = new List<string>();
                    List<string> Ad_itemname = new List<string>();
                    for (int j = 0; j < com_count; j++)
                    {
                        BdValueType advaluetype = BdValueType.Constant;
                        double advalue = 0.0;
                        string adfilename = "";
                        string aditemname = "";
                        int ad_comnumber = comsec.GetKeyword("Component", j + 1).GetParameter(1).ToInt();

                        PFSParameter compar = comsec.GetKeyword("Component", j + 1).GetParameter(3);
                        PFSParameter compar1 = comsec.GetKeyword("Component", j + 1).GetParameter(4);
                        if (compar.ToInt() == 0 && compar1.ToFileNamePath() != "")  //边界值为dfs0文件
                        {
                            advaluetype = BdValueType.TS_File;
                            adfilename = comsec.GetKeyword("Component", j + 1).GetParameter(4).ToFileName();

                            //默认的边界文件用的是相对路径名，会导致求项目号出错，需要补全路径
                            if (adfilename.StartsWith("."))
                            {
                                adfilename = Path.GetDirectoryName(sourcefilename) + adfilename.Substring(1);
                            }
                            aditemname = comsec.GetKeyword("Component", j + 1).GetParameter(7).ToString();
                        }
                        else   //边界值为常量
                        {
                            advalue = comsec.GetKeyword("Component", j + 1).GetParameter(5).ToDouble();
                        }

                        Ad_valuetype.Add(advaluetype);
                        Ad_comnumber.Add(ad_comnumber);
                        Ad_bdtype.Add(ad_bdtype);
                        Ad_value.Add(advalue);
                        Ad_filename.Add(adfilename);
                        Ad_itemname.Add(aditemname);
                    }
                    reachbd.Ad_valuetype = Ad_valuetype;
                    reachbd.AD_comnumber = Ad_comnumber;
                    reachbd.AD_bdtype = Ad_bdtype;
                    reachbd.Ad_value = Ad_value;
                    reachbd.Ad_filename = Ad_filename;
                    reachbd.Ad_itemname = Ad_itemname;
                }

                //边界ID
                reachbd.BoundaryId = DescType.GetParameter(7).ToString();

                //水位流量关系
                reachbd.HQ = null;
                PFSSection QhArray = BndCndArray.GetSection(i + 1).GetSection("QhArray", 1);
                if (QhArray.GetKeywordsCount() != 0)  //水位流量关系节有关键字
                {
                    Dictionary<double, double> HQ = new Dictionary<double, double>();
                    for (int j = 0; j < QhArray.GetKeywordsCount(); j++)
                    {
                        HQ.Add(QhArray.GetKeyword(j + 1).GetParameter(2).ToDouble(), QhArray.GetKeyword(j + 1).GetParameter(1).ToDouble());
                    }
                    reachbd.HQ = HQ;
                }

                //加入边界集合中
                boundary_list.Add(reachbd);
            }

            //成为边界参数
            Boundarylist.Boundary_infolist = boundary_list;

            Console.WriteLine("边界信息初始化成功!");
            pfsfile.Close();
        }
        #endregion *******************************************************************************************


        #region ************************************** 更新bnd11文件 *****************************************
        // 提取最新的边界信息，更新bnd11文件
        public static void Rewrite_Bnd11_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Bnd11_filename;
            string outputfilename = hydromodel.Modelfiles.Bnd11_filename;
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;
            if (boundarylist == null) return;

            //打开PFS文件，开始编辑
            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection BndCondition = pfsfile.GetTarget("BndCondition", 1);   //目标

            //更新新安江流域连接的入流边界(如果选择的是新安江模型)
            Update_XAJcatchmentconnect_Bd(ref hydromodel);

            //更新边界数组节
            Update_BndCndArraySec(hydromodel, boundarylist, ref BndCondition);
            Console.WriteLine("流域基本信息节更新成功!");

            //重新生成bnd11文件
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("Bnd11一维边界条件文件更新成功!");
            Console.WriteLine("");
        }

        //更新新安江流域连接的入流边界
        public static void Update_XAJcatchmentconnect_Bd(ref HydroModel hydromodel)
        {
            Catchment_ConnectList catchment_connectlist = hydromodel.Mike11Pars.Catchment_Connectlist;
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            if (catchment_connectlist.CatchmentConnect_Infolist == null) return;

            for (int i = 0; i < catchment_connectlist.CatchmentConnect_Infolist.Count; i++)
            {
                CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchment_connectlist.CatchmentConnect_Infolist[i].catchment_name);
                if (catchment.Now_RfmodelType == RFModelType.XAJ)
                {
                    //获取连接河段信息
                    Reach_Segment reachinfo = catchment_connectlist.CatchmentConnect_Infolist[i].connect_reach;

                    //获取新安江模型
                    string catchmentname = catchment_connectlist.CatchmentConnect_Infolist[i].catchment_name;
                    Xaj xajmodel = catchmentlist.Get_Catchmentinfo(catchmentname).Now_Rfmodel as Xaj;

                    //进行新安江模型运算
                    Dictionary<DateTime, double> resultdic = xajmodel.Forecast(hydromodel);
                    Console.WriteLine("流域{0}新安江模型模拟完成!", catchment_connectlist.CatchmentConnect_Infolist[i].catchment_name);

                    //将计算得到的洪水流量过程结果制作成DFS0文件
                    string newdfs0_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + catchmentname + "_xaj_rl.dfs0";
                    Dfs0.Dfs0_Creat(newdfs0_filename, resultdic, dfs0type.discharge);

                    //将该dfs0文件按分布源加入边界
                    Add_NewInflowBd(ref hydromodel, reachinfo, newdfs0_filename, "discharge");
                }
            }
        }

        //更新边界数组节
        public static void Update_BndCndArraySec(HydroModel hydromodel, BoundaryList boundary_List, ref PFSSection BndCondition)
        {
            //清空原默认bnd11文件中的边界集合
            BndCondition.DeleteSection("BndCndArray", 1);  //删除的节也是单独排
            PFSSection BndCndArray = BndCondition.InsertNewSection("BndCndArray", 1);  //重新添加边界数组的节

            //重新逐个添加边界节
            List<Reach_Bd> boundarylist = boundary_List.Boundary_infolist;
            if (boundarylist == null) return;

            int secnumber = 1;
            string now_modeldir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);
            for (int i = 0; i < boundarylist.Count; i++)
            {
                //逐个替换dfs0为本模型目录
                Reach_Bd reachbd = boundarylist[i];
                if (reachbd.Bd_filename != "" && Path.GetDirectoryName(reachbd.Bd_filename) != now_modeldir)
                {
                    reachbd.Bd_filename = now_modeldir + @"\" + Path.GetFileName(reachbd.Bd_filename);
                }

                if (reachbd.Ad_filename.Count != 0)
                {
                    List<string> ad_filelist = new List<string>();
                    for (int j = 0; j < reachbd.Ad_filename.Count; j++)
                    {
                        if (reachbd.Ad_filename[j] == "") continue;

                        if (Path.GetDirectoryName(reachbd.Ad_filename[j]) != now_modeldir)
                        {
                            ad_filelist.Add(now_modeldir + @"\" + Path.GetFileName(reachbd.Ad_filename[j]));
                        }
                    }
                    if (ad_filelist.Count != 0) reachbd.Ad_filename = ad_filelist;
                }

                //逐个添加最新的边界
                Insert_BoundarySec(ref BndCndArray, ref secnumber, reachbd);
            }

        }

        //新增边界条件子节,注意插入的关键字和节是统一排序的，而获取是分组排序
        public static void Insert_BoundarySec(ref PFSSection sec, ref int secnumber, Reach_Bd newreach_bdinfo)
        {
            //在指定位置插入该建筑物的节
            PFSSection bnditemsec = sec.InsertNewSection("BndItem", secnumber);
            secnumber++;

            //插入节和关键字是统一排序的，先定义序号
            int sec_number = 1;
            int key_number = 1;

            //添加边界描述关键字和参数
            PFSKeyword key1 = bnditemsec.InsertNewKeyword("DescType", key_number);
            object[] key1_array ={
                                     newreach_bdinfo.Bd_description,
                                     newreach_bdinfo.Bd_type,
                                     newreach_bdinfo.Reachinfo.reachname,
                                     newreach_bdinfo.Reachinfo.chainage,
                                     newreach_bdinfo.Endchainage ,
                                     "",
                                     newreach_bdinfo.BoundaryId
                                 };
            Nwk11.InsertKeyPars(ref key1, ref key_number, key1_array);

            //添加中间的默认关键字和参数
            Insert_Default_Bndsec(newreach_bdinfo, ref bnditemsec, ref sec_number, ref key_number);

            //边界条件值节--InflowArray
            PFSSection InflowArraysec = bnditemsec.InsertNewSection("InflowArray", sec_number);
            PFSKeyword Inflowkey = InflowArraysec.InsertNewKeyword("Inflow", 1);

            Filestring filestr;
            filestr.filename = newreach_bdinfo.Bd_filename;
            if (newreach_bdinfo.Valuetype == BdValueType.Constant)
            {
                object[] Inflowkey_array = { 2, 0, 1, filestr, newreach_bdinfo.Bd_value, 0, newreach_bdinfo.Bd_itemname, 0, 1 };
                Nwk11.InsertKeyPars(ref Inflowkey, Inflowkey_array);
            }
            else
            {
                //根据项目名得到项目编号  从1开始排
                int itemnumber = Dfs0.Dfs0_Reader_GetItemId(newreach_bdinfo.Bd_filename, newreach_bdinfo.Bd_itemname);

                object[] Inflowkey_array = { 2, 0, 0, filestr, newreach_bdinfo.Bd_value, itemnumber, newreach_bdinfo.Bd_itemname, 0, 1 };
                Nwk11.InsertKeyPars(ref Inflowkey, Inflowkey_array);
            }
            sec_number++;

            //添加水位流量关系节
            PFSSection Qhsec = bnditemsec.InsertNewSection("QhArray", sec_number);
            if (newreach_bdinfo.HQ != null)
            {
                for (int i = 0; i < newreach_bdinfo.HQ.Count; i++)
                {
                    PFSKeyword qhkey = Qhsec.InsertNewKeyword("Qh", i + 1);
                    object[] qhkey_array = { newreach_bdinfo.HQ.ElementAt(i).Value, newreach_bdinfo.HQ.ElementAt(i).Key };
                    Nwk11.InsertKeyPars(ref qhkey, qhkey_array);
                }
            }
            sec_number++;

            //添加最后一个AD组分节
            PFSSection ComponentArraysec = bnditemsec.InsertNewSection("ComponentArray", sec_number);
            if (newreach_bdinfo.IncludeAD)
            {
                for (int i = 0; i < newreach_bdinfo.Ad_valuetype.Count; i++)
                {
                    Filestring adfilestr;
                    adfilestr.filename = newreach_bdinfo.Ad_filename[i];
                    PFSKeyword comkey = ComponentArraysec.InsertNewKeyword("Component", i + 1);
                    //组分序号
                    int com_number = newreach_bdinfo.AD_comnumber[i];
                    if (newreach_bdinfo.Ad_valuetype[i] == BdValueType.Constant)  //常量
                    {
                        object[] comkey_array = { com_number, 0, 1, adfilestr, newreach_bdinfo.Ad_value[i], 0, "", 0, 1 };
                        Nwk11.InsertKeyPars(ref comkey, comkey_array);
                    }
                    else
                    {
                        //根据项目名得到项目编号  从1开始排
                        int itemnumber = Dfs0.Dfs0_Reader_GetItemId(newreach_bdinfo.Ad_filename[i], newreach_bdinfo.Ad_itemname[i]);

                        object[] comkey_array = { com_number, 0, 0, adfilestr, 0, itemnumber, newreach_bdinfo.Ad_itemname[i], 0, 1 };
                        Nwk11.InsertKeyPars(ref comkey, comkey_array);
                    }
                }
            }

        }

        //添加边界条件节中的默认关键字和节
        public static void Insert_Default_Bndsec(Reach_Bd newreach_bdinfo, ref PFSSection sec, ref int sec_number, ref int key_number)
        {
            PFSKeyword key1 = sec.InsertNewKeyword("OpenDesc", key_number);
            object[] key1_array = { 0, 0 };
            Nwk11.InsertKeyPars(ref key1, ref key_number, key1_array);

            PFSKeyword key2 = sec.InsertNewKeyword("Dam", key_number);
            object[] key2_array = { 0, 0, 0 };
            Nwk11.InsertKeyPars(ref key2, ref key_number, key2_array);

            PFSKeyword key3 = sec.InsertNewKeyword("Inflow", key_number);
            if (sec.GetKeyword("DescType", 1).GetParameter(2).ToInt() == 2 &&  //水位流量关系
                sec.GetKeyword("DescType", 1).GetParameter(1).ToInt() != 1 &&  //点源
                sec.GetKeyword("DescType", 1).GetParameter(1).ToInt() != 2)    //分布源
            {
                object[] key3_array = { false, newreach_bdinfo.IncludeAD, false, false };
                Nwk11.InsertKeyPars(ref key3, ref key_number, key3_array);
            }
            else
            {
                object[] key3_array = { true, newreach_bdinfo.IncludeAD, false, false };  //第一个参数为true后，"include HD caculation"将被选中
                Nwk11.InsertKeyPars(ref key3, ref key_number, key3_array);
            }

            PFSKeyword key4 = sec.InsertNewKeyword("ADRR", key_number);
            object[] key4_array = { "", 0, 0 };
            Nwk11.InsertKeyPars(ref key4, ref key_number, key4_array);

            PFSKeyword key5 = sec.InsertNewKeyword("QhADM12", key_number);
            object[] key5_array = new object[3] { 2, 1, 0 };
            if (sec.GetKeyword("DescType", 1).GetParameter(1).ToInt() == 0 && sec.GetKeyword("DescType", 1).GetParameter(2).ToInt() == 2) //开边界，且水位流量关系(下边界)
            {
                key5_array[0] = 1; key5_array[1] = 0.15; key5_array[2] = 0;
            }
            else
            {
                key5_array[0] = 2; key5_array[1] = 1; key5_array[2] = 0;
            }
            Nwk11.InsertKeyPars(ref key5, ref key_number, key5_array);

            PFSKeyword key6 = sec.InsertNewKeyword("AutoCalQh", key_number);
            object[] key6_array = { 0, 0.001, 40 };
            Nwk11.InsertKeyPars(ref key6, ref key_number, key6_array);

            PFSKeyword key7 = sec.InsertNewKeyword("BndTS", key_number);
            Filestring blankstr;
            blankstr.filename = "";
            object[] key7_array = { 0, blankstr, 0, 0, "", 0, 1 };
            Nwk11.InsertKeyPars(ref key7, ref key_number, key7_array);

            PFSSection fractionsec = sec.InsertNewSection("FractionArray", sec_number);
            sec_number++;
            PFSSection hdarraysec = sec.InsertNewSection("HDArray", sec_number);
            sec_number++;

        }

        #endregion ********************************************************************************************


        #region **********************边界操作 -- 修改边界条件 **************************************
        //综合判断来修改边界条件
        public static void Change_Reach_Boundry(HydroModel hydromodel, string bnd_type, string bnd_value)
        {
            string plan_code = hydromodel.Modelname;
            DateTime plan_start_time = hydromodel.ModelGlobalPars.Simulate_time.Begin.AddHours(hydromodel.ModelGlobalPars.Ahead_hours);

            if (bnd_type == "rf_model")
            {
                Dictionary<string, string> catchment_rfmodel = RR11.Get_Catchment_RFmodel(plan_code);
                if (catchment_rfmodel == null) RR11.Change_Catchment_RFmodel(plan_code, "");
                Thread.Sleep(2000);  //等待2秒，防止数据库没写完，已经开始请求产汇流计算了，没有默认模型而导致出错

                //采用产汇流模型计算洪水，并修改边界条件(淮干全部子流域产汇流模型用NAM模型)
                //Dictionary<string, Dictionary<DateTime, double>> catchment_q = Mysql_GlobalVar.now_instance
                //== "wg_mike11" ? hydromodel.Modify_BndID_ToDischargeDic_FromNAM() : hydromodel.Modify_BndID_ToDischargeDic();
                Dictionary<string, Dictionary<DateTime, double>> catchment_q = hydromodel.Modify_BndID_ToDischargeDic();

                //开辟新线程判断是否为新的场次降雨洪水，如果不是则只是更新该方案的场次洪水ID字段，否则增加新场次洪水信息后再更新方案的场次洪水ID
                Add_Update_RainFloodInfo(hydromodel);

                //更新模型参数数据库中mike11边界条件字段
                Dictionary<string, object> bnd_dic = new Dictionary<string, object>() { };
                bnd_dic.Add("bnd_type", "降雨计算洪水");
                Dictionary<string, Dictionary<DateTime, double>> catchment_q1 = Dfs0.Del_Hours_InheadDic(catchment_q, plan_start_time);
                bnd_dic.Add("bnd_value", catchment_q1);
                Hd11.Update_ModelPara_DBInfo(hydromodel.Modelname, "mike11_bnd_info", File_Common.Serializer_Obj(bnd_dic));
            }
            else if (bnd_type == "no_inflow")
            {
                //不考虑入流
                hydromodel.Modify_BndID_ToNoInflow();

                //删除降雨数据库
                RR11.Del_RainFall_Res(plan_code);

                //更新模型参数数据库中mike11边界条件字段
                Dictionary<string, object> bnd_dic = new Dictionary<string, object>() { { "bnd_type", "无洪水入流" }, { "bnd_value", null } };
                Hd11.Update_ModelPara_DBInfo(hydromodel.Modelname, "mike11_bnd_info", File_Common.Serializer_Obj(bnd_dic));
            }
            else if (bnd_type == "catchment_inflow")
            {
                //指定各子流域入流过程
                Dictionary<string, Dictionary<DateTime, double>> inflow_dic =
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, double>>>(bnd_value);

                //删除降雨数据库
                RR11.Del_RainFall_Res(plan_code);

                //加上前面的时间，修改边界条件
                Dictionary<string, Dictionary<DateTime, double>> inflow_dic1 = Dfs0.Add_Hours_InheadDic(inflow_dic, hydromodel.ModelGlobalPars.Ahead_hours);
                Bnd11.Modify_Bnds_ToDischargeDic(ref hydromodel, inflow_dic1);

                //更新模型参数数据库中mike11边界条件字段
                Dictionary<string, object> bnd_dic = new Dictionary<string, object>() { };
                bnd_dic.Add("bnd_type", "指定子流域洪水");
                Dictionary<string, Dictionary<DateTime, double>> inflow_dic2 = Dfs0.Del_Hours_InheadDic(inflow_dic1, plan_start_time);
                bnd_dic.Add("bnd_value", inflow_dic2);
                Hd11.Update_ModelPara_DBInfo(hydromodel.Modelname, "mike11_bnd_info", File_Common.Serializer_Obj(bnd_dic));
            }
            else if (bnd_type == "reach_inflow")
            {
                //仅指定部分特定边界入流过程(用于计算设计洪水)
                Dictionary<string, Dictionary<DateTime, double>> inflow_dic =
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, double>>>(bnd_value);

                //删除降雨数据库
                RR11.Del_RainFall_Res(plan_code);

                //加上前面的时间，修改边界条件
                Dictionary<string, Dictionary<DateTime, double>> bnd_inflow_dic1 = Dfs0.Add_Hours_InheadDic(inflow_dic, hydromodel.ModelGlobalPars.Ahead_hours);
                Bnd11.Modify_Bnds_ToDischargeDic(ref hydromodel, bnd_inflow_dic1,true);

                //更新模型参数数据库中mike11边界条件字段
                Dictionary<string, object> bnd_dic = new Dictionary<string, object>() { };
                bnd_dic.Add("bnd_type", "指定河道洪水");
                Dictionary<string, Dictionary<DateTime, double>> inflow_dic2 = Dfs0.Del_Hours_InheadDic(bnd_inflow_dic1, plan_start_time);
                bnd_dic.Add("bnd_value", inflow_dic2);
                Hd11.Update_ModelPara_DBInfo(hydromodel.Modelname, "mike11_bnd_info", File_Common.Serializer_Obj(bnd_dic));
            }
        }

        //开辟新线程判断是否为新的场次降雨洪水，如果不是则只是更新该方案的场次洪水ID字段，否则增加新场次洪水信息后再更新方案的场次洪水ID
        public static void Add_Update_RainFloodInfo(HydroModel hydromodel)
        {
            //判断该方案的业务模型类型，如果是一二维洪水调度或优化调度，则再新增场次洪水
            string business_code = HydroModel.Get_BusinessCode_FromDB(hydromodel.Modelname);
            if(business_code == "flood_dispatch_wg" || business_code == "optimize_dispatch_wg" 
                || business_code == "flood_dispatch_route_wg")
            {
                //开辟新线程，处理相关信息
                Thread newth = new Thread(Update_RainFloodInfo);
                newth.IsBackground = true;

                string start_time = SimulateTime.TimetoStr(hydromodel.ModelGlobalPars.Simulate_time.Begin);
                string end_time = SimulateTime.TimetoStr(hydromodel.ModelGlobalPars.Simulate_time.End);
                object[] parameters = new object[] { hydromodel.Modelname, start_time, end_time };
                newth.Start(parameters);
            }
        }

        //通过判断是否为新的场次降雨洪水，进行相应操作
        public static void Update_RainFloodInfo(object par)
        {
            //方案ID和模拟起止时间
            object[] parameters = (object[])par;
            string plan_code = (string)parameters[0];
            DateTime start_time = SimulateTime.StrToTime((string)parameters[1]);
            DateTime end_time = SimulateTime.StrToTime((string)parameters[2]);

            //通过接口请求获取该方案的降雨信息
            string request_url = Mysql_GlobalVar.arearain_serverurl + "?planCode=" + plan_code;
            string request_res = File_Common.Get_HttpReSponse(request_url);
            dynamic jsonObject = JsonConvert.DeserializeObject(request_res);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());
            double total_rain = Math.Round(double.Parse(json_obj["sum"].ToString()), 1);
            double duration = Math.Round(double.Parse(json_obj["duration"].ToString()), 1);
            double max_rain = Math.Round(double.Parse(json_obj["max"].ToString()), 1);
            string source = json_obj["source"].ToString();

            // 提取 rain 下的 t 数组（字符串列表）
            JArray tArray = json_obj["rain"]["t"] as JArray;
            List<string> time_list = tArray.ToObject<List<string>>();
            JArray vArray = json_obj["rain"]["v"] as JArray;
            List<double> rain_list = new List<double>();
            foreach (var item in vArray)
            {
                double value = double.Parse(item.ToString());
                rain_list.Add(value);
            }

            //获取场次降雨洪水清单信息,判断是否为新的场次降雨洪水
            bool is_new_rain = true; string plan_floodid = "";
            List<Rain_Flood_Info> rain_flood_info = Rain_Flood_Info.Get_ReachFloodInfo_FromDB();
            for (int i = 0; i < rain_flood_info.Count; i++)
            {
                Rain_Flood_Info rain_flood = rain_flood_info[i];

                //如果中间有一个场次降雨与该方案的降雨完全相同，则认为已经存在该场次降雨
                if (rain_flood.start_time == start_time && rain_flood.end_time == end_time 
                    && Math.Round(rain_flood.total_rain) == Math.Round(total_rain))
                {
                    is_new_rain = false; plan_floodid = rain_flood.flood_id;
                    break;
                }
            }

            //如果是新的场次降雨，则新增场次降雨信息
            int year = start_time.Year;
            string start_monthDay = start_time.ToString("MMdd");
            string end_monthDay = end_time.ToString("MMdd");
            if (is_new_rain)
            {
                int total_rain_qz = (int)Math.Round(total_rain / 10.0) * 10;
                string flood_id = $"flood_{year}_{start_monthDay}_{end_monthDay}_{total_rain_qz}mm";
                plan_floodid = flood_id;
                string flood_name; string flood_desc;
                if (source == "0")
                {
                    int maxIndex = rain_list.IndexOf(rain_list.Max());
                    DateTime maxrain_monthDay = SimulateTime.StrToTime(time_list[maxIndex]);
                    
                    //如果是历史时段降雨
                    flood_name = $"{year}年{maxrain_monthDay.Month}月{maxrain_monthDay.Day}降雨";
                    flood_desc = $"降雨发生在{year}年{start_time.Month}月{start_time.Day}日至{end_time.Month}月{end_time.Day}日，" +
                        $"历时{duration}h，峰值时刻{maxrain_monthDay.Month}月{maxrain_monthDay.Day}日{maxrain_monthDay.Hour}时，全流域累计降雨量{total_rain}mm。";
                }
                else
                {
                    //如果是指定降雨量
                    flood_name = $"全流域{duration}h降雨{Math.Round(total_rain)}mm";
                    flood_desc = $"全流域{duration}h累积降雨{Math.Round(total_rain)}mm，降雨历时{duration}h，最大雨强{max_rain}mm/h。";
                }

                //组合对象插入数据库
                Rain_Flood_Info rain_info = new Rain_Flood_Info(flood_id, flood_name, flood_desc, start_time, end_time, duration,
                    total_rain, max_rain, plan_code);
                Rain_Flood_Info.Insert_RainInfo(rain_info);
            }

            //更新方案基本信息表里的场次洪水信息
            Rain_Flood_Info.Update_ModelRainInfo(plan_code, plan_floodid);
        }

        //修改所有相同边界条件ID的流量过程,不同的增加
        public static void Modify_Bnds_ToDischargeDic(ref HydroModel hydromodel,
            Dictionary<string, Dictionary<DateTime, double>> bnd_q, bool allow_insert_newbnd = false)
        {
            for (int i = 0; i < bnd_q.Count; i++)
            {
                string bnd_id = bnd_q.ElementAt(i).Key;
                Dictionary<DateTime, double> discharge_dic = bnd_q.ElementAt(i).Value;

                //获取边界条件
                Reach_Bd reach_bd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(bnd_id);

                //如果获取到该边界条件ID
                if (reach_bd != null)
                {
                    //修改该河道上边界条件的值
                    reach_bd.Valuetype = BdValueType.TS_File;

                    //创建入流过程的dfs0文件
                    string dfs0_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + bnd_id + "_q.dfs0";
                    Dfs0.Dfs0_Creat(dfs0_in_filename, discharge_dic, dfs0type.discharge);

                    reach_bd.Bd_filename = dfs0_in_filename;
                    reach_bd.Bd_itemname = "discharge";
                }
                else if (allow_insert_newbnd) //如果获取不到该边界条件ID，即为新的边界条件,此时在上游增加拦截堰
                {
                    //获取经纬度"jd11407411_wd3469530"和桩号位置
                    var parts = bnd_id.Split('_');
                    double jd = double.Parse(parts[0].Substring(2).Insert(3, "."));
                    double wd = double.Parse(parts[1].Substring(2).Insert(2, "."));
                    PointXY pointxy = PointXY.Get_Pointxy(jd, wd);
                    PointXY xy_pro = PointXY.CoordTranfrom(pointxy, 4326, 4547, 3);
                    AtReach atreach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, xy_pro);

                    //插入新的边界
                    string new_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + bnd_id + "_q.dfs0";
                    Dfs0.Dfs0_Creat(new_in_filename, discharge_dic, dfs0type.discharge);
                    Bnd11.Add_NewInflowBd(ref hydromodel, atreach, new_in_filename, "discharge", bnd_id);

                    //增加非可控建筑物--拦截堰，防止向上游倒灌
                    AtReach atreach_up = AtReach.Get_Atreach(atreach.reachname, atreach.chainage - 200.0);
                    string strname = atreach_up.reachname + Math.Round(atreach_up.chainage).ToString() + "Weir";
                    Nwk11.Add_WeirGate_AtReach(ref hydromodel, strname, atreach_up);
                }
            }
        }


        //上边界修改 -- 将指定ID的边界条件改为常量值入流
        public static void Modify_BndID_ToConstDischarge(ref HydroModel hydromodel, string bnd_id, double const_discharge)
        {
            //获取边界条件
            Reach_Bd reach_bd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(bnd_id);
            if (reach_bd == null) return;

            //获取入流类型
            reach_bd.Bd_type = Reach_Bd.Get_InflowBndType(reach_bd.Bd_description);

            //修改该河道上边界条件的值
            reach_bd.Valuetype = BdValueType.Constant;
            reach_bd.Bd_value = const_discharge;
        }


        //上边界修改 -- 改为常量值入流
        public static void Modify_ReachUpBnd_ToConstDischarge(ref HydroModel hydromodel, string reachname, double const_discharge)
        {
            //判断该河道上边界是否存在，如果不存在(上游与其他河道连接),直接返回
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            if (reach.upstream_connect.reachname != "")
            {
                Console.WriteLine("该河道上边界不存在！");
                return;
            }

            //获取河道上游边界条件
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.chainage = reach.start_chainage;
            atreach.reachid = reach.reachtopoid;
            Reach_Bd reach_upbd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(atreach);

            reach_upbd.Bd_type = 1;

            //修改该河道上边界条件的值
            reach_upbd.Valuetype = BdValueType.Constant;
            reach_upbd.Bd_value = const_discharge;
        }

        //上边界修改 -- 改为常量水位
        public static void Modify_ReachUpBnd_ToConstwl(ref HydroModel hydromodel, string reachname, double const_waterlevel)
        {
            //判断该河道下边界是否存在，如果不存在(下游与其他河道连接),直接返回
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            if (reach.downstream_connect.reachname != "")
            {
                Console.WriteLine("该河道上边界不存在！");
                return;
            }

            //获取河道上游边界条件
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.chainage = reach.start_chainage;
            atreach.reachid = reach.reachtopoid;
            Reach_Bd reach_upbd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(atreach);

            //修改边界类型为water level
            reach_upbd.Bd_type = 5;  //5代表边界条件类型为water level
            reach_upbd.Valuetype = BdValueType.Constant;
            reach_upbd.Bd_value = const_waterlevel;
        }


        //上边界修改 -- 改为入流时间过程
        public static void Modify_ReachUpBnd_ToDischargeDic(ref HydroModel hydromodel, string reachname, Dictionary<DateTime, double> discharge_dic)
        {
            //判断该河道上边界是否存在，如果不存在(上游与其他河道连接),直接返回
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            if (reach.upstream_connect.reachname != "")
            {
                Console.WriteLine("该河道上边界不存在！");
                return;
            }

            //获取河道上游边界条件
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.chainage = reach.start_chainage;
            atreach.reachid = reach.reachtopoid;
            Reach_Bd reach_upbd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(atreach);

            reach_upbd.Bd_type = 1;

            //修改该河道上边界条件的值
            reach_upbd.Valuetype = BdValueType.TS_File;

            //创建入流过程的dfs0文件
            string dfs0_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + atreach.reachname + "_up.dfs0";
            Dfs0.Dfs0_Creat(dfs0_in_filename, discharge_dic, dfs0type.discharge);

            reach_upbd.Bd_filename = dfs0_in_filename;
            reach_upbd.Bd_itemname = "discharge";
        }

        //下边界修改 -- 改为水位流量关系
        public static void Modify_ReachDownBnd_ToQH(ref HydroModel hydromodel, string reachname)
        {
            //判断该河道下边界是否存在，如果不存在(下游与其他河道连接),直接返回
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            if (reach.downstream_connect.reachname != "")
            {
                Console.WriteLine("该河道下边界不存在！");
                return;
            }

            //获取河道下游边界条件
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.chainage = reach.end_chainage;
            atreach.reachid = reach.reachtopoid;
            Reach_Bd reach_upbd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(atreach);

            //已经是水位流量关系了则不再修改
            if (reach_upbd.Bd_type == 2)
            {
                Console.WriteLine("该河道下边界已经是水位流量关系了！");
                return;
            }

            //获取河道下游出口水位流量关系
            Dictionary<double, double> new_out_hq = Xns11.Get_ReachEnd_QH(hydromodel.Modelfiles.Xns11_filename, reach.reachname, reach.reachtopoid);

            //修改边界类型和Q~H关系
            reach_upbd.Bd_type = 2;
            reach_upbd.HQ = new_out_hq;
        }

        //下边界修改 -- 改为常量水位
        public static void Modify_ReachDownBnd_ToConstwl(ref HydroModel hydromodel, string reachname, double const_waterlevel)
        {
            //判断该河道下边界是否存在，如果不存在(下游与其他河道连接),直接返回
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            if (reach.downstream_connect.reachname != "")
            {
                Console.WriteLine("该河道下边界不存在！");
                return;
            }

            //获取河道下游边界条件
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.chainage = reach.end_chainage;
            atreach.reachid = reach.reachtopoid;
            Reach_Bd reach_upbd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(atreach);

            //修改边界类型为water level
            reach_upbd.Bd_type = 5;  //5代表边界条件类型为water level
            reach_upbd.Valuetype = BdValueType.Constant;
            reach_upbd.Bd_value = const_waterlevel;
        }

        //下边界修改 -- 改为水位过程
        public static void Modify_ReachDownBnd_Towldic(ref HydroModel hydromodel, string reachname, Dictionary<DateTime, double> waterleveldic)
        {
            //判断该河道下边界是否存在，如果不存在(下游与其他河道连接),直接返回
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            if (reach.downstream_connect.reachname != "")
            {
                Console.WriteLine("该河道下边界不存在！");
                return;
            }

            //获取河道下游边界条件
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.chainage = reach.end_chainage;
            atreach.reachid = reach.reachtopoid;
            Reach_Bd reach_upbd = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(atreach);

            //修改边界类型为water level
            reach_upbd.Bd_type = 5;  //5代表边界条件类型为water level
            reach_upbd.Valuetype = BdValueType.TS_File;

            //创建水位过程的dfs0文件
            string dfs0_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + atreach.reachname + "_down.dfs0";
            Dfs0.Dfs0_Creat(dfs0_filename, waterleveldic, dfs0type.waterlevel);

            reach_upbd.Bd_filename = dfs0_filename;
            reach_upbd.Bd_itemname = "waterlevel";
        }


        #endregion **********************************************************************************


        #region **************** 边界操作 -- 因为新增河道而新增(新边界)或删除边界(原边界)**********************
        //从新增的河道信息中相应增加入流、出流边界条件,新河道下边界的水位流量关系自己从新断面文件中提取
        //若新增的河道上游连接河道 与文件中出流边界同河名、同桩号，则替换掉该出流边界条件，如XHH2 50310)
        //若新增的河道下游连接河道 与文件中入流边界同河名、同桩号，则替换掉该入流边界条件，如TG 0

        //增加新河道的第3步操作 -- 根据新河道的连接判断，按默认自动增加边界条件,更新边界条件集合 -- 上边界入流默认常量0.1m3/s
        public static void Add_NewReach_Bnd(ref HydroModel hydromodel, string new_reachname)
        {
            //获取已经加入河道集合的新河道
            ReachInfo newreach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(new_reachname);

            //更加上下游连接河道信息判断连接情况
            if (newreach.upstream_connect.reachname != "")
            {
                //上边界条件无，下边界设置为水位流量关系

                //断面文件需要单独更新下，否则获取不到断面的处理数据
                Xns11.Rewrite_Xns11_UpdateFile(hydromodel);

                //从xns11文件中得到新河道下游出口的水位流量关系
                Dictionary<double, double> new_out_hq = Xns11.Get_ReachEnd_QH(hydromodel.Modelfiles.Xns11_filename, newreach.reachname, newreach.reachtopoid);
                Add_NewReachBd(ref hydromodel, newreach, new_out_hq);
            }
            else if (newreach.downstream_connect.reachname != "")
            {
                //上边界设置为默认流量的恒定入流，下边界条件无
                double new_in_value = Model_Const.UPBOUND_DISCHARGE;
                Add_NewReachBd(ref hydromodel, newreach, new_in_value);
            }
            else   //上下游都没有连接河道
            {
                //上边界设置为默认流量的恒定入流，下边界设置为水位流量关系

                double new_in_value = Model_Const.UPBOUND_DISCHARGE;

                //断面文件需要单独更新，否则获取不到断面的处理数据
                Xns11.Rewrite_Xns11_UpdateFile(hydromodel);

                //从xns11文件中得到新河道下游出口的水位流量关系
                Dictionary<double, double> new_out_hq = Xns11.Get_ReachEnd_QH(hydromodel.Modelfiles.Xns11_filename, newreach.reachname, newreach.reachtopoid);
                Add_NewReachBd(ref hydromodel, newreach, new_in_value, new_out_hq);
            }

            Console.WriteLine("增加新边界成功！");
        }

        //适合于新增的独立河道，上游入流为常量，下游水位流量关系
        public static void Add_NewReachBd(ref HydroModel hydromodel, ReachInfo newreach, double new_in_value, Dictionary<double, double> new_out_hq)
        {
            //定义新河道的上下游边界条件
            Reach_Bd new_in_bd;
            Reach_Bd new_out_hqbd;
            Define_NewReachBd(newreach, new_in_value, new_out_hq, out new_in_bd, out new_out_hqbd);

            //通过新河道与老河道连接关系的判断，相应修改或增加边界条件
            ModifyOrInsert_For_NewReach(ref hydromodel, newreach, new_in_bd, new_out_hqbd);
        }

        //适合于新增的独立河道，上游入流为时间序列文件，下游水位流量关系
        public static void Add_NewReachBd(ref HydroModel hydromodel, ReachInfo newreach, string new_in_filename, string new_in_itemname, Dictionary<double, double> new_out_hq)
        {
            //定义新河道的上下游边界条件
            Reach_Bd new_in_bd;
            Reach_Bd new_out_hqbd;
            Define_NewReachBd(newreach, new_in_filename, new_in_itemname, new_out_hq, out new_in_bd, out new_out_hqbd);

            //通过新河道与老河道连接关系的判断，相应修改或增加边界条件
            ModifyOrInsert_For_NewReach(ref hydromodel, newreach, new_in_bd, new_out_hqbd);
        }

        //适合于新增的连接河道，上游与原河道连接，下游水位流量关系
        public static void Add_NewReachBd(ref HydroModel hydromodel, ReachInfo newreach, Dictionary<double, double> new_out_hq)
        {
            //定义新河道的上下游边界条件
            Reach_Bd new_in_bd;
            Reach_Bd new_out_hqbd;
            Define_NewReachBd(newreach, new_out_hq, out new_in_bd, out new_out_hqbd);

            //通过新河道与老河道连接关系的判断，相应修改或增加边界条件
            ModifyOrInsert_For_NewReach(ref hydromodel, newreach, new_in_bd, new_out_hqbd);
        }

        //适合于新增的连接河道，上游常量入流，下游与原河道连接
        public static void Add_NewReachBd(ref HydroModel hydromodel, ReachInfo newreach, double new_in_value)
        {
            //定义新河道的上下游边界条件
            Reach_Bd new_in_bd;
            Reach_Bd new_out_hqbd;
            Define_NewReachBd(newreach, new_in_value, out new_in_bd, out new_out_hqbd);

            //通过新河道与老河道连接关系的判断，相应修改或增加边界条件
            ModifyOrInsert_For_NewReach(ref hydromodel, newreach, new_in_bd, new_out_hqbd);
        }

        //适合于新增的连接河道，上游时间序列文件入流，下游与原河道连接
        public static void Add_NewReachBd(ref HydroModel hydromodel, ReachInfo newreach, string new_in_filename, string new_in_itemname)
        {
            //定义新河道的上下游边界条件
            Reach_Bd new_in_bd;
            Reach_Bd new_out_hqbd;
            Define_NewReachBd(newreach, new_in_filename, new_in_itemname, out new_in_bd, out new_out_hqbd);

            //通过新河道与老河道连接关系的判断，相应修改或增加边界条件
            ModifyOrInsert_For_NewReach(ref hydromodel, newreach, new_in_bd, new_out_hqbd);
        }


        //判断新河道的上下游是否与原河道连接，并相应修改或增加边界条件
        public static void ModifyOrInsert_For_NewReach(ref HydroModel hydromodel, ReachInfo newreach, Reach_Bd new_in_bd, Reach_Bd new_out_hqbd)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;

            //如果新河道 下、下边界不为默认边界(默认边界即是无边界，与其他河连)，则相应增加边界
            if (new_in_bd.BoundaryId != "")
            {
                boundarylist.Add_Boundary(new_in_bd);
            }

            if (new_out_hqbd.BoundaryId != "")
            {
                boundarylist.Add_Boundary(new_out_hqbd);
            }

            //获取边界文件中原上边界和下边界的集合
            List<Reach_Bd> upbd_list;
            List<Reach_Bd> downbd_list;
            Get_UpDwonBd_list(ref hydromodel, out upbd_list, out downbd_list);

            //判断新河道 上游是否与原河道相连
            for (int i = 0; i < downbd_list.Count; i++)
            {
                //如果河名和桩号均吻合,则直接删除原河道的下边界
                if (newreach.upstream_connect.reachname == downbd_list[i].Reachinfo.reachname &&
                    newreach.upstream_connect.chainage == downbd_list[i].Reachinfo.chainage)
                {
                    boundarylist.Remove_Boundary(downbd_list[i].BoundaryId);
                    break;
                }
            }

            //判断新河道 下游是否与原河道相连
            for (int i = 0; i < upbd_list.Count; i++)
            {
                //如果河名和桩号均吻合,则直接删除原河道的上边界
                if (newreach.downstream_connect.reachname == upbd_list[i].Reachinfo.reachname &&
                    newreach.downstream_connect.chainage == upbd_list[i].Reachinfo.chainage)
                {
                    boundarylist.Remove_Boundary(upbd_list[i].BoundaryId);
                    break;
                }
            }
        }

        //获取上下游边界集合
        public static void Get_UpDwonBd_list(ref HydroModel hydromodel, out List<Reach_Bd> upbd_list, out List<Reach_Bd> downbd_list)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;

            //先初始化
            upbd_list = new List<Reach_Bd>();
            downbd_list = new List<Reach_Bd>();

            //遍历各边界，找到入流上边界和出流下边界
            for (int i = 0; i < boundarylist.Boundary_infolist.Count; i++)
            {
                Reach_Bd reachboundary = boundarylist.Boundary_infolist[i];
                if (reachboundary.Bd_type == 1)  //入流上边界
                {
                    upbd_list.Add(reachboundary);
                }
                else if (reachboundary.Bd_type == 2 || reachboundary.Bd_type == 5)  //出流下边界
                {
                    downbd_list.Add(reachboundary);
                }
            }
        }


        //定义新河道的上下游边界条件对象 -- 独立河道 上游常量入流，下游水位流量关系
        public static void Define_NewReachBd(ReachInfo newreach, double new_in_value, Dictionary<double, double> new_out_hq, out Reach_Bd new_in_bd, out Reach_Bd new_out_hqbd)
        {
            //定义新河道的上边界条件
            AtReach in_reachinfo;
            in_reachinfo.reachname = newreach.reachname;
            in_reachinfo.chainage = newreach.start_chainage;
            in_reachinfo.reachid = newreach.reachtopoid;
            new_in_bd = new Reach_Bd(in_reachinfo, 0, 1, new_in_value);


            //定义新河道的下边界条件
            AtReach out_reachinfo;
            out_reachinfo.reachname = newreach.reachname;
            out_reachinfo.chainage = newreach.end_chainage;
            out_reachinfo.reachid = newreach.reachtopoid;
            new_out_hqbd = new Reach_Bd(out_reachinfo, new_out_hq);
        }

        //定义新河道的上下游边界条件对象 -- 独立河道 上游时间序列文件入流，下游水位流量关系
        public static void Define_NewReachBd(ReachInfo newreach, string new_in_filename, string new_in_itemname, Dictionary<double, double> new_out_hq, out Reach_Bd new_in_bd, out Reach_Bd new_out_hqbd)
        {
            //定义新河道的上边界条件
            AtReach in_reachinfo;
            in_reachinfo.reachname = newreach.reachname;
            in_reachinfo.chainage = newreach.start_chainage;
            in_reachinfo.reachid = newreach.reachtopoid;
            new_in_bd = new Reach_Bd(in_reachinfo, 0, 1, new_in_filename, new_in_itemname);


            //定义新河道的下边界条件
            AtReach out_reachinfo;
            out_reachinfo.reachname = newreach.reachname;
            out_reachinfo.chainage = newreach.end_chainage;
            out_reachinfo.reachid = newreach.reachtopoid;
            new_out_hqbd = new Reach_Bd(out_reachinfo, new_out_hq);
        }

        //定义新河道的上下游边界条件对象-- 上游与某河道连接，下游水位流量关系
        public static void Define_NewReachBd(ReachInfo newreach, Dictionary<double, double> new_out_hq, out Reach_Bd new_in_bd, out Reach_Bd new_out_hqbd)
        {
            //定义新河道的上边界条件
            new_in_bd = new Reach_Bd();

            //定义新河道的下边界条件
            AtReach out_reachinfo;
            out_reachinfo.reachname = newreach.reachname;
            out_reachinfo.chainage = newreach.end_chainage;
            out_reachinfo.reachid = newreach.reachtopoid;
            new_out_hqbd = new Reach_Bd(out_reachinfo, new_out_hq);
        }

        //定义新河道的上下游边界条件对象 -- 上游常量入流，下游与某河道连接
        public static void Define_NewReachBd(ReachInfo newreach, double new_in_value, out Reach_Bd new_in_bd, out Reach_Bd new_out_hqbd)
        {
            //定义新河道的上边界条件
            AtReach in_reachinfo;
            in_reachinfo.reachname = newreach.reachname;
            in_reachinfo.chainage = newreach.start_chainage;
            in_reachinfo.reachid = newreach.reachtopoid;
            new_in_bd = new Reach_Bd(in_reachinfo, 0, 1, new_in_value);

            //定义新河道的下边界条件
            new_out_hqbd = new Reach_Bd();
        }

        //定义新河道的上下游边界条件对象 -- 上游常量时间序列入流，下游与某河道连接
        public static void Define_NewReachBd(ReachInfo newreach, string new_in_filename, string new_in_itemname, out Reach_Bd new_in_bd, out Reach_Bd new_out_hqbd)
        {
            //定义新河道的上边界条件
            AtReach in_reachinfo;
            in_reachinfo.reachname = newreach.reachname;
            in_reachinfo.chainage = newreach.start_chainage;
            in_reachinfo.reachid = newreach.reachtopoid;
            new_in_bd = new Reach_Bd(in_reachinfo, 0, 1, new_in_filename, new_in_itemname);

            //定义新河道的下边界条件
            new_out_hqbd = new Reach_Bd();
        }
        #endregion ********************************************************************************************


        #region *********************** 边界操作 -- 为新产汇流流域增加旁侧入流边界条件***********************************
        //新增 文件类型 的旁侧入流  --  点源
        public static void Add_NewInflowBd(ref HydroModel hydromodel, AtReach reachinfo,
            string new_in_filename, string new_in_itemname, string bnd_id = "")
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //如果旁侧入流距离计算点过近，则相应调整
            double chainage = reachinfo.chainage;
            List<double> chainagelist = reachlist.Get_ReachGridChainage(reachinfo.reachname);
            Nwk11.ChangeNearChainage(chainagelist, ref chainage);
            reachinfo.chainage = chainage;

            //定义旁侧入流边界条件
            Reach_Bd new_in_bd = new Reach_Bd(reachinfo, 1, 0, new_in_filename, new_in_itemname,bnd_id);  //注意点源的inflow边界type是0，分布源是2

            //向边界集合中添加新旁侧入流边界
            boundarylist.Add_Boundary(new_in_bd);

            //同步更新河道计算点桩号
            reachlist.Add_ExistReach_GridChainage(reachinfo.reachname, reachinfo.chainage);
        }

        //新增 文件类型 的旁侧入流  --  分布源
        public static void Add_NewInflowBd(ref HydroModel hydromodel, Reach_Segment reachinfo, string new_in_filename, string new_in_itemname)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;

            //定义旁侧入流边界条件
            Reach_Bd new_in_bd = new Reach_Bd(reachinfo, 2, 2, new_in_filename, new_in_itemname);

            //向边界集合中添加新旁侧入流边界
            boundarylist.Add_Boundary(new_in_bd);
        }

        //新增 常量 旁侧入流  --  点源
        public static void Add_NewInflowBd(ref HydroModel hydromodel, AtReach reachinfo, double new_in_value)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //如果旁侧入流距离计算点过近，则相应调整
            double chainage = reachinfo.chainage;
            List<double> chainagelist = reachlist.Get_ReachGridChainage(reachinfo.reachname);
            Nwk11.ChangeNearChainage(chainagelist, ref chainage);
            reachinfo.chainage = chainage;

            //定义旁侧入流边界条件
            Reach_Bd new_in_bd = new Reach_Bd(reachinfo, 1, 0, new_in_value);

            //向边界集合中添加新旁侧入流边界
            boundarylist.Add_Boundary(new_in_bd);

            //同步更新河道计算点桩号
            reachlist.Add_ExistReach_GridChainage(reachinfo.reachname, reachinfo.chainage);
        }

        //新增 常量 旁侧入流  --  分布源
        public static void Add_NewInflowBd(ref HydroModel hydromodel, Reach_Segment reachinfo, double new_in_value)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;

            //定义旁侧入流边界条件
            Reach_Bd new_in_bd = new Reach_Bd(reachinfo, 2, 2, new_in_value);

            //向边界集合中添加新旁侧入流边界
            boundarylist.Add_Boundary(new_in_bd);
        }
        #endregion ******************************************************************************************************


        #region *********************** 边界操作 -- 为新污染源增加点源边界条件***********************************
        //新增 随时间变化 的旁侧入流污染源  --  污染点源(HD+AD)
        public static Reach_Bd Add_Adhdbd(ref HydroModel hydromodel, string jdstr, string wdstr, double volumn, string starttimestr, string endtimestr, int sourcetype, string bd_desc)
        {
            //转换前端时间
            DateTime starttime = SimulateTime.StrToTime(starttimestr);
            DateTime endtime = SimulateTime.StrToTime(endtimestr);
            SimulateTime time; time.Begin = starttime; time.End = endtime;

            //转换前端经纬度到大地坐标，并找到所在附近河道和桩号
            PointXY p = PointXY.Get_Pointxy(double.Parse(jdstr), double.Parse(wdstr));
            if (p.X < 200)
            {
                p = PointXY.CoordTranfrom(p, 4326, 4547, 3);
            }
            AtReach atreach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //dfs0文件路径
            string hd_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + atreach.reachname + atreach.chainage + "_newrl_q.dfs0";
            string ad_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + atreach.reachname + atreach.chainage + "_newrl_con.dfs0";

            //创建入流过程的dfs0文件(HD流量过程)
            Dictionary<DateTime, double> discharge_dic = Dfs0.CreateDic_FromAccumulatedValue(hydromodel.ModelGlobalPars.Simulate_time, starttime, endtime, volumn);
            Dfs0.Dfs0_Creat(hd_filename, discharge_dic, dfs0type.discharge);
            string hd_itemname = "discharge";

            //创建入流过程的dfs0文件(AD浓度过程)
            Dictionary<DateTime, double> concentration_dic = Dfs0.CreateDic_FromInstantValue(hydromodel.ModelGlobalPars.Simulate_time, starttime, endtime, Model_Const.CONCENTRATION);
            Dfs0.Dfs0_Creat(ad_filename, concentration_dic, dfs0type.concentration);
            string ad_itemname = "concentration";

            AdSourceType source_type = (AdSourceType)sourcetype;

            //加入边界集合
            Reach_Bd new_bd = Add_NewInflow_AdhdBd(ref hydromodel, atreach, hd_filename, hd_itemname, ad_filename, ad_itemname, source_type, bd_desc);

            return new_bd;
        }

        //新增 文件类型 的旁侧入流  --  水质和水动力学点源
        public static Reach_Bd Add_NewInflow_AdhdBd(ref HydroModel hydromodel, AtReach reachinfo, string hd_filename, string hd_itemname,
            string ad_filename, string ad_itemname, AdSourceType sourcetype, string bd_desc)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //如果旁侧入流距离计算点过近，则相应调整
            double chainage = reachinfo.chainage;
            List<double> chainagelist = reachlist.Get_ReachGridChainage(reachinfo.reachname);
            Nwk11.ChangeNearChainage(chainagelist, ref chainage);
            reachinfo.chainage = chainage;

            //定义旁侧入流边界条件
            Reach_Bd new_in_bd = new Reach_Bd(reachinfo, 1, 0, hd_filename, hd_itemname, ad_filename, ad_itemname, sourcetype, bd_desc);  //注意点源的inflow边界type是0，分布源是2

            //向边界集合中添加新旁侧入流边界
            boundarylist.Add_Boundary(new_in_bd);

            //同步更新河道计算点桩号
            reachlist.Add_ExistReach_GridChainage(reachinfo.reachname, reachinfo.chainage);

            return new_in_bd;
        }

        //根据经纬度位置删除边界条件
        public static void Del_Adbd_Byjwd(ref HydroModel hydromodel, string jdstr, string wdstr)
        {
            //转换前端经纬度到大地坐标，并找到所在附近河道和桩号,如果该位置有点源边界条件，则删除
            PointXY p = PointXY.Get_Pointxy(double.Parse(jdstr), double.Parse(wdstr));
            if (p.X < 200)
            {
                double centra = PointXY.Get_Centra(hydromodel.ModelGlobalPars.Coordinate_type);
                p = PointXY.CoordTranfrom(p, 4326, 4547, 3);
            }
            AtReach atreach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            List<Reach_Bd> boundarylist = hydromodel.Mike11Pars.BoundaryList.Boundary_infolist;
            for (int i = 0; i < boundarylist.Count; i++)
            {
                if (boundarylist[i].Reachinfo.reachname == atreach.reachname &&
                    boundarylist[i].Reachinfo.chainage == atreach.chainage)
                {
                    hydromodel.Mike11Pars.BoundaryList.Remove_Boundary(boundarylist[i].BoundaryId);
                }
            }
        }

        //删除所有污染点源边界
        public static void Del_All_Adbd(ref HydroModel hydromodel)
        {
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;
            for (int i = 0; i < boundarylist.Boundary_infolist.Count; i++)
            {
                Reach_Bd boundary = boundarylist.Boundary_infolist[i];
                if (boundary.Bd_description == 1 && boundary.Bd_type == 0 && boundary.IncludeAD)
                {
                    hydromodel.Mike11Pars.BoundaryList.Remove_Boundary(boundary.BoundaryId);
                }
            }
        }

        //获取模型的所有污染点源信息
        public static List<string[]> Get_ADsourceInfo_list(ref HydroModel hydromodel)
        {
            List<string[]> adsourcelist = new List<string[]>();
            //搜索所有污染点源边界，不含其他点源边界
            BoundaryList boundarylist = hydromodel.Mike11Pars.BoundaryList;
            for (int i = 0; i < boundarylist.Boundary_infolist.Count; i++)
            {
                Reach_Bd boundary = boundarylist.Boundary_infolist[i];
                if (boundary.Bd_description == 1 && boundary.Bd_type == 0 && boundary.IncludeAD)
                {
                    string[] sourceinfo = Get_AdsourceInfo(ref hydromodel, boundary.BoundaryId);
                    adsourcelist.Add(sourceinfo);
                }
            }
            return adsourcelist;
        }

        //获取特定格式的污染点源信息
        private static string[] Get_AdsourceInfo(ref HydroModel hydromodel, string boundaryid)
        {
            Reach_Bd boundary = hydromodel.Mike11Pars.BoundaryList.Get_ReachBd(boundaryid);
            string[] strlist = new string[6];
            string sourcetype = boundary.Adsourcetype == AdSourceType.Solubility ? "可溶性液体" : "不可溶性液体";
            strlist[0] = sourcetype;

            //数据转桩号字符串
            double zhs = boundary.Reachinfo.chainage;
            string zh1 = Math.Truncate((zhs / 1000.0)).ToString();
            string zh2 = (zhs - double.Parse(zh1) * 1000.0).ToString("000.0");
            string zh_str = zh1 + "+" + zh2;

            strlist[1] = zh_str;
            strlist[2] = Math.Round((Dfs0.Get_Accumulated_Value(boundary.Bd_filename) * 1000.0), 1).ToString(); //转为升

            DateTime start_time;
            DateTime end_time;
            Dfs0.Get_StartEndTime(boundary.Ad_filename[0], out start_time, out end_time);
            strlist[3] = start_time.ToString(Model_Const.TIMEFORMAT);
            strlist[4] = end_time.ToString(Model_Const.TIMEFORMAT);
            strlist[5] = boundary.BoundaryId;
            return strlist;
        }
        #endregion ******************************************************************************************************
    }


    //保存最新所有边界的类
    [Serializable]
    public class BoundaryList
    {
        //**********************属性************************
        //现有边界信息集合
        public List<Reach_Bd> Boundary_infolist
        { get; set; }

        //**********************方法************************
        //增加新边界方法
        public void Add_Boundary(Reach_Bd newReach_Bd)
        {
            if (!Boundary_infolist.Contains(newReach_Bd) && Have_TheSameBd(newReach_Bd) == false)
            {
                //往集合中增加新产汇流流域
                Boundary_infolist.Add(newReach_Bd);
            }
        }

        //减少边界方法
        public void Remove_Boundary(string boundaryid)
        {
            for (int i = 0; i < Boundary_infolist.Count; i++)
            {
                if (Boundary_infolist[i].BoundaryId == boundaryid)
                {
                    Boundary_infolist.RemoveAt(i);
                    break;
                }
            }
        }

        //根据边界ID获取该边界方法
        public Reach_Bd Get_ReachBd(string boundaryid)
        {
            for (int i = 0; i < Boundary_infolist.Count; i++)
            {
                if (String.Equals(Boundary_infolist[i].BoundaryId, boundaryid, StringComparison.OrdinalIgnoreCase))
                {
                    return Boundary_infolist[i];
                }
            }

            Console.WriteLine("未找到指定边界!");
            return null;
        }

        //根据所做河道信息获取边界方法
        public Reach_Bd Get_ReachBd(AtReach atreach)
        {
            //先定义一个默认的边界对象
            Reach_Bd reachbd = new Reach_Bd();

            for (int i = 0; i < Boundary_infolist.Count; i++)
            {
                //判断边界条件的河名、桩号是否与给的新边界条件相同
                if (Boundary_infolist[i].Reachinfo.reachname == atreach.reachname &&
                    Boundary_infolist[i].Reachinfo.chainage == atreach.chainage)
                {
                    return Boundary_infolist[i];
                }
            }

            Console.WriteLine("未找到指定边界!");
            return null;
        }

        //判断边界集合中是否有 各属性均与给定边界相同的
        public bool Have_TheSameBd(Reach_Bd reachbd)
        {
            for (int i = 0; i < Boundary_infolist.Count; i++)
            {
                Reach_Bd reach_bd = Boundary_infolist[i];
                if (reach_bd.Bd_description == reachbd.Bd_description &&
                    reach_bd.Bd_type == reachbd.Bd_type &&
                reach_bd.BoundaryId == reachbd.BoundaryId &&
                reach_bd.Reachinfo.reachname == reachbd.Reachinfo.reachname &&
                reach_bd.Reachinfo.chainage == reachbd.Reachinfo.chainage &&
                reach_bd.Endchainage == reachbd.Endchainage
                    )
                {
                    return true;
                }

            }
            return false;
        }

        //获取时间序列类 边界条件的时间序列数据过程
        public Dictionary<DateTime, double> Get_TimeBnd_DataGC(string bnd_id)
        {
            Dictionary<DateTime, double> discharge_dic = new Dictionary<DateTime, double>();

            //获取边界条件
            Reach_Bd reach_bd = Get_ReachBd(bnd_id);
            if (reach_bd == null) return null;
            if (reach_bd.Valuetype != BdValueType.TS_File) return null;

            //读取入流过程的dfs0文件
            string dfs0_filename = reach_bd.Bd_filename;
            if (!File.Exists(dfs0_filename)) return null;

            discharge_dic = Dfs0.Dfs0_Reader_GetItemDic(dfs0_filename, 1);

            return discharge_dic;
        }
    }

    //河道边界类 -- 对应河道的一个边界
    [Serializable]
    public class Reach_Bd
    {
        #region ***************************构造函数*****************************
        //无参构造函数
        public Reach_Bd()
        {
            Set_Default();
        }

        //构建常量入流上边界
        public Reach_Bd(AtReach reach, double value)
        {
            Set_Default();
            this.Reachinfo = reach;
            this.BoundaryId = reach.reachname + "_IN";

            this.Valuetype = BdValueType.Constant;
            this.Bd_value = value;
        }

        //构建时间序列文件入流上边界
        public Reach_Bd(AtReach reach, string filename, string itemname)
        {
            Set_Default();
            this.Reachinfo = reach;
            this.BoundaryId = reach.reachname + "_IN";

            this.Valuetype = BdValueType.TS_File;
            this.Bd_filename = filename;
            this.Bd_itemname = itemname;
        }


        //构建边界值为常量的边界条件,包括水位、流量，入流或出流
        public Reach_Bd(AtReach reach, int description, int bdtype, double value)
        {
            Set_Default();
            this.Bd_description = description;
            this.Bd_type = bdtype;
            this.Reachinfo = reach;
            if (bdtype == 1)   //边界类型为入流
            {
                this.BoundaryId = reach.reachname + "_IN";
            }
            else              //边界类型为水位或水位流量关系
            {
                this.BoundaryId = reach.reachname + "_OUT";
            }

            this.Valuetype = BdValueType.Constant;
            this.Bd_value = value;
        }

        //构建边界值为时间序列文件的边界条件,包括水位、流量，入流或出流,旁侧点源入流或上下游边界
        public Reach_Bd(AtReach reach, int description, int bdtype, string filename, string itemname, string bnd_id = "")
        {
            Set_Default();
            this.Bd_description = description;
            this.Bd_type = bdtype;
            this.Reachinfo = reach;

            if (bnd_id == "")
            {
                if (bdtype == 1)   //边界类型为入流
                {
                    this.BoundaryId = reach.reachname + "_IN";
                }
                else              //边界类型为水位或水位流量关系
                {
                    this.BoundaryId = reach.reachname + "_OUT";
                }
            }
            else
            {
                this.BoundaryId = bnd_id;
            }

            this.Valuetype = BdValueType.TS_File;
            this.Bd_filename = filename;
            this.Bd_itemname = itemname;
        }

        //构建水质和水动力学边界条件，其边界值均为时间序列,主要用于入流的污染点源,只包含一个组分
        public Reach_Bd(AtReach reach, int description, int bdtype, string hd_filename, string hd_itemname,
            string ad_filename, string ad_itemname, AdSourceType sourcetype, string bd_desc)
        {
            Set_Default();
            this.Bd_description = description;
            this.Bd_type = bdtype;
            this.Reachinfo = reach;
            this.Adsourcetype = sourcetype;
            this.BoundaryId = bd_desc;

            this.Valuetype = BdValueType.TS_File;
            this.Bd_filename = hd_filename;
            this.Bd_itemname = hd_itemname;

            this.IncludeAD = true;
            List<BdValueType> advaluetype_list = new List<BdValueType>();
            advaluetype_list.Add(BdValueType.TS_File);
            this.Ad_valuetype = advaluetype_list;

            List<string> ad_filelist = new List<string>();
            ad_filelist.Add(ad_filename);
            this.Ad_filename = ad_filelist;

            List<string> ad_itemlist = new List<string>();
            ad_itemlist.Add(ad_itemname);
            this.Ad_itemname = ad_itemlist;

            List<int> ad_comnumber = new List<int>();
            ad_comnumber.Add(1);
            this.AD_comnumber = ad_comnumber;

            List<int> ad_bdtype = new List<int>();
            ad_bdtype.Add(0);
            this.AD_bdtype = ad_bdtype;
        }

        //构建边界值为常量的的旁侧分布入流边界
        public Reach_Bd(Reach_Segment reach, int description, int bdtype, double value)
        {
            Set_Default();
            this.Bd_description = description;
            this.Bd_type = bdtype;

            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.reachid = reach.reachtopoid;
            atreach.chainage = reach.start_chainage;
            this.Reachinfo = atreach;
            this.Endchainage = reach.end_chainage;

            if (bdtype == 1)   //边界类型为入流
            {
                this.BoundaryId = reach.reachname + "_IN";
            }
            else              //边界类型为水位或水位流量关系
            {
                this.BoundaryId = reach.reachname + "_OUT";
            }

            this.Valuetype = BdValueType.Constant;
            this.Bd_value = value;
        }

        //构建边界值为时间序列文件的旁侧分布入流边界
        public Reach_Bd(Reach_Segment reach, int description, int bdtype, string filename, string itemname)
        {
            Set_Default();
            this.Bd_description = description;
            this.Bd_type = bdtype;
            AtReach atreach;
            atreach.reachname = reach.reachname;
            atreach.reachid = reach.reachtopoid;
            atreach.chainage = reach.start_chainage;
            this.Reachinfo = atreach;
            this.Endchainage = reach.end_chainage;

            if (bdtype == 1)   //边界类型为入流
            {
                this.BoundaryId = reach.reachname + "_IN";
            }
            else              //边界类型为水位或水位流量关系
            {
                this.BoundaryId = reach.reachname + "_OUT";
            }

            this.Valuetype = BdValueType.TS_File;
            this.Bd_filename = filename;
            this.Bd_itemname = itemname;
        }


        //构建边界值为水位流量关系的下游边界
        public Reach_Bd(AtReach reach, Dictionary<double, double> hq_dic)
        {
            Set_Default();
            this.Bd_description = 0;
            this.Bd_type = 2;
            this.Reachinfo = reach;

            this.BoundaryId = reach.reachname + "_OUT";
            this.HQ = hq_dic;
        }
        #endregion *****************************************************************

        #region ****************************属性****************************
        //边界条件定义
        public int Bd_description    //边界描述，开边界、闭边界、点源等
        { get; set; }

        public int Bd_type           //边界类型，入流、水位流量关系、水位等
        { get; set; }

        public AtReach Reachinfo    //边界的河道信息，包括河名、桩号
        { get; set; }

        public double Endchainage    //边界的结束桩号
        { get; set; }

        public string BoundaryId    //边界名称
        { get; set; }

        //是否包含AD模块
        public bool IncludeAD
        { get; set; }

        //污染源类型
        public AdSourceType Adsourcetype
        { get; set; }

        //水动力边界条件取值
        public BdValueType Valuetype    //边界条件值的类型
        { get; set; }
        public double Bd_value          //边界条件值
        { get; set; }
        public string Bd_filename       //边界条件文件
        { get; set; }
        public string Bd_itemname       //边界条件文件项目
        { get; set; }
        public Dictionary<double, double> HQ  //水位流量关系键值对集合，其中水位是键
        { get; set; }

        //水质边界条件取值
        public List<BdValueType> Ad_valuetype
        { get; set; }
        public List<int> AD_comnumber     //组分编号
        { get; set; }
        public List<int> AD_bdtype     //水质边界类型
        { get; set; }
        public List<double> Ad_value
        { get; set; }
        public List<string> Ad_filename
        { get; set; }
        public List<string> Ad_itemname
        { get; set; }
        #endregion **********************************************************

        #region *********************************方法********************************
        public void Set_Default()
        {
            this.Bd_description = 0;
            this.Bd_type = 1;
            AtReach reachinfo;
            reachinfo.reachname = "";
            reachinfo.reachid = "";
            reachinfo.chainage = 0;
            this.Endchainage = 0;
            this.Reachinfo = reachinfo;
            this.BoundaryId = "";

            this.IncludeAD = false;
            this.Adsourcetype = AdSourceType.Solubility;

            this.Valuetype = BdValueType.Constant;
            this.Bd_value = 0;
            this.Bd_filename = "";
            this.Bd_itemname = "";
            this.HQ = null;

            this.Ad_valuetype = new List<BdValueType>();
            this.AD_comnumber = new List<int>();
            this.AD_bdtype = new List<int>();
            this.Ad_value = new List<double>();
            this.Ad_filename = new List<string>();
            this.Ad_itemname = new List<string>();
        }

        //判断两边界是否各主要属性均相同
        public bool Is_TheSame(Reach_Bd reachbd)
        {
            if (this.Bd_description == reachbd.Bd_description &&
                this.Bd_type == reachbd.Bd_type &&
            this.BoundaryId == reachbd.BoundaryId &&
            this.Reachinfo.reachname == reachbd.Reachinfo.reachname &&
            this.Reachinfo.chainage == reachbd.Reachinfo.chainage &&
            this.Endchainage == reachbd.Endchainage &&
            this.Bd_value == reachbd.Bd_value
                )
            {
                return true;
            }

            return false;
        }

        //根据不同的边界条件描述得到边界条件类型编号
        public static int Get_InflowBndType(int Bd_description)
        {
            int bd_type = 1;
            if (Bd_description == 0)
            {
                bd_type = 1;
            }
            else if (Bd_description == 1)
            {
                bd_type = 0;
            }
            else if (Bd_description == 2)
            {
                bd_type = 2;
            }

            return bd_type;
        }
        #endregion *******************************************************************
    }





}