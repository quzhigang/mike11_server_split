using System;
using System.Collections.Generic;
using System.IO;

using DHI.PFS;

using bjd_model.Mike11;

namespace bjd_model.CatchMent
{
    public partial class RR11
    {
        #region **********************************更新rr11文件*********************************************
        // 提取最新的流域信息和参数、初始条件、降雨时间序列文件，更新RR11文件
        public static void Rewrite_RR11_UpdateFile(HydroModel hydromodel)
        {
            string sourcefilename = hydromodel.BaseModel.Modelfiles.Rr11_filename;
            string outputfilename = hydromodel.Modelfiles.Rr11_filename;
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;
            if (catchmentlist.Catchment_infolist == null) return;

            PFSFile pfsfile = new PFSFile(sourcefilename, false);
            PFSSection MIKE_RR = pfsfile.GetTarget("MIKE_RR", 1);

            Update_CatchListSec(hydromodel, ref MIKE_RR);
            Console.WriteLine("流域基本信息节更新成功!");

            Update_ParameterListSec(hydromodel, ref MIKE_RR);
            Console.WriteLine("流域参数节更新成功!");

            Update_TimeseriesSec(hydromodel, ref MIKE_RR);
            Console.WriteLine("时间序列节更新成功!");

            pfsfile.Write(outputfilename);
            pfsfile.Close();
            Console.WriteLine("RR11产汇流参数文件更新成功!");
            Console.WriteLine("");
        }

        //更新流域区域信息节
        public static void Update_CatchListSec(HydroModel hydromodel, ref PFSSection MIKE_RR)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            MIKE_RR.DeleteSection("CatchList", 1);
            PFSSection CatchList = MIKE_RR.InsertNewSection("CatchList", 1);

            List<CatchmentInfo> catchmentinfolist = catchmentlist.Catchment_infolist;
            if (catchmentinfolist == null) return;

            for (int i = 0; i < catchmentinfolist.Count; i++)
            {
                if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM || catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                {
                    PFSSection catchmentsec = CatchList.InsertNewSection("Catchment", i + 1);
                    Insert_CatchMentKey(catchmentinfolist[i], ref catchmentsec);
                }
            }
        }

        //添加流域Catchhment基本信息节
        public static void Insert_CatchMentKey(CatchmentInfo catchment, ref PFSSection catchmentsec)
        {

            PFSKeyword Catchment_Name = catchmentsec.InsertNewKeyword("Catchment_Name", 1);
            Catchment_Name.InsertNewParameterString(catchment.Name, 1);
            PFSKeyword Catchment_Model = catchmentsec.InsertNewKeyword("Catchment_Model", 2);
            if (catchment.Now_RfmodelType == RFModelType.NAM)
            {
                Catchment_Model.InsertNewParameterString("NAM", 1);
            }
            else if (catchment.Now_RfmodelType == RFModelType.UHM)
            {
                Catchment_Model.InsertNewParameterString("UHM", 1);
            }

            PFSKeyword Catchment_Area = catchmentsec.InsertNewKeyword("Catchment_Area", 3);
            Catchment_Area.InsertNewParameterDouble(catchment.Area, 1);
            PFSKeyword Number_ID = catchmentsec.InsertNewKeyword("Number_ID", 4);
            Number_ID.InsertNewParameterInt(catchment.Id, 1);
            PFSKeyword Additional_output = catchmentsec.InsertNewKeyword("Additional_output", 5);
            Additional_output.InsertNewParameterBool(true, 1);
            PFSKeyword Calibration_plot = catchmentsec.InsertNewKeyword("Calibration_plot", 6);
            Calibration_plot.InsertNewParameterBool(false, 1);
        }

        //更新流域参数集合节
        public static void Update_ParameterListSec(HydroModel hydromodel, ref PFSSection MIKE_RR)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            MIKE_RR.DeleteSection("ParameterList", 1);
            PFSSection ParameterList = MIKE_RR.InsertNewSection("ParameterList", 1);

            List<CatchmentInfo> catchmentinfolist = catchmentlist.Catchment_infolist;
            if (catchmentinfolist == null) return;

