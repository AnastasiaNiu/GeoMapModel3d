using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonDataStructureLib.RockStratumModel;

namespace CommonMethodHelpLib
{
    /// <summary>
    /// 矢量帮助类
    /// </summary>
    public class ShapeFileHelper
    {
        public FeatureCollection pFeatureCollection { get; set; }


        public ShapeFileHelper(string shapeFilePath)
        {
            if(shapeFilePath.Contains(".shp"))
            {
                pFeatureCollection = ReadShpFile(shapeFilePath);
            }
            

        }
      
        public FeatureType featureType
        {
            get
            {
                string type = pFeatureCollection.Features[0].Geometry.GeometryType;
                switch (type)
                {
                    case "Point":
                        return FeatureType.Point;
                    case "LineString":
                        return FeatureType.PolyLine;
                    default:
                        return FeatureType.Polygon;
                }
            }
        }

        /// <summary>
        /// 读取Shapefile(S)
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public FeatureCollection ReadShpFile(string pathName)
        {
            FeatureCollection featureCollection = new FeatureCollection();
            IGeometryFactory gfactory = GeometryFactory.Floating;
            if (pathName == "")
            {
                return null;
            }
            ShapefileDataReader dataReader = new ShapefileDataReader(pathName, gfactory);
            while (dataReader.Read())
            {
                Feature feature = new Feature { Geometry = dataReader.Geometry };
                int length = dataReader.DbaseHeader.NumFields;
                var keys = new string[length];
                for (var i = 0; i < length; i++)
                {
                    keys[i] = dataReader.DbaseHeader.Fields[i].Name;
                }
                feature.Attributes = new AttributesTable();
                for (var i = 0; i < length; i++)
                {
                    var val = dataReader.GetValue(i);
                    string value = val.ToString();
                    feature.Attributes.AddAttribute(keys[i], value);
                }
                featureCollection.Add(feature);
            }
            return featureCollection;
            dataReader.Dispose();
        }
        public IFeature ReadOneShpFile(int t)
        {

            return pFeatureCollection[t];
        }
        /// <summary>
        /// 读取多个Shapefile
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public FeatureCollection ReadShpFiles(string pathName)
        {
            if (pathName == "")
            {
                return null;
            }
            FeatureCollection featureCollection = new FeatureCollection();
            IGeometryFactory gfactory = GeometryFactory.Floating;
            ShapefileDataReader dataReader = new ShapefileDataReader(pathName, gfactory);
            while (dataReader.Read())
            {
                Feature feature = new Feature { Geometry = dataReader.Geometry };
                int length = dataReader.DbaseHeader.NumFields;
                var keys = new string[length];
                for (var i = 0; i < length; i++)
                {
                    keys[i] = dataReader.DbaseHeader.Fields[i].Name;
                }

                feature.Attributes = new AttributesTable();

                for (var i = 0; i < length; i++)
                {
                    var val = dataReader.GetValue(i);
                    string value = val.ToString();
                    feature.Attributes.AddAttribute(keys[i], value);

                }


                featureCollection.Add(feature);
            }
            return featureCollection;
        }
        /// <summary>
        /// 读取Shapefile字段
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public List<string> ReadShpFileField(string pathName)
        {
            List<string> fields = new List<string>();
            FeatureCollection featureCollection = new FeatureCollection();
            IGeometryFactory gfactory = GeometryFactory.Floating;
            ShapefileDataReader dataReader = new ShapefileDataReader(pathName, gfactory);
            int length = dataReader.DbaseHeader.NumFields;
            for (var i = 0; i < length; i++)
            {
                fields.Add(dataReader.DbaseHeader.Fields[i].Name);
            }
            return fields;
        }

