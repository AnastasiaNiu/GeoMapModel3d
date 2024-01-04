using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Triangulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CommonDataStructureLib
{
    /// <summary>
    /// 三角形顶点模型
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// 顶点的id
        /// </summary>
        public int id;

        /// <summary>
        /// 顶点的X坐标
        /// </summary>
        public double x;

        /// <summary>
        /// 顶点的Y坐标
        /// </summary>
        public double y;

        /// <summary>
        /// 顶点的Z坐标
        /// </summary>
        public double z;
    }
    /// <summary>
    /// 三角形模型
    /// </summary>
    public class TriMesh
    {
        public TriMesh()
        {
            vertexList = new List<Vertex>();

            triangleList = new List<Triangle>();
        }

        /// <summary>
        /// 三角网顶点列表
        /// </summary>
        public IList<Vertex> vertexList;

        /// <summary>
        /// 三角网三角形列表
        /// </summary>
        public IList<Triangle> triangleList;

        /// <summary>
        /// 添加顶点
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public int AddVertex(double x, double y, double z)
        {
            //注意：每次添加顶点时要保证是新的节点

            Vertex geoVertex = new Vertex();

            geoVertex.id = vertexList.Count;
            geoVertex.x = Math.Round(x, 3);//保留两位小数
            geoVertex.y = Math.Round(y, 3);
            geoVertex.z = Math.Round(z, 3);

            foreach (var vertex in vertexList)
            {
                if (Math.Abs(vertex.x - geoVertex.x) < 0.01 &&
                    Math.Abs(vertex.y - geoVertex.y) < 0.01 &&
                    Math.Abs(vertex.z - geoVertex.z) < 0.01)
                {
                    return vertex.id;
                }
            }

            vertexList.Add(geoVertex);

            return geoVertex.id;
        }
        public int AddVertex(double x, double y, double z, int i)
        {
            //注意：每次添加顶点时要保证是新的节点

            Vertex geoVertex = new Vertex();
            geoVertex.id = vertexList.Count;
            geoVertex.x = Math.Round(x, 3);//保留两位小数
            geoVertex.y = Math.Round(y, 3);
            geoVertex.z = Math.Round(z, 3);



            vertexList.Add(geoVertex);

            return geoVertex.id;
        }
        /// <summary>
        /// 向三角网模型中添加一个三角形--形式一
        /// </summary>
        /// <param name="id"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public void AddTriangle(int id, int v0, int v1, int v2)
        {
            Triangle geoTri = new Triangle();

            geoTri.id = id;
            geoTri.v0 = v0;
            geoTri.v1 = v1;
            geoTri.v2 = v2;

            triangleList.Add(geoTri);
        }

        /// <summary>
        /// 向三角网模型中添加一个三角形--形式二
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="z0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="z2"></param>
        public void AddTriangle(double x0, double y0, double z0,
            double x1, double y1, double z1,
            double x2, double y2, double z2)
        {
            int gv0 = AddVertex(x0, y0, z0);
            int gv1 = AddVertex(x1, y1, z1);
            int gv2 = AddVertex(x2, y2, z2);

            Triangle geoTri = new Triangle();
            geoTri.id = triangleList.Count;
            geoTri.v0 = gv0;
            geoTri.v1 = gv1;
            geoTri.v2 = gv2;
            triangleList.Add(geoTri);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="area"></param>
        /// <param name="pointpath"></param>
        /// <param name="savepath"></param>
        /// <returns></returns>
        public List<TriMesh> createTriMesh(FeatureCollection area, FeatureCollection  points, string savepath)
        {

            //将点要素转为coordinateArray
            //这里是将所有点都生成，但由于数据量太大，所以分批生成mesh数据
            List<TriMesh> meshs = new List<TriMesh>();
            //Coordinate[] coordinateArray = new Coordinate[points.Count];
            //获得有多少col
            List<int> cols = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                cols.Add(Convert.ToInt32(points[i].Attributes["col"].ToString()));
            }
            cols = cols.Distinct().ToList();
            //单列输出（将每个要素与地层线相交，得到相交地层线），到最后要素合并
            //List<Triangle> alltris = new List<Triangle>();
            //var num = Math.Ceiling(Convert.ToDouble(cols.Max() / 2.0));
            //for (int k = 1; k < cols.Max(); k++)
            //{


            //    List<Coordinate> coordinateArraylist = new List<Coordinate>();
            //    for (int i = 0; i < points.Count; i++)
            //    {
            //        if (Convert.ToInt32(points[i].Attributes["col"].ToString()) >= k && Convert.ToInt32(points[i].Attributes["col"].ToString()) <= (k + 1))
            //        {
            //            coordinateArraylist.Add(points[i].Geometry.Coordinate);
            //        }
            //    }


            //    if (coordinateArraylist.Count > 0)
            //    {
            //        //-------直接用NTS不做约束的三角网
            //        Coordinate[] coordinateArray = new Coordinate[coordinateArraylist.Count];
            //        for (int i = 0; i < coordinateArraylist.Count; i++)
            //        {
            //            coordinateArray[i] = coordinateArraylist[i];
            //        }
            //        //创建GeometryFactory实例
            //        GeometryFactory geoFactory0 = new GeometryFactory();
            //        //创建约束型三角化器
            //        ConformingDelaunayTriangulationBuilder builder = new ConformingDelaunayTriangulationBuilder();
            //        builder.SetSites(geoFactory0.CreateMultiPoint(coordinateArray));
            //        //这里NTS库中带约束的不管用
            //        //得到三角面
            //        var triangles0 = builder.GetTriangles(geoFactory0);
            //        //将多个三角面合并为一个面
            //        UnaryUnionOp unionOp = new UnaryUnionOp(triangles0.Union());
            //        var meshtri = unionOp.Union();
            //        var meshface = meshtri.Envelope;
            //        //对获得的面与地层线进行相交，并获得相交几何
            //        List<IGeometry> insectlines = new List<IGeometry>();
            //        for (int i = 0; i < area.Count; i++)
            //        {
            //            var insectline = meshface.Intersection(area[i].Geometry);
            //            if (insectline.IsEmpty == true)
            //            {
            //                continue;
            //            }
            //            else if (insectline.NumGeometries > 0)
            //            {
            //                for (int j = 0; j < insectline.NumGeometries; j++)
            //                {
            //                    var oneinsectline = insectline.GetGeometryN(j);
            //                    insectlines.Add(oneinsectline);
            //                }
            //            }
            //            else
            //            {
            //                insectlines.Add(insectline);
            //            }


            //        }
            //        insectlines = insectlines.Distinct().ToList();
            //        //交线端点就是虚拟钻孔点,,不对，，，在最开始最地层线做了简化
            //        //将线段作为约束线,将最两边端点直接填入即可
            //        //List<Points> rightinpointlist = new List<Points>();
            //        //for(int i=0;i < insectlines.Count; i++)
            //        //{
            //        //    rightinpointlist.Add(new Points(insectlines[i].Coordinates[0].X, insectlines[i].Coordinates[0].Y,0));
            //        //    rightinpointlist.Add(new Points(insectlines[i].Coordinates[1].X, insectlines[i].Coordinates[1].Y, 0));
            //        //}
            //        //必须这样：现生成的虚拟点不在地层线上，so针对相交几何线，找到与其最临近的点，以此为两端点作为约束线
            //        //所有内边界约束点集列表
            //        List<Points> inpointlist = new List<Points>();
            //        IList<IList<Points>> inpointlists = new List<IList<Points>>();
            //        for (int i = 0; i < insectlines.Count; i++)
            //        {
            //            List<Points> oneinpointlist = new List<Points>();
            //            for (int j = 0; j < coordinateArraylist.Count; j++)
            //            {
            //                if (Math.Abs(insectlines[i].Coordinates[0].Distance(coordinateArraylist[j])) < 1)
            //                {
            //                    oneinpointlist.Add(new Points(coordinateArraylist[j].X, coordinateArraylist[j].Y, 0));
            //                    break;
            //                }

            //            }
            //            for (int j = 0; j < coordinateArraylist.Count; j++)
            //            {
            //                if (Math.Abs(insectlines[i].Coordinates[insectlines[i].Coordinates.Count() - 1].Distance(coordinateArraylist[j])) < 1)
            //                {
            //                    oneinpointlist.Add(new Points(coordinateArraylist[j].X, coordinateArraylist[j].Y, 0));
            //                    break;
            //                }

            //            }
            //            if (oneinpointlist.Count > 1)
            //            {
            //                inpointlists.Add(oneinpointlist);
            //                inpointlist.Add((Points)oneinpointlist[0]);
            //                inpointlist.Add((Points)oneinpointlist[1]);
            //            }

            //        }


            //        //参与构成三角网的所有点
            //        List<Points> pointlist = new List<Points>();
            //        List<Points> linepointlist = new List<Points>();
            //        for (int i = 0; i < coordinateArraylist.Count; i++)
            //        {
            //            Points one = new Points();
            //            one.x = coordinateArraylist[i].X;
            //            one.y = coordinateArraylist[i].Y;
            //            one.z = coordinateArraylist[i].Z;
            //            pointlist.Add(one);
            //        }
            //        IDelaunay dotri = DelaunayEntry.GetDelaunay();
            //        List<RockTriMeshLib.Triangle> tris = new List<RockTriMeshLib.Triangle>();
            //        //创建带约束的三角网
            //        tris = dotri.buildDelaunay(pointlist, linepointlist, inpointlist, inpointlists);
            //        //将单列三角网填入大三角网
            //        foreach (var one in tris)
            //        {
            //            alltris.Add(one);
            //        }
            //    }
            //    //将mesh按51列分开，即k是否是五十的倍数或者最后一个
            //    if (k != 0)
            //    {
            //        if (k % 50 == 0 || k == cols.Max() - 1)
            //        {
            //            GeometryFactory geoFactory = new GeometryFactory();
            //            IGeometry[] plyss = new IGeometry[alltris.Count];
            //            for (int l = 0; l < alltris.Count; l++)
            //            {
            //                var onetri = geoFactory.CreatePolygon(new Coordinate[4]
            //                { new Coordinate(alltris[l].p1.x,alltris[l].p1.y,alltris[l].p1.z),
            //            new Coordinate(alltris[l].p2.x,alltris[l].p2.y,alltris[l].p2.z),
            //            new Coordinate(alltris[l].p3.x,alltris[l].p3.y,alltris[l].p3.z),
            //        new Coordinate(alltris[l].p1.x,alltris[l].p1.y,alltris[l].p1.z)});
            //                var one = geoFactory.CreateGeometry(onetri);
            //                plyss[l] = onetri;
            //            }
            //            var triangles = geoFactory.CreateGeometryCollection(plyss);
            //            //Coordinate[] coordinateArray = new Coordinate[coordinateArraylist.Count];
            //            //for (int i = 0; i < coordinateArraylist.Count; i++)
            //            //{
            //            //    coordinateArray[i] = coordinateArraylist[i];
            //            //}
            //            ////创建GeometryFactory实例
            //            //GeometryFactory geoFactory = new GeometryFactory();
            //            ////创建约束型三角化器
            //            //ConformingDelaunayTriangulationBuilder builder = new ConformingDelaunayTriangulationBuilder();
            //            //builder.SetSites(geoFactory.CreateMultiPoint(coordinateArray));
            //            ////这里NTS库中带约束的不管用
            //            ////得到三角面
            //            //var triangles = builder.GetTriangles(geoFactory);

            //            //创建基于newtin的要素并放入要素列表
            //            FeatureCollection features = new FeatureCollection();
            //            var onefeature = new NetTopologySuite.Features.Feature();
            //            Polygon[] plys = new Polygon[triangles.Count];
            //            for (int i = 0; i < triangles.Count; i++)
            //            {
            //                // 创建带有属性字段的要素
            //                // 创建属性字段集合
            //                AttributesTable attributes = new AttributesTable();
            //                var feature = new NetTopologySuite.Features.Feature(triangles.Geometries[i], attributes);
            //                features.Add(feature);
            //                onefeature = feature;
            //            }

            //            //指定保存的shp路径
            //            var shapefilepath = savepath + "\\meshnew" + k + ".shp";

            //            // 创建ShapefileDataWriter实例，指定保存路径和GeometryFactory
            //            var dataWriter = new ShapefileDataWriter(shapefilepath, new GeometryFactory());

            //            // 设置Shapefile的头部信息
            //            dataWriter.Header = ShapefileDataWriter.GetHeader(onefeature, features.Count);

            //            // 将面要素集合写入Shapefile         
            //            dataWriter.Write(features.Features);
            //            TriMesh mesh = new TriMesh();
            //            for (int i = 0; i < features.Count; i++)
            //            {
            //                mesh.AddTriangle(features[i].Geometry.Coordinates[0].X, features[i].Geometry.Coordinates[0].Y, features[i].Geometry.Coordinates[0].Z,
            //                    features[i].Geometry.Coordinates[1].X, features[i].Geometry.Coordinates[1].Y, features[i].Geometry.Coordinates[1].Z,
            //                    features[i].Geometry.Coordinates[2].X, features[i].Geometry.Coordinates[2].Y, features[i].Geometry.Coordinates[2].Z);
            //            }
            //            meshs.Add(mesh);
            //            alltris.Clear();

            //        }
            //    }

            //}
            //按照每51个为一个集合，分成集合列表List-coordinateArrays
            var num = Math.Ceiling(Convert.ToDouble(cols.Max() / 50.0));
            for (int k = 0; k < num; k++)
            {
                List<Coordinate> coordinateArraylist = new List<Coordinate>();
                for (int i = 0; i < points.Count; i++)
                {
                    if (Convert.ToInt32(points[i].Attributes["col"].ToString()) >= k * 50 && Convert.ToInt32(points[i].Attributes["col"].ToString()) <= (k + 1) * 50)
                    {
                        coordinateArraylist.Add(points[i].Geometry.Coordinate);
                    }
                }
                if (coordinateArraylist.Count > 0)
                {
                    //参与构成三角网的所有点
                    List<Points> pointlist = new List<Points>();
                    List<Points> linepointlist = new List<Points>();
                    for (int i = 0; i < coordinateArraylist.Count; i++)
                    {
                        Points onepoint = new Points();
                        onepoint.x = coordinateArraylist[i].X;
                        onepoint.y = coordinateArraylist[i].Y;
                        onepoint.z = coordinateArraylist[i].Z;
                        pointlist.Add(onepoint);

                    }

                    Coordinate[] coordinateArray = new Coordinate[coordinateArraylist.Count];
                    for (int i = 0; i < coordinateArraylist.Count; i++)
                    {
                        coordinateArray[i] = coordinateArraylist[i];
                    }
                    //创建GeometryFactory实例
                    GeometryFactory geoFactory = new GeometryFactory();
                    //创建约束型三角化器
                    ConformingDelaunayTriangulationBuilder builder = new ConformingDelaunayTriangulationBuilder();
                    builder.SetSites(geoFactory.CreateMultiPoint(coordinateArray));
                    //这里NTS库中带约束的不管用
                    //得到三角面
                    var triangles = builder.GetTriangles(geoFactory);



                    //创建基于newtin的要素并放入要素列表
                    FeatureCollection features = new FeatureCollection();
                    var onefeature = new NetTopologySuite.Features.Feature();
                    Polygon[] plys = new Polygon[triangles.Count];
                    for (int i = 0; i < triangles.Count; i++)
                    {
                        // 创建带有属性字段的要素
                        // 创建属性字段集合
                        AttributesTable attributes = new AttributesTable();
                        var feature = new NetTopologySuite.Features.Feature(triangles.Geometries[i], attributes);
                        //判断三角形中点是否在polygon内部，若不在则不添加
                        //if (area == null)
                        //{
                        features.Add(feature);
                        onefeature = feature;
                        //}
                        //else
                        //{
                        //    if (!feature.Geometry.InteriorPoint.Intersects(area[0].Geometry)) continue;
                        //    features.Add(feature);
                        //    onefeature = feature;
                        //}


                    }


                    //指定保存的shp路径
                    var shapefilepath = savepath + "\\mesh" + k + ".shp";

                    // 创建ShapefileDataWriter实例，指定保存路径和GeometryFactory
                    var dataWriter = new ShapefileDataWriter(shapefilepath, new GeometryFactory());

                    // 设置Shapefile的头部信息
                    dataWriter.Header = ShapefileDataWriter.GetHeader(onefeature, features.Count);

                    // 将面要素集合写入Shapefile         
                    dataWriter.Write(features.Features);
                    TriMesh mesh = new TriMesh();
                    for (int i = 0; i < features.Count; i++)
                    {
                        mesh.AddTriangle(features[i].Geometry.Coordinates[0].X, features[i].Geometry.Coordinates[0].Y, features[i].Geometry.Coordinates[0].Z,
                            features[i].Geometry.Coordinates[1].X, features[i].Geometry.Coordinates[1].Y, features[i].Geometry.Coordinates[1].Z,
                            features[i].Geometry.Coordinates[2].X, features[i].Geometry.Coordinates[2].Y, features[i].Geometry.Coordinates[2].Z);


                    }
                    meshs.Add(mesh);
                }


            }


            return meshs;
        }
       /// <summary>
       /// 由ID寻找点
       /// </summary>
       /// <param name="ID"></param>
       /// <returns></returns>
        public Vertex findVertexByID(int ID)
        {
            for (int i = 0; i < vertexList.Count; i++)
            {
                if (vertexList[i].id == ID)
                {
                    return vertexList[i];
                }
            }
            return null;

        }
    }
    public class Points
    {
        /// <summary>
        /// x坐标
        /// </summary>
        public double x;
        /// <summary>
        /// y坐标
        /// </summary>
        public double y;
        /// <summary>
        /// z坐标
        /// </summary>
        public double z;

        public double v { get; set; }
    }
    public class Triangle
    {
        /// <summary>
        /// 三角形ID
        /// </summary>
        public int id;
        /// <summary>
        /// 顶点0ID
        /// </summary>
        public int v0;

        /// <summary>
        /// 顶点1ID
        /// </summary>
        public int v1;

        /// <summary>
        /// 顶点2ID
        /// </summary>
        public int v2;

        public Points p1;
        public Points p2;
        public Points p3;
        /// <summary>
        /// 构造函数
        /// </summary>

        public Triangle()
        {

        }

    }

}
