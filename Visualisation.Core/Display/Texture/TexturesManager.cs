using System.Collections.Concurrent;
using System.Diagnostics;
using ImageMagick;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Visualisation.Core.Display.Texture;

public static class TexturesManager
{
    /// <summary>
    /// Encapsulates texture-related metadata and state information, including
    /// unique texture identification and file path.
    /// </summary>
    public class TextureData
    {
        /// <summary>
        /// Unique identifier for the texture used in OpenGL operations. Volatile.
        /// Can hold id of temporary texture if the texture from the path is not ready yet.
        /// </summary>
        public int TextureId
        {
            get => Volatile.Read(ref textureId);
            set => Volatile.Write(ref textureId, value);
        }

        /// <summary>
        /// Texture path. Used to identify the texture.
        /// </summary>
        public required string TexturePath { get; init; }

        private int textureId;
    }

    /// <summary>
    /// Represents an internal structure for managing texture-related data and states.
    /// </summary>
    private class Entry
    {
        public int UsagesCount;
        public Task? LoadingTask;
        public CancellationTokenSource? LoadingCts;
        public required TextureData PublicTextureData { get; init; }
        public InitTextureCallback? InitCallback { get; set; }

        public bool IsLoaded
        {
            get => Volatile.Read(ref isLoaded);
            set => Volatile.Write(ref isLoaded, value);
        }

        private bool isLoaded;
    }

    private class PendingLoadResult
    {
        public string Path = null!;
        public byte[] PixelData = null!;
        public int Width;
        public int Height;
        public PixelInternalFormat InternalFormat;
        public PixelFormat Format;
    }

    private static readonly ConcurrentDictionary<string, Entry> TextureDataDict = new();
    private static readonly ConcurrentQueue<PendingLoadResult> PendingUploads = new();
    private static int? PlaceholderTextureId;

    /// <summary>
    /// Represents a delegate defining a callback to be invoked during the initialization
    /// or setup of texture resources.
    /// </summary>
    public delegate void InitTextureCallback();

    /// <summary>
    /// Call on the G thread each frame to finalize background loads.
    /// </summary>
    public static void ProcessPendingUploads()
    {
        while (PendingUploads.TryDequeue(out var result))
        {
            if (!TextureDataDict.TryGetValue(result.Path, out var entry))
            {
                continue;
            }

            lock (entry)
            {
                if (!TextureDataDict.TryGetValue(result.Path, out var current) || !ReferenceEquals(current, entry))
                {
                    continue;
                }

                if (entry.IsLoaded) continue;

                GL.CreateTextures(TextureTarget.Texture2D, 1, out int textureId);
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.TexImage2D(TextureTarget.Texture2D, 0, result.InternalFormat, result.Width, result.Height, 0,
                    result.Format, PixelType.UnsignedByte, result.PixelData);
                entry.InitCallback?.Invoke();

                entry.PublicTextureData.TextureId = textureId;
                entry.IsLoaded = true;
            }

            GlHelper.CheckGlError("TexturesManager::ProcessPendingUploads");
        }
    }

    private static void EnsurePlaceholderCreated()
    {
        if (PlaceholderTextureId.HasValue) return;

        GL.CreateTextures(TextureTarget.Texture2D, 1, out int texId);
        GL.BindTexture(TextureTarget.Texture2D, texId);

        byte[] pixels =
        [
            255, 0, 255, 255, 0, 0, 0, 255,
            0, 0, 0, 255, 255, 0, 255, 255
        ];

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 2, 2, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        PlaceholderTextureId = texId;
        GlHelper.CheckGlError("TexturesManager::EnsurePlaceholderCreated");
    }

