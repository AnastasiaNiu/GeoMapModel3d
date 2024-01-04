using CommonDataStructureLib;
using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMethodHelpLib
{
    public class DrillProcessHelper
    {
        /// <summary>
        /// 钻孔处理辅助类
        /// </summary>

        /// <summary>
        /// 钻孔集合
        /// </summary>
        public Dictionary<string, DrillModel> a;
        public Dictionary<string, DrillModel> DrillList
        {
            set { a = value; }

            get { return a; }
        }

        /// <summary>
        /// 岩性表
        /// </summary>
        public Dictionary<int, Lithology> b;
        public Dictionary<int, Lithology> LithList
        {
            set { b = value; }

            get { return b; }
        }

        /// <summary>
        /// 钻孔预处理
        /// </summary>
        /// <param name="_ShapeFileHelper">区域范围</param>
        /// <param name="_elevationHelper">区域高程</param>
        /// <param name="pathname">DEM路径</param>
        /// <param name="bottomHeight">底面统一高程</param>
        public void Pretreatment(IFeature _Shape, RasterHelper _ElevationHelper, string pathname, double bottomHeight)
        {



            int maxLithCount = b.Count;

            var newDrillList = new Dictionary<string, DrillModel>();
            int inum = 1;

            foreach (KeyValuePair<string, DrillModel> keyValuePair in a)
            {

                var oldDr = keyValuePair.Value;

                IPoint geometry = new Point(new Coordinate(oldDr.X+40000000, oldDr.Y));
                if (!_Shape.Geometry.Intersects(geometry)) continue;
                if (oldDr.StratumList.Count == 0)
                {
                    continue;
                }
                _ElevationHelper.ReadTIFF(pathname);
                double value = 0;

                int ha = Convert.ToInt32(oldDr.X + 40000000);
                int hb = Convert.ToInt32(oldDr.Y);
                _ElevationHelper.GetRasterValue(ha, hb, out value);
                var newDr = new DrillModel(DrillType.Investigate)
                {
                    Id = oldDr.Id,
                    X = Math.Round(oldDr.X + 40000000, 3),
                    Y = Math.Round(oldDr.Y, 3),
                    H = Math.Round(value, 3),
                    StratumList = new List<StratumModel>(maxLithCount)
                };
                //// 如果设定范围比原来高，裁掉原来的下面的部分
                for (int i = 0; i < oldDr.StratumList.Count; i++)
                {
                    if (bottomHeight > oldDr.H - oldDr.StratumList[i].Height)
                    {
                        oldDr.StratumList[i].ZDown = bottomHeight;
                        oldDr.StratumList[i].Height = oldDr.H - oldDr.StratumList[i].ZDown;
                    }
                }



                // 合并地层，如果连续两层一致，则合并；如果层号不属于底层表，则去除，孙
                for (int i = 0; i < oldDr.StratumList.Count; i++)
                {
                    // 把不合规则的地层去掉
                    if (oldDr.StratumList[i].LithId == -1)
                    {
                        // 非0层，将上层底下延
                        if (i != 0)
                        {
                            oldDr.StratumList[i - 1].ZDown = oldDr.StratumList[i].ZDown;
                            oldDr.StratumList[i - 1].Height = oldDr.StratumList[i].Height;
                            oldDr.StratumList.RemoveAt(i);
                            i--;
                        }
                        // 0层，将下层顶上延
                        else
                        {
                            oldDr.StratumList[oldDr.StratumList.Count - 1].ZUp = oldDr.StratumList[i].ZUp;
                            oldDr.StratumList.RemoveAt(i);
                            i--;
                        }
                    }
                    if (i > 0)
                    {
                        if (oldDr.StratumList[i].LithCode == oldDr.StratumList[i - 1].LithCode)
                        {
                            oldDr.StratumList[i - 1].ZDown = oldDr.StratumList[i].ZDown;
                            oldDr.StratumList[i - 1].Height = oldDr.StratumList[i].Height;
                            oldDr.StratumList.RemoveAt(i);
                            i--;
                        }
                    }

                }

                // 添加虚拟地层

                for (int i = 0; i < maxLithCount; i++)
                {
                    newDr.StratumList.Add(null);
                }

                foreach (StratumModel stratum in oldDr.StratumList)
                {

                    newDr.StratumList[stratum.LithId] = stratum;
                }

                for (int i = 0; i < maxLithCount; i++)
                {
                    if (newDr.StratumList[i] == null)
                    {
                        StratumModel s = new StratumModel(StratumType.Virtual);
                        s.LithId = i;


                        if (i == 0)
                        {
                            s.Height = 0;
                        }
                        else
                        {
                            s.Height = newDr.StratumList[i - 1].Height;
                        }

                        newDr.StratumList[i] = s;
                    }
                }

                // 计算地层顶板和底板的绝对坐标
                for (int i = 0; i < maxLithCount; i++)
                {
                    newDr.StratumList[i].ZDown = Math.Round(newDr.H - newDr.StratumList[i].Height, 3);

                    if (i == 0)
                    {
                        newDr.StratumList[i].ZUp = newDr.H;
                    }
                    else
                    {
                        newDr.StratumList[i].ZUp = newDr.StratumList[i - 1].ZDown;
                    }
                    if (i == maxLithCount - 1)
                    {
                        newDr.StratumList[i].ZDown = bottomHeight;
                    }
                }
                newDr.Depth = newDr.H;
                newDrillList[newDr.Id] = newDr;
                inum = inum + 1;
            }

            a = newDrillList;
        }


        /// <summary>
        /// 匹配钻孔点
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <returns></returns>
        public DrillModel RetriveDrillByCoord(double x, double y)
        {

            foreach (KeyValuePair<string, DrillModel> keyValuePair in a)
            {
                var oldDr = keyValuePair.Value;

                if (Math.Abs(oldDr.X - x) < 0.1 && Math.Abs(oldDr.Y - y) < 0.1)
                {
                    return oldDr;
                }
            }

            return null;
        }
    }
}
