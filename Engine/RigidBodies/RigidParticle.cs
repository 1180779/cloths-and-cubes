using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.ContactGenerators;

namespace Engine.RigidBodies;

/// <summary>
/// Rigid particle that is part of a spring-based cloth.
/// </summary>
public class ClothRigidParticle : RigidParticle, IBodyWithSingleJoint
{
    /// <summary>
    /// The X index of this particle in the cloth grid.
    /// </summary>
    public required int XIndex { get; init; }

    /// <summary>
    /// The Y index of this particle in the cloth grid.
    /// </summary>
    public required int YIndex { get; init; }

    /// <summary>
    /// The cloth of which this particle is part of.
    /// </summary>
    public required Cloth Cloth { get; init; }

    /// <summary>
    /// The connected joint data for this particle. 
    /// </summary>
    public ConnectedJointData ConnectedJoint { get; set; } = new();
}

// The CollisionParticle already provides a default friction value.
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
        Body.Acceleration = Vector3.Gravity;

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