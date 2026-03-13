using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using DHI.PFS;
using System.Collections;
using System.IO;

using bjd_model.Common;
using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike_Flood;
using bjd_model.Mike21;
using bjd_model;

namespace bjd_model.Mike11
{
    //组分信息结构体
    [Serializable]
    public struct Component
    {
        public string com_name;
        public int com_number;
        public int com_units;
        public int com_type;

        public static Component Get_Component(string com_name = "WRW", int comnumber = 1,int com_units = 6,int com_type = 0)
        {
            Component com;
            com.com_name = com_name;
            com.com_number = comnumber;
            com.com_units = com_units;
            com.com_type = com_type;
            return com;
        }
    }

    //扩散系数结构体
     [Serializable]
    public struct Dispersion
    {
        public double dispersion_a;
        public double dispersion_b;
        public double min_dispersion;
        public double max_dispersion;
        public static Dispersion Get_Dispersion(double dis_a = 10,double dis_b = 0.05,double min = 5.0,double max = 20.0)
        {
            Dispersion disper;
            disper.dispersion_a = dis_a;
            disper.dispersion_b = dis_b;
            disper.min_dispersion = min;
            disper.max_dispersion = max;
            return disper;
        }
    }

    //初始值结构体
    [Serializable]
     public struct Component_Init
     {
        public int com_number;
        public double com_con;
        public bool isglobal;
        public AtReach atreach;
        public static Component_Init Get_ComponentInit(int comnumber = 1,double com_con = 0.0,bool isglobal = true)
        {
            Component_Init component_init;
            component_init.com_number = comnumber;
            component_init.com_con = com_con;
            component_init.isglobal = isglobal;
            component_init.atreach = AtReach.Get_Atreach("", 0.0);
            return component_init;
        }
     }

    //衰减系数结构体
    [Serializable]
    public struct Decay
    {
        public int com_number;
        public double decay_const;
        public bool isglobal;
        public AtReach atreach;
        public static Decay Get_Decay(int comnumber = 1, double decay_const = 0.0, bool isglobal = true)
        {
            Decay decay;
            decay.com_number = comnumber;
            decay.decay_const = decay_const;
            decay.isglobal = isglobal;
            decay.atreach = AtReach.Get_Atreach("", 0.0);
            return decay;
        }
    }

    public class Ad11
    {
        #region ***************************从ad11文件中获取参数信息*********************************
        //从默认ad11参数文件中获取已有参数信息
        public static void GetDefault_Ad11Info(string sourcefilename, ref Ad11_ParametersList ad11_Pars)
        {
            //读取PFS文件
            PFSFile pfsfile = new PFSFile(sourcefilename);   //读取文件
            PFSSection target = pfsfile.GetTarget("MIKE0_AD", 1);   //最外面的节

            //获取组分
            PFSSection CompList = target.GetSection("CompList", 1);  //第2层节：各组分
            List<Component> com_list = new List<Component>();
            for (int i = 0; i < CompList.GetKeywordsCount(); i++)
            {
                int com_number = CompList.GetKeyword("DATA", i+1).GetParameter(1).ToInt();
                string com_name = CompList.GetKeyword("DATA", i+1).GetParameter(2).ToString();
                int com_unit = CompList.GetKeyword("DATA", i+1).GetParameter(3).ToInt();
                int com_type = CompList.GetKeyword("DATA", i+1).GetParameter(4).ToInt();
                Component com = Component.Get_Component(com_name, com_number, com_unit, com_type);
                com_list.Add(com);
            }
            ad11_Pars.Component_list = com_list;

            //获取全局扩散系数
            PFSSection Global_Variables = target.GetSection("Global_Variables", 1);  //第2层节：全局参数
            double dispersion_a = Global_Variables.GetKeyword("G_disp_factor", 1).GetParameter(1).ToDouble();
            double dispersion_b = Global_Variables.GetKeyword("G_exponent", 1).GetParameter(1).ToDouble();
            double min_dispersion = Global_Variables.GetKeyword("G_min_disp_coef", 1).GetParameter(1).ToDouble();
            double max_dispersion = Global_Variables.GetKeyword("G_max_disp_coef", 1).GetParameter(1).ToDouble();
            ad11_Pars.Dispersion = Dispersion.Get_Dispersion(dispersion_a, dispersion_b, min_dispersion, max_dispersion);

            //获取组分初始值
            PFSSection InitList = target.GetSection("InitList", 1);  //第2层节：各组分初始值
            List<Component_Init> Init_list = new List<Component_Init>();
            for (int i = 0; i < InitList.GetKeywordsCount(); i++)
            {
                int com_number = InitList.GetKeyword("DATA", i+1).GetParameter(1).ToInt();
                double initval = InitList.GetKeyword("DATA",  i+1 ).GetParameter(2).ToDouble();
                bool com_isglobal = InitList.GetKeyword("DATA", i+1).GetParameter(3).ToBoolean();

                AtReach atreach;
                atreach.reachname = InitList.GetKeyword("DATA", i+1).GetParameter(4).ToString();
                atreach.chainage = InitList.GetKeyword("DATA", i+1).GetParameter(5).ToDouble();
                atreach = AtReach.Get_Atreach(atreach.reachname, atreach.chainage);

                Component_Init com_init = Component_Init.Get_ComponentInit(com_number,initval,com_isglobal);
                com_init.atreach = atreach;

                Init_list.Add(com_init);
            }
            ad11_Pars.ComponetInit_list = Init_list;

            //获取组分衰减系数
            PFSSection DecayList = target.GetSection("DecayList", 1);  //第2层节：各组分衰减系数
            List<Decay> decay_list = new List<Decay>();
            for (int i = 0; i < InitList.GetKeywordsCount(); i++)
            {
                int com_number = DecayList.GetKeyword("DATA", i+1).GetParameter(1).ToInt();
                double decayval = DecayList.GetKeyword("DATA", i+1).GetParameter(2).ToDouble();
                bool com_isglobal = DecayList.GetKeyword("DATA", i+1).GetParameter(3).ToBoolean();

                AtReach atreach;
                atreach.reachname = DecayList.GetKeyword("DATA", i+1).GetParameter(4).ToString();
                atreach.chainage = DecayList.GetKeyword("DATA", i+1).GetParameter(5).ToDouble();
                atreach = AtReach.Get_Atreach(atreach.reachname, atreach.chainage);

                Decay com_decay = Decay.Get_Decay(com_number, decayval, com_isglobal);
                com_decay.atreach = atreach;

                decay_list.Add(com_decay);
            }
            ad11_Pars.Decay_list = decay_list;

            Console.WriteLine("ad11参数信息初始化成功!");
            pfsfile.Close();
        }
        #endregion *******************************************************************************************


