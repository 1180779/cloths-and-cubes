using Engine.Collision;

using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Plane : GameObject
{
    public CollisionPlane EnginePlane = new() { Direction = new Engine.Vector3(0, 1, 0), Offset = 0, };

    protected override IMesh Mesh { get; set; } = new PlaneMesh();
    public override object PhysicsObject => EnginePlane;

    public override Matrix4 Model
    {
        get
        {
            var normal = new Vector3(EnginePlane.Direction.X, EnginePlane.Direction.Y, EnginePlane.Direction.Z);
            Quaternion rotation;

            if (MathHelper.ApproximatelyEquivalent(Math.Abs(Vector3.Dot(Vector3.UnitY, normal)), 1.0f,
                Engine.Core.Epsilon))
            {
                rotation = normal.Y > 0 ? Quaternion.Identity : Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.Pi);
            }
            else
            {
                var axis = Vector3.Cross(Vector3.UnitY, normal).Normalized();
                var angle = (float)Math.Acos(Vector3.Dot(Vector3.UnitY, normal));
                rotation = Quaternion.FromAxisAngle(axis, angle);
            }

            var position = normal * EnginePlane.Offset;
            var scale = new Vector3(1000.0f, 1000.0f, 1000.0f);

            return GenerateModelMatrix(position, scale, rotation);
        }
    }
}