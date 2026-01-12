namespace Visualisation.Core.Display;

public interface IRenderStrategy
{
    void Render(RenderContext context, Matrix4 model);
    void DrawOutline(RenderContext context, Matrix4 model);
}