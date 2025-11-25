using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.EnvironmentMaps;

/// <summary>
/// Manages the BRDF LUT texture which is environment-independent and only needs to be computed once.
/// </summary>
public static class BrdfLutManager
{
    private static int? CachedBrdfLutTexture;
    private static readonly Lock Lock = new();

    /// <summary>
    /// Gets or generates the BRDF LUT texture. If it's already been generated or loaded, returns the cached version.
    /// </summary>
    /// <param name="brdfShader">The shader used to generate the BRDF LUT</param>
    /// <param name="forceRegenerate">If true, regenerates even if the cache exists</param>
    public static int GetOrGenerateBrdfLut(Shader brdfShader, bool forceRegenerate = false)
    {
        lock (Lock)
        {
            if (CachedBrdfLutTexture.HasValue && !forceRegenerate)
            {
                return CachedBrdfLutTexture.Value;
            }

            var cachePath = Path.Combine(Config.Pbr.CacheDirectory, Config.Pbr.BrfdLuftCacheFile);
            if (!forceRegenerate && File.Exists(cachePath))
            {
                Debug.WriteLine($"Loading BRDF LUT from cache: {cachePath}");
                CachedBrdfLutTexture = LoadBrdfLut(cachePath);
            }
            else
            {
                Debug.WriteLine("Generating BRDF LUT...");
                CachedBrdfLutTexture = PbrTextureGenerator.GenerateBrdfLutOnly(brdfShader);

                Debug.WriteLine($"Saving BRDF LUT to cache: {cachePath}");
                SaveBrdfLut(cachePath, CachedBrdfLutTexture.Value);
            }

            return CachedBrdfLutTexture.Value;
        }
    }

    /// <summary>
    /// Saves a BRDF LUT texture to disk.
    /// </summary>
    public static void SaveBrdfLut(string path, int textureId)
    {
        EnsureDirectoryExists(path);

        const int size = 512;
        GL.BindTexture(TextureTarget.Texture2D, textureId);

        var data = new float[size * size * 2]; // RG16F
        GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rg, PixelType.Float, data);

        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(size);

        foreach (var value in data)
        {
            writer.Write((Half)value);
        }

        GlHelper.CheckGlError($"SaveBrdfLut - {path}");
    }

    /// <summary>
    /// Loads a BRDF LUT texture from disk.
    /// </summary>
    public static int LoadBrdfLut(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        var size = reader.ReadInt32();
        var halfData = new Half[size * size * 2];

        for (int i = 0; i < halfData.Length; i++)
        {
            halfData[i] = reader.ReadHalf();
        }

        var floatData = new float[halfData.Length];
        for (int i = 0; i < halfData.Length; i++)
        {
            floatData[i] = (float)halfData[i];
        }

        var textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rg16f, size, size, 0,
            PixelFormat.Rg, PixelType.Float, floatData);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        GlHelper.CheckGlError($"LoadBrdfLut - {path}");
        return textureId;
    }

    /// <summary>
    /// Clears the cached BRDF LUT texture. Note: This does not delete the OpenGL texture.
    /// </summary>
    public static void ClearCache()
    {
        lock (Lock)
        {
            CachedBrdfLutTexture = null;
        }
    }


    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}