using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Light;

namespace Visualisation.Core.Display;

public sealed class RenderContext
{
    public CameraBase? Camera { get; set; }
    public Shader? DefaultShader { get; set; }
    public Shader? OutlineShader { get; set; }
    public Shader? PbrShader { get; set; }
    public EnvironmentMap? EnvironmentMap { get; set; }
    public LightsManager? LightsManager { get; set; }
    public bool IsSelectionPass { get; set; }
    public bool DrawInvisibleObjects { get; set; }
    public bool SkipMaterial { get; set; }
    public float OutlineSize { get; set; } = 0.05f;
    public Vector4 OutlineColor { get; set; } = new(0.0f, 1.0f, 0.0f, 1.0f); // Green by default
    public float PositionEpsilon { get; set; } = 0.0f;
}