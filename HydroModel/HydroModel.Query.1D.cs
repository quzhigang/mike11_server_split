using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.Diagnostics;
using System.IO;
using DHI.PFS;
using Kdbndp;

using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike11;
using bjd_model.Mike21;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;


namespace bjd_model
{
    public partial class HydroModel
    {
        #region 一维断面数据查询操作 -- 从Xns11文件和数据库中查询
        //从数据库中获取指定河道的左右堤顶高程、地面高程和河底高程   ** 静态方法 **
        public static ReachSection_Altitude Get_Altitude_FromDB(AtReach atreach)
        {
            ReachSection_Altitude altitude = Xns11.Get_Altitude(atreach);
            return altitude;
        }

        //获取指定河道的左右堤顶高程、地面高程和河底高程 -- 从模型参数中获取
        public ReachSection_Altitude Get_Altitude(AtReach atreach)
        {
            ReachSection_Altitude altitude = this.Mike11Pars.SectionList.Get_Altitude(atreach);
            return altitude;
        }

        //获取河道所有断面桩号集合 -- 从模型参数中获取
        public List<double> Get_Reach_SecChainageList(string reachname)
        {
            return this.Mike11Pars.SectionList.Get_Reach_SecChainageList(reachname);
        }

        //获取河段内的断面桩号集合 -- 从模型参数中获取
        public List<double> Get_SegReach_SecChainageList(Reach_Segment reach_segment)
        {
            return this.Mike11Pars.SectionList.Get_SegReach_SecChainageList(reach_segment);
        }

        //获取河道指定断面的断面数据 -- 从模型参数中获取
        public List<PointXZS> Get_Sectiondata(AtReach atreach)
        {
            return this.Mike11Pars.SectionList.Get_Sectiondata(atreach);
        }

        //获取河道指定断面的断面数据 -- 根据点从模型断面数据库中获取
        public static Dictionary<string,object> Get_Sectiondata_FromPoint(PointXY p)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            //转为投影坐标
            PointXY pro_p = PointXY.CoordTranfrom(p, 4326, 4547, 3);

            //从默认模型文件中初始化河道数据
            ReachList reachlist = new ReachList();
            string default_modeldir = Model_Files.Get_Modeldir(Model_Const.DEFAULT_MODELNAME);
            string nwk11_file = File_Common.Get_MaxFile(default_modeldir, ".nwk11");

            Nwk11.GetDefault_ReachInfo(nwk11_file, ref reachlist);
            AtReach atreach = Nwk11.Get_NearReach(reachlist, pro_p);
            if (atreach.chainage == -1) return null;

            //断面如果不在断面上，则获取最近的断面
            List<PointXZS> sectiondata = Xns11.Get_SectionData(atreach.reachname, atreach.chainage);

            //转换格式
            List<double[]> sectiondata1 = new List<double[]>();
            for (int i = 0; i < sectiondata.Count; i++)
            {
                sectiondata1.Add(new double[] { sectiondata[i].X, sectiondata[i].Z });
            }

            //断面数据排序
            List<double[]> sectiondata2 = sectiondata1.OrderBy(arr => arr[0]).ToList();

            //获取河道中文名
            string reach_cnname = Item_Info.Get_ReachChinaName(atreach.reachname);
            double chainage = atreach.chainage;

            res.Add("reach_name", reach_cnname);
            res.Add("chainage", chainage);
            res.Add("section_data", sectiondata2);
            return res;
        }

        //获取河道出口的水位流量关系 -- 从断面文件中计算
        public Dictionary<double, double> Get_ReachOut_QH(string reachname)
        {
            string Xns11_sourcefilename = this.Modelfiles.Xns11_filename;
            return Xns11.Get_ReachEnd_QH(Xns11_sourcefilename, reachname, reachname + Model_Const.REACHID_HZ);
        }
        #endregion


        #region 一维结果查询操作 -- 一维河道结果、产汇流结果、水量平衡结果等
        //查询河道指定点 全部时间结果数据（水位、水深、流速、流量）
        public static Dictionary<DateTime, mike11_res> Get_Mike11Point_AllRes(string plan_code,PointXY p)
        {
            return Res11.Get_Mike11PointRes(plan_code, p);
        }

        //查询河道指定点的某时刻 结果数据（水位、水深、流速、流量）
        public static mike11_res Get_Mike11Point_Res(string plan_code, PointXY p,DateTime time)
        {
            Dictionary<DateTime, mike11_res> mike11_res_dic = HydroModel.Get_Mike11Point_AllRes(plan_code, p);

            mike11_res res = mike11_res_dic.Keys.Contains(time)? mike11_res_dic[time]:new mike11_res();
            return res;
        }

        //查询指定点的指定项结果数据
        public Dictionary<DateTime, double> Get_Mike11Point_SingleRes(PointXY p, mike11_restype itemtype)
        {
            HydroModel hydromodel = this;
            return Res11.Res11reader(hydromodel.Modelname, p, itemtype);
        }

