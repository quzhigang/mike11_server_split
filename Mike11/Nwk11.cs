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
    //建筑物基本参数
    [Serializable]
    public class Struct_BasePars
    {
        public string str_name;     //建筑物英文编号
        public string cn_name;     //建筑物中文名称
        public GateType gate_type;  //过流闸门类型
        public string str_type;    //水工建筑类型
        public string reach_name;  //河道ID
        public string reach_cnname;  //河道中文名
        public double chainage;     //桩号
        public double design_q;     //设计流量
        public double datumn;       //闸底高程
        public int gate_n;          //闸门数量
        public double gate_b;       //单孔净宽
        public double gate_h;       //闸门高度
        public double gate_height;  //闸门顶高程
        public string ddrule_info;   //规则调度信息
        public string control_str;   //是否洪水调度节点控制闸门,>=1为，并按重要性排序
        public List<string> model_instances; //所属模型实例
        public string struct_stcd;    //该闸门所属建筑stcd   

        public Struct_BasePars()
        {
            this.str_name = "";
            this.cn_name = "";
            this.gate_type = GateType.YLZ;
            this.str_type = "";
            this.reach_name = "";
            this.reach_cnname = "";
            this.chainage = 0;
            this.design_q = 0;
            this.datumn = 0;
            this.gate_n = 1;
            this.gate_b = 0;
            this.gate_h = 0;
            this.gate_height = 0;
            this.ddrule_info = "";
            this.model_instances = new List<string>();
            this.struct_stcd = "";
        }

        public Struct_BasePars(string str_name, string cn_name, GateType gate_type,string str_type, string reach_name,string reach_cnname,
            double chainage, double design_q, double datumn, int gate_n, double gate_b,double gate_h,double gate_height,
            string ddrule, string control_str,string stcd, List<string> model_instances)
        {
            this.str_name = str_name;
            this.cn_name = cn_name;
            this.gate_type = gate_type;
            this.str_type = str_type;
            this.reach_name = reach_name;
            this.reach_cnname = reach_cnname;
            this.chainage = chainage;
            this.design_q = design_q;
            this.datumn = datumn;
            this.gate_n = gate_n;
            this.gate_b = gate_b;
            this.gate_h = gate_h;
            this.gate_height = gate_height;
            this.ddrule_info = ddrule;
            this.control_str = control_str;
            this.struct_stcd = stcd;
            this.model_instances = model_instances;
        }

        //获取建筑物中文名称
        public static string Get_StrCNType(GateType gate_type)
        {
            string cn_name = "";
            switch (gate_type)
            {
                case GateType.YLZ:
                    cn_name = "节制闸";
                    break;
                case GateType.PBZ:
                    cn_name = "退水闸";
                    break;
                case GateType.LLZ:
                    cn_name = "分水闸";
                    break;
                case GateType.BZ:
                    cn_name = "泵站";
                    break;
                default:
                    break;
            }
            return cn_name;
        }

        //判断建筑物是否有可控制的溢洪道
        public static bool Have_GateYhd(List<Struct_BasePars> res_strlist)
        {
            bool resyhd_havegate = false;
            for (int j = 0; j < res_strlist.Count; j++)
            {
                if (res_strlist[j].str_type == "溢洪道")
                {
                    if (res_strlist[j].gate_type != GateType.NOGATE)
                    {
                        resyhd_havegate = true; break;
                    }
                }
            }
            return resyhd_havegate;
        }
    }

    //河道基本参数
    [Serializable]
    public class Reach_BasePars
    {
        public string name;
        public int id;
        public string reach_cnname;
        public string start_str;
        public string end_str;
        public double length;

        public Reach_BasePars(string name, int id, string reach_cnname, string start_str, string end_str, double length)
        {
            this.name = name;
            this.id = id;
            this.reach_cnname = reach_cnname;
            this.start_str = start_str;
            this.end_str = end_str;
            this.length = length;
        }
    }

    //RR产汇流域信息结构体
    [Serializable]
    public struct Catchment_Connectinfo
    {
        public string catchment_name;
        public double catchment_area;
        public Reach_Segment connect_reach;
    }

    //河道连接类型
    [Serializable]
    public enum ConnectType
    {
        Upconnect,      //上游连接
        Downconncet,   //下游连接
        Allconnect,     //两头连接
        Noneconnect    //两头都不连
    }

    //河网文件中河道控制点信息结构体
    [Serializable]
    public struct ReachPoint
    {
        public int number;
        public double X;
        public double Y;
        public double pointchainage;
        public int chainagetype;   //0为系统定义，1为自定义

        public static ReachPoint Get_reachpoint(int number, double X, double Y, double pointchainage, int chainagetype)
        {
            ReachPoint res;
            res.number = number;
            res.X = X;
            res.Y = Y;
            res.pointchainage = pointchainage;
            res.chainagetype = chainagetype;
            return res;
        }
    }

    //几种常用的建筑物参数更新类型
    [Serializable]
    public enum UpdateType
    {
        Chainage_Change,   //所在桩号位置改变
        Attri_Change,      //基本属性改变
        Radial_Change,     //弧门参数改变
        Formula_Change,    //公式参数改变
    }

    //插入几种常用的默认节类型
    [Serializable]
    public enum Default_SecName
    {
        ReservoirData,                           //水库
        HorizOffset_GateHeight_HeadLossFactors,  //平移、闸门高度、水头损失系统
        RadialGateParam_KissimeeGateParam,       //弧门和公式门参数
        controlpoint,                            //控制点
        Targetpoint,                             //目标点
        iterate_pid                              //迭代
    }

    //对应NEK11参数类型中的文件类型的字符串
    [Serializable]
    public struct Filestring
    {
        public string filename;
    }

    //河网文件操作指令枚举
    [Serializable]
    public enum OperateReach
    {
        Struct_ddchange = 0,    //改变水工建筑物的调度方式
        Struct_add,            //增加水工建筑物
        Struct_del,            //删除水工建筑物
        Reach_add,            //增加河段
        Cross_change         //河道断面修改
    }

    //各种建筑物调度方式
    [Serializable]
    public class Str_DdInfo
    {
        public string str_name;                //建筑物英文名
        public AtReach atreach;                //所在河道位置
        public Str_DdType str_ddtype;          //调度方式
        public int open_n;                     //开闸数量
        public double dd_value;                //调度量(闸门开度或流量)

        //构造
        public Str_DdInfo(string str_name, AtReach atreach, Str_DdType str_ddtype,int open_n = 0,double dd_value = 0)
        {
            this.atreach = atreach;
            this.str_name = str_name;
            this.str_ddtype = str_ddtype;
            this.open_n = open_n;
            this.dd_value = dd_value;
        }

        //改为全开
        public void Change_ToOpen()
        {
            this.str_ddtype = Str_DdType.Open;
        }

        //改为全关
        public void Change_ToClose()
        {
            this.str_ddtype = Str_DdType.Close;
            this.dd_value = 0;
        }

        //改为指定开度
        public void Change_ToSetH(double h_value)
        {
            this.str_ddtype = Str_DdType.Set_H;
            this.dd_value = h_value;
        }

        //改为指定流量
        public void Change_ToSetQ(double q_value)
        {
            this.str_ddtype = Str_DdType.Set_Q;
            this.dd_value = q_value;
        }

        //获取调度方式中文名
        public string Get_DdType_CNName()
        {
            string dd_cnname = "";
            switch (str_ddtype)
            {
                case Str_DdType.Open:
                    dd_cnname = "全开";
                    break;
                case Str_DdType.Close:
                    dd_cnname = "全关";
                    break;
                case Str_DdType.Set_H:
                    dd_cnname = "半开";
                    break;
                case Str_DdType.Set_Q:
                    dd_cnname = "控泄";
                    break;
                case Str_DdType.Rule:
                    dd_cnname = "规则";
                    break;
                default:
                    break;
            }

            return dd_cnname;
        }

        //获取调度方式枚举
        public static Str_DdType Get_DdType(string dd_cnname)
        {
            Str_DdType str_ddtype = Str_DdType.Open;

            if(dd_cnname == "全关") str_ddtype = Str_DdType.Close;
            if(dd_cnname == "半开") str_ddtype = Str_DdType.Set_H;
            if(dd_cnname == "控泄") str_ddtype = Str_DdType.Set_Q;
            if (dd_cnname == "规则" || dd_cnname == "规则调度") str_ddtype = Str_DdType.Rule;

            return str_ddtype;
        }

    }

    //各种建筑物调度方式
    [Serializable]
    public class DdInfo
    {
        public string dd_time;              //调度时间      
        public string dd_type;          //调度方式
        public int open_n;                     //开闸数量
        public double dd_value;                //调度量(闸门开度或流量)

        //构造
        public DdInfo()
        {
            this.dd_time = "";
            this.dd_type = "全开";
            this.open_n = 0;
            this.dd_value = 0;
        }

        public DdInfo(string dd_time, string dd_type, int open_n, double dd_value)
        {
            this.dd_time = dd_time;
            this.dd_type = dd_type;
            this.open_n = open_n;
            this.dd_value = dd_value;
        }
    }

    //建筑物调度方式枚举
    [Serializable]
    public enum Str_DdType
    {
        Open = 0,              //全开
        Close = 1,             //全关
        Set_H = 2,             //半开  （半开闸门）
        Set_Q = 3,             //控泄   (控泄流量)
        Rule = 4               //规则调度
    }

    //针对闸门调度的信息结构体
    [Serializable]
    public struct ZMDUinfo
    {
        public int opengatenumber;      //开闸数量
        public double opengateheight;   //开闸高度
        public bool fullyopen;          //是否全开

        public static ZMDUinfo Get_ZMDUinfo(bool fullyopen, int opengatenumber, double opengateheight)
        {
            ZMDUinfo zmduinfo;
            zmduinfo.fullyopen = fullyopen;
            zmduinfo.opengateheight = opengateheight;
            zmduinfo.opengatenumber = opengatenumber;
            return zmduinfo;
        }
    }

    //可控建筑物闸门枚举类型
    [Serializable]
    public enum CtrGateType
    {
        Overflow = 0,              //橡胶坝、溢流堰、分洪口
        Underflow = 1,             //涵洞
        Discharge = 2,             //泵站
        Radial_Gate = 3,           //常规弧门闸
        Plant_Gate = 4            //常规平板闸(用闸门公式计算)
    }

    //总干渠闸门分类
    [Serializable]
    public enum GateType
    {
        YLZ = 0,              //溢流闸
        PBZ = 1,             //平板闸
        LLZ = 2,             //流量闸
        BZ = 3,           //泵站
        NOGATE = 4        //无闸门 
    }

    //可控建筑物闸门的调度方式枚举
    [Serializable]
    public enum CtrDdtype
    {
        GZDU = 0,
        ZMDU,
        ZMDU_TIME,  //描述指令调度
        KXDU,
        KXDU_TIME
    }

    //闸门全开或全关
    [Serializable]
    public enum CtrOpenClose
    {
        AllOpen = 1,
        AllClose = 2
    }

    //建筑物所在河道基本信息结构体
    [Serializable]
    public struct AtReach
    {
        public string reachname;
        public string reachid;
        public double chainage;

        public static AtReach Get_Atreach(string reachname, double chainage)
        {
            AtReach atreach;
            atreach.reachname = reachname;
            atreach.chainage = chainage;
            atreach.reachid = reachname + Model_Const.REACHID_HZ;
            return atreach;
        }
    }

    //建筑物所在河道基本信息结构体
    [Serializable]
    public struct AtReach1
    {
        public string reachname;
        public double chainage;

        public static AtReach1 Get_Atreach(string reachname, double chainage)
        {
            AtReach1 atreach;
            atreach.reachname = reachname;
            atreach.chainage = chainage;
            return atreach;
        }
    }

    //河道的多个断面
    [Serializable]
    public class Reach_Sections
    {
        public string reach;
        public List<double> chainages;

        public Reach_Sections(string reachname, List<double> section_chainage)
        {
            this.reach = reachname;
            this.chainages = section_chainage;
        }
    }

    //河道详细信息结构体
    [Serializable]
    public struct ReachInfo
    {
        public string reachname;
        public string reachtopoid;
        public double start_chainage;
        public double end_chainage;
        public double dx;                          //河道计算步长

        public AtReach upstream_connect;           //上游连接河道
        public AtReach downstream_connect;         //下游连接河道

        public List<ReachPoint> reachpoint_list;   //河道控制点集合 

        public static string Zhstozh(double zhs)
        {
            //数据转桩号字符串
            string zh1 = Math.Truncate((zhs / 1000.0)).ToString();
            string zh2 = string.Format("{0:000}", (zhs - double.Parse(zh1) * 1000.0));
            string zh_str = zh1 + "+" + zh2;
            return zh_str;
        }
    }

    //供断面定义的河道信息结构体
    [Serializable]
    public struct Reach_Segment
    {
        public string reachname;
        public string reachtopoid;
        public double start_chainage;
        public double end_chainage;
        public static Reach_Segment Get_ReachSegment(string reachname, double start_chainage, double end_chainage)
        {
            Reach_Segment Reachsegment;
            Reachsegment.reachname = reachname;
            Reachsegment.start_chainage = start_chainage;
            Reachsegment.end_chainage = end_chainage;
            Reachsegment.reachtopoid = reachname + Model_Const.REACHID_HZ;
            return Reachsegment;
        }
    }

    //河堤溃口基本信息
    public class Reach_Break_BaseInfo
    {
        public string str_name;       //溃口建筑物id
        public string name;           //溃口名称
        public double[] location;     //溃口位置
        public double fh_width;       //溃口宽
        public double fh_minutes;       //溃堤时长(分)
        public string break_condition;   //溃决时机,河道水位达到最高水位"max_level"、指定河道水位"set_level"
        public double break_level;     //溃决水位，当break_condition为max_level时，break_level用任意值比如0，否则用指定值

        //二维需要的数据
        public double fh_datumn;        //溃口底高程
        public string fh_start_time;    //溃口开始时间
        public string fh_finish_time;   //溃口结束时间

        public Reach_Break_BaseInfo(string name, double[] location, double fh_width, double fh_minutes,
            string break_condition, double break_level)
        {
            this.str_name = "";
            this.name = name;
            this.location = location;
            this.fh_width = fh_width;
            this.fh_minutes = fh_minutes;
            this.break_condition = break_condition;
            this.break_level = break_level;

            this.fh_datumn = 0;
            this.fh_start_time = "";
            this.fh_finish_time = "";
        }

        //更新溃口设置信息
        public static void Update_Reach_BreakInfo(string plan_code,string model_instance, Reach_Break_BaseInfo reach_break)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            string break_info = File_Common.Serializer_Obj(reach_break);

            string sql = $"UPDATE {tableName} SET mike21_break_info=:Break_Info WHERE plan_code=:planCode AND model_instance=:modelInstance";

            KdbndpParameter[] mysqlPara =
            {
                new KdbndpParameter(":Break_Info", break_info),
                new KdbndpParameter(":planCode", plan_code),
                new KdbndpParameter(":modelInstance", model_instance)
            };
            Mysql.Execute_Command(sql, mysqlPara);
        }

        //获取溃口设置信息
        public static string Get_Reach_BreakInfo(string plan_code, string model_instance)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select mike21_break_info from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //溃坝信息
            string break_str = dt.Rows[0][0].ToString();
            

            return break_str;
        }

        //通过模型试算，获得指定断面最高水位
        public static double Get_Section_MaxLevel(HydroModel hydromodel,AtReach atreach)
        {
            double max_level = 0;
            hydromodel.Quick_Simulate();
            Dictionary<DateTime, double> level_dic = hydromodel.Get_Mike11Section_SingleRes(atreach, mike11_restype.Water_Level);
            if(level_dic != null)
            {
                if(level_dic.Count != 0)
                {
                    max_level = Math.Round(level_dic.Values.Max(), 1) - 0.1;
                }
            }

            return max_level;
        }

    }

    //mike21所需溃口信息
    [Serializable]
    public class Mike21_Reach_BreakInfo
    {
        public double[] location;       //溃口位置,经纬度
        public double fh_width;         //溃口宽
        public double fh_datumn;        //溃口底高程
        public string fh_start_time;    //溃口开始时间
        public string fh_finish_time;   //溃口结束时间

        public Mike21_Reach_BreakInfo(double[] location, double fh_width, double fh_datumn, 
            string fh_start_time, string fh_finish_time)
        {
            this.location = location;
            this.fh_width = fh_width;
            this.fh_datumn = fh_datumn;
            this.fh_start_time = fh_start_time;
            this.fh_finish_time = fh_finish_time;
        }
    }


    //构建溃堤建筑物（分洪口）信息的结构体
    [Serializable]
    public struct FhkstrInfo
    {
        public string strname;            //分洪口名
        public CtrDdtype strddgz;         //分洪口调度规则
        public AtReach atreach;           //所在河道、桩号信息
        public double width;              //分洪口宽度
        public double dm_level;         //分洪口处地面高程
        public double fhk_datumn;          //分洪口底高程，不应小于分洪口处地面高程
        public double dd_level;           //分洪口处堤顶高程
        public double fh_waterlevel;        //分洪水位
        public double break_seconds;   //溃决过程时长

        public string time_filename;             //当采用时间调度的时候需要的文件路径名
        public string time_itemname;             //相应的项目名
    }

    //水头损失系数结构体
    [Serializable]
    public struct Headloss
    {
        public double positive_inflow;          //顺流 进口水头损失系数因子
        public double positive_outflow;         //顺流 出口水头损失系数因子
        public double positive_freeoverflow;    //顺流 自由溢流水头损失系数因子

        public double negative_inflow;          //逆流 进口水头损失系数因子
        public double negative_outflow;         //逆流 出口水头损失系数因子
        public double negative_freeoverflow;    //逆流 自由溢流水头损失系数因子
    }

    //普通建筑物是正向的还是侧向的
    [Serializable]
    public enum Str_SideType
    {
        regular = 0,              //正向建筑物
        sidestr = 1                   //侧向建筑物
    }

    //普通闸门(除泵外)的5个必须参数
    [Serializable]
    public struct Necessary_Attrs
    {
        public CtrGateType gate_type;              //闸门类型
        public int gate_count;             //闸门数量
        public double gate_width;         //闸门宽度
        public double sill_level;         //闸底高程
        public double max_value;         //最门最大开启高程

        public static Necessary_Attrs Get_Gate_Necessary_Attrs(CtrGateType gate_type,
            int gate_count, double gate_width, double sill_level, double max_value)
        {
            Necessary_Attrs cecessary_attrs;
            cecessary_attrs.gate_type = gate_type;
            cecessary_attrs.gate_count = gate_count;
            cecessary_attrs.gate_width = gate_width;
            cecessary_attrs.sill_level = sill_level;
            cecessary_attrs.max_value = max_value;

            return cecessary_attrs;
        }
    }

    //可控建筑物闸门尺寸参数结构体
    [Serializable]
    public struct Attributes
    {
        public int gate_type;
        public int gate_count;
        public double under_flowcc;        //底流的收缩系数
        public double gate_width;         //闸门宽度
        public double sill_level;         //闸底高程
        public double max_speed;          //最大开闸速度
        public bool bool_initial_value;  //是否设置初始值
        public bool bool_max_value;      //是否设置最大限值
        public double initial_value;     //设置初始值
        public double max_value;         //设置最大限值
        public double gate_height;      //闸门高度

        public Attributes(int gate_type, int gate_count, double under_flowcc, double gate_width, 
            double sill_level, double max_speed, bool bool_initial_value, bool bool_max_value, 
            double initial_value, double max_value, double gate_height)
        {
            this.gate_type = gate_type;
            this.gate_count = gate_count;
            this.under_flowcc = under_flowcc;
            this.gate_width = gate_width;
            this.sill_level = sill_level;
            this.max_speed = max_speed;
            this.bool_initial_value = bool_initial_value;
            this.bool_max_value = bool_max_value;
            this.initial_value = initial_value;
            this.max_value = max_value;
            this.gate_height = gate_height;
        }
    }

    //弧形闸门参数结构体
    [Serializable]
    public struct RadiaParams
    {
        public double tune_factor;   //流量校准系数
        public double height;        //弧形闸门高度
        public double radius;        //弧形闸门半径
        public double trunnion;      //弧形轴承离闸底高度
        public double weir_coeff;    //堰的收缩系数
        public double weri_exp;      //堰的扩张系数
        public double tran_bottom;   //判断自由出流转换为过渡区的高程
        public double tran_depth;    //判断自由出流转换为过渡区的水深
    }

    //水闸公式参数结构体
    [Serializable]
    public struct FormulaParams
    {
        public double CS_highlimit;  //控制淹没流高限因子
        public double CS_lowlimit;   //控制淹没流低限因子
        public double CF_highlimit;  //控制自由流高限因子
        public double CF_lowlimit;   //控制自由流低限因子
        public double US_highlimit;  //无控淹没流高限因子
        public double US_lowlimit;   //无控淹没流低限因子

        public double CS_coef_a;
        public double CF_coef_a;
        public double US_coef_a;
        public double UF_coef_a;

        public double CS_exp_b;
        public double CF_exp_b;
        public double US_exp_b;
    }


    //用于编辑河网文件的类
    public class Nwk11
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
                Struct_BasePars gate_str = WG_INFO.Get_StrBaseInfo(strname);
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


        #region **********************闸门操作 -- 添加分洪口可控建筑物***********************************
        //根据分洪口位置和宽度 构建默认规则的分洪口信息 -- 断面数据通过数据库获取
        public static FhkstrInfo Get_Default_Fhkstrinfo(HydroModel hydromodel, AtReach at_reach, PointXY p, double fhk_width,double fh_level =0, double break_second = 0)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            double now_protectheight_leveltodd = hydromodel.Mike11Pars.Protectheight_leveltodd;

            //调用方法求点在河道的左岸还是右岸
            ReachLR point_atreachLR = Get_PointAtReachLR(hydromodel.Mike11Pars.ReachList, p, at_reach);

            //定义分洪口信息
            FhkstrInfo fhkstrinfo;

            //分洪口名字
            int nowfhkcount = 0;
            for (int i = 0; i < controllist.NewAdd_GateList.Count; i++)
            {
                if (controllist.NewAdd_GateList[i] is Fhkstr) nowfhkcount++;
            }

            string fhkname = nowfhkcount == 0? "KB": "KB" + (nowfhkcount + 1).ToString(); // 统一按这个名字 + 序号
            fhkstrinfo.strname = at_reach.reachname + "_" + fhkname;    //建筑物名等于河道名 + 分洪口名
            fhkstrinfo.strddgz = CtrDdtype.GZDU;
            fhkstrinfo.atreach = at_reach;
            fhkstrinfo.width = fhk_width;

            //从断面参数数据中查询横断面的堤顶地面高程，溃堤水位设为堤顶高程，溃口底高程为地面高程
            ReachSection_Altitude reachsection_altitude = hydromodel.Get_Altitude(at_reach);

            //溃堤水位设为堤顶高程，溃口底高程为地面高程
            switch (point_atreachLR)
            {
                case ReachLR.left:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                case ReachLR.right:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.right_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.right_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.right_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.right_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                default:   //如果点刚好在河道中心线上，也按左岸考虑
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
            }
            if (fh_level != 0) fhkstrinfo.fh_waterlevel = fh_level;
            fhkstrinfo.break_seconds = break_second ==0? Model_Const.FHK_INSTANTANEOUSBREAK_TIME: break_second;  //按30s瞬溃考虑
            fhkstrinfo.time_filename = "";
            fhkstrinfo.time_itemname = "";

            Console.WriteLine("分洪口信息构建成功!");
            return fhkstrinfo;
        }

        //根据分洪口位置和宽度 构建默认规则的分洪口信息 -- 断面数据通过数据库获取
        public static FhkstrInfo Get_Default_Fhkstrinfo(HydroModel hydromodel, PointXY p, double fhk_width, double fh_level = 0, double break_second = 0)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            double now_protectheight_leveltodd = hydromodel.Mike11Pars.Protectheight_leveltodd;

            AtReach at_reach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //调用方法求点在河道的左岸还是右岸
            ReachLR point_atreachLR = Get_PointAtReachLR(hydromodel.Mike11Pars.ReachList, p, at_reach);

            //定义分洪口信息
            FhkstrInfo fhkstrinfo;

            //分洪口名字
            int nowfhkcount = 0;
            for (int i = 0; i < controllist.NewAdd_GateList.Count; i++)
            {
                if (controllist.NewAdd_GateList[i] is Fhkstr) nowfhkcount++;
            }

            string fhkname = nowfhkcount == 0 ? "KB" : "KB" + (nowfhkcount + 1).ToString(); // 统一按这个名字 + 序号
            fhkstrinfo.strname = at_reach.reachname + "_" + fhkname;    //建筑物名等于河道名 + 分洪口名
            fhkstrinfo.strddgz = CtrDdtype.GZDU;
            fhkstrinfo.atreach = at_reach;
            fhkstrinfo.width = fhk_width;

            //从断面参数数据中查询横断面的堤顶地面高程，溃堤水位设为堤顶高程，溃口底高程为地面高程
            ReachSection_Altitude reachsection_altitude = hydromodel.Get_Altitude(at_reach);

            //溃堤水位设为堤顶高程，溃口底高程为地面高程
            switch (point_atreachLR)
            {
                case ReachLR.left:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                case ReachLR.right:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.right_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.right_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.right_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.right_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                default:   //如果点刚好在河道中心线上，也按左岸考虑
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
            }
            if (fh_level != 0) fhkstrinfo.fh_waterlevel = fh_level;
            fhkstrinfo.break_seconds = break_second == 0 ? Model_Const.FHK_INSTANTANEOUSBREAK_TIME : break_second;  //按30s瞬溃考虑
            fhkstrinfo.time_filename = "";
            fhkstrinfo.time_itemname = "";

            Console.WriteLine("分洪口信息构建成功!");
            return fhkstrinfo;
        }

        //在任意位置添加溃口可控建筑物(分洪口)
        public static string Add_Fhkstr(ref HydroModel hydromodel, ref FhkstrInfo fhkstrinfo)
        {
            //获取可控建筑物集合和河道集合
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //根据溃口建筑物信息新建可控建筑物对象
            Fhkstr fhkstr = new Fhkstr(fhkstrinfo);

            //先判断该桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
            List<double> chainagelist = reachlist.Get_ReachGridChainage(fhkstr.Reachinfo.reachname);

            //溃口的桩号可能会被修改
            bool Change = false;
            if (ChangeNearChainage(chainagelist, ref fhkstr) == 0)
            {
                Change = true;   //桩号被改动过
            }

            //全局建筑物集合中增加新的元素
            controllist.Addgate(fhkstr);

            //全局河道集合中，相应河道增加该建筑物桩号计算点
            reachlist.Add_ExistReach_GridChainage(fhkstr.Reachinfo.reachname, fhkstr.Reachinfo.chainage);

            return fhkstr.Strname;
        }

        //判断建筑物位置桩号是否离计算点桩号集合中的桩号太近，如果太近则移动,关联的参数类型必须相同，不能形参父类，实参子类
        public static int ChangeNearChainage(List<double> chainagelist, ref Fhkstr fhkstr)
        {
            //建筑物的桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (fhkstr.Reachinfo.chainage >= chainagelist[i] && fhkstr.Reachinfo.chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(fhkstr.Reachinfo.chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = fhkstr.Reachinfo.reachname;
                        reach.reachid = fhkstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        fhkstr.Reachinfo = reach;
                        return 0;
                    }
                    else if (Math.Abs(fhkstr.Reachinfo.chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = fhkstr.Reachinfo.reachname;
                        reach.reachid = fhkstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;

                        fhkstr.Reachinfo = reach;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        #endregion*********************************************************************************************


        #region ************************闸门操作 -- 添加普通可控建筑物*****************************************
        //新增 除泵站外 的普通可控建筑物 -- 除必要参数外，其他参数默认、规则调度(全开)
        public static string Add_Normalstr(ref HydroModel hydromodel, string gatename, PointXY p, Necessary_Attrs neces_attrs)
        {
            //调用方法求所在河道信息
            AtReach reachinfo = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //获取闸门所在位置河底高程，如果闸门底高程低于河底高程，则认为出错
            ReachSection_Altitude section_altitude = hydromodel.Get_Altitude(reachinfo);
            if (neces_attrs.sill_level < section_altitude.section_lowest)
            {
                Console.WriteLine("建筑物底高程低于河底高程，请检查断面数据和建筑物底高程数据！");
                return "";
            }

            //给新增的普通建筑物命名
            string strname = reachinfo.reachname + "_" + gatename;    //建筑物名等于河道名 + 闸门名

            //创建闸门新对象 
            Attributes gate_attrs = GetNew_Atts(neces_attrs);
            Normalstr normalstr = new Normalstr(strname, reachinfo, gate_attrs);   //根据 必须的5个参数构建普通建筑物

            //添加到集合里
            Add_Normalstr(ref hydromodel, ref normalstr);

            return strname;
        }

        //新增 泵站 普通可控建筑物 -- 除必要参数外，其他参数默认、规则调度(全开)
        public static string Add_Bumpstr(ref HydroModel hydromodel, string gatename, PointXY p, double design_discharge)
        {
            //调用方法求所在河道信息
            AtReach reachinfo = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //给新增的普通建筑物命名
            string strname = reachinfo.reachname + "_" + gatename;    //建筑物名等于河道名 + 闸门名

            //创建闸门新对象 
            Normalstr normalstr = new Normalstr(strname, reachinfo, CtrDdtype.KXDU, design_discharge);

            //添加到集合里
            Add_Normalstr(ref hydromodel, ref normalstr);

            return strname;
        }

        //添加普通可控建筑物到建筑物集合，更新模型
        public static void Add_Normalstr(ref HydroModel hydromodel, ref Normalstr normalstr)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //先判断该桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
            List<double> chainagelist = reachlist.Get_ReachGridChainage(normalstr.Reachinfo.reachname);

            //建筑物的桩号可能会被修改
            bool Change = false;
            if (ChangeNearChainage(chainagelist, ref normalstr) == 0)
            {
                Change = true;   //桩号被改动过
            }

            //全局建筑物集合中增加新的元素
            controllist.Addgate(normalstr);

            //全局河道集合中，相应河道增加该建筑物桩号计算点
            reachlist.Add_ExistReach_GridChainage(normalstr.Reachinfo.reachname, normalstr.Reachinfo.chainage);
        }

        //判断建筑物位置桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
        public static int ChangeNearChainage(List<double> chainagelist, ref Normalstr normalstr)
        {
            //建筑物的桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (normalstr.Reachinfo.chainage >= chainagelist[i] && normalstr.Reachinfo.chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(normalstr.Reachinfo.chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = normalstr.Reachinfo.reachname;
                        reach.reachid = normalstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        normalstr.Reachinfo = reach;
                        return 0;
                    }
                    else if (Math.Abs(normalstr.Reachinfo.chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = normalstr.Reachinfo.reachname;
                        reach.reachid = normalstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;

                        normalstr.Reachinfo = reach;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        //判断建筑物位置桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
        public static int ChangeNearChainage(List<double> chainagelist, ref Controlstr controlstr)
        {
            //建筑物的桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (controlstr.Reachinfo.chainage >= chainagelist[i] && controlstr.Reachinfo.chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(controlstr.Reachinfo.chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = controlstr.Reachinfo.reachname;
                        reach.reachid = controlstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        controlstr.Reachinfo = reach;
                        return 0;
                    }
                    else if (Math.Abs(controlstr.Reachinfo.chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = controlstr.Reachinfo.reachname;
                        reach.reachid = controlstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;

                        controlstr.Reachinfo = reach;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        #endregion *********************************************************************************************


        #region ****************************闸门操作 -- 改变建筑物位置、参数**************************************
        //根据前端给定的新点坐标 对闸门所在河道位置（河道、桩号）变化更新
        public static void Change_GateLocation(ref HydroModel hydromodel, string strname, PointXY newpoint)
        {
            //获取可控建筑物和河道集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //获取最近河道信息
            AtReach reach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, newpoint);
            controlstr.Reachinfo = reach;

            //如果新位置距离计算点太近，则修改建筑物的桩号
            List<double> chainagelist = reachlist.Get_ReachGridChainage(controlstr.Reachinfo.reachname);
            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref normalstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
            else
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref fhkstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
        }

        //根据前端给定的新点坐标 对闸门所在河道位置（河道、桩号）变化更新
        public static void Change_GateLocation(ref HydroModel hydromodel, string strname, AtReach atreach)
        {
            //获取可控建筑物和河道集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //获取最近河道信息
            controlstr.Reachinfo = atreach;

            //如果新位置距离计算点太近，则修改建筑物的桩号
            List<double> chainagelist = reachlist.Get_ReachGridChainage(controlstr.Reachinfo.reachname);
            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref normalstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
            else
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref fhkstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
        }


        //改变闸门基本属性
        public static void Change_GateAttributes(ref HydroModel hydromodel, string strname, Necessary_Attrs new_nes_attributes)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //根据5个必须参数创建闸门完整属性值
            Attributes newattributes = GetNew_Atts(new_nes_attributes);

            //更新闸门属性参数
            controlstr.Stratts = newattributes;
        }

        //改变弧形闸门参数
        public static void Change_GateRadiapars(ref HydroModel hydromodel, string strname, double radia_height)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //由闸底高程和弧形闸门高按规范推算弧形闸门的其他参数
            RadiaParams new_radiapars = GetNew_Radiaparams(controlstr.Stratts.sill_level, radia_height);

            //更新弧形闸门参数
            controlstr.Strradiapars = new_radiapars;
        }

        //改变公式闸门参数
        public static void Change_GateFormulapars(ref HydroModel hydromodel, string strname, double factor_a, double factor_b)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //通过因子相应调整公式闸门参数
            FormulaParams newformulapas = GetNew_Formulaparams(factor_a, factor_b);

            //更新弧形闸门参数
            controlstr.Strformulapars = newformulapas;
        }

        //根据闸门5个必须参数创建闸门属性值，其他参数取默认值
        public static Attributes GetNew_Atts(Necessary_Attrs neces_attrs)
        {
            //闸门基本属性更新
            Attributes newattribute;

            //需要修改的参数
            newattribute.gate_type = (int)neces_attrs.gate_type;    //闸门类型---目前只支持 弧形闸门、平板闸门 和 橡胶坝
            newattribute.gate_count = neces_attrs.gate_count;
            newattribute.gate_width = neces_attrs.gate_width;
            newattribute.sill_level = neces_attrs.sill_level;
            newattribute.max_value = neces_attrs.max_value;
            newattribute.gate_height = newattribute.max_value + 2;  //闸的最大高度(一般为堤顶高程)按闸门最大提升高度 +2m考虑

            //默认参数
            newattribute.under_flowcc = 0.63;
            newattribute.max_speed = Model_Const.FLATGATE_MAXSPEED;
            newattribute.bool_initial_value = true;
            newattribute.bool_max_value = true;
            newattribute.initial_value = newattribute.sill_level;

            return newattribute;
        }

        //由闸底高程和弧形闸门高按规范推算弧形闸门的其他参数
        public static RadiaParams GetNew_Radiaparams(double sill_level, double height)
        {
            //弧形闸门参数更新
            RadiaParams radiapars;

            //必须修改的参数
            radiapars.height = height;
            radiapars.radius = height * 1.2;
            radiapars.trunnion = height * 0.7;
            radiapars.tran_bottom = sill_level + radiapars.height / 3;

            //默认参数
            radiapars.tune_factor = 1;
            radiapars.weir_coeff = 1.838;
            radiapars.weri_exp = 1.5;
            radiapars.tran_depth = radiapars.height / 2;  //水深达到闸门高度一般时认为进入临界流

            return radiapars;
        }

        //根据因子相应扩大和缩小 公式闸门各参数
        public static FormulaParams GetNew_Formulaparams(double factor_a, double factor_b)
        {
            //首先得到默认值
            FormulaParams newformulapas = Controlstr.Getdefault_FormulaParams();

            //乘系数因子a
            newformulapas.CF_coef_a *= factor_a;
            newformulapas.CS_coef_a *= factor_a;
            newformulapas.UF_coef_a *= factor_a;
            newformulapas.US_coef_a *= factor_a;

            //乘指数因子b
            newformulapas.CF_exp_b *= factor_b;
            newformulapas.CS_exp_b *= factor_b;
            newformulapas.US_exp_b *= factor_b;

            return newformulapas;
        }
        #endregion ************************************************************************************************


        #region **********************闸门操作 -- 改变分洪口建筑物的特定参数 **************************************
        //改变分洪口的分洪水位为堤顶高程
        public static void Change_FhkWaterLevel_ToMax(ref HydroModel hydromodel, string strname)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr is Normalstr)
            {
                Console.WriteLine("该建筑物不是分洪口！");
                return;
            }

            Fhkstr fhkstr = controlstr as Fhkstr;

            //分洪水位恢复到堤顶高程
            fhkstr.fhwaterlevel = fhkstr.Stratts.max_value;
        }

        //改变分洪口的分洪水位为指定水位
        public static void Change_FhkWaterLevel_ToValue(ref HydroModel hydromodel, string strname, double new_waterlevel)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr is Normalstr)
            {
                Console.WriteLine("该建筑物不是分洪口！");
                return;
            }

            Fhkstr fhkstr = controlstr as Fhkstr;

            //否则按左右岸堤顶平均值考虑
            fhkstr.fhwaterlevel = new_waterlevel;
        }

        //改变分洪持续时间为指定时间
        public static void Change_FhkBreakTime(ref HydroModel hydromodel, string strname, TimeSpan new_breaktime)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr is Normalstr)
            {
                Console.WriteLine("该建筑物不是分洪口！");
                return;
            }

            Fhkstr fhkstr = controlstr as Fhkstr;
            Attributes fhk_atts = fhkstr.Stratts;
            double breaktime_seconds = new_breaktime.TotalSeconds;
            fhk_atts.max_speed = Math.Round((fhk_atts.max_value - fhkstr.fhklevel) / breaktime_seconds, 3);    //溃决速率 m/s
        }
        #endregion ************************************************************************************************


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
                Struct_BasePars base_par = WG_INFO.Get_StrBaseInfo(strname);
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
                Struct_BasePars base_par = WG_INFO.Get_StrBaseInfo(strname);
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
                Struct_BasePars base_par = WG_INFO.Get_StrBaseInfo(normalstr.Strname);

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
                Struct_BasePars base_par = WG_INFO.Get_StrBaseInfo(normalstr.Strname);

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
                Struct_BasePars base_par = WG_INFO.Get_StrBaseInfo(normalstr.Strname);

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
            Struct_BasePars str_basepar = WG_INFO.Get_StrBaseInfo(strname);

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


        #region ***************河道操作 -- 新增一维河道，根据控制点顺序设定上下游，会自动连接或不连*******************
        //增加新河道的第1步操作 --  任意方向和位置增加一条一维河道，并加入河道集合
        public static void Add_NewReach_Network(ref HydroModel hydromodel, ref string reachname, ref List<PointXY> reachpoints)
        {
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //调用方法获取新增的上下游河道详细信息
            ReachInfo newreach = Get_NewReachInfo(ref reachlist, ref reachpoints, ref reachname);

            //加入全局河道集合中
            reachlist.AddReach(newreach);
        }

        //创建新增的河道详细信息,所给的控制点顺序决定河道流向,河道可上游、下游与已有河道连接，也可不连，成为独立河道
        public static ReachInfo Get_NewReachInfo(ref ReachList reachlist, ref List<PointXY> reachpoints, ref string reachname)
        {
            //判断是否有相同位置的点，如果有，则x y +1
            ChangeNearPoint(reachlist, ref reachpoints);

            //创建一个河道详细信息变量
            ReachInfo newreach;

            //判断是否有同名河道，如果有自动改新河道名
            if (reachlist.Reach_baseinfolist != null)
            {
                for (int i = 0; i < reachlist.Reach_baseinfolist.Count; i++)
                {
                    if (reachname == reachlist.Reach_baseinfolist[i].reachname)
                    {
                        Console.WriteLine("有同名河道，自动改名,在河名后 + G");
                        reachname = reachname + "G";
                        break;
                    }
                }
            }

            newreach.reachname = reachname;
            newreach.reachtopoid = reachname + Model_Const.REACHID_HZ;     //按默认后缀取河道ID
            newreach.dx = Model_Const.MIKE11_DEFAULTDX;                //设置为默认步长(300m)

            //设置河道控制点属性
            List<ReachPoint> reachpoint_list = new List<ReachPoint>();

            int maxpointnumber = reachlist.Maxpointnumber;
            for (int i = 0; i < reachpoints.Count; i++)
            {
                //定义河道控制点
                ReachPoint reachpoint;
                reachpoint.number = maxpointnumber + i + 1;

                reachpoint.X = reachpoints[i].X;
                reachpoint.Y = reachpoints[i].Y;
                reachpoint.chainagetype = 1;

                //计算控制点桩号，默认起点桩号为0
                double chainage = 0;
                if (i != 0)
                {
                    double lastchainage = reachpoint_list[i - 1].pointchainage;
                    double lastX = reachpoint_list[i - 1].X;
                    double lastY = reachpoint_list[i - 1].Y;
                    double stance = Math.Round(Math.Sqrt((reachpoint.X - lastX) * (reachpoint.X - lastX) + (reachpoint.Y - lastY) * (reachpoint.Y - lastY)), 1);
                    chainage = lastchainage + stance;
                }
                reachpoint.pointchainage = chainage;

                reachpoint_list.Add(reachpoint);
            }

            //设置河道的起止桩号以及控制点集合
            newreach.reachpoint_list = reachpoint_list;
            newreach.start_chainage = newreach.reachpoint_list[0].pointchainage;
            newreach.end_chainage = newreach.reachpoint_list[newreach.reachpoint_list.Count - 1].pointchainage;

            //通过新河道起点和终点与最近河道的距离判断 是哪头连接或两头都连接 并输出上下游连接河道信息
            AtReach upreach;
            AtReach downreach;
            ConnectType reach_connecttype;
            GetConnect_ReachInfo(reachlist, reachpoints, out upreach, out downreach, out reach_connecttype);

            //连接桩号重新修改，防止距离过近
            if (upreach.reachname != "" &&
                upreach.chainage != reachlist.Get_Reachinfo(upreach.reachname).start_chainage &&
                upreach.chainage != reachlist.Get_Reachinfo(upreach.reachname).end_chainage)
            {
                List<double> chainagelist = reachlist.Get_ReachGridChainage(upreach.reachname);

                //连接点桩号可能会被修改
                double chainage = upreach.chainage;
                ChangeNearChainage(chainagelist, ref chainage);
                upreach.chainage = chainage;
            }

            if (downreach.reachname != "" &&
              downreach.chainage != reachlist.Get_Reachinfo(downreach.reachname).start_chainage &&
              downreach.chainage != reachlist.Get_Reachinfo(downreach.reachname).end_chainage)
            {
                List<double> chainagelist = reachlist.Get_ReachGridChainage(downreach.reachname);

                //连接点桩号可能会被修改
                double chainage = downreach.chainage;
                ChangeNearChainage(chainagelist, ref chainage);
                downreach.chainage = chainage;
            }

            newreach.upstream_connect = upreach;
            newreach.downstream_connect = downreach;

            //增加一个新的河道信息
            reachlist.AddReach(newreach);

            //更新河道计算点(**新河道计算点在河道断面文件操作中增加***)
            if (upreach.reachname != "")
            {
                reachlist.Add_ExistReach_GridChainage(upreach.reachname, upreach.chainage);
            }

            if (downreach.reachname != "")
            {
                reachlist.Add_ExistReach_GridChainage(downreach.reachname, downreach.chainage);
            }

            return newreach;
        }

        //判断新河道的点是否与原有河道具有相同的坐标，如果有则x y +1
        public static void ChangeNearPoint(ReachList reachlist, ref List<PointXY> reachpoints)
        {
            if (reachlist.Reach_infolist == null) return;

            //判断是否有相同位置的点，如果有，则x y +1
            int reachcount = reachlist.Reach_infolist.Count;

            for (int i = 0; i < reachpoints.Count; i++)
            {
                bool pointxychange = false;
                PointXY point;
                point.X = reachpoints[i].X;
                point.Y = reachpoints[i].Y;

                for (int j = 0; j < reachcount; j++)
                {
                    int reachpointcount = reachlist.Reach_infolist[j].reachpoint_list.Count;
                    for (int k = 0; k < reachpointcount; k++)
                    {
                        double X1 = reachlist.Reach_infolist[j].reachpoint_list[k].X;
                        double Y1 = reachlist.Reach_infolist[j].reachpoint_list[k].Y;
                        if (point.X == X1 && point.Y == Y1)
                        {
                            point.X += 1;
                            point.Y += 1;
                            pointxychange = true;
                            break;
                        }
                    }
                }

                if (pointxychange == true)
                {
                    reachpoints.RemoveAt(i);
                    reachpoints.Insert(i, point);
                }
            }
        }

        //判断所给桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
        public static int ChangeNearChainage(List<double> chainagelist, ref double chainage)
        {
            //桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (chainage >= chainagelist[i] && chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        return 0;
                    }
                    else if (Math.Abs(chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        //从已有河道中找出与新河道 起点或终点 最近的河道信息,作为新河道的上游或下游连接河道
        public static void GetConnect_ReachInfo(ReachList reachlist, List<PointXY> reachpoints, out AtReach upreach, out AtReach downreach, out ConnectType connecttype)
        {
            //先初始化赋值
            downreach.reachname = "";
            downreach.reachid = "";
            downreach.chainage = -1e-155;

            upreach.reachname = "";
            upreach.reachid = "";
            upreach.chainage = -1e-155;

            connecttype = ConnectType.Upconnect;  //连接类型

            //第一个点和最后一个点
            PointXY startpoint = reachpoints[0];
            PointXY endpoint = reachpoints[reachpoints.Count - 1];

            //找到离新河道第1个点最近的河道信息
            int min_reachnumber;
            double chainage;
            double min_distance;
            Get_ConnectReach(reachlist, startpoint, out min_reachnumber, out chainage, out min_distance);

            //判断是上游连接还是下游连接，并返回相应连接河道信息
            if (min_distance < Model_Const.MIKE11_MINCONNECTDISTANCE)  //上游连接
            {
                //重新设置上游连接河道信息
                upreach.reachname = reachlist.Reach_infolist[min_reachnumber].reachname;
                upreach.reachid = reachlist.Reach_infolist[min_reachnumber].reachtopoid;
                upreach.chainage = chainage;
            }

            //找到离新河道最后1个点最近的河道信息
            int min_reachnumber1;
            double chainage1;
            double min_distance1;
            Get_ConnectReach(reachlist, endpoint, out min_reachnumber1, out chainage1, out min_distance1);

            if (min_distance1 < Model_Const.MIKE11_MINCONNECTDISTANCE)  //下游连接
            {
                //重新设置下游连接河道信息
                downreach.reachname = reachlist.Reach_infolist[min_reachnumber1].reachname;
                downreach.reachid = reachlist.Reach_infolist[min_reachnumber1].reachtopoid;
                downreach.chainage = chainage1;
            }

            //判断新河道与上下游河道的连接方式
            if (upreach.reachname != "" && downreach.reachname == "")
            {
                connecttype = ConnectType.Upconnect;
            }
            else if (upreach.reachname == "" && downreach.reachname != "")
            {
                connecttype = ConnectType.Downconncet;
            }
            else if (upreach.reachname != "" && downreach.reachname != "")
            {
                connecttype = ConnectType.Allconnect;
            }
            else
            {
                connecttype = ConnectType.Noneconnect;
            }
        }

        //求距离给定点最近的河道名和内插桩号 -- 用于获取最近河道
        public static AtReach Get_NearReach(ReachList reachlist, PointXY p)
        {
            //调用方法求所在河道信息
            int min_reachnumber;
            double chainage;
            double min_distance;
            Nwk11.Get_ConnectReach(reachlist, p, out min_reachnumber, out chainage, out min_distance);

            AtReach reachinfo;
            reachinfo.reachname = reachlist.Reach_baseinfolist[min_reachnumber].reachname;
            reachinfo.reachid = reachlist.Reach_baseinfolist[min_reachnumber].reachtopoid;
            reachinfo.chainage = Math.Round(chainage,0);

            return reachinfo;
        }

        //求距离给定点最近的河道名和内插桩号 -- 用于获取最近河道，限定河道
        public static AtReach Get_NearReach(ReachList reachlist,string reachname, PointXY p)
        {
            //调用方法求所在河道信息
            double chainage;
            double min_distance;
            Nwk11.Get_NearstReach(reachlist, reachname, p, out chainage, out min_distance);

            AtReach reachinfo;
            reachinfo.reachname = reachname;
            reachinfo.reachid = reachname + Model_Const.REACHID_HZ;
            reachinfo.chainage = Math.Round(chainage, 0);

            return reachinfo;
        }

        //求距离给定点最近的河道横断面位置
        public static AtReach Get_NearReach_Sectionchainage(SectionList sectionlist, ReachList reachlist, PointXY p)
        {
            //先获取距离给定点最近的河道名和内插的桩号
            AtReach nearreach = Get_NearReach(reachlist, p);

            //根据河名获取河道断面桩号
            List<double> reach_sectionchainagelist = sectionlist.Get_Reach_SecChainageList(nearreach.reachname);

            //找到最近的断面桩号位置
            double min_distance = 10000;
            double min_distance_chainage = reach_sectionchainagelist[0];
            for (int i = 0; i < reach_sectionchainagelist.Count; i++)
            {
                if (Math.Abs(reach_sectionchainagelist[i] - nearreach.chainage) < min_distance)
                {
                    min_distance = Math.Abs(reach_sectionchainagelist[i] - nearreach.chainage);
                    min_distance_chainage = reach_sectionchainagelist[i];
                }
            }

            return AtReach.Get_Atreach(nearreach.reachname, min_distance_chainage);
        }

        //求距离给定点最近的河道名和内插桩号 -- 用于最近河道的连接
        public static int Get_ConnectReach(ReachList reachlist, PointXY p, out int min_reachnumber, out double chainage, out double min_distance)
        {
            min_reachnumber = 0;
            chainage = -1;
            min_distance = Model_Const.MIKE11_MAXPTOREACHSTANCE;
            if (reachlist.Reach_infolist == null) return -1;

            //定义存储各河道名河各节点距离指定点距离的嵌套键值对集合
            double mindistance = 2000;
            for (int i = 0; i < reachlist.Reach_infolist.Count; i++)
            {
                //遍历各河道
                int reachpointcount = reachlist.Reach_infolist[i].reachpoint_list.Count;

                //最后通过判断找出距离给定点最近的两个点和所在的河道，内插出桩号
                for (int j = 0; j < reachpointcount; j++)
                {
                    double X1 = reachlist.Reach_infolist[i].reachpoint_list[j].X;
                    double Y1 = reachlist.Reach_infolist[i].reachpoint_list[j].Y;

                    //所给的点恰好在控制点上
                    if (p.X == X1 && p.Y == Y1)
                    {
                        min_reachnumber = i;
                        chainage = reachlist.Reach_infolist[i].reachpoint_list[j].pointchainage;
                        min_distance = 0;
                        return 0;
                    }

                    //所给的点不在控制点上的情况
                    double distance = Math.Sqrt((X1 - p.X) * (X1 - p.X) + (Y1 - p.Y) * (Y1 - p.Y));
                    if (distance < mindistance)
                    {
                        mindistance = distance;
                        //重新给用于输出的河道名称、桩号赋值
                        min_reachnumber = i;
                        if (j == 0 || j == reachpointcount - 1)
                        {
                            //直接为起点或终点的桩号
                            chainage = reachlist.Reach_infolist[i].reachpoint_list[j].pointchainage;
                            min_distance = mindistance;
                        }
                        else
                        {
                            //通过垂足求最小距离和桩号
                            Get_CZdistance(reachlist.Reach_infolist[i],p, j, out min_distance,out chainage);
                        }
                    }
                }
            }
            return 0;
        }

        //求距离给定点最近的河道名和内插桩号 -- 限定河道
        public static int Get_NearstReach(ReachList reachlist, string reachname, PointXY p, out double chainage, out double min_distance)
        {
            chainage = -1;
            min_distance = 10000;
            if (reachlist.Reach_infolist == null) return -1;

            ReachInfo reach = reachlist.Get_Reachinfo(reachname);

            //定义存储各河道名河各节点距离指定点距离的嵌套键值对集合
            double mindistance = 10000;

            int reachpointcount = reach.reachpoint_list.Count;

            //最后通过判断找出距离给定点最近的两个点和所在的河道，内插出桩号
            for (int j = 0; j < reachpointcount; j++)
            {
                double X1 = reach.reachpoint_list[j].X;
                double Y1 = reach.reachpoint_list[j].Y;

                //所给的点恰好在控制点上
                if (p.X == X1 && p.Y == Y1)
                {
                    chainage = reach.reachpoint_list[j].pointchainage;
                    min_distance = 0;
                    return 0;
                }

                //所给的点不在控制点上的情况
                double distance = Math.Sqrt((X1 - p.X) * (X1 - p.X) + (Y1 - p.Y) * (Y1 - p.Y));
                if (distance < mindistance)
                {
                    mindistance = distance;
                    //重新给用于输出的河道桩号赋值
                    if (j == 0 || j == reachpointcount - 1)
                    {
                        //直接为起点或终点的桩号
                        chainage = reach.reachpoint_list[j].pointchainage;
                        min_distance = mindistance;
                    }
                    else
                    {
                       //通过垂足求最小距离和桩号
                       Get_CZdistance(reach, p, j, out min_distance,out chainage);
                    }
                }
            }
            return 0;
        }

        //通过垂足求最小距离和桩号
        private static void Get_CZdistance(ReachInfo reach, PointXY p, int j, out double min_distance, out double chainage)
        {
            //定义上游点和下游点
            PointXY uppoint;
            uppoint.X = 0;
            uppoint.Y = 0;

            PointXY downpoint;
            downpoint.X = 0;
            downpoint.Y = 0;

            //定义上游点坐标、当前点坐标和下游点点坐标
            double upx = reach.reachpoint_list[j - 1].X;
            double upy = reach.reachpoint_list[j - 1].Y;
            double downx = reach.reachpoint_list[j + 1].X;
            double downy = reach.reachpoint_list[j + 1].Y;
            double currentx = reach.reachpoint_list[j].X;
            double currenty = reach.reachpoint_list[j].Y;

            //求j-1点和J点线段的垂足，并判断垂足是否在线段上
            double k = (upy - currenty) / (upx - currentx); //直线斜率
            PointXY upp;
            upp.X = upx;
            upp.Y = upy;
            PointXY Projectpoint1 = PointXY.GetProjectivePoint(upp, k, p);  //垂足
            double updowndistance1 = PointXY.Get_ptop_distance(currentx, currenty, upp.X, upp.Y);
            double sumdistance1 = PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, upp.X, upp.Y) + PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, currentx, currenty);

            //求j点和J+1点线段的垂足，并判断垂足是否在线段上
            double k1 = (downy - currenty) / (downx - currentx); //直线斜率
            PointXY downp;
            downp.X = downx;
            downp.Y = downy;
            PointXY Projectpoint2 = PointXY.GetProjectivePoint(downp, k1, p);  //垂足
            double updowndistance2 = PointXY.Get_ptop_distance(currentx, currenty, downp.X, downp.Y);
            double sumdistance2 = PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, downp.X, downp.Y) + PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, currentx, currenty);

            //判断垂足是否在线段上，以及在哪个线段（上游线段 还是 下游线段）上
            double upchainage = 0;
            if (Math.Abs(sumdistance1 - updowndistance1) < 1) //垂足在j-1点和J点组成的线段之间 
            {
                //定义上游点为上一点，下游点为当前点
                uppoint.X = reach.reachpoint_list[j - 1].X;
                uppoint.Y = reach.reachpoint_list[j - 1].Y;
                upchainage = reach.reachpoint_list[j - 1].pointchainage;
                downpoint.X = reach.reachpoint_list[j].X;
                downpoint.Y = reach.reachpoint_list[j].Y;

                //求点到线段的垂直距离以及垂足桩号
                min_distance = PointXY.PToL_Distance(upx, upy, currentx, currenty, p.X, p.Y);
                chainage = upchainage + PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, uppoint.X, uppoint.Y);
            }
            else if (Math.Abs(sumdistance2 - updowndistance2) < 1)  //垂足在j点和J+1点组成的线段之间 
            {
                //定义上游点为当前点，下游点为下一点
                uppoint.X = reach.reachpoint_list[j].X;
                uppoint.Y = reach.reachpoint_list[j].Y;
                upchainage = reach.reachpoint_list[j].pointchainage;
                downpoint.X = reach.reachpoint_list[j + 1].X;
                downpoint.Y = reach.reachpoint_list[j + 1].Y;

                //求点到线段的垂直距离以及垂足桩号
                min_distance = PointXY.PToL_Distance(currentx, currenty, downx, downy, p.X, p.Y);
                chainage = upchainage + PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, uppoint.X, uppoint.Y);
            }
            else   //垂足不在前、后线段内
            {
                min_distance = PointXY.Get_ptop_distance(currentx, currenty, p.X, p.Y);
                chainage = reach.reachpoint_list[j].pointchainage;
            }
        }

        //判断点在河道左岸还是右岸
        public static ReachLR Get_PointAtReachLR(ReachList reachlist, PointXY point, AtReach reachinfo)
        {
            //获取河道位置最近的两个控制点的集合
            List<PointXY> reachpointlist = reachlist.Get_ReachNearPointxyList(reachinfo.reachname, reachinfo.chainage);

            //求该点与河道线段的夹角
            double angle = PointXY.GetAngle(reachpointlist[0], reachpointlist[reachpointlist.Count - 1], point);

            if (angle > 0) //在左岸
            {
                return ReachLR.left;
            }
            else if (angle < 0)
            {
                return ReachLR.right;
            }
            else  //在河道线上
            {
                return ReachLR.left_right;
            }
        }

        //通过GIS服务，用断面线DEM上剖切断面数据 -  新河道
        public static Dictionary<AtReach, List<PointXZS>> GetSectionData_FromGIS(HydroModel hydromodel, ReachInfo newreach)
        {
            //获取上下游连接河道的最大宽度
            double distance = 0;
            if (newreach.upstream_connect.reachname != "")
            {
                //获取上游连接河道的最大宽度 
                distance = hydromodel.Mike11Pars.SectionList.Get_ReachSection_Maxwidth(newreach.upstream_connect.reachname) / 2;
            }
            else if (newreach.downstream_connect.reachname != "")
            {
                //获取下游连接河道的最大宽度 
                distance = hydromodel.Mike11Pars.SectionList.Get_ReachSection_Maxwidth(newreach.downstream_connect.reachname) * 1.2 / 2;
            }
            else   //上下游都没有连接河道，则按默认剖切宽度 300m
            {
                distance = Model_Const.SECTION_MAXWIDTH;
            }

            //获取获取新河道断面线桩号坐标 
            Dictionary<AtReach, List<PointXY>> ReachSection_LRPointXY = GetReachSection_LRPointXY(newreach, distance, distance);

            //逐个断面通过gis服务获取断面高程点
            Dictionary<AtReach, List<PointXZS>> SectionData = new Dictionary<AtReach, List<PointXZS>>();
            for (int i = 0; i < ReachSection_LRPointXY.Count; i++)
            {
                List<PointXZS> section_points = Gis_Service.Get_Sectiondata_FromGisService(ReachSection_LRPointXY.Values.ElementAt(i));
                SectionData.Add(ReachSection_LRPointXY.Keys.ElementAt(i), section_points);
            }

            return SectionData;
        }

        //通过GIS服务，用断面线DEM上剖切断面数据 -  原有河道断面数据更新
        public static Dictionary<AtReach, List<PointXZS>> GetSectionData_FromGIS(HydroModel hydromodel, Reach_Segment reach_segment)
        {
            //先获取指定河段的断面数据
            Dictionary<AtReach, List<PointXZS>> ReachSectionDataList = hydromodel.Mike11Pars.SectionList.Get_SegReach_SecDataList(reach_segment);

            //获取原河道断面线桩号坐标 
            Dictionary<AtReach, List<PointXY>> ReachSection_LRPointXY = GetReachSection_LRPointXY(hydromodel, ReachSectionDataList);

            //逐个断面通过gis服务获取断面高程点
            Dictionary<AtReach, List<PointXZS>> SectionData = new Dictionary<AtReach, List<PointXZS>>();
            for (int i = 0; i < ReachSectionDataList.Count; i++)
            {
                List<PointXZS> section_points = Gis_Service.Get_Sectiondata_FromGisService(ReachSection_LRPointXY.Values.ElementAt(i));
                SectionData.Add(ReachSection_LRPointXY.Keys.ElementAt(i), section_points);
            }

            return SectionData;
        }


        //获取新河道断面线桩号坐标 -- 按步长分 distance -- 用于剖切断面的宽度,左右相同
        public static Dictionary<AtReach, List<PointXY>> GetReachSection_LRPointXY(ReachInfo newreach, double L_distance, double R_distance)
        {
            Dictionary<AtReach, List<PointXY>> ReachSectionChainage = new Dictionary<AtReach, List<PointXY>>();
            double reachlength = Math.Abs(newreach.end_chainage - newreach.start_chainage);

            //断面桩号
            int sectioncount = (int)(reachlength / newreach.dx) + 1;
            double[] section_chainage = new double[sectioncount];
            for (int i = 0; i < sectioncount - 1; i++)
            {
                section_chainage[i] = newreach.start_chainage + i * newreach.dx;
            }
            section_chainage[sectioncount - 1] = newreach.end_chainage;

            //断面起止点坐标
            List<PointXY> LRPoints;
            for (int i = 0; i < sectioncount; i++)
            {
                double chainage = section_chainage[i];
                AtReach atreach;
                atreach.chainage = chainage;
                atreach.reachname = newreach.reachname;
                atreach.reachid = atreach.reachname + Model_Const.REACHID_HZ;

                LRPoints = PointXY.Get_Distance_LRPoints(newreach.reachpoint_list, chainage, L_distance, R_distance);

                ReachSectionChainage.Add(atreach, LRPoints);
            }

            return ReachSectionChainage;
        }

        //获取老河道断面线桩号坐标 
        public static Dictionary<AtReach, List<PointXY>> GetReachSection_LRPointXY(HydroModel hydromodel, Dictionary<AtReach, List<PointXZS>> ReachSectionDataList)
        {
            Dictionary<AtReach, List<PointXY>> ReachSectionChainage = new Dictionary<AtReach, List<PointXY>>();
            string reachname = ReachSectionDataList.Keys.ElementAt(0).reachname;

            //获取河道的最大宽度
            double distance = hydromodel.Mike11Pars.SectionList.Get_ReachSection_Maxwidth(reachname) / 2;
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);

            //断面起止点坐标
            List<PointXY> LRPoints;
            for (int i = 0; i < ReachSectionDataList.Count; i++)
            {
                AtReach atreach = ReachSectionDataList.Keys.ElementAt(i);
                List<PointXZS> section_pointlist = ReachSectionDataList.Values.ElementAt(i);
                LRPoints = PointXY.Get_Distance_LRPoints(reach.reachpoint_list, atreach.chainage, distance, distance);

                ReachSectionChainage.Add(atreach, LRPoints);
            }

            return ReachSectionChainage;
        }

        #endregion ***************************************************************************************************


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

    //集水区连接类
    [Serializable]
    public class Catchment_ConnectList
    {
        //**********************属性************************
        //集水区连接信息
        public List<Catchment_Connectinfo> CatchmentConnect_Infolist
        { get; set; }

        //**********************方法************************
        //增加新集水区连接方法
        public void AddCatchmentConnect(Catchment_Connectinfo newcatchment_connect)
        {
            //如果没有相同名字的集水区则将该连接加入集合
            if (!Have_TheSameCatchment(newcatchment_connect.catchment_name))
            {
                //往集合中增加新集水区连接
                CatchmentConnect_Infolist.Add(newcatchment_connect);
            }
        }

        //增加新集水区连接方法
        public void AddCatchmentConnect(CatchmentList Catchmentlist, string catchmentname, Reach_Segment reach_segment)
        {
            //创建新连接
            Catchment_Connectinfo catchment_connect;
            CatchmentInfo catchment = Catchmentlist.Get_Catchmentinfo(catchmentname);
            catchment_connect.catchment_name = catchment.Name;
            catchment_connect.catchment_area = catchment.Area;
            catchment_connect.connect_reach = reach_segment;

            //判断后加入
            if (!CatchmentConnect_Infolist.Contains(catchment_connect))
            {
                //往集合中增加新集水区连接
                CatchmentConnect_Infolist.Add(catchment_connect);
            }
        }

        //减少集水区连接方法
        public void RemoveCatchmentConnect(string catchmentname)
        {
            for (int i = 0; i < CatchmentConnect_Infolist.Count; i++)
            {
                if (CatchmentConnect_Infolist[i].catchment_name == catchmentname)
                {
                    CatchmentConnect_Infolist.RemoveAt(i);
                    break;
                }
            }
        }

        //根据集水区名获取该集水区连接详细信息的方法
        public Catchment_Connectinfo Get_CatchmentConnect(string catchmentname)
        {
            Catchment_Connectinfo Catchment_connect;
            Catchment_connect.catchment_name = "";
            Catchment_connect.catchment_area = 0;
            Catchment_connect.connect_reach = CatchmentConnect_Infolist[0].connect_reach;

            for (int i = 0; i < CatchmentConnect_Infolist.Count; i++)
            {
                if (CatchmentConnect_Infolist[i].catchment_name == catchmentname)
                {
                    Catchment_connect = CatchmentConnect_Infolist[i];
                    break;
                }
            }
            return Catchment_connect;
        }

        //集水区连接对象集合中是否有相同名字的集水区
        public bool Have_TheSameCatchment(string catchment_name)
        {
            for (int i = 0; i < CatchmentConnect_Infolist.Count; i++)
            {
                if (CatchmentConnect_Infolist[i].catchment_name == catchment_name)
                {
                    return true;
                }
            }
            return false;
        }
    }

    //河道类，可由此得到默认所有河道的详细信息和新增加的河道信息(统一排)
    [Serializable]
    public class ReachList
    {
        //**********************属性************************
        //河道详细信息属性集合,里面含河道的控制点
        public List<ReachInfo> Reach_infolist
        { get; set; }

        //河道基本属性信息集合  其中的桩号为河道起点桩号
        public List<Reach_Segment> Reach_baseinfolist
        { get; set; }

        //河道计算点桩号集合，包括 断面桩号、入流出流点桩号、建筑物桩号，防止新增建筑物离它们过近
        public Dictionary<string, List<double>> Reach_gridchainagelist
        { get; set; }

        //河道控制点编号的最大值
        public int Maxpointnumber
        { get; set; }


        //**********************方法************************
        //增加已有河道计算点桩号集合方法
        public void Add_ExistReach_GridChainage(string reachname, double newchainage)
        {
            //获取该河道原来的计算点集合
            List<double> newchainagelist = Get_ReachGridChainage(reachname);
            if (!newchainagelist.Contains(newchainage))
            {
                newchainagelist.Add(newchainage);
            }

            //排序
            newchainagelist.Sort();

            //删除该河道原来的河道计算点集合
            Reach_gridchainagelist.Remove(reachname);

            //重新添加
            Reach_gridchainagelist.Add(reachname, newchainagelist);
        }

        //减少已有河道计算点桩号集合方法
        public void Remove_ExistReach_GridChainage(string reachname, double substactchainage)
        {
            //获取该河道原来的计算点集合
            List<double> newchainagelist = Get_ReachGridChainage(reachname);
            if (newchainagelist.Contains(substactchainage))
            {
                newchainagelist.Remove(substactchainage);
            }

            //删除该河道原来的河道计算点集合
            Reach_gridchainagelist.Remove(reachname);

            //重新添加
            Reach_gridchainagelist.Add(reachname, newchainagelist);
        }

        //增加新河道计算点桩号集合方法
        public void Add_NewReach_GridChainage(string reachname, List<double> newreach_gridchainagelist)
        {
            if (!Reach_gridchainagelist.Keys.Contains(reachname))
            {
                Reach_gridchainagelist.Add(reachname, newreach_gridchainagelist);
            }
        }

        //增加新河道方法
        public void AddReach(ReachInfo newreach)
        {
            if (!Reach_infolist.Contains(newreach))
            {
                //更改河道信息信息属性
                Reach_infolist.Add(newreach);

                //更改河道基本信息属性
                Reach_Segment reachbaseinfo;
                reachbaseinfo.reachname = newreach.reachname;
                reachbaseinfo.reachtopoid = newreach.reachtopoid;
                reachbaseinfo.start_chainage = newreach.start_chainage;
                reachbaseinfo.end_chainage = newreach.end_chainage;
                Reach_baseinfolist.Add(reachbaseinfo);

                //更改河道控制点编号最大值属性
                if (newreach.reachpoint_list[newreach.reachpoint_list.Count - 1].number > Maxpointnumber)
                {
                    Maxpointnumber = newreach.reachpoint_list[newreach.reachpoint_list.Count - 1].number;
                }

                //增加2个计算点（起点和终点）
                List<double> newreach_grid = new List<double>();
                newreach_grid.Add(newreach.start_chainage);
                newreach_grid.Add(newreach.end_chainage);
                Reach_gridchainagelist.Add(newreach.reachname, newreach_grid);
            }
        }

        //减少河道方法
        public void RemoveReach(string reachname)
        {
            for (int i = 0; i < Reach_infolist.Count; i++)
            {
                if (Reach_infolist[i].reachname == reachname)
                {
                    Reach_infolist.RemoveAt(i);
                    Reach_baseinfolist.RemoveAt(i);
                    Reach_gridchainagelist.Remove(reachname);
                    break;
                }
            }
        }

        //根据河道名获取该河道详细信息方法
        public ReachInfo Get_Reachinfo(string reachname)
        {
            ReachInfo reach = Reach_infolist[0];

            for (int i = 0; i < Reach_infolist.Count; i++)
            {
                if (Reach_infolist[i].reachname == reachname)
                {
                    reach = Reach_infolist[i];
                    break;
                }
            }
            return reach;
        }

        //根据河道名称和桩号获取河道坐标
        public PointXY Get_ReachPointxy(string reachname, double chainage)
        {
            List<ReachPoint> reach_points = Get_Reachinfo(reachname).reachpoint_list;

            //通过二分法查找
            return Search_Value(reach_points, chainage);
        }

        //根据河道名称和桩号获取河道坐标
        public PointXY Get_ReachPointxy(string reachname, double chainage,out int angle)
        {
            List<ReachPoint> reach_points = Get_Reachinfo(reachname).reachpoint_list;

            //通过二分法查找
            return Search_Value(reach_points, chainage,out angle);
        }

        //二分法查找指定桩号值的集合 序号
        public PointXY Search_Value(List<ReachPoint> reach_points, double chainage)
        {
            PointXY pointxy;
            pointxy.X = 0;
            pointxy.Y = 0;

            int tou = 0;
            int wei = reach_points.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (reach_points[zhong].pointchainage == chainage)
                {
                    return PointXY.Get_Pointxy(reach_points[zhong].X, reach_points[zhong].Y);
                }
                else if (reach_points[zhong].pointchainage > chainage)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            //如果没有找到，说明不在网格点上，则采用尾部序号附近的再找找范围内的，然后内插
            for (int i = Math.Max(wei - 1, 0); i < wei + 1; i++)
            {
                if(reach_points[i].pointchainage < chainage && reach_points[i+1].pointchainage > chainage)
                {
                    double chainage1 = reach_points[i].pointchainage;
                    double chainage2 = reach_points[i + 1].pointchainage;

                    double x1 = reach_points[i].X;
                    double x2 = reach_points[i + 1].X;

                    double y1 = reach_points[i].Y;
                    double y2 = reach_points[i + 1].Y;

                    pointxy.X = x1 + (x2 - x1) * (chainage - chainage1) / (chainage2 - chainage1);
                    pointxy.Y = y1 + (y2 - y1) * (chainage - chainage1) / (chainage2 - chainage1);
                    return pointxy;
                }
            }

            return pointxy;
        }

        //二分法查找指定桩号值的集合 序号 输出角度
        public PointXY Search_Value(List<ReachPoint> reach_points, double chainage,out int angle)
        {
            angle = 0;
            PointXY pointxy;
            pointxy.X = 0;
            pointxy.Y = 0;

            int tou = 0;
            int wei = reach_points.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (reach_points[zhong].pointchainage == chainage)
                {
                    PointXY res_point = PointXY.Get_Pointxy(reach_points[zhong].X, reach_points[zhong].Y);
                    if (zhong != wei)
                    {
                        PointXY next_point = PointXY.Get_Pointxy(reach_points[zhong +1].X, reach_points[zhong +1].Y);
                        angle = PointXY.Get_ptop_Angle(res_point, next_point);
                    }
                    return res_point;
                }
                else if (reach_points[zhong].pointchainage > chainage)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            //如果没有找到，说明不在网格点上，则采用尾部序号附近的再找找范围内的，然后内插
            for (int i = Math.Max(wei - 1,0); i < wei + 1; i++)
            {
                if (reach_points[i].pointchainage < chainage && reach_points[i + 1].pointchainage > chainage)
                {
                    double chainage1 = reach_points[i].pointchainage;
                    double chainage2 = reach_points[i + 1].pointchainage;

                    double x1 = reach_points[i].X;
                    double x2 = reach_points[i + 1].X;

                    double y1 = reach_points[i].Y;
                    double y2 = reach_points[i + 1].Y;

                    pointxy.X = x1 + (x2 - x1) * (chainage - chainage1) / (chainage2 - chainage1);
                    pointxy.Y = y1 + (y2 - y1) * (chainage - chainage1) / (chainage2 - chainage1);

                    PointXY start_point = PointXY.Get_Pointxy(x1, y1);
                    PointXY end_point = PointXY.Get_Pointxy(x2,y2);
                    angle = PointXY.Get_ptop_Angle(start_point, end_point);
                    return pointxy;
                }
            }

            return pointxy;
        }

        //根据河道名称和桩号获取河道上下游控制点的集合
        public List<PointXY> Get_ReachNearPointxyList(string reachname, double chainage)
        {
            List<PointXY> ReachNearPointxyList = new List<PointXY>();
            PointXY lastpointxy;
            lastpointxy.X = 0;
            lastpointxy.Y = 0;

            PointXY nextpointxy;
            nextpointxy.X = 0;
            nextpointxy.Y = 0;

            ReachInfo reachinfo = Get_Reachinfo(reachname);
            for (int i = 0; i < reachinfo.reachpoint_list.Count - 1; i++)
            {
                if (chainage == reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 1].pointchainage)
                {
                    lastpointxy.X = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 2].X;
                    lastpointxy.Y = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 2].Y;

                    nextpointxy.X = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 1].X;
                    nextpointxy.Y = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 1].Y;
                    break;
                }

                if (reachinfo.reachpoint_list[i].pointchainage <= chainage &&
                    reachinfo.reachpoint_list[i + 1].pointchainage > chainage)
                {

                    lastpointxy.X = reachinfo.reachpoint_list[i].X;
                    lastpointxy.Y = reachinfo.reachpoint_list[i].Y;

                    nextpointxy.X = reachinfo.reachpoint_list[i + 1].X;
                    nextpointxy.Y = reachinfo.reachpoint_list[i + 1].Y;
                    break;
                }
            }
            ReachNearPointxyList.Add(lastpointxy);
            ReachNearPointxyList.Add(nextpointxy);
            return ReachNearPointxyList;
        }

        //根据河道名称和起止桩号获取该河段控制点坐标集合
        public List<PointXY> Get_Segment_PointjwdList(string reachname, double start_chainage, double end_chainage)
        {
            List<PointXY> segment_jwdlist = new List<PointXY>();

            //河道和河道中心线折点信息
            ReachInfo reachinfo = Get_Reachinfo(reachname);
            List<ReachPoint> reach_pointinfo = reachinfo.reachpoint_list;

            // 如果河道控制点为空或起止桩号无效，返回空列表
            if (reach_pointinfo == null || reach_pointinfo.Count == 0 || start_chainage > end_chainage)
                return segment_jwdlist;

            // 找到包含起止桩号的控制点段
            int start_index = -1, end_index = -1;

            // 查找起始桩号所在的段
            for (int i = 0; i < reach_pointinfo.Count - 1; i++)
            {
                if (start_chainage >= reach_pointinfo[i].pointchainage &&
                    start_chainage <= reach_pointinfo[i + 1].pointchainage)
                {
                    start_index = i;
                    break;
                }
            }

            // 查找结束桩号所在的段
            for (int i = 0; i < reach_pointinfo.Count - 1; i++)
            {
                if (end_chainage >= reach_pointinfo[i].pointchainage &&
                    end_chainage <= reach_pointinfo[i + 1].pointchainage)
                {
                    end_index = i;
                    break;
                }
            }

            // 如果起止桩号不在河道范围内，返回空列表
            if (start_index == -1 || end_index == -1)
                return segment_jwdlist;

            // 获取该河段折点坐标集合
            List<PointXY> segment_xylist = new List<PointXY>();

            // 添加起始内插点
            if (start_chainage > reach_pointinfo[start_index].pointchainage)
            {
                // 在起始段内插点
                PointXY start_point = InterpolatePoint(reach_pointinfo[start_index],
                                                    reach_pointinfo[start_index + 1],
                                                    start_chainage);
                segment_xylist.Add(start_point);
            }
            else
            {
                // 直接添加起始控制点
                segment_xylist.Add(PointXY.Get_Pointxy(reach_pointinfo[start_index].X,
                                                    reach_pointinfo[start_index].Y));
            }

            // 添加中间完整段的所有控制点
            for (int i = start_index + 1; i <= end_index; i++)
            {
                segment_xylist.Add(PointXY.Get_Pointxy(reach_pointinfo[i].X, reach_pointinfo[i].Y));
            }

            // 添加结束内插点（如果需要）
            if (end_chainage < reach_pointinfo[end_index + 1].pointchainage)
            {
                // 在结束段内插点
                PointXY end_point = InterpolatePoint(reach_pointinfo[end_index],
                                                  reach_pointinfo[end_index + 1],
                                                  end_chainage);
                segment_xylist.Add(end_point);
            }
            else if (end_chainage == reach_pointinfo[end_index + 1].pointchainage)
            {
                // 如果正好在结束控制点上，添加该点
                segment_xylist.Add(PointXY.Get_Pointxy(reach_pointinfo[end_index + 1].X,
                                                    reach_pointinfo[end_index + 1].Y));
            }

            // 转为投影坐标
            segment_jwdlist = PointXY.CoordTranfrom(segment_xylist, 4547, 4326, 6);

            return segment_jwdlist;
        }

        // 内插点方法：根据桩号在线性段上内插坐标点
        private PointXY InterpolatePoint(ReachPoint p1, ReachPoint p2, double chainage)
        {
            // 计算比例因子
            double ratio = (chainage - p1.pointchainage) / (p2.pointchainage - p1.pointchainage);

            // 线性内插坐标
            double x = p1.X + ratio * (p2.X - p1.X);
            double y = p1.Y + ratio * (p2.Y - p1.Y);

            return PointXY.Get_Pointxy(x, y);
        }

        //根据河道名获取该河道计算点桩号方法
        public List<double> Get_ReachGridChainage(string reachname)
        {
            List<double> gridchainagelist = Reach_gridchainagelist.Values.ElementAt(0);

            for (int i = 0; i < Reach_infolist.Count; i++)
            {
                if (Reach_infolist[i].reachname == reachname)
                {
                    try
                    {
                        gridchainagelist = Reach_gridchainagelist.Values.ElementAt(i);
                    }
                    catch (Exception er)
                    {
                        Console.WriteLine(er.Message);
                    }
                    break;
                }
            }
            return gridchainagelist;
        }

    }

    //模型中的可控建筑物集合类
    [Serializable]
    public class ControlList
    {
        //********************属性***********************
        //闸门基本信息属性，包括建筑物名称和建筑物闸门数量，包含已有的的新增的
        public Dictionary<string, int> Gatebaseinfo
        { get; set; }

        //默认已有可控建筑物的集合，包括建筑物具体的名称、闸门数量、调度规则等，为全局变量
        public List<Controlstr> Default_GateList
        { get; set; }

        //默认已有可控建筑物的名字集合
        public List<string> Default_GateNameList
        { get; set; }

        //新增加的可控建筑物集合，类型可为普通闸门和分洪口闸门，为全局变量
        public List<Controlstr> NewAdd_GateList
        { get; set; }

        //总建筑物集合(包含已有默认的和新增加的)
        public List<Controlstr> GateListInfo
        { get; set; }

        //********************方法***********************
        //增加建筑物-- 包括默认已有的删了再加和新的建筑物加入
        public void Addgate(Controlstr gate)
        {
            if (!Gatebaseinfo.ContainsKey(gate.Strname))
            {
                Gatebaseinfo.Add(gate.Strname, gate.Stratts.gate_count);
                GateListInfo.Add(gate);

                //如果是默认已有建筑物的名字，则加入默认已有建筑物（用于默认已有建筑物的调度规则修改）
                if (!Default_GateNameList.Contains(gate.Strname))
                {
                    NewAdd_GateList.Add(gate); //相当于添加了新建筑物的引用
                }
                else
                {
                    Default_GateList.Add(gate);
                }
            }
        }

        //减少建筑物方法 *
        //**注意：集合remove对象后，则表示该集合不再引用该内存地址，但该对象内存地址还在，一直到没任何集合或变量引用了才会被系统回收***
        public void RemoveGate(string gatename)
        {
            if (Default_GateNameList.Contains(gatename)) Default_GateNameList.Remove(gatename);

            int gatecount = GateListInfo.Count;
            for (int i = 0; i < gatecount; i++)
            {
                //基础信息和总建筑物集合移除
                if (Gatebaseinfo.ElementAt(i).Key == gatename)
                {
                    Gatebaseinfo.Remove(gatename);
                    GateListInfo.RemoveAt(i);
                    break;
                }
            }

            int newgatecount = NewAdd_GateList.Count;
            for (int i = 0; i < newgatecount; i++)
            {
                //如果该建筑物为新增加的建筑物，则相应移除
                if (NewAdd_GateList[i].Strname == gatename)
                {
                    NewAdd_GateList.RemoveAt(i);
                    break;
                }
            }

            int defaultgatecount = Default_GateList.Count;
            for (int i = 0; i < defaultgatecount; i++)
            {
                //如果该建筑物为默认已有的建筑物，则相应移除
                if (Default_GateList[i].Strname == gatename)
                {
                    Default_GateList.RemoveAt(i);
                    break;
                }
            }
        }

        //根据建筑物名获取建筑物信息
        public Controlstr GetGate(string gatename)
        {
            for (int i = 0; i < GateListInfo.Count; i++)
            {
                if (gatename == GateListInfo[i].Strname)
                {
                    Controlstr controlstr = GateListInfo[i];
                    return controlstr;
                }
            }
            return null;
        }

        //根据建筑物名获取建筑物规则调度节的集合(可能有好几个节共同组成规则调度)
        public static List<PFSSection> GetGate_GZDUSec(PFSFile pfsfile, string gatename)
        {
            return Nwk11.Get_DefaultStr_GZDUSecList(pfsfile, gatename);
        }

    }

    //专用于构建普通建筑物及其调度信息的类
    [Serializable]
    public class Normalstr : Controlstr
    {
        #region ************************************构造函数****************************************

        //************************************规则调度********************************************//
        //适合于默认类型闸门 规则调度的构造函数,所有建筑物属性参数默认（闸门默认为溢流堰）
        public Normalstr(string strname)   //适用于初始化时默认的已有建筑物
            : base(strname)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

        }

        //适合于任意类型闸门 规则调度的构造函数--只有所在河道信息
        public Normalstr(string strname, AtReach atreach)
            : base(strname, atreach)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

        }

        //适合于任意类型闸门 规则调度的构造函数--带闸门基本属性值（闸门公式和弧形闸门参数均采用默认值）
        public Normalstr(string strname, AtReach atreach, Attributes stratts)
            : base(strname, atreach, stratts)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

        }

        //适合于平板公式闸门 规则调度的构造函数--带闸门基本属性值，带闸门公式参数
        public Normalstr(string strname, AtReach atreach, Attributes stratts, FormulaParams strformulapars)
            : base(strname, atreach, stratts, strformulapars)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

        }

        //适合于弧形闸门 规则调度的构造函数--带闸门基本属性值，带弧形闸门参数
        public Normalstr(string strname, AtReach atreach, Attributes stratts, RadiaParams strradiapars)
            : base(strname, atreach, stratts, strradiapars)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

        }
        //************************************规则调度********************************************//


        //************************************控泄调度********************************************//
        //适合于控泄调度的构造函数
        public Normalstr(string strname, CtrDdtype strddgz, double ddparams_double)
            : base(strname)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

            //部分重新赋值
            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_double = ddparams_double;

            Set_KXDUatts();  //基本属性改为适合控泄调度的属性
        }


        public Normalstr(string strname, AtReach atreach, CtrDdtype strddgz, double ddparams_double)
            : base(strname, atreach)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

            //部分重新赋值
            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_double = ddparams_double;

            Set_KXDUatts();  //基本属性改为适合控泄调度的属性
        }

        public Normalstr(string strname, AtReach atreach, CtrDdtype strddgz, List<double[]> str_qhrelation)
            : base(strname, atreach)
        {
            //先初始化，以免有些属性未赋值
            this.Setdefault();

            //部分重新赋值
            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Str_QHrelation = str_qhrelation;

            Set_KXDUatts();  //基本属性改为适合控泄调度的属性
        }
        //************************************控泄调度********************************************//


        //************************************时间调度********************************************//
        //适合于默认类型闸门 时间调度的构造函数,所有建筑物属性参数默认（闸门默认为溢流堰）
        public Normalstr(string strname, CtrDdtype strddgz, string ddparams_filenamestring, string ddparams_itemstring)
            : base(strname)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_filename = ddparams_filenamestring;
            this.Ddparams_itemname = ddparams_itemstring;
        }

        //适合于任意类型闸门 ZMDU_TIME调度的构造函数--带闸门基本属性值（闸门公式和弧形闸门参数均采用默认值）
        public Normalstr(string strname, AtReach atreach, Attributes stratts, CtrDdtype strddgz, string ddparams_filenamestring, string ddparams_itemstring)
            : base(strname, atreach, stratts)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_filename = ddparams_filenamestring;
            this.Ddparams_itemname = ddparams_itemstring;
        }

        //适合于任意类型闸门 KXDU_TIME调度的构造函数--不带闸门基本属性值（闸门公式和弧形闸门参数均采用默认值）
        public Normalstr(string strname, AtReach atreach, CtrDdtype strddgz, string ddparams_filenamestring, string ddparams_itemstring)
            : base(strname, atreach, strddgz)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_filename = ddparams_filenamestring;
            this.Ddparams_itemname = ddparams_itemstring;
        }

        //适合于平板公式闸门  ZMDU_TIME调度的构造函数--带闸门基本属性值，带闸门公式参数
        public Normalstr(string strname, AtReach atreach, Attributes stratts, FormulaParams strformulapars, CtrDdtype strddgz, string ddparams_filenamestring, string ddparams_itemstring)
            : base(strname, atreach, stratts, strformulapars)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_filename = ddparams_filenamestring;
            this.Ddparams_itemname = ddparams_itemstring;
        }


        //适合于弧形闸门  ZMDU_TIME调度的构造函数--带闸门基本属性值，带弧形闸门参数
        public Normalstr(string strname, AtReach atreach, Attributes stratts, RadiaParams strradiapars, CtrDdtype strddgz, string ddparams_filenamestring, string ddparams_itemstring)
            : base(strname, atreach, stratts, strradiapars)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_filename = ddparams_filenamestring;
            this.Ddparams_itemname = ddparams_itemstring;
        }
        //************************************时间调度********************************************//


        //************************************闸门调度********************************************//
        //适合于默认类型闸门 闸门调度的构造函数,所有建筑物属性参数默认（闸门默认为溢流堰）
        public Normalstr(string strname, CtrDdtype strddgz, ZMDUinfo ddparams_zmdu)
            : base(strname)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_zmdu = ddparams_zmdu;
        }

        //适合于任意类型闸门 闸门调度的构造函数--带闸门基本属性值（闸门公式和弧形闸门参数均采用默认值）
        public Normalstr(string strname, AtReach atreach, Attributes stratts, CtrDdtype strddgz, ZMDUinfo ddparams_zmdu)
            : base(strname, atreach, stratts)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_zmdu = ddparams_zmdu;
        }

        //适合于平板公式闸门 闸门调度的构造函数--带闸门基本属性值，带闸门公式参数
        public Normalstr(string strname, AtReach atreach, Attributes stratts, FormulaParams strformulapars, CtrDdtype strddgz, ZMDUinfo ddparams_zmdu)
            : base(strname, atreach, stratts, strformulapars)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_zmdu = ddparams_zmdu;
        }

        //适合于弧形闸门 闸门调度的构造函数--带闸门基本属性值，带弧形闸门参数
        public Normalstr(string strname, AtReach atreach, Attributes stratts, RadiaParams strradiapars, CtrDdtype strddgz, ZMDUinfo ddparams_zmdu)
            : base(strname, atreach, stratts, strradiapars)
        {
            this.Setdefault();

            this.Strname = strname;
            this.Strddgz = strddgz;
            this.Ddparams_zmdu = ddparams_zmdu;
        }
        //************************************闸门调度********************************************//
        #endregion *******************************************

        #region ****************************属性********************************************
        //属性
        public CtrDdtype Strddgz    //选用的调度规则
        { get; set; }

        public double Ddparams_double    //double类型的参数
        { get; set; }

        public string Ddparams_filename   //string类型的文件名参数
        { get; set; }

        public string Ddparams_itemname   //string类型的文件中项目名参数
        { get; set; }

        public ZMDUinfo Ddparams_zmdu   //ZMDUinfo类型的参数
        { get; set; }

        public List<double[]> Str_QHrelation   //建筑物水位流量关系
        { get; set; }
        #endregion *****************************************************************************

        #region *********************************方法******************************************
        //方法
        //设置默认参数
        public void Setdefault()
        {
            CtrDdtype strddgz = CtrDdtype.GZDU;
            ZMDUinfo zmduinfo;
            zmduinfo.fullyopen = true;
            zmduinfo.opengatenumber = 1;
            zmduinfo.opengateheight = 0;

            //初始化
            this.Strddgz = strddgz;
            this.Ddparams_double = 0.0;
            this.Ddparams_filename = "";
            this.Ddparams_itemname = "";
            this.Ddparams_zmdu = zmduinfo;
            this.Str_QHrelation = new List<double[]>();
        }

        //设置控泄调度的建筑物基本属性
        public void Set_KXDUatts()
        {
            Attributes attribute;
            attribute.gate_type = 2;
            attribute.gate_count = 1;
            attribute.under_flowcc = 0.63;
            attribute.gate_width = 0;
            attribute.sill_level = 0;
            attribute.max_speed = Model_Const.Q_MAXSPEED;  //控泄调度的最大流量改变速率
            attribute.bool_initial_value = false;
            attribute.bool_max_value = false;
            attribute.initial_value = 0;
            attribute.max_value = 0;
            attribute.gate_height = 0;
            this.Stratts = attribute;
        }

        //获取模型的普通闸门集合
        public static List<Normalstr> Get_Model_Normalstr(HydroModel model)
        {
            List<Normalstr> normal_str = new List<Normalstr>();
            List<Controlstr> gate_list = model.Mike11Pars.ControlstrList.GateListInfo;

            //转为普通闸门
            for (int i = 0; i < gate_list.Count; i++)
            {
                if (gate_list[i] is Normalstr) normal_str.Add(gate_list[i] as Normalstr);
            }
            return normal_str;
        }
        #endregion ****************************************************************************
    }

    //构建溃堤建筑物的类
    [Serializable]
    public class Fhkstr : Controlstr
    {
        //构造函数,两种调度规则都适合
        public Fhkstr(FhkstrInfo fhkstrinfo)
            : base(fhkstrinfo.strname, fhkstrinfo.atreach)
        {
            //先初始化
            this.Setdefault();

            //部分重新赋值
            Setattribute(fhkstrinfo);
            this.Strddgz = fhkstrinfo.strddgz;
            this.fhklevel = fhkstrinfo.fhk_datumn;
            this.fhwaterlevel = fhkstrinfo.fh_waterlevel;
            this.time_filename = fhkstrinfo.time_filename;
            this.time_itemname = fhkstrinfo.time_itemname;
        }

        //属性
        public CtrDdtype Strddgz    //选用的调度规则
        { get; set; }

        public double fhklevel    //double类型的参数
        { get; set; }

        public double fhwaterlevel    //double类型的参数
        { get; set; }

        public string time_filename   //string类型的文件名参数
        { get; set; }

        public string time_itemname   //string类型的文件中项目名参数
        { get; set; }

        //方法
        //设置默认参数
        public void Setdefault()
        {
            CtrDdtype strddgz = CtrDdtype.GZDU;
            ZMDUinfo zmduinfo;
            zmduinfo.fullyopen = true;
            zmduinfo.opengatenumber = 1;
            zmduinfo.opengateheight = 0;


            //初始化
            this.Strddgz = strddgz;
            this.fhklevel = 0.0;
            this.fhwaterlevel = 0.0;
            this.time_filename = "";
            this.time_itemname = "";
        }

        //根据分洪口信息重新设置建筑物基本属性
        public void Setattribute(FhkstrInfo fhkstrinfo)
        {
            Attributes attribute;
            attribute.gate_type = 0;
            attribute.gate_count = 1;
            attribute.under_flowcc = 0.63;
            attribute.gate_width = fhkstrinfo.width;       //溃口宽度赋值
            attribute.sill_level = fhkstrinfo.dm_level;  //溃口处地面高程赋值
            attribute.max_speed = Math.Round((fhkstrinfo.dd_level - fhkstrinfo.fhk_datumn) / fhkstrinfo.break_seconds, 3);    //溃决速率 m/s
            attribute.bool_initial_value = true;
            attribute.bool_max_value = true;
            attribute.initial_value = fhkstrinfo.dd_level;  //初始值赋值为溃口处堤顶高程
            attribute.max_value = fhkstrinfo.dd_level;      //最大值赋值为溃口处堤顶高程
            attribute.gate_height = 0;
            this.Stratts = attribute;
        }
    }

    //专用于构建可控建筑物的类，不含调度信息
    [Serializable]
    public class Controlstr
    {
        //构造函数

        //构造其他连属性都没有的闸门
        public Controlstr(string strname)
        {
            this.Strname = strname;

            //其他设置为默认
            Setdefault_Sidetype();
            Setdefault_atreach();
            Setdefault_atts();
            Setdefault_headloss();
            Setdefault_radiapars();
            Setdefault_formulapars();
        }

        //构造其他连属性都没有的闸门，但有所在河道信息,闸门类型默认为第一种溢流，相应设置最大开启速度0.01
        public Controlstr(string strname, AtReach atreach)
        {
            this.Strname = strname;
            this.Reachinfo = atreach;

            //其他设置为默认
            Setdefault_Sidetype();
            Setdefault_atts();
            Setdefault_headloss();
            Setdefault_radiapars();
            Setdefault_formulapars();
        }

        //构造其他连属性都没有的闸门，但有所在河道信息,闸门类型默认为第3种流量，相应设置最大流量变化速率2
        public Controlstr(string strname, AtReach atreach, CtrDdtype strddgz)
        {
            this.Strname = strname;
            this.Reachinfo = atreach;

            //其他设置为默认
            if (strddgz == CtrDdtype.KXDU || strddgz == CtrDdtype.KXDU_TIME)
            {
                Setdefault_atts1();
            }
            else
            {
                Setdefault_atts();
            }

            Setdefault_Sidetype();
            Setdefault_headloss();
            Setdefault_radiapars();
            Setdefault_formulapars();
        }

        //构造其他有属性的闸门
        public Controlstr(string strname, AtReach atreach, Attributes stratts)
        {
            this.Strname = strname;
            this.Reachinfo = atreach;
            this.Stratts = stratts;

            //其他设置为默认
            Setdefault_Sidetype();
            Setdefault_headloss();
            Setdefault_radiapars();
            Setdefault_formulapars();
        }

        //构建水闸公式闸门
        public Controlstr(string strname, AtReach atreach, Attributes stratts, FormulaParams strformulapars)
        {
            this.Strname = strname;
            this.Reachinfo = atreach;
            this.Stratts = stratts;
            this.Strformulapars = strformulapars;

            //其他设置为默认
            Setdefault_Sidetype();
            Setdefault_headloss();
            Setdefault_radiapars();
        }

        //构建弧形闸门
        public Controlstr(string strname, AtReach atreach, Attributes stratts, RadiaParams strradiapars)
        {
            this.Strname = strname;
            this.Reachinfo = atreach;
            this.Stratts = stratts;
            this.Strradiapars = strradiapars;

            //其他设置为默认
            Setdefault_Sidetype();
            Setdefault_headloss();
            Setdefault_formulapars();
        }

        //属性
        public AtReach Reachinfo        //所在河道信息
        { get; set; }

        public Str_SideType Str_Sidetype  //是正向还是侧向
        { get; set; }

        public string Strname       //建筑物名称
        { get; set; }

        public Headloss Strheadloss    //水头损失系数因子参数
        { get; set; }

        public Attributes Stratts   //可控建筑物类型、尺寸等参数
        { get; set; }

        public RadiaParams Strradiapars   //可控建筑物弧形闸门参数
        { get; set; }

        public FormulaParams Strformulapars  //水闸公式参数
        { get; set; }

        //方法
        public void Setdefault_headloss()   //默认水头损失系数设置
        {
            Headloss headloss;
            headloss.positive_inflow = 0.5;
            headloss.positive_outflow = 1;
            headloss.positive_freeoverflow = 1;

            headloss.negative_inflow = 0.5;
            headloss.negative_outflow = 1;
            headloss.negative_freeoverflow = 1;
            this.Strheadloss = headloss;
        }

        public void Setdefault_radiapars()   //默认弧形闸门参数设置
        {
            RadiaParams radiapars;
            radiapars.tune_factor = 1;
            radiapars.height = 1;
            radiapars.radius = 1;
            radiapars.trunnion = 1;
            radiapars.weir_coeff = 1.838;
            radiapars.weri_exp = 1.5;
            radiapars.tran_bottom = -0.152;
            radiapars.tran_depth = 0.304;
            this.Strradiapars = radiapars;
        }

        public void Setdefault_formulapars()  //默认水闸公式参数设置
        {
            FormulaParams formulaparams;
            formulaparams.CS_highlimit = 1.05;
            formulaparams.CS_lowlimit = 0.95;
            formulaparams.CF_highlimit = 1.55;
            formulaparams.CF_lowlimit = 1.45;
            formulaparams.US_highlimit = 0.7;
            formulaparams.US_lowlimit = 0.6;

            formulaparams.CS_coef_a = 1.12;
            formulaparams.CF_coef_a = 0.89;
            formulaparams.US_coef_a = 0.86;
            formulaparams.UF_coef_a = 0.77;

            formulaparams.CS_exp_b = 0.21;
            formulaparams.CF_exp_b = 0.17;
            formulaparams.US_exp_b = 0.38;
            this.Strformulapars = formulaparams;
        }

        //默认基本属性参数设置，溢流类型的闸门，最大速度为开闸速度
        public void Setdefault_atts()
        {
            //先获取所在河道断面的高程信息
            ReachSection_Altitude Section_Altitude = Xns11.Get_Altitude(this.Reachinfo);

            Attributes attribute;
            attribute.gate_type = 4;   //公式闸门
            attribute.gate_count = 1;
            attribute.under_flowcc = 0.63;

            //从数据库求所在河底高程
            attribute.sill_level = Section_Altitude.section_lowest + 0.5; //闸底高程按所在河底高程+0.5m

            attribute.max_speed = Model_Const.FLATGATE_MAXSPEED;
            attribute.bool_initial_value = true;
            attribute.bool_max_value = true;
            attribute.initial_value = (Section_Altitude.left_dd_altitude + Section_Altitude.right_dd_altitude) / 2;
            attribute.max_value = (Section_Altitude.left_dd_altitude + Section_Altitude.right_dd_altitude) / 2;
            attribute.gate_height = attribute.max_value - attribute.sill_level;
            attribute.gate_width = attribute.gate_height * 2;
            this.Stratts = attribute;
        }

        //默认基本属性参数设置，流量类型的闸门
        public void Setdefault_atts1()
        {
            Attributes attribute;
            attribute.gate_type = 2;
            attribute.gate_count = 1;
            attribute.under_flowcc = 0.63;
            attribute.gate_width = 0;
            attribute.sill_level = 0;
            attribute.max_speed = Model_Const.Q_MAXSPEED;
            attribute.bool_initial_value = false;
            attribute.bool_max_value = false;
            attribute.initial_value = 0;
            attribute.max_value = 0;
            attribute.gate_height = 0;
            this.Stratts = attribute;
        }

        //默认所在河道属性
        public void Setdefault_atreach()
        {
            AtReach atreach;
            atreach.reachname = "";
            atreach.reachid = "";
            atreach.chainage = 0.0;
            this.Reachinfo = atreach;
        }

        //默认建筑物放置位置类型 -- 普通拦河建筑物、侧向建筑物
        public void Setdefault_Sidetype()
        {
            this.Str_Sidetype = Str_SideType.regular;
        }

        //返回默认所在河道信息
        public static AtReach Getdefault_Atreach()
        {
            //默认所在河道信息
            AtReach atreach;
            atreach.reachname = "";
            atreach.reachid = "";
            atreach.chainage = 0.0;

            return atreach;
        }

        //返回默认属性(闸门类型为流量)值
        public static Attributes Getdefault_Discharge_Attribute()
        {
            //默认属性参数
            Attributes attribute;
            attribute.gate_type = 2;
            attribute.gate_count = 1;
            attribute.under_flowcc = 0.63;
            attribute.gate_width = 0;
            attribute.sill_level = 0;
            attribute.max_speed = Model_Const.Q_MAXSPEED;
            attribute.bool_initial_value = false;
            attribute.bool_max_value = false;
            attribute.initial_value = 0;
            attribute.max_value = 0;
            attribute.gate_height = 0;

            return attribute;
        }

        //返回默认公式参数值
        public static FormulaParams Getdefault_FormulaParams()
        {
            //默认属性参数
            FormulaParams formulaparams;
            formulaparams.CS_highlimit = 1.05;
            formulaparams.CS_lowlimit = 0.95;
            formulaparams.CF_highlimit = 1.55;
            formulaparams.CF_lowlimit = 1.45;
            formulaparams.US_highlimit = 0.7;
            formulaparams.US_lowlimit = 0.6;

            formulaparams.CS_coef_a = 1.12;
            formulaparams.CF_coef_a = 0.89;
            formulaparams.US_coef_a = 0.86;
            formulaparams.UF_coef_a = 0.77;

            formulaparams.CS_exp_b = 0.21;
            formulaparams.CF_exp_b = 0.17;
            formulaparams.US_exp_b = 0.38;

            return formulaparams;
        }


    }

}