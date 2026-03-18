using System;
using System.Collections.Generic;
using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;

using bjd_model;
using System.Data;
using Kdbndp;

using Newtonsoft.Json;

namespace bjd_model.Mike11
{
    //定义多边形几何的类
    [Serializable]
    public class Geometry_Ploygon
    {
        //构造函数
        public Geometry_Ploygon()
        {
            this.type = "Polygon";
            this.coordinates = new List<List<List<double>>>();
        }

        public Geometry_Ploygon(List<List<List<double>>> coordinates)
        {
            this.type = "Polygon";
            this.coordinates = coordinates;
        }

        //属性
        public string type
        { get; set; }

        //(区域(一般就1个) --> 点的集合 --> 点的x,y,z集合 )
        public List<List<List<double>>> coordinates
        { get; set; }
    }

    //定义点几何的类
    [Serializable]
    public class Geometry_Point
    {
        //构造函数
        public Geometry_Point()
        {
            this.type = "Point";
            this.coordinates = new List<double>() {};
        }

        public Geometry_Point(double x_value,double y_value)
        {
            this.type = "Point";
            this.coordinates = new List<double>() { x_value, y_value };
        }

        public Geometry_Point(double x_value, double y_value, double z_value)
        {
            this.type = "Point";
            this.coordinates = new List<double>() { x_value, y_value, z_value };
        }

        //属性
        public string type
        { get; set; }

        public List<double> coordinates
        { get; set; }
    }

    //定义线几何的类
    [Serializable]
    public class Geometry_Line
    {
        //构造函数
        public Geometry_Line()
        {
            this.type = "LineString";
            this.coordinates = new List<List<double>>();
        }

        public Geometry_Line(List<List<double>> coordinates)
        {
            this.type = "LineString";
            this.coordinates = coordinates;
        }

        //属性
        public string type
        { get; set; }

        public List<List<double>> coordinates
        { get; set; }
    }


    //用于定义面要素类中的要素的类
    [Serializable]
    public class Feature_Ploygon
    {
        //构造函数
        public Feature_Ploygon()
        {
            this.type = "Feature";
            this.id = 0;
            this.geometry = new Geometry_Ploygon();
            this.properties = new Dictionary<string, object>();
        }

        public Feature_Ploygon(int id, Geometry_Ploygon geometry, Dictionary<string, object> properties)
        {
            this.type = "Feature";
            this.id = id;
            this.geometry = geometry;
            this.properties = properties;
        }

        public Feature_Ploygon(int id, Geometry_Ploygon geometry)
        {
            this.type = "Feature";
            this.id = id;
            this.geometry = geometry;
            this.properties = new Dictionary<string, object>();
            this.properties.Add("FID", 0);
        }

        public string type
        { get; set; }

        public int id
        { get; set; }
        public Geometry_Ploygon geometry
        { get; set; }

        //属性
        public Dictionary<string, object> properties
        { get; set; }
    }

    //用于定义点要素类中的要素的类
    [Serializable]
    public class Feature_Point
    {
        //构造函数
        public Feature_Point()
        {
            this.type = "Feature";
            this.id = 0;
            this.geometry = new Geometry_Point();
            this.properties = new Dictionary<string, object>();
        }

        public Feature_Point(int id, Geometry_Point geometry, Dictionary<string, object> properties)
        {
            this.type = "Feature";
            this.id = id;
            this.geometry = geometry;
            this.properties = properties;
        }

        public string type
        { get; set; }

        public int id
        { get; set; }
        public Geometry_Point geometry
        { get; set; }

        //属性
        public Dictionary<string, object> properties
        { get; set; }
    }

    //用于定义线要素类中的要素的类
    [Serializable]
    public class Feature_Line
    {
        //构造函数
        public Feature_Line()
        {
            this.type = "Feature";
            this.id = 0;
            this.geometry = new Geometry_Line();
            this.properties = new Dictionary<string, object>();
        }

        public Feature_Line(int id, Geometry_Line geometry, Dictionary<string, object> properties)
        {
            this.type = "Feature";
            this.id = id;
            this.geometry = geometry;
            this.properties = properties;
        }

        public string type
        { get; set; }

        public int id
        { get; set; }
        public Geometry_Line geometry
        { get; set; }

        //属性
        public Dictionary<string, object> properties
        { get; set; }
    }


    //用于定义坐标参考空间的类
    [Serializable]
    public class CRS
    {
        //构造函数
        public CRS()
        {
            this.type = "name";
            this.properties = new CRS_Properties();
        }

