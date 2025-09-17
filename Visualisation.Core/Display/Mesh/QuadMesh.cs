using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display.Mesh;

public sealed class QuadMesh: IMesh
{
    public QuadMesh()
    {
        Init();
    }
    
    private static readonly float[] QuadVertices =
    [
        // positions        // texture Coords
        -1.0f, 1.0f, 0.0f, 0.0f, 1.0f,
        -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
        1.0f, 1.0f, 0.0f, 1.0f, 1.0f,
        1.0f, -1.0f, 0.0f, 1.0f, 0.0f
    ];
    
    private static readonly string MeshName = nameof(QuadMesh);
    private static MeshManager.MeshData? _meshData;
    
    public void Dispose()
    {
        MeshManager.FreeMesh(MeshName, (data) =>
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteVertexArray(data.Vao);
        });
    }

    public void Init()
    {
        _meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            GL.GenVertexArrays(1, out int quadVao);
            GL.GenBuffers(1, out int quadVbo);
            GL.BindVertexArray(quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, QuadVertices.Length * sizeof(float), QuadVertices,
                BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float),
                3 * sizeof(float));

            return new MeshManager.MeshData
            {
                MeshName = MeshName,
                Vbo = quadVbo,
                Vao = quadVao
            };
        });
    }

    public void Render()
    {
        if (_meshData is null)
            throw new MeshDataEmptyException();
        GL.BindVertexArray(_meshData.Vao);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
    }
}