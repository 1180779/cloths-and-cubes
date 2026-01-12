namespace Visualisation.Core.State;

public class EditorState
{
    public SelectionState Selection { get; } = new();
    public GizmoState Gizmo { get; } = new();
}