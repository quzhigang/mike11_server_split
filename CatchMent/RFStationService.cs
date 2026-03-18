using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using Kdbndp;

using bjd_model.Common;
using bjd_model.Const_Global;

using bjd_model.Mike11;
using bjd_model;

namespace bjd_model.CatchMent
{

    //计算流域平均雨量的服务类
    public class RFService
    {
        //连接的数据库字段
        // private Mysql m_pDBMgr = null;

        //构造函数
        public RFService()
        {

        }

        // 根据流域名获取流域里的某个时间段内的降雨量序列
        public static Dictionary<DateTime, double> Get_CatchmentRainfall(string sBasin, DateTime begin, DateTime end, int nStep = 1)
        {
            //获取流域内的所有雨量站点
            RFService pRainfallStationService = new RFService();
            List<RainfallStation> rainfallStation = pRainfallStationService.getRainfallStaion(sBasin, begin, end, nStep);

            //获取流域的降雨过程和每一步开始时间的对应关系
            int nStepCount = Convert.ToInt16(end.Subtract(begin).TotalHours / nStep); //时段划分的总步数
            Dictionary<DateTime, double> areaP = getBasinAverageP(rainfallStation, nStepCount, begin, nStep);

            //如果最后一个时间步不等于结束时间，则添加0
            if (!areaP.Keys.Contains(end))
            {
                areaP.Add(end, 0);
            }

            return areaP;
        }

        // 根据雨量站集合获取流域的平均雨量
        public static Dictionary<DateTime, double> getBasinAverageP(List<RainfallStation> Rfstationlist, int nCount, DateTime begin, int nStep = 1)
        {
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();//降雨时间和降雨量对应关系,一个步长对应一个降雨量
            DateTime start = new DateTime();

            //找到第一个正常的雨量站
            RainfallStation first_normal_RFstation = Rfstationlist[0];
            for (int i = 0; i < Rfstationlist.Count; i++)
            {
                if (Rfstationlist[i].m_isNormal == true)
                {
                    first_normal_RFstation = Rfstationlist[i];
                    break;
                }
            }

            //把每一个时间步里 每一个雨量站的这步的值相加
            for (int i = 0; i < nCount; i++)
            {
                if (i == 0)
                {
                    start = begin;
                }
                else
                {
                    start = start.AddHours(nStep);
                }

                double fBasinP = 0F;
                foreach (RainfallStation station in Rfstationlist)
                {
                    if (station.m_isNormal)
                    {
                        Dictionary<int, double> P = station.m_dP;
                        double fP = P.Values.ElementAt(i); //每个站点的第i步 乘权重后的雨量
                        fBasinP += fP;                 //所有站点这个权重雨量值累加
                    }
                    else
                    {
                        //等于第一个正常的
                        Dictionary<int, double> P = first_normal_RFstation.m_dP;
                        double fP = P.Values.ElementAt(i) * station.m_fWeight / first_normal_RFstation.m_fWeight;
                        fBasinP += fP;
                    }
                }
                result.Add(start, fBasinP);
            }
            return result;
        }

