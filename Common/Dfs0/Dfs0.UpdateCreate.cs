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
    }
}
