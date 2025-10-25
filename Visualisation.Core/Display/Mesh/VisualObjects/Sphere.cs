using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects
{
    public class Sphere : AbstractVisualObject
    {
        private static readonly string MeshName = nameof(Sphere);
        private static MeshManager.MeshData? _meshData;

        private static Real[] Vertices = null!;
        private static uint[] Indices = null!;
        private static readonly int Precision = 40; // number of segments per half circle

        private void GenerateSurface()
        {
            List<Real> vertices = new List<Real>();
            List<uint> indices = new List<uint>();

            for (int i = 0; i <= Precision; i++)
            {
                double lat = Math.PI * (-0.5 + (double)i / Precision); // latitude
                double sinLat = Math.Sin(lat);
                double cosLat = Math.Cos(lat);
                for (int j = 0; j <= Precision; j++)
                {
                    double lon = 2 * Math.PI * (double)(j == Precision ? 0 : j) / Precision; // longitude
                    double sinLon = Math.Sin(lon);
                    double cosLon = Math.Cos(lon);
                    double x = cosLon * cosLat;
                    double y = sinLat;
                    double z = sinLon * cosLat;

                    // Position
                    vertices.Add((Real)x);
                    vertices.Add((Real)y);
                    vertices.Add((Real)z);

                    // Normal
                    vertices.Add((Real)x);
                    vertices.Add((Real)y);
                    vertices.Add((Real)z);
                }
            }

            for (int i = 0; i < Precision; i++)
            {
                for (int j = 0; j < Precision; j++)
                {
                    uint first = (uint)((i * (Precision + 1)) + j);
                    uint second = (uint)(first + Precision + 1);

                    indices.Add(first);
                    indices.Add(second);
                    indices.Add(first + 1);

                    indices.Add(second);
                    indices.Add(second + 1);
                    indices.Add(first + 1);
                }
            }

            Vertices = [.. vertices.ToArray()];
            Indices = [.. indices.ToArray()];
        }

        public override void Dispose()
        {
            MeshManager.FreeMesh(MeshName, (data) =>
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.DeleteBuffer(data.Vbo);
                GL.DeleteVertexArray(data.Vao);
            });
        }

        public override void Init()
        {
            GenerateSurface();

            _meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
            {
                int vbo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices,
                    BufferUsageHint.StaticDraw);

                int vao = GL.GenVertexArray();
                GL.BindVertexArray(vao);

                int ebo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices,
                    BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);

                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float),
                    3 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                return new MeshManager.MeshData
                {
                    MeshName = MeshName,
                    Vbo = vbo,
                    Vao = vao,
                    Ebo = ebo
                };
            });
        }

        public override void Render()
        {
            if (_meshData is null)
                throw new MeshDataEmptyException();
            GL.BindVertexArray(_meshData.Vao);
            GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
        }
    }
}