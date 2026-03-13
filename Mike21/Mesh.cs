using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfsu;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS.mesh;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.IO;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model;

namespace bjd_model.Mike21
{
    public class Mesh
    {
        #region ***************************从mesh文件中获取信息*********************************
        //从mesh参数文件中获取已有参数信息
        public static void GetDefault_MeshInfo(string sourcefilename, ref MeshPars MeshParslist)
        {
            //读取PFS文件
            MeshFile mesh = MeshFile.ReadMesh(sourcefilename);
            double[] X = mesh.X;
            double[] Y = mesh.Y;
            double[] Z = mesh.Z;

            //节点和元素数量
            MeshParslist.Elementcount = mesh.NumberOfElements;
            MeshParslist.Nodecount = mesh.NumberOfNodes;

            //最大范围
            SubArea subarea;
            subarea.x1 = X.Min();
            subarea.y1 = Y.Min();
            subarea.x2 = X.Max();
            subarea.y2 = Y.Max();
            MeshParslist.Max_Extent = subarea;

            //Z的最大最小值
            MeshParslist.Min_Z = Z.Min();
            MeshParslist.Max_Z = Z.Max();

            //计算mesh的平均网格边长(取几个平均就行)
            int n = MeshParslist.Elementcount / 1000;  //1000个网格中取一个
            double sublength = 0;
            for (int i = 0; i < n; i++)
            {
                //元素表是一个交错数组，即每个网格元素又是一个数组，里面包含的是它的3个节点的编号
                int elenumber = i * 1000;
                int nodenumber1 = mesh.ElementTable[i][0];
                int nodenumber2 = mesh.ElementTable[i][1];
                double x1 = X[nodenumber1 - 1];
                double y1 = Y[nodenumber1 - 1];

                double x2 = X[nodenumber2 - 1];
                double y2 = Y[nodenumber2 - 1];

                //每个元素只取一个边计算边长
                sublength += PointXY.Get_ptop_distance(x1, y1, x2, y2);
            }
            double avelength = sublength / n;
            MeshParslist.Mesh_Sidelength = ((int)(avelength / 10)) * 10;   //按10取整

            //获取MeshFile文件类信息
            MeshParslist.Mesh_Filename = sourcefilename;

            Console.WriteLine("Mesh二维网格信息初始化成功!");
        }
        #endregion ******************************************************************************


        #region  ********************************** 更新mesh文件 ***********************************
        // 提取最新的断面数据信息，更新mesh文件
        public static void Rewrite_Mesh_UpdateFile(HydroModel hydromodel)
        {
            string outputfilename = hydromodel.Modelfiles.Mesh_filename;
            if (!File.Exists(hydromodel.BaseModel.Modelfiles.Mesh_filename)) return;

            //如果不存在，则CoPY一份基础的,各Mesh操作时都已经更改过了文件了
            if (!File.Exists(outputfilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, outputfilename);
            }

            Console.WriteLine("Mesh二维地形网格文件更新成功!");
            Console.WriteLine("");
        }
        #endregion **********************************************************************************


        #region ********************* mesh操作 -- 改变mesh高程、合并mesh ****************************
        //以原mesh文件为基础，将多边形区域内高程值 增加或减少一定值(value为负即为减)，生成新的mesh文件
        public static void EditMesh_Addvalue(ref HydroModel hydromodel, List<PointXY> polygon_boundpoints, double value)
        {
            string sourceFilename = hydromodel.Modelfiles.Mesh_filename;
            if (!File.Exists(sourceFilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, sourceFilename);
            }

            MeshPars meshpars = hydromodel.Mike21Pars.MeshParsList;

            MeshFile meshfile = MeshFile.ReadMesh(sourceFilename);
            double[] Z = meshfile.Z;

            //先选出在多边形范围内的节点
            List<int> NodeIncluded = Get_Include_NodeList(meshfile, polygon_boundpoints);

            //改变这些节点的高程值（Z值）
            int includcout = NodeIncluded.Count;
            for (int i = 0; i < includcout; i++)
            {
                double newvalue = Z[NodeIncluded[i]] + value;
                meshfile.Z.SetValue(newvalue, NodeIncluded[i]);
            }

            //写出mesh
            meshfile.Write(sourceFilename);

            //更新meshparslist参数
            meshpars.Update_Meshpars_FromMeshFile(sourceFilename);
        }

        //以原mesh文件为基础，将多边形区域的高程值 设置为为某单一高程值，生成新的mesh文件
        public static void EditMesh_Setvalue(ref HydroModel hydromodel, List<PointXY> polygon_boundpoints, double value)
        {
            string sourceFilename = hydromodel.Modelfiles.Mesh_filename;
            MeshPars meshpars = hydromodel.Mike21Pars.MeshParsList;

            if (!File.Exists(sourceFilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, sourceFilename);
            }
            MeshFile meshfile = MeshFile.ReadMesh(sourceFilename);
            double[] Z = meshfile.Z;

            //先选出在多边形范围内的节点
            List<int> NodeIncluded = Get_Include_NodeList(meshfile, polygon_boundpoints);

            //改变这些节点的高程值（Z值）
            int includcout = NodeIncluded.Count;
            for (int i = 0; i < includcout; i++)
            {
                meshfile.Z.SetValue(value, NodeIncluded[i]);
            }

            //写出mesh
            meshfile.Write(sourceFilename);

            //更新meshparslist参数
            meshpars.Update_Meshpars_FromMeshFile(sourceFilename);
        }

