namespace Visualisation.Core.Display;

public interface IHasRenderStrategy
{
    IRenderStrategy RenderStrategy { get; }
    Matrix4 Model { get; }
}