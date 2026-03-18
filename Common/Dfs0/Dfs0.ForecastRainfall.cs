using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.IO;

using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike11;
using bjd_model.Mike21;
using bjd_model;


namespace bjd_model.Common
{
    public partial class Dfs0
    {
        #region *********** 根据未来预报降雨量和时长，采用模板或指定时间序列补齐未来降雨过程 **************

        //根据未来预报降雨量、未来预报降雨时长、典型24小时降雨过程模板预测降雨过程,start_index_eq0为false则自动选择值总和为最大的集合
        public static Dictionary<DateTime, double> Creat_Rfdic_future(Dictionary<DateTime, double> template_dic, bool start_index_eq0, TimeSpan future_timespan, double totalrain)
        {
            //定义开始时间(到小时)
            DateTime starttime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            //根据dfs模板、总降雨、未来降雨时长、开始时间生成键值对序列
            Dictionary<DateTime, double> resdic = Getdic(template_dic, start_index_eq0, totalrain, future_timespan, starttime);

            //返回dic
            return resdic;
        }

        //根据未来预报降雨量、未来预报降雨时长、用户指定降雨过程模板预测降雨过程
        public static Dictionary<DateTime, double> Creat_Rfdic_future(Dictionary<DateTime, double> futuredic, double totalrain)
        {
            Dictionary<DateTime, double> Rfdic_future = futuredic;

            //首先判断给定的未来键值对集合值总和是否等于总降雨，不等则按总降雨重新分配
            if (Rfdic_future.Values.Sum() != totalrain)
            {
                double xs = totalrain / Rfdic_future.Values.Sum();
                for (int i = 0; i < Rfdic_future.Count; i++)
                {
                    Rfdic_future[futuredic.ElementAt(i).Key] = Rfdic_future.ElementAt(i).Value * xs;
                }
            }

            //返回dic
            return Rfdic_future;
        }

        //根据已降雨过程(键值对集合)、未来预报降雨量和时长、典型降雨过程模板补齐降雨过程---降雨进行时的降雨过程预测
        public static Dictionary<DateTime, double> Create_Rfdic_addfuture(Dictionary<DateTime, double> template_dic, bool start_index_eq0, Dictionary<DateTime, double> agodic, TimeSpan future_timespan, double totalrain)
        {
            //用新的timespan套模板得到完整的时间数据键值对集合
            Dictionary<DateTime, double> resdic = Creat_Rfdic_future(template_dic, start_index_eq0, future_timespan, totalrain);

            //用原键值对集合重新修改新生成的键值对集合
            Dictionary<DateTime, double> newdic = resdic;
            if (agodic.Count != 1)
            {
                newdic = Getnewdic(resdic, agodic);
            }

            //新增的future序列值总和会小于给定的，故得求出还需要增加的倍数
            double syptxs = totalrain / (newdic.Values.Sum() - agodic.Values.Sum());

            //修改增加的future部分键值对序列的值
            for (int i = 0; i < newdic.Count; i++)
            {
                if (newdic.ElementAt(i).Key > agodic.ElementAt(agodic.Count - 1).Key)
                {
                    newdic[newdic.ElementAt(i).Key] = newdic.ElementAt(i).Value * syptxs;
                    //Console.WriteLine("{0}  {1}", newdic.ElementAt(i).Key, newdic.ElementAt(i).Value);
                }
            }

            //返回新dic
            return newdic;
        }

        //根据已降雨过程(键值对集合)、未来预报降雨量和时长、指定未来键值对集合补齐降雨过程---降雨进行时的降雨过程预测
        public static Dictionary<DateTime, double> Create_Rfdic_addfuture(Dictionary<DateTime, double> agodic, Dictionary<DateTime, double> future_dictemplate, double totalrain)
        {
            //首先判断给定的未来键值对集合值总和是否等于总降雨，不等则按总降雨重新分配
            Dictionary<DateTime, double> futuredic = Creat_Rfdic_future(future_dictemplate, totalrain);

            //过去和未来的键值对集合叠加生成新的,如果未来的与过去有重合，会被过去的覆盖(因为过去的是实测的)
            Dictionary<DateTime, double> newdic;
            if (agodic.Count != 1)
            {
                newdic = Getnewdic(futuredic, agodic);
            }
            else
            {
                newdic = futuredic;
            }

            //返回新dic
            return newdic;
        }

