using Engine;
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
    private float _originalInverseMass;
    private bool _isDragging;
    private bool _isPinned;
    private Engine.Vector3 _pinnedPosition;
    private Func<Engine.Vector3> _gravityProvider;

    public ClothParticleWrapper(Cloth parentCloth, int particleX, int particleY, Func<Engine.Vector3> gravityProvider)
    {
        _parentCloth = parentCloth;
        _particleX = particleX;
        _particleY = particleY;
        _gravityProvider = gravityProvider;
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
            // If pinned, we'll handle position updates through the Pin method
            // if (_isPinned)
            // {
            //     // During drag, the position is managed by pinning logic
            //     return;
            // }

            // When dragging, set infinite mass (inverseMass = 0) to make it an anchor
            if (!_isDragging)
            {
                BeginDrag();
            }

            Particle.Body.Position = value.ToEngine();
            Particle.Body.Velocity = Engine.Vector3.Zero;
            Particle.Body.ClearAccumulators();
        }
    }

    /// <summary>
    /// Begins dragging this particle, making it an anchor by setting its mass to infinity.
    /// </summary>
    public void BeginDrag()
    {
        if (_isDragging)
        {
            return;
        }

        _isDragging = true;

        // Only save original values if not already pinned
        if (!_isPinned)
        {
            _originalInverseMass = Particle.Body.InverseMass;
        }

        Particle.Body.Acceleration = Engine.Vector3.Zero;
        Particle.Body.InverseMass = 0; // Infinite mass
    }

    /// <summary>
    /// Ends dragging this particle, restoring its original mass only if not pinned.
    /// </summary>
    public void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;

        // Only restore if not pinned
        if (!_isPinned)
        {
            Particle.Body.InverseMass = _originalInverseMass;
            Particle.Body.Velocity = _gravityProvider();
        }
    }

    /// <summary>
    /// Permanently anchors this particle by setting its mass to infinity.
    /// </summary>
    public void MakeAnchor()
    {
        Particle.Body.InverseMass = 0; // Infinite translational mass
        Particle.Body.InverseInertiaTensor = new Matrix3(); // Infinite rotational inertia
        Particle.Body.Velocity = Engine.Vector3.Zero;
        Particle.Body.Acceleration = Engine.Vector3.Zero;
        Particle.Body.Rotation = Engine.Vector3.Zero;
        Particle.Body.ClearAccumulators();
        Particle.Body.CalculateDerivedData();
        _isDragging = false;
    }

    /// <summary>
    /// Restores the particle to its default mass.
    /// </summary>
    public void RestoreDefaultMass()
    {
        float defaultMass = _parentCloth.EngineCloth.ParticleMass;
        Particle.Body.InverseMass = 1.0f / defaultMass;
        Particle.Body.Acceleration = Engine.Vector3.Gravity;

        // Restore the inertia tensor to a zero matrix (standard for particles)
        Matrix3 tensor = new();
        Particle.Body.SetInertiaTensor(tensor);

        Particle.Body.CalculateDerivedData();
        _isDragging = false;
    }

    /// <summary>
    /// Pins this cloth particle to a specific world position (like a box corner).
    /// This saves the original mass/inertia if not already saved.
    /// </summary>
    public void Pin(Engine.Vector3 position)
    {
        if (_isPinned)
        {
            // Already pinned, just update position
            _pinnedPosition = position;
            Particle.Body.Position = position;
            Particle.Body.Velocity = Engine.Vector3.Zero;
            Particle.Body.ClearAccumulators();
            return;
        }

        // First time pinning - save original values if not dragging
        if (!_isDragging)
        {
            _originalInverseMass = Particle.Body.InverseMass;
        }

        _isPinned = true;
        _pinnedPosition = position;

        Particle.Body.Position = position;
        Particle.Body.Velocity = Engine.Vector3.Zero;
        Particle.Body.Acceleration = Engine.Vector3.Zero;

        // Make it truly immovable
        Particle.Body.InverseMass = 0; // Infinite translational mass

        Particle.Body.ClearAccumulators();
        Particle.Body.CalculateDerivedData();
    }

    /// <summary>
    /// Unpins this cloth particle, restoring its original mass and inertia tensor.
    /// </summary>
    public void Unpin()
    {
        if (!_isPinned) return;


        _isPinned = false;

        // Restore original values
        Particle.Body.InverseMass = _originalInverseMass;
        Particle.Body.Velocity = _gravityProvider();
        Particle.Body.CalculateDerivedData();
    }

    /// <summary>
    /// Gets whether this particle is currently pinned to a box corner.
    /// </summary>
    public bool IsPinned => _isPinned;

    /// <summary>
    /// Gets the position this particle is pinned to (if pinned).
    /// </summary>
    public Engine.Vector3 PinnedPosition => _pinnedPosition;

    public override string ToString()
    {
        return $"Cloth Particle [{_particleX}, {_particleY}]";
    }

    public BoundingBox GetBoundingBox() => Particle.GetBoundingBox();
}