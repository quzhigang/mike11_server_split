using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using System.IO;
using DHI.Generic.MikeZero.DFS.mesh;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike11;
using bjd_model.Mike21;
using bjd_model;

namespace bjd_model.Mike_Flood
{
    // MIKE11和MIKE21模型选择及文件输入
    [Serializable]
    public struct MIKE11_21FliePath
    {
        public string Mike21_Path;  //mike21文件路径
        public string Mike11_Path;  //mike11文件路径
    }

    //侧向连接参数结构体
    [Serializable]
    public struct LateralPars
    {
        public double WeirC;         //堰流系数
        public double Depth_Tol;     //水头差阀值
        public double Fric;          //糙率系数
        public double Smooth_factor;  //平滑因子
    }

    public static class Couple
    {
        #region ****************************从默认couple文件中获取连接信息*********************************
        //从默认couple文件中获取耦合连接详细信息
        public static void GetCoupleInfo(string sourcefilename, ref CoupleLinkList CoupleLinklist)
        {
            PFSFile pfsfile = new PFSFile(sourcefilename);   //读取文件
            PFSSection target = pfsfile.GetTarget("M11M21_Coupling_Parameters", 1);   //最外面的节

            PFSKeyword Number_of_CoupledBoundaries = target.GetKeyword("Number_of_CoupledBoundaries");
            int couplecount = Number_of_CoupledBoundaries.GetParameter(1).ToInt();

            List<Link> linklist = new List<Link>();
            for (int i = 0; i < couplecount; i++)
            {
                PFSSection Definition = target.GetSection("Definition", i + 1);
                if (Definition.GetKeyword("Linktype").GetParameter(1).ToInt() == 2)  //侧向连接
                {
                    //获取连接基本情况-- 连接类型、河段信息、连接点集合等
                    Reach_Segment link_reach;
                    link_reach.reachname = Definition.GetKeyword("M11_River_Name").GetParameter(1).ToString();
                    link_reach.start_chainage = Definition.GetKeyword("M11_Chainage").GetParameter(1).ToDouble();
                    link_reach.end_chainage = Definition.GetKeyword("M11_Chainage").GetParameter(2).ToDouble();
                    link_reach.reachtopoid = link_reach.reachname + Model_Const.REACHID_HZ;

                    //获取连接点信息
                    List<ReachPoint> Link_PointList = new List<ReachPoint>();
                    int pointcount = Definition.GetKeyword("npoints_FM").GetParameter(1).ToInt();
                    PFSKeyword points_x = Definition.GetKeyword("points_x");
                    PFSKeyword points_y = Definition.GetKeyword("points_y");
                    PFSKeyword points_chainage = Definition.GetKeyword("points_chainage");

                    for (int j = 0; j < pointcount; j++)
                    {
                        ReachPoint reachpoint;
                        reachpoint.number = j + 1;
                        reachpoint.X = points_x.GetParameter(j + 1).ToDouble();
                        reachpoint.Y = points_y.GetParameter(j + 1).ToDouble();
                        reachpoint.pointchainage = points_chainage.GetParameter(j + 1).ToDouble();
                        reachpoint.chainagetype = 0;
                        Link_PointList.Add(reachpoint);
                    }

                    //获取侧向连接参数
                    int Left_Right = Definition.GetKeyword("Side_of_River").GetParameter(1).ToInt();
                    LateralPars Link_pars;
                    Link_pars.WeirC = Definition.GetKeyword("Lateral_structure_params").GetParameter(1).ToDouble();
                    Link_pars.Fric = Definition.GetKeyword("Lateral_structure_params").GetParameter(3).ToDouble();
                    Link_pars.Smooth_factor = Definition.GetKeyword("M21_relaxation_factor").GetParameter(1).ToDouble();
                    Link_pars.Depth_Tol = Definition.GetKeyword("Lateral_structure_deptol").GetParameter(1).ToDouble();

                    //根据获取的信息构建 侧向连接对象
                    Lateral_Link lateral_link = new Lateral_Link(link_reach, Link_PointList, Left_Right, Link_pars);

                    //加入连接集合
                    linklist.Add(lateral_link);
                }
                else if (Definition.GetKeyword("Linktype").GetParameter(1).ToInt() == 9)  //侧向建筑物连接
                {
                    //获取连接基本情况-- 连接类型、河段信息、连接点集合等
                    Reach_Segment link_reach;
                    link_reach.reachname = Definition.GetKeyword("M11_River_Name").GetParameter(1).ToString();
                    link_reach.start_chainage = Definition.GetKeyword("M11_Chainage").GetParameter(1).ToDouble();
                    link_reach.end_chainage = Definition.GetKeyword("M11_Chainage").GetParameter(2).ToDouble();
                    link_reach.reachtopoid = link_reach.reachname + Model_Const.REACHID_HZ;

                    //获取连接点信息
                    List<ReachPoint> Link_PointList = new List<ReachPoint>();
                    int pointcount = Definition.GetKeyword("npoints_FM").GetParameter(1).ToInt();
                    PFSKeyword points_x = Definition.GetKeyword("points_x");
                    PFSKeyword points_y = Definition.GetKeyword("points_y");
                    PFSKeyword points_chainage = Definition.GetKeyword("points_chainage");

                    for (int j = 0; j < pointcount; j++)
                    {
                        ReachPoint reachpoint;
                        reachpoint.number = j + 1;
                        reachpoint.X = points_x.GetParameter(j + 1).ToDouble();
                        reachpoint.Y = points_y.GetParameter(j + 1).ToDouble();
                        reachpoint.pointchainage = points_chainage.GetParameter(j + 1).ToDouble();
                        reachpoint.chainagetype = 0;
                        Link_PointList.Add(reachpoint);
                    }

                    //获取侧向建筑物连接参数
                    double smoothfactor = Definition.GetKeyword("Lateral_structure_deptol").GetParameter(1).ToDouble();

                    //根据获取的信息构建 侧向建筑物连接对象
                    SideStructure_Link sidestructure_link = new SideStructure_Link(link_reach, Link_PointList, smoothfactor);

                    //加入连接集合
                    linklist.Add(sidestructure_link);
                }
            }

            CoupleLinklist.Link_infolist = linklist;
            Console.WriteLine("耦合信息初始化成功!");
            pfsfile.Close();
        }
        #endregion ****************************************************************************************



