using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public class RockStratumModel
    {

        /// <summary>
        /// 地层线属性_点，存储该点所在的线类型（地层线、断层线）
        /// </summary>
        public  struct StratigraphicAttribute_Point
        {
            public IPoint point;
            public string lineType;
            public int id;
            public StratigraphicAttribute_Point(IPoint a, string b, int c = -1)
            {
                point = a;
                lineType = b;
                id = c;
            }
        }
        /// <summary>
        /// 产状信息
        /// </summary>
        public struct Altitude
        {
            /// <summary>
            /// X坐标            
            /// </summary>
            public double x;

            /// <summary>
            /// Y坐标
            /// </summary>
            public double y;

            /// <summary>
            /// 倾向
            /// </summary>
            public double tendency;

            /// <summary>
            /// 倾角
            /// </summary>
            public double angle;
            /// <summary>
            /// 走向
            /// </summary>
            public double lineangle;
        }
        /// <summary>
        /// 地层岩性属性
        /// </summary>
        public struct StratigraphicAttribute
        {
            /// <summary>
            /// 地质属性            
            /// </summary>
            public string DSN;

            /// <summary>
            /// 地质代号            
            /// </summary>
            public string DSO;


        }
        /// <summary>
        /// 地层岩性属性_线
        /// </summary>
        public struct StratigraphicAttribute_Polyline
        {
            /// <summary>
            /// 地质属性            
            /// </summary>
            public StratigraphicAttribute attributeLeft;
            public StratigraphicAttribute attributeRight;
            public string lineType;
            public double tendency;
            public double angle;
            public double sectionLineAngle;
            public double sightAngle;
            public LineString Line;
            public int LineID;

            public StratigraphicAttribute_Polyline(StratigraphicAttribute al, StratigraphicAttribute ar,
                string lt, double t, double a, double sla, double sa, LineString l, int lineid = -1)
            {
                attributeLeft = al;
                attributeRight = ar;
                lineType = lt;
                tendency = t;
                angle = a;
                sectionLineAngle = sla;
                sightAngle = sa;
                Line = l;
                LineID = lineid;


            }


        }
        /// <summary>
        /// 图切剖面中的单条地层线/断层线
        /// </summary>
        public struct SectionPolyline
        {
            public int id;
            //该线的地表点x
            public double x;
            //该线的地表点y
            public double y;
            //该线的地表点
            public Point pointUp;
            //该线的底部点
            public Point pointDown;
            //产状
            public double tendency;
            public double angle;
            public double sectionLineAngle;
            public double sightAngle;
        }
        /// <summary>
        /// 单幅图切剖面中的所有线要素合集：包括地层线、断层线、地表线、底部线
        /// </summary>
        public struct GenSectionLine
        {
            public string Direction;
            //若direction为X，x为在真实平面地质图上的x坐标（每条线统一），否则为0
            public double x;
            //若direction为Y，y为在真实平面地质图上的y坐标（每条线统一），否则为0
            public double y;

            //若direction为X，horizontalMinIndex为yMin,反之则为xMin
            public double horizontalMin;
            public List<StratigraphicAttribute_Polyline> sectionLinesAttribute;

            public string m_sectionName;
            public double m_sectionValue;

            public GenSectionLine(List<StratigraphicAttribute_Polyline> sectionLinesAttributes, double xx, double yy, double horizontalmin, string m_sname, double m_sValue, string direction = "X")
            {
                x = xx;
                y = yy;
                horizontalMin = horizontalmin;
                Direction = direction;
                sectionLinesAttribute = sectionLinesAttributes;
                m_sectionName = m_sname;
                m_sectionValue = m_sValue;


            }

        }
        /// <summary>
        /// 地层岩性属性_面
        /// </summary>
        public struct StratigraphicAttribute_Polygon
        {
            /// <summary>
            /// 地质属性            
            /// </summary>
            public StratigraphicAttribute attribute;
            public int faceId;
            public Geometry polygon;

            public StratigraphicAttribute_Polygon(StratigraphicAttribute al, Geometry g, int fId = -1)
            {
                attribute = al;
                polygon = g;
                faceId = fId;

            }


        }

        /// <summary>
        /// 单幅图切剖面中的所有面要素合集
        /// </summary>
        public struct GenSectionFace
        {
            public string Direction;
            //若direction为X，x为在真实平面地质图上的x坐标（每个面统一），否则为0
            public double x;
            //若direction为Y，y为在真实平面地质图上的y坐标（每个面统一），否则为0
            public double y;

            public List<StratigraphicAttribute_Polygon> sectionFaceAttribute;
            public string m_sectionName;
            public double m_sectionValue;
            public GenSectionFace(List<StratigraphicAttribute_Polygon> sectionFaceAttributes, double xx, double yy, string name, double value, string d = "X")
            {
                Direction = d;
                sectionFaceAttribute = sectionFaceAttributes;
                m_sectionName = name;
                m_sectionValue = value;
                x = xx;
                y = yy;




            }

        }
        /// <summary>
        /// 图切剖面和线组合
        /// </summary>
        public struct GenCombine
        {
            public  GenSectionFace genSectionFace;
            public  GenSectionLine genSectionLine;
            public GenCombine(GenSectionFace f, GenSectionLine l)
            {
                genSectionFace = f;
                genSectionLine = l;
            }



        }
        /// <summary>
        /// 单幅图切剖面中的所有虚拟钻孔合集
        /// </summary>
        public struct GenSectionVirtualDrill
        {
            public List<StratigraphicAttribute_VDirll> DrillAttribute;
            public string m_sectionName;
            public GenSectionVirtualDrill(List<StratigraphicAttribute_VDirll> drillAttribute, string m_Name)
            {
                DrillAttribute = drillAttribute;
                m_sectionName = m_Name;

            }
        }
        /// <summary>
        /// 虚拟钻孔属性点
        /// </summary>
        public struct StratigraphicAttribute_VDirll
        {
            public string LineType;
            public int LineId;
            public string FaceId;
            public double x;
            public double y;
            public double z;
            public string DSN;
            public string DSO;
            public Point VPoint;
            public StratigraphicAttribute_VDirll(Point p, string lineType, int lineId, string faceId, double xx, double yy, double zz, string dsn, string dso)
            {
                VPoint = p;
                LineType = lineType;
                LineId = lineId;
                FaceId = faceId;
                x = xx;
                y = yy;
                z = zz;
                DSN = dsn;
                DSO = dso;

            }


        }
        /// <summary>
        /// 所有钻孔
        /// </summary>
        public struct AllDrills
        {
            public FeatureCollection drills { get; set; }
        }
        /// <summary>
        /// 单个地表钻孔
        /// </summary>
        public class ProfileDrill
        {
            public ProfileDrill(IFeature sourcedrill)
            {
                this.sourcedrill = sourcedrill;
            }

            public IFeature sourcedrill { get; set; }
            public FeatureCollection drills { get; set; }
            public FeatureCollection getdrills(IFeature profiledrill, FeatureCollection alldrills)
            {
                var proflirdrillId = profiledrill.Attributes["profileId"].ToString();
                FeatureCollection drills = new FeatureCollection();

                for (int i = 0; i < alldrills.Count; i++)
                {
                    if (alldrills[i].Attributes["profileId"].ToString() == proflirdrillId)
                    {
                        drills.Add(alldrills[i]);
                    }
                }

                return drills;
            }

        }
        /// <summary>
        /// 地表钻孔集合
        /// </summary>
        public class ProfileDrillCollection
        {

            public List<ProfileDrill> profiledrills { get; set; }
            // 构造函数中初始化列表
            public ProfileDrillCollection()
            {
                profiledrills = new List<ProfileDrill>();
            }
            public List<ProfileDrill> addprofiledrill(ProfileDrill one)
            {
                profiledrills.Add(one);
                return profiledrills;
            }
            public ProfileDrill getProfileBYxy(double x, double y)
            {

                for (int i = 0; i < profiledrills.Count; i++)
                {
                    if (Math.Abs(profiledrills[i].sourcedrill.Geometry.Coordinate.X - x) < 0.01 && Math.Abs(profiledrills[i].sourcedrill.Geometry.Coordinate.Y - y) < 0.01)
                    {
                        return profiledrills[i];
                    }

                }

                return null;

            }
        }
    }
}
