using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using JavaToC;
using java.util;
using java.lang;

using bjd_model.Common;
using bjd_model.Const_Global;
using bjd_model.Mike11;

namespace bjd_model.CatchMent
{

    public class BasForeCastData
    {
        public string reservoirCode = "bab0b0a0001";
        public FloodNo[] floodNo;
        public EvapCode[] evapCode;

        public void setReservoirCode(string reservoirCode)
        {
            this.reservoirCode = reservoirCode;
        }
        public string getReservoirCode()
        {
            return reservoirCode;
        }
        public void setNum(int n)
        {

            floodNo = new FloodNo[n];
        }
        public void setEvapNum(int n)
        {

            evapCode = new EvapCode[n];
        }
    }

    public class BasForeCastDataPOJO
    {
        public BasForeCastData getFloodNo(Xaj xaj)
        {
            BasForeCastData bfcd = new BasForeCastData();

            string[] floodno = new string[] { "洪水过程" };
            List<DateTime> rain_dt = new List<DateTime>(xaj.Step_P.Keys);

            string s_begin = rain_dt[0].ToString("yyyy-MM-dd HH:mm:ss");
            Date dateBegin = String_Date.StringToDate(s_begin);
            string s_end = rain_dt[rain_dt.Count - 1].ToString("yyyy-MM-dd HH:mm:ss");
            Date dateEnd = String_Date.StringToDate(s_end);
            bfcd.setNum(1);
            for (int i = 0; i < bfcd.floodNo.Length; i++)
            {
                bfcd.floodNo[i] = new FloodNo();
                bfcd.floodNo[i].floodno = floodno[i];
                bfcd.floodNo[i].rainbeg = dateBegin;
                bfcd.floodNo[i].raindt = dateEnd;
            }

            return bfcd;
        }
    }

    public class RainCode
    {
        public string rainCode;
        public string rainName;
        public float rainWeight;
        public string idLocation;
        public int n;
        public DayRainOff[] dayRainOff;
        public Date[] p_step_dt;
        public float[] p_step;
        public Date[] p_day_dt;
        public float[] p_day;
        public float[] q_step;
        public Date[] q_step_dt;

        public void setStepRainNum(int n)
        {
            p_step_dt = new Date[n];
            p_step = new float[n];
        }

        public void setStepQNum(int n)
        {
            q_step = new float[n];
            q_step_dt = new Date[n];
        }

        public void setDayNum(int n)
        {
            p_day = new float[n];
            p_day_dt = new Date[n];
        }

        public void setRainCode(string rainCode)
        {
            this.rainCode = rainCode;
        }
        public string getRainCode()
        {
            return rainCode;
        }

        public void setRainName(string rainName)
        {
            this.rainName = rainName;
        }
        public string getRainName()
        {
            return rainName;
        }

        public void setRainWeight(float rainWeight)
        {
            this.rainWeight = rainWeight;
        }
        public float getRainWeight()
        {
            return rainWeight;
        }

        public void setIdLocation(string idLocation)
        {
            this.idLocation = idLocation;
        }
        public string getIdLocation()
        {
            return idLocation;
        }

        public void setN(int n)
        {
            this.n = n;
        }
        public int getN()
        {
            return n;
        }
    }

    public class RainCodePOJO
    {
        public RainCode[][][] getRainOff(Xaj xaj)
        {
            BasForeCastData bfcd;
            BasForeCastDataPOJO bfcdpojo = new BasForeCastDataPOJO();
            bfcd = bfcdpojo.getFloodNo(xaj);

            FloodNo[] fn;
            FloodNoPOJO fnpojo = new FloodNoPOJO();
            fn = fnpojo.getUnitCode(xaj);

            UnitCode[][] uc;
            UnitCodePOJO ucpojo = new UnitCodePOJO();
            uc = ucpojo.getRainCode(xaj);

            RainCode[][][] rc;
            rc = new RainCode[bfcd.floodNo.Length][][];

            CommonFun cf = new CommonFun();
            int a, b, c;
            for (a = 0; a < bfcd.floodNo.Length; a++)  //循环场次洪水
            {

                rc[a] = new RainCode[fn[a].unitCode.Length][];

                for (b = 0; b < fn[a].unitCode.Length; b++)  //单元区域
                {

                    rc[a][b] = new RainCode[uc[a][b].rainCode.Length];

                    for (c = 0; c < uc[a][b].rainCode.Length; c++)
                    {
                        //循环每个预报单元的雨量站
                        rc[a][b][c] = new RainCode();
                        // 提取时段雨量

                        List<DateTime> p_step_dt = new List<DateTime>(xaj.Step_P.Keys);
                        List<DateTime> p_day_dt = new List<DateTime>(xaj.Day_P.Keys);

                        rc[a][b][c].setStepRainNum(p_step_dt.Count);
                        rc[a][b][c].setDayNum(p_day_dt.Count);

                        for (int d = 0; d < p_step_dt.Count; d++)
                        {
                            string s_time = p_step_dt[d].ToString("yyyy-MM-dd HH:mm:ss");
                            Date date = String_Date.StringToDate(s_time);
                            rc[a][b][c].p_step_dt[d] = date;
                            rc[a][b][c].p_step[d] = (float)xaj.Step_P[p_step_dt[d]];

                        }

                        for (int i = 0; i < p_day_dt.Count; i++)
                        {
                            string s_time = p_day_dt[i].ToString("yyyy-MM-dd HH:mm:ss");
                            Date date = String_Date.StringToDate(s_time);
                            rc[a][b][c].p_day_dt[i] = date;
                            rc[a][b][c].p_day[i] = (float)xaj.Day_P[p_day_dt[i]];

                        }

                    }

                }

            }
            return rc;
        }
    }

    public class EvapCode
    {
        public string evapCode;
        public Date[] dt;
        public float[] em;
        public Date[] dt_ave;
        public float[] em_ave;

        public void setNum(int n)
        {
            dt = new Date[n];
            em = new float[n];
        }

        public void setAveNum(int n)
        {
            dt_ave = new Date[n];
            em_ave = new float[n];
        }

        public void setEvapCode(string evapCode)
        {
            this.evapCode = evapCode;
        }
        public string getEvapCode()
        {
            return evapCode;
        }

        public void setDt(Date[] dt)
        {
            this.dt = dt;
        }
        public Date[] getDt()
        {
            return dt;
        }

        public void setEm(float[] em)
        {
            this.em = em;
        }
        public float[] getEm()
        {
            return em;
        }
    }

