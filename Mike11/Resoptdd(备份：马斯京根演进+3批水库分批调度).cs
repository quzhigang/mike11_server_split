using System;
using System.Collections.Generic;
using System.Linq;
using bjd_model.Common;
using bjd_model.Const_Global;

using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bjd_model.Mike11
{
    public class Resoptdd
    {
        #region ****************************** 库群优化调度 *************************************
        //获得在指定调洪水位下，以最小出库为优化目标的水库闸站调度信息
        public static Dictionary<string, List<DdInfo>> Get_ResGate_OptimizeDd(HydroModel hydromodel, List<Res_Level_Constraint> res_level_constraint)
        {
            Dictionary<string, List<DdInfo>> optimize_ddinfo = new Dictionary<string, List<DdInfo>>();

            //更新模型实例
            string model_instance = HydroModel.Get_Model_Instance(hydromodel.Modelname);
            Mysql_GlobalVar.now_instance = model_instance;

            //获取各水库汛限水位
            Dictionary<string, double> res_xxlevel = Hd11.Read_Res_XXLevels(model_instance);
            List<Res_Outflow_Constraint> res_outflow_constraint = new List<Res_Outflow_Constraint>();

            //刷选出所有 有上游水库的水库名称
            List<List<string>> res_batch_list = Get_Batch_ResList(model_instance);

            //获取所有水库的边界条件合并入流过程(马斯京根演进后)
            //Dictionary<string, Dictionary<DateTime, double>> allres_bndinflow = Get_AllResBnd_InqCombine(hydromodel, model_instance);

            //通过一维模型采用默认的规则调度试算，获取所有水库的入流过程
            if (hydromodel.Modelname != Model_Const.DEFAULT_MODELNAME) hydromodel.Write_ModelFiles();
            Dictionary<string, Dictionary<DateTime, double>> allres_gzdd_inflow = Get_ResFlowIn_Gzdd(hydromodel, model_instance);

            //获取各水库初始水位(从模型中)
            Dictionary<string, double> initial_levels = WG_INFO.GetRes_InitialValue_FromModel(hydromodel);

            //获取水库迭代水位(初始水位和约束水位之间的各分层水位)
            Dictionary<string, double> res_iteration_level = Get_ResIter_Levels(res_level_constraint);

            //获取各水库的最优闸站调度过程
            Dictionary<string, List<DdInfo>> res_optimize_ddinfo = Get_AllRes_XfddInfo(res_batch_list, allres_bndinflow,res_outflow_constraint, 
                initial_levels, res_xxlevel, res_iteration_level, model_instance);

            return res_optimize_ddinfo;
        }

        //通过一维模型采用默认的规则调度试算，获取所有水库的入流过程
        public static Dictionary<string, Dictionary<DateTime, double>> Get_ResFlowIn_Gzdd(HydroModel hydromodel, string model_instance)
        {
            //开始同步快速模拟
            hydromodel.Quick_Simulate();

            //计算完成后，获取所有水库入库洪水过程结果
            Dictionary<string, Dictionary<DateTime, double>> res_floodin_res = Res11.Get_Mike11_ResFloodIn_Result(hydromodel);

            return res_floodin_res;
        }

        //获取各水库可行消峰控泄流量下 的闸站调度过程(每个水库均不相同)
        public static Dictionary<string, List<DdInfo>> Get_AllRes_XfddInfo(List<List<string>> res_batch_list,Dictionary<string, Dictionary<DateTime, double>> allres_bndinflow,
            List<Res_Outflow_Constraint> res_outflow_constraint, Dictionary<string, double> initial_levels, Dictionary<string, double> xx_levels,
            Dictionary<string, double> res_iteration_level, string model_instance)
        {
            Dictionary<string, List<DdInfo>> res_ddinfos = new Dictionary<string, List<DdInfo>>();

            //分批次逐步、逐个水库获取消峰控泄流量和起止时间
            //先计算上游无水库的第1批次
            List<string> st1_batch_list = res_batch_list[0];
            Dictionary<string, Dictionary<DateTime, double>> st1_batch_out = new Dictionary<string, Dictionary<DateTime, double>>();
            for (int i = 0; i < st1_batch_list.Count; i++)
            {
                string res_name = st1_batch_list[i];

                //第1批次水库入流直接用边界条件入流，出流通过试算最小
                Dictionary<DateTime, double> inflow_combine = allres_bndinflow[res_name];
                Dictionary<DateTime, double> res_total_out = Get_ResXfddinfo(res_outflow_constraint, initial_levels, inflow_combine, xx_levels, 
                    res_iteration_level, model_instance, res_name, ref res_ddinfos);

                st1_batch_out.Add(res_name, res_total_out);
            }

            //接着计算中游水库的第2批次
            List<string> st2_batch_list = res_batch_list[1];
            Dictionary<string, Dictionary<DateTime, double>> st2_batch_out = new Dictionary<string, Dictionary<DateTime, double>>();
            for (int i = 0; i < st2_batch_list.Count; i++)
            {
                string res_name = st2_batch_list[i];

                //第2批次水库入流用 第1批次水库出流马斯京根演进后的洪水 + 区间马斯京根演进后的子流域洪水
                Dictionary<DateTime, double> region_bnd_inflow = allres_bndinflow[res_name]; //合并后的区间子流域洪水(马斯京根演进后)
                Dictionary<string,double> res_upreslist = WG_INFO.Get_Res_UpResList(model_instance)[res_name];

                //根据水库上游各水库的下泄流量合并峰值和最大合并下泄流量峰值(合并边界条件入流过程)，按比值计算入流过程
                Dictionary<DateTime, double> inflow_combine = Get_RegionRes_CombineInflow(res_upreslist, st1_batch_out, region_bnd_inflow);
                Dictionary<DateTime, double> res_total_out = Get_ResXfddinfo(res_outflow_constraint, initial_levels, inflow_combine, xx_levels,
                    res_iteration_level, model_instance, res_name, ref res_ddinfos);

                st2_batch_out.Add(res_name, res_total_out);
            }

            //最后计算最下游的第3批次水库
            List<string> st3_batch_list = res_batch_list[2];
            Dictionary<string, Dictionary<DateTime, double>> st12_batch_out = Dfs0.Combine_Two_Item(st1_batch_out, st2_batch_out);
            Dictionary<string, Dictionary<DateTime, double>> st3_batch_out = new Dictionary<string, Dictionary<DateTime, double>>();
            for (int i = 0; i < st3_batch_list.Count; i++)
            {
                string res_name = st3_batch_list[i];

                //第3批次水库入流t同样用 第2批次水库出流马斯京根演进后的洪水 + 区间马斯京根演进后的子流域洪水
                Dictionary<DateTime, double> region_bnd_inflow = allres_bndinflow[res_name]; //合并后的区间子流域洪水(马斯京根演进后)
                Dictionary<string, double> res_upreslist = WG_INFO.Get_Res_UpResList(model_instance)[res_name];

                //根据水库上游各水库的下泄流量合并峰值和最大合并下泄流量峰值(合并边界条件入流过程)，按比值计算入流过程
                Dictionary<DateTime, double> inflow_combine = Get_RegionRes_CombineInflow(res_upreslist, st12_batch_out, region_bnd_inflow);
                Dictionary<DateTime, double> res_total_out = Get_ResXfddinfo(res_outflow_constraint, initial_levels, inflow_combine, xx_levels,
                    res_iteration_level, model_instance, res_name, ref res_ddinfos);

                st3_batch_out.Add(res_name, res_total_out);
            }
            return res_ddinfos;
        }

        //计算区间水库的合并入流过程
        public static Dictionary<DateTime, double> Get_RegionRes_CombineInflow(Dictionary<string,double> res_upreslist,
            Dictionary<string, Dictionary<DateTime, double>> up_batchres_out, Dictionary<DateTime, double> region_bnd_inflow)
        {
            Dictionary<DateTime, double> combine_res = new Dictionary<DateTime, double>();

            //刷选出上一批次结果 在该水库上游水库列表中的水库结果
            List<Dictionary<DateTime, double>> upres_outflow = new List<Dictionary<DateTime, double>>();
            for (int i = 0; i < res_upreslist.Count; i++)
            {
                string res_name = res_upreslist.ElementAt(i).Key;
                if (up_batchres_out.Keys.Contains(res_name)) upres_outflow.Add(up_batchres_out[res_name]);
            }

            //采用马斯京根演进到水库后的过程
            List<Dictionary<DateTime, double>> upres_outflow_rout = Get_Musk_RoutRes(upres_outflow, res_upreslist);

            //合并上游水库出流演进后的 洪水过程
            Dictionary<DateTime, double> upres_out_combine = Dfs0.Combine_Dic(upres_outflow_rout);

            //合并上游水库出流和区间洪水
            double step1 = upres_out_combine.ElementAt(1).Key.Subtract(upres_out_combine.ElementAt(0).Key).TotalMinutes;
            double step2 = region_bnd_inflow.ElementAt(1).Key.Subtract(region_bnd_inflow.ElementAt(0).Key).TotalMinutes;
            Dictionary<DateTime, double> region_bnd_inflow1 = new Dictionary<DateTime, double>();
            if (step1 != step2)
            {
                region_bnd_inflow1 = Dfs0.Insert_Instantdic(region_bnd_inflow, new TimeSpan(0, (int)step1, 0));
            }
            combine_res = Dfs0.Combine_Dic(upres_out_combine, region_bnd_inflow1);

            return combine_res;
        }

        //将水库分为3批次，上游水库\区间水库\下游水库
        public static List<List<string>> Get_Batch_ResList(string model_instance)
        {
            List<List<string>> res_batch_list = new List<List<string>>();

            //获取所有有上游水库的 水库的上游水库信息
            Dictionary<string, Dictionary<string,double>> res_upreslist = WG_INFO.Get_Res_UpResList(model_instance);

            //刷选出最上游水库、区间水库和最下游水库
            List<string> up_res = new List<string>();
            List<string> center_res = new List<string>();
            List<string> down_res = new List<string>();
            for (int i = 0; i < res_upreslist.Count; i++)
            {
                List<string> up_reslist = res_upreslist.ElementAt(i).Value== null?null: res_upreslist.ElementAt(i).Value.Keys.ToList();
                if (up_reslist == null)
                {
                    up_res.Add(res_upreslist.ElementAt(i).Key);
                }
                else
                {
                    bool upres_haveupres = false;
                    for (int j = 0; j < up_reslist.Count; j++)
                    {
                        string upres = up_reslist[j];
                        if (res_upreslist[upres] != null) upres_haveupres = true;
                    }
                    if (!upres_haveupres)
                    {
                        center_res.Add(res_upreslist.ElementAt(i).Key);
                    }
                    else
                    {
                        down_res.Add(res_upreslist.ElementAt(i).Key);
                    }
                }
            }

            res_batch_list.Add(up_res);
            res_batch_list.Add(center_res);
            res_batch_list.Add(down_res);

            return res_batch_list;
        }

        //获取最下游水库清单，没有上游水库的自身就是最下游水库
        public static List<string> Get_Down_ResList(string model_instance)
        {
            List<string> res_down_list = new List<string>();

            //获取所有有上游水库的 水库的上游水库信息
            Dictionary<string, Dictionary<string, double>> res_upreslist = WG_INFO.Get_Res_UpResList(model_instance);

            //刷选出最上游水库、区间水库和最下游水库
            List<string> up_res = new List<string>();
            List<string> center_res = new List<string>();
            List<string> down_res = new List<string>();
            for (int i = 0; i < res_upreslist.Count; i++)
            {
                List<string> up_reslist = res_upreslist.ElementAt(i).Value == null ? null : res_upreslist.ElementAt(i).Value.Keys.ToList();
                if (up_reslist == null)
                {
                    up_res.Add(res_upreslist.ElementAt(i).Key);
                }
                else
                {
                    bool upres_haveupres = false;
                    for (int j = 0; j < up_reslist.Count; j++)
                    {
                        string upres = up_reslist[j];
                        if (res_upreslist[upres] != null) upres_haveupres = true;
                    }
                    if (!upres_haveupres)
                    {
                        center_res.Add(res_upreslist.ElementAt(i).Key);
                    }
                    else
                    {
                        down_res.Add(res_upreslist.ElementAt(i).Key);
                    }
                }
            }

            res_down_list.Add(up_res);
            res_down_list.Add(center_res);
            res_down_list.Add(down_res);

            return res_down_list;
        }

        //获取上游水库不调蓄下(控泄，来多少泄多少)的调度过程信息
        public static Dictionary<string, List<DdInfo>> Get_UpRes_DdInfo_KX(HydroModel hydromodel, string model_instance, bool res_close)
        {
            Dictionary<string, List<DdInfo>> res_ddino = new Dictionary<string, List<DdInfo>>();

            //刷选出所有 有下游水库的水库名称
            List<string> have_downres_res = Get_DownRes_list(model_instance);

            //获取这些水库的入库流量过程,并生成控泄调度信息
            for (int i = 0; i < have_downres_res.Count; i++)
            {
                string res_name = have_downres_res[i];

                //获取这些水库的泄洪建筑物信息(每个水库只算一次，优先泄洪洞)
                Struct_BasePars res_xhd = WG_INFO.Get_Res_YHDXHD_StrInfo(res_name, "泄洪洞");
                Struct_BasePars res_yhd = WG_INFO.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");
                if (res_close == true)  //全关就都关
                {
                    if (res_xhd != null)
                    {
                        if (res_xhd.gate_type != GateType.NOGATE)
                        {
                            Add_Res_Strdd(hydromodel, model_instance, res_name, res_xhd.str_name, true, ref res_ddino);
                        }
                    }

                    if (res_yhd != null)
                    {
                        if (res_yhd.gate_type != GateType.NOGATE)
                        {
                            Add_Res_Strdd(hydromodel, model_instance, res_name, res_yhd.str_name, true, ref res_ddino);
                        }
                    }
                }
                else   //控泄就只用一个建筑物控泄,优先溢洪道，另一个则设为全关
                {
                    if (res_yhd != null)
                    {
                        if (res_yhd.gate_type != GateType.NOGATE)
                        {
                            Add_Res_Strdd(hydromodel, model_instance, res_name, res_yhd.str_name, false, ref res_ddino);
                        }

                        //如果有泄洪洞，且溢洪道有闸门则全关，否则控泄
                        if (res_xhd != null)
                        {
                            if (res_xhd.gate_type != GateType.NOGATE)
                            {
                                if (res_yhd.gate_type != GateType.NOGATE)  //溢洪道有闸门全关
                                {
                                    Add_Res_Strdd(hydromodel, model_instance, res_name, res_xhd.str_name, true, ref res_ddino);
                                }
                                else  //溢洪道无闸门控泄
                                {
                                    Add_Res_Strdd(hydromodel, model_instance, res_name, res_xhd.str_name, false, ref res_ddino);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (res_xhd != null)
                        {
                            if (res_xhd.gate_type != GateType.NOGATE)
                            {
                                Add_Res_Strdd(hydromodel, model_instance, res_name, res_xhd.str_name, false, ref res_ddino);
                            }
                        }
                    }
                }
            }

            return res_ddino;
        }

        //刷选出所有 有上游水库的水库名称
        private static List<string> Get_UpRes_list(string model_instance)
        {
            //获取所有有上游水库的 水库的上游水库信息
            Dictionary<string, Dictionary<string,double>> res_upreslist = WG_INFO.Get_Res_UpResList(model_instance);

            //刷选出有下游水库的上游水库
            List<string> up_res = new List<string>();
            for (int i = 0; i < res_upreslist.Count; i++)
            {
                List<string> up_reslist = res_upreslist.ElementAt(i).Value.Keys.ToList();
                if (up_reslist != null)
                {
                    up_res.Add(res_upreslist.ElementAt(i).Key);
                }
            }

            return up_res;
        }

        //刷选出所有 有下游水库的水库名称
        private static List<string> Get_DownRes_list(string model_instance)
        {
            //获取所有有上游水库的 水库的上游水库信息
            Dictionary<string, Dictionary<string,double>> res_upreslist = WG_INFO.Get_Res_UpResList(model_instance);

            //刷选出有下游水库的上游水库
            List<string> up_res = new List<string>();
            for (int i = 0; i < res_upreslist.Count; i++)
            {
                List<string> up_reslist = res_upreslist.ElementAt(i).Value.Keys.ToList();
                if (up_reslist == null) continue;

                for (int j = 0; j < up_reslist.Count; j++)
                {
                    if (!up_res.Contains(up_reslist[j])) up_res.Add(up_reslist[j]);
                }
            }

            return up_res;
        }

        //增加水库溢洪道、泄洪洞闸站调度信息
        private static void Add_Res_Strdd(HydroModel hydromodel, string model_instance, string res_name, string str_name, bool res_close,
           ref Dictionary<string, List<DdInfo>> res_ddino)
        {
            List<DdInfo> ddino = new List<DdInfo>();
            if (res_close)
            {
                DdInfo dd = new DdInfo("", "全关", 0, 0);
                ddino.Add(dd);
            }
            else
            {
                Dictionary<DateTime, double> bnd_inqcombine = Get_ResBnd_InqCombine(hydromodel, model_instance, res_name);

                //基于入库流量过程改成控泄流量调度过程
                ddino = Get_Str_Ddinfo(bnd_inqcombine);
            }

            res_ddino.Add(str_name, ddino);
        }

        //获取指定水库边界条件合并的入流过程
        public static Dictionary<DateTime, double> Get_ResBnd_InqCombine(HydroModel hydromodel, string model_instance, string res_name)
        {
            //入库流量过程
            Dictionary<string, Res_InSection_BndID> res_inflows = WG_INFO.Get_ResInFlow_SectionBndID(model_instance);
            Res_InSection_BndID res_inflow = res_inflows[res_name];
            List<Dictionary<DateTime, double>> bnd_inqs = Res11.Get_Res_BndInQ(hydromodel, res_inflow);
            Dictionary<DateTime, double> bnd_inqcombine = Dfs0.Combine_Dic(bnd_inqs);
            return bnd_inqcombine;
        }

        //获取所有水库边界条件合并的入流过程
        public static Dictionary<string, Dictionary<DateTime, double>> Get_AllResBnd_InqCombine(HydroModel hydromodel, string model_instance)
        {
            Dictionary<string, Dictionary<DateTime, double>> result = new Dictionary<string, Dictionary<DateTime, double>>();

            //各水库的区间子流域ID和距离该水库距离
            Dictionary<string, Dictionary<string, double>> res_inflows = WG_INFO.Get_Res_Subcatchment_InFlow(model_instance);
            for (int i = 0; i < res_inflows.Count; i++)
            {
                string res_name = res_inflows.ElementAt(i).Key;
                Dictionary<string, double> res_subcatchment_bnds = res_inflows[res_name];

                //各子流域洪水过程
                List<Dictionary<DateTime, double>> bnd_inqs = Res11.Get_Res_BndInQ(hydromodel, res_subcatchment_bnds.Keys.ToList());

                //各子流域洪水采用马斯京根演进到水库后的过程
                List<Dictionary<DateTime, double>> bnd_inqs_rout = Get_Musk_RoutRes(bnd_inqs, res_subcatchment_bnds);

                //合并洪水过程
                Dictionary<DateTime, double> bnd_inqcombine = Dfs0.Combine_Dic(bnd_inqs_rout);

                result.Add(res_name, bnd_inqcombine);
            }

            return result;
        }

        //各子流域洪水采用马斯京根演进到水库后的过程
        public static List<Dictionary<DateTime, double>> Get_Musk_RoutRes(List<Dictionary<DateTime, double>> source_dic, 
            Dictionary<string, double> res_subcatchment_bnds)
        {
            List<Dictionary<DateTime, double>> res = new List<Dictionary<DateTime, double>>();
            if (source_dic.Count != res_subcatchment_bnds.Count) return res;
            double dt = source_dic[0].ElementAt(1).Key.Subtract(source_dic[0].ElementAt(0).Key).TotalHours;

            for (int i = 0; i < source_dic.Count; i++)
            {
                double rout_dis = res_subcatchment_bnds.ElementAt(i).Value;
                Dictionary<DateTime, double> outflow = MuskingumRouting.Get_Musk_Rout(source_dic[i], rout_dis);
                res.Add(outflow);
            }

            return res;
        }


        //获取上游水库合并的下泄流量过程的峰值
        public static double Get_Upres_OutFlow_Combine(List<string> res_upreslist, Dictionary<string, Dictionary<DateTime, double>> st1_batch_out)
        {
            List<Dictionary<DateTime, double>> up_res_out = new List<Dictionary<DateTime, double>>();
            for (int i = 0; i < res_upreslist.Count; i++)
            {
                string up_res = res_upreslist[i];
                if (st1_batch_out.Keys.Contains(up_res)) up_res_out.Add(st1_batch_out[up_res]);
            }

            //过程合并
            Dictionary<DateTime, double> combine = Dfs0.Combine_Dic(up_res_out);

            return combine.Values.Max();
        }

        //获取指定水库的消峰调度信息
        private static Dictionary<DateTime, double> Get_ResXfddinfo(List<Res_Outflow_Constraint> res_outflow_constraint, Dictionary<string, double> initial_levels,
            Dictionary<DateTime, double> res_inflow_dic, Dictionary<string, double> xx_levels, Dictionary<string, double> res_iteration_level, string model_instance,
            string res_name, ref Dictionary<string, List<DdInfo>> res_ddinfos)
        {
            Reservoir res_info = Reservoir.Get_Res_Info(model_instance, res_name);

            //获取该水库的溢洪道和泄洪洞信息
            Struct_BasePars res_yhd = WG_INFO.Get_Res_YHDXHD_StrInfo(res_name, "溢洪道");
            Struct_BasePars res_xhd = WG_INFO.Get_Res_YHDXHD_StrInfo(res_name, "泄洪洞");

            //水库出流约束
            Res_Outflow_Constraint res_out_cons = Res_Outflow_Constraint.Get_Res_Outflow_Constraint(res_outflow_constraint, res_info.Stcd);

            //通过迭代计算，获取消峰流量
            double initial_level = initial_levels[res_name];
            double xx_level = xx_levels[res_name];
            double iteration_level = res_iteration_level[res_name];
            double kx_q = Get_Res_XfResult(res_info, res_yhd, res_xhd, res_out_cons, initial_level, xx_level, res_inflow_dic, iteration_level);

            //根据该消峰流量，重新获取该水库的溢洪道、泄洪洞建筑物的调度信息
            Dictionary<DateTime, double> res_totalout_dic;
            Dictionary<string, List<DdInfo>> str_ddinfo = Get_ResYhdXhd_Ddinfo(res_info, res_yhd, res_xhd, res_out_cons, initial_level, xx_level, res_inflow_dic, kx_q, out res_totalout_dic);
            for (int j = 0; j < str_ddinfo.Count; j++)
            {
                res_ddinfos.Add(str_ddinfo.ElementAt(j).Key, str_ddinfo.ElementAt(j).Value);
            }

            return res_totalout_dic;
        }

        //获取水库约束水位下的 消峰控泄流量阈值 (通过迭代计算)
        public static double Get_Res_XfResult(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd,
            Res_Outflow_Constraint res_out_cons, double initial_level, double xx_level,
            Dictionary<DateTime, double> res_inflow_dic, double constraint_level)
        {
            //通过迭代计算水库消峰流量
            double min_q = 0;
            double inflow_max = res_inflow_dic.Values.Max();
            double max_q = inflow_max;
            double kx_q = (min_q + max_q) / 2;

            //计算水库调蓄水位
            int n = 0;
            while (true)
            {
                //进行水库调蓄计算，返回最高调蓄水位
                double max_res_level = Cal_ResStore_ChangeDic(res_info, res_yhd, res_xhd, res_out_cons, initial_level, xx_level, res_inflow_dic, kx_q);

                //当约束水位与最大水位之间误差小于0.1m 或控泄流量基本达到最大(基本不调蓄)或全部调蓄时，则停止迭代
                if (Math.Abs(max_res_level - constraint_level) < 0.1 || kx_q > inflow_max * 0.95 || kx_q < 1.0 || n >= 50) break;

                //继续迭代
                if (max_res_level > constraint_level)
                {
                    //当计算的最大调蓄水位> 约束水位时，表示该设计控泄流量偏小，使的消峰调蓄过多
                    min_q = kx_q;
                }
                else
                {
                    max_q = kx_q;
                }

                kx_q = (min_q + max_q) / 2;
                n++;
            }

            kx_q = Math.Round(kx_q * 1.1 / 10.0) * 10;
            return kx_q;
        }

        //进行水库调蓄计算，返回最高调蓄水位
        public static double Cal_ResStore_ChangeDic(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd, Res_Outflow_Constraint res_out_cons,
            double initial_level, double xx_level, Dictionary<DateTime, double> flood_gc, double kx_q)
        {
            //将入库洪水过程细分到10分钟
            int step_minutes = 10;
            Dictionary<DateTime, double> flood_gc1 = Dfs0.Getdic1(flood_gc, new TimeSpan(0, step_minutes, 0));

            //获取水库的3个关系(溢洪道和泄洪洞水位-泄流流量曲线有可能为空)
            Dictionary<double, double> res_lv = res_info.Level_Volume;
            Dictionary<double, double> yhd_lq = res_info.Level_YhdQ;
            Dictionary<double, double> xhd_lq = res_info.Level_XhdQ;

            //逐时间步进行水库调洪演算，计算水库水位
            Dictionary<DateTime, double> res_level_dic = new Dictionary<DateTime, double>();
            double last_level = initial_level;
            double last_res_volumn = File_Common.Insert_Zd_Value(res_lv, last_level);
            double last_in_q = flood_gc1.First().Value;
            double last_out_q = 0;

            for (int i = 0; i < flood_gc1.Count; i++)
            {
                //获取该水位下溢洪道和泄洪洞最大下泄流量
                double[] yhd_xhd_maxq = Get_YHDXHD_LevelMaxQ(res_yhd, res_xhd, yhd_lq, xhd_lq, last_level);
                double yhd_max_q = yhd_xhd_maxq[0];
                double xhd_max_q = yhd_xhd_maxq[1];

                //该时刻来流流量
                double now_in_q = flood_gc1.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_q + now_in_q) / 2.0) * (step_minutes * 60);

                //**该时刻出流流量**
                double now_out_q = Get_Res_OutDischarge(res_yhd, res_out_cons, xx_level, kx_q, last_level, yhd_max_q, xhd_max_q, now_in_q);

                //该时段出库洪量(m3)
                double out_volumn = ((last_out_q + now_out_q) / 2.0) * (step_minutes * 60);

                //当前水库库容(万m3)
                double now_volumn = last_res_volumn + (in_volumn - out_volumn) / 10000.0;

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

            return max_res_level;
        }

        //计算水库出库流量 (由当前水位、最大过流能力、控泄流量综合判断)
        //1、洪峰没来之前，来多少泄多少，且当库水位大于汛限水位时，泄流要大于来流，使库水位降至汛限水位，一旦降至汛限水位就不再超泄
        //2、当来流流量> 该水位下水库泄洪能力时(溢洪道+泄洪洞)，但还未到达设计控泄流量时，水库按最大过流能力泄洪，被动调蓄
        //3、当来流流量> 设计控泄流量时，且小于该水位下水库泄洪能力时，水库按设计控泄流量下泄，主动调蓄
        public static double Get_Res_OutDischarge(Struct_BasePars res_yhd, Res_Outflow_Constraint res_out_cons, double xx_level,
            double kx_q, double res_level, double yhd_max_q, double xhd_max_q, double now_in_q)
        {
            double total_max_q = yhd_max_q + xhd_max_q;
            double now_out_q = 0;

            //如果有约束，则优先单独考虑
            if (res_out_cons != null)
            {
                if (res_out_cons.outflow_constraint == Outflow_Constraint_Type.no_outflow)  //不出流约束，所有闸门全关
                {
                    //只有当溢洪道存在，且库水位大于溢洪道堰顶高程时，且无闸门时才按溢洪道最大能力下泄，否则就是0出流
                    if (res_yhd != null)
                    {
                        if (res_yhd.gate_type == GateType.NOGATE && res_level > res_yhd.datumn) now_out_q = yhd_max_q;
                    }
                }
                else if (res_out_cons.outflow_constraint == Outflow_Constraint_Type.no_overflow) //不溢流泄洪约束，所有溢洪道闸门全关
                {
                    if (res_yhd != null)
                    {
                        if (res_yhd.gate_type == GateType.NOGATE)
                        {
                            //如果溢洪道没有闸门，则不溢流约束是无效的，该怎么泄怎么泄
                            now_out_q = Get_Res_OutQ2(res_yhd, xx_level, res_level, now_in_q, total_max_q, xhd_max_q);
                        }
                        else
                        {
                            //计算溢洪道带闸门控制，不溢流约束情况下的出流
                            now_out_q = Get_Res_OutQ4(xx_level, kx_q, res_level, now_in_q, total_max_q);
                        }
                    }
                }
            }
            else
            {
                if (res_yhd != null)
                {
                    if (res_yhd.gate_type == GateType.NOGATE)
                    {
                        //如果溢洪道没有闸门
                        now_out_q = Get_Res_OutQ2(res_yhd, xx_level, res_level, now_in_q, total_max_q, xhd_max_q);
                    }
                    else
                    {
                        //计算溢洪道带闸门控制，且无约束情况下的出流
                        now_out_q = Get_Res_OutQ3(xx_level, kx_q, res_level, now_in_q, total_max_q);
                    }
                }
                else
                {
                    //无溢洪道
                    now_out_q = Get_Res_OutQ3(xx_level, kx_q, res_level, now_in_q, total_max_q);
                }
            }

            return now_out_q;
        }

        //计算溢洪道没有闸门，且无约束情况下的出流
        private static double Get_Res_OutQ2(Struct_BasePars res_yhd, double xx_level, double res_level, double now_in_q, double total_max_q, double xhd_max_q)
        {
            double now_out_q = 0;
            if (res_level < res_yhd.datumn)  //当库水位小于溢洪道堰顶高程时
            {
                if (res_level > xx_level)
                {
                    //当库水位大于汛限水位时，按1.5倍来流量超泄(受泄洪洞过流能力约束)
                    now_out_q = Math.Min(now_in_q * 1.5, xhd_max_q);
                }
                else
                {
                    //当库水位小于汛限水位时，来多少泄多少，且受泄洪洞过流能力约束
                    now_out_q = Math.Min(now_in_q, xhd_max_q);
                }
            }
            else       //当库水位大于溢洪道堰顶高程时，泄洪和来流、设计控泄流量均无关，直接按当前水位的最大能力下泄
            {
                now_out_q = total_max_q;
            }

            return now_out_q;
        }

        //计算溢洪道带闸门控制，且无约束情况下的出流
        private static double Get_Res_OutQ3(double xx_level, double kx_q, double res_level, double now_in_q, double total_max_q)
        {
            double now_out_q = 0;

            if (now_in_q > kx_q)  //来流量大于设计控泄流量，要消峰(自由溢流无控泄能力的水库除外)
            {
                now_out_q = Math.Min(kx_q, total_max_q);  //受过流能力约束
            }
            else   //来流量小于设计控泄流量，分2种情况
            {
                if (now_in_q > total_max_q)
                {
                    //来流量小于设计控泄流量，但大于最大泄洪能力，水库按最大过流能力泄洪，被动调蓄
                    now_out_q = total_max_q;
                }
                else   //来流量小于设计控泄流量，且小于最大泄洪能力，又分2种情况
                {
	    now_out_q = Math.Min(now_in_q, total_max_q);
                    //if (res_level > xx_level)
                    //{
                    //    //当库水位大于汛限水位时，按1.5倍来流量超泄
                    //    now_out_q = Math.Min(now_in_q * 1.5, total_max_q);
                   // }
                    //else
                   // {
                   //     //当库水位小于汛限水位时，来多少泄多少，且受总过流能力约束
                   //     now_out_q = Math.Min(now_in_q, total_max_q);
                   // }
                }
            }

            return now_out_q;
        }

        //计算溢洪道带闸门控制，不溢流约束情况下的出流
        private static double Get_Res_OutQ4(double xx_level, double kx_q, double res_level, double now_in_q, double xhd_max_q)
        {
            double now_out_q = 0;

            if (now_in_q > kx_q)  //来流量大于设计控泄流量，要消峰
            {
                now_out_q = Math.Min(kx_q, xhd_max_q);  //受泄洪洞过流能力约束
            }
            else   //来流量小于设计控泄流量，分2种情况
            {
                if (now_in_q > xhd_max_q)
                {
                    //来流量小于设计控泄流量，但大于泄洪洞最大泄洪能力，水库按泄洪洞过流能力泄洪，被动调蓄
                    now_out_q = xhd_max_q;
                }
                else   //来流量小于设计控泄流量，且小于最大泄洪能力，又分2种情况
                {
                    if (res_level > xx_level)
                    {
                        //当库水位大于汛限水位时，按1.5倍来流量超泄
                        now_out_q = Math.Min(now_in_q * 1.5, xhd_max_q);
                    }
                    else
                    {
                        //当库水位小于汛限水位时，来多少泄多少，且受泄洪洞过流能力约束
                        now_out_q = Math.Min(now_in_q, xhd_max_q);
                    }
                }
            }

            return now_out_q;
        }


        //根据消峰流量，获取该水库的溢洪道、泄洪洞建筑物的调度信息
        public static Dictionary<string, List<DdInfo>> Get_ResYhdXhd_Ddinfo(Reservoir res_info, Struct_BasePars res_yhd, Struct_BasePars res_xhd,
            Res_Outflow_Constraint res_out_cons, double initial_level, double xx_level, Dictionary<DateTime, double> flood_gc, double kx_q, out Dictionary<DateTime, double> res_totalout_dic)
        {
            Dictionary<string, List<DdInfo>> yhd_xdh_ddinfos = new Dictionary<string, List<DdInfo>>();

            //将入库洪水过程细分到20分钟
            int step_minutes = 20;
            Dictionary<DateTime, double> flood_gc1 = Dfs0.Getdic1(flood_gc, new TimeSpan(0, step_minutes, 0));

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
            Dictionary<DateTime, double> yhd_maxq_dic = new Dictionary<DateTime, double>();
            Dictionary<DateTime, double> xhd_maxq_dic = new Dictionary<DateTime, double>();

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
                yhd_maxq_dic.Add(now_time, yhd_max_q);
                xhd_maxq_dic.Add(now_time, xhd_max_q);
                double total_max_q = yhd_max_q + xhd_max_q;

                //该时刻来流流量
                double now_in_q = flood_gc1.ElementAt(i).Value;

                //该时段入库洪量(m3)
                double in_volumn = ((last_in_q + now_in_q) / 2.0) * (step_minutes * 60);

                //该时刻出流流量
                double now_out_q = Get_Res_OutDischarge(res_yhd, res_out_cons, xx_level, kx_q, last_level, yhd_max_q, xhd_max_q, now_in_q);
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

                //反向内插当前水库水位
                double now_level = File_Common.Insert_Zd_Key(res_lv, now_volumn);

                last_level = now_level;
                last_res_volumn = now_volumn;
                last_in_q = now_in_q;
                last_out_q = now_out_q;
            }

            //水库合并出流过程
            List<Dictionary<DateTime, double>> out_q_list = new List<Dictionary<DateTime, double>>() { yhd_outq_dic, xhd_outq_dic };
            res_totalout_dic = Dfs0.Combine_Dic(out_q_list);

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

        //从所有特征断面(水文站)中，获取目标断面信息
        public static List<Water_Condition> Get_Target_Section(List<Dispatch_Target> dd_target, string model_instance)
        {
            List<Water_Condition> section_waterlevel = Res11.Get_ReachSection_Info(model_instance);
            List<Water_Condition> target_section = new List<Water_Condition>();
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                Water_Condition section = section_waterlevel[i];
                for (int j = 0; j < dd_target.Count; j++)
                {
                    if (dd_target[j].stcd == section.Stcd) target_section.Add(section);
                }
            }

            return target_section;
        }

        //从所有特征断面(水文站)中，获取目标断面信息
        public static Water_Condition Get_Target_Section(Dispatch_Target dd_target, string model_instance)
        {
            List<Water_Condition> section_waterlevel = Res11.Get_ReachSection_Info(model_instance);
            Water_Condition target_section = new Water_Condition();
            for (int i = 0; i < section_waterlevel.Count; i++)
            {
                Water_Condition section = section_waterlevel[i];

                if (dd_target.stcd == section.Stcd) target_section = section;
            }

            return target_section;
        }


        //获取各水库迭代水位
        public static Dictionary<string, double> Get_ResIter_Levels(List<Res_Level_Constraint> res_level_constraint)
        {
            //约束水位换一下格式
            Dictionary<string, double> constraint_levels = new Dictionary<string, double>();
            for (int i = 0; i < res_level_constraint.Count; i++)
            {
                constraint_levels.Add(res_level_constraint[i].name, res_level_constraint[i].level_value);
            }

            return constraint_levels;
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
                //if (str_outq == 0)
                //{
                //    ddinfo.dd_type = "全关";
                //    ddinfo.open_n = 0;
                //    ddinfo.dd_value = 0;
                //}
                //else if(str_outq == str_maxq)
                //{
                //    ddinfo.dd_type = "全开";
                //    ddinfo.open_n = 1;
                //    ddinfo.dd_value = 0;
                //}
                //else
                //{
                //    ddinfo.dd_type = "控泄";
                //    ddinfo.open_n = 1;
                //    ddinfo.dd_value = Math.Round(str_outq,1);
                //}

                ddinfo.dd_type = "控泄";
                ddinfo.open_n = 1;
                ddinfo.dd_value = Math.Round(str_outq, 1);

                dd_result.Add(time, ddinfo);
            }

            //将调度在时间上的融合
            str_ddinfo = Combine_Str_DdInfo(dd_result);

            return str_ddinfo;
        }

        //根据建筑物出流过程，获取其标准调度过程(时间上的控泄)
        public static List<DdInfo> Get_Str_Ddinfo(Dictionary<DateTime, double> outq_dic)
        {
            List<DdInfo> str_ddinfo = new List<DdInfo>();

            //各时刻调度方式判断
            Dictionary<DateTime, DdInfo> dd_result = new Dictionary<DateTime, DdInfo>();
            for (int i = 0; i < outq_dic.Count; i++)
            {
                DateTime time = outq_dic.ElementAt(i).Key;
                double str_outq = outq_dic.ElementAt(i).Value;

                DdInfo ddinfo = new DdInfo();
                ddinfo.dd_time = SimulateTime.TimetoStr(time);

                ddinfo.dd_type = "控泄";
                ddinfo.open_n = 1;
                ddinfo.dd_value = Math.Round(str_outq, 1);

                dd_result.Add(time, ddinfo);
            }

            //调度过程融合
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

        //获取各水库消峰调蓄库容
        public static Dictionary<string, double> Get_Res_Xftx_Volume(string model_instance,
             Dictionary<string, double> initial_levels, Dictionary<string, double> res_iteration_level)
        {
            Dictionary<string, double> res_xfvolume = new Dictionary<string, double>();

            //获取初始水位和汛限水位的低值,成为消峰调蓄的水位
            Dictionary<string, double> res_xxlevel = Hd11.Read_Res_XXLevels(model_instance);
            Dictionary<string, double> res_xfmin_level = new Dictionary<string, double>();
            for (int i = 0; i < initial_levels.Count; i++)
            {
                string res_name = initial_levels.ElementAt(i).Key;
                double res_level = initial_levels.ElementAt(i).Value;
                double res_xx_level = res_xxlevel.Keys.Contains(res_name) ? res_xxlevel[res_name] : res_level;

                double xfmin_level = Math.Min(res_level, res_xx_level);
                res_xfmin_level.Add(res_name, xfmin_level);
            }

            //通过与迭代水位对比，计算消峰调蓄库容
            for (int i = 0; i < initial_levels.Count; i++)
            {
                //获取水库的库容曲线
                string res_name = initial_levels.ElementAt(i).Key;
                Dictionary<double, double> res_hq = Reservoir.Get_Res_LevelVolume(model_instance, res_name);
                if (res_hq == null) continue; if (res_hq.Count == 0) continue;

                //初始水位和迭代水位
                double initial_level = initial_levels.ElementAt(i).Value;
                double iteration_level = res_iteration_level[res_name];

                //初始库容和迭代库容
                double initial_volume = File_Common.Insert_Zd_Value(res_hq, initial_level);
                double iteration_volume = File_Common.Insert_Zd_Value(res_hq, iteration_level);
                double xf_volume = Math.Max(iteration_volume - initial_volume, 0);

                res_xfvolume.Add(res_name, xf_volume);
            }

            return res_xfvolume;
        }

        #endregion


    }
}