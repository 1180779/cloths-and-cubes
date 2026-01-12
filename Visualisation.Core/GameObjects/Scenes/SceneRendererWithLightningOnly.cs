using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.ContactGenerators;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Light;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects.Scenes;

public class SceneRendererWithLightningOnly : SceneRenderer
{
    public SceneRendererWithLightningOnly(
        float aspectRatio,
        IInputProvider inputProvider,
        Func<IEnumerable<GameObject>> getGameObjects,
        Func<CameraBase> cameraProvider,
        Func<BVH> bvhProvider,
        Func<Dictionary<int, IBoxable>> bvhDictionaryProvider,
        Func<Dictionary<Engine.Cloth, Cloth>> clothsProvider,
        Func<Plane> planeProvider,
        Func<float> positionEpsilonProvider,
        Func<IEnumerable<Box>> boxesProvider,
        Func<GlobalJointsList> globalJointsProvider)
        : base(aspectRatio, inputProvider, getGameObjects, cameraProvider, bvhProvider,
            bvhDictionaryProvider, clothsProvider, planeProvider, positionEpsilonProvider, boxesProvider,
            globalJointsProvider)
    {
    }

    public override void SetUp()
    {
        LightDirectional lightDirectional2 = new(LightsManager.GetCurrentCameraFunc)
        {
            Direction = new Vector3(0.0f, -1.0f, -1.0f),
        };
        LightsManager.DirectionalLight = lightDirectional2;

        FollowingCamera followingCamera = new(() => _getGameObjects().ToArray());
        CamerasManager.AddCamera(followingCamera);
    }
}