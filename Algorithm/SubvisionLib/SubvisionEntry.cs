using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubvisionLib
{
    public class SubvisionEntry
    {
        public static ISubvision GetSubvision()
        {
            return new Subvision();


        }
    }
}
