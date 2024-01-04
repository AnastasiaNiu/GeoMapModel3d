using CommonDataStructureLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterpolateLib
{
    public interface IInterpolate
    {
        /// <summary>
        /// 三维插值方法
        /// </summary>
        /// <param name="sourcePoints">已知点</param>
        /// <param name="targetPoints">未知点</param>
        /// <param name="way">插值方法</param>
        /// <returns></returns>
        IList<double> interpolate(IList<Points> sourcePoints, IList<Points> targetPoints, EnumInterpWay way);
        /// <summary>
        /// 二维插值方法IDW
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        double IdwInterpolate2d(double x, double y, List<Points> source);
    }
}
