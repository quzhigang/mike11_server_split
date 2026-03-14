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
    //二维参数
    [Serializable]
    public class Mike21_Pars
    {
        #region 构造函数
        public Mike21_Pars()
        {
            SetDefault();
        }
        #endregion

        #region 属性
        //mike21结果保存模拟时间步倍数
        public SaveTimeStep Mike21_savetimestepbs { get; set; }

        //干湿边界
        public Dry_Wet Dry_wet { get; set; }

        //糙率设置
        public Bed_Resistance Resistance_set { get; set; }

        //二维里的降雨设置
        public Pre_Eva Pre_set { get; set; }

        //二维里的蒸发设置
        public Pre_Eva Eva_set { get; set; }

        //二维里的初始条件设置
        public Initial_Contition Initial_set { get; set; }

        //***** 以下属性为模型数据的集合 *****
        //二维堤防集合
        public DikeList DikeList { get; set; }

        //二维堰（涵洞）集合
        public WeirList WeirList { get; set; }

        //二维网格参数集合,含网格边长
        public MeshPars MeshParsList { get; set; }
        #endregion

        #region 方法
        public void SetDefault()
        {
            //注意：一些参数的元素如果是对象的集合，则必须初始化，否则建立全新模型时会出错
            this.Mike21_savetimestepbs = SaveTimeStep.step_60;

            Dry_Wet Dry_wet;
            Dry_wet.dry = 0.005;
            Dry_wet.flood = 0.05;
            Dry_wet.wet = 0.1;
            this.Dry_wet = Dry_wet;

            Bed_Resistance resistance = Bed_Resistance.Get_Default_Resistance();
            this.Resistance_set = resistance;

            Pre_Eva Pre_set;
            Pre_set.type_pre_eva = Type_Pre_Eva.NoPreEva;
            Pre_set.format_pre_eva = Format_Pre_Eva.constant;
            Pre_set.constantEvaporation = 0;
            Pre_set.fileName = "";
            Pre_set.itemName = "";
            this.Pre_set = Pre_set;

            this.Eva_set = Pre_set;

            Initial_Contition Initial_set;
            Initial_set.initial_type = 1;
            Initial_set.const_surface_elevation = 0;
            Initial_set.fileName = "";
            Initial_set.itemName = "";
            this.Initial_set = Initial_set;

            DikeList DikeList = new DikeList();
            DikeList.Dike_List = new List<Dike>();
            DikeList.MaxDikeId = 0;
            this.DikeList = DikeList;

            WeirList WeirList = new WeirList();
            WeirList.Weir_List = new List<Weir>();
            WeirList.MaxWeirId = 0;
            this.WeirList = WeirList;

            MeshPars MeshParsList = new MeshPars();
            MeshParsList.Mesh_Sidelength = Model_Const.MESHSIDE_LENGTH;
            this.MeshParsList = MeshParsList;
        }

        #endregion
    }


}
