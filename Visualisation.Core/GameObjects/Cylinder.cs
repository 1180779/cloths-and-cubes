using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Cylinder : GameObjectRigidBody, IBoxable
{
    public Cylinder()
    {
        Material = MaterialConstant.Porcelain;
    }

    public Engine.RigidBodies.Cylinder EngineCylinder = new();

    protected override IMesh Mesh { get; set; } = new CylinderMesh();
    public override object PhysicsObject => new object();

    public override Vector3 Position => EngineCylinder.Body.Position.ToOpenTK();

    public override Matrix4 Model
    {
        get
        {
            var position = EngineCylinder.Body.Position.ToOpenTK();
            var scale = new Vector3(EngineCylinder.Radius, EngineCylinder.Radius, EngineCylinder.Height);

            var q = EngineCylinder.Body.Orientation.ToOpenTK();

            // Normalize to guard against drift
            if (MathF.Abs(1f - q.Length) > 1e-3f)
                q = Quaternion.Normalize(q);

            return GenerateModelMatrix(position, scale, q);
        }
    }

    public BoundingBox GetBoundingBox()
    {
        return EngineCylinder.GetBoundingBox();
    }

    public override RigidBody EngineRigidBody => EngineCylinder.Body;
}