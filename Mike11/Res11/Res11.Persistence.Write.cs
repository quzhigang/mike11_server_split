using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;
using System.Runtime.Serialization;
using System.IO;
using DHI.Mike1D.CrossSectionModule;
using System.Threading;
using System.Web.Script.Serialization;
using Kdbndp;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using System.Diagnostics;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json.Linq;


namespace bjd_model.Mike11
{
    public partial class Res11
    {
        #region ********************将txt一维结果文件写入数据库********************
        //将一维结果写入数据库
        public static void Write_Mike11res_IntoDB(HydroModel model, List<object> mike11_result, string model_instance)
        {
            if (model == null || mike11_result == null || mike11_result.Count == 0) return;

            string username = Mysql_GlobalVar.now_modeldb_user;
            string password = Mysql_GlobalVar.now_modeldb_password;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;

            try
            {
                //设置唯一约束，保证plan_code和model_instance的唯一性
                string unique_sql = "ALTER TABLE " + tableName + " ADD CONSTRAINT constraint_unique_model_instance_plan_code UNIQUE (model_instance, plan_code)";
                Mysql.Execute_Command(unique_sql);

                //先判断和删除旧的模型结果
                string select_sql = "select model_instance,plan_code from " + tableName + " where plan_code = '" + model.Modelname + "' and model_instance = '" + model_instance + "'";
                DataTable dt = Mysql.query(select_sql);
                if (dt != null)
                {
                    int nLen = dt.Rows.Count;
                    if (nLen != 0)
                    {
                        string del_sql = "delete from " + tableName + " where plan_code = '" + model.Modelname + "' and model_instance = '" + model_instance + "'";
                        Mysql.Execute_Command(del_sql);
                    }
                }

                //序列化对象
                List<byte[]> byte_list = Serial_Objs_ToByte(mike11_result);

                string sql = "insert into " + tableName + " (model_instance,plan_code,reservoir_result,reachsection_result,floodblq_result,swzd_result,qzd_result," +
                    "vzd_result,ddzd_result,qdzd_result,chainage_h,sound_result,reach_risk,result_desc,tdzd_result) values(:model_instance,:plan_code,:reservoir_result,:reachsection_result," +
                    ":floodblq_result,:swzd_result,:qzd_result,:vzd_result,:ddzd_result,:qdzd_result,:chainage_h,:sound_result,:reach_risk,:result_desc,:tdzd_result)";
                KdbndpParameter[] mysqlPara = { new KdbndpParameter(":model_instance", model_instance),
                                                    new KdbndpParameter(":plan_code", model.Modelname),

                                                    new KdbndpParameter(":reservoir_result", byte_list[0]),
                                                    new KdbndpParameter(":reachsection_result", byte_list[1]) ,

                                                    new KdbndpParameter(":floodblq_result", byte_list[2]) ,
                                                    new KdbndpParameter(":swzd_result", byte_list[3]) ,
                                                    new KdbndpParameter(":qzd_result", byte_list[4]) ,
                                                    new KdbndpParameter(":vzd_result", byte_list[5]) ,

                                                    new KdbndpParameter(":ddzd_result", byte_list[6]) ,
                                                    new KdbndpParameter(":qdzd_result", byte_list[7]) ,
                                                    new KdbndpParameter(":chainage_h", byte_list[8]) ,
                                                    new KdbndpParameter(":sound_result", byte_list[9]),
                                                    new KdbndpParameter(":reach_risk", byte_list[10]),
                                                    new KdbndpParameter(":result_desc",byte_list[11]),
                                                    new KdbndpParameter(":tdzd_result",byte_list[12]),
                                                };
                Mysql.Execute_Command(sql, mysqlPara);
            }
            catch (Exception e)
            {
                Console.WriteLine("事务处理出错！" + e.ToString());
            }

            Console.WriteLine("MIKE11 模型结果存储完成！");
        }

