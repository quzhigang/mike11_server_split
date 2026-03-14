using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Mike1D.CrossSectionModule;
using DHI.Mike1D.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DHI.PFS;
using System.Threading;
using Kdbndp;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;

namespace bjd_model.Mike11
{
    //河道左右岸
    [Serializable]
    public enum ReachLR
    {
        left,
        right,
        left_right
    }

    //渠底或堤顶
    [Serializable]
    public enum DD_QD
    {
        dd,
        qd
    }

    //断面最大最小Z值
    [Serializable]
    public struct Max_Min_Z
    {
        public double min_z;
        public double max_z;
    }

    //河道某位置的高程信息 -- 左右堤顶高程、左右地面高程
    [Serializable]
    public struct ReachSection_Altitude
    {
        public double left_dd_altitude;      //左堤顶高程
        public double left_ground_altitude;   //左地面高程

        public double right_dd_altitude;      //右堤顶高程
        public double right_ground_altitude;   //右地面高程

        public double section_lowest;         //河底高程

        public static ReachSection_Altitude Get_ReachSection_Altitude(double left_dd_altitude,
            double left_ground_altitude, double right_dd_altitude, double right_ground_altitude, double section_lowest)
        {
            ReachSection_Altitude Altitude;
            Altitude.left_dd_altitude = left_dd_altitude;
            Altitude.left_ground_altitude = left_ground_altitude;
            Altitude.right_ground_altitude = right_ground_altitude;
            Altitude.right_dd_altitude = right_dd_altitude;
            Altitude.section_lowest = section_lowest;
            return Altitude;
        }
    }

    public class Xns11
    {
        #region ***************************从默认xns11文件和数据库中获取断面数据*********************************
        //从默认xns11文件中获取默认模型断面信息
        public static void GetDefault_SectionInfo(string sourcefilename, ref SectionList Sectionlist)
        {
            Dictionary<AtReach, List<PointXZS>> ReachSectionList = new Dictionary<AtReach, List<PointXZS>>();

            //先从文件中获取所有的河道的所有断面桩号集合
            Dictionary<string, List<double>> Reach_Chainagelist = Get_Reach_Chainage(sourcefilename);

            //再从文件中获取所有河道及河道的主槽滩地糙率比值集合
            Dictionary<string, double> Reach_LRHighflowList = Get_Reach_LRHighflow(sourcefilename);

            //从XNS文件中读取断面数据，逐个添加到集合
            for (int i = 0; i < Reach_Chainagelist.Count; i++)
            {
                string reachname = Reach_Chainagelist.Keys.ElementAt(i);
                Dictionary<AtReach, List<PointXZS>> reach_sectiondata = Get_ReachSectionData(sourcefilename, reachname, reachname + Model_Const.REACHID_HZ);
                for (int j = 0; j < reach_sectiondata.Count; j++)
                {
                    AtReach atreach = reach_sectiondata.ElementAt(j).Key;
                    List<PointXZS> pointxzs = reach_sectiondata.ElementAt(j).Value;
                    if (!ReachSectionList.Keys.Contains(atreach)) ReachSectionList.Add(atreach,pointxzs);
                }
            }

            //断面数据和LR赋值
            Sectionlist.ReachSectionList = ReachSectionList;
            Sectionlist.Reach_LR_Highflow = Reach_LRHighflowList;

            //从xns文件中获取 各断面的预处理数据中的 附加库容面积
            List<AtReach> sectionlist = ReachSectionList.Keys.ToList();
            Dictionary<AtReach, Dictionary<double, double>> AddStorageArea = Get_ReachSection_AddStorageArea(sourcefilename, sectionlist);
            Sectionlist.AddStorageAreaList = AddStorageArea;

            Console.WriteLine("河道断面信息初始化成功!");
        }

        //从xns文件中获取 各断面的预处理数据中的 附加库容面积
        public static Dictionary<AtReach, Dictionary<double, double>> Get_ReachSection_AddStorageArea(string sourcefilename, List<AtReach> section_chainagelist)
        {
            Dictionary<AtReach, Dictionary<double, double>> AddStorageArea = new Dictionary<AtReach, Dictionary<double, double>>();

            //获取河道名集合
            List<string> reachnamelist = new List<string>();
            for (int i = 0; i < section_chainagelist.Count; i++)
            {
                if (!reachnamelist.Contains(section_chainagelist[i].reachname))
                {
                    reachnamelist.Add(section_chainagelist[i].reachname);
                }
            }

            //根据河道名获取桩号、断面数据集合
            for (int i = 0; i < reachnamelist.Count; i++)
            {
                Dictionary<double, ICrossSection> reach_sectiondic = Get_ReachSection(sourcefilename, reachnamelist[i], reachnamelist[i] + Model_Const.REACHID_HZ);

                //根据键找值
                for (int j = 0; j < section_chainagelist.Count; j++)
                {
                    if (section_chainagelist[j].reachname == reachnamelist[i])
                    {
                        //获取断面数据
                        double chainage = section_chainagelist[j].chainage;
                        ICrossSection sectioninfo = reach_sectiondic[chainage];

                        //获取断面预处理数据中的附加水位_库容面积
                        XSBase xsbase = sectioninfo.BaseCrossSection;
                        double[] item_level = xsbase.ProcessedLevels;
                        double[] item_addstorage_area = xsbase.ProcessedAdditionalSurfaceAreas;
                        Dictionary<double, double> add_level_volume = new Dictionary<double, double>();
                        for (int k = 0; k < item_level.Length; k++)
                        {
                            if(!add_level_volume.Keys.Contains(item_level[k])) add_level_volume.Add(item_level[k], item_addstorage_area[k]);
                        }

                        //将附加库容面积加入集合
                        AtReach section_atreach = AtReach.Get_Atreach(reachnamelist[i], chainage);
                        if (!AddStorageArea.Keys.Contains(section_atreach))
                        {
                            AddStorageArea.Add(section_atreach, add_level_volume);
                        }
                    }
                }
            }

            return AddStorageArea;
        }

        //从河道断面桩号数据集合中找到与给定桩号最近的断面的桩号
        public static double Get_Nearlist_SectionChainage(double section_chainage, List<double> reachsectionlist)
        {
            double nearlist_chainage = reachsectionlist[0];
            double mindistance = Math.Abs(nearlist_chainage - section_chainage);
            for (int i = 0; i < reachsectionlist.Count; i++)
            {
                double distance = Math.Abs(reachsectionlist[i] - section_chainage);
                if (distance < mindistance)
                {
                    mindistance = distance;
                    nearlist_chainage = reachsectionlist[i];
                }
            }
            return nearlist_chainage;
        }
        #endregion *******************************************************************************************


        #region  ************************************* 更新断面文件 ***********************************
        // 提取最新的断面数据信息，更新mesh文件
        public static void Rewrite_Xns11copy_UpdateFile(HydroModel hydromodel)
        {
            string outputfilename = hydromodel.Modelfiles.Xns11_filename;
            if (!File.Exists(hydromodel.BaseModel.Modelfiles.Xns11_filename)) return;

            //如果不存在，则CoPY一份基础的,各Mesh操作时都已经更改过了文件了
            if (!File.Exists(outputfilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Xns11_filename, outputfilename);
            }

            Console.WriteLine("Xns11一维河道断面文件更新成功！");
            Console.WriteLine("");
        }

        // 提取最新的断面数据信息，更新xns11文件
        public static void Rewrite_Xns11_UpdateFile(HydroModel hydromodel)
        {
            string outputfilename = hydromodel.Modelfiles.Xns11_filename;

            //先下载空白断面文件模板
            if (File.Exists(outputfilename))
            {
                File.Delete(outputfilename);
            }
            HydroModel.Load_ModelFile_Template(outputfilename);

            //新建断面数据工厂对象
            CrossSectionDataFactory crossdatafactory = new CrossSectionDataFactory();

            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();

            //新建crossdata对象，并采用断面数据工厂对象的open方法打开文件
            CrossSectionData crossdata = crossdatafactory.Open(outputfilename, diagn);

            //获取当前模型的所有断面数据
            Dictionary<AtReach, List<PointXZS>> modelsectionlist = hydromodel.Mike11Pars.SectionList.ReachSectionList;
            if (modelsectionlist == null) return;

            //如果没有河道断面数据，则直接保存后返回
            if (modelsectionlist == null)
            {
                //保存数据
                CrossSectionDataFactory.Save(crossdata);

                Console.WriteLine("Xns11一维河道断面文件更新成功！");
                return;
            }

            //重新采用断面工厂生产断面
            for (int i = 0; i < modelsectionlist.Count; i++)
            {
                //提取断面的河道信息和断面数据
                AtReach section_reachinfo = modelsectionlist.Keys.ElementAt(i);
                List<PointXZS> section_pointlist = modelsectionlist.Values.ElementAt(i);

                //获取主槽滩地糙率比
                double lr_highflow = hydromodel.Mike11Pars.SectionList.Get_Reach_LR_Highflow(section_reachinfo.reachname);

                //获取断面的附加水库面积集合
                Dictionary<double, double> section_addstorage = hydromodel.Mike11Pars.SectionList.Get_Section_AddstorageArea(section_reachinfo);

                //调用方法生产断面
                CrossSectionLocated section = Product_Sections(section_reachinfo, section_pointlist, lr_highflow, section_addstorage);

                //添加断面 
                crossdata.Add(section);
            }

            //保存数据
            CrossSectionDataFactory.Save(crossdata);

            Console.WriteLine("Xns11一维河道断面文件更新成功！");
        }

        //生产断面--用于更新河道断面
        public static CrossSectionLocated Product_Sections(AtReach section_reachinfo, List<PointXZS> section_pointlist, double lr_highflow, Dictionary<double, double> section_addstorage)
        {
            //采用断面工厂生产断面
            CrossSectionFactory crossfactory = new CrossSectionFactory();

            //创建开放式断面
            string info = Get_ChainageName(section_reachinfo.chainage, "");

            //设置河道信息
            ZLocation zcrosslocation = new ZLocation(section_reachinfo.reachname, section_reachinfo.chainage);
            crossfactory.SetLocation(zcrosslocation);

            //设置糙率模式
            crossfactory.SetRadiusType(RadiusType.ResistanceRadius);

            //设置糙率
            FlowResistance resistance = new FlowResistance();
            resistance.ResistanceDistribution = ResistanceDistribution.Zones;   //设置糙率分布类型
            resistance.Formulation = ResistanceFormulation.Relative;           //采用相对糙率
            resistance.ResistanceLeftHighFlow = lr_highflow;                  //左河滩糙率倍数
            resistance.ResistanceRightHighFlow = lr_highflow;                 //右河滩糙率倍数
            resistance.ResistanceLowFlow = 1;                                 //主河槽糙率取固定值1
            crossfactory.SetFlowResistance(resistance);

            crossfactory.SetResistanceDistribution(ResistanceDistribution.Zones);

            //设置断面点的集合
            CrossSectionPointList pointlist = new CrossSectionPointList();
            int pointnumber = section_pointlist.Count;
            for (int i = 0; i < pointnumber; i++)
            {
                //设置断面数据点
                CrossSectionPoint point = new CrossSectionPoint(section_pointlist[i].X, section_pointlist[i].Z);

                //设置点的标记
                point.UserMarker = section_pointlist[i].sign;

                //断面依次添加点
                pointlist.Add(point);
            }
            crossfactory.SetRawPoints(pointlist);

            //在这里设置标记点后才有效，否则点的标记点将无效，则会按默认添加1 2 3点(2点为最低点，1，3为左右侧最边上的点)
            double min_z = 0;
            for (int i = 0; i < pointlist.Count; i++)
            {
                if (pointlist[i].UserMarker == 1)
                {
                    crossfactory.SetLeftLeveeBank(pointlist[i]);
                }

                if (pointlist[i].UserMarker == 2)
                {
                    crossfactory.SetLowestPoint(pointlist[i]);
                    min_z = pointlist[i].Z;
                }

                if (pointlist[i].UserMarker == 3)
                {
                    crossfactory.SetRightLeveeBank(pointlist[i]);
                }

                if (pointlist[i].UserMarker == 4)
                {
                    crossfactory.SetLeftLowFlowBank(pointlist[i] as CrossSectionPoint);
                }

                if (pointlist[i].UserMarker == 5)
                {
                    crossfactory.SetRightLowFlowBank(pointlist[i]);
                }
            }

            //根据附加水库的最大最小水位，重新设定断面的水位层级
            Max_Min_Z section_maxminz = SectionList.Get_Max_MinZ(section_pointlist);
            double servior_maxlevel = section_addstorage.Keys.ElementAt(section_addstorage.Count - 1);
            if (section_addstorage.Values.Sum() != 0 && servior_maxlevel > section_maxminz.max_z)
            {
                //按新最大Z值设置预处理数据的水位,20个等距水位
                crossfactory.SetEquidistantProcessingLevelSpecs(Model_Const.SECTION_LEVELCOUNT, min_z, servior_maxlevel);
            }
            else
            {
                //按原最大最小Z值设置预处理数据的水位,20个等距水位
                crossfactory.SetEquidistantProcessingLevelSpecs(Model_Const.SECTION_LEVELCOUNT, min_z, section_maxminz.max_z);
            }

            //提取断面工厂做好的断面
            CrossSectionLocated section = crossfactory.GetCrossSection();

            //设置TOPOID
            section.TopoID = section_reachinfo.reachid;

            //重新获取该河道所有断面
            XSBase xsbase = section.BaseCrossSection;
            xsbase.CreateAllProcessedArrays(Model_Const.SECTION_LEVELCOUNT);

            //***计算预处理数据，要想这个方法运行，需要在系统环境里添加Mike的bin的x64路径,并重启电脑***
            xsbase.Initialize();
            xsbase.CalculateProcessedData();

            //设置水深偏移值，即最小Z值
            xsbase.AdjustProcessedLevels(-section_maxminz.min_z);

            //重新设置所有的预处理各项目数据 -- 以改变某些项目数据，如增加额外的储水面积
            double[] item_level = xsbase.ProcessedLevels;
            double[] item_storagewidth = xsbase.ProcessedStorageWidths;
            double[] item_area = xsbase.ProcessedAreas;
            double[] item_radius = xsbase.ProcessedRadii;
            double[] item_resistance = xsbase.ProcessedResistanceFactors;
            for (int i = 0; i < item_resistance.Length; i++)
            {
                item_resistance[i] = 1;
            }

            //如果附加库容数量与水位层数不同，则重新按水位内插该断面的附加库容面积
            Dictionary<double, double> new_section_addstorage = new Dictionary<double, double>();
            if (item_level.Length != section_addstorage.Count)
            {
                new_section_addstorage = Get_InterpolateData(section_addstorage, item_level);
            }
            else
            {
                new_section_addstorage = section_addstorage;
            }
            double[] item_addstorage_area = new_section_addstorage.Values.ToArray();

            //根据新的附加库容面积给预处理项目数据赋值
            xsbase.SetAllProcessedValues(item_level, item_storagewidth, item_area, item_radius, item_resistance, item_addstorage_area);

            //重新设置预处理数据
            section.BaseCrossSection = xsbase;

            return section;
        }
        #endregion ************************************************************************************


