using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.PFS;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model;
using System.Globalization;
using System.Timers;

namespace bjd_model.Mike11
{
    //进度信息
    [Serializable]
    public struct ProgressInfo
    {
        public double max_progress_value;   //总值 -- 均采用100
        public double now_progress_value;  //当前值 

        public TimeSpan elapsed_time;      //已耗费时间 
        public TimeSpan remain_time;       //估计剩余时间

        public string progress_info;       //进度信息

        public static ProgressInfo Get_StartProgress_Info()
        {
            ProgressInfo progressinfo;
            progressinfo.max_progress_value = 100;
            progressinfo.now_progress_value = 0;
            progressinfo.elapsed_time = new TimeSpan(0, 0, 0);
            progressinfo.remain_time = new TimeSpan(0, 0, 0);
            progressinfo.progress_info = "开始模拟";

            return progressinfo;
        }

    }

    //简单进度信息
    [Serializable]
    public struct Progress_Time
    {
        public double total_second;   //需要的总秒
        public double remain_second;  //还剩下的秒数

        public static Progress_Time Get_Progress_Time(double total_second, double remain_second)
        {
            Progress_Time progressinfo;
            progressinfo.total_second = total_second;
            progressinfo.remain_second = remain_second;
            return progressinfo;
        }
    }

    //Mike常用计算引擎名称
    [Serializable]
    public enum EngineName
    {
        mike11,             //用于simulate一维模拟
        Application        //用于simulate一维模拟1D
    }

    // 五种模型选择情况
    [Serializable]
    public enum CalculateModel
    {
        only_rr,        //mike11 RR 只选择降雨产汇流模型，计算各流域出口来流流量过程
        only_hd,       //mike11 hd 只选择河道水动力学模型,上边界条件采用入流过程，模拟河道上的洪水演进过程

        rr_and_hd,      //mike11 hd + rr 进行一维水动力学和产汇流模拟
        ad_and_hd       //mike11 hd + ad 进行一维水动力学和水质模拟
    }


    //洪水模拟类型
    [Serializable]
    public enum SimulateFloodType
    {
        Design,               //设计洪水模拟 -- 固定的几个模拟过程，以2020.7.1为起点时间   
        History,              //历史洪水模拟 -- 给定的模拟时间首尾均在以前    
        Realtime_Forecast     //实时预报洪水模拟 -- 给定的模拟时间部分在以后或全部在以后
    }

    // 参数文件路径
    [Serializable]
    public struct Simulate_FilePaths
    {
        public string Network;
        public string Cross_section;
        public string Boundary;
        public string RR_Parameter;
        public string HD;
        public string AD;
    }

    // 时间步长,以秒计
    [Serializable]
    public enum SimTimeStep
    {
        step_60 = 60,
        step_30 = 30,
        step_10 = 10,
        step_2 = 2,
        step_1 = 1
    }

    //模型状态
    [Serializable]
    public enum Model_State
    {
        Finished =1,
        Error = 2,
        Ready_Calting = 3,
        Iscalting = 4
    }

    // 模型计算开始时间和结束时间
    [Serializable]
    public struct SimulateTime
    {
        public DateTime Begin;
        public DateTime End;
        public static DateTime Get_NowTime()
        {
            DateTime nowdatetime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            return nowdatetime;
        }

        //按固定的时间格式转换时间字符串成时间
        public static DateTime StrToTime(string timestr)
        {
            string[] time_strs = timestr.Split(new char[] { '/', ':', ' ', '_', '-' });
            if(time_strs.Length == 0) return new DateTime();
            if(time_strs.Length != 6) return new DateTime();
            if (time_strs[0].Length == 2) time_strs[0] = "20"+ time_strs[0];
            for (int i = 1; i < time_strs.Length; i++)
            {
                if(time_strs[i].Length == 1)
                {
                    time_strs[i] = "0" + time_strs[i];
                }
            }
            string timestr_new = time_strs[0] + "/" + time_strs[1] + "/" + time_strs[2] + " " +
                                 time_strs[3] + ":" + time_strs[4] + ":" + time_strs[5];

            DateTime datetime = DateTime.ParseExact(timestr_new, Model_Const.TIMEFORMAT, CultureInfo.CurrentCulture);
            return datetime;
        }

        //转为固定格式的时间字符串
        public static string TimetoStr(DateTime time)
        {
            return time.ToString(Model_Const.TIMEFORMAT);
        }

        //将时间转换为特定字符串，用于语音播报
        public static string Time_ToSoundStr(DateTime time,bool include_md = false)
        {
            string date_str = "";
            string time_str = "";

            //时间拼接
            if (time.Hour < 13)
            {
                if(time.Minute == 0)
                {
                     time_str += "上午" + time.Hour.ToString() + "点";
                }
                else
                {
                    time_str += "上午" + time.Hour.ToString() + "点" + time.Minute.ToString() + "分";
                }
            }
            else
            {
                if (time.Minute == 0)
                {
                    time_str += "下午" + (time.Hour - 12).ToString() + "点";
                }
                else
                {
                    time_str += "下午" + (time.Hour - 12).ToString() + "点" + time.Minute.ToString() + "分";
                }
            }

            //日期拼接
            if(include_md)
            {
                date_str += time.Month.ToString() + "月" + time.Day.ToString() + "日";
            }
            else
            {
                date_str += "";
            }

            return date_str + time_str + ",";
        }
    }

