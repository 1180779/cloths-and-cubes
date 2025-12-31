using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects;
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

public interface IGizmo : IDisposable
{
    public delegate void TargetChangedEventHandler(GameObjectCollisionPrimitive collisionPrimitive);

    public event TargetChangedEventHandler TargetChangedEvent;

    public float DefaultTransparency { get; set; }
    public Vector4 SelectionColor { get; set; }
    public Vector4 HoverColor { get; set; }

    public GizmoSpace Space { get; set; }
    bool ConstantScreenSize { get; set; }
    public float HandleSize { get; set; }

    public GameObjectCollisionPrimitive? Target { get; set; }

    public void Render(CameraBase camera);
    public bool HandleInput(IInputProvider input, Vector2 mousePos, CameraBase camera, Vector2i screenSize);
}