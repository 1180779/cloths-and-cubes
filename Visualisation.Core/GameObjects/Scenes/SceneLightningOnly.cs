using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects.Scenes;

public class SceneLightningOnly : SceneManager
{
    public SceneLightningOnly(float aspectRatio, IInputProvider inputProvider) : base(aspectRatio, inputProvider)
    {
    }

    public override void SetUp()
    {
        LightDirectional lightDirectional2 = new(LightsManager.GetCurrentCameraFunc)
        {
            Direction = new Vector3(0.0f, -1.0f, -1.0f),
        };
        LightsManager.DirectionalLight = lightDirectional2;

        FollowingCamera followingCamera = new();
        followingCamera.AttachTo(GameObjects.ToArray());
        CamerasManager.AddCamera(followingCamera);
    }
}