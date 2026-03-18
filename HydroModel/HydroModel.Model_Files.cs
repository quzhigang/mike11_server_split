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
using bjd_model.Mike11;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;


namespace bjd_model
{
    //模型文件地址参数
    [Serializable]
    public class Model_Files
    {
        #region 构造函数
        public Model_Files()
        {
            SetDefault();
        }

        //根据方案名创建新文件夹和各文件地址
        public Model_Files(string modelname)
        {
            Create_ModelFilePath(modelname);
        }
        #endregion

        #region 属性
        //*********** Mike11各模型文件地址************//
        //mike11模型文件地址
        public string Simulate_filename { get; set; }
        public string Nwk11_filename { get; set; }
        public string Xns11_filename { get; set; }
        public string Bnd11_filename { get; set; }
        public string Rr11_filename { get; set; }
        public string Hd11_filename { get; set; }
        public string Ad11_filename { get; set; }

        //mike11和rr ad结果文件地址
        public string Hdres11_filename { get; set; }
        public string Hd_addres11_filename { get; set; }
        public string Rrres11_filename { get; set; }
        public string Adres11_filename { get; set; }
        public string Hdtxt_filename { get; set; }
        public string XAJres_filename { get; set; }

        //进度信息文件地址
        public string Progressinfo_filename { get; set; }
        //***********************************************//
        #endregion

        #region 方法
        //获取默认模型的模型文件地址类
        public static Model_Files Get_Modelfilepath(string model_dirname)
        {
            Model_Files modelfiles = new Model_Files();

            //先判断是否存在online_models\default\username的文件夹
            //Console.WriteLine(System.Environment.CurrentDirectory);
            if (!Directory.Exists(model_dirname))
            {
                Console.WriteLine("未找到默认模型文件夹!!");
                return null;
            }

            //搜索文件夹，逐个加载默认模型文件
            Get_ModelFile_FromDir(ref modelfiles, model_dirname);

            return modelfiles;
        }

