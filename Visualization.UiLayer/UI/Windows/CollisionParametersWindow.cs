using Engine.Collision;

using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public sealed class CollisionParametersWindow(CollisionData collisionData) : IWindow
{
    private CollisionData _collisionData = collisionData; /* borrowed */

    public sealed record State
    {
        public Real Friction { get; init; }
        public Real Restitution { get; init; }
        public Real Tolerance { get; init; }
    }

    public State SaveState()
    {
        return new State
        {
            Friction = _collisionData.Friction,
            Restitution = _collisionData.Restitution,
            Tolerance = _collisionData.Tolerance
        };
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