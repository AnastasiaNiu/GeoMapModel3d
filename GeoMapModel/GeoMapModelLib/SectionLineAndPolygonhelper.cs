using CommonDataStructureLib;
using CommonMethodHelpLib;
using GeoAPI.Geometries;
using InterpolateLib;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Polygonize;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static CommonDataStructureLib.RockStratumModel;

namespace GeoMapModelLib
{
    public  class SectionLineAndPolygonhelper
    {
        public double XMin = 0.0;
        public double XMax = 0.0;
        public double YMin = 0.0;
        public double YMax = 0.0;
        public string Direction = "";

        public string CreateSectionLineAndPolygon(FeatureCollection  origonLinesLayer, RasterHelper dem, ShapeFileHelper altitudePoint, ShapeFileHelper contourLine, ShapeFileHelper faultLine, ShapeFileHelper stratumLine, ShapeFileHelper stratumGroup, string savePath, double sectionDepth, double elevSampleStep, int zZoom, int scale) 
        {
            //---1、确定范围
            //获取第一条剖面线
            IFeature origonPolylineFeature0 = origonLinesLayer[0];
            LineString origonLine0 = origonPolylineFeature0.Geometry as LineString;
            IAttributesTable attributesTable0 = origonPolylineFeature0.Attributes;
            int oriNum = origonLinesLayer.Count;
            //获取最后一条剖面线
            IFeature origonPolylineFeatureNum = origonLinesLayer[oriNum - 1];
            LineString origonLineNum = origonPolylineFeatureNum.Geometry as LineString;
            //确定xyminmax
            double xmin;
            double xmax;
            double ymin;
            double ymax;

            if (attributesTable0.Exists("x"))
            {
                this.Direction = "X";
                xmin = origonLine0.StartPoint.X;
                xmax = origonLineNum.StartPoint.X;
                ymin = origonLine0.StartPoint.Y;
                ymax = origonLine0.EndPoint.Y;
            }
            else
            {
                this.Direction = "Y";
                xmin = origonLine0.StartPoint.X;
                xmax = origonLine0.EndPoint.X;
                ymin = origonLineNum.StartPoint.Y;
                ymax = origonLine0.StartPoint.Y;
            }
            if (ymin <= ymax)
            {
                this.YMin = ymin;
                this.YMax = ymax;
            }
            else
            {
                this.YMin = ymax;
                this.YMax = ymin;

            }
            if (xmin <= xmax)
            {
                this.XMin = xmin;
                this.XMax = xmax;
            }
            else
            {
                this.XMin = xmax;
                this.XMax = xmin;
            }
            //----2/获取与地质体范围相交的所有等高线
            var ContourLineLayer = contourLine.pFeatureCollection;
            List<LineString> multiContourLine = GetLineStringListInEnvolop(ContourLineLayer);
            //----3/针对每条剖面线生成对应的地层剖面
            for(int s=0;s<oriNum;s++)
            {
                if (s >100 )
                {
                    //continue;
                    var stop = 0;
                }
                //3.1获取原始剖面线
                IFeature origonPolylineFeature = origonLinesLayer[s];
                LineString origonLine = origonPolylineFeature.Geometry as LineString;
                var a = origonLine.Length;
                IAttributesTable attributesTable = origonPolylineFeature.Attributes;
                string m_sectionName;
                double m_sectionValue;

                if (this.Direction == "X")
                {

                    m_sectionValue = Convert.ToDouble(attributesTable["x"]);
                    m_sectionName = (m_sectionValue * 10000).ToString();
                }
                else
                {

                    m_sectionValue = Convert.ToDouble(attributesTable["y"]);
                    m_sectionName = (m_sectionValue * 10000).ToString();
                }
                // 3.2、生成地层线和断层线
                List<StratigraphicAttribute_Polyline> stratumLinesAttribute = new List<StratigraphicAttribute_Polyline>();
                List<LineString> stratumLines = new List<LineString>();
                if (stratumLine.pFeatureCollection != null || faultLine.pFeatureCollection != null)
                {
                    stratumLinesAttribute = StratumLineGen(stratumGroup.pFeatureCollection, stratumLine.pFeatureCollection, faultLine.pFeatureCollection, altitudePoint.pFeatureCollection,dem,origonLine,multiContourLine,zZoom);
                    if (stratumLinesAttribute == null) continue;
                    for (int w = 0; w < stratumLinesAttribute.Count; w++)
                    {
                        stratumLines.Add(stratumLinesAttribute[w].Line);
                    }
                }
                if (stratumLines.Count == 0)
                {
                    continue;
                }
                //3.3生成地表线
                LineString groundLine = GroundLineGen(dem,origonLine, stratumLines,elevSampleStep,zZoom);

                // 3.4、生成底部线
                LineString bottomLine = BottomLineGen(groundLine,sectionDepth,zZoom);
                //3.5地层线后处理为生成面所用，两头边长，//需重新计算所属地层
                List<StratigraphicAttribute_Polyline> stratumLinesAttribute1 = new List<StratigraphicAttribute_Polyline>();

                //地层线首步处理（解决线未能与地表地底相交问题）
                stratumLinesAttribute1 = PostProcessingStratumLine(groundLine, bottomLine, stratumLinesAttribute);
                //地层线最后判断（解决复杂地层线出现排序错误的情况）
                stratumLinesAttribute1 = PostProcessingStratumLine_lastanalysis(stratumLinesAttribute1);
                //地层线再处理（解决地层线相交问题）
                stratumLinesAttribute1 = PostProcessingStratumLine_donotIntersect(stratumLinesAttribute1, groundLine, bottomLine);
                stratumLinesAttribute1 = PostProcessingStratumLine_InuptPolygon(groundLine, bottomLine, stratumLinesAttribute1);
                //3.6输出线面要素
                ShapeFileHelper shapeFileHelper = new ShapeFileHelper("");
                if (Direction == "X")
                {
                    
                    //GenSectionLine resultGenSectionLine0 = new GenSectionLine(stratumLinesAttribute1, m_sectionValue, 0, this.YMin, m_sectionName, m_sectionValue, "X");
                    //gW.CreateSectionPolyLineFeatureLayer(resultGenSectionLine0);
                    // 6 生成地层线面
                    GenSectionLine resultGenSectionLine = new GenSectionLine(stratumLinesAttribute1, m_sectionValue, 0, this.YMin, m_sectionName, m_sectionValue, "X");
                    shapeFileHelper.CreateSectionPolyLineFeatureLayer(resultGenSectionLine, savePath);
                    List<StratigraphicAttribute_Polygon> stratumFaceAttribute = CreateSectionFaceFromSectionLine(resultGenSectionLine);
                    if (stratumFaceAttribute == null) continue;
                    GenSectionFace resultGenSectionFace = new GenSectionFace(stratumFaceAttribute, m_sectionValue, 0, m_sectionName, m_sectionValue, "X");
                    shapeFileHelper.CreateSectionPolygoneatureLayer(resultGenSectionFace, savePath);
                }
                else
                {

                    //GenSectionLine resultGenSectionLine0 = new GenSectionLine(stratumLinesAttribute, 0, m_sectionValue, this.XMin, m_sectionName, m_sectionValue, "Y");
                    //gW.CreateSectionPolyLineFeatureLayer(resultGenSectionLine0);
                    // 6 生成地层面
                    GenSectionLine resultGenSectionLine = new GenSectionLine(stratumLinesAttribute1, m_sectionValue, 0, this.XMin, m_sectionName, m_sectionValue, "Y");
                    shapeFileHelper.CreateSectionPolyLineFeatureLayer(resultGenSectionLine, savePath);
                    List<StratigraphicAttribute_Polygon> stratumFaceAttribute = CreateSectionFaceFromSectionLine(resultGenSectionLine);
                    if (stratumFaceAttribute == null) continue;
                    GenSectionFace resultGenSectionFace = new GenSectionFace(stratumFaceAttribute, m_sectionValue, 0, m_sectionName, m_sectionValue, "Y");
                    shapeFileHelper.CreateSectionPolygoneatureLayer(resultGenSectionFace, savePath);
                }
            }
            return savePath;
        }
        /// <summary>
        /// 生成地层线和断层线
        /// </summary>
        /// <param name="origonLine"></param>
        /// <param name="multiContourLine"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private List<StratigraphicAttribute_Polyline> StratumLineGen(FeatureCollection stratumGroupLayer,FeatureCollection StratumLineLayer,FeatureCollection FaultLineLayer, FeatureCollection altitudesLayer,RasterHelper dem,  LineString orgLine, List<LineString> multiContourLine,double ZZoom)
        {
            List<StratigraphicAttribute> stratumAttributeList = new List<StratigraphicAttribute>();
            List<LineString> result = new List<LineString>();
            List<double> tendencyList = new List<double>();
            List<double> angleList = new List<double>();
            List<double> sectionLineAngleList = new List<double>();
            List<double> sightAngleList = new List<double>();
            List<string> lineTypeList = new List<string>();
            List<int> lineIDList = new List<int>();


            // 预处理地层产状信息，角度
            List<Altitude> altitudes = PreProcessingAltitude(altitudesLayer);


            List<StratigraphicAttribute_Point> pointCollectionAll = new List<StratigraphicAttribute_Point>();

            //获得原始剖面线与地层线的所有交点
            for (int i = 0; i < StratumLineLayer.Count; i++)
            {
                IFeature structLineFeature = StratumLineLayer[i];

                if (!orgLine.Intersects(structLineFeature.Geometry))
                {
                    continue;
                }
                IGeometry pGeometry = orgLine.Intersection(structLineFeature.Geometry);
                int jj = pGeometry.NumGeometries;
                GeometryFactory geo = new GeometryFactory();
                var pointCollection = geo.CreateMultiPoint(pGeometry.Coordinates);
                //MultiPoint pointCollection = pGeometry as MultiPoint;

                for (int j = 0; j < pointCollection.NumPoints; j++)
                {
                    Point ssPoint = pointCollection.GetGeometryN(j) as Point;
                    StratigraphicAttribute_Point tempAttribute_Point = new StratigraphicAttribute_Point();
                    tempAttribute_Point.point = ssPoint;
                    tempAttribute_Point.lineType = "地层线";
                    int fid = Convert.ToInt32(GetFieldValueByPoint(ssPoint, StratumLineLayer, "Id"));
                    tempAttribute_Point.id = fid;
                    pointCollectionAll.Add(tempAttribute_Point);
                }
            }
            if (FaultLineLayer != null)
            {
                //获得原始剖面线与断层线的所有交点
                for (int i = 0; i < FaultLineLayer.Count; i++)
                {
                    IFeature faultLineFeature = FaultLineLayer[i];
                    if (!orgLine.Intersects(faultLineFeature.Geometry))
                    {
                        continue;
                    }

                    IGeometry pGeometry = orgLine.Intersection(faultLineFeature.Geometry);
                    GeometryFactory geo = new GeometryFactory();
                    var pointCollection = geo.CreateMultiPoint(pGeometry.Coordinates);
                    //MultiPoint pointCollection = pGeometry as MultiPoint;

                    for (int j = 0; j < pointCollection.NumPoints; j++)
                    {
                        Point ssPoint = pointCollection.GetGeometryN(j) as Point;
                        StratigraphicAttribute_Point tempAttribute_Point = new StratigraphicAttribute_Point();
                        tempAttribute_Point.point = ssPoint;
                        tempAttribute_Point.lineType = "断层线";
                        int fid = Convert.ToInt32(GetFieldValueByPoint(ssPoint,FaultLineLayer, "Id"));
                        tempAttribute_Point.id = fid;
                        pointCollectionAll.Add(tempAttribute_Point);
                    }
                }
            }


            if (pointCollectionAll.Count == 0)
            {
                return null;
                //生成虚拟地层线
                double virtualx = (orgLine.StartPoint.X + orgLine.EndPoint.X) / 2;
                double virtualy = (orgLine.StartPoint.Y + orgLine.EndPoint.Y) / 2;
                Point ssPoint = new Point(new Coordinate(virtualx, virtualy));

                StratigraphicAttribute_Point tempAttribute_Point = new StratigraphicAttribute_Point();
                tempAttribute_Point.point = ssPoint;
                tempAttribute_Point.lineType = "地层线";
                pointCollectionAll.Add(tempAttribute_Point);
            }

            List<IPoint> pointCollection2 = new List<IPoint>();
            //所有交点排序
            GeoSortHelper Sort = new GeoSortHelper();
            List<StratigraphicAttribute_Point> bubblePoints = Sort.bubbleSort(pointCollectionAll);
            List<StratigraphicAttribute_Point> bubblePoints2 = new List<StratigraphicAttribute_Point>();
            bubblePoints2.Add(bubblePoints[0]);
            for (int niu = 1; niu < bubblePoints.Count; niu++)
            {
                double niuX1 = Math.Floor(bubblePoints[niu].point.X * 1000) / 1000;
                double niuY1 = Math.Floor(bubblePoints[niu].point.Y * 1000) / 1000;
                string niuL1 = bubblePoints[niu].lineType;
                int niuI1 = bubblePoints[niu].id;

                double niuX = Math.Floor(bubblePoints[niu - 1].point.X * 1000) / 1000;
                double niuY = Math.Floor(bubblePoints[niu - 1].point.Y * 1000) / 1000;
                string niuL = bubblePoints[niu - 1].lineType;
                int niuI = bubblePoints[niu - 1].id;

                if (niuX1 == niuX && niuY1 == niuY)
                {
                    if (niuL1 == "地层线" && niuL == "断层线")
                    {
                        continue;
                    }
                    else if (niuL1 == "断层线" && niuL == "地层线")
                    {
                        int bubNum = bubblePoints2.Count;
                        bubblePoints2[bubNum - 1] = new StratigraphicAttribute_Point(bubblePoints2[bubNum - 1].point, "断层线", niuI1);
                        continue;
                    }

                }
                else
                {
                    bubblePoints2.Add(bubblePoints[niu]);
                }
            }

            for (int j = 0; j < bubblePoints2.Count; j++)
            {
                IPoint ssPoint = bubblePoints2[j].point;
                double zValue=0;                
                dem.GetRasterValue(ssPoint.X, ssPoint.Y, out zValue);
                ssPoint.Z = zValue;
                lineTypeList.Add(bubblePoints2[j].lineType);
                lineIDList.Add(bubblePoints2[j].id);
                LineString tempStratum = GetStratumLineByPoint(ssPoint, StratumLineLayer);


                // 计算实测倾向

                double tendency = 0.0;
                double angle = 0.0;
                //间接求产状
                //int occurence = GetOccurence(tempStratum, multiContourLine, ref tendency, ref angle);
                int occurence = 0;
                //测试，只使用产状点
                //occurence = 0;

                //
                if (occurence == 0)
                {
                    tendency = GetTendency(ssPoint.X, ssPoint.Y, altitudes);
                    tendencyList.Add(tendency);
                    angle = GetAngle(ssPoint.X, ssPoint.Y, altitudes);
                    angleList.Add(angle);
                }
                else
                {
                    tendencyList.Add(tendency);
                    angleList.Add(angle);
                }
                // 计算剖面线走向
                double sectionLineAngle = 0;
                //如果产状点直接有产状数据，则直接使用
                if (altitudes[0].tendency >= 0)
                {
                    sectionLineAngle = GetLineAngleByPoint(ssPoint.X, ssPoint.Y, altitudes);
                }
                //否则
                else
                {
                    sectionLineAngle = GetLineAngle(ssPoint.X, ssPoint.Y, orgLine);
                }

                //                double angleofsection = sectionLineAngle * 180 / Math.PI;
                //                if (angleofsection <= 90)
                //                {
                //                    angleofsection = 90 - angleofsection;
                //                }
                //                else
                //                {
                //                    angleofsection = 360 - angleofsection + 90
                //;
                //                }
                sectionLineAngleList.Add(sectionLineAngle);
                // 计算视倾角
                double sightAngle =
                    Math.Atan(
                        Math.Tan(angle * Math.PI / 180) *
                        Math.Cos(sectionLineAngle - tendency * Math.PI / 180)
                    );
                sightAngleList.Add(sightAngle);

                //////////////////////// 计算剖面上的地层线

                //设置剖面图垂直厚度为100 
                double H = 100.0;

                //设置上界点
                Point stratumPoint0 = new Point(new Coordinate(GetDistanceFromPoint(ssPoint.X, ssPoint.Y, orgLine), ssPoint.Z * ZZoom));

                //设置下界点
                //Point stratumPoint1 = new Point(new Coordinate(GetDistanceFromPoint(ssPoint.X, ssPoint.Y, orgLine), stratumPoint0.Coordinate.Y-H*ZZoom));
                Point stratumPoint1 = new Point(new Coordinate());
                stratumPoint1.X = stratumPoint0.X + H / Math.Tan(sightAngle);
                //stratumPoint1.Y = stratumPoint0.Y+H ;
                stratumPoint1.Y = stratumPoint0.Y - H * ZZoom;


                List<Point> temP = new List<Point>() { stratumPoint0, stratumPoint1 };
                ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
                LineString sectionStratumLine = convertGeometriesHelper.ConvertPointsToLine(temP);

                result.Add(sectionStratumLine);
            }

            //获得被交点打断的线的中点
            if (bubblePoints2.Count > 0)
            {

                pointCollection2.Add(orgLine.StartPoint);
                for (int j = 0; j < bubblePoints2.Count - 1; j++)
                {
                    IPoint temp1 = bubblePoints2[j].point;
                    IPoint temp2 = bubblePoints2[j + 1].point;
                    IPoint midPoint = new Point(new Coordinate((temp1.X + temp2.X) / 2, (temp1.Y + temp2.Y) / 2));

                    pointCollection2.Add(midPoint);

                }
                pointCollection2.Add(orgLine.EndPoint);
                for (int j = 0; j < pointCollection2.Count; j++)
                {
                    IPoint ssPoint = pointCollection2[j];
                    ssPoint.Coordinate.Y += 0.2;
                    // 计算剖面点的地层属性 
                    StratigraphicAttribute stratumAttribute = GetAttributeFromStratumGroup(ssPoint,stratumGroupLayer);
                    stratumAttributeList.Add(stratumAttribute);
                }
                for (int j = 0; j < pointCollection2.Count; j++)
                {
                    IPoint ssPoint = pointCollection2[j];
                    ssPoint.Coordinate.Y -= 0.2;
                }
            }
            Comparison<LineString> comparison1 = new Comparison<LineString>(SortByvalue2);//将方法传给委托实例
            result.Sort(comparison1);
            List<StratigraphicAttribute_Polyline> lineAttributes = new List<StratigraphicAttribute_Polyline>();
            //计算地层线对应的地层属性，用left_right表示
            for (int h = 0; h < result.Count; h++)
            {
                StratigraphicAttribute_Polyline lineAttribute = new StratigraphicAttribute_Polyline();
                lineAttribute.Line = result[h];
                lineAttribute.tendency = tendencyList[h];
                lineAttribute.angle = angleList[h];
                lineAttribute.sectionLineAngle = sectionLineAngleList[h];
                lineAttribute.sightAngle = sightAngleList[h];
                lineAttribute.lineType = lineTypeList[h];
                lineAttribute.attributeLeft = stratumAttributeList[h];
                lineAttribute.attributeRight = stratumAttributeList[h + 1];
                lineAttribute.LineID = lineIDList[h];
                lineAttributes.Add(lineAttribute);


            }
            Comparison<StratigraphicAttribute_Polyline> comparison = new Comparison<StratigraphicAttribute_Polyline>(SortByvalue);//将方法传给委托实例
            lineAttributes.Sort(comparison);
            return lineAttributes;
        }

