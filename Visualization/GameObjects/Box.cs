using Visualization.Display;
using Visualization.Display.Objects;

namespace Visualization.GameObjects;

public class Box : IVisualObject
{
    public Engine.RigidBodies.Box EngineBox { get; private set; } = new();
    public Display.VisualObjects.Cube VisualBox { get; private set; } = new();

    public void Init()
    {
        VisualBox.Init();
    }

    public void SetForShader(Shader sh)
    {
        VisualBox.SetForShader(sh);
    }

    /// <summary>
    /// Draws the box, excluding its shadow.
    /// </summary>
    public void Render()
    {
        // Set the cube's transform from the physics body
        VisualBox.Position = new Vector3((float)EngineBox.Body.Position.X, (float)EngineBox.Body.Position.Y, (float)EngineBox.Body.Position.Z);
        VisualBox.Scale = new Vector3((float)Math.Abs(EngineBox.HalfSize.X * 2), (float)Math.Abs(EngineBox.HalfSize.Y * 2), (float)Math.Abs(EngineBox.HalfSize.Z * 2));
        VisualBox.Rotation = new Quaternion((float)EngineBox.Body.Orientation.R, (float)EngineBox.Body.Orientation.I, (float)EngineBox.Body.Orientation.J, (float)EngineBox.Body.Orientation.K);

        // Render the cube
        VisualBox.Render();
    }


    public void Dispose()
    {
        VisualBox.Dispose();
    }
}