        #region *************************** 从xns11中读取断面信息 ************************************
        //从断面文件xns11中获取指定河道指定断面的断面信息
        public static ICrossSection Get_ReachSectionInfo(string sourcefilename, string reachname, string reachid, double chainage)
        {
            //新建断面桩号集合
            List<double> ReachChainageList = new List<double>();

            //获取桩号、断面键值对集合
            Dictionary<double, ICrossSection> reach_infodic = Get_ReachSection(sourcefilename, reachname, reachid);

            //根据键找值
            ICrossSection sectioninfo = reach_infodic[chainage];

            return sectioninfo;
        }

        //从断面文件xns11中获取指定河道的断面桩号集合
        public static List<double> Get_ReachChainageList(string sourcefilename, string reachname, string reachid)
        {
            //新建断面桩号集合
            List<double> ReachChainageList = new List<double>();

            //获取桩号、断面键值对集合
            Dictionary<double, ICrossSection> reach_infodic = Get_ReachSection(sourcefilename, reachname, reachid);

            //循环赋值
            for (int i = 0; i < reach_infodic.Keys.Count; i++)
            {
                ReachChainageList.Add(reach_infodic.Keys.ElementAt(i));
            }

            return ReachChainageList;
        }

        //从断面文件xns11中获取指定河道的桩号、断面键值对集合
        public static Dictionary<double, ICrossSection> Get_ReachSection(string sourcefilename, string reachname, string reachtopoid)
        {
            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();

            //新建断面数据工厂对象
            CrossSectionDataFactory crossdatafactory = new CrossSectionDataFactory();

            //新建crossdata对象，并采用断面数据工厂对象的open方法打开文件
            CrossSectionData crossdata = crossdatafactory.Open(sourcefilename, diagn);

            //获取河道数据，返回一个ReachCrossSections类型的IEnumerable（枚举器），从中获得河道数量以及单个河道的所有信息，包括断面
            IEnumerable<ReachCrossSections> reachlist = crossdata.GetReachTopoIdEnumerable();

            //获取指定河道的序号
            int reachnumber = 0;
            for (int i = 0; i < reachlist.Count(); i++)
            {
                if (reachlist.ElementAt(i).ReachId == reachname)
                {
                    reachnumber = i;
                }
            }

            //获取指定河道
            ReachCrossSections reach = reachlist.ElementAt(reachnumber);

            //通过方法得到该河道的 桩号、断面键值对集合
            SortedList<double, ICrossSection> sortlist = reach.GetChainageSortedCrossSections();

            //把键值对集合转换成常用类型
            Dictionary<double, ICrossSection> reach_infodic = new Dictionary<double, ICrossSection>();
            for (int i = 0; i < sortlist.Count; i++)
            {
                reach_infodic.Add(sortlist.Keys.ElementAt(i), sortlist.Values.ElementAt(i));
            }

            return reach_infodic;
        }

        //从断面文件xns11中获取指定河道的桩号、断面数据键值对集合
        public static Dictionary<AtReach, List<PointXZS>> Get_ReachSectionData(string sourcefilename, string reachname, string reachtopoid)
        {
            Dictionary<AtReach, List<PointXZS>> section_datas = new Dictionary<AtReach, List<PointXZS>>();
            Dictionary<double, ICrossSection> reach_section = Get_ReachSection(sourcefilename, reachname, reachtopoid);
            for (int i = 0; i < reach_section.Count; i++)
            {
                AtReach atreach = AtReach.Get_Atreach(reachname,reach_section.ElementAt(i).Key);  //桩号保留2位数
                ICrossSection section = reach_section.ElementAt(i).Value;
                List<PointXZS> sectiondata = new List<PointXZS>();

                //父类直接转子类成功了。。
                XSBase bases = section.BaseCrossSection;
                XSBaseRaw xbaseraw = bases as XSBaseRaw;
                CrossSectionPointList p_list = xbaseraw.Points;
                for (int j = 0; j < p_list.Count; j++)
                {
                    PointXZS pointxzs = PointXZS.Get_PointXZS(p_list[j].X, p_list[j].Z, p_list[j].UserMarker);
                    sectiondata.Add(pointxzs);
                }

                section_datas.Add(atreach, sectiondata);
            }

            return section_datas;
        }

        //从断面文件xns11中获取所有河道以及河道的断面桩号集合
        public static Dictionary<string, List<double>> Get_Reach_Chainage(string sourcefilename)
        {
            Dictionary<string, List<double>> Reach_Chainage = new Dictionary<string, List<double>>();

            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();

            //新建断面数据工厂对象
            CrossSectionDataFactory crossdatafactory = new CrossSectionDataFactory();

            //新建crossdata对象，并采用断面数据工厂对象的open方法打开文件
            CrossSectionData crossdata = crossdatafactory.Open(sourcefilename, diagn);

            //获取河道数据，返回一个ReachCrossSections类型的IEnumerable（枚举器），从中获得河道数量以及单个河道的所有信息，包括断面
            IEnumerable<ReachCrossSections> reachlist = crossdata.GetReachTopoIdEnumerable();

            //获取所有河道和河道的断面桩号集合
            for (int i = 0; i < reachlist.Count(); i++)
            {
                ReachCrossSections reach = reachlist.ElementAt(i);
                string reachname = reach.ReachId;   //河道名

                //通过方法得到该河道的 桩号、断面键值对集合
                List<double> reachsectionlist = new List<double>();
                SortedList<double, ICrossSection> sortlist = reach.GetChainageSortedCrossSections();
                for (int j = 0; j < sortlist.Count; j++)
                {
                    reachsectionlist.Add(sortlist.Keys.ElementAt(j));
                }

                Reach_Chainage.Add(reachname, reachsectionlist);
            }

            return Reach_Chainage;
        }

        //从断面文件xns11中获取所有河道及河道的主槽滩地糙率比值
        public static Dictionary<string, double> Get_Reach_LRHighflow(string sourcefilename)
        {
            Dictionary<string, double> Reach_LRHighflow = new Dictionary<string, double>();

            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();

            //新建断面数据工厂对象
            CrossSectionDataFactory crossdatafactory = new CrossSectionDataFactory();

            //新建crossdata对象，并采用断面数据工厂对象的open方法打开文件
            CrossSectionData crossdata = crossdatafactory.Open(sourcefilename, diagn);

            //获取河道数据，返回一个ReachCrossSections类型的IEnumerable（枚举器），从中获得河道数量以及单个河道的所有信息，包括断面
            IEnumerable<ReachCrossSections> reachlist = crossdata.GetReachTopoIdEnumerable();

            //获取所有河道和河道的断面桩号集合
            for (int i = 0; i < reachlist.Count(); i++)
            {
                ReachCrossSections reach = reachlist.ElementAt(i);
                string reachname = reach.ReachId;   //河道名

                //通过方法得到该河道的 桩号、断面键值对集合
                List<double> reachsectionlist = new List<double>();
                SortedList<double, ICrossSection> sortlist = reach.GetChainageSortedCrossSections();
                ICrossSection sourcesection = sortlist.Values.ElementAt(0);
                double lr_highflow = sourcesection.BaseCrossSection.FlowResistance.ResistanceLeftHighFlow;
                Reach_LRHighflow.Add(reachname, lr_highflow);
            }

            return Reach_LRHighflow;
        }
        #endregion ***********************************************************************************


        #region  ************************* 从数据库中读取断面数据 ************************************
        ////从数据库中获取指定河道的断面桩号集合
        //public static List<double> Get_ReachChainageList(string tablename, string reachname)
        //{
        //    //定义sql语句字符串
        //    string select = "select chainage from " + tablename + " where reach_name = '" + reachname + "' group by chainage order by chainage";

        //    //获取记录
        //    List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
        //    Mysql.Get_Rows(select, out rows);

        //    //重新获取河道桩号集合
        //    List<double> ReachChainageList = new List<double>();
        //    for (int i = 0; i < rows.Count; i++)
        //    {
        //        //读取一条记录上的数据
        //        double chainage = double.Parse(rows[i].ElementAt(0).ToString());
        //        if (!ReachChainageList.Contains(chainage))
        //        {
        //            ReachChainageList.Add(chainage);
        //        }
        //    }

            
        //    return ReachChainageList;
        //}

        //从数据库中获取指定河道的断面桩号集合
        public static List<double> Get_ReachChainageList(string tablename, string reachname)
        {
            //定义sql语句字符串
            string select = "select DISTINCT ON(section_id) chainage from " + tablename + " where reach_name = '" + reachname + "' order by section_id";

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);

            //重新获取河道桩号集合
            List<double> ReachChainageList = new List<double>();
            for (int i = 0; i < rows.Count; i++)
            {
                //读取一条记录上的数据
                double chainage = double.Parse(rows[i].ElementAt(0).ToString());
                if (!ReachChainageList.Contains(chainage))
                {
                    ReachChainageList.Add(chainage);
                }
            }

            return ReachChainageList;
        }

        //从数据库中获取指定河段的断面桩号集合
        public static List<double> Get_Segment_ReachChainageList(string tablename, Reach_Segment segment_reach)
        {
            List<double> ReachChainageList = new List<double>();

            //定义sql语句字符串
            string reachname = segment_reach.reachname;
            string select = "select chainage from " + tablename + " where reach_name = '" + reachname + "' and chainage >= " + segment_reach.start_chainage + " and chainage <= " + segment_reach.end_chainage;

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);

            //重新获取断面数据
            List<PointXZS> sectiondata = new List<PointXZS>();
            for (int i = 0; i < rows.Count; i++)
            {
                //读取一条记录上的数据
                double nowchainage = double.Parse(rows[i].ElementAt(0).ToString());
                if (!ReachChainageList.Contains(nowchainage))
                {
                    ReachChainageList.Add(nowchainage);
                }
            }
            
