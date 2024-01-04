using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoMapModelCli
{
    public class ReadData
    {
        public List<entity> Pathname { get; set; }

    }
    public class entity
    {
        /// <summary>
        /// 地层图层
        /// </summary>
        public string stratumLayerPath { get; set; }
        /// <summary>
        /// 地层线
        /// </summary>
        public string stratumLinePath { get; set; }
        /// <summary>
        /// 断层图层
        /// </summary>
        public string faultLayerPath { get; set; }
        /// <summary>
        /// 剖面线方向
        /// </summary>
        public string direction { get; set; }
        /// <summary>
        /// 保存路径
        /// </summary>
        public string savePath { get; set; }
        /// <summary>
        /// 范围xmax
        /// </summary>
        public double xMax { get; set; }
        /// <summary>
        /// 范围xmin
        /// </summary>
        public double xMin { get; set; }
        /// <summary>
        /// 范围ymin
        /// </summary>
        public double yMin { get; set; }
        /// <summary>
        /// 范围ymax
        /// </summary>
        public double yMax { get; set; }
        /// <summary>
        /// 离散步长
        /// </summary>
        public double stepLength { get; set; }
        /// <summary>
        /// 高程
        /// </summary>
        public string demPath { get; set; }
        /// <summary>
        /// 产状点
        /// </summary>
        public string altitudePointPath { get; set; }
        /// <summary>
        /// 等高线
        /// </summary>
        public string contourLinePath { get; set; }
        /// <summary>
        /// 地层剖面厚度
        /// </summary>
        public double sectionDepth { get; set; }
        /// <summary>
        /// 高程离散步长
        /// </summary>
        public double elevSampleStep { get; set; }
        /// <summary>
        /// 放大系数
        /// </summary>
        public int zZoom { get; set; }
        /// <summary>
        /// 比例
        /// </summary>
        public int scale { get; set; }
        /// <summary>
        /// 地层属性表
        /// </summary>
        public string solidtypetxt { get; set; }
        /// <summary>
        /// 重设y偏移量
        /// </summary>
        public double VirtualDrillResetY { get; set; }
        /// <summary>
        /// 虚拟钻孔点间隔步长
        /// </summary>
        public double VirtualDrillStep { get; set; }
    }
}
