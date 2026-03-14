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
    //调度指令
    [Serializable]
    public class Dispatch_Info
    {
        //属性
        public string dispatch_plan;                             //调度方案
        public Dictionary<string, List<string>> gate_dispatch;   //各可控建筑物具体调度
        public string stcd;

        public Dispatch_Info()
        {
            this.dispatch_plan = "";
            this.gate_dispatch = new Dictionary<string, List<string>>();
            this.stcd = "";
        }

        public Dispatch_Info(string dispatch_plan, Dictionary<string, List<string>> gate_dispatch,string stcd = "")
        {
            this.dispatch_plan = dispatch_plan;
            this.gate_dispatch = gate_dispatch;
            this.stcd = stcd;
        }
    }

    //预警信息
    [Serializable]
    public class Warning_Info
    {
        //属性
        public string Name;              //预警类型名称
        public Warning_Level Level;      //预警总体级别
        public string Desc;               //预警总体描述 
        public List<Warning_Info> Warning_List;  //预警信息清单，里面的元素Name为预警对象名称

        public Warning_Info(string name, Warning_Level level, string desc , List<Warning_Info> warning_list = null)
        {
            Name = name;
            Level = level;
            Desc = desc;
            if (warning_list != null) Warning_List = warning_list;
        }
    }

    //预警级别
    [Serializable]
    public enum Warning_Level
    {
        blue_warining = 4,    //蓝色预警
        yellow_warining = 3,   //黄色预警
        orange_warining = 2,   //橙色预警
        red_warining = 1,      //红色预警 
        crimson_warining = 0,    //深红色预警
    }


    //设计洪水
    [Serializable]
    public class Design_Flood
    {
        public Design_Flood()
        {
            Flood_Name = "";
            Flood_GC = null;
        }

        public Design_Flood(string flood_Name, Dictionary<DateTime, double> flood_GC)
        {
            Flood_Name = flood_Name;
            Flood_GC = flood_GC;
        }

        public string Flood_Name { get; set; }
        public Dictionary<DateTime, double> Flood_GC { get; set; }
    }

    //河道设计洪水
    [Serializable]
    public class Reach_Design_Flood
    {
        public string flood_Name;
        public string station;
        public string stcd;
        public string reach;
        public string reach_cn;
        public double chainage;
        public string bnd_id;
        public List<Design_Flood> design_floods;

        public Reach_Design_Flood(string flood_Name, string station, string stcd, string reach, 
            string reach_cn, double chainage, string bnd_id, List<Design_Flood> design_floods)
        {
            this.flood_Name = flood_Name;
            this.station = station;
            this.stcd = stcd;
            this.reach = reach;
            this.reach_cn = reach_cn;
            this.chainage = chainage;
            this.bnd_id = bnd_id;
            this.design_floods = design_floods;
        }

        //从数据库获取所有模型实例的 河道设计洪水
        public static List<Reach_Design_Flood> Get_ReachFloodInfo_FromDB(string model_instance)
        {
            List<Reach_Design_Flood> res = new List<Reach_Design_Flood>();

            //定义连接对象
            
            string table = Mysql_GlobalVar.REACH_FLOODINFO;

            //从数据库中获取区域json字符串
            string sql = "select * from " + table ;
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //逐个水库获取数据
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string[] model_instance_list = dt.Rows[i][9].ToString().Split(',');
                if (!model_instance_list.Contains(model_instance)) continue;

                string flood_Name = dt.Rows[i][1].ToString();
                string station = dt.Rows[i][2].ToString();
                string stcd = dt.Rows[i][3].ToString();
                string reach = dt.Rows[i][4].ToString();
                string reach_cn = dt.Rows[i][5].ToString();
                double chainage = double.Parse(dt.Rows[i][6].ToString());
                string bnd_id = dt.Rows[i][7].ToString();
                List<Design_Flood> design_flood = JsonConvert.DeserializeObject<List<Design_Flood>>(dt.Rows[i][8].ToString());    //设计洪水 
                Reach_Design_Flood reach_flood = new Reach_Design_Flood(flood_Name, station, stcd, reach, reach_cn, chainage, bnd_id, design_flood);

                res.Add(reach_flood);
            }

            
            return res;
        }
    }


    //水库河道 水情监测信息类
    public class Water_Info
    {
        public string tm;
        public double q = 0;
        public double z = 0;
        public string stcd;
        public string type = "0";
        public string stnm = "";

        public Water_Info(double q, string stcd, string tm, double z, string type, string stnm)
        {
            this.stcd = stcd;
            this.tm = tm;
            this.z = z;
            this.q = q;
            this.type = type;
            this.stnm = stnm;
        }
    }

    //闸站状态监测信息类
    public class Gate_StateInfo
    {
        public string str_name;       //建筑物ID
        public string nowState;   //中文名称的闸站状态，全开、全关、半开、控泄
        public string tm;         //数据更新时间
        public int openN;         //开孔数
        public double openHQ;      //开度或控泄流量

        public Gate_StateInfo(string str_name, string now_state,string tm, int openn,double openhq)
        {
            this.str_name = str_name;
            this.tm = tm;
            this.nowState = now_state;
            this.openN = openn;
            this.openHQ = openhq;
        }

        public Gate_StateInfo(string str_name, string now_state, string tm, string openn, string openhq)
        {
            this.str_name = str_name;
            this.tm = tm;
            this.nowState = now_state;
            this.openN = openn == ""?0: int.Parse(openn);
            this.openHQ = openn == "" ?0: double.Parse(openhq);
        }
    }

    //闸门现状信息
    [Serializable]
    public class GateNow
    {
        //属性
        public string gate_code; //闸门编码
        public string gate_name; //闸门名称
        public double gate_h; //闸门最大开度
        public double bottom; //闸门底高程
        public double open_h; //开度
        public string now_state; //状态（全开、全关、半开）

        public GateNow(string gate_code, string gate_name, double gate_h, double bottom, double open_h, string now_state)
        {
            this.gate_code = gate_code;
            this.gate_name = gate_name;
            this.gate_h = gate_h;
            this.bottom = bottom;
            this.open_h = open_h;
            this.now_state = now_state;
        }
    }


    //故障闸门基础信息
    [Serializable]
    public class FaultGate_BaseInfo
    {
        //属性
        public string gate_code; //闸门编码
        public string gate_name; //闸门名称
        public double gate_bottom; //闸门底高程
        public double gate_h; //闸门最大开度
        public double gate_jd; //闸门经度
        public double gate_wd; //闸门纬度

        public FaultGate_BaseInfo(string gate_code, string gate_name,
            double bottom, double gate_h, double gate_jd, double gate_wd)
        {
            this.gate_code = gate_code;
            this.gate_name = gate_name;
            this.gate_bottom = bottom;
            this.gate_h = gate_h;
            this.gate_jd = gate_jd;
            this.gate_wd = gate_wd;
        }

        //从数据库获取故障闸门基本信息 -- 根据模型实例
        public static Dictionary<string,object> Read_StrGate_Info(string model_instance)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.FAULTGATE_INFO_TABLENAME;

            //先判断数据库中是否存在
            string sqlstr = $"select * from {tableName} where model_instance = '{model_instance}'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            string str_name = dt.Rows[0][2].ToString();
            string cn_name = dt.Rows[0][3].ToString();
            List<FaultGate_BaseInfo> gate_info = new List<FaultGate_BaseInfo>();
            for (int i = 0; i < nLen; i++)
            {
                string gate_code = dt.Rows[i][4].ToString();
                string gate_name = dt.Rows[i][5].ToString();
                double gate_bottom = double.Parse(dt.Rows[i][6].ToString());
                double gate_h = double.Parse(dt.Rows[i][7].ToString());
                double gate_jd = double.Parse(dt.Rows[i][8].ToString());
                double gate_wd = double.Parse(dt.Rows[i][9].ToString());
                FaultGate_BaseInfo fault_gate = new FaultGate_BaseInfo(gate_code, gate_name, gate_bottom, gate_h, gate_jd, gate_wd);
                gate_info.Add(fault_gate);
            }
            res.Add("str_name",str_name);
            res.Add("cn_name",cn_name);
            res.Add("gate_info",gate_info);

            return res;
        }

        //更新故障闸站设置信息
        public static void Update_FaultGate_SetInfo(string model_name,string str_name,
            string fault_type, List<string> fault_gates, List<double> gate_h)
        {
            string mike21_modelinstance = HydroModel.Get_Mike21_ModelInstance(model_name);
            Dictionary<string, object> fault_baseinfo = FaultGate_BaseInfo.Read_StrGate_Info(mike21_modelinstance);
            List<FaultGate_BaseInfo> gate_info = fault_baseinfo["gate_info"] as List<FaultGate_BaseInfo>;

            Dictionary<string, object> fault_info = new Dictionary<string, object>();
            fault_info.Add("gate_name", fault_baseinfo["cn_name"]);
            fault_info.Add("fault_type", fault_type);

            List<object[]> gate_h_list = new List<object[]>();
            for (int i = 0; i < gate_info.Count; i++)
            {
                gate_h_list.Add(new object[] { $"{i+1}#闸门", gate_info[i].gate_h, gate_h[i]});
            }
            fault_info.Add("gate_h", gate_h_list);

            //故障闸门经纬度
            Dictionary<string, double[]> faultgate_jwd = new Dictionary<string, double[]>();
            string fault_gate_desc = "";
            for (int i = 0; i < fault_gates.Count; i++)
            {
                string fault_gate_id = fault_gates[i];
                string fault_gate_name = "";double fault_gate_jd = 0; double fault_gate_wd = 0;
                for (int j = 0; j < gate_info.Count; j++)
                {
                    if(gate_info[j].gate_code == fault_gate_id)
                    {
                        fault_gate_name = gate_info[j].gate_name;
                        fault_gate_jd = gate_info[j].gate_jd;
                        fault_gate_wd = gate_info[j].gate_wd;
                    }
                }

                fault_gate_desc = i == 0? fault_gate_name: fault_gate_desc + "、" + fault_gate_name;
                faultgate_jwd.Add(fault_gate_name, new double[] { fault_gate_jd, fault_gate_wd});
            }
            fault_info.Add("fault_gate", fault_gate_desc);
            fault_info.Add("fault_gate_jwd", faultgate_jwd);

            //更新数据库
            Hd11.Update_ModelPara_DBInfo(model_name, "mike21_faultgate_info", File_Common.Serializer_Obj(fault_info));
        }

        //获取故障闸站设置信息
        public static string Get_FaultGate_SetInfo(string plan_code,string model_instance)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //先判断数据库中是否存在
            string sqlstr = $"select mike21_faultgate_info from {tableName} where plan_code = '{plan_code}' and model_instance = '{model_instance}'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            string res = dt.Rows[0][0].ToString();

            return res;
        }

    }

    //总干渠断面风险信息类
    public class ZGQ_SectionInfo
    {
        public AtReach section_atreach;    //断面位置
        public double sj_5n_level;       //5年一遇设计水位
        public double sj_10n_level;       //10年一遇设计水位
        public double sj_20n_level;       //20年一遇设计水位
        public double sj_50n_level;       //50年一遇设计水位
        public double sj_100n_level;      //100年一遇设计水位
        public double sj_300n_level;      //300年一遇设计水位
        public double sj_q;                 //设计流量
        public double jh_q;               //校核流量

        public double dd_level;          //渠堤或防护堤高程
        public string dd_level_name;      //渠堤还是防护堤
        public double hp_bottom_level;   //护坡底高程
        public double other_level;        //其他高程
        public string other_level_name;   //其他高程名称
        public double jd;        //经度
        public double wd;        //纬度

        public ZGQ_SectionInfo(AtReach section_atreach, double sj_5n_level, double sj_10n_level, 
            double sj_20n_level, double sj_50n_level, double sj_100n_level, double sj_300n_level,
            double sj_q, double jh_q, double dd_level, string dd_level_name, double hp_bottom_level, 
            double other_level, string other_level_name,double jd,double wd)
        {
            this.section_atreach = section_atreach;
            this.sj_5n_level = sj_5n_level;
            this.sj_10n_level = sj_10n_level;
            this.sj_20n_level = sj_20n_level;
            this.sj_50n_level = sj_50n_level;
            this.sj_100n_level = sj_100n_level;
            this.sj_300n_level = sj_300n_level;
            this.sj_q = sj_q;
            this.jh_q = jh_q;
            this.dd_level = dd_level;
            this.dd_level_name = dd_level_name;
            this.hp_bottom_level = hp_bottom_level;
            this.other_level = other_level;
            this.other_level_name = other_level_name;
            this.jd = jd;
            this.wd = wd;
        }
    }

    //山洪基本信息
    [Serializable]
    public class SH_INFO
    {
        public string region_name;  //山洪小流域名称
        public string region_code;  //山洪小流域编码
        public string village_name;  //村庄名称
        public string village_code;  //村庄编码
        public float village_x;      //村庄经度
        public float village_y;      //村庄纬度
        public float village_z;      //村庄高程
        public string catchment_code;  //对应产汇流子流域编码
        public string risk_level;      //地形风险级别

        public SH_INFO(string region_name, string region_code, string village_name,
            string village_code, float village_x, float village_y, float village_z,
            string catchment_code, string risk_level)
        {
            this.region_name = region_name;
            this.region_code = region_code;
            this.village_name = village_name;
            this.village_code = village_code;
            this.village_x = village_x;
            this.village_y = village_y;
            this.village_z = village_z;
            this.catchment_code = catchment_code;
            this.risk_level = risk_level;
        }
    }

    //场次洪水信息
    [Serializable]
    public class Rain_Flood_Info
    {
        public string flood_id;      //场次降雨/洪水ID
        public string flood_Name;    //场次降雨/洪水名称
        public string flood_desc;    //场次降雨/洪水描述
        public DateTime start_time;  //场次降雨/洪水开始时间
        public DateTime end_time;    //场次降雨/洪水结束时间
        public double time_duration; //场次降雨/洪水持续时间(天)   
        public double total_rain;    //累积降雨量
        public double max_rainrate;  //最大雨强
        public string recom_plan;    //该场次降雨/洪水的推荐预演方案

        public Rain_Flood_Info()
        {
            this.flood_id = "";
            this.flood_Name = "";
            this.flood_desc = "";
            this.start_time = new DateTime();
            this.end_time = new DateTime();
            this.time_duration = 0;
            this.total_rain = 0;
            this.max_rainrate = 0;
            this.recom_plan = "";
        }

        public Rain_Flood_Info(string flood_id, string flood_Name,
            string flood_desc, DateTime start_time, DateTime end_time, 
            double time_duration, double total_rain, double max_rainrate, string recom_plan)
        {
            this.flood_id = flood_id;
            this.flood_Name = flood_Name;
            this.flood_desc = flood_desc;
            this.start_time = start_time;
            this.end_time = end_time;
            this.time_duration = time_duration;
            this.total_rain = total_rain;
            this.max_rainrate = max_rainrate;
            this.recom_plan = recom_plan;
        }

        //从数据库获取所有场次洪水信息清单
        public static List<Rain_Flood_Info> Get_ReachFloodInfo_FromDB()
        {
            List<Rain_Flood_Info> res = new List<Rain_Flood_Info>();

            //定义连接对象
            string table = Mysql_GlobalVar.MODEL_RAIN_FLOOD_TABLENAME;

            //从数据库中获取区域json字符串
            string sql = "select * from " + table + " order by start_time";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //逐个获取数据
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string flood_id = dt.Rows[i][0].ToString();
                string flood_Name = dt.Rows[i][1].ToString();
                string flood_desc = dt.Rows[i][2].ToString();
                DateTime start_time = SimulateTime.StrToTime(dt.Rows[i][3].ToString());
                DateTime end_time = SimulateTime.StrToTime(dt.Rows[i][4].ToString());
                double time_duration = double.Parse(dt.Rows[i][5].ToString());
                double total_rain = double.Parse(dt.Rows[i][6].ToString());
                double max_rainrate = double.Parse(dt.Rows[i][7].ToString());
                string recom_plan = dt.Rows[i][8].ToString();

                Rain_Flood_Info rain_flood = new Rain_Flood_Info(flood_id,flood_Name,flood_desc,start_time,end_time,
                    time_duration,total_rain,max_rainrate,recom_plan);

                res.Add(rain_flood);
            }

            return res;
        }

        //向场次降雨数据库中插入新的记录
        public static void Insert_RainInfo(Rain_Flood_Info rain_info)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODEL_RAIN_FLOOD_TABLENAME;

            string sql = "insert into " + tableName + " (flood_id,flood_Name,flood_desc,start_time,end_time,time_duration,total_rain,max_rainrate,recom_plan) " +
                "values(:flood_id,:flood_Name,:flood_desc,:start_time,:end_time,:time_duration,:total_rain,:max_rainrate,:recom_plan)";
            KdbndpParameter[] mysqlPara = { new KdbndpParameter(":flood_id", rain_info.flood_id),
                                            new KdbndpParameter(":flood_Name", rain_info.flood_Name),
                                            new KdbndpParameter(":flood_desc", rain_info.flood_desc),
                                            new KdbndpParameter(":start_time",SimulateTime.TimetoStr(rain_info.start_time)),
                                            new KdbndpParameter(":end_time",SimulateTime.TimetoStr(rain_info.end_time)),

                                            new KdbndpParameter(":time_duration", rain_info.time_duration),
                                            new KdbndpParameter(":total_rain", rain_info.total_rain),
                                            new KdbndpParameter(":max_rainrate", rain_info.max_rainrate),
                                            new KdbndpParameter(":recom_plan", rain_info.recom_plan)
                                         };

            Mysql.Execute_Command(sql, mysqlPara);
        }

        //从模型方案表里 更新模型场次洪水信息
        public static void Update_ModelRainInfo(string plan_code,string flood_id)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先判断
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;

            //更新场次降雨洪水信息
            if (nLen != 0)
            {
                //更新模型状态和其他
                string sql = "update " + tableName + " set flood_id = '" + flood_id + "' where plan_code = '" + plan_code + "'";
                Mysql.Execute_Command(sql);
            }
        }

        //修改场次洪水推荐方案
        public static void Update_Rain_RecomPlan(string plan_code)
        {
            //定义连接的数据库和表
            string rain_tableName = Mysql_GlobalVar.MODEL_RAIN_FLOOD_TABLENAME;
            string plan_tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先获取该方案所属场次洪水ID
            string select_sql1 = "select flood_id from " + plan_tableName + " where plan_code = '" + plan_code + "'";
            DataTable dt1 = Mysql.query(select_sql1);
            int nLen1 = dt1.Rows.Count;
            string flood_id = nLen1 == 0?"": dt1.Rows[0][0].ToString();

            //先判断
            string select_sql = "select * from " + rain_tableName + " where flood_id = '" + flood_id + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;

            //更新场次降雨推荐方案信息
            if (nLen != 0)
            {
                //更新
                string sql = "update " + rain_tableName + " set recom_plan = '" + plan_code + "' where flood_id = '" + flood_id + "'";
                Mysql.Execute_Command(sql);
            }
        }

        //当删除方案为场次洪水推荐方案，如果场次洪水没有其他方案了，则删除场次洪水，否则更改推荐方案
        public static void Del_RainFlood_Plan(string plan_code)
        {
            //定义连接的数据库和表
            string rain_tableName = Mysql_GlobalVar.MODEL_RAIN_FLOOD_TABLENAME;
            string plan_tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //先判断
            string select_sql = "select flood_id from " + rain_tableName + " where recom_plan = '" + plan_code + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if(nLen != 0)
            {
                //删除的是推荐方案
                string flood_id = dt.Rows[0][0].ToString();

                //查看该场次洪水是否还有其他方案
                string select_sql1 = $"select plan_code from {plan_tableName} where flood_id = '{flood_id}' and plan_code != '{plan_code}'";
                DataTable dt1 = Mysql.query(select_sql1);
                if(dt1.Rows.Count != 0)
                {
                    //还有其他方案，则更改推荐方案
                    Update_Rain_RecomPlan(dt1.Rows[0][0].ToString());
                }
                else
                {
                    //无其他方案，则删除该场次洪水
                    string del_sql = "delete from " + rain_tableName + " where recom_plan = '" + plan_code + "'";
                    Mysql.Execute_Command(del_sql);
                }
            }
        }

    }

    //水库河道蓄滞洪区 所在地市和主管单位信息
    [Serializable]
    public class Struct_Region_Info
    {
        public string name;             // 工程编码，包括河道、水库、蓄滞洪区
        public string cn_name;          // 工程中文名称
        public string type;             // 工程类型：水库/河道/蓄滞洪区
        public double start_chainage;    // 起点桩号
        public double end_chainage;      // 终点桩号
        public string city;             // 所属地市
        public string admin_unit;       // 主管单位(责任主体)

        public Struct_Region_Info()
        {
            this.name = "";
            this.cn_name = "";
            this.type = "";
            this.start_chainage = 0;
            this.end_chainage = 0;
            this.city = "";
            this.admin_unit = "";
        }

        public Struct_Region_Info(string name, string cn_name, string type, double start_chainage,
                                 double end_chainage, string city, string admin_unit)
        {
            this.name = name;
            this.cn_name = cn_name;
            this.type = type;
            this.start_chainage = start_chainage;
            this.end_chainage = end_chainage;
            this.city = city;
            this.admin_unit = admin_unit;
        }

        //从数据库读取区域信息
        public static List<Struct_Region_Info> Get_Struct_RegionInfo()
        {
            List<Struct_Region_Info> regionInfos = new List<Struct_Region_Info>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.STRUCT_REGIONINFO_TABLENAME; 

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName + " order by id";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return regionInfos;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string name = dt.Rows[i]["name"].ToString();
                string cn_name = dt.Rows[i]["cn_name"].ToString();
                string type = dt.Rows[i]["type"].ToString();

                double start_chainage = dt.Rows[i]["start_chainage"].ToString() == "" ? 0 : double.Parse(dt.Rows[i]["start_chainage"].ToString());
                double end_chainage = dt.Rows[i]["end_chainage"].ToString() == "" ? 0 : double.Parse(dt.Rows[i]["end_chainage"].ToString());

                string city = dt.Rows[i]["city"].ToString();
                string admin_unit = dt.Rows[i]["admin_unit"].ToString();

                Struct_Region_Info regionInfo = new Struct_Region_Info(name, cn_name, type, start_chainage,
                                                                      end_chainage, city, admin_unit);
                regionInfos.Add(regionInfo);
            }

            return regionInfos;
        }

        //判断河道是否在巡查的重点河段里
        public static bool Is_Contain_Atreach(List<Struct_Region_Info> insepct_reachsegment,AtReach atreach)
        {
            bool is_contain = false;
            for (int i = 0; i < insepct_reachsegment.Count; i++)
            {
                if(atreach.reachname == insepct_reachsegment[i].name &&
                    atreach.chainage >= insepct_reachsegment[i].start_chainage &&
                    atreach.chainage <= insepct_reachsegment[i].end_chainage)
                {
                    is_contain = true;
                }
            }
            return is_contain;
        }
    }

    //重点巡查单位信息
    [Serializable]
    public class Important_Inspect_UnitInfo
    {
        public string name;             //名称，水库、河道和蓄滞洪区直接用中文名
        public string type;             //巡查类型  水库、河道、蓄滞洪区
        public string region;           //巡查区域  河道用河名加桩号，如大沙河(200+000~201+000)  
        public string inspect_reason;   //巡查原因
        public string at_loation;       //所在地市
        public string admin_unit;       //主管单位
        public string stcd;             //stcd
        public List<PointXY> location;      //经纬度坐标 

        public Important_Inspect_UnitInfo()
        {
            this.name = "";
            this.type = "";
            this.region = "";
            this.inspect_reason = "";
            this.at_loation = "";
            this.admin_unit = "";
            this.stcd = "";
            this.location = new List<PointXY>();
        }

        public Important_Inspect_UnitInfo(string name, string type, string region,
            string inspect_reason, string at_loation, string admin_unit,string stcd, List<PointXY> location)
        {
            this.name = name;
            this.type = type;
            this.region = region;
            this.inspect_reason = inspect_reason;
            this.at_loation = at_loation;
            this.admin_unit = admin_unit;
            this.stcd = stcd;
            this.location = location;
        }
    }
}