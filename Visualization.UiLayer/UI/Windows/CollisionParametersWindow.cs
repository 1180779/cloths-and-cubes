using Engine.Collision;

using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class CollisionParametersWindow(CollisionData collisionData) : IWindow
{
    private CollisionData _collisionData = collisionData; /* borrowed */

    public record State(Real Friction, Real Restitution, Real Tolerance);

    public State SaveState()
    {
        return new State(_collisionData.Friction, _collisionData.Restitution, _collisionData.Tolerance);
    }

    public void RestoreState(State state)
    {
        _collisionData.Friction = state.Friction;
        _collisionData.Restitution = state.Restitution;
        _collisionData.Tolerance = state.Tolerance;
    }

    public string Name => "Collision Parameters";
    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin("Collision Data", ref isOpen))
        {
            ImGui.SliderFloat("Friction", ref _collisionData.Friction, 0.0f, 1.0f);
            ImGui.SliderFloat("Restitution", ref _collisionData.Restitution, 0.0f, 1.0f);
            ImGui.SliderFloat("Tolerance", ref _collisionData.Tolerance, 0.0f, 1.0f);
        }
        ImGui.End();
    }
}