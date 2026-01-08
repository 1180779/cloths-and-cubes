// some macros for conditional logic inclusion

// turn on for handling of shadows of objects beyond frustum
// at the cost of shadow resolution
// #define INCLUDE_CAMERA
// #define CASTER_MARGIN

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Light;

public class LightDirectional : LightPoint
{
    public LightDirectional(Func<CameraBase> getCurrentCamera)
    {
        GetCurrentCamera = getCurrentCamera;
        Direction = new(2, 2, 2);

        Init();
        ResetShadowBiasToDefault();
    }

    public Func<CameraBase> GetCurrentCamera { get; set; }

    // TODO: change the shader associated with the shadow to allow for specification of number of cascades before compilation
    public float[] ShadowCascadeLevels
    {
        get
        {
            var camera = GetCurrentCamera();
            return
            [
                camera.FarPlane / 50,
                camera.FarPlane / 25,
                camera.FarPlane / 10,
                camera.FarPlane / 2
            ];
        }
    }

#if CASTER_MARGIN
    private const float CasterMargin = 0f;
#endif

    private const int ShadowWidth = 1024, ShadowHeight = 1024;

    private float _shadowBiasMin;
    private float _shadowBiasMax;
    private float _shadowBiasModifier;
    private bool _shadowsBiasChanged = true;

    public void ResetShadowBiasToDefault()
    {
        ShadowBiasMin = 0.0f;
        ShadowBiasMax = 0.015f;
        ShadowBiasModifier = 5.0f;
        ZMult = 4.0f;
    }

    public float ShadowBiasMin
    {
        get => _shadowBiasMin;
        set
        {
            _shadowBiasMin = value;
            _shadowsBiasChanged = true;
        }
    }

    public float ShadowBiasMax
    {
        get => _shadowBiasMax;
        set
        {
            _shadowBiasMax = value;
            _shadowsBiasChanged = true;
        }
    }

    public float ShadowBiasModifier
    {
        get => _shadowBiasModifier;
        set
        {
            _shadowBiasModifier = value;
            _shadowsBiasChanged = true;
        }
    }

    // Tune this parameter according to the scene
    public float ZMult { get; set; }

    public Matrix4 GetLightSpaceMatrix(float nearPlane, float farPlane)
    {
        var camera = GetCurrentCamera();
        var corners = ProjectionHelper.GetFrustumCornersWorldSpace(
            Matrix4.CreatePerspectiveFieldOfView(
                camera.FovRadians,
                CameraBase.AspectRatio,
                nearPlane,
                farPlane),
            camera.ViewMatrix);

        Vector3 center = new();
        foreach (var corner in corners)
        {
            center += corner.Xyz;
        }

        center /= corners.Count;

        var up = Vector3.UnitY;
        if (Math.Abs(Vector3.Dot(Direction, Vector3.UnitY)) > 0.9f)
        {
            up = Vector3.UnitZ;
        }

        var lightView = Matrix4.LookAt(
            center - Direction, // always place parallel to what the camera sees
            center,
            up);

        float minX, minY, minZ;
        minX = minY = minZ = float.MaxValue;
        float maxX, maxY, maxZ;
        maxX = maxY = maxZ = float.MinValue;

        foreach (var corner in corners)
        {
            var trf = Vector4.TransformRow(corner, lightView);
            minX = Math.Min(minX, trf.X);
            minY = Math.Min(minY, trf.Y);
            minZ = Math.Min(minZ, trf.Z);

            maxX = Math.Max(maxX, trf.X);
            maxY = Math.Max(maxY, trf.Y);
            maxZ = Math.Max(maxZ, trf.Z);
        }

        if (minZ < 0)
        {
            minZ *= ZMult;
        }
        else
        {
            minZ /= ZMult;
        }

        if (maxZ < 0)
        {
            maxZ /= ZMult;
        }
        else
        {
            maxZ *= ZMult;
        }

#if INCLUDE_CAMERA
        // Include camera position so near occluders cast shadows
        var camPosLight = Vector4.TransformRow(new Vector4(camera.Position, 1.0f), lightView);
        minX = Math.Min(minX, camPosLight.X);
        maxX = Math.Max(maxX, camPosLight.X);
        minY = Math.Min(minY, camPosLight.Y);
        maxY = Math.Max(maxY, camPosLight.Y);
        minZ = Math.Min(minZ, camPosLight.Z - 10.0f); // allow behind-camera occluders
#endif

#if CASTER_MARGIN
        // Expand bounds with margin for off-frustum casters
        minX -= CasterMargin;
        maxX += CasterMargin;
        minY -= CasterMargin;
        maxY += CasterMargin;
        minZ -= CasterMargin;
        maxZ += CasterMargin;
#endif

        var lightProjection = Matrix4.CreateOrthographicOffCenter(
            minX, maxX, minY, maxY, minZ, maxZ);
        return lightView * lightProjection;
    }