        //查询指定河道断面桩号的指定项
        public Dictionary<DateTime, double> Get_Mike11Section_SingleRes(AtReach atreach, mike11_restype itemtype)
        {
            HydroModel hydromodel = this;
            return Res11.Res11reader(hydromodel.Modelname, atreach, itemtype);
        }

        //查询指定流域的指定结果项(径流流量或净雨)
        public Dictionary<DateTime, double> Get_RRCatchment_SingleRes(string catchmentname, RF_restype restype)
        {
            HydroModel hydromodel = this;
            if (this.RfPars.Rainfall_selectmodel == RFModelType.XAJ)
            {
                if (restype == RF_restype.NetRainfall)
                {
                    Console.WriteLine("新安江模拟没有净雨深结果！");
                    return null;
                }
                return Res11.Get_XAJresdic(hydromodel, catchmentname);
            }
            else
            {
                return Res11.Res11reader(hydromodel, catchmentname, restype);
            }
        }

        //查询指定流域的所有统计值 -- 最大径流量，出现的时间，总径流量，总净雨深
        public rainfallstatist Get_RRCatchment_StatistRes(string catchmentname)
        {
            HydroModel hydromodel = this;
            return Res11.Res11reader(hydromodel, catchmentname);
        }

        //获取模型水量平衡结果 -- 包括一维河道或二维网格水量
        public water_volume_balance Get_WaterVolume_BalanceRes(Mike11_Mike21 mike11_mike21)
        {
            HydroModel hydromodel = this;
            return Res11.Htmlreader(hydromodel, mike11_mike21);
        }

        //将特征点一维结果写入数据库
        public void Write_Mike11TxtRes_ToDB()
        {
            HydroModel hydromodel = this;
            Res11.Writejgtxt_ToMysql(hydromodel);
        }


        //*****************模型结果后处理**************************
        //获取格网预报降雨风险
        public static Warning_Info Get_Rain_WarningInfo(string plan_code)
        {
            //获取模型信息
            Dictionary<string, string> model_info = HydroModel.Get_Model_Info_FromDB(plan_code);
            string start_time = model_info["start_time"];
            string end_time = SimulateTime.TimetoStr(SimulateTime.StrToTime(start_time).AddHours(24));

            //未来24小时格网预报降雨和雨强
            string request_url = Mysql_GlobalVar.forecast_rain_serverurl + "?st=" + start_time+ "&ed=" + end_time;
            string response_res = File_Common.Get_HttpReSponse(request_url);
            dynamic jsonObject = JsonConvert.DeserializeObject(response_res);
            JObject json_obj = JObject.Parse(jsonObject["data"].ToString());

            //遍历其属性和值
            Dictionary<string, object> res_list = new Dictionary<string, object>();
            foreach (var property in json_obj.Properties())
            {
                string item = property.Name;
                object value = property.Value;
                res_list.Add(item, value);
            }

            //降雨过程值
            JArray rainArray = res_list["v"] as JArray;
            List<double> rain_value = rainArray.ToObject<List<double>>();
            double total_rain = Math.Round(rain_value.Sum(),2);
            double max_rain = Math.Round(rain_value.Max(),2);

            //预警分级和预警信息
            Warning_Level warning_level = Warning_Level.blue_warining;
            string warning_desc = "";
            if (total_rain > 200)
            {
                warning_level = Warning_Level.crimson_warining;

                warning_desc = $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }
            else if (total_rain > 100)
            {
                warning_level = Warning_Level.red_warining;

                warning_desc = $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }
            else if (total_rain > 50)
            {
                warning_level = Warning_Level.yellow_warining;
                warning_desc = $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }
            else
            {
                warning_level = Warning_Level.blue_warining;
                
                warning_desc = total_rain == 0? $"预报未来24小时无降雨":
                    $"预报未来24小时累积降雨{total_rain} mm,最大雨强{max_rain} mm/h";
            }

            List<Warning_Info> rain_station_warning = new List<Warning_Info>();
            Warning_Info warning_info = new Warning_Info("降雨预警", warning_level, warning_desc, rain_station_warning);

            return warning_info;
        }

