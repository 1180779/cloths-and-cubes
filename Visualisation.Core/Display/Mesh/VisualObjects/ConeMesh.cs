using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class ConeMesh : IMesh
{
    public const int SectorCount = 32;
    public const int StackCount = 5;
    private static readonly string MeshName = nameof(ConeMesh);

    private static MeshManager.MeshData? s_meshData;
    private static VertexData[]? s_vertices;
    private static uint[]? s_indices;

    public ConeMesh()
    {
        if (s_vertices is null)
        {
            GenerateCone();
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

    private static void GenerateCone()
    {
        var unitCircleVertices = CylinderMesh.GenerateUnitCircleVertices();
        var sideVertices = GenerateSideVertices(unitCircleVertices);
        var bottomVertices = GenerateBottomVertices(unitCircleVertices);

        s_vertices = [..sideVertices, ..bottomVertices];
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
                indices.Add(k2 + (uint)j);
                indices.Add(k1 + nextJ);

                // For the last stack, the top vertices (k2) are all at the same position (the tip).
                // The second triangle (k2+j, k2+nextJ, k1+nextJ) becomes degenerate (zero area)
                // because k2+j and k2+nextJ are at the same position.
                // We skip it to avoid artifacts (e.g. in tangent calculation).
                if (i != StackCount - 1)
                {
                    indices.Add(k2 + (uint)j);
                    indices.Add(k2 + nextJ);
                    indices.Add(k1 + nextJ);
                }
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

        return indices.ToArray();
    }

    private static VertexData[] GenerateSideVertices(Vector2[] unitCircleVertices)
    {
        var vertices = new List<VertexData>();
        for (var i = 0; i <= StackCount; ++i)
        {
            var h = -0.5f + (float)i / StackCount;
            var t = 1.0f - (float)i / StackCount;
            var radius = 1.0f - (float)i / StackCount;

            for (var j = 0; j <= SectorCount; ++j)
            {
                var position = new Vector3(unitCircleVertices[j].X * radius, unitCircleVertices[j].Y * radius, h);

                // 
                // The normal can be found by considering the 2D normal in the (r,h) plane.
                // Where r is the radius from the center axis and h is the height. 
                // This can be thought of as equivalent to projecting the unit cone onto the xy plane. 
                // The x and y components of the normal are hidden in the r, while the z component is solely dependent on the h. 
                // 
                // In the (r, h) plane it is obvious that the projected cone is a right-angled triangle with shorter sides of length 1.
                // Then the normal is at a 45-degree angle to both axes.
                // Thus, the normal vector in (r, h) plane is (1, 1) which normalizes to (1/sqrt(2), 1/sqrt(2)).
                //
                // h ^
                //   |  
                //   |          ^ (normal at 45 degrees to both axes)
                //   |   │╲    ╱
                //   |   │  ╲╱
                //   |   │    ╲
                //   |   └──────╲
                //   |     
                //   +--------------------> r
                // 
                // The radial component (1/√2) (on the r axis) is distributed into x and y based on the angle θ around the cone,
                // using the unit circle vertices as was in the cylinder case. 
                // 

                float nXy = 1.0f / MathF.Sqrt(2.0f);
                float nZ = 1.0f / MathF.Sqrt(2.0f);

                var normal = new Vector3(unitCircleVertices[j].X * nXy, unitCircleVertices[j].Y * nXy, nZ);

                float u = (float)j / SectorCount;

                // Adjust UV for the tip (last stack) to be in the middle of the sector
                // This makes the single triangle symmetric in UV space
                if (i == StackCount)
                {
                    u = (j + 0.5f) / SectorCount;
                }

                var textureUv = new Vector2(u, t);
                vertices.Add(new VertexData(position, normal, textureUv));
            }
        }

        return vertices.ToArray();
    }

    private static VertexData[] GenerateBottomVertices(Vector2[] unitCircleVertices)
    {
        var vertices = new List<VertexData>();

        const float h = -0.5f;
        const float nz = -1.0f;

        // center point
        vertices.Add(new VertexData(new(0, 0, h), new(0, 0, nz), new(0.5f, 0.5f)));
        for (var j = 0; j < SectorCount; ++j)
        {
            var position = new Vector3(unitCircleVertices[j].X, unitCircleVertices[j].Y, h);
            var normal = new Vector3(0, 0, nz);
            var textureUV = new Vector2(-unitCircleVertices[j].X * 0.5f + 0.5f,
                -unitCircleVertices[j].Y * 0.5f + 0.5f);

            vertices.Add(new VertexData(position, normal, textureUV));
        }

        return vertices.ToArray();
    }
}