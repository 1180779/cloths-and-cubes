namespace Visualisation.Core.State;

public sealed class EditorState : IDisposable
{
    public DraggingState DraggingState { get; } = new();
    public SelectionState Selection { get; } = new();
    public GizmoState Gizmo { get; } = new();

    public void Dispose()
    {
        DraggingState.Dispose();
    }
}