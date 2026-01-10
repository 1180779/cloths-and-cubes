using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Light;

namespace Visualisation.Core.Display;

public sealed class RenderContext
{
    public required CameraBase Camera { get; set; }
    public required Shader DefaultShader { get; set; }
    public required Shader OutlineShader { get; set; }
    public required Shader PbrShader { get; set; }
    public required EnvironmentMap EnvironmentMap { get; set; }
    public required LightsManager LightsManager { get; set; }
    public float OutlineSize { get; set; } = 0.05f;
    public Vector4 OutlineColor { get; set; } = new(0.0f, 1.0f, 0.0f, 1.0f); // Green by default
    public float PositionEpsilon { get; set; } = 0.0f;
}