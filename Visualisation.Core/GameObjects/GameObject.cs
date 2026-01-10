using System.Diagnostics;

using Visualisation.Core.Display;
using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public abstract class GameObject : IIdentifiable, IDisposable, IHasRenderStrategy
{
    protected static Matrix4 GenerateModelMatrix(Vector3 position, Vector3 scale, Quaternion rotation) =>
        Matrix4.CreateScale(scale) * Matrix4.CreateFromQuaternion(rotation) *
        Matrix4.CreateTranslation(position);

    protected abstract IMesh Mesh { get; set; }
    public abstract Matrix4 Model { get; }

    public abstract IRenderStrategy RenderStrategy { get; }

    public abstract object PhysicsObject { get; }
    public virtual Vector3 Position => Vector3.Zero;

    private IMaterial? _material;

    public IMaterial Material
    {
        get
        {
            _material ??= MaterialConstant.RedPlastic;
            return _material;
        }
        set
        {
            var oldMaterial = _material;
            value.EnsureLoaded();
            _material = value;
            oldMaterial?.Dispose();
        }
    }

    public Guid Id { get; set; } = Guid.NewGuid();

    private bool _isDisposed;

    public void SetForShaderNoMaterial(Shader sh)
    {
        sh.SetMatrix4("model", Model);
    }

    public void SetForShader(Shader sh)
    {
        sh.SetMatrix4("model", Model);
        Material.SetForPbrShader(sh);
    }

    protected virtual void PreRender() { }

    public void Render(bool drawEvenInvisible = false)
    {
        PreRender();
        Mesh.Render();
    }

    ~GameObject()
    {
        Debug.Assert(_isDisposed, "GameObject was not disposed before finalization.");
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Mesh.Dispose();
        Material.Dispose();
        GC.SuppressFinalize(this);
    }

    public string DisplayName => $"{GetType().Name} ({Id.ToString().Substring(0, 8)})";
    public object InspectedObject => this;
}