        //根据输入的键值对集合对原来的进行修改,返回新的键值对集合
        //包含前端追加，后端追加以及各种交错重叠情况
        public static Dictionary<DateTime, double> Getnewdic(Dictionary<DateTime, double> sourcedic, Dictionary<DateTime, double> inputdic)
        {
            //初始化原集合和新集合的特征值
            DateTime source_starttime = sourcedic.ElementAt(0).Key;
            DateTime source_endtime = sourcedic.ElementAt(sourcedic.Count - 1).Key;
            DateTime input_starttime = inputdic.ElementAt(0).Key;
            DateTime input_endtime = inputdic.ElementAt(inputdic.Count - 1).Key;

            //创建新的集合用于存储
            Dictionary<DateTime, double> newdic = new Dictionary<DateTime, double>();

            //两集合没有交集的情况
            if (input_starttime > source_endtime) //位于原集合后，相当于后段追加
            {
                for (int i = 0; i < sourcedic.Count; i++)
                {
                    if (!newdic.Keys.Contains(sourcedic.ElementAt(i).Key))
                        newdic.Add(sourcedic.ElementAt(i).Key, sourcedic.ElementAt(i).Value);
                }

                for (int i = 0; i < inputdic.Count; i++)
                {
                    if (!newdic.Keys.Contains(inputdic.ElementAt(i).Key))
                        newdic.Add(inputdic.ElementAt(i).Key, inputdic.ElementAt(i).Value);
                }
                return newdic;
            }
            else if (input_endtime < source_starttime) //位于原集合前，相当于前段追加
            {
                for (int i = 0; i < inputdic.Count; i++)
                {
                    newdic.Add(inputdic.ElementAt(i).Key, inputdic.ElementAt(i).Value);
                }

                for (int i = 0; i < sourcedic.Count; i++)
                {
                    newdic.Add(sourcedic.ElementAt(i).Key, sourcedic.ElementAt(i).Value);
                }
                return newdic;
            }

            //两集合有交集的情况
            if (input_starttime >= source_starttime && input_starttime <= source_endtime) //如果输入集合的开始时间大于原集合的开始时间(*时间类型可判断大小*)
            {
                if (input_endtime >= source_endtime) //如果输入集合结束时间也大于原集合的结束时间
                {
                    //修改和后部追加
                    //第1步 保存前面剩下的元素
                    for (int i = 0; i < sourcedic.Count; i++)
                    {
                        if (sourcedic.ElementAt(i).Key >= inputdic.ElementAt(0).Key)
                        {
                            break;
                        }
                        newdic.Add(sourcedic.ElementAt(i).Key, sourcedic.ElementAt(i).Value);
                    }
                    //第2步 把新的接上
                    for (int i = 0; i < inputdic.Count; i++)
                    {
                        newdic.Add(inputdic.ElementAt(i).Key, inputdic.ElementAt(i).Value);
                    }
                    return newdic;
                }
                else if (input_endtime >= source_starttime) //被完全包住
                {
                    //中段修改
                    //第1步 保存前面剩下的元素
                    int startindex = 0;
                    for (int i = 0; i < sourcedic.Count; i++)
                    {
                        if (sourcedic.ElementAt(i).Key >= inputdic.ElementAt(0).Key)
                        {
                            startindex = i;
                            break;
                        }
                        newdic.Add(sourcedic.ElementAt(i).Key, sourcedic.ElementAt(i).Value);
                    }

                    //第2步 增加中间新的元素
                    for (int i = 0; i < inputdic.Count; i++)
                    {
                        if (!newdic.Keys.Contains(inputdic.ElementAt(i).Key))
                            newdic.Add(inputdic.ElementAt(i).Key, inputdic.ElementAt(i).Value);
                    }

                    //第3步 增加后面的元素
                    for (int i = startindex; i < sourcedic.Count; i++)
                    {
                        if (sourcedic.ElementAt(i).Key > input_endtime)
                        {
                            if (!newdic.Keys.Contains(sourcedic.ElementAt(i).Key))
                                newdic.Add(sourcedic.ElementAt(i).Key, sourcedic.ElementAt(i).Value);
                        }
                    }
                    return newdic;
                }
            }
            else       //如果输入集合的开始时间小于原集合的开始时间
            {
                if (input_endtime >= source_endtime)
                {
                    //相当于完全替换
                    return inputdic;
                }
                else if (input_endtime >= source_starttime)      //前半包
                {
                    //前部追加和修改
                    //第1步 把新的全部加上
                    for (int i = 0; i < inputdic.Count; i++)
                    {
                        newdic.Add(inputdic.ElementAt(i).Key, inputdic.ElementAt(i).Value);
                    }

                    //第2步 把原集合后面剩下的元素加上
                    for (int i = 0; i < sourcedic.Count; i++)
                    {
                        if (sourcedic.ElementAt(i).Key > input_endtime)
                        {
                            newdic.Add(sourcedic.ElementAt(i).Key, sourcedic.ElementAt(i).Value);
                        }
                    }
                    return newdic;
                }
            }
            return newdic;
        }

