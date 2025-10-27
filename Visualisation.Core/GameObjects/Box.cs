using Engine.Collision.Bounding_Volume_Hierarchy;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public class Box : IVisualObject, IBoxable
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


    public AbstractVisualObject AbstractVisualObject => VisualBox;
    public object PhysicsObject => EngineBox;


    public void Dispose()
    {
        VisualBox.Dispose();
    }

    public Guid Id
    {
        get
        {
            return VisualBox.Id;
        }
    }

    public BoundingBox GetBoundingBox()
    {
        float max = Math.Max(EngineBox.HalfSize.X, Math.Max(EngineBox.HalfSize.Y, EngineBox.HalfSize.Z));
        return new Engine.Collision.Bounding_Volume_Hierarchy.BoundingBox(
            center: this.EngineBox.Body.Position,
            halfSize: new Engine.Vector3(max, max, max));
    }
}