using Engine.Collision;
using Visualization.Display;
using Visualization.Display.VisualObjects;
using VisualObjects_IVisualObject = Visualisation.Core.Display.VisualObjects.IVisualObject;

namespace Visualization.GameObjects;

public class Plane : VisualObjects_IVisualObject
{
    public readonly Display.VisualObjects.Plane VisualPlane = new();

    public CollisionPlane EnginePlane = new()
    {
        Direction = new Engine.Vector3(0, 1, 0),
        Offset = 0,
    };

    public void Dispose()
    {
        VisualPlane.Dispose();
    }

    public void Init()
    {
        VisualPlane.Init();
    }

    public void SetForShader(Shader sh)
    {
        VisualPlane.SetForShader(sh);
    }

    public void Render()
    {
        VisualPlane.Render();
    }

    public VisualObjectBase VisualObject => VisualPlane;
    public object PhysicsObject => EnginePlane;
    public Guid Id => VisualPlane.Id;
}