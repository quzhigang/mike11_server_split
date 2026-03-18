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
    public partial class Dfs0
    {
        #region **************************** 根据模板创建dfs0文件******************************************
        //创建dfs0文件,根据键值对集合和参考dfs文件创建
        public static int Dfs0_Creat(string outputfilename, string sourcefilename, Dictionary<DateTime, double> inputdic) //dfsfilename为参考文件
        {
            DateTime starttime = inputdic.ElementAt(0).Key;

            //根据参考dfs文件调用自定义的静态方法得到DfsBuilder对象
            DfsBuilder dfsbuilder = Getdfsbder(sourcefilename, starttime);

            //用自定义的静态方法完成文件的创建和数据的写入
            int n = Dfs0_Creat1(dfsbuilder, outputfilename, inputdic);

            //n=0表示创建成功
            return n;
        }

        //根据模板创建DfsBuilder对象--适用dfs0一个动态项目的情况,自己定义开始时间
        public static DfsBuilder Getdfsbder(string sourcefilename, DateTime starttime)
        {
            //以只读格式打开文件
            IDfsFile dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

            //直接获取头文件信息
            IDfsFileInfo headinfo = dfsfile.FileInfo;

            //创建DfsBuilder对象，初始化参数为：文件名 应用程序标题(MIKE Zero) 版本号(100)
            int vn = headinfo.ApplicationVersion;
            string appstr = headinfo.ApplicationTitle;
            string filetitle = headinfo.FileName;
            DfsBuilder dfsbuilder = DfsBuilder.Create(filetitle, appstr, vn);

            //需要重新设置时间轴
            DfsFactory dfsfact = new DfsFactory();

            //该语句作用：当输入的键值对集合与时间轴类型不同时，用非等距绝对时间以强制匹配键值对集合
            IDfsTemporalAxis timeaxis = dfsfact.CreateTemporalNonEqCalendarAxis(headinfo.TimeAxis.TimeUnit, starttime);
            dfsbuilder.SetTemporalAxis(timeaxis);

            //设置地图投影和数据类型
            dfsbuilder.SetGeographicalProjection(headinfo.Projection);
            dfsbuilder.SetDataType(headinfo.DataType);

            //将参考dfs文件的第1个动态项赋值给新建的动态项信息对象
            IDfsDynamicItemInfo dyiteminfo = dfsfile.ItemInfo[0];

            //使用DfsBuilder对象的相应方法增加相应的动态项目
            dfsbuilder.AddDynamicItem(dyiteminfo);

            return dfsbuilder;
        }

        //根据DfsBuilder对象、键值对集合完成dfs0文件的创建和数据的写入---适合1个动态项目
        public static int Dfs0_Creat1(DfsBuilder dfsbuilder, string outputfilename, Dictionary<DateTime, double> inputdic)
        {
            DateTime starttime = inputdic.ElementAt(0).Key;
            //错误信息反馈,没错误了就可以CreateFile()了
            try
            {
                string[] strarray = dfsbuilder.Validate();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return -1;
            }

            //生成dfs文件
            try
            {
                dfsbuilder.CreateFile(outputfilename);
            }
            catch (Exception er)
            {
                Console.WriteLine("error:{0}", er.Message);
                return -1;
            }

            //重新得到用于编辑dfs文件的IDfsFile类对象
            IDfsFile newdfsfile = dfsbuilder.GetFile();

            //将数据写入项目
            Writeitem(newdfsfile, inputdic);

            return 0;
        }

        //将数据写入IDfsFile项目中---适合于单个动态项目情况
        public static void Writeitem(IDfsFile dfsfile, Dictionary<DateTime, double> inputdic)
        {
            DateTime starttime = inputdic.ElementAt(0).Key;
            //*重点注意* dfs0文件的动态项目单个记录也是一个数组，且长度是1，注意数据类型
            switch (dfsfile.ItemInfo[0].DataType.ToString())
            {
                case "Float":
                    foreach (KeyValuePair<DateTime, double> kv in inputdic)
                    {
                        //如果IDfsFile的打开模式为edit，则文件指针在起点步，如果为append则在终点步，在起点步会改写，终点步会追加
                        Single[] valuearray = { (Single)kv.Value };
                        //使用WriteItemTimeStepNext方法添加记录，采用datatime类的Subtract方法减开始日期，然后换算成相对秒数(double类型)
                        dfsfile.WriteItemTimeStepNext(kv.Key.Subtract(starttime).TotalSeconds, valuearray);
                    }
                    break;
                case "Double":
                    foreach (KeyValuePair<DateTime, double> kv in inputdic)
                    {
                        Double[] valuearray = { (double)kv.Value };
                        dfsfile.WriteItemTimeStepNext(kv.Key.Subtract(starttime).TotalSeconds, valuearray);
                    }
                    break;
                case "Int":
                    foreach (KeyValuePair<DateTime, double> kv in inputdic)
                    {
                        int[] valuearray = { (int)kv.Value };
                        dfsfile.WriteItemTimeStepNext(kv.Key.Subtract(starttime).TotalSeconds, valuearray);
                    }
                    break;
                default:
                    Console.WriteLine("请检查动态项目数据类型是否有误");
                    return;
            }
            //记得关闭
            dfsfile.Close();
        }

        //根据DfsBuilder对象、键值对集合数组完成dfs0文件的创建和数据的写入---适合多个动态项目
        public static int Dfs0_Creat1(DfsBuilder dfsbuilder, string outputfilename, Dictionary<DateTime, double>[] inputdicarray)
        {
            DateTime starttime = inputdicarray[0].ElementAt(0).Key;
            //错误信息反馈,没错误了就可以CreateFile()了
            try
            {
                string[] strarray = dfsbuilder.Validate();
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return -1;
            }

            //生成dfs文件
            try
            {
                dfsbuilder.CreateFile(outputfilename);
            }
            catch (Exception er)
            {
                Console.WriteLine("error:{0}", er.Message);
                return -1;
            }

            //重新得到用于编辑dfs文件的IDfsFile类对象
            IDfsFile newdfsfile = dfsbuilder.GetFile();

            //将数据写入项目
            Writeitem(newdfsfile, inputdicarray);

            return 0;
        }

        //将数据写入IDfsFile项目中---适合于多个动态项目情况
        public static void Writeitem(IDfsFile dfsfile, Dictionary<DateTime, double>[] inputdicarray)
        {
            DateTime starttime = inputdicarray[0].ElementAt(0).Key;

            //*重点注意* dfs0文件的动态项目单个记录也是一个数组，且长度是1，注意数据类型
            switch (dfsfile.ItemInfo[0].DataType.ToString())
            {
                case "Float":
                    //横着写，一次写所有项目的某一个时刻的数据
                    for (int i = 0; i < inputdicarray[0].Count; i++)
                    {
                        Single[] valuearray = new Single[1];
                        for (int j = 0; j < inputdicarray.Length; j++)
                        {
                            valuearray[0] = (Single)inputdicarray[j].ElementAt(i).Value;
                            dfsfile.WriteItemTimeStepNext(inputdicarray[j].ElementAt(i).Key.Subtract(starttime).TotalSeconds, valuearray);
                        }
                    }
                    break;
                case "Double":
                    for (int i = 0; i < inputdicarray[0].Count; i++)
                    {
                        double[] valuearray = new double[1];
                        for (int j = 0; j < inputdicarray.Length; j++)
                        {
                            valuearray[0] = (double)inputdicarray[j].ElementAt(i).Value;
                            dfsfile.WriteItemTimeStepNext(inputdicarray[j].ElementAt(i).Key.Subtract(starttime).TotalSeconds, valuearray);
                        }
                    }
                    break;
                case "Int":
                    for (int i = 0; i < inputdicarray[0].Count; i++)
                    {
                        int[] valuearray = new int[1];
                        for (int j = 0; j < inputdicarray.Length; j++)
                        {
                            valuearray[0] = (int)inputdicarray[j].ElementAt(i).Value;
                            dfsfile.WriteItemTimeStepNext(inputdicarray[j].ElementAt(i).Key.Subtract(starttime).TotalSeconds, valuearray);
                        }
                    }
                    break;
                default:
                    Console.WriteLine("请检查动态项目数据类型是否有误");
                    return;
            }

            //记得关闭
            dfsfile.Close();
        }

        //根据模板创建DfsBuilder对象,包含所有动态项--适用dfsu N个动态项目的情况,并采用模板的开始时间,只能是等距时间轴
        public static DfsBuilder Getdfsbder(string sourcefilename)
        {
            //以只读格式打开文件
            IDfsFile dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

            //获取开始时间
            DateTime[] time = dfsfile.FileInfo.TimeAxis.GetDateTimes();
            DateTime starttime = time[0];

            //直接获取头文件信息
            IDfsFileInfo headinfo = dfsfile.FileInfo;

            //创建DfsBuilder对象，初始化参数为：文件名 应用程序标题(MIKE Zero) 版本号(100)
            int vn = headinfo.ApplicationVersion;
            string appstr = headinfo.ApplicationTitle;
            string filetitle = headinfo.FileName;
            DfsBuilder dfsbuilder = DfsBuilder.Create(filetitle, appstr, vn);

            //需要重新设置时间轴
            DfsFactory dfsfact = new DfsFactory();

            //设置为等距时间步，开始时间、步长、步数与模板相同
            IDfsTemporalAxis timeaxis = dfsfact.CreateTemporalNonEqCalendarAxis(headinfo.TimeAxis.TimeUnit, starttime);
            dfsbuilder.SetTemporalAxis(timeaxis);

            //设置地图投影和数据类型
            dfsbuilder.SetGeographicalProjection(headinfo.Projection);
            dfsbuilder.SetDataType(headinfo.DataType);

            //创建新的动态项目对象
            for (int i = 0; i < dfsfile.ItemInfo.Count; i++)
            {
                DfsDynamicItemBuilder dynitem = dfsbuilder.CreateDynamicItemBuilder();
                //设置动态项目名称和数据单位
                dynitem.Set(dfsfile.ItemInfo[i].Name, dfsfile.ItemInfo[i].Quantity, dfsfile.ItemInfo[i].DataType);

                //设置动态项目的空间轴,如果采用原项目的空间轴，则会有几万个网格元素
                IDfsSpatialAxis spatialaxis = dfsfact.CreateAxisEqD0();
                dynitem.SetAxis(spatialaxis);

                //设置动态项目数据值类型（如累积、单步、单步累积等）
                dynitem.SetValueType(dfsfile.ItemInfo[i].ValueType);

                //最后采用DfsDynamicItemBuilder类的GetDynamicItemInfo方法返回动态项目信息
                IDfsDynamicItemInfo dyiteminfo = dynitem.GetDynamicItemInfo();

                //使用DfsBuilder对象的相应方法增加相应的动态项目
                dfsbuilder.AddDynamicItem(dyiteminfo);
            }
            return dfsbuilder;
        }

        //根据模板创建DfsBuilder对象，包含指定动态项--适用dfsu N个动态项目的情况,并采用模板的开始时间
        public static DfsBuilder Getdfsbder(string sourcefilename, int itemnumber)
        {
            //以只读格式打开文件
            IDfsFile dfsfile = DfsFileFactory.DfsGenericOpen(sourcefilename);

            //获取开始时间
            DateTime[] time = dfsfile.FileInfo.TimeAxis.GetDateTimes();
            DateTime starttime = time[0];

            //直接获取头文件信息
            IDfsFileInfo headinfo = dfsfile.FileInfo;

            //创建DfsBuilder对象，初始化参数为：文件名 应用程序标题(MIKE Zero) 版本号(100)
            int vn = headinfo.ApplicationVersion;
            string appstr = headinfo.ApplicationTitle;
            string filetitle = headinfo.FileName;
            DfsBuilder dfsbuilder = DfsBuilder.Create(filetitle, appstr, vn);

            //需要重新设置时间轴
            DfsFactory dfsfact = new DfsFactory();

            //该语句作用：当输入的键值对集合与时间轴类型不同时，用非等距绝对时间以强制匹配键值对集合
            IDfsTemporalAxis timeaxis = dfsfact.CreateTemporalNonEqCalendarAxis(headinfo.TimeAxis.TimeUnit, starttime);
            dfsbuilder.SetTemporalAxis(timeaxis);

            //设置地图投影和数据类型
            dfsbuilder.SetGeographicalProjection(headinfo.Projection);
            dfsbuilder.SetDataType(headinfo.DataType);

            //使用DfsBuilder对象的相应方法增加相应的动态项目
            DfsDynamicItemBuilder dynitem = dfsbuilder.CreateDynamicItemBuilder();
            //设置动态项目名称和数据单位
            dynitem.Set(dfsfile.ItemInfo[itemnumber].Name, dfsfile.ItemInfo[itemnumber].Quantity, dfsfile.ItemInfo[itemnumber].DataType);

            //设置动态项目的空间轴,如果采用原项目的空间轴，则会有几万个网格元素
            IDfsSpatialAxis spatialaxis = dfsfact.CreateAxisEqD0();
            dynitem.SetAxis(spatialaxis);

            //设置动态项目数据值类型（如累积、单步、单步累积等）
            dynitem.SetValueType(dfsfile.ItemInfo[itemnumber].ValueType);

            //最后采用DfsDynamicItemBuilder类的GetDynamicItemInfo方法返回动态项目信息
            IDfsDynamicItemInfo dyiteminfo = dynitem.GetDynamicItemInfo();

            //使用DfsBuilder对象的相应方法增加相应的动态项目
            dfsbuilder.AddDynamicItem(dyiteminfo);

            return dfsbuilder;
        }
        #endregion ***********************************************************************************************


        #region **************************** 根据参数创建dfs0文件******************************************

        //创建常用类型时间序列dfs0文件(统一按不等距绝对时间)
        public static void Dfs0_Creat(string outputfilename, Dictionary<DateTime, double> inputdic, dfs0type dfs0_type)
        {
            //头参数
            DfsFactory factory = new DfsFactory();
            IDfsProjection project = factory.CreateProjectionUndefined();  //设置头信息的地图投影

            Dfscreate_headprs headprs;
            if (dfs0_type != dfs0type.hydrograph)  //除了单位线外的其他项目
            {
                headprs = Dfs0.Getdfsheadprs(Path.GetFileNameWithoutExtension(outputfilename), "MIKE Zero", 100, project, eumUnit.eumUsec, inputdic.ElementAt(0).Key,
                              timetype.Non_Equidistant_Calendar, inputdic.Count, 3600, headdatetype.dfs0_1_2_Mikezero);  //这里的10 和3600由于采取的是不等距绝对时间，故并没起作用
            }
            else   //单位线--用相对等距时间轴
            {
                headprs = Dfs0.Getdfsheadprs(Path.GetFileNameWithoutExtension(outputfilename), "MIKE Zero", 100, project, eumUnit.eumUsec, inputdic.ElementAt(0).Key,
                              timetype.Equidistant_relative, inputdic.Count, 3600, headdatetype.dfs0_1_2_Mikezero);
            }

            //项目参数
            DfsFactory dfsfact = new DfsFactory();
            IDfsSpatialAxis spatialaxis = dfsfact.CreateAxisEqD0();  //设置项目的空间坐标轴

            Dfscreate_itemprs itemprs;
            switch (dfs0_type)
            {
                case dfs0type.waterlevel:
                    itemprs = Dfs0.Getdfsitemprs("waterlevel", eumItem.eumIWaterLevel, eumUnit.eumUmeter, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                case dfs0type.discharge:
                    itemprs = Dfs0.Getdfsitemprs("discharge", eumItem.eumIDischarge, eumUnit.eumUm3PerSec, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                case dfs0type.rainfall:
                    itemprs = Dfs0.Getdfsitemprs("rainfall", eumItem.eumIRainfall, eumUnit.eumUmillimeter, DfsSimpleType.Float, DataValueType.StepAccumulated, spatialaxis);
                    break;
                case dfs0type.evaporation:
                    itemprs = Dfs0.Getdfsitemprs("evaporation", eumItem.eumIEvaporation, eumUnit.eumUmillimeter, DfsSimpleType.Float, DataValueType.StepAccumulated, spatialaxis);
                    break;
                case dfs0type.water_depth:
                    itemprs = Dfs0.Getdfsitemprs("water_depth", eumItem.eumIWaterDepth, eumUnit.eumUmeter, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                case dfs0type.water_vector:
                    itemprs = Dfs0.Getdfsitemprs("water_vector", eumItem.eumIuVelocity, eumUnit.eumUmeterPerSec, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                case dfs0type.hydrograph:
                    itemprs = Dfs0.Getdfsitemprs("hydrograph", eumItem.eumIUnitHydrographOrdinate, eumUnit.eumUm3PerSecPer10mm, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                case dfs0type.concentration:
                    itemprs = Dfs0.Getdfsitemprs("concentration", eumItem.eumIConcentration, eumUnit.eumUmilliGramPerL, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                case dfs0type.gatelevel:
                    itemprs = Dfs0.Getdfsitemprs("gatelevel", eumItem.eumIGateLevel, eumUnit.eumUmeter, DfsSimpleType.Float, DataValueType.Instantaneous, spatialaxis);
                    break;
                default:
                    itemprs = Dfs0.Getdfsitemprs("rainfall", eumItem.eumIRainfall, eumUnit.eumUmillimeter, DfsSimpleType.Float, DataValueType.StepAccumulated, spatialaxis);
                    break;
            }

            //创建dfs文件
            Dfscreateprs dfspars;
            dfspars.headprs = headprs;
            dfspars.itemprs = itemprs;

            //创建dfs0文件
            Dfs0.Dfs0_Creat(outputfilename, inputdic, dfspars);
        }

        //创建dfs0文件,根据键值对集合和参数创建
        public static int Dfs0_Creat(string outputfilename, Dictionary<DateTime, double> inputdic, Dfscreateprs dfspars)
        {
            //根据参数调用自定义的静态方法创建DfsBuilder对象
            DateTime starttime = inputdic.ElementAt(0).Key;
            DfsBuilder dfsbuilder = Getdfsbder(dfspars);

            //用自定义的静态方法完成文件的创建和数据的写入
            int n = Dfs0_Creat1(dfsbuilder, outputfilename, inputdic);
            return n;
        }

        //根据参数创建DfsBuilder对象
        public static DfsBuilder Getdfsbder(Dfscreateprs dfspars)
        {
            //创建DfsBuilder对象，并首先设置地图投影、数据类型和时间轴
            //三个初始化参数：文件名 应用程序标题(MIKE Zero) 版本号(100)
            DfsBuilder dfsbuilder = DfsBuilder.Create(dfspars.headprs.filetitle, dfspars.headprs.apptitle, dfspars.headprs.appversion);

            //新建DfsFactory对象用于各类信息的创建
            DfsFactory dfsfact = new DfsFactory();

            //设置头信息中的地图投影
            dfsbuilder.SetGeographicalProjection(dfspars.headprs.project);

            //设置头信息中的数据类型
            dfsbuilder.SetDataType((int)dfspars.headprs.headdate); //看参考书，0代表全部数据，适用于dfs0

            //设置头信息的时间轴，统一使用秒作为时间数字单位，第一个参数为枚举，第二个为datetime类型的开始时间
            IDfsTemporalAxis timeaxis = null;
            switch (dfspars.headprs.dfstimety)
            {
                case timetype.Equidistant_Calendar:   //等距绝对时间
                    timeaxis = dfsfact.CreateTemporalEqCalendarAxis(dfspars.headprs.timeunit, dfspars.headprs.starttime, dfspars.headprs.timesteps, dfspars.headprs.offtimestart);
                    break;
                case timetype.Equidistant_relative:  //等距相对时间
                    timeaxis = dfsfact.CreateTemporalEqTimeAxis(dfspars.headprs.timeunit, dfspars.headprs.offtimestart, dfspars.headprs.timesteps);
                    break;
                case timetype.Non_Equidistant_Calendar:  //不等距绝对时间
                    timeaxis = dfsfact.CreateTemporalNonEqCalendarAxis(dfspars.headprs.timeunit, dfspars.headprs.starttime);
                    break;
                case timetype.Non_Equidistant_relative:  //不等距相对时间
                    timeaxis = dfsfact.CreateTemporalNonEqTimeAxis(dfspars.headprs.timeunit);
                    break;
            }
            dfsbuilder.SetTemporalAxis(timeaxis);

            //创建新的动态项目对象
            DfsDynamicItemBuilder dynitem = dfsbuilder.CreateDynamicItemBuilder();

            //项目类型，只能是一对一的固定枚举类型(如降雨 和单位mm)
            eumQuantity item_enum = null;
            try
            {
                item_enum = new eumQuantity(dfspars.itemprs.item, dfspars.itemprs.itemunit);
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                return null;
            }

            //设置动态项目名称和数据单位
            dynitem.Set(dfspars.itemprs.itemname, item_enum, dfspars.itemprs.itemdatatype);

            //设置动态项目的空间轴
            dynitem.SetAxis(dfspars.itemprs.itemspatialaxis);

            //设置动态项目数据值类型（如累积、单步、单步累积等）
            dynitem.SetValueType(dfspars.itemprs.itemvaluetype);

            //最后采用DfsDynamicItemBuilder类的GetDynamicItemInfo方法返回动态项目信息
            IDfsDynamicItemInfo dyiteminfo = dynitem.GetDynamicItemInfo();

            //使用DfsBuilder对象的相应方法增加相应的动态项目
            dfsbuilder.AddDynamicItem(dyiteminfo);

            return dfsbuilder;
        }

        //返回头参数
        public static Dfscreate_headprs Getdfsheadprs(string filetitle, string apptitle, int appvers, IDfsProjection project,
            eumUnit timeuint, DateTime starttime, timetype dfstimety, double timesteps, double offtimestart, headdatetype headdate)
        {
            Dfscreate_headprs headprs;
            //标题名，应用程序
            headprs.filetitle = filetitle;
            headprs.apptitle = apptitle;
            headprs.appversion = appvers;

            //地图投影  -- 头信息
            headprs.project = project;

            //时间轴定义 -- 头信息
            headprs.timeunit = timeuint;  //时间轴的时间单位
            headprs.starttime = starttime;      //开始时间
            headprs.dfstimety = dfstimety;     //时间类型
            headprs.timesteps = timesteps;        //时间步
            headprs.offtimestart = offtimestart;  //开始时间偏移

            //头信息数据类型  -- 头信息
            headprs.headdate = headdate; //默认为dfs0
            return headprs;
        }

        //返回动态项目参数
        public static Dfscreate_itemprs Getdfsitemprs(string itemname, eumItem item, eumUnit itemunit,
            DfsSimpleType itemdatatype, DataValueType itemvaluetype, IDfsSpatialAxis itemspatialaxis)
        {
            Dfscreate_itemprs itemprs;

            itemprs.itemname = itemname; //项目名称
            itemprs.item = item;    //项目类型,如rainfall
            itemprs.itemunit = itemunit;  //项目单位,如mm
            itemprs.itemdatatype = itemdatatype; //项目数据类型，默认为double
            itemprs.itemvaluetype = itemvaluetype; //项目数据形式，如累积，均步累积等
            itemprs.itemspatialaxis = itemspatialaxis; //动态项目空间轴
            return itemprs;
        }
        #endregion ****************************************************************************************
    }
}