        #region ************************************** 更新ad11文件 *****************************************
        // 提取最新的参数信息，更新ad11文件
        public static void Rewrite_Ad11_UpdateFile(HydroModel hydromodel)
        {
            //基础模型文件路径和当前模型文件路径
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Ad11_filename;
            string outputfilename = hydromodel.Modelfiles.Ad11_filename;
            if (!File.Exists(sourcefilename)) return;
            Ad11_ParametersList ad11pars = hydromodel.Mike11Pars.Ad_Pars;
            if (ad11pars == null) return;

            //打开PFS文件，开始编辑
            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection MIKE0_AD = pfsfile.GetTarget("MIKE0_AD", 1);   //目标

            //更新全局扩散系数变量节
            Update_GlobalVarSec(ad11pars, ref MIKE0_AD);
            Console.WriteLine("全局扩散系数更新成功!");

            //更新组分节
            Update_ComListSec(ad11pars, ref MIKE0_AD);
            Console.WriteLine("水质组分更新成功!");

            //更新组分初始条件节
            Update_InitListSec(ad11pars, ref MIKE0_AD);
            Console.WriteLine("水质初始条件更新成功!");

            //更新衰减系数节
            Update_DecaySec(ad11pars, ref MIKE0_AD);
            Console.WriteLine("衰减系数更新成功!");

            //重新生成ad11文件
            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("ad11一维水质参数文件更新成功!");
            Console.WriteLine("");
        }

        //更新全局变量节 - 修改全局糙率、输出txt文件路径
        public static void Update_GlobalVarSec(Ad11_ParametersList ad11pars, ref PFSSection MIKE0_AD)
        {
            PFSSection Global_Variables = MIKE0_AD.GetSection("Global_Variables", 1);  //第2层节：全局变量

            //修改全局扩散系数
            Global_Variables.GetKeyword("G_disp_factor", 1).DeleteParameter(1);
            Global_Variables.GetKeyword("G_disp_factor", 1).InsertNewParameterDouble(ad11pars.Dispersion.dispersion_a, 1);

            Global_Variables.GetKeyword("G_exponent", 1).DeleteParameter(1);
            Global_Variables.GetKeyword("G_exponent", 1).InsertNewParameterDouble(ad11pars.Dispersion.dispersion_b, 1);

            Global_Variables.GetKeyword("G_min_disp_coef", 1).DeleteParameter(1);
            Global_Variables.GetKeyword("G_min_disp_coef", 1).InsertNewParameterDouble(ad11pars.Dispersion.min_dispersion, 1);

            Global_Variables.GetKeyword("G_max_disp_coef", 1).DeleteParameter(1);
            Global_Variables.GetKeyword("G_max_disp_coef", 1).InsertNewParameterDouble(ad11pars.Dispersion.max_dispersion, 1);
        }

