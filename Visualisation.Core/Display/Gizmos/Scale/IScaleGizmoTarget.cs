namespace Visualisation.Core.Display.Gizmos.Scale;

public interface IScaleGizmoTarget : IGizmoTarget
{
    public Vector3 Scale { get; set; }
    public Vector3 Offset { get; }
    public Vector3 GetTargetScale(Vector3 scale, float factor, GizmoAxis axis);
}