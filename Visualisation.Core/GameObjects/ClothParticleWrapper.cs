using System.Diagnostics;

using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Wraps a single particle from a cloth to allow it to be selected and manipulated
/// individually using translation gizmos. When moved, the particle's mass is set to
/// infinity (inverse mass = 0) to act as an anchor point.
/// </summary>
public sealed class ClothParticleWrapper : IBoxable, IHasRenderStrategy
{
    private readonly Cloth _parentCloth;
    private readonly int _particleX;
    private readonly int _particleY;
    private IRenderStrategy? _renderStrategy;

    private static readonly CubeMesh SharedCubeMesh = new();

    public ClothParticleWrapper(Cloth parentCloth, int particleX, int particleY)
    {
        _parentCloth = parentCloth;
        _particleX = particleX;
        _particleY = particleY;
        Debug.Assert(_particleX < parentCloth.EngineCloth.SizeX);
        Debug.Assert(_particleY < parentCloth.EngineCloth.SizeY);
    }

    public ClothRigidParticle Particle => _parentCloth.EngineCloth.Particles[_particleX, _particleY];

    public int ParticleX => _particleX;
    public int ParticleY => _particleY;

    public Cloth ParentCloth => _parentCloth;

    public Matrix4 Model
    {
        get
        {
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
            _renderStrategy ??= new ClothParticleRenderStrategy(SharedCubeMesh);
            return _renderStrategy;
        }
    }

    public override string ToString()
    {
        return $"Cloth Particle [{_particleX}, {_particleY}]";
    }

    public BoundingBox GetBoundingBox() => Particle.GetBoundingBox();
}