    // 结果保存时间步长，以分钟计
    [Serializable]
    public enum SaveTimeStep
    {
        step_1 = 1,
        step_2 = 2,
        step_5 = 5,
        step_10 = 10,
        step_20 = 20,
        step_30 = 30,
        step_60 = 60,
        step_120 = 120,
        step_200 = 200,
        step_300 = 300,
    }

    // 结果文件存储路径
    [Serializable]
    public struct ResultPath
    {
        public string HD;
        public string RR;
    }

    public class Sim11
    {
        #region **********************从simulate文件中获取信息****************************
        //simulate 相关参数初始化--从simulate文件中获取
        public static void GetDefault_M11SimulatePars(string sourcefilename, ref Model_GlobalPars GlobalPars, ref Mike11_Pars Mike11Pars)
        {
            //找到相应的节
            PFSFile sim11 = new PFSFile(sourcefilename, false);   //读取文件
            PFSSection target = sim11.GetTarget("Run11", 1);   //最外面的节
            PFSSection Models = target.GetSection("Models", 1);  //第2层节：模型列表
            PFSSection Simulation = target.GetSection("Simulation", 1); //模拟节

            //获取模型选择信息
            PFSKeyword model_hd = Models.GetKeyword("hd", 1);
            PFSKeyword model_ad = Models.GetKeyword("ad", 1);
            PFSKeyword model_rr = Models.GetKeyword("rr", 1);
            
            if (model_hd.GetParameter(1).ToBoolean() && model_rr.GetParameter(1).ToBoolean())
            {
                GlobalPars.Select_model = CalculateModel.rr_and_hd;
            }
            else if (model_hd.GetParameter(1).ToBoolean() && model_ad.GetParameter(1).ToBoolean())
            {
                GlobalPars.Select_model = CalculateModel.ad_and_hd;
            }
            else if (model_hd.GetParameter(1).ToBoolean())
            {
                GlobalPars.Select_model = CalculateModel.only_hd;
            }
            else
            {
                GlobalPars.Select_model = CalculateModel.only_rr;
            }

            //获取模型模拟时间和步长信息
            PFSSection Simulation_Period = Simulation.GetSection("Simulation_Period", 1);
            PFSKeyword start = Simulation_Period.GetKeyword("start", 1);
            PFSKeyword end = Simulation_Period.GetKeyword("end", 1);

            //模拟时间
            SimulateTime simulatetime;
            simulatetime.Begin = new DateTime(start.GetParameter(1).ToInt(), start.GetParameter(2).ToInt(), start.GetParameter(3).ToInt(),
                start.GetParameter(4).ToInt(), start.GetParameter(5).ToInt(), start.GetParameter(6).ToInt());
            simulatetime.End = new DateTime(end.GetParameter(1).ToInt(), end.GetParameter(2).ToInt(), end.GetParameter(3).ToInt(),
    end.GetParameter(4).ToInt(), end.GetParameter(5).ToInt(), end.GetParameter(6).ToInt());
            GlobalPars.Simulate_time = simulatetime;

            //初始条件
            PFSSection Initial_Conditions = Simulation.GetSection("Initial_Conditions", 1);
            PFSKeyword hd_key = Initial_Conditions.GetKeyword("hd", 1);
            if (hd_key.GetParameter(1).ToInt() == 1)
            {
                Mike11Pars.Mike11_HD_Hotstart = Hotstart.Get_Default_Hotstart();
            }
            else if (hd_key.GetParameter(1).ToInt() == 2)
            {
                string hotfile = hd_key.GetParameter(2).ToFileName().Substring(1);
                DateTime hot_time = new DateTime(hd_key.GetParameter(4).ToInt(), hd_key.GetParameter(5).ToInt(), hd_key.GetParameter(6).ToInt(),
                    hd_key.GetParameter(7).ToInt(), hd_key.GetParameter(8).ToInt(), hd_key.GetParameter(9).ToInt());
                Mike11Pars.Mike11_HD_Hotstart = Hotstart.Get_Hotstart(hotfile, hot_time);
            }

            PFSKeyword ad_key = Initial_Conditions.GetKeyword("ad", 1);
            if (ad_key.GetParameter(1).ToInt() == 0)
            {
                Mike11Pars.Mike11_AD_Hotstart = Hotstart.Get_Default_Hotstart();
            }
            else if (ad_key.GetParameter(1).ToInt() == 1)
            {
                string hotfile = ad_key.GetParameter(2).ToFileName().Substring(1);
                DateTime hot_time = new DateTime(ad_key.GetParameter(4).ToInt(), ad_key.GetParameter(5).ToInt(), ad_key.GetParameter(6).ToInt(),
                    ad_key.GetParameter(7).ToInt(), ad_key.GetParameter(8).ToInt(), ad_key.GetParameter(9).ToInt());
                Mike11Pars.Mike11_AD_Hotstart = Hotstart.Get_Hotstart(hotfile, hot_time);
            }

            //时间步长
            GlobalPars.Simulate_timestep = (SimTimeStep)Simulation_Period.GetKeyword("timestep", 1).GetParameter(1).ToInt();

            //一维保存时间步长
            PFSSection Results = target.GetSection("Results", 1);
            PFSKeyword resulthd = Results.GetKeyword("hd", 1);
            Mike11Pars.Mike11_savetimestep = (SaveTimeStep)(resulthd.GetParameter(3).ToInt());

            sim11.Close();
        }
        #endregion ***********************************************************************


