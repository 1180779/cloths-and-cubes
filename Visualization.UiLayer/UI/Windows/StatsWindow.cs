using ImGuiNET;

using Visualisation.Core.GameObjects.Scenes;

namespace Visualization.UiLayer.UI.Windows;

public class StatsWindow(SceneManager sceneManager) : IWindow
{
    private readonly SceneManager _sceneManager = sceneManager; /* borrowed */

    public string Name => "Stats";

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            Vector3 cameraPos = _sceneManager.CamerasManager.CurrentCamera.Position;

            ImGui.Text($"Application FPS: {ImGui.GetIO().Framerate:0.0}");
            ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:0.0} ms");
            ImGui.Text($"Camera Position: {cameraPos.X:0.00}, {cameraPos.Y:0.00}, {cameraPos.Z:0.00}");
        }

        ImGui.End();
    }
}