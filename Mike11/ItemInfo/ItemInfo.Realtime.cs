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
        #region *************************** 更新和获取水库、河道实时水情和工情 ************************
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
            List<Water_Info> now_waterinfo = Item_Info.Read_Now_WaterInfo();
            List<Gate_StateInfo> gatestate_list = new List<Gate_StateInfo>();
            for (int i = 0; i < now_waterinfo.Count; i++)
            {
                Water_Info water_info = now_waterinfo[i];
                Dictionary<string, Struct_BasePars> str_infos = Item_Info.Get_StrBaseInfo();
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

            if (Item_Info.Str_BaseInfo == null) Initial_StrBaseInfo();

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
                    if(Item_Info.Str_BaseInfo[str_name].model_instances.Contains(instances[j]))
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
            Item_Info.Update_LevelTable();

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

        //请求各流域面雨量过程
        public static Dictionary<string, Dictionary<DateTime, double>> Get_Catchment_RainGC_FromHttpRequest(string http_url)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_list = new Dictionary<string, Dictionary<DateTime, double>>();

            //请求各流域时段雨量
            string catchment_q = File_Common.Get_HttpReSponse(http_url);

            dynamic jsonObject = JsonConvert.DeserializeObject(catchment_q);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());

            //逐个流域解析
            foreach (var property in json_obj.Properties())
            {
                string catchment_id = property.Name;
                Dictionary<DateTime, double> q_dic = property.Value.ToObject<Dictionary<DateTime, double>>();

                res_list.Add(catchment_id, q_dic);
            }
            return res_list;
        }

        //请求各流域洪水过程(NAM 计算)
        public static Dictionary<string, Dictionary<DateTime, double>> Get_Catchment_FloodGC_FromHttpRequest(HydroModel hydromodel, string http_url, string request_pars)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_list = new Dictionary<string, Dictionary<DateTime, double>>();

            //向服务端提交参数并获取结果
            string catchment_q = File_Common.Post_HttpReSponse(http_url, request_pars);

            dynamic jsonObject = JsonConvert.DeserializeObject(catchment_q);
            JObject json_obj = JObject.Parse(jsonObject["discharge"].ToString());

            //逐个流域解析
            foreach (var property in json_obj.Properties())
            {
                string catchment_id = property.Name;
                Dictionary<DateTime, double> q_dic = property.Value.ToObject<Dictionary<DateTime, double>>();
                res_list.Add(catchment_id, q_dic);
            }
            return res_list;
        }

        //请求各流域洪水过程(通用)
        public static Dictionary<string, Dictionary<DateTime, double>> Get_CatchmentQ_FromHttpRequest(HydroModel hydromodel, string http_url, string request_pars = "")
        {
            Dictionary<string, Dictionary<DateTime, double>> res_list = new Dictionary<string, Dictionary<DateTime, double>>();

            //根据是否有请求体选择 GET 或 POST
            string catchment_q = request_pars == "" ? File_Common.Get_HttpReSponse(http_url) : File_Common.Post_HttpReSponse(http_url, request_pars);

            dynamic jsonObject = JsonConvert.DeserializeObject(catchment_q);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());

            //逐个流域解析
            foreach (var property in json_obj.Properties())
            {
                string catchment_id = property.Name;
                Dictionary<DateTime, double> q_dic = property.Value.ToObject<Dictionary<DateTime, double>>();
                if (hydromodel.ModelGlobalPars.Ahead_hours != 0)
                {
                    DateTime starttime = q_dic.First().Key.Subtract(new TimeSpan(hydromodel.ModelGlobalPars.Ahead_hours, 0, 0));
                    q_dic = Dfs0.Dfs0_AddToStart_Usevalue0(q_dic, starttime);
                }

                res_list.Add(catchment_id, q_dic);
            }
            return res_list;
        }
        #endregion ********************************************************
    }
}
