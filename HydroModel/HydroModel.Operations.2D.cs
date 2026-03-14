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
        #region 二维堤防操作 -- 正常挡水 、完全挡水、取消堤防（不挡水）、新建堤防
        //堤防操作 -- 正常挡水 、完全挡水（+100m)、完全不挡水 ,dike编号从1开始
        public void Dike_Opereate(string dikename, Dike_Operate operate)
        {
            HydroModel hydromodel = this;
            M21fm.Dike_Opereate(ref hydromodel, dikename, operate);
        }

        //新建堤防1 -- 仅给走向控制点，其他默认
        public void Add_Dike(string newdikename, List<PointXY> pointlist)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, newdikename);
        }

        //新建堤防2 -- 给走向控制点和堤防高度
        public void Add_Dike(string dikename, List<PointXY> pointlist, double dike_aveheight)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, dikename, dike_aveheight);
        }

        //新建堤防3 -- 给走向控制点和堤防高程常量
        public void Add_Dike(string dikename, List<PointXY> pointlist, double dike_crest, bool isconst)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, dikename, dike_crest, true);
        }

        //新建堤防4 -- 给走向和高程控制点
        public void Add_Dike(string dikename, List<PointXYZ> pointlist)
        {
            HydroModel hydromodel = this;
            M21fm.Add_Dike(ref hydromodel, pointlist, dikename);
        }
        #endregion


        #region 二维堰(涵洞)操作 -- 正常过水、堵死不过水、新增单独涵洞、新增堤防上的涵洞并将堤防一分为二
        //涵洞操作 -- 正常启用过水、堵死不过水
        public void Weir_Opereate(string weirname, Weir_Operate operate)
        {
            HydroModel hydromodel = this;
            M21fm.Weir_Opereate(ref hydromodel, weirname, operate);
        }

        //新增涵洞1 -- 在已有dike上增加涵洞(堰底取地面高程)，并将原来的dike一分为二
        public void AddWeirAtDike(string new_weirname, PointXY point, double weir_width)
        {
            HydroModel hydromodel = this;
            M21fm.AddWeirAtDike(ref hydromodel, point, new_weirname, weir_width);
        }

        //新增涵洞2 -- 在已有dike上增加涵洞(给定堰底高程)，并将原来的dike一分为二
        public void AddWeirAtDike(string weir_name, PointXY point, double weir_width, double datum)
        {
            HydroModel hydromodel = this;
            M21fm.AddWeirAtDike(ref hydromodel, point, weir_name, weir_width, datum);
        }
        #endregion


        #region 二维其他参数操作
        //设置干湿边界
        public void Set_Mike21_DryWet(Dry_Wet new_drywet)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            m21pars.Dry_wet = new_drywet;
        }

        //设置保存时间步长
        public void Set_Mike21_Savetimestep(SaveTimeStep savetimestep)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            m21pars.Mike21_savetimestepbs = savetimestep;
        }

        //设置常量蒸发(mm/day)
        public void Set_Mike21_ConstEva(double const_eva)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            Pre_Eva pre_eva = m21pars.Eva_set;
            pre_eva.type_pre_eva = Type_Pre_Eva.Pre_Eva;
            pre_eva.constantEvaporation = const_eva;
            m21pars.Eva_set = pre_eva;
        }

        //按该月多年平均蒸发速率设置常量蒸发
        public void Set_Mike21_AverageConstEva()
        {
            //获取该月常量蒸发速率(mm/day)
            DateTime start_time = this.ModelGlobalPars.Simulate_time.Begin;
            DateTime end_time = this.ModelGlobalPars.Simulate_time.End;
            Dictionary<DateTime, double> Average_DayEvaRfRate = CatchmentList.Get_Average_DayEvaRfRate(start_time, end_time, Eva_RF.eva);
            double const_eva = Average_DayEvaRfRate.Values.ElementAt(Average_DayEvaRfRate.Count - 1);

            //设置常量蒸发
            Set_Mike21_ConstEva(const_eva);
        }

        //设置常量初始水位
        public void Set_Mike21_ConstInitial_WaterLevel(double const_waterlevel)
        {
            Mike21_Pars m21pars = this.Mike21Pars;
            Initial_Contition initial = m21pars.Initial_set;
            initial.initial_type = 1;
            initial.const_surface_elevation = const_waterlevel;
            initial.fileName = "";
            initial.itemName = "";

            m21pars.Initial_set = initial;
        }

        //如果业务模型包含二维，则请求二维模型服务参数设置和计算服务
        public static void Request_Mike21_Server(HydroModel model, List<string> instance_list,string model_instance,string business_code)
        {
            try
            {
                //同步请求二维模型来修改边界条件(二维模型会请求一维模型特定结果来修改其边界条件)
                string http_url = Mysql_GlobalVar.mike21_serverurl;
                string changebnd_url = http_url + "?request_type=change_reachflood&request_pars=[\"" + model.Modelname + "\",\"" + model.Modelname + "\"]";
                File_Common.Get_HttpReSponse(changebnd_url);
                Thread.Sleep(2000);

                //如果是溃堤模型，则请求二维设置溃口
                if (business_code == "embank_break_gq" || business_code == "embank_break_wh")
                {
                    string post_request_pars = Get_Modify_ReachBreak_RequestPars(model, model_instance);
                    if (post_request_pars != null) File_Common.Post_HttpReSponse(http_url, post_request_pars);
                }

                //如果是闸门故障模型，则请求二维设置故障闸门
                if (business_code.Contains("fault"))
                {
                    string post_request_pars = Get_Modify_FaultGate_RequestPars(model,model_instance);
                    if (post_request_pars != null) File_Common.Post_HttpReSponse(http_url, post_request_pars);
                }

                //请求修改初始水情
                //Dictionary<string, object> post_request_pars = Get_Modify_InitialLevel_RequestPars(model, instance_list);
                //if (post_request_pars != null) File_Common.Post_HttpReSponse(http_url, File_Common.Serializer_Obj(post_request_pars));

                //请求二维模型开始计算
                Thread.Sleep(2000);
                string run_url = http_url + "?request_type=run_model&request_pars=" + model.Modelname;
                File_Common.Get_HttpReSponse(run_url);
            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
                throw;
            }
        }

        //获取二维设置故障闸门设置请求参数
        private static string Get_Modify_FaultGate_RequestPars(HydroModel model, string model_instance)
        {
            //组合成二维需要的对象
            List<GateNow> gate_state_list = new List<GateNow>();

            //获取建筑物各闸门基础信息
            string mike21_modelinstance = HydroModel.Get_Mike21_ModelInstance(model.Modelname);
            Dictionary<string, object> fault_baseinfo = FaultGate_BaseInfo.Read_StrGate_Info(mike21_modelinstance);
            List<FaultGate_BaseInfo> gate_info = fault_baseinfo["gate_info"] as List<FaultGate_BaseInfo>;

            //获取闸门故障设置信息
            string fault_info_desc = FaultGate_BaseInfo.Get_FaultGate_SetInfo(model.Modelname, model_instance);
            Dictionary<string, object> fault_info = JsonConvert.DeserializeObject<Dictionary<string, object>>(fault_info_desc);
            List<object[]> gate_h_list = JsonConvert.DeserializeObject<List<object[]>>(fault_info["gate_h"].ToString());

            //各个闸门信息
            for (int i = 0; i < gate_info.Count; i++)
            {
                string open_state = "半开";
                double open_h = double.Parse(gate_h_list[i][2].ToString());
                if (open_h == gate_info[i].gate_h) open_state = "全开";
                if (open_h == 0) open_state = "全关";

                GateNow gate_state = new GateNow(gate_info[i].gate_code, gate_info[i].gate_name,
                    gate_info[i].gate_h, gate_info[i].gate_bottom, open_h, open_state);

                gate_state_list.Add(gate_state);
            }

            //组合二维模型要设置故障闸门的信息
            List<object> request_pars = new List<object>();
            request_pars.Add(model.Modelname);
            request_pars.Add(gate_state_list);
            Dictionary<string, object> post_request_pars = new Dictionary<string, object>();
            post_request_pars.Add("request_type", "set_fault_gate");
            post_request_pars.Add("request_pars", request_pars);

            string post_string = File_Common.Serializer_Obj(post_request_pars);
            return post_string;
        }

        //获取二维模型设置溃口请求参数
        private static string Get_Modify_ReachBreak_RequestPars(HydroModel model,string model_instance)
        {
            //从数据库获取一维模型的溃口信息
            string break_info = Reach_Break_BaseInfo.Get_Reach_BreakInfo(model.Modelname, model_instance);
            if (break_info == null || break_info == "") return null;
            Reach_Break_BaseInfo break_baseinfo = JsonConvert.DeserializeObject<Reach_Break_BaseInfo>(break_info);
            string break_reachname = "SSS_BRANCH_" + model.Mike11Pars.ControlstrList.GateListInfo.Last().Strname + "_GZDU";
            AtReach atreach = AtReach.Get_Atreach(break_reachname,50);
            Dictionary<DateTime, double> dischage_dic = Res11.Res11reader(model.Modelname, atreach, mike11_restype.Discharge);
            DateTime fh_starttime = dischage_dic.FirstOrDefault(x => x.Value > 1.0).Key;
            DateTime fh_endtime = fh_starttime.AddMinutes(break_baseinfo.fh_minutes);
            break_baseinfo.fh_start_time = SimulateTime.TimetoStr(fh_starttime);
            break_baseinfo.fh_finish_time = SimulateTime.TimetoStr(fh_endtime);

            //更新模型参数数据库中的溃口设置信息
            Reach_Break_BaseInfo.Update_Reach_BreakInfo(model.Modelname, model_instance, break_baseinfo);

            //组合成二维需要的对象
            Mike21_Reach_BreakInfo mike21_breakinfo = new Mike21_Reach_BreakInfo(break_baseinfo.location, break_baseinfo.fh_width, break_baseinfo.fh_datumn,
                break_baseinfo.fh_start_time, break_baseinfo.fh_finish_time);

            //组合二维模型要修改溃口的信息
            List<object> request_pars = new List<object>();
            request_pars.Add(model.Modelname);
            request_pars.Add(mike21_breakinfo);
            Dictionary<string, object> post_request_pars = new Dictionary<string, object>();
            post_request_pars.Add("request_type", "change_reach_break");
            post_request_pars.Add("request_pars", request_pars);

            string post_string = File_Common.Serializer_Obj(post_request_pars);
            return post_string;
        }

        //请求二维模型来修改初始条件(请求参数包含水库水位)
        private static Dictionary<string, object> Get_Modify_InitialLevel_RequestPars(HydroModel model, List<string> instance_list)
        {
            //从数据库获取一维模型的初始水情条件
            string intial_water = Item_Info.Get_Model_SingleParInfo(model.Modelname, "mike11_initial_info"); 
            if (intial_water == null || intial_water == "") return null;
            Dictionary<string, List<List<object>>> intial_water_obj = JsonConvert.DeserializeObject<Dictionary<string, List<List<object>>>>(intial_water);

            //获取松耦合的二维模型实例
            List<string> mike21_instancs = new List<string>();
            for (int i = 0; i < instance_list.Count; i++)
            {
                if (instance_list[i].Contains("mike21")) mike21_instancs.Add(instance_list[i]);
            }

            //从数据库获取该模型实例的初始水位设置区域信息(可有多个) 
            string mike21_instance = mike21_instancs[0];  //后期再考虑多个二维模型松耦合的情况
            string changebnd_url = Mysql_GlobalVar.mike21_serverurl + "?request_type=get_initial_regionname&request_pars=" + mike21_instance;
            string mike21_initialregion_names = File_Common.Get_HttpReSponse(changebnd_url);
            Dictionary<string,string> region_names = JsonConvert.DeserializeObject<Dictionary<string, string>>(mike21_initialregion_names);

            //组合二维模型要修改初始水情的信息
            List<object> request_pars = new List<object>();
            List<Dictionary<string, object>> initial_levels = new List<Dictionary<string, object>>();
            for (int i = 0; i < region_names.Count; i++)
            {
                string resion_id = region_names.ElementAt(i).Key;
                string resion_name = region_names.ElementAt(i).Value;
                Dictionary<string, object> initial_level = new Dictionary<string, object>();

                for (int j = 0; j < intial_water_obj.Count; j++)
                {
                    List<List<object>> intial_water_array = intial_water_obj.ElementAt(j).Value;
                    for (int k = 0; k < intial_water_array.Count; k++)
                    {
                        if (intial_water_array[k][1].ToString() == resion_name)
                        {
                            double level = Double.Parse(intial_water_array[k][2].ToString());
                            initial_level.Add("res_id", resion_id);
                            initial_level.Add("res_name", resion_name);
                            initial_level.Add("initial_level_name", "指定水位: " + intial_water_array[k][2].ToString());
                            initial_level.Add("initial_level", level);

                            initial_levels.Add(initial_level);
                            break;
                        }
                    }
                }
            }
            request_pars.Add(model.Modelname);
            request_pars.Add(initial_levels);

            Dictionary<string, object> post_request_pars = new Dictionary<string, object>();
            post_request_pars.Add("request_type", "change_res_initiallevel");
            post_request_pars.Add("request_pars", request_pars);

            return post_request_pars;
        }

        #endregion


        #region 二维地面操作1 -- 改变地形高程、查询指定点高程
        //改变多边形区域内地形高程 -- 增加或减少一定值(value为负即为减)
        public void EditMesh_Addvalue(List<PointXY> polygon_points, double change_value)
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Addvalue(ref hydromodel, polygon_points, change_value);
        }

        //改变多边形区域内地形高程 -- 设为常量值
        public void EditMesh_Setvalue(List<PointXY> polygon_points, double const_value)
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Setvalue(ref hydromodel, polygon_points, const_value);
        }

        //改变多边形区域内地形高程 -- 重新根据DEM散点插值
        public void EditMesh_Interpolate_InPolygon(List<PointXY> polygon_points)
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Interpolate_InPolygon(ref hydromodel, polygon_points);
        }

        //重新内插所有mesh节点高程值
        public void EditMesh_Interpolate_Allnodes()
        {
            HydroModel hydromodel = this;
            Mesh.EditMesh_Interpolate_Allnodes(ref hydromodel);
        }

        //从文件中读取多边形控制点数据   ** 静态方法 **
        public static List<PointXY> ReadXYFile(string sourcefile)
        {
            return Mesh.ReadXYFile(sourcefile);
        }

        //从文件中读取DEM高程散点数据   ** 静态方法 **
        public static List<PointXYZ> ReadXYZFile(string sourcefile)
        {
            return Mesh.ReadXYZFile(sourcefile);
        }

        //获取指定点地面高程 -- 从mesh文件中
        public double Get_Point_Z(PointXY point)
        {
            HydroModel hydromodel = this;
            return Mesh.Get_Point_Z(hydromodel, point);
        }
        #endregion


        #region 二维地面操作2 -- 重新生成mesh以替换原mesh、新增加mesh与原mesh结合
        //根据前端给定的边界，创建新mesh -- 原来的mesh将被替换
        public void Create_New_MdfMesh(List<List<PointXY>> boundaryPoint)
        {
            HydroModel hydromodel = this;

            Mdf.Create_New_MdfMesh(ref hydromodel, boundaryPoint);
        }

        //根据前端给定的边界，创建新mesh并与原mesh合并 -- 原来的mesh范围将增大
        public void Create_NewMesh_AndMerge(List<List<PointXY>> boundaryPoint)
        {
            HydroModel hydromodel = this;

            Mdf.Create_NewMesh_AndMerge(ref hydromodel, boundaryPoint);
        }

        #endregion


        #region 耦合连接操作 -- 新增侧向耦合连接、侧向建筑物耦合连接(**缺少普通侧向建筑物的耦合连接**)
        //新增侧向连接 -- 给定连接河段和河道哪一侧，可以两侧都选
        public void Add_Lateral_Link(Reach_Segment reachsegment, ReachLR reachlr)
        {
            HydroModel hydromodel = this;
            Couple.Add_Lateral_Link(ref hydromodel, reachsegment, reachlr);
        }

        //新增侧向连接 -- 给定两点和河道哪一侧，可以两侧都选
        public void Add_Lateral_Link(PointXY start_p, PointXY end_p, ReachLR reachlr)
        {
            HydroModel hydromodel = this;
            Couple.Add_Lateral_Link(ref hydromodel, start_p, end_p, reachlr);
        }

        //新增侧向建筑物(如分洪口)连接 -- 给定分洪口建筑物对象
        public void Add_SideStructure_Link(string sidestr_name)
        {
            HydroModel hydromodel = this;
            Controlstr str = this.Mike11Pars.ControlstrList.GetGate(sidestr_name);
            if (str == null) return;
            Couple.Add_SideStructure_Link(ref hydromodel, str as Fhkstr);
        }

        //去除指定河道的所有侧向连接
        public void Remove_ReachALL_LateralLink(string reachname)
        {
            CoupleLinkList couplelinklist = this.CoupleLinklist;
            couplelinklist.Remove_ReachALL_LateralLink(reachname);
        }

        //去除指定侧向建筑物连接
        public void Remove_Sidestr_Link(string sidestr_name)
        {
            CoupleLinkList couplelinklist = this.CoupleLinklist;
            Controlstr str = this.Mike11Pars.ControlstrList.GetGate(sidestr_name);
            if (str == null) return;
            couplelinklist.Remove_Sidestr_Link(str as Fhkstr);
        }
        #endregion
    }
}
