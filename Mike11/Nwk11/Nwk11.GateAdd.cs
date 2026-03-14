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
        #region ************************闸门操作 -- 添加普通可控建筑物*****************************************
        //新增 除泵站外 的普通可控建筑物 -- 除必要参数外，其他参数默认、规则调度(全开)
        public static string Add_Normalstr(ref HydroModel hydromodel, string gatename, PointXY p, Necessary_Attrs neces_attrs)
        {
            //调用方法求所在河道信息
            AtReach reachinfo = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //获取闸门所在位置河底高程，如果闸门底高程低于河底高程，则认为出错
            ReachSection_Altitude section_altitude = hydromodel.Get_Altitude(reachinfo);
            if (neces_attrs.sill_level < section_altitude.section_lowest)
            {
                Console.WriteLine("建筑物底高程低于河底高程，请检查断面数据和建筑物底高程数据！");
                return "";
            }

            //给新增的普通建筑物命名
            string strname = reachinfo.reachname + "_" + gatename;    //建筑物名等于河道名 + 闸门名

            //创建闸门新对象 
            Attributes gate_attrs = GetNew_Atts(neces_attrs);
            Normalstr normalstr = new Normalstr(strname, reachinfo, gate_attrs);   //根据 必须的5个参数构建普通建筑物

            //添加到集合里
            Add_Normalstr(ref hydromodel, ref normalstr);

            return strname;
        }

        //新增 泵站 普通可控建筑物 -- 除必要参数外，其他参数默认、规则调度(全开)
        public static string Add_Bumpstr(ref HydroModel hydromodel, string gatename, PointXY p, double design_discharge)
        {
            //调用方法求所在河道信息
            AtReach reachinfo = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, p);

            //给新增的普通建筑物命名
            string strname = reachinfo.reachname + "_" + gatename;    //建筑物名等于河道名 + 闸门名

            //创建闸门新对象 
            Normalstr normalstr = new Normalstr(strname, reachinfo, CtrDdtype.KXDU, design_discharge);

            //添加到集合里
            Add_Normalstr(ref hydromodel, ref normalstr);

            return strname;
        }

        //添加普通可控建筑物到建筑物集合，更新模型
        public static void Add_Normalstr(ref HydroModel hydromodel, ref Normalstr normalstr)
        {
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //先判断该桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
            List<double> chainagelist = reachlist.Get_ReachGridChainage(normalstr.Reachinfo.reachname);

            //建筑物的桩号可能会被修改
            bool Change = false;
            if (ChangeNearChainage(chainagelist, ref normalstr) == 0)
            {
                Change = true;   //桩号被改动过
            }

            //全局建筑物集合中增加新的元素
            controllist.Addgate(normalstr);

            //全局河道集合中，相应河道增加该建筑物桩号计算点
            reachlist.Add_ExistReach_GridChainage(normalstr.Reachinfo.reachname, normalstr.Reachinfo.chainage);
        }

        //判断建筑物位置桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
        public static int ChangeNearChainage(List<double> chainagelist, ref Normalstr normalstr)
        {
            //建筑物的桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (normalstr.Reachinfo.chainage >= chainagelist[i] && normalstr.Reachinfo.chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(normalstr.Reachinfo.chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = normalstr.Reachinfo.reachname;
                        reach.reachid = normalstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        normalstr.Reachinfo = reach;
                        return 0;
                    }
                    else if (Math.Abs(normalstr.Reachinfo.chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = normalstr.Reachinfo.reachname;
                        reach.reachid = normalstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;

                        normalstr.Reachinfo = reach;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        //判断建筑物位置桩号是否离计算点桩号集合中的桩号太近，如果太近则移动
        public static int ChangeNearChainage(List<double> chainagelist, ref Controlstr controlstr)
        {
            //建筑物的桩号可能会被修改
            for (int i = 0; i < chainagelist.Count - 1; i++)
            {
                if (controlstr.Reachinfo.chainage >= chainagelist[i] && controlstr.Reachinfo.chainage <= chainagelist[i + 1])
                {
                    if (Math.Abs(controlstr.Reachinfo.chainage - chainagelist[i]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = controlstr.Reachinfo.reachname;
                        reach.reachid = controlstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i] + Model_Const.MIKE11_MINDX;

                        controlstr.Reachinfo = reach;
                        return 0;
                    }
                    else if (Math.Abs(controlstr.Reachinfo.chainage - chainagelist[i + 1]) < Model_Const.MIKE11_MINDX)
                    {
                        AtReach reach;
                        reach.reachname = controlstr.Reachinfo.reachname;
                        reach.reachid = controlstr.Reachinfo.reachid;
                        reach.chainage = chainagelist[i + 1] - Model_Const.MIKE11_MINDX;

                        controlstr.Reachinfo = reach;
                        return 0;
                    }
                    return -1;
                }
            }
            return -1;
        }

        #endregion *********************************************************************************************
    }
}
