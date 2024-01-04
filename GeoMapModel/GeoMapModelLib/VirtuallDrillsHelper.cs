using CommonMethodHelpLib;
using ExcelDataReader;
using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonDataStructureLib.RockStratumModel;

namespace GeoMapModelLib
{
    internal class VirtuallDrillsHelper
    {
        public FeatureCollection PolygonCollection;
        public FeatureCollection PolylineCollection;
        public void CreateVirtuallDrills(string sectinFolderPath, string solidtypetxt, string savePath, double resety, double step)
        {
            ShapeFileHelper shapeFileHelper =new ShapeFileHelper("");
            List<string> m_SectionNameList = shapeFileHelper.GetAllSectionNamesFromFolder(sectinFolderPath, ".shp");
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            //1生成每个图切剖面的虚拟钻孔点
            foreach (string m_Name in m_SectionNameList)
            {
                string polylinePath = sectinFolderPath + "\\" + m_Name + "Polyline.shp";
                string polygonPath = sectinFolderPath + "\\" + m_Name + "Polygon.shp";
                FeatureCollection polylineC = shapeFileHelper.ReadShpFile(polylinePath);
                FeatureCollection polygonC = shapeFileHelper.ReadShpFile(polygonPath);
                
                PolylineCollection = polylineC;
                PolygonCollection = polygonC;
                GenSectionLine genL = convertGeometriesHelper.ConvertFeationCollection2GenL(polylineC);
                GenSectionFace genF = convertGeometriesHelper.ConvertFeationCollection2GenF(polygonC);
                GenCombine genCombine = new GenCombine(genF, genL);
                GenSectionVirtualDrill genSD=CreatePointsFromProfile(genCombine, genCombine.genSectionFace.Direction, step);
                shapeFileHelper.CreateSectionDrillFeatureLayer(genSD, savePath);
            }
            //2将生成的所有钻孔合并
            var mergeDrills = MergeShapefilPoint(sectinFolderPath, savePath);
            //3将合并点重新排序，确保沿xyz都有一个规范的顺序，方便后续建模
            var sortDrills = SortPoints(mergeDrills);
            //重新设置合并钻孔的xyz值
            var resetDrills = resetxyz(sortDrills, savePath);
            //4对钻孔点的soildtype赋值
            var solidDrills = convertDSOtoValue(solidtypetxt, resetDrills, savePath);
            //5对钻孔点中地表点提取并进行xy重新设值
            var profileDrills = getprofileDrills(solidDrills, resety, savePath);
            //6将所有点与地表点连接
            var connectDrills = connectingDrills(profileDrills, solidDrills, resety, savePath);

        }
        ///<summary> 
        /// 生成点文件
        /// </summary>
        /// <param name="sectionPolygonLayer">图切剖面面要素</param>
        /// <param name="sectionLineLayer">图切剖面线要素</param>
        /// <param name="direction">方向：X or Y</param>
        /// <param name="XStep">钻孔线最小线间距</param>
        /// <param name="xMaxStep">钻孔线最大间距</param>
        /// <returns></returns>
        public GenSectionVirtualDrill CreatePointsFromProfile(GenCombine genCombine, string direction, double XStep)
        {
            GenSectionLine genSectionLine = genCombine.genSectionLine;
            List<StratigraphicAttribute_Polyline> polylineFeatureClass = genSectionLine.sectionLinesAttribute;

            GenSectionFace genSectionFace = genCombine.genSectionFace;
            List<StratigraphicAttribute_Polygon> polygonFeatureClass = genSectionFace.sectionFaceAttribute;

            //自适应生成钻孔线范围
            List<double> xOriginList1 = GetOrginLineListForPoint(genSectionLine);
            //使钻孔线间距大于1
            List<double> xOriginList = MakeStepMoreThan1(xOriginList1, XStep);
            double startX = 0;
            double endX = 0;
            double minValue = genSectionLine.horizontalMin;
            //double minValue = 0;
            List<StratigraphicAttribute_VDirll> results = new List<StratigraphicAttribute_VDirll>();

            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            for (int u = 0; u < xOriginList.Count; u++)
            {
                //生成钻孔线
                double j = xOriginList[u];
                Point startPoint = new Point(new Coordinate(j, 7000));
                Point endPoint = new Point(new Coordinate(j, -5000));

                LineString clipLinetemp = convertGeometriesHelper.ConvertPointsToLine(new List<Point> { startPoint, endPoint });




                for (int i = 0; i < polylineFeatureClass.Count; i++)
                {

                    StratigraphicAttribute_Polyline featureLine = polylineFeatureClass[i];

                    string linetype = featureLine.lineType;
                    int lineid = featureLine.LineID;
                    //if (linetype.ToString() != "边界线" && linetype.ToString() != "断层线")
                    if (linetype != "边界线")
                    {
                        LineString pfeatureLine = featureLine.Line;
                        IGeometry pointCollection = clipLinetemp.Intersection(pfeatureLine);
                        int nn = pointCollection.NumPoints;
                        //MultiPoint pointCollection = geometryPoints as MultiPoint;
                        for (int h = 0; h < pointCollection.NumPoints; h++)
                        {
                            if (linetype.ToString() == "地层线" || linetype.ToString() == "断层线")
                            {
                                IGeometry selectTemp = pointCollection.GetGeometryN(h);

                                Point selectUp = new Point(new Coordinate(selectTemp.Coordinate.X, selectTemp.Coordinate.Y + 0.02));
                                Point selectDown = new Point(new Coordinate(selectTemp.Coordinate.X, selectTemp.Coordinate.Y - 0.02));

                                Feature selectPolygonUp = GetPolygonFeature(selectUp, this.PolygonCollection);
                                Feature selectPolygonDown = GetPolygonFeature(selectDown, this.PolygonCollection);



                                if (selectPolygonUp != null && selectPolygonDown != null)
                                {
                                    string DSNup = selectPolygonUp.Attributes["DSN"].ToString();
                                    string DSOup = selectPolygonUp.Attributes["DSO"].ToString();
                                    string DSNdown = selectPolygonDown.Attributes["DSN"].ToString();
                                    string DSOdown = selectPolygonDown.Attributes["DSO"].ToString();
                                    string faceIdup = selectPolygonUp.Attributes["faceId"].ToString();
                                    string faceIddown = selectPolygonDown.Attributes["faceId"].ToString();
                                    string DSN = DSNup + "_" + DSNdown;
                                    string DSO = DSOup + "_" + DSOdown;
                                    string faceId = faceIdup + "_" + faceIddown;
                                    double x, y, z;
                                    if (direction == "X")
                                    {
                                        x = Convert.ToDouble(selectPolygonUp.Attributes["x"].ToString());
                                        //y = (int)selectTemp.X + this.yMin;
                                        y = selectTemp.Coordinate.X + minValue;
                                        z = selectTemp.Coordinate.Y;
                                    }
                                    else
                                    {
                                        y = Convert.ToDouble(selectPolygonUp.Attributes["y"].ToString());
                                        //x = (int)selectTemp.X + this.xMin;
                                        x = selectTemp.Coordinate.X + minValue;
                                        z = selectTemp.Coordinate.Y;
                                    }

                                    string lineType = linetype.ToString();
                                    int lineId = (int)lineid;
                                    StratigraphicAttribute_VDirll tempAttribute = new StratigraphicAttribute_VDirll(selectTemp as Point, lineType, lineId, faceId, x, y, z, DSN, DSO);

                                    results.Add(tempAttribute);
                                }

                            }
                            else
                            {

                                IGeometry select = pointCollection.GetGeometryN(h);
                                IFeature selectPolygon;
                                if (linetype.ToString() == "地表线")
                                {

                                    endX = featureLine.Line.EndPoint.X;
                                    if (j == (double)endX)
                                    {
                                        Point select2 = new Point(new Coordinate(select.Coordinate.X - 0.05, select.Coordinate.Y - 0.05));
                                        selectPolygon = GetPolygonFeature(select2, this.PolygonCollection);
                                    }
                                    else if (j == (double)startX)
                                    {
                                        Point select2 = new Point(new Coordinate(select.Coordinate.X + 0.05, select.Coordinate.Y - 2));
                                        selectPolygon = GetPolygonFeature(select2, this.PolygonCollection);
                                    }
                                    else
                                    {
                                        Point select2 = new Point(new Coordinate(select.Coordinate.X, select.Coordinate.Y - 0.05));
                                        selectPolygon = GetPolygonFeature(select2, this.PolygonCollection);
                                    }


                                }
                                else if (linetype.ToString() == "底部线")
                                {
                                    endX = featureLine.Line.EndPoint.X;
                                    if (j == (int)endX)
                                    {

                                        Point select3 = new Point(new Coordinate(select.Coordinate.X - 0.05, select.Coordinate.Y + 0.05));
                                        selectPolygon = GetPolygonFeature(select3, this.PolygonCollection);
                                    }
                                    else if (j == (int)startX)
                                    {
                                        Point select2 = new Point(new Coordinate(select.Coordinate.X + 0.05, select.Coordinate.Y + 0.05));
                                        selectPolygon = GetPolygonFeature(select2, this.PolygonCollection);
                                    }
                                    else
                                    {

                                        Point select3 = new Point(new Coordinate(select.Coordinate.X, select.Coordinate.Y + 0.05));
                                        selectPolygon = GetPolygonFeature(select3, this.PolygonCollection);
                                    }


                                }
                                else
                                {
                                    selectPolygon = GetPolygonFeature(select as Point, this.PolygonCollection);
                                }


                                if (selectPolygon != null)
                                {
                                    string DSN = selectPolygon.Attributes["DSN"].ToString();
                                    string DSO = selectPolygon.Attributes["DSO"].ToString();
                                    string faceId = selectPolygon.Attributes["faceId"].ToString();
                                    double x, y, z;
                                    if (direction == "X")
                                    {
                                        x = Convert.ToDouble( selectPolygon.Attributes["x"].ToString());
                                        //y = (int)select.X + this.yMin; 
                                        y = select.Coordinate.X + minValue;
                                        z = select.Coordinate.Y;
                                    }
                                    else
                                    {
                                        y = Convert.ToDouble(selectPolygon.Attributes["y"].ToString());
                                        //x = (int)select.X + this.xMin;
                                        x = select.Coordinate.X + minValue;
                                        z = select.Coordinate.Y;
                                    }

                                    string lineType = linetype.ToString();
                                    int lineId = (int)lineid;
                                    StratigraphicAttribute_VDirll tempAttribute = new StratigraphicAttribute_VDirll(select as Point, lineType, lineId, faceId, x, y, z, DSN, DSO);
                                    results.Add(tempAttribute);

                                }
                            }

                        }




                    }
                    else
                    {
                        continue;
                    }


                }

            }
            GenSectionVirtualDrill resultGenDrill = new GenSectionVirtualDrill(results, genSectionFace.m_sectionName);
            return resultGenDrill;

        }
        /// <summary>
        /// 生成原始钻孔线范围合集
        /// </summary>
        public List<double> GetOrginLineListForPoint(GenSectionLine sectionLineLayer)
        {
            List<StratigraphicAttribute_Polyline> sectionLineList = sectionLineLayer.sectionLinesAttribute;
            List<double> result = new List<double>();
            List<double> xbound = new List<double>();
            ConvertGeometriesHelper convertGeometriesHelper = new ConvertGeometriesHelper();
            GeoSortHelper geoSortHelper = new GeoSortHelper();
            //寻找地表线和底部线
            StratigraphicAttribute_Polyline groundline = new StratigraphicAttribute_Polyline();
            StratigraphicAttribute_Polyline bottomline = new StratigraphicAttribute_Polyline();
            for (int i = 0; i < sectionLineList.Count; i++)
            {
                StratigraphicAttribute_Polyline structLineF = sectionLineList[i];
                string linetype = structLineF.lineType;
                if (linetype == "地表线")
                {
                    groundline = structLineF;
                }
                if (linetype == "底部线")
                {
                    bottomline = structLineF;
                }
            }

            for (int i = 0; i < sectionLineList.Count; i++)
            {
                StratigraphicAttribute_Polyline structLineF = sectionLineList[i];


                string linetype = structLineF.lineType;

                if (linetype != "地表线" && linetype != "底部线")
                {

                    LineString source = structLineF.Line;
                    var geo1 = groundline.Line.Intersection(source);
                    if (geo1.IsEmpty) continue;
                    var geo2 = bottomline.Line.Intersection(source);
                    if (geo2.IsEmpty) continue;
                    double fromX = geo1.GetGeometryN(0).Coordinate.X;
                    double toX = geo2.GetGeometryN(0).Coordinate.X;
                    if (linetype == "边界线")
                    {
                        xbound.Add(fromX);
                    }
                    if (fromX == toX)
                    {
                        result.Add(fromX);

                    }
                    else if (fromX > toX)
                    {
                        fromX = geo2.GetGeometryN(0).Coordinate.X;
                        toX = geo1.GetGeometryN(0).Coordinate.X;
                        result.Add(fromX);

                    }
                    else
                    {
                        result.Add(fromX);

                    }

                    //for (double x = result[0]; x < toX; x += xStep)
                    //{
                    //    if(x> result[0])
                    //    {
                    //        result.Add(x);
                    //    }

                    //}

                    result.Add(toX);

                }

                else
                {
                    continue;
                }
            }
            if (xbound[0] > xbound[1])
            {
                double boundF = xbound[1];
                xbound[1] = xbound[0];
                xbound[0] = boundF;

            }

            for (int i = 0; i < sectionLineList.Count; i++)
            {
                StratigraphicAttribute_Polyline structLineF = sectionLineList[i];
                string linetype = structLineF.lineType;
                if (linetype == "地表线")
                {
                    LineString groundLine = structLineF.Line;
                    //按斜率简化折点
                    //IPointCollection newVertexs = simplifyVertexBySlope(groundPoints, 0.05);

                    //按direction将点的direction方向的值排序，并剔除在范围外的点
                    List<Point> groundPoints = convertGeometriesHelper.ConvertLineToPoints(groundLine);
                    result.AddRange(geoSortHelper.BubbleSortCollectionByDirection(groundPoints, xbound[0], xbound[1], -5000, 7000, "X"));
                }
            }

            /*for (double x = xbound[0]; x < xbound[1]; x += xMaxStep)
            {
                result.Add(x);
            }*/
            //result =GeoSortHelperLib.BubbleSortDoubleList(result);
            result.Sort();


            return result;
        }

