using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    /// <summary>
    /// 岩性
    /// </summary>
    public class Lithology
    {
        /// <summary>
        /// 颜色b分量
        /// </summary>
        public double b { get; set; }

        /// <summary>
        /// 颜色g分量
        /// </summary>
        public double g { get; set; }

        /// <summary>
        /// 岩性编号
        /// </summary>
        public int lithId { get; set; }

        /// <summary>
        /// 岩性名称
        /// </summary>
        public string lithName { get; set; }

        /// <summary>
        /// 颜色r分量
        /// </summary>
        public double r { get; set; }

        public int index { get; set; }
    }
}
