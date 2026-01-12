using System.Diagnostics;

using ImGuiNET;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects.Scenes;
using Visualisation.Core.Inputs;

using Visualization.UiLayer.Inputs;

namespace Visualization.UiLayer.UI.Windows;

public sealed class SceneWindow(
    ImGuiController imGuiController,
    SceneRenderer sceneRendererRenderer,
    IInputProvider inputProvider,
    Vector2i size
) : IDisposable
{
    private readonly ImGuiController _imGuiController = imGuiController; /* borrowed */
    private readonly SceneRenderer _sceneRenderer = sceneRendererRenderer; /* borrowed */
    private readonly IInputProvider _inputProvider = inputProvider; /* borrowed */

    private bool _disposed;
    private WindowFrameBuffer _sceneRenderWindowFrb = new(size.X, size.Y);
    private MsaaFrameBuffer? _msaaFrameBuffer;
    private readonly Shader _debugBasicShader = new("sceneBasicShader.vert", "sceneBasicShader.frag");

    public delegate void DebugDraw(Shader sh);

    public DebugDraw? DebugRenderInScene { get; set; }

    public int Width => _sceneRenderWindowFrb.Width;
    public int Height => _sceneRenderWindowFrb.Height;
    private bool _isHovered;
    public bool IsHovered => _isHovered;
    public System.Numerics.Vector2 ImageTopLeft { get; private set; }
    public System.Numerics.Vector2 ImageSize { get; private set; }

    public void Draw(Vector2i framebufferSize, float dt)
    {
        ImGui.Begin("Game Viewport");

        _isHovered = ImGui.IsWindowHovered();
        // Keep input processing active during drag even if hover state is lost
        bool isDragging = _sceneRenderer.InteractionManager.StaticDragManager.IsDragging;
        bool isGizmoActive = _sceneRenderer.InteractionManager.ActiveGizmo?.IsActive ?? false;
        bool viewportIsActive = _isHovered || _inputProvider.GetCursorState() == CursorState.Grabbed || isDragging ||
            isGizmoActive;

        // When viewport is active, bypass ImGui keyboard capture so all keyboard input works
        // Note: This is also set in OnUpdateFrame for timing, but we keep it here for consistency
        if (_inputProvider is OpenTKWithImGuiInputProvider imguiProvider)
        {
            imguiProvider.BypassImGuiKeyboardCapture = viewportIsActive;
        }

        if (viewportIsActive)
        {
            _sceneRenderer.ProcessInputInFocus(_inputProvider, dt);
        }
        else
        {
            _sceneRenderer.ProcessInputOutOfFocus(_inputProvider, dt);
        }

        _sceneRenderer.ProcessInputInAndOutOfFocus(_inputProvider, dt);

        System.Numerics.Vector2 viewportSize = ImGui.GetContentRegionAvail();
        var fbScale = _imGuiController.ScaleFactor;
        int fbW = Math.Max(1, (int)Math.Round(viewportSize.X * fbScale.X));
        int fbH = Math.Max(1, (int)Math.Round(viewportSize.Y * fbScale.Y));
        CameraBase.AspectRatio = viewportSize.X / viewportSize.Y;

        _sceneRenderWindowFrb.Resize(fbW, fbH);
        if (_antialiasingType != AntialiasingType.None)
        {
            Debug.Assert(_msaaFrameBuffer is not null);

            _msaaFrameBuffer.Resize(fbW, fbH);
            _msaaFrameBuffer.Bind();
            _sceneRenderer.RenderSceneWindow(_msaaFrameBuffer);
            DrawDebug();
            _sceneRenderer.RenderGizmo();
            _sceneRenderer.RenderDragHoverIndicator();
            _sceneRenderer.RenderDragCrosshair();
            _msaaFrameBuffer.Unbind();

            _msaaFrameBuffer.BlitTo(_sceneRenderWindowFrb);
        }
        else
        {
            _sceneRenderWindowFrb.Bind();
            _sceneRenderer.RenderSceneWindow(_sceneRenderWindowFrb);
            DrawDebug();
            _sceneRenderer.RenderGizmo();
            _sceneRenderer.RenderDragHoverIndicator();
            _sceneRenderer.RenderDragCrosshair();
            _sceneRenderWindowFrb.Unbind();
        }

        GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);

        var cursorPos = ImGui.GetCursorScreenPos();

        ImGui.Image(_sceneRenderWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
            new System.Numerics.Vector2(1, 0));

        ImageTopLeft = cursorPos;
        ImageSize = viewportSize;

        ImGui.End();
    }

    private void DrawDebug()
    {
        if (DebugRenderInScene is null)
            return;

        _debugBasicShader.Use();
        _debugBasicShader.SetMatrix4("view", _sceneRenderer.CamerasManager.CurrentCamera.ViewMatrix);
        _debugBasicShader.SetMatrix4("projection", _sceneRenderer.CamerasManager.CurrentCamera.ProjectionMatrix);
        GL.Disable(EnableCap.CullFace);
        DebugRenderInScene(_debugBasicShader);
        GL.Enable(EnableCap.CullFace);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _sceneRenderWindowFrb.Dispose();
        _debugBasicShader.Dispose();
        if (_msaaFrameBuffer is not null)
        {
            _msaaFrameBuffer.Dispose();
        }
    }

    public enum AntialiasingType
    {
        None,
        MSAA2,
        MSAA4,
        MSAA8,
    }

    private AntialiasingType _antialiasingType = AntialiasingType.None;

    public AntialiasingType Antialiasing
    {
        get
        {
            return _antialiasingType;
        }

        set
        {
            if (value == _antialiasingType)
                return;
            _antialiasingType = value;

            int samples = _antialiasingType switch
            {
                AntialiasingType.None => 0,
                AntialiasingType.MSAA2 => 2,
                AntialiasingType.MSAA4 => 4,
                AntialiasingType.MSAA8 => 8,
                _ => throw new ArgumentOutOfRangeException()
            };

            _msaaFrameBuffer?.Dispose();
            _msaaFrameBuffer = samples == 0
                ? null
                : new MsaaFrameBuffer(_sceneRenderWindowFrb.Width, _sceneRenderWindowFrb.Height, samples);
        }
    }
}