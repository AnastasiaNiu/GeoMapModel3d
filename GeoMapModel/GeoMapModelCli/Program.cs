using GeoMapModelLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoMapModelCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //命令行输入配置文件路径
            Console.WriteLine("Please enter the configuration file path:");
            string jsonpath = Console.ReadLine();
            Console.WriteLine("Modeling in progress...");
            //1、读取数据
            string json = File.ReadAllText(jsonpath);
            ReadData data = JsonConvert.DeserializeObject<ReadData>(json);
            for (int i = 0; i < data.Pathname.Count; i++)
            {
                string stratumLayerPath = data.Pathname[i].stratumLayerPath;
                string stratumLinePath = data.Pathname[i].stratumLinePath;
                string faultLayerPath = data.Pathname[i].faultLayerPath;
                string altitudePointPath = data.Pathname[i].altitudePointPath;
                string contourLinePath = data.Pathname[i].contourLinePath;
                string savePath = data.Pathname[i].savePath;
                string demPath = data.Pathname[i].demPath;
                string solidtypetxt=data.Pathname[i].solidtypetxt;
                //2、参数设置
                double xMax = data.Pathname[i].xMax;
                double xMin = data.Pathname[i].xMin;
                double yMin = data.Pathname[i].yMin;
                double yMax = data.Pathname[i].yMax;
                double resety= data.Pathname[i].VirtualDrillResetY;
                double virstep= data.Pathname[i].VirtualDrillStep;
                double elevSampleStep = data.Pathname[i].elevSampleStep;
                string direction = data.Pathname[i].direction;
                double stepLength = data.Pathname[i].stepLength;
                int zZoom= data.Pathname[i].zZoom;
                int scale= data.Pathname[i].scale;
                GeoMapRockModeling.BuildGeoMapRock( stratumLayerPath,  stratumLinePath,  faultLayerPath,  demPath,  altitudePointPath,  contourLinePath,  xMax,  xMin,  yMin,  yMax,  direction,  stepLength,  savePath,   elevSampleStep,  zZoom,  scale,  solidtypetxt,   resety,   virstep);
            }
            Console.WriteLine("Modeling successful!");
            Console.ReadLine();
        }
    }
}
