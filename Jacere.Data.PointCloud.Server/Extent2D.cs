using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public class Extent2D
    {
        private const double ErrorBound = 0.005;

        public readonly double MinX;
        public readonly double MinY;

        public readonly double MaxX;
        public readonly double MaxY;

        public Extent2D(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
        
        public double RangeX => MaxX - MinX;

        public double RangeY => MaxY - MinY;

        public double MidpointX => (MaxX + MinX) / 2;

        public double MidpointY => (MaxY + MinY) / 2;

        public double Area => RangeX * RangeY;

        public double Aspect => RangeX / RangeY;

        public bool Contains(Extent2D extent)
        {
            return extent.MinX >= MinX && extent.MaxX <= MaxX && extent.MinY >= MinY && extent.MaxY <= MaxY;
        }

        public bool Contains(double x, double y)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
        }

        public bool Contains(double x, double y, bool errorBound)
        {
            var eb = errorBound ? ErrorBound : 0.0f;

            // todo: Implement comparison operator for float/double equals. The below may work in practice but in general it is unsafe due to mantisa/exponent ratios.
            return ((MinX - eb) <= x) && (x <= (MaxX + eb)) && ((MinY - eb) <= y) && (y <= (MaxY + eb));
        }
        
        public override string ToString()
        {
            return string.Format("({0:f}, {1:f})", RangeX, RangeY);
        }
    }
}
