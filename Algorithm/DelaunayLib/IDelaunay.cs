using CommonDataStructureLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelaunayLib
{
    public interface IDelaunay
    {
        /// <summary>
        /// 带内外约束的三角剖分
        /// </summary>
        /// <param name="points">需要对其构建的点集</param>
        /// <param name="linePoints">外边界的点集</param>
        /// <param name="allinlinePoints">内边界的所有点</param>
        /// <param name="inlinePointslists">多个内边界点列表</param>
        /// <returns></returns>
        TriMesh BuildDelaunay(IList<Points> points, IList<Points> linePoints, IList<Points> allinlinePoints, IList<IList<Points>> inlinePointslists);

    }
}
