using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;

namespace Visualisation.Core.Display;

public sealed class StaticMeshNoOutlineRenderStrategy : IRenderStrategy
{
    private readonly IMesh _mesh;
    private readonly IMaterial _material;

    public StaticMeshNoOutlineRenderStrategy(IMesh mesh, IMaterial material)
    {
        _mesh = mesh;
        _material = material;
    }

    public void Render(RenderContext context, Matrix4 model)
    {
        context.PbrShader.Use();
        context.PbrShader.SetMatrix4("model", model);
        _material.SetForPbrShader(context.PbrShader);
        _mesh.Render();
    }

    public void DrawOutline(RenderContext context, Matrix4 model)
    {
        // No outline rendering
    }
}