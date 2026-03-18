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
        #region ******************** dfs0文件修改 -- 部分替换、内插成任意时长等距 ************************
        //修改dfs0文件(包含已有的修改和没有的追加,按时间判断是修改还是,只能用于只有1个项目的dfs文件)
        public static int Dfs0_Edit(string sourcefilename, string outputfilename, Dictionary<DateTime, double> inputdic, bool reverse = false)
        {
            DateTime starttime = inputdic.ElementAt(0).Key;

            //打开文件，并判断是否为1个项目
            IDfsFile dfsfile = DfsFileFactory.DfsGenericOpenEdit(sourcefilename);
            if (dfsfile.ItemInfo.Count != 1)
            {
                dfsfile.Close();
                return -1;
            }
            dfsfile.Close();

            //调用自定义的静态方法返回指定项目的键值对集合
            Dictionary<DateTime, double> sourcedic = Dfs0_Reader_GetItemDic(sourcefilename, 1);

            //调用2个键值对集合相互内插修改的静态方法，返回新的键值对集合
            Dictionary<DateTime, double> newdic = reverse ? Getnewdic(inputdic, sourcedic) : Getnewdic(sourcedic, inputdic);

            //重新以只读方式打开
            dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

            //将原项目的项目信息提取出来创建新的DfsBuilder对象
            DfsBuilder dfsbuilder = Getdfsbder(sourcefilename, newdic.ElementAt(0).Key);

            //关闭并原文件
            dfsfile.Close();

            //根据DfsBuilder对象完成dfs文件的创建和数据写入
            if (Dfs0_Creat1(dfsbuilder, outputfilename, newdic) == -1)
            {
                Console.WriteLine("创建文件失败");
                return -1;
            }
            return 0;
        }

        //将dfs0内插成等距时间，比如1小时一步(适用于按步累积数值类型),不改变起止时间
        public static void Formatdfs(string sourcefilename, string outputfilename, TimeSpan inputtimespan)
        {
            //得到原dfs的键值对集合
            int itemnumber = 1;
            Dictionary<DateTime, double> resdic = Dfs0_Reader_GetItemDic(sourcefilename, itemnumber);

            //得到新的给定时间步长的键值对集合
            Dictionary<DateTime, double> newdic = Insert_Accutedic(resdic, inputtimespan);

            //以原dfs文件为模板生成新的dfs文件
            Dfs0_Creat(outputfilename, sourcefilename, newdic);
        }

        //将键值对集合内插成指定时间跨度的键值对集合--按固定值 细化或加大时间步长(步瞬时值类型)
        public static Dictionary<DateTime, double> Insert_Instantdic(Dictionary<DateTime, double> inputdic, TimeSpan inputtimespan)
        {
            Dictionary<DateTime, double> newdic = new Dictionary<DateTime, double>();

            //如果输入字典最大时间间距小于给定时间步长，不插值直接返回输入
            double all_timespan_seconds = inputdic.Last().Key.Subtract(inputdic.First().Key).TotalSeconds;
            if (all_timespan_seconds < inputtimespan.TotalSeconds) return inputdic;

            //新的时间序列
            List<DateTime> newdic_timelist = new List<DateTime>();
            int new_step_count = (int)(all_timespan_seconds / inputtimespan.TotalSeconds);
            for (int i = 0; i < new_step_count; i++)
            {
                DateTime time = inputdic.ElementAt(0).Key.AddSeconds(i * inputtimespan.TotalSeconds);
                if (!newdic_timelist.Contains(time)) newdic_timelist.Add(time);
            }
            if(!newdic_timelist.Contains(inputdic.Last().Key)) newdic_timelist.Add(inputdic.Last().Key);

            //逐步内插值
            for (int i = 0; i < newdic_timelist.Count; i++)
            {
                DateTime time = newdic_timelist[i];
                double value = 0;
                if (inputdic.Keys.Contains(time))
                {
                    value = inputdic[time];
                }
                else
                {
                    for (int j = 0; j < inputdic.Count -1; j++)
                    {
                        DateTime time1 = inputdic.ElementAt(j).Key;
                        DateTime time2 = inputdic.ElementAt(j +1).Key;

                        if (DateTime.Compare(time1,time)< 0 && DateTime.Compare(time2, time) > 0)
                        {
                            double value1 = inputdic.ElementAt(j).Value;
                            double value2 = inputdic.ElementAt(j + 1).Value;
                            value = inputdic.ElementAt(j).Value + (inputdic.ElementAt(j + 1).Value - inputdic.ElementAt(j).Value) * time.Subtract(time1).TotalSeconds/ time2.Subtract(time1).TotalSeconds;
                            break;
                        }
                    }
                }
                newdic.Add(time,value);
            }

            return newdic;
        }

        //将键值对集合内插成指定时间跨度的键值对集合--按固定值 细化或加大时间步长(步累积值类型)
        public static Dictionary<DateTime, double> Insert_Accutedic(Dictionary<DateTime, double> inputdic, TimeSpan inputtimespan)
        {
            //第1步，检查每一时间步，返回能被每一时间跨度整除的最大时间步长
            double timespan = Gettimespan(inputdic);

            //检查输入的时间跨度是否过小
            if (inputtimespan.TotalSeconds < timespan || Math.Abs(Math.Round(inputtimespan.TotalSeconds / timespan) - inputtimespan.TotalSeconds / timespan) != 0.0)
            {
                //求两个时间跨度的最大公约数
                timespan = Maxy((int)inputtimespan.TotalSeconds, (int)timespan);
            }

            //第2步,按能整除的最大时间步细化,生成新的键值对集合
            Dictionary<DateTime, double> newdic = new Dictionary<DateTime, double>();
            newdic.Add(inputdic.ElementAt(0).Key, inputdic.ElementAt(0).Value);
            for (int i = 0; i < inputdic.Count - 1; i++)
            {
                int stepnumber = (int)(inputdic.ElementAt(i + 1).Key.Subtract(inputdic.ElementAt(i).Key).TotalSeconds / timespan);
                for (int j = 0; j < stepnumber; j++)
                {
                    newdic.Add(inputdic.ElementAt(i).Key.AddSeconds(timespan * (j + 1)), inputdic.ElementAt(i + 1).Value / stepnumber);
                }
            }

            //第3步，分步累加至指定时间步长
            Dictionary<DateTime, double> newdic1 = new Dictionary<DateTime, double>();
            newdic1.Add(inputdic.ElementAt(0).Key, inputdic.ElementAt(0).Value);

            double timeaccumulate = 0; double valueaccumulate = 0;
            for (int i = 1; i < newdic.Count; i++)
            {
                timeaccumulate += timespan;
                valueaccumulate += newdic.ElementAt(i).Value;
                if (timeaccumulate == inputtimespan.TotalSeconds || i == newdic.Count - 1)
                {
                    newdic1.Add(newdic.ElementAt(i).Key, valueaccumulate);
                    timeaccumulate = 0;
                    valueaccumulate = 0;
                }
            }
            return newdic1;
        }

        //将键值对集合内插成指定时间跨度的键值对集合--按固定值 细化或加大时间步长(适用于瞬时数值类型)
        public static Dictionary<DateTime, double> Getdic1(Dictionary<DateTime, double> inputdic, TimeSpan inputtimespan)
        {
            //第1步，检查每一时间步，返回能被每一时间跨度整除的最大时间步长
            double timespan = Gettimespan(inputdic);
            double source_timestep_seconds = inputdic.ElementAt(1).Key.Subtract(inputdic.ElementAt(0).Key).TotalSeconds;

            //检查输入的时间跨度是否过小
            if (inputtimespan.TotalSeconds < timespan || Math.Abs(Math.Round(inputtimespan.TotalSeconds / timespan) - inputtimespan.TotalSeconds / timespan) != 0.0)
            {
                //求两个时间跨度的最大公约数
                timespan = Maxy((int)inputtimespan.TotalSeconds, (int)timespan);
            }

            //第2步,按能整除的最大时间步细化,生成新的键值对集合
            Dictionary<DateTime, double> newdic = new Dictionary<DateTime, double>();
            newdic.Add(inputdic.ElementAt(0).Key, inputdic.ElementAt(0).Value);
            for (int i = 0; i < inputdic.Count - 1; i++)
            {
                int stepnumber = (int)(inputdic.ElementAt(i + 1).Key.Subtract(inputdic.ElementAt(i).Key).TotalSeconds / timespan);
                for (int j = 0; j < stepnumber; j++)
                {
                    DateTime time = inputdic.ElementAt(i).Key.AddSeconds(timespan * (j + 1));
                    double value1 = inputdic.ElementAt(i).Value; double value2 = inputdic.ElementAt(i + 1).Value;
                    double insert_value = value1 + (value2 - value1) * (time.Subtract(inputdic.ElementAt(i).Key).TotalSeconds) / (source_timestep_seconds);
                    double value = inputdic.Keys.Contains(time) ? inputdic[time] : insert_value;
                    newdic.Add(time, Math.Round(value, 2));
                }
            }

            return newdic;
        }


        //从键值对集合中截取指定时期内的键值对集合
        public static Dictionary<DateTime, double> Get_Subdic(Dictionary<DateTime, double> inputdic, DateTime starttime, DateTime endtime)
        {
            Dictionary<DateTime, double> subdic = new Dictionary<DateTime, double>();

            if (starttime.CompareTo(inputdic.Keys.ElementAt(0)) >= 0 && endtime.CompareTo(inputdic.Keys.ElementAt(inputdic.Count - 1)) <= 0)
            {
                double startvalue = 0;
                double endvalue = 0;
                double value = 0;
                for (int i = 0; i < inputdic.Count - 1; i++)
                {
                    if (starttime.CompareTo(inputdic.Keys.ElementAt(i)) >= 0 && starttime.CompareTo(inputdic.Keys.ElementAt(i + 1)) <= 0)
                    {
                        startvalue = (inputdic.Values.ElementAt(i) + inputdic.Values.ElementAt(i + 1)) / 2;
                        break;
                    }
                }

                for (int i = 0; i < inputdic.Count - 1; i++)
                {
                    if (endtime.CompareTo(inputdic.Keys.ElementAt(i)) >= 0 && endtime.CompareTo(inputdic.Keys.ElementAt(i + 1)) <= 0)
                    {
                        endvalue = (inputdic.Values.ElementAt(i) + inputdic.Values.ElementAt(i + 1)) / 2;
                        break;
                    }
                }

                //加上起段元素
                subdic.Add(starttime, startvalue);

                for (int i = 0; i < inputdic.Count - 1; i++)
                {
                    //加上中间元素
                    if (starttime.CompareTo(inputdic.Keys.ElementAt(i)) < 0 && endtime.CompareTo(inputdic.Keys.ElementAt(i)) > 0)
                    {
                        value = inputdic.Values.ElementAt(i);
                        subdic.Add(inputdic.Keys.ElementAt(i), value);
                    }
                }

                //加上末端元素
                if (!subdic.Keys.Contains(endtime))
                {
                    subdic.Add(endtime, endvalue);
                }
            }
            else
            {
                Console.WriteLine("指定时期不完全在目标键值对集合日期范围内！");
            }

            return subdic;
        }

        //检查每一时间步，扫描从1小时开始，到10分钟、1分钟、10秒、1秒，返回能被所有时间步跨度整除的最大时间步长
        public static double Gettimespan(Dictionary<DateTime, double> resdic)
        {
            //判断指定时间跨度(1小时)能不能被每一步时间跨度整除
            double timespan = 3600.0;
            if (Steptimespan_timespan(resdic, timespan) == false)
            {
                timespan = 600.0;
            }

            //如果上一个指定时间跨度(1小时)不能被每一步时间跨度整除，进入下一个指定时间跨度(10分钟)判断
            if (Steptimespan_timespan(resdic, timespan) == false)
            {
                timespan = 60.0;
            }

            //如果上一个指定时间跨度(10分钟)不能被每一步时间跨度整除，进入下一个指定时间跨度(1分钟)判断
            if (Steptimespan_timespan(resdic, timespan) == false)
            {
                timespan = 10.0;
            }

            //如果上一个指定时间跨度(1分钟)不能被每一步时间跨度整除，进入下一个指定时间跨度(10s)判断
            if (Steptimespan_timespan(resdic, timespan) == false)
            {
                timespan = 1.0;
            }

            return timespan;
        }

        //判断指定时间跨度(如1小时)能不能被每一步时间跨度整除
        public static bool Steptimespan_timespan(Dictionary<DateTime, double> resdic, double timespan)
        {
            TimeSpan steptimespan;
            for (int i = 0; i < resdic.Count - 1; i++)
            {
                steptimespan = resdic.ElementAt(i + 1).Key.Subtract(resdic.ElementAt(i).Key);
                if (Math.Abs(Math.Round(steptimespan.TotalSeconds / timespan) - steptimespan.TotalSeconds / timespan) > 0)  //不能被整除
                {
                    return false;
                }
            }
            return true;
        }

        //延长时间
        public static Dictionary<string, Dictionary<DateTime, double>> Add_Hours_InheadDic(Dictionary<string, Dictionary<DateTime, double>> inflow_dic, int ahead_hours)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_dic = new Dictionary<string, Dictionary<DateTime, double>>();
            if (inflow_dic == null) return res_dic;
            for (int i = 0; i < inflow_dic.Count; i++)
            {
                string catchment_id = inflow_dic.ElementAt(i).Key;
                Dictionary<DateTime, double> q_dic = inflow_dic.ElementAt(i).Value;
                if (ahead_hours != 0)
                {
                    DateTime starttime = q_dic.First().Key.Subtract(new TimeSpan(ahead_hours, 0, 0));
                    q_dic = Dfs0.Dfs0_AddToStart_Usevalue0(q_dic, starttime);
                }

                res_dic.Add(catchment_id, q_dic);
            }

            return res_dic;
        }

        //裁剪时间
        public static Dictionary<string, Dictionary<DateTime, double>> Del_Hours_InheadDic(Dictionary<string, Dictionary<DateTime, double>> inflow_dic, DateTime start_time)
        {
            Dictionary<string, Dictionary<DateTime, double>> res_dic = new Dictionary<string, Dictionary<DateTime, double>>();
            if (inflow_dic == null) return res_dic;
            for (int i = 0; i < inflow_dic.Count; i++)
            {
                string catchment_id = inflow_dic.ElementAt(i).Key;
                Dictionary<DateTime, double> q_dic = inflow_dic.ElementAt(i).Value;
                Dictionary<DateTime, double> q_dic1 = new Dictionary<DateTime, double>();
                if (start_time.Subtract(q_dic.ElementAt(0).Key).TotalMinutes >= 0)
                {
                    for (int j = 0; j < q_dic.Count; j++)
                    {
                        if (q_dic.ElementAt(j).Key.Subtract(start_time).TotalMinutes >= 0) q_dic1.Add(q_dic.ElementAt(j).Key, q_dic.ElementAt(j).Value);
                    }
                }
                else
                {
                    q_dic1 = q_dic;
                }

                res_dic.Add(catchment_id, q_dic1);
            }

            return res_dic;
        }

        //创建恒定数值 指定时长键值对集合
        public static Dictionary<DateTime, double> Get_ConstValue_Dic(DateTime start_time, DateTime end_time, TimeSpan inputtimespan, double value)
        {
            Dictionary<DateTime, double> dic = new Dictionary<DateTime, double>();
            dic.Add(start_time, value);
            dic.Add(end_time, value);

            Dictionary<DateTime, double> dic1 = Insert_Instantdic(dic, inputtimespan);
            return dic1;
        }

        //内插时间序列过程,根据比值
        public static Dictionary<DateTime, double> Inser_DicGc(Dictionary<DateTime, double> min_dic, Dictionary<DateTime, double> max_dic, double bz)
        {
            Dictionary<DateTime, double> res = new Dictionary<DateTime, double>();

            //判断各种错误
            if (min_dic == null || max_dic == null ) return null;
            if (min_dic.Count == 0 || max_dic.Count == 0 || min_dic.Count != max_dic.Count ) return res;

            //逐时间步内插值
            for (int i = 0; i < min_dic.Count; i++)
            {
                double dic_value;
                double min_value = min_dic.ElementAt(i).Value;
                double max_value = max_dic.ElementAt(i).Value;

                if (max_value == min_value)
                {
                    dic_value = min_value;
                }
                else if(max_value == 0)
                {
                    dic_value = 0;
                }
                else
                {
                    dic_value = min_value + (max_value - min_value) * bz;
                }

                res.Add(min_dic.ElementAt(i).Key, dic_value);
            }

            return res;
        }
        
        //对比两个时间序列，计算出比值
        public static Dictionary<DateTime,double> Get_Dic1_Dic2_Bz(Dictionary<DateTime, double> min_dic, Dictionary<DateTime, double> max_dic)
        {
            Dictionary<DateTime, double> res = new Dictionary<DateTime, double>();

            //判断各种错误
            if (min_dic == null || max_dic == null ) return null;
            if (min_dic.Count == 0 || max_dic.Count == 0  || min_dic.Count != max_dic.Count ) return res;

            //逐时间计算比值
            for (int i = 0; i < min_dic.Count; i++)
            {
                double bz = max_dic.ElementAt(i).Value == 0?1:min_dic.ElementAt(i).Value/ max_dic.ElementAt(i).Value;
                res.Add(max_dic.ElementAt(i).Key, bz);
            }

            return res;
        }

        //错动dfs0的数值时间
        public static Dictionary<DateTime, double> Modify_Dic_ChangeHours(Dictionary<DateTime, double> inflow_dic, double hours)
        {
            Dictionary<DateTime, double> res_dic = new Dictionary<DateTime, double>();

            // 处理空字典情况
            if (inflow_dic == null || inflow_dic.Count == 0) return inflow_dic;

            // 获取有序的时间序列
            List<DateTime> sortedTimes = inflow_dic.Keys.ToList();

            // 计算时间间隔（小时）
            double intervalHours = (sortedTimes[1] - sortedTimes[0]).TotalHours;

            // 计算实际移动步数（取整除的整数）
            int steps = (int)(hours / intervalHours);

            // 遍历每个时间点
            for (int i = 0; i < sortedTimes.Count; i++)
            {
                DateTime currentTime = sortedTimes[i];
                int sourceIndex = i - steps;
                double value = 0;

                // 检查源索引是否在有效范围内
                if (sourceIndex >= 0 && sourceIndex < sortedTimes.Count)
                {
                    value = inflow_dic[sortedTimes[sourceIndex]];
                }

                res_dic.Add(currentTime, value);
            }

            return res_dic;
        }

        //对前段一定时间范围内的数据进行曲线修正(S曲线)
        public static Dictionary<DateTime, double> Modify_DicFront_WithSmoothCurve(Dictionary<DateTime, double> old_dic,double value,double dur_hours)
        {
            // 直接获取第一个键作为开始时间
            var keys = old_dic.Keys.ToList();
            DateTime startTime = keys[0];

            // 计算调整结束时间
            DateTime endTime = startTime.AddHours(dur_hours);

            // 获取结束时间的原始值
            double endValue = old_dic[endTime];

            // 创建新字典
            var newSeries = new Dictionary<DateTime, double>();

            // 使用for循环处理每个时间点
            for (int i = 0; i < keys.Count; i++)
            {
                DateTime currentTime = keys[i];
                double originalValue = old_dic[currentTime];

                if (currentTime <= endTime)
                {
                    // 计算时间比例t（0到1之间）
                    double t = (currentTime - startTime).TotalHours / dur_hours;

                    // 使用余弦插值计算平滑因子
                    double smoothFactor = (1 - Math.Cos(Math.PI * t)) / 2;

                    // 计算新值
                    double newValue = value + (endValue - value) * smoothFactor;
                    newSeries[currentTime] = Math.Round(newValue,2);
                }
                else
                {
                    newSeries[currentTime] = originalValue;
                }
            }

            return newSeries;
        }

        #endregion ****************************************************************************************
    }
}
