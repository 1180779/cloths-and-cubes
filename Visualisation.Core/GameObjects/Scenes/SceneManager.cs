using Engine.RigidBodies;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects.Scenes;

public abstract class SceneManager : IDisposable
{
    public SceneManager(float aspectRatio, IInputProvider inputProvider)
    {
        EnvironmentMap = new(
            Hdr,
            EquirectangularToCubemapShader,
            IrradianceConvolutionShader,
            PrefilterShader,
            BrdfLutShader);

        CamerasManager = new CamerasManager(inputProvider);
        LightsManager = new LightsManager(CamerasManager);

        // TODO: remove magic numbers
        var camera = new FreeMovingCamera(aspectRatio)
        {
            Position = new Vector3(-6.5f, 3.2f, 6.6f), PitchDegrees = 6.3f, YawDegrees = -777.8f,
        };
        CamerasManager.AddCamera(camera);

        _translationGizmo = new TranslationGizmo(BasicShader);
        _scaleGizmo = new ScaleGizmo(BasicShader);
        _rotationGizmo = new RotationGizmo(BasicShader);

        StaticDragManager = new(() => _selectionManager?.HoveredObject ?? null, () => CamerasManager.CurrentCamera);
    }

    public StaticDragManager StaticDragManager;

    public Func<float>? PositionEpsilonProvider { get; set; }

    public const float OutlineSize = 0.05f;
    public const float OutlineFactor = 1f + OutlineSize;

    public readonly Shader PbrShader = new("scenePBRShader.vert", "scenePBRShader.frag");
    public readonly Shader BasicShader = new("sceneBasicShader.vert", "sceneBasicShader.frag");
    public readonly Shader OutlineShader = new("outline.vert", "sceneBasicShader.frag");

    public readonly Shader EquirectangularToCubemapShader =
        new("cubemap.vert", "equirectangularToCubemapShader.frag");

    public readonly Shader IrradianceConvolutionShader = new("cubemap.vert", "irradianceConvolutionShader.frag");
    public readonly Shader PrefilterShader = new("cubemap.vert", "prefilterShader.frag");

    public readonly Shader SkyboxShader = new("sceneSkyboxShader.vert", "sceneSkyboxShader.frag");

    public readonly Shader BrdfLutShader = new("depthMapShader.vert", "brdfLUTShader.frag");
    private const string Hdr = "Hdr/symmetrical_garden_02_4k.exr";
    public EnvironmentMap EnvironmentMap { get; set; }
    private CubeMesh _cube = new();

    public LightsManager LightsManager { get; private set; }
    public CamerasManager CamerasManager { get; private set; }
    protected List<GameObject> _gameObjects = [];
    public ICollection<GameObject> GameObjects => _gameObjects;

    private readonly TranslationGizmo _translationGizmo;
    private readonly ScaleGizmo _scaleGizmo;
    private readonly RotationGizmo _rotationGizmo;
    private IGizmo? _activeGizmo;
    public IGizmo? ActiveGizmo => _activeGizmo;

    public GizmoType ActiveGizmoType
    {
        get
        {
            return ActiveGizmo switch
            {
                Display.Gizmos.Translation.TranslationGizmo => GizmoType.Translation,
                Display.Gizmos.Rotation.RotationGizmo => GizmoType.Rotation,
                Display.Gizmos.Scale.ScaleGizmo => GizmoType.Scale,
                _ => GizmoType.None
            };
        }
    }

    public void AddGameObject(GameObject gameObject)
    {
        _gameObjects.Add(gameObject);
    }

    public void RemoveGameObject(GameObject gameObject)
    {
        _gameObjects.Remove(gameObject);
    }

    public abstract void SetUp();

    private SelectionManager? _selectionManager;

    public SelectionManager? SelectionManager
    {
        get => _selectionManager;
        set
        {
            _selectionManager = value;
        }
    }

    public Vector4 SelectionColor = new(0.0f, 1.0f, 0.0f, 1.0f);

    public void ProcessInputInAndOutOfFocus(IInputProvider input, float dt)
    {
        // Gizmo switching
        if (input.IsKeyPressed(InputKey.T))
        {
            SetActiveGizmoType(GizmoType.Translation);
        }
        else if (input.IsKeyPressed(InputKey.Y))
        {
            SetActiveGizmoType(GizmoType.Scale);
            _activeGizmo = _scaleGizmo;
        }
        else if (input.IsKeyPressed(InputKey.U))
        {
            SetActiveGizmoType(GizmoType.Rotation);
        }
    }

    public void ProcessInputOutOfFocus(IInputProvider input, float dt)
    {
        CamerasManager.ProcessInputOutOfFocus(input, dt);
    }

