using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Visualization.Display.Inputs;
using Visualization.Scenes;

namespace Visualization.Applications
{
    public class Application : GameWindow
    {
        public readonly IInputProvider InputProvider;
        protected SceneManager Scene = null!;

        protected const int DefaultWidth = 800;
        protected const int DefaultHeight = 600;
        protected const string DefaultTitle = "Display";
        public Application(int width = DefaultWidth, int height = DefaultHeight, string title = DefaultTitle) : base(GameWindowSettings.Default,
            new NativeWindowSettings())
        {
            Size = (width, height);
            Title = title;
            InputProvider = new OpenTkInputProvider(this);
        }

        protected virtual void Update(float deltaTime)
        {
            
        }

        protected static bool CameraMode { get; set; } = false;
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            
            if (!IsFocused) // check to see if the window is focused
            {
                if (CameraMode)
                {
                    CameraMode = false;
                }
                return;
            }
            
            // update CameraMode
            if (CameraMode && InputProvider.IsKeyDown(InputKey.Escape))
            {
                InputProvider.SetCursorState(Display.Inputs.CursorState.Normal);
                CameraMode = false;
            }
            else if (!CameraMode)
            {
                if (InputProvider.IsMouseButtonPressed(MouseButton.Left))
                {
                    InputProvider.SetCursorState(Display.Inputs.CursorState.Grabbed);
                    CameraMode = true;
                }
            }
            
            if (CameraMode)
            {
                Scene.Camera.ProcessInput(InputProvider, (float)e.Time);
            }

            InputProvider.UpdateMousePosition();
        }

        /// <summary>
        /// This method is called after doing some initial setup.
        /// It can overriden to add game objects to the initial scene. 
        /// </summary>
        protected virtual void InitializeScene()
        {
            
        }
        

        protected override void OnLoad()
        {
            base.OnLoad();
            
            GL.ClearColor(0.2f, 0.3f, 0.5f, 1f);
            GL.Enable(EnableCap.DepthTest);
 
            Scene = new SceneLightningOnly(Size.X / (float)Size.Y);
            Scene.SetUp();
            if (CameraMode)
            {
                InputProvider.SetCursorState(Display.Inputs.CursorState.Grabbed);
            }

            InitializeScene();
            Scene.Init();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            
            Update((float)args.Time);
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Scene.Render();
            SwapBuffers();
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            // Unbind all the resources by binding the targets to 0/null.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            
            Scene.Dispose();
            
            base.OnUnload();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            Scene.Camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            
            if (Scene.Camera != null)
            {
                Scene.Camera.AspectRatio = Size.X / (float)Size.Y;
            }
        }
    }
}