        // 通过流域名称获取该流域内的所有的雨量站和其雨量时间序列信息
        public List<RainfallStation> getRainfallStaion(string catchmentname, DateTime begin, DateTime end, int nStep)
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select STCD from " + Mysql_GlobalVar.RFINCATCHMENT_TABLENAME + " where catchment = '" + catchmentname + "'";
            List<RainfallStation> Rfstation_List = new List<RainfallStation>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            double allcatchment_value_sum = 0;
            for (int i = 0; i < nLen; i++)
            {
                RainfallStation pRainfallStation = new RainfallStation();
                string sSTCD = dt.Rows[i][0].ToString();
                pRainfallStation.m_sSTCD = sSTCD;
                pRainfallStation.m_fWeight = 1.0 / nLen;  //暂时先按平均考虑，以后再做泰森多边形

                Dictionary<int, double> P;
                if (begin.Year >= 2016)   //大于等于2016年就从实时雨量数据库读取数据
                {
                    P = getRainfall(begin, end, nStep, sSTCD, 1.0 / nLen);//获取单个雨量站的时段内的降雨字典,和降雨的小时数有关
                }
                else
                {
                    P = getRainfall_History(begin, end, nStep, sSTCD, 1.0 / nLen);//获取单个雨量站的时段内的降雨字典,和降雨的小时数有关
                }
                pRainfallStation.m_dP = P;

                //如果获取不到记录 或者 该雨量站的值总和为0，则认为该雨量站不正常

                if (pRainfallStation.m_dP.Count == 0 || pRainfallStation.m_dP.Values.Sum() == 0)
                {
                    pRainfallStation.m_isNormal = false;
                }

                if (pRainfallStation.m_dP.Count != 0)
                {
                    allcatchment_value_sum += pRainfallStation.m_dP.Values.Sum();
                }
                Rfstation_List.Add(pRainfallStation);
            }

            //重新判断一次，如果集合内所有的雨量站降雨量总和都是0，则有记录但降雨量为0的重新赋值为正常
            bool all_rfstation_isnotnormal = true;
            for (int i = 0; i < Rfstation_List.Count; i++)
            {
                //如果总和为0，更改有纪录但值为0的雨量站为正常
                if (allcatchment_value_sum == 0)
                {
                    if (Rfstation_List[i].m_dP.Count != 0)  //如果有记录
                    {
                        Rfstation_List[i].m_isNormal = true;
                    }
                }

                //判断是否全部不正常
                if (Rfstation_List[i].m_isNormal == true)
                {
                    all_rfstation_isnotnormal = false;
                }
            }

            //如果一个雨量站也没有获取 或者 获取的雨量站没一个正常，则取最近的一个代替
            if (Rfstation_List == null || all_rfstation_isnotnormal == true)
            {
                //求流域的中心点坐标
                List<PointXY> polygonPoints = GetCatchment_Pointdic(catchmentname);
                PointXY centerpoint = PointXY.Get_CenterPointxy(polygonPoints);

                //获取距离流域中心最近且能在给定时间段内有降雨记录的雨量站名
                string minrfstation_stcd = Get_Near_HaveRows_Rfstcd(mysql, centerpoint, begin, end, nStep);

                //获取雨量站的降雨序列
                RainfallStation pRainfallStation = new RainfallStation();
                pRainfallStation.m_sSTCD = minrfstation_stcd;
                pRainfallStation.m_fWeight = 1.0;

                Dictionary<int, double> P;
                if (begin.Year >= 2016)   //大于等于2016年就从实时雨量数据库读取数据
                {
                    P = getRainfall(begin, end, nStep, minrfstation_stcd, 1.0);//获取单个雨量站的时段内的降雨字典,和降雨的小时数有关
                }
                else
                {
                    P = getRainfall_History(begin, end, nStep, minrfstation_stcd, 1.0);//获取单个雨量站的时段内的降雨字典,和降雨的小时数有关
                }

                pRainfallStation.m_dP = P;

                Rfstation_List.Add(pRainfallStation);
            }
            return Rfstation_List;
        }

