using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

// TODO: implement cone interactions on the engine side
public sealed class Cone : GameObjectCollisionPrimitive, IBoxable
{
    public Cone()
    {
        Material = MaterialConstant.Porcelain;
    }

    // For now, we don't have a physics cone, so we'll just store position/rotation/scale locally
    // or reuse a cylinder body if we wanted physics, but the request is just to display a sample cone.
    // Let's just use simple properties for display purposes.

    public Engine.RigidBodies.Cone EngineCone = new();
    protected override IMesh Mesh { get; set; } = new ConeMesh();
    public override object PhysicsObject => new();

    public override Matrix4 Model
    {
        get
        {
            var position = EngineCone.Body.Position.ToOpenTK();
            var scale = new Vector3(EngineCone.Radius, EngineCone.Radius, EngineCone.Height);

            var q = EngineCone.Body.Orientation.ToOpenTK();

            // Normalize to guard against drift
            if (MathF.Abs(1f - q.Length) > 1e-3f)
                q = Quaternion.Normalize(q);

            return GenerateModelMatrix(position, scale, q);
        }
    }

    public override Vector3 Position => EngineCone.Body.Position.ToOpenTK();

    public BoundingBox GetBoundingBox()
    {
        return EngineCone.GetBoundingBox();
    }

    public override CollisionPrimitive EngineCollisionPrimitive => EngineCone;
}