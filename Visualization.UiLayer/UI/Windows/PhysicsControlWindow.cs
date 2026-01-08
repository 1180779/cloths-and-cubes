using ImGuiNET;

using Visualization.UiLayer.Applications;
using Visualization.UiLayer.Applications.Demos;

namespace Visualization.UiLayer.UI.Windows;

public sealed class PhysicsControlWindow : IWindow
{
    private readonly Application _application;
    private readonly BoxesDemo? _boxesDemo;
    private int _stepCount = 1;

    public PhysicsControlWindow(Application application)
    {
        _application = application;
        _boxesDemo = application as BoxesDemo;
    }

    public string Name => "Physics Control";

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            ImGui.SeparatorText("Simulation Control");

            // Play/Pause Section
            bool isPaused = _application.StepsLimit;

            if (isPaused)
            {
                if (ImGui.Button("Resume", new System.Numerics.Vector2(120, 30)))
                {
                    _application.StepsLimit = false;
                }

                ImGui.SameLine();
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 1.0f), "PAUSED");
            }
            else
            {
                if (ImGui.Button("Pause", new System.Numerics.Vector2(120, 30)))
                {
                    _application.StepsLimit = true;
                    _application.AvailableSteps = 0;
                }

                ImGui.SameLine();
                ImGui.TextColored(new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f), "RUNNING");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Step Control Section
            ImGui.SeparatorText("Step Control");

            if (!isPaused)
            {
                ImGui.BeginDisabled();
            }

            ImGui.SliderInt("Step Count", ref _stepCount, 1, 100);

            if (ImGui.Button("Step Forward", new System.Numerics.Vector2(120, 0)))
            {
                _application.StepsLimit = true;
                _application.AvailableSteps = _stepCount;
            }

            ImGui.SameLine();
            ImGui.TextDisabled("(Advances N frames)");

            // Quick step buttons
            ImGui.Spacing();
            ImGui.Text("Quick Steps:");

            if (ImGui.Button("1##step"))
            {
                _application.StepsLimit = true;
                _application.AvailableSteps = 1;
            }

            ImGui.SameLine();
            if (ImGui.Button("5##step"))
            {
                _application.StepsLimit = true;
                _application.AvailableSteps = 5;
            }

            ImGui.SameLine();
            if (ImGui.Button("10##step"))
            {
                _application.StepsLimit = true;
                _application.AvailableSteps = 10;
            }

            ImGui.SameLine();
            if (ImGui.Button("50##step"))
            {
                _application.StepsLimit = true;
                _application.AvailableSteps = 50;
            }

            if (!isPaused)
            {
                ImGui.EndDisabled();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Reset Section
            ImGui.SeparatorText("Scene Actions");

            if (_boxesDemo != null)
            {
                if (ImGui.Button("Reset Scene", new System.Numerics.Vector2(120, 0)))
                {
                    _boxesDemo.Reset();
                }

                UiControls.SetTooltip("Reset all objects to initial positions (Keyboard: R)");
            }
            else
            {
                ImGui.BeginDisabled();
                ImGui.Button("Reset Scene", new System.Numerics.Vector2(120, 0));
                ImGui.EndDisabled();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Status Display
            ImGui.SeparatorText("Status");

            ImGui.Text(isPaused ? $"Available Steps: {_application.AvailableSteps}" : "Simulation Running");

            // FPS Cap Status
            bool fpsCapped = _application.UpdateFrequency > 0;
            ImGui.Text($"FPS Cap: {(fpsCapped ? $"{_application.UpdateFrequency:F0}" : "Unlimited")}");

            ImGui.Spacing();
            ImGui.TextWrapped("Keyboard Shortcuts:");
            ImGui.BulletText("[ - Enable step mode");
            ImGui.BulletText("] - Disable step mode");
            ImGui.BulletText("0-9 - Set step count (in step mode)");
            ImGui.BulletText("R - Reset scene");
            ImGui.BulletText("X - Toggle FPS cap (60/Unlimited)");
        }

        ImGui.End();
    }

    public sealed record State
    {
        public bool IsPaused { get; init; }
        public int StepCount { get; init; }
    }

    public State SaveState()
    {
        return new State { IsPaused = _application.StepsLimit, StepCount = _stepCount };
    }

    public void RestoreState(State state)
    {
        _stepCount = state.StepCount;
        _application.StepsLimit = state.IsPaused;
        if (state.IsPaused)
        {
            _application.AvailableSteps = 0;
        }
    }
}