using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterpolateLib
{
    public class InterpolateEntry
    {

        public static IInterpolate GetInterpolate()
        {
            return new Interpolate();


        }
    }
}
