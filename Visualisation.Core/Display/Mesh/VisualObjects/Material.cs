namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class Material(Vector3 albedo, float metallic, float roughness, float ao)
{
    private static readonly string AlbedoShaderName = "albedo";
    private static readonly string MetallicShaderName = "metallic";
    private static readonly string RoughnessShaderName = "roughness";
    private static readonly string AoShaderName = "ao";

    public override string ToString()
    {
        return $"{{Material: Albedo: {Albedo}, Metallic: {Metallic}, Roughness: {Roughness}, Ao: {Ao}}}";
    }

    public void SetForShader(Shader sh)
    {
        sh.SetVector3(AlbedoShaderName, Albedo);
        sh.SetFloat(MetallicShaderName, Metallic);
        sh.SetFloat(RoughnessShaderName, Roughness);
        sh.SetFloat(AoShaderName, Ao);
    }

    public Vector3 Albedo = albedo;
    public float Metallic = metallic;
    public float Roughness = roughness;
    public float Ao = ao;

    public Material() : this(new Vector3(0.0f, 0.0f, 0.0f), 0.0f,
        0.0f, 1.0f)
    {
    }

    public static readonly Material Emerald = new(
        new Vector3(0.0215f, 0.614f, 0.0757f),
        metallic: 0.0f,
        roughness: 0.25f,
        ao: 1.0f
    );

    public static readonly Material Gold = new(
        new Vector3(1.0f, 0.766f, 0.336f),
        metallic: 1.0f,
        roughness: 0.3f,
        ao: 1.0f
    );

    public static readonly Material Iron = new(
        new Vector3(0.560f, 0.570f, 0.580f),
        metallic: 1.0f,
        roughness: 0.6f,
        ao: 1.0f
    );

    public static readonly Material Plastic = new(
        new Vector3(0.9f, 0.9f, 0.9f),
        metallic: 0.0f,
        roughness: 0.35f,
        ao: 1.0f
    );

    public static readonly Material Wood = new(
        new Vector3(0.65f, 0.45f, 0.2f),
        metallic: 0.0f,
        roughness: 0.7f,
        ao: 1.0f
    );
}