            for (int i = 0; i < catchmentinfolist.Count; i++)
            {
                if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM || catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                {
                    if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM)
                    {
                        PFSSection NAM_Parameters = ParameterList.InsertNewSection("NAM_Parameters", i + 1);
                        Insert_NamparameterSec(ref NAM_Parameters, catchmentinfolist[i].Now_Rfmodel as Nam);
                    }
                    else if (catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                    {
                        PFSSection UHMParameters = ParameterList.InsertNewSection("UHMParameters", i + 1);
                        string modeldir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);
                        Insert_UhmparameterSec(ref UHMParameters, modeldir, catchmentinfolist[i].Now_Rfmodel as Uhm);
                    }
                }
            }
        }

        //插入NAM模型参数节 关键字和参数
        public static void Insert_NamparameterSec(ref PFSSection namparsec, Nam Nammodel)
        {
            PFSSection SurfaceRootzone = namparsec.InsertNewSection("SurfaceRootzone", 1);
            PFSKeyword U_Max = SurfaceRootzone.InsertNewKeyword("U_Max", 1);
            U_Max.InsertNewParameterDouble(Nammodel.NAMpar.U_Max, 1);
            PFSKeyword L_Max = SurfaceRootzone.InsertNewKeyword("L_Max", 2);
            L_Max.InsertNewParameterDouble(Nammodel.NAMpar.L_Max, 1);
            PFSKeyword CQOF = SurfaceRootzone.InsertNewKeyword("CQOF", 3);
            CQOF.InsertNewParameterDouble(Nammodel.NAMpar.CQOF, 1);
            PFSKeyword CKIF = SurfaceRootzone.InsertNewKeyword("CKIF", 4);
            CKIF.InsertNewParameterDouble(Nammodel.NAMpar.CKIF, 1);
            PFSKeyword CK1 = SurfaceRootzone.InsertNewKeyword("CK1", 5);
            CK1.InsertNewParameterDouble(Nammodel.NAMpar.CK1, 1);
            PFSKeyword CK12_DIF = SurfaceRootzone.InsertNewKeyword("CK12_DIF", 6);
            CK12_DIF.InsertNewParameterBool(false, 1);
            PFSKeyword CK2 = SurfaceRootzone.InsertNewKeyword("CK2", 7);
            CK2.InsertNewParameterDouble(Nammodel.NAMpar.CK1, 1);
            PFSKeyword TOF = SurfaceRootzone.InsertNewKeyword("TOF", 8);
            TOF.InsertNewParameterDouble(Nammodel.NAMpar.TOF, 1);
            PFSKeyword TIF = SurfaceRootzone.InsertNewKeyword("TIF", 9);
            TIF.InsertNewParameterDouble(Nammodel.NAMpar.TIF, 1);

            PFSSection GroundWater = namparsec.InsertNewSection("GroundWater", 2);
            PFSKeyword IS_CAREA = GroundWater.InsertNewKeyword("IS_CAREA", 1);
            IS_CAREA.InsertNewParameterBool(false, 1);
            PFSKeyword CAREA = GroundWater.InsertNewKeyword("CAREA", 2);
            CAREA.InsertNewParameterDouble(1, 1);
            PFSKeyword TG = GroundWater.InsertNewKeyword("TG", 3);
            TG.InsertNewParameterDouble(Nammodel.NAMpar.TG, 1);
            PFSKeyword IS_SY = GroundWater.InsertNewKeyword("IS_SY", 4);
            IS_SY.InsertNewParameterBool(false, 1);
            PFSKeyword S_Y = GroundWater.InsertNewKeyword("S_Y", 5);
            S_Y.InsertNewParameterDouble(0.1, 1);
            PFSKeyword CKBF = GroundWater.InsertNewKeyword("CKBF", 6);
            CKBF.InsertNewParameterDouble(Nammodel.NAMpar.CKBF, 1);
            PFSKeyword IS_GWLBF0 = GroundWater.InsertNewKeyword("IS_GWLBF0", 7);
            IS_GWLBF0.InsertNewParameterBool(false, 1);
            PFSKeyword GWLBF0 = GroundWater.InsertNewKeyword("GWLBF0", 8);
            GWLBF0.InsertNewParameterDouble(10, 1);
            PFSKeyword GWLBF0_Season = GroundWater.InsertNewKeyword("GWLBF0_Season", 9);
            GWLBF0_Season.InsertNewParameterBool(false, 1);
            PFSKeyword GWLBF_min = GroundWater.InsertNewKeyword("GWLBF_min", 10);
            GWLBF_min.InsertNewParameterDouble(0, 1);
            PFSKeyword GWLBF = GroundWater.InsertNewKeyword("GWLBF", 11);
            object[] GWLBF_array = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            Nwk11.InsertKeyPars(ref GWLBF, GWLBF_array);
            PFSKeyword Is_GWLBF1 = GroundWater.InsertNewKeyword("Is_GWLBF1", 12);
            Is_GWLBF1.InsertNewParameterBool(false, 1);
            PFSKeyword GWLBF1 = GroundWater.InsertNewKeyword("GWLBF1", 13);
            GWLBF1.InsertNewParameterDouble(0, 1);
            PFSKeyword GWPUMP_Season = GroundWater.InsertNewKeyword("GWPUMP_Season", 14);
            GWPUMP_Season.InsertNewParameterBool(false, 1);
            PFSKeyword GWPUMP_File = GroundWater.InsertNewKeyword("GWPUMP_File", 15);
            GWPUMP_File.InsertNewParameterBool(false, 1);
            PFSKeyword GWPUMP = GroundWater.InsertNewKeyword("GWPUMP", 16);
            object[] GWPUMP_array = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Nwk11.InsertKeyPars(ref GWPUMP, GWPUMP_array);
            PFSKeyword GW_LOWER = GroundWater.InsertNewKeyword("GW_LOWER", 17);
            GW_LOWER.InsertNewParameterBool(false, 1);
            PFSKeyword CQLOW = GroundWater.InsertNewKeyword("CQLOW", 18);
            CQLOW.InsertNewParameterDouble(0, 1);
            PFSKeyword CKLOW = GroundWater.InsertNewKeyword("CKLOW", 19);
            CKLOW.InsertNewParameterDouble(10000, 1);

            PFSSection SnowMelt = namparsec.InsertNewSection("SnowMelt", 3);
            PFSKeyword SNOWused = SnowMelt.InsertNewKeyword("SNOWused", 1);
            SNOWused.InsertNewParameterBool(false, 1);
            PFSKeyword C1_SNOW = SnowMelt.InsertNewKeyword("C1_SNOW", 2);
            C1_SNOW.InsertNewParameterDouble(2, 1);
            PFSKeyword T0_SNOW = SnowMelt.InsertNewKeyword("T0_SNOW", 3);
            T0_SNOW.InsertNewParameterDouble(0, 1);
            PFSKeyword C2_Season = SnowMelt.InsertNewKeyword("C2_Season", 4);
            C2_Season.InsertNewParameterBool(false, 1);
            PFSKeyword C2_File = SnowMelt.InsertNewKeyword("C2_File", 5);
            C2_File.InsertNewParameterBool(false, 1);
            PFSKeyword C2_SNOW = SnowMelt.InsertNewKeyword("C2_SNOW", 6);
            object[] C2_SNOW_array = { 1, 1.5, 2, 3, 4, 4.5, 4.5, 4, 3, 2, 1.5, 1 };
            Nwk11.InsertKeyPars(ref C2_SNOW, C2_SNOW_array);

            PFSKeyword C_RAIN_USED = SnowMelt.InsertNewKeyword("C_RAIN_USED", 7);
            C_RAIN_USED.InsertNewParameterBool(false, 1);
            PFSKeyword C_RAIN = SnowMelt.InsertNewKeyword("C_RAIN ", 8);
            C_RAIN.InsertNewParameterDouble(0, 1);
            PFSKeyword C_RADIATION_Used = SnowMelt.InsertNewKeyword("C_RADIATION_Used", 9);
            C_RADIATION_Used.InsertNewParameterBool(false, 1);
            PFSKeyword C_RADIATION = SnowMelt.InsertNewKeyword("C_RADIATION", 10);
            C_RADIATION.InsertNewParameterDouble(0, 1);
            PFSKeyword SNOW_Zones = SnowMelt.InsertNewKeyword("SNOW_Zones", 11);
            SNOW_Zones.InsertNewParameterBool(false, 1);
            PFSSection ZoneList = SnowMelt.InsertNewSection("Zonelist", 1);
            PFSKeyword NO_ZONES = ZoneList.InsertNewKeyword("NO_ZONES", 1);
            NO_ZONES.InsertNewParameterInt(10, 1);
            PFSKeyword T_ELEVREF = ZoneList.InsertNewKeyword("T_ELEVREF", 2);
            T_ELEVREF.InsertNewParameterDouble(0, 1);
            PFSKeyword T_DRYCHECK = ZoneList.InsertNewKeyword("T_DRYCHECK", 3);
            T_DRYCHECK.InsertNewParameterBool(false, 1);
            PFSKeyword T_LAPSEDRY = ZoneList.InsertNewKeyword("T_LAPSEDRY", 4);
            T_LAPSEDRY.InsertNewParameterDouble(-0.6, 1);
            PFSKeyword T_WETCHECK = ZoneList.InsertNewKeyword("T_WETCHECK", 5);
            T_WETCHECK.InsertNewParameterBool(false, 1);
            PFSKeyword T_LAPSEWET = ZoneList.InsertNewKeyword("T_LAPSEWET", 6);
            T_LAPSEWET.InsertNewParameterDouble(-0.4, 1);
            PFSKeyword P_CHECK = ZoneList.InsertNewKeyword("P_CHECK", 7);
            P_CHECK.InsertNewParameterBool(false, 1);
            PFSKeyword P_ELEVREF = ZoneList.InsertNewKeyword("P_ELEVREF", 8);
            P_ELEVREF.InsertNewParameterDouble(0, 1);
            PFSKeyword P_LAPSE = ZoneList.InsertNewKeyword("P_LAPSE", 9);
            P_LAPSE.InsertNewParameterDouble(2, 1);
            for (int i = 1; i < 11; i++)
            {
                PFSSection Zone = ZoneList.InsertNewSection("Zone", i);
                PFSKeyword ZONE_ELEVATION = Zone.InsertNewKeyword("ZONE_ELEVATION", 1);
                ZONE_ELEVATION.InsertNewParameterDouble(100 * i, 1);
                PFSKeyword ZONE_AREA = Zone.InsertNewKeyword("ZONE_AREA", 2);
                ZONE_AREA.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_MINSNOW = Zone.InsertNewKeyword("ZONE_MINSNOW", 3);
                ZONE_MINSNOW.InsertNewParameterDouble(100, 1);
                PFSKeyword ZONE_MAXSNOW = Zone.InsertNewKeyword("ZONE_MAXSNOW", 4);
                ZONE_MAXSNOW.InsertNewParameterDouble(10000, 1);
                PFSKeyword ZONE_MAXWATER = Zone.InsertNewKeyword("ZONE_MAXWATER", 5);
                ZONE_MAXWATER.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_TDRYCOR = Zone.InsertNewKeyword("ZONE_TDRYCOR", 6);
                ZONE_TDRYCOR.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_TWETCOR = Zone.InsertNewKeyword("ZONE_TWETCOR", 7);
                ZONE_TWETCOR.InsertNewParameterDouble(0, 1);
                PFSKeyword ZONE_PCOR = Zone.InsertNewKeyword("ZONE_PCOR", 8);
                ZONE_PCOR.InsertNewParameterDouble(0, 1);
            }

            PFSSection Irrigation = namparsec.InsertNewSection("Irrigation", 4);
            PFSKeyword IRRIGATION = Irrigation.InsertNewKeyword("IRRIGATION", 1);
            IRRIGATION.InsertNewParameterBool(false, 1);
            PFSKeyword K0INF = Irrigation.InsertNewKeyword("K0INF", 2);
            K0INF.InsertNewParameterDouble(1, 1);
            PFSKeyword PCT_LGW = Irrigation.InsertNewKeyword("PCT_LGW", 3);
            PCT_LGW.InsertNewParameterDouble(50, 1);
            PFSKeyword PCT_LRI = Irrigation.InsertNewKeyword("PCT_LRI", 4);
            PCT_LRI.InsertNewParameterDouble(50, 1);
            PFSKeyword PCT_EXT = Irrigation.InsertNewKeyword("PCT_EXT", 5);
            PCT_EXT.InsertNewParameterDouble(0, 1);
            PFSKeyword EXT_RIVERNAME = Irrigation.InsertNewKeyword("EXT_RIVERNAME", 6);
            EXT_RIVERNAME.InsertNewParameterString("", 1);
            PFSKeyword EXT_RIVERCHAIN = Irrigation.InsertNewKeyword("EXT_RIVERCHAIN", 7);
            EXT_RIVERCHAIN.InsertNewParameterDouble(0, 1);
            PFSKeyword IRR_CROPLOSSES = Irrigation.InsertNewKeyword("IRR_CROPLOSSES", 8);
            IRR_CROPLOSSES.InsertNewParameterBool(true, 1);
            PFSKeyword CROP_COEF = Irrigation.InsertNewKeyword("CROP_COEF", 9);
            for (int i = 1; i < 13; i++)
            {
                CROP_COEF.InsertNewParameterDouble(1, i);
            }
            PFSKeyword LOSS_GRW = Irrigation.InsertNewKeyword("LOSS_GRW", 10);
            for (int i = 1; i < 13; i++)
            {
                LOSS_GRW.InsertNewParameterDouble(0, i);
            }
            PFSKeyword LOSS_OF = Irrigation.InsertNewKeyword("LOSS_OF", 11);
            for (int i = 1; i < 13; i++)
            {
                LOSS_OF.InsertNewParameterDouble(0, i);
            }
            PFSKeyword LOSS_EVAP = Irrigation.InsertNewKeyword("LOSS_EVAP", 12);
            for (int i = 1; i < 13; i++)
            {
                LOSS_EVAP.InsertNewParameterDouble(0, i);
            }

            PFSSection InitialCondition = namparsec.InsertNewSection("InitialCondition", 5);
            PFSKeyword U_Ini = InitialCondition.InsertNewKeyword("U_Ini", 1);
            U_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.U_Ini, 1);
            PFSKeyword L_Ini = InitialCondition.InsertNewKeyword("L_Ini", 2);
            L_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.L_Ini, 1);
            PFSKeyword OF_Ini = InitialCondition.InsertNewKeyword("OF_Ini", 3);
            OF_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.OF_Ini, 1);
            PFSKeyword IF_Ini = InitialCondition.InsertNewKeyword("IF_Ini", 4);
            IF_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.IF_Ini, 1);
            PFSKeyword GWL_Ini = InitialCondition.InsertNewKeyword("GWL_Ini", 5);
            GWL_Ini.InsertNewParameterDouble(Nammodel.NAMInitial.BFlow, 1);
            PFSKeyword BFlow = InitialCondition.InsertNewKeyword("BFlow", 6);
            BFlow.InsertNewParameterDouble(0, 1);
            PFSKeyword Snow_Ini = InitialCondition.InsertNewKeyword("Snow_Ini", 7);
            Snow_Ini.InsertNewParameterDouble(0, 1);
            PFSSection Zonelist = InitialCondition.InsertNewSection("Zonelist", 1);
            for (int i = 1; i < 11; i++)
            {
                PFSSection Zone = Zonelist.InsertNewSection("Zone", i);
                PFSKeyword snow_Ini = Zone.InsertNewKeyword("Snow_Ini", 1);
                snow_Ini.InsertNewParameterDouble(0, 1);
                PFSKeyword Water_Ini = Zone.InsertNewKeyword("Water_Ini", 2);
                Water_Ini.InsertNewParameterDouble(0, 1);
            }

            PFSSection AutoCal = namparsec.InsertNewSection("AutoCal", 6);
            PFSKeyword AUTOCAL = AutoCal.InsertNewKeyword("AUTOCAL", 1);
            AUTOCAL.InsertNewParameterBool(false, 1);
            PFSKeyword U_MAX_fit = AutoCal.InsertNewKeyword("U_MAX_fit ", 2);
            U_MAX_fit.InsertNewParameterBool(true, 1);
            PFSKeyword U_Max_upper = AutoCal.InsertNewKeyword("U_Max_upper", 3);
            U_Max_upper.InsertNewParameterDouble(20, 1);
            PFSKeyword U_Max_lower = AutoCal.InsertNewKeyword("U_Max_lower", 4);
            U_Max_lower.InsertNewParameterDouble(10, 1);
            PFSKeyword L_MAX_fit = AutoCal.InsertNewKeyword("L_MAX_fit", 5);
            L_MAX_fit.InsertNewParameterBool(true, 1);
            PFSKeyword L_Max_upper = AutoCal.InsertNewKeyword("L_Max_upper", 6);
            L_Max_upper.InsertNewParameterDouble(300, 1);
            PFSKeyword L_Max_lower = AutoCal.InsertNewKeyword("L_Max_lower", 7);
            L_Max_lower.InsertNewParameterDouble(100, 1);
            PFSKeyword CQOF_fit = AutoCal.InsertNewKeyword("CQOF_fit", 8);
            CQOF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CQOF_upper = AutoCal.InsertNewKeyword("CQOF_upper", 9);
            CQOF_upper.InsertNewParameterDouble(1, 1);
            PFSKeyword CQOF_lower = AutoCal.InsertNewKeyword("CQOF_lower", 10);
            CQOF_lower.InsertNewParameterDouble(0.1, 1);
            PFSKeyword CKIF_fit = AutoCal.InsertNewKeyword("CKIF_fit", 11);
            CKIF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CKIF_upper = AutoCal.InsertNewKeyword("CKIF_upper", 12);
            CKIF_upper.InsertNewParameterDouble(1000, 1);
            PFSKeyword CKIF_lower = AutoCal.InsertNewKeyword("CKIF_lower", 13);
            CKIF_lower.InsertNewParameterDouble(200, 1);
            PFSKeyword CK1_fit = AutoCal.InsertNewKeyword("CK1_fit", 14);
            CK1_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CK1_upper = AutoCal.InsertNewKeyword("CK1_upper", 15);
            CK1_upper.InsertNewParameterDouble(50, 1);
            PFSKeyword CK1_lower = AutoCal.InsertNewKeyword("CK1_lower", 16);
            CK1_lower.InsertNewParameterDouble(10, 1);
            PFSKeyword TOF_fit = AutoCal.InsertNewKeyword("TOF_fit", 17);
            TOF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword TOF_upper = AutoCal.InsertNewKeyword("TOF_upper", 18);
            TOF_upper.InsertNewParameterDouble(0.99, 1);
            PFSKeyword TOF_lower = AutoCal.InsertNewKeyword("TOF_lower", 19);
            TOF_lower.InsertNewParameterDouble(0, 1);
            PFSKeyword TIF_fit = AutoCal.InsertNewKeyword("TIF_fit", 20);
            TIF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword TIF_upper = AutoCal.InsertNewKeyword("TIF_upper", 21);
            TIF_upper.InsertNewParameterDouble(0.99, 1);
            PFSKeyword TIF_lower = AutoCal.InsertNewKeyword("TIF_lower", 22);
            TIF_lower.InsertNewParameterDouble(0, 1);
            PFSKeyword TG_fit = AutoCal.InsertNewKeyword("TG_fit", 23);
            TG_fit.InsertNewParameterBool(true, 1);
            PFSKeyword TG_upper = AutoCal.InsertNewKeyword("TG_upper", 24);
            TG_upper.InsertNewParameterDouble(0.99, 1);
            PFSKeyword TG_lower = AutoCal.InsertNewKeyword("TG_lower", 25);
            TG_lower.InsertNewParameterDouble(0, 1);
            PFSKeyword CKBF_fit = AutoCal.InsertNewKeyword("CKBF_fit", 26);
            CKBF_fit.InsertNewParameterBool(true, 1);
            PFSKeyword CKBF_upper = AutoCal.InsertNewKeyword("CKBF_upper", 27);
            CKBF_upper.InsertNewParameterDouble(4000, 1);
            PFSKeyword CKBF_lower = AutoCal.InsertNewKeyword("CKBF_lower", 28);
            CKBF_lower.InsertNewParameterDouble(1000, 1);
            PFSKeyword CK2_fit = AutoCal.InsertNewKeyword("CK2_fit", 29);
            CK2_fit.InsertNewParameterBool(false, 1);
            PFSKeyword CK2_upper = AutoCal.InsertNewKeyword("CK2_upper", 30);
            CK2_upper.InsertNewParameterDouble(50, 1);
            PFSKeyword CK2_lower = AutoCal.InsertNewKeyword("CK2_lower", 31);
            CK2_lower.InsertNewParameterDouble(10, 1);
            PFSKeyword CQLOW_fit = AutoCal.InsertNewKeyword("CQLOW_fit", 32);
            CQLOW_fit.InsertNewParameterBool(false, 1);
            PFSKeyword CQLOW_upper = AutoCal.InsertNewKeyword("CQLOW_upper", 33);
            CQLOW_upper.InsertNewParameterDouble(100, 1);
            PFSKeyword CQLOW_lower = AutoCal.InsertNewKeyword("CQLOW_lower", 34);
            CQLOW_lower.InsertNewParameterDouble(1, 1);
            PFSKeyword CKLOW_fit = AutoCal.InsertNewKeyword("CKLOW_fit", 35);
            CKLOW_fit.InsertNewParameterBool(false, 1);
            PFSKeyword CKLOW_upper = AutoCal.InsertNewKeyword("CKLOW_upper", 36);
            CKLOW_upper.InsertNewParameterDouble(30000, 1);
            PFSKeyword CKLOW_lower = AutoCal.InsertNewKeyword("CKLOW_lower", 37);
            CKLOW_lower.InsertNewParameterDouble(1000, 1);
            PFSKeyword WBL = AutoCal.InsertNewKeyword("WBL", 38);
            WBL.InsertNewParameterBool(true, 1);
            PFSKeyword RMSE = AutoCal.InsertNewKeyword("RMSE", 39);
            RMSE.InsertNewParameterBool(true, 1);
            PFSKeyword Peak_flow_RMSE = AutoCal.InsertNewKeyword("Peak_flow_RMSE", 40);
            Peak_flow_RMSE.InsertNewParameterBool(false, 1);
            PFSKeyword Low_flow_RMSE = AutoCal.InsertNewKeyword("Low_flow_RMSE", 41);
            Low_flow_RMSE.InsertNewParameterBool(false, 1);
            PFSKeyword Peak_flow_min = AutoCal.InsertNewKeyword("Peak_flow_min", 42);
            Peak_flow_min.InsertNewParameterDouble(0, 1);
            PFSKeyword Low_flow_max = AutoCal.InsertNewKeyword("Low_flow_max", 43);
            Low_flow_max.InsertNewParameterDouble(0, 1);
            PFSKeyword Maximum_evaluation = AutoCal.InsertNewKeyword("Maximum_evaluation", 44);
            Maximum_evaluation.InsertNewParameterDouble(2000, 1);
            PFSKeyword Initial_excluded = AutoCal.InsertNewKeyword("Initial_excluded", 45);
            Initial_excluded.InsertNewParameterDouble(0, 1);
        }

        // 写入 UHM 模型参数节
        public static void Insert_UhmparameterSec(ref PFSSection UHMParameters, string modeldir, Uhm uhmmodel)
        {
            PFSKeyword Area_RF = UHMParameters.InsertNewKeyword("Area_RF", 1);
            Area_RF.InsertNewParameterDouble(uhmmodel.UHMpar.Area_RF, 1);
            PFSKeyword Baseflow = UHMParameters.InsertNewKeyword("Baseflow", 2);
            Baseflow.InsertNewParameterDouble(uhmmodel.UHMpar.Baseflow, 1);
            PFSKeyword LossModel = UHMParameters.InsertNewKeyword("LossModel", 3);
            LossModel.InsertNewParameterDouble(0, 1);
            PFSKeyword InitLoss = UHMParameters.InsertNewKeyword("InitLoss", 4);
            InitLoss.InsertNewParameterDouble(uhmmodel.UHMpar.InitLoss, 1);
            PFSKeyword ConstLoss = UHMParameters.InsertNewKeyword("ConstLoss", 5);
            ConstLoss.InsertNewParameterDouble(uhmmodel.UHMpar.ConstLoss, 1);
            PFSKeyword RunoffCoef = UHMParameters.InsertNewKeyword("RunoffCoef", 6);
            RunoffCoef.InsertNewParameterDouble(0.75, 1);
            PFSKeyword LossCurveNumber = UHMParameters.InsertNewKeyword("LossCurveNumber", 7);
            LossCurveNumber.InsertNewParameterDouble(75, 1);
            PFSKeyword InitialAMC = UHMParameters.InsertNewKeyword("InitialAMC", 8);
            InitialAMC.InsertNewParameterDouble(2, 1);
            PFSKeyword Hydrograph = UHMParameters.InsertNewKeyword("Hydrograph", 9);
            Hydrograph.InsertNewParameterDouble(2, 1);
            PFSKeyword Filename = UHMParameters.InsertNewKeyword("Filename", 10);

            string uhmfilename = modeldir + @"\" + uhmmodel.Catchment_Name + "_uhm.dfs0";

            Filename.InsertNewParameterFileName(uhmfilename, 1);
            PFSKeyword Item = UHMParameters.InsertNewKeyword("Item", 11);
            Item.InsertNewParameterString("hydrograph", 1);
            Item.InsertNewParameterDouble(0, 2);
            PFSKeyword LagTime = UHMParameters.InsertNewKeyword("LagTime", 12);
            LagTime.InsertNewParameterDouble(0, 1);
            PFSKeyword Tlag = UHMParameters.InsertNewKeyword("Tlag", 13);
            Tlag.InsertNewParameterDouble(uhmmodel.UHMpar.Tlag, 1);
            PFSKeyword HLength = UHMParameters.InsertNewKeyword("HLength", 14);
            HLength.InsertNewParameterDouble(10, 1);
            PFSKeyword Slope = UHMParameters.InsertNewKeyword("Slope", 15);
            Slope.InsertNewParameterDouble(5, 1);
            PFSKeyword LagCurveNumber = UHMParameters.InsertNewKeyword("LagCurveNumber", 16);
            LagCurveNumber.InsertNewParameterDouble(75, 1);
            PFSKeyword InitialAbstractionDepth = UHMParameters.InsertNewKeyword("InitialAbstractionDepth", 17);
            InitialAbstractionDepth.InsertNewParameterDouble(1, 1);
            PFSSection UHM_EffectiveRainfall_Parameters = UHMParameters.InsertNewSection("UHM_EffectiveRainfall_Parameters", 1);
            PFSKeyword RainfallEnlargementNumber = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("RainfallEnlargementNumber", 1);
            RainfallEnlargementNumber.InsertNewParameterDouble(0, 1);
            PFSKeyword EffectiveRainfallNumber = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("EffectiveRainfallNumber", 2);
            EffectiveRainfallNumber.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantK = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantK", 3);
            BasinConstantK.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantP = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantP", 4);
            BasinConstantP.InsertNewParameterDouble(0, 1);
            PFSKeyword TimeOfDelay = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("TimeOfDelay", 5);
            TimeOfDelay.InsertNewParameterDouble(0, 1);
            PFSKeyword f1 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("f1", 6);
            f1.InsertNewParameterDouble(0, 1);
            PFSKeyword Rsa = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("Rsa", 7);
            Rsa.InsertNewParameterDouble(0, 1);
            PFSKeyword f2 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("f2", 8);
            f2.InsertNewParameterDouble(0, 1);
            PFSKeyword QLSFLossMethod = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("QLSFLossMethod", 9);
            QLSFLossMethod.InsertNewParameterDouble(0, 1);
            PFSKeyword LandUseArea = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("LandUseArea", 10);
            LandUseArea.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantT1 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantT1", 11);
            BasinConstantT1.InsertNewParameterDouble(0, 1);
            PFSKeyword BasinConstantT03 = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("BasinConstantT03", 12);
            BasinConstantT03.InsertNewParameterDouble(0, 1);
            PFSKeyword TimeConcentration = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("TimeConcentration", 13);
            TimeConcentration.InsertNewParameterDouble(0, 1);
            PFSKeyword RationalRunoffCoef = UHM_EffectiveRainfall_Parameters.InsertNewKeyword("RationalRunoffCoef", 14);
            RationalRunoffCoef.InsertNewParameterDouble(0, 1);
            PFSSection UHM_KinematicWave_Parameters = UHMParameters.InsertNewSection("UHM_KinematicWave_Parameters", 2);
            PFSKeyword ANS1 = UHM_KinematicWave_Parameters.InsertNewKeyword("ANS1", 1);
            ANS1.InsertNewParameterDouble(0, 1);
            PFSKeyword AL = UHM_KinematicWave_Parameters.InsertNewKeyword("AL", 2);
            AL.InsertNewParameterDouble(0, 1);
            PFSKeyword ALR = UHM_KinematicWave_Parameters.InsertNewKeyword("ALR", 3);
            ALR.InsertNewParameterDouble(0, 1);
            PFSKeyword SI = UHM_KinematicWave_Parameters.InsertNewKeyword("SI", 4);
            SI.InsertNewParameterDouble(0, 1);
            PFSKeyword ANS2 = UHM_KinematicWave_Parameters.InsertNewKeyword("ANS2", 5);
            ANS2.InsertNewParameterDouble(0, 1);
            PFSKeyword SLO = UHM_KinematicWave_Parameters.InsertNewKeyword("SLO", 6);
            SLO.InsertNewParameterDouble(0, 1);
        }

        // 更新时间序列节
        public static void Update_TimeseriesSec(HydroModel hydromodel, ref PFSSection MIKE_RR)
        {
            CatchmentList catchmentlist = hydromodel.RfPars.Catchmentlist;

            MIKE_RR.DeleteSection("TimeseriesList", 1);
            PFSSection TimeseriesList = MIKE_RR.InsertNewSection("TimeseriesList", 1);
            TimeseriesList.InsertNewKeyword("Max_Comb_Number", 1).InsertNewParameterInt(8, 1);

            List<CatchmentInfo> catchmentinfolist = catchmentlist.Catchment_infolist;
            if (catchmentinfolist == null) return;

            int conditionnumber = 1;
            string modeldir = Path.GetDirectoryName(hydromodel.Modelfiles.Simulate_filename);
            for (int i = 0; i < catchmentinfolist.Count; i++)
            {
                if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM || catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                {
                    if (catchmentinfolist[i].Now_RfmodelType == RFModelType.NAM)
                    {
                        PFSSection Condition1 = TimeseriesList.InsertNewSection("Condition", conditionnumber);
                        Insert_RainConditionKey(ref Condition1, catchmentinfolist[i].Name, modeldir);
                        conditionnumber++;

                        PFSSection Condition2 = TimeseriesList.InsertNewSection("Condition", conditionnumber);
                        Insert_EvpConditionKey(ref Condition2, catchmentinfolist[i].Name, modeldir);
                        conditionnumber++;
                    }
                    else if (catchmentinfolist[i].Now_RfmodelType == RFModelType.UHM)
                    {
                        PFSSection Condition = TimeseriesList.InsertNewSection("Condition", conditionnumber);
                        Insert_RainConditionKey(ref Condition, catchmentinfolist[i].Name, modeldir);
                        conditionnumber++;
                    }
                }
            }
        }

        // 写入降雨时间序列条件节
        public static void Insert_RainConditionKey(ref PFSSection Condition, string catchmentname, string modeldir)
        {
            PFSKeyword catchmentNameRain = Condition.InsertNewKeyword("Catchment_Name", 1);
            catchmentNameRain.InsertNewParameterString(catchmentname, 1);
            PFSKeyword MAWCalculationRain = Condition.InsertNewKeyword("MAWCalculation", 2);
            MAWCalculationRain.InsertNewParameterBool(false, 1);
            PFSKeyword dataTypeRain = Condition.InsertNewKeyword("DataType", 3);
            dataTypeRain.InsertNewParameterInt(0, 1);
            PFSKeyword timeSeriesRain = Condition.InsertNewKeyword("Timeseries", 4);

            string catchment_rf_filename = modeldir + @"\" + catchmentname + "_rf.dfs0";

            timeSeriesRain.InsertNewParameterFileName(catchment_rf_filename, 1);
            PFSSection timeSeriesItemsRain = Condition.InsertNewSection("TimeSeriesItems", 1);
            PFSKeyword itemRain = timeSeriesItemsRain.InsertNewKeyword("Item", 1);
            itemRain.InsertNewParameterString("rainfall", 1);
            itemRain.InsertNewParameterInt(1, 2);
            Condition.InsertNewSection("Weighted_average_timeseries", 2);
            Condition.InsertNewSection("Weighted_average_combinations", 3);
            Condition.InsertNewSection("Temporal_distribution_timeseries", 4);
            Condition.InsertNewSection("Temporal_distribution_combinations", 5);
        }

        // 写入蒸发时间序列条件节
        public static void Insert_EvpConditionKey(ref PFSSection Condition, string catchmentname, string modeldir)
        {
            PFSKeyword catchmentNameRain = Condition.InsertNewKeyword("Catchment_Name", 1);
            catchmentNameRain.InsertNewParameterString(catchmentname, 1);
            PFSKeyword MAWCalculationRain = Condition.InsertNewKeyword("MAWCalculation", 2);
            MAWCalculationRain.InsertNewParameterBool(false, 1);
            PFSKeyword dataTypeRain = Condition.InsertNewKeyword("DataType", 3);
            dataTypeRain.InsertNewParameterInt(1, 1);
            PFSKeyword timeSeriesRain = Condition.InsertNewKeyword("Timeseries", 4);

            string evp_filename = modeldir + @"\" + "evp.dfs0";

            timeSeriesRain.InsertNewParameterFileName(evp_filename, 1);
            PFSSection timeSeriesItemsRain = Condition.InsertNewSection("TimeSeriesItems", 1);
            PFSKeyword itemRain = timeSeriesItemsRain.InsertNewKeyword("Item", 1);
            itemRain.InsertNewParameterString("evaporation", 1);
            itemRain.InsertNewParameterInt(1, 2);
            Condition.InsertNewSection("Weighted_average_timeseries", 2);
            Condition.InsertNewSection("Weighted_average_combinations", 3);
            Condition.InsertNewSection("Temporal_distribution_timeseries", 4);
            Condition.InsertNewSection("Temporal_distribution_combinations", 5);
        }
        #endregion ******************************************************************************************
    }
}
