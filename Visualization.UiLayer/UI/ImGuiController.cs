using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

// based on
// https://github.com/NogginBops/ImGui.NET_OpenTK_Sample

namespace Visualization.UiLayer.UI;

public class ImGuiController : IDisposable
{
    private bool frameBegun;

    private int vertexArray;
    private int vertexBuffer;
    private int vertexBufferSize;
    private int indexBuffer;
    private int indexBufferSize;

    private int fontTexture;

    private int shader;
    private int shaderFontTextureLocation;
    private int shaderProjectionMatrixLocation;

    // window framebuffer size in pixels (used for GL projection/scissor)
    private int framebufferWidth;

    private int framebufferHeight;

    // logical window size (in window units / logical pixels)
    private int windowWidth;
    private int windowHeight;

    // scale from logical/window coordinates to framebuffer pixels (fb / logical)
    private System.Numerics.Vector2 scaleFactor = System.Numerics.Vector2.One;

    // attached window reference so we can query sizes on resize events
    private GameWindow? attachedWindow = null;

    // expose framebuffer scale to callers (e.g. for FBO sizing)
    public System.Numerics.Vector2 DisplayFramebufferScale => scaleFactor;

    private static bool khrDebugAvailable = false;

    private int glVersion;
    private bool compatibilityProfile;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public ImGuiController(int width, int height)
    {
        windowWidth = width;
        windowHeight = height;

        framebufferWidth = width;
        framebufferHeight = height;

        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);

        glVersion = major * 100 + minor * 10;

        khrDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

