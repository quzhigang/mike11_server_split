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
    //集水区连接类
    [Serializable]
    public class Catchment_ConnectList
    {
        //**********************属性************************
        //集水区连接信息
        public List<Catchment_Connectinfo> CatchmentConnect_Infolist
        { get; set; }

        //**********************方法************************
        //增加新集水区连接方法
        public void AddCatchmentConnect(Catchment_Connectinfo newcatchment_connect)
        {
            //如果没有相同名字的集水区则将该连接加入集合
            if (!Have_TheSameCatchment(newcatchment_connect.catchment_name))
            {
                //往集合中增加新集水区连接
                CatchmentConnect_Infolist.Add(newcatchment_connect);
            }
        }

        //增加新集水区连接方法
        public void AddCatchmentConnect(CatchmentList Catchmentlist, string catchmentname, Reach_Segment reach_segment)
        {
            //创建新连接
            Catchment_Connectinfo catchment_connect;
            CatchmentInfo catchment = Catchmentlist.Get_Catchmentinfo(catchmentname);
            catchment_connect.catchment_name = catchment.Name;
            catchment_connect.catchment_area = catchment.Area;
            catchment_connect.connect_reach = reach_segment;

            //判断后加入
            if (!CatchmentConnect_Infolist.Contains(catchment_connect))
            {
                //往集合中增加新集水区连接
                CatchmentConnect_Infolist.Add(catchment_connect);
            }
        }

        //减少集水区连接方法
        public void RemoveCatchmentConnect(string catchmentname)
        {
            for (int i = 0; i < CatchmentConnect_Infolist.Count; i++)
            {
                if (CatchmentConnect_Infolist[i].catchment_name == catchmentname)
                {
                    CatchmentConnect_Infolist.RemoveAt(i);
                    break;
                }
            }
        }

        //根据集水区名获取该集水区连接详细信息的方法
        public Catchment_Connectinfo Get_CatchmentConnect(string catchmentname)
        {
            Catchment_Connectinfo Catchment_connect;
            Catchment_connect.catchment_name = "";
            Catchment_connect.catchment_area = 0;
            Catchment_connect.connect_reach = CatchmentConnect_Infolist[0].connect_reach;

            for (int i = 0; i < CatchmentConnect_Infolist.Count; i++)
            {
                if (CatchmentConnect_Infolist[i].catchment_name == catchmentname)
                {
                    Catchment_connect = CatchmentConnect_Infolist[i];
                    break;
                }
            }
            return Catchment_connect;
        }

        //集水区连接对象集合中是否有相同名字的集水区
        public bool Have_TheSameCatchment(string catchment_name)
        {
            for (int i = 0; i < CatchmentConnect_Infolist.Count; i++)
            {
                if (CatchmentConnect_Infolist[i].catchment_name == catchment_name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
