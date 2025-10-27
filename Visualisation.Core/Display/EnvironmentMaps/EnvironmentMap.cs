using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.EnvironmentMaps;

public class EnvironmentMap
{
    private TexturesManager.TextureData hdr;

    public EnvironmentMap(string path)
    {
        LoadImmediately(path);
    }

    public void SetForShader(Shader sh)
    {
        sh.SetTexture("equirectangularMap", TextureTarget.Texture2D, TextureUnit.Texture1, hdr.TextureId);
    }


    public void LoadImmediately(string path)
    {
        hdr = TexturesManager.LoadTextureImmediately(path, TextureInit);

        void TextureInit()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
        }
    }
}