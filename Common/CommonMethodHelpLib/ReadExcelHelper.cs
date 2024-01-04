using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMethodHelpLib
{
    public  class ReadExcelHelper
    {
        /// <summary>
        /// 读取Excel表格内容
        /// </summary>
        /// <param name="fileNmaePath"></param>
        /// <returns></returns>
        public DataSet ReadExcelToDataSet(string fileNmaePath)
        {
            FileStream stream = null;
            IExcelDataReader excelReader = null;
            DataSet dataSet = null;
            try
            {
                //stream = File.Open(fileNmaePath, FileMode.Open, FileAccess.Read);
                stream = new FileStream(fileNmaePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch
            {
                return null;
            }
            string extension = Path.GetExtension(fileNmaePath);

            if (extension.ToUpper() == ".XLS")
            {
                excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            }
            else if (extension.ToUpper() == ".XLSX")
            {
                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            }
            else
            {
                return null;

            }
            //dataSet = excelReader.AsDataSet();//第一行当作数据读取
            dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });//第一行当作列名读取
            excelReader.Close();
            return dataSet;
        }
        public List<int> getAttribute(string fileNmaePath)
        {
            var Dataset = ReadExcelToDataSet(fileNmaePath);
            List<int> attributeList = new List<int>();
            for (int j = 0; j < Dataset.Tables[0].Rows.Count; j++)
            {
                var dsovalue = Dataset.Tables[0].Rows[j][2];
                attributeList.Add(Convert.ToInt32(dsovalue.ToString()));
            }
            return attributeList;
        }
    }
}
