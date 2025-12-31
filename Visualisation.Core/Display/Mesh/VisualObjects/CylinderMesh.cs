using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

// ex. https://www.songho.ca/opengl/gl_cylinder.html

public sealed class CylinderMesh : IMesh
{
    public const int SectorCount = 32;
    public const int StackCount = 5;
    private static readonly string MeshName = nameof(CylinderMesh);

    private static MeshManager.MeshData? s_meshData;
    private static VertexData[]? s_vertices;
    private static uint[]? s_indices;

    public CylinderMesh()
    {
        if (s_vertices is null)
        {
            GenerateCylinder();
        }

        s_meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            Debug.Assert(s_vertices is not null);
            Debug.Assert(s_indices is not null);

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, s_vertices.Length * 14 * sizeof(float), s_vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, s_indices.Length * sizeof(uint), s_indices,
                BufferUsageHint.StaticDraw);

            VertexData.VertexAttribPositionNormalTexCoordsTangentBitangent();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            return new MeshManager.MeshData { MeshName = MeshName, Vbo = vbo, Vao = vao, Ebo = ebo };
        });
    }

    public void Dispose()
    {
        MeshManager.FreeMesh(MeshName, data =>
        {
            Debug.Assert(data.Ebo is not null);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteBuffer(data.Ebo.Value);
            GL.DeleteVertexArray(data.Vao);

            s_meshData = null;
            s_vertices = null;
            s_indices = null;
        });
    }

    public void Render()
    {
        if (s_meshData is null || s_vertices is null || s_indices is null)
            throw new MeshDataEmptyException();

        GL.BindVertexArray(s_meshData.Vao);
        GL.DrawElements(PrimitiveType.Triangles, s_indices.Length, DrawElementsType.UnsignedInt, 0);
    }

    private static void GenerateCylinder()
    {
        var unitCircleVertices = GenerateUnitCircleVertices();
        var sideVertices = GenerateSideVertices(unitCircleVertices);
        var topAndBottomVertices = GenerateTopAndBottomVertices(unitCircleVertices);

        s_vertices = [..sideVertices, ..topAndBottomVertices];
        s_indices = GenerateIndices();

        s_vertices.CalculateTangentBitangent(s_indices);
    }

    private static uint[] GenerateIndices()
    {
        var indices = new List<uint>();

        // Side indices
        for (int i = 0; i < StackCount; ++i)
        {
            uint k1 = (uint)(i * (SectorCount + 1));
            uint k2 = k1 + SectorCount + 1;

            for (var j = 0; j < SectorCount; ++j)
            {
                var nextJ = (uint)(j + 1);

                indices.Add(k1 + (uint)j);
                indices.Add(k1 + nextJ);
                indices.Add(k2 + (uint)j);

                indices.Add(k2 + (uint)j);
                indices.Add(k2 + nextJ);
                indices.Add(k1 + nextJ);
            }
        }

        var sideVertexCount = (uint)((StackCount + 1) * (SectorCount + 1));

        // Bottom cap indices (h=-0.5)
        var bottomCenterIndex = sideVertexCount;
        var bottomRingStartIndex = bottomCenterIndex + 1;
        for (var i = 0; i < SectorCount; ++i)
        {
            var nextI = (uint)((i + 1) % SectorCount);
            indices.Add(bottomCenterIndex);
            indices.Add(bottomRingStartIndex + (uint)i);
            indices.Add(bottomRingStartIndex + nextI);
        }

        // Top cap indices (h=0.5)
        var topCenterIndex = sideVertexCount + (SectorCount + 1);
        var topRingStartIndex = topCenterIndex + 1;
        for (var i = 0; i < SectorCount; ++i)
        {
            var nextI = (uint)((i + 1) % SectorCount);
            indices.Add(topCenterIndex);
            indices.Add(topRingStartIndex + (uint)i);
            indices.Add(topRingStartIndex + nextI);
        }

        return indices.ToArray();
    }

    private static VertexData[] GenerateSideVertices(Vector2[] unitCircleVertices)
    {
        var vertices = new List<VertexData>();
        for (var i = 0; i <= StackCount; ++i)
        {
            var h = -0.5f + (float)i / StackCount;
            var t = 1.0f - (float)i / StackCount;
            for (var j = 0; j <= SectorCount; ++j)
            {
                var position = new Vector3(unitCircleVertices[j].X, unitCircleVertices[j].Y, h);
                var normal = new Vector3(unitCircleVertices[j].X, unitCircleVertices[j].Y, 0.0f).Normalized();
                var textureUv = new Vector2((float)j / SectorCount, t);
                vertices.Add(new VertexData(position.X, position.Y, position.Z, normal.X, normal.Y, normal.Z,
                    textureUv.X, textureUv.Y));
            }
        }

        return vertices.ToArray();
    }

    private static VertexData[] GenerateTopAndBottomVertices(Vector2[] unitCircleVertices)
    {
        var vertices = new List<VertexData>();

        for (int i = 0; i < 2; ++i)
        {
            float h = -0.5f + 1.0f * i; // [-0.5f, 0.5f]
            float nz = -1.0f + 2.0f * i; // [-1.0f, 1.0f]

            // center point
            vertices.Add(new VertexData(0, 0, h, 0, 0, nz, 0.5f, 0.5f));
            for (var j = 0; j < SectorCount; ++j)
            {
                var position = new Vector3(unitCircleVertices[j].X, unitCircleVertices[j].Y, h);
                var normal = new Vector3(0, 0, nz);
                var textureUV = new Vector2(-unitCircleVertices[j].X * 0.5f + 0.5f,
                    -unitCircleVertices[j].Y * 0.5f + 0.5f);

                vertices.Add(new VertexData(position.X, position.Y, position.Z, normal.X, normal.Y, normal.Z,
                    textureUV.X, textureUV.Y));
            }
        }

        return vertices.ToArray();
    }

    public static Vector2[] GenerateUnitCircleVertices()
    {
        var unitCircleVertices = new Vector2[SectorCount + 1];
        var angleStep = 2 * MathF.PI / SectorCount;
        for (var i = 0; i <= SectorCount; ++i)
        {
            var theta = i * angleStep;
            unitCircleVertices[i].X = MathF.Cos(theta);
            unitCircleVertices[i].Y = MathF.Sin(theta);
        }

        return unitCircleVertices;
    }
}