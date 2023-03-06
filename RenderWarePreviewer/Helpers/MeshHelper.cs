using RenderWareIo.Structs.Dff;
using RenderWareIo.Structs.Dff.Plugins;
using RenderWareIo.Structs.Ide;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RenderWarePreviewer.Helpers
{
    public static class MeshHelper
    {
        private static readonly Dictionary<string, MeshGeometry3D> pyramids = new();
        public static Image<Rgba32> MissingTexture { get; } = new(64, 64);

        static MeshHelper()
        {
            var black = true;
            for (int x = 0; x < MissingTexture.Width; x++)
            {
                for (int y = 0; y < MissingTexture.Width; y++)
                {
                    MissingTexture[x, y] = black ? new Rgba32(0, 0, 0) : new Rgba32(255, 0, 255);

                    black = !black;
                }
                black = !black;
            }
        }

        public static IEnumerable<GeometryModel3D> GetModels(
            Dff dff, 
            Dictionary<string, SixLabors.ImageSharp.Image<Rgba32>> images,
            bool useBinMeshPlugin = false)
        {
            var models = new List<GeometryModel3D>();
            foreach (var geometry in dff.Clump.GeometryList.Geometries)
            {
                if (geometry.Extension.Extensions.Any(x => x is BinMesh) && useBinMeshPlugin)
                {
                    var binMesh = (geometry.Extension.Extensions.First(x => x is BinMesh) as BinMesh)!;
                    foreach (var strip in binMesh.BinMeshStrips)
                    {
                        var index = (int)strip.MaterialIndex;
                        if (index >= geometry.MaterialList.Materials.Count)
                            index = 0;

                        var material = geometry.MaterialList.Materials[index];
                        var materialName = AssetHelper.SanitizeName(material.Texture.Name.Value);

                        if (images.ContainsKey(materialName))
                            models.Add(GetModel(GetMesh(geometry, strip), images[materialName]));
                        else
                            models.Add(GetModel(GetMesh(geometry, strip), MissingTexture));
                    }
                }
                else
                {
                    var materialIndices = geometry.Triangles
                        .Select(x => x.MaterialIndex)
                        .Distinct();

                    foreach (var materialIndex in materialIndices)
                    {
                        var index = materialIndex;
                        if (materialIndex >= geometry.MaterialList.Materials.Count)
                            index = 0;

                        var material = geometry.MaterialList.Materials[index];
                        var materialName = AssetHelper.SanitizeName(material.Texture.Name.Value);
                        if (images.ContainsKey(materialName))
                            models.Add(GetModel(GetMesh(geometry, index), images[materialName]));
                        else
                            models.Add(GetModel(GetMesh(geometry, index), MissingTexture));
                    }
                }
            }

            return models;
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
                material.Brush = new ImageBrush(GetBitMap(image))
                {
                    TileMode = TileMode.Tile,
                    ViewportUnits = BrushMappingMode.Absolute
                };
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

        public static MeshGeometry3D GetMesh(RenderWareIo.Structs.Dff.Geometry geometry, BinMeshStrip strip)
        {
            Dictionary<int, int> vertexTranslationMap = new();
            Dictionary<int, int> reverseVertexTranslationMap = new();
            List<Vector3> vertices = new();

            foreach (var index in strip.Indices)
            {
                var vertex = geometry.MorphTargets.First().Vertices[(int)index];
                vertexTranslationMap[vertices.Count] = (int)index;
                reverseVertexTranslationMap[(int)index] = vertices.Count;
                vertices.Add(new Vector3(vertex.X, vertex.Y, vertex.Z));
            }

            var mesh = new MeshGeometry3D();
            foreach (var vertex in vertices)
                mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));

            if (geometry.TexCoords.Any())
            {
                var uvs = vertices
                    .Select((vector, index) => geometry.TexCoords.ElementAt(vertexTranslationMap[index]))
                    .Select(x => new Vector2(x.X, x.Y));

                foreach (var uv in uvs)
                    mesh.TextureCoordinates.Add(new System.Windows.Point(uv.X, uv.Y));
            }

            foreach (var index in strip.Indices.Select(x => reverseVertexTranslationMap[(int)x]))
                mesh.TriangleIndices.Add(index);

            //foreach (var normal in geometry.MorphTargets.SelectMany(y => y.Normals))
            //    mesh.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));

            return mesh;
        }

        public static MeshGeometry3D GetMesh(RenderWareIo.Structs.Dff.Geometry geometry, int materialIndex)
        {
            var mesh = new MeshGeometry3D();
            foreach (var vertex in geometry.MorphTargets.SelectMany(y => y.Vertices))
                mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));

            foreach (var triangle in geometry.Triangles.Where(x => x.MaterialIndex == materialIndex))
            {
                mesh.TriangleIndices.Add(triangle.VertexIndexThree);
                mesh.TriangleIndices.Add(triangle.VertexIndexTwo);
                mesh.TriangleIndices.Add(triangle.VertexIndexOne);
            }

            foreach (var normal in geometry.MorphTargets.SelectMany(y => y.Normals))
                mesh.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));

            foreach (var uv in geometry.TexCoords)
                mesh.TextureCoordinates.Add(new System.Windows.Point(uv.X, uv.Y));

            return mesh;
        }

        private static BitmapSource GetBitMap(SixLabors.ImageSharp.Image<Rgba32> image)
        {
            Rgba32[] rgbaArray = new Rgba32[image.Width * image.Height];
            Span<Rgba32> span = new Span<Rgba32>(rgbaArray, 0, rgbaArray.Length);
            image.CopyPixelDataTo(span);
            var data = MemoryMarshal.AsBytes(span).ToArray();
            var bitmap = new RgbaBitmapSource(data, image.Width);
            //System.IO.File.WriteAllBytes("output.rgba", data);

            return bitmap;
        }
    }
}
