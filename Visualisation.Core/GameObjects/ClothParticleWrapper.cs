using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Wraps a single particle from a cloth to allow it to be selected and manipulated
/// individually using translation gizmos. When moved, the particle's mass is set to
/// infinity (inverse mass = 0) to act as an anchor point.
/// </summary>
public sealed class ClothParticleWrapper : ITranslationGizmoTarget, IBoxable, IHasRenderStrategy
{
    private readonly Cloth _parentCloth;
    private readonly int _particleX;
    private readonly int _particleY;
    private IRenderStrategy? _renderStrategy;

    // Shared static mesh for all particles to avoid creating one per wrapper
    private static readonly CubeMesh _sharedCubeMesh = new();

    public ClothParticleWrapper(Cloth parentCloth, int particleX, int particleY)
    {
        _parentCloth = parentCloth;
        _particleX = particleX;
        _particleY = particleY;
    }

    public ClothRigidParticle Particle => _parentCloth.EngineCloth.Particles[_particleX, _particleY];

    public int ParticleX => _particleX;
    public int ParticleY => _particleY;

    public Cloth ParentCloth => _parentCloth;

    public Vector3 AxisPosition => Position;
    public Quaternion AxisOrientation => Quaternion.Identity;

    public Vector3 Position
    {
        get => Particle.Body.Position.ToOpenTK();
        set
        {
            Particle.Body.Position = value.ToEngine();
            Particle.Body.Velocity = Engine.Vector3.Zero;
            Particle.Body.ClearAccumulators();
        }
    }

    public Matrix4 Model
    {
        get
        {
            // Calculate model matrix for the particle visualization
            // Logic taken from RenderObjectOutlineLegacy
            float scale;
            if (Particle is ClothRigidParticleInCorner corner)
            {
                scale = corner.BoundingBoxHalfSize * 2.0f;
            }
            else
            {
                scale = RigidParticle.BoundingBoxHalfSize * 2.0f;
            }

            var position = Particle.GetAxis(3);
            return Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(position.X, position.Y, position.Z);
        }
    }

    public IRenderStrategy RenderStrategy
    {
        get
        {
            // We use a scale of 1.0 here because the scale is baked into the Model matrix
            _renderStrategy ??= new ClothParticleRenderStrategy(_sharedCubeMesh, 1.0f);
            return _renderStrategy;
        }
    }

    public override string ToString()
    {
        return $"Cloth Particle [{_particleX}, {_particleY}]";
    }

    public BoundingBox GetBoundingBox() => Particle.GetBoundingBox();
}