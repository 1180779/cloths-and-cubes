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
}