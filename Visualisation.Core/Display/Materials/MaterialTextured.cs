using System.Diagnostics;
using System.Xml.Serialization;

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
        return new MaterialTextured(Name, _albedoMap, _normalMap, _metallicMap, _roughnessMap, _aoMap);
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

    [XmlIgnore]
    [NonSerialized]
    private bool _loaded = false;

    private string _albedoMap;
    private string _normalMap;
    private string _metallicMap;
    private string _roughnessMap;
    private string _aoMap;

    [XmlIgnore]
    [NonSerialized]
    private TexturesManager.TextureData? _albedoMapTextureData;

    [XmlIgnore]
    [NonSerialized]
    private TexturesManager.TextureData? _normalMapTextureData;

    [XmlIgnore]
    [NonSerialized]
    private TexturesManager.TextureData? _metallicMapTextureData;

    [XmlIgnore]
    [NonSerialized]
    private TexturesManager.TextureData? _roughnessMapTextureData;

    [XmlIgnore]
    [NonSerialized]
    private TexturesManager.TextureData? _aoMapTextureData;

    public MaterialTextured(
        string name,
        string albedoMap,
        string normalMap,
        string metallicMap,
        string roughnessMap,
        string aoMap)
    {
        Name = name;
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

        _albedoMapTextureData = TexturesManager.GetOrLoadTexture(_albedoMap, TextureInit);
        _normalMapTextureData = TexturesManager.GetOrLoadTexture(_normalMap, TextureInit);
        _metallicMapTextureData = TexturesManager.GetOrLoadTexture(_metallicMap, TextureInit);
        _roughnessMapTextureData = TexturesManager.GetOrLoadTexture(_roughnessMap, TextureInit);
        _aoMapTextureData = TexturesManager.GetOrLoadTexture(_aoMap, TextureInit);
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
            _albedoMapTextureData != null && _normalMapTextureData != null && _metallicMapTextureData != null &&
            _roughnessMapTextureData != null && _aoMapTextureData != null,
            "Material not loaded"
        );

        TexturesManager.FreeTexture(_albedoMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_normalMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_metallicMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_roughnessMapTextureData.TexturePath);
        TexturesManager.FreeTexture(_aoMapTextureData.TexturePath);
    }
}