using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonDataStructureLib
{
    public class SoilGrid
    {
        public Dictionary<int, double[]> vertexDict { get; set; }
        public List<double[]> voxelCollection { get; set; }
        public SoilGrid()
        {
            vertexDict = new Dictionary<int, double[]>();
            voxelCollection = new List<double[]>();
        }

        public int addVertex(double x, double y, double z)
        {
            int id = vertexDict.Count;
            double[] vertex = { id, x, y, z };

            vertexDict.Add(id, vertex);


            return id;
        }
        public double[] findVertexByID(int id)
        {
            if (id < vertexDict.Count)
            {
                return vertexDict[id];
            }
            else
            {
                return null;
            }
        }
        public int addVoxel(int v0, int v1, int v2, int v3, int v4, int v5, int v6, int v7, double value, int profileId)
        {
            var id = voxelCollection.Count;
            double[] voxel = { id, v0, v1, v2, v3, v4, v5, v6, v7, value, profileId, 0 };
            voxelCollection.Add(voxel);
            return id;

        }
    }
}
