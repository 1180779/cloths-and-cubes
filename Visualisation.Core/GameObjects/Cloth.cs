using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;

using Visualisation.Core.Display;
using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Cloth : GameObject, IBoxable
{
    public Engine.Cloth EngineCloth { get; set; }
    public ClothMesh VisualCloth { get; set; } // borrowed (does not own the data) from Mesh interface here
    public ForceRegistry ForceRegistry { get; set; }
    private IRenderStrategy? _renderStrategy;

    public Cloth(ForceRegistry registry, float positionEpsilon)
    {
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry);
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.PointsVelocityAdjusted(positionEpsilon));
        VisualCloth = new ClothMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    public Cloth(
        ForceRegistry registry,
        float positionEpsilon,
        int sizeX = 21,
        int sizeY = 21,
        float springLength = 0.25f,
        float springConstant = 1.0f,
        float particleMass = 0.1f)
    {
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry, sizeX, sizeY, springLength, springConstant, particleMass);
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.PointsVelocityAdjusted(positionEpsilon));
        VisualCloth = new ClothMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    public override IRenderStrategy RenderStrategy
    {
        get
        {
            _renderStrategy ??= new ClothRenderStrategy(VisualCloth, Material, EngineCloth);
            return _renderStrategy;
        }
    }

    protected override void OnMaterialChanged()
    {
        _renderStrategy = null;
    }

    public BoundingBox GetBoundingBox()
    {
        var min = new Engine.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Engine.Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var particle in EngineCloth.Particles)
        {
            var pos = particle.Body.Position;
            min.X = Math.Min(min.X, pos.X);
            min.Y = Math.Min(min.Y, pos.Y);
            min.Z = Math.Min(min.Z, pos.Z);
            max.X = Math.Max(max.X, pos.X);
            max.Y = Math.Max(max.Y, pos.Y);
            max.Z = Math.Max(max.Z, pos.Z);
        }

        var center = (min + max) / 2;
        var halfSize = (max - min) / 2;
        return new BoundingBox(center, halfSize);
    }

    public override Vector3 Position =>
        new(EngineCloth.Particles[0, 0].Body.Position.X, EngineCloth.Particles[0, 0].Body.Position.Y,
            EngineCloth.Particles[0, 0].Body.Position.Z);

    protected override IMesh Mesh { get; set; }
    public override object PhysicsObject => EngineCloth;

    public override Matrix4 Model => Matrix4.Identity;

    public static Vector3[,] ConvertToOpenTk(Engine.Vector3[,] enginePoints)
    {
        int sx = enginePoints.GetLength(0);
        int sy = enginePoints.GetLength(1);
        var result = new Vector3[sx, sy];

        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                var e = enginePoints[x, y];
                result[x, y] = new Vector3(e.X, e.Y, e.Z);
            }
        }

        return result;
    }

    public void RegenerateClothPreservingTheCenter(
        int newSizeX,
        int newSizeY,
        float newSpringLength,
        float newSpringConstant,
        float newParticleMass)
    {
        EngineCloth.RegenerateGridPreservingTheCenter(newSizeX, newSizeY, newSpringLength, newSpringConstant,
            newParticleMass);

        // The render strategy will take care of updating the mesh points
    }
}