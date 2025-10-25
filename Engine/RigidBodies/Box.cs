using Engine.Collision;

namespace Engine.RigidBodies;

public class Box : CollisionBox
{
    public bool IsOverlapping { get; set; } = false; // previously used for some rendering (???)


    static readonly Vector3 MinPos = new(-15, 5, -15);
    static readonly Vector3 MaxPos = new(15, 20, 15);
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

        float mass = (float)(HalfSize.X * HalfSize.Y * HalfSize.Z * 8.0f);
        Body.Mass = mass;

        Matrix3 tensor = new();
        tensor.SetBlockInertiaTensor(HalfSize, mass);
        Body.SetInertiaTensor(tensor);

        Body.LinearDamping = 0.95f;
        Body.AngularDamping = 0.8f;
        Body.ClearAccumulators();
        Body.Acceleration = new(0, -10f, 0);

        Body.SetAwake();

        Body.CalculateDerivedData();
    }
}