    public class EvapCodePOJO
    {
        public EvapCode[] getEvap(Xaj xaj)
        {
            Vector vec1;
            EvapCode[] ec;
            BasForeCastData bfcd;

            BasForeCastDataPOJO bfcdpojo = new BasForeCastDataPOJO();
            bfcd = bfcdpojo.getFloodNo(xaj);

            List<DateTime> day_em_dt = new List<DateTime>(xaj.Day_em.Keys);

            Date[] dt = new Date[day_em_dt.Count];
            for (int i = 0; i < day_em_dt.Count; i++)
            {
                string s_time = day_em_dt[i].ToString("yyyy-MM-dd HH:mm:ss");
                Date date = String_Date.StringToDate(s_time);
                dt[i] = date;
            }
            float[] em = new float[day_em_dt.Count];

            for (int i = 0; i < day_em_dt.Count; i++)
            {
                em[i] = (float)xaj.Day_em[day_em_dt[i]];//**********
            }

            bfcd.setEvapNum(1);
            ec = new EvapCode[1];

            DayToAve dta = new DayToAve();

            for (int i = 0; i < bfcd.evapCode.Length; i++)
            {
                ec[i] = new EvapCode();
                ec[i].setNum(day_em_dt.Count);
                ec[i].dt = dt;
                ec[i].em = em;

                vec1 = dta.dayToAve(em, dt);
                ec[i].dt_ave = (Date[])vec1.get(0);
                ec[i].em_ave = (float[])vec1.get(1);
            }
            return ec;
        }
    }

    public class FloodNo
    {
        public string floodno;//洪号
        public Date rainbeg;
        public Date raindt;
        public float sum_p;
        public Date flowdt;
        public float flowmax;
        public float R;
        public float qrainbeg;
        public float[] Q;
        public float[] Q1;
        public Date[] dt_Q;
        public Date[] dt_Q1;
        public float[] Q2;
        public Date[] dt_Q2;

        public UnitCode[] unitCode;

        public void setNum(int n)
        {
            unitCode = new UnitCode[n];
        }

        public void setQ(int n)
        {
            Q = new float[n];
            dt_Q = new Date[n];
            Q1 = new float[n];
            dt_Q1 = new Date[n];
            Q2 = new float[n];
            dt_Q2 = new Date[n];

        }
        public void setFloodno(string floodno)
        {
            floodno = this.floodno;
        }
        public string getFloodNo()
        {
            return this.floodno;
        }

        public void setRainBeg(Date rainbeg)
        {
            rainbeg = this.rainbeg;
        }
        public Date getRainBeg()
        {
            return this.rainbeg;
        }

        public void setRainDt(Date raindt)
        {
            raindt = this.raindt;
        }
        public Date getRainDt()
        {
            return this.raindt;
        }

        public void setSum_p(float sum_p)
        {
            sum_p = this.sum_p;
        }
        public float getSum_p()
        {
            return this.sum_p;
        }

        public void setFlowDt(Date flowdt)
        {
            flowdt = this.flowdt;
        }
        public Date getFlowDt()
        {
            return this.flowdt;
        }

        public void setFlowMax(float flowmax)
        {
            flowmax = this.flowmax;
        }
        public float getFlowMax()
        {
            return this.flowmax;
        }

        public void setR(float R)
        {
            R = this.R;
        }
        public float getR()
        {
            return this.R;
        }

        public void QrainBeg(float qrainbeg)
        {
            qrainbeg = this.qrainbeg;
        }
        public float getQrainBeg()
        {
            return this.qrainbeg;
        }
    }

    public class FloodNoPOJO
    {
        public FloodNo[] getUnitCode(Xaj xaj)
        {
            BasForeCastData bfcd;
            BasForeCastDataPOJO bfcdpojo = new BasForeCastDataPOJO();
            bfcd = bfcdpojo.getFloodNo(xaj);
            FloodNo[] fn = new FloodNo[bfcd.floodNo.Length];

            for (int a = 0; a < bfcd.floodNo.Length; a++)
            {
                string[] unitCode = new string[] { "uces01" };
                float[] unitWeight = new float[] { 1 };

                fn[a] = new FloodNo();
                fn[a].setNum(unitCode.Length);

                for (int i = 0; i < fn[a].unitCode.Length; i++)
                {
                    fn[a].unitCode[i] = new UnitCode();
                    fn[a].unitCode[i].unitCode = unitCode[i];
                    fn[a].unitCode[i].unitWeight = unitWeight[i];
                }
            }
            return fn;
        }
    }

    public class UnitCode
    {
        public string unitCode;//单元代吗
        public string unitName;//单元名称（冰峪沟单元）
        public float unitWeight;//单元权重
        public float routWay;//演进方法代码
        public float routTime;//传播时间
        public RainCode[] rainCode;

        public void setNum(int n)
        {
            rainCode = new RainCode[n];
        }

        public void setUnitCode(string unitCode)
        {
            unitCode = this.unitCode;
        }
        public string getUnitCode()
        {
            return this.unitCode;
        }

        public void setUnitName(string unitName)
        {
            unitName = this.unitName;
        }
        public string getUnitName()
        {
            return this.unitName;
        }

        public void setUnitWeight(float unitWeight)
        {
            unitWeight = this.unitWeight;
        }
        public float getUnitWeight()
        {
            return this.unitWeight;
        }

        public void setRoutWay(float routWay)
        {
            routWay = this.routWay;
        }
        public float getRoutWay()
        {
            return this.routWay;
        }
        public void setRoutTime(float routTime)
        {
            routTime = this.routTime;
        }
        public float getRoutTime()
        {
            return this.routTime;
        }
    }

    public class UnitCodePOJO
    {
        public UnitCode[][] getRainCode(Xaj xaj)
        {
            BasForeCastData bfcd;
            BasForeCastDataPOJO bfcdpojo = new BasForeCastDataPOJO();
            bfcd = bfcdpojo.getFloodNo(xaj);

            FloodNo[] fn;
            FloodNoPOJO fnpojo = new FloodNoPOJO();
            fn = fnpojo.getUnitCode(xaj);

            UnitCode[][] uc = new UnitCode[bfcd.floodNo.Length][];

            for (int a = 0; a < bfcd.floodNo.Length; a++)  //循环场次洪水
            {

                uc[a] = new UnitCode[fn[a].unitCode.Length];

                for (int b = 0; b < fn[a].unitCode.Length; b++)  //循环单元
                {
                    string[] rainCode = new string[] { "rainces001" };  //面雨量，看做只有一个雨量站
                    float[] rainWeight = new float[] { 1 };  //权重为1
                    string[] rainName = new string[rainCode.Length];
                    uc[a][b] = new UnitCode();
                    uc[a][b].setNum(rainCode.Length);

                    for (int i = 0; i < uc[a][b].rainCode.Length; i++)  //循环雨量站
                    {

                        Vector v;
                        uc[a][b].rainCode[i] = new RainCode();
                        uc[a][b].rainCode[i].rainCode = rainCode[i];
                        uc[a][b].rainCode[i].rainWeight = rainWeight[i];
                        uc[a][b].rainCode[i].n = rainCode.Length;
                    }
                }
            }
            return uc;
        }
    }

    public class FloodForecast
    {
        public static BasForeCastData bfcd;
        public static FloodNo[] fn;
        public static UnitCode[][] uc;
        public static RainCode[][][] rc;
        public static EvapCode[] ec;

