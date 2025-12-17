using Visualisation.Core.GameObjects;

using Visualization.UiLayer.UI.Windows;

namespace Visualization.UiLayer.Applications;

public class ApplicationState
{
    public GraphicsSettingsWindow.State? GraphicsSettings { get; set; }
    public CascadingShadowMapsWindow.State? CascadingShadowMaps { get; set; }
    public BvhNodesWindow.State? BvhNodes { get; set; }
    public CollisionParametersWindow.State? CollisionParameters { get; set; }
    public BoxesDemoSettingsWindow.State? ClothSettings { get; set; }
    public SelectionManagerWindow.State? SelectionSettings { get; set; }
    public WindowsManager.State? WindowsState { get; set; }
}