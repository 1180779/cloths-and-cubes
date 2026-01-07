using Engine.RigidBodies;

using Visualisation.Core.Display.Gizmos.Translation;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Wraps a single particle from a cloth to allow it to be selected and manipulated
/// individually using translation gizmos. When moved, the particle's mass is set to
/// infinity (inverse mass = 0) to act as an anchor point.
/// </summary>
public sealed class ClothParticleWrapper : ITranslationGizmoTarget
{
    private readonly Cloth _parentCloth;
    private readonly int _particleX;
    private readonly int _particleY;
    private float _originalInverseMass;
    private bool _isDragging;

    public ClothParticleWrapper(Cloth parentCloth, int particleX, int particleY)
    {
        _parentCloth = parentCloth;
        _particleX = particleX;
        _particleY = particleY;
    }

    public RigidParticle Particle => _parentCloth.EngineCloth.Particles[_particleX, _particleY];

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
        if (_isDragging) return;

        _isDragging = true;
        _originalInverseMass = Particle.Body.InverseMass;
        Particle.Body.InverseMass = 0; // Infinite mass
    }

    /// <summary>
    /// Ends dragging this particle, restoring its original mass.
    /// </summary>
    public void EndDrag()
    {
        if (!_isDragging) return;

        _isDragging = false;
        Particle.Body.InverseMass = _originalInverseMass;
    }

    /// <summary>
    /// Permanently anchors this particle by setting its mass to infinity.
    /// </summary>
    public void MakeAnchor()
    {
        Particle.Body.InverseMass = 0;
        _isDragging = false;
    }

    /// <summary>
    /// Restores the particle to its default mass.
    /// </summary>
    public void RestoreDefaultMass()
    {
        float defaultMass = _parentCloth.EngineCloth.ParticleMass;
        Particle.Body.InverseMass = 1.0f / defaultMass;
        _isDragging = false;
    }

    /// <summary>
    /// Pins this cloth particle to a specific world position (like a box corner).
    /// </summary>
    public void PinToPosition(Engine.Vector3 position)
    {
        Particle.Body.Position = position;
        Particle.Body.Velocity = Engine.Vector3.Zero;
        Particle.Body.InverseMass = 0; // Make it immovable
        Particle.Body.ClearAccumulators();
    }

    /// <summary>
    /// Pins this cloth particle to a box corner, making it follow that corner's position.
    /// </summary>
    public void PinToBoxCorner(ClothRigidParticleInCorner corner)
    {
        Particle.Body.Position = corner.Body.Position;
        Particle.Body.Velocity = Engine.Vector3.Zero;
        Particle.Body.InverseMass = 0; // Make it immovable like the corner
        Particle.Body.ClearAccumulators();

        // Note: This creates a one-time pin. For a continuous attachment,
        // you would need to update the particle position every frame based on the corner.
    }

    public override string ToString()
    {
        return $"Cloth Particle [{_particleX}, {_particleY}]";
    }
}