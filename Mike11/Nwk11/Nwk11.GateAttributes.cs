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

using bjd_model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bjd_model.Mike11
{
    public partial class Nwk11
    {
        #region ****************************闸门操作 -- 改变建筑物位置、参数**************************************
        //根据前端给定的新点坐标 对闸门所在河道位置（河道、桩号）变化更新
        public static void Change_GateLocation(ref HydroModel hydromodel, string strname, PointXY newpoint)
        {
            //获取可控建筑物和河道集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //获取最近河道信息
            AtReach reach = Nwk11.Get_NearReach(hydromodel.Mike11Pars.ReachList, newpoint);
            controlstr.Reachinfo = reach;

            //如果新位置距离计算点太近，则修改建筑物的桩号
            List<double> chainagelist = reachlist.Get_ReachGridChainage(controlstr.Reachinfo.reachname);
            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref normalstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
            else
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref fhkstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
        }

        //根据前端给定的新点坐标 对闸门所在河道位置（河道、桩号）变化更新
        public static void Change_GateLocation(ref HydroModel hydromodel, string strname, AtReach atreach)
        {
            //获取可控建筑物和河道集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;
            ReachList reachlist = hydromodel.Mike11Pars.ReachList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //获取最近河道信息
            controlstr.Reachinfo = atreach;

            //如果新位置距离计算点太近，则修改建筑物的桩号
            List<double> chainagelist = reachlist.Get_ReachGridChainage(controlstr.Reachinfo.reachname);
            if (controlstr is Normalstr)
            {
                Normalstr normalstr = controlstr as Normalstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref normalstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
            else
            {
                Fhkstr fhkstr = controlstr as Fhkstr;
                bool Change = false;
                if (ChangeNearChainage(chainagelist, ref fhkstr) == 0)
                {
                    Change = true;   //桩号被改动过
                }
                Console.WriteLine("桩号是否已经被改变?: {0}", Change);
            }
        }

        //改变闸门基本属性
        public static void Change_GateAttributes(ref HydroModel hydromodel, string strname, Necessary_Attrs new_nes_attributes)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //根据5个必须参数创建闸门完整属性值
            Attributes newattributes = GetNew_Atts(new_nes_attributes);

            //更新闸门属性参数
            controlstr.Stratts = newattributes;
        }

        //根据闸门5个必须参数创建闸门属性值，其他参数取默认值
        public static Attributes GetNew_Atts(Necessary_Attrs neces_attrs)
        {
            //闸门基本属性更新
            Attributes newattribute;

            //需要修改的参数
            newattribute.gate_type = (int)neces_attrs.gate_type;    //闸门类型---目前只支持 弧形闸门、平板闸门 和 橡胶坝
            newattribute.gate_count = neces_attrs.gate_count;
            newattribute.gate_width = neces_attrs.gate_width;
            newattribute.sill_level = neces_attrs.sill_level;
            newattribute.max_value = neces_attrs.max_value;
            newattribute.gate_height = newattribute.max_value + 2;  //闸的最大高度(一般为堤顶高程)按闸门最大提升高度 +2m考虑

            //默认参数
            newattribute.under_flowcc = 0.63;
            newattribute.max_speed = Model_Const.FLATGATE_MAXSPEED;
            newattribute.bool_initial_value = true;
            newattribute.bool_max_value = true;
            newattribute.initial_value = newattribute.sill_level;

            return newattribute;
        }

        //改变弧形闸门参数
        public static void Change_GateRadiapars(ref HydroModel hydromodel, string strname, double radia_height)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //由闸底高程和弧形闸门高按规范推算弧形闸门的其他参数
            RadiaParams new_radiapars = GetNew_Radiaparams(controlstr.Stratts.sill_level, radia_height);

            //更新弧形闸门参数
            controlstr.Strradiapars = new_radiapars;
        }

        //由闸底高程和弧形闸门高按规范推算弧形闸门的其他参数
        public static RadiaParams GetNew_Radiaparams(double sill_level, double height)
        {
            //弧形闸门参数更新
            RadiaParams radiapars;

            //必须修改的参数
            radiapars.height = height;
            radiapars.radius = height * 1.2;
            radiapars.trunnion = height * 0.7;
            radiapars.tran_bottom = sill_level + radiapars.height / 3;

            //默认参数
            radiapars.tune_factor = 1;
            radiapars.weir_coeff = 1.838;
            radiapars.weri_exp = 1.5;
            radiapars.tran_depth = radiapars.height / 2;  //水深达到闸门高度一般时认为进入临界流

            return radiapars;
        }

        //改变公式闸门参数
        public static void Change_GateFormulapars(ref HydroModel hydromodel, string strname, double factor_a, double factor_b)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            //通过因子相应调整公式闸门参数
            FormulaParams newformulapas = GetNew_Formulaparams(factor_a, factor_b);

            //更新弧形闸门参数
            controlstr.Strformulapars = newformulapas;
        }

        //根据因子相应扩大和缩小 公式闸门各参数
        public static FormulaParams GetNew_Formulaparams(double factor_a, double factor_b)
        {
            //首先得到默认值
            FormulaParams newformulapas = Controlstr.Getdefault_FormulaParams();

            //乘系数因子a
            newformulapas.CF_coef_a *= factor_a;
            newformulapas.CS_coef_a *= factor_a;
            newformulapas.UF_coef_a *= factor_a;
            newformulapas.US_coef_a *= factor_a;

            //乘指数因子b
            newformulapas.CF_exp_b *= factor_b;
            newformulapas.CS_exp_b *= factor_b;
            newformulapas.US_exp_b *= factor_b;

            return newformulapas;
        }
        #endregion ************************************************************************************************
    }
}