        //根据预报的总雨量、时间跨度(如未来12小时降雨100mm)、模板生成键值对集合,给定的时间跨度可大于模板时间跨度，模板自动重复延长
        //若start_index_eq0为false，则套模板时自动选取其中总和值最大的序列，否则强行从0开始
        public static Dictionary<DateTime, double> Getdic(Dictionary<DateTime, double> template_dic, bool start_index_eq0, double total, TimeSpan timespan, DateTime starttime)
        {
            //创建键值对集合
            Dictionary<DateTime, double> resdic = new Dictionary<DateTime, double>();
            resdic.Add(starttime, 0);

            //最后一个参数若为0，则返回模板中指定跨度值总和最大的键值对系数序列,否则按最后一个参数的位置开始截取序列
            Dictionary<int, double> intdic = Getintdic(template_dic, start_index_eq0, timespan); //最后一个参数为0表示不硬性指定位置

            //根据总雨量和键值对系数系列添加其他键值对元素
            double timestepspan = timespan.TotalSeconds / intdic.Count;
            for (int i = 0; i < intdic.Count; i++)
            {
                resdic.Add(starttime.AddSeconds((i + 1) * timestepspan), intdic.ElementAt(i).Value * total);
            }
            return resdic;
        }

        //从大时间段(如24小时)雨型中提取小时间段(如12、6、3小时或>1小时 的任意时段)雨形--取最大值段,返回键值对集合<int,double>
        public static Dictionary<int, double> Getintdic(Dictionary<DateTime, double> template_dic, bool start_index_eq0, TimeSpan time)
        {
            Dictionary<int, double> intdic = new Dictionary<int, double>();
            Dictionary<DateTime, double> resdic = template_dic;

            //求输入的时间跨度是模板键值对集合第1、2个元素时间差的几倍
            double inputtimestep = resdic.ElementAt(1).Key.Subtract(resdic.ElementAt(0).Key).TotalSeconds;
            int number = (int)(time.TotalSeconds / inputtimestep);

            //判断是否满足截取要求:模板时间跨度大于给定，如果小于，则模板自动重复延长
            double inputtimespan = resdic.ElementAt(resdic.Count - 1).Key.Subtract(resdic.ElementAt(0).Key).TotalSeconds;
            int max_start_index;
            if (inputtimespan < time.TotalSeconds)
            {
                //最多循环20次应该够了
                for (int i = 0; i < 20; i++)
                {
                    DateTime dic_endtime = resdic.ElementAt(resdic.Count - 1).Key;
                    int dic_count = resdic.Count;
                    for (int j = 1; j < dic_count; j++)
                    {
                        resdic.Add(dic_endtime.AddSeconds(j * inputtimestep), resdic.ElementAt(j).Value);
                    }

                    inputtimespan = resdic.ElementAt(resdic.Count - 1).Key.Subtract(resdic.ElementAt(0).Key).TotalSeconds;
                    if (inputtimespan > time.TotalSeconds)
                    {
                        break;
                    }
                }
                //如果是这种情况，开始索引就是0
                max_start_index = 0;
            }
            else
            {
                //调用方法求键值对集合序列中指定跨度总和值最大的开始索引值
                max_start_index = Getmax_start_index(resdic, number);
            }

            //根据用户选择，可强行指定开始索引为0
            if (start_index_eq0 == true)
            {
                max_start_index = 0;
            }

            //写入新的键值对集合
            int valueindex = max_start_index;
            for (int i = 0; i < number; i++)
            {
                intdic.Add(i, resdic.ElementAt(valueindex).Value);
                valueindex++;
            }
            //归一化重新修改值
            double valuesum = intdic.Values.Sum();
            for (int i = 0; i < intdic.Count; i++)
            {
                if (valuesum != 0.0)
                {
                    intdic[i] = intdic.ElementAt(i).Value / valuesum;
                }
            }
            return intdic;
        }

