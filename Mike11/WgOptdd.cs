using System;
using System.Collections.Generic;
using System.Linq;
using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace bjd_model.Mike11
{
    //马斯京根演算模型
    [Serializable]
    public class MuskingumRouting
    {
        private double K; // 行进时间
        private double X; // 权重因子
        private double Dt; // 时间步长
        private double C0, C1, C2; // 计算系数
        private double Initial_Outflow;  //初始出流

        public MuskingumRouting(double k)
        {
            // 示例参数
            K = k; // 行进时间（小时）
            X = 0.4; // 权重因子,山区河流：0.1-0.3,平原河流：0.3 - 0.5
            Dt = 1; // 时间步长1小时
            Initial_Outflow = 0; // 初始出流0

            CalculateCoefficients();
        }

        public MuskingumRouting(double k, double x, double dt, double init_out)
        {
            K = k;
            X = x;
            Dt = dt;
            Initial_Outflow = init_out;
            CalculateCoefficients();
        }

        private void CalculateCoefficients()
        {
            double denominator = K * (1 - X) + 0.5 * Dt;
            C0 = (0.5 * Dt - K * X) / denominator;
            C1 = (0.5 * Dt + K * X) / denominator;
            C2 = (K * (1 - X) - 0.5 * Dt) / denominator;
        }

        public Dictionary<DateTime, double> RouteFlood(Dictionary<DateTime, double> inflow)
        {
            var outflow = new Dictionary<DateTime, double>();
            var sortedInflow = inflow.OrderBy(x => x.Key).ToList();

            outflow.Add(sortedInflow[0].Key, Initial_Outflow);

            for (int i = 1; i < sortedInflow.Count; i++)
            {
                DateTime currentTime = sortedInflow[i].Key;
                DateTime previousTime = sortedInflow[i - 1].Key;

                double Q = C0 * sortedInflow[i].Value + C1 * sortedInflow[i - 1].Value + C2 * outflow[previousTime];
                outflow.Add(currentTime, Math.Max(Q, 0));
            }

            return outflow;
        }

        //采用默认参数计算 马斯京根演进到水库后的过程
        public static Dictionary<DateTime, double> Get_Musk_Rout(Dictionary<DateTime, double> source_dic,double distance)
        {
            if (source_dic == null) return null;
            double dt = source_dic.ElementAt(1).Key.Subtract(source_dic.ElementAt(0).Key).TotalHours;

            // 创建马斯京根模型实例
            double K = (distance / 1.5) / 3600;  //按1.5m/s流速考虑
            MuskingumRouting muskingum = new MuskingumRouting(K, 0.4, dt, 0);
            Dictionary<DateTime, double> outflow = muskingum.RouteFlood(source_dic);

            return outflow;
        }
    }


    //卫共流域优化调度 --水库+蓄滞洪区
    public class WgOptdd
    {
        #region ****************************** 洪水调度反向推演 *************************************
        //通过迭代计算，获得所有闸门优化的闸站调度方式，并完整计算提取结果
        public static void Get_OptimizeDd_CalResult(HydroModel hydromodel)
        {
            //从数据库获取优化调度目标、约束等参数
            string target_info = Item_Info.Get_Model_SingleParInfo(hydromodel.Modelname, "mike11_dispatch_target_info");
            if (target_info == null || target_info == "") return;
            JArray jar = JArray.Parse(target_info);
            Dispatch_Target dd_target = JsonConvert.DeserializeObject<Dispatch_Target>(jar[1].ToString());
            List<Res_Level_Constraint> res_level_constraint = JsonConvert.DeserializeObject<List<Res_Level_Constraint>>(jar[2].ToString());

            //更新数据库里的模型状态信息和模型实体
            hydromodel.Model_state = Model_State.Iscalting;
            string start_simulate_time = DateTime.Now.ToString(Model_Const.TIMEFORMAT);
            string[] model_info = Item_Info.Get_Model_Info(hydromodel, start_simulate_time, true);
            HydroModel.Update_ModelStateInfo(hydromodel, model_info, false);
            Model_Const.Now_Model_State = Model_State.Iscalting;

            //获取水库的各闸站优化调度信息
            hydromodel.Changeddgz_AllToGZDU();
            Dictionary<string, List<DdInfo>> res_optiddinfo = AllResoptdd.Get_ResGate_OptimizeDd(hydromodel, res_level_constraint);

            //更新模型实例
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            Mysql_GlobalVar.now_instance = model_instance;

            //从所有特征断面(水文站)中，获取目标断面信息
            Water_Condition target_section = AllResoptdd.Get_Target_Section(dd_target, model_instance);

            //设置各次试算闸站调度方式 
            List<Dictionary<string, List<DdInfo>>> gate_ddinfo_list = Get_Gate_IterDdinfo_list(hydromodel, dd_target.max_discharge, res_optiddinfo);

            //获取初始水情
            List<Water_Condition> now_waterlevel = Hd11.Read_NowWater();

            //开始逐次迭代计算
            string itercal_file = Model_Files.Get_ModelResdir(hydromodel.Modelname, Mysql_GlobalVar.now_instance) + @"\" + "iter_cal.json";
            if (File.Exists(itercal_file)) File.Delete(itercal_file);
            string res_desc = ""; int iter_i = 0;
            for (int i = 0; i < gate_ddinfo_list.Count; i++)
            {
                bool target_q;
                bool target_finish = Start_Iteration_Cal(hydromodel, dd_target, gate_ddinfo_list[i], now_waterlevel, itercal_file, out target_q);
                if (target_finish)
                {
                    res_desc = "可按推演调度方案，达成调度目标"; iter_i = i;
                    break;
                }

                //第7次如果流量目标达到了，也可以
                if (i == gate_ddinfo_list.Count - 2 && target_q)
                {
                    res_desc = "可达成调度目标,但部分蓄滞洪区需超设计启用"; iter_i = i;
                    break;
                }

                iter_i = i;
                res_desc = target_q ? "可达成调度目标,但部分蓄滞洪区需超设计启用" : "在该约束条件下，调度目标无法达成";
            }

            //完整提取计算结果后存入数据库
            //string res_desc = "可按推演调度方案，达成调度目标"; int iter_i = 3;
            Get_Optdd_AllRes_ToDb(hydromodel, model_instance,res_desc, iter_i, dd_target);

            //更新模型状态
            if (File.Exists(hydromodel.Modelfiles.Progressinfo_filename))
            {
                hydromodel.Model_state = hydromodel.GetProgress().now_progress_value == 100 ? Model_State.Finished : Model_State.Error;
            }
            model_info = Item_Info.Get_Model_Info(hydromodel, start_simulate_time, false);
            HydroModel.Update_ModelStateInfo(hydromodel, model_info[4], true);
            Model_Const.Now_Model_State = Model_State.Finished;
        }

        //完整提取计算结果后存入数据库
        private static void Get_Optdd_AllRes_ToDb(HydroModel hydromodel, string model_instance,string res_desc,int iter_i, Dispatch_Target dd_target)
        {
            //获取一维结果
            List<object> mike11_datares = hydromodel.Get_Mike11_AllResult(model_instance);

            //修改结果描述
            Dictionary<string, object> opti_res_desc = Get_Opti_MainRes(res_desc, iter_i, dd_target, mike11_datares);
            if (mike11_datares != null) mike11_datares[11] = opti_res_desc;

            //保存结果
            Res11.Write_Mike11res_IntoDB(hydromodel, mike11_datares, model_instance);
            Res11.Write_GCGisLine_TJGisLine_toDB(hydromodel, mike11_datares, model_instance);
            Res11.Write_GCPolygonFile_toDB(hydromodel, mike11_datares, model_instance);
            Res11.Write_TJPolygonFile_toDB(hydromodel, mike11_datares, model_instance);
        }

        //获取反向推演主要结果
        private static Dictionary<string, object> Get_Opti_MainRes(string res_desc, int iter_i, Dispatch_Target dd_target, List<object> mike11_datares)
        {
            Dictionary<string, Reservoir_FloodRes> res_result = mike11_datares[0] as Dictionary<string, Reservoir_FloodRes>;
            Dictionary<string, ReachSection_FloodRes> reach_result = mike11_datares[1] as Dictionary<string, ReachSection_FloodRes>;
            Dictionary<string, Xzhq_FloodRes> xzhq_result = mike11_datares[2] as Dictionary<string, Xzhq_FloodRes>;

            //洪水调度方案
            List<Dictionary<string, double>> main_strdd = new List<Dictionary<string, double>>();

            //主要的闸站控泄流量
            Dictionary<string, double> main_gate_kxq = new Dictionary<string, double>();
            Dictionary<string, double> main_gate_kxq1 = new Dictionary<string, double>();
            if (iter_i == 5 || iter_i == 6)
            {
                main_gate_kxq = new Dictionary<string, double>() { { "GQ_YTZJZZ", 1600 }, { "TYH_SFCJZZ", 300 }, { "AYH_GPJZZ", 600 } };
            }
            else
            {
                main_gate_kxq = Get_JdStr_Kxq(dd_target.max_discharge);
            }
            main_gate_kxq1.Add("盐土庄闸", main_gate_kxq.ElementAt(0).Value);
            main_gate_kxq1.Add("四伏厂闸", main_gate_kxq.ElementAt(1).Value);
            main_gate_kxq1.Add("郭盆闸", main_gate_kxq.ElementAt(2).Value);
            main_strdd.Add(main_gate_kxq1);

            //主要的水库控泄流量
            Dictionary<string, double> main_res_kxq = new Dictionary<string, double>();
            main_res_kxq.Add("盘石头水库", Math.Round(Math.Round(res_result["盘石头水库"].Max_OutQ) * 10) / 10);
            main_res_kxq.Add("小南海水库", Math.Round(Math.Round(res_result["小南海水库"].Max_OutQ) * 10) / 10);
            main_res_kxq.Add("彰武水库", Math.Round(Math.Round(res_result["彰武水库"].Max_OutQ) * 10) / 10);
            main_strdd.Add(main_res_kxq);

            //洪水推演结果
            Dictionary<string, double> flood_res = new Dictionary<string, double>();
            double target_discharge = reach_result.Keys.Contains(dd_target.name) ? reach_result[dd_target.name].Max_Qischarge : 0;
            int xzhq_on = 0; int xhzq_upon = 0; double flood_area = 0;
            for (int i = 0; i < xzhq_result.Count; i++)
            {
                if (xzhq_result.ElementAt(i).Value.Xzhq_State == "超设计")
                {
                    xzhq_on++;
                    xhzq_upon++;
                }
                else if (xzhq_result.ElementAt(i).Value.Xzhq_State == "启用")
                {
                    xzhq_on++;
                }
                flood_area = flood_area + xzhq_result.ElementAt(i).Value.Max_Area;
            }
            flood_res.Add("target_q",Math.Round(target_discharge,1));
            flood_res.Add("xzhq_on", xzhq_on);
            flood_res.Add("xhzq_upon", xhzq_upon);
            flood_res.Add("flood_area", Math.Round(flood_area,2));

            //结果汇总
            Dictionary<string, object> res_desc_obj = new Dictionary<string, object>();
            res_desc_obj.Add("res_desc", res_desc);
            res_desc_obj.Add("main_strdd", main_strdd);
            res_desc_obj.Add("flood_res", flood_res);

            return res_desc_obj;
        }

        //设置各次试算闸站调度
        public static List<Dictionary<string, List<DdInfo>>> Get_Gate_IterDdinfo_list(HydroModel hydromodel,double target_discharge,
            Dictionary<string, List<DdInfo>> res_optiddinfo)
        {
            List<Dictionary<string, List<DdInfo>>> iter_ddinfo_list = new List<Dictionary<string, List<DdInfo>>>();

            //获取所有闸门的调度信息，并替换调水库的
            Dictionary<string, List<DdInfo>> all_strdd = Get_AllStr_Ddinfo(hydromodel, res_optiddinfo);

            //获取节点建筑控泄流量
            Dictionary<string, double> gate_kxq = Get_JdStr_Kxq(target_discharge);

            //修改节点建筑控泄流量和关闭所有分洪闸堰
            Dictionary<string, List<DdInfo>> gate_dd = Get_FhzFhyClose_Ddinfo(all_strdd, gate_kxq);

            //第1次试算闸站调度 -- 关闭所有分洪闸分洪堰，包括小河口闸
            Dictionary<string, List<DdInfo>> gate_dd1 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd.Count; i++)
            {
                string str_name = gate_dd.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd.ElementAt(i).Value;

                gate_dd1.Add(str_name, str_ddinfo);
            }

            //第2次试算闸站调度 -- 刘庄分洪堰按规则调度开启
            Dictionary<string, List<DdInfo>> gate_dd2 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd1.Count; i++)
            {
                string str_name = gate_dd1.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd1.ElementAt(i).Value;

                if (str_name == "LZ_FHY")
                {
                    DdInfo dd = new DdInfo("", "规则调度", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }
                gate_dd2.Add(str_name, str_ddinfo);
            }

            //第3次试算闸站调度 -- 刘庄、白寺坡分洪闸和分洪堰按规则调度开启
            Dictionary<string, List<DdInfo>> gate_dd3 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd2.Count; i++)
            {
                string str_name = gate_dd2.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd2.ElementAt(i).Value;

                if (str_name == "BSP_FHZ" || str_name == "BSP_FHY")
                {
                    DdInfo dd = new DdInfo("", "规则调度", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }
                gate_dd3.Add(str_name, str_ddinfo);
            }

            //第4次试算闸站调度  -- 刘庄、白寺坡、长虹渠分洪闸和分洪堰按规则调度开启，小河口闸控泄700
            Dictionary<string, List<DdInfo>> gate_dd4 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd3.Count; i++)
            {
                string str_name = gate_dd3.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd3.ElementAt(i).Value;

                if (str_name == "CHQ_FHZ" || str_name == "CHQ_FHY")
                {
                    DdInfo dd = new DdInfo("", "规则调度", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                if (str_name == "QHNZ_XHKJZZ")
                {
                    DdInfo dd = new DdInfo("", "控泄", 1, 700);
                    str_ddinfo = new List<DdInfo>() { dd };
                }
                gate_dd4.Add(str_name, str_ddinfo);
            }

            //第5次试算闸站调度 --刘庄、白寺坡、长虹渠、宋村和西沿村分洪闸和分洪堰按规则调度开启，小河口闸控泄700
            Dictionary<string, List<DdInfo>> gate_dd5 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd4.Count; i++)
            {
                string str_name = gate_dd4.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd4.ElementAt(i).Value;

                if (str_name == "SC_FHY" || str_name == "XYC_FHY")
                {
                    DdInfo dd = new DdInfo("", "规则调度", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                gate_dd5.Add(str_name, str_ddinfo);
            }

            //第6次试算闸站调度 --调整节点闸站控泄流量，在5片蓄滞洪区基础上，启用小滩坡(直接采用全开，避免规则调度流量判断条件错误)
            Dictionary<string, List<DdInfo>> gate_dd6 = new Dictionary<string, List<DdInfo>>();
            Dictionary<string, double> max_kxdd = new Dictionary<string, double>() { { "GQ_YTZJZZ", 1600 }, { "TYH_SFCJZZ", 300 }, { "AYH_GPJZZ", 600 } };
            for (int i = 0; i < gate_dd5.Count; i++)
            {
                string str_name = gate_dd5.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd5.ElementAt(i).Value;

                //修正3个节点建筑物控泄调度
                if (max_kxdd.Keys.Contains(str_name))
                {
                    DdInfo dd = new DdInfo("", "控泄", 1, max_kxdd[str_name]);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                if (str_name == "QL_FHY")
                {
                    DdInfo dd = new DdInfo("", "全开", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                gate_dd6.Add(str_name, str_ddinfo);
            }

            //第7次试算闸站调度 --调整节点闸站控泄流量，在6片蓄滞洪区基础上，启用小滩坡和任固坡(直接采用全开，避免规则调度流量判断条件错误)
            Dictionary<string, List<DdInfo>> gate_dd7 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd6.Count; i++)
            {
                string str_name = gate_dd6.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd6.ElementAt(i).Value;

                if (str_name == "BWL_FHY")
                {
                    DdInfo dd = new DdInfo("", "全开", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                gate_dd7.Add(str_name, str_ddinfo);
            }

            //第8次试算闸站调度 --在第7次基础上，控泄还原到按比例缩减，蓄滞洪区超标就超标，优先保目标、保河道
            Dictionary<string, List<DdInfo>> gate_dd8 = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gate_dd7.Count; i++)
            {
                string str_name = gate_dd7.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = gate_dd7.ElementAt(i).Value;

                //修正3个节点建筑物控泄调度
                if (gate_kxq.Keys.Contains(str_name))
                {
                    DdInfo dd = new DdInfo("", "控泄", 1, gate_kxq[str_name]);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                gate_dd8.Add(str_name, str_ddinfo);
            }

            iter_ddinfo_list.Add(gate_dd1);
            iter_ddinfo_list.Add(gate_dd2);
            iter_ddinfo_list.Add(gate_dd3);
            iter_ddinfo_list.Add(gate_dd4);
            iter_ddinfo_list.Add(gate_dd5);
            iter_ddinfo_list.Add(gate_dd6);
            iter_ddinfo_list.Add(gate_dd7);
            iter_ddinfo_list.Add(gate_dd8);

            return iter_ddinfo_list;
        }

        //通过试算一次，获取节点建筑控泄流量
        private static Dictionary<string, double> Get_JdStr_Kxq(double target_discharge)
        {
            //3个节点闸站控泄流量
            Dictionary<string, double> gate_kxq = new Dictionary<string, double>();
            if (target_discharge >= 2500)
            {
                gate_kxq.Add("GQ_YTZJZZ", 1600); gate_kxq.Add("TYH_SFCJZZ", 300); gate_kxq.Add("AYH_GPJZZ", 600);
            }
            else
            {
                double ytz_jzz = Math.Round((target_discharge * 1600 / 2500) / 10) * 10;  //按10取整
                double sfc_jzz = Math.Round((target_discharge * 300 / 2500) / 10) * 10;  //按10取整
                double gp_jzz = Math.Round((target_discharge * 600 / 2500) / 10) * 10;  //按10取整
                gate_kxq.Add("GQ_YTZJZZ", ytz_jzz); gate_kxq.Add("TYH_SFCJZZ", sfc_jzz); gate_kxq.Add("AYH_GPJZZ", gp_jzz);
            }

            return gate_kxq;
        }

        //获取所有闸门的调度信息，并替换调水库的
        private static Dictionary<string, List<DdInfo>> Get_AllStr_Ddinfo(HydroModel hydromodel, Dictionary<string, List<DdInfo>> res_optiddinfo)
        {
            List<Str_DdInfo> gatedd_info = Item_Info.Get_ModelGatedd_Info(hydromodel);
            Dictionary<string, List<DdInfo>> all_strdd = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < gatedd_info.Count; i++)
            {
                string str_name = gatedd_info[i].str_name;
                List<DdInfo> dd = new List<DdInfo>();
                if (res_optiddinfo.Keys.Contains(str_name))
                {
                    dd = res_optiddinfo[str_name];
                }
                else
                {
                    string dd_type = gatedd_info[i].Get_DdType_CNName();

                    DdInfo ddinfo = new DdInfo("", dd_type, gatedd_info[i].open_n, gatedd_info[i].dd_value);
                    dd.Add(ddinfo);
                }

                all_strdd.Add(str_name, dd);
            }

            return all_strdd;
        }

        //修改节点建筑控泄流量和关闭所有分洪闸堰
        private static Dictionary<string, List<DdInfo>> Get_FhzFhyClose_Ddinfo(Dictionary<string, List<DdInfo>> all_str_ddinfo, Dictionary<string, double> gate_kxq)
        {
            //所有分洪闸堰可控建筑物集合
            List<string> fhz_fhy = new List<string>();
            Dictionary<string, Struct_BasePars> str_infos = Item_Info.Get_StrBaseInfo();
            for (int i = 0; i < all_str_ddinfo.Count; i++)
            {
                string str_name = all_str_ddinfo.ElementAt(i).Key;
                if (str_infos.Keys.Contains(str_name))
                {
                    Struct_BasePars str_baseinfo = str_infos[str_name];
                    if (str_baseinfo.str_type == "分洪闸" || str_baseinfo.str_type == "分洪堰") fhz_fhy.Add(str_name);
                }
            }

            //修改节点建筑控泄流量和关闭所有分洪闸堰
            Dictionary<string, List<DdInfo>> gate_dd = new Dictionary<string, List<DdInfo>>();
            for (int i = 0; i < all_str_ddinfo.Count; i++)
            {
                string str_name = all_str_ddinfo.ElementAt(i).Key;
                List<DdInfo> str_ddinfo = all_str_ddinfo.ElementAt(i).Value;

                //修正3个节点建筑物控泄调度
                if (gate_kxq.Keys.Contains(str_name))
                {
                    DdInfo dd = new DdInfo("", "控泄", 1, gate_kxq[str_name]);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                //关闭所有分洪闸堰
                if (fhz_fhy.Contains(str_name))
                {
                    DdInfo dd = new DdInfo("", "全关", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                //关闭小河口闸
                if (str_name == "QHNZ_XHKJZZ")
                {
                    DdInfo dd = new DdInfo("", "全关", 0, 0);
                    str_ddinfo = new List<DdInfo>() { dd };
                }

                gate_dd.Add(str_name, str_ddinfo);
            }

            return gate_dd;
        }

        //各次试算
        public static bool Start_Iteration_Cal(HydroModel hydromodel, Dispatch_Target dd_target ,Dictionary<string, List<DdInfo>> optimize_ddinfo,
            List<Water_Condition> now_waterlevel, string itercal_file,out bool target_q)
        {
            //判断目标是否达到
            bool target_isfinish = false;

            //修改模型闸站调度
            Item_Info.Update_AllStr_DdInfo(ref hydromodel, optimize_ddinfo);

            //开始同步快速模拟
            hydromodel.Quick_Simulate();

            //判断目标流量是否达到
            double target_section_maxq = Res11.Get_TargetSection_Result(hydromodel,dd_target, now_waterlevel);
            bool target_maxq_isfinish = target_section_maxq <= dd_target.max_discharge?true:false;

            //判断各蓄滞洪区是否有超设计启用的 --忽略广润坡和崔家桥
            Dictionary<string, List<string>> xzhq_main_res;
            bool xzhq_state = Res11.Get_Xzhq_Csj_Result(hydromodel,out xzhq_main_res);

            //结合判断是否满足要求(同时满足 调度目标和蓄滞洪区不超设计启用)
            if (target_maxq_isfinish && xzhq_state) target_isfinish = true;
            target_q = target_maxq_isfinish;

            //写入迭代计算结果信息
            Dictionary<string, object> res_info = new Dictionary<string, object>();
            res_info.Add("target_discharge",dd_target.max_discharge);
            res_info.Add("itercal_discharge",target_section_maxq);
            res_info.Add("xzhq_res", xzhq_main_res);
            File_Common.AppendWrige_FileContent(itercal_file, File_Common.Serializer_Obj(res_info));
            File_Common.AppendWrige_FileContent(itercal_file, ",");

            return target_isfinish;
        }


        #endregion

    }

}