        public void getBaseData(Xaj xaj)
        {

            BasForeCastDataPOJO bfcdpojo = new BasForeCastDataPOJO();
            bfcd = bfcdpojo.getFloodNo(xaj);

            FloodNoPOJO fnpojo = new FloodNoPOJO();
            fn = fnpojo.getUnitCode(xaj);

            UnitCodePOJO ucpojo = new UnitCodePOJO();
            uc = ucpojo.getRainCode(xaj);

            RainCodePOJO rcpojo = new RainCodePOJO();
            rc = rcpojo.getRainOff(xaj);		//时段降雨过程和日降雨过程

            EvapCodePOJO ecpojo = new EvapCodePOJO();
            ec = ecpojo.getEvap(xaj);           //日蒸发以及时段蒸发

        }

        // 洪水预报
        public static Dictionary<DateTime, double> Forecast(Xaj xaj)
        {
            Dictionary<DateTime, double> resultdic = new Dictionary<DateTime, double>();

            Console.WriteLine("新安江模型开始运行......");

            FloodForecast bfd = new FloodForecast();
            bfd.getBaseData(xaj);

            DataLinkToXaj dltx = new DataLinkToXaj(xaj.Catchment_Area);//*************************************
            XajParam xajParam = new XajParam(xaj.XajPar.Sm, xaj.XajPar.Wm, xaj.XajPar.Um, xaj.XajPar.Lm, xaj.XajPar.B, xaj.XajPar.Im, xaj.XajPar.K, xaj.XajPar.C, xaj.XajPar.Ki, xaj.XajPar.Ex, xaj.XajPar.Ci, xaj.XajPar.Cg, xaj.XajPar.Cs, xaj.XajPar.L, xaj.XajPar.Cr);

            Vector vec;
            float[][] oneSessionTotalFlood;
            float[] para = new float[15];
            para[0] = xajParam.Sm; para[1] = xajParam.Wm; para[2] = xajParam.Um;
            para[3] = xajParam.Lm; para[4] = xajParam.B; para[5] = xajParam.Im;
            para[6] = xajParam.K; para[7] = xajParam.C; para[8] = xajParam.Ki;
            para[9] = xajParam.Ex; para[10] = xajParam.Ci; para[11] = xajParam.Cg;
            para[12] = xajParam.Cs; para[13] = xajParam.L;
            para[14] = xajParam.Cr;

            float ua0_day = (float)xaj.XajInitial.ua_day;// /上层土壤初始含水量
            float la0_day = (float)xaj.XajInitial.la_day;// /下层土壤初始含水量
            float da0_day = (float)xaj.XajInitial.da_day;// /深层土壤初始含水量
            float baseFlow = (float)xaj.XajInitial.baseFlow;//基流
            vec = dltx.getOneFitness(bfcd, fn, uc, rc, ec, para, dltx, ua0_day, la0_day, da0_day, baseFlow);
            oneSessionTotalFlood = (float[][])vec.get(0);
            Vector flood_dt = (Vector)vec.get(1);

            /**************转换洪水过程数据************/
            for (int k = 0; k < bfcd.floodNo.Length; k++)
            {
                for (int m = 0; m < oneSessionTotalFlood[k].Length; m++)
                {
                    string date = String_Date.DateToString((Date)flood_dt.get(m));
                    resultdic.Add(DateTime.Parse(date), oneSessionTotalFlood[k][m]);
                }
            }
            /******************************************/

            return resultdic;
        }



    }

    class DataLinkToXaj
    {
        public float area;

        public DataLinkToXaj(double area)
        {
            this.area = (float)area;
        }

        public Vector xajParaTran(BasForeCastData bfcd, FloodNo[] fn, UnitCode[][] uc,
                RainCode[][][] rc, EvapCode[] ec, float[] para, float ua0_day, float la0_day, float da0_day, float baseFlow)
        {
            // 该方法运行太慢
            Vector vec = new Vector();
            Vector vec0, vec1, vec2, vec3, vec4, vec5;
            float[][] oneSessionTotalFlood = new float[bfcd.floodNo.Length][];
            float[][] oneSessionTotalR = new float[bfcd.floodNo.Length][];
            float[][] oneSessionTotalQ = new float[bfcd.floodNo.Length][];
            float[] W00 = new float[bfcd.floodNo.Length];

            CommonFun cf = new CommonFun();
            GetEm ge = new GetEm();
            Vector flood_dt = new Vector();
            for (int i = 0; i < bfcd.floodNo.Length; i++)  //循环不同场次洪水
            {
                //System.out.println("**** "+i);
                float[] unitWeighth = new float[fn[i].unitCode.Length];
                float[][] oneUnitFlood = new float[fn[i].unitCode.Length][];
                float[][] oneUnitR = new float[fn[i].unitCode.Length][];
                float[] w0 = new float[fn[i].unitCode.Length];
                float[] rainWeight;
                float[] step_em = { 0f };


                float[][] day_p;
                float[][] step_p;

                float[][] aveDay_p = new float[fn[i].unitCode.Length][];
                float[][] aveStep_p = new float[fn[i].unitCode.Length][];

                vec1 = ge.getDay_em(ec[0].em, ec[0].dt, String_Date.DateToString(ec[0].dt[0]),
                        String_Date.DateToString(ec[0].dt[ec[0].dt.Length - 1]));  // 取得日蒸发数据     //2016-11-03写：前期日蒸发直接给定的，选取开始日期和结束日期截取日蒸发
                Date[] day_dt = (Date[])vec1.get(0);
                float[] day_em = (float[])vec1.get(1);


                for (int j = 0; j < fn[i].unitCode.Length; j++)
                {//循环不同单元,
                    unitWeighth[j] = fn[i].unitCode[j].unitWeight;
                    day_p = new float[uc[i][j].rainCode.Length][];
                    step_p = new float[uc[i][j].rainCode.Length][];
                    rainWeight = new float[uc[i][j].rainCode.Length];
                    float areaUnit = fn[i].unitCode[j].unitWeight * area;   ///原来是692，改为260
                    for (int k = 0; k < uc[i][j].rainCode.Length; k++)
                    {   //循环每个预报单元里面的雨量站	
                        rainWeight[k] = uc[i][j].rainCode[k].rainWeight;
                        step_p[k] = new float[rc[i][j][0].p_step.Length];
                        day_p[k] = rc[i][j][k].p_day;  //每一场次每个单元中每个雨量站的日降雨
                        for (int a = 0; a < rc[i][j][k].p_step.Length; a++)
                        {
                            if (a < rc[i][j][k].p_step.Length)
                            {
                                step_p[k][a] = rc[i][j][k].p_step[a];
                            }
                            else
                            {
                                step_p[k][a] = 0;
                            }
                        }
                    }
                    vec4 = XajModel.getAveValue(day_p, rainWeight);
                    aveDay_p[j] = (float[])vec4.get(0);  // 得到平均日降雨

                    vec5 = XajModel.getAveValue(step_p, rainWeight);///时段雨量				
                    aveStep_p[j] = (float[])vec5.get(0);///时段雨量

                    vec0 = XajModel.getOneUnitFloodArray(aveDay_p[j], day_em, aveStep_p[j],
                             step_em, areaUnit, para, fn[i].unitCode.Length, unitWeighth, ua0_day, la0_day, da0_day, baseFlow, String_Date.DateToString(bfcd.floodNo[i].rainbeg));

                    oneUnitFlood[j] = (float[])vec0.get(0);    // 每一个雨量站子单元的演进4  
                    flood_dt = (Vector)vec0.get(1);


                }

                vec3 = XajModel.getOneSessionTotalValue(oneUnitFlood, fn[i].unitCode.Length,
                         unitWeighth);
                oneSessionTotalFlood[i] = (float[])vec3.get(0);


            }  // 场次循环结束

            vec.add(oneSessionTotalFlood);  // 0

            vec.add(flood_dt);
            return vec;

        }

