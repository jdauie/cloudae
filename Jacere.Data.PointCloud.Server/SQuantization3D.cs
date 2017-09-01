using System.IO;

namespace Jacere.Data.PointCloud.Server
{
    public class SQuantization3D : Quantization3D<SQuantizedPoint3D, SQuantizedExtent3D>
    {
        public SQuantization3D(double sfX, double sfY, double sfZ, double oX, double oY, double oZ)
            : base(sfX, sfY, sfZ, oX, oY, oZ)
        {
        }

        public SQuantization3D(BinaryReader reader)
            : base(reader)
        {
        }
    }
}
