using Engine.Display.Inputs;
using Engine.Physics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector3 = OpenTK.Mathematics.Vector3;
using Matrix4 = OpenTK.Mathematics.Matrix4;

namespace Engine.Display
{

    public class Window : GameWindow
    {
        public readonly IInputProvider InputProvider;
        
        int vertexBufferObject;
        Shader shader;
        int vertexArrayObject;
        int elementBufferObject;
        Random r;
        Real secCounter = 0;
        ParticleForceRegistry particleForceRegistry = new();
        private Camera _camera;

        // TODO: REMOVE
        Particle[] particles = [];
        Physics.Vector3[] positions = [];


        float[] vertices =
        {
            0.5f, 0.5f, 0.0f, // top right
            0.5f, -0.5f, 0.0f, // bottom right
            -0.5f, -0.5f, 0.0f, // bottom left
            -0.5f, 0.5f, 0.0f // top left
        };

        uint[] indices =
        {
            // note that we start from 0!
            0, 1, 3, // first triangle
            1, 2, 3 // second triangle
        };

        public Window(int width, int height, string title) : base(GameWindowSettings.Default,
            new NativeWindowSettings())
        {
            Size = (width, height);
            Title = title;
            InputProvider = new OpenTkInputProvider(this);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            
            if (!IsFocused) // check to see if the window is focused
            {
                return;
            }

            if (InputProvider.IsKeyDown(InputKey.Escape))
            {
                Close();
            }
            
            _camera.ProcessInput(InputProvider, (float)e.Time);
            
            InputProvider.UpdateMousePosition();
        }


        protected override void OnLoad()
        {
            base.OnLoad();
            
            GL.ClearColor(0.2f, 0.3f, 0.5f, 1f);

            GL.Enable(EnableCap.DepthTest);

            vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);

            vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);

            var transform = Matrix4.Identity;

            transform = transform * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(20f));

            transform = transform * Matrix4.CreateScale(1.1f);

            transform = transform * Matrix4.CreateTranslation(0.1f, 0.1f, 0.0f);


            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
            // TODO: CHANGE BACK TO STATIC DRAW
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices,
                BufferUsageHint.DynamicDraw);

            // TODO: CHANGE BACK TO STATIC DRAW
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices,
                BufferUsageHint.DynamicDraw);

            // TODO: REMOVE
            r = new Random();
            positions =
            [
                new Physics.Vector3(0.5f, 0.5f), new Physics.Vector3(0.5f, -0.5f),
                new Physics.Vector3(0.5f, 0.5f, 0.0f), new Physics.Vector3(0.5f, -0.5f, 0.0f),
                new Physics.Vector3(-0.5f, -0.5f, 0.0f), new Physics.Vector3(-0.5f, 0.5f, 0.0f)
            ];
            Real[] dampingfactors = [0.99f, 0.99f, 0.99f, 0.99f];
            particles = [new Particle(), new Particle(), new Particle(), new Particle()];

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].position = positions[i];
                particles[i].damping = dampingfactors[i];
            }
            // TODO: REMOVE
            ParticleAnchoredSpring pas1 = new(positions[3], 1, 0.5f);
            ParticleAnchoredSpring pas2 = new(positions[2], 1, 0.5f);
            particleForceRegistry.Add(particles[0], pas1);
            particleForceRegistry.Add(particles[1], pas2);
            
            string fragmentPath = Shader.LoadShader("shader.frag");
            string vertexPath = Shader.LoadShader("shader.vert");

            shader = new(vertexPath, fragmentPath);
            shader.Use();
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);
            InputProvider.SetCursorState(Inputs.CursorState.Grabbed);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            secCounter += (Real)args.Time;

            // TODO: REMOVE
            //Vector3 force = new Vector3();
            //if (secCounter > 3.0)
            //{
            //    secCounter = 0.0f;
            //    force = Vector3.RandomVector(r, new(-50f, -50f, 0f), new(50f, 50f, 0f));
            //    Console.WriteLine($"[DEBUG]: Applying force: {force}");
            //}
            particleForceRegistry.UpdateForces((Real)args.Time);
            for (int i = 0; i < particles.Length; i++)
            {
               
                particles[i].Integrate((Real)args.Time);
                //particles[i].AddForce(force);

                positions[i] = particles[i].position;
                //Console.WriteLine(particles[i].position);
            }

            vertices = Physics.Vector3.ConcatAndNormalize(positions);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices,
                BufferUsageHint.DynamicDraw);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();

            var model = Matrix4.Identity * Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(4.0 * secCounter));

            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", _camera.GetViewMatrix());
            shader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            GL.BindVertexArray(vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

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

            // Delete all the resources.
            GL.DeleteBuffer(vertexBufferObject);
            GL.DeleteVertexArray(vertexArrayObject);

            GL.DeleteProgram(shader.Handle);

            base.OnUnload();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            
            if (_camera != null)
            {
                _camera.AspectRatio = Size.X / (float)Size.Y;
            }
        }
    }
}
