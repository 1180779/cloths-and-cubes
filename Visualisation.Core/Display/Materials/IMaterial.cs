namespace Visualisation.Core.Display.Materials;

public interface IMaterial : IDisposable
{
    public string Name { get; set; }
    public static readonly string AlbedoMapShaderName = "albedoMap";
    public static readonly string NormalMapShaderName = "normalMap";
    public static readonly string MetallicMapShaderName = "metallicMap";
    public static readonly string RoughnessMapShaderName = "roughnessMap";
    public static readonly string AoMapShaderName = "aoMap";

    public static readonly string UseMaps = "useMaps";

    public static readonly string AlbedoValue = "albedoValue";
    public static readonly string MetallicValue = "metallicValue";
    public static readonly string RoughnessValue = "roughnessValue";
    public static readonly string AoValue = "aoValue";

    public void SetForShader(Shader sh);
    public void EnsureLoaded();
}