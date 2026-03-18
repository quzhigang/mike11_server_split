using System;
using System.Collections;

using JavaToC;
using java.util;

using bjd_model.Common;
using bjd_model.Const_Global;

namespace bjd_model.CatchMent
{
    class DayToAve
    {
        public Vector dayToAve(float[] day_em, Date[] day_dt)
        {
            int hour_number = ((int)(24 / Model_Const.XAJ_TIMESTEP_HOUR));
            Vector vec = new Vector();
            CommonFun cf = new CommonFun();
            Calendar cal = Calendar.getInstance();
            int n = day_em.Length * hour_number; ///姝ラ暱涓?灏忔椂*********************************************************
            float[] ave_em = new float[n];
            Date[] ave_dt = new Date[n];
            int i;
            int j;
            for (i = 0; i < day_em.Length; i++)
            {
                cal.setTime(day_dt[i]);
                /***********************鏃舵涓?灏忔椂*****************************/
                for (j = 0; j < hour_number; j++)
                {
                    ave_em[i * hour_number + j] = day_em[i] / hour_number;
                    ave_dt[i * hour_number + j] = CommonFun.getDT(day_dt[i], cal.get(Calendar.MONTH), cal.get(Calendar.DAY_OF_MONTH), j);
                }
                /***********************鏃舵涓?灏忔椂*****************************/
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
            //姣忎竴鍦虹殑钂稿彂鏁版嵁鎻愬彇鎯呭喌
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

    class CommonFun
    {
        public static Date getDT(Date dt, int MM, int dd, int HH)
        { //璁剧疆6鏈?鍙?鏃剁殑鏃堕棿
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
