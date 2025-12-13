using ImGuiNET;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.GameObjects.Scenes;
using Visualisation.Core.Inputs;

namespace Visualization.UiLayer.UI.Windows;

public sealed class CascadingShadowMapsWindow(ImGuiController imGuiController, IInputProvider inputProvider, SceneManager scene, Vector2i size) : IDisposable
{
    private readonly ImGuiController _imGuiController = imGuiController; /* borrowed */
    private readonly IInputProvider _inputProvider = inputProvider; /* borrowed */
    private readonly SceneManager _sceneManager = scene; /* borrowed */

    private bool _disposed;
    private readonly WindowFrameBuffer _depthMapWindowFrb = new(size.X, size.Y);
    private readonly Shader _quadCsmShader = new("depthMapShader.vert", "depthMapShader.frag"); /* owned */
    private readonly QuadMesh _quadMesh = new();
    
    private int _directionalLightLayer;
    public int DirectionalLightLayer
    {
        get => _directionalLightLayer;
        set
        {
            if (_sceneManager.LightsManager.DirectionalLight is null)
                return;

            _directionalLightLayer = value % (_sceneManager.LightsManager.DirectionalLight.ShadowCascadeLevels.Length + 1);
            if (_directionalLightLayer < 0)
            {
                _directionalLightLayer += (_sceneManager.LightsManager.DirectionalLight.ShadowCascadeLevels.Length + 1);
            }
        }
    }
    
    public void Draw()
    {
        ImGui.Begin("Cascading Shadow Maps");

        System.Numerics.Vector2 viewportSize = ImGui.GetContentRegionAvail();
        var fbScale = _imGuiController.ScaleFactor;
        int fbWidth = Math.Max(1, (int)Math.Round(viewportSize.X * fbScale.X));
        int fbHeight = Math.Max(1, (int)Math.Round(viewportSize.Y * fbScale.Y));
        _depthMapWindowFrb.Resize(fbWidth, fbHeight);

        _depthMapWindowFrb.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _quadCsmShader.Use();
        _sceneManager.LightsManager.DirectionalLight?.SetForDepthTextureShader(_quadCsmShader, DirectionalLightLayer);
        _quadMesh.Render();

        _depthMapWindowFrb.Unbind();

        ImGui.Image(_depthMapWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
            new System.Numerics.Vector2(1, 0)); // Flipped Y for OpenGL texture
        ImGui.End();
    }

    public void HandleInput()
    {
        if (_inputProvider.IsKeyPressed(InputKey.Equal))
        {
            DirectionalLightLayer++;
        }

        if (_inputProvider.IsKeyPressed(InputKey.Minus))
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

    public record State(int DirectionalLightLayer); 
    public State SaveState()
    {
        return new State(DirectionalLightLayer);
    }

    public void RestoreState(State state)
    {
        DirectionalLightLayer = state.DirectionalLightLayer;
    }
}