        /// <summary>
        /// 获取在本建模范围内的所有线
        /// </summary>
        /// <param name="lineFeatureCollection"></param>
        /// <returns></returns>
        private List<LineString> GetLineStringListInEnvolop(FeatureCollection lineFeatureCollection)
        {
            //生成范围面
            List<LineString> result = new List<LineString>();

            //左边界 
            var coor1 = new Coordinate[2];
            coor1[0] = new Coordinate(this.XMin, this.YMax);
            coor1[1] = new Coordinate(this.XMin, this.YMin);

            LineString leftBoundary = new LineString(coor1);

            //右边界
            var coor2 = new Coordinate[2];
            coor2[0] = new Coordinate(this.XMax, this.YMax);
            coor2[1] = new Coordinate(this.XMax, this.YMin);

            LineString rightBoundary = new LineString(coor2);


            //上边界
            var coor3 = new Coordinate[2];
            coor3[0] = new Coordinate(this.XMin, this.YMax);
            coor3[1] = new Coordinate(this.XMax, this.YMax);

            LineString topBoundary = new LineString(coor3);

            //下边界
            var coor4 = new Coordinate[2];
            coor4[0] = new Coordinate(this.XMin, this.YMin);
            coor4[1] = new Coordinate(this.XMax, this.YMin);

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

            // 创建 Polygonizer
            var polygonizer = new Polygonizer();

            // 添加几何对象
            foreach (Geometry geometry in validatedGeometries)
            {
                polygonizer.Add(geometry);
            }

            // 获取多边形对象
            var bondaryOfRegion = polygonizer.GetPolygons();


            //获取与面相交的线合集
            for (int i = 0; i < lineFeatureCollection.Count; i++)
            {
                IFeature structLineF = lineFeatureCollection[i];
                foreach (IGeometry envo in bondaryOfRegion)
                {
                    LineString tempLine = structLineF.Geometry as LineString;

                    //线与面有交点
                    if (tempLine.Intersects(envo))
                    {
                        result.Add(tempLine);
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
        /// 预处理产状符号
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private List<Altitude> PreProcessingAltitude(FeatureCollection  altitudes)
        {
            List<Altitude> altitudess = new List<Altitude>();

            //若不指定ID，即lineId=-1,则全部点都需参加计算
            for (int i = 0; i < altitudes.Count; i++)
            {
                IFeature alFea = altitudes[i];

                Altitude alt = new Altitude();
                alt.x = (alFea.Geometry as IPoint).X;
                alt.y = (alFea.Geometry as IPoint).Y;
                if (alFea.Attributes.Exists("GZBBAC") && alFea.Attributes.Exists("GZBBAD"))
                {
                    string tendencyString = alFea.Attributes["GZBBAC"] as string;
                    switch (tendencyString)
                    {
                        case "N":
                            alt.tendency = 0;
                            break;
                        case "S":
                            alt.tendency = 180.0;
                            break;
                        case "NNE":
                            alt.tendency = 22.5;
                            break;
                        case "SSW":
                            alt.tendency = 202.5;
                            break;
                        case "NE":
                            alt.tendency = 45.0;
                            break;
                        case "SW":
                            alt.tendency = 225.0;
                            break;
                        case "ENE":
                            alt.tendency = 67.5;
                            break;
                        case "WSW":
                            alt.tendency = 247.5;
                            break;
                        case "E":
                            alt.tendency = 90.0;
                            break;
                        case "W":
                            alt.tendency = 270.0;
                            break;
                        case "ESE":
                            alt.tendency = 112.5;
                            break;
                        case "WNW":
                            alt.tendency = 292.5;
                            break;
                        case "SE":
                            alt.tendency = 135.0;
                            break;
                        case "NW":
                            alt.tendency = 315.0;
                            break;
                        case "SSE":
                            alt.tendency = 157.5;
                            break;
                        default:
                            alt.tendency = 337.5;
                            break;
                    }

                    var newAngle = Convert.ToString(alFea.Attributes["GZBBAD"]);
                    newAngle = newAngle.Replace("°", "");
                    alt.angle = Convert.ToDouble(newAngle);
                }
                else if (alFea.Attributes.Exists("倾向") && alFea.Attributes.Exists("倾角"))
                {
                    alt.tendency = Convert.ToDouble(alFea.Attributes["倾向"].ToString());
                    alt.angle = Convert.ToDouble(alFea.Attributes["倾角"].ToString());
                }
                if (alFea.Attributes.Exists("走向"))
                {
                    alt.lineangle = Convert.ToDouble(alFea.Attributes["走向"].ToString());
                }
                altitudess.Add(alt);

            }

            return altitudess;
        }
        ///<summary>
        /// 计算某点所在的地层要素的给定字段值
        /// </summary>
        /// <param name="pPoint"></param>
        /// <param name=""></param>
        /// <returns></returns>
        private object GetFieldValueByPoint(IPoint pPoint, FeatureCollection gfc, string fieldName)
        {

            //List<Feature> results = new List<Feature>();
            object result = null;
            Coordinate centerCoordinate = new Coordinate(pPoint.X, pPoint.Y);

            GeometryFactory geometryFactory = new GeometryFactory();
            Geometry queryGeometry = geometryFactory.CreatePoint(centerCoordinate) as Geometry;
            //未查询到
            // 构建空间索引
            var tree = new STRtree<Feature>();
            for (int i = 0; i < gfc.Count; i++)
            {
                Feature f = gfc[i] as Feature;
                tree.Insert(f.Geometry.EnvelopeInternal, f);
            }

            // 创建一个测试查询的几何形状 queryGeometry，并在索引中查找与 queryGeometry 相交的所有 Feature

            ICollection<Feature> results = tree.Query(queryGeometry.EnvelopeInternal)
                                                   .Where(f => f.Geometry.Intersects(queryGeometry))
                                                   .ToList();
            //循环地层线点判断是否是否是该点，只需判断至两位小数即可
            //for(int i=0;i< gfc.Count; i++)
            //{
            //    Feature f = gfc[i] as Feature;

            //        if(f.Geometry.Distance(queryGeometry)<1)
            //        {
            //            results.Add(f);
            //            break;
            //        }

            //}

            if (results.Count != 0)
            {
                foreach (Feature r in results)
                {
                    result = r.Attributes[fieldName];
                }

            }
            else
            {
                return null;
            }
            return result;



        }
        ///<summary>
        /// 计算点所在的地层线
        /// </summary>
        /// <param name="pPoint"></param>
        /// <param name="stratumGroup2dLayer"></param>
        /// <returns></returns>
        private LineString GetStratumLineByPoint(IPoint pPoint, FeatureCollection gfc)
        {


            Coordinate centerCoordinate = new Coordinate(pPoint.X, pPoint.Y);

            GeometryFactory geometryFactory = new GeometryFactory();
            Geometry queryGeometry = geometryFactory.CreatePoint(centerCoordinate) as Geometry;
            // 构建空间索引
            var tree = new STRtree<Feature>();
            for (int i = 0; i < gfc.Count; i++)
            {
                Feature f = gfc[i] as Feature;
                foreach (var one in f.Geometry.Coordinates)
                {
                    if ((Math.Round(one.X, 1) == Math.Round(queryGeometry.Coordinate.X, 1)) && (Math.Round(one.Y, 1) == Math.Round(queryGeometry.Coordinate.Y, 1)))
                    {
                        return f.Geometry as LineString;
                    }
                }

                //tree.Insert(f.Geometry.EnvelopeInternal, f);
            }
            return null;
            // 创建一个测试查询的几何形状 queryGeometry，并在索引中查找与 queryGeometry 相交的所有 Feature

            //ICollection<Feature> results = tree.Query(queryGeometry.EnvelopeInternal)
            //                                       .Where(f => f.Geometry.Intersects(queryGeometry))
            //                                       .ToList();


            //if (results.Count != 0)
            //{
            //    foreach (Feature r in results)
            //    {
            //        result = r.Geometry as LineString;
            //    }

            //}
            //return result;
        }
        /// <summary>
        ///  插值计算倾向
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        private double GetTendency(double x, double y, List<Altitude> altitudes)
        {
            List<Points> source = new List<Points>();

            foreach (Altitude a in altitudes)
            {
                Points p =new Points();
                p.x = a.x;
                p.y = a.y;
                p.z = a.tendency;
                source.Add(p);
            }
            IInterpolate interpolate = InterpolateEntry.GetInterpolate();
            double tendency = interpolate.IdwInterpolate2d(x, y, source);

            return tendency;
        }

        /// <summary>
        /// 插值计算倾角
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="altitudes"></param>
        /// <returns></returns>
        private double GetAngle(double x, double y, List<Altitude> altitudes)
        {
            List<Points> source = new List<Points>();

            foreach (Altitude a in altitudes)
            {
                Points p=new Points();
                p.x = a.x;
                p.y = a.y;
                p.v = a.angle;

                source.Add(p);
            }
            IInterpolate interpolate = InterpolateEntry.GetInterpolate();
            double angle = interpolate.IdwInterpolate2d(x, y, source);

            return angle;
        }
        /// <summary>
        /// 点插值计算线的走向
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="altitudes"></param>
        /// <returns></returns>
        public double GetLineAngleByPoint(double x, double y, List<Altitude> altitudes)
        {
            List<Points> source = new List<Points>();

            foreach (Altitude a in altitudes)
            {
                Points p = new Points();
                p.x = a.x;
                p.y = a.y;
                p.v = a.lineangle;

                source.Add(p);
            }
            IInterpolate interpolate = InterpolateEntry.GetInterpolate();
            double lineangle = interpolate.IdwInterpolate2d(x, y, source);

            return lineangle;
        }
        /// <summary>
        /// 计算一条线的走向
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private double GetLineAngle(double x, double y, LineString line)
        {
            double lineAngle = 0;
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            List<Point> sectionLinePointCollection = convertGeometriesHelper.ConvertLineToPoints(line);


            int j;
            for (j = 0; j < sectionLinePointCollection.Count - 1; j++)
            {
                IPoint p1 = sectionLinePointCollection[j];
                IPoint p2 = sectionLinePointCollection[j + 1];

                double detY = p2.Y - p1.Y;
                double detX = p2.X - p1.X;
                if (detX != 0)
                {
                    double k = detY / detX;
                    double b = p1.Y - k * p1.X;

                    //判断地层线与剖面线的交点(x,y,z)是否在该直线上
                    double py = k * x + b;
                    if (y - py < 0.001)//点在该直线上，返回
                    {
                        if (detY > 0)
                        {
                            if (detX > 0)
                            {
                                lineAngle = Math.Atan(Math.Abs(k));
                            }
                            else
                            {
                                lineAngle = Math.PI - Math.Atan(Math.Abs(k));
                            }
                        }
                        else if (detY < 0)
                        {
                            if (detX > 0)
                            {
                                lineAngle = 2 * Math.PI - Math.Atan(Math.Abs(k));
                            }
                            else
                            {
                                lineAngle = Math.PI + Math.Atan(Math.Abs(k));
                            }
                        }
                        else
                        {
                            if (detX > 0)
                            {
                                lineAngle = 0.0;
                            }
                            else
                            {
                                lineAngle = Math.PI;
                            }
                        }

                        break;
                    }
                }
                else //detX == 0
                {
                    if (x - p1.X < 0.001)//点在该直线(该直线为y轴)上，返回
                    {
                        if (detY > 0)
                        {
                            lineAngle = 0.5 * Math.PI;
                        }
                        else
                        {
                            lineAngle = 0.5 * Math.PI + Math.PI;
                        }

                        break;
                    }
                }

            }
            if (j >= sectionLinePointCollection.Count - 1)
            {//计算交点错误，交点没有在该SectionLine上
                throw new Exception("计算交点错误，交点没有在该剖面线上！");
            }

            return lineAngle;
        }
        /// <summary>
        /// 求一个点到某条直线起点的距离
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private double GetDistanceFromPoint(double x, double y, LineString line)
        {
            double distance = 0.0;

            Point inPoint = new Point(new Coordinate(x, y));

            IPoint lineStart = line.StartPoint;
            distance = lineStart.Distance(inPoint);


            return distance;
        }
        ///<summary>
        /// 计算点所在的地层面的属性
        /// </summary>
        /// <param name="pPoint"></param>
        /// <param name="stratumGroup2dLayer"></param>
        /// <returns></returns>
        private StratigraphicAttribute GetAttributeFromStratumGroup(IPoint pPoint,FeatureCollection stratumGroupLayer)
        {

            StratigraphicAttribute attribute = new StratigraphicAttribute();
            string DSN = null;
            string DSO = null;
            if (GetFieldValueByPoint(pPoint, stratumGroupLayer, "DSN") != null)
            {
                DSN = GetFieldValueByPoint(pPoint, stratumGroupLayer, "DSN").ToString();
                DSO = GetFieldValueByPoint(pPoint, stratumGroupLayer, "DSO").ToString();
            }
            else
            {
                DSN = "其他"; DSO = "other";
            }
            attribute.DSN = DSN;
            attribute.DSO = DSO;

            return attribute;

        }
        static int SortByvalue(StratigraphicAttribute_Polyline x, StratigraphicAttribute_Polyline y)
        {
            return x.Line.StartPoint.X.CompareTo(y.Line.StartPoint.X);
        }
        static int SortByvalue1(Point x, Point y)
        {
            return x.InteriorPoint.X.CompareTo(y.InteriorPoint.X);
        }
        static int SortByvalue2(LineString x, LineString y)
        {
            return x.StartPoint.X.CompareTo(y.StartPoint.X);
        }
        /// <summary>
        /// 生成地表线
        /// </summary>
        /// <param name="orgLine"></param>
        /// <param name="elevLayer"></param>
        /// <param name="elevSampleStep"></param>
        /// <param name="zZoom"></param>
        /// <returns></returns>
        private LineString GroundLineGen(RasterHelper dem, LineString orgLine, List<LineString> stratumLines,double ElevSampleStep,double ZZoom)
        {

            List<Point> groundPoints = new List<Point>();

            int DispersePointCount = (int)(orgLine.Length / ElevSampleStep);
            //double k = 1 / (double)DispersePointCount;
            double k = 0;
            for (int j = 0; j < DispersePointCount + 1; j++)
            {
                double x = 0;
                double y = 0;
                if (this.Direction == "X")
                {
                    x = orgLine.StartPoint.X;
                    y = orgLine.StartPoint.Y + orgLine.Length * k;
                }
                else
                {
                    x = orgLine.StartPoint.X + orgLine.Length * k;
                    y = orgLine.StartPoint.Y;
                }
                double zValue = 0;
                dem.GetRasterValue(x, y, out zValue );
                double z = zValue;
                double sectionX = orgLine.Length * k;
                double sectionY = z*ZZoom ;

                Point sectionPoint = new Point(new Coordinate(sectionX, sectionY));

                bool isempty = sectionPoint.IsEmpty;
                if (isempty == true)
                {
                    k += 1 / (double)(DispersePointCount);
                    continue;
                }
                groundPoints.Add(sectionPoint);

                k += 1 / (double)DispersePointCount;
            }

            //去除空值点

            List<Point> groundPointsNew = new List<Point>();
            for (int i = 0; i < groundPoints.Count; i++)
            {
                Point n = groundPoints[i];
                bool isempty = n.IsEmpty;
                if (isempty == true)
                {
                    continue;
                }


                groundPointsNew.Add(n);
            }
            //for (int i = 0; i < stratumLines.Count; i++)
            //{
            //    if (stratumLines[i].Coordinates[0].Y > 200)
            //    {
            //        Point point = new Point(stratumLines[i].Coordinates[0].X, stratumLines[i].Coordinates[0].Y);
            //        groundPointsNew.Add(point);
            //    }
            //    else
            //    {
            //        Point point = new Point(stratumLines[i].Coordinates[1].X, stratumLines[i].Coordinates[1].Y);
            //        groundPointsNew.Add(point);
            //    }

            //}
            Comparison<Point> comparison = new Comparison<Point>(SortByvalue1);//将方法传给委托实例
            groundPointsNew.Sort(comparison);
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();    
            LineString groundLineNew = convertGeometriesHelper.ConvertPointsToLine(groundPointsNew);
            return groundLineNew;
        }

        /// <summary>
        /// 生成底部线
        /// </summary>
        /// <param name="groundLine"></param>
        /// <param name="sectionDepth"></param>
        /// <returns></returns>
        private LineString BottomLineGen(LineString groundLine,double  SectionDepth, double ZZoom)
        {

            List<Point> bottomPoints = new List<Point>();
            object o = Type.Missing;
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            List<Point> groundPoints = convertGeometriesHelper.ConvertLineToPoints(groundLine);
            for (int i = 0; i < groundPoints.Count; i++)
            {

                Point n = groundPoints[i];
                bool isempty = n.IsEmpty;
                if (isempty == true)
                {
                    continue;
                }
                Point p = new Point(new Coordinate(n.X, (n.Y - SectionDepth * ZZoom)));


                bottomPoints.Add(p);
            }

            LineString bottomLine = convertGeometriesHelper.ConvertPointsToLine(bottomPoints);
            return bottomLine;
        }
        /// <summary>
        /// 地层线修正，确保是符合常理的
        /// </summary>
        /// <param name="groundLine"></param>
        /// <param name="bottomLine"></param>
        /// <param name="stratumLinesAttribute"></param>
        /// <returns></returns>
        private List<StratigraphicAttribute_Polyline> PostProcessingStratumLine(LineString groundLine, LineString bottomLine, List<StratigraphicAttribute_Polyline> stratumLinesAttribute)
        {
            List<LineString> result = new List<LineString>();
            List<StratigraphicAttribute_Polyline> stratumResult = new List<StratigraphicAttribute_Polyline>();
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            //延长至与顶部线相交

            List<StratigraphicAttribute_Polyline> stratumResult2 = new List<StratigraphicAttribute_Polyline>();
            foreach (StratigraphicAttribute_Polyline sourceAttribute2 in stratumLinesAttribute)
            {
                LineString source2 = sourceAttribute2.Line;
                LineString newLine = source2;
                Double k = (source2.EndPoint.Y - source2.StartPoint.Y) / (source2.EndPoint.X - source2.StartPoint.X);
                if (k > 0)
                {
                    source2.StartPoint.X += 5;
                    source2.StartPoint.Y += 5 * k;
                    source2.EndPoint.X -= 5;
                    source2.EndPoint.Y -= 5 * k;
                }
                else
                {
                    source2.StartPoint.X -= 5;
                    source2.StartPoint.Y -= 5 * k;
                    source2.EndPoint.X += 5;
                    source2.EndPoint.Y += 5 * k;
                }    

                StratigraphicAttribute_Polyline newLineAttribute = new StratigraphicAttribute_Polyline();

                newLineAttribute.Line = newLine;
                newLineAttribute.LineID = sourceAttribute2.LineID;
                newLineAttribute.attributeLeft = sourceAttribute2.attributeLeft;
                newLineAttribute.attributeRight = sourceAttribute2.attributeRight;

                newLineAttribute.lineType = sourceAttribute2.lineType;
                newLineAttribute.tendency = sourceAttribute2.tendency;
                newLineAttribute.angle = sourceAttribute2.angle;
                newLineAttribute.sightAngle = sourceAttribute2.sightAngle;
                newLineAttribute.sectionLineAngle = sourceAttribute2.sectionLineAngle;

                stratumResult2.Add(newLineAttribute);



            }
            int num = 0;
            //求与底部线的交点
            foreach (StratigraphicAttribute_Polyline sourceAttribute in stratumResult2)
            {

                LineString source = sourceAttribute.Line;
                bool redo = false;
                Double k = (source.EndPoint.Y - source.StartPoint.Y) / (source.EndPoint.X - source.StartPoint.X);
                if (k > -0.5 && k < 0.5)
                {
                    redo = true;
                }
                Point p01 = source.StartPoint as Point;
                Point p02 = source.EndPoint as Point;
                GeometryFactory geo = new GeometryFactory();
                IGeometry pGeometry = bottomLine.Intersection(source);
                var pointCollection = geo.CreateMultiPoint(pGeometry.Coordinates);
                IGeometry pGeometry3 = groundLine.Intersection(source);
                var pointCollection3 = geo.CreateMultiPoint(pGeometry3.Coordinates);

                if (!redo)
                {
                    if (pointCollection.Count == 1 && pointCollection3.Count == 1)
                    {
                        //continue;
                        Point p1 = pointCollection.GetGeometryN(0) as Point;
                        Point p0 = pointCollection3.GetGeometryN(0) as Point;

                        StratigraphicAttribute_Polyline newLineAttribute = new StratigraphicAttribute_Polyline();
                        List<Point> newPoints = new List<Point>();
                        object o = Type.Missing;
                        newPoints.Add(p0);
                        newPoints.Add(p1);

                        LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                        var k1 = (newLine.EndPoint.Y - newLine.StartPoint.Y) / (newLine.EndPoint.X - newLine.StartPoint.X);
                        //if (!newLine.Intersects(pGeometry)|| !newLine.Intersects(pGeometry3))
                        //{
                        newLine.StartPoint.Y += 2;
                        newLine.StartPoint.X += 2 / k1;
                        newLine.EndPoint.Y -= 2;
                        newLine.EndPoint.X -= 2 / k1;
                        //}

                        newLineAttribute.Line = newLine;
                        newLineAttribute.LineID = sourceAttribute.LineID;
                        newLineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                        newLineAttribute.attributeRight = sourceAttribute.attributeRight;
                        newLineAttribute.lineType = sourceAttribute.lineType;
                        newLineAttribute.tendency = sourceAttribute.tendency;
                        newLineAttribute.angle = sourceAttribute.angle;
                        newLineAttribute.sightAngle = sourceAttribute.sightAngle;
                        newLineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;

                        stratumResult.Add(newLineAttribute);



                    }
                    //否则，计算的产状是有问题的，需要进行修正
                    else
                    {
                        //这里原先方法是修改固定值，现对其不做处理让其上下一致1.56
                        Coordinate c1 = new Coordinate();
                        double sightAngle = -0.5;
                        c1.X = source.Coordinates[0].X + 100 / Math.Tan(sightAngle);
                        c1.Y = source.Coordinates[0].Y - 1000;

                        Point p1 = new Point(c1);
                        Point pnew1 = new Point(source.Coordinates[0].X, source.Coordinates[0].Y + 50);
                        // var p00 = pointCollection3.GetGeometryN(0) as Point;
                        LineString newStruamLine = convertGeometriesHelper.ConvertPointsToLine(new List<Point> { pnew1, p1 });
                        Double k11 = (newStruamLine.EndPoint.Y - newStruamLine.StartPoint.Y) / (newStruamLine.EndPoint.X - newStruamLine.StartPoint.X);
                        newStruamLine.StartPoint.Y += 20;
                        newStruamLine.StartPoint.X += 20 / k11;
                        newStruamLine.EndPoint.Y -= 20;
                        newStruamLine.EndPoint.X -= 20 / k11;

                        IGeometry pGeometry1 = groundLine.Intersection(newStruamLine);
                        var pointCollection1 = geo.CreateMultiPoint(pGeometry1.Coordinates);
                        IGeometry pGeometry2 = bottomLine.Intersection(newStruamLine);
                        var pointCollection2 = geo.CreateMultiPoint(pGeometry2.Coordinates);
                        //MultiPoint pointCollection2 = pGeometry2 as MultiPoint;
                        if (pointCollection1.Count > 0 && pointCollection2.Count > 0)
                        {
                            //continue;
                            Point p30 = pointCollection1.GetGeometryN(0) as Point;
                            Point p3 = pointCollection2.GetGeometryN(0) as Point;

                            StratigraphicAttribute_Polyline newLineAttribute = new StratigraphicAttribute_Polyline();

                            List<Point> newPoints = new List<Point>();
                            newPoints.Add(p30);
                            newPoints.Add(p3);
                            LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                            var k1 = (newLine.EndPoint.Y - newLine.StartPoint.Y) / (newLine.EndPoint.X - newLine.StartPoint.X);
                            newLine.StartPoint.Y += 2;
                            newLine.StartPoint.X += 2 / k1;
                            newLine.EndPoint.Y -= 2;
                            newLine.EndPoint.X -= 2 / k1;
                            //newLine = ExtendLine(newLine, 100, 0);
                            //newLine = ExtendLine(newLine, 100, 1);
                            newLineAttribute.Line = newLine;
                            newLineAttribute.LineID = sourceAttribute.LineID;
                            newLineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                            newLineAttribute.attributeRight = sourceAttribute.attributeRight;
                            newLineAttribute.lineType = sourceAttribute.lineType;
                            newLineAttribute.tendency = 54.588788;
                            newLineAttribute.angle = 42.91753;
                            newLineAttribute.sightAngle = sightAngle;
                            newLineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;

                            stratumResult.Add(newLineAttribute);


                        }
                        else
                        {
                            //continue;
                            StratigraphicAttribute_Polyline newLineAttribute = new StratigraphicAttribute_Polyline();
                            Point p10 = new Point(0, 0);
                            Point p03 = new Point(new Coordinate(0, 0));
                            List<Point> newPoints = new List<Point>();
                            if (num != stratumResult2.Count - 1 || pointCollection3.Count == 1)
                            {
                                try
                                {
                                    p10 = pointCollection3.GetGeometryN(0) as Point;
                                }
                                catch
                                {
                                    continue;
                                }

                            }
                            else
                            {
                                p10 = pointCollection3.GetGeometryN(pointCollection3.NumGeometries - 1) as Point;
                            }
                            p03.Coordinate.X = p10.Coordinate.X;
                            p03.Coordinate.Y = p10.Coordinate.Y - 50;
                            newPoints.Add(p03);
                            newPoints.Add(p10);

                            LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                            var k1 = (newLine.EndPoint.Y - newLine.StartPoint.Y) / (newLine.EndPoint.X - newLine.StartPoint.X);
                            newLine.StartPoint.Y -= 2;
                            newLine.StartPoint.X -= 2 / k1;
                            newLine.EndPoint.Y += 2;
                            newLine.EndPoint.X += 2 / k1;
                            //newLine = ExtendLine(newLine, 100, 0);
                            //newLine = ExtendLine(newLine, 100, 1);
                            newLineAttribute.Line = newLine;
                            newLineAttribute.LineID = sourceAttribute.LineID;
                            newLineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                            newLineAttribute.attributeRight = sourceAttribute.attributeRight;
                            newLineAttribute.lineType = sourceAttribute.lineType;
                            newLineAttribute.tendency = sourceAttribute.tendency;
                            newLineAttribute.angle = sourceAttribute.angle;
                            newLineAttribute.sightAngle = sourceAttribute.angle;
                            newLineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;

                            stratumResult.Add(newLineAttribute);
                        }

                    }
                }
                else
                {
                    //这里原先方法是修改固定值，现对其不做处理让其上下一致1.56
                    Coordinate c1 = new Coordinate();
                    double sightAngle = -0.5;
                    c1.X = source.Coordinates[0].X + 100 / Math.Tan(sightAngle);
                    c1.Y = source.Coordinates[0].Y - 1000;

                    Point p1 = new Point(c1);
                    Point pnew1 = new Point(source.Coordinates[0].X, source.Coordinates[0].Y + 50);
                    // var p00 = pointCollection3.GetGeometryN(0) as Point;
                    LineString newStruamLine = convertGeometriesHelper.ConvertPointsToLine(new List<Point> { pnew1, p1 });
                    Double k11 = (newStruamLine.EndPoint.Y - newStruamLine.StartPoint.Y) / (newStruamLine.EndPoint.X - newStruamLine.StartPoint.X);
                    newStruamLine.StartPoint.Y += 20;
                    newStruamLine.StartPoint.X += 20 / k11;
                    newStruamLine.EndPoint.Y -= 20;
                    newStruamLine.EndPoint.X -= 20 / k11;

                    IGeometry pGeometry1 = groundLine.Intersection(newStruamLine);
                    var pointCollection1 = geo.CreateMultiPoint(pGeometry1.Coordinates);
                    IGeometry pGeometry2 = bottomLine.Intersection(newStruamLine);
                    var pointCollection2 = geo.CreateMultiPoint(pGeometry2.Coordinates);
                    //MultiPoint pointCollection2 = pGeometry2 as MultiPoint;
                    if (pointCollection1.Count > 0 && pointCollection2.Count > 0)
                    {
                        //continue;
                        Point p30 = pointCollection1.GetGeometryN(0) as Point;
                        Point p3 = pointCollection2.GetGeometryN(0) as Point;

                        StratigraphicAttribute_Polyline newLineAttribute = new StratigraphicAttribute_Polyline();

                        List<Point> newPoints = new List<Point>();
                        newPoints.Add(p30);
                        newPoints.Add(p3);
                        LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                        var k1 = (newLine.EndPoint.Y - newLine.StartPoint.Y) / (newLine.EndPoint.X - newLine.StartPoint.X);
                        newLine.StartPoint.Y += 2;
                        newLine.StartPoint.X += 2 / k1;
                        newLine.EndPoint.Y -= 2;
                        newLine.EndPoint.X -= 2 / k1;
                        //newLine = ExtendLine(newLine, 100, 0);
                        //newLine = ExtendLine(newLine, 100, 1);
                        newLineAttribute.Line = newLine;
                        newLineAttribute.LineID = sourceAttribute.LineID;
                        newLineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                        newLineAttribute.attributeRight = sourceAttribute.attributeRight;
                        newLineAttribute.lineType = sourceAttribute.lineType;
                        newLineAttribute.tendency = 54.588788;
                        newLineAttribute.angle = 42.91753;
                        newLineAttribute.sightAngle = sightAngle;
                        newLineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;

                        stratumResult.Add(newLineAttribute);


                    }
                    else
                    {
                        //continue;
                        StratigraphicAttribute_Polyline newLineAttribute = new StratigraphicAttribute_Polyline();
                        Point p10 = new Point(0, 0);
                        Point p03 = new Point(new Coordinate(0, 0));
                        List<Point> newPoints = new List<Point>();
                        if (num != stratumResult2.Count - 1 || pointCollection3.Count == 1)
                        {
                            try
                            {
                                p10 = pointCollection3.GetGeometryN(0) as Point;
                            }
                            catch
                            {
                                continue;
                            }

                        }
                        else
                        {
                            p10 = pointCollection3.GetGeometryN(pointCollection3.NumGeometries - 1) as Point;
                        }
                        p03.Coordinate.X = p10.Coordinate.X;
                        p03.Coordinate.Y = p10.Coordinate.Y - 50;
                        newPoints.Add(p03);
                        newPoints.Add(p10);

                        LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                        var k1 = (newLine.EndPoint.Y - newLine.StartPoint.Y) / (newLine.EndPoint.X - newLine.StartPoint.X);
                        newLine.StartPoint.Y -= 2;
                        newLine.StartPoint.X -= 2 / k1;
                        newLine.EndPoint.Y += 2;
                        newLine.EndPoint.X += 2 / k1;
                        //newLine = ExtendLine(newLine, 100, 0);
                        //newLine = ExtendLine(newLine, 100, 1);
                        newLineAttribute.Line = newLine;
                        newLineAttribute.LineID = sourceAttribute.LineID;
                        newLineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                        newLineAttribute.attributeRight = sourceAttribute.attributeRight;
                        newLineAttribute.lineType = sourceAttribute.lineType;
                        newLineAttribute.tendency = sourceAttribute.tendency;
                        newLineAttribute.angle = sourceAttribute.angle;
                        newLineAttribute.sightAngle = sourceAttribute.angle;
                        newLineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;

                        stratumResult.Add(newLineAttribute);
                    }

                }

                num++;
            }


            if (stratumResult.Count != 0)
            {
                return stratumResult;
            }
            else
            {
                return stratumResult2;
            }

        }
        /// <summary>
        /// 后处理地层线，使地层线不相交
        /// </summary>
        /// 
        /// <param name="stratumLinesAttribute"></param>
        /// <returns></returns>
        private List<StratigraphicAttribute_Polyline> PostProcessingStratumLine_donotIntersect(List<StratigraphicAttribute_Polyline> stratumLinesAttribute, LineString groundLine, LineString bottomLine)
        {
            List<StratigraphicAttribute_Polyline> resultAttribute = stratumLinesAttribute;
            List<StratigraphicAttribute_Polyline> resultAttributefirst = stratumLinesAttribute;
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            bool lastdo = false;
            for (int i = 0; i < resultAttributefirst.Count; i++)
            {
                LineString source = resultAttribute[i].Line;

                for (int j = i + 1; j < resultAttribute.Count; j++)
                {
                    LineString source2 = resultAttribute[j].Line;
                    IGeometry pGeometry = source.Intersection(source2);
                    GeometryFactory geo = new GeometryFactory();
                    var polylineCollection = geo.CreateMultiPoint(pGeometry.Coordinates);
                    //MultiPoint polylineCollection = pGeometry as MultiPoint;
                    if (polylineCollection.Count > 0)
                    {
                        //重新设置地层线
                        List<Point> newPoints = new List<Point>() { source2.StartPoint as Point, source.EndPoint as Point };
                        LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                        IGeometry IsGeometry = newLine.Intersection(groundLine);
                        var IspolylineCollection = geo.CreateMultiPoint(IsGeometry.Coordinates);
                        if (IspolylineCollection.Count > 1)
                        {
                            lastdo = true; break;
                        }



                    }
                }
            }
            if (lastdo == true)
            {
                for (int i = resultAttributefirst.Count - 1; i > 0; i--)
                {
                    LineString source = resultAttributefirst[i].Line;

                    for (int j = i - 1; j > 0; j--)
                    {
                        LineString source2 = resultAttributefirst[j].Line;
                        IGeometry pGeometry = source.Intersection(source2);
                        GeometryFactory geo = new GeometryFactory();
                        var polylineCollection = geo.CreateMultiPoint(pGeometry.Coordinates);
                        //MultiPoint polylineCollection = pGeometry as MultiPoint;
                        if (polylineCollection.Count > 0)
                        {
                            //重新设置地层线
                            List<Point> newPoints = new List<Point>() { source2.StartPoint as Point, source.EndPoint as Point };
                            LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                            StratigraphicAttribute attributeLeft = resultAttributefirst[j].attributeLeft;
                            StratigraphicAttribute attributeRight = resultAttributefirst[j].attributeRight;
                            string lineType = resultAttributefirst[j].lineType;
                            double tendency = resultAttributefirst[j].tendency;
                            double angle = resultAttributefirst[j].angle;
                            double sectionLineAngle = resultAttributefirst[j].sectionLineAngle;
                            double sightAngle = resultAttributefirst[j].sightAngle;
                            int fid = resultAttributefirst[j].LineID;
                            resultAttributefirst[j] = new StratigraphicAttribute_Polyline(attributeLeft,
                                attributeRight, lineType, tendency, angle, sectionLineAngle, sightAngle, newLine, fid);


                        }
                    }
                }
                return resultAttributefirst;
            }
            else
            {
                for (int i = 0; i < resultAttributefirst.Count; i++)
                {
                    LineString source = resultAttribute[i].Line;

                    for (int j = i + 1; j < resultAttribute.Count; j++)
                    {
                        LineString source2 = resultAttribute[j].Line;
                        IGeometry pGeometry = source.Intersection(source2);
                        GeometryFactory geo = new GeometryFactory();
                        var polylineCollection = geo.CreateMultiPoint(pGeometry.Coordinates);
                        //MultiPoint polylineCollection = pGeometry as MultiPoint;
                        if (polylineCollection.Count > 0)
                        {
                            //重新设置地层线
                            List<Point> newPoints = new List<Point>() { source2.StartPoint as Point, source.EndPoint as Point };
                            LineString newLine = convertGeometriesHelper.ConvertPointsToLine(newPoints);
                            StratigraphicAttribute attributeLeft = resultAttribute[j].attributeLeft;
                            StratigraphicAttribute attributeRight = resultAttribute[j].attributeRight;
                            string lineType = resultAttribute[j].lineType;
                            double tendency = resultAttribute[j].tendency;
                            double angle = resultAttribute[j].angle;
                            double sectionLineAngle = resultAttribute[j].sectionLineAngle;
                            double sightAngle = resultAttribute[j].sightAngle;
                            int fid = resultAttribute[j].LineID;
                            resultAttribute[j] = new StratigraphicAttribute_Polyline(attributeLeft,
                                attributeRight, lineType, tendency, angle, sectionLineAngle, sightAngle, newLine, fid);


                        }
                    }
                }

                return resultAttribute;
            }

        }

        private List<StratigraphicAttribute_Polyline> PostProcessingStratumLine_lastanalysis(List<StratigraphicAttribute_Polyline> stratumLinesAttribute)
        {
            List<StratigraphicAttribute_Polyline> resultAttribute = stratumLinesAttribute;
            List<StratigraphicAttribute_Polyline> resultAttributetemp = new List<StratigraphicAttribute_Polyline>();
            //判断地层线顺序问题
            for (int i = 0; i< resultAttribute.Count-1; i++)
            {
                if (resultAttribute[i].attributeRight.DSN.ToString() != resultAttribute[i + 1].attributeLeft.DSN.ToString())
                {

                    resultAttributetemp.Add(resultAttribute[i]);

                    StratigraphicAttribute attributeLeftfirst = resultAttribute[i +2].attributeLeft;
                    StratigraphicAttribute attributeRightfirst = resultAttribute[i + 2].attributeRight;
                    double sightAnglefirst = resultAttribute[i + 2].sightAngle;
                    double anglefirst = resultAttribute[i + 2].angle;
                    LineString newLine = resultAttribute[i + 1].Line;
                    string lineType = resultAttribute[i + 2].lineType;
                    double tendency = resultAttribute[i + 2].tendency;
                    double sectionLineAngle = resultAttribute[i + 2].sectionLineAngle;
                    int fid = resultAttribute[i + 2].LineID;
                    StratigraphicAttribute_Polyline resultAttributenew = new StratigraphicAttribute_Polyline(attributeLeftfirst,
                        attributeRightfirst, lineType, tendency, anglefirst, sectionLineAngle, sightAnglefirst, newLine, fid);
                    resultAttributetemp.Add(resultAttributenew);

                     attributeLeftfirst = resultAttribute[i + 1].attributeLeft;
                     attributeRightfirst = resultAttribute[i + 1].attributeRight;
                     sightAnglefirst = resultAttribute[i +1].sightAngle;
                     anglefirst = resultAttribute[i + 1].angle;
                     newLine = resultAttribute[i + 2].Line;
                     lineType = resultAttribute[i + 1].lineType;
                     tendency = resultAttribute[i + 1].tendency;
                     sectionLineAngle = resultAttribute[i + 1].sectionLineAngle;
                     fid = resultAttribute[i + 1].LineID;
                    resultAttributenew = new StratigraphicAttribute_Polyline(attributeLeftfirst,
                        attributeRightfirst, lineType, tendency, anglefirst, sectionLineAngle, sightAnglefirst, newLine, fid);
                    resultAttributetemp.Add(resultAttributenew);

                    i =i+2;

                    //StratigraphicAttribute attributeLeftfirst = resultAttribute[i - 1].attributeLeft;                    
                    //StratigraphicAttribute attributeRightfirst = resultAttribute[i - 1].attributeRight;
                    //double sightAnglefirst = resultAttribute[i - 1].sightAngle;
                    //double anglefirst = resultAttribute[i - 1].angle;                    
                    //LineString newLine = resultAttribute[i - 1].Line;                   
                    //string lineType = resultAttribute[i - 1].lineType;
                    //double tendency = resultAttribute[i - 1].tendency;
                    //double sectionLineAngle = resultAttribute[i - 1].sectionLineAngle;
                    //int fid = resultAttribute[i - 1].LineID;
                    //resultAttribute[i] = new StratigraphicAttribute_Polyline(attributeLeftfirst,
                    //    attributeRightfirst, lineType, tendency, anglefirst, sectionLineAngle, sightAnglefirst, newLine, fid);


                    //StratigraphicAttribute attributeLeftlast = resultAttributetemp[i].attributeLeft;
                    //StratigraphicAttribute attributeRightlastt = resultAttributetemp[i].attributeRight;
                    //double sightAnglelast = resultAttributetemp[i].sightAngle;
                    //double anglelast = resultAttributetemp[i].angle;
                    //newLine = resultAttributetemp[i].Line;
                    //lineType = resultAttributetemp[i].lineType;
                    //tendency = resultAttributetemp[i].tendency;
                    //double sectionLineAnglelastlast = resultAttributetemp[i].sectionLineAngle;
                    //fid = resultAttribute[i].LineID;
                    //resultAttribute[i-1] = new StratigraphicAttribute_Polyline(attributeLeftlast,
                    //    attributeRightlastt, lineType, tendency, anglelast, sectionLineAngle, sightAnglelast, newLine, fid);


                }
                else
                {
                    resultAttributetemp.Add(resultAttribute[i]);
                }
                
            }
            resultAttributetemp.Add(resultAttribute[resultAttribute.Count - 1]);
            return resultAttributetemp;
           

        }

        /// <summary>
        /// 后处理地层线，保留在剖面范围内的地层线
        /// </summary>
        /// <param name="groundLine"></param>
        /// <param name="bottomLine"></param>
        /// <param name="stratumLines"></param>
        /// <returns></returns>
        private List<StratigraphicAttribute_Polyline> PostProcessingStratumLine_InuptPolygon(LineString groundLine, LineString bottomLine, List<StratigraphicAttribute_Polyline> stratumLinesAttribute)
        {
            List<StratigraphicAttribute_Polyline> stratumResult = new List<StratigraphicAttribute_Polyline>();
            //左右边界生成
            // 左边界线
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            Point leftPoint0 = groundLine.StartPoint as Point;
            Point leftPoint1 = bottomLine.StartPoint as Point;
            List<Point> leftBoundaryPoints = new List<Point> { leftPoint0, leftPoint1 };

            LineString leftBoundary = convertGeometriesHelper.ConvertPointsToLine(leftBoundaryPoints);


            // 右边界线


            Point rightPoint0 = groundLine.EndPoint as Point;
            Point rightPoint1 = bottomLine.EndPoint as Point;
            List<Point> rightBoundaryPoints = new List<Point> { rightPoint0, rightPoint1 };

            LineString rightBoundary = convertGeometriesHelper.ConvertPointsToLine(rightBoundaryPoints);


            //地表线、底部线、左右边界集合
            //创建一个Polygon对象
            List<Geometry> geomes = new List<Geometry>()
            {
                groundLine, rightBoundary, bottomLine, leftBoundary
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

            // 创建 Polygonizer
            var polygonizer = new Polygonizer();
            // 添加几何对象
            foreach (Geometry geometry in validatedGeometries)
            {
                polygonizer.Add(geometry);
            }

            // 获取多边形对象
            var pPolygonGeoCol = polygonizer.GetPolygons();

            //求地层线和剖面范围的交集

            foreach (StratigraphicAttribute_Polyline sourceAttribute in stratumLinesAttribute)
            {
                List<Geometry> polylineCollection = new List<Geometry>();
                foreach (IGeometry enve in pPolygonGeoCol)
                {
                    GeometryFactory geos = new GeometryFactory();
                    LineString source = sourceAttribute.Line;
                    var pGeometry = (Geometry)geos.CreateGeometry(source);
                    //Geometry pGeometry = enve.Intersection(source) as Geometry;
                    if (pGeometry.IsEmpty)
                    {
                        continue;
                    }
                    polylineCollection.Add(pGeometry);
                }

                if (polylineCollection.Count > 0)
                {
                    for (int g = 0; g < polylineCollection.Count; g++)
                    {
                        IGeometry geometryPath = polylineCollection[g];

                        Point p0 = new Point(geometryPath.Coordinates[0]);
                        Point p1 = new Point(geometryPath.Coordinates[geometryPath.NumPoints - 1]);

                        List<Point> newLinePoints = new List<Point> { p0, p1 };
                        LineString newline = convertGeometriesHelper.ConvertPointsToLine(newLinePoints);
                        StratigraphicAttribute_Polyline newlineAttribute = new StratigraphicAttribute_Polyline();
                        newlineAttribute.Line = newline;
                        newlineAttribute.LineID = sourceAttribute.LineID;
                        newlineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                        newlineAttribute.attributeRight = sourceAttribute.attributeRight;
                        newlineAttribute.lineType = sourceAttribute.lineType;
                        newlineAttribute.tendency = sourceAttribute.tendency;
                        newlineAttribute.angle = sourceAttribute.angle;
                        newlineAttribute.sightAngle = sourceAttribute.sightAngle;
                        newlineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;
                        stratumResult.Add(newlineAttribute);

                    }
                    /*IPolyline newline = polylineCollection as IPolyline;
                    StratigraphicAttribute_Polyline newlineAttribute = new StratigraphicAttribute_Polyline();
                    newlineAttribute.Line = newline;
                    newlineAttribute.attributeLeft = sourceAttribute.attributeLeft;
                    newlineAttribute.attributeRight = sourceAttribute.attributeRight;
                    newlineAttribute.tendency = sourceAttribute.tendency;
                    newlineAttribute.angle = sourceAttribute.angle;
                    newlineAttribute.sightAngle = sourceAttribute.sightAngle;
                    newlineAttribute.sectionLineAngle = sourceAttribute.sectionLineAngle;
                    stratumResult.Add(newlineAttribute);*/
                }
                else
                {
                    stratumResult.Add(sourceAttribute);
                }

            }
            
            Comparison<StratigraphicAttribute_Polyline> comparison = new Comparison<StratigraphicAttribute_Polyline>(SortByvalue);//将方法传给委托实例
            stratumResult.Sort(comparison);

            //增加上下左右四个界限
            //地表线
            StratigraphicAttribute_Polyline groundLineAtt_Polyline = new StratigraphicAttribute_Polyline();
            groundLineAtt_Polyline.Line = groundLine;
            groundLineAtt_Polyline.lineType = "地表线";
            groundLineAtt_Polyline.LineID = -1;

            //底部线
            StratigraphicAttribute_Polyline bottomLineAtt_Polyline = new StratigraphicAttribute_Polyline();
            bottomLineAtt_Polyline.Line = bottomLine;
            bottomLineAtt_Polyline.lineType = "底部线";
            bottomLineAtt_Polyline.LineID = -1;

            //左边界线
            StratigraphicAttribute_Polyline leftLineAtt_Polyline = new StratigraphicAttribute_Polyline();
            leftLineAtt_Polyline.Line = leftBoundary;
            leftLineAtt_Polyline.lineType = "边界线";
            leftLineAtt_Polyline.LineID = -1;
            leftLineAtt_Polyline.attributeRight = stratumResult[0].attributeLeft;

            //右边界线
            int straumlineCount = stratumLinesAttribute.Count;
            StratigraphicAttribute_Polyline rightLineAtt_Polyline = new StratigraphicAttribute_Polyline();
            rightLineAtt_Polyline.Line = rightBoundary;
            rightLineAtt_Polyline.lineType = "边界线";
            rightLineAtt_Polyline.LineID = -1;
            rightLineAtt_Polyline.attributeLeft = stratumResult[straumlineCount - 1].attributeRight;

            stratumResult.Add(groundLineAtt_Polyline);
            stratumResult.Add(bottomLineAtt_Polyline);
            stratumResult.Add(leftLineAtt_Polyline);
            stratumResult.Add(rightLineAtt_Polyline);


            return stratumResult;


        }
        /// <summary>
        /// 由图切剖面线要素生成面要素
        /// </summary>
        /// <param name="lineAttriList"></param>
        /// <returns></returns>
        private List<StratigraphicAttribute_Polygon> CreateSectionFaceFromSectionLine(GenSectionLine genSectionLine)
        {
            List<StratigraphicAttribute_Polygon> result = new List<StratigraphicAttribute_Polygon>();
            List<LineString> allLineString = new List<LineString>();
            List<StratigraphicAttribute_Polyline> lineAttriList = genSectionLine.sectionLinesAttribute;
            foreach (StratigraphicAttribute_Polyline straAttr in lineAttriList)
            {
                allLineString.Add(straAttr.Line);
            }
            ///线转面
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            ICollection<IGeometry> polygonGeometryC = convertGeometriesHelper.ConvertPolylineList2Polygon(lineAttriList);
            if (polygonGeometryC == null) return null;

            foreach (IGeometry polygon in polygonGeometryC)
            {
                List<StratigraphicAttribute_Polyline> tempLineFeatures = new List<StratigraphicAttribute_Polyline>();
                for (int i = 0; i < allLineString.Count; i++)
                {
                    LineString tempLine = allLineString[i];
                    //这里相交判断出现问题
                    IGeometry pGeometry = tempLine.Intersection(polygon);
                    double IsqGeometry = tempLine.Distance(polygon);
                    if (!pGeometry.IsEmpty || IsqGeometry < 0.01)
                    {
                        string lineType = lineAttriList[i].lineType;
                        if (lineType != "地表线" && lineType != "底部线")
                        {
                            tempLineFeatures.Add(lineAttriList[i]);
                            continue;
                        }
                    }



                }

                StratigraphicAttribute_Polygon temPolygon = new StratigraphicAttribute_Polygon();
                int l = tempLineFeatures.Count;
                if (l > 0)
                {
                    if (l >= 2)
                    {
                        StratigraphicAttribute_Polyline featureLine1 = tempLineFeatures[0];
                        StratigraphicAttribute_Polyline featureLine2 = tempLineFeatures[1];

                        string linetype1 = featureLine1.lineType;

                        string linetype2 = featureLine2.lineType;


                        //获得要素1的中心点
                        LineString line1 = featureLine1.Line;
                        Point centerPoint1 = new Point(Centroid.GetCentroid(line1));
                        double line1FrompointX = Math.Floor(line1.StartPoint.X * 1000) / 1000;
                        //获得要素2的中心点
                        LineString line2 = featureLine2.Line;
                        Point centerPoint2 = new Point(Centroid.GetCentroid(line2));
                        double line2FrompointX = Math.Floor(line2.EndPoint.X * 1000) / 1000;
                        double aveFromPointsX = Math.Floor(((line1FrompointX + line2FrompointX) / 2) * 1000) / 1000;


                        temPolygon.polygon = polygon as Geometry;
                        //要素1在左
                        if (centerPoint1.X < centerPoint2.X)
                        {

                            string aa = featureLine1.attributeRight.DSN;
                            string bb = featureLine2.attributeLeft.DSN;

                            string cc = featureLine1.attributeRight.DSO;
                            string dd = featureLine2.attributeLeft.DSO;
                            StratigraphicAttribute tempAttr = new StratigraphicAttribute();
                            if (cc != " ")
                            {

                                tempAttr.DSN = aa;
                                tempAttr.DSO = cc;
                                temPolygon.attribute = tempAttr;

                            }
                            else
                            {
                                if (dd.ToString() != " ")
                                {

                                    tempAttr.DSN = bb;
                                    tempAttr.DSO = dd;
                                    temPolygon.attribute = tempAttr;
                                }
                                else
                                {
                                    tempAttr.DSN = "其它";
                                    tempAttr.DSO = "other";
                                    temPolygon.attribute = tempAttr;

                                    /*pFeature.set_Value(DSNIndex, "浦口组");
                                    pFeature.set_Value(DSOIndex, "K↓2→p");*/

                                }

                            }
                            //找到该面图层所对应的平面面id
                            double selectPointsY = aveFromPointsX;
                            Point selectPoints = new Point(new Coordinate());
                            selectPoints.X = genSectionLine.m_sectionValue;
                            //平面上真实的y坐标
                            selectPoints.Y = selectPointsY + this.YMin;

                            int faceOID = 0;
                            //int faceOID = Convert.ToInt32(GetFieldValueByPoint(selectPoints, this.StratumGroupLayer, "class"));
                            temPolygon.faceId = faceOID;

                        }
                        //要素1在右
                        else if (centerPoint1.X >= centerPoint2.X)
                        {
                            string aa = featureLine1.attributeLeft.DSN;
                            string bb = featureLine2.attributeRight.DSN;


                            string cc = featureLine1.attributeLeft.DSO;
                            string dd = featureLine2.attributeRight.DSO;
                            StratigraphicAttribute tempAttr = new StratigraphicAttribute();

                            if (dd != " ")
                            {
                                tempAttr.DSN = bb;
                                tempAttr.DSO = dd;
                                temPolygon.attribute = tempAttr;
                            }
                            else
                            {
                                if (cc.ToString() != " ")
                                {
                                    tempAttr.DSN = aa;
                                    tempAttr.DSO = cc;
                                    temPolygon.attribute = tempAttr;
                                }
                                else
                                {
                                    tempAttr.DSN = "其它";
                                    tempAttr.DSO = "other";
                                    temPolygon.attribute = tempAttr;
                                    /*tempAttr.DSN = "浦口组";
                                    tempAttr.DSO = "K↓2→p";
                                    temPolygon.attribute = tempAttr;*/

                                }

                            }

                            //找到该面图层所对应的平面面id
                            double selectPointsY = aveFromPointsX;
                            Point selectPoints = new Point(new Coordinate());
                            selectPoints.X = genSectionLine.m_sectionValue;
                            //平面上真实的y坐标
                            selectPoints.Y = selectPointsY + this.YMin;
                            int faceOID = 0;
                            //int faceOID = Convert.ToInt32(GetFieldValueByPoint(selectPoints, this.StratumGroupLayer, "class"));
                            temPolygon.faceId = faceOID;


                        }

                    }
                    else if (l == 1)
                    {
                        StratigraphicAttribute_Polyline featureLine1 = tempLineFeatures[0];

                        //获得要素1的中心点
                        LineString line1 = featureLine1.Line;
                        Point centerPoint1 = new Point(Centroid.GetCentroid(line1));//获得要素1的中心点
                        double line1FrompointX = Math.Floor(line1.StartPoint.X * 1000) / 1000;



                        IPoint centerPoint2 = polygon.Centroid;//获得面要素的中心点

                        temPolygon.polygon = polygon as Geometry;

                        if (centerPoint1.X < centerPoint2.X)
                        {
                            string aa = featureLine1.attributeRight.DSN;
                            string cc = featureLine1.attributeRight.DSO;
                            StratigraphicAttribute tempAttr = new StratigraphicAttribute();


                            if (cc.ToString() != " ")
                            {
                                tempAttr.DSN = aa;
                                tempAttr.DSO = cc;
                                temPolygon.attribute = tempAttr;

                            }
                            else
                            {
                                tempAttr.DSN = "其它";
                                tempAttr.DSO = "other";
                                temPolygon.attribute = tempAttr;
                                /*pFeature.set_Value(DSNIndex, "浦口组");
                                    pFeature.set_Value(DSOIndex, "K↓2→p");*/
                            }
                            //找到该面图层所对应的平面面id

                            double selectPointsY = line1FrompointX + 1; ;
                            Point selectPoints = new Point(new Coordinate());
                            selectPoints.X = genSectionLine.m_sectionValue;
                            //平面上真实的y坐标
                            selectPoints.Y = selectPointsY + this.YMin;

                            int faceOID = 0;
                            //int faceOID = Convert.ToInt32(GetFieldValueByPoint(selectPoints, this.StratumGroupLayer, "class"));
                            temPolygon.faceId = faceOID;

                        }
                        else
                        {

                            string aa = featureLine1.attributeLeft.DSN;
                            string cc = featureLine1.attributeLeft.DSO;
                            StratigraphicAttribute tempAttr = new StratigraphicAttribute();

                            if (cc.ToString() != " ")
                            {
                                tempAttr.DSN = aa;
                                tempAttr.DSO = cc;
                                temPolygon.attribute = tempAttr;
                            }
                            else
                            {
                                tempAttr.DSN = "其它";
                                tempAttr.DSO = "other";
                                temPolygon.attribute = tempAttr;
                                /*pFeature.set_Value(DSNIndex, "浦口组");
                                pFeature.set_Value(DSOIndex, "K↓2→p");*/
                            }
                            //找到该面图层所对应的平面面id

                            double selectPointsY = line1FrompointX - 1; ;
                            Point selectPoints = new Point(new Coordinate());
                            selectPoints.X = genSectionLine.m_sectionValue;
                            //平面上真实的y坐标
                            selectPoints.Y = selectPointsY + this.YMin;

                            int faceOID = 0;
                            //int faceOID = Convert.ToInt32(GetFieldValueByPoint(selectPoints, this.StratumGroupLayer, "class"));
                            temPolygon.faceId = faceOID;
                        }


                    }

                    result.Add(temPolygon);

                }
            }

            return result;
        }

    }
}
