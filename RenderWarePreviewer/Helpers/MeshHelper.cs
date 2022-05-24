using RenderWareIo.Structs.Dff;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RenderWarePreviewer.Helpers
{
    public static class MeshHelper
    {
        private static Dictionary<string, MeshGeometry3D> pyramids = new();

        public static GeometryModel3D GetModel(Dff dff, SixLabors.ImageSharp.Image<Rgba32>? image)
        {
            return GetModel(GetMesh(dff), image);
        }

        public static GeometryModel3D GetModel(MeshGeometry3D mesh, SixLabors.ImageSharp.Image<Rgba32>? image)
        {
            return GetModel(mesh, GetMaterial(image));
        }

        public static GeometryModel3D GetModel(MeshGeometry3D mesh, System.Windows.Media.Media3D.Material material)
        {
            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material
            };
        }

        public static GeometryModel3D GetPyramid(System.Windows.Media.Color color, float scale)
        {
            var key = $"{color}{scale}";
            if (pyramids.ContainsKey(key))
                return GetModel(pyramids[key], GetMaterial(color));

            var mesh = new MeshGeometry3D();
            foreach (var position in new Point3D[]
            {
                new Point3D(-scale, -scale, 0),
                new Point3D(scale, -scale, 0),
                new Point3D(scale, scale, 0),
                new Point3D(-scale, scale, 0),
                new Point3D(0, 0, scale),
            })
                mesh.Positions.Add(position);

            foreach (var triangleIndex in new int[]
            {
                0, 1, 2,
                0, 2, 3,
                0, 1, 4,
                1, 2, 4,
                2, 3, 4,
                3, 0, 4
            })
                mesh.TriangleIndices.Add(triangleIndex);

            foreach (var _ in mesh.Positions)
                mesh.Normals.Add(new Vector3D(0, 0, 1));

            pyramids[key] = mesh;

            return GetModel(mesh, GetMaterial(color));
        }

        public static DiffuseMaterial GetMaterial(SixLabors.ImageSharp.Image<Rgba32>? image)
        {
            var material = new DiffuseMaterial
            {
                Color = System.Windows.Media.Color.FromArgb(255, 255, 255, 255)
            };
            if (image != null)
            {
                material.Brush = new ImageBrush(GetBitMap(image));
            }
            else
            {
                material.Brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 150, 150, 150));
            }
            return material;
        }

        public static DiffuseMaterial GetMaterial(System.Windows.Media.Color color)
        {
            var material = new DiffuseMaterial
            {
                Color = System.Windows.Media.Color.FromArgb(255, 255, 255, 255),
                Brush = new SolidColorBrush(color)
            };
            return material;
        }

        public static MeshGeometry3D GetMesh(Dff dff)
        {
            var mesh = new MeshGeometry3D();
            foreach (var vertex in dff.Clump.GeometryList.Geometries.SelectMany(x => x.MorphTargets.SelectMany(y => y.Vertices)))
            {
                mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
            }

            foreach (var triangle in dff.Clump.GeometryList.Geometries.SelectMany(x => x.Triangles))
            {
                mesh.TriangleIndices.Add(triangle.VertexIndexThree);
                mesh.TriangleIndices.Add(triangle.VertexIndexTwo);
                mesh.TriangleIndices.Add(triangle.VertexIndexOne);
            }

            foreach (var normal in dff.Clump.GeometryList.Geometries.SelectMany(x => x.MorphTargets.SelectMany(y => y.Normals)))
            {
                mesh.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));
            }

            foreach (var uv in dff.Clump.GeometryList.Geometries.SelectMany(x => x.TexCoords))
            {
                mesh.TextureCoordinates.Add(new Point(uv.X, uv.Y));
            }
            return mesh;
        }

        private static BitmapSource GetBitMap(SixLabors.ImageSharp.Image<Rgba32> image)
        {
            Rgba32[] rgbaArray = new Rgba32[image.Width * image.Height];
            Span<Rgba32> span = new Span<Rgba32>(rgbaArray, 0, rgbaArray.Length);
            image.CopyPixelDataTo(span);
            var data = MemoryMarshal.AsBytes(span).ToArray();
            var bitmap = new RgbaBitmapSource(data, image.Width);
            //File.WriteAllBytes("output.rgba", data);

            return bitmap;
        }
    }
}
