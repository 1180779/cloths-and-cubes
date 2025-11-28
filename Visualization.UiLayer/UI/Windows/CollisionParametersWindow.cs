using Engine.Collision;

using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public static class CollisionParametersWindow
{
    public static void Draw(CollisionData collisionData)
    {
        ImGui.Begin("Collision Data");
        ImGui.SliderFloat("Friction", ref collisionData.Friction, 0.0f, 1.0f);
        ImGui.SliderFloat("Restitution", ref collisionData.Restitution, 0.0f, 1.0f);
        ImGui.SliderFloat("Tolerance", ref collisionData.Tolerance, 0.0f, 1.0f);
        ImGui.End();
    }
}