using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display.Light;

public class LightsManager : IDisposable
{
    public LightsManager(CamerasManager camerasManager)
    {
        this.camerasManager = camerasManager;
    }

    private readonly CamerasManager camerasManager;

    private Shader DirectionalShadowDepthShader { get; } =
        new("shadowDepthMapShader.vert", "shadowDepthMapShader.frag", "shadowDepthMapShader.geom");

    public const int MaxSpotlights = 4;
    public const int MaxPointlights = 4;

    private List<LightPoint> pointLights = new(MaxPointlights);
    private List<LightSpotlight> spotlights = new(MaxSpotlights);

    private LightDirectional? directionalLight;

    public Func<CameraBase> GetCurrentCameraFunc => () => camerasManager.CurrentCamera;

    public LightDirectional? DirectionalLight
    {
        get => directionalLight;
        set
        {
            directionalLight = value;
            if (directionalLight != null)
                directionalLight.GetCurrentCamera = () => camerasManager.CurrentCamera;
        }
    }

    public ICollection<LightPoint> PointLights => pointLights;

    public void AddPointLight(LightPoint light)
    {
        if (pointLights.Count == MaxPointlights)
        {
            throw new Exception($"Too many point lights (max: {MaxPointlights})");
        }

        pointLights.Add(light);
    }

    public void RemovePointLight(LightPoint light)
    {
        pointLights.Remove(light);
    }

    public ICollection<LightSpotlight> Spotlights => spotlights;

    public void AddSpotlight(LightSpotlight light)
    {
        if (spotlights.Count == MaxSpotlights)
        {
            throw new Exception($"Too many spotlights (max: {MaxSpotlights})");
        }

        spotlights.Add(light);
    }

    public void RemoveSpotlight(LightSpotlight light)
    {
        spotlights.Remove(light);
    }

    public LightSpotlight? Flashlight { get; private set; }

    public bool FlashlightOn = false;
    public bool Day = true;

    public void RenderShadowsToMaps(ICollection<GameObject> objects)
    {
        if (DirectionalLight is null) return;
        GL.CullFace(TriangleFace.Front);
        DirectionalLight.RenderToDepthMap(DirectionalShadowDepthShader, objects);
        GL.CullFace(TriangleFace.Back);
    }

    public void Dispose()
    {
        if (DirectionalLight is not null)
        {
            DirectionalLight.Dispose();
        }

        DirectionalShadowDepthShader.Dispose();
    }


    public void SetForShader(Shader sh)
    {
        if (Day)
        {
            if (DirectionalLight is not null)
            {
                DirectionalLight.SetForShader(sh, $"lightD");
                sh.SetInt("lightDCount", 1);
            }
        }

        int lightSCount = spotlights.Count;
        if (FlashlightOn && Flashlight != null)
        {
            /* add flashlight to spotlight if set */
            Flashlight.SetForShader(sh, $"lightS[{spotlights.Count}]");
            lightSCount++;
        }

        sh.SetInt("lightSCount", lightSCount);
        for (int i = 0; i < spotlights.Count; ++i)
        {
            spotlights[i].SetForShader(sh, $"lightS[{i}]");
        }

        sh.SetInt("lightPCount", pointLights.Count);
        for (int i = 0; i < pointLights.Count; ++i)
        {
            pointLights[i].SetForShader(sh, $"lightP[{i}]");
        }
    }
}