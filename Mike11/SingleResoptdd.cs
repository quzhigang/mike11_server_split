using System;
using System.Collections.Generic;
using System.Linq;
using bjd_model.Common;
using bjd_model.Const_Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;

namespace bjd_model.Mike11
{
    public class Resoptdd
    {
        #region ******************** 根据完成的预报预演方案反算指定水库调度方案 ******************
        //根据已完成的预报预演方案，反向推演单水库调度方案
        public static Dictionary<string,object> Get_SingelResGate_OptimizeDd(HydroModel hydromodel,string model_instance,
            string res_name,double cons_level)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();

            //调度目标信息
            Reservoir res_info = Reservoir.Get_Res_Info(model_instance, res_name);
            Res_Dispatch_Target dd_target = new Res_Dispatch_Target(res_name, res_info.Stcd, Res_Target_Type.max_level, cons_level);
            
            //获取水库汛限水位
            double res_xxlevel = Hd11.Read_Res_XXLevels(model_instance, dd_target.stcd).ElementAt(0).Value;

            //从模型结果文件里获取该水库的入流过程
            Dictionary<string, Dictionary<DateTime, double>> res_floodin_res = Res11.Get_Mike11_ResFloodIn_Result(hydromodel);
            Dictionary<DateTime, double> res_inflow = res_floodin_res.Keys.Contains(res_name) ? res_floodin_res[res_name] : null;

            //获取水库的初始水位(先从数据库中，没有则从模型中)
            double initial_level1 = Item_Info.GetRes_InitialValue_FromDB(hydromodel.Modelname, dd_target.name);
            double initial_level2 = Item_Info.GetRes_InitialValue_FromModel(hydromodel, dd_target.name);
            double initial_level = initial_level1 == 0 ? initial_level2 : initial_level1;

            //获取水库的最优闸站调度过程
            Dictionary<DateTime, double> out_dic; Dictionary<DateTime, double> level_dic; DateTime kx_starttime;double initial_outq;
            Get_ResXfddinfo_Ddresult(hydromodel,initial_level, res_inflow,res_xxlevel, cons_level, model_instance,
                res_name, out out_dic, out level_dic,out kx_starttime,out initial_outq);

            //水库调洪演算完整结果
            Reservoir_FloodRes res_result = new Reservoir_FloodRes();
            res_result.ResName = res_name;

            //如果时间不一致，内插一下
            TimeSpan inflow_timespan = res_inflow.ElementAt(1).Key.Subtract(res_inflow.ElementAt(0).Key);
            TimeSpan level_timespan = level_dic.ElementAt(1).Key.Subtract(level_dic.ElementAt(0).Key);
            res_result.InQ_Dic = inflow_timespan.TotalHours == level_timespan.TotalHours? res_inflow: Dfs0.Getdic1(res_inflow, level_timespan);

            res_result.OutQ_Dic = out_dic;
            res_result.Level_Dic = level_dic;
            res_result.Max_Level = level_dic.Values.Max();
            res_result.Max_OutQ = out_dic.Values.Max();
            res_result.Max_InQ = res_inflow.Values.Max();

            //调度方案描述
            bool is_possible = (res_result.Max_Level < cons_level || res_result.Max_Level - cons_level < 0.1) ? true : false;
            if (cons_level < initial_level && level_dic.Last().Value - cons_level < 0.1) is_possible = true;
            string dd_desc = Get_ResBackcal_DdPlanDesc(res_name, is_possible, cons_level, res_result, kx_starttime, initial_outq);

            //结果汇总
            res.Add("res_name", res_name);
            res.Add("reservoir_result", res_result);
            res.Add("is_possible", is_possible);
            res.Add("dd_desc", dd_desc);

            return res;
        }

        //获取反向推演调度方案描述
        private static string Get_ResBackcal_DdPlanDesc(string res_name,bool is_possible,double cons_level,Reservoir_FloodRes result,
            DateTime kx_starttime,double initial_outq)
        {
            string dd_desc = "";

            //获取该水库的溢洪道和泄洪洞信息
            Struct_BasePars res_xhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "泄洪洞");
            Struct_BasePars res_yhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");

            //未达成目标
            if (!is_possible)
            {
                dd_desc = $"无法实现库水位不超{cons_level}m调度目标，建议所有泄洪闸门全开";
                return dd_desc;
            }

            //如果出库流量为0
            if (result.Max_OutQ == 0)
            {
                dd_desc = "水库各泄洪建筑全程关闭";
                return dd_desc;
            }
                
            //如果包含泄洪洞
            if (res_xhd != null)
            {
                double kx_q = Math.Min(result.Max_OutQ, res_xhd.design_q);
                if(result.Max_OutQ < res_xhd.design_q)
                {
                    if (kx_q == initial_outq)
                    {
                        dd_desc = $"{res_xhd.cn_name}全过程维持当前控泄流量{kx_q}m³/s";
                    }
                    else
                    {
                        dd_desc = $"{res_xhd.cn_name}自{kx_starttime}开始控泄流量{kx_q}m³/s";
                    }
                }
                else
                {
                    dd_desc = $"{res_xhd.cn_name}自{kx_starttime}开始全开，最大下泄流量为设计流量{res_xhd.design_q}m³/s";
                }
            }

            //如果包含溢洪道且包含闸门
            if (res_yhd != null && res_yhd.gate_type != GateType.NOGATE)
            {
                string yhd_dd;
                if (res_xhd != null)
                {
                    if(result.Max_OutQ < res_xhd.design_q)
                    {
                        yhd_dd = $"; {res_yhd.cn_name}全程关闭。";
                    }
                    else
                    {
                        double yhd_outq = result.Max_OutQ - res_xhd.design_q;
                        if (yhd_outq >= res_yhd.design_q)
                        {
                            yhd_dd = $"; {res_yhd.cn_name}全开，最大下泄流量为设计流量{res_yhd.design_q}m³/s";
                        }
                        else
                        {
                            double yhd_outq_zz = yhd_outq;
                            yhd_dd = $"; {res_xhd.cn_name}最大控泄流量{yhd_outq_zz}m³/s";
                        }
                    }
                }
                else
                {
                    double yhd_outq = result.Max_OutQ ;
                    if (yhd_outq >= res_yhd.design_q)
                    {
                        yhd_dd = $"; {res_yhd.cn_name}全开，最大下泄流量为设计流量{res_yhd.design_q}m³/s";
                    }
                    else
                    {
                        double yhd_outq_zz = yhd_outq;
                        yhd_dd = $"; {res_xhd.cn_name}最大控泄流量{yhd_outq_zz}m³/s";
                    }
                }
                dd_desc = dd_desc + yhd_dd;
            }

