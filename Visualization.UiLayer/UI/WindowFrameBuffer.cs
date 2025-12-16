using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;

namespace Visualization.UiLayer.UI;

public class WindowFrameBuffer : IDisposable, IBindable
{
    public int FboId { get; private set; }
    public int TextureId { get; private set; }
    public int DepthBufferId { get; private set; }

    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    public WindowFrameBuffer(int width, int height)
    {
        this._width = width;
        this._height = height;
        SetupFbo();
    }

    private void SetupFbo()
    {
        FboId = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);

        TextureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, TextureId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, TextureId, 0);

        DepthBufferId = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBufferId);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, _width, _height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
            RenderbufferTarget.Renderbuffer, DepthBufferId);

        FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine($"Error creating FBO: {status}");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public void Resize(int width, int height)
    {
        if (width == this._width && height == this._height) return;

        this._width = width;
        this._height = height;

        GL.BindTexture(TextureTarget.Texture2D, TextureId);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, this._width, this._height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBufferId);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, this._width,
            this._height);

        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);
        GL.Viewport(0, 0, _width, _height);
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, _width, _height);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(FboId);
        GL.DeleteTexture(TextureId);
        GL.DeleteRenderbuffer(DepthBufferId);
    }
}