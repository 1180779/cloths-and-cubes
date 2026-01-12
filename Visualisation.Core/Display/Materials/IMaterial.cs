namespace Visualisation.Core.Display.Materials;

public interface IMaterial : IDisposable, ICloneable
{
    public string Name { get; set; }
    public static readonly string AlbedoMapShaderName = "albedoMap";
    public static readonly string NormalMapShaderName = "normalMap";
    public static readonly string ArmMapShaderName = "armMap";

    public static readonly string UseMaps = "useMaps";

    public static readonly string AlbedoValue = "albedoValue";
    public static readonly string MetallicValue = "metallicValue";
    public static readonly string RoughnessValue = "roughnessValue";
    public static readonly string AoValue = "aoValue";

    object ICloneable.Clone()
    {
        return TypedClone();
    }

    public IMaterial TypedClone();
    public void SetForPbrShader(Shader sh);
    public void EnsureLoaded();
}