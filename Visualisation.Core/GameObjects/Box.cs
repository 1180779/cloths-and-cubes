using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Box : GameObjectRigidBody, IBoxable
{
    public Engine.RigidBodies.Box EngineBox { get; private set; } = new();
    protected override IMesh Mesh { get; set; } = new CubeMesh();
    public override object PhysicsObject => EngineBox.Body;

    public override Vector3 Position =>
        new(EngineBox.Body.Position.X, EngineBox.Body.Position.Y, EngineBox.Body.Position.Z);

    public override Matrix4 Model
    {
        get
        {
            var position = new Vector3(
                (float)EngineBox.Body.Position.X,
                (float)EngineBox.Body.Position.Y,
                (float)EngineBox.Body.Position.Z);

            var scale = 2.0f * new Vector3(
                (float)EngineBox.HalfSize.X,
                (float)EngineBox.HalfSize.Y,
                (float)EngineBox.HalfSize.Z);

            var q = new Quaternion(
                (float)EngineBox.Body.Orientation.I,
                (float)EngineBox.Body.Orientation.J,
                (float)EngineBox.Body.Orientation.K,
                (float)EngineBox.Body.Orientation.R);

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
    
    public override RigidBody EngineRigidBody => EngineBox.Body;
}