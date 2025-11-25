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
            var position = new Vector3(0.0f, 0.0f, 0.0f);
            var scale = new Vector3(1000.0f, 1000.0f, 1000.0f);
            var q = Quaternion.Identity;

            return GenerateModelMatrix(position, scale, q);
        }
    }
}