        //以原mesh文件为基础，将多边形区域的高程值 按给定DEM散点高程内插，生成新的mesh文件
        public static void EditMesh_Interpolate_InPolygon(ref HydroModel hydromodel, List<PointXY> polygon_boundpoints)
        {
            string sourceFilename = hydromodel.Modelfiles.Mesh_filename;
            MeshPars meshpars = hydromodel.Mike21Pars.MeshParsList;

            if (!File.Exists(sourceFilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, sourceFilename);
            }
            MeshFile meshfile = MeshFile.ReadMesh(sourceFilename);
            double[] X = meshfile.X;
            double[] Y = meshfile.Y;
            double[] Z = meshfile.Z;

            //先选出在多边形范围内的节点
            List<int> NodeIncluded = Get_Include_NodeList(meshfile, polygon_boundpoints);

            List<PointXY> points = new List<PointXY>();
            for (int i = 0; i < NodeIncluded.Count; i++)
            {
                PointXY point;
                point.X = X[NodeIncluded[i]];
                point.Y = Y[NodeIncluded[i]];

                points.Add(point);
            }

            //调用GIS服务内插给定点高程值
            List<PointXYZ> Interpolate_PointList = Gis_Service.Get_Nodes_DemZ_FromGisService(points);

            //改变这些节点的高程值（Z值）
            int includcout = NodeIncluded.Count;
            for (int i = 0; i < includcout; i++)
            {
                double value = Interpolate_PointList[i].Z;
                meshfile.Z.SetValue(value, NodeIncluded[i]);
            }

            //写出mesh
            meshfile.Write(sourceFilename);

            //更新meshparslist参数
            meshpars.Update_Meshpars_FromMeshFile(sourceFilename);
        }

        // 内插mesh所有节点地面高程1
        public static void EditMesh_Interpolate_Allnodes(ref HydroModel hydromodel)
        {
            string sourceFilename = hydromodel.Modelfiles.Mesh_filename;
            MeshPars meshpars = hydromodel.Mike21Pars.MeshParsList;

            if (!File.Exists(sourceFilename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, sourceFilename);
            }
            MeshFile meshfile = MeshFile.ReadMesh(sourceFilename);
            double[] X = meshfile.X;
            double[] Y = meshfile.Y;
            double[] Z = meshfile.Z;

            List<PointXY> Nodespoint = new List<PointXY>();  //mesh文件中节点的坐标集合
            for (int i = 0; i < meshfile.NumberOfNodes; i++)
            {
                PointXY point;
                point.X = X[i];
                point.Y = Y[i];

                Nodespoint.Add(point);
            }

            //调用GIS服务内插给定点高程值
            List<PointXYZ> interpolate_point = Gis_Service.Get_Nodes_DemZ_FromGisService(Nodespoint);

            for (int i = 0; i < meshfile.NumberOfNodes; i++)
            {
                meshfile.Z.SetValue(interpolate_point[i].Z, i);
            }

            meshfile.Write(sourceFilename);

            Console.WriteLine("高程插值完成！");

            //更新meshparslist参数
            meshpars.Update_Meshpars_FromMeshFile(sourceFilename);
        }

        // 内插mesh所有节点地面高程2  
        public static void EditMesh_Interpolate_Allnodes(string Filename)
        {
            Console.WriteLine("开始mesh高程插值........");
            MeshFile mesh = MeshFile.ReadMesh(Filename);
            double[] X = mesh.X;
            double[] Y = mesh.Y;
            double[] Z = mesh.Z;

            List<PointXY> Nodespoint = new List<PointXY>();  //mesh文件中节点的坐标集合
            for (int i = 0; i < mesh.NumberOfNodes; i++)
            {
                PointXY point;
                point.X = X[i];
                point.Y = Y[i];

                Nodespoint.Add(point);
            }

            //调用GIS服务内插给定点高程值
            List<PointXYZ> interpolate_point = Gis_Service.Get_Nodes_DemZ_FromGisService(Nodespoint);

            for (int i = 0; i < mesh.NumberOfNodes; i++)
            {
                mesh.Z.SetValue(interpolate_point[i].Z, i);
            }

            mesh.Write(Filename);

            Console.WriteLine("高程插值成功");
        }

        //返回多边形区域内的mesh节点标号集合
        public static List<int> Get_Include_NodeList(MeshFile mesh, List<PointXY> polygon_boundpoints)
        {
            // 节点坐标
            double[] X = mesh.X;
            double[] Y = mesh.Y;
            double[] Z = mesh.Z;

            //多边形的最大、最小XY
            double Polygon_minx;
            double Polygon_maxx;
            double Polygon_miny;
            double Polygon_maxy;
            PointXY.Getmaxmin(polygon_boundpoints, out Polygon_minx, out Polygon_maxx, out Polygon_miny, out Polygon_maxy);

            // 遍历所有节点，看是否有节点在给定区域范围内
            List<int> NodeIncluded = new List<int>();

            int nodecount = mesh.NumberOfNodes;
            for (int i = 0; i < nodecount; i++)
            {
                //先判断是否在多边形的最大最小值范围内
                if (X[i] >= Polygon_minx && X[i] <= Polygon_maxx && Y[i] >= Polygon_miny && Y[i] <= Polygon_maxy)
                {
                    PointXY p;
                    p.X = X[i];
                    p.Y = Y[i];

                    //进一步判断如果在多边形内，则集合相应增加该节点编号元素
                    if (PointXY.IsInPolygon(p, polygon_boundpoints))
                    {
                        NodeIncluded.Add(i);
                    }
                }
            }

            return NodeIncluded;
        }

