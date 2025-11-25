using ImGuiNET;

using Visualisation.Core.Display.Light;

namespace Visualization.UiLayer.UI.Windows;

public sealed class ShadowSettingsWindow
{
    public ShadowSettingsWindow(Func<LightDirectional?> getDirectionalLight)
    {
        this.getDirectionalLight = getDirectionalLight;
    }

    private Func<LightDirectional?> getDirectionalLight;

    private float shadowBiasMin;
    private float shadowBiasMax;
    private float shadowBiasModifier;
    private float zMult;

    public void Render()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250f, 160f), ImGuiCond.Always);
        ImGui.Begin("Shadow Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

        var light = this.getDirectionalLight();
        if (light is not null)
        {
            shadowBiasMin = light.ShadowBiasMin;
            shadowBiasMax = light.ShadowBiasMax;
            shadowBiasModifier = light.ShadowBiasModifier;
            zMult = light.ZMult;

            // ========================================
            // min bias
            if (ImGui.SmallButton("-##minBias"))
            {
                shadowBiasMin -= 0.01f;
                light.ShadowBiasMin = shadowBiasMin;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##minBias"))
            {
                shadowBiasMin += 0.01f;
                light.ShadowBiasMin = shadowBiasMin;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("min bias", ref shadowBiasMin))
            {
                light.ShadowBiasMin = shadowBiasMin;
            }

            // ========================================
            // max bias
            if (ImGui.SmallButton("-##maxBias"))
            {
                shadowBiasMax -= 0.01f;
                light.ShadowBiasMax = shadowBiasMax;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##maxBias"))
            {
                shadowBiasMax += 0.01f;
                light.ShadowBiasMax = shadowBiasMax;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("max bias", ref shadowBiasMax))
            {
                light.ShadowBiasMax = shadowBiasMax;
            }

            // ========================================
            // bias modifier
            if (ImGui.SmallButton("-##biasModifier"))
            {
                shadowBiasModifier -= 0.01f;
                light.ShadowBiasModifier = shadowBiasModifier;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##biasModifier"))
            {
                shadowBiasModifier += 0.01f;
                light.ShadowBiasModifier = shadowBiasModifier;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("bias modifier", ref shadowBiasModifier))
            {
                light.ShadowBiasModifier = shadowBiasModifier;
            }

            // ========================================
            // zMult
            if (ImGui.SmallButton("-##zMult"))
            {
                zMult -= 0.5f;
                light.ZMult = zMult;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("+##zMult"))
            {
                zMult += 0.5f;
                light.ZMult = zMult;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60f);
            if (ImGui.InputFloat("z Mult", ref zMult))
            {
                light.ZMult = zMult;
            }

            ImGui.Spacing();
            ImGui.Separator();
        }

        ImGui.End();
    }
}