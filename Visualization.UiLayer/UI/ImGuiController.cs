using System.Runtime.CompilerServices;

using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Visualisation.Core;

//
// based on
// https://github.com/NogginBops/ImGui.NET_OpenTK_Sample
// 
// MIT License
// 
// Copyright (c) 2025 Julius Häger
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
namespace Visualization.UiLayer.UI;

public sealed class ImGuiController : IDisposable
{
    private readonly Shader _shader = new("imguiShader.vert", "imguiShader.frag");

    private bool _frameBegun;

    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;

    private int _fontTexture;
    public ImFontPtr FontRegular { get; private set; }
    public ImFontPtr FontBold { get; private set; }
    public ImFontPtr FontItalic { get; private set; }
    public ImFontPtr FontBoldItalic { get; private set; }

    /// <summary>
    /// Helper to use a specific font. Must be followed by PopFont() or wrapped in a using statement.
    /// Example: ImGui.PushFont(controller.FontBold); ImGui.Text("Bold"); ImGui.PopFont();
    /// </summary>
    public void PushRegularFont() => ImGui.PushFont(FontRegular);

    /// <summary>
    /// Helper to use a specific font. Must be followed by PopFont() or wrapped in a using statement.
    /// </summary>
    public void PushBoldFont() => ImGui.PushFont(FontBold);

    /// <summary>
    /// Helper to use a specific font. Must be followed by PopFont() or wrapped in a using statement.
    /// </summary>
    public void PushItalicFont() => ImGui.PushFont(FontItalic);

    /// <summary>
    /// Helper to use a specific font. Must be followed by PopFont() or wrapped in a using statement.
    /// </summary>
    public void PushBoldItalicFont() => ImGui.PushFont(FontBoldItalic);


    public Vector2i FramebufferSize { get; private set; }

    // logical window size (in window units / logical pixels)
    public Vector2i WindowSize { get; private set; }


    // scale from logical/window coordinates to framebuffer pixels (fb / logical)
    public Vector2 ScaleFactor { get; private set; } = Vector2.One;
    public int GlVersion { get; private set; }

    private static bool s_khrDebugAvailable;

    private bool _compatibilityProfile;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public ImGuiController(GameWindow window)
    {
        WindowSize = window.Size;
        FramebufferSize = window.FramebufferSize;
        UpdateScaleFactor();

        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);

        GlVersion = major * 100 + minor * 10;

        s_khrDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

        _compatibilityProfile =
            (GL.GetInteger((GetPName)All.ContextProfileMask) & (int)All.ContextCompatibilityProfileBit) != 0;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        string fontFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JetBrainsMono-2.304", "fonts", "ttf");
        if (Directory.Exists(fontFolder))
        {
            // The first loaded font becomes the default font
            FontBold = io.Fonts.AddFontFromFileTTF(Path.Combine(fontFolder, "JetBrainsMono-Bold.ttf"), 16.0f);
            FontRegular = io.Fonts.AddFontFromFileTTF(Path.Combine(fontFolder, "JetBrainsMono-Regular.ttf"), 16.0f);
            FontItalic = io.Fonts.AddFontFromFileTTF(Path.Combine(fontFolder, "JetBrainsMono-Italic.ttf"), 16.0f);
            FontBoldItalic =
                io.Fonts.AddFontFromFileTTF(Path.Combine(fontFolder, "JetBrainsMono-BoldItalic.ttf"), 16.0f);
        }
        else
        {
            io.Fonts.AddFontDefault();
        }

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void HookToWindow(GameWindow window)
    {
        window.MouseMove += MouseMove;
        window.MouseDown += MouseDown;
        window.MouseUp += MouseUp;
        window.MouseWheel += MouseScroll;

        window.KeyDown += KeyDown;
        window.KeyUp += KeyUp;
        window.TextInput += TextInput;

        window.Resize += Resize;
        window.FramebufferResize += FramebufferResize;

        WindowSize = window.Size;
        FramebufferSize = window.FramebufferSize;
        UpdateScaleFactor();
    }

