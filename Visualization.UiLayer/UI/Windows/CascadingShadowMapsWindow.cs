using ImGuiNET;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Display.Mesh;

namespace Visualization.UiLayer.UI.Windows;

public sealed class CascadingShadowMapsWindow(
    ImGuiController imGuiController,
    LightsManager lightsManager,
    Vector2i size
) : IDisposable, IWindow
{
    private bool _disposed;

    private readonly ImGuiController _imGuiController = imGuiController; /* borrowed */
    private readonly LightsManager _lightsManager = lightsManager; /* borrowed */

    private readonly WindowFrameBuffer _depthMapWindowFrb = new(size.X, size.Y); /* owned */
    private readonly Shader _quadCsmShader = new("depthMapShader.vert", "depthMapShader.frag"); /* owned */
    private readonly QuadMesh _quadMesh = new(); /* owned */

    private int _directionalLightLayer;

    public int DirectionalLightLayer
    {
        get => _directionalLightLayer;
        set
        {
            if (_lightsManager.DirectionalLight is null)
                return;

            _directionalLightLayer =
                value % (_lightsManager.DirectionalLight.ShadowCascadeLevels.Length + 1);
            if (_directionalLightLayer < 0)
            {
                _directionalLightLayer += (_lightsManager.DirectionalLight.ShadowCascadeLevels.Length + 1);
            }
        }
    }

    public string Name => "Cascading Shadow Maps";

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;

        if (ImGui.Begin(Name, ref isOpen))
        {
            System.Numerics.Vector2 viewportSize = ImGui.GetContentRegionAvail();
            var fbScale = _imGuiController.ScaleFactor;
            int fbWidth = Math.Max(1, (int)Math.Round(viewportSize.X * fbScale.X));
            int fbHeight = Math.Max(1, (int)Math.Round(viewportSize.Y * fbScale.Y));
            _depthMapWindowFrb.Resize(fbWidth, fbHeight);

            _depthMapWindowFrb.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _quadCsmShader.Use();
            _lightsManager.DirectionalLight?.SetForDepthTextureShader(_quadCsmShader, DirectionalLightLayer);
            _quadMesh.Render();

            _depthMapWindowFrb.Unbind();

            ImGui.Image(_depthMapWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0)); // Flipped Y for OpenGL texture
        }

        ImGui.End();
    }

    public void HandleInput()
    {
        if (ImGui.IsKeyPressed(ImGuiKey.Equal))
        {
            DirectionalLightLayer++;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Minus))
        {
            DirectionalLightLayer--;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _depthMapWindowFrb.Dispose();
        _quadCsmShader.Dispose();
        _quadMesh.Dispose();
    }

    public sealed record State
    {
        public int DirectionalLightLayer { get; init; }
    }

    public State SaveState()
    {
        return new State { DirectionalLightLayer = DirectionalLightLayer };
    }

    public void RestoreState(State state)
    {
        DirectionalLightLayer = state.DirectionalLightLayer;
    }
}