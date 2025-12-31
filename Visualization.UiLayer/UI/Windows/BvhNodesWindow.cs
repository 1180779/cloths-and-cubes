using Engine.Collision.Bounding_Volume_Hierarchy;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.UI.Windows;

public sealed class BvhNodesWindow : IWindow
{
    private readonly bool[] _bvhLevelsToRender = Enumerable.Repeat(false, 20).ToArray();

    private readonly Vector3[] _levelColors =
    [
        new(1.0f, 0.0f, 0.0f), // Red
        new(0.0f, 1.0f, 0.0f), // Green
        new(0.0f, 0.0f, 1.0f), // Blue
        new(1.0f, 1.0f, 0.0f), // Yellow
        new(0.0f, 1.0f, 1.0f), // Cyan
        new(1.0f, 0.0f, 1.0f) // Magenta
    ];

    private bool _bvhLeafsRender;
    private bool _parallelizeBuilding = false;
    private readonly Vector3 _leafColor = new(0.5f, 0.5f, 1);

    public string Name => "BVH Nodes";

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin("Bvh Nodes to render", ref isOpen))
        {
            if (ImGui.Button("Select All"))
            {
                for (int i = 0; i < _bvhLevelsToRender.Length; i++)
                    _bvhLevelsToRender[i] = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Deselect All"))
            {
                for (int i = 0; i < _bvhLevelsToRender.Length; i++)
                    _bvhLevelsToRender[i] = false;
            }

            ImGui.Checkbox("Leafs", ref _bvhLeafsRender);

            for (var i = 0; i < _bvhLevelsToRender.Length; i++)
            {
                var color = _levelColors[i % _levelColors.Length];
                ImGui.PushStyleColor(ImGuiCol.Text,
                    new System.Numerics.Vector4(new System.Numerics.Vector3(color.X, color.Y, color.Z), 1.0f));
                ImGui.Checkbox($"Level {i}", ref _bvhLevelsToRender[i]);
                ImGui.PopStyleColor();
            }
        }

        ImGui.End();
    }

    public void DebugRenderInScene(Shader sh, BVH bvh)
    {
        BvhWireframe bvhWireframe = new(bvh)
        {
            RenderLeafs = _bvhLeafsRender,
            LeafColor = _leafColor,
            LevelColors = _levelColors,
            LevelsToRender = _bvhLevelsToRender
                .Select((enabled, index) => new { enabled, index })
                .Where(x => x.enabled)
                .Select(x => x.index)
                .ToArray()
        };
        bvhWireframe.Render(sh);
    }

    public sealed record State
    {
        public bool[] BvhLevelsToRender { get; init; } = [];
        public bool BvhLeafsRender { get; init; }
        public bool ParallelizeBuilding { get; init; }
    }

    public State SaveState()
    {
        return new State
        {
            BvhLeafsRender = _bvhLeafsRender,
            BvhLevelsToRender = _bvhLevelsToRender,
            ParallelizeBuilding = _parallelizeBuilding,
        };
    }

    public void RestoreState(State state)
    {
        _bvhLeafsRender = state.BvhLeafsRender;
        for (int i = 0; i < _bvhLevelsToRender.Length; i++)
            _bvhLevelsToRender[i] = state.BvhLevelsToRender[i];
        _parallelizeBuilding = state.ParallelizeBuilding;
    }
}