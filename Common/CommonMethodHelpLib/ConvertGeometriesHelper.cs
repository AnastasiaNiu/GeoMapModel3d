using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Polygonize;
using NetTopologySuite.Operation.Union;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonDataStructureLib.RockStratumModel;

namespace CommonMethodHelpLib
{
    public  class ConvertGeometriesHelper
    {
        /// <summary>
        /// 单条线转点
        /// </summary>
        /// <param name="Line"></param>
        /// <returns></returns>
        public List<Point> ConvertLineToPoints(IGeometry Line)
        {
            List<Point> result = new List<Point>();
            Coordinate[] coordinates = Line.Coordinates;

            //将线转点
            for (int h = 0; h < coordinates.Length; h++)
            {
                Point point = new Point(coordinates[h]);
                result.Add(point);
            }
            return result;
        }

        /// <summary>
        /// 点转线
        /// </summary>
        /// <param name="pointList"></param>
        /// <returns></returns>
        public LineString ConvertPointsToLine(List<Point> pointList)
        {
            int num = pointList.Count;
            var coorArray = new Coordinate[num];
            for (int i = 0; i < num; i++)
            {
                coorArray[i] = pointList[i].Coordinate;
            }
            LineString result = new LineString(coorArray);
            return result;
        }
        /// <summary>
        /// 线转面
        /// </summary>
        /// <param name="lineAttriList"></param>
        /// <returns></returns>
        public  ICollection<IGeometry> ConvertPolylineList2Polygon(List<StratigraphicAttribute_Polyline> lineAttriList)
        {

            //取得所有线
            List<LineString> limitlineList = new List<LineString>();
            List<LineString> PolylineList = new List<LineString>();

            foreach (StratigraphicAttribute_Polyline straAttr in lineAttriList)
            {

                //将断层地层线抽出
                if (straAttr.lineType == "地层线" || straAttr.lineType == "断层线")
                {
                    limitlineList.Add(straAttr.Line);
                }
                else
                {
                    PolylineList.Add(straAttr.Line);
                }
            }

            PolylineList.Add(limitlineList[0]);
            //对地层断层线进行判断处理，如果存在相交，那么将线缩短
            for (int n = 0; n < limitlineList.Count - 1; n++)
            {

                //将处理好的断层地层线加入到polylinelist
                if (!limitlineList[n].Intersects(limitlineList[n + 1]))
                {
                    PolylineList.Add(limitlineList[n + 1]);
                }
                else
                {
                    var geo = new GeometryFactory();
                    var newm = ExtendLine(limitlineList[n + 1], 10, 1);
                    //limitlineList.Remove(limitlineList[n + 1]);
                    if (!limitlineList[n].Intersects(newm))
                    {
                        PolylineList.Add(newm);
                    }
                }
            }

            PolylineList.Distinct();

            //截取单独线段
            var result = new UnaryUnionOp(PolylineList).Union();
            //将单独线段加入到Collection<IGeometry> a中
            Collection<IGeometry> a = new Collection<IGeometry>();
            for (int k = 0; k < result.NumGeometries; k++)
            {
                a.Add(result.GetGeometryN(k));
            }
            //对多线进行循环剔除，保证每个面要素生成
            ICollection<IGeometry> mult = new Collection<IGeometry>();
            for (int k = a.Count - 1; k >= 0; k--)
            {
                List<IGeometry> geomes = new List<IGeometry>();
                for (int i = a.Count - 1; i >= 0; i--)
                {
                    var one = a[i];
                    geomes.Add(one);

                }
                //剔除单条，并生成面
                var random = new Random();
                geomes.RemoveAt(k);
                var polygonizer = new Polygonizer();
                polygonizer.Add(geomes);
                polygonizer.IsCheckingRingsValid = false;
                var bondaryOfRegion = polygonizer.GetPolygons();
                foreach (var one in bondaryOfRegion)
                {
                    mult.Add(one);
                }

            }
            var newmult = mult.Distinct().ToList();
            Collection<IGeometry> resultply = new Collection<IGeometry>();
            for (int k = 0; k < newmult.Count; k++)
            {
                for (int m = 0; m < newmult.Count; m++)
                {
                    if (m != k)
                    {
                        if (newmult[k].Within(newmult[m]) && newmult[m].Area >= newmult[k].Area)
                        {
                            newmult.Remove(newmult[m]);
                        }
                        else if (newmult[m].InteriorPoint == newmult[k].InteriorPoint)
                        {
                            newmult.Remove(newmult[m]);
                        }
                    }

                }
            }
            Comparison<IGeometry> comparison = new Comparison<IGeometry>(SortByvalue);//将方法传给委托实例
            newmult.Sort(comparison);
            return newmult;


        }
        static int SortByvalue(IGeometry x, IGeometry y)
        {
            return x.Centroid.X.CompareTo(y.Centroid.X);
        }
        public static LineString ExtendLine(LineString inPutLine, double length, int direction)
        {
            double fraction = length / inPutLine.Length;// 计算延长线段的长度分数
            //向起点方向延长
            if (direction == 0)
            {
                //startIndex长度偏移量
                var startIndex = 1 + fraction; // 计算延长线段的起点位置


                var extendedLine = new LineString(
                    new[] {
                            new LengthIndexedLine(inPutLine).ExtractPoint(startIndex),

                               inPutLine.EndPoint.Coordinate
                    });
                return extendedLine;
            }
            //向终点方向延长
            else if (direction == 1)
            {
                var endIndex = 0 - fraction; // 计算延长线段的终点位置
                var extendedLine = new LineString(
                    new[] {
                            inPutLine.StartPoint.Coordinate,

                               new LengthIndexedLine(inPutLine).ExtractPoint(endIndex)
                    });
                return extendedLine;
            }
            else
            {
                var startIndex = 1 + fraction; // 计算延长线段的起点位置
                var endIndex = 0 - fraction; // 计算延长线段的终点位置

                var extendedLine = new LineString(
                    new[] {

                        new LengthIndexedLine(inPutLine).ExtractPoint(startIndex),
                        new LengthIndexedLine(inPutLine).ExtractPoint(endIndex),

                    });
                return extendedLine;
            }

        }
        /// <summary>
        /// 要素集合转为可查可用地层线
        /// </summary>
        /// <param name="gfc"></param>
        /// <returns></returns>
        public  GenSectionLine ConvertFeationCollection2GenL(FeatureCollection gfc)
        {
            GenSectionLine result = new GenSectionLine();
            Feature f1 = gfc[0] as Feature;
            if (Convert.ToDouble(f1.Attributes["x"]) != 0)
            {
                result.Direction = "X";
                result.x = Convert.ToDouble(f1.Attributes["x"]);
                //result.horizontalMin = Convert.ToDouble(f1.Attributes["horizontal"]);
                result.m_sectionName = (Convert.ToDouble(result.x) * 10000).ToString();
                result.m_sectionValue = result.x;
                result.y = 0;
            }
            else
            {
                result.Direction = "Y";
                result.y = Convert.ToDouble(f1.Attributes["y"]);
                result.horizontalMin = Convert.ToDouble(f1.Attributes["horizontal"]);
                result.m_sectionName = (Convert.ToDouble(result.y) * 10000).ToString();
                result.m_sectionValue = result.y;
                result.x = 0;
            }


            List<StratigraphicAttribute_Polyline> resultLineList = new List<StratigraphicAttribute_Polyline>();
            for (int i = 0; i < gfc.Count; i++)
            {
                Feature f = gfc[i] as Feature;
                Geometry geometry = f.Geometry as Geometry;
                GeometryFactory geo = new GeometryFactory();
                var line = (LineString)geo.CreateLineString(geometry.Coordinates);
                StratigraphicAttribute al = new StratigraphicAttribute();
                al.DSN = f.Attributes["leftDSN"].ToString();
                al.DSO = f.Attributes["leftDSO"].ToString();

                StratigraphicAttribute ar = new StratigraphicAttribute();
                ar.DSN = f.Attributes["rightDSN"].ToString();
                ar.DSO = f.Attributes["rightDSO"].ToString();

                string[] fields = f.Attributes.GetNames();
                object[] objectList = new object[6];
                foreach (string name in fields)
                {
                    if (name == "LineType")
                    {
                        objectList[0] = f.Attributes[name];
                    }
                    else if (name == "tendency")
                    {
                        objectList[1] = f.Attributes[name];
                    }
                    else if (name == "angle")
                    {
                        objectList[2] = f.Attributes[name];
                    }
                    else if (name == "sectionLin")
                    {
                        objectList[3] = f.Attributes[name];
                    }
                    else if (name == "sightAngle")
                    {
                        objectList[4] = f.Attributes[name];
                    }
                    else if (name == "LineId")
                    {
                        objectList[5] = f.Attributes[name];
                    }
                    else
                    {
                        continue;
                    }

                }

                StratigraphicAttribute_Polyline lAttr = new StratigraphicAttribute_Polyline(al, ar,
                    objectList[0].ToString(),
                    Convert.ToDouble(objectList[1]),
                    Convert.ToDouble(objectList[2]),
                    Convert.ToDouble(objectList[3]),
                    Convert.ToDouble(objectList[4]),
                    line, Convert.ToInt32(objectList[5])
                    );
                resultLineList.Add(lAttr);


            }
            result.sectionLinesAttribute = resultLineList;
            return result;

        }
        /// <summary>
        /// 要素集合转为可查可用地质剖面
        /// </summary>
        /// <param name="gfc"></param>
        /// <returns></returns>
        public  GenSectionFace ConvertFeationCollection2GenF(FeatureCollection gfc)
        {
            GenSectionFace result = new GenSectionFace();
            Feature f1 = gfc[0] as Feature;
            if (Convert.ToDouble(f1.Attributes["x"]) != 0)
            {
                result.Direction = "X";
                result.x = Convert.ToDouble(f1.Attributes["x"]);

                result.m_sectionName = (Convert.ToDouble(result.x) * 10000).ToString();
                result.m_sectionValue = result.x;
                result.y = 0;
            }
            else
            {
                result.Direction = "Y";
                result.y = Convert.ToDouble(f1.Attributes["y"]);

                result.m_sectionName = (Convert.ToDouble(result.y) * 10000).ToString();
                result.m_sectionValue = result.y;
                result.x = 0;
            }


            List<StratigraphicAttribute_Polygon> resultFaceList = new List<StratigraphicAttribute_Polygon>();
            for (int i = 0; i < gfc.Count; i++)
            {
                Feature f = gfc[i] as Feature;
                Geometry geometry = f.Geometry as Geometry;
                GeometryFactory geo = new GeometryFactory();
                var face = (Geometry)geo.CreatePolygon(geometry.Coordinates);

                StratigraphicAttribute a = new StratigraphicAttribute();
                a.DSN = f.Attributes["DSN"].ToString();
                a.DSO = f.Attributes["DSO"].ToString();

                StratigraphicAttribute_Polygon fAttr = new StratigraphicAttribute_Polygon(a, face, Convert.ToInt32(f.Attributes["faceId"])
                    );
                resultFaceList.Add(fAttr);

            }
            result.sectionFaceAttribute = resultFaceList;

            return result;
        }
    }
}