        //高程值内插---以给定离散高程点为基础，内插给定坐标点的高程值 
        //方法 -- 找到最近的3个点，根据距离取加权平均值（距离越近权重越大）
        public static List<PointXYZ> Get_Interpolatexyz(List<PointXYZ> dem_points, List<PointXY> points)
        {
            List<PointXYZ> pointxyz = new List<PointXYZ>();

            int pointcount = points.Count;

            //如果DEM散点集合就1个点，那就全部取这个值
            if (dem_points.Count == 1)
            {
                for (int i = 0; i < pointcount; i++)
                {
                    PointXYZ point;
                    point.X = points[i].X;
                    point.Y = points[i].Y;
                    point.Z = dem_points[0].Z;
                    pointxyz.Add(point);
                }
                return pointxyz;
            }

            //如果DEM散点集合就2个点，那就取与这两个值距离的加权平均
            if (dem_points.Count == 2)
            {
                for (int i = 0; i < pointcount; i++)
                {
                    PointXYZ point;
                    point.X = points[i].X;
                    point.Y = points[i].Y;

                    double x1 = dem_points[0].X;
                    double y1 = dem_points[0].Y;
                    double z1 = dem_points[0].Z;

                    double x2 = dem_points[1].X;
                    double y2 = dem_points[1].Y;
                    double z2 = dem_points[1].Z;

                    double distant1 = Math.Sqrt((point.X - x1) * (point.X - x1) + (point.Y - y1) * (point.Y - y1));
                    double distant2 = Math.Sqrt((point.X - x2) * (point.X - x2) + (point.Y - y2) * (point.Y - y2));

                    //取反向权重，即距离越近权重越大
                    double sumdistant = distant1 + distant2;
                    point.Z = z1 * (distant2 / sumdistant) + z2 * (distant1 / sumdistant);
                    pointxyz.Add(point);
                }
                return pointxyz;
            }

            //如果大于等于3个点，取最近的3个点加权平均
            for (int i = 0; i < pointcount; i++)
            {
                PointXYZ point;
                point.X = points[i].X;
                point.Y = points[i].Y;

                //找到最近的3个点
                Dictionary<PointXYZ, double> nearpoints = GetNearpoints(dem_points, points[i], 3);

                //3个点离指定点的距离
                double distant1 = nearpoints.ElementAt(0).Value;
                double distant2 = nearpoints.ElementAt(1).Value;
                double distant3 = nearpoints.ElementAt(2).Value;

                //3个点的XYZ
                double x1 = nearpoints.ElementAt(0).Key.X;
                double y1 = nearpoints.ElementAt(0).Key.Y;
                double z1 = nearpoints.ElementAt(0).Key.Z;

                double x2 = nearpoints.ElementAt(1).Key.X;
                double y2 = nearpoints.ElementAt(1).Key.Y;
                double z2 = nearpoints.ElementAt(1).Key.Z;

                double x3 = nearpoints.ElementAt(2).Key.X;
                double y3 = nearpoints.ElementAt(2).Key.Y;
                double z3 = nearpoints.ElementAt(2).Key.Z;

                //先两两取反向加权平均，然后再取平均
                double sumdistant1 = distant1 + distant2;
                double sumdistant2 = distant1 + distant3;
                double sumdistant3 = distant2 + distant3;

                double z11 = z1 * (distant2 / sumdistant1) + z2 * (distant1 / sumdistant1);
                double z22 = z1 * (distant3 / sumdistant2) + z3 * (distant1 / sumdistant2);
                double z33 = z2 * (distant3 / sumdistant3) + z3 * (distant2 / sumdistant3);

                point.Z = (z11 + z22 + z33) / 3;
                pointxyz.Add(point);
            }

            return pointxyz;
        }

        //找到附近最近的几个点
        public static Dictionary<PointXYZ, double> GetNearpoints(List<PointXYZ> dem_points, PointXY point, int number)
        {
            //新建一个键值对集合，存储距离最近的点和距离
            Dictionary<PointXYZ, double> nearpointlist = new Dictionary<PointXYZ, double>();

            //新建一个键值对集合，存储该点编号 和 与目标点的距离
            Dictionary<double, int> dic = new Dictionary<double, int>();

            int pointcount = dem_points.Count;
            for (int i = 0; i < pointcount; i++)
            {
                double x = dem_points[i].X;
                double y = dem_points[i].Y;

                //计算距离值并增加键值对集合元素
                double distant = Math.Sqrt((point.X - x) * (point.X - x) + (point.Y - y) * (point.Y - y));
                if (!dic.Keys.Contains(distant))
                {
                    dic.Add(distant, i);
                }
            }

            //取出键(距离)的集合
            List<double> distantlist = dic.Keys.ToList();

            //排序--升序
            distantlist.Sort();

            //找出前number个最近的点
            for (int i = 0; i < number; i++)
            {
                double neardistant = distantlist[i];
                int near_number = dic[neardistant];  //根据键取值

                PointXYZ nearpoint;
                nearpoint.X = dem_points[near_number].X;
                nearpoint.Y = dem_points[near_number].Y;
                nearpoint.Z = dem_points[near_number].Z;

                nearpointlist.Add(nearpoint, neardistant);
            }
            return nearpointlist;
        }

        //读取多边形控制点文件
        public static List<PointXY> ReadXYFile(string sourcefile)
        {
            //读取多边形边界控制点
            List<PointXY> polygon_boundpoints = new List<PointXY>();

            string[] contents = File.ReadAllLines(sourcefile, Encoding.Default);
            for (int i = 0; i < contents.Length; i++)
            {
                PointXY point;
                char[] chararray = { ',' };
                string[] xystring = contents[i].Split(chararray);

                point.X = double.Parse(xystring[0]);
                point.Y = double.Parse(xystring[1]);
                polygon_boundpoints.Add(point);
            }
            return polygon_boundpoints;
        }

        //读取DEM高程散点文件
        public static List<PointXYZ> ReadXYZFile(string sourcefile)
        {
            //读取DEM高程点
            List<PointXYZ> dem_points = new List<PointXYZ>();
            string[] contents = File.ReadAllLines(sourcefile, Encoding.Default);
            for (int i = 0; i < contents.Length; i++)
            {
                PointXYZ point;
                char[] chararray = { ',' };
                string[] xyzstring = contents[i].Split(chararray);

                point.X = double.Parse(xyzstring[0]);
                point.Y = double.Parse(xyzstring[1]);
                point.Z = double.Parse(xyzstring[2]);
                dem_points.Add(point);
            }
            return dem_points;
        }

