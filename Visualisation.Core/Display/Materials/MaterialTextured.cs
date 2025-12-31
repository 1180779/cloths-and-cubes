using System.Diagnostics;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Texture;

namespace Visualisation.Core.Display.Materials;

public sealed partial class MaterialTextured : IMaterial
{
    public string Name { get; set; }

    public override string ToString()
    {
        return Name;
    }

    public IMaterial TypedClone()
    {
        return new MaterialTextured(Name, AlbedoMap, NormalMap, MetallicMap, RoughnessMap, AoMap);
    }


    public void SetForPbrShader(Shader sh)
    {
        EnsureLoaded();
        Debug.Assert(
            _albedoMapTextureData != null && _normalMapTextureData != null && _metallicMapTextureData != null &&
            _roughnessMapTextureData != null && _aoMapTextureData != null,
            "Material not loaded"
        );
        sh.SetBool(IMaterial.UseMaps, true);

        sh.SetTexture(IMaterial.AlbedoMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture4,
            _albedoMapTextureData.TextureId);
        sh.SetTexture(IMaterial.NormalMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture5,
            _normalMapTextureData.TextureId);
        sh.SetTexture(IMaterial.MetallicMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture6,
            _metallicMapTextureData.TextureId);
        sh.SetTexture(IMaterial.RoughnessMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture7,
            _roughnessMapTextureData.TextureId);
        sh.SetTexture(IMaterial.AoMapShaderName, TextureTarget.Texture2D, TextureUnit.Texture8,
            _aoMapTextureData.TextureId);
    }

    bool _isDisposed;

    ~MaterialTextured()
    {
        Debug.Assert(_isDisposed, "Material was not disposed before finalization.");
    }

    private bool _loaded;

    public string AlbedoMap { get; init; }
    public string NormalMap { get; init; }
    public string MetallicMap { get; init; }
    public string RoughnessMap { get; init; }
    public string AoMap { get; init; }

    private TexturesManager.TextureData? _albedoMapTextureData;
    private TexturesManager.TextureData? _normalMapTextureData;
    private TexturesManager.TextureData? _metallicMapTextureData;
    private TexturesManager.TextureData? _roughnessMapTextureData;
    private TexturesManager.TextureData? _aoMapTextureData;

    public MaterialTextured()
    {
        Name = "Empty Textured Material";
        AlbedoMap = "";
        NormalMap = "";
        MetallicMap = "";
        RoughnessMap = "";
        AoMap = "";
    }

    public MaterialTextured(
        string name,
        string albedoMap,
        string normalMap,
        string metallicMap,
        string roughnessMap,
        string aoMap)
    {
        Name = name;
        this.AlbedoMap = albedoMap;
        this.NormalMap = normalMap;
        this.MetallicMap = metallicMap;
        this.RoughnessMap = roughnessMap;
        this.AoMap = aoMap;
    }

    public void EnsureLoaded()
    {
        if (_loaded)
            return;

        _albedoMapTextureData = TexturesManager.GetOrLoadTexture(AlbedoMap, TextureInit);
        _normalMapTextureData = TexturesManager.GetOrLoadTexture(NormalMap, TextureInit);
        _metallicMapTextureData = TexturesManager.GetOrLoadTexture(MetallicMap, TextureInit);
        _roughnessMapTextureData = TexturesManager.GetOrLoadTexture(RoughnessMap, TextureInit);
        _aoMapTextureData = TexturesManager.GetOrLoadTexture(AoMap, TextureInit);
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
        if (_isDisposed)
            return;

        _isDisposed = true;

        Debug.Assert(
            _albedoMapTextureData != null && _normalMapTextureData != null && _metallicMapTextureData != null &&
            _roughnessMapTextureData != null && _aoMapTextureData != null,
            "Material not loaded"
        );

        TexturesManager.FreeTexture(_albedoMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_normalMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_metallicMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_roughnessMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_aoMapTextureData.TexturePath);
        GC.SuppressFinalize(this);
    }
}