using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public class SQuantizedExtent3D : IQuantizedExtent3D
    {
        private readonly SQuantizedPoint3D _min;
        private readonly SQuantizedPoint3D _max;
        
        public IQuantizedPoint3D Min => _min;

        public IQuantizedPoint3D Max => _max;

        public int MinX => _min.X;

        public int MinY => _min.Y;

        public int MinZ => _min.Z;

        public int MaxX => _max.X;

        public int MaxY => _max.Y;

        public int MaxZ => _max.Z;

        public uint RangeX => (uint)(_max.X - _min.X);

        public uint RangeY => (uint)(_max.Y - _min.Y);

        public uint RangeZ => (uint)(_max.Z - _min.Z);

        public SQuantizedExtent3D(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            _min = new SQuantizedPoint3D(minX, minY, minZ);
            _max = new SQuantizedPoint3D(maxX, maxY, maxZ);
        }

        public SQuantizedExtent3D(SQuantizedPoint3D min, SQuantizedPoint3D max)
        {
            _min = min;
            _max = max;
        }

        public SQuantizedExtent3D(Extent3D extent)
        {
            _min = new SQuantizedPoint3D((int)extent.MinX, (int)extent.MinY, (int)extent.MinZ);
            _max = new SQuantizedPoint3D((int)extent.MaxX, (int)extent.MaxY, (int)extent.MaxZ);
        }

        public SQuantizedExtent3D(BinaryReader reader)
        {
            _min = reader.ReadSQuantizedPoint3D();
            _max = reader.ReadSQuantizedPoint3D();
        }
        
        public Extent3D GetExtent3D()
        {
            return new Extent3D(Min.GetPoint3D(), Max.GetPoint3D());
        }
        
        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", RangeX, RangeY, RangeZ);
        }
    }
}
