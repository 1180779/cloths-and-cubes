using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core;

public static class GlHelper
{
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