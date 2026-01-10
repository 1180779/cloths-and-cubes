using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;

namespace Visualisation.Core.Display;

public sealed class StaticMeshRenderStrategy : IRenderStrategy
{
    private readonly IMesh _mesh;
    private readonly IMaterial _material;

    public StaticMeshRenderStrategy(IMesh mesh, IMaterial material)
    {
        _mesh = mesh;
        _material = material;
    }

    public void Render(RenderContext context, Matrix4 model)
    {
        if (context.PbrShader == null)
        {
            return;
        }

        context.PbrShader.Use();
        context.PbrShader.SetMatrix4("model", model);
        if (!context.SkipMaterial)
        {
            _material.SetForPbrShader(context.PbrShader);
        }

        _mesh.Render();
    }

    public void DrawOutline(RenderContext context, Matrix4 model)
    {
        if (context.DefaultShader == null || context.Camera == null)
        {
            return;
        }

        context.DefaultShader.Use();
        context.Camera.SetForSimpleShader(context.DefaultShader);

        Vector3 scale = model.ExtractScale();
        Vector3 translation = model.ExtractTranslation();
        Quaternion rotation = model.ExtractRotation();

        float scaleFactor = 1.0f + context.OutlineSize;
        Matrix4 outlineModel = Matrix4.CreateScale(scale * scaleFactor) *
            Matrix4.CreateFromQuaternion(rotation) *
            Matrix4.CreateTranslation(translation);

        context.DefaultShader.SetMatrix4("model", outlineModel);
        context.DefaultShader.SetVector3("color", context.OutlineColor.Xyz);
        context.DefaultShader.SetFloat("alpha", context.OutlineColor.W);

        _mesh.Render();
    }
}