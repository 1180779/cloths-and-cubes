using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class Plane : AbstractVisualObject
{
    public Plane()
    {
        Material = Material.CyanRubber;
        Scale = new(1000.0f, 1000.0f, 1000.0f);
    }

    private static readonly string MeshName = nameof(Plane);
    private static MeshManager.MeshData? _meshData;

    // Two triangles forming a unit square on the XZ plane, centered at origin (Y = 0)
    // Each vertex: position (x, y, z) + normal (nx, ny, nz)
    private static readonly float[] Vertices =
    {
        // Triangle 1
        -0.5f, 0.0f, -0.5f, 0.0f, 1.0f, 0.0f,
        0.5f, 0.0f, -0.5f, 0.0f, 1.0f, 0.0f,
        0.5f, 0.0f, 0.5f, 0.0f, 1.0f, 0.0f,

        // Triangle 2
        0.5f, 0.0f, 0.5f, 0.0f, 1.0f, 0.0f,
        -0.5f, 0.0f, 0.5f, 0.0f, 1.0f, 0.0f,
        -0.5f, 0.0f, -0.5f, 0.0f, 1.0f, 0.0f
    };

    private static readonly int VerticesTriangleCount = Vertices.Length / 6;

    public override void Init()
    {
        _meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // layout (location = 0) vec3 position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // layout (location = 1) vec3 normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

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
        GL.DrawArrays(PrimitiveType.Triangles, 0, VerticesTriangleCount);
    }
}