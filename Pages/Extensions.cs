using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUMonitor.Pages
{
    public static class Extensions
    {
        public static bool AlmostEqual(this double a, double b)
        {
            return Math.Abs(a - b) <= 10e-2;
        }
    }
}
