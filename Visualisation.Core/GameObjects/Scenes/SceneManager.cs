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
    }

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

    private void SelectionChange(object? obj)
    {
        if (ActiveGizmo is not null)
        {
            if (obj is IGizmoTarget gizmoTarget)
                ActiveGizmo.Target = gizmoTarget;
            else
                ActiveGizmo.Target = null;
        }
    }

    private SelectionManager? _selectionManager;

    public SelectionManager? SelectionManager
    {
        get => _selectionManager;
        set
        {
            if (_selectionManager is not null)
            {
                _selectionManager.OnSelectionChanged -= SelectionChange;
            }

            _selectionManager = value;
            if (_selectionManager is not null)
            {
                _selectionManager.OnSelectionChanged += SelectionChange;
            }
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
        bool gizmoHandled = false;
        if (_activeGizmo != null)
        {
            gizmoHandled = _activeGizmo.HandleInput(input, viewportMousePos, CamerasManager.CurrentCamera, screenSize);
        }

        if (!gizmoHandled && SelectionManager != null)
        {
            SelectionManager.HandleInput(viewportMousePos, screenSize);
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

            // Disable backface culling for cloth to enable two-sided rendering
            if (gameObject is Cloth)
            {
                GL.Disable(EnableCap.CullFace);
            }

            gameObject.SetForShader(PbrShader);
            gameObject.Render(SelectionManager?.DrawInvisibleObjects ?? false);

            // Re-enable backface culling after rendering cloth
            if (gameObject is Cloth)
            {
                GL.Enable(EnableCap.CullFace);
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

            BasicShader.Use();
            CamerasManager.CurrentCamera.SetForSimpleShader(BasicShader);
            BasicShader.SetVector3("color", SelectionColor.Xyz);
            BasicShader.SetFloat("alpha", SelectionColor.W);

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            var selectedObject = SelectionManager.SelectedObject;
            switch (selectedObject)
            {
                case Box box:
                    {
                        var originalHalfSize = box.EngineBox.HalfSize;
                        box.EngineBox.HalfSize *= OutlineFactor;

                        box.SetForShaderNoMaterial(BasicShader);
                        box.Render(SelectionManager.DrawInvisibleObjects);

                        box.EngineBox.HalfSize = originalHalfSize;
                        break;
                    }
                case Ball ball:
                    {
                        var originalRadius = ball.EngineBall.Radius;
                        ball.EngineBall.Radius *= OutlineFactor;

                        ball.SetForShaderNoMaterial(BasicShader);
                        ball.Render(SelectionManager.DrawInvisibleObjects);

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
                        OutlineShader.SetVector3("color", SelectionColor.Xyz);
                        cloth.SetForShaderNoMaterial(OutlineShader);
                        cloth.Render(SelectionManager.DrawInvisibleObjects);

                        // Re-enable culling
                        GL.Enable(EnableCap.CullFace);
                        break;
                    }
                case RigidParticle particle:
                    {
                        var particleScale = RigidParticle.BoundingBoxHalfSize * 2;
                        var position = particle.GetAxis(3);
                        BasicShader.SetMatrix4("model",
                            Matrix4.CreateScale(particleScale, particleScale, particleScale) *
                            Matrix4.CreateTranslation(position.X, position.Y, position.Z));
                        _cube.Render();
                        break;
                    }
                case Cylinder cylinder:
                    {
                        var originalRadius = cylinder.EngineCylinder.Radius;
                        cylinder.EngineCylinder.Radius *= OutlineFactor;
                        var originalHeight = cylinder.EngineCylinder.Height;
                        cylinder.EngineCylinder.Height *= OutlineFactor;

                        cylinder.SetForShaderNoMaterial(BasicShader);
                        cylinder.Render(SelectionManager.DrawInvisibleObjects);

                        cylinder.EngineCylinder.Radius = originalRadius;
                        cylinder.EngineCylinder.Height = originalHeight;
                        break;
                    }
                case Cone cone:
                    {
                        var originalRadius = cone.EngineCone.Radius;
                        cone.EngineCone.Radius *= OutlineFactor;
                        var originalHeight = cone.EngineCone.Height;
                        cone.EngineCone.Height *= OutlineFactor;

                        cone.SetForShaderNoMaterial(BasicShader);
                        cone.Render(SelectionManager.DrawInvisibleObjects);

                        cone.EngineCone.Radius = originalRadius;
                        cone.EngineCone.Height = originalHeight;
                        break;
                    }
            }

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