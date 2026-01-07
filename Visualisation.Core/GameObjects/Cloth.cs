#define CLOTH_POINTS_POSITION_EPSILON_VELOCITY_ADJUSTMENT

using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;
using Engine.RigidBodies;

using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Cloth : GameObject, IBoxable, ITranslationGizmoTarget, IRotationGizmoTarget
{
    public Engine.Cloth EngineCloth { get; set; }
    public ClothMesh VisualCloth { get; set; } // borrowed (does not own the data) from Mesh interface here
    public ForceRegistry ForceRegistry { get; set; }
    private readonly Func<float> _positionEpsilonProvider;

    public Cloth(ForceRegistry registry, Func<float> positionEpsilonProvider)
    {
        _positionEpsilonProvider = positionEpsilonProvider;
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry);
#if CLOTH_POINTS_POSITION_EPSILON_VELOCITY_ADJUSTMENT
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.PointsVelocityAdjusted(_positionEpsilonProvider()));
#else
    Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
#endif
        VisualCloth = new ClothMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    public Cloth(
        ForceRegistry registry,
        Func<float> positionEpsilonProvider,
        int sizeX = 21,
        int sizeY = 21,
        float springLength = 0.25f,
        float springConstant = 1.0f,
        float particleMass = 0.1f)
    {
        _positionEpsilonProvider = positionEpsilonProvider;
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry, sizeX, sizeY, springLength, springConstant, particleMass);
#if CLOTH_POINTS_POSITION_EPSILON_VELOCITY_ADJUSTMENT
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.PointsVelocityAdjusted(_positionEpsilonProvider()));
#else
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
#endif
        VisualCloth = new ClothMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    protected override void PreRender()
    {
#if CLOTH_POINTS_POSITION_EPSILON_VELOCITY_ADJUSTMENT
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.PointsVelocityAdjusted(_positionEpsilonProvider()));
#else
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
#endif
        VisualCloth.UpdatePoints(pts);
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

    /// <summary>
    /// Retrieves the indices of a specific particle within the cloth simulation's particle grid.
    /// </summary>
    /// <param name="particle">The rigid particle to locate within the grid.</param>
    /// <returns>A tuple containing the row and column indices of the particle. Returns (-1, -1) if the particle is not found.</returns>
    public (int, int) GetParticleIndices(RigidParticle particle)
    {
        for (int i = 0; i < EngineCloth.SizeX; i++)
        {
            for (int j = 0; j < EngineCloth.SizeY; j++)
            {
                if (EngineCloth.Particles[i, j] == particle)
                {
                    return (i, j);
                }
            }
        }

        return (-1, -1);
    }

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

        // Update the mesh
        var pts = ConvertToOpenTk(EngineCloth.Points());
        VisualCloth.UpdatePoints(pts);
    }

    public Vector3 AxisPosition => EngineCloth.Center.ToOpenTK();
    public Quaternion AxisOrientation => Quaternion.Identity;

    Vector3 ITranslationGizmoTarget.Position
    {
        get => EngineCloth.Center.ToOpenTK();
        set
        {
            EngineCloth.Center = value.ToEngine();
            EngineCloth.ClearAccumulators();
        }
    }

    private Quaternion _previousOrientation = Quaternion.Identity;

    public Quaternion Orientation
    {
        get => _previousOrientation;
        set
        {
            var deltaRotation = value * Quaternion.Invert(_previousOrientation);
            _previousOrientation = value;

            var axisAngle = deltaRotation.ToAxisAngle();
            var axis = axisAngle.Xyz;
            var angle = axisAngle.W;

            // Convert axis-angle to a rotation vector (scaled axis)
            var rotationVector = axis * angle;

            EngineCloth.RotateAroundCenter(rotationVector.ToEngine());
            EngineCloth.ClearAccumulators();
        }
    }
}