using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Box : GameObjectCollisionPrimitive, IBoxable
{
    public Engine.RigidBodies.Box EngineBox { get; init; } = new();
    protected override IMesh Mesh { get; set; } = new CubeMesh();
    public override object PhysicsObject => EngineBox.Body;

    public override Vector3 Position =>
        new(EngineBox.Body.Position.X, EngineBox.Body.Position.Y, EngineBox.Body.Position.Z);

    public override Matrix4 Model
    {
        get
        {
            var position = new Vector3(
                EngineBox.Body.Position.X,
                EngineBox.Body.Position.Y,
                EngineBox.Body.Position.Z);

            var scale = 2.0f * new Vector3(
                EngineBox.HalfSize.X,
                EngineBox.HalfSize.Y,
                EngineBox.HalfSize.Z);

            var q = new Quaternion(
                EngineBox.Body.Orientation.I,
                EngineBox.Body.Orientation.J,
                EngineBox.Body.Orientation.K,
                EngineBox.Body.Orientation.R);

            // Normalize to guard against drift
            if (MathF.Abs(1f - q.Length) > 1e-3f)
                q = Quaternion.Normalize(q);

            return GenerateModelMatrix(position, scale, q);
        }
    }

    public BoundingBox GetBoundingBox()
    {
        return EngineBox.GetBoundingBox();
    }

    public override CollisionPrimitive EngineCollisionPrimitive => EngineBox;
}