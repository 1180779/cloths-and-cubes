using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class SphereMesh : IMesh
{
    public SphereMesh()
    {
        Init();
    }

    private static readonly string MeshName = nameof(SphereMesh);
    private static MeshManager.MeshData? s_meshData;

    private static VertexData[]? s_vertices;
    private static uint[]? s_indices;
    private static readonly int Precision = 40; // number of segments per half-circle

    private void GenerateSurface()
    {
        if (s_vertices is not null)
            return;

        List<VertexData> vertices = new();
        List<uint> indices = new();

        for (var i = 0; i <= Precision; i++)
        {
            double lat = Math.PI * (-0.5 + (double)i / Precision); // latitude
            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            for (int j = 0; j <= Precision; j++)
            {
                double lon = 2 * Math.PI * (j == Precision ? 0 : j) / Precision; // longitude
                double sinLon = Math.Sin(lon);
                double cosLon = Math.Cos(lon);

                Vector3 positionAndNormal = new Vector3(
                    (float)(cosLon * cosLat),
                    (float)(sinLat),
                    (float)(sinLon * cosLat)
                );
                Vector2 texCoords = new Vector2(
                    (float)j / Precision,
                    (float)i / Precision
                );

                vertices.Add(new VertexData(positionAndNormal, positionAndNormal, texCoords));
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

        s_vertices = [.. vertices.ToArray()];
        s_indices = [.. indices.ToArray()];
    }

    private void CalculateTangentsAndBitangents()
    {
        if (s_vertices is null || s_indices is null)
            return;

        for (int i = 0; i < s_indices.Length; i += 3)
        {
            // Get the vertices of the triangle
            ref var v0 = ref s_vertices[s_indices[i]];
            ref var v1 = ref s_vertices[s_indices[i + 1]];
            ref var v2 = ref s_vertices[s_indices[i + 2]];

            // Calculate tangent and bitangent
            // This method adds the results to the existing values
            VertexData.CalculateTangentBitangent(ref v0, ref v1, ref v2);
        }
    }

    public void Dispose()
    {
        MeshManager.FreeMesh(MeshName, data =>
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(data.Vbo);
            GL.DeleteVertexArray(data.Vao);

            s_meshData = null;
            s_vertices = null;
            s_indices = null;
        });
    }

    private void Init()
    {
        GenerateSurface();
        CalculateTangentsAndBitangents();

        s_meshData = MeshManager.GetOrLoadMesh(MeshName, () =>
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, s_vertices!.Length * 14 * sizeof(float), s_vertices,
                BufferUsageHint.StaticDraw);

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, s_indices!.Length * sizeof(uint), s_indices,
                BufferUsageHint.StaticDraw);

            VertexData.VertexAttribPositionNormalTexCoordsTangentBitangent();

            return new MeshManager.MeshData { MeshName = MeshName, Vbo = vbo, Vao = vao, Ebo = ebo };
        });
    }

    public void Render()
    {
        if (s_meshData is null || s_indices is null)
            throw new MeshDataEmptyException();
        GL.BindVertexArray(s_meshData.Vao);
        GL.DrawElements(PrimitiveType.Triangles, s_indices.Length, DrawElementsType.UnsignedInt, 0);
    }
}