        //用0值补齐后面时间序列，步长为1小时
        public static Dictionary<DateTime, double> Dfs0_AddToEND_Usevalue0(Dictionary<DateTime, double> source_dic, DateTime endtime)
        {
            Dictionary<DateTime, double> fulldic = new Dictionary<DateTime, double>();

            //创建后半段时间序列
            Dictionary<DateTime, double> enddic = new Dictionary<DateTime, double>();
            TimeSpan timespan = endtime.Subtract(source_dic.Keys.ElementAt(source_dic.Count - 1));
            for (int i = 0; i < (int)(timespan.TotalHours + 1); i++)
            {
                DateTime inserttime = source_dic.Keys.ElementAt(source_dic.Count - 1).AddHours(i);
                enddic.Add(inserttime, 0);
            }

            //如果后半段序列结束时间 不等于最终结束时间
            if (enddic.Keys.ElementAt(enddic.Count - 1) != endtime)
            {
                enddic.Add(endtime, 0);
            }

            //将前后合并
            fulldic = Dfs0.Getnewdic(source_dic, enddic);
            return fulldic;
        }

        //用0值补齐前面时间序列，步长为1小时
        public static Dictionary<DateTime, double> Dfs0_AddToStart_Usevalue0(Dictionary<DateTime, double> source_dic, DateTime starttime)
        {
            Dictionary<DateTime, double> fulldic = new Dictionary<DateTime, double>();

            //创建前半段时间序列
            Dictionary<DateTime, double> dic = new Dictionary<DateTime, double>();
            TimeSpan timespan = source_dic.Keys.First().Subtract(starttime);
            dic.Add(starttime.AddHours(-1), 0);
            for (int i = 0; i < (int)(timespan.TotalHours); i++)
            {
                DateTime inserttime = starttime.AddHours(i);
                dic.Add(inserttime, 0);
            }

            //将前后合并
            fulldic = Dfs0.Getnewdic(dic, source_dic);
            return fulldic;
        }

