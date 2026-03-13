using System;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;



namespace bjd_model.Common
{
    public class Excel_Util
    {
        #region ***********************获取excel数据通用方法 ******************************
        //单元格判断1 -- 获取元素double数据
        public static double Get_CellDouble(ISheet sheet, int row, int column, bool show_error = true)
        {
            if (sheet.GetRow(row) == null) return 0;
            ICell cell = sheet.GetRow(row).GetCell(column);
            string cell_info = " 单元格(" + NunberToChar(column + 1) + (row + 1).ToString() + ")";

            if (show_error && cell == null)
            {
                return 0;
            }

            double value = 0;
            try
            {
                value = cell.NumericCellValue;
            }
            catch (Exception)
            {
            }
            return value;
        }

        //单元格判断2 -- 获取元素int数据
        public static int Get_CellInt(ISheet sheet, int row, int column, bool show_error = true)
        {
            if (sheet.GetRow(row) == null) return 0;
            ICell cell = sheet.GetRow(row).GetCell(column);
            string cell_info = " 单元格(" + NunberToChar(column + 1) + (row + 1).ToString() + ")";

            if (show_error && cell == null)
            {
                return 0;
            }

            int value = 0;
            try
            {
                value = (int)cell.NumericCellValue;
            }
            catch (Exception)
            {
            }
            return value;
        }

        //单元格判断3 -- 获取元素string数据
        public static string Get_CellStr(ISheet sheet, int row, int column,bool show_error = true)
        {
            if(sheet.GetRow(row) == null) return "";
            ICell cell = sheet.GetRow(row).GetCell(column);

            string cell_info = " 单元格(" + NunberToChar(column + 1) + (row + 1).ToString() + ")";
            if (show_error && cell == null)
            {
                return "";
            }

            string value = "";
            try
            {
                value = cell.ToString();
            }
            catch (Exception)
            {
            }
            return value;
        }

        //单元格判断4 -- 获取元素date数据
        public static DateTime Get_CellDate(ISheet sheet, int row, int column, bool show_error = true)
        {
            if (sheet.GetRow(row) == null) return new DateTime();
            ICell cell = sheet.GetRow(row).GetCell(column);
            string cell_info = " 单元格(" + NunberToChar(column + 1) + (row + 1).ToString() + ")";

            if (show_error && cell == null)
            {
                return new DateTime();
            }

            DateTime value = new DateTime();
            try
            {
                value = cell.DateCellValue;
            }
            catch (Exception)
            {
                string error = "表 " + sheet.SheetName + cell_info + "不是数据; 取0值";
            }
            return value;
        }

        ///要转换成字母的数字（数字范围在闭区间[1,36]）
        public static string NunberToChar(int number)
        {
            if (1 <= number && 36 >= number)
            {
                int num = number + 64;
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                byte[] btNumber = new byte[] { (byte)num };
                return asciiEncoding.GetString(btNumber);
            }
            return "数字不在转换范围内";
        }
        #endregion *************************************************************************

    }
}
