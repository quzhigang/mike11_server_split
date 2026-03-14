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
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bjd_model.Mike11
{
    public partial class Nwk11
    {
        #region ***********从默认nwk11河网文件中获取河道、建筑物、计算点信息***********************
        //从河网文件中获取河道详细信息和基本信息
        public static void GetDefault_ReachInfo(string sourcefilename, ref ReachList Reachlist)
        {
            //新建PFSfile类对象，打开文件
            PFSFile pfsfile = new PFSFile(sourcefilename);

            //新建目标对象，找到指定的目标和指定的大节进行修改
            PFSSection target = pfsfile.GetTarget("MIKE_11_Network_editor", 1);

            //点节
            PFSSection pointsec = target.GetSection("POINTS", 1);
            List<ReachPoint> reachpointlist = new List<ReachPoint>();

            //河道最大控制点编号
            int maxnumber = pointsec.GetKeyword(pointsec.GetKeywordsCount()).GetParameter(1).ToInt();
            Reachlist.Maxpointnumber = maxnumber;

            for (int i = 0; i < pointsec.GetKeywordsCount(); i++)
            {
                ReachPoint reachpoint;
                PFSKeyword key = pointsec.GetKeyword(i + 1);

                reachpoint.number = key.GetParameter(1).ToInt();
                reachpoint.X = key.GetParameter(2).ToDouble();
                reachpoint.Y = key.GetParameter(3).ToDouble();
                reachpoint.chainagetype = key.GetParameter(4).ToInt();
                reachpoint.pointchainage = key.GetParameter(5).ToDouble();

                reachpointlist.Add(reachpoint);
            }

            //河道的节位置
            PFSSection reachsec = target.GetSection("BRANCHES", 1);

            List<ReachInfo> reachinfolist = new List<ReachInfo>();

            //遍历所有河道，获取河道信息
            for (int i = 0; i < reachsec.GetSectionsCount(); i++)
            {
                ReachInfo reachinfo;
                PFSSection subsec = reachsec.GetSection("branch", i + 1);

                //基本信息
                PFSKeyword key = subsec.GetKeyword("definitions");
                reachinfo.reachname = key.GetParameter(1).ToString();
                reachinfo.reachtopoid = key.GetParameter(2).ToString();
                reachinfo.start_chainage = key.GetParameter(3).ToDouble();
                reachinfo.end_chainage = key.GetParameter(4).ToDouble();
                reachinfo.dx = key.GetParameter(6).ToDouble();

                //上下游连接河道信息
                PFSKeyword key1 = subsec.GetKeyword("connections");
                AtReach upreach;
                upreach.reachname = key1.GetParameter(1).ToString();
                upreach.chainage = key1.GetParameter(2).ToDouble();
                upreach.reachid = upreach.reachname + Model_Const.REACHID_HZ;
                AtReach downreach;
                downreach.reachname = key1.GetParameter(3).ToString();
                downreach.chainage = key1.GetParameter(4).ToDouble();
                downreach.reachid = downreach.reachname + Model_Const.REACHID_HZ;
                reachinfo.upstream_connect = upreach;
                reachinfo.downstream_connect = downreach;

                //河道点的组成信息
                PFSKeyword key2 = subsec.GetKeyword("points");
                List<int> reachpointnumber = new List<int>();
                for (int j = 0; j < key2.GetParametersCount(); j++)
                {
                    reachpointnumber.Add(key2.GetParameter(j + 1).ToInt());
                }

                //判断所有控制点，找出属于河道的控制点
                List<ReachPoint> reachpoint_list = new List<ReachPoint>();
                for (int j = 0; j < reachpointnumber.Count; j++)
                {
                    for (int k = 0; k < reachpointlist.Count; k++)
                    {
                        if (reachpointnumber[j] == reachpointlist[k].number)
                        {
                            reachpoint_list.Add(reachpointlist[k]);
                            break;
                        }
                    }
                }
                reachinfo.reachpoint_list = reachpoint_list;

                reachinfolist.Add(reachinfo);
            }

            pfsfile.Close();

            //河道详细信息初始化
            Reachlist.Reach_infolist = reachinfolist;

            //河道基本信息初始化
            List<Reach_Segment> baseinfolist = new List<Reach_Segment>();
            for (int i = 0; i < reachinfolist.Count; i++)
            {
                Reach_Segment reachbaseinfo;
                reachbaseinfo.reachname = reachinfolist[i].reachname;
                reachbaseinfo.reachtopoid = reachinfolist[i].reachtopoid;
                reachbaseinfo.start_chainage = reachinfolist[i].start_chainage;
                reachbaseinfo.end_chainage = reachinfolist[i].end_chainage;
                baseinfolist.Add(reachbaseinfo);
            }
            Reachlist.Reach_baseinfolist = baseinfolist;

            Console.WriteLine("河道基本信息初始化成功!");
        }

        //从河网文件中获取已有建筑物详细信息
        public static void GetDefault_GateInfo(string sourcefilename, ref ControlList Controlstrlist)
        {
            //新建PFSfile类对象，打开文件
            PFSFile pfsfile = new PFSFile(sourcefilename);

            //新建目标对象，找到指定的目标和指定的大节进行修改
            PFSSection target = pfsfile.GetTarget("MIKE_11_Network_editor", 1);

            //可控建筑物的节
            PFSSection CONTROL_STR = target.GetSection("STRUCTURE_MODULE", 1).GetSection("CONTROL_STR", 1);

            //获取已有的建筑物详细信息
            //注意这里默认建筑集合和总建筑集合虽然相同，但必须新建两个集合，如果都指向一个集合，则当默认建筑集合减少元素了，总建筑集合也会跟着减少
            List<Controlstr> GateList = new List<Controlstr>();
            List<Controlstr> DefaultGateList = new List<Controlstr>();
            Get_Gatelist(CONTROL_STR,ref GateList,ref DefaultGateList);

            //默认闸门信息初始化
            Controlstrlist.Default_GateList = DefaultGateList;
            List<string> gatenamelist = new List<string>();
            for (int i = 0; i < GateList.Count; i++)
            {
                gatenamelist.Add(GateList[i].Strname);
            }
            Controlstrlist.Default_GateNameList = gatenamelist;

            //闸门基本信息初始化
            Dictionary<string, int> Gatebaseinfo = new Dictionary<string, int>();
            for (int i = 0; i < GateList.Count; i++)
            {
                Gatebaseinfo.Add(GateList[i].Strname, GateList[i].Stratts.gate_count);
            }
            Controlstrlist.Gatebaseinfo = Gatebaseinfo;

            //创建新增建筑物集合
            List<Controlstr> newgatelist = new List<Controlstr>();
            Controlstrlist.NewAdd_GateList = newgatelist;

            //所有建筑物集合
            Controlstrlist.GateListInfo = GateList;

            pfsfile.Close();

            Console.WriteLine("河道建筑物信息初始化成功!");
        }

        //获取已有的建筑物详细信息
        private static void Get_Gatelist(PFSSection CONTROL_STR,ref List<Controlstr> GateList,ref List<Controlstr> DefaultGateList)
        {
            string now_strname = "";
            for (int i = 0; i < CONTROL_STR.GetSectionsCount(); i++)
            {
                PFSSection control_str_data = CONTROL_STR.GetSection("control_str_data", i + 1);  //注意，各大节小节均从1开始

                //获取所在河道信息
                PFSKeyword Location = control_str_data.GetKeyword("Location");
                AtReach Reachinfo;
                Reachinfo.reachname = Location.GetParameter(1).ToString();
                Reachinfo.reachid = Reachinfo.reachname + Model_Const.REACHID_HZ;
                Reachinfo.chainage = Location.GetParameter(2).ToDouble();
                string strID = Location.GetParameter(3).ToString();  //建筑物ID，即建筑物名+调度规则，注意用'_'隔开
                string[] strarray = strID.Split('_');
                string strname = strarray[0] + "_" + strarray[1]; //用前2个字符串组成建筑物名
                PFSKeyword Attributes = control_str_data.GetKeyword("Attributes");

                //如果闸门类型不是"discharge"，则提取闸门信息
                if (strname != now_strname)
                {
                    //闸门名称重新赋值(防止多次提取同一闸门)
                    now_strname = strname;

                    //闸门放置位置
                    Str_SideType strsidetype = (Str_SideType)control_str_data.GetSection("ReservoirData", 1).GetKeyword("StructureType").GetParameter(1).ToInt();

                    //获取闸门主要属性参数
                    Attributes stratts;
                    Headloss Strheadloss;
                    RadiaParams Strradiapars;
                    FormulaParams Strformulapars;
                    GetGate_Attrs(control_str_data,Attributes, strname, out stratts, out Strheadloss, out Strradiapars, out Strformulapars);

                    //获取调度逻辑里的value值
                    PFSSection logical_sec = control_str_data.GetSection("logical_statement", 1); //提取第一个逻辑判断节
                    PFSKeyword ls_parameterkey = logical_sec.GetKeyword("LS_parameter");
                    double double_value = ls_parameterkey.GetParameter(5).ToDouble();
                    int cal_mode = ls_parameterkey.GetParameter(2).ToInt();  //全开、全关等

                    //创建可控建筑物对象
                    if (!strname.Contains("FHK"))  //建筑物名字中用FHK的按分洪口构建，否则按普通建筑物构建
                    {
                        Normalstr normalstr = new Normalstr(strname, Reachinfo, stratts);
                        normalstr.Str_Sidetype = strsidetype;
                        normalstr.Strheadloss = Strheadloss;
                        normalstr.Strradiapars = Strradiapars;
                        normalstr.Strformulapars = Strformulapars;
                        normalstr.Ddparams_double = double_value;  //获取value值

                        //修改闸门调度
                        ZMDUinfo zmdu_info = normalstr.Ddparams_zmdu;
                        if (cal_mode == 4)  //全开
                        {
                            zmdu_info.fullyopen = true;
                            zmdu_info.opengatenumber = stratts.gate_count;
                            zmdu_info.opengateheight = stratts.max_value;
                        }
                        else if (cal_mode == 5)  //全关
                        {
                            zmdu_info.fullyopen = false;
                            zmdu_info.opengatenumber = 0;
                            zmdu_info.opengateheight = stratts.sill_level;
                        }
                        else
                        {
                            zmdu_info.fullyopen = false;
                            zmdu_info.opengatenumber = stratts.gate_count;
                            zmdu_info.opengateheight = double_value;
                        }
                        normalstr.Ddparams_zmdu = zmdu_info;

                        //加入集合
                        GateList.Add(normalstr);
                        DefaultGateList.Add(normalstr);
                    }
                    else
                    {
                        FhkstrInfo fhkstrinfo;
                        fhkstrinfo.atreach = Reachinfo;
                        fhkstrinfo.break_seconds = Math.Abs(stratts.initial_value - stratts.sill_level) / stratts.max_speed;
                        fhkstrinfo.dd_level = stratts.max_value;
                        fhkstrinfo.fhk_datumn = stratts.sill_level;
                        fhkstrinfo.dm_level = stratts.sill_level;
                        fhkstrinfo.strddgz = CtrDdtype.GZDU;
                        fhkstrinfo.strname = strname;
                        fhkstrinfo.time_filename = "";
                        fhkstrinfo.time_itemname = "";
                        fhkstrinfo.fh_waterlevel = stratts.max_value;
                        fhkstrinfo.width = stratts.gate_width;

                        Fhkstr fhkstr = new Fhkstr(fhkstrinfo);
                        fhkstr.Strheadloss = Strheadloss;
                        fhkstr.Strradiapars = Strradiapars;
                        fhkstr.Strformulapars = Strformulapars;

                        //加入集合
                        GateList.Add(fhkstr);
                        DefaultGateList.Add(fhkstr);
                    }

                }
            }
        }

        //获取闸门主要属性参数
        private static void GetGate_Attrs(PFSSection control_str_data, PFSKeyword Attributes,string strname,out Attributes stratts, out Headloss Strheadloss, out RadiaParams Strradiapars, out FormulaParams Strformulapars)
        {
            if (Attributes.GetParameter(1).ToInt() != 2) //非流量类型闸门
            {
                //获取闸门基本属性
                stratts.gate_type = Attributes.GetParameter(1).ToInt();
                stratts.gate_count = Attributes.GetParameter(2).ToInt();
                stratts.under_flowcc = Attributes.GetParameter(3).ToDouble();
                stratts.gate_width = Attributes.GetParameter(4).ToDouble();
                stratts.sill_level = Attributes.GetParameter(5).ToDouble();
                stratts.max_speed = Attributes.GetParameter(6).ToDouble();
                stratts.bool_initial_value = Attributes.GetParameter(7).ToBoolean();
                stratts.initial_value = Attributes.GetParameter(8).ToDouble();
                stratts.bool_max_value = Attributes.GetParameter(9).ToBoolean();
                stratts.max_value = Attributes.GetParameter(10).ToDouble();
                stratts.gate_height = Attributes.GetParameter(11).ToDouble();
            }
            else  // 若是流量类型闸门，从数据库获取闸门基本属性，并采用闸门类型
            {
                //从获取该闸门属性
                Struct_BasePars gate_str = Item_Info.Get_StrBaseInfo(strname);
                if(gate_str.gate_type == GateType.BZ || gate_str.gate_type == GateType.LLZ)
                {
                    stratts.gate_type = 2;
                }
                else
                {
                    stratts.gate_type = 4;
                }
                stratts.gate_count = gate_str.gate_n;
                stratts.under_flowcc = Attributes.GetParameter(3).ToDouble();
                stratts.gate_width = gate_str.gate_b;
                stratts.sill_level = gate_str.datumn;
                stratts.max_speed = Model_Const.FLATGATE_MAXSPEED;
                stratts.bool_initial_value = true;
                stratts.initial_value = gate_str.datumn;
                stratts.bool_max_value = true;
                stratts.max_value = gate_str.datumn + gate_str.gate_h;
                stratts.gate_height = gate_str.gate_height;
            }
            
            //获取闸门水头损失系数因子
            PFSKeyword HeadLossFactors = control_str_data.GetKeyword("HeadLossFactors");
            Strheadloss.positive_inflow = HeadLossFactors.GetParameter(1).ToDouble();
            Strheadloss.positive_outflow = HeadLossFactors.GetParameter(2).ToDouble();
            Strheadloss.positive_freeoverflow = HeadLossFactors.GetParameter(3).ToDouble();
            Strheadloss.negative_inflow = HeadLossFactors.GetParameter(4).ToDouble();
            Strheadloss.negative_outflow = HeadLossFactors.GetParameter(5).ToDouble();
            Strheadloss.negative_freeoverflow = HeadLossFactors.GetParameter(6).ToDouble();

            //获取弧形闸门参数
            PFSKeyword RadialGateParam = control_str_data.GetKeyword("RadialGateParam");
            Strradiapars.tune_factor = RadialGateParam.GetParameter(1).ToDouble();
            Strradiapars.height = RadialGateParam.GetParameter(2).ToDouble();
            Strradiapars.radius = RadialGateParam.GetParameter(3).ToDouble();
            Strradiapars.trunnion = RadialGateParam.GetParameter(4).ToDouble();
            Strradiapars.weir_coeff = RadialGateParam.GetParameter(5).ToDouble();
            Strradiapars.weri_exp = RadialGateParam.GetParameter(6).ToDouble();
            Strradiapars.tran_bottom = RadialGateParam.GetParameter(7).ToDouble();
            Strradiapars.tran_depth = RadialGateParam.GetParameter(8).ToDouble();

            //获取公式闸门参数
            PFSKeyword KissimeeGateParam = control_str_data.GetKeyword("KissimeeGateParam");
            Strformulapars.CS_highlimit = KissimeeGateParam.GetParameter(1).ToDouble();
            Strformulapars.CS_lowlimit = KissimeeGateParam.GetParameter(2).ToDouble();
            Strformulapars.CF_highlimit = KissimeeGateParam.GetParameter(3).ToDouble();
            Strformulapars.CF_lowlimit = KissimeeGateParam.GetParameter(4).ToDouble();
            Strformulapars.US_highlimit = KissimeeGateParam.GetParameter(5).ToDouble();
            Strformulapars.US_lowlimit = KissimeeGateParam.GetParameter(6).ToDouble();
            Strformulapars.CS_coef_a = KissimeeGateParam.GetParameter(7).ToDouble();
            Strformulapars.CF_coef_a = KissimeeGateParam.GetParameter(8).ToDouble();
            Strformulapars.US_coef_a = KissimeeGateParam.GetParameter(9).ToDouble();
            Strformulapars.UF_coef_a = KissimeeGateParam.GetParameter(10).ToDouble();
            Strformulapars.CS_exp_b = KissimeeGateParam.GetParameter(11).ToDouble();
            Strformulapars.CF_exp_b = KissimeeGateParam.GetParameter(12).ToDouble();
            Strformulapars.US_exp_b = KissimeeGateParam.GetParameter(13).ToDouble();
        }

        //从河网文件中获取集水区连接信息
        public static void GetDefault_CatchmentConnectInfo(string sourcefilename, ref Catchment_ConnectList Catchment_connectlist)
        {
            //新建PFSfile类对象，打开文件
            PFSFile pfsfile = new PFSFile(sourcefilename);

            //新建目标对象，找到指定的目标和指定的大节进行修改
            PFSSection target = pfsfile.GetTarget("MIKE_11_Network_editor", 1);

            //集水区连接节
            PFSSection CATCHMENT = target.GetSection("CATCHMENT", 1);
            List<Catchment_Connectinfo> CatchmentConnect_Infolist = new List<Catchment_Connectinfo>();

            for (int i = 0; i < CATCHMENT.GetKeywordsCount(); i++)
            {
                Catchment_Connectinfo catchmentconnect;
                PFSKeyword key = CATCHMENT.GetKeyword(i + 1);

                catchmentconnect.catchment_name = key.GetParameter(1).ToString();
                catchmentconnect.catchment_area = key.GetParameter(2).ToDouble();
                Reach_Segment connnectreach;
                connnectreach.reachname = key.GetParameter(3).ToString();
                connnectreach.reachtopoid = connnectreach.reachname + Model_Const.REACHID_HZ;
                connnectreach.start_chainage = key.GetParameter(4).ToDouble();
                connnectreach.end_chainage = key.GetParameter(5).ToDouble();
                catchmentconnect.connect_reach = connnectreach;

                CatchmentConnect_Infolist.Add(catchmentconnect);
            }

            Catchment_connectlist.CatchmentConnect_Infolist = CatchmentConnect_Infolist;
            pfsfile.Close();

            Console.WriteLine("河道集水区连接信息初始化成功!");
        }

        //从河网文件中获取河道计算点桩号信息，包括：断面桩号、入流出流点桩号、建筑物桩号，生成桩号集合
        public static void GetDefault_ComputeChainageList(string sourcefilename, ref ReachList Reachlist)
        {
            //新建PFSfile类对象，打开文件
            PFSFile pfsfile = new PFSFile(sourcefilename);

            //新建目标对象，找到指定的目标和指定的大节进行修改
            PFSSection target = pfsfile.GetTarget("MIKE_11_Network_editor", 1);

            //计算点的节
            PFSSection sec = target.GetSection("COMPUTATIONAL_SETUP", 1);

            //从默认原始NWK11文件中获取计算点桩号集合，包括 断面桩号、入流出流点桩号、建筑物桩号
            Dictionary<string, List<double>> reachgridchainage = new Dictionary<string, List<double>>();

            for (int n = 0; n < Reachlist.Reach_baseinfolist.Count; n++)
            {
                string reachname = Reachlist.Reach_baseinfolist[n].reachname;

                List<double> chainagelist = new List<double>();
                //遍历所有的河道字节，找到相同的河道上的特点计算点，加该计算点的桩号到集合
                for (int i = 0; i < sec.GetSectionsCount(); i++)
                {
                    PFSSection subsec = sec.GetSection(i + 1);
                    PFSKeyword subkey = subsec.GetKeyword("name");
                    if (subkey.GetParameter(1).ToString() == reachname)
                    {
                        PFSSection subsec1 = subsec.GetSection("points", 1);
                        for (int j = 0; j < subsec1.GetKeywordsCount(); j++)
                        {
                            PFSKeyword subkey1 = subsec1.GetKeyword(j + 1);
                            if (subkey1.GetParameter(3).ToInt() == 0 || subkey1.GetParameter(4).ToInt() == 2)   //水位点、入流点或建筑物点
                            {
                                double chainage = subkey1.GetParameter(1).ToDouble();
                                //如果集合里没有该桩号，则添加进去
                                if (!chainagelist.Contains(chainage))
                                {
                                    chainagelist.Add(subkey1.GetParameter(1).ToDouble());
                                }
                            }
                        }
                    }
                }

                reachgridchainage.Add(reachname, chainagelist);
            }

            //河道水位计算点初始化
            Reachlist.Reach_gridchainagelist = reachgridchainage;

            pfsfile.Close();

            Console.WriteLine("河道计算点桩号信息初始化成功!");
        }

        //从河网文件中获取已有建筑物规则调度节
        public static List<PFSSection> Get_DefaultStr_GZDUSecList(PFSFile pfsfile, string defaultstr_name)
        {
            //新建目标对象，找到指定的目标和指定的大节进行修改
            PFSSection target = pfsfile.GetTarget("MIKE_11_Network_editor", 1);

            //可控建筑物的节
            PFSSection CONTROL_STR = target.GetSection("STRUCTURE_MODULE", 1).GetSection("CONTROL_STR", 1);

            //获取规则调度节
            List<PFSSection> GateGZDU_SecList = new List<PFSSection>();
            for (int i = 0; i < CONTROL_STR.GetSectionsCount(); i++)
            {
                PFSSection control_str_data = CONTROL_STR.GetSection("control_str_data", i + 1);  //注意，各大节小节均从1开始

                //获取所在河道信息
                PFSKeyword Location = control_str_data.GetKeyword("Location");
                AtReach Reachinfo;
                Reachinfo.reachname = Location.GetParameter(1).ToString();
                Reachinfo.reachid = Reachinfo.reachname + Model_Const.REACHID_HZ;
                Reachinfo.chainage = Location.GetParameter(2).ToDouble();
                string strID = Location.GetParameter(3).ToString();  //建筑物ID，即建筑物名+调度规则，注意用'_'隔开
                string[] strarray = strID.Split('_');
                string strname = strarray[0] + "_" + strarray[1]; //用前2个字符串组成建筑物名
                string strddgz = strID.Substring(strname.Length + 1);  //截取建筑物调度规则名

                //如果是"GZDU"，则加入集合
                if (strname == defaultstr_name && strddgz.Contains("GZDU"))
                {
                    GateGZDU_SecList.Add(control_str_data);
                }
            }
            // pfsfile.Close(); ** 这个不能关，否则Gate.DefaultGate_GZDUSec_List就没有了，存储的PFSection对象并没有包含数据，需要连接pfsfile文件**

            return GateGZDU_SecList;
        }
        #endregion ***********************************************************************************


        #region ******************************更新NWK11河网文件***************************************
        // 根据一系列操作和修改，更新nwk11文件
        public static void Rewrite_Nkw11_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Nwk11_filename;
            string outputfilename = hydromodel.Modelfiles.Nwk11_filename;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            Catchment_ConnectList catchment_connectlist = hydromodel.Mike11Pars.Catchment_Connectlist;
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            if (catchmentlist == null) return;

            //打开PFS文件，开始编辑
            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection target = pfsfile.GetTarget("MIKE_11_Network_editor", 1);   //目标

            //更新数据区域节
            Update_DataAreaSec(hydromodel, ref target);
            Console.WriteLine("数据区域节更新成功!");

            //更新河道控制点节
            Update_PointsSec(reachlist, ref target);
            Console.WriteLine("河道控制点节更新成功!");

            //更新河道节
            Update_BranchSec(reachlist, ref target);
            Console.WriteLine("河道节更新成功!");

            //规则调度需要在基础模型的该文件里有，但如果基础模型改了调度规则就没有了，所以要从默认模型文件里找
            string default_sourcefilename = sourcefilename;
            string default_dirname = Model_Files.Get_Modeldir(Model_Const.DEFAULT_MODELNAME);
            Model_Files modelfiles = Model_Files.Get_Modelfilepath(default_dirname); //获取默认模型文件参数类
            if(modelfiles.Nwk11_filename != "") default_sourcefilename = modelfiles.Nwk11_filename;

            //更新可控建筑物节
            PFSSection CONTROL_STR = target.GetSection("STRUCTURE_MODULE", 1).GetSection("CONTROL_STR", 1);
            Update_ControlSec(default_sourcefilename, controllist, ref CONTROL_STR);  
            Console.WriteLine("可控建筑物节更新成功!");

            //更新集水区连接节
            Update_CatchmentSec(hydromodel, ref target);
            Console.WriteLine("集水区节更新成功!");

            //如果没有河道，清空计算点节
            Update_ComputeSec(hydromodel, ref target);
            Console.WriteLine("计算点清除成功!");

            //重新生成Nwk11文件
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("Nwk11一维河网文件更新成功!");
            Console.WriteLine("");

        }

        //更新数据区域节
        public static void Update_DataAreaSec(HydroModel hydromodel, ref PFSSection target)
        {
            //更新mesh文件路径
            PFSSection DATA_AREA = target.GetSection("DATA_AREA", 1);

            //更新当前坐标投影
            string Projection = hydromodel.ModelGlobalPars.Coordinate_type;
            PFSKeyword projection = DATA_AREA.GetKeyword("projection", 1);
            projection.GetParameter(1).ModifyStringParameter(Projection);

            //更新范围
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;
            if (reachlist.Reach_infolist == null) return;

            double min_x = reachlist.Reach_infolist[0].reachpoint_list[0].X;
            double min_y = reachlist.Reach_infolist[0].reachpoint_list[0].Y;
            double max_x = reachlist.Reach_infolist[0].reachpoint_list[0].X;
            double max_y = reachlist.Reach_infolist[0].reachpoint_list[0].Y;
            for (int i = 0; i < reachlist.Reach_infolist.Count; i++)
            {
                List<ReachPoint> reachpointlist = reachlist.Reach_infolist[i].reachpoint_list;
                for (int j = 0; j < reachpointlist.Count; j++)
                {
                    if (reachpointlist[j].X < min_x)
                    {
                        min_x = reachpointlist[j].X;
                    }

                    if (reachpointlist[j].X > max_x)
                    {
                        max_x = reachpointlist[j].X;
                    }

                    if (reachpointlist[j].Y < min_y)
                    {
                        min_y = reachpointlist[j].Y;
                    }

                    if (reachpointlist[j].Y > max_y)
                    {
                        max_y = reachpointlist[j].Y;
                    }
                }
            }

            //更新范围
            SubArea subarea;
            subarea.x1 = min_x;
            subarea.y1 = min_y;
            subarea.x2 = max_x;
            subarea.y2 = max_y;
            DATA_AREA.DeleteKeyword("x0", 1);
            DATA_AREA.DeleteKeyword("y0", 1);
            DATA_AREA.DeleteKeyword("x1", 1);
            DATA_AREA.DeleteKeyword("y1", 1);
            DATA_AREA.InsertNewKeyword("x0", 1).InsertNewParameterDouble(subarea.x1, 1);
            DATA_AREA.InsertNewKeyword("y0", 2).InsertNewParameterDouble(subarea.y1, 1);
            DATA_AREA.InsertNewKeyword("x1", 3).InsertNewParameterDouble(subarea.x2, 1);
            DATA_AREA.InsertNewKeyword("y1", 4).InsertNewParameterDouble(subarea.y2, 1);
        }

        //更新河道控制点节
        public static void Update_PointsSec(ReachList reachlist, ref PFSSection target)
        {
            //清空原nwk11文件中的河道控制点节
            target.DeleteSection("POINTS", 1);  //删除的节也是单独排
            PFSSection pointsec = target.InsertNewSection("POINTS", 3);  //重新添加节

            if (reachlist.Reach_infolist == null) return;

            //重新逐个添加控制点关键字
            int pointnumber = 1;
            for (int i = 0; i < reachlist.Reach_infolist.Count; i++)
            {
                List<ReachPoint> reachpointlist = reachlist.Reach_infolist[i].reachpoint_list;

                //添加新河道控制点关键字和参数
                for (int j = 0; j < reachpointlist.Count; j++)
                {
                    PFSKeyword key = pointsec.InsertNewKeyword("point", pointnumber);
                    object[] key_array ={
                                            reachpointlist[j].number,
                                            reachpointlist[j].X,
                                            reachpointlist[j].Y,
                                            reachpointlist[j].chainagetype,
                                            reachpointlist[j].pointchainage,
                                            0
                                        };
                    InsertKeyPars(ref key, ref pointnumber, key_array);
                }

            }
        }

        //更新河道节
        public static void Update_BranchSec(ReachList reachlist, ref PFSSection target)
        {
            //清空原nwk11文件中的河道节
            target.DeleteSection("BRANCHES", 1);  //删除的节也是单独排
            PFSSection reachsec = target.InsertNewSection("BRANCHES", 4);  //重新添加河道节
            if (reachlist.Reach_infolist == null) return;

            //重新逐个添加河道
            for (int i = 0; i < reachlist.Reach_infolist.Count; i++)
            {
                ReachInfo reach = reachlist.Reach_infolist[i];

                //添加河道节以及其中的关键字和参数
                PFSSection newreachsec = reachsec.InsertNewSection("branch", i + 1);

                //添加definitions 关键字和参数
                PFSKeyword key1 = newreachsec.InsertNewKeyword("definitions", 1);
                object[] key1_array ={
                                      reach.reachname,
                                      reach.reachtopoid,
                                      reach.start_chainage ,
                                      reach.end_chainage,
                                      0,                        //正向流
                                      reach.dx,              //步长
                                      0                         //常规河道
                                   };
                InsertKeyPars(ref key1, key1_array);

                //添加connections 关键字和参数
                PFSKeyword key2 = newreachsec.InsertNewKeyword("connections", 2);
                object[] key2_array ={
                                      reach.upstream_connect.reachname,
                                      reach.upstream_connect.chainage,
                                      reach.downstream_connect.reachname,
                                      reach.downstream_connect.chainage
                                  };
                InsertKeyPars(ref key2, key2_array);

                //添加points 关键字和参数
                PFSKeyword key3 = newreachsec.InsertNewKeyword("points", 3);

                object[] key3_array = new object[reach.reachpoint_list.Count];
                for (int j = 0; j < key3_array.Length; j++)
                {
                    key3_array[j] = reach.reachpoint_list[j].number;
                }
                InsertKeyPars(ref key3, key3_array);
            }
        }

        //更新可控建筑物节
        public static void Update_ControlSec(string sourcefilename, ControlList controllist, ref PFSSection CONTROL_STR)
        {
            //清空原nwk11文件中的可控建筑物节
            int controlcount = CONTROL_STR.GetSectionsCount();
            for (int i = 0; i < controlcount; i++)
            {
                CONTROL_STR.DeleteSection("control_str_data", 1);  //删除的节也是单独排
            }

            if (controllist.GateListInfo == null) return;

            //新建PFSfile类对象，打开文件
            PFSFile pfsfile = new PFSFile(sourcefilename);

            //重新逐个添加可控建筑物节
            int strsec_number = 1;
            for (int i = 0; i < controllist.GateListInfo.Count; i++)
            {
                //如果是普通闸门
                if (controllist.GateListInfo[i] is Normalstr)
                {
                    Normalstr normalstr = controllist.GateListInfo[i] as Normalstr;

                    //如果该闸门是现有闸门，且调度方式为规则调度，则采用可控建筑物节整体写入
                    if (controllist.Default_GateList.Contains(normalstr) && normalstr.Strddgz == CtrDdtype.GZDU)
                    {
                        List<PFSSection> Defaultstr_GZDUSecList = ControlList.GetGate_GZDUSec(pfsfile, normalstr.Strname);
                        for (int j = 0; j < Defaultstr_GZDUSecList.Count; j++)
                        {
                            PFSSection Defaultstr_GZDUSec = Defaultstr_GZDUSecList[j];
                            PFSSection control_str_data = CONTROL_STR.InsertNewSection("control_str_data", strsec_number);
                            Copy_PFSSection(Defaultstr_GZDUSec, ref control_str_data);

                            //修改节里的属性参数为最新的参数(以防已有默认建筑物参数被改)
                            UpdateParams(ref control_str_data, UpdateType.Chainage_Change, normalstr);

                            //规则调度非流量则改
                            if (control_str_data.GetKeyword("Attributes", 1).GetParameter(1).ToInt() != 2)
                            {
                                UpdateParams(ref control_str_data, UpdateType.Attri_Change, normalstr);
                            }

                            UpdateParams(ref control_str_data, UpdateType.Formula_Change, normalstr);
                            UpdateParams(ref control_str_data, UpdateType.Radial_Change, normalstr);

                            strsec_number++;
                        }
                    }
                    else
                    {
                        Insert_ControlStrSec(ref CONTROL_STR, ref strsec_number, normalstr);
                    }
                }

                //如果是分洪口
                if (controllist.GateListInfo[i] is Fhkstr)
                {
                    Fhkstr fhkstr = controllist.GateListInfo[i] as Fhkstr;

                    //如果该闸门是现有闸门，且调度方式为规则调度，则采用可控建筑物节整体写入
                    if (controllist.Default_GateList.Contains(fhkstr) && fhkstr.Strddgz == CtrDdtype.GZDU)
                    {
                        List<PFSSection> Defaultstr_GZDUSecList = ControlList.GetGate_GZDUSec(pfsfile, fhkstr.Strname);
                        for (int j = 0; j < Defaultstr_GZDUSecList.Count; j++)
                        {
                            PFSSection Defaultstr_GZDUSec = Defaultstr_GZDUSecList[j];
                            PFSSection control_str_data = CONTROL_STR.InsertNewSection("control_str_data", strsec_number);
                            Copy_PFSSection(Defaultstr_GZDUSec, ref control_str_data);

                            //修改节里的属性参数为最新的参数(以防已有默认建筑物参数被改)
                            UpdateParams(ref control_str_data, UpdateType.Chainage_Change, fhkstr);
                            UpdateParams(ref control_str_data, UpdateType.Attri_Change, fhkstr);

                            strsec_number++;
                        }
                    }
                    else
                    {
                        Insert_ControlStrSec(ref CONTROL_STR, ref strsec_number, fhkstr);
                    }
                }
            }
        }

        //更新集水区连接节
        public static void Update_CatchmentSec(HydroModel hydromodel, ref PFSSection target)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            Catchment_ConnectList catchment_connectlist = hydromodel.Mike11Pars.Catchment_Connectlist;

            //清空原nwk11文件中的集水区节
            target.DeleteSection("CATCHMENT", 1);  //删除的节也是单独排
            PFSSection catchmentsec = target.InsertNewSection("CATCHMENT", 6);  //重新添加集水区节

            //从全局集水区连接集合中获取集水区连接信息
            List<Catchment_Connectinfo> catchment_connnectlist = catchment_connectlist.CatchmentConnect_Infolist;

            if (catchment_connnectlist == null) return;

            //添加连接的集水区关键字和参数，需剔除xaj模型
            int number = 1;
            for (int i = 0; i < catchment_connnectlist.Count; i++)
            {
                CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchment_connnectlist[i].catchment_name);
                if (catchment.Now_RfmodelType != RFModelType.XAJ && hydromodel.ModelGlobalPars.Select_model != CalculateModel.only_hd)
                {
                    PFSKeyword key = catchmentsec.InsertNewKeyword("catchment", number);
                    object[] key_array ={
                                            catchment_connnectlist[i].catchment_name,
                                            catchment_connnectlist[i].catchment_area,
                                            catchment_connnectlist[i].connect_reach.reachname,
                                            catchment_connnectlist[i].connect_reach.start_chainage,
                                            catchment_connnectlist[i].connect_reach.end_chainage ,
                                         };
                    InsertKeyPars(ref key, ref number, key_array);
                }
            }
        }

        //如果没有河道，则清空计算点节
        public static void Update_ComputeSec(HydroModel hydromodel, ref PFSSection target)
        {
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;
            if (reachlist.Reach_gridchainagelist == null)
            {
                //清空原nwk11文件中的集水区节
                target.DeleteSection("COMPUTATIONAL_SETUP", 1);  //删除的节也是单独排
                PFSSection computesec = target.InsertNewSection("COMPUTATIONAL_SETUP", 7);  //重新添加集水区节

                PFSKeyword key = computesec.InsertNewKeyword("SaveAllGridPoints ", 1);
                key.InsertNewParameterBool(true, 1);
            }
        }
        #endregion *****************************************************************************
    

        #region ***********************更新河网文件里用到的各类子函数***************************
        //根据更新类型，更改可控建筑物节里的相关参数
        public static void UpdateParams(ref PFSSection controlsec, UpdateType updatetype, Controlstr controlstr)
        {
            //更新类型固定为4种
            switch (updatetype)
            {
                case UpdateType.Chainage_Change:   //里程位置更新

                    //修改Location关键字下的相关桩号参数
                    PFSKeyword location_key = controlsec.GetKeyword("Location");
                    location_key.DeleteParameter(1);
                    location_key.InsertNewParameterString(controlstr.Reachinfo.reachname, 1);
                    location_key.DeleteParameter(2);
                    location_key.InsertNewParameterDouble(controlstr.Reachinfo.chainage, 2);
                    location_key.DeleteParameter(4);
                    location_key.InsertNewParameterString(controlstr.Reachinfo.reachid, 4);

                    //修改目标点关键字下的相关桩号参数
                    PFSSection logsec = controlsec.GetSection("logical_statement", 1);
                    PFSKeyword logsec_key = logsec.GetKeyword("Targetpoint");
                    logsec_key.DeleteParameter(1);
                    logsec_key.InsertNewParameterString(controlstr.Reachinfo.reachname, 1);

                    logsec_key.DeleteParameter(2);
                    logsec_key.InsertNewParameterDouble(controlstr.Reachinfo.chainage, 2);

                    break;
                case UpdateType.Attri_Change:   //基本属性更新

                    //修改Location关键字下的相关桩号参数
                    controlsec.DeleteKeyword("Attributes", 1);        //关键字还是分类排，从1开始
                    PFSKeyword Att_key = controlsec.InsertNewKeyword("Attributes", 1);
                    object[] attarray ={  
                                        controlstr.Stratts.gate_type,
                                        controlstr.Stratts.gate_count,
                                        controlstr.Stratts.under_flowcc,
                                        controlstr.Stratts.gate_width,
                                        controlstr.Stratts.sill_level,
                                        controlstr.Stratts.max_speed,
                                        controlstr.Stratts.bool_initial_value,
                                        controlstr.Stratts.initial_value,
                                        controlstr.Stratts.bool_max_value,
                                        controlstr.Stratts.max_value,
                                        controlstr.Stratts.gate_height
                                        };
                    InsertKeyPars(ref Att_key, attarray);
                    break;
                case UpdateType.Radial_Change:    //弧门参数更新,可用于弧形闸门的参数率定

                    //修改RadialGateParam关键字下的相关桩号参数
                    controlsec.DeleteKeyword("RadialGateParam", 1);
                    PFSKeyword Radial_key = controlsec.InsertNewKeyword("RadialGateParam", 1);
                    object[] attarray1 ={ 
                                            controlstr.Strradiapars.tune_factor,
                                            controlstr.Strradiapars.height,
                                            controlstr.Strradiapars.radius,
                                            controlstr.Strradiapars.trunnion,
                                            controlstr.Strradiapars.weir_coeff,
                                            controlstr.Strradiapars.weri_exp,
                                            controlstr.Strradiapars.tran_bottom,
                                            controlstr.Strradiapars.tran_depth
                                        };
                    InsertKeyPars(ref Radial_key, attarray1);
                    break;
                case UpdateType.Formula_Change:   //公式参数更新,可用于公式闸门的参数率定

                    controlsec.DeleteKeyword("KissimeeGateParam", 1);
                    PFSKeyword Formula_key = controlsec.InsertNewKeyword("KissimeeGateParam", 1);
                    object[] attarray2 ={
                                            controlstr.Strformulapars.CS_highlimit,
                                            controlstr.Strformulapars.CS_lowlimit,
                                            controlstr.Strformulapars.CF_highlimit,
                                            controlstr.Strformulapars.CF_lowlimit,
                                            controlstr.Strformulapars.US_highlimit,
                                            controlstr.Strformulapars.US_lowlimit,

                                            controlstr.Strformulapars.CS_coef_a,
                                            controlstr.Strformulapars.CF_coef_a,
                                            controlstr.Strformulapars.US_coef_a,
                                            controlstr.Strformulapars.UF_coef_a,

                                            controlstr.Strformulapars.CS_exp_b,
                                            controlstr.Strformulapars.CF_exp_b,
                                            controlstr.Strformulapars.US_exp_b
                                         };

                    InsertKeyPars(ref Formula_key, attarray2);
                    break;
                default:
                    break;
            }
        }

        //将一个PFSSection节里的节、关键字、参数拷贝到另一个空白的节里
        public static void Copy_PFSSection(PFSSection SourceSec, ref PFSSection New_BlankSec)
        {
            Copy_PfsKeyword(SourceSec, New_BlankSec);//将SourceSec里的关键字及其参数拷贝到New_BlankSec

            List<PFSSection> list1 = new List<PFSSection>();
            List<PFSSection> list2 = new List<PFSSection>();
            List<PFSSection> new_list1 = new List<PFSSection>();
            List<PFSSection> new_list2 = new List<PFSSection>();

            bool isIncludeSec;  //用来判断节是否含有子节

            int IncludeSec = SourceSec.GetSectionsCount();  //SourceSec中子节的数量

            if (IncludeSec == 0)  //此时SourceSec不含有子节
            {
                isIncludeSec = false;
            }
            else  //此时SourceSec含有子节
            {
                isIncludeSec = true;
            }

            for (int i = 0; i < IncludeSec; i++)  //遍历每一个节
            {
                list1.Add(SourceSec.GetSection(i + 1));  //将SourceSec中的节添加到list1中
                PFSSection sec_New_BlankSec = New_BlankSec.InsertNewSection(SourceSec.GetSection(i + 1).Name, i + 1);
                new_list1.Add(sec_New_BlankSec);  //将New_BlankSec新增加的节添加到new_list1中
            }

            while (isIncludeSec)
            {
                isIncludeSec = false;  //赋值为false，如果经判断子节中还有子节，则后面会再赋值为true

                for (int j = 0; j < list1.Count; j++)  //循环list1中每一个节，将每一个节里的关键字都拷贝到新的节中
                {
                    Copy_PfsKeyword(list1[j], new_list1[j]);  //将每一个节里的关键字都拷贝到新的节中
                }

                for (int j = 0; j < list1.Count; j++)  //循环每一个节，将每一个节中的子节都拷贝到新的节中
                {
                    int sec_count = list1[j].GetSectionsCount();  //判断是否还有子节

                    if (sec_count != 0)  //含有子节
                    {
                        isIncludeSec = true;
                    }

                    for (int k = 0; k < sec_count; k++)  //循环每一个子节
                    {
                        list2.Add(list1[j].GetSection(k + 1));  //将list1[j]中还有的子节添加到list2中

                        PFSSection sec = new_list1[j].InsertNewSection(list1[j].GetSection(k + 1).Name, k + 1);  //添加相应的子节

                        new_list2.Add(sec);  //将新添加的子节添加到new_list2中
                    }

                }

                list1.Clear();  //清空list1
                for (int j = 0; j < list2.Count; j++)//将list2中存储的子节添加到list1中，开始新的循环
                {
                    list1.Add(list2[j]);
                }

                list2.Clear();

                new_list1.Clear();
                for (int j = 0; j < new_list2.Count; j++)
                {
                    new_list1.Add(new_list2[j]);
                }
                new_list2.Clear();

            }

        }

        // 将节里的关键字及其参数拷贝到新的节里
        public static void Copy_PfsKeyword(PFSSection sec, PFSSection newSec)
        {
            for (int i = 0; i < sec.GetKeywordsCount(); i++)
            {
                PFSKeyword key_i = sec.GetKeyword(i + 1);

                PFSKeyword new_key_i = newSec.InsertNewKeyword(sec.GetKeyword(i + 1).Name, i + 1);  //在新的节里插入一个与key_i的名字和位置都一样的关键字

                Copy_Keyword(key_i, new_key_i);  //将key_i的参数拷贝到new_key_i
            }
        }

        // 将一个关键字的参数拷贝到新的关键字中
        public static void Copy_Keyword(PFSKeyword key, PFSKeyword newKey)
        {
            //遍历关键字的参数，判断每一个参数的类型，然后拷贝到新的关键字中
            for (int i = 0; i < key.GetParametersCount(); i++)
            {
                if (key.GetParameter(i + 1).IsBool())  //是否为bool类型
                {
                    newKey.InsertNewParameterBool(key.GetParameter(i + 1).ToBoolean(), i + 1);
                }

                else if (key.GetParameter(i + 1).IsDouble())  //是否为double类型
                {
                    newKey.InsertNewParameterDouble(key.GetParameter(i + 1).ToDouble(), i + 1);
                }

                else if (key.GetParameter(i + 1).IsFilename())
                {
                    newKey.InsertNewParameterFileName(key.GetParameter(i + 1).ToFileName(), i + 1);
                }

                else if (key.GetParameter(i + 1).IsInt())
                {
                    newKey.InsertNewParameterInt(key.GetParameter(i + 1).ToInt(), i + 1);
                }

                else if (key.GetParameter(i + 1).IsString())
                {
                    newKey.InsertNewParameterString(key.GetParameter(i + 1).ToString(), i + 1);
                }
            }
        }

        //写入新增的溃堤建筑物子节,注意插入的关键字和节是统一排序的，而获取是分组排序
        public static void Insert_ControlStrSec(ref PFSSection sec, ref int secnumber, Fhkstr fhkstr)
        {
            //在末端插入该建筑物的节
            PFSSection strsec = sec.InsertNewSection("control_str_data", secnumber);
            secnumber++;

            //添加位置关键字和参数
            PFSKeyword key1 = strsec.InsertNewKeyword("Location", 1);
            string strid = fhkstr.Strname + "_" + fhkstr.Strddgz.ToString();
            object[] key1_array ={
                                     fhkstr.Reachinfo.reachname,
                                     fhkstr.Reachinfo.chainage,
                                     strid,
                                     fhkstr.Reachinfo.reachid
                                 };
            InsertKeyPars(ref key1, key1_array);

            //添加默认水库节
            InsertDefualtSec(ref strsec, Default_SecName.ReservoirData, fhkstr);

            //添加默认显示水平偏移、闸门高度、水头损失系数关键字
            InsertDefualtSec(ref strsec, Default_SecName.HorizOffset_GateHeight_HeadLossFactors, fhkstr);

            //添加基本属性关键字和参数
            PFSKeyword key2 = strsec.InsertNewKeyword("Attributes", 5);
            object[] attarray ={ 
                                  fhkstr.Stratts.gate_type,
                                  fhkstr.Stratts.gate_count,
                                  fhkstr.Stratts.under_flowcc,
                                  fhkstr.Stratts.gate_width,
                                  fhkstr.Stratts.sill_level,
                                  fhkstr.Stratts.max_speed,
                                  fhkstr.Stratts.bool_initial_value,
                                  fhkstr.Stratts.initial_value,
                                  fhkstr.Stratts.bool_max_value,
                                  fhkstr.Stratts.max_value,
                                  fhkstr.Stratts.gate_height
                               };
            InsertKeyPars(ref key2, attarray);

            //添加默认弧门参数、闸门公式参数关键字
            InsertDefualtSec(ref strsec, Default_SecName.RadialGateParam_KissimeeGateParam, fhkstr);

            //添加逻辑判断节
            if (fhkstr.Strddgz == CtrDdtype.GZDU)
            {
                //调用相应的插入2个逻辑声明节的方法
                Insert_Fhk_LogicSec(ref strsec, fhkstr);

            }
            else if (fhkstr.Strddgz == CtrDdtype.ZMDU_TIME)
            {
                //调用相应的插入时间逻辑声明节的方法
                Insert_LogicSec_TIME(ref strsec, fhkstr);
            }
        }

        //写入新增的普通建筑物子节,注意插入的关键字和节是统一排序的，而获取是分组排序
        public static void Insert_ControlStrSec(ref PFSSection sec, ref int secnumber, Normalstr normalstr)
        {
            //在末端插入该建筑物的节
            PFSSection strsec = sec.InsertNewSection("control_str_data", secnumber);
            secnumber++;

            //添加位置关键字和参数
            PFSKeyword key1 = strsec.InsertNewKeyword("Location", 1);
            string strid = normalstr.Strname + "_" + normalstr.Strddgz.ToString();
            object[] key1_array ={
                                     normalstr.Reachinfo.reachname,
                                     normalstr.Reachinfo.chainage,
                                     strid,
                                     normalstr.Reachinfo.reachid
                                 };
            InsertKeyPars(ref key1, key1_array);

            //添加水库节
            InsertDefualtSec(ref strsec, Default_SecName.ReservoirData, normalstr);

            //添加显示水平偏移、闸门高度、水头损失系数关键字
            Insert_Headlossparams(ref strsec, normalstr);

            //添加基本属性关键字和参数
            PFSKeyword key2 = strsec.InsertNewKeyword("Attributes", 5);

            //如果调度规则改为流量类型，但闸门类型属性又不是流量，则采用默认流量属性
            Attributes gate_attributes;
            if ((normalstr.Strddgz == CtrDdtype.KXDU || normalstr.Strddgz == CtrDdtype.KXDU_TIME) && normalstr.Stratts.gate_type != 2)
            {
                gate_attributes = Controlstr.Getdefault_Discharge_Attribute();
            }
            else
            {
                gate_attributes = normalstr.Stratts;
            }

            //添加关键字和值
            object[] attarray ={  gate_attributes.gate_type,
                                  gate_attributes.gate_count,
                                  gate_attributes.under_flowcc,
                                  gate_attributes.gate_width,
                                  gate_attributes.sill_level,
                                  gate_attributes.max_speed,
                                  gate_attributes.bool_initial_value,
                                  gate_attributes.initial_value,
                                  gate_attributes.bool_max_value,
                                  gate_attributes.max_value,
                                  gate_attributes.gate_height
                               };
            InsertKeyPars(ref key2, attarray);

            //添加弧门参数、闸门公式参数关键字
            Insert_RKparams(ref strsec, normalstr);

            //添加逻辑判断节--新增闸门不设置规则调度
            switch (normalstr.Strddgz)
            {
                //新增的可控建筑物规则调度统一采用闸门类型的全开模式
                case CtrDdtype.GZDU:
                    //如果不是全开，强行变为全开
                    if (normalstr.Ddparams_zmdu.fullyopen != true)
                    {
                        ZMDUinfo zmduinfo;
                        zmduinfo.fullyopen = true;
                        zmduinfo.opengateheight = 0;
                        zmduinfo.opengatenumber = 1;
                        normalstr.Ddparams_zmdu = zmduinfo;
                    }
                    Insert_LogicSec_ZMDU(ref strsec, normalstr);
                    break;
                case CtrDdtype.KXDU_TIME:
                case CtrDdtype.ZMDU_TIME:
                    Insert_LogicSec_TIME(ref strsec, normalstr);
                    break;
                case CtrDdtype.ZMDU:
                    Insert_LogicSec_ZMDU(ref strsec, normalstr);
                    break;
                case CtrDdtype.KXDU:
                    if(normalstr.Str_QHrelation == null)
                    {
                        Insert_LogicSec_KXDU(ref strsec, normalstr);
                    }
                    else
                    {
                        //采用水位流量关系控制建筑物流量
                        Insert_LogicSec_KXDU1(ref strsec, normalstr);
                    }
                    break;
                default:
                    break;
            }
        }

        //写入分洪口规则调度的两个逻辑声明节
        public static void Insert_Fhk_LogicSec(ref PFSSection strsec, Fhkstr fhkstr)
        {
            //增加第1个逻辑申明节**************************** Set equal to ************************************************
            PFSSection logicalsec1 = strsec.InsertNewSection("logical_statement", 2);

            //插入逻辑基本参数
            PFSKeyword sec1_key1 = logicalsec1.InsertNewKeyword("LS_parameter", 1);
            object[] sec1_key1_array = { 1, 8, 0, 0, fhkstr.fhklevel }; //设置分洪底高程，未必等于地面高程
            InsertKeyPars(ref sec1_key1, sec1_key1_array);

            //插入逻辑运算节
            PFSSection logoperandsec = logicalsec1.InsertNewSection("logical_operands", 1);

            //插入逻辑运算关键字中的节、关键字和参数
            PFSKeyword sec1_key2 = logoperandsec.InsertNewKeyword("logicaloperand", 1);
            Filestring filenamestring;
            filenamestring.filename = "";
            object[] sec1_key2_array = { 21, "", 0, "", 0, "", 0, 2, 0, fhkstr.fhwaterlevel, filenamestring, "", 0, 1, 1000, 0, 0, 0, false, 0 };
            InsertKeyPars(ref sec1_key2, sec1_key2_array);

            PFSSection LOSummation = logoperandsec.InsertNewSection("LOSummation", 1);
            PFSKeyword LOSummationRule = LOSummation.InsertNewKeyword("LOSummationRule", 1);
            InsertKeyPars(ref LOSummationRule, 0, 0);

            //插入默认控制点关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.controlpoint, fhkstr);

            //插入默认目标点关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.Targetpoint, fhkstr);

            //插入默认迭代和PID关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.iterate_pid, fhkstr);

            //增加第2个逻辑申明节**************************** Close ************************************************
            PFSSection logicalsec2 = strsec.InsertNewSection("logical_statement", 3);

            //插入逻辑基本参数
            PFSKeyword sec2_key1 = logicalsec2.InsertNewKeyword("LS_parameter", 1);
            object[] sec2_key1_array = { 2, 5, 0, 8, 0 }; //全关  
            InsertKeyPars(ref sec2_key1, sec2_key1_array);
            PFSSection logoperandsec1 = logicalsec2.InsertNewSection("logical_operands", 1);
            PFSSection LOSummation1 = logoperandsec.InsertNewSection("LOSummation", 1);

            //插入默认控制点关键字
            InsertDefualtSec(ref logicalsec2, Default_SecName.controlpoint, fhkstr);

            //插入默认目标点关键字
            InsertDefualtSec(ref logicalsec2, Default_SecName.Targetpoint, fhkstr);

            //插入默认迭代和PID关键字
            InsertDefualtSec(ref logicalsec2, Default_SecName.iterate_pid, fhkstr);
        }

        //插入其他几个常用的默认节
        public static void InsertDefualtSec(ref PFSSection sec, Default_SecName secorkeyname, Controlstr controlstr)
        {
            switch (secorkeyname)
            {
                case Default_SecName.ReservoirData:
                    //添加水库节
                    PFSSection reservsec = sec.InsertNewSection("ReservoirData", 1);
                    PFSKeyword reskey1 = reservsec.InsertNewKeyword("StructureType", 1);
                    if (controlstr is Normalstr)
                    {
                        int side = (int)controlstr.Str_Sidetype;
                        reskey1.InsertNewParameterDouble(side, 1);    //正向或侧向建筑物
                    }
                    else if (controlstr is Fhkstr)
                    {
                        reskey1.InsertNewParameterDouble(1, 1);    //测向建筑物
                    }

                    PFSKeyword reskey2 = reservsec.InsertNewKeyword("StorageType", 2);
                    reskey2.InsertNewParameterDouble(0, 1);
                    PFSKeyword reskey3 = reservsec.InsertNewKeyword("ApplyXY", 3);
                    reskey3.InsertNewParameterDouble(0, 1);
                    PFSKeyword reskey4 = reservsec.InsertNewKeyword("CoordXY", 4);
                    reskey4.InsertNewParameterDouble(0, 1);
                    reskey4.InsertNewParameterDouble(0, 2);
                    PFSKeyword reskey5 = reservsec.InsertNewKeyword("InitialArea", 5);
                    reskey5.InsertNewParameterDouble(0, 1);
                    PFSSection Elevation = reservsec.InsertNewSection("Elevation", 1);

                    break;
                case Default_SecName.HorizOffset_GateHeight_HeadLossFactors:
                    //水平偏移、闸门高度、水头损失系数
                    PFSKeyword HorizOffset = sec.InsertNewKeyword("HorizOffset", 2);
                    HorizOffset.InsertNewParameterDouble(0, 1);
                    PFSKeyword GateHeight = sec.InsertNewKeyword("GateHeight", 3);
                    GateHeight.InsertNewParameterDouble(0, 1);
                    PFSKeyword HeadLossFactors = sec.InsertNewKeyword("HeadLossFactors", 4);
                    InsertKeyPars(ref HeadLossFactors, 0.5, 1, 1, 0.5, 1, 1);

                    break;
                case Default_SecName.RadialGateParam_KissimeeGateParam:
                    //弧形闸门和公式闸门参数
                    PFSKeyword RadialGateParam = sec.InsertNewKeyword("RadialGateParam", 6);
                    InsertKeyPars(ref RadialGateParam, 1, 1, 1, 1, 1.838, 1.5, -0.152, 0.304);
                    PFSKeyword KissimeeGateParam = sec.InsertNewKeyword("KissimeeGateParam", 7);
                    InsertKeyPars(ref KissimeeGateParam, 1.05, 0.95, 1.55, 1.45, 0.7, 0.6, 1.12, 0.89, 0.86, 0.77, 0.21, 0.17, 0.38);

                    break;
                case Default_SecName.controlpoint:
                    //控制点
                    PFSKeyword controlpoint = sec.InsertNewKeyword("controlpoint", 2);
                    InsertKeyPars(ref controlpoint, "", 0, "", 0, "", 0, 0, 0, 0, 0, 0);
                    PFSSection CtrSummation = sec.InsertNewSection("CtrSummation", 2);
                    PFSKeyword CTRSummationRule = CtrSummation.InsertNewKeyword("CTRSummationRule", 1);

                    InsertKeyPars(ref CTRSummationRule, 0, 0);
                    break;
                case Default_SecName.Targetpoint:      //目标点
                    if (controlstr is Fhkstr || controlstr is Normalstr)
                    {
                        Fhkstr str = controlstr as Fhkstr;
                        PFSKeyword Targetpoint = sec.InsertNewKeyword("Targetpoint", 3);
                        Filestring filenamestring1;
                        filenamestring1.filename = "";
                        string strid = str.Strname + "_" + str.Strddgz.ToString();
                        object[] Targetpoint_array = {  
                                                str.Reachinfo.reachname,        //所在河名
                                                str.Reachinfo.chainage,    //桩号
                                                strid,                             //建筑物ID
                                                0,"",0,                            
                                                filenamestring1,                     //时间文件路径
                                                "",               //项目名
                                                0,0
                                                };
                        InsertKeyPars(ref Targetpoint, Targetpoint_array);

                        PFSSection TargetSummation = sec.InsertNewSection("TargetSummation", 3);
                        PFSKeyword TargetSummationRule = TargetSummation.InsertNewKeyword("TargetSummationRule", 1);
                        InsertKeyPars(ref TargetSummationRule, 0, 0);
                    }
                    break;
                case Default_SecName.iterate_pid:      //迭代
                    PFSKeyword pid_variables = sec.InsertNewKeyword("pid_variables", 4);
                    InsertKeyPars(ref pid_variables, 1, 300, 0.8, 1, 0.7, 1);
                    PFSKeyword iterate = sec.InsertNewKeyword("iterate", 5);
                    InsertKeyPars(ref iterate, 0.1, 0, 0.1, 0.1);
                    PFSSection control_strategy = sec.InsertNewSection("control_strategy", 4);
                    PFSKeyword data = control_strategy.InsertNewKeyword("data", 1);
                    InsertKeyPars(ref data, 0, 0);
                    PFSKeyword scaling = sec.InsertNewKeyword("scaling", 6);
                    Filestring filename1;
                    filename1.filename = "";
                    InsertKeyPars(ref scaling, 0, 0, "", 0, "", 0, "", 0, filename1, "", 0, 0);
                    PFSSection ScaleSummation = sec.InsertNewSection("ScaleSummation", 5);
                    PFSKeyword ScaleSummationRule = ScaleSummation.InsertNewKeyword("ScaleSummationRule", 1);
                    InsertKeyPars(ref ScaleSummationRule, 0, 0);

                    break;
                default:
                    break;
            }
        }

        //插入水平偏移、闸门高度、水头损失参数
        public static void Insert_Headlossparams(ref PFSSection sec, Normalstr normalstr)
        {
            //水平偏移、闸门高度、水头损失系数
            PFSKeyword HorizOffset = sec.InsertNewKeyword("HorizOffset", 2);
            HorizOffset.InsertNewParameterDouble(0, 1);
            PFSKeyword GateHeight = sec.InsertNewKeyword("GateHeight", 3);
            GateHeight.InsertNewParameterDouble(normalstr.Stratts.gate_height, 1);

            //水头损失系数
            PFSKeyword HeadLossFactors = sec.InsertNewKeyword("HeadLossFactors", 4);
            object[] attarray ={ 
                                   normalstr.Strheadloss.positive_inflow,
                                   normalstr.Strheadloss.positive_outflow,
                                   normalstr.Strheadloss.positive_freeoverflow,
                                   normalstr.Strheadloss.negative_inflow,
                                   normalstr.Strheadloss.negative_outflow,
                                   normalstr.Strheadloss.positive_freeoverflow
                                };

            InsertKeyPars(ref HeadLossFactors, attarray);
        }

        //插入弧形闸门、公式闸门参数
        public static void Insert_RKparams(ref PFSSection sec, Normalstr normalstr)
        {
            //插入弧形闸门的参数参数
            PFSKeyword RadialGateParam = sec.InsertNewKeyword("RadialGateParam", 6);
            object[] attarray1 ={ 
                                  normalstr.Strradiapars.tune_factor,
                                  normalstr.Strradiapars.height,
                                  normalstr.Strradiapars.radius,
                                  normalstr.Strradiapars.trunnion,
                                  normalstr.Strradiapars.weir_coeff,
                                  normalstr.Strradiapars.weri_exp,
                                  normalstr.Strradiapars.tran_bottom,
                                  normalstr.Strradiapars.tran_depth
                                };
            InsertKeyPars(ref RadialGateParam, attarray1);

            //插入公式闸门的参数
            PFSKeyword KissimeeGateParam = sec.InsertNewKeyword("KissimeeGateParam", 7);
            object[] attarray2 ={
                                    normalstr.Strformulapars.CS_highlimit,
                                    normalstr.Strformulapars.CS_lowlimit,
                                    normalstr.Strformulapars.CF_highlimit,
                                    normalstr.Strformulapars.CF_lowlimit,
                                    normalstr.Strformulapars.US_highlimit,
                                    normalstr.Strformulapars.US_lowlimit,

                                    normalstr.Strformulapars.CS_coef_a,
                                    normalstr.Strformulapars.CF_coef_a,
                                    normalstr.Strformulapars.US_coef_a,
                                    normalstr.Strformulapars.UF_coef_a,

                                    normalstr.Strformulapars.CS_exp_b,
                                    normalstr.Strformulapars.CF_exp_b,
                                    normalstr.Strformulapars.US_exp_b
                               };

            InsertKeyPars(ref KissimeeGateParam, attarray2);
        }

        //添加闸门时间或控泄时间调度逻辑申明节
        public static void Insert_LogicSec_TIME(ref PFSSection strsec, Controlstr controlstr)
        {
            //增加1个逻辑申明节
            PFSSection logicalsec1 = strsec.InsertNewSection("logical_statement", 2);

            //插入逻辑基本参数
            PFSKeyword sec1_key1 = logicalsec1.InsertNewKeyword("LS_parameter", 1);
            object[] sec1_key1_array = { 1, 0, 10, 0, 0 }; //时间调度
            InsertKeyPars(ref sec1_key1, sec1_key1_array);

            //插入逻辑运算节
            PFSSection logoperandsec = logicalsec1.InsertNewSection("logical_operands", 1);
            PFSSection LOSummation = logoperandsec.InsertNewSection("LOSummation", 1);

            //插入控制点关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.controlpoint, controlstr);

            //插入目标点关键字及其参数
            PFSKeyword Targetpoint = logicalsec1.InsertNewKeyword("Targetpoint", 3);

            //分洪口建筑物和普通建筑物文件名河项目名参数属性定义有区别
            Filestring timefilename;
            timefilename.filename = null;
            string itemname = null;
            string strid = null;
            if (controlstr is Fhkstr)
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                timefilename.filename = fhkstr.time_filename;

                strid = fhkstr.Strname + "_" + fhkstr.Strddgz.ToString();
                itemname = fhkstr.time_itemname;
            }
            else if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                timefilename.filename = normalstr.Ddparams_filename;

                strid = normalstr.Strname + "_" + normalstr.Strddgz.ToString();
                itemname = normalstr.Ddparams_itemname;
            }

            object[] Targetpoint_array = {  
                                                controlstr.Reachinfo.reachname,        //所在河名
                                                controlstr.Reachinfo.chainage,    //桩号
                                                strid,                             //建筑物ID
                                                0,"",0,                            
                                                timefilename,                       //时间文件路径
                                                itemname,        //项目名
                                                1,0
                                          };
            InsertKeyPars(ref Targetpoint, Targetpoint_array);

            PFSSection TargetSummation = logicalsec1.InsertNewSection("TargetSummation", 3);
            PFSKeyword TargetSummationRule = TargetSummation.InsertNewKeyword("TargetSummationRule", 1);
            InsertKeyPars(ref TargetSummationRule, 0, 0);

            //插入迭代和PID关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.iterate_pid, controlstr);
        }

        //添加闸门调度逻辑声明节
        public static void Insert_LogicSec_ZMDU(ref PFSSection strsec, Normalstr normalstr)
        {
            //增加1个逻辑申明节
            PFSSection logicalsec1 = strsec.InsertNewSection("logical_statement", 2);

            //插入逻辑基本参数
            PFSKeyword sec1_key1 = logicalsec1.InsertNewKeyword("LS_parameter", 1);

            if (normalstr.Ddparams_zmdu.fullyopen == true)   //全开
            {
                object[] sec1_key1_array = { 1, 4, 0, 8, 0 }; //全开
                InsertKeyPars(ref sec1_key1, sec1_key1_array);
            }
            else if (normalstr.Ddparams_zmdu.opengatenumber == 0) //全关
            {
                object[] sec1_key1_array = { 1, 5, 0, 8, 0 }; //全开
                InsertKeyPars(ref sec1_key1, sec1_key1_array);
            }
            else    //半开，改闸门总开启宽度和各闸开启高度
            {
                PFSKeyword key1 = strsec.GetKeyword("Attributes");

                //因关键字的ModifyDoubleParameter方法不能用，只好先删再加参数
                key1.DeleteParameter(2);
                key1.InsertNewParameterDouble(normalstr.Ddparams_zmdu.opengatenumber, 2);  //更改开闸数量

                //修改逻辑申明中的闸门开启高度
                object[] sec1_key1_array = { 1, 8, 0, 8, normalstr.Ddparams_zmdu.opengateheight }; //用实际开度
                InsertKeyPars(ref sec1_key1, sec1_key1_array);
            }

            //插入逻辑运算节
            PFSSection logoperandsec = logicalsec1.InsertNewSection("logical_operands", 1);
            PFSSection LOSummation = logoperandsec.InsertNewSection("LOSummation", 1);

            //插入控制点关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.controlpoint, normalstr);

            //插入目标点关键字及其参数
            PFSKeyword Targetpoint = logicalsec1.InsertNewKeyword("Targetpoint", 3);
            Filestring timefilename;
            timefilename.filename = normalstr.Ddparams_filename;
            string strid = normalstr.Strname + "_" + normalstr.Strddgz.ToString();
            object[] Targetpoint_array = {  
                                                normalstr.Reachinfo.reachname,        //所在河名
                                                normalstr.Reachinfo.chainage,    //桩号
                                                strid,                             //建筑物ID
                                                0,"",0,                            
                                                timefilename,                 //时间文件路径
                                                normalstr.Ddparams_itemname,          //项目名
                                                0,0
                                                };
            InsertKeyPars(ref Targetpoint, Targetpoint_array);

            PFSSection TargetSummation = logicalsec1.InsertNewSection("TargetSummation", 3);
            PFSKeyword TargetSummationRule = TargetSummation.InsertNewKeyword("TargetSummationRule", 1);
            InsertKeyPars(ref TargetSummationRule, 0, 0);

            //插入迭代和PID关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.iterate_pid, normalstr);
        }

        //添加控泄调度逻辑声明节(设置等流量)
        public static void Insert_LogicSec_KXDU(ref PFSSection strsec, Normalstr normalstr)
        {
            //增加1个逻辑申明节
            PFSSection logicalsec1 = strsec.InsertNewSection("logical_statement", 2);

            //插入逻辑基本参数
            PFSKeyword sec1_key1 = logicalsec1.InsertNewKeyword("LS_parameter", 1);

            //逻辑声明基本参数写入
            object[] sec1_key1_array = { 1, 8, 0, 5, normalstr.Ddparams_double };
            InsertKeyPars(ref sec1_key1, sec1_key1_array);

            //插入逻辑运算节
            PFSSection logoperandsec = logicalsec1.InsertNewSection("logical_operands", 1);
            PFSSection LOSummation = logoperandsec.InsertNewSection("LOSummation", 1);

            //插入控制点关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.controlpoint, normalstr);

            //插入目标点关键字及其参数
            PFSKeyword Targetpoint = logicalsec1.InsertNewKeyword("Targetpoint", 3);
            Filestring timefilename;
            timefilename.filename = normalstr.Ddparams_filename;
            string strid = normalstr.Strname + "_" + normalstr.Strddgz.ToString();
            object[] Targetpoint_array = {  
                                                normalstr.Reachinfo.reachname,        //所在河名
                                                normalstr.Reachinfo.chainage,    //桩号
                                                strid,                             //建筑物ID
                                                0,"",0,                            
                                                timefilename,                 //时间文件路径
                                                normalstr.Ddparams_itemname,          //项目名
                                                0,0
                                                };
            InsertKeyPars(ref Targetpoint, Targetpoint_array);

            PFSSection TargetSummation = logicalsec1.InsertNewSection("TargetSummation", 3);
            PFSKeyword TargetSummationRule = TargetSummation.InsertNewKeyword("TargetSummationRule", 1);
            InsertKeyPars(ref TargetSummationRule, 0, 0);

            //插入迭代和PID关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.iterate_pid, normalstr);
        }

        //添加控泄调度逻辑声明节(设置表格类型的水位流量关系)
        public static void Insert_LogicSec_KXDU1(ref PFSSection strsec, Normalstr normalstr)
        {
            //增加1个逻辑申明节
            PFSSection logicalsec1 = strsec.InsertNewSection("logical_statement", 2);

            //插入逻辑基本参数
            PFSKeyword sec1_key1 = logicalsec1.InsertNewKeyword("LS_parameter", 1);

            //逻辑声明基本参数写入
            object[] sec1_key1_array = { 1, 0, 19, 8, 0 };
            InsertKeyPars(ref sec1_key1, sec1_key1_array);

            //插入逻辑运算节
            PFSSection logoperandsec = logicalsec1.InsertNewSection("logical_operands", 1);
            PFSSection LOSummation = logoperandsec.InsertNewSection("LOSummation", 1);

            //插入控制点关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.controlpoint, normalstr);

            //插入目标点关键字及其参数
            PFSKeyword Targetpoint = logicalsec1.InsertNewKeyword("Targetpoint", 3);
            Filestring timefilename;
            timefilename.filename = normalstr.Ddparams_filename;
            string strid = normalstr.Strname + "_" + normalstr.Strddgz.ToString();
            object[] Targetpoint_array = {
                                                normalstr.Reachinfo.reachname,        //所在河名
                                                normalstr.Reachinfo.chainage,    //桩号
                                                strid,                             //建筑物ID
                                                0,"",0,
                                                timefilename,                 //时间文件路径
                                                normalstr.Ddparams_itemname,          //项目名
                                                0,0
                                                };
            InsertKeyPars(ref Targetpoint, Targetpoint_array);

            PFSSection TargetSummation = logicalsec1.InsertNewSection("TargetSummation", 3);
            PFSKeyword TargetSummationRule = TargetSummation.InsertNewKeyword("TargetSummationRule", 1);
            InsertKeyPars(ref TargetSummationRule, 0, 0);

            //插入迭代和PID等默认关键字
            InsertDefualtSec(ref logicalsec1, Default_SecName.iterate_pid, normalstr);

            //修改控制策略节
            PFSSection control_strategy = logicalsec1.GetSection("control_strategy", 1);
            control_strategy.DeleteKeyword(1);
            for (int i = 0; i < normalstr.Str_QHrelation.Count; i++)
            {
                PFSKeyword key = control_strategy.InsertNewKeyword("data", i+1);
                key.InsertNewParameterDouble(normalstr.Str_QHrelation[i][1], 1);
                key.InsertNewParameterDouble(normalstr.Str_QHrelation[i][0], 2);
            }

        }

        //插入关键字参数
        public static void InsertKeyPars(ref PFSKeyword key, params object[] valuearray)
        {
            for (int i = 0; i < valuearray.Length; i++)
            {
                if (valuearray[i] is String)
                {
                    string value = valuearray[i].ToString();
                    key.InsertNewParameterString(value, i + 1);
                }
                else if (valuearray[i] is Filestring)
                {
                    string value = ((Filestring)valuearray[i]).filename;
                    key.InsertNewParameterFileName(value, i + 1);
                }
                else if (valuearray[i] is double)
                {
                    double value = double.Parse(valuearray[i].ToString());
                    key.InsertNewParameterDouble(value, i + 1);
                }
                else if (valuearray[i] is int)
                {
                    int value = int.Parse(valuearray[i].ToString());
                    key.InsertNewParameterInt(value, i + 1);
                }
                else if (valuearray[i] is bool)
                {
                    bool value = bool.Parse(valuearray[i].ToString());
                    key.InsertNewParameterBool(value, i + 1);
                }
            }
        }

        //插入关键字参数,计算关键字序号
        public static void InsertKeyPars(ref PFSKeyword key, ref int keynumber, params object[] valuearray)
        {
            keynumber++;
            for (int i = 0; i < valuearray.Length; i++)
            {
                if (valuearray[i] is String)
                {
                    string value = valuearray[i].ToString();
                    key.InsertNewParameterString(value, i + 1);
                }
                else if (valuearray[i] is Filestring)
                {
                    string value = ((Filestring)valuearray[i]).filename;
                    key.InsertNewParameterFileName(value, i + 1);
                }
                else if (valuearray[i] is double)
                {
                    double value = double.Parse(valuearray[i].ToString());
                    key.InsertNewParameterDouble(value, i + 1);
                }
                else if (valuearray[i] is int)
                {
                    int value = int.Parse(valuearray[i].ToString());
                    key.InsertNewParameterInt(value, i + 1);
                }
                else if (valuearray[i] is bool)
                {
                    bool value = bool.Parse(valuearray[i].ToString());
                    key.InsertNewParameterBool(value, i + 1);
                }
            }
        }
        #endregion ******************************************************************************
    }
}
