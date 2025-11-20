using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects.Scenes;

public class SceneLightningOnly : SceneManager
{
    public SceneLightningOnly(float aspectRatio) : base(aspectRatio)
    {
    }

    public override void SetUp()
    {
        // LightSpotlight spotlight = new()
        // {
        //     Position = new Vector3(0.0f, 0.0f, 3.0f),
        //     Direction = new Vector3(0.0f, 0.0f, -1.0f),
        // };
        // LightsManager.Spotlights.Add(spotlight);

        LightDirectional lightDirectional2 = new()
        {
            Direction = new Vector3(0.0f, -1.0f, -1.0f),
        };
        LightsManager.DirectionalLight = lightDirectional2;

        // LightPoint pointLight = new()
        // {
        //     Position = new Vector3(-3.8f, -4.0f, -12.3f),
        // };
        // LightsManager.PointLights.Add(pointLight);

        FollowingCamera followingCamera = new(CamerasManager.CurrentCamera.AspectRatio);
        followingCamera.AttachTo(GameObjects
            .Select<IVisualObject, Func<AbstractVisualObject>>(g => () => g.AbstractVisualObject).ToArray());
        CamerasManager.AddCamera(followingCamera);
    }
}