using System;

namespace bjd_model.CatchMent
{
    public partial class RR11
    {
        #region *********************** 流域操作 -- 修改流域产汇流模型参数 ******************************
        // 修改指定流域的 NAM模型初始条件
        public static void Modify_Nam_Initial(ref HydroModel hydromodel, string catchmentname, Nam_InitialCondition new_initial)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;
            if (nowrfmodel is Nam)
            {
                Nam nammodel = nowrfmodel as Nam;
                nammodel.NAMInitial = new_initial;

                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Nam)
                    {
                        Nam nammodel = nowrfmodel as Nam;
                        nammodel.NAMInitial = new_initial;
                        break;
                    }
                }
            }
        }

        // 修改指定流域 NAM模型的参数
        public static void Modify_NAM_Parameter(ref HydroModel hydromodel, string catchmentname, NAMparameters new_nampara)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;
            if (nowrfmodel is Nam)
            {
                Nam nammodel = nowrfmodel as Nam;
                nammodel.NAMpar = new_nampara;

                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Nam)
                    {
                        Nam nammodel = nowrfmodel as Nam;
                        nammodel.NAMpar = new_nampara;
                        break;
                    }
                }
            }
        }

        // 修改指定流域 UHM模型的参数
        public static void Modify_UHM_Parameter(ref HydroModel hydromodel, string catchmentname, UHMparameters uhmpara)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;

            if (nowrfmodel is Uhm)
            {
                Uhm uhmmodel = nowrfmodel as Uhm;
                uhmmodel.UHMpar = uhmpara;

                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Uhm)
                    {
                        Uhm uhmmodel = nowrfmodel as Uhm;
                        uhmmodel.UHMpar = uhmpara;
                        break;
                    }
                }
            }
        }

        // 修改指定流域 XAJ模型的参数
        public static void Modify_XAJ_Parameter(ref HydroModel hydromodel, string catchmentname, Xajparameters xajpara)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;

            if (nowrfmodel is Xaj)
            {
                Xaj xajmodel = nowrfmodel as Xaj;
                xajmodel.XajPar = xajpara;

                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Xaj)
                    {
                        Xaj xajmodel = rfmodel as Xaj;
                        xajmodel.XajPar = xajpara;
                        break;
                    }
                }
            }

        }

        // 修改指定流域 XAJ模型的初始条件
        public static void Modify_XAJ_Initial(ref HydroModel hydromodel, string catchmentname, XajInitialConditional xajInitial)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            CatchmentInfo catchment = catchmentlist.Get_Catchmentinfo(catchmentname);
            Rfmodel nowrfmodel = catchment.Now_Rfmodel;

            if (nowrfmodel is Xaj)
            {
                Xaj xajmodel = nowrfmodel as Xaj;
                xajmodel.XajInitial = xajInitial;

                catchment.Update_Rfmodellist();
            }
            else
            {
                for (int i = 0; i < catchment.Rfmodel_List.Count; i++)
                {
                    Rfmodel rfmodel = catchment.Rfmodel_List[i];
                    if (rfmodel is Xaj)
                    {
                        Xaj xajmodel = rfmodel as Xaj;
                        xajmodel.XajInitial = xajInitial;
                        break;
                    }
                }
            }

        }
        #endregion ******************************************************************************************
    }
}
