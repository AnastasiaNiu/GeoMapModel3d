using CommonMethodHelpLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonDataStructureLib;
using NetTopologySuite.Geometries;
using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Operation.Polygonize;

namespace GeoMapModelLib
{
    public  class OrigonLineHelper
    {
        GeoSortHelper sort = new GeoSortHelper();
        ConvertGeometriesHelper convert = new ConvertGeometriesHelper();

        public FeatureCollection AutoCreateOriLine(ShapeFileHelper stratumLayer, ShapeFileHelper faultLayer, double xMax, double xMin, double yMin, double yMax, string direction, double stepLength, string savePath)
        {

            //生成范围面
            List<double> result = new List<double>();

            object o = Type.Missing;

            //左边界 
            var coor1 = new Coordinate[2];
            coor1[0] = new Coordinate(xMin, yMax);
            coor1[1] = new Coordinate(xMin, yMin);


            LineString leftBoundary = new LineString(coor1);



            //右边界
            var coor2 = new Coordinate[2];
            coor2[0] = new Coordinate(xMax, yMax);
            coor2[1] = new Coordinate(xMax, yMin);


            LineString rightBoundary = new LineString(coor2);


            //上边界
            var coor3 = new Coordinate[2];
            coor3[0] = new Coordinate(xMin, yMax);
            coor3[1] = new Coordinate(xMax, yMax);


            LineString topBoundary = new LineString(coor3);

            //下边界
            var coor4 = new Coordinate[2];
            coor4[0] = new Coordinate(xMin, yMin);
            coor4[1] = new Coordinate(xMax, yMin);

            LineString downBoundary = new LineString(coor4);

            //对边界线进行自相交判断
            //构建几何对象
            List<Geometry> geomes = new List<Geometry>()
            {
                topBoundary, rightBoundary, downBoundary, leftBoundary
            };


            // 验证边线段是否有效和非自交，并输出修复后的几何对象
            List<Geometry> validatedGeometries = new List<Geometry>();
            foreach (Geometry geometry in geomes)
            {
                if (!geometry.IsValid)
                {
                    geometry.Buffer(0);
                }
                validatedGeometries.Add(geometry);
            }

            // 创建 Polygonizer（多边形器）
            var polygonizer = new Polygonizer();

            // 添加几何对象
            foreach (Geometry geometry in validatedGeometries)
            {
                polygonizer.Add(geometry);
            }

            // 获取多边形对象
            var bondaryOfRegion = polygonizer.GetPolygons();


            //获取与面相交的线的点合集
            for (int i = 0; i < stratumLayer.pFeatureCollection.Count; i++)
            {
                IFeature structLineF = stratumLayer.pFeatureCollection[i];
                foreach (IGeometry envo in bondaryOfRegion)
                {
                    IGeometry tempLine = structLineF.Geometry;

                    //线与面有交点
                    if (tempLine.Intersects(envo))
                    {
                        List<Point> tempPoints = convert.ConvertLineToPoints(tempLine);
                        //按斜率简化折点
                        List<Point> newVertexs = sort.SimplifyVertexBySlope(tempPoints, 20);

                        //方法二：直接采用简化线的方式
                        //----------------------------------------------------
                        //DouglasPeuckerSimplifier simplifier = new DouglasPeuckerSimplifier(tempLine);
                        ////设置简化的距离阈值
                        //simplifier.DistanceTolerance = 0.5;
                        ////执行线要素简化
                        //IGeometry simplifiedGeometry = simplifier.GetResultGeometry();
                        //GeometryFactory geofactory = new GeometryFactory();
                        //foreach(var onecoordinate in simplifiedGeometry.Coordinates)
                        //{
                        //    newVertexs.Add((Point)geofactory.CreatePoint(onecoordinate));
                        //}
                        //-----------------------------------------------------

                        //按direction将点的direction方向的值排序，并剔除在范围外的点
                        result.AddRange(sort.BubbleSortCollectionByDirection(newVertexs, xMin, xMax, yMin, yMax, direction));
                    }

                    else
                    {
                        continue;
                    }
                }



            }
            if (faultLayer.pFeatureCollection != null)
            {
                for (int i = 0; i < faultLayer.pFeatureCollection.Count; i++)
                {
                    IFeature structLineF = faultLayer.pFeatureCollection[i];
                    foreach (IGeometry envo in bondaryOfRegion)
                    {
                        IGeometry tempLine = structLineF.Geometry;

                        if (tempLine.Intersects(envo))
                        {
                            List<Point> tempPoints = convert.ConvertLineToPoints(tempLine);
                            //按斜率简化折点
                            List<Point> newVertexs = sort.SimplifyVertexBySlope(tempPoints, 0.08);
                            //按direction将点的direction方向的值排序，并剔除在范围外的点
                            result.AddRange(sort.BubbleSortCollectionByDirection(newVertexs, xMin, xMax, yMin, yMax, direction));
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            result = sort.BubbleSortDoubleList(result);
            //生成剖面线合集
            var origonlines=CreateSectionPolyLine(result, xMax, xMin, yMin, yMax, direction, stepLength);
            //输出剖面线
            OrigonLine featureLine = new OrigonLine();
            var featurecollectionlines=featureLine.CreateSectionPolyLineFeatureLayer(origonlines,savePath);
            return featurecollectionlines;
        }
        /// <summary>
        /// 按最小间距step抽稀剖面线集合
        /// </summary>
        /// <param name="originList"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        private List<double> ResetListbyStep(List<double> originList, int step)
        {
            List<double> result = new List<double>();
            result.Add(originList[0]);
            for (int i = 0; i < originList.Count - 1; i++)
            {
                int n = result.Count;
                if ((originList[i + 1] - result[n - 1]) >= step)
                {
                    result.Add(originList[i + 1]);
                }
            }
            return result;

        }
        /// <summary>
        /// 生成剖面线合集
        /// </summary>
        /// <param name="origonLineList"></param>
        /// <returns></returns>
        public List<OrigonLine> CreateSectionPolyLine(List<double> origonLineList, double xMax, double xMin, double yMin, double yMax, string direction, double stepLength)
        {
            List<OrigonLine> result = new List<OrigonLine>();
            List<double> newOrigonList = new List<double>();
            for (double x = xMin; x <= xMax; x += stepLength)
            {
                origonLineList.Add(x);
            }
            //这里列表去重复值list有专门函数
            //--------------------------------------------------
            ////排序
            //input.Sort();
            ////去重复值
            //List<double> resultList = input.Distinct().ToList();
            //-----------------------------------------------------
            
            origonLineList = sort.BubbleSortDoubleList(origonLineList);
            origonLineList = ResetListbyStep(origonLineList, 1);

            for (int i = 0; i < origonLineList.Count; i++)
            {

                object o = Type.Missing;
                // 线段轨迹
                List<Point> vituralpoints = new List<Point>();

                Point point1 = new Point(origonLineList[i], yMin);
                Point point2 = new Point(origonLineList[i], yMax);
                point1.X = origonLineList[i];
                point1.Y = yMin;
                point2.X = origonLineList[i];
                point2.Y = yMax;
                vituralpoints.Add(point1);
                vituralpoints.Add(point2);
                
                LineString sectionLine = convert.ConvertPointsToLine(vituralpoints); ;
                OrigonLine tempOA = new OrigonLine(i, sectionLine, direction);
                result.Add(tempOA);
            }

            return result;
        }
    }
}
