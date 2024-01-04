using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{

    public  class OrigonLine
    {
        public int Id;
        //线要素
        public LineString lineString;
        //线方向（默认为X方向）：X-南北走向，Y东西走向
        public string Direction;

        public OrigonLine()
        {
        }

        public OrigonLine(int id, LineString linestring, string direction = "X")
        {
            this.Id = id;
            lineString = linestring;
            Direction = direction;
        }
        /// <summary>
        /// 将剖面线转为可用线要素
        /// </summary>
        /// <param name="stratumLinesAttribute"></param>
        /// <param name="workPath"></param>
        public FeatureCollection  CreateSectionPolyLineFeatureLayer(List<OrigonLine> stratumLinesAttribute, string workPath)
        {

            FeatureCollection featureCollection = new FeatureCollection();
            string wkt;
            WKTReader wktReader = new WKTReader();
            string fileName = "";
            string direction = stratumLinesAttribute[0].Direction;
            fileName = "origonLine_" + direction;
            for (int i = 0; i < stratumLinesAttribute.Count; i++)
            {
                var origonLineAtt = stratumLinesAttribute[i];
                var polyline = origonLineAtt.lineString;
                wkt = polyline.AsText();
                var geometry = wktReader.Read(wkt);
                var feature = new Feature();

                featureCollection.Add(feature);
                feature.Geometry = geometry;
                var attributeTable = new AttributesTable();
                feature.Attributes = attributeTable;
                attributeTable.AddAttribute("id", i);

                string coorDirectionField = "x";

                IPoint startPoint = polyline.StartPoint;
                double coor = 0.0;
                if (direction == "X")
                {
                    coorDirectionField = "x";
                    coor = startPoint.X;

                }
                else
                {
                    coorDirectionField = "y";
                    coor = startPoint.Y;

                }

                attributeTable.AddAttribute(coorDirectionField, coor);



            }
            string filePath = workPath + "\\" + fileName;
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(featureCollection[0], featureCollection.Count);
            dataWriter.Write(featureCollection.Features);
            return featureCollection;






        }
    }
}
