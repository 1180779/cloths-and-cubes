using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class CubeMesh : IMesh
{
    static CubeMesh()
    {
        Vertices.CalculateTangentBitangent();
    }

    public CubeMesh()
    {
        Init();
    }

    private static readonly string MeshName = nameof(CubeMesh);
    private static MeshManager.MeshData? s_meshData;

    private static readonly VertexData[] Vertices =
    [
        // Back face (Z = -0.5, normal pointing -Z)
        new(new(-0.5f, -0.5f, -0.5f), -Vector3.UnitZ, new(0.0f, 0.0f)),
        new(new(-0.5f, 0.5f, -0.5f), -Vector3.UnitZ, new(0.0f, 1.0f)),
        new(new(0.5f, 0.5f, -0.5f), -Vector3.UnitZ, new(1.0f, 1.0f)),
        new(new(0.5f, 0.5f, -0.5f), -Vector3.UnitZ, new(1.0f, 1.0f)),
        new(new(0.5f, -0.5f, -0.5f), -Vector3.UnitZ, new(1.0f, 0.0f)),
        new(new(-0.5f, -0.5f, -0.5f), -Vector3.UnitZ, new(0.0f, 0.0f)),

        // Front face (Z = +0.5, normal pointing +Z)
        new(new(-0.5f, -0.5f, 0.5f), Vector3.UnitZ, new(0.0f, 0.0f)),
        new(new(0.5f, -0.5f, 0.5f), Vector3.UnitZ, new(1.0f, 0.0f)),
        new(new(0.5f, 0.5f, 0.5f), Vector3.UnitZ, new(1.0f, 1.0f)),
        new(new(0.5f, 0.5f, 0.5f), Vector3.UnitZ, new(1.0f, 1.0f)),
        new(new(-0.5f, 0.5f, 0.5f), Vector3.UnitZ, new(0.0f, 1.0f)),
        new(new(-0.5f, -0.5f, 0.5f), Vector3.UnitZ, new(0.0f, 0.0f)),

        // Left face (X = -0.5, normal pointing -X)
        new(new(-0.5f, -0.5f, -0.5f), -Vector3.UnitX, new(0.0f, 0.0f)),
        new(new(-0.5f, -0.5f, 0.5f), -Vector3.UnitX, new(1.0f, 0.0f)),
        new(new(-0.5f, 0.5f, 0.5f), -Vector3.UnitX, new(1.0f, 1.0f)),
        new(new(-0.5f, 0.5f, 0.5f), -Vector3.UnitX, new(1.0f, 1.0f)),
        new(new(-0.5f, 0.5f, -0.5f), -Vector3.UnitX, new(0.0f, 1.0f)),
        new(new(-0.5f, -0.5f, -0.5f), -Vector3.UnitX, new(0.0f, 0.0f)),

        // Right face (X = +0.5, normal pointing +X)
        new(new(0.5f, -0.5f, -0.5f), Vector3.UnitX, new(0.0f, 0.0f)),
        new(new(0.5f, 0.5f, -0.5f), Vector3.UnitX, new(0.0f, 1.0f)),
        new(new(0.5f, 0.5f, 0.5f), Vector3.UnitX, new(1.0f, 1.0f)),
        new(new(0.5f, 0.5f, 0.5f), Vector3.UnitX, new(1.0f, 1.0f)),
        new(new(0.5f, -0.5f, 0.5f), Vector3.UnitX, new(1.0f, 0.0f)),
        new(new(0.5f, -0.5f, -0.5f), Vector3.UnitX, new(0.0f, 0.0f)),

        // Bottom face (Y = -0.5, normal pointing -Y)
        new(new(-0.5f, -0.5f, -0.5f), -Vector3.UnitY, new(0.0f, 0.0f)),
        new(new(0.5f, -0.5f, -0.5f), -Vector3.UnitY, new(1.0f, 0.0f)),
        new(new(0.5f, -0.5f, 0.5f), -Vector3.UnitY, new(1.0f, 1.0f)),
        new(new(0.5f, -0.5f, 0.5f), -Vector3.UnitY, new(1.0f, 1.0f)),
        new(new(-0.5f, -0.5f, 0.5f), -Vector3.UnitY, new(0.0f, 1.0f)),
        new(new(-0.5f, -0.5f, -0.5f), -Vector3.UnitY, new(0.0f, 0.0f)),

        // Top face (Y = +0.5, normal pointing +Y)
        new(new(-0.5f, 0.5f, -0.5f), Vector3.UnitY, new(0.0f, 1.0f)),
        new(new(-0.5f, 0.5f, 0.5f), Vector3.UnitY, new(0.0f, 0.0f)),
        new(new(0.5f, 0.5f, 0.5f), Vector3.UnitY, new(1.0f, 0.0f)),
        new(new(0.5f, 0.5f, 0.5f), Vector3.UnitY, new(1.0f, 0.0f)),
        new(new(0.5f, 0.5f, -0.5f), Vector3.UnitY, new(1.0f, 1.0f)),
        new(new(-0.5f, 0.5f, -0.5f), Vector3.UnitY, new(0.0f, 1.0f))
    ];

    private void Init()
    {
        s_meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * Marshal.SizeOf(Vertices[0]), Vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            VertexData.VertexAttribPositionNormalTexCoordsTangentBitangent();

            return new MeshManager.MeshData { MeshName = MeshName, Vbo = vbo, Vao = vao };
        });
    }

    public void Dispose()
    {
        MeshManager.FreeMesh(MeshName, data =>
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteVertexArray(data.Vao);

            s_meshData = null;
        });
    }

    public void Render()
    {
        if (s_meshData is null)
            throw new MeshDataEmptyException();
        GL.BindVertexArray(s_meshData.Vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
    }
}