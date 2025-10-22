using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Visualisation.Core;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Texture;
using Visualisation.Core.FrameCapsule;
using Visualisation.Core.GameObjects.Scenes;
using Visualisation.Core.Inputs;
using Visualization.UiLayer.Inputs;
using Visualization.UiLayer.UI;
using Visualization.UiLayer.UI.Windows;

namespace Visualization.UiLayer.Applications;

public class Application : GameWindow
{
    private readonly ImGuiController imGuiController;
    protected readonly IInputProvider InputProvider;
    protected readonly WindowFrameBuffer SceneRenderWindowFrb;
    protected readonly SceneManager Scene;

#if DEBUG
    protected readonly QuadMesh QuadMesh;
    protected readonly Shader QuadShader;
    protected readonly WindowFrameBuffer DepthMapWindowFrb;
    protected readonly FrameSaver FrameSaver = new(1000);
    private readonly ShadowSettingsWindow shadowSettingsWindow;
#endif

    protected const int DefaultWidth = 800;
    protected const int DefaultHeight = 600;
    protected const string DefaultTitle = "Display";

    public Application(int width = DefaultWidth, int height = DefaultHeight, string title = DefaultTitle) : base(
        GameWindowSettings.Default,
        new NativeWindowSettings())
    {
        Size = (width, height);
        Title = title;

        imGuiController = new ImGuiController(this);
        imGuiController.HookToWindow(this);
        InputProvider = new ImGuiInputProvider(this, imGuiController);

        SceneRenderWindowFrb = new(width, height);
#if DEBUG
        QuadMesh = new();
        DepthMapWindowFrb = new(width, height);
        QuadShader = new("depthMapShader.vert", "depthMapShader.frag");
        // windows
        shadowSettingsWindow = new ShadowSettingsWindow(() => this.Scene.LightsManager.DirectionalLight);
#endif

        Scene = new SceneLightningOnly(Size.X / (float)Size.Y);

        // GL
        GL.ClearColor(0.2f, 0.3f, 0.5f, 1f);
        GL.Enable(EnableCap.DepthTest);
    }

    protected bool StepsLimit { get; set; }
    protected long AvailableSteps { get; set; }

    protected bool DoUpdate
    {
        get
        {
            if (!StepsLimit) return true;
            if (AvailableSteps <= 0) return false;
            AvailableSteps--;
            return true;
        }
    }

    protected virtual void Update(float deltaTime)
    {
        if (!DoUpdate)
            return;
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        InputProvider.UpdateMousePosition();

        if (!IsFocused)
        {
            return;
        }

        if (InputProvider.IsKeyPressed(InputKey.Equal))
        {
            DirectionalLightLayer++;
        }

        if (InputProvider.IsKeyPressed(InputKey.Minus))
        {
            DirectionalLightLayer--;
        }
    }

    /// <summary>
    /// This method is called after doing some initial setup.
    /// It can overriden to add game objects to the initial scene. 
    /// </summary>
    protected virtual void InitializeScene()
    {
    }


    protected override void OnLoad()
    {
        base.OnLoad();
        InitializeScene();

        // set up internal scene objects after the scene is initialized
        // this way some objects are already in the scene and can be accessed
        // during the scene setup (e.g. attach camera to an object from demo)
        Scene.SetUp();
        Scene.Init(InputProvider);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        imGuiController.Update((float)args.Time);

        Update((float)args.Time); // Update game/app logic before any rendering

        // --- ImGui Docking Setup ---
        ImGuiWindowFlags dockspaceFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground;
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));
        ImGui.Begin("DockSpace Host", dockspaceFlags);
        ImGui.PopStyleVar(3);

        uint dockspaceId = ImGui.GetID("MyDockSpace");
        ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);

        // draw windows here
        HelpWindow.Draw();
#if DEBUG
        RenderObjectInspectorWindow();
        ShadowCascadingMapsWindow();
        shadowSettingsWindow.Render();
