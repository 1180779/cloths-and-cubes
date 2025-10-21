using System.Reflection;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos;

public class PbrMaterialsDemo : BoxesDemo
{
    private const int NrOfRows = 5;
    private const int NFirstMaterials = 8 * NrOfRows;
    private readonly Type materialsSource = typeof(Material.Metals);

    protected override void InitializeScene()
    {
        var topLevelMaterials = materialsSource
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Material))
            .Select(p => (Material)p.GetValue(null)!)
            .ToArray();
        var nestedMaterials = materialsSource
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(p => p.PropertyType == typeof(Material))
            .Select(p => (Material)p.GetValue(null)!)
            .ToArray();

        Material[] materials = [..topLevelMaterials, ..nestedMaterials];
        materials = materials.Take(NFirstMaterials).ToArray();

        boxes = new Box[materials.Length];
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            var box = new Box();
            var row = i / NrOfRows;
            var col = i % NrOfRows;
            box.EngineBox.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(1.0f, 1.0f, 1.0f),
                velocity: new Engine.Vector3()
            );
            box.VisualBox.Material = material;
            boxes[i] = box;
            Scene.AddGameObject(box);
        }


        /* add ground plane to the scene */
        plane = new();
        Scene.AddGameObject(plane);

        /* set everything up */
        Reset();
    }

    protected override void Reset()
    {
        for (var i = 0; i < boxes.Length; i++)
        {
            var box = boxes[i];
            var row = i / NrOfRows;
            var col = i % NrOfRows;
            box.EngineBox.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(1.0f, 1.0f, 1.0f),
                velocity: new Engine.Vector3()
            );
        }

        // Reset the contacts
        CollisionData.ContactCount = 0;
    }
}