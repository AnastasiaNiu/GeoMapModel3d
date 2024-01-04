using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public class BrepModel
    {
        public Hashtable vertexTable = new Hashtable();

        public ArrayList triangleList = new ArrayList();

        /// <summary>
        /// 添加一个三角形
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
        public void addTriangle(double x0, double y0, double z0,
            double x1, double y1, double z1,
            double x2, double y2, double z2)
        {
            int v0_id = insetVertex(x0, y0, z0);
            int v1_id = insetVertex(x1, y1, z1);
            int v2_id = insetVertex(x2, y2, z2);

            Triangle tri = new Triangle();
            tri.id = this.triangleList.Count;
            tri.v0 = v0_id;
            tri.v1 = v1_id;
            tri.v2 = v2_id;

            this.triangleList.Add(tri);
        }

        /// <summary>
        /// 加入一个顶点，如果是新点则加入这个新点，然后返回新点的ID，如果已经存在该点，直接返回该点的ID
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private int insetVertex(double x, double y, double z)
        {
            foreach (DictionaryEntry de in vertexTable)
            {
                int id = (int)(de.Key);
                Vertex v = de.Value as Vertex;

                if (v.x == x && v.y == y && v.z == z)
                {
                    return id;
                }
            }

            // 加入新的vertex
            Vertex nv = new Vertex();
            nv.id = vertexTable.Count;
            nv.x = x;
            nv.y = y;
            nv.z = z;

            this.vertexTable.Add(nv.id, nv);

            return nv.id;
        }

        /// <summary>
        /// 输出表面模型至obj文件格式
        /// </summary>
        /// <param name="fileName"></param>
        public void exportobj(string fileName)
        {
            int vertexCount = this.vertexTable.Count;
            int triangleCount = this.triangleList.Count;

            try
            {
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    string tl = Convert.ToString(vertexCount) + " " + Convert.ToString(triangleCount);

                    //sw.WriteLine(tl);

                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vertex v = vertexTable[i] as Vertex;
                        tl = "v" + " " + Convert.ToString(v.x) + " " + Convert.ToString(v.y) + " " + Convert.ToString(v.z);
                        sw.WriteLine(tl);


                    }

                    for (int i = 0; i < triangleCount; i++)
                    {
                        Triangle tri = triangleList[i] as Triangle;
                        tl = "f" + " " + Convert.ToString(tri.v0 + 1) + " " + Convert.ToString(tri.v1 + 1) + " " + Convert.ToString(tri.v2 + 1);
                        sw.WriteLine(tl);
                    }

                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("表面模型信息导出失败" + ex.ToString());
            }
        }
        /// <summary>
        /// 炸开输出地层
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="number"></param>
        /// <exception cref="Exception"></exception>
        public void export2obj(string fileName, int number)
        {
            int vertexCount = this.vertexTable.Count;
            int triangleCount = this.triangleList.Count;

            try
            {
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    string tl = Convert.ToString(vertexCount) + " " + Convert.ToString(triangleCount);

                    //sw.WriteLine(tl);

                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vertex v = vertexTable[i] as Vertex;
                        tl = "v" + " " + Convert.ToString(v.x) + " " + Convert.ToString(v.y) + " " + Convert.ToString(v.z - (number * 200));
                        sw.WriteLine(tl);


                    }

                    for (int i = 0; i < triangleCount; i++)
                    {
                        Triangle tri = triangleList[i] as Triangle;
                        tl = "f" + " " + Convert.ToString(tri.v0 + 1) + " " + Convert.ToString(tri.v1 + 1) + " " + Convert.ToString(tri.v2 + 1);
                        sw.WriteLine(tl);
                    }

                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("表面模型信息导出失败" + ex.ToString());
            }
        }
    }
}
