namespace Visualisation.Core.Display.Mesh.VisualObjects;

public abstract class AbstractVisualObject : IIdentifiable, IMesh
{
    public Guid Id { get; set; } = Guid.NewGuid();
    protected Material Material { get; set; } = Material.Organic.Feathers;

    public Vector3 Position { get; set; } = new(0.0f, 0.0f, 0.0f);
    public Vector3 Scale { get; set; } = new(1.0f, 1.0f, 1.0f);
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public Matrix4 ModelMatrix => Matrix4.Identity;

    public Matrix4 Model => Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) *
        Matrix4.CreateTranslation(Position);

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