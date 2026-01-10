using Engine;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display;

public sealed class ClothRenderStrategy : IRenderStrategy
{
    private readonly ClothMesh _mesh;
    private readonly IMaterial _material;
    private readonly Cloth _engineCloth;

    public ClothRenderStrategy(
        ClothMesh mesh,
        IMaterial material,
        Cloth engineCloth)
    {
        _mesh = mesh;
        _material = material;
        _engineCloth = engineCloth;
    }

    public void Render(RenderContext context, Matrix4 model)
    {
        if (context.PbrShader == null)
        {
            return;
        }

        UpdateMeshPoints(context.PositionEpsilon);

        context.PbrShader.Use();
        context.PbrShader.SetMatrix4("model", model);
        if (!context.SkipMaterial)
        {
            _material.SetForPbrShader(context.PbrShader);
        }

        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.PolygonOffsetFill);
        float offset = -(context.PositionEpsilon) * 1000.0f;
        GL.PolygonOffset(offset, offset);

        _mesh.Render();

        GL.Enable(EnableCap.CullFace);
        GL.Disable(EnableCap.PolygonOffsetFill);
    }

    public void DrawOutline(RenderContext context, Matrix4 model)
    {
        if (context.OutlineShader == null || context.Camera == null)
        {
            return;
        }

        GL.Disable(EnableCap.CullFace);

        context.OutlineShader.Use();
        context.Camera.SetForSimpleShader(context.OutlineShader);
        context.OutlineShader.SetMatrix4("model", model);
        context.OutlineShader.SetFloat("outline_size", context.OutlineSize);
        context.OutlineShader.SetVector3("color", context.OutlineColor.Xyz);
        context.OutlineShader.SetFloat("alpha", context.OutlineColor.W);

        _mesh.Render();

        GL.Enable(EnableCap.CullFace);
    }

    private void UpdateMeshPoints(float positionEpsilon)
    {
        var enginePoints = _engineCloth.PointsVelocityAdjusted(positionEpsilon);
        int sx = enginePoints.GetLength(0);
        int sy = enginePoints.GetLength(1);
        var result = new Vector3[sx, sy];

        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                var e = enginePoints[x, y];
                result[x, y] = new Vector3(e.X, e.Y, e.Z);
            }
        }

        _mesh.UpdatePoints(result);
    }
}