using System.Diagnostics;

namespace Visualisation.Core.Display.Mesh;

public static class VertexDataArrayExtensions
{
    public static void CalculateTangentBitangent(this VertexData[] vertices)
    {
        if (vertices.Length % 3 != 0)
        {
            throw new ArgumentException("Vertex array length must be a multiple of 3 to form triangles.");
        }

        for (int i = 0; i < vertices.Length; i += 3)
        {
            VertexData.CalculateTangentBitangent(ref vertices[i], ref vertices[i + 1], ref vertices[i + 2]);
        }
    }
    
    public static void CalculateTangentBitangent(this VertexData[] vertices, uint[] indices)
    {
        if (indices.Length % 3 != 0)
        {
            throw new ArgumentException("Indices array length must be a multiple of 3 to form triangles.");
        }

        Debug.Assert(vertices is not null);
        Debug.Assert(indices is not null);

        for (var i = 0; i < indices.Length; i += 3)
        {
            var i1 = indices[i];
            var i2 = indices[i + 1];
            var i3 = indices[i + 2];
            
            VertexData.CalculateTangentBitangent(ref vertices[i1], ref vertices[i2], ref vertices[i3]);
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].Tangent != Vector3.Zero)
                vertices[i].Tangent = Vector3.Normalize(vertices[i].Tangent);
            if (vertices[i].Bitangent != Vector3.Zero)
                vertices[i].Bitangent = Vector3.Normalize(vertices[i].Bitangent);
        }
    }
}