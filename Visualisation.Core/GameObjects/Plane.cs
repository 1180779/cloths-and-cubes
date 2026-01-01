using Engine.Collision;

using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Plane : GameObject, ITranslationGizmoTarget, IRotationGizmoTarget
{
    public CollisionPlane EnginePlane = new() { Direction = new Engine.Vector3(0, 1, 0), Offset = 0, };

    protected override IMesh Mesh { get; set; } = new PlaneMesh();
    public override object PhysicsObject => EnginePlane;

    public override Matrix4 Model
    {
        get
        {
            var normal = EnginePlane.Direction.ToOpenTK();
            var position = normal * EnginePlane.Offset;
            var scale = new Vector3(1000.0f, 1000.0f, 1000.0f);

            return GenerateModelMatrix(position, scale, Orientation);
        }
    }

    public Vector3 AxisPosition => EnginePlane.Direction.ToOpenTK() * EnginePlane.Offset;
    public Quaternion AxisOrientation => Orientation;

    Vector3 ITranslationGizmoTarget.Position
    {
        get => EnginePlane.Direction.ToOpenTK() * EnginePlane.Offset;
        set
        {
            EnginePlane.Offset = Vector3.Dot(value, EnginePlane.Direction.ToOpenTK());
        }
    }

    public Quaternion Orientation
    {
        get
        {
            var normal = EnginePlane.Direction.ToOpenTK();

            if (MathHelper.ApproximatelyEquivalent(Math.Abs(Vector3.Dot(Vector3.UnitY, normal)), 1.0f,
                Engine.Core.Epsilon))
            {
                return normal.Y > 0 ? Quaternion.Identity : Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.Pi);
            }

            var axis = Vector3.Cross(Vector3.UnitY, normal).Normalized();
            var angle = (float)Math.Acos(Vector3.Dot(Vector3.UnitY, normal));
            return Quaternion.FromAxisAngle(axis, angle);
        }
        set
        {
            var dir = Vector3.Transform(Vector3.UnitY, value);
            EnginePlane.Direction = dir.ToEngine();
        }
    }
}