            return ReachChainageList;
        }

        //从数据库中获取指定河道的 指定断面数据,返回断面数据点的集合
        public static List<PointXZS> Get_SectionData(string tablename, string reachname, double chainage)
        {
            List<PointXZS> sectiondata = new List<PointXZS>();

            //判断给定桩号是否属于河道断面桩号
            bool chainage_inlist = false;
            List<double> ReachChainageList = Get_ReachChainageList(tablename, reachname);
            for (int i = 0; i < ReachChainageList.Count; i++)
            {
                if (chainage == ReachChainageList[i])
                {
                    chainage_inlist = true;
                    break;
                }
            }

            if (chainage_inlist == false)
            {
                Console.WriteLine("桩号不属于河道已有断面数据的桩号，请检查桩号是否正确！");
                
                return sectiondata;
            }

            //打开数据库连接
            

            //定义sql语句字符串
            string select = "select * from " + tablename + " where reach_name = '" + reachname + "' and  chainage >= " + (chainage -1) + " and  chainage <= " + (chainage+1);

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);

            //重新获取断面数据
            for (int i = 0; i < rows.Count; i++)
            {
                //读取一条记录上的数据
                PointXZS pointdata;
                pointdata.X = double.Parse(rows[i].ElementAt(2).ToString());
                pointdata.Z = double.Parse(rows[i].ElementAt(3).ToString());
                pointdata.sign = int.Parse(rows[i].ElementAt(4).ToString());

                sectiondata.Add(pointdata);
            }

            
            return sectiondata;
        }

        //从数据库中获取指定河道的 指定断面数据,返回断面数据点的集合
        public static List<PointXZS> Get_SectionData(string reachname, double chainage)
        {
            List<PointXZS> sectiondata = new List<PointXZS>();
    
            string tablename = Mysql_GlobalVar.SECTION_TABLENAME;
            

            //定义sql语句字符串
            string select = "select * from " + tablename + " where reach_name = '" + reachname + "' and  chainage = " + chainage;

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);

            if(rows.Count != 0)
            {
                //获取断面数据
                for (int i = 0; i < rows.Count; i++)
                {
                    //读取一条记录上的数据
                    PointXZS pointdata;
                    pointdata.X = double.Parse(rows[i].ElementAt(2).ToString());
                    pointdata.Z = double.Parse(rows[i].ElementAt(3).ToString());
                    pointdata.sign = int.Parse(rows[i].ElementAt(4).ToString());

                    sectiondata.Add(pointdata);
                }
            }
            else
            {
                //找最近的断面
                List<double> section_list = Get_ReachChainageList(tablename,reachname);
                int index = File_Common.Search_Value(section_list, chainage,false);
                double chainage1 = section_list[index] -0.1;
                double chainage2 = section_list[index] + 0.1;

                select = "select * from " + tablename + " where reach_name = '" + reachname 
                    + "' and  chainage > " + chainage1 + " and  chainage < " + chainage2;
                Mysql.Get_Rows(select, out rows);

                //获取断面数据
                for (int i = 0; i < rows.Count; i++)
                {
                    //读取一条记录上的数据
                    PointXZS pointdata;
                    pointdata.X = double.Parse(rows[i].ElementAt(2).ToString());
                    pointdata.Z = double.Parse(rows[i].ElementAt(3).ToString());
                    pointdata.sign = int.Parse(rows[i].ElementAt(4).ToString());

                    sectiondata.Add(pointdata);
                }
            }
            

            return sectiondata;
        }


        //从数据库中获取指定河道的 所有断面数据,返回 河道断面信息和断面点信息 的键值对集合
        public static Dictionary<double, List<PointXZS>> Get_AllSectionData(string tablename, string reachname)
        {
            Dictionary<double, List<PointXZS>> sectiondatadic = new Dictionary<double, List<PointXZS>>();

            //判断给定桩号是否属于河道断面桩号
            List<double> ReachChainageList = Get_ReachChainageList(tablename, reachname);

            //打开数据库连接
            

            //定义sql语句字符串
            string select = "select * from " + tablename + " where reach_name = '" + reachname + "' order by chainage";

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);

            double chainage = double.Parse(rows[0].ElementAt(7).ToString());

            //重新获取断面数据
            List<PointXZS> sectiondata = new List<PointXZS>();
            for (int i = 0; i < rows.Count; i++)
            {
                //读取一条记录上的数据
                double nowchainage = double.Parse(rows[i].ElementAt(7).ToString());
                if (nowchainage != chainage)
                {
                    sectiondatadic.Add(chainage, sectiondata);

                    sectiondata = new List<PointXZS>();
                    chainage = nowchainage;
                }

                PointXZS pointdata;
                pointdata.X = double.Parse(rows[i].ElementAt(2).ToString());
                pointdata.Z = double.Parse(rows[i].ElementAt(3).ToString());
                pointdata.sign = int.Parse(rows[i].ElementAt(4).ToString());
                sectiondata.Add(pointdata);
            }
            sectiondatadic.Add(double.Parse(rows[rows.Count - 1].ElementAt(7).ToString()), sectiondata);

            
            return sectiondatadic;
        }

        //从数据库中获取指定河道的 指定河段范围内断面数据,返回 河道断面信息和断面点信息 的键值对集合
        public static Dictionary<double, List<PointXZS>> Get_SegmentSectionData(KdbndpConnection connect, string tablename, string reachname, Reach_Segment segment_reach)
        {
            Dictionary<double, List<PointXZS>> sectiondatadic = new Dictionary<double, List<PointXZS>>();

            //判断给定桩号是否属于河道断面桩号
            List<double> ReachChainageList = Get_ReachChainageList(tablename, reachname);

            //打开数据库连接
            

            //定义sql语句字符串
            string select = "select * from " + tablename + " where reach_name = '" + reachname + "' and chainage >= " + segment_reach.start_chainage + " and chainage <= " + segment_reach.end_chainage;

            //获取记录
            List<List<Object>> rows;   //集合中的每一个元素又是一个集合，且该集合是一条完整的记录，其中的每个元素对于的是字段值
            Mysql.Get_Rows(select, out rows);

            double chainage = double.Parse(rows[0].ElementAt(7).ToString());

            //重新获取断面数据
            List<PointXZS> sectiondata = new List<PointXZS>();
            for (int i = 0; i < rows.Count; i++)
            {
                //读取一条记录上的数据
                double nowchainage = double.Parse(rows[i].ElementAt(7).ToString());
                if (nowchainage != chainage)
                {
                    sectiondatadic.Add(chainage, sectiondata);

                    sectiondata = new List<PointXZS>();
                    chainage = nowchainage;
                }

                PointXZS pointdata;
                pointdata.X = double.Parse(rows[i].ElementAt(2).ToString());
                pointdata.Z = double.Parse(rows[i].ElementAt(3).ToString());
                pointdata.sign = int.Parse(rows[i].ElementAt(4).ToString());
                sectiondata.Add(pointdata);
            }
            sectiondatadic.Add(double.Parse(rows[rows.Count - 1].ElementAt(7).ToString()), sectiondata);

            
            return sectiondatadic;
        }

        //从数据库中获取指定河道断面的
        //从数据库中获取指定河道的左右堤顶高程和地面高程数据,不在断面上的内插
        public static ReachSection_Altitude Get_Altitude(AtReach atreach)
        {
            ReachSection_Altitude section_altitude;

            //采用默认的基本资料数据库和断面表格
            string tablename = Mysql_GlobalVar.SECTION_TABLENAME;
            string reachname = atreach.reachname;
            double chainage = atreach.chainage;

            double left_dd_altitude;
            double left_ground_altitude;
            double right_dd_altitude;
            double right_ground_altitude;
            double section_lowest;

            //调用方法内插左右堤顶和地面高程
            Get_Altitude(tablename, reachname, chainage, ReachLR.left, out left_dd_altitude, out left_ground_altitude, out section_lowest);
            Get_Altitude(tablename, reachname, chainage, ReachLR.right, out right_dd_altitude, out right_ground_altitude, out section_lowest);

            //左右堤顶地面高程赋值
            section_altitude.left_dd_altitude = left_dd_altitude;
            section_altitude.left_ground_altitude = left_ground_altitude;
            section_altitude.right_dd_altitude = right_dd_altitude;
            section_altitude.right_ground_altitude = right_ground_altitude;
            section_altitude.section_lowest = section_lowest;

            return section_altitude;
        }

        //从数据库中获取指定河道的左右堤顶高程和地面高程
        public static void Get_Altitude(string tablename, string reachname, double chainage, ReachLR reachlr, out double dd_altitude, out double ground_altitude, out double lowest)
        {
            //先初始化赋值
            dd_altitude = -1;
            ground_altitude = -1;
            lowest = -1;

            //获取数据库中该河道的桩号集合, 判断所给桩号是否在其中
            bool chainage_inlist = false;
            double upchainage = 0;
            double downchainage = 0;
            List<double> ReachChainageList = Get_ReachChainageList(tablename, reachname);
            if (ReachChainageList.Count == 0)
            {
                return;
            }

            //判断给定桩号是否属于河道断面桩号
            if (ReachChainageList[ReachChainageList.Count - 1] == chainage)
            {
                chainage_inlist = true;
            }

            for (int i = 0; i < ReachChainageList.Count - 1; i++)
            {
                if (chainage == ReachChainageList[i])
                {
                    chainage_inlist = true;
                    break;
                }
                else if (ReachChainageList[i] < chainage && ReachChainageList[i + 1] > chainage)
                {
                    upchainage = ReachChainageList[i];
                    downchainage = ReachChainageList[i + 1];
                }
            }


            //根据给定桩号是否属于河道断面桩号 采取相应措施
            if (chainage_inlist == true)
            {
                //获取断面数据
                List<PointXZS> sectiondata = Get_SectionData(tablename, reachname, chainage);

                //调用方法从断面数据中获取左右堤顶高程和地面高程、河底高程
                Get_LRAltitude(sectiondata, reachlr, out dd_altitude, out ground_altitude, out lowest);
            }
            else
            {
                //获取上游断面数据
                List<PointXZS> upsectiondata = Get_SectionData(tablename, reachname, upchainage);

                //获取下游断面数据
                List<PointXZS> downsectiondata = Get_SectionData(tablename, reachname, downchainage);

                //调用方法从断面数据中获取上下游左右堤顶高程和地面高程
                double up_dd_altitude;
                double up_ground_altitude;
                double up_lowest;
                Get_LRAltitude(upsectiondata, reachlr, out up_dd_altitude, out up_ground_altitude, out up_lowest);

                double down_dd_altitude;
                double down_ground_altitude;
                double down_lowest;
                Get_LRAltitude(downsectiondata, reachlr, out down_dd_altitude, out down_ground_altitude, out down_lowest);

                //实际高程通过桩号内插得到
                dd_altitude = up_dd_altitude + (chainage - upchainage) * (down_dd_altitude - up_dd_altitude) / (downchainage - upchainage);
                ground_altitude = up_ground_altitude + (chainage - upchainage) * (down_ground_altitude - up_ground_altitude) / (downchainage - upchainage);
                lowest = up_lowest + (chainage - upchainage) * (down_lowest - up_lowest) / (downchainage - upchainage);
            }

        }

        //从数据库获取的断面数据中获取左右堤顶高程和地面高程
        public static void Get_LRAltitude(List<PointXZS> sectiondata, ReachLR reachlr, out double dd_altitude, out double ground_altitude, out double lowest)
        {
            //先初始化赋值
            dd_altitude = -1;
            ground_altitude = -1;

            //求最小Z值和其X值
            double minz = sectiondata[0].Z;
            double x_minz = sectiondata[0].X;
            for (int i = 0; i < sectiondata.Count; i++)
            {
                if (sectiondata[i].Z < minz)
                {
                    minz = sectiondata[i].Z;
                    x_minz = sectiondata[i].X;
                }
            }
            lowest = minz;

            //判断断面是否有2点,以及2点所在的X
            bool sign2 = false;
            double x_sign2 = sectiondata[0].X;
            for (int i = 0; i < sectiondata.Count; i++)
            {
                if (sectiondata[i].sign == 2)
                {
                    sign2 = true;
                    x_sign2 = sectiondata[i].X;
                    lowest = sectiondata[i].Z;
                    break;
                }
            }

            //提取相应堤顶高程(1点或3点) 和地面高程(第一个点或最后一个点)
            switch (reachlr)
            {
                case ReachLR.left:
                    for (int i = 0; i < sectiondata.Count; i++)
                    {
                        if (sectiondata[i].sign == 1)
                        {
                            dd_altitude = sectiondata[i].Z;
                        }
                    }

                    //如果没有1的标记，查找失败，则按左侧最高点赋值
                    if (dd_altitude == -1)
                    {
                        double maxz = sectiondata[0].Z;
                        for (int i = 0; i < sectiondata.Count; i++)
                        {
                            if (sectiondata[i].Z > maxz) maxz = sectiondata[i].Z;

                            if (sign2 == true)
                            {
                                if (sectiondata[i].sign == 2) break;
                            }
                            else
                            {
                                if (sectiondata[i].Z == minz) break;
                            }
                        }
                        dd_altitude = maxz;
                    }

                    //地面高程取最左侧的点高程
                    ground_altitude = Get_Ground_Altitude(sectiondata, ReachLR.left,dd_altitude);

                    break;
                case ReachLR.right:
                    for (int i = 0; i < sectiondata.Count; i++)
                    {
                        if (sectiondata[i].sign == 3)
                        {
                            dd_altitude = sectiondata[i].Z;
                        }
                    }

                    //如果没有3的标记，查找失败，则按右侧最高点赋值
                    if (dd_altitude == -1)
                    {
                        double maxz = sectiondata[sectiondata.Count - 1].Z;
                        for (int i = 0; i < sectiondata.Count; i++)
                        {
                            if (sign2 == true)
                            {
                                if (sectiondata[i].X < x_sign2)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (sectiondata[i].X < x_minz)
                                {
                                    continue;
                                }
                            }

                            if (sectiondata[i].Z > maxz)
                            {
                                maxz = sectiondata[i].Z;
                            }
                        }
                        dd_altitude = maxz;
                    }

                    //地面高程取最右侧的点高程
                    ground_altitude = Get_Ground_Altitude(sectiondata, ReachLR.right, dd_altitude);
                    break;
            }
        }

        //根据给定断面，获取外围地面高程
        public static double Get_Ground_Altitude(List<PointXZS> sectiondata, ReachLR lr, double max_z)
        {
            double ground_z;
            if(lr == ReachLR.left)
            {
                ground_z = sectiondata[0].Z;

                //如果堤顶和地面高程差小于0.5m，则认为断面数据不够，只到堤顶而没有到更左侧
                if(Math.Abs(ground_z - max_z) < 0.5)
                {
                    PointXZS first_p = sectiondata[0];
                    for (int i = 1; i < sectiondata.Count ; i++)
                    {
                        PointXZS now_p = sectiondata[i];
                        double bp = Math.Abs(now_p.X - first_p.X) / Math.Abs(now_p.Z - first_p.Z);

                        //大于5则认为该点是到了平地上，而不是还在堤上
                        if (bp > 5)
                        {
                            ground_z = first_p.Z;
                            break;
                        }
                        first_p = now_p;
                    }
                }
            }
            else
            {
                ground_z = sectiondata[sectiondata.Count - 1].Z;

                //如果堤顶和地面高程差小于0.5m，则认为断面数据不够，只到堤顶而没有到更左侧
                if (Math.Abs(ground_z - max_z) < 0.5)
                {
                    PointXZS last_p = sectiondata[sectiondata.Count - 1];
                    for (int i = sectiondata.Count -2; i > -1; i--)
                    {
                        PointXZS now_p = sectiondata[i];
                        double bp = Math.Abs(now_p.X - last_p.X) / Math.Abs(now_p.Z - last_p.Z);

                        //大于5则认为该点是到了平地上，而不是还在堤上
                        if (bp > 5)
                        {
                            ground_z = now_p.Z;
                            break;
                        }
                        last_p = now_p;
                    }
                }
            }

            return ground_z;
        }
        #endregion **************************************************************************************


        #region *********** 操作断面数据 -- 修改某河段断面数据、增加新河道和其断面 *******************
        //增加新河道的第2步操作 -- 通过推求河道断面桩号，并调用GIS服务剖切新河道断面，更新河道断面集合数据
        public static void Add_NewReach_Section(ref HydroModel hydromodel, string new_reachname)
        {
            //获取新河道断面,更新河道断面集合 -- 需要借助GIS剖切断面服务
            //获取已经加入河道集合的新河道
            ReachInfo newreach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(new_reachname);

            //调用GIS服务获取河道断面数据
            Dictionary<AtReach, List<PointXZS>> SectionData = Nwk11.GetSectionData_FromGIS(hydromodel, newreach);

            //重新获取需要更新的河段范围
            Reach_Segment reachsegment;
            reachsegment.reachname = newreach.reachname;
            reachsegment.reachtopoid = newreach.reachtopoid;
            reachsegment.start_chainage = newreach.start_chainage;
            reachsegment.end_chainage = newreach.end_chainage;

            //更新河道断面集合
            Xns11.Update_ReachSection(ref hydromodel, reachsegment, ref SectionData);

            //开辟新线程更新数据库相关断面数据
            Update_SectionDb(reachsegment, SectionData);
        }

        //更新已有河段 断面数据 -- 原断面桩号保持不变，重新从DEM上剖切
        public static void Update_ReachSection_FromDEM(ref HydroModel hydromodel, Reach_Segment reach_segment)
        {
            //调用GIS服务获取河道断面数据
            Dictionary<AtReach, List<PointXZS>> SectionData = Nwk11.GetSectionData_FromGIS(hydromodel, reach_segment);

            //更新河道断面集合
            Xns11.Update_ReachSection(ref hydromodel, reach_segment, ref SectionData);

            //开辟新线程更新数据库相关断面数据
            Update_SectionDb(reach_segment, SectionData);
        }

        //更新已有河道断面数据  reachsegment---要更新的河段   reachsectiondata--用于更新的河道断面数据
        //如果更新的河段原来不存在，则属于新增河道
        public static void Update_ReachSection(ref HydroModel hydromodel, Reach_Segment reachsegment, ref Dictionary<AtReach, List<PointXZS>> reachsectiondata)
        {
            //先获取指定河道的所有断面数据
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            Dictionary<AtReach, List<PointXZS>> ReachSectionDataList = hydromodel.Mike11Pars.SectionList.ReachSectionList;
            Dictionary<AtReach, Dictionary<double, double>> AddStorageAreaList = hydromodel.Mike11Pars.SectionList.AddStorageAreaList;

            List<double> ReachSectionList = hydromodel.Mike11Pars.SectionList.Get_Reach_SecChainageList(reachsegment.reachname);
            ReachSectionList.Sort();  //排下序稳妥点

            //获取原河道断面的滩地/主河槽 糙率倍数
            double lr_highflow = hydromodel.Mike11Pars.SectionList.Get_Reach_LR_Highflow(reachsegment.reachname);

            //将需要更新的河段范围内的断面删除
            List<double> oldsectionchainage_in_update = new List<double>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                double sectionchainage = ReachSectionList[i];
                AtReach atreach = AtReach.Get_Atreach(reachsegment.reachname, sectionchainage);
                if ((sectionchainage >= reachsegment.start_chainage && sectionchainage <= reachsegment.end_chainage)
                    || Math.Abs(sectionchainage - reachsegment.start_chainage) < Model_Const.MIKE11_MINDX
                    || Math.Abs(sectionchainage - reachsegment.end_chainage) < Model_Const.MIKE11_MINDX)
                {
                    ReachSectionDataList.Remove(atreach);         //减少断面
                    sectionlist.Remove_SectionAddstorageArea(atreach); //减少相应断面的附加库容
                    oldsectionchainage_in_update.Add(sectionchainage);
                }
            }

            //如果是全新河道，则相应增加主槽滩地糙率比集合
            if (ReachSectionList.Count == 0)
            {
                Dictionary<string, double> Reach_LR_Highflow = hydromodel.Mike11Pars.SectionList.Reach_LR_Highflow;
                Reach_LR_Highflow.Add(reachsegment.reachname, lr_highflow);
            }

            //预先处理断面数据，去除过密的断面和河道范围外的断面，补全首尾断面
            ProcessSectionData(reachsegment, ref reachsectiondata);

            //增加断面数据 
            for (int i = 0; i < reachsectiondata.Count; i++)
            {
                ReachSectionDataList.Add(reachsectiondata.Keys.ElementAt(i), reachsectiondata.Values.ElementAt(i));

                //增加断面的附加库容
                List<PointXZS> section_data = reachsectiondata.Values.ElementAt(i);
                double min_level = section_data[0].Z;
                double max_level = section_data[0].Z;
                for (int j = 0; j < section_data.Count; j++)
                {
                    if (section_data[j].Z < min_level)
                    {
                        min_level = section_data[j].Z;
                    }

                    if (section_data[j].Z > max_level)
                    {
                        max_level = section_data[j].Z;
                    }
                }
                Dictionary<double, double> new_level_volume = Reservoir.Get_Default_level_volume(min_level, max_level);
                AddStorageAreaList.Add(reachsectiondata.Keys.ElementAt(i), new_level_volume);
            }

            //河道开始和末端断面没有了，则用最近的代替
            Update_Reach_StartEndSection(ref hydromodel, reachsegment);

            //重新按河道按桩号大小排序
            Dictionary<AtReach, List<PointXZS>> New_ReachSectionDataList = ReachSectionDataList.OrderBy(p => p.Key.chainage).ToDictionary(p => p.Key, p => p.Value);  //按桩号大小排序
            Dictionary<AtReach, List<PointXZS>> New_ReachSectionDataList1 = New_ReachSectionDataList.OrderBy(p => p.Key.reachname).ToDictionary(p => p.Key, p => p.Value);   //按河道名排序
            ReachSectionDataList = New_ReachSectionDataList1;

            Dictionary<AtReach, Dictionary<double, double>> New_AddStorageAreaList = AddStorageAreaList.OrderBy(p => p.Key.chainage).ToDictionary(p => p.Key, p => p.Value);  //按桩号大小排序
            Dictionary<AtReach, Dictionary<double, double>> New_AddStorageAreaList1 = New_AddStorageAreaList.OrderBy(p => p.Key.reachname).ToDictionary(p => p.Key, p => p.Value);   //按河道名排序
            AddStorageAreaList = New_AddStorageAreaList1;

            //更新河道计算点
            Update_ReachGrid(ref hydromodel, reachsegment, oldsectionchainage_in_update, reachsectiondata);
        }

        //如果河道的开始和结束断面没有了，则用最近的代替
        public static void Update_Reach_StartEndSection(ref HydroModel hydromodel, Reach_Segment reachsegment)
        {
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            Dictionary<AtReach, List<PointXZS>> ReachSectionDataList = hydromodel.Mike11Pars.SectionList.ReachSectionList;
            Dictionary<AtReach, Dictionary<double, double>> AddStorageAreaList = hydromodel.Mike11Pars.SectionList.AddStorageAreaList;

            //河道开始和末端断面
            AtReach start_section = AtReach.Get_Atreach(reachsegment.reachname, hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachsegment.reachname).start_chainage);
            AtReach end_section = AtReach.Get_Atreach(reachsegment.reachname, hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachsegment.reachname).end_chainage);

            //如果断面更新后没有了始止断面
            List<double> ReachSectionList1 = hydromodel.Mike11Pars.SectionList.Get_Reach_SecChainageList(reachsegment.reachname);
            if (!ReachSectionList1.Contains(start_section.chainage))
            {
                //找到距离该断面最近的横断面
                AtReach nearsection = sectionlist.Get_Near_SectionChainage(start_section);
                Dictionary<double, double> nearsection_lv = sectionlist.Get_Section_AddstorageArea(nearsection);

                //将该断面作为起始断面添加进去
                ReachSectionDataList.Add(start_section, ReachSectionDataList[nearsection]);

                AddStorageAreaList.Add(start_section, nearsection_lv);
            }

            if (!ReachSectionList1.Contains(end_section.chainage))
            {
                //找到距离该断面最近的横断面
                AtReach nearsection = sectionlist.Get_Near_SectionChainage(end_section);
                Dictionary<double, double> nearsection_lv = sectionlist.Get_Section_AddstorageArea(nearsection);

                //将该断面作为最终断面添加进去
                ReachSectionDataList.Add(end_section, ReachSectionDataList[nearsection]);
                AddStorageAreaList.Add(end_section, nearsection_lv);
            }
        }

        //更新河道计算点
        public static void Update_ReachGrid(ref HydroModel hydromodel, Reach_Segment reachsegment, List<double> oldsectionchainage_in_update, Dictionary<AtReach, List<PointXZS>> reachsectiondata)
        {
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;
            List<double> chainagelist = reachlist.Get_ReachGridChainage(reachsegment.reachname);
            List<AtReach> newsection_reachlist = reachsectiondata.Keys.ToList();

            //获取更新河道段范围内的建筑物和入流、出流点桩号
            List<double> strchainagelist = new List<double>();
            for (int i = 0; i < chainagelist.Count; i++)
            {
                if (!oldsectionchainage_in_update.Contains(chainagelist[i]))
                {
                    strchainagelist.Add(chainagelist[i]);
                }
            }

            //将更新河道段内的计算点桩号清除(入流、出流点和建筑物计算点桩号除外)
            double newstart_chainage = newsection_reachlist[0].chainage;
            double newend_chainage = newsection_reachlist[newsection_reachlist.Count - 1].chainage;
            for (int i = 0; i < chainagelist.Count; i++)
            {
                if ((chainagelist[i] > newstart_chainage && chainagelist[i] > newend_chainage) && !strchainagelist.Contains(chainagelist[i]))
                {
                    reachlist.Remove_ExistReach_GridChainage(reachsegment.reachname, chainagelist[i]);
                }
            }

            //增加新断面桩号 --- 增加方法会自动排序
            for (int i = 0; i < newsection_reachlist.Count; i++)
            {
                reachlist.Add_ExistReach_GridChainage(reachsegment.reachname, newsection_reachlist[i].chainage);
            }
        }


        //开辟新线程更新数据库相关断面数据
        public static void Update_SectionDb(Reach_Segment reachsegment, Dictionary<AtReach, List<PointXZS>> reachsectiondata)
        {
            //获取数据库中该河段内的断面桩号集合
            List<double> segment_chainage = Get_Segment_ReachChainageList(Mysql_GlobalVar.SECTION_TABLENAME, reachsegment);

            //非全新河道则开辟新线程删除数据库中该河段的断面数据
            if (segment_chainage.Count != 0)   //非全新河道
            {
                Thread th1 = new Thread(Delete_SectionDB);
                th1.IsBackground = true;
                List<AtReach> old_sectiondic = new List<AtReach>();
                for (int i = 0; i < segment_chainage.Count; i++)
                {
                    AtReach atreach;
                    atreach.reachname = reachsegment.reachname;
                    atreach.chainage = segment_chainage[i];
                    atreach.reachid = atreach.reachname + Model_Const.REACHID_HZ;
                    old_sectiondic.Add(atreach);
                }
                th1.Start(old_sectiondic);
            }

            //开辟新线程插入数据库相关断面数据
            Thread th2 = new Thread(Insert_SectionDB);
            th2.IsBackground = true;
            th2.Start(reachsectiondata);
        }

        //子线程程序 -- 删除指定断面数据
        public static void Delete_SectionDB(object pars)
        {
            //转换参数
            List<AtReach> sectionchainagelist = (List<AtReach>)pars;

            Delete_sectiondata_frommysql(Mysql_GlobalVar.SECTION_TABLENAME, sectionchainagelist);

            Console.WriteLine("断面数据库--断面数据删除完成!");
        }

        //子线程程序 -- 插入断面数据
        public static void Insert_SectionDB(object pars)
        {
            Thread.Sleep(2000);
            Dictionary<AtReach, List<PointXZS>> reachsectiondata = (Dictionary<AtReach, List<PointXZS>>)pars;

            Write_sectiondata_intomysql(Mysql_GlobalVar.SECTION_TABLENAME, reachsectiondata);

            Console.WriteLine("断面数据库--断面数据更新完成!");
        }


        //自动计算所选河道下边界H~Q关系，返回水位流量关系键值对集合  *************直接添加的断面没有求后处理值将查询不到ZMin等值**************
        public static Dictionary<double, double> Get_ReachEnd_QH(string sourcefilename, string reachname, string reachtopoid)
        {
            Dictionary<double, double> reachqh = new Dictionary<double, double>();

            //获取该河道所有断面
            Dictionary<double, ICrossSection> reachchainagelist = Get_ReachSection(sourcefilename, reachname, reachtopoid);

            //推算纵坡
            List<double> section_minz = new List<double>();
            double slop = 1.0 / 50000.0;   //默认坡度
            for (int i = 0; i < reachchainagelist.Count; i++)
            {
                double minz = reachchainagelist.Values.ElementAt(i).BaseCrossSection.ProcessedLevels[0];
                //  Console.WriteLine(minz);
                section_minz.Add(minz);
            }

            if (section_minz[0] > section_minz[section_minz.Count - 1])
            {
                double distance = Math.Abs(reachchainagelist.Keys.ElementAt(reachchainagelist.Count - 1) - reachchainagelist.Keys.ElementAt(0));
                slop = (section_minz[0] - section_minz[section_minz.Count - 1]) / distance;
            }

            //  Console.WriteLine("纵坡为1/{0:0}", 1 / slop);

            //获取河道最后一个断面处理后的水力信息 -- 水位、过流面积、水力半径
            ICrossSection endsection = reachchainagelist.Values.ElementAt(reachchainagelist.Count - 1);

            double[] level = endsection.BaseCrossSection.ProcessedLevels;
            double[] area = endsection.BaseCrossSection.ProcessedFlowAreas;
            double[] radius = endsection.BaseCrossSection.ProcessedRadii;

            //设置糙率为默认河道糙率
            double Resistance = Model_Const.DEFAULT_RESISTANCE;

            //计算水位流量关系
            double[] q = new double[level.Length];
            for (int i = 0; i < level.Length; i++)
            {
                double nowq = (area[i] * Math.Pow(radius[i], 2.0 / 3.0) / Resistance) * Math.Sqrt(slop);
                q[i] = nowq;
            }

            //在最下面和最上面各添加一个水位流量关系点，以保证能包住
            double downlevel = level[0] - 2;
            double downq = 0;

            int end1 = q.Length - 1;
            int end2 = q.Length - 2;
            double uplevel = level[end1] + 10;
            double upq = q[end2] + (q[end1] - q[end2]) * (uplevel - level[end2]) / (level[end1] - level[end2]);

            //添加到水位流量关系集合
            reachqh.Add(downlevel, downq);
            for (int i = 0; i < level.Length; i++)
            {
                reachqh.Add(level[i], q[i]);
            }
            reachqh.Add(uplevel, upq);

            return reachqh;
        }

        //自动计算给定断面Q~H关系，返回流量水位关系键值对集合  *************直接添加的断面没有求后处理值将查询不到ZMin等值**************
        public static List<double[]> Get_ReachSection_QH(HydroModel model, AtReach at_reach)
        {
            List<double[]> reachqh = new List<double[]>();

            //获取该河道所有断面
            Dictionary<double, ICrossSection> reachchainagelist = Get_ReachSection(model.Modelfiles.Xns11_filename, at_reach.reachname, at_reach.reachid);

            //推算断面附近的河段平均纵坡
            List<double> section_minz = new List<double>();
            List<double> section_chainage = reachchainagelist.Keys.ToList();
            double slop = 1.0 / 50000.0;   //默认坡度
            for (int i = 0; i < reachchainagelist.Count; i++)
            {
                double minz = reachchainagelist.Values.ElementAt(i).BaseCrossSection.ProcessedLevels[0];
                section_minz.Add(minz);
            }

            //用上一个断面和下一个断面的坡度计算
            int last_index = File_Common.Search_Value(section_chainage, at_reach.chainage, true);
            if (at_reach.chainage == reachchainagelist.Keys.Last())
            {
                double distance = Math.Abs(reachchainagelist.Keys.Last() - reachchainagelist.Keys.ElementAt(reachchainagelist.Count - 2));
                slop = (section_minz[reachchainagelist.Count - 2] - section_minz[reachchainagelist.Count - 1]) / distance;
            }
            else if (last_index + 2 <= section_minz.Count - 1)
            {
                if (section_minz[last_index] > section_minz[last_index + 2])
                {
                    double distance = Math.Abs(reachchainagelist.Keys.ElementAt(last_index + 2) - reachchainagelist.Keys.ElementAt(last_index));
                    slop = (section_minz[last_index] - section_minz[last_index + 2]) / distance;
                }
            }

            //获取河道指定断面的水力信息 -- 水位、过流面积、水力半径
            ICrossSection section_ys = last_index == reachchainagelist.Count - 1 ? reachchainagelist.Values.ElementAt(last_index) : reachchainagelist.Values.ElementAt(last_index + 1);

            double[] level = section_ys.BaseCrossSection.ProcessedLevels;
            double[] area = section_ys.BaseCrossSection.ProcessedFlowAreas;
            double[] radius = section_ys.BaseCrossSection.ProcessedRadii;

            //设置糙率为默认河道糙率
            double Resistance = Model_Const.DEFAULT_RESISTANCE;

            //计算水位流量关系
            double[] q = new double[level.Length];
            for (int i = 0; i < level.Length; i++)
            {
                double nowq = (area[i] * Math.Pow(radius[i], 2.0 / 3.0) / Resistance) * Math.Sqrt(slop);
                q[i] = nowq;
            }

            //在最下面和最上面各添加一个水位流量关系点，以保证能包住
            double downlevel = level[0] - 2;
            double downq = 0;
            double[] down_qh = new double[] { Math.Round(downq, 2), Math.Round(downlevel, 2) };

            int end1 = q.Length - 1;
            int end2 = q.Length - 2;
            double uplevel = level[end1] + 10;
            double upq = q[end2] + (q[end1] - q[end2]) * (uplevel - level[end2]) / (level[end1] - level[end2]);
            double[] up_qh = new double[] { Math.Round(upq, 2), Math.Round(uplevel, 2) };

            //添加到水位流量关系集合
            reachqh.Add(down_qh);
            for (int i = 0; i < level.Length; i++)
            {
                double[] section_qh = new double[] { Math.Round(q[i], 2), Math.Round(level[i], 2) };
                reachqh.Add(section_qh);
            }
            reachqh.Add(up_qh);

            return reachqh;
        }


        //预先处理断面数据，去除过密的断面和河道范围外的断面，补全首尾断面
        public static void ProcessSectionData(Reach_Segment reachinfo, ref Dictionary<AtReach, List<PointXZS>> reachsectiondata)
        {
            //判断是否有断面过密
            List<int> near_number = new List<int>();
            for (int i = 0; i < reachsectiondata.Count - 1; i++)
            {
                double upchainage = reachsectiondata.Keys.ElementAt(i).chainage;
                double downchainage = reachsectiondata.Keys.ElementAt(i + 1).chainage;
                if (Math.Abs(downchainage - upchainage) < Model_Const.MIKE11_MINDX)   //如果两个断面间距小于最小永许步长
                {
                    near_number.Add(i + 1);
                }
            }

            //移除过密断面
            for (int i = 0; i < near_number.Count; i++)
            {
                reachsectiondata.Remove(reachsectiondata.Keys.ElementAt(near_number[i]));
            }

            //如果首尾断面完全相同，则直接返回
            if (reachsectiondata.Keys.ElementAt(0).chainage == reachinfo.start_chainage && reachsectiondata.Keys.ElementAt(reachsectiondata.Count - 1).chainage == reachinfo.end_chainage)
            {
                return;
            }

            //首尾有不相同的情况

            //前面多余断面处理
            for (int i = 0; i < reachsectiondata.Count - 1; i++)
            {
                double upchainage = reachsectiondata.Keys.ElementAt(i).chainage;
                double downchainage = reachsectiondata.Keys.ElementAt(i + 1).chainage;
                if (upchainage <= reachinfo.start_chainage && downchainage > reachinfo.start_chainage)
                {
                    //新断面数据的第一个断面取上游断面
                    AtReach startreach = reachsectiondata.Keys.ElementAt(i);
                    startreach.chainage = reachinfo.start_chainage;
                    List<PointXZS> startpointlist = reachsectiondata.Values.ElementAt(i);

                    //在新断面数据中添加第一个断面
                    Dictionary<AtReach, List<PointXZS>> newreachsectiondata = new Dictionary<AtReach, List<PointXZS>>();
                    newreachsectiondata.Add(startreach, startpointlist);

                    //在新断面数据中添加剩余的断面
                    for (int j = i + 1; j < reachsectiondata.Count; j++)
                    {
                        newreachsectiondata.Add(reachsectiondata.Keys.ElementAt(j), reachsectiondata.Values.ElementAt(j));
                    }

                    //原断面数据清空
                    for (int j = 0; j < reachsectiondata.Count; j++)
                    {
                        reachsectiondata.Remove(reachsectiondata.Keys.ElementAt(j));
                    }

                    //原断面数据重新赋值为新断面数据
                    for (int j = 0; j < newreachsectiondata.Count; j++)
                    {
                        reachsectiondata.Add(newreachsectiondata.Keys.ElementAt(j), newreachsectiondata.Values.ElementAt(j));
                    }
                }
            }

            //前面断面还不够的情况
            if (reachsectiondata.Keys.ElementAt(0).chainage != reachinfo.start_chainage)
            {
                //新断面数据的第一个断面取上游断面
                AtReach startreach = reachsectiondata.Keys.ElementAt(0);
                startreach.chainage = reachinfo.start_chainage;
                List<PointXZS> startpointlist = reachsectiondata.Values.ElementAt(0);

                //在新断面数据中添加第一个断面
                Dictionary<AtReach, List<PointXZS>> newreachsectiondata = new Dictionary<AtReach, List<PointXZS>>();
                newreachsectiondata.Add(startreach, startpointlist);

                //在新断面数据中添加剩余的断面
                for (int j = 0; j < reachsectiondata.Count; j++)
                {
                    newreachsectiondata.Add(reachsectiondata.Keys.ElementAt(j), reachsectiondata.Values.ElementAt(j));
                }

                //原断面数据清空
                for (int j = 0; j < reachsectiondata.Count; j++)
                {
                    reachsectiondata.Remove(reachsectiondata.Keys.ElementAt(j));
                }

                //原断面数据重新赋值为新断面数据
                for (int j = 0; j < newreachsectiondata.Count; j++)
                {
                    reachsectiondata.Add(newreachsectiondata.Keys.ElementAt(j), newreachsectiondata.Values.ElementAt(j));
                }
            }

            //后面多余断面处理
            for (int i = 0; i < reachsectiondata.Count - 1; i++)
            {
                double upchainage = reachsectiondata.Keys.ElementAt(i).chainage;
                double downchainage = reachsectiondata.Keys.ElementAt(i + 1).chainage;

                if (upchainage < reachinfo.end_chainage && downchainage >= reachinfo.end_chainage)
                {
                    //新断面数据的最后一个断面取上游断面
                    AtReach endreach = reachsectiondata.Keys.ElementAt(i + 1);
                    endreach.chainage = reachinfo.end_chainage;
                    List<PointXZS> endpointlist = reachsectiondata.Values.ElementAt(i + 1);

                    //原断面数据后面的断面清空
                    for (int j = i + 1; j < reachsectiondata.Count; j++)
                    {
                        reachsectiondata.Remove(reachsectiondata.Keys.ElementAt(j));
                    }

                    //原断面数据后面添加新的最后一个断面
                    reachsectiondata.Add(endreach, endpointlist);
                }
            }

            //后面断面还不够的情况
            if (reachsectiondata.Keys.ElementAt(reachsectiondata.Count - 1).chainage != reachinfo.end_chainage)
            {
                //断面数据的最后一个断面取最后一个断面
                AtReach endreach = reachsectiondata.Keys.ElementAt(reachsectiondata.Count);
                endreach.chainage = reachinfo.end_chainage;
                List<PointXZS> endpointlist = reachsectiondata.Values.ElementAt(reachsectiondata.Count);
                reachsectiondata.Add(endreach, endpointlist);
            }

        }

        //数据转桩号
        public static string Get_ChainageName(double chainage, string zhname)
        {
            string ChainageName = null;
            double number = chainage / 1000;
            double end = chainage - Math.Truncate(number) * 1000;
            ChainageName = zhname + Math.Truncate(number).ToString() + "+" + end.ToString("000.0");
            return ChainageName;
        }
        #endregion ***********************************************************************************


        #region ******************** 将水库与一维河道连接 -- 水库库容挂在河道相应断面上 **************
        //将水库与一维河道连接 -- 1个连接点
        public static void Connect_Reservoir_OnSection(HydroModel hydromodel, Dictionary<double, double> Level_Volume, AtReach section, bool substract_sectionvolume)
        {
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //根据给定点获取最近的河道断面
            Reach_Segment reachsegment = Reach_Segment.Get_ReachSegment(section.reachname, section.chainage, section.chainage);

            //新建水库对象
            Reservoir reservoir = new Reservoir(Level_Volume, reachsegment);

            //将水库与一维河道连接，库容挂在一维相应河道断面上
            Add_StorageArea_OnSection(hydromodel, reservoir, substract_sectionvolume);
        }

        //将水库与一维河道连接 -- 1个连接点
        public static void Connect_Reservoir_OnSection(HydroModel hydromodel, Dictionary<double, double> Level_Volume, PointXY p, bool substract_sectionvolume)
        {
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //根据给定点获取最近的河道断面
            AtReach near_section = Nwk11.Get_NearReach_Sectionchainage(sectionlist, reachlist, p);
            Reach_Segment reachsegment = Reach_Segment.Get_ReachSegment(near_section.reachname, near_section.chainage, near_section.chainage);

            //新建水库对象
            Reservoir reservoir = new Reservoir(Level_Volume, reachsegment);

            //将水库与一维河道连接，库容挂在一维相应河道断面上
            Add_StorageArea_OnSection(hydromodel, reservoir, substract_sectionvolume);
        }

        //将水库与一维河道连接 -- 2个连接点
        public static void Connect_Reservoir_OnSection(HydroModel hydromodel, Dictionary<double, double> Level_Volume, PointXY p1, PointXY p2, bool substract_sectionvolume)
        {
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //求距离给定点最近的河道名和内插桩号
            AtReach atreach_p1 = Nwk11.Get_NearReach(reachlist, p1);
            AtReach atreach_p2 = Nwk11.Get_NearReach(reachlist, p2);
            Reach_Segment reachsegment;
            if (atreach_p1.chainage < atreach_p2.chainage)
            {
                reachsegment = Reach_Segment.Get_ReachSegment(atreach_p1.reachname, atreach_p1.chainage, atreach_p2.chainage);
            }
            else
            {
                reachsegment = Reach_Segment.Get_ReachSegment(atreach_p1.reachname, atreach_p2.chainage, atreach_p1.chainage);
            }

            //新建水库对象
            Reservoir reservoir = new Reservoir(Level_Volume, reachsegment);

            //将水库与一维河道连接，库容挂在一维相应河道断面上
            Add_StorageArea_OnSection(hydromodel, reservoir, substract_sectionvolume);
        }

        //将水库与一维河道连接，库容挂在一维相应河道断面上
        public static void Add_StorageArea_OnSection(HydroModel hydromodel, Reservoir reservoir, bool substract_sectionvolume)
        {
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;

            //获取水库所在的河段
            Reach_Segment reach_segment = reservoir.Atreach_Segment;

            //获取附加库容和面积
            Dictionary<double, double> Add_StorgeArea = Get_AddStorgeArea(hydromodel, reach_segment, reservoir.Level_Volume, substract_sectionvolume);

            //在相应断面预处理数据上增加附加库容面积
            sectionlist.Addstorage_OnSection(reach_segment, Add_StorgeArea);
        }

        //计算附加库容和面积
        public static Dictionary<double, double> Get_AddStorgeArea(HydroModel hydromodel, Reach_Segment reach_segment, Dictionary<double, double> level_volume, bool substract_sectionvolume)
        {
            string sourcefilename = hydromodel.Modelfiles.Xns11_filename;
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            Dictionary<double, double> Add_StorgeArea = new Dictionary<double, double>();

            //获取附加库容断面有效距离
            double section_length = Get_Section_Distance(hydromodel, ref reach_segment);

            //找到距离河段中心点桩号最近的断面
            AtReach segment_center = AtReach.Get_Atreach(reach_segment.reachname, (reach_segment.start_chainage + reach_segment.end_chainage) / 2);
            AtReach near_atreach = sectionlist.Get_Near_SectionChainage(segment_center);

            //根据断面位置获取河道断面预处理数据
            ICrossSection section = Get_ReachSectionInfo(sourcefilename, near_atreach.reachname, near_atreach.reachid, near_atreach.chainage);
            XSBase xsbase = section.BaseCrossSection;

            //获取所有的预处理各项目数据 
            double[] item_level = xsbase.ProcessedLevels;
            double[] item_area = xsbase.ProcessedAreas;
            double[] item_width = xsbase.ProcessedStorageWidths;

            //判断库容曲线的顶底和河道断面的顶底，重新生成新的河道水位数组
            List<double> new_item_level = new List<double>();
            List<double> new_item_area = new List<double>();
            new_item_level.AddRange(item_level);
            new_item_area.AddRange(item_area);
            double reservoir_maxlevel = level_volume.Keys.ElementAt(level_volume.Count - 1);
            double section_maxlevel = item_level[item_level.Length - 1];
            double section_maxarea = item_area[item_level.Length - 1];
            double section_maxwidth = item_width[item_level.Length - 1];
            if (reservoir_maxlevel > section_maxlevel)
            {
                //重新按新范围划分水位层级
                double[] newsection_level = Reservoir.Get_Default_level_volume(item_level[0], reservoir_maxlevel).Keys.ToArray();

                //断面水位和面积加长
                for (int i = 0; i < level_volume.Count; i++)
                {
                    if (level_volume.Keys.ElementAt(i) > section_maxlevel)
                    {
                        double add_waterdepth = level_volume.Keys.ElementAt(i) - section_maxlevel;
                        new_item_level.Add(level_volume.Keys.ElementAt(i));
                        new_item_area.Add(section_maxarea + add_waterdepth * section_maxwidth);
                    }
                }
            }

            //将库容曲线按预处理水位内插，得到新水位层级的库容曲线
            Dictionary<double, double> new_level_volume = Get_InterpolateData(level_volume, new_item_level.ToArray());

            //计算断面占用库容
            Dictionary<double, double> section_level_volume = new Dictionary<double, double>();
            for (int i = 0; i < new_item_level.Count; i++)
            {
                double section_level = new_item_level[i];
                double section_volume;
                if (substract_sectionvolume == true)  //附加库容 = 水库库容 - 断面占用库容
                {
                    if (i == 0)
                    {
                        section_volume = 0;
                    }
                    else
                    {
                        section_volume = (new_item_area[i] + new_item_area[i - 1]) * section_length / 2;
                    }
                }
                else  //附加库容直接等于水库库容，不核减
                {
                    section_volume = 0;
                }
                section_level_volume.Add(section_level, section_volume);
            }

            //计算附加库容对应的各水位面积
            Dictionary<double, double> level_averge_area = new Dictionary<double, double>();
            for (int i = 0; i < new_item_level.Count - 1; i++)
            {
                //剩余附加库容
                double section_add_volume = Math.Abs(section_level_volume.Values.ElementAt(i + 1) - section_level_volume.Values.ElementAt(i));
                double reservoir_add_volume = Math.Abs(new_level_volume.Values.ElementAt(i + 1) - new_level_volume.Values.ElementAt(i));
                double add_volume = Math.Max(0, reservoir_add_volume - section_add_volume);

                //换算成附加面积 -- 第1个水位附加面积和第2个相同
                double water_depth = new_item_level[i + 1] - new_item_level[i];
                double add_averge_area = add_volume / water_depth;

                //第1个水位附加面积和第2个相同
                if (i == 0)
                {
                    level_averge_area.Add(new_item_level[i], add_averge_area);
                }
                level_averge_area.Add(new_item_level[i + 1], add_averge_area);
            }

            //根据每层的平均面积推算每层上下水位面积
            Dictionary<double, double> level_area1 = new Dictionary<double, double>();
            for (int i = 0; i < level_averge_area.Count - 1; i++)
            {
                level_area1.Add(level_averge_area.Keys.ElementAt(i), level_averge_area.Values.ElementAt(i + 1));
            }
            level_area1.Add(level_averge_area.Keys.ElementAt(level_averge_area.Count - 1), level_averge_area.Values.ElementAt(level_averge_area.Count - 1));

            //重新平均错动的两个水位
            for (int i = 0; i < level_averge_area.Count; i++)
            {
                double levelarea = (level_averge_area.Values.ElementAt(i) + level_area1.Values.ElementAt(i)) / 2;
                Add_StorgeArea.Add(level_averge_area.Keys.ElementAt(i), levelarea);
            }

            return Add_StorgeArea;
        }

        //获取附加库容断面有效距离
        private static double Get_Section_Distance(HydroModel hydromodel, ref Reach_Segment reach_segment)
        {
            //获取模型断面集合对象
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;

            //获取该河道断面所在的桩号位置，并得到该断面代表的长度
            List<double> reach_sectionchainage_list = sectionlist.Get_Reach_SecChainageList(reach_segment.reachname);

            //看是否有断面包含在内
            Dictionary<int, double> sectionchainage_include = new Dictionary<int, double>();
            for (int i = 0; i < reach_sectionchainage_list.Count; i++)
            {
                if (reach_sectionchainage_list[i] >= reach_segment.start_chainage && reach_sectionchainage_list[i] <= reach_segment.end_chainage)
                {
                    sectionchainage_include.Add(i, reach_sectionchainage_list[i]);
                }
            }

            //如果起止点相同或没有断面包含在内
            double section_distance;
            if (reach_segment.start_chainage == reach_segment.end_chainage || sectionchainage_include == null)
            {
                AtReach atreach = AtReach.Get_Atreach(reach_segment.reachname, (reach_segment.start_chainage + reach_segment.end_chainage) / 2);
                double last_chainage = atreach.chainage;
                double next_chainage = atreach.chainage;

                //找到最近的断面
                double min_distance = 10000;
                double min_distance_sectionchainage = reach_sectionchainage_list[0];
                int min_i = 0;
                for (int i = 0; i < reach_sectionchainage_list.Count; i++)
                {
                    if (Math.Abs(reach_sectionchainage_list[i] - atreach.chainage) < min_distance)
                    {
                        min_distance = Math.Abs(reach_sectionchainage_list[i] - atreach.chainage);
                        min_distance_sectionchainage = reach_sectionchainage_list[i];
                        min_i = i;
                    }
                }

                //找到最近断面的上下游断面
                if (min_i != 0)
                {
                    last_chainage = reach_sectionchainage_list[min_i - 1];
                }
                else
                {
                    last_chainage = reach_sectionchainage_list[min_i];
                }

                if (min_i != reach_sectionchainage_list.Count - 1)
                {
                    next_chainage = reach_sectionchainage_list[min_i + 1];
                }
                else
                {
                    next_chainage = reach_sectionchainage_list[min_i];
                }

                section_distance = (next_chainage - min_distance_sectionchainage) / 2 + (min_distance_sectionchainage - last_chainage) / 2;
            }
            else  //起止点不同,且有断面包含在内
            {
                double last_chainage = reach_segment.start_chainage;
                double next_chainage = reach_segment.end_chainage;

                //找到包含断面集合的前一个断面
                if (sectionchainage_include.Keys.ElementAt(0) != 0)
                {
                    int ii = sectionchainage_include.Keys.ElementAt(0);
                    last_chainage = reach_sectionchainage_list[ii - 1];
                }
                else
                {
                    last_chainage = reach_sectionchainage_list[0];
                }

                //找到包含断面集合的后一个断面
                if (sectionchainage_include.Keys.ElementAt(sectionchainage_include.Count - 1) != reach_sectionchainage_list.Count - 1)
                {
                    int ii = sectionchainage_include.Keys.ElementAt(sectionchainage_include.Count - 1);
                    last_chainage = reach_sectionchainage_list[ii + 1];
                }
                else
                {
                    last_chainage = reach_sectionchainage_list[reach_sectionchainage_list.Count - 1];
                }

                double include_startchainage = sectionchainage_include.Values.ElementAt(0);
                double include_endchainage = sectionchainage_include.Values.ElementAt(sectionchainage_include.Count - 1);

                section_distance = (next_chainage - include_endchainage) / 2 + (include_startchainage - last_chainage) / 2 + (include_endchainage - include_startchainage);
            }

            return section_distance;
        }

        //数据内插
        public static Dictionary<double, double> Get_InterpolateData(Dictionary<double, double> sourcedata, double[] level)
        {
            Dictionary<double, double> resultdic = new Dictionary<double, double>();
            for (int i = 0; i < level.Length; i++)
            {
                //需要内插库容的水位
                double water_level = level[i];
                double water_volue = -100;
                for (int j = 0; j < sourcedata.Count - 1; j++)
                {
                    double now_level = sourcedata.Keys.ElementAt(j);
                    double next_level = sourcedata.Keys.ElementAt(j + 1);

                    double now_volue = sourcedata.Values.ElementAt(j);
                    double next_volue = sourcedata.Values.ElementAt(j + 1);

                    //内插库容
                    if (water_level >= now_level && water_level <= next_level)
                    {
                        water_volue = now_volue + (water_level - now_level) * (next_volue - now_volue) / (next_level - now_level);
                        break;
                    }
                }

                //如果该水位不在库容曲线的水位范围内，则相应外延
                if (water_volue == -100)
                {
                    //如果该水位大于库容曲线的最大水位
                    if (water_level > sourcedata.Keys.Max())
                    {
                        double now_level = sourcedata.Keys.ElementAt(sourcedata.Count - 2);
                        double next_level = sourcedata.Keys.ElementAt(sourcedata.Count - 1);

                        double now_volue = sourcedata.Values.ElementAt(sourcedata.Count - 2);
                        double next_volue = sourcedata.Values.ElementAt(sourcedata.Count - 1);

                        //内插外延，并适度扩大
                        water_volue = now_volue + (water_level - now_level) * (next_volue - now_volue) / (next_level - now_level);
                        water_volue = water_volue * 1.1;
                    }

                    //如果该水位小于库容曲线的最小水位
                    if (water_level < sourcedata.Keys.Min())
                    {
                        water_volue = 0;  //直接取0
                    }
                }

                if (!resultdic.Keys.Contains(water_level))
                {
                    resultdic.Add(water_level, water_volue);
                }
            }

            return resultdic;
        }
        #endregion ***********************************************************************************


        #region ************ 其他：将原始数据写入断面资料数据库，更新断面资料数据库 *******************
        //将文本上的横断面数据写入数据库，方便前端快速查询
        public static void Readtxt_Write_sectiondata_intomysql(string sourcefilename)
        {
            //定义连接的数据库和表
            string tablename = Mysql_GlobalVar.SECTION_TABLENAME;
            Mysql mysql = new Mysql(Mysql_GlobalVar.NOWITEM_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port,
                                       Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            

            //从文本中获取所有河道的所有断面数据并将各节点坐标提取出来
            Dictionary<AtReach, List<PointXZS>> section_datadic = ReadSectionDataFile(sourcefilename);

            //将河道断面数据写入指定数据库指定表
            Write_sectiondata_intomysql(tablename, section_datadic);
        }

        //将默认模型横断面数据写入数据库，方便前端快速查询
        public static void Readtxt_Write_sectiondata_intomysql()
        {
            //定义连接的数据库和表
            string tablename = Mysql_GlobalVar.SECTION_TABLENAME;
            Mysql mysql = new Mysql(Mysql_GlobalVar.NOWITEM_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port,
                                       Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            

            //获取模型模型
            HydroModel default_model = HydroModel.Get_Default_Model();

            //从默认模型中获取断面数据
            Dictionary<AtReach, List<PointXZS>> section_datadic = default_model.Mike11Pars.SectionList.ReachSectionList;

            //将河道断面数据写入指定数据库指定表
            Write_sectiondata_intomysql(tablename, section_datadic);
        }

        //将默认模型横断面数据对象写入数据库
        public static void Readxns11_Write_Sectionobject_IntoDB()
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.SECTIONOBJECT_TABLENAME;
            string model_instance = Mysql_GlobalVar.now_instance;

            //先判断和删除旧的模型
            string select_sql = "select * from " + tableName + " where model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(select_sql);
            int nLen = dt.Rows.Count;
            if (nLen != 0) 
            {
                string del_sql = "delete from " + tableName + " where model_instance = '" + model_instance + "'";
                Mysql.Execute_Command(del_sql);
            }

            //从该模型实例的xns11文件中获取断面对象
            string xns11_dirname = Model_Files.Get_Item_DefaultModel_Dir(model_instance);
            string xns11_filename = Directory.GetFiles(xns11_dirname, "*.xns11").Length != 0 ? xns11_dirname + @"\" + Path.GetFileName(Directory.GetFiles(xns11_dirname, "*.xns11")[0]) : "";
            SectionList item_sectionlist = new SectionList();
            if (xns11_filename != "")
            {
                Xns11.GetDefault_SectionInfo(xns11_filename, ref item_sectionlist);
            }

            //插入新的数据
            MemoryStream ms = new MemoryStream();   //将模型流域集合类序列化后写入数据库
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            binFormat.Serialize(ms, item_sectionlist);  //将对象序列化
            byte[] buffer = ms.ToArray();  //转换成字节数组

            string sql = "insert into " + tableName + " (model_instance,section_object) values(:model_instance,:section_object)";
            KdbndpParameter[] mysqlPara = { 
                                             new KdbndpParameter(":model_instance", model_instance),
                                             new KdbndpParameter(":section_object", buffer)
                                         };
            Mysql.Execute_Command(sql, mysqlPara);
            ms.Close();
            ms.Dispose();

            Console.WriteLine("断面对象存储完成！");
        }

        //获取模型断面数据对象
        public static SectionList Load_ModelSection_Object(string model_instance)
        {
            SectionList section_object = null;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.SECTIONOBJECT_TABLENAME;

            //先判断该模型实例在数据库中是否存在
            string sql = "select section_object from " + tableName + " where model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //解析模型断面数据对象
            byte[] blob = Mysql.Get_Blob(sql);
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            section_object = binFormat.Deserialize(ms) as SectionList;
            ms.Close();
            ms.Dispose();

            return section_object;
        }

        //从数据库中删除指定的断面数据
        public static void Delete_sectiondata_frommysql(string tablename, List<AtReach> section_datadic)
        {
            //打开数据库连接
            

            //获取字段名
            for (int i = 0; i < section_datadic.Count; i++)
            {
                string sql = "delete from " + tablename + " where reach_name =  '" + section_datadic[i].reachname + "' and chainage = " + section_datadic[i].chainage;

                Mysql.Execute_Command(sql);
            }

            //关闭连接
            
        }

        //将河道断面数据写入指定数据库指定表
        public static void Write_sectiondata_intomysql(string tablename, Dictionary<AtReach, List<PointXZS>> section_datadic)
        {
            //获取字段名
            string select = "select * from " + tablename + " where id = 1 ";
            List<string> strlist = Mysql.Get_FieldsName(select);
            string fieldname_withoutid = null;
            for (int i = 1; i < strlist.Count - 1; i++)
            {
                fieldname_withoutid = fieldname_withoutid + strlist[i] + " , ";
            }
            fieldname_withoutid = fieldname_withoutid + strlist[strlist.Count - 1];

            //新建OBJECT类型的数组
            object[] value = new object[strlist.Count - 1];

            //遍历断面数据键值对集合中的所有断面数据
            int sectioncount = section_datadic.Count;
            for (int i = 0; i < sectioncount; i++)
            {
                //首先提取河道断面信息
                AtReach reachinfo = section_datadic.Keys.ElementAt(i);
                List<PointXZS> pointlist = section_datadic.Values.ElementAt(i);

                //其次遍历该断面上的点信息
                int pointcount = pointlist.Count;
                for (int j = 0; j < pointcount; j++)
                {
                    //每条记录的每个字段赋值
                    value[0] = j + 1;
                    value[1] = pointlist.ElementAt(j).X;
                    value[2] = pointlist.ElementAt(j).Z;
                    value[3] = pointlist.ElementAt(j).sign;

                    value[4] = reachinfo.reachname;
                    value[5] = i + 1;
                    value[6] = reachinfo.chainage;

                    //执行插入记录的sql语句,不写第1个字段id字段

                    Mysql.Execute_Command(Mysql.sql_insert(tablename, value, fieldname_withoutid));
                }
                Console.WriteLine("正在写入第{0}个断面，总断面数:{1}", i, sectioncount);
            }

            //关闭连接
            
        }

        //读取断面数据文件(sectiondata)，返回 河道断面信息和断面点信息 的键值对集合
        public static Dictionary<AtReach, List<PointXZS>> ReadSectionDataFile(string sourcefilename)
        {
            Dictionary<AtReach, List<PointXZS>> sectiondatadic = new Dictionary<AtReach, List<PointXZS>>();

            //使用文本流类StreamReader来读取大文本数据
            List<string> strlist = new List<string>();
            AtReach reachinfo;
            reachinfo.reachname = "";
            reachinfo.reachid = "";
            reachinfo.chainage = 0;
            List<PointXZS> pointinfolist = new List<PointXZS>();

            //文本读取
            using (StreamReader sr = new StreamReader(sourcefilename, Encoding.Default))
            {
                //使用StringBuilder类处理字符串速度更快
                StringBuilder strbuilder = new StringBuilder();
                int n = 0; int kk = 0;
                while (!sr.EndOfStream)
                {
                    //在strbuilder对象内添加文本的一行字符串
                    strbuilder.Append(sr.ReadLine());

                    char[] chararray = { ',' };
                    string[] str = strbuilder.ToString().Split(chararray);

                    //使用"正则表达式"判断是否是实数数据还是字符串
                    string regex = @"^-?\d+\.?\d*$";
                    if (!Regex.IsMatch(str[0], regex))   //如果不是实数
                    {
                        if (n != 0)
                        {
                            //这里注意，要新建一个键值对集合接过来，否则点集清空后，即使之前添加了的键值，也会被清空，因为集合是引用类型
                            List<PointXZS> pointinfolist1 = new List<PointXZS>();
                            for (int i = 0; i < pointinfolist.Count; i++)
                            {
                                PointXZS point = pointinfolist[i];
                                pointinfolist1.Add(point);

                            }

                            //添加上一个河道断面的数据
                            sectiondatadic.Add(reachinfo, pointinfolist1);

                            //点集重新清空
                            pointinfolist.Clear();

                        }

                        reachinfo.reachname = str[0];
                        reachinfo.chainage = double.Parse(str[1]);
                        reachinfo.reachid = reachinfo.reachname + Model_Const.REACHID_HZ;
                    }
                    else
                    {
                        //该断面点集合中添加该点
                        PointXZS point;
                        point.X = double.Parse(str[0]);
                        point.Z = double.Parse(str[1]);
                        point.sign = int.Parse(str[2]);
                        pointinfolist.Add(point);
                    }

                    //清空strbuilder对象内的字符串
                    strbuilder.Clear();
                    n++;
                }

                //最后一个断面也得加上
                sectiondatadic.Add(reachinfo, pointinfolist);
            }

            return sectiondatadic;
        }

        #endregion **************************************************************************************

    }

    //保存最新所有断面数据的类
    [Serializable]
    public class SectionList
    {
        //**********************属性************************
        //现有断面原始数据集合
        public Dictionary<AtReach, List<PointXZS>> ReachSectionList
        { get; set; }

        //现有断面附加库容数据集合
        public Dictionary<AtReach, Dictionary<double, double>> AddStorageAreaList
        { get; set; }

        //各河道的主槽滩地糙率比
        public Dictionary<string, double> Reach_LR_Highflow
        { get; set; }


        //**********************方法************************
        //增加新断面方法
        public void Add_Section(AtReach newreachlocal, List<PointXZS> newsectiondata)
        {
            if (!ReachSectionList.Keys.Contains(newreachlocal))
            {
                //往集合中增加新断面数据
                ReachSectionList.Add(newreachlocal, newsectiondata);
            }
        }

        //减少断面方法
        public void Remove_Section(AtReach newreachlocal)
        {
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).Equals(newreachlocal))
                {
                    ReachSectionList.Remove(newreachlocal);
                    break;
                }
            }
        }

        //减少断面附加库容方法
        public void Remove_SectionAddstorageArea(AtReach newreachlocal)
        {
            for (int i = 0; i < AddStorageAreaList.Count; i++)
            {
                if (AddStorageAreaList.Keys.ElementAt(i).Equals(newreachlocal))
                {
                    AddStorageAreaList.Remove(newreachlocal);
                    break;
                }
            }
        }

        //获取河道指定断面的断面数据
        public List<PointXZS> Get_Sectiondata(AtReach atreach)
        {
            List<PointXZS> res = new List<PointXZS>();
            Dictionary<AtReach, List<PointXZS>> reach_secions = Get_Reach_AllSecDataList(atreach.reachname);

            //断面在断面上
            for (int i = 0; i < reach_secions.Count; i++)
            {
                if (reach_secions.Keys.ElementAt(i).Equals(atreach))
                {
                    return reach_secions.Values.ElementAt(i);
                }
            }

            //断面不在断面上
            if(res.Count == 0)
            {
                List<AtReach> sectionlist = reach_secions.Keys.ToList();
                int index = File_Common.Search_Value(sectionlist, atreach.chainage);
                AtReach atreach1 = sectionlist[index];
                return reach_secions[atreach1];
            }
            Console.WriteLine("未找到指定断面!");
            return null;
        }


        //获取河道指定断面接近的断面数据
        public List<PointXZS> Get_NearSectiondata(AtReach newreachlocal)
        {
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                double now_chainage = Math.Round(ReachSectionList.Keys.ElementAt(i).chainage);

                if (newreachlocal.reachname == ReachSectionList.Keys.ElementAt(i).reachname && Math.Round(newreachlocal.chainage) == now_chainage)
                {
                    return ReachSectionList.Values.ElementAt(i);
                }
            }

            return null;
        }

        //获取河道指定断面的附加面积集合
        public Dictionary<double, double> Get_Section_AddstorageArea(AtReach newreachlocal)
        {
            for (int i = 0; i < AddStorageAreaList.Count; i++)
            {
                if (AddStorageAreaList.Keys.ElementAt(i).Equals(newreachlocal))
                {
                    return AddStorageAreaList.Values.ElementAt(i);
                }
            }

            Console.WriteLine("未找到指定断面!");
            return null;
        }

        //获取河道指定范围内的所有断面数据集合
        public Dictionary<AtReach, List<PointXZS>> Get_SegReach_SecDataList(Reach_Segment reach_segment)
        {
            Dictionary<AtReach, List<PointXZS>> SegReach_SecDataList = new Dictionary<AtReach, List<PointXZS>>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).reachname == reach_segment.reachname
                    && ReachSectionList.Keys.ElementAt(i).chainage >= reach_segment.start_chainage
                    && ReachSectionList.Keys.ElementAt(i).chainage <= reach_segment.end_chainage)
                {
                    SegReach_SecDataList.Add(ReachSectionList.Keys.ElementAt(i), ReachSectionList.Values.ElementAt(i));
                }
            }

            return SegReach_SecDataList;
        }

        //获取河道所有断面的断面数据
        public Dictionary<AtReach, List<PointXZS>> Get_Reach_AllSecDataList(string reachname)
        {
            Dictionary<AtReach, List<PointXZS>> SegReach_SecDataList = new Dictionary<AtReach, List<PointXZS>>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).reachname == reachname)
                {
                    SegReach_SecDataList.Add(ReachSectionList.Keys.ElementAt(i), ReachSectionList.Values.ElementAt(i));
                }
            }

            return SegReach_SecDataList;
        }

        //获取河道所有断面的最大最小值集合
        public Dictionary<double, Max_Min_Z> GetReach_AllSec_MaxminList(string reachname)
        {
            Dictionary<double, Max_Min_Z> result_dic = new Dictionary<double, Max_Min_Z>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).reachname == reachname)
                {
                    List<PointXZS> section_data = ReachSectionList.Values.ElementAt(i);
                    Max_Min_Z maxmin = Get_Max_MinZ(section_data);

                    result_dic.Add(Math.Round(ReachSectionList.Keys.ElementAt(i).chainage, 1), maxmin);
                }
            }

            return result_dic;
        }

        //获取河道所有断面的 滩地高程值集合
        public Dictionary<double, double> GetReach_AllSec_TdList(string reachname,Dictionary<double,double> reach_td_list = null)
        {
            Dictionary<double, double> result_dic = new Dictionary<double, double>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).reachname == reachname)
                {
                    double chainage = ReachSectionList.Keys.ElementAt(i).chainage;
                    double td = 10000;
                    if (reach_td_list != null)
                    {
                        if(reach_td_list.Keys.Contains(chainage)) td = reach_td_list[chainage];
                    }
                    else
                    {
                        List<PointXZS> section_data = ReachSectionList.Values.ElementAt(i);
                        td = (section_data.First().Z + section_data.Last().Z) * 0.5;
                    }
                    result_dic.Add(chainage, td);
                }
            }

            return result_dic;
        }



        //获取指定河道断面的最大最小值
        public Max_Min_Z GetReach_Section_MaxminZ(AtReach atreach)
        {
            List<PointXZS> section_data = Get_Sectiondata(atreach);
            Max_Min_Z maxmin = Get_Max_MinZ(section_data);

            return maxmin;
        }

        //根据河道名获取该河道所有断面的桩号集合
        public List<double> Get_Reach_SecChainageList(string reachname)
        {
            List<double> ReachSecList = new List<double>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).reachname == reachname)
                {
                    ReachSecList.Add(ReachSectionList.Keys.ElementAt(i).chainage);
                }
            }

            return ReachSecList;
        }

        //根据河道名获取该河道指定范围内的所有断面的桩号集合
        public List<double> Get_SegReach_SecChainageList(Reach_Segment reach_segment)
        {
            List<double> ReachSecList = new List<double>();
            for (int i = 0; i < ReachSectionList.Count; i++)
            {
                if (ReachSectionList.Keys.ElementAt(i).reachname == reach_segment.reachname
                    && ReachSectionList.Keys.ElementAt(i).chainage >= reach_segment.start_chainage
                    && ReachSectionList.Keys.ElementAt(i).chainage <= reach_segment.end_chainage)
                {
                    ReachSecList.Add(ReachSectionList.Keys.ElementAt(i).chainage);
                }
            }

            return ReachSecList;
        }


        //获取河道的最大断面宽度
        public double Get_ReachSection_Maxwidth(string reachname)
        {
            double max_width = 0;
            List<double> ReachSecList = Get_Reach_SecChainageList(reachname);
            for (int i = 0; i < ReachSecList.Count; i++)
            {
                AtReach newreachlocal;
                newreachlocal.reachname = reachname;
                newreachlocal.reachid = reachname + Model_Const.REACHID_HZ; ;
                newreachlocal.chainage = ReachSecList[i];

                //断面宽度
                List<PointXZS> Sectiondata = Get_Sectiondata(newreachlocal);
                double sec_width = Math.Abs(Sectiondata[Sectiondata.Count - 1].X - Sectiondata[0].X);
                if (sec_width > max_width)
                {
                    max_width = sec_width;
                }
            }
            return max_width;
        }

        //获取河段的断面左岸或右岸宽度集合
        public Dictionary<double, double> Get_Section_LRwidth(Reach_Segment reach_segment, ReachLR reachlr)
        {
            Dictionary<double, double> chainage_distancedic = new Dictionary<double, double>();

            //获取河段内的断面数据集合
            Dictionary<AtReach, List<PointXZS>> SecDataList = Get_SegReach_SecDataList(reach_segment);

            for (int i = 0; i < SecDataList.Count; i++)
            {
                double distance = 0;
                double section_chainage = SecDataList.Keys.ElementAt(i).chainage;
                List<PointXZS> sectionpointlist = SecDataList.Values.ElementAt(i);
                double point1_x = 0;
                double point2_x = 0;
                double point3_x = 0;
                for (int j = 0; j < sectionpointlist.Count; j++)
                {
                    if (sectionpointlist[j].sign == 1)
                    {
                        point1_x = sectionpointlist[j].X;
                    }

                    if (sectionpointlist[j].sign == 2)
                    {
                        point2_x = sectionpointlist[j].X;
                    }

                    if (sectionpointlist[j].sign == 3)
                    {
                        point3_x = sectionpointlist[j].X;
                    }
                }

                //根据要推求的左右岸选择，计算相应的距离值
                switch (reachlr)
                {
                    case ReachLR.left:
                        distance = Math.Abs(point1_x - point2_x);
                        break;
                    case ReachLR.right:
                        distance = Math.Abs(point3_x - point2_x);
                        break;
                }

                //在键值对（桩号，距离）集合中增加元素
                chainage_distancedic.Add(section_chainage, distance);
            }

            return chainage_distancedic;
        }

        //根据河道名获取该河道的主槽滩地糙率比
        public double Get_Reach_LR_Highflow(string reachname)
        {
            for (int i = 0; i < Reach_LR_Highflow.Count; i++)
            {
                if (Reach_LR_Highflow.Keys.ElementAt(i) == reachname)
                {
                    return Reach_LR_Highflow.Values.ElementAt(i);
                }
            }

            Console.WriteLine("该河道为新河道，按默认主槽滩地糙率比!");
            return Model_Const.LRHIGHFLOW;
        }

        //给断面增加附加库容
        public void Addstorage_OnSection(Reach_Segment section_atreachseg, Dictionary<double, double> addstorage_level_area)
        {
            //获取河段内的断面桩号集合
            List<double> sectionchainage_include = Get_SegReach_SecChainageList(section_atreachseg);

            //如果没有包含断面，则取距离河段中心最近的断面
            if (section_atreachseg.start_chainage == section_atreachseg.end_chainage || sectionchainage_include == null)
            {
                AtReach center_atreach = AtReach.Get_Atreach(section_atreachseg.reachname, (section_atreachseg.start_chainage + section_atreachseg.end_chainage) / 2);
                AtReach nearsection = Get_Near_SectionChainage(center_atreach);
                AddStorageAreaList[nearsection] = addstorage_level_area;
            }
            else
            {
                //每一个包含在内的断面都加上 附加库容面积的百分比
                for (int i = 0; i < sectionchainage_include.Count; i++)
                {
                    AtReach atreach = AtReach.Get_Atreach(section_atreachseg.reachname, sectionchainage_include[i]);

                    //每段渠道占总的百分比
                    double value = 1.0 / sectionchainage_include.Count;

                    Dictionary<double, double> new_addstorage_level_area = new Dictionary<double, double>();
                    for (int j = 0; j < addstorage_level_area.Count; j++)
                    {
                        new_addstorage_level_area.Add(addstorage_level_area.Keys.ElementAt(j), addstorage_level_area.Values.ElementAt(j) * value);
                    }
                    AddStorageAreaList[atreach] = new_addstorage_level_area;
                }
            }
        }

        //找到距离给定桩号最近的横断面
        public AtReach Get_Near_SectionChainage(AtReach atreach)
        {
            //获取该河道断面所在的桩号位置，并得到该断面代表的长度
            List<double> reach_sectionchainage_list = Get_Reach_SecChainageList(atreach.reachname);

            //找到最近的断面
            double min_distance = 10000;
            double min_distance_sectionchainage = reach_sectionchainage_list[0];
            int min_i = 0;
            for (int i = 0; i < reach_sectionchainage_list.Count; i++)
            {
                if (Math.Abs(reach_sectionchainage_list[i] - atreach.chainage) < min_distance)
                {
                    min_distance = Math.Abs(reach_sectionchainage_list[i] - atreach.chainage);
                    min_distance_sectionchainage = reach_sectionchainage_list[i];
                    min_i = i;
                }
            }

            return AtReach.Get_Atreach(atreach.reachname, min_distance_sectionchainage);
        }

        //找出给定桩号的上一个断面
        public AtReach Get_Last_SectionChainage(AtReach atreach)
        {
            AtReach Last_SectionChainage = atreach;
            List<double> reach_sectionchainage_list = Get_Reach_SecChainageList(atreach.reachname);

            //刚好等于第一个断面桩号
            if (atreach.chainage == reach_sectionchainage_list[0])
            {
                return AtReach.Get_Atreach(atreach.reachname, reach_sectionchainage_list[0]);
            }

            for (int i = 0; i < reach_sectionchainage_list.Count - 1; i++)
            {
                if (reach_sectionchainage_list[i] == atreach.chainage)
                {
                    Last_SectionChainage = AtReach.Get_Atreach(atreach.reachname, reach_sectionchainage_list[i - 1]);
                    break;
                }
                else if (reach_sectionchainage_list[i] < atreach.chainage && reach_sectionchainage_list[i + 1] > atreach.chainage)
                {
                    Last_SectionChainage = AtReach.Get_Atreach(atreach.reachname, reach_sectionchainage_list[i]);
                    break;
                }
            }
            return Last_SectionChainage;
        }

        //找出给定桩号的下一个断面
        public AtReach Get_Next_SectionChainage(AtReach atreach)
        {
            AtReach Next_SectionChainage = atreach;
            List<double> reach_sectionchainage_list = Get_Reach_SecChainageList(atreach.reachname);

            //刚好等于最后一个断面桩号
            if (atreach.chainage == reach_sectionchainage_list[reach_sectionchainage_list.Count - 1])
            {
                return AtReach.Get_Atreach(atreach.reachname, reach_sectionchainage_list[reach_sectionchainage_list.Count - 1]);
            }

            for (int i = 0; i < reach_sectionchainage_list.Count - 1; i++)
            {
                if (reach_sectionchainage_list[i] == atreach.chainage)
                {
                    Next_SectionChainage = AtReach.Get_Atreach(atreach.reachname, reach_sectionchainage_list[i + 1]);
                    break;
                }
                else if (reach_sectionchainage_list[i] < atreach.chainage && reach_sectionchainage_list[i + 1] > atreach.chainage)
                {
                    Next_SectionChainage = AtReach.Get_Atreach(atreach.reachname, reach_sectionchainage_list[i + 1]);
                    break;
                }
            }
            return Next_SectionChainage;
        }

        //获取断面最大最小Z值
        public static Max_Min_Z Get_Max_MinZ(List<PointXZS> section_pointlist)
        {
            //获取最大最小Z值
            double zmin = 10000;
            double zmax = -10000;
            for (int i = 0; i < section_pointlist.Count; i++)
            {
                if (section_pointlist[i].Z < zmin)
                {
                    zmin = section_pointlist[i].Z;
                }

                if (section_pointlist[i].Z > zmax)
                {
                    zmax = section_pointlist[i].Z;
                }
            }

            Max_Min_Z result;
            result.max_z = zmax;
            result.min_z = zmin;
            return result;
        }

        //获取指定河道断面的左右堤顶高程和地面高程数据,不在断面上的内插
        public ReachSection_Altitude Get_Altitude(AtReach atreach)
        {
            //如果刚好在断面上的情况
            ReachSection_Altitude section_altitude = Get_Section_Altitude(atreach);
            if (section_altitude.left_dd_altitude != 0)
            {
                return section_altitude;
            }

            //获取上、下游最近断面数据
            AtReach up_atreach = Get_Last_SectionChainage(atreach);
            AtReach down_atreach = Get_Next_SectionChainage(atreach);
            ReachSection_Altitude up_section_altitude = Get_Section_Altitude(up_atreach);
            ReachSection_Altitude down_section_altitude = Get_Section_Altitude(down_atreach);

            //实际高程通过桩号内插得到
            double l_dd_altitude = up_section_altitude.left_dd_altitude + (atreach.chainage - up_atreach.chainage) *
                (down_section_altitude.left_dd_altitude - up_section_altitude.left_dd_altitude) / (down_atreach.chainage - up_atreach.chainage);

            double l_ground_altitude = up_section_altitude.left_ground_altitude + (atreach.chainage - up_atreach.chainage) *
                (down_section_altitude.left_ground_altitude - up_section_altitude.left_ground_altitude) / (down_atreach.chainage - up_atreach.chainage);

            double r_dd_altitude = up_section_altitude.right_dd_altitude + (atreach.chainage - up_atreach.chainage) *
                (down_section_altitude.right_dd_altitude - up_section_altitude.right_dd_altitude) / (down_atreach.chainage - up_atreach.chainage);

            double r_ground_altitude = up_section_altitude.right_ground_altitude + (atreach.chainage - up_atreach.chainage) *
                (down_section_altitude.right_ground_altitude - up_section_altitude.right_ground_altitude) / (down_atreach.chainage - up_atreach.chainage);

            double lowest = up_section_altitude.section_lowest + (atreach.chainage - up_atreach.chainage) *
                (down_section_altitude.section_lowest - up_section_altitude.section_lowest) / (down_atreach.chainage - up_atreach.chainage);

            section_altitude = ReachSection_Altitude.Get_ReachSection_Altitude(l_dd_altitude, l_ground_altitude, r_dd_altitude, r_ground_altitude, lowest);
            return section_altitude;
        }

        //获取指定断面上的特征点高程数据(所在河道断面桩号必须属于已有断面)
        public ReachSection_Altitude Get_Section_Altitude(AtReach atreach)
        {
            ReachSection_Altitude section_altitude;
            double left_dd_altitude = 0;
            double left_ground_altitude = 0;
            double right_dd_altitude = 0;
            double right_ground_altitude = 0;
            double section_lowest = 0;

            section_altitude.left_dd_altitude = left_dd_altitude;
            section_altitude.left_ground_altitude = left_ground_altitude;
            section_altitude.right_dd_altitude = right_dd_altitude;
            section_altitude.right_ground_altitude = right_dd_altitude;
            section_altitude.section_lowest = section_lowest;

            //刚好位于断面上
            List<PointXZS> sectiondata = Get_Sectiondata(atreach);
            if (sectiondata != null)
            {
                //调用方法从断面数据中获取左右堤顶高程和地面高程、河底高程
                Xns11.Get_LRAltitude(sectiondata, ReachLR.left, out left_dd_altitude, out left_ground_altitude, out section_lowest);
                Xns11.Get_LRAltitude(sectiondata, ReachLR.right, out right_dd_altitude, out right_ground_altitude, out section_lowest);

                //高程赋值
                section_altitude.left_dd_altitude = left_dd_altitude;
                section_altitude.left_ground_altitude = left_ground_altitude;
                section_altitude.right_dd_altitude = right_dd_altitude;
                section_altitude.right_ground_altitude = right_ground_altitude;
                section_altitude.section_lowest = section_lowest;
                return section_altitude;
            }

            Console.WriteLine("该河道位置没有断面数据！");
            return section_altitude;
        }

        //根据桩号和水位获取水面宽度
        public double Get_WaterWidth(AtReach atreach, double water_level)
        {
            double waterwidth = 0;
            List<PointXZS> section_point = this.ReachSectionList.Keys.Contains(atreach) ? this.ReachSectionList[atreach] : null;
            if (section_point == null) return waterwidth;

            for (int i = 0; i < section_point.Count - 1; i++)
            {
                if (section_point[i].Z > water_level && section_point[i + 1].Z < water_level)
                {
                    double k = Math.Abs(section_point[i + 1].X - section_point[i].X) / Math.Abs(section_point[i + 1].Z - section_point[i].Z);
                    double w1 = Math.Abs(water_level - section_point[i + 1].Z) * k;
                    waterwidth += w1;
                }
                else if (section_point[i].Z < water_level && section_point[i + 1].Z < water_level)
                {
                    waterwidth += Math.Abs(section_point[i + 1].X - section_point[i].X);
                }
                else if (section_point[i].Z < water_level && section_point[i + 1].Z > water_level)
                {
                    double k = Math.Abs(section_point[i + 1].X - section_point[i].X) / Math.Abs(section_point[i + 1].Z - section_point[i].Z);
                    double w1 = Math.Abs(water_level - section_point[i].Z) * k;
                    waterwidth += w1;
                }
            }
            return waterwidth;
        }

        //根据桩号获取断面宽度
        public double Get_SectionWidth(AtReach atreach, Dictionary<AtReach, List<PointXZS>> reach_sections)
        {
            double waterwidth = 0;
            List<PointXZS> section_point;
            if (reach_sections.Keys.Contains(atreach) )
            {
                section_point = reach_sections[atreach];
            }
            else  //断面桩号由于保留1位小数，会导致同一个断面判断不包含
            {
                List<AtReach> section_chainage = reach_sections.Keys.ToList();
                int res_index = File_Common.Search_Value(section_chainage, atreach.chainage, true);
                section_point = reach_sections[section_chainage[res_index]];
            }

            if (section_point == null) return waterwidth;

            return Math.Abs(section_point.Last().X - section_point[0].X);
        }
    
    }

    //水库类(**挂在断面上的水库库容与断面间距无关，库容 = ∑各层平均面积 * 各层水深 **)
    [Serializable]
    public class Reservoir
    {
        //构造函数
        public Reservoir(Dictionary<double, double> Level_Volume, Reach_Segment Atreach_Segment)
        {
            Set_Default();
            this.Level_Volume = Level_Volume;
            this.Atreach_Segment = Atreach_Segment;
        }

        public Reservoir(string stcd, string name, Dictionary<double, double> level_Volume,
            Dictionary<double, double> level_yhdq, Dictionary<double, double> level_xhdq, Reach_Segment atreach_Segment,
            List<Reach_Segment> contain_ReachSeg)
        {
            Set_Default();
            this.Stcd = stcd;
            this.Name = name;
            this.Level_Volume = level_Volume;
            this.Level_YhdQ = level_yhdq;
            this.Level_XhdQ = level_xhdq;
            this.Atreach_Segment = atreach_Segment;
            this.Contain_ReachSeg = contain_ReachSeg;
        }

        //属性
        //水库的编码
        public string Stcd
        { get; set; }

        //水库名称
        public string Name
        { get; set; }

        //库容曲线(m - m3)
        public Dictionary<double, double> Level_Volume
        { get; set; }

        //溢洪道泄流曲线
        public Dictionary<double,double> Level_YhdQ
        { get; set; }

        //泄洪洞泄流曲线
        public Dictionary<double, double> Level_XhdQ
        { get; set; }

        //所在主河道位置 -- 一段河道(起止点相同则为一个点)
        public Reach_Segment Atreach_Segment
        { get; set; }

        //坝上包括的河段集合
        public List<Reach_Segment> Contain_ReachSeg
        { get; set; }

        //方法
        public void Set_Default()
        {
            this.Stcd = "";
            this.Name = "";
            this.Level_Volume = null;
            this.Level_YhdQ = null;
            this.Level_XhdQ = null;
            this.Atreach_Segment = Reach_Segment.Get_ReachSegment("", 0, 0);
            this.Contain_ReachSeg = new List<Reach_Segment>() { this.Atreach_Segment };
        }

        //得到默认附加库容 - 20个水位
        public static Dictionary<double, double> Get_Default_level_volume(double min_level, double max_level)
        {
            Dictionary<double, double> new_level_volume = new Dictionary<double, double>();

            //将水位细分为默认等份
            for (int i = 0; i < Model_Const.SECTION_LEVELCOUNT; i++)
            {
                double level = min_level + (max_level - min_level) * i / Model_Const.SECTION_LEVELCOUNT;
                if (i == Model_Const.SECTION_LEVELCOUNT - 1)
                {
                    level = max_level;
                }
                new_level_volume.Add(level, 0);
            }
            return new_level_volume;
        }

        //根据名称获取指定水库的水位库容曲线
        public static Dictionary<double, double> Get_Res_LevelVolume(string model_instance, string res_name)
        {
            Dictionary<double, double> result = new Dictionary<double, double>();
            List<Reservoir> res_list = Item_Info.Get_ResInfo(model_instance);

            for (int i = 0; i < res_list.Count; i++)
            {
                Reservoir res = res_list[i];
                if(res.Name == res_name)
                {
                    result = res.Level_Volume;
                    break;
                }
            }

            return result;
        }

        //根据名称获取指定水库
        public static Reservoir Get_Res_Info(string model_instance, string res_name)
        {
            List<Reservoir> res_list = Item_Info.Get_ResInfo(model_instance);

            for (int i = 0; i < res_list.Count; i++)
            {
                Reservoir res = res_list[i];
                if (res.Name == res_name) return res;
            }

            return null;
        }

        //宽顶堰流公式计算溢洪道指定水位下的流量
        public static double Get_Yhd_OverDischarge(double level,double datumn,double width)
        {
            if (level <= datumn) return 0;
            return Model_Const.WEIR_M * width * (level - datumn) * Math.Sqrt(2 * 9.8 * (level - datumn));
        }
    }

    //蓄滞洪区
    [Serializable]
    public class Xzhq_Info
    {
        public Xzhq_Info()
        {
            Stcd = "";
            Name = "";
            Father = "";
            Design_Level = "";
            Only_FhyPd_State = false;
            State_Pd_AtReachQ = new Dictionary<AtReach, double>();
            InQ_Section_List = new Dictionary<string, AtReach>();
            OutQ_Section_List = new Dictionary<string, AtReach>();
            Level_Atreach = new AtReach();
            Xzhq_IsFlood_Level = 0;
            Level_Volume = new Dictionary<double, double>();
            Level_Area = new Dictionary<double, double>();
        }

        public Xzhq_Info(string stcd, string name, string father,string design_l,bool fhy_pd, Dictionary<AtReach, double> state_atreachq,
            Dictionary<string, AtReach> inQ_Section_List,Dictionary<string, AtReach> outQ_Section_List,
            AtReach level_Atreach, AtReach xzhq_Atreach, double xzhq_IsFlood_Level, 
            Dictionary<double, double> level_Volume, Dictionary<double, double> level_Area)
        {
            Stcd = stcd;
            Name = name;
            Father = father;
            Design_Level = design_l;
            Only_FhyPd_State = fhy_pd;
            State_Pd_AtReachQ = state_atreachq;
            InQ_Section_List = inQ_Section_List;
            OutQ_Section_List = outQ_Section_List;
            Level_Atreach = level_Atreach;
            Xzhq_IsFlood_Level = xzhq_IsFlood_Level;
            Level_Volume = level_Volume;
            Level_Area = level_Area;
        }

        //属性
        public string Stcd { get; set; }
        public string Name { get; set; }
        public string Father { get; set; }         //父蓄滞洪区
        public string Design_Level { get; set; }  //设计水位
        public bool Only_FhyPd_State { get; set; }  //是否仅采用分洪堰判断是否启用
        public Dictionary<AtReach,double> State_Pd_AtReachQ { get; set; }     //判断是否启用的特征断面流量(仅Only_FhyPd_State为false时起作用)
        public Dictionary<string,AtReach> InQ_Section_List { get; set; }    //入库断面集合
        public Dictionary<string, AtReach> OutQ_Section_List { get; set; }   //出库断面集合
        public AtReach Level_Atreach { get; set; }  //代表蓄滞洪区水位的断面位置,用于判断是否启用的标准之一
        public double Xzhq_IsFlood_Level { get; set; } //蓄滞洪区进洪、已基本退洪的水位阈值(库容曲线第1个水位+0.3m)
        public Dictionary<double, double> Level_Volume { get; set; } //水位-库容曲线(m - 万m3)
        public Dictionary<double, double> Level_Area { get; set; } //水位-淹没面积曲线(m - km2)


        //刷选出各父类蓄滞洪区的子蓄滞洪区
        public static Dictionary<string, List<string>> Get_Son_List(List<Xzhq_Info> xzhq_list)
        {
            Dictionary<string, List<string>> son_xzhq = new Dictionary<string, List<string>>();
            for (int i = 0; i < xzhq_list.Count; i++)
            {
                string father = xzhq_list[i].Father;
                if (father != "")
                {
                    if (son_xzhq.Keys.Contains(father))
                    {
                        son_xzhq[father].Add(xzhq_list[i].Name);
                    }
                    else
                    {
                        List<string> son_list = new List<string>() { xzhq_list[i].Name };
                        son_xzhq.Add(father, son_list);
                    }
                }
            }

            return son_xzhq;
        }

        //刷选父类蓄滞洪区
        public static List<Xzhq_Info> Get_Father_List(List<Xzhq_Info> xzhq_list)
        {
            List<Xzhq_Info> father_xzhq = new List<Xzhq_Info>();
            for (int i = 0; i < xzhq_list.Count; i++)
            {
                Xzhq_Info xzhq = xzhq_list[i];
                if (xzhq.Father == "") father_xzhq.Add(xzhq);
            }

            return father_xzhq;
        }
    }

    //水库入流位置和边界ID
    [Serializable]
    public class Res_InSection_BndID
    {
        public Res_InSection_BndID()
        {
            Up_Insection = new List<AtReach>();
            In_BndID = new List<string>();
            Inflow_Speed = Model_Const.RES_INFLOW_SPEED;
        }

        public Res_InSection_BndID(List<string> in_BndID)
        {
            Up_Insection = new List<AtReach>();
            In_BndID = in_BndID;
            Inflow_Speed = Model_Const.RES_INFLOW_SPEED;
        }

        public Res_InSection_BndID(List<AtReach> atreach)
        {
            Up_Insection = atreach;
            In_BndID = new List<string>();
            Inflow_Speed = Model_Const.RES_INFLOW_SPEED;
        }


        public Res_InSection_BndID(List<AtReach> up_Insection, List<string> in_BndID)
        {
            Up_Insection = up_Insection;
            In_BndID = in_BndID;
            Inflow_Speed = Model_Const.RES_INFLOW_SPEED;
        }

        public Res_InSection_BndID(List<AtReach> up_Insection, List<string> in_BndID, double in_speed)
        {
            Up_Insection = up_Insection;
            In_BndID = in_BndID;
            Inflow_Speed = in_speed;
        }

        //入流断面集合
        public List<AtReach> Up_Insection { get; set; }

        //入流边界条件集合
        public List<string> In_BndID { get; set; }

        //入库平均流速
        public double Inflow_Speed { get; set; }
    }
}