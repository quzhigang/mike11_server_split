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
        //查询渠道水量
        public static Dictionary<string, Dictionary<DateTime, double>> Get_Reach_Volume(HydroModel hydromodel)
        {
            Dictionary<string, Dictionary<DateTime, double>> reach_volumes = new Dictionary<string, Dictionary<DateTime, double>>();
            List<ReachInfo> main_reach = Item_Info.Get_MainReachInfo(Mysql_GlobalVar.now_instance);

            //遍历主河道
            for (int i = 0; i < main_reach.Count; i++)
            {
                Reach_Segment reachsegment = Reach_Segment.Get_ReachSegment(main_reach[i].reachname, main_reach[i].start_chainage, main_reach[i].end_chainage);
                Dictionary<DateTime, double> reach_volume = Get_ReachSegment_Volume(hydromodel, reachsegment);
                reach_volumes.Add(main_reach[i].reachname, reach_volume);
            }

            return reach_volumes;
        }

        //查询渠道水量时间序列结果(全过程)
        public static Dictionary<DateTime, double> Get_ReachSegment_Volume(HydroModel hydromodel, Reach_Segment reachsegment)
        {
            Dictionary<DateTime, double> reachsegment_volume = new Dictionary<DateTime, double>();

            //源文件和项目类型
            string sourcefilename = hydromodel.Modelfiles.Hd_addres11_filename;
            mike11_restype itemtype = mike11_restype.Volume;

            //河名
            string reachname = reachsegment.reachname;

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
                itemname = itemtype.ToString();
            }

            reachsegment_volume = Get_ReachSegment_ItemSumValue(hydromodel, sourcefilename, reachname, itemname, reachsegment);

            //移除前面提前的小时数
            Remove_Ahead_Res(hydromodel, ref reachsegment_volume);

            return reachsegment_volume;
        }

        //求指定项目 在指定河段内的总和过程，如水量过程
        public static Dictionary<DateTime, double> Get_ReachSegment_ItemSumValue(HydroModel hydromodel, string sourcefilename, string reachname, string itemname, Reach_Segment reachsegment)
        {
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();

            //新建ResultData对象，并将其连接属性赋值(用Connection类的Create方法)
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(sourcefilename);


            //必须得新建一个诊断对象，并给ResultData对象加载
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);

            //获取时间数组
            DateTime[] timearray = resdata.TimesList.ToArray();
            if (timearray.Last() != hydromodel.ModelGlobalPars.Simulate_time.End)
            {
                timearray = Get_ResTime_List(hydromodel);//修正时间，重新采用水位结果的时间
            }

            //找到指定的河道,注意有的河道分段了，要找到正确的河道河段
            IRes1DReach reach = resdata.Reaches.ElementAt(0);
            List<int> reach_id = new List<int>(); //包含该河道名字的河道序号集合
            for (int i = 0; i < resdata.Reaches.Count; i++)
            {
                reach = resdata.Reaches.ElementAt(i);

                double reachupchainage = reach.GetChainages(0)[0];
                double reachdownchainage = reach.GetChainages(0)[reach.GetChainages(0).Length - 1];

                //如果河名和桩号都能对上，且最小最大桩号在范围内，则找到了指定河道
                if (reach.Name == reachname && reachupchainage < reachsegment.end_chainage
                    && reachdownchainage > reachsegment.start_chainage)
                {
                    reach_id.Add(i);
                }
            }

            //找到指定的项目
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

            //各同名河道遍历
            List<Dictionary<DateTime, double>> reach_reslist = new List<Dictionary<DateTime, double>>();
            for (int n = 0; n < reach_id.Count; n++)
            {
                Dictionary<DateTime, double> sum_resdic = new Dictionary<DateTime, double>();  //求渠段内计算点值的总和

                reach = resdata.Reaches.ElementAt(reach_id[n]);
                IDataItem data_item = reach.DataItems.ElementAt(itemnumber);

                //直接获取河道结果的二维数组，其中行为时间数量，列为计算点数量
                double[,] reach_res = data_item.CreateDataArray();
                int row_count = reach_res.GetLength(0);
                int field_count = reach_res.GetLength(1);
                for (int i = 0; i < row_count; i++)
                {
                    double sum_value = 0;
                    for (int j = 0; j < field_count; j++)
                    {
                        double sec_chainage = reach.GridPoints.ElementAt(j).Chainage;
                        if (sec_chainage < reachsegment.start_chainage) continue;  //提高效率
                        if (sec_chainage > reachsegment.end_chainage) break;      //提高效率
                        sum_value += reach_res[i, j];
                    }

                    sum_resdic.Add(timearray[i], sum_value);
                }
                reach_reslist.Add(sum_resdic);
            }

            //重新将该名字下的各河道片段的值进行累加
            for (int i = 0; i < timearray.Length; i++)
            {
                double combine_value = 0;
                Dictionary<double, double> combine_dic = new Dictionary<double, double>();
                for (int j = 0; j < reach_reslist.Count; j++)
                {
                    Dictionary<DateTime, double> res_dic = reach_reslist[j];
                    combine_value += res_dic.ElementAt(i).Value;
                }
                result.Add(timearray[i], Math.Round(combine_value / 10000, 1)); //按万m3计算
            }

            return result;
        }

    }
}
