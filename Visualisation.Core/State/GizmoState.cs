using Visualisation.Core.Display.Gizmos;

namespace Visualisation.Core.State;

public class GizmoState
{
    public IGizmo? ActiveGizmo { get; set; }
    public GizmoType ActiveGizmoType { get; set; } = GizmoType.None;
}