namespace Visualisation.Core.Display.Gizmos;

public interface IGizmoTarget
{
    /// <summary>
    /// The position of the axis in 3D space.
    /// </summary>
    public Vector3 AxisPosition { get; }

    /// <summary>
    /// The orientation of the axis in 3D space. Used to align gizmos when in local space.
    /// </summary>
    public Quaternion AxisOrientation { get; }
}