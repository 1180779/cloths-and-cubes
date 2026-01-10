using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.ContactGenerators;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Visualisation.Core.Display;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects.Scenes;

public abstract class SceneRenderer : IDisposable
{
    public SceneRenderer(
        float aspectRatio,
        IInputProvider inputProvider,
        Func<IEnumerable<GameObject>> getGameObjects,
        Func<CameraBase> cameraProvider,
        Func<BVH> bvhProvider,
        Func<Dictionary<int, IBoxable>> bvhDictionaryProvider,
        Func<Dictionary<Engine.Cloth, Cloth>> clothsProvider,
        Func<Plane> planeProvider,
        Func<float> positionEpsilonProvider,
        Func<IEnumerable<Box>> boxesProvider,
        Func<GlobalJointsList> globalJointsProvider)
    {
        _getGameObjects = getGameObjects;

        EnvironmentMap = new(
            Hdr,
            EquirectangularToCubemapShader,
            IrradianceConvolutionShader,
            PrefilterShader,
            BrdfLutShader);

        InteractionManager = new(
            BasicShader,
            inputProvider,
            cameraProvider,
            bvhProvider,
            bvhDictionaryProvider,
            clothsProvider,
            planeProvider,
            positionEpsilonProvider,
            boxesProvider,
            globalJointsProvider);

        CamerasManager = new CamerasManager(inputProvider);
        LightsManager = new LightsManager(CamerasManager);

        // TODO: remove magic numbers
        var camera = new FreeMovingCamera(aspectRatio)
        {
            Position = new Vector3(-6.5f, 3.2f, 6.6f), PitchDegrees = 6.3f, YawDegrees = -777.8f,
        };
        CamerasManager.AddCamera(camera);
    }

    public bool DrawSelectedObjectWithoutDepthTesting;

    protected readonly Func<IEnumerable<GameObject>> _getGameObjects;
    public InteractionManager InteractionManager { get; set; }

    public Func<float>? PositionEpsilonProvider { get; set; }

    public const float OutlineSize = 0.05f;

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

    public SelectionManager SelectionManager => InteractionManager.SelectionManager;

    public abstract void SetUp();

    public Vector4 SelectionColor = new(0.0f, 1.0f, 0.0f, 1.0f);

    public void ProcessInputInAndOutOfFocus(IInputProvider input, float dt)
    {
        // Gizmo switching
        if (input.IsKeyPressed(InputKey.T))
        {
            InteractionManager.SetActiveGizmoType(GizmoType.Translation);
        }
        else if (input.IsKeyPressed(InputKey.Y))
        {
            InteractionManager.SetActiveGizmoType(GizmoType.Scale);
        }
        else if (input.IsKeyPressed(InputKey.U))
        {
            InteractionManager.SetActiveGizmoType(GizmoType.Rotation);
        }
    }

    public void ProcessInputOutOfFocus(IInputProvider input, float dt)
    {
        CamerasManager.ProcessInputOutOfFocus(input, dt);
    }

    public void ProcessInputInFocus(IInputProvider input, float dt)
    {
        CamerasManager.ProcessInput(input, dt, SelectionManager.SelectionEnabled);
    }

    public void HandleInput(IInputProvider input, Vector2 viewportMousePos, Vector2i screenSize)
    {
        InteractionManager.HandleInput(input, viewportMousePos, screenSize);
    }

    public void RenderSceneWindow(IBindable framebuffer)
    {
        var gameObjects = _getGameObjects();
        LightsManager.RenderShadowsToMaps(gameObjects);

        gameObjects = _getGameObjects();
        framebuffer.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        GL.Enable(EnableCap.StencilTest);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

        GL.StencilMask(0x00);
        RenderSkybox();

        PbrShader.Use();
        SetSharedPbrUniforms();

        // Create RenderContext
        var renderContext = new RenderContext
        {
            Camera = CamerasManager.CurrentCamera,
            DefaultShader = BasicShader,
            OutlineShader = OutlineShader,
            PbrShader = PbrShader,
            EnvironmentMap = EnvironmentMap,
            LightsManager = LightsManager,
            OutlineSize = OutlineSize,
            PositionEpsilon = PositionEpsilonProvider?.Invoke() ?? 0.0f
        };

        GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
        GL.StencilMask(0xFF);
        foreach (var gameObject in gameObjects)
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

            gameObject.RenderStrategy.Render(renderContext, gameObject.Model);

            if (SelectionManager is not null && gameObject == SelectionManager.SelectedObject)
            {
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            }
        }

