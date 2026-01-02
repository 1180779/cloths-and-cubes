using System.Runtime.CompilerServices;

using ImGuiNET;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Display.Light;

namespace Visualization.UiLayer.UI.Controls;

/// <summary>
/// A 3D interactive control for visualizing and editing a directional vector.
/// Renders a 3D arrow with coordinate axes in an ImGui window using GizmoArrow with MSAA x8.
///
/// The control consists of a 2x2 grid layout:
/// - Top-left: 3D view with orbit camera
/// - Top-right: XY plane view (orthographic)
/// - Bottom-left: XZ plane view (orthographic)
/// - Bottom-right: YZ plane view (orthographic)
///
/// The user can adjust the direction vector by:
/// - Left mouse drag in the 3D view to rotate the direction in 3D
/// - Left mouse drag in the planar views to set the direction's projection on that plane
/// - Right mouse drag in the 3D view to orbit the camera in the 3D view
/// </summary>
public sealed class DirectionArrowControl : IDisposable
{
    // 3D view (orbit camera)
    private readonly WindowFrameBuffer _framebuffer3D;
    private readonly MsaaFrameBuffer _msaaFramebuffer3D;
    private readonly OrbitCamera _camera3D;

    // Planar views (XY, XZ, YZ) with orthographic cameras
    private readonly WindowFrameBuffer _framebufferXY;
    private readonly MsaaFrameBuffer _msaaFramebufferXY;
    private readonly OrthographicCamera _cameraXY; // Looking along -Z

    private readonly WindowFrameBuffer _framebufferXZ;
    private readonly MsaaFrameBuffer _msaaFramebufferXZ;
    private readonly OrthographicCamera _cameraXZ; // Looking along -Y

    private readonly WindowFrameBuffer _framebufferYZ;
    private readonly MsaaFrameBuffer _msaaFramebufferYZ;
    private readonly OrthographicCamera _cameraYZ; // Looking along -X

    // Shared rendering resources
    private readonly Shader _shader;
    private readonly GizmoArrow _arrow;
    private readonly GizmoArrow _xAxisArrow;
    private readonly GizmoArrow _yAxisArrow;
    private readonly GizmoArrow _zAxisArrow;
    private bool _disposed;

    private enum PlaneView
    {
        XY,
        XZ,
        YZ
    }

    private enum ActiveView
    {
        None,
        View3D,
        XYPlane,
        XZPlane,
        YZPlane
    }

    /// <summary>
    /// Maps an active view to its corresponding planar view representation.
    ///
    /// Only the planar views (XY, XZ, YZ) are mapped; for other values the method returns XY by default.
    /// </summary>
    /// <param name="activeView">The currently active view, which specifies the perspective being interacted with.</param>
    /// <returns>The corresponding planar view based on the active view.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PlaneView PlaneViewFromActiveView(ActiveView activeView)
    {
        return activeView switch
        {
            ActiveView.XYPlane => PlaneView.XY,
            ActiveView.XZPlane => PlaneView.XZ,
            ActiveView.YZPlane => PlaneView.YZ,
            _ => PlaneView.XY
        };
    }

    // Interaction state
    private bool _wasRightMouseDown;
    private System.Numerics.Vector2 _lastMousePos;
    private ActiveView _activeView = ActiveView.None;

    private const int ViewportSize = 150;
    private const int MsaaSamples = 8;

    private static readonly Vector4 ArrowColor = new(1.0f, 0.8f, 0.2f, 1.0f); // Gold
    private static readonly Vector4 XAxisColor = new(1.0f, 0.0f, 0.0f, 0.5f); // Red
    private static readonly Vector4 YAxisColor = new(0.0f, 1.0f, 0.0f, 0.5f); // Green
    private static readonly Vector4 ZAxisColor = new(0.0f, 0.0f, 1.0f, 0.5f); // Blue
    private static readonly Vector3 BackgroundColor = new(0.15f, 0.15f, 0.15f); // Dark gray

