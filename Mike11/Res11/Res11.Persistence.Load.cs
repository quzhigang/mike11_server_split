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
        public static Dictionary<string, object> Load_Res11(string plan_code)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, object> result11 = new Dictionary<string, object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //水库统计结果、河道统计结果、保留区统计结果
            Dictionary<string, Reservoir_FloodRes> res1 = Res11_Deserialize1(res_data[2]);
            //Dictionary<string, Dictionary<string, double>> res1_str = Res11.Get_StrDic(res1); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[2], res1);

            Dictionary<string, ReachSection_FloodRes> res2 = Res11_Deserialize0(res_data[3]);
            //Dictionary<string, Dictionary<string, Dictionary<string, double>>> res2_str = Res11.Get_StrDic(res2); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[3], res2);

            Dictionary<string, Xzhq_FloodRes> res3 = Res11_Deserialize2(res_data[4]);
            //Dictionary<string, Dictionary<string, Dictionary<string, double>>> res3_str = Res11.Get_StrDic(res3); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[4], res3);

            //水位、流量、流速
            Dictionary<string, Dictionary<DateTime, List<float>>> res4 = Res11_Deserialize22(res_data[5]);
            Dictionary<string, Dictionary<string, List<float>>> res4_str = Res11.Get_StrDic(res4); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[5], res4_str);

            Dictionary<string, Dictionary<DateTime, List<float>>> res5 = Res11_Deserialize22(res_data[6]);
            Dictionary<string, Dictionary<string, List<float>>> res5_str = Res11.Get_StrDic(res5); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[6], res4_str);

            Dictionary<string, Dictionary<DateTime, List<float>>> res6 = Res11_Deserialize22(res_data[7]);
            Dictionary<string, Dictionary<string, List<float>>> res6_str = Res11.Get_StrDic(res6); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[7], res4_str);


            //堤顶、渠底、H点桩号纵断
            Dictionary<string, List<float>> res7 = Res11_Deserialize5(res_data[8]);
            result11.Add(field_name[8], res7);

            Dictionary<string, List<float>> res8 = Res11_Deserialize5(res_data[9]);
            result11.Add(field_name[9], res8);

            Dictionary<string, List<float>> res9 = Res11_Deserialize5(res_data[10]);
            result11.Add(field_name[10], res9);

            //计算纵断最大最小值
            Dictionary<string, Dictionary<string, Zdm_MaxMinValue>> maxmin = Cal_MaxMin(res5, res7, res8);
            result11.Add("zd_maxmin", maxmin);

            //语音
            Dictionary<DateTime, string> res10 = Res11_Deserialize4(res_data[11]);
            Dictionary<string, string> res10_str = Res11.Get_StrDic(res10); //转换成可序列化的格式(键为字符串)
            result11.Add(field_name[11], res10_str);

            //结果过程统计数据
            Dictionary<string, List<object[]>> res11 = Res11_Deserialize3(res_data[12]);
            result11.Add(field_name[12], res11);

            //最后把时间序列单独作为数组传给前端
            string[] time_array = res4_str.First().Value.Keys.ToArray();
            result11.Add("time_list", time_array);

            

            sw.Stop();
            Console.WriteLine("获取全部结果耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //获取模型结果数据记录
        private static void Get_Res11Data(DataTable dt, out List<string> field_name, out List<byte[]> res_data, int remove_for = -1)
        {
            field_name = new List<string>();
            res_data = new List<byte[]>();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (remove_for != -1 && i < remove_for) continue;

                field_name.Add(dt.Columns[i].ColumnName);
                byte[] res_byte = new byte[] { };
                if (dt.Rows[0][i].ToString() != "")
                {
                    if (dt.Columns[i].ColumnName != "model_instance" && dt.Columns[i].ColumnName != "plan_code")
                    {
                        res_byte = (byte[])dt.Rows[0][i];
                    }
                }
                res_data.Add(res_byte);
            }
        }

        //获取模型MIke11特定统计结果 -- 水库、河道、保留区、结果综述
        public static Dictionary<string, Object> Load_Res11_TJ(string plan_code)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, Object> result11 = new Dictionary<string, Object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            
            //先判断该模型在数据库中是否存在
            string sqlstr = "select reservoir_result,reachsection_result,floodblq_result,reach_risk,result_desc from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //水库统计结果、河道统计结果、保留区统计结果、风险结果、结果总结
            Dictionary<string, Reservoir_FloodRes> res1 = Res11_Deserialize1(res_data[0]);
            result11.Add(field_name[0], res1);

            Dictionary<string, ReachSection_FloodRes> res2 = Res11_Deserialize0(res_data[1]);
            result11.Add(field_name[1], res2);

            Dictionary<string, Xzhq_FloodRes> res3 = Res11_Deserialize2(res_data[2]);
            result11.Add(field_name[2], res3);

            //获取风险结果
            Dictionary<string, object> res4 = Res11_Deserialize44(res_data[3]);
            result11.Add(field_name[3], res4);

            //获取综述结果
            string business_code = HydroModel.Get_BusinessCode_FromDB(plan_code);
            string res5 = Res11_Deserialize33(res_data[4]);
            Dictionary<string, object> opti_res = new Dictionary<string, object>();
            if (res5 == null || business_code == "")
            {
                //获取各类风险的总结
                res5 = Get_Risk_Decs_FromResult(res1, res2, res3, res4);
                result11.Add(field_name[4], res5);

                //如果有反向推演结果，则添加一个字段
                if (res_data[4].Length > 0)
                {
                    opti_res = Res11_Deserialize44(res_data[4]);
                    result11.Add("opti_res", opti_res);
                }
            }
            else
            {
                result11.Add(field_name[4], res5);
            }

            sw.Stop();
            Console.WriteLine("获取data结果 耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //获取各类风险的总结
        private static string Get_Risk_Decs_FromResult(Dictionary<string, Reservoir_FloodRes> res1, 
            Dictionary<string, ReachSection_FloodRes> res2, Dictionary<string, Xzhq_FloodRes> res3,
            Dictionary<string, object> res4)
        {
            string res = "";

            //水库风险总结
            string res_resultdesc = HydroModel.Get_Res_ResultDesc(res1);

            //河道漫堤风险
            Dictionary<string, List<Reach_MDRisk_Result>> reach_mt_segment = res4["漫堤风险"] as Dictionary<string, List<Reach_MDRisk_Result>>;
            string mdreach = "2、";
            for (int i = 0; i < reach_mt_segment.Count; i++)
            {
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_mt_segment.ElementAt(i).Key);
                mdreach += reach_cnname;
                if (i > 2) break;  //只放3条河道
                if (i != reach_mt_segment.Count - 1) mdreach += "、";
            }
            if (reach_mt_segment.Count > 3) mdreach += $"等{reach_mt_segment.Count}条河道";
            string reach_resultdesc = reach_mt_segment.Count == 0 ? "2、流域各主要河道均不存在漫堤风险" : mdreach + "局部河段存在漫堤风险";

            //蓄滞洪区风险总结
            string fhblq_resultdesc = HydroModel.Get_Fhblq_ResultDesc(res3);

            //出省洪峰流量
            double ycz_outq = res2.Keys.Contains("元村") ? res2["元村"].Max_Qischarge : 0;
            string outq_desc = $"4、下游(元村站)出省洪峰流量{Math.Round(ycz_outq)}m³/s。";

            res = res_resultdesc + "; " + reach_resultdesc + "; " + fhblq_resultdesc + "; " + outq_desc;
            return res;
        }

        //按时间获取模型MIke11特定结果 -- 水库、河道、保留区
        public static Dictionary<string, Object> Load_Res11_GC(string plan_code)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, Object> result11 = new Dictionary<string, Object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select reservoir_result,reachsection_result,floodblq_result from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //水库统计结果
            Dictionary<string, Reservoir_FloodRes> res1 = Res11_Deserialize1(res_data[0]);
            //Dictionary<string, List<double>> res_value = new Dictionary<string, List<double>>();
            //for (int i = 0; i < res1.Count; i++)
            //{
            //    string name = res1.ElementAt(i).Key;
            //    List<double> res = new List<double>();
            //    res.Add(res1.ElementAt(i).Value.Level_Dic[time]);
            //    res.Add(res1.ElementAt(i).Value.Volumn_Dic[time]);
            //    res.Add(res1.ElementAt(i).Value.InQ_Dic[time]);
            //    res.Add(res1.ElementAt(i).Value.OutQ_Dic[time]);
            //    res_value.Add(name, res);
            //}
            result11.Add(field_name[0], res1);

            //河道统计结果
            Dictionary<string, ReachSection_FloodRes> res2 = Res11_Deserialize0(res_data[1]);
            //Dictionary<string, List<double>> reach_value = new Dictionary<string, List<double>>();
            //for (int i = 0; i < res2.Count; i++)
            //{
            //    string name = res2.ElementAt(i).Key;
            //    List<double> section = new List<double>();
            //    section.Add(res2.ElementAt(i).Value.Level_Dic[time]);
            //    section.Add(res2.ElementAt(i).Value.Discharge_Dic[time]);
            //    reach_value.Add(name, section);
            //}
            result11.Add(field_name[1], res2);

            //保留区统计结果
            Dictionary<string, Xzhq_FloodRes> res3 = Res11_Deserialize2(res_data[2]);
            //Dictionary<string, List<double>> blq_value = new Dictionary<string, List<double>>();
            //for (int i = 0; i < res3.Count; i++)
            //{
            //    string name = res3.ElementAt(i).Key;
            //    List<double> blqres = new List<double>();
            //    blqres.Add(res3.ElementAt(i).Value.Discharge_Dic[time]);
            //    blqres.Add(res3.ElementAt(i).Value.Total_Vomumn[time]);
            //    blq_value.Add(name, blqres);
            //}
            result11.Add(field_name[2], res3);

            

            sw.Stop();
            Console.WriteLine("获取水库河道保留区结果 耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //获取模型各子流域产汇流计算结果
        public static Dictionary<string, Dictionary<DateTime, double>> Load_CatchmentFlood_Res(string plan_code)
        {
            Dictionary<string, Dictionary<DateTime, double>> res = new Dictionary<string, Dictionary<DateTime, double>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.CATCHMENT_FLOODRES_TABLENAME;
            string tableName1 = Mysql_GlobalVar.CATCHMENT_BASEINFO;
            

            //从数据库获取子流域清单
            string sqlstr1 = "select code,name from " + tableName1;
            DataTable dt1 = Mysql.query(sqlstr1);
            int nLen1 = dt1.Rows.Count;
            if (nLen1 == 0) return null;

            //子流域清单
            Dictionary<string, string> catchment_list = new Dictionary<string, string>();
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                string catchment_id = dt1.Rows[i][0].ToString();
                string catchment_name = dt1.Rows[i][1].ToString();
                catchment_list.Add(catchment_id, catchment_name);
            }

            //从数据库获取子流域洪水结果
            string sqlstr = "select bsn_code,outflow_json from " + tableName + " where plan_code='" + plan_code + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //整理结果
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string catchment_id = dt.Rows[i][0].ToString();
                string catchment_res = dt.Rows[i][1].ToString();
                Dictionary<DateTime, double> catchment_flood = JsonConvert.DeserializeObject<Dictionary<DateTime, double>>(catchment_res);
                Dictionary<DateTime, double> catchment_flood1 = catchment_flood.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
                string catchment_name = catchment_list.Keys.Contains(catchment_id) ? catchment_list[catchment_id] : "";
                res.Add(catchment_name, catchment_flood1);
            }


            
            return res;
        }


        //按时间获取模型MIke11特定结果 -- 某时刻的水位、流量、流速、渠底
        public static List<Dictionary<string, List<float>>> Load_Res11_Part2(string plan_code, DateTime time)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<Dictionary<string, List<float>>> result11 = new List<Dictionary<string, List<float>>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select swzd_result,qzd_result,vzd_result,ddzd_result,qdzd_result from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //水位、流量、流速
            Dictionary<string, Dictionary<DateTime, List<float>>> level_time = Res11_Deserialize22(res_data[0]);
            Dictionary<string, List<float>> level = Get_Res11TimeValue(level_time, time);
            result11.Add(level);

            Dictionary<string, Dictionary<DateTime, List<float>>> discharge_time = Res11_Deserialize22(res_data[1]);
            Dictionary<string, List<float>> discharge = Get_Res11TimeValue(discharge_time, time);
            result11.Add(discharge);

            Dictionary<string, Dictionary<DateTime, List<float>>> speed_time = Res11_Deserialize22(res_data[2]);
            Dictionary<string, List<float>> speed = Get_Res11TimeValue(speed_time, time);
            result11.Add(speed);

            //堤顶
            Dictionary<string, List<float>> dd = Res11_Deserialize5(res_data[3]);
            result11.Add(dd);

            //渠底
            Dictionary<string, List<float>> qd = Res11_Deserialize5(res_data[4]);
            result11.Add(qd);

            

            sw.Stop();
            Console.WriteLine("获取 某时刻的水位、流量、流速、渠底结果 耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //按时间获取模型MIke11特定结果 -- 获取模型某时刻的断面桩号和水位
        public static Dictionary<string, Dictionary<float, float>> Load_WaterRes11(string plan_code, DateTime time)
        {
            Dictionary<string, Dictionary<float, float>> res = new Dictionary<string, Dictionary<float, float>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select swzd_result,chainage_h from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //水位
            Dictionary<string, Dictionary<DateTime, List<float>>> level_time = Res11_Deserialize22(res_data[0]);
            Dictionary<string, List<float>> level = Get_Res11TimeValue(level_time, time);

            //桩号
            Dictionary<string, List<float>> chainage = Res11_Deserialize5(res_data[1]);
            

            for (int i = 0; i < chainage.Count; i++)
            {
                string reach_name = chainage.ElementAt(i).Key;
                List<float> section_chainage = chainage.ElementAt(i).Value;

                Dictionary<float, float> section_level = new Dictionary<float, float>();
                for (int j = 0; j < section_chainage.Count; j++)
                {
                    section_level.Add(section_chainage[j], level[reach_name][j]);
                }
                res.Add(reach_name, section_level);
            }

            return res;
        }

        //按时间获取模型MIke11特定结果 -- 主要河道 纵剖面图相关，某时刻的水位、流量、流速、渠底、堤顶、桩号
        public static Dictionary<string, object> Load_Res11_Part3(string plan_code, string res_type)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, object> result11 = new Dictionary<string, object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select "+ res_type + ",ddzd_result,qdzd_result,chainage_h from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //水位、流量、流速
            Dictionary<string, Dictionary<DateTime, List<float>>> res1 = Res11_Deserialize22(res_data[0]);
            Dictionary<string, object> res1_zz = Get_MainReach_Res(res1);
            result11.Add(field_name[0], res1_zz);

            //堤顶、渠底、H点桩号纵断
            Dictionary<string, List<float>> res2 = Res11_Deserialize5(res_data[1]);
            Dictionary<string, object> res2_zz = Get_MainReach_Res(res2);
            result11.Add(field_name[1], res2_zz);

            Dictionary<string, List<float>> res3 = Res11_Deserialize5(res_data[2]);
            Dictionary<string, object> res3_zz = Get_MainReach_Res(res3);
            result11.Add(field_name[2], res3_zz);

            Dictionary<string, List<float>> res4 = Res11_Deserialize5(res_data[3]);
            Dictionary<string, object> res4_zz = Get_MainReach_Res(res4);
            result11.Add(field_name[3], res4_zz);

            //计算纵断最大最小值
            Dictionary<string, Dictionary<string, Zdm_MaxMinValue>> maxmin = Cal_MaxMin(res1, res2, res3);
            Dictionary<string, object> maxmin_zz = Get_MainReach_Res(maxmin);
            result11.Add("zd_maxmin", maxmin_zz);

            //纵剖图标记点位置桩号
            Dictionary<string, Dictionary<string, double>> jzz_str = Item_Info.Get_Reachzpm_Lables();
            result11.Add("str_location", jzz_str);

            

            sw.Stop();
            Console.WriteLine("获取 某时刻纵剖面图相关 结果耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //刷选出主要河道
        public static Dictionary<string, object> Get_MainReach_Res(Dictionary<string, List<float>> source_dic)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            List<string> jlh_mainreach = Item_Info.Get_MainReach_Names(Mysql_GlobalVar.now_instance,true);
            for (int i = 0; i < source_dic.Count; i++)
            {
                string reach_name = source_dic.ElementAt(i).Key;
                if (jlh_mainreach.Contains(reach_name))
                {
                    res.Add(reach_name, source_dic.ElementAt(i).Value);
                }
            }

            return res;
        }

        //刷选出主要河道1
        public static Dictionary<string, object> Get_MainReach_Res(Dictionary<string, Dictionary<DateTime, List<float>>> source_dic)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            List<string> jlh_mainreach = Item_Info.Get_MainReach_Names(Mysql_GlobalVar.now_instance,true);
            for (int i = 0; i < source_dic.Count; i++)
            {
                string reach_name = source_dic.ElementAt(i).Key;
                if (jlh_mainreach.Contains(reach_name))
                {
                    res.Add(reach_name, source_dic.ElementAt(i).Value);
                }
            }

            return res;
        }

        //刷选出主要河道
        public static Dictionary<string, object> Get_MainReach_Res(Dictionary<string, Dictionary<string, Zdm_MaxMinValue>> source_dic)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            List<string> jlh_mainreach = Item_Info.Get_MainReach_Names(Mysql_GlobalVar.now_instance,true);
            for (int i = 0; i < source_dic.Count; i++)
            {
                string reach_name = source_dic.ElementAt(i).Key;
                if (jlh_mainreach.Contains(reach_name))
                {
                    res.Add(reach_name, source_dic.ElementAt(i).Value);
                }
            }

            return res;
        }

        //根据时间获取模型MIke11结果 -- 语音
        public static Dictionary<string, object> Load_Res11_Part4(string plan_code, DateTime time)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, object> result11 = new Dictionary<string, object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //语音
            Dictionary<DateTime, string> res10 = Res11_Deserialize4(res_data[11]);
            string sound_filepath = res10.Keys.Contains(time) ? res10[time] : "";
            result11.Add(field_name[11], sound_filepath);

            

            sw.Stop();
            Console.WriteLine("获取 某时刻语音结果耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //获取模型MIke11特定结果 -- 风险结果
        public static Dictionary<string, object> Load_Res11_Part5(string plan_code)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, object> result11 = new Dictionary<string, object>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select * from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", plan_code);
                return null;
            }

            //遍历记录
            List<string> field_name = new List<string>();
            List<byte[]> res_data = new List<byte[]>();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i >= 2)
                {
                    byte[] res_byte = null;
                    if (dt.Rows[0][i].ToString() != "")
                    {
                        res_byte = (byte[])dt.Rows[0][i];
                    }
                    res_data.Add(res_byte);
                }
                else
                {
                    byte[] res_byte = null;
                    res_data.Add(res_byte);
                }

                field_name.Add(dt.Columns[i].ColumnName);
            }


            //河道风险结果
            Dictionary<string, List<object[]>> res11 = Res11_Deserialize3(res_data[12]);
            result11.Add(field_name[12], res11);

            

            sw.Stop();
            Console.WriteLine("获取结果耗费时间{0}", sw.Elapsed.ToString());
            return result11;
        }

        //获取模型MIke11的所有河道断面
        public static Dictionary<string, List<float>> Load_ReachSections(string plan_code)
        {
            Dictionary<string, List<float>> res = new Dictionary<string, List<float>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select chainage_h from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //桩号
            res = Res11_Deserialize5(res_data[0]);
            

            return res;
        }

        //获取单一河道断面的水位流量结果过程(从结果 数据库)
        public static Dictionary<string, Dictionary<DateTime, float>> Load_SectionRes_FromDB(string plan_code, AtReach atreach)
        {
            Dictionary<string, Dictionary<DateTime, float>> res = new Dictionary<string, Dictionary<DateTime, float>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11RES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select swzd_result,qzd_result,chainage_h from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //桩号
            Dictionary<string, List<float>> chainage_res = Res11_Deserialize5(res_data[2]);
            if (!chainage_res.Keys.Contains(atreach.reachname))
            {
                
                return null;
            }

            //水位
            Dictionary<string, Dictionary<DateTime, List<float>>> res4 = Res11_Deserialize22(res_data[0]);
            Dictionary<DateTime, float> res4_zz = ExtractSectionData(res4, chainage_res, atreach);
            res.Add("level", res4_zz);

            //流量
            Dictionary<string, Dictionary<DateTime, List<float>>> res5 = Res11_Deserialize22(res_data[1]);
            Dictionary<DateTime, float> res5_zz = ExtractSectionData(res5, chainage_res, atreach);
            res.Add("discharge", res5_zz);

            
            return res;
        }

        //获取单一河道断面的水位流量结果过程(从结果 文件里)
        public static Dictionary<string, Dictionary<DateTime, double>> Load_SectionRes_FromResFile(string plan_code, AtReach atreach)
        {
            Dictionary<string, Dictionary<DateTime, double>> res = new Dictionary<string, Dictionary<DateTime, double>>();

            //从数据库加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);
           
            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //水位流量
            Dictionary<DateTime, double> level_dic = Res11.Res11reader(plan_code, atreach, mike11_restype.Water_Level, resdata);
            Dictionary<DateTime, double> discharge_dic = Res11.Res11reader(plan_code, atreach, mike11_restype.Discharge, resdata);
            res.Add("level", level_dic);
            res.Add("discharge", discharge_dic);

            //去掉前面增加的小时数
            Remove_Ahead_Res(hydromodel, ref level_dic);
            Remove_Ahead_Res(hydromodel, ref discharge_dic);

            return res;
        }

        //获取多个河道断面的水位流量结果过程(从结果 文件里)
        public static Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_SectionList_Res_FromResFile(string plan_code,
            List<Reach_Sections> section_list)
        {
            //从数据库加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //断面集合
            List<AtReach> atreach_list = new List<AtReach>();
            for (int i = 0; i < section_list.Count; i++)
            {
                Reach_Sections reach_sections = section_list[i];
                string reach_name = reach_sections.reach;
                List<double> chainages = reach_sections.chainages;
                for (int j = 0; j < chainages.Count; j++)
                {
                    AtReach atreach = AtReach.Get_Atreach(reach_name, chainages[j]);
                    atreach_list.Add(atreach);
                }
            }

            //获取多断面结果
            Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> res = Load_SectionRes_FromResFile(hydromodel,
                hydromodel.Modelname, resdata, atreach_list);

            return res;
        }

        //获取多个河道断面的水位流量结果过程(从结果 文件里)
        public static Dictionary<string,Dictionary<string, Dictionary<DateTime, double>>> Load_SectionRes_FromResFile(HydroModel hydromodel,
            string plan_code,ResultData resdata, List<AtReach> atreach_list)
        {
            Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> res = new Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>>();

            //批量获取水位和流量结果
            Dictionary<string, Dictionary<DateTime, double>> level_res = new Dictionary<string, Dictionary<DateTime, double>>();
            Dictionary<string, Dictionary<DateTime, double>> discharge_res = new Dictionary<string, Dictionary<DateTime, double>>();
            for (int i = 0; i < atreach_list.Count; i++)
            {
                string section_info = atreach_list[i].reachname + atreach_list[i].chainage.ToString();
                Dictionary<DateTime, double> level_dic = Res11.Res11reader(plan_code, atreach_list[i], mike11_restype.Water_Level, resdata);
                Dictionary<DateTime, double> discharge_dic = Res11.Res11reader(plan_code, atreach_list[i], mike11_restype.Discharge, resdata);
                level_res.Add(section_info, level_dic);
                discharge_res.Add(section_info, discharge_dic);

                //去掉前面增加的小时数
                Remove_Ahead_Res(hydromodel, ref level_dic);
                Remove_Ahead_Res(hydromodel, ref discharge_dic);
            }

            //水位流量
            res.Add("level", level_res);
            res.Add("discharge", discharge_res);

            return res;
        }

        //获取单一建筑物的过流结果(从结果 文件里)
        public static Dictionary<string, object> Load_SectionRes_FromResFile(string plan_code, string gate_name)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            //从数据库加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);

            //获取建筑物前后河道水位断面
            Controlstr str = hydromodel.Mike11Pars.ControlstrList.GetGate(gate_name);
            AtReach str_atreach = str.Reachinfo;
            SectionList section_list = hydromodel.Mike11Pars.SectionList;
            AtReach last_section = section_list.Get_Last_SectionChainage(str_atreach);
            AtReach next_section = section_list.Get_Next_SectionChainage(str_atreach);

            //水闸底高程和总过流宽度
            double gate_datumn = str.Stratts.sill_level;
            double gate_total_width = str.Stratts.gate_count * str.Stratts.gate_width;

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(hydromodel.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //上下游水位
            Dictionary<DateTime, double> last_level_dic = Res11.Res11reader(plan_code, last_section, mike11_restype.Water_Level, resdata);
            Dictionary<DateTime, double> next_level_dic = Res11.Res11reader(plan_code, next_section, mike11_restype.Water_Level, resdata);
            
            //过闸流量
            Dictionary<DateTime, double> discharge_dic = Res11.Res11reader(plan_code, str_atreach, mike11_restype.Discharge, resdata);

            //去掉前面增加的小时数
            Remove_Ahead_Res(hydromodel, ref last_level_dic);
            Remove_Ahead_Res(hydromodel, ref next_level_dic);
            Remove_Ahead_Res(hydromodel, ref discharge_dic);

            //保留有限位数
            last_level_dic = last_level_dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));
            next_level_dic = next_level_dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));
            discharge_dic = discharge_dic.ToDictionary(pair => pair.Key, pair => Math.Round(pair.Value, 2));

            //闸门水头过程
            Dictionary<DateTime, double> water_head = new Dictionary<DateTime, double>();
            for (int i = 0; i < last_level_dic.Count; i++)
            {
                double head = last_level_dic.ElementAt(i).Value - next_level_dic.ElementAt(i).Value;
                water_head.Add(last_level_dic.ElementAt(i).Key, Math.Round(head,2));
            }

            //闸前后水深及 过闸流速过程
            Dictionary<DateTime, double> up_depth_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> down_depth_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> speed_dic = new Dictionary<DateTime, double>();
            for (int i = 0; i < discharge_dic.Count; i++)
            {
                DateTime time = discharge_dic.ElementAt(i).Key;

                //闸前后水深
                double up_depth = last_level_dic.ElementAt(i).Value - gate_datumn;
                double down_depth = next_level_dic.ElementAt(i).Value - gate_datumn;

                //水闸处水深
                double gate_waterh = (up_depth + down_depth) / 2;

                //平均过闸流速
                double gate_speed = gate_waterh * gate_total_width == 0 ? 0:Math.Max(discharge_dic.ElementAt(i).Value,0) / (gate_waterh * gate_total_width);

                //加入字典
                up_depth_dic.Add(time, Math.Round(Math.Max(up_depth,0),2));
                down_depth_dic.Add(time, Math.Round(Math.Max(down_depth,0),2));
                speed_dic.Add(time, Math.Round(Math.Max(gate_speed,0),2));
            }

            //最大值统计
            double max_discharge = discharge_dic.Values.Max();
            double max_gate_uplevel = last_level_dic.Values.Max();
            double max_water_head = water_head.Values.Max();
            double max_gate_speed = speed_dic.Values.Max();

            res.Add("discharge_gc", discharge_dic);
            res.Add("up_level_gc", last_level_dic);
            res.Add("down_level_gc", next_level_dic);
            res.Add("gate_speed_gc", speed_dic);
            res.Add("up_depth_dic", up_depth_dic);
            res.Add("down_depth_dic", down_depth_dic);

            res.Add("max_discharge", max_discharge);
            res.Add("max_uplevel", max_gate_uplevel);
            res.Add("max_waterhead", max_water_head);
            res.Add("max_speed", max_gate_speed);

            return res;
        }

        public static Dictionary<DateTime, float> ExtractSectionData(Dictionary<string,Dictionary<DateTime,List<float>>> source_res, 
            Dictionary<string, List<float>> chainage_res, AtReach atreach)
        {
            Dictionary<DateTime, float> section_res = new Dictionary<DateTime, float>();
            string reachname = atreach.reachname;
            float chainage = (float)atreach.chainage;

            var reach_data = source_res[reachname];
            List<float> chainages = chainage_res[reachname];

            //桩号所在索引
            int index = 0;
            if (chainages.Contains(chainage))
            {
                index = chainages.IndexOf(chainage);
            }
            else
            {
                index = File_Common.Search_Value(chainages, chainage);
            }

            foreach (var data in reach_data)
            {
                if (data.Value.Count > 0)
                {
                    section_res[data.Key] = data.Value[index];
                }
            }

            return section_res;
        }


        //从时间序列中获取某时刻的值
        public static Dictionary<string, Dictionary<string, double>> Get_Res11TimeValue(Dictionary<string, Dictionary<string, Dictionary<string, double>>> source, string time_str)
        {
            Dictionary<string, Dictionary<string, double>> result = new Dictionary<string, Dictionary<string, double>>();
            for (int i = 0; i < source.Count; i++)
            {
                string reach_name = source.ElementAt(i).Key;
                Dictionary<string, Dictionary<string, double>> data = source.ElementAt(i).Value;
                Dictionary<string, double> dic1 = new Dictionary<string, double>();
                for (int j = 0; j < data.Count; j++)
                {
                    string item = data.ElementAt(j).Key;
                    double value = source[reach_name][item][time_str];
                    dic1.Add(item, value);
                }
                result.Add(reach_name, dic1);
            }

            return result;
        }

        //从时间序列中获取某时刻的值
        public static Dictionary<string, double> Get_Res11TimeValue(Dictionary<string, Dictionary<DateTime, double>> source, DateTime time)
        {
            Dictionary<string, double> result = new Dictionary<string, double>();
            for (int i = 0; i < source.Count; i++)
            {
                string reach_name = source.ElementAt(i).Key;
                result.Add(reach_name, source[reach_name][time]);
            }

            return result;
        }

        //从时间序列中获取某时刻的值
        public static Dictionary<string, List<float>> Get_Res11TimeValue(Dictionary<string, Dictionary<DateTime, List<float>>> source, DateTime time)
        {
            Dictionary<string, List<float>> result = new Dictionary<string, List<float>>();
            for (int i = 0; i < source.Count; i++)
            {
                string reach_name = source.ElementAt(i).Key;
                result.Add(reach_name, source[reach_name][time]);
            }

            return result;
        }

        //从时间序列中获取某时刻的值
        public static Dictionary<string, List<float>> Get_Res11TimeValue(Dictionary<string, Dictionary<string, List<float>>> source, string time_str)
        {
            Dictionary<string, List<float>> result = new Dictionary<string, List<float>>();
            for (int i = 0; i < source.Count; i++)
            {
                string reach_name = source.ElementAt(i).Key;
                result.Add(reach_name, source[reach_name][time_str]);
            }

            return result;
        }


        //计算纵断最大最小值
        private static Dictionary<string, Dictionary<string, Zdm_MaxMinValue>> Cal_MaxMin(Dictionary<string, Dictionary<DateTime, List<float>>> res,
            Dictionary<string, List<float>> dd, Dictionary<string, List<float>> qd)
        {
            Dictionary<string, Dictionary<string, Zdm_MaxMinValue>> maxmin = new Dictionary<string, Dictionary<string, Zdm_MaxMinValue>>();
            for (int i = 0; i < dd.Count; i++)
            {
                string reach_name = dd.ElementAt(i).Key;
                Dictionary<string, Zdm_MaxMinValue> reach_maxmin = new Dictionary<string, Zdm_MaxMinValue>();

                double max_dd = dd[reach_name].Max();
                double min_qd = qd[reach_name].Min();
                Zdm_MaxMinValue level_maxmin = new Zdm_MaxMinValue(Math.Floor(max_dd) + 1, Math.Floor(min_qd) - 1);
                Zdm_MaxMinValue speed_maxmin = new Zdm_MaxMinValue(5.0, 0.0);

                double max_q = res[reach_name].Last().Value.Max();
                Zdm_MaxMinValue discharge_maxmin = new Zdm_MaxMinValue(Math.Floor(max_q) + 1, 0.0);
                reach_maxmin.Add("level", level_maxmin);
                reach_maxmin.Add("speed", speed_maxmin);
                reach_maxmin.Add("discharge", discharge_maxmin);

                maxmin.Add(reach_name, reach_maxmin);
            }

            return maxmin;
        }


    }
}
