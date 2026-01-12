using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core;

public static class GlHelper
{
    public sealed record GlSavedState
    {
        public bool ScissorTest;
        public bool Blend;
        public bool DepthTest;
        public bool CullFace;
        public bool StencilTest;
        public bool DepthMask;

        public int[] Viewport = new int[4];
        public float[] ClearColor = new float[4];
        public int[] PolygonMode = new int[2];
    }

    public static GlSavedState SaveGlState()
    {
        var scissorTest = GL.IsEnabled(EnableCap.ScissorTest);
        var blend = GL.IsEnabled(EnableCap.Blend);
        var depthTest = GL.IsEnabled(EnableCap.DepthTest);
        var cullFace = GL.IsEnabled(EnableCap.CullFace);
        var stencilTest = GL.IsEnabled(EnableCap.StencilTest);

        GL.GetBoolean(GetPName.DepthWritemask, out var depthMask);
        var viewport = new int[4];
        GL.GetInteger(GetPName.Viewport, viewport);

        var clearColor = new float[4];
        GL.GetFloat(GetPName.ColorClearValue, clearColor);

        var polygonMode = new int[2];
        GL.GetInteger(GetPName.PolygonMode, polygonMode);

        return new GlSavedState
        {
            ScissorTest = scissorTest,
            Blend = blend,
            DepthMask = depthMask,
            DepthTest = depthTest,
            CullFace = cullFace,
            StencilTest = stencilTest,
            Viewport = viewport,
            ClearColor = clearColor,
            PolygonMode = polygonMode,
        };
    }

    public static void RestoreGlState(GlSavedState state)
    {
        if (state.ScissorTest) GL.Enable(EnableCap.ScissorTest);
        else GL.Disable(EnableCap.ScissorTest);
        if (state.Blend) GL.Enable(EnableCap.Blend);
        else GL.Disable(EnableCap.Blend);
        if (state.DepthTest) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        if (state.CullFace) GL.Enable(EnableCap.CullFace);
        else GL.Disable(EnableCap.CullFace);
        if (state.StencilTest) GL.Enable(EnableCap.StencilTest);
        else GL.Disable(EnableCap.StencilTest);

        GL.DepthMask(state.DepthMask);
        GL.Viewport(state.Viewport[0], state.Viewport[1], state.Viewport[2], state.Viewport[3]);
        GL.ClearColor(state.ClearColor[0], state.ClearColor[1], state.ClearColor[2], state.ClearColor[3]);
        GL.PolygonMode(TriangleFace.Front, (PolygonMode)state.PolygonMode[0]);
        GL.PolygonMode(TriangleFace.Back, (PolygonMode)state.PolygonMode[1]);
    }

    [Conditional("DEBUG")]
    public static void CheckGlError(string title)
    {
        ErrorCode error;
        int i = 1;
        while ((error = GL.GetError()) != ErrorCode.NoError)
        {
            Debug.Print($"{title} ({i}): {error}");
            ++i;
        }
    }
}