using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

using Sphere = Engine.RigidBodies.Sphere;

namespace Visualisation.Core.GameObjects;

public sealed class Ball : GameObjectCollisionPrimitive, IBoxable, IScaleGizmoTarget
{
    public Sphere EngineBall { get; init; } = new();
    public override RigidBody PhysicsObject => EngineBall.Body;
    protected override IMesh Mesh { get; set; } = new SphereMesh();

    public override Vector3 Position
    {
        get => EngineBall.Body.Position.ToOpenTK();
    }

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

    public Vector3 Scale
    {
        get => new(EngineBall.Radius);
        set
        {
            EngineBall.Radius = value.X;
        }
    }

    public Vector3 Offset => new(EngineBall.Radius);

    public Vector3 GetTargetScale(Vector3 scale, float factor, GizmoAxis axis)
    {
        scale.X *= factor;
        return scale;
    }
}