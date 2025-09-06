using OpenTK.Graphics.OpenGL4;

namespace Visualization.Display
{
    public class Shader : IDisposable
    {
        public int Handle;
        private readonly Dictionary<string, int> _uniformLocations = new();

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = File.ReadAllText(LoadShader(vertexPath));
            string fragmentShaderSource = File.ReadAllText(LoadShader(fragmentPath));

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);

            GL.CompileShader(vertexShader);

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine(infoLog);
            }

            GL.CompileShader(fragmentShader);

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine(infoLog);
            }

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Console.WriteLine(infoLog);
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            // Loop over all the uniforms,
            for (var i = 0; i < numberOfUniforms; i++)
            {
                // get the name of this uniform,
                var key = GL.GetActiveUniform(Handle, i, out _, out _);

                // get the location,
                var location = GL.GetUniformLocation(Handle, key);

                // and then add it to the dictionary.
                _uniformLocations.Add(key, location);
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader()
        {
            if (disposedValue == false)
            {
                Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static string LoadShader(string shaderName)
        {
            // Assuming shaders are in "Shaders" subfolder of output directory
            string shaderPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Shaders",
                shaderName);

            if (!File.Exists(shaderPath))
                throw new FileNotFoundException($"Shader not found at: {shaderPath}");

            return shaderPath;
        }

        public void SetBool(string name, bool b)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], b ? 1 : 0);
        }

        public void SetInt(string name, int v)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], v);
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="v">The value to set</param>
        public void SetFloat(string name, float v)
        {
            GL.UseProgram(Handle);
            // will not work for arrays
            GL.Uniform1(_uniformLocations[name], v);
        }

        public void SetFloatMember(string name, float v)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(GL.GetUniformLocation(Handle, name), v);
        }

        public void SetFloat(string name, int count, float[] values)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], count, values);
        }

        public void SetVector3(string name, float v0, float v1, float v2)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], v0, v1, v2);
        }

        public void SetVector3Member(string name, float v0, float v1, float v2)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(GL.GetUniformLocation(Handle, name), v0, v1, v2);
        }

        public void SetVector3Member(string name, Vector3 v)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(GL.GetUniformLocation(Handle, name), v);
        }


        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="v">The vector v to set</param>
        public void SetVector3(string name, Vector3 v)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], v);
        }

        public void SetVector4(string name, float v0, float v1, float v2, float v3)
        {
            GL.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], v0, v1, v2, v3);
        }

        public void SetVector4(string name, Vector4 v)
        {
            GL.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], v);
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        /// <remarks>
        ///   <para>
        ///   The matrix is transposed before being sent to the shader.
        ///   </para>
        /// </remarks>
        public void SetMatrix4(string name, Matrix4 data)
        {
            var transposed = Matrix4.Transpose(data);
            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocations[name], true, ref transposed);
        }
    }
}