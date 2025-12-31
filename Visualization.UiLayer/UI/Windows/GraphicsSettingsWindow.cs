using ImGuiNET;

using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Light;
using Visualisation.Core.GameObjects.Scenes;

namespace Visualization.UiLayer.UI.Windows;

public sealed class GraphicsSettingsWindow(
    Func<LightDirectional?> getDirectionalLight,
    SceneManager sceneManager,
    SceneWindow sceneWindow
) : IWindow
{
    private SceneManager _sceneManager = sceneManager; /* borrowed */
    private SceneWindow _sceneWindow = sceneWindow; /* borrowed */
    private Func<LightDirectional?> _getDirectionalLight = getDirectionalLight;

    private float _shadowBiasMin;
    private float _shadowBiasMax;
    private float _shadowBiasModifier;
    private float _zMult;

    public string Name => "Graphics Settings";

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            DrawShadowMapSettings();
            ImGui.Separator();
            ImGui.Spacing();
            DrawEnvironmentMap();
            ImGui.Separator();
            ImGui.Spacing();
            DrawSceneWindow();
        }

        ImGui.End();
    }

    private void DrawShadowMapSettings()
    {
        ImGui.SeparatorText("Shadow Map");

        var light = this._getDirectionalLight();
        if (light is not null)
        {
            _shadowBiasMin = light.ShadowBiasMin;
            _shadowBiasMax = light.ShadowBiasMax;
            _shadowBiasModifier = light.ShadowBiasModifier;
            _zMult = light.ZMult;

            // ========================================
            // min bias
            if (ImGui.SmallButton("-##minBias"))
            {
                _shadowBiasMin -= 0.01f;
                light.ShadowBiasMin = _shadowBiasMin;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##minBias"))
            {
                _shadowBiasMin += 0.01f;
                light.ShadowBiasMin = _shadowBiasMin;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("min bias", ref _shadowBiasMin))
            {
                light.ShadowBiasMin = _shadowBiasMin;
            }

            // ========================================
            // max bias
            if (ImGui.SmallButton("-##maxBias"))
            {
                _shadowBiasMax -= 0.01f;
                light.ShadowBiasMax = _shadowBiasMax;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##maxBias"))
            {
                _shadowBiasMax += 0.01f;
                light.ShadowBiasMax = _shadowBiasMax;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("max bias", ref _shadowBiasMax))
            {
                light.ShadowBiasMax = _shadowBiasMax;
            }

            // ========================================
            // bias modifier
            if (ImGui.SmallButton("-##biasModifier"))
            {
                _shadowBiasModifier -= 0.01f;
                light.ShadowBiasModifier = _shadowBiasModifier;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##biasModifier"))
            {
                _shadowBiasModifier += 0.01f;
                light.ShadowBiasModifier = _shadowBiasModifier;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("bias modifier", ref _shadowBiasModifier))
            {
                light.ShadowBiasModifier = _shadowBiasModifier;
            }

            // ========================================
            // zMult
            if (ImGui.SmallButton("-##zMult"))
            {
                _zMult -= 0.5f;
                light.ZMult = _zMult;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##zMult"))
            {
                _zMult += 0.5f;
                light.ZMult = _zMult;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("z Mult", ref _zMult))
            {
                light.ZMult = _zMult;
            }

            ImGui.Spacing();
        }
    }

    private void DrawEnvironmentMap()
    {
        ImGui.SeparatorText("Environment Map Settings");
        int currentDisplayType = (int)_sceneManager.EnvironmentMap.DisplayType;
        ImGui.Text("Display Type:");
        ImGui.RadioButton("Skybox", ref currentDisplayType,
            (int)EnvironmentMap.EnvironmentMapDisplayType.EnvironmentCubemap);
        ImGui.SameLine();
        ImGui.RadioButton("Irradiance Map", ref currentDisplayType,
            (int)EnvironmentMap.EnvironmentMapDisplayType.IrradianceMap);
        ImGui.SameLine();
        ImGui.RadioButton("Prefiltered Map", ref currentDisplayType,
            (int)EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap);
        _sceneManager.EnvironmentMap.DisplayType = (EnvironmentMap.EnvironmentMapDisplayType)currentDisplayType;

        if (_sceneManager.EnvironmentMap.DisplayType != EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap)
        {
            ImGui.BeginDisabled();
        }

        float roughness = _sceneManager.EnvironmentMap.PrefilterMapValue;
        if (ImGui.SliderFloat("Roughness", ref roughness, 1.0f, 5.0f))
        {
            _sceneManager.EnvironmentMap.PrefilterMapValue = roughness;
        }

        if (_sceneManager.EnvironmentMap.DisplayType != EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawSceneWindow()
    {
        ImGui.SeparatorText("Antialiasing");
        int antialiasing = (int)_sceneWindow.Antialiasing;
        ImGui.RadioButton("None", ref antialiasing, (int)SceneWindow.AntialiasingType.None);
        ImGui.RadioButton("MSAA2", ref antialiasing, (int)SceneWindow.AntialiasingType.MSAA2);
        ImGui.RadioButton("MSAA4", ref antialiasing, (int)SceneWindow.AntialiasingType.MSAA4);
        ImGui.RadioButton("MSAA8", ref antialiasing, (int)SceneWindow.AntialiasingType.MSAA8);
        _sceneWindow.Antialiasing = (SceneWindow.AntialiasingType)antialiasing;
    }

    public sealed record EnvironmentMapState
    {
        public EnvironmentMap.EnvironmentMapDisplayType EnvironmentMapDisplayType =
            EnvironmentMap.EnvironmentMapDisplayType.EnvironmentCubemap;

        public float PrefilterMapValue = 1.0f;
    }

    public sealed record SceneWindowState
    {
        public SceneWindow.AntialiasingType Antialiasing = SceneWindow.AntialiasingType.None;
    }

    public sealed record ShadowState
    {
        public float ShadowBiasMin { get; init; }
        public float ShadowBiasMax { get; init; }
        public float ShadowBiasModifier { get; init; }
        public float ZMult { get; init; }
    }

    public sealed record State
    {
        public ShadowState? Shadows { get; init; }
        public EnvironmentMapState? EnvironmentMap { get; init; }
        public SceneWindowState? SceneWindow { get; init; }
    }

    public State SaveState()
    {
        return new State
        {
            Shadows = new ShadowState
            {
                ShadowBiasMin = _shadowBiasMin,
                ShadowBiasMax = _shadowBiasMax,
                ShadowBiasModifier = _shadowBiasModifier,
                ZMult = _zMult,
            },
            EnvironmentMap = new EnvironmentMapState
            {
                EnvironmentMapDisplayType = _sceneManager.EnvironmentMap.DisplayType,
                PrefilterMapValue = _sceneManager.EnvironmentMap.PrefilterMapValue,
            },
            SceneWindow = new SceneWindowState { Antialiasing = _sceneWindow.Antialiasing }
        };
    }

    public void RestoreState(State state)
    {
        if (state.Shadows is not null)
        {
            _shadowBiasMin = state.Shadows.ShadowBiasMin;
            _shadowBiasMax = state.Shadows.ShadowBiasMax;
            _shadowBiasModifier = state.Shadows.ShadowBiasModifier;
            _zMult = state.Shadows.ZMult;

            var light = this._getDirectionalLight();
            if (light is not null)
            {
                light.ShadowBiasMin = _shadowBiasMin;
                light.ShadowBiasMax = _shadowBiasMax;
                light.ShadowBiasModifier = _shadowBiasModifier;
                light.ZMult = _zMult;
            }
        }

        if (state.EnvironmentMap is not null)
        {
            _sceneManager.EnvironmentMap.DisplayType = state.EnvironmentMap.EnvironmentMapDisplayType;
            _sceneManager.EnvironmentMap.PrefilterMapValue = state.EnvironmentMap.PrefilterMapValue;
        }

        if (state.SceneWindow is not null)
        {
            _sceneWindow.Antialiasing = state.SceneWindow.Antialiasing;
        }
    }
}