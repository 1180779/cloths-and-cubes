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

    public readonly Shader PbrShader = new("scenePBRShader.vert", "scenePBRShader.frag");
    public readonly Shader SolidColorShader = new("sceneBasicShader.vert", "sceneBasicShader.frag");

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

    public SelectionManager? SelectionManager { get; set; }
    public Vector3 SelectionColor = new(0.0f, 1.0f, 0.0f);

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

            gameObject.SetForShader(PbrShader);
            gameObject.Render(SelectionManager?.DrawInvisibleObjects ?? false);

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

            SolidColorShader.Use();
            CamerasManager.CurrentCamera.SetForSimpleShader(SolidColorShader);
            SolidColorShader.SetVector3("color", SelectionColor);

            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            var selectedObject = SelectionManager.SelectedObject;
            switch (selectedObject)
            {
                case Box box:
                    {
                        var originalHalfSize = box.EngineBox.HalfSize;
                        box.EngineBox.HalfSize *= 1.05f;

                        box.SetForShaderNoMaterial(SolidColorShader);
                        box.Render(SelectionManager.DrawInvisibleObjects);

                        box.EngineBox.HalfSize = originalHalfSize;
                        break;
                    }
                case Ball ball:
                    {
                        var originalRadius = ball.EngineBall.Radius;
                        ball.EngineBall.Radius *= 1.05f;

                        ball.SetForShaderNoMaterial(SolidColorShader);
                        ball.Render(SelectionManager.DrawInvisibleObjects);

                        ball.EngineBall.Radius = originalRadius;
                        break;
                    }
            }

            GL.StencilMask(0xFF);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.Enable(EnableCap.DepthTest);
            
            if (SelectionManager.DrawSelectedObjectWithoutDepthTesting && SelectionManager.SelectedObject is GameObject gameObject)
            {
                GL.Clear(ClearBufferMask.DepthBufferBit);
                gameObject.SetForShader(PbrShader);
                gameObject.Render(SelectionManager?.DrawInvisibleObjects ?? false);
            }
        }

        GL.Disable(EnableCap.StencilTest);
    }

    private void RenderSkybox()
    {
        GL.DepthFunc(DepthFunction.Lequal);
        SkyboxShader.Use();
        SkyboxShader.SetMatrix4("view", CamerasManager.CurrentCamera.ViewMatrix);
        SkyboxShader.SetMatrix4("projection", CamerasManager.CurrentCamera.ProjectionMatrix);
        EnvironmentMap.SetForSkyBoxShader(SkyboxShader);
        _cube.Render();
        GL.DepthFunc(DepthFunction.Less); // Reset
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
        SolidColorShader.Dispose();
        SkyboxShader.Dispose();
        EquirectangularToCubemapShader.Dispose();

        LightsManager.Dispose();
        foreach (var gameObject in _gameObjects)
        {
            gameObject.Dispose();
        }
    }
}