        // 从mesh文件中获取获取指定点的地面高程
        public static double Get_Point_Z(HydroModel hydromodel, PointXY point)
        {
            double now_mesh_sidelength = hydromodel.Mike21Pars.MeshParsList.Mesh_Sidelength;

            //定义搜索的子区间
            SubArea subArea;
            subArea.x1 = point.X - now_mesh_sidelength * 2;
            subArea.y1 = point.Y - now_mesh_sidelength * 2;
            subArea.x2 = point.X + now_mesh_sidelength * 2;
            subArea.y2 = point.Y + now_mesh_sidelength * 2;

            if (!File.Exists(hydromodel.Modelfiles.Mesh_filename))
            {
                File.Copy(hydromodel.BaseModel.Modelfiles.Mesh_filename, hydromodel.Modelfiles.Mesh_filename);
            }
            MeshFile meshfile = MeshFile.ReadMesh(hydromodel.Modelfiles.Mesh_filename);  //mesh文件

            //节点坐标
            double[] X = meshfile.X;
            double[] Y = meshfile.Y;
            double[] Z = meshfile.Z;

            //遍历所有节点，判断所给点是否刚好位于节点
            for (int i = 0; i < meshfile.NumberOfNodes; i++)
            {
                PointXYZ node_point;
                node_point.X = X[i]; node_point.Y = Y[i]; node_point.Z = Z[i];
                if (Math.Abs(node_point.X - point.X) < 0.1 && Math.Abs(node_point.Y - point.Y) < 0.1)
                {
                    return node_point.Z;
                }
            }

            //所给点不位于节点的情况
            int elmnumber = Mesh.Get_Elmnumber(meshfile, point);  //找到给定点所在的网格元素编号

            //网格元素的3个节点编号(注意对应的节点号需要 -1)
            int node1_number = meshfile.ElementTable[elmnumber][0] - 1;
            int node2_number = meshfile.ElementTable[elmnumber][1] - 1;
            int node3_number = meshfile.ElementTable[elmnumber][2] - 1;

            List<PointXYZ> points_list = new List<PointXYZ>();
            points_list.Add(PointXYZ.Get_PointXYZ(X[node1_number], Y[node1_number], Z[node1_number]));
            points_list.Add(PointXYZ.Get_PointXYZ(X[node2_number], Y[node1_number], Z[node2_number]));
            points_list.Add(PointXYZ.Get_PointXYZ(X[node2_number], Y[node1_number], Z[node2_number]));

            double resultz = InterpolateZ(points_list, point);
            return resultz;
        }

        // 从mesh文件中获取获取指定点的地面高程
        public static double Get_Point_Z(MeshFile meshfile, PointXY point)
        {
            double now_mesh_sidelength = MeshPars.Get_MeshSide_AveLength(meshfile);

            //定义搜索的子区间
            SubArea subArea;
            subArea.x1 = point.X - now_mesh_sidelength * 2;
            subArea.y1 = point.Y - now_mesh_sidelength * 2;
            subArea.x2 = point.X + now_mesh_sidelength * 2;
            subArea.y2 = point.Y + now_mesh_sidelength * 2;

            //节点坐标
            double[] X = meshfile.X;
            double[] Y = meshfile.Y;
            double[] Z = meshfile.Z;

            //遍历所有节点，判断所给点是否刚好位于节点
            for (int i = 0; i < meshfile.NumberOfNodes; i++)
            {
                PointXYZ node_point;
                node_point.X = X[i]; node_point.Y = Y[i]; node_point.Z = Z[i];
                if (Math.Abs(node_point.X - point.X) < 0.1 && Math.Abs(node_point.Y - point.Y) < 0.1)
                {
                    return node_point.Z;
                }
            }

            //所给点不位于节点的情况
            int elmnumber = Mesh.Get_Elmnumber(meshfile, point);  //找到给定点所在的网格元素编号

            //网格元素的3个节点编号(注意对应的节点号需要 -1)
            int node1_number = meshfile.ElementTable[elmnumber][0] - 1;
            int node2_number = meshfile.ElementTable[elmnumber][1] - 1;
            int node3_number = meshfile.ElementTable[elmnumber][2] - 1;

            List<PointXYZ> points_list = new List<PointXYZ>();
            points_list.Add(PointXYZ.Get_PointXYZ(X[node1_number], Y[node1_number], Z[node1_number]));
            points_list.Add(PointXYZ.Get_PointXYZ(X[node2_number], Y[node1_number], Z[node2_number]));
            points_list.Add(PointXYZ.Get_PointXYZ(X[node2_number], Y[node1_number], Z[node2_number]));

            double resultz = InterpolateZ(points_list, point);
            return resultz;
        }

        //根据mesh元素的3点内插高程
        public static double InterpolateZ(List<PointXYZ> points, PointXY point)
        {
            //3个点离指定点的距离
            double distant1 = PointXY.Get_ptop_distance(points[0].X, points[0].Y, point.X, point.Y);
            double distant2 = PointXY.Get_ptop_distance(points[1].X, points[1].Y, point.X, point.Y);
            double distant3 = PointXY.Get_ptop_distance(points[2].X, points[2].Y, point.X, point.Y);

            //3个点的XYZ
            double x1 = points[0].X;
            double y1 = points[0].Y;
            double z1 = points[0].Z;

            double x2 = points[1].X;
            double y2 = points[1].Y;
            double z2 = points[1].Z;

            double x3 = points[2].X;
            double y3 = points[2].Y;
            double z3 = points[2].Z;

            //先两两取反向加权平均，然后再取平均
            double sumdistant1 = distant1 + distant2;
            double sumdistant2 = distant1 + distant3;
            double sumdistant3 = distant2 + distant3;

            double z11 = z1 * (distant2 / sumdistant1) + z2 * (distant1 / sumdistant1);
            double z22 = z1 * (distant3 / sumdistant2) + z3 * (distant1 / sumdistant2);
            double z33 = z2 * (distant3 / sumdistant3) + z3 * (distant2 / sumdistant3);

            return (z11 + z22 + z33) / 3;
        }
  
        //获取指定点所在的网格元素编号
        public static int Get_Elmnumber(MeshFile mesh, PointXY inputp)
        {
            int elmnumber = 1;
            // 节点坐标
            double[] X = mesh.X;
            double[] Y = mesh.Y;

            // 遍历所有的网格元素，以及每个网格元素的3个节点，看是否有网格元素满足要求，并将满足要求的元素收集起来
            List<int> elmtsIncluded = new List<int>();
            double xmax = 0, xmin = 0;
            double ymax = 0, ymin = 0;
            for (int i = 0; i < mesh.NumberOfElements; i++)
            {
                // 提取网格元素的节点数组
                int[] nodes = mesh.ElementTable[i];  //元素表是一个交错数组，即每个网格元素又是一个数组，里面包含的是它的3个节点的编号

                // 判断网格元素3个节点的坐标是否在子区域范围内
                xmax = X[nodes[0] - 1]; xmin = X[nodes[0] - 1];
                ymax = Y[nodes[0] - 1]; ymin = Y[nodes[0] - 1];
                for (int j = 0; j < nodes.Length; j++)
                {
                    int node = nodes[j] - 1;   //注意这里要-1,说明节点是从0开始计数的
                    if (X[node] > xmax)
                    {
                        xmax = X[node];
                    }

                    if (X[node] < xmin)
                    {
                        xmin = X[node];
                    }

                    if (Y[node] > ymax)
                    {
                        ymax = Y[node];
                    }

                    if (Y[node] < ymin)
                    {
                        ymin = Y[node];
                    }
                }

                if ((inputp.X >= xmin && inputp.X <= xmax) && (inputp.Y >= ymin && inputp.Y <= ymax))
                {
                    elmtsIncluded.Add(i);
                }
            }

            //在可能的网格元素里找到指定点在内的一个
            for (int i = 0; i < elmtsIncluded.Count; i++)
            {
                int[] nodes = mesh.ElementTable[elmtsIncluded[i]];
                if (PointXY.Pointinelm(inputp, X, Y, nodes))
                {
                    elmnumber = elmtsIncluded[i];
                    break;
                }
            }
            return elmnumber;
        }

