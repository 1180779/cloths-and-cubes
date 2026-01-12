using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class BoxScaleGizmoAdapter : IScaleGizmoTarget
{
    private readonly Box _box;

    public BoxScaleGizmoAdapter(Box box)
    {
        _box = box;
    }

    public Vector3 AxisPosition => _box.Position;
    public Quaternion AxisOrientation => _box.EngineBox.Body.Orientation.ToOpenTK();

    public Vector3 Scale
    {
        get => _box.EngineBox.HalfSize.ToOpenTK();
        set => _box.EngineBox.HalfSize = value.ToEngine();
    }

    public Vector3 Offset => _box.EngineBox.HalfSize.ToOpenTK();

    public Vector3 GetTargetScale(Vector3 scale, float factor, GizmoAxis axis)
    {
        switch (axis)
        {
            case GizmoAxis.X:
                scale.X *= factor;
                break;
            case GizmoAxis.Y:
                scale.Y *= factor;
                break;
            case GizmoAxis.Z:
                scale.Z *= factor;
                break;
        }

        return scale;
    }
}