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
        #region ******************水位纵断面结果********************
        //查询所有河道全部的水位、流量、流速等纵断面数据过程
        public static Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_AllReach_Grid_ZdmData(HydroModel hydromodel,
            mike11_restype item_type, ResultData resdata, string model_instance)
        {
            //查询河道列表
            List<ReachInfo> main_reach = Item_Info.Get_MainReachInfo(model_instance);
            Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> allreach_grid_zdmdata = new Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>>();

            //获取结果时间数组
            DateTime[] timearray = resdata.TimesList.ToArray();
            if (item_type == mike11_restype.Volume || item_type == mike11_restype.Velocity) timearray = Get_ResTime_List(hydromodel);  //统一用res的时间序列

            //遍历主河道
            for (int i = 0; i < main_reach.Count; i++)
            {
                string reachname = main_reach[i].reachname;
                Dictionary<DateTime, Dictionary<double, double>> grid_zdmdata = Get_Grid_ZdmData(hydromodel, reachname, item_type, resdata, timearray);
                allreach_grid_zdmdata.Add(reachname, grid_zdmdata);
            }
            return allreach_grid_zdmdata;
        }

        //查询指定河道全部的水位、流量、流速等纵断面数据过程
        public static Dictionary<DateTime, Dictionary<double, double>> Get_Grid_ZdmData(HydroModel hydromodel, string reachname, mike11_restype item_type, ResultData resdata, DateTime[] timearray)
        {
            //判断是否有水质
            if (item_type == mike11_restype.Concentration && hydromodel.ModelGlobalPars.Select_model != CalculateModel.ad_and_hd) return null;

            ReachInfo reach = hydromodel.Mike11Pars.ReachList.Get_Reachinfo(reachname);
            Reach_Segment reachsegment = Reach_Segment.Get_ReachSegment(reach.reachname, reach.start_chainage, reach.end_chainage);
            Dictionary<DateTime, Dictionary<double, double>> zdm_date_dic = Res11reader_Allgird(hydromodel, reachname, item_type, resdata, timearray);

            //移除提前时刻数据
            Remove_Ahead_Res(hydromodel, ref zdm_date_dic);

            return zdm_date_dic;
        }

        //查询指定河段的 水位、水量纵断面数据过程
        public static Dictionary<DateTime, Dictionary<double, double>> Res11reader_Allgird(HydroModel hydromodel, string reachname, mike11_restype itemtype, ResultData resdata, DateTime[] timearray)
        {
            Dictionary<DateTime, Dictionary<double, double>> all_grid_resdic = new Dictionary<DateTime, Dictionary<double, double>>();

            //项目名
            string itemname = null;
            if (itemtype == mike11_restype.Water_Level)
            {
                itemname = itemtype.ToString().Replace("_", " ");
            }
            else if (itemtype == mike11_restype.Concentration)
            {
                itemname = hydromodel.Mike11Pars.Ad_Pars.Component_list[0].com_name; //这里有问题，暂时只取第一个组分
            }
            else
            {
                if (Path.GetExtension(hydromodel.Modelfiles.Hdres11_filename) == ".res11")
                {
                    itemname = itemtype.ToString();
                }
                else
                {
                    if (itemtype == mike11_restype.Velocity)
                    {
                        itemname = "Flow velocity";
                    }
                    else if (itemtype == mike11_restype.Volume)
                    {
                        itemname = "Water volume";
                    }
                    else
                    {
                        itemname = itemtype.ToString();
                    }
                }
            }

            all_grid_resdic = Get_Allgrid_resdic(hydromodel, reachname, itemname, itemtype, resdata, timearray);

            return all_grid_resdic;
        }

        //查询指定河段的 水位、水量纵断面数据过程
        public static Dictionary<DateTime, Dictionary<double, double>> Get_Allgrid_resdic(HydroModel hydromodel, string reachname,
            string itemname, mike11_restype itemtype, ResultData resdata, DateTime[] timearray)
        {
            Dictionary<DateTime, Dictionary<double, double>> all_grid_result = new Dictionary<DateTime, Dictionary<double, double>>();

            //找到指定的河道,注意有的河道分段了，要找到正确的河道河段
            IRes1DReach reach = resdata.Reaches.ElementAt(0);
            List<int> reach_id = new List<int>(); //包含该河道名字的河道序号集合
            for (int i = 0; i < resdata.Reaches.Count; i++)
            {
                reach = resdata.Reaches.ElementAt(i);
                if (reach.Name != reachname) continue;
                reach_id.Add(i);
            }

            ////获取项目编号，注意：hd_resultHDAdd.res11各河段是固定的，但ADDOUT.res1d每段都在变化
            //int itemnumber = Get_Itemnumber(itemname, reach);

            //各同名河道遍历
            List<Dictionary<DateTime, Dictionary<double, double>>> reach_reslist = new List<Dictionary<DateTime, Dictionary<double, double>>>();
            for (int n = 0; n < reach_id.Count; n++)
            {
                //每条该名称河道结果
                Dictionary<DateTime, Dictionary<double, double>> resdic = new Dictionary<DateTime, Dictionary<double, double>>();
                reach = resdata.Reaches.ElementAt(reach_id[n]);
                int itemnumber = Get_Itemnumber(itemname, reach);
                IDataItem data_item = reach.DataItems.ElementAt(itemnumber);

                //直接获取河道结果的二维数组，其中行为时间数量，列为计算点数量
                double[,] reach_res = data_item.CreateDataArray();

                //遍历水质、水位、水量二维结果数据
                resdic = ResSection_Select(reach, reach_res, timearray, itemtype);

                reach_reslist.Add(resdic);
            }


            //重新将该名字下的各河道片段的值进行合并
            for (int i = 0; i < timearray.Length; i++)
            {
                Dictionary<double, double> combine_dic = new Dictionary<double, double>();
                for (int j = 0; j < reach_reslist.Count; j++)
                {
                    Dictionary<DateTime, Dictionary<double, double>> res_dic = reach_reslist[j];
                    Dictionary<double, double> now_dic = res_dic.ElementAt(i).Value;
                    for (int k = 0; k < now_dic.Count; k++)
                    {
                        double chainage = Math.Round(now_dic.ElementAt(k).Key, 1);
                        double value = Math.Round(now_dic.ElementAt(k).Value, 3);
                        if (!combine_dic.Keys.Contains(chainage))
                        {
                            combine_dic.Add(chainage, value);
                        }
                    }
                }
                all_grid_result.Add(timearray[i], combine_dic);
            }

            return all_grid_result;
        }

        //获取项目编号
        private static int Get_Itemnumber(string itemname, IRes1DReach reach)
        {
            //找到项目编号
            int itemnumber = 0;
            IDataItem dataitem = reach.DataItems.ElementAt(0);
            for (int i = 0; i < reach.DataItems.Count; i++)
            {
                dataitem = reach.DataItems.ElementAt(i);
                if (string.Equals(dataitem.Quantity.Description, itemname, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                itemnumber++;
            }

            return itemnumber;
        }

        //遍历二维结果数据
        private static Dictionary<DateTime, Dictionary<double, double>> ResSection_Select(IRes1DReach reach, double[,] reach_res, DateTime[] timearray, mike11_restype itemtype)
        {
            Dictionary<DateTime, Dictionary<double, double>> resdic = new Dictionary<DateTime, Dictionary<double, double>>();
            int row_count = reach_res.GetLength(0);
            int field_count = reach_res.GetLength(1);

            //遍历行、列
            for (int i = 0; i < row_count; i++)
            {
                Dictionary<double, double> chainage_value = new Dictionary<double, double>();
                int n = itemtype == mike11_restype.Discharge ? 1 : 0;
                for (int j = 0; j < field_count; j++)
                {
                    double sec_chainage = reach.GridPoints.ElementAt(n).Chainage;
                    double value = reach_res[i, j];

                    chainage_value.Add(sec_chainage, value);
                    n += 2;  //如果不是得到全部计算点数据(Q\H点)，则加2次，去掉流量点处的数据，以与水位数据相同桩号
                    if (n >= reach.GridPoints.Count) break;
                }
                resdic.Add(timearray[i], chainage_value);
            }
            return resdic;
        }

        //查询所有河道全部的 滩地高程 数据(匹配水位纵剖点)
        public static Dictionary<string, List<double>> Get_AllReach_Td_ZdmData(HydroModel hydromodel, Dictionary<string, double[]> grids)
        {
            Dictionary<string, List<double>> allreach_td_zdmdata = new Dictionary<string, List<double>>();

            //首先从数据库获取所有河道的滩地高程
            Dictionary<string, Dictionary<double, double>> reach_td = Get_Reach_TdAltitude_FromDB();

            //遍历主河道
            for (int i = 0; i < grids.Count; i++)
            {
                string reachname = grids.ElementAt(i).Key;
                Dictionary<double, double> db_td = reach_td.Keys.Contains(reachname) ? reach_td[reachname] : null;
                double[] grid = grids.ElementAt(i).Value;
                List<double> td_zdmdata = Get_Td_ZdmData(hydromodel, reachname, grid, db_td);
                allreach_td_zdmdata.Add(reachname, td_zdmdata);
            }
            return allreach_td_zdmdata;
        }

        //从数据库获取所有河道的滩地高程
        public static Dictionary<string, Dictionary<double, double>> Get_Reach_TdAltitude_FromDB()
        {
            Dictionary<string, Dictionary<double, double>> res = new Dictionary<string, Dictionary<double, double>>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.BASE_SEC_B;
            

            //先判断该模型在数据库中是否存在
            string sqlstr = $"select reach_code,chainage,bot from {tableName} ORDER BY reach_code,chainage";
            DataTable dt = Mysql.query(sqlstr);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return null;

            //遍历记录
            string last_reach = dt.Rows[0][0].ToString();
            Dictionary<double, double> chainage_td = new Dictionary<double, double>();
            for (int i = 0; i < nLen; i++)
            {
                string reach_code = dt.Rows[i][0].ToString();
                if (reach_code != last_reach)
                {
                    Dictionary<double, double> reach_chainage_td = chainage_td;
                    res.Add(last_reach, reach_chainage_td);
                    chainage_td = new Dictionary<double, double>();
                }

                double chainage = double.Parse(dt.Rows[i][1].ToString());
                double td = double.Parse(dt.Rows[i][2].ToString());
                if (!chainage_td.Keys.Contains(chainage)) chainage_td.Add(chainage, td);

                last_reach = reach_code;
            }
            res.Add(last_reach, chainage_td);

            

            return res;
        }

        //查询所有河道全部的堤顶高程纵断面数据过程(匹配水位纵剖点)
        public static Dictionary<string, List<double>> Get_AllReach_Dd_ZdmData(HydroModel hydromodel, Dictionary<string, double[]> grids)
        {
            Dictionary<string, List<double>> dd_res = new Dictionary<string, List<double>>();
            Dictionary<string, List<List<double>>> allreach_ddqd_zdmdata = Get_AllReach_Ddqd_ZdmData(hydromodel, grids);

            //遍历主河道
            for (int i = 0; i < allreach_ddqd_zdmdata.Count; i++)
            {
                string reachname = allreach_ddqd_zdmdata.ElementAt(i).Key;
                List<double> dd_list = allreach_ddqd_zdmdata.ElementAt(i).Value[0];

                dd_res.Add(reachname, dd_list);
            }
            return dd_res;
        }

        //查询所有河道全部的渠底高程纵断面数据过程(匹配水位纵剖点)
        public static Dictionary<string, List<double>> Get_AllReach_Qd_ZdmData(HydroModel hydromodel, Dictionary<string, double[]> grids)
        {
            Dictionary<string, List<double>> qd_res = new Dictionary<string, List<double>>();
            Dictionary<string, List<List<double>>> allreach_ddqd_zdmdata = Get_AllReach_Ddqd_ZdmData(hydromodel, grids);

            //遍历主河道
            for (int i = 0; i < allreach_ddqd_zdmdata.Count; i++)
            {
                string reachname = allreach_ddqd_zdmdata.ElementAt(i).Key;
                List<double> qd_list = allreach_ddqd_zdmdata.ElementAt(i).Value[1];

                qd_res.Add(reachname, qd_list);
            }
            return qd_res;
        }

        //查询所有河道全部的渠底、堤顶高程纵断面数据过程(匹配水位纵剖点)
        public static Dictionary<string, List<List<double>>> Get_AllReach_Ddqd_ZdmData(HydroModel hydromodel, Dictionary<string, double[]> grids)
        {
            Dictionary<string, List<List<double>>> allreach_ddqd_zdmdata = new Dictionary<string, List<List<double>>>();

            //遍历主河道
            for (int i = 0; i < grids.Count; i++)
            {
                string reachname = grids.ElementAt(i).Key;
                double[] grid = grids.ElementAt(i).Value;
                List<List<double>> qdqd_zdmdata = Get_Ddqd_ZdmData(hydromodel, reachname, grid);
                allreach_ddqd_zdmdata.Add(reachname, qdqd_zdmdata);
            }
            return allreach_ddqd_zdmdata;
        }

        //查询指定河道全部的渠底、堤顶高程纵断面数据过程(匹配水位纵剖点)
        public static List<List<double>> Get_Ddqd_ZdmData(HydroModel hydromodel, string reachname, double[] grid)
        {
            List<List<double>> zdm_data_dic = new List<List<double>>();
            List<double> zdm_dddata_dic = new List<double>();
            List<double> zdm_qddata_dic = new List<double>();

            Dictionary<double, Max_Min_Z> section_ddqdvalue = hydromodel.Mike11Pars.SectionList.GetReach_AllSec_MaxminList(reachname);

            //重新匹配水位点，提取数据
            Max_Min_Z last_maxmin = section_ddqdvalue.ElementAt(0).Value;
            for (int i = 0; i < grid.Length; i++)
            {
                double chainage = grid[i];
                Max_Min_Z maxmin;
                if (section_ddqdvalue.Keys.Contains(chainage))
                {
                    maxmin = section_ddqdvalue[chainage];
                    last_maxmin = maxmin;
                }
                else
                {
                    maxmin = last_maxmin;
                }

                zdm_dddata_dic.Add(maxmin.max_z);
                zdm_qddata_dic.Add(maxmin.min_z);
            }

            zdm_data_dic.Add(zdm_dddata_dic);
            zdm_data_dic.Add(zdm_qddata_dic);
            return zdm_data_dic;
        }

        //查询指定河道全部的 滩地高程 纵断面数据(匹配水位纵剖点)
        public static List<double> Get_Td_ZdmData(HydroModel hydromodel, string reachname, double[] grid,
            Dictionary<double, double> db_td)
        {
            List<double> td_data_dic = new List<double>();

            SectionList section = hydromodel.Mike11Pars.SectionList;
            Dictionary<double, double> section_td = db_td == null ?
                section.GetReach_AllSec_TdList(reachname) : section.GetReach_AllSec_TdList(reachname, db_td);

            //重新匹配水位点，提取数据
            for (int i = 0; i < grid.Length; i++)
            {
                double chainage = grid[i];
                double td_gc;
                if (section_td.Keys.Contains(chainage))
                {
                    td_gc = section_td[chainage];
                }
                else
                {
                    //内插
                    td_gc = File_Common.Insert_Zd_Value(section_td, chainage, 2);
                }

                td_data_dic.Add(td_gc);
            }

            return td_data_dic;
        }

        //从水位纵剖数据中获取每条河道的网格点
        public static Dictionary<string, double[]> Get_Reach_GridChainages(Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> LevelZdmHD_Data)
        {
            Dictionary<string, double[]> grid_res = new Dictionary<string, double[]>();
            for (int i = 0; i < LevelZdmHD_Data.Count; i++)
            {
                string reach_name = LevelZdmHD_Data.ElementAt(i).Key;
                Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data = LevelZdmHD_Data.ElementAt(i).Value;
                double[] grid = ZdmHD_Data.ElementAt(0).Value.Keys.ToArray();
                grid_res.Add(reach_name, grid);
            }

            return grid_res;
        }

        //从水位纵剖数据中获取每条河道的网格点
        public static Dictionary<string, List<double>> Get_Reach_GridChainages1(Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> LevelZdmHD_Data)
        {
            Dictionary<string, List<double>> grid_res = new Dictionary<string, List<double>>();
            for (int i = 0; i < LevelZdmHD_Data.Count; i++)
            {
                string reach_name = LevelZdmHD_Data.ElementAt(i).Key;
                Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data = LevelZdmHD_Data.ElementAt(i).Value;
                List<double> grid = ZdmHD_Data.ElementAt(0).Value.Keys.ToList();
                grid_res.Add(reach_name, grid);
            }

            return grid_res;
        }

        //将Q点的流量纵段内插为H点的流量纵段
        public static Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Insert_Qzd_FromHzd(Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> level_zd,
            Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> discharge_zd)
        {
            Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> new_qzd = new Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>>();

            //遍历河道
            for (int i = 0; i < level_zd.Count; i++)
            {
                string reach_name = level_zd.ElementAt(i).Key;
                Dictionary<DateTime, Dictionary<double, double>> levels = level_zd.ElementAt(i).Value;
                Dictionary<DateTime, Dictionary<double, double>> q_zd = new Dictionary<DateTime, Dictionary<double, double>>();

                //时间遍历
                for (int j = 0; j < levels.Count; j++)
                {
                    DateTime time = levels.ElementAt(j).Key;
                    Dictionary<double, double> level_dic = levels[time];
                    Dictionary<double, double> discharge_dic = discharge_zd[reach_name][time];

                    Dictionary<double, double> new_q_dic = new Dictionary<double, double>();
                    for (int k = 0; k < level_dic.Count; k++)
                    {
                        double chainage = level_dic.ElementAt(k).Key;
                        double q;
                        if (discharge_dic.Keys.Contains(chainage))
                        {
                            q = discharge_dic[chainage];
                        }
                        else
                        {
                            //搜索与给定桩号最近的值
                            List<double> value_dic = discharge_dic.Keys.ToList();
                            int section_index = File_Common.Search_Value(value_dic, chainage, false);
                            q = discharge_dic.ElementAt(section_index).Value;
                        }
                        new_q_dic.Add(chainage, q);
                    }
                    q_zd.Add(levels.ElementAt(j).Key, new_q_dic);
                }

                new_qzd.Add(reach_name, q_zd);
            }

            return new_qzd;
        }

        //求纵断结果的时间最大值--保留桩号
        public static Dictionary<string, List<float>> Get_ZdRes_TimeMaxValue(Dictionary<string, Dictionary<DateTime, List<float>>> zd_data)
        {
            Dictionary<string, List<float>> res = new Dictionary<string, List<float>>();

            for (int i = 0; i < zd_data.Count; i++)
            {
                string reach_name = zd_data.ElementAt(i).Key;
                Dictionary<DateTime, List<float>> reach_zd = zd_data.ElementAt(i).Value;
                int reach_dmcount = reach_zd.Values.First().Count;

                List<float> max_res = new List<float>();
                for (int j = 0; j < reach_dmcount; j++)
                {
                    float maxNum = 0;
                    foreach (List<float> list in reach_zd.Values)
                    {
                        if (list[j] > maxNum) maxNum = list[j];
                    }

                    max_res.Add(maxNum);
                }
                res.Add(reach_name, max_res);
            }

            return res;
        }
        #endregion***********************************************
    }
}
