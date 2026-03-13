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

    public class Dfs0
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


        #region ************** 更新创建dfs0文件 - 边界条件dfs0、各流域降雨、蒸发和水文单位线 *******************
        // 将基础模型的dfs0全部copy过来,并逐个修正起止时间
        public static void Rewrite_Dfs0copy_UpdateFile(HydroModel hydromodel)
        {
            string model_dirname = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);
            string basemodel_dirname = Path.GetDirectoryName(hydromodel.BaseModel.Modelfiles.Simulate_filename);

            string[] base_dfs0files = Directory.GetFiles(basemodel_dirname, "*.dfs0");

            for (int i = 0; i < base_dfs0files.Length; i++)
            {
                string outputfilename = model_dirname + @"\" + Path.GetFileName(base_dfs0files[i]);
                if (!File.Exists(outputfilename))
                {
                    File.Copy(base_dfs0files[i], outputfilename);

                    //修正起止时间
                    Dictionary<DateTime, double> inputdic = new Dictionary<DateTime, double>();

                    //获取键值对集合
                    Dictionary<DateTime, double> source_dic = Dfs0_Reader_GetItemDic(base_dfs0files[i], 1);

                    //修正起止时间
                    inputdic.Add(hydromodel.ModelGlobalPars.Simulate_time.Begin, source_dic.First().Value);
                    inputdic.Add(hydromodel.ModelGlobalPars.Simulate_time.End, source_dic.Last().Value);

                    //编辑dfs0文件
                    Dfs0_Edit(base_dfs0files[i], outputfilename, inputdic, true);
                }
            }

            Console.WriteLine("DFS0文件COPY和时间修正成功！");
            Console.WriteLine("");
        }

        // 提取最新的各流域和其3个产汇流模型参数，更新dfs0文件,各流域蒸发用一个文件
        //水文单位线文件地址为空，则不创建该dfs0
        public static void Rewrite_Dfs0_UpdateFile(HydroModel hydromodel)
        {
            //文件夹地址
            string dirname = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);

            //各流域各项目的dfs0文件路径
            List<CatchmentInfo> catchment_list = hydromodel.RfPars.Catchmentlist.Catchment_infolist;
            if (catchment_list == null) return;
            if (catchment_list.Count == 0) return;

            string[] rf_filename = new string[catchment_list.Count];   //降雨文件路径
            for (int i = 0; i < catchment_list.Count; i++)
            {
                //降雨dfs0文件地址
                rf_filename[i] = dirname + @"\" + catchment_list[i].Name + "_rf.dfs0";

                //生成各流域的降雨dfs0文件
                Creat_Rainfall_dfs0file(rf_filename[i], hydromodel, catchment_list[i].Name);
            }

            //生成各流域统一蒸发dfs0文件
            string evp_filename = dirname + @"\" + "evp.dfs0";
            Create_Eva_dfs0file(evp_filename, hydromodel);

            //生成水文单位线
            string[] uhm_filename = new string[catchment_list.Count];   //单位线文件路径
            for (int i = 0; i < catchment_list.Count; i++)
            {
                //单位线dfs0文件地址
                rf_filename[i] = dirname + @"\" + catchment_list[i].Name + "_uhm.dfs0";

                //根据流域名从数据库获取水文单位线数据，生成水文单位线dfs0文件
                Creat_Uhm_dfs0file(rf_filename[i], catchment_list[i].Name);
            }

            Console.WriteLine("Dfs0降雨蒸发单位线时间序列文件更新成功!");
            Console.WriteLine("");
        }

        //创建各流域的单位线dfs0文件
        public static void Creat_Uhm_dfs0file(string outputfilename, string catchmentname)
        {
            //根据流域获取单位线序列
            Dictionary<int, double> Catchment_UHMdic = RFService.GetCatchment_UHMdic(catchmentname);

            //如果没有获取到，则先按默认的，并提醒前端
            if (Catchment_UHMdic.Count == 0 || Catchment_UHMdic == null)
            {
                Catchment_UHMdic = RFService.GetCatchment_UHMdic(Model_Const.DEFAULT_CATCHMENTNAME);
                Console.WriteLine("该流域的单位线不存在，暂按默认单位线！");
            }

            //创建单位线序列文件
            Dictionary<DateTime, double> datetimedic = new Dictionary<DateTime, double>();
            DateTime datetime = new DateTime();
            for (int i = 0; i < Catchment_UHMdic.Count; i++)
            {
                int hour = Catchment_UHMdic.Keys.ElementAt(i);
                double discharge = Catchment_UHMdic.Values.ElementAt(i);
                datetimedic.Add(datetime.AddHours(hour), discharge);
            }
            Dfs0.Dfs0_Creat(outputfilename, datetimedic, dfs0type.hydrograph);
        }

        //创建各流域的降雨dfs0文件
        public static void Creat_Rainfall_dfs0file(string outputfilename, HydroModel hydromodel, string catchmentname)
        {
            //根据前端选择的不同计算类型相应获取降雨序列
            Dictionary<DateTime, double> rainfalldic = GetRFdic_FromDb(hydromodel, catchmentname);

            //构建降雨时间序列文件
            Dfs0.Dfs0_Creat(outputfilename, rainfalldic, dfs0type.rainfall);
        }

        //根据洪水计算类型选择从数据库相应获取降雨时间序列
        public static Dictionary<DateTime, double> GetRFdic_FromDb(HydroModel hydromodel, string catchmentname)
        {
            SimulateTime simulatetime = hydromodel.ModelGlobalPars.Simulate_time;

            //根据前端选择的不同计算类型相应获取降雨序列
            DateTime starttime = simulatetime.Begin;
            DateTime endtime = simulatetime.End;
            Dictionary<DateTime, double> rainfalldic = new Dictionary<DateTime, double>();

            SimulateFloodType simulateflood_type = hydromodel.ModelGlobalPars.Simulate_floodtype;
            switch (simulateflood_type)
            {
                case SimulateFloodType.Design:               //设计洪水计算

                    //从基础资料数据库(basedata)的设计降雨表(design_rainfall)读取设计降雨时间序列
                    Dictionary<DateTime, double> design_rfdic = RFService.GetDesign_RFdic(hydromodel.RfPars.Designrf_info);
                    rainfalldic = Dfs0.Dfs0_AddToEND_Usevalue0(design_rfdic, endtime);
                    break;
                case SimulateFloodType.History:              //历史洪水计算  

                    //通过判断后会从基础资料数据库(basedata)的历史降雨表(design_rainfall)中读取历史降雨时间序列（只能到2012.12.31）
                    Dictionary<DateTime, double> history_rfdic = RFService.Get_CatchmentRainfall(catchmentname, starttime, endtime, 1);
                    rainfalldic = history_rfdic;
                    break;
                case SimulateFloodType.Realtime_Forecast:    //实时预报洪水计算

                    //获取实时预报洪水序列
                    rainfalldic = Get_Realtime_Rfdic(hydromodel, catchmentname);
                    break;
            }
            return rainfalldic;
        }

        //首先从数据库获取实时预报洪水的前半段序列，然后根据预报时长和雨量补齐后面的
        public static Dictionary<DateTime, double> Get_Realtime_Rfdic(HydroModel hydromodel, string catchmentname)
        {
            //模拟起止时间
            SimulateTime simulatetime = hydromodel.ModelGlobalPars.Simulate_time;
            DateTime starttime = simulatetime.Begin;
            DateTime endtime = simulatetime.End;

            //获取未来预报降雨和雨形模板套用信息

            Dictionary<DateTime, double> modeldic = CatchmentList.Stand24_Rfmodel;
            TimeSpan future_timespan = hydromodel.RfPars.Forecast_future_rftimespan;
            double totalrain = hydromodel.RfPars.Forecast_future_rfaccumulated;
            Dictionary<DateTime, double> typedefdic = hydromodel.RfPars.Forecast_typedef_rfmodeldic;

            Dictionary<DateTime, double> rainfalldic = new Dictionary<DateTime, double>();
            //先判断模拟时间是否全部在未来
            if (starttime.Subtract(DateTime.Now).TotalHours < 0)   //部分在未来
            {
                //通过判断后从会从外部实时雨量站数据库(hydrological)的降雨表(hy_dcp_d)中读取实时降雨时间序列
                Dictionary<DateTime, double> rfdic = RFService.Get_CatchmentRainfall(catchmentname, starttime, endtime, 1);

                //截取数据库读取的序列的 开始到现在 的时间段
                DateTime nowdate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                Dictionary<DateTime, double> rfdic1 = Dfs0.Get_Subdic(rfdic, starttime, nowdate);

                //降雨序列后半段用预报的补齐
                if (hydromodel.RfPars.Forecast_future_rfaccumulated != 0)   //如果预报未来累积降雨量不为0
                {
                    if (hydromodel.RfPars.Forecast_rfmodel_type == forecast_rfmodeltype.default_model)   //未来的降雨过程采用默认模板套用
                    {
                        rainfalldic = Dfs0.Create_Rfdic_addfuture(modeldic, false, rfdic1, future_timespan, totalrain);
                    }
                    else  //未来的降雨过程采用自定义过程套用
                    {
                        rainfalldic = Dfs0.Create_Rfdic_addfuture(rfdic1, typedefdic, totalrain);
                    }

                    //后面不到模拟时间的话还要补0
                    if (rainfalldic.Keys.ElementAt(rainfalldic.Count - 1) != endtime)
                    {
                        rainfalldic = Dfs0.Dfs0_AddToEND_Usevalue0(rainfalldic, endtime);
                    }
                }
                else  //如果预报未来累积降雨量=0 则后半段全部补0
                {
                    rainfalldic = Dfs0.Dfs0_AddToEND_Usevalue0(rfdic1, endtime);
                }
            }
            else       //模拟时间完全在未来
            {
                if (hydromodel.RfPars.Forecast_rfmodel_type == forecast_rfmodeltype.default_model)   //未来的降雨过程采用默认模板套用
                {
                    rainfalldic = Dfs0.Creat_Rfdic_future(modeldic, false, future_timespan, totalrain);
                }
                else  //未来的降雨过程采用自定义过程套用
                {
                    rainfalldic = Dfs0.Creat_Rfdic_future(typedefdic, totalrain);
                }

                //后面不到模拟时间的话还要补0
                if (rainfalldic.Keys.ElementAt(rainfalldic.Count - 1) != endtime)
                {
                    rainfalldic = Dfs0.Dfs0_AddToEND_Usevalue0(rainfalldic, endtime);
                }
            }

            return rainfalldic;
        }

        //创建蒸发dfs0文件
        public static void Create_Eva_dfs0file(string outputfilename, HydroModel hydromodel)
        {
            SimulateTime simulatetime = hydromodel.ModelGlobalPars.Simulate_time;

            //获取蒸发时间序列,如果模拟时间在历史已有蒸发数据时间范围内则直接截取，否则采用历史各月的小时平均值
            DateTime starttime = simulatetime.Begin;
            DateTime endtime = simulatetime.End;
            Dictionary<DateTime, double> evadic = CatchmentList.Get_Average_HourEvaRfRate(starttime, endtime, Eva_RF.eva);

            //构建蒸发dfs0文件
            Dfs0.Dfs0_Creat(outputfilename, evadic, dfs0type.evaporation);
        }

        //创建各流域XAJ模拟结果（流量过程）的dfs0文件
        public static void Creat_XAJres_Dfs0(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, double>> Catchment_XAJresdic)
        {
            for (int i = 0; i < Catchment_XAJresdic.Count; i++)
            {
                string catchmentname = Catchment_XAJresdic.Keys.ElementAt(i);
                Dictionary<DateTime, double> Catchment_resdic = Catchment_XAJresdic.Values.ElementAt(i);
                string outputfilename = Path.GetDirectoryName(hydromodel.Modelfiles.XAJres_filename) + @"\" + catchmentname + "_XAJres.dfs0";

                Dfs0_Creat(outputfilename, Catchment_resdic, dfs0type.discharge);
            }
        }

        #endregion *********************************************************************************


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