        //从mesh文件获得蓄洪区边界点集合
        public static List<PointXY> Get_Mesh_BoundaryPointlist(MeshFile meshFile)
        {
            double[] X = meshFile.X;
            double[] Y = meshFile.Y;
            int[] code = meshFile.Code;

            List<PointXY> boundaryPoint = new List<PointXY>();
            for (int i = 0; i < code.Length; i++)
            {
                PointXY bdPoint;  //定义一个点，用来临时存储边界点
                if (code[i] == 1)  //code等于1，说明为边界点
                {
                    bdPoint.X = X[i];
                    bdPoint.Y = Y[i];
                    boundaryPoint.Add(bdPoint);
                }
            }
            return boundaryPoint;
        }

        #endregion ***************************************************************************************

    }

    //用于存储mesh参数的类
    [Serializable]
    public class MeshPars
    {
        //属性
        //元素网格数量
        public int Elementcount
        { get; set; }

        //顶点数量
        public int Nodecount
        { get; set; }

        //网格范围
        public SubArea Max_Extent
        { get; set; }

        //最大Z值
        public double Max_Z
        { get; set; }

        //最小Z值
        public double Min_Z
        { get; set; }

        //采用的剖分网格边长
        public double Mesh_Sidelength
        { get; set; }

        //网格文件路径
        public string Mesh_Filename
        { get; set; }

        //方法
        //获取网格剖分长度,取整
        public static double Get_MeshSide_AveLength(MeshFile mesh)
        {
            //节点和元素数量
            int Elementcount = mesh.NumberOfElements;

            double[] X = mesh.X;
            double[] Y = mesh.Y;

            //计算mesh的平均网格边长(取几个平均就行)
            int n = Elementcount / 1000;  //1000个网格中取一个
            double sublength = 0;
            for (int i = 0; i < n; i++)
            {
                //元素表是一个交错数组，即每个网格元素又是一个数组，里面包含的是它的3个节点的编号
                int elenumber = i * 1000;
                int nodenumber1 = mesh.ElementTable[i][0];
                int nodenumber2 = mesh.ElementTable[i][1];

                if (nodenumber1 < X.Length - 1 && nodenumber2 < X.Length - 1)
                {
                    //注意，直接打开mesh文件和mesh元素表里获取的节点号都是从1排的，对应在X[]、Y[]数组里需要-1
                    double x1 = X[nodenumber1 - 1];
                    double y1 = Y[nodenumber1 - 1];

                    double x2 = X[nodenumber2 - 1];
                    double y2 = Y[nodenumber2 - 1];

                    //每个元素只取一个边计算边长
                    sublength += PointXY.Get_ptop_distance(x1, y1, x2, y2);
                }
            }
            double avelength = sublength / n;

            return Math.Round(avelength);
        }

        //根据meshfile更新其他属性参数
        public void Update_Meshpars_FromMeshFile(string Mesh_Filename)
        {
            MeshFile Mesh_File = MeshFile.ReadMesh(Mesh_Filename);

            double[] X = Mesh_File.X;
            double[] Y = Mesh_File.Y;
            double[] Z = Mesh_File.Z;

            //节点和元素数量
            this.Elementcount = Mesh_File.NumberOfElements;
            this.Nodecount = Mesh_File.NumberOfNodes;

            //最大范围
            SubArea subarea;
            subarea.x1 = X.Min();
            subarea.y1 = Y.Min();
            subarea.x2 = X.Max();
            subarea.y2 = Y.Max();
            this.Max_Extent = subarea;

            //Z的最大最小值
            this.Min_Z = Z.Min();
            this.Max_Z = Z.Max();

            //剖分长度
            this.Mesh_Sidelength = Get_MeshSide_AveLength(Mesh_File);
        }

        //根据meshfile获取网格数量
        public static int Get_NodeCount(string Mesh_Filename)
        {
            MeshFile Mesh_File = MeshFile.ReadMesh(Mesh_Filename);

            //节点和元素数量
            return Mesh_File.NumberOfNodes;
        }
    }

    #region 样例中的类
    // 将多个mesh合并成一个更大的mesh
    public class MeshMerger
    {
        // 构造函数
        public MeshMerger(double tol, string newMeshFileName, bool tryMergeAllNodes)
        {
            _newMeshFileName = newMeshFileName;
            TryMergeAllNodes = tryMergeAllNodes;
            NodeTolerance = tol;
        }

        //字段和属性

        //是否结合所有节点的属性，当为false时，仅仅结合边界节点，则内部节点不被结合；当为true时，结合所有节点
        public bool TryMergeAllNodes { get; set; }

        //两个节点的最小距离，不应大于最小mesh边长度
        public double NodeTolerance { get; set; }

        //结合的节点数量
        public int NodeMergeCount { get; set; }

        private readonly string _newMeshFileName;
        private QuadSearchTree _nodeSearchTree;

        //用于merged的节点变量
        private readonly List<double> _x = new List<double>();
        private readonly List<double> _y = new List<double>();
        private readonly List<double> _z = new List<double>();
        private readonly List<int> _code = new List<int>();

        //用于merged的mesh元素变量
        private readonly List<int> _elementType = new List<int>();
        private readonly List<int[]> _connectivity = new List<int[]>();

        //方法

