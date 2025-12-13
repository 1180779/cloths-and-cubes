using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Visualisation.Core;
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
    protected readonly SceneManager _sceneManager;
    protected readonly SettingsSaverLoader _settingsSaverLoader;

    protected readonly SceneWindow _sceneWindow;
    protected readonly StatsWindow _statsWindow;
#if DEBUG
    protected readonly CascadingShadowMapsWindow _cascadingShadowMapsWindow;
    protected readonly ObjectInspectorWindow _objectInspectorWindow;
    private readonly GraphicsSettingsWindow _graphicsSettingsWindow;
    
#if FRAMESAVER
    protected readonly FrameSaver FrameSaver = new(0);
#endif
#endif

    protected const int DefaultWidth = 800;
    protected const int DefaultHeight = 600;
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

        _sceneManager = new SceneLightningOnly(Size.X / (float)Size.Y, _inputProvider);
        _sceneWindow = new SceneWindow(_imGuiController, _sceneManager, _inputProvider, Size);
        _statsWindow = new StatsWindow(_sceneManager);
        _sceneWindow.DebugRenderInScene += DebugRenderInScene;
        
        _settingsSaverLoader = new SettingsSaverLoader();
#if DEBUG
        _cascadingShadowMapsWindow = new(_imGuiController, _inputProvider, _sceneManager, Size);
        _objectInspectorWindow = new(_sceneManager);
        
        // windows
        _graphicsSettingsWindow = new GraphicsSettingsWindow(() => this._sceneManager.LightsManager.DirectionalLight, _sceneManager, _sceneWindow);
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
        // if (!DoUpdate)
        //     return;
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        _inputProvider.UpdateMousePosition();

        // if (!IsFocused)
        // {
        //     return;
        // }
        
#if DEBUG
        _cascadingShadowMapsWindow.HandleInput();
#endif
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
        _sceneManager.SetUp();
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
        _sceneManager.Dispose();
        _sceneWindow.Dispose();

        base.OnUnload();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!ImGui.GetIO().WantCaptureMouse && _sceneManager.CamerasManager.CameraMode)
        {
            _sceneManager.CamerasManager.CurrentCamera.FovDegrees -= _inputProvider.GetMouseScroll();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    protected virtual void RenderWindows(double dt)
    {
        HelpWindow.Draw();
        _statsWindow.Draw();
#if DEBUG
        _cascadingShadowMapsWindow.Draw();
        _objectInspectorWindow.Draw();
        _graphicsSettingsWindow.Render();
#endif
        _sceneWindow.Draw(FramebufferSize, (float)dt);
    }

    protected virtual void DebugRenderInScene(Shader sh)
    {

    }

    protected virtual ApplicationState SaveState()
    {
        return new ApplicationState
        {
#if DEBUG
            ShadowSettings = _graphicsSettingsWindow.SaveState(),
            CascadingShadowMaps = _cascadingShadowMapsWindow.SaveState()
#endif
        };
    }

    protected virtual void LoadState(ApplicationState state)
    {
#if DEBUG
        if (state.ShadowSettings is not null)
        {
            _graphicsSettingsWindow.RestoreState(state.ShadowSettings);
        }

        if (state.CascadingShadowMaps is not null)
        {
            _cascadingShadowMapsWindow.RestoreState(state.CascadingShadowMaps);
        }
#endif
    }
}