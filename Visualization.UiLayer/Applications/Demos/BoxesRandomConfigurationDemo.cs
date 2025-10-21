using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesRandomConfigurationDemo : BoxesDemo
{
    private const int Boxes = 20;

    protected override void InitializeScene()
    {
        boxes = new Box[Boxes];

        /* add the cubes to the scene to be rendered */
        for (var i = 0; i < Boxes; i++)
        {
            this.boxes[i] = new Box();
            var box = boxes[i];
            Scene.AddGameObject(box);
        }

        /* add ground plane to the scene */
        plane = new();
        Scene.AddGameObject(plane);

        /* set everything up */
        Reset();
    }
}