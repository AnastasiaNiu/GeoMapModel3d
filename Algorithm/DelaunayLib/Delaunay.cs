using CommonDataStructureLib;
using GeoAPI.Geometries;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DelaunayLib
{
    class Delaunay : IDelaunay
    {
        internal Delaunay() { }
        /// <summary>
        /// 带内外约束的三角剖分
        /// </summary>
        /// <param name="pointss"></param>
        /// <param name="linePoints"></param>
        /// <param name="allinlinePoints"></param>
        /// <param name="inlinePointslists"></param>
        /// <returns></returns>
        public TriMesh  BuildDelaunay(IList<Points> pointss, IList<Points> linePoints, IList<Points> allinlinePoints, IList<IList<Points>> inlinePointslists)
        {
            List<Triangle> T1 = new List<Triangle>();//三角形
            //定义一个三角形列表对象，并将三角形列表对象初始化为null，作为后续条件
            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            List<double> H = new List<double>();
            //Bowyer-Watson算法
            for (int i = 0; i < pointss.Count; i++)
            {
                X.Add(pointss[i].x);
                Y.Add(pointss[i].y);
                H.Add(pointss[i].z);
            }
            List<Vector2> polygon = new List<Vector2>();
            double Xmin = X.Min();
            double Xmax = X.Max();
            double Ymin = Y.Min();
            double Ymax = Y.Max();
            Point p1 = new Point("p1", Xmin - 1, Ymin - 1, 0);
            Point p2 = new Point("p2", Xmin - 1, Ymax + 1, 0);
            Point p3 = new Point("p3", Xmax + 1, Ymax + 1, 0);
            Point p4 = new Point("p4", Xmax + 1, Ymin - 1, 0);
            List<Triangle> tempT1 = new List<Triangle>();//三角形

            tempT1.Add(new Triangle(p2, p1, p3, 0));//添加初始三角形
            tempT1.Add(new Triangle(p4, p1, p3, 0));

            List<Point> point = new List<Point>();
            for (int i = 0; i < X.Count; i++)
            {
                string id = Convert.ToString(i);
                point.Add(new Point(id, Math.Round(X[i], 3), Math.Round(Y[i], 3), Math.Round(H[i], 3)));//点

            }
            if (allinlinePoints != null)
            {
                //添加内边界约束点，point中添加是为了依次约束做三角网，inlinepoints添加是为了之后的删除边界内的三角形
                for (int i = 0; i < allinlinePoints.Count; i++)
                {
                    string id = Convert.ToString(i);

                    point.Add(new Point("in" + id, Math.Round(allinlinePoints[i].x, 3),
                        Math.Round(allinlinePoints[i].y, 3), Math.Round(allinlinePoints[i].z, 3)));//点

                }
            }

            for (int i = 0; i < point.Count; i++)
            {
                for (int j = point.Count - 1; j > i; j--)
                {
                    if (point[i].x == point[j].x && point[i].y == point[j].y)
                    {
                        point.RemoveAt(j);
                    }
                }
            }
            T1 = BuildDe(point, tempT1);

            //使用NTS生成三角网

            //将所有三角形的边插入edgeSet（注意这里每条边都重复，所以要在边类里面明确不能重复）

            HashSet<Edge> edgeSet = new HashSet<Edge>();
            for (int i = 0; i < T1.Count; i++)
            {
                Edge ea = new Edge(T1[i].p1, T1[i].p2);
                Edge eb = new Edge(T1[i].p3, T1[i].p2);
                Edge ec = new Edge(T1[i].p1, T1[i].p3);

                edgeSet.Add(ea);
                edgeSet.Add(eb);
                edgeSet.Add(ec);
            }
            List<Edge> CE = new List<Edge>();
            List<double> la = new List<double>();
            List<double> lb = new List<double>();
            List<double> lc = new List<double>();
            if (linePoints.Count != 0)
            {
                for (int i = 0; i < linePoints.Count; i++)
                {
                    la.Add(Math.Round(linePoints[i].x, 3));
                    lb.Add(Math.Round(linePoints[i].y, 3));
                    lc.Add(Math.Round(linePoints[i].z, 3));
                }
                //定义外约束边
                for (int i = 0; i < linePoints.Count - 1; i++)
                {

                    Point l1 = new Point("l" + Convert.ToString(i), la[i], lb[i], lc[i]);
                    Point l2 = new Point("l" + Convert.ToString(i + 1), la[i + 1], lb[i + 1], lc[i + 1]);
                    Edge e1 = new Edge(l1, l2);
                    CE.Add(e1);
                }
                string idnew = Convert.ToString(linePoints.Count - 1);
                Point la1 = new Point("l" + Convert.ToString(linePoints.Count - 1), la[linePoints.Count - 1], lb[linePoints.Count - 1], lc[linePoints.Count - 1]);
                Point la2 = new Point("l" + "0", la[0], lb[0], lc[0]);
                Edge ea1 = new Edge(la1, la2);
                CE.Add(ea1);
            }
            

            //定义内约束边（这里使用lists，使得每个孔为一个封闭圈）
            for (int g = 0; g < inlinePointslists.Count; g++)
            {
                IList<Points> inlinePoints = inlinePointslists[g];
                for (int i = 0; i < inlinePoints.Count - 1; i++)
                {

                    Point l1 = new Point("in" + Convert.ToString(g) + "id" + Convert.ToString(i), inlinePoints[i].x, inlinePoints[i].y, inlinePoints[i].z);
                    Point l2 = new Point("in" + Convert.ToString(g) + "id" + Convert.ToString(i + 1), inlinePoints[i + 1].x, inlinePoints[i + 1].y, inlinePoints[i + 1].z);
                    Edge e1 = new Edge(l1, l2);

                    CE.Add(e1);
                }
            }


            List<Edge> edgeSets = new List<Edge>();
            edgeSets = edgeSet.ToList();
            //判断约束边是否在三角网所有边内

            //创建一个需要操作的约束边列表doedge
            List<Edge> doedgepast = new List<Edge>();
            for (int j = 0; j < CE.Count; j++)
            {
                if (!IsContainline(edgeSets, CE[j]))
                {
                    doedgepast.Add(CE[j]);
                }
            }
            //--------------------寻找影响域

            //查找该边与所有边中哪些边相交,并将其放入对角边列表Des(这里Des为双层列表，内层为每个约束边下的一系列小的待处理对角线边)
            List<List<Edge>> Des = new List<List<Edge>>();
            for (int i = 0; i < doedgepast.Count; i++)
            {
                HashSet<Edge> De = new HashSet<Edge>();
                for (int j = 0; j < edgeSets.Count; j++)
                {
                    if (IsIntersect(doedgepast[i], edgeSets[j]))
                    {
                        De.Add(edgeSets[j]);
                    }
                }
                List<Edge> tDe = new List<Edge>();
                tDe = De.ToList();
                if (tDe.Count != 0)
                {
                    Des.Add(tDe);
                }
                else
                {
                    doedgepast[i] = null;
                }

            }
            List<Edge> doedge = new List<Edge>();
            for (int k = 0; k < doedgepast.Count; k++)
            {
                if (doedgepast[k] != null)
                    doedge.Add(doedgepast[k]);
            }
            //由线段找所在的三角形即影响域
            //定义有多个三角形组成的大影响域双层列表（内层为每个大影响域下的一系列小影响域）
            List<List<Triangle>> infulTris = new List<List<Triangle>>();

            for (int k = 0; k < Des.Count; k++)
            {
                List<Triangle> infulTri = new List<Triangle>();
                for (int i = 0; i < Des[k].Count; i++)
                {
                    for (int j = 0; j < T1.Count; j++)
                    {
                        if (IsEdgeEqual(Des[k][i], new Edge(T1[j].p1, T1[j].p2)) ||
                            IsEdgeEqual(Des[k][i], new Edge(T1[j].p1, T1[j].p3)) ||
                            IsEdgeEqual(Des[k][i], new Edge(T1[j].p3, T1[j].p2)))
                        {
                            if (!IsContaintri(infulTri, T1[j]))
                                infulTri.Add(T1[j]);

                        }
                    }

                }
                if (infulTri.Count > 0)
                {
                    infulTris.Add(infulTri);
                }

            }

            //对第i个大影响域中的每个小影响域进行顺序排列
            //Triangle temp = infulTris[0][0];
            for (int i = 0; i < infulTris.Count; i++)
            {
                int q = 0;
                List<Triangle> sortTri = new List<Triangle>();

                //寻找第一个三角形
                for (int j = 0; j < infulTris[i].Count; j++)
                {
                    //如果包含约束边的P1点就将此三角形传给sortTri作为第一个三角形
                    if (infulTris[i][j].Iscontainpoint(new Point("first", doedge[i].p1.x, doedge[i].p1.y, doedge[i].p1.z)))
                    {
                        sortTri.Add(infulTris[i][j]);
                    }
                    else
                    {
                        continue;
                    }
                }
                if (sortTri.Count == 0)
                {
                    for (int j = 0; j < infulTris[i].Count; j++)
                    {
                        //如果包含约束边的P1点就将此三角形传给sortTri作为第一个三角形
                        if (infulTris[i][j].Iscontainpoint(new Point("first", doedge[i].p2.x, doedge[i].p2.y, doedge[i].p2.z)))
                        {
                            sortTri.Add(infulTris[i][j]);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                //从第一个三角形开始排序，相邻就往后接着排
                for (int j = infulTris[i].Count - 1; j >= 0; j--)
                {
                    if (IsTriangleEqual(sortTri[q], infulTris[i][j]))
                    {
                        continue;
                    }
                    if (sortTri.Contains(infulTris[i][j]) == false)
                    {
                        if (sortTri[q].IsAdjacentTo(infulTris[i][j]))
                        {
                            sortTri.Add(infulTris[i][j]);
                            q = q + 1;
                            j = infulTris[i].Count;
                        }
                    }

                }
                //if (sortTri.Count != infulTris[i].Count)
                //{
                //    i = i - 1;
                //}
                //else
                //{
                infulTris[i].RemoveRange(0, infulTris[i].Count);
                infulTris[i] = sortTri;
                //}

            }
            //-------------------在某约束边的影响域中，两两相邻的三角影响域合并的四边形判断是否为凸边形
            //在第k个约束边的影响域j中开始进行翻转操作             
            for (int i = infulTris.Count - 1; i >= 0; i--)
            {
                bool Isright = true;
                bool die = false;
                for (int j = infulTris[i].Count - 1; j > 0; j--)
                {

                    Edge value = sameEdge(infulTris[i][j - 1], infulTris[i][j]);
                    if (value == null)
                    {
                        continue;
                    }
                    //如果是凸边形
                    if (IsConvexQuad3(infulTris[i][j - 1], infulTris[i][j], value) == 1)
                    {

                        //删除该对角线
                        edgeSets.Remove(value);
                        Edge newedge = new Edge(infulTris[i][j - 1].reOtherEdge(value), infulTris[i][j].reOtherEdge(value));
                        //填入新的对角线（即四边形对角线交换）
                        edgeSets.Add(newedge);
                        //原三角形列表删除与原先对角线相关的三角形
                        T1.Remove(infulTris[i][j - 1]);
                        T1.Remove(infulTris[i][j]);

                        //三角形列表填入新三角形
                        Triangle tt1 = new Triangle(value.p1, newedge.p1, newedge.p2, 0);
                        T1.Add(tt1);
                        Triangle tt2 = new Triangle(value.p2, newedge.p1, newedge.p2, 0);
                        T1.Add(tt2);

                        //如果j!=infulTris[i].Count - 1,那么将tt1或tt2与上一个三角形相邻的作为j，不相邻的作为j-1
                        if (j != infulTris[i].Count - 1 && j - 2 >= 0)
                        {
                            if (tt1.IsAdjacentTo(infulTris[i][j - 2]))
                            {
                                infulTris[i][j - 1] = tt1;
                                infulTris[i][j] = tt2;
                            }
                            else
                            {
                                infulTris[i][j - 1] = tt2;
                                infulTris[i][j] = tt1;
                            }
                        }
                        else
                        {
                            if (infulTris[i].Count >= 3)
                            {
                                if (tt2.IsAdjacentTo(infulTris[i][infulTris[i].Count - 3]))
                                {
                                    infulTris[i][j - 1] = tt2;
                                    infulTris[i][j] = tt1;
                                }
                                else
                                {
                                    infulTris[i][j - 1] = tt1;
                                    infulTris[i][j] = tt2;
                                }
                            }
                            else
                            {
                                infulTris[i][j - 1] = tt2;
                                infulTris[i][j] = tt1;
                            }

                        }

                    }
                    else if (IsConvexQuad3(infulTris[i][j - 1], infulTris[i][j], value) == 0 || IsConvexQuad3(infulTris[i][j - 1], infulTris[i][j], value) == 2)
                    {
                        Isright = false;
                        //if (j == 1)
                        //{
                        //    Isright = true;
                        //    die = true;
                        //}
                        //if (j == infulTris[i].Count - 1) { die = true; }
                    }
                }
                //处理后仍有凹边形的，影响域重新整理
                if (!Isright && !die)
                {
                    //获得受影响的区域，这里注意踩了无数坑（重新开始循环时候，影响域会发生改变！！）
                    int q = 0;
                    List<Triangle> sortTri = new List<Triangle>();
                    List<Triangle> newsortTri = new List<Triangle>();
                    for (int s = 0; s < infulTris[i].Count; s++)
                    {
                        if (IsIntersect(new Edge(infulTris[i][s].p1, infulTris[i][s].p2), doedge[i]) ||
                            IsIntersect(new Edge(infulTris[i][s].p1, infulTris[i][s].p3), doedge[i]) ||
                            IsIntersect(new Edge(infulTris[i][s].p3, infulTris[i][s].p2), doedge[i]))
                        {
                            newsortTri.Add(infulTris[i][s]);
                        }
                    }
                    //寻找第一个三角形
                    //从第一个三角形开始排序，相邻就往后接着排
                    for (int j = 0; j < newsortTri.Count; j++)
                    {
                        //如果包含约束边的P1点就将此三角形传给temp
                        if (newsortTri[j].Iscontainpoint(new Point("first", doedge[i].p1.x, doedge[i].p1.y, doedge[i].p1.z)))
                        {
                            sortTri.Add(newsortTri[j]);

                        }

                    }
                    for (int j = newsortTri.Count - 1; j >= 0; j--)
                    {
                        if (IsTriangleEqual(sortTri[q], newsortTri[j]))
                        {
                            continue;
                        }
                        if (sortTri.Contains(newsortTri[j]) == false)
                        {
                            if (sortTri[q].IsAdjacentTo(newsortTri[j]))
                            {
                                sortTri.Add(newsortTri[j]);
                                q = q + 1;
                                j = newsortTri.Count;
                            }
                        }
                    }

                    infulTris[i].RemoveRange(0, infulTris[i].Count);
                    infulTris[i] = sortTri;

                    i = i + 1;

                }
            }


            for (int i = 0; i < T1.Count; i++)
            {
                if (T1[i].p1 == p1 || T1[i].p2 == p1 || T1[i].p3 == p1)
                {
                    T1[i].B = false;
                }
                else if (T1[i].p1 == p2 || T1[i].p2 == p2 || T1[i].p3 == p2)
                {
                    T1[i].B = false;
                }
                else if (T1[i].p1 == p3 || T1[i].p2 == p3 || T1[i].p3 == p3)
                {
                    T1[i].B = false;
                }
                else if (T1[i].p1 == p4 || T1[i].p2 == p4 || T1[i].p3 == p4)
                {
                    T1[i].B = false;
                }
            }
            //对不在外边界内部的三角形进行删除处理
            //将drillmodel转换为point
            List<double> x = new List<double>();
            List<double> y = new List<double>();
            List<double> h = new List<double>();
            //设置newlinepoints边界点
            if (linePoints.Count != 0)
            {
                for (int i = 0; i < linePoints.Count; i++)
                {
                    x.Add(Math.Round(linePoints[i].x, 3));
                    y.Add(Math.Round(linePoints[i].y, 3));
                    h.Add(Math.Round(linePoints[i].z, 3));
                }
                List<Point> newlinepoints = new List<Point>();
                for (int i = 0; i < linePoints.Count; i++)
                {
                    Point p = new Point("l", x[i], y[i], h[i]);
                    newlinepoints.Add(p);
                }
                //判断中心点是否在多边形内部

                for (int i = 0; i < T1.Count; i++)
                {
                    Point centerpoint = T1[i].Iscenterpoint();
                    if (!IsInPolygon2(centerpoint, newlinepoints))
                    {
                        T1[i].B = false;
                    }
                }
            }



            //处理内边界，获得所有三角形的中心，当中心在内边界范围内删除该三角形
            if (inlinePointslists != null)
            {
                for (int g = 0; g < inlinePointslists.Count; g++)
                {
                    List<Point> inlinepointslist = new List<Point>();
                    for (int k = 0; k < inlinePointslists[g].Count - 1; k++)
                    {
                        inlinepointslist.Add(new Point("in" + Convert.ToString(g) + "id" + Convert.ToString(k),
                            inlinePointslists[g][k].x, inlinePointslists[g][k].y, inlinePointslists[g][k].z));
                    }
                    for (int i = 0; i < T1.Count; i++)
                    {
                        Point centerpoint = T1[i].Iscenterpoint();
                        if (IsInPolygon2(centerpoint, inlinepointslist))
                        {
                            T1[i].B = false;
                        }

                        //如果这个三角形一点也在范围内则删除该三角形
                        if (IsOnBoundary(T1[i].p1, inlinepointslist) || IsOnBoundary(T1[i].p2, inlinepointslist) || IsOnBoundary(T1[i].p3, inlinepointslist))
                        {
                            //如果三角形中其中两点在这个边界上跳过，如果不在则删除
                            if (IsOnBoundary(T1[i].p1, inlinepointslist) && IsOnBoundary(T1[i].p2, inlinepointslist) ||
                                 IsOnBoundary(T1[i].p1, inlinepointslist) && IsOnBoundary(T1[i].p3, inlinepointslist) ||
                                 IsOnBoundary(T1[i].p3, inlinepointslist) && IsOnBoundary(T1[i].p2, inlinepointslist))
                            {
                                continue;
                            }
                            else
                            {
                                if (IsInPolygon2(AVERAGE(T1[i].p1, T1[i].p2), inlinepointslist) ||
                                   IsInPolygon2(AVERAGE(T1[i].p1, T1[i].p3), inlinepointslist) ||
                                   IsInPolygon2(AVERAGE(T1[i].p3, T1[i].p2), inlinepointslist))
                                {
                                    //T1[i].B = false;
                                }

                            }

                        }

                    }
                }
            }


            //删除不符合规定的三角形
            for (int i = T1.Count - 1; i >= 0; i--)
            {
                if (T1[i].B == false)
                {
                    T1.RemoveAt(i);
                }
            }

            TriMesh triMesh0 = new TriMesh();
            for (int i = 0; i < T1.Count; i++)
            {

                triMesh0.AddTriangle(T1[i].p3.x, T1[i].p3.y, T1[i].p3.z,
                   T1[i].p2.x, T1[i].p2.y, T1[i].p2.z,
                   T1[i].p1.x, T1[i].p1.y, T1[i].p1.z);

            }

            return triMesh0;
        }

        private Point AVERAGE(Point p1, Point p2)
        {
            return new Point("average", (p1.x + p2.x) / 2, (p1.y + p2.y) / 2, (p1.z + p2.z) / 2);
        }
        public static bool IsOnBoundary(Point point, IList<Point> polygon)
        {

            for (int i = polygon.Count - 1; i > 0; i--)
            {

                Point p1 = polygon[i];
                Point p2 = polygon[i - 1];
                if (IsOnLine(point, p1, p2, 0.00001))
                {
                    return true;
                }
            }

            return false;
        }
        public static bool IsOnLine(Point point, Point p1, Point p2, double threshold)
        {



            double px = Math.Round(point.x, 3);
            double py = Math.Round(point.y, 3);
            double p1x = Math.Round(p1.x, 3);
            double p1y = Math.Round(p1.y, 3);
            double p2x = Math.Round(p2.x, 3);
            double p2y = Math.Round(p2.y, 3);
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
        //判断三角形面积
        public static double ComputeTriangleArea(Point a, Point b, Point c)
        {
            // Calculate the lengths of the sides of the triangle.
            double sideA = Math.Sqrt(Math.Pow(b.x - a.x, 3) + Math.Pow(b.y - a.y, 3));
            double sideB = Math.Sqrt(Math.Pow(c.x - b.x, 3) + Math.Pow(c.y - b.y, 3));
            double sideC = Math.Sqrt(Math.Pow(a.x - c.x, 3) + Math.Pow(a.y - c.y, 3));

            // Use Heron's formula to calculate the area of the triangle.
            double s = (sideA + sideB + sideC) / 2;
            double area = Math.Sqrt(s * (s - sideA) * (s - sideB) * (s - sideC));

            return area;
        }
        //构建三角网，注意这是从原编码中剥离出来的，如果不想使用可以再带回去
        public static List<Triangle> BuildDe(List<Point> point, List<Triangle> T1)
        {
            for (int i = 0; i < point.Count; i++)//循环点
            {
                List<Edge> edge = new List<Edge>();//储存边
                for (int j = 0; j < T1.Count; j++)
                {
                    //计算圆心坐标和半径
                    double[] x0y0r = X0Y0R(T1[j].p1.x, T1[j].p1.y, T1[j].p2.x, T1[j].p2.y, T1[j].p3.x, T1[j].p3.y);
                    //计算点到圆心的距离
                    double Dis = Math.Sqrt((point[i].x - x0y0r[0]) * (point[i].x - x0y0r[0]) + (point[i].y - x0y0r[1]) * (point[i].y - x0y0r[1]));

                    if (Dis < x0y0r[2])
                    {
                        edge.Add(new Edge(T1[j].p1, T1[j].p2));
                        edge.Add(new Edge(T1[j].p2, T1[j].p3));
                        edge.Add(new Edge(T1[j].p3, T1[j].p1));
                        T1[j].B = false;
                    }
                }
                for (int j = T1.Count - 1; j >= 0; j--)//删除不满足三角形
                {
                    if (T1[j].B == false)
                    {
                        T1.RemoveAt(j);
                    }
                }

                for (int j = 0; j < edge.Count; j++)//删除重复边
                {
                    for (int n = j + 1; n < edge.Count; n++)
                    {
                        if ((edge[j].p1 == edge[n].p1 && edge[j].p2 == edge[n].p2) || (edge[j].p2 == edge[n].p1 && edge[j].p1 == edge[n].p2))
                        {
                            edge[j].B = false;
                            edge[n].B = false;
                        }
                    }
                }

                for (int j = 0; j < edge.Count; j++)//组成新三角形
                {
                    if (edge[j].B == true)
                    {
                        T1.Add(new Triangle(edge[j].p1, edge[j].p2, point[i], 0));
                    }
                }
            }
            return T1;
        }
        //判断三角形列表中是否包含该三角形
        public static bool IsContaintri(List<Triangle> listtri, Triangle tri)
        {
            for (int i = 0; i < listtri.Count; i++)
            {
                if (IsTriangleEqual(listtri[i], tri))
                {
                    return true;
                }
            }
            return false;
        }
        //判断点是否在多边形内部
        public static bool IsInPolygon2(Point checkPoint, List<Point> polygonPoints)
        {

            int counter = 0;
            int i;
            double xinters;
            Point p1, p2;
            int pointCount = polygonPoints.Count;
            p1 = polygonPoints[0];
            for (i = 1; i <= pointCount; i++)
            {
                p2 = polygonPoints[i % pointCount];
                if (checkPoint.y > Math.Min(p1.y, p2.y)//校验点的Y大于线段端点的最小Y
                    && checkPoint.y <= Math.Max(p1.y, p2.y))//校验点的Y小于线段端点的最大Y
                {
                    if (checkPoint.x <= Math.Max(p1.x, p2.x))//校验点的X小于等线段端点的最大X(使用校验点的左射线判断).
                    {
                        if (p1.y != p2.y)//线段不平行于X轴
                        {
                            xinters = (checkPoint.y - p1.y) * (p2.x - p1.x) / (p2.y - p1.y) + p1.x;
                            if (p1.x == p2.x || checkPoint.x <= xinters)
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
        //判断边列表中是否包含该边
        public static bool IsContainline(List<Edge> listedge, Edge edge)
        {
            for (int i = 0; i < listedge.Count; i++)
            {
                if (IsEdgeEqual(listedge[i], edge))
                {
                    return true;
                }
            }
            return false;
        }
        public static double[] X0Y0R(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double x0 = ((y2 - y1) * (y3 * y3 - y1 * y1 + x3 * x3 - x1 * x1) - (y3 - y1) * (y2 * y2 - y1 * y1 + x2 * x2 - x1 * x1)) / (2.0 * (x3 - x1) * (y2 - y1) - 2.0 * (x2 - x1) * (y3 - y1));
            double y0 = ((x2 - x1) * (x3 * x3 - x1 * x1 + y3 * y3 - y1 * y1) - (x3 - x1) * (x2 * x2 - x1 * x1 + y2 * y2 - y1 * y1)) / (2.0 * (y3 - y1) * (x2 - x1) - 2.0 * (y2 - y1) * (x3 - x1));
            double r = Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
            double[] x0y0r = { x0, y0, r };
            return x0y0r;
        }
        //判断两条边是否相交
        public static bool IsIntersect(Edge e1, Edge e2)
        {
            if (IsEdgeEqual(e1, e2))
            {
                return false;
            }

            Vector3 p1 = new Vector3((float)e1.p1.x, (float)e1.p1.y, 0);
            Vector3 p2 = new Vector3((float)e1.p2.x, (float)e1.p2.y, 0);
            Vector3 q1 = new Vector3((float)e2.p1.x, (float)e2.p1.y, 0);
            Vector3 q2 = new Vector3((float)e2.p2.x, (float)e2.p2.y, 0);

            Vector3 v1 = p2 - p1;
            Vector3 v2 = q1 - p1;
            Vector3 v3 = q2 - p1;

            Vector3 u1 = q2 - q1;
            Vector3 u2 = p1 - q1;
            Vector3 u3 = p2 - q1;

            float cross1 = Vector3.Cross(v1, v2).z;
            float cross2 = Vector3.Cross(v1, v3).z;
            float cross3 = Vector3.Cross(u1, u2).z;
            float cross4 = Vector3.Cross(u1, u3).z;
            if (e1.p1.IssamePoint(e2.p1) || e1.p1.IssamePoint(e2.p2) || e1.p2.IssamePoint(e2.p1) || e1.p2.IssamePoint(e2.p2))
            {
                return false;
            }
            if (cross1 * cross2 < 0 && cross3 * cross4 < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //判断两边是否相等
        public static bool IsEdgeEqual(Edge edge1, Edge edge2)
        {
            return ((IsPointEqual(edge1.p1, edge2.p1) && IsPointEqual(edge1.p2, edge2.p2)) ||
               (IsPointEqual(edge1.p1, edge2.p2) && IsPointEqual(edge1.p2, edge2.p1)));

        }
        //判断两点是否相等
        public static bool IsPointEqual(Point p1, Point p2)
        {
            return Math.Abs(p1.x - p2.x) < 0.1 && Math.Abs(p1.y - p2.y) < 0.1;
        }
        //判断四个点是否在同一圆周上
        public static bool IsPointInCircle(Point p, Point a, Point b, Point c)
        {
            double x1 = a.x, y1 = a.y;
            double x2 = b.x, y2 = b.y;
            double x3 = c.x, y3 = c.y;
            double A = x2 - x1, B = y2 - y1, C = x3 - x1, D = y3 - y1;
            double E = A * (x1 + x2) + B * (y1 + y2);
            double F = C * (x1 + x3) + D * (y1 + y3);
            double G = 2 * (A * (y3 - y2) - B * (x3 - x2));
            double x = (D * E - B * F) / G;
            double y = (A * F - C * E) / G;
            double r = Math.Sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1));

            // 计算距离并判断是否在圆内
            double d = Math.Sqrt((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y));
            return d < r;
        }


        //判断是否为凸四边形(方法三，调用NTS库计算）
        public int IsConvexQuad3(Triangle t1, Triangle t2, Edge sameEdge)
        {
            var coords = new Coordinate[]
            {
                new Coordinate(sameEdge.p1.x,sameEdge.p1.y),
                new Coordinate(reSqOtherEdge(t1, sameEdge).x,reSqOtherEdge(t1, sameEdge).y),
                new Coordinate(sameEdge.p2.x,sameEdge.p2.y),
                new Coordinate(reSqOtherEdge(t2, sameEdge).x,reSqOtherEdge(t2, sameEdge).y),
                new Coordinate(sameEdge.p1.x,sameEdge.p1.y),
            };
            var ring = new LinearRing(coords);
            var list = AnalysisAngles(ring);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] > 180)
                {
                    return 0;
                }
                if (list[i] == 180)
                {
                    return 2;
                }
            }
            return 1;

        }
        public static List<double> AnalysisAngles(LinearRing ring)
        {
            if (ring == null || !ring.IsSimple)
            {
                var angel = new List<double> { 0 };
                return angel;
            }

            var angels = new List<double>();
            for (int i = 0, len = ring.Coordinates.Length - 1; i < len; i++)
            {
                var tail = ring[i];
                var t2 = ring[(i + 1) % len];
                var t1 = ring[(i - 1 + len) % len];

                var angle = AngleUtility.AngleBetweenOriented(t1, tail, t2);
                var angleDegree = AngleUtility.ToDegrees(angle);
                if (ring.IsCCW)
                {
                    //逆时针
                    if (angle > 0)
                    {
                        //concave
                        angleDegree = 360 - angleDegree;
                    }
                    else if (angle < 0)
                    {
                        //convex
                        angleDegree = -angleDegree;
                    }
                    else
                    {
                        //等于0 平行
                        angleDegree = 180;
                    }
                }
                else
                {
                    //顺时针
                    if (angle < 0)
                    {
                        //concave
                        angleDegree = angleDegree + 360;
                    }
                    else if (angle > 0)
                    {
                        //convex
                    }
                    else
                    {
                        //等于0 平行
                        angleDegree = 180;
                    }
                }
                angels.Add(angleDegree);
            }
            return angels;
        }
        //返回三角形中的边的对应点
        public static Point reSqOtherEdge(Triangle t1, Edge edge)
        {
            if (((t1.p1 == edge.p1) && (t1.p2 == edge.p2)) || ((t1.p2 == edge.p1) && (t1.p1 == edge.p2)))
            {
                return t1.p3;
            }
            else if (((t1.p1 == edge.p1) && (t1.p3 == edge.p2)) || ((t1.p3 == edge.p1) && (t1.p1 == edge.p2)))
            {
                return t1.p2;
            }
            else
            {
                return t1.p1;
            }
        }
        //返回两个三角形中的共同边
        public static Edge sameEdge(Triangle t1, Triangle t2)
        {
            if (IsEdgeEqual(new Edge(t1.p1, t1.p2), new Edge(t2.p1, t2.p2)))
            {
                return new Edge(t1.p1, t1.p2);
            }
            else if (IsEdgeEqual(new Edge(t1.p1, t1.p3), new Edge(t2.p1, t2.p2)))
            {
                return new Edge(t1.p1, t1.p3);
            }
            else if (IsEdgeEqual(new Edge(t1.p2, t1.p3), new Edge(t2.p1, t2.p2)))
            {
                return new Edge(t1.p2, t1.p3);
            }
            else if (IsEdgeEqual(new Edge(t1.p1, t1.p2), new Edge(t2.p1, t2.p3)))
            {
                return new Edge(t1.p1, t1.p2);
            }
            else if (IsEdgeEqual(new Edge(t1.p1, t1.p3), new Edge(t2.p1, t2.p3)))
            {
                return new Edge(t1.p1, t1.p3);
            }
            else if (IsEdgeEqual(new Edge(t1.p2, t1.p3), new Edge(t2.p1, t2.p3)))
            {
                return new Edge(t1.p2, t1.p3);
            }
            else if (IsEdgeEqual(new Edge(t1.p1, t1.p2), new Edge(t2.p2, t2.p3)))
            {
                return new Edge(t1.p1, t1.p2);
            }
            else if (IsEdgeEqual(new Edge(t1.p1, t1.p3), new Edge(t2.p2, t2.p3)))
            {
                return new Edge(t1.p1, t1.p3);
            }
            else if (IsEdgeEqual(new Edge(t1.p2, t1.p3), new Edge(t2.p2, t2.p3)))
            {
                return new Edge(t1.p2, t1.p3);
            }
            else
            {

                return null;
            }
        }
        //判断两个三角形是否相等
        public static bool IsTriangleEqual(Triangle t1, Triangle t2)
        {
            if (t1.p1 == t2.p1 &&
                t1.p2 == t2.p2 &&
                t1.p3 == t2.p3)
            {
                return true;
            }
            else if (t1.p1 == t2.p2 && t1.p2 == t2.p1 && t1.p3 == t2.p3)
            {
                return true;
            }
            else if (t1.p1 == t2.p3 && t1.p2 == t2.p1 && t1.p3 == t2.p2)
            {
                return true;
            }
            else if (t1.p1 == t2.p1 && t1.p2 == t2.p3 && t1.p3 == t2.p2)
            {
                return true;
            }
            else if (t1.p1 == t2.p2 && t1.p2 == t2.p3 && t1.p3 == t2.p1)
            {
                return true;
            }
            else if (t1.p1 == t2.p3 && t1.p2 == t2.p2 && t1.p3 == t2.p1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    /// <summary>
    /// 点类
    /// </summary>
    public class Point
    {
        public double x;
        public double y;
        public double z;
        public string Id;
        public Point(string Id, double x, double y, double z)
        {
            this.Id = Id;
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public bool IssamePoint(Point p2)
        {
            if (Math.Abs(x - p2.x) < 0.1 && Math.Abs(y - p2.y) < 0.1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    /// <summary>
    /// 边类
    /// </summary>
    public class Edge
    {
        public Point p1;
        public Point p2;
        public bool B = true;

        public Edge(Point p1, Point p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
        public override bool Equals(object o)
        {
            if (o == null)
                return false;
            var other = o as Edge;
            if (other != null)
                return (p1.x == other.p1.x && p1.y == other.p1.y && p2.x == other.p2.x && p2.y == other.p2.y) ||
                    (p1.x == other.p2.x && p1.y == other.p2.y && p2.x == other.p1.x && p2.y == other.p1.y);
            return false;
        }
        public override int GetHashCode()
        {
            long bits0 = BitConverter.DoubleToInt64Bits(p1.x);
            bits0 ^= BitConverter.DoubleToInt64Bits(p1.y) * 31;
            int hash0 = (((int)bits0) ^ ((int)(bits0 >> 32)));

            long bits1 = BitConverter.DoubleToInt64Bits(p2.x);
            bits1 ^= BitConverter.DoubleToInt64Bits(p2.y) * 31;
            int hash1 = (((int)bits1) ^ ((int)(bits1 >> 32)));

            // XOR is supposed to be a good way to combine hashcodes
            return hash0 ^ hash1;

            // return base.GetHashCode();
        }

    }

    /// <summary>
    /// 三角形类
    /// </summary>
    public class Triangle
    {
        public Point p1;
        public Point p2;
        public Point p3;
        public bool B = true;
        public bool A = false;
        public double V;
        public Triangle(Point p1, Point p2, Point p3, double V)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.V = V;
        }

        //返回三角形中对边的点
        public Point reOtherEdge(Edge a)
        {

            if ((a.p1 == p1 && a.p2 == p2) || (a.p1 == p2 && a.p2 == p1))
            {
                return p3;
            }
            else if ((a.p1 == p1 && a.p2 == p3) || (a.p1 == p3 && a.p2 == p1))
            {
                return p2;
            }
            else
            {
                return p1;
            }

        }
        //返回三角形第三个顶点
        public Point GetThirdVertex(Point a, Point b)
        {

            Point nullPoint = null; ;
            if ((p1 == a && p2 == b) || (p1 == b && p2 == a))
            {
                return p3;
            }
            else if ((p1 == a && p3 == b) || (p1 == b && p3 == a))
            {
                return p2;
            }
            else if ((p2 == a && p3 == b) || (p2 == b && p3 == a))
            {
                return p1;
            }
            else
            {
                return nullPoint;
            }
        }
        //判断是否包含点a
        public bool Iscontainpoint(Point a)
        {
            if (Delaunay.IsPointEqual(p1, a))
            {
                return true;
            }
            else if (Delaunay.IsPointEqual(p2, a))
            {
                return true;
            }
            else if (Delaunay.IsPointEqual(p3, a))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //判断是否与other三角形相邻
        public bool IsAdjacentTo(Triangle other)
        {
            if ((p1.Id == other.p1.Id && p2.Id == other.p2.Id) ||
                (p1.Id == other.p2.Id && p2.Id == other.p1.Id))
                return true;
            if ((p1.Id == other.p1.Id && p2.Id == other.p3.Id) ||
                (p1.Id == other.p3.Id && p2.Id == other.p1.Id))
                return true;
            if ((p1.Id == other.p2.Id && p2.Id == other.p3.Id) ||
                (p1.Id == other.p3.Id && p2.Id == other.p2.Id))
                return true;
            if ((p1.Id == other.p1.Id && p3.Id == other.p2.Id) ||
                (p1.Id == other.p2.Id && p3.Id == other.p1.Id))
                return true;
            if ((p1.Id == other.p1.Id && p3.Id == other.p3.Id) ||
                (p1.Id == other.p3.Id && p3.Id == other.p1.Id))
                return true;
            if ((p1.Id == other.p2.Id && p3.Id == other.p3.Id) ||
                (p1.Id == other.p3.Id && p3.Id == other.p2.Id))
                return true;
            if ((p3.Id == other.p1.Id && p2.Id == other.p2.Id) ||
                (p3.Id == other.p2.Id && p2.Id == other.p1.Id))
                return true;
            if ((p3.Id == other.p1.Id && p2.Id == other.p3.Id) ||
                (p3.Id == other.p3.Id && p2.Id == other.p1.Id))
                return true;
            if ((p3.Id == other.p2.Id && p2.Id == other.p3.Id) ||
                (p3.Id == other.p3.Id && p2.Id == other.p2.Id))
                return true;

            return false;
        }
        //获得该三角形的中心点
        public Point Iscenterpoint()
        {
            double A = dis(p2, p3);
            double B = dis(p1, p3);
            double C = dis(p1, p2);
            double S = A + B + C;
            double x = (A * p1.x + B * p2.x + C * p3.x) / S;
            double y = (A * p1.y + B * p2.y + C * p3.y) / S;
            return new Point("center", x, y, 0);

        }
        //计算两点距离
        private double dis(Point p1, Point p2)
        {
            double dis = Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
            return dis;
        }
        //判断三角形是否包含边t
        public bool Iscontainedge(Edge t)
        {
            if (Delaunay.IsEdgeEqual(new Edge(p1, p2), t) ||
                Delaunay.IsEdgeEqual(new Edge(p1, p3), t) ||
                Delaunay.IsEdgeEqual(new Edge(p3, p2), t))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