        //对水位、流量、流速纵断进一步压缩
        public static List<object> Modify_Mike11_LQV_ZdRes(List<object> mike11_result)
        {
            List<object> mike11_res = new List<object>();
            for (int i = 0; i < mike11_result.Count; i++)
            {
                mike11_res.Add(mike11_result[i]);
            }

            if (mike11_result[3] == null || mike11_result[4] == null || mike11_result[5] == null) return mike11_res;

            //对水位、流量、流速纵断结果进一步压缩，去掉时间
            Dictionary<string, Dictionary<DateTime, List<float>>> sw_zd = mike11_result[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, Dictionary<DateTime, List<float>>> q_zd = mike11_result[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, Dictionary<DateTime, List<float>>> v_zd = mike11_result[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;

            Dictionary<string, List<List<float>>> sw_zd1 = new Dictionary<string, List<List<float>>>();
            Dictionary<string, List<List<float>>> q_zd1 = new Dictionary<string, List<List<float>>>();
            Dictionary<string, List<List<float>>> v_zd1 = new Dictionary<string, List<List<float>>>();
            for (int i = 0; i < sw_zd.Count; i++)
            {
                Dictionary<DateTime, List<float>> sw_res = sw_zd.ElementAt(i).Value;
                Dictionary<DateTime, List<float>> q_res = q_zd.ElementAt(i).Value;
                Dictionary<DateTime, List<float>> v_res = v_zd.ElementAt(i).Value;

                List<List<float>> sw_list = new List<List<float>>();
                List<List<float>> q_list = new List<List<float>>();
                List<List<float>> v_list = new List<List<float>>();
                for (int j = 0; j < sw_res.Count; j++)
                {
                    sw_list.Add(sw_res.ElementAt(j).Value);
                    q_list.Add(q_res.ElementAt(j).Value);
                    v_list.Add(v_res.ElementAt(j).Value);
                }

                sw_zd1.Add(sw_zd.ElementAt(i).Key, sw_list);
                q_zd1.Add(sw_zd.ElementAt(i).Key, q_list);
                v_zd1.Add(sw_zd.ElementAt(i).Key, v_list);
            }

            mike11_res[3] = sw_zd1;
            mike11_res[4] = q_zd1;
            mike11_res[5] = v_zd1;

            return mike11_res;
        }

        //如果是自动预报，通过判断，对超过降雨量阈值的自动预报主要结果进行保存
        public static void Save_History_AutoForcastRes()
        {
            //开辟新线程，对自动预报结果进行保持
            Thread newth = new Thread(Save_AutoForcastRes);
            newth.IsBackground = true;
            newth.Start();
        }

        //对超过降雨量阈值的自动预报主要结果进行保存
        public static void Save_AutoForcastRes()
        {
            Thread.Sleep(2000);

            //请求自动预报面降雨过程
            try
            {
                string request_url = Mysql_GlobalVar.arearain_serverurl + "?planCode=" + Model_Const.AUTO_MODELNAME;
                string request_res = File_Common.Get_HttpReSponse(request_url);
                dynamic jsonObject = JsonConvert.DeserializeObject(request_res);
                JObject json_obj = JObject.Parse(jsonObject["data"].ToString());
                double total_rain = Math.Round(double.Parse(json_obj["sum"].ToString()), 1);

                //总降雨量大于10mm的才保存
                if (total_rain >= Model_Const.AUTOFORECAST_RESSAVE_RAINVALUE)
                {
                    //面雨量过程
                    Dictionary<DateTime, double> rain_gc = new Dictionary<DateTime, double>();
                    List<DateTime> time_list = JsonConvert.DeserializeObject<List<DateTime>>(json_obj["rain"]["t"].ToString());
                    List<double> rain_list = JsonConvert.DeserializeObject<List<double>>(json_obj["rain"]["v"].ToString());
                    for (int i = 0; i < time_list.Count; i++)
                    {
                        rain_gc.Add(time_list[i], rain_list[i]);
                    }
                    string rain_gc_res = File_Common.Serializer_Obj(rain_gc);

                    //重新命名方案ID
                    DateTime starttime = rain_gc.ElementAt(rain_gc.Count - 1).Key.Subtract(new TimeSpan((int)Model_Const.AUTOFORECAST_HOURS, 0, 0));
                    string name = starttime.Year.ToString() + starttime.Month.ToString("00") + starttime.Day.ToString("00") +
                        starttime.Hour.ToString("00") + starttime.Minute.ToString("00") + starttime.Second.ToString("00");
                    string plan_code = Model_Const.AUTO_MODELNAME + "_" + name;

                    //开始时间和结束时间
                    string start_time = SimulateTime.TimetoStr(starttime);
                    string end_time = SimulateTime.TimetoStr(rain_gc.ElementAt(rain_gc.Count - 1).Key);

                    //请求各子流域洪水结果
                    Dictionary<string, Dictionary<DateTime, double>> catchment_q = Load_CatchmentFlood_Res(Model_Const.AUTO_MODELNAME);
                    string catchment_q_res = File_Common.Serializer_Obj(catchment_q);

                    //从数据库获取自动预报的水库、河道、保留区统计结果
                    Dictionary<string, object> res = Res11.Load_Res11_TJ(Model_Const.AUTO_MODELNAME);
                    string reservoir_result = File_Common.Serializer_Obj(res["reservoir_result"]);
                    string reachsection_result = File_Common.Serializer_Obj(res["reachsection_result"]);
                    string floodblq_result = File_Common.Serializer_Obj(res["floodblq_result"]);

                    //从该数据库获取自动预报的GIS线统计结果
                    string gis_result = HydroModel.Get_Mike11GisTJ_Result(Model_Const.AUTO_MODELNAME);

                    //将该场次自动预报结果写入自动预报结果数据库
                    Write_AutoForcastRes_toDB(plan_code, start_time, end_time, total_rain, rain_gc_res, catchment_q_res, reservoir_result,
                        reachsection_result, floodblq_result, gis_result);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //将自动预报结果写入数据库
        public static void Write_AutoForcastRes_toDB(string plan_code, string start_time, string end_time, double total_rain, string rain_gc,
            string catchment_flood, string reservoir_res, string section_res, string blq_res, string gis_tjres)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_HISTORY_AUTOFORCASTRES_TABLENAME;
            

            try
            {
                //设置唯一约束，保证plan_code和model_instance的唯一性
                string unique_sql = "ALTER TABLE " + tableName + " ADD CONSTRAINT constraint_unique_model_instance_plan_code UNIQUE (model_instance, plan_code)";
                Mysql.Execute_Command(unique_sql);

                //先判断和删除旧的模型结果
                string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                DataTable dt = Mysql.query(select_sql);
                int nLen = dt.Rows.Count;
                if (nLen != 0)
                {
                    string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                    Mysql.Execute_Command(del_sql);
                }

                //存入数据库
                string sql = "insert into " + tableName + " (model_instance,plan_code,start_time,end_time,total_rain,rain_gc," +
                    "catchment_flood,reservoir_result,reachsection_result,floodblq_result,hdline_tj_result) values(:model_instance,:plan_code,:start_time,:end_time,:total_rain,:rain_gc," +
                    ":catchment_flood,:reservoir_result,:reachsection_result,:floodblq_result,:hdline_tj_result)";
                KdbndpParameter[] mysqlPara = { new KdbndpParameter(":model_instance", Mysql_GlobalVar.now_instance),
                                                new KdbndpParameter(":plan_code", plan_code),
                                                new KdbndpParameter(":start_time", start_time),
                                                new KdbndpParameter(":end_time", end_time),
                                                new KdbndpParameter(":total_rain", total_rain),
                                                new KdbndpParameter(":rain_gc", rain_gc),
                                                new KdbndpParameter(":catchment_flood", catchment_flood),
                                                new KdbndpParameter(":reservoir_result", reservoir_res),
                                                new KdbndpParameter(":reachsection_result", section_res),
                                                new KdbndpParameter(":floodblq_result", blq_res),
                                                new KdbndpParameter(":hdline_tj_result", gis_tjres)
                                            };
                Mysql.Execute_Command(sql, mysqlPara);
            }
            catch (Exception e)
            {
                Console.WriteLine("事务处理出错！" + e.ToString());
            }

            Console.WriteLine("自动预报结果存储完成！");
        }

        //获取历史自动预报清单
        public static Dictionary<string, string[]> Get_History_AutoForcast_List()
        {
            Dictionary<string, string[]> res = new Dictionary<string, string[]>();

            //定义连接的数据库和表
            string moel_instance = Mysql_GlobalVar.now_instance;
            string tableName = Mysql_GlobalVar.MIKE11_HISTORY_AUTOFORCASTRES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select plan_code,start_time,end_time,total_rain from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //建筑物调度信息
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string plan_code = dt.Rows[i][0].ToString();
                string[] res_list = new string[] { dt.Rows[i][1].ToString(), dt.Rows[i][2].ToString(), dt.Rows[i][3].ToString() };
                res.Add(plan_code, res_list);
            }

            

            return res;
        }

        //删除历史自动预报方案
        public static void Del_History_AutoForcast(string plan_code)
        {
            Dictionary<string, string[]> res = new Dictionary<string, string[]>();

            //定义连接的数据库和表
            string moel_instance = Mysql_GlobalVar.now_instance;
            string tableName = Mysql_GlobalVar.MIKE11_HISTORY_AUTOFORCASTRES_TABLENAME;

            //删除数据库中的模型
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
                Mysql.Execute_Command(del_sql);
            }
        }


        //获取历史自动预报结果
        public static Dictionary<string, object> Get_History_AutoForcast_List(string plan_code)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            //定义连接的数据库和表
            string moel_instance = Mysql_GlobalVar.now_instance;
            string tableName = Mysql_GlobalVar.MIKE11_HISTORY_AUTOFORCASTRES_TABLENAME;

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //model_instance,plan_code,start_time,end_time,total_rain,rain_gc,catchment_flood,reservoir_result,reachsection_result,floodblq_result,hdline_tj_result
            res.Add("plan_code", dt.Rows[0][1].ToString());
            res.Add("start_time", dt.Rows[0][2].ToString());
            res.Add("end_time", dt.Rows[0][3].ToString());
            res.Add("total_rain", dt.Rows[0][4].ToString());

            Dictionary<DateTime, double> rain_gc = JsonConvert.DeserializeObject<Dictionary<DateTime, double>>(dt.Rows[0][5].ToString());
            res.Add("rain_gc", rain_gc);

            Dictionary<string, Dictionary<DateTime, double>> catchment_flood = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, double>>>(dt.Rows[0][6].ToString());
            res.Add("catchment_flood", catchment_flood);

            Dictionary<string, Reservoir_FloodRes> reservoir_result = JsonConvert.DeserializeObject<Dictionary<string, Reservoir_FloodRes>>(dt.Rows[0][7].ToString());
            res.Add("reservoir_result", reservoir_result);

            Dictionary<string, ReachSection_FloodRes> reachsection_result = JsonConvert.DeserializeObject<Dictionary<string, ReachSection_FloodRes>>(dt.Rows[0][8].ToString());
            res.Add("reachsection_result", reachsection_result);

            Dictionary<string, Xzhq_FloodRes> floodblq_result = JsonConvert.DeserializeObject<Dictionary<string, Xzhq_FloodRes>>(dt.Rows[0][9].ToString());
            res.Add("floodblq_result", floodblq_result);

            GeoJson_Line hdline_tj_result = JsonConvert.DeserializeObject<GeoJson_Line>(dt.Rows[0][10].ToString());
            res.Add("hdline_tj_result", hdline_tj_result);

            return res;
        }

        //获取山洪预报结果
        public static List<Dictionary<string, object>> Get_Mountain_Forecast_Flood(string plan_code)
        {
            List<Dictionary<string, object>> sh_res = new List<Dictionary<string, object>>();
            List<SH_INFO> sh_info = Item_Info.Get_Mountain_Flood_BaseInfo();

            //获取山洪涉及的子流域
            List<string> sub_catchment = new List<string>();
            for (int i = 0; i < sh_info.Count; i++)
            {
                string catchment_code = sh_info[i].catchment_code;
                if (!sub_catchment.Contains(catchment_code)) sub_catchment.Add(catchment_code);
            }

            //获取结果
            string bnd_info = HydroModel.Get_Model_BndInfo(plan_code);
            Dictionary<string, object> bnd_dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(bnd_info);
            Dictionary<string, Dictionary<DateTime, double>> bnd_value = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, double>>>(bnd_dic.Last().Value.ToString());

            //获取子流域洪水结果 -- 洪峰出现时间
            Dictionary<string, DateTime> sub_catchment_q = new Dictionary<string, DateTime>();
            for (int i = 0; i < sub_catchment.Count; i++)
            {
                Dictionary<DateTime, double> sub_qlist = bnd_value[sub_catchment[i]];
                DateTime maxKey = sub_qlist.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                sub_catchment_q.Add(sub_catchment[i], maxKey);
            }

            //请求子流域累计降雨
            string request_rain_url = Mysql_GlobalVar.catchmentrain_serverurl;
            request_rain_url += "?planCode=" + plan_code;
            Dictionary<string, Dictionary<DateTime, double>> catchment_rain = Item_Info.Get_Catchment_RainGC_FromHttpRequest(request_rain_url);
            Dictionary<string, double> sub_catchment_rain = new Dictionary<string, double>();
            for (int i = 0; i < sub_catchment.Count; i++)
            {
                Dictionary<DateTime, double> sub_qlist = catchment_rain[sub_catchment[i]];
                double total_rain = sub_qlist.Values.Sum();
                sub_catchment_rain.Add(sub_catchment[i], total_rain);
            }

            //各村庄山洪预报结果
            for (int i = 0; i < sh_info.Count; i++)
            {
                string catchment = sh_info[i].catchment_code;
                double total_rain = sub_catchment_rain[catchment];
                DateTime flood_time = sub_catchment_q[catchment];
                Dictionary<string, object> res = new Dictionary<string, object>();

                res.Add("region_name", sh_info[i].region_name);
                res.Add("village_name", sh_info[i].village_name);
                res.Add("village_x", sh_info[i].village_x);
                res.Add("village_y", sh_info[i].village_y);
                string risk_level = total_rain < 20? "无风险": sh_info[i].risk_level;
                string at_time = total_rain < 5 ? "--" : SimulateTime.TimetoStr(flood_time);
                res.Add("village_flood_time", at_time);
                res.Add("village_risk_level", risk_level);

                sh_res.Add(res);
            }

            return sh_res;
        }

        //序列化对象
        public static List<byte[]> Serial_Objs_ToByte(List<object> mike11_result)
        {
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器

            //遍历各结果
            List<byte[]> byte_list = new List<byte[]>();
            for (int i = 0; i < mike11_result.Count; i++)
            {
                MemoryStream ms = new MemoryStream();
                if (mike11_result[i] != null)
                {
                    binFormat.Serialize(ms, mike11_result[i]);  //将对象序列化
                }
                byte[] buffer = mike11_result[i] == null ? null : ms.ToArray();  //转换成字节数组
                byte_list.Add(buffer);
                ms.Close();
                ms.Dispose();
            }
            return byte_list;
        }

        //写河道过程三维水面geojson文件，并将文件路径结果写入数据库
        public static void Write_GCPolygonFile_toDB(HydroModel model, List<object> mike11_datares,string model_instance)
        {
            if (mike11_datares == null) return;

            //转换格式，获取不同时刻下，各河道水位和桩号对应关系
            Dictionary<DateTime, Dictionary<string, Dictionary<float, float>>> reach_zdres = Change_LevelZD_Format(mike11_datares);
            Dictionary<string, List<float>> zdm_chainage = mike11_datares[8] as Dictionary<string, List<float>>;
            Dictionary<string, List<float>> zdm_td = mike11_datares[12] as Dictionary<string, List<float>>;

            //子蓄滞洪区启用结果和 需要特殊处理的三维面要素 **卫共独有**
            Dictionary<DateTime, Dictionary<string, string>> sub_xzhq_res = mike11_datares.Last() as Dictionary<DateTime, Dictionary<string, string>>;
            Dictionary<int, string> tx_polygonlist = Item_Info.Get_Mike11_TxPolygonFeatures();

            //直接从样本表里读取河道样本三维面
            string field_name = "polygon_sample_result";
            string sample_polygon_json = Res11.Load_Sample_GisRes(field_name, model_instance);
            GeoJson_Polygon sample_polygon = JsonConvert.DeserializeObject<GeoJson_Polygon>(sample_polygon_json);

            //直接从数据库里读取样本面折点和河道断面的对应关系
            List<Dictionary<int, AtReach>> polygonpoint_sectionrelation = Get_SamplePolygonPoint_SectionRelation(model_instance);

            //结果文件夹
            string res_dir = Model_Files.Get_ModelResdir(model.Modelname, model_instance);
            string res_geojson_dir = res_dir + @"\" + Model_Const.GEOJSON_DIRNAME;
            if (!Directory.Exists(res_geojson_dir)) Directory.CreateDirectory(res_geojson_dir);

            //将各时刻的json文件写入结果文件夹
            List<DateTime> time_list = reach_zdres.Keys.ToList();
            Dictionary<string, string> polygon_geojson_res = new Dictionary<string, string>();
            for (int i = 0; i < time_list.Count; i++)
            {
                DateTime time = time_list[i];
                string time_str = SimulateTime.TimetoStr(time);

                //修改样本面折点高程，成为该时刻下的面要素
                GeoJson_Polygon geojson = Res11.Get_NowTime_FloodpolygonRes(reach_zdres[time], sample_polygon, 
                    polygonpoint_sectionrelation,tx_polygonlist,sub_xzhq_res[time], zdm_chainage, zdm_td);

                //序列化面要素
                string geojson_str = File_Common.Serializer_Obj(geojson);

                //将geojson写入json文本
                string jsonfile_name = res_geojson_dir + @"\" + "polygon_" + (i).ToString() + ".json";
                File_Common.Write_FileContent(jsonfile_name, geojson_str);

                //加入集合
                polygon_geojson_res.Add(time_str, jsonfile_name);
            }

            //批量将结果文件路径存入数据库
            if(polygon_geojson_res != null)
            {
                Save_ReachPolygon_GeojonFile_ToDB(model.Modelname, polygon_geojson_res);
            }

            Console.WriteLine("MIKE11 GIS面过程结果存储完成！");
        }

        //写最大淹没水深分布面要素geojson文件，并将文件路径结果写入数据库(调用python进程)
        public static void Write_TJPolygonFile_toDB(HydroModel model, List<object> mike11_datares, string model_instance)
        {
            if (mike11_datares == null) return;

            //转换格式，获取不同时刻下，各河道水位和桩号对应关系
            Dictionary<DateTime, Dictionary<string, Dictionary<float, float>>> reach_zdres = Change_LevelZD_Format(mike11_datares);
            Dictionary<string, List<float>> zdm_chainage = mike11_datares[8] as Dictionary<string, List<float>>;
            Dictionary<string, List<float>> zdm_td = mike11_datares[12] as Dictionary<string, List<float>>;

            //子蓄滞洪区启用结果和 需要特殊处理的三维面要素 **卫共独有**
            Dictionary<DateTime, Dictionary<string, string>> sub_xzhq_alltime_res = mike11_datares.Last() as Dictionary<DateTime, Dictionary<string, string>>;
            Dictionary<string, string> sub_xzhq_res = Xzhq_FloodRes.Get_Xzhq_State(sub_xzhq_alltime_res);
            Dictionary<int, string> tx_polygonlist = Item_Info.Get_Mike11_TxPolygonFeatures();

            //直接从样本表里读取河道样本三维面
            string field_name = "polygon_sample_result";
            string sample_polygon_json = Res11.Load_Sample_GisRes(field_name, model_instance);
            GeoJson_Polygon sample_polygon = JsonConvert.DeserializeObject<GeoJson_Polygon>(sample_polygon_json);

            //直接从数据库里读取样本面折点和河道断面的对应关系
            List<Dictionary<int, AtReach>> polygonpoint_sectionrelation = Get_SamplePolygonPoint_SectionRelation(model_instance);

            //结果文件夹
            string res_dir = Model_Files.Get_ModelResdir(model.Modelname, model_instance);
            if (!Directory.Exists(res_dir)) Directory.CreateDirectory(res_dir);

            //获取最高水位geojson面要素(只统计特殊的那些面要素)
            Dictionary<string, Dictionary<float, float>> reach_maxlevel = Change_LevelZD_Format_TjMax(mike11_datares);
            GeoJson_Polygon geojson = Res11.Get_MaxLevel_FloodPolygonRes(reach_maxlevel, sample_polygon,
                polygonpoint_sectionrelation, tx_polygonlist, sub_xzhq_res, zdm_chainage, zdm_td);

            //有一个特殊面要素就进行淹没分析
            if(geojson.features.Count != 0)
            {
                ArcGis_Polygon arcgis_polygon = ArcGis_Polygon.Change_PolygonGeoJson_ToArcGisJson(geojson);

                //写入json文件,调用python进程，进行最大淹没的处理
                string json_file = res_dir + @"\" + "max_level.json";
                File_Common.Write_FileContent(json_file, File_Common.Serializer_Obj(geojson));
                Get_MaxFlood_GisTJ_GPToDB(model, json_file,model_instance);
            }
        }

        //开辟python进程，进行最大淹没的处理
        public static void Get_MaxFlood_GisTJ_GPToDB(HydroModel model, string json_file,string model_instance)
        {
            //调用python工具进行轻量化处理
            string py_path = Mysql_GlobalVar.GP_MIKE11TJ_PYTHON;
            string db_connect_info = Mysql_GlobalVar.now_modeldb_serip + "," + Mysql_GlobalVar.now_port + "," 
                + Mysql_GlobalVar.now_modeldb_user + "," + Mysql_GlobalVar.now_modeldb_password 
                + "," + Mysql_GlobalVar.NOWITEM_DBNAME + ","+ Mysql_GlobalVar.MIKE11_GISPOLYGONRES_TABLENAME;
            string[] par_strs = new string[] { db_connect_info, model.Modelname,json_file, model_instance, Mysql_GlobalVar.BASE_DEM_FILE};

            if (File.Exists(py_path)) File_Common.RunPythonScript(py_path, par_strs);
        }

        //将Geojson文件路径结果写入数据库中，所有时间一次写入
        public static void Save_ReachPolygon_GeojonFile_ToDB(string plan_code, Dictionary<string, string> model_res)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISPOLYGONRES_TABLENAME;
            
            string model_instance = Mysql_GlobalVar.now_instance;

            //先判断和删除旧的信息
            string select_sql = "select * from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0)
            {
                string del_sql = "delete from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + model_instance + "'";
                Mysql.Execute_Command(del_sql);
            }

            //将模型名、描述、模型对象 存入数据库
            for (int i = 0; i < model_res.Count; i++)
            {
                string time = model_res.ElementAt(i).Key;
                string res_str = model_res.ElementAt(i).Value;

                //插入数据库
                string sql = "insert into " + tableName + " (model_instance,plan_code,time,reach_gispolygon) values(:model_instance,:plan_code,:time,:reach_gispolygon)";
                KdbndpParameter[] mysqlPara = { new KdbndpParameter(":model_instance", model_instance),
                                                    new KdbndpParameter(":plan_code", plan_code),

                                                    new KdbndpParameter(":time",time) ,
                                                    new KdbndpParameter(":reach_gispolygon", res_str)
                                                };
                Mysql.Execute_Command(sql, mysqlPara);
            }

            Console.WriteLine("Mike11 模型{0}河道面GIS过程结果存储完成！", plan_code);
        }


        //转换格式
        private static Dictionary<DateTime, Dictionary<string, Dictionary<float, float>>> Change_LevelZD_Format(List<object> mike11_datares)
        {
            Dictionary<DateTime, Dictionary<string, Dictionary<float, float>>> res = new Dictionary<DateTime, Dictionary<string, Dictionary<float, float>>>();

            //转换格式，时间为键
            Dictionary<string, Dictionary<DateTime, List<float>>> level_zd = mike11_datares[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, List<float>> chainage = mike11_datares[8] as Dictionary<string, List<float>>;

            List<DateTime> time_list = level_zd.First().Value.Keys.ToList();
            for (int i = 0; i < time_list.Count; i++)
            {
                DateTime time = time_list[i];

                //获取该时刻下的河道水位数据
                Dictionary<string, List<float>> reach_zdres = Res11.Get_Res11TimeValue(level_zd, time);

                Dictionary<string, Dictionary<float, float>> reach_res = new Dictionary<string, Dictionary<float, float>>();
                for (int j = 0; j < chainage.Count; j++)
                {
                    string reach_name = chainage.ElementAt(j).Key;
                    List<float> section_chainage = chainage.ElementAt(j).Value;

                    Dictionary<float, float> section_level = new Dictionary<float, float>();
                    for (int k = 0; k < section_chainage.Count; k++)
                    {
                        section_level.Add(section_chainage[k], reach_zdres[reach_name][k]);
                    }
                    reach_res.Add(reach_name, section_level);
                }

                res.Add(time, reach_res);
            }

            return res;
        }

        //转换格式并统计最大值
        private static Dictionary<string, Dictionary<float, float>> Change_LevelZD_Format_TjMax(List<object> mike11_datares)
        {
            Dictionary<string, Dictionary<float, float>> res = new Dictionary<string, Dictionary<float, float>>();

            //转换格式，时间为键
            Dictionary<string, Dictionary<DateTime, List<float>>> level_zd = mike11_datares[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, List<float>> chainage = mike11_datares[8] as Dictionary<string, List<float>>;

            for (int i = 0; i < level_zd.Count; i++)
            {
                string reach_name = level_zd.ElementAt(i).Key;
                Dictionary<DateTime, List<float>> reach_gcres = level_zd[reach_name];
                List<float> reach_chainage = chainage[reach_name];
                List<float> reach_maxlevel = Get_Section_MaxValues(reach_gcres);

                Dictionary<float, float> reach_tjres = new Dictionary<float, float>();
                for (int j = 0; j < reach_chainage.Count; j++)
                {
                    reach_tjres.Add(reach_chainage[j],reach_maxlevel[j]);
                }
                res.Add(reach_name, reach_tjres);
            }

            return res;
        }

        //统计断面最大值
        public static List<float> Get_Section_MaxValues(Dictionary<DateTime, List<float>> reach_gcres)
        {
            // 假设输入字典的所有值的元素数量相同
            if (reach_gcres == null) return null;
            if (reach_gcres.Count == 0) return null;

            int numberOfLists = reach_gcres.Values.First().Count;
            List<float> maxValues = new List<float>(new float[numberOfLists]);
            foreach (var list in reach_gcres.Values)
            {
                for (int i = 0; i < numberOfLists; i++)
                {
                    maxValues[i] = Math.Max(maxValues[i], list[i]);
                }
            }

            return maxValues;
        }
    
        //将一维GIS线和统计GIS线结果写入数据库
        public static void Write_GCGisLine_TJGisLine_toDB(HydroModel model, List<object> mike11_datares,string model_instance)
        {
            if (mike11_datares == null) return;

            //直接从默认模型结果里读取河道样本三维线
            string field_name = "line_sample_result";
            string sample_linejsonstr = Res11.Load_Sample_GisRes(field_name, model_instance);

            //计算统计GIS结果
            string tjgisres_json = Res11.Get_Mike11_TJGisJson(model,mike11_datares, sample_linejsonstr);

            //计算样本线的过程水位、水深、流速、流量结果属性
            List<Dictionary<int, List<double>>> sampleline_alltime_res = Get_SampleLine_Alltime_ResAttrs(mike11_datares);
            string waterlevel_str = File_Common.Serializer_Obj(sampleline_alltime_res[0]);
            string discharge_str = File_Common.Serializer_Obj(sampleline_alltime_res[1]);
            string speed_str = File_Common.Serializer_Obj(sampleline_alltime_res[2]);
            string waterh_str = File_Common.Serializer_Obj(sampleline_alltime_res[3]);

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISLineRES_TABLENAME;

            try
            {
                //设置唯一约束，保证plan_code和model_instance的唯一性
                string unique_sql = "ALTER TABLE " + tableName + " ADD CONSTRAINT constraint_unique_model_instance_plan_code UNIQUE (model_instance, plan_code)";
                Mysql.Execute_Command(unique_sql);

                //先判断和删除旧的模型结果
                string select_sql = "select * from " + tableName + " where plan_code = '" + model.Modelname + "' and model_instance = '" + model_instance + "'";
                DataTable dt = Mysql.query(select_sql);
                int nLen = dt.Rows.Count;
                if (nLen != 0)
                {
                    string del_sql = "delete from " + tableName + " where plan_code = '" + model.Modelname + "' and model_instance = '" + model_instance + "'";
                    Mysql.Execute_Command(del_sql);
                }

                //存入数据库
                BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器

                string sql = "insert into " + tableName + " (model_instance,plan_code,hdline_tj_result,waterlevel,waterh," +
                    "speed,discharge) values(:model_instance,:plan_code,:hdline_tj_result,:waterlevel,:waterh,:speed,:discharge)";
                KdbndpParameter[] mysqlPara = { new KdbndpParameter(":model_instance", model_instance),
                                                new KdbndpParameter(":plan_code", model.Modelname),
                                                new KdbndpParameter(":hdline_tj_result", tjgisres_json),

                                                new KdbndpParameter(":waterlevel", waterlevel_str),
                                                new KdbndpParameter(":waterh", waterh_str),
                                                new KdbndpParameter(":speed",speed_str),
                                                new KdbndpParameter(":discharge", discharge_str)
                                            };
                Mysql.Execute_Command(sql, mysqlPara);
            }
            catch (Exception e)
            {
                Console.WriteLine("事务处理出错！" + e.ToString());
            }

            Console.WriteLine("MIKE11 GIS结果存储完成！");
        }

        //计算一维河道统计GIS结果，获得结果GeoJSON字符串
        public static string Get_Mike11_TJGisJson(HydroModel model, List<object> mike11_result,string hd_sample_gisline_geojson)
        {
            //按时间获取模型MIke11特定结果 -- 某时刻的水位、流量、流速、渠底
            Dictionary<string, Dictionary<DateTime, List<float>>> level_zd = mike11_result[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, Dictionary<DateTime, List<float>>> discharge_zd = mike11_result[4] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, Dictionary<DateTime, List<float>>> speed_zd = mike11_result[5] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, List<float>> dd_zd = mike11_result[6] as Dictionary<string, List<float>>;
            Dictionary<string, List<float>> qd_zd = mike11_result[7] as Dictionary<string, List<float>>;
            Dictionary<string, List<float>> reach_section_chainage = mike11_result[8] as Dictionary<string, List<float>>;
            Dictionary<string, List<float>> td_zd = mike11_result[12] as Dictionary<string, List<float>>;

            //求纵断结果的时间最大值
            Dictionary<string, List<float>> level_max = Get_ZdRes_TimeMaxValue(level_zd);
            Dictionary<string, List<float>> discharge_max = Get_ZdRes_TimeMaxValue(discharge_zd);
            Dictionary<string, List<float>> speed_max = Get_ZdRes_TimeMaxValue(speed_zd);

            //获取各断面的不冲流速
            Dictionary<string, List<float>> reach_bc = Item_Info.Get_Reach_NoDestorySpeed(reach_section_chainage);

            //将各河道的断面属性合并
            List<float> level = Combine_ReachSection_ZdPars(level_max);
            List<float> discharge = Combine_ReachSection_ZdPars(discharge_max);
            List<float> speed = Combine_ReachSection_ZdPars(speed_max);
            List<float> dd = Combine_ReachSection_ZdPars(dd_zd);
            List<float> qd = Combine_ReachSection_ZdPars(qd_zd);
            List<float> td = Combine_ReachSection_ZdPars(td_zd);
            List<float> waterh = Get_WaterH_List(level, qd);
            List<float> bc_speed = Combine_ReachSection_ZdPars(reach_bc);

            //修改样例GIS要素，重新得到该时刻下的水面GIS几何对象
            GeoJson_Line hd_sample_gisline = JsonConvert.DeserializeObject<GeoJson_Line>(hd_sample_gisline_geojson);
            List<Feature_Line> feature_list = hd_sample_gisline.features;
            List<Feature_Line> new_feature_list = new List<Feature_Line>();
            for (int i = 0; i < feature_list.Count - 1; i++)
            {
                Feature_Line feature = feature_list[i];

                //修改线折点Z值
                Geometry_Line geometry = feature.geometry;
                List<List<double>> points = geometry.coordinates;
                for (int j = 0; j < points.Count; j++)
                {
                    points[j][2] = level[i] + 2.0;
                }

                //修改要素属性
                Dictionary<string, object> feature_atts = feature.properties;
                double level_h = Math.Min(Math.Max(dd[i] - level[i], 0), 5);
                feature_atts["Waterlevel"] = Math.Round(level_h, 2);
                feature_atts["Speed"] = Math.Round(Math.Min(Math.Max(speed[i], 0), 5), 2);
                feature_atts["Waterh"] = Math.Round(Math.Max(waterh[i], 0), 2);
                feature_atts["Discharge"] = Math.Round(Math.Max(discharge[i], 0), 2);

                //增加3个属性：冲刷和漫滩、倒灌
                int destory = Math.Abs(speed[i]) > bc_speed[i]? 1 : 0;
                feature_atts.Add("Destory", destory);
                int overflow = level[i] >= td[i]? 1: 0;
                feature_atts.Add("Overflow", overflow);
                int backflow = discharge[i] < 0? 1: 0;
                feature_atts.Add("Backflow", backflow);

                new_feature_list.Add(feature);
            }

            GeoJson_Line gis_tjres_line = new GeoJson_Line(true, hd_sample_gisline.crs, new_feature_list);

            string gis_tjres_geojson = File_Common.Serializer_Obj(gis_tjres_line);
            return gis_tjres_geojson;
        }

        //计算样本线的过程水位、水深、流速、流量结果属性
        public static List<Dictionary<int,List<double>>> Get_SampleLine_Alltime_ResAttrs(List<object> mike11_result)
        {
            List<Dictionary<int, List<double>>> res = new List<Dictionary<int, List<double>>>();

            //按时间获取模型MIke11特定结果 -- 某时刻的水位、流量、流速、渠底
            Dictionary<string, Dictionary<DateTime, List<float>>> level_zd = mike11_result[3] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, Dictionary<DateTime, List<float>>> discharge_zd = mike11_result[4] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, Dictionary<DateTime, List<float>>> speed_zd = mike11_result[5] as Dictionary<string, Dictionary<DateTime, List<float>>>;
            Dictionary<string, List<float>> dd_zd = mike11_result[6] as Dictionary<string, List<float>>;
            Dictionary<string, List<float>> qd_zd = mike11_result[7] as Dictionary<string, List<float>>;

            //遍历全时间段
            Dictionary<int, List<double>> level_res = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> discharge_res = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> speed_res = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> waterh_res = new Dictionary<int, List<double>>();
            List<DateTime> time_list = level_zd.ElementAt(0).Value.Keys.ToList();

            for (int i = 0; i < time_list.Count; i++)
            {
                List<double> time_level = new List<double>();
                Dictionary<string, List<float>> levels = Get_Res11TimeValue(level_zd, time_list[i]);
                Dictionary<string, List<float>> discharges = Get_Res11TimeValue(discharge_zd, time_list[i]);
                Dictionary<string, List<float>> speeds = Get_Res11TimeValue(speed_zd, time_list[i]);

                //将各河道的断面属性合并
                List<float> level = Combine_ReachSection_ZdPars(levels);
                List<float> discharge = Combine_ReachSection_ZdPars(discharges);
                List<float> speed = Combine_ReachSection_ZdPars(speeds);
                List<float> dd = Combine_ReachSection_ZdPars(dd_zd);
                List<float> qd = Combine_ReachSection_ZdPars(qd_zd);
                List<float> waterh = Get_WaterH_List(level, qd);

                //将各个河段的4个属性保留2位小数
                List<double> level1 = new List<double>();
                List<double> discharge1 = new List<double>();
                List<double> speed1 = new List<double>();
                List<double> waterh1 = new List<double>();
                for (int j = 0; j < level.Count; j++)
                {
                    //修改各个要素的属性
                    double level_h = Math.Min(Math.Max(dd[j] - level[j], 0), 5);

                    double waterlevel_seg = Math.Round(level_h, 2);
                    double speed_seg = Math.Round(Math.Min(Math.Max(speed[j], 0), 5), 2);
                    double waterh_seg = Math.Round(Math.Max(waterh[j], 0), 2);
                    double discharge_seg = Math.Round(Math.Max(discharge[j], 0), 2);
                    level1.Add(waterlevel_seg);
                    discharge1.Add(discharge_seg);
                    speed1.Add(speed_seg);
                    waterh1.Add(waterh_seg);
                }
                level_res.Add(i, level1);
                discharge_res.Add(i, discharge1);
                speed_res.Add(i, speed1);
                waterh_res.Add(i, waterh1);
            }

            //全部加入最后的结果
            res.Add(level_res);
            res.Add(discharge_res);
            res.Add(speed_res);
            res.Add(waterh_res);
            return res;
        }
        #endregion *******************************************
    }
}
