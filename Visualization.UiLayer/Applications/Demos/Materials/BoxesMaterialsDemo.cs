using Visualisation.Core.Display.Materials;
using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos.Materials;

public abstract class BoxesMaterialsDemo : BoxesDemo
{
    protected int _nrOfRows = 5;

    protected abstract IMaterial[] GetMaterials();

    protected override void InitializeScene()
    {
        var materials = GetMaterials();
        _boxes = new Box[materials.Length];
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            var box = new Box();
            var row = i / _nrOfRows;
            var col = i % _nrOfRows;
            box.EngineBox.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(1.0f, 1.0f, 1.0f),
                velocity: new Engine.Vector3()
            );
            box.Material = material;
            _boxes[i] = box;
            _scene.AddGameObject(box);
        }


        /* add ground plane to the scene */
        _plane = new();
        _scene.AddGameObject(_plane);

        /* set everything up */
        Reset();
    }

    protected override void Reset()
    {
        for (var i = 0; i < _boxes.Length; i++)
        {
            var box = _boxes[i];
            var row = i / _nrOfRows;
            var col = i % _nrOfRows;
            box.EngineBox.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(1.0f, 1.0f, 1.0f),
                velocity: new Engine.Vector3()
            );
        }

        // Reset the contacts
        _collisionData.ContactCount = 0;
    }
}