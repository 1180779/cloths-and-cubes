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

    public const int MaxCascades = 16;
    public const int MinCascades = 1;
    public const int DefaultCascades = 4;
    public float CascadeSplitLambda = DefaultCascadeSplitLambda;
    private int _cascadeCount = DefaultCascades;

    public int CascadeCount
    {
        get => _cascadeCount;
        set
        {
            if (_cascadeCount != value)
            {
                _cascadeCount = value;
                Dispose();
                Init();
            }
        }
    }

    public const float DefaultCascadeSplitLambda = 0.5f;

    public float[] ShadowCascadeLevels
    {
        get
        {
            var camera = GetCurrentCamera();
            var near = camera.NearPlane;
            var far = camera.FarPlane;
            var splits = new float[CascadeCount - 1];

            // Based pm the practical split scheme
            // originally proposed in
            // https://www.researchgate.net/publication/220805307_Parallel-split_shadow_maps_for_large-scale_virtual_environments
            //
            // Adapted to use a CascadeSplitLambda parameter to control between logarithmic and uniform split
            // For 0.5f it's the same as the practical split scheme
            for (var i = 0; i < splits.Length; i++)
            {
                float p = (i + 1) / (float)CascadeCount;
                float log = near * (float)Math.Pow(far / near, p);
                float lin = near + (far - near) * p;
                splits[i] = CascadeSplitLambda * log + (1 - CascadeSplitLambda) * lin;
            }

            return splits;
        }
    }

#if CASTER_MARGIN
    private const float CasterMargin = 0f;
