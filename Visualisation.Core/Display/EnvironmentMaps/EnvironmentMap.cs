using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.EnvironmentMaps;

public class EnvironmentMap : IDisposable
{
    public enum DisplayType
    {
        EnvironmentCubemap,
        IrradianceMap,
        PrefilterMap,
    }

    public DisplayType displayType = DisplayType.EnvironmentCubemap;
    public float PrefilterMapValue = 1.0f;

    public bool IrradianceMapInsteadOfSkybox;

    private QuadMesh quad = new();
    private Cube cube = new();
    private readonly int envCubemap, irradianceMap, prefilterMap, brdfLUTTexture;
    private TexturesManager.TextureData hdr;

    public EnvironmentMap(
        string path,
        Shader equirectangularToCubemapShader,
        Shader irradianceConvolutionShader,
        Shader prefilterShader,
        Shader brdfShader
    )
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
        equirectangularToCubemapShader.Use();
        equirectangularToCubemapShader.SetTexture("equirectangularMap", TextureTarget.Texture2D, TextureUnit.Texture0,
            hdr.TextureId);
        equirectangularToCubemapShader.SetMatrix4("projection", captureProjection);

        GL.Viewport(0, 0, 512, 512);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        for (int i = 0; i < 6; ++i)
        {
            equirectangularToCubemapShader.SetMatrix4("view", captureViews[i]);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, envCubemap, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            cube.Render();
        }

        // TODO: Bright dots in the pre-filter convolution
        // GL.BindTexture(TextureTarget.TextureCubeMap, envCubemap);
        // GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
        GlHelper.CheckGlError("EnvironmentMap - envCubemap");

        /* now get the irradianceMap map */
        irradianceMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, irradianceMap);
        for (var i = 0; i < 6; ++i)
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, envCubemap, 0);
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f, 32, 32, 0,
                PixelFormat.Rgb, PixelType.Float, 0);
        }

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 32, 32);

        irradianceConvolutionShader.Use();
        irradianceConvolutionShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
            envCubemap);
        irradianceConvolutionShader.SetMatrix4("projection", captureProjection);

        GL.Viewport(0, 0, 32, 32); // don't forget to configure the viewport to the capture dimensions.
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        for (var i = 0; i < 6; ++i)
        {
            irradianceConvolutionShader.SetMatrix4("view", captureViews[i]);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, irradianceMap, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            cube.Render();
        }

        GlHelper.CheckGlError("EnvironmentMap - irradianceMap");

        /* prefilter convolution map based on the env cubemap */
        prefilterMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, prefilterMap);

        for (var i = 0; i < 6; ++i)
        {
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f, 128, 128, 0,
                PixelFormat.Rgb, PixelType.Float, 0);
        }

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.LinearMipmapLinear); // Enable mipmap filtering
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap); // Generate mipmaps

        prefilterShader.Use();
        prefilterShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1, envCubemap);
        prefilterShader.SetMatrix4("projection", captureProjection);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);

        int maxMipLevels = 5;
        for (var mip = 0; mip < maxMipLevels; ++mip)
        {
            // resize framebuffer according to mip-level size.
            var mipWidth = (int)(128 * Math.Pow(0.5, mip));
            var mipHeight = (int)(128 * Math.Pow(0.5, mip));
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, mipWidth,
                mipHeight);
            GL.Viewport(0, 0, mipWidth, mipHeight);

            float roughness = (float)mip / (float)(maxMipLevels - 1);
            prefilterShader.SetFloat("roughness", roughness);
            for (var i = 0; i < 6; ++i)
            {
                prefilterShader.SetMatrix4("view", captureViews[i]);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    TextureTarget.TextureCubeMapPositiveX + i, prefilterMap, mip);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                cube.Render();
            }
        }

        GlHelper.CheckGlError("EnvironmentMap - prefilterMap");

        /* brdfLUTTexture */
        brdfLUTTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, brdfLUTTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rg16f, 512, 512, 0, PixelFormat.Rg,
            PixelType.Float, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 512, 512);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, brdfLUTTexture, 0);

        GL.Viewport(0, 0, 512, 512);

        brdfShader.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        quad.Render();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        /* free the framebuffer and renderbuffer */
        GL.DeleteFramebuffer(captureFbo);
        GL.DeleteRenderbuffer(captureRbo);
    }

    public void SetForSkyBoxShader(Shader skyboxShader)
    {
        switch (displayType)
        {
            case DisplayType.EnvironmentCubemap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    envCubemap);
                skyboxShader.SetFloat("lookup", 1.0f);
                break;
            case DisplayType.IrradianceMap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    irradianceMap);
                skyboxShader.SetFloat("lookup", 1.0f);
                break;
            case DisplayType.PrefilterMap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    prefilterMap);
                skyboxShader.SetFloat("lookup", PrefilterMapValue);
                break;
        }
    }

    public void SetForQuadTextureShader(Shader textureShader)
    {
        textureShader.SetTexture("inTexture", TextureTarget.Texture2D, TextureUnit.Texture0, brdfLUTTexture);
    }

    public void SetForPbrShader(Shader pbrShader)
    {
        pbrShader.SetTexture("irradianceMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1, irradianceMap);
        pbrShader.SetTexture("prefilterMap", TextureTarget.TextureCubeMap, TextureUnit.Texture2, prefilterMap);
        pbrShader.SetTexture("brdfLUT", TextureTarget.Texture2D, TextureUnit.Texture3, brdfLUTTexture);
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
        GL.DeleteTexture(this.irradianceMap);
        GL.DeleteTexture(this.prefilterMap);
        GL.DeleteTexture(brdfLUTTexture);
        cube.Dispose();
        quad.Dispose();
    }
}