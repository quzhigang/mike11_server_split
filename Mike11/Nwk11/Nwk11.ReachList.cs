using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using System.Globalization;
using System.Data;
using Kdbndp;

using System.IO;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;

using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bjd_model.Mike11
{
    //河道类，可由此得到默认所有河道的详细信息和新增加的河道信息(统一排)
    [Serializable]
    public class ReachList
    {
        //**********************属性************************
        //河道详细信息属性集合,里面含河道的控制点
        public List<ReachInfo> Reach_infolist
        { get; set; }

        //河道基本属性信息集合  其中的桩号为河道起点桩号
        public List<Reach_Segment> Reach_baseinfolist
        { get; set; }

        //河道计算点桩号集合，包括 断面桩号、入流出流点桩号、建筑物桩号，防止新增建筑物离它们过近
        public Dictionary<string, List<double>> Reach_gridchainagelist
        { get; set; }

        //河道控制点编号的最大值
        public int Maxpointnumber
        { get; set; }

        //**********************方法************************
        //增加已有河道计算点桩号集合方法
        public void Add_ExistReach_GridChainage(string reachname, double newchainage)
        {
            //获取该河道原来的计算点集合
            List<double> newchainagelist = Get_ReachGridChainage(reachname);
            if (!newchainagelist.Contains(newchainage))
            {
                newchainagelist.Add(newchainage);
            }

            //排序
            newchainagelist.Sort();

            //删除该河道原来的河道计算点集合
            Reach_gridchainagelist.Remove(reachname);

            //重新添加
            Reach_gridchainagelist.Add(reachname, newchainagelist);
        }

        //减少已有河道计算点桩号集合方法
        public void Remove_ExistReach_GridChainage(string reachname, double substactchainage)
        {
            //获取该河道原来的计算点集合
            List<double> newchainagelist = Get_ReachGridChainage(reachname);
            if (newchainagelist.Contains(substactchainage))
            {
                newchainagelist.Remove(substactchainage);
            }

            //删除该河道原来的河道计算点集合
            Reach_gridchainagelist.Remove(reachname);

            //重新添加
            Reach_gridchainagelist.Add(reachname, newchainagelist);
        }

        //增加新河道计算点桩号集合方法
        public void Add_NewReach_GridChainage(string reachname, List<double> newreach_gridchainagelist)
        {
            if (!Reach_gridchainagelist.Keys.Contains(reachname))
            {
                Reach_gridchainagelist.Add(reachname, newreach_gridchainagelist);
            }
        }

        //增加新河道方法
        public void AddReach(ReachInfo newreach)
        {
            if (!Reach_infolist.Contains(newreach))
            {
                //更改河道信息信息属性
                Reach_infolist.Add(newreach);

                //更改河道基本信息属性
                Reach_Segment reachbaseinfo;
                reachbaseinfo.reachname = newreach.reachname;
                reachbaseinfo.reachtopoid = newreach.reachtopoid;
                reachbaseinfo.start_chainage = newreach.start_chainage;
                reachbaseinfo.end_chainage = newreach.end_chainage;
                Reach_baseinfolist.Add(reachbaseinfo);

                //更改河道控制点编号最大值属性
                if (newreach.reachpoint_list[newreach.reachpoint_list.Count - 1].number > Maxpointnumber)
                {
                    Maxpointnumber = newreach.reachpoint_list[newreach.reachpoint_list.Count - 1].number;
                }

                //增加2个计算点（起点和终点）
                List<double> newreach_grid = new List<double>();
                newreach_grid.Add(newreach.start_chainage);
                newreach_grid.Add(newreach.end_chainage);
                Reach_gridchainagelist.Add(newreach.reachname, newreach_grid);
            }
        }

        //减少河道方法
        public void RemoveReach(string reachname)
        {
            for (int i = 0; i < Reach_infolist.Count; i++)
            {
                if (Reach_infolist[i].reachname == reachname)
                {
                    Reach_infolist.RemoveAt(i);
                    Reach_baseinfolist.RemoveAt(i);
                    Reach_gridchainagelist.Remove(reachname);
                    break;
                }
            }
        }

        //根据河道名获取该河道详细信息方法
        public ReachInfo Get_Reachinfo(string reachname)
        {
            ReachInfo reach = Reach_infolist[0];

            for (int i = 0; i < Reach_infolist.Count; i++)
            {
                if (Reach_infolist[i].reachname == reachname)
                {
                    reach = Reach_infolist[i];
                    break;
                }
            }
            return reach;
        }

        //根据河道名称和桩号获取河道坐标
        public PointXY Get_ReachPointxy(string reachname, double chainage)
        {
            List<ReachPoint> reach_points = Get_Reachinfo(reachname).reachpoint_list;

            //通过二分法查找
            return Search_Value(reach_points, chainage);
        }

        //根据河道名称和桩号获取河道坐标
        public PointXY Get_ReachPointxy(string reachname, double chainage,out int angle)
        {
            List<ReachPoint> reach_points = Get_Reachinfo(reachname).reachpoint_list;

            //通过二分法查找
            return Search_Value(reach_points, chainage,out angle);
        }

        //二分法查找指定桩号值的集合 序号
        public PointXY Search_Value(List<ReachPoint> reach_points, double chainage)
        {
            PointXY pointxy;
            pointxy.X = 0;
            pointxy.Y = 0;

            int tou = 0;
            int wei = reach_points.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (reach_points[zhong].pointchainage == chainage)
                {
                    return PointXY.Get_Pointxy(reach_points[zhong].X, reach_points[zhong].Y);
                }
                else if (reach_points[zhong].pointchainage > chainage)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            //如果没有找到，说明不在网格点上，则采用尾部序号附近的再找找范围内的，然后内插
            for (int i = Math.Max(wei - 1, 0); i < wei + 1; i++)
            {
                if(reach_points[i].pointchainage < chainage && reach_points[i+1].pointchainage > chainage)
                {
                    double chainage1 = reach_points[i].pointchainage;
                    double chainage2 = reach_points[i + 1].pointchainage;

                    double x1 = reach_points[i].X;
                    double x2 = reach_points[i + 1].X;

                    double y1 = reach_points[i].Y;
                    double y2 = reach_points[i + 1].Y;

                    pointxy.X = x1 + (x2 - x1) * (chainage - chainage1) / (chainage2 - chainage1);
                    pointxy.Y = y1 + (y2 - y1) * (chainage - chainage1) / (chainage2 - chainage1);
                    return pointxy;
                }
            }

            return pointxy;
        }

        //二分法查找指定桩号值的集合 序号 输出角度
        public PointXY Search_Value(List<ReachPoint> reach_points, double chainage,out int angle)
        {
            angle = 0;
            PointXY pointxy;
            pointxy.X = 0;
            pointxy.Y = 0;

            int tou = 0;
            int wei = reach_points.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (reach_points[zhong].pointchainage == chainage)
                {
                    PointXY res_point = PointXY.Get_Pointxy(reach_points[zhong].X, reach_points[zhong].Y);
                    if (zhong != wei)
                    {
                        PointXY next_point = PointXY.Get_Pointxy(reach_points[zhong +1].X, reach_points[zhong +1].Y);
                        angle = PointXY.Get_ptop_Angle(res_point, next_point);
                    }
                    return res_point;
                }
                else if (reach_points[zhong].pointchainage > chainage)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            //如果没有找到，说明不在网格点上，则采用尾部序号附近的再找找范围内的，然后内插
            for (int i = Math.Max(wei - 1,0); i < wei + 1; i++)
            {
                if (reach_points[i].pointchainage < chainage && reach_points[i + 1].pointchainage > chainage)
                {
                    double chainage1 = reach_points[i].pointchainage;
                    double chainage2 = reach_points[i + 1].pointchainage;

                    double x1 = reach_points[i].X;
                    double x2 = reach_points[i + 1].X;

                    double y1 = reach_points[i].Y;
                    double y2 = reach_points[i + 1].Y;

                    pointxy.X = x1 + (x2 - x1) * (chainage - chainage1) / (chainage2 - chainage1);
                    pointxy.Y = y1 + (y2 - y1) * (chainage - chainage1) / (chainage2 - chainage1);

                    PointXY start_point = PointXY.Get_Pointxy(x1, y1);
                    PointXY end_point = PointXY.Get_Pointxy(x2,y2);
                    angle = PointXY.Get_ptop_Angle(start_point, end_point);
                    return pointxy;
                }
            }

            return pointxy;
        }

        //根据河道名称和桩号获取河道上下游控制点的集合
        public List<PointXY> Get_ReachNearPointxyList(string reachname, double chainage)
        {
            List<PointXY> ReachNearPointxyList = new List<PointXY>();
            PointXY lastpointxy;
            lastpointxy.X = 0;
            lastpointxy.Y = 0;

            PointXY nextpointxy;
            nextpointxy.X = 0;
            nextpointxy.Y = 0;

            ReachInfo reachinfo = Get_Reachinfo(reachname);
            for (int i = 0; i < reachinfo.reachpoint_list.Count - 1; i++)
            {
                if (chainage == reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 1].pointchainage)
                {
                    lastpointxy.X = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 2].X;
                    lastpointxy.Y = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 2].Y;

                    nextpointxy.X = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 1].X;
                    nextpointxy.Y = reachinfo.reachpoint_list[reachinfo.reachpoint_list.Count - 1].Y;
                    break;
                }

                if (reachinfo.reachpoint_list[i].pointchainage <= chainage &&
                    reachinfo.reachpoint_list[i + 1].pointchainage > chainage)
                {

                    lastpointxy.X = reachinfo.reachpoint_list[i].X;
                    lastpointxy.Y = reachinfo.reachpoint_list[i].Y;

                    nextpointxy.X = reachinfo.reachpoint_list[i + 1].X;
                    nextpointxy.Y = reachinfo.reachpoint_list[i + 1].Y;
                    break;
                }
            }
            ReachNearPointxyList.Add(lastpointxy);
            ReachNearPointxyList.Add(nextpointxy);
            return ReachNearPointxyList;
        }

        //根据河道名称和起止桩号获取该河段控制点坐标集合
        public List<PointXY> Get_Segment_PointjwdList(string reachname, double start_chainage, double end_chainage)
        {
            List<PointXY> segment_jwdlist = new List<PointXY>();

            //河道和河道中心线折点信息
            ReachInfo reachinfo = Get_Reachinfo(reachname);
            List<ReachPoint> reach_pointinfo = reachinfo.reachpoint_list;

            // 如果河道控制点为空或起止桩号无效，返回空列表
            if (reach_pointinfo == null || reach_pointinfo.Count == 0 || start_chainage > end_chainage)
                return segment_jwdlist;

            // 找到包含起止桩号的控制点段
            int start_index = -1, end_index = -1;

            // 查找起始桩号所在的段
            for (int i = 0; i < reach_pointinfo.Count - 1; i++)
            {
                if (start_chainage >= reach_pointinfo[i].pointchainage &&
                    start_chainage <= reach_pointinfo[i + 1].pointchainage)
                {
                    start_index = i;
                    break;
                }
            }

            // 查找结束桩号所在的段
            for (int i = 0; i < reach_pointinfo.Count - 1; i++)
            {
                if (end_chainage >= reach_pointinfo[i].pointchainage &&
                    end_chainage <= reach_pointinfo[i + 1].pointchainage)
                {
                    end_index = i;
                    break;
                }
            }

            // 如果起止桩号不在河道范围内，返回空列表
            if (start_index == -1 || end_index == -1)
                return segment_jwdlist;

            // 获取该河段折点坐标集合
            List<PointXY> segment_xylist = new List<PointXY>();

            // 添加起始内插点
            if (start_chainage > reach_pointinfo[start_index].pointchainage)
            {
                // 在起始段内插点
                PointXY start_point = InterpolatePoint(reach_pointinfo[start_index],
                                                    reach_pointinfo[start_index + 1],
                                                    start_chainage);
                segment_xylist.Add(start_point);
            }
            else
            {
                // 直接添加起始控制点
                segment_xylist.Add(PointXY.Get_Pointxy(reach_pointinfo[start_index].X,
                                                    reach_pointinfo[start_index].Y));
            }

            // 添加中间完整段的所有控制点
            for (int i = start_index + 1; i <= end_index; i++)
            {
                segment_xylist.Add(PointXY.Get_Pointxy(reach_pointinfo[i].X, reach_pointinfo[i].Y));
            }

            // 添加结束内插点（如果需要）
            if (end_chainage < reach_pointinfo[end_index + 1].pointchainage)
            {
                // 在结束段内插点
                PointXY end_point = InterpolatePoint(reach_pointinfo[end_index],
                                                  reach_pointinfo[end_index + 1],
                                                  end_chainage);
                segment_xylist.Add(end_point);
            }
            else if (end_chainage == reach_pointinfo[end_index + 1].pointchainage)
            {
                // 如果正好在结束控制点上，添加该点
                segment_xylist.Add(PointXY.Get_Pointxy(reach_pointinfo[end_index + 1].X,
                                                    reach_pointinfo[end_index + 1].Y));
            }

            // 转为投影坐标
            segment_jwdlist = PointXY.CoordTranfrom(segment_xylist, 4547, 4326, 6);

            return segment_jwdlist;
        }

        // 内插点方法：根据桩号在线性段上内插坐标点
        private PointXY InterpolatePoint(ReachPoint p1, ReachPoint p2, double chainage)
        {
            // 计算比例因子
            double ratio = (chainage - p1.pointchainage) / (p2.pointchainage - p1.pointchainage);

            // 线性内插坐标
            double x = p1.X + ratio * (p2.X - p1.X);
            double y = p1.Y + ratio * (p2.Y - p1.Y);

            return PointXY.Get_Pointxy(x, y);
        }

        //根据河道名获取该河道计算点桩号方法
        public List<double> Get_ReachGridChainage(string reachname)
        {
            List<double> gridchainagelist = Reach_gridchainagelist.Values.ElementAt(0);

            for (int i = 0; i < Reach_infolist.Count; i++)
            {
                if (Reach_infolist[i].reachname == reachname)
                {
                    try
                    {
                        gridchainagelist = Reach_gridchainagelist.Values.ElementAt(i);
                    }
                    catch (Exception er)
                    {
                        Console.WriteLine(er.Message);
                    }
                    break;
                }
            }
            return gridchainagelist;
        }

    }

}
