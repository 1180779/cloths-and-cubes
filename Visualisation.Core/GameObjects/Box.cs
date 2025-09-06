using Visualisation.Core.Display.VisualObjects;
using Visualization.Display;
using Visualization.Display.VisualObjects;
using VisualObjects_IVisualObject = Visualisation.Core.Display.VisualObjects.IVisualObject;

namespace Visualisation.Core.GameObjects;

public class Box : VisualObjects_IVisualObject
{
    public Engine.RigidBodies.Box EngineBox { get; private set; } = new();
    public Cube VisualBox { get; private set; } = new();

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
        VisualBox.Position = new Vector3(
            (float)EngineBox.Body.Position.X,
            (float)EngineBox.Body.Position.Y,
            (float)EngineBox.Body.Position.Z);

        var halfSize = new Vector3(
            (float)EngineBox.HalfSize.X,
            (float)EngineBox.HalfSize.Y,
            (float)EngineBox.HalfSize.Z);
        VisualBox.Scale = halfSize * 2f;

        var q = new Quaternion(
            (float)EngineBox.Body.Orientation.I,
            (float)EngineBox.Body.Orientation.J,
            (float)EngineBox.Body.Orientation.K,
            (float)EngineBox.Body.Orientation.R);

        // Normalize to guard against drift
        if (MathF.Abs(1f - q.Length) > 1e-3f)
            q = Quaternion.Normalize(q);

        VisualBox.Rotation = q;

        VisualBox.Render();
    }


    public VisualObjectBase VisualObject => VisualBox;
    public object PhysicsObject => EngineBox;


    public void Dispose()
    {
        VisualBox.Dispose();
    }

    public Guid Id => VisualBox.Id;
}