        /// <summary>
        /// 使list里数字间隔大于xStep
        /// </summary>
        /// <param name="xOriginList"></param>
        /// <param name="xStep"></param>
        /// <returns></returns>
        private List<double> MakeStepMoreThan1(List<double> xOriginList, double xStep)
        {
            List<double> result = new List<double>();
            result.Add(xOriginList[0]);
            for (int i = 1; i < xOriginList.Count; i++)
            {
                int n = result.Count;
                if (xOriginList[i] - result[n - 1] >= xStep)
                {
                    result.Add(xOriginList[i]);
                }
            }
            return result;
        }

        ///<summary>
        /// 计算点所在的地层
        /// </summary>
        /// <param name="pPoint"></param>
        /// <param name="polygonLayer"></param>
        /// <returns></returns>
        private Feature GetPolygonFeature(IPoint pPoint, FeatureCollection gfc)
        {

            Feature result = null;

            Coordinate centerCoordinate = new Coordinate(pPoint.X, pPoint.Y);

            GeometryFactory geometryFactory = new GeometryFactory();
            Geometry queryGeometry = geometryFactory.CreatePoint(centerCoordinate) as Geometry;
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


            if (results.Count != 0)
            {
                foreach (Feature r in results)
                {
                    result = r;
                }

            }
            return result;



        }
        /// <summary>
        /// 合并所有钻孔
        /// </summary>
        /// <param name="folderPath"></param>
        public FeatureCollection MergeShapefilPoint(string folderPath, string workPath)
        {
            IGeometryFactory gfactory = GeometryFactory.Floating;
            FeatureCollection mergeDrills = new FeatureCollection();
            ShapeFileHelper shapeFileHelper=new ShapeFileHelper(folderPath);
            List<string> pointShpList = shapeFileHelper. GetAllSectionNamesFromFolder(folderPath, ".shp");
            int id = 0;
            foreach (var inputPoints in pointShpList)
            {
                var dataReader = new ShapefileDataReader(workPath + "\\" + inputPoints + "Point.shp", gfactory);

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

                        feature.Attributes.AddAttribute(keys[i], val);
                    }
                    feature.Attributes.DeleteAttribute("id");
                    feature.Attributes.AddAttribute("id", id);

                    id++;
                    mergeDrills.Add(feature);
                }

            }
            string filePath = workPath + "\\allPointsMerge.shp";
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(mergeDrills[0], mergeDrills.Count);
            dataWriter.Write(mergeDrills.Features);
            return mergeDrills;
        }
        /// <summary>
        /// 重设xy
        /// </summary>
        /// <param name="mergePoints"></param>
        public FeatureCollection resetxyz(FeatureCollection mergePoints, string workPath)
        {
            IGeometryFactory gfactory = GeometryFactory.Floating;
            FeatureCollection resetDrills = new FeatureCollection();
            for (int i = 0; i < mergePoints.Count; i++)
            {
                var attributes = new AttributesTable();
                var newPoint = gfactory.CreatePoint(new Coordinate(Convert.ToDouble(mergePoints[i].Attributes["x"].ToString()), Convert.ToDouble(mergePoints[i].Attributes["y"].ToString())));
                //var newPoint = gfactory.CreatePoint(new Coordinate(Convert.ToDouble(mergePoints[i].Geometry.Coordinate.X), Convert.ToDouble(mergePoints[i].Attributes["y"].ToString())));
                var neeGeometry = gfactory.CreateGeometry(newPoint);
                Feature feature = new Feature(neeGeometry, attributes);
                foreach (var oneattribute in mergePoints[i].Attributes.GetNames())
                {
                    attributes.AddAttribute(oneattribute, mergePoints[i].Attributes[oneattribute]);
                }
                resetDrills.Add(feature);
            }
            string filePath = workPath + "\\resetxyz.shp";
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(resetDrills[0], resetDrills.Count);
            dataWriter.Write(resetDrills.Features);
            return resetDrills;
        }
        /// <summary>
        /// 对点根据xyz进行排序
        /// </summary>
        /// <param name="mergePoints"></param>
        public FeatureCollection SortPoints(FeatureCollection mergePoints)
        {
            List<IFeature> listPoints = new List<IFeature>();
            for (int i = 0; i < mergePoints.Count; i++)
            {
                listPoints.Add(mergePoints[i]);
            }
            //设置一个条件进行排序
            Comparison<IFeature> comparison = new Comparison<IFeature>(SortByvalue);//将方法传给委托实例
            listPoints.Sort(comparison);
            FeatureCollection sortPoints = new FeatureCollection();
            for (int i = 0; i < listPoints.Count; i++)
            {
                sortPoints.Add(listPoints[i]);
            }
            return sortPoints;
        }
        //排序条件(按值)
        static int SortByvalue(IFeature x, IFeature y)
        {
            double xx = (double)x.Attributes["x"];
            double xy = (double)x.Attributes["y"];
            double xh = (double)x.Attributes["h"];
            double yx = (double)y.Attributes["x"];
            double yy = (double)y.Attributes["y"];
            double yh = (double)y.Attributes["h"];
            if (xx != yx)
            {
                return xx.CompareTo(yx);
            }
            else if (xy != yy)
            {
                return xy.CompareTo(yy);
            }
            else
            {
                return xh.CompareTo(yh);
            }

        }
        /// <summary>
        /// 将地层属性赋予固定编号
        /// </summary>
        /// <param name="resetDrills"></param>
        /// <returns></returns>
        public FeatureCollection convertDSOtoValue(string soildtypepath, FeatureCollection resetDrills, string workPath)
        {
            var Dataset = ReadExcelToDataSet(soildtypepath);
            for (int i = 0; i < resetDrills.Count; i++)
            {
                for (int j = 0; j < Dataset.Tables[0].Rows.Count; j++)
                {
                    var dso = resetDrills[i].Attributes["DSN"];
                    var dsovalue = Dataset.Tables[0].Rows[j][0];

                    if (dso.ToString() == dsovalue.ToString())
                    {
                        resetDrills[i].Attributes["soildType"] = Dataset.Tables[0].Rows[j][2].ToString();
                    }
                }
            }
            //针对组合情况特殊处理
            for (int i = 0; i < resetDrills.Count; i++)
            {
                string updso = "";
                string downdso = "";

                if (resetDrills[i].Attributes["soildType"].ToString() == "10086")
                {
                    var dso = resetDrills[i].Attributes["DSO"].ToString();
                    var dsolist = dso.Split('_');
                    for (int j = 0; j < Dataset.Tables[0].Rows.Count; j++)
                    {
                        var dsovalue = Dataset.Tables[0].Rows[j][1];
                        if (dsolist[0] == dsovalue.ToString())
                        {
                            updso = Dataset.Tables[0].Rows[j][2].ToString();
                        }
                    }
                    for (int j = 0; j < Dataset.Tables[0].Rows.Count; j++)
                    {
                        var dsovalue = Dataset.Tables[0].Rows[j][1];
                         if (dsolist[1] == dsovalue.ToString())
                        {
                            downdso = Dataset.Tables[0].Rows[j][2].ToString();
                        }
                    }
                    resetDrills[i].Attributes["soildType"] = updso + "_" + downdso;
                }
            }
            string filePath = workPath + "\\soildtypeDrills.shp";
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(resetDrills[0], resetDrills.Count);
            dataWriter.Write(resetDrills.Features);
            return resetDrills;

            //返回新的所有钻孔点
        }
        /// <summary>
        /// 读取Excel表格内容
        /// </summary>
        /// <param name="fileNmaePath"></param>
        /// <returns></returns>
        private DataSet ReadExcelToDataSet(string fileNmaePath)
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
        /// <summary>
        /// 提取地表点，并重新设行列值
        /// </summary>
        /// <param name="solidDrills"></param>
        /// <returns></returns>
        public FeatureCollection getprofileDrills(FeatureCollection solidDrills, double redoy, string workPath)
        {
            //提取地表点
            FeatureCollection profileDrills = new FeatureCollection();
            for (int i = 0; i < solidDrills.Count; i++)
            {
                if (solidDrills[i].Attributes["LineType"].ToString() == "地表线")
                {
                    profileDrills.Add(solidDrills[i]);
                }

            }
            //对所有钻孔点根据xy值进行提取，并去重排序
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            Dictionary<double, int> newx = new Dictionary<double, int>();
            Dictionary<double, int> newy = new Dictionary<double, int>();
            for (int i = 0; i < profileDrills.Count; i++)
            {
                x.Add(Convert.ToDouble(profileDrills[i].Attributes["x"]));
                y.Add(Convert.ToDouble(profileDrills[i].Attributes["y"]));
            }
            x.Distinct().ToList().Sort();
            y.Distinct().ToList().Sort();
            int ii = 1;
            for (int i = 0; i < x.Count; i++)
            {

                try
                {
                    newx.Add(x[i], ii);
                    ii++;
                }
                catch
                {
                    continue;
                }

            }
            int jj = 1;
            for (int j = y.Count - 1; j >= 0; j--)
            {
                try
                {
                    newy.Add(y[j], jj);
                    jj++;
                }
                catch
                {
                    continue;
                }
            }
            for (int i = 0; i < profileDrills.Count; i++)
            {
                foreach (var m in newx)
                {

                    if (m.Key.ToString() == profileDrills[i].Attributes["x"].ToString())
                    {
                        profileDrills[i].Attributes.AddAttribute("col", m.Value);
                    }
                }
                foreach (var n in newy)
                {
                    if (n.Key.ToString() == profileDrills[i].Attributes["y"].ToString())
                    {
                        profileDrills[i].Attributes.AddAttribute("row", n.Value);
                    }
                }
            }
            //根据行列顺序对地表点设置序号.Attributes.AddAttribute("profileId", i);
            List<IFeature> prolist = new List<IFeature>();

            for (int i = 0; i < profileDrills.Count; i++)
            {
                prolist.Add(profileDrills[i]);
            }
            //设置行列号排序条件
            Comparison<IFeature> comparison = new Comparison<IFeature>(SortBycolraw);//将方法传给委托实例
            prolist.Sort(comparison);
            FeatureCollection newprofileDrills = new FeatureCollection();
            for (int i = 0; i < prolist.Count; i++)
            {
                newprofileDrills.Add(prolist[i]);
            }
            //添加profileid属性
            for (int i = 0; i < newprofileDrills.Count; i++)
            {
                newprofileDrills[i].Attributes.AddAttribute("profileId", i);
                newprofileDrills[i].Geometry.Coordinate.Y = Convert.ToDouble(newprofileDrills[i].Attributes["y"].ToString()) + redoy;
                newprofileDrills[i].Geometry.Coordinate.Z = Convert.ToDouble(newprofileDrills[i].Attributes["h"].ToString());
            }
            string filePath = workPath + "\\profileDrills.shp";
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(newprofileDrills[0], newprofileDrills.Count);
            dataWriter.Write(newprofileDrills.Features);
            return newprofileDrills;

        }
        //排序条件：按照行列号
        static int SortBycolraw(IFeature x, IFeature y)
        {
            if (Convert.ToInt32(x.Attributes["row"]) != (Convert.ToInt32(y.Attributes["row"])))
            {
                return Convert.ToInt32(x.Attributes["row"]).CompareTo(Convert.ToInt32(y.Attributes["row"]));
            }
            else
            {
                return Convert.ToInt32(x.Attributes["col"]).CompareTo(Convert.ToInt32(y.Attributes["col"]));
            }


        }
        /// <summary>
        /// 将所有钻孔点与地表点连接(131370实验)
        /// </summary>
        /// <param name="profileDrills"></param>
        /// <param name="solidDrills"></param>
        /// <returns></returns>
        public  FeatureCollection connectingDrills(FeatureCollection profileDrills, FeatureCollection solidDrills, double redoy, string workPath)
        {
            for (int i = 0; i < solidDrills.Count; i++)
            {
                if (solidDrills[i].Attributes["LineType"].ToString() == "地表线")
                { continue; }
                var stop = 1;

                for (int j = 0; j < profileDrills.Count; j++)
                {

                    if (stop == 0) { continue; }
                    if ((profileDrills[j].Attributes["x"].ToString() == solidDrills[i].Attributes["x"].ToString()) &&
                        (profileDrills[j].Attributes["y"].ToString() == solidDrills[i].Attributes["y"].ToString()))
                    {
                        solidDrills[i].Attributes.AddAttribute("profileId", profileDrills[j].Attributes["profileId"]);

                        stop = 0;
                    }
                }
            }
            for (int i = 0; i < solidDrills.Count; i++)
            {
                var drilly = solidDrills[i].Attributes["y"].ToString();
                solidDrills[i].Attributes["y"] = Convert.ToDouble(drilly) + redoy;
                solidDrills[i].Geometry.Coordinate.Y = Convert.ToDouble(drilly) + redoy;
                solidDrills[i].Geometry.Coordinate.Z = Convert.ToDouble(solidDrills[i].Attributes["h"].ToString());
            }

            string filePath = workPath + "\\connectDrills.shp";
            IGeometryFactory geometryFactory = GeometryFactory.Floating;
            var dataWriter = new ShapefileDataWriter(filePath, geometryFactory, Encoding.Default);//设置Encoding编码方式
            dataWriter.Header = ShapefileDataWriter.GetHeader(solidDrills[0], solidDrills.Count);
            dataWriter.Write(solidDrills.Features);
            return solidDrills;
        }
    }
}