        //获取距离流域中心最近且能在给定时间段内有降雨记录的雨量站名
        public static string Get_Near_HaveRows_Rfstcd(Mysql mysql, PointXY catchment_centerpoint, DateTime begin, DateTime end, int nStep)
        {
            RFService pRainfallStationService = new RFService();

            //获取所有雨量站点的坐标（经纬度）
            string sql1 = "select STCD,LAT,LNG from " + Mysql_GlobalVar.RFINCATCHMENT_TABLENAME;

            //获取最近雨量站的编号
            DataTable dt = Mysql.query(sql1);
            string minrfstation_stcd = "";
            int nLen1 = dt.Rows.Count;

            Dictionary<string, double> rfstation_distance = new Dictionary<string, double>();
            for (int i = 0; i < nLen1; i++)
            {
                string STCD = dt.Rows[i][0].ToString();
                double x1 = double.Parse(dt.Rows[i][1].ToString());
                double y1 = double.Parse(dt.Rows[i][2].ToString());
                double distance = PointXY.Get_ptop_distance(x1, y1, catchment_centerpoint.X, catchment_centerpoint.Y);
                rfstation_distance.Add(STCD, distance);
            }

            //键值对集合重新按值由小到大排序
            Dictionary<string, double> new_rfstation = rfstation_distance.OrderBy(p => p.Value).ToDictionary(p => p.Key, p => p.Value);

            //从第一个即最近的开始找，如果有记录则返回
            Dictionary<int, double> P = new Dictionary<int, double>();
            string first_haveraws_stcd = "";
            bool first_haveraws = false;
            for (int i = 0; i < new_rfstation.Count; i++)
            {
                minrfstation_stcd = new_rfstation.Keys.ElementAt(i);
                if (begin.Year >= 2016)   //大于等于2016年就从实时雨量数据库读取数据
                {
                    P = pRainfallStationService.getRainfall(begin, end, nStep, minrfstation_stcd, 1.0);
                }
                else
                {
                    P = pRainfallStationService.getRainfall_History(begin, end, nStep, minrfstation_stcd, 1.0);
                }

                //找到首个有记录的雨量站
                if (P.Count != 0 && first_haveraws == false)
                {
                    first_haveraws_stcd = new_rfstation.Keys.ElementAt(i);
                    first_haveraws = true;
                }

                //如果能找到有记录且总和不等于0的则返回，如果连找周围10个都没有就返回了（以免浪费时间）
                if ((P.Count != 0 && P.Values.Sum() != 0) || i > 10)
                {
                    break;
                }
            }

            if (minrfstation_stcd == "")
            {
                minrfstation_stcd = first_haveraws_stcd;
            }

            return minrfstation_stcd;
        }