    public void ProcessInputInFocus(IInputProvider input, float dt)
    {
        CamerasManager.ProcessInput(input, dt, SelectionManager?.SelectionEnabled ?? false);
    }

    public void HandleInput(IInputProvider input, Vector2 viewportMousePos, Vector2i screenSize)
    {
        // When camera mode is active (cursor grabbed), use center of screen for raycasting
        // since the mouse position doesn't move
        Vector2 raycastPos = input.GetCursorState() == CursorState.Grabbed
            ? new Vector2(screenSize.X / 2f, screenSize.Y / 2f)
            : viewportMousePos;

        // Update hover detection with the appropriate position
        SelectionManager?.UpdateHover(raycastPos, screenSize);

        // Priority system: Once an input handler starts an operation (gizmo drag, mouse drag),
        // it maintains priority until the operation completes (mouse button released)

        // Check if gizmo is already active (the highest priority when active)
        if (!(SelectionManager?.GizmosEnabled ?? false) && _activeGizmo?.Target is not null)
        {
            _activeGizmo.Target = null;
            SetActiveGizmoType(GizmoType.None);
        }

        if (((SelectionManager?.GizmosEnabled ?? false) && (_activeGizmo?.IsActive ?? false)) &&
            _activeGizmo is not null)
        {
            _activeGizmo.HandleInput(input, raycastPos, CamerasManager.CurrentCamera, screenSize);
            return;
        }

        if (StaticDragManager.Enabled && StaticDragManager.ShowHoverIndicator &&
            SelectionManager?.HoveredObject is not null &&
            _activeGizmo is not null &&
            (_activeGizmo.Target is not null || SelectionManager.SelectedObject is not null))
        {
            SelectionManager.ClearSelection();
            _activeGizmo.Target = null;
        }

        // Check if the drag manager is already active
        if (StaticDragManager.IsDragging)
        {
            StaticDragManager.HandleInput(input);
            return;
        }

        // Check gizmo first (the highest priority for new operations)
        bool gizmoTookMouseInput = false;
        if ((SelectionManager?.GizmosEnabled ?? false) && _activeGizmo is not null)
        {
            _activeGizmo.Target = (IGizmoTarget?)_selectionManager?.SelectedObject;
            gizmoTookMouseInput =
                _activeGizmo?.HandleInput(input, raycastPos, CamerasManager.CurrentCamera, screenSize) ?? false;
        }

        bool selectedHoveredObject = !gizmoTookMouseInput && input.IsMouseButtonPressed(MouseButton.Left) &&
            (SelectionManager?.SelectHoveredObject() ?? false);

        // Check if the drag manager can use the hovered object
        if (!gizmoTookMouseInput && StaticDragManager.Enabled)
        {
            bool dragTookMouseInput = StaticDragManager.HandleInput(input);
            if (dragTookMouseInput)
            {
                SelectionManager?.ClearSelection();
                if (_activeGizmo is not null)
                {
                    _activeGizmo.Target = null;
                }

                return;
            }
        }
    }

    public void RenderSceneWindow(IBindable framebuffer)
    {
        LightsManager.RenderShadowsToMaps(_gameObjects);

        framebuffer.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.Enable(EnableCap.StencilTest);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

        GL.StencilMask(0x00);
        RenderSkybox();

        PbrShader.Use();
        SetSharedPbrUniforms();

        GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
        GL.StencilMask(0xFF);
        foreach (var gameObject in _gameObjects)
        {
            if (SelectionManager is not null && gameObject == SelectionManager.SelectedObject)
            {
                GL.StencilMask(0xFF);
                GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
            }
            else
            {
                GL.StencilMask(0x00);
            }

            // Also enable polygon offset to prevent Z-fighting with coplanar surfaces
            // The offset is dynamically scaled based on physics positionEpsilon
            if (gameObject is Cloth)
            {
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.PolygonOffsetFill);
                // Negative values pull cloth toward camera
                float offset = -(PositionEpsilonProvider?.Invoke() ?? 0) * 1000.0f;
                GL.PolygonOffset(offset, offset);
            }

            gameObject.SetForShader(PbrShader);
            gameObject.Render(SelectionManager?.DrawInvisibleObjects ?? false);

            // Re-enable backface culling and disable polygon offset after rendering cloth
            if (gameObject is Cloth)
            {
                GL.Enable(EnableCap.CullFace);
                GL.Disable(EnableCap.PolygonOffsetFill);
            }

            if (SelectionManager is not null && gameObject == SelectionManager.SelectedObject)
            {
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            }
        }

