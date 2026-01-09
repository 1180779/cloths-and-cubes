using Visualization.UiLayer.UI.Windows;

namespace Visualization.UiLayer.Applications;

public sealed record ApplicationState
{
    public GraphicsSettingsWindow.State? GraphicsSettings { get; set; }
    public CascadingShadowMapsWindow.State? CascadingShadowMaps { get; set; }
    public BvhNodesWindow.State? BvhNodes { get; set; }
    public CollisionParametersWindow.State? CollisionParameters { get; set; }
    public BoxesDemoSettingsWindow.State? ClothSettings { get; set; }
    public SelectionManagerWindow.State? SelectionSettings { get; set; }
    public WindowsManager.State? WindowsState { get; set; }
    public GizmoSettingsWindow.State? GizmoSettings { get; set; }
    public PhysicsControlWindow.State? PhysicsControl { get; set; }
    public SceneManagementWindow.State? SceneManagement { get; set; }
}