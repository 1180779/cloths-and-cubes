using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display.Gizmos.Translation;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Wraps a single particle from a cloth to allow it to be selected and manipulated
/// individually using translation gizmos. When moved, the particle's mass is set to
/// infinity (inverse mass = 0) to act as an anchor point.
/// </summary>
public sealed class ClothParticleWrapper : ITranslationGizmoTarget, IBoxable
{
    private readonly Cloth _parentCloth;
    private readonly int _particleX;
    private readonly int _particleY;

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

    public override string ToString()
    {
        return $"Cloth Particle [{_particleX}, {_particleY}]";
    }

    public BoundingBox GetBoundingBox() => Particle.GetBoundingBox();
}