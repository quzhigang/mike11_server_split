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
        #region ***************键值对数据轻量化、可序列化类型转换***************
        //转变结果数据格式0，使其更小（应用引用类型）
        public static Dictionary<string, List<float>> Get_FloatDic(Dictionary<string, List<double>> allsource_dic)
        {
            Dictionary<string, List<float>> result_dic = new Dictionary<string, List<float>>();
            for (int i = 0; i < allsource_dic.Count; i++)
            {
                string reach_name = allsource_dic.ElementAt(i).Key;
                List<double> source_dic = allsource_dic.ElementAt(i).Value;

                //桩号集合
                List<float> res_list = new List<float>();
                for (int j = 0; j < source_dic.Count; j++)
                {
                    res_list.Add((float)source_dic[j]);
                }
                result_dic.Add(reach_name, res_list);
            }

            return result_dic;
        }

        //转变结果数据格式00，使其更小（应用引用类型）
        public static Dictionary<string, Dictionary<DateTime, List<float>>> Get_FloatDicAll(Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> source_dic)
        {
            Dictionary<string, Dictionary<DateTime, List<float>>> result_dic = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                string reach_name = source_dic.ElementAt(i).Key;
                Dictionary<DateTime, List<float>> dic = Get_FloatDic(source_dic.ElementAt(i).Value);
                result_dic.Add(reach_name, dic);
            }
            return result_dic;
        }

        //转变结果数据格式1，使其更小（应用引用类型）
        public static Dictionary<DateTime, List<float>> Get_FloatDic(Dictionary<DateTime, Dictionary<double, double>> source_dic)
        {
            //桩号集合
            Dictionary<DateTime, List<float>> result_dic = new Dictionary<DateTime, List<float>>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                Dictionary<double, double> cv_dic = source_dic.ElementAt(i).Value;
                List<float> f_dic = new List<float>();
                for (int j = 0; j < cv_dic.Count; j++)
                {
                    f_dic.Add((float)cv_dic.ElementAt(j).Value);
                }

                result_dic.Add(source_dic.ElementAt(i).Key, f_dic);
            }

            return result_dic;
        }

        //转变结果数据格式2
        public static Dictionary<string, Dictionary<string, double>> Get_StrDic(Dictionary<string, Dictionary<DateTime, double>> source_dic)
        {
            if (source_dic == null) return null;
            Dictionary<string, Dictionary<string, double>> result_dic = new Dictionary<string, Dictionary<string, double>>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                Dictionary<DateTime, double> s_dic = source_dic.ElementAt(i).Value;
                Dictionary<string, double> r_dic = new Dictionary<string, double>();
                for (int j = 0; j < s_dic.Count; j++)
                {
                    string date_str = s_dic.ElementAt(j).Key.ToString(Model_Const.TIMEFORMAT);
                    r_dic.Add(date_str, s_dic.ElementAt(j).Value);
                }

                result_dic.Add(source_dic.ElementAt(i).Key, r_dic);
            }

            return result_dic;
        }

        //转变结果数据格式22
        public static Dictionary<string, Dictionary<string, Dictionary<string, double>>> Get_StrDic(Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> source_dic)
        {
            if (source_dic == null) return null;
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> result_dic = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();

            for (int i = 0; i < source_dic.Count; i++)
            {
                Dictionary<string, Dictionary<DateTime, double>> source_dic1 = source_dic.ElementAt(i).Value;
                Dictionary<string, Dictionary<string, double>> new_source = new Dictionary<string, Dictionary<string, double>>();
                for (int j = 0; j < source_dic1.Count; j++)
                {
                    Dictionary<DateTime, double> s_dic = source_dic1.ElementAt(j).Value;
                    Dictionary<string, double> r_dic = new Dictionary<string, double>();
                    for (int k = 0; k < s_dic.Count; k++)
                    {
                        string date_str = s_dic.ElementAt(k).Key.ToString(Model_Const.TIMEFORMAT);
                        r_dic.Add(date_str, s_dic.ElementAt(k).Value);
                    }

                    new_source.Add(source_dic1.ElementAt(j).Key, r_dic);
                }
                result_dic.Add(source_dic.ElementAt(i).Key, new_source);
            }

            return result_dic;
        }

        //转变结果数据格式222
        public static Dictionary<string, Dictionary<string, List<float>>> Get_StrDic(Dictionary<string, Dictionary<DateTime, List<float>>> source)
        {
            if (source == null) return null;
            Dictionary<string, Dictionary<string, List<float>>> res = new Dictionary<string, Dictionary<string, List<float>>>();

            for (int i = 0; i < source.Count; i++)
            {
                Dictionary<DateTime, List<float>> source_dic = source.ElementAt(i).Value;
                Dictionary<string, List<float>> result_dic = new Dictionary<string, List<float>>();

                for (int j = 0; j < source_dic.Count; j++)
                {
                    string date_str = source_dic.ElementAt(j).Key.ToString(Model_Const.TIMEFORMAT);
                    result_dic.Add(date_str, source_dic.ElementAt(j).Value);
                }
                res.Add(source.ElementAt(i).Key, result_dic);
            }

            return res;
        }

        //转换格式
        public static Dictionary<string, Dictionary<DateTime, double>> Get_DateDic(Dictionary<string, Dictionary<string, double>> source)
        {
            Dictionary<string, Dictionary<DateTime, double>> res = new Dictionary<string, Dictionary<DateTime, double>>();
            for (int i = 0; i < source.Count; i++)
            {
                string name = source.ElementAt(i).Key;
                Dictionary<string, double> dic = source.ElementAt(i).Value;
                Dictionary<DateTime, double> dic1 = new Dictionary<DateTime, double>();
                for (int j = 0; j < dic.Count; j++)
                {
                    DateTime time = SimulateTime.StrToTime(dic.ElementAt(j).Key);
                    dic1.Add(time, dic.ElementAt(j).Value);
                }
                res.Add(name, dic1);
            }

            return res;
        }

        //转变结果数据格式3
        public static Dictionary<string, string[]> Get_StrDic(Dictionary<DateTime, string[]> source_dic)
        {
            if (source_dic == null) return null;
            Dictionary<string, string[]> result_dic = new Dictionary<string, string[]>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                string date_str = source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT);
                result_dic.Add(date_str, source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转变结果数据格式4
        public static Dictionary<string, string> Get_StrDic(Dictionary<DateTime, string> source_dic)
        {
            if (source_dic == null) return null;
            Dictionary<string, string> result_dic = new Dictionary<string, string>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                string date_str = source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT);
                result_dic.Add(date_str, source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转变结果数据格式44
        public static Dictionary<string, GeoJson_Point> Get_StrDic(Dictionary<DateTime, GeoJson_Point> source_dic)
        {
            if (source_dic == null) return null;
            Dictionary<string, GeoJson_Point> result_dic = new Dictionary<string, GeoJson_Point>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                string date_str = source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT);
                result_dic.Add(date_str, source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转变结果数据格式444
        public static Dictionary<string, mike11_res> Get_StrDic(Dictionary<DateTime, mike11_res> source_dic)
        {
            if (source_dic == null) return null;
            Dictionary<string, mike11_res> result_dic = new Dictionary<string, mike11_res>();
            for (int i = 0; i < source_dic.Count; i++)
            {
                string date_str = source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT);
                result_dic.Add(date_str, source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }


        //转变结果数据格式5
        public static Dictionary<string, List<float>> Get_StrDic(Dictionary<DateTime, List<float>> source_dic)
        {
            Dictionary<string, List<float>> result_dic = new Dictionary<string, List<float>>();
            if (source_dic == null) return null;
            for (int i = 0; i < source_dic.Count; i++)
            {
                result_dic.Add(source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT), source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转变结果数据格式6
        public static Dictionary<string, Dictionary<string, List<Gate_State_Res>>> Get_StrDic(Dictionary<DateTime, Dictionary<string, List<Gate_State_Res>>> source_dic)
        {
            Dictionary<string, Dictionary<string, List<Gate_State_Res>>> result_dic = new Dictionary<string, Dictionary<string, List<Gate_State_Res>>>();
            if (source_dic == null) return null;
            for (int i = 0; i < source_dic.Count; i++)
            {
                result_dic.Add(source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT), source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转变结果数据格式7
        public static Dictionary<string, List<PointXY>> Get_StrDic(Dictionary<DateTime, List<PointXY>> source_dic)
        {
            Dictionary<string, List<PointXY>> result_dic = new Dictionary<string, List<PointXY>>();
            if (source_dic == null) return null;
            for (int i = 0; i < source_dic.Count; i++)
            {
                result_dic.Add(source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT), source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转变结果数据格式8
        public static Dictionary<string, Res11AD_GisRes> Get_StrDic(Dictionary<DateTime, Res11AD_GisRes> source_dic)
        {
            Dictionary<string, Res11AD_GisRes> result_dic = new Dictionary<string, Res11AD_GisRes>();
            if (source_dic == null) return null;
            for (int i = 0; i < source_dic.Count; i++)
            {
                result_dic.Add(source_dic.ElementAt(i).Key.ToString(Model_Const.TIMEFORMAT), source_dic.ElementAt(i).Value);
            }

            return result_dic;
        }

        //转换格式
        public static Dictionary<DateTime, Dictionary<string, string>> Get_Dic_Change(Dictionary<string, Dictionary<DateTime, string>> old_dic)
        {
            Dictionary<DateTime, Dictionary<string, string>> res = new Dictionary<DateTime, Dictionary<string, string>>();

            // 获取所有的键
            List<string> outerKeys = old_dic.Keys.ToList();

            // 假设所有内部字典有相同的DateTime键
            List<DateTime> dateTimes = old_dic.ElementAt(0).Value.Keys.ToList();

            // 初始化结果字典
            for (int i = 0; i < dateTimes.Count; i++)
            {
                res[dateTimes[i]] = new Dictionary<string, string>();
            }

            // 填充结果字典
            for (int i = 0; i < outerKeys.Count; i++)
            {
                string outerKey = outerKeys[i];
                Dictionary<DateTime, string> innerDic = old_dic[outerKey];

                for (int j = 0; j < dateTimes.Count; j++)
                {
                    DateTime dateTime = dateTimes[j];
                    if (innerDic.ContainsKey(dateTime))
                    {
                        res[dateTime][outerKey] = innerDic[dateTime];
                    }
                }
            }

            return res;
        }

        #endregion*******************************************************************
    }
}
