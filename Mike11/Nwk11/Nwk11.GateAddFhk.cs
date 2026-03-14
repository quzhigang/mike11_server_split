using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using System.Globalization;
using System.Data;
using Kdbndp;
using System.IO;
using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace bjd_model.Mike11
{
    public partial class Nwk11
    {        
        #region **********************闸门操作 -- 添加分洪口可控建筑物***********************************
        //根据分洪口位置和宽度 构建默认规则的分洪口信息 -- 断面数据通过数据库获取
        public static FhkstrInfo Get_Default_Fhkstrinfo(HydroModel hydromodel, AtReach at_reach, PointXY p, double fhk_width,double fh_level =0, double break_second = 0)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            double now_protectheight_leveltodd = hydromodel.Mike11Pars.Protectheight_leveltodd;

            //调用方法求点在河道的左岸还是右岸
            ReachLR point_atreachLR = Get_PointAtReachLR(hydromodel.Mike11Pars.ReachList, p, at_reach);

            //定义分洪口信息
            FhkstrInfo fhkstrinfo;

            //分洪口名字
            int nowfhkcount = 0;
            for (int i = 0; i < controllist.NewAdd_GateList.Count; i++)
            {
                if (controllist.NewAdd_GateList[i] is Fhkstr) nowfhkcount++;
            }

            string fhkname = nowfhkcount == 0? "KB": "KB" + (nowfhkcount + 1).ToString(); // 统一按这个名字 + 序号
            fhkstrinfo.strname = at_reach.reachname + "_" + fhkname;    //建筑物名等于河道名 + 分洪口名
            fhkstrinfo.strddgz = CtrDdtype.GZDU;
            fhkstrinfo.atreach = at_reach;
            fhkstrinfo.width = fhk_width;

            //从断面参数数据中查询横断面的堤顶地面高程，溃堤水位设为堤顶高程，溃口底高程为地面高程
            ReachSection_Altitude reachsection_altitude = hydromodel.Get_Altitude(at_reach);

            //溃堤水位设为堤顶高程，溃口底高程为地面高程
            switch (point_atreachLR)
            {
                case ReachLR.left:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                case ReachLR.right:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.right_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.right_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.right_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.right_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                default:   //如果点刚好在河道中心线上，也按左岸考虑
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
            }
            if (fh_level != 0) fhkstrinfo.fh_waterlevel = fh_level;
            fhkstrinfo.break_seconds = break_second ==0? Model_Const.FHK_INSTANTANEOUSBREAK_TIME: break_second;  //按30s瞬溃考虑
            fhkstrinfo.time_filename = "";
            fhkstrinfo.time_itemname = "";

            Console.WriteLine("分洪口信息构建成功!");
            return fhkstrinfo;
        }

        //根据分洪口位置和宽度 构建默认规则的分洪口信息 -- 断面数据通过数据库获取
        public static FhkstrInfo Get_Default_Fhkstrinfo(HydroModel hydromodel, PointXY p, double fhk_width, double fh_level = 0, double break_second = 0)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            double now_protectheight_leveltodd = hydromodel.Mike11Pars.Protectheight_leveltodd;

            AtReach at_reach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //调用方法求点在河道的左岸还是右岸
            ReachLR point_atreachLR = Get_PointAtReachLR(hydromodel.Mike11Pars.ReachList, p, at_reach);

            //定义分洪口信息
            FhkstrInfo fhkstrinfo;

            //分洪口名字
            int nowfhkcount = 0;
            for (int i = 0; i < controllist.NewAdd_GateList.Count; i++)
            {
                if (controllist.NewAdd_GateList[i] is Fhkstr) nowfhkcount++;
            }

            string fhkname = nowfhkcount == 0 ? "KB" : "KB" + (nowfhkcount + 1).ToString(); // 统一按这个名字 + 序号
            fhkstrinfo.strname = at_reach.reachname + "_" + fhkname;    //建筑物名等于河道名 + 分洪口名
            fhkstrinfo.strddgz = CtrDdtype.GZDU;
            fhkstrinfo.atreach = at_reach;
            fhkstrinfo.width = fhk_width;

            //从断面参数数据中查询横断面的堤顶地面高程，溃堤水位设为堤顶高程，溃口底高程为地面高程
            ReachSection_Altitude reachsection_altitude = hydromodel.Get_Altitude(at_reach);

            //溃堤水位设为堤顶高程，溃口底高程为地面高程
            switch (point_atreachLR)
            {
                case ReachLR.left:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                case ReachLR.right:
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.right_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.right_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.right_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.right_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
                default:   //如果点刚好在河道中心线上，也按左岸考虑
                    fhkstrinfo.dm_level = Math.Round(reachsection_altitude.left_ground_altitude, 3);       //洪水口建筑物底高程 = 地面高程
                    fhkstrinfo.fhk_datumn = Math.Round(reachsection_altitude.left_ground_altitude, 3);        //洪水口分洪底高程 = 地面高程
                    fhkstrinfo.dd_level = Math.Round(reachsection_altitude.left_dd_altitude, 3);             //堤顶高程
                    fhkstrinfo.fh_waterlevel = Math.Round(reachsection_altitude.left_dd_altitude - now_protectheight_leveltodd, 3);   //开始分洪水位 = 堤顶高程 - 超高
                    break;
            }
            if (fh_level != 0) fhkstrinfo.fh_waterlevel = fh_level;
            fhkstrinfo.break_seconds = break_second == 0 ? Model_Const.FHK_INSTANTANEOUSBREAK_TIME : break_second;  //按30s瞬溃考虑
            fhkstrinfo.time_filename = "";
            fhkstrinfo.time_itemname = "";

            Console.WriteLine("分洪口信息构建成功!");
            return fhkstrinfo;
        }

        //在任意位置添加溃口可控建筑物(分洪口)
        public static string Add_Fhkstr(ref HydroModel hydromodel, ref FhkstrInfo fhkstrinfo)
        {
            //获取可控建筑物集合和河道集合
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //根据溃口建筑物信息新建可控建筑物对象
            Fhkstr fhkstr = new Fhkstr(fhkstrinfo);

            //先判断该桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
            List<double> chainagelist = reachlist.Get_ReachGridChainage(fhkstr.Reachinfo.reachname);

            //溃口的桩号可能会被修改
            bool Change = false;
            if (ChangeNearChainage(chainagelist, ref fhkstr) == 0)
            {
                Change = true;   //桩号被改动过
            }

            //全局建筑物集合中增加新的元素
            controllist.Addgate(fhkstr);

            //全局河道集合中，相应河道增加该建筑物桩号计算点
            reachlist.Add_ExistReach_GridChainage(fhkstr.Reachinfo.reachname, fhkstr.Reachinfo.chainage);

            return fhkstr.Strname;
        }

        //判断建筑物位置桩号是否离计算点桩号集合中的桩号太近，如果太近则移动,关联的参数类型必须相同，不能形参父类，实参子类
        public static int ChangeNearChainage(List<double> chainagelist, ref Fhkstr fhkstr)
        {
            //建筑物的桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (fhkstr.Reachinfo.chainage >= chainagelist[i] && fhkstr.Reachinfo.chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(fhkstr.Reachinfo.chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = fhkstr.Reachinfo.reachname;
                        reach.reachid = fhkstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        fhkstr.Reachinfo = reach;
                        return 0;
                    }
                    else if (Math.Abs(fhkstr.Reachinfo.chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = fhkstr.Reachinfo.reachname;
                        reach.reachid = fhkstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;

                        fhkstr.Reachinfo = reach;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        #endregion*********************************************************************************************
    }
}
