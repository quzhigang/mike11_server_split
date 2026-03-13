using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using bjd_model.Mike11;

namespace bjd_model.Const_Global
{
    //整个项目中，用于存储常量的类
    public static class Model_Const
    {
        //**********河网文件中的常量**********//
        public const double MIKE11_MINDX = 10.0;       //河网文件中最小容许断面间距
        public const double MIKE11_DEFAULTDX = 500;    //河网断面间距---空间步长
        public const SaveTimeStep MIKE11_SAVESTEPTIME = SaveTimeStep.step_1;  //先默认mik11保存时间步，新建的模型会被改为该保存时间步(分钟)
        public static int Now_Model_SaveTime = 20;    //当前项目的保存时间(分钟)；
        public const int STATICS_TABLERES_INTERAL = 5;  //结果统计报表步数间隔

        public const double MIKE11_MINCONNECTDISTANCE = 500;  //最小连接距离，当新河道与最近河道的距离小于这一值时，河道将自动连接
        public const double MIKE11_MAXPTOREACHSTANCE = 1000;  //用点查询河道断面桩号的最大偏差距离

        public const float DEFAULT_NODESTORY_SPEED = 2.5f;   //默认不冲流速
        public const double REACH_DG_DISCHARGE = -2.0;   //小于该流量则认为倒灌

        public const double Q_MAXSPEED = 2.0;          //流量最大改变速度 m3/s
        public const double FLATGATE_MAXSPEED = 0.01;  //平板闸门最大开启速度 m/s
        public const double RADIALGATE_MAXSPEED = 0.006; //弧形闸门最大开启速度 m/s

        public const double JZZ_OCTIME = 30.0;  //节制闸开关时间(分钟)
        public const double TSZ_OCTIME = 20.0;  //退水闸开关时间
        public const double FSZ_OCTIME = 10.0; //分水闸开关时间

        public const double FHK_INSTANTANEOUSBREAK_TIME = 30;  //分洪口瞬溃时间秒second
        public const double Res_LevelIteration_Numbers = 10;  //库群优化调度水库水位划分等分(决定迭代精度，但过大使得计算耗费时间长)

        public const string REACHID_HZ = "_2030";       //河道ID后缀,河道ID统一由河名 + 河道ID后缀组成
        public static double starttimespan_hour;    //触发条件的时间间隔，如240小时，即10天内只触发一次，触发一次管10天
        public static double interveltimespan_hour;  //闸门操作间隔，如2小时，即2个小时只做一次闸门开关操作，以防止水位震荡
        public static bool Stop_Geting_Mike11Res = false;         //用于停止继续读取结果数据，包括GP、存内存
        public static Model_State Now_Model_State = Model_State.Ready_Calting;   //当前模型的状态
        public const double WEIR_M = 0.385;  //溢洪道默认流量系数
        public static double Modify_Front_Hours = 6.0;    //预报水位流量等曲线过程前段修正时长
        public const double RES_KXQ_SFXX = 1.0;   //水库反算中控泄流量增大倍数(受模型水位库容关系误差 和 入库流量误差影响)
        //************************************//


        //**********断面文件中的常量**********//
        public const double LRHIGHFLOW = 1.2;         //河滩相对主河槽的糙率倍数
        public const double DEFAULT_RESISTANCE = 0.035;  //默认河道糙率
        public const double SECTION_MAXWIDTH = 300;      //如果新河道没有与原有河道相连时的剖切断面宽度
        public const int SECTION_LEVELCOUNT = 40;        //断面预处理水位层数
        //************************************//

        //************ 边界常量 **************//
        public const double UPBOUND_DISCHARGE = 0.1;     //在无上边界时，默认的上边界入流流量 0.1m3/s
        public const double CONCENTRATION = 1000000;    //水质浓度常量 mg/l
        //************************************//

        //************产汇流常量**************//
        public const int EVP_BEFORE_DAYCOUNT = 90;  //截取的前期降雨、蒸发时间天数,新安江模型用
        public const double XAJ_TIMESTEP_HOUR = 0.2;  //XAJ模型的时间步长，包括结果步长和降雨过程步长,以小时计
        //************************************//

        //********Couple耦合文件中的常量******//
        public const double SIDESTR_MAXCONNECT_DISTANCE = 300; //侧向建筑物主要为分洪口位置与二维网格外边界的距离小于该值时，才再二维网格中寻找连接点，匹配连接
        //************************************//

