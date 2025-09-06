using Visualisation.Core.Display.VisualObjects;

namespace Visualization.Display.VisualObjects;

/* TODO: change name to more appropriate? Like IGameObject? */
public abstract class VisualObjectBase : IIdentifiable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Material Material { get; set; } = Material.Copper;

    public Vector3 Position { get; set; } = new(0.0f, 0.0f, 0.0f);
    public Vector3 Scale { get; set; } = new(1.0f, 1.0f, 1.0f);
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public Matrix4 ModelMatrix => Matrix4.Identity;

    public Matrix4 Model => Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) *
        Matrix4.CreateTranslation(Position);
    //
    // Matrix4.CreateScale(Scale);

    public void SetForShader(Shader sh)
    {
        sh.SetMatrix4("model", Model);
        Material.SetForShader(sh);
    }

    public abstract void Init();
    public abstract void Render();
    public abstract void Dispose();
}

public class MeshDataEmptyException : Exception
{
    public MeshDataEmptyException() : base("Mesh Data Not Initialized")
    {
    }

    public MeshDataEmptyException(string message) : base(message)
    {
    }
}