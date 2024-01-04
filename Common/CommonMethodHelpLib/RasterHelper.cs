using CommonDataStructureLib;
using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMethodHelpLib
{
    /// <summary>
    /// 栅格帮助类
    /// </summary>
    public class RasterHelper
    {

        public Dataset pDataset { get; set; }


        public RasterHelper(string tiffPath)
        {

            pDataset = ReadTIFF(tiffPath);
        }

        /// <summary>
        /// 读取tif
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public Dataset ReadTIFF(string pathName)
        {
            GdalConfiguration.ConfigureGdal();//注册
            GdalConfiguration.ConfigureOgr();
            Gdal.AllRegister();
            Dataset dataset = Gdal.Open(pathName, Access.GA_ReadOnly);
            return dataset;
        }

        /// <summary>
        /// 获取tif的Z范围
        /// </summary>
        /// <param name="zMax"></param>
        public void GetZExtent(out double zMax)
        {
            // 获取栅格数据的长和宽
            int xSize = pDataset.RasterXSize;
            int ySize = pDataset.RasterYSize;
            double[] databuf = new double[xSize * ySize];
            // 获取第一个band 
            Band demband = pDataset.GetRasterBand(1);
            demband.ReadRaster(0, 0, pDataset.RasterXSize, pDataset.RasterYSize, databuf, pDataset.RasterXSize, pDataset.RasterYSize, 0, 0);
            zMax = -9999;
            foreach (var grid in databuf)
                if (grid > zMax)
                    zMax = grid;
        }

        /// <summary>
        /// 根据点坐标获取value
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool GetRasterValue(double x, double y, out double value)
        {
            double[] gt = new double[6];
            pDataset.GetGeoTransform(gt);
            double dTemp = gt[1] * gt[5] - gt[2] * gt[4];
            int c, r;
            try
            {
                c = Convert.ToInt32((gt[5] * (x - gt[0]) - gt[2] * (y - gt[3])) / dTemp);
                r = Convert.ToInt32((gt[1] * (y - gt[3]) - gt[4] * (x - gt[0])) / dTemp);
            }
            catch
            {
                Band demband = pDataset.GetRasterBand(1);
                double noDataValue;
                int hasval;
                demband.GetNoDataValue(out noDataValue, out hasval);
                value = noDataValue;
                return false;
            }
            return GetRasterValue(c, r, out value);
        }

        /// <summary>
        /// 根据点在图像中的行列号获取value
        /// </summary>
        /// <param name="c"></param>
        /// <param name="r"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool GetRasterValue(int c, int r, out double value)
        {
            // 读取第一个波段
            Band demband = pDataset.GetRasterBand(1);
            // 构建一个像元的缓冲区
            double[] databuf = new double[1];
            double noDataValue;
            int hasval;
            demband.GetNoDataValue(out noDataValue, out hasval);
            try
            {
                demband.ReadRaster(c, r, 1, 1, databuf, 1, 1, 0, 0);
                value = databuf[0];
            }
            catch
            {
                value = noDataValue;
            }
            // 如果获取的是nodata
            if (value == noDataValue)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 对点集获取value
        /// </summary>
        /// <param name="p"></param>
        /// <param name="r"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void GetRasterValue(List<GeoAPI.Geometries.Coordinate> points)
        {
            // 读取第一个波段
            Band demband = pDataset.GetRasterBand(1);
            // 构建一个像元的缓冲区
            double[] databuf = new double[1];
            double noDataValue;
            int hasval;
            demband.GetNoDataValue(out noDataValue, out hasval);
            double[] gt = new double[6];
            pDataset.GetGeoTransform(gt);

            foreach (var point in points)
            {
                double dTemp = gt[1] * gt[5] - gt[2] * gt[4];
                int c = Convert.ToInt32((gt[5] * (point.X - gt[0]) - gt[2] * (point.Y - gt[3])) / dTemp);
                int r = Convert.ToInt32((gt[1] * (point.Y - gt[3]) - gt[4] * (point.X - gt[0])) / dTemp);
                demband.ReadRaster(c, r, 1, 1, databuf, 1, 1, 0, 0);

                // 如果获取值为nodatavalue
                if (databuf[0] == noDataValue)
                {
                    double nearestZ = 0;
                    double nearestD = 99999;
                    for (var i = 0; i < points.Count; i++)
                    {
                        if (points[i].Z != 0)
                        {
                            double D = Math.Sqrt((point.X - points[i].X) * (point.X - points[i].X) + (point.Y - points[i].Y) * (point.Y - points[i].Y));
                            if (D < nearestD)
                            {
                                nearestD = D;
                                nearestZ = point.Z;
                            }
                        }
                    }
                    point.Z = nearestZ;
                }
                else
                {
                    point.Z = databuf[0];
                }
            }
        }
        public void GetRasterValueByDrill(IList<DrillModel> points)
        {
            // 读取第一个波段
            Band demband = pDataset.GetRasterBand(1);
            // 构建一个像元的缓冲区
            double[] databuf = new double[1];
            double noDataValue;
            int hasval;
            demband.GetNoDataValue(out noDataValue, out hasval);
            double[] gt = new double[6];
            pDataset.GetGeoTransform(gt);

            foreach (var point in points)
            {
                double dTemp = gt[1] * gt[5] - gt[2] * gt[4];
                int c = Convert.ToInt32((gt[5] * (point.X - gt[0]) - gt[2] * (point.Y - gt[3])) / dTemp);
                int r = Convert.ToInt32((gt[1] * (point.Y - gt[3]) - gt[4] * (point.X - gt[0])) / dTemp);
                demband.ReadRaster(c, r, 1, 1, databuf, 1, 1, 0, 0);

                // 如果获取值为nodatavalue
                if (databuf[0] == noDataValue)
                {
                    double nearestZ = 0;
                    double nearestD = 99999;
                    for (var i = 0; i < points.Count; i++)
                    {
                        if (points[i].H != 0)
                        {
                            double D = Math.Sqrt((point.X - points[i].X) * (point.X - points[i].X) + (point.Y - points[i].Y) * (point.Y - points[i].Y));
                            if (D < nearestD)
                            {
                                nearestD = D;
                                nearestZ = point.H;
                            }
                        }
                    }
                    point.H = nearestZ;
                }
                else
                {
                    point.H = databuf[0];
                }
            }
        }
        /// <summary>
        /// 根据点坐标获取value
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool GetRasterValueByPoint(double x, double y, out double value)
        {
            double[] gt = new double[6];
            pDataset.GetGeoTransform(gt);
            double dTemp = gt[1] * gt[5] - gt[2] * gt[4];
            int c = Convert.ToInt32((gt[5] * (x - gt[0]) - gt[2] * (y - gt[3])) / dTemp);
            int r = Convert.ToInt32((gt[1] * (y - gt[3]) - gt[4] * (x - gt[0])) / dTemp);
            return GetRasterValue(c, r, out value);
        }

    }
}
