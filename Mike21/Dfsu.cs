using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfsu;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Kdbndp;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model;

namespace bjd_model.Mike21
{
    //二维动态项目结果类型
    [Serializable]
    public enum Mike21Res_Itemtype
    {
        Surface_elevation,
        Total_water_depth,
        P_flux,
        Current_speed,
        Current_direction
    }

    //二维结果结构体
    [Serializable]
    public struct Dfsu_ItemValue
    {
        public double water_level;      //水位
        public double water_depth;      //水深
        public double p_flux;           //通量
        public double water_speed;      //流速
        public double water_direction;  //流向
    }

    //二维洪水风险常用特征值结构体
    [Serializable]
    public struct Flood_StaticsValue
    {
        public double max_water_depth;      //最大水深
        public double max_water_speed;      //最大流速
        public DateTime flood_attime;       //到达时间
        public TimeSpan flood_duration;     //淹没历时
    }
    //dfsu的静态项目数据类型--节点和网格元素
    [Serializable]
    public struct Node_elm
    {
        public double[] X;
        public double[] Y;
        public float[] Z;

        public int[] Code;          //节点的边界编码
        public int[] nodeIds;      //节点ID
        public int[,] elmttable;  //元素的节点表
        public int[] elmtids;      //元素ID
    }

    public class Dfsu
    {
        #region ************************* 将dfsu结果写入数据库或文件 **********************************
        //将dfsu结果写入数据库,使得二维结果永久保存(*****速度慢,慎用!!****)
        public static void Writedfsu_intomysql(string sourcefilename,string tablename)
        {
            //打开dfsu文件，并将各节点坐标提取出来
            IDfsuFile dfsu = DfsuFile.Open(sourcefilename);
            DateTime starttime = dfsu.StartDateTime;
            double[] X = dfsu.X;
            double[] Y = dfsu.Y;
            float[] Z = dfsu.Z;
            int[] Code = dfsu.Code;

            //先判断是否有和数据库匹配的项目(数据库设计为固定5个项目的数据level、depth、speed、direction、p_flux)
            int level = -1;
            int depth = -1;
            int speed = -1;
            int direction = -1;
            int p_flux = -1;

            for (int i = 0; i < dfsu.ItemInfo.Count; i++)
            {
                switch (dfsu.ItemInfo[i].Name)
                {
                    case "Surface elevation":
                        level = i;
                        break;
                    case "Total water depth":
                        depth = i;
                        break;
                    case "Current speed":
                        speed = i;
                        break;
                    case "Current direction":
                        direction = i;
                        break;
                    case "P flux":
                        p_flux = i;
                        break;
                    default:
                        break;
                }
            }

            //定义交错数组，用于存储每个项目的每个元素的值
            float[][] data = new float[dfsu.ItemInfo.Count][];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new float[dfsu.NumberOfElements];
            }

            //定义一个IDfsItemData类型的数组，用于存储每个时间步每个动态项目的数据,<float>为实例化为float类型
            IDfsItemData<float>[] itemData = new IDfsItemData<float>[dfsu.ItemInfo.Count];
            int n = 1;
            for (int i = 0; i < dfsu.NumberOfTimeSteps; i++)
            {
                for (int j = 0; j < dfsu.ItemInfo.Count; j++)
                {
                    // 从原文件项目中读取数据
                    itemData[j] = (IDfsItemData<float>)dfsu.ReadItemTimeStepNext();

                    //将每一个项目的每一个网格元素的数据赋值给数组
                    for (int k = 0; k < dfsu.NumberOfElements; k++)
                    {
                        data[j][k] = itemData[j].Data[k];  //elmtsIncluded[k]为第k个包含在区域内元素在原mesh中的编号
                    }
                }

                //将这个时刻的所有项目的所有元素的值写入数据库
                for (int j = 0; j < dfsu.NumberOfElements; j++)
                {
                    //组建sql语句
                    object[] value = new object[8];

                    float levelvalue, depthvalue, speedvalue, directionvalue, pfluxvalue;
                    levelvalue = depthvalue = speedvalue = directionvalue = pfluxvalue = -1;
                    if (level != -1)
                    {
                        levelvalue = data[level][j];
                    }

                    if (depth != -1)
                    {
                        depthvalue = data[depth][j];
                    }

                    if (speed != -1)
                    {
                        speedvalue = data[speed][j];
                    }

                    if (direction != -1)
                    {
                        directionvalue = data[direction][j];
                    }

                    if (p_flux != -1)
                    {
                        pfluxvalue = data[p_flux][j];
                    }

                    value[0] = n;
                    value[1] = starttime.AddSeconds(itemData[0].Time);
                    value[2] = j + 1;
                    value[3] = levelvalue;
                    value[4] = depthvalue;
                    value[5] = speedvalue;
                    value[6] = directionvalue;
                    value[7] = pfluxvalue;

                    string sql = Mysql.sql_insert(tablename, value);

                    // Console.WriteLine(sql);
                    Mysql.Execute_Command(sql);
                    n++;
                }
            }

            //关闭连接
            
        }

        #endregion ************************************************************************************


