using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public class DrillModel
    {
        /// <summary>
        /// 钻孔编号
        /// </summary>
        [Description("钻孔编号")]
        public string Id { get; set; }


        /// <summary>
        /// 钻孔横坐标
        /// </summary>
        [Description("钻孔横坐标")]
        public double X { get; set; }

        /// <summary>
        /// 钻孔纵坐标
        /// </summary>
        [Description("钻孔纵坐标")]
        public double Y { get; set; }

        /// <summary>
        /// 钻孔高程
        /// </summary>
        [Description("钻孔孔口高程")]
        public double H { get; set; }

        /// <summary>
        /// 钻孔完井深度
        /// </summary>
        [Description("钻孔完井深度")]
        public double Depth { get; set; }

        /// <summary>
        /// 水位
        /// </summary>
        [Description("水位")]
        public double WaterLevel { get; set; }
        /// <summary>
        /// 扩展信息
        /// </summary>
        [Description("扩展信息")]
        public IDictionary<string, string> Extensions { get; set; }

        /// <summary>
        /// 地层集合
        /// </summary>
        public IList<StratumModel> StratumList { get; set; } = new List<StratumModel>();

        /// <summary>
        /// 采样集合
        /// </summary>
        //public IList<SampleModel> SampleList { get; set; } = new List<SampleModel>();

        /// <summary>
        /// 钻孔类型
        /// </summary>
        public DrillType DrillType = DrillType.Investigate;
        private double v1;
        private double v2;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="t"></param>
        public DrillModel(DrillType t)
        {
            DrillType = t;
        }

        /// <summary>
        /// 构造函数2
        /// </summary>
        public DrillModel()
        { }

        public DrillModel(double v1, double v2)
        {
            this.X = v1;
            this.Y = v2;
        }

        /// <summary>
        /// 插入一个地层
        /// </summary>
        /// <param name="s"></param>
        public void addStratum(StratumModel s)
        {
            if (StratumList.Count == 0)
            {
                StratumList.Add(s);
            }
            else
            {
                for (int i = 0; i < StratumList.Count; i++)
                {
                    StratumModel current = StratumList[i];

                    if (s.Height < current.Height)
                    {
                        StratumList.Insert(i, s);
                        return;
                    }
                }

                StratumList.Add(s);
            }
        }
    }
    public enum DrillType
    {
        Investigate,
        Interpolation,
        PinchOut,
        Subdivision,
        Unknown
    }
}
