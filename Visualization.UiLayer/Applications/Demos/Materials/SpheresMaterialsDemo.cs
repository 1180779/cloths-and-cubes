using Visualisation.Core.Display.Materials;
using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public abstract class SpheresMaterialsDemo : BoxesDemo
{
    protected int NrOfRows = 5;

    protected abstract IMaterial[] GetMaterials();

    protected override void InitializeScene()
    {
        var materials = GetMaterials();
        balls = new Ball[materials.Length];
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            var ball = new Ball();
            var row = i / NrOfRows;
            var col = i % NrOfRows;
            ball.EngineBall.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3()
            );
            ball.VisualBall.Material = material;
            balls[i] = ball;
            Scene.AddGameObject(ball);
        }


        /* add ground plane to the scene */
        plane = new();
        Scene.AddGameObject(plane);

        /* set everything up */
        Reset();
    }

    protected override void Reset()
    {
        for (var i = 0; i < balls.Length; i++)
        {
            var ball = balls[i];
            var row = i / NrOfRows;
            var col = i % NrOfRows;
            ball.EngineBall.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3()
            );
        }

        // Reset the contacts
        CollisionData.ContactCount = 0;
    }
}