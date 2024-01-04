using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public class GTP
    {
        /// <summary>
        /// 似三棱柱编号
        /// </summary>
        public int id;

        /// <summary>
        /// 顶点0
        /// </summary>
        public Vertex v0;

        /// <summary>
        /// 顶点1
        /// </summary>
        public Vertex v1;

        /// <summary>
        /// 顶点2
        /// </summary>
        public Vertex v2;

        /// <summary>
        /// 顶点3
        /// </summary>
        public Vertex v3;

        /// <summary>
        /// 顶点4
        /// </summary>
        public Vertex v4;

        /// <summary>
        /// 顶点5
        /// </summary>
        public Vertex v5;

        /// <summary>
        /// 体元属性值
        /// </summary>
        public int value;

        /// <summary>
        /// 构造函数
        /// </summary>
        public GTP()
        {
            id = value = 0;

            v0 = v1 = v2 = v3 = v4 = v5 = null;
        }
    }
}