        #region ***************** 从dfsu文件中获取指定点指定项目的键值对集合 *************************
        //获取指定位置的洪水风险特征值
        public static Flood_StaticsValue Extract_Flood_StaticsValue(HydroModel hydromodel, PointXY inputp)
        {
            double depthvalue = hydromodel.Mike21Pars.Dry_wet.dry;

            //获取所有项目值的时间序列
            Dictionary<DateTime, Dfsu_ItemValue> DfsuRes_AllItemRes = Extract_DfsuRes_AllItem(hydromodel, inputp);

            //统计值
            bool isattime = false;
            double max_waterdepth = DfsuRes_AllItemRes.Values.ElementAt(0).water_depth;
            double max_waterspeed = DfsuRes_AllItemRes.Values.ElementAt(0).water_speed;
            DateTime flood_attime = DfsuRes_AllItemRes.Keys.ElementAt(0);
            TimeSpan flood_duration = new TimeSpan(0, 0, 0);
            TimeSpan step_timespan = new TimeSpan(0, (int)(hydromodel.Mike21Pars.Mike21_savetimestepbs), 0);

            for (int i = 0; i < DfsuRes_AllItemRes.Count; i++)
            {
                DateTime itemtime = DfsuRes_AllItemRes.Keys.ElementAt(i);
                Dfsu_ItemValue itemvalues = DfsuRes_AllItemRes.Values.ElementAt(i);

                if (itemvalues.water_depth >= depthvalue)
                {
                    //最大水深统计
                    if (itemvalues.water_depth > max_waterdepth)
                    {
                        max_waterdepth = itemvalues.water_depth;
                    }

                    //最大流速统计
                    if (itemvalues.water_speed > max_waterspeed)
                    {
                        max_waterspeed = itemvalues.water_speed;
                    }

                    //到达时间统计
                    if (isattime == false)
                    {
                        flood_attime = itemtime;
                        isattime = true;
                    }

                    //淹没历时统计
                    flood_duration = flood_duration.Add(step_timespan);
                }
            }

            //特征值结果
            Flood_StaticsValue statics_value;
            statics_value.max_water_depth = max_waterdepth;
            statics_value.max_water_speed = max_waterspeed;
            statics_value.flood_attime = flood_attime;
            statics_value.flood_duration = flood_duration;

            return statics_value;
        }

        //获取指定位置指定项目的数据，返回一个包含datetime时间在内的键值对集合-----数据查询专用
        public static Dictionary<DateTime, double> Extract_DfsuRes_SingleItem(HydroModel hydromodel, PointXY inputp, Mike21Res_Itemtype res_itemtype)
        {
            string sourcefilename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            IDfsuFile dfsu = DfsuFile.Open(sourcefilename);

            int itemnumber = -1;
            string itemname = res_itemtype.ToString().Replace("_", " ");
            //获取指定项
            for (int i = 0; i < dfsu.ItemInfo.Count; i++)
            {
                if (dfsu.ItemInfo[i].Name == itemname)
                {
                    itemnumber = i;
                    break;
                }
            }
            if (itemnumber == -1)
            {
                Console.WriteLine("无法找到该项目!");
                return null;
            }

            //确定指定点所在的元素编号
            int elmnumber = Getelmnumber(dfsu, inputp);

            //提取dfsu的指定动态项目数据，生成键值对集合
            Dictionary<DateTime, double> dic = Dfsu_Reader(sourcefilename, elmnumber, itemnumber);
            return dic;
        }

        //获取指定位置所有项目的数据，返回一个包含datetime时间在内的键值对集合-----数据查询专用
        public static Dictionary<DateTime, Dfsu_ItemValue> Extract_DfsuRes_AllItem(HydroModel hydromodel, PointXY inputp)
        {
            string sourcefilename = hydromodel.Modelfiles.Dfsu1_gc_filename;

            Dictionary<DateTime, Dfsu_ItemValue> outdic = new Dictionary<DateTime, Dfsu_ItemValue>();
            IDfsuFile dfsfile = DfsuFile.Open(sourcefilename);

            //确定指定点所在的元素编号
            int elmnumber = Getelmnumber(dfsfile, inputp);

            //提取dfsu的指定动态项目数据，生成键值对集合
            DateTime starttime = dfsfile.StartDateTime;

            //读取项目数据,返回double类型的二维数组，包含时间在内
            for (int i = 0; i < dfsfile.NumberOfTimeSteps; i++)
            {
                double[] itemres = new double[dfsfile.ItemInfo.Count];
                IDfsItemData<float> itemData1 = (IDfsItemData<float>)dfsfile.ReadItemTimeStep(1, i);

                for (int j = 0; j < dfsfile.ItemInfo.Count; j++)
                {
                    // 从原文件项目中读取数据,注意项目数从1开始
                    IDfsItemData<float> itemData = (IDfsItemData<float>)dfsfile.ReadItemTimeStep(j + 1, i);
                    itemres[j] = (double)itemData.Data[elmnumber];
                }

                Dfsu_ItemValue itemvalue;
                itemvalue.water_level = 0;
                itemvalue.water_depth = 0;
                itemvalue.p_flux = 0;
                itemvalue.water_speed = 0;
                itemvalue.water_direction = 0;

                if (dfsfile.ItemInfo.Count == 5)
                {
                    itemvalue.water_level = itemres[0];
                    itemvalue.water_depth = itemres[1];
                    itemvalue.p_flux = itemres[2];
                    itemvalue.water_speed = itemres[3];
                    itemvalue.water_direction = itemres[4];
                }

                //添加键值对元素
                outdic.Add(starttime.AddSeconds(itemData1.Time), itemvalue);
            }

            return outdic;
        }