        /// <summary>
        /// 计算xmin、ymin、xmax、ymax
        /// </summary>
        /// <param name="featureCollection"></param>
        /// <param name="xMin"></param>
        /// <param name="yMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMax"></param>
        public void GetExtent(out double xMin, out double yMin, out double xMax, out double yMax)
        {
            var geoms = pFeatureCollection.Features;
            var bbox = geoms[0].Geometry.EnvelopeInternal;
            for (int i = 1; i < geoms.Count; i++)
                bbox.ExpandToInclude(geoms[i].Geometry.EnvelopeInternal);
            var bboxGeom = geoms[0].Geometry.Factory.ToGeometry(bbox);
            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            for (int i = 0; i < bboxGeom.Coordinates.Count(); i++)
            {
                xList.Add(bboxGeom.Coordinates[i].X);
                yList.Add(bboxGeom.Coordinates[i].Y);
            }
            xMin = xList.Min();
            yMin = yList.Min();
            xMax = xList.Max();
            yMax = yList.Max();
        }
        /// <summary>
        /// 从文件夹中获取所有指定扩展名的文件名
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public List<string> GetAllSectionNamesFromFolder(string folderName, string exName)
        {
            List<string> result = new List<string>();
            List<FileInfo> lstFiles = Getdir(folderName, exName);
            foreach (FileInfo f in lstFiles)
            {
                string fileName = f.Name;
                string m_FileName = fileName.Replace("Point.shp", "");
                m_FileName = m_FileName.Replace("Polyline.shp", "");
                m_FileName = m_FileName.Replace("Polygon.shp", "");
                if (result.Count == 0)
                {
                    result.Add(m_FileName);
                }
                else
                {
                    if (result[result.Count - 1] == m_FileName)
                    {
                        continue;
                    }
                    else
                    {
                        result.Add(m_FileName);
                    }
                }


            }

            return result;

        }
        /// <summary>
        /// 从文件夹中获取所有指定扩展名的文件信息
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extName"></param>
        /// <returns></returns>
        private List<FileInfo> Getdir(string path, string extName)
        {
            try
            {
                List<FileInfo> lst = new List<FileInfo>();
                string[] dir = System.IO.Directory.GetDirectories(path); //文件夹列表   
                DirectoryInfo fdir = new DirectoryInfo(path);
                FileInfo[] file = fdir.GetFiles();
                //FileInfo[] file = Directory.GetFiles(path); //文件列表   
                if (file.Length != 0 || dir.Length != 0) //当前目录文件或文件夹不为空                   
                {
                    foreach (FileInfo f in file) //显示当前目录所有文件   
                    {
                        if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0)
                        {
                            lst.Add(f);
                        }
                    }
                    foreach (string d in dir)
                    {
                        Getdir(d, extName);//递归   
                    }
                }

                return lst;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }
        /// <summary>
        /// 判断点是否在面内
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        public int PointInPolygon(double X, double Y)
        {
            Point point = new Point(X, Y);
            for (int i = 0; i < pFeatureCollection.Features.Count; i++)
            {
                if (pFeatureCollection.Features[i].Geometry.Intersects(point))
                {
                    return i;
                }

            }
            return -1;
        }

        /// <summary>
        /// 根据点坐标，获取面的值，输入：坐标，值的字段名
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="FieldName"></param>
        /// <returns></returns>        
        public string GetFieldValue(double X, double Y, string FieldName)
        {
            string value = null;
            int index = this.PointInPolygon(X, Y);
            if (index != -1)
            {
                value = pFeatureCollection.Features[index].Attributes[FieldName].ToString();
            }
            return value;
        }

