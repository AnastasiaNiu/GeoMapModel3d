using CommonDataStructureLib;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterpolateLib
{
    public class Interpolate : IInterpolate
    {
        internal Interpolate() { }
        public static int scope;
        public static int number;
        public IList<double> interpolate(IList<Points> sourcePoints, IList<Points> targetPoints, EnumInterpWay way)
        {
            List<double> result = new List<double>();
            // 原始钻孔数据
            double[,] msrdPts = new double[sourcePoints.Count, 3];
            for (int j = 0; j < sourcePoints.Count; j++)
            {
                msrdPts[j, 0] = sourcePoints[j].x;
                msrdPts[j, 1] = sourcePoints[j].y;
                msrdPts[j, 2] = sourcePoints[j].z;
            }
            //得到targetPoints的插值结果
            for (int t = 0; t < targetPoints.Count; t++)
            {
                double result0ne;
                switch (way)
                {
                    //反距离权重插值
                    case EnumInterpWay.IDW:
                        result0ne = IDW_Interpolation(targetPoints[t].x, targetPoints[t].y, msrdPts, sourcePoints.Count, number, scope);
                        result.Add(result0ne);
                        break;
                    //径向基函数插值
                    case EnumInterpWay.RBF:
                        result0ne = RBF_Interpolation(targetPoints[t].x, targetPoints[t].y, msrdPts, sourcePoints.Count, number, scope);
                        result.Add(result0ne);
                        break;
                    //薄板插值(较小数字测试可以，换到这里相差较大的数时有问题，不好用，别用)
                    case EnumInterpWay.THIN:
                        result0ne = ThinPlate_Interpolation(targetPoints[t].x, targetPoints[t].y, msrdPts, sourcePoints.Count, number, scope);
                        result.Add(result0ne);
                        break;
                    default:
                        IList<double> error = new List<double>();
                        return error;
                }

            }
            return result;

        }
        public IList<double> doIDW(IList<Points> sourcePoints, IList<Points> targetPoints, int scope)
        {
            List<double> result = new List<double>();
            // 原始钻孔数据
            double[,] msrdPts = new double[sourcePoints.Count, 3];
            for (int j = 0; j < sourcePoints.Count; j++)
            {
                msrdPts[j, 0] = sourcePoints[j].x;
                msrdPts[j, 1] = sourcePoints[j].y;
                msrdPts[j, 2] = sourcePoints[j].z;
            }
            //得到targetPoints的插值结果
            for (int t = 0; t < targetPoints.Count; t++)
            {
                double result0ne = IDW_Interpolation(targetPoints[t].x, targetPoints[t].y, msrdPts, sourcePoints.Count, number, scope);
                result.Add(result0ne);
            }
            return result;
        }

        /// <summary>
        /// IDW插值方法
        /// </summary>
        /// <param name="x">预测点X</param>
        /// <param name="y">预测点Y</param>
        /// <param name="datasource">已知点（包含XYZ）</param>
        /// <param name="pointcount">已知点的个数</param>
        /// <param name="number">选取周围最近点的个数</param>
        /// <param name="scope">半径范围</param>
        /// <returns></returns>
        public double IDW_Interpolation(double x, double y, double[,] datasource, int pointcount, int number, int scope)
        {
            double fz = 0.0, fm = 0.0, pfh = 0.0;
            double[,] ds = datasource;

            Dictionary<int, double> distanceDictionary = new Dictionary<int, double>();
            Dictionary<int, double> aziDictionary = new Dictionary<int, double>();
            List<int> closestIndex = new List<int>();
            List<double> closestAzi = new List<double>();
            // 算每个原始点与待求点的距离值，算每个点和与待求点的方位角
            for (int i = 0; i < pointcount; i++)
            {
                distanceDictionary.Add(i, Math.Sqrt(distance(ds[i, 0], ds[i, 1], x, y)));
                aziDictionary.Add(i, azimuth(ds[i, 0], ds[i, 1], x, y));
            }
            // 选出最近的Number个点
            for (int i = 0; i < number;)
            {
                if (distanceDictionary.Count <= 0) break;
                double minDistance = distanceDictionary.Min(myDictionary => myDictionary.Value);
                int minIndex = distanceDictionary.FirstOrDefault(index => index.Value == minDistance).Key;
                // 扣除正负角相差不到10°的
                double azi = -99;
                aziDictionary.TryGetValue(minIndex, out azi);
                // 只在半径scope做搜索
                if (minDistance > scope) break;
                if (!isIn10Degrees(azi, closestAzi))
                {
                    closestIndex.Add(minIndex);
                    closestAzi.Add(azi);
                    i++;
                }
                distanceDictionary.Remove(minIndex);
            }
            //if (closestIndex.Count() == 0)
            //    Console.WriteLine("1");
            for (int i = 0; i < pointcount; i++)
            {
                if (closestIndex.Contains(i))
                {
                    pfh = distance(ds[i, 0], ds[i, 1], x, y);
                    pfh = 1 / pfh;
                    fz += ds[i, 2] * pfh;
                    fm += pfh;
                }
            }
            return fz / fm;
        }
        private double distance(double x, double y, double x0, double y0)
        {
            double m = x - x0, n = y - y0;
            m = m * m;
            n = n * n;
            return m + n;
        }
        private double azimuth(double x, double y, double x0, double y0)
        {
            double deltax = x - x0, deltay = y - y0;
            double radian = Math.Atan2(deltax, deltay);
            if (radian < 0)
                radian = Math.PI * 2 + radian;
            double degree = radian * 180 / Math.PI;
            return degree;
        }
        private bool isIn10Degrees(double azi0, List<double> aziList)
        {
            bool flag = false;
            foreach (var azi in aziList)
            {
                if (Math.Abs(azi - azi0) < 180)
                {
                    if (Math.Abs(azi - azi0) < 1)
                        return true;
                }
                else
                {
                    if (360 - Math.Abs(azi - azi0) < 1)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// IDW二维插值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public  double IdwInterpolate2d(double x, double y, List<Points> source)
        {
            double sum1 = 0;
            double sum2 = 0;

            for (int j = 0; j < source.Count; j++)
            {
                double distance = getDistance(x, y, source[j].x, source[j].y);
                if (distance == 0) continue;
                sum1 += source[j].v / (distance * distance);
                sum2 += 1 / (distance * distance);
            }
            
            double value = sum1 / sum2;

            return value;
        }

        /// <summary>
        /// 求二维平面距离
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <returns></returns>
        private static double getDistance(double x0, double y0, double x1, double y1)
        {
            return Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
        }

        public IList<double> doRBF(IList<Points> sourcePoints, IList<Points> targetPoints, int scope)
        {
            List<double> result = new List<double>();
            // 原始钻孔数据
            double[,] msrdPts = new double[sourcePoints.Count, 3];
            for (int j = 0; j < sourcePoints.Count; j++)
            {
                msrdPts[j, 0] = sourcePoints[j].x;
                msrdPts[j, 1] = sourcePoints[j].y;
                msrdPts[j, 2] = sourcePoints[j].z;
            }
            //得到targetPoints的插值结果
            for (int t = 0; t < targetPoints.Count; t++)
            {
                double result0ne = RBF_Interpolation(targetPoints[t].x, targetPoints[t].y, msrdPts, sourcePoints.Count, number, scope);
                result.Add(result0ne);
            }
            return result;
        }

        /// <summary>
        ///RBF插值方法2.0（选定一定范围的若干点）
        /// </summary>
        /// <param name="x">预测点X</param>
        /// <param name="y">预测点Y</param>
        /// <param name="datasource">已知点（包含XYZ）</param>
        /// <param name="pointcount">已知点的个数</param>
        /// <param name="number">选取周围最近点的个数</param>
        /// <param name="scope">半径范围</param>
        /// <returns></returns>
        public double RBF_Interpolation(double x, double y, double[,] datasource, int pointcount, int number, int scope)
        {
            double fz = 0.0, fm = 0.0, pfh = 0.0;
            double[,] ds = datasource;
            List<double> inputTXY = new List<double>();
            inputTXY.Add(x);
            inputTXY.Add(y);
            Dictionary<int, double> distanceDictionary = new Dictionary<int, double>();
            Dictionary<int, double> aziDictionary = new Dictionary<int, double>();
            List<int> closestIndex = new List<int>();
            List<double> closestAzi = new List<double>();
            // 算每个原始点与待求点的距离值，算每个点和与待求点的方位角
            for (int i = 0; i < pointcount; i++)
            {
                distanceDictionary.Add(i, Math.Sqrt(distance(ds[i, 0], ds[i, 1], x, y)));
                aziDictionary.Add(i, azimuth(ds[i, 0], ds[i, 1], x, y));
            }
            // 选出最近的Number个点
            for (int i = 0; i < number;)
            {
                if (distanceDictionary.Count <= 0) break;
                double minDistance = distanceDictionary.Min(myDictionary => myDictionary.Value);
                int minIndex = distanceDictionary.FirstOrDefault(index => index.Value == minDistance).Key;
                // 扣除正负角相差不到10°的
                double azi = -99;
                aziDictionary.TryGetValue(minIndex, out azi);
                // 只在半径scope做搜索
                if (minDistance > scope) break;
                if (!isIn10Degrees(azi, closestAzi))
                {
                    closestIndex.Add(minIndex);
                    closestAzi.Add(azi);
                    i++;
                }
                distanceDictionary.Remove(minIndex);
            }
            //if (closestIndex.Count() == 0)
            //    Console.WriteLine("1");
            List<double> knownPointsx = new List<double>();
            List<double> knownPointsy = new List<double>();
            List<double> knownPointsz = new List<double>();
            for (int i = 0; i < pointcount; i++)
            {
                if (closestIndex.Contains(i))
                {
                    knownPointsx.Add(ds[i, 0]);
                    knownPointsy.Add(ds[i, 1]);
                    knownPointsz.Add(ds[i, 2]);
                }
            }
            double oneResult = RBFInterpolation(knownPointsx, knownPointsy, knownPointsz, inputTXY);
            return oneResult;
        }
        public static double RBFInterpolation(List<double> knownPointsx, List<double> knownPointsy, List<double> knownPointsz, List<double> predictPoint)
        {
            // 计算距离权重矩阵
            int n = knownPointsx.Count;
            Matrix<double> A = Matrix<double>.Build.Dense(n + 1, n + 1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double distance = Math.Sqrt(Math.Pow(knownPointsx[i] - knownPointsx[j], 2) + Math.Pow(knownPointsy[i] - knownPointsy[j], 2));
                    A[i, j] = RBF(distance);
                }
                A[i, n] = 1;
                A[n, i] = 1;
            }
            A[n, n] = 0;

            // 计算系数矩阵
            Vector<double> b = Vector<double>.Build.Dense(n + 1);
            for (int i = 0; i < n; i++)
            {
                b[i] = knownPointsz[i];
            }
            b[n] = 0;
            //这里指求A（已知距离权重矩阵）X=B中的X
            Vector<double> x = A.Solve(b);

            // 计算预测值
            double predictValue = 0;
            for (int i = 0; i < n; i++)
            {
                double distance = Math.Sqrt(Math.Pow(predictPoint[0] - knownPointsx[i], 2) + Math.Pow(predictPoint[1] - knownPointsy[i], 2));
                //使用上述求出的系数矩阵解X来进一步计算C=A（未知距离权重矩阵）X
                predictValue += RBF(distance) * x[i];
            }
            predictValue += x[n];
            return predictValue;
        }
        /// <summary>
        /// RBF核函数
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        static double RBF(double r)
        {
            // linear函数
            //return r;
            // 高斯函数
            return Math.Exp(-r * r);

        }
        /// <summary>
        /// RBF插值核心算法1.0（有问题，未使用）
        /// </summary>
        /// <param name="inputXY">已知点的xy值</param>
        /// <param name="inputZ">已知点的z值</param>
        /// <param name="numberNodes">已知点的个数</param>
        /// <param name="inputTXY">预测点的xy值</param>
        /// <param name="tPoints">预测点的个数</param>
        /// <returns></returns>
        //public static List<double> RBF_Interpolation(List<double> inputXY, List<double> inputZ, int numberNodes, List<double> inputTXY, int tPoints)
        //{
        //    double RBFConstant;
        //    int i;
        //    int j;

        //    double maxx;
        //    double maxy;
        //    double minx;
        //    double miny;
        //    maxx = minx = inputXY[0];
        //    maxy = miny = inputXY[numberNodes];
        //    for (i = 0; i < numberNodes; i++)
        //    {
        //        if (inputXY[i] > maxx)
        //            maxx = inputXY[i];
        //        if (inputXY[i] < minx)
        //            minx = inputXY[i];
        //        if (inputXY[i + numberNodes] > maxy)
        //            maxy = inputXY[i + numberNodes];
        //        if (inputXY[i + numberNodes] < miny)
        //            miny = inputXY[i + numberNodes];

        //    }
        //    RBFConstant = Math.Pow(((maxx - minx) * (maxy - miny) / numberNodes), 0.5);
        //    double[] A = new double[numberNodes * numberNodes];
        //    double[] P = new double[3 * numberNodes];
        //    double[] AA = new double[(3 + numberNodes) * (3 + numberNodes)];
        //    double norm1;
        //    double norm2;
        //    double temp;
        //    double r = 0;
        //    for (i = 0; i < numberNodes * numberNodes; i++)
        //    {
        //        A[i] = 0;
        //    }
        //    for (i = 0; i < numberNodes; i++)
        //    {
        //        for (j = 0; j <= i; j++)
        //        {
        //            norm1 = inputXY[i] - inputXY[j];
        //            norm2 = inputXY[numberNodes + i] - inputXY[numberNodes + j];
        //            r = 0;
        //            r = norm1 * norm1 + norm2 * norm2;
        //            r = Math.Pow(r, 0.5);
        //            temp = Math.Exp(-0.5 * r * r / (RBFConstant * RBFConstant));
        //            A[i * numberNodes + j] = temp;
        //            A[j * numberNodes + i] = temp; //???
        //        }
        //    }
        //    for (i = 0; i < 3 * numberNodes; i += 3)
        //    {
        //        P[i] = 1;
        //        P[i + 1] = inputXY[i / 3];
        //        P[i + 2] = inputXY[numberNodes + i / 3];
        //    }
        //    for (i = 0; i < numberNodes; i++) //x
        //    {
        //        for (j = 0; j < numberNodes; j++) //y
        //        {
        //            AA[j * (numberNodes + 3) + i] = A[j * numberNodes + i];
        //        }
        //    }

        //    for (i = numberNodes; i < numberNodes + 3; i++)
        //    {
        //        for (j = 0; j < numberNodes; j++)
        //        {
        //            AA[j * (numberNodes + 3) + i] = P[j * 3 + i - numberNodes]; //??
        //        }
        //    }

        //    for (i = 0; i < numberNodes; i++)
        //    {
        //        for (j = numberNodes; j < numberNodes + 3; j++)
        //        {
        //            AA[j * (numberNodes + 3) + i] = P[i * 3 + j - numberNodes];
        //        }
        //    }
        //    P = null;
        //    for (i = numberNodes; i < numberNodes + 3; i++)
        //    {
        //        for (j = numberNodes; j < numberNodes + 3; j++)
        //        {
        //            AA[j * (numberNodes + 3) + i] = 0;
        //        }
        //    }

        //    double[] btemp = new double[numberNodes + 3];
        //    for (i = 0; i < numberNodes; i++)
        //    {
        //        btemp[i] = inputZ[i];
        //    }

        //    for (i = numberNodes; i < numberNodes + 3; i++)
        //    {
        //        btemp[i] = 0;
        //    }
        //    DenseMatrix b = new DenseMatrix(1, numberNodes + 3);
        //    b.SetRow(0, btemp);

        //    DenseMatrix Aa = new DenseMatrix(numberNodes + 3, numberNodes + 3);

        //    int sb = 0;
        //    int st = 0;
        //    for (j = 0; j < numberNodes + 3; j++)
        //    {
        //        for (i = 0; i < numberNodes + 3; i++)
        //        {
        //            if (st == 0)
        //            {
        //                Aa[i, j] = AA[0];
        //                st = 1;
        //            }
        //            else
        //            {
        //                for (int n = sb + 1; n < AA.Length; n++)
        //                {
        //                    Aa[i, j] = AA[n];
        //                    sb = n;
        //                    st = 1;
        //                    break;
        //                }
        //            }
        //        }

        //    }

        //    var AAA = (DenseMatrix)Aa.Inverse();
        //    var Y = b * AAA; 
        //    List<double> output = new List<double>();
        //    for (int t = 0; t < tPoints; t++)
        //    {
        //        output.Add(0);
        //    }
        //    for (i = 0; i < tPoints; i++)
        //        output[i] = 0; //initialize output
        //    double[] R = new double[2 * numberNodes];
        //    double[] R_ = new double[numberNodes];
        //    double[] feval = new double[numberNodes];
        //    double s = 0;
        //    for (i = 0; i < tPoints; i++)
        //    {
        //        s = 0;
        //        for (j = 0; j < numberNodes; j++)
        //        {

        //            R[j] = inputTXY[i];
        //            R[numberNodes + j] = inputTXY[tPoints + i];
        //        }
        //        for (j = 0; j < numberNodes; j++)
        //        {
        //            R[j] = R[j] - inputXY[j];
        //            R[j] = R[j] * R[j];
        //            R[numberNodes + j] = R[numberNodes + j] - inputXY[numberNodes + j];
        //            R[numberNodes + j] = R[numberNodes + j] * R[numberNodes + j];
        //        }
        //        for (j = 0; j < numberNodes; j++)
        //        {
        //            R_[j] = Math.Pow((R[j] + R[j + numberNodes]), 0.5);
        //        }
        //        for (j = 0; j < numberNodes; j++)
        //        {
        //            feval[j] = Math.Exp(-0.5 * R_[j] * R_[j] / (RBFConstant * RBFConstant));
        //        }
        //        for (j = 0; j < numberNodes; j++)
        //        {
        //            s = s + Y[0, j] * feval[j];

        //        }
        //        s = s + Y[0, numberNodes];
        //        s = s + Y[0, numberNodes + 1] * inputTXY[i];
        //        s = s + Y[0, numberNodes + 2] * inputTXY[tPoints + i];
        //        output[i] = s;
        //    }

        //    AA = null;
        //    btemp = null;
        //    b = null;
        //    Aa.Clear();
        //    Y = null;
        //    A = null;
        //    R = null;
        //    R_ = null;
        //    feval = null;
        //    return output;
        //}


        public IList<double> doThinPlate(IList<Points> sourcePoints, IList<Points> targetPoints, int scope)
        {
            List<double> result = new List<double>();
            // 原始钻孔数据
            double[,] msrdPts = new double[sourcePoints.Count, 3];
            for (int j = 0; j < sourcePoints.Count; j++)
            {
                msrdPts[j, 0] = sourcePoints[j].x;
                msrdPts[j, 1] = sourcePoints[j].y;
                msrdPts[j, 2] = sourcePoints[j].z;
            }
            //得到targetPoints的插值结果
            for (int t = 0; t < targetPoints.Count; t++)
            {
                double result0ne = ThinPlate_Interpolation(targetPoints[t].x, targetPoints[t].y, msrdPts, sourcePoints.Count, number, scope);
                result.Add(result0ne);
            }
            return result;
        }
        /// <summary>
        /// 薄板插值方法
        /// </summary>
        /// <param name="x">预测点X</param>
        /// <param name="y">预测点Y</param>
        /// <param name="datasource">已知点（包含XYZ）</param>
        /// <param name="pointcount">已知点的个数</param>
        /// <param name="number">选取周围最近点的个数</param>
        /// <param name="scope">半径范围</param>
        /// <returns></returns>
        public double ThinPlate_Interpolation(double x, double y, double[,] datasource, int pointcount, int number, int scope)
        {
            double fz = 0.0, fm = 0.0, pfh = 0.0;
            double[,] ds = datasource;
            List<double> inputTXY = new List<double>();
            inputTXY.Add(x);
            inputTXY.Add(y);
            Dictionary<int, double> distanceDictionary = new Dictionary<int, double>();
            Dictionary<int, double> aziDictionary = new Dictionary<int, double>();
            List<int> closestIndex = new List<int>();
            List<double> closestAzi = new List<double>();
            // 算每个原始点与待求点的距离值，算每个点和与待求点的方位角
            for (int i = 0; i < pointcount; i++)
            {
                distanceDictionary.Add(i, Math.Sqrt(distance(ds[i, 0], ds[i, 1], x, y)));
                aziDictionary.Add(i, azimuth(ds[i, 0], ds[i, 1], x, y));
            }
            // 选出最近的Number个点
            for (int i = 0; i < number;)
            {
                if (distanceDictionary.Count <= 0) break;
                double minDistance = distanceDictionary.Min(myDictionary => myDictionary.Value);
                int minIndex = distanceDictionary.FirstOrDefault(index => index.Value == minDistance).Key;
                // 扣除正负角相差不到10°的
                double azi = -99;
                aziDictionary.TryGetValue(minIndex, out azi);
                // 只在半径scope做搜索
                if (minDistance > scope) break;
                if (!isIn10Degrees(azi, closestAzi))
                {
                    closestIndex.Add(minIndex);
                    closestAzi.Add(azi);
                    i++;
                }
                distanceDictionary.Remove(minIndex);
            }
            //if (closestIndex.Count() == 0)
            //    Console.WriteLine("1");
            List<double> knownPointsx = new List<double>();
            List<double> knownPointsy = new List<double>();
            List<double> knownPointsz = new List<double>();
            for (int i = 0; i < pointcount; i++)
            {
                if (closestIndex.Contains(i))
                {
                    knownPointsx.Add(ds[i, 0]);
                    knownPointsy.Add(ds[i, 1]);
                    knownPointsz.Add(ds[i, 2]);
                }
            }
            double oneResult = ThinPlateSplineInterpolation(knownPointsx, knownPointsy, knownPointsz, inputTXY);
            return oneResult;
        }
        /// <summary>
        /// 薄板插值
        /// </summary>
        /// <param name="knownPointsx">list：x</param>
        /// <param name="knownPointsy">list：y</param>
        /// <param name="knownPointsz">list：z</param>
        /// <param name="predictPoint">待预测点</param>
        /// <returns></returns>
        public static double ThinPlateSplineInterpolation(List<double> knownPointsx, List<double> knownPointsy, List<double> knownPointsz, List<double> predictPoint)
        {
            int n = knownPointsx.Count;
            var P = Matrix<double>.Build.Dense(n, 3, 1.0);
            for (int i = 0; i < n; i++)
            {
                P[i, 0] = knownPointsx[i];
                P[i, 1] = knownPointsy[i];
            }

            var K = Matrix<double>.Build.Dense(n, n, 0.0);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        K[i, j] = 0.0;
                    }
                    else
                    {
                        var dist = Math.Sqrt(Math.Pow(P[i, 0] - P[j, 0], 2.0) + Math.Pow(P[i, 1] - P[j, 1], 2.0));
                        K[i, j] = ThinPlateSpline(dist);
                    }
                }
            }

            var V = Vector<double>.Build.Dense(n, 1.0);
            for (int i = 0; i < n; i++)
            {
                V[i] = knownPointsz[i];
            }

            var Y = Vector<double>.Build.Dense(3, 1.0);
            Y[0] = predictPoint[0];
            Y[1] = predictPoint[1];

            var L = Matrix<double>.Build.Dense(n, n, 0.0);
            L.SetSubMatrix(0, 0, K);
            L.SetColumn(n - 1, P.Column(2));
            Matrix<double> m = P.Column(2).ToRowMatrix();
            Matrix<double> mt = m.Transpose();
            Vector<double> vt = m.Row(0);
            L.SetRow(n - 1, vt);
            L[n - 1, n - 1] = 0.0;

            var f = Vector<double>.Build.Dense(n, 1.0);
            f.SetSubVector(0, n, V);
            f[n - 1] = 0.0;

            var p = L.Inverse() * f;

            double result = 0.0;
            for (int i = 0; i < n; i++)
            {
                var dist = Math.Sqrt(Math.Pow(P[i, 0] - Y[0], 2.0) + Math.Pow(P[i, 1] - Y[1], 2.0));
                result += p[i] * ThinPlateSpline(dist);
            }

            return result + p[n - 1];
        }
        /// <summary>
        /// 薄板核函数
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private static double ThinPlateSpline(double r)
        {
            if (r == 0.0)
            {
                return 0.0;
            }
            else
            {
                return Math.Pow(r, 2.0) * Math.Log(r);
            }
        }



    }
    public enum EnumInterpWay
    {
        //反距离权重插值
        IDW,
        //径向基函数插值
        RBF,
        //薄板插值
        THIN
    }
}
