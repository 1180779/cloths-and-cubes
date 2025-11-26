using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display.Light;

public class LightsManager : IDisposable
{
    public LightsManager(CamerasManager camerasManager)
    {
        this._camerasManager = camerasManager;
    }

    private readonly CamerasManager _camerasManager;

    private Shader DirectionalShadowDepthShader { get; } =
        new("shadowDepthMapShader.vert", "shadowDepthMapShader.frag", "shadowDepthMapShader.geom");

    public const int MaxSpotlights = 4;
    public const int MaxPointlights = 4;

    private List<LightPoint> _pointLights = new(MaxPointlights);
    private List<LightSpotlight> _spotlights = new(MaxSpotlights);

    private LightDirectional? _directionalLight;

    public Func<CameraBase> GetCurrentCameraFunc => () => _camerasManager.CurrentCamera;

    public LightDirectional? DirectionalLight
    {
        get => _directionalLight;
        set
        {
            _directionalLight = value;
            if (_directionalLight != null)
                _directionalLight.GetCurrentCamera = () => _camerasManager.CurrentCamera;
        }
    }

    public ICollection<LightPoint> PointLights => _pointLights;

    public void AddPointLight(LightPoint light)
    {
        if (_pointLights.Count == MaxPointlights)
        {
            throw new Exception($"Too many point lights (max: {MaxPointlights})");
        }

        _pointLights.Add(light);
    }

    public void RemovePointLight(LightPoint light)
    {
        _pointLights.Remove(light);
    }

    public ICollection<LightSpotlight> Spotlights => _spotlights;

    public void AddSpotlight(LightSpotlight light)
    {
        if (_spotlights.Count == MaxSpotlights)
        {
            throw new Exception($"Too many spotlights (max: {MaxSpotlights})");
        }

        _spotlights.Add(light);
    }

    public void RemoveSpotlight(LightSpotlight light)
    {
        _spotlights.Remove(light);
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

        int lightSCount = _spotlights.Count;
        if (FlashlightOn && Flashlight != null)
        {
            /* add flashlight to spotlight if set */
            Flashlight.SetForShader(sh, $"lightS[{_spotlights.Count}]");
            lightSCount++;
        }

        sh.SetInt("lightSCount", lightSCount);
        for (int i = 0; i < _spotlights.Count; ++i)
        {
            _spotlights[i].SetForShader(sh, $"lightS[{i}]");
        }

        sh.SetInt("lightPCount", _pointLights.Count);
        for (int i = 0; i < _pointLights.Count; ++i)
        {
            _pointLights[i].SetForShader(sh, $"lightP[{i}]");
        }
    }
}