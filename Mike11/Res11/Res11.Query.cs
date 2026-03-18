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
        #region ***********************原单点数据查询 *****************************
        //查询河道指定点的全部结果数据（水位、流量、流速）
        public static Dictionary<DateTime, mike11_res> Get_Mike11PointRes(string plan_code, PointXY p)
        {
            Dictionary<DateTime, mike11_res> mike11_resdic = new Dictionary<DateTime, mike11_res>();
            string model_dir = Model_Files.Get_Modeldir(plan_code);
            string res1d = model_dir + @"\" + "results" + @"\" + "hd_result.res1d";
            string res11 = model_dir + @"\" + "results" + @"\" + "hd_result.res11";
            string sourcefilename = File.Exists(res1d) ? res1d : res11;

            PointXY jw_p = PointXY.CoordTranfrom(p, 4326, 4547, 3);

            //求距离指定点最近的河道并内插该点的桩号
            string reachname;
            double chainage;
            Res11.Get_reach_chainage(sourcefilename, jw_p, out reachname, out chainage);
            if (chainage == -1) return null;
            AtReach atreach = AtReach.Get_Atreach(reachname, chainage);

            //根据河名、桩号求各指定项目的键值对集合
            Dictionary<DateTime, double> levelresdic = Res11.Res11reader(plan_code, atreach, mike11_restype.Water_Level);
            Dictionary<DateTime, double> dischargeresdic = Res11.Res11reader(plan_code, atreach, mike11_restype.Discharge);

            //减去前面的数值
            int remove_hours = plan_code == Model_Const.AUTO_MODELNAME ? Model_Const.AUTOFORECAST_AHEAD_HOURS : Model_Const.AHEAD_HOURS;
            Remove_Ahead_Res(ref levelresdic, remove_hours);
            Remove_Ahead_Res(ref dischargeresdic, remove_hours);

            //获取最近的断面桩号
            string xns11_file = model_dir + @"\" + "hddm.xns11";
            List<double> sectionlist = Xns11.Get_ReachChainageList(xns11_file, reachname, reachname + Model_Const.REACHID_HZ);
            double section_chainage = sectionlist[0];
            for (int i = 0; i < sectionlist.Count - 1; i++)
            {
                if (sectionlist[i] <= chainage && sectionlist[i + 1] >= chainage)
                {
                    if (Math.Abs(sectionlist[i] - chainage) < Math.Abs(sectionlist[i + 1] - chainage))
                    {
                        section_chainage = sectionlist[i];
                    }
                    else
                    {
                        section_chainage = sectionlist[i + 1];
                    }
                    break;
                }
            }

            //获取断面
            ICrossSection sectioninfo = Xns11.Get_ReachSectionInfo(xns11_file, reachname, reachname + Model_Const.REACHID_HZ, section_chainage);

            //获取平均流速
            DateTime datetime = levelresdic.Keys.ElementAt(0);
            double level = levelresdic.Values.ElementAt(0);
            double discharge = dischargeresdic.Values.ElementAt(0);
            double sill = sectioninfo.ZMin;
            double avespeed = 0;

            for (int i = 0; i < levelresdic.Count; i++)
            {
                datetime = levelresdic.Keys.ElementAt(i);
                level = Math.Max(sectioninfo.BaseCrossSection.BottomLevel, levelresdic.Values.ElementAt(i));
                discharge = dischargeresdic.Values.ElementAt(i);

                //计算平均流速
                if (level > sectioninfo.ZMax)
                {
                    avespeed = discharge / (sectioninfo.GetArea(sectioninfo.ZMax) + sectioninfo.GetStorageWidth(sectioninfo.ZMax - 0.1) * (level - sectioninfo.ZMax));
                }
                else if (sectioninfo.GetArea(level) != 0)
                {
                    avespeed = discharge / sectioninfo.GetArea(level);
                }
                else
                {
                    avespeed = 0;
                }

                avespeed = Math.Min(avespeed, 6);

                mike11_res res;
                res.Water_Level = Math.Round(level, 2);
                res.Water_H = Math.Round(level - sill, 2);
                res.Discharge = Math.Round(discharge, 2);
                res.Average_Speed = Math.Round(avespeed, 2);

                //加入集合
                mike11_resdic.Add(datetime, res);
            }

            return mike11_resdic;
        }

        //查询指定坐标的指定项(坐标不一定要在河道上，根据最近原则求河道和桩号后，再求指定项的时间序列数据)
        public static Dictionary<DateTime, double> Res11reader(string plan_code, PointXY p, mike11_restype itemtype)
        {
            string model_dir = Model_Files.Get_Modeldir(plan_code);
            string res1d = model_dir + @"\" + "results" + @"\" + "hd_result.res1d";
            string res11 = model_dir + @"\" + "results" + @"\" + "hd_result.res11";
            string source_file = File.Exists(res1d) ? res1d : res11;

            string reachname;
            double chainage;
            //求距离指定点最近的河道并内插该点的桩号
            Res11.Get_reach_chainage(source_file, p, out reachname, out chainage);

            AtReach atreach = AtReach.Get_Atreach(reachname, chainage);

            //根据河名、桩号求指定项目的键值对集合
            Dictionary<DateTime, double> resdic = Res11.Res11reader(plan_code, atreach, itemtype);

            return resdic;
        }


        //查询指定河道桩号的指定项（水位或流量），返回该项的时间序列(若指定桩号不在网格点上则内插，给定结果文件)--mike11的res11、res1D结果查询
        public static Dictionary<DateTime, double> Res11reader(string plan_code, AtReach atreach, mike11_restype itemtype)
        {
            string model_dir = Model_Files.Get_Modeldir(plan_code);
            string res1d = model_dir + @"\" + "results" + @"\" + "hd_result.res1d";
            string res11 = model_dir + @"\" + "results" + @"\" + "hd_result.res11";
            string source_file = File.Exists(res1d) ? res1d : res11;

            string reachname = atreach.reachname;
            double chainage = atreach.chainage;

            string itemname = null;
            if (itemtype == mike11_restype.Water_Level)
            {
                itemname = itemtype.ToString().Replace("_", " ");
            }
            else
            {
                itemname = itemtype.ToString();
            }
            Dictionary<DateTime, double> resdic = Get_ReachItemValue(source_file, reachname, chainage, itemname);

            return resdic;
        }

        //查询指定河道桩号的指定项（水位或流量），返回该项的时间序列(若指定桩号不在网格点上则内插，给定结果数据)--mike11的res11、res1D结果查询
        public static Dictionary<DateTime, double> Res11reader(string plan_code, AtReach atreach, mike11_restype itemtype, ResultData resdata)
        {
            string reachname = atreach.reachname;
            double chainage = atreach.chainage;

            string itemname = null;
            if (itemtype == mike11_restype.Water_Level)
            {
                itemname = itemtype.ToString().Replace("_", " ");
            }
            else
            {
                itemname = itemtype.ToString();
            }

            Dictionary<DateTime, double> resdic = Get_ReachItemValue(reachname, chainage, itemname, resdata);

            return resdic;
        }

        //根据指定河道、桩号、项目获取项目值(给定结果文件)
        public static Dictionary<DateTime, double> Get_ReachItemValue(string sourcefilename, string reachname, double chainage, string itemname)
        {
            Dictionary<DateTime, double> resdic = new Dictionary<DateTime, double>();
            try
            {
                //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
                ResultData resdata = new ResultData();
                resdata.Connection = Connection.Create(sourcefilename);

                //必须得新建一个诊断对象，并给ResultData对象加载
                Diagnostics diagn = new Diagnostics();
                resdata.Load(diagn);

                DateTime[] timearray = resdata.TimesList.ToArray();

                //找到指定的河道,注意有的河道分段了，要找到正确的河道河段
                IRes1DReach reach = resdata.Reaches.ElementAt(0);
                for (int j = 0; j < resdata.Reaches.Count; j++)
                {
                    reach = resdata.Reaches.ElementAt(j);

                    //获取河道的第0个项目即水位项目的起点桩号和终点桩号
                    double reachupchainage = reach.GetChainages(0)[0];
                    double reachdownchainage = reach.GetChainages(0)[reach.GetChainages(0).Length - 1];

                    //如果河名和桩号都能对上，则找到了指定河道
                    if (reach.Name == reachname && (chainage >= reachupchainage && chainage <= reachdownchainage))
                    {
                        break;
                    }
                }

                //找到指定的项目
                int itemnumber = 0;
                IDataItem dataitem = reach.DataItems.ElementAt(0);
                for (int j = 0; j < reach.DataItems.Count; j++)
                {
                    dataitem = reach.DataItems.ElementAt(j);
                    if (string.Equals(dataitem.Quantity.Description, itemname, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    itemnumber++;
                }

                //根据桩号找到最近的水位点或流量点序号
                List<int> numberlist = Getnumberlist(reach, chainage, itemnumber);

                //返回指定点指定项目的键值对集合
                if (numberlist.Count == 1)  //不是1就是2，即在网格点上和不在网格点上
                {
                    double[] resarray = dataitem.CreateTimeSeriesData(numberlist[0]); //如 第numberlist[0] 个流量点
                    for (int j = 0; j < resarray.Length; j++)
                    {
                        resdic.Add(timearray[j], resarray[j]);
                    }
                }
                else
                {
                    //该点上下游的两个指定项目点的值数组
                    double[] resarray1 = dataitem.CreateTimeSeriesData(numberlist[0]);
                    double[] resarray2 = dataitem.CreateTimeSeriesData(numberlist[1]);

                    //获取该河流上的所有指定项目的网格节点桩号
                    double[] gridchainage = reach.GetChainages(itemnumber);
                    double chainageup = gridchainage[numberlist[0]];
                    double chainagedown = gridchainage[numberlist[1]];

                    for (int j = 0; j < resarray1.Length; j++)
                    {
                        //根据桩号内插值
                        double resvalue = resarray1[j] + (resarray2[j] - resarray1[j]) * (chainage - chainageup) / (chainagedown - chainageup);
                        resdic.Add(timearray[j], resvalue);
                    }
                }

            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return null;
            }
            return resdic;
        }

        //根据指定河道、桩号、项目获取项目值(给定结果数据)
        public static Dictionary<DateTime, double> Get_ReachItemValue(string reachname, double chainage, string itemname, ResultData resdata)
        {
            Dictionary<DateTime, double> resdic = new Dictionary<DateTime, double>();
            try
            {
                DateTime[] timearray = resdata.TimesList.ToArray();

                //找到指定的河道,注意有的河道分段了，要找到正确的河道河段
                IRes1DReach reach = resdata.Reaches.ElementAt(0);
                for (int j = 0; j < resdata.Reaches.Count; j++)
                {
                    reach = resdata.Reaches.ElementAt(j);

                    //获取河道的第0个项目即水位项目的起点桩号和终点桩号
                    double reachupchainage = reach.GetChainages(0)[0];
                    double reachdownchainage = reach.GetChainages(0)[reach.GetChainages(0).Length - 1];

                    //如果河名和桩号都能对上，则找到了指定河道
                    if (reach.Name == reachname && (chainage >= reachupchainage && chainage <= reachdownchainage))
                    {
                        break;
                    }
                }

                //找到指定的项目
                int itemnumber = 0;
                IDataItem dataitem = reach.DataItems.ElementAt(0);
                for (int j = 0; j < reach.DataItems.Count; j++)
                {
                    dataitem = reach.DataItems.ElementAt(j);
                    if (string.Equals(dataitem.Quantity.Description, itemname, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    itemnumber++;
                }

                //根据桩号找到最近的水位点或流量点序号
                List<int> numberlist = Getnumberlist(reach, chainage, itemnumber);

                //返回指定点指定项目的键值对集合
                if (numberlist.Count == 1)  //不是1就是2，即在网格点上和不在网格点上
                {
                    double[] resarray = dataitem.CreateTimeSeriesData(numberlist[0]); //如 第numberlist[0] 个流量点
                    for (int j = 0; j < resarray.Length; j++)
                    {
                        resdic.Add(timearray[j], resarray[j]);
                    }
                }
                else
                {
                    //该点上下游的两个指定项目点的值数组
                    double[] resarray1 = dataitem.CreateTimeSeriesData(numberlist[0]);
                    double[] resarray2 = dataitem.CreateTimeSeriesData(numberlist[1]);

                    //获取该河流上的所有指定项目的网格节点桩号
                    double[] gridchainage = reach.GetChainages(itemnumber);
                    double chainageup = gridchainage[numberlist[0]];
                    double chainagedown = gridchainage[numberlist[1]];

                    if(itemname != "Discharge")
                    {
                        for (int j = 0; j < resarray1.Length; j++)
                        {
                            //根据桩号内插值
                            double resvalue = resarray1[j] + (resarray2[j] - resarray1[j]) * (chainage - chainageup) / (chainagedown - chainageup);
                            resdic.Add(timearray[j], resvalue);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < resarray1.Length; j++)
                        {
                            //直接用下一个值
                            resdic.Add(timearray[j], resarray2[j]);
                        }
                    }
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return null;
            }
            return resdic;
        }

        //根据桩号找到最近的水位点或流量点的 单独序号集合(1个序号或者2个序号)
        public static List<int> Getnumberlist(IRes1DReach reach, double chainage, int itemnumber)
        {
            List<int> numberlist = new List<int>();

            //假如所提供的桩号恰好在网格点上
            if (reach.GridPointIndexForChainage(chainage) != -1)
            {
                int n = reach.GridPointIndexForChainage(chainage);
                if (n % 2 == 0 && itemnumber == 0)   //n为偶数表示水位点，且所选项目刚好是水位
                {
                    numberlist.Add(n / 2);
                    return numberlist;
                }
                else if (n % 2 != 0 && itemnumber == 1)  //n为奇数表示流量点，且所选项目刚好是流量
                {
                    numberlist.Add((n - 1) / 2);
                    return numberlist;
                }
            }

            //不在网格点的情况
            if (itemnumber == 0)       //水位
            {
                double[] gridchainage = reach.GetChainages(0);
                for (int i = 0; i < gridchainage.Length; i++)
                {
                    if (chainage > gridchainage[i] && chainage < gridchainage[i + 1])
                    {
                        numberlist.Add(i);
                        numberlist.Add(i + 1);
                        return numberlist;
                    }
                }
            }
            else if (itemnumber == 1)   //流量
            {
                double[] gridchainage = reach.GetChainages(1);
                for (int i = 0; i < gridchainage.Length; i++)
                {
                    if (chainage > gridchainage[gridchainage.Length - 1])
                    {
                        numberlist.Add(gridchainage.Length - 1);
                        return numberlist;
                    }

                    if (chainage < gridchainage[0])
                    {
                        numberlist.Add(0);
                        return numberlist;
                    }

                    if (chainage > gridchainage[i] && chainage < gridchainage[i + 1])
                    {
                        numberlist.Add(i);
                        numberlist.Add(i + 1);
                        return numberlist;
                    }
                }
            }
            return null;
        }

        //求距离给定点最近的河道名和内插桩号
        public static int Get_reach_chainage(string sourcefilename, PointXY p, out string reachname, out double chainage)
        {
            reachname = null;
            chainage = -1;

            //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(sourcefilename);

            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //定义存储各河道名河各节点距离指定点距离的嵌套键值对集合
            double mindistance = Model_Const.MIKE11_MAXPTOREACHSTANCE;
            for (int i = 0; i < resdata.Reaches.Count; i++)
            {
                //遍历各河道
                IRes1DReach reach = resdata.Reaches.ElementAt(i);
                int gridcount = reach.GridPoints.Count;

                //最后通过判断找出距离给定点最近的两个点和所在的河道，内插出桩号
                for (int j = 0; j < gridcount; j++)
                {
                    double corx = reach.GridPoints.ElementAt(j).X;
                    double cory = reach.GridPoints.ElementAt(j).Y;

                    //所给的点恰好在节点上
                    if (p.X == corx && p.Y == cory)
                    {
                        reachname = reach.Name;
                        chainage = reach.GridPoints.ElementAt(j).Chainage;
                        return 0;
                    }

                    //所给的点不在节点上的情况
                    double distance = Math.Sqrt((corx - p.X) * (corx - p.X) + (cory - p.Y) * (cory - p.Y));
                    if (distance < mindistance)
                    {
                        mindistance = distance;
                        //重新给用于输出的河道名称、桩号赋值
                        reachname = reach.Name;
                        if (j == 0 || j == gridcount - 1)
                        {
                            //直接为起点或终点的桩号
                            chainage = reach.GridPoints.ElementAt(j).Chainage;
                        }
                        else
                        {
                            IRes1DGridPoint uppoint;
                            IRes1DGridPoint downpoint;
                            double upcorx = reach.GridPoints.ElementAt(j - 1).X;
                            double upcory = reach.GridPoints.ElementAt(j - 1).Y;
                            double downcorx = reach.GridPoints.ElementAt(j + 1).X;
                            double downcory = reach.GridPoints.ElementAt(j + 1).Y;
                            double currentx = reach.GridPoints.ElementAt(j).X;
                            double currenty = reach.GridPoints.ElementAt(j).Y;

                            //求j-1点和J点线段的垂足
                            double k = (upcory - currenty) / (upcorx - currentx); //直线斜率
                            PointXY upp;
                            upp.X = upcorx;
                            upp.Y = upcory;
                            PointXY Projectpoint1 = PointXY.GetProjectivePoint(upp, k, p);  //垂足
                            double updowndistance1 = PointXY.Get_ptop_distance(currentx, currenty, upp.X, upp.Y);
                            double sumdistance1 = PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, upp.X, upp.Y) + PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, currentx, currenty);

                            //求j点和J+1点线段的垂足
                            double k1 = (downcory - currenty) / (downcorx - currentx); //直线斜率
                            PointXY downp;
                            downp.X = downcorx;
                            downp.Y = downcory;
                            PointXY Projectpoint2 = PointXY.GetProjectivePoint(downp, k1, p);  //垂足
                            double updowndistance2 = PointXY.Get_ptop_distance(currentx, currenty, downp.X, downp.Y);
                            double sumdistance2 = PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, downp.X, downp.Y) + PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, currentx, currenty);

                            //判断垂足是否在线段上，以及在哪个线段（上游线段 还是 下游线段）上
                            double upchainage = 0;
                            if (Math.Abs(sumdistance1 - updowndistance1) < 1) //垂足在j-1点和J点组成的线段之间 
                            {
                                //定义上游点为上一点，下游点为当前点
                                uppoint = reach.GridPoints.ElementAt(j - 1);
                                downpoint = reach.GridPoints.ElementAt(j);
                                upchainage = uppoint.Chainage;

                                //求垂足桩号
                                chainage = upchainage + PointXY.Get_ptop_distance(Projectpoint1.X, Projectpoint1.Y, uppoint.X, uppoint.Y);
                            }
                            else if (Math.Abs(sumdistance2 - updowndistance2) < 1)  //垂足在j点和J+1点组成的线段之间 
                            {
                                //定义上游点为当前点，下游点为下一点
                                uppoint = reach.GridPoints.ElementAt(j);
                                downpoint = reach.GridPoints.ElementAt(j + 1);
                                upchainage = uppoint.Chainage;

                                //求垂足桩号
                                chainage = upchainage + PointXY.Get_ptop_distance(Projectpoint2.X, Projectpoint2.Y, uppoint.X, uppoint.Y);
                            }
                            else   //垂足不在前、后线段内
                            {
                                //桩号直接等于当前点
                                chainage = reach.GridPoints.ElementAt(j).Chainage;
                            }

                        }
                    }

                }
            }

            return 0;
        }

        //获取最后结果时间
        public static DateTime Get_LastResTime(string plan_code)
        {
            string model_dir = Model_Files.Get_Modeldir(plan_code);
            string res1d = model_dir + @"\" + "results" + @"\" + "hd_result.res1d";
            string res11 = model_dir + @"\" + "results" + @"\" + "hd_result.res11";
            string source_file = File.Exists(res1d) ? res1d : res11;

            DateTime time = new DateTime();
            try
            {
                //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
                ResultData resdata = new ResultData();
                resdata.Connection = Connection.Create(source_file);

                //必须得新建一个诊断对象，并给ResultData对象加载
                Diagnostics diagn = new Diagnostics();
                resdata.Load(diagn);

                DateTime[] timearray = resdata.TimesList.ToArray();
                time = resdata.EndTime;

            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return time;
            }
            return time;
        }
        #endregion ***************************************************************


        #region *********************** 查询res11、RR结果 *************************
        //查询指定流域的指定项(径流流量或净雨),返回该项的时间序列 --- nam的res11结果查询1
        public static Dictionary<DateTime, double> Res11reader(HydroModel hydromodel, string catchmentname, RF_restype restype)
        {
            string sourcefilename = hydromodel.Modelfiles.Rrres11_filename;

            Dictionary<DateTime, double> resdic = new Dictionary<DateTime, double>();
            string itemname = restype.ToString();
            try
            {
                //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
                ResultData resdata = new ResultData();
                resdata.Connection = Connection.Create(sourcefilename);

                //必须得新建一个诊断对象，并给ResultData对象加载
                Diagnostics diagn = new Diagnostics();
                resdata.Load(diagn);

                //定义时间序列数组
                DateTime[] timearray = resdata.TimesList.ToArray();

                //找到指定的流域
                IRes1DCatchment catchment = resdata.Catchments.ElementAt(0);
                for (int i = 0; i < resdata.Catchments.Count; i++)
                {
                    catchment = resdata.Catchments.ElementAt(i);
                    //如果流域名能对上，则找到了指定流域
                    if (catchment.Id == catchmentname)
                    {
                        break;
                    }
                }

                //找到指定的项目
                int itemnumber = 0;
                IDataItem dataitem = catchment.DataItems.ElementAt(0);
                for (int i = 0; i < catchment.DataItems.Count; i++)
                {
                    dataitem = catchment.DataItems.ElementAt(i);
                    if (string.Equals(dataitem.Quantity.Description, itemname, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    itemnumber++;
                }

                //返回指定项目的键值对集合
                double[] resarray = dataitem.CreateTimeSeriesData(0); //相当于只有一个网格节点
                for (int i = 0; i < resarray.Length; i++)
                {
                    resdic.Add(timearray[i], resarray[i]);
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return null;
            }

            return resdic;
        }

        //查询指定流域的指定项径流流量或净雨),返回该项的统计特征值 --- nam的res11结果查询2
        public static rainfallstatist Res11reader(HydroModel hydromodel, string catchmentname)
        {
            string sourcefilename = hydromodel.Modelfiles.Rrres11_filename;

            //初始化数据
            rainfallstatist resstatist;
            resstatist.catchment_area = 0;
            resstatist.max_runoff = 0;
            resstatist.max_runoff_time = DateTime.Now;
            resstatist.runnoff_volume = 0;
            resstatist.netrainfall_h = 0;

            try
            {
                //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
                ResultData resdata = new ResultData();
                resdata.Connection = Connection.Create(sourcefilename);

                //必须得新建一个诊断对象，并给ResultData对象加载
                Diagnostics diagn = new Diagnostics();
                resdata.Load(diagn);

                //定义时间序列数组
                DateTime[] timearray = resdata.TimesList.ToArray();

                //找到指定的流域
                IRes1DCatchment catchment = resdata.Catchments.ElementAt(0);
                for (int i = 0; i < resdata.Catchments.Count; i++)
                {
                    catchment = resdata.Catchments.ElementAt(i);
                    //如果流域名能对上，则找到了指定流域
                    if (catchment.Id == catchmentname)
                    {
                        //流域面积赋值
                        resstatist.catchment_area = catchment.Area;
                        break;
                    }
                }

                //找到指定的项目
                int itemnumber = 0;
                IDataItem dataitem = catchment.DataItems.ElementAt(0);
                for (int i = 0; i < catchment.DataItems.Count; i++)
                {
                    dataitem = catchment.DataItems.ElementAt(i);
                    if (dataitem.Quantity.Description == "RunOff")
                    {
                        break;
                    }
                    itemnumber++;
                }

                //返回指定项目的键值对集合
                double[] resarray = dataitem.CreateTimeSeriesData(0);
                resstatist.max_runoff = resarray.Max();

                for (int i = 0; i < resarray.Length; i++)
                {
                    if (resarray.ElementAt(i) == resstatist.max_runoff)
                    {
                        resstatist.max_runoff_time = timearray[i];
                        break;
                    }
                }

                //用平均流量计算求总径流量(m3)
                resstatist.runnoff_volume = resarray.Average() * resdata.EndTime.Subtract(resdata.StartTime).TotalSeconds;

                //用总径流量/面积得净雨深(m)
                resstatist.netrainfall_h = resstatist.runnoff_volume / resstatist.catchment_area;
                return resstatist;
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return resstatist;
            }
        }

        //获取指定流域的XAJ模拟的模拟结果
        public static Dictionary<DateTime, double> Get_XAJresdic(HydroModel hydromodel, string catchmentname)
        {
            //如果只是产汇流计算且采用的是XAJ模型，则结果文件在结果文件夹里
            string sourcefilename;
            if (hydromodel.RfPars.Rainfall_selectmodel == RFModelType.XAJ)
            {
                if (hydromodel.ModelGlobalPars.Select_model == CalculateModel.only_rr)
                {
                    //如果只是产汇流计算，则结果文件在结果文件夹里
                    sourcefilename = Path.GetDirectoryName(hydromodel.Modelfiles.XAJres_filename) + @"\" + catchmentname + "_XAJres.dfs0";
                }
                else
                {
                    //否则在边界条件引用的dfs0里
                    sourcefilename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + catchmentname + "_xaj_rl.dfs0";
                }
            }
            else
            {
                sourcefilename = null;
            }

            if (File.Exists(sourcefilename) == false)
            {
                Console.WriteLine("未找到该流域XAJ结果文件！");
                return null;
            }

            Dictionary<DateTime, double> XAJresdic = Dfs0.Dfs0_Reader_GetItemDic(sourcefilename, 1);
            return XAJresdic;
        }
        #endregion *****************************************************************
    }
}