        #region ************************* 更新simulate模拟文件*****************************
        // 根据模型选择，输入相应模型文件和参数，修改相关参数为最新设置值
        public static void RewriteSimulate_SelectModel(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Simulate_filename;
            string outputfilename = hydromodel.Modelfiles.Simulate_filename;

            //获取用户前端选择的计算模型
            CalculateModel model = hydromodel.ModelGlobalPars.Select_model;

            PFSFile sim11 = new PFSFile(sourcefilename, false);   //读取文件
            PFSSection target = sim11.GetTarget("Run11", 1);   //最外面的节
            PFSSection Models = target.GetSection("Models", 1);  //第2层节：模型列表

            PFSKeyword model_hd = Models.GetKeyword("hd", 1);
            PFSKeyword model_ad = Models.GetKeyword("ad", 1);
            PFSKeyword model_rr = Models.GetKeyword("rr", 1);

            PFSSection Input = target.GetSection("Input", 1);  //第2层节：参数文件输入

            //各输入文件关键字
            PFSKeyword inputnwk = Input.GetKeyword("nwk", 1);
            PFSKeyword inputxs = Input.GetKeyword("xs", 1);
            PFSKeyword inputbnd = Input.GetKeyword("bnd", 1);
            PFSKeyword inputrr = Input.GetKeyword("rr", 1);
            PFSKeyword inputhd = Input.GetKeyword("hd", 1);
            PFSKeyword inputad = Input.GetKeyword("ad", 1);

            //从全局变量中找到值，给各文件路径赋值
            Simulate_FilePaths input_filepath;
            input_filepath.Network = hydromodel.Modelfiles.Nwk11_filename;
            input_filepath.Cross_section = hydromodel.Modelfiles.Xns11_filename;
            input_filepath.Boundary = hydromodel.Modelfiles.Bnd11_filename;
            input_filepath.RR_Parameter = hydromodel.Modelfiles.Rr11_filename;
            input_filepath.HD = hydromodel.Modelfiles.Hd11_filename;
            input_filepath.AD = hydromodel.Modelfiles.Ad11_filename;

            //三种模型计算方式选择
            switch (model)
            {
                case CalculateModel.only_rr:  //单独产汇流
                    model_hd.GetParameter(1).ModifyBoolParameter(false);
                    model_rr.GetParameter(1).ModifyBoolParameter(true);
                    model_ad.GetParameter(1).ModifyBoolParameter(false);

                    //输入文件
                    inputnwk.GetParameter(1).ModifyFileNameParameter("");
                    inputxs.GetParameter(1).ModifyFileNameParameter("");
                    inputbnd.GetParameter(1).ModifyFileNameParameter("");
                    inputrr.GetParameter(1).ModifyFileNameParameter(input_filepath.RR_Parameter);
                    inputhd.GetParameter(1).ModifyFileNameParameter("");
                    break;
                case CalculateModel.only_hd:  //单独河道水动力学
                    model_hd.GetParameter(1).ModifyBoolParameter(true);
                    model_rr.GetParameter(1).ModifyBoolParameter(false);
                    model_ad.GetParameter(1).ModifyBoolParameter(false);

                    //输入文件
                    inputnwk.GetParameter(1).ModifyFileNameParameter(input_filepath.Network);
                    inputxs.GetParameter(1).ModifyFileNameParameter(input_filepath.Cross_section);
                    inputbnd.GetParameter(1).ModifyFileNameParameter(input_filepath.Boundary);
                    inputrr.GetParameter(1).ModifyFileNameParameter("");
                    inputhd.GetParameter(1).ModifyFileNameParameter(input_filepath.HD);
                    break;
                case CalculateModel.rr_and_hd:  //产汇流和河道水动力学耦合
                    model_hd.GetParameter(1).ModifyBoolParameter(true);
                    model_ad.GetParameter(1).ModifyBoolParameter(false);

                    //如果全部采用XAJ模型，则不能加载rr文件
                    if (hydromodel.RfPars.Catchmentlist.AllisXAJ() == true)  //全部为XAJ模型
                    {
                        model_rr.GetParameter(1).ModifyBoolParameter(false);
                    }
                    else
                    {
                        model_rr.GetParameter(1).ModifyBoolParameter(true);
                    }

                    //输入文件
                    inputnwk.GetParameter(1).ModifyFileNameParameter(input_filepath.Network);
                    inputxs.GetParameter(1).ModifyFileNameParameter(input_filepath.Cross_section);
                    inputbnd.GetParameter(1).ModifyFileNameParameter(input_filepath.Boundary);
                    inputrr.GetParameter(1).ModifyFileNameParameter(input_filepath.RR_Parameter);
                    inputhd.GetParameter(1).ModifyFileNameParameter(input_filepath.HD);
                    break;
                case CalculateModel.ad_and_hd:  //水动力学、水质耦合
                    model_hd.GetParameter(1).ModifyBoolParameter(true);
                    model_ad.GetParameter(1).ModifyBoolParameter(true);
                    model_rr.GetParameter(1).ModifyBoolParameter(false);

                    //输入文件
                    inputnwk.GetParameter(1).ModifyFileNameParameter(input_filepath.Network);
                    inputxs.GetParameter(1).ModifyFileNameParameter(input_filepath.Cross_section);
                    inputbnd.GetParameter(1).ModifyFileNameParameter(input_filepath.Boundary);
                    inputhd.GetParameter(1).ModifyFileNameParameter(input_filepath.HD);
                    inputad.GetParameter(1).ModifyFileNameParameter(input_filepath.AD);
                    break;
            }

            //模拟时间
            PFSSection Simulationsec = target.GetSection("Simulation", 1);
            PFSSection Simulation_Period = Simulationsec.GetSection("Simulation_Period", 1);  //第3层节：模拟时间设置
            SimulateTime begin_endDateTime = hydromodel.ModelGlobalPars.Simulate_time;

            //设置开始
            PFSKeyword start = Simulation_Period.GetKeyword("start", 1);  //修改开始时间
            start.GetParameter(1).ModifyIntParameter(begin_endDateTime.Begin.Year);
            start.GetParameter(2).ModifyIntParameter(begin_endDateTime.Begin.Month);
            start.GetParameter(3).ModifyIntParameter(begin_endDateTime.Begin.Day);
            start.GetParameter(4).ModifyIntParameter(begin_endDateTime.Begin.Hour);
            start.GetParameter(5).ModifyIntParameter(begin_endDateTime.Begin.Minute);
            start.GetParameter(6).ModifyIntParameter(begin_endDateTime.Begin.Second);

            //设置结束时刻
            PFSKeyword end = Simulation_Period.GetKeyword("end", 1);
            end.GetParameter(1).ModifyIntParameter(begin_endDateTime.End.Year);
            end.GetParameter(2).ModifyIntParameter(begin_endDateTime.End.Month);
            end.GetParameter(3).ModifyIntParameter(begin_endDateTime.End.Day);
            end.GetParameter(4).ModifyIntParameter(begin_endDateTime.End.Hour);
            end.GetParameter(5).ModifyIntParameter(begin_endDateTime.End.Minute);
            end.GetParameter(6).ModifyIntParameter(begin_endDateTime.End.Second);

            //设置计算步长
            PFSKeyword timestep = Simulation_Period.GetKeyword("timestep", 1);
            timestep.GetParameter(1).ModifyIntParameter((int)hydromodel.ModelGlobalPars.Simulate_timestep);

            //初始条件修改
            PFSSection initialsec = Simulationsec.GetSection("Initial_Conditions", 1);

            //水动力学初始条件修改
            string basemodel_dir = Path.GetDirectoryName(hydromodel.BaseModel.Modelfiles.Simulate_filename);
            string base_hot_hdfile = basemodel_dir + hydromodel.BaseModel.Mike11Pars.Mike11_HD_Hotstart.Hotstart_file;
            if (hydromodel.Mike11Pars.Mike11_HD_Hotstart.Use_Hotstart && File.Exists(base_hot_hdfile) )
            {
                string hotfile = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename) + 
                    hydromodel.Mike11Pars.Mike11_HD_Hotstart.Hotstart_file;
                DateTime date_time = hydromodel.Mike11Pars.Mike11_HD_Hotstart.Hotstart_datetime;

                initialsec.GetKeyword("hd").GetParameter(1).ModifyIntParameter(2);
                initialsec.GetKeyword("hd").GetParameter(2).ModifyFileNameParameter(hotfile);

                initialsec.GetKeyword("hd").GetParameter(4).ModifyIntParameter(date_time.Year);
                initialsec.GetKeyword("hd").GetParameter(5).ModifyIntParameter(date_time.Month);
                initialsec.GetKeyword("hd").GetParameter(6).ModifyIntParameter(date_time.Day);

                initialsec.GetKeyword("hd").GetParameter(7).ModifyIntParameter(date_time.Hour);
                initialsec.GetKeyword("hd").GetParameter(8).ModifyIntParameter(date_time.Minute);
                initialsec.GetKeyword("hd").GetParameter(9).ModifyIntParameter(date_time.Second);

                //如果本模型热启动文件不存在 而基础模型热启动文件存在，则拷贝过来
                if (!File.Exists(hotfile) )
                {
                    File.Copy(base_hot_hdfile, hotfile);
                }
            }
            else
            {
                initialsec.GetKeyword("hd").GetParameter(1).ModifyIntParameter(1);
            }