        public Vector getOneFitness(BasForeCastData bfcd, FloodNo[] fn, UnitCode[][] uc,
                RainCode[][][] rc, EvapCode[] ec, float[] par, DataLinkToXaj dltx, float ua0_day, float la0_day, float da0_day, float baseFlow)
        {

            Vector vec = new Vector();
            Vector vec0;

            float fitness = 0;


            vec0 = dltx.xajParaTran(bfcd, fn, uc, rc, ec, par, ua0_day, la0_day, da0_day, baseFlow);
            float[][] oneSessionTotalFlood = (float[][])vec0.get(0);
            Vector flood_dt = (Vector)vec0.get(1);

            float[] totalFlow0 = new float[bfcd.floodNo.Length];///真实流量总和
            float[] totalFlowf = new float[bfcd.floodNo.Length];///模拟流量总和
            float[] totalRf = new float[bfcd.floodNo.Length];
            float[] absR = new float[bfcd.floodNo.Length];   ///R绝对误差
            float[] relaR = new float[bfcd.floodNo.Length];  ///R相对误差
            float[] R0 = new float[bfcd.floodNo.Length];///实测R
            float[] Rf = new float[bfcd.floodNo.Length];///预报R

            float[] passNum = new float[bfcd.floodNo.Length];
            float totalPassNum = 0;
            float[] sum_p = new float[bfcd.floodNo.Length];///降雨量
            float[] aerfa0 = new float[bfcd.floodNo.Length];/// 实测产流系数 aerfa0
            float[] aerfaf = new float[bfcd.floodNo.Length];
            float[] maxQ0 = new float[bfcd.floodNo.Length];
            float[] absMaxQ = new float[bfcd.floodNo.Length];
            float[] relaMaxQ = new float[bfcd.floodNo.Length];

            int[] numMaxQ0 = new int[bfcd.floodNo.Length];
            Date[] dateMaxQ0 = new Date[bfcd.floodNo.Length];
            int[] absNumMaxQ = new int[bfcd.floodNo.Length];///峰现绝对误差


            float[] nash = new float[bfcd.floodNo.Length];///确定性系数
            float[] sum_a1 = new float[bfcd.floodNo.Length];///用来计算确定性系数
            float[] sum_b1 = new float[bfcd.floodNo.Length];///用来计算确定性系数

            float[] maxQf = new float[bfcd.floodNo.Length];   ///预报流量峰值，guokelun


            fitness = totalPassNum;
            vec.addElement(oneSessionTotalFlood);  // 0

            vec.add(flood_dt);
            return vec;

        }
    }

    public class DayRainOff
    {
        public Date dt;
        public float p;
        public float em;

        public void setDt(Date dt)
        {
            this.dt = dt;
        }
        public Date getDt()
        {
            return dt;
        }

        public void setP(float p)
        {
            this.p = p;
        }
        public float getP()
        {
            return p;
        }

        public void setEm(float em)
        {
            this.em = em;
        }
        public float getEm()
        {
            return em;
        }
    }

    class DayToAve
    {
        public Vector dayToAve(float[] day_em, Date[] day_dt)
        {
            int hour_number = ((int)(24 / Model_Const.XAJ_TIMESTEP_HOUR));
            Vector vec = new Vector();
            CommonFun cf = new CommonFun();
            Calendar cal = Calendar.getInstance();
            int n = day_em.Length * hour_number; ///步长为1小时*********************************************************
            float[] ave_em = new float[n];
            Date[] ave_dt = new Date[n];
            int i;
            int j;
            for (i = 0; i < day_em.Length; i++)
            {
                cal.setTime(day_dt[i]);
                /***********************时段为1小时*****************************/
                for (j = 0; j < hour_number; j++)
                {
                    ave_em[i * hour_number + j] = day_em[i] / hour_number;
                    ave_dt[i * hour_number + j] = CommonFun.getDT(day_dt[i], cal.get(Calendar.MONTH), cal.get(Calendar.DAY_OF_MONTH), j);
                }
                /***********************时段为1小时*****************************/
            }
            vec.addElement(ave_dt);
            vec.addElement(ave_em);
            return vec;
        }
    }

    public class GetEm
    {
        public Vector getDay_em(float[] em, Date[] dt, string emBeg, string emEnd)
        {
            //每一场的蒸发数据提取情况
            Vector vec = new Vector();
            CommonFun cf = new CommonFun();
            float[] day_em;
            Date[] day_dt;
            string[] date = new string[dt.Length];
            int numBeg = 0;
            int numEnd = 0;
            for (int i = 0; i < dt.Length; i++)
            {
                date[i] = String_Date.DateToString(dt[i]);
                if (date[i].Equals(emBeg))
                {
                    numBeg = i;
                }
                if (date[i].Equals(emEnd))
                {
                    numEnd = i;
                }
            }

            int temp = numEnd - numBeg + 1;
            day_dt = new Date[temp];
            day_em = new float[temp];
            for (int j = 0; j < temp; j++)
            {
                day_dt[j] = dt[numBeg + j];
                day_em[j] = em[numBeg + j];
            }

            vec.addElement(day_dt);
            vec.addElement(day_em);
            return vec;
        }

        public Vector getStep_em(float[] ave_em, Date[] dt, string step_emBeg, string step_emEnd)
        {
            Vector vec = new Vector();
            float[] step_em;
            Date[] step_dt;
            string[] date = new string[dt.Length];
            int numBeg = 0;
            int numEnd = 0;

            int i;
            for (i = 0; i < dt.Length; i++)
            {
                date[i] = String_Date.DateToString(dt[i]);
                if (date[i].Equals(step_emBeg))
                {
                    numBeg = i;
                }

                if (date[i].Equals(step_emEnd))
                {
                    numEnd = i;
                }
            }
            int temp = numEnd - numBeg + 1;
            step_dt = new Date[temp];
            step_em = new float[temp];

            for (int j = 0; j < temp; j++)
            {
                step_dt[j] = dt[numBeg + j];
                step_em[j] = ave_em[numBeg + j];
            }
            vec.addElement(step_dt);
            vec.addElement(step_em);
            return vec;
        }
    }

    class XajParam
    {