        //从预报方案结果中，获取预警风险信息，包括降雨、水库、河道、蓄滞洪区进洪、南水北调、山洪灾害预警信息 
        public static List<Warning_Info> Get_WarningInfo(string plan_code)
        {
            List<Warning_Info> res = new List<Warning_Info>();

            //获取一维模型结果
            Dictionary<string, object> mike11_res = HydroModel.Get_Mike11_TjResult(plan_code);
            if (mike11_res == null) return res;
            Dictionary<string, Reservoir_FloodRes> res_results = mike11_res["reservoir_result"] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, ReachSection_FloodRes> reach_result = mike11_res["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = mike11_res["floodblq_result"] as Dictionary<string, Xzhq_FloodRes>;
            Dictionary<string, object> reach_risk = mike11_res["reach_risk"] as Dictionary<string, object>;
            string result_desc = mike11_res["result_desc"] as string;
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //1、获取水库预警信息
            Warning_Info res_warninginfo = Res11.Get_AllRes_Warninginfo(plan_code, res_results,now_waterlevel);
            res.Add(res_warninginfo);

            //2、获取河道预警信息
            Warning_Info reach_warninginfo = Res11.Get_Reach_Warninginfo(plan_code, reach_result, reach_risk, now_waterlevel);
            res.Add(reach_warninginfo);

            //3、获取蓄滞洪区进洪预警信息
            Warning_Info xzhq_warninginfo = Res11.Get_Xzhq_Warninginfo(plan_code, xzhq_result);
            res.Add(xzhq_warninginfo);

            //4、获取南水北调预警信息
            Warning_Info nsbd_warninginfo = Res11.Get_Nsbd_Warninginfo(plan_code, reach_risk);
            res.Add(nsbd_warninginfo);

            //5、获取山洪预警信息
            Warning_Info shzh_warninginfo = Res11.Get_Sh_Warninginfo(plan_code, reach_result);
            res.Add(shzh_warninginfo);



            return res;
        }

        //从方案结果中，获取水库、河道、蓄滞洪区存在高风险的重点巡查部位
        public static Dictionary<string, List<Important_Inspect_UnitInfo>> Get_Important_Inspect_Parts(HydroModel model)
        {
            Dictionary<string, List<Important_Inspect_UnitInfo>> inspect_list = new Dictionary<string, List<Important_Inspect_UnitInfo>>();

            //获取一维模型结果
            Dictionary<string, object> mike11_res = HydroModel.Get_Mike11_TjResult(model.Modelname);
            if (mike11_res == null) return inspect_list;
            Dictionary<string, Reservoir_FloodRes> res_results = mike11_res["reservoir_result"] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, ReachSection_FloodRes> reach_result = mike11_res["reachsection_result"] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = mike11_res["floodblq_result"] as Dictionary<string, Xzhq_FloodRes>;
            Dictionary<string, object> reach_risk = mike11_res["reach_risk"] as Dictionary<string, object>;
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //1、获取水库类型 重点巡查的大中型水库
            List<Important_Inspect_UnitInfo> res_inspect_list = Res11.Get_AllRes_Inspectinfo(model, res_results, now_waterlevel);
            inspect_list.Add("水库", res_inspect_list);

            //2、获取河道类型 重点巡查的河段清单
            List<Important_Inspect_UnitInfo> reach_inspect_list = Res11.Get_Reach_Inspectinfo(model, reach_result, reach_risk, now_waterlevel);
            inspect_list.Add("河道", reach_inspect_list);

            //3、获取蓄滞洪区类型 重点巡查的蓄滞洪区
            List<Important_Inspect_UnitInfo> xzhq_inspect_list = Res11.Get_Xzhq_Inspectinfo(model, xzhq_result);
            inspect_list.Add("蓄滞洪区", xzhq_inspect_list);

            return inspect_list;
        }

        //获取模型的所有mike11结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_Result(string plan_code)
        {
            return Res11.Load_Res11(plan_code);
        }

        //获取模型的mike11全过程结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_GcResult(string plan_code)
        {
            Dictionary<string, object> res = Res11.Load_Res11_GC(plan_code);
            return res;
        }

        //获取模型mike11 统计结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_TjResult(string plan_code)
        {
            Dictionary<string, object> res = Res11.Load_Res11_TJ(plan_code);
            return res;
        }

        //获取模型指定类别的mike11纵剖面结果信息 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_ZPResult(string plan_code, string res_type)
        {
            Dictionary<string, object> res = Res11.Load_Res11_Part3(plan_code, res_type);
            return res;
        }

        //获取模型指定时刻的mike11纵剖面结果详表 -- 从数据库，用于前端获取
        public static Dictionary<string, object> Get_Mike11_XbResult(string plan_code)
        {
            Dictionary<string, object> res = Res11.Load_Res11_Part5(plan_code);
            return res;
        }

        //获取河道水位断面详表 -- 从数据库，用于前端获取
        public static Dictionary<string, List<float>> Get_ReachSections(string plan_code)
        {
            Dictionary<string, List<float>> res = Res11.Load_ReachSections(plan_code);
            return res;
        }

        //获取单一河道断面的水位流量过程(优先数据库，没有就读结果文件)
        public static Dictionary<string, Dictionary<DateTime, float>> Get_SectionRes(string plan_code, AtReach atreach)
        {
            Dictionary<string, Dictionary<DateTime, float>> res = Res11.Load_SectionRes_FromDB(plan_code, atreach);
            return res;
        }

        //获取模型指定时刻的 mike11 GIS结果信息 -- 从数据库，用于前端获取
        public static string Get_Mike11Gis_Result(string plan_code,string model_instance,Mike11FloodRes_GisType res_gistype, string time = "")
        {
            if(res_gistype == Mike11FloodRes_GisType.reach_centerline)
            {
                string field_name = "line_sample_result";
                return Res11.Load_Sample_GisRes(field_name, model_instance);
            }
            else
            {
                return Res11.Load_Reach_GisPolygon_Res(plan_code, model_instance, time);
            }
        }

        //获取模型 mike11 GIS统计结果 -- 从数据库，用于前端获取
        public static string Get_Mike11GisTJ_Result(string plan_code)
        {
            return Res11.Load_Res11Gis(plan_code);
        }

        //获取模型的所有mike11结果信息 -- 从结果文件,用于模型结果保存,在模型计算结果出来后调用
        public List<object> Get_Mike11_AllResult(string model_instance, bool is_quick = false)
        {
            List<object> model_allresult = new List<object>();
            Stopwatch sw = new Stopwatch();
            HydroModel model = this;

            //判断
            if (model == null || !File.Exists(model.Modelfiles.Hdres11_filename)) return null;

            //提前读取模型结果
            ResultData resdata = new ResultData();
            resdata.Connection = Connection.Create(model.Modelfiles.Hdres11_filename);
            Diagnostics diagn = new Diagnostics();
            resdata.Load(diagn);
            IDataItem dataitem = resdata.Reaches.ElementAt(0).DataItems.ElementAt(0);
            if (dataitem == null) return null;

            //如果是自动预报模型，临时修改模型提前模拟时间，从而减少时间裁剪
            //int model_ahead_hours = model.ModelGlobalPars.Ahead_hours;
            //if(model.Modelname == Model_Const.AUTO_MODELNAME) model.ModelGlobalPars.Ahead_hours = Model_Const.AUTOFORECAST_AHEAD_HOURS;

            //水库洪水结果
            sw.Start();
            Dictionary<string, Reservoir_FloodRes> res_result = Res11.Get_AllReservoir_FloodRes(model, resdata,model_instance);
            model_allresult.Add(res_result);
            sw.Stop();
            Console.WriteLine("0水库洪水结果查询完毕！耗时：{0}", sw.Elapsed);

            //河道特征断面洪水结果
            sw.Restart();
            Dictionary<string, ReachSection_FloodRes> reach_result = Res11.Get_ReachSection_FloodRes(model, resdata, model_instance);
            model_allresult.Add(reach_result);
            sw.Stop();
            Console.WriteLine("1河道特征断面洪水结果查询完毕！耗时：{0}", sw.Elapsed);

            //蓄滞洪区洪水结果
            sw.Restart();
            Dictionary<string, Dictionary<DateTime, string>> sub_xzhq_res;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = Res11.Get_Xzhq_FloodRes(model, resdata,out sub_xzhq_res);
            model_allresult.Add(xzhq_result);
            sw.Stop();
            Console.WriteLine("2蓄滞洪区洪水结果查询完毕！耗时：{0}", sw.Elapsed);

            //如果是快速模拟，则这些结果全部为空
            if (is_quick)
            {
                Add_Null_ZDres(ref model_allresult);
            }
            else
            {
                //水位纵断(H点)
                sw.Restart();
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> LevelZdmHD_Data = model.Get_LevelZdm_Data(resdata, model_instance);
                Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float1 = Res11.Get_FloatDicAll(LevelZdmHD_Data); //轻量化一下再存，会快点
                model_allresult.Add(ZdmHD_Data_Float1);
                sw.Stop();
                Console.WriteLine("3水位纵断查询完毕！耗时：{0}", sw.Elapsed);

                //流量纵断(Q点插值为H点)
                sw.Restart();
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> DischargeZdmHD_Data = model.Get_DischargeZdm_Data(resdata, model_instance);
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> New_DischargeZdmHD_Data = Res11.Insert_Qzd_FromHzd(LevelZdmHD_Data, DischargeZdmHD_Data);
                Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float2 = Res11.Get_FloatDicAll(New_DischargeZdmHD_Data); //轻量化一下再存，会快点
                model_allresult.Add(ZdmHD_Data_Float2);
                sw.Stop();
                Console.WriteLine("4流量纵断查询完毕！耗时：{0}", sw.Elapsed);

                //流速纵断(H点)
                sw.Restart();
                ResultData resdata1 = new ResultData();
                resdata1.Connection = Connection.Create(model.Modelfiles.Hd_addres11_filename);
                Diagnostics diagn1 = new Diagnostics();
                resdata1.Load(diagn1);
                Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> SpeedZdmHD_Data = model.Get_SpeedZdm_Data(resdata1, model_instance);
                Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float3 = Res11.Get_FloatDicAll(SpeedZdmHD_Data); //轻量化一下再存，会快点
                model_allresult.Add(ZdmHD_Data_Float3);
                sw.Stop();
                Console.WriteLine("5流速纵断查询完毕！耗时：{0}", sw.Elapsed);

                //堤顶和渠底纵断(H点)
                sw.Restart();
                DateTime[] timearray = LevelZdmHD_Data.First().Value.Keys.ToArray();
                Dictionary<string, double[]> grid = Res11.Get_Reach_GridChainages(LevelZdmHD_Data);
                Dictionary<string, List<double>> Zdm_dd_Data = model.Get_Zdmdd_Data(grid);
                Dictionary<string, List<double>> Zdm_qd_Data = model.Get_Zdmqd_Data(grid);
                Dictionary<string, List<double>> Zdm_td_Data = model.Get_Zdmtd_Data(grid);

                Dictionary<string, List<float>> Zdmdd_Data = Res11.Get_FloatDic(Zdm_dd_Data);
                Dictionary<string, List<float>> Zdmqd_Data = Res11.Get_FloatDic(Zdm_qd_Data);
                Dictionary<string, List<float>> Zdmtd_Data = Res11.Get_FloatDic(Zdm_td_Data);

                model_allresult.Add(Zdmdd_Data);
                model_allresult.Add(Zdmqd_Data);
                sw.Stop();
                Console.WriteLine("67堤顶渠底纵断查询完毕！耗时：{0}", sw.Elapsed);

                //H点桩号序列
                sw.Restart();
                Dictionary<string, List<double>> H_Chainage = Res11.Get_Reach_GridChainages1(LevelZdmHD_Data);
                Dictionary<string, List<float>> H_Chainage_Float = Res11.Get_FloatDic(H_Chainage); //轻量化一下再存，会快点
                model_allresult.Add(H_Chainage_Float);
                sw.Stop();
                Console.WriteLine("8桩号序列查询完毕！耗时：{0}", sw.Elapsed);

                //语音结果
                sw.Restart();
                Dictionary<DateTime, string> ResultSoundFiles = new Dictionary<DateTime, string>(); //暂缓 model.Get_ResultSoundFiles(gate_totaldischarge, tsz_accumulatedV, LevelZdmHD_Data);
                model_allresult.Add(ResultSoundFiles);
                sw.Stop();
                Console.WriteLine("9语音结果生成完毕！耗时：{0}", sw.Elapsed);

                //河道风险统计结果
                sw.Restart();
                Dictionary<string, object> reach_risk = Res11.Get_Reach_RiskResult(model, ZdmHD_Data_Float1, ZdmHD_Data_Float2, ZdmHD_Data_Float3,
                    Zdmdd_Data, Zdmtd_Data, H_Chainage_Float, reach_result, res_result, model_instance);
                model_allresult.Add(reach_risk);
                sw.Stop();
                Console.WriteLine("10河道风险结果统计完毕！耗时:{0}", sw.Elapsed);

                //结果综述
                Dictionary<string, List<float>> level_max = Res11.Get_ZdRes_TimeMaxValue(ZdmHD_Data_Float1);
                string res_desc = Get_Result_Desc(res_result, xzhq_result, level_max, Zdmdd_Data, reach_result);
                model_allresult.Add(res_desc);
                Console.WriteLine("11结果综述生成完毕！耗时:{0}", sw.Elapsed);

                //滩地纵剖
                model_allresult.Add(Zdmtd_Data);
                Console.WriteLine("12滩地纵剖生成完毕！耗时:{0}", sw.Elapsed);

                //子蓄滞洪区启用结果
                Dictionary<DateTime, Dictionary<string, string>> sub_xzhq_res1 = Res11.Get_Dic_Change(sub_xzhq_res);
                model_allresult.Add(sub_xzhq_res1);
                Console.WriteLine("13子蓄滞洪区启用结果！耗时:{0}", sw.Elapsed);
            }

            return model_allresult;
        }

        //空的各纵断结果
        private static void Add_Null_ZDres(ref List<object> model_allresult)
        {
            //水位纵断(H点)
            Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float1 = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            model_allresult.Add(ZdmHD_Data_Float1);

            //流量纵断(Q点插值为H点)
            Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float2 = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            model_allresult.Add(ZdmHD_Data_Float2);

            //流速纵断(H点)
            Dictionary<string, Dictionary<DateTime, List<float>>> ZdmHD_Data_Float3 = new Dictionary<string, Dictionary<DateTime, List<float>>>();
            model_allresult.Add(ZdmHD_Data_Float3);

            //堤顶和渠底纵断(H点)
            Dictionary<string, List<float>> Zdmdd_Data = new Dictionary<string, List<float>>();
            Dictionary<string, List<float>> Zdmqd_Data = new Dictionary<string, List<float>>();
            model_allresult.Add(Zdmdd_Data);
            model_allresult.Add(Zdmqd_Data);

            //H点桩号序列
            Dictionary<string, List<float>> H_Chainage_Float = new Dictionary<string, List<float>>();
            model_allresult.Add(H_Chainage_Float);

            //语音结果
            Dictionary<DateTime, string> ResultSoundFiles = new Dictionary<DateTime, string>();
            model_allresult.Add(ResultSoundFiles);

            //河道风险统计结果
            Dictionary<string, object> reach_risk = new Dictionary<string, object>();
            model_allresult.Add(reach_risk);

            //结果综述
            model_allresult.Add("");

            //滩地纵剖
            Dictionary<string, List<float>> Zdmtd_Data = new Dictionary<string, List<float>>();
            model_allresult.Add(Zdmtd_Data);

            //子蓄滞洪区启用结果
            Dictionary<DateTime, Dictionary<string, string>> sub_xzhq_res1 = new Dictionary<DateTime, Dictionary<string, string>>();
            model_allresult.Add(sub_xzhq_res1);
        }

        //获取结果综述
        public string Get_Result_Desc(Dictionary<string, Reservoir_FloodRes> res_result, Dictionary<string, Xzhq_FloodRes> fhblq_result,
            Dictionary<string, List<float>> level_max, Dictionary<string, List<float>> Zdmdd_Data, Dictionary<string, ReachSection_FloodRes> reach_result)
        {
            //水库风险总结
            string res_resultdesc = Get_Res_ResultDesc(res_result);

            //河道漫堤风险
            string reach_resultdesc = Get_Reach_ResultDesc(level_max, Zdmdd_Data);

            //蓄滞洪区风险总结
            string fhblq_resultdesc = Get_Fhblq_ResultDesc(fhblq_result);

            //出省洪峰流量
            double ycz_outq = reach_result.Keys.Contains("元村")? reach_result["元村"].Max_Qischarge:0;
            string outq_desc = $"4、下游(元村站)出省洪峰流量{Math.Round(ycz_outq)}m³/s。";

            return res_resultdesc + "; " + reach_resultdesc + "; " + fhblq_resultdesc + "; " + outq_desc;
        }

        //保留区风险综述
        public static string Get_Fhblq_ResultDesc(Dictionary<string, Xzhq_FloodRes> fhblq_result)
        {
            Dictionary<string, int> blq_fxres = new Dictionary<string, int>();
            string first_blq_name = ""; int run_blq = 0; int overrun_blq = 0;
            for (int i = 0; i < fhblq_result.Count; i++)
            {
                string blq_cnname = fhblq_result.ElementAt(i).Value.Name;
                Xzhq_FloodRes blq_res = fhblq_result.ElementAt(i).Value;
                if(blq_res.Xzhq_State == "启用" || blq_res.Xzhq_State == "超设计")
                {
                    if (first_blq_name == "") first_blq_name = blq_cnname;
                    run_blq++;
                    if(blq_res.Xzhq_State == "超设计") overrun_blq++;
                }
            }

            string blq_fx_str = $"3、{run_blq}个蓄滞洪区启用，其中{overrun_blq}个超设计启用";

            return blq_fx_str;
        }

        //南水北调交叉断面风险
        private static string Get_Reach_ResultDesc(HydroModel model, Dictionary<string, ReachSection_FloodRes> reach_result,string model_instance)
        {
            //获取河道特征断面  --河道南水北调交叉断面
            List<Water_Condition> section_waterlevel = Res11.Get_ReachSection_Info(model_instance);

            //换一下格式
            Dictionary<string, Water_Condition> section_info = new Dictionary<string, Water_Condition>();
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                section_info.Add(section_waterlevel[i].Reach_CnName, section_waterlevel[i]);
            }

            //获取各断面堤顶高程
            Dictionary<string, double> dd_level = new Dictionary<string, double>();
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                AtReach section_atreach = AtReach.Get_Atreach(section_waterlevel[i].Reach, section_waterlevel[i].Chainage);
                double section_dd = model.Mike11Pars.SectionList.GetReach_Section_MaxminZ(section_atreach).max_z;
                dd_level.Add(section_waterlevel[i].Reach_CnName, section_dd);
            }