        //获取dfsu文件的指定元素的指定项目数据，返回一个包含datetime时间在内的键值对集合
        public static Dictionary<DateTime, double> Dfsu_Reader(string sourcefilename, int elmnumber, int itemnumber)
        {
            Dictionary<DateTime, double> outdic = new Dictionary<DateTime, double>();
            try
            {
                //打开文件
                IDfsuFile dfsfile = DfsuFile.Open(sourcefilename);

                DateTime starttime = dfsfile.StartDateTime;

                //读取项目数据,返回double类型的二维数组，包含时间在内
                for (int i = 0; i < dfsfile.NumberOfTimeSteps; i++)
                {
                    // 从原文件项目中读取数据,注意项目数从1开始
                    IDfsItemData<float> itemData = (IDfsItemData<float>)dfsfile.ReadItemTimeStep(itemnumber + 1, i);

                    //添加键值对元素
                    outdic.Add(starttime.AddSeconds(itemData.Time), (double)itemData.Data[elmnumber]);
                }

                //用完记得关闭
                dfsfile.Dispose();
                return outdic;
            }
            catch (Exception er)
            {
                Console.WriteLine("无法打开文件:{0}", er.Message);
                return null;
            }
        }
        #endregion ***************************************************************************************


        #region *************** 从dfsu中刷选出用于二维动态展示的数据 -- 水深项目湿网格 *******************
        //一次获取dfsu文件指定项目的数据，刷选出湿网格和湿网格存在的时间段，返回(datetime、数组)键值对集合
        public static Dictionary<DateTime, List<Valuexy>> Dfsu_Reader(HydroModel hydromodel, Mike21Res_Itemtype res_itemtype)
        {
            string sourcefilename = hydromodel.Modelfiles.Dfsu1_gc_filename;

            //打开文件
            IDfsuFile dfsfile = DfsuFile.Open(sourcefilename);

            DateTime starttime = dfsfile.StartDateTime;
            double depthvalue = hydromodel.Mike21Pars.Dry_wet.dry;

            //获取指定项
            int itemnumber = -1;
            string itemname = res_itemtype.ToString().Replace("_", " ");
            for (int i = 0; i < dfsfile.ItemInfo.Count; i++)
            {
                if (dfsfile.ItemInfo[i].Name == itemname)
                {
                    itemnumber = i;
                    break;
                }
            }
            if (itemnumber == -1)
            {
                Console.WriteLine("无法找到该项目!");
                return null;
            }

            //新建键值对集合对象
            Dictionary<DateTime, List<Valuexy>> outdic = new Dictionary<DateTime, List<Valuexy>>();

            //计算各网格元素中心点的X,Y,Z
            double[] centerx = new double[dfsfile.NumberOfElements];
            double[] centery = new double[dfsfile.NumberOfElements];
            double[] centerz = new double[dfsfile.NumberOfElements];
            dfsfile.CalculateElementCenterCoordinates(out centerx, out centery, out centerz);

            //读取项目数据,返回double类型的二维数组，包含时间在内
            for (int i = 0; i < dfsfile.NumberOfTimeSteps; i++)
            {
                // 从原文件项目中读取数据,注意项目数从1开始
                IDfsItemData<float> itemData = (IDfsItemData<float>)dfsfile.ReadItemTimeStep(itemnumber + 1, i);

                //新建作为值的子集合对象
                List<Valuexy> reslist = new List<Valuexy>();

                //添加作为值的 子集合元素
                if (itemData.Data.Sum() > depthvalue)
                {
                    int n = 0;
                    for (int j = 0; j < dfsfile.NumberOfElements; j++)
                    {
                        if (itemData.Data[j] > depthvalue)
                        {
                            Valuexy resvalue;
                            resvalue.number = n;
                            resvalue.X = centerx[j];
                            resvalue.Y = centery[j];
                            resvalue.Z = centerz[j];
                            resvalue.value = itemData.Data[j];

                            reslist.Add(resvalue);
                            n++;
                        }
                    }
                }

                //添加上层键值对元素
                if (reslist.Count != 0)
                {
                    outdic.Add(starttime.AddSeconds(itemData.Time), reslist);
                }
            }

            //关闭文件，释放内存资源
            dfsfile.Dispose();

            return outdic;
        }

        //每次从指定时间步读取数据，刷选出湿网格，返回(datetime、数组)键值对集合
        public static Dictionary<DateTime, List<Valuexy>> Dfsu_Reader_Getnewstepdic(HydroModel hydromodel, Mike21Res_Itemtype res_itemtype, int timestep)
        {
            string sourcefilename = hydromodel.Modelfiles.Dfsu1_gc_filename;

            //打开文件
            IDfsuFile dfsfile = DfsuFile.Open(sourcefilename);

            DateTime starttime = dfsfile.StartDateTime;
            double depthvalue = hydromodel.Mike21Pars.Dry_wet.dry;

            //获取指定项
            int itemnumber = -1;
            string itemname = res_itemtype.ToString().Replace("_", " ");
            for (int i = 0; i < dfsfile.ItemInfo.Count; i++)
            {
                if (dfsfile.ItemInfo[i].Name == itemname)
                {
                    itemnumber = i;
                    break;
                }
            }
            if (itemnumber == -1)
            {
                Console.WriteLine("无法找到该项目!");
                return null;
            }

            //新建键值对集合对象
            Dictionary<DateTime, List<Valuexy>> outdic = new Dictionary<DateTime, List<Valuexy>>();

            //计算各网格元素中心点的X,Y,Z
            double[] centerx = new double[dfsfile.NumberOfElements];
            double[] centery = new double[dfsfile.NumberOfElements];
            double[] centerz = new double[dfsfile.NumberOfElements];
            dfsfile.CalculateElementCenterCoordinates(out centerx, out centery, out centerz);

            //读取项目数据,返回double类型的二维数组，包含时间在内
            for (int i = timestep; i < dfsfile.NumberOfTimeSteps; i++)
            {
                // 从原文件项目中读取数据,注意项目数从1开始
                IDfsItemData<float> itemData = (IDfsItemData<float>)dfsfile.ReadItemTimeStep(itemnumber + 1, i);

                //新建作为值的子集合对象
                List<Valuexy> reslist = new List<Valuexy>();

                //添加作为值的 子集合元素
                if (itemData.Data.Sum() > depthvalue)
                {
                    int n = 0;
                    for (int j = 0; j < dfsfile.NumberOfElements; j++)
                    {
                        if (itemData.Data[j] > depthvalue)
                        {
                            Valuexy resvalue;
                            resvalue.number = n;
                            resvalue.X = centerx[j];
                            resvalue.Y = centery[j];
                            resvalue.Z = centerz[j];
                            resvalue.value = itemData.Data[j];

                            reslist.Add(resvalue);
                            n++;
                        }
                    }
                }

                //添加上层键值对元素
                if (reslist.Count != 0)
                {
                    outdic.Add(starttime.AddSeconds(itemData.Time), reslist);
                }
            }

            //关闭文件，释放内存资源
            dfsfile.Dispose();

            return outdic;
        }
        #endregion ***************************************************************************************


