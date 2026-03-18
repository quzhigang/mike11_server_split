using System;
using System.Collections.Generic;
using System.Linq;
using bjd_model.Const_Global;
using OSGeo.OGR;
using OSGeo.OSR;

namespace bjd_model.Mike11
{
    //包含x,y坐标和值的结构体
    [Serializable]
    public struct Valuexy
    {
        public int number;
        public double X;
        public double Y;
        public double Z;
        public double value;
    }

    //子区域坐标
    [Serializable]
    public struct SubArea
    {
        public double x1;
        public double y1;

        public double x2;
        public double y2;
        public double Getarea()
        {
            return Math.Abs((x2 - x1) * (y2 - y1));
        }
    }

    //含XYZ三元素的点结构体
    [Serializable]
    public struct PointXYZ
    {
        public double X;
        public double Y;
        public double Z;
        public static PointXYZ Get_PointXYZ(double x, double y, double z)
        {
            PointXYZ point;
            point.X = x;
            point.Y = y;
            point.Z = z;
            return point;
        }

        public static PointXYZ Get_PointXYZ(double x, double y, double z, int del)
        {
            PointXYZ point;
            point.X = Math.Round(x, del);
            point.Y = Math.Round(y, del);
            point.Z = z;
            return point;
        }
    }

    //含XZ和标记信息点的结构体
    [Serializable]
    public struct PointXZS
    {
        public double X;
        public double Z;
        public int sign;

        public static PointXZS Get_PointXZS(double x, double z, int sign)
        {
            PointXZS pointxzs;
            pointxzs.X = x;
            pointxzs.Z = z;
            pointxzs.sign = sign;
            return pointxzs;
        }
    }

    //含经纬度和XY投影坐标的点
    [Serializable]
    public struct PointXYJW
    {
        public double X;
        public double Y;
        public double JD;
        public double WD;
        public static PointXYJW Get_PointXYJW(double x, double y, double jd, double wd)
        {
            PointXYJW p;
            p.X = x;
            p.Y = y;
            p.JD = jd;
            p.WD = wd;
            return p;
        }
    }

    [Serializable]
    public enum Pointdata_type
    {
        xy,
        jw
    }

    //点位置和判断是否在元素内的结构体
    [Serializable]
    public struct PointXY
    {
        public double X;
        public double Y;

        public static PointXY Get_Pointxy(double x, double y)
        {
            PointXY point;
            point.X = x;
            point.Y = y;
            return point;
        }

        public static PointXY Get_Pointxy(double x, double y, int del)
        {
            PointXY point;
            point.X = del == -1 ? x : Math.Round(x, del);
            point.Y = del == -1 ? y : Math.Round(y, del);
            return point;
        }

        // 判断点是否在三角形内
        public static bool Pointinelm(PointXY p, double[] X, double[] Y, int[] elmpoint)
        {
            bool inside = false;
            VectorS vectA = new VectorS(X[elmpoint[0] - 1], Y[elmpoint[0] - 1], 0.0);
            VectorS vectB = new VectorS(X[elmpoint[1] - 1], Y[elmpoint[1] - 1], 0.0);
            VectorS vectC = new VectorS(X[elmpoint[2] - 1], Y[elmpoint[2] - 1], 0.0);
            VectorS vectP = new VectorS(p.X, p.Y, 0.0);

            if (VectorS.PointinTriangle(vectA, vectB, vectC, vectP) == true)
            {
                inside = true;
            }
            return inside;
        }

