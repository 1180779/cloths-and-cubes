using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class CylinderScaleGizmoAdapter : IScaleGizmoTarget
{
    private readonly Cylinder _cylinder;

    public CylinderScaleGizmoAdapter(Cylinder cylinder)
    {
        _cylinder = cylinder;
    }

    public Vector3 AxisPosition => _cylinder.Position;
    public Quaternion AxisOrientation => _cylinder.EngineCylinder.Body.Orientation.ToOpenTK();

    public Vector3 Scale
    {
        get => new(_cylinder.EngineCylinder.Radius, _cylinder.EngineCylinder.Radius, _cylinder.EngineCylinder.Height);
        set
        {
            _cylinder.EngineCylinder.Height = value.Z;
            _cylinder.EngineCylinder.Radius = value.X;
            _cylinder.EngineCylinder.Body.SetAwake();
            _cylinder.EngineCylinder.Body.CalculateDerivedData();
            _cylinder.EngineCylinder.CalculateInternals();
        }
    }

    public Vector3 Offset => new(_cylinder.EngineCylinder.Radius, _cylinder.EngineCylinder.Radius,
        _cylinder.EngineCylinder.Height / 2.0f);

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