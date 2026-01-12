using Engine.Rays;

namespace Visualisation.Core.State;

public sealed class SelectionState
{
    public object? HoveredObject { get; set; }
    public object? SelectedObject { get; set; }
    public bool IsSelectionEnabled { get; set; } = true;
    public bool UnselectOnSelectedObjectClick { get; set; } = true;
    public Ray? HoverRay { get; set; }
    public Ray? SelectionRay { get; set; }
    public float HoverDistance { get; set; }
    public float SelectionDistance { get; set; }
}