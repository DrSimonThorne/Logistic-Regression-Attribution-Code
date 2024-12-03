using System;
using System.Collections.Generic;
using System.Text;

namespace CoreForecast.ClassFiles.Extensions
{
    static class IsFinite
    {
        public static bool isFinite(this float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
        public static bool isFinite(this double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }
    }
}