        public float Sm;    //张力水 蓄水容量
        public float Wm;
        public float Um;    //上层蓄水容量 
        public float Lm;    //下层蓄水容量  
        public float B;   //张力水蓄水容量曲线的方次
        public float Im;  //不透水的面积比例      0.01~0.02
        public float K;   //蒸发能力折算系数
        public float C;    //深层蒸发系数
        public float Ki;  //自由水蓄水水库对壤中流的出流系数
        public float Ex;  //表土自由水蓄水容量曲线的方次
        public float Ci;  //壤中流消退系数
        public float Cg;  //地下径流消退系数                          值越小  峰值越大   对退水的影响较小
        public float Cs;   //地表径流消退系数                   降低洪量比较敏感  值越大 消退越慢 
        public float L;      // 滞后时段数,增大使峰值向后，并有降低洪峰的作用。
        public float Cr;   //河网蓄水消退系数


        public XajParam(double Sm, double Wm, double Um, double Lm, double B, double Im, double K, double C, double Ki, double Ex, double Ci, double Cg, double Cs, double L, double Cr)
        {
            this.Sm = (float)Sm;
            this.Wm = (float)Wm;
            this.Um = (float)Um;
            this.Lm = (float)Lm;
            this.B = (float)B;
            this.Im = (float)Im;
            this.K = (float)K;
            this.C = (float)C;
            this.Ki = (float)Ki;
            this.Ex = (float)Ex;
            this.Ci = (float)Ci;
            this.Cg = (float)Cg;
            this.Cs = (float)Cs;
            this.L = (float)L;
            this.Cr = (float)Cr;
        }


    }

    public class XajModel
    {
        public static Vector getOneUnitFloodArray(float[] day_p, float[] day_em,
            float[] step_p, float[] ave_em, float area, float[] para,
            int numUnit, float[] weighth, float ua0_day, float la0_day,
            float da0_day, float baseFlow, string rainBeg)
        {
            // 每场洪水每场预报单元的模拟方法 ★★★★☆☆☆☆☆☆☆☆☆☆☆☆☆☆★★★★
            Vector vec = new Vector();

            int hour_number = ((int)(24 / Model_Const.XAJ_TIMESTEP_HOUR));
            int length1;
            int deday = 0;  // 不知道是什么意思guokelun
            int i_step = 1; //时段步长
            int i_max_len = deday * hour_number / i_step;  //不知道是什么意思guokelun
            Vector flood = new Vector();

            /**************** 新安江参数赋值 *****************/
            float sm = para[0];
            float WM = para[1];
            float UM = para[2];
            float LM = para[3];
            float b = para[4];
            float im = para[5];
            float k = para[6];
            float c = para[7];
            float ki = para[8];
            float ex = para[9];
            float ci = para[10];
            float cg = para[11];
            float cs = para[12];
            float L = para[13];
            float cr = para[14];
            /**************** 新安江参数赋值 *****************/
            length1 = day_p.Length;// /日降雨历时长度

            /********** 日蒸发计算土壤含水量初始值 ***********/
            float[] R_day = new float[length1];// 径流历时长度等于日降雨历时长度
            float[] ua_day = new float[length1 + 1];  //上层土壤含水量
            float[] la_day = new float[length1 + 1];  //上层土壤含水量
            float[] da_day = new float[length1 + 1];  //上层土壤含水量
            XajFunction.runoff_xaj3_model_day(WM, UM, LM, im, b, k, c, length1,
                    day_p, day_em, ua0_day, la0_day, da0_day, R_day, ua_day,
                    la_day, da_day); // *****************************************1
            /********** 日蒸发计算土壤含水量初始值 ***********/
            float[] sum = new float[day_p.Length + 1];
            float ua0 = ua_day[length1];
            float la0 = la_day[length1];
            float da0 = da_day[length1]; //
            float W0 = (ua0 + la0 + da0);

            float fr0 = 0;
            float s0 = 0;

            int length = step_p.Length;
            Vector flood_dt = new Vector();
            flood = XajFunction.zjb_flow_xaj3_net(UM, LM, WM, im, b, k, c, step_p, ave_em,
                    ua0, la0, da0, ex, sm, ki, i_max_len, fr0, s0, cs, ci, cg,
                    deday, i_step, area, length, baseFlow, rainBeg, flood, flood_dt);   //产流、分水源、以及坡地汇流计算
            /****************** 河网汇流计算 *********************/
            float[] dq = new float[flood.size()];
            dq = XajFunction.Zhihourouting(flood, cr, L);
            /****************** 河网汇流计算 *********************/

            vec.add(dq); // 4 该单元的产汇流结果
            vec.add(flood_dt);
            return vec;
        }

        public static Vector getOneSessionTotalValue(float[][] oneUnitFlood,
                int numUnit, float[] weighth)
        {

            Vector vec = new Vector();
            Vector vec_Flood = new Vector();


            for (int i = 0; i < oneUnitFlood[0].Length; i++)
            { // 循环场次洪水过程的不同时刻
                float Flood = 0;
                for (int j = 0; j < numUnit; j++)
                { // 循环不同区域
                    Flood += oneUnitFlood[j][i];
                }
                vec_Flood.add(Flood);
            }
            float[] allUnitFlood = new float[vec_Flood.size()];
            for (int i = 0; i < vec_Flood.size(); i++)
            {
                allUnitFlood[i] = (float)vec_Flood.get(i);
            }
            vec.add(allUnitFlood);// /0 一场洪水的洪水过程
            return vec;
        }

        //计算雨量的加权平均值值***这个函数对于本系统没有用，因为本系统直接用的面雨量，但是删掉的话需要改动程序，所以就保留了下来 guokelun
        public static Vector getAveValue(float[][] value, float[] rainWeight)
        {
            Vector vec = new Vector();
            float[] aveValue = new float[value[0].Length];
            for (int i = 0; i < value[0].Length; i++)
            {
                for (int j = 0; j < rainWeight.Length; j++)
                {
                    aveValue[i] += value[j][i] * rainWeight[j];
                }
            }
            vec.add(aveValue);
            return vec;
        }
    }

    class XajFunction
    {