        #region **************************根据前端模型选择和连接参数，更新couple文件*********************
        // 根据前端计算模型选择，相应输入MIKE11和MIKE21文件路径
        public static void Rewrite_Couple_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Couple_filename;
            string outputfilename = hydromodel.Modelfiles.Couple_filename;
            CoupleLinkList couplelinklist = hydromodel.CoupleLinklist;
            if (couplelinklist == null) return;
            if (couplelinklist.Link_infolist == null) return;
            if (couplelinklist.Link_infolist.Count == 0) return;

            PFSFile pfsfile = new PFSFile(sourcefilename, false);   //读取文件
            PFSSection target = pfsfile.GetTarget("M11M21_Coupling_Parameters", 1);   //最外面的节

            //根据前端模型选择对输入文件进行更新
            Update_FilePathKey(hydromodel, ref target);
            Console.WriteLine("模型文件选择成功!");

            //耦合连接更新
            Update_DefinitionSec(couplelinklist, ref target);
            Console.WriteLine("连接信息更新成功!");

            //重新写入
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("Couple耦合模拟文件更新成功！");
            Console.WriteLine("");
        }

        //根据前端模型选择对输入文件进行选择和更新
        public static void Update_FilePathKey(HydroModel hydromodel, ref PFSSection target)
        {
            //获取用户前端选择的计算模型
            CalculateModel model = hydromodel.ModelGlobalPars.Select_model;

            //获取最新模拟文件路径
            MIKE11_21FliePath mike11_21FilePath;
            mike11_21FilePath.Mike11_Path = hydromodel.Modelfiles.Simulate_filename;
            mike11_21FilePath.Mike21_Path = hydromodel.Modelfiles.M21fm_filename;

            PFSKeyword MIKE21 = target.GetKeyword("MIKE21", 1);

            //根据模型选择判断是否需要加入二维模型
            if (model == CalculateModel.rr_hd_flood)
            {
                MIKE21.GetParameter(1).ModifyBoolParameter(true);
                PFSKeyword MIKE21_Path = target.GetKeyword("MIKE21_Path", 1);
                MIKE21_Path.GetParameter(1).ModifyFileNameParameter(mike11_21FilePath.Mike21_Path);
            }
            else
            {
                MIKE21.GetParameter(1).ModifyBoolParameter(false);
                PFSKeyword MIKE21_Path = target.GetKeyword("MIKE21_Path", 1);
                MIKE21_Path.GetParameter(1).ModifyFileNameParameter("");
            }

            //一维模型肯定加，但需要换成最新地址
            PFSKeyword MIKE11 = target.GetKeyword("MIKE11", 1);
            MIKE11.GetParameter(1).ModifyBoolParameter(true);
            PFSKeyword MIKE11_Path = target.GetKeyword("MIKE11_Path", 1);

            MIKE11_Path.GetParameter(1).ModifyFileNameParameter(mike11_21FilePath.Mike11_Path);
        }

        //耦合连接更新
        public static void Update_DefinitionSec(CoupleLinkList couplelinklist, ref PFSSection target)
        {
            //清空原默认couple文件中的连接集合
            int linkcount = target.GetKeyword("Number_of_CoupledBoundaries").GetParameter(1).ToInt();
            for (int i = 0; i < linkcount; i++)
            {
                target.DeleteSection("Definition", 1);  //注意这样循环删就始终用编号1才不会出错
            }

            //更新耦合数量
            PFSKeyword Number_of_CoupledBoundaries = target.GetKeyword("Number_of_CoupledBoundaries", 1);
            if (couplelinklist.Link_infolist == null) return;

            Number_of_CoupledBoundaries.GetParameter(1).ModifyIntParameter(couplelinklist.Link_infolist.Count);

            //重新逐个添加连接
            List<Link> Linklist = couplelinklist.Link_infolist;
            for (int i = 0; i < Linklist.Count; i++)
            {
                PFSSection Definition = target.InsertNewSection("Definition", i + 2);
                Insert_DefinitionKey(ref  Definition, Linklist[i]);
            }
        }