        // 将多个mesh合并成一个的方法
        public void Process(List<string> files, List<List<int>> fileBoundaryCodesToRemove)
        {
            // 新建一个区域
            Extent extent = new Extent();  //区域

            // 加载所有的mesh
            List<MeshFile> meshes = new List<MeshFile>(files.Count);  //mesh文件的集合
            for (int i = 0; i < files.Count; i++)
            {
                MeshFile mesh = MeshFile.ReadMesh(files[i]);  //读取mesh文件
                meshes.Add(mesh);  //添加到集合中
                for (int j = 0; j < mesh.NumberOfNodes; j++)  //循环每一个节点
                {
                    extent.Include(mesh.X[j], mesh.Y[j]);  //重新确定区域X、Y的最大值和最小值
                }
            }

            //区域的边界与流域的边界保留一点距离，距离为NodeTolerance
            extent.XMin = extent.XMin - NodeTolerance;
            extent.XMax = extent.XMax + NodeTolerance;
            extent.YMin = extent.YMin - NodeTolerance;
            extent.YMax = extent.YMax + NodeTolerance;

            // 初始化搜索树
            _nodeSearchTree = new QuadSearchTree(extent);

            // 创建新mesh元素和节点
            for (int i = 0; i < files.Count; i++)
            {
                int prevNodeMergeCount = NodeMergeCount;
                AddMesh(meshes[i], fileBoundaryCodesToRemove[i]);
            }

            // 创建新mesh文件
            string projection = meshes[0].ProjectionString;
            eumQuantity eumQuantity = meshes[0].EumQuantity;

            MeshBuilder builder = new MeshBuilder();
            builder.SetNodes(_x.ToArray(), _y.ToArray(), _z.ToArray(), _code.ToArray());
            builder.SetElements(_connectivity.ToArray());
            builder.SetProjection(projection);
            builder.SetEumQuantity(eumQuantity);

            MeshFile newMesh = builder.CreateMesh();

            newMesh.Write(_newMeshFileName);
        }

        private void UpdateStatistics(int meshCount, int meshIndex, SortedDictionary<int, int[]> stats, List<KeyValuePair<int, int>> data)
        {
            foreach (KeyValuePair<int, int> faceCodeCount in data)
            {
                int[] counts;
                if (!stats.TryGetValue(faceCodeCount.Key, out counts))
                {
                    counts = new int[meshCount];
                    stats.Add(faceCodeCount.Key, counts);
                }
                counts[meshIndex] += faceCodeCount.Value;
            }
        }

        private void AddMesh(MeshFile mesh, List<int> boundaryCodesToRemove)
        {

            // node numbers in new mesh for each node in provided mesh
            int[] renumberNodes = new int[mesh.NumberOfNodes];  //存储新mesh文件中节点序号的数组

            // Reused variables
            Extent e = new Extent();  //定义一个可视化区域

            List<QuadSearchTree.Point> points = new List<QuadSearchTree.Point>();

            // Index of first new node
            int newMeshFirstNodeIndex = _x.Count;  //_x没有赋值??

            //将节点添加到合并mesh文件中，寻找哪些节点应该被合并
            // Add nodes of current mesh to merged mesh, find which nodes should be merged
            for (int i = 0; i < mesh.NumberOfNodes; i++)  //循环mesh文件中的节点
            {
                double x = mesh.X[i];
                double y = mesh.Y[i];
                double z = mesh.Z[i];
                int code = mesh.Code[i];

                bool boundaryNode = code != 0;

                if (boundaryCodesToRemove.Contains(code))
                    code = 0;

                // This criteria selects which nodes to try to merge on, i.e. 
                // when to try to find a "duplicate" in merged mesh.判断哪些节点要被合并
                if (TryMergeAllNodes || boundaryNode)  //（全部都要合并||是边界节点）
                {
                    // Check if this current node already "exists" in the merged mesh
                    // i.e. if the current node is "close to" a node in the merged mesh.
                    // "Close to" is decided by the NodeTolerance //检查节点是否已经存在mesh文件中

                    // Find all nodes within NodeTolerance of (x,y) in search tree
                    UpdatePointExtent(e, x, y, NodeTolerance);
                    points.Clear();
                    _nodeSearchTree.Find(e, points);  //？？
                    if (points.Count > 0)
                    {
                        // find the point closest to (x,y), and witin NodeTolerance distance
                        QuadSearchTree.Point closest = null;
                        // Distance must be less than NodeTolerance in order to reuse existing node
                        // Comparing on squared distances, to avoid taking a lot of square roots
                        double minDistSq = NodeTolerance * NodeTolerance;
                        foreach (QuadSearchTree.Point point in points)
                        {
                            double dx = point.X - x;
                            double dy = point.Y - y;
                            double distSq = dx * dx + dy * dy;
                            if (distSq < minDistSq)
                            {
                                closest = point;
                                minDistSq = distSq;
                            }
                        }

                        if (closest != null)
                        {
                            // Node already exist at/close to coordinate, use existing node 
                            // and disregard the current node, remember node number (1-based)
                            // for renumbering when adding elements from current mesh.
                            renumberNodes[i] = closest.No;
                            // Update boundary code
                            if (_code[closest.No - 1] == 0 && code != 0)
                                _code[closest.No - 1] = code;
                            // Store some statistics
                            NodeMergeCount++;
                            // Skip from here, since the current node should not be added
                            // to the merged mesh.
                            continue;
                        }
                    }
                }

                // Add node to merged mesh  把节点增加到合并的mesh文件中
                _x.Add(x);
                _y.Add(y);
                _z.Add(z);
                _code.Add(code);

                // Number of node (1-based) in new mesh
                int nodeNum = _x.Count;
                // remember new node number, for renumbering when adding elements from current mesh
                renumberNodes[i] = nodeNum;
            }  //for循环结束

            // Add all new nodes to the quad search tree. New nodes are first added to the search tree 
            // now, such that the merge routine never merges two nodes from the same mesh.
            for (int i = newMeshFirstNodeIndex; i < _x.Count; i++)
            {
                // Add to search tree
                QuadSearchTree.Point p = new QuadSearchTree.Point()
                {
                    No = i + 1,
                    X = _x[i],
                    Y = _y[i],
                };
                _nodeSearchTree.Add(p);
            }

            // Add all elements
            for (int i = 0; i < mesh.NumberOfElements; i++)
            {
                _elementType.Add(mesh.ElementType[i]);
                // Renumber the nodes of each element to the new node numbers
                int[] meshElmt = mesh.ElementTable[i];
                int[] newelmt = new int[meshElmt.Length];
                for (int j = 0; j < meshElmt.Length; j++)
                {
                    newelmt[j] = renumberNodes[meshElmt[j] - 1];
                }
                _connectivity.Add(newelmt);
            }

        }  //AddMesh 结尾

