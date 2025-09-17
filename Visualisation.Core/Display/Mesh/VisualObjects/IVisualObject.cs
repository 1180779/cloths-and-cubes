namespace Visualisation.Core.Display.Mesh.VisualObjects;

public interface IVisualObject : IMesh, IIdentifiable
{
    public void SetForShader(Shader sh);
    public AbstractVisualObject AbstractVisualObject { get; }
    public object PhysicsObject { get; }
}