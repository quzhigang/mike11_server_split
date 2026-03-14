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
        #region ***********************闸门操作 -- 改变可控建筑物的调度方式******************************
        //将全部闸门改为规则调度
        public static void Changeddgz_AllToGZDU(ref HydroModel hydromodel)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            for (int i = 0; i < controllist.GateListInfo.Count; i++)
            {
                string strname = controllist.GateListInfo[i].Strname;
                Changeddgz_ToGZDU(ref hydromodel, strname);
            }
        }

        //将指定闸门改为全开的闸门调度
        public static void Changeddgz_ToZMDU_FullOpen(ref HydroModel hydromodel, string strname)
        {
            //闸门类型设置
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            Controlstr con_str = controllist.GetGate(strname);

            //如果闸门为流量类型，更新该闸门的基本信息
            if (con_str.Stratts.gate_type == 2)
            {
                Struct_BasePars base_par = Item_Info.Get_StrBaseInfo(strname);
                Attributes source_stratts = new Attributes();
                source_stratts.gate_count = base_par.gate_n;
                source_stratts.gate_width = base_par.gate_b;
                source_stratts.gate_type = 4;
                source_stratts.initial_value = base_par.datumn;
                source_stratts.max_speed = Model_Const.FLATGATE_MAXSPEED;
                source_stratts.max_value = base_par.datumn + base_par.gate_h;
                source_stratts.gate_height = base_par.datumn + base_par.gate_h;
                source_stratts.sill_level = base_par.datumn;
                source_stratts.bool_initial_value = true;
                source_stratts.bool_max_value = true;

                con_str.Stratts = source_stratts;
            }

            ZMDUinfo ddparams_zmdu;
            ddparams_zmdu.fullyopen = true;
            ddparams_zmdu.opengateheight = con_str.Stratts.gate_height;
            ddparams_zmdu.opengatenumber = con_str.Stratts.gate_count;
            Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        //将指定闸门改为全关的闸门调度
        public static void Changeddgz_ToZMDU_FullClose(ref HydroModel hydromodel, string strname)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            Controlstr con_str = controllist.GetGate(strname);

            //如果闸门为流量类型，更新该闸门的基本信息
            if(con_str.Stratts.gate_type == 2)
            {
                Struct_BasePars base_par = Item_Info.Get_StrBaseInfo(strname);
                Attributes source_stratts = new Attributes();
                source_stratts.gate_count = base_par.gate_n;
                source_stratts.gate_width = base_par.gate_b;
                source_stratts.gate_type = 4;
                source_stratts.initial_value = base_par.datumn;
                source_stratts.max_speed = Model_Const.FLATGATE_MAXSPEED;
                source_stratts.max_value = base_par.datumn + base_par.gate_h;
                source_stratts.gate_height = base_par.datumn + base_par.gate_h;
                source_stratts.sill_level = base_par.datumn;
                source_stratts.bool_initial_value = true;
                source_stratts.bool_max_value = true;

                con_str.Stratts = source_stratts;
            }

            ZMDUinfo ddparams_zmdu;
            ddparams_zmdu.fullyopen = false;
            ddparams_zmdu.opengateheight = con_str.Stratts.sill_level;
            ddparams_zmdu.opengatenumber = 0;
            Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        //将指定闸门改为一定开度的闸门调度
        public static void Changeddgz_ToZMDU_SetH(ref HydroModel hydromodel,string strname,GateType gate_type,double kd,int openn = -1)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            Attributes gateattr = controllist.GetGate(strname).Stratts;
            ZMDUinfo ddparams_zmdu;
            ddparams_zmdu.fullyopen = false;

            //不让设置开度大于闸门最大开度
            ddparams_zmdu.opengateheight = gate_type == GateType.PBZ? Math.Min(gateattr.max_value,gateattr.sill_level + kd): 
                                                              Math.Max(gateattr.max_value - kd, gateattr.sill_level);
            if (ddparams_zmdu.opengateheight < gateattr.sill_level) ddparams_zmdu.opengateheight = gateattr.sill_level;
            if (openn == -1 || openn >= gateattr.gate_count)
            {
                ddparams_zmdu.opengatenumber = gateattr.gate_count;
            }
            else
            {
                ddparams_zmdu.opengatenumber = openn;
            }
            Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        //在原溢流闸门处增加一个新的溢流闸门，闸门数量为未打开闸门数
        public static void Add_WeirGate_SyGaten(ref HydroModel hydromodel,string strname,int openn)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            Controlstr gate = controllist.GetGate(strname);
            Attributes attrs = gate.Stratts;
            AtReach atreach = gate.Reachinfo;

            //创建闸门新对象 
            string new_strname = strname + "(copy)";
            Attributes new_attrs = attrs;
            new_attrs.gate_count = Math.Max(1, attrs.gate_count - openn);
            Normalstr normalstr = new Normalstr(new_strname, atreach, new_attrs);

            //添加到集合里
            Add_Normalstr(ref hydromodel, ref normalstr);

            //设置为全关
            Changeddgz_ToZMDU_FullClose(ref hydromodel, new_strname);
        }
        
        //在指定位置增加一个全关的拦河堰(宽度5m)
        public static void Add_WeirGate_AtReach(ref HydroModel hydromodel, string strname, AtReach atreach)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取该位置河道断面底高程和河底宽度
            ReachSection_Altitude section_level = hydromodel.Mike11Pars.SectionList.Get_Section_Altitude(atreach);
            double qd_level = section_level.section_lowest;
            double dd_level = (section_level.left_dd_altitude + section_level.right_dd_altitude) * 0.5;

            //创建闸门新对象 
            Attributes new_attrs = new Attributes(0, 1, 0.63, 5, qd_level, 0.01, true, true, qd_level, dd_level, dd_level + 1.0);
            Normalstr normalstr = new Normalstr(strname, atreach, new_attrs);

            //添加到集合里
            Add_Normalstr(ref hydromodel, ref normalstr);

            //设置为全关
            Changeddgz_ToZMDU_FullClose(ref hydromodel, strname);
        }

        //将全部闸门改为全开的闸门调度
        public static void Changeddgz_AllToZMDU_FullOpen(ref HydroModel hydromodel)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            ZMDUinfo ddparams_zmdu;
            ddparams_zmdu.fullyopen = true;
            ddparams_zmdu.opengateheight = 10;
            ddparams_zmdu.opengatenumber = 1;
            for (int i = 0; i < controllist.GateListInfo.Count; i++)
            {
                string strname = controllist.GateListInfo[i].Strname;
                Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
            }
        }

        //将指定闸门 改为规则调度--闸门类型的全开   
        public static void Changeddgz_ToGZDU(ref HydroModel hydromodel, string strname)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //判断闸门类型，相应改变闸门调度方式
            Controlstr controlstr = controllist.GetGate(strname);
            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                normalstr.Strddgz = CtrDdtype.GZDU;
            }
            else
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                fhkstr.Strddgz = CtrDdtype.GZDU;
            }
        }

        //将指定闸门 改为控泄调度(等流量)
        public static void Changeddgz_ToKXDU(ref HydroModel hydromodel, string strname, double ddparams_double)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //判断闸门类型，相应改变闸门调度方式
            Controlstr controlstr = controllist.GetGate(strname);
            if (controlstr == null)
            {
                Console.WriteLine("建筑物未找到!");
                return;
            }

            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                normalstr.Strddgz = CtrDdtype.KXDU;

                //获取基本参数
                Struct_BasePars base_par = Item_Info.Get_StrBaseInfo(normalstr.Strname);

                //不让设置流量大于闸门设计流量
                normalstr.Ddparams_double = Math.Min(base_par.design_q,ddparams_double); 
                if (normalstr.Ddparams_double < 0) normalstr.Ddparams_double = 0;

                //水位流量关系(闸底高程为0，闸底高+闸门高为设计)
                List<double[]> str_qhrelation = new List<double[]>();
                str_qhrelation.Add(new double[]{0,base_par.datumn - 2});
                str_qhrelation.Add(new double[] {0,base_par.datumn});
                str_qhrelation.Add(new double[] {base_par.design_q,base_par.datumn + base_par.gate_h});
                str_qhrelation.Add(new double[] {base_par.design_q * 1.2,base_par.datumn + base_par.gate_h + 5});
                normalstr.Str_QHrelation = str_qhrelation;

                //闸门类型设置为流量
                Attributes stratts = normalstr.Stratts;
                stratts.gate_count = 1;
                stratts.gate_type = 2;
                stratts.initial_value = 0;
                stratts.max_speed = Model_Const.Q_MAXSPEED;
                stratts.max_value = Math.Min(base_par.design_q, ddparams_double);
                stratts.bool_initial_value = true;
                stratts.bool_max_value = true;

                normalstr.Stratts = stratts;
            }
            else
            {
                Console.WriteLine("溃堤建筑物不能进行控泄调度！");
                return;
            }
        }


        //将指定闸门 改为控泄调度(水位流量关系)
        public static void Changeddgz_ToKXDU(ref HydroModel hydromodel, string strname, double ddparams_double, List<double[]> str_qhrelation)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //判断闸门类型，相应改变闸门调度方式
            Controlstr controlstr = controllist.GetGate(strname);
            if (controlstr == null)
            {
                Console.WriteLine("建筑物未找到!");
                return;
            }

            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                normalstr.Strddgz = CtrDdtype.KXDU;

                //获取基本参数
                Struct_BasePars base_par = Item_Info.Get_StrBaseInfo(normalstr.Strname);

                //水位流量关系
                normalstr.Str_QHrelation = str_qhrelation;

                //闸门类型设置为流量
                Attributes stratts = normalstr.Stratts;
                stratts.gate_count = 1;
                stratts.gate_type = 2;
                stratts.initial_value = 0;
                stratts.max_speed = Model_Const.Q_MAXSPEED;
                stratts.max_value = Math.Min(base_par.design_q, ddparams_double);
                stratts.bool_initial_value = true;
                stratts.bool_max_value = true;

                normalstr.Stratts = stratts;
            }
            else
            {
                Console.WriteLine("溃堤建筑物不能进行控泄调度！");
                return;
            }
        }

        //将指定闸门 改为控泄时间调度
        public static void Changeddgz_ToKXDU_TIME(ref HydroModel hydromodel, string strname, Dictionary<DateTime, double> discharge_dic)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //判断闸门类型，相应改变闸门调度方式
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr == null)
            {
                Console.WriteLine("建筑物未找到!");
                return;
            }

            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                normalstr.Strddgz = CtrDdtype.KXDU_TIME;

                //获取基本参数
                Struct_BasePars base_par = Item_Info.Get_StrBaseInfo(normalstr.Strname);

                //闸门类型设置为流量
                Attributes stratts = normalstr.Stratts;
                stratts.gate_count = 1;
                stratts.gate_type = 2;
                stratts.initial_value = 0;
                stratts.max_speed = Model_Const.Q_MAXSPEED;
                stratts.max_value = base_par.design_q;
                stratts.bool_initial_value = true;
                stratts.bool_max_value = true;

                normalstr.Stratts = stratts;

                //创建控泄流量过程的dfs0文件
                string outputfilename = Path.GetDirectoryName(hydromodel.Modelfiles.Nwk11_filename) + @"\" + normalstr.Strname + "_KXDU.dfs0";
                Dfs0.Dfs0_Creat(outputfilename, discharge_dic, dfs0type.discharge);

                //创建控泄流量过程的dfs0文件
                normalstr.Ddparams_filename = outputfilename;
                normalstr.Ddparams_itemname = "discharge";
            }
            else
            {
                Console.WriteLine("溃堤建筑物不能进行控泄时间调度！");
                return;
            }
        }

        //将指定闸门 改为闸门调度   *****新建普通可控建筑物时闸门属性必须设置,否则这里就改了有问题!!!*****
        public static void Changeddgz_ToZMDU(ref HydroModel hydromodel, string strname, ZMDUinfo ddparams_zmdu)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //判断闸门类型，相应改变闸门调度方式
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr == null)
            {
                Console.WriteLine("建筑物未找到!");
                return;
            }

            if (controlstr is Normalstr)
            {
                //***注意，由于全局建筑物集合里都是对象，都是引用类型，故在这里接过来后也只是引用了内存地址，可以直接修改
                Normalstr normalstr = controlstr as Normalstr;   //这里用as也只是引用了地址，还是那个内存地址，没有开辟新内存
                normalstr.Strddgz = CtrDdtype.ZMDU;
                normalstr.Ddparams_zmdu = ddparams_zmdu;
            }
            else
            {
                Console.WriteLine("溃堤建筑物不能进行闸门调度！");
                return;
            }
        }

        //将指定闸门 改为闸门时间调度   *****新建普通可控建筑物时闸门属性必须设置,否则这里就改了有问题!!!*****
        public static void Changeddgz_ToZMDU_TIME(ref HydroModel hydromodel, string strname, Dictionary<DateTime, double> gateheight_dic)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //判断闸门类型，相应改变闸门调度方式
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr == null)
            {
                Console.WriteLine("建筑物未找到!");
                return;
            }

            //创建闸门开启高度过程dfs0文件
            string outputfilename = Path.GetDirectoryName(hydromodel.Modelfiles.Nwk11_filename) + @"\" + controlstr.Strname + "_ZMDU.dfs0";
            Dfs0.Dfs0_Creat(outputfilename, gateheight_dic, dfs0type.gatelevel);

            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                normalstr.Strddgz = CtrDdtype.ZMDU_TIME;
                normalstr.Ddparams_filename = outputfilename;
                normalstr.Ddparams_itemname = "gatelevel";
            }
            else
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                fhkstr.Strddgz = CtrDdtype.ZMDU_TIME;
                fhkstr.time_filename = outputfilename;
                fhkstr.time_itemname = "gatelevel";
            }
        }

        //按时间调度闸门 (开闸关闸所需时间按默认)
        public static void Changeddgz_ToZLDD_TIME(ref HydroModel hydromodel, string strname, List<DdInfo> gate_dds)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //找到闸门
            Controlstr controlstr = controllist.GetGate(strname);
            if (controlstr == null) return;

            //获取该闸门的基本信息
            Struct_BasePars str_basepar = Item_Info.Get_StrBaseInfo(strname);

            //闸门操作时间
            double operate_time = Get_GateOperatetime(strname);

            //先对闸门参数进行处理，因为传过来的是开度，而非闸门底高程
            for (int i = 0; i < gate_dds.Count; i++)
            {
                if (gate_dds[i].dd_type == "半开")
                {
                    Attributes gateattr = controlstr.Stratts;
                    double gate_level = str_basepar.gate_type == GateType.PBZ ? Math.Min(gateattr.max_value, gateattr.sill_level + gate_dds[i].dd_value) :
                                                  Math.Max(gateattr.max_value - gate_dds[i].dd_value, gateattr.sill_level);
                    if (gate_level < gateattr.sill_level) gate_level = gateattr.sill_level;

                    //重新给闸门调度值赋值为闸门底高程
                    gate_dds[i].dd_value = gate_level;
                }
            }

            //分闸门类型分别进行操作
            if (str_basepar.gate_type == GateType.PBZ || str_basepar.gate_type == GateType.YLZ)  
            {
                //生成非流量类型的闸门调度时间序列
                if(gate_dds[0].dd_type == "控泄")
                {
                    Dictionary<DateTime, double> gate_dddic = Create_GateDD_TimeDic(hydromodel, str_basepar, gate_dds, operate_time,true);
                    Changeddgz_ToKXDU_TIME(ref hydromodel, strname, gate_dddic);
                }
                else
                {
                    Dictionary<DateTime, double> gate_dddic = Create_GateDD_TimeDic(hydromodel, str_basepar, gate_dds, operate_time,false);
                    Changeddgz_ToZMDU_TIME(ref hydromodel, strname, gate_dddic);
                }
            }
            else   //如果为流量类型闸门(支持全开、全关、控泄)
            {
                //流量类型闸门不支持设置闸门开度
                for (int i = 0; i < gate_dds.Count; i++)
                {
                    if (gate_dds[i].dd_type == "半开") return;
                }

                //生成流量类型的闸门调度时间序列
                Dictionary<DateTime, double> gate_dddic = Create_GateDD_TimeDic(hydromodel, str_basepar, gate_dds, operate_time,true);
                Changeddgz_ToKXDU_TIME(ref hydromodel, strname, gate_dddic);
            }
        }

        //非流量类型的闸门，生成调度时间序列,不管指定多少孔均默认为全部孔
        private static Dictionary<DateTime,double> Create_GateDD_TimeDic(HydroModel hydromodel,Struct_BasePars str_basepar,
            List<DdInfo> gate_dds,double operate_time,bool as_discharge = false)
        {
            double max_value;
            double min_value; 
            if (!as_discharge)
            {
                max_value = str_basepar.datumn + str_basepar.gate_h;
                min_value = str_basepar.datumn;
            }
            else
            {
                max_value = str_basepar.design_q;
                min_value = 0;
            }

            //开始时刻和结束时刻闸门状态,初始时刻调度按与第一次相反(第一次全开、半开、控泄流量则初始为全关；第一次为全关，则初始为全开)
            double starttime_gatevalue;
            Str_DdType dd_type = Str_DdInfo.Get_DdType(gate_dds.First().dd_type);
            if (dd_type == Str_DdType.Open || dd_type == Str_DdType.Set_H || dd_type == Str_DdType.Set_Q)
            {
                starttime_gatevalue = min_value;
            }
            else
            {
                starttime_gatevalue = max_value;
            }

            //生成时间序列
            Dictionary<DateTime, double> gatedd_dic = new Dictionary<DateTime, double>();
            gatedd_dic.Add(hydromodel.ModelGlobalPars.Simulate_time.Begin, starttime_gatevalue);

            double now_gate_value = starttime_gatevalue;
            double next_gate_value = starttime_gatevalue;
            for (int i = 0; i < gate_dds.Count; i++)
            {
                //操作开始时的闸门高度或流量
                if (i == 0)
                {
                    now_gate_value = starttime_gatevalue;
                }
                else
                {
                    DateTime now_ddtime = SimulateTime.StrToTime(gate_dds[i].dd_time);
                    DateTime pre_ddtime = SimulateTime.StrToTime(gate_dds[i-1].dd_time);
                    if (now_ddtime.Subtract(pre_ddtime).TotalMinutes < 0.0) break;

                    now_gate_value = next_gate_value;
                }

                Str_DdType dd_type1 = Str_DdInfo.Get_DdType(gate_dds[i].dd_type);

                //操作完成后的闸门高度或流量
                if (dd_type1 == Str_DdType.Open)
                {
                    next_gate_value = max_value;
                }
                else if(dd_type1 == Str_DdType.Close)
                {
                    next_gate_value = min_value;
                }
                else
                {
                    next_gate_value = Math.Min(gate_dds[i].dd_value,max_value);
                }

                //开始时刻与模拟开始时刻相同
                DateTime dd_time = SimulateTime.StrToTime(gate_dds[i].dd_time);
                if (dd_time == hydromodel.ModelGlobalPars.Simulate_time.Begin ||
                    dd_time == hydromodel.ModelGlobalPars.Simulate_time.End)
                {
                    double operate_time1 = as_discharge ? 1 : operate_time; //如果是流量类型的，则操作时间按1分钟考虑，而不是20分钟
                    gatedd_dic.Add(dd_time.AddMinutes(operate_time1), next_gate_value);
                }
                else
                {
                    double operate_time1 = as_discharge ? 1 : operate_time; 
                    gatedd_dic.Add(dd_time, now_gate_value);
                    gatedd_dic.Add(dd_time.AddMinutes(operate_time1), next_gate_value);
                }
            }

            //增加一个结尾时刻,结尾时刻调度按最后一次调度
            double to_lasttime = gatedd_dic.Last().Key.Subtract(hydromodel.ModelGlobalPars.Simulate_time.End).TotalMinutes;
            if (to_lasttime < 0)
            {
                gatedd_dic.Add(hydromodel.ModelGlobalPars.Simulate_time.End, gatedd_dic.Last().Value);
            }

            return gatedd_dic;
        }


        //获取闸门调度时间
        private static double Get_GateOperatetime(string strname)
        {
            double operate_time;
            if (strname.EndsWith(GateType.YLZ.ToString()))
            {
                operate_time = Model_Const.JZZ_OCTIME;
            }
            else if (strname.EndsWith(GateType.PBZ.ToString()))
            {
                operate_time = Model_Const.TSZ_OCTIME;
            }
            else if (strname.EndsWith(GateType.LLZ.ToString()))
            {
                operate_time = Model_Const.FSZ_OCTIME;
            }
            else
            {
                operate_time = 20.0;
            }

            return operate_time;
        }

        //统一通过这个函数来选择闸门调度、还是闸门时间调度
        public static void ZMDU_TIME_Select(ref HydroModel hydromodel, string strname, Dictionary<DateTime, double> gateheight_dic, Dictionary<DateTime, double> gatenumber_dic)
        {
            //获取变量的最大最小开启高度、开启数量
            double max_height = gateheight_dic.Values.Max();
            double min_height = gateheight_dic.Values.Min();
            int max_number = (int)(gatenumber_dic.Values.Max());
            int min_number = (int)(gatenumber_dic.Values.Min());

            //找到该闸门
            Controlstr str = hydromodel.Mike11Pars.ControlstrList.GetGate(strname);

            //开度、开启闸门数量全部相同,则采用闸门调度
            if (max_height == min_height && max_number == min_number)
            {
                ZMDUinfo ddparams_zmdu = ZMDUinfo.Get_ZMDUinfo(false, max_number, max_height);
                Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
            }
            else if (max_height == min_height)   //仅开度完全相同，闸门数量不同
            {
                //设置原闸门一个相应把原闸门一分为几，每个都用闸门时间调度  - 开度不变
                ZMDU_TIME_Divide(ref hydromodel, strname, max_height, gatenumber_dic);
            }
            else if (max_number == min_number)  //仅闸门数量完全相同,则更改闸门数量后再选用闸门时间调度类型
            {
                //改变闸门数量
                Attributes gate_attributes = str.Stratts;
                gate_attributes.gate_count = max_number;
                str.Stratts = gate_attributes;

                //改为闸门时间调度方式
                Changeddgz_ToZMDU_TIME(ref hydromodel, strname, gateheight_dic);
            }
            else   //都不同
            {
                //设置原闸门一个相应把原闸门一分为几，每个都用闸门时间调度 - 开度随时间变化
                ZMDU_TIME_Divide(ref hydromodel, strname, gateheight_dic, gatenumber_dic);
            }
        }

        //根据时间序列，相应把闸门一分为几，每个均采用闸门时间调度 -- 开度相同
        public static void ZMDU_TIME_Divide(ref HydroModel hydromodel, string strname, double max_height, Dictionary<DateTime, double> gatenumber_dic)
        {
            //闸门开启数量集合
            List<int> gate_open_number = new List<int>();
            gate_open_number.Add((int)gatenumber_dic.ElementAt(0).Value);
            for (int i = 0; i < gatenumber_dic.Count; i++)
            {
                int now_gatenumber = (int)gatenumber_dic.ElementAt(i).Value;
                if (!gate_open_number.Contains(now_gatenumber))
                {
                    gate_open_number.Add(now_gatenumber);
                }
            }

            //获取原闸门信息
            Normalstr normalstr = hydromodel.Mike11Pars.ControlstrList.GetGate(strname) as Normalstr;
            AtReach atreach = normalstr.Reachinfo;
            Attributes stratts = normalstr.Stratts;

            //将闸门分为相应数量.每个均采用闸门时间调度
            for (int i = 0; i < gate_open_number.Count; i++)
            {
                //增加闸门
                stratts.gate_count = gate_open_number[i];
                string newstr_name = strname + (i + 1).ToString();
                Normalstr new_str = new Normalstr(newstr_name, atreach, stratts);
                Add_Normalstr(ref hydromodel, ref new_str);

                //改变该闸门调度方式为闸门时间调度
                Dictionary<DateTime, double> new_dic = new Dictionary<DateTime, double>();
                for (int j = 0; j < gatenumber_dic.Count; j++)
                {
                    if (gatenumber_dic.ElementAt(j).Value == gate_open_number[i])  //这个建筑物的时段闸门开度为正常高度
                    {
                        new_dic.Add(gatenumber_dic.ElementAt(j).Key, max_height);
                    }
                    else   //非这个建筑物的时段闸门开度为0
                    {
                        new_dic.Add(gatenumber_dic.ElementAt(j).Key, 0.0);
                    }
                }

                //改为闸门时间调度
                Changeddgz_ToZMDU_TIME(ref hydromodel, newstr_name, new_dic);
            }

            //移除原闸门
            hydromodel.Mike11Pars.ControlstrList.RemoveGate(strname);
        }

        //根据时间序列，相应把闸门一分为几，每个均采用闸门时间调度 -- 开度为时间序列
        public static void ZMDU_TIME_Divide(ref HydroModel hydromodel, string strname, Dictionary<DateTime, double> gateheight_dic, Dictionary<DateTime, double> gatenumber_dic)
        {
            //闸门开启数量集合
            List<int> gate_open_number = new List<int>();
            gate_open_number.Add((int)gatenumber_dic.ElementAt(0).Value);
            for (int i = 0; i < gatenumber_dic.Count; i++)
            {
                int now_gatenumber = (int)gatenumber_dic.ElementAt(i).Value;
                if (!gate_open_number.Contains(now_gatenumber))
                {
                    gate_open_number.Add(now_gatenumber);
                }
            }

            //获取原闸门信息
            Normalstr normalstr = hydromodel.Mike11Pars.ControlstrList.GetGate(strname) as Normalstr;
            AtReach atreach = normalstr.Reachinfo;
            Attributes stratts = normalstr.Stratts;

            //将闸门分为相应数量.每个均采用闸门时间调度
            for (int i = 0; i < gate_open_number.Count; i++)
            {
                //增加闸门
                stratts.gate_count = gate_open_number[i];
                string newstr_name = strname + (i + 1).ToString();
                Normalstr new_str = new Normalstr(newstr_name, atreach, stratts);
                Add_Normalstr(ref hydromodel, ref new_str);

                //改变该闸门调度方式为闸门时间调度
                Dictionary<DateTime, double> new_dic = new Dictionary<DateTime, double>();
                for (int j = 0; j < gatenumber_dic.Count; j++)
                {
                    if (gatenumber_dic.ElementAt(j).Value == gate_open_number[i])  //这个建筑物的时段闸门开度为正常高度
                    {
                        new_dic.Add(gatenumber_dic.ElementAt(j).Key, gateheight_dic.ElementAt(j).Value); //两个键值对集合时间序列必须相同
                    }
                    else   //非这个建筑物的时段闸门开度为0
                    {
                        new_dic.Add(gatenumber_dic.ElementAt(j).Key, 0.0);
                    }
                }

                //改为闸门时间调度
                Changeddgz_ToZMDU_TIME(ref hydromodel, newstr_name, new_dic);
            }

            //移除原闸门
            hydromodel.Mike11Pars.ControlstrList.RemoveGate(strname);
        }

        #endregion *************************************************************************************************
    }
}