    public Matrix4[] GetLightSpaceMatrices()
    {
        var camera = GetCurrentCamera();

        List<Matrix4> ret = new();
        for (var i = 0; i < ShadowCascadeLevels.Length + 1; ++i)
        {
            if (i == 0)
            {
                ret.Add(GetLightSpaceMatrix(camera.NearPlane, ShadowCascadeLevels[i]));
            }
            else if (i < ShadowCascadeLevels.Length)
            {
                ret.Add(GetLightSpaceMatrix(ShadowCascadeLevels[i - 1], ShadowCascadeLevels[i]));
            }
            else
            {
                ret.Add(GetLightSpaceMatrix(ShadowCascadeLevels[i - 1], camera.FarPlane));
            }
        }

        return ret.ToArray();
    }

    private int _depthMapFbo;
    public int DepthMapsTextureArray { get; private set; }

    public void SetForDepthTextureShader(Shader sh, int layer)
    {
        var camera = GetCurrentCamera();
        GL.ActiveTexture(TextureUnit.Texture0);
        sh.SetInt("depthMap", 0);
        GL.BindTexture(TextureTarget.Texture2DArray, DepthMapsTextureArray);
        sh.SetInt("layer", layer);

        if (layer == 0)
        {
            sh.SetFloat("near_plane", camera.NearPlane);
            sh.SetFloat("far_plane", ShadowCascadeLevels[0]);
        }
        else if (layer < ShadowCascadeLevels.Length)
        {
            sh.SetFloat("near_plane", ShadowCascadeLevels[layer - 1]);
            sh.SetFloat("far_plane", ShadowCascadeLevels[layer]);
        }
        else
        {
            sh.SetFloat("near_plane", ShadowCascadeLevels[layer - 1]);
            sh.SetFloat("far_plane", camera.FarPlane);
        }
    }

    private void Init()
    {
        GL.GenFramebuffers(1, out _depthMapFbo);
        GL.GenTextures(1, out int depthMap);
        DepthMapsTextureArray = depthMap;

        GL.BindTexture(TextureTarget.Texture2DArray, DepthMapsTextureArray);
        GL.TexImage3D(
            TextureTarget.Texture2DArray,
            0,
            PixelInternalFormat.DepthComponent32f,
            ShadowWidth,
            ShadowHeight,
            ShadowCascadeLevels.Length + 1,
            0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToBorder);
        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, borderColor);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.FramebufferTexture(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            DepthMapsTextureArray,
            0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception("Framebuffer not complete");
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void RenderToDepthMap(Shader sh, IEnumerable<GameObject> objects)
    {
        GL.Viewport(0, 0, ShadowWidth, ShadowHeight);
        sh.Use();

        var matrices = GetLightSpaceMatrices();
        sh.SetMatrix4N("lightSpaceMatrices[0]", matrices.Length, matrices);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        foreach (var o in objects)
        {
            sh.SetMatrix4("model", o.Model);
            o.Render();
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffers(1, ref _depthMapFbo);
        int depthMap = DepthMapsTextureArray;
        GL.DeleteTextures(1, ref depthMap);
        DepthMapsTextureArray = 0;
    }

    private Vector3 _direction;

    public Vector3 Direction
    {
        get => _direction;
        set
        {
            if (float.IsNaN(value.X) || float.IsNaN(value.Y) || float.IsNaN(value.Z))
                return;

            var max = Math.Max(Math.Abs(value.X), Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
            if (max > 1e10f)
                value /= max;

            if (value.LengthSquared <= 1e-6f)
                return;

            _direction = value.Normalized();
        }
    }

    public override void SetForShader(Shader sh, string structShName)
    {
        sh.SetVector3Member(structShName + ".direction", -Direction);

        sh.SetTexture("shadowMap", TextureTarget.Texture2DArray, TextureUnit.Texture0, DepthMapsTextureArray);
        sh.SetInt("cascadeCount", ShadowCascadeLevels.Length);

        var matrices = GetLightSpaceMatrices();
        sh.SetMatrix4N("lightSpaceMatrices[0]", matrices.Length, matrices);
        sh.SetFloatN("cascadePlaneDistances[0]", ShadowCascadeLevels.Length, ShadowCascadeLevels);

        // set shadow bias if it has changed
        if (_shadowsBiasChanged)
        {
            sh.SetFloat("BIAS_MAX", ShadowBiasMax);
            sh.SetFloat("BIAS_MIN", ShadowBiasMin);
            sh.SetFloat("BIAS_MODIFIER", ShadowBiasModifier);
            _shadowsBiasChanged = false;
        }
    }
}