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
}