        //添加连接的关键字和参数
        public static void Insert_DefinitionKey(ref PFSSection Definition, Link link)
        {
            PFSKeyword Touched = Definition.InsertNewKeyword("Touched", 1);
            Touched.InsertNewParameterDouble(0, 1);
            PFSKeyword Linktype = Definition.InsertNewKeyword("Linktype", 2);
            if (link is Lateral_Link)
            {
                Linktype.InsertNewParameterDouble(2, 1);
            }
            else if (link is SideStructure_Link)
            {
                Linktype.InsertNewParameterDouble(9, 1);
            }
            else
            {
                Linktype.InsertNewParameterDouble(2, 1);
            }

            PFSKeyword HDLink = Definition.InsertNewKeyword("HDLink", 3);
            HDLink.InsertNewParameterBool(true, 1);
            PFSKeyword ADLink = Definition.InsertNewKeyword("ADLink", 4);
            ADLink.InsertNewParameterBool(false, 1);
            PFSKeyword M11_River_Name = Definition.InsertNewKeyword("M11_River_Name", 5);
            M11_River_Name.InsertNewParameterString(link.LinkReachSeg.reachname, 1);
            PFSKeyword Urban_ID = Definition.InsertNewKeyword("Urban_ID", 6);
            Urban_ID.InsertNewParameterString("", 1);
            PFSKeyword M11_Chainage = Definition.InsertNewKeyword("M11_Chainage", 7);
            M11_Chainage.InsertNewParameterDouble(link.LinkReachSeg.start_chainage, 1);
            M11_Chainage.InsertNewParameterDouble(link.LinkReachSeg.end_chainage, 2);
            PFSKeyword Side_of_River = Definition.InsertNewKeyword("Side_of_River", 8);
            int lr;
            if (link is Lateral_Link)
            {
                lr = (link as Lateral_Link).Left_Right;
            }
            else
            {
                lr = 0;
            }

            Side_of_River.InsertNewParameterInt(lr, 1);

            PFSKeyword Flow_Direction = Definition.InsertNewKeyword("Flow_Direction", 9);
            Flow_Direction.InsertNewParameterDouble(1, 1);
            PFSKeyword npoints = Definition.InsertNewKeyword("npoints", 10);
            npoints.InsertNewParameterDouble(1, 1);
            PFSKeyword points_j = Definition.InsertNewKeyword("points_j", 11);
            points_j.InsertNewParameterDouble(0, 1);
            PFSKeyword points_k = Definition.InsertNewKeyword("points_k", 12);
            points_k.InsertNewParameterDouble(0, 1);
            PFSKeyword npoints_FM = Definition.InsertNewKeyword("npoints_FM", 13);
            npoints_FM.InsertNewParameterInt(link.LinkPointList.Count, 1);

            //插入连接点x坐标关键字和参数
            PFSKeyword points_x = Definition.InsertNewKeyword("points_x", 14);
            List<object> xlist = new List<object>();

            for (int i = 0; i < link.LinkPointList.Count; i++)
            {
                xlist.Add(link.LinkPointList[i].X);
            }
            Nwk11.InsertKeyPars(ref points_x, xlist.ToArray());

            //插入连接点y坐标关键字和参数
            PFSKeyword points_y = Definition.InsertNewKeyword("points_y", 15);
            List<object> ylist = new List<object>();
            for (int i = 0; i < link.LinkPointList.Count; i++)
            {
                ylist.Add(link.LinkPointList[i].Y);
            }
            Nwk11.InsertKeyPars(ref points_y, ylist.ToArray());

            //插入连接点桩号关键字和参数
            PFSKeyword points_chainage = Definition.InsertNewKeyword("points_chainage", 16);

            List<object> chainagelist = new List<object>();
            for (int i = 0; i < link.LinkPointList.Count; i++)
            {
                chainagelist.Add(link.LinkPointList[i].pointchainage);
            }
            Nwk11.InsertKeyPars(ref points_chainage, chainagelist.ToArray());

            //其他默认关键字和参数
            PFSKeyword Implicit = Definition.InsertNewKeyword("Implicit", 17);
            Implicit.InsertNewParameterBool(false, 1);
            PFSKeyword Lateral_structure_params = Definition.InsertNewKeyword("Lateral_structure_params", 18);

            //定义侧向连接参数
            LateralPars lateralpars;
            if (link is Lateral_Link)
            {
                lateralpars = (link as Lateral_Link).Linkpars;
            }
            else
            {
                lateralpars.WeirC = 1.838;
                lateralpars.Depth_Tol = 0.1;
                lateralpars.Fric = 0.05;
                lateralpars.Smooth_factor = 0.5;
            }

            //如果是侧向建筑物，光滑因子参数取用侧向建筑物的
            if (link is SideStructure_Link)
            {
                lateralpars.Smooth_factor = (link as SideStructure_Link).Smooth_Factor;
            }

            //各关键字和参数赋值
            Lateral_structure_params.InsertNewParameterDouble(lateralpars.WeirC, 1);
            Lateral_structure_params.InsertNewParameterDouble(1.5, 2);
            Lateral_structure_params.InsertNewParameterDouble(lateralpars.Fric, 3);
            Lateral_structure_params.InsertNewParameterDouble(1.5, 4);
            PFSKeyword Lateral_structure_extfiles = Definition.InsertNewKeyword("Lateral_structure_extfiles", 19);
            Lateral_structure_extfiles.InsertNewParameterFileName("", 1);
            Lateral_structure_extfiles.InsertNewParameterFileName("", 2);
            PFSKeyword Lateral_structure_definition = Definition.InsertNewKeyword("Lateral_structure_definition", 20);
            Lateral_structure_definition.InsertNewParameterString("CelltoCell", 1);
            Lateral_structure_definition.InsertNewParameterString("Weir1", 2);
            Lateral_structure_definition.InsertNewParameterString("HGH", 3);
            PFSKeyword Lateral_structure_deptol = Definition.InsertNewKeyword("Lateral_structure_deptol", 21);
            Lateral_structure_deptol.InsertNewParameterDouble(lateralpars.Depth_Tol, 1);
            PFSKeyword M21_area_no = Definition.InsertNewKeyword("M21_area_no", 22);
            M21_area_no.InsertNewParameterDouble(1, 1);
            PFSKeyword M21_extrapolation_factor = Definition.InsertNewKeyword("M21_extrapolation_factor", 23);
            M21_extrapolation_factor.InsertNewParameterDouble(0, 1);
            PFSKeyword M21_momentum_factor = Definition.InsertNewKeyword("M21_momentum_factor", 24);
            M21_momentum_factor.InsertNewParameterDouble(0, 1);
            PFSKeyword M21_relaxation_factor = Definition.InsertNewKeyword("M21_relaxation_factor", 25);
            M21_relaxation_factor.InsertNewParameterDouble(lateralpars.Smooth_factor, 1);
            PFSKeyword M21_depth_distribution = Definition.InsertNewKeyword("M21_depth_distribution", 26);
            M21_depth_distribution.InsertNewParameterBool(false, 1);
            PFSKeyword M21_struc_addrep = Definition.InsertNewKeyword("M21_struc_addrep", 27);
            M21_struc_addrep.InsertNewParameterBool(false, 1);
            PFSKeyword M21_struc_dirtol = Definition.InsertNewKeyword("M21_struc_dirtol", 28);
            M21_struc_dirtol.InsertNewParameterDouble(5, 1);
            PFSKeyword M21_struc_cell_location = Definition.InsertNewKeyword("M21_struc_cell_location", 29);
            M21_struc_cell_location.InsertNewParameterDouble(-1, 1);
            PFSKeyword M21_flow0_xy = Definition.InsertNewKeyword("M21_flow0_xy", 30);
            M21_flow0_xy.InsertNewParameterBool(false, 1);
            M21_flow0_xy.InsertNewParameterBool(false, 2);
            PFSKeyword M21_flow0_dep_minmax = Definition.InsertNewKeyword("M21_flow0_dep_minmax", 31);
            M21_flow0_dep_minmax.InsertNewParameterDouble(0, 1);
            M21_flow0_dep_minmax.InsertNewParameterDouble(0, 2);
            PFSKeyword Urban = Definition.InsertNewKeyword("Urban", 32);
            object[] Urban_array = { 0, 0.1, 0.16, 0, 1, 0.98, 1, 1, 0 };
            Nwk11.InsertKeyPars(ref Urban, Urban_array);
            PFSKeyword RiverUrban = Definition.InsertNewKeyword("RiverUrban", 33);
            RiverUrban.InsertNewParameterDouble(0, 1);
        }
        #endregion


