using System.Runtime.InteropServices;

using Engine;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects
{
    /// <summary>
    /// Represents a 3D mesh that simulates a cloth-like structure, allowing for visualization
    /// and manipulation of its points, vertices, and associated data.
    ///
    /// This mesh is unique in comparison to other meshes in that each of the instances holds the vertex data
    /// as there are directly tied to the points in the world.
    /// </summary>
    public class ClothMesh : IMesh
    {
        private MeshManager.MeshData? _meshData;
        private readonly string _meshName;

        private VertexData[]? _vertices;
        private int _vertexCount;

        public ClothMesh(Vector3[,] points)
        {
            _meshName = Guid.NewGuid().ToString();
            Init(points);
        }

        public void UpdatePoints(Vector3[,] newPoints)
        {
            int width = newPoints.GetLength(0);
            int height = newPoints.GetLength(1);

            var vertices = new VertexData[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    vertices[x, y].Position = newPoints[x, y];
                    vertices[x, y].TexCoords = new Vector2((float)x / (width - 1), (float)y / (height - 1));
                }
            }

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var v00 = vertices[x, y];
                    var v10 = vertices[x + 1, y];
                    var v01 = vertices[x, y + 1];
                    var v11 = vertices[x + 1, y + 1];

                    var n1 = Vector3.Cross(v10.Position - v00.Position, v01.Position - v00.Position);
                    var n2 = Vector3.Cross(v11.Position - v10.Position, v01.Position - v10.Position);

                    vertices[x, y].Normal += n1;
                    vertices[x + 1, y].Normal += n1 + n2;
                    vertices[x, y + 1].Normal += n1 + n2;
                    vertices[x + 1, y + 1].Normal += n2;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    vertices[x, y].Normal.Normalize();
                }
            }

            List<VertexData> vertexData = new();
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var v00 = vertices[x, y];
                    var v10 = vertices[x + 1, y];
                    var v01 = vertices[x, y + 1];
                    var v11 = vertices[x + 1, y + 1];

                    VertexData.CalculateTangentBitangent(ref v00, ref v10, ref v01);
                    VertexData.CalculateTangentBitangent(ref v10, ref v11, ref v01);

                    vertexData.Add(v00);
                    vertexData.Add(v10);
                    vertexData.Add(v01);
                    vertexData.Add(v10);
                    vertexData.Add(v11);
                    vertexData.Add(v01);
                }
            }

            var newVertices = vertexData.ToArray();
            int newVertexCount = newVertices.Length;

            // Check if vertex count changed
            bool sizeChanged = newVertexCount != _vertexCount;

            _vertices = newVertices;
            _vertexCount = newVertexCount;

            if (_meshData != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _meshData.Vbo);

                // If size changed, reallocate the buffer; otherwise just update data
                if (sizeChanged)
                {
                    GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<VertexData>(), _vertices,
                        BufferUsageHint.StreamDraw);
                }
                else
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                        _vertices.Length * Marshal.SizeOf<VertexData>(), _vertices);
                }
            }
        }

        public Triangle[] GetTriangles()
        {
            if (_vertices == null)
            {
                return [];
            }

            var triangles = new List<Triangle>();
            for (int i = 0; i < _vertices.Length; i += 3)
            {
                var v1 = new Engine.Vector3(_vertices[i].Position.X, _vertices[i].Position.Y, _vertices[i].Position.Z);
                var v2 = new Engine.Vector3(_vertices[i + 1].Position.X, _vertices[i + 1].Position.Y,
                    _vertices[i + 1].Position.Z);
                var v3 = new Engine.Vector3(_vertices[i + 2].Position.X, _vertices[i + 2].Position.Y,
                    _vertices[i + 2].Position.Z);
                triangles.Add(new Triangle(v1, v2, v3));
            }

            return triangles.ToArray();
        }

        private void BuildMesh(Vector3[,] points)
        {
            int width = points.GetLength(0);
            int height = points.GetLength(1);

            var vertices = new VertexData[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    vertices[x, y].Position = points[x, y];
                    vertices[x, y].TexCoords = new Vector2((float)x / (width - 1), (float)y / (height - 1));
                }
            }

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var v00 = vertices[x, y];
                    var v10 = vertices[x + 1, y];
                    var v01 = vertices[x, y + 1];
                    var v11 = vertices[x + 1, y + 1];

                    var n1 = Vector3.Cross(v10.Position - v00.Position, v01.Position - v00.Position);
                    var n2 = Vector3.Cross(v11.Position - v10.Position, v01.Position - v10.Position);

                    vertices[x, y].Normal += n1;
                    vertices[x + 1, y].Normal += n1 + n2;
                    vertices[x, y + 1].Normal += n1 + n2;
                    vertices[x + 1, y + 1].Normal += n2;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    vertices[x, y].Normal.Normalize();
                }
            }

            List<VertexData> vertexData = new();
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    var v00 = vertices[x, y];
                    var v10 = vertices[x + 1, y];
                    var v01 = vertices[x, y + 1];
                    var v11 = vertices[x + 1, y + 1];

                    VertexData.CalculateTangentBitangent(ref v00, ref v10, ref v01);
                    VertexData.CalculateTangentBitangent(ref v10, ref v11, ref v01);

                    vertexData.Add(v00);
                    vertexData.Add(v10);
                    vertexData.Add(v01);
                    vertexData.Add(v10);
                    vertexData.Add(v11);
                    vertexData.Add(v01);
                }
            }

            _vertices = vertexData.ToArray();
            _vertexCount = _vertices.Length;
        }

        private void Init(Vector3[,] points)
        {
            BuildMesh(points);

            if (_vertices == null)
                throw new InvalidOperationException("Mesh not built.");

            _meshData = MeshManager.GetOrLoadMesh(_meshName, () =>
            {
                int vbo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Marshal.SizeOf<VertexData>(), _vertices,
                    BufferUsageHint.StaticDraw);

                int vao = GL.GenVertexArray();
                GL.BindVertexArray(vao);

                VertexData.VertexAttribPositionNormalTexCoordsTangentBitangent();

                return new MeshManager.MeshData { MeshName = _meshName, Vbo = vbo, Vao = vao };
            });
        }

        public void Dispose()
        {
            MeshManager.FreeMesh(_meshName, data =>
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
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
        }
    }
}