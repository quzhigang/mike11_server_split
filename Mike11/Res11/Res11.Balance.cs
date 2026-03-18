using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;
using System.Runtime.Serialization;
using System.IO;
using DHI.Mike1D.CrossSectionModule;
using System.Threading;
using System.Web.Script.Serialization;
using Kdbndp;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;

using bjd_model;
using System.Diagnostics;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json.Linq;


namespace bjd_model.Mike11
{
    public partial class Res11
    {
        #region ******************* 从结果html文件中获取统计水量 *******************
        //一维河道水量统计结果读取
        public static water_volume_balance Htmlreader(HydroModel hydromodel, Mike11_Mike21 mike11_mike21)
        {
            //先初始化
            water_volume_balance waterv = water_volume_balance.Get_Default_Res();

            //获取平衡文件
            string sourcefilename = null;
            sourcefilename = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename) + @"\" +
                                    Path.GetFileNameWithoutExtension(hydromodel.Modelfiles.Simulate_filename) + "VolumeBalance.HTML";
            if (!File.Exists(sourcefilename))
            {
                Console.WriteLine("未找到一维平衡结果文件！");
                return waterv;
            }

            //逐行读取文件，生成字符串数组，定义一些判断字符串
            waterv = Get_Balance_FromHtml(sourcefilename);

            return waterv;
        }

        //读取一维或二维HTML文件结果
        public static water_volume_balance Get_Balance_FromHtml(string sourcefilename)
        {
            water_volume_balance waterv = water_volume_balance.Get_Default_Res();

            //逐行读取文件，生成字符串数组，定义一些判断字符串
            string[] strarray = File.ReadAllLines(sourcefilename, Encoding.Default);
            string mike11 = "<H2> MIKE 11</H2>";
            string mike21fm = "<H2> MIKE 21 Flow Model FM</H2>";

            int initialn, finaln, totalinflown, totaloutflown;
            string initial_volumestr = "Initial volume in model area";
            string final_volumestr = "Final volume in model area";
            string total_inflowstr = "Total inflow";
            string total_outflowstr = "Total outflow";

            //判断是读取的哪种文件mike11 还是mike 21fm
            bool mike11file = false;
            bool mike21file = false;
            for (int i = 0; i < strarray.Length; i++)
            {
                if (strarray[i].Contains(mike11))
                {
                    mike11file = true;
                }
                else if (strarray[i].Contains(mike21fm))
                {
                    mike21file = true;
                }
            }

            //如果读取的是mike 11的平衡结果文件html
            if (mike11file == true)
            {
                //求各项数据所在的行数，即所在字符串数组的元素位置
                initialn = Getlinen(strarray, initial_volumestr);
                finaln = Getlinen(strarray, final_volumestr);
                totalinflown = Getlinen(strarray, total_inflowstr);
                totaloutflown = Getlinen(strarray, total_outflowstr);

                //求各项数据的具体值
                waterv.initial_volume = Getvalue(strarray[initialn]);
                waterv.final_volume = Getvalue(strarray[finaln]);
                waterv.total_inflow = Getvalue(strarray[totalinflown]);
                waterv.total_outflow = Getvalue(strarray[totaloutflown]);
                return waterv;
            }

            //如果读取的是mike 21fm的平衡结果文件html
            if (mike21file == true)
            {
                //求各项数据所在的行数，即所在字符串数组的元素位置
                initialn = Getlinen(strarray, initial_volumestr);
                finaln = Getlinen(strarray, final_volumestr);

                //求各项数据的具体值
                waterv.initial_volume = Getvalue(strarray[initialn]);
                waterv.final_volume = Getvalue(strarray[finaln]);
                waterv.total_inflow = Math.Max(waterv.final_volume - waterv.initial_volume, 0);
                waterv.total_outflow = Math.Max(waterv.initial_volume - waterv.final_volume, 0);
                return waterv;
            }
            return waterv;
        }

        //从字符串数组中找到包含指定字符串的元素.返回该字符串所在的元素索引
        public static int Getlinen(string[] strarray, string itemstr)
        {
            for (int i = 0; i < strarray.Length; i++)
            {
                if (strarray[i].Contains(itemstr))
                {
                    for (int j = i + 1; j < strarray.Length; j++)
                    {
                        if (strarray[j].Contains("<TH align=right>"))
                        {
                            return j;
                        }
                    }
                }
            }
            return 0;
        }

        //从字符串中找出唯一的数据，只适合包含唯一数据的字符串,且该数据前后为空格
        public static double Getvalue(string inputstr)
        {
            string[] str = inputstr.Split(new char[] { ' ' });
            for (int i = 0; i < str.Length; i++)
            {
                double value = 0;
                if (double.TryParse(str[i], out value) && value >= 0)
                {
                    return value;
                }
            }
            return -1;
        }
        #endregion ****************************************************************
    }
}