    public DirectionArrowControl(Shader basicShader)
    {
        _framebuffer3D = new WindowFrameBuffer(ViewportSize, ViewportSize);
        _msaaFramebuffer3D = new MsaaFrameBuffer(ViewportSize, ViewportSize, MsaaSamples);
        _camera3D = new OrbitCamera(distance: 2.5f, aspectRatio: 1.0f) { PitchLimitDegrees = 45.0f };

        // XY plane view
        _framebufferXY = new WindowFrameBuffer(ViewportSize, ViewportSize);
        _msaaFramebufferXY = new MsaaFrameBuffer(ViewportSize, ViewportSize, MsaaSamples);
        _cameraXY = new OrthographicCamera(-3 * Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitY, 1.0f);

        // XZ plane view
        _framebufferXZ = new WindowFrameBuffer(ViewportSize, ViewportSize);
        _msaaFramebufferXZ = new MsaaFrameBuffer(ViewportSize, ViewportSize, MsaaSamples);
        _cameraXZ = new OrthographicCamera(3 * Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, 1.0f);

        // YZ plane view
        _framebufferYZ = new WindowFrameBuffer(ViewportSize, ViewportSize);
        _msaaFramebufferYZ = new MsaaFrameBuffer(ViewportSize, ViewportSize, MsaaSamples);
        _cameraYZ = new OrthographicCamera(3 * Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, 1.0f);

        _shader = basicShader;

        _arrow = new GizmoArrow(shaftLength: 0.8f, shaftRadius: 0.05f, tipHeight: 0.3f, tipRadius: 0.15f);
        _xAxisArrow = new GizmoArrow(shaftLength: 1.3f, shaftRadius: 0.035f, tipHeight: 0.2f, tipRadius: 0.1f);
        _yAxisArrow = new GizmoArrow(shaftLength: 1.3f, shaftRadius: 0.035f, tipHeight: 0.2f, tipRadius: 0.1f);
        _zAxisArrow = new GizmoArrow(shaftLength: 1.3f, shaftRadius: 0.035f, tipHeight: 0.2f, tipRadius: 0.1f);
    }


    /// <summary>
    /// Draws the 3D arrow control with 2x2 grid layout: 3D view + XY/XZ/YZ plane views.
    /// </summary>
    /// <param name="light">The directional light to update when the direction changes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw(LightDirectional light)
    {
        var direction = light.Direction.ToNumerics();
        Draw(ref direction, light);
    }

