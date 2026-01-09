using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using ImageMagick;

using OpenTK.Graphics.OpenGL4;

using StbImageSharp;

namespace Visualisation.Core.Display.Texture;

public static class TexturesManager
{
    static TexturesManager()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    /// <summary>
    /// Encapsulates texture-related metadata and state information, including
    /// unique texture identification and file path.
    /// </summary>
    public sealed class TextureData
    {
        public int TextureId;

        /// <summary>
        /// Texture path. Used to identify the texture.
        /// </summary>
        public required string TexturePath { get; init; }
    }

    /// <summary>
    /// Represents an internal structure for managing texture-related data and states.
    /// </summary>
    private sealed class Entry
    {
        public int UsagesCount;
        public required Task LoadingTask;
        public CancellationTokenSource? LoadingCts;
        public required TextureData PublicTextureData { get; init; }

        public InitTextureCallback? InitCallback;
        public bool IsLoaded;
    }

    private interface IPixelData
    {
        public void UploadToGpu(
            TextureTarget target,
            int level,
            PixelInternalFormat internalformat,
            int width,
            int height,
            int border,
            PixelFormat format);
    }

    private class PixelDataByte : IPixelData
    {
        private readonly byte[] _data;

        public PixelDataByte(byte[] data)
        {
            this._data = data;
        }

        public void UploadToGpu(
            TextureTarget target,
            int level,
            PixelInternalFormat internalformat,
            int width,
            int height,
            int border,
            PixelFormat format)
        {
            GL.TexImage2D(target, level, internalformat, width, height, border, format, PixelType.UnsignedByte,
                _data);
        }
    }

    private sealed class PixelDataFloat : IPixelData
    {
        private readonly float[] _data;

        public PixelDataFloat(float[] data)
        {
            this._data = data;
        }

        public void UploadToGpu(
            TextureTarget target,
            int level,
            PixelInternalFormat internalformat,
            int width,
            int height,
            int border,
            PixelFormat format)
        {
            GL.TexImage2D(target, level, internalformat, width, height, border, format, PixelType.Float, _data);
        }
    }

    private sealed class PendingLoadResult
    {
        public string Path = null!;
        public IPixelData PixelData = null!;
        public int Width;
        public int Height;
        public PixelInternalFormat InternalFormat;
        public PixelFormat Format;
    }

    private static readonly ConcurrentDictionary<string, Entry> TextureDataDict = new();
    private static readonly ConcurrentQueue<PendingLoadResult> PendingUploads = new();
    private static readonly SemaphoreSlim LoadingSemaphore = new(1, 1);

    private static readonly Lock PlaceholderCreateLock = new();
    private static int s_placeholderTextureId = -1; // value of -1 means not created yet

    public static int PlaceholderTextureId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsurePlaceholderCreated();
            return s_placeholderTextureId;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPlaceholderTexture(TextureData textureData)
    {
        var result = s_placeholderTextureId != -1 && textureData.TextureId == s_placeholderTextureId;
        return result;
    }

    /// <summary>
    /// Represents a delegate defining a callback to be invoked during the initialization
    /// or setup of texture resources.
    /// </summary>
    public delegate void InitTextureCallback();

    /// <summary>
    /// Call on the GL thread each frame to finalize background loads.
    /// </summary>
    public static void ProcessPendingUploads()
    {
        while (PendingUploads.TryDequeue(out var result))
        {
            if (!TextureDataDict.TryGetValue(result.Path, out var entry))
            {
                // No entry found for this texture path; it may have been freed already.
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
                result.PixelData.UploadToGpu(TextureTarget.Texture2D, 0, result.InternalFormat, result.Width,
                    result.Height, 0, result.Format);
                entry.InitCallback?.Invoke();

                entry.PublicTextureData.TextureId = textureId;
                entry.IsLoaded = true;
            }

            GlHelper.CheckGlError("TexturesManager::ProcessPendingUploads");
        }
    }

