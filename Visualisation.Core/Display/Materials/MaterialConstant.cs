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
        Albedo = new Vector3(0.8f, 0.1f, 0.1f), Metallic = 0.0f, Roughness = 0.3f, Ao = 1.0f
    };

    public static MaterialConstant BlueRubber => new MaterialConstant
    {
        Albedo = new Vector3(0.1f, 0.1f, 0.8f), Metallic = 0.0f, Roughness = 0.7f, Ao = 1.0f
    };

    public static MaterialConstant Gold => new MaterialConstant
    {
        Albedo = new Vector3(1.0f, 0.766f, 0.336f), Metallic = 1.0f, Roughness = 0.1f, Ao = 1.0f
    };

    public static MaterialConstant Silver => new MaterialConstant
    {
        Albedo = new Vector3(0.972f, 0.960f, 0.915f), Metallic = 1.0f, Roughness = 0.2f, Ao = 1.0f
    };

    public static MaterialConstant Wood => new MaterialConstant
    {
        Albedo = new Vector3(0.5f, 0.3f, 0.1f), Metallic = 0.0f, Roughness = 0.6f, Ao = 1.0f
    };
    
    public static MaterialConstant Bronze => new MaterialConstant
    {
        Albedo = new Vector3(0.714f, 0.428f, 0.181f), Metallic = 1.0f, Roughness = 0.30f, Ao = 1.0f
    };

    public static MaterialConstant Copper => new MaterialConstant
    {
        Albedo = new Vector3(0.955f, 0.637f, 0.538f), Metallic = 1.0f, Roughness = 0.25f, Ao = 1.0f
    };

    public static MaterialConstant Brass => new MaterialConstant
    {
        Albedo = new Vector3(0.71f, 0.65f, 0.26f), Metallic = 1.0f, Roughness = 0.25f, Ao = 1.0f
    };

    public static MaterialConstant Chrome => new MaterialConstant
    {
        Albedo = new Vector3(0.95f, 0.93f, 0.88f), Metallic = 1.0f, Roughness = 0.05f, Ao = 1.0f
    };

    public static MaterialConstant WhitePlastic => new MaterialConstant
    {
        Albedo = new Vector3(0.9f, 0.9f, 0.9f), Metallic = 0.0f, Roughness = 0.40f, Ao = 1.0f
    };

    public static MaterialConstant GreenPlastic => new MaterialConstant
    {
        Albedo = new Vector3(0.0f, 0.6f, 0.1f), Metallic = 0.0f, Roughness = 0.30f, Ao = 1.0f
    };

    public static MaterialConstant Obsidian => new MaterialConstant
    {
        Albedo = new Vector3(0.053f, 0.05f, 0.066f), Metallic = 0.0f, Roughness = 0.08f, Ao = 1.0f
    };

    public static MaterialConstant Marble => new MaterialConstant
    {
        Albedo = new Vector3(0.86f, 0.86f, 0.86f), Metallic = 0.0f, Roughness = 0.60f, Ao = 1.0f
    };

    public static MaterialConstant Concrete => new MaterialConstant
    {
        Albedo = new Vector3(0.5f, 0.5f, 0.5f), Metallic = 0.0f, Roughness = 0.90f, Ao = 1.0f
    };

    public static MaterialConstant Fabric => new MaterialConstant
    {
        Albedo = new Vector3(0.4f, 0.1f, 0.1f), Metallic = 0.0f, Roughness = 0.80f, Ao = 1.0f
    };

    public static MaterialConstant Leather => new MaterialConstant
    {
        Albedo = new Vector3(0.36f, 0.25f, 0.2f), Metallic = 0.0f, Roughness = 0.60f, Ao = 1.0f
    };

    public static MaterialConstant Porcelain => new MaterialConstant
    {
        Albedo = new Vector3(0.98f, 0.97f, 0.95f), Metallic = 0.0f, Roughness = 0.12f, Ao = 1.0f
    };
}