        /**
         * 蒸发日计算模型
         * 
         * "param WM
         * "param UM
         * "param LM
         * "param im
         * "param b
         * "param k
         * "param c
         * "param length1
         *            日模型长度
         * "param P_day
         *            日降雨
         * "param em_day
         *            日蒸发
         * "param ua0_day
         * "param la0_day
         * "param da0_day
         * "param xajParam
         * "param R_day
         *            日径流量
         * "param ua_day
         * "param la_day
         * "param da_day
         * "author ZhangJiabin
         */
        public static void runoff_xaj3_model_day(float WM, float UM, float LM,
                float im,
                float b,
                float k,// **********************************1
                float c, int length1, float[] P_day, float[] em_day, float ua0_day,
                float la0_day, float da0_day, float[] R_day, float[] ua_day,
                float[] la_day, float[] da_day)
        {
            float PE;
            float DM;
            float[] EU = new float[length1];// 蒸发
            float[] EL = new float[length1];
            float[] ED = new float[length1];
            float wmt, MM, A, EP;// t时刻的土壤平均含水量//点蓄水容量的最大值（7-1）
            // wmt所对应的流域蓄水容量曲线的纵坐标、蒸发
            ua_day[0] = ua0_day;
            la_day[0] = la0_day;
            da_day[0] = da0_day;// 每一层的初始含水量

            DM = WM - (UM + LM);
            if (DM < 0)
            {

            }
            else
            {

                MM = WM * (1.0f + b) / (1.0f - im);// 点蓄水容量的最大值（7-1）

                for (int i = 0; i < length1; i++)
                {
                    wmt = ua_day[i] + la_day[i] + da_day[i]; // 当前时段土壤含水量
                    if (wmt > WM)
                        wmt = WM;
                    EP = k * em_day[i];
                    PE = P_day[i] - EP;

                    // 以下计算径流和
                    if (PE >= 0)
                    {
                        float temp = new java.lang.Double(java.lang.Math.pow(1 - wmt / WM,
                                1.0 / (b + 1))).floatValue();// （3-6）
                        A = MM * (1 - temp);
                        if (PE + A < MM)// （3-7）
                        {
                            temp = new java.lang.Double(java.lang.Math.pow(1 - (PE + A) / MM, b + 1))
                                    .floatValue();
                            R_day[i] = PE - WM + wmt + WM * temp;
                        }
                        else
                        {
                            R_day[i] = PE - WM + wmt;
                        }
                        // 以下计算蒸散发，按照水文教材p156流程图
                        EU[i] = k * em_day[i];// 上层按蒸散发能力蒸发
                        EL[i] = 0;
                        ED[i] = 0;
                        if (ua_day[i] + PE - R_day[i] < UM)// 如果上层土壤含水量没有饱和
                        {
                            ua_day[i + 1] = ua_day[i] + PE - R_day[i];
                            la_day[i + 1] = la_day[i];
                            da_day[i + 1] = da_day[i];
                        }
                        else
                        { // 如果上层土壤含水量已经饱和

                            if (ua_day[i] + la_day[i] + PE - R_day[i] - UM > LM)// 如果第二层土壤含水量已经饱和
                            {
                                ua_day[i + 1] = UM;
                                la_day[i + 1] = LM;
                                da_day[i + 1] = wmt + PE - R_day[i] - ua_day[i + 1]
                                        - la_day[i + 1];
                            }
                            else// 如果第二层土壤含水量没有饱和
                            {
                                ua_day[i + 1] = UM;
                                la_day[i + 1] = ua_day[i] + la_day[i] + PE
                                        - R_day[i] - UM;
                                da_day[i + 1] = da_day[i];
                            }
                        }
                    }
                    // 以下计算蒸发量PE<0
                    else if (PE < 0)
                    {
                        R_day[i] = 0;
                        if (ua_day[i] + PE > 0)// 上层没有被蒸发完
                        {
                            EU[i] = EP;// 上层按蒸散发能力蒸发
                            EL[i] = 0;
                            ED[i] = 0;
                            ua_day[i + 1] = ua_day[i] + PE;
                            la_day[i + 1] = la_day[i];
                            da_day[i + 1] = da_day[i];
                        }
                        else// 上层已经被蒸发完
                        {
                            EU[i] = ua_day[i] + P_day[i];
                            ua_day[i + 1] = 0;
                            if (la_day[i] > c * LM)
                            {
                                EL[i] = (k * em_day[i] - EU[i]) * la_day[i] / LM;
                                la_day[i + 1] = la_day[i] - EL[i];
                                da_day[i + 1] = da_day[i];
                                ED[i] = 0;
                            }
                            else
                            {
                                if (la_day[i] > c * (k * em_day[i] - EU[i]))
                                {
                                    EL[i] = c * (k * em_day[i] - EU[i]);
                                    ED[i] = 0;
                                    la_day[i + 1] = la_day[i] - EL[i];
                                    da_day[i + 1] = da_day[i];
                                }
                                else
                                {
                                    EL[i] = la_day[i];
                                    la_day[i + 1] = 0;
                                    ED[i] = c * (k * em_day[i] - EU[i]) - EL[i];
                                    da_day[i + 1] = da_day[i] - ED[i];
                                }
                            }
                        }
                    }
                    // 约束最小值
                    if (ua_day[i + 1] < 0)
                        ua_day[i + 1] = 0;
                    if (la_day[i + 1] < 0)
                        la_day[i + 1] = 0;
                    if (da_day[i + 1] < 0)
                        da_day[i + 1] = 0;

                    if (ua_day[i + 1] > UM)
                        ua_day[i + 1] = UM;
                    if (la_day[i + 1] > LM)
                        la_day[i + 1] = LM;
                    if (da_day[i + 1] > DM)
                        da_day[i + 1] = DM;
                }
            }

        }
        /**
         * 坡地汇流计算模型
         * 
         * "param cs
         *            地表（河网蓄水量）消退系数
         * "param ci
         *            壤中流消退系数
         * "param cg
         *            地下径流消退系数
         * "param deday
         * "param i_step
         * "param i_periods
         * "param i_floods
         * "param area
         * "param RG
         * "param RI
         * "param RS
         * "param xajParam
         * "param flood
         * "return flood
         * "author ZhangJiabin
         * "throws ParseException
         */