        public CRS(string name, CRS_Properties properties)
        {
            this.type = name;
            this.properties = properties;
        }

        //属性
        public string type
        { get; set; }

        public CRS_Properties properties
        { get; set; }
    }

    //用于定义坐标参考空间的类的属性
    [Serializable]
    public class CRS_Properties
    {
        //构造函数
        public CRS_Properties()
        {
            this.name = "EPSG:4326";
        }

        public CRS_Properties(string name)
        {
            this.name = name;
        }

        //属性
        public string name
        { get; set; }
    }


    #region *************最终几何对象****************
    //生成GIS面要素GeoJSON的类
    [Serializable]
    public class GeoJson_Polygon
    {
        //构造函数
        public GeoJson_Polygon()
        {
            this.hasZ = false;
            this.type = "FeatureCollection";
            this.crs = new CRS();
            this.features = new List<Feature_Ploygon>();
        }

        public GeoJson_Polygon(bool hasZ, CRS crs, List<Feature_Ploygon> features)
        {
            this.hasZ = hasZ;
            this.type = "FeatureCollection";
            this.crs = crs;
            this.features = features;
        }


        //属性
        //是否启用Z
        public bool hasZ
        { get; set; }

        public string type
        { get; set; }

        //空间参考坐标系
        public CRS crs
        { get;set;}

        //要素另一个名字
        public List<Feature_Ploygon> features
        { get; set; }

        
        //从数据库中获取水面样例GIS结果,对经纬度取6位小数修改后写入数据(静态类)
        public static void Load_DmswSampleRes_AndWriteToBD()
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_SAMPLEGIS_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select polygon_sample_result from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到默认模型Gis结果!");
                return;
            }

            string sample_polygon_geojson = dt.Rows[0][0].ToString();
            GeoJson_Polygon hd_sample_gisploygon = JsonConvert.DeserializeObject<GeoJson_Polygon>(sample_polygon_geojson);
            List<Feature_Ploygon> features = hd_sample_gisploygon.features;
            for (int i = 0; i < features.Count; i++)
            {
                Geometry_Ploygon ploygon_geometry = features[i].geometry;
                List<List<List<double>>> points1 = ploygon_geometry.coordinates;
                for (int j = 0; j < points1.Count; j++)
                {
                    List<List<double>> points2 = points1[j];
                    for (int k = 0; k < points2.Count; k++)
                    {
                        List<double> points3 = points2[k];
                        points3[0] = Math.Round(points3[0], 6);  //经纬度保留6位小数
                        points3[1] = Math.Round(points3[1], 6);
                        points3[2] = Math.Round(points3[1], 2);
                    }
                }
            }

            //更新数据库
            string res_geojson = File_Common.Serializer_Obj(hd_sample_gisploygon);
            string sql = "update " + tableName + " set polygon_sample_result = '" + res_geojson + "' where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            Mysql.Execute_Command(sql);

            
        }
    }


    //生成GIS点要素GeoJSON的类
    [Serializable]
    public class GeoJson_Point
    {
        //构造函数
        public GeoJson_Point()
        {
            this.hasZ = false;
            this.type = "FeatureCollection";
            this.crs = new CRS();
            this.features = new List<Feature_Point>();
        }

        public GeoJson_Point(bool hasZ, CRS crs, List<Feature_Point> features)
        {
            this.hasZ = hasZ;
            this.type = "FeatureCollection";
            this.crs = crs;
            this.features = features;
        }


        //属性
        //是否启用Z
        public bool hasZ
        { get; set; }

        public string type
        { get; set; }

        //空间参考坐标系
        public CRS crs
        { get; set; }

        //要素另一个名字
        public List<Feature_Point> features
        { get; set; }
    }


    //生成GIS线要素GeoJSON的类
    [Serializable]
    public class GeoJson_Line
    {
        //构造函数
        public GeoJson_Line()
        {
            this.hasZ = false;
            this.type = "FeatureCollection";
            this.crs = new CRS();
            this.features = new List<Feature_Line>();
        }

        public GeoJson_Line(bool hasZ, CRS crs, List<Feature_Line> features)
        {
            this.hasZ = hasZ;
            this.type = "FeatureCollection";
            this.crs = crs;
            this.features = features;
        }


        //属性
        //是否启用Z
        public bool hasZ
        { get; set; }

        public string type
        { get; set; }

        //空间参考坐标系
        public CRS crs
        { get; set; }

        //要素另一个名字
        public List<Feature_Line> features
        { get; set; }
    }

    #endregion **************************************

}