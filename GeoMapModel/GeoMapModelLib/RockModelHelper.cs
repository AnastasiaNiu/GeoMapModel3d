using CommonDataStructureLib;
using CommonMethodHelpLib;
using DelaunayLib;
using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Triangulate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonDataStructureLib.RockStratumModel;
using Triangle = CommonDataStructureLib.Triangle;

namespace GeoMapModelLib
{
    internal class RockModelHelper
    {
        public void CreateRockModel(string savePath, string connectDrillsPath, string profileDrillsPath, ShapeFileHelper stratumLayer, ShapeFileHelper stratumLine, string soildtypepath)
        {
            //读取钻孔数据
            ShapeFileHelper connectDrills = new ShapeFileHelper(connectDrillsPath);
            ShapeFileHelper profileDrills = new ShapeFileHelper(profileDrillsPath);
            //1、生成三角网列表，分批生成
            TriMesh triMesh = new TriMesh();            
            var meshss = triMesh. createTriMesh(stratumLine.pFeatureCollection, profileDrills.pFeatureCollection, savePath);
            //2、获得地层属性表
            ReadExcelHelper read = new ReadExcelHelper();
            var geoAttributes = read.getAttribute(soildtypepath);
            //3、将地表点与所有点进行匹配
            List<int> cols = new List<int>();
            for (int i = 0; i < profileDrills.pFeatureCollection.Count; i++)
            {
                cols.Add(Convert.ToInt32(profileDrills.pFeatureCollection[i].Attributes["col"].ToString()));
            }
            cols = cols.Distinct().ToList();
            //4、按照每51个为一个集合，分成集合列表List-coordinateArrays
            var num = Math.Ceiling(Convert.ToDouble(cols.Max() / 50.0));
            for (int k = 0; k < num; k++)
            {

                FeatureCollection onewprofilelist = new FeatureCollection();
                for (int i = 0; i < profileDrills.pFeatureCollection.Count; i++)
                {
                    if (Convert.ToInt32(profileDrills.pFeatureCollection[i].Attributes["col"].ToString()) >= k * 50 && Convert.ToInt32(profileDrills.pFeatureCollection[i].Attributes["col"].ToString()) <= (k + 1) * 50 + 1)
                    {
                        onewprofilelist.Add(profileDrills.pFeatureCollection[i]);
                    }
                }
                if (onewprofilelist.Count > 0)
                {
                    AllDrills drills = new AllDrills();
                    drills.drills = connectDrills.pFeatureCollection;
                    ProfileDrillCollection profiledrillCollection = new ProfileDrillCollection();
                    for (int i = 0; i < onewprofilelist.Count; i++)
                    {
                        ProfileDrill profiledrill = new ProfileDrill(onewprofilelist[i]);
                        var onedrills = profiledrill.getdrills(onewprofilelist[i], connectDrills.pFeatureCollection);
                        profiledrill.drills = onedrills;
                        profiledrillCollection.addprofiledrill(profiledrill);
                    }

                    //4、根据meshs判断地层情况并创建三棱柱地层列表
                    //读取沉积地层
                    var soilGrid = newcreateTriangilarPrism(stratumLine.pFeatureCollection , profiledrillCollection, meshss[k]);
                    StreamWriter soilGridVoxelWriter = new StreamWriter(savePath + "\\soildVoxelGrids-" + k + ".txt");
                    soilGridVoxelWriter.WriteLine(soilGrid.voxelCollection.Count);
                    StreamWriter soilGridWriter = new StreamWriter(savePath + "\\soildGrids-" + k + ".txt");
                    foreach (var item in soilGrid.vertexDict.Values)
                    {
                        soilGridVoxelWriter.WriteLine(item[0] + " " + item[1] + " " + item[2] + " " + item[3]);
                    }
                    for (int i = 0; i < soilGrid.voxelCollection.Count; i++)
                    {
                        for (int j = 0; j < 12; j++)
                        {
                            soilGridWriter.Write(soilGrid.voxelCollection[i][j] + " ");
                        }
                        soilGridWriter.WriteLine("");
                    }
                    //5、创建VTK
                    foreach (var soildtype in geoAttributes)
                    {                       
                        var vtkname =savePath + "//rock_" + soildtype.ToString() + "==" + k + ".vtk";
                        VTK createvtk = new VTK();
                        createvtk.createVtkFromGridForValue(vtkname, "Unstructured Soil Grid", soilGrid, Convert.ToDouble(soildtype.ToString()));
                    }

                    

                }

            }
        }
        /// <summary>
        /// 判断三个钻孔排列组合情况
        /// </summary>
        /// <param name="area"></param>
        /// <param name="profile1"></param>
        /// <param name="profile2"></param>
        /// <param name="profile3"></param>
        /// <returns></returns>
        public int newdetect3Profiles(FeatureCollection area, ProfileDrill profile1, ProfileDrill profile2, ProfileDrill profile3)
        {
            bool Isdetect = false;
            var num1 = 0;
            var num2 = 0;
            var num3 = 0;
            if (profile1 != null && profile2 != null && profile3 != null)
            {
                num1 = profile1.drills.Count;
                num2 = profile2.drills.Count;
                num3 = profile3.drills.Count;

                if (num1 == num2 && num2 == num3)
                {
                    bool isDifferent = false;
                    for (int i = 0; i < num1; i++)
                    {
                        if (profile1.drills[i].Attributes["soildType"].ToString() == profile2.drills[i].Attributes["soildType"].ToString() &&
                   profile1.drills[i].Attributes["soildType"].ToString() == profile3.drills[i].Attributes["soildType"].ToString())
                        {
                            continue;
                        }
                        else
                        {
                            isDifferent = true;
                            break;
                        }
                    }
                    if (isDifferent == false)
                    {
                        Isdetect = true;
                        return 1;
                    }

                }

            }
            else
            {
                Isdetect = true;
                return 10086;

            }
            //单层判断，保证其地层与地质图一致



            //判断是否为顺序角度不整合

            ///2、判断三个钻孔中，各个地层是否按照正常顺序排列
            //钻孔1
            bool Istrueno1 = true;
            for (int i = 0; i < num1 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile1.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile1.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile1.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile1.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile1.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile1.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 < value1)
                {
                    Istrueno1 = false;
                    break;
                }
            }