        #region ********************** 连接操作 -- 添加侧向连接和建筑物侧向连接***********************
        //根据给定的起止点，增加侧向耦合
        public static void Add_Lateral_Link(ref HydroModel hydromodel, PointXY start_point, PointXY end_point, ReachLR reachlr)
        {
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //获取距离2个点最近的河道和其桩号
            AtReach start_atreach = Nwk11.Get_NearReach(reachlist, start_point);
            AtReach end_atreach = Nwk11.Get_NearReach(reachlist, end_point);

            //判断两点是否在一条河道上
            if (start_atreach.reachname != end_atreach.reachname)
            {
                Console.WriteLine("两点不在同一条河道上!");
                return;
            }

            //获取河段并新增测向连接
            Reach_Segment reachsegment = Reach_Segment.Get_ReachSegment(start_atreach.reachname, start_atreach.chainage, end_atreach.chainage);
            Add_Lateral_Link(ref hydromodel, reachsegment, reachlr);
        }

        // 根据给定的测向连接河段，提取横断面数据，通过计算新增河道的侧向耦合连接，只选一侧则增加一个侧向连接，选两侧则增加两个侧向连接
        public static void Add_Lateral_Link(ref HydroModel hydromodel, Reach_Segment reachsegment, ReachLR reachlr)
        {
            CoupleLinkList couplelinklist = hydromodel.CoupleLinklist;

            //判断添加哪边的一边加侧向连接还是两边都加
            if (reachlr != ReachLR.left_right)   //一侧连接
            {
                //获取侧向耦合点
                Dictionary<double, PointXY> linkpointdic = Get_ConnectPointxydic(ref hydromodel, reachsegment, reachlr);


                //新建一个侧向连接对象
                List<ReachPoint> LinkPointList = new List<ReachPoint>();
                for (int i = 0; i < linkpointdic.Count; i++)
                {
                    ReachPoint reachpoint;
                    reachpoint.number = i + 1;
                    reachpoint.chainagetype = 0;
                    reachpoint.X = linkpointdic.Values.ElementAt(i).X;
                    reachpoint.Y = linkpointdic.Values.ElementAt(i).Y;
                    reachpoint.pointchainage = linkpointdic.Keys.ElementAt(i);
                    LinkPointList.Add(reachpoint);
                }

                //左右判断
                Lateral_Link newlateral_link;
                if (reachlr == ReachLR.left)
                {
                    newlateral_link = new Lateral_Link(reachsegment, LinkPointList, -1);
                }
                else
                {
                    newlateral_link = new Lateral_Link(reachsegment, LinkPointList, 1);
                }

                //向连接集合中添加一个连接
                couplelinklist.AddLink(newlateral_link);
            }
            else  //两侧都连接
            {
                //获取左侧耦合点
                Dictionary<double, PointXY> left_linkpointdic = Get_ConnectPointxydic(ref hydromodel, reachsegment, ReachLR.left);

                //新建一个左侧侧向连接对象
                List<ReachPoint> LinkPointList = new List<ReachPoint>();
                for (int i = 0; i < left_linkpointdic.Count; i++)
                {
                    ReachPoint reachpoint;
                    reachpoint.number = i + 1;
                    reachpoint.chainagetype = 0;
                    reachpoint.X = left_linkpointdic.Values.ElementAt(i).X;
                    reachpoint.Y = left_linkpointdic.Values.ElementAt(i).Y;
                    reachpoint.pointchainage = left_linkpointdic.Keys.ElementAt(i);
                    LinkPointList.Add(reachpoint);
                }
                Lateral_Link newleft_lateral_link = new Lateral_Link(reachsegment, LinkPointList, -1);

                //向连接集合中添加一个连接
                couplelinklist.AddLink(newleft_lateral_link);

                //获取右侧耦合点
                Dictionary<double, PointXY> right_linkpointdic = Get_ConnectPointxydic(ref hydromodel, reachsegment, ReachLR.right);

                //新建一个右侧侧向连接对象
                List<ReachPoint> LinkPointList1 = new List<ReachPoint>();
                for (int i = 0; i < left_linkpointdic.Count; i++)
                {
                    ReachPoint reachpoint;
                    reachpoint.number = i + 1;
                    reachpoint.chainagetype = 0;
                    reachpoint.X = left_linkpointdic.Values.ElementAt(i).X;
                    reachpoint.Y = left_linkpointdic.Values.ElementAt(i).Y;
                    reachpoint.pointchainage = left_linkpointdic.Keys.ElementAt(i);
                    LinkPointList1.Add(reachpoint);
                }
                Lateral_Link newright_lateral_link = new Lateral_Link(reachsegment, LinkPointList1, 1);

                //向连接集合中添加一个连接
                couplelinklist.AddLink(newright_lateral_link);
            }
        }