        //*********M21fm和Mesh文件中的常量*********//
        public const double INSERT_WEIR_MINDISTANCE = 200;  //当给定点的距离小于该值时，则认为点在堤防上，并将堤防一分为二，新建一个堰
        public const double MESHSIDE_LENGTH = 100;    //当构建全新模型时，默认的剖分网格边长
        //************************************//

        //设计洪水模拟时间在降雨时间后增加的天数
        public const int DESIGNRF_SIMULATE_ADDDAYS = 3;  //选择设计洪水的量级和历时后，再增加3天为模拟时间

        //默认提前模拟小时数(为了模型的稳定和现在水情的复现，提前N小时模拟，结果提取再去掉N小时)，当模拟当前和历史洪水时，会采用该值
        public const int AHEAD_HOURS = 72;
        public const int AUTOFORECAST_AHEAD_HOURS = 24;  //自动预报预热期输出结果(还剩48h)，需<=提前模拟小时数

        //水库入库里，水库所在河道流速--用于计算入库增加时间
        public const double RES_INFLOW_SPEED = 1;

        //自动预报模拟的小时数量
        public const double AUTOFORECAST_HOURS = 72;  //自动预报未来3天
        public const double AUTOFORECAST_RESSAVE_RAINVALUE = 10; //自动预报结果保存的降雨阈值

        //默认模型的方案名(文件夹名)
        public const string DEFAULT_MODELNAME = "default";
        public const string DEFAULT_FANGANNAME = "默认方案";

        //自动预报方案名
        public const string AUTO_MODELNAME = "model_auto";

        //结果水面json文件夹名
        public const string GEOJSON_DIRNAME = "polygon_json";

        //默认水文单位线的流域名
        public const string DEFAULT_CATCHMENTNAME = "DEFAULT";

        //所有方案模型所在的文件夹名(固定死！)
        public const string MODEL_DIRNAME = "wg_models";

        //处理dfsu生成GIS时态数据的子进程名
        public const string SUBPROCESSNAME_DFSUTOGIS = "DfsuResultProcess.exe";

        //定义一个全局信号量，用于控制进度文件的读写冲突
        public static Semaphore write_read_semaphore = new Semaphore(1, 1);

        //时间格式常量
        public static string TIMEFORMAT = "yyyy/MM/dd HH:mm:ss";

        //方案名称和模型描述分隔符
        public static char MODEL_DESC_SPLIT = '$';

        //默认投影
        public static string DEFAULT_COOR = "NON-UTM";

        //前端每秒播放的速度（步/秒）
        public static int STEPS_PERSECOND = 1;

        //获取系统部署路径
        public static string system_path = "";

        //获取系统部署路径
        public static string Get_Sysdir()
        {
            return system_path;
            //return System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //return @"E:\";
        }

        //设置系统部署路径
        public static void Set_Sysdir(string systempath)
        {
            Model_Const.system_path = systempath;
        }
    }

    //整个项目中，用于记录数据库名和表名的常量、全局变量
    public static class Mysql_GlobalVar
    {
        //模型数据库
        public const string NOWITEM_DBNAME = "kingbase";        //当前项目的数据库

        public const string BASEDATA_DBNAME = "basedata";        //用于存储基本资料的数据库名
        public const string RFINCATCHMENT_TABLENAME = "rainfallstation";    //用于存储雨量站和流域对应关系的表名
        public const string DESIGNRAINFALL_TABLENAME = "design_rainfall";   //用于存储设计降雨时间序列的表名
        public const string HISTORYRAINFALL_TABLENAME = "history_day_p";    //用于存储历史降雨时间序列的表名（1951年至2012年）
        public const string HISTORYEVA_TABLENAME = "history_day_e";         //用于存储历史蒸发时间序列的表名
        public const string STANDARY24TEMPLATE_TABLENAME = "template_rainfall";     //用于存储标准24小时雨形模板时间序列的表名
        public const string MODELFILETEMPLATE_TABLENAME = "template_modelfile";    //用于存储模型文件模板的表名
        public const string CATCHMENTPOINT_TABLENAME = "catchment_boundrypoint"; //用于存储集水区边界顶点坐标的表名
        public const string HYDROGRAPH_TABLENAME = "hydrograph";       //用于存放各流域时段单位线的表名
        public const string EVA_STCDNAME = "50320350";        //小洪河流域的历史日蒸发资料(用于无数据时期的内插平均值)就固定从这个典型站点提取
        public const string RAINFALL_STCDNAME = "50320450";           //小洪河流域的历史日降雨资料(用于无数据时期的内插平均值)就固定从这个典型站点提取

