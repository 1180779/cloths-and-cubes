namespace Visualization.Display.Objects;

public interface IVisualObject : IDisposable
{
    public void Init();
    public void SetForShader(Shader sh);
    public void Render();
}