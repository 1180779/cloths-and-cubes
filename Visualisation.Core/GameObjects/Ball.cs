using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

using Sphere = Engine.RigidBodies.Sphere;

namespace Visualisation.Core.GameObjects;

public sealed class Ball : GameObjectCollisionPrimitive, IBoxable
{
    public Sphere EngineBall { get; init; } = new();
    public override RigidBody PhysicsObject => EngineBall.Body;
    protected override IMesh Mesh { get; set; } = new SphereMesh();

    public override Vector3 Position =>
        new(EngineBall.Body.Position.X, EngineBall.Body.Position.Y, EngineBall.Body.Position.Z);

    public override Matrix4 Model
    {
        get
        {
            var position = new Vector3(EngineBall.Body.Position.X, EngineBall.Body.Position.Y,
                EngineBall.Body.Position.Z);

            var scale = new Vector3(EngineBall.Radius, EngineBall.Radius, EngineBall.Radius);

            var q = new Quaternion(EngineBall.Body.Orientation.I, EngineBall.Body.Orientation.J,
                EngineBall.Body.Orientation.K, EngineBall.Body.Orientation.R);

            if (MathF.Abs(1f - q.Length) > 1e-3f)
                q = Quaternion.Normalize(q);

            return GenerateModelMatrix(position, scale, q);
        }
    }

    public BoundingBox GetBoundingBox()
    {
        return EngineBall.GetBoundingBox();
    }

    public override CollisionPrimitive EngineCollisionPrimitive => EngineBall;
}