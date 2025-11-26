using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Mesh;

[StructLayout(LayoutKind.Sequential)]
public struct VertexData
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoords;

    public Vector3 Tangent;
    public Vector3 Bitangent;

    public VertexData(float px, float py, float pz, float nx, float ny, float nz, float u, float v)
    {
        Position = new Vector3(px, py, pz);
        Normal = new Vector3(nx, ny, nz);
        TexCoords = new Vector2(u, v);
    }

    public VertexData(float px, float py, float pz, float u, float v)
    {
        Position = new Vector3(px, py, pz);
        Normal = Vector3.Zero;
        TexCoords = new Vector2(u, v);
    }

    public static void VertexAttribPositionTexCoords()
    {
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    public static void VertexAttribPositionNormalTexCoords()
    {
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
    }

    public static void VertexAttribPositionNormalTexCoordsTangentBitangent()
    {
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 8 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));
        GL.EnableVertexAttribArray(4);
    }

    public static void CalculateTangentBitangent(
        ref VertexData a,
        ref VertexData b,
        ref VertexData c)
    {
        var edge1 = b.Position - a.Position;
        var edge2 = c.Position - a.Position;
        var deltaUv1 = b.TexCoords - a.TexCoords;
        var deltaUv2 = c.TexCoords - a.TexCoords;

        float f = 1.0f / (deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y);

        var tangent = new Vector3
        {
            X = f * (deltaUv2.Y * edge1.X - deltaUv1.Y * edge2.X),
            Y = f * (deltaUv2.Y * edge1.Y - deltaUv1.Y * edge2.Y),
            Z = f * (deltaUv2.Y * edge1.Z - deltaUv1.Y * edge2.Z)
        };
        tangent = Vector3.Normalize(tangent);

        var bitangent = new Vector3
        {
            X = f * (-deltaUv2.X * edge1.X + deltaUv1.X * edge2.X),
            Y = f * (-deltaUv2.X * edge1.Y + deltaUv1.X * edge2.Y),
            Z = f * (-deltaUv2.X * edge1.Z + deltaUv1.X * edge2.Z)
        };
        bitangent = Vector3.Normalize(bitangent);

        a.Tangent += tangent;
        b.Tangent += tangent;
        c.Tangent += tangent;
        a.Bitangent += bitangent;
        b.Bitangent += bitangent;
        c.Bitangent += bitangent;
    }
}