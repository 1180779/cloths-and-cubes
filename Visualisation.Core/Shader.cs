using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core;

public sealed class Shader : IDisposable
{
    private readonly int _handle;
    private readonly Dictionary<string, int> _uniformLocations = new();

    public Shader(string vertexPath, string fragmentPath, string? geometryPath = null)
    {
        var vertexShaderSource = File.ReadAllText(LoadShader(vertexPath));
        var fragmentShaderSource = File.ReadAllText(LoadShader(fragmentPath));

        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);

        GL.CompileShader(vertexShader);

        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
        GlHelper.CheckGlError("Vertex shader compilation");
        if (success == 0)
        {
            var infoLog = GL.GetShaderInfoLog(vertexShader);
            Console.WriteLine(infoLog);
            throw new Exception($"Vertex shader compilation failed! {infoLog}");
        }

        GL.CompileShader(fragmentShader);
        GlHelper.CheckGlError("Fragment shader compilation");

        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
        if (success == 0)
        {
            var infoLog = GL.GetShaderInfoLog(fragmentShader);
            Console.WriteLine(infoLog);
            throw new Exception($"Fragment shader compilation failed! {infoLog}");
        }

        _handle = GL.CreateProgram();

        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);

        int geometryShader = 0;
        if (geometryPath != null)
        {
            var geometryShaderSource = File.ReadAllText(LoadShader(geometryPath));
            geometryShader = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(geometryShader, geometryShaderSource);

            GL.CompileShader(geometryShader);

            GL.GetShader(geometryShader, ShaderParameter.CompileStatus, out success);
            GlHelper.CheckGlError("Geometry shader compilation");
            if (success == 0)
            {
                var infoLog = GL.GetShaderInfoLog(geometryShader);
                Console.WriteLine(infoLog);
                throw new Exception($"GeometryShader shader compilation failed! {infoLog}");
            }

            GL.AttachShader(_handle, geometryShader);
        }

        GL.LinkProgram(_handle);
        GlHelper.CheckGlError("Shader program linking");

        GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out success);
        if (success == 0)
        {
            var infoLog = GL.GetProgramInfoLog(_handle);
            Console.WriteLine(infoLog);
            throw new Exception($"Shader linking failed! {infoLog}");
        }

        GL.DetachShader(_handle, vertexShader);
        GL.DetachShader(_handle, fragmentShader);
        if (geometryPath != null)
        {
            GL.DetachShader(_handle, geometryShader);
            GL.DeleteShader(geometryShader);
        }

        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);
        GlHelper.CheckGlError("Shader cleanup");

        // get uniform locations
        GL.GetProgram(_handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
        for (var i = 0; i < numberOfUniforms; i++)
        {
            var key = GL.GetActiveUniform(_handle, i, out _, out _);
            var location = GL.GetUniformLocation(_handle, key);
            _uniformLocations.Add(key, location);
        }
    }

    public void Use()
    {
        GL.UseProgram(_handle);
        GlHelper.CheckGlError("Shader use");
    }

    private bool _disposedValue;

    ~Shader()
    {
        Debug.Assert(_disposedValue, "GPU Resource leak! Did you forget to call Dispose()?");
    }


    public void Dispose()
    {
        if (!_disposedValue)
        {
            GL.DeleteProgram(_handle);
            GlHelper.CheckGlError("Shader deletion");

            _disposedValue = true;
        }

        GC.SuppressFinalize(this);
    }

    private static string LoadShader(string shaderName)
    {
        // Assuming shaders are in "Shaders" subfolder of output directory
        string shaderPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Shaders",
            shaderName);

        if (!File.Exists(shaderPath))
        {
            throw new FileNotFoundException($"Shader not found at: {shaderPath}");
        }

        return shaderPath;
    }

    public void SetBool(string name, bool b)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], b ? 1 : 0);
    }

    public void SetInt(string name, int v)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], v);
    }

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="v">The value to set</param>
    public void SetFloat(string name, float v)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], v);
        GlHelper.CheckGlError("Shader SetFloat");
    }

    /// <summary>
    /// <remarks>
    /// Use this method when assigning to a member of openGL struct. 
    /// </remarks>
    /// </summary>
    public void SetFloatMember(string name, float v)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(GL.GetUniformLocation(_handle, name), v);
        GlHelper.CheckGlError("Shader SetFloatMember");
    }

    public void SetFloat(string name, int count, float[] values)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], count, values);
        GlHelper.CheckGlError("Shader SetFloat");
    }

    /// <summary>
    /// Set a uniform array of float on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="n">array length</param>
    /// <param name="data">The data to set</param>
    public void SetFloatN(string name, int n, float[] data)
    {
        if (data.Length == 0)
        {
            // Nothing to set
            return;
        }

        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], n, ref data[0]);
        GlHelper.CheckGlError("Shader SetFloatN");
    }

    public void SetVector3(string name, float v0, float v1, float v2)
    {
        GL.UseProgram(_handle);
        GL.Uniform3(_uniformLocations[name], v0, v1, v2);
        GlHelper.CheckGlError("Shader SetVector3");
    }

    /// <summary>
    /// <remarks>
    /// Use this method when assigning to a member of an openGL struct. 
    /// </remarks>
    /// </summary>
    public void SetVector3Member(string name, float v0, float v1, float v2)
    {
        GL.UseProgram(_handle);
        GL.Uniform3(GL.GetUniformLocation(_handle, name), v0, v1, v2);
        GlHelper.CheckGlError("Shader SetVector3Member");
    }

    /// <summary>
    /// <remarks>
    /// Use this method when assigning to a member of openGL struct. 
    /// </remarks>
    /// </summary>
    public void SetVector3Member(string name, Vector3 v)
    {
        GL.UseProgram(_handle);
        GL.Uniform3(GL.GetUniformLocation(_handle, name), v);
        GlHelper.CheckGlError("Shader SetVector3Member");
    }


    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="v">The vector v to set</param>
    public void SetVector3(string name, Vector3 v)
    {
        GL.UseProgram(_handle);
        GL.Uniform3(_uniformLocations[name], v);
        GlHelper.CheckGlError("Shader SetVector3");
    }

    public void SetVector4(string name, float v0, float v1, float v2, float v3)
    {
        GL.UseProgram(_handle);
        GL.Uniform4(_uniformLocations[name], v0, v1, v2, v3);
        GlHelper.CheckGlError("Shader SetVector4");
    }

    public void SetVector4(string name, Vector4 v)
    {
        GL.UseProgram(_handle);
        GL.Uniform4(_uniformLocations[name], v);
        GlHelper.CheckGlError("Shader SetVector4");
    }

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is not transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    public void SetMatrix4(string name, Matrix4 data)
    {
        GL.UseProgram(_handle);
        GL.UniformMatrix4(_uniformLocations[name], false, ref data);
        GlHelper.CheckGlError("Shader SetMatrix4");
    }

    /// <summary>
    /// Set a uniform array of Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="n">array length</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is not transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    public void SetMatrix4N(string name, int n, Matrix4[] data)
    {
        GL.UseProgram(_handle);
        GL.UniformMatrix4(_uniformLocations[name], n, false, ref data[0].Row0.X);
        GlHelper.CheckGlError("Shader SetMatrix4N");
    }

    public void ReserveTexture(string name, TextureUnit unit)
    {
        GL.UseProgram(_handle);
        GL.Uniform1(_uniformLocations[name], (int)unit - (int)TextureUnit.Texture0);
        GlHelper.CheckGlError("Shader ReserveTexture");
    }

    public void SetTexture(string name, TextureTarget textureTarget, TextureUnit unit, int textureHandle)
    {
        GL.UseProgram(_handle);
        GL.ActiveTexture(unit);
        GL.BindTexture(textureTarget, textureHandle);
        GL.Uniform1(_uniformLocations[name], (int)unit - (int)TextureUnit.Texture0);
        GlHelper.CheckGlError("Shader SetTexture");
    }
}