        // 从数据库获取流域边界点集合
        public static List<PointXY> GetCatchment_Pointdic(string catchmentname)
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select x,y from " + Mysql_GlobalVar.CATCHMENTPOINT_TABLENAME + " where name = '" + catchmentname + "' order by number";
            List<PointXY> Catchment_Pointdic = new List<PointXY>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                PointXY point;
                point.X = double.Parse(dt.Rows[i][0].ToString());
                point.Y = double.Parse(dt.Rows[i][1].ToString());
                Catchment_Pointdic.Add(point);
            }

            return Catchment_Pointdic;
        }

        //根据流域名获取该流域时段单位线(单位线时段1小时)
        public static Dictionary<int, double> GetCatchment_UHMdic(string catchmentname)
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select axis_x,discharge_unit10mm from " + Mysql_GlobalVar.HYDROGRAPH_TABLENAME + " where catchment_name = '" + catchmentname + "'";

            Dictionary<int, double> Catchment_UHMdic = new Dictionary<int, double>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                int axisx = int.Parse(dt.Rows[i][0].ToString());
                double discharge_unit = double.Parse(dt.Rows[i][1].ToString());
                if (!Catchment_UHMdic.Keys.Contains(axisx))
                {
                    Catchment_UHMdic.Add(axisx, discharge_unit);
                }
            }

            return Catchment_UHMdic;
        }

        // 通过设计降雨量级和历时，获取设计降雨时间序列
        public static Dictionary<DateTime, double> GetDesign_RFdic(DesignRF_Info designrf_info)
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select datetime,p from " + Mysql_GlobalVar.DESIGNRAINFALL_TABLENAME + " where level = " + (int)designrf_info.designrf_level + " and span_day_level = " + (int)designrf_info.designrf_timespan;
            Dictionary<DateTime, double> Design_RFdic = new Dictionary<DateTime, double>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                DateTime nowdatetime = DateTime.Parse(dt.Rows[i][0].ToString());
                double rfp = double.Parse(dt.Rows[i][1].ToString());
                Design_RFdic.Add(nowdatetime, rfp);
            }

            return Design_RFdic;
        }

        // 获取该地区历史蒸发时间序列
        public static Dictionary<DateTime, double> GetHistory_Evadic(DateTime starttime, DateTime endtime)
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select DT,WSFE from " + Mysql_GlobalVar.HISTORYEVA_TABLENAME + " where STCD = '" + Mysql_GlobalVar.EVA_STCDNAME + "' and DT between '" + starttime + "' and '" + endtime + "' order by DT asc";
            Dictionary<DateTime, double> History_Evadic = new Dictionary<DateTime, double>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                DateTime nowdatetime = DateTime.Parse(dt.Rows[i][0].ToString());
                double rfp = double.Parse(dt.Rows[i][1].ToString());
                if (i == 0) rfp = 0;
                History_Evadic.Add(nowdatetime, rfp);
            }

            return History_Evadic;
        }

        // 获取该地区历史降雨时间序列
        public static Dictionary<DateTime, double> GetHistory_RFdic(DateTime starttime, DateTime endtime)
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select DT,P from " + Mysql_GlobalVar.HISTORYRAINFALL_TABLENAME + " where STCD = '" + Mysql_GlobalVar.RAINFALL_STCDNAME + "' and DT between '" + starttime + "' and '" + endtime + "' order by DT asc";
            Dictionary<DateTime, double> History_RFdic = new Dictionary<DateTime, double>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                DateTime nowdatetime = DateTime.Parse(dt.Rows[i][0].ToString());
                double rfp = double.Parse(dt.Rows[i][1].ToString());
                History_RFdic.Add(nowdatetime, rfp);
            }

            return History_RFdic;
        }

        // 获取标准24小时雨形模板dic
        public static Dictionary<DateTime, double> GetStand24_RFmodeldic()
        {
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
            string sql = "select datetime,P from " + Mysql_GlobalVar.STANDARY24TEMPLATE_TABLENAME + " order by datetime asc";
            Dictionary<DateTime, double> Stand_RFdic = new Dictionary<DateTime, double>();

            //获取的记录按表形式存储
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                DateTime nowdatetime = DateTime.Parse(dt.Rows[i][0].ToString());
                double rfp = double.Parse(dt.Rows[i][1].ToString());
                Stand_RFdic.Add(nowdatetime, rfp);
            }

            return Stand_RFdic;
        }

        // 获取每个站点对应的每一步的降雨量总和（实时数据）
        Dictionary<int, double> getRainfall(DateTime begin, DateTime end, int nStep, string stcd, double fWeidght)
        {
            stcd = stcd.Trim();//出去stcd中的空格
            Dictionary<int, double> result = new Dictionary<int, double>();
            int nCount = Convert.ToInt16(end.Subtract(begin).TotalHours / nStep);//总的步数
            DataTable dt = getRainfall(stcd, begin, end);//时间段降雨数据
            int nRowCount = dt.Rows.Count;//dt的总行数

            int nLastStepIndex = 0;
            float fSump = 0f;
            for (int i = 0; i < nRowCount; i++)
            {
                string sSTCD = dt.Rows[i][0].ToString();
                DateTime timeNote = Convert.ToDateTime(dt.Rows[i][1]);//dt的某一行的降雨的时间点
                float fP = 0f;
                string sP = dt.Rows[i][2].ToString();//从数据库中获取每一条数据对应的雨量信息
                if (sP.Length > 0)
                {
                    fP = float.Parse(dt.Rows[i][2].ToString());//将雨量数据的格式进行转换
                }

                int nStepIndex = stepIndex(begin, timeNote, nStep);//获取每一条的数据属于哪一步的数据
                if (sSTCD.Equals(stcd) && nLastStepIndex == nStepIndex)
                {
                    fSump += fP;//将每一步内所有的时间点的雨量进行求和
                }
                else
                {
                    result.Add(nLastStepIndex, fSump * fWeidght);//将上一步的结果进行保存到结果集中

                    //如果是连续的一步则直接添加到结果集中,
                    //如果不是连续的一步则将nStepIndex和上一步直接的每一步进行补0操作
                    if (nStepIndex - nLastStepIndex == 1)
                    {
                        nLastStepIndex = nStepIndex;//记录该步是第几步
                        fSump = 0f;//每一步的求和基数初始化为0
                    }
                    else
                    {
                        int nStepCount = nStepIndex - nLastStepIndex;//当前步与上一步之间相差的步数
                        emptyData(result, nStepCount);//进行补0操作
                        fSump = 0f;
                        nLastStepIndex = nStepIndex;//记录该步是第几步
                    }
                }
            }

            //如果没有达到总步数，则后面补0
            int step_count = Convert.ToInt16(end.Subtract(begin).TotalHours / nStep);   //实际应该有的总步数
            int last_step = result.Count;
            if (result.Count != step_count)
            {
                int add_step_count = step_count - result.Count;  //还需要增加的步数
                for (int i = 0; i < add_step_count; i++)
                {
                    result.Add(last_step + i, 0);
                }
            }

            return result;
        }

        // 获取每个站点对应的每一步的降雨量总和（历史数据）
        Dictionary<int, double> getRainfall_History(DateTime begin, DateTime end, int nStep, string stcd, double fWeidght)
        {
            stcd = stcd.Trim();//出去stcd中的空格
            Dictionary<int, double> result = new Dictionary<int, double>();
            int nCount = Convert.ToInt16(end.Subtract(begin).TotalHours / nStep);//总的步数
            DataTable dt = getRainfall(stcd, begin, end);//时间段降雨数据
            int nRowCount = dt.Rows.Count;//dt的总行数

            int nLastStepIndex = 0;
            float fSump = 0f;

            DateTime last_datetime = begin;
            int last_step_number = 0;
            for (int i = 0; i < nRowCount - 1; i++)
            {
                string sSTCD = dt.Rows[i][0].ToString();
                DateTime timeNote = Convert.ToDateTime(dt.Rows[i + 1][1]);//注意，这里取下一个记录的时间，
                float fP = 0f;
                string sP = dt.Rows[i][2].ToString();//雨量值还是取这一步的
                if (sP.Length > 0)
                {
                    fP = float.Parse(dt.Rows[i][2].ToString());//将雨量数据的格式进行转换
                }

                int hour_count = (int)timeNote.Subtract(last_datetime).TotalHours;
                fSump = fP;
                for (int j = 0; j < hour_count; j++)
                {
                    nLastStepIndex = last_step_number + j + 1;
                    result.Add(nLastStepIndex, fSump * fWeidght / hour_count);
                }
                last_step_number = last_step_number + nLastStepIndex;

                last_datetime = timeNote;

            }
            return result;
        }



        // 格式化结果数据将一步的数据进行求和计算
        private int stepIndex(DateTime begin, DateTime timeNote, int nStep)
        {
            double dHours = Convert.ToInt16(begin.Subtract(timeNote).TotalHours / nStep);
            double dStepIndex = Math.Floor(dHours);
            return Math.Abs(Convert.ToInt16(dStepIndex));
        }

        // 如果某个步长内没有降雨则补0
        private void emptyData(Dictionary<int, double> result, int nCount)
        {
            int nStartIndex = result.Keys.Count - 1;//添加数据的起始位置
            for (int i = 1; i < nCount; i++)
            {
                result.Add(nStartIndex + i, 0f);
            }
        }

        // 获取给定的时间段内的所有的降雨量
        public DataTable getRainfall(string sSTCD, DateTime begin, DateTime end)
        {
            if (begin.Year >= 2016)   //大于等于2016年就从实时雨量数据库读取数据
            {
                Mysql pMysql = new Mysql(Mysql_GlobalVar.RFSTATION_DBNAME, Mysql_GlobalVar.now_rfstationdb_serip,
                Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_rfstationdb_user, Mysql_GlobalVar.now_rfstationdb_password, Mysql_GlobalVar.now_character);
                string sql = "select STCD , timenote , P from " + Mysql_GlobalVar.RFSTATION_TABLENAME + " where STCD = '" + sSTCD + "' and timenote between '" + begin + "' and '" + end + "' order by timenote asc";
                return Mysql.query(sql);//获取时间段内全部雨量
            }
            else         //否则就从历史数据库读取数据
            {
                Mysql pMysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip,
                Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);
                string sql = "select STCD , DT , P from " + Mysql_GlobalVar.HISTORYRAINFALL_TABLENAME + " where STCD = '" + sSTCD + "' and DT between '" + begin + "' and '" + end + "' order by DT asc";
                return Mysql.query(sql);//获取时间段内全部雨量
            }

        }


        //根据给定的流域控制点的集合更新雨量站所属流域名称
        public static void Update_RFStation_CatchmentName(string catchmentname, List<PointXY> catchment_pointxylist)
        {
            //获取最大最小值
            double min_x;
            double max_x;
            double min_y;
            double max_y;
            PointXY.Getmaxmin(catchment_pointxylist, out min_x, out max_x, out min_y, out max_y);

            //打开数据库连接
            Mysql mysql = new Mysql(Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.now_modeldb_serip, Mysql_GlobalVar.now_port, Mysql_GlobalVar.now_modeldb_user, Mysql_GlobalVar.now_modeldb_password, Mysql_GlobalVar.now_character);

            //获取满足最大最小坐标值要求的雨量站点编号和XY坐标，并判断是否在多边形内，如果在就更新相应的流域名
            string sql = "select STCD,LAT,LNG from " + Mysql_GlobalVar.RFINCATCHMENT_TABLENAME + " where LAT > " + min_x + " and LAT < " + max_x + " and LNG > " + min_y + " and LNG < " + max_y;
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            for (int i = 0; i < nLen; i++)
            {
                string sSTCD = dt.Rows[i][0].ToString();
                PointXY point;
                point.X = double.Parse(dt.Rows[i][1].ToString());
                point.Y = double.Parse(dt.Rows[i][2].ToString());
                bool IsIn = PointXY.IsInPolygon(point, catchment_pointxylist);
                if (IsIn && sSTCD.EndsWith("0"))
                {
                    string sql1 = "update " + Mysql_GlobalVar.RFINCATCHMENT_TABLENAME + " set catchment = '" + catchmentname + "' where STCD = " + "'" + sSTCD + "'";
                    Mysql.Execute_Command(sql1);
                }
            }
            Console.WriteLine("数据库{0}的表格{1}更新完成！", Mysql_GlobalVar.BASEDATA_DBNAME, Mysql_GlobalVar.RFINCATCHMENT_TABLENAME);

        }
    }

    //雨量站类
    public class RainfallStation
    {
        public string m_sName;// 观测站名称
        public string m_sSTCD;//观测站编号
        public Dictionary<int, double> m_dP;//观测站雨量
        public DateTime m_dt;//降雨的时段
        public bool m_isNormal = true;//测站是否正常运转，默认情况下正常运转
        public double m_fWeight;//流域雨量站根据泰森多边形获得权重值
        public string Name
        {
            get
            {
                return m_sName;
            }
            set
            {
                m_sName = Name;
            }
        }
        public string STCD
        {
            get
            {
                return m_sSTCD;
            }
            set
            {
                m_sSTCD = STCD;
            }
        }
        public Dictionary<int, double> P
        {
            get
            {
                return m_dP;
            }
            set
            {
                m_dP = P;
            }
        }
        public bool isNormal
        {
            get
            {
                return m_isNormal;
            }
            set
            {
                m_isNormal = isNormal;
            }
        }
        public double weight
        {
            get
            {
                return m_fWeight;
            }
            set
            {
                m_fWeight = weight;
            }
        }
    }

}