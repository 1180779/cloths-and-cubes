using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
namespace Visualisation.Core.Display.Mesh.VisualObjects
{
    internal class SpringMesh : AbstractVisualObject
    {
        private readonly Vector3[,] _points;
        private static MeshManager.MeshData? _meshData;
        private readonly string _meshName;

        private float[]? _vertices;
        private int _triangleCount;

        public SpringMesh(Vector3[,] points, string? meshName = null)
        {
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _meshName = meshName ?? Guid.NewGuid().ToString();
        }

        private void BuildMesh()
        {
            int width = _points.GetLength(0);
            int height = _points.GetLength(1);

            List<float> vertexData = new();

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    Vector3 p00 = _points[x, y];
                    Vector3 p10 = _points[x + 1, y];
                    Vector3 p01 = _points[x, y + 1];
                    Vector3 p11 = _points[x + 1, y + 1];

                    AddTriangle(vertexData, p00, p10, p01);

                    AddTriangle(vertexData, p10, p11, p01);
                }
            }

            _vertices = vertexData.ToArray();
            _triangleCount = _vertices.Length / 6;
        }

        private void AddTriangle(List<float> data, Vector3 a, Vector3 b, Vector3 c)
        {
            // Flat normal from triangle
            Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));

            void AddVertex(Vector3 v)
            {
                data.Add(v.X);
                data.Add(v.Y);
                data.Add(v.Z);
                data.Add(normal.X);
                data.Add(normal.Y);
                data.Add(normal.Z);
            }

            AddVertex(a);
            AddVertex(b);
            AddVertex(c);
        }

        public override void Init()
        {
            BuildMesh();

            if (_vertices == null)
                throw new InvalidOperationException("Mesh not built.");

            _meshData = MeshManager.GetOrLoadMesh(_meshName, () =>
            {
                int vbo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices,
                    BufferUsageHint.StaticDraw);

                int vao = GL.GenVertexArray();
                GL.BindVertexArray(vao);

                // Position
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);

                // Normal
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                return new MeshManager.MeshData
                {
                    MeshName = _meshName,
                    Vbo = vbo,
                    Vao = vao
                };
            });
        }

        public override void Dispose()
        {
            MeshManager.FreeMesh(_meshName, (data) =>
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
            GL.DrawArrays(PrimitiveType.Triangles, 0, _triangleCount);
        }
    }
}