            //水质初始条件修改
            string base_hot_adfile = basemodel_dir + hydromodel.BaseModel.Mike11Pars.Mike11_AD_Hotstart.Hotstart_file;
            if (hydromodel.Mike11Pars.Mike11_AD_Hotstart.Use_Hotstart && File.Exists(base_hot_adfile) )
            {
                string hotfile = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename) + 
                    hydromodel.Mike11Pars.Mike11_AD_Hotstart.Hotstart_file;
                DateTime date_time = hydromodel.Mike11Pars.Mike11_AD_Hotstart.Hotstart_datetime;

                initialsec.GetKeyword("ad").GetParameter(1).ModifyIntParameter(1);
                initialsec.GetKeyword("ad").GetParameter(2).ModifyFileNameParameter(hotfile);

                initialsec.GetKeyword("ad").GetParameter(4).ModifyIntParameter(date_time.Year);
                initialsec.GetKeyword("ad").GetParameter(5).ModifyIntParameter(date_time.Month);
                initialsec.GetKeyword("ad").GetParameter(6).ModifyIntParameter(date_time.Day);

                initialsec.GetKeyword("ad").GetParameter(7).ModifyIntParameter(date_time.Hour);
                initialsec.GetKeyword("ad").GetParameter(8).ModifyIntParameter(date_time.Minute);
                initialsec.GetKeyword("ad").GetParameter(9).ModifyIntParameter(date_time.Second);