        public static Vector zjb_flow_xaj3_net(float UM, float LM, float WM,
                float im, float b, float k, float c, float[] P, float[] em,
                float ua0,
                float la0,
                float da0, // *****************************2
                float ex, float sm, float ki, int i_max_len,
                float fr0,
                float s0, // **************************3
                float cs, float ci, float cg, int deday, int i_step, float area,
                int length, float baseFlow, string rainBeg, Vector flood,
                Vector flood_dt)
        { // ************************4

            /****************************************************************************************/
            //
            /*** 2 */
            Vector ua = new Vector();
            Vector la = new Vector();
            Vector da = new Vector();

            Vector RG = new Vector();
            Vector RI = new Vector();
            Vector RS = new Vector();

            float r = 0;
            Vector R = new Vector();

            float _fr;
            Vector fr = new Vector();
            float _s;
            Vector s = new Vector();

            Vector P1 = new Vector();
            for (int pp = 0; pp < P.Length; pp++)
            {
                P1.add(P[pp]);
                // System.out.println((float)P1.get(pp));
            }
            float _em;

            /****************** 洪水开始时间 **********************/

            //SimpleDateFormat simpledateformat = new SimpleDateFormat(
            //        "yyyy-MM-dd HH:mm:ss");
            Date rain_Beg = new Date();
            try
            {
                rain_Beg = String_Date.StringToDate(rainBeg);
            }
            catch (java.lang.Exception e)
            {
                // TODO 自动生成的 catch 块
                e.printStackTrace();
            }
            Calendar flood_date = Calendar.getInstance();
            flood_date.setTime(rain_Beg);

            float PE;// 降雨量和蒸发量的差值。
            float DM;// 深层土壤蓄水容量。
            float EU;// 上层蒸发量
            float EL;// 下层蒸发量
            float ED;// 深层蒸发量
            float wmt, MM, A, EP;// t时刻的土壤平均含水量//点蓄水容量的最大值（7-1）
            // wmt所对应的流域蓄水容量曲线的纵坐标、流域蒸散发能力
            ua.add(ua0);
            la.add(la0);
            da.add(da0);// 每一层的初始含水量
            // 从三水源模型结构变量中取得模型参数
            // 产流量计算模型参数
            DM = WM - (UM + LM);
            /** 2 **/
            /** 3 **/
            float kg; // 深层地下水出流系数
            // float PE;
            float SMM, AU, X;// 全流域最大点的蓄水容量SMM=MS、、前一时段的产流面积
            float SS, Q, GD, ID;// 单位面积径流量、///==KI/KG
            int d, G;
            float KID, KGD; // /KID=书中KI△t;KGD=书中KG△t，即时段转换后的KI、KG
            kg = 0.7f - ki;
            d = 24 / i_step; // 一天分的时段数
            float temp1 = new java.lang.Double(java.lang.Math.pow(1 - (kg + ki), 1.0 / d)).floatValue();
            KID = (1.0f - temp1) / (1.0f + kg / ki);// kssd-和kss对应的参数 ///公式（5-33）
            // 计算步长内流域自由水蓄水水库的壤中流的出流系数
            KGD = KID * kg / ki;// 和kg对应的参数 /计算步长内流域自由水蓄水水库的地下水的出流系数
            SMM = (1 + ex) * sm;// 自由水的点蓄水容量对应的自由水蓄水容量///公式（5-23）
            _s = s0;
            s.add(_s);
            _fr = fr0; // s-自由水蓄水深、fr-产流面积
            /** 3 **/

            /****************************************************************************************/
            float CSD, CID, CGD;

            // float[] flood = new float[i_floods];
            float Flood, qg, qs, qi;
            // Vector flood = new Vector();

            CSD = new java.lang.Double(java.lang.Math.pow(cs, 1.0 / d)).floatValue();// 每时段的参数值，消退系数
            CID = new java.lang.Double(java.lang.Math.pow(ci, 1.0 / d)).floatValue();
            CGD = new java.lang.Double(java.lang.Math.pow(cg, 1.0 / d)).floatValue();

            float U = area / (3.6f * i_step); // 单位换算系数，将径流深转化为流量
            // float QG[] = new float[i_floods];
            // float QI[] = new float[i_floods];
            // float QS[] = new float[i_floods];
            Vector QG = new Vector();
            Vector QI = new Vector();
            Vector QS = new Vector();
            // 地下径流和壤中流汇流

            bool b_flood = true;
            int i = 0;
            while (b_flood)
            {
                /************************************************************************************************/
                if (i >= P.Length)
                {
                    P1.add(0f);

                }

                if (i < em.Length)
                {
                    _em = em[i];
                }
                else
                {
                    _em = 0f;
                }

                /** 2 **/
                if (DM < 0)
                {
                }
                else
                {
                    /*
                     * 先求得点蓄水容量最大值。 再通过初始土壤含水量与土壤最大蓄水容量进行比较。 再计算径流和：
                     * 1，计算初始平均蓄水量相应的纵坐标A 2，再分情况考虑产流
                     */
                    MM = WM * (1.0f + b) / (1 - im);// 点蓄水容量的最大值（7-1）
                    // System.out.println(i_periods+" "");

                    wmt = (float)ua.get(i) + (float)la.get(i) + (float)da.get(i); // 当前时段土壤含水量
                    if (wmt > WM)
                        wmt = WM;
                    EP = k * _em; // /蒸发能力；k为折减系数
                    // System.out.println(em[i]+" """""");
                    // System.out.println(em.Length+" """""");
                    // System.out.println((float)P1.get(i));
                    PE = (float)P1.get(i) - EP;
                    float _ua = 0, _la = 0, _da = 0;
                    // 以下计算径流和
                    if (PE >= 0)
                    {
                        float temp = new java.lang.Double(java.lang.Math.pow(1 - wmt / WM,
                                1.0 / (b + 1))).floatValue(); // （3-6）
                        A = MM * (1 - temp); // /公式（5-9）
                        if (PE + A < MM) // （3-7）
                        {

                            temp = new java.lang.Double(java.lang.Math.pow(1 - (PE + A) / MM, b + 1))
                                    .floatValue();
                            r = PE - WM + wmt + WM * temp;
                            R.add(r);
                            // System.out.println(r);
                        }
                        else
                        {
                            r = PE - WM + wmt;
                            R.add(r);

                        }
                        // System.out.println("R["+i+"]= "+R[i]);
                        // 以下计算蒸散发，按照水文教材p156流程图
                        EU = k * _em; // 上层按蒸散发能力蒸发
                        EL = 0;
                        ED = 0;
                        if ((float)ua.get(i) + PE - (float)R.get(i) < UM)// 如果上层土壤含水量没有饱和
                        {
                            _ua = (float)ua.get(i) + PE - (float)R.get(i);

                            _la = (float)la.get(i);

                            _da = (float)da.get(i);

                        }
                        else
                        {// 如果上层土壤含水量已经饱和

                            if ((float)ua.get(i) + (float)la.get(i) + PE
                                    - (float)R.get(i) - UM > LM)// 如果第二层土壤含水量已经饱和
                            {
                                _ua = UM;

                                _la = LM;

                                _da = wmt + PE - (float)R.get(i) - _ua - _la;

                            }
                            else// 如果第二层土壤含水量没有饱和
                            {
                                _ua = UM;

                                _la = (float)ua.get(i) + (float)la.get(i) + PE
                                        - (float)R.get(i) - UM;

                                _da = (float)da.get(i);

                            }
                        }
                    }
                    // 以下计算产流量PE<0
                    else if (PE < 0)
                    {// ？？？？？？？？？？？？？？？？？？？？？？？？？？？？？？？？？？？
                        r = 0;
                        R.add(r);
                        if ((float)ua.get(i) + PE > 0)// 上层没有被蒸发完
                        {
                            EU = EP;// 上层按蒸散发能力蒸发
                            EL = 0;
                            ED = 0;
                            _ua = (float)ua.get(i) + PE;

                            _la = (float)la.get(i);

                            _da = (float)da.get(i);

                        }
                        else// 上层已经被蒸发完
                        {
                            EU = (float)ua.get(i) + (float)P1.get(i);
                            _ua = 0;

                            if ((float)la.get(i) > c * LM)
                            {
                                EL = (k * _em - EU) * (float)la.get(i) / LM;
                                _la = (float)la.get(i) - EL;

                                _da = (float)da.get(i);

                                ED = 0;
                            }
                            else
                            {
                                if ((float)la.get(i) > c * (k * _em - EU))
                                {
                                    EL = c * (k * _em - EU);
                                    ED = 0;
                                    _la = (float)la.get(i) - EL;

                                    _da = (float)da.get(i);

                                }
                                else
                                {
                                    EL = (float)la.get(i);
                                    _la = 0;

                                    ED = c * (k * _em - EU) - EL;
                                    _da = (float)da.get(i) - ED;

                                }
                            }
                        }
                    }
                    // float _ua,_la,_da;
                    if (_ua < 0)
                        _ua = 0;
                    if (_la < 0)
                        _la = 0;
                    if (_da < 0)
                        _da = 0;

                    if (_ua > UM)
                        _ua = UM;
                    if (_la > LM)
                        _la = LM;
                    if (_da > DM)
                        _da = DM;

                    ua.add(_ua);
                    la.add(_la);
                    da.add(_da);

                }

                /** 2 **/

                /** 3 **/
                PE = (float)P1.get(i) - k * _em;
                float rs, rg, ri;
                rs = 0;
                rg = 0;
                ri = 0;// 地面径流RS、地下径流RG、壤中流RSS(RI)
                if (PE > 0)
                {
                    if (i == 0)
                    {
                        X = fr0;// X 表示前一个时段的产流面积
                    }
                    else
                    {
                        X = (float)fr.get(i - 1);// 图7-9中的 k
                    }
                    _fr = (float)R.get(i) / PE;// 计算产流面积，是否要考虑不透水面积？？？？？？？？？？
                    if (_fr < 0.001f)
                    {
                        _fr = 0.001f;
                    }
                    fr.add(_fr);
                    _s = X * (float)s.get(i) / (float)fr.get(i);// 自由水
                    SS = _s; // 暂时保存
                    Q = (float)R.get(i) / (float)fr.get(i); // 单位产流面积产生的径流///？？？？？？
                    G = new Float(Q / 5.0f).intValue() + 1;// 解决差分计算的误差问题，5毫米净雨一段把每时段分为G段，///公式（5-31）

                    Q = Q / G;// 单位段（5mm）产生的径流深
                    temp1 = new java.lang.Double(java.lang.Math.pow(1 - (kg + ki), 1.0 / G))
                            .floatValue();
                    ID = (1 - temp1) / (1 + kg / ki);// 以每个时段的KSSD和KGD为参数采用公式(7-9)计算G时段中的每个时段的KSSD和KGD
                    GD = ID * kg / ki;

                    for (int j = 0; j < G; j++)
                    {
                        if (_s > sm)
                            _s = sm;
                        temp1 = new java.lang.Double(java.lang.Math.pow(1 - _s / sm, 1.0 / (1 + ex)))// 错了吧？？？？？？？？？？
                                .floatValue();
                        AU = SMM * (1 - temp1); // （7-6)
                        if (AU + Q < SMM)
                        {
                            float tmp = new java.lang.Double(java.lang.Math.pow(1 - (Q + AU) / SMM,
                                    1 + ex)).floatValue();
                            rs += (Q - sm + _s + sm * tmp) * (float)fr.get(i);

                        }
                        else
                        {
                            rs += (Q + _s - sm) * (float)fr.get(i);// G个时段的径流的和
                        }
                        _s += (j + 1) * Q - rs / (float)fr.get(i);// 暂时保存每个G时段的平均自由水深度
                        rg += _s * GD * (float)fr.get(i);// /（5-29）
                        ri += _s * ID * (float)fr.get(i);
                        _s = (j + 1) * Q + SS - (rs + ri + rg) / (float)fr.get(i);// //？？？？？？
                    }
                    if (_s > sm)
                    {
                        _s = sm;
                    }
                    s.add(_s);
                }
                else
                { // PE < 0，不需要分时段

                    if (i == 0)
                    {
                        _fr = fr0;
                    }
                    else
                    {
                        _fr = (float)fr.get(i - 1);// 产流面积不变
                    }
                    // s[i] = 0;
                    rg = (float)s.get(i) * KGD * _fr;// （7-5）
                    ri = rg * KID / KGD;
                    if (_fr < 0.001f)
                    {
                        _fr = 0.001f;
                    }
                    fr.add(_fr);
                    _s = (float)s.get(i) - (rg + ri) / (float)fr.get(i);
                    if (_s < 0)
                    {
                        _s = 0;
                    }
                    if (_s > sm)
                    {
                        _s = sm;
                    }
                    s.add(_s);
                }
                RS.add(rs);
                RG.add(rg);
                RI.add(ri);

                /** 3 **/

                /*********************************************************************************************/
                if (i == 0)
                {
                    qs = (float)RS.get(i) * (1 - cs) * U;// 地面径流
                    qg = (float)RG.get(i) * (1 - cg) * U;// 地下径流
                    qi = (float)RI.get(i) * (1 - ci) * U;// 壤中流
                }
                else
                {
                    qs = (float)QS.get(i - 1) * cs + (float)RS.get(i) * (1 - cs)
                            * U;// 地面径流经过地表水蓄水库的消退、、更改时间11.5
                    qg = (float)QG.get(i - 1) * cg + (float)RG.get(i) * (1 - cg)
                            * U;
                    qi = (float)QI.get(i - 1) * ci + (float)RI.get(i) * (1 - ci)
                            * U;

                }
                QS.add(qs);
                QG.add(qg);
                QI.add(qi);

                flood_dt.add(flood_date.getTime());
                flood_date.add(Calendar.MINUTE, (int)(Model_Const.XAJ_TIMESTEP_HOUR * 60));

                //if (i > length && qs + qg + qi < baseFlow)//降雨结束后，流量小于等于基流时，计算结束
                if (i > length)//降雨结束后,计算结束
                {
                    b_flood = false;
                }
                i++;
            }

            for (int j = 0; j < QG.size(); j++) // 河网总入流
            {
                if (j == 0)
                {
                    Flood = 0;

                }
                else
                {

                    Flood = (float)QG.get(j) + (float)QI.get(j)
                            + (float)QS.get(j);

                    if (Flood < 0)
                    {
                        Flood = 0;
                    }
                }
                flood.add(Flood);
            }

            return flood;
        }

