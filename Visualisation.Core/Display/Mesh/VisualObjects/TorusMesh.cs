using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

/// <summary>
/// A torus (ring shape) mesh.
/// </summary>
public sealed class TorusMesh : IMesh
{
    public const int MajorSegments = 64; // Segments around the major radius (the ring)
    public const int MinorSegments = 16; // Segments around the minor radius (the tube)
    private static readonly string MeshName = nameof(TorusMesh);

    private static MeshManager.MeshData? s_meshData;
    private static VertexData[]? s_vertices;
    private static uint[]? s_indices;

    public TorusMesh()
    {
        if (s_vertices is null)
        {
            GenerateTorus();
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

    private static void GenerateTorus()
    {
        // Major radius = 1.0 (distance from center to the tube center)
        // Minor radius = 0.05 (tube thickness)
        float majorRadius = 1.0f;
        float minorRadius = 0.05f;

        var vertices = new List<VertexData>();
        var indices = new List<uint>();

        // Generate vertices
        for (int i = 0; i <= MajorSegments; i++)
        {
            float theta = (float)i / MajorSegments * MathF.PI * 2.0f;
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);

            for (int j = 0; j <= MinorSegments; j++)
            {
                float phi = (float)j / MinorSegments * MathF.PI * 2.0f;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);

                // Position on the torus surface
                float x = (majorRadius + minorRadius * cosPhi) * cosTheta;
                float y = (majorRadius + minorRadius * cosPhi) * sinTheta;
                float z = minorRadius * sinPhi;

                // Normal vector (points outward from the tube center)
                float nx = cosPhi * cosTheta;
                float ny = cosPhi * sinTheta;
                float nz = sinPhi;

                // Texture coordinates
                float u = (float)i / MajorSegments;
                float v = (float)j / MinorSegments;

                vertices.Add(new VertexData
                {
                    Position = new Vector3(x, y, z),
                    Normal = new Vector3(nx, ny, nz),
                    TexCoords = new Vector2(u, v),
                    Tangent = new Vector3(0, 0, 0),
                    Bitangent = new Vector3(0, 0, 0)
                });
            }
        }

        // Generate indices
        for (int i = 0; i < MajorSegments; i++)
        {
            for (int j = 0; j < MinorSegments; j++)
            {
                uint first = (uint)(i * (MinorSegments + 1) + j);
                uint second = first + MinorSegments + 1;

                // First triangle
                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);

                // Second triangle
                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        s_vertices = vertices.ToArray();
        s_indices = indices.ToArray();

        s_vertices.CalculateTangentBitangent(s_indices);
    }
}