namespace Visualization.Display.Light;

public class LightsManager
{
    public List<LightPoint> PointLights = new();
    public List<LightSpotlight> Spotlights = new();
    public List<LightDirectional> DirectionalLights = new();

    public LightSpotlight? Flashlight { get; private set; }

    public bool Fog = false;
    public float FogDensity = 0.10f;
    public Vector3 FogColor = new(0.5f, 0.5f, 0.5f);
    public bool FlashlightOn = false;
    public bool Day = true;

    public void SetForShader(Shader sh)
    {
        sh.SetFloat("fogDensity", Fog ? FogDensity : 0.0f);
        sh.SetVector3("fogColor", FogColor);

        if (Day)
        {
            sh.SetInt("lightDCount", DirectionalLights.Count);
            for (int i = 0; i < DirectionalLights.Count; ++i)
            {
                DirectionalLights[i].SetForShader(sh, $"lightD[{i}]");
            }
        }
        else
        {
            sh.SetInt("lightDCount", 0);
        }

        int lightSCount = Spotlights.Count;
        if (FlashlightOn && Flashlight != null)
        {
            /* add flashlight to spotlight if set */
            Flashlight.SetForShader(sh, $"lightS[{Spotlights.Count}]");
            lightSCount++;
        }

        sh.SetInt("lightSCount", lightSCount);
        for (int i = 0; i < Spotlights.Count; ++i)
        {
            Spotlights[i].SetForShader(sh, $"lightS[{i}]");
        }

        sh.SetInt("lightPCount", PointLights.Count);
        for (int i = 0; i < PointLights.Count; ++i)
        {
            PointLights[i].SetForShader(sh, $"lightP[{i}]");
        }
    }
}