    public void UnhookFromWindow(GameWindow window)
    {
        window.MouseMove -= MouseMove;
        window.MouseDown -= MouseDown;
        window.MouseUp -= MouseUp;
        window.MouseWheel -= MouseScroll;

        window.KeyDown -= KeyDown;
        window.KeyUp -= KeyUp;
        window.TextInput -= TextInput;

        window.Resize -= Resize;
        window.FramebufferResize -= FramebufferResize;
    }

    public void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        int prevVao = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);
        LabelObject(ObjectLabelIdentifier.VertexArray, _vertexArray, "ImGui");

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();
        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);

        GlHelper.CheckGlError("End of ImGui setup");
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
        LabelObject(ObjectLabelIdentifier.Texture, _fontTexture, "ImGui Text Atlas");

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte,
            pixels);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);

        // Restore state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID(_fontTexture);

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// </summary>
    public void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        if (_frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData(deltaSeconds);

        _frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(WindowSize.X, WindowSize.Y);
        io.DisplayFramebufferScale = new System.Numerics.Vector2(ScaleFactor.X, ScaleFactor.Y);
        io.DeltaTime = deltaSeconds;
    }

    public void KeyDown(KeyboardKeyEventArgs e)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.AddKeyEvent(TranslateKey(e.Key), true);

        io.KeyCtrl = e.Control;
        io.KeyAlt = e.Alt;
        io.KeyShift = e.Shift;
        // io.KeySuper = e.IsSuper;
    }

    public void KeyUp(KeyboardKeyEventArgs e)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.AddKeyEvent(TranslateKey(e.Key), false);

        io.KeyCtrl = e.Control;
        io.KeyAlt = e.Alt;
        io.KeyShift = e.Shift;
        // io.KeySuper = e.IsSuper;
    }

    public void MouseMove(MouseMoveEventArgs e)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.MousePos = new System.Numerics.Vector2(e.X, e.Y);
    }

    public void MouseDown(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        if (e.Button == MouseButton.Left) io.MouseDown[0] = true;
        if (e.Button == MouseButton.Right) io.MouseDown[1] = true;
        if (e.Button == MouseButton.Middle) io.MouseDown[2] = true;
        if (e.Button == MouseButton.Button4) io.MouseDown[3] = true;
        if (e.Button == MouseButton.Button5) io.MouseDown[4] = true;
    }

    public void MouseUp(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        if (e.Button == MouseButton.Left) io.MouseDown[0] = false;
        if (e.Button == MouseButton.Right) io.MouseDown[1] = false;
        if (e.Button == MouseButton.Middle) io.MouseDown[2] = false;
    }

    public void MouseScroll(MouseWheelEventArgs e)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.MouseWheel += e.OffsetY;
        io.MouseWheelH += e.OffsetX;
    }

    public void TextInput(TextInputEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddInputCharacter((char)e.Unicode);
    }

    public void Resize(ResizeEventArgs e)
    {
        WindowSize = (e.Width, e.Height);

        UpdateScaleFactor();
    }

    private void FramebufferResize(FramebufferResizeEventArgs e)
    {
        FramebufferSize = (e.Width, e.Height);

        UpdateScaleFactor();
    }

    private void UpdateScaleFactor()
    {
        if (WindowSize is { X: > 0, Y: > 0 })
        {
            ScaleFactor = FramebufferSize / WindowSize;
        }
        else
        {
            ScaleFactor = Vector2.One;
        }
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        GlHelper.CheckGlError("Start Projection (RenderImDrawData imGuiController)");
        if (drawData.CmdListsCount == 0)
        {
            return;
        }

        // Get intial state.
        int prevVao = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
        int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
        bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
        Span<int> prevScissorBox = stackalloc int[4];
        unsafe
        {
            fixed (int* iptr = &prevScissorBox[0])
            {
                GL.GetInteger(GetPName.ScissorBox, iptr);
            }
        }

        Span<int> prevPolygonMode = stackalloc int[2];
        unsafe
        {
            fixed (int* iptr = &prevPolygonMode[0])
            {
                GL.GetInteger(GetPName.PolygonMode, iptr);
            }
        }

        if (GlVersion <= 310 || _compatibilityProfile)
        {
            GL.PolygonMode(TriangleFace.Front, PolygonMode.Fill);
            GL.PolygonMode(TriangleFace.Back, PolygonMode.Fill);
        }
        else
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        }

        // Bind the element buffer so that we can resize it.
        GL.BindVertexArray(_vertexArray);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            int vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();

            if (vertexSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);

                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _vertexBufferSize = newSize;
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _indexBufferSize = newSize;
            }
        }

        // Set up the orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();
        // Compute framebuffer size from logical DisplaySize and DisplayFramebufferScale
        int fbWidth = (int)(io.DisplaySize.X * io.DisplayFramebufferScale.X);
        int fbHeight = (int)(io.DisplaySize.Y * io.DisplayFramebufferScale.Y);

        // Use framebuffer pixel dimensions for the projection so GL coordinates match vertex positions
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            fbWidth,
            fbHeight,
            0.0f,
            -1.0f,
            1.0f);

        _shader.Use();
        _shader.SetMatrix4("projection_matrix", mvp);
        _shader.SetInt("in_fontTexture", 0);
        GlHelper.CheckGlError("Projection");

        GL.BindVertexArray(_vertexArray);
        GlHelper.CheckGlError("VAO");

        // clip rects are expected to be scaled from logical to framebuffer pixels by ImGui using DisplayFramebufferScale
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        // Render command lists
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);
            GlHelper.CheckGlError($"Data Vert {n}");

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmdList.IdxBuffer.Size * sizeof(ushort),
                cmdList.IdxBuffer.Data);
            GlHelper.CheckGlError($"Data Idx {n}");

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                GlHelper.CheckGlError("Texture");

                // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                var clip = pcmd.ClipRect;
                // Use framebuffer height for flipping Y
                GL.Scissor((int)clip.X, fbHeight - (int)clip.W, (int)(clip.Z - clip.X),
                    (int)(clip.W - clip.Y));
                GlHelper.CheckGlError("Scissor");

                if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount,
                        DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)),
                        unchecked((int)pcmd.VtxOffset));
                }
                else
                {
                    GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (int)pcmd.IdxOffset * sizeof(ushort));
                }

                GlHelper.CheckGlError("Draw");
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);

        // Reset state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);
        GL.UseProgram(prevProgram);
        GL.BindVertexArray(prevVao);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb,
            (BlendEquationMode)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)prevBlendFuncSrcRgb,
            (BlendingFactorDest)prevBlendFuncDstRgb,
            (BlendingFactorSrc)prevBlendFuncSrcAlpha,
            (BlendingFactorDest)prevBlendFuncDstAlpha);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend);
        else GL.Disable(EnableCap.Blend);
        if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace);
        else GL.Disable(EnableCap.CullFace);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest);
        else GL.Disable(EnableCap.ScissorTest);
        if (GlVersion <= 310 || _compatibilityProfile)
        {
            GL.PolygonMode(TriangleFace.Front, (PolygonMode)prevPolygonMode[0]);
            GL.PolygonMode(TriangleFace.Back, (PolygonMode)prevPolygonMode[1]);
        }
        else
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, (PolygonMode)prevPolygonMode[0]);
        }
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);

        GL.DeleteTexture(_fontTexture);
        _shader.Dispose();
    }

    public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
    {
        if (s_khrDebugAvailable)
            GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
    }

    static bool IsExtensionSupported(string name)
    {
        int n = GL.GetInteger(GetPName.NumExtensions);
        for (int i = 0; i < n; i++)
        {
            string extension = GL.GetString(StringNameIndexed.Extensions, i);
            if (extension == name) return true;
        }

        return false;
    }

    public static ImGuiKey TranslateKey(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F24)
            return key - Keys.F1 + ImGuiKey.F1;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            case Keys.LeftShift: return ImGuiKey.LeftShift;
            case Keys.LeftControl: return ImGuiKey.LeftCtrl;
            case Keys.LeftAlt: return ImGuiKey.LeftAlt;
            case Keys.LeftSuper: return ImGuiKey.LeftSuper;
            case Keys.RightShift: return ImGuiKey.RightShift;
            case Keys.RightControl: return ImGuiKey.RightCtrl;
            case Keys.RightAlt: return ImGuiKey.RightAlt;
            case Keys.RightSuper: return ImGuiKey.RightSuper;
            case Keys.Menu: return ImGuiKey.Menu;
            default: return ImGuiKey.None;
        }
    }
}