        #region ************ 从dfsu文件中获取指定点的指定项目或全部项目，生成dfs0文件 ********************
        //从DFSU文件中抽取指定位置的指定动态项目数据，生成dfs0文件
        public static string Extract_Dfs0_SingleItem(HydroModel hydromodel, PointXY inputp, Mike21Res_Itemtype res_itemtype)
        {
            string dfsufilename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            string outputfilename = Path.GetDirectoryName(dfsufilename) + @"\" + "point(" + ((int)inputp.X).ToString() + ")_" + res_itemtype.ToString() + ".dfs0";

            IDfsuFile dfsu = DfsuFile.Open(dfsufilename);

            int itemnumber = -1;
            string itemname = res_itemtype.ToString().Replace("_", " ");
            //获取指定项
            for (int i = 0; i < dfsu.ItemInfo.Count; i++)
            {
                if (dfsu.ItemInfo[i].Name == itemname)
                {
                    itemnumber = i;
                    break;
                }
            }
            if (itemnumber == -1)
            {
                Console.WriteLine("无法找到该项目!");
                return null;
            }

            //提取dfsu的头信息和动态项目信息，用以创建dfs0
            DfsBuilder dfs0build = Dfs0.Getdfsbder(dfsufilename, itemnumber);

            //确定指定点所在的元素编号
            int elmnumber = Getelmnumber(dfsu, inputp);

            //提取dfsu的指定动态项目数据，生成键值对集合
            Dictionary<DateTime, double> dic = Dfsu_Reader(dfsufilename, elmnumber, itemnumber);

            //创建包含指定动态项目的dfs0文件
            Dfs0.Dfs0_Creat1(dfs0build, outputfilename, dic);

            return outputfilename;
        }

        //从DFSU文件中抽取指定位置的所有动态项目数据，生成dfs0文件
        public static string Extract_Dfs0_AllItem(HydroModel hydromodel, PointXY inputp)
        {
            string dfsufilename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            string outputfilename = Path.GetDirectoryName(dfsufilename) + @"\" + "point(" + ((int)inputp.X).ToString() + ")_" + "allitem.dfs0";

            //提取dfsu的头信息和动态项目信息，用以创建dfs0
            DfsBuilder dfs0build = Dfs0.Getdfsbder(dfsufilename);

            //确定指定点所在的元素编号
            IDfsuFile dfsu = DfsuFile.Open(dfsufilename);
            int elmnumber = Getelmnumber(dfsu, inputp);

            //提取dfsu的动态项目数据，生成键值对集合数组
            Dictionary<DateTime, double>[] dicarray = Dfsu_Reader(dfsufilename, elmnumber);

            //创建包含多个动态项目的dfs0文件
            Dfs0.Dfs0_Creat1(dfs0build, outputfilename, dicarray);

            return outputfilename;
        }

        //获取指定点所在的网格元素编号
        public static int Getelmnumber(IDfsuFile dfsu, PointXY inputp)
        {
            int elmnumber = 1;
            // 节点坐标
            double[] X = dfsu.X;
            double[] Y = dfsu.Y;

            // 遍历所有的网格元素，以及每个网格元素的3个节点，看是否有网格元素满足要求，并将满足要求的元素收集起来
            List<int> elmtsIncluded = new List<int>();
            double xmax = 0, xmin = 0;
            double ymax = 0, ymin = 0;
            for (int i = 0; i < dfsu.NumberOfElements; i++)
            {
                // 提取网格元素的节点数组
                int[] nodes = dfsu.ElementTable[i];  //元素表是一个交错数组，即每个网格元素又是一个数组，里面包含的是它的3个节点的编号

                // 判断网格元素3个节点的坐标是否在子区域范围内
                xmax = X[nodes[0] - 1]; xmin = X[nodes[0] - 1];
                ymax = Y[nodes[0] - 1]; ymin = Y[nodes[0] - 1];
                for (int j = 0; j < nodes.Length; j++)
                {
                    int node = nodes[j] - 1;   //注意这里要-1,说明节点是从0开始计数的
                    if (X[node] > xmax)
                    {
                        xmax = X[node];
                    }

                    if (X[node] < xmin)
                    {
                        xmin = X[node];
                    }

                    if (Y[node] > ymax)
                    {
                        ymax = Y[node];
                    }

                    if (Y[node] < ymin)
                    {
                        ymin = Y[node];
                    }
                }

                if ((inputp.X >= xmin && inputp.X <= xmax) && (inputp.Y >= ymin && inputp.Y <= ymax))
                {
                    elmtsIncluded.Add(i);
                }
            }

            //在可能的网格元素里找到指定点在内的一个
            for (int i = 0; i < elmtsIncluded.Count; i++)
            {
                int[] nodes = dfsu.ElementTable[elmtsIncluded[i]];
                if (PointXY.Pointinelm(inputp, X, Y, nodes))
                {
                    elmnumber = elmtsIncluded[i];
                    break;
                }
            }
            return elmnumber;
        }

