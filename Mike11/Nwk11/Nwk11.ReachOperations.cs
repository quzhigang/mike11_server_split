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
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace bjd_model.Mike11
{
    public partial class Nwk11
    {        
        #region ***************河道操作 -- 新增一维河道，根据控制点顺序设定上下游，会自动连接或不连*******************
        //增加新河道的第1步操作 --  任意方向和位置增加一条一维河道，并加入河道集合
        public static void Add_NewReach_Network(ref HydroModel hydromodel, ref string reachname, ref List<PointXY> reachpoints)
        {
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //调用方法获取新增的上下游河道详细信息
            ReachInfo newreach = Get_NewReachInfo(ref reachlist, ref reachpoints, ref reachname);

            //加入全局河道集合中
            reachlist.AddReach(newreach);
        }

        //创建新增的河道详细信息,所给的控制点顺序决定河道流向,河道可上游、下游与已有河道连接，也可不连，成为独立河道
        public static ReachInfo Get_NewReachInfo(ref ReachList reachlist, ref List<PointXY> reachpoints, ref string reachname)
        {
            //判断是否有相同位置的点，如果有，则x y +1
            ChangeNearPoint(reachlist, ref reachpoints);

            //创建一个河道详细信息变量
            ReachInfo newreach;

            //判断是否有同名河道，如果有自动改新河道名
            if (reachlist.Reach_baseinfolist != null)
            {
                for (int i = 0; i < reachlist.Reach_baseinfolist.Count; i++)
                {
                    if (reachname == reachlist.Reach_baseinfolist[i].reachname)
                    {
                        Console.WriteLine("有同名河道，自动改名,在河名后 + G");
                        reachname = reachname + "G";
                        break;
                    }
                }
            }

            newreach.reachname = reachname;
            newreach.reachtopoid = reachname + Model_Const.REACHID_HZ;     //按默认后缀取河道ID
            newreach.dx = Model_Const.MIKE11_DEFAULTDX;                //设置为默认步长(300m)

            //设置河道控制点属性
            List<ReachPoint> reachpoint_list = new List<ReachPoint>();

            int maxpointnumber = reachlist.Maxpointnumber;
            for (int i = 0; i < reachpoints.Count; i++)
            {
                //定义河道控制点
                ReachPoint reachpoint;
                reachpoint.number = maxpointnumber + i + 1;

                reachpoint.X = reachpoints[i].X;
                reachpoint.Y = reachpoints[i].Y;
                reachpoint.chainagetype = 1;

                //计算控制点桩号，默认起点桩号为0
                double chainage = 0;
                if (i != 0)
                {
                    double lastchainage = reachpoint_list[i - 1].pointchainage;
                    double lastX = reachpoint_list[i - 1].X;
                    double lastY = reachpoint_list[i - 1].Y;
                    double stance = Math.Round(Math.Sqrt((reachpoint.X - lastX) * (reachpoint.X - lastX) + (reachpoint.Y - lastY) * (reachpoint.Y - lastY)), 1);
                    chainage = lastchainage + stance;
                }
                reachpoint.pointchainage = chainage;

                reachpoint_list.Add(reachpoint);
            }

            //设置河道的起止桩号以及控制点集合
            newreach.reachpoint_list = reachpoint_list;
            newreach.start_chainage = newreach.reachpoint_list[0].pointchainage;
            newreach.end_chainage = newreach.reachpoint_list[newreach.reachpoint_list.Count - 1].pointchainage;

            //通过新河道起点和终点与最近河道的距离判断 是哪头连接或两头都连接 并输出上下游连接河道信息
            AtReach upreach;
            AtReach downreach;
            ConnectType reach_connecttype;
            GetConnect_ReachInfo(reachlist, reachpoints, out upreach, out downreach, out reach_connecttype);

            //连接桩号重新修改，防止距离过近
            if (upreach.reachname != "" &&
                upreach.chainage != reachlist.Get_Reachinfo(upreach.reachname).start_chainage &&
                upreach.chainage != reachlist.Get_Reachinfo(upreach.reachname).end_chainage)
            {
                List<double> chainagelist = reachlist.Get_ReachGridChainage(upreach.reachname);

                //连接点桩号可能会被修改
                double chainage = upreach.chainage;
                ChangeNearChainage(chainagelist, ref chainage);
                upreach.chainage = chainage;
            }

            if (downreach.reachname != "" &&
              downreach.chainage != reachlist.Get_Reachinfo(downreach.reachname).start_chainage &&
              downreach.chainage != reachlist.Get_Reachinfo(downreach.reachname).end_chainage)
            {
                List<double> chainagelist = reachlist.Get_ReachGridChainage(downreach.reachname);

                //连接点桩号可能会被修改
                double chainage = downreach.chainage;
                ChangeNearChainage(chainagelist, ref chainage);
                downreach.chainage = chainage;
            }

            newreach.upstream_connect = upreach;
            newreach.downstream_connect = downreach;

            //增加一个新的河道信息
            reachlist.AddReach(newreach);

            //更新河道计算点(**新河道计算点在河道断面文件操作中增加***)
            if (upreach.reachname != "")
            {
                reachlist.Add_ExistReach_GridChainage(upreach.reachname, upreach.chainage);
            }

            if (downreach.reachname != "")
            {
                reachlist.Add_ExistReach_GridChainage(downreach.reachname, downreach.chainage);
            }

            return newreach;
        }

        //判断新河道的点是否与原有河道具有相同的坐标，如果有则x y +1
        public static void ChangeNearPoint(ReachList reachlist, ref List<PointXY> reachpoints)
        {
            if (reachlist.Reach_infolist == null) return;

            //判断是否有相同位置的点，如果有，则x y +1
            int reachcount = reachlist.Reach_infolist.Count;

            for (int i = 0; i < reachpoints.Count; i++)
            {
                bool pointxychange = false;
                PointXY point;
                point.X = reachpoints[i].X;
                point.Y = reachpoints[i].Y;

                for (int j = 0; j < reachcount; j++)
                {
                    int reachpointcount = reachlist.Reach_infolist[j].reachpoint_list.Count;
                    for (int k = 0; k < reachpointcount; k++)
                    {
                        double X1 = reachlist.Reach_infolist[j].reachpoint_list[k].X;
                        double Y1 = reachlist.Reach_infolist[j].reachpoint_list[k].Y;
                        if (point.X == X1 && point.Y == Y1)
                        {
                            point.X += 1;
                            point.Y += 1;
                            pointxychange = true;
                            break;
                        }
                    }
                }

                if (pointxychange == true)
                {
                    reachpoints.RemoveAt(i);
                    reachpoints.Insert(i, point);
                }
            }
        }

        //判断所给桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
        public static int ChangeNearChainage(List<double> chainagelist, ref double chainage)
        {
            //桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (chainage >= chainagelist[i] && chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        return 0;
                    }
                    else if (Math.Abs(chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        //从已有河道中找出与新河道 起点或终点 最近的河道信息,作为新河道的上游或下游连接河道
        public static void GetConnect_ReachInfo(ReachList reachlist, List<PointXY> reachpoints, out AtReach upreach, out AtReach downreach, out ConnectType connecttype)
        {
            //先初始化赋值
            downreach.reachname = "";
            downreach.reachid = "";
            downreach.chainage = -1e-155;

            upreach.reachname = "";
            upreach.reachid = "";
            upreach.chainage = -1e-155;

            connecttype = ConnectType.Upconnect;  //连接类型

            //第一个点和最后一个点
            PointXY startpoint = reachpoints[0];
            PointXY endpoint = reachpoints[reachpoints.Count - 1];

            //找到离新河道第1个点最近的河道信息
            int min_reachnumber;
            double chainage;
            double min_distance;
            Get_ConnectReach(reachlist, startpoint, out min_reachnumber, out chainage, out min_distance);

            //判断是上游连接还是下游连接，并返回相应连接河道信息
            if (min_distance < Model_Const.MIKE11_MINCONNECTDISTANCE)  //上游连接
            {
                //重新设置上游连接河道信息
                upreach.reachname = reachlist.Reach_infolist[min_reachnumber].reachname;
                upreach.reachid = reachlist.Reach_infolist[min_reachnumber].reachtopoid;
                upreach.chainage = chainage;
            }

            //找到离新河道最后1个点最近的河道信息
            int min_reachnumber1;
            double chainage1;
            double min_distance1;
            Get_ConnectReach(reachlist, endpoint, out min_reachnumber1, out chainage1, out min_distance1);

            if (min_distance1 < Model_Const.MIKE11_MINCONNECTDISTANCE)  //下游连接
            {
                //重新设置下游连接河道信息
                downreach.reachname = reachlist.Reach_infolist[min_reachnumber1].reachname;
                downreach.reachid = reachlist.Reach_infolist[min_reachnumber1].reachtopoid;
                downreach.chainage = chainage1;
            }

            //判断新河道与上下游河道的连接方式
            if (upreach.reachname != "" && downreach.reachname == "")
            {
                connecttype = ConnectType.Upconnect;
            }
            else if (upreach.reachname == "" && downreach.reachname != "")
            {
                connecttype = ConnectType.Downconncet;
            }
            else if (upreach.reachname != "" && downreach.reachname != "")
            {
                connecttype = ConnectType.Allconnect;
            }
            else
            {
                connecttype = ConnectType.Noneconnect;
            }
        }

        //求距离给定点最近的河道名和内插桩号 -- 用于获取最近河道
        public static AtReach Get_NearReach(ReachList reachlist, PointXY p)
        {
            //调用方法求所在河道信息
            int min_reachnumber;
            double chainage;
            double min_distance;
            Nwk11.Get_ConnectReach(reachlist, p, out min_reachnumber, out chainage, out min_distance);

            AtReach reachinfo;
            reachinfo.reachname = reachlist.Reach_baseinfolist[min_reachnumber].reachname;
            reachinfo.reachid = reachlist.Reach_baseinfolist[min_reachnumber].reachtopoid;
            reachinfo.chainage = Math.Round(chainage,0);

            return reachinfo;
        }

        //求距离给定点最近的河道名和内插桩号 -- 用于获取最近河道，限定河道
        public static AtReach Get_NearReach(ReachList reachlist,string reachname, PointXY p)
        {
            //调用方法求所在河道信息
            double chainage;
            double min_distance;
            Nwk11.Get_NearstReach(reachlist, reachname, p, out chainage, out min_distance);

            AtReach reachinfo;
            reachinfo.reachname = reachname;
            reachinfo.reachid = reachname + Model_Const.REACHID_HZ;
            reachinfo.chainage = Math.Round(chainage, 0);

            return reachinfo;
        }

        //求距离给定点最近的河道横断面位置
        public static AtReach Get_NearReach_Sectionchainage(SectionList sectionlist, ReachList reachlist, PointXY p)
        {
            //先获取距离给定点最近的河道名和内插的桩号
            AtReach nearreach = Get_NearReach(reachlist, p);

            //根据河名获取河道断面桩号
            List<double> reach_sectionchainagelist = sectionlist.Get_Reach_SecChainageList(nearreach.reachname);

            //找到最近的断面桩号位置
            double min_distance = 10000;
            double min_distance_chainage = reach_sectionchainagelist[0];
            for (int i = 0; i < reach_sectionchainagelist.Count; i++)
            {
                if (Math.Abs(reach_sectionchainagelist[i] - nearreach.chainage) < min_distance)
                {
                    min_distance = Math.Abs(reach_sectionchainagelist[i] - nearreach.chainage);
                    min_distance_chainage = reach_sectionchainagelist[i];
                }
            }

            return AtReach.Get_Atreach(nearreach.reachname, min_distance_chainage);
        }

        //求距离给定点最近的河道名和内插桩号 -- 用于最近河道的连接
        public static int Get_ConnectReach(ReachList reachlist, PointXY p, out int min_reachnumber, out double chainage, out double min_distance)
        {
            min_reachnumber = 0;
            chainage = -1;
            min_distance = Model_Const.MIKE11_MAXPTOREACHSTANCE;
            if (reachlist.Reach_infolist == null) return -1;

            //定义存储各河道名河各节点距离指定点距离的嵌套键值对集合
            double mindistance = 2000;
            for (int i = 0; i < reachlist.Reach_infolist.Count; i++)
            {
                //遍历各河道
                int reachpointcount = reachlist.Reach_infolist[i].reachpoint_list.Count;

                //最后通过判断找出距离给定点最近的两个点和所在的河道，内插出桩号
                for (int j = 0; j < reachpointcount; j++)
                {
                    double X1 = reachlist.Reach_infolist[i].reachpoint_list[j].X;
                    double Y1 = reachlist.Reach_infolist[i].reachpoint_list[j].Y;

                    //所给的点恰好在控制点上
                    if (p.X == X1 && p.Y == Y1)
                    {
                        min_reachnumber = i;
                        chainage = reachlist.Reach_infolist[i].reachpoint_list[j].pointchainage;
                        min_distance = 0;
                        return 0;
                    }

                    //所给的点不在控制点上的情况
                    double distance = Math.Sqrt((X1 - p.X) * (X1 - p.X) + (Y1 - p.Y) * (Y1 - p.Y));
                    if (distance < mindistance)
                    {
                        mindistance = distance;
                        //重新给用于输出的河道名称、桩号赋值
                        min_reachnumber = i;
                        if (j == 0 || j == reachpointcount - 1)
                        {
                            //直接为起点或终点的桩号
                            chainage = reachlist.Reach_infolist[i].reachpoint_list[j].pointchainage;
                            min_distance = mindistance;
                        }
                        else
                        {
                            //通过垂足求最小距离和桩号
                            Get_CZdistance(reachlist.Reach_infolist[i],p, j, out min_distance,out chainage);
                        }
                    }
                }
            }
            return 0;
        }

        //求距离给定点最近的河道名和内插桩号 -- 限定河道
        public static int Get_NearstReach(ReachList reachlist, string reachname, PointXY p, out double chainage, out double min_distance)
        {
            chainage = -1;
            min_distance = 10000;
            if (reachlist.Reach_infolist == null) return -1;

            ReachInfo reach = reachlist.Get_Reachinfo(reachname);

            //定义存储各河道名河各节点距离指定点距离的嵌套键值对集合
            double mindistance = 10000;

            int reachpointcount = reach.reachpoint_list.Count;

            //最后通过判断找出距离给定点最近的两个点和所在的河道，内插出桩号
            for (int j = 0; j < reachpointcount; j++)
            {
                double X1 = reach.reachpoint_list[j].X;
                double Y1 = reach.reachpoint_list[j].Y;

                //所给的点恰好在控制点上
                if (p.X == X1 && p.Y == Y1)
                {
                    chainage = reach.reachpoint_list[j].pointchainage;
                    min_distance = 0;
                    return 0;
                }

                //所给的点不在控制点上的情况
                double distance = Math.Sqrt((X1 - p.X) * (X1 - p.X) + (Y1 - p.Y) * (Y1 - p.Y));
                if (distance < mindistance)
                {
                    mindistance = distance;
                    //重新给用于输出的河道桩号赋值
                    if (j == 0 || j == reachpointcount - 1)
                    {
                        //直接为起点或终点的桩号
                        chainage = reach.reachpoint_list[j].pointchainage;
                        min_distance = mindistance;
                    }
                    else
                    {
                       //通过垂足求最小距离和桩号
                       Get_CZdistance(reach, p, j, out min_distance,out chainage);
                    }
                }
            }
            return 0;
        }

        //通过垂足求最小距离和桩号
        private static void Get_CZdistance(ReachInfo reach, PointXY p, int j, out double min_distance, out double chainage)
        {
            //定义上游点和下游点
            PointXY uppoint;
            uppoint.X = 0;
            uppoint.Y = 0;

            PointXY downpoint;
            downpoint.X = 0;
            downpoint.Y = 0;

            //定义上游点坐标、当前点坐标和下游点点坐标
            double upx = reach.reachpoint_list[j - 1].X;
            double upy = reach.reachpoint_list[j - 1].Y;
            double downx = reach.reachpoint_list[j + 1].X;
            double downy = reach.reachpoint_list[j + 1].Y;
            double currentx = reach.reachpoint_list[j].X;
            double currenty = reach.reachpoint_list[j].Y;

            //求j-1点和J点线段的垂足，并判断垂足是否在线段上
            double k = (upy - currenty) / (upx - currentx); //直线斜率
            PointXY upp;
            upp.X = upx;
            upp.Y = upy;
            PointXY Projectpoint1 = PointXY.GetProjectivePoint(upp, k, p);  //垂足
            double updowndistance1 = PointXY.Get_ptop_distance(currentx, currenty, upp.X, upp.Y);
            double sumdistance1 = PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, upp.X, upp.Y) + PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, currentx, currenty);

            //求j点和J+1点线段的垂足，并判断垂足是否在线段上
            double k1 = (downy - currenty) / (downx - currentx); //直线斜率
            PointXY downp;
            downp.X = downx;
            downp.Y = downy;
            PointXY Projectpoint2 = PointXY.GetProjectivePoint(downp, k1, p);  //垂足
            double updowndistance2 = PointXY.Get_ptop_distance(currentx, currenty, downp.X, downp.Y);
            double sumdistance2 = PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, downp.X, downp.Y) + PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, currentx, currenty);

            //判断垂足是否在线段上，以及在哪个线段（上游线段 还是 下游线段）上
            double upchainage = 0;
            if (Math.Abs(sumdistance1 - updowndistance1) < 1) //垂足在j-1点和J点组成的线段之间 
            {
                //定义上游点为上一点，下游点为当前点
                uppoint.X = reach.reachpoint_list[j - 1].X;
                uppoint.Y = reach.reachpoint_list[j - 1].Y;
                upchainage = reach.reachpoint_list[j - 1].pointchainage;
                downpoint.X = reach.reachpoint_list[j].X;
                downpoint.Y = reach.reachpoint_list[j].Y;

                //求点到线段的垂直距离以及垂足桩号
                min_distance = PointXY.PToL_Distance(upx, upy, currentx, currenty, p.X, p.Y);
                chainage = upchainage + PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, uppoint.X, uppoint.Y);
            }
            else if (Math.Abs(sumdistance2 - updowndistance2) < 1)  //垂足在j点和J+1点组成的线段之间 
            {
                //定义上游点为当前点，下游点为下一点
                uppoint.X = reach.reachpoint_list[j].X;
                uppoint.Y = reach.reachpoint_list[j].Y;
                upchainage = reach.reachpoint_list[j].pointchainage;
                downpoint.X = reach.reachpoint_list[j + 1].X;
                downpoint.Y = reach.reachpoint_list[j + 1].Y;

                //求点到线段的垂直距离以及垂足桩号
                min_distance = PointXY.PToL_Distance(currentx, currenty, downx, downy, p.X, p.Y);
                chainage = upchainage + PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, uppoint.X, uppoint.Y);
            }
            else   //垂足不在前、后线段内
            {
                min_distance = PointXY.Get_ptop_distance(currentx, currenty, p.X, p.Y);
                chainage = reach.reachpoint_list[j].pointchainage;
            }
        }

        //判断点在河道左岸还是右岸
        public static ReachLR Get_PointAtReachLR(ReachList reachlist, PointXY point, AtReach reachinfo)
        {
            //获取河道位置最近的两个控制点的集合
            List<PointXY> reachpointlist = reachlist.Get_ReachNearPointxyList(reachinfo.reachname, reachinfo.chainage);

            //求该点与河道线段的夹角
            double angle = PointXY.GetAngle(reachpointlist[0], reachpointlist[reachpointlist.Count - 1], point);

            if (angle > 0) //在左岸
            {
                return ReachLR.left;
            }
            else if (angle < 0)
            {
                return ReachLR.right;
            }
            else  //在河道线上
            {
                return ReachLR.left_right;
            }
        }

        //通过GIS服务，用断面线DEM上剖切断面数据 -  新河道
        public static Dictionary<AtReach, List<PointXZS>> GetSectionData_FromGIS(HydroModel hydromodel, ReachInfo newreach)
        {
            //获取上下游连接河道的最大宽度
            double distance = 0;
            if (newreach.upstream_connect.reachname != "")
            {
                //获取上游连接河道的最大宽度 
                distance = hydromodel.Mike11Pars.SectionList.Get_ReachSection_Maxwidth(newreach.upstream_connect.reachname) / 2;
            }
            else if (newreach.downstream_connect.reachname != "")
            {
                //获取下游连接河道的最大宽度 
                distance = hydromodel.Mike11Pars.SectionList.Get_ReachSection_Maxwidth(newreach.downstream_connect.reachname) * 1.2 / 2;
            }
            else   //上下游都没有连接河道，则按默认剖切宽度 300m
            {
                distance = Model_Const.SECTION_MAXWIDTH;
            }

            //获取获取新河道断面线桩号坐标 
            Dictionary<AtReach, List<PointXY>> ReachSection_LRPointXY = GetReachSection_LRPointXY(newreach, distance, distance);

            //逐个断面通过gis服务获取断面高程点
            Dictionary<AtReach, List<PointXZS>> SectionData = new Dictionary<AtReach, List<PointXZS>>();
            for (int i = 0; i < ReachSection_LRPointXY.Count; i++)
            {
                List<PointXZS> section_points = Gis_Service.Get_Sectiondata_FromGisService(ReachSection_LRPointXY.Values.ElementAt(i));
                SectionData.Add(ReachSection_LRPointXY.Keys.ElementAt(i), section_points);
            }

            return SectionData;
        }

        //通过GIS服务，用断面线DEM上剖切断面数据 -  原有河道断面数据更新
        public static Dictionary<AtReach, List<PointXZS>> GetSectionData_FromGIS(HydroModel hydromodel, Reach_Segment reach_segment)
        {
            //先获取指定河段的断面数据
            Dictionary<AtReach, List<PointXZS>> ReachSectionDataList = hydromodel.Mike11Pars.SectionList.Get_SegReach_SecDataList(reach_segment);

            //获取原河道断面线桩号坐标 
            Dictionary<AtReach, List<PointXY>> ReachSection_LRPointXY = GetReachSection_LRPointXY(hydromodel, ReachSectionDataList);

            //逐个断面通过gis服务获取断面高程点
            Dictionary<AtReach, List<PointXZS>> SectionData = new Dictionary<AtReach, List<PointXZS>>();
            for (int i = 0; i < ReachSectionDataList.Count; i++)
            {
                List<PointXZS> section_points = Gis_Service.Get_Sectiondata_FromGisService(ReachSection_LRPointXY.Values.ElementAt(i));
                SectionData.Add(ReachSection_LRPointXY.Keys.ElementAt(i), section_points);
            }

            return SectionData;
        }


        //获取新河道断面线桩号坐标 -- 按步长分 distance -- 用于剖切断面的宽度,左右相同
        public static Dictionary<AtReach, List<PointXY>> GetReachSection_LRPointXY(ReachInfo newreach, double L_distance, double R_distance)
        {
            Dictionary<AtReach, List<PointXY>> ReachSectionChainage = new Dictionary<AtReach, List<PointXY>>();
            double reachlength = Math.Abs(newreach.end_chainage - newreach.start_chainage);

            //断面桩号
            int sectioncount = (int)(reachlength / newreach.dx) + 1;
            double[] section_chainage = new double[sectioncount];
            for (int i = 0; i < sectioncount - 1; i++)
            {
                section_chainage[i] = newreach.start_chainage + i * newreach.dx;
            }
            section_chainage[sectioncount - 1] = newreach.end_chainage;

            //断面起止点坐标
            List<PointXY> LRPoints;
            for (int i = 0; i < sectioncount; i++)
            {
                double chainage = section_chainage[i];
                AtReach atreach;
                atreach.chainage = chainage;
                atreach.reachname = newreach.reachname;
                atreach.reachid = atreach.reachname + Model_Const.REACHID_HZ;

                LRPoints = PointXY.Get_Distance_LRPoints(newreach.reachpoint_list, chainage, L_distance, R_distance);

                ReachSectionChainage.Add(atreach, LRPoints);
            }

            return ReachSectionChainage;
        }

        //获取老河道断面线桩号坐标 
        public static Dictionary<AtReach, List<PointXY>> GetReachSection_LRPointXY(HydroModel hydromodel, Dictionary<AtReach, List<PointXZS>> ReachSectionDataList)
        {
            Dictionary<AtReach, List<PointXY>> ReachSectionChainage = new Dictionary<AtReach, List<PointXY>>();
            string reachname = ReachSectionDataList.Keys.ElementAt(0).reachname;

            //获取河道的最大宽度
            double distance = hydromodel.Mike11Pars.SectionList.Get_ReachSection_Maxwidth(reachname) / 2;
            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);

            //断面起止点坐标
            List<PointXY> LRPoints;
            for (int i = 0; i < ReachSectionDataList.Count; i++)
            {
                AtReach atreach = ReachSectionDataList.Keys.ElementAt(i);
                List<PointXZS> section_pointlist = ReachSectionDataList.Values.ElementAt(i);
                LRPoints = PointXY.Get_Distance_LRPoints(reach.reachpoint_list, atreach.chainage, distance, distance);

                ReachSectionChainage.Add(atreach, LRPoints);
            }

            return ReachSectionChainage;
        }

        #endregion ***************************************************************************************************
    }
}
