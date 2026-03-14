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
        //加载模型 -- 根据模型名从数据库中获取(静态类)(可用模型名或方案名)
        private static HydroModel Load_Model(string plan_code)
        {
            //产汇流的一些静态属性要重新初始化
            //if (CatchmentList.Stand24_Rfmodel == null )
            //{
            //    RR11.GetDefault_AverageRF();             //流域各月平均降雨
            //    RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
            //    RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
            //}

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            
            //先判断该模型在数据库中是否存在
            string sql = "select model_object from " + tableName + " where plan_code = '" + plan_code + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", plan_code);
                return null;
            }

            //流域信息最后从数据库中读取，并给模型流域参数赋值
            byte[] blob = Mysql.Get_Blob(sql);
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            HydroModel model = binFormat.Deserialize(ms) as HydroModel;
            ms.Close();
            ms.Dispose();

            //产汇流的一些静态属性要重新初始化
            if (model.RfPars.Catchmentlist.Catchment_infolist != null &&
                CatchmentList.Stand24_Rfmodel == null &&
                (model.ModelGlobalPars.Select_model == CalculateModel.only_rr ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_and_hd ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood))
            {
                RR11.GetDefault_AverageRF();             //流域各月平均降雨
                RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
                RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
            }

            //如果是一维模型且断面数为空，且非默认模型，则从全局中获取当前项目的断面
            if (model.ModelGlobalPars.Select_model.ToString().Contains("hd") && 
                model.Mike11Pars.SectionList == null && plan_code != Model_Const.DEFAULT_MODELNAME)
            {
                SectionList nowitem_sectiondata = Item_Info.Get_Item_SectionDatas(Mysql_GlobalVar.now_instance);
                model.Mike11Pars.SectionList = nowitem_sectiondata;
            }

            return model;
        }


        //加载该用户的所有模型 -- (静态类)
        public static List<HydroModel> Load_AllModel()
        {
            List<HydroModel> model_list = new List<HydroModel>();

            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select * from " + tableName + " where model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("模型实例：{0}没有模型!", Mysql_GlobalVar.now_instance);
                return null;
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string plan_code = dt.Rows[i][2].ToString();
                sql = "select model_object from " + tableName + " where plan_code='" + plan_code + "' and user = '" + Mysql_GlobalVar.now_modeldb_user + "'";

                byte[] blob = Mysql.Get_Blob(sql);

                MemoryStream ms = new MemoryStream(blob);
                BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器

                ms.Position = 0;
                ms.Seek(0, SeekOrigin.Begin);

                HydroModel model = binFormat.Deserialize(ms) as HydroModel;

                ms.Close();
                ms.Dispose();

                model_list.Add(model);
            }

            
            return model_list;
        }

        //用户加载默认模型 -- 根据模型名从数据库中获取(静态类)
        public static HydroModel Get_DefaultModel_AndCopytoself_FromDB()
        {
            //定义连接的数据库和表
            string tableName = Mysql_GlobalVar.MODELPAR_TABLENAME;
            

            //先判断该模型在数据库中是否存在
            string sql = "select model_object from " + tableName + " where plan_code='" + Model_Const.DEFAULT_MODELNAME + "' and model_instance = '" + Mysql_GlobalVar.now_instance + "'";
            DataTable dt = Mysql.query(sql);
            int nLen = dt.Rows.Count;

            if (nLen == 0)
            {
                Console.WriteLine("数据库中未找到模型:{0}!", Model_Const.DEFAULT_MODELNAME);
                return null;
            }

            //流域信息最后从数据库中读取，并给模型流域参数赋值
            byte[] blob = Mysql.Get_Blob(sql);
            MemoryStream ms = new MemoryStream(blob);
            BinaryFormatter binFormat = new BinaryFormatter();//创建二进制序列化器
            ms.Position = 0;
            ms.Seek(0, SeekOrigin.Begin);
            HydroModel model = binFormat.Deserialize(ms) as HydroModel;
            ms.Close();
            ms.Dispose();

            //修改模型文件路径
            HydroModel.Modify_ModelFilePath(ref model, Mysql_GlobalVar.now_instance);

            //产汇流的一些静态属性要重新初始化
            if (model.RfPars.Catchmentlist.Catchment_infolist != null &&
                CatchmentList.Stand24_Rfmodel == null &&
                (model.ModelGlobalPars.Select_model == CalculateModel.only_rr ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_and_hd ||
                model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood))
            {
                RR11.GetDefault_AverageRF();             //流域各月平均降雨
                RR11.GetDefault_AverageEvp();           //流域各月平均蒸发
                RR11.GetDefault_Stand24_Rfmodeldic();  //流域标准24小时雨形模板
            }

            //上传模型
            model.Save();

            //将默认模型文件拷贝一份到现基础模型实例文件夹下
            string source_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Model_Const.DEFAULT_MODELNAME;
            string dest_dir = Model_Const.Get_Sysdir() + @"\" + Model_Const.MODEL_DIRNAME + @"\" + Mysql_GlobalVar.now_instance + @"\" + Model_Const.DEFAULT_MODELNAME;
            if (!Directory.Exists(dest_dir))
            {
                Directory.CreateDirectory(dest_dir);
                File_Common.Copy_Directory_Allfile(source_dir, dest_dir);
            }

            //更新默认模型的河网文件，只保留规则调度
            //Nwk11.Rewrite_Nkw11_UpdateFile(model);

            return model;
        }

        //从文件中获取默认模型后保存
        public static void Load_DefaultModel_ToDB()
        {
            //从默认模型文件中获取模型
            HydroModel model = HydroModel.Get_Default_Model();

            //设置当前项目的结果保存时间
            Set_NowItem_SaveMinutes(model);

            //保存至数据库
            HydroModel.Save_ModelObject(model);
        }

        public static void Set_NowItem_SaveMinutes(HydroModel default_model)
        {
            if (default_model.ModelGlobalPars.Select_model == CalculateModel.rr_hd_flood)
            {
                //藕合模型也用二维模型
                Model_Const.Now_Model_SaveTime = (int)default_model.Mike21Pars.Mike21_savetimestepbs * (int)default_model.ModelGlobalPars.Simulate_timestep;
            }
            else if (default_model.ModelGlobalPars.Select_model == CalculateModel.only_m21 || default_model.ModelGlobalPars.Select_model == CalculateModel.ad_hd_m21)
            {
                //二维模型
                Model_Const.Now_Model_SaveTime = (int)default_model.Mike21Pars.Mike21_savetimestepbs * (int)default_model.ModelGlobalPars.Simulate_timestep;
            }
            else if (default_model.ModelGlobalPars.Select_model == CalculateModel.only_hd || default_model.ModelGlobalPars.Select_model == CalculateModel.ad_and_hd)
            {
                //一维模型(注意，需要保存的是分钟)
                Model_Const.Now_Model_SaveTime = (int)default_model.Mike11Pars.Mike11_savetimestep;
            }
            else
            {
                //产汇流模型
                Model_Const.Now_Model_SaveTime = 60;
            }
        }
    }
}

