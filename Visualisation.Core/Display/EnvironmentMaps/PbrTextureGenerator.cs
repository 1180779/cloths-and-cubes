using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.Display.EnvironmentMaps;

public record PbrTexturesMonocolor(HdrMonocolor GeneratedEnvironmentMap, PbrTextures PbrTextures);

public struct HdrMonocolor(
    int hdrEnvironmentMap
) : IDisposable
{
    public int HdrEnvironmentMap { get; private set; } = hdrEnvironmentMap;

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        GL.DeleteTexture(HdrEnvironmentMap);
        HdrEnvironmentMap = 0;
    }
}

public struct PbrTextures(
    int envCubemap,
    int irradianceMap,
    int prefilterMap
) : IDisposable
{
    public int EnvCubemap { get; private set; } = envCubemap;
    public int IrradianceMap { get; private set; } = irradianceMap;
    public int PrefilterMap { get; private set; } = prefilterMap;

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        GL.DeleteTexture(EnvCubemap);
        GL.DeleteTexture(IrradianceMap);
        GL.DeleteTexture(PrefilterMap);

        EnvCubemap = 0;
        IrradianceMap = 0;
        PrefilterMap = 0;
    }
}

/// <summary>
/// Generates PBR textures from HDR environment maps. This is an expensive operation
/// that should be done once and then the results cached to disk.
/// </summary>
public static class PbrTextureGenerator
{
    private const int EnvCubemapSize = 512;
    private const int IrradianceMapSize = 32;
    private const int PrefilterMapSize = 128;
    private const int BrdfLutSize = 512;
    private const int MaxMipLevels = 5;

    /// <summary>
    /// Generates PBR textures from an HDR equirectangular map (excluding BRDF LUT which is environment-independent).
    /// </summary>
    public static PbrTextures GenerateFromHdr(
        int hdrTextureId,
        Shader equirectangularToCubemapShader,
        Shader irradianceConvolutionShader,
        Shader prefilterShader)
    {
        using var cube = new CubeMesh();

        var captureFbo = GL.GenFramebuffer();
        var captureRbo = GL.GenRenderbuffer();

        var captureProjection = CreateCaptureProjection();
        var captureViews = CreateCaptureViews();

        var envCubemap = GenerateEnvironmentCubemap(
            hdrTextureId,
            equirectangularToCubemapShader,
            captureFbo,
            captureRbo,
            captureProjection,
            captureViews,
            cube);

        var irradianceMap = GenerateIrradianceMap(
            envCubemap,
            irradianceConvolutionShader,
            captureFbo,
            captureRbo,
            captureProjection,
            captureViews,
            cube);

        var prefilterMap = GeneratePrefilterMap(
            envCubemap,
            prefilterShader,
            captureFbo,
            captureRbo,
            captureProjection,
            captureViews,
            cube);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.DeleteFramebuffer(captureFbo);
        GL.DeleteRenderbuffer(captureRbo);

        return new PbrTextures(envCubemap, irradianceMap, prefilterMap);
    }

    /// <summary>
    /// Generates PBR textures from a generated 1x1 HDR equirectangular map (excluding BRDF LUT which is environment-independent).
    /// </summary>
    public static PbrTexturesMonocolor Generate1X1(
        Shader equirectangularToCubemapShader,
        Shader irradianceConvolutionShader,
        Shader prefilterShader,
        byte[]? pixels = null)
    {
        // Generate monocolor "Hdr" replacement, then proceed with the proper call
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int texId);
        GL.BindTexture(TextureTarget.Texture2D, texId);

        if (pixels is null || pixels.Length < 4)
        {
            pixels =
            [
                (int)(0.2f * 255), (int)(0.3f * 255), (int)(0.5f * 255), 255
            ];
        }

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 1, 1, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GlHelper.CheckGlError("TexturesManager::EnsurePlaceholderCreated");

