using CommonMethodHelpLib;
using NetTopologySuite.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GeoMapModelLib
{
    public  class GeoMapRockModeling
    {
        public static void BuildGeoMapRock(string stratumLayerPath,string stratumLinePath, string faultLayerPath, string demPath, string altitudePointPath, string contourLinePath, double xMax, double xMin, double yMin, double yMax, string direction, double stepLength, string savePath, double elevSampleStep, int zZoom, int scale, string solidtypetxt, double  resety,double  virstep)
        {
            DirectoryHelper directoryHelper = new DirectoryHelper();
            directoryHelper.BuildDirectory(savePath);
            //0数据读取
            ShapeFileHelper stratumLayer = new ShapeFileHelper(stratumLayerPath);
            ShapeFileHelper faultLayer = new ShapeFileHelper(faultLayerPath);
            ShapeFileHelper altitudePoint = new ShapeFileHelper(altitudePointPath);
            ShapeFileHelper contourLine = new ShapeFileHelper(contourLinePath);
            ShapeFileHelper stratumLine = new ShapeFileHelper(stratumLinePath);
            RasterHelper dem = new RasterHelper(demPath);
            //1自动生成剖面线
            Console.WriteLine("orionlines are building...");
            OrigonLineHelper origonLineHelper=new OrigonLineHelper();
            directoryHelper.BuildDirectory(savePath + "//origonLine");
            var orionlines=origonLineHelper.AutoCreateOriLine(stratumLayer, faultLayer, xMax, xMin, yMin, yMax, direction, stepLength, savePath+ "//origonLine");
            Console.WriteLine("orionlines are ready!");
            //2生成地层剖面
            Console.WriteLine("SectionLineAndPolygon are building...");
            SectionLineAndPolygonhelper sectionLineAndPolygonhelper=new SectionLineAndPolygonhelper();
            var linesAndpolysPath = sectionLineAndPolygonhelper.CreateSectionLineAndPolygon(orionlines,  dem,  altitudePoint,  contourLine, faultLayer,  stratumLine, stratumLayer,  savePath, stepLength,  elevSampleStep,  zZoom,  scale);
            Console.WriteLine("SectionLineAndPolygon are ready!");
            //3生成虚拟钻孔点
            Console.WriteLine("virtuallDrills are buliding...");
            VirtuallDrillsHelper virtuallDrillsHelper = new VirtuallDrillsHelper();
            virtuallDrillsHelper.CreateVirtuallDrills(linesAndpolysPath, solidtypetxt, savePath, resety, virstep);
            Console.WriteLine("virtuallDrills are ready!");
            //4构建三维地质体
            Console.WriteLine("rockModel is buliding...");
            RockModelHelper rockModelHelper = new RockModelHelper();
            rockModelHelper.CreateRockModel(savePath, savePath + "//connectDrills.shp", savePath + "//profileDrills.shp", stratumLayer, stratumLine, solidtypetxt);
            Console.WriteLine("rockModel is ready!");
        }
        
    }
}
