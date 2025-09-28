using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class Plane : AbstractVisualObject
{
    public Plane()
    {
        Material = Material.Floors.RichBrownTile;
        Scale = new(1000.0f, 1000.0f, 1000.0f);
    }

    private static readonly string MeshName = nameof(Plane);
    private static MeshManager.MeshData? _meshData;

    // Two triangles forming a unit square on the XZ plane, centered at origin (Y = 0)
    // Each vertex: position (x, y, z) + normal (nx, ny, nz) + texture coords (u, v)
    private static readonly VertexData[] Vertices =
    [
        // Triangle 1
        new(-0.5f, 0.0f, -0.5f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f),
        new(0.5f, 0.0f, -0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f),
        new(0.5f, 0.0f, 0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f),

        // Triangle 2 
        new(0.5f, 0.0f, 0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f),
        new(-0.5f, 0.0f, 0.5f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f),
        new(-0.5f, 0.0f, -0.5f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f)
    ];

    public override void Init()
    {
        _meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            Vertices.CalculateTangentBitangent();

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * Marshal.SizeOf(Vertices[0]), Vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            VertexData.VertexAttribPositionNormalTexCoordsTangentBitangent();

            return new MeshManager.MeshData
            {
                MeshName = MeshName,
                Vbo = vbo,
                Vao = vao
            };
        });
    }

    public override void Dispose()
    {
        MeshManager.FreeMesh(MeshName, data =>
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteVertexArray(data.Vao);
        });
    }

    public override void Render()
    {
        if (_meshData is null)
            throw new MeshDataEmptyException();

        GL.BindVertexArray(_meshData.Vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
    }
}