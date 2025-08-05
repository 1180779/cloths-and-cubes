using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using Engine.Physics;
using OpenTK.Mathematics;
using System.Reflection;
using Vector3 = Engine.Physics.Vector3;
using vec3 = OpenTK.Mathematics.Vector3;



namespace Engine.Display
{

    public class Window : GameWindow
    {

        int VertexBufferObject;
        Shader shader;
        int VertexArrayObject;
        int ElementBufferObject;
        Random r;
        Real secCounter = (Real)0;
        ParticleForceRegistry particleForceRegistry = new ParticleForceRegistry();
        private Matrix4 _view;
        private Matrix4 _projection;
        private OpenTK.Mathematics.Vector3 cameraPos = new OpenTK.Mathematics.Vector3(0.0f, 0.0f, 3.0f);
        private OpenTK.Mathematics.Vector3 front = new(0, 0, -1.0f);
        private OpenTK.Mathematics.Vector3 cameraDirection = new(0, 0, 0);        
        private OpenTK.Mathematics.Vector3 up = OpenTK.Mathematics.Vector3.UnitY;
        private OpenTK.Mathematics.Vector3 cameraRight;
        private OpenTK.Mathematics.Vector3 cameraUp;
        private float speed = 1.5f;

        // TODO: REMOVE
        Engine.Physics.Particle[] particles;
        Engine.Physics.Vector3[] positions;


        float[] vertices = {
             0.5f,  0.5f, 0.0f,  // top right
             0.5f, -0.5f, 0.0f,  // bottom right
            -0.5f, -0.5f, 0.0f,  // bottom left
            -0.5f,  0.5f, 0.0f   // top left
        };

        uint[] indices = {  // note that we start from 0!
            0, 1, 3,   // first triangle
            1, 2, 3    // second triangle
        };
        public Window(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings())
        {
            Size = (width, height);
            Title = title;

            cameraDirection = OpenTK.Mathematics.Vector3.Normalize(cameraPos + front);
            cameraRight = OpenTK.Mathematics.Vector3.Normalize(OpenTK.Mathematics.Vector3.Cross(up, cameraDirection));
            cameraUp = OpenTK.Mathematics.Vector3.Cross(cameraDirection, cameraRight);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            
            if (!IsFocused) // check to see if the window is focused
            {
                return;
            }

            KeyboardState input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            if (input.IsKeyDown(Keys.W))
            {
                cameraPos += front * speed  * (float)e.Time; //Forward 
            }

            if (input.IsKeyDown(Keys.S))
            {
                cameraPos -= front * speed * (float)e.Time; //Backwards
            }

            if (input.IsKeyDown(Keys.A))
            {
                cameraPos -= vec3.Normalize(vec3.Cross(front, up)) * speed * (float)e.Time; //Left
            }

            if (input.IsKeyDown(Keys.D))
            {
                cameraPos += vec3.Normalize(vec3.Cross(front, up)) * speed * (float)e.Time; //Right
            }

            if (input.IsKeyDown(Keys.Space))
            {
                cameraPos += up * speed * (float)e.Time; //Up 
            }

            if (input.IsKeyDown(Keys.LeftShift))
            {
                cameraPos -= up * speed * (float)e.Time; //Down
            }
        }
        

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.5f, 1f);

            GL.Enable(EnableCap.DepthTest);

            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            var transform = Matrix4.Identity;

            transform = transform * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(20f));

            transform = transform * Matrix4.CreateScale(1.1f);

            transform = transform * Matrix4.CreateTranslation(0.1f, 0.1f, 0.0f);


            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            // TODO: CHANGE BACK TO STATIC DRAW
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            // TODO: CHANGE BACK TO STATIC DRAW
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            // TODO: REMOVE
            r = new Random();
            positions = [new Engine.Physics.Vector3(0.5f, 0.5f, 0.0f), new Engine.Physics.Vector3(0.5f, -0.5f, 0.0f), new Engine.Physics.Vector3(-0.5f, -0.5f, 0.0f), new Engine.Physics.Vector3(-0.5f, 0.5f)];
            Real[] dampingfactors = [0.99f, 0.99f, 0.99f, 0.99f];
            particles = [new Particle(), new Particle(), new Particle(), new Particle()];
            
            for(int i = 0; i<particles.Length; i++) { 
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

            _view = Matrix4.LookAt(cameraPos, cameraPos + front, up);

            _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100.0f);

            shader.SetMatrix4("view", _view);
            shader.SetMatrix4("projection", _projection);


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
            for (int i = 0; i< particles.Length; i++)
            {
               
                particles[i].Integrate((Real)args.Time);
                //particles[i].AddForce(force);

                positions[i] = particles[i].position;
                //Console.WriteLine(particles[i].position);
            }
            vertices = Engine.Physics.Vector3.ConcatAndNormalize(positions);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();

            var model = Matrix4.Identity * Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(4.0 * secCounter));
            _view = Matrix4.LookAt(cameraPos, cameraPos + front, up);
            _projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100.0f);

            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", _view);
            shader.SetMatrix4("projection", _projection);

            GL.BindVertexArray(VertexArrayObject);
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
            GL.DeleteBuffer(VertexBufferObject);
            GL.DeleteVertexArray(VertexArrayObject);

            GL.DeleteProgram(shader.Handle);

            base.OnUnload();
        }
    }
}
