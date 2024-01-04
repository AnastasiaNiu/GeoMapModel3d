using CommonDataStructureLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CommonMethodHelpLib
{
    public class BREPModelHelper
    {
        /// <summary>
        /// 三角网
        /// </summary>
        private TriMesh _triMesh;

        /// <summary>
        /// 钻孔数据管理类
        /// </summary>
        private DrillProcessHelper _drillProcessor;

        /// <summary>
        /// 边界缝合点列表
        /// </summary>
        private IList<DrillModel> _boundaryDispersPoints;

        /// <summary>
        /// Brep模型列表
        /// </summary>
        private IList<BrepModel> _brepList;

        double maxm = 0;
        double maxmx = 0;
        double maxmy = 0;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="triMesh"></param>
        /// <param name="drillProcessor"></param>
        /// <param name="boundaryDispersPoints"></param>
        public BREPModelHelper()
        {
            _brepList = new List<BrepModel>();
        }

        /// <summary>
        /// 基于钻孔：构建边界模型
        /// </summary>
        /// <param name="_triMesh">三角网格</param>
        /// <param name="_drillProcessor">钻孔处理</param>
        /// <param name="_boundaryDispersPoints">边界离散点</param>
        public void build(TriMesh _triMesh, DrillProcessHelper _drillProcessor, IList<DrillModel> _pastboundaryDispersPoints, IList<IList<DrillModel>> _inDispersPointslists)
        {

            //初始化每层的BrepModel
            for (int i = 0; i < _drillProcessor.LithList.Count; i++)
            {
                this._brepList.Add(new BrepModel());
            }

            //////////////////////////////////////////////////////////////////////////

            double detZZoom = 0;
            // 计算上下地层面的三角形
            foreach (Triangle tri in _triMesh.triangleList)
            {
                Vertex tv0 = _triMesh.vertexList[tri.v0];
                Vertex tv1 = _triMesh.vertexList[tri.v1];
                Vertex tv2 = _triMesh.vertexList[tri.v2];


                DrillModel d0 = _drillProcessor.RetriveDrillByCoord(tv0.x, tv0.y);
                DrillModel d1 = _drillProcessor.RetriveDrillByCoord(tv1.x, tv1.y);
                DrillModel d2 = _drillProcessor.RetriveDrillByCoord(tv2.x, tv2.y);

                if (d0 == null || d1 == null || d2 == null)
                {
                    //continue;
                    throw new Exception("找不到对应的钻孔");
                }

                for (int i = 0; i < d0.StratumList.Count; i++)
                {
                    BrepModel brep = _brepList[i] as BrepModel;

                    StratumModel d0S = d0.StratumList[i] as StratumModel;
                    StratumModel d1S = d1.StratumList[i] as StratumModel;
                    StratumModel d2S = d2.StratumList[i] as StratumModel;

                    //处理地层缺失
                    // GTP高度为0
                    if (d0S.ZUp == d0S.ZDown && d1S.ZUp == d1S.ZDown && d2S.ZUp == d2S.ZDown)
                    {
                        continue;
                    }
                    //当对于厚度非常小时，也进行缺失处理
                    if (Math.Abs(d0S.ZUp - d0S.ZDown) < 0.11 && Math.Abs(d1S.ZUp - d1S.ZDown) < 0.11 && Math.Abs(d2S.ZUp - d2S.ZDown) < 0.11)
                    {
                        continue;
                    }
                    Vector3 vn1 = new Vector3((float)d0.X, (float)d0.Y, (float)Math.Round(d0S.ZUp - (detZZoom * i), 3));
                    Vector3 vn2 = new Vector3((float)d1.X, (float)d1.Y, (float)Math.Round(d1S.ZUp - (detZZoom * i), 3));
                    Vector3 vn3 = new Vector3((float)d2.X, (float)d2.Y, (float)Math.Round(d2S.ZUp - (detZZoom * i), 3));

                    Vector3 v1 = vn1 - vn2;
                    Vector3 v2 = vn2 - vn3;
                    Vector3 v3 = vn3 - vn1;
                    float cross1 = Vector3.Cross(v1, v2).Z;
                    float cross2 = Vector3.Cross(v2, v3).Z;
                    float cross3 = Vector3.Cross(v3, v1).Z;
                    if (cross1 * cross2 * cross3 >= 0)
                    {
                        brep.addTriangle(d0.X, d0.Y, Math.Round(d0S.ZUp - (detZZoom * i), 3),
                         d1.X, d1.Y, Math.Round(d1S.ZUp - (detZZoom * i), 3),
                         d2.X, d2.Y, Math.Round(d2S.ZUp - (detZZoom * i), 3));
                    }
                    else
                    {
                        brep.addTriangle(d0.X, d0.Y, Math.Round(d0S.ZUp - (detZZoom * i), 3),
                        d2.X, d2.Y, Math.Round(d2S.ZUp - (detZZoom * i), 3),
                        d1.X, d1.Y, Math.Round(d1S.ZUp - (detZZoom * i), 3));
                    }

                    Vector3 vm1 = new Vector3((float)d0.X, (float)d0.Y, (float)Math.Round(d0S.ZDown - (detZZoom * i), 3));
                    Vector3 vm2 = new Vector3((float)d1.X, (float)d1.Y, (float)Math.Round(d1S.ZDown - (detZZoom * i), 3));
                    Vector3 vm3 = new Vector3((float)d2.X, (float)d2.Y, (float)Math.Round(d2S.ZDown - (detZZoom * i), 3));

                    Vector3 vv1 = vm1 - vm2;
                    Vector3 vv2 = vm2 - vm3;
                    Vector3 vv3 = vm3 - vm1;
                    float crossv1 = Vector3.Cross(vv1, vv2).Z;
                    float crossv2 = Vector3.Cross(vv2, vv3).Z;
                    float crossv3 = Vector3.Cross(vv3, vv1).Z;
                    if (crossv1 * crossv2 * crossv3 >= 0)
                    {
                        brep.addTriangle(d0.X, d0.Y, Math.Round(d0S.ZDown - (detZZoom * (i + 1)), 3),
                        d1.X, d1.Y, Math.Round(d1S.ZDown - (detZZoom * (i + 1)), 3),
                        d2.X, d2.Y, Math.Round(d2S.ZDown - (detZZoom * (i + 1)), 3));
                    }
                    else
                    {
                        brep.addTriangle(d0.X, d0.Y, Math.Round(d0S.ZDown - (detZZoom * (i + 1)), 3),
                         d2.X, d2.Y, Math.Round(d2S.ZDown - (detZZoom * (i + 1)), 3),
                         d1.X, d1.Y, Math.Round(d1S.ZDown - (detZZoom * (i + 1)), 3));
                    }

                }
            }
            //这里有问题，由于进行了尖灭和细分边界点已经发生改变，所以要对新的边界点进行缝合
            //由于在出现约束的区域其细分部位发生变化，不再是简单的平分，所以需要重新搜索边界点
            //①处理外围边界:将外围钻孔找出
            List<DrillModel> drills = new List<DrillModel>();
            List<DrillModel> allbreppoints = new List<DrillModel>();
            _pastboundaryDispersPoints.Add(_pastboundaryDispersPoints[0]);
            foreach (KeyValuePair<string, DrillModel> keyValuePair in _drillProcessor.DrillList)
            {
                DrillModel drill = keyValuePair.Value;
                drills.Add(drill);
            }
            for (int i = 0; i < drills.Count; i++)
            {
                if (IsOnBoundary(drills[i], _pastboundaryDispersPoints))
                {
                    allbreppoints.Add(drills[i]);
                }
            }
            //直接对边界点取中值，这样点就顺利找到


            //②对外围钻孔进行排序---可能这一步就有问题

            _boundaryDispersPoints = SortPoints(allbreppoints);
            // 缝合外围多层DEM
            for (int i = 0; i < _drillProcessor.LithList.Count; i++)
            {
                BrepModel brep = _brepList[i];

                for (int j = 0; j < _pastboundaryDispersPoints.Count; j++)
                {
                    DrillModel p0;
                    DrillModel p1;
                    if (j != (_pastboundaryDispersPoints.Count - 1))
                    {
                        p0 = _pastboundaryDispersPoints[j];
                        p1 = _pastboundaryDispersPoints[j + 1];
                    }
                    else
                    {
                        p0 = _pastboundaryDispersPoints[j];
                        p1 = _pastboundaryDispersPoints[0];
                    }

                    DrillModel b0 = _drillProcessor.RetriveDrillByCoord(p0.X, p0.Y);
                    DrillModel b1 = _drillProcessor.RetriveDrillByCoord(p1.X, p1.Y);

                    StratumModel b0S = b0.StratumList[i];
                    StratumModel b1S = b1.StratumList[i];

                    //处理特殊情况:

                    if (b0S.ZUp == b0S.ZDown && b1S.ZUp == b1S.ZDown)
                    {
                        continue;
                    }

                    if (b0S.ZUp == b0S.ZDown)
                    {
                        brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                            b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3),
                            b1.X, b1.Y, Math.Round(b1S.ZUp - (detZZoom * i), 3));

                        continue;
                    }

                    if (b1S.ZUp == b1S.ZDown)
                    {
                        brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                            b0.X, b0.Y, Math.Round(b0S.ZDown - (detZZoom * (i + 1)), 3),
                            b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3));

                        continue;
                    }

                    //处理正常情况
                    brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                        b0.X, b0.Y, Math.Round(b0S.ZDown - (detZZoom * (i + 1)), 3),
                        b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3));

                    brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                        b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3),
                        b1.X, b1.Y, Math.Round(b1S.ZUp - (detZZoom * i), 3));
                }
            }
            if (_inDispersPointslists != null)
            {

                for (int k = 0; k < _inDispersPointslists.Count; k++)
                {
                    List<DrillModel> inbreppoints = new List<DrillModel>();
                    for (int i = 0; i < drills.Count; i++)
                    {
                        if (IsOnBoundary(drills[i], _inDispersPointslists[k]))
                        {
                            inbreppoints.Add(drills[i]);
                        }
                    }
                    List<DrillModel> inDispersPointslists = new List<DrillModel>();
                    //②对内部钻孔进行排序
                    inDispersPointslists = SortPoints(inbreppoints);
                    // 缝合内部孔隙多层DEM
                    for (int i = 0; i < _drillProcessor.LithList.Count; i++)
                    {
                        BrepModel brep = _brepList[i];

                        for (int j = 0; j < _inDispersPointslists[k].Count; j++)
                        {
                            DrillModel p0;
                            DrillModel p1;
                            if (j != (_inDispersPointslists[k].Count - 1))
                            {
                                p0 = _inDispersPointslists[k][j];
                                p1 = _inDispersPointslists[k][j + 1];
                            }
                            else
                            {
                                p0 = _inDispersPointslists[k][j];
                                p1 = _inDispersPointslists[k][0];
                            }

                            DrillModel b0 = _drillProcessor.RetriveDrillByCoord(p0.X, p0.Y);
                            DrillModel b1 = _drillProcessor.RetriveDrillByCoord(p1.X, p1.Y);
                            if (b0 == null || b1 == null)
                            {
                                throw new Exception("找不到对应的钻孔");
                            }
                            StratumModel b0S = b0.StratumList[i];
                            StratumModel b1S = b1.StratumList[i];

                            //处理特殊情况:

                            if (b0S.ZUp == b0S.ZDown && b1S.ZUp == b1S.ZDown)
                            {
                                continue;
                            }

                            if (b0S.ZUp == b0S.ZDown)
                            {
                                brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                                    b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3),
                                    b1.X, b1.Y, Math.Round(b1S.ZUp - (detZZoom * i), 3));

                                continue;
                            }

                            if (b1S.ZUp == b1S.ZDown)
                            {
                                brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                                    b0.X, b0.Y, Math.Round(b0S.ZDown - (detZZoom * (i + 1)), 3),
                                    b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3));

                                continue;
                            }

                            //处理正常情况
                            brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                                b0.X, b0.Y, Math.Round(b0S.ZDown - (detZZoom * (i + 1)), 3),
                                b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3));

                            brep.addTriangle(b0.X, b0.Y, Math.Round(b0S.ZUp - (detZZoom * i), 3),
                                b1.X, b1.Y, Math.Round(b1S.ZDown - (detZZoom * (i + 1)), 3),
                                b1.X, b1.Y, Math.Round(b1S.ZUp - (detZZoom * i), 3));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 基于钻孔：输出地层边界包围盒
        /// </summary>
        /// <param name="fileDirectory">文件存储路径</param>
        /// <param name="_drillProcessor">钻孔</param>
        /// <param name="count">图层序列</param>
        public void ExportBrepModels(string fileDirectory, DrillProcessHelper _drillProcessor, int count)
        {
            //输出配置文件
            string configFileName = fileDirectory + "//" + count + "\\LithLists" + count + ".txt";
            StreamWriter configWriter = new StreamWriter(configFileName);
            configWriter.WriteLine(_drillProcessor.LithList.Count.ToString());
                            
            string fileName = fileDirectory + "//" + count + "\\brepsLayer" + count + "-";
            List<Dictionary<int, int>> brepsis = new List<Dictionary<int, int>>();
            

            for (int i = 0; i < _drillProcessor.LithList.Count; i++)
            {

                string mathinterphelpFileName;
                BrepModel brep = _brepList[i];
                if (brep.triangleList.Count > 1)
                {
                    try
                    {
                        mathinterphelpFileName = fileName + _drillProcessor.LithList[i].lithName + ".obj";
                        configWriter.WriteLine(_drillProcessor.LithList[i].lithName);
                    }
                    catch
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }


                brep.exportobj(mathinterphelpFileName);

            }

            configWriter.Close();
        }
        /// <summary>
        /// 基于钻孔：炸开输出地层包围盒
        /// </summary>
        /// <param name="fileDirectory">保存路径</param>
        /// <param name="_drillProcessor">钻孔</param>
        /// <param name="count">图层序列</param>
        public void eExportBrepModels(string fileDirectory, DrillProcessHelper _drillProcessor, int count)
        {
            //输出配置文件
            string configFileName = fileDirectory + count + "\\config.txt";
            StreamWriter configWriter = new StreamWriter(configFileName);
            configWriter.WriteLine(_drillProcessor.LithList.Count.ToString());

            string fileName = fileDirectory + count + "\\brepsLayer" + count + "-";

            for (int i = 0; i < _drillProcessor.LithList.Count; i++)
            {
                BrepModel brep = _brepList[i];
                if (brep.triangleList.Count > 1)
                {
                    string mathinterphelpFileName;


                    mathinterphelpFileName = fileName + _drillProcessor.LithList[i + 1].lithId + ".obj";



                    brep.export2obj(mathinterphelpFileName, i + 1);

                }
            }

            configWriter.Close();
        }
        //判断点是否在边界上
        public static bool IsOnBoundary(DrillModel point, IList<DrillModel> polygon)
        {

            for (int i = polygon.Count - 1; i > 0; i--)
            {
                DrillModel p1 = polygon[i];
                DrillModel p2 = polygon[i - 1];

                NetTopologySuite.Geometries.GeometryFactory factory = new NetTopologySuite.Geometries.GeometryFactory();
                var pointgeo = factory.CreatePoint(new GeoAPI.Geometries.Coordinate(point.X, point.Y));

                GeoAPI.Geometries.Coordinate[] coordinates = { new GeoAPI.Geometries.Coordinate(p1.X, p1.Y), new GeoAPI.Geometries.Coordinate(p2.X, p2.Y) };
                var linegeo = factory.CreateLineString(coordinates);
                //oneline.Geometry = linegeo;
                if (linegeo.Distance(pointgeo.Buffer(0.5)) < 1)
                {
                    // return true;
                }
                if (IsOnLine(point, p1, p2, 1))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool IsOnLine(DrillModel point, DrillModel p1, DrillModel p2, double threshold)
        {
            double px = Math.Round(point.X, 3);
            double py = Math.Round(point.Y, 3);
            double p1x = Math.Round(p1.X, 3);
            double p1y = Math.Round(p1.Y, 3);
            double p2x = Math.Round(p2.X, 3);
            double p2y = Math.Round(p2.Y, 3);
            double pp1distance = Math.Sqrt(Math.Abs((px - p1x) * (px - p1x) - (py - p1y) * (py - p1y)));
            double pp2distance = Math.Sqrt(Math.Abs((px - p2x) * (px - p2x) - (py - p2y) * (py - p2y)));
            double p1p2distance = Math.Sqrt(Math.Abs((p1x - p2x) * (p1x - p2x) - (p1y - p2y) * (p1y - p2y)));
            if (Math.Abs(p1p2distance - (pp1distance + pp2distance)) < threshold)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //判断点是否在多边形内部
        public static bool IsInPolygon2(DrillModel checkPoint, IList<DrillModel> polygonPoints)
        {
            int counter = 0;
            int i;
            double xinters;
            DrillModel p1, p2;
            int pointCount = polygonPoints.Count;
            p1 = polygonPoints[0];
            for (i = 1; i <= pointCount; i++)
            {
                p2 = polygonPoints[i % pointCount];
                if (checkPoint.Y > Math.Min(p1.Y, p2.Y)//校验点的Y大于线段端点的最小Y
                    && checkPoint.Y <= Math.Max(p1.Y, p2.Y))//校验点的Y小于线段端点的最大Y
                {
                    if (checkPoint.X <= Math.Max(p1.X, p2.X))//校验点的X小于等线段端点的最大X(使用校验点的左射线判断).
                    {
                        if (p1.Y != p2.Y)//线段不平行于X轴
                        {
                            xinters = (checkPoint.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
                            if (p1.X == p2.X || checkPoint.X <= xinters)
                            {
                                counter++;
                            }
                        }
                    }

                }
                p1 = p2;
            }

            if (counter % 2 == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        //对点进行排序
        public static List<DrillModel> SortPoints(List<DrillModel> points)
        {
            DrillModel centroid = new DrillModel();
            double signedArea = 0;

            // 计算质心
            for (int i = 0; i < points.Count; i++)
            {
                DrillModel p1 = points[i];
                DrillModel p2 = points[(i + 1) % points.Count];
                double a = p1.X * p2.Y - p2.X * p1.Y;
                signedArea += a;
                centroid.X += (p1.X + p2.X) * a;
                centroid.Y += (p1.Y + p2.Y) * a;
            }

            signedArea /= 2;
            centroid.X /= (6 * signedArea);
            centroid.Y /= (6 * signedArea);

            // 计算每个点到质心的向量
            List<DrillModel> vectors = new List<DrillModel>();
            for (int i = 0; i < points.Count; i++)
            {
                DrillModel drill = new DrillModel();
                drill.X = points[i].X - centroid.X;
                drill.Y = points[i].Y - centroid.Y;
                vectors.Add(drill);
            }

            // 排序
            vectors.Sort((v1, v2) => Math.Atan2(v1.Y, v1.X).CompareTo(Math.Atan2(v2.Y, v2.X)));
            List<DrillModel> sortedPoints = vectors.Select(v => new DrillModel(v.X + centroid.X, v.Y + centroid.Y)).ToList();

            return sortedPoints;
        }
        public Points RetriveDrillByCoord(double x, double y, IList<Points> points)
        {

            foreach (Points keyValuePair in points)
            {
                var oldDr = keyValuePair;

                if (Math.Abs(oldDr.x - x) < 0.1 && Math.Abs(oldDr.y - y) < 0.1)
                {
                    return oldDr;
                }
            }
            return null;
        }

        /// <summary>
        /// 块体模型：构建体模型
        /// </summary>
        /// <param name="_triMesh">三角网</param>
        /// <param name="_pastboundaryDispersPoints">边界离散点</param>
        /// <param name="allPointson">上层所有点</param>
        /// <param name="allPointsdown">下层所有点</param>
        /// <exception cref="Exception"></exception>
        public void BuildBlockModel(TriMesh _triMesh, IList<Points> _pastboundaryDispersPoints, IList<Points> allPointson, IList<Points> allPointsdown)
        {

            //初始化每层的BrepModel
            for (int i = 0; i < 1; i++)
            {
                this._brepList.Add(new BrepModel());
            }
            BrepModel brep = _brepList[0] as BrepModel;
            //////////////////////////////////////////////////////////////////////////

            // 计算上下地层面的三角形
            foreach (Triangle tri in _triMesh.triangleList)
            {
                Vertex tv0 = _triMesh.vertexList[tri.v0];
                Vertex tv1 = _triMesh.vertexList[tri.v1];
                Vertex tv2 = _triMesh.vertexList[tri.v2];


                Points d0 = RetriveDrillByCoord(tv0.x, tv0.y, allPointson);
                Points d1 = RetriveDrillByCoord(tv1.x, tv1.y, allPointson);
                Points d2 = RetriveDrillByCoord(tv2.x, tv2.y, allPointson);
                Points d0d = RetriveDrillByCoord(tv0.x, tv0.y, allPointsdown);
                Points d1d = RetriveDrillByCoord(tv1.x, tv1.y, allPointsdown);
                Points d2d = RetriveDrillByCoord(tv2.x, tv2.y, allPointsdown);
                if (d0 == null || d1 == null || d2 == null || d0d == null || d1d == null || d2d == null)
                {
                    // 孙，钻孔数量不匹配，暂做跳过处理
                    //continue;&& d1.z < d1d.z && d2.z < d2d.z
                    throw new Exception("找不到对应的钻孔");

                }




                Vector3 vn1 = new Vector3((float)d0.x, (float)d0.y, (float)Math.Round(d0.z, 3));
                Vector3 vn2 = new Vector3((float)d1.x, (float)d1.y, (float)Math.Round(d1.z, 3));
                Vector3 vn3 = new Vector3((float)d2.x, (float)d2.y, (float)Math.Round(d2.z, 3));

                Vector3 v1 = vn1 - vn2;
                Vector3 v2 = vn2 - vn3;
                Vector3 v3 = vn3 - vn1;
                float cross1 = Vector3.Cross(v1, v2).Z;
                float cross2 = Vector3.Cross(v2, v3).Z;
                float cross3 = Vector3.Cross(v3, v1).Z;

                if (d0.z <= d0d.z && d1.z <= d1d.z && d2.z <= d2d.z)
                {
                    continue;
                }

                if (cross1 * cross2 * cross3 >= 0)
                {
                    brep.addTriangle(d0.x, d0.y, Math.Round(d0.z, 3),
                    d1.x, d1.y, Math.Round(d1.z, 3),
                   d2.x, d2.y, Math.Round(d2.z, 3));
                }
                else
                {
                    brep.addTriangle(d0.x, d0.y, Math.Round(d0.z, 3),
                    d2.x, d2.y, Math.Round(d2.z, 3),
                   d1.x, d1.y, Math.Round(d1.z, 3));
                }

                Vector3 vm1 = new Vector3((float)d0d.x, (float)d0d.y, (float)Math.Round(d0d.z, 3));
                Vector3 vm2 = new Vector3((float)d1d.x, (float)d1.y, (float)Math.Round(d1d.z, 3));
                Vector3 vm3 = new Vector3((float)d2d.x, (float)d2d.y, (float)Math.Round(d2d.z, 3));

                Vector3 vv1 = vm1 - vm2;
                Vector3 vv2 = vm2 - vm3;
                Vector3 vv3 = vm3 - vm1;
                float crossv1 = Vector3.Cross(vv1, vv2).Z;
                float crossv2 = Vector3.Cross(vv2, vv3).Z;
                float crossv3 = Vector3.Cross(vv3, vv1).Z;


                if (crossv1 * crossv2 * crossv3 >= 0)
                {
                    brep.addTriangle(d0d.x, d0d.y, Math.Round(d0d.z, 3),
                    d1d.x, d1d.y, Math.Round(d1d.z, 3),
                   d2d.x, d2d.y, Math.Round(d2d.z, 3));
                }
                else
                {
                    brep.addTriangle(d0d.x, d0d.y, Math.Round(d0d.z, 3),
                    d2d.x, d2d.y, Math.Round(d2d.z, 3),
                   d1d.x, d1d.y, Math.Round(d1d.z, 3));
                }
            }
            //return;//Temp code, 不缝合处理d2.x, d2.y, Math.Round(d2.z, 3)
            //这里有问题，由于进行了尖灭和细分边界点已经发生改变，所以要对新的边界点进行缝合
            //由于在出现约束的区域其细分部位发生变化，不再是简单的平分，所以需要重新搜索边界点
            //①处理外围边界:将外围钻孔找出

            // 缝合外围多层DEM
            for (int i = 0; i < 1; i++)
            {


                for (int j = 0; j < _pastboundaryDispersPoints.Count; j++)
                {
                    Points p0;
                    Points p1;
                    if (j != (_pastboundaryDispersPoints.Count - 1))
                    {
                        p0 = _pastboundaryDispersPoints[j];
                        p1 = _pastboundaryDispersPoints[j + 1];
                    }
                    else
                    {
                        p0 = _pastboundaryDispersPoints[j];
                        p1 = _pastboundaryDispersPoints[0];
                    }

                    Points b0 = RetriveDrillByCoord(p0.x, p0.y, allPointson);
                    Points b1 = RetriveDrillByCoord(p1.x, p1.y, allPointson);
                    Points b0d = RetriveDrillByCoord(p0.x, p0.y, allPointsdown);
                    Points b1d = RetriveDrillByCoord(p1.x, p1.y, allPointsdown);
                    if (b0.z == b0d.z && b1.z == b1d.z)
                    {
                        continue;
                    }

                    if (b0.z == b0d.z)
                    {
                        brep.addTriangle(b0d.x, b0d.y, Math.Round(b0d.z, 3),
                       b1.x, b1.y, Math.Round(b1.z, 3),
                       b1d.x, b1d.y, Math.Round(b1d.z, 3));

                        continue;
                    }

                    if (b1.z == b1d.z)
                    {
                        brep.addTriangle(b0.x, b0.y, Math.Round(b0.z, 3),
                      b1.x, b1.y, Math.Round(b1.z, 3),
                      b0d.x, b0d.y, Math.Round(b0d.z, 3));

                        continue;
                    }
                    brep.addTriangle(b0.x, b0.y, Math.Round(b0.z, 3),
                      b1.x, b1.y, Math.Round(b1.z, 3),
                      b0d.x, b0d.y, Math.Round(b0d.z, 3));

                    brep.addTriangle(b0d.x, b0d.y, Math.Round(b0d.z, 3),
                        b1.x, b1.y, Math.Round(b1.z, 3),
                        b1d.x, b1d.y, Math.Round(b1d.z, 3));

                }
            }


        }

        /// <summary>
        /// 块体模型：构建面模型
        /// </summary>
        /// <param name="_triMesh">三角网</param>
        /// <param name="allPoints">所有点</param>
        /// <exception cref="Exception"></exception>
        public void BuildSurfaceModel(TriMesh _triMesh, IList<Points> allPoints)
        {

            //初始化每层的BrepModel
            for (int i = 0; i < 1; i++)
            {
                this._brepList.Add(new BrepModel());
            }

            //////////////////////////////////////////////////////////////////////////

            // 计算上下地层面的三角形
            foreach (Triangle tri in _triMesh.triangleList)
            {
                Vertex tv0 = _triMesh.vertexList[tri.v0];
                Vertex tv1 = _triMesh.vertexList[tri.v1];
                Vertex tv2 = _triMesh.vertexList[tri.v2];


                Points d0 = RetriveDrillByCoord(tv0.x, tv0.y, allPoints);
                Points d1 = RetriveDrillByCoord(tv1.x, tv1.y, allPoints);
                Points d2 = RetriveDrillByCoord(tv2.x, tv2.y, allPoints);
                if (d0 == null || d1 == null || d2 == null)
                {
                    // 孙，钻孔数量不匹配，暂做跳过处理
                    //continue;
                    throw new Exception("找不到对应的钻孔");
                }



                BrepModel brep = _brepList[0] as BrepModel;

                Vector3 vn1 = new Vector3((float)d0.x, (float)d0.y, (float)Math.Round(d0.z, 3));
                Vector3 vn2 = new Vector3((float)d1.x, (float)d1.y, (float)Math.Round(d1.z, 3));
                Vector3 vn3 = new Vector3((float)d2.x, (float)d2.y, (float)Math.Round(d2.z, 3));

                Vector3 v1 = vn1 - vn2;
                Vector3 v2 = vn2 - vn3;
                Vector3 v3 = vn3 - vn1;
                float cross1 = Vector3.Cross(v1, v2).Z;
                float cross2 = Vector3.Cross(v2, v3).Z;
                float cross3 = Vector3.Cross(v3, v1).Z;
                if (cross1 * cross2 * cross3 >= 0)
                {
                    brep.addTriangle(d0.x, d0.y, Math.Round(d0.z, 3),
                    d1.x, d1.y, Math.Round(d1.z, 3),
                   d2.x, d2.y, Math.Round(d2.z, 3));
                }
                else
                {
                    brep.addTriangle(d0.x, d0.y, Math.Round(d0.z, 3),
                    d2.x, d2.y, Math.Round(d2.z, 3),
                   d1.x, d1.y, Math.Round(d1.z, 3));
                }



            }


        }
        /// <summary>
        /// 块体模型：输出模型
        /// </summary>
        /// <param name="fileDirectory"></param>
        /// <param name="q"></param>
        public void ExportBrepModels(string fileDirectory)
        {

            string fileName = fileDirectory + "\\brepsLayer" ;
            for (int i = 0; i < 1; i++)
            {
                BrepModel brep = _brepList[i];

                string mathinterphelpFileName = fileName + ".obj";

                brep.exportobj(mathinterphelpFileName);

            }

        }
    }
}