        //获取dfsu文件的指定元素的所有项目数据，返回一个包含datetime时间在内的键值对集合数组
        public static Dictionary<DateTime, double>[] Dfsu_Reader(string sourcefilename, int elmnumber)
        {
            Dictionary<DateTime, double>[] outdicarray;
            try
            {
                //打开文件
                IDfsuFile dfsfile = DfsuFile.Open(sourcefilename);

                DateTime starttime = dfsfile.StartDateTime;
                //初始化数组
                outdicarray = new Dictionary<DateTime, double>[dfsfile.ItemInfo.Count];

                //读取项目数据,返回double类型的二维数组，包含时间在内
                for (int i = 0; i < dfsfile.ItemInfo.Count; i++)
                {
                    //因为数组的元素是集合，需要开辟内存，故数组的每个元素都得先初始化
                    outdicarray[i] = new Dictionary<DateTime, double>();
                    for (int j = 0; j < dfsfile.NumberOfTimeSteps; j++)
                    {
                        // 从原文件项目中读取数据,注意项目数从1开始
                        IDfsItemData<float> itemData = (IDfsItemData<float>)dfsfile.ReadItemTimeStep(i + 1, j);

                        //添加键值对元素
                        outdicarray[i].Add(starttime.AddSeconds(itemData.Time), (double)itemData.Data[elmnumber]);
                    }
                }
                //用完记得关闭
                dfsfile.Dispose();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return null;
            }

            return outdicarray;
        }
        #endregion ****************************************************************************************


        #region *************** 从dfsu文件中截取矩形区域或湿网格区域数据,生成新的dfsu文件 *********************
        //从原dfsu文件中抽取指定矩形区域，生成新的dfsu文件，单元格编码重排,返回子区域dfsu路径
        public static string Extract_Subarea_Dfsu(HydroModel hydromodel, SubArea sub)
        {
            string sourceFilename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            string outputFilename = Path.GetDirectoryName(sourceFilename) + @"\" + Path.GetFileNameWithoutExtension(sourceFilename) + "_sub.dfsu";

            IDfsuFile dfsu = DfsuFile.Open(sourceFilename);

            // 节点坐标
            double[] X = dfsu.X;
            double[] Y = dfsu.Y;
            float[] Z = dfsu.Z;
            int[] Code = dfsu.Code;  //节点的边界代码，大于0则为开边界

            // 遍历所有的网格元素，以及每个网格元素的3个节点，看是否有节点在给定子区域范围内
            // 如果有，则相应的区域、网格元素和它的节点被包含在新mesh里  
            List<int> elmtsIncluded = new List<int>();
            bool[] nodesIncluded = new bool[dfsu.NumberOfNodes];
            for (int i = 0; i < dfsu.NumberOfElements; i++)
            {
                // 提取网格元素的节点数组
                int[] nodes = dfsu.ElementTable[i];  //元素表是一个交错数组，即每个网格元素又是一个数组，里面包含的是它的3个节点的编号

                // 判断网格元素3个节点的坐标是否在子区域范围内
                bool elmtIncluded = false;
                for (int j = 0; j < nodes.Length; j++)
                {
                    int node = nodes[j] - 1;   //注意这里要-1,说明节点是从0开始计数的
                    if (X[node] >= sub.x1 && X[node] <= sub.x2 && Y[node] >= sub.y1 && Y[node] <= sub.y2)
                        elmtIncluded = true;
                    break;  //有一个包含在内就算
                }

                //如果元素包含在子区域内
                if (elmtIncluded)
                {
                    // 将该元素编号加入到集合中
                    elmtsIncluded.Add(i);
                    // 标记该元素的所有节点为包含在区域内的
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        int node = nodes[j] - 1;
                        nodesIncluded[node] = true;
                    }
                }
            }

            //调用方法生成新的dfsu文件
            Create_New_Dfsu(sourceFilename, outputFilename, nodesIncluded, elmtsIncluded);

