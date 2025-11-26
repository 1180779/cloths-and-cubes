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
    public enum EnvironmentMapDisplayType
    {
        EnvironmentCubemap,
        IrradianceMap,
        PrefilterMap,
    }

    public EnvironmentMapDisplayType DisplayType = EnvironmentMapDisplayType.EnvironmentCubemap;
    public float PrefilterMapValue = 1.0f;

    private readonly PbrTextures _textures;
    private readonly int _brdfLutTexture;

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
            _textures = PbrTextureCache.Load(hdrFileName);
        }
        else
        {
            Debug.WriteLine($"Generating PBR textures from HDR: {hdrPath}");

            var hdr = LoadHdrTexture(hdrPath);

            _textures = PbrTextureGenerator.GenerateFromHdr(
                hdr.TextureId,
                equirectangularToCubemapShader,
                irradianceConvolutionShader,
                prefilterShader);

            TexturesManager.FreeTexture(hdr.TexturePath);

            Debug.WriteLine($"Saving PBR textures to cache: {Config.Pbr.CacheDirectory}");
            PbrTextureCache.Save(hdrFileName, _textures);
        }

        _brdfLutTexture = BrdfLutManager.GetOrGenerateBrdfLut(brdfShader, forceRegenerate);
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
        switch (DisplayType)
        {
            case EnvironmentMapDisplayType.EnvironmentCubemap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    _textures.EnvCubemap);
                skyboxShader.SetFloat("lookup", 1.0f);
                break;
            case EnvironmentMapDisplayType.IrradianceMap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    _textures.IrradianceMap);
                skyboxShader.SetFloat("lookup", 1.0f);
                break;
            case EnvironmentMapDisplayType.PrefilterMap:
                skyboxShader.SetTexture("environmentMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
                    _textures.PrefilterMap);
                skyboxShader.SetFloat("lookup", PrefilterMapValue);
                break;
        }
    }

    public void SetForPbrShader(Shader pbrShader)
    {
        pbrShader.SetTexture("irradianceMap", TextureTarget.TextureCubeMap, TextureUnit.Texture1,
            _textures.IrradianceMap);
        pbrShader.SetTexture("prefilterMap", TextureTarget.TextureCubeMap, TextureUnit.Texture2,
            _textures.PrefilterMap);
        pbrShader.SetTexture("brdfLUT", TextureTarget.Texture2D, TextureUnit.Texture3, _brdfLutTexture);
    }

    public void Dispose()
    {
        _textures.Dispose();
    }
}