            //交叉断面 洪水风险总结
            List<string> secton_level_jj = new List<string>();
            List<string> secton_level_bz = new List<string>();
            List<string> secton_level_md = new List<string>();
            for (int i = 0; i < reach_result.Count; i++)
            {
                string section_name = reach_result.ElementAt(i).Key;   //中文
                double max_level = reach_result.ElementAt(i).Value.Max_Level;
                double level1 = section_info.Keys.Contains(section_name) ? double.Parse(section_info[section_name].Level1):10000;
                double level3 = section_info.Keys.Contains(section_name) ? double.Parse(section_info[section_name].Level3):10000;
                double dd = dd_level.Keys.Contains(section_name) ? dd_level[section_name] : 10000;
                if (max_level > dd)
                {
                    //超堤顶水位
                    secton_level_md.Add(section_name);
                }
                else if (max_level > level3) 
                {
                    //超保证水位
                    secton_level_bz.Add(section_name);
                }
                else if(max_level > level1)
                {
                    //超警戒水位
                    secton_level_jj.Add(section_name);
                }
            }

            string reach_jj_info = ""; 
            for (int i = 0; i < secton_level_jj.Count; i++)
            {
                reach_jj_info += secton_level_jj[i];
                if (i != secton_level_jj.Count - 1) reach_jj_info += "、";
            }
            reach_jj_info = secton_level_jj.Count == 0 ? "无交叉断面河道水位超警戒" : reach_jj_info + "河道水位超警戒";

