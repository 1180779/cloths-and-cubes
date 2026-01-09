using Engine;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;

using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;
using System.Collections.Generic;

namespace Visualisation.Core.GameObjects;

public sealed class Cloth : GameObject, IBoxable
{
    public Engine.Cloth EngineCloth { get; set; }
    public SpringMesh VisualCloth { get; set; } // borrowed (does not own the data) from Mesh interface here
    public ForceRegistry ForceRegistry { get; set; }

    // Now stores checked/saved triangles per square (top-left index)
    public Dictionary<(int x, int y), Triangle[]> SavedTriangles { get; } = new();

    public Cloth(ForceRegistry registry)
    {
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry);
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
        VisualCloth = new SpringMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    public Cloth(
        ForceRegistry registry,
        int sizeX = 21,
        int sizeY = 21,
        float springLength = 0.25f,
        float springConstant = 1.0f,
        float particleMass = 0.1f)
    {
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry, sizeX, sizeY, springLength, springConstant, particleMass);
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
        VisualCloth = new SpringMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    protected override void PreRender()
    {
        var pts = ConvertToOpenTk(EngineCloth.Points());
        VisualCloth.UpdatePoints(pts);
    }

    public BoundingBox GetBoundingBox()
    {
        var min = new Engine.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Engine.Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var particle in EngineCloth.Particles)
        {
            if (particle == null || particle.Body == null) continue;
            var pos = particle.Body.Position;
            min.X = Math.Min(min.X, pos.X);
            min.Y = Math.Min(min.Y, pos.Y);
            min.Z = Math.Min(min.Z, pos.Z);
            max.X = Math.Max(max.X, pos.X);
            max.Y = Math.Max(max.Y, pos.Y);
            max.Z = Math.Max(max.Z, pos.Z);
        }

        var center = new Engine.Vector3((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
        var halfSize = new Engine.Vector3((max.X - min.X) / 2, (max.Y - min.Y) / 2, (max.Z - min.Z) / 2);
        return new BoundingBox(center, halfSize);
    }

    public override Vector3 Position =>
        new(EngineCloth.Particles[0, 0].Body.Position.X, EngineCloth.Particles[0, 0].Body.Position.Y,
            EngineCloth.Particles[0, 0].Body.Position.Z);

    protected override IMesh Mesh { get; set; }
    public override object PhysicsObject => EngineCloth;
    public override Matrix4 Model => Matrix4.Identity;

    private static Vector3[,] ConvertToOpenTk(Engine.Vector3[,] enginePoints)
    {
        int sx = enginePoints.GetLength(0);
        int sy = enginePoints.GetLength(1);
        var result = new Vector3[sx, sy];

        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                var e = enginePoints[x, y];
                result[x, y] = new Vector3((float)e.X, (float)e.Y, (float)e.Z);
            }
        }

        return result;
    }
}