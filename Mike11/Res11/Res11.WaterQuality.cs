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
        #region **********************水质结果 **************************
        //查询污染物位置、污染统计信息过程
        public static Dictionary<DateTime, string[]> Get_ADLocationStatistics_Info(HydroModel hydromodel, string reachname,
            Dictionary<DateTime, Dictionary<double, double>> adzdm_data, Dictionary<DateTime, Dictionary<double, double>> ZdmVolume_Data)
        {
            //判断是否有水质
            if (adzdm_data == null || hydromodel.ModelGlobalPars.Select_model != CalculateModel.ad_and_hd)
            {
                return null;
            }

            //定义结果键值对
            Dictionary<DateTime, string[]> ad_result_info = new Dictionary<DateTime, string[]>();

            //无污染物时的值
            int info_count = 19;

            //先初始化
            ReachInfo reachinfo = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            double init_chainage = reachinfo.start_chainage;
            double reach_lastchainage = reachinfo.end_chainage;

            //通过数据判断获取污染物前锋、中心、尾部桩号位置以及最大污染浓度值
            double for_location = init_chainage;
            string for_fskname = "";
            string for_tszname = "";
            string for_jzzname = "";
            double fsk_maxdistance = reachinfo.end_chainage - reachinfo.start_chainage;
            double tsz_maxdistance = reachinfo.end_chainage - reachinfo.start_chainage;
            double jzz_maxdistance = reachinfo.end_chainage - reachinfo.start_chainage;

            //求第一次和相隔10时间步后有污染时的各类桩号、最大浓度
            double first_chainage1, center_chainage1, last_chainage1, max_con1;  //前一个有污染物的时间步值
            double first_chainage2, center_chainage2, last_chainage2, max_con2;  //下一个增加10步的值
            int i1, first_i, i2;
            Get_I1I2(adzdm_data, init_chainage, out i1, out i2, out first_i, out first_chainage1, out center_chainage1, out last_chainage1,
            out max_con1, out first_chainage2, out center_chainage2, out last_chainage2, out max_con2);


            double first_chainage, center_chainage, last_chainage, max_con;
            for (int i = 0; i < adzdm_data.Count; i++)
            {
                DateTime nowtime = adzdm_data.ElementAt(i).Key;
                string[] location_info = new string[info_count];

                //如果该时刻下各断面污染浓度中的最大值小于等于0，则直接continue;
                if (adzdm_data.ElementAt(i).Value.Values.Max() <= 0.0)
                {
                    location_info = Get_Nowrwstr(info_count);
                    ad_result_info.Add(nowtime, location_info);
                    continue;
                }

                //求污染物位置桩号
                //10步，即20分钟计算一次，中间内插，以此尽量消除因断面间距大导致的位置误差
                if (i != first_i && (i - i1) % 10 == 0)
                {
                    first_chainage1 = first_chainage2;
                    center_chainage1 = center_chainage2;
                    last_chainage1 = last_chainage2;
                    max_con1 = max_con2;
                    i1 = i2;

                    i2 += 10;  //下一个距离10步的位置,不用担心最后10步有几步空值导致的插值误差问题，因为最后污染物变化很缓慢
                    if (i2 > adzdm_data.Count - 1) i2 = adzdm_data.Count - 1;
                    if (adzdm_data.ElementAt(i2).Value.Values.Max() > 0.0)
                    {
                        Dictionary<double, double> zdm_datadic = adzdm_data.ElementAt(i2).Value;
                        Get_ADChainage(zdm_datadic, init_chainage, out first_chainage2, out center_chainage2, out last_chainage2, out max_con2);
                    }
                }

                //内插得到值
                first_chainage = Math.Round(first_chainage1 + ((i - i1) % 10) * (first_chainage2 - first_chainage1) / 10);
                center_chainage = Math.Round(center_chainage1 + ((i - i1) % 10) * (center_chainage2 - center_chainage1) / 10);
                last_chainage = Math.Round(last_chainage1 + ((i - i1) % 10) * (last_chainage2 - last_chainage1) / 10);
                max_con = Math.Round(max_con1 + ((i - i1) % 10) * (max_con2 - max_con1) / 10, 3);
                location_info[0] = ReachInfo.Zhstozh(first_chainage);
                location_info[4] = ReachInfo.Zhstozh(center_chainage);
                location_info[2] = ReachInfo.Zhstozh(last_chainage);

                //根据桩号求坐标点、并最终转化为经纬度、经纬度合并字符串
                PointXY first_p = hydromodel.Mike11Pars.ReachList.Get_ReachPointxy(reachname, first_chainage);
                PointXY first_jwd = PointXY.CoordTranfrom(first_p, 4547, 4326, 6);

                location_info[1] = PointXY.Changeto_dms(first_jwd.X) + " ," + PointXY.Changeto_dms(first_jwd.Y);

                PointXY center_p = hydromodel.Mike11Pars.ReachList.Get_ReachPointxy(reachname, center_chainage);
                PointXY center_jwd = PointXY.CoordTranfrom(center_p, 4547, 4326, 6);
                location_info[5] = PointXY.Changeto_dms(center_jwd.X) + " ," + PointXY.Changeto_dms(center_jwd.Y);

                PointXY last_p = hydromodel.Mike11Pars.ReachList.Get_ReachPointxy(reachname, last_chainage);
                PointXY last_jwd = PointXY.CoordTranfrom(last_p, 4547, 4326, 6);
                location_info[3] = PointXY.Changeto_dms(last_jwd.X) + " ," + PointXY.Changeto_dms(last_jwd.Y);

                //求污染物行径速度（保留2位小数）
                double wrw_speed = 0.0;
                if (first_chainage != last_chainage && i != 0)
                {
                    double seconds = adzdm_data.ElementAt(i).Key.Subtract(adzdm_data.ElementAt(i - 1).Key).TotalSeconds;
                    wrw_speed = Math.Round((first_chainage - for_location) / seconds, 2);
                    if (wrw_speed >= 3.0) wrw_speed = 1.0;
                }
                //Console.WriteLine(" 尾部位置:{0}  核心位置:{1}  前锋位置：{2}  最大浓度:{3} 污染速度:{4}", last_chainage, center_chainage, first_chainage, max_con, wrw_speed);

                for_location = first_chainage;
                location_info[6] = wrw_speed.ToString();

                //获取下一个最近的 指定类型 建筑物名称、距离
                //下一个最近的分水口
                string near_fskname;
                double near_fskchainage;
                AtReach first_atreach = AtReach.Get_Atreach(reachname, first_chainage);
                WG_INFO.Get_NextNearStr(hydromodel, first_atreach, GateType.LLZ, out near_fskname, out near_fskchainage);
                string[] near_fskinfo = Get_Nearstr_Info(hydromodel, near_fskname, near_fskchainage, first_chainage, wrw_speed, GateType.LLZ);
                location_info[7] = near_fskinfo[0]; location_info[8] = near_fskinfo[1];

                //如果下一个分水口换了，则到达最大时间刷新
                double fsk_distance = Math.Round(near_fskchainage - first_chainage, 0);
                if (i != 0 && near_fskname != for_fskname)
                {
                    fsk_maxdistance = fsk_distance;
                    for_fskname = near_fskname;
                }

                //下一个最近的退水闸
                string near_tszname;
                double near_tszchainage;
                WG_INFO.Get_NextNearStr(hydromodel, first_atreach, GateType.PBZ, out near_tszname, out near_tszchainage);
                string[] near_tszinfo = Get_Nearstr_Info(hydromodel, near_tszname, near_tszchainage, first_chainage, wrw_speed, GateType.PBZ);
                location_info[9] = near_tszinfo[0]; location_info[10] = near_tszinfo[1];

                //如果下一个退水闸换了，则到达最大时间刷新
                double tszdistance = Math.Round(near_tszchainage - first_chainage, 0);
                if (i != 0 && near_tszname != for_tszname)
                {
                    tsz_maxdistance = tszdistance;
                    for_tszname = near_tszname;
                }

                //下一个最近的节制闸
                string near_jzzname;
                double near_jzzchainage;
                WG_INFO.Get_NextNearStr(hydromodel, first_atreach, GateType.YLZ, out near_jzzname, out near_jzzchainage);
                string[] near_jzzinfo = Get_Nearstr_Info(hydromodel, near_jzzname, near_jzzchainage, first_chainage, wrw_speed, GateType.YLZ);
                location_info[11] = near_jzzinfo[0]; location_info[12] = near_jzzinfo[1];

                //如果下一个节制闸换了，则到达最大时间刷新
                double jzzdistance = Math.Round(near_jzzchainage - first_chainage, 0);
                if (i != 0 && near_jzzname != for_jzzname)
                {
                    jzz_maxdistance = jzzdistance;
                    for_jzzname = near_jzzname;
                }

                //到达完成度(小于1的小数，保留2位数)
                location_info[13] = 1 - fsk_distance / fsk_maxdistance < 0 ? "0.0" : Math.Round(1 - fsk_distance / fsk_maxdistance, 2).ToString();
                location_info[14] = 1 - tszdistance / tsz_maxdistance < 0 ? "0.0" : Math.Round(1 - tszdistance / tsz_maxdistance, 2).ToString();
                location_info[15] = 1 - jzzdistance / jzz_maxdistance < 0 ? "0.0" : Math.Round(1 - jzzdistance / jzz_maxdistance, 2).ToString();

                //污染统计
                //被污染渠长
                location_info[16] = Math.Round((first_chainage - last_chainage) / 1000.0, 2).ToString();

                //被污染水量(万m3)
                Reach_Segment reach_segment = Reach_Segment.Get_ReachSegment(reachname, last_chainage, first_chainage);
                Dictionary<double, double> now_allgridvolume = ZdmVolume_Data[nowtime];

                double sum_value = 0.0; int start_i = 0;
                for (int j = 0; j < now_allgridvolume.Count; j++)
                {
                    double chainage = now_allgridvolume.ElementAt(j).Key;
                    if (chainage < reach_segment.start_chainage) continue;  //提高效率

                    //内插最后一个值
                    if (chainage > reach_segment.end_chainage && j != 0)
                    {
                        double end_value = (reach_segment.end_chainage - now_allgridvolume.ElementAt(j - 1).Key) * now_allgridvolume.ElementAt(j).Value / (chainage - now_allgridvolume.ElementAt(j - 1).Key);
                        sum_value += end_value;
                        break;      //提高效率
                    }

                    start_i++;
                    sum_value += now_allgridvolume.ElementAt(j).Value;

                    //内插第一个值
                    if (start_i == 1 && j != 0)
                    {
                        double start_value = (chainage - reach_segment.start_chainage) * now_allgridvolume.ElementAt(j).Value / (chainage - now_allgridvolume.ElementAt(j - 1).Key);
                        sum_value += start_value;
                    }
                }
                location_info[17] = Math.Round(sum_value / 10000, 2).ToString();

                //最大污染浓度(mg/l)
                location_info[18] = max_con.ToString();

                ad_result_info.Add(nowtime, location_info);
            }

            return ad_result_info;
        }

        //求第一次和相隔10步后有污染时的桩号、最大浓度
        private static void Get_I1I2(Dictionary<DateTime, Dictionary<double, double>> adzdm_data, double init_chainage,
            out int i1, out int i2, out int first_i, out double first_chainage1, out double center_chainage1, out double last_chainage1,
            out double max_con1, out double first_chainage2, out double center_chainage2, out double last_chainage2, out double max_con2)
        {
            i1 = 0;
            first_i = 0;
            i2 = 10;
            for (int i = 0; i < adzdm_data.Count; i++)
            {
                //找到第1个出现污染浓度的时间步
                if (adzdm_data.ElementAt(i).Value.Values.Max() > 0.0)
                {
                    i1 = i;
                    first_i = i;
                    break;
                }
            }
            Get_ADChainage(adzdm_data.ElementAt(i1).Value, init_chainage, out first_chainage1, out center_chainage1, out last_chainage1, out max_con1);

            //求首次末端各桩号、最大浓度值
            i2 = i1 + 10;
            if (adzdm_data.Count > i2)
            {
                Get_ADChainage(adzdm_data.ElementAt(i2).Value, init_chainage, out first_chainage2, out center_chainage2, out last_chainage2, out max_con2);
            }
            else
            {
                Get_ADChainage(adzdm_data.ElementAt(i1).Value, init_chainage, out first_chainage2, out center_chainage2, out last_chainage2, out max_con2);
            }
        }

        //无污染物时的值
        private static string[] Get_Nowrwstr(int info_count)
        {
            //无污染物时的值
            string[] info = new string[info_count];
            for (int i = 0; i < info.Length; i++)
            {
                info[i] = " ";
                if (i >= 13)
                {
                    info[i] = "0.0";
                }
            }
            return info;
        }

        //获取下一个最近的建筑物的2个字符串
        private static string[] Get_Nearstr_Info(HydroModel hydromodel, string near_strname, double near_strchainage, double first_chainage, double wrw_speed, GateType str_type)
        {
            string[] str_info = new string[2];
            string near_fsk_cnname = WG_INFO.Get_StrChinaName(near_strname);
            double fsk_distance = Math.Round(near_strchainage - first_chainage, 0);
            string fsk_string1 = (near_fsk_cnname == null || fsk_distance < 0) ? "" : near_fsk_cnname + " " + fsk_distance + "m";
            string fsk_string2 = (wrw_speed != 0.0 && fsk_distance > 0) ? Math.Round((fsk_distance / wrw_speed) / 3600, 1).ToString() : "";
            str_info[0] = fsk_string1;
            str_info[1] = fsk_string2;

            return str_info;
        }

        //获取某时刻下的污染物位置桩号
        public static void Get_ADChainage(Dictionary<double, double> zdm_data, double init_chainage, out double first_chainage, out double center_chainage, out double end_chainage, out double max_con)
        {
            first_chainage = init_chainage;
            center_chainage = init_chainage;
            end_chainage = init_chainage;

            //获取有效数据子集合
            Dictionary<double, double> subres_dic = Get_AD_SubDic(zdm_data);
            max_con = subres_dic.Count == 0 ? 0 : subres_dic.Values.Max();
            if (subres_dic.Count < 2) return;
            for (int i = 0; i < subres_dic.Count; i++)
            {
                if (subres_dic.ElementAt(i).Value == max_con)
                {
                    center_chainage = subres_dic.ElementAt(i).Key;
                    break;
                }
            }

            //尾部位置(内插得到)
            //用最大值确定斜率
            double xl1 = max_con * 0.3 / (center_chainage - subres_dic.First().Key);  //后段斜率
            double xl2 = max_con * 0.3 / (subres_dic.Last().Key - center_chainage);   //前段斜率

            //尾部位置
            double xz_value1 = subres_dic.ElementAt(1).Value / xl1;
            if (subres_dic.ElementAt(1).Key - xz_value1 < subres_dic.ElementAt(0).Key)
            {
                end_chainage = Math.Round((subres_dic.ElementAt(0).Key + subres_dic.ElementAt(1).Key) / 2);
            }
            else
            {
                end_chainage = Math.Round(subres_dic.ElementAt(1).Key - xz_value1);
            }

            //前锋位置
            double xz_value2 = subres_dic.ElementAt(subres_dic.Count - 2).Value / xl2;
            if (subres_dic.ElementAt(subres_dic.Count - 2).Key + xz_value2 > subres_dic.Last().Key)
            {
                first_chainage = Math.Round((subres_dic.ElementAt(subres_dic.Count - 2).Key + subres_dic.Last().Key) / 2);
            }
            else
            {
                first_chainage = Math.Round(subres_dic.ElementAt(subres_dic.Count - 2).Key + xz_value2);
            }

        }

        //获取有效数据子集合,把负数、前面的去掉
        private static Dictionary<double, double> Get_AD_SubDic(Dictionary<double, double> zdm_data)
        {
            Dictionary<double, double> sub_dicresult = new Dictionary<double, double>();

            //最大浓度值和最大浓度值所在序号
            int max_i = 0;
            double max_con = zdm_data.Values.Max();
            if (max_con <= 0) return sub_dicresult;

            for (int i = zdm_data.Count - 1; i > -1; i--)
            {
                //最大浓度值和最大浓度值所在桩号(中心桩号)
                double con_res = zdm_data.ElementAt(i).Value;
                if (con_res == max_con)
                {
                    max_i = i;
                    break;
                }
            }

            //以最大值为中心，向两侧遍历,获取首尾i
            int i1 = max_i;
            int i2 = max_i;
            while (true)
            {
                if (i1 <= 0) break;
                i1--;

                if (zdm_data.ElementAt(i1).Value <= 0)
                {
                    break;
                }
            }

            //获取尾端位置i
            while (true)
            {
                if (i2 >= zdm_data.Count - 1) break;
                i2++;
                if (zdm_data.ElementAt(i2).Value <= 0)
                {
                    break;
                }
            }

            //加入结果子集合(首尾可能为0，也可能为负)
            for (int i = i1; i < i2 + 1; i++)
            {
                sub_dicresult.Add(zdm_data.ElementAt(i).Key, zdm_data.ElementAt(i).Value);
            }

            return sub_dicresult;
        }
        #endregion ***********************************************************
    }
}