        //推求河道侧向耦合点坐标
        public static Dictionary<double, PointXY> Get_ConnectPointxydic(ref HydroModel hydromodel, Reach_Segment reachsegment, ReachLR reachlr)
        {
            Dictionary<double, PointXY> ConnectPointxydic = new Dictionary<double, PointXY>();
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //通过SectionList类自带的方法获取河段内断面的左右岸宽度
            SectionList sectionlist = hydromodel.Mike11Pars.SectionList;
            Dictionary<double, double> Section_LRwidth = sectionlist.Get_Section_LRwidth(reachsegment, reachlr);

            //推求各桩号对应的耦合点坐标
            for (int i = 0; i < Section_LRwidth.Count; i++)
            {
                double chainage = Section_LRwidth.Keys.ElementAt(i);
                double distance = Section_LRwidth.Values.ElementAt(i);
                ReachInfo reachinfo = reachlist.Get_Reachinfo(reachsegment.reachname);
                List<ReachPoint> reachpoint_list = reachinfo.reachpoint_list;

                //推求河道上包住该断面桩号的 起点和终点控制点坐标,以及河道中心线坐标
                ReachPoint start_reachpoint = reachpoint_list[0];
                ReachPoint end_reachpoint = reachpoint_list[1];
                PointXY sectionpoint;
                sectionpoint.X = 0;
                sectionpoint.Y = 0;
                for (int j = 0; j < reachpoint_list.Count - 1; j++)
                {
                    if (reachpoint_list[reachpoint_list.Count - 1].pointchainage == chainage)
                    {
                        PointXY section_point;
                        section_point.X = reachpoint_list[reachpoint_list.Count - 1].X;
                        section_point.Y = reachpoint_list[reachpoint_list.Count - 1].Y;
                        sectionpoint = section_point;
                        break;
                    }

                    if (reachpoint_list[j].pointchainage <= chainage && reachpoint_list[j + 1].pointchainage > chainage)
                    {
                        start_reachpoint = reachpoint_list[j];
                        end_reachpoint = reachpoint_list[j + 1];

                        PointXY section_point;
                        section_point.X = start_reachpoint.X + (end_reachpoint.X - start_reachpoint.X) * (chainage - reachpoint_list[j].pointchainage) / (reachpoint_list[j + 1].pointchainage - reachpoint_list[j].pointchainage);
                        section_point.Y = start_reachpoint.Y + (end_reachpoint.Y - start_reachpoint.Y) * (chainage - reachpoint_list[j].pointchainage) / (reachpoint_list[j + 1].pointchainage - reachpoint_list[j].pointchainage);
                        sectionpoint = section_point;
                        break;
                    }
                }

                //根据起点和终点控制点坐标、桩号、断面桩号
                PointXY point1000;
                if (reachlr == ReachLR.left)
                {
                    point1000.X = sectionpoint.X - 1000 * (end_reachpoint.Y - sectionpoint.Y) / (end_reachpoint.pointchainage - chainage);
                    point1000.Y = 1000 * (end_reachpoint.X - sectionpoint.X) / (end_reachpoint.pointchainage - chainage) + sectionpoint.Y;
                }
                else
                {
                    point1000.X = sectionpoint.X - 1000 * (end_reachpoint.Y - sectionpoint.Y) / (end_reachpoint.pointchainage - chainage);
                    point1000.Y = 1000 * (end_reachpoint.X - sectionpoint.X) / (end_reachpoint.pointchainage - chainage) + sectionpoint.Y;
                    point1000.X = 2 * sectionpoint.X - point1000.X;
                    point1000.Y = 2 * sectionpoint.Y - point1000.Y;
                }

                //最终耦合点
                PointXY point;
                point.X = sectionpoint.X + distance * (point1000.X - sectionpoint.X) / 1000;
                point.Y = sectionpoint.Y + distance * (point1000.Y - sectionpoint.Y) / 1000;

                ConnectPointxydic.Add(chainage, point);
            }

            Console.WriteLine("推求连接点坐标成功！");
            return ConnectPointxydic;
        }

