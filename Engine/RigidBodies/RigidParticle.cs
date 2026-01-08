using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.ContactGenerators;

namespace Engine.RigidBodies;

/// <summary>
/// Rigid particle that is part of a spring-based cloth.
/// </summary>
public class ClothRigidParticle : RigidParticle
{
    /// <summary>
    /// The X index of this particle in the cloth grid.
    /// </summary>
    public required int ClothParticleX { get; init; }

    /// <summary>
    /// The Y index of this particle in the cloth grid.
    /// </summary>
    public required int ClothParticleY { get; init; }

    /// <summary>
    /// The cloth to which this particle belongs.
    /// </summary>
    public required Cloth AttachedToCloth { get; init; }

    /// <summary>
    /// The connected joint data for this particle. 
    /// </summary>
    public ConnectedJointData ConnectedJointData = new();
}

public class RigidParticle : CollisionParticle, IBoxable
{
    public const float BoundingBoxHalfSize = 0.04f;
    public const float BoxScale = 2 * BoundingBoxHalfSize;

    public virtual BoundingBox GetBoundingBox()
    {
        return new BoundingBox(
            center: this.Body.Position,
            halfSize: new Vector3(BoundingBoxHalfSize, BoundingBoxHalfSize,
                BoundingBoxHalfSize) // small AABB for particle
        );
    }

    public void SetState(
        Vector3 position,
        float extents,
        Vector3 velocity,
        float mass = 0.1f)
    {
        Body.Position = position;
        Body.Velocity = velocity;
        Body.Rotation = new();
        Body.Mass = mass;

        Matrix3 tensor = new();
        Body.SetInertiaTensor(tensor);

        Body.LinearDamping = 0.95f;
        Body.AngularDamping = 0.8f;
        Body.ClearAccumulators();
        Body.Acceleration = new(0, -10f, 0);

        Body.SetAwake();
        Body.CalculateDerivedData();
    }

    public void RefreshPhysicsState()
    {
        Matrix3 tensor = new();
        Body.SetInertiaTensor(tensor);

        Body.LinearDamping = 0.95f;
        Body.AngularDamping = 0.8f;

        Body.SetAwake();
        Body.CalculateDerivedData();
        CalculateInternals();
    }
}