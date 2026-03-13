using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using DHI.PFS;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using DHI.Generic.MikeZero.DFS.mesh;
using System.Windows.Forms;
using System.Diagnostics;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model;

namespace bjd_model.Mike21
{
    //命令枚举
    public enum ShowCommands : int
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
        SW_FORCEMINIMIZE = 11,
        SW_MAX = 11
    }

    // 控制点所在边界的信息
    public struct ARCS_POINTS
    {
        public int arcNo;  //控制点所在边界的排序
        public int connectNo;  //控制点在该边界中的连接顺序
    }

    //边界点结构体1
    public struct BoundaryPoint1
    {
        public int arrangeNo;  //所有控制点总的排序
        public int beginOrEnd;  //1为第一个点或最后一个点，0为中间点
        public double X;
        public double Y;
    }

    //边界点结构体2
    public struct BoundaryPoint
    {
        public int arcNo;  //点所在边界序号
        public double X;  //x坐标
        public double Y;  //y坐标
        public int Z;  //存储是不是开始点/结束点
        public int connectNo;  //点在边界中的连接序号
        public int No;  //按照横坐标的大小排列顺序
    }

    //MESH剖分的相关参数
    public struct Meshparameters
    {
        public double Maximum_Area;  //网格最大面积
        public double Minimum_Angle;  //网格最小角度
    }

    public static class Mdf
    {
        #region ********************** 制作新的mdf文件，并导出mesh文件 *****************************
        //调用C++编的动态库里的键盘方法，在这里需要先声明方法定义
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        //调用C++编的动态库里的执行方法
        [DllImport("shell32.dll")]   //为函数单独引用动态库的方法,需要标记为extern(外部的)才行
        public static extern IntPtr ShellExecute(IntPtr hwnd, string IpOperation, string IpFile, string IpParameters, string IpDirectory, ShowCommands nShowCmd);


        //根据前端给定的边界，创建新mesh、糙率，并作为新模型的mesh文件
        public static void Create_New_MdfMesh(ref HydroModel hydromodel, List<List<PointXY>> boundaryPoint)
        {
            string mdf_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Mesh_filename) + @"\" + "new.mdf";
            string mesh_filename = hydromodel.Modelfiles.Mesh_filename;
            string caolv_filename = hydromodel.Modelfiles.M21caolv_filename;
            if (File.Exists(mdf_filename))
            {
                File.Delete(mdf_filename);
            }

            if (File.Exists(mesh_filename))
            {
                File.Delete(mesh_filename);
            }

            if (File.Exists(caolv_filename))
            {
                File.Delete(caolv_filename);
            }

            MeshPars meshpars = hydromodel.Mike21Pars.MeshParsList;

            //第1步，创建一个空的mdf文件
            SubArea domain = Get_Domain(boundaryPoint);
            Build_BlankMdf(mdf_filename, domain, hydromodel.ModelGlobalPars.Coordinate_type);

            //第2步，打开该mdf文件（默认是mike zero打开）,导入边界并均匀化
            PFSFile mdffile = new PFSFile(mdf_filename, false);
            ImportBoundary(ref mdffile, boundaryPoint, meshpars.Mesh_Sidelength);
            mdffile.Write(mdf_filename);

            //第3步，使用外挂方式生成mesh,并光滑处理,mesh文件先不插值
            GenerateMesh(mdf_filename, mesh_filename, caolv_filename, meshpars.Mesh_Sidelength);

            //采用Gis服务内插mesh顶点地面高程
            Mesh.EditMesh_Interpolate_Allnodes(mesh_filename);

            //采用Gis服务内插糙率DFSU元素糙率值
            Dfsu.EditDfsu_Interpolate_Allnodes(caolv_filename);

            //更新meshparsl参数
            meshpars.Update_Meshpars_FromMeshFile(mesh_filename);
            meshpars.Mesh_Filename = mesh_filename;

            //更新全局参数中的mesh文件地址
            Model_Files model_file = hydromodel.Modelfiles;
            model_file.Mesh_filename = mesh_filename;

            //更新mike21的参数 -- 堤防和堰清0
            Mike21_Pars mike21pars = hydromodel.Mike21Pars;
            DikeList DikeList = new DikeList();
            DikeList.Dike_List = new List<Dike>();
            DikeList.MaxDikeId = 0;
            mike21pars.DikeList = DikeList;

            WeirList WeirList = new WeirList();
            WeirList.Weir_List = new List<Weir>();
            WeirList.MaxWeirId = 0;
            mike21pars.WeirList = WeirList;
        }

        //根据前端给定的边界，创建新mesh并与原mesh合并成新的更大的mesh
        public static void Create_NewMesh_AndMerge(ref HydroModel hydromodel, List<List<PointXY>> boundaryPoint)
        {
            string now_meshfilename = hydromodel.Modelfiles.Mesh_filename;
            string now_caolvfilename = hydromodel.Modelfiles.M21caolv_filename;

            if (!File.Exists(now_meshfilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, now_meshfilename);
            }

            string newmesh_filename_tmp = Path.GetDirectoryName(now_meshfilename) + "\\tmp.mesh";
            MeshPars meshpars = hydromodel.Mike21Pars.MeshParsList;

            string newcaolv_filename_tmp = Path.GetDirectoryName(now_meshfilename) + "\\tmp.dfsu";

            //第1步，创建一个空的mdf文件
            SubArea domain = Get_Domain(boundaryPoint);
            string newmdf_filename_tmp = Path.GetDirectoryName(now_meshfilename) + "\\tmp.mdf";
            Build_BlankMdf(newmdf_filename_tmp, domain, hydromodel.ModelGlobalPars.Coordinate_type);

            //第2步，打开该mdf文件（默认是mike zero打开）,导入边界并均匀化
            PFSFile mdffile = new PFSFile(newmdf_filename_tmp, false);
            ImportBoundary(ref mdffile, boundaryPoint, meshpars.Mesh_Sidelength);
            mdffile.Write(newmdf_filename_tmp);

            //第3步，使用外挂方式生成mesh,并光滑处理,mesh文件先不插值
            GenerateMesh(newmdf_filename_tmp, newmesh_filename_tmp, newcaolv_filename_tmp, meshpars.Mesh_Sidelength);

            //采用Gis服务内插临时mesh顶点地面高程
            Mesh.EditMesh_Interpolate_Allnodes(newmesh_filename_tmp);

            //采用Gis服务内插临时DFSU元素糙率值
            Dfsu.EditDfsu_Interpolate_Allnodes(newcaolv_filename_tmp);

            //合并mesh
            List<string> sourcefilenamelist = new List<string>();
            sourcefilenamelist.Add(now_meshfilename);
            sourcefilenamelist.Add(newmesh_filename_tmp);
            Merge_Mesh(sourcefilenamelist, now_meshfilename);

            //合并糙率Dfsu
            string new_hebing_caolvfilename = Path.GetDirectoryName(now_meshfilename) + "\\hebing_tmp.dfsu";
            Dfsu.Merge_Dfsu(now_caolvfilename, newcaolv_filename_tmp, new_hebing_caolvfilename);
            File.Copy(new_hebing_caolvfilename, now_caolvfilename, true);
            File.Delete(new_hebing_caolvfilename);

            //更新meshpars参数
            meshpars.Update_Meshpars_FromMeshFile(now_meshfilename);
        }
        #endregion ********************************************************************************


        #region *************************** 制作新的mdf、mesh的子函数 *****************************
        //获取边界点的最大最小范围,返回数据区域
        public static SubArea Get_Domain(List<List<PointXY>> boundaryPoint)
        {
            SubArea domain;
            double max_x = boundaryPoint[0][0].X;
            double max_y = boundaryPoint[0][0].Y;
            double min_x = boundaryPoint[0][0].X;
            double min_y = boundaryPoint[0][0].Y;
            for (int i = 0; i < boundaryPoint.Count; i++)
            {
                for (int j = 0; j < boundaryPoint[i].Count; j++)
                {
                    if (boundaryPoint[i][j].X > max_x)
                    {
                        max_x = boundaryPoint[i][j].X;
                    }

                    if (boundaryPoint[i][j].Y > max_y)
                    {
                        max_y = boundaryPoint[i][j].Y;
                    }

                    if (boundaryPoint[i][j].X < min_x)
                    {
                        min_x = boundaryPoint[i][j].X;
                    }

                    if (boundaryPoint[i][j].Y < min_y)
                    {
                        min_y = boundaryPoint[i][j].Y;
                    }
                }
            }
            domain.x1 = min_x - 500;
            domain.y1 = min_y - 500;
            domain.x2 = max_x + 500;
            domain.y2 = max_y + 500;
            return domain;
        }

        // 新建一个空的mdf文件，坐标采用全局变量现在的坐标
        public static void Build_BlankMdf(string newmdfFilePath, SubArea domain, string Projection)
        {
            PFSBuilder mdfbuilder = new PFSBuilder();
            mdfbuilder.AddTarget("MESH_EDITOR_SCATTERDATAFILE_SETTINGS");  //第一个目标
            mdfbuilder.AddSection("MESH_EDITOR_SCATTERDATAFILE");
            mdfbuilder.EndSection();  //MESH_EDITOR_SCATTERDATAFILE
            mdfbuilder.EndSection();  //MESH_EDITOR_SCATTERDATAFILE_SETTINGS，结束第一个目标

            mdfbuilder.AddTarget("MESH_EDITOR_BACKGROUND_SETTINGS");  //第二个目标
            mdfbuilder.AddSection("IMAGE_FILES");
            mdfbuilder.EndSection();  //IMAGE_FILES
            mdfbuilder.EndSection();  //MESH_EDITOR_BACKGROUND_SETTINGS,结束第二个目标

            mdfbuilder.AddTarget("MESH_DATA");  //第三个目标
            mdfbuilder.AddKeywordValues("Save_Data_As_String", true);
            mdfbuilder.AddKeywordValues("version", 600);
            mdfbuilder.AddKeywordValues("Interpolate_Using_String_Data", true);

            mdfbuilder.AddSection("DATA_AREA");
            mdfbuilder.AddKeywordValues("x0", domain.x1);
            mdfbuilder.AddKeywordValues("y0", domain.y1);
            mdfbuilder.AddKeywordValues("x1", domain.x2);
            mdfbuilder.AddKeywordValues("y1", domain.y2);

            mdfbuilder.AddKeywordValues("UTMZone", Projection);
            mdfbuilder.EndSection();  //DATA_AREA

            mdfbuilder.AddSection("INTERNAL");
            mdfbuilder.AddKeywordValues("Use_Fast_Point_Read", true);
            mdfbuilder.EndSection();  //INTERNAL

            mdfbuilder.AddSection("TRIANGULATION_OPTIONS");
            mdfbuilder.AddKeywordValues("DefaultIsSet", true);
            mdfbuilder.AddKeywordValues("Maximum_Area", 50000);
            mdfbuilder.AddKeywordValues("Minimum_Angle", 26);
            mdfbuilder.AddKeywordValues("Limit_Num_Elements", true);
            mdfbuilder.AddKeywordValues("Max_Num_Elements", 100000);
            mdfbuilder.AddKeywordValues("Use_Command_Line", false);
            mdfbuilder.AddKeywordValues("Command_Line", "pzAenq26a100");
            mdfbuilder.EndSection();  //TRIANGULATION_OPTIONS

            mdfbuilder.AddSection("SMOOTHING_OPTIONS_1");
            mdfbuilder.AddKeywordValues("Iterations", 10);
            mdfbuilder.AddKeywordValues("ConstrainedSmoothing", 0);
            mdfbuilder.AddKeywordValues("ArcNodesFixed", 1);
            mdfbuilder.EndSection();  //SMOOTHING_OPTIONS_1

            mdfbuilder.AddSection("INTERPOLATION_OPTION");
            mdfbuilder.AddKeywordValues("Method", 0);
            mdfbuilder.AddKeywordValues("Truncate", false);
            mdfbuilder.AddKeywordValues("Truncate_Min", -1e+030);
            mdfbuilder.AddKeywordValues("Truncate_Max", 1e+030);
            mdfbuilder.AddKeywordValues("Extrapolate", 0);
            mdfbuilder.AddKeywordValues("CopyValue", false);
            mdfbuilder.AddKeywordValues("MaxDistance", 100);
            mdfbuilder.AddKeywordValues("InverseDistanceWeightedP", 2);
            mdfbuilder.AddKeywordValues("BoundaryMethod", 0);
            mdfbuilder.AddSection("NATURAL_NEIGHBOUR_OPTION");
            mdfbuilder.AddKeywordValues("Extrapolate", 1);
            mdfbuilder.AddKeywordValues("Bounding_Window_Size", 1000);
            mdfbuilder.EndSection();  //NATURAL_NEIGHBOUR_OPTION
            mdfbuilder.EndSection();  //INTERPOLATION_OPTION

            mdfbuilder.AddSection("REFINEMENT_OPTIONS");
            mdfbuilder.AddKeywordValues("Factor", 1.5);
            mdfbuilder.AddKeywordValues("Iterations", 5);
            mdfbuilder.EndSection();  //REFINEMENT_OPTIONS

            mdfbuilder.AddSection("MESHEDITING_OPTIONS");
            mdfbuilder.AddKeywordValues("DeleteMeshNodeAction", 0);
            mdfbuilder.AddKeywordValues("CodeValueSetting", 0);
            mdfbuilder.AddKeywordValues("CodeValue", 1);
            mdfbuilder.AddKeywordValues("ReTriangulateAddNode", 0);
            mdfbuilder.EndSection();  //MESHEDITING_OPTIONS

            mdfbuilder.AddSection("ANALYSEMESH_OPTIONS");
            mdfbuilder.AddKeywordValues("AnalyseMethod", 0);
            mdfbuilder.AddKeywordValues("WaterLevel", 0);
            mdfbuilder.AddKeywordValues("WaterDepth", 1);
            mdfbuilder.AddKeywordValues("CFL", 0.8);
            mdfbuilder.AddKeywordValues("RowCount", 10);
            mdfbuilder.EndSection();  //ANALYSEMESH_OPTIONS

            mdfbuilder.AddSection("PRIORITIZE_DATA_OPTIONS");
            mdfbuilder.AddKeywordValues("Use_Prioritization", false);
            mdfbuilder.AddKeywordValues("Global_Prioritization", 0);
            mdfbuilder.AddSection("WEIGHTS");
            mdfbuilder.EndSection();  //WEIGHTS
            mdfbuilder.AddSection("LOCAL_PRIORITIZATION_POLYGON_LIST");
            mdfbuilder.EndSection();  //LOCAL_PRIORITIZATION_POLYGON_LIST
            mdfbuilder.EndSection();  //PRIORITIZE_DATA_OPTIONS

            mdfbuilder.AddSection("POINTS");
            mdfbuilder.AddKeywordValues("Data", "0");
            mdfbuilder.EndSection();  //POINTS

            mdfbuilder.AddSection("ARCS");
            mdfbuilder.AddKeywordValues("Data", "0");
            mdfbuilder.EndSection();  //ARCS

            mdfbuilder.AddSection("BREAKER_LINES");
            mdfbuilder.AddKeywordValues("Data", "0");
            mdfbuilder.EndSection();  //BREAKER_LINES

            mdfbuilder.AddSection("POLYGONS");
            mdfbuilder.AddKeywordValues("Data", "0");
            mdfbuilder.EndSection();  //POLYGONS

            mdfbuilder.AddSection("MESH");
            mdfbuilder.AddKeywordValues("Data", 0);
            mdfbuilder.AddKeywordValues("Nodes", 0);
            mdfbuilder.AddKeywordValues("Max_Node_Count", 500000);
            mdfbuilder.AddKeywordValues("Elements", 0);
            mdfbuilder.AddKeywordValues("Max_Element_Count", 500000);
            mdfbuilder.AddKeywordValues("Segments", 0);
            mdfbuilder.AddKeywordValues("Max_Segment_Count", 500000);
            mdfbuilder.EndSection();  //MESH

            mdfbuilder.AddSection("CURVE_OBJECT");
            mdfbuilder.AddKeywordValues("Data", "0 0 0 1 1 1 152 187 0 0 0 0 5 Curve 1 2 1 1 1  152 187 1");
            mdfbuilder.EndSection();  //CURVE_OBJECT

            mdfbuilder.EndSection();  //MESH_DATA,结束第三个目标

            mdfbuilder.Write(newmdfFilePath);
            Console.WriteLine("空白mdf文件新建成功！");
        }

        // 根据用户给定点集合导入边界
        public static void ImportBoundary(ref PFSFile mdffile, List<List<PointXY>> boundaryPoint, double meshsidelength)
        {
            //根据用户给定的边界控制点，将边界控制点插值均匀化
            List<List<PointXY>> boundaryControlPoint = InsertBoundaryPoint(boundaryPoint, meshsidelength);

            Dictionary<ARCS_POINTS, BoundaryPoint1> arc_BoundaryPoint;  //边界与控制点的关系（键值对集合）
            double min_X;  //边界控制点X最小值
            double min_Y;  //边界控制点Y最小值
            double max_X;  //边界控制点X最大值
            double max_Y;  //边界控制点Y最大值
            ArrangeBoundaryPoint(boundaryControlPoint, out arc_BoundaryPoint, out min_X, out min_Y, out max_X, out max_Y);

            string POINTS_Data = GetControlPoint(boundaryControlPoint, arc_BoundaryPoint);  //控制点关键字的参数
            string ARCS_Data = GetConnectNo(boundaryControlPoint, arc_BoundaryPoint);  //边界连接信息关键字的参数

            PFSSection MESH_DATA = mdffile.GetTarget("MESH_DATA", 1);  //目标

            PFSSection DATA_AREA = MESH_DATA.GetSection("DATA_AREA", 1);
            PFSKeyword x0 = DATA_AREA.GetKeyword("x0", 1);
            x0.DeleteParameter(1);
            x0.InsertNewParameterDouble(min_X, 1);
            PFSKeyword y0 = DATA_AREA.GetKeyword("y0", 1);
            y0.DeleteParameter(1);
            y0.InsertNewParameterDouble(min_Y, 1);
            PFSKeyword x1 = DATA_AREA.GetKeyword("x1", 1);
            x1.DeleteParameter(1);
            x1.InsertNewParameterDouble(max_X, 1);
            PFSKeyword y1 = DATA_AREA.GetKeyword("y1", 1);
            y1.DeleteParameter(1);
            y1.InsertNewParameterDouble(max_Y, 1);

            PFSSection POINTS = MESH_DATA.GetSection("POINTS", 1);
            PFSKeyword Data = POINTS.GetKeyword("Data", 1);
            Data.GetParameter(1).ModifyStringParameter(POINTS_Data);

            PFSSection ARCS = MESH_DATA.GetSection("ARCS", 1);
            PFSKeyword Data1 = ARCS.GetKeyword("Data", 1);
            Data1.GetParameter(1).ModifyStringParameter(ARCS_Data);

            Console.WriteLine("边界导入成功！");
        }

        // 根据用户给定的边界控制点，将边界控制点插值均匀化
        public static List<List<PointXY>> InsertBoundaryPoint(List<List<PointXY>> boundaryPoint, double sidelength)
        {
            List<List<PointXY>> boundaryControlPoint = new List<List<PointXY>>();  //存储插值后边界控制点集合的集合

            for (int i = 0; i < boundaryPoint.Count; i++)  //循环各边界点的集合
            {
                List<PointXY> boundaryControlPoint_i = new List<PointXY>();  //存储该边界控制点的集合

                for (int j = 0; j < boundaryPoint[i].Count; j++)  //循环该边界控制点
                {
                    boundaryControlPoint_i.Add(boundaryPoint[i][j]);

                    if (j < boundaryPoint[i].Count - 1)  //判断是不是最后一个点，因为最后一个点不需要插值
                    {
                        List<PointXY> point = InsertPoint(boundaryPoint[i][j], boundaryPoint[i][j + 1], sidelength);

                        for (int k = 0; k < point.Count; k++)  //循环插值点
                        {
                            boundaryControlPoint_i.Add(point[k]);
                        }
                    }

                }
                boundaryControlPoint.Add(boundaryControlPoint_i);
            }
            return boundaryControlPoint;
        }

        // 两点之间进行插值，最后一个插值点与终点之间的距离大于等于插值距离
        public static List<PointXY> InsertPoint(PointXY p1, PointXY p2, double distances)
        {
            double length = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));  //计算两点之间的距离
            int number_inserPoint = (int)(length / distances) - 1;  //计算插值的个数，最后一个插值点到终点的距离必须大于等于distance，所以计算出来的(int)(length / distances)要减去1
            List<PointXY> insertPoint = new List<PointXY>();
            for (int i = 0; i < number_inserPoint; i++)
            {
                PointXY point;
                point.X = (i + 1) * distances / length * (p2.X - p1.X) + p1.X;
                point.Y = (i + 1) * distances / length * (p2.Y - p1.Y) + p1.Y;

                insertPoint.Add(point);
            }
            return insertPoint;
        }

        // 统一给边界控制点的集合排列顺序，并建立边界与控制点排列序号的关系（键值对集合）
        public static void ArrangeBoundaryPoint(List<List<PointXY>> boundaryControlPoint, out Dictionary<ARCS_POINTS, BoundaryPoint1> arc_BoundaryPoint, out double min_X, out double min_Y, out double max_X, out double max_Y)
        {
            arc_BoundaryPoint = new Dictionary<ARCS_POINTS, BoundaryPoint1>();  //存储

            min_X = 0;
            min_Y = 0;
            max_X = 0;
            max_Y = 0;

            int arrange_No = 0;
            for (int i = 0; i < boundaryControlPoint.Count; i++)  //循环每一条边界
            {
                ARCS_POINTS arc_Points = new ARCS_POINTS();
                arc_Points.arcNo = i;  //边界的排列序号

                for (int j = 0; j < boundaryControlPoint[i].Count; j++)  //循环每一条边界上的控制点
                {
                    arc_Points.connectNo = j;  //控制点在该边界中的连接顺序

                    BoundaryPoint1 bdPoint = new BoundaryPoint1();

                    bdPoint.X = boundaryControlPoint[i][j].X;
                    bdPoint.Y = boundaryControlPoint[i][j].Y;

                    if (j == 0 || j == boundaryControlPoint[i].Count - 1)  //第一个点或最后一个点
                    {
                        bdPoint.beginOrEnd = 1;
                    }
                    else
                    {
                        bdPoint.beginOrEnd = 0;
                    }


                    if (j != boundaryControlPoint[i].Count - 1)  //不是最后一个点
                    {
                        bdPoint.arrangeNo = arrange_No;
                        arrange_No += 1;
                    }
                    else  //最后一个点,j=boundaryControlPoint[i].Count - 1
                    {
                        if ((boundaryControlPoint[i][j].X == boundaryControlPoint[i][0].X) && (boundaryControlPoint[i][j].Y == boundaryControlPoint[i][0].Y))  //最后一个点和第一个点重合
                        {
                            ARCS_POINTS arcs_point1;
                            arcs_point1.arcNo = i;
                            arcs_point1.connectNo = 0;  //本边界的第一个控制点

                            bdPoint.arrangeNo = arc_BoundaryPoint[arcs_point1].arrangeNo;
                            arrange_No = arrange_No;  //不变
                        }
                        else  //最后一个点和第一个点不重合
                        {
                            bdPoint.arrangeNo = arrange_No;
                            arrange_No += 1;

                        }
                    }

                    arc_BoundaryPoint.Add(arc_Points, bdPoint);

                    //计算X、Y的最大值
                    if (boundaryControlPoint[i][j].X > max_X)
                    {
                        max_X = boundaryControlPoint[i][j].X;
                    }
                    if (boundaryControlPoint[i][j].Y > max_Y)
                    {
                        max_Y = boundaryControlPoint[i][j].Y;
                    }
                    //计算X、Y的最小值
                    if (i == 0 && j == 0)  //第一个点
                    {
                        min_X = boundaryControlPoint[i][j].X;
                        min_Y = boundaryControlPoint[i][j].Y;
                    }
                    else  //不是第一个点
                    {
                        if (boundaryControlPoint[i][j].X < min_X)
                        {
                            min_X = boundaryControlPoint[i][j].X;
                        }
                        if (boundaryControlPoint[i][j].Y < min_Y)
                        {
                            min_Y = boundaryControlPoint[i][j].Y;
                        }
                    }


                }
            }
            min_X = min_X - 200;
            min_Y = min_Y - 200;
            max_X = max_X + 200;
            max_Y = max_Y + 200;
        }

        // 得到控制点信息的关键字参数
        public static string GetControlPoint(List<List<PointXY>> boundaryControlPoint, Dictionary<ARCS_POINTS, BoundaryPoint1> arc_BoundaryPoint)
        {
            int numberPoints = 0;
            for (int i = 0; i < boundaryControlPoint.Count; i++)
            {
                if ((boundaryControlPoint[i][0].X == boundaryControlPoint[i][boundaryControlPoint[i].Count - 1].X) && (boundaryControlPoint[i][0].Y == boundaryControlPoint[i][boundaryControlPoint[i].Count - 1].Y))
                {
                    numberPoints = numberPoints + boundaryControlPoint[i].Count - 1;  //第一个点与最后一个点重合
                }
                else
                {
                    numberPoints = numberPoints + boundaryControlPoint[i].Count;
                }

            }
            string Data = null;  //定义存储控制点信息的关键词参数
            int zero = 0;
            Data = numberPoints.ToString();

            int arrange_No = 0;
            for (int i = 0; i < boundaryControlPoint.Count; i++)  //循环边界
            {
                for (int j = 0; j < boundaryControlPoint[i].Count; j++)  //循环边界的每一个控制点
                {
                    ARCS_POINTS arc_point1;
                    arc_point1.arcNo = i;
                    arc_point1.connectNo = j;


                    if (j != boundaryControlPoint[i].Count - 1)  //不是最后一个点
                    {
                        if (arrange_No == arc_BoundaryPoint[arc_point1].arrangeNo)  //重新排序，并与键值对中的控制点排序比较，如果相等说明没有错误
                        {
                            Data = Data + " " + arc_BoundaryPoint[arc_point1].arrangeNo.ToString() + " " + arc_BoundaryPoint[arc_point1].X.ToString() + " " + arc_BoundaryPoint[arc_point1].Y.ToString() + " " + zero.ToString() + " " + zero.ToString() + " " + arc_BoundaryPoint[arc_point1].beginOrEnd.ToString();
                            arrange_No += 1;
                        }
                        else
                        {
                            Console.WriteLine("控制点排序出错！");
                        }
                    }
                    else  //最后一个点,j=boundaryControlPoint[i].Count - 1
                    {
                        if ((boundaryControlPoint[i][j].X == boundaryControlPoint[i][0].X) && (boundaryControlPoint[i][j].Y == boundaryControlPoint[i][0].Y))  //最后一个点与第一个点重合
                        {
                            ARCS_POINTS arc_point2;
                            arc_point2.arcNo = i;
                            arc_point2.connectNo = 0;
                            if (arc_BoundaryPoint[arc_point2].arrangeNo == arc_BoundaryPoint[arc_point1].arrangeNo)  //重新排序，并与键值对中的控制点排序比较，如果相等说明没有错误
                            {
                                arrange_No = arrange_No;
                            }
                            else
                            {
                                Console.WriteLine("控制点排序出错！");
                            }
                        }
                        else  //最后一个点和第一个点不重合
                        {
                            if (arrange_No == arc_BoundaryPoint[arc_point1].arrangeNo)  //重新排序，并与键值对中的控制点排序比较，如果相等说明没有错误
                            {
                                Data = Data + " " + arc_BoundaryPoint[arc_point1].arrangeNo.ToString() + " " + arc_BoundaryPoint[arc_point1].X.ToString() + " " + arc_BoundaryPoint[arc_point1].Y.ToString() + " " + zero.ToString() + " " + zero.ToString() + " " + arc_BoundaryPoint[arc_point1].beginOrEnd.ToString();
                                arrange_No += 1;
                            }
                            else
                            {
                                Console.WriteLine("控制点排序出错！");
                            }
                        }


                    }

                }
            }

            return Data;

        }

        // 得到边界控制点连接信息的关键字参数
        public static string GetConnectNo(List<List<PointXY>> boundaryControlPoint, Dictionary<ARCS_POINTS, BoundaryPoint1> arc_BoundaryPoint)
        {
            string Data = null;  //定义存储边界控制点连接信息的关键字参数

            int zero = 0;
            Data = boundaryControlPoint.Count.ToString();  //边界总数

            for (int i = 0; i < boundaryControlPoint.Count; i++)  //循环边界
            {
                ARCS_POINTS arc_point;
                arc_point.arcNo = i;
                Data = Data + " " + i.ToString();  //第几条边界

                List<int> connectNo = new List<int>();  //按照连接顺序存储控制点的总体排列序号

                for (int j = 0; j < boundaryControlPoint[i].Count; j++)  //循环边界的每一个控制点
                {
                    arc_point.connectNo = j;  //该控制点的连接序号

                    int arrangeNO = arc_BoundaryPoint[arc_point].arrangeNo;  //该控制点的整体排列序号

                    connectNo.Add(arrangeNO);  //把排列序号按照连接序号添加到集合中

                }

                Data = Data + " " + connectNo[0].ToString();  //第一个控制点
                Data = Data + " " + connectNo[connectNo.Count - 1].ToString();

                int betweenBegin_EndNumber = connectNo.Count - 2;  //第一个点与最后一个点之间控制点的数量
                Data = Data + " " + betweenBegin_EndNumber.ToString();
                for (int j = 1; j < connectNo.Count - 1; j++)
                {
                    Data = Data + " " + connectNo[j].ToString();
                }

                Data = Data + " " + zero.ToString();

            }
            return Data;
        }

        // 根据mdf文件，使用外挂方式生成mesh,并光滑处理,mesh文件先不插值
        public static void GenerateMesh(string mdfFilePath, string meshFilePath, string caolvFilePath, double sideLength)
        {
            //打开MDF文件
            IntPtr mdf_exe = ShellExecute(IntPtr.Zero, "open", mdfFilePath, "", "", ShowCommands.SW_SHOWNORMAL);  //运行mdf文件
            Thread.Sleep(2000);  //线程挂起2秒
            Key_Down_Up3(18, 77, 71, 1); //alt + M +G
            Key_Down_Up1(9, 2); //2次TAB

            double area = 0.5 * sideLength * sideLength;
            Clipboard.SetText(area.ToString());
            Key_Down_Up2(0x11, 86, 1); //CTRL + V
            Key_Down_Up1(9, 4); //4次TAB
            Key_Down_Up1(13, 1); //enter
            Thread.Sleep(2000);  //线程挂起2秒,生成MESH结束
            
            //关闭
            Key_Down_Up2(18, 115, 1);    //ALT +F4

            //保存
            Key_Down_Up2(0x11, 83, 1); //CTRL + S
            Thread.Sleep(1000);  //线程挂起1秒

            //光滑处理栅格
            Key_Down_Up3(18, 77, 83, 1); //alt + M +S
            Thread.Sleep(200);  //线程挂起1秒 
            int iteration = 50;
            Clipboard.SetText(iteration.ToString());
            Key_Down_Up1(9, 2); //2次TAB
            Key_Down_Up2(0x11, 86, 1); //CTRL + V
            Key_Down_Up1(9, 3); //3次TAB
            Key_Down_Up1(13, 1); //enter

            //根据进程状态，确定线程被挂起的时间
            Thread_Sleep_FromProcess();
            Console.WriteLine("光滑网格结束!");

            //导出mesh文件
            Key_Down_Up3(18, 77, 69, 1); //alt + M +E
            Key_Down_Up1(9, 3); //3次TAB
            Clipboard.SetText(meshFilePath);
            Key_Down_Up2(0x11, 86, 1); //CTRL + V
            Key_Down_Up1(13, 1); //enter
            Thread.Sleep(500);  //线程挂起1秒
            Console.WriteLine("导出mesh成功!");

            //导出糙率文件
            Key_Down_Up3(18, 77, 69, 1); //alt + M +E
            Key_Down_Up1(9, 2); //2次TAB
            Key_Down_Up1(0x28, 1); //方向键下
            Key_Down_Up1(9, 1); //1次TAB
            Clipboard.SetText(caolvFilePath);
            Key_Down_Up2(0x11, 86, 1); //CTRL + V
            Key_Down_Up1(9, 3); //3次TAB
            Clipboard.SetText("caolv");
            Key_Down_Up2(0x11, 86, 1); //CTRL + V
            Key_Down_Up1(9, 2); //2次TAB
            Clipboard.SetText("Manning's M");
            Key_Down_Up2(0x11, 86, 1); //CTRL + V
            Key_Down_Up1(0x28, 1); //方向键下
            Key_Down_Up1(13, 1); //enter
            Thread.Sleep(500);  //线程挂起1秒
            Console.WriteLine("导出糙率dfsu成功!");

            //保存mdf文件
            Key_Down_Up2(0x11, 83, 1); //CTRL + S
            Thread.Sleep(1000);  //线程挂起1秒

            //关闭mdf文件
            Key_Down_Up2(18, 115, 1);    //ALT +F4
            Thread.Sleep(500);  //线程挂起0.5秒  
        }

        //根据进程状态设置线程挂起时间
        private static void Thread_Sleep_FromProcess()
        {
            //根据进程状态，确定线程被挂起的时间
            Thread.Sleep(1000);
            Console.WriteLine("开始光滑网格......");
            while (true)
            {
                string process_name = "MzShell";
                Process[] pros = Process.GetProcessesByName(process_name);
                if (pros.Length == 0)
                {
                    break;
                }
                else
                {
                    bool isno = true;
                    for (int i = 0; i < pros.Length; i++)
                    {
                        if (pros[i].Responding == false)
                        {
                            isno = false;
                            break;
                        }
                    }

                    if (isno == true)
                    {
                        Thread.Sleep(1000);
                        break;
                    }
                }
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
        }

        //键盘事件
        public static void Key_Down_Up1(byte by, int key_count)
        {
            for (int i = 0; i < key_count; i++)
            {
                keybd_event(by, 0, 0, 0);
                keybd_event(by, 0, 2, 0);
                Thread.Sleep(100);  //线程挂0.1秒
            }
        }

        //键盘事件
        public static void Key_Down_Up2(byte by1, byte by2, int key_count)
        {
            for (int i = 0; i < key_count; i++)
            {
                keybd_event(by1, 0, 0, 0);
                keybd_event(by2, 0, 0, 0);

                keybd_event(by1, 0, 2, 0);
                keybd_event(by2, 0, 2, 0);
                Thread.Sleep(100);  //线程挂0.1秒
            }
        }

        //键盘事件 -- 3键同时按下和弹起
        public static void Key_Down_Up3(byte by1, byte by2, byte by3, int key_count)
        {
            for (int i = 0; i < key_count; i++)
            {
                keybd_event(by1, 0, 0, 0);
                keybd_event(by2, 0, 0, 0);
                keybd_event(by3, 0, 0, 0);

                keybd_event(by1, 0, 2, 0);
                keybd_event(by2, 0, 2, 0);
                keybd_event(by3, 0, 2, 0);
                Thread.Sleep(100);  //线程挂0.1秒
            }
        }

        // 合并mesh文件
        public static void Merge_Mesh(List<string> sourcefilenamelist, string output_MeshFileName)
        {
            List<List<int>> fileBoundaryCodesToRemove = new List<List<int>>();  //定义存储各mesh文件边界代码的集合
            List<int> allBoundaryCode = new List<int>();

            for (int i = 0; i < sourcefilenamelist.Count; i++)  //循环每一个mesh文件
            {
                List<int> BoundaryCodes = new List<int>();  //定义存储边界代码的集合

                MeshFile mesh = MeshFile.ReadMesh(sourcefilenamelist[i]);
                int[] code = mesh.Code;
                for (int j = 0; j < code.Length; j++)  //循环每一个节点
                {
                    BoundaryCodes.Add(code[j]);
                    allBoundaryCode.Add(code[j]);
                }
                fileBoundaryCodesToRemove.Add(BoundaryCodes);
            }

            double tol = 1e-2;  //我也不太清楚什么意思
            bool tryMergeAllNodes = true;  //

            MeshMerger merger = new MeshMerger(tol, output_MeshFileName, tryMergeAllNodes);

            merger.Process(sourcefilenamelist, fileBoundaryCodesToRemove);

            MeshFile newmesh = MeshFile.ReadMesh(output_MeshFileName);
            for (int i = 0; i < newmesh.NumberOfNodes; i++)
            {
                newmesh.Code.SetValue(allBoundaryCode[i], i);
            }

            newmesh.Write(output_MeshFileName);

            //检验是否出错
            MeshValidator meshValidator = new MeshValidator();
            meshValidator.ValidateMesh(newmesh.X, newmesh.Y, newmesh.Code, newmesh.ElementTable);
            //输出错误
            foreach (string error in meshValidator.Errors)
            {
                Console.Out.WriteLine(error);
            }
            if (meshValidator.Errors.Count == 0)  //如果错误数量为0
            {
                Console.WriteLine("mesh文件合并成功！");
            }
        }
        #endregion ***************************************************************************************

        // 获取mdf文件中边界控制点
        public static List<List<BoundaryPoint>> GetBoundaryPoint(string sourcefilename)
        {
            PFSFile mdf = new PFSFile(sourcefilename, false);
            PFSSection MESH_DATA = mdf.GetTarget("MESH_DATA", 1);
            PFSSection POINTS = MESH_DATA.GetSection("POINTS", 1);  //边界控制点的坐标
            PFSSection ARCS = MESH_DATA.GetSection("ARCS", 1);  //边界控制点连接顺序

            string Data = POINTS.GetKeyword("Data", 1).GetParameter(1).ToString();
            string[] data = Data.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);  //边界控制点的坐标
            int pointNumber = (int)data.Length / 6;  //边界控制点的总数量

            List<BoundaryPoint> bdPoint = new List<BoundaryPoint>();  //存储边界控制点的集合
            for (int i = 0; i < pointNumber; i++)
            {
                BoundaryPoint boundaryPoint = new BoundaryPoint();

                boundaryPoint.arcNo = 0;  //点在哪条边界上，此时统一初始化为0
                boundaryPoint.X = double.Parse(data[6 * i + 2]);  //点的X坐标
                boundaryPoint.Y = double.Parse(data[6 * i + 3]);  //点的Y坐标
                boundaryPoint.Z = int.Parse(data[6 * i + 6]);  //1为初始点/结束点，0为中间点
                boundaryPoint.connectNo = 0;  //点在边界中的连接序号，初始化为0
                boundaryPoint.No = 0;  //按照横坐标大小排列序号，初始化为0

                bdPoint.Add(boundaryPoint);
            }

            string ARCS_Data = ARCS.GetKeyword("Data", 1).GetParameter(1).ToString();
            string[] arcs_data = ARCS_Data.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);  //每条边界控制点的连接顺序
            int arcNumber = int.Parse(arcs_data[0]);  //边界数量

            List<List<int>> arcs = new List<List<int>>();  //存储边界控制点排列的集合

            int begin = 1;  //第一个数为边界总条数，故从1开始
            while (begin < arcs_data.Length)  //没有超出arcs_data的长度
            {
                List<int> arc_i = new List<int>();
                arc_i.Add(int.Parse(arcs_data[begin]));  //存储边界的序号
                arc_i.Add(int.Parse(arcs_data[begin + 1]));  //边界的开始点
                int arcPointNumber = int.Parse(arcs_data[begin + 3]);  //该条边界开始点和结束点中间的控制点数
                for (int j = 0; j < arcPointNumber; j++)
                {
                    arc_i.Add(int.Parse(arcs_data[begin + 4 + j]));

                }
                if (arcs_data[begin + 1] != arcs_data[begin + 2])  //开始点和结束点不同，即边界没有闭合
                {
                    arc_i.Add(int.Parse(arcs_data[begin + 2]));
                }

                arcs.Add(arc_i);  //把该条边界控制点连接顺序添加到集合中
                begin = begin + 4 + arcPointNumber + 1;  //下一条边界的排列序号在arcs_data的位置
            }

            List<List<BoundaryPoint>> arcsBoundaryPoint = new List<List<BoundaryPoint>>();  //各条边界控制点的集合

            for (int i = 0; i < arcs.Count; i++)  //循环每一条边界
            {
                List<BoundaryPoint> arc_BoundaryPoint = new List<BoundaryPoint>();  //存储某边界控制点的集合
                //arcs[i]  第几条边界
                //arcs[i][j]  该条边界中的某控制点的序号
                //bdPoint[arcs[i][j]]  该条边界中的某控制点
                for (int j = 1; j < arcs[i].Count; j++)  //循环该条边界的每一个控制点序号，根据序号找到控制点，并按顺序存储到集合中
                {
                    BoundaryPoint boundaryPoint = new BoundaryPoint();
                    boundaryPoint.arcNo = arcs[i][0];  //边界序号

                    boundaryPoint.X = bdPoint[arcs[i][j]].X;
                    boundaryPoint.Y = bdPoint[arcs[i][j]].Y;
                    boundaryPoint.Z = bdPoint[arcs[i][j]].Z;

                    boundaryPoint.connectNo = j;  //存储点在边界中的连接序号（第一个点即j=1时为起始点，j=0是边界的排列序号）
                    boundaryPoint.No = bdPoint[arcs[i][j]].No;

                    //bdPoint[arcs[i][j]] = boundaryPoint;
                    arc_BoundaryPoint.Add(boundaryPoint);
                }
                arcsBoundaryPoint.Add(arc_BoundaryPoint);
            }
            return arcsBoundaryPoint;
        }


    }
 
}