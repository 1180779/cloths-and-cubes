using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects.Scenes;

public abstract class SceneManager : IDisposable
{
    public SceneManager(float aspectRatio)
    {
        CamerasManager = new();
        LightsManager = new(CamerasManager);

        // TODO: remove magic numbers
        var camera = new FreeMovingCamera(aspectRatio)
        {
            Position = new(-6.5f, 3.2f, 6.6f),
            PitchDegrees = 6.3f,
            YawDegrees = -777.8f,
        };
        CamerasManager.AddCamera(camera);
    }

    private const string VertexShader = "phongShader.vert";
    private const string FragmentShader = "phongShader.frag";
    public readonly Shader Shader = new(VertexShader, FragmentShader);

    private const string EquirectangularToCubemapVertexShader = "equirectangularToCubemapShader.vert";
    private const string EquirectangularToCubemapFragmentShader = "equirectangularToCubemapShader.frag";

    public readonly Shader EquirectangularToCubemapShader =
        new(EquirectangularToCubemapVertexShader, EquirectangularToCubemapFragmentShader);

    private const string Hdr = "HDR_blue_nebulae-1.hdr";
    public EnvironmentMap EnvironmentMap { get; set; } = new(Hdr);
    private Cube cube = new();

    public LightsManager LightsManager { get; private set; }
    public CamerasManager CamerasManager { get; private set; }
    protected List<IVisualObject> gameObjects = [];
    public ICollection<IVisualObject> GameObjects => gameObjects;

    public void AddGameObject(IVisualObject gameObject)
    {
        gameObjects.Add(gameObject);
    }

    public void RemoveGameObject(IVisualObject gameObject)
    {
        gameObjects.Remove(gameObject);
    }

    public abstract void SetUp();

    public void ProcessInputOutOfFocus(IInputProvider input, float dt)
    {
        CamerasManager.ProcessInputOutOfFocus(input, dt);
    }

    public void ProcessInput(IInputProvider input, float dt)
    {
        CamerasManager.ProcessInput(input, dt);
    }

    public virtual void RenderSceneWindow(int screenWidth, int screenHeight, IBindable framebuffer)
    {
        LightsManager.RenderShadowsToMaps(gameObjects);

        /* clear before rendering */
        framebuffer.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        /* render environment first */
        EquirectangularToCubemapShader.Use();
        EquirectangularToCubemapShader.SetMatrix4("view", CamerasManager.CurrentCamera.ViewMatrix);
        EquirectangularToCubemapShader.SetMatrix4("projection", CamerasManager.CurrentCamera.ProjectionMatrix);
        EnvironmentMap.SetForShader(EquirectangularToCubemapShader);
        cube.Render();

        /* render every other object */
        Shader.Use();
        CamerasManager.CurrentCamera.SetForShader(Shader);
        LightsManager.SetForShader(Shader);
        foreach (var gameObject in gameObjects)
        {
            gameObject.SetForShader(Shader);
            gameObject.Render();
        }
    }

    public void Init(IInputProvider input)
    {
        cube.Init();
        LightsManager.Init();
        CamerasManager.Init(input);

        foreach (var gameObject in gameObjects)
        {
            gameObject.Init();
        }
    }

    public void Dispose()
    {
        cube.Dispose();
        Shader.Dispose();
        EquirectangularToCubemapShader.Dispose();

        LightsManager.Dispose();
        foreach (var gameObject in gameObjects)
        {
            gameObject.Dispose();
        }
    }
}