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
using bjd_model;


namespace bjd_model.Common
{
    //预报降雨模板套用类型
    [Serializable]
    public enum forecast_rfmodeltype
    {
        default_model,  //默认模板
        typedef_dic     //自定义过程序列
    }

    //几种常用dfs0序列文件
    [Serializable]
    public enum dfs0type
    {
        waterlevel,   //水位
        discharge,    //流量

        rainfall,     //降雨
        evaporation,  //蒸发

        water_depth,   //水深
        water_vector,   //流速

        hydrograph,       //水文单位线
        concentration,    //水质浓度

        gatelevel        //闸门高度
    }

    //时间类型
    [Serializable]
    public enum timetype
    {
        Equidistant_Calendar = 1,    //等距绝对时间
        Equidistant_relative = 2,   //等距相对时间
        Non_Equidistant_Calendar = 3,   //不等距绝对时间
        Non_Equidistant_relative = 4   //不等距相对时间
    }

    //头信息数据类型
    [Serializable]
    public enum headdatetype
    {
        dfs2_mike21 = -1,
        dfs0_1_2_Mikezero = 0,
        dfsu_mikeFm = 2000,
    }

    //创建DFS文件参数
    [Serializable]
    public struct Dfscreateprs
    {
        public Dfscreate_headprs headprs;    //头参数
        public Dfscreate_itemprs itemprs;    //项目参数
    }

    //头信息
    [Serializable]
    public struct Dfscreate_headprs
    {
        //文件标题和所用版本
        public string filetitle;
        public string apptitle;
        public int appversion;

        //地图投影  
        public IDfsProjection project;

        //时间轴定义 
        public eumUnit timeunit;  //时间轴的时间单位
        public DateTime starttime;      //开始时间
        public timetype dfstimety;     //时间类型
        public double timesteps;             //时间步
        public double offtimestart;  //开始时间偏移

        //头信息数据类型  
        public headdatetype headdate; //默认为dfs0
    }

    //动态项目参数
    [Serializable]
    public struct Dfscreate_itemprs
    {
        public string itemname; //项目名称
        public eumItem item;    //项目类型,如rainfall
        public eumUnit itemunit;  //项目单位,如mm
        public DfsSimpleType itemdatatype; //项目数据类型，默认为double
        public DataValueType itemvaluetype; //项目数据形式，如累积，均步累积等
        public IDfsSpatialAxis itemspatialaxis; //动态项目空间轴
    }

    public partial class Dfs0
    {
        #region ***************************** 从dfs0文件中读取数据 *************************************
        //根据项目号获取项目名称
        public static string Dfs0_Reader_GetItemName(string sourcefilename, int ItemNo)
        {
            IDfsFile dfs0File = DfsFileFactory.DfsGenericOpen(sourcefilename);  //打开dfs0文件
            IDfsFileInfo fileInfo = dfs0File.FileInfo;
            IDfsSimpleDynamicItemInfo dynamicItemInfo = dfs0File.ItemInfo[ItemNo - 1];
            string name = dynamicItemInfo.Name;   //Item的name是MIKE11选择运行哪个Item的关键，所以只需要在txt输入name就可以    
            return name;
        }

        //根据名称获取项目号，从1开始排
        public static int Dfs0_Reader_GetItemId(string sourcefilename, string itemname)
        {
            int itemnumber = 1;
            IDfsFile dfs0File = DfsFileFactory.DfsGenericOpen(sourcefilename);  //打开dfs0文件
            IDfsFileInfo fileInfo = dfs0File.FileInfo;
            for (int i = 0; i < dfs0File.ItemInfo.Count; i++)
            {
                IDfsSimpleDynamicItemInfo dynamicItemInfo = dfs0File.ItemInfo[i];
                if (dynamicItemInfo.Name == itemname)
                {
                    itemnumber = i + 1;
                    break;
                }
            }
            return itemnumber;
        }

        //获取dfs0文件的指定项目数据，返回一个包含datetime时间在内的键值对集合
        public static Dictionary<DateTime, double> Dfs0_Reader_GetItemDic(string sourcefilename, int itemnumber)
        {
            //先初始化
            Dictionary<DateTime, double> resdic = new Dictionary<DateTime, double>();
            try
            {
                //调用DfsFileFactory类的DfsGenericOpen静态方法打开dfs0文件，返回IDfsFile接口类型
                IDfsFile dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

                //使用专门针对DFS0的Dfs0Util类的ReadDfs0DataDouble静态方法读取数据，返回double类型的二维数组，包含时间在内
                double[,] d = Dfs0Util.ReadDfs0DataDouble(dfsfile);

                //增加键值对集合元素
                DateTime[] time = dfsfile.FileInfo.TimeAxis.GetDateTimes();

                DateTime timeoffset = time[0];
                for (int i = 0; i < time.Length; i++)
                {
                    //除了第一个和最后一个，中间没值，故要重新赋值,增加偏移时间
                    time[i] = timeoffset.AddSeconds(d[i, 0]);
                    resdic.Add(time[i], d[i, itemnumber]);  //由于0列是时间，故给的项目数就是所要的项目数
                }
                //用完记得关闭
                dfsfile.Close();
            }
            catch (Exception er)
            {
                Console.WriteLine("无法打开文件:{0}", er.Message);
                return null;
            }
            return resdic;
        }

        //获取dfs0文件的具体数据，返回一个包含时间在内的二维数组
        public static double[,] Dfs0_Reader_GetItemArray(string sourcefilename)
        {
            double[,] resarray;
            try
            {
                IDfsFile dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

                resarray = Dfs0Util.ReadDfs0DataDouble(dfsfile);

                dfsfile.Close();
                return resarray;
            }
            catch (Exception er)
            {
                Console.WriteLine("无法打开文件:{0}", er.Message);
                resarray = new double[1, 1];
            }
            return resarray;
        }

        //获取dfs0的开始时间和结束时间
        public static void Dfs0_Reader_GetTime(string sourcefilename, out DateTime starttime, out DateTime endtime, out int steps)
        {
            starttime = DateTime.Now;
            endtime = DateTime.Now;
            steps = 0;
            try
            {
                IDfsFile dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

                //直接调用 GetDateTimes方法得到时间数组
                DateTime[] time = dfsfile.FileInfo.TimeAxis.GetDateTimes();
                starttime = time[0];
                endtime = time[time.Length - 1];  //注意数组长-1

                //直接调用NumberOfTimeSteps方法获取时间步数
                steps = dfsfile.FileInfo.TimeAxis.NumberOfTimeSteps;
                //关闭dfs文件
                dfsfile.Close();
            }
            catch (Exception er)
            {
                Console.WriteLine("无法打开文件:{0}", er.Message);
                return;
            }

        }

        #endregion **************************************************************************************
    }
}