        var textures = GenerateFromHdr(texId, equirectangularToCubemapShader, irradianceConvolutionShader,
            prefilterShader);
        return new PbrTexturesMonocolor(new HdrMonocolor(texId), textures);
    }

    /// <summary>
    /// Generates only the BRDF LUT texture. This is independent of any environment map.
    /// </summary>
    public static int GenerateBrdfLutOnly(Shader brdfShader)
    {
        var captureFbo = GL.GenFramebuffer();
        var captureRbo = GL.GenRenderbuffer();

        try
        {
            return GenerateBrdfLut(brdfShader, captureFbo, captureRbo);
        }
        finally
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DeleteFramebuffer(captureFbo);
            GL.DeleteRenderbuffer(captureRbo);
        }
    }

    private static int GenerateEnvironmentCubemap(
        int hdrTextureId,
        Shader equirectangularToCubemapShader,
        int captureFbo,
        int captureRbo,
        Matrix4 captureProjection,
        Matrix4[] captureViews,
        CubeMesh cube)
    {
        var envCubemap = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, envCubemap);

        for (var i = 0; i < 6; ++i)
        {
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f,
                EnvCubemapSize, EnvCubemapSize, 0, PixelFormat.Rgb, PixelType.Float, 0);
        }

        SetupCubemapParameters(TextureMinFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24,
            EnvCubemapSize, EnvCubemapSize);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, captureRbo);

        equirectangularToCubemapShader.Use();
        equirectangularToCubemapShader.SetTexture("equirectangularMap", TextureTarget.Texture2D,
            TextureUnit.Texture0, hdrTextureId);
        equirectangularToCubemapShader.SetMatrix4("projection", captureProjection);

        GL.Viewport(0, 0, EnvCubemapSize, EnvCubemapSize);

        for (int i = 0; i < 6; ++i)
        {
            equirectangularToCubemapShader.SetMatrix4("view", captureViews[i]);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, envCubemap, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            cube.Render();
        }

        GlHelper.CheckGlError("PbrTextureGenerator - envCubemap");
        return envCubemap;
    }

    private static int GenerateIrradianceMap(
        int envCubemap,
        Shader irradianceConvolutionShader,
        int captureFbo,
        int captureRbo,
        Matrix4 captureProjection,
        Matrix4[] captureViews,
        CubeMesh cube)
    {
        var irradianceMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, irradianceMap);

        for (var i = 0; i < 6; ++i)
        {
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f,
                IrradianceMapSize, IrradianceMapSize, 0, PixelFormat.Rgb, PixelType.Float, 0);
        }

        SetupCubemapParameters(TextureMinFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24,
            IrradianceMapSize, IrradianceMapSize);

        irradianceConvolutionShader.Use();
        irradianceConvolutionShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap,
            TextureUnit.Texture1, envCubemap);
        irradianceConvolutionShader.SetMatrix4("projection", captureProjection);

        GL.Viewport(0, 0, IrradianceMapSize, IrradianceMapSize);

        for (var i = 0; i < 6; ++i)
        {
            irradianceConvolutionShader.SetMatrix4("view", captureViews[i]);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + i, irradianceMap, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            cube.Render();
        }

        GlHelper.CheckGlError("PbrTextureGenerator - irradianceMap");
        return irradianceMap;
    }

    private static int GeneratePrefilterMap(
        int envCubemap,
        Shader prefilterShader,
        int captureFbo,
        int captureRbo,
        Matrix4 captureProjection,
        Matrix4[] captureViews,
        CubeMesh cube)
    {
        var prefilterMap = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, prefilterMap);

        for (var i = 0; i < 6; ++i)
        {
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f,
                PrefilterMapSize, PrefilterMapSize, 0, PixelFormat.Rgb, PixelType.Float, 0);
        }

        SetupCubemapParameters(TextureMinFilter.LinearMipmapLinear);
        GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);

        // Setup shader
        prefilterShader.Use();
        prefilterShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1, envCubemap);
        prefilterShader.SetMatrix4("projection", captureProjection);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);

        for (var mip = 0; mip < MaxMipLevels; ++mip)
        {
            var mipWidth = (int)(PrefilterMapSize * Math.Pow(0.5, mip));
            var mipHeight = (int)(PrefilterMapSize * Math.Pow(0.5, mip));

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24,
                mipWidth, mipHeight);
            GL.Viewport(0, 0, mipWidth, mipHeight);

            float roughness = (float)mip / (float)(MaxMipLevels - 1);
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

        GlHelper.CheckGlError("PbrTextureGenerator - prefilterMap");
        return prefilterMap;
    }

    private static int GenerateBrdfLut(Shader brdfShader, int captureFbo, int captureRbo)
    {
        using var quad = new QuadMesh();

        var brdfLutTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, brdfLutTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rg16f, BrdfLutSize, BrdfLutSize, 0,
            PixelFormat.Rg, PixelType.Float, 0);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24,
            BrdfLutSize, BrdfLutSize);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, brdfLutTexture, 0);

        GL.Viewport(0, 0, BrdfLutSize, BrdfLutSize);

        brdfShader.Use();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        quad.Render();

        GlHelper.CheckGlError("PbrTextureGenerator - brdfLUT");
        return brdfLutTexture;
    }

    private static Matrix4 CreateCaptureProjection()
    {
        return Matrix4.CreatePerspectiveFieldOfView(90.0f * MathHelper.DegToRad, 1.0f, 0.1f, 10.0f);
    }

    private static Matrix4[] CreateCaptureViews()
    {
        return
        [
            Matrix4.LookAt(Vector3.Zero, new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            Matrix4.LookAt(Vector3.Zero, new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f))
        ];
    }

    private static void SetupCubemapParameters(TextureMinFilter minFilter)
    {
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
    }
}