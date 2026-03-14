using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike21;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kdbndp;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace bjd_model.Mike11
{
    public partial class Item_Info
    {        
        //获取全部水库水位站控制的一维河道HD11中的河道断面(键为水库水情监测断面名称，值为控制的河段集合)
        public static Dictionary<string, List<AtReach>> GetRes_ControlInitialAtreach()
        {
            Dictionary<string, List<AtReach>> res = new Dictionary<string, List<AtReach>>();

            res.Add("陈家院水库", new List<AtReach>() { AtReach.Get_Atreach("QH", 0), AtReach.Get_Atreach("QH", 948)});
            res.Add("三郊口水库", new List<AtReach>() {AtReach.Get_Atreach("QH", 950), AtReach.Get_Atreach("QH", 5794)});
            res.Add("盘石头水库", new List<AtReach>() {AtReach.Get_Atreach("QH",56000), AtReach.Get_Atreach("QH", 75400),
                AtReach.Get_Atreach("PSTSK_YHD", 0), AtReach.Get_Atreach("PSTSK_YHD", 600)});
            res.Add("小南海水库", new List<AtReach>() {AtReach.Get_Atreach("AYH",36300), AtReach.Get_Atreach("AYH", 44910),
                AtReach.Get_Atreach("XNHSK_YHD", 0), AtReach.Get_Atreach("XNHSK_YHD", 420)});
            res.Add("彰武水库", new List<AtReach>() {AtReach.Get_Atreach("AYH",45000), AtReach.Get_Atreach("AYH", 54696),
                AtReach.Get_Atreach("ZWSK_XHD", 0), AtReach.Get_Atreach("ZWSK_XHD", 187)});
            res.Add("群英水库", new List<AtReach>() { AtReach.Get_Atreach("DSH", 0), AtReach.Get_Atreach("DSH", 2321)});
            res.Add("马鞍石水库", new List<AtReach>() { AtReach.Get_Atreach("ZFGH", 0), AtReach.Get_Atreach("ZFGH", 3465)});
            res.Add("石门水库(辉县)", new List<AtReach>() { AtReach.Get_Atreach("SMH", 0), AtReach.Get_Atreach("SMH", 4334)});
            res.Add("石门水库(安阳)", new List<AtReach>() { AtReach.Get_Atreach("QHZL", 0), AtReach.Get_Atreach("QHZL", 2140)});
            res.Add("弓上水库", new List<AtReach>() { AtReach.Get_Atreach("XIHE", 0), AtReach.Get_Atreach("XIHE", 3670)});
            res.Add("琵琶寺水库", new List<AtReach>() { AtReach.Get_Atreach("TYH", 0), AtReach.Get_Atreach("TYH", 3682) });
            res.Add("汤河水库", new List<AtReach>() { AtReach.Get_Atreach("TH", 0), AtReach.Get_Atreach("TH", 7394)});
            res.Add("双泉水库", new List<AtReach>() { AtReach.Get_Atreach("FHJ", 0), AtReach.Get_Atreach("FHJ", 2960) });
            res.Add("夺丰水库", new List<AtReach>() { AtReach.Get_Atreach("SDH", 0), AtReach.Get_Atreach("SDH", 2473)});
            res.Add("宝泉水库", new List<AtReach>() { AtReach.Get_Atreach("YH", 0), AtReach.Get_Atreach("YH", 7402)});
            res.Add("正面水库", new List<AtReach>() { AtReach.Get_Atreach("CH", 0), AtReach.Get_Atreach("CH", 8085)});
            res.Add("狮豹头水库", new List<AtReach>() { AtReach.Get_Atreach("CH", 8090), AtReach.Get_Atreach("CH", 15743)});
            res.Add("塔岗水库", new List<AtReach>() { AtReach.Get_Atreach("CH", 15750), AtReach.Get_Atreach("CH", 28191)});
            res.Add("孤山湖水库", new List<AtReach>() { AtReach.Get_Atreach("DSH", 4500), AtReach.Get_Atreach("DSH", 8420) });
            return res;
        }

        //获取流域部分水库水位站控制的一维河道HD11中初始条件序号
        public static Dictionary<string, List<AtReach>> GetRes_ControlInitialAtreach(List<string> res_list)
        {
            Dictionary<string, List<AtReach>> res = new Dictionary<string, List<AtReach>>();
            Dictionary<string, List<AtReach>> res_initialid = GetRes_ControlInitialAtreach();
            for (int i = 0; i < res_initialid.Count; i++)
            {
                string res_name = res_initialid.ElementAt(i).Key;

                if (res_list.Contains(res_name))
                {
                    res.Add(res_name, res_initialid.ElementAt(i).Value);
                }
            }

            return res;
        }

        //从模型中获取各水库的初始水位值
        public static Dictionary<string,double> GetRes_InitialValue_FromModel(HydroModel hydromodel)
        {
            Dictionary<string, double> res_initial_level = new Dictionary<string, double>();

            //模型所有初始水位
            Dictionary<AtReach, double> initial_water = hydromodel.Mike11Pars.Hd_Pars.InitialWater;

            //水库初始水位控制的河道断面
            Dictionary<string, List<AtReach>> res_atreachs = Item_Info.GetRes_ControlInitialAtreach();

            //遍历获取水库
            for (int i = 0; i < res_atreachs.Count; i++)
            {
                string res_name = res_atreachs.ElementAt(i).Key;
                AtReach atreach = res_atreachs.ElementAt(i).Value[0];
                double res_level = initial_water.Keys.Contains(atreach)? initial_water[atreach]:0;
                res_initial_level.Add(res_name, res_level);
            }

            return res_initial_level;
        }

        //从模型中获取水库的初始水位值
        public static double GetRes_InitialValue_FromModel(HydroModel hydromodel, string res)
        {
            double level = 0;

            //模型所有初始水位
            Dictionary<AtReach, double> initial_water = hydromodel.Mike11Pars.Hd_Pars.InitialWater;

            //水库初始水位控制的河道断面
            Dictionary<string, List<AtReach>> res_atreachs = Item_Info.GetRes_ControlInitialAtreach();

            //遍历获取水库
            for (int i = 0; i < res_atreachs.Count; i++)
            {
                string res_name = res_atreachs.ElementAt(i).Key;
                AtReach atreach = res_atreachs.ElementAt(i).Value[0];
                double res_level = initial_water.Keys.Contains(atreach) ? initial_water[atreach] : 0;
                if (res_name == res)
                {
                    level = res_level;
                    break;
                }
            }

            return level;
        }

        //从数据库中获取水库的初始水位
        public static double GetRes_InitialValue_FromDB(string plan_code, string res_name)
        {
            double initial_level = 0;
            string initial_info = HydroModel.Get_Model_InitialLevel(plan_code);
            Dictionary<string, List<List<object>>> initial = JsonConvert.DeserializeObject<Dictionary<string, List<List<object>>>>(initial_info);
            List<List<object>> res_initial = initial.Keys.Contains("水库") ? initial["水库"] : null;
            if (res_initial == null) return 0;

            for (int i = 0; i < res_initial.Count; i++)
            {
                if (res_initial[i][1].ToString() == res_name)
                {
                    initial_level = double.Parse(res_initial[i][2].ToString());
                    break;
                }
            }

            return initial_level;
        }

        //获取全部水库出流代表断面位置(第1个为溢洪道，第2个为泄洪洞(没有则表示没有泄洪洞))
        public static Dictionary<string, AtReach[]> GetRes_OutQAtreach(string model_instance)
        {
            Dictionary<string, AtReach[]> outq_atreach = new Dictionary<string, AtReach[]>();

            outq_atreach.Add("陈家院水库", new AtReach[] { AtReach.Get_Atreach("QH", 948) });
            outq_atreach.Add("三郊口水库", new AtReach[] {AtReach.Get_Atreach("QH", 5794) });

            outq_atreach.Add("盘石头水库", new AtReach[] { AtReach.Get_Atreach("PSTSK_YHD", 600), AtReach.Get_Atreach("QH", 75400)});
            outq_atreach.Add("小南海水库", new AtReach[] { AtReach.Get_Atreach("XNHSK_YHD", 420),AtReach.Get_Atreach("AYH", 44910)});
            outq_atreach.Add("彰武水库", new AtReach[] { AtReach.Get_Atreach("ZWSK_YHD", 54557.5), AtReach.Get_Atreach("ZWSK_XHD", 187)});

            outq_atreach.Add("群英水库", new AtReach[] { AtReach.Get_Atreach("DSH", 2321) });
            outq_atreach.Add("马鞍石水库", new AtReach[] { AtReach.Get_Atreach("ZFGH", 3465) });
            outq_atreach.Add("石门水库(辉县)", new AtReach[] { AtReach.Get_Atreach("SMH", 4334) });
            outq_atreach.Add("石门水库(安阳)", new AtReach[] { AtReach.Get_Atreach("QHZL", 2140) });
            outq_atreach.Add("弓上水库", new AtReach[] {  AtReach.Get_Atreach("XIHE", 3670) });
            outq_atreach.Add("琵琶寺水库", new AtReach[] { AtReach.Get_Atreach("TYH", 3682) });
            outq_atreach.Add("汤河水库", new AtReach[] { AtReach.Get_Atreach("TH", 7394) });
            outq_atreach.Add("双泉水库", new AtReach[] {  AtReach.Get_Atreach("FHJ", 2960) });
            outq_atreach.Add("夺丰水库", new AtReach[] {  AtReach.Get_Atreach("SDH", 2473) });
            outq_atreach.Add("宝泉水库", new AtReach[] { AtReach.Get_Atreach("YH", 7402) });
            outq_atreach.Add("正面水库", new AtReach[] { AtReach.Get_Atreach("CH", 8085) });
            outq_atreach.Add("狮豹头水库", new AtReach[] {  AtReach.Get_Atreach("CH", 15743) });
            outq_atreach.Add("塔岗水库", new AtReach[] {  AtReach.Get_Atreach("CH", 28191) });

            outq_atreach.Add("孤山湖水库", new AtReach[] { AtReach.Get_Atreach("DSH", 8420) });
            return outq_atreach;
        }

        //获取全部水库入流边界ID和额外入流断面位置(位于河道中段的水库既有入流边界，也有上游断面入流)
        public static Dictionary<string, Res_InSection_BndID> Get_ResInFlow_SectionBndID(string model_instance)
        {
            Dictionary<string, Res_InSection_BndID> res_inflow = new Dictionary<string, Res_InSection_BndID>();
            List<string> cjysk_inbnd = new List<string>() { "qh_sjk_up" };
            List<string> sjksk_inbnd = new List<string>() { "qh_sjk_up" };
            List<AtReach> pstsk_upInflow = new List<AtReach>() { AtReach.Get_Atreach("QH", 59500) };
            List<string> pstsk_inbnd = new List<string>() { "qh_xh_pst" };
            List<AtReach> xnhsk_upInflow = new List<AtReach>() { AtReach.Get_Atreach("AYH", 36300) };
            List<string> xnhsk_inbnd = new List<string>() { "ayh_xzh_xnh" };
            List<AtReach> zwsk_upInflow = new List<AtReach>() { AtReach.Get_Atreach("AYH", 46000) };
            List<string> zwsk_inbnd = new List<string>() { "ayh_xnh_zwsk" };
            List<string> qysk_inbnd = new List<string>() { "dsh_qysk_up" };
            List<string> massk_inbnd = new List<string>() { "zfh_mas_up" };
            List<string> hxsmsk_inbnd = new List<string>() { "smh_smsk_up" };
            List<string> aysmsk_inbnd = new List<string>() { "qh_smsk" };
            List<string> gssk_inbnd = new List<string>() { "xh_gssk_up" };
            List<string> ppssk_inbnd = new List<string>() { "tyh_ppsk_up" };
            List<string> thsk_inbnd = new List<string>() { "th_thsk_up" };
            List<string> sqsk_inbnd = new List<string>() { "fhj_sqsk_up" };
            List<string> dfsk_inbnd = new List<string>() { "sdh_dfsk_up" };
            List<string> bqsk_inbnd = new List<string>() { "yh_bqsk_up" };
            List<string> zmsk_inbnd = new List<string>() { "ch_zmsk_up" };
            List<AtReach> sbtsk_upInflow = new List<AtReach>() { AtReach.Get_Atreach("CH", 8085) };
            List<string> sbtsk_inbnd = new List<string>() { "ch_zmsk_sbt" };
            List<AtReach> tgsk_upInflow = new List<AtReach>() { AtReach.Get_Atreach("CH", 15743) };
            List<string> tgsk_inbnd = new List<string>() { "ch_sbt_tgsk" };

            List<AtReach> gshsk_upInflow = new List<AtReach>() { AtReach.Get_Atreach("DSH", 2321) };

            Res_InSection_BndID cjysk_resin = new Res_InSection_BndID(cjysk_inbnd);
            Res_InSection_BndID sjksk_resin = new Res_InSection_BndID(sjksk_inbnd);
            Res_InSection_BndID pstsk_resin = new Res_InSection_BndID(pstsk_upInflow, pstsk_inbnd);
            Res_InSection_BndID xnhsk_resin = new Res_InSection_BndID(xnhsk_upInflow, xnhsk_inbnd);
            Res_InSection_BndID zwsk_resin = new Res_InSection_BndID(zwsk_upInflow, zwsk_inbnd);
            Res_InSection_BndID qysk_resin = new Res_InSection_BndID(qysk_inbnd);
            Res_InSection_BndID massk_resin = new Res_InSection_BndID(massk_inbnd);
            Res_InSection_BndID hxsmsk_resin = new Res_InSection_BndID(hxsmsk_inbnd);
            Res_InSection_BndID aysmsk_resin = new Res_InSection_BndID(aysmsk_inbnd);

            Res_InSection_BndID gssk_resin = new Res_InSection_BndID(gssk_inbnd);
            Res_InSection_BndID ppssk_resin = new Res_InSection_BndID(ppssk_inbnd);
            Res_InSection_BndID thsk_resin = new Res_InSection_BndID(thsk_inbnd);
            Res_InSection_BndID sqsk_resin = new Res_InSection_BndID(sqsk_inbnd);
            Res_InSection_BndID dfsk_resin = new Res_InSection_BndID(dfsk_inbnd);
            Res_InSection_BndID bqsk_resin = new Res_InSection_BndID(bqsk_inbnd);
            Res_InSection_BndID zmsk_resin = new Res_InSection_BndID(zmsk_inbnd);
            Res_InSection_BndID sbtsk_resin = new Res_InSection_BndID(sbtsk_upInflow, sbtsk_inbnd);
            Res_InSection_BndID tgsk_resin = new Res_InSection_BndID(tgsk_upInflow, tgsk_inbnd);
            Res_InSection_BndID gshsk_resin = new Res_InSection_BndID(gshsk_upInflow);

            res_inflow.Add("陈家院水库", cjysk_resin);
            res_inflow.Add("三郊口水库", sjksk_resin);
            res_inflow.Add("盘石头水库", pstsk_resin);
            res_inflow.Add("小南海水库", xnhsk_resin);
            res_inflow.Add("彰武水库", zwsk_resin);
            res_inflow.Add("群英水库", qysk_resin);
            res_inflow.Add("马鞍石水库", massk_resin);
            res_inflow.Add("石门水库(辉县)", hxsmsk_resin);
            res_inflow.Add("石门水库(安阳)", aysmsk_resin);
            res_inflow.Add("弓上水库", gssk_resin);
            res_inflow.Add("琵琶寺水库", ppssk_resin);
            res_inflow.Add("汤河水库", thsk_resin);
            res_inflow.Add("双泉水库", sqsk_resin);
            res_inflow.Add("夺丰水库", dfsk_resin);
            res_inflow.Add("宝泉水库", bqsk_resin);
            res_inflow.Add("正面水库", zmsk_resin);
            res_inflow.Add("狮豹头水库", sbtsk_resin);
            res_inflow.Add("塔岗水库", tgsk_resin);
            res_inflow.Add("孤山湖水库", gshsk_resin);

            return res_inflow;
        }

        //获取全部水库的区间子流域ID和子流域距水库距离(不包含上游水库区间)
        public static Dictionary<string, Dictionary<string,double>> Get_Res_Subcatchment_InFlow(string model_instance)
        {
            Dictionary<string,Dictionary<string, double>> res_inflow = new Dictionary<string, Dictionary<string, double>>();
             Dictionary<string,double> cjysk_inbnd = new Dictionary<string,double>() { { "zjq_up", 2000 } };
             Dictionary<string,double> sjksk_inbnd = new Dictionary<string,double>() { { "qh_sjk_up", 2000 } };
             Dictionary<string,double> pstsk_inbnd = new Dictionary<string,double>() {
                 { "qh_sjk_hdh", 40000},{ "qh_hdh_xh",20500},{ "xh_gssk_down", 16000 },{ "qh_xh_pst",2000}
             };

             Dictionary<string,double> xnhsk_inbnd = new Dictionary<string,double>()  {
                 { "ayh_ly_up", 35000},{ "ayh_ly_hs",27000},{ "ayh_hs_mzh", 18000 },
                 { "ayh_mzh_xnh", 10000},{ "xzh_all",9000},{ "ayh_xzh_xnh", 2000},
             };
             Dictionary<string,double> zwsk_inbnd = new Dictionary<string,double>() { { "ayh_xnh_zwsk", 2000 } };
             Dictionary<string,double> qysk_inbnd = new Dictionary<string,double>() { { "dsh_qysk_up", 2000 } };
             Dictionary<string,double> massk_inbnd = new Dictionary<string,double>() { { "zfh_mas_up", 2000 } };
             Dictionary<string,double> hxsmsk_inbnd = new Dictionary<string,double>() { { "smh_smsk_up", 2000 } };
             Dictionary<string,double> aysmsk_inbnd = new Dictionary<string,double>() { { "qh_smsk", 2000} };
             Dictionary<string,double> gssk_inbnd = new Dictionary<string,double>() { { "xh_gssk_up", 2000 } };
             Dictionary<string,double> ppssk_inbnd = new Dictionary<string,double>() { { "tyh_ppsk_up", 2000} };
             Dictionary<string,double> thsk_inbnd = new Dictionary<string,double>() { { "th_thsk_up", 2000 } };
             Dictionary<string,double> sqsk_inbnd = new Dictionary<string,double>() { { "fhj_sqsk_up", 2000 } };
             Dictionary<string,double> dfsk_inbnd = new Dictionary<string,double>() { { "sdh_dfsk_up", 2000 } };
             Dictionary<string,double> bqsk_inbnd = new Dictionary<string,double>() { { "yh_bqsk_up", 2000} };
             Dictionary<string,double> zmsk_inbnd = new Dictionary<string,double>() { { "ch_zmsk_up", 2000} };
             Dictionary<string,double> sbtsk_inbnd = new Dictionary<string,double>() { { "ch_zmsk_sbt", 2000 } };
             Dictionary<string,double> tgsk_inbnd = new Dictionary<string,double>() { { "ch_sbt_tgsk",2000 } };

            res_inflow.Add("陈家院水库", cjysk_inbnd);
            res_inflow.Add("三郊口水库", sjksk_inbnd);
            res_inflow.Add("盘石头水库", pstsk_inbnd);
            res_inflow.Add("小南海水库", xnhsk_inbnd);
            res_inflow.Add("彰武水库", zwsk_inbnd);
            res_inflow.Add("群英水库", qysk_inbnd);
            res_inflow.Add("马鞍石水库", massk_inbnd);
            res_inflow.Add("石门水库(辉县)", hxsmsk_inbnd);
            res_inflow.Add("石门水库(安阳)", aysmsk_inbnd);
            res_inflow.Add("弓上水库", gssk_inbnd);
            res_inflow.Add("琵琶寺水库", ppssk_inbnd);
            res_inflow.Add("汤河水库", thsk_inbnd);
            res_inflow.Add("双泉水库", sqsk_inbnd);
            res_inflow.Add("夺丰水库", dfsk_inbnd);
            res_inflow.Add("宝泉水库", bqsk_inbnd);
            res_inflow.Add("正面水库", zmsk_inbnd);
            res_inflow.Add("狮豹头水库", sbtsk_inbnd);
            res_inflow.Add("塔岗水库", tgsk_inbnd);
            return res_inflow;
        }

        //获取部分中间水库的上游水库信息 -- 用于库群优化调度中的入库洪水计算
        public static Dictionary<string,Dictionary<string,double>> Get_Res_UpResList(string model_instance)
        {
            Dictionary<string, Dictionary<string, double>> res_upres = new Dictionary<string, Dictionary<string, double>>();
            res_upres.Add("陈家院水库", null);
            res_upres.Add("三郊口水库", new Dictionary<string, double> { { "陈家院水库", 5000} });
            res_upres.Add("盘石头水库", new Dictionary<string, double> {
                { "三郊口水库", 63000 },{ "弓上水库", 45000},{ "石门水库(安阳)", 33000} });
            res_upres.Add("小南海水库", null);
            res_upres.Add("彰武水库", new Dictionary<string, double> { { "小南海水库", 10000 } });
            res_upres.Add("群英水库", null);
            res_upres.Add("马鞍石水库", null);
            res_upres.Add("石门水库(辉县)", null);
            res_upres.Add("石门水库(安阳)", null);
            res_upres.Add("弓上水库", null);
            res_upres.Add("琵琶寺水库", null);
            res_upres.Add("汤河水库", null);
            res_upres.Add("双泉水库", null);
            res_upres.Add("夺丰水库", null);
            res_upres.Add("宝泉水库", null);
            res_upres.Add("正面水库", null);
            res_upres.Add("狮豹头水库", new Dictionary<string, double> { { "正面水库", 7000} });
            res_upres.Add("塔岗水库", new Dictionary<string, double> { { "狮豹头水库", 12000 } });

            return res_upres;
        }

        //从数据库获取水库溢洪道和泄洪洞建筑(如果没有的话，则为空)
        public static Struct_BasePars Get_Res_YHDXHD_StrInfo(string res_name, string str_type)
        {
            Struct_BasePars str_info = null;

            List<Struct_BasePars> str_list = new List<Struct_BasePars>();

            //获取所有建筑物基本信息
            Dictionary<string, Struct_BasePars> str_infos = Item_Info.Get_StrBaseInfo();

            //获取该水库的泄洪建筑物
            string str_name = ""; 
            for (int i = 0; i < str_infos.Count; i++)
            {
                Struct_BasePars str = str_infos.ElementAt(i).Value;
                if (str.cn_name.Contains(res_name) && str.str_type == str_type)
                {
                    str_name = str_infos.ElementAt(i).Key;
                    str_info = str;
                    str_list.Add(str_info);
                }
            }
            if (str_name == "") return null;

            //多个的情况，选其中底高程低的
            Struct_BasePars lowdatumn_str_info = str_list[0];
            for (int i = 0; i < str_list.Count; i++)
            {
                if (str_list[i].datumn < lowdatumn_str_info.datumn) lowdatumn_str_info = str_list[i];
            }

            return lowdatumn_str_info;
        }

        //从数据库获取水库的所有建筑物信息
        public static List<Struct_BasePars> Get_Res_All_StrInfo(string res_name)
        {
            List<Struct_BasePars> str_list = new List<Struct_BasePars>();

            //获取所有建筑物基本信息
            Dictionary<string, Struct_BasePars> str_infos = Item_Info.Get_StrBaseInfo();

            //获取该水库的泄洪建筑物
            string str_name = "";
            for (int i = 0; i < str_infos.Count; i++)
            {
                Struct_BasePars str = str_infos.ElementAt(i).Value;
                if (str.cn_name.Contains(res_name) )
                {
                    str_name = str_infos.ElementAt(i).Key;
                    str_list.Add(str);
                }
            }

            return str_list;
        }

        //获取模型实例的所有水库基本信息
        public static List<Reservoir> Get_ResInfo(string model_instance)
        {
            List<Reservoir> res_info = new List<Reservoir>();

            //初始化所有水库的基础信息
            if (Item_Info.Reservoir_Info == null) Initial_ResInfo();
            if (Item_Info.Reservoir_Info == null) return null;

            //获取监测站点和模型实例对应关系
            List<string> stations = Get_StationStcd_FromModelInstance(model_instance);

            //遍历
            for (int i = 0; i < Item_Info.Reservoir_Info.Count; i++)
            {
                Reservoir res = Item_Info.Reservoir_Info[i];
                if (stations.Contains(res.Stcd)) res_info.Add(res);
            }
            return res_info;
        }

        public static List<Reach_Segment> CombineAtReachs(List<AtReach> contain_atreachs)
        {
            // 定义结果集合
            List<Reach_Segment> reach_segments = new List<Reach_Segment>();

            // 按照河道名称进行分组
            var grouped_atreachs = contain_atreachs.GroupBy(a => a.reachname);

            // 遍历每个分组
            foreach (var group in grouped_atreachs)
            {
                // 对断面按照桩号排序
                var sorted_atreachs = group.OrderBy(a => a.chainage);

                // 定义起点和终点断面
                AtReach start_atreach = sorted_atreachs.First();
                AtReach end_atreach = sorted_atreachs.Last();

                // 处理最后一个Reach_Segment
                Reach_Segment last_segment = Reach_Segment.Get_ReachSegment(start_atreach.reachname, start_atreach.chainage, end_atreach.chainage);

                // 加入到结果集合
                reach_segments.Add(last_segment);
            }
            return reach_segments;
        }

        public static Reach_Segment Get_ResAt_ReachSeg(List<Reach_Segment> contain_ReachSeg, string reachname)
        {
            for (int i = 0; i < contain_ReachSeg.Count; i++)
            {
                if (contain_ReachSeg[i].reachname == reachname)
                {
                    return contain_ReachSeg[i];
                }
            }
            return contain_ReachSeg[0];
        }
    }
}