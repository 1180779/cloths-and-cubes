using Visualization.UiLayer.UI.Windows;

namespace Visualization.UiLayer.Applications;

public sealed record ApplicationState
{
    public GraphicsSettingsWindow.State? GraphicsSettings;
    public CascadingShadowMapsWindow.State? CascadingShadowMaps;
    public BvhNodesWindow.State? BvhNodes;
    public CollisionParametersWindow.State? CollisionParameters;
    public BoxesDemoSettingsWindow.State? ClothSettings;
    public SelectionManagerWindow.State? SelectionSettings;
    public WindowsManager.State? WindowsState;
    public GizmoSettingsWindow.State? GizmoSettings;
    public PhysicsControlWindow.State? PhysicsControl;
    public SceneManagementWindow.State? SceneManagement;
}