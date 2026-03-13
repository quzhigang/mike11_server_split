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

    //专用于流域相关属性和方法的类
    [Serializable]
    public class WG_INFO
    {
        #region ********************* 类的静态属性 ************************
        //建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Str_BaseInfo
        { get; set; }

        //主要河道基本信息(除退水闸支流以外)
        public static List<ReachInfo> Main_ReachInfo
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



        #region ***************************方法*****************************
        #region *********************河道 闸站 水库 主要信息*********************
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
            if (WG_INFO.Item_SectionDatas == null) Update_Item_SectionDatas(model_instance);

            //如果不包括当前项目的网格，则从数据库读取当前项目的网格
            if (!Item_SectionDatas.Keys.Contains(model_instance)) Update_Item_SectionDatas(model_instance);
            SectionList item_sectionlist = Item_SectionDatas[model_instance];

            return item_sectionlist;
        }

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

        //获取所有建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Get_StrBaseInfo()
        {
            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();

            return WG_INFO.Str_BaseInfo;
        }

        //获取指定建筑物基本信息
        public static Struct_BasePars Get_StrBaseInfo(string str_name)
        {
            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();

            Struct_BasePars str_par = new Struct_BasePars();
            if (WG_INFO.Str_BaseInfo.Keys.Contains(str_name))
            {
                str_par = WG_INFO.Str_BaseInfo[str_name];
            }

            return str_par;
        }

        //根据指定模型实例获取建筑物基本信息
        public static Dictionary<string, Struct_BasePars> Get_StrBaseInfo_ByModelInstance(string model_instance)
        {
            Dictionary<string, Struct_BasePars> res = new Dictionary<string, Struct_BasePars>();

            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();

            for (int i = 0; i < WG_INFO.Str_BaseInfo.Count; i++)
            {
                string str_name = WG_INFO.Str_BaseInfo.ElementAt(i).Key;
                Struct_BasePars str_par = WG_INFO.Str_BaseInfo.ElementAt(i).Value;
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
            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();

            Dictionary<string, List<string>> business_instance = HydroModel.Get_BusinessModel_ModelInstance_Relation();
            if (!business_instance.Keys.Contains(business_code)) return null;

            List<string> model_instances = business_instance[business_code];
            for (int i = 0; i < WG_INFO.Str_BaseInfo.Count; i++)
            {
                string str_name = WG_INFO.Str_BaseInfo.ElementAt(i).Key;
                Struct_BasePars str_par = WG_INFO.Str_BaseInfo.ElementAt(i).Value;
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

            WG_INFO.Str_BaseInfo = str_baseinfos;
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
            Dictionary<string, List<AtReach>> res_controlreach = WG_INFO.GetRes_ControlInitialAtreach();

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

            WG_INFO.Reservoir_Info = res_info;
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
            lxpup_inq_section.Add("共渠入流",AtReach.Get_Atreach("GQ",40000));
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

            WG_INFO.Xzhq_Info = Xzhq_Info;
        }

        //从数据库获取水库溢洪道、泄洪洞泄流曲线(如果没有的话，则为空)
        public static Dictionary<double, double> Get_Res_Strlq_Relation(string res_name,string str_type)
        {
            Dictionary<double, double> res = new Dictionary<double, double>();

            //获取该建筑物信息
            Struct_BasePars str_info = Get_Res_YHDXHD_StrInfo(res_name, str_type);
            if (str_info == null) return res;

            //获取该建筑的泄流曲线
            List<double[]> str_qh = WG_INFO.Get_StrQHrelation(str_info.str_name);
            if(str_qh.Count != 0)
            {
                res = Change_Relation_Format(str_qh);
            }

            return res;
        }

        //从数据库获取水库溢洪道和泄洪洞建筑(如果没有的话，则为空)
        public static Struct_BasePars Get_Res_YHDXHD_StrInfo(string res_name, string str_type)
        {
            Struct_BasePars str_info = null;

            List<Struct_BasePars> str_list = new List<Struct_BasePars>();

            //获取所有建筑物基本信息
            Dictionary<string, Struct_BasePars> str_infos = WG_INFO.Get_StrBaseInfo();

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
            Dictionary<string, Struct_BasePars> str_infos = WG_INFO.Get_StrBaseInfo();

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

        //改关系格式
        public static Dictionary<double, double> Change_Relation_Format(List<double[]> source_list)
        {
            if (source_list.First().Length != 2) return null;
            Dictionary<double, double> res = new Dictionary<double, double>();
            for (int i = 0; i < source_list.Count; i++)
            {
                res.Add(source_list[i][1], source_list[i][0]);
            }

            return res;
        }

        //水库的水位-库容关系曲线(读取数据库)
        public static Dictionary<string, Dictionary<double, double>> Get_ResHVrelation()
        {
            Dictionary<string, Dictionary<double, double>> hvrelation = new Dictionary<string, Dictionary<double, double>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.RES_HVRELATION;
            

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string stcd = dt.Rows[i][1].ToString();
                string res_cnname = dt.Rows[i][2].ToString();
                string hv_relationstr = dt.Rows[i][3].ToString();
                List<double[]> h_v = JsonConvert.DeserializeObject<List<double[]>>(hv_relationstr);
                Dictionary<double, double> res_hv = new Dictionary<double, double>();
                for (int j = 0; j < h_v.Count; j++)
                {
                    res_hv.Add(h_v[j][0], h_v[j][1]);
                }
                hvrelation.Add(stcd, res_hv);
            }

            

            return hvrelation;
        }


        //读取蓄滞洪区的水位-库容关系和水位-面积关系曲线(读取数据库)
        public static void Modify_Xzhq_Relation(ref List<Xzhq_Info> xzhq_Info)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.RES_HVRELATION;
            

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string stcd = dt.Rows[i][1].ToString();
                string res_cnname = dt.Rows[i][2].ToString();
                string hv_relationstr = dt.Rows[i][3].ToString();
                string ha_relationstr = dt.Rows[i][4].ToString();

                //更新蓄滞洪区(子类)水位-库容曲线和水位-面积曲线
                for (int j = 0; j < xzhq_Info.Count; j++)
                {
                    if(xzhq_Info[j].Stcd == stcd)
                    {
                        List<double[]> h_v = JsonConvert.DeserializeObject<List<double[]>>(hv_relationstr);
                        List<double[]> h_a = JsonConvert.DeserializeObject<List<double[]>>(ha_relationstr);

                        //水位-库容关系曲线
                        Dictionary<double, double> hv_list = new Dictionary<double, double>();
                        for (int k = 0; k < h_v.Count; k++)
                        {
                            hv_list.Add(h_v[k][0], h_v[k][1]);
                        }

                        //水位-面积关系曲线
                        Dictionary<double, double> ha_list = new Dictionary<double, double>();
                        for (int k = 0; k < h_a.Count; k++)
                        {
                            ha_list.Add(h_a[k][0], h_a[k][1]);
                        }

                        xzhq_Info[j].Level_Volume = hv_list;
                        xzhq_Info[j].Level_Area = ha_list;
                        if (hv_list.Count != 0) xzhq_Info[j].Xzhq_IsFlood_Level = hv_list.ElementAt(0).Key + 0.2;
                        break;
                    }
                }

            }
            
        }

        //获取蓄滞洪区基本信息
        public static List<Xzhq_Info> Get_Xzhq_BaseInfo()
        {
            List<Xzhq_Info> Xzhq_Info = new List<Xzhq_Info>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.XZHQ_BASEINFO;

            //先判断数据库中是否存在
            string sqlstr = $"select name,code,des_fl_sta,pcode from {tableName} order by sort_num" ;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string cnname = dt.Rows[i][0].ToString();
                string stcd = dt.Rows[i][1].ToString();
                string design_level = dt.Rows[i][2].ToString();
                string father = dt.Rows[i][3].ToString();

                Xzhq_Info xzhq = new Xzhq_Info();
                xzhq.Stcd = stcd;
                xzhq.Name = cnname;
                xzhq.Father = father;
                xzhq.Design_Level = design_level;
                Xzhq_Info.Add(xzhq);
            }
            

            return Xzhq_Info;
        }

        //获取模型实例的所有水库基本信息
        public static List<Reservoir> Get_ResInfo(string model_instance)
        {
            List<Reservoir> res_info = new List<Reservoir>();

            //初始化所有水库的基础信息
            if (WG_INFO.Reservoir_Info == null) Initial_ResInfo();
            if (WG_INFO.Reservoir_Info == null) return null;

            //获取监测站点和模型实例对应关系
            List<string> stations = Get_StationStcd_FromModelInstance(model_instance);

            //遍历
            for (int i = 0; i < WG_INFO.Reservoir_Info.Count; i++)
            {
                Reservoir res = WG_INFO.Reservoir_Info[i];
                if (stations.Contains(res.Stcd)) res_info.Add(res);
            }
            return res_info;
        }

        //获取所有蓄滞洪区区基本信息
        public static List<Xzhq_Info> Get_XzhqInfo()
        {
            if (WG_INFO.Xzhq_Info == null) Initial_Xzhq_Info();
            List<Xzhq_Info> Fhblq_Info = WG_INFO.Xzhq_Info;
            return Fhblq_Info;
        }

        //一维模型三维面要素中的判断显示要素 及与蓄滞洪区对应关系 
        public static Dictionary<int, string> Get_Mike11_TxPolygonFeatures()
        {
            Dictionary<int, string> polygon_fid = new Dictionary<int, string>();
            polygon_fid.Add(58, "");
            polygon_fid.Add(59, "LXP_UP_ZHQ");
            polygon_fid.Add(60, "LXP_DOWN_ZHQ");
            polygon_fid.Add(61, "GQX_UP_ZHQ");
            polygon_fid.Add(62, "GQX_DOWN_ZHQ");
            polygon_fid.Add(63, "BSP_ZHQ");
            polygon_fid.Add(64, "CHQ_ZHQ");
            polygon_fid.Add(65, "LWP_ZHQ");
            polygon_fid.Add(66, "XTP_ZHQ");
            polygon_fid.Add(67, "RGP_ZHQ");
            polygon_fid.Add(68, "GRP1_ZHQ");
            polygon_fid.Add(69, "GRP2_ZHQ");
            for (int i = 70; i < 84; i++){ polygon_fid.Add(i, ""); }
            polygon_fid.Add(84, "CJQ_ZHQ");
            polygon_fid.Add(85, "");
            polygon_fid.Add(86, "");
            polygon_fid.Add(87, "LXP_UP_ZHQ");
            polygon_fid.Add(88, "LXP_DOWN_ZHQ");
            polygon_fid.Add(89, "LXP_DOWN_ZHQ");
            return polygon_fid;
        }

        //获取模型实例的所有监测站点stcd
        public static List<string> Get_StationStcd_FromModelInstance(string model_instance)
        {
            List<string> res = new List<string>();  

            //从数据库读取水情监测信息
            List<Water_Condition> now_water = Hd11.Read_NowWater();

            //获取对应关系
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Model_instance.Contains(model_instance)) res.Add(now_water[i].Stcd);
            }

            return res;
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

        //获取河道全部控制断面水位流量关系
        public static Dictionary<AtReach, List<double[]>> Get_SectionQHrelation()
        {
            if (WG_INFO.Section_QHrelation == null) Initial_ReachSectionQHrelation();

            return WG_INFO.Section_QHrelation;
        }

        //更新河道全部控制断面水位流量关系(没有的全部重新计算，有且有stcd的不更新)
        public static void Update_ReachQH_RelationTable()
        {
            //加载默认模型
            HydroModel hydromodel = HydroModel.Load(Model_Const.DEFAULT_MODELNAME);

            //获取河道全部控制断面水位流量关系
            Dictionary<AtReach, string> section_stcd = Initial_ReachSectionQHrelation();
            Dictionary<AtReach, List<double[]>> section_qhrelation = WG_INFO.Section_QHrelation;

            //更新或补全水位流量关系
            for (int i = 0; i < section_qhrelation.Count; i++)
            {
                AtReach atreach = section_qhrelation.ElementAt(i).Key;
                List<double[]> qh_relation = section_qhrelation.ElementAt(i).Value;
                string stcd = section_stcd.Keys.Contains(atreach) ? section_stcd[atreach] : "";
                if (qh_relation == null || stcd == "")
                {
                    List<double[]> new_qh_relation = Xns11.Get_ReachSection_QH(hydromodel, atreach);
                    section_qhrelation[atreach] = new_qh_relation;
                }
            }

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.SECTION_QHRELATION;
            

            //更新数据库水位
            for (int i = 0; i < section_qhrelation.Count; i++)
            {
                AtReach atreach = section_qhrelation.ElementAt(i).Key;
                List<double[]> qh_relation = section_qhrelation.ElementAt(i).Value;
                string qh_str = File_Common.Serializer_Obj(qh_relation);

                //处理
                string sql = "update " + tableName + " set qh_relation = '" + qh_str
                    + "' where reach = '" + atreach.reachname + "' and chainage = " + atreach.chainage + " and model_instance = '" + Mysql_GlobalVar.now_instance + "'";

                Mysql.Execute_Command(sql);
            }
            

            Console.WriteLine("河道断面水位流量关系更新完成！");
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

            

            WG_INFO.Section_QHrelation = qhrelation;
            return reach_stcd;
        }

        //获取河道指定断面水位流量关系
        public static List<double[]> Get_SectionQHrelation(AtReach atreach)
        {
            List<double[]> res = new List<double[]>();

            if (WG_INFO.Section_QHrelation == null) Initial_ReachSectionQHrelation();
            Dictionary<AtReach, List<double[]>> section_qh = WG_INFO.Section_QHrelation;
            for (int i = 0; i < section_qh.Count; i++)
            {
                if (atreach.reachname == section_qh.ElementAt(i).Key.reachname && atreach.chainage == section_qh.ElementAt(i).Key.chainage)
                {
                    res = section_qh.ElementAt(i).Value;
                }
            }

            return res;
        }

        //获取指定建筑物闸前水位和流量关系
        public static List<double[]> Get_StrQHrelation(string str_name)
        {
            if (WG_INFO.Struct_QHrelation == null) Initial_Struct_QHrelation();

            List<double[]> str_qhrelation = new List<double[]>();
            if (WG_INFO.Struct_QHrelation.Keys.Contains(str_name))
            {
                str_qhrelation = WG_INFO.Struct_QHrelation[str_name];
            }

            return str_qhrelation;
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

            

            WG_INFO.Struct_QHrelation = qhrelation;
        }

        //根据监测水位得到重点河道断面流量
        public static Dictionary<AtReach, double> Get_MainStatin_Discharge(List<Water_Condition> now_water, Dictionary<AtReach, List<double[]>> section_qhrelations)
        {
            Dictionary<AtReach, double> res = new Dictionary<AtReach, double>();

            //河道重点监测站点
            List<string> reach_station = new List<string>() { "修武", "合河(共)", "刘庄", "淇门", "新村", "五陵", "安阳", "元村","横水"};

            //各河道重点监测站点最新流量
            for (int i = 0; i < reach_station.Count; i++)
            {
                string station_name = reach_station[i];
                Water_Condition station_condition = Water_Condition.Get_WaterCondition(now_water, station_name);
                AtReach station_atreach = AtReach.Get_Atreach(station_condition.Reach, station_condition.Chainage);
                double station_q = station_condition.Discharge;

                //如果没有实测流量，则用水位根据水位-流量关系内插得到流量
                if (station_q == 0)
                {
                    station_q = Get_SectionQ_FromLevelandQH(now_water, section_qhrelations, station_condition.Stcd, station_atreach);
                }

                if(!res.Keys.Contains(station_atreach)) res.Add(station_atreach, station_q);
            }

            //给主要断面流量赋值
            WG_INFO.Reach_NowDischarge = res;

            return res;
        }

        //获取主要河道额外 基流流量
        public static Dictionary<string, double> Get_MainReach_Base_Discharge()
        {
            Dictionary<string, double> res = new Dictionary<string, double>();

            //主要水文站点流量
            Dictionary<AtReach, double> main_station_q = WG_INFO.Reach_NowDischarge;
            if (main_station_q == null) return null;

            //各主要站点流量 ("修武", "合河(共)", "刘庄", "淇门", "新村", "五陵", "安阳", "元村","横水")
            double xw_q = main_station_q.Values.ElementAt(0);
            double hh_q = main_station_q.Values.ElementAt(1);
            double lz_q = main_station_q.Values.ElementAt(2);
            double qm_q = main_station_q.Values.ElementAt(3);
            double xc_q = main_station_q.Values.ElementAt(4);
            double wl_q = main_station_q.Values.ElementAt(5);
            double ay_q = main_station_q.Values.ElementAt(6);
            double yc_q = main_station_q.Values.ElementAt(7);
            double hs_q = main_station_q.Values.ElementAt(8);

            //获取当前水情
            List<Water_Condition> now_water = Hd11.Read_NowWater();

            //修武以上大沙河基流,剔除洪水后
            res.Add("xw_base_in", Math.Min(xw_q, 8));

            //合河(共)站前大沙河额外基流,剔除洪水后
            res.Add("hh_base_in", Math.Min(Math.Max(hh_q - xw_q, 0.01),10));

            //共渠合河至刘庄额外基流
            double add_q = Math.Min(lz_q + qm_q - hh_q - xc_q,0.01);
            res.Add("lz_base_in", Math.Min(hh_q + add_q * 0.5, 10));

            //卫河合河至淇门额外基流
            res.Add("qm_base_in", Math.Min(qm_q + add_q * 0.5, 15));

            //淇河新村以上额外基流
            Water_Condition pstsk_water = Water_Condition.Get_WaterCondition(now_water, "盘石头水库");
            res.Add("xc_base_in", Math.Min(Math.Max(xc_q - pstsk_water.Discharge,0), 20));

            //卫河老观嘴基于五陵站额外基流
            res.Add("wl_base_in", Math.Min(Math.Max(wl_q - lz_q - qm_q, 0.01), 30));

            //安阳河安阳以上额外基流
            Water_Condition zwsk_water = Water_Condition.Get_WaterCondition(now_water, "彰武水库");
            res.Add("ay_base_in", Math.Min(Math.Max(ay_q - zwsk_water.Discharge, 0), 20));

            //卫河元村以上基于元村站额外基流
            res.Add("yc_base_in", Math.Min(Math.Max(yc_q - wl_q - ay_q, 0.01),50));

            //小南海水库入库基流
            res.Add("xnhsk_base_in", Math.Min(hs_q, 10));

            //盘石头水库入库基流
            List<Water_Info> now_waterinfo = WG_INFO.Read_Now_WaterInfo();
            double sjksk_outq = now_waterinfo.FirstOrDefault(x => x.stcd == "31005580").q;
            double gssk_outq = now_waterinfo.FirstOrDefault(x => x.stcd == "31006000").q;
            double aysk_outq = now_waterinfo.FirstOrDefault(x => x.stcd == "31006050").q;
            res.Add("pstsk_base_in", Math.Min(Math.Max((sjksk_outq + gssk_outq + aysk_outq),1.74), 10));
            return res;
        }

        //根据站点监测水位内插流量
        private static double Get_SectionQ_FromLevelandQH(List<Water_Condition> now_water, Dictionary<AtReach, List<double[]>> section_qhrelations, string stcd, AtReach section)
        {
            //监测水位
            double station_level = 0;
            for (int i = 0; i < now_water.Count; i++)
            {
                if (now_water[i].Stcd == stcd && now_water[i].Level != 0) station_level = now_water[i].Level;
            }

            //水位流量关系
            List<double[]> qh = new List<double[]>();
            for (int i = 0; i < section_qhrelations.Count; i++)
            {
                if (section_qhrelations.ElementAt(i).Key.reachname == section.reachname &&
                    section_qhrelations.ElementAt(i).Key.chainage == section.chainage) qh = section_qhrelations.ElementAt(i).Value;
            }

            //内插实测流量
            double station_q = Insert_q(qh, station_level);

            return station_q;
        }

        //根据站点监测水位内插流量
        private static double Get_SectionQ_FromLevelandQH(double inital_level, Dictionary<AtReach, List<double[]>> section_qhrelations, AtReach section)
        {
            //水位流量关系
            List<double[]> qh = new List<double[]>();
            for (int i = 0; i < section_qhrelations.Count; i++)
            {
                if (section_qhrelations.ElementAt(i).Key.reachname == section.reachname &&
                    section_qhrelations.ElementAt(i).Key.chainage == section.chainage) qh = section_qhrelations.ElementAt(i).Value;
            }

            //内插实测流量
            double station_q = Insert_q(qh, inital_level);

            return station_q;
        }

        //根据主要河道流量，内插这河道所有控制断面水位
        public static Dictionary<AtReach, double> Insert_SectionLevel_FromQH(Dictionary<AtReach, List<double[]>> section_qhrelations, Dictionary<AtReach, double> reach_q)
        {
            Dictionary<AtReach, double> section_level = new Dictionary<AtReach, double>();

            //分河道处理
            List<string> reach_list = new List<string>();
            for (int i = 0; i < reach_q.Count; i++)
            {
                if (!reach_list.Contains(reach_q.ElementAt(i).Key.reachname)) reach_list.Add(reach_q.ElementAt(i).Key.reachname);
            }

            Dictionary<string, Dictionary<double, double>> reach_q1 = new Dictionary<string, Dictionary<double, double>>();
            string last_reach = reach_q.ElementAt(0).Key.reachname;
            for (int i = 0; i < reach_list.Count; i++)
            {
                string reach = reach_list[i];
                Dictionary<double, double> reach_chainages = new Dictionary<double, double>();
                for (int j = 0; j < reach_q.Count; j++)
                {
                    double chainage = reach_q.ElementAt(j).Key.chainage;
                    double discharge = reach_q.ElementAt(j).Value;
                    if (reach_q.ElementAt(j).Key.reachname == reach)
                    {
                        if (!reach_chainages.Keys.Contains(chainage)) reach_chainages.Add(chainage, discharge);
                    }
                }
                reach_chainages = reach_chainages.OrderBy(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
                reach_q1.Add(reach, reach_chainages);
            }

            for (int i = 0; i < section_qhrelations.Count; i++)
            {
                AtReach at_reach = section_qhrelations.ElementAt(i).Key;
                string reachname = at_reach.reachname;

                //河道流量
                double q = 0;
                for (int j = 0; j < reach_q1.Count; j++)
                {
                    if (reach_q1.ElementAt(j).Key != reachname) continue;

                    Dictionary<double, double> chainage_discharge = reach_q1.ElementAt(j).Value;
                    if(chainage_discharge.Count == 1)
                    {
                        q = chainage_discharge.ElementAt(0).Value;
                    }
                    else
                    {
                        //根据桩号内插流量
                        q = File_Common.Insert_Zd_Value1(chainage_discharge, at_reach.chainage);
                    }
                }

                //河道水位
                List<double[]> qh = section_qhrelations.ElementAt(i).Value;
                double level = Insert_h(qh, q);
                section_level.Add(section_qhrelations.ElementAt(i).Key, level);
            }
            return section_level;
        }

        //根据流量水位关系 由流量内插水位
        public static double Insert_h(List<double[]> qh_relation, double q)
        {
            for (int i = 0; i < qh_relation.Count - 1; i++)
            {
                if (q >= qh_relation[i][0] && q < qh_relation[i + 1][0])
                {
                    double data = qh_relation[i][1] + (qh_relation[i + 1][1] - qh_relation[i][1])
                        * (q - qh_relation[i][0]) / (qh_relation[i + 1][0] - qh_relation[i][0]);
                    return data;
                }
            }
            return 0;
        }

        //根据流量水位关系 由水位内插流量
        public static double Insert_q(List<double[]> qh_relation, double h)
        {
            for (int i = 0; i < qh_relation.Count - 1; i++)
            {
                if (h >= qh_relation[i][1] && h < qh_relation[i + 1][1])
                {
                    double data = qh_relation[i][0] + (qh_relation[i + 1][0] - qh_relation[i][0])
                        * (h - qh_relation[i][1]) / (qh_relation[i + 1][1] - qh_relation[i][1]);
                    return data;
                }
            }
            return 0;
        }

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

        //从模型中获取各水库的初始水位值
        public static Dictionary<string,double> GetRes_InitialValue_FromModel(HydroModel hydromodel)
        {
            Dictionary<string, double> res_initial_level = new Dictionary<string, double>();

            //模型所有初始水位
            Dictionary<AtReach, double> initial_water = hydromodel.Mike11Pars.Hd_Pars.InitialWater;

            //水库初始水位控制的河道断面
            Dictionary<string, List<AtReach>> res_atreachs = WG_INFO.GetRes_ControlInitialAtreach();

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
            Dictionary<string, List<AtReach>> res_atreachs = WG_INFO.GetRes_ControlInitialAtreach();

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

        //获取初始水情条件中需要特殊固定初始水位的 初始条件段
        public static Dictionary<AtReach, double> Get_Custom_Section_InitialLevel()
        {
            Dictionary<AtReach, double> custom_section_level = new Dictionary<AtReach, double>();
            //custom_section_level.Add(AtReach.Get_Atreach("YH", 230000), 114);
            //custom_section_level.Add(AtReach.Get_Atreach("YH", 243505), 110);
            return custom_section_level;
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

        //获取主河道信息(非退水闸支流等虚拟河道)
        public static List<ReachInfo> Get_MainReachInfo(string model_instance,bool main_reach = false)
        {
            //判断一下是否包含所有河道
            List<string> main_reachname = WG_INFO.Get_MainReach_Names(model_instance);

            if (WG_INFO.Main_ReachInfo == null)
            {
                Initial_MainReachInfo(model_instance);
            }
            else 
            {
                List<ReachInfo> reachs = WG_INFO.Main_ReachInfo;
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
                for (int i = 0; i < WG_INFO.Main_ReachInfo.Count; i++)
                {
                    if (main_reachname.Contains(WG_INFO.Main_ReachInfo[i].reachname))
                    {
                        res.Add(WG_INFO.Main_ReachInfo[i]);
                    }
                }
                return res;
            }

            return WG_INFO.Main_ReachInfo;
        }

        //获取主河道信息(非退水闸支流等虚拟河道)
        public static List<Reach_BasePars> Get_MainReach_Info(string model_instance)
        {
            List<Reach_BasePars> main_reachinfo = new List<Reach_BasePars>();

            if (WG_INFO.Main_ReachInfo == null) Initial_MainReachInfo(model_instance);
            List<ReachInfo> main_reach = WG_INFO.Main_ReachInfo;
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reach_name = main_reach[i].reachname;
                string reach_cnname = WG_INFO.Get_ReachChinaName(reach_name);

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

            if (WG_INFO.Main_ReachInfo == null) Initial_MainReachInfo(model_instance);
            List<ReachInfo> main_reach = WG_INFO.Main_ReachInfo;
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reach_name = main_reach[i].reachname;
                string reach_cnname = WG_INFO.Get_ReachChinaName(reach_name);

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
            WG_INFO.Main_ReachInfo = main_reach;
        }

        //获取各河道的建筑物桩号
        public static List<float> Get_ReachStrInfo(string reach_name)
        {
            List<float> str_list = new List<float>();
            for (int i = 0; i < WG_INFO.Str_BaseInfo.Count; i++)
            {
                Struct_BasePars str_basepar = WG_INFO.Str_BaseInfo.ElementAt(i).Value;
                if (str_basepar.reach_name == reach_name) str_list.Add((float)str_basepar.chainage);
            }

            return str_list;
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

        //获取各河道的中文名
        public static string Get_ReachChinaName(string reach_en_name)
        {
            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();
            string name = "";
            for (int i = 0; i < WG_INFO.Str_BaseInfo.Count; i++)
            {
                if (WG_INFO.Str_BaseInfo.ElementAt(i).Value.reach_name == reach_en_name)
                {
                    name = WG_INFO.Str_BaseInfo.ElementAt(i).Value.reach_cnname;
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
        
        //从数据库获取与南水北调总干渠交叉断面的总干渠相关数据
        public static Dictionary<string, ZGQ_SectionInfo> Get_ZGQ_SectionInfo()
        {
            Dictionary<string, ZGQ_SectionInfo> zgq_sectioninfo = new Dictionary<string, ZGQ_SectionInfo>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.NSBD_INFO_TABLENAME;
            

            //先判断数据库中是否存在
            string sql = "select * from " + tableName;
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string name = dt.Rows[i][1].ToString();
                string stcd = dt.Rows[i][2].ToString();
                string reach = dt.Rows[i][3].ToString();
                string reach_cnname = dt.Rows[i][4].ToString();
                double chainage = double.Parse(dt.Rows[i][5].ToString());
                double sj_5n_level = double.Parse(dt.Rows[i][6].ToString());
                double sj_10n_level = double.Parse(dt.Rows[i][7].ToString());
                double sj_20n_level = double.Parse(dt.Rows[i][8].ToString());
                double sj_50n_level = double.Parse(dt.Rows[i][9].ToString());
                double sj_100n_level = double.Parse(dt.Rows[i][10].ToString());
                double sj_300n_level = double.Parse(dt.Rows[i][11].ToString());
                double sj_q = double.Parse(dt.Rows[i][12].ToString());
                double jh_q = double.Parse(dt.Rows[i][13].ToString());
                string dd_level_name = dt.Rows[i][14].ToString();
                double dd_level = double.Parse(dt.Rows[i][15].ToString());
                double hp_bottom_level = double.Parse(dt.Rows[i][16].ToString());
                string other_level_name = dt.Rows[i][17].ToString();
                double other_level = other_level_name ==""?0:double.Parse(dt.Rows[i][18].ToString());
                double jd = double.Parse(dt.Rows[i][19].ToString());
                double wd = double.Parse(dt.Rows[i][20].ToString());

                AtReach atreach = AtReach.Get_Atreach(reach, chainage);
                ZGQ_SectionInfo section_info = new ZGQ_SectionInfo(atreach,sj_5n_level,sj_10n_level,
                    sj_20n_level, sj_50n_level, sj_100n_level, sj_300n_level,sj_q,jh_q, dd_level, 
                    dd_level_name, hp_bottom_level,other_level, other_level_name,jd,wd);

                zgq_sectioninfo.Add(name, section_info);
            }

            
            return zgq_sectioninfo;
        }

        //获取山洪基础信息
        public static List<SH_INFO> Get_Mountain_Flood_BaseInfo()
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODEL_SH_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //山洪基础信息
            List<SH_INFO> sh_infos = new List<SH_INFO>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string region_name = dt.Rows[i][1].ToString();
                string region_code = dt.Rows[i][2].ToString();
                string village_name = dt.Rows[i][3].ToString();
                string village_code = dt.Rows[i][4].ToString();
                float village_x = float.Parse(dt.Rows[i][5].ToString());
                float village_y = float.Parse(dt.Rows[i][6].ToString());
                float village_z = float.Parse(dt.Rows[i][7].ToString());
                string catchment_code = dt.Rows[i][9].ToString();
                string risk_level = dt.Rows[i][10].ToString();

                SH_INFO sh_info = new SH_INFO(region_name, region_code, village_name,
                    village_code, village_x, village_y, village_z, catchment_code, risk_level);
                sh_infos.Add(sh_info);
            }

            return sh_infos;
        }

        //获取工程(水库河道蓄滞洪区) 所在地市和主管单位信息
        public static List<Struct_Region_Info> Get_Struct_LocationInfo(string struct_type = "")
        {
            if (WG_INFO.Struct_Location_Info == null)
            {
                WG_INFO.Struct_Location_Info = Struct_Region_Info.Get_Struct_RegionInfo();
            }

            if(struct_type == "")
            {
                return WG_INFO.Struct_Location_Info;
            }
            else
            {
                List<Struct_Region_Info> all_strinfo = WG_INFO.Struct_Location_Info;
                List<Struct_Region_Info> type_strinfo = new List<Struct_Region_Info>();
                for (int i = 0; i < all_strinfo.Count; i++)
                {
                    if(all_strinfo[i].type == struct_type) type_strinfo.Add(all_strinfo[i]);
                }
                return type_strinfo;
            }
        }

        //获取工程(水库河道蓄滞洪区) 所在地市和主管单位信息 --根据水库和蓄滞洪区中文名称
        public static Struct_Region_Info Get_SingleStruct_LocationInfo(string cn_name)
        {
            Struct_Region_Info res = new Struct_Region_Info();

            if (WG_INFO.Struct_Location_Info == null)
            {
                WG_INFO.Struct_Location_Info = Struct_Region_Info.Get_Struct_RegionInfo();
            }

            List<Struct_Region_Info> all_strinfo = WG_INFO.Struct_Location_Info;
            for (int i = 0; i < all_strinfo.Count; i++)
            {
                if (all_strinfo[i].cn_name == cn_name)
                {
                    res = all_strinfo[i];
                    break;
                }
            }
            return res;
        }

        //获取工程(水库河道蓄滞洪区) 所在地市和主管单位信息 --根据河道位置
        public static Struct_Region_Info Get_SingleStruct_LocationInfo(AtReach atreach)
        {
            Struct_Region_Info res = new Struct_Region_Info();

            if (WG_INFO.Struct_Location_Info == null)
            {
                WG_INFO.Struct_Location_Info = Struct_Region_Info.Get_Struct_RegionInfo();
            }

            List<Struct_Region_Info> all_strinfo = WG_INFO.Struct_Location_Info;
            for (int i = 0; i < all_strinfo.Count; i++)
            {
                if (all_strinfo[i].name == atreach.reachname && 
                    atreach.chainage >= all_strinfo[i].start_chainage && atreach.chainage <= all_strinfo[i].end_chainage)
                {
                    res = all_strinfo[i];
                    break;
                }
            }
            return res;
        }
        
        #endregion *****************************************************************


        #region *********************获取和更新闸站调度*********************
        //从模型中获取建筑物调度信息(初始化默认模型时使用)
        public static string Get_Model_DdInfo(HydroModel hydromodel)
        {
            List<string[]> modeldd_infostr = Get_Model_DdInfoArray(hydromodel);

            string model_ddinfo = File_Common.Serializer_Obj(modeldd_infostr);
            return model_ddinfo;
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
            Dictionary<string, List<AtReach>> res_initialid = WG_INFO.GetRes_ControlInitialAtreach();

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

                res_res.Add(i+1);
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
            Dictionary<string, List<AtReach>> res_initialid = WG_INFO.GetRes_ControlInitialAtreach();

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
                if (Math.Abs(res_levels[res_name] -level)< 0.1) isjc = true;
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
                    if(now_water[j].Stcd == stcd && now_water[j].Datasource != "水库水文站" && !reach_level.Keys.Contains(now_water[j].Name))
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
                    if (reach_station_levels.Keys.Contains(station_name) && Math.Abs(reach_station_levels[station_name]- station_level)< 0.1) isjc = true;
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

        //从模型中获取建筑物调度信息
        public static List<string[]> Get_Model_DdInfoArray(HydroModel hydromodel)
        {
            List<string[]> modeldd_infostr = new List<string[]>();

            //从默认模型中获取建筑物调度信息(初始化默认模型时使用)
            List<Str_DdInfo> gatedd_info = Get_ModelGatedd_Info(hydromodel);

            //分类排序 -- 按河道和建筑物(节制闸、退水闸、泵站、分水闸)分类
            List<ReachInfo> reach_info = hydromodel.Mike11Pars.ReachList.Reach_infolist;
            List<Str_DdInfo> new_gatedd_info = Get_Order_Str(gatedd_info);

            //调度信息
            for (int i = 0; i < new_gatedd_info.Count; i++)
            {
                string str_name = new_gatedd_info[i].str_name;
                Struct_BasePars str_basepar = WG_INFO.Get_StrBaseInfo(str_name);

                //建筑物中文名
                string str_cnname = str_basepar.cn_name;

                //类型中文名
                string str_type = str_basepar.str_type;

                //所在河道中文名
                string reach_cn_name = str_basepar.reach_cnname;

                //获取调度方式中文名、闸门及值
                string strdd_cn_name = new_gatedd_info[i].Get_DdType_CNName();
                int open_n = new_gatedd_info[i].open_n; 
                double value = Math.Round(new_gatedd_info[i].dd_value, 3); //保留3位小数

                //调度
                List<string[]> dd_info = new List<string[]>();
                string[] gate_dd = new string[] { "", strdd_cn_name.ToString(), open_n.ToString(), value.ToString() };
                dd_info.Add(gate_dd);

                string dd_str = File_Common.Serializer_Obj(dd_info);
                string[] dd_strs = { str_name, (i + 1).ToString(), str_cnname, str_type, reach_cn_name, dd_str };
                modeldd_infostr.Add(dd_strs);
            }

            return modeldd_infostr;
        }

        //获取 指定模型 的初始条件\边界条件、闸站调度等单一参数信息(从数据库，经常性查询)
        public static string Get_Model_SingleParInfo(string plan_code,string field_name)
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

        //获取该方案水库、河道、蓄滞洪区 简短调度指令
        public static string Get_Model_DispatchPlan_Fromdb(string plan_code)
        {
            //定义连接的数据库和表
            string moel_instance = Mysql_GlobalVar.now_instance;
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select mike11_strdd_info from " + tableName +
                " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //从数据库获取该方案 建筑物调度信息
            string strddinfo_str = dt.Rows[0][0].ToString();
            List<string[]> dd_list = JsonConvert.DeserializeObject<List<string[]>>(strddinfo_str);

            //获取建筑物基本信息
            Dictionary<string, Struct_BasePars> str_baseinfo = WG_INFO.Get_StrBaseInfo();

            //获取模型统计data结果
            Dictionary<string, object> model_result = HydroModel.Get_Mike11_TjResult(plan_code);

            //获取水库调度指令
            Dictionary<string, Reservoir_FloodRes> reservoir_result = model_result["reservoir_result"] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, Dispatch_Info> reservoir_ddplan = Get_ResGate_DispatchPlan(str_baseinfo, dd_list,reservoir_result);

            //获取河道节点控制闸站调度指令
            Dictionary<string, ReachSection_FloodRes> reach_station_res = model_result["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Dispatch_Info> reachgate_ddplan = Get_ReachGate_DispatchPlan(str_baseinfo,dd_list, reach_station_res);

            //获取蓄滞洪区调度指令
            Dictionary<string, Xzhq_FloodRes> xzhq_res = model_result["floodblq_result"] as Dictionary<string, Xzhq_FloodRes>;
            Dictionary<string, Dispatch_Info> xzhq_ddplan = Get_Xzhq_DispatchPlan(xzhq_res);

            //各类型调度组合
            Dictionary<string, object> model_ddplan = new Dictionary<string, object>();
            model_ddplan.Add("reservoir_dispatch", reservoir_ddplan);
            model_ddplan.Add("reach_dispatch", reachgate_ddplan);
            model_ddplan.Add("xzhq_dispatch", xzhq_ddplan);

            return File_Common.Serializer_Obj(model_ddplan);
        }

        //获取水库的调度方案指令
        public static Dictionary<string, Dispatch_Info> Get_ResGate_DispatchPlan(Dictionary<string, Struct_BasePars> str_baseinfo,
            List<string[]> dd_list, Dictionary<string, Reservoir_FloodRes> reservoir_result)
        {
            Dictionary<string, Dispatch_Info> res_dd_plan = new Dictionary<string, Dispatch_Info>();
            for (int i = 0; i < reservoir_result.Count; i++)
            {
                string res_name = reservoir_result.ElementAt(i).Key;
                Reservoir_FloodRes reservoir_res = reservoir_result.ElementAt(i).Value;
                DateTime start_time = reservoir_res.Level_Dic.ElementAt(0).Key;

                //获取该水库泄洪建筑集合
                List<Struct_BasePars> res_strlist = WG_INFO.Get_Res_All_StrInfo(res_name);

                //只保留溢洪道可以控制的水库
                if (!Struct_BasePars.Have_GateYhd(res_strlist)) continue;

                //判断水库的是否为规程调度(各建筑均为规程调度)
                bool res_isgzdd = Res_IsGzdd(dd_list, res_strlist);
                string res_dispatch_type = res_isgzdd ? "规程调度": "指令调度";

                //水库调度方案和 水库各泄洪建筑调度指令
                string res_dispatch_plan;
                Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();
                double max_outq = Math.Round(reservoir_res.Max_OutQ);
                DateTime flood_outtime = reservoir_res.OutQ_Dic.Values.Max() == 0? start_time:reservoir_res.OutQ_Dic.First(kvp => kvp.Value != 0).Key;
                if (max_outq < 10)
                {
                    //水库工程调度方案
                    res_dispatch_plan = $"{res_dispatch_type}，水库各建筑不泄洪";

                    //水库工程的 各建筑物调度指令
                    for (int j = 0; j < res_strlist.Count; j++)
                    {
                        gate_dispatch.Add(res_strlist[j].cn_name, new List<string>() { $"{start_time}开始，全程关闭不泄洪" });
                    }
                }
                else
                {
                    //水库工程调度方案
                    double max_outq1 = Math.Round(max_outq / 10) * 10;
                    res_dispatch_plan = $"{res_dispatch_type}，控制水库下泄流量不超{max_outq1}m³/s";

                    //水库泄洪洞设计过流能力
                    double res_xhq_q = 0;
                    for (int j = 0; j < res_strlist.Count; j++)
                    {
                        if (res_strlist[j].str_type == "泄洪洞") res_xhq_q += res_strlist[j].design_q;
                    }

                    //获取各水库建筑的调度指令
                    gate_dispatch = Get_Res_StructDDinfo(res_strlist, max_outq, res_xhq_q, start_time, flood_outtime);
                }

                //工程和闸门调度指令
                Dispatch_Info dispatch_info_obj = new Dispatch_Info(res_dispatch_plan, gate_dispatch, reservoir_res.Stcd);
                res_dd_plan.Add(res_name, dispatch_info_obj);
            }

            return res_dd_plan;
        }

        //判断水库的是否为规程调度(各建筑均为规程调度)
        private static bool Res_IsGzdd(List<string[]> dd_list, List<Struct_BasePars> res_strlist)
        {
            //判断是规程调度还是指令调度
            bool res_isgzdd = true;
            for (int j = 0; j < res_strlist.Count; j++)
            {
                for (int k = 0; k < dd_list.Count; k++)
                {
                    string dd_strname = dd_list[k][0];
                    List<string[]> str_dd_list = JsonConvert.DeserializeObject<List<string[]>>(dd_list[k][5]);
                    string dd_type = str_dd_list[0][1];
                    if (dd_type != "规则")
                    {
                        res_isgzdd = false; break;
                    }
                }
            }
            return res_isgzdd;
        }

        //获取各水库建筑的调度指令
        public static Dictionary<string,List<string>> Get_Res_StructDDinfo(List<Struct_BasePars> res_strlist,
            double max_outq,double res_xhq_q, DateTime start_time,DateTime flood_outtime)
        {
            Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();

            //水库工程的 各建筑物调度指令
            double last_design_q = 0;
            for (int j = 0; j < res_strlist.Count; j++)
            {
                double design_q = res_strlist[j].design_q;
                DateTime str_ddtime = start_time;

                string gate_dd_info = "";
                if (res_strlist[j].str_type == "泄洪洞")
                {
                    double min_q = Math.Round(Math.Min(design_q, max_outq) / 10) * 10;
                    if (last_design_q == 0)
                    {
                        gate_dd_info = $"控制下泄流量不超{min_q}m³/s";
                    }
                    else
                    {
                        double design_last_q = Math.Round((design_q - last_design_q) / 10) * 10;
                        gate_dd_info = last_design_q >= max_outq ? "泄洪洞全程关闭": $"控制下泄流量不超{design_last_q}m³/s";
                    }

                    if (gate_dd_info.Contains("m³/s")) str_ddtime = flood_outtime;
                    last_design_q = design_q;
                }
                else if (res_strlist[j].str_type == "溢洪道")
                {
                    if (res_strlist[j].gate_type != GateType.NOGATE)
                    {
                        if (res_xhq_q - max_outq >= -1)
                        {
                            gate_dd_info = $"溢洪道全程关闭";
                        }
                        else
                        {
                            double yhd_q = Math.Round((max_outq - res_xhq_q) / 10) * 10;
                            gate_dd_info = $"控制下泄流量不超{yhd_q}m³/s";
                        }
                    }
                    else
                    {
                        if (res_xhq_q - max_outq >= -1)
                        {
                            gate_dd_info = $"溢洪道全程不溢流";
                        }
                        else
                        {
                            double yhd_q = Math.Round((max_outq - res_xhq_q) / 10) * 10;
                            gate_dd_info = $"溢洪道自由溢流最大流量{yhd_q}m³/s";
                        }
                    }

                    if (gate_dd_info.Contains("m³/s")) str_ddtime = flood_outtime;
                }

                gate_dispatch.Add(res_strlist[j].cn_name, new List<string>() { $"{str_ddtime}开始, {gate_dd_info}" });
            }

            return gate_dispatch;
        }

        //获取河道节点控制闸站调度方案指令
        public static Dictionary<string, Dispatch_Info> Get_ReachGate_DispatchPlan(Dictionary<string, Struct_BasePars> str_baseinfo,
            List<string[]> dd_list, Dictionary<string, ReachSection_FloodRes> reach_station_res)
        {
            Dictionary<string, Dispatch_Info> reachgate_dd_plan = new Dictionary<string, Dispatch_Info>();

            //获取河道主要节点控制建筑
            Dictionary<string, List<string>> main_str = Get_Main_ControlStr();
            List<string> reach_main_gate = main_str["reach_main_gate"];
            DateTime start_time = reach_station_res.ElementAt(0).Value.Discharge_Dic.ElementAt(0).Key;

            //各个节点河道闸站调度方案指令
            for (int i = 0; i < reach_main_gate.Count; i++)
            {
                Struct_BasePars gate_baseinfo = str_baseinfo[reach_main_gate[i]];
                double max_q = reach_station_res.Keys.Contains(gate_baseinfo.cn_name) ? reach_station_res[gate_baseinfo.cn_name].Max_Qischarge : 0;
                max_q = Math.Round(max_q/10)*10;
                Dictionary<DateTime,double> gate_qdic = reach_station_res.Keys.Contains(gate_baseinfo.cn_name) ? reach_station_res[gate_baseinfo.cn_name].Discharge_Dic: null;
                string str_stcd = reach_station_res.Keys.Contains(gate_baseinfo.cn_name) ? reach_station_res[gate_baseinfo.cn_name].Stcd:"";

                //获取该建筑物的调度方式
                Dispatch_Info dispatch_info_obj = new Dispatch_Info();
                dispatch_info_obj.stcd = str_stcd;

                //调度方案
                string dispatch_plan = "";

                //建筑物调度详情
                Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();
                for (int j = 0; j < dd_list.Count; j++)
                {
                    string dd_strname = dd_list[j][0];

                    if(dd_strname == reach_main_gate[i])
                    {
                        List<string[]> str_dd_list = JsonConvert.DeserializeObject<List<string[]>>(dd_list[j][5]);
                        string str_dispatch_type = str_dd_list[0][1] == "规则"? "规程调度" : "指令调度";
                        if (max_q < 10)
                        {
                            dispatch_plan = $"{str_dispatch_type}，闸门全程关闭";
                        }
                        else
                        {
                            dispatch_plan = $"{str_dispatch_type}，控制过闸流量不超{max_q}m³/s";
                        }

                        //获取单个河道控制建筑物调度指令
                        List<string> str_dispatch = Get_Gate_DispatchPlan(dd_list[j], gate_baseinfo, max_q, gate_qdic, dd_strname, start_time);

                        gate_dispatch.Add(gate_baseinfo.cn_name, str_dispatch);
                        dispatch_info_obj.gate_dispatch = gate_dispatch;
                        break;
                    }
                }

                dispatch_info_obj.dispatch_plan = dispatch_plan;
                reachgate_dd_plan.Add(gate_baseinfo.cn_name, dispatch_info_obj);
            }

            return reachgate_dd_plan;
        }

        //获取单个河道控制建筑物调度指令
        private static List<string> Get_Gate_DispatchPlan(string[] dd_list, Struct_BasePars gate_baseinfo, double max_q,
            Dictionary<DateTime, double> gate_qdic, string dd_strname,DateTime start_time)
        {
            List<string[]> str_dd_list = JsonConvert.DeserializeObject<List<string[]>>(dd_list[5]);
            string dd_type = str_dd_list[0][1];
            string dd_n = str_dd_list[0][2];
            string dd_value = str_dd_list[0][3];

            //建筑物调度方案指令
            List<string> str_dispatch = new List<string>();
            if (dd_type == "规则")
            {
                if (max_q < 10)
                {
                    string str_ddmessage = $"{start_time}开始，闸门全程关闭";
                    str_dispatch.Add(str_ddmessage);
                }
                else
                {
                    if (dd_strname == "QHNZ_XHKJZZ")  //小河口闸
                    {
                        string cx_start_time = gate_qdic == null ? "" : SimulateTime.TimetoStr(gate_qdic.FirstOrDefault(x => x.Value > 400).Key);
                        string close_start_time = gate_qdic == null ? "" : SimulateTime.TimetoStr(gate_qdic.LastOrDefault(x => x.Value > 10).Key);
                        string str_ddmessage1 = $"1、{start_time}开始，闸门控泄流量不超400m³/s";
                        string str_ddmessage2 = $"2、{cx_start_time}开始,闸门全开敞泄";
                        string str_ddmessage3 = $"3、{close_start_time}开始,闸门全闭";
                        str_dispatch.Add(str_ddmessage1);
                        str_dispatch.Add(str_ddmessage2);
                        str_dispatch.Add(str_ddmessage3);
                    }
                    else if (dd_strname == "GQ_YTZJZZ")  //盐土庄闸
                    {
                        string str_ddmessage = $"{start_time}开始，控制下泄流量不超{gate_baseinfo.design_q}m³/s";
                        str_dispatch.Add(str_ddmessage);
                    }
                    else
                    {
                        string str_ddmessage = gate_baseinfo.ddrule_info;
                        str_dispatch.Add(str_ddmessage);
                    }
                }
            }
            else if (dd_type == "全开")
            {
                string str_ddmessage = $"{start_time}开始，全程{gate_baseinfo.gate_h}孔闸门全开";
                str_dispatch.Add(str_ddmessage);
            }
            else if (dd_type == "全关")
            {
                string str_ddmessage = $"{start_time}开始，闸门全程关闭";
                str_dispatch.Add(str_ddmessage);
            }
            else if (dd_type == "控泄")
            {
                string str_ddmessage = $"{start_time}开始，全程控制下泄流量不超{dd_value}m³/s";
                str_dispatch.Add(str_ddmessage);
            }
            else if (dd_type == "时间")
            {
                for (int k = 0; k < str_dd_list.Count; k++)
                {
                    string ddtime = str_dd_list[k][0];
                    string ddtype = str_dd_list[k][1];
                    string ddn = str_dd_list[k][2];
                    string ddvalue = str_dd_list[k][3];

                    string str_ddmessage = $"{k + 1}、{ddtime},调度方式:{ddtype},调度值:{ddvalue}";
                    str_dispatch.Add(str_ddmessage);
                }
            }
            else if (dd_type == "半开")  //全程半开
            {
                string str_ddmessage = $"{start_time}开始，全程{dd_n}孔闸门半开,平均开度{dd_value}m";
                str_dispatch.Add(str_ddmessage);
            }

            return str_dispatch;
        }

        //获取蓄滞洪区调度指令
        public static Dictionary<string, Dispatch_Info> Get_Xzhq_DispatchPlan(Dictionary<string, Xzhq_FloodRes> xzhq_res)
        {
            Dictionary<string, Dispatch_Info> xzhq_dd_plan = new Dictionary<string, Dispatch_Info>();
            for (int i = 0; i < xzhq_res.Count; i++)
            {
                string xzhq_name = xzhq_res.ElementAt(i).Key;
                Xzhq_FloodRes res = xzhq_res.ElementAt(i).Value;

                //蓄滞洪区调度指令和各建筑物调度指令
                string dispatch_plan;
                Dictionary<string, List<string>> gate_dispatch = new Dictionary<string, List<string>>();
                List<string> fhy_list = res.Max_FhyInQ.Keys.ToList();
                if (res.Xzhq_State == "未启用")
                {
                    //蓄滞洪区调度方案
                    dispatch_plan = "蓄滞洪区不启用";

                    //蓄滞洪区工程的 各建筑物调度指令
                    for (int j = 0; j < fhy_list.Count; j++)
                    {
                        gate_dispatch.Add(fhy_list[j], new List<string>() {"不启用"});
                    }
                }
                else
                {
                    //蓄滞洪区工程调度指令
                    double fhy_q_sum = res.Max_FhyInQ.Values.Sum();
                    string jh_type = fhy_q_sum == 0 ? "河道漫溢": "分洪闸堰";
                    dispatch_plan = $"{res.Start_FloodTime},通过{jh_type}启用蓄滞洪区";

                    //蓄滞洪区工程的 各建筑物调度指令
                    Dictionary<string, Dictionary<DateTime, double>> allstr_inq = res.InQ_Dic;
                    for (int j = 0; j < fhy_list.Count; j++)
                    {
                        List<string> fhydd_list = new List<string>();

                        string fhy_ddinfo;
                        double max_q = res.Max_FhyInQ[fhy_list[j]];
                        if(max_q == 0)
                        {
                            fhy_ddinfo = $"全过程不分洪";
                        }
                        else
                        {
                            Dictionary<DateTime, double> q_list = allstr_inq.Keys.Contains(fhy_list[j]) ? allstr_inq[fhy_list[j]] : null;
                            string start_time = q_list == null ? res.Start_FloodTime : SimulateTime.TimetoStr(q_list.FirstOrDefault(x => x.Value > 0).Key);
                            fhy_ddinfo = $"{start_time},启用分洪";
                        }
                        fhydd_list.Add(fhy_ddinfo);
                        gate_dispatch.Add(fhy_list[j], fhydd_list);
                    }
                }

                //工程和闸门调度指令
                Dispatch_Info dispatch_info_obj = new Dispatch_Info(dispatch_plan, gate_dispatch, res.Stcd);
                xzhq_dd_plan.Add(xzhq_name, dispatch_info_obj);
            }

            return xzhq_dd_plan;
        }

        //分类获取主要控制建筑
        public static Dictionary<string, List<string>> Get_Main_ControlStr()
        {
            Dictionary<string, List<string>> main_str = new Dictionary<string, List<string>>();

            //获取建筑物基本信息
            Dictionary<string, Struct_BasePars> str_info = WG_INFO.Get_StrBaseInfo();

            //分类获取建筑物，键为水库、河道、蓄滞洪区
            List<string> res_str = new List<string>();
            List<string> reach_str = new List<string>();
            List<string> xzhq_str = new List<string>();
            List<string> other_str = new List<string>();
            for (int i = 0; i < str_info.Count; i++)
            {
                string str_name = str_info.ElementAt(i).Key;
                Struct_BasePars str = str_info.ElementAt(i).Value;
                if (str.control_str != "")
                {
                    //主要控制闸门 -- 水库
                    if (str.str_type == "溢洪道" || str.str_type == "泄洪洞") res_str.Add(str_name);

                    //主要控制闸门 -- 河道闸站
                    if (str.str_type == "拦河闸" || str.str_type == "退水闸") reach_str.Add(str_name);

                    //主要控制闸门 -- 蓄滞洪区
                    if (str.str_type == "分洪闸" || str.str_type == "分洪堰") xzhq_str.Add(str_name);
                }
                else
                {
                    //其他非控制闸门
                    other_str.Add(str_name);
                }
            }

            main_str.Add("main_res_str", res_str);
            main_str.Add("reach_main_gate", reach_str);
            main_str.Add("main_xzhq_str", xzhq_str);
            main_str.Add("other_str", other_str);

            return main_str;
        }

        //建筑物按桩号排序
        public static List<Str_DdInfo> Get_Order_Str(List<Str_DdInfo> gatedd_info)
        {
            List<Str_DdInfo> new_gatedd_info = new List<Str_DdInfo>();

            //分类
            List<Str_DdInfo> jzz_gate = new List<Str_DdInfo>();
            List<Str_DdInfo> tsz_gate = new List<Str_DdInfo>();
            List<Str_DdInfo> bz_gate = new List<Str_DdInfo>();
            List<Str_DdInfo> fsk_gate = new List<Str_DdInfo>();
            for (int i = 0; i < gatedd_info.Count; i++)
            {
                Struct_BasePars str_par = WG_INFO.Get_StrBaseInfo(gatedd_info[i].str_name);
                if (str_par.gate_type == GateType.YLZ) jzz_gate.Add(gatedd_info[i]);
                if (str_par.gate_type == GateType.PBZ) tsz_gate.Add(gatedd_info[i]);
                if (str_par.gate_type == GateType.BZ) bz_gate.Add(gatedd_info[i]);
                if (str_par.gate_type == GateType.LLZ) fsk_gate.Add(gatedd_info[i]);
            }

            //按桩号排序
            //jzz_gate = jzz_gate.OrderBy(p => p.atreach.reachchainage).ToList();  //节制闸不排序了
            tsz_gate = tsz_gate.OrderBy(p => p.atreach.chainage).ToList();
            bz_gate = bz_gate.OrderBy(p => p.atreach.chainage).ToList();
            fsk_gate = fsk_gate.OrderBy(p => p.atreach.chainage).ToList();

            //集合在一起
            new_gatedd_info.AddRange(jzz_gate);
            new_gatedd_info.AddRange(tsz_gate);
            new_gatedd_info.AddRange(bz_gate);
            new_gatedd_info.AddRange(fsk_gate);

            return new_gatedd_info;
        }

        //从模型中获取建筑物调度信息
        public static List<Str_DdInfo> Get_ModelGatedd_Info(HydroModel hydromodel)
        {
            List<Str_DdInfo> modeldd_info = new List<Str_DdInfo>();

            //从建筑物集合中获取模型模型调度信息
            List<Normalstr> gate_list = Normalstr.Get_Model_Normalstr(hydromodel);
            for (int i = 0; i < gate_list.Count; i++)
            {
                //获取建筑物调度方式(全开、全关、指定开度、指定流量)、开闸数量和调度值
                int open_n = gate_list[i].Ddparams_zmdu.opengatenumber;        //开闸数量
                double dd_value = 0;   //调度值

                //判断建筑物调度方式
                Str_DdType str_ddtype = gate_list[i].Strddgz == CtrDdtype.GZDU? Str_DdType.Rule: Get_Str_DdType(gate_list[i], ref dd_value);

                //建筑物调度信息
                Str_DdInfo str_ddinfo = new Str_DdInfo(gate_list[i].Strname, gate_list[i].Reachinfo, str_ddtype, open_n,dd_value);

                if(str_ddtype == Str_DdType.Open)
                {
                    str_ddinfo.open_n = gate_list[i].Stratts.gate_count;
                    str_ddinfo.dd_value = gate_list[i].Stratts.gate_height;
                }
                else if(str_ddtype == Str_DdType.Close)
                {
                    str_ddinfo.open_n = 0;
                    str_ddinfo.dd_value = 0;
                }
                else if (str_ddtype == Str_DdType.Set_Q)
                {
                    str_ddinfo.open_n = 0;
                }

                modeldd_info.Add(str_ddinfo);
            }
            return modeldd_info;
        }

        //判断建筑物调度方式
        public static Str_DdType Get_Str_DdType(Normalstr str, ref double dd_value)
        {
            Str_DdType str_ddtype;
            if (str.Stratts.gate_type == 2)   //设置流量
            {
                str_ddtype = Str_DdType.Set_Q;
                dd_value = Math.Max(str.Ddparams_double, str.Stratts.max_value);
            }
            else   //其他闸门类型
            {
                if (str.Ddparams_zmdu.opengatenumber == 0)
                {
                    str_ddtype = Str_DdType.Close;
                }
                else if (str.Ddparams_zmdu.fullyopen == true)
                {
                    str_ddtype = Str_DdType.Open;
                }
                else
                {
                    str_ddtype = Str_DdType.Set_H;
                    dd_value = str.Ddparams_zmdu.opengateheight - str.Stratts.sill_level;
                }
            }

            return str_ddtype;
        }

        //更新所有闸门的调度(每个建筑物4个参数 -- 英文名、调度方式、开闸数量、调度量)
        public static void Update_AllStr_DdInfo(ref HydroModel hydromodel, Dictionary<string, List<DdInfo>> new_ddinfos)
        {
            //逐一修改闸门调度方式
            for (int i = 0; i < new_ddinfos.Count; i++)
            {
                string strname = new_ddinfos.ElementAt(i).Key;
                List<DdInfo> gate_dds = new_ddinfos.ElementAt(i).Value;

                //修改该闸门调度方式
                if(gate_dds.Count == 1 && gate_dds[0].dd_time == "")   
                {
                    //只有一个调度方式，从头到尾全过程一样
                    Change_Str_Zldd(ref hydromodel, strname, gate_dds[0].dd_type, gate_dds[0].open_n, gate_dds[0].dd_value);
                }
                else
                {
                    //按时间调度闸门 (开闸关闸所需时间按默认)
                    Nwk11.Changeddgz_ToZLDD_TIME(ref hydromodel, strname, gate_dds);
                }
            }
        }

        //刷选出需要修改的这些闸站原始调度信息
        public static List<string[]> FilterData(List<string[]> source_ddinfo, List<string[]> new_ddinfos)
        {
            List<string[]> filteredData = new List<string[]>();
            for (int i = 0; i < source_ddinfo.Count; i++)
            {
                string firstString = source_ddinfo[i][0];
                for (int j = 0; j < new_ddinfos.Count; j++)
                {
                    if (firstString == new_ddinfos[j][0])
                    {
                        filteredData.Add(source_ddinfo[i]);
                        break;
                    }
                }
            }
            return filteredData;
        }

        //更新单个闸门的调度(4个参数 -- 英文名、调度方式、开闸数量、调度量)
        public static void Update_Str_DdInfo(ref HydroModel hydromodel, string[] new_ddinfo)
        {
            if (new_ddinfo.Length != 3) return;

            int open_n = 0;
            if (new_ddinfo[2] != "" && new_ddinfo[2] != null) open_n = int.Parse(new_ddinfo[2]);

            double dd_value = 0;
            if (new_ddinfo[3] != "" && new_ddinfo[3] != null) dd_value = double.Parse(new_ddinfo[3]);

            //修改该闸门调度方式
            Change_Str_Zldd(ref hydromodel, new_ddinfo[0], new_ddinfo[1], open_n, dd_value);
        }

        //根据数据库闸站状态，修改所有的建筑物闸站调度方式及数据
        public static void Update_AllStr_DdInfo_FromGB(ref HydroModel hydromodel)
        {
            //从数据库获取当前闸门状态
            List<Gate_StateInfo> gate_state = WG_INFO.Read_NowGateState();

            //逐一修改修改闸门调度方式
            Dictionary<string, int> str_baseinfo = hydromodel.Mike11Pars.ControlstrList.Gatebaseinfo;
            for (int i = 0; i < gate_state.Count; i++)
            {
                string strname = gate_state[i].str_name;
                if (!str_baseinfo.Keys.Contains(strname)) continue;  //建筑物基本信息表里有mike11可控建筑物没有的建筑物
                string cn_ddfs = gate_state[i].nowState;
                double dd_value = gate_state[i].openHQ;
                int open_n = gate_state[i].openN;
                Change_Str_Zldd(ref hydromodel, strname, cn_ddfs, open_n, dd_value);
            }
        }

        //修改某闸门的调度方式(闸门状态全过程一样)
        public static void Change_Str_Zldd(ref HydroModel hydromodel, string strname, string str_ddtype, int open_n = 0, double dd_value = 0)
        {
            //获取该闸门的基本信息
            Struct_BasePars str_basepar = Get_StrBaseInfo(strname);

            //判断
            Str_DdType dd_type = Str_DdInfo.Get_DdType(str_ddtype);
            switch (dd_type)
            {
                case Str_DdType.Open:
                    if(str_basepar.gate_type == GateType.LLZ || str_basepar.gate_type == GateType.BZ)
                    {
                        //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
                        Change_Tokxdd(ref hydromodel, strname, str_basepar.design_q);
                    }
                    else
                    {
                        Nwk11.Changeddgz_ToZMDU_FullOpen(ref hydromodel, strname);
                    }
                    break;
                case Str_DdType.Close:
                    if (str_basepar.gate_type == GateType.LLZ || str_basepar.gate_type == GateType.BZ)
                    {
                        //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
                        Change_Tokxdd(ref hydromodel, strname, 0);
                    }
                    else
                    {
                        Nwk11.Changeddgz_ToZMDU_FullClose(ref hydromodel, strname);
                    }
                    break;
                case Str_DdType.Set_H:
                    Nwk11.Changeddgz_ToZMDU_SetH(ref hydromodel, strname, str_basepar.gate_type, dd_value, open_n);

                    //在原溢流闸门处增加一个新的溢流闸门，闸门数量为未打开闸门数,状态为全关(溢流类型的闸门全关后水流依然能从上面溢流)
                    if (str_basepar.gate_type == GateType.YLZ && hydromodel.Mike11Pars.ControlstrList.GetGate(strname).Stratts.gate_count != open_n)
                    {
                        Nwk11.Add_WeirGate_SyGaten(ref hydromodel, strname, open_n);
                    }
                    break;
                case Str_DdType.Set_Q:
                    //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
                    Change_Tokxdd(ref hydromodel, strname,dd_value);
                    break;
                case Str_DdType.Rule:
                    //按规则调度
                    Nwk11.Changeddgz_ToGZDU(ref hydromodel, strname);
                    break;
                default:
                    break;
            }
        }

        //按水位流量关系控泄，防止流量超过建筑物本身在该水位下的过流能力
        private static void Change_Tokxdd(ref HydroModel hydromodel, string strname, double dd_value)
        {
            //获取水位流量关系
            List<double[]> str_qh1 = WG_INFO.Get_StrQHrelation(strname);

            if (str_qh1.Count != 0 && dd_value != 0)
            {
                Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, dd_value, str_qh1);
            }
            else
            {
                Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, dd_value);
            }
        }

        #endregion ***********************************************************


        #region ********************建筑物名称 类型 调度规则**********************
        //获取下一个最近的 指定类型 建筑物名称及其桩号
        public static void Get_NextNearStr(HydroModel hydromodel, AtReach atreach, GateType gatetype, out string near_strname, out double near_strchainage)
        {
            double neardistance = 1000000;
            near_strname = "";
            near_strchainage = 0.0;
            List<Controlstr> controlstr_list = hydromodel.Mike11Pars.ControlstrList.GateListInfo;

            //遍历所有可控建筑物，找到与给定类型相同的，且位于下游最近的一个
            for (int i = 0; i < controlstr_list.Count; i++)
            {
                if (controlstr_list[i].Strname.EndsWith(gatetype.ToString()))
                {
                    double str_chainage = 0.0;
                    string reachname = controlstr_list[i].Reachinfo.reachname;
                    ReachInfo reach_info = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);

                    //如果该建筑物不在给定河道上，但连接到该河道，则按连接桩号
                    if (reachname == atreach.reachname)
                    {
                        str_chainage = controlstr_list[i].Reachinfo.chainage;
                    }
                    else if (reach_info.upstream_connect.reachname == atreach.reachname)  //如果上游连接河道与给定河道相同也行
                    {
                        str_chainage = reach_info.upstream_connect.chainage;
                    }
                    else if (reach_info.downstream_connect.reachname == atreach.reachname)  //如果下游连接河道与给定河道相同也行
                    {
                        str_chainage = reach_info.downstream_connect.chainage;
                    }

                    if (str_chainage >= atreach.chainage)
                    {
                        double str_distance = str_chainage - atreach.chainage;
                        if (str_distance < neardistance)
                        {
                            near_strname = controlstr_list[i].Strname;
                            near_strchainage = str_chainage;
                            neardistance = str_distance;
                        }
                    }
                }
            }

        }

        //获取所有指定类型的建筑物，如节制闸、分水闸、退水闸
        public static Dictionary<string, AtReach> Get_TypeGate_FromModel(HydroModel model, GateType gate_type)
        {
            ControlList controllist = model.Mike11Pars.ControlstrList;
            Dictionary<string, AtReach> reachdic = new Dictionary<string, AtReach>();

            //获取指定类型建筑物(根据名称最后字符判断)
            for (int i = 0; i < controllist.GateListInfo.Count; i++)
            {
                if (controllist.GateListInfo[i].Strname.EndsWith(gate_type.ToString()))
                {
                    reachdic.Add(controllist.GateListInfo[i].Strname, controllist.GateListInfo[i].Reachinfo);
                }
            }
            return reachdic;
        }

        //从建筑物基本参数里 获取所有指定类型的建筑物，如节制闸、分水闸、退水闸
        public static Dictionary<string, AtReach> Get_TypeGate_FromStrBaseInfo(HydroModel model, GateType gate_type)
        {
            Dictionary<string, AtReach> reachdic = new Dictionary<string, AtReach>();
            if (gate_type == GateType.PBZ)
            {
                reachdic = Get_TypeGate_FromModel(model, GateType.PBZ);
                return reachdic;
            }

            //获取指定类型建筑物(根据名称最后字符判断)
            for (int i = 0; i < WG_INFO.Str_BaseInfo.Count; i++)
            {
                if (WG_INFO.Str_BaseInfo.ElementAt(i).Value.gate_type == gate_type)
                {
                    //获取名
                    string strname = WG_INFO.Str_BaseInfo.ElementAt(i).Key;
                    AtReach atreach = AtReach.Get_Atreach(WG_INFO.Str_BaseInfo.ElementAt(i).Value.reach_name, WG_INFO.Str_BaseInfo.ElementAt(i).Value.chainage);
                    if (!reachdic.Keys.Contains(strname)) reachdic.Add(strname, atreach);
                }
            }
            return reachdic;
        }

        //从建筑物基本参数里 获取节制闸
        public static Dictionary<string, Dictionary<string, double>> Get_JZZ_FromStrBaseInfo()
        {
            Dictionary<string, Dictionary<string, double>> res = new Dictionary<string, Dictionary<string, double>>();

            //获取JZZ
            Dictionary<string, AtReach> reach_jzzdic = new Dictionary<string, AtReach>();
            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();
            for (int i = 0; i < WG_INFO.Str_BaseInfo.Count; i++)
            {
                if (WG_INFO.Str_BaseInfo.ElementAt(i).Value.gate_type == GateType.YLZ)
                {
                    //获取名
                    string strname = WG_INFO.Str_BaseInfo.ElementAt(i).Key;
                    string str_cnname = WG_INFO.Str_BaseInfo.ElementAt(i).Value.cn_name;
                    AtReach atreach = AtReach.Get_Atreach(WG_INFO.Str_BaseInfo.ElementAt(i).Value.reach_name, WG_INFO.Str_BaseInfo.ElementAt(i).Value.chainage);
                    if (!reach_jzzdic.Keys.Contains(strname)) reach_jzzdic.Add(str_cnname, atreach);
                }
            }

            //按河道分类

            List<Reach_BasePars> main_reach = WG_INFO.Get_MainReach_Info(Mysql_GlobalVar.now_instance);
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reach_name = main_reach[i].name;
                Dictionary<string, double> jzz = new Dictionary<string, double>();
                for (int j = 0; j < reach_jzzdic.Count; j++)
                {
                    if (reach_jzzdic.ElementAt(j).Value.reachname == reach_name)
                    {
                        jzz.Add(reach_jzzdic.ElementAt(j).Key, reach_jzzdic.ElementAt(j).Value.chainage);
                    }
                }
                res.Add(reach_name, jzz);
            }

            return res;
        }

        //获取各类型闸门规则调度方式
        public static string Get_Gate_Rule(GateType gate_type)
        {
            switch (gate_type)
            {
                case GateType.YLZ:
                    return "闸门全程开启";
                case GateType.PBZ:
                    return "闸门全程关闭";
                case GateType.LLZ:
                    return "闸前水位大于最低运行水位时开启，否则关闭";
                case GateType.BZ:
                    return "闸前水位大于最低运行水位时开启，否则关闭";
                default:
                    return "";
            }
        }

        //获取闸门类型
        public static GateType Get_Gate_Type(string name)
        {
            Struct_BasePars str_par = WG_INFO.Get_StrBaseInfo(name);

            return str_par.gate_type;
        }

        //获取模型模拟所需时间(秒)
        public static double Get_ModelRun_ElispedTime(double time_hours)
        {
            //计算时间步长按固定考虑
            double elasp_second = 10;
            if (time_hours <= 24)
            {
                elasp_second = 15;
            }
            else if (time_hours <= 48)
            {
                elasp_second = 20;
            }
            else if (time_hours <= 72)   //3天
            {
                elasp_second = 25;
            }
            else if (time_hours <= 96)   //4天
            {
                elasp_second = 35;
            }
            else if (time_hours <= 120)   //5天
            {
                elasp_second = 50;
            }
            else if (time_hours <= 168)   //7天
            {
                elasp_second = 60;
            }
            else
            {
                elasp_second = Math.Round(time_hours * 1.1 * 80 / 168);
            }

            return elasp_second;  
        }

        //获取模型信息(包括调度信息)
        public static string[] Get_Model_Info(HydroModel model, string start_simulatetime = "", bool iscalting = false)
        {
            //定义信息数组
            string[] model_info = new string[8];
            model_info[0] = model.Model_Faname;

            model_info[1] = model.Modeldesc;

            model_info[2] = model.ModelGlobalPars.Simulate_time.Begin.AddHours(model.ModelGlobalPars.Ahead_hours).ToString(Model_Const.TIMEFORMAT);
            model_info[3] = model.ModelGlobalPars.Simulate_time.End.ToString(Model_Const.TIMEFORMAT);

            switch (model.Model_state)
            {
                case Model_State.Finished:
                    model_info[4] = "已完成";
                    break;
                case Model_State.Error:
                    model_info[4] = "模型错误";
                    break;
                case Model_State.Ready_Calting:
                    model_info[4] = "待计算";
                    break;
                default:
                    model_info[4] = "计算中";
                    break;
            }

            model_info[5] = start_simulatetime;

            //获取模型调度信息
            model_info[6] = WG_INFO.Get_Model_DdInfo(model);
            return model_info;
        }

        //获取新建模型信息
        public static Dictionary<string, Dictionary<string, string>> Get_NewModel_Info(HydroModel model)
        {
            //获取模型信息
            string[] model_info = WG_INFO.Get_Model_Info(model);

            //与数据库对应的字段，并修改了最后一个以返回给前端
            string[] item = new string[] { "fangan_name", "model_desc", "start_time", "end_time", "model_state", "progress", "zzdd_info" };

            //将模拟开始时间信息 改为进度信息
            Dictionary<string, Dictionary<string, string>> new_modelinfo = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> model_info_dic = new Dictionary<string, string>();
            for (int i = 0; i < item.Length; i++)
            {
                if (i == item.Length - 2)
                {
                    model_info_dic.Add("progress", "0$0");
                    continue;
                }
                model_info_dic.Add(item[i], model_info[i]);
            }
            new_modelinfo.Add(model.Modelname, model_info_dic);
            return new_modelinfo;
        }
        #endregion ************************************************************************


        #region *************************** 水库 河道水情和工情 ************************
        //更新水情初始条件数据库
        public static void Update_LevelTable()
        {
            //请求水库和河道水情(水位、流量)
            List<Water_Info> riverlevel_list = Get_RiverLevel_FromHttpRequest(Mysql_GlobalVar.level_serverurl);

            //改变格式
            Dictionary<string, object[]> initial_level = new Dictionary<string, object[]>();
            for (int i = 0; i < riverlevel_list.Count; i++)
            {
                object[] data = { riverlevel_list[i].tm, riverlevel_list[i].z, riverlevel_list[i].q};
                if(!initial_level.Keys.Contains(riverlevel_list[i].stcd)) initial_level.Add(riverlevel_list[i].stcd, data);
            }
            
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.WATER_CONDITION;

            //更新数据库水位
            for (int i = 0; i < initial_level.Count; i++)
            {
                string time_str = initial_level.ElementAt(i).Value[0].ToString().Replace('-', '/');

                //更新数据
                string stcd = initial_level.ElementAt(i).Key;
                double q = (double)initial_level.ElementAt(i).Value[2];
                string sql = "update " + tableName + " set level = " + (double)initial_level.ElementAt(i).Value[1] +
                    ",discharge = " + (double)initial_level.ElementAt(i).Value[2] + ",date ='" + time_str
                    + "' where stcd = '" + stcd + "'";

                //这里q==-1表示无实测数据，不更新
                if (q == -1 ) sql = "update " + tableName + " set level = " + (double)initial_level.ElementAt(i).Value[1] +
                     ",date ='" + time_str + "' where stcd = '" + stcd + "'";

                Mysql.Execute_Command(sql);
            }

            

            Console.WriteLine("最新水情监测数据更新完成！");
        }

        //更新闸站工情数据库
        public static void Update_GateStateTable()
        {
            //请求闸站工情数据
            //List<Gate_StateInfo> gatestate_list = Get_GateState_FromHttpRequest(Mysql_GlobalVar.gatestate_serverurl);

            //请求闸站工情数据 --根据水库当前下泄流量，得到泄洪洞建筑物最新调度信息(全关、控泄)
            List<Gate_StateInfo> gatestate_list = Get_Now_ResXHD_DDinfo();

            Dictionary<string, object[]> gate_infolist = new Dictionary<string, object[]>();
            for (int i = 0; i < gatestate_list.Count; i++)
            {
                object[] data = { gatestate_list[i].tm, gatestate_list[i].nowState, gatestate_list[i].openN, gatestate_list[i].openHQ };
                gate_infolist.Add(gatestate_list[i].str_name, data);
            }

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.STRUCT_INFO;

            //更新数据库入渠流量或水位
            for (int i = 0; i < gate_infolist.Count; i++)
            {
                string time_str = gate_infolist.ElementAt(i).Value[0].ToString().Replace('-', '/');
                string sql = "update " + tableName + " set now_state = '" + gate_infolist.ElementAt(i).Value[1].ToString() + "',update_time ='" + time_str + "',open_n = "
                    + (int)gate_infolist.ElementAt(i).Value[2] + ",open_h = " + (double)gate_infolist.ElementAt(i).Value[3] + " where str_name = '" + gate_infolist.ElementAt(i).Key + "'";
                Mysql.Execute_Command(sql);
            }

            Console.WriteLine("最新闸站状态数据更新完成！");
        }

        //根据水库当前下泄流量，得到泄洪洞建筑物最新调度信息(全关、控泄)
        private static List<Gate_StateInfo> Get_Now_ResXHD_DDinfo()
        {
            //获取最新水情，并将水库的泄洪流量作为泄洪洞的控泄流量
            List<Water_Info> now_waterinfo = WG_INFO.Read_Now_WaterInfo();
            List<Gate_StateInfo> gatestate_list = new List<Gate_StateInfo>();
            for (int i = 0; i < now_waterinfo.Count; i++)
            {
                Water_Info water_info = now_waterinfo[i];
                Dictionary<string, Struct_BasePars> str_infos = WG_INFO.Get_StrBaseInfo();
                List<string> res_kx = new List<string>();
                for (int j = 0; j < str_infos.Count; j++)
                {
                    Struct_BasePars str = str_infos.ElementAt(j).Value;
                    if (str.cn_name.Contains(water_info.stnm) && water_info.stnm.Contains("水库") && !res_kx.Contains(water_info.stnm) 
                        && (str.str_type == "泄洪洞" || water_info.stnm == "石门水库(辉县)"))    //就石门水库(辉县)采用溢洪道控泄
                    {
                        string now_state = water_info.q == 0 ? "全关" : "控泄";
                        Gate_StateInfo gate_ddinfo = new Gate_StateInfo(str.str_name, now_state, water_info.tm, 1, water_info.q);
                        gatestate_list.Add(gate_ddinfo);
                        res_kx.Add(water_info.stnm);
                    }
                }
            }

            return gatestate_list;
        }

        //从数据库中读取最新闸站状态数据
        public static List<Gate_StateInfo> Read_NowGateState()
        {
            List<Gate_StateInfo> now_gatestate = new List<Gate_StateInfo>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.STRUCT_INFO;

            //先判断数据库中是否存在
            string sqlstr = "select * from " + tableName;
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            for (int i = 0; i < nLen; i++)
            {
                string stcd = dt.Rows[i][2].ToString();
                string type = dt.Rows[i][3].ToString();
                string nowstate = dt.Rows[i][14].ToString();
                string time = dt.Rows[i][15].ToString();
                int openn = int.Parse(dt.Rows[i][16].ToString());
                double openh = double.Parse(dt.Rows[i][17].ToString());

                Gate_StateInfo gate_info = new Gate_StateInfo(stcd, nowstate, time, openn,openh);
                if(type != "无闸门") now_gatestate.Add(gate_info);
            }

            
            return now_gatestate;
        }

        //从数据库中读取最新闸站状态数据(指定业务模型)
        public static List<Gate_StateInfo> Read_NowGateState(string business_code)
        {
            List<Gate_StateInfo> res = new List<Gate_StateInfo>();
            List<Gate_StateInfo> now_gatestate = Read_NowGateState();

            if (WG_INFO.Str_BaseInfo == null) Initial_StrBaseInfo();

            //获取该业务模型的模型实例集合
            Dictionary<string, List<string>> business_instance = HydroModel.Get_BusinessModel_ModelInstance_Relation();
            if (!business_instance.Keys.Contains(business_code)) return null;
            List<string> instances = business_instance[business_code];

            //逐个建筑物判断
            for (int i = 0; i < now_gatestate.Count; i++)
            {
                string str_name = now_gatestate[i].str_name;
                bool contain = false;
                for (int j = 0; j < instances.Count; j++)
                {
                    if(WG_INFO.Str_BaseInfo[str_name].model_instances.Contains(instances[j]))
                    {
                        contain = true;
                        break;
                    }
                }

                if (contain) res.Add(now_gatestate[i]);
            }

            return res;
        }

        //从数据库中读取最新水情数据(指定业务模型)
        public static List<Water_Info> Read_Now_WaterInfo(string business_code = "")
        {
            List<Water_Info> res = new List<Water_Info>();

            //更新水情数据库
            WG_INFO.Update_LevelTable();

            //从数据库获取最新的数据
            List<Water_Condition> now_waterinfo = Hd11.Read_NowWater();

            //若业务模型为空字符串
            if(business_code == "" || business_code == "undefined")
            {
                for (int i = 0; i < now_waterinfo.Count; i++)
                {
                    string type = now_waterinfo[i].Datasource == "水库水文站" ? "0" : "1";
                    Water_Info water_info = new Water_Info(now_waterinfo[i].Discharge, now_waterinfo[i].Stcd,
                        now_waterinfo[i].Update_Date, now_waterinfo[i].Level, type, now_waterinfo[i].Name);
                    res.Add(water_info);
                }
                return res;
            }

            //获取该业务模型的模型实例集合
            Dictionary<string, List<string>> business_instance = HydroModel.Get_BusinessModel_ModelInstance_Relation();
            if (!business_instance.Keys.Contains(business_code)) return null;
            List<string> instances = business_instance[business_code];

            //逐个监测站点判断
            for (int i = 0; i < now_waterinfo.Count; i++)
            {
                string station_stcd = now_waterinfo[i].Stcd;
                bool contain = false;
                for (int j = 0; j < instances.Count; j++)
                {
                    if (now_waterinfo[i].Model_instance.Contains(instances[j]))
                    {
                        contain = true;
                        break;
                    }
                }

                //如果包含
                if (contain)
                {
                    string type = now_waterinfo[i].Datasource == "水库水文站" ? "0" : "1";
                    Water_Info water_info = new Water_Info(now_waterinfo[i].Discharge, now_waterinfo[i].Stcd,
                        now_waterinfo[i].Update_Date, now_waterinfo[i].Level, type, now_waterinfo[i].Name);
                    res.Add(water_info);
                }
            }

            return res;
        }

        //请求河道水情(水位流量)
        public static List<Water_Info> Get_RiverLevel_FromHttpRequest(string http_url)
        {
            List<Water_Info> riverlevel_list = new List<Water_Info>();

            //请求河道水情(水位)
            string river_level = File_Common.Get_HttpReSponse(http_url);

            dynamic jsonObject = JsonConvert.DeserializeObject(river_level);
            JArray jsonArray = JArray.Parse(jsonObject["data"].ToString());
            foreach (JObject obj in jsonArray)
            {
                string obj_per = obj.Property("z").Value.ToString();
                if (obj_per != "")
                {
                    string json_str = obj.ToString();

                    JObject json = JObject.Parse(json_str);
                    if (json["q"] == null || json["q"].ToString() == "")
                    {
                        json["q"] = -1;
                    }

                    Water_Info res = JsonConvert.DeserializeObject<Water_Info>(json.ToString());

                    //双泉水库和马鞍石水库高程系有问题，需要转
                    if (res.stcd == "31006950" && res.z > 150) res.z = res.z - 77.2;
                    if (res.stcd == "31004950" && res.z < 300) res.z = res.z + 263.2;
                    riverlevel_list.Add(res);
                }
            }

            return riverlevel_list;
        }

        //请求闸站工情(闸门开度)
        public static List<Gate_StateInfo> Get_GateState_FromHttpRequest(string http_url)
        {
            List<Gate_StateInfo> res_list = new List<Gate_StateInfo>();

            //请求闸站工情
            string gate_state = File_Common.Get_HttpReSponse(http_url);

            dynamic jsonObject = JsonConvert.DeserializeObject(gate_state);
            JArray jsonArray = JArray.Parse(jsonObject["data"].ToString());
            foreach (JObject obj in jsonArray)
            {
                string nowstate = obj.Property("nowState").Value.ToString();
                if (nowstate != "")
                {
                    Gate_StateInfo gate = new Gate_StateInfo(obj.Property("stcd").Value.ToString(), nowstate,
                        obj.Property("tm").Value.ToString(), obj.Property("openN").Value.ToString(), obj.Property("openH").Value.ToString());
                    res_list.Add(gate);
                }
            }

            return res_list;
        }


        //请求子流域降雨过程
        public static Dictionary<string, Dictionary<DateTime, double>> Get_Catchment_RainGC_FromHttpRequest(string http_url)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_list = new Dictionary<string, Dictionary<DateTime, double>>();

            //请求子流域降雨洪水过程
            string catchment_q = File_Common.Get_HttpReSponse(http_url);

            dynamic jsonObject = JsonConvert.DeserializeObject(catchment_q);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());

            //遍历其属性和值
            foreach (var property in json_obj.Properties())
            {
                string catchment_id = property.Name;
                Dictionary<DateTime, double> q_dic = property.Value.ToObject<Dictionary<DateTime, double>>();

                res_list.Add(catchment_id, q_dic);
            }
            return res_list;
        }

        //请求子流域洪水过程(NAM模型接口)
        public static Dictionary<string, Dictionary<DateTime, double>> Get_Catchment_FloodGC_FromHttpRequest(HydroModel hydromodel, string http_url, string request_pars)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_list = new Dictionary<string, Dictionary<DateTime, double>>();

            //请求子流域降雨洪水过程
            string catchment_q = File_Common.Post_HttpReSponse(http_url, request_pars);

            dynamic jsonObject = JsonConvert.DeserializeObject(catchment_q);
            JObject json_obj = JObject.Parse(jsonObject["discharge"].ToString());

            //遍历其属性和值
            foreach (var property in json_obj.Properties())
            {
                string catchment_id = property.Name;
                Dictionary<DateTime, double> q_dic = property.Value.ToObject<Dictionary<DateTime, double>>();
                res_list.Add(catchment_id, q_dic);
            }
            return res_list;
        }

        //请求子流域洪水过程(小马接口)
        public static Dictionary<string,Dictionary<DateTime,double>> Get_CatchmentQ_FromHttpRequest(HydroModel hydromodel,string http_url,string request_pars = "")
        {
            Dictionary<string,Dictionary<DateTime,double>> res_list = new Dictionary<string, Dictionary<DateTime,double>>();

            //请求子流域降雨洪水过程
            string catchment_q = request_pars == ""? File_Common.Get_HttpReSponse(http_url): File_Common.Post_HttpReSponse(http_url, request_pars);

            dynamic jsonObject = JsonConvert.DeserializeObject(catchment_q);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());

            //遍历其属性和值
            foreach (var property in json_obj.Properties())
            {
                string catchment_id = property.Name;
                Dictionary<DateTime, double> q_dic = property.Value.ToObject<Dictionary<DateTime, double>>();
                if (hydromodel.ModelGlobalPars.Ahead_hours != 0)
                {
                    DateTime starttime = q_dic.First().Key.Subtract(new TimeSpan(hydromodel.ModelGlobalPars.Ahead_hours, 0, 0));
                    q_dic = Dfs0.Dfs0_AddToStart_Usevalue0(q_dic,starttime);
                }
       
                res_list.Add(catchment_id, q_dic);
            }
            return res_list;
        }



        //根据模型方案id和业务模型类型获取需要统计流量的特征断面(用于分洪、溃堤、闸站故障流量统计)
        public static AtReach Get_ReachSection_From_PlanCode(string plan_code)
        {
            AtReach atreach;

            //根据方案id获取业务模型类型
            string business_code = HydroModel.Get_BusinessCode_FromDB(plan_code);
            string model_instance = HydroModel.Get_Model_Instance(plan_code);
            if (model_instance == "") return AtReach.Get_Atreach("",0);

            //不同业务模型的特征断面
            Dictionary<string, AtReach> business_section = new Dictionary<string, AtReach>();
            business_section.Add("embank_break_gq", AtReach.Get_Atreach("SS_BRANCH_GQ_KD_GZDU", 50));
            business_section.Add("embank_break_wh", AtReach.Get_Atreach("SS_BRANCH_WH_KD_GZDU", 50));

            business_section.Add("gate_fault_hhz", AtReach.Get_Atreach("WH", 484));
            business_section.Add("gate_fault_xhk", AtReach.Get_Atreach("QHNZ", 294));
            business_section.Add("gate_fault_dlz", AtReach.Get_Atreach("GQ", 56272));
            business_section.Add("gate_fault_hzz", AtReach.Get_Atreach("GQ", 80857));
            business_section.Add("gate_fault_ytz", AtReach.Get_Atreach("GQ", 93001));

            atreach = business_section.Keys.Contains(business_code)? business_section[business_code]: AtReach.Get_Atreach("", 0);

            return atreach;
        }

 
        #endregion ***************************************************


        #endregion *******************************************************************************
    }



}