        if (SelectionManager?.SelectedObject is not null)
        {
            GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
            GL.StencilMask(0x00);
            if (SelectionManager.DrawSelectedObjectWithoutDepthTesting)
            {
                GL.Disable(EnableCap.DepthTest);
            }

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

            RenderObjectOutline(SelectionManager.SelectedObject, SelectionColor, OutlineFactor,
                SelectionManager.DrawInvisibleObjects);

            GL.StencilMask(0xFF);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.Enable(EnableCap.DepthTest);
        }

        GL.Disable(EnableCap.StencilTest);
    }

    public void RenderGizmo()
    {
        if (_activeGizmo != null)
        {
            _activeGizmo.Render(CamerasManager.CurrentCamera);
        }
    }

    /// <summary>
    /// Renders an outline around the specified object.
    /// </summary>
    /// <param name="obj">The object to outline (GameObject or RigidParticle)</param>
    /// <param name="color">The color of the outline</param>
    /// <param name="scaleFactor">How much to scale the object for the outline (e.g., 1.02 for 2% larger)</param>
    /// <param name="drawInvisible">Whether to draw even if the object is marked invisible</param>
    public void RenderObjectOutline(
        object obj,
        Vector4 color,
        float scaleFactor = OutlineFactor,
        bool drawInvisible = false)
    {
        BasicShader.Use();
        CamerasManager.CurrentCamera.SetForSimpleShader(BasicShader);
        BasicShader.SetVector3("color", color.Xyz);
        BasicShader.SetFloat("alpha", color.W);

        switch (obj)
        {
            case Box box:
                {
                    var originalHalfSize = box.EngineBox.HalfSize;
                    box.EngineBox.HalfSize *= scaleFactor;

                    box.SetForShaderNoMaterial(BasicShader);
                    box.Render(drawInvisible);

                    box.EngineBox.HalfSize = originalHalfSize;
                    break;
                }
            case Ball ball:
                {
                    var originalRadius = ball.EngineBall.Radius;
                    ball.EngineBall.Radius *= scaleFactor;

                    ball.SetForShaderNoMaterial(BasicShader);
                    ball.Render(drawInvisible);

                    ball.EngineBall.Radius = originalRadius;
                    break;
                }
            case Cloth cloth:
                {
                    // Disable culling for two-sided rendering
                    GL.Disable(EnableCap.CullFace);

                    OutlineShader.Use();
                    CamerasManager.CurrentCamera.SetForSimpleShader(OutlineShader);
                    OutlineShader.SetFloat("outline_size", OutlineSize);
                    OutlineShader.SetVector3("color", color.Xyz);
                    cloth.SetForShaderNoMaterial(OutlineShader);
                    cloth.Render(drawInvisible);

                    // Re-enable culling
                    GL.Enable(EnableCap.CullFace);
                    break;
                }
            case ClothParticleWrapper particleWrapper:
                {
                    var innerParticle = particleWrapper.Particle;
                    switch (innerParticle)
                    {
                        case ClothRigidParticleInCorner particleInCorner:
                            {
                                var particleScale = particleInCorner.BoundingBoxHalfSize * 2.0f;
                                var position = particleInCorner.GetAxis(3);
                                BasicShader.SetMatrix4("model",
                                    Matrix4.CreateScale(particleScale, particleScale, particleScale) *
                                    Matrix4.CreateTranslation(position.X, position.Y, position.Z));
                                _cube.Render();
                                break;
                            }
                        // TODO: remove; depreciated
                        case { } particle:
                            {
                                var particleScale = RigidParticle.BoundingBoxHalfSize * 2.0f;
                                var position = particle.GetAxis(3);
                                BasicShader.SetMatrix4("model",
                                    Matrix4.CreateScale(particleScale, particleScale, particleScale) *
                                    Matrix4.CreateTranslation(position.X, position.Y, position.Z));
                                _cube.Render();
                                break;
                            }
                    }

                    break;
                }
            case Cylinder cylinder:
                {
                    var originalRadius = cylinder.EngineCylinder.Radius;
                    cylinder.EngineCylinder.Radius *= scaleFactor;
                    var originalHeight = cylinder.EngineCylinder.Height;
                    cylinder.EngineCylinder.Height *= scaleFactor;

                    cylinder.SetForShaderNoMaterial(BasicShader);
                    cylinder.Render(drawInvisible);

                    cylinder.EngineCylinder.Radius = originalRadius;
                    cylinder.EngineCylinder.Height = originalHeight;
                    break;
                }
            case Cone cone:
                {
                    var originalRadius = cone.EngineCone.Radius;
                    cone.EngineCone.Radius *= scaleFactor;
                    var originalHeight = cone.EngineCone.Height;
                    cone.EngineCone.Height *= scaleFactor;

                    cone.SetForShaderNoMaterial(BasicShader);
                    cone.Render(drawInvisible);

                    cone.EngineCone.Radius = originalRadius;
                    cone.EngineCone.Height = originalHeight;
                    break;
                }
        }
    }

    /// <summary>
    /// Renders the hover indicator for the drag manager to show which object can be dragged.
    /// Only shows when: drag manager is enabled, object is hovered, not currently dragging,
    /// and the object is not selected for gizmo manipulation.
    /// </summary>
    public void RenderDragHoverIndicator()
    {
        if (!StaticDragManager.Enabled || !StaticDragManager.ShowHoverIndicator ||
            StaticDragManager.HoverTarget == null || StaticDragManager.IsDragging)
            return;

        // Don't show hover indicator if the object is selected for gizmo manipulation
        if (SelectionManager?.SelectedObject != null &&
            SelectionManager.SelectedObject == StaticDragManager.HoverTarget)
            return;

        GL.Disable(EnableCap.DepthTest);

        RenderObjectOutline(StaticDragManager.HoverTarget, StaticDragManager.HoverIndicatorColor,
            StaticDragManager.HoverIndicatorScale, drawInvisible: true);

        GL.Enable(EnableCap.DepthTest);
    }

    public void RenderSelectedObjectOnTop()
    {
        if (SelectionManager is { DrawSelectedObjectWithoutDepthTesting: true, SelectedObject: GameObject gameObject })
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);
            PbrShader.Use();
            SetSharedPbrUniforms();
            gameObject.SetForShader(PbrShader);
            gameObject.Render(SelectionManager.DrawInvisibleObjects);
        }
    }

    private void RenderSkybox()
    {
        SkyboxShader.Use();
        SkyboxShader.SetMatrix4("view", CamerasManager.CurrentCamera.ViewMatrix);
        SkyboxShader.SetMatrix4("projection", CamerasManager.CurrentCamera.ProjectionMatrix);
        EnvironmentMap.SetForSkyBoxShader(SkyboxShader);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.CullFace(TriangleFace.Front);
        _cube.Render();
        GL.CullFace(TriangleFace.Back);
        GL.DepthFunc(DepthFunction.Less);
    }


    private void SetSharedPbrUniforms()
    {
        EnvironmentMap.SetForPbrShader(PbrShader);
        CamerasManager.CurrentCamera.SetForPbrShader(PbrShader);
        LightsManager.SetForShader(PbrShader);
    }

    /// <summary>
    /// Sets the active gizmo type based on enum value.
    /// </summary>
    public void SetActiveGizmoType(GizmoType gizmoType)
    {
        _activeGizmo = gizmoType switch
        {
            GizmoType.None => null,
            GizmoType.Translation => _translationGizmo,
            GizmoType.Rotation => _rotationGizmo,
            GizmoType.Scale => _scaleGizmo,
            _ => null
        };

        if (_activeGizmo is not null && SelectionManager?.SelectedObject is IGizmoTarget gizmoTarget)
        {
            _activeGizmo.Target = gizmoTarget;
        }
    }

    /// <summary>
    /// Resets all gizmo handle sizes to 1.0.
    /// </summary>
    public void ResetAllGizmoScales()
    {
        _translationGizmo.HandleSize = 1.0f;
        _rotationGizmo.HandleSize = 1.0f;
        _scaleGizmo.HandleSize = 1.0f;
    }

    /// <summary>
    /// Clears the current selection and deactivates any active gizmo.
    /// Should be called when loading or resetting scenes to prevent stale references.
    /// </summary>
    public void ClearSelectionAndGizmos()
    {
        // Clear selection first (this will also trigger OnSelectionChanged event)
        SelectionManager?.ClearSelection();

        // Deactivate any active gizmo
        _activeGizmo = null;
    }

    public TranslationGizmo TranslationGizmo => _translationGizmo;
    public RotationGizmo RotationGizmo => _rotationGizmo;
    public ScaleGizmo ScaleGizmo => _scaleGizmo;


    public void Dispose()
    {
        _cube.Dispose();
        PbrShader.Dispose();
        BasicShader.Dispose();
        OutlineShader.Dispose();
        SkyboxShader.Dispose();
        EquirectangularToCubemapShader.Dispose();

        LightsManager.Dispose();
        foreach (var gameObject in _gameObjects)
        {
            gameObject.Dispose();
        }

        _translationGizmo.Dispose();
        _scaleGizmo.Dispose();
        _rotationGizmo.Dispose();
    }
}