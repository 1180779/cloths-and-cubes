using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Light;
using Visualisation.Core.GameObjects.Scenes;

using Visualization.UiLayer.UI.Controls;

namespace Visualization.UiLayer.UI.Windows;

public sealed class GraphicsSettingsWindow(
    Func<LightDirectional?> getDirectionalLight,
    SceneRenderer sceneRenderer,
    SceneWindow sceneWindow
) : IWindow, IDisposable
{
    private SceneRenderer _sceneRenderer = sceneRenderer; /* borrowed */
    private SceneWindow _sceneWindow = sceneWindow; /* borrowed */
    private Func<LightDirectional?> _getDirectionalLight = getDirectionalLight;

    private DirectionArrowControl? _arrowControl;
    private bool _disposed;

    public const string WindowName = "Graphics Settings";
    public string Name => WindowName;

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            var light = this._getDirectionalLight();
            if (light is not null)
            {
                var direction = light.Direction.ToNumerics();
                DrawLightDirectionControls(ref direction, light);
                DrawCascadesControls(light);
                DrawShadowBiasControls(light);
            }

            DrawEnvironmentMapControls();
            RenderAntialiasingControls();
        }

        ImGui.End();
    }

    private void DrawCascadesControls(LightDirectional light)
    {
        if (ImGui.CollapsingHeader("Cascaded Shadow Maps"))
        {
            ImGui.Indent();
            int cascadeCount = light.CascadeCount;
            if (ImGui.SliderInt("Cascade Count", ref cascadeCount, LightDirectional.MinCascades,
                LightDirectional.MaxCascades))
            {
                light.CascadeCount = cascadeCount;
            }

            ImGui.TextWrapped("Lambda controls the distribution of cascade splits. It is a weight beteween 0 and 1 " +
                "that determines how the splits are calculated. The formula used is a combination of uniform and logarithmic splits. " +
                "0 means uniform splits, while 1 means logarithmic splits. The values in between provide a balance between the two methods. ");
            ImGui.SliderFloat("Lambda", ref light.CascadeSplitLambda, 0.0f, 1.0f);

            bool debugCascades = light.DebugCascades;
            if (ImGui.Checkbox("Debug Cascades", ref debugCascades))
            {
                light.DebugCascades = debugCascades;
            }

            const string resetButtonText = "Reset to default";
            if (ImGui.Button(resetButtonText, UiControls.Style.ButtonSizes.Medium(resetButtonText)))
            {
                light.CascadeCount = LightDirectional.DefaultCascades;
                light.CascadeSplitLambda = LightDirectional.DefaultCascadeSplitLambda;
                light.DebugCascades = false;
            }

            ImGui.Unindent();
        }
    }

    private void DrawShadowBiasControls(LightDirectional light)
    {
        if (ImGui.CollapsingHeader("Shadow Bias Parameters"))
        {
            ImGui.Indent();
            UiControls.DragFloatPropertyPositive(() => light.ShadowBiasMin, v => light.ShadowBiasMin = v, "min bias",
                0.001f);
            UiControls.DragFloatPropertyPositive(() => light.ShadowBiasMax, v => light.ShadowBiasMax = v, "max bias",
                0.001f);
            UiControls.DragFloatPropertyPositive(() => light.ShadowBiasModifier, v => light.ShadowBiasModifier = v,
                "bias modifier",
                0.001f);
            UiControls.DragFloatPropertyPositive(() => light.ZMult, v => light.ZMult = v, "z mult",
                0.001f);
            const string resetButtonText = "Reset to defaults";
            if (ImGui.Button(resetButtonText, UiControls.Style.ButtonSizes.Medium(resetButtonText)))
            {
                light.ResetShadowBiasToDefault();
            }

            ImGui.Unindent();
        }
    }


    private void DrawLightDirectionControls(ref System.Numerics.Vector3 direction, LightDirectional light)
    {
        if (ImGui.CollapsingHeader("Light direction control"))
        {
            ImGui.Indent();
            _arrowControl ??= new DirectionArrowControl(_sceneRenderer.BasicShader);
            _arrowControl.Draw(ref direction, light);
            UiControls.SetTooltip("Use the arrows to visually adjust the light direction. "
                + "The arrows represent the X (red), Y (green), and Z (blue) axes.");

            ImGui.Spacing();
            if (ImGui.DragFloat3("Direction", ref direction, 0.01f))
            {
                light.Direction = direction.ToOpenTK();
            }

            ImGui.Unindent();
        }
    }

    private void DrawEnvironmentMapControls()
    {
        if (ImGui.CollapsingHeader("Environment Map Settings"))
        {
            ImGui.Indent();
            int currentDisplayType = (int)_sceneRenderer.EnvironmentMap.DisplayType;
            ImGui.Text("Display Type:");
            ImGui.RadioButton("Skybox", ref currentDisplayType,
                (int)EnvironmentMap.EnvironmentMapDisplayType.EnvironmentCubemap);
            ImGui.SameLine();
            ImGui.RadioButton("Irradiance Map", ref currentDisplayType,
                (int)EnvironmentMap.EnvironmentMapDisplayType.IrradianceMap);
            ImGui.SameLine();
            ImGui.RadioButton("Prefiltered Map", ref currentDisplayType,
                (int)EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap);
            _sceneRenderer.EnvironmentMap.DisplayType = (EnvironmentMap.EnvironmentMapDisplayType)currentDisplayType;

            if (_sceneRenderer.EnvironmentMap.DisplayType != EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap)
            {
                ImGui.BeginDisabled();
            }

            float roughness = _sceneRenderer.EnvironmentMap.PrefilterMapValue;
            if (ImGui.SliderFloat("Roughness", ref roughness, 1.0f, 5.0f))
            {
                _sceneRenderer.EnvironmentMap.PrefilterMapValue = roughness;
            }

            if (_sceneRenderer.EnvironmentMap.DisplayType != EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap)
            {
                ImGui.EndDisabled();
            }

            ImGui.Unindent();
        }
    }

    private void RenderAntialiasingControls()
    {
        if (ImGui.CollapsingHeader("Antialiasing"))
        {
            ImGui.Indent();
            int antialiasing = (int)_sceneWindow.Antialiasing;
            ImGui.RadioButton("None", ref antialiasing, (int)SceneWindow.AntialiasingType.None);
            ImGui.RadioButton("MSAA2", ref antialiasing, (int)SceneWindow.AntialiasingType.MSAA2);
            ImGui.RadioButton("MSAA4", ref antialiasing, (int)SceneWindow.AntialiasingType.MSAA4);
            ImGui.RadioButton("MSAA8", ref antialiasing, (int)SceneWindow.AntialiasingType.MSAA8);
            _sceneWindow.Antialiasing = (SceneWindow.AntialiasingType)antialiasing;
            ImGui.Unindent();
        }
    }

    public sealed record EnvironmentMapState
    {
        public EnvironmentMap.EnvironmentMapDisplayType EnvironmentMapDisplayType { get; init; } =
            EnvironmentMap.EnvironmentMapDisplayType.EnvironmentCubemap;

        public float PrefilterMapValue { get; init; } = 1.0f;
    }

    public sealed record SceneWindowState
    {
        public SceneWindow.AntialiasingType Antialiasing { get; init; } = SceneWindow.AntialiasingType.None;
    }

    public sealed record ShadowState
    {
        public float ShadowBiasMin { get; init; }
        public float ShadowBiasMax { get; init; }
        public float ShadowBiasModifier { get; init; }
        public float ZMult { get; init; }
        public System.Numerics.Vector3 Direction { get; init; }
        public bool DebugCascades { get; init; }
    }

    public sealed record State
    {
        public ShadowState? Shadows { get; init; }
        public EnvironmentMapState? EnvironmentMap { get; init; }
        public SceneWindowState? SceneWindow { get; init; }
    }

    public State SaveState()
    {
        var light = this._getDirectionalLight();
        return new State
        {
            Shadows = light is not null
                ? new ShadowState
                {
                    ShadowBiasMin = light.ShadowBiasMin,
                    ShadowBiasMax = light.ShadowBiasMin,
                    ShadowBiasModifier = light.ShadowBiasModifier,
                    ZMult = light.ZMult,
                    Direction = light.Direction.ToNumerics(),
                    DebugCascades = light.DebugCascades
                }
                : null,
            EnvironmentMap = new EnvironmentMapState
            {
                EnvironmentMapDisplayType = _sceneRenderer.EnvironmentMap.DisplayType,
                PrefilterMapValue = _sceneRenderer.EnvironmentMap.PrefilterMapValue,
            },
            SceneWindow = new SceneWindowState { Antialiasing = _sceneWindow.Antialiasing }
        };
    }

    public void RestoreState(State state)
    {
        var light = this._getDirectionalLight();
        if (light is not null && state.Shadows is not null)
        {
            light.ShadowBiasMin = state.Shadows.ShadowBiasMin;
            light.ShadowBiasMax = state.Shadows.ShadowBiasMax;
            light.ShadowBiasModifier = state.Shadows.ShadowBiasModifier;
            light.ZMult = state.Shadows.ZMult;
            light.Direction = state.Shadows.Direction.ToOpenTK();
            light.DebugCascades = state.Shadows.DebugCascades;
        }

        if (state.EnvironmentMap is not null)
        {
            _sceneRenderer.EnvironmentMap.DisplayType = state.EnvironmentMap.EnvironmentMapDisplayType;
            _sceneRenderer.EnvironmentMap.PrefilterMapValue = state.EnvironmentMap.PrefilterMapValue;
        }

        if (state.SceneWindow is not null)
        {
            _sceneWindow.Antialiasing = state.SceneWindow.Antialiasing;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _arrowControl?.Dispose();
    }
}