                //如果本模型热启动文件不存在 而基础模型热启动文件存在，则拷贝过来
                if (!File.Exists(hotfile))
                {
                    File.Copy(base_hot_adfile, hotfile);
                }
            }
            else
            {
                initialsec.GetKeyword("ad").GetParameter(1).ModifyIntParameter(0);
            }

            initialsec.GetKeyword(3).GetParameter(1).ModifyIntParameter(0);
            initialsec.GetKeyword(4).GetParameter(1).ModifyIntParameter(0);

            //修改结果保存文件为最新指定的路径
            PFSSection Results = target.GetSection("Results", 1);
            PFSKeyword resulthd = Results.GetKeyword("hd", 1);
            PFSKeyword resultad = Results.GetKeyword("ad", 1);

            resulthd.GetParameter(1).ModifyFileNameParameter(hydromodel.Modelfiles.Hdres11_filename);  //(Model_GlobalVar.now_hdres11_filename);
            resulthd.GetParameter(3).ModifyIntParameter((int)hydromodel.Mike11Pars.Mike11_savetimestep);

            PFSKeyword resultrr = Results.GetKeyword("rr", 1);
            resultrr.GetParameter(1).ModifyFileNameParameter(hydromodel.Modelfiles.Rrres11_filename);
            resultrr.GetParameter(3).ModifyIntParameter((int)hydromodel.Mike11Pars.Mike11_savetimestep);

            //保存时间步长(默认单位分钟)
            resulthd.GetParameter(3).ModifyIntParameter((int)hydromodel.Mike11Pars.Mike11_savetimestep);
            resultad.GetParameter(3).ModifyIntParameter((int)hydromodel.Mike11Pars.Mike11_savetimestep);
            resultrr.GetParameter(3).ModifyIntParameter((int)hydromodel.Mike11Pars.Mike11_savetimestep);

