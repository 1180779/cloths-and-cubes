using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class BallScaleGizmoAdapter : IScaleGizmoTarget
{
    private readonly Ball _ball;

    public BallScaleGizmoAdapter(Ball ball)
    {
        _ball = ball;
    }

    public Vector3 AxisPosition => _ball.Position;
    public Quaternion AxisOrientation => _ball.EngineBall.Body.Orientation.ToOpenTK();

    public Vector3 Scale
    {
        get => new(_ball.EngineBall.Radius);
        set => _ball.EngineBall.Radius = value.X;
    }

    public Vector3 Offset => new(_ball.EngineBall.Radius);

    public Vector3 GetTargetScale(Vector3 scale, float factor, GizmoAxis axis)
    {
        scale.X *= factor;
        return scale;
    }
}