using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesRandomConfigurationDemo : BoxesDemo
{
    private const int Boxes = 30;
    private const int Balls = 20;

    protected override void InitializeScene()
    {
        boxes = new Box[Boxes];
        balls = new Ball[Balls];

        /* add the cubes to the scene to be rendered */
        for (var i = 0; i < Boxes; i++)
        {
            this.boxes[i] = new Box();
            var box = boxes[i];
            Scene.AddGameObject(box);
        }

        /* add the spheres to the scene to be rendered */
        for (var i = 0; i < Balls; ++i)
        {
            this.balls[i] = new Ball();
            var ball = balls[i];
            Scene.AddGameObject(ball);
        }

        /* add ground plane to the scene */
        plane = new();
        Scene.AddGameObject(plane);

        /* set everything up */
        Reset();
    }
}