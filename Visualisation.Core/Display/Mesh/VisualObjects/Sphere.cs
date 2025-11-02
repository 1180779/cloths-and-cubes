using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class SphereMesh : IMesh
{
    public SphereMesh()
    {
        Init();
    }

    private static readonly string MeshName = nameof(SphereMesh);
    private static MeshManager.MeshData? _meshData;

    private static VertexData[] Vertices = null!;
    private static uint[] Indices = null!;
    private static readonly int Precision = 40; // number of segments per half circle

    private void GenerateSurface()
    {
        List<VertexData> vertices = new List<VertexData>();
        List<uint> indices = new List<uint>();

        for (var i = 0; i <= Precision; i++)
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

                var u = (float)j / Precision;
                var v = (float)i / Precision;

                vertices.Add(new VertexData(
                    (float)x, (float)y, (float)z, // Position
                    (float)x, (float)y, (float)z, // Normal
                    u, v // TexCoords
                ));
            }
        }

        for (uint i = 0; i < Precision; i++)
        {
            for (uint j = 0; j < Precision; j++)
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

    private void CalculateTangentsAndBitangents()
    {
        for (int i = 0; i < Indices.Length; i += 3)
        {
            // Get the vertices of the triangle
            ref var v0 = ref Vertices[Indices[i]];
            ref var v1 = ref Vertices[Indices[i + 1]];
            ref var v2 = ref Vertices[Indices[i + 2]];

            // Calculate tangent and bitangent
            // This method adds the results to the existing values
            VertexData.CalculateTangentBitangent(ref v0, ref v1, ref v2);
        }
    }

    public void Dispose()
    {
        MeshManager.FreeMesh(MeshName, (data) =>
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteVertexArray(data.Vao);
        });
    }

    private void Init()
    {
        GenerateSurface();
        CalculateTangentsAndBitangents();

        _meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * 14 * sizeof(float), Vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices,
                BufferUsageHint.StaticDraw);

            VertexData.VertexAttribPositionNormalTexCoordsTangentBitangent();

            return new MeshManager.MeshData
            {
                MeshName = MeshName,
                Vbo = vbo,
                Vao = vao,
                Ebo = ebo
            };
        });
    }

    public void Render()
    {
        if (_meshData is null)
            throw new MeshDataEmptyException();
        GL.BindVertexArray(_meshData.Vao);
        GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
    }
}