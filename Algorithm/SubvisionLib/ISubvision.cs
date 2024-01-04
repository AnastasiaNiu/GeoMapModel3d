using CommonDataStructureLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubvisionLib
{
    public interface ISubvision
    {
        /// <summary>
        /// 细分
        /// </summary>
        /// <param name="DrillList">钻孔集合</param>
        /// <param name="oriMesh">原始网格</param>
        /// <param name="times">细分次数</param>
        /// <param name="resultFileName">结果保存路径</param>
        void DoSub(List<DrillModel> DrillList, TriMesh oriMesh, int times, string resultFileName);
    }
}
