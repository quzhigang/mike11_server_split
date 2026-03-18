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
}