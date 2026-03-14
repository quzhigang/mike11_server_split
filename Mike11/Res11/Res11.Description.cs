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
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
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
        #region *********************** 语音播报结果 *************************
        //根据播放速度,生成语音(mp3)文件(在结果文件夹下)
        public static Dictionary<DateTime, string> Get_ResultSoundFiles(HydroModel hydromodel,
            Dictionary<string, Dictionary<DateTime, double>> tsz_discharge, Dictionary<string, Dictionary<DateTime, double>> tsz_accumulatedV,
            Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data)
        {
            Dictionary<DateTime, string> result_soundfile = new Dictionary<DateTime, string>();
            int steps_persecond = Model_Const.STEPS_PERSECOND;

            //创建存放mp3的文件夹(使用相对路径)
            string soundres_dir = Model_Files.Get_Modeldir(hydromodel.Modelname) + @"\" + "results" + @"\" + "sound";
            if (!Directory.Exists(soundres_dir))
            {
                Directory.CreateDirectory(soundres_dir);
            }

            //获取播报文字
            Dictionary<DateTime, string> sound_stringdic = Create_SoundString(hydromodel, tsz_discharge, tsz_accumulatedV, ZdmHD_Data);

            //手动修改播报文字
            Modify_SoundContent(soundres_dir, ref sound_stringdic);

            //根据播报速度选择播报文字,生成播报语音文件
            Dictionary<DateTime, string> mp3_filepath = new Dictionary<DateTime, string>();
            double play_speed = steps_persecond * (int)hydromodel.Mike11Pars.Mike11_savetimestep;
            int mp3filecount = 0;

            DateTime for_time = sound_stringdic.ElementAt(0).Key;
            for (int i = 0; i < sound_stringdic.Count; i++)
            {
                double total_minutes = i != 0 ? sound_stringdic.ElementAt(i).Key.Subtract(for_time).TotalMinutes : 10000; //两时间步之间的分钟数
                double simulate_time_minutes = i != 0 ? (sound_stringdic.ElementAt(i - 1).Value.Length / 6) * play_speed : 0;   //语音播报完需要分钟数
                if (total_minutes > simulate_time_minutes)
                {
                    mp3filecount++;

                    //新建mp3文件
                    string file_path = soundres_dir + "\\result" + mp3filecount + ".mp3";

                    //将文字转换为语音，写入语音文件
                    Speech speechobj = new Speech();
                    speechobj.Get_Sound_FromString(sound_stringdic.ElementAt(i).Value, file_path);

                    //添加到集合(采用相对路径)
                    result_soundfile.Add(sound_stringdic.ElementAt(i).Key, file_path.Substring(Model_Const.Get_Sysdir().Length + 1));

                    for_time = sound_stringdic.ElementAt(i).Key;
                }
            }

            return result_soundfile;
        }

        //组织播报文字
        public static Dictionary<DateTime, string> Create_SoundString(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, double>> tsz_discharge,
             Dictionary<string, Dictionary<DateTime, double>> tsz_accumulatedV, Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data)
        {
            Dictionary<DateTime, string> must_content = new Dictionary<DateTime, string>();  //必须播报内容
            Dictionary<DateTime, string> generic_content = new Dictionary<DateTime, string>(); //一般播报内容
            if (hydromodel.Model_state != Model_State.Finished) return null;

            //开始模拟与结束模拟描述
            DateTime start_time = hydromodel.ModelGlobalPars.Simulate_time.Begin.AddHours(hydromodel.ModelGlobalPars.Ahead_hours); //去掉提前的小时
            DateTime end_time = hydromodel.ModelGlobalPars.Simulate_time.End;
            string start_simulatestr = SimulateTime.Time_ToSoundStr(start_time, true) + "模拟开始";
            string end_simulatestr = SimulateTime.Time_ToSoundStr(end_time, true) + "模拟结束";
            must_content.Add(start_time, start_simulatestr);
            must_content.Add(end_time, end_simulatestr);

            //闸门调度过程描述
            Dictionary<DateTime, string> str_combinedd_desc = Get_Combinestrdd_Desc(hydromodel);
            for (int i = 0; i < str_combinedd_desc.Count; i++)
            {
                if (!must_content.Keys.Contains(str_combinedd_desc.ElementAt(i).Key)) must_content.Add(str_combinedd_desc.ElementAt(i).Key, str_combinedd_desc.ElementAt(i).Value);
            }

            //漫堤风险提示信息(必须) 和 退水流量、总退水量过程信息(一般)
            Get_LevelTsz_Desc(hydromodel, tsz_discharge, tsz_accumulatedV, ZdmHD_Data, ref must_content, ref generic_content);

            //必须的和一般的排序
            Dictionary<DateTime, string> new_must_content = must_content.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
            Dictionary<DateTime, string> new_generic_content = generic_content.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);

            //必须的和一般的合并
            Dictionary<DateTime, string> combine_desc = Combine_DescItem(new_must_content, new_generic_content);

            //去掉不在模拟时间范围内的
            for (int i = 0; i < combine_desc.Count; i++)
            {
                if (combine_desc.ElementAt(i).Key.Subtract(start_time).TotalHours < 0 || combine_desc.ElementAt(i).Key.Subtract(end_time).TotalHours > 0)
                {
                    combine_desc.Remove(combine_desc.ElementAt(i).Key);
                    i--;
                }
            }

            return combine_desc;
        }

        //用于手动修改语音播报内容
        public static void Modify_SoundContent(string soundres_dir, ref Dictionary<DateTime, string> sound_stringdic)
        {
            //将播报内容写入文本
            string sound_filepath = soundres_dir + "\\sound_content.txt";
            string[] sound_content = sound_stringdic.Values.ToArray();
            for (int i = 0; i < sound_stringdic.Count; i++)
            {
                sound_content[i] = SimulateTime.TimetoStr(sound_stringdic.ElementAt(i).Key) + "$" + sound_content[i];
            }
            File.WriteAllLines(sound_filepath, sound_content);

            //修改文本内容后，重新读取
            Console.WriteLine("开始读取文件");
            string[] newsound_content = File.ReadAllLines(sound_filepath);
            Dictionary<DateTime, string> newsound_stringdic = new Dictionary<DateTime, string>();
            for (int i = 0; i < newsound_content.Length; i++)
            {
                DateTime nowtime = SimulateTime.StrToTime(newsound_content[i].Split(new char[] { '$' })[0].Trim());
                string nowcontent = newsound_content[i].Split(new char[] { '$' })[1];
                newsound_stringdic.Add(nowtime, nowcontent);
            }
            sound_stringdic = newsound_stringdic;
        }

        //整合建筑物调度过程
        private static Dictionary<DateTime, string> Get_Combinestrdd_Desc(HydroModel hydromodel)
        {
            Dictionary<DateTime, string> combinedd_content = new Dictionary<DateTime, string>();

            //闸门调度过程描述
            List<Dictionary<DateTime, string>> fskdd_content = Get_Strdd_ContentDic(hydromodel, GateType.LLZ);
            List<Dictionary<DateTime, string>> tszdd_content = Get_Strdd_ContentDic(hydromodel, GateType.PBZ);
            List<Dictionary<DateTime, string>> jzzdd_content = Get_Strdd_ContentDic(hydromodel, GateType.YLZ);

            //整合调度(时间相同的整合成一个时间序列)
            List<DateTime> time_list = new List<DateTime>();
            for (int i = 0; i < fskdd_content.Count; i++)
            {
                for (int j = 0; j < fskdd_content[i].Count; j++)
                {
                    if (!time_list.Contains(fskdd_content[i].ElementAt(j).Key))
                    {
                        time_list.Add(fskdd_content[i].ElementAt(j).Key);
                    }
                }

            }

            for (int i = 0; i < tszdd_content.Count; i++)
            {
                for (int j = 0; j < tszdd_content[i].Count; j++)
                {
                    if (!time_list.Contains(tszdd_content[i].ElementAt(j).Key))
                    {
                        time_list.Add(tszdd_content[i].ElementAt(j).Key);
                    }
                }
            }

            for (int i = 0; i < jzzdd_content.Count; i++)
            {
                for (int j = 0; j < jzzdd_content[i].Count; j++)
                {
                    if (!time_list.Contains(jzzdd_content[i].ElementAt(j).Key))
                    {
                        time_list.Add(jzzdd_content[i].ElementAt(j).Key);
                    }
                }
            }
            time_list.Sort();

            //重新合成各时刻各类型各闸门调度
            for (int i = 0; i < time_list.Count; i++)
            {
                string combinedd_str = SimulateTime.Time_ToSoundStr(time_list[i]);
                //优先分水闸
                for (int j = 0; j < fskdd_content.Count; j++)
                {
                    for (int k = 0; k < fskdd_content[j].Count; k++)
                    {
                        if (fskdd_content[j].ElementAt(k).Key == time_list[i])
                        {
                            combinedd_str += fskdd_content[j].ElementAt(k).Value + ",";
                        }
                    }
                }

                //其次节制闸
                for (int j = 0; j < jzzdd_content.Count; j++)
                {
                    for (int k = 0; k < jzzdd_content[j].Count; k++)
                    {
                        if (jzzdd_content[j].ElementAt(k).Key == time_list[i])
                        {
                            combinedd_str += jzzdd_content[j].ElementAt(k).Value + ",";
                        }
                    }
                }

                //最后退水闸
                for (int j = 0; j < tszdd_content.Count; j++)
                {
                    for (int k = 0; k < tszdd_content[j].Count; k++)
                    {
                        if (tszdd_content[j].ElementAt(k).Key == time_list[i])
                        {
                            combinedd_str += tszdd_content[j].ElementAt(k).Value + ",";
                        }
                    }
                }

                combinedd_content.Add(time_list[i], combinedd_str.Substring(0, combinedd_str.Length - 1));
            }

            return combinedd_content;
        }

        //漫堤风险提示信息(必须) 和 退水流量、退水量过程信息(一般)
        private static void Get_LevelTsz_Desc(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, double>> tsz_discharge, Dictionary<string, Dictionary<DateTime, double>> tsz_accumulatedV,
            Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data, ref Dictionary<DateTime, string> must_content, ref Dictionary<DateTime, string> generic_content)
        {
            //漫堤风险提示信息(必须)
            Get_Level_Desc(hydromodel, ZdmHD_Data, ref must_content);

            //获取退水流量、退水量过程信息(一般)
            Get_Tszq_Desc(hydromodel, tsz_discharge, tsz_accumulatedV, ref generic_content);
        }

        //获取漫堤风险提示信息(必须)
        private static void Get_Level_Desc(HydroModel hydromodel, Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data, ref Dictionary<DateTime, string> must_content)
        {
            DateTime time = DateTime.Now;

            //获取各个节制闸 闸前断面和其水位过程
            Dictionary<string, AtReach> jzz_dic = Item_Info.Get_TypeGate_FromModel(hydromodel, GateType.YLZ);
            string reach_name = jzz_dic.ElementAt(0).Value.reachname;

            //获取闸前最近的一个非建筑物所在的断面编号
            Dictionary<double, int> jzz_forsection = new Dictionary<double, int>();
            for (int i = 0; i < jzz_dic.Count; i++)
            {
                double jzzstr_chainage = jzz_dic.ElementAt(i).Value.chainage;

                Dictionary<double, double> value_dic = ZdmHD_Data.ElementAt(0).Value;
                int section_i = File_Common.Search_Value(value_dic, jzzstr_chainage, true);

                while (true)
                {
                    double chainage = value_dic.ElementAt(section_i).Key;
                    AtReach atreach = AtReach.Get_Atreach(jzz_dic.ElementAt(i).Value.reachname, chainage);
                    List<PointXZS> section_pointlist = hydromodel.Mike11Pars.SectionList.Get_NearSectiondata(atreach);
                    if (section_pointlist == null)
                    {
                        section_i--;
                        continue;
                    }

                    if (Math.Abs(section_pointlist[0].X - section_pointlist[1].X) > 0.5 || section_i <= 0) //垂直边坡
                    {
                        break;
                    }
                    section_i--;
                }

                jzz_forsection.Add(value_dic.ElementAt(section_i).Key, section_i);
            }

            //获取各节制闸前断面水位过程
            Dictionary<string, Dictionary<DateTime, double>> section_levellist = Get_Section_Level(jzz_dic, jzz_forsection, ZdmHD_Data);

            //遍历各节制闸前水位,找出闸前水位距堤顶高程小于0.5m的时刻
            Get_Level_Desc1(hydromodel, reach_name, section_levellist, jzz_forsection, ref must_content);

            //遍历各节制闸前水位,找出闸前水位超过堤顶高程的描述
            Get_Level_Desc2(hydromodel, reach_name, section_levellist, jzz_forsection, ref must_content);
        }

        //获取退水流量过程信息(一般)
        private static void Get_Tszq_Desc(HydroModel hydromodel, Dictionary<string, Dictionary<DateTime, double>> tsz_discharge, Dictionary<string, Dictionary<DateTime, double>> tsz_accumulatedV, ref Dictionary<DateTime, string> generic_content)
        {
            Dictionary<DateTime, double> tsz0_discharge = tsz_discharge.ElementAt(0).Value;
            for (int i = 0; i < tsz0_discharge.Count; i++)
            {
                DateTime now_time = tsz0_discharge.ElementAt(i).Key;

                //if (now_time.Minute != 0) continue;
                //if (now_time.Hour % 6 != 0) continue;
                if (now_time.Minute != 40) continue;

                string tszq_content = SimulateTime.Time_ToSoundStr(now_time);
                double sum_tsl = 0.0;
                for (int j = 0; j < tsz_discharge.Count; j++)
                {
                    if (tsz_discharge.ElementAt(j).Value[now_time] > 0.1)
                    {
                        //tszq_content += tsz_discharge.ElementAt(j).Key + "流量" + tsz_discharge.ElementAt(j).Value[now_time].ToString() + "方每秒,";
                        sum_tsl += tsz_accumulatedV.ElementAt(j).Value[now_time];
                    }
                }
                if (sum_tsl != 0.0)
                {
                    tszq_content += "总干渠累计退水量" + sum_tsl.ToString() + "万方";
                }

                if (!generic_content.Keys.Contains(now_time) && tszq_content != SimulateTime.Time_ToSoundStr(now_time)) generic_content.Add(now_time, tszq_content);
            }
            Console.WriteLine();
        }

        //获取各节制闸前断面水位过程
        private static Dictionary<string, Dictionary<DateTime, double>> Get_Section_Level(Dictionary<string, AtReach> jzz_dic,
            Dictionary<double, int> jzz_forsection, Dictionary<DateTime, Dictionary<double, double>> ZdmHD_Data)
        {
            Dictionary<string, Dictionary<DateTime, double>> section_levellist = new Dictionary<string, Dictionary<DateTime, double>>();
            for (int i = 0; i < jzz_dic.Count; i++)
            {
                string jzzstr_name = jzz_dic.ElementAt(i).Key;
                int n = jzz_forsection.ElementAt(i).Value;  //当前节制闸前断面所在的序号

                Dictionary<DateTime, double> value_dic = new Dictionary<DateTime, double>();
                for (int j = 0; j < ZdmHD_Data.Count; j++)
                {
                    Dictionary<double, double> allsection_dic = ZdmHD_Data.ElementAt(j).Value;
                    DateTime now_time = ZdmHD_Data.ElementAt(j).Key;
                    double now_level = allsection_dic.ElementAt(n).Value;
                    value_dic.Add(now_time, now_level);
                }
                section_levellist.Add(jzzstr_name, value_dic);
            }
            return section_levellist;
        }

        //遍历各节制闸前水位,找出闸前水位距堤顶高程小于0.5m的描述
        private static void Get_Level_Desc1(HydroModel hydromodel, string reach_name, Dictionary<string, Dictionary<DateTime, double>> section_levellist,
                      Dictionary<double, int> jzz_forsection, ref Dictionary<DateTime, string> must_content)
        {
            //遍历各节制闸前水位,找出闸前水位距堤顶高程小于0.5m的描述
            for (int i = 0; i < section_levellist.Count; i++)
            {
                //堤顶高程
                AtReach atreach = AtReach.Get_Atreach(reach_name, jzz_forsection.ElementAt(i).Key);
                List<PointXZS> section_pointlist = hydromodel.Mike11Pars.SectionList.Get_NearSectiondata(atreach);
                Max_Min_Z maxminz = SectionList.Get_Max_MinZ(section_pointlist);
                double dd_level = maxminz.max_z;

                string jzzstr_name = section_levellist.ElementAt(i).Key;
                Dictionary<DateTime, double> str_leveldic = section_levellist.ElementAt(i).Value;

                if (dd_level - str_leveldic.Values.Max() > 0.5) continue;
                for (int j = 0; j < str_leveldic.Count; j++)
                {
                    DateTime now_time = str_leveldic.ElementAt(j).Key;
                    if (dd_level - section_levellist.ElementAt(i).Value[now_time] <= 0.5)
                    {
                        string level_desc = "警告，" + SimulateTime.Time_ToSoundStr(now_time) + jzzstr_name + "前水位过高,水位距堤顶小于0.5米";
                        if (!must_content.Keys.Contains(now_time)) must_content.Add(now_time, level_desc);
                        break;
                    }
                }
            }
        }

        //遍历各节制闸前水位,找出闸前水位超过堤顶高程的描述
        private static void Get_Level_Desc2(HydroModel hydromodel, string reach_name, Dictionary<string, Dictionary<DateTime, double>> section_levellist,
            Dictionary<double, int> jzz_forsection, ref Dictionary<DateTime, string> must_content)
        {
            //遍历各节制闸前水位,找出闸前水位超过堤顶高程的描述
            for (int i = 0; i < section_levellist.Count; i++)
            {
                //堤顶高程
                AtReach atreach = AtReach.Get_Atreach(reach_name, jzz_forsection.ElementAt(i).Key);
                List<PointXZS> section_pointlist = hydromodel.Mike11Pars.SectionList.Get_NearSectiondata(atreach);
                Max_Min_Z maxminz = SectionList.Get_Max_MinZ(section_pointlist);
                double dd_level = maxminz.max_z;

                string jzzstr_name = section_levellist.ElementAt(i).Key;
                Dictionary<DateTime, double> str_leveldic = section_levellist.ElementAt(i).Value;

                if (dd_level - str_leveldic.Values.Max() > 0) continue;
                for (int j = 0; j < str_leveldic.Count; j++)
                {
                    DateTime now_time = str_leveldic.ElementAt(j).Key;
                    if (dd_level - section_levellist.ElementAt(i).Value[now_time] <= 0)
                    {
                        string level_desc = "警告，" + SimulateTime.Time_ToSoundStr(now_time) + jzzstr_name + "前水位过高，水位已漫过堤顶";
                        if (!must_content.Keys.Contains(now_time)) must_content.Add(now_time, level_desc);
                        break;
                    }
                }
            }
        }


        //整合多个时间序列(按先后顺序，参数越在前面的越优先)
        public static Dictionary<DateTime, string> Combine_DescItem(Dictionary<DateTime, string> new_must_content,
            Dictionary<DateTime, string> new_generic_content)
        {
            Dictionary<DateTime, string> combine_content = new Dictionary<DateTime, string>();

            //重新合成各时刻各类型各闸门调度
            combine_content.Add(new_must_content.ElementAt(0).Key, new_must_content.ElementAt(0).Value);
            for (int i = 1; i < new_must_content.Count; i++)
            {
                DateTime time1 = new_must_content.ElementAt(i - 1).Key;
                DateTime time2 = new_must_content.ElementAt(i).Key;

                //看一般的是否能插入
                for (int j = 0; j < new_generic_content.Count; j++)
                {
                    DateTime generic_time = new_generic_content.ElementAt(j).Key;
                    bool timeis_enough = TimeYN(new_must_content, new_generic_content, generic_time, time1, time2);
                    if (timeis_enough)
                    {
                        combine_content.Add(new_generic_content.ElementAt(j).Key, new_generic_content.ElementAt(j).Value);
                    }
                }
                combine_content.Add(new_must_content.ElementAt(i).Key, new_must_content.ElementAt(i).Value);
            }

            return combine_content;
        }

        //判断时间是否足够，如果足够则插入该一般内容
        public static bool TimeYN(Dictionary<DateTime, string> new_must_content, Dictionary<DateTime, string> new_generic_content,
            DateTime generic_time, DateTime time1, DateTime time2)
        {
            string str1 = new_must_content[time1];
            string str2 = new_generic_content[generic_time];

            //读完上一句必须内容时间充裕度判断
            double play_speed = Model_Const.STEPS_PERSECOND * (int)Model_Const.MIKE11_SAVESTEPTIME;
            double total_minutes = generic_time.Subtract(time1).TotalMinutes; //两时间步之间的分钟数(模拟时间)
            double simulate_time_minutes = (str1.Length / 4.5) * play_speed; //语音播报完需要分钟数

            //读完当前一般内容时间充裕度判断
            double total_minutes1 = time2.Subtract(generic_time).TotalMinutes; //两时间步之间的分钟数(模拟时间)
            double simulate_time_minutes1 = (str2.Length / 4.5) * play_speed; //语音播报完需要分钟数

            if (total_minutes > simulate_time_minutes && total_minutes1 > simulate_time_minutes1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //获取指定种类建筑物调度过程描述集合
        private static List<Dictionary<DateTime, string>> Get_Strdd_ContentDic(HydroModel hydromodel, GateType zgq_gatetype)
        {
            List<Dictionary<DateTime, string>> strdd_contentlist = new List<Dictionary<DateTime, string>>();

            //获取各建筑物调度信息
            List<string[]> strdd_info = new List<string[]>(); // ****修改*** ZGQ_hnq.Get_Gatedd_Info(hydromodel, zgq_gatetype);

            for (int i = 0; i < strdd_info.Count; i++)
            {
                Dictionary<DateTime, string> strdd_content = new Dictionary<DateTime, string>();
                if (strdd_info[i][2] == "指令调度")
                {
                    //单个调度
                    string[] dd_strinfo = strdd_info[i][3].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < dd_strinfo.Length; j++)
                    {
                        string datestr = dd_strinfo[j].Trim().Substring(0, dd_strinfo[j].Trim().Length - 4);
                        string openorclose = dd_strinfo[j].Trim().Substring(dd_strinfo[j].Trim().Length - 4).Substring(0, 2);
                        DateTime dd_time = SimulateTime.StrToTime(datestr);
                        string str_content = openorclose + strdd_info[i][1];
                        strdd_content.Add(dd_time, str_content);
                    }
                    strdd_contentlist.Add(strdd_content);
                }
            }
            return strdd_contentlist;
        }

        //污染源位置过程 和 信息统计过程描述
        private static void Get_ADLocationStatistics_Desc(HydroModel hydromodel, Dictionary<DateTime, string[]> ADLocation_Info,
            ref Dictionary<DateTime, string> must_content, ref Dictionary<DateTime, string> generic_content)
        {
            for (int i = 0; i < ADLocation_Info.Count; i++)
            {
                DateTime now_time = ADLocation_Info.ElementAt(i).Key;

                //获取污染体位置过程描述(必须的到达 和 可有的污染期间整时的位置描述)
                Get_ADsource_LocationDesc(ADLocation_Info, i, now_time, ref must_content, ref generic_content);

                //获取污染体统计信息描述(可有的，且在分水闸、退水闸、节制闸均已到达的情况下)
                Get_ADsource_TotalDesc(ADLocation_Info, i, now_time, ref generic_content);
            }
        }

        //获取污染体位置过程描述
        private static void Get_ADsource_LocationDesc(Dictionary<DateTime, string[]> AD_Info, int i, DateTime now_time, ref Dictionary<DateTime, string> must_content, ref Dictionary<DateTime, string> generic_content)
        {
            //存在污染水体
            double ad_con = double.Parse(AD_Info.ElementAt(i).Value.Last());
            if (ad_con != 0.0)
            {
                //分水口 到达过程描述(必须)
                string fsk_locationinfo = AD_Info.ElementAt(i).Value[7];
                string fsk_cnname = fsk_locationinfo.Split(new char[] { ' ' }).First();
                string next_fskcnname = i < AD_Info.Count - 1 ? AD_Info.ElementAt(i + 1).Value[7].Split(new char[] { ' ' }).First() : "";
                if (fsk_cnname != "")
                {
                    if (fsk_cnname != next_fskcnname)
                    {
                        string fsk_arrive_content = SimulateTime.Time_ToSoundStr(now_time) + "污染水体抵达" + fsk_cnname;
                        if (!must_content.Keys.Contains(now_time)) must_content.Add(now_time, fsk_arrive_content);
                    }
                    else if (now_time.Minute == 0)
                    {
                        string fsk_location_content = SimulateTime.Time_ToSoundStr(now_time) + "污染水体距下一个分水口," + fsk_locationinfo;
                        if (AD_Info.ElementAt(i).Value[8] != "" && AD_Info.ElementAt(i).Value[8] != " " && AD_Info.ElementAt(i).Value[8] != null)
                            fsk_location_content += ",预计到达时间" + AD_Info.ElementAt(i).Value[8] + "小时";
                        generic_content.Add(now_time, fsk_location_content);
                    }
                }

                //退水闸 到达过程描述(必须)
                string tsz_locationinfo = AD_Info.ElementAt(i).Value[9];
                string tsz_cnname = tsz_locationinfo.Split(new char[] { ' ' }).First();
                string next_tszcnname = i < AD_Info.Count - 1 ? AD_Info.ElementAt(i + 1).Value[9].Split(new char[] { ' ' }).First() : "";
                if (tsz_cnname != "")
                {
                    if (tsz_cnname != next_tszcnname)
                    {
                        string tsz_arrive_content = SimulateTime.Time_ToSoundStr(now_time) + "污染水体抵达" + tsz_cnname;
                        if (!must_content.Keys.Contains(now_time)) must_content.Add(now_time, tsz_arrive_content);
                    }
                    else if (now_time.Minute == 0)
                    {
                        string tsz_location_content = SimulateTime.Time_ToSoundStr(now_time) + "污染水体距下一个退水闸," + tsz_locationinfo;
                        if (AD_Info.ElementAt(i).Value[10] != "" && AD_Info.ElementAt(i).Value[10] != " " && AD_Info.ElementAt(i).Value[10] != null)
                            tsz_location_content += ",预计到达时间" + AD_Info.ElementAt(i).Value[10] + "小时";
                        if (!generic_content.Keys.Contains(now_time)) generic_content.Add(now_time, tsz_location_content);
                    }
                }

                //节制闸 到达过程描述(必须)
                string jzz_locationinfo = AD_Info.ElementAt(i).Value[11];
                string jzz_cnname = jzz_locationinfo.Split(new char[] { ' ' }).First();
                string next_jzzcnname = i < AD_Info.Count - 1 ? AD_Info.ElementAt(i + 1).Value[11].Split(new char[] { ' ' }).First() : "";
                if (jzz_cnname != "")
                {
                    if (jzz_cnname != next_jzzcnname)
                    {
                        string jzz_arrive_content = SimulateTime.Time_ToSoundStr(now_time) + "污染水体抵达" + jzz_cnname;
                        if (!must_content.Keys.Contains(now_time)) must_content.Add(now_time, jzz_arrive_content);
                    }
                    else if (now_time.Minute == 0)
                    {
                        string jzz_location_content = SimulateTime.Time_ToSoundStr(now_time) + "污染水体距下一个节制闸," + jzz_locationinfo;
                        if (AD_Info.ElementAt(i).Value[12] != "" && AD_Info.ElementAt(i).Value[12] != " " && AD_Info.ElementAt(i).Value[12] != null)
                            jzz_location_content += ",预计到达时间" + AD_Info.ElementAt(i).Value[12] + "小时";
                        if (!generic_content.Keys.Contains(now_time)) generic_content.Add(now_time, jzz_location_content);
                    }
                }
            }


        }

        //获取污染体统计信息描述(可有的，且在分水闸、退水闸、节制闸均已到达的情况下)
        private static void Get_ADsource_TotalDesc(Dictionary<DateTime, string[]> AD_Info, int i, DateTime now_time, ref Dictionary<DateTime, string> generic_content)
        {
            string fsk_locationinfo = AD_Info.ElementAt(i).Value[7];
            string fsk_cnname = fsk_locationinfo.Split(new char[] { ' ' }).First();
            string tsz_locationinfo = AD_Info.ElementAt(i).Value[9];
            string tsz_cnname = tsz_locationinfo.Split(new char[] { ' ' }).First();
            string jzz_locationinfo = AD_Info.ElementAt(i).Value[11];
            string jzz_cnname = jzz_locationinfo.Split(new char[] { ' ' }).First();

            double ad_con = double.Parse(AD_Info.ElementAt(i).Value.Last());
            string ad_speedstr = AD_Info.ElementAt(i).Value[6];

            string ad_reachlengthstr = AD_Info.ElementAt(i).Value[16];
            string ad_volumestr = AD_Info.ElementAt(i).Value[17];
            if (ad_con != 0.0 && now_time.Minute == 20) // && fsk_cnname == "" && tsz_cnname == "" && jzz_cnname == "")
            {
                string str_qbj = double.Parse(ad_speedstr) != 0 ? "当前污染物前锋行径速度" + ad_speedstr + "米每秒," : "";
                string wrwtotal_content = SimulateTime.Time_ToSoundStr(now_time) + str_qbj + "总干渠被污染渠长" + ad_reachlengthstr + "公里";  //,总干渠污染水量" + ad_volumestr + "万方";
                generic_content.Add(now_time, wrwtotal_content);
            }
        }

        #endregion *****************************************************************************
    }
}
