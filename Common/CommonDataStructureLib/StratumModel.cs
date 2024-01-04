using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public class StratumModel
    {

        private string _lithName;

        /// <summary>
        /// 用于给地层进行分组：分为工程、水文
        /// </summary>
        [Description("地层组名称")]
        public string StratumGroupName { get; set; }

        /// <summary>
        /// 地质年代编号
        /// </summary>
        [Description("地质年代编号")]
        public string GeologyAge { get; set; }
        /// <summary>
        /// 地层分层编号
        /// </summary>
        [Description("地层分层编号")]
        public int Level { get; set; }

        /// <summary>
        /// 地层岩性编码
        /// </summary>
        [Description("地层编码")]
        public string LithCode { get; set; }

        /// <summary>
        /// 地层岩性名称
        /// </summary>
        [Description("地层岩性名称")]
        public string LithName
        {
            get
            {
                if (string.IsNullOrEmpty(_lithName) && !string.IsNullOrEmpty(LithDescribe))
                {
                    _lithName = LithDescribe.Split(new char[] { ':', '：' })[0];
                }
                return _lithName;
            }
            set
            {
                _lithName = value;
            }
        }


        /// <summary>
        /// 地层顺序号
        /// </summary>
        [Description("地层顺序号")]
        public int LithId { get; set; }

        /// <summary>
        /// 岩性描述
        /// </summary>
        [Description("岩性描述")]
        public string LithDescribe { get; set; }

        /// <summary>
        /// 地层底板绝对高度
        /// </summary>
        [Description("地层底板高度")]
        public double ZDown { get; set; }

        /// <summary>
        /// 地层顶板绝对高度
        /// </summary>
        [Description("地层顶板高度")]
        public double ZUp { get; set; }

        /// <summary>
        /// 地层底板埋深
        /// </summary>
        [Description("地层底板埋深")]
        public double Height { get; set; }

        /// <summary>
        /// 地层类型
        /// </summary>
        [Description("地层类型")]
        public StratumType StratumType { get; set; }


        /// <summary>
        /// 按照地层顶部高程排序
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(StratumModel other)
        {
            if (this.ZUp < other.ZUp)
            {
                return 1;
            }

            return -1;
        }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">地层类型</param>
        public StratumModel(StratumType type)
        {
            StratumType = type;
        }
    }
    public enum StratumType
    {
        Actual,
        Virtual
    }
}