    private static void EnsurePlaceholderCreated()
    {
        if (s_placeholderTextureId != -1)
            return;

        lock (PlaceholderCreateLock)
        {
            if (s_placeholderTextureId != -1)
                return;

            GL.CreateTextures(TextureTarget.Texture2D, 1, out int texId);
            GL.BindTexture(TextureTarget.Texture2D, texId);

            byte[] pixels =
            [
                255, 0, 255, 255, 0, 0, 0, 255,
                0, 0, 0, 255, 255, 0, 255, 255
            ];

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 2, 2, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            s_placeholderTextureId = texId;
            GlHelper.CheckGlError("TexturesManager::EnsurePlaceholderCreated");
        }
    }

    private static async Task<PendingLoadResult> LoadPixelDataAsync(
        string texturePath,
        CancellationToken cancellationToken)
    {
        var pathToLoad = texturePath;

        if (Path.GetExtension(texturePath).Equals(".psd", StringComparison.InvariantCultureIgnoreCase))
        {
            var pngPath = Path.ChangeExtension(texturePath, ".png");
            if (!File.Exists(pngPath))
            {
                Debug.WriteLine($"Converting PSD to PNG ({texturePath})");
                using var magickImage = new MagickImage(texturePath);
                magickImage.Format = MagickFormat.Png;
                await magickImage.WriteAsync(pngPath, cancellationToken);
            }
            else
            {
                Debug.WriteLine($"Loading previously converted PNG ({pngPath})");
            }

            pathToLoad = pngPath;
        }

        if (Path.GetExtension(pathToLoad).Equals(".exr", StringComparison.InvariantCultureIgnoreCase))
        {
            var hdrPath = Path.ChangeExtension(pathToLoad, ".hdr");
            if (!File.Exists(hdrPath))
            {
                Debug.WriteLine($"Converting EXR to HDR ({pathToLoad})");
                using var magickImage = new MagickImage(pathToLoad);
                magickImage.Format = MagickFormat.Hdr;
                await magickImage.WriteAsync(hdrPath, cancellationToken);
            }
            else
            {
                Debug.WriteLine($"Loading previously converted HDR ({hdrPath})");
            }

            pathToLoad = hdrPath;
        }

        var extension = Path.GetExtension(pathToLoad);
        if (extension.Equals(".hdr", StringComparison.InvariantCultureIgnoreCase))
        {
            await using var stream = new FileStream(pathToLoad, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                useAsync: true);
            var buffer = new byte[stream.Length];
            await stream.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken);

            using var memoryStream = new MemoryStream(buffer);
            var image = ImageResultFloat.FromStream(memoryStream, ColorComponents.RedGreenBlueAlpha);
            return new PendingLoadResult
            {
                Path = texturePath,
                PixelData = new PixelDataFloat(image.Data),
                Width = image.Width,
                Height = image.Height,
                InternalFormat = PixelInternalFormat.Rgba32f,
                Format = PixelFormat.Rgba
            };
        }

        await using (var stream = new FileStream(pathToLoad, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
            useAsync: true))
        {
            var buffer = new byte[stream.Length];
            await stream.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken);

            using var memoryStream = new MemoryStream(buffer);
            var image = ImageResult.FromStream(memoryStream, ColorComponents.RedGreenBlueAlpha);