        // 判断点是否在多边形内.
        public static bool IsInPolygon(PointXY p, List<PointXY> polygonPoints)
        {
            bool inside = false;
            int pointCount = polygonPoints.Count;
            PointXY p1, p2;

            for (int i = 0, j = pointCount - 1; i < pointCount; j = i, i++)
            {
                p1 = polygonPoints[i];
                p2 = polygonPoints[j];
                if (p.Y < p2.Y)
                {
                    if (p1.Y <= p.Y)
                    {
                        if ((p.Y - p1.Y) * (p2.X - p1.X) > (p.X - p1.X) * (p2.Y - p1.Y))
                        {
                            inside = (!inside);
                        }
                    }
                }
                else if (p.Y < p1.Y)
                {
                    if ((p.Y - p1.Y) * (p2.X - p1.X) < (p.X - p1.X) * (p2.Y - p1.Y))
                    {
                        inside = (!inside);
                    }
                }
            }
            return inside;
        }

        public static bool IsInPolygon(PointXY p, List<List<double>> polygonPoints)
        {
            bool inside = false;
            int pointCount = polygonPoints.Count;
            PointXY p1, p2;

            for (int i = 0, j = pointCount - 1; i < pointCount; j = i, i++)
            {
                p1 = PointXY.Get_Pointxy(polygonPoints[i][0], polygonPoints[i][1]);
                p2 = PointXY.Get_Pointxy(polygonPoints[j][0], polygonPoints[j][1]);
                if (p.Y < p2.Y)
                {
                    if (p1.Y <= p.Y)
                    {
                        if ((p.Y - p1.Y) * (p2.X - p1.X) > (p.X - p1.X) * (p2.Y - p1.Y))
                        {
                            inside = (!inside);
                        }
                    }
                }
                else if (p.Y < p1.Y)
                {
                    if ((p.Y - p1.Y) * (p2.X - p1.X) < (p.X - p1.X) * (p2.Y - p1.Y))
                    {
                        inside = (!inside);
                    }
                }
            }
            return inside;
        }

