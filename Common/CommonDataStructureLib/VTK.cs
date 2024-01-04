using Kitware.VTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public  class VTK
    {
        public vtkUnstructuredGrid createVtkFromGridForValue(string fileName, string title, SoilGrid soilGrids_total, double value)
        {
            List<List<double>> pointList = new List<List<double>>();
            foreach (var item in soilGrids_total.vertexDict.Values)
            {
                List<double> temp_point = new List<double>();
                temp_point.Add(item[1]);
                temp_point.Add(item[2]);
                temp_point.Add(item[3]);
                pointList.Add(temp_point);
            }
            List<List<double>> voxelIDlist = new List<List<double>>();
            List<double> cellDataList = new List<double>();
            foreach (var item in soilGrids_total.voxelCollection)
            {
                if (item[9] == value)
                {
                    List<double> voxelID_temp = new List<double>();
                    for (int m = 1; m < 9; m++)
                    {
                        voxelID_temp.Add(item[m]);
                    }
                    voxelIDlist.Add(voxelID_temp);
                    cellDataList.Add(item[9]);
                }
                else
                {
                    continue;
                }
            }
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            if (voxelIDlist.Count != 0)
            {
                grid = createUstructuredGrid(fileName, title, pointList, voxelIDlist, cellDataList);
            }
            return grid;
        }

        public vtkUnstructuredGrid createUstructuredGrid(string fileName, string title, List<List<double>> pointList, List<List<double>> voxelIDlist, List<double> cellDataList)
        {

            // 创建点
            vtkPoints points = vtkPoints.New();
            for (int i = 0; i < pointList.Count; i++)
            {

                points.InsertNextPoint(pointList[i][0], pointList[i][1], pointList[i][2]);

            }

            // 创建单元

            vtkCellArray cells = vtkCellArray.New();
            for (int i = 0; i < voxelIDlist.Count; i++)
            {
                // 创建单元
                vtkIdList idlist0 = vtkIdList.New();
                idlist0.InsertId(0, Convert.ToInt32(voxelIDlist[i][0]));
                idlist0.InsertId(1, Convert.ToInt32(voxelIDlist[i][1]));
                idlist0.InsertId(2, Convert.ToInt32(voxelIDlist[i][2]));
                idlist0.InsertId(3, Convert.ToInt32(voxelIDlist[i][3]));
                vtkIdList idlist1 = vtkIdList.New();
                idlist1.InsertId(0, Convert.ToInt32(voxelIDlist[i][0]));
                idlist1.InsertId(1, Convert.ToInt32(voxelIDlist[i][1]));
                idlist1.InsertId(2, Convert.ToInt32(voxelIDlist[i][4]));
                idlist1.InsertId(3, Convert.ToInt32(voxelIDlist[i][5]));
                vtkIdList idlist2 = vtkIdList.New();
                idlist2.InsertId(0, Convert.ToInt32(voxelIDlist[i][2]));
                idlist2.InsertId(1, Convert.ToInt32(voxelIDlist[i][3]));
                idlist2.InsertId(2, Convert.ToInt32(voxelIDlist[i][7]));
                idlist2.InsertId(3, Convert.ToInt32(voxelIDlist[i][6]));
                vtkIdList idlist3 = vtkIdList.New();
                idlist3.InsertId(0, Convert.ToInt32(voxelIDlist[i][0]));
                idlist3.InsertId(1, Convert.ToInt32(voxelIDlist[i][2]));
                idlist3.InsertId(2, Convert.ToInt32(voxelIDlist[i][4]));
                idlist3.InsertId(3, Convert.ToInt32(voxelIDlist[i][6]));
                vtkIdList idlist4 = vtkIdList.New();
                idlist4.InsertId(0, Convert.ToInt32(voxelIDlist[i][0]));
                idlist4.InsertId(1, Convert.ToInt32(voxelIDlist[i][3]));
                idlist4.InsertId(2, Convert.ToInt32(voxelIDlist[i][4]));
                idlist4.InsertId(3, Convert.ToInt32(voxelIDlist[i][7]));
                vtkIdList idlist5 = vtkIdList.New();
                idlist5.InsertId(0, Convert.ToInt32(voxelIDlist[i][4]));
                idlist5.InsertId(1, Convert.ToInt32(voxelIDlist[i][5]));
                idlist5.InsertId(2, Convert.ToInt32(voxelIDlist[i][6]));
                idlist5.InsertId(3, Convert.ToInt32(voxelIDlist[i][7]));
                vtkIdList idlist6 = vtkIdList.New();
                idlist6.InsertId(0, Convert.ToInt32(voxelIDlist[i][1]));
                idlist6.InsertId(1, Convert.ToInt32(voxelIDlist[i][2]));
                idlist6.InsertId(2, Convert.ToInt32(voxelIDlist[i][5]));
                idlist6.InsertId(3, Convert.ToInt32(voxelIDlist[i][6]));

                cells.InsertNextCell(idlist0);
                cells.InsertNextCell(idlist1);
                cells.InsertNextCell(idlist2);
                cells.InsertNextCell(idlist3);
                //cells.InsertNextCell(idlist4);
                cells.InsertNextCell(idlist5);
                cells.InsertNextCell(idlist6);
            }


            // 创建网格
            vtkUnstructuredGrid grid = vtkUnstructuredGrid.New();
            grid.SetPoints(points);
            grid.SetCells(6, cells);

            // 创建写入器并将网格写入文件
            vtkXMLUnstructuredGridWriter writer = vtkXMLUnstructuredGridWriter.New();
            writer.SetFileName(fileName);
            writer.SetInput(grid);
            writer.Write();
            vtkXMLUnstructuredGridReader reader = vtkXMLUnstructuredGridReader.New();
            reader.SetFileName(fileName);
            reader.Update();
            return grid;
        }
    }
}