        /**
         * 单元河网汇流——滞后演算法
         * 
         * "param inflow
         *            输入流量
         * "param cs
         *            河网蓄水消退系数
         * "param L
         *            滞后时段
         * "return dq
         */

        public static float[] Zhihourouting(Vector inflow, float cs, float L)
        {// **********************************5

            float[] dq = new float[inflow.size()];
            int T = (int)L;
            if (T <= 0)
            {
                for (int i = 0; i < inflow.size(); i++)
                {
                    dq[i] = (float)inflow.get(i);
                }
            }
            else
            {
                for (int i = 0; i < inflow.size(); i++)
                {
                    if (i < T)
                    {
                        dq[i] = (float)inflow.get(i);
                    }
                    else
                    {
                        dq[i] = cs * (float)inflow.get(i - 1) + (1.0f - cs)
                                * (float)inflow.get(i - T);
                    }
                }
            }
            return dq;

        }
    }




    class CommonFun
    {
        public static Date getDT(Date dt, int MM, int dd, int HH)
        { //设置6月1号8时的时间
            Calendar cal = Calendar.getInstance();
            cal.setTime(dt);
            cal.set(Calendar.MONTH, MM);
            cal.set(Calendar.DAY_OF_MONTH, dd);
            cal.set(Calendar.HOUR_OF_DAY, HH);
            cal.set(Calendar.MINUTE, 0);
            cal.set(Calendar.SECOND, 0);
            cal.set(Calendar.MILLISECOND, 0);
            return cal.getTime();
        }

    }

}