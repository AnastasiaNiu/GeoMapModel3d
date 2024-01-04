using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonDataStructureLib.RockStratumModel;

namespace CommonMethodHelpLib
{
    public  class GeoSortHelper
    {
        /// <summary>
        /// 按地层线折点间的斜率简化地层线的折点
        /// </summary>
        /// <param name="vertexs"></param>
        /// <param name="thresholdSlope"></param>
        /// <returns></returns>
        public List<Point> SimplifyVertexBySlope(List<Point> vertexs, double thresholdSlope)
        {
            if (vertexs.Count <= 2)
            {
                return vertexs;
            }
            List<Point> result = new List<Point>();
            object a = Type.Missing;
            Point fromPoint = vertexs[0];
            result.Add(fromPoint);
            Point fromPoint1 = vertexs[1];
            result.Add(fromPoint1);
            double dx = fromPoint1.X - fromPoint.X;
            double dy = fromPoint1.Y - fromPoint.Y;
            double slopeLast = 0;
            if (dx == 0)
            {
                slopeLast = 10086;

            }
            else
            {
                slopeLast = dy / dx;
            }
            for (int i = 2; i < vertexs.Count; i++)
            {
                Point tempPoint = vertexs[i];
                Point lastPoint = result[result.Count - 1];
                dx = tempPoint.X - lastPoint.X;
                dy = tempPoint.Y - lastPoint.Y;
                if (dx == 0 || slopeLast == 10086)
                {
                    result.Add(tempPoint);

                }
                else
                {
                    double tempSlope = dy / dx;
                    double dS = Math.Abs(tempSlope - slopeLast);
                    if (dS > thresholdSlope)
                    {
                        result.Add(tempPoint);
                        slopeLast = tempSlope;
                    }
                    else
                    {
                        continue;
                    }


                }
            }


            return result;
        }
        /// <summary>
        /// 按direction 排序折点
        /// </summary>
        /// <param name="tempPoints"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public List<double> BubbleSortCollectionByDirection(List<Point> tempPoints, double xMin, double xMax, double yMin, double yMax, String direction)
        {
            List<IPoint> resultPoints = new List<IPoint>();
            List<double> result = new List<double>();
            for (int i = 0; i < tempPoints.Count; i++)
            {

                IPoint temp = tempPoints[i];
                if (temp.X < xMin || temp.X > xMax || temp.Y < yMin || temp.Y > yMax)
                {
                    continue;
                }
                resultPoints.Add(temp);
            }
            if (resultPoints == null)
            {

                return result;
            }
            else if (resultPoints.Count == 1)
            {
                if (direction == "X")
                {
                    result.Add(resultPoints[0].X);
                }
                else
                {

                    result.Add(resultPoints[0].Y);

                }
                return result;


            }
            else
            {
                int n = resultPoints.Count;
                if (direction == "X")
                {
                    for (int i = 0; i < n; i++)
                    {
                        result.Add(resultPoints[i].X);
                    }
                    int w = result.Count;
                    for (int i = 0; i < w; i++)
                    {
                        for (int j = 0; j < w - i - 1; j++)
                        {
                            double j1x = Math.Floor(result[j + 1] * 1000) / 1000;
                            double j0x = Math.Floor(result[j] * 1000) / 1000;

                            if (j1x <= j0x)
                            {
                                result[j] = j1x;
                                result[j + 1] = j0x;

                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        result.Add(resultPoints[i].Y);
                    }
                    int w = result.Count;
                    for (int i = 0; i < w; i++)
                    {
                        for (int j = 0; j < w - i - 1; j++)
                        {
                            double j1y = Math.Floor(result[j + 1] * 1000) / 1000;
                            double j0y = Math.Floor(result[j] * 1000) / 1000;

                            if (j1y <= j0y)
                            {
                                result[j] = j1y;
                                result[j + 1] = j0y;

                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                }

                return result;
            }


        }
        /// <summary>
        /// 排序并去重
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<double> BubbleSortDoubleList(List<double> input)
        {
            List<double> temp = input;
            List<double> result = new List<double>();

            for (int i = 0; i < temp.Count; i++)
            {
                for (int j = 0; j < temp.Count - i - 1; j++)
                {
                    double j1x = Math.Floor(temp[j + 1] * 1000) / 1000;
                    double j0x = Math.Floor(temp[j] * 1000) / 1000;

                    if (j1x <= j0x)
                    {
                        temp[j] = j1x;
                        temp[j + 1] = j0x;

                    }
                    else
                    {
                        continue;
                    }
                }
            }

            result.Add(temp[0]);
            for (int i = 0; i < temp.Count - 1; i++)
            {
                if (temp[i] != temp[i + 1])
                {
                    result.Add(temp[i + 1]);
                }
                else
                {
                    continue;
                }
            }
            return result;
        }
        /// <summary>
        /// 对StratigraphicAttribute_Point列排序并去重
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public List<StratigraphicAttribute_Point> bubbleSort(List<StratigraphicAttribute_Point> points)
        {
            List<StratigraphicAttribute_Point> resultPoints = points;

            if (resultPoints == null || resultPoints.Count < 2)
            {
                return resultPoints;
            }

            int n = resultPoints.Count;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    double j1x = Math.Floor(resultPoints[j + 1].point.X * 1000) / 1000;
                    double j0x = Math.Floor(resultPoints[j].point.X * 1000) / 1000;
                    double j1y = Math.Floor(resultPoints[j + 1].point.Y * 1000) / 1000;
                    double j0y = Math.Floor(resultPoints[j].point.Y * 1000) / 1000;

                    if (j1x < j0x)
                    {
                        double tx = resultPoints[j].point.X;
                        double ty = resultPoints[j].point.Y;
                        Point t = new Point(new Coordinate(tx, ty));

                        string tLinetype = resultPoints[j].lineType;
                        int tLineid = resultPoints[j].id;
                        resultPoints[j] = new StratigraphicAttribute_Point(resultPoints[j + 1].point, resultPoints[j + 1].lineType, resultPoints[j + 1].id);
                        resultPoints[j + 1] = new StratigraphicAttribute_Point(t, tLinetype, tLineid);

                    }
                    else if (j1x == j0x)
                    {
                        if (j1y < j0y)
                        {
                            double tx = resultPoints[j].point.X;
                            double ty = resultPoints[j].point.Y;
                            Point t = new Point(new Coordinate(tx, ty));
                            string tLinetype = resultPoints[j].lineType;
                            int tLineid = resultPoints[j].id;
                            resultPoints[j] = new StratigraphicAttribute_Point(resultPoints[j + 1].point, resultPoints[j + 1].lineType, resultPoints[j + 1].id);

                            resultPoints[j + 1] = new StratigraphicAttribute_Point(t, tLinetype, tLineid);


                        }
                    }
                }
            }

            return resultPoints;
        }
    }
}
