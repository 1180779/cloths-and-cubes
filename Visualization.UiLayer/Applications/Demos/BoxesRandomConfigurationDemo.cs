using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesRandomConfigurationDemo : BoxesDemo
{
    private const int BoxesCount = 100;
    private const int BallsCount = 0;

    protected override void InitializeScene()
    {
        _boxes = new Box[BoxesCount];
        _balls = new Ball[BallsCount];

        /* add the cubes to the scene to be rendered */
        for (var i = 0; i < _boxes.Length; i++)
        {
            this._boxes[i] = new Box();
            var box = _boxes[i];
            _scene.AddGameObject(box);
        }

        /* add the spheres to the scene to be rendered */
        for (var i = 0; i < _balls.Length; ++i)
        {
            this._balls[i] = new Ball();
            var ball = _balls[i];
            _scene.AddGameObject(ball);
        }

        /* add ground plane to the scene */
        _plane = new();
        // _scene.AddGameObject(_plane);

        /* add cloth to the scene */
        this._cloth = new Cloth(_forceRegistry);
        _scene.AddGameObject(this._cloth);

        /* set everything up */
        Reset();
    }
}