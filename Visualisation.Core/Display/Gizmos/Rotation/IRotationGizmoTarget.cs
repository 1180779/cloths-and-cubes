namespace Visualisation.Core.Display.Gizmos.Rotation;

public interface IRotationGizmoTarget : IGizmoTarget
{
    public Quaternion Orientation { get; set; }
}