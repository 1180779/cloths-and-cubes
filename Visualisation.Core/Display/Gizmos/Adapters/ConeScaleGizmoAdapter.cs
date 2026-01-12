using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class ConeScaleGizmoAdapter : IScaleGizmoTarget
{
    private readonly Cone _cone;

    public ConeScaleGizmoAdapter(Cone cone)
    {
        _cone = cone;
    }

    public Vector3 AxisPosition => _cone.Position;
    public Quaternion AxisOrientation => _cone.EngineCone.Body.Orientation.ToOpenTK();

    public Vector3 Scale
    {
        get => new(_cone.EngineCone.Radius, _cone.EngineCone.Radius, _cone.EngineCone.Height);
        set
        {
            _cone.EngineCone.Height = value.Z;
            _cone.EngineCone.Radius = value.X;
            _cone.EngineCone.Body.SetAwake();
            _cone.EngineCone.Body.CalculateDerivedData();
            _cone.EngineCone.CalculateInternals();
        }
    }

    public Vector3 Offset => new(_cone.EngineCone.Radius, _cone.EngineCone.Radius, _cone.EngineCone.Height / 2.0f);

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