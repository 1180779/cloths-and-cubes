using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies;

public class Box : CollisionBox, IBoxable
{
    public bool IsOverlapping { get; set; } // previously used for some rendering (???)
    static readonly Vector3 MinPos = new(-15, 5, -15);
    static readonly Vector3 MaxPos = new(15, 10, 15);
    static readonly Vector3 MinSize = new(0.5f, 0.5f, 0.5f);
    static readonly Vector3 MaxSize = new(4.5f, 1.5f, 1.5f);

    /// <summary>
    /// Positions the box at a random location.
    /// </summary>
    public void Random(Random random)
    {
        SetState(
            position: random.RandomVector(MinPos, MaxPos),
            orientation: random.RandomQuaternion(),
            extents: random.RandomVector(MinSize, MaxSize),
            velocity: new Vector3()
        );
    }

    /// <summary>
    /// Sets the box to a specific location.
    /// </summary>
    public void SetState(
        Vector3 position,
        Quaternion orientation,
        Vector3 extents,
        Vector3 velocity)
    {
        Body.Position = position;

        Body.Orientation = orientation;
        Body.Velocity = velocity;
        Body.Rotation = new();

        HalfSize = extents;

        SetAutoMass();
        RecalculateInertiaTensor();

        Body.LinearDamping = 0.95f;
        Body.AngularDamping = 0.8f;
        Body.ClearAccumulators();
        Body.Acceleration = new(0, -10f, 0);

        Body.SetAwake();

        Body.CalculateDerivedData();
    }

    public void SetAutoMass()
    {
        float mass = (float)(HalfSize.X * HalfSize.Y * HalfSize.Z * 8.0f);
        Body.Mass = mass;
    }

    public void RecalculateInertiaTensor()
    {
        Matrix3 tensor = new();
        tensor.SetBlockInertiaTensor(HalfSize, Body.Mass);
        Body.SetInertiaTensor(tensor);
    }

    public BoundingBox GetBoundingBox()
    {
        // Update the transform to ensure we have the current rotation/position
        CalculateInternals();

        // Get the rotation matrix (3x3 part of the transform)
        Matrix3 rotation = Transform.Matrix3;

        // Transform each local axis by the rotation and take absolute values
        // This gives us the maximum extent the rotated box reaches along each world axis
        Vector3 worldHalfSize = new Vector3(
            MathF.Abs(rotation.Data[0] * HalfSize.X) + MathF.Abs(rotation.Data[1] * HalfSize.Y) +
            MathF.Abs(rotation.Data[2] * HalfSize.Z),
            MathF.Abs(rotation.Data[3] * HalfSize.X) + MathF.Abs(rotation.Data[4] * HalfSize.Y) +
            MathF.Abs(rotation.Data[5] * HalfSize.Z),
            MathF.Abs(rotation.Data[6] * HalfSize.X) + MathF.Abs(rotation.Data[7] * HalfSize.Y) +
            MathF.Abs(rotation.Data[8] * HalfSize.Z)
        );

        return new BoundingBox(
            center: Body.Position,
            halfSize: worldHalfSize);
    }
}