        //搜索文件夹，从中获取默认模型各文件
        public static void Get_ModelFile_FromDir(ref Model_Files modelfiles, string dirname)
        {
            DirectoryInfo modeldirinfo = new DirectoryInfo(dirname);

            //先从文件中判断 默认模型是那种模型
            modelfiles.Simulate_filename = Directory.GetFiles(dirname, "*.sim11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.sim11")[0]) : "";
            modelfiles.Nwk11_filename = Directory.GetFiles(dirname, "*.nwk11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.nwk11")[0]) : "";
            modelfiles.Xns11_filename = Directory.GetFiles(dirname, "*.xns11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.xns11")[0]) : "";
            modelfiles.Bnd11_filename = Directory.GetFiles(dirname, "*.bnd11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.bnd11")[0]) : "";
            modelfiles.Rr11_filename = Directory.GetFiles(dirname, "*.rr11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.rr11")[0]) : "";
            modelfiles.Hd11_filename = Directory.GetFiles(dirname, "*.hd11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.hd11")[0]) : "";
            modelfiles.Ad11_filename = Directory.GetFiles(dirname, "*.ad11").Length != 0 ? modeldirinfo.FullName + @"\" + Path.GetFileName(Directory.GetFiles(dirname, "*.ad11")[0]) : "";

            //新建结果文件夹
            if (Directory.Exists(modeldirinfo.FullName + @"\" + "results")) Directory.Delete(modeldirinfo.FullName + @"\" + "results", true);
            DirectoryInfo modelresultdirinfo = Directory.CreateDirectory(modeldirinfo.FullName + @"\" + "results");
            modelfiles.Hdres11_filename = modelresultdirinfo.FullName + @"\hd_result.res1d";  //默认模型的这个结果文件被重写了，故没问题
            modelfiles.Hd_addres11_filename = modelresultdirinfo.FullName + @"\hd_result.ADDOUT.res1d";
            modelfiles.Adres11_filename = modelresultdirinfo.FullName + @"\ad_result.res1d";

            modelfiles.Progressinfo_filename = modelresultdirinfo.FullName + @"\progress.txt"; //这个也没问题
        }

        //设置模型属性 -- 全部为空
        public void SetDefault()
        {
            this.Simulate_filename = "";
            this.Nwk11_filename = "";
            this.Xns11_filename = "";
            this.Bnd11_filename = "";
            this.Rr11_filename = "";
            this.Hd11_filename = "";
            this.Ad11_filename = "";

            this.Hdres11_filename = "";
            this.Hd_addres11_filename = "";
            this.Rrres11_filename = "";
            this.Adres11_filename = "";
            this.XAJres_filename = "";
            this.Hdtxt_filename = "";
            this.Progressinfo_filename = "";
        }

        //设置默认属性 -- 根据模型名(模拟方案名)自动构建各模型文件所在路径
        public void Create_ModelFilePath(string modelname)
        {
            //先判断是否存在online_models的文件夹
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance;
            if (!Directory.Exists(dirname))
            {
                Directory.CreateDirectory(dirname);
            }

            //新建该模型方案的文件夹
            DirectoryInfo modeldirinfo = Directory.CreateDirectory(dirname + @"\" + modelname);

            this.Simulate_filename = modeldirinfo.FullName + @"\simulate11.sim11";
            this.Nwk11_filename = modeldirinfo.FullName + @"\hd.nwk11";
            this.Xns11_filename = modeldirinfo.FullName + @"\hddm.xns11";
            this.Bnd11_filename = modeldirinfo.FullName + @"\bjtj.bnd11";
            this.Rr11_filename = modeldirinfo.FullName + @"\rainfall.rr11";
            this.Hd11_filename = modeldirinfo.FullName + @"\cs.hd11";
            this.Ad11_filename = modeldirinfo.FullName + @"\cs.ad11";

            //新建结果文件夹
            DirectoryInfo modelresultdirinfo = Directory.CreateDirectory(modeldirinfo.FullName + @"\" + "results");
            this.Hdres11_filename = modelresultdirinfo.FullName + @"\hd_result.res1d";
            this.Hd_addres11_filename = modelresultdirinfo.FullName + @"\hd_result.ADDOUT.res1d";
           
            this.Rrres11_filename = modelresultdirinfo.FullName + @"\rr_result.res1d";
            this.Adres11_filename = modelresultdirinfo.FullName + @"\ad_result.res1d";
            this.XAJres_filename = modelresultdirinfo.FullName + @"\XAJ_result.dfs0";
            this.Hdtxt_filename = modelresultdirinfo.FullName + @"\jg.txt";

            this.Progressinfo_filename = modelresultdirinfo.FullName + @"\progress.txt";
        }

        //获取项目文件夹路径
        public static string Get_Itemdir()
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance;
            return dirname;
        }

        //获取项目文件夹路径
        public static string Get_Itemdir(string model_instance)
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance;
            return dirname;
        }

        //获取项目文件夹路径
        public static string Get_Item_DefaultModel_Dir(string model_instance)
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + Model_Const.DEFAULT_MODELNAME;
            return dirname;
        }

        //获取模型文件夹路径
        public static string Get_Modeldir(string plan_code)
        {
            string dirname = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance + @"\" + plan_code;
            return dirname;
        }

        //获取模型结果文件夹路径
        public static string Get_ModelResdir(string plan_code,string model_instance = "")
        {
            if(model_instance== "") model_instance = Mysql_GlobalVar.now_instance;
            string model_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + model_instance + @"\" + plan_code;
            string res_dir = File_Common.Get_SubDir(model_dir, ".res1d");
            if (res_dir == "") res_dir = File_Common.Get_SubDir(model_dir, ".res11");
            string result_direct = model_dir + @"\" + "results";
            if (res_dir == "")
            {
                if (Directory.Exists(result_direct)) res_dir = result_direct;
            }
            return res_dir;
        }
       
        #endregion

    }
}