            //钻孔2
            bool Istrueno2 = true;
            for (int i = 0; i < num2 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile2.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile2.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile2.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile2.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile2.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile2.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 < value1)
                {
                    Istrueno2 = false;
                    break;
                }
            }

            //钻孔3
            bool Istrueno3 = true;
            for (int i = 0; i < num3 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile3.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile3.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile3.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile3.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile3.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile3.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 < value1)
                {
                    Istrueno3 = false;
                    break;
                }
            }
            if ((Istrueno3 || num3 == 2) && (Istrueno1 || num1 == 2) && (Istrueno2 || num2 == 2))
            {
                Isdetect = true;

                //创建三角形ply
                GeometryFactory geometryFactory = new GeometryFactory();
                var tri = geometryFactory.CreatePolygon(new Coordinate[4] { profile1.sourcedrill.Geometry.Coordinate, profile2.sourcedrill.Geometry.Coordinate, profile3.sourcedrill.Geometry.Coordinate, profile1.sourcedrill.Geometry.Coordinate });
                //判断该三角形中点在哪一个地层上
                string attburite = null;
                for (int a = 0; a < area.Count; a++)
                {
                    if (area[a].Geometry.Contains(tri.Centroid))
                    {
                        attburite = area[a].Attributes["DSN"].ToString();
                        break;
                    }
                }
                //判断该地层与三点哪个相对应
                if (attburite == profile1.drills.Features[profile1.drills.Features.Count - 1].Attributes["DSN"].ToString())
                {
                    if (Convert.ToInt32(profile1.drills.Features[profile1.drills.Features.Count - 1].Attributes["soildType"].ToString()) > Convert.ToInt32(profile2.drills.Features[profile2.drills.Count - 1].Attributes["soildType"].ToString()) ||
                        Convert.ToInt32(profile1.drills.Features[profile1.drills.Features.Count - 1].Attributes["soildType"].ToString()) > Convert.ToInt32(profile3.drills.Features[profile3.drills.Count - 1].Attributes["soildType"].ToString()))
                    {
                        if (num1 == 2 && num2 == 2 && num3 == 2)
                        {
                            return 121;
                        }
                        else
                        {
                            return 1230;
                        }


                    }
                }
                if (attburite == profile2.drills.Features[profile2.drills.Features.Count - 1].Attributes["DSN"].ToString())
                {
                    if (Convert.ToInt32(profile2.drills.Features[profile2.drills.Features.Count - 1].Attributes["soildType"].ToString()) > Convert.ToInt32(profile1.drills.Features[profile1.drills.Count - 1].Attributes["soildType"].ToString()) ||
                        Convert.ToInt32(profile2.drills.Features[profile2.drills.Features.Count - 1].Attributes["soildType"].ToString()) > Convert.ToInt32(profile3.drills.Features[profile3.drills.Count - 1].Attributes["soildType"].ToString()))
                    {
                        if (num1 == 2 && num2 == 2 && num3 == 2)
                        {
                            return 121;
                        }
                        else
                        {
                            return 1231;
                        }
                    }
                }
                if (attburite == profile3.drills.Features[profile3.drills.Features.Count - 1].Attributes["DSN"].ToString())
                {
                    if (Convert.ToInt32(profile3.drills.Features[profile3.drills.Features.Count - 1].Attributes["soildType"].ToString()) > Convert.ToInt32(profile1.drills.Features[profile1.drills.Count - 1].Attributes["soildType"].ToString()) ||
                        Convert.ToInt32(profile3.drills.Features[profile3.drills.Features.Count - 1].Attributes["soildType"].ToString()) > Convert.ToInt32(profile2.drills.Features[profile2.drills.Count - 1].Attributes["soildType"].ToString()))
                    {
                        if (num1 == 2 && num2 == 2 && num3 == 2)
                        {
                            return 121;
                        }
                        else
                        {
                            return 1232;
                        }
                    }
                }
                return 120;

            }
            ///2、判断三个钻孔中，各个地层是否不按照正常顺序排列
            //钻孔1
            bool Istrue1 = true;
            for (int i = 0; i < num1 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile1.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile1.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile1.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile1.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile1.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile1.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 > value1)
                {
                    Istrue1 = false;
                    break;
                }
            }

            //钻孔2
            bool Istrue2 = true;
            for (int i = 0; i < num2 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile2.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile2.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile2.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile2.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile2.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile2.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 > value1)
                {
                    Istrue2 = false;
                    break;
                }
            }

            //钻孔3
            bool Istrue3 = true;
            for (int i = 0; i < num3 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile3.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile3.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile3.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile3.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile3.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile3.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 > value1)
                {
                    Istrue3 = false;
                    break;
                }
            }
            if ((Istrue3) && (Istrue1) && (Istrue2))
            {
                Isdetect = true;


                //创建三角形ply
                GeometryFactory geometryFactory = new GeometryFactory();
                var tri = geometryFactory.CreatePolygon(new Coordinate[4] { profile1.sourcedrill.Geometry.Coordinate, profile2.sourcedrill.Geometry.Coordinate, profile3.sourcedrill.Geometry.Coordinate, profile1.sourcedrill.Geometry.Coordinate });
                //判断该三角形中点在哪一个地层上
                string attburite = null;
                for (int a = 0; a < area.Count; a++)
                {
                    if (area[a].Geometry.Contains(tri.Centroid))
                    {
                        attburite = area[a].Attributes["DSN"].ToString();
                        break;
                    }
                }
                //判断该地层与三点哪个相对应
                if (attburite == profile1.drills.Features[profile1.drills.Features.Count - 1].Attributes["DSN"].ToString())
                {
                    if (Convert.ToInt32(profile1.drills.Features[profile1.drills.Features.Count - 1].Attributes["soildType"].ToString()) < Convert.ToInt32(profile2.drills.Features[profile2.drills.Count - 1].Attributes["soildType"].ToString()) ||
                        Convert.ToInt32(profile1.drills.Features[profile1.drills.Features.Count - 1].Attributes["soildType"].ToString()) < Convert.ToInt32(profile3.drills.Features[profile3.drills.Count - 1].Attributes["soildType"].ToString()))
                    {
                        if (num1 == 2 && num2 == 2 && num3 == 2)
                        {
                            return 120;
                        }
                        else
                        {
                            return 1240;
                        }


                    }
                }
                if (attburite == profile2.drills.Features[profile2.drills.Features.Count - 1].Attributes["DSN"].ToString())
                {
                    if (Convert.ToInt32(profile2.drills.Features[profile2.drills.Features.Count - 1].Attributes["soildType"].ToString()) < Convert.ToInt32(profile1.drills.Features[profile1.drills.Count - 1].Attributes["soildType"].ToString()) ||
                        Convert.ToInt32(profile2.drills.Features[profile2.drills.Features.Count - 1].Attributes["soildType"].ToString()) < Convert.ToInt32(profile3.drills.Features[profile3.drills.Count - 1].Attributes["soildType"].ToString()))
                    {
                        if (num1 == 2 && num2 == 2 && num3 == 2)
                        {
                            return 120;
                        }
                        else
                        {
                            return 1241;
                        }
                    }
                }
                if (attburite == profile3.drills.Features[profile3.drills.Features.Count - 1].Attributes["DSN"].ToString())
                {
                    if (Convert.ToInt32(profile3.drills.Features[profile3.drills.Features.Count - 1].Attributes["soildType"].ToString()) < Convert.ToInt32(profile1.drills.Features[profile1.drills.Count - 1].Attributes["soildType"].ToString()) ||
                        Convert.ToInt32(profile3.drills.Features[profile3.drills.Features.Count - 1].Attributes["soildType"].ToString()) < Convert.ToInt32(profile2.drills.Features[profile2.drills.Count - 1].Attributes["soildType"].ToString()))
                    {
                        if (num1 == 2 && num2 == 2 && num3 == 2)
                        {
                            return 120;
                        }
                        else
                        {
                            return 1242;
                        }
                    }
                }
                return 121;

            }
            ///3、判断三个钻孔中，各个地层是否不按照顺序排列（既不是正序也不是倒序）
            //钻孔1
            var positivetemp1 = 0;
            var reversetemp1 = 0;
            for (int i = 0; i < num1 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile1.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile1.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile1.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile1.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile1.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile1.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 >= value1)
                {
                    positivetemp1 = positivetemp1 + 1;
                }
                else
                {
                    reversetemp1 = reversetemp1 + 1;
                }
            }
            if (positivetemp1 != num1 - 1)
            {
                if (reversetemp1 != num1 - 1)
                {
                    return 122;
                }
            }
            //钻孔2
            var positivetemp2 = 0;
            var reversetemp2 = 0;
            for (int i = 0; i < num2 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile2.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile2.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile2.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile2.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile2.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile2.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 >= value1)
                {
                    positivetemp2 = positivetemp2 + 1;
                }
                else
                {
                    reversetemp2 = reversetemp2 + 1;
                }
            }
            if (positivetemp2 != num2 - 1)
            {
                if (reversetemp2 != num2 - 1)
                {
                    return 122;
                }
            }
            //钻孔3
            var positivetemp3 = 0;
            var reversetemp3 = 0;
            for (int i = 0; i < num3 - 1; i++)
            {
                var value0 = 0;
                var value1 = 0;
                if (profile3.drills.Features[i].Attributes["soildType"].ToString().Length > 2)
                {
                    value0 = Convert.ToInt32(profile3.drills.Features[i].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value0 = Convert.ToInt32(profile3.drills.Features[i].Attributes["soildType"].ToString());
                }
                if (profile3.drills.Features[i + 1].Attributes["soildType"].ToString().Length > 2)
                {
                    value1 = Convert.ToInt32(profile3.drills.Features[i + 1].Attributes["soildType"].ToString().Split('_').ToList()[1]);
                }
                else
                {
                    value1 = Convert.ToInt32(profile3.drills.Features[i + 1].Attributes["soildType"].ToString());
                }
                if (value0 >= value1)
                {
                    positivetemp3 = positivetemp3 + 1;
                }
                else
                {
                    reversetemp3 = reversetemp3 + 1;
                }
            }
            if (positivetemp3 != num3 - 1)
            {
                if (reversetemp3 != num3 - 1)
                {
                    return 122;
                }
            }

            if (!Isdetect)
            {
                return 999999;
            }
            else
            {
                return 44444;
            }
        }
        /// <summary>
        /// 根据组合情况创建体元
        /// </summary>
        /// <param name="area"></param>
        /// <param name="profileCollection"></param>
        /// <param name="triangleList"></param>
        /// <returns></returns>
        public SoilGrid newcreateTriangilarPrism(FeatureCollection area, ProfileDrillCollection profileCollection, TriMesh triangleList)
        {
            SoilGrid soilGrid = new SoilGrid();
            foreach (var triangle in triangleList.triangleList)
            {
                //1、找到对应的地表钻孔
                var triangleID = triangle.id;
                if (triangleID == 3690)
                {
                    var stop = 0;
                }
                else
                {

                }
                var v0 = triangleList.findVertexByID(triangle.v0);
                var v1 = triangleList.findVertexByID(triangle.v1);
                var v2 = triangleList.findVertexByID(triangle.v2);
                ProfileDrill profile1 = profileCollection.getProfileBYxy(v0.x, v0.y);
                ProfileDrill profile2 = profileCollection.getProfileBYxy(v1.x, v1.y);
                ProfileDrill profile3 = profileCollection.getProfileBYxy(v2.x, v2.y);
                if (profile1 == null || profile2 == null || profile3 == null) continue;
                //2、判断三棱柱组成的每种情况
                var detect = newdetect3Profiles(area, profile1, profile2, profile3);
                ProfileDrill[] profiles = { profile1, profile2, profile3 };
                //3、生成三棱柱地层列表
                List<List<List<double>>> layerList = new List<List<List<double>>>();
                foreach (var pro in profiles)
                {
                    List<List<double>> templayerList = new List<List<double>>();
                    if (profiles == null)
                    {
                    }
                    var oripropoints = pro.drills;
                    var propoints = pro.drills;
                    var numPoints = propoints.Count;
                    //在处理中发现不法虚拟钻孔，需要对其进行删除，要保证第一个是底部线，最后一个是地表线

                    for (int l = 0; l < oripropoints.Count; l++)
                    {
                        //查找底部线的索引
                        int index = l;
                        if (oripropoints[index].Attributes["LineType"].ToString() == "底部线")
                        {
                            if (index == 0) continue;
                            else
                            {
                                for (int m = 0; m < index; m++)
                                {
                                    propoints.Remove(oripropoints[m]);
                                }
                            }
                        }

                    }


                    numPoints = propoints.Count;
                    for (int i = 0; i < numPoints - 1; i++)
                    {
                        var zUp = propoints[i + 1].Geometry.Coordinate.Z;
                        var zDown = propoints[i].Geometry.Coordinate.Z;
                        if (propoints[i + 1].Attributes["LineType"].ToString() == "地表线")
                        {
                            var valuesList = propoints[i].Attributes["soildType"].ToString().Split('_').ToList();
                            var value = Convert.ToDouble(valuesList[0]);
                            List<double> layer = new List<double>();
                            layer.Add(value);
                            layer.Add(zUp);
                            layer.Add(zDown);
                            layer.Add(propoints[i + 1].Geometry.Coordinate.X);
                            layer.Add(propoints[i + 1].Geometry.Coordinate.Y);
                            layer.Add(i + 1);
                            templayerList.Add(layer);

                        }
                        if (propoints[i].Attributes["LineType"].ToString() == "地层线" || propoints[i].Attributes["LineType"].ToString() == "断层线")
                        {
                            var valuesList = propoints[i].Attributes["soildType"].ToString().Split('_').ToList();
                            //有的地层线多生成了，这里先给出处理方法，之后对生成地层线代码进行修改
                            var value = Convert.ToDouble(valuesList[1]);
                            var value1 = Convert.ToDouble(valuesList[0]);
                            List<double> layer = new List<double>();
                            //判断该钻孔上下地层是否一致
                            if (value == value1)
                            {
                                var stop = 0;
                            }

                            layer.Add(value);
                            layer.Add(propoints[i].Geometry.Coordinate.Z);
                            layer.Add(propoints[i - 1].Geometry.Coordinate.Z);
                            layer.Add(propoints[i].Geometry.Coordinate.X);
                            layer.Add(propoints[i].Geometry.Coordinate.Y);
                            layer.Add(Convert.ToDouble(propoints[i].Attributes["LineId"].ToString()));
                            layer.Add(Convert.ToDouble(propoints[i].Attributes["LineId"].ToString()));
                            layer.Add(Convert.ToDouble(propoints[i].Attributes["faceId"].ToString().Split('_').ToList()[0]));
                            layer.Add(i);
                            templayerList.Add(layer);

                        }


                    }
                    //将地层按照顺序排列（地表-底部）
                    for (int k = 0; k < templayerList.Count; k++)
                    {
                        for (int m = k + 1; m < templayerList.Count; m++)
                        {
                            if (templayerList[k][templayerList[k].Count - 1] < templayerList[m][templayerList[m].Count - 1])
                            {

                                var temp = templayerList[k];
                                templayerList[k] = templayerList[m];
                                templayerList[m] = temp;
                            }
                        }
                    }
                    layerList.Add(templayerList);
                }

                //1正常沉积
                //2顺序（倒转）平行不整合
                if (detect == 1)
                {
                    var layerNum = layerList[0].Count;
                    //创建体元
                    for (int i = 0; i < layerNum; i++)
                    {
                        var value = layerList[0][i][0];
                        var v00 = soilGrid.addVertex(layerList[0][i][3], layerList[0][i][4], layerList[0][i][1]);
                        var v01 = soilGrid.addVertex(layerList[1][i][3], layerList[1][i][4], layerList[1][i][1]);
                        var v02 = soilGrid.addVertex(layerList[2][i][3], layerList[2][i][4], layerList[2][i][1]);
                        //v3和v2一样
                        var v03 = v02;
                        var v04 = soilGrid.addVertex(layerList[0][i][3], layerList[0][i][4], layerList[0][i][2]);
                        var v05 = soilGrid.addVertex(layerList[1][i][3], layerList[1][i][4], layerList[1][i][2]);
                        var v06 = soilGrid.addVertex(layerList[2][i][3], layerList[2][i][4], layerList[2][i][2]);
                        //v7和v6一样
                        var v07 = v06;
                        //创建voxel
                        soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                    }
                }
                //3顺序角度不整合或者地层不按照顺序排列
                else if (detect == 120 || detect == 122)
                {
                    //从最老地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    var numand = 0;
                    if (lay0 > 0 & lay1 > 0 & lay2 > 0)
                    {
                        var stop = 0;
                    }
                    if (numand > 10)
                    {
                        var stop = 0;
                    }
                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == -1 && value1 == -1 && value2 == -1) break;
                        //分别以钻孔0、1、2底为最老地层
                        //情况1：钻孔0最老（以钻孔0为例）
                        //情况2：钻孔0==钻孔1>钻孔2
                        //情况3：钻孔0==钻孔2>钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 > value1 && value0 > value2)
                        {
                            //跳出循环
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;
                            }
                            //判断上一层是否与这一层一致
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;
                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔0==钻孔2>钻孔1
                        else if (value0 == value2 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }

                        }
                        //②以钻孔1底为最老地层
                        //情况1：钻孔1最老
                        else if (value1 > value0 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;

                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}


                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;

                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }

                        }
                        //③以钻孔2底为最老地层
                        //情况1：钻孔2最老
                        else if (value2 > value0 && value2 > value1)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔2==钻孔0>钻孔1
                        else if (value2 == value0 && value0 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //4倒转角度不整合
                else if (detect == 121)
                {
                    //从最底地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    var numand = 0;
                    if (numand > 10)
                    {
                        var stop = 0;
                    }
                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == 100 && value1 == 100 && value2 == 100) break;
                        //①分别以钻孔0、1、2底为最新地层
                        //情况1：钻孔0最新
                        //情况2：钻孔0==钻孔1<钻孔2
                        //情况3：钻孔0==钻孔2<钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 < value1 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;
                            }
                            //判断上一层是否与这一层一致
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔0==钻孔2<钻孔1
                        else if (value0 == value2 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }

                        }
                        //②以钻孔1底为最xin地层
                        //情况1：钻孔1最xin
                        else if (value1 < value0 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;

                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }

                        }
                        //③以钻孔2底为最xin地层
                        //情况1：钻孔2最xin
                        else if (value2 < value0 && value2 < value1)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔2==钻孔0<钻孔1
                        else if (value2 == value0 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 100;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 100;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 100;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //5、顺序地层生成的最新层与平面地质图不符，单独夹层处理
                //5.1钻孔0的最新地层为最老地层
                else if (detect == 1230)
                {
                    //其处理方法与顺序地层相同，但其中关键是将顶层作为最最最新地层处理

                    //从最老地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    //将钻孔0的最新地层设置成最最最新地层
                    var orivalue = layerList[0][0][0];
                    layerList[0][0][0] = -1000;
                    var numand = 0;
                    if (lay0 > 0 & lay1 > 0 & lay2 > 0)
                    {
                        var stop = 0;
                    }
                    //判断是否陷入死循环
                    if (numand > 10)
                    {
                        var stop = 0;
                    }

                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == -1001 && value1 == -1001 && value2 == -1001) break;
                        //分别以钻孔0、1、2底为最老地层
                        //情况1：钻孔0最老（以钻孔0为例）
                        //情况2：钻孔0==钻孔1>钻孔2
                        //情况3：钻孔0==钻孔2>钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 > value1 && value0 > value2)
                        {


                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }


                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }


                        }
                        //情况3：钻孔0==钻孔2>钻孔1
                        else if (value0 == value2 && value1 < value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //②以钻孔1底为最老地层
                        //情况1：钻孔1最老
                        else if (value1 > value0 && value1 > value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }



                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);

                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }


                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //③以钻孔2底为最老地层
                        //情况1：钻孔2最老
                        else if (value2 > value0 && value2 > value1)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况2：钻孔2==钻孔0>钻孔1
                        else if (value2 == value0 && value0 > value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //5.2钻孔1的最新地层为最老地层
                else if (detect == 1231)
                {
                    //其处理方法与顺序地层相同，但其中关键是将顶层作为最最最新地层处理
                    //从最老地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    //将钻孔1的最新地层设置成最最最新地层
                    var orivalue = layerList[1][0][0];
                    layerList[1][0][0] = -1000;
                    var numand = 0;
                    if (lay0 > 0 & lay1 > 0 & lay2 > 0)
                    {
                        var stop = 0;
                    }
                    if (numand > 10)
                    {
                        var stop = 0;
                    }

                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == -1001 && value1 == -1001 && value2 == -1001) break;
                        //分别以钻孔0、1、2底为最老地层
                        //情况1：钻孔0最老（以钻孔0为例）
                        //情况2：钻孔0==钻孔1>钻孔2
                        //情况3：钻孔0==钻孔2>钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 > value1 && value0 > value2)
                        {
                            //跳出循环

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }

                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }


                        }
                        //情况3：钻孔0==钻孔2>钻孔1
                        else if (value0 == value2 && value1 < value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //②以钻孔1底为最老地层
                        //情况1：钻孔1最老
                        else if (value1 > value0 && value1 > value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }



                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }


                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //③以钻孔2底为最老地层
                        //情况1：钻孔2最老
                        else if (value2 > value0 && value2 > value1)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况2：钻孔2==钻孔0>钻孔1
                        else if (value2 == value0 && value0 > value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }


                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //5.3钻孔2的最新地层为最老地层
                else if (detect == 1232)
                {
                    //其处理方法与顺序地层相同，但其中关键是将顶层作为最最最新地层处理
                    //从最老地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    //将钻孔2的最新地层设置成最最最新地层
                    var orivalue = layerList[2][0][0];
                    layerList[2][0][0] = -1000;
                    var numand = 0;
                    if (lay0 > 0 & lay1 > 0 & lay2 > 0)
                    {
                        var stop = 0;
                    }
                    if (numand > 10)
                    {
                        var stop = 0;
                    }

                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == -1001 && value1 == -1001 && value2 == -1001) break;
                        //分别以钻孔0、1、2底为最老地层
                        //情况1：钻孔0最老（以钻孔0为例）
                        //情况2：钻孔0==钻孔1>钻孔2
                        //情况3：钻孔0==钻孔2>钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 > value1 && value0 > value2)
                        {
                            //跳出循环
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            //判断上一层是否与这一层一致
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔0==钻孔2>钻孔1
                        else if (value0 == value2 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //②以钻孔1底为最老地层
                        //情况1：钻孔1最老
                        else if (value1 > value0 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}


                        }
                        //情况2：钻孔0==钻孔1>钻孔2
                        else if (value0 == value1 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;

                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        //③以钻孔2底为最老地层
                        //情况1：钻孔2最老
                        else if (value2 > value0 && value2 > value1)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔2==钻孔0>钻孔1
                        else if (value2 == value0 && value0 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2>钻孔0
                        else if (value1 == value2 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == -1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = -1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = -1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = -1001;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //6、倒转地层生成的最新层与平面地质图不符，单独夹层处理
                //6.1钻孔0的最新地层为最新地层
                else if (detect == 1240)
                {
                    //从最底地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    //将钻孔0的最新地层设置成最最最老地层
                    var orivalue = layerList[0][0][0];
                    layerList[0][0][0] = 1000;
                    var numand = 0;
                    if (numand > 10)
                    {
                        var stop = 0;
                    }
                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == 1001 && value1 == 1001 && value2 == 1001) break;
                        //①分别以钻孔0、1、2底为最新地层
                        //情况1：钻孔0最新
                        //情况2：钻孔0==钻孔1<钻孔2
                        //情况3：钻孔0==钻孔2<钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 < value1 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            //判断上一层是否与这一层一致
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔0==钻孔2<钻孔1
                        else if (value0 == value2 && value1 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //②以钻孔1底为最xin地层
                        //情况1：钻孔1最xin
                        else if (value1 < value0 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;

                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay2 - 1 >= 0)
                            //{
                            //    if (layerList[2][lay2 - 1][0] == value2)
                            //    {
                            //        lay2 = lay2 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //③以钻孔2底为最xin地层
                        //情况1：钻孔2最xin
                        else if (value2 < value0 && value2 < value1)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}
                            ////判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况2：钻孔2==钻孔0<钻孔1
                        else if (value2 == value0 && value0 < value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay1 - 1 >= 0)
                            //{
                            //    if (layerList[1][lay1 - 1][0] == value1)
                            //    {
                            //        lay1 = lay1 - 1;
                            //    }
                            //}

                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                            //判断与上一层是否相同
                            //if (lay0 - 1 >= 0)
                            //{
                            //    if (layerList[0][lay0 - 1][0] == value0)
                            //    {
                            //        lay0 = lay0 - 1;
                            //    }
                            //}

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {
                            if (layerList[0][0][0] == -1 && layerList[1][0][0] == -1 && layerList[2][0][0] == -1) break;
                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //6.2钻孔1的最新地层为最新地层
                else if (detect == 1241)
                {
                    //从最底地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    //将钻孔1的最新地层设置成最最最老地层
                    var orivalue = layerList[1][0][0];
                    layerList[1][0][0] = 1000;
                    var numand = 0;
                    if (numand > 10)
                    {
                        var stop = 0;
                    }
                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == 1001 && value1 == 1001 && value2 == 1001) break;
                        //①分别以钻孔0、1、2底为最新地层
                        //情况1：钻孔0最新
                        //情况2：钻孔0==钻孔1<钻孔2
                        //情况3：钻孔0==钻孔2<钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 < value1 && value0 < value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }


                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }


                        }
                        //情况3：钻孔0==钻孔2<钻孔1
                        else if (value0 == value2 && value1 > value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //②以钻孔1底为最xin地层
                        //情况1：钻孔1最xin
                        else if (value1 < value0 && value1 < value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;

                            }

                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }


                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }
                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //③以钻孔2底为最xin地层
                        //情况1：钻孔2最xin
                        else if (value2 < value0 && value2 < value1)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //情况2：钻孔2==钻孔0<钻孔1
                        else if (value2 == value0 && value0 < value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }


                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }


                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                //6.3钻孔2的最新地层为最新地层
                else if (detect == 1242)
                {
                    //从最底地层开始
                    //找到最多层数
                    var layNum0 = layerList[0].Count;
                    var layNum1 = layerList[1].Count;
                    var layNum2 = layerList[2].Count;
                    //获取最底层的地层
                    var lay0 = layNum0 - 1;
                    var lay1 = layNum1 - 1;
                    var lay2 = layNum2 - 1;
                    //将钻孔1的最新地层设置成最最最老地层
                    var orivalue = layerList[2][0][0];
                    layerList[2][0][0] = 1000;
                    var numand = 0;
                    if (numand > 10)
                    {
                        var stop = 0;
                    }
                    while (lay0 >= 0 && lay1 >= 0 && lay2 >= 0)
                    {
                        numand = numand + 1;
                        var value0 = layerList[0][lay0][0];
                        var value1 = layerList[1][lay1][0];
                        var value2 = layerList[2][lay2][0];
                        if (value0 == 1001 && value1 == 1001 && value2 == 1001) break;
                        //①分别以钻孔0、1、2底为最新地层
                        //情况1：钻孔0最新
                        //情况2：钻孔0==钻孔1<钻孔2
                        //情况3：钻孔0==钻孔2<钻孔1
                        //情况4：钻孔0==钻孔1==钻孔2
                        if (value0 < value1 && value0 < value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }


                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }


                        }
                        //情况3：钻孔0==钻孔2<钻孔1
                        else if (value0 == value2 && value1 > value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value0;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //②以钻孔1底为最xin地层
                        //情况1：钻孔1最xin
                        else if (value1 < value0 && value1 < value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;

                            }


                        }
                        //情况2：钻孔0==钻孔1<钻孔2
                        else if (value0 == value1 && value1 < value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }


                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value1;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        //③以钻孔2底为最xin地层
                        //情况1：钻孔2最xin
                        else if (value2 < value0 && value2 < value1)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }


                        }
                        //情况2：钻孔2==钻孔0<钻孔1
                        else if (value2 == value0 && value0 < value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }


                        }
                        //情况3：钻孔1==钻孔2<钻孔0
                        else if (value1 == value2 && value0 > value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }


                        }
                        //情况4：钻孔0==钻孔1==钻孔2
                        else if (value0 == value1 && value1 == value2)
                        {

                            var value = value2;
                            var v00 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][1]);
                            var v04 = soilGrid.addVertex(layerList[0][lay0][3], layerList[0][lay0][4], layerList[0][lay0][2]);

                            var v01 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][1]);
                            var v05 = soilGrid.addVertex(layerList[1][lay1][3], layerList[1][lay1][4], layerList[1][lay1][2]);

                            var v02 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v06 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            var v03 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][1]);
                            var v07 = soilGrid.addVertex(layerList[2][lay2][3], layerList[2][lay2][4], layerList[2][lay2][2]);
                            if (value == 1000)
                            {
                                value = orivalue;
                            }
                            soilGrid.addVoxel(v00, v01, v02, v03, v04, v05, v06, v07, value, triangleID);

                            lay0 = lay0 - 1;
                            if (lay0 == -1)
                            {
                                lay0 = 0;
                                layerList[0][0][2] = layerList[0][0][1];
                                layerList[0][0][0] = 1001;

                            }
                            lay1 = lay1 - 1;
                            if (lay1 == -1)
                            {
                                lay1 = 0;
                                layerList[1][0][2] = layerList[1][0][1];
                                layerList[1][0][0] = 1001;
                            }
                            lay2 = lay2 - 1;
                            if (lay2 == -1)
                            {
                                lay2 = 0;
                                layerList[2][0][2] = layerList[2][0][1];
                                layerList[2][0][0] = 1001;
                            }

                        }
                        else
                        {
                            var error = 1;
                        }
                    }
                }
                else
                {
                    var errors = 0;
                }
            }
            return soilGrid;
        }
        public List<TriMesh> createTriMesh(FeatureCollection area, FeatureCollection points, string savepath)
        {

            //将点要素转为coordinateArray
            //这里是将所有点都生成，但由于数据量太大，所以分批生成mesh数据
            List<TriMesh> meshs = new List<TriMesh>();
            //Coordinate[] coordinateArray = new Coordinate[points.Count];
            //获得有多少col
            List<int> cols = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                cols.Add(Convert.ToInt32(points[i].Attributes["col"].ToString()));
            }
            cols = cols.Distinct().ToList();
            //单列输出（将每个要素与地层线相交，得到相交地层线），到最后要素合并
            List<Triangle> alltris = new List<Triangle>();
            var num = Math.Ceiling(Convert.ToDouble(cols.Max() / 2.0));
            for (int k = 1; k < cols.Max(); k++)
            {


                List<Coordinate> coordinateArraylist = new List<Coordinate>();
                for (int i = 0; i < points.Count; i++)
                {
                    if (Convert.ToInt32(points[i].Attributes["col"].ToString()) >= k && Convert.ToInt32(points[i].Attributes["col"].ToString()) <= (k + 1))
                    {
                        coordinateArraylist.Add(points[i].Geometry.Coordinate);
                    }
                }


                if (coordinateArraylist.Count > 0)
                {
                    //-------直接用NTS不做约束的三角网
                    Coordinate[] coordinateArray = new Coordinate[coordinateArraylist.Count];
                    for (int i = 0; i < coordinateArraylist.Count; i++)
                    {
                        coordinateArray[i] = coordinateArraylist[i];
                    }
                    //创建GeometryFactory实例
                    GeometryFactory geoFactory0 = new GeometryFactory();
                    //创建约束型三角化器
                    ConformingDelaunayTriangulationBuilder builder = new ConformingDelaunayTriangulationBuilder();
                    builder.SetSites(geoFactory0.CreateMultiPoint(coordinateArray));
                    //这里NTS库中带约束的不管用
                    //得到三角面
                    var triangles0 = builder.GetTriangles(geoFactory0);
                    //将多个三角面合并为一个面
                    UnaryUnionOp unionOp = new UnaryUnionOp(triangles0.Union());
                    var meshtri = unionOp.Union();
                    var meshface = meshtri.Envelope;
                    //对获得的面与地层线进行相交，并获得相交几何
                    List<IGeometry> insectlines = new List<IGeometry>();
                    for (int i = 0; i < area.Count; i++)
                    {
                        var insectline = meshface.Intersection(area[i].Geometry);
                        if (insectline.IsEmpty == true)
                        {
                            continue;
                        }
                        else if (insectline.NumGeometries > 0)
                        {
                            for (int j = 0; j < insectline.NumGeometries; j++)
                            {
                                var oneinsectline = insectline.GetGeometryN(j);
                                insectlines.Add(oneinsectline);
                            }
                        }
                        else
                        {
                            insectlines.Add(insectline);
                        }


                    }
                    insectlines = insectlines.Distinct().ToList();
                    //交线端点就是虚拟钻孔点,,不对，，，在最开始最地层线做了简化
                    //将线段作为约束线,将最两边端点直接填入即可
                    //List<Points> rightinpointlist = new List<Points>();
                    //for(int i=0;i < insectlines.Count; i++)
                    //{
                    //    rightinpointlist.Add(new Points(insectlines[i].Coordinates[0].X, insectlines[i].Coordinates[0].Y,0));
                    //    rightinpointlist.Add(new Points(insectlines[i].Coordinates[1].X, insectlines[i].Coordinates[1].Y, 0));
                    //}
                    //必须这样：现生成的虚拟点不在地层线上，so针对相交几何线，找到与其最临近的点，以此为两端点作为约束线
                    //所有内边界约束点集列表
                    List<Points> inpointlist = new List<Points>();
                    IList<IList<Points>> inpointlists = new List<IList<Points>>();
                    for (int i = 0; i < insectlines.Count; i++)
                    {
                        List<Points> oneinpointlist = new List<Points>();
                        for (int j = 0; j < coordinateArraylist.Count; j++)
                        {
                            if (Math.Abs(insectlines[i].Coordinates[0].Distance(coordinateArraylist[j])) < 1)
                            {
                                Points one = new Points();
                                one.x = coordinateArraylist[j].X;
                                one.y = coordinateArraylist[j].Y;
                                one.z = 0;
                                oneinpointlist.Add(one);
                                break;
                            }

                        }
                        for (int j = 0; j < coordinateArraylist.Count; j++)
                        {
                            if (Math.Abs(insectlines[i].Coordinates[insectlines[i].Coordinates.Count() - 1].Distance(coordinateArraylist[j])) < 1)
                            {
                                Points one = new Points();
                                one.x = coordinateArraylist[j].X;
                                one.y = coordinateArraylist[j].Y;
                                one.z = 0;
                                oneinpointlist.Add(one);
                                break;
                            }

                        }
                        if (oneinpointlist.Count > 1)
                        {
                            inpointlists.Add(oneinpointlist);
                            inpointlist.Add((Points)oneinpointlist[0]);
                            inpointlist.Add((Points)oneinpointlist[1]);
                        }

                    }


                    //参与构成三角网的所有点
                    List<Points> pointlist = new List<Points>();
                    List<Points> linepointlist = new List<Points>();
                    for (int i = 0; i < coordinateArraylist.Count; i++)
                    {
                        Points one = new Points();
                        one.x = coordinateArraylist[i].X;
                        one.y = coordinateArraylist[i].Y;
                        one.z = coordinateArraylist[i].Z;
                        pointlist.Add(one);
                    }
                    IDelaunay dotri = DelaunayEntry.GetDelaunay();
                    List<Triangle> tris = new List<Triangle>();
                    //创建带约束的三角网
                    TriMesh mesh = new TriMesh();
                    mesh = dotri.BuildDelaunay(pointlist, linepointlist, inpointlist, inpointlists);
                    //将单列三角网填入大三角网
                    foreach (var one in mesh.triangleList)
                    {
                        alltris.Add(one);
                    }
                }
                //将mesh按51列分开，即k是否是五十的倍数或者最后一个
                if (k != 0)
                {
                    if (k % 50 == 0 || k == cols.Max() - 1)
                    {
                        GeometryFactory geoFactory = new GeometryFactory();
                        IGeometry[] plyss = new IGeometry[alltris.Count];
                        for (int l = 0; l < alltris.Count; l++)
                        {
                            var onetri = geoFactory.CreatePolygon(new Coordinate[4]
                            { new Coordinate(alltris[l].p1.x,alltris[l].p1.y,alltris[l].p1.z),
                        new Coordinate(alltris[l].p2.x,alltris[l].p2.y,alltris[l].p2.z),
                        new Coordinate(alltris[l].p3.x,alltris[l].p3.y,alltris[l].p3.z),
                    new Coordinate(alltris[l].p1.x,alltris[l].p1.y,alltris[l].p1.z)});
                            var one = geoFactory.CreateGeometry(onetri);
                            plyss[l] = onetri;
                        }
                        var triangles = geoFactory.CreateGeometryCollection(plyss);
                        //Coordinate[] coordinateArray = new Coordinate[coordinateArraylist.Count];
                        //for (int i = 0; i < coordinateArraylist.Count; i++)
                        //{
                        //    coordinateArray[i] = coordinateArraylist[i];
                        //}
                        ////创建GeometryFactory实例
                        //GeometryFactory geoFactory = new GeometryFactory();
                        ////创建约束型三角化器
                        //ConformingDelaunayTriangulationBuilder builder = new ConformingDelaunayTriangulationBuilder();
                        //builder.SetSites(geoFactory.CreateMultiPoint(coordinateArray));
                        ////这里NTS库中带约束的不管用
                        ////得到三角面
                        //var triangles = builder.GetTriangles(geoFactory);

                        //创建基于newtin的要素并放入要素列表
                        FeatureCollection features = new FeatureCollection();
                        var onefeature = new NetTopologySuite.Features.Feature();
                        Polygon[] plys = new Polygon[triangles.Count];
                        for (int i = 0; i < triangles.Count; i++)
                        {
                            // 创建带有属性字段的要素
                            // 创建属性字段集合
                            AttributesTable attributes = new AttributesTable();
                            var feature = new NetTopologySuite.Features.Feature(triangles.Geometries[i], attributes);
                            features.Add(feature);
                            onefeature = feature;
                        }

                        //指定保存的shp路径
                        var shapefilepath = savepath + "\\meshnew" + k + ".shp";

                        // 创建ShapefileDataWriter实例，指定保存路径和GeometryFactory
                        var dataWriter = new ShapefileDataWriter(shapefilepath, new GeometryFactory());

                        // 设置Shapefile的头部信息
                        dataWriter.Header = ShapefileDataWriter.GetHeader(onefeature, features.Count);

                        // 将面要素集合写入Shapefile         
                        dataWriter.Write(features.Features);
                        TriMesh mesh = new TriMesh();
                        for (int i = 0; i < features.Count; i++)
                        {
                            mesh.AddTriangle(features[i].Geometry.Coordinates[0].X, features[i].Geometry.Coordinates[0].Y, features[i].Geometry.Coordinates[0].Z,
                                features[i].Geometry.Coordinates[1].X, features[i].Geometry.Coordinates[1].Y, features[i].Geometry.Coordinates[1].Z,
                                features[i].Geometry.Coordinates[2].X, features[i].Geometry.Coordinates[2].Y, features[i].Geometry.Coordinates[2].Z);
                        }
                        meshs.Add(mesh);
                        alltris.Clear();

                    }
                }

            }
            ////按照每51个为一个集合，分成集合列表List-coordinateArrays
            //var num = Math.Ceiling(Convert.ToDouble(cols.Max() / 50.0));
            //for (int k = 0; k < num; k++)
            //{
            //    List<Coordinate> coordinateArraylist = new List<Coordinate>();
            //    for (int i = 0; i < points.Count; i++)
            //    {
            //        if (Convert.ToInt32(points[i].Attributes["col"].ToString()) >= k * 50 && Convert.ToInt32(points[i].Attributes["col"].ToString()) <= (k + 1) * 50)
            //        {
            //            coordinateArraylist.Add(points[i].Geometry.Coordinate);
            //        }
            //    }
            //    if (coordinateArraylist.Count > 0)
            //    {
            //        //参与构成三角网的所有点
            //        List<Points> pointlist = new List<Points>();
            //        List<Points> linepointlist = new List<Points>();
            //        for (int i = 0; i < coordinateArraylist.Count; i++)
            //        {
            //            Points onepoint = new Points();
            //            onepoint.x = coordinateArraylist[i].X;
            //            onepoint.y = coordinateArraylist[i].Y;
            //            onepoint.z = coordinateArraylist[i].Z;
            //            pointlist.Add(onepoint);

            //        }

            //        Coordinate[] coordinateArray = new Coordinate[coordinateArraylist.Count];
            //        for (int i = 0; i < coordinateArraylist.Count; i++)
            //        {
            //            coordinateArray[i] = coordinateArraylist[i];
            //        }
            //        //创建GeometryFactory实例
            //        GeometryFactory geoFactory = new GeometryFactory();
            //        //创建约束型三角化器
            //        ConformingDelaunayTriangulationBuilder builder = new ConformingDelaunayTriangulationBuilder();
            //        builder.SetSites(geoFactory.CreateMultiPoint(coordinateArray));
            //        //这里NTS库中带约束的不管用
            //        //得到三角面
            //        var triangles = builder.GetTriangles(geoFactory);



            //        //创建基于newtin的要素并放入要素列表
            //        FeatureCollection features = new FeatureCollection();
            //        var onefeature = new NetTopologySuite.Features.Feature();
            //        Polygon[] plys = new Polygon[triangles.Count];
            //        for (int i = 0; i < triangles.Count; i++)
            //        {
            //            // 创建带有属性字段的要素
            //            // 创建属性字段集合
            //            AttributesTable attributes = new AttributesTable();
            //            var feature = new NetTopologySuite.Features.Feature(triangles.Geometries[i], attributes);
            //            //判断三角形中点是否在polygon内部，若不在则不添加
            //            //if (area == null)
            //            //{
            //            features.Add(feature);
            //            onefeature = feature;
            //            //}
            //            //else
            //            //{
            //            //    if (!feature.Geometry.InteriorPoint.Intersects(area[0].Geometry)) continue;
            //            //    features.Add(feature);
            //            //    onefeature = feature;
            //            //}


            //        }


            //        //指定保存的shp路径
            //        var shapefilepath = savepath + "\\mesh" + k + ".shp";

            //        // 创建ShapefileDataWriter实例，指定保存路径和GeometryFactory
            //        var dataWriter = new ShapefileDataWriter(shapefilepath, new GeometryFactory());

            //        // 设置Shapefile的头部信息
            //        dataWriter.Header = ShapefileDataWriter.GetHeader(onefeature, features.Count);

            //        // 将面要素集合写入Shapefile         
            //        dataWriter.Write(features.Features);
            //        TriMesh mesh = new TriMesh();
            //        for (int i = 0; i < features.Count; i++)
            //        {
            //            mesh.AddTriangle(features[i].Geometry.Coordinates[0].X, features[i].Geometry.Coordinates[0].Y, features[i].Geometry.Coordinates[0].Z,
            //                features[i].Geometry.Coordinates[1].X, features[i].Geometry.Coordinates[1].Y, features[i].Geometry.Coordinates[1].Z,
            //                features[i].Geometry.Coordinates[2].X, features[i].Geometry.Coordinates[2].Y, features[i].Geometry.Coordinates[2].Z);


            //        }
            //        meshs.Add(mesh);
            //    }


            //}


            return meshs;
        }
    }
}
