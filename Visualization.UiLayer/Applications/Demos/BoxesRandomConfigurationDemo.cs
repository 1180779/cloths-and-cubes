using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesRandomConfigurationDemo : BoxesDemo
{
    private const int BoxesCount = 0;
    private const int BallsCount = 20;

    protected override void InitializeScene()
    {
        Boxes = new Box[BoxesCount];
        Balls = new Ball[BallsCount];

        /* add the cubes to the scene to be rendered */
        for (var i = 0; i < Boxes.Length; i++)
        {
            this.Boxes[i] = new Box();
            var box = Boxes[i];
            Scene.AddGameObject(box);
        }

        /* add the spheres to the scene to be rendered */
        for (var i = 0; i < Balls.Length; ++i)
        {
            this.Balls[i] = new Ball();
            var ball = Balls[i];
            Scene.AddGameObject(ball);
        }

        /* add ground plane to the scene */
        Plane = new();
        Scene.AddGameObject(Plane);

        /* set everything up */
        Reset();
    }
}