        // 添加侧向建筑物(溃堤建筑物--分洪口)连接
        public static void Add_SideStructure_Link(ref HydroModel hydromodel, Fhkstr fhkstr)
        {
            CoupleLinkList couplelinklist = hydromodel.CoupleLinklist;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //先计算得到侧向建筑物上下游坐标，并存到集合里
            PointXY sideStructure_up;
            PointXY sideStructure_down;

            //获取最新mesh文件的边界点集合（标记为1的点为边界点）
            MeshFile meshfile = MeshFile.ReadMesh(hydromodel.Modelfiles.Mesh_filename);
            List<PointXY> BoundaryPoint = Mesh.Get_Mesh_BoundaryPointlist(meshfile);

            //获得侧向建筑物连接上下游连接点坐标
            GetSideStructure_XY(reachlist, BoundaryPoint, fhkstr, out sideStructure_up, out sideStructure_down);

            //将两个连接点加入集合
            List<PointXY> points = new List<PointXY>();
            points.Add(sideStructure_up);
            points.Add(sideStructure_down);

            //新建一个侧向建筑物连接对象
            List<ReachPoint> LinkPointList = new List<ReachPoint>();
            for (int i = 0; i < points.Count; i++)
            {
                ReachPoint reachpoint;
                reachpoint.number = i + 1;
                reachpoint.chainagetype = 0;
                reachpoint.X = points[i].X;
                reachpoint.Y = points[i].Y;
                reachpoint.pointchainage = 0;
                LinkPointList.Add(reachpoint);
            }

            Reach_Segment reachsegment;
            reachsegment.reachname = fhkstr.Reachinfo.reachname;
            reachsegment.reachtopoid = fhkstr.Reachinfo.reachid;
            reachsegment.start_chainage = fhkstr.Reachinfo.chainage;
            reachsegment.end_chainage = 0;
            SideStructure_Link newside_structure_link = new SideStructure_Link(reachsegment, LinkPointList);

            //向连接集合中添加一个连接
            couplelinklist.AddLink(newside_structure_link);
        }

        // 获得侧向建筑物连接上下游连接点坐标
        public static void GetSideStructure_XY(ReachList reachlist, List<PointXY> BoundaryPoint, Fhkstr fhkstr, out PointXY sideStructure_up, out PointXY sideStructure_down)
        {
            //获取分洪口所在的河道控制点集合
            ReachInfo reachinfo = reachlist.Get_Reachinfo(fhkstr.Reachinfo.reachname);
            List<ReachPoint> reachPoint = reachinfo.reachpoint_list;

            //重新定义溃口点
            ReachPoint breakPoint;
            PointXY breakpointxy = reachlist.Get_ReachPointxy(fhkstr.Reachinfo.reachname, fhkstr.Reachinfo.chainage);
            breakPoint.X = breakpointxy.X;
            breakPoint.Y = breakpointxy.Y;
            breakPoint.pointchainage = fhkstr.Reachinfo.chainage;
            breakPoint.chainagetype = 1;
            breakPoint.number = 1;

            //分洪口宽度
            double sideStructureLongth = fhkstr.Stratts.gate_width;

            //通过与二维边界控制点(标记为1的点)的距离匹配，在二维范围内找到与河道控制点对应的点
            Dictionary<double, PointXY> chaing_InZoneRiverPoint;  //键为河道控制点桩号，值为二维范围内的点
            GetRelativeRiver_InZone(BoundaryPoint, reachPoint, out chaing_InZoneRiverPoint);

            //找到溃口点在蓄洪区内部自定义河道对应的点
            PointXY breakPoint_InZone = InsertX_Y(chaing_InZoneRiverPoint, fhkstr.Reachinfo.chainage);

            int breakPoint_No = 0;  //定义并初始化溃口点在河道控制点中的排序
            for (int i = 0; i < reachPoint.Count; i++)  //遍历河道控制点，寻找溃口点在河道控制点中的排序
            {
                if (reachPoint[i].X == breakPoint.X && reachPoint[i].Y == breakPoint.Y && reachPoint[i].pointchainage == breakPoint.pointchainage && reachPoint[i].number == breakPoint.number)  //坐标、桩号、排序都相等
                {
                    breakPoint_No = i;
                }
            }

            if (breakPoint.pointchainage == chaing_InZoneRiverPoint.Keys.ElementAt(0))  //溃口点是耦合范围内河道的第一个控制点，此时，只能从点breakPoint_InZone（溃口点在蓄洪区内部自定义河道对应的点）向下游新建长度为sideStructureLongth的侧向建筑物
            {
                sideStructure_down = InsertX_Y(chaing_InZoneRiverPoint, breakPoint.pointchainage + sideStructureLongth);  //侧向建筑物下游控制点
                sideStructure_up = breakPoint_InZone;    //侧向建筑物上游控制点坐标
            }
            else if (breakPoint.pointchainage == chaing_InZoneRiverPoint.Keys.ElementAt(chaing_InZoneRiverPoint.Count - 1))  //溃口点是耦合范围内河道的最后一个控制点，此时，只能从点breakPoint_InZone（溃口点在蓄洪区内部自定义河道对应的点）向上游新建长度为sideStructureLongth的侧向建筑物
            {
                sideStructure_up = InsertX_Y(chaing_InZoneRiverPoint, breakPoint.pointchainage - sideStructureLongth);    //溃口点前面一个河道控制点在蓄洪区内部自定义河道对应的点
                sideStructure_down = breakPoint_InZone;
            }
            else  //溃口点在耦合段中间，此时，只能点breakPoint_InZone（溃口点在蓄洪区内部自定义河道对应的点）向上下上游各新建长度为sideStructureLongth/2的侧向建筑物
            {
                sideStructure_up = InsertX_Y(chaing_InZoneRiverPoint, breakPoint.pointchainage - sideStructureLongth / 2);    //溃口点前面一个河道控制点在蓄洪区内部自定义河道对应的点
                sideStructure_down = InsertX_Y(chaing_InZoneRiverPoint, breakPoint.pointchainage + sideStructureLongth / 2);  //溃口点后面一个河道控制点在蓄洪区内部自定义河道对应的点

                //根据实际溃口宽度重新内插连接点
                double distance = PointXY.Get_ptop_distance(sideStructure_up.X, sideStructure_up.Y, sideStructure_down.X, sideStructure_down.Y);
                PointXY centerpoint;
                centerpoint.X = (sideStructure_up.X + sideStructure_down.X) / 2;
                centerpoint.Y = (sideStructure_up.Y + sideStructure_down.Y) / 2;

                PointXY new_uppoint;
                new_uppoint.X = centerpoint.X + (sideStructure_up.X - centerpoint.X) * (fhkstr.Stratts.gate_width / 2) / (distance / 2);
                new_uppoint.Y = centerpoint.Y + (sideStructure_up.Y - centerpoint.Y) * (fhkstr.Stratts.gate_width / 2) / (distance / 2);

                PointXY new_downpoint;
                new_downpoint.X = centerpoint.X + (sideStructure_down.X - centerpoint.X) * (fhkstr.Stratts.gate_width / 2) / (distance / 2);
                new_downpoint.Y = centerpoint.Y + (sideStructure_down.Y - centerpoint.Y) * (fhkstr.Stratts.gate_width / 2) / (distance / 2);

                sideStructure_up = new_uppoint;
                sideStructure_down = new_downpoint;
            }
        }