            return dd_desc;
        }

        //获取指定水库的消峰调度信息
        private static void Get_ResXfddinfo_Ddresult(HydroModel hydromodel,double initial_level,Dictionary<DateTime, double> inflow_dic,
            double xx_level, double constraint_level, string model_instance, string res_name,
            out Dictionary<DateTime, double> out_dic, out Dictionary<DateTime, double> level_dic, out DateTime kx_start, out double ini_outq )
        {
            Reservoir res_info = Reservoir.Get_Res_Info(model_instance, res_name);

            //获取该水库的溢洪道和泄洪洞信息
            Struct_BasePars res_yhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");
            Struct_BasePars res_xhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "泄洪洞");

            //获取该水库初始时刻出库流量(未来预演为0，预报为当前时刻监测出库流量)
            double initial_outq = Get_ResInitial_Outq(hydromodel, res_yhd.str_name, res_xhd.str_name);
            ini_outq = initial_outq;

            //获取消峰开始时刻，开始时刻之前采用初始出库流量，之后才采用消峰流量
            DateTime kx_starttime = Get_Res_KxStarttime(res_info, constraint_level, inflow_dic, initial_outq, xx_level, initial_level);
            kx_start = kx_starttime;

            //通过迭代计算，获取消峰流量
            double kx_q = Get_Res_XfResult(res_info, res_yhd, res_xhd, initial_level, xx_level, inflow_dic, constraint_level,
                 kx_starttime, initial_outq);

            //如果计算的消峰流量还不如初始出库流量大，则消峰流量就采用初始出库流量
            if (kx_q < initial_outq) kx_q = initial_outq;

            //根据该消峰流量，重新获取该水库的溢洪道、泄洪洞建筑物的调度信息
            Dictionary<DateTime, double> res_totalout_dic; Dictionary<DateTime, double> res_zzlevel_dic;
            Get_ResYhdXhd_Ddinfo(res_info, res_yhd, res_xhd,initial_level, constraint_level, xx_level,
                inflow_dic, kx_q, kx_starttime, initial_outq, out res_totalout_dic, out res_zzlevel_dic);

            //出库流量过程、库水位过程
            out_dic = res_totalout_dic;
            level_dic = res_zzlevel_dic;
        }

        //根据消峰流量，获取该水库的溢洪道、泄洪洞建筑物的调度信息
        public static void Get_ResYhdXhd_Ddinfo(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd,
             double initial_level, double cons_level, double xx_level, Dictionary<DateTime, double> inflow_dic, double kx_q,
             DateTime kx_starttime, double initial_outq,
             out Dictionary<DateTime, double> res_totalout_dic, out Dictionary<DateTime, double> res_zzlevel_dic)
        {
            //将入库洪水过程细分到20分钟
            int step_minutes = 20;
            Dictionary<DateTime, double> inflow_dic1 = Dfs0.Getdic1(inflow_dic, new TimeSpan(0, step_minutes, 0));

            //获取水库的3个关系(溢洪道和泄洪洞水位-泄流流量曲线有可能为空)
            Dictionary<double, double> res_lv = res_info.Level_Volume;
            Dictionary<double, double> yhd_lq = res_info.Level_YhdQ;
            Dictionary<double, double> xhd_lq = res_info.Level_XhdQ;

            //逐时间步进行水库调洪演算，计算水库水位
            Dictionary<DateTime, double> res_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> yhd_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> xhd_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> res_level_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> res_volumn_dic = new Dictionary<DateTime, double>();

            Dictionary<DateTime, double> yhd_maxq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> xhd_maxq_dic = new Dictionary<DateTime, double>();

            double last_level = initial_level;
            double last_res_volumn = File_Common.Insert_Zd_Value(res_lv, last_level);
            double last_in_q = inflow_dic1.First().Value;
            double last_out_q = 0; bool is_toinitial_level = false;
            for (int i = 0; i < inflow_dic1.Count; i++)
            {
                DateTime now_time = inflow_dic1.ElementAt(i).Key;

                //获取该水位下溢洪道和泄洪洞最大下泄流量
                double[] yhd_xhd_maxq = Get_YHDXHD_LevelMaxQ(res_yhd, res_xhd, yhd_lq, xhd_lq, last_level);
                double yhd_max_q = yhd_xhd_maxq[0];
                double xhd_max_q = yhd_xhd_maxq[1];
                yhd_maxq_dic.Add(now_time, yhd_max_q);
                xhd_maxq_dic.Add(now_time, xhd_max_q);
                double total_max_q = yhd_max_q + xhd_max_q;

                //该时刻来流流量
                double now_in_q = inflow_dic1.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_q + now_in_q) / 2.0) * (step_minutes * 60);

                //该时刻出流流量
                double now_out_q;
                if (now_time.CompareTo(kx_starttime) <= 0)
                {
                    now_out_q = initial_outq;
                }
                else
                {
                    DateTime max_inq_time = inflow_dic1.OrderByDescending(kv => kv.Value).First().Key;

                    //如果洪峰过了，则判断从当前水位开始，采用初始时刻出库流量，到结束时刻是否能将水位回落到初始水位
                    if (!is_toinitial_level && now_time.CompareTo(max_inq_time) > 0)
                    {
                        is_toinitial_level = Cal_Res_OutToLevel_Hours(res_lv, inflow_dic1, initial_outq, last_level, now_time, initial_level);
                    }

                    //洪峰过流，且采用初始控泄流量
                    if (now_time.CompareTo(max_inq_time) > 0 && is_toinitial_level && cons_level > initial_level)
                    {
                        now_out_q = initial_outq;
                    }
                    else
                    {
                        now_out_q = Get_Res_OutDischarge(res_yhd, kx_q, last_level, yhd_max_q, xhd_max_q, now_in_q);
                    }
                }

                double[] yhd_xhd_out_q = Get_Res_YhdXhd_Outq(now_out_q, yhd_max_q, xhd_max_q);
                double yhd_out_q = yhd_xhd_out_q[0];
                double xhd_out_q = yhd_xhd_out_q[1];
                res_outq_dic.Add(now_time, now_out_q);
                yhd_outq_dic.Add(now_time, yhd_out_q);
                xhd_outq_dic.Add(now_time, xhd_out_q);

                //该时段出库洪量(m3)
                double out_volumn = ((last_out_q + now_out_q) / 2.0) * (step_minutes * 60);

                //当前水库库容(万m3)
                double now_volumn = last_res_volumn + (in_volumn - out_volumn) / 10000.0;
                res_volumn_dic.Add(now_time, now_volumn);

                //反向内插当前水库水位
                double now_level = File_Common.Insert_Zd_Key(res_lv, now_volumn);
                res_level_dic.Add(now_time, now_level);

                last_level = now_level;
                last_res_volumn = now_volumn;
                last_in_q = now_in_q;
                last_out_q = now_out_q;
            }

            //出库过程
            res_totalout_dic = res_outq_dic.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 1));
            res_zzlevel_dic = res_level_dic.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 2));
        }

        #endregion ********************************************************************************



        #region ****************************** 单一水库优化调度 *************************************
        //通过迭代计算，获得所有闸门优化的闸站调度方式，并完整计算提取结果
        public static void Get_OptimizeDd_CalResult(HydroModel hydromodel)
        {
            //更新数据库里的模型状态信息和模型实体
            hydromodel.Model_state = Model_State.Iscalting;
            string start_simulate_time = DateTime.Now.ToString(Model_Const.TIMEFORMAT);
            string[] model_info = Item_Info.Get_Model_Info(hydromodel, start_simulate_time, true);
            HydroModel.Update_ModelStateInfo(hydromodel, model_info, false);
            Model_Const.Now_Model_State = Model_State.Iscalting;

            //获取水库的各闸站优化调度信息
            Dictionary<string, List<DdInfo>> res_optiddinfo = Resoptdd.Get_ResGate_OptimizeDd(hydromodel);

            //更新模型实例
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            Mysql_GlobalVar.now_instance = model_instance;

            //修改模型闸站调度
            Item_Info.Update_AllStr_DdInfo(ref hydromodel, res_optiddinfo);

            //先通过一次试算还原初始水情
            if (hydromodel.ModelGlobalPars.Ahead_hours != 0)
            {
                List<object> quick_simulate = hydromodel.Quick_SimulateAhead_GetMainres();
                if (quick_simulate != null) Hd11.Update_ResInitiallevel_FromIter(ref hydromodel, quick_simulate);
            }

            //重新更新模拟文件
            hydromodel.Write_ModelFiles();

            //其他则使用DOS程序调用相应引擎进行计算
            hydromodel.Run_MikeEngine_UseDos();

            //将进度信息写入文本
            if (hydromodel.ModelGlobalPars.Select_model != CalculateModel.only_rr) Sim11.Write_ProgressInfo(hydromodel);
            Thread.Sleep(2000);

            //全局变量重置
            if (Model_Const.Stop_Geting_Mike11Res) return;

            //获取模型实例
            model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);

            //计算完成后，获取一维模型结果
            List<object> mike11_datares = hydromodel.Get_Mike11_AllResult(model_instance);

            //将一维模型结果写入数据库
            Res11.Write_Mike11res_IntoDB(hydromodel, mike11_datares, model_instance);

            //将GIS过程线的结果和统计GIS线结果 写入该方案数据库
            Res11.Write_GCGisLine_TJGisLine_toDB(hydromodel, mike11_datares, model_instance);

            //写河道过程水面geojson文件，并将文件路径结果写入数据库
            Res11.Write_GCPolygonFile_toDB(hydromodel, mike11_datares, model_instance);

            //更新模型状态
            if (File.Exists(hydromodel.Modelfiles.Progressinfo_filename))
            {
                hydromodel.Model_state = hydromodel.GetProgress().now_progress_value == 100 ? Model_State.Finished : Model_State.Error;
            }
            model_info = Item_Info.Get_Model_Info(hydromodel, start_simulate_time, false);
            HydroModel.Update_ModelStateInfo(hydromodel, model_info[4], true);
            Model_Const.Now_Model_State = Model_State.Finished;
        }

        //获得在指定调洪水位下，以最小出库为优化目标的水库闸站调度信息
        public static Dictionary<string, List<DdInfo>> Get_ResGate_OptimizeDd(HydroModel hydromodel)
        {
            //从数据库获取优化调度目标、约束等参数
            string target_info = Item_Info.Get_Model_SingleParInfo(hydromodel.Modelname, "mike11_dispatch_target_info");
            if (target_info == null || target_info == "") return null;
            JArray jar = JArray.Parse(target_info);

            //调度目标信息
            Res_Dispatch_Target dd_target = JsonConvert.DeserializeObject<Res_Dispatch_Target>(jar[1].ToString());

            //水库约束水位
            Dictionary<string, object> constraint_obj = JsonConvert.DeserializeObject<Dictionary<string,object>>(jar[2].ToString());
            double constraint_level = double.Parse(constraint_obj["level_value"].ToString());

            //更新模型实例
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            Mysql_GlobalVar.now_instance = model_instance;

            //获取水库汛限水位
            double res_xxlevel = Hd11.Read_Res_XXLevels(model_instance, dd_target.stcd).ElementAt(0).Value;

            //快速模拟一次，获取水库的入流过程
            Dictionary<DateTime, double> res_inflow = Get_ResFlowIn_Gzdd(hydromodel, model_instance, dd_target.name);

            //获取水库的初始水位(先从数据库中，没有则从模型中)
            double initial_level1 = Item_Info.GetRes_InitialValue_FromDB(hydromodel.Modelname, dd_target.name);
            double initial_level2 = Item_Info.GetRes_InitialValue_FromModel(hydromodel, dd_target.name);
            double initial_level = initial_level1 == 0? initial_level2: initial_level1;

            //获取水库的最优闸站调度过程
            Dictionary<string, List<DdInfo>> res_optimize_ddinfo = Get_Res_XfddInfo(hydromodel,dd_target, res_inflow, 
                initial_level,res_xxlevel,constraint_level, model_instance);

            return res_optimize_ddinfo;
        }

        //快速模拟一次，获取水库的入流过程
        public static Dictionary<DateTime, double> Get_ResFlowIn_Gzdd(HydroModel hydromodel, string model_instance,string res_name)
        {
            //开始同步快速模拟
            hydromodel.Quick_Simulate();

            //计算完成后，获取所有水库入库洪水过程结果
            Dictionary<string, Dictionary<DateTime, double>> res_floodin_res = Res11.Get_Mike11_ResFloodIn_Result(hydromodel);

            //获取该水库的洪水过程
            Dictionary<DateTime, double> res_inq_dic = res_floodin_res.Keys.Contains(res_name) ? res_floodin_res[res_name] : null;

            return res_inq_dic;
        }

        //获取各水库可行消峰控泄流量下 的闸站调度过程
        public static Dictionary<string, List<DdInfo>> Get_Res_XfddInfo(HydroModel hydromodel,Res_Dispatch_Target dd_target,
            Dictionary<DateTime, double> res_inflow,double initial_level, double xx_level,double constraint_level, string model_instance)
        {
            Dictionary<string, List<DdInfo>> res_ddinfos = new Dictionary<string, List<DdInfo>>();

            //水库调蓄计算
            string res_name = dd_target.name;

            //获取水库迭代水位(初始水位和约束水位之间的各分层水位)
            List<double> res_iteration_level = Get_ResIter_Levels(dd_target, constraint_level, initial_level, model_instance);

            //开始进行逐层库水位下的 迭代计算
            if(dd_target.target == Res_Target_Type.max_level)
            {
                //调蓄计算，max_level只有一个水位，用满最大水位，向最小出库迭代
                double[] max_level_outq = Get_ResXfddinfo(hydromodel,dd_target, initial_level, res_inflow, xx_level,
                res_iteration_level[0], model_instance, res_name, ref res_ddinfos);
            }
            else
            {
                for (int i = 0; i < res_iteration_level.Count; i++)
                {
                    //调蓄计算，max_out有多个水位，用满最大出库，向最低水位迭代
                    double[] max_level_outq = Get_ResXfddinfo(hydromodel,dd_target, initial_level, res_inflow, xx_level,
                    res_iteration_level[i], model_instance, res_name, ref res_ddinfos);

                    //如果各调度目标都实现了，就提前返回
                    if (max_level_outq[0] <= res_iteration_level[i] + 0.1) break;
                }
            }

            return res_ddinfos;
        }

        //获取各水库迭代水位
        public static List<double> Get_ResIter_Levels(Res_Dispatch_Target dd_target, double constraint_level,
            double initial_level, string model_instance)
        {
            //获取迭代水位序列
            List<double> res_iteration_levels = new List<double>();

            //约束水位
            double cons_level = dd_target.target == Res_Target_Type.max_level ? dd_target.value : constraint_level;

            //根据优化目标 设置迭代水位
            if (dd_target.target == Res_Target_Type.max_level)
            {
                //如果调度目标是最大水位，则直接用满最大水位约束
                res_iteration_levels.Add(cons_level);
            }
            else 
            {
                //如果调度目标是最大泄洪流量，根据初始水位和约束水位判断，刷选迭代水位(包含初始水位和约束水位)
                for (int i = 0; i < Model_Const.Res_LevelIteration_Numbers + 1; i++)
                {
                    double level_h = Math.Max(cons_level - initial_level, 0);
                    if(level_h == 0)
                    {
                        res_iteration_levels.Add(initial_level); break;
                    }
                    double res_iterlevel = initial_level + level_h * (i * 1.0 / Model_Const.Res_LevelIteration_Numbers);

                    res_iteration_levels.Add(res_iterlevel);
                }
            }

            return res_iteration_levels;
        }


        //获取指定水库的消峰调度信息
        private static double[] Get_ResXfddinfo(HydroModel hydromodel,Res_Dispatch_Target dd_target, double initial_level,
            Dictionary<DateTime, double> inflow_dic,double xx_level,double constraint_level, string model_instance,
            string res_name, ref Dictionary<string, List<DdInfo>> res_ddinfos)
        {
            double[] res = new double[2];
            Reservoir res_info = Reservoir.Get_Res_Info(model_instance, res_name);

            //获取该水库的溢洪道和泄洪洞信息
            Struct_BasePars res_yhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");
            Struct_BasePars res_xhd = Item_Info.Get_Res_YHDXHD_StrInfo(res_name, "泄洪洞");

            //获取该水库初始时刻出库流量(未来预演为0，预报为当前时刻监测出库流量)
            double initial_outq = Get_ResInitial_Outq(hydromodel, res_yhd.str_name, res_xhd.str_name);

            //获取消峰开始时刻，开始时刻之前采用初始出库流量，之后才采用消峰流量
            DateTime kx_starttime = dd_target.target == Res_Target_Type.max_level? 
                Get_Res_KxStarttime(res_info, dd_target.value,inflow_dic, initial_outq, xx_level, initial_level):DateTime.Now;

            //通过迭代计算，获取消峰流量
            double kx_q = dd_target.target == Res_Target_Type.max_outq? dd_target.value:
                Get_Res_XfResult(res_info, res_yhd, res_xhd, initial_level, xx_level, inflow_dic, 
                constraint_level,kx_starttime,initial_outq, Model_Const.RES_KXQ_SFXX);

            //如果计算的消峰流量还不如初始出库流量大，则消峰流量就采用初始出库流量
            if (kx_q < initial_outq) kx_q = initial_outq;

            //根据该消峰流量，重新获取该水库的溢洪道、泄洪洞建筑物的调度信息
            Dictionary<DateTime, double> res_totalout_dic; double max_level;
            Dictionary<string, List<DdInfo>> str_ddinfo = Get_ResYhdXhd_Ddinfo(res_info, res_yhd, res_xhd, 
                initial_level, constraint_level,xx_level, inflow_dic, kx_q, kx_starttime, initial_outq, out res_totalout_dic,out max_level);

            //调度结果
            res_ddinfos = str_ddinfo;

            //最高水位和最大出库结果
            double max_outq = res_totalout_dic.Values.Max();
            res[0] = max_level;  res[1] = max_outq;
            return res;
        }

        //获取消峰开始时刻，开始时刻之前采用初始出库流量，之后才采用消峰流量
        public static DateTime Get_Res_KxStarttime(Reservoir res_info,double cons_level,
            Dictionary<DateTime, double> inflow_dic,double initial_outq,double xx_level,double initial_level)
        {
            DateTime xf_start_time = inflow_dic.ElementAt(0).Key;

            //如果目标水位低于初始水位，则直接取开始后2小时
            if(cons_level < initial_level)
            {
                xf_start_time = xf_start_time.AddHours(2.0);
                return xf_start_time;
            }

            //如果目标水位大于汛限水位，则将汛限水位作为评判开始消峰时刻之一
            if (cons_level > xx_level && initial_level < xx_level)
            {
                //通过调洪演算计算什么时候达到汛限水位(达不到则为初始时刻)
                xf_start_time = Get_ResStore_LevelTime(res_info,inflow_dic,initial_outq,initial_level,xx_level);
            }

            //以入库流量开始大于 初始出库流量为消峰开始时刻
            DateTime inflow_overtime = inflow_dic.Values.Max()> initial_outq?inflow_dic.First(kv => kv.Value > initial_outq).Key: inflow_dic.ElementAt(0).Key;

            //2个开始时间比较一下，取后值
            if (inflow_overtime.CompareTo(xf_start_time) > 0) xf_start_time = inflow_overtime;

            //洪峰流量1/5时刻
            double max_inq15 = inflow_dic.Values.Max() / 5.0;
            if (max_inq15 > initial_outq)
            {
                DateTime inflow_over15time = inflow_dic.First(kv => kv.Value > max_inq15).Key;

                //2个开始时间再比较一下，取前值
                if (inflow_over15time.CompareTo(xf_start_time) < 0) xf_start_time = inflow_over15time;
            }

            return xf_start_time;
        }

        //获取水库初始时刻出库流量(未来预演为0，预报为当前时刻监测出库流量)
        public static double Get_ResInitial_Outq(HydroModel hydromodel,string yhd_str,string xhd_str)
        {
            double kx_q = 0;

            if (hydromodel.ModelGlobalPars.Ahead_hours != 0)
            {
                //从数据库获取当前闸门状态
                List<Gate_StateInfo> gate_state = Item_Info.Read_NowGateState();

                //逐一修改修改闸门调度方式
                Dictionary<string, int> str_baseinfo = hydromodel.Mike11Pars.ControlstrList.Gatebaseinfo;
                for (int i = 0; i < gate_state.Count; i++)
                {
                    string strname = gate_state[i].str_name;

                    //建筑物基本信息表里有mike11可控建筑物没有的建筑物
                    if (!str_baseinfo.Keys.Contains(strname)) continue;  
                    string cn_ddfs = gate_state[i].nowState;
                    double dd_value = gate_state[i].openHQ;

                    if (strname == yhd_str && cn_ddfs == "控泄") kx_q += dd_value;
                    if (strname == xhd_str && cn_ddfs == "控泄") kx_q += dd_value;
                }
            }

            return kx_q;
        }

        //根据消峰流量，获取该水库的溢洪道、泄洪洞建筑物的调度信息
        public static Dictionary<string, List<DdInfo>> Get_ResYhdXhd_Ddinfo(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd,
             double initial_level,double cons_level,double xx_level,Dictionary<DateTime, double> inflow_dic,double kx_q,DateTime kx_starttime,double initial_outq,
             out Dictionary<DateTime, double> res_totalout_dic,out double max_level)
        {
            Dictionary<string, List<DdInfo>> yhd_xdh_ddinfos = new Dictionary<string, List<DdInfo>>();

            //将入库洪水过程细分到20分钟
            int step_minutes = 20;
            Dictionary<DateTime, double> inflow_dic1 = Dfs0.Getdic1(inflow_dic, new TimeSpan(0, step_minutes, 0));

            //获取水库的3个关系(溢洪道和泄洪洞水位-泄流流量曲线有可能为空)
            Dictionary<double, double> res_lv = res_info.Level_Volume;
            Dictionary<double, double> yhd_lq = res_info.Level_YhdQ;
            Dictionary<double, double> xhd_lq = res_info.Level_XhdQ;

            //逐时间步进行水库调洪演算，计算水库水位
            List<DdInfo> yhd_ddinfo = new List<DdInfo>();
            List<DdInfo> xhd_ddinfo = new List<DdInfo>();
            Dictionary<DateTime, double> res_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> yhd_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> xhd_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> res_level_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> res_volumn_dic = new Dictionary<DateTime, double>();

            Dictionary<DateTime, double> yhd_maxq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> xhd_maxq_dic = new Dictionary<DateTime, double>();

            double last_level = initial_level;
            double last_res_volumn = File_Common.Insert_Zd_Value(res_lv, last_level);
            double last_in_q = inflow_dic1.First().Value;
            double last_out_q = 0;
            bool is_toinitial_level = false;
            for (int i = 0; i < inflow_dic1.Count; i++)
            {
                DateTime now_time = inflow_dic1.ElementAt(i).Key;

                //获取该水位下溢洪道和泄洪洞最大下泄流量
                double[] yhd_xhd_maxq = Get_YHDXHD_LevelMaxQ(res_yhd, res_xhd, yhd_lq, xhd_lq, last_level);
                double yhd_max_q = yhd_xhd_maxq[0];
                double xhd_max_q = yhd_xhd_maxq[1];
                yhd_maxq_dic.Add(now_time, yhd_max_q);
                xhd_maxq_dic.Add(now_time, xhd_max_q);
                double total_max_q = yhd_max_q + xhd_max_q;

                //该时刻来流流量
                double now_in_q = inflow_dic1.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_q + now_in_q) / 2.0) * (step_minutes * 60);

                //该时刻出流流量
                double now_out_q;
                if (now_time.CompareTo(kx_starttime) <= 0)
                {
                    now_out_q = initial_outq;
                }
                else
                {
                    DateTime max_inq_time = inflow_dic1.OrderByDescending(kv => kv.Value).First().Key;

                    //如果洪峰过了，则判断从当前水位开始，采用初始时刻出库流量，到结束时刻是否能将水位回落到初始水位
                    if (!is_toinitial_level && now_time.CompareTo(max_inq_time) > 0)
                    {
                        is_toinitial_level = Cal_Res_OutToLevel_Hours(res_lv, inflow_dic1, initial_outq, last_level, now_time, initial_level);
                    }

                    //洪峰过流，且采用初始控泄流量
                    if (now_time.CompareTo(max_inq_time) > 0 && is_toinitial_level && cons_level > initial_level)
                    {
                        now_out_q = initial_outq;
                    }
                    else
                    {
                        now_out_q = Get_Res_OutDischarge(res_yhd, kx_q, last_level, yhd_max_q, xhd_max_q, now_in_q);
                    }
                }

                double[] yhd_xhd_out_q = Get_Res_YhdXhd_Outq(now_out_q, yhd_max_q, xhd_max_q);
                double yhd_out_q = yhd_xhd_out_q[0];
                double xhd_out_q = yhd_xhd_out_q[1];
                res_outq_dic.Add(now_time, now_out_q);
                yhd_outq_dic.Add(now_time, yhd_out_q);
                xhd_outq_dic.Add(now_time, xhd_out_q);

                //该时段出库洪量(m3)
                double out_volumn = ((last_out_q + now_out_q) / 2.0) * (step_minutes * 60);

                //当前水库库容(万m3)
                double now_volumn = last_res_volumn + (in_volumn - out_volumn) / 10000.0;
                res_volumn_dic.Add(now_time, now_volumn);

                //反向内插当前水库水位
                double now_level = File_Common.Insert_Zd_Key(res_lv, now_volumn);
                res_level_dic.Add(now_time, now_level);

                last_level = now_level;
                last_res_volumn = now_volumn;
                last_in_q = now_in_q;
                last_out_q = now_out_q;
            }

            //出库过程
            res_totalout_dic = res_outq_dic;
            max_level = res_level_dic.Values.Max();

            //根据溢洪道和泄洪洞出流流量过程，获取其标准调度过程(时间上的全关、全开、控泄)
            List<DdInfo> yhd_dd_info = Get_Str_Ddinfo(yhd_outq_dic, yhd_maxq_dic);
            List<DdInfo> xhd_dd_info = Get_Str_Ddinfo(xhd_outq_dic, xhd_maxq_dic);
            if (res_yhd != null)
            {
                if (res_yhd.gate_type != GateType.NOGATE) yhd_xdh_ddinfos.Add(res_yhd.str_name, yhd_dd_info);
            }
            if (res_xhd != null) yhd_xdh_ddinfos.Add(res_xhd.str_name, xhd_dd_info);

            return yhd_xdh_ddinfos;
        }

        //判断水库从当前水位开始，采用指定出库流量，到结束时刻是否能将水位回落到指定水位
        public static bool Cal_Res_OutToLevel_Hours(Dictionary<double, double> res_lv, Dictionary<DateTime, double> inflow_dic,
            double kx_q,double now_level,DateTime now_time,double target_level)
        {
            bool res = false;
            //if (now_level <= target_level) return true;

            //当前水位对应的库容
            double now_res_volumn = File_Common.Insert_Zd_Value(res_lv, now_level);

            //目标水位对应的库容
            double target_res_volumn = File_Common.Insert_Zd_Value(res_lv, target_level);

            //库容差
            double change_volumn = now_res_volumn - target_res_volumn;

            //当前时刻开始到最后的累计入库和出库洪量
            Dictionary<DateTime, double> inflow_dic1 = inflow_dic.Where(kv => kv.Key >= now_time).ToDictionary(kv => kv.Key, kv => kv.Value);
            double total_in_volumn = Res11.Get_TotalVolume(inflow_dic1);
            double total_out_volumn = kx_q * (inflow_dic.Last().Key.Subtract(now_time).TotalSeconds) / 10000.0;

            //判断是否能回落到指定水位
            if (total_out_volumn - total_in_volumn >= change_volumn) res = true;

            return res;
        }

        //获取水库约束水位下的 消峰控泄流量阈值 (通过迭代计算)
        public static double Get_Res_XfResult(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd,
            double initial_level, double xx_level, Dictionary<DateTime, double> inflow_dic, double cons_level,
             DateTime kx_starttime, double initial_outq, double sfxx = 1.0)
        {
            //通过迭代计算水库消峰流量
            double min_q = 0;
            double inflow_max = inflow_dic.Values.Max();
            double max_q = cons_level > initial_level? inflow_max:10000;
            double kx_q = (min_q + max_q) / 2;

            //计算水库调蓄水位
            int n = 0;
            while (true)
            {
                //进行水库调蓄计算，返回最高调蓄水位
                double end_level;
                double max_res_level = Cal_ResStore_ChangeDic(res_info, res_yhd, res_xhd, 
                    initial_level, xx_level, inflow_dic, kx_q, kx_starttime,initial_outq,out end_level);

                //当约束水位与最大水位之间误差小于0.1m 或控泄流量基本为0时，则停止迭代
                bool isok = cons_level < initial_level && end_level <= cons_level && Math.Abs(end_level - cons_level) < 0.1 ? true : false;
                if ((Math.Abs(max_res_level - cons_level)<0.1 && max_res_level < cons_level) 
                    || (kx_q > inflow_max * 0.95 && cons_level > initial_level)|| isok || kx_q < 1.0 || n >= 50)
                {
                    if (kx_q > inflow_max * 0.95 && cons_level > initial_level) kx_q = inflow_max;
                    if (kx_q < 1.0) kx_q = 0;
                    break;
                }

                //继续迭代
                double tz_level = cons_level > initial_level? max_res_level : end_level;
                if (tz_level > cons_level)
                {
                    //当计算的特征调蓄水位> 约束水位时，表示该设计控泄流量偏小，使的消峰调蓄过多
                    min_q = kx_q;
                }
                else
                {
                    max_q = kx_q;
                }

                kx_q = (min_q + max_q) / 2;
                n++;
            }

            if(kx_q != inflow_max) kx_q = Math.Ceiling(kx_q* sfxx / 10) * 10;

            return kx_q;
        }

        //进行水库调蓄计算，计算消峰流量，并返回最高调蓄水位
        public static double Cal_ResStore_ChangeDic(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd,
            double initial_level, double xx_level, Dictionary<DateTime, double> inflow_dic, double kx_q,
             DateTime kx_starttime, double initial_outq,out double end_level)
        {
            //将入库洪水过程细分到20分钟
            int step_minutes = 20;
            Dictionary<DateTime, double> flood_gc1 = Dfs0.Getdic1(inflow_dic, new TimeSpan(0, step_minutes, 0));

            //获取水库的3个关系(溢洪道和泄洪洞水位-泄流流量曲线有可能为空)
            Dictionary<double, double> res_lv = res_info.Level_Volume;
            Dictionary<double, double> yhd_lq = res_info.Level_YhdQ;
            Dictionary<double, double> xhd_lq = res_info.Level_XhdQ;

            //逐时间步进行水库调洪演算，计算水库水位
            Dictionary<DateTime, double> res_level_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> res_outq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> res_volumn_dic = new Dictionary<DateTime, double>();
            double last_level = initial_level;
            double last_res_volumn = File_Common.Insert_Zd_Value(res_lv, last_level);
            double last_in_q = flood_gc1.First().Value;
            double last_out_q = 0;

            for (int i = 0; i < flood_gc1.Count; i++)
            {
                DateTime now_time = flood_gc1.ElementAt(i).Key;

                //获取该水位下溢洪道和泄洪洞最大下泄流量
                double[] yhd_xhd_maxq = Get_YHDXHD_LevelMaxQ(res_yhd, res_xhd, yhd_lq, xhd_lq, last_level);
                double yhd_max_q = yhd_xhd_maxq[0];
                double xhd_max_q = yhd_xhd_maxq[1];

                //该时刻来流流量
                double now_in_q = flood_gc1.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_q + now_in_q) / 2.0) * (step_minutes * 60);

                //**该时刻出流流量**
                double now_out_q = now_time.CompareTo(kx_starttime) > 0?
                    Get_Res_OutDischarge(res_yhd, kx_q, last_level, yhd_max_q, xhd_max_q, now_in_q): initial_outq;
                res_outq_dic.Add(now_time, now_out_q);

                //该时段出库洪量(m3)
                double out_volumn = ((last_out_q + now_out_q) / 2.0) * (step_minutes * 60);

                //当前水库库容(万m3)
                double now_volumn = last_res_volumn + (in_volumn - out_volumn) / 10000.0;
                res_volumn_dic.Add(now_time, now_volumn);

                //反向内插当前水库水位
                double now_level = File_Common.Insert_Zd_Key(res_lv, now_volumn);
                res_level_dic.Add(flood_gc1.ElementAt(i).Key, now_level);

                last_level = now_level;
                last_res_volumn = now_volumn;
                last_in_q = now_in_q;
                last_out_q = now_out_q;
            }

            //最高调蓄水位
            double max_res_level = res_level_dic.Values.Max();
            end_level = last_level;

            return max_res_level;
        }

        //进行水库调蓄计算，返回指定水位最初出现的时间
        public static DateTime Get_ResStore_LevelTime(Reservoir res_info,Dictionary<DateTime, double> inflow_dic, 
            double kx_q,double initial_level, double level)
        {
            //将入库洪水过程细分到20分钟
            int step_minutes = 20;
            Dictionary<DateTime, double> flood_gc1 = Dfs0.Getdic1(inflow_dic, new TimeSpan(0, step_minutes, 0));

            //获取水库的水位库容关系
            Dictionary<double, double> res_lv = res_info.Level_Volume;

            //逐时间步进行水库调洪演算，计算水库水位
            double last_level = initial_level;
            double last_res_volumn = File_Common.Insert_Zd_Value(res_lv, last_level);
            double last_in_q = flood_gc1.First().Value;
            double last_out_q = 0;
            for (int i = 0; i < flood_gc1.Count; i++)
            {
                DateTime now_time = flood_gc1.ElementAt(i).Key;
                
                //该时刻来流流量
                double now_in_q = flood_gc1.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_q + now_in_q) / 2.0) * (step_minutes * 60);

                //该时段出库洪量(m3)
                double out_volumn = ((last_out_q + kx_q) / 2.0) * (step_minutes * 60);

                //当前水库库容(万m3)
                double now_volumn = last_res_volumn + (in_volumn - out_volumn) / 10000.0;

                //反向内插当前水库水位
                double now_level = File_Common.Insert_Zd_Key(res_lv, now_volumn);

                last_level = now_level;
                last_res_volumn = now_volumn;
                last_in_q = now_in_q;
                last_out_q = kx_q;

                if(now_level >= level) return now_time;
            }

            //中途无返回，则表示未达到该水位，返回开始时间
            return inflow_dic.First().Key;
        }


        //计算水库出库流量 (由当前水位、最大过流能力、控泄流量综合判断)
        public static double Get_Res_OutDischarge(Struct_BasePars res_yhd,double kx_q,double res_level, 
            double yhd_max_q, double xhd_max_q, double now_in_q)
        {
            double total_max_q = yhd_max_q + xhd_max_q;
            double now_out_q = 0;

            if (res_yhd != null)
            {
                if (res_yhd.gate_type == GateType.NOGATE)
                {
                    //如果溢洪道没有闸门
                    now_out_q = Get_Res_OutQ2(res_yhd, kx_q, res_level, now_in_q, total_max_q, xhd_max_q);
                }
                else
                {
                    //计算溢洪道带闸门控制，且无约束情况下的出流
                    now_out_q = Math.Min(kx_q, total_max_q); 
                }
            }
            else
            {
                //无溢洪道
                now_out_q = Math.Min(kx_q, xhd_max_q);
            }

            return now_out_q;
        }

        //获的该水库的其他建筑全关调度信息
        public static Dictionary<string, List<DdInfo>> Get_Res_OtherClose_Ddinfo(string res_name, Dictionary<string, List<DdInfo>> str_ddinfo)
        {
            Dictionary<string, List<DdInfo>> res = new Dictionary<string, List<DdInfo>>();

            //该水库的所有建筑
            List<Struct_BasePars> res_allstr = Item_Info.Get_Res_All_StrInfo(res_name);

            //加入全关调度信息
            for (int i = 0; i < res_allstr.Count; i++)
            {
                string str = res_allstr[i].str_name;
                if (!str_ddinfo.Keys.Contains(str))
                {
                    List<DdInfo> dd_info = new List<DdInfo>();
                    DdInfo dd = new DdInfo("", "全关", 0, 0);
                    dd_info.Add(dd);
                    res.Add(str, dd_info);
                }
            }

            return res;
        }

        //计算溢洪道没有闸门，且无约束情况下的出流
        private static double Get_Res_OutQ2(Struct_BasePars res_yhd,
            double kx_q, double res_level, double now_in_q, double total_max_q, double xhd_max_q)
        {
            double now_out_q = 0;
            if (res_level < res_yhd.datumn)  //当库水位小于溢洪道堰顶高程时
            {
                //当库水位大于汛限水位时，按消峰和泄洪洞最大过流能力的 小值下泄
                now_out_q = Math.Min(kx_q, xhd_max_q);
            }
            else       
            {
                //当库水位大于溢洪道堰顶高程时，泄洪和来流、设计控泄流量均无关，直接按当前水位的最大能力下泄
                now_out_q = total_max_q;
            }

            return now_out_q;
        }

        //根据溢洪道和泄洪洞出流流量过程，获取其标准调度过程(时间上的控泄)
        public static List<DdInfo> Get_Str_Ddinfo(Dictionary<DateTime, double> str_outq_dic, Dictionary<DateTime, double> str_maxq_dic)
        {
            List<DdInfo> str_ddinfo = new List<DdInfo>();
            if (str_outq_dic.Count != str_maxq_dic.Count) return null;

            //各时刻调度方式判断
            Dictionary<DateTime, DdInfo> dd_result = new Dictionary<DateTime, DdInfo>();
            for (int i = 0; i < str_outq_dic.Count; i++)
            {
                DateTime time = str_outq_dic.ElementAt(i).Key;
                double str_outq = str_outq_dic.ElementAt(i).Value;
                double str_maxq = str_maxq_dic.ElementAt(i).Value;

                DdInfo ddinfo = new DdInfo();
                ddinfo.dd_time = SimulateTime.TimetoStr(time);

                ddinfo.dd_type = "控泄";
                ddinfo.open_n = 1;
                ddinfo.dd_value = Math.Round(str_outq);

                dd_result.Add(time, ddinfo);
            }

            //将调度在时间上的融合
            str_ddinfo = Combine_Str_DdInfo(dd_result);

            return str_ddinfo;
        }

        //调度在时间上的融合
        public static List<DdInfo> Combine_Str_DdInfo(Dictionary<DateTime, DdInfo> dd_result)
        {
            List<DdInfo> res = new List<DdInfo>();

            DdInfo last_ddinfo = dd_result.ElementAt(0).Value;
            res.Add(last_ddinfo);
            for (int i = 0; i < dd_result.Count; i++)
            {
                DdInfo now_ddinfo = dd_result.ElementAt(i).Value;

                if (now_ddinfo.dd_type != last_ddinfo.dd_type || now_ddinfo.dd_value != last_ddinfo.dd_value)
                {
                    res.Add(now_ddinfo);
                    last_ddinfo = now_ddinfo;
                }
            }

            return res;
        }


        //根据水库总下泄流量，获取溢洪道和泄洪洞各自的下泄流量
        public static double[] Get_Res_YhdXhd_Outq(double res_outq, double yhd_max_q, double xhd_max_q)
        {
            double[] yhd_xhd_outq = new double[2];

            double yhd_out_q;
            double xhd_out_q;
            if (res_outq == 0)
            {
                yhd_out_q = 0;
                xhd_out_q = 0;
            }
            else if (res_outq <= xhd_max_q)
            {
                yhd_out_q = 0;
                xhd_out_q = res_outq;
            }
            else
            {
                yhd_out_q = res_outq - xhd_max_q;
                xhd_out_q = xhd_max_q;
            }

            yhd_xhd_outq[0] = yhd_out_q;
            yhd_xhd_outq[1] = xhd_out_q;

            return yhd_xhd_outq;
        }

        //获取溢洪道和泄洪洞指定水位下的最大下泄流量
        public static double[] Get_YHDXHD_LevelMaxQ(Struct_BasePars res_yhd, Struct_BasePars res_xhd,
              Dictionary<double, double> yhd_lq, Dictionary<double, double> xhd_lq, double last_level)
        {
            double[] res = new double[2];

            //溢洪道和泄洪洞该水位下最大泄洪流量
            double yhd_max_q = 0;
            if (yhd_lq.Count != 0)   //数据库有水位泄流关系
            {
                yhd_max_q = File_Common.Insert_Zd_Value(yhd_lq, last_level);
            }
            else if (res_yhd != null) //存在该泄洪建筑，但数据库没有水位泄流关系，则采用堰流公式计算
            {
                yhd_max_q = Reservoir.Get_Yhd_OverDischarge(last_level, res_yhd.datumn, res_yhd.gate_b * res_yhd.gate_n);
            }

            double xhd_max_q = 0;
            if (xhd_lq.Count != 0)    //数据库有水位泄流关系
            {
                xhd_max_q = File_Common.Insert_Zd_Value(xhd_lq, last_level); ;
            }
            else if (res_xhd != null)  //存在该泄洪建筑，但数据库没有水位泄流关系，则采用设计流量和水位内插
            {
                if (last_level <= res_xhd.datumn)
                {
                    xhd_max_q = 0;
                }
                else if (last_level > res_xhd.datumn && last_level < res_xhd.datumn + res_xhd.gate_h + 1)
                {
                    //按水位内插,假设泄洪洞顶高+1m为泄洪洞设计流量对应的水位
                    xhd_max_q = (last_level - res_xhd.datumn) * res_xhd.design_q / (res_xhd.gate_h + 1);
                }
                else
                {
                    xhd_max_q = res_xhd.design_q;
                }
            }

            res[0] = yhd_max_q;
            res[1] = xhd_max_q;

            return res;
        }

        #endregion
    }
}