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
    public partial class Item_Info
    {        
        //获取河道纵剖面标记点
        public static Dictionary<string,Dictionary<string,double>> Get_Reachzpm_Lables()
        {
            Dictionary<string, Dictionary<string, double>> lables = new Dictionary<string, Dictionary<string, double>>();

            //大沙河
            Dictionary<string, double> dsh_lables = new Dictionary<string, double>();
            dsh_lables.Add("群英水库", 2321);
            dsh_lables.Add("孤山湖水库", 8402);
            dsh_lables.Add("出山口", 23400);
            dsh_lables.Add("幸福河口", 37357);
            dsh_lables.Add("蒋沟河口", 52100);
            dsh_lables.Add("新河口", 56435);
            dsh_lables.Add("山门河口", 61486);
            dsh_lables.Add("修武站", 64800);
            dsh_lables.Add("常桥闸", 69683);
            dsh_lables.Add("马厂闸", 73661);
            dsh_lables.Add("纸坊河口", 77865);
            dsh_lables.Add("大狮涝河口", 79608);
            dsh_lables.Add("峪河口", 87987);
            lables.Add("DSH", dsh_lables);

            //共渠
            Dictionary<string, double> gq_lables = new Dictionary<string, double>();
            gq_lables.Add("卫河口", 2991);
            gq_lables.Add("合河站", 4300);
            gq_lables.Add("京广铁路", 14030);
            gq_lables.Add("G107国道", 26240);
            gq_lables.Add("十里河口", 31977);
            gq_lables.Add("黄土岗", 39600);
            gq_lables.Add("香泉河口", 44044);
            gq_lables.Add("沧河口", 47913);
            gq_lables.Add("西沿村分洪堰", 53300);
            gq_lables.Add("思德河口", 55322);
            gq_lables.Add("淇河口", 60564);
            gq_lables.Add("刘庄站", 61350);
            gq_lables.Add("S222省道", 73952);
            gq_lables.Add("胡庄闸", 80857);
            gq_lables.Add("白寺坡分洪堰", 85045);
            gq_lables.Add("盐土庄闸", 93001);
            lables.Add("GQ", gq_lables);

            //卫河
            Dictionary<string, double> wh_lables = new Dictionary<string, double>();
            wh_lables.Add("合河闸", 484);
            wh_lables.Add("西孟姜女河", 12224);
            wh_lables.Add("晋新高速", 26600);
            wh_lables.Add("G107国道", 31876);
            wh_lables.Add("东孟姜女河", 47735);
            wh_lables.Add("宋村分洪堰", 63827);
            wh_lables.Add("淇河口", 73328);
            wh_lables.Add("淇门站", 75600);
            wh_lables.Add("长虹渠口", 103485);
            wh_lables.Add("圈里分洪堰", 131720);
            wh_lables.Add("共渠口", 143670);
            wh_lables.Add("五陵站", 147100);
            wh_lables.Add("北五陵分洪堰", 152066);
            wh_lables.Add("浚内沟口", 154040);
            wh_lables.Add("汤永河口", 166912);
            wh_lables.Add("西元村站", 172000);
            wh_lables.Add("安阳河口", 178168);
            wh_lables.Add("硝河口", 201309);
            wh_lables.Add("元村站", 210700);
            lables.Add("WH", wh_lables);

            //淇河
            Dictionary<string, double> qh_lables = new Dictionary<string, double>();
            qh_lables.Add("三郊口水库", 5794);
            qh_lables.Add("淅河口", 58830);
            qh_lables.Add("盘石头水库", 75400);
            qh_lables.Add("新村", 107600);
            qh_lables.Add("闫村分洪堰", 143412);
            qh_lables.Add("刘庄分洪堰", 147635);
            lables.Add("QH", qh_lables);

            //安阳河
            Dictionary<string, double> ayh_lables = new Dictionary<string, double>();
            ayh_lables.Add("横水站", 13300);
            ayh_lables.Add("小南海水库", 44910);
            ayh_lables.Add("彰武水库", 54696);
            ayh_lables.Add("粉红江口", 65697);
            ayh_lables.Add("安阳站", 83680);
            ayh_lables.Add("曹马分洪堰", 96360);
            ayh_lables.Add("郭盆分洪堰", 101350);
            ayh_lables.Add("郭盆闸", 101940);
            ayh_lables.Add("豆公闸", 139000);
            lables.Add("AYH", ayh_lables);

            return lables;
        }

        //获取流域的主要河道
        public static List<string> Get_MainReach_Names(string model_instance, bool iszp = false)
        {
            //所有主要河道(去除溢洪道、泄洪洞支流等辅助河道)
            List<string> hh_main_reach = new List<string>() {"XH","CHQ","DMJNH","XNG","SDH","GQ","XMJNH","WH","ZFGH",
                "QH","XIHE","LYG","TH","YOUHE","ZJQ","QHNZ","BMMH","PJH","WJH","LH","XFH","DSH","BQH","QYH","SHANMH",
                "FHJ","QHZL","XIAOHE","JGH","GQUP","DSLH","SLH","MFG","BCHQ","NCHQ","AYH","CDPG","JGLH","CFG","WHZL",
                "HWG","BLG","WHGD","CH","HH","SMH","TYH","XQH","YH","GCG","ZWG","SCG"};

            //绘制纵剖面图的河道
            List<string> hh_zp_reach = new List<string>() { "DSH", "GQ", "WH", "QH", "AYH", "TYH" };

            //根据是否是请求纵坡河道返回数据
            List<string> res = iszp ? hh_zp_reach : hh_main_reach;
            return res;
        }

        //获取主河道信息(非退水闸支流等虚拟河道)
        public static List<ReachInfo> Get_MainReachInfo(string model_instance,bool main_reach = false)
        {
            //判断一下是否包含所有河道
            List<string> main_reachname = Item_Info.Get_MainReach_Names(model_instance);

            if (Item_Info.Main_ReachInfo == null)
            {
                Initial_MainReachInfo(model_instance);
            }
            else 
            {
                List<ReachInfo> reachs = Item_Info.Main_ReachInfo;
                List<string> reach_names = new List<string>();
                for (int i = 0; i < reachs.Count; i++)
                {
                    reach_names.Add(reachs[i].reachname);
                }

                for (int i = 0; i < main_reachname.Count; i++)
                {
                    string reach_name = main_reachname[i];
                    if (!reach_names.Contains(reach_name))
                    {
                        Initial_MainReachInfo(model_instance);
                        break;
                    }
                }
            }

            if (main_reach)
            {
                List<ReachInfo> res = new List<ReachInfo>();
                for (int i = 0; i < Item_Info.Main_ReachInfo.Count; i++)
                {
                    if (main_reachname.Contains(Item_Info.Main_ReachInfo[i].reachname))
                    {
                        res.Add(Item_Info.Main_ReachInfo[i]);
                    }
                }
                return res;
            }

            return Item_Info.Main_ReachInfo;
        }

        //获取主河道信息(非退水闸支流等虚拟河道)
        public static List<Reach_BasePars> Get_MainReach_Info(string model_instance)
        {
            List<Reach_BasePars> main_reachinfo = new List<Reach_BasePars>();

            if (Item_Info.Main_ReachInfo == null) Initial_MainReachInfo(model_instance);
            List<ReachInfo> main_reach = Item_Info.Main_ReachInfo;
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reach_name = main_reach[i].reachname;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);

                double start_chainage = main_reach[i].start_chainage;
                double end_chainage = main_reach[i].end_chainage;

                string start_str = File_Common.Get_ChainageStr("K", start_chainage);
                string end_str = File_Common.Get_ChainageStr("K", end_chainage);
                double length = (end_chainage - start_chainage) / 1000;
                Reach_BasePars reach_par = new Reach_BasePars(reach_name, i + 1, reach_cnname, start_str, end_str, length);
                main_reachinfo.Add(reach_par);
            }

            return main_reachinfo;
        }

        //获取主河道信息(非退水闸支流等虚拟河道)
        public static Dictionary<string,Reach_BasePars> Get_MainReach_Infodic(string model_instance)
        {
            Dictionary<string, Reach_BasePars> main_reachinfodic = new Dictionary<string, Reach_BasePars>();

            if (Item_Info.Main_ReachInfo == null) Initial_MainReachInfo(model_instance);
            List<ReachInfo> main_reach = Item_Info.Main_ReachInfo;
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reach_name = main_reach[i].reachname;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);

                double start_chainage = main_reach[i].start_chainage;
                double end_chainage = main_reach[i].end_chainage;

                string start_str = File_Common.Get_ChainageStr("K", start_chainage);
                string end_str = File_Common.Get_ChainageStr("K", end_chainage);
                double length = (end_chainage - start_chainage) / 1000;
                Reach_BasePars reach_par = new Reach_BasePars(reach_name, i + 1, reach_cnname, start_str, end_str, length);
                main_reachinfodic.Add(main_reach[i].reachname, reach_par);
            }

            return main_reachinfodic;
        }

        //根据模型方案id和业务模型类型获取需要统计流量的特征断面(用于分洪、溃堤、闸站故障流量统计)
        public static AtReach Get_ReachSection_From_PlanCode(string plan_code)
        {
            string business_code = HydroModel.Get_BusinessCode_FromDB(plan_code);
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            if (model_instance == "") return AtReach.Get_Atreach("", 0);

            Dictionary<string, AtReach> business_section = new Dictionary<string, AtReach>();
            business_section.Add("embank_break_gq", AtReach.Get_Atreach("SS_BRANCH_GQ_KD_GZDU", 50));
            business_section.Add("embank_break_wh", AtReach.Get_Atreach("SS_BRANCH_WH_KD_GZDU", 50));

            business_section.Add("gate_fault_hhz", AtReach.Get_Atreach("WH", 484));
            business_section.Add("gate_fault_xhk", AtReach.Get_Atreach("QHNZ", 294));
            business_section.Add("gate_fault_dlz", AtReach.Get_Atreach("GQ", 56272));
            business_section.Add("gate_fault_hzz", AtReach.Get_Atreach("GQ", 80857));
            business_section.Add("gate_fault_ytz", AtReach.Get_Atreach("GQ", 93001));

            return business_section.Keys.Contains(business_code)
                ? business_section[business_code]
                : AtReach.Get_Atreach("", 0);
        }

        //获取各河道的建筑物桩号
        public static List<float> Get_ReachStrInfo(string reach_name)
        {
            List<float> str_list = new List<float>();
            for (int i = 0; i < Item_Info.Str_BaseInfo.Count; i++)
            {
                Struct_BasePars str_basepar = Item_Info.Str_BaseInfo.ElementAt(i).Value;
                if (str_basepar.reach_name == reach_name) str_list.Add((float)str_basepar.chainage);
            }

            return str_list;
        }

        //获取各河道的中文名
        public static string Get_ReachChinaName(string reach_en_name)
        {
            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();
            string name = "";
            for (int i = 0; i < Item_Info.Str_BaseInfo.Count; i++)
            {
                if (Item_Info.Str_BaseInfo.ElementAt(i).Value.reach_name == reach_en_name)
                {
                    name = Item_Info.Str_BaseInfo.ElementAt(i).Value.reach_cnname;
                    break;
                }
            }

            Dictionary<string, string> reach_namedic = new Dictionary<string, string>();
            reach_namedic.Add("AYH", "安阳河");
            reach_namedic.Add("BCHQ", "北长虹渠");
            reach_namedic.Add("BLG", "八里沟");
            reach_namedic.Add("BMMH", "白马门河");
            reach_namedic.Add("BQH", "百泉河");
            reach_namedic.Add("CDPG", "茶店坡沟");
            reach_namedic.Add("CFG", "长丰沟");
            reach_namedic.Add("CH", "沧河");
            reach_namedic.Add("CHQ", "长虹渠");
            reach_namedic.Add("DMJNH", "东孟姜女河");
            reach_namedic.Add("DSH", "大沙河");
            reach_namedic.Add("DSLH", "大狮涝河");
            reach_namedic.Add("FHJ", "粉红江");
            reach_namedic.Add("GCG", "故城沟");
            reach_namedic.Add("GQ", "共产主义渠");
            reach_namedic.Add("GQUP", "共产主义渠上段");
            reach_namedic.Add("HH", "洪河");
            reach_namedic.Add("HWG", "红卫沟");
            reach_namedic.Add("JGH", "蒋沟河");
            reach_namedic.Add("JGLH", "镜高涝河");
            reach_namedic.Add("LH", "李河");
            reach_namedic.Add("LYG", "梨园沟");
            reach_namedic.Add("MFG", "民丰排水沟");
            reach_namedic.Add("NCHQ", "南长虹渠");
            reach_namedic.Add("PJH", "普济河");
            reach_namedic.Add("QH", "淇河");
            reach_namedic.Add("QHNZ", "淇河南支");
            reach_namedic.Add("QHZL", "淇河支流");
            reach_namedic.Add("QYH", "群英河");
            reach_namedic.Add("SCG", "苏村沟");
            reach_namedic.Add("SDH", "思德河");
            reach_namedic.Add("SHANMH", "山门河");
            reach_namedic.Add("SLH", "十里河");
            reach_namedic.Add("SMH", "石门河");
            reach_namedic.Add("TH", "汤河");
            reach_namedic.Add("TYH", "汤永河");
            reach_namedic.Add("WH", "卫河");
            reach_namedic.Add("WHGD", "卫河故道");
            reach_namedic.Add("WHZL", "卫河支流");
            reach_namedic.Add("WJH", "翁涧河");
            reach_namedic.Add("XFH", "幸福河");
            reach_namedic.Add("XH", "新河");
            reach_namedic.Add("XIAOHE", "硝河");
            reach_namedic.Add("XIHE", "淅河");
            reach_namedic.Add("XMJNH", "西孟姜女河");
            reach_namedic.Add("XNG", "浚内沟");
            reach_namedic.Add("XQH", "香泉河");
            reach_namedic.Add("YH", "峪河");
            reach_namedic.Add("YOUHE", "羑河");
            reach_namedic.Add("ZFGH", "纸坊沟河");
            reach_namedic.Add("ZJQ", "赵家渠");
            reach_namedic.Add("ZWG", "赵王沟");

            if (reach_namedic.Keys.Contains(reach_en_name)) name = reach_namedic[reach_en_name];
            return name;
        }

        //获取流域一些不参与水面高程插值的河道断面
        public static Dictionary<string,List<double>> Get_JLH_TSSection()
        {
            Dictionary<string, List<double>> sections = new Dictionary<string, List<double>>();

            sections.Add("CLSKYHD", new List<double>() { 70 });
            return sections;
        }

        //获取流域考虑倒灌的主要一级支流
        public static List<string> Get_DG_Reach()
        {
            List<string> reach_list = new List<string>();
            reach_list.Add("AYH");
            reach_list.Add("BQH");
            reach_list.Add("CH");
            reach_list.Add("CHQ");
            reach_list.Add("DMJNH");
            reach_list.Add("DSLH");
            reach_list.Add("GQUP");
            reach_list.Add("JGH");
            reach_list.Add("QH");
            reach_list.Add("SHANMH");
            reach_list.Add("SLH");
            reach_list.Add("SMH");
            reach_list.Add("TYH");
            reach_list.Add("WHGD");
            reach_list.Add("XH");
            reach_list.Add("XIAOHE");
            reach_list.Add("XMJNH");
            reach_list.Add("XNG");
            reach_list.Add("XQH");
            reach_list.Add("YH");
            reach_list.Add("ZFGH");
            return reach_list;
        }

        //根据模型桩号求设计桩号
        public static double Get_Design_Chainage(string reach_name,double model_chainage)
        {
            double design_chainage = model_chainage;
            Dictionary<string, Dictionary<double, double>> reach_chainage_dy = new Dictionary<string, Dictionary<double, double>>();
            Dictionary<double, double> wh_dy = new Dictionary<double, double>{
                { 0, 0 },
                { 226688, 226688 }
            };

            reach_chainage_dy.Add("WH", wh_dy);

            //如果包含该河道的对应关系，则内插桩号，否则用原桩号
            if (reach_chainage_dy.Keys.Contains(reach_name))
            {
                design_chainage = Insert_Chainage(model_chainage, reach_chainage_dy[reach_name]);
            }
            return Math.Round(design_chainage,1);
        }

        //桩号内插
        public static double Insert_Chainage(double model_chainage, Dictionary<double, double> chainage_dy)
        {
            // 如果直接找到对应的模型桩号，则返回对应的设计桩号
            if (chainage_dy.ContainsKey(model_chainage))
            {
                return chainage_dy[model_chainage];
            }

            // 如果模型桩号不存在，则找到最近的两个模型桩号，并使用线性插值计算设计桩号
            List<double> model_chainages = chainage_dy.Keys.ToList();
            double previousModelStake = model_chainages[0];
            double nextModelStake = model_chainages[model_chainages.Count - 1];
            foreach (double chainage in model_chainages)
            {
                if (chainage < model_chainage)
                {
                    previousModelStake = chainage;
                }
                else
                {
                    nextModelStake = chainage;
                    break;
                }
            }

            double previousDesignStake = chainage_dy[previousModelStake];
            double nextDesignStake = chainage_dy[nextModelStake];

            //使用线性插值计算设计桩号
            double designStake = previousDesignStake + (nextDesignStake - previousDesignStake) * (model_chainage - previousModelStake) / (nextModelStake - previousModelStake);

            return designStake;
        }

        //获取各河道不冲流速 --参数为各河道各断面的桩号
        public static Dictionary<string, List<float>> Get_Reach_NoDestorySpeed(Dictionary<string, List<float>> reach_section_chainage)
        {
            Dictionary<string, List<float>> res = new Dictionary<string, List<float>>();

            //从数据库中读取全部河道 各河段的不冲流速
            Dictionary<Reach_Segment, double> bc_speed = Load_Reach_NoDestorySpeed();

            //将各河道的编组
            Dictionary<string, Dictionary<Reach_Segment, double>> bc_speed1 = bc_speed ==null?null:bc_speed.GroupBy(item => item.Key.reachname).ToDictionary(group => group.Key,
                group => group.ToDictionary(item => item.Key, item => item.Value));

            //推求各河道 各断面的不冲流速，没有值的用默认值
            for (int i = 0; i < reach_section_chainage.Count; i++)
            {
                string reach_name = reach_section_chainage.ElementAt(i).Key;
                List<float> section_chainage = reach_section_chainage.ElementAt(i).Value;
                Dictionary<Reach_Segment, double> reach_bc = null;
                if (bc_speed1 != null)
                {
                    if (bc_speed1.Keys.Contains(reach_name)) reach_bc = bc_speed1[reach_name];
                }

                List<float> section_bc = section_chainage.ToList();
                //如果数据库里不冲流速没有该河道
                if (reach_bc == null)
                {
                    for (int j = 0; j < section_bc.Count; j++)
                    {
                        section_bc[j] = Model_Const.DEFAULT_NODESTORY_SPEED;
                    }
                }
                else
                {
                    //通过给定的河段抗冲流速，搜索断面的抗冲流速，没有的给恒定值
                    section_bc = GetResult(section_chainage, reach_bc);
                }

                res.Add(reach_name, section_bc);
            }

            return res;
        }

        //通过给定的河段抗冲流速，搜索断面的抗冲流速，没有的给恒定值
        public static List<float> GetResult(List<float> section_chainage, Dictionary<Reach_Segment, double> reach_bc)
        {
            List<float> res = new List<float>();

            for (int i = 0; i < section_chainage.Count; i++)
            {
                float chainage = section_chainage[i];
                bool found = false;

                Reach_Segment[] reachSegments = reach_bc.Keys.ToArray();
                double[] values = reach_bc.Values.ToArray();

                for (int j = 0; j < reachSegments.Length; j++)
                {
                    Reach_Segment segment = reachSegments[j];
                    double value = values[j];

                    if (chainage >= segment.start_chainage && chainage <= segment.end_chainage)
                    {
                        res.Add((float)value);
                        found = true;
                        break;
                    }
                }

                if (!found) res.Add(Model_Const.DEFAULT_NODESTORY_SPEED);
            }

            return res;
        }

        //从数据库中读取全部河道 各河段的不冲流速
        public static Dictionary<Reach_Segment, double> Load_Reach_NoDestorySpeed()
        {
            Dictionary<Reach_Segment, double> bc_speed = new Dictionary<Reach_Segment, double>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.NODESTORY_SPEED;
            

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string reach = dt.Rows[i][1].ToString();
                double start_chainage = double.Parse(dt.Rows[i][3].ToString());
                double end_chainage = double.Parse(dt.Rows[i][4].ToString());
                double segment_bcspeed = double.Parse(dt.Rows[i][5].ToString());
                Reach_Segment segment = Reach_Segment.Get_ReachSegment(reach, start_chainage, end_chainage);

                if(!bc_speed.Keys.Contains(segment)) bc_speed.Add(segment, segment_bcspeed);
            }

            

            return bc_speed;
        }
    }
}