            sim11.Write(outputfilename);
            Console.WriteLine("Sim11一维模拟文件更新成功！");
        }
        #endregion ***********************************************************************


        #region ***************************** 修改相关参数 ********************************
        // 修改计算时间步长
        public static void Modify_SimulationTimeStep(ref HydroModel hydromodel, SimTimeStep new_simtimestep)
        {
            Model_GlobalPars globalpars = hydromodel.ModelGlobalPars;
            globalpars.Simulate_timestep = new_simtimestep;
        }

        // 修改模拟起止时间
        public static void Modify_BeginEndTime(ref HydroModel hydromodel, SimulateTime new_simstartendtime)
        {
            Model_GlobalPars globalpars = hydromodel.ModelGlobalPars;
            globalpars.Simulate_time = new_simstartendtime;
        }

        // 修改存储时间步长
        public static void Modify_SaveTimeStep(ref HydroModel hydromodel, SaveTimeStep new_savetimestep)
        {
            Mike11_Pars mike11pars = hydromodel.Mike11Pars;
            mike11pars.Mike11_savetimestep = new_savetimestep;
        }
        #endregion ***********************************************************************


        #region *************** 模拟过程中，通过查询结果文件大小反馈模拟进度信息 ****************
        //将模拟进度信息写入文本
        public static void Write_ProgressInfo(HydroModel hydromodel)
        {
            TimeSpan simulatespan = hydromodel.ModelGlobalPars.Simulate_time.End.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin);

            //获取合适的进度判定结果文件
            string res_file;
            if (hydromodel.ModelGlobalPars.Select_model == CalculateModel.only_rr)  //产汇流计算
            {
                res_file = null;
                return;
            }
            else        //其他均为hd河道结果
            {
                res_file = hydromodel.Modelfiles.Hdres11_filename;
            }

            //从指定结果中推求计算进度信息,将进度信息写入进度文本
            for (int i = 0; i < 200; i++)
            {
                if (File.Exists(res_file))
                {
                    Write_ProgressInfo_Totxt(hydromodel, res_file);
                    return;
                }
                Thread.Sleep(100);
            }

            //等待20秒后还找不到指定文件
            Console.WriteLine("结果文件不存在!");
            return;
        }

        //系统的发现窗体方法引用,user32.dll为windows的API
        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Ansi)]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public extern static int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpText, int nCount);

        private const int WM_Close = 0x0010;
        private const int WM_SETFOCUS = 0x0007;

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private static void Key_Down_Up1(byte vk, int count)
        {
            for (int i = 0; i < count; i++)
            {
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                keybd_event(vk, 0, 2, UIntPtr.Zero);
            }
        }

        //从结果文件中推求计算进度信息,将进度信息写入进度文本
        public static void Write_ProgressInfo_Totxt(HydroModel hydromodel, string sourcefilename)
        {
            ProgressInfo progress = ProgressInfo.Get_StartProgress_Info();
            TimeSpan simulatespan = hydromodel.ModelGlobalPars.Simulate_time.End.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin);

            //获取保存步数量 和 单步保存大小
            int savestep_count;
            int single_stepb;
            Get_SingleStepb(hydromodel, out single_stepb, out savestep_count);

            //推求引擎名
            string engine_name = Get_EngineName(hydromodel);

            //部分值进行定义
            progress.now_progress_value = 0;
            progress.max_progress_value = 100;

            //用于保存文件大小和时间的集合
            Dictionary<DateTime, long> resfile_info = new Dictionary<DateTime, long>();

            //首次打开结果文件，获取结果文件大小信息
            FileInfo fileinfo;
            fileinfo = new FileInfo(sourcefilename);
            DateTime last_datetime = DateTime.Now;
            long last_fileb = fileinfo.Length;
            double now_step = 0.0;
            double last_step = 0.0;

            //创建进度文本
            Model_Const.write_read_semaphore.WaitOne();  //占用一个信号量
            string process_filename = hydromodel.Modelfiles.Progressinfo_filename;

            //先写一次
            string[] processinfo_array_initial = new string[]{progress.max_progress_value.ToString(),progress.now_progress_value.ToString(),
                                                                   progress.elapsed_time.ToString(),progress.remain_time.ToString(),progress.progress_info};
            File.WriteAllLines(process_filename, processinfo_array_initial);
            Thread.Sleep(200);  //必须等到，否则引擎进程尚未开始
            Model_Const.write_read_semaphore.Release();  //释放一个信号量

            //无限循环，直到连续5次结果文件大小不变，然后读取结果文件，看时间序列最后一个时刻
            double last_value = -1;
            while (true)
            {
                //搜索可能弹出的mike警告对话框，并回复
                IntPtr find_mikew = FindWindow(null, "AD Courant number warning");
                if (find_mikew != IntPtr.Zero) //Mdf.Key_Down_Up1(78, 1); //N键
                {
                    //向窗体发送消息，设置窗体为焦点后，按N键取消
                    SendMessage(find_mikew, WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
                    Key_Down_Up1(78, 1);
                }

                //再判断一次
                if (!File.Exists(sourcefilename))
                {
                    Thread.Sleep(200);
                    continue;
                }

                //循环获取结果文件大小
                fileinfo = new FileInfo(sourcefilename);
                long now_fileb = fileinfo.Length;
                if (now_fileb != last_fileb)
                {
                    //将文件时间-大小记录加入集合
                    resfile_info.Add(DateTime.Now, now_fileb);
                    int n = resfile_info.Count;

                    //归100化当前进度值
                    long now_addfileb = now_fileb - last_fileb;  //当前步增加的文件大小
                    double now_addstep;  //当前增加的计算步数
                    if (now_addfileb > 0)
                    {
                        now_addstep = now_addfileb / (single_stepb * 1.0);  //每步保存的数据量
                    }
                    else
                    {
                        now_addstep = 0.0;
                    }
                    now_step = last_step + now_addstep;  //当前已保存计算步数
                    progress.now_progress_value = Math.Round((now_step / savestep_count) * 100.0);

                    //Console.WriteLine("上次文件大小{0}b  本次大小{1}b  增加{2}b 单次大小{3}b 上次步数{4:0.00} 本次增加步数{5:0.00} 本次步数{6:0.00} 总步数{7:0.00} 进度值{8}",
                    //     last_fileb, now_fileb, now_fileb - last_fileb, single_stepb, last_step, now_addstep, now_step, savestep_count, progress.now_progress_value);

                    if (progress.now_progress_value > 100)
                    {
                        progress.now_progress_value = 100;
                    }

                    //已耗费时间
                    TimeSpan elapsed_time = resfile_info.Keys.ElementAt(n - 1).Subtract(resfile_info.Keys.ElementAt(0));
                    progress.elapsed_time = new TimeSpan(0, 0, (int)elapsed_time.TotalSeconds);

                    //估计剩余时间
                    double remain_seconds = progress.elapsed_time.TotalSeconds * ((100 - progress.now_progress_value) / (progress.now_progress_value + 0.001));
                    if (remain_seconds > 360000)
                    {
                        remain_seconds = 360000;
                    }
                    progress.remain_time = new TimeSpan(0, 0, (int)remain_seconds);

                    //模拟进度信息
                    DateTime now_simulate_datetime = hydromodel.ModelGlobalPars.Simulate_time.Begin.AddHours(simulatespan.TotalHours * progress.now_progress_value / progress.max_progress_value);
                    progress.progress_info = "时间:" + now_simulate_datetime.ToString();

                    //向进度文本写入数据
                    if (progress.now_progress_value != last_value)
                    {
                        string[] processinfo_array = new string[]{progress.max_progress_value.ToString(),progress.now_progress_value.ToString(),
                                                                   progress.elapsed_time.ToString(),progress.remain_time.ToString(),progress.progress_info};

                        Model_Const.write_read_semaphore.WaitOne();  //占用一个信号量
                        File.WriteAllLines(process_filename, processinfo_array);
                        Model_Const.write_read_semaphore.Release();  //释放一个信号量
                    }
                    last_value = progress.now_progress_value;
                }
                last_fileb = now_fileb;
                last_step = now_step;

                //如果模拟引擎进程退出了，则写好结果后循环退出
                Process[] pros = Process.GetProcessesByName(engine_name);
                if (pros.Length == 0)
                {
                    DateTime last_time = Res11.Get_LastResTime(hydromodel.Modelname);
                    TimeSpan last_timespan = last_time.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin);
                    progress.now_progress_value = Math.Round((last_timespan.TotalSeconds/ simulatespan.TotalSeconds) * 100.0);
                    progress.progress_info = "时间:" + last_time.ToString();
                    progress.remain_time = new TimeSpan(0, 0, 0);
                    string[] processinfo_array = new string[]{progress.max_progress_value.ToString(),progress.now_progress_value.ToString(),
                                                                   progress.elapsed_time.ToString(),progress.remain_time.ToString(),progress.progress_info};

                    Model_Const.write_read_semaphore.WaitOne();  //占用一个信号量
                    File.WriteAllLines(process_filename, processinfo_array);
                    Model_Const.write_read_semaphore.Release();  //释放一个信号量
                    break;
                }
                Thread.Sleep(200);
            }
        }

        //获取总保存步数和单步保存大小
        public static void Get_SingleStepb(HydroModel hydromodel, out int single_stepb, out int savestep_count)
        {
            ProgressInfo progress = ProgressInfo.Get_StartProgress_Info();

            savestep_count = 1;
            single_stepb = 0;
            TimeSpan simulatespan = hydromodel.ModelGlobalPars.Simulate_time.End.Subtract(hydromodel.ModelGlobalPars.Simulate_time.Begin);

            //m11、m11+rr
            {
                //总保存步数
                savestep_count = (int)(simulatespan.TotalMinutes / (double)hydromodel.Mike11Pars.Mike11_savetimestep);

                //获取总计算点数量(包括流量计算点和水位计算点)、总保存大小
                Dictionary<string, List<double>> Reach_gridchainagelist = hydromodel.Mike11Pars.ReachList.Reach_gridchainagelist;
                int gridcount_level = 0;
                for (int i = 0; i < Reach_gridchainagelist.Count; i++)
                {
                    gridcount_level += Reach_gridchainagelist.Values.ElementAt(i).Count;
                }
                int gridcount = gridcount_level + gridcount_level - 1 - hydromodel.Mike11Pars.ControlstrList.GateListInfo.Count;
                single_stepb = gridcount * 4 ;  //单步保存字节
            }
        }

        //推求引擎名
        public static string Get_EngineName(HydroModel hydromodel)
        {
            string engine_name;
            engine_name = "DHI.Mike1D.Application"; // EngineName.mike11.ToString();   //单独非XAJ产汇流、一维河道以及两者耦合
            return engine_name;
        }

        //读取模拟进度信息 
        public static ProgressInfo Get_NowProgress_Realtime(HydroModel hydromodel)
        {
            ProgressInfo progress = ProgressInfo.Get_StartProgress_Info();
            Thread.Sleep(100);

            if (hydromodel.ModelGlobalPars.Select_model == CalculateModel.only_rr &&
                hydromodel.RfPars.Catchmentlist.Catchment_infolist[0].Now_RfmodelType == RFModelType.XAJ)
            {
                //从结果文件夹中获取所有的文件目录
                string reult_dir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename) + @"\" + "results";
                string[] filename_array = Directory.GetFiles(reult_dir, "*.dfs0");

                //根据dfs0文件数量推求进度
                int catchment_count = hydromodel.RfPars.Catchmentlist.Catchment_infolist.Count;
                progress.now_progress_value = Math.Round(filename_array.Length * 1.0 / catchment_count, 0);
                progress.progress_info = "已完成" + filename_array.Length + "个流域的产汇流模拟";
                progress.remain_time = new TimeSpan(0, 0, 0);
                progress.elapsed_time = new TimeSpan(0, 0, 1);
            }
            else
            {
                //从进度文件中读取进度信息
                string progress_filename = hydromodel.Modelfiles.Progressinfo_filename;
                progress = Get_NowProgress_FromTxt(progress_filename, progress);
            }

            return progress;
        }

        //从进度文件中读取进度信息
        public static ProgressInfo Get_NowProgress_FromTxt(string progress_filename, ProgressInfo progress)
        {
            //从进度文件中读取进度信息
            for (int i = 0; i < 50; i++)
            {
                if (File.Exists(progress_filename) == true)
                {
                    Model_Const.write_read_semaphore.WaitOne();  //占用一个信号量
                    string[] str_row = File.ReadAllLines(progress_filename);
                    Thread.Sleep(100);
                    Model_Const.write_read_semaphore.Release();  //释放一个信号量

                    if (str_row.Length == 5)
                    {
                        progress.max_progress_value = int.Parse(str_row[0]);
                        progress.now_progress_value = double.Parse(str_row[1]);
                        progress.elapsed_time = TimeSpan.Parse(str_row[2]);
                        progress.remain_time = TimeSpan.Parse(str_row[3]);
                        progress.progress_info = str_row[4];
                    }

                    return progress;
                }
                Thread.Sleep(200);
            }

            //等待10秒后还找不到指定文件
            Console.WriteLine("结果文件不存在!");
            return progress;
        }

        //显示模拟进度
        public static void Show_Simulate_ProgressInfo(HydroModel hydromodel)
        {
            int result_filecount = 0;
            while (true)
            {
                ProgressInfo progress = hydromodel.GetProgress();
                int nowpro = -1;
                if (((int)progress.now_progress_value) != nowpro)
                {
                    Console.WriteLine("总进度：{0}  当前进度：{1}  已耗费时间：{2}  估计剩余时间：{3} 进度信息：{4}",
                        progress.max_progress_value, progress.now_progress_value, progress.elapsed_time, progress.remain_time, progress.progress_info);
                }
                nowpro = (int)progress.now_progress_value;
                Thread.Sleep(100);

                //如果模拟引擎退出了，则循环退出
                string engine_name;
                engine_name = EngineName.mike11.ToString();   //单独非XAJ产汇流、一维河道以及两者耦合

                Process[] pros = Process.GetProcessesByName(engine_name);

                //模型结果文件数量
                string reult_dir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename) + @"\" + "results";
                int now_resultfile_count = Directory.GetFiles(reult_dir, "*.*").Length;

                //模拟引擎退出了，且结果文件数量不再变化了就退出
                if (pros.Length == 0 && now_resultfile_count == result_filecount)
                {
                    break;
                }
                result_filecount = Directory.GetFiles(reult_dir, "*.*").Length;
            }
        }

        #endregion *******************************************************************************

    }


    //定期执行方法的类
    public class Timer_Event
    {
        public static void Print()
        {
            string path = @"F:\test";
            File.Create(path + "\\" + SimulateTime.TimetoStr(DateTime.Now) + ".txt");
            Console.WriteLine("被执行一次，时间{0}",SimulateTime.TimetoStr(DateTime.Now));
        }

        public static void Event_Go()
        {
            // 添加定时器
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += OnTimedEvent;
            timer.Interval = GetIntervalToNextExecution();
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.WriteLine("等待定时器执行中...");
            Thread.Sleep(Timeout.Infinite);
        }

        //定点触发事件，执行方法
        public static void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("定时器被触发： " + e.SignalTime);

            // 判断是否在规定时间执行 Add 方法
            // 如果是，则开辟线程执行 Add 方法
            bool is_attime = false;
            DateTime time = e.SignalTime;
            //if ((time.Second == 0 || time.Hour == 20) && time.Minute == 0 && time.Second == 0) is_attime = true;
            if (time.Second == 0) is_attime = true;
            if (is_attime) Timer_Event.Print();

            // 计算下一次执行时间并更新定时器
            ((System.Timers.Timer)sender).Interval = GetIntervalToNextExecution();
        }

        // 计算下一次执行时间并更新定时器
        public static double GetIntervalToNextExecution()
        {
            DateTime now = DateTime.Now;
            DateTime next = now.Date.AddHours(now.Hour < 8 || (now.Hour == 8 && now.Minute == 0 && now.Second == 0) ? 8 : 20);

            if (now > next)
                next = next.AddDays(1);

            return (next - now).TotalMilliseconds;
        }
    }
}