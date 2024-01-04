using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DelaunayLib
{
    public class DelaunayEntry
    {
        public static IDelaunay GetDelaunay()
        {
            return new Delaunay();
        }
    }
}
