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
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
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
        public static string Load_Reach_GisPolygon_Res(string plan_code,string model_instance, string time_str)
        {
            string res_json = null;

            //定义连接对象
            string tableName = Mysql_GlobalVar.MIKE11_GISPOLYGONRES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select reach_gispolygon from " + tableName + " where model_instance ='" 
                + model_instance + "' and plan_code = '" + plan_code + "'" + " and time = '" + time_str + "'";

            DataTable dt = Mysql.query(sql);
            if (dt == null) return null;
            if (dt.Rows.Count == 0) return null;

            string res_file = dt.Rows[0][0].ToString();
            

            if (File.Exists(res_file))
            {
                //读取json文件
                res_json = File_Common.Read_FileContent(res_file);
            }

            return res_json;
        }

        //获取模型MIke11 样板线或面 -- 从数据库中获取(静态类)
        public static string Load_Sample_GisRes(string line_polygon,string model_instance)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_SAMPLEGIS_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select "+ line_polygon + " from " + tableName + " where model_instance = '" + model_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到样例GIS数据!");
                return null;
            }

            //样例GIS要素(已转为经纬度)
            string sample_gis_geojson = dt.Rows[0][0].ToString();

            
            return sample_gis_geojson;
        }

        //获取模型MIke11 GIS过程结果 -- 从数据库中获取(静态类)(用模型名)
        public static string Load_Res11Gis_LineData_Attrs(string plan_code,string data_restype)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISLineRES_TABLENAME;
            

            string field_name = data_restype.ToLower();

            //先判断该模型在数据库中是否存在
            string sqlstr = "select " + field_name + " from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", plan_code);
                return null;
            }

            string res = dt.Rows[0][0].ToString();

            
            return res;
        }

        //获取模型MIke11 GIS统计结果 -- 从数据库中获取(静态类)(用模型名)
        public static string Load_Res11Gis(string plan_code)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISLineRES_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select hdline_tj_result from " + tableName + " where plan_code='" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", plan_code);
                return null;
            }

            //河道中小线样例GIS要素(已转为经纬度)
            string tjgis_geojson = dt.Rows[0][0].ToString();

            

            return tjgis_geojson;
        }

        //获取当前时刻下洪水三维面结果
        private static GeoJson_Polygon Get_NowTime_FloodpolygonRes(Dictionary<string, Dictionary<float, float>> reach_zdres, 
            GeoJson_Polygon hd_sample_gisploygon, List<Dictionary<int, AtReach>> polygonpoint_sectionrelation,
            Dictionary<int, string> tx_polygonlist,Dictionary<string, string> sub_xzhq_res, 
            Dictionary<string, List<float>> zdm_chainage, Dictionary<string, List<float>> zdm_td)
        {
            //修改样例GIS要素，重新得到该时刻下的水面GIS几何对象
            List<Feature_Ploygon> new_feature_list = new List<Feature_Ploygon>();
            List<Feature_Ploygon> feature_list = hd_sample_gisploygon.features;
            for (int i = 0; i < feature_list.Count; i++)
            {
                //不显示的要素直接跳过
                bool feature_show = Polygon_IsShow(tx_polygonlist, sub_xzhq_res, polygonpoint_sectionrelation[i],
                    reach_zdres, zdm_chainage, zdm_td, i);
                if (!feature_show) continue;

                //每段河道折点
                Feature_Ploygon feature = feature_list[i];
                List<List<double>> point_list = feature.geometry.coordinates[0];

                //每段河道和断面对应关系
                Dictionary<int, AtReach> point_atreach = polygonpoint_sectionrelation[i];

                //修改每段河道的折点高程
                Modify_PolygonZ(ref point_list, point_atreach, reach_zdres);

                //如果还有内环，修改内环高程
                if (feature.geometry.coordinates.Count == 2)
                {
                    List<List<double>> point_list1 = feature.geometry.coordinates[1];
                    Modify_NeiPolygonZ(ref point_list1, point_list);
                }

                new_feature_list.Add(feature);
            }

            GeoJson_Polygon hd_gisploygon = new GeoJson_Polygon(true, hd_sample_gisploygon.crs, new_feature_list);

            return hd_gisploygon;
        }

        //获取最高水位的 洪水三维面结果
        private static GeoJson_Polygon Get_MaxLevel_FloodPolygonRes(Dictionary<string, Dictionary<float, float>> reach_zdres,
            GeoJson_Polygon hd_sample_gisploygon, List<Dictionary<int, AtReach>> polygonpoint_sectionrelation,
            Dictionary<int, string> tx_polygonlist, Dictionary<string, string> sub_xzhq_res,
            Dictionary<string, List<float>> zdm_chainage, Dictionary<string, List<float>> zdm_td)
        {
            //修改样例GIS要素，重新得到该时刻下的水面GIS几何对象
            List<Feature_Ploygon> new_feature_list = new List<Feature_Ploygon>();
            List<Feature_Ploygon> feature_list = hd_sample_gisploygon.features;
            for (int i = 0; i < feature_list.Count; i++)
            {
                if (!tx_polygonlist.Keys.Contains(i)) continue;

                //不显示的要素也直接跳过
                bool feature_show = Polygon_IsShow(tx_polygonlist, sub_xzhq_res, polygonpoint_sectionrelation[i],
                    reach_zdres, zdm_chainage, zdm_td, i);
                if (!feature_show) continue;

                //每段折点
                Feature_Ploygon feature = feature_list[i];
                List<List<double>> point_list = feature.geometry.coordinates[0];

                //每段河道和断面对应关系
                Dictionary<int, AtReach> point_atreach = polygonpoint_sectionrelation[i];

                //修改每段河道的折点高程
                Modify_PolygonZ(ref point_list, point_atreach, reach_zdres);

                //如果还有内环，修改内环高程
                if (feature.geometry.coordinates.Count == 2)
                {
                    List<List<double>> point_list1 = feature.geometry.coordinates[1];
                    Modify_NeiPolygonZ(ref point_list1, point_list);
                }

                new_feature_list.Add(feature);
            }

            GeoJson_Polygon hd_gisploygon = new GeoJson_Polygon(true, hd_sample_gisploygon.crs, new_feature_list);

            return hd_gisploygon;
        }

        //需要判断显示的部分特殊面要素
        private static bool Polygon_IsShow(Dictionary<int, string> tx_polygonlist, Dictionary<string, string> sub_xzhq_res,
            Dictionary<int, AtReach> point_atreach, Dictionary<string, Dictionary<float, float>> zdm_level,
            Dictionary<string, List<float>> zdm_chainage, Dictionary<string, List<float>> zdm_td, int fid)
        {
            bool show = true;
            if (!tx_polygonlist.Keys.Contains(fid))
            {
                //不在里面的都显示
                show = true;
            }
            else if(tx_polygonlist[fid] != "")
            {
                //如果对应蓄滞洪区，则采用蓄滞洪区启用判断
                string xzhq_stcd = tx_polygonlist[fid];
                if (sub_xzhq_res.Keys.Contains(xzhq_stcd))
                {
                    if(sub_xzhq_res[xzhq_stcd] == "未启用") show = false;
                }
            }
            else
            {
                //否则采用河道漫滩判断
                show = Reach_Mt_PolygonShow(point_atreach, zdm_level, zdm_chainage, zdm_td);
            }

            return show;
        }

        //根据是否漫滩确定是否显示该面要素
        public static bool Reach_Mt_PolygonShow(Dictionary<int, AtReach> point_atreach,
            Dictionary<string, Dictionary<float, float>> zdm_level, Dictionary<string, List<float>> zdm_chainage,
            Dictionary<string, List<float>> zdm_td)
        {
            bool show = false;

            //河道断面集合
            List<AtReach> section_list = new List<AtReach>();
            for (int i = 0; i < point_atreach.Count; i++)
            {
                AtReach atreach = point_atreach.ElementAt(i).Value;
                if (!section_list.Contains(atreach)) section_list.Add(atreach);
            }

            //如果有1个断面漫滩，则认为该断漫滩
            for (int i = 0; i < section_list.Count; i++)
            {
                string reach_name = section_list[i].reachname;
                float chainage = (float)section_list[i].chainage;
                double level = 0;
                if (zdm_level[reach_name].Keys.Contains(chainage))
                {
                    level = zdm_level[reach_name][chainage];
                }
                else
                {
                    //内插该值
                    level = File_Common.Insert_Zd_Value(zdm_level[reach_name], chainage);
                }

                List<float> reach_chainages = zdm_chainage[reach_name];
                int number = reach_chainages.IndexOf(chainage);
                if (number == -1) continue;
                float td = zdm_td[reach_name][number];
                if(level > td)
                {
                    show = true;
                    break;
                }
            }

            return show;
        }


        //修改内环折点高程
        public static void Modify_NeiPolygonZ(ref List<List<double>> point_list, List<List<double>> wai_pointlist)
        {
            Dictionary<int, int> near_p = new Dictionary<int, int>();
            for (int i = 0; i < point_list.Count; i++)
            {
                //找到最近的一个中心点,用该点高程作为多边形折点高程
                double min_distance = 100000; int min_index = -1;
                for (int j = 0; j < wai_pointlist.Count; j++)
                {
                    double now_distance = PointXY.Get_ptop_distance(wai_pointlist[j][0], wai_pointlist[j][1], point_list[i][0], point_list[i][1]);
                    if (now_distance < min_distance)
                    {
                        min_distance = now_distance;
                        min_index = j;
                    }
                }

                point_list[i][2] = wai_pointlist[min_index][2];
            }
        }

        //修改每段河道的折点高程
        public static void Modify_PolygonZ(ref List<List<double>> point_list, Dictionary<int, AtReach> point_atreach,
            Dictionary<string, Dictionary<float, float>> reach_zdres)
        {
            Dictionary<PointXY, double> res_dic = new Dictionary<PointXY, double>();

            //多边形折点遍历
            for (int i = 0; i < point_list.Count; i++)
            {
                //该折点对应的断面
                AtReach atreach = point_atreach[i];

                //该折点对应的水位
                Dictionary<float, float> reach_zd = reach_zdres[atreach.reachname];
                float chainage = (float)atreach.chainage;
                double level = 0;
                if (reach_zd.Keys.Contains(chainage))
                {
                    level = reach_zd[chainage];
                }
                else
                {
                    //内插该值
                    level = File_Common.Insert_Zd_Value(reach_zd, chainage);
                }
                point_list[i][2] = Math.Round(level, 2);
            }
        }

        //从数据库获取样例多边形和河道断面的对应关系
        public static List<Dictionary<int, AtReach>> Get_SamplePolygonPoint_SectionRelation(string model_instance)
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISPOLYGONP_SECTION;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = "select polygon_pointsection from " + tableName + " where model_instance = '" + model_instance + "'"; 
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //获取模型结果数据记录
            List<string> field_name;
            List<byte[]> res_data;
            Get_Res11Data(dt, out field_name, out res_data);

            //多边形和河道断面的对应关系结果
            List<Dictionary<int, AtReach>> res = Relation_Deserialize(res_data[0]);

            

            return res;
        }

        //对样例多边形进行处理(保留6位数)、建立多边形折点和断面对应关系、生成样例线
        public static void Gis_SamplePolygonLine_Process()
        {
            //修改数据库中Default模型GIS结果里样例GIS多边形(坐标保留6位数)
            //GeoJson_Polygon.Load_DmswSampleRes_AndWriteToBD();

            //建立样例多边形和河道断面的对应关系，并写入数据库
            Get_SamplePolygonPoint_SectionRelation_WriteToDB();

            //生成样例线，保存进Default模型GIS结果里
            Create_SampleLine_WritetoDb();
        }

        //建立样例多边形和河道断面的对应关系，并写入数据库
        public static void Get_SamplePolygonPoint_SectionRelation_WriteToDB()
        {
            //获取一个已经完成计算的模型名称
            string plan_code = "model_20250513170039";// Get_Finish_Plancode();
            if (plan_code == "") return;

            //加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);

            //获取Mike11各分段水面多边形包含的河道断面集合
            Dictionary<int, Dictionary<AtReach, PointXY>> reachsegpolygon_containsections = Get_SegReachPolygon_ContainSections(hydromodel, plan_code);

            //从数据库获取水位面样例GIS要素(已转为经纬度)
            string field_name = "polygon_sample_result";
            string sample_polygonjsonstr = Res11.Load_Sample_GisRes(field_name, Mysql_GlobalVar.now_instance);
            GeoJson_Polygon hdsample_gisploygon = JsonConvert.DeserializeObject<GeoJson_Polygon>(sample_polygonjsonstr);

            //对样例GIS面要素的每个折点与横断面建立关系
            List <Dictionary<int, AtReach>> polygonpoint_sectionrelation = new List<Dictionary<int, AtReach>>(); //每个元素是一个多边形里折点与河道断面的对应关系
            List<Feature_Ploygon> feature_list = hdsample_gisploygon.features;
            for (int i = 0; i < feature_list.Count; i++)
            {
                Feature_Ploygon feature = feature_list[i];

                //每段河道(只考虑外环)
                List<List<double>> point_list = feature.geometry.coordinates[0];

                //获取每段河道内的断面桩号及中心点
                Dictionary<AtReach, PointXY> sections = reachsegpolygon_containsections[i];

                //找到距离每个多边形折点对应的最近断面
                Dictionary<int, AtReach> pointnear_section = Get_PolygonPoint_NearSection(point_list, sections);

                polygonpoint_sectionrelation.Add(pointnear_section);
            }

            //序列化后存入数据库
            byte[] res_relation = File_Common.Serializer_ObjByte(polygonpoint_sectionrelation);
            Write_Mike11Samplepolygon_PointSectionRealtion_IntoDB(res_relation);
        }

        //生成样例线，保存进Default模型GIS结果里
        public static void Create_SampleLine_WritetoDb()
        {
            //获取一个已经完成计算的模型名称
            string plan_code = "model_20250513170039";// Get_Finish_Plancode();
            if (plan_code == "") return;

            //加载模型
            HydroModel hydromodel = HydroModel.Load(plan_code);

            //从已经计算完成的模型中 -- 获取所有河道断面桩号和水位
            Dictionary<string, Dictionary<float, float>> reach_zdres = Load_WaterRes11(plan_code, hydromodel.ModelGlobalPars.Simulate_time.End);

            //转一下格式
            Dictionary<string, Dictionary<double, double>> reach_zdres1 = new Dictionary<string, Dictionary<double, double>>();
            for (int i = 0; i < reach_zdres.Count; i++)
            {
                string reach_name = reach_zdres.ElementAt(i).Key;
                Dictionary<float, float> zd = reach_zdres.ElementAt(i).Value;
                Dictionary<double, double> zd1 = new Dictionary<double, double>();
                for (int j = 0; j < zd.Count; j++)
                {
                    zd1.Add(zd.ElementAt(j).Key, zd.ElementAt(j).Value);
                }
                reach_zdres1.Add(reach_name, zd1);
            }

            //生成模板带多属性三维线
            GeoJson_Line geojson_sampleline = Get_ReachLine_GisRes(hydromodel, reach_zdres1);

            //更新数据库
            string tableName = Mysql_GlobalVar.MIKE11_SAMPLEGIS_TABLENAME;

            string res_geojson = File_Common.Serializer_Obj(geojson_sampleline);
            string sql = "update " + tableName + " set line_sample_result = '" + res_geojson + "' where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            Mysql.Execute_Command(sql);


            Console.WriteLine("三维样板线已生成并保存进数据库!");
        }

        //去除一些特殊断面，防止水面过高
        public static void Remove_TSSections(ref Dictionary<string, Dictionary<float, PointXY>> reach_section)
        {
            //贾鲁河流域一些特殊断面
            Dictionary<string, List<double>> jlh_tssection = Item_Info.Get_JLH_TSSection();

            //去除这些断面
            for (int i = 0; i < reach_section.Count; i++)
            {
                string reach_name = reach_section.ElementAt(i).Key;

                Dictionary<float, PointXY> reach_centerp = reach_section.ElementAt(i).Value;
                for (int j = 0; j < reach_centerp.Count; j++)
                {
                    if (jlh_tssection.Keys.Contains(reach_name))
                    {
                        double chainage = reach_centerp.ElementAt(j).Key;

                        if (jlh_tssection[reach_name].Contains(chainage) ||
                            jlh_tssection[reach_name].Contains(Math.Round(chainage, 0)) ||
                            jlh_tssection[reach_name].Contains(Math.Round(chainage, 1)))
                        {
                            reach_centerp.Remove((float)chainage);
                            j--;
                        }
                    }
                }
            }
        }

        //将序列化后多边形折点和断面的对应关系存入数据库
        public static void Write_Mike11Samplepolygon_PointSectionRealtion_IntoDB(byte[] res_relation)
        {
            if (res_relation.Length == 0) return;

            string username = Mysql_GlobalVar.now_modeldb_user;
            string password = Mysql_GlobalVar.now_modeldb_password;

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MIKE11_GISPOLYGONP_SECTION;
            

            //先判断和删除旧的模型结果
            string select_sql = "select * from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'"; 
            DataTable dt = Mysql.query(select_sql);
            if(dt != null)
            {
                int nLen = dt.Rows.Count;
                if (nLen != 0)
                {
                    string del_sql = "delete from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'"; 
                    Mysql.Execute_Command(del_sql);
                }
            }
            
            string sql = "insert into " + tableName + " (model_instance,polygon_pointsection) values(:model_instance,:polygon_pointsection)";
            KdbndpParameter[] mysqlPara = { new KdbndpParameter(":model_instance", Mysql_GlobalVar.now_instance),
                new KdbndpParameter(":polygon_pointsection", res_relation)
            };

            Mysql.Execute_Command(sql, mysqlPara);

            Console.WriteLine("河道样例多边形 折点与河道断面的对应关系存储完成！");
        }

        //找到距离每个多边形折点最近的断面，建立一对一关系
        public static Dictionary<int, AtReach> Get_PolygonPoint_NearSection(List<List<double>> polygon, Dictionary<AtReach, PointXY> section_points)
        {
            Dictionary<int, AtReach> near_section = new Dictionary<int, AtReach>();

            for (int i = 0; i < polygon.Count; i++)
            {
                //找到最近的一个中心点,用该点高程作为多边形折点高程
                double min_distance = 100000; int min_index = -1;
                for (int j = 0; j < section_points.Count; j++)
                {
                    double now_distance = PointXY.Get_ptop_distance(section_points.ElementAt(j).Value.X, section_points.ElementAt(j).Value.Y, polygon[i][0], polygon[i][1]);
                    if (now_distance < min_distance)
                    {
                        min_distance = now_distance;
                        min_index = j;
                    }
                }

                near_section.Add(i, section_points.ElementAt(min_index).Key);
            }

            return near_section;
        }

        //获取一个已经完成计算的模型名称
        private static string Get_Finish_Plancode()
        {
            //获取一个已经完成计算的模型名称
            string modelname = "";
            Dictionary<string, Dictionary<string, string>> model_infos = HydroModel.Get_AllModel_Info();
            for (int i = 0; i < model_infos.Count; i++)
            {
                string plan_code = model_infos.ElementAt(i).Key;
                Dictionary<string, string> model_info = model_infos.ElementAt(i).Value;
                for (int j = 0; j < model_info.Count; j++)
                {
                    if (model_info.ElementAt(j).Value == "已完成")
                    {
                        modelname = plan_code;
                        break;
                    }
                }
            }

            return modelname;
        }


        //获取Mike11各分段水面多边形包含的河道断面集合(根据有结果的模型名)
        public static Dictionary<int, Dictionary<AtReach, PointXY>> Get_SegReachPolygon_ContainSections(HydroModel hydromodel, string plan_code)
        {
            Dictionary<int, Dictionary<AtReach, PointXY>> reachsegpolygon_containsections = new Dictionary<int, Dictionary<AtReach, PointXY>>();

            //加载模型
            List<Reach_Segment> reachinfo = hydromodel.Mike11Pars.ReachList.Reach_baseinfolist;

            //从已经计算完成的模型中 -- 获取所有河道断面桩号和水位
            Dictionary<string, Dictionary<float, float>> reach_zdres = Load_WaterRes11(plan_code, hydromodel.ModelGlobalPars.Simulate_time.End);

            //计算河道所有H断面的中心线交叉点坐标
            Dictionary<string, Dictionary<float, PointXY>> reach_sectioncenter_points = new Dictionary<string, Dictionary<float, PointXY>>();
            for (int i = 0; i < reach_zdres.Count; i++)
            {
                string reach_name = reach_zdres.ElementAt(i).Key;
                Dictionary<float, float> section_level = reach_zdres.ElementAt(i).Value;

                Dictionary<float, PointXY> section_p = new Dictionary<float, PointXY>();
                for (int j = 0; j < section_level.Count; j++)
                {
                    float chainage = section_level.ElementAt(j).Key;
                    PointXY section_centerp = hydromodel.Mike11Pars.ReachList.Get_ReachPointxy(reach_name, double.Parse(chainage.ToString()));
                    section_p.Add(chainage, section_centerp);
                }
                reach_sectioncenter_points.Add(reach_name, section_p);
            }

            //去除一些特殊断面，防止水面过高
            Remove_TSSections(ref reach_sectioncenter_points);

            //转换一下格式,合并断面
            
            Dictionary<AtReach, PointXY> reach_cerp = new Dictionary<AtReach, PointXY>();
            for (int i = 0; i < reach_sectioncenter_points.Count; i++)
            {
                string reach_name = reach_sectioncenter_points.ElementAt(i).Key;
                Dictionary<float, PointXY> section_cerp = reach_sectioncenter_points.ElementAt(i).Value;
                for (int j = 0; j < section_cerp.Count; j++)
                {
                    AtReach atreach = AtReach.Get_Atreach(reach_name, section_cerp.ElementAt(j).Key);

                    //点转为经纬度坐标
                    PointXY pro_p = section_cerp.ElementAt(j).Value;
                    PointXY jwd_p = PointXY.CoordTranfrom(pro_p, 4547,4326,6);
                    reach_cerp.Add(atreach, jwd_p);
                }
            }

            //获取默认模型GIS河道三维面样例Geojson结果
            string field_name = "polygon_sample_result";
            string sample_polygonjsonstr = Res11.Load_Sample_GisRes(field_name, Mysql_GlobalVar.now_instance);
            GeoJson_Polygon sample_gisploygon = JsonConvert.DeserializeObject<GeoJson_Polygon>(sample_polygonjsonstr);

            //计算每段河道多边形里包括的河道断面
            List<Feature_Ploygon> feature_list = sample_gisploygon.features;
            for (int i = 0; i < feature_list.Count; i++)
            {
                //每段河道的边界坐标集合
                Feature_Ploygon feature = feature_list[i];
                List<List<double>> point_list = feature.geometry.coordinates[0];

                //遍历所有河道断面，看中心点是否在多边形内部
                Dictionary<AtReach, PointXY> contain_in = new Dictionary<AtReach, PointXY>();
                for (int j = 0; j < reach_cerp.Count; j++)
                {
                    if (PointXY.IsInPolygon(reach_cerp.ElementAt(j).Value, point_list))
                    {
                        contain_in.Add(reach_cerp.ElementAt(j).Key, reach_cerp.ElementAt(j).Value);
                    }
                }

                reachsegpolygon_containsections.Add(i, contain_in);
            }

            return reachsegpolygon_containsections;
        }

        //获取当前时刻下洪水三维线要素结果
        private static GeoJson_Line Modify_NowTime_FloodLineRes(string plan_code, DateTime time, GeoJson_Line hd_sample_gisline)
        {
            //time = new DateTime(2021, 7, 22, 0, 0, 0);

            //按时间获取模型MIke11特定结果 -- 某时刻的水位、流量、流速、渠底
            List<Dictionary<string, List<float>>> reach_zdres = Load_Res11_Part2(plan_code, time);

            //将各河道的断面属性合并
            List<float> level = Combine_ReachSection_ZdPars(reach_zdres[0]);
            List<float> discharge = Combine_ReachSection_ZdPars(reach_zdres[1]);
            List<float> speed = Combine_ReachSection_ZdPars(reach_zdres[2]);
            List<float> dd = Combine_ReachSection_ZdPars(reach_zdres[3]);
            List<float> qd = Combine_ReachSection_ZdPars(reach_zdres[4]);
            List<float> waterh = Get_WaterH_List(level, qd);

            //修改样例GIS要素，重新得到该时刻下的水面GIS几何对象
            List<Feature_Line> feature_list = hd_sample_gisline.features;
            List<Feature_Line> new_feature_list = new List<Feature_Line>();
            for (int i = 0; i < feature_list.Count; i++)
            {
                Feature_Line feature = feature_list[i];

                //修改要素属性
                Dictionary<string, object> feature_atts = feature.properties;
                double level_h = Math.Min(Math.Max(dd[i] - level[i], 0), 5);
                feature_atts["Waterlevel"] = Math.Round(level_h, 2);
                feature_atts["Speed"] = Math.Round(Math.Min(Math.Max(speed[i], 0), 5), 2);
                feature_atts["Waterh"] = Math.Round(Math.Max(waterh[i], 0), 2);
                feature_atts["Discharge"] = Math.Round(Math.Max(discharge[i], 0), 2);

                //修改控制点高程
                double now_level = (float)Math.Round(level[i], 3);
                List<List<double>> point_list = feature.geometry.coordinates;
                for (int j = 0; j < point_list.Count; j++)
                {
                    point_list[j][2] = Math.Round(now_level + 1.0, 2);  //水位+1m考虑
                }

                new_feature_list.Add(feature);
            }

            GeoJson_Line hd_gisline = new GeoJson_Line(true, hd_sample_gisline.crs, new_feature_list);

            return hd_gisline;
        }

        //将各河道的断面属性合并(3个断面2根线，所以每条河道都得减少一个才能和要素数量吻合)
        public static List<float> Combine_ReachSection_ZdPars(Dictionary<string, List<float>> reach_zdres)
        {
            List<float> reach_zd_data = new List<float>();
            for (int i = 0; i < reach_zdres.Count; i++)
            {
                string reach_name = reach_zdres.ElementAt(i).Key;
                if (reach_name == "WHGBLQ" || reach_name == "WHGFHY") continue;
                List<float> reach_zd = reach_zdres.ElementAt(i).Value;

                //3个断面2根线，所以每条河道都得减少一个才能和要素数量吻合
                for (int j = 0; j < reach_zd.Count -1; j++)
                {
                    reach_zd_data.Add(reach_zd[j]);
                }
            }

            return reach_zd_data;
        }

        //得到水深集合
        public static List<float> Get_WaterH_List(List<float> level, List<float> qd)
        {
            List<float> h_list = new List<float>();
            if (level.Count != qd.Count) return null;
            for (int i = 0; i < level.Count; i++)
            {
                h_list.Add(level[i] - qd[i]);
            }
            return h_list;
        }

    }
}
