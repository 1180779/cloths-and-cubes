using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Mesh;

namespace Visualisation.Core.GameObjects;

public sealed class Line : IMesh
{
    private readonly Vector3 _start;
    private readonly Vector3 _end;
    private readonly string _meshName;

    private MeshManager.MeshData? _meshData;

    public Line(Vector3 start, Vector3 end, string? meshName = null)
    {
        _start = start;
        _end = end;
        _meshName = meshName ?? Guid.NewGuid().ToString();
        Init();
    }

    private void Init()
    {
        float[] vertices =
        {
            _start.X, _start.Y, _start.Z,
            _end.X, _end.Y, _end.Z
        };

        _meshData = MeshManager.GetOrLoadMesh(_meshName, () =>
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            return new MeshManager.MeshData { MeshName = _meshName, Vbo = vbo, Vao = vao };
        });
    }

    public void Dispose()
    {
        MeshManager.FreeMesh(_meshName, (data) =>
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteVertexArray(data.Vao);
        });
    }

    public void Render()
    {
        if (_meshData is null)
            throw new MeshDataEmptyException();
        
        GL.BindVertexArray(_meshData.Vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, 2);
    }
}