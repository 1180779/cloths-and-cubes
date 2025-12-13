using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Visualisation.Core;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Texture;
using Visualisation.Core.GameObjects.Scenes;
using Visualisation.Core.Inputs;

using Visualization.UiLayer.Inputs;
using Visualization.UiLayer.UI;
using Visualization.UiLayer.UI.Windows;

namespace Visualization.UiLayer.Applications;

public class Application : GameWindow
{
    private readonly ImGuiController _imGuiController;
    protected readonly IInputProvider _inputProvider;
    protected readonly WindowFrameBuffer _sceneRenderWindowFrb;
    protected readonly SceneManager _scene;
    protected readonly SettingsSaverLoader _settingsSaverLoader;

    protected readonly Shader _debugBasicShader;
    
#if DEBUG
    protected readonly CascadingShadowMapsWindow _cascadingShadowMapsWindow;
    protected readonly ObjectInspectorWindow _objectInspectorWindow;
#if FRAMESAVER
    protected readonly FrameSaver FrameSaver = new(0);
#endif
    private readonly ShadowSettingsWindow _shadowSettingsWindow;
#endif

    protected const int DefaultWidth = 8000;
    protected const int DefaultHeight = 6000;
    protected const string DefaultTitle = "Display";

    protected Application(int width = DefaultWidth, int height = DefaultHeight, string title = DefaultTitle) : base(
        GameWindowSettings.Default,
        new NativeWindowSettings { WindowState = WindowState.Maximized, })
    {
        Size = (width, height);
        Title = title;

        _imGuiController = new ImGuiController(this);
        _imGuiController.HookToWindow(this);
        _inputProvider = new ImGuiInputProvider(this, _imGuiController);

        _sceneRenderWindowFrb = new(width, height);

        _debugBasicShader = new("sceneBasicShader.vert", "sceneBasicShader.frag");
        
        _scene = new SceneLightningOnly(Size.X / (float)Size.Y, _inputProvider);
        _settingsSaverLoader = new SettingsSaverLoader();
#if DEBUG
        _cascadingShadowMapsWindow = new(_imGuiController, _inputProvider, _scene, Size);
        _objectInspectorWindow = new(_scene);
        
        // windows
        _shadowSettingsWindow = new ShadowSettingsWindow(() => this._scene.LightsManager.DirectionalLight);
#endif

        // GL
        GL.ClearColor(0.2f, 0.3f, 0.5f, 1f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.TextureCubeMapSeamless); /* for low mip levels of pre-filter convolution map */
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

        _inputProvider.UpdateMousePosition();

        if (!IsFocused)
        {
            return;
        }

        _cascadingShadowMapsWindow.HandleInput();
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
        var state = _settingsSaverLoader.Load();
        if (state is not null)
        {
            LoadState(state);
        }
        
        InitializeScene();

        // set up internal scene objects after the scene is initialized
        // this way some objects are already in the scene and can be accessed
        // during the scene setup (e.g. attach camera to an object from demo)
        _scene.SetUp();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _imGuiController.Update((float)args.Time);

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

        RenderWindows(args.Time);

        // end dockspace
        ImGui.End();

        // clear the screen underneath the imGui windows (now background only)
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _imGuiController.Render();

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
        var state = SaveState();
        _settingsSaverLoader.Save(state);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
#if DEBUG
        _cascadingShadowMapsWindow.Dispose();
#endif
        TexturesManager.AbortAllLoads();
        _imGuiController.UnhookFromWindow(this);
        _debugBasicShader.Dispose();
        _scene.Dispose();
        _sceneRenderWindowFrb.Dispose();

        base.OnUnload();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!ImGui.GetIO().WantCaptureMouse && _scene.CamerasManager.CameraMode)
        {
            _scene.CamerasManager.CurrentCamera.FovDegrees -= _inputProvider.GetMouseScroll();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    protected virtual void RenderWindows(double dt)
    {
        RenderEnvironmentMapWindow();
        HelpWindow.Draw();
        StatsWindow.Draw(_scene.CamerasManager.CurrentCamera.Position);
#if DEBUG
        _cascadingShadowMapsWindow.Draw();
        _objectInspectorWindow.Draw();
        _shadowSettingsWindow.Render();
#endif
        RenderSceneWindow((float)dt);
    }

    private void RenderEnvironmentMapWindow()
    {
        ImGui.Begin("Environment Map Settings");
        int currentDisplayType = (int)_scene.EnvironmentMap.DisplayType;
        ImGui.Text("Display Type:");
        ImGui.RadioButton("Skybox", ref currentDisplayType,
            (int)EnvironmentMap.EnvironmentMapDisplayType.EnvironmentCubemap);
        ImGui.SameLine();
        ImGui.RadioButton("Irradiance Map", ref currentDisplayType,
            (int)EnvironmentMap.EnvironmentMapDisplayType.IrradianceMap);
        ImGui.SameLine();
        ImGui.RadioButton("Prefiltered Map", ref currentDisplayType,
            (int)EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap);
        _scene.EnvironmentMap.DisplayType = (EnvironmentMap.EnvironmentMapDisplayType)currentDisplayType;

        if (_scene.EnvironmentMap.DisplayType == EnvironmentMap.EnvironmentMapDisplayType.PrefilterMap)
        {
            float roughness = _scene.EnvironmentMap.PrefilterMapValue;
            if (ImGui.SliderFloat("Roughness", ref roughness, 1.0f, 5.0f))
            {
                _scene.EnvironmentMap.PrefilterMapValue = roughness;
            }
        }

        ImGui.End();
    }

    protected virtual void DebugRenderInScene(Shader sh)
    {
        _debugBasicShader.Use();
        sh.SetMatrix4("view", _scene.CamerasManager.CurrentCamera.ViewMatrix);
        sh.SetMatrix4("projection", _scene.CamerasManager.CurrentCamera.ProjectionMatrix);
    }

    private void RenderSceneWindow(float dt)
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
        DebugRenderInScene(_debugBasicShader);
        
        _sceneRenderWindowFrb.Unbind();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        ImGui.Image(_sceneRenderWindowFrb.TextureId, viewportSize, new System.Numerics.Vector2(0, 1),
            new System.Numerics.Vector2(1, 0));
        ImGui.End();
    }

    protected virtual ApplicationState SaveState()
    {
        return new ApplicationState
        {
            ShadowSettings = _shadowSettingsWindow.SaveState(),
            CascadingShadowMaps = _cascadingShadowMapsWindow.SaveState()
        };
    }

    protected virtual void LoadState(ApplicationState state)
    {
        if (state.ShadowSettings is not null)
        {
            _shadowSettingsWindow.RestoreState(state.ShadowSettings);
        }

        if (state.CascadingShadowMaps is not null)
        {
            _cascadingShadowMapsWindow.RestoreState(state.CascadingShadowMaps);
        }
    }
}