            return new PendingLoadResult
            {
                Path = texturePath,
                PixelData = new PixelDataByte(image.Data),
                Width = image.Width,
                Height = image.Height,
                InternalFormat = PixelInternalFormat.Rgba8,
                Format = PixelFormat.Rgba
            };
        }
    }

    private static PendingLoadResult LoadPixelData(string texturePath)
    {
        return LoadPixelDataAsync(texturePath, CancellationToken.None).GetAwaiter().GetResult();
    }


    private static int PerformImmediateLoad(string path, InitTextureCallback initCallback)
    {
        int textureId = -1;
        try
        {
            var result = LoadPixelData(path);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out textureId);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            result.PixelData.UploadToGpu(TextureTarget.Texture2D, 0, result.InternalFormat, result.Width, result.Height,
                0,
                result.Format);

            initCallback.Invoke();

            GlHelper.CheckGlError("TexturesManager::PerformImmediateLoad");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load texture '{path}' immediately: {ex}");
        }

        if (textureId == -1)
        {
            textureId = s_placeholderTextureId;
        }

        return textureId;
    }

    /// <summary>
    /// Starts the asynchronous loading process for a texture.
    /// Converts PSD files to PNG if necessary and prepares pixel data for rendering.
    /// Loading is limited to one texture at a time to avoid overwhelming the system.
    /// </summary>
    /// <param name="texturePath">The file path of the texture to load.</param>
    private static (CancellationTokenSource cts, Task task) EnqueueLoad(string texturePath)
    {
        CancellationTokenSource cts = new();
        var token = cts.Token;

        var task = Task.Run(async () =>
        {
            try
            {
                if (token.IsCancellationRequested)
                    return;

                await LoadingSemaphore.WaitAsync(token);
                try
                {
                    if (token.IsCancellationRequested)
                        return;

                    var result = await LoadPixelDataAsync(texturePath, token);

                    if (token.IsCancellationRequested)
                        return;

                    PendingUploads.Enqueue(result);
                }
                finally
                {
                    LoadingSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load texture '{texturePath}': {ex}");
            }
        }, token);
        return (cts, task);
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
            var td = new TextureData { TextureId = s_placeholderTextureId, TexturePath = path };

            var (cts, task) = EnqueueLoad(path);
            var e = new Entry
            {
                PublicTextureData = td,
                UsagesCount = 0,
                InitCallback = initCallback,
                IsLoaded = false,
                LoadingTask = task,
                LoadingCts = cts
            };

            return e;
        });

        lock (entry)
        {
            entry.UsagesCount++;
        }

        return entry.PublicTextureData;
    }

    /// <summary>
    /// Get the Texture. If already loaded returns the real texture. If not ready, loads the texture on the current thread.
    /// This function must be called from the GL thread.
    /// If an async load is in progress for the same texture, it will wait for it to complete and then process the upload.
    /// </summary>
    public static TextureData LoadTextureImmediately(string texturePath, InitTextureCallback initCallback)
    {
        EnsurePlaceholderCreated();

        // Try to get or add the entry for the texture path
        // If it doesn't exist, create a new entry and load the texture immediately
        // If it exists, check if it's already loaded or wait for any ongoing load to complete
        var entry = TextureDataDict.GetOrAdd(texturePath, path =>
        {
            int textureId = PerformImmediateLoad(path, initCallback);
            var td = new TextureData { TextureId = textureId, TexturePath = path };
            var e = new Entry
            {
                PublicTextureData = td,
                UsagesCount = 0,
                InitCallback = initCallback,
                IsLoaded = true,
                LoadingTask = Task.CompletedTask,
                LoadingCts = null
            };

            return e;
        });

        // Bool and int32 operations are atomic, so we can check without locking first
        if (entry.IsLoaded)
        {
            entry.UsagesCount++;
            return entry.PublicTextureData;
        }

        lock (entry)
        {
            var loadingTask = entry.LoadingTask;
            try
            {
                loadingTask.Wait();
            }
            // TODO: what to do on failure here?
            catch (Exception ex)
            {
                Debug.WriteLine($"Waiting on texture load task failed for '{texturePath}': {ex}");
            }

            ProcessPendingUploads();
        }

        return entry.PublicTextureData;
    }

    public static void FreeTexture(TextureData textureData)
    {
        FreeTexture(textureData.TexturePath);
    }

    public static void FreeTexture(string textureName)
    {
        if (!TextureDataDict.TryGetValue(textureName, out var entry))
        {
            throw new InvalidOperationException("Texture Not Initialized");
        }

        entry.UsagesCount--;
        if (entry.UsagesCount < 0)
        {
            throw new InvalidOperationException("Released more times than freed!");
        }

        if (entry.UsagesCount != 0) return;

        // ensure single thread deletes resources
        lock (entry)
        {
            if (entry.UsagesCount != 0) return;

            TextureDataDict.TryRemove(textureName, out _);

            entry.LoadingCts?.Cancel();

            if (entry.IsLoaded)
            {
                var idToDelete = entry.PublicTextureData.TextureId;
                if (idToDelete != s_placeholderTextureId)
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
    /// This can leave some texture in a zombie-like state, where they will not be loaded until all of its instances are manually freed. 
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