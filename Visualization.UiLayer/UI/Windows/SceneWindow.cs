using ImGuiNET;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects.Scenes;
using Visualisation.Core.Inputs;

namespace Visualization.UiLayer.UI.Windows;

public sealed class SceneWindow(
    ImGuiController imGuiController,
    SceneManager sceneManager,
    IInputProvider inputProvider,
    Vector2i size
) : IDisposable
{
    private ImGuiController _imGuiController = imGuiController; /* borrowed */
    private SceneManager _scene = sceneManager; /* borrowed */
    private IInputProvider _inputProvider = inputProvider; /* borrowed */

    private WindowFrameBuffer _sceneRenderWindowFrb = new(size.X, size.Y);
    private Shader _debugBasicShader = new("sceneBasicShader.vert", "sceneBasicShader.frag");

    public delegate void DebugDraw(Shader sh);

    public DebugDraw? DebugRenderInScene { get; set; }

    public void Draw(Vector2i framebufferSize, float dt)
    {
        ImGui.Begin("Game Viewport");

        bool isWindowHovered = ImGui.IsWindowHovered();
        if (isWindowHovered)
        {
            _scene.ProcessInput(_inputProvider, dt);
        }
        else
        {
            _scene.ProcessInputOutOfFocus(_inputProvider, dt);
        }

        System.Numerics.Vector2 viewportSize = ImGui.GetContentRegionAvail();
        var fbScale = _imGuiController.ScaleFactor;
        int fbW = Math.Max(1, (int)Math.Round(viewportSize.X * fbScale.X));
        int fbH = Math.Max(1, (int)Math.Round(viewportSize.Y * fbScale.Y));
        _sceneRenderWindowFrb.Resize(fbW, fbH);

        CameraBase.AspectRatio = viewportSize.X / viewportSize.Y;

        _scene.RenderSceneWindow(fbW, fbH, _sceneRenderWindowFrb);

        DrawDebug();

        _sceneRenderWindowFrb.Unbind();
        GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);
        ImGui.Image(_sceneRenderWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
            new System.Numerics.Vector2(1, 0));
        ImGui.End();
    }

    private void DrawDebug()
    {
        if (DebugRenderInScene is null)
            return;

        _debugBasicShader.Use();
        _debugBasicShader.SetMatrix4("view", _scene.CamerasManager.CurrentCamera.ViewMatrix);
        _debugBasicShader.SetMatrix4("projection", _scene.CamerasManager.CurrentCamera.ProjectionMatrix);
        DebugRenderInScene(_debugBasicShader);
    }

    public void Dispose()
    {
        _sceneRenderWindowFrb.Dispose();
        _debugBasicShader.Dispose();
    }
}