        public const string MODELPLAN_TABLENAME = "model_plan"; //用于存储所有业务模型方案的表名
        public const string MODELPAR_TABLENAME = "model_plan_para"; //用于存储模型的表名
        public const string MODELBUSINESS_TABLENAME = "model_business"; //用于存储业务模型属性的表名

        public const string MIKE11RES_TABLENAME = "mike11_data_result";   //用于存储典型位置一维结果数据库表名  
        public const string MIKE11_HISTORY_AUTOFORCASTRES_TABLENAME = "mike11_history_autoforcast_result";   //用于存储历史自动预报主要结果的表名
        public const string MODEL_SH_TABLENAME = "mike_sh_village";   //用于存储小流域山洪信息的表名
        public const string MIKE11_SAMPLEGIS_TABLENAME = "mike11_gis_sample";//用于存储一维GIS 样本线和面的表名
        public const string MIKE11_GISLineRES_TABLENAME = "mike11_gisline_result";//用于存储一维GIS 线过程和统计结果的表名
        public const string MIKE11_GISPOLYGONRES_TABLENAME = "mike11_gispolygon_result";//用于存储一维GIS 面过程和统计结果的表名
        public const string MIKE11_GISPOLYGONP_SECTION = "mike11_gispolygon_pointsection";  //河道样例多边形中，每段河道的每个折点对应的河道断面
        public const string SECTION_TABLENAME = "mike11_sectiondata";   //用于存储原始断面数据的数据库表名
        public const string SECTIONOBJECT_TABLENAME = "mike11_section_object";   //用于存储原始断面数据对象的数据库表名
        public const string STRUCT_INFO = "mike11_struct_info";        //用于存储建筑物中文名称和桩号信息的表(渠道建筑物进出口分开)
        public const string WATER_CONDITION = "mike11_water_condition"; //用于存储实测水位流量数据
        public const string RES_BASEINFO = "st_rsvrfcch_b"; //用于水库工程基础信息表名
        public const string SECTION_QHRELATION = "mike11_section_qhrelation"; //用于存储河道控制断面水位流量关系
        public const string STRUCT_QHRELATION = "mike11_struct_qhrelation";    //用于存储建筑物闸前水位和流量的关系
        public const string NODESTORY_SPEED = "mike11_reach_nodestory_speed";   //用于存储河道不冲流速的表  
        public const string RES_HVRELATION = "mike11_res_hvrelation";          //用于存储水库水位-库容关系曲线
        public const string XZHQ_BASEINFO = "geo_fsda_base";          //用于存储蓄滞洪区基本信息
        public const string CATCHMENT_RFMODEL = "mike11_catchment_rfmodel";    //各子流域产汇流模型类型
        public const string CATCHMENT_BASEINFO = "base_basin_b";               //各子流域基本信息
        public const string BASE_SEC_B = "base_sect_b";               //河道断面滩地和堤顶高程表
        public const string MIKE11_CATCHMENT_SETDISCHARGE = "model_result_outflow";   //存储各子流域指定的洪水过程的表
        public const string CATCHMENT_RAINFALLRES_TABLENAME = "model_rain_area";   //存储各子流域降雨过程的表
        public const string MODEL_RAIN_FLOOD_TABLENAME = "model_rain_flood";   //存储场次降雨洪水的表
        public const string CATCHMENT_FLOODRES_TABLENAME = "model_result_outflow";   //存储各子流域洪水结果的表
        public const string REACH_FLOODINFO = "mike_design_floodinfo";    //河道设计洪水信息
        public const string NSBD_INFO_TABLENAME = "mike11_nsbdsection_info";      //南水北调交叉断面洪水信息
        public const string FAULTGATE_INFO_TABLENAME = "mike21_gate_info";      //故障闸门基础信息表
        public const string STRUCT_REGIONINFO_TABLENAME = "mike_struct_region_info";  //河道水库蓄滞洪区工程所在地市和主管单位信息表

        public static string now_modeldb_serip = "10.20.2.153";  //服务器IP地址
        public static int now_port = 54321;  //端口号
        public static string now_modeldb_user =  "hnsl"; //用户名
        public static string now_modeldb_password = "Hnsl@6915"; //密码
        public static string now_character = "utf8"; //字符集

        public static string now_instance = "wg_mike11";

        //外部雨量站数据库
        public const string RFSTATION_DBNAME = "hydrological";
        public const string RFSTATION_TABLENAME = "hy_dcp_d";
        public static string now_rfstationdb_serip = "10.41.25.197";
        public static string now_rfstationdb_user = "root";
        public static string now_rfstationdb_password = "1234";

