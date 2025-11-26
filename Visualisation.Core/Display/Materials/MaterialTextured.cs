using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.Materials;

public sealed partial class MaterialTextured : IMaterial
{
    public override string ToString()
    {
        return $"{{Material: " +
            $"AlbedoMap: {_albedoMap}, " +
            $"NormalMap: {_normalMap}, " +
            $"MetallicMap: {_metallicMap}, " +
            $"Roughness: {_roughnessMap}, " +
            $"Ao: {_aoMap}}}";
    }

    public void SetForShader(Shader sh)
    {
        EnsureLoaded();
        Debug.Assert(
            AlbedoMap != null && NormalMap != null && MetallicMap != null && RoughnessMap != null && AoMap != null,
            "Material not loaded"
        );
        sh.SetBool(IMaterial.UseMaps, true);

        sh.SetTexture(IMaterial.AlbedoMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture4,
            AlbedoMap.TextureId);
        sh.SetTexture(IMaterial.NormalMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture5,
            NormalMap.TextureId);
        sh.SetTexture(IMaterial.MetallicMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture6,
            MetallicMap.TextureId);
        sh.SetTexture(IMaterial.RoughnessMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture7,
            RoughnessMap.TextureId);
        sh.SetTexture(IMaterial.AoMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture8, AoMap.TextureId);
    }

    private bool _loaded = false;
    private string _albedoMap;
    private string _normalMap;
    private string _metallicMap;
    private string _roughnessMap;
    private string _aoMap;

    private TexturesManager.TextureData? AlbedoMap { get; set; }
    private TexturesManager.TextureData? NormalMap { get; set; }
    private TexturesManager.TextureData? MetallicMap { get; set; }
    private TexturesManager.TextureData? RoughnessMap { get; set; }
    private TexturesManager.TextureData? AoMap { get; set; }

    public MaterialTextured(string albedoMap, string normalMap, string metallicMap, string roughnessMap, string aoMap)
    {
        this._albedoMap = albedoMap;
        this._normalMap = normalMap;
        this._metallicMap = metallicMap;
        this._roughnessMap = roughnessMap;
        this._aoMap = aoMap;
    }

    public void EnsureLoaded()
    {
        if (_loaded)
            return;

        AlbedoMap = TexturesManager.GetOrLoadTexture(_albedoMap, TextureInit);
        NormalMap = TexturesManager.GetOrLoadTexture(_normalMap, TextureInit);
        MetallicMap = TexturesManager.GetOrLoadTexture(_metallicMap, TextureInit);
        RoughnessMap = TexturesManager.GetOrLoadTexture(_roughnessMap, TextureInit);
        AoMap = TexturesManager.GetOrLoadTexture(_aoMap, TextureInit);
        _loaded = true;
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
        if (!_loaded)
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