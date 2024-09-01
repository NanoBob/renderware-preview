using RenderWareIo.Structs.Dff;
using RenderWareIo.Structs.Dff.Plugins;
using RenderWarePreviewer.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace RenderWarePreviewer.Helpers;

public record OffsetGeometry(GeometryModel3D model, Vector3 position, Vector3 rotation);
public record OffsetMeshGeometry(MeshGeometry3D model, Vector3 position, Vector3 rotation);

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

    public static IEnumerable<OffsetGeometry> GetModels(
        Dff dff,
        Dictionary<string, SixLabors.ImageSharp.Image<Rgba32>> images,
        bool useBinMeshPlugin = false)
    {
        var models = new List<OffsetGeometry>();
        foreach (var atomic in dff.Clump.Atomics)
        {
            var geometry = dff.Clump.GeometryList.Geometries[(int)atomic.GeometryIndex];
            var frame = dff.Clump.FrameList.Frames[(int)atomic.FrameIndex];

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

                    var isStrip = (binMesh.Flags & 0x01) != 0;
                    if (images.ContainsKey(materialName))
                        models.Add(GetModel(GetMesh(geometry, frame, strip, isStrip), images[materialName]));
                    else
                        models.Add(GetModel(GetMesh(geometry, frame, strip, isStrip), MissingTexture));
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
                        models.Add(GetModel(GetMesh(geometry, frame, index), images[materialName]));
                    else
                        models.Add(GetModel(GetMesh(geometry, frame, index), MissingTexture));
                }
            }
        }

        return models;
    }

    public static OffsetGeometry GetModel(OffsetMeshGeometry mesh, SixLabors.ImageSharp.Image<Rgba32>? image)
    {
        return GetModel(mesh, GetMaterial(image));
    }

    public static OffsetGeometry GetModel(OffsetMeshGeometry mesh, System.Windows.Media.Media3D.Material material)
    {
        return new OffsetGeometry(new GeometryModel3D
        {
            Geometry = mesh.model,
            Material = material
        }, mesh.position, mesh.rotation);
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

    public static OffsetMeshGeometry GetMesh(RenderWareIo.Structs.Dff.Geometry geometry, Frame frame, BinMeshStrip strip, bool isStrip)
    {
        Dictionary<int, int> vertexTranslationMap = new();
        Dictionary<int, int> reverseVertexTranslationMap = new();
        List<Vector3> vertices = new();

        var mesh = new MeshGeometry3D();

        if (geometry.MorphTargets.First().Vertices.Any())
        {
            foreach (var index in strip.Indices)
            {
                var vertex = geometry.MorphTargets.First().Vertices[(int)index];
                vertexTranslationMap[vertices.Count] = (int)index;
                reverseVertexTranslationMap[(int)index] = vertices.Count;
                vertices.Add(new Vector3(vertex.X, vertex.Y, vertex.Z));
            }

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

            var indices = strip.Indices.Select(x => reverseVertexTranslationMap[(int)x]).ToArray();

            if (isStrip)
            {
                for (var index = 2; index < indices.Length; index++)
                {
                    if (index % 2 == 0)
                    {
                        mesh.TriangleIndices.Add(indices[index - 2]);
                        mesh.TriangleIndices.Add(indices[index - 1]);
                        mesh.TriangleIndices.Add(indices[index]);
                    }
                    else
                    {
                        mesh.TriangleIndices.Add(indices[index]);
                        mesh.TriangleIndices.Add(indices[index - 1]);
                        mesh.TriangleIndices.Add(indices[index - 2]);
                    }
                }
            } else
            {
                foreach (var index in indices)
                    mesh.TriangleIndices.Add(index);
            }

        }

        foreach (var normal in geometry.MorphTargets.SelectMany(y => y.Normals))
            mesh.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));

        return new (mesh, frame.Position, GetFrameRotation(frame));
    }

    public static OffsetMeshGeometry GetMesh(RenderWareIo.Structs.Dff.Geometry geometry, Frame frame, int materialIndex)
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
                
        return new(mesh, frame.Position, GetFrameRotation(frame));
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

    private static Vector3 GetFrameRotation(Frame frame)
    {
        float[,] matrix =
        {
            { frame.Rot1.X, frame.Rot1.Y, frame.Rot1.Z },
            { frame.Rot2.X, frame.Rot2.Y, frame.Rot2.Z },
            { frame.Rot3.X, frame.Rot3.Y, frame.Rot3.Z },
        };

        var theta = MathF.Asin(-matrix[2, 0]);
        var cosTheta = MathF.Cos(theta);

        float phi, psi;

        if (cosTheta != 0)
        {
            phi = MathF.Atan2(matrix[2, 1], matrix[2, 2]);
            psi = MathF.Atan2(matrix[1, 0], matrix[0, 0]);
        }
        else
        {
            phi = 0;
            psi = MathF.Atan2(-matrix[0, 1], matrix[1, 1]);
        }

        var phiDegrees = phi * (180.0f / MathF.PI);
        var thetaDegrees = theta * (180.0f / MathF.PI);
        var psiDegrees = psi * (180.0f / MathF.PI);

        return -new Vector3(phiDegrees, thetaDegrees, psiDegrees);
    }
}
