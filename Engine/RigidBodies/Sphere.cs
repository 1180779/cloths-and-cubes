using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies;

public class Sphere : CollisionSphere, IBoxable
{
    public bool IsOverlapping { get; set; } = false; // previously used for some rendering (???)

    static readonly Vector3 MinPos = new(-15, 5, -15);
    static readonly Vector3 MaxPos = new(15, 20, 15);
    static readonly Real MinRadius = 0.5f;
    static readonly Real MaxRadius = 2.5f;

    /// <summary>
    /// Positions the box at a random location.
    /// </summary>
    public void Random(Random random)
    {
        SetState(
            position: random.RandomVector(MinPos, MaxPos),
            orientation: random.RandomQuaternion(),
            radius: random.RandomReal(MinRadius, MaxRadius),
            velocity: new Vector3()
        );
    }

    /// <summary>
    /// Sets the box to a specific location.
    /// </summary>
    public void SetState(
        Vector3 position,
        Quaternion orientation,
        float radius,
        Vector3 velocity)
    {
        Body.Position = position;

        Body.Orientation = orientation;
        Body.Velocity = velocity;
        Body.Rotation = new();

        Radius = radius;

        float mass = (float)(Math.PI * Radius * Radius * Radius * 4.0f / 3.0f);
        Body.Mass = mass;

        Matrix3 tensor = new();
        tensor.SetSphereInertiaTensor(Radius, mass);
        Body.SetInertiaTensor(tensor);

        Body.LinearDamping = 0.95f;
        Body.AngularDamping = 0.8f;
        Body.ClearAccumulators();
        Body.Acceleration = new(0, -10f, 0);

        Body.SetAwake();

        Body.CalculateDerivedData();
    }

    public Collision.Bounding_Volume_Hierarchy.BoundingBox GetBoundingBox()
    {
        return new Collision.Bounding_Volume_Hierarchy.BoundingBox(
            center: this.Body.Position,
            halfSize: new Vector3(Radius, Radius, Radius));
    }
}