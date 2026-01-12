using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Gizmos;

public enum GizmoSpace
{
    Global,
    Local
}

public enum GizmoAxis
{
    None = -1,
    X = 0,
    Y = 1,
    Z = 2
}

public delegate void TargetChangedEventHandler(IGizmoTarget collisionPrimitive);

public interface IGizmo : IDisposable
{
    public event TargetChangedEventHandler TargetChangedEvent;

    public float DefaultTransparency { get; set; }
    public Vector4 SelectionColor { get; set; }
    public Vector4 HoverColor { get; set; }

    public GizmoSpace Space { get; set; }
    bool ConstantScreenSize { get; set; }
    public float HandleSize { get; set; }

    public IGizmoTarget? Target { get; set; }

    /// <summary>
    /// Returns true if the gizmo is currently in use
    /// </summary>
    public bool IsActive { get; }

    public void Render(CameraBase camera);
    public bool HandleInput(IInputProvider input, Vector2 mousePos, CameraBase camera, Vector2i screenSize);
}

public interface IGizmo<in TTarget> : IDisposable, IGizmo where TTarget : IGizmoTarget;