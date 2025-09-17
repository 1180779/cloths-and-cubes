using Engine.Collision;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public class Plane : IVisualObject
{
    public readonly Display.Mesh.VisualObjects.Plane AbstractVisualPlane = new();

    public CollisionPlane EnginePlane = new()
    {
        Direction = new Engine.Vector3(0, 1, 0),
        Offset = 0,
    };

    public void Dispose()
    {
        AbstractVisualPlane.Dispose();
    }

    public void Init()
    {
        AbstractVisualPlane.Init();
    }

    public void SetForShader(Shader sh)
    {
        AbstractVisualPlane.SetForShader(sh);
    }

    public void Render()
    {
        AbstractVisualPlane.Render();
    }

    public AbstractVisualObject AbstractVisualObject => AbstractVisualPlane;
    public object PhysicsObject => EnginePlane;
    public Guid Id => AbstractVisualPlane.Id;
}