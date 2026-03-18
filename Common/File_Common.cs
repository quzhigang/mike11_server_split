using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using bjd_model.Const_Global;
using bjd_model.Mike11;
using bjd_model.Mike21;
using Newtonsoft.Json;

namespace bjd_model.Common
{
    public class File_Common
    {
        #region ****************** 其他公用文件处理函数 ****************************
        //获取模型文件字节数组
        public static byte[] Get_Filebyte(string filename)
        {
            //获取文件大小信息
            FileInfo fileinfo = new FileInfo(filename);
            long last_file = fileinfo.Length;

            //1、创建一个负责读取的流
            using (FileStream fsread = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read))
            {
                byte[] buffer = new byte[last_file];

                //因为文件可能会比较大，所以我们在读取的时候，应该通过一个循环区读取
                int n = fsread.Read(buffer, 0, buffer.Length); //返回读取到的字节数

                return buffer;
            }
        }

        //把一个文件夹下的所有文件复制到另一个文件下下
        public static void Copy_Directory_Allfile(string source_dic, string dest_dic)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(source_dic);

                //获取目录下的文件和子目录数组（不含子文件）
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();

                foreach (FileSystemInfo info in fileinfo)
                {
                    if (info is DirectoryInfo)  //如果是文件夹
                    {

                    }
                    else
                    {
                        //true表示可覆盖同名文件
                        File.Copy(info.FullName, dest_dic + @"\" + info.Name, true);
                    }
                }
            }
            catch (Exception er)
            {
                throw;
            }
        }

        //下载模型文件服务端部分 -- 从服务端得到字节数组，一个字节数组对应一个模型文件，其中string为带扩展名的文件名
        public static Dictionary<string, byte[]> Get_ModelFile_Byte(string source_dir)
        {
            Dictionary<string, byte[]> modelfile_byte = new Dictionary<string, byte[]>();

            //搜索所有的本文件夹下和子文件夹下的文件，返回文件路径
            string[] filename_array = Directory.GetFiles(source_dir, "*.*");

            //遍历文件路径，将各文件序列化为字节
            for (int i = 0; i < filename_array.Length; i++)
            {
                byte[] file_byte = Get_Filebyte(filename_array[i]);

                modelfile_byte.Add(filename_array[i], file_byte);
            }

            return modelfile_byte;
        }

        //下载模型及结果文件服务端部分
        public static Dictionary<string, byte[]> Get_AllFile_Byte(string source_dir)
        {
            Dictionary<string, byte[]> allfile_byte = new Dictionary<string, byte[]>();

            //搜索所有的本文件夹下和子文件夹下的文件，返回文件路径
            string[] filename_array = Directory.GetFiles(source_dir, "*.*", SearchOption.AllDirectories);

            //遍历文件路径，将各文件序列化为字节
            for (int i = 0; i < filename_array.Length; i++)
            {
                byte[] file_byte = Get_Filebyte(filename_array[i]);

                allfile_byte.Add(filename_array[i], file_byte);
            }

            return allfile_byte;
        }

        //将时序二维结果写入文本(*****速度慢,慎用!!****)
        public static void Writedic_intotxt(string outputfilename, Dictionary<DateTime, List<Valuexy>> inputdic)
        {
            try
            {
                int diccount = inputdic.Count;
                int[] listcount = new int[diccount];
                for (int i = 0; i < diccount; i++)
                {
                    listcount[i] = inputdic.ElementAt(i).Value.Count;
                }

                for (int i = 0; i < diccount; i++)
                {
                    List<Valuexy> reslist = inputdic.ElementAt(i).Value;
                    File.AppendAllText(outputfilename, inputdic.ElementAt(i).Key.ToString("yyyy-MM-dd hh:mm:ss") + "\r\n");
                    Valuexy[] resvalue = reslist.ToArray();
                    for (int j = 0; j < listcount[i]; j++)
                    {
                        StringBuilder strbuilder = new StringBuilder();
                        strbuilder.Append(resvalue[j].number);
                        strbuilder.Append(" ");
                        strbuilder.Append(Math.Round(resvalue[j].X, 3));
                        strbuilder.Append(" ");
                        strbuilder.Append(Math.Round(resvalue[j].Y, 3));
                        strbuilder.Append(" ");
                        strbuilder.Append(Math.Round(resvalue[j].Z, 2));
                        strbuilder.Append(" ");
                        strbuilder.Append(Math.Round(resvalue[j].value, 2));
                        File.AppendAllText(outputfilename, strbuilder.ToString() + "\r\n");
                    }
                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
        }

        //将模型字节数组转换成模型文件，如果有结果文件则会新建新的结果文件夹，并把结果文件也转换出来
        public static void Down_Files(Dictionary<string, byte[]> modelfile_byte, string dest_dir)
        {
            //根据模型文件字节数组反序列化成为模型文件
            string old_modeldir = Path.GetDirectoryName(modelfile_byte.Keys.ElementAt(0));
            string old_resultdir = Path.GetDirectoryName(modelfile_byte.Keys.ElementAt(0)) + @"\" + "results";
            for (int i = 0; i < modelfile_byte.Count; i++)
            {
                //文件路径
                string filename;
                if (Path.GetDirectoryName(modelfile_byte.Keys.ElementAt(i)) == old_modeldir)
                {
                    filename = dest_dir + @"\" + Path.GetFileName(modelfile_byte.Keys.ElementAt(i));
                }
                else
                {
                    //如果结果文件夹不存在，则创建一个
                    if (!Directory.Exists(dest_dir + @"\" + "results"))
                    {
                        Directory.CreateDirectory(dest_dir + @"\" + "results");
                    }
                    filename = dest_dir + @"\" + "results" + @"\" + Path.GetFileName(modelfile_byte.Keys.ElementAt(i));
                }

                //反序列化成模型文件和模型结果文件
                using (FileStream fswrite = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] buffer = modelfile_byte.Values.ElementAt(i);
                    fswrite.Write(buffer, 0, buffer.Length);
                }
            }
            Console.WriteLine("模型文件下载完成!");
        }

        //判断文件是否正在使用
        public static bool File_IsUsing(string file)
        {
            bool is_using = true;

            FileStream fs = null;
            try
            {
                fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
                is_using = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null) fs.Close();
            }
            return is_using;
        }

        //较高效序列化对象,将对象转为json字符串
        public static string Serializer_Obj(Object obj,string datetime_format = "yyyy/MM/dd HH:mm:ss")
        {
            string json_str = null;
            try
            {
                var settings = new JsonSerializerSettings
                {
                    DateFormatString = datetime_format
                };

                json_str = JsonConvert.SerializeObject(obj, settings);
            }
            catch (Exception)
            {
            }
            return json_str;
        }

        //序列化位字节数组
        public static byte[] Serializer_ObjByte(Object obj)
        {
            MemoryStream ms1 = new MemoryStream();   //将模型流域集合类序列化后写入数据库
            BinaryFormatter binFormat1 = new BinaryFormatter();//创建二进制序列化器
            binFormat1.Serialize(ms1, obj);  //将对象序列化
            byte[] buffer = obj == null ? null : ms1.ToArray();  //转换成字节数组
            ms1.Close();
            ms1.Dispose();
            return buffer;
        }

        //从文件夹中找到制定类型最大的文件
        public static string Get_MaxFile(string source_dir, string extend = ".dfsu")
        {
            string[] files = Directory.GetFiles(source_dir, "*" + extend);

            long max_b = 0; string max_file = files[0];
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fileinfo = new FileInfo(files[i]);
                long fileb = fileinfo.Length;
                if (fileb > max_b)
                {
                    max_b = fileb; max_file = files[i];
                }
            }
            return max_file;
        }

        //从文件夹中找到制定类型最小的文件
        public static string Get_MinFile(string source_dir, string extend = ".dfsu")
        {
            string[] files = Directory.GetFiles(source_dir, "*" + extend);

            long min_b = (new FileInfo(files[0])).Length;
            string min_file = files[0];
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fileinfo = new FileInfo(files[i]);
                long fileb = fileinfo.Length;
                if (fileb < min_b)
                {
                    min_b = fileb; min_file = files[i];
                }
            }
            return min_file;
        }

        //找到包含指定类型数据的子文件夹
        public static string Get_SubDir(string source_dir, string extend = ".dfsu")
        {
            string[] dirs = Directory.GetDirectories(source_dir);
            for (int i = 0; i < dirs.Length; i++)
            {
                string[] files = Directory.GetFiles(dirs[i], "*" + extend);
                if (files.Length != 0) return dirs[i];
            }
            return "";
        }

        #endregion *************************************************************


        #region ******************一些常用算法 *********************************
        //二分查找法，求指定值的序号(值集必须从小到大排序) -- 数组
        public static int Search_Value(double[] array, double value)
        {
            int tou = 0;
            int wei = array.Length - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (array[zhong] == value)
                {
                    return zhong;
                }
                else if (array[zhong] > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }
            return -1;
        }

        //二分查找法，求指定值的序号(值集必须从小到大排序) -- 集合
        public static int Search_Value(List<double> value_list, double value)
        {
            int tou = 0;
            int wei = value_list.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_list[zhong] == value)
                {
                    return zhong;
                }
                else if (value_list[zhong] > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }
            return -1;
        }

        //二分查找法，求指定值的序号(值集必须从小到大排序) -- 键值对(根据值找键)
        public static int Search_Value(Dictionary<double, double> value_dic, double value)
        {
            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic.ElementAt(zhong).Value == value)
                {
                    return zhong;
                }
                else if (value_dic.ElementAt(zhong).Value > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }
            return -1;
        }

        //二分查找法(适合从小到大排序的键值对)，求与给定值最近的值(可限定获取的值在该值前面)
        public static int Search_Value(Dictionary<string, double> value_dic, double value,bool limit_small = false)
        {
            int min_i = 0;

            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic.ElementAt(zhong).Value == value)
                {
                    return zhong;
                }
                else if (value_dic.ElementAt(zhong).Value > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            double min_distance = 10000000;
            if (wei < 1 || tou > value_dic.Count -1 ) return -1;
            for (int i = wei -1; i < tou +1; i++)
            {
                double now_distance = Math.Abs(value - value_dic.ElementAt(i).Value);
                if(now_distance < min_distance)
                {
                    min_distance = now_distance;
                    min_i = i;
                }
            }

            //如果限定了获取的I值所对应的value值要小于指定值(在前面)
            if(limit_small && value_dic.ElementAt(min_i).Value > value)
            {
                min_i--;
            }
            return min_i;
        }

        //二分查找法(适合从小到大排序的键值对)，求与给定值最近的值(不含等于该值的，可限定获取的值在该值前面，即小于该值，如寻找闸前最近的断面)
        public static int Search_Value(Dictionary<double, double> value_dic, double value, bool limit_small = false)
        {
            int min_i = 0;

            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic.ElementAt(zhong).Key > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            double min_distance = 10000000;
            if (wei < 1 || tou > value_dic.Count - 1) return -1;
            for (int i = wei - 1; i < tou + 1; i++)
            {
                double now_distance = Math.Abs(value - value_dic.ElementAt(i).Key);
                if (now_distance < min_distance)
                {
                    min_distance = now_distance;
                    min_i = i;
                }
            }

            //如果限定了获取的I值所对应的value值要小于指定值(在前面)
            if (limit_small && value_dic.ElementAt(min_i).Key >= value)
            {
                if (value_dic.ElementAt(min_i - 1).Key == value && min_i >=2)
                {
                    min_i = min_i -2;
                }
                else if (min_i >= 1)
                {
                    min_i--;
                }
            }

            return min_i;
        }

        //二分查找法(适合从小到大排序的键值对)，求与给定值最近的值(不含等于该值的，可限定获取的值在该值前面，即小于该值，如寻找闸前最近的断面)
        public static int Search_Value(List<AtReach> value_dic, double value, bool limit_small = false)
        {
            int min_i = 0;

            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic[zhong].chainage > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            double min_distance = 10000000;
            if (wei < 1 || tou > value_dic.Count - 1) return tou < 0 ? 0 : Math.Min(tou, value_dic.Count - 1); ;
            for (int i = wei - 1; i < tou + 1; i++)
            {
                double now_distance = Math.Abs(value - value_dic[i].chainage);
                if (now_distance < min_distance)
                {
                    min_distance = now_distance;
                    min_i = i;
                }
            }

            //如果限定了获取的I值所对应的value值要小于指定值(在前面)
            if (limit_small && value_dic[min_i].chainage >= value)
            {
                if (value_dic[min_i -1].chainage == value && min_i >= 2)
                {
                    min_i = min_i - 2;
                }
                else if (min_i >= 1)
                {
                    min_i--;
                }
            }

            return min_i < 0 ? 0 : Math.Min(min_i, value_dic.Count - 1);
        }

        //二分查找法(适合从小到大排序的键值对)，求与给定值最近的值(不含等于该值的，可限定获取的值在该值前面，即小于该值)
        public static int Search_Value(List<ReachPoint> value_dic, double value, bool limit_small = false)
        {
            int min_i = 0;

            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic[zhong].pointchainage > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            double min_distance = 10000000;
            if (wei < 1 || tou > value_dic.Count - 1) return -1;
            for (int i = wei - 1; i < tou + 1; i++)
            {
                double now_distance = Math.Abs(value - value_dic[i].pointchainage);
                if (now_distance < min_distance)
                {
                    min_distance = now_distance;
                    min_i = i;
                }
            }

            //如果限定了获取的I值所对应的value值要小于指定值(在前面)
            if (limit_small && value_dic[min_i].pointchainage >= value)
            {
                if (value_dic[min_i - 1].pointchainage == value && min_i >= 2)
                {
                    min_i = min_i - 2;
                }
                else if (min_i >= 1)
                {
                    min_i--;
                }
            }

            return min_i;
        }

        //二分查找法(适合从小到大排序的键值对)，求与给定值最近的值(不含等于该值的，可限定获取的值在该值前面，即小于该值，如寻找闸前最近的断面)
        public static int Search_Value(List<double> value_dic, double value, bool limit_small = false)
        {
            int min_i = 0;

            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic[zhong] > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            double min_distance = 10000000;
            if (wei < 1 || tou > value_dic.Count - 1) return tou < 0?0: Math.Min(tou, value_dic.Count - 1);
            for (int i = wei - 1; i < tou + 1; i++)
            {
                double now_distance = Math.Abs(value - value_dic[i]);
                if (now_distance < min_distance)
                {
                    min_distance = now_distance;
                    min_i = i;
                }
            }

            //如果限定了获取的I值所对应的value值要小于指定值(在前面)
            if (limit_small && value_dic[min_i] >= value)
            {
                if (value_dic[min_i - 1] == value && min_i >= 2)
                {
                    min_i = min_i - 2;
                }
                else if (min_i >= 1)
                {
                    min_i--;
                }
            }

            return min_i<0?0:Math.Min(min_i, value_dic.Count -1);
        }

        //二分查找法(适合从小到大排序的键值对)，求与给定值最近的值(不含等于该值的，可限定获取的值在该值前面，即小于该值，如寻找闸前最近的断面)
        public static int Search_Value(List<float> value_dic, float value, bool limit_small = false)
        {
            int min_i = 0;

            int tou = 0;
            int wei = value_dic.Count - 1;
            int zhong = (tou + wei) / 2;
            while (tou <= wei)
            {
                if (value_dic[zhong] > value)
                {
                    wei = zhong - 1;
                    zhong = (tou + wei) / 2;
                }
                else
                {
                    tou = zhong + 1;
                    zhong = (tou + wei) / 2;
                }
            }

            double min_distance = 10000000;
            if (wei < 1 || tou > value_dic.Count - 1) return tou < 0 ? 0 : Math.Min(tou, value_dic.Count - 1);
            for (int i = wei - 1; i < tou + 1; i++)
            {
                double now_distance = Math.Abs(value - value_dic[i]);
                if (now_distance < min_distance)
                {
                    min_distance = now_distance;
                    min_i = i;
                }
            }

            //如果限定了获取的I值所对应的value值要小于指定值(在前面)
            if (limit_small && value_dic[min_i] >= value)
            {
                if (value_dic[min_i - 1] == value && min_i >= 2)
                {
                    min_i = min_i - 2;
                }
                else if (min_i >= 1)
                {
                    min_i--;
                }
            }

            return min_i < 0 ? 0 : Math.Min(min_i, value_dic.Count - 1);
        }

        //内插
        public static float Insert_Zd_Value(Dictionary<float, float> zd_dic, float section)
        {
            //前一个最近的点
            List<float> value_dic = zd_dic.Keys.ToList();
            int for_i = File_Common.Search_Value(value_dic, section, true);
            float for_value = zd_dic.ElementAt(for_i).Value;

            //下一个
            int next_i = (for_i + 1) >= zd_dic.Count - 1 ? zd_dic.Count - 1 : for_i + 1;
            float next_value = zd_dic.ElementAt(next_i).Value;

            //内插值
            float value = PointXY.Insert_double(zd_dic.ElementAt(for_i).Key, zd_dic.ElementAt(next_i).Key, for_value, next_value, section);

            return value;
        }


        //根据库容曲线，从水位内插库容
        public static Dictionary<DateTime, double> Insert_ResVolumn(Dictionary<DateTime, double> level_dic, Dictionary<double, double> res_qhrelation)
        {
            Dictionary<DateTime, double> volumn_dic = new Dictionary<DateTime, double>();
            for (int i = 0; i < level_dic.Count; i++)
            {
                double volumn = File_Common.Insert_Zd_Value(res_qhrelation, level_dic.ElementAt(i).Value,1);
                volumn_dic.Add(level_dic.ElementAt(i).Key,volumn);
            }

            return volumn_dic;
        }

        //内插
        public static double Insert_Zd_Value(Dictionary<double, double> res_qhrelation, double insertkey,int ws = -1)
        {
            double res = 0;

            // 二分查找insertkey在字典中的位置
            int index = Array.BinarySearch(res_qhrelation.Keys.ToArray(), insertkey);

            // 如果精确匹配，则直接返回对应value值
            if (index >= 0)
            {
                res = ws == -1 ? res_qhrelation.Values.ElementAt(index) : Math.Round(res_qhrelation.Values.ElementAt(index), ws);
                return res;
            }
            else
            {
                // 获取上下限的key值
                int lower = ~index - 1;
                int upper = ~index;

                // 如果插入位置在字典范围内，则进行线性内插计算
                if (lower >= 0 && upper < res_qhrelation.Keys.Count)
                {
                    double x1 = res_qhrelation.Keys.ElementAt(lower);
                    double x2 = res_qhrelation.Keys.ElementAt(upper);
                    double y1 = res_qhrelation.Values.ElementAt(lower);
                    double y2 = res_qhrelation.Values.ElementAt(upper);
                    res = y1 + (y2 - y1) * (insertkey - x1) / (x2 - x1);
                }
            }
            if (ws != -1) res = Math.Round(res,ws);

            return res;
        }

        //反向内插
        public static double Insert_Zd_Key(Dictionary<double, double> res_qhrelation, double insert_value)
        {
            double res = 0;

            // 二分查找insertkey在字典中的位置
            int index = Array.BinarySearch(res_qhrelation.Values.ToArray(), insert_value);

            // 如果精确匹配，则直接返回对应value值
            if (index >= 0)
            {
                return res_qhrelation.Keys.ElementAt(index);
            }
            else
            {
                // 获取上下限的key值
                int lower = ~index - 1;
                int upper = ~index;

                // 如果插入位置在字典范围内，则进行线性内插计算
                if (lower >= 0 && upper < res_qhrelation.Keys.Count)
                {
                    double x1 = res_qhrelation.Values.ElementAt(lower);
                    double x2 = res_qhrelation.Values.ElementAt(upper);
                    double y1 = res_qhrelation.Keys.ElementAt(lower);
                    double y2 = res_qhrelation.Keys.ElementAt(upper);
                    res = y1 + (y2 - y1) * (insert_value - x1) / (x2 - x1);
                }
            }

            return res;
        }

        //内插,上下游顺延且相同
        public static double Insert_Zd_Value1(Dictionary<double, double> res_qhrelation, double insertkey)
        {
            double res = 0;

            // 二分查找insertkey在字典中的位置
            int index = Array.BinarySearch(res_qhrelation.Keys.ToArray(), insertkey);

            // 如果精确匹配，则直接返回对应value值
            if (index >= 0)
            {
                return res_qhrelation.Values.ElementAt(index);
            }
            else
            {
                // 获取上下限的key值
                int lower = ~index - 1;
                int upper = ~index;

                // 如果插入位置在字典范围内，则进行线性内插计算
                if (insertkey <= res_qhrelation.ElementAt(0).Key)
                {
                    res = res_qhrelation.ElementAt(0).Value;
                }
                else if (insertkey >= res_qhrelation.ElementAt(res_qhrelation.Count - 1).Key)
                {
                    res = res_qhrelation.ElementAt(res_qhrelation.Count - 1).Value;
                }
                else if (lower >= 0 && upper < res_qhrelation.Keys.Count)
                {
                    double x1 = res_qhrelation.Keys.ElementAt(lower);
                    double x2 = res_qhrelation.Keys.ElementAt(upper);
                    double y1 = res_qhrelation.Values.ElementAt(lower);
                    double y2 = res_qhrelation.Values.ElementAt(upper);
                    res = y1 + (y2 - y1) * (insertkey - x1) / (x2 - x1);
                }
            }

            return res;
        }

        //获取线程信息
        public static void Get_ThreadInfo(string process_name)
        {
            while (true)
            {
                //线程数据
                Process[] pro = Process.GetProcessesByName("process_name");
                for (int i = 0; i < pro.Length; i++)
                {
                    Console.WriteLine("**************************************");

                    Console.WriteLine("进程名称:{0} 进程的线程数{1}", pro[i].ProcessName, pro[i].Threads.Count);
                    for (int j = 0; j < pro[i].Threads.Count; j++)
                    {
                        Console.WriteLine("线程ID{0}", pro[i].Threads[j].Id);
                    }

                    Console.WriteLine("**************************************");
                }
                Thread.Sleep(2000);
            }
        }

        //获取桩号
        public static string Get_ChainageStr(string qz, double chainage)
        {
            string chainage_end = "000" + Math.Round((chainage - Math.Floor(chainage / 1000) * 1000.0), 0).ToString();
            string chainage_str = qz + Math.Floor(chainage / 1000).ToString() + "+" + chainage_end.Substring(chainage_end.Length - 3);
            return chainage_str;
        }

        //读取文本
        public static string Read_FileContent(string sourcefilename)
        {
            if (!File.Exists(sourcefilename)) return "";

            string content = "";

            //文本读取
            using (StreamReader sr = new StreamReader(sourcefilename, Encoding.Default))
            {
                //使用StringBuilder类处理字符串速度更快
                StringBuilder strbuilder = new StringBuilder();
                while (!sr.EndOfStream)
                {
                    //在strbuilder对象内添加文本的一行字符串
                    strbuilder.Append(sr.ReadLine());
                }
                content = strbuilder.ToString();
            }

            return content;
        }

        //写入文本
        public static string Write_FileContent(string file_path, string content_data)
        {
            if (File.Exists(file_path)) File.Delete(file_path);

            //写入流文件，使用using()这个语法，系统会自动关闭流并释放资源
            using (System.IO.FileStream fswrite = new System.IO.FileStream(file_path, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] buffer = Encoding.Default.GetBytes(content_data);//使用Encoding按指定方法将拿到的字节解码成字节数组
                fswrite.Write(buffer, 0, buffer.Length);
            }
            return file_path;
        }

        //追加写入文本
        public static string AppendWrige_FileContent(string file_path, string content_data)
        {
            //写入流文件，使用using()这个语法，系统会自动关闭流并释放资源
            using (System.IO.FileStream fswrite = new System.IO.FileStream(file_path, FileMode.Append, FileAccess.Write))
            {
                byte[] buffer = Encoding.Default.GetBytes(content_data);//使用Encoding按指定方法将拿到的字节解码成字节数组
                fswrite.Write(buffer, 0, buffer.Length);
            }
            return file_path;
        }
        #endregion *******************************************************************


        #region *************************网络请求等**********************************
        // 网络接口请求通用方法，Get请求(同步请求)
        public static string Get_HttpReSponse(string url)
        {
            string res = "";
            try
            {
                // 创建一个WebRequest对象
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                // 设置请求方法
                request.Method = "GET" ;

                // 获取响应
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // 读取响应内容
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                    res = reader.ReadToEnd();
                }

                // 关闭响应
                response.Close();
            }
            catch (Exception)
            {
                return res;
                throw;
            }

            return res;
        }

        // 网络接口请求通用方法，Post请求(同步请求)
        public static string Post_HttpReSponse(string url, string request_parsjson)
        {
            // 发送POST请求
            using (var client = new HttpClient())
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent(request_parsjson, Encoding.UTF8, "application/json");
                var response = client.SendAsync(requestMessage).Result;
                string response_str = response.Content.ReadAsStringAsync().Result;
                return response_str;
            }
        }

        //开辟新进程，执行python脚本
        public static void RunPythonScript(string py_path, params string[] par_strs)
        {
            Process p = new Process();

            string sArguments = py_path;
            foreach (string par in par_strs)
            {
                sArguments += " " + par;//传递参数
            }

            p.StartInfo.FileName = Mysql_GlobalVar.PYTHONEXE_PATH;
            p.StartInfo.Arguments = sArguments;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = false;

            p.Start();
        }


        #endregion ******************************************************************



    }

}