        // 得到河道控制点桩号对应的二维范围内点
        public static void GetRelativeRiver_InZone(List<PointXY> BoundaryPoint, List<ReachPoint> reachPoint, out Dictionary<double, PointXY> chainge_InZoneRiverPoint)
        {
            chainge_InZoneRiverPoint = new Dictionary<double, PointXY>();  //定义一个键值对，存储蓄洪区外河流控制点的序号与蓄洪区内部自定义河流的关系
            double searchDistance = Model_Const.SIDESTR_MAXCONNECT_DISTANCE;  //最大距离所属范围

            for (int i = 0; i < reachPoint.Count; i++)  //循环河道控制点
            {
                PointXY closestPoint = new PointXY(); ;  //声明离河道第i个控制点最近的点
                double shortestDistance = 0;  //声明并初始化最近的点与河道控制点距离
                int numberPoint = 0;  //用来记录第几个在寻找范围内的蓄洪区边界控制点

                for (int j = 0; j < BoundaryPoint.Count; j++)  //循环蓄洪区边界控制点
                {
                    double distance = Math.Sqrt(Math.Pow(reachPoint[i].X - BoundaryPoint[j].X, 2) + Math.Pow(reachPoint[i].Y - BoundaryPoint[j].Y, 2));  //河道上的第i个点与蓄洪区边界的第j个点之间的距离
                    if (distance <= searchDistance)
                    {
                        if (numberPoint == 0)  //寻找到的第一个在寻找范围内的蓄洪区边界点
                        {
                            closestPoint.X = BoundaryPoint[j].X;
                            closestPoint.Y = BoundaryPoint[j].Y;
                            shortestDistance = distance;
                        }
                        else  //numberPoint != 0 不是第一个，则需要比较哪一个点更近
                        {
                            if (distance < shortestDistance)  //第j个蓄洪区边界点更近，则把此点的坐标赋值给最近点的坐标
                            {
                                closestPoint.X = BoundaryPoint[j].X;
                                closestPoint.Y = BoundaryPoint[j].Y;
                                shortestDistance = distance;
                            }
                        }
                        numberPoint += 1;
                    }
                }

                if (numberPoint != 0)  //说明在蓄洪区边界控制点中找到了河道第i个控制点的最近点
                {
                    PointXY InZoneRiverPoint = new PointXY();  //河道第i个控制点在蓄洪区内部的对应点
                    InZoneRiverPoint.X = 2 * closestPoint.X - reachPoint[i].X;
                    InZoneRiverPoint.Y = 2 * closestPoint.Y - reachPoint[i].Y;

                    chainge_InZoneRiverPoint.Add(reachPoint[i].pointchainage, InZoneRiverPoint);
                }
            }
        }