        private void UpdatePointExtent(Extent e, double x, double y, double tol)
        {
            e.XMin = x - tol;
            e.XMax = x + tol;
            e.YMin = y - tol;
            e.YMax = y + tol;
        }

    }

    // 简单的2D搜索树，基于一个2D的quad树 
    internal class QuadSearchTree
    {
        public class Point
        {
            public int No;
            public double X;
            public double Y;
        }

        /// <summary>
        /// Head of the tree
        /// </summary>
        private readonly TreeNode _head;

        /// <summary>
        /// Number of coordinates in search tree
        /// </summary>
        private int _numPoints;

        /// <summary>
        /// Create a new search tree that covers the provided <paramref name="extent"/>
        /// </summary>
        /// <param name="extent">Extent that the search tree should cover</param>
        public QuadSearchTree(Extent extent)
        {
            _head = new TreeNode(extent);
        }

        /// <summary>
        /// Number of coordinates in search tree
        /// </summary>
        public int Count
        {
            get { return _numPoints; }
        }

        /// <summary>
        /// Add point to the search tree, thereby building the tree.
        /// </summary>
        /// <param name="point">xy point to add</param>
        /// <returns>Returns true on success, false if point already exists in tree</returns>
        public bool Add(Point point)
        {
            bool added = _head.Add(point);
            if (added)
                _numPoints++;
            return added;
        }

        /// <summary>
        /// Find points that is included in the provided <paramref name="extent"/>
        /// </summary>
        /// <param name="extent">Extent to look for points within</param>
        /// <param name="points">List to put points in</param>
        public void Find(Extent extent, List<Point> points)
        {
            _head.Find(extent, points);
        }

        private class TreeNode
        {
            public int MaxPointsPerNode = 10;

            private readonly Extent _extent;
            private TreeNode[] _children;
            private List<Point> _points = new List<Point>();

            public TreeNode(Extent extent)
            {
                _extent = extent;
            }

            private bool HasChildren
            {
                get { return (_children != null); }
            }

            public bool Add(Point point)
            {
                bool added = false;
                // Check if inside this domain
                if (!_extent.Contains(point.X, point.Y))
                    return false;

                // If has children, add recursively
                if (HasChildren)
                {
                    foreach (TreeNode child in _children)
                    {
                        added |= child.Add(point);
                    }
                }
                else // it does not have children, add it here
                {
                    // Check if it already exists
                    foreach (Point existingPoint in _points)
                    {
                        if (point.X == existingPoint.X && point.Y == existingPoint.Y)
                            return false; // It did exist, do nothing
                    }
                    // Add point
                    _points.Add(point);
                    added = true;

                    // Check if we should subdivide
                    if (_points.Count > MaxPointsPerNode)
                    {
                        SubDivide();
                    }
                }
                return (added);
            }

            private void SubDivide()
            {
                // Create children
                _children = new TreeNode[4];

                double xMid = 0.5 * (_extent.XMin + _extent.XMax);
                double yMid = 0.5 * (_extent.YMin + _extent.YMax);
                _children[0] = new TreeNode(new Extent(xMid, _extent.XMax, yMid, _extent.YMax));
                _children[1] = new TreeNode(new Extent(_extent.XMin, xMid, yMid, _extent.YMax));
                _children[2] = new TreeNode(new Extent(_extent.XMin, xMid, _extent.YMin, yMid));
                _children[3] = new TreeNode(new Extent(xMid, _extent.XMax, _extent.YMin, yMid));

                // Add points of this node to the new children
                foreach (Point point in _points)
                {
                    foreach (TreeNode child in _children)
                    {
                        child.Add(point);
                    }
                }

                // Delete points of this node
                _points.Clear();
                _points = null;

            }


            public void Find(Extent extent, List<Point> elmts)
            {
                // If no overlap, just return
                if (!_extent.Overlaps(extent))
                    return;

                // If has children, ask those
                if (HasChildren)
                {
                    foreach (TreeNode child in _children)
                    {
                        child.Find(extent, elmts);
                    }
                }
                else // No children, search in elements of this node.
                {
                    foreach (Point coor in _points)
                    {
                        if (extent.Contains(coor.X, coor.Y))
                        {
                            elmts.Add(coor);
                        }
                    }
                }
            }
        }
    }

    // xy坐标区域类
    public class Extent
    {
        /// <summary>
        /// Minimum x coordinate of extent
        /// </summary>
        public double XMin;
        /// <summary>
        /// Maximum x coordinate of extent
        /// </summary>
        public double XMax;
        /// <summary>
        /// Minimum y coordinate of extent
        /// </summary>
        public double YMin;
        /// <summary>
        /// Maximum y coordinate of extent
        /// </summary>
        public double YMax;

        //无参构造函数
        public Extent()
        {
            XMin = Double.MaxValue;
            XMax = Double.MinValue;
            YMin = Double.MaxValue;
            YMax = Double.MinValue;
        }

        //有参构造函数
        public Extent(double xmin, double xmax, double ymin, double ymax)
        {
            XMin = xmin;
            XMax = xmax;
            YMin = ymin;
            YMax = ymax;
        }

        /// <summary>
        /// Copy constructor that ininitalizes the extent to a certain extent.
        /// </summary>
        public Extent(Extent other)
        {
            XMin = other.XMin;
            XMax = other.XMax;
            YMin = other.YMin;
            YMax = other.YMax;
        }

        /// <summary>
        /// Make this extent include <paramref name="other"/>. This will
        /// grow this extent, if the <paramref name="other"/> point is outside
        /// this extent.
        /// </summary>
        /// <param name="other">Other extent to include</param>
        public void Include(Extent other)
        {
            if (other.XMin < XMin)
                XMin = other.XMin;
            if (other.XMax > XMax)
                XMax = other.XMax;
            if (other.YMin < YMin)
                YMin = other.YMin;
            if (other.YMax > YMax)
                YMax = other.YMax;
        }