        //对两个相同长度的字典进行值的合并
        public static Dictionary<DateTime, double> Combine_Dic(Dictionary<DateTime, double> dic1, Dictionary<DateTime, double> dic2)
        {
            if (dic1 == null && dic2 == null) return null;
            if (dic1 == null || dic1.Count == 0) return dic2;
            if (dic2 == null || dic2.Count == 0) return dic1;

            //如果起点不同，取共有部分
            if (dic1.First().Key.Subtract(dic2.First().Key).TotalSeconds > 0.0)  //第1个开始时间在第2个后面,即第2个有多余
            {
                for (int i = 0; i < dic2.Count; i++)
                {
                    DateTime time = dic2.ElementAt(i).Key;
                    if (!dic1.Keys.Contains(time))
                    {
                        dic2.Remove(time);
                        i--;
                    }
                }
            }
            else if (dic1.First().Key.Subtract(dic2.First().Key).TotalSeconds < 0.0)
            {
                for (int i = 0; i < dic1.Count; i++)
                {
                    DateTime time = dic1.ElementAt(i).Key;
                    if (!dic2.Keys.Contains(time))
                    {
                        dic1.Remove(time);
                        i--;
                    }
                }
            }

            //如果终点点不同，取最大部分，剩余的补充0
            if (dic1.Last().Key.Subtract(dic2.Last().Key).TotalSeconds > 0.0)  //第1个结束时间在第2个后面,即第1个有多余
            {
                for (int i = 0; i < dic1.Count; i++)
                {
                    DateTime time = dic1.ElementAt(i).Key;
                    if (!dic2.Keys.Contains(time))
                    {
                        dic2.Add(time, 0);
                    }
                }
            }
            else if (dic1.Last().Key.Subtract(dic2.Last().Key).TotalSeconds < 0.0)
            {
                for (int i = 0; i < dic2.Count; i++)
                {
                    DateTime time = dic2.ElementAt(i).Key;
                    if (!dic1.Keys.Contains(time))
                    {
                        dic1.Add(time, 0);
                    }
                }
            }

            Dictionary<DateTime, double> res = new Dictionary<DateTime, double>();
            if (dic1.Count != dic2.Count) return res;
            for (int i = 0; i < dic1.Count; i++)
            {
                res.Add(dic1.ElementAt(i).Key, dic1.ElementAt(i).Value + dic2.ElementAt(i).Value);
            }

            return res;
        }

        //对多个相同长度的字典进行值的合并
        public static Dictionary<DateTime, double> Combine_Dic(List<Dictionary<DateTime, double>> dics)
        {
            if (dics.Count == 0) return null;
            if (dics.Count == 1) return dics[0];

            Dictionary<DateTime, double> out_dic = new Dictionary<DateTime, double>();

            int dictCount = dics.Count;
            for (int i = 0; i < dictCount; i++)
            {
                Dictionary<DateTime, double> dict = dics[i];
                int kvCount = dict.Count;
                KeyValuePair<DateTime, double>[] kvs = dict.ToArray();
                for (int j = 0; j < kvCount; j++)
                {
                    var kvp = kvs[j];
                    if (out_dic.ContainsKey(kvp.Key))
                    {
                        out_dic[kvp.Key] += kvp.Value;
                    }
                    else
                    {
                        out_dic[kvp.Key] = kvp.Value;
                    }
                }
            }
            return out_dic;
        }

        //合并2个项目
        public static Dictionary<string, Dictionary<DateTime, double>> Combine_Two_Item(Dictionary<string, Dictionary<DateTime, double>> dic1,
            Dictionary<string, Dictionary<DateTime, double>> dic2)
        {
            Dictionary<string, Dictionary<DateTime, double>> res = new Dictionary<string, Dictionary<DateTime, double>>();

            for (int i = 0; i < dic1.Count; i++)
            {
                res.Add(dic1.ElementAt(i).Key, dic1.ElementAt(i).Value);
            }

            for (int i = 0; i < dic2.Count; i++)
            {
                res.Add(dic2.ElementAt(i).Key, dic2.ElementAt(i).Value);
            }

            return res;
        }
        #endregion ****************************************************************************************
    }
}