        // 内插河道上的桩号在蓄洪区内对应的点
        public static PointXY InsertX_Y(Dictionary<double, PointXY> chainage_point, double chainage)
        {
            PointXY points;
            points.X = 0; //赋一个初始值
            points.Y = 0; //赋一个初始值
            List<double> Chainage = new List<double>(chainage_point.Keys);
            for (int i = 0; i < Chainage.Count; i++)
            {
                if (chainage == Chainage[i])
                {
                    points.X = chainage_point[Chainage[i]].X;
                    points.Y = chainage_point[Chainage[i]].Y;
                }
                else if ((i != Chainage.Count - 1) && (chainage > Chainage[i]) && (chainage < Chainage[i + 1]))
                {
                    points.X = (chainage_point[Chainage[i + 1]].X - chainage_point[Chainage[i]].X) * (chainage - Chainage[i]) / (Chainage[i + 1] - Chainage[i]) + chainage_point[Chainage[i]].X;  //桩号在蓄洪区对应的X
                    points.Y = (chainage_point[Chainage[i + 1]].Y - chainage_point[Chainage[i]].Y) * (chainage - Chainage[i]) / (Chainage[i + 1] - Chainage[i]) + chainage_point[Chainage[i]].Y;  //桩号在蓄洪区对应的Y
                }
            }
            return points;
        }
        #endregion ************************************************************************************
    }


    //保存最新所有连接的类
    [Serializable]
    public class CoupleLinkList
    {
        //**********************构造函数*********************
        public CoupleLinkList()
        {
            SetDefault();
        }

        //**********************属性************************
        //连接信息
        public List<Link> Link_infolist
        { get; set; }

        //**********************方法************************
        public void SetDefault()
        {
            List<Link> Link_List = new List<Link>();

            this.Link_infolist = Link_List;
        }

        //增加新连接方法
        public void AddLink(Link newlink)
        {
            if (!Link_infolist.Contains(newlink))
            {
                //往集合中增加新产汇流流域
                Link_infolist.Add(newlink);
            }
        }

        //去除指定河道的所有侧向连接的静态方法
        public void Remove_ReachALL_LateralLink(string reachname)
        {
            for (int i = 0; i < Link_infolist.Count; i++)
            {
                if (Link_infolist[i] is Lateral_Link && Link_infolist[i].LinkReachSeg.reachname == reachname)
                {
                    Link_infolist.RemoveAt(i);
                }
            }
        }

        //去除指定侧向建筑物连接
        public void Remove_Sidestr_Link(Fhkstr fhkstr)
        {
            AtReach atreach = fhkstr.Reachinfo;
            for (int i = 0; i < Link_infolist.Count; i++)
            {
                if (Link_infolist[i] is SideStructure_Link
                    && Link_infolist[i].LinkReachSeg.reachname == atreach.reachname
                    && Link_infolist[i].LinkReachSeg.start_chainage == atreach.chainage)
                {
                    Link_infolist.RemoveAt(i);
                }
            }
        }

        //根据河道名获取连接集合的方法
        public List<Link> Get_LinkList(string reachname)
        {
            List<Link> linklist = new List<Link>();

            for (int i = 0; i < Link_infolist.Count; i++)
            {
                if (Link_infolist[i].LinkReachSeg.reachname == reachname)
                {
                    linklist.Add(Link_infolist[i]);
                }
            }
            return linklist;
        }


    }

    //侧向建筑物连接类(Side structure)
    [Serializable]
    public class SideStructure_Link : Link
    {
        //构造函数

        //默认参数构造函数
        public SideStructure_Link(Reach_Segment LinkReachSeg, List<ReachPoint> LinkPointList)
            : base(9, LinkReachSeg, LinkPointList, 0)
        {
            SetDefault();
        }

        //带光滑因子的构造函数
        public SideStructure_Link(Reach_Segment LinkReachSeg, List<ReachPoint> LinkPointList, double smooth_factor)
            : base(9, LinkReachSeg, LinkPointList, 0)
        {
            SetDefault();
        }

        //属性
        public double Smooth_Factor   //光滑因子
        { get; set; }

        //设置默认属性方法
        public void SetDefault()
        {
            this.Smooth_Factor = 0.2;
        }
    }

    //侧向连接类(Lateral)
    [Serializable]
    public class Lateral_Link : Link
    {
        //构造函数

        //有参数构造函数
        public Lateral_Link(Reach_Segment LinkReachSeg, List<ReachPoint> LinkPointList, int Left_Right)
            : base(2, LinkReachSeg, LinkPointList, Left_Right)
        {
            SetDefault();
        }

        //有参数构造函数
        public Lateral_Link(Reach_Segment LinkReachSeg, List<ReachPoint> LinkPointList, int Left_Right, LateralPars Linkpars)
            : base(2, LinkReachSeg, LinkPointList, Left_Right)
        {
            SetDefault();
            this.Linkpars = Linkpars;
        }

        //属性
        public LateralPars Linkpars   //侧向连接参数参数
        { get; set; }

        //设置默认属性方法
        public void SetDefault()
        {
            LateralPars linkpars;
            linkpars.WeirC = 1.838;
            linkpars.Depth_Tol = 0.1;
            linkpars.Fric = 0.08;
            linkpars.Smooth_factor = 0.2;
            this.Linkpars = linkpars;
        }
    }

    //Link连接基类
    [Serializable]
    public class Link
    {
        //构造函数

        //构造函数
        public Link(int linktype, Reach_Segment LinkReachSeg, List<ReachPoint> LinkPointList, int Left_Right)
        {
            Link_SetDefault();
            this.LinkType = linktype;
            this.LinkReachSeg = LinkReachSeg;
            this.LinkPointList = LinkPointList;
            this.Left_Right = Left_Right;
        }

        //属性
        public int LinkType       //连接类型 2-Lateral(侧向连接) 9-Side Structure(侧向建筑物连接) 
        { get; set; }

        public Reach_Segment LinkReachSeg    //连接河道信息--名称、起止桩号
        { get; set; }

        public List<ReachPoint> LinkPointList    //连接点集合
        { get; set; }

        public int Left_Right  //左右 0为中 -1为左，1为右
        { get; set; }

        //方法

        //设置默认属性
        public void Link_SetDefault()
        {
            this.LinkType = 2;

            List<ReachPoint> LinkPointList = new List<ReachPoint>();
            ReachPoint point;
            point.number = 1;
            point.X = 0;
            point.Y = 0;
            point.pointchainage = 0;
            point.chainagetype = 0;
            LinkPointList.Add(point);
            this.LinkPointList = LinkPointList;

            Reach_Segment Linkreach;
            Linkreach.reachname = "";
            Linkreach.start_chainage = 0;
            Linkreach.end_chainage = 0;
            Linkreach.reachtopoid = Linkreach.reachname + Model_Const.REACHID_HZ;
            this.LinkReachSeg = Linkreach;

            this.Left_Right = -1;
        }

    }


}