        //python执行程序和脚本
        public const string PYTHONEXE_PATH = @"C:\Python39\python.exe";  //python执行程序路径版3.9.11,对应ArcgisPro3.0
        public static string GP_MIKE11TJ_PYTHON = Model_Const.system_path + @"\source\ymfx\project.py";   //python处理过程脚本文件路径
        public static string BASE_DEM_FILE = Model_Const.system_path + @"\source\ymfx\base_dem\wg_20m.tif";    //用于淹没分析的基础DEM

        //请求更新水情和工情URL地址
        public const string server_url = "http://10.20.2.153:8089/modelPlatf";
        public static string level_serverurl = server_url+ "/monitor/rsvr/now";  //水库河道水情请求地址
        public static string gatestate_serverurl = server_url + "/model/mike/init/gate";  //闸站工情请求地址
        public static string catchmentrain_serverurl = server_url + "/model/modelRainArea/getByPlan";  //子流域降雨过程请求地址
        public static string catchmentq_serverurl = server_url + "/model/mike/runoff";  //子流域洪水过程请求地址
        public static string arearain_serverurl = server_url + "/model/modelRainArea/getBasinAreaRainStc";    //流域平均面降雨过程请求地址

        public static string create_rftjgis_serverurl = server_url + "/contour/rainProc/plan";  //请求降雨等值面生成
        public static string forecast_rain_serverurl = server_url + "/forecast/forecastRainEcmwf/avg";   //获取格网预报降雨结果地址
        public static string csdsk_kqym_serverurl = server_url + "/base/basePolderB/stc";          //蓄滞洪区不同水位下圩堤淹没结果

        public const string nam_serverurl = "http://hzz.ysy.com.cn/wg_modelserver/namserver/Model_Ser.ashx";         //Nam模型服务地址
        public const string mike21_serverurl = "http://hzz.ysy.com.cn/wg_modelserver/hd_mike21server/Model_Ser.ashx"; //mike21模型服务地址
        //public const string mike21_serverurl = "http://localhost:7046//Model_Ser.ashx"; //mike21模型服务地址

        //百度语音合成ID和秘钥
        public static string now_baidu_AppID = "11217672";
        public static string now_baidu_APIKey = "YxVhnI2U3Qkb08gDBe3Gq3ui";
        public static string now_baidu_SecretKey = "uUSXuBh9hWGATbNxGonQjLlaLVcxvpNF";

        //arcgis的GP服务URL地址
        public static string gp_serverurl = "http://hzz.ysy.com.cn/arcgis/rest/services/qsfj/qsdgpserver/GPServer/渠首段GP服务工具/submitJob"; //水质GIS生产
        public static string gp_cancleserverurl = "http://hzz.ysy.com.cn/arcgis/rest/services/qsfj/qsdgpserver/GPServer/渠首段GP服务工具/jobs"; //停止GP服务

        //初始化服务器IP、登录名和密码
        public static void Intial_DBGlobalVar()
        {
            //数据库服务器和连接用户信息初始化
            Mysql_GlobalVar.now_modeldb_serip = "10.20.2.153";  //服务器IP地址
            Mysql_GlobalVar.now_port = 54321;  //端口号
            Mysql_GlobalVar.now_modeldb_user = "hnsl"; //用户名
            Mysql_GlobalVar.now_modeldb_password = "Hnsl@6915"; //密码
            Mysql_GlobalVar.now_character = "utf8"; //字符集

            Mysql_GlobalVar.now_rfstationdb_serip = "10.41.25.197";
            Mysql_GlobalVar.now_rfstationdb_user = "root";
            Mysql_GlobalVar.now_rfstationdb_password = "1234";

            //百度语音合成ID和秘钥
            Mysql_GlobalVar.now_baidu_AppID = "11217672";
            Mysql_GlobalVar.now_baidu_APIKey = "YxVhnI2U3Qkb08gDBe3Gq3ui";
            Mysql_GlobalVar.now_baidu_SecretKey = "uUSXuBh9hWGATbNxGonQjLlaLVcxvpNF";

            //arcgis的GP服务URL地址(http是6080,https是6443)
            Mysql_GlobalVar.gp_serverurl = "http://hzz.ysy.com.cn/arcgis/rest/services/qsfj/qsdgpserver/GPServer/渠首段GP服务工具/submitJob";
            Mysql_GlobalVar.gp_cancleserverurl = "http://hzz.ysy.com.cn/arcgis/rest/services/qsfj/qsdgpserver/GPServer/渠首段GP服务工具/jobs"; 
        }
    }

}