        /// <summary>
        /// Make this extent include the xy-point. This will
        /// grow this extent, if the xy-point is outside
        /// this extent.
        /// </summary>
        /// <param name="x">x coordinate of point to include</param>
        /// <param name="y">y coordinate of point to include</param>
        public void Include(double x, double y)
        {
            if (x < XMin)
                XMin = x;
            if (x > XMax)
                XMax = x;
            if (y < YMin)
                YMin = y;
            if (y > YMax)
                YMax = y;
        }

        /// <summary>
        /// Checks if this extent contains the xy-point
        /// </summary>
        /// <param name="x">x coordinate of point to include</param>
        /// <param name="y">y coordinate of point to include</param>
        /// <returns>True if xy-point is inside (or on boundary of) this extent.</returns>
        public bool Contains(double x, double y)
        {
            return (
                       XMin <= x && x <= XMax &&
                       YMin <= y && y <= YMax
                   );
        }

        /// <summary>
        /// Checks if this extent overlaps the other extent
        /// </summary>
        /// <param name="other">Extent to check overlap with</param>
        /// <returns>True if the two extends overlaps</returns>
        public bool Overlaps(Extent other)
        {
            return
                (
                    XMin <= other.XMax && XMax >= other.XMin &&
                    YMin <= other.YMax && YMax >= other.YMin
                );
        }
    }

    //专用于mesh错误验证的类
    public class MeshValidator
    {
        /// <summary>
        /// For each node, list of all faces starting
        /// from the node. The list for each node contains to-node
        /// indices (0-based) for the face.
        /// <para>
        /// If a face is an internal face, it exists in both
        /// directions, i.a. A->B and B->A.
        /// </para>
        /// </summary>
        public List<int>[] FaceNodes { get; private set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; private set; }

        private int _internalFaceCount;
        private int[] _faceCodeCount;

        public void ValidateMesh(double[] xs, double[] ys, int[] codes, int[][] connectivity)
        {
            Errors = new List<string>();
            _internalFaceCount = 0;
            _faceCodeCount = new int[codes.Max() + 1];

            BuildFaceInfo(xs.Length, connectivity);
            ValidateFaceInfo(codes);

        }

        private void BuildFaceInfo(int numNodes, int[][] connectivity)
        {
            FaceNodes = new List<int>[numNodes];
            for (int i = 0; i < numNodes; i++)
                FaceNodes[i] = new List<int>();

            for (int i = 0; i < connectivity.Length; i++)
            {
                int[] elmt = connectivity[i];
                for (int j = 0; j < elmt.Length; j++)
                {
                    int fromNode = elmt[j] - 1;
                    int toNode = elmt[(j + 1) % elmt.Length] - 1;
                    AddFace(fromNode, toNode);
                }
            }
        }

        private void AddFace(int fromNode, int toNode)
        {
            List<int> toNodes = FaceNodes[fromNode];
            if (toNodes.Contains(toNode))
            {
                #region 此处错误报告翻译成汉语了
                Errors.Add(string.Format("(0)无效的mesh: 两层mesh， 从节点 {0} 到节点 {1}。 " +
                                         "提示: 可能太多的节点重复." +
                                         "可以试着减小节点容忍度（mdf方法中变量tol）的值。",
                                          fromNode + 1, toNode + 1));
                #endregion
            }
            else
            {
                toNodes.Add(toNode);
            }
        }

        private void ValidateFaceInfo(int[] codes)
        {
            for (int fromNode = 0; fromNode < FaceNodes.Length; fromNode++)
            {
                List<int> toNodes = FaceNodes[fromNode];

                for (int i = 0; i < toNodes.Count; i++)
                {
                    int toNode = toNodes[i];

                    // If the opposite face exists, this face is an internal face
                    if (FaceNodes[toNode].Contains(fromNode))
                    {
                        FaceInternal();
                        continue;
                    }

                    // Opposite face does not exist, so it is a boundary face.
                    int fromCode = codes[fromNode];
                    int toCode = codes[toNode];

                    // True if "invalid" boundary face, then set it as internal face.
                    bool internalFace = false;

                    if (fromCode == 0)
                    {
                        #region 此处错误报告翻译成汉语了
                        Errors.Add(string.Format("(1)无效的mesh: 边界面, 从节点 {0} 到节点 {1} 丢失了节点 {0} 的边界代码. " +
                                                 "提示: 修改节点 {0} 的边界代码",
                                                 fromNode + 1, toNode + 1));
                        #endregion
                        internalFace = true;
                    }
                    if (toCode == 0)
                    {
                        #region 此处错误报告翻译成汉语了
                        Errors.Add(string.Format("(2)无效的mesh: 边界面, 从节点 {0} 到节点 {1} 丢失了节点 {1} 的边界代码. " +
                                                 "提示: 修改节点 {1} 的边界代码",
                                                 fromNode + 1, toNode + 1));
                        #endregion
                        internalFace = true;
                    }

                    int faceCode;

                    // Find face code:
                    // 1) In case any of the nodes is a land node (code value 1) then the
                    //    boundary face is a land face, given boundary code value 1.
                    // 2) For boundary faces (if both fromNode and toNode have code values larger than 1), 
                    //    the face code is the boundary code value of toNode.
                    if (fromCode == 1 || toCode == 1)
                        faceCode = 1;
                    else
                        faceCode = toCode;

                    if (internalFace)
                        FaceInternal();
                    else
                        FaceBoundary(faceCode);
                }
            }
        }

        private void FaceInternal()
        {
            _internalFaceCount++;
        }

        private void FaceBoundary(int code)
        {
            _faceCodeCount[code]++;
        }

        public List<KeyValuePair<int, int>> GetFaceCodeStatistics()
        {
            List<KeyValuePair<int, int>> res = new List<KeyValuePair<int, int>>();

            // Divide internal faces with two, since they are counted twice
            res.Add(new KeyValuePair<int, int>(0, _internalFaceCount));
            for (int i = 0; i < _faceCodeCount.Length; i++)
            {
                int c = _faceCodeCount[i];
                if (c > 0)
                {
                    res.Add(new KeyValuePair<int, int>(i, c));
                }
            }
            return res;
        }
    }
    #endregion

}