#endif
        RenderSceneWindow((float)args.Time);

        // end dockspace
        ImGui.End();

        // clear the screen underneath the imGui windows (now background only)
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        imGuiController.Render();

        SwapBuffers();

        // upload any finished texture loads to the openGL
        TexturesManager.ProcessPendingUploads();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);

        TexturesManager.AbortAllLoads();

        imGuiController.UnhookFromWindow(this);
        SceneRenderWindowFrb.Dispose();
#if DEBUG
        QuadMesh.Dispose();
        DepthMapWindowFrb.Dispose();
#endif
        Scene.Dispose();

        base.OnUnload();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!ImGui.GetIO().WantCaptureMouse && Scene.CamerasManager.CameraMode)
        {
            Scene.CamerasManager.CurrentCamera.FovDegrees -= InputProvider.GetMouseScroll();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    private int directionalLightLayer;

    private int DirectionalLightLayer
    {
        get => directionalLightLayer;
        set
        {
            if (Scene.LightsManager.DirectionalLight is null)
                return;

            directionalLightLayer = value % (Scene.LightsManager.DirectionalLight.ShadowCascadeLevels.Length + 1);
            if (directionalLightLayer < 0)
            {
                directionalLightLayer += (Scene.LightsManager.DirectionalLight.ShadowCascadeLevels.Length + 1);
            }
        }
    }

    private void RenderSceneWindow(float dt)
    {
        ImGui.Begin("Game Viewport");

        bool isWindowHovered = ImGui.IsWindowHovered();
        if (isWindowHovered)
        {
            Scene.ProcessInput(InputProvider, dt);
        }
        else
        {
            Scene.ProcessInputOutOfFocus(InputProvider, dt);
        }

        System.Numerics.Vector2 viewportSize = ImGui.GetContentRegionAvail();
        var fbScale = imGuiController.ScaleFactor;
        int fbW = Math.Max(1, (int)Math.Round(viewportSize.X * fbScale.X));
        int fbH = Math.Max(1, (int)Math.Round(viewportSize.Y * fbScale.Y));
        SceneRenderWindowFrb.Resize(fbW, fbH);
        Scene.RenderSceneWindow(fbW, fbH, SceneRenderWindowFrb);

        SceneRenderWindowFrb.Unbind();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        ImGui.Image(SceneRenderWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
            new System.Numerics.Vector2(1, 0));
        ImGui.End();
    }

/* debug code */
#if DEBUG
    private void RenderObjectInspectorWindow()
    {
        if (Scene.CamerasManager.CurrentCamera is FollowingCamera followingCamera)
        {
            var o = Scene.GameObjects.First(g => g.AbstractVisualObject == followingCamera.TargetObject);
            ObjectInspectorWindow.Draw([o.PhysicsObject]);
        }
        else
        {
            ObjectInspectorWindow.Draw(Scene.GameObjects.Select(g => g.PhysicsObject).ToArray());
        }
    }

    private void ShadowCascadingMapsWindow()
    {
        ImGui.Begin("Cascading Depth Maps");

        System.Numerics.Vector2 viewportSize = ImGui.GetContentRegionAvail();
        var fbScale = imGuiController.ScaleFactor;
        int fbWidth = Math.Max(1, (int)Math.Round(viewportSize.X * fbScale.X));
        int fbHeight = Math.Max(1, (int)Math.Round(viewportSize.Y * fbScale.Y));
        DepthMapWindowFrb.Resize(fbWidth, fbHeight);

        DepthMapWindowFrb.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        QuadShader.Use();
        Scene.LightsManager.DirectionalLight?.SetForDepthTextureShader(QuadShader, DirectionalLightLayer);
        QuadMesh.Render();

        DepthMapWindowFrb.Unbind();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        ImGui.Image(DepthMapWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
            new System.Numerics.Vector2(1, 0)); // Flipped Y for OpenGL texture
        ImGui.End();
    }
#endif
}