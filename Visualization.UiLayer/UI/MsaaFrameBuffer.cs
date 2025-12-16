using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core;

namespace Visualization.UiLayer.UI;

public class MsaaFrameBuffer : IDisposable, IBindable
{
    public int FboId { get; private set; }
    public int TextureId { get; private set; }
    public int DepthBufferId { get; private set; }

    private int _width;
    private int _height;
    private readonly int _samples;

    public MsaaFrameBuffer(int width, int height, int samples)
    {
        this._width = width;
        this._height = height;
        this._samples = samples;
        SetupFbo();
    }

    private void SetupFbo()
    {
        FboId = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FboId);

        TextureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DMultisample, TextureId);
        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, _samples, PixelInternalFormat.Rgba,
            _width, _height, true);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2DMultisample, TextureId, 0);

        DepthBufferId = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBufferId);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _samples,
            RenderbufferStorage.Depth24Stencil8, _width, _height);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
            RenderbufferTarget.Renderbuffer, DepthBufferId);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Debug.WriteLine($"Error creating MSAA FBO: {status}");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Resize(int width, int height)
    {
        if (width == this._width && height == this._height) return;

        this._width = width;
        this._height = height;

        GL.BindTexture(TextureTarget.Texture2DMultisample, TextureId);
        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, _samples, PixelInternalFormat.Rgba,
            _width, _height, true);

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBufferId);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _samples,
            RenderbufferStorage.Depth24Stencil8, _width, _height);

        GL.BindTexture(TextureTarget.Texture2DMultisample, 0);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
    }

    public void BlitTo(WindowFrameBuffer target)
    {
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FboId);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, target.FboId);
        GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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