            return outputFilename;
        }

        //从原dfsu文件中抽取湿网格区域，生成新的dfsu文件，单元格编码重排
        public static string Extract_Wetarea_Dfsu(HydroModel hydromodel)
        {
            string sourceFilename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            string outputFilename = Path.GetDirectoryName(sourceFilename) + @"\" + Path.GetFileNameWithoutExtension(sourceFilename) + "_wet.dfsu";

            IDfsuFile dfsu = DfsuFile.Open(sourceFilename);
            double depthvalue = hydromodel.Mike21Pars.Dry_wet.dry;

            //先判断是否包含水深项,以及所在的项目编号
            bool includeYN = false;
            int itemdepthnumber = 1;
            for (int i = 0; i < dfsu.ItemInfo.Count; i++)
            {
                if (dfsu.ItemInfo[i].Name == "Total water depth")
                {
                    includeYN = true;
                    itemdepthnumber = i;
                }
            }

            if (includeYN == false)
            {
                Console.WriteLine("不包含水深项!");
                return null;
            }

            // 遍历所有的网格元素，看元素的最大水深是否大于0
            // 如果大于0即为湿网格，则相应的网格元素和它的节点被包含在新mesh里  
            List<int> elmtsIncluded = new List<int>();
            bool[] nodesIncluded = new bool[dfsu.NumberOfNodes];

            for (int i = 0; i < dfsu.NumberOfElements; i++)
            {
                // 提取网格元素的节点数组
                int[] nodes = dfsu.ElementTable[i];  //元素表是一个交错数组，即每个网格元素又是一个数组，里面包含的是它的3个节点的编号

                // 判断网格元素各时间步的累积水深值是否大于0
                bool elmtIncluded = false;
                float sumdepth = 0;

                for (int j = 0; j < dfsu.NumberOfTimeSteps; j++)
                {
                    IDfsItemData<float> itemData = (IDfsItemData<float>)dfsu.ReadItemTimeStep(itemdepthnumber + 1, j);
                    sumdepth += itemData.Data[i];  //读取的结果数组里第i个网格元素的值
                    if (itemData.Data[i] > depthvalue)
                    {
                        break;
                    }
                }

                //表示第i个网格元素为湿网格
                if (sumdepth > depthvalue)
                {
                    elmtIncluded = true;
                }

                //如果元素包含在子区域内
                if (elmtIncluded)
                {
                    // 将该元素编号加入到集合中
                    elmtsIncluded.Add(i);
                    // 标记该元素的所有节点为包含在区域内的
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        int node = nodes[j] - 1;
                        nodesIncluded[node] = true;
                    }
                }
            }

            //调用方法生成新的dfsu文件
            Create_New_Dfsu(sourceFilename, outputFilename, nodesIncluded, elmtsIncluded);

            return outputFilename;
        }

        //根据原dfsu文件中指定需要包含的元素集合，需要包含的节点数组(标记为bool)创建新dfsu文件
        public static void Create_New_Dfsu(string sourceFilename, string outputFilename, bool[] nodesIncluded, List<int> elmtsIncluded)
        {
            // 节点坐标
            IDfsuFile dfsu = DfsuFile.Open(sourceFilename);
            double[] X = dfsu.X;
            double[] Y = dfsu.Y;
            float[] Z = dfsu.Z;
            int[] Code = dfsu.Code;  //节点的边界代码，大于0则为开边界

            // 在新网格中的节点信息数组
            int[] renumber = new int[dfsu.NumberOfNodes];  //长度有点偏大
            List<double> X2 = new List<double>();
            List<double> Y2 = new List<double>();
            List<float> Z2 = new List<float>();
            List<int> Code2 = new List<int>();
            List<int> nodeIds = new List<int>();

            int n = 0;
            for (int i = 0; i < dfsu.NumberOfNodes; i++)
            {
                if (nodesIncluded[i])
                {
                    X2.Add(X[i]);
                    Y2.Add(Y[i]);
                    Z2.Add(Z[i]);
                    Code2.Add(Code[i]);            //节点的边界代码
                    nodeIds.Add(dfsu.NodeIds[i]);  //节点的ID
                    //重新定义节点索引号，从0开始
                    renumber[i] = n;    //如 renumber[250] = 0;
                    n++;
                }
            }

            // 新网格元素
            List<int[]> elmttable2 = new List<int[]>();
            List<int> elmtIds = new List<int>();
            for (int i = 0; i < elmtsIncluded.Count; i++)
            {
                //获取第i个包含在区域内元素在原mesh中的编号
                int elmt = elmtsIncluded[i];

                //获取该编号元素的节点列表
                int[] nodes = dfsu.ElementTable[elmt];

                //该网格元素的节点要重新编号
                int[] newNodes = new int[nodes.Length];
                for (int j = 0; j < nodes.Length; j++)
                {
                    newNodes[j] = renumber[nodes[j] - 1] + 1;
                }

                //增加每个元素的节点表数组
                elmttable2.Add(newNodes);
                //增加元素编号，从1开始，这里相当于elmtIds.Add(i+1);
                elmtIds.Add(dfsu.ElementIds[i]);
            }

            // 创建新的dfsu文件,DfsuFileType为枚举类型
            DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);

            // 设置头信息、地图投影和静态项目(节点、网格)

            //设置节点，参数为节点的x,y,z数组和边界代码数组，类型分别为double,double,float,int
            builder.SetNodes(X2.ToArray(), Y2.ToArray(), Z2.ToArray(), Code2.ToArray());

            //设置网格元素，参数为元素的数组，类型为交错数组
            builder.SetElements(elmttable2.ToArray());

            //设置元素ID，参数为整形数组
            builder.SetElementIds(elmtIds.ToArray());

            //设置地图投影等于原文件地图投影
            builder.SetProjection(dfsu.Projection);

            //设置时间信息等于原文件开始时间和时间步长(以秒计)
            builder.SetTimeInfo(dfsu.StartDateTime, dfsu.TimeStepInSeconds);

            //设置数据单位，如果原数据单位未定义，则按m定义，否则等于原单位
            if (dfsu.ZUnit == eumUnit.eumUUnitUndefined)
            {
                builder.SetZUnit(eumUnit.eumUmeter);
            }
            else
            {
                builder.SetZUnit(dfsu.ZUnit);
            }

            // 将原文件的动态项目拷贝过来
            for (int i = 0; i < dfsu.ItemInfo.Count; i++)
            {
                IDfsSimpleDynamicItemInfo itemInfo = dfsu.ItemInfo[i];
                builder.AddDynamicItem(itemInfo.Name, itemInfo.Quantity);
            }

            // 创建dfsu文件
            DfsuFile dfsuOut = builder.CreateFile(outputFilename);

            // 写入项目数据
            float[] data2 = new float[elmtsIncluded.Count];
            for (int i = 0; i < dfsu.NumberOfTimeSteps; i++)
            {
                for (int j = 0; j < dfsu.ItemInfo.Count; j++)
                {
                    // 从原文件项目中读取数据
                    IDfsItemData<float> itemData = (IDfsItemData<float>)dfsu.ReadItemTimeStep(j + 1, i);
                    //提取在区域内的元素的数据
                    for (int k = 0; k < elmtsIncluded.Count; k++)
                    {
                        data2[k] = itemData.Data[elmtsIncluded[k]];  //elmtsIncluded[k]为第k个包含在区域内元素在原mesh中的编号
                    }

                    // 写入数据,每一时间步，每一个项目，都需要写入一系列的网格元素数据
                    dfsuOut.WriteItemTimeStepNext(itemData.Time, data2);
                }
            }
            dfsuOut.Dispose();
            dfsu.Dispose();
        }

        // 内插糙率DFSU文件所有节点糙率
        public static void EditDfsu_Interpolate_Allnodes(string sourceFilename)
        {
            //打开DFSU文件
            IDfsuFile dfsu = DfsuFile.OpenEdit(sourceFilename);
            double[] X = dfsu.X;
            double[] Y = dfsu.Y;
            float[] Z = dfsu.Z;

            Console.WriteLine("开始糙率插值........");

            //计算各网格元素中心点的X,Y,Z
            double[] centerx = new double[dfsu.NumberOfElements];
            double[] centery = new double[dfsu.NumberOfElements];
            double[] centerz = new double[dfsu.NumberOfElements];
            dfsu.CalculateElementCenterCoordinates(out centerx, out centery, out centerz);
            List<PointXY> ele_centerpoint = new List<PointXY>();  //mesh文件中节点的坐标集合
            for (int i = 0; i < centerx.Length; i++)
            {
                ele_centerpoint.Add(PointXY.Get_Pointxy(centerx[i], centery[i]));
            }

            //调用GIS服务内插元素中心点糙率值
            List<PointXYZ> interpolate_point = Gis_Service.Get_Nodes_Clvalue_FromGisService(ele_centerpoint);
            float[] data = new float[interpolate_point.Count];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)interpolate_point[i].Z;
            }

            // 从原文件项目中读取数据
            IDfsItemData<float> itemData = (IDfsItemData<float>)dfsu.ReadItemTimeStep(1, 0);
            dfsu.Reset(); //文件指针重设到起点
            dfsu.WriteItemTimeStepNext(itemData.Time, data);

            //释放资源
            dfsu.Close();
            dfsu.Dispose();

            Console.WriteLine("糙率插值成功");
        }
        #endregion ********************************************************************************************


        #region ***************** 合并dfsu文件 ****************************************************************
        // 将两个dfsu合并成一个(dfsu无交叉重叠),sourcefilenames源dfsu文件路径，outputfilename输出dfsu文件路径
        public static void Merge_Dfsu(string sourcefilename1, string sourcefilename2, string outputfilename)
        {
            // 节点坐标
            IDfsuFile dfsu1 = DfsuFile.Open(sourcefilename1);
            double[] X1 = dfsu1.X;
            double[] Y1 = dfsu1.Y;
            float[] Z1 = dfsu1.Z;
            int[] Code1 = dfsu1.Code;  //节点的边界代码，大于0则为开边界
            int[] Node_Id1 = dfsu1.NodeIds;

            IDfsuFile dfsu2 = DfsuFile.Open(sourcefilename2);
            double[] X2 = dfsu2.X;
            double[] Y2 = dfsu2.Y;
            float[] Z2 = dfsu2.Z;
            int[] Code2 = dfsu2.Code;
            int[] Node_Id2 = dfsu2.NodeIds;

            // 在新网格中的节点信息数组
            int[] renumber = new int[X1.Length + X2.Length];
            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            List<float> Z = new List<float>();
            List<int> Code = new List<int>();
            List<int> Node_Id = new List<int>();

            int n = 0;
            for (int i = 0; i < dfsu1.NumberOfNodes; i++)
            {
                X.Add(X1[i]);
                Y.Add(Y1[i]);
                Z.Add(Z1[i]);
                Code.Add(Code1[i]);            //节点的边界代码
                Node_Id.Add(Node_Id1[i]);  //节点的ID

                //重新定义节点索引号，从0开始
                renumber[i] = n;    //如 renumber[250] = 0;
                n++;
            }

            for (int i = 0; i < dfsu2.NumberOfNodes; i++)
            {
                X.Add(X2[i]);
                Y.Add(Y2[i]);
                Z.Add(Z2[i]);
                Code.Add(Code2[i]);            //节点的边界代码
                Node_Id.Add(Node_Id2[i]);  //节点的ID

                //重新定义节点索引号，从0开始
                renumber[i] = n;    //如 renumber[250] = 0;
                n++;
            }

            // 新网格元素
            List<int[]> elmttable = new List<int[]>();
            List<int> elmtIds = new List<int>();
            for (int i = 0; i < dfsu1.NumberOfElements; i++)
            {
                //获取该编号元素的节点列表
                int[] nodes = dfsu1.ElementTable[i];

                //增加每个元素的节点表数组
                elmttable.Add(nodes);

                //增加元素编号，从1开始，这里相当于elmtIds.Add(i+1);
                elmtIds.Add(dfsu1.ElementIds[i]);
            }

            for (int i = 0; i < dfsu2.NumberOfElements; i++)
            {
                //获取该编号元素的节点列表
                int[] nodes = dfsu2.ElementTable[i];

                //该网格元素的节点要重新编号
                int[] newNodes = new int[nodes.Length];
                for (int j = 0; j < nodes.Length; j++)
                {
                    newNodes[j] = renumber[nodes[j] - 1] + 1;
                }

                //增加每个元素的节点表数组
                elmttable.Add(newNodes);

                //增加元素编号，从1开始，这里相当于elmtIds.Add(i+1);
                elmtIds.Add(dfsu1.NumberOfElements + dfsu2.ElementIds[i]);
            }

            // 创建新的dfsu文件,DfsuFileType为枚举类型
            DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);

            // 设置头信息、地图投影和静态项目(节点、网格)

            //设置节点，参数为节点的x,y,z数组和边界代码数组，类型分别为double,double,float,int
            builder.SetNodes(X.ToArray(), Y.ToArray(), Z.ToArray(), Code.ToArray());

            //设置网格元素，参数为元素的数组，类型为交错数组
            builder.SetElements(elmttable.ToArray());

            //设置元素ID，参数为整形数组
            builder.SetElementIds(elmtIds.ToArray());

            //设置地图投影等于原文件地图投影
            builder.SetProjection(dfsu1.Projection);

            //设置时间信息等于原文件开始时间和时间步长(以秒计)
            builder.SetTimeInfo(dfsu1.StartDateTime, dfsu1.TimeStepInSeconds);

            //设置Z数据单位m
            builder.SetZUnit(eumUnit.eumUmeter);

            // 将原文件的动态项目拷贝过来
            for (int i = 0; i < dfsu1.ItemInfo.Count; i++)
            {
                IDfsSimpleDynamicItemInfo itemInfo = dfsu1.ItemInfo[i];
                builder.AddDynamicItem(itemInfo.Name, itemInfo.Quantity);
            }

            // 创建dfsu文件
            DfsuFile dfsuOut = builder.CreateFile(outputfilename);

            // 写入项目数据
            List<float> data = new List<float>();
            for (int i = 0; i < dfsu1.NumberOfTimeSteps; i++)
            {
                for (int j = 0; j < dfsu1.ItemInfo.Count; j++)
                {
                    // 从原文件项目中读取数据
                    IDfsItemData<float> itemData1 = (IDfsItemData<float>)dfsu1.ReadItemTimeStep(j + 1, i);
                    IDfsItemData<float> itemData2 = (IDfsItemData<float>)dfsu2.ReadItemTimeStep(j + 1, i);

                    //提取在区域内的元素的数据
                    for (int k = 0; k < dfsu1.NumberOfElements; k++)
                    {
                        data.Add(itemData1.Data[k]);  //elmtsIncluded[k]为第k个包含在区域内元素在原mesh中的编号
                    }

                    for (int k = 0; k < dfsu2.NumberOfElements; k++)
                    {
                        data.Add(itemData2.Data[k]);   //elmtsIncluded[k]为第k个包含在区域内元素在原mesh中的编号
                    }

                    // 写入数据,每一时间步，每一个项目，都需要写入一系列的网格元素数据
                    dfsuOut.WriteItemTimeStepNext(itemData1.Time, data.ToArray());
                }
            }
            dfsuOut.Dispose();
            dfsu1.Dispose();
        }


        #endregion

        #region  ***************  调用新进程和GIS服务，共同处理二维结果数据，生成时态数据GIS图层 *********************
        //调用新进程处理dfsu数据，生成时态数据GIS图层
        public static void Create_NewProcess_DfsuToGis(HydroModel hydromodel)
        {
            //子进程 exe程序路径
            string dfsuprocessexe = Model_Const.SUBPROCESSNAME_DFSUTOGIS;

            //总步数
            TimeSpan totaltimespan = hydromodel.ModelGlobalPars.Simulate_time.End.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin);
            int dfsu_totalsteps = (int)totaltimespan.TotalSeconds / ((int)hydromodel.Mike21Pars.Mike21_savetimestepbs * (int)hydromodel.ModelGlobalPars.Simulate_timestep);

            //子进程信息 -- 包括进程exe程序路径和exe程序参数
            string sourceFilename = hydromodel.Modelfiles.Dfsu1_gc_filename;
            int n = 0;
            if (!File.Exists(sourceFilename))
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    if (File.Exists(sourceFilename))
                    {
                        break;
                    }
                    else if (n > 20)
                    {
                        Console.WriteLine("未找到结果DFSU文件！");
                        return;
                    }
                    n++;
                }
            }

            string infostr = sourceFilename + "&" + dfsu_totalsteps.ToString();
            ProcessStartInfo appinfo = new ProcessStartInfo(dfsuprocessexe, infostr);
            appinfo.CreateNoWindow = false;

            //新建子进程并执行子进程程序
            Process dfsu_process = new Process();
            dfsu_process.StartInfo = appinfo;

            try
            {
                dfsu_process.Start();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
        }
        #endregion

    }

}