#endif

    public const int ShadowMapSize = 2 * 1024;

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

    /// <summary>
    /// Whether to visualize the different shadow cascades with colors.
    /// </summary>
    /// <remarks>
    /// This is a rather simple visualization mode that colors the parts of the scene
    /// according to which shadow cascade they belong to. This can be useful for debugging
    /// and understanding how the cascades are distributed across the view frustum.
    /// </remarks>
    public bool DebugCascades { get; set; }

    /// <summary>
    /// Whether Percentage-Closer Filtering (PCF) is used for shadow rendering.
    /// </summary>
    /// <remarks>
    /// Percentage-Closer Filtering (PCF) is a shadow-mapping technique that reduces aliasing and jagged edges
    /// in shadow casting. When this property is enabled, multiple samples are taken within the shadow map
    /// and their results are averaged. This results in softer and more natural-looking shadows but may
    /// incur a performance cost depending on the hardware. 16 samples are used for filtering when enabled.
    /// </remarks>
    public bool UsePCF { get; set; } = true;

    /// <summary>
    /// Whether shimmering in shadows is reduced.
    /// </summary>
    /// <remarks>
    /// Shimmering in shadows often occurs due to precision and sampling issues in shadow mapping.
    /// Enabling this property applies techniques to mitigate this artifact, improving the visual stability
    /// of shadow edges as the camera or light source moves. This is at the cost of some shadow resolution, detail
    /// and some performance.
    /// </remarks>
    public bool ReduceShimmering { get; set; } = true;

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

        Matrix4 lightProjection;
        if (!ReduceShimmering)
        {
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

            lightProjection = Matrix4.CreateOrthographicOffCenter(
                minX, maxX, minY, maxY, minZ, maxZ);
        }
        else
        {
            // based on https://alextardif.com/shadowmapping.html

            // use constant projection size
            // and snap to texel size
            // to reduce shimmering

            // Calculate radius as max distance from centroid to any corner
            // This ensures the sphere encloses the frustum
            float radius = 0.0f;
            foreach (var corner in corners)
            {
                float dist = (corner.Xyz - center).Length;
                radius = Math.Max(radius, dist);
            }

            float texelsPerUnit = ShadowMapSize / (2.0f * radius);
            Matrix4 scalar = Matrix4.CreateScale(texelsPerUnit, texelsPerUnit, texelsPerUnit);

            // transform center to light space and scale
            Vector4 centerLightSpace = Vector4.TransformRow(new Vector4(center, 1.0f), lightView);
            centerLightSpace = Vector4.TransformRow(centerLightSpace, scalar);

            // snap to the nearest texel
            centerLightSpace.X = (float)Math.Floor(centerLightSpace.X);
            centerLightSpace.Y = (float)Math.Floor(centerLightSpace.Y);

            // transform back
            Matrix4.Invert(scalar, out Matrix4 scalarInv);
            centerLightSpace = Vector4.TransformRow(centerLightSpace, scalarInv);

            // new center in light space
            Vector3 newCenter = centerLightSpace.Xyz;

            // Recalculate lightView to look at the new snapped center
            lightView = Matrix4.LookAt(
                newCenter - Direction, // Look from the direction of light towards the new center
                newCenter,
                up);

            // Use fixed size projection
            float minX = -radius;
            float maxX = radius;
            float minY = -radius;
            float maxY = radius;
            float minZ = -radius * ZMult;
            float maxZ = radius * ZMult;

            lightProjection = Matrix4.CreateOrthographicOffCenter(
                minX, maxX, minY, maxY, minZ, maxZ);
        }

        return lightView * lightProjection;
    }

    public Matrix4[] GetLightSpaceMatrices()
    {
        var camera = GetCurrentCamera();
        var splits = ShadowCascadeLevels;

        if (splits.Length == 0)
        {
            return [GetLightSpaceMatrix(camera.NearPlane, camera.FarPlane)];
        }

        List<Matrix4> ret = new();
        for (var i = 0; i < splits.Length + 1; ++i)
        {
            if (i == 0)
            {
                ret.Add(GetLightSpaceMatrix(camera.NearPlane, splits[i]));
            }
            else if (i < splits.Length)
            {
                ret.Add(GetLightSpaceMatrix(splits[i - 1], splits[i]));
            }
            else
            {
                ret.Add(GetLightSpaceMatrix(splits[i - 1], camera.FarPlane));
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

        var splits = ShadowCascadeLevels;

        if (splits.Length == 0)
        {
            sh.SetFloat("near_plane", camera.NearPlane);
            sh.SetFloat("far_plane", camera.FarPlane);
            return;
        }

        if (layer == 0)
        {
            sh.SetFloat("near_plane", camera.NearPlane);
            sh.SetFloat("far_plane", splits[0]);
        }
        else if (layer < splits.Length)
        {
            sh.SetFloat("near_plane", splits[layer - 1]);
            sh.SetFloat("far_plane", splits[layer]);
        }
        else
        {
            sh.SetFloat("near_plane", splits[layer - 1]);
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
            ShadowMapSize,
            ShadowMapSize,
            CascadeCount,
            0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureCompareMode,
            (int)TextureCompareMode.CompareRefToTexture);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureCompareFunc,
            (int)All.Greater);
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
        GL.Viewport(0, 0, ShadowMapSize, ShadowMapSize);
        sh.Use();

        var matrices = GetLightSpaceMatrices();
        sh.SetMatrix4N("lightSpaceMatrices[0]", matrices.Length, matrices);
        sh.SetInt("cascadeCount", CascadeCount);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _depthMapFbo);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        // Create a minimal context for shadow rendering
        var shadowContext = new RenderContext
        {
            PbrShader = sh, // We use the shadow shader as the "PbrShader" for the strategy to use
            SkipMaterial = true, // Don't set material uniforms for shadow pass
        };

        foreach (var o in objects)
        {
            o.RenderStrategy.Render(shadowContext, o.Model);
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
        sh.SetInt("cascadeCount", CascadeCount - 1);
        sh.SetBool("debugCascades", DebugCascades);
        sh.SetBool("usePCF", UsePCF);

        var matrices = GetLightSpaceMatrices();
        sh.SetMatrix4N("lightSpaceMatrices[0]", matrices.Length, matrices);

        var splits = ShadowCascadeLevels;
        sh.SetFloatN("cascadePlaneDistances[0]", splits.Length, splits);

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