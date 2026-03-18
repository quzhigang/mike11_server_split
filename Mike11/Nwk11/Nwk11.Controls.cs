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
