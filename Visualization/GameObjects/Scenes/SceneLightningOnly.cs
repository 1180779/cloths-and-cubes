using Visualization.Display;
using Visualization.Display.Light;
using Visualization.Display.Objects;

namespace Visualization.Scenes;

public class SceneLightningOnly : SceneManager
{
    public SceneLightningOnly(float aspectRatio) : base(aspectRatio)
    {
    }

    public override void SetUp()
    {
        LightSpotlight spotlight = new()
        {
            Position = new Vector3(0.0f, 0.0f, 3.0f),
            Direction = new Vector3(0.0f, 0.0f, -1.0f),
        };
        LightsManager.Spotlights.Add(spotlight);

        LightDirectional lightDirectional = new()
        {
            Direction = new Vector3(0.0f, -1.0f, 0.0f),
        };
        LightsManager.DirectionalLights.Add(lightDirectional);

        LightPoint pointLight = new()
        {
            Position = new Vector3(-3.8f, -4.0f, -12.3f),
        };
        LightsManager.PointLights.Add(pointLight);
    }
}