        public static void Getmaxmin(List<PointXY> polygonPoints, out double min_x, out double max_x, out double min_y, out double max_y)
        {
            min_x = polygonPoints[0].X;
            max_x = polygonPoints[0].X;
            min_y = polygonPoints[0].Y;
            max_y = polygonPoints[0].Y;

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                if (polygonPoints[i].X < min_x) min_x = polygonPoints[i].X;
            }

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                if (polygonPoints[i].X > max_x) max_x = polygonPoints[i].X;
            }

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                if (polygonPoints[i].Y < min_y) min_y = polygonPoints[i].Y;
            }

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                if (polygonPoints[i].Y > max_y) max_y = polygonPoints[i].Y;
            }
        }

        public static double Get_ptop_distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static int Get_ptop_Angle(PointXY start_p, PointXY end_p)
        {
            int angle = 0;
            if (end_p.X > start_p.X && end_p.Y > start_p.Y) angle = (int)(Math.Atan((end_p.X - start_p.X) / (end_p.Y - start_p.Y)) * 180 / 3.14);
            if (end_p.X > start_p.X && end_p.Y < start_p.Y) angle = -(int)(Math.Atan((end_p.X - start_p.X) / (end_p.Y - start_p.Y)) * 180 / 3.14);
            if (end_p.X < start_p.X && end_p.Y < start_p.Y) angle = 180 + (int)(Math.Atan((end_p.X - start_p.X) / (end_p.Y - start_p.Y)) * 180 / 3.14);
            if (end_p.X < start_p.X && end_p.Y > start_p.Y) angle = (int)(Math.Atan((end_p.X - start_p.X) / (end_p.Y - start_p.Y)) * 180 / 3.14);
            return angle;
        }

        public static double PToL_Distance(double x1, double y1, double x2, double y2, double x0, double y0)
        {
            double distance = 0;
            double a, b, c;
            a = Get_ptop_distance(x1, y1, x2, y2);
            b = Get_ptop_distance(x1, y1, x0, y0);
            c = Get_ptop_distance(x2, y2, x0, y0);
            if (c <= 0.000001 || b <= 0.000001) return 0;
            if (a <= 0.000001) return b;
            if (c * c >= a * a + b * b) return b;
            if (b * b >= a * a + c * c) return c;
            double p = (a + b + c) / 2;
            double s = Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            distance = 2 * s / a;
            return distance;
        }

        public static PointXY GetProjectivePoint(PointXY pLine, double k, PointXY p)
        {
            PointXY pProject;
            if (k == 0)
            {
                pProject.X = p.X;
                pProject.Y = pLine.Y;
            }
            else
            {
                pProject.X = (float)((k * pLine.X + p.X / k + p.Y - pLine.Y) / (1 / k + k));
                pProject.Y = (float)(-1 / k * (pProject.X - p.X) + p.Y);
            }
            return pProject;
        }

        public static double GetAngle(PointXY up_point, PointXY down_point, PointXY point)
        {
            PointXY newpoint1;
            newpoint1.X = down_point.X - up_point.X;
            newpoint1.Y = down_point.Y - up_point.Y;
            PointXY newpoint2;
            newpoint2.X = point.X - up_point.X;
            newpoint2.Y = point.Y - up_point.Y;

            double Longth = newpoint1.X * newpoint2.X + newpoint1.Y * newpoint2.Y;
            double newpoint1_Longth = Math.Sqrt(Math.Pow(newpoint1.X, 2) + Math.Pow(newpoint1.Y, 2));
            double newpoint2_Lingth = Math.Sqrt(Math.Pow(newpoint2.X, 2) + Math.Pow(newpoint2.Y, 2));

            double cosAngle = Longth / (newpoint1_Longth * newpoint2_Lingth);
            double Angle = Math.Acos(cosAngle) * 180 / Math.PI;

            double slope1 = (down_point.Y - up_point.Y) / (down_point.X - up_point.X);
            double slope2 = (point.Y - up_point.Y) / (point.X - up_point.X);
            double multiply = (slope2 - slope1) * (1 + slope1 * slope2);
            if ((Angle >= 0) && (Angle < 90) && (multiply >= 0))
            {
                return Angle;
            }
            else if ((Angle > 0) && (Angle < 90) && (multiply < 0))
            {
                return -Angle;
            }
            else if ((Angle > 90) && (Angle < 180) && (multiply <= 0))
            {
                return Angle;
            }
            else if ((Angle > 90) && (Angle < 180) && (multiply >= 0))
            {
                return -Angle;
            }
            else if (Angle == 90)
            {
                if (slope2 - slope1 > 0) return Angle;
                return -Angle;
            }
            else if (Angle == 180)
            {
                return Angle;
            }
            else
            {
                Console.WriteLine("转角计算错误，返回0！");
                return 0;
            }
        }

        public static PointXY Get_CenterPointxy(List<PointXY> polygonPoints)
        {
            PointXY centerpoint;
            double sumx = 0;
            double sumy = 0;
            for (int i = 0; i < polygonPoints.Count; i++)
            {
                sumx += polygonPoints[i].X;
                sumy += polygonPoints[i].Y;
            }
            centerpoint.X = sumx / polygonPoints.Count;
            centerpoint.Y = sumy / polygonPoints.Count;
            return centerpoint;
        }

        public static double Get_PolygonArea(List<PointXY> polygonPoints)
        {
            double area = 0;
            List<PointXY> Points = polygonPoints[0].X < 200 ? PointXY.CoordTranfrom(polygonPoints, 4326, 4547, 3) : polygonPoints;
            double iArea = 0;
            int iCount = Points.Count;
            for (int i = 0; i < iCount; i++)
            {
                iArea = iArea + (Points[i].X * Points[(i + 1) % iCount].Y - Points[(i + 1) % iCount].X * Points[i].Y);
            }

            if (polygonPoints[0].X < 200)
            {
                area = Math.Abs(0.5 * iArea) / 1.052;
            }
            else
            {
                area = Math.Abs(0.5 * iArea);
            }
            return area;
        }

        public static List<PointXY> Get_Distance_LRPoints(List<ReachPoint> reachpoint_list, double chainage, double L_distance, double R_distance)
        {
            List<PointXY> LRPoints = new List<PointXY>();
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
                    double start_distance = PointXY.Get_ptop_distance(start_reachpoint.X, start_reachpoint.Y, end_reachpoint.X, end_reachpoint.Y);
                    double chainage_distance = chainage - start_reachpoint.pointchainage;
                    sectionpoint.X = start_reachpoint.X + (end_reachpoint.X - start_reachpoint.X) * chainage_distance / start_distance;
                    sectionpoint.Y = start_reachpoint.Y + (end_reachpoint.Y - start_reachpoint.Y) * chainage_distance / start_distance;
                    break;
                }
            }

            PointXY point1000;
            double distance = PointXY.Get_ptop_distance(start_reachpoint.X, start_reachpoint.Y, end_reachpoint.X, end_reachpoint.Y);
            point1000.X = sectionpoint.X - 1000 * (end_reachpoint.Y - sectionpoint.Y) / (distance - PointXY.Get_ptop_distance(sectionpoint.X, sectionpoint.Y, start_reachpoint.X, start_reachpoint.Y));
            point1000.Y = 1000 * (end_reachpoint.X - sectionpoint.X) / (distance - PointXY.Get_ptop_distance(sectionpoint.X, sectionpoint.Y, start_reachpoint.X, start_reachpoint.Y)) + sectionpoint.Y;

            PointXY point_l;
            PointXY point_r;
            point_l.X = sectionpoint.X + L_distance * (point1000.X - sectionpoint.X) / 1000;
            point_l.Y = sectionpoint.Y + L_distance * (point1000.Y - sectionpoint.Y) / 1000;

            point_r.X = point_l.X + (L_distance + R_distance) * (sectionpoint.X - point_l.X) / L_distance;
            point_r.Y = point_l.Y + (L_distance + R_distance) * (sectionpoint.Y - point_l.Y) / L_distance;

            LRPoints.Add(point_l);
            LRPoints.Add(point_r);

            return LRPoints;
        }

        public static void Get_Distance_LRPoints(PointXY start_point, PointXY end_point, double L_distance, double R_distance, out PointXY point_l, out PointXY point_r, double start_distance = 0)
        {
            PointXY sectionpoint;
            double ptop_distance = PointXY.Get_ptop_distance(start_point.X, start_point.Y, end_point.X, end_point.Y);
            sectionpoint.X = start_point.X + (end_point.X - start_point.X) * start_distance / ptop_distance;
            sectionpoint.Y = start_point.Y + (end_point.Y - start_point.Y) * start_distance / ptop_distance;

            PointXY point1000;
            double distance = PointXY.Get_ptop_distance(start_point.X, start_point.Y, end_point.X, end_point.Y);
            point1000.X = sectionpoint.X - 1000 * (end_point.Y - sectionpoint.Y) / (distance - start_distance);
            point1000.Y = 1000 * (end_point.X - sectionpoint.X) / (distance - start_distance) + sectionpoint.Y;

            point_l.X = sectionpoint.X + L_distance * (point1000.X - sectionpoint.X) / 1000;
            point_l.Y = sectionpoint.Y + L_distance * (point1000.Y - sectionpoint.Y) / 1000;

            point_r.X = point_l.X + (L_distance + R_distance) * (sectionpoint.X - point_l.X) / L_distance;
            point_r.Y = point_l.Y + (L_distance + R_distance) * (sectionpoint.Y - point_l.Y) / L_distance;

            if (ptop_distance == 0 || R_distance == 0 || L_distance == 0)
            {
                point_l.X = 0; point_l.Y = 0; point_r.X = 0; point_r.Y = 0;
            }
        }

        public static List<PointXY> CoordTranfrom(List<PointXY> points, int srcCode, int tgtCode, int del)
        {
            List<PointXY> newPoints = new List<PointXY>();
            Ogr.RegisterAll();
            SpatialReference srcSrs = new SpatialReference(null);
            srcSrs.ImportFromEPSG(srcCode);
            SpatialReference tgtSrs = new SpatialReference(null);
            tgtSrs.ImportFromEPSG(tgtCode);
            CoordinateTransformation coordTrans = new CoordinateTransformation(srcSrs, tgtSrs);
            for (int i = 0; i < points.Count; i++)
            {
                double[] coords = new double[] { points[i].Y, points[i].X };
                coordTrans.TransformPoint(coords);
                newPoints.Add(PointXY.Get_Pointxy(coords[1], coords[0], del));
            }
            return newPoints;
        }

        public static List<PointXYZ> CoordTranfrom(List<PointXYZ> points, int srcCode, int tgtCode, int del)
        {
            List<PointXYZ> newPoints = new List<PointXYZ>();
            Ogr.RegisterAll();
            SpatialReference srcSrs = new SpatialReference(null);
            srcSrs.ImportFromEPSG(srcCode);
            SpatialReference tgtSrs = new SpatialReference(null);
            tgtSrs.ImportFromEPSG(tgtCode);
            CoordinateTransformation coordTrans = new CoordinateTransformation(srcSrs, tgtSrs);
            for (int i = 0; i < points.Count; i++)
            {
                double[] coords = new double[] { points[i].Y, points[i].X };
                coordTrans.TransformPoint(coords);
                newPoints.Add(PointXYZ.Get_PointXYZ(coords[1], coords[0], points[i].Z, del));
            }
            return newPoints;
        }

        public static List<PointXY> CoordTranfrom1(List<PointXYZ> points, int srcCode, int tgtCode, int del)
        {
            List<PointXY> newPoints = new List<PointXY>();
            Ogr.RegisterAll();
            SpatialReference srcSrs = new SpatialReference(null);
            srcSrs.ImportFromEPSG(srcCode);
            SpatialReference tgtSrs = new SpatialReference(null);
            tgtSrs.ImportFromEPSG(tgtCode);
            CoordinateTransformation coordTrans = new CoordinateTransformation(srcSrs, tgtSrs);
            for (int i = 0; i < points.Count; i++)
            {
                double[] coords = new double[] { points[i].Y, points[i].X };
                coordTrans.TransformPoint(coords);
                newPoints.Add(PointXY.Get_Pointxy(coords[1], coords[0], del));
            }
            return newPoints;
        }

        public static List<PointXY> CoordTranfrom(double[] x, double[] y, int srcCode, int tgtCode, int del)
        {
            List<PointXY> newPoints = new List<PointXY>();
            Ogr.RegisterAll();
            SpatialReference srcSrs = new SpatialReference(null);
            srcSrs.ImportFromEPSG(srcCode);
            SpatialReference tgtSrs = new SpatialReference(null);
            tgtSrs.ImportFromEPSG(tgtCode);
            CoordinateTransformation coordTrans = new CoordinateTransformation(srcSrs, tgtSrs);
            for (int i = 0; i < x.Length; i++)
            {
                double[] coords = new double[] { y[i], x[i] };
                coordTrans.TransformPoint(coords);
                newPoints.Add(PointXY.Get_Pointxy(coords[1], coords[0], del));
            }
            return newPoints;
        }

        public static PointXY CoordTranfrom(PointXY point, int srcCode, int tgtCode, int del)
        {
            Ogr.RegisterAll();
            SpatialReference srcSrs = new SpatialReference(null);
            srcSrs.ImportFromEPSG(srcCode);
            SpatialReference tgtSrs = new SpatialReference(null);
            tgtSrs.ImportFromEPSG(tgtCode);
            CoordinateTransformation coordTrans = new CoordinateTransformation(srcSrs, tgtSrs);
            double[] coords = new double[] { point.Y, point.X };
            coordTrans.TransformPoint(coords);
            PointXY new_point = PointXY.Get_Pointxy(coords[1], coords[0], del);
            return new_point;
        }

        public static PointXYZ CoordTranfrom(PointXYZ point, int srcCode, int tgtCode, int del)
        {
            Ogr.RegisterAll();
            SpatialReference srcSrs = new SpatialReference(null);
            srcSrs.ImportFromEPSG(srcCode);
            SpatialReference tgtSrs = new SpatialReference(null);
            tgtSrs.ImportFromEPSG(tgtCode);
            CoordinateTransformation coordTrans = new CoordinateTransformation(srcSrs, tgtSrs);
            double[] coords = new double[] { point.Y, point.X };
            coordTrans.TransformPoint(coords);
            PointXYZ new_point = PointXYZ.Get_PointXYZ(coords[1], coords[0], point.Z, del);
            return new_point;
        }

        public static double Insert_double(double zhs1, double zhs2, double su1, double su2, double zhs)
        {
            double res = 0;
            if (zhs >= zhs1 && zhs <= zhs2)
            {
                res = zhs2 == zhs1 ? (su2 + su1) / 2 : su1 + (su2 - su1) * (zhs - zhs1) / (zhs2 - zhs1);
            }
            return res;
        }

        public static float Insert_double(float zhs1, float zhs2, float su1, float su2, float zhs)
        {
            float res = 0;
            if (zhs >= zhs1 && zhs <= zhs2)
            {
                res = su1 + (su2 - su1) * (zhs - zhs1) / (zhs2 - zhs1);
            }
            return res;
        }

        public static double Get_Centra(string Coordinate_type)
        {
            double centra = 114.0;
            char[] splitchar = new char[] { '"' };
            string str;
            if (Coordinate_type == Model_Const.DEFAULT_COOR)
            {
                str = Model_Const.DEFAULT_COOR;
            }
            else
            {
                str = Coordinate_type.Split(splitchar)[1];
            }

            string str1 = str.Split(new char[] { '_' }).Last();
            if (str.EndsWith("E"))
            {
                centra = double.Parse(str1.Substring(0, str1.Length - 1));
            }
            else
            {
                centra = double.Parse(str1) * 3;
            }
            return centra;
        }

        public static string Changeto_dms(double degree)
        {
            double[] dms = new double[3];
            double d = Math.Floor(degree);
            double m = Math.Floor((degree - d) * 60);
            double s = Math.Round(((degree - d) * 60 - m) * 60, 1);
            dms[0] = d;
            dms[1] = m;
            dms[2] = s;
            return dms[0].ToString() + " " + dms[1].ToString() + " " + dms[2].ToString();
        }
    }

    //判断点是否在三角形内的类
    [Serializable]
    public class VectorS
    {
        public VectorS(double fx, double fy, double fz)
        {
            this.x = fx;
            this.y = fy;
            this.z = fz;
        }

        double x, y, z;
        public static VectorS substract(VectorS v1, VectorS v2)
        {
            VectorS vect = new VectorS(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
            return vect;
        }

        double Dot(VectorS v)
        {
            return this.x * v.x + this.y * v.y + this.z * v.z;
        }

        public VectorS Cross(VectorS v)
        {
            VectorS vect = new VectorS(this.y * v.z - this.z * v.y, this.z * v.x - this.x * v.z, this.x * v.y - this.y * v.x);
            return vect;
        }

        public static bool SameSide(VectorS A, VectorS B, VectorS C, VectorS P)
        {
            VectorS AB = substract(B, A);
            VectorS AC = substract(C, A);
            VectorS AP = substract(P, A);

            VectorS v1 = AB.Cross(AC);
            VectorS v2 = AB.Cross(AP);

            return (v1.Dot(v2) >= 0);
        }

        public static bool PointinTriangle(VectorS A, VectorS B, VectorS C, VectorS P)
        {
            return (SameSide(A, B, C, P) && SameSide(B, C, A, P) && SameSide(C, A, B, P));
        }
    }
}
