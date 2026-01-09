using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

// TODO: implement cylinder interactions on the engine side
public sealed class Cylinder : GameObjectCollisionPrimitive, IBoxable, IScaleGizmoTarget
{
    public Cylinder()
    {
        Material = MaterialConstant.Porcelain;
    }

    public Engine.RigidBodies.Cylinder EngineCylinder = new();

    protected override IMesh Mesh { get; set; } = new CylinderMesh();
    public override object PhysicsObject => new();

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

    public override CollisionPrimitive EngineCollisionPrimitive => EngineCylinder;

    public Vector3 Scale
    {
        get => new(EngineCylinder.Radius, EngineCylinder.Radius, EngineCylinder.Height);
        set
        {
            EngineCylinder.Height = value.Z;
            EngineCylinder.Radius = value.X;
            EngineCylinder.Body.SetAwake();
            EngineCylinder.Body.CalculateDerivedData();
            EngineCylinder.CalculateInternals();
        }
    }

    public Vector3 Offset => new(EngineCylinder.Radius, EngineCylinder.Radius, EngineCylinder.Height / 2.0f);

    public Vector3 GetTargetScale(Vector3 scale, float factor, GizmoAxis axis)
    {
        if (axis == GizmoAxis.Z)
        {
            scale.Z *= factor;
        }
        else
        {
            scale.X *= factor;
        }

        return scale;
    }
}