using Visualization.Display;
using Visualization.Display.Light;
using Visualization.Display.Objects;

namespace Visualization.Scenes;

public abstract class SceneManager
{
    public SceneManager(float aspectRatio)
    {
        // TODO: remove magic numbers
        Camera = new Camera(aspectRatio)
        {
            Position = new(-6.5f, 3.2f, 6.6f),
            Pitch = 6.3f,
            Yaw = -777.8f,
        };
    }

    private const string VertexShader = "phongShader.vert";
    private const string FragmentShader = "phongShader.frag";
    public readonly Shader Shader = new(VertexShader, FragmentShader);

    public LightsManager LightsManager { get; private set; }= new();
    public Camera Camera { get; private set; }
    public List<IVisualObject> GameObjects { get; private set; } = [];

    public abstract void SetUp();

    public void Render()
    {
        Shader.Use();
        Camera.SetForShader(Shader);
        LightsManager.SetForShader(Shader);
        foreach (var gameObject in GameObjects)
        {
            gameObject.SetForShader(Shader);
            gameObject.Render();
        }
    }

    public void Init()
    {
        foreach (var gameObject in GameObjects)
        {
            gameObject.Init();
        }
    }

    public void Dispose()
    {
        foreach (var gameObject in GameObjects)
        {
            gameObject.Dispose();
        }
    }
}