        // Handle outline for selection
        if (SelectionManager?.SelectedObject is not null)
        {
            GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
            GL.StencilMask(0x00);
            if (DrawSelectedObjectWithoutDepthTesting)
            {
                GL.Disable(EnableCap.DepthTest);
            }

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

            if (SelectionManager.SelectedObject is IHasRenderStrategy renderable)
            {
                renderable.RenderStrategy.DrawOutline(renderContext, renderable.Model);
            }

            GL.StencilMask(0xFF);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.Enable(EnableCap.DepthTest);
        }

        GL.Disable(EnableCap.StencilTest);
    }

    public void RenderGizmo()
    {
        InteractionManager.ActiveGizmo?.Render(CamerasManager.CurrentCamera);
    }

    /// <summary>
    /// Renders an outline around the specified object.
    /// </summary>
    public void RenderObjectOutline(
        object obj,
        Vector4 color,
        float scaleFactor = OutlineSize,
        bool drawInvisible = false)
    {
        if (obj is IHasRenderStrategy renderable)
        {
            var renderContext = new RenderContext
            {
                Camera = CamerasManager.CurrentCamera,
                DefaultShader = BasicShader,
                OutlineShader = OutlineShader,
                PbrShader = PbrShader,
                EnvironmentMap = EnvironmentMap,
                LightsManager = LightsManager,
                OutlineSize = scaleFactor,
                OutlineColor = color,
                PositionEpsilon = PositionEpsilonProvider?.Invoke() ?? 0.0f
            };
            renderable.RenderStrategy.DrawOutline(renderContext, renderable.Model);
        }
    }

    /// <summary>
    /// Renders the hover indicator for the drag manager to show which object can be dragged.
    /// Only shows when: drag manager is enabled, an object is hovered, not currently dragging,
    /// and the object is not selected for gizmo manipulation.
    /// </summary>
    public void RenderDragHoverIndicator()
    {
        if (InteractionManager.StaticDragManager.Enabled != true ||
            !InteractionManager.StaticDragManager.ShowHoverIndicator ||
            InteractionManager.StaticDragManager.HoverTarget == null ||
            InteractionManager.StaticDragManager.IsDragging)
            return;

        // Don't show the hover indicator if the object is selected for gizmo manipulation
        if (SelectionManager.SelectedObject != null &&
            SelectionManager.SelectedObject == InteractionManager.StaticDragManager.HoverTarget)
            return;

        GL.Disable(EnableCap.DepthTest);

        RenderObjectOutline(InteractionManager.StaticDragManager.HoverTarget,
            InteractionManager.StaticDragManager.HoverIndicatorColor,
            InteractionManager.StaticDragManager.HoverIndicatorScale, drawInvisible: true);

        GL.Enable(EnableCap.DepthTest);
    }

    public void RenderSelectedObjectOnTop()
    {
        if (DrawSelectedObjectWithoutDepthTesting &&
            InteractionManager.SelectionManager.SelectedObject is IHasRenderStrategy renderable)
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // Use RenderStrategy for this too
            var renderContext = new RenderContext
            {
                Camera = CamerasManager.CurrentCamera,
                DefaultShader = BasicShader,
                OutlineShader = OutlineShader,
                PbrShader = PbrShader,
                EnvironmentMap = EnvironmentMap,
                LightsManager = LightsManager,
                OutlineSize = OutlineSize,
                PositionEpsilon = PositionEpsilonProvider?.Invoke() ?? 0.0f
            };

            renderable.RenderStrategy.Render(renderContext, renderable.Model);
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

    public void Dispose()
    {
        _cube.Dispose();
        PbrShader.Dispose();
        BasicShader.Dispose();
        OutlineShader.Dispose();
        SkyboxShader.Dispose();
        EquirectangularToCubemapShader.Dispose();

        LightsManager.Dispose();

        // Note: GameObjects are owned by the application/demo, not by SceneRenderer
        // Note: InteractionManager is owned by the application/demo, not by SceneRenderer
    }
}