        /// <summary>
        ///  输出带属性的剖面线要素图层
        /// </summary>
        /// <param name="sectionLines"></param>
        public void CreateSectionPolyLineFeatureLayer(GenSectionLine sectionLines, string workPath)
        {

            FeatureCollection featureCollection = new FeatureCollection();

            string wkt;
            WKTReader wktReader = new WKTReader();
            string fileName = "";

            List<StratigraphicAttribute_Polyline> stratumLinesAttribute = sectionLines.sectionLinesAttribute;

            string direction = sectionLines.Direction;


            fileName = sectionLines.m_sectionName + "Polyline";




            for (int i = 0; i < stratumLinesAttribute.Count; i++)
            {
                var stratumLineAtt = stratumLinesAttribute[i];
                var polyline = stratumLineAtt.Line;
                wkt = polyline.AsText();
                var geometry = wktReader.Read(wkt);
                var feature = new Feature();

                featureCollection.Add(feature);
                feature.Geometry = geometry;
                var attributeTable = new AttributesTable();
                feature.Attributes = attributeTable;
                attributeTable.AddAttribute("id", i);
                attributeTable.AddAttribute("LineType", stratumLineAtt.lineType);
                attributeTable.AddAttribute("LineId", stratumLineAtt.LineID);
                attributeTable.AddAttribute("leftDSN", stratumLineAtt.attributeLeft.DSN);
                attributeTable.AddAttribute("leftDSO", stratumLineAtt.attributeLeft.DSO);
                attributeTable.AddAttribute("rightDSN", stratumLineAtt.attributeRight.DSN);
                attributeTable.AddAttribute("rightDSO", stratumLineAtt.attributeRight.DSO);
                attributeTable.AddAttribute("tendency", stratumLineAtt.tendency);
                attributeTable.AddAttribute("angle", stratumLineAtt.angle);
                attributeTable.AddAttribute("sLAngle", stratumLineAtt.attributeRight.DSN);
                attributeTable.AddAttribute("sightAngle", stratumLineAtt.sightAngle);
                attributeTable.AddAttribute("x", sectionLines.x);
                attributeTable.AddAttribute("y", sectionLines.y);
                attributeTable.AddAttribute("z", 0);
                attributeTable.AddAttribute("htalMin", sectionLines.horizontalMin);



            }
            string filePath = workPath + "\\" + fileName;
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(featureCollection[0], featureCollection.Count);
            dataWriter.Write(featureCollection.Features);


        }
        /// <summary>
        /// 输出带属性的图切剖面面要素文件
        /// </summary>
        /// <param name="genSectionFace"></param>
        public void CreateSectionPolygoneatureLayer(GenSectionFace genSectionFace, string workPath)
        {

            FeatureCollection featureCollection = new FeatureCollection();

            string wkt;
            WKTReader wktReader = new WKTReader();

            string fileName = "";
            List<StratigraphicAttribute_Polygon> stratumFaceAttribute = genSectionFace.sectionFaceAttribute;
            string direction = genSectionFace.Direction;


            fileName = genSectionFace.m_sectionName + "Polygon";
            string filePath = workPath + "\\" + fileName;
            for (int i = 0; i < stratumFaceAttribute.Count; i++)
            {
                var stratumFaceAtt = stratumFaceAttribute[i];
                var polygon = stratumFaceAtt.polygon;
                wkt = polygon.AsText();
                var geometry = wktReader.Read(wkt);
                var feature = new Feature();

                featureCollection.Add(feature);
                feature.Geometry = geometry;
                var attributeTable = new AttributesTable();
                feature.Attributes = attributeTable;
                attributeTable.AddAttribute("id", i);

                attributeTable.AddAttribute("faceId", stratumFaceAtt.faceId);
                attributeTable.AddAttribute("DSN", stratumFaceAtt.attribute.DSN);
                attributeTable.AddAttribute("DSO", stratumFaceAtt.attribute.DSO);

                attributeTable.AddAttribute("x", genSectionFace.x);
                attributeTable.AddAttribute("y", genSectionFace.y);
                attributeTable.AddAttribute("z", 0);


            }

            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(featureCollection[0], featureCollection.Count);
            dataWriter.Write(featureCollection.Features);
        }
        /// <summary>
        ///  输出带属性的剖面线要素图层
        /// </summary>
        /// <param name="sectionLines"></param>
        public void CreateSectionDrillFeatureLayer(GenSectionVirtualDrill sectionPoints, string workPath)
        {

            FeatureCollection featureCollection = new FeatureCollection();

            string wkt;
            WKTReader wktReader = new WKTReader();
            string fileName = "";

            List<StratigraphicAttribute_VDirll> virtualDrillsAttribute = sectionPoints.DrillAttribute;




            fileName = sectionPoints.m_sectionName + "Point";




            for (int i = 0; i < virtualDrillsAttribute.Count; i++)
            {
                var virtualDrillAtt = virtualDrillsAttribute[i];
                var drill = virtualDrillAtt.VPoint;
                wkt = drill.AsText();
                var geometry = wktReader.Read(wkt);
                var feature = new Feature();

                featureCollection.Add(feature);
                feature.Geometry = geometry;
                var attributeTable = new AttributesTable();
                feature.Attributes = attributeTable;
                attributeTable.AddAttribute("id", i);
                attributeTable.AddAttribute("LineType", virtualDrillAtt.LineType);
                attributeTable.AddAttribute("faceId", virtualDrillAtt.FaceId);
                attributeTable.AddAttribute("LineId", virtualDrillAtt.LineId);
                attributeTable.AddAttribute("DSN", virtualDrillAtt.DSN);
                attributeTable.AddAttribute("DSO", virtualDrillAtt.DSO);


                attributeTable.AddAttribute("x", virtualDrillAtt.x);
                attributeTable.AddAttribute("y", virtualDrillAtt.y);
                attributeTable.AddAttribute("h", virtualDrillAtt.z);
                attributeTable.AddAttribute("soildType", "10086");




            }
            string filePath = workPath + "\\" + fileName;
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(featureCollection[0], featureCollection.Count);
            dataWriter.Write(featureCollection.Features);







        }
    }
    /// <summary>
    /// 矢量类型
    /// </summary>
    public enum FeatureType
    {
        /// <summary>
        /// 面
        /// </summary>
        Polygon,
        /// <summary>
        /// 线
        /// </summary>
        PolyLine,
        /// <summary>
        /// 点
        /// </summary>
        Point
    }
}