        compatibilityProfile =
            (GL.GetInteger((GetPName)All.ContextProfileMask) & (int)All.ContextCompatibilityProfileBit) != 0;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        CreateDeviceResources();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
        frameBegun = true;
    }

    public void HookToWindow(GameWindow window)
    {
        attachedWindow = window;

        window.MouseMove += MouseMove;
        window.MouseDown += MouseDown;
        window.MouseUp += MouseUp;
        window.MouseWheel += MouseScroll;

        window.KeyDown += KeyDown;
        window.KeyUp += KeyUp;
        window.TextInput += TextInput;

        window.Resize += Resize;
        window.FramebufferResize += FramebufferResize;

        try
        {
            var fb = window.FramebufferSize;
            var sz = window.Size;
            if (sz.X > 0 && sz.Y > 0)
            {
                scaleFactor = new System.Numerics.Vector2(fb.X / (float)sz.X, fb.Y / (float)sz.Y);
            }
            else
            {
                scaleFactor = System.Numerics.Vector2.One;
            }

            windowWidth = sz.X;
            windowHeight = sz.Y;
            framebufferWidth = fb.X;
            framebufferHeight = fb.Y;
        }
        catch
        {
            scaleFactor = System.Numerics.Vector2.One;
        }
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
        vertexBufferSize = 10000;
        indexBufferSize = 2000;

        int prevVao = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(vertexArray);
        LabelObject(ObjectLabelIdentifier.VertexArray, vertexArray, "ImGui");

        vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        string vertexSource = @"#version 330 core

            uniform mat4 projection_matrix;

            layout(location = 0) in vec2 in_position;
            layout(location = 1) in vec2 in_texCoord;
            layout(location = 2) in vec4 in_color;

            out vec4 color;
            out vec2 texCoord;

            void main()
            {
                gl_Position = projection_matrix * vec4(in_position, 0, 1);
                color = in_color;
                texCoord = in_texCoord;
            }";
        string fragmentSource = @"#version 330 core

            uniform sampler2D in_fontTexture;

            in vec4 color;
            in vec2 texCoord;

            out vec4 outputColor;

            void main()
            {
                outputColor = color * texture(in_fontTexture, texCoord);
            }";

        shader = CreateProgram("ImGui", vertexSource, fragmentSource);
        shaderProjectionMatrixLocation = GL.GetUniformLocation(shader, "projection_matrix");
        shaderFontTextureLocation = GL.GetUniformLocation(shader, "in_fontTexture");

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);

        CheckGlError("End of ImGui setup");
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, fontTexture);
        GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
        LabelObject(ObjectLabelIdentifier.Texture, fontTexture, "ImGui Text Atlas");

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

        io.Fonts.SetTexID((IntPtr)fontTexture);

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// </summary>
    public void Render()
    {
        if (frameBegun)
        {
            frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        if (frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData(deltaSeconds);

        frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(windowWidth, windowHeight);
        io.DisplayFramebufferScale = scaleFactor;
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
        if (attachedWindow != null)
        {
            var fb = attachedWindow.FramebufferSize;
            if (e.Width > 0 && e.Height > 0)
            {
                scaleFactor = new System.Numerics.Vector2(fb.X / (float)e.Width, fb.Y / (float)e.Height);
            }

            windowWidth = e.Width;
            windowHeight = e.Height;
            framebufferWidth = fb.X;
            framebufferHeight = fb.Y;
        }
        else
        {
            windowWidth = e.Width;
            windowHeight = e.Height;
            framebufferWidth = e.Width;
            framebufferHeight = e.Height;
        }
    }

    private void FramebufferResize(FramebufferResizeEventArgs e)
    {
        framebufferWidth = e.Width;
        framebufferHeight = e.Height;

        if (attachedWindow != null && attachedWindow.Size.X > 0 && attachedWindow.Size.Y > 0)
        {
            scaleFactor = new System.Numerics.Vector2(e.Width / (float)attachedWindow.Size.X,
                e.Height / (float)attachedWindow.Size.Y);
        }
        else
        {
            scaleFactor = System.Numerics.Vector2.One;
        }
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
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

        if (glVersion <= 310 || compatibilityProfile)
        {
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
            GL.PolygonMode(MaterialFace.Back, PolygonMode.Fill);
        }
        else
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(vertexArray);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            int vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();

            if (vertexSize > vertexBufferSize)
            {
                int newSize = (int)Math.Max(vertexBufferSize * 1.5f, vertexSize);

                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                vertexBufferSize = newSize;
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > indexBufferSize)
            {
                int newSize = (int)Math.Max(indexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                indexBufferSize = newSize;
            }
        }

        // Setup orthographic projection matrix into our constant buffer
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

        GL.UseProgram(shader);
        GL.UniformMatrix4(shaderProjectionMatrixLocation, false, ref mvp);
        GL.Uniform1(shaderFontTextureLocation, 0);
        CheckGlError("Projection");

        GL.BindVertexArray(vertexArray);
        CheckGlError("VAO");

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
            CheckGlError($"Data Vert {n}");

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmdList.IdxBuffer.Size * sizeof(ushort),
                cmdList.IdxBuffer.Data);
            CheckGlError($"Data Idx {n}");

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                    CheckGlError("Texture");

                    // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                    var clip = pcmd.ClipRect;
                    // Use framebuffer height for flipping Y
                    GL.Scissor((int)clip.X, fbHeight - (int)clip.W, (int)(clip.Z - clip.X),
                        (int)(clip.W - clip.Y));
                    CheckGlError("Scissor");

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

                    CheckGlError("Draw");
                }
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
        if (glVersion <= 310 || compatibilityProfile)
        {
            GL.PolygonMode(MaterialFace.Front, (PolygonMode)prevPolygonMode[0]);
            GL.PolygonMode(MaterialFace.Back, (PolygonMode)prevPolygonMode[1]);
        }
        else
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, (PolygonMode)prevPolygonMode[0]);
        }
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(vertexArray);
        GL.DeleteBuffer(vertexBuffer);
        GL.DeleteBuffer(indexBuffer);

        GL.DeleteTexture(fontTexture);
        GL.DeleteProgram(shader);
    }

    public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
    {
        if (khrDebugAvailable)
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

    public static int CreateProgram(string name, string vertexSource, string fragmentSoruce)
    {
        int program = GL.CreateProgram();
        LabelObject(ObjectLabelIdentifier.Program, program, $"Program: {name}");

        int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSoruce);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);

        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetProgramInfoLog(program);
            Debug.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private static int CompileShader(string name, ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        LabelObject(ObjectLabelIdentifier.Shader, shader, $"Shader: {name}");

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            Debug.WriteLine($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
        }

        return shader;
    }

    public static void CheckGlError(string title)
    {
        ErrorCode error;
        int i = 1;
        while ((error = GL.GetError()) != ErrorCode.NoError)
        {
            Debug.Print($"{title} ({i++}): {error}");
        }
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