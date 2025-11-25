using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.EnvironmentMaps;

/// <summary>
/// Manages PBR environment maps for rendering. Supports both cached (fast) and
/// generated (slow but flexible) loading modes.
/// </summary>
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

    private readonly PbrTextures textures;
    private readonly int brdfLutTexture;

    /// <summary>
    /// Creates an EnvironmentMap by loading from cache or generating if cache doesn't exist.
    /// </summary>
    /// <param name="hdrPath">Path to the HDR equirectangular map</param>
    /// <param name="equirectangularToCubemapShader">Shader for converting equirectangular to cubemap</param>
    /// <param name="irradianceConvolutionShader">Shader for generating irradiance map</param>
    /// <param name="prefilterShader">Shader for generating prefilter map</param>
    /// <param name="brdfShader">Shader for generating BRDF LUT (shared resource)</param>
    /// <param name="forceRegenerate">If true, ignores cache and regenerates textures</param>
    public EnvironmentMap(
        string hdrPath,
        Shader equirectangularToCubemapShader,
        Shader irradianceConvolutionShader,
        Shader prefilterShader,
        Shader brdfShader,
        bool forceRegenerate = false
    )
    {
        var hdrFileName = Path.Combine(
            Config.Pbr.CacheDirectory,
            Path.GetFileNameWithoutExtension(hdrPath)
        );
        bool allCached = !forceRegenerate &&
            PbrTextureCache.Exists(hdrFileName);

        if (allCached)
        {
            Debug.WriteLine($"Loading PBR textures from cache: {Config.Pbr.CacheDirectory}");
            textures = PbrTextureCache.Load(hdrFileName);
        }
        else
        {
            Debug.WriteLine($"Generating PBR textures from HDR: {hdrPath}");

            var hdr = LoadHdrTexture(hdrPath);

            textures = PbrTextureGenerator.GenerateFromHdr(
                hdr.TextureId,
                equirectangularToCubemapShader,
                irradianceConvolutionShader,
                prefilterShader);

            TexturesManager.FreeTexture(hdr.TexturePath);

            Debug.WriteLine($"Saving PBR textures to cache: {Config.Pbr.CacheDirectory}");
            PbrTextureCache.Save(hdrFileName, textures);
        }

        brdfLutTexture = BrdfLutManager.GetOrGenerateBrdfLut(brdfShader, forceRegenerate);
    }

    private static TexturesManager.TextureData LoadHdrTexture(string path)
    {
        return TexturesManager.LoadTextureImmediately(path, () =>
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
        });
    }

    public void SetForSkyBoxShader(Shader skyboxShader)
    {
        switch (displayType)
        {
            case DisplayType.EnvironmentCubemap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    textures.EnvCubemap);
                skyboxShader.SetFloat("lookup", 1.0f);
                break;
            case DisplayType.IrradianceMap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    textures.IrradianceMap);
                skyboxShader.SetFloat("lookup", 1.0f);
                break;
            case DisplayType.PrefilterMap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    textures.PrefilterMap);
                skyboxShader.SetFloat("lookup", PrefilterMapValue);
                break;
        }
    }

    public void SetForPbrShader(Shader pbrShader)
    {
        pbrShader.SetTexture("irradianceMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
            textures.IrradianceMap);
        pbrShader.SetTexture("prefilterMap", TextureTarget.TextureCubeMap, TextureUnit.Texture2, textures.PrefilterMap);
        pbrShader.SetTexture("brdfLUT", TextureTarget.Texture2D, TextureUnit.Texture3, brdfLutTexture);
    }

    public void Dispose()
    {
        textures.Dispose();
    }
}