using Visualisation.Core.Display.VisualObjects;
using Visualisation.Core.Inputs;
using Visualization.Display;
using Visualization.Display.Cameras;
using Visualization.Display.Light;

namespace Visualisation.Core.GameObjects.Scenes;

public abstract class SceneManager
{
    public SceneManager(float aspectRatio)
    {
        // TODO: remove magic numbers
        var camera = new FreeMovingCamera(aspectRatio)
        {
            Position = new(-6.5f, 3.2f, 6.6f),
            Pitch = 6.3f,
            Yaw = -777.8f,
        };
        CamerasManager.AddCamera(camera);
    }

    private const string VertexShader = "phongShader.vert";
    private const string FragmentShader = "phongShader.frag";
    public readonly Shader Shader = new(VertexShader, FragmentShader);

    public LightsManager LightsManager { get; private set; } = new();
    public CamerasManager CamerasManager { get; private set; } = new();
    protected List<IVisualObject> gameObjects = [];
    public IEnumerable<IVisualObject> GameObjects => gameObjects;

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

    public virtual void RenderSceneWindow()
    {
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
        CamerasManager.Init(input);

        foreach (var gameObject in gameObjects)
        {
            gameObject.Init();
        }
    }

    public void Dispose()
    {
        foreach (var gameObject in gameObjects)
        {
            gameObject.Dispose();
        }
    }
}