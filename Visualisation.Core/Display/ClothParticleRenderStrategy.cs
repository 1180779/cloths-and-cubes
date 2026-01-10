using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display;

public sealed class ClothParticleRenderStrategy : IRenderStrategy
{
    private readonly CubeMesh _cubeMesh;

    public ClothParticleRenderStrategy(CubeMesh cubeMesh)
    {
        _cubeMesh = cubeMesh;
    }

    public void Render(RenderContext context, Matrix4 model)
    {
        // Particles are not rendered in the main pass, only as selection/hover indicators
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

        _cubeMesh.Render();
    }
}