using Visualisation.Core.Display.Materials;
using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesMaterialsDemo : Application
{
    public BoxesMaterialsDemo()
    {
        _boxesDemoSettingsWindow =
            new(() => _boxes.Length, () => _balls.Length, () => _cloths.Length, () => _joints.Joints.Count)
            {
                SetBoxesCount = count =>
                {
                    int length = _boxes.Length;
                    for (int i = count; i < length; ++i)
                    {
                        _boxes[i].Dispose();
                    }

                    Array.Resize(ref _boxes, count);
                    int rowCount = _balls.Length + length;
                    for (int i = length; i < count; ++i)
                    {
                        _boxes[i] = new Box();
                        var row = rowCount / _nrOfRows;
                        var col = rowCount % _nrOfRows;
                        _boxes[i].EngineBox.SetState(
                            position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                            orientation: new Engine.Quaternion(),
                            extents: new Engine.Vector3(1.0f, 1.0f, 1.0f),
                            velocity: new Engine.Vector3()
                        );
                        _boxes[i].Material = Materials[i % Materials.Length].TypedClone();
                        rowCount++;
                    }
                },
                SetSpheresCount = count =>
                {
                    int length = _balls.Length;
                    for (int i = count; i < length; ++i)
                    {
                        _balls[i].Dispose();
                    }

                    Array.Resize(ref _balls, count);
                    int rowCount = _boxes.Length + length;
                    for (int i = length; i < count; ++i)
                    {
                        _balls[i] = new Ball();
                        var row = rowCount / _nrOfRows;
                        var col = rowCount % _nrOfRows;
                        _balls[i].EngineBall.SetState(
                            position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                            orientation: new Engine.Quaternion(),
                            radius: 1.0f,
                            velocity: new Engine.Vector3()
                        );
                        _balls[i].Material = Materials[i % Materials.Length].TypedClone();
                        rowCount++;
                    }
                }
            };
        _boxesDemoSettingsWindow.SetClothsCount = count =>
        {
            int length = _cloths.Length;
            for (int i = count; i < length; ++i)
            {
                _cloths[i].EngineCloth.RemoveSpringsFromForceRegistry();
                _cloths[i].Dispose();
            }

            Array.Resize(ref _cloths, count);
            for (int i = length; i < count; ++i)
            {
                _cloths[i] = new Cloth(_forceRegistry, () => _contactResolver.PositionEpsilon,
                    _boxesDemoSettingsWindow.SizeX, _boxesDemoSettingsWindow.SizeY,
                    _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
                    _boxesDemoSettingsWindow.ParticleMass);
            }
        };
    }

    protected int _nrOfRows = 5;

    protected IMaterial[] Materials => MaterialsHelper.AllTexturedMaterials;

    public override void Reset()
    {
        _forceBVHRebuildOnNoUpdate = true;

        _forceRegistry.Clear();
        foreach (Cloth cloth in _cloths)
        {
            cloth.EngineCloth = new Engine.Cloth(_forceRegistry, _boxesDemoSettingsWindow.SizeX,
                _boxesDemoSettingsWindow.SizeY,
                _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
                _boxesDemoSettingsWindow.ParticleMass);
        }

        // reset plane
        _plane.EnginePlane.Direction = Engine.Vector3.Up;
        _plane.EnginePlane.Offset = 0f;

        int count = 0;
        foreach (var box in _boxes)
        {
            var row = count / _nrOfRows;
            var col = count % _nrOfRows;
            box.EngineBox.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(1.0f, 1.0f, 1.0f),
                velocity: new Engine.Vector3()
            );
            count++;
        }

        foreach (var ball in _balls)
        {
            var row = count / _nrOfRows;
            var col = count % _nrOfRows;
            ball.EngineBall.SetState(
                position: new Engine.Vector3(col * 2.5f - 2.5f, row * 2.5f + 5.0f, 0.0f),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3()
            );
            count++;
        }

        // Reset the contacts
        _collisionData.ContactCount = 0;
    }

    // BoxesMaterialsDemo inherits UpdateObjectArrays and UpdatePlane from BoxesDemo
}