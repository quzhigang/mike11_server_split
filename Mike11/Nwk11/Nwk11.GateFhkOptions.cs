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
        #region **********************闸门操作 -- 改变分洪口建筑物的特定参数 **************************************
        //改变分洪口的分洪水位为堤顶高程
        public static void Change_FhkWaterLevel_ToMax(ref HydroModel hydromodel, string strname)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr is Normalstr)
            {
                Console.WriteLine("该建筑物不是分洪口！");
                return;
            }

            Fhkstr fhkstr = controlstr as Fhkstr;

            //分洪水位恢复到堤顶高程
            fhkstr.fhwaterlevel = fhkstr.Stratts.max_value;
        }

        //改变分洪口的分洪水位为指定水位
        public static void Change_FhkWaterLevel_ToValue(ref HydroModel hydromodel, string strname, double new_waterlevel)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr is Normalstr)
            {
                Console.WriteLine("该建筑物不是分洪口！");
                return;
            }

            Fhkstr fhkstr = controlstr as Fhkstr;

            //否则按左右岸堤顶平均值考虑
            fhkstr.fhwaterlevel = new_waterlevel;
        }

        //改变分洪持续时间为指定时间
        public static void Change_FhkBreakTime(ref HydroModel hydromodel, string strname, TimeSpan new_breaktime)
        {
            //获取可控建筑物集合对象
            ControlList controllist = hydromodel.Mike11Pars.ControlstrList;

            //获取可控建筑物
            Controlstr controlstr = controllist.GetGate(strname);

            if (controlstr is Normalstr)
            {
                Console.WriteLine("该建筑物不是分洪口！");
                return;
            }

            Fhkstr fhkstr = controlstr as Fhkstr;
            Attributes fhk_atts = fhkstr.Stratts;
            double breaktime_seconds = new_breaktime.TotalSeconds;
            fhk_atts.max_speed = Math.Round((fhk_atts.max_value - fhkstr.fhklevel) / breaktime_seconds, 3);    //溃决速率 m/s
        }
        #endregion ************************************************************************************************
    }
}
