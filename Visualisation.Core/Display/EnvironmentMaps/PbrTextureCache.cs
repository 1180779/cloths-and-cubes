using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.EnvironmentMaps;

/// <summary>
/// Handles loading and saving of pre-computed PBR textures to/from disk on a per-texture basis.
/// This allows expensive computations to be done once and reused.
/// </summary>
public static class PbrTextureCache
{
    public static bool Exists(string path)
    {
        return File.Exists($"{path}_env.dds") &&
            File.Exists($"{path}_irradiance.dds") &&
            File.Exists($"{path}_prefilter.dds");
    }

    public static void Save(string path, PbrTextures textures)
    {
        Directory.CreateDirectory(Config.Pbr.CacheDirectory);
        SaveCubemap($"{path}_env.dds", textures.EnvCubemap, 512);
        SaveCubemap($"{path}_irradiance.dds", textures.IrradianceMap, 32);
        SaveCubemap($"{path}_prefilter.dds", textures.PrefilterMap, 128, 5);
    }

    public static PbrTextures Load(string path)
    {
        int environmentCubemap = LoadCubemap($"{path}_env.dds");
        int irradianceMap = LoadCubemap($"{path}_irradiance.dds");
        int prefilterMap = LoadCubemap($"{path}_prefilter.dds");
        return new PbrTextures(environmentCubemap, irradianceMap, prefilterMap);
    }

    private static void SaveCubemap(string path, int textureId, int size, int mipmaps = 1)
    {
        GL.BindTexture(TextureTarget.TextureCubeMap, textureId);

        using var writer = new BinaryWriter(File.Create(path));

        writer.Write(0x20534444);
        writer.Write(size);
        writer.Write(mipmaps);

        for (int mip = 0; mip < mipmaps; mip++)
        {
            int mipSize = (int)(size * Math.Pow(0.5, mip));

            for (int face = 0; face < 6; face++)
            {
                var data = new float[mipSize * mipSize * 3]; // RGB16F
                GL.GetTexImage(TextureTarget.TextureCubeMapPositiveX + face, mip,
                    PixelFormat.Rgb, PixelType.Float, data);

                foreach (var value in data)
                {
                    writer.Write((Half)value);
                }
            }
        }

        GlHelper.CheckGlError($"SaveCubemap - {path}");
    }


    private static int LoadCubemap(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        var magic = reader.ReadInt32();
        if (magic != 0x20534444)
        {
            throw new InvalidDataException($"Invalid DDS file: {path}");
        }

        var size = reader.ReadInt32();
        var mipmaps = reader.ReadInt32();

        var textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, textureId);

        for (int mip = 0; mip < mipmaps; mip++)
        {
            int mipSize = (int)(size * Math.Pow(0.5, mip));

            for (int face = 0; face < 6; face++)
            {
                var halfData = new Half[mipSize * mipSize * 3];
                for (int i = 0; i < halfData.Length; i++)
                {
                    halfData[i] = reader.ReadHalf();
                }

                // Convert to float for upload
                var floatData = new float[halfData.Length];
                for (int i = 0; i < halfData.Length; i++)
                {
                    floatData[i] = (float)halfData[i];
                }

                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + face, mip,
                    PixelInternalFormat.Rgb16f, mipSize, mipSize, 0,
                    PixelFormat.Rgb, PixelType.Float, floatData);
            }
        }

        // Set texture parameters
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR,
            (int)TextureWrapMode.ClampToEdge);

        if (mipmaps > 1)
        {
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel,
                mipmaps - 1);
        }
        else
        {
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
        }

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        GlHelper.CheckGlError($"LoadCubemap - {path}");
        return textureId;
    }
}