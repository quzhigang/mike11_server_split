using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace bjd_model.Mike11
{
    //定义多边形几何的类
    [Serializable]
    public class ArcGisGeometry_Ploygon
    {
        //构造函数
        public ArcGisGeometry_Ploygon()
        {
            this.hasZ = false;
            this.type = "polygon";
            this.rings = new List<List<List<double>>>();
        }

        //构造函数
        public ArcGisGeometry_Ploygon(bool hasz_value, List<List<List<double>>> rings_value)
        {
            this.hasZ = hasz_value;
            this.type = "polygon";
            this.rings = rings_value;
        }

        //属性
        public bool hasZ
        { get; set; }

        public string type
        { get; set; }

        //(区域(一般就1个) --> 点的集合 --> 点的x,y,z集合 )
        public List<List<List<double>>> rings
        { get; set; }
    }

    //定义点几何的类
    [Serializable]
    public class Geometry_PointXY
    {
        //构造函数
        public Geometry_PointXY()
        {
            this.type = "point";
            this.x = 0;
            this.y = 0;
        }

        //构造函数
        public Geometry_PointXY(double x_value, double y_value)
        {
            this.type = "point";
            this.x = x_value;
            this.y = y_value;
        }

        //属性
        public string type
        { get; set; }

        public double x
        { get; set; }

        public double y
        { get; set; }
    }


    //定义点几何的类
    [Serializable]
    public class Geometry_PointXYZ:Geometry_PointXY
    {
        //属性
        public double z
        { get; set; }

        public Geometry_PointXYZ(double x, double y,double z):base(x,y)
        {
            this.z = z;
        }

        public Geometry_PointXYZ()
        {
            this.z = 0;
        }
    }

    //用于定义面要素类中的要素的类
    [Serializable]
    public class ArcGisFeature_Ploygon
    {
        //构造函数
        public ArcGisFeature_Ploygon()
        {
            this.attributes = new Dictionary<string, object>();
            this.geometry = new ArcGisGeometry_Ploygon();
        }

        //构造函数
        public ArcGisFeature_Ploygon(Dictionary<string, object> attributes_value, ArcGisGeometry_Ploygon geometry_value)
        {
            this.attributes = attributes_value;
            this.geometry = geometry_value;
        }
        
        //属性
        public Dictionary<string, object> attributes
        { get; set; }

        public ArcGisGeometry_Ploygon geometry
        { get; set; }

        //根据面要素上的点创建面要素集合
        public static List<ArcGisFeature_Ploygon> Get_Featurelist(List<List<PointXYZ>> ploygon_list, Dictionary<string, string> field_aliases)
        {
            List<ArcGisFeature_Ploygon> feature_list = new List<ArcGisFeature_Ploygon>();

            //遍历湿网格元素
            for (int i = 0; i < ploygon_list.Count; i++)
            {
                //定义要素属性
                Dictionary<string, object> attributes = new Dictionary<string, object>();
                attributes.Add(field_aliases.ElementAt(0).Key, i);

                //定义要素几何( 区域(一般就1个) --> 点的集合 --> 点的x,y,z集合 )
                List<List<List<double>>> rings = new List<List<List<double>>>();
                List<List<double>> part1 = new List<List<double>>();

                for (int j = 0; j < ploygon_list[i].Count; j++)
                {
                    //如果该节点已经赋值，则直接采用该值
                    PointXYZ now_p = PointXYZ.Get_PointXYZ(ploygon_list[i][j].X, ploygon_list[i][j].Y, ploygon_list[i][j].Z);

                    List<double> point = new List<double>() { now_p.X, now_p.Y, now_p.Z };
                    part1.Add(point);
                }
                rings.Add(part1);
                ArcGisGeometry_Ploygon geometry = new ArcGisGeometry_Ploygon(true, rings);

                //定义单个要素,并加入要素集合
                ArcGisFeature_Ploygon feature_ploygon = new ArcGisFeature_Ploygon(attributes, geometry);
                feature_list.Add(feature_ploygon);
            }

            return feature_list;
        }

    }

    //用于定义点要素类中的要素的类
    [Serializable]
    public class ArcGisFeature_Point
    {
        //构造函数
        public ArcGisFeature_Point()
        {
            this.attributes = new Dictionary<string, object>();
            this.geometry = new Geometry_PointXY();
        }

        //构造函数
        public ArcGisFeature_Point(Dictionary<string, object> attributes_value, Geometry_PointXY geometry_value)
        {
            this.attributes = attributes_value;
            this.geometry = geometry_value;
        }

        //属性
        public Dictionary<string, object> attributes
        { get; set; }

        public Geometry_PointXY geometry
        { get; set; }
    }

    //用于定义坐标参考空间的类
    [Serializable]
    public class Spatial_Reference
    {
        //构造函数
        public Spatial_Reference()
        {
            this.wkid = 4326;
            this.latestWkid = 4326;
        }

        //构造函数
        public Spatial_Reference(int wkid_value = 4548, int latestWkid_value = 4548)
        {
            this.wkid = wkid_value;
            this.latestWkid = latestWkid_value;
        }

        //属性
        public int wkid
        { get; set; }

        public int latestWkid
        { get; set; }
    }

    //字段定义的类
    [Serializable]
    public class Field
    {
        public Field()
        {
            this.name = "";
            this.type = "";
            this.alias = "";
        }

        public Field(string name_value,string type_value,string alias_value)
        {
            this.name = name_value;
            this.type = type_value;
            this.alias = alias_value;
        }

        //属性
        public string name
        { get; set; }

        public string type
        { get; set; }

        public string alias
        { get; set; }
    }

    //生成GIS面要素JSON的类
    [Serializable]
    public class ArcGis_Polygon
    {
        //构造函数
        public ArcGis_Polygon()
        {
            this.displayFieldName = "";
            this.hasZ = false;
            this.fieldAliases = new Dictionary<string, string>();
            this.geometryType = "";
            this.spatialReference = new Spatial_Reference(4326);
            this.fields = new List<Field>();
            this.features = new List<ArcGisFeature_Ploygon>();
        }

        //构造函数
        public ArcGis_Polygon(string fieldname, bool hasz , Dictionary<string,string> field_aliases, string geometry_type,
            Spatial_Reference spatial_ref , List<Field> field_list, List<ArcGisFeature_Ploygon> feature_list)
        {
            this.displayFieldName = fieldname;
            this.hasZ = hasz;
            this.fieldAliases = field_aliases;
            this.geometryType = geometry_type;
            this.spatialReference = spatial_ref;
            this.fields = field_list;
            this.features = feature_list;
        }

        //属性
        //要素显示名称
        public string displayFieldName
        { get; set; }

        //是否启用Z
        public bool hasZ
        { get; set; }

        //字段轴即名称定义(键-字段名称 值- 字段名称)
        public Dictionary<string, string> fieldAliases
        { get; set; }

        //几何类型
        public string geometryType
        { get; set; }

        //空间参考坐标系
        public Spatial_Reference spatialReference
        { get;set;}

        //字段类型
        public List<Field> fields
        { get; set; }

        //要素另一个名字
        public List<ArcGisFeature_Ploygon> features
        { get; set; }

        //方法
        //面要素geojson对象转arcgisjson对象(均为WGS84坐标)
        public static ArcGis_Polygon Change_PolygonGeoJson_ToArcGisJson(GeoJson_Polygon geojson,int wkid = 4547)
        {
            //定义要素名、是否含Z，以及要素坐标轴
            string fieldname = "";
            bool hasz = geojson.hasZ;

            //定义几何类型和坐标系
            string geometry_type = "esriGeometryPolygon";
            Spatial_Reference spatial_ref = new Spatial_Reference(wkid, wkid); //WGS84

            //定义字段
            List<Field> field_list = new List<Field>();
            Dictionary<string, object> propers = geojson.features.First().properties;
            for (int i = 0; i < propers.Count; i++)
            {
                string field_name = propers.ElementAt(i).Key;
                object field_value = propers.ElementAt(i).Value;
                string field_type;
                if (field_value is float)
                {
                    field_type = "esriFieldTypeSingle";
                }
                else if(field_value is double)
                {
                    field_type = "esriFieldTypeDouble";
                }
                else if (field_value is int)
                {
                    field_type = "esriFieldTypeInteger";
                }
                else
                {
                    field_type = "esriFieldTypeString";
                }

                if (field_name != "OBJECTID" && field_name != "FID" && field_name != "Id")
                {
                    Field other_field = new Field(field_name, field_type, field_name);
                    field_list.Add(other_field);
                }
            }

            //定义字段轴
            Dictionary<string, string> field_aliases = new Dictionary<string, string>();
            for (int i = 0; i < field_list.Count; i++)
            {
                field_aliases.Add(field_list[i].name, field_list[i].name);
            }

            //定义要素对象集合
            List<ArcGisFeature_Ploygon> feature_list = new List<ArcGisFeature_Ploygon>();
            List<Feature_Ploygon> features = geojson.features;
            for (int i = 0; i < features.Count; i++)
            {
                ArcGisFeature_Ploygon feature_ploygon = new ArcGisFeature_Ploygon();

                //定义要素属性
                feature_ploygon.attributes = features[i].properties;
                ArcGisGeometry_Ploygon geometry = new ArcGisGeometry_Ploygon();
                geometry.hasZ = hasz;
                geometry.rings = features[i].geometry.coordinates;
                feature_ploygon.geometry = geometry;

                feature_list.Add(feature_ploygon);
            }

            //定义最终的水面 面要素几何对象
            ArcGis_Polygon arcgis_polygon = new ArcGis_Polygon(fieldname, hasz, field_aliases, geometry_type, spatial_ref, field_list, feature_list);

            return arcgis_polygon;
        }

    }

    //定义一个面的子类，满足前端要求
    [Serializable]
    public class ArcGis_Polygon1
    {
        //构造函数
        public ArcGis_Polygon1()
        {
            this.displayFieldName = "";
            this.hasZ = false;
            this.objectIdField = "FID";
            this.fieldAliases = new Dictionary<string, string>();
            this.geometryType = "";
            this.spatialReference = new Spatial_Reference(4548);
            this.fields = new List<Field>();
            this.source = new List<ArcGisFeature_Ploygon>();
        }

        //构造函数
        public ArcGis_Polygon1(ArcGis_Polygon gis_polygon)
        {
            this.displayFieldName = gis_polygon.displayFieldName;
            this.hasZ = gis_polygon.hasZ;
            this.objectIdField = "FID";
            this.fieldAliases = gis_polygon.fieldAliases;
            this.geometryType = gis_polygon.geometryType;
            this.spatialReference = gis_polygon.spatialReference;
            this.fields = gis_polygon.fields;
            this.source = gis_polygon.features;
        }

        //构造函数
        public ArcGis_Polygon1(string fieldname, bool hasz, Dictionary<string, string> field_aliases, string geometry_type,
            Spatial_Reference spatial_ref, List<Field> field_list, List<ArcGisFeature_Ploygon> feature_list)
        {
            this.displayFieldName = fieldname;
            this.hasZ = hasz;
            this.fieldAliases = field_aliases;
            this.geometryType = geometry_type;
            this.spatialReference = spatial_ref;
            this.fields = field_list;
            this.source = feature_list;
        }

        //属性
        //要素显示名称
        public string displayFieldName
        { get; set; }

        //是否启用Z
        public bool hasZ
        { get; set; }

        //新增对象ID字段注释
        public string objectIdField
        { get; set; }

        //字段轴即名称定义(键-字段名称 值- 字段名称)
        public Dictionary<string, string> fieldAliases
        { get; set; }

        //几何类型
        public string geometryType
        { get; set; }

        //空间参考坐标系
        public Spatial_Reference spatialReference
        { get; set; }

        //字段类型
        public List<Field> fields
        { get; set; }

        //要素另一个名字
        public List<ArcGisFeature_Ploygon> source
        { get; set; }

    }

    //生成GIS点要素JSON的类
    [Serializable]
    public class ArcGis_Point
    {
        //构造函数
        public ArcGis_Point()
        {
            this.displayFieldName = "";
            this.hasZ = false;
            this.fieldAliases = new Dictionary<string, string>();
            this.geometryType = "";
            this.spatialReference = new Spatial_Reference();
            this.fields = new List<Field>();
            this.features = new List<ArcGisFeature_Point>();
        }

        //构造函数
        public ArcGis_Point(string fieldname , bool hasz , Dictionary<string, string> field_aliases, string geometry_type ,
            Spatial_Reference spatial_ref , List<Field> field_list, List<ArcGisFeature_Point> feature_list)
        {
            this.displayFieldName = fieldname;
            this.hasZ = hasz;
            this.fieldAliases = field_aliases;
            this.geometryType = geometry_type;
            this.spatialReference = spatial_ref;
            this.fields = field_list;
            this.features = feature_list;
        }

        //属性
        //要素显示名称
        public string displayFieldName
        { get; set; }

        //是否启用Z
        public bool hasZ
        { get; set; }

        //字段轴即名称定义(键-字段名称 值- 字段名称)
        public Dictionary<string, string> fieldAliases
        { get; set; }

        //几何类型
        public string geometryType
        { get; set; }

        //空间参考坐标系
        public Spatial_Reference spatialReference
        { get; set; }

        //字段类型
        public List<Field> fields
        { get; set; }

        //要素
        public List<ArcGisFeature_Point> features
        { get; set; }
    }

    //定义一个点的子类，满足前端要求
    [Serializable]
    public class ArcGis_Point1 
    {
        //构造函数
        public ArcGis_Point1()
        {
            ArcGis_Point gis_point = new ArcGis_Point();
            this.displayFieldName = gis_point.displayFieldName;
            this.hasZ = gis_point.hasZ;
            this.objectIdField = "FID";
            this.fieldAliases = gis_point.fieldAliases;
            this.geometryType = gis_point.geometryType;
            this.spatialReference = gis_point.spatialReference;
            this.fields = gis_point.fields;
            this.source = gis_point.features;
        }

        //构造函数
        public ArcGis_Point1(ArcGis_Point gis_point)
        {
            this.displayFieldName = gis_point.displayFieldName;
            this.hasZ = gis_point.hasZ;
            this.objectIdField = "FID";
            this.fieldAliases = gis_point.fieldAliases;
            this.geometryType = gis_point.geometryType;
            this.spatialReference = gis_point.spatialReference;
            this.fields = gis_point.fields;
            this.source = gis_point.features;
        }

        //属性
        //要素显示名称
        public string displayFieldName
        { get; set; }

        //是否启用Z
        public bool hasZ
        { get; set; }

        //新增对象ID字段注释
        public string objectIdField
        { get; set; }

        //字段轴即名称定义(键-字段名称 值- 字段名称)
        public Dictionary<string, string> fieldAliases
        { get; set; }

        //几何类型
        public string geometryType
        { get; set; }

        //空间参考坐标系
        public Spatial_Reference spatialReference
        { get; set; }

        //字段类型
        public List<Field> fields
        { get; set; }

        //要素
        public List<ArcGisFeature_Point> source
        { get; set; }
    }
}