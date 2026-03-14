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
        #region 一维建筑物操作 -- 新增 、更新分洪口(溃口)和普通建筑物、泵站
        //新建分洪口 -- 默认调度规则
        public string Add_Fhkstr(PointXY p, double fhk_width)
        {
            HydroModel hydromodel = this;
            FhkstrInfo fhkstrinfo = Nwk11.Get_Default_Fhkstrinfo(hydromodel, p, fhk_width);
            string fhkstrname = Nwk11.Add_Fhkstr(ref hydromodel, ref fhkstrinfo);
            return fhkstrname;
        }

        //新建分洪口 -- 默认调度规则
        public void Add_Fhkstr(ref Reach_Break_BaseInfo break_info)
        {
            HydroModel hydromodel = this;

            //坐标和桩号转换
            PointXY p_jwd = PointXY.Get_Pointxy(break_info.location[0], break_info.location[1]);
            PointXY p_pro = PointXY.CoordTranfrom(p_jwd, 4326, 4547, 3);

            string business_code = HydroModel.Get_BusinessCode_FromDB(hydromodel.Modelname);
            string reachname = "";
            if(business_code == "embank_break_gq")
            {
                reachname = "GQ";
            }
            else if(business_code == "embank_break_wh")
            {
                reachname = "WH";
            }

            AtReach at_reach = reachname==""? Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList,p_pro): Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, reachname, p_pro);

            //删除已有的溃口建筑
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            List<Controlstr> contro_list = controllist.GateListInfo;
            for (int i = 0; i < contro_list.Count; i++)
            {
                string str_name = contro_list[i].Strname;
                if (str_name.Contains("_KB")) controllist.RemoveGate(str_name);
            }

            //确定分洪水位，如果按最高水位分洪，则需要先试算一次
            double fh_level = break_info.break_level;
            if (break_info.break_condition == "max_level") fh_level = Reach_Break_BaseInfo.Get_Section_MaxLevel(hydromodel, at_reach);

            //新增溃堤建筑物
            FhkstrInfo fhk_str = Nwk11.Get_Default_Fhkstrinfo(hydromodel, at_reach, p_pro, break_info.fh_width,fh_level,break_info.fh_minutes * 60);
            break_info.str_name = Nwk11.Add_Fhkstr(ref hydromodel, ref fhk_str);
            break_info.fh_datumn = fhk_str.dm_level;
            break_info.break_level = fh_level;

           Nwk11.Add_Fhkstr(ref hydromodel, ref fhk_str);
        }

        //新建分洪口 -- 默认调度规则，并耦合到二维
        public string Add_Fhkstr_AndLink(PointXY p, double fhk_width)
        {
            HydroModel hydromodel = this;
            FhkstrInfo fhkstrinfo = Nwk11.Get_Default_Fhkstrinfo(hydromodel, p, fhk_width);
            string fhkstrname = Nwk11.Add_Fhkstr(ref hydromodel, ref fhkstrinfo);

            //添加侧向建筑物连接
            Add_SideStructure_Link(fhkstrname);

            return fhkstrname;
        }

        //新建其他普通建筑物 -- 默认调度规则(全开)，正向
        public string Add_Normalstr(string gatename, PointXY p, Necessary_Attrs neces_attrs)
        {
            HydroModel hydromodel = this;
            return Nwk11.Add_Normalstr(ref hydromodel, gatename, p, neces_attrs);
        }

        //新建泵站-- 默认正向，按设计流量全开
        public string Add_Bumpstr_Normal(string gatename, PointXY p, double design_discharge)
        {
            HydroModel hydromodel = this;
            return Nwk11.Add_Bumpstr(ref hydromodel, gatename, p, design_discharge);
        }

        //新建泵站-- 默认侧向，按设计流量全开
        public string Add_Bumpstr_Side(string gatename, PointXY p, double design_discharge)
        {
            HydroModel hydromodel = this;
            string strname = Nwk11.Add_Bumpstr(ref hydromodel, gatename, p, design_discharge);

            //改为侧向
            Normalstr str = this.Mike11Pars.ControlstrList.GetGate(strname) as Normalstr;
            str.Str_Sidetype = Str_SideType.sidestr;

            return strname;
        }

        //因为溃口设置， 从数据库中更新初始视角
        public static void Update_ModelPlan_InitialView(HydroModel model, Reach_Break_BaseInfo break_baseinfo)
        {
            //转换前端经纬度到大地坐标，并找到所在附近河道和桩号
            PointXY p = PointXY.Get_Pointxy(break_baseinfo.location[0], break_baseinfo.location[1]);
            if (p.X < 200)
            {
                p = PointXY.CoordTranfrom(p, 4326, 4547, 3);
            }
            AtReach atreach = Nwk11.Get_NearReach(model.Mike11Pars.ReachList, p);
            int heading;   //需要更新的那个heading角度
            PointXY view_point = model.Mike11Pars.ReachList.Get_ReachPointxy(atreach.reachname, atreach.chainage - 1000, out heading);//从上游500m俯瞰
            if (heading < 0) heading = heading + 360;
            view_point = PointXY.CoordTranfrom(view_point, 4547, 4326, 6);

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPLAN_TABLENAME;

            //获取最初视角
            string sql = "select view_point from " + tableName + " where plan_code = '" + model.Modelname + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;
            if (nLen == 0) return;
            string view_jsonstr = dt.Rows[0][0].ToString();

            // 解析JSON对象
            JObject jsonObject = JObject.Parse(view_jsonstr);
            JObject cameraObject = (JObject)jsonObject["camera"];

            // 修改x、y和heading属性
            cameraObject["position"]["x"] = view_point.X;
            cameraObject["position"]["y"] = view_point.Y;
            cameraObject["heading"] = heading;
            cameraObject["position"]["spatialReference"]["latestWkid"] = 4326;
            cameraObject["position"]["spatialReference"]["wkid"] = 4326;

            // 将修改后的JSON对象转换回字符串
            string new_view = jsonObject.ToString();

            //更新数据库的污染源信息
            sql = $"UPDATE {tableName} SET view_point=:new_view WHERE plan_code=:planCode";

            KdbndpParameter[] mysqlPara =
            {
                    new KdbndpParameter(":new_view", new_view),
                    new KdbndpParameter(":planCode", model.Modelname)
             };
            Mysql.Execute_Command(sql, mysqlPara);

            Console.WriteLine("视角修改完成！");
        }

        #endregion


        #region  一维建筑物操作 -- 改变建筑物位置、参数
        //改变闸门位置 -- 根据给定的点坐标
        public void Change_GateLocation(string strname, PointXY newloacal_p)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateLocation(ref hydromodel, strname, newloacal_p);
        }

        //改变闸门位置 -- 根据给定河道位置
        public void Change_GateLocation(string strname, AtReach atreach)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateLocation(ref hydromodel, strname, atreach);
        }

        //改变闸门基本属性 -- 根据必须的5个参数(闸门类型、数量、单宽、底高、最大开启高程)
        public void Change_GateAttributes(string strname, Necessary_Attrs new_nes_attributes)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateAttributes(ref hydromodel, strname, new_nes_attributes);
        }

        //改变弧形闸门参数 -- 按弧形闸门高根据规范推算其他参数
        public void Change_GateRadiapars(string strname, double radia_height)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateRadiapars(ref hydromodel, strname, radia_height);
        }

        //改变公式闸门参数 - 根据因子相应扩大和缩小
        public void Change_GateFormulapars(string strname, double factor_a, double factor_b)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_GateFormulapars(ref hydromodel, strname, factor_a, factor_b);
        }

        //改变分洪口分洪水位 -- 为堤顶高程
        public void Change_FhkWaterLevel_ToMax(string strname)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_FhkWaterLevel_ToMax(ref hydromodel, strname);
        }

        //改变分洪口分洪水位 -- 为指定水位
        public void Change_FhkWaterLevel_ToValue(string strname, double new_waterlevel)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_FhkWaterLevel_ToValue(ref hydromodel, strname, new_waterlevel);
        }

        //改变分洪口溃堤时间 -- 为指定时间
        public void Change_FhkBreakTime(string strname, TimeSpan new_breaktime)
        {
            HydroModel hydromodel = this;
            Nwk11.Change_FhkBreakTime(ref hydromodel, strname, new_breaktime);
        }
        #endregion


        #region 一维建筑物操作 -- 改变建筑物调度规则
        //通过迭代计算，获得所有闸门优化的闸站调度方式，并完整计算提取结果
        public void Start_IterCal()
        {
            HydroModel hydromodel = this;

            WgOptdd.Get_OptimizeDd_CalResult(hydromodel);
        }

        //将全部闸门改为规则调度
        public void Changeddgz_AllToGZDU()
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_AllToGZDU(ref hydromodel);
        }

        //将全部闸门改为全开
        public void Changeddgz_AllToZMDU_FullOpen()
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_AllToZMDU_FullOpen(ref hydromodel);
        }

        //将指定闸门 改为规则调度 -- 闸门类型的全开   
        public void Changeddgz_ToGZDU(string strname)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToGZDU(ref hydromodel, strname);
        }

        //将指定闸门 改为控泄调度 -- 指定流量   
        public void Changeddgz_ToKXDU(string strname, double ddparams_double)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, ddparams_double);
        }

        //将指定闸门 改为控泄调度 -- 以水位流量关系确定流量   
        public void Changeddgz_ToKXDU(string strname, double ddparams_double, List<double[]> str_relation)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToKXDU(ref hydromodel, strname, ddparams_double, str_relation);
        }

        //将指定闸门 改为控泄时间调度 -- 指定控泄流量过程
        public void Changeddgz_ToKXDU_TIME(string strname, Dictionary<DateTime, double> dischargedic)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToKXDU_TIME(ref hydromodel, strname, dischargedic);
        }

        //将指定闸门 改为闸门调度 
        public void Changeddgz_ToZMDU(string strname, ZMDUinfo ddparams_zmdu)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        //将指定闸门 改为闸门时间调度 -- 指定闸门开启高度过程
        public void Changeddgz_ToZMDU_TIME(string strname, Dictionary<DateTime, double> gateheight_dic)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToZMDU_TIME(ref hydromodel, strname, gateheight_dic);
        }

        //根据数据库闸站状态，修改所有的建筑物闸站调度方式及数据
        public void Update_AllStr_DdInfo_FromGB()
        {
            HydroModel hydromodel = this;
            Item_Info.Update_AllStr_DdInfo_FromGB(ref hydromodel);

            this.Model_state = Model_State.Ready_Calting;

            //重新保存模型 --保存模型实体和调度信息，但未更新状态
            this.Save();

            //更新模型状态为待计算  --不再保存模型实体
            string[] model_info = Item_Info.Get_Model_Info(this, "", false);
            Update_ModelStateInfo(this, model_info[4],false);

            //单独更新闸站状态信息
            Update_Model_Ddinfo(hydromodel, model_info[6]);
        }

        //对比和更新所有建筑物的调度方式(每个建筑物每次指令调度有4个参数 -- 时间、调度方式、开闸数、调度量)
        public void Update_AllStr_DdInfo(Dictionary<string, List<DdInfo>> new_ddinfos)
        {
            HydroModel hydromodel = this;
            Item_Info.Update_AllStr_DdInfo(ref hydromodel, new_ddinfos);

            this.Model_state = Model_State.Ready_Calting;

            //重新保存模型 --保存模型实体，但未更新状态
            this.Save();

            //更新模型状态信息为待计算
            string[] model_info = Item_Info.Get_Model_Info(this, "", false);

            //更新模型状态为待计算 --不再保存模型实体
            Update_ModelStateInfo(this, model_info[4],false);

            //重新与给定的需要修改的闸门调度信息匹配下
            Match_Gatedd_Infos(ref model_info, new_ddinfos);

            //单独更新调度信息
            Update_Model_Ddinfo(hydromodel, model_info[6]);
        }

        //重新与给定的需要修改的闸门调度信息匹配
        public static void Match_Gatedd_Infos(ref string[] model_info, Dictionary<string, List<DdInfo>> new_ddinfos)
        {
            string gate_ddinfo = model_info[6];
            List<string[]> dd_list = JsonConvert.DeserializeObject<List<string[]>>(gate_ddinfo);

            //重新匹配
            for (int i = 0; i < dd_list.Count; i++)
            {
                string firstString = dd_list[i][0];
                for (int j = 0; j < new_ddinfos.Count; j++)
                {
                    if (firstString == new_ddinfos.ElementAt(j).Key)
                    {
                        List<DdInfo> dd_type = new_ddinfos.ElementAt(j).Value;
                        List<string[]> dd_info = new List<string[]>();
                        for (int k = 0; k < dd_type.Count; k++)
                        {
                            string[] gate_dd = new string[] { dd_type[k].dd_time, dd_type[k].dd_type, dd_type[k].open_n.ToString(), dd_type[k].dd_value.ToString() };
                            dd_info.Add(gate_dd);
                        }

                        string dd_str = File_Common.Serializer_Obj(dd_info);
                        dd_list[i][5] = dd_str;
                    }
                }
            }

            model_info[6] = JsonConvert.SerializeObject(dd_list);
        }

        //对比和更新单个建筑物的调度方式(3个参数 -- 英文名、调度方式、调度量)
        public void Update_Str_DdInfo(string[] new_ddinfo)
        {
            HydroModel hydromodel = this;
            Item_Info.Update_Str_DdInfo(ref hydromodel, new_ddinfo);

            //重新保存模型
            this.Save();

            //更新模型状态信息为待计算
            this.Model_state = Model_State.Ready_Calting;
            string[] model_info = Item_Info.Get_Model_Info(this, "", false);
            Update_ModelStateInfo(this, model_info[4]);
        }

        //判断采用哪一种闸门时间调度方式(开度过程\还是闸门数过程\还是两者都是)
        public void Changeddgz_ToZMDU_TIME_Select(string strname, Dictionary<DateTime, double> gateheight_dic, Dictionary<DateTime, double> gatenumber_dic)
        {
            HydroModel hydromodel = this;
            Nwk11.ZMDU_TIME_Select(ref hydromodel, strname, gateheight_dic, gatenumber_dic);
        }

        //判断各闸门调度的变化，修改变化闸门的调度方式
        public void Update_GateDd(string strname, ZMDUinfo ddparams_zmdu)
        {
            HydroModel hydromodel = this;
            Nwk11.Changeddgz_ToZMDU(ref hydromodel, strname, ddparams_zmdu);
        }

        #endregion


        #region 一维河道操作 -- 增加新河道、增加减少河道和产汇流的连接
        //增加一条新河道 -- 根据河名和河道控制点
        public void Add_NewReach(string newreachname, List<PointXY> reachpoints)
        {
            HydroModel hydromodel = this;

            //第1步 -- 增加新河道，更新河道集合
            Nwk11.Add_NewReach_Network(ref hydromodel, ref newreachname, ref reachpoints);

            //第2步 -- 调用GIS服务剖切断面，更新河道断面集合和数据库
            Xns11.Add_NewReach_Section(ref hydromodel, newreachname);

            //第3步 -- 判定连接关系，生成边界条件，更新边界条件集合
            Bnd11.Add_NewReach_Bnd(ref hydromodel, newreachname);
        }

        //新增RR产汇流流域连接 - 不分产汇流模型类型
        public void Add_CatchmentConnect(string catchmentname, Reach_Segment connect_reachseg)
        {
            HydroModel hydromodel = this;
            Nwk11.Add_CatchmentConnect(ref hydromodel, catchmentname, connect_reachseg);
        }

        //断开指定RR产汇流流域连接 - 不分产汇流模型类型
        public void Substract_catchment_conncet(string catchmentname)
        {
            HydroModel hydromodel = this;
            Nwk11.Substract_catchmentconncet(ref hydromodel, catchmentname);
        }

        //断开所有RR产汇流流域连接 - 不分产汇流模型类型
        public void Substract_allcatchment_conncet()
        {
            HydroModel hydromodel = this;
            int n = hydromodel.Mike11Pars.Catchment_Connectlist.CatchmentConnect_Infolist.Count;
            for (int i = 0; i < n; i++)
            {
                string catchmentname = hydromodel.Mike11Pars.Catchment_Connectlist.CatchmentConnect_Infolist[0].catchment_name;
                Nwk11.Substract_catchmentconncet(ref hydromodel, catchmentname);
            }
        }
        #endregion


        #region 一维断面操作 -- 更新某河段断面,断面数据获取（一般可通过sectionlist类的成员方法获取）
        //更新已有河段 断面数据 -- 原桩号保持不变，重新从DEM上剖切,该操作不会改变河网、边界等数据
        public void Update_ReachSection_OldeSecChainage(Reach_Segment reach_segment)
        {
            HydroModel hydromodel = this;
            Xns11.Update_ReachSection_FromDEM(ref hydromodel, reach_segment);
        }

        //更新已有河段 断面数据 -- 重新读入新的断面桩号和数据
        public void Update_ReachSection_NewSecChainage(Reach_Segment reachsegment, ref Dictionary<AtReach, List<PointXZS>> reachsectiondata)
        {
            HydroModel hydromodel = this;
            Xns11.Update_ReachSection(ref hydromodel, reachsegment, ref  reachsectiondata);
        }
        #endregion


        #region 一维断面与水库连接操作 -- 将水库库容挂在断面上以发挥水库的调蓄作用
        //将水库与一维河道连接 -- 1个连接点,substract_sectionvolume(是否核减断面占用库容)
        public void Connect_Reservoir_OnSection(Dictionary<double, double> Level_Volume, PointXY p, bool substract_sectionvolume)
        {
            HydroModel hydromodel = this;
            Xns11.Connect_Reservoir_OnSection(hydromodel, Level_Volume, p, substract_sectionvolume);
        }

        //将水库与一维河道连接 -- 2个连接点
        public void Connect_Reservoir_OnSection(Dictionary<double, double> Level_Volume, PointXY p1, PointXY p2, bool substract_sectionvolume)
        {
            HydroModel hydromodel = this;
            Xns11.Connect_Reservoir_OnSection(hydromodel, Level_Volume, p1, p2, substract_sectionvolume);
        }
        #endregion


        #region 一维边界条件操作 -- 修改边界条件，新增旁侧入流
        //请求NAM产汇流模型计算的各子流域洪水流量过程
        public Dictionary<string, Dictionary<DateTime, double>> Modify_BndID_ToDischargeDic_FromNAM()
        {
            HydroModel hydromodel = this;

            //通过网络请求降雨过程
            string request_rain_url = Mysql_GlobalVar.catchmentrain_serverurl; 
            request_rain_url += "?planCode=" + hydromodel.Modelname;
            Dictionary<string, Dictionary<DateTime, double>> catchment_rain = Item_Info.Get_Catchment_RainGC_FromHttpRequest(request_rain_url);
            
            //请求产汇流模型计算 网络地址
            string request_flood_url = Mysql_GlobalVar.nam_serverurl;

            //请求参数组合
            string plan_code = hydromodel.Modelname;
            List<object> request_pars = new List<object>();
            request_pars.Add(plan_code);
            request_pars.Add(catchment_rain);
            request_pars.Add(85.0);
            request_pars.Add(0);
            //request_pars.Add(HydroModel.Get_BusinessCode_FromDB(plan_code));
            string request_pars_str = File_Common.Serializer_Obj(request_pars);

            Dictionary<string, object> allrequest_pars = new Dictionary<string, object>();
            allrequest_pars.Add("request_type", "catchment_cal");
            allrequest_pars.Add("request_pars", request_pars_str);
            string allrequest_pars_str = File_Common.Serializer_Obj(allrequest_pars);

            //请求产汇流模型计算所有子流域洪水过程
            Dictionary<string, Dictionary<DateTime, double>> catchment_q = Item_Info.Get_Catchment_FloodGC_FromHttpRequest(hydromodel, request_flood_url, allrequest_pars_str);
            Bnd11.Modify_Bnds_ToDischargeDic(ref hydromodel, catchment_q);
            return catchment_q;
        }

        //请求产汇流模型，修改所有相同边界条件ID的流量过程
        public Dictionary<string, Dictionary<DateTime, double>> Modify_BndID_ToDischargeDic()
        {
            HydroModel hydromodel = this;

            //请求产汇流模型计算 网络地址
            string request_url = Mysql_GlobalVar.catchmentq_serverurl;

            //请求参数
            string plan_code = hydromodel.Modelname;
            request_url += "?planCode=" + plan_code;

            //请求产汇流模型计算所有子流域洪水过程
            Dictionary<string, Dictionary<DateTime, double>> catchment_q = Item_Info.Get_CatchmentQ_FromHttpRequest(hydromodel, request_url);
            Bnd11.Modify_Bnds_ToDischargeDic(ref hydromodel, catchment_q);
            return catchment_q;
        }

        //综合判断修改边界条件
        public void Change_Reach_Boundry(string bnd_type, string bnd_value)
        {
            HydroModel hydromodel = this;
            Bnd11.Change_Reach_Boundry(hydromodel,bnd_type,bnd_value);
        }

        //将全部子流域入流边界条件改为0
        public void Modify_BndID_ToNoInflow()
        {
            //获取各子流域ID和默认的产汇流模型
            Dictionary<string, string> rf_models = RR11.Get_Default_RFmodel();

            for (int i = 0; i < rf_models.Count; i++)
            {
                Modify_BndID_ToConstDischarge(rf_models.ElementAt(i).Key,0.01);
            }
        }

        //修改指定边界条件 为常量值入流
        public void Modify_BndID_ToConstDischarge(string bnd_id,double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_BndID_ToConstDischarge(ref hydromodel, bnd_id, const_discharge);
        }

        //修改指定河道上边界为 常量值入流
        public void Modify_ReachUpBnd_ToConstDischarge(string reachname, double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachUpBnd_ToConstDischarge(ref hydromodel, reachname, const_discharge);
        }

        //修改指定河道上边界为 常量水位
        public void Modify_ReachUpBnd_ToConstwl(string reachname, double const_waterlevel)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachUpBnd_ToConstwl(ref hydromodel, reachname, const_waterlevel);
        }

        //修改指定河道上边界为 时间过程入流
        public void Modify_ReachUpBnd_ToDischargeDic(string reachname, Dictionary<DateTime, double> discharge_dic)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachUpBnd_ToDischargeDic(ref hydromodel, reachname, discharge_dic);
        }

        //修改指定河道下边界为 水位流量关系
        public void Modify_ReachDownBnd_ToQH(string reachname)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachDownBnd_ToQH(ref hydromodel, reachname);
        }

        //修改指定河道下边界为 常量水位
        public void Modify_ReachDownBnd_ToConstwl(string reachname, double const_waterlevel)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachDownBnd_ToConstwl(ref hydromodel, reachname, const_waterlevel);
        }

        //修改指定河道下边界为 水位过程
        public void Modify_ReachDownBnd_Towldic(string reachname, Dictionary<DateTime, double> waterleveldic)
        {
            HydroModel hydromodel = this;
            Bnd11.Modify_ReachDownBnd_Towldic(ref hydromodel, reachname, waterleveldic);
        }

        //新增 随时间变化 的旁侧入流  --  点源(HD)
        public void Add_NewInflowBd(AtReach atreach, Dictionary<DateTime, double> discharge)
        {
            HydroModel hydromodel = this;
            string new_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + atreach.reachname + atreach.chainage + "_newrl.dfs0";

            //创建入流过程的dfs0文件
            Dfs0.Dfs0_Creat(new_in_filename, discharge, dfs0type.discharge);

            //加入边界集合
            Bnd11.Add_NewInflowBd(ref hydromodel, atreach, new_in_filename, "discharge");
        }

        //新增的 随时间变化 旁侧入流  --  分布源(HD)
        public void Add_NewInflowBd(Reach_Segment seg_reach, Dictionary<DateTime, double> discharge)
        {
            HydroModel hydromodel = this;
            string new_in_filename = Path.GetDirectoryName(hydromodel.Modelfiles.Bnd11_filename) + @"\" + seg_reach.reachname + seg_reach.start_chainage + "_" + seg_reach.end_chainage + "_newrl.dfs0";

            //创建入流过程的dfs0文件
            Dfs0.Dfs0_Creat(new_in_filename, discharge, dfs0type.discharge);

            //加入边界集合
            Bnd11.Add_NewInflowBd(ref hydromodel, seg_reach, new_in_filename, "discharge");
        }

        //新增 常量 旁侧入流  --  点源(HD)
        public void Add_NewInflowBd(AtReach atreach, double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Add_NewInflowBd(ref hydromodel, atreach, const_discharge);
        }

        //新增 常量 旁侧入流  --  分布源(HD)
        public void Add_NewInflowBd(Reach_Segment seg_reach, double const_discharge)
        {
            HydroModel hydromodel = this;
            Bnd11.Add_NewInflowBd(ref hydromodel, seg_reach, const_discharge);
        }

        #endregion


        #region 一维河道参数操作 -- 修改水库和河道初始水位，修改河道糙率，输出点

        //根据数据库实时水位更新全部河道水库初始水位和河道基流流量(贾鲁河流域)
        public void Update_InitiallevelAndBaseIn()
        {
            //如果基础模型是默认模型，则从数据库读取实时水位修改初始条件,修改各河道边界条件基流
            if (this.BaseModel.Modelname == "" || this.BaseModel.Modelname == Model_Const.DEFAULT_MODELNAME)
            {
                //用流量推算控制断面水位，修改河道沿线初始水位
                this.Update_InitialWaterlevel();

                //通过一定算法，获得几个主要河道额外基流流量(也可能是负值，表示取水)
                Dictionary<string, double> base_in_q = Item_Info.Get_MainReach_Base_Discharge();

                //修改河道与初始水位对应的边界条件基流流量
                if (base_in_q == null) return;
                for (int i = 0; i < base_in_q.Count; i++)
                {
                    this.Modify_BndID_ToConstDischarge(base_in_q.ElementAt(i).Key, base_in_q.ElementAt(i).Value);
                }
            }
        }

        //根据数据库实时水位更新全部河道水库初始水位
        public void Update_InitialWaterlevel()
        {
            HydroModel hydromodel = this;
            Hd11.Update_InitialWaterlevel(ref hydromodel);
        }

        //根据给定水位更新水库河道初始水位
        public void Update_InitialWaterlevel(Dictionary<string, double> inital_level)
        {
            HydroModel hydromodel = this;
            Hd11.Update_InitialWaterlevel(ref hydromodel, inital_level);
        }

        //更新边界条件的基流
        public void Update_Base_Discharge()
        {
            //获取主要河道额外 基流流量
            Dictionary<string, double> base_in_q = Item_Info.Get_MainReach_Base_Discharge();

            //修改河道与初始水位对应的边界条件基流流量
            if (Item_Info.Reach_NowDischarge != null)
            {
                for (int i = 0; i < base_in_q.Count; i++)
                {
                    this.Modify_BndID_ToConstDischarge(base_in_q.ElementAt(i).Key, base_in_q.ElementAt(i).Value);
                }
            }
        }

        //修改某位置的初始水位值
        public void Modify_ReachSection_InitialWaterLevel(AtReach atreach, double initialWaterLevel)
        {
            HydroModel hydromodel = this;
            Hd11.Modify_InitialWaterLevel(ref hydromodel, atreach, initialWaterLevel);
        }

        // 重新设置某河的糙率
        public void Modify_Reach_Resistance(string reachname, double resistance)
        {
            HydroModel hydromodel = this;
            Hd11.Modify_BedResistance(ref hydromodel, reachname, resistance);
        }

        // 添加新的河道的初始水位
        public void Add_NewInitial_WaterLevel(AtReach atreach, double initial_waterlevel)
        {
            HydroModel hydromodel = this;
            Hd11.Add_InitialWaterLevel(ref hydromodel, atreach, initial_waterlevel);
        }

        // 添加河道点的时间序列结果输出
        public void Add_OutputGridPoint(AtReach reachGrid)
        {
            HydroModel hydromodel = this;
            Hd11.Add_OutputGridPoint(ref hydromodel, reachGrid);
        }

        // 添加水质新的组分
        public void Add_NewComponent(string new_component)
        {
            HydroModel hydromodel = this;
            Ad11.Add_Component(ref hydromodel, new_component);
        }

        // 修改全局扩散系数 -- 用一个值
        public void Modify_Dispersion(double dispersion)
        {
            HydroModel hydromodel = this;
            Ad11.Modify_Dispersion(ref hydromodel, dispersion);
        }

        // 修改所有组分的衰减系数 -- 用一个值
        public void Modify_Decay(double decay)
        {
            HydroModel hydromodel = this;
            Ad11.Modify_Decay(ref hydromodel, decay);
        }
        #endregion
    }
}
