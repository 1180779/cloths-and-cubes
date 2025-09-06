using Visualization.Display;
using Visualization.Display.VisualObjects;

namespace Visualisation.Core.Display.VisualObjects;

public interface IVisualObject : IDisposable, IIdentifiable
{
    public void Init();
    public void SetForShader(Shader sh);
    public void Render();
    public VisualObjectBase VisualObject { get; }
    public object PhysicsObject { get; }
}