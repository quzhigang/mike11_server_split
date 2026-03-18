using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bjd_model.Mike11;

namespace bjd_model.Common
{
    public class Gis_Service
    {
        //调用GIS切割剖面服务接口，获取断面高程点和标记( ** 今后该方案需调用GIS服务 **)
        public static List<PointXZS> Get_Sectiondata_FromGisService(List<PointXY> points)
        {
            List<PointXZS> Sectiondata = new List<PointXZS>();
            for (int i = 0; i < 10; i++)
            {
                PointXZS point;
                point.X = i * 10;
                point.Z = 50 + (5 - i % 5) * 2;
                if (i == 0)
                {
                    point.sign = 1;
                }

                if (i == 9)
                {
                    point.sign = 3;
                }

                if (i == 4)
                {
                    point.sign = 2;
                }
            }

            string section_txt = @"D:\sectiondata1.txt";
            Dictionary<AtReach, List<PointXZS>> reach_section = Xns11.ReadSectionDataFile(section_txt);
            Sectiondata = reach_section.Values.ElementAt(0);

            return Sectiondata;
        }

        //调用GIS内插DEM高程服务接口，获取网格顶点地面高程( ** 今后该方案需调用GIS服务 **)
        public static List<PointXYZ> Get_Nodes_DemZ_FromGisService(List<PointXY> points)
        {
            List<PointXYZ> result_points = new List<PointXYZ>();

            for (int i = 0; i < points.Count; i++)
            {
                double z = 53 + (i % 10) * 1.0 / 10;
                PointXYZ point = PointXYZ.Get_PointXYZ(points[i].X, points[i].Y, z);
                result_points.Add(point);
            }

            return result_points;
        }

        //调用GIS内插糙率值服务接口，获取网格顶点糙率值( ** 今后该方案需调用GIS服务 **)
        public static List<PointXYZ> Get_Nodes_Clvalue_FromGisService(List<PointXY> points)
        {
            List<PointXYZ> result_points = new List<PointXYZ>();

            for (int i = 0; i < points.Count; i++)
            {
                double z = 20 + (i % 10) * 1.0;
                PointXYZ point = PointXYZ.Get_PointXYZ(points[i].X, points[i].Y, z);
                result_points.Add(point);
            }

            return result_points;
        }

    }

}