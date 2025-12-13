using ImGuiNET;

using Visualisation.Core.Display.Light;

namespace Visualization.UiLayer.UI.Windows;

public sealed class ShadowSettingsWindow
{
    public ShadowSettingsWindow(Func<LightDirectional?> getDirectionalLight)
    {
        this._getDirectionalLight = getDirectionalLight;
    }

    private Func<LightDirectional?> _getDirectionalLight;

    private float _shadowBiasMin;
    private float _shadowBiasMax;
    private float _shadowBiasModifier;
    private float _zMult;

    public void Render()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250f, 160f), ImGuiCond.Always);
        ImGui.Begin("Shadow Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

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
            ImGui.Separator();
        }

        ImGui.End();
    }

    public record State(float ShadowBiasMin, float ShadowBiasMax, float ShadowBiasModifier, float ZMult);

    public State SaveState()
    {
        return new State(_shadowBiasMin, _shadowBiasMax, _shadowBiasModifier, _zMult);
    }
    
    public void RestoreState(State state)
    {
        _shadowBiasMin = state.ShadowBiasMin;
        _shadowBiasMax = state.ShadowBiasMax;
        _shadowBiasModifier = state.ShadowBiasModifier;
        _zMult = state.ZMult;
        
        var light = this._getDirectionalLight();
        if (light is not null)
        {
            light.ShadowBiasMin = _shadowBiasMin;
            light.ShadowBiasMax = _shadowBiasMax;
            light.ShadowBiasModifier = _shadowBiasModifier;
            light.ZMult = _zMult;
        }
    }
}