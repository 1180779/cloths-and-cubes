using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.EnvironmentMaps;

public class EnvironmentMap : IDisposable
{
    private Cube cube = new();
    private readonly int envCubemap;
    private TexturesManager.TextureData hdr;

    public EnvironmentMap(string path, Shader equirectangularToCubemapShader)
    {
        cube.Init();
        LoadImmediately(path);

        var captureFbo = GL.GenFramebuffer();
        var captureRbo = GL.GenRenderbuffer();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 512, 512);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, captureRbo);

        envCubemap = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, envCubemap);

        for (var i = 0; i < 6; ++i)
        {
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f, 512, 512, 0,
                PixelFormat.Rgb, PixelType.Float, 0);
        }

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        var captureProjection = Matrix4.CreatePerspectiveFieldOfView(90.0f * MathHelper.DegToRad, 1.0f, 0.1f, 10.0f);
        Matrix4[] captureViews =
        [
            Matrix4.LookAt(Vector3.Zero, new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f))
        ];

        // convert HDR equirectangular environment map to cubemap equivalent
        equirectangularToCubemapShader.Use(); // Assuming this is a Shader object
        equirectangularToCubemapShader.SetInt("equirectangularMap", 0); // Assuming this is a Shader object
        equirectangularToCubemapShader.SetMatrix4("projection", captureProjection); // Assuming this is a Shader object
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, hdr.TextureId);

        GL.Viewport(0, 0, 512, 512); // don't forget to configure the viewport to the capture dimensions.
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        for (int i = 0; i < 6; ++i)
        {
            equirectangularToCubemapShader.SetMatrix4("view", captureViews[i]); // Assuming this is a Shader object
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, envCubemap, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            cube.Render();
            // renderCube(); // renders a 1x1 cube
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        GL.DeleteFramebuffer(captureFbo);
        GL.DeleteRenderbuffer(captureRbo);
    }

    public void SetForShader(Shader sh)
    {
        sh.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1, envCubemap);
    }


    public void LoadImmediately(string path)
    {
        hdr = TexturesManager.LoadTextureImmediately(path, TextureInit);
        return;

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

    public void Dispose()
    {
        GL.DeleteTexture(envCubemap);
        cube.Dispose();
    }
}