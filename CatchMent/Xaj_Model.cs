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
            rc = rcpojo.getRainOff(xaj);		//鏃舵闄嶉洦杩囩▼鍜屾棩闄嶉洦杩囩▼

            EvapCodePOJO ecpojo = new EvapCodePOJO();
            ec = ecpojo.getEvap(xaj);           //鏃ヨ捀鍙戜互鍙婃椂娈佃捀鍙?

        }

        // 娲按棰勬姤
        public static Dictionary<DateTime, double> Forecast(Xaj xaj)
        {
            Dictionary<DateTime, double> resultdic = new Dictionary<DateTime, double>();

            Console.WriteLine("鏂板畨姹熸ā鍨嬪紑濮嬭繍琛?.....");

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

            float ua0_day = (float)xaj.XajInitial.ua_day;// /涓婂眰鍦熷￥鍒濆鍚按閲?
            float la0_day = (float)xaj.XajInitial.la_day;// /涓嬪眰鍦熷￥鍒濆鍚按閲?
            float da0_day = (float)xaj.XajInitial.da_day;// /娣卞眰鍦熷￥鍒濆鍚按閲?
            float baseFlow = (float)xaj.XajInitial.baseFlow;//鍩烘祦
            vec = dltx.getOneFitness(bfcd, fn, uc, rc, ec, para, dltx, ua0_day, la0_day, da0_day, baseFlow);
            oneSessionTotalFlood = (float[][])vec.get(0);
            Vector flood_dt = (Vector)vec.get(1);

            /**************杞崲娲按杩囩▼鏁版嵁************/
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
            // 璇ユ柟娉曡繍琛屽お鎱?
            Vector vec = new Vector();
            Vector vec0, vec1, vec2, vec3, vec4, vec5;
            float[][] oneSessionTotalFlood = new float[bfcd.floodNo.Length][];
            float[][] oneSessionTotalR = new float[bfcd.floodNo.Length][];
            float[][] oneSessionTotalQ = new float[bfcd.floodNo.Length][];
            float[] W00 = new float[bfcd.floodNo.Length];

            CommonFun cf = new CommonFun();
            GetEm ge = new GetEm();
            Vector flood_dt = new Vector();
            for (int i = 0; i < bfcd.floodNo.Length; i++)  //寰幆涓嶅悓鍦烘娲按
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
                        String_Date.DateToString(ec[0].dt[ec[0].dt.Length - 1]));  // 鍙栧緱鏃ヨ捀鍙戞暟鎹?    //2016-11-03鍐欙細鍓嶆湡鏃ヨ捀鍙戠洿鎺ョ粰瀹氱殑锛岄€夊彇寮€濮嬫棩鏈熷拰缁撴潫鏃ユ湡鎴彇鏃ヨ捀鍙?
                Date[] day_dt = (Date[])vec1.get(0);
                float[] day_em = (float[])vec1.get(1);


                for (int j = 0; j < fn[i].unitCode.Length; j++)
                {//寰幆涓嶅悓鍗曞厓,
                    unitWeighth[j] = fn[i].unitCode[j].unitWeight;
                    day_p = new float[uc[i][j].rainCode.Length][];
                    step_p = new float[uc[i][j].rainCode.Length][];
                    rainWeight = new float[uc[i][j].rainCode.Length];
                    float areaUnit = fn[i].unitCode[j].unitWeight * area;   ///鍘熸潵鏄?92锛屾敼涓?60
                    for (int k = 0; k < uc[i][j].rainCode.Length; k++)
                    {   //寰幆姣忎釜棰勬姤鍗曞厓閲岄潰鐨勯洦閲忕珯	
                        rainWeight[k] = uc[i][j].rainCode[k].rainWeight;
                        step_p[k] = new float[rc[i][j][0].p_step.Length];
                        day_p[k] = rc[i][j][k].p_day;  //姣忎竴鍦烘姣忎釜鍗曞厓涓瘡涓洦閲忕珯鐨勬棩闄嶉洦
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
                    aveDay_p[j] = (float[])vec4.get(0);  // 寰楀埌骞冲潎鏃ラ檷闆?

                    vec5 = XajModel.getAveValue(step_p, rainWeight);///鏃舵闆ㄩ噺				
                    aveStep_p[j] = (float[])vec5.get(0);///鏃舵闆ㄩ噺

                    vec0 = XajModel.getOneUnitFloodArray(aveDay_p[j], day_em, aveStep_p[j],
                             step_em, areaUnit, para, fn[i].unitCode.Length, unitWeighth, ua0_day, la0_day, da0_day, baseFlow, String_Date.DateToString(bfcd.floodNo[i].rainbeg));

                    oneUnitFlood[j] = (float[])vec0.get(0);    // 姣忎竴涓洦閲忕珯瀛愬崟鍏冪殑婕旇繘4  
                    flood_dt = (Vector)vec0.get(1);


                }

                vec3 = XajModel.getOneSessionTotalValue(oneUnitFlood, fn[i].unitCode.Length,
                         unitWeighth);
                oneSessionTotalFlood[i] = (float[])vec3.get(0);


            }  // 鍦烘寰幆缁撴潫

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

            float[] totalFlow0 = new float[bfcd.floodNo.Length];///鐪熷疄娴侀噺鎬诲拰
            float[] totalFlowf = new float[bfcd.floodNo.Length];///妯℃嫙娴侀噺鎬诲拰
            float[] totalRf = new float[bfcd.floodNo.Length];
            float[] absR = new float[bfcd.floodNo.Length];   ///R缁濆璇樊
            float[] relaR = new float[bfcd.floodNo.Length];  ///R鐩稿璇樊
            float[] R0 = new float[bfcd.floodNo.Length];///瀹炴祴R
            float[] Rf = new float[bfcd.floodNo.Length];///棰勬姤R

            float[] passNum = new float[bfcd.floodNo.Length];
            float totalPassNum = 0;
            float[] sum_p = new float[bfcd.floodNo.Length];///闄嶉洦閲?
            float[] aerfa0 = new float[bfcd.floodNo.Length];/// 瀹炴祴浜ф祦绯绘暟 aerfa0
            float[] aerfaf = new float[bfcd.floodNo.Length];
            float[] maxQ0 = new float[bfcd.floodNo.Length];
            float[] absMaxQ = new float[bfcd.floodNo.Length];
            float[] relaMaxQ = new float[bfcd.floodNo.Length];

            int[] numMaxQ0 = new int[bfcd.floodNo.Length];
            Date[] dateMaxQ0 = new Date[bfcd.floodNo.Length];
            int[] absNumMaxQ = new int[bfcd.floodNo.Length];///宄扮幇缁濆璇樊


            float[] nash = new float[bfcd.floodNo.Length];///纭畾鎬х郴鏁?
            float[] sum_a1 = new float[bfcd.floodNo.Length];///鐢ㄦ潵璁＄畻纭畾鎬х郴鏁?
            float[] sum_b1 = new float[bfcd.floodNo.Length];///鐢ㄦ潵璁＄畻纭畾鎬х郴鏁?

            float[] maxQf = new float[bfcd.floodNo.Length];   ///棰勬姤娴侀噺宄板€硷紝guokelun


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


    class XajParam
    {

        public float Sm;    //寮犲姏姘?钃勬按瀹归噺
        public float Wm;
        public float Um;    //涓婂眰钃勬按瀹归噺 
        public float Lm;    //涓嬪眰钃勬按瀹归噺  
        public float B;   //寮犲姏姘磋搫姘村閲忔洸绾跨殑鏂规
        public float Im;  //涓嶉€忔按鐨勯潰绉瘮渚?     0.01~0.02
        public float K;   //钂稿彂鑳藉姏鎶樼畻绯绘暟
        public float C;    //娣卞眰钂稿彂绯绘暟
        public float Ki;  //鑷敱姘磋搫姘存按搴撳澹や腑娴佺殑鍑烘祦绯绘暟
        public float Ex;  //琛ㄥ湡鑷敱姘磋搫姘村閲忔洸绾跨殑鏂规
        public float Ci;  //澹や腑娴佹秷閫€绯绘暟
        public float Cg;  //鍦颁笅寰勬祦娑堥€€绯绘暟                          鍊艰秺灏? 宄板€艰秺澶?  瀵归€€姘寸殑褰卞搷杈冨皬
        public float Cs;   //鍦拌〃寰勬祦娑堥€€绯绘暟                   闄嶄綆娲噺姣旇緝鏁忔劅  鍊艰秺澶?娑堥€€瓒婃參 
        public float L;      // 婊炲悗鏃舵鏁?澧炲ぇ浣垮嘲鍊煎悜鍚庯紝骞舵湁闄嶄綆娲嘲鐨勪綔鐢ㄣ€?
        public float Cr;   //娌崇綉钃勬按娑堥€€绯绘暟


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
            // 姣忓満娲按姣忓満棰勬姤鍗曞厓鐨勬ā鎷熸柟娉?鈽呪槄鈽呪槄鈽嗏槅鈽嗏槅鈽嗏槅鈽嗏槅鈽嗏槅鈽嗏槅鈽嗏槅鈽呪槄鈽呪槄
            Vector vec = new Vector();

            int hour_number = ((int)(24 / Model_Const.XAJ_TIMESTEP_HOUR));
            int length1;
            int deday = 0;  // 涓嶇煡閬撴槸浠€涔堟剰鎬漡uokelun
            int i_step = 1; //鏃舵姝ラ暱
            int i_max_len = deday * hour_number / i_step;  //涓嶇煡閬撴槸浠€涔堟剰鎬漡uokelun
            Vector flood = new Vector();

            /**************** 鏂板畨姹熷弬鏁拌祴鍊?*****************/
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
            /**************** 鏂板畨姹熷弬鏁拌祴鍊?*****************/
            length1 = day_p.Length;// /鏃ラ檷闆ㄥ巻鏃堕暱搴?

            /********** 鏃ヨ捀鍙戣绠楀湡澹ゅ惈姘撮噺鍒濆鍊?***********/
            float[] R_day = new float[length1];// 寰勬祦鍘嗘椂闀垮害绛変簬鏃ラ檷闆ㄥ巻鏃堕暱搴?
            float[] ua_day = new float[length1 + 1];  //涓婂眰鍦熷￥鍚按閲?
            float[] la_day = new float[length1 + 1];  //涓婂眰鍦熷￥鍚按閲?
            float[] da_day = new float[length1 + 1];  //涓婂眰鍦熷￥鍚按閲?
            XajFunction.runoff_xaj3_model_day(WM, UM, LM, im, b, k, c, length1,
                    day_p, day_em, ua0_day, la0_day, da0_day, R_day, ua_day,
                    la_day, da_day); // *****************************************1
            /********** 鏃ヨ捀鍙戣绠楀湡澹ゅ惈姘撮噺鍒濆鍊?***********/
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
                    deday, i_step, area, length, baseFlow, rainBeg, flood, flood_dt);   //浜ф祦銆佸垎姘存簮銆佷互鍙婂潯鍦版眹娴佽绠?
            /****************** 娌崇綉姹囨祦璁＄畻 *********************/
            float[] dq = new float[flood.size()];
            dq = XajFunction.Zhihourouting(flood, cr, L);
            /****************** 娌崇綉姹囨祦璁＄畻 *********************/

            vec.add(dq); // 4 璇ュ崟鍏冪殑浜ф眹娴佺粨鏋?
            vec.add(flood_dt);
            return vec;
        }

        public static Vector getOneSessionTotalValue(float[][] oneUnitFlood,
                int numUnit, float[] weighth)
        {

            Vector vec = new Vector();
            Vector vec_Flood = new Vector();


            for (int i = 0; i < oneUnitFlood[0].Length; i++)
            { // 寰幆鍦烘娲按杩囩▼鐨勪笉鍚屾椂鍒?
                float Flood = 0;
                for (int j = 0; j < numUnit; j++)
                { // 寰幆涓嶅悓鍖哄煙
                    Flood += oneUnitFlood[j][i];
                }
                vec_Flood.add(Flood);
            }
            float[] allUnitFlood = new float[vec_Flood.size()];
            for (int i = 0; i < vec_Flood.size(); i++)
            {
                allUnitFlood[i] = (float)vec_Flood.get(i);
            }
            vec.add(allUnitFlood);// /0 涓€鍦烘椽姘寸殑娲按杩囩▼
            return vec;
        }

        //璁＄畻闆ㄩ噺鐨勫姞鏉冨钩鍧囧€煎€?**杩欎釜鍑芥暟瀵逛簬鏈郴缁熸病鏈夌敤锛屽洜涓烘湰绯荤粺鐩存帴鐢ㄧ殑闈㈤洦閲忥紝浣嗘槸鍒犳帀鐨勮瘽闇€瑕佹敼鍔ㄧ▼搴忥紝鎵€浠ュ氨淇濈暀浜嗕笅鏉?guokelun
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
         * 钂稿彂鏃ヨ绠楁ā鍨?
         * 
         * "param WM
         * "param UM
         * "param LM
         * "param im
         * "param b
         * "param k
         * "param c
         * "param length1
         *            鏃ユā鍨嬮暱搴?
         * "param P_day
         *            鏃ラ檷闆?
         * "param em_day
         *            鏃ヨ捀鍙?
         * "param ua0_day
         * "param la0_day
         * "param da0_day
         * "param xajParam
         * "param R_day
         *            鏃ュ緞娴侀噺
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
            float[] EU = new float[length1];// 钂稿彂
            float[] EL = new float[length1];
            float[] ED = new float[length1];
            float wmt, MM, A, EP;// t鏃跺埢鐨勫湡澹ゅ钩鍧囧惈姘撮噺//鐐硅搫姘村閲忕殑鏈€澶у€硷紙7-1锛?
            // wmt鎵€瀵瑰簲鐨勬祦鍩熻搫姘村閲忔洸绾跨殑绾靛潗鏍囥€佽捀鍙?
            ua_day[0] = ua0_day;
            la_day[0] = la0_day;
            da_day[0] = da0_day;// 姣忎竴灞傜殑鍒濆鍚按閲?

            DM = WM - (UM + LM);
            if (DM < 0)
            {

            }
            else
            {

                MM = WM * (1.0f + b) / (1.0f - im);// 鐐硅搫姘村閲忕殑鏈€澶у€硷紙7-1锛?

                for (int i = 0; i < length1; i++)
                {
                    wmt = ua_day[i] + la_day[i] + da_day[i]; // 褰撳墠鏃舵鍦熷￥鍚按閲?
                    if (wmt > WM)
                        wmt = WM;
                    EP = k * em_day[i];
                    PE = P_day[i] - EP;

                    // 浠ヤ笅璁＄畻寰勬祦鍜?
                    if (PE >= 0)
                    {
                        float temp = new java.lang.Double(java.lang.Math.pow(1 - wmt / WM,
                                1.0 / (b + 1))).floatValue();// 锛?-6锛?
                        A = MM * (1 - temp);
                        if (PE + A < MM)// 锛?-7锛?
                        {
                            temp = new java.lang.Double(java.lang.Math.pow(1 - (PE + A) / MM, b + 1))
                                    .floatValue();
                            R_day[i] = PE - WM + wmt + WM * temp;
                        }
                        else
                        {
                            R_day[i] = PE - WM + wmt;
                        }
                        // 浠ヤ笅璁＄畻钂告暎鍙戯紝鎸夌収姘存枃鏁欐潗p156娴佺▼鍥?
                        EU[i] = k * em_day[i];// 涓婂眰鎸夎捀鏁ｅ彂鑳藉姏钂稿彂
                        EL[i] = 0;
                        ED[i] = 0;
                        if (ua_day[i] + PE - R_day[i] < UM)// 濡傛灉涓婂眰鍦熷￥鍚按閲忔病鏈夐ケ鍜?
                        {
                            ua_day[i + 1] = ua_day[i] + PE - R_day[i];
                            la_day[i + 1] = la_day[i];
                            da_day[i + 1] = da_day[i];
                        }
                        else
                        { // 濡傛灉涓婂眰鍦熷￥鍚按閲忓凡缁忛ケ鍜?

                            if (ua_day[i] + la_day[i] + PE - R_day[i] - UM > LM)// 濡傛灉绗簩灞傚湡澹ゅ惈姘撮噺宸茬粡楗卞拰
                            {
                                ua_day[i + 1] = UM;
                                la_day[i + 1] = LM;
                                da_day[i + 1] = wmt + PE - R_day[i] - ua_day[i + 1]
                                        - la_day[i + 1];
                            }
                            else// 濡傛灉绗簩灞傚湡澹ゅ惈姘撮噺娌℃湁楗卞拰
                            {
                                ua_day[i + 1] = UM;
                                la_day[i + 1] = ua_day[i] + la_day[i] + PE
                                        - R_day[i] - UM;
                                da_day[i + 1] = da_day[i];
                            }
                        }
                    }
                    // 浠ヤ笅璁＄畻钂稿彂閲廝E<0
                    else if (PE < 0)
                    {
                        R_day[i] = 0;
                        if (ua_day[i] + PE > 0)// 涓婂眰娌℃湁琚捀鍙戝畬
                        {
                            EU[i] = EP;// 涓婂眰鎸夎捀鏁ｅ彂鑳藉姏钂稿彂
                            EL[i] = 0;
                            ED[i] = 0;
                            ua_day[i + 1] = ua_day[i] + PE;
                            la_day[i + 1] = la_day[i];
                            da_day[i + 1] = da_day[i];
                        }
                        else// 涓婂眰宸茬粡琚捀鍙戝畬
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
                    // 绾︽潫鏈€灏忓€?
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
         * 鍧″湴姹囨祦璁＄畻妯″瀷
         * 
         * "param cs
         *            鍦拌〃锛堟渤缃戣搫姘撮噺锛夋秷閫€绯绘暟
         * "param ci
         *            澹や腑娴佹秷閫€绯绘暟
         * "param cg
         *            鍦颁笅寰勬祦娑堥€€绯绘暟
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

            /****************** 娲按寮€濮嬫椂闂?**********************/

            //SimpleDateFormat simpledateformat = new SimpleDateFormat(
            //        "yyyy-MM-dd HH:mm:ss");
            Date rain_Beg = new Date();
            try
            {
                rain_Beg = String_Date.StringToDate(rainBeg);
            }
            catch (java.lang.Exception e)
            {
                // TODO 鑷姩鐢熸垚鐨?catch 鍧?
                e.printStackTrace();
            }
            Calendar flood_date = Calendar.getInstance();
            flood_date.setTime(rain_Beg);

            float PE;// 闄嶉洦閲忓拰钂稿彂閲忕殑宸€笺€?
            float DM;// 娣卞眰鍦熷￥钃勬按瀹归噺銆?
            float EU;// 涓婂眰钂稿彂閲?
            float EL;// 涓嬪眰钂稿彂閲?
            float ED;// 娣卞眰钂稿彂閲?
            float wmt, MM, A, EP;// t鏃跺埢鐨勫湡澹ゅ钩鍧囧惈姘撮噺//鐐硅搫姘村閲忕殑鏈€澶у€硷紙7-1锛?
            // wmt鎵€瀵瑰簲鐨勬祦鍩熻搫姘村閲忔洸绾跨殑绾靛潗鏍囥€佹祦鍩熻捀鏁ｅ彂鑳藉姏
            ua.add(ua0);
            la.add(la0);
            da.add(da0);// 姣忎竴灞傜殑鍒濆鍚按閲?
            // 浠庝笁姘存簮妯″瀷缁撴瀯鍙橀噺涓彇寰楁ā鍨嬪弬鏁?
            // 浜ф祦閲忚绠楁ā鍨嬪弬鏁?
            DM = WM - (UM + LM);
            /** 2 **/
            /** 3 **/
            float kg; // 娣卞眰鍦颁笅姘村嚭娴佺郴鏁?
            // float PE;
            float SMM, AU, X;// 鍏ㄦ祦鍩熸渶澶х偣鐨勮搫姘村閲廠MM=MS銆併€佸墠涓€鏃舵鐨勪骇娴侀潰绉?
            float SS, Q, GD, ID;// 鍗曚綅闈㈢Н寰勬祦閲忋€?//==KI/KG
            int d, G;
            float KID, KGD; // /KID=涔︿腑KI鈻硉;KGD=涔︿腑KG鈻硉锛屽嵆鏃舵杞崲鍚庣殑KI銆並G
            kg = 0.7f - ki;
            d = 24 / i_step; // 涓€澶╁垎鐨勬椂娈垫暟
            float temp1 = new java.lang.Double(java.lang.Math.pow(1 - (kg + ki), 1.0 / d)).floatValue();
            KID = (1.0f - temp1) / (1.0f + kg / ki);// kssd-鍜宬ss瀵瑰簲鐨勫弬鏁?///鍏紡锛?-33锛?
            // 璁＄畻姝ラ暱鍐呮祦鍩熻嚜鐢辨按钃勬按姘村簱鐨勫￥涓祦鐨勫嚭娴佺郴鏁?
            KGD = KID * kg / ki;// 鍜宬g瀵瑰簲鐨勫弬鏁?/璁＄畻姝ラ暱鍐呮祦鍩熻嚜鐢辨按钃勬按姘村簱鐨勫湴涓嬫按鐨勫嚭娴佺郴鏁?
            SMM = (1 + ex) * sm;// 鑷敱姘寸殑鐐硅搫姘村閲忓搴旂殑鑷敱姘磋搫姘村閲?//鍏紡锛?-23锛?
            _s = s0;
            s.add(_s);
            _fr = fr0; // s-鑷敱姘磋搫姘存繁銆乫r-浜ф祦闈㈢Н
            /** 3 **/

            /****************************************************************************************/
            float CSD, CID, CGD;

            // float[] flood = new float[i_floods];
            float Flood, qg, qs, qi;
            // Vector flood = new Vector();

            CSD = new java.lang.Double(java.lang.Math.pow(cs, 1.0 / d)).floatValue();// 姣忔椂娈电殑鍙傛暟鍊硷紝娑堥€€绯绘暟
            CID = new java.lang.Double(java.lang.Math.pow(ci, 1.0 / d)).floatValue();
            CGD = new java.lang.Double(java.lang.Math.pow(cg, 1.0 / d)).floatValue();

            float U = area / (3.6f * i_step); // 鍗曚綅鎹㈢畻绯绘暟锛屽皢寰勬祦娣辫浆鍖栦负娴侀噺
            // float QG[] = new float[i_floods];
            // float QI[] = new float[i_floods];
            // float QS[] = new float[i_floods];
            Vector QG = new Vector();
            Vector QI = new Vector();
            Vector QS = new Vector();
            // 鍦颁笅寰勬祦鍜屽￥涓祦姹囨祦

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
                     * 鍏堟眰寰楃偣钃勬按瀹归噺鏈€澶у€笺€?鍐嶉€氳繃鍒濆鍦熷￥鍚按閲忎笌鍦熷￥鏈€澶ц搫姘村閲忚繘琛屾瘮杈冦€?鍐嶈绠楀緞娴佸拰锛?
                     * 1锛岃绠楀垵濮嬪钩鍧囪搫姘撮噺鐩稿簲鐨勭旱鍧愭爣A 2锛屽啀鍒嗘儏鍐佃€冭檻浜ф祦
                     */
                    MM = WM * (1.0f + b) / (1 - im);// 鐐硅搫姘村閲忕殑鏈€澶у€硷紙7-1锛?
                    // System.out.println(i_periods+" "");

                    wmt = (float)ua.get(i) + (float)la.get(i) + (float)da.get(i); // 褰撳墠鏃舵鍦熷￥鍚按閲?
                    if (wmt > WM)
                        wmt = WM;
                    EP = k * _em; // /钂稿彂鑳藉姏锛沰涓烘姌鍑忕郴鏁?
                    // System.out.println(em[i]+" """""");
                    // System.out.println(em.Length+" """""");
                    // System.out.println((float)P1.get(i));
                    PE = (float)P1.get(i) - EP;
                    float _ua = 0, _la = 0, _da = 0;
                    // 浠ヤ笅璁＄畻寰勬祦鍜?
                    if (PE >= 0)
                    {
                        float temp = new java.lang.Double(java.lang.Math.pow(1 - wmt / WM,
                                1.0 / (b + 1))).floatValue(); // 锛?-6锛?
                        A = MM * (1 - temp); // /鍏紡锛?-9锛?
                        if (PE + A < MM) // 锛?-7锛?
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
                        // 浠ヤ笅璁＄畻钂告暎鍙戯紝鎸夌収姘存枃鏁欐潗p156娴佺▼鍥?
                        EU = k * _em; // 涓婂眰鎸夎捀鏁ｅ彂鑳藉姏钂稿彂
                        EL = 0;
                        ED = 0;
                        if ((float)ua.get(i) + PE - (float)R.get(i) < UM)// 濡傛灉涓婂眰鍦熷￥鍚按閲忔病鏈夐ケ鍜?
                        {
                            _ua = (float)ua.get(i) + PE - (float)R.get(i);

                            _la = (float)la.get(i);

                            _da = (float)da.get(i);

                        }
                        else
                        {// 濡傛灉涓婂眰鍦熷￥鍚按閲忓凡缁忛ケ鍜?

                            if ((float)ua.get(i) + (float)la.get(i) + PE
                                    - (float)R.get(i) - UM > LM)// 濡傛灉绗簩灞傚湡澹ゅ惈姘撮噺宸茬粡楗卞拰
                            {
                                _ua = UM;

                                _la = LM;

                                _da = wmt + PE - (float)R.get(i) - _ua - _la;

                            }
                            else// 濡傛灉绗簩灞傚湡澹ゅ惈姘撮噺娌℃湁楗卞拰
                            {
                                _ua = UM;

                                _la = (float)ua.get(i) + (float)la.get(i) + PE
                                        - (float)R.get(i) - UM;

                                _da = (float)da.get(i);

                            }
                        }
                    }
                    // 浠ヤ笅璁＄畻浜ф祦閲廝E<0
                    else if (PE < 0)
                    {// 锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵锛?
                        r = 0;
                        R.add(r);
                        if ((float)ua.get(i) + PE > 0)// 涓婂眰娌℃湁琚捀鍙戝畬
                        {
                            EU = EP;// 涓婂眰鎸夎捀鏁ｅ彂鑳藉姏钂稿彂
                            EL = 0;
                            ED = 0;
                            _ua = (float)ua.get(i) + PE;

                            _la = (float)la.get(i);

                            _da = (float)da.get(i);

                        }
                        else// 涓婂眰宸茬粡琚捀鍙戝畬
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
                ri = 0;// 鍦伴潰寰勬祦RS銆佸湴涓嬪緞娴丷G銆佸￥涓祦RSS(RI)
                if (PE > 0)
                {
                    if (i == 0)
                    {
                        X = fr0;// X 琛ㄧず鍓嶄竴涓椂娈电殑浜ф祦闈㈢Н
                    }
                    else
                    {
                        X = (float)fr.get(i - 1);// 鍥?-9涓殑 k
                    }
                    _fr = (float)R.get(i) / PE;// 璁＄畻浜ф祦闈㈢Н锛屾槸鍚﹁鑰冭檻涓嶉€忔按闈㈢Н锛燂紵锛燂紵锛燂紵锛燂紵锛燂紵
                    if (_fr < 0.001f)
                    {
                        _fr = 0.001f;
                    }
                    fr.add(_fr);
                    _s = X * (float)s.get(i) / (float)fr.get(i);// 鑷敱姘?
                    SS = _s; // 鏆傛椂淇濆瓨
                    Q = (float)R.get(i) / (float)fr.get(i); // 鍗曚綅浜ф祦闈㈢Н浜х敓鐨勫緞娴?//锛燂紵锛燂紵锛燂紵
                    G = new Float(Q / 5.0f).intValue() + 1;// 瑙ｅ喅宸垎璁＄畻鐨勮宸棶棰橈紝5姣背鍑€闆ㄤ竴娈垫妸姣忔椂娈靛垎涓篏娈碉紝///鍏紡锛?-31锛?

                    Q = Q / G;// 鍗曚綅娈碉紙5mm锛変骇鐢熺殑寰勬祦娣?
                    temp1 = new java.lang.Double(java.lang.Math.pow(1 - (kg + ki), 1.0 / G))
                            .floatValue();
                    ID = (1 - temp1) / (1 + kg / ki);// 浠ユ瘡涓椂娈电殑KSSD鍜孠GD涓哄弬鏁伴噰鐢ㄥ叕寮?7-9)璁＄畻G鏃舵涓殑姣忎釜鏃舵鐨凨SSD鍜孠GD
                    GD = ID * kg / ki;

                    for (int j = 0; j < G; j++)
                    {
                        if (_s > sm)
                            _s = sm;
                        temp1 = new java.lang.Double(java.lang.Math.pow(1 - _s / sm, 1.0 / (1 + ex)))// 閿欎簡鍚э紵锛燂紵锛燂紵锛燂紵锛燂紵锛?
                                .floatValue();
                        AU = SMM * (1 - temp1); // 锛?-6)
                        if (AU + Q < SMM)
                        {
                            float tmp = new java.lang.Double(java.lang.Math.pow(1 - (Q + AU) / SMM,
                                    1 + ex)).floatValue();
                            rs += (Q - sm + _s + sm * tmp) * (float)fr.get(i);

                        }
                        else
                        {
                            rs += (Q + _s - sm) * (float)fr.get(i);// G涓椂娈电殑寰勬祦鐨勫拰
                        }
                        _s += (j + 1) * Q - rs / (float)fr.get(i);// 鏆傛椂淇濆瓨姣忎釜G鏃舵鐨勫钩鍧囪嚜鐢辨按娣卞害
                        rg += _s * GD * (float)fr.get(i);// /锛?-29锛?
                        ri += _s * ID * (float)fr.get(i);
                        _s = (j + 1) * Q + SS - (rs + ri + rg) / (float)fr.get(i);// //锛燂紵锛燂紵锛燂紵
                    }
                    if (_s > sm)
                    {
                        _s = sm;
                    }
                    s.add(_s);
                }
                else
                { // PE < 0锛屼笉闇€瑕佸垎鏃舵

                    if (i == 0)
                    {
                        _fr = fr0;
                    }
                    else
                    {
                        _fr = (float)fr.get(i - 1);// 浜ф祦闈㈢Н涓嶅彉
                    }
                    // s[i] = 0;
                    rg = (float)s.get(i) * KGD * _fr;// 锛?-5锛?
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
                    qs = (float)RS.get(i) * (1 - cs) * U;// 鍦伴潰寰勬祦
                    qg = (float)RG.get(i) * (1 - cg) * U;// 鍦颁笅寰勬祦
                    qi = (float)RI.get(i) * (1 - ci) * U;// 澹や腑娴?
                }
                else
                {
                    qs = (float)QS.get(i - 1) * cs + (float)RS.get(i) * (1 - cs)
                            * U;// 鍦伴潰寰勬祦缁忚繃鍦拌〃姘磋搫姘村簱鐨勬秷閫€銆併€佹洿鏀规椂闂?1.5
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

                //if (i > length && qs + qg + qi < baseFlow)//闄嶉洦缁撴潫鍚庯紝娴侀噺灏忎簬绛変簬鍩烘祦鏃讹紝璁＄畻缁撴潫
                if (i > length)//闄嶉洦缁撴潫鍚?璁＄畻缁撴潫
                {
                    b_flood = false;
                }
                i++;
            }

            for (int j = 0; j < QG.size(); j++) // 娌崇綉鎬诲叆娴?
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
         * 鍗曞厓娌崇綉姹囨祦鈥斺€旀粸鍚庢紨绠楁硶
         * 
         * "param inflow
         *            杈撳叆娴侀噺
         * "param cs
         *            娌崇綉钃勬按娑堥€€绯绘暟
         * "param L
         *            婊炲悗鏃舵
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




}