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
        #region ***********************水面边界矢量要素结果 ************************
        //获取 分段河道中心线GIS的全部结果(河道分段中心线、 分段平均水位等)
        public static GeoJson_Line Get_ReachLine_GisRes(HydroModel hydromodel, Dictionary<string, Dictionary<double, double>> reach_zdres)
        {
            // 获取各河道分段(按横断面分段)
            Dictionary<string, List<Reach_Segment>> reach_segment = Get_HdReach_segment(reach_zdres);

            //获取各河道 各段中心线控制点坐标集合
            Dictionary<string, List<List<PointXY>>> reach_centerline_plist = Get_Reach_CenterLineSeglist(hydromodel, reach_zdres, reach_segment);

            //生成单个时间的带Z值、带属性的分段河道中心线GeoJson样本
            GeoJson_Line sample_hdpolygon_Gisres = Get_Reach_CenterLine_Gisres(reach_centerline_plist);

            return sample_hdpolygon_Gisres;
        }

        //渠道分段 -- 按横断面
        public static Dictionary<string, List<Reach_Segment>> Get_HdReach_segment(Dictionary<string, Dictionary<double, double>> ZdmHD_Data)
        {
            Dictionary<string, List<Reach_Segment>> Reach_segments = new Dictionary<string, List<Reach_Segment>>();

            //遍历河道
            for (int i = 0; i < ZdmHD_Data.Count; i++)
            {
                string reach_name = ZdmHD_Data.ElementAt(i).Key;

                Dictionary<double, double> Zdm_Data = ZdmHD_Data[reach_name];
                List<Reach_Segment> reach_segment = new List<Reach_Segment>();
                for (int j = 0; j < Zdm_Data.Count - 1; j++)
                {
                    Reach_Segment segment = Reach_Segment.Get_ReachSegment(reach_name, Math.Round(Zdm_Data.ElementAt(j).Key, 1), Math.Round(Zdm_Data.ElementAt(j + 1).Key, 1));
                    reach_segment.Add(segment);
                }
                Reach_segments.Add(reach_name, reach_segment);
            }

            return Reach_segments;
        }

        //获取各河道 各段中心线控制点坐标集合
        private static Dictionary<string, List<List<PointXY>>> Get_Reach_CenterLineSeglist(HydroModel hydromodel, Dictionary<string, Dictionary<double, double>> ZdmHD_Data,
            Dictionary<string, List<Reach_Segment>> reach_segment)
        {
            Dictionary<string, List<List<PointXY>>> reachseg_list = new Dictionary<string, List<List<PointXY>>>();
            for (int i = 0; i < reach_segment.Count; i++)
            {
                string reach_name = reach_segment.ElementAt(i).Key;
                Dictionary<AtReach, List<PointXZS>> reach_sections = hydromodel.Mike11Pars.SectionList.Get_Reach_AllSecDataList(reach_name);

                //河道断面桩号保留1位小数
                List<List<PointXY>> reachseg_centerpoint_list = new List<List<PointXY>>();
                List<Reach_Segment> segments = reach_segment[reach_name];

                for (int j = 0; j < segments.Count; j++)
                {
                    Dictionary<double, PointXY> centerline_pointdic = Get_Grid_Sec_CenterLine(hydromodel, ZdmHD_Data[reach_name], segments[j]);
                    List<PointXY> seg_points = centerline_pointdic.Values.ToList();
                    reachseg_centerpoint_list.Add(seg_points);
                }
                reachseg_list.Add(reach_name, reachseg_centerpoint_list);
            }

            return reachseg_list;
        }

        //获取各河道开始时间水位集合
        public static Dictionary<string, List<double>> Get_Reach_StartDic(Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> ZdmHD_Data)
        {
            Dictionary<string, List<double>> reach_startlevel = new Dictionary<string, List<double>>();
            for (int i = 0; i < ZdmHD_Data.Count; i++)
            {
                string reach_name = ZdmHD_Data.ElementAt(i).Key;
                Dictionary<DateTime, Dictionary<double, double>> time_levels = ZdmHD_Data[reach_name];
                List<double> levels = time_levels.First().Value.Values.ToList();

                reach_startlevel.Add(reach_name, levels);
            }

            return reach_startlevel;
        }

        //获取带Z值、带属性的分段河道中心线GeoJson样本
        private static GeoJson_Line Get_Reach_CenterLine_Gisres(Dictionary<string, List<List<PointXY>>> reach_centerline_plist)
        {
            //定义要素名、是否含Z，以及要素坐标轴
            bool hasz = true;

            //定义几何坐标系
            CRS crs = new CRS();  //默认4326坐标系

            //定义要素对象集合(多段河道中心线，每段一个要素)
            List<Feature_Line> feature_list = new List<Feature_Line>();
            int fid = 0;
            for (int i = 0; i < reach_centerline_plist.Count; i++)
            {
                string reach_name = reach_centerline_plist.ElementAt(i).Key;
                if (reach_name == "WHGBLQ" || reach_name == "WHGFHY") continue;

                List<List<PointXY>> line_list = reach_centerline_plist[reach_name];

                for (int j = 0; j < line_list.Count; j++)
                {
                    //定义要素属性
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties.Add("Waterlevel", 0);              //水位属性
                    properties.Add("Speed", 0);                   //流速属性
                    properties.Add("Waterh", 0);                  //水深属性
                    properties.Add("Discharge", 0);                //流量属性

                    //定义要素几何坐标
                    List<List<double>> coordinates = new List<List<double>>();

                    for (int k = 0; k < line_list[j].Count; k++)
                    {
                        double x = line_list[j][k].X;
                        double y = line_list[j][k].Y;
                        PointXY now_p = PointXY.Get_Pointxy(x, y);

                        PointXY jwd_p = PointXY.CoordTranfrom(now_p, 4547, 4326, 6);
                        List<double> point = new List<double>() { jwd_p.X, jwd_p.Y, 0 };
                        coordinates.Add(point);
                    }
                    Geometry_Line geometry = new Geometry_Line(coordinates);

                    //定义单个要素,并加入要素集合
                    Feature_Line feature_line = new Feature_Line(fid, geometry, properties);
                    feature_list.Add(feature_line);
                    fid++;
                }
            }

            //定义最终的河道中心线要素几何对象
            GeoJson_Line geojson_gisobj = new GeoJson_Line(hasz, crs, feature_list);

            return geojson_gisobj;
        }


        //内插点的坐标
        private static void Insert_Reach_Pointxy(List<ReachPoint> reachpoint_list, double chainage, out PointXY p, out int index)
        {
            if (chainage == reachpoint_list.Last().pointchainage)
            {
                p = PointXY.Get_Pointxy(reachpoint_list.Last().X, reachpoint_list.Last().Y);
                index = reachpoint_list.Count - 1;
                return;
            }

            int res_index = File_Common.Search_Value(reachpoint_list, chainage, true);
            int res_i = res_index != -1 ? res_index : 0;
            int next_i = res_i != reachpoint_list.Count - 1 ? res_i + 1 : res_i;
            double end_point_x = Math.Round(PointXY.Insert_double(reachpoint_list[res_i].pointchainage, reachpoint_list[next_i].pointchainage, reachpoint_list[res_i].X, reachpoint_list[next_i].X, chainage), 3);
            double end_point_y = Math.Round(PointXY.Insert_double(reachpoint_list[res_i].pointchainage, reachpoint_list[next_i].pointchainage, reachpoint_list[res_i].Y, reachpoint_list[next_i].Y, chainage), 3);
            p = PointXY.Get_Pointxy(end_point_x, end_point_y);
            index = res_i;
        }

        //获取横断面、网格点中心线 控制点桩号、坐标键值对集合
        private static Dictionary<double, PointXY> Get_Grid_Sec_CenterLine(HydroModel hydromodel, Dictionary<double, double> ZdmHD_Data, Reach_Segment segment)
        {
            Dictionary<double, PointXY> result_dic = new Dictionary<double, PointXY>();

            //获取该范围内的控制点坐标和桩号
            List<ReachPoint> reachpoint_list = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(segment.reachname).reachpoint_list;

            //内插开始点坐标
            PointXY start_p;
            int start_index;
            Insert_Reach_Pointxy(reachpoint_list, segment.start_chainage, out start_p, out start_index);

            //内插结束点坐标
            PointXY end_p;
            int end_index;
            Insert_Reach_Pointxy(reachpoint_list, segment.end_chainage, out end_p, out end_index);

            //加入集合
            result_dic.Add(segment.start_chainage, start_p);
            if (!result_dic.Keys.Contains(segment.end_chainage)) result_dic.Add(segment.end_chainage, end_p);

            //加入范围内grid点中心线坐标
            for (int i = start_index; i < end_index + 1; i++)
            {
                PointXY p1 = PointXY.Get_Pointxy(reachpoint_list[i].X, reachpoint_list[i].Y);
                if (!result_dic.Keys.Contains(reachpoint_list[i].pointchainage) &&
                    (Math.Round(reachpoint_list[i].pointchainage, 1) > Math.Round(segment.start_chainage, 1) &&
                    Math.Round(reachpoint_list[i].pointchainage, 1) < Math.Round(segment.end_chainage, 1)))
                {
                    result_dic.Add(reachpoint_list[i].pointchainage, p1);
                }
            }

            //加入范围内水位点中心线坐标
            int end_hgrid_index = File_Common.Search_Value(ZdmHD_Data, segment.end_chainage, true);   //获取起点、终点桩号上一个断面序号
            int start_hgrid_index = File_Common.Search_Value(ZdmHD_Data, segment.start_chainage, true);
            for (int i = start_hgrid_index + 1; i < end_hgrid_index + 1; i++)
            {
                PointXY h_grid;
                int h_index;
                Insert_Reach_Pointxy(reachpoint_list, ZdmHD_Data.ElementAt(i).Key, out h_grid, out h_index);
                if (!result_dic.Keys.Contains(ZdmHD_Data.ElementAt(i).Key)) result_dic.Add(ZdmHD_Data.ElementAt(i).Key, h_grid);
            }

            //排序
            Dictionary<double, PointXY> new_result_dic = result_dic.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
            return new_result_dic;
        }

        //获取各断面最大水面宽度
        private static Dictionary<double, double> Get_WaterMaxWidth(HydroModel hydromodel, string reach_name, Dictionary<AtReach, List<PointXZS>> reach_sections,
            Dictionary<double, double> hd_data, List<double> chainage)
        {
            Dictionary<double, double> Water_Width = new Dictionary<double, double>();
            for (int i = 0; i < chainage.Count; i++)
            {
                //推求水位
                double grid_chainage = chainage[i];
                int res_index = File_Common.Search_Value(hd_data, grid_chainage, true);
                int res_i = res_index != -1 ? res_index : 0;

                //搜索的断面是上一个断面，如果该网格点非断面点，则就用上一个断面，否则就用该断面
                if (res_i != hd_data.Count - 1 && hd_data.ElementAt(res_i + 1).Key == grid_chainage)
                {
                    res_i++;
                }

                //推求水面最大宽度(根据断面点推求，最大宽度等于断面宽度)
                AtReach atreach = AtReach.Get_Atreach(reach_name, hd_data.ElementAt(res_i).Key);
                double section_width = hydromodel.Mike11Pars.SectionList.Get_SectionWidth(atreach, reach_sections);

                //加入集合
                Water_Width.Add(grid_chainage, section_width);
            }

            //部分特殊的断面水位点无断面，故宽会等于0,如支流连接断面等，重新根据前后内插
            for (int i = 0; i < Water_Width.Count; i++)
            {
                if (Water_Width.ElementAt(i).Value == 0)
                {
                    //取前面最近的非0值
                    double value = 0;
                    for (int j = i; j > -1; j--)
                    {
                        if (Water_Width.ElementAt(j).Value != 0)
                        {
                            value = Water_Width.ElementAt(j).Value;
                            break;
                        }
                    }

                    //如果还是0，取后面最近的非0值
                    if (value == 0)
                    {
                        for (int j = i; j < Water_Width.Count; j++)
                        {
                            if (Water_Width.ElementAt(j).Value != 0)
                            {
                                value = Water_Width.ElementAt(j).Value;
                                break;
                            }
                        }
                    }

                    Water_Width[Water_Width.ElementAt(i).Key] = value;
                }
            }

            return Water_Width;
        }

        //根据断面点、桩号和水面宽度，推求水面边界线坐标
        private static List<PointXY> Get_HDploy_Pointxy(Dictionary<double, PointXY> centerline_pointdic, Dictionary<double, double> water_width, bool isfirst_seg,
                                                               ref PointXY w_point_l_last, ref PointXY w_point_r_last)
        {
            List<PointXY> ploy_point = new List<PointXY>();

            List<PointXY> l_w_point = new List<PointXY>();
            List<PointXY> r_w_point = new List<PointXY>();

            //重新遍历控制点
            for (int i = 0; i < centerline_pointdic.Count - 1; i++)
            {
                PointXY w_point_l;
                PointXY w_point_r;

                if (i != 0 || isfirst_seg)
                {
                    //水面宽度的一半
                    double lr_width = water_width.ElementAt(i).Value / 2;

                    //控制点
                    PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(i).Value, centerline_pointdic.ElementAt(i + 1).Value, lr_width, lr_width, out w_point_l, out w_point_r);
                }
                else
                {
                    //为上一个断面的2个点
                    w_point_l = w_point_l_last;
                    w_point_r = w_point_r_last;
                }

                l_w_point.Add(w_point_l);
                r_w_point.Add(w_point_r);
            }

            //最后一个断面
            double lr_width_last = water_width.ElementAt(water_width.Count - 1).Value / 2;
            double chainage_last = centerline_pointdic.ElementAt(water_width.Count - 1).Key;
            double last_distance = Math.Abs(centerline_pointdic.ElementAt(water_width.Count - 1).Key - centerline_pointdic.ElementAt(water_width.Count - 2).Key);
            PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(centerline_pointdic.Count - 2).Value, centerline_pointdic.ElementAt(centerline_pointdic.Count - 1).Value, lr_width_last, lr_width_last, out w_point_l_last, out w_point_r_last, last_distance);
            l_w_point.Add(w_point_l_last);
            r_w_point.Add(w_point_r_last);

            //重新按顺序整合控制点
            if (l_w_point != null) ploy_point.AddRange(l_w_point);
            r_w_point.Reverse();
            if (r_w_point != null) ploy_point.AddRange(r_w_point);
            if (l_w_point != null) ploy_point.Add(l_w_point[0]);

            //暂时先用一个固定值代替！
            //List<PointXY> ploy_point1 = new List<PointXY>();
            //for (int i = 0; i < ploy_point.Count; i++)
            //{
            //    double x = Double.IsInfinity(ploy_point[i].X) ? 460639 : ploy_point[i].X;
            //    double y = Double.IsInfinity(ploy_point[i].Y) ? 3840685 : ploy_point[i].Y;
            //    PointXY point = PointXY.Get_Pointxy(x, y);
            //    ploy_point1.Add(point);
            //}

            return ploy_point;
        }



        //**************************运行动态要素点的生成******************************
        //获取运行动态GIS结果
        public static Dictionary<DateTime, GeoJson_Point> Get_DTPoint_GisRes(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> DischargeZdmHD_Data)
        {
            Dictionary<DateTime, GeoJson_Point> dtpoint_gisres = new Dictionary<DateTime, GeoJson_Point>();

            //按一定的间隔获取 运行动态箭头点和其角度
            Dictionary<DateTime, Dictionary<PointXY, int>> run_points = Get_DT_Point(hydromodel, DischargeZdmHD_Data);

            //获取各时刻动态点要素几何对象
            for (int i = 0; i < run_points.Count; i++)
            {
                GeoJson_Point gis_point = Get_Adpoint_Gisres(run_points.ElementAt(i).Value);
                dtpoint_gisres.Add(run_points.ElementAt(i).Key, gis_point);
            }

            return dtpoint_gisres;
        }

        //获取中心线上一定间距的控制点桩号、坐标键值对集合
        private static Dictionary<DateTime, Dictionary<PointXY, int>> Get_DT_Point(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> DischargeZdmHD_Data)
        {
            //各河道流量纵断面按时间合并
            Dictionary<DateTime, Dictionary<string, Dictionary<double, double>>> q_zdm = Combine_ReachRes(DischargeZdmHD_Data);

            //获取有流量的渠段范围，包括干渠和支渠(流量>0.1)
            Dictionary<DateTime, List<Reach_Segment>> runreach_segment = Get_AllTime_RunReach(q_zdm);

            //按一定的间隔获取 运行动态箭头点和其角度
            Dictionary<DateTime, Dictionary<PointXY, int>> run_points = Get_DT_Point(hydromodel, runreach_segment);

            return run_points;
        }

        //将河道结果按时间进行合并
        public static Dictionary<DateTime, Dictionary<string, Dictionary<double, double>>> Combine_ReachRes(Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> ZdmHD_Data)
        {
            Dictionary<DateTime, Dictionary<string, Dictionary<double, double>>> res = new Dictionary<DateTime, Dictionary<string, Dictionary<double, double>>>();
            List<DateTime> time_list = ZdmHD_Data.First().Value.Keys.ToList();

            //遍历时间
            for (int i = 0; i < time_list.Count; i++)
            {
                DateTime time = time_list[i];
                Dictionary<string, Dictionary<double, double>> reach_res = new Dictionary<string, Dictionary<double, double>>();

                //遍历河道
                for (int j = 0; j < ZdmHD_Data.Count; j++)
                {
                    string reach_name = ZdmHD_Data.ElementAt(j).Key;
                    Dictionary<DateTime, Dictionary<double, double>> reach_timeres = ZdmHD_Data.ElementAt(j).Value;
                    reach_res.Add(reach_name, reach_timeres[time]);
                }
                res.Add(time, reach_res);
            }

            return res;
        }

        //获取有流量的渠段范围，包括干渠和支渠(流量>0.1)
        private static Dictionary<DateTime, List<Reach_Segment>> Get_AllTime_RunReach(Dictionary<DateTime, Dictionary<string, Dictionary<double, double>>> q_zdm)
        {
            Dictionary<DateTime, List<Reach_Segment>> runreach_segment = new Dictionary<DateTime, List<Reach_Segment>>();

            //遍历时刻,获取各时刻下 有流量的河段范围
            for (int i = 0; i < q_zdm.Count; i++)
            {
                DateTime time = q_zdm.ElementAt(i).Key;
                List<Reach_Segment> run_reachsegs = new List<Reach_Segment>();

                //遍历河道
                Dictionary<string, Dictionary<double, double>> reach_q_zd = q_zdm.ElementAt(i).Value;
                for (int j = 0; j < reach_q_zd.Count; j++)
                {
                    string reach_name = reach_q_zd.ElementAt(j).Key;
                    Dictionary<double, double> chainage_q = reach_q_zd.ElementAt(j).Value;

                    //获取该河道正在流动的渠段
                    Reach_Segment run_reachseg = Get_Reach_RunSegment(reach_name, chainage_q);
                    run_reachsegs.Add(run_reachseg);
                }
                runreach_segment.Add(time, run_reachsegs);
            }

            return runreach_segment;
        }

        //获取某河道正在流动的渠段(流量>0.1)
        private static Reach_Segment Get_Reach_RunSegment(string reach_name, Dictionary<double, double> chainage_q)
        {
            //开始桩号默认为河道起点桩号，结束桩号从前往后遍历，知道最后一个流量>0.1的断面桩号
            double start_chainage = chainage_q.First().Key;
            double end_chainage = chainage_q.First().Key;
            for (int i = 0; i < chainage_q.Count; i++)
            {
                if (chainage_q.ElementAt(i).Value > 0.1) end_chainage = chainage_q.ElementAt(i).Key;
            }

            Reach_Segment run_reachseg = Reach_Segment.Get_ReachSegment(reach_name, start_chainage, end_chainage);

            return run_reachseg;
        }

        //按一定的间隔获取 运行动态箭头点
        private static Dictionary<DateTime, Dictionary<PointXY, int>> Get_DT_Point(HydroModel hydromodel, Dictionary<DateTime, List<Reach_Segment>> runreach_segment)
        {
            Dictionary<DateTime, Dictionary<PointXY, int>> run_points = new Dictionary<DateTime, Dictionary<PointXY, int>>();
            for (int i = 0; i < runreach_segment.Count; i++)
            {
                DateTime time = runreach_segment.ElementAt(i).Key;
                List<Reach_Segment> run_reachseg = runreach_segment.ElementAt(i).Value;
                Dictionary<PointXY, int> run_pointxyz = new Dictionary<PointXY, int>();

                //遍历各河段
                for (int j = 0; j < run_reachseg.Count; j++)
                {
                    string reach_name = run_reachseg[j].reachname;
                    double start_chainage = run_reachseg[j].start_chainage;
                    double end_chainage = run_reachseg[j].end_chainage;

                    //获取不同时刻下运动点的桩号
                    List<double> point_chainages = Get_RunPoint_Chainage(start_chainage, end_chainage, i);

                    //内插这些运动点的坐标，计算其角度
                    Insert_RunPointXYZ_Angle(hydromodel, reach_name, point_chainages, ref run_pointxyz);
                }
                run_points.Add(time, run_pointxyz);
            }

            return run_points;
        }

        //获取不同时刻下运动点的桩号
        private static List<double> Get_RunPoint_Chainage(double start_chainage, double end_chainage, int time_i)
        {
            List<double> res = new List<double>();

            //运行动态点间距
            double distance_step = 1200;

            //偶数序号时间步则加起点
            if (time_i % 2 == 0) res.Add(start_chainage);

            //加上中间的点，偶数序号时间步从1步长开始加，奇数序号时间步从0.5步长开始加
            if (end_chainage - start_chainage > distance_step)
            {
                int steps = (int)((end_chainage - start_chainage) / distance_step);
                for (int i = 0; i < steps; i++)
                {
                    double chainage = time_i % 2 == 0 ? start_chainage + distance_step * (i + 1) : start_chainage + distance_step * (i + 0.5);
                    res.Add(chainage);
                }
            }

            //偶数序号时间步则加终点
            if (start_chainage != end_chainage)
            {
                if (!res.Contains(end_chainage) && time_i % 2 == 0) res.Add(end_chainage);
            }

            return res;
        }

        //内插这些运动点的坐标，计算其角度
        private static void Insert_RunPointXYZ_Angle(HydroModel hydromodel, string reach_name, List<double> point_chainages, ref Dictionary<PointXY, int> run_pointxyz)
        {
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            for (int i = 0; i < point_chainages.Count; i++)
            {
                //根据桩号内插点的XYZ坐标
                int angle = 0;
                PointXY point = reachlist.Get_ReachPointxy(reach_name, point_chainages[i], out angle);
                if (!run_pointxyz.Keys.Contains(point)) run_pointxyz.Add(point, angle);
            }
        }

        //获取点要素几何对象
        private static GeoJson_Point Get_Adpoint_Gisres(Dictionary<PointXY, int> points)
        {
            //定义要素名、是否含Z，以及要素坐标轴
            bool hasz = true;

            //定义坐标系
            CRS crs = new CRS();  //默认4326

            //定义要素对象集合
            List<Feature_Point> features = new List<Feature_Point>();

            for (int i = 0; i < points.Count; i++)
            {
                //定义要素属性
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties.Add("Angle", points.ElementAt(i).Value);

                //定义要素几何(就一个点)
                PointXY now_p = points.ElementAt(i).Key;
                PointXY jwd_p = PointXY.CoordTranfrom(now_p, 4547, 4326, 6);
                Geometry_Point geometry = new Geometry_Point(jwd_p.X, jwd_p.Y);

                //定义单个要素,并加入要素集合
                Feature_Point feature_point = new Feature_Point(i, geometry, properties);

                if (jwd_p.X > 100 && jwd_p.X < 200) features.Add(feature_point);
            }

            //定义点要素几何对象
            GeoJson_Point gis_point = new GeoJson_Point(hasz, crs, features);

            return gis_point;
        }

        //**************************运行动态要素点的生成******************************



        //根据桩号内插水面宽度
        private static Dictionary<double, double> Get_WaterWidth(HydroModel hydromodel, Dictionary<double, double> hd_data, List<double> chainage, out double max_level)
        {
            Dictionary<double, double> Water_Width = new Dictionary<double, double>();
            max_level = 0;
            for (int i = 0; i < chainage.Count; i++)
            {
                //推求水位
                double grid_chainage = chainage[i];
                int res_index = File_Common.Search_Value(hd_data, grid_chainage, true);
                int res_i = res_index != -1 ? res_index : 0;
                int next_i = res_i != hd_data.Count - 1 ? res_i + 1 : res_i;
                double water_level = PointXY.Insert_double(hd_data.ElementAt(res_i).Key, hd_data.ElementAt(next_i).Key, hd_data.ElementAt(res_i).Value, hd_data.ElementAt(next_i).Value, grid_chainage);

                //推求水面宽度(根据断面点推求)
                AtReach atreach = AtReach.Get_Atreach("ZGQ", hd_data.ElementAt(res_i).Key);
                double water_width = hydromodel.Mike11Pars.SectionList.Get_WaterWidth(atreach, water_level);

                //加入集合
                Water_Width.Add(grid_chainage, water_width);

                if (water_level > max_level) max_level = water_level;
            }

            //最大水面高程
            max_level = Math.Round(max_level, 3) + 0.3;

            //部分特殊的断面水位宽会等于0,如支流连接断面等，重新根据前后内插
            for (int i = 0; i < Water_Width.Count; i++)
            {
                if (Water_Width.ElementAt(i).Value == 0)
                {
                    //取前面最近的非0值
                    double value = 0;
                    for (int j = i; j > -1; j--)
                    {
                        if (Water_Width.ElementAt(j).Value != 0)
                        {
                            value = Water_Width.ElementAt(j).Value;
                            break;
                        }
                    }

                    //如果还是0，取后面最近的非0值
                    if (value == 0)
                    {
                        for (int j = i; j < Water_Width.Count; j++)
                        {
                            if (Water_Width.ElementAt(j).Value != 0)
                            {
                                value = Water_Width.ElementAt(j).Value;
                                break;
                            }
                        }
                    }

                    Water_Width[Water_Width.ElementAt(i).Key] = value;
                }
            }

            return Water_Width;
        }

        //根据断面点、桩号和水面宽度，推求水质边界线坐标(泄漏期以后的燕尾形)
        private static List<PointXY> Get_ADploy_Pointxy1(Dictionary<double, PointXY> centerline_pointdic, Dictionary<double, double> water_width, double start1_chainage, double end1_chainage, double ad_wk)
        {
            List<PointXY> adploy_point = new List<PointXY>();

            List<PointXY> l_w_point = new List<PointXY>();
            List<PointXY> r_w_point = new List<PointXY>();
            List<PointXY> l_n_point = new List<PointXY>();
            List<PointXY> r_n_point = new List<PointXY>();

            //重新遍历控制点
            for (int i = 0; i < centerline_pointdic.Count - 1; i++)
            {
                //外线宽(水面宽度的一半)
                double LRwater_width = water_width.ElementAt(i).Value * ad_wk / 2;
                double lr_width;
                double grid_chainage = centerline_pointdic.ElementAt(i).Key;

                if (grid_chainage < start1_chainage)   //后叉尾巴段
                {
                    //外线控制点
                    lr_width = LRwater_width;

                    //内线宽(按外线宽的百分比)
                    double k1 = 1.0 - (grid_chainage - centerline_pointdic.ElementAt(0).Key) / (start1_chainage - centerline_pointdic.ElementAt(0).Key);
                    double n_width = k1 * LRwater_width;

                    //内线控制点
                    if (i != 0)
                    {
                        PointXY n_point_l;
                        PointXY n_point_r;
                        PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(i).Value, centerline_pointdic.ElementAt(i + 1).Value, n_width, n_width, out n_point_l, out n_point_r);
                        l_n_point.Add(n_point_l);
                        r_n_point.Add(n_point_r);
                    }
                }
                else if (grid_chainage <= end1_chainage)  //中间段
                {
                    //外线控制点
                    lr_width = LRwater_width;
                }
                else   //前锋箭头段
                {
                    //内线宽(按外线宽的百分比)
                    double end_chainage = centerline_pointdic.Last().Key;
                    double k1 = 1.0 - Math.Abs((grid_chainage - end1_chainage) / (end_chainage - end1_chainage));
                    lr_width = k1 * LRwater_width;
                }

                //外线控制点
                PointXY w_point_l;
                PointXY w_point_r;
                PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(i).Value, centerline_pointdic.ElementAt(i + 1).Value, lr_width, lr_width, out w_point_l, out w_point_r);
                l_w_point.Add(w_point_l);
                r_w_point.Add(w_point_r);

            }

            //重新按顺序整合控制点
            adploy_point.AddRange(l_w_point);
            adploy_point.Add(centerline_pointdic.ElementAt(centerline_pointdic.Count - 1).Value);
            r_w_point.Reverse();
            adploy_point.AddRange(r_w_point);

            //增加内线控制点
            adploy_point.AddRange(r_n_point);
            adploy_point.Add(centerline_pointdic[start1_chainage]);
            l_n_point.Reverse();
            adploy_point.AddRange(l_n_point);

            //重复加一个外控制线第1个点
            adploy_point.Add(l_w_point[0]);

            return adploy_point;
        }

        //根据断面点、桩号和水面宽度，推求水质边界线坐标(泄漏期圆 + 箭头形)
        private static List<PointXY> Get_ADploy_Pointxy2(Dictionary<double, PointXY> centerline_pointdic, Dictionary<double, double> water_width, double start1_chainage, double end1_chainage, double ad_wk)
        {
            List<PointXY> adploy_point = new List<PointXY>();

            List<PointXY> l_w_point = new List<PointXY>();
            List<PointXY> r_w_point = new List<PointXY>();

            //重新遍历控制点
            int index2 = 0;
            for (int i = 0; i < centerline_pointdic.Count - 1; i++)
            {
                //水面宽度的一半
                double LRwater_width = water_width.ElementAt(i).Value * ad_wk / 2;
                double lr_width;
                double grid_chainage = centerline_pointdic.ElementAt(i).Key;

                PointXY w_point_l;
                PointXY w_point_r;

                //后圆弧段
                if (centerline_pointdic.ElementAt(i).Key == start1_chainage)
                {
                    //圆弧上第1个点
                    double start_distance = 0.3;
                    lr_width = 1.0;
                    PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(0).Value, centerline_pointdic.ElementAt(i).Value, lr_width, lr_width, out w_point_l, out w_point_r, start_distance);
                    l_w_point.Add(w_point_l);
                    r_w_point.Add(w_point_r);

                    //圆弧上第2个点
                    start_distance = 1;
                    lr_width = 1.73;
                    PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(0).Value, centerline_pointdic.ElementAt(i).Value, lr_width, lr_width, out w_point_l, out w_point_r, start_distance);
                    l_w_point.Add(w_point_l);
                    r_w_point.Add(w_point_r);

                    //圆弧上第3个点
                    start_distance = 2;
                    lr_width = 2;
                    PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(0).Value, centerline_pointdic.ElementAt(i).Value, lr_width, lr_width, out w_point_l, out w_point_r, start_distance);
                    l_w_point.Add(w_point_l);
                    r_w_point.Add(w_point_r);
                }

                if (centerline_pointdic.ElementAt(i).Key > start1_chainage)
                {
                    if (centerline_pointdic.ElementAt(i).Key <= end1_chainage)  //中间段
                    {

                        lr_width = LRwater_width;
                    }
                    else   //前锋箭头段
                    {
                        double end_chainage = centerline_pointdic.Last().Key;
                        double k1 = 1.0 - Math.Abs((grid_chainage - end1_chainage) / (end_chainage - end1_chainage));
                        lr_width = k1 * LRwater_width;
                    }
                    //控制点
                    PointXY.Get_Distance_LRPoints(centerline_pointdic.ElementAt(i).Value, centerline_pointdic.ElementAt(i + 1).Value, lr_width, lr_width, out w_point_l, out w_point_r);
                    l_w_point.Add(w_point_l);
                    r_w_point.Add(w_point_r);
                }
            }

            //重新按顺序整合控制点
            adploy_point.Add(centerline_pointdic.ElementAt(0).Value);
            adploy_point.AddRange(l_w_point);
            adploy_point.Add(centerline_pointdic.ElementAt(centerline_pointdic.Count - 1).Value);
            r_w_point.Reverse();
            adploy_point.AddRange(r_w_point);
            adploy_point.Add(centerline_pointdic.ElementAt(0).Value);

            return adploy_point;
        }

        #endregion *********************************************************
    }
}
