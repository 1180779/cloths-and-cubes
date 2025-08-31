using Visualization.Display;
using Visualization.Display.Objects;

namespace Visualization.GameObjects;

public class Plane : IVisualObject
{
    public readonly Display.VisualObjects.Plane VisualPlane = new();
    public Engine.Collision.CollisionPlane EnginePlane = new ()
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
}
