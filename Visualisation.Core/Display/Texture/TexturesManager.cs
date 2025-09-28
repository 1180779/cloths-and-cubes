using System.Collections.Concurrent;
using ImageMagick;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Visualisation.Core.Display.Texture;

public static class TexturesManager
{
    public class TextureData
    {
        public int TextureId { get; init; }


        /// <summary>
        /// Texture path. Used to identify the texture.
        /// </summary>
        public required string TexturePath { get; init; }
    }

    public class InternalTextureData
    {
        public required TextureData PublicTextureData;

        /// <summary>
        /// Number of references to the texture registered as in use.
        /// </summary>
        public int UsagesCount;

        public static InternalTextureData FromTextureData(TextureData textureData)
        {
            return new InternalTextureData
            {
                PublicTextureData = textureData,
            };
        }
    }

    private static readonly ConcurrentDictionary<string, Lazy<InternalTextureData>> TextureDataDict = new();

    public delegate void InitTextureCallback();

    private static InternalTextureData LoadTexture(string texturePath, InitTextureCallback initCallback)
    {
        var pathToLoad = texturePath;

        if (Path.GetExtension(texturePath).Equals(".psd", StringComparison.InvariantCultureIgnoreCase))
        {
            var pngPath = Path.ChangeExtension(texturePath, ".png");
            if (!File.Exists(pngPath))
            {
                using var magickImage = new MagickImage(texturePath);
                magickImage.Format = MagickFormat.Png;
                magickImage.Write(pngPath);
            }

            pathToLoad = pngPath;
        }

        using var image = Image.Load<Rgba32>(pathToLoad);
        image.Mutate(x => x.Flip(FlipMode.Vertical));

        var pixelData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelData);

        PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8;
        PixelFormat format = PixelFormat.Rgba;

        GL.CreateTextures(TextureTarget.Texture2D, 1, out int textureId);
        GL.BindTexture(TextureTarget.Texture2D, textureId);

        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, image.Width, image.Height, 0, format,
            PixelType.UnsignedByte, pixelData);

        var textureData = InternalTextureData.FromTextureData(new TextureData
        {
            TextureId = textureId,
            TexturePath = texturePath
        });

        initCallback();
        GlHelper.CheckGlError("TexturesManager::InitTextureCallback");

        GlHelper.CheckGlError("TexturesManager::GetOrLoadTexture");
        return textureData;
    }


    /// <summary>
    /// Get the Texture. Load the texture first if necessary. 
    /// </summary>
    /// <param name="texturePath">Path to the texture file. </param>
    /// <param name="initCallback">Callback used to set texture parameters. The texture is already bound at the call time. </param>
    /// <returns>MeshData class with mesh data. Copy of the data is returned. </returns>
    /// <exception cref="InvalidOperationException">Provided callback did not return the mesh data.</exception>
    public static TextureData GetOrLoadTexture(string texturePath, InitTextureCallback initCallback)
    {
        var lazyResult = TextureDataDict.GetOrAdd(texturePath,
            path => new Lazy<InternalTextureData>(() => LoadTexture(path, initCallback)));

        var textureData = lazyResult.Value;
        Interlocked.Increment(ref textureData.UsagesCount);
        return textureData.PublicTextureData;
    }

    public static void FreeTexture(string textureName)
    {
        if (!TextureDataDict.TryGetValue(textureName, out var lazyTextureData))
        {
            throw new InvalidOperationException("Texture Not Initialized");
        }

        if (!lazyTextureData.IsValueCreated)
        {
            // This can happen if FreeTexture is called for a texture that is in the process of being loaded
            // but hasn't finished yet. Depending on desired behavior, you could wait, or throw.
            // Throwing seems reasonable as it indicates a potential logic error in the calling code.
            throw new InvalidOperationException("Cannot free a texture that is still being loaded.");
        }

        var meshData = lazyTextureData.Value;

        var newCount = Interlocked.Decrement(ref meshData.UsagesCount);

        if (newCount < 0)
        {
            Interlocked.Increment(ref meshData.UsagesCount); // Correct the count
            throw new InvalidOperationException("Released more times than freed!");
        }

        if (newCount != 0) return;
        // It's possible another thread is adding a reference right now, so we lock here
        // to ensure that we don't free a texture that is about to be used again.
        lock (meshData)
        {
            // After acquiring the lock, we check the usage count again to make sure
            // it's still zero.
            if (meshData.UsagesCount != 0) return;
            GL.DeleteTexture(meshData.PublicTextureData.TextureId);
            TextureDataDict.TryRemove(textureName, out _);
            GlHelper.CheckGlError("TexturesManager::FreeTexture");
        }
    }
}