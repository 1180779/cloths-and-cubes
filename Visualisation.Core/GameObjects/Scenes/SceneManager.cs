using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.EnvironmentMaps;
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
    }

    public readonly Shader PbrShader = new("scenePBRShader.vert",  "scenePBRShader.frag");

    public readonly Shader EquirectangularToCubemapShader =
        new("cubemap.vert", "equirectangularToCubemapShader.frag");

    public readonly Shader IrradianceConvolutionShader = new("cubemap.vert", "irradianceConvolutionShader.frag");
    public readonly Shader PrefilterShader = new("cubemap.vert", "prefilterShader.frag");
    
    public readonly Shader SkyboxShader = new("sceneSkyboxShader.vert", "sceneSkyboxShader.frag");

    public readonly Shader BrdfLutShader = new("depthMapShader.vert", "brdfLUTShader.frag");
    private const string Hdr = "Hdr/symmetrical_garden_02_4k.exr";
    public EnvironmentMap EnvironmentMap { get; set; }
    private SphereMesh _cube = new();

    public LightsManager LightsManager { get; private set; }
    public CamerasManager CamerasManager { get; private set; }
    protected List<GameObject> _gameObjects = [];
    public ICollection<GameObject> GameObjects => _gameObjects;

    public void AddGameObject(GameObject gameObject)
    {
        _gameObjects.Add(gameObject);
    }

    public void RemoveGameObject(GameObject gameObject)
    {
        _gameObjects.Remove(gameObject);
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

    public void RenderSceneWindow(int screenWidth, int screenHeight, IBindable framebuffer)
    {
        LightsManager.RenderShadowsToMaps(_gameObjects);

        /* clear before rendering */
        framebuffer.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // TODO:
        // environment rendering is commented out for now
        // for the cases when the environment map is not present
        // in that case the temporary texture would be displayed instead
        // which is not desired. The environment map 
        // will be revisited later to allow for creation of single color environment map from scratch
        /* render environment first */
        GL.DepthFunc(DepthFunction.Lequal);
        // SkyboxShader.Use();
        // SkyboxShader.SetMatrix4("view", CamerasManager.CurrentCamera.ViewMatrix);
        // SkyboxShader.SetMatrix4("projection", CamerasManager.CurrentCamera.ProjectionMatrix);
        // EnvironmentMap.SetForSkyBoxShader(SkyboxShader);
        // _cube.Render();

        /* render every other object */
        PbrShader.Use();
        EnvironmentMap.SetForPbrShader(PbrShader);
        CamerasManager.CurrentCamera.SetForShader(PbrShader);
        LightsManager.SetForShader(PbrShader);
        foreach (var gameObject in _gameObjects)
        {
            gameObject.SetForShader(PbrShader);
            gameObject.Render();
        }
    }

    public void Dispose()
    {
        _cube.Dispose();
        PbrShader.Dispose();
        SkyboxShader.Dispose();
        EquirectangularToCubemapShader.Dispose();

        LightsManager.Dispose();
        foreach (var gameObject in _gameObjects)
        {
            gameObject.Dispose();
        }
    }
}