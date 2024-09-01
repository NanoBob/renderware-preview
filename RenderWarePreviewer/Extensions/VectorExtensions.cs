using System.Numerics;
using System.Windows.Media.Media3D;

namespace RenderWarePreviewer.Extensions;

public static class VectorExtensions
{
    public static Vector3 ToVector3(this Vector3D value) => new Vector3((float)value.X, (float)value.Y, (float)value.Z);
    public static Vector3D ToVector3D(this Vector3 value) => new Vector3D(value.X, value.Y, value.Z);

    public static Vector3 ToVector3(this Point3D value) => new Vector3((float)value.X, (float)value.Y, (float)value.Z);
    public static Point3D ToPoint3D(this Vector3 value) => new Point3D(value.X, value.Y, value.Z);
}
