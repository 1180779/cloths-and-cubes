using Visualisation.Core.Display.Materials;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public abstract class GameObject : IIdentifiable, IDisposable
{
    public static Matrix4 GenerateModelMatrix(Vector3 position, Vector3 scale, Quaternion rotation) =>
        Matrix4.CreateScale(scale) * Matrix4.CreateFromQuaternion(rotation) *
        Matrix4.CreateTranslation(position);

    protected abstract IMesh Mesh { get; set; }
    public abstract object PhysicsObject { get; }
    public abstract Matrix4 Model { get; }
    public virtual Vector3 Position => Vector3.Zero;
    public IMaterial Material { get; set; } = MaterialConstant.RedPlastic; // MaterialTextured.Organic.Feathers;
    public Guid Id { get; set; } = Guid.NewGuid();

    public void SetForShader(Shader sh)
    {
        sh.SetMatrix4("model", Model);
        Material.SetForShader(sh);
    }

    protected virtual void PreRender() { }

    public void Render()
    {
        PreRender();
        Mesh.Render();
    }

    public void Dispose()
    {
        Mesh.Dispose();
    }
}