using Engine.Rays;

namespace Visualisation.Core.State;

public class SelectionState
{
    public object? HoveredObject { get; set; }
    public object? SelectedObject { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool UnselectOnSelectedObjectClick { get; set; } = true;
    public Ray? LastRay { get; set; }
    public float SelectedObjectDistance { get; set; }
}