        //更新初始条件节
        public static void Update_InitListSec(Ad11_ParametersList ad11pars, ref PFSSection MIKE0_AD)
        {
            //清空原默认ad11文件中的参数集合
            MIKE0_AD.DeleteSection("InitList", 1);  //删除的节也是单独排
            PFSSection InitListSec = MIKE0_AD.InsertNewSection("InitList", 1);  //重新添加初始条件数组的节

            //重新逐个添加初始条件关键字
            List<Component_Init> Cominit_list = ad11pars.ComponetInit_list;
            if (Cominit_list == null) return;

            for (int i = 0; i < Cominit_list.Count; i++)
            {
                //逐个添加最新的组分初始条件
                PFSKeyword data1 = InitListSec.InsertNewKeyword("DATA", i);
                object[] data1_array = { Cominit_list[i].com_number, Cominit_list[i].com_con, Cominit_list[i].isglobal, Cominit_list[i].atreach.reachname, Cominit_list[i].atreach.chainage };
                Nwk11.InsertKeyPars(ref data1, data1_array);
            }
        }

        //更新水质组分节
        public static void Update_ComListSec(Ad11_ParametersList ad11pars, ref PFSSection MIKE0_AD)
        {
            //清空原默认ad11文件中的参数集合
            MIKE0_AD.DeleteSection("CompList", 1);  //删除的节也是单独排
            PFSSection Comsec = MIKE0_AD.InsertNewSection("CompList", 1);  //重新添加组分数组的节

            //重新逐个添加组分关键字
            List<Component> Com_list = ad11pars.Component_list;
            if (Com_list == null) return;

            for (int i = 0; i < Com_list.Count; i++)
            {
                //逐个添加最新的组分
                PFSKeyword data1 = Comsec.InsertNewKeyword("DATA", i);
                object[] data1_array = { Com_list[i].com_number, Com_list[i].com_name,Com_list[i].com_units,Com_list[i].com_type };
                Nwk11.InsertKeyPars(ref data1, data1_array);
            }
        }

        //更新衰减系数节
        public static void Update_DecaySec(Ad11_ParametersList ad11pars, ref PFSSection MIKE0_AD)
        {
            //清空原默认ad11文件中的参数集合
            MIKE0_AD.DeleteSection("DecayList", 1);  //删除的节也是单独排
            PFSSection DecayListSec = MIKE0_AD.InsertNewSection("DecayList", 1);  //重新添加衰减系数数组的节

            //重新逐个添加衰减系数关键字
            List<Decay> Decay_list = ad11pars.Decay_list;
            if (Decay_list == null) return;

            for (int i = 0; i < Decay_list.Count; i++)
            {
                //逐个添加最新的组分初始条件
                PFSKeyword data1 = DecayListSec.InsertNewKeyword("DATA", i);
                object[] data1_array = { Decay_list[i].com_number, Decay_list[i].decay_const, Decay_list[i].isglobal, Decay_list[i].atreach.reachname, Decay_list[i].atreach.chainage };
                Nwk11.InsertKeyPars(ref data1, data1_array);
            }
        }

        #endregion ********************************************************************************************


        #region ************************************* ad11参数修改操作 *****************************************
        // 添加新的水质组分（相当于增加了污染点源）
        public static void Add_Component(ref HydroModel hydromodel,string new_component)
        {
            Ad11_ParametersList ad11pars = hydromodel.Mike11Pars.Ad_Pars;

            //添加新的水质组分
            ad11pars.Component_list.Add(Component.Get_Component(new_component));
        }

        // 修改全局扩散系数 -- 用详细结构体
        public static void Modify_Dispersion(ref HydroModel hydromodel, Dispersion new_dispersion)
        {
            Ad11_ParametersList ad11pars = hydromodel.Mike11Pars.Ad_Pars;

            ad11pars.Dispersion = new_dispersion;
        }

        // 修改全局扩散系数 -- 用一个值
        public static void Modify_Dispersion(ref HydroModel hydromodel, double new_dispersion)
        {
            Ad11_ParametersList ad11pars = hydromodel.Mike11Pars.Ad_Pars;

            ad11pars.Dispersion = Dispersion.Get_Dispersion(new_dispersion,0,new_dispersion*0.5,new_dispersion*2);
        }

        // 修改所有组分的衰减系数
        public static void Modify_Decay(ref HydroModel hydromodel, double new_decay)
        {
            Ad11_ParametersList ad11pars = hydromodel.Mike11Pars.Ad_Pars;
            List<Decay> decay_list = ad11pars.Decay_list;

            for (int i = 0; i < decay_list.Count; i++)
            {
                decay_list[i] = Decay.Get_Decay(decay_list[i].com_number, new_decay, decay_list[i].isglobal);
            }

            ad11pars.Decay_list = decay_list;
        }
        #endregion ******************************************************************************************
    }


    //保存最新的ad11参数
    [Serializable]
    public class Ad11_ParametersList
    {
        //**********************属性************************
        //水质组分
        public List<Component> Component_list
        { get; set; }

        //全局扩散系数
        public Dispersion Dispersion 
        { get; set; }

        //各组分初始值
        public List<Component_Init> ComponetInit_list
        { get; set; }

        //各组分降解系数
        public List<Decay> Decay_list
        { get; set; }
    }

}