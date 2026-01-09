using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class PlaneMesh : IMesh
{
    public PlaneMesh()
    {
        Init();
    }

    private static readonly string MeshName = nameof(PlaneMesh);
    private static MeshManager.MeshData? s_meshData;

    // Two triangles forming a unit square on the XZ plane, centered at origin (Y = 0)
    // Each vertex: position (x, y, z) + normal (nx, ny, nz) + texture coords (u, v)
    private static readonly VertexData[] Vertices =
    [
        // Triangle 1
        new(new(-0.5f, 0.0f, -0.5f), Vector3.UnitY, new(0.0f, 0.0f)),
        new(new(0.5f, 0.0f, 0.5f), Vector3.UnitY, new(1.0f, 1.0f)),
        new(new(0.5f, 0.0f, -0.5f), Vector3.UnitY, new(1.0f, 0.0f)),

        // Triangle 2 
        new(new(0.5f, 0.0f, 0.5f), Vector3.UnitY, new(1.0f, 1.0f)),
        new(new(-0.5f, 0.0f, -0.5f), Vector3.UnitY, new(0.0f, 0.0f)),
        new(new(-0.5f, 0.0f, 0.5f), Vector3.UnitY, new(0.0f, 1.0f))
    ];

    private void Init()
    {
        s_meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            Vertices.CalculateTangentBitangent();

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