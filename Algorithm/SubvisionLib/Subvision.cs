using CommonDataStructureLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubvisionLib
{
    class Subvision : ISubvision
    {
        //计算三角形的法向量
        double PI = 3.1415926;
        double everageFlatness;
        double subThreshold;
        Dictionary<CHalfEdgeCell, Vector3D> cellNormals = new Dictionary<CHalfEdgeCell, Vector3D>();
        Dictionary<CHalfEdgeCell, double> cellFlatnesses = new Dictionary<CHalfEdgeCell, double>();
        Dictionary<CHalfEdgeEdge, CHalfEdgeVertex> edgeMap2Subvertex = new Dictionary<CHalfEdgeEdge, CHalfEdgeVertex>();
        Dictionary<CHalfEdgeVertex, DrillModel> _drillList = new Dictionary<CHalfEdgeVertex, DrillModel>();
        Dictionary<CHalfEdgeVertex, DrillModel> virtualDrillList = new Dictionary<CHalfEdgeVertex, DrillModel>();
        //执行细分函数
        public void DoSub(List<DrillModel> DrillList, TriMesh trimeshsub, int times, string resultFileName)
        {
            Dictionary<CHalfEdgeVertex, DrillModel> drillList = new Dictionary<CHalfEdgeVertex, DrillModel>();
            List<DrillModel> newdrillList = new List<DrillModel>();
            for (int i = 0; i < trimeshsub.vertexList.Count; i++)
            {
                for (int j = 0; j < DrillList.Count; j++)
                {
                    if (Math.Round(DrillList[j].X, 3) == trimeshsub.vertexList[i].x)
                    {
                        if (Math.Round(DrillList[j].Y, 3) == trimeshsub.vertexList[i].y)
                        {
                            if (Math.Round(DrillList[j].H, 3) == trimeshsub.vertexList[i].z)
                            {
                                newdrillList.Add(DrillList[j]);

                            }
                        }
                    }
                }

            }

            CHalfEdgeSolid mhalfEdgeSolid = new CHalfEdgeSolid();

            string tempId = "";
            int tempCount = 0;
            for (int i = 0; i < newdrillList.Count; i++)
            {
                CHalfEdgeVertex hVertex = new CHalfEdgeVertex();
                hVertex.x = newdrillList[i].X;
                hVertex.y = newdrillList[i].Y;
                hVertex.z = newdrillList[i].H;
                tempId = newdrillList[i].Id;
                //cout<<tempId<<" "<<tempx<<" "<<tempy<<" "<<tempz<<" "<<tempCount<<endl;

                //添加Solid顶点
                mhalfEdgeSolid.AddVertex(i, hVertex.x, hVertex.y, hVertex.z);

                //构建Vertex到DrillModel的映射
                DrillModel d = new DrillModel();
                d.Id = tempId;
                d.X = hVertex.x;
                d.Y = hVertex.y;
                d.H = hVertex.z;

                for (int j = 0; j < newdrillList[i].StratumList.Count; j++)
                {
                    StratumModel s = new StratumModel(StratumType.Virtual);
                    s.LithId = newdrillList[i].StratumList[j].LithId;
                    s.ZUp = newdrillList[i].StratumList[j].ZUp;
                    s.ZDown = newdrillList[i].StratumList[j].ZDown;
                    s.Height = newdrillList[i].StratumList[j].Height;
                    d.StratumList.Add(s);
                }
                drillList[hVertex] = d;
            }

            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            for (int i = 0; i < trimeshsub.triangleList.Count; i++)
            {
                v1 = trimeshsub.triangleList[i].v0;
                v2 = trimeshsub.triangleList[i].v1;
                v3 = trimeshsub.triangleList[i].v2;
                mhalfEdgeSolid.AddTriangle(i, v1, v2, v3);
            }



            //2.对source实体进行自适应蝶形细分
            // CGeoAdaptiveMButterfly mGeoAdMButter = new CGeoAdaptiveMButterfly();
            CHalfEdgeSolid result = subdision(mhalfEdgeSolid, times, drillList, resultFileName);

        }
        //细分函数
        public CHalfEdgeSolid subdision(CHalfEdgeSolid source, int times, Dictionary<CHalfEdgeVertex, DrillModel> drillList, string resultFileName)
        {
            CHalfEdgeSolid result = null;

            this._drillList = drillList;

            //Method 2
            source = copySolid(source);
            for (int i = 0; i < times; i++)
            {
                currentSubTime = i;

                result = dosub(source);


                //ORIGINAL LINE: delete source;
                source = null;

                source = copySolid(result);

            }

            if (result != null)
            {
                //输出细分结果
                reportResult(result, resultFileName);

                return result;


                //ORIGINAL LINE: delete source;
                source = null;
            }
            else
            { //没有经过细分，不用输出结果
                return source;
            }


        }
        //输出细分结果
        private bool reportResult(CHalfEdgeSolid result, string resultFileName)
        {
            StreamWriter subVDStreamWriter = new StreamWriter(resultFileName);

            if (subVDStreamWriter == null)
            {
                return false;
            }
            //输出点数目和三角形的数目
            subVDStreamWriter.WriteLine(result.vertexMapByIdList.Count + "+" + result.cellList.Count);

            //输出三角网几何信息
            DrillModel d = new DrillModel();
            foreach (var vItera in result.vertexMapByIdList)
            {
                CHalfEdgeVertex vKey = new CHalfEdgeVertex();
                vKey.x = vItera.Value.x;
                vKey.y = vItera.Value.y;
                vKey.z = vItera.Value.z;
                //确保存在与该键值对应的钻孔
                //assert(_drillList.count(vKey) > 0);
                foreach (var i in _drillList)
                {
                    if (i.Key.x == vKey.x)
                    {
                        if (i.Key.y == vKey.y)
                        {
                            if (i.Key.z == vKey.z)
                            {
                                d = i.Value;
                            }
                        }
                    }
                }
                // DrillModel d = _drillList[vKey];

                //输出钻孔基本信息
                // subVDStreamWriter.WriteLine(d.id);

                string line;
                line = string.Format("{0}+{1}+{2}", vKey.x, vKey.y, vKey.z);
                // subVDStreamWriter.WriteLine(line);
                subVDStreamWriter.WriteLine(d.Id + "+" + line + "+" + d.StratumList.Count);

                //输出地层信息
                for (int i = 0; i < d.StratumList.Count(); ++i)
                {
                    StratumModel s = d.StratumList[i];
                    subVDStreamWriter.WriteLine(s.LithId + "+" + s.ZUp + "+" + s.ZDown + "+" + s.Height);
                }
            }

            //输出三角形顶点索引

            foreach (var tItera in result.cellList)
            {
                subVDStreamWriter.WriteLine(tItera.edge.endPoint.id + " " + tItera.edge.next.endPoint.id + " " + tItera.edge.next.next.endPoint.id);
            }
            subVDStreamWriter.Close();
            return true;
        }
        //复制半边结构实体
        private CHalfEdgeSolid copySolid(CHalfEdgeSolid source)
        {
            CHalfEdgeSolid result = new CHalfEdgeSolid();
            if (source == null)
            {
                return null;
            }
            // TODO

            foreach (var vIter in source.vertexMapByIdList)
            {
                result.AddVertex(vIter.Key, vIter.Value.x, vIter.Value.y, vIter.Value.z);
            }


            foreach (var tIter in source.cellList)
            {
                result.AddTriangle(tIter.id, tIter.edge.endPoint.id, tIter.edge.next.endPoint.id, tIter.edge.next.next.endPoint.id);
            }
            return result;

        }

        public CHalfEdgeSolid dosub(CHalfEdgeSolid source)
        {
            if (source == null)
            {
                return null;
            }
            this.everageFlatness = 0.0;

            CHalfEdgeSolid result = new CHalfEdgeSolid();
            foreach (var iter in source.cellList)
            {
                CHalfEdgeCell cell = iter;
                Vector3D cNormal;
                Vector3D acNormal0;
                Vector3D acNormal1;
                Vector3D acNormal2;
                double flatness = 0.0;

                //cell
                if (cellNormals.ContainsKey(cell) == false) //1.The normal of this cell is not calculated
                {
                    cNormal = calcTriangleNormal2(cell);

                    cellNormals[cell] = cNormal;
                }
                else
                {
                    cNormal = cellNormals[cell];
                }

                //adjoining cell0 normal
                if (cell.edge.pair != null)
                {
                    CHalfEdgeCell ac0 = cell.edge.pair.cell;
                    //ac0
                    if (cellNormals.ContainsKey(ac0) == false)
                    {
                        acNormal0 = calcTriangleNormal2(ac0);

                        cellNormals[ac0] = acNormal0;
                    }
                    else
                    {
                        acNormal0 = cellNormals[ac0];
                    }

                    //calculate the angle between cNormal and acNormal0
                    double cac0Angle = calcAngleOf2Vectors(cNormal, acNormal0);

                    if (flatness < cac0Angle)
                    {
                        flatness = cac0Angle;
                    }
                }

                //adjoining cell1 normal
                if (cell.edge.next.pair != null)
                {
                    CHalfEdgeCell ac1 = cell.edge.next.pair.cell;
                    //ac1
                    if (cellNormals.ContainsKey(ac1) == false)
                    {
                        acNormal1 = calcTriangleNormal2(ac1);

                        cellNormals[ac1] = acNormal1;
                    }
                    else
                    {
                        acNormal1 = cellNormals[ac1];
                    }

                    //calculate the angle between cNormal and acNormal0
                    double cac1Angle = calcAngleOf2Vectors(cNormal, acNormal1);

                    if (flatness < cac1Angle)
                    {
                        flatness = cac1Angle;
                    }
                }

                //adjoining cell2 normal
                if (cell.edge.next.next.pair != null)
                {
                    CHalfEdgeCell ac2 = cell.edge.next.next.pair.cell;
                    //ac2
                    if (cellNormals.ContainsKey(ac2) == false)
                    {
                        acNormal2 = calcTriangleNormal2(ac2);

                        cellNormals[ac2] = acNormal2;
                    }
                    else
                    {
                        acNormal2 = cellNormals[ac2];
                    }

                    //calculate the angle between cNormal and acNormal0
                    double cac2Angle = calcAngleOf2Vectors(cNormal, acNormal2);

                    if (flatness < cac2Angle)
                    {
                        flatness = cac2Angle;
                    }
                }

                cellFlatnesses[cell] = flatness;
                everageFlatness += flatness;

            }
            this.everageFlatness = this.everageFlatness / (int)source.cellList.Count();
            //TODO:need to modify
            this.subThreshold = this.everageFlatness * 0.3;
            foreach (var tIter in source.cellList)
            {
                CHalfEdgeCell cell = tIter;

                double flatness = cellFlatnesses[cell];

                if (flatness < this.subThreshold)
                { //this face is flat, and does not need to be subdivided.
                    continue;
                }

                //Temp code: if the triangle is crease/boundary, do not do refinement. 
                if (isCreaseCell(cell))
                {
                    continue;
                }

                //////////////////////////////////////////////////////////////////////////
                //the face is needed to be subdivided

                // 求三角形三个边的细分边顶点
                CHalfEdgeVertex ev0 = null;
                CHalfEdgeVertex ev1 = null;
                CHalfEdgeVertex ev2 = null;

                //cell.edge
                if (edgeMap2Subvertex.ContainsKey(cell.edge) == false) //1.该边的边顶点不存在
                {
                    ev0 = this.getSubVertex(cell.edge); //2.计算边顶点

                    edgeMap2Subvertex[cell.edge] = ev0; //3.将该边顶点添加到map中,键为该边

                    if (cell.edge.pair != null) //4.如果该边的对边存在
                    {
                        //cell.edge.pair
                        if (edgeMap2Subvertex.ContainsKey(cell.edge.pair) == false) //5.对边的边顶点不存在
                        {
                            edgeMap2Subvertex[cell.edge.pair] = ev0; //6.将该边顶点添加到map中,键为该边
                        }
                    }
                }
                else
                {
                    ev0 = edgeMap2Subvertex[cell.edge];
                }

                //cell.edge.next
                if (edgeMap2Subvertex.ContainsKey(cell.edge.next) == false)
                {
                    ev1 = this.getSubVertex(cell.edge.next);

                    edgeMap2Subvertex[cell.edge.next] = ev1;

                    if (cell.edge.next.pair != null)
                    {
                        //cell.edge.next.pair
                        if (edgeMap2Subvertex.ContainsKey(cell.edge.next.pair) == false)
                        {
                            edgeMap2Subvertex[cell.edge.next.pair] = ev1;
                        }
                    }
                }
                else
                {
                    ev1 = edgeMap2Subvertex[cell.edge.next];
                }

                //cell.edge.next.next
                if (edgeMap2Subvertex.ContainsKey(cell.edge.next.next) == false)
                {
                    ev2 = this.getSubVertex(cell.edge.next.next);

                    edgeMap2Subvertex[cell.edge.next.next] = ev2;

                    if (cell.edge.next.next.pair != null)
                    {
                        //cell.edge.next.next.pair
                        if (edgeMap2Subvertex.ContainsKey(cell.edge.next.next.pair) == false)
                        {
                            edgeMap2Subvertex[cell.edge.next.next.pair] = ev2;
                        }
                    }
                }
                else
                {
                    ev2 = edgeMap2Subvertex[cell.edge.next.next];
                }
            }
            int edgeVertexCount;

            List<CHalfEdgeEdge> edgeList = new List<CHalfEdgeEdge>();
            foreach (var iter in source.cellList)
            {
                CHalfEdgeCell cell = iter;
                CHalfEdgeEdge e0 = cell.edge;
                CHalfEdgeEdge e1 = cell.edge.next;
                CHalfEdgeEdge e2 = cell.edge.next.next;

                edgeVertexCount = 0;
                edgeList.Clear();
                //e0
                if (edgeMap2Subvertex.ContainsKey(e0) == true)
                {
                    edgeVertexCount++;

                    edgeList.Add(e0);
                }
                //e1
                if (edgeMap2Subvertex.ContainsKey(e1) == true)
                {
                    edgeVertexCount++;

                    edgeList.Add(e1);
                }
                //e2
                if (edgeMap2Subvertex.ContainsKey(e2) == true)
                {
                    edgeVertexCount++;

                    edgeList.Add(e2);
                }
                // result.AddTriangle(e0.endPoint.x, e0.endPoint.y, e0.endPoint.z, e1.endPoint.x, e1.endPoint.y, e1.endPoint.z, e2.endPoint.x, e2.endPoint.y, e2.endPoint.z);

                //构建新三角网格
                if (edgeVertexCount == 0)
                { //添加原三角形的顶点到新实体中
                    result.AddTriangle(e0.endPoint.x, e0.endPoint.y, e0.endPoint.z, e1.endPoint.x, e1.endPoint.y, e1.endPoint.z, e2.endPoint.x, e2.endPoint.y, e2.endPoint.z);
                }
                else if (edgeVertexCount == 1)
                { //添加新三角形的顶点到新实体中
                    try
                    {
                        result.AddTriangle(edgeList[0].endPoint.x, edgeList[0].endPoint.y, edgeList[0].endPoint.z, edgeList[0].next.endPoint.x, edgeList[0].next.endPoint.y, edgeList[0].next.endPoint.z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);
                    }
                    catch (NullReferenceException E)
                    {
                        return null;
                    }

                    result.AddTriangle(edgeList[0].next.endPoint.x, edgeList[0].next.endPoint.y, edgeList[0].next.endPoint.z, edgeList[0].next.next.endPoint.x, edgeList[0].next.next.endPoint.y, edgeList[0].next.next.endPoint.z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);

                }
                else if (edgeVertexCount == 2)
                { //构造生成三个新的三角形
                    if (edgeList[0].next == edgeList[1])
                    {
                        result.AddTriangle(edgeList[0].endPoint.x, edgeList[0].endPoint.y, edgeList[0].endPoint.z, edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);

                        result.AddTriangle(edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z, edgeList[1].endPoint.x, edgeList[1].endPoint.y, edgeList[1].endPoint.z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);

                        result.AddTriangle(edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z, edgeList[1].endPoint.x, edgeList[1].endPoint.y, edgeList[1].endPoint.z, edgeList[1].next.endPoint.x, edgeList[1].next.endPoint.y, edgeList[1].next.endPoint.z);

                    }
                    else
                    {
                        result.AddTriangle(edgeList[0].endPoint.x, edgeList[0].endPoint.y, edgeList[0].endPoint.z, edgeList[0].next.endPoint.x, edgeList[0].next.endPoint.y, edgeList[0].next.endPoint.z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);

                        result.AddTriangle(edgeList[0].next.endPoint.x, edgeList[0].next.endPoint.y, edgeList[0].next.endPoint.z, edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);

                        result.AddTriangle(edgeList[0].next.next.endPoint.x, edgeList[0].next.next.endPoint.y, edgeList[0].next.next.endPoint.z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z, edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z);
                    }
                }
                else if (edgeVertexCount == 3)
                { //一分四生成四个新的三角形
                    try
                    {
                        result.AddTriangle(edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z, edgeList[0].endPoint.x, edgeList[0].endPoint.y, edgeList[0].endPoint.z, edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z);
                    }
                    catch (NullReferenceException E)
                    {
                        return null;
                    }

                    result.AddTriangle(edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z, edgeList[0].next.endPoint.x, edgeList[0].next.endPoint.y, edgeList[0].next.endPoint.z, edgeMap2Subvertex[edgeList[2]].x, edgeMap2Subvertex[edgeList[2]].y, edgeMap2Subvertex[edgeList[2]].z);

                    result.AddTriangle(edgeMap2Subvertex[edgeList[2]].x, edgeMap2Subvertex[edgeList[2]].y, edgeMap2Subvertex[edgeList[2]].z, edgeList[0].next.next.endPoint.x, edgeList[0].next.next.endPoint.y, edgeList[0].next.next.endPoint.z, edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z);

                    result.AddTriangle(edgeMap2Subvertex[edgeList[0]].x, edgeMap2Subvertex[edgeList[0]].y, edgeMap2Subvertex[edgeList[0]].z, edgeMap2Subvertex[edgeList[1]].x, edgeMap2Subvertex[edgeList[1]].y, edgeMap2Subvertex[edgeList[1]].z, edgeMap2Subvertex[edgeList[2]].x, edgeMap2Subvertex[edgeList[2]].y, edgeMap2Subvertex[edgeList[2]].z);
                }

            }
            if ((int)virtualDrillList.Count > 0)
            {
                foreach (var t in virtualDrillList)
                {

                    _drillList.Add(t.Key, t.Value);
                }

            }
            virtualDrillList.Clear();
            List<CHalfEdgeVertex> vSet = new List<CHalfEdgeVertex>();



            foreach (var mIter in edgeMap2Subvertex)
            {
                CHalfEdgeVertex v = mIter.Value;
                vSet.Add(v);
            }

            foreach (var iter in vSet)
            {
                CHalfEdgeVertex v = iter;

                //ORIGINAL LINE: delete v;
                v = null;
            }
            //6.3 注销
            edgeMap2Subvertex.Clear();
            vSet.Clear();

            return result;
        }
        //计算三角形法向量
        public Vector3D calcTriangleNormal(double x0, double y0, double z0,
                                                double x1, double y1, double z1,
                                                double x2, double y2, double z2)
        {
            Vector3D AB = new Vector3D();
            Vector3D AC = new Vector3D();
            AB.x = x1 - x0;
            AB.y = y1 - y0;
            AB.z = z1 - z0;

            AC.x = x2 - x0;
            AC.y = y2 - y0;
            AC.z = z2 - z0;
            Vector3D TriNormal = new Vector3D();
            TriNormal.x = AB.y * AC.z - AB.z * AC.y;
            TriNormal.y = AB.z * AC.x - AB.x * AC.z;
            TriNormal.z = AB.x * AC.y - AB.y * AC.x;

            return TriNormal;
        }
        //计算三角形法向量2
        public Vector3D calcTriangleNormal2(CHalfEdgeCell cell)
        {
            CHalfEdgeVertex v0 = cell.edge.endPoint;
            CHalfEdgeVertex v1 = cell.edge.next.endPoint;
            CHalfEdgeVertex v2 = cell.edge.next.next.endPoint;
            return calcTriangleNormal(v0.x, v0.y, v0.z, v1.x, v1.y, v1.z, v2.x, v2.y, v2.z);
        }
        //计算两个向量间的夹角
        public double calcAngleOf2Vectors(Vector3D A, Vector3D B)
        {
            double ADotB = A.x * B.x + A.y * B.y + A.z * B.z;

            //2.计算 A和B的模
            double modA = Math.Sqrt(A.x * A.x + A.y * A.y + A.z * A.z);
            double modB = Math.Sqrt(B.x * B.x + B.y * B.y + B.z * B.z);

            //3.计算夹角
            double costht = ADotB / (modA * modB);

            // 设置精度值
            string lv_value;
            lv_value = string.Format("{0:G5}", costht);
            costht = Convert.ToDouble(lv_value);
            return Math.Acos(costht);
        }
        //判断三角形是否为折痕/边界三角形
        public bool isCreaseCell(CHalfEdgeCell cell)
        {
            CHalfEdgeEdge edge = cell.edge;

            if (edge.pair == null)
            {
                return true;
            }

            else if (edge.next.pair == null)
            {
                return true;
            }

            else if (edge.next.next.pair == null)
            {
                return true;
            }
            //是否为折痕三角形，即判断两边的夹角是否小于某一值

            return false;
        }
        //获取某条边的顶点
        public CHalfEdgeVertex getSubVertex(CHalfEdgeEdge edge)
        {
            CHalfEdgeVertex subVertex = null;

            DrillModel vDrill = null;
            if (edge.pair == null) //boundary edge
            {
                //boundary edge Mask
                subVertex = boundaryBoundaryMask(edge, out vDrill);
            }
            else
            {
                int v0Degree;
                int v1Degree;

                //顶点v0,v1是否为内部点的标识
                bool isInnerVertex_v0 = true;
                bool isInnerVertex_v1 = true;

                //If v0 or v1 is crease vertex.calculate the boundary 
                //vertex number i in the ring,when counted from the boundary.
                int bvni_v0 = 0;
                int bvni_v1 = 0;
                //neighboring vertexes of v0(v0=edge->startPoint)
                List<CHalfEdgeVertex> neighVersOfV0;
                neighVersOfV0 = this.calc1NeighborVers(edge, out isInnerVertex_v0, ref bvni_v0);

                //neighboring vertexes of v1(v1=edge->endPoint=edge->pair->startPoint)
                List<CHalfEdgeVertex> neighVersOfV1;

                neighVersOfV1 = this.calc1NeighborVers(edge.pair, out isInnerVertex_v1, ref bvni_v1);
                if (neighVersOfV0 == null || neighVersOfV1 == null)
                {
                    return null;
                }
                v0Degree = (int)neighVersOfV0.Count;
                v1Degree = (int)neighVersOfV1.Count;

                if (isInnerVertex_v0 == true && isInnerVertex_v1 == true)
                {
                    if (v0Degree == 6 && v1Degree == 6) //边的两个端点都是规则点
                    {
                        subVertex = reg_regMask(edge, neighVersOfV0, neighVersOfV1, out vDrill);
                    }
                    else if (v0Degree != 6 && v1Degree != 6)
                    {
                        CHalfEdgeVertex v1TMP = null;
                        CHalfEdgeVertex v2TMP = null;

                        DrillModel d1TMP = null;
                        DrillModel d2TMP = null;
                        if (v0Degree == 3)
                        {
                            if (v1Degree == 3)
                            {
                                v1TMP = extraMask_d3(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_d3(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }
                            else if (v1Degree == 4)
                            {
                                v1TMP = extraMask_d3(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_d4(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }
                            else
                            {
                                v1TMP = extraMask_d3(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_dgte5(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }

                        }
                        else if (v0Degree == 4)
                        {
                            if (v1Degree == 3)
                            {
                                v1TMP = extraMask_d4(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_d3(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }
                            else if (v1Degree == 4)
                            {
                                v1TMP = extraMask_d4(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_d4(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }
                            else
                            {
                                v1TMP = extraMask_d4(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_dgte5(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }

                        }
                        else
                        {
                            if (v1Degree == 3)
                            {
                                v1TMP = extraMask_dgte5(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_d3(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);
                            }
                            else if (v1Degree == 4)
                            {
                                v1TMP = extraMask_dgte5(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_d4(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);

                            }
                            else
                            {
                                v1TMP = extraMask_dgte5(edge, neighVersOfV0, out d1TMP);

                                v2TMP = extraMask_dgte5(edge.pair, neighVersOfV1, out d2TMP);

                                subVertex = calc2VerticesAverage(v1TMP, v2TMP, d1TMP, d2TMP, out vDrill);

                            }

                        }
                    }
                    else if (v0Degree == 6 && v1Degree != 6) //边的起始端点是规则点，另一个为奇异点
                    {
                        if (v1Degree == 3)
                        {
                            subVertex = extraMask_d3(edge.pair, neighVersOfV1, out vDrill);
                        }
                        else if (v1Degree == 4)
                        {
                            subVertex = extraMask_d4(edge.pair, neighVersOfV1, out vDrill);
                        }
                        else
                        {
                            subVertex = extraMask_dgte5(edge.pair, neighVersOfV1, out vDrill);
                        }

                    }
                    else if (v0Degree != 6 && v1Degree == 6)
                    {
                        if (v0Degree == 3)
                        {
                            subVertex = extraMask_d3(edge, neighVersOfV0, out vDrill);
                        }
                        else if (v0Degree == 4)
                        {
                            subVertex = extraMask_d4(edge, neighVersOfV0, out vDrill);
                        }
                        else
                        {
                            subVertex = extraMask_dgte5(edge, neighVersOfV0, out vDrill);
                        }

                    }
                }
                else if (isInnerVertex_v0 == true && isInnerVertex_v1 == false) //v0--interior,v1--crease
                {
                    if (v0Degree == 6) //regular interior
                    {
                        if (v1Degree == 4) //regular interior--regular crease
                        {
                            //interior - crease rule
                            subVertex = interCreaseRule(edge.pair, neighVersOfV1, bvni_v1, neighVersOfV0, out vDrill);

                        }
                        else //regular interior--extraordinary crease
                        {
                            //crease extraordinary Mask
                            subVertex = extraCreaseMask(edge.pair, neighVersOfV1, bvni_v1, out vDrill);
                        }

                    }
                    else //extraordinary interior
                    {
                        if (v1Degree == 4) //extraordinary interior--regular crease
                        {
                            //interior extraordinary Mask
                            if (v0Degree == 3)
                            {
                                subVertex = extraMask_d3(edge, neighVersOfV0, out vDrill);
                            }
                            else if (v0Degree == 4)
                            {
                                subVertex = extraMask_d4(edge, neighVersOfV0, out vDrill);
                            }
                            else
                            {
                                subVertex = extraMask_dgte5(edge, neighVersOfV0, out vDrill);
                            }
                        }
                        else //extraordinary interior--extraordinary crease
                        { //average two extraordinary rules

                            //interior extraordinary Mask
                            CHalfEdgeVertex extraV0 = null;
                            DrillModel interEtraDTMP = null;
                            if (v0Degree == 3)
                            {
                                extraV0 = extraMask_d3(edge, neighVersOfV0, out interEtraDTMP);
                            }
                            else if (v0Degree == 4)
                            {
                                extraV0 = extraMask_d4(edge, neighVersOfV0, out interEtraDTMP);
                            }
                            else
                            {
                                extraV0 = extraMask_dgte5(edge, neighVersOfV0, out interEtraDTMP);
                            }

                            DrillModel creaseExtraDTMP = null;
                            CHalfEdgeVertex creaseV1 = extraCreaseMask(edge, neighVersOfV1, bvni_v1, out creaseExtraDTMP);

                            subVertex = calc2VerticesAverage(extraV0, creaseV1, interEtraDTMP, creaseExtraDTMP, out vDrill);

                        }
                    }
                }
                else if (isInnerVertex_v0 == false && isInnerVertex_v1 == true) //v0--crease,v1--interior
                {
                    if (v0Degree == 4) //regular crease
                    {
                        if (v1Degree == 6) //regular crease--regular interior
                        {
                            //interior - crease rule
                            subVertex = interCreaseRule(edge, neighVersOfV0, bvni_v0, neighVersOfV1, out vDrill);

                        }
                        else //regular crease--extraordinary interior
                        {
                            // interior extraordinary Mask
                            if (v1Degree == 3)
                            {
                                subVertex = extraMask_d3(edge.pair, neighVersOfV1, out vDrill);
                            }
                            else if (v1Degree == 4)
                            {
                                subVertex = extraMask_d4(edge.pair, neighVersOfV1, out vDrill);
                            }
                            else
                            {
                                subVertex = extraMask_dgte5(edge.pair, neighVersOfV1, out vDrill);
                            }

                        }
                    }
                    else //extraordinary crease
                    {
                        if (v1Degree == 6) //extraordinary crease--regular interior
                        {
                            //crease extraordinary Mask
                            subVertex = extraCreaseMask(edge, neighVersOfV0, bvni_v0, out vDrill);
                        }
                        else //extraordinary crease--extraordinary interior
                        {
                            //average two extraordinary rules
                            DrillModel creaseExtraDTMP = null;
                            CHalfEdgeVertex creaseExtraV = extraCreaseMask(edge, neighVersOfV0, bvni_v0, out creaseExtraDTMP);

                            CHalfEdgeVertex interiorExtraV = null;
                            DrillModel interExtraDTMP = null;
                            if (v1Degree == 3)
                            {
                                interiorExtraV = extraMask_d3(edge.pair, neighVersOfV1, out interExtraDTMP);
                            }
                            else if (v1Degree == 4)
                            {
                                interiorExtraV = extraMask_d4(edge.pair, neighVersOfV1, out interExtraDTMP);
                            }
                            else
                            {
                                interiorExtraV = extraMask_dgte5(edge.pair, neighVersOfV1, out interExtraDTMP);
                            }

                            subVertex = calc2VerticesAverage(creaseExtraV, interiorExtraV, creaseExtraDTMP, interExtraDTMP, out vDrill);

                        }
                    }
                }
                else //v0 and v1 are both crease
                {
                    if (v0Degree == 4)//regular crease
                    {
                        if (v1Degree == 4)//regular crease--regular crease
                        {
                            subVertex = creaseCreaseRule(edge, neighVersOfV0, bvni_v0, neighVersOfV1, bvni_v1, out vDrill);
                        }
                        else//regular crease--extraordinary crease
                        {
                            //Crease extraordinary Mask
                            subVertex = extraCreaseMask(edge.pair, neighVersOfV1, bvni_v1, out vDrill);
                        }

                    }
                    else //extraordinary crease
                    {
                        if (v1Degree == 4)//extraordinary crease--regular crease
                        {
                            //Crease extraordinary Mask
                            subVertex = extraCreaseMask(edge, neighVersOfV0, bvni_v0, out vDrill);
                        }
                        else//extraordinary crease--extraordinary crease
                        {
                            //average two extraordinary crease rules
                            DrillModel extraTMPD0 = null;
                            DrillModel extraTMPD1 = null;
                            CHalfEdgeVertex extraCreaseV0 = extraCreaseMask(edge, neighVersOfV0, bvni_v0, out extraTMPD0);

                            CHalfEdgeVertex extraCreaseV1 = extraCreaseMask(edge.pair, neighVersOfV1, bvni_v1, out extraTMPD1);

                            subVertex = calc2VerticesAverage(extraCreaseV0, extraCreaseV1, extraTMPD0, extraTMPD1, out vDrill);

                        }

                    }

                }
            }
            afterConstructionProcess(subVertex, vDrill);

            return subVertex;
        }
        //对细分虚拟钻孔做构建后处理
        private void afterConstructionProcess(CHalfEdgeVertex v, DrillModel vDrill)
        {
            //assert(vDrill != null);

            //assert((int)vDrill.sList.Count() > 0);

            CHalfEdgeVertex vKey = new CHalfEdgeVertex(); //用于键值
            vKey.x = v.x;
            vKey.y = v.y;
            vKey.z = v.z;

            //设置钻孔几何信息
            vDrill.X = v.x;
            vDrill.Y = v.y;
            vDrill.H = v.z;

            //创建细分钻孔后处理
            for (int i = 0; i < (int)vDrill.StratumList.Count; i++)
            {
                StratumModel vs = vDrill.StratumList[i];

                //处理拓扑错误
                if (vs.ZUp < vs.ZDown)
                {
                    vs.ZDown = vs.ZUp;
                }

                //计算地层底板的埋深
                vs.Height = vDrill.H - vs.ZDown;
            }

            virtualDrillList[vKey] = vDrill;
        }
        //crease-crease rule
        private CHalfEdgeVertex creaseCreaseRule(CHalfEdgeEdge edge, List<CHalfEdgeVertex> neighVers0, int bvni_v0, List<CHalfEdgeVertex> neighVers1, int bvni_v1, out DrillModel vDrill)
        {
            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            //v0
            subVertex.x = 1.0f / 2.0f * edge.findStartVertex().x;
            subVertex.y = 1.0f / 2.0f * edge.findStartVertex().y;
            subVertex.z = 1.0f / 2.0f * edge.findStartVertex().z;

            //assert(vDrill == null);
            vDrill = createVDrill(edge.findStartVertex(), 0.5);
            if (bvni_v0 != bvni_v1) //crease - crease rule 1
            {
                if (bvni_v0 == 1) //i_v0=1,i_v1=2;
                {
                    //s1
                    CHalfEdgeVertex s1 = neighVers0[1];
                    subVertex.x += 1.0f / 2.0f * s1.x;
                    subVertex.y += 1.0f / 2.0f * s1.y;
                    subVertex.z += 1.0f / 2.0f * s1.z;

                    modifyVDrill(s1, 0.5, vDrill);

                    //s2
                    CHalfEdgeVertex s2 = neighVers0[2];
                    subVertex.x += 1.0f / 4.0f * s2.x;
                    subVertex.y += 1.0f / 4.0f * s2.y;
                    subVertex.z += 1.0f / 4.0f * s2.z;

                    modifyVDrill(s2, 1.0f / 4.0f, vDrill);

                    //s3
                    CHalfEdgeVertex s3 = neighVers0[3];
                    subVertex.x -= 1.0f / 8.0f * s3.x;
                    subVertex.y -= 1.0f / 8.0f * s3.y;
                    subVertex.z -= 1.0f / 8.0f * s3.z;

                    modifyVDrill(s3, -1.0f / 8.0f, vDrill);

                    //s4
                    CHalfEdgeVertex s4 = neighVers1[0];
                    subVertex.x -= 1.0f / 8.0f * s4.x;
                    subVertex.y -= 1.0f / 8.0f * s4.y;
                    subVertex.z -= 1.0f / 8.0f * s4.z;

                    modifyVDrill(s4, -1.0f / 8.0f, vDrill);

                }
                else //i_v0=2,i_v1=1;
                {
                    //s1
                    CHalfEdgeVertex s1 = neighVers0[2];
                    subVertex.x += 1.0f / 2.0f * s1.x;
                    subVertex.y += 1.0f / 2.0f * s1.y;
                    subVertex.z += 1.0f / 2.0f * s1.z;

                    modifyVDrill(s1, 1.0f / 2.0f, vDrill);

                    //s2
                    CHalfEdgeVertex s2 = neighVers0[1];
                    subVertex.x += 1.0f / 4.0f * s2.x;
                    subVertex.y += 1.0f / 4.0f * s2.y;
                    subVertex.z += 1.0f / 4.0f * s2.z;

                    modifyVDrill(s2, 1.0f / 4.0f, vDrill);

                    //s3
                    CHalfEdgeVertex s3 = neighVers0[0];
                    subVertex.x -= 1.0f / 8.0f * s3.x;
                    subVertex.y -= 1.0f / 8.0f * s3.y;
                    subVertex.z -= 1.0f / 8.0f * s3.z;

                    modifyVDrill(s3, -1.0f / 8.0f, vDrill);

                    //s4
                    CHalfEdgeVertex s4 = neighVers1[3];
                    subVertex.x -= 1.0f / 8.0f * s4.x;
                    subVertex.y -= 1.0f / 8.0f * s4.y;
                    subVertex.z -= 1.0f / 8.0f * s4.z;

                    modifyVDrill(s4, -1.0f / 8.0f, vDrill);
                }
            }
            else //crease - crease rule 2
            {
                //v1
                subVertex.x += 1.0f / 2.0f * edge.endPoint.x;
                subVertex.y += 1.0f / 2.0f * edge.endPoint.y;
                subVertex.z += 1.0f / 2.0f * edge.endPoint.z;

                modifyVDrill(edge.endPoint, 1.0f / 2.0f, vDrill);

            }


            return subVertex;
        }
        //extraordinary crease rule
        private CHalfEdgeVertex extraCreaseMask(CHalfEdgeEdge edge, List<CHalfEdgeVertex> neighVers, int i, out DrillModel vDrill)
        {
            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();
            subVertex = midEdgeVertex(edge, out vDrill);

            return subVertex;
        }
        //中点
        private CHalfEdgeVertex midEdgeVertex(CHalfEdgeEdge edge, out DrillModel vDrill)
        {
            CHalfEdgeVertex midVertex = new CHalfEdgeVertex();

            midVertex.x = 1.0f / 2.0f * (edge.findStartVertex().x + edge.endPoint.x);
            midVertex.y = 1.0f / 2.0f * (edge.findStartVertex().y + edge.endPoint.y);
            midVertex.z = 1.0f / 2.0f * (edge.findStartVertex().z + edge.endPoint.z);

            //assert(vDrill == null);
            vDrill = createVDrill(edge.findStartVertex(), 0.5);

            modifyVDrill(edge.endPoint, 0.5, vDrill);

            return midVertex;
        }
        //interior-crease rule
        private CHalfEdgeVertex interCreaseRule(CHalfEdgeEdge edge, List<CHalfEdgeVertex> neighVers0, int bvni_v0, List<CHalfEdgeVertex> neighVers1, out DrillModel vDrill)
        {
            //assert(vDrill == null);

            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            if (bvni_v0 == 1)
            {
                //v0
                subVertex.x = 3.0f / 8.0f * edge.findStartVertex().x;
                subVertex.y = 3.0f / 8.0f * edge.findStartVertex().y;
                subVertex.z = 3.0f / 8.0f * edge.findStartVertex().z;

                vDrill = createVDrill(edge.findStartVertex(), 3.0f / 8.0f);

                //s0
                CHalfEdgeVertex s0 = neighVers0[0];
                subVertex.x += 1.0f / 16.0f * s0.x;
                subVertex.y += 1.0f / 16.0f * s0.y;
                subVertex.z += 1.0f / 16.0f * s0.z;

                modifyVDrill(s0, 1.0f / 16.0f, vDrill);

                //s1
                CHalfEdgeVertex s1 = neighVers0[1];
                subVertex.x += 5.0f / 8.0f * s1.x;
                subVertex.y += 5.0f / 8.0f * s1.y;
                subVertex.z += 5.0f / 8.0f * s1.z;

                modifyVDrill(s1, 5.0f / 8.0f, vDrill);

                //s2
                CHalfEdgeVertex s2 = neighVers0[2];
                subVertex.x += 3.0f / 16.0f * s2.x;
                subVertex.y += 3.0f / 16.0f * s2.y;
                subVertex.z += 3.0f / 16.0f * s2.z;

                modifyVDrill(s2, 3.0f / 16.0f, vDrill);

                //s3
                CHalfEdgeVertex s3 = neighVers0[3];
                subVertex.x -= 1.0f / 16.0f * s3.x;
                subVertex.y -= 1.0f / 16.0f * s3.y;
                subVertex.z -= 1.0f / 16.0f * s3.z;

                modifyVDrill(s3, -1.0f / 16.0f, vDrill);

                //s4
                CHalfEdgeVertex s4 = neighVers1[2];
                subVertex.x -= 1.0f / 8.0f * s4.x;
                subVertex.y -= 1.0f / 8.0f * s4.y;
                subVertex.z -= 1.0f / 8.0f * s4.z;

                modifyVDrill(s4, -1.0f / 8.0f, vDrill);

                //s5
                CHalfEdgeVertex s5 = neighVers1[5];
                subVertex.x -= 1.0f / 16.0f * s5.x;
                subVertex.y -= 1.0f / 16.0f * s5.y;
                subVertex.z -= 1.0f / 16.0f * s5.z;

                modifyVDrill(s5, -1.0f / 16.0f, vDrill);

            }
            else //bvni_v0 == 2
            {
                //v0
                subVertex.x = 3.0f / 8.0f * edge.findStartVertex().x;
                subVertex.y = 3.0f / 8.0f * edge.findStartVertex().y;
                subVertex.z = 3.0f / 8.0f * edge.findStartVertex().z;

                vDrill = createVDrill(edge.findStartVertex(), 3.0f / 8.0f);

                //s3
                CHalfEdgeVertex s3 = neighVers0[0];
                subVertex.x -= 1.0f / 16.0f * s3.x;
                subVertex.y -= 1.0f / 16.0f * s3.y;
                subVertex.z -= 1.0f / 16.0f * s3.z;

                modifyVDrill(s3, -1.0f / 16.0f, vDrill);

                //s2
                CHalfEdgeVertex s2 = neighVers0[1];
                subVertex.x += 3.0f / 16.0f * s2.x;
                subVertex.y += 3.0f / 16.0f * s2.y;
                subVertex.z += 3.0f / 16.0f * s2.z;

                modifyVDrill(s2, 3.0f / 16.0f, vDrill);

                //s1
                CHalfEdgeVertex s1 = neighVers0[2];
                subVertex.x += 5.0f / 8.0f * s1.x;
                subVertex.y += 5.0f / 8.0f * s1.y;
                subVertex.z += 5.0f / 8.0f * s1.z;

                modifyVDrill(s1, 5.0f / 8.0f, vDrill);

                //s0
                CHalfEdgeVertex s0 = neighVers0[3];
                subVertex.x += 1.0f / 16.0f * s0.x;
                subVertex.y += 1.0f / 16.0f * s0.y;
                subVertex.z += 1.0f / 16.0f * s0.z;

                modifyVDrill(s0, 1.0f / 16.0f, vDrill);

                //s4
                CHalfEdgeVertex s4 = neighVers1[4];
                subVertex.x -= 1.0f / 8.0f * s4.x;
                subVertex.y -= 1.0f / 8.0f * s4.y;
                subVertex.z -= 1.0f / 8.0f * s4.z;

                modifyVDrill(s4, -1.0f / 8.0f, vDrill);

                //s5
                CHalfEdgeVertex s5 = neighVers1[2];
                subVertex.x -= 1.0f / 16.0f * s5.x;
                subVertex.y -= 1.0f / 16.0f * s5.y;
                subVertex.z -= 1.0f / 16.0f * s5.z;

                modifyVDrill(s5, -1.0f / 16.0f, vDrill);
            }

            return subVertex;
        }

        private CHalfEdgeVertex extraMask_dgte5(CHalfEdgeEdge edge, List<CHalfEdgeVertex> neighVers, out DrillModel vDrill)
        {
            vDrill = null;

            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            //Coefficients
            float ci;

            //sum of ci
            float sumci = 0;

            // number of neighborhood vertexes
            int k = (int)neighVers.Count();
            for (int i = 0; i < k; i++)
            {
                CHalfEdgeVertex vi = neighVers[i];

                ci = (float)(1.0f / (float)k * (1.0f / 4.0f + Math.Cos(2.0 * i * PI / (float)k) + 1.0f / 2.0f * Math.Cos(4.0 * i * PI / (float)k)));

                subVertex.x += ci * vi.x;
                subVertex.y += ci * vi.y;
                subVertex.z += ci * vi.z;

                sumci += ci;

                if (0 == i)
                {
                    vDrill = createVDrill(vi, ci);
                }
                else
                {
                    modifyVDrill(vi, ci, vDrill);
                }

            }

            subVertex.x += (1 - sumci) * edge.findStartVertex().x;
            subVertex.y += (1 - sumci) * edge.findStartVertex().y;
            subVertex.z += (1 - sumci) * edge.findStartVertex().z;

            modifyVDrill(edge.findStartVertex(), 1 - sumci, vDrill);

            return subVertex;
        }

        private CHalfEdgeVertex extraMask_d4(CHalfEdgeEdge edge, List<CHalfEdgeVertex> neighVers, out DrillModel vDrill)
        {
            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            //v0
            subVertex.x = 3.0f / 4.0f * edge.findStartVertex().x;
            subVertex.y = 3.0f / 4.0f * edge.findStartVertex().y;
            subVertex.z = 3.0f / 4.0f * edge.findStartVertex().z;

            //assert(vDrill == null);
            vDrill = createVDrill(edge.findStartVertex(), 3.0f / 4.0f);

            //s0
            CHalfEdgeVertex s0 = neighVers[0];
            subVertex.x += 3.0f / 8.0f * s0.x;
            subVertex.y += 3.0f / 8.0f * s0.y;
            subVertex.z += 3.0f / 8.0f * s0.z;

            modifyVDrill(s0, 3.0f / 8.0f, vDrill);

            //s1,3 = 0

            //s2
            CHalfEdgeVertex s2 = neighVers[2];
            subVertex.x -= 1.0f / 8.0f * s2.x;
            subVertex.y -= 1.0f / 8.0f * s2.y;
            subVertex.z -= 1.0f / 8.0f * s2.z;

            modifyVDrill(s0, -1.0f / 8.0f, vDrill);

            return subVertex;
        }

        private CHalfEdgeVertex calc2VerticesAverage(CHalfEdgeVertex v1, CHalfEdgeVertex v2, DrillModel d1, DrillModel d2, out DrillModel vDrill)
        {
            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            subVertex.x = 1.0f / 2.0f * (v1.x + v2.x);
            subVertex.y = 1.0f / 2.0f * (v1.y + v2.y);
            subVertex.z = 1.0f / 2.0f * (v1.z + v2.z);

            //保证钻孔是经过标准化处理--即：地层已经补全
            //assert(d1.sList.Count() == d2.sList.Count());

            //assert(vDrill == null);

            vDrill = new DrillModel();

            //To set the basic information of subVirDrill

            string strId = "";
            strId = "subdivisionVDrill";
            string number1;
            string number2;
            number1 = string.Format("{0:D}", currentSubTime);

            number2 = string.Format("{0:D}", (int)virtualDrillList.Count());

            strId = "subdivisionVDrill" + number1 + "-" + number2;
            vDrill.Id = strId;

            //	(*vDrill)->id = strId;

            for (int i = 0; i < (int)d1.StratumList.Count; i++)
            {
                StratumModel vs = new StratumModel(StratumType.Virtual);

                vs.LithId = i;
                vs.ZUp = 0.5 * (d1.StratumList[i].ZUp + d2.StratumList[i].ZUp);
                vs.ZDown = 0.5 * (d1.StratumList[i].ZDown + d2.StratumList[i].ZDown);

                vDrill.StratumList.Add(vs);
            }



            //ORIGINAL LINE: delete v1;
            v1 = null;

            //ORIGINAL LINE: delete v2;
            v2 = null;

            //ORIGINAL LINE: delete d1;
            d1 = null;

            //ORIGINAL LINE: delete d2;
            d2 = null;

            return subVertex;
        }

        private CHalfEdgeVertex extraMask_d3(CHalfEdgeEdge edge, List<CHalfEdgeVertex> neighVers, out DrillModel vDrill)
        {
            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            //v0
            subVertex.x = 3.0f / 4.0f * edge.findStartVertex().x;
            subVertex.y = 3.0f / 4.0f * edge.findStartVertex().y;
            subVertex.z = 3.0f / 4.0f * edge.findStartVertex().z;

            //assert(vDrill == null);
            vDrill = createVDrill(edge.findStartVertex(), 3.0f / 4.0f);

            //s0
            CHalfEdgeVertex s0 = neighVers[0];
            subVertex.x += 5.0f / 12.0f * s0.x;
            subVertex.y += 5.0f / 12.0f * s0.y;
            subVertex.z += 5.0f / 12.0f * s0.z;

            modifyVDrill(s0, 5.0f / 12.0f, vDrill);

            //s1,3 = 0
            CHalfEdgeVertex s1 = neighVers[1];
            subVertex.x -= 1.0f / 12.0f * s1.x;
            subVertex.y -= 1.0f / 12.0f * s1.y;
            subVertex.z -= 1.0f / 12.0f * s1.z;

            modifyVDrill(s1, -1.0f / 12.0f, vDrill);

            //s2
            CHalfEdgeVertex s2 = neighVers[2];
            subVertex.x -= 1.0f / 12.0f * s2.x;
            subVertex.y -= 1.0f / 12.0f * s2.y;
            subVertex.z -= 1.0f / 12.0f * s2.z;

            modifyVDrill(s2, -1.0f / 12.0f, vDrill);

            return subVertex;
        }
        //由规则点计算新的边顶点
        public CHalfEdgeVertex reg_regMask(CHalfEdgeEdge edge, List<CHalfEdgeVertex> v0Vec, List<CHalfEdgeVertex> v1Vec, out DrillModel vDrill)
        {
            CHalfEdgeVertex p1 = edge.findStartVertex();
            CHalfEdgeVertex p2 = v0Vec[0];
            CHalfEdgeVertex p3 = v0Vec[1];
            CHalfEdgeVertex p4 = v0Vec[5];
            CHalfEdgeVertex p5 = v0Vec[2];
            CHalfEdgeVertex p6 = v0Vec[4];
            CHalfEdgeVertex p7 = v1Vec[2];
            CHalfEdgeVertex p8 = v1Vec[4];



            CHalfEdgeVertex subVertex = by8PointsStencilCalcNewPoint(p1, p2, p3, p4, p5, p6, p7, p8, out vDrill);

            return subVertex;
        }
        float w = (float)(1.0 / 16.0);
        //八个点计算边顶点坐标
        private CHalfEdgeVertex by8PointsStencilCalcNewPoint(CHalfEdgeVertex p1, CHalfEdgeVertex p2, CHalfEdgeVertex p3, CHalfEdgeVertex p4, CHalfEdgeVertex p5, CHalfEdgeVertex p6, CHalfEdgeVertex p7, CHalfEdgeVertex p8, out DrillModel vDrill)
        {
            CHalfEdgeVertex v = new CHalfEdgeVertex();

            v.x = (float)(0.5 * (p1.x + p2.x));
            v.y = (float)(0.5 * (p1.y + p2.y));
            v.z = (float)(0.5 * (p1.z + p2.z));

            //assert(vDrill == null);
            vDrill = createVDrill(p1, 0.5);
            modifyVDrill(p2, 0.5, vDrill);

            if (p3 != null)
            {
                v.x += 2 * w * p3.x;
                v.y += 2 * w * p3.y;
                v.z += 2 * w * p3.z;

                modifyVDrill(p3, 2 * w, vDrill);
            }

            if (p4 != null)
            {
                v.x += 2 * w * p4.x;
                v.y += 2 * w * p4.y;
                v.z += 2 * w * p4.z;

                modifyVDrill(p4, 2 * w, vDrill);
            }

            if (p5 != null)
            {
                v.x -= w * p5.x;
                v.y -= w * p5.y;
                v.z -= w * p5.z;

                modifyVDrill(p5, -1 * w, vDrill);
            }

            if (p6 != null)
            {
                v.x -= w * p6.x;
                v.y -= w * p6.y;
                v.z -= w * p6.z;

                modifyVDrill(p6, -1 * w, vDrill);
            }

            if (p7 != null)
            {
                v.x -= w * p7.x;
                v.y -= w * p7.y;
                v.z -= w * p7.z;

                modifyVDrill(p7, -1 * w, vDrill);
            }

            if (p8 != null)
            {
                v.x -= w * p8.x;
                v.y -= w * p8.y;
                v.z -= w * p8.z;

                modifyVDrill(p8, -1 * w, vDrill);
            }

            return v;
        }
        //修改细分虚拟钻孔信息
        private void modifyVDrill(CHalfEdgeVertex p, double coefficient, DrillModel vDrill)
        {
            //assert(vDrill != null);

            CHalfEdgeVertex vk = new CHalfEdgeVertex();
            vk.x = p.x;
            vk.y = p.y;
            vk.z = p.z;
            DrillModel d = new DrillModel();
            //assert(_drillList.count(vk) > 0);
            foreach (var i in _drillList)
            {
                if (i.Key.x == vk.x)
                {
                    if (i.Key.y == vk.y)
                    {
                        if (i.Key.z == vk.z)
                        {
                            d = i.Value;
                        }
                    }
                }
            }
            // DrillModel d = _drillList[vk];

            //保证vDrill已经初始化
            //assert(d.sList.Count() == vDrill.sList.Count());

            for (int i = 0; i < (int)vDrill.StratumList.Count(); i++)
            {
                StratumModel vs = vDrill.StratumList[i];

                vs.ZUp += d.StratumList[i].ZUp * coefficient;
                vs.ZDown += d.StratumList[i].ZDown * coefficient;
            }
        }

        public List<CHalfEdgeVertex> calc1NeighborVers(CHalfEdgeEdge edge, out bool isInnerVertex, ref int bvni)
        {
            isInnerVertex = false;
            //bvni = 0;
            //get neighboring vertexes of v0(v0=edge->startPoint)
            List<CHalfEdgeVertex> neighVers = new List<CHalfEdgeVertex>();

            neighVers.Add(edge.endPoint); //edge边的结束端点进栈

            CHalfEdgeEdge edgeTMP = edge;
            while (edgeTMP.pair != null)
            {
                edgeTMP = edgeTMP.pair.next;

                //已经构成一个Loop
                if (edgeTMP == edge)
                {
                    isInnerVertex = true;

                    break;
                }
                try
                {
                    neighVers.Add(edgeTMP.endPoint);
                }
                catch (OutOfMemoryException e)
                {
                    return null;
                }
            }

            //crease vertex
            if (isInnerVertex == false) //In this state, only a half ring has been counted
            {
                //boundary vertex number i
                bvni = (int)neighVers.Count() - 1;

                //reverse vector
                neighVers.Reverse();

                //count another half ring
                neighVers.Add(edge.next.endPoint); //edge->next边的结束端点进栈

                CHalfEdgeEdge edgeTMP2 = edge;
                while (edgeTMP2.next.next.pair != null)
                {
                    edgeTMP2 = edgeTMP2.next.next.pair;
                    try
                    {
                        neighVers.Add(edgeTMP2.next.endPoint);
                    }
                    catch (OutOfMemoryException e)
                    {
                        return null;
                    }
                }
            }

            return neighVers;

        }
        //计算边界边顶点，返回虚拟钻孔
        public CHalfEdgeVertex boundaryBoundaryMask(CHalfEdgeEdge edge, out DrillModel vDrill)
        {
            CHalfEdgeVertex subVertex = new CHalfEdgeVertex();

            //v0
            subVertex.x = 9.0f / 16.0f * edge.findStartVertex().x;
            subVertex.y = 9.0f / 16.0f * edge.findStartVertex().y;
            subVertex.z = 9.0f / 16.0f * edge.findStartVertex().z;

            //assert(vDrill == null);
            vDrill = createVDrill(edge.findStartVertex(), 9.0f / 16.0f);

            //v1
            subVertex.x += 9.0f / 16.0f * edge.endPoint.x;
            subVertex.y += 9.0f / 16.0f * edge.endPoint.y;
            subVertex.z += 9.0f / 16.0f * edge.endPoint.z;

            modifyVDrill(edge.endPoint, 9.0f / 16.0f, vDrill);

            //v2
            bool isInnerVertex_v2 = false;
            int bvni_v2 = 0;
            List<CHalfEdgeVertex> neighVers_v2 = calc1NeighborVers(edge, out isInnerVertex_v2, ref bvni_v2);

            CHalfEdgeVertex v2 = neighVers_v2.Last();

            subVertex.x -= 1.0f / 16.0f * v2.x;
            subVertex.y -= 1.0f / 16.0f * v2.y;
            subVertex.z -= 1.0f / 16.0f * v2.z;

            modifyVDrill(v2, -1.0f / 16.0f, vDrill);

            //v3
            bool isInnerVertex_v3 = false;
            int bvni_v3 = 0;
            List<CHalfEdgeVertex> neighVers_v3 = calc1NeighborVers(edge.next, out isInnerVertex_v3, ref bvni_v3);

            CHalfEdgeVertex v3 = neighVers_v3.Last();

            subVertex.x -= 1.0f / 16.0f * v3.x;
            subVertex.y -= 1.0f / 16.0f * v3.y;
            subVertex.z -= 1.0f / 16.0f * v3.z;

            modifyVDrill(v3, -1.0f / 16.0f, vDrill);

            return subVertex;
        }
        int currentSubTime;
        public DrillModel createVDrill(CHalfEdgeVertex vertex, double coefficient)
        {
            DrillModel vDrill = new DrillModel();


            String strId = "";

            string number1 = "";
            string number2 = "";
            number1 = string.Format("{0:D}", currentSubTime);

            number2 = string.Format("{0:D}", (int)virtualDrillList.Count());

            strId = "subdivisionVDrill" + number1 + "-" + number2;
            vDrill.Id = strId;

            //创建键值
            CHalfEdgeVertex vKey = new CHalfEdgeVertex();
            vKey.x = vertex.x;
            vKey.y = vertex.y;
            vKey.z = vertex.z;

            DrillModel d = new DrillModel();
            int t = 0;
            if (_drillList.ContainsKey(vKey) == true)
            {
                t = 1;

            }
            foreach (var i in _drillList)
            {
                if (i.Key.x == vKey.x)
                {
                    if (i.Key.y == vKey.y)
                    {
                        if (i.Key.z == vKey.z)
                        {
                            d = i.Value;
                        }
                    }
                }
            }
            //_drillList.Add(vKey);
            //d = _drillList[vKey];

            for (int i = 0; i < (int)d.StratumList.Count(); i++)
            {
                StratumModel vs = new StratumModel(StratumType.Virtual);

                vs.LithId = i;
                vs.ZUp = coefficient * (d.StratumList[i].ZUp);
                vs.ZDown = coefficient * (d.StratumList[i].ZDown);
                //vs->height = coefficient * (d->sList[i].height);

                vDrill.StratumList.Add(vs);
            }

            return vDrill;
        }



        public class CHalfEdgeCell
        {
            public int id;

            public CHalfEdgeEdge edge; // 该Cell的一条邻接边( 任意一条 )
        }

        public class Vector3D
        {
            public double x;
            public double y;
            public double z;
        }

        public class CHalfEdgeEdge
        {
            public CHalfEdgeVertex endPoint; // 该条半边的终点

            public CHalfEdgeEdge pair; // 该条半边的对边，如果是边界边，则对边为NULL

            public CHalfEdgeCell cell; // 该边的临接面

            public CHalfEdgeEdge next; // 该边的下一条边
            public CHalfEdgeVertex findStartVertex()
            {

                CHalfEdgeVertex stratVertex = new CHalfEdgeVertex();

                CHalfEdgeCell cell = this.cell;


                CHalfEdgeEdge nextEdge = this.next;

                while (nextEdge != this)
                {
                    if (nextEdge.next == this)
                    {
                        stratVertex = nextEdge.endPoint;
                        break;
                    }
                    nextEdge = nextEdge.next;
                }

                return stratVertex;
            }

            public CHalfEdgeEdge()
            {
                this.cell = cell;
                this.endPoint = endPoint;
                this.next = next;
                this.pair = pair;
            }
        }

        public class CHalfEdgeVertex
        {
            public CHalfEdgeVertex()
            {
                float a = 0;
                this.id = 0;
                this.x = this.y = this.z = a;
                this.nx = this.ny = this.nz = a;
                this.edge = edge;
            }
            public CHalfEdgeVertex(int _id, double _x, double _y, double _z)
            {
                this.id = _id;
                this.x = _x;
                this.y = _y;
                this.z = _z;
                this.edge = edge;
            }

            public int id;

            public double x;

            public double y;

            public double z;

            public double nx;

            public double ny;

            public double nz;

            public CHalfEdgeEdge edge { get; set; } // one of the half-edges emantating from the vertex
        }

        public class CHalfEdgeSolid
        {
            public Dictionary<CHalfEdgeVertex, CHalfEdgeVertex> vertexMapByCoordList { get; set; } = new Dictionary<CHalfEdgeVertex, CHalfEdgeVertex>(); // 顶点的坐标排序列表

            public Dictionary<int, CHalfEdgeVertex> vertexMapByIdList { get; set; } = new Dictionary<int, CHalfEdgeVertex>(); // 顶点的ID排序列表

            // 边列表
            public List<CHalfEdgeEdge> edgeList { get; set; } = new List<CHalfEdgeEdge>();

            // 面列表
            public List<CHalfEdgeCell> cellList { get; set; } = new List<CHalfEdgeCell>();

            //点到其发射边的映射-- for AddTrangle("Coordinate")
            private Dictionary<CHalfEdgeVertex, List<CHalfEdgeEdge>> vertexMap2Edges_Coord { get; set; } = new Dictionary<CHalfEdgeVertex, List<CHalfEdgeEdge>>();

            //点到其发射边的映射-- for AddTrangle("Index")
            private Dictionary<CHalfEdgeVertex, List<CHalfEdgeEdge>> vertexMap2Edges_Index { get; set; } = new Dictionary<CHalfEdgeVertex, List<CHalfEdgeEdge>>();
            public CHalfEdgeSolid()
            {

                foreach (var iter in vertexMapByCoordList)
                {
                    CHalfEdgeVertex v = iter.Value;


                    v = null;
                }

                this.vertexMapByCoordList.Clear();
                this.vertexMapByIdList.Clear();

                for (int i = 0; i < edgeList.Count; i++)
                {

                    //ORIGINAL LINE: delete edgeList[i];
                    edgeList[i] = null;
                }
                this.edgeList.Clear();

                for (int i = 0; i < cellList.Count; i++)
                {

                    //ORIGINAL LINE: delete cellList[i];
                    cellList[i] = null;
                }
                this.cellList.Clear();

                this.vertexMap2Edges_Coord.Clear();
                this.vertexMap2Edges_Index.Clear();
            }
            // 添加一个顶点

            public CHalfEdgeVertex AddVertex(double x, double y, double z, out bool isNew)
            {
                CHalfEdgeVertex vertex = new CHalfEdgeVertex();
                vertex.x = x;
                vertex.y = y;
                vertex.z = z;

                if (vertexMapByCoordList.ContainsKey(vertex) == true)
                {
                    isNew = false;
                    return this.vertexMapByCoordList[vertex];

                }
                else
                {
                    // 顶点不存在，则加入该顶点
                    isNew = true;

                    CHalfEdgeVertex pVertex = new CHalfEdgeVertex();
                    pVertex.id = (int)this.vertexMapByCoordList.Count;
                    pVertex.x = x;
                    pVertex.y = y;
                    pVertex.z = z;

                    this.vertexMapByCoordList[vertex] = pVertex;
                    this.vertexMapByIdList[pVertex.id] = pVertex;

                    return pVertex;
                }
            }


            public CHalfEdgeVertex AddVertex(int id, double x, double y, double z)
            {
                CHalfEdgeVertex pVertex = new CHalfEdgeVertex(id, x, y, z);
                // Dictionary<int, CHalfEdgeVertex> vertexMapByIdList = new Dictionary<int, CHalfEdgeVertex>();
                this.vertexMapByIdList[id] = pVertex;

                CHalfEdgeVertex vertex = new CHalfEdgeVertex();
                vertex.x = x;
                vertex.y = y;
                vertex.z = z;
                // Dictionary<CHalfEdgeVertex, CHalfEdgeVertex> vertexMapByCoordList=new Dictionary<CHalfEdgeVertex, CHalfEdgeVertex> ();
                this.vertexMapByCoordList[vertex] = pVertex;

                return pVertex;
            }


            public CHalfEdgeCell AddTriangle(int id, int v1, int v2, int v3)
            {
                CHalfEdgeVertex pV1 = this.vertexMapByIdList[v1];
                CHalfEdgeVertex pV2 = this.vertexMapByIdList[v2];
                CHalfEdgeVertex pV3 = this.vertexMapByIdList[v3];

                // 创建一个新的三角形面片
                CHalfEdgeCell pC1 = new CHalfEdgeCell();
                pC1.id = id;

                this.cellList.Add(pC1);
                // 创建三个新的半边，设置每个半边的终点，next边，和邻接三角形面片
                CHalfEdgeEdge pE1 = new CHalfEdgeEdge();
                CHalfEdgeEdge pE2 = new CHalfEdgeEdge();
                CHalfEdgeEdge pE3 = new CHalfEdgeEdge();

                this.edgeList.Add(pE1);
                this.edgeList.Add(pE2);
                this.edgeList.Add(pE3);


                pE1.endPoint = pV2;
                pE1.next = pE2;
                pE1.cell = pC1;

                pE2.endPoint = pV3;
                pE2.next = pE3;
                pE2.cell = pC1;

                pE3.endPoint = pV1;
                pE3.next = pE1;
                pE3.cell = pC1;

                // 将该面片的邻接边设置为三个新半边中的任意一个
                pC1.edge = pE1;

                // 将每个顶点的出发边设为这三个新创建的半边
                pV1.edge = pE1;

                pV2.edge = pE2;

                pV3.edge = pE3;
                List<CHalfEdgeEdge> pV1E = new List<CHalfEdgeEdge>();
                List<CHalfEdgeEdge> pV2E = new List<CHalfEdgeEdge>();
                List<CHalfEdgeEdge> pV3E = new List<CHalfEdgeEdge>();

                if (vertexMap2Edges_Index.ContainsKey(pV1) == false)
                {
                    vertexMap2Edges_Index.Add(pV1, pV1E);
                    vertexMap2Edges_Index[pV1].Add(pE1);
                }
                else
                {
                    vertexMap2Edges_Index[pV1].Add(pE1);
                }


                //  List<CHalfEdgeEdge> pV2E = new List<CHalfEdgeEdge>();
                if (vertexMap2Edges_Index.ContainsKey(pV2) == false)
                {
                    vertexMap2Edges_Index.Add(pV2, pV2E);
                    vertexMap2Edges_Index[pV2].Add(pE2);
                }
                else
                {
                    vertexMap2Edges_Index[pV2].Add(pE2);
                }


                // List<CHalfEdgeEdge> pV3E = new List<CHalfEdgeEdge>();
                if (vertexMap2Edges_Index.ContainsKey(pV3) == false)
                {
                    vertexMap2Edges_Index.Add(pV3, pV3E);
                    vertexMap2Edges_Index[pV3].Add(pE3);
                }
                else
                {
                    vertexMap2Edges_Index[pV3].Add(pE3);
                }


                //   
                //
                //vertexMap2Edges_Index[pV3].Add(pE3);

                // 寻找对边
                CHalfEdgeEdge pTempEdge = null;
                //List<CHalfEdgeEdge> edgeItera = new List<CHalfEdgeEdge>();
                foreach (CHalfEdgeEdge edgeItera in vertexMap2Edges_Index[pV2])
                {
                    pTempEdge = edgeItera;
                    if (pTempEdge.endPoint == pV1)
                    {
                        pE1.pair = pTempEdge;

                        pTempEdge.pair = pE1;

                        vertexMap2Edges_Index[pV2].Remove(edgeItera);
                        vertexMap2Edges_Index[pV1].Remove(pE1); //将e1从pV1的发出边数组中也删除

                        break;
                    }
                }
                foreach (CHalfEdgeEdge edgeItera in vertexMap2Edges_Index[pV3])
                {
                    pTempEdge = edgeItera;
                    if (pTempEdge.endPoint == pV2)
                    {
                        pE2.pair = pTempEdge;

                        pTempEdge.pair = pE2;

                        vertexMap2Edges_Index[pV3].Remove(edgeItera);
                        vertexMap2Edges_Index[pV2].Remove(pE2); //将e2从pV2的发出边数组中也删除

                        break;
                    }
                }
                foreach (CHalfEdgeEdge edgeItera in vertexMap2Edges_Index[pV1])
                {
                    pTempEdge = edgeItera;
                    if (pTempEdge.endPoint == pV3)
                    {
                        pE3.pair = pTempEdge;

                        pTempEdge.pair = pE3;

                        vertexMap2Edges_Index[pV1].Remove(edgeItera);
                        vertexMap2Edges_Index[pV3].Remove(pE3); //将e1从pV1的发出边数组中也删除

                        break;
                    }
                }

                return pC1;
            }



            public void AddTriangle(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3)
            {
                // 创建一个新的三角形面
                CHalfEdgeCell c1 = new CHalfEdgeCell();
                c1.id = (int)this.cellList.Count;
                this.cellList.Add(c1);

                // 试着创建新的顶点，如果该顶点是新的，则创建，反之返回该已存在的顶点
                bool isV1New = false;
                CHalfEdgeVertex v1 = this.AddVertex(x1, y1, z1, out isV1New);

                bool isV2New = false;
                CHalfEdgeVertex v2 = this.AddVertex(x2, y2, z2, out isV2New);

                bool isV3New = false;
                CHalfEdgeVertex v3 = this.AddVertex(x3, y3, z3, out isV3New);

                // 创建三个新的半边，设置每个半边的终点，next边，和邻接三角形面片
                CHalfEdgeEdge e1 = new CHalfEdgeEdge();
                CHalfEdgeEdge e2 = new CHalfEdgeEdge();
                CHalfEdgeEdge e3 = new CHalfEdgeEdge();

                this.edgeList.Add(e1);
                this.edgeList.Add(e2);
                this.edgeList.Add(e3);

                e1.endPoint = v2;
                e1.next = e2;
                e1.cell = c1;

                e2.endPoint = v3;
                e2.next = e3;
                e2.cell = c1;

                e3.endPoint = v1;
                e3.next = e1;
                e3.cell = c1;

                // 将该面片的邻接边设置为三个新半边中的任意一个
                c1.edge = e1;

                // 将每个顶点的出发边设为这三个新创建的半边
                v1.edge = e1;

                v2.edge = e2;

                v3.edge = e3;
                // pV1E0.Add(e1);
                //vertexMap2Edges_Coord[v1].Add(e1);
                //pV2E0.Add(e2);
                //vertexMap2Edges_Coord[v2].Add(e2);
                //pV3E0.Add(e3);
                //vertexMap2Edges_Coord[v3].Add(e3);
                //List<CHalfEdgeEdge> pV1E = new List<CHalfEdgeEdge>();
                List<CHalfEdgeEdge> pV1E0 = new List<CHalfEdgeEdge>();
                List<CHalfEdgeEdge> pV2E0 = new List<CHalfEdgeEdge>();
                List<CHalfEdgeEdge> pV3E0 = new List<CHalfEdgeEdge>();
                if (vertexMap2Edges_Coord.ContainsKey(v1) == false)
                {
                    vertexMap2Edges_Coord.Add(v1, pV1E0);
                    vertexMap2Edges_Coord[v1].Add(e1);
                }
                else
                {
                    vertexMap2Edges_Coord[v1].Add(e1);
                }


                //List<CHalfEdgeEdge> pV2E = new List<CHalfEdgeEdge>();
                if (vertexMap2Edges_Coord.ContainsKey(v2) == false)
                {
                    vertexMap2Edges_Coord.Add(v2, pV2E0);
                    vertexMap2Edges_Coord[v2].Add(e2);
                }
                else
                {
                    vertexMap2Edges_Coord[v2].Add(e2);
                }


                //List<CHalfEdgeEdge> pV3E = new List<CHalfEdgeEdge>();
                if (vertexMap2Edges_Coord.ContainsKey(v3) == false)
                {
                    vertexMap2Edges_Coord.Add(v3, pV3E0);
                    vertexMap2Edges_Coord[v3].Add(e3);
                }
                else
                {
                    vertexMap2Edges_Coord[v3].Add(e3);
                }


                //    vertexMap2Edges_Coord[v1].Add(e1);
                //vertexMap2Edges_Coord[v2].Add(e2);
                //vertexMap2Edges_Coord[v3].Add(e3);
                if (!isV1New && !isV2New) // V1或者V2 只要有一个是新建点，则e1就没有对边，否则就有。因此必须两个点都是旧点才行
                {
                    // 设置e1
                    // 寻找从v2点出发的半边，终点为v1者就是e1的对边
                    //vector.iterator Edge*> iterator edgeItera;
                    foreach (CHalfEdgeEdge edgeItera in this.vertexMap2Edges_Coord[v2])
                    {
                        CHalfEdgeEdge pEdge = edgeItera;
                        if (pEdge.endPoint == v1)
                        {
                            e1.pair = pEdge;

                            pEdge.pair = e1;

                            vertexMap2Edges_Coord[v2].Remove(edgeItera);
                            vertexMap2Edges_Coord[v1].Remove(e1); //将e1从v1的发出边数组中也删除

                            break;
                        }
                    }
                }

                if (!isV2New && !isV3New) // V2或者V3 只要有一个是新建点，则e2就没有对边，否则就有。因此必须两个点都是旧点才行
                {
                    // 设置e2
                    // 寻找从v3点出发的半边，终点为v2者就是e1的对边
                    //vector.iterator Edge*> iterator edgeItera;
                    foreach (CHalfEdgeEdge edgeItera in vertexMap2Edges_Coord[v3])
                    {
                        CHalfEdgeEdge pEdge = edgeItera;
                        if (pEdge.endPoint == v2)
                        {
                            e2.pair = pEdge;

                            pEdge.pair = e2;

                            vertexMap2Edges_Coord[v3].Remove(edgeItera);
                            vertexMap2Edges_Coord[v2].Remove(e2); //将e2从v2的发出边数组中也删除

                            break;
                        }
                    }

                }

                if (!isV3New && !isV1New) // V3或者V1 只要有一个是新建点，则e3就没有对边，否则就有。因此必须两个点都是旧点才行
                {
                    // 设置e3
                    // 寻找从v1点出发的半边，终点为v3者就是e1的对边
                    //vector.iterator Edge*> iterator edgeItera;
                    foreach (CHalfEdgeEdge edgeItera in vertexMap2Edges_Coord[v1])
                    {
                        CHalfEdgeEdge pEdge = edgeItera;
                        if (pEdge.endPoint == v3)
                        {
                            e3.pair = pEdge;

                            pEdge.pair = e3;

                            vertexMap2Edges_Coord[v1].Remove(edgeItera);
                            vertexMap2Edges_Coord[v3].Remove(e3); //将e3从v3的发出边数组中也删除

                            break;
                        }
                    }
                }
            }
        }
    }
}
