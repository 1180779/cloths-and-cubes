using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed partial class Material : IDisposable
{
    private static readonly string AlbedoMapShaderName = "albedoMap";
    private static readonly string NormalMapShaderName = "normalMap";
    private static readonly string MetallicMapShaderName = "metallicMap";
    private static readonly string RoughnessMapShaderName = "roughnessMap";
    private static readonly string AoMapShaderName = "aoMap";

    public override string ToString()
    {
        return $"{{Material: " +
            $"AlbedoMap: {albedoMap}, " +
            $"NormalMap: {normalMap}, " +
            $"MetallicMap: {metallicMap}, " +
            $"Roughness: {roughnessMap}, " +
            $"Ao: {aoMap}}}";
    }

    public void SetForShader(Shader sh)
    {
        EnsureLoaded();
        Debug.Assert(
            AlbedoMap != null && NormalMap != null && MetallicMap != null && RoughnessMap != null && AoMap != null,
            "Material not loaded"
        );

        sh.SetTexture(AlbedoMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture4, AlbedoMap.TextureId);
        sh.SetTexture(NormalMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture5, NormalMap.TextureId);
        sh.SetTexture(MetallicMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture6, MetallicMap.TextureId);
        sh.SetTexture(RoughnessMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture7, RoughnessMap.TextureId);
        sh.SetTexture(AoMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture8, AoMap.TextureId);
    }

    private bool loaded = false;
    private string albedoMap;
    private string normalMap;
    private string metallicMap;
    private string roughnessMap;
    private string aoMap;

    private TexturesManager.TextureData? AlbedoMap { get; set; }
    private TexturesManager.TextureData? NormalMap { get; set; }
    private TexturesManager.TextureData? MetallicMap { get; set; }
    private TexturesManager.TextureData? RoughnessMap { get; set; }
    private TexturesManager.TextureData? AoMap { get; set; }

    public Material(string albedoMap, string normalMap, string metallicMap, string roughnessMap, string aoMap)
    {
        this.albedoMap = albedoMap;
        this.normalMap = normalMap;
        this.metallicMap = metallicMap;
        this.roughnessMap = roughnessMap;
        this.aoMap = aoMap;
    }

    public void EnsureLoaded()
    {
        if (loaded)
            return;

        AlbedoMap = TexturesManager.GetOrLoadTexture(albedoMap, TextureInit);
        NormalMap = TexturesManager.GetOrLoadTexture(normalMap, TextureInit);
        MetallicMap = TexturesManager.GetOrLoadTexture(metallicMap, TextureInit);
        RoughnessMap = TexturesManager.GetOrLoadTexture(roughnessMap, TextureInit);
        AoMap = TexturesManager.GetOrLoadTexture(aoMap, TextureInit);
        loaded = true;
        return;

        void TextureInit()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
        }
    }

    public void Dispose()
    {
        if (!loaded)
            return;

        Debug.Assert(
            AlbedoMap != null && NormalMap != null && MetallicMap != null && RoughnessMap != null && AoMap != null,
            "Material not loaded"
        );

        TexturesManager.FreeTexture(AlbedoMap.TexturePath);
        TexturesManager.FreeTexture(NormalMap.TexturePath);
        TexturesManager.FreeTexture(MetallicMap.TexturePath);
        TexturesManager.FreeTexture(RoughnessMap.TexturePath);
        TexturesManager.FreeTexture(AoMap.TexturePath);
    }
}