    /// <summary>
    /// Starts the asynchronous loading process for a texture.
    /// Converts PSD files to PNG if necessary and prepares pixel data for rendering.
    /// </summary>
    /// <param name="texturePath">The file path of the texture to load.</param>
    /// <param name="entry">The internal entry object that holds texture-related data and state.</param>
    private static void EnqueueLoad(string texturePath, Entry entry)
    {
        entry.LoadingCts = new CancellationTokenSource();
        var token = entry.LoadingCts.Token;

        entry.LoadingTask = Task.Run(() =>
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var pathToLoad = texturePath;

                if (Path.GetExtension(texturePath).Equals(".psd", StringComparison.InvariantCultureIgnoreCase))
                {
                    var pngPath = Path.ChangeExtension(texturePath, ".png");
                    if (!File.Exists(pngPath))
                    {
                        Debug.WriteLine($"Converting PSD to PNG ({texturePath})");
                        using var magickImage = new MagickImage(texturePath);
                        magickImage.Format = MagickFormat.Png;
                        magickImage.Write(pngPath);
                    }
                    else
                    {
                        Debug.WriteLine($"Loading previously converted PNG ({pngPath})");
                    }

                    pathToLoad = pngPath;
                }

                token.ThrowIfCancellationRequested();

                using var image = Image.Load<Rgba32>(pathToLoad);
                image.Mutate(x => x.Flip(FlipMode.Vertical));

                token.ThrowIfCancellationRequested();

                var pixelData = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(pixelData);

                var result = new PendingLoadResult
                {
                    Path = texturePath,
                    PixelData = pixelData,
                    Width = image.Width,
                    Height = image.Height,
                    InternalFormat = PixelInternalFormat.Rgba8,
                    Format = PixelFormat.Rgba
                };

                token.ThrowIfCancellationRequested();
                PendingUploads.Enqueue(result);
            }
            catch (OperationCanceledException)
            {
                // cancelled
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load texture '{texturePath}': {ex}");
            }
            finally
            {
                entry.LoadingCts?.Dispose();
                entry.LoadingCts = null;
            }
        }, token);
    }

    /// <summary>
    /// Get the Texture. If already loaded returns the real texture. If not ready, returns a placeholder immediately
    /// and schedules a background load. Call ProcessPendingUploads() on the GL thread to upload completed loads.
    /// </summary>
    public static TextureData GetOrLoadTexture(string texturePath, InitTextureCallback initCallback)
    {
        EnsurePlaceholderCreated();

        var entry = TextureDataDict.GetOrAdd(texturePath, path =>
        {
            var td = new TextureData
            {
                TextureId = PlaceholderTextureId!.Value,
                TexturePath = path
            };

            var e = new Entry
            {
                PublicTextureData = td,
                UsagesCount = 0,
                InitCallback = initCallback,
                IsLoaded = false
            };

            EnqueueLoad(path, e);
            return e;
        });

        Interlocked.Increment(ref entry.UsagesCount);

        return entry.PublicTextureData;
    }

    public static void FreeTexture(string textureName)
    {
        if (!TextureDataDict.TryGetValue(textureName, out var entry))
        {
            throw new InvalidOperationException("Texture Not Initialized");
        }

        var newCount = Interlocked.Decrement(ref entry.UsagesCount);

        if (newCount < 0)
        {
            Interlocked.Increment(ref entry.UsagesCount);
            throw new InvalidOperationException("Released more times than freed!");
        }

        if (newCount != 0) return;

        // ensure single thread deletes resources
        lock (entry)
        {
            if (entry.UsagesCount != 0) return;

            TextureDataDict.TryRemove(textureName, out _);

            entry.LoadingCts?.Cancel();

            if (entry.IsLoaded)
            {
                // exchange to zero atomically if you want to avoid double-delete elsewhere:
                var idToDelete = entry.PublicTextureData.TextureId;
                if (idToDelete != PlaceholderTextureId)
                {
                    GL.DeleteTexture(idToDelete);
                }
            }

            GlHelper.CheckGlError("TexturesManager::FreeTexture");
        }
    }

    /// <summary>
    /// Abort all background loading tasks. 
    /// Cancels per-entry loading token sources and drains the pending upload queue.
    /// Does not delete GPU textures - GL deletions should still run on the GL thread if needed.
    /// This can leave some texture in zombie like state, where they will not be loaded until all of its instances are manually freed. 
    /// </summary>
    public static void AbortAllLoads()
    {
        // Cancel all running loading tasks
        foreach (var kv in TextureDataDict)
        {
            var entry = kv.Value;
            lock (entry)
            {
                entry.LoadingCts?.Cancel();
            }
        }

        // Drain pending uploads to release memory and prevent further GL uploads
        while (PendingUploads.TryDequeue(out _))
        {
        }
    }
}