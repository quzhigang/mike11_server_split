using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using bjd_model.Common;
using bjd_model.Const_Global;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kdbndp;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace bjd_model.Mike11
{
    //专用于流域相关属性和方法的类
    [Serializable]
    public partial class Item_Info
    {
        #region ********************* 类的静态属性 ************************
        //主要河道基本信息(除退水闸支流以外)
        public static List<ReachInfo> Main_ReachInfo
        { get; set; }

        //建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Str_BaseInfo
        { get; set; }

        //水库基本信息
        public static List<Reservoir> Reservoir_Info
        { get; set; }

        //蓄滞洪区信息
        public static List<Xzhq_Info> Xzhq_Info
        { get; set; }

        //河道当前监测流量
        public static Dictionary<AtReach, double> Reach_NowDischarge
        { get; set; }

        //河道控制断面水位流量关系
        public static Dictionary<AtReach, List<double[]>> Section_QHrelation
        { get; set; }

        //主要闸站闸前水位和流量关系
        public static Dictionary<string, List<double[]>> Struct_QHrelation
        { get; set; }

        //项目的断面数据
        public static Dictionary<string, SectionList> Item_SectionDatas
        { get; set; }

        //工程(水库河道蓄滞洪区) 所在地市和主管单位信息
        public static List<Struct_Region_Info> Struct_Location_Info
        { get; set; }
        #endregion *********************************************************
       

        #region ********************* 初始化静态属性 ************************
        //初始化项目的原始断面数据
        public static void Update_Item_SectionDatas(string model_instance)
        {
            if(Item_SectionDatas == null) Item_SectionDatas = new Dictionary<string, SectionList>();

            //首先从数据库里获取
            SectionList section_object = Xns11.Load_ModelSection_Object(model_instance);
            if (section_object != null)
            {
                Item_SectionDatas.Add(model_instance, section_object);
            }
            else
            {
                //数据库里没有则从模型模型断面文件里获取
                string xns11_dirname = Model_Files.Get_Item_DefaultModel_Dir(model_instance);
                string xns11_filename = Directory.GetFiles(xns11_dirname, "*.xns11").Length != 0 ? xns11_dirname + @"\" + Path.GetFileName(Directory.GetFiles(xns11_dirname, "*.xns11")[0]) : "";
                if (xns11_filename != "")
                {
                    SectionList item_sectionlist = new SectionList();
                    Xns11.GetDefault_SectionInfo(xns11_filename, ref item_sectionlist);
                    Item_SectionDatas.Add(model_instance, item_sectionlist);
                }
            }

            Console.WriteLine("当前项目横断面数据加载完成！");
        }

        //初始化建筑物基本信息(读取数据库)
        public static void Initial_StrBaseInfo()
        {
            Dictionary<string, Struct_BasePars> str_baseinfos = new Dictionary<string, Struct_BasePars>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.STRUCT_INFO;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName + " order by id";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string cn_name = dt.Rows[i][1].ToString();
                string str_name = dt.Rows[i][2].ToString();
                string gatetype = dt.Rows[i][3].ToString();
                GateType gate_type = GateType.YLZ;
                if (gatetype == "平板闸")
                {
                    gate_type = GateType.PBZ;
                }
                else if (gatetype == "流量闸")
                {
                    gate_type = GateType.LLZ;
                }
                else if (gatetype == "泵站")
                {
                    gate_type = GateType.BZ;
                }
                else if (gatetype == "无闸门")
                {
                    gate_type = GateType.NOGATE;
                }
                string str_type = dt.Rows[i][4] == null ? "" : dt.Rows[i][4].ToString();
                string reach = dt.Rows[i][5] == null ? "" : dt.Rows[i][5].ToString();
                string reach_cn = dt.Rows[i][6] == null ? "" : dt.Rows[i][6].ToString();
                double chainage = dt.Rows[i][7] == null ? 0 : double.Parse(dt.Rows[i][7].ToString());
                double design_q = dt.Rows[i][8] == null ? 0 : double.Parse(dt.Rows[i][8].ToString());
                double datumn = dt.Rows[i][9] == null ? 0 : double.Parse(dt.Rows[i][9].ToString());

                int gate_n = dt.Rows[i][10].ToString() == "" ? 0 : int.Parse(dt.Rows[i][10].ToString());
                double gate_b = dt.Rows[i][11].ToString() == "" ? 0 : double.Parse(dt.Rows[i][11].ToString());
                double gate_h = dt.Rows[i][12].ToString() == "" ? 0 : double.Parse(dt.Rows[i][12].ToString());
                double gate_height = dt.Rows[i][13].ToString() == "" ? 0 : double.Parse(dt.Rows[i][13].ToString());

                string dd_rule = dt.Rows[i][18] == null ? "" : dt.Rows[i][18].ToString();
                List<string> model_instances = dt.Rows[i][19] == null ? null : dt.Rows[i][19].ToString().Split(',').ToList();

                string stcd1 = dt.Rows[i][20] == null ? "" : dt.Rows[i][20].ToString();
                string stcd2 = dt.Rows[i][21] == null ? "" : dt.Rows[i][21].ToString();
                string stcd = stcd1 + stcd2;
                string control_str = dt.Rows[i][22] == null ? "" : dt.Rows[i][22].ToString();

                Struct_BasePars str_par = new Struct_BasePars(str_name,cn_name, gate_type, str_type, reach, reach_cn, chainage, design_q,
                    datumn, gate_n, gate_b, gate_h, gate_height, dd_rule, control_str, stcd, model_instances);
                str_baseinfos.Add(str_name, str_par);
            }

            Item_Info.Str_BaseInfo = str_baseinfos;
            Console.WriteLine("建筑物基础信息加载完成！");
        }

        //初始化水库基本信息(读取数据库)
        public static void Initial_ResInfo()
        {
            List<Reservoir> res_info = new List<Reservoir>();

            //从数据库读取水情监测信息
            List<Water_Condition> now_water = Hd11.Read_NowWater();

            //从数据库读取水库水位-库容关系曲线
            Dictionary<string, Dictionary<double, double>> res_qhrelation = Get_ResHVrelation();

            //水库包含的河道信息
            Dictionary<string, List<AtReach>> res_controlreach = Item_Info.GetRes_ControlInitialAtreach();

            //从中刷选出水库
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Datasource == "水库水文站")
                {
                    List<AtReach> contain_atreachs = res_controlreach.Keys.Contains(now_water[i].Name) ? res_controlreach[now_water[i].Name] : null;
                    List<Reach_Segment> contain_ReachSeg = CombineAtReachs(contain_atreachs);
                    Reach_Segment atreach_Segment = Get_ResAt_ReachSeg(contain_ReachSeg, now_water[i].Reach);
                    string res_stcd = now_water[i].Stcd;
                    Dictionary<double, double> res_qh = res_qhrelation[res_stcd];
                    Dictionary<double, double> level_yhdq = Get_Res_Strlq_Relation(now_water[i].Name,"溢洪道");
                    Dictionary<double, double> level_xhdq = Get_Res_Strlq_Relation(now_water[i].Name, "泄洪洞");
                    Reservoir res = new Reservoir(now_water[i].Stcd, now_water[i].Name, res_qh, level_yhdq, level_xhdq, atreach_Segment, contain_ReachSeg);
                    res_info.Add(res);
                }
            }

            Item_Info.Reservoir_Info = res_info;
            Console.WriteLine("水库基础信息加载完成！");
        }

        //初始化蓄滞洪区基本信息
        public static void Initial_Xzhq_Info()
        {
            //从数据库获取蓄滞洪区实例，并更新lv,la虚线
            List<Xzhq_Info> Xzhq_Info = Get_Xzhq_BaseInfo();
            Modify_Xzhq_Relation(ref Xzhq_Info);

            //***** 手动逐个补充其他参数*****//
            //良相坡 -- //入库断面集合、出库断面集合、蓄滞洪区水位代表位置
            Xzhq_Info lxp = Xzhq_Info.Find(p => p.Stcd == "LXP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> lxp_inq_section = new Dictionary<string, AtReach>();
            lxp_inq_section.Add("共渠入流", AtReach.Get_Atreach("GQ", 40000));
            lxp_inq_section.Add("卫河入流", AtReach.Get_Atreach("WH", 45000));
            lxp_inq_section.Add("香泉河入流", AtReach.Get_Atreach("XQH", 29000));
            lxp_inq_section.Add("沧河入流", AtReach.Get_Atreach("CH", 40000));
            lxp_inq_section.Add("思德河入流", AtReach.Get_Atreach("SDH", 25000));
            lxp_inq_section.Add("闫村分洪堰", AtReach.Get_Atreach("YC_KKZL", 500));
            lxp.InQ_Section_List = lxp_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> lxp_outq_section = new Dictionary<string, AtReach>();
            lxp_outq_section.Add("共渠出流", AtReach.Get_Atreach("GQ", 61000));
            lxp_outq_section.Add("卫河出流", AtReach.Get_Atreach("WH", 74000));
            lxp_outq_section.Add("宋村分洪堰", AtReach.Get_Atreach("SC_KKZL", 500));
            lxp_outq_section.Add("刘庄分洪堰", AtReach.Get_Atreach("LZ_KKZL", 500));
            lxp_outq_section.Add("长虹渠分洪闸(堰)", AtReach.Get_Atreach("CHQ_KKZL", 500));
            lxp.OutQ_Section_List = lxp_outq_section;
            lxp.Level_Atreach = AtReach.Get_Atreach("YC_KKZL", 1500);
            lxp.Only_FhyPd_State = false;
            lxp.State_Pd_AtReachQ = new Dictionary<AtReach, double>() { { AtReach.Get_Atreach("GQ", 56000), 500 } };

            //良相坡（上片） -- //入库断面集合、出库断面集合、蓄滞洪区水位代表位置
            Xzhq_Info lxp_up = Xzhq_Info.Find(p => p.Stcd == "LXP_UP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> lxpup_inq_section = new Dictionary<string, AtReach>();
            lxpup_inq_section.Add("共渠入流", AtReach.Get_Atreach("GQ", 40000));
            lxpup_inq_section.Add("香泉河入流", AtReach.Get_Atreach("XQH", 29000));
            lxpup_inq_section.Add("沧河入流", AtReach.Get_Atreach("CH", 40000));
            lxpup_inq_section.Add("思德河入流", AtReach.Get_Atreach("SDH", 25000));
            lxpup_inq_section.Add("闫村分洪堰", AtReach.Get_Atreach("YC_KKZL", 500));
            lxp_up.InQ_Section_List = lxpup_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> lxpup_outq_section = new Dictionary<string, AtReach>();
            lxpup_outq_section.Add("共渠出流", AtReach.Get_Atreach("GQ", 61000));
            lxpup_outq_section.Add("小河口闸", AtReach.Get_Atreach("QHNZ", 294));
            lxpup_outq_section.Add("刘庄分洪堰", AtReach.Get_Atreach("LZ_KKZL", 500));
            lxp_up.OutQ_Section_List = lxpup_outq_section;
            lxp_up.Level_Atreach = AtReach.Get_Atreach("YC_KKZL", 1500);
            lxp_up.Only_FhyPd_State = false;
            lxp_up.State_Pd_AtReachQ = new Dictionary<AtReach, double>() { { AtReach.Get_Atreach("GQ", 56000), 500 } };

            //良相坡（下片）
            Xzhq_Info lxp_down = Xzhq_Info.Find(p => p.Stcd == "LXP_DOWN_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> lxpdown_inq_section = new Dictionary<string, AtReach>();
            lxpdown_inq_section.Add("卫河入流", AtReach.Get_Atreach("WH", 45000));
            lxpdown_inq_section.Add("小河口闸", AtReach.Get_Atreach("QHNZ", 294));
            lxpdown_inq_section.Add("西沿村分洪堰", AtReach.Get_Atreach("XYC_KKZL", 500));
            lxp_down.InQ_Section_List = lxpdown_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> lxpdown_outq_section = new Dictionary<string, AtReach>();
            lxpdown_outq_section.Add("卫河出流", AtReach.Get_Atreach("WH", 74000));
            lxpdown_outq_section.Add("宋村分洪堰", AtReach.Get_Atreach("SC_KKZL", 500));
            lxpdown_outq_section.Add("长虹渠分洪闸(堰)", AtReach.Get_Atreach("CHQ_KKZL", 500));
            lxp_down.OutQ_Section_List = lxpdown_outq_section;
            lxp_down.Level_Atreach = AtReach.Get_Atreach("XYC_KKZL", 500);
            lxp_down.Only_FhyPd_State = true;

            //共渠西 -- //入库断面集合、出库断面集合、蓄滞洪区水位代表位置
            Xzhq_Info gqx = Xzhq_Info.Find(p => p.Stcd == "GQX_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> gqx_inq_section = new Dictionary<string, AtReach>();
            gqx_inq_section.Add("共渠入流", AtReach.Get_Atreach("GQ", 61000));
            gqx_inq_section.Add("刘庄分洪堰", AtReach.Get_Atreach("LZ_KKZL", 500));
            gqx.InQ_Section_List = gqx_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> gqx_outq_section = new Dictionary<string, AtReach>();
            gqx_outq_section.Add("长丰沟节制闸", AtReach.Get_Atreach("CFG", 254));
            gqx_outq_section.Add("盐土庄节制闸", AtReach.Get_Atreach("GQ", 93001));
            gqx_outq_section.Add("白寺坡分洪闸(堰)", AtReach.Get_Atreach("BSP_KKZL", 500));
            gqx.OutQ_Section_List = gqx_outq_section;
            gqx.Level_Atreach = AtReach.Get_Atreach("LZ_KKZL", 9500);
            gqx.Only_FhyPd_State = false;
            gqx.State_Pd_AtReachQ = new Dictionary<AtReach, double>() { { AtReach.Get_Atreach("GQ", 83000), 250 } };

            //共渠西（上片）
            Xzhq_Info gqx_up = Xzhq_Info.Find(p => p.Stcd == "GQX_UP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> qgxup_inq_section = new Dictionary<string, AtReach>();
            qgxup_inq_section.Add("共渠入流", AtReach.Get_Atreach("GQ", 61000));
            qgxup_inq_section.Add("刘庄分洪堰", AtReach.Get_Atreach("LZ_KKZL", 500));
            gqx_up.InQ_Section_List = qgxup_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> qgxup_outq_section = new Dictionary<string, AtReach>();
            qgxup_outq_section.Add("共渠出流", AtReach.Get_Atreach("GQ", 73700));
            gqx_up.OutQ_Section_List = qgxup_outq_section;
            gqx_up.Level_Atreach = AtReach.Get_Atreach("LZ_KKZL", 9500);
            gqx_up.Only_FhyPd_State = true;

            //共渠西（下片）
            Xzhq_Info gqx_down = Xzhq_Info.Find(p => p.Stcd == "GQX_DOWN_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> qgxdown_inq_section = new Dictionary<string, AtReach>();
            qgxdown_inq_section.Add("共渠入流", AtReach.Get_Atreach("GQ", 73700));
            gqx_down.InQ_Section_List = qgxdown_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> qgxdown_outq_section = new Dictionary<string, AtReach>();
            qgxdown_outq_section.Add("长丰沟节制闸", AtReach.Get_Atreach("CFG", 254));
            qgxdown_outq_section.Add("盐土庄节制闸", AtReach.Get_Atreach("GQ", 93001));
            qgxdown_outq_section.Add("白寺坡分洪闸(堰)", AtReach.Get_Atreach("BSP_KKZL", 500));
            gqx_down.OutQ_Section_List = qgxdown_outq_section;
            gqx_down.Level_Atreach = AtReach.Get_Atreach("GQ", 92090.7);
            gqx_down.Only_FhyPd_State = false;
            gqx_down.State_Pd_AtReachQ = new Dictionary<AtReach, double>() { { AtReach.Get_Atreach("GQ", 83000), 250 } };

            //白寺坡
            Xzhq_Info bsp = Xzhq_Info.Find(p => p.Stcd == "BSP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> bsp_inq_section = new Dictionary<string, AtReach>();
            bsp_inq_section.Add("白寺坡分洪闸(堰)", AtReach.Get_Atreach("BSP_KKZL", 500));
            bsp.InQ_Section_List = bsp_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> bsp_outq_section = new Dictionary<string, AtReach>();
            bsp_outq_section.Add("白寺坡退水闸", AtReach.Get_Atreach("MFG", 9721));
            bsp.OutQ_Section_List = bsp_outq_section;
            bsp.Level_Atreach = AtReach.Get_Atreach("BSP_KKZL", 10000);
            bsp.Only_FhyPd_State = true;

            //长虹渠
            Xzhq_Info chq = Xzhq_Info.Find(p => p.Stcd == "CHQ_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> chq_inq_section = new Dictionary<string, AtReach>();
            chq_inq_section.Add("长虹渠分洪闸(堰)", AtReach.Get_Atreach("CHQ_KKZL", 500));
            chq_inq_section.Add("北长虹渠退水闸", AtReach.Get_Atreach("BCHQ", 11365));
            chq_inq_section.Add("南长虹渠退水闸", AtReach.Get_Atreach("NCHQ", 12912));
            chq.InQ_Section_List = chq_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> chq_outq_section = new Dictionary<string, AtReach>();
            chq_outq_section.Add("长虹渠退水闸", AtReach.Get_Atreach("CHQ", 21999));
            chq.OutQ_Section_List = chq_outq_section;
            chq.Level_Atreach = AtReach.Get_Atreach("CHQ_KKZL", 20000);
            chq.Only_FhyPd_State = true;

            //柳围坡
            Xzhq_Info lwp = Xzhq_Info.Find(p => p.Stcd == "LWP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> lwp_inq_section = new Dictionary<string, AtReach>();
            lwp_inq_section.Add("宋村分洪堰", AtReach.Get_Atreach("SC_KKZL", 500));
            lwp.InQ_Section_List = lwp_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> lwp_outq_section = new Dictionary<string, AtReach>();
            lwp_outq_section.Add("北长虹渠退水闸", AtReach.Get_Atreach("BCHQ", 11365));
            lwp_outq_section.Add("南长虹渠退水闸", AtReach.Get_Atreach("NCHQ", 12912));
            lwp.OutQ_Section_List = lwp_outq_section;
            lwp.Level_Atreach = AtReach.Get_Atreach("SC_KKZL", 14540);
            lwp.Only_FhyPd_State = true;

            //小滩坡
            Xzhq_Info xtp = Xzhq_Info.Find(p => p.Stcd == "XTP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> xtp_inq_section = new Dictionary<string, AtReach>();
            xtp_inq_section.Add("圈里分洪堰", AtReach.Get_Atreach("QL_KKZL", 500));
            xtp.InQ_Section_List = xtp_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> xtp_outq_section = new Dictionary<string, AtReach>();
            xtp_outq_section.Add("浚内沟退水闸", AtReach.Get_Atreach("XNG", 18664));
            xtp_outq_section.Add("苏村沟退水闸", AtReach.Get_Atreach("SCG", 168));
            xtp.OutQ_Section_List = xtp_outq_section;
            xtp.Level_Atreach = AtReach.Get_Atreach("QL_KKZL", 15000);
            xtp.Only_FhyPd_State = true;

            //崔家桥
            Xzhq_Info cjq = Xzhq_Info.Find(p => p.Stcd == "CJQ_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> cjq_inq_section = new Dictionary<string, AtReach>();
            cjq_inq_section.Add("曹马分洪堰", AtReach.Get_Atreach("CM_KKZL", 500));
            cjq_inq_section.Add("郭盆分洪堰", AtReach.Get_Atreach("GP_KKZL", 500));
            cjq_inq_section.Add("梨园沟入流", AtReach.Get_Atreach("GQ", 7000));
            cjq.InQ_Section_List = cjq_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> cjq_outq_section = new Dictionary<string, AtReach>();
            cjq_outq_section.Add("梨园沟退水闸", AtReach.Get_Atreach("LYG", 10816));
            cjq_outq_section.Add("王家口退水闸", AtReach.Get_Atreach("WJK_TSZL", 9950));
            cjq.OutQ_Section_List = cjq_outq_section;
            cjq.Level_Atreach = AtReach.Get_Atreach("GP_KKZL", 900);
            cjq.Only_FhyPd_State = true;

            //广润坡 -- //入库断面集合、出库断面集合、蓄滞洪区水位代表位置
            Xzhq_Info grp = Xzhq_Info.Find(p => p.Stcd == "GRP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> grp_inq_section = new Dictionary<string, AtReach>();
            grp_inq_section.Add("双石桥分洪堰", AtReach.Get_Atreach("SSQ_KKZL", 500));
            grp_inq_section.Add("洪河入流", AtReach.Get_Atreach("HH", 23500));
            grp.InQ_Section_List = grp_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> grp_outq_section = new Dictionary<string, AtReach>();
            grp_outq_section.Add("洪河出流", AtReach.Get_Atreach("HH", 35500));
            grp_outq_section.Add("田大晁退水闸", AtReach.Get_Atreach("WGZ_KKZL", 24700));
            grp.OutQ_Section_List = grp_outq_section;
            grp.Level_Atreach = AtReach.Get_Atreach("SSQ_KKZL", 9000);
            grp.Only_FhyPd_State = true;

            //广润坡（一级）
            Xzhq_Info grp1 = Xzhq_Info.Find(p => p.Stcd == "GRP1_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> grp1_inq_section = new Dictionary<string, AtReach>();
            grp1_inq_section.Add("双石桥分洪堰", AtReach.Get_Atreach("SSQ_KKZL", 500));
            grp1_inq_section.Add("洪河入流", AtReach.Get_Atreach("HH", 23500));
            grp1.InQ_Section_List = grp1_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> grp1_outq_section = new Dictionary<string, AtReach>();
            grp1_outq_section.Add("王贵庄分洪堰", AtReach.Get_Atreach("WGZ_KKZL", 500));
            grp1_outq_section.Add("洪河出流", AtReach.Get_Atreach("HH", 35500));
            grp1.OutQ_Section_List = grp1_outq_section;
            grp1.Level_Atreach = AtReach.Get_Atreach("SSQ_KKZL", 9000);
            grp1.Only_FhyPd_State = true;

            //广润坡（二级）
            Xzhq_Info grp2 = Xzhq_Info.Find(p => p.Stcd == "GRP2_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> grp2_inq_section = new Dictionary<string, AtReach>();
            grp2_inq_section.Add("王贵庄分洪堰", AtReach.Get_Atreach("WGZ_KKZL", 500));
            grp2.InQ_Section_List = grp2_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> grp2_outq_section = new Dictionary<string, AtReach>();
            grp2_outq_section.Add("田大晁退水闸", AtReach.Get_Atreach("WGZ_KKZL", 24700));
            grp2.OutQ_Section_List = grp2_outq_section;
            grp2.Level_Atreach = AtReach.Get_Atreach("WGZ_KKZL", 23000);
            grp2.Only_FhyPd_State = true;

            //任固坡
            Xzhq_Info rgp = Xzhq_Info.Find(p => p.Stcd == "RGP_ZHQ");
            //入库断面集合
            Dictionary<string, AtReach> rgp_inq_section = new Dictionary<string, AtReach>();
            rgp_inq_section.Add("北五陵分洪堰", AtReach.Get_Atreach("BWL_KKZL", 600));
            rgp.InQ_Section_List = rgp_inq_section;
            //出库断面集合
            Dictionary<string, AtReach> rgp_outq_section = new Dictionary<string, AtReach>();
            rgp_outq_section.Add("红卫沟退水闸", AtReach.Get_Atreach("HWG", 1087));
            rgp_outq_section.Add("赵王沟退水闸", AtReach.Get_Atreach("ZWG", 144));
            rgp_outq_section.Add("八里沟退水闸", AtReach.Get_Atreach("BLG", 660));
            rgp_outq_section.Add("故城沟退水闸", AtReach.Get_Atreach("GCG", 438));
            rgp.OutQ_Section_List = rgp_outq_section;
            rgp.Level_Atreach = AtReach.Get_Atreach("BWL_KKZL", 9200);
            rgp.Only_FhyPd_State = true;

            Item_Info.Xzhq_Info = Xzhq_Info;
        }

        //初始化河道 控制断面 水位流量关系
        public static Dictionary<AtReach, string> Initial_ReachSectionQHrelation()
        {
            Dictionary<AtReach, string> reach_stcd = new Dictionary<AtReach, string>();
            Dictionary<AtReach, List<double[]>> qhrelation = new Dictionary<AtReach, List<double[]>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.SECTION_QHRELATION;


            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string reach = dt.Rows[i][1].ToString();
                string reach_cnname = dt.Rows[i][2].ToString();
                double chainage = double.Parse(dt.Rows[i][3].ToString());
                string qh_relationstr = dt.Rows[i][4].ToString();
                string stcd = dt.Rows[i][5].ToString();

                AtReach atreach = AtReach.Get_Atreach(reach, chainage);
                List<double[]> q_h = JsonConvert.DeserializeObject<List<double[]>>(qh_relationstr);

                qhrelation.Add(atreach, q_h);
                reach_stcd.Add(atreach, stcd);
            }



            Item_Info.Section_QHrelation = qhrelation;
            return reach_stcd;
        }

        //初始化建筑物 闸前水位和流量关系
        public static void Initial_Struct_QHrelation()
        {
            Dictionary<string, List<double[]>> qhrelation = new Dictionary<string, List<double[]>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.STRUCT_QHRELATION;


            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string cn_name = dt.Rows[i][1].ToString();
                string str_name = dt.Rows[i][2].ToString();
                string type = dt.Rows[i][3].ToString();
                string reach = dt.Rows[i][4].ToString();
                string reach_cnname = dt.Rows[i][5].ToString();
                double chainage = double.Parse(dt.Rows[i][6].ToString());
                string qh_relationstr = dt.Rows[i][7].ToString();

                List<double[]> q_h = JsonConvert.DeserializeObject<List<double[]>>(qh_relationstr);

                qhrelation.Add(str_name, q_h);
            }



            Item_Info.Struct_QHrelation = qhrelation;
        }

        //初始化主河道信息(非退水闸支流等虚拟河道)
        public static void Initial_MainReachInfo(string model_instance)
        {
            Mysql_GlobalVar.now_instance = model_instance;
            HydroModel default_model = HydroModel.Load(Model_Const.DEFAULT_MODELNAME);
            List<ReachInfo> main_reach = new List<ReachInfo>();
            for (int i = 0; i < default_model.Mike11Pars.ReachList.Reach_infolist.Count; i++)
            {
                string reach_name = default_model.Mike11Pars.ReachList.Reach_infolist[i].reachname;
                if (!reach_name.EndsWith("TSZZL") && !reach_name.EndsWith("KKZL"))
                {
                    main_reach.Add(default_model.Mike11Pars.ReachList.Reach_infolist[i]);
                }
            }
            Item_Info.Main_ReachInfo = main_reach;
        }
        #endregion *******************************************************


        #region ********************* 部分主要属性获取 ************************
        //获取当前项目断面原始数据
        public static SectionList Get_Item_SectionDatas(string model_instance)
        {
            //如果给定的项目名为空字符串，则采用全局的项目名
            int n = 0;
            if (model_instance == "")
            {
                while (true)
                {
                    if (model_instance != "" || n >= 20) break;
                    Thread.Sleep(500);
                    model_instance = Mysql_GlobalVar.now_instance;
                }
            }

            //如果项目全局网格为空，则根据项目名更新全局网格
            if (Item_Info.Item_SectionDatas == null) Update_Item_SectionDatas(model_instance);

            //如果不包括当前项目的网格，则从数据库读取当前项目的网格
            if (!Item_SectionDatas.Keys.Contains(model_instance)) Update_Item_SectionDatas(model_instance);
            SectionList item_sectionlist = Item_SectionDatas[model_instance];

            return item_sectionlist;
        }

        //获取所有建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Get_StrBaseInfo()
        {
            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();

            return Item_Info.Str_BaseInfo;
        }

        //获取指定建筑物基本信息
        public static Struct_BasePars Get_StrBaseInfo(string str_name)
        {
            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();

            Struct_BasePars str_par = new Struct_BasePars();
            if (Item_Info.Str_BaseInfo.Keys.Contains(str_name))
            {
                str_par = Item_Info.Str_BaseInfo[str_name];
            }

            return str_par;
        }

        //根据指定模型实例获取建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Get_StrBaseInfo_ByModelInstance(string model_instance)
        {
            Dictionary<string, Struct_BasePars> res = new Dictionary<string, Struct_BasePars>();

            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();

            for (int i = 0; i < Item_Info.Str_BaseInfo.Count; i++)
            {
                string str_name = Item_Info.Str_BaseInfo.ElementAt(i).Key;
                Struct_BasePars str_par = Item_Info.Str_BaseInfo.ElementAt(i).Value;
                if (str_par.model_instances.Contains(model_instance))
                {
                    res.Add(str_name, str_par);
                }
            }

            return res;
        }

        //获取指定业务模型获取建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Get_StrBaseInfo_ByModelBusiness(string business_code)
        {
            Dictionary<string, Struct_BasePars> res = new Dictionary<string, Struct_BasePars>();
            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();

            Dictionary<string, List<string>> business_instance = HydroModel.Get_BusinessModel_ModelInstance_Relation();
            if (!business_instance.Keys.Contains(business_code)) return null;

            List<string> model_instances = business_instance[business_code];
            for (int i = 0; i < Item_Info.Str_BaseInfo.Count; i++)
            {
                string str_name = Item_Info.Str_BaseInfo.ElementAt(i).Key;
                Struct_BasePars str_par = Item_Info.Str_BaseInfo.ElementAt(i).Value;
                for (int j = 0; j < model_instances.Count; j++)
                {
                    if (str_par.model_instances.Contains(model_instances[j]))
                    {
                        res.Add(str_name, str_par);
                    }
                }
            }

            return res;
        }

        //获取各建筑的中文名
        public static string Get_StrChinaName(string str_en_name)
        {
            Struct_BasePars str_baseinfo = Get_StrBaseInfo(str_en_name);
            return str_baseinfo.cn_name;
        }

        //获取各建筑的英文名
        public static string Get_StrEnglishName(string str_cn_name, string cn_type, string cn_reach)
        {
            string en_name = "";
            for (int i = 0; i < Str_BaseInfo.Count; i++)
            {
                if (Str_BaseInfo.ElementAt(i).Value.cn_name == str_cn_name &&
                    Struct_BasePars.Get_StrCNType(Str_BaseInfo.ElementAt(i).Value.gate_type) == cn_type && Str_BaseInfo.ElementAt(i).Value.reach_cnname == cn_reach)
                {
                    en_name = Str_BaseInfo.ElementAt(i).Key;
                    break;
                }
            }
            return en_name;
        }

        //从模型中获取初始水情信息(初始化默认模型时使用)
        public static string Get_Model_InitialInfo(HydroModel model)
        {
            Dictionary<string, List<List<object>>> res = new Dictionary<string, List<List<object>>>();

            //从数据库中读取全部实时水位数据
            List<Water_Condition> now_water = Hd11.Read_NowWater();

            //从中刷选出有监测数据的水库水文站点水位
            Dictionary<string, double> res_levels = new Dictionary<string, double>();
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Datasource == "水库水文站" && now_water[i].Level != 0 && now_water[i].Update_Date != null)
                {
                    res_levels.Add(now_water[i].Name, now_water[i].Level);
                }
            }

            //获取各水库控制的河道断面
            Dictionary<string, List<AtReach>> res_initialid = Item_Info.GetRes_ControlInitialAtreach();

            //获取所有初始条件集合
            Dictionary<AtReach, double> initial_waterlevel = model.Mike11Pars.Hd_Pars.InitialWater;

            //逐个增加水库的初始水位
            List<List<object>> res_waterlevel = new List<List<object>>();
            for (int i = 0; i < res_initialid.Count; i++)
            {
                List<object> res_res = new List<object>();
                //从初始条件中获得水库水位
                string res_name = res_initialid.ElementAt(i).Key;
                List<AtReach> res_sections = res_initialid.ElementAt(i).Value;
                double level = 0;
                if (initial_waterlevel.Keys.Contains(res_sections[0])) level = initial_waterlevel[res_sections[0]];

                //与监测数据对比判断是否来自于最新监测
                bool isjc = false;
                if (res_levels.Keys.Contains(res_name) && res_levels[res_name] == level) isjc = true;
                string source = isjc ? "最新监测水位" : "人为指定";

                res_res.Add(i + 1);
                res_res.Add(res_name);
                res_res.Add(level);
                res_res.Add(source);
                res_waterlevel.Add(res_res);
            }
            res.Add("水库", res_waterlevel);

            //从中刷选出有监测数据的河道站点和闸站水位
            List<List<object>> station_levels = new List<List<object>>();
            int n = 1;
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Datasource != "水库水文站" && now_water[i].Level != 0 && now_water[i].Update_Date != null)
                {
                    List<object> reach_res = new List<object>();
                    reach_res.Add(n);
                    reach_res.Add(now_water[i].Name);
                    reach_res.Add(now_water[i].Level);
                    reach_res.Add("最新监测水位");
                    n++;

                    station_levels.Add(reach_res);
                }
            }
            res.Add("河道", station_levels);

            string res_str = File_Common.Serializer_Obj(res);
            return res_str;
        }

        //从模型中获取初始水情信息(人为指定初始条件时使用)
        public static string Get_Model_InitialInfo(HydroModel model, Dictionary<string, double> inital_level)
        {
            Dictionary<string, List<List<object>>> res = new Dictionary<string, List<List<object>>>();

            //从数据库中读取全部实时水位数据
            List<Water_Condition> now_water = Hd11.Read_NowWater(Mysql_GlobalVar.now_instance);

            //从中刷选出有监测数据的水库水文站点水位
            Dictionary<string, double> res_levels = new Dictionary<string, double>();
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Datasource == "水库水文站" && now_water[i].Level != 0 && now_water[i].Update_Date != null)
                {
                    res_levels.Add(now_water[i].Name, now_water[i].Level);
                }
            }

            //获取各水库控制的河道断面
            Dictionary<string, List<AtReach>> res_initialid = Item_Info.GetRes_ControlInitialAtreach();

            //获取所有初始条件集合
            Dictionary<AtReach, double> initial_waterlevel = model.Mike11Pars.Hd_Pars.InitialWater;

            //逐个增加水库的初始水位
            List<List<object>> res_waterlevel = new List<List<object>>();
            for (int i = 0; i < res_initialid.Count; i++)
            {
                List<object> res_res = new List<object>();

                //从初始条件中获得水库水位
                string res_name = res_initialid.ElementAt(i).Key;
                List<AtReach> res_sections = res_initialid.ElementAt(i).Value;
                double level = 0;
                if (initial_waterlevel.Keys.Contains(res_sections[0])) level = initial_waterlevel[res_sections[0]];

                //与监测数据对比判断是否来自于最新监测
                if (!res_levels.Keys.Contains(res_name)) continue;

                bool isjc = false;
                if (Math.Abs(res_levels[res_name] - level) < 0.1) isjc = true;
                string source = isjc ? "最新监测水位" : "人为指定";

                res_res.Add(i + 1);
                res_res.Add(res_name);
                res_res.Add(level);
                res_res.Add(source);
                string res_stcd = Water_Condition.Get_Stcd(now_water, res_name);
                res_res.Add(res_stcd);
                res_waterlevel.Add(res_res);
            }
            res.Add("水库", res_waterlevel);

            //通过实时水情与给定初始水情stcd对比，从给定的初始水情中刷选出河道的初始水情
            Dictionary<string, double> reach_level = new Dictionary<string, double>();
            for (int i = 0; i < inital_level.Count; i++)
            {
                string stcd = inital_level.ElementAt(i).Key;
                double level = inital_level.ElementAt(i).Value;
                for (int j = 0; j < now_water.Count; j++)
                {
                    if (now_water[j].Stcd == stcd && now_water[j].Datasource != "水库水文站" && !reach_level.Keys.Contains(now_water[j].Name))
                    {
                        reach_level.Add(now_water[j].Name, level);
                        break;
                    }
                }
            }

            //从监测水情中刷选出河道的初始水情
            Dictionary<string, double> reach_station_levels = new Dictionary<string, double>();
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Datasource != "水库水文站" && now_water[i].Level != 0 && now_water[i].Update_Date != null && !reach_station_levels.Keys.Contains(now_water[i].Name))
                {
                    reach_station_levels.Add(now_water[i].Name, now_water[i].Level);
                }
            }

            //对比和判断来源
            List<List<object>> station_levels = new List<List<object>>();
            int n = 1;
            for (int i = 0; i < reach_level.Count; i++)
            {
                //从初始条件中获得水库水位
                string station_name = reach_level.ElementAt(i).Key;
                double station_level = reach_level.ElementAt(i).Value;

                if (station_level != 0)
                {
                    List<object> reach_res = new List<object>();

                    //与监测数据对比判断是否来自于最新监测
                    bool isjc = false;
                    if (reach_station_levels.Keys.Contains(station_name) && Math.Abs(reach_station_levels[station_name] - station_level) < 0.1) isjc = true;
                    string source = isjc ? "最新监测水位" : "人为指定";

                    reach_res.Add(n);
                    reach_res.Add(station_name);
                    reach_res.Add(station_level);
                    reach_res.Add(source);

                    string station_stcd = Water_Condition.Get_Stcd(now_water, station_name);
                    reach_res.Add(station_stcd);

                    n++;
                    station_levels.Add(reach_res);
                }
            }
            res.Add("河道", station_levels);

            string res_str = File_Common.Serializer_Obj(res);
            return res_str;
        }

        //获取 指定模型 的初始条件\边界条件、闸站调度等单一参数信息(从数据库，经常性查询)
        public static string Get_Model_SingleParInfo(string plan_code, string field_name)
        {
            //定义连接的数据库和表
            string moel_instance = Mysql_GlobalVar.now_instance;
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select " + field_name + " from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //初始水情信息
            string res = dt.Rows[0][0].ToString();

            return res;
        }

        #endregion *******************************************************
    }
}