            string reach_bz_info = ""; 
            for (int i = 0; i < secton_level_bz.Count; i++)
            {
                reach_bz_info += secton_level_bz[i];
                if (i != secton_level_bz.Count - 1) reach_bz_info += "、";
            }
            reach_bz_info = secton_level_bz.Count == 0 ? "无交叉断面河道水位超保证" : reach_bz_info + "河道水位超保证";

            string reach_md_info = "";
            for (int i = 0; i < secton_level_md.Count; i++)
            {
                reach_md_info += secton_level_md[i];
                if (i != secton_level_md.Count - 1) reach_md_info += "、";
            }
            reach_md_info = secton_level_md.Count == 0? "无交叉断面河道水位漫堤" : reach_md_info +"河道水位漫堤";

            string res_desc = "2、南水北调示范段" + reach_jj_info + "，" + reach_bz_info + "，" + reach_md_info;
            return res_desc;
        }

        //河道漫堤风险
        private static string Get_Reach_ResultDesc(Dictionary<string, List<float>> level_max, Dictionary<string, List<float>> Zdmdd_Data)
        {
            //河堤风险总结
            List<string> reach_md = new List<string>();
            for (int i = 0; i < level_max.Count; i++)
            {
                string reach_name = level_max.ElementAt(i).Key;
                List<float> reach_level_max = level_max.ElementAt(i).Value;
                List<float> reach_dd_level = Zdmdd_Data.ElementAt(i).Value;
                string reach_cnname = Item_Info.Get_ReachChinaName(reach_name);
                if (reach_cnname.Contains("连接河") || reach_cnname.Contains("分洪堰") || reach_cnname.Contains("泄洪洞") || reach_cnname.Contains("溢洪") || reach_cnname.Contains("保留区")) continue;
                for (int j = 0; j < reach_level_max.Count; j++)
                {
                    if (reach_level_max[j] >= reach_dd_level[j] - 0.1)
                    {
                        reach_md.Add(reach_cnname);
                        break;
                    }
                }
            }

            string mdreach = "2、";
            for (int i = 0; i < reach_md.Count; i++)
            {
                mdreach += reach_md[i];
                if (i > 2) break;  //只放3条河道
                if (i != reach_md.Count - 1) mdreach += "、";
            }
            if (reach_md.Count > 3) mdreach += "等河道";

            string reach_md_str = reach_md.Count == 0 ? "2、流域各主要河道均不存在漫堤风险" : mdreach + "局部河段存在漫堤风险";

            return reach_md_str;
        }


        //获取水库的结果描述
        public static string Get_Res_ResultDesc(Dictionary<string, Reservoir_FloodRes> res_result)
        {
            //各水库的坝顶高程
            Dictionary<string, Dictionary<double, double>> res_qhrelation = Item_Info.Get_ResHVrelation();
            Dictionary<string, double> res_damlevel = new Dictionary<string, double>();
            for (int i = 0; i < res_qhrelation.Count; i++)
            {
                res_damlevel.Add(res_qhrelation.ElementAt(i).Key, res_qhrelation.ElementAt(i).Value.Keys.Max());
            }

            //各水库的设计水位
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();
            Dictionary<string, double> res_sjlevel = new Dictionary<string, double>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                if (now_waterlevel[i].Datasource == "水库水文站" && now_waterlevel[i].Level3 != "")
                {
                    res_sjlevel.Add(now_waterlevel[i].Name, double.Parse(now_waterlevel[i].Level3));
                }
            }

            //各水库的堰底高程
            Dictionary<string, double> res_yhd_datumn = new Dictionary<string, double>();
            for (int i = 0; i < now_waterlevel.Count; i++)
            {
                if (now_waterlevel[i].Datasource == "水库水文站" && now_waterlevel[i].Level2 != "")
                {
                    res_yhd_datumn.Add(now_waterlevel[i].Name, double.Parse(now_waterlevel[i].Level2));
                }
            }

            //水库风险总结
            int res_md = 0; int res_yh = 0; int res_sj = 0;
            for (int i = 0; i < res_result.Count; i++)
            {
                //漫坝水库数量
                Reservoir_FloodRes res = res_result.ElementAt(i).Value;
                if (res_damlevel.Keys.Contains(res.ResName))
                {
                    if (res.Max_Level > res_damlevel[res.ResName]) res_md++;
                }

                //溢洪水库数量
                if (res_yhd_datumn.Keys.Contains(res.ResName))
                {
                    if (res.Max_Level > res_yhd_datumn[res.ResName]) res_yh++;
                }

                //超设计水位数量
                if (res_sjlevel.Keys.Contains(res.ResName))
                {
                    if (res.Max_Level > res_sjlevel[res.ResName]) res_sj++;
                }
            }
            res_sj = res_sj - res_md;
            res_yh = res_yh - res_sj;

            string res_md_str = res_md == 0 ? "各水库无漫坝风险" : res_md.ToString() + "座水库有漫坝风险";
            string res_yh_str = res_yh == 0 ? "各水库无溢流泄洪" : res_yh.ToString() + "座水库溢流泄洪";
            string res_sj_str = res_sj == 0 ? "各水库未超设计水位" : res_sj.ToString() + "座水库超设计水位";
            string res_str = "1、" + res_md_str + "，" + res_yh_str + "，" + res_sj_str;
            return res_str;
        }

        //获取结果语音播报文件
        public Dictionary<DateTime, string> Get_ResultSoundFiles(Dictionary<string, Dictionary<DateTime, double>> tsz_discharge,
            Dictionary<string, Dictionary<DateTime, double>> tsz_accumulatedV, Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data)
        {
            HydroModel hydromodel = this;
            return Res11.Get_ResultSoundFiles(hydromodel, tsz_discharge, tsz_accumulatedV, ZdmHD_Data);
        }

        //查询渠道总水量
        public Dictionary<string, Dictionary<DateTime, double>> Get_Reach_Volume()
        {
            HydroModel hydromodel = this;
            return Res11.Get_Reach_Volume(hydromodel);
        }

        //统计引水、退水、分水3类的流量过程
        public Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_AllGate_Discharge()
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllGate_Discharge(this);
        }


        //统计引水、退水、分水3类的累积水量过程(根据流量过程统计)
        public Dictionary<string, Dictionary<string, Dictionary<DateTime, double>>> Get_AllGate_AccumulatedVolume(Dictionary<string, 
            Dictionary<string, Dictionary<DateTime, double>>> gate_discharges)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllGate_AccumulatedVolume(gate_discharges);
        }

        //查询水位纵断面数据过程(H点)
        public Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_LevelZdm_Data(ResultData resdata,string model_instance)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Grid_ZdmData(hydromodel, mike11_restype.Water_Level, resdata, model_instance);
        }

        //查询流量纵断面数据过程(Q点)
        public Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_DischargeZdm_Data(ResultData resdata,string model_instance)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Grid_ZdmData(hydromodel,  mike11_restype.Discharge, resdata, model_instance);
        }

        //查询流速纵断面数据过程(H点)
        public Dictionary<string, Dictionary<DateTime, Dictionary<double, double>>> Get_SpeedZdm_Data(ResultData resdata,string model_instance)
        {
            HydroModel hydromodel = this;

            return Res11.Get_AllReach_Grid_ZdmData(hydromodel, mike11_restype.Velocity, resdata, model_instance);
        }

        //查询堤顶纵断面数据(H点)
        public Dictionary<string,List<double>> Get_Zdmdd_Data(Dictionary<string, double[]> grid)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Dd_ZdmData(hydromodel,grid);
        }

        //查询渠底纵断面数据(H点)
        public Dictionary<string, List<double>> Get_Zdmqd_Data(Dictionary<string, double[]> grid)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Qd_ZdmData(hydromodel, grid);
        }

        //查询滩地纵断面数据(H点)
        public Dictionary<string, List<double>> Get_Zdmtd_Data(Dictionary<string, double[]> grid)
        {
            HydroModel hydromodel = this;
            return Res11.Get_AllReach_Td_ZdmData(hydromodel, grid);
        }

        //结果统计信息报表
        public Dictionary<string, List<object[]>> Get_ResStatistics_Table(Dictionary<string, List<float>> H_Chainage_Float,Dictionary<string, Dictionary<DateTime, List<float>>> DischargeZdmHD_Data,
            Dictionary<string, Dictionary<DateTime, List<float>>> LevelZdmHD_Data, Dictionary<string, Dictionary<DateTime, List<float>>> SpeedZdmHD_Data,
            Dictionary<string, List<float>> Zdm_dd_Data, Dictionary<string, List<float>> Zdm_qd_Data)
        {
            HydroModel hydromodel = this;
            return Res11.Get_ResStatistics_Table(hydromodel, H_Chainage_Float, DischargeZdmHD_Data, LevelZdmHD_Data, SpeedZdmHD_Data, Zdm_dd_Data, Zdm_qd_Data);
        }

        #endregion
    }
}