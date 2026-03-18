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
        #region ****************** 其他公用键值对集合<DateTime, double>处理函数 ****************************
        //获取键值对集合<datetime，double>的最大值以及出现的时间
        public static void Dfs0_Max(Dictionary<DateTime, double> resdic, out double max, out DateTime max_time)
        {
            //直接求得最大值
            max = resdic.Values.Max();
            //先初始化
            max_time = DateTime.Now;
            foreach (KeyValuePair<DateTime, double> kv in resdic)
            {
                if (kv.Value.Equals(max))
                {
                    max_time = kv.Key;
                    return;
                }
            }
        }

        //获取键值对集合<datetime，double>的最小值和出现的时间
        public static void Dfs0_Min(Dictionary<DateTime, double> resdic, out double min, out DateTime min_time)
        {
            //直接求得最小值
            min = resdic.Values.Min();
            //先初始化
            min_time = DateTime.Now;
            foreach (KeyValuePair<DateTime, double> kv in resdic)
            {
                if (kv.Value.Equals(min))
                {
                    min_time = kv.Key;
                    return;
                }
            }
        }

        //求键值对集合序列中指定跨度总和值最大的开始索引值
        public static int Getmax_start_index(Dictionary<DateTime, double> inputdic, int number)
        {
            int n = 0;
            double maxsum = 0;
            double temmax = 0;
            for (int i = 0; i < inputdic.Count - number + 1; i++)
            {
                for (int j = i; j < i + number; j++)
                {
                    maxsum += inputdic.ElementAt(j).Value;
                }

                if (maxsum > temmax)
                {
                    temmax = maxsum;
                    n = i;
                }
                maxsum = 0;
            }
            return n;
        }

        //求两个整数的最大公约数
        public static int Maxy(int first, int second)
        {
            int max = first > second ? first : second;
            int min = first < second ? first : second;
            int r = max % min;
            if (r == 0)
            {
                return min;
            }
            else
            {
                while (r != 0)
                {
                    max = min;
                    min = r;
                    r = max % min;
                }
                return min;
            }
        }

        //返回键值对集合的键数组(DateTime类型)
        public static DateTime[] Getkey(Dictionary<DateTime, double> inputdic)
        {
            DateTime[] keyarray = new DateTime[inputdic.Count];
            int n = 0;
            foreach (var item in inputdic.Keys)
            {
                keyarray[n] = item;
                n++;
            }
            return keyarray;
        }

        //返回键值对集合的值数组(double类型)
        public static double[] Getvalue(Dictionary<DateTime, double> inputdic)
        {
            double[] valuearray = new double[inputdic.Count];
            int n = 0;
            foreach (var item in inputdic.Values)
            {
                valuearray[n] = item;
                n++;
            }
            return valuearray;
        }

        //从txt文件中读取键值对集合
        public static Dictionary<DateTime, double> GetDic_FromDicFile(string filepath)
        {
            Dictionary<DateTime, double> data = new Dictionary<DateTime, double>();

            string[] str_row = File.ReadAllLines(filepath);
            for (int i = 0; i < str_row.Length; i++)
            {
                string[] read_column = str_row[i].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);  //把每一行的字符串按空格分隔

                DateTime dt = DateTime.Parse(read_column[0]);
                double d = double.Parse(read_column[1]);

                data.Add(dt, d);
            }
            return data;
        }

        //以一个累积总量值和起止时间,创建键值对集合(水量 - 流量)(非等距)
        public static Dictionary<DateTime, double> CreateDic_FromAccumulatedValue(SimulateTime simulatetime, DateTime start_time, DateTime end_time, double accumulated_value) 
        {
            //根据水量求平均流量
            double ave_q = accumulated_value /(end_time.Subtract(start_time).TotalSeconds);
            Dictionary<DateTime, double> dic = new Dictionary<DateTime, double>();
            dic.Add(simulatetime.Begin, 0);

            //开始结束时间按0.5分钟错位
            dic.Add(start_time, 0);
            dic.Add(start_time.AddMinutes(0.5), ave_q);
            dic.Add(end_time, ave_q);
            dic.Add(end_time.AddMinutes(0.5), 0);
            dic.Add(simulatetime.End, 0);

            return dic;
        }

        //一个固定值和起止时间创建键值对集合(水质浓度)(非等距)
        public static Dictionary<DateTime, double> CreateDic_FromInstantValue(SimulateTime simulatetime, DateTime start_time, DateTime end_time, double instant_value)
        {
            //开始结束时间按1分钟错位
            Dictionary<DateTime, double> dic = new Dictionary<DateTime, double>();
            dic.Add(simulatetime.Begin, 0);
            dic.Add(start_time, 0);
            dic.Add(start_time.AddMinutes(0.5), instant_value);
            dic.Add(end_time, instant_value);
            dic.Add(end_time.AddMinutes(0.5), 0);
            dic.Add(simulatetime.End, 0);

            return dic;
        }

        //获取水质泄漏的开始和结束时刻
        public static void Get_StartEndTime(string sourcefilename,out DateTime start_time,out DateTime end_time)
        {
            start_time = DateTime.Now;
            end_time = DateTime.Now;

            //得到原dfs的键值对集合
            int itemnumber = 1;
            Dictionary<DateTime, double> resdic = Dfs0_Reader_GetItemDic(sourcefilename, itemnumber);

            for (int i = 0; i < resdic.Count; i++)
            {
                if(resdic.ElementAt(i).Value != 0.0)
                {
                    start_time = resdic.ElementAt(i - 1).Key;
                    end_time = resdic.ElementAt(i + 1).Key;
                    break;
                }
            }
        }

        //创建单一值的dic
        public static Dictionary<DateTime, double> GetDic_SingleValueDic(List<DateTime> time_list, double value)
        {
            Dictionary<DateTime, double> data = new Dictionary<DateTime, double>();

            for (int i = 0; i < time_list.Count; i++)
            {
                DateTime dt = time_list[i];
                data.Add(dt, value);
            }
            return data;
        }


        //根据瞬时时间序列获取总量值，用于计算污染源总量累积体积m3
        public static double Get_Accumulated_Value(string sourcefilename)
        {
            //得到原dfs的键值对集合
            int itemnumber = 1;
            Dictionary<DateTime, double> resdic = Dfs0_Reader_GetItemDic(sourcefilename, itemnumber);

            double acculatevalue = 0.0;
            for (int i = 1; i < resdic.Count; i++)
            {
                DateTime start_time = resdic.ElementAt(i - 1).Key;
                DateTime end_time = resdic.ElementAt(i).Key;
                double sec_count = end_time.Subtract(start_time).TotalSeconds;
                double ave_value = (resdic.ElementAt(i - 1).Value + resdic.ElementAt(i).Value)/2;
                acculatevalue += sec_count * ave_value;
            }
            return acculatevalue;
        }

        //将二维数组变成交错数组
        public static int[][] Getarray(int[,] inputarray)
        {
            int[][] array = new int[inputarray.GetLength(0)][];
            for (int i = 0; i < inputarray.GetLength(0); i++)
            {
                array[i] = new int[inputarray.GetLength(1)];
                for (int j = 0; j < array[i].Length; j++)
                {
                    array[i][j] = inputarray[i, j];
                }
            }
            return array;
        }
        
        #endregion
    }
}
