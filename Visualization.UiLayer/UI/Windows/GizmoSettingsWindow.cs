using System.Diagnostics;

using ImGuiNET;

using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.GameObjects.Scenes;

namespace Visualization.UiLayer.UI.Windows;

public sealed class GizmoSettingsWindow : IWindow
{
    private readonly SceneManager _sceneManager;

    public GizmoSettingsWindow(SceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    public string Name => "Gizmo Settings";

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            DrawGizmoSelection();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Gizmo Settings
            if (_sceneManager.ActiveGizmo == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f),
                    "No active gizmo. Select an object and choose a gizmo type.");
            }
            else
            {
                DrawGizmo();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Quick Actions
        if (ImGui.CollapsingHeader("Quick Actions"))
        {
            if (ImGui.Button("Reset All Gizmo Scales"))
            {
                _sceneManager.ResetAllGizmoScales();
            }

            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1.0f),
                "(Sets all handle sizes to 1.0)");
        }

        ImGui.End();
    }

    private void DrawGizmoCoordinateSpace(IGizmo gizmo)
    {
        bool isLocal = gizmo.Space == GizmoSpace.Local;
        if (ImGui.RadioButton("Global", !isLocal))
        {
            gizmo.Space = GizmoSpace.Global;
        }

        ImGui.SameLine();
        if (ImGui.RadioButton("Local", isLocal))
        {
            gizmo.Space = GizmoSpace.Local;
        }
    }

    private void DrawGizmoHandleSize(IGizmo gizmo)
    {
        float handleSize = gizmo.HandleSize;
        if (ImGui.SliderFloat("Handle Size", ref handleSize, 0.1f, 5.0f, "%.2f"))
        {
            gizmo.HandleSize = handleSize;
        }

        if (ImGui.Button("Reset Handle Size"))
        {
            gizmo.HandleSize = 1.0f;
        }
    }

    private void DrawGizmo()
    {
        Debug.Assert(_sceneManager.ActiveGizmo is not null);

        IGizmo gizmo = _sceneManager.ActiveGizmo;

        ImGui.SeparatorText("Coordinate Space");
        DrawGizmoCoordinateSpace(gizmo);

        ImGui.Spacing();
        ImGui.SeparatorText("Handle Settings");
        DrawGizmoHandleSize(gizmo);

        bool constantScreenSize = gizmo.ConstantScreenSize;
        if (ImGui.Checkbox("Constant Screen Size", ref constantScreenSize))
        {
            gizmo.ConstantScreenSize = constantScreenSize;
        }

        if (gizmo is RotationGizmo rotationGizmo)
        {
            ImGui.Spacing();
            ImGui.SeparatorText("Rotation Settings");

            ImGui.SliderFloat("Rotation Sensitivity", ref rotationGizmo.Sensitivity, 0.001f, 0.1f, "%.4f");

            if (ImGui.Button("Reset Rotation Sensitivity"))
            {
                rotationGizmo.Sensitivity = 0.01f;
            }

            ImGui.TextWrapped(
                "Tip: Increase sensitivity for faster rotation, decrease for more precise control.");
        }
    }

    private void DrawGizmoSelection()
    {
        ImGui.SeparatorText("Gizmo Type");

        var previousType = _sceneManager.ActiveGizmoType;
        var selectedGizmoType = _sceneManager.ActiveGizmoType;
        if (ImGui.RadioButton("None", selectedGizmoType == GizmoType.None))
        {
            selectedGizmoType = GizmoType.None;
        }

        if (ImGui.RadioButton("Translation (T)", selectedGizmoType == GizmoType.Translation))
        {
            selectedGizmoType = GizmoType.Translation;
        }

        if (ImGui.RadioButton("Scale (Y)", selectedGizmoType == GizmoType.Scale))
        {
            selectedGizmoType = GizmoType.Scale;
        }

        if (ImGui.RadioButton("Rotation (U)", selectedGizmoType == GizmoType.Rotation))
        {
            selectedGizmoType = GizmoType.Rotation;
        }

        if (previousType != selectedGizmoType)
        {
            _sceneManager.SetActiveGizmoType(selectedGizmoType);
        }
    }

    public record GizmoState
    {
        public float HandleSize { get; init; }
        public GizmoSpace Space { get; init; }
        public bool ConstantScreenSize { get; init; }
    }

    public sealed record RotationGizmoState : GizmoState
    {
        public float Sensitivity { get; init; }
    }

    public sealed class State
    {
        public GizmoType SelectedGizmoType { get; init; } = GizmoType.None;
        public GizmoState? TranslationGizmoState { get; init; }
        public GizmoState? ScaleGizmoState { get; init; }
        public RotationGizmoState? RotationGizmoState { get; init; }
    }

    public State SaveState()
    {
        var state = new State
        {
            SelectedGizmoType = _sceneManager.ActiveGizmoType,
            TranslationGizmoState =
                new GizmoState
                {
                    HandleSize = _sceneManager.TranslationGizmo.HandleSize,
                    Space = _sceneManager.TranslationGizmo.Space,
                    ConstantScreenSize = _sceneManager.TranslationGizmo.ConstantScreenSize
                },
            ScaleGizmoState =
                new GizmoState
                {
                    HandleSize = _sceneManager.ScaleGizmo.HandleSize,
                    Space = _sceneManager.ScaleGizmo.Space,
                    ConstantScreenSize = _sceneManager.ScaleGizmo.ConstantScreenSize
                },
            RotationGizmoState = new RotationGizmoState
            {
                HandleSize = _sceneManager.RotationGizmo.HandleSize,
                Space = _sceneManager.RotationGizmo.Space,
                ConstantScreenSize = _sceneManager.RotationGizmo.ConstantScreenSize,
                Sensitivity = _sceneManager.RotationGizmo.Sensitivity
            },
        };
        return state;
    }

    public void RestoreState(State state)
    {
        _sceneManager.SetActiveGizmoType(state.SelectedGizmoType);

        if (state.TranslationGizmoState is not null)
        {
            _sceneManager.TranslationGizmo.HandleSize = state.TranslationGizmoState.HandleSize;
            _sceneManager.TranslationGizmo.Space = state.TranslationGizmoState.Space;
            _sceneManager.TranslationGizmo.ConstantScreenSize = state.TranslationGizmoState.ConstantScreenSize;
        }

        if (state.ScaleGizmoState is not null)
        {
            _sceneManager.ScaleGizmo.HandleSize = state.ScaleGizmoState.HandleSize;
            _sceneManager.ScaleGizmo.Space = state.ScaleGizmoState.Space;
            _sceneManager.ScaleGizmo.ConstantScreenSize = state.ScaleGizmoState.ConstantScreenSize;
        }

        if (state.RotationGizmoState is not null)
        {
            _sceneManager.RotationGizmo.ConstantScreenSize = state.RotationGizmoState.ConstantScreenSize;
            _sceneManager.RotationGizmo.Space = state.RotationGizmoState.Space;
            _sceneManager.RotationGizmo.ConstantScreenSize = state.RotationGizmoState.ConstantScreenSize;
            _sceneManager.RotationGizmo.Sensitivity = state.RotationGizmoState.Sensitivity;
        }
    }
}