    /// <summary>
    /// Draws the 3D arrow control with 2x2 grid layout: 3D view + XY/XZ/YZ plane views.
    /// </summary>
    /// <param name="direction">The direction vector to display and edit.</param>
    /// <param name="light">The directional light to update when the direction changes.</param>
    public void Draw(ref System.Numerics.Vector3 direction, LightDirectional light)
    {
        RenderToFramebuffers(direction);

        // 2x2 Grid layout: [3D View][XY Plane] / [XZ Plane][YZ Plane]
        var canvasSize = new System.Numerics.Vector2(ViewportSize, ViewportSize);

        ImGui.BeginGroup();
        // Top-left: 3D view
        ImGui.Image(_framebuffer3D.TextureId, canvasSize,
            new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
        HandleViewInteraction(ref direction, light, ActiveView.View3D);
        ImGui.SameLine();
        // Top-right: XY plane
        ImGui.Image(_framebufferXY.TextureId, canvasSize,
            new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
        HandleViewInteraction(ref direction, light, ActiveView.XYPlane);
        ImGui.EndGroup();

        ImGui.BeginGroup();
        // Bottom-left: XZ plane
        ImGui.Image(_framebufferXZ.TextureId, canvasSize,
            new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
        HandleViewInteraction(ref direction, light, ActiveView.XZPlane);
        ImGui.SameLine();
        // Bottom-right: YZ plane
        ImGui.Image(_framebufferYZ.TextureId, canvasSize,
            new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
        HandleViewInteraction(ref direction, light, ActiveView.YZPlane);
        ImGui.EndGroup();

        ImGui.Text("LMB: adjust | RMB: orbit 3D | Views: 3D/XY/XZ/YZ");
    }

    /// <summary>
    /// Renders the 3D arrow and its planar projections to their respective framebuffers.
    /// </summary>
    /// <param name="direction">The direction vector to use when rendering the 3D arrow and its projections.</param>
    private void RenderToFramebuffers(System.Numerics.Vector3 direction)
    {
        // Save OpenGL state
        int previousFbo = GL.GetInteger(GetPName.FramebufferBinding);
        int[] previousViewport = new int[4];
        GL.GetInteger(GetPName.Viewport, previousViewport);
        bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
        bool blendEnabled = GL.IsEnabled(EnableCap.Blend);
        int[] blendSrc = new int[1];
        int[] blendDst = new int[1];
        GL.GetInteger(GetPName.BlendSrc, blendSrc);
        GL.GetInteger(GetPName.BlendDst, blendDst);

        // Render all views
        Render3DArrow(direction);
        RenderPlanarArrow(direction, PlaneView.XY);
        RenderPlanarArrow(direction, PlaneView.XZ);
        RenderPlanarArrow(direction, PlaneView.YZ);

        // Restore OpenGL state
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFbo);
        GL.Viewport(previousViewport[0], previousViewport[1], previousViewport[2], previousViewport[3]);
        if (!depthTestEnabled)
            GL.Disable(EnableCap.DepthTest);
        if (!blendEnabled)
            GL.Disable(EnableCap.Blend);
        else
            GL.BlendFunc((BlendingFactor)blendSrc[0], (BlendingFactor)blendDst[0]);
    }

    private void HandleViewInteraction(
        ref System.Numerics.Vector3 direction,
        LightDirectional light,
        ActiveView activeView // not ActiveView.None
    )
    {
        bool isHovered = ImGui.IsItemHovered();
        var currentMousePos = ImGui.GetMousePos();
        var itemMin = ImGui.GetItemRectMin();
        var itemSize = ImGui.GetItemRectSize();
        var center = itemMin + itemSize * 0.5f;

        HandleMouseDirectionAdjustment(ref direction, light, activeView, currentMousePos, center, isHovered);
        HandleMouseOrbit(activeView, currentMousePos, isHovered);
    }

    private void HandleMouseDirectionAdjustment(
        ref System.Numerics.Vector3 direction,
        LightDirectional light,
        ActiveView activeView, // not ActiveView.None
        System.Numerics.Vector2 currentMousePos,
        System.Numerics.Vector2 center,
        bool isHovered)
    {
        // Left mouse: Adjust direction
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            // If this view is already active, continue dragging regardless of hover
            if (_activeView == activeView)
            {
                if (_activeView == ActiveView.View3D)
                {
                    // 3D view: use deltas
                    var mouseDelta = currentMousePos - _lastMousePos;
                    UpdateDirectionFromMouseDelta(ref direction, mouseDelta, light);
                    _lastMousePos = currentMousePos;
                }
                else
                {
                    // Planar views: use absolute position relative to center
                    var relativePos = currentMousePos - center;
                    UpdateDirectionInPlane(ref direction, relativePos, light, PlaneViewFromActiveView(activeView));
                    _lastMousePos = currentMousePos;
                }
            }
            // Only start a new drag if no view is active and control is hovered
            else if (_activeView == ActiveView.None && isHovered)
            {
                _activeView = activeView;
                _lastMousePos = currentMousePos;

                // For planar views, apply immediately on click
                if (activeView != ActiveView.View3D)
                {
                    var relativePos = currentMousePos - center;
                    UpdateDirectionInPlane(ref direction, relativePos, light, PlaneViewFromActiveView(activeView));
                }
            }
        }
        else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _activeView == activeView)
        {
            _activeView = ActiveView.None;
            _lastMousePos = System.Numerics.Vector2.Zero;
        }
    }

    private void HandleMouseOrbit(
        ActiveView activeView, // not ActiveView.None
        System.Numerics.Vector2 currentMousePos,
        bool isHovered)
    {
        // Right mouse: Orbit 3D view only
        if (activeView == ActiveView.View3D && ImGui.IsMouseDown(ImGuiMouseButton.Right))
        {
            // If already orbiting, continue regardless of hover
            if (_wasRightMouseDown)
            {
                var mouseDelta = currentMousePos - _lastMousePos;
                _lastMousePos = currentMousePos;
                const float sensitivity = 0.3f;
                _camera3D.YawRadians += mouseDelta.X * sensitivity;
                _camera3D.PitchRadians -= mouseDelta.Y * sensitivity;
            }
            // Only start orbiting if not already orbiting and control is hovered
            else if (isHovered)
            {
                _wasRightMouseDown = true;
                _lastMousePos = currentMousePos;
            }
        }
        else if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && _wasRightMouseDown)
        {
            _wasRightMouseDown = false;
            _lastMousePos = System.Numerics.Vector2.Zero;
        }
    }

    /// <summary>
    /// Renders an arrow pointing in the specified direction in 3D along with axis arrows.
    /// They are rendered to the <see cref="_framebuffer3D">_framebuffer3D</see> internal framebuffer for the 3D view.
    /// </summary>
    /// <param name="direction">The normalized direction vector of the arrow to be rendered.</param>
    private void Render3DArrow(System.Numerics.Vector3 direction)
    {
        _msaaFramebuffer3D.Bind();
        GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _camera3D.SetForSimpleShader(_shader);

        _arrow.Render(_shader, Vector3.Zero, direction.ToOpenTK(), ArrowColor);
        _xAxisArrow.Render(_shader, Vector3.Zero, Vector3.UnitX, XAxisColor);
        _yAxisArrow.Render(_shader, Vector3.Zero, Vector3.UnitY, YAxisColor);
        _zAxisArrow.Render(_shader, Vector3.Zero, Vector3.UnitZ, ZAxisColor);

        _msaaFramebuffer3D.BlitTo(_framebuffer3D);
    }

    /// <summary>
    /// Renders an arrow pointing in the specified direction on a selected 2D plane.
    /// The arrow is rendered to the corresponding internal framebuffer for the planar view.
    /// </summary>
    /// <param name="direction">The normalized direction vector of the arrow to be rendered.</param>
    /// <param name="planeView">The 2D plane on which the arrow is rendered.</param>
    private void RenderPlanarArrow(System.Numerics.Vector3 direction, PlaneView planeView)
    {
        // Bind MSAA framebuffer for rendering
        switch (planeView)
        {
            case PlaneView.XY: _msaaFramebufferXY.Bind(); break;
            case PlaneView.XZ: _msaaFramebufferXZ.Bind(); break;
            case PlaneView.YZ: _msaaFramebufferYZ.Bind(); break;
        }

        GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        switch (planeView)
        {
            case PlaneView.XY: _cameraXY.SetForSimpleShader(_shader); break;
            case PlaneView.XZ: _cameraXZ.SetForSimpleShader(_shader); break;
            case PlaneView.YZ: _cameraYZ.SetForSimpleShader(_shader); break;
        }

        _arrow.Render(_shader, Vector3.Zero, direction.ToOpenTK(), ArrowColor);
        _xAxisArrow.Render(_shader, Vector3.Zero, Vector3.UnitX, XAxisColor);
        _yAxisArrow.Render(_shader, Vector3.Zero, Vector3.UnitY, YAxisColor);
        _zAxisArrow.Render(_shader, Vector3.Zero, Vector3.UnitZ, ZAxisColor);

        switch (planeView)
        {
            case PlaneView.XY: _msaaFramebufferXY.BlitTo(_framebufferXY); break;
            case PlaneView.XZ: _msaaFramebufferXZ.BlitTo(_framebufferXZ); break;
            case PlaneView.YZ: _msaaFramebufferYZ.BlitTo(_framebufferYZ); break;
        }
    }

    private void UpdateDirectionFromMouseDelta(
        ref System.Numerics.Vector3 direction,
        System.Numerics.Vector2 mouseDelta,
        LightDirectional light)
    {
        // project current direction into the camera's coordinate system
        var dir = direction.ToOpenTK();
        float x = Vector3.Dot(dir, _camera3D.Right);
        float y = Vector3.Dot(dir, _camera3D.Up);
        float z = Vector3.Dot(dir, _camera3D.Front);

        // Convert mouse delta to world space movement
        // World units per pixel = visible height / viewport height
        //
        // For perspective
        // VisibleHeight = 2 * tan(VerticalFOV / 2) * Distance
        // 
        // Here: Distance = 2.5, FOV = 90 deg (Pi/2) -> tan(45 deg) = 1
        // Visible height = 2 * 2.5 * 1 = 5.0
        // Viewport is 150px
        // Thus, 1 pixel = 5.0 / 150.0 in world units
        const float sensitivity = 5.0f / 150.0f;

        x += mouseDelta.X * sensitivity;
        y -= mouseDelta.Y * sensitivity;
        Vector3 newDir = x * _camera3D.Right + y * _camera3D.Up + z * _camera3D.Front;
        newDir.Normalize();

        direction = newDir.ToNumerics();
        light.Direction = newDir;
    }

    private void UpdateDirectionInPlane(
        ref System.Numerics.Vector3 direction,
        System.Numerics.Vector2 relativeMousePos,
        LightDirectional light,
        PlaneView planeView)
    {
        OrthographicCamera camera = planeView switch
        {
            PlaneView.XY => _cameraXY,
            PlaneView.XZ => _cameraXZ,
            PlaneView.YZ => _cameraYZ,
            _ => _cameraXY
        };

        // project current direction into the camera's coordinate system
        var dir = direction.ToOpenTK();
        float x = Vector3.Dot(dir, camera.Right);
        float y = Vector3.Dot(dir, camera.Up);
        float z = Vector3.Dot(dir, camera.Front);

        // Calculate the target angle from mouse position relative to center
        float targetX = relativeMousePos.X;
        float targetY = -relativeMousePos.Y;
        float newAngle = MathF.Atan2(targetY, targetX);

        // Reconstruct the vector using the new angle but keeping the original magnitude in the plane
        float magnitude = MathF.Sqrt(x * x + y * y);
        float newX = magnitude * MathF.Cos(newAngle);
        float newY = magnitude * MathF.Sin(newAngle);
        Vector3 newDir = newX * camera.Right + newY * camera.Up + z * camera.Front;
        newDir.Normalize();

        direction = newDir.ToNumerics();
        light.Direction = newDir;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose of the 3D view
        _framebuffer3D.Dispose();
        _msaaFramebuffer3D.Dispose();

        // Dispose of the planar views
        _framebufferXY.Dispose();
        _msaaFramebufferXY.Dispose();
        _framebufferXZ.Dispose();
        _msaaFramebufferXZ.Dispose();
        _framebufferYZ.Dispose();
        _msaaFramebufferYZ.Dispose();

        // Dispose of the shared resources
        // _shader.Dispose(); // shader is borrowed, do not dispose here
        _arrow.Dispose();
        _xAxisArrow.Dispose();
        _yAxisArrow.Dispose();
        _zAxisArrow.Dispose();
    }
}