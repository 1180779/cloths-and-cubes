using OpenTK.Graphics.OpenGL4;

namespace Visualisation.Core.Display.Materials;

public class MaterialConstant : IMaterial
{
    public float Metallic { get; set; }
    public float Ao { get; set; }
    public Vector3 Albedo { get; set; }
    public float Roughness { get; set; }

    public void Dispose()
    {
        /* nothing to do here */
    }

    public void SetForShader(Shader sh)
    {
        sh.SetBool(IMaterial.UseMaps, false);

        sh.ReserveTexture(IMaterial.AlbedoMapShaderName, TextureUnit.Texture4);
        sh.ReserveTexture(IMaterial.NormalMapShaderName, TextureUnit.Texture5);
        sh.ReserveTexture(IMaterial.MetallicMapShaderName, TextureUnit.Texture6);
        sh.ReserveTexture(IMaterial.RoughnessMapShaderName, TextureUnit.Texture7);
        sh.ReserveTexture(IMaterial.AoMapShaderName, TextureUnit.Texture8);

        sh.SetVector3(IMaterial.AlbedoValue, Albedo);
        sh.SetFloat(IMaterial.MetallicValue, Metallic);
        sh.SetFloat(IMaterial.RoughnessValue, Roughness);
        sh.SetFloat(IMaterial.AoValue, Ao);
    }

    public void EnsureLoaded()
    {
        /* nothing to do here */
    }

    public static MaterialConstant RedPlastic => new MaterialConstant
    {
        Albedo = new Vector3(0.8f, 0.1f, 0.1f),
        Metallic = 0.0f,
        Roughness = 0.3f,
        Ao = 1.0f
    };

    public static MaterialConstant BlueRubber => new MaterialConstant
    {
        Albedo = new Vector3(0.1f, 0.1f, 0.8f),
        Metallic = 0.0f,
        Roughness = 0.7f,
        Ao = 1.0f
    };

    public static MaterialConstant Gold => new MaterialConstant
    {
        Albedo = new Vector3(1.0f, 0.766f, 0.336f),
        Metallic = 1.0f,
        Roughness = 0.1f,
        Ao = 1.0f
    };

    public static MaterialConstant Silver => new MaterialConstant
    {
        Albedo = new Vector3(0.972f, 0.960f, 0.915f),
        Metallic = 1.0f,
        Roughness = 0.2f,
        Ao = 1.0f
    };

    public static MaterialConstant Wood => new MaterialConstant
    {
        Albedo = new Vector3(0.5f, 0.3f, 0.1f),
        Metallic = 0.0f,
        Roughness = 0.6f,
        Ao = 1.0f
    };
}