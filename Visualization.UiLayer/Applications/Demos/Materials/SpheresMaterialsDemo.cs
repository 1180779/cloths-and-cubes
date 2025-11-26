using Visualisation.Core.Display.Materials;
using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public abstract class SpheresMaterialsDemo : BoxesDemo
{
    protected int _nrOfRows = 5;

    protected abstract IMaterial[] GetMaterials();

    protected override void InitializeScene()
    {
        var materials = GetMaterials();
        _balls = new Ball[materials.Length];
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            var ball = new Ball();
            var row = i / _nrOfRows;
            var col = i % _nrOfRows;
            ball.EngineBall.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3()
            );
            ball.Material = material;
            _balls[i] = ball;
            _scene.AddGameObject(ball);
        }


        /* add ground plane to the scene */
        _plane = new();
        _scene.AddGameObject(_plane);

        /* set everything up */
        Reset();
    }

    protected override void Reset()
    {
        for (var i = 0; i < _balls.Length; i++)
        {
            var ball = _balls[i];
            var row = i / _nrOfRows;
            var col = i % _nrOfRows;
            ball.EngineBall.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3()
            );
        }

        // Reset the contacts
        _collisionData.ContactCount = 0;
    }
}