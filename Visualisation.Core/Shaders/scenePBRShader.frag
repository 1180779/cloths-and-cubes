#version 430 core

/* constants */
const float PI = 3.14159265359;

/* struct definitions */

struct PbrMaterial {
    vec3 albedo;
    vec3 normal;
    float metallic;
    float roughness;
    float ao;
};

struct LightDirectional {
    vec3 direction;
};

struct LightPoint {
    vec3 pos;
};

struct LightSpotlight {
    vec3 pos;
    vec3 direction;
    float cutoff;
    float outerCutoff;
};

struct LightDirectionalOut {
    vec3 direction;
};

struct LightSpotlightOut {
    vec3 tangentPos;
    vec3 direction;
    float cutoff;
    float outerCutoff;
};

struct LightPointOut {
    vec3 tangentPos;
};

/* PBR parameters*/
uniform samplerCube irradianceMap;

uniform bool useMaps; /* whether to use texture maps or uniform values */

uniform samplerCube prefilterMap;
uniform sampler2D   brdfLUT;

uniform sampler2D albedoMap;
uniform sampler2D normalMap;
uniform sampler2D metallicMap;
uniform sampler2D roughnessMap;
uniform sampler2D aoMap;

uniform vec3 albedoValue;
uniform float metallicValue;
uniform float roughnessValue;
uniform float aoValue;

/* biad parameters */
uniform float BIAS_MAX;
uniform float BIAS_MIN;
uniform float BIAS_MODIFIER;

/* cascade shadow maps parameters (directional light only) */
uniform sampler2DArray shadowMap;
uniform mat4 lightSpaceMatrices[16];
uniform float cascadePlaneDistances[16];
uniform int cascadeCount;
uniform mat4 view;
uniform float farPlane;

/* inputs from vertex shader */
in vec3 Normal;
in vec3 TangentNormal;
in vec3 TangentFragPosition;
in vec3 FragPosition;
in vec2 TexCoords;
in vec3 WorldViewPos;
in mat3 TBN;

/* lights data */
uniform vec3 globalAmbient;

uniform LightDirectional lightD;
in LightDirectionalOut tangentLightD;
in LightSpotlightOut tangentLightS[4];
in LightPointOut tangentLightP[4];

uniform int lightDCount;
uniform int lightSCount;
uniform int lightPCount;

in vec3 TangentViewPos;

out vec4 FragColor;

/* ------------------------------------------ /
/               helper functions              /
/------------------------------------------- */

/* PBR helper functions */


vec3 fresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

/* other helper functions */

float ShadowCalculation()
{
    // select cascade layer
    vec4 fragPosViewSpace = view * vec4(FragPosition, 1.0);
    float depthValue = abs(fragPosViewSpace.z);

    int layer = -1;
    for (int i = 0; i < cascadeCount; ++i)
    {
        if (depthValue < cascadePlaneDistances[i])
        {
            layer = i;
            break;
        }
    }
    if (layer == -1)
    {
        layer = cascadeCount;
    }

    vec4 fragPosLightSpace = lightSpaceMatrices[layer] * vec4(FragPosition, 1.0);
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;

    // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if (currentDepth > 1.0)
    {
        return 0.0;
    }
    // calculate bias (based on depth map resolution and slope)
    float bias = max(BIAS_MAX * (1.0 - dot(Normal, lightD.direction)), BIAS_MIN);
    if (layer == cascadeCount)
    {
        bias *= 1 / (farPlane * BIAS_MODIFIER);
    }
    else
    {
        bias *= 1 / (cascadePlaneDistances[layer] * BIAS_MODIFIER);
    }

    // PCF
    float shadow = 0.0;
    vec2 texelSize = 1.0 / vec2(textureSize(shadowMap, 0));
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, vec3(projCoords.xy + vec2(x, y) * texelSize, layer)).r;
            shadow += (currentDepth - bias) > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 9.0;

    return shadow;
}

vec3 lightdirectionalCalculate(LightDirectionalOut light, vec3 N, vec3 V, vec3 F0, PbrMaterial material) {
    vec3 L = light.direction;
    vec3 H = normalize(V + L);

    vec3 d = light.direction;
    vec3 radiance = vec3(1.0, 1.0, 1.0); /* directional light = no attenuation */

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, material.roughness);
    float G   = GeometrySmith(N, V, L, material.roughness);
    vec3 F    = fresnelSchlick(clamp(dot(H, V), 0.0, 1.0), F0);

    vec3 numerator    = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;// + 0.0001 to prevent divide by zero
    vec3 specular = numerator / denominator;

    // kS is equal to Fresnel
    vec3 kS = F;
    // for energy conservation, the diffuse and specular light can't
    // be above 1.0 (unless the surface emits light); to preserve this
    // relationship the diffuse component (kD) should equal 1.0 - kS.
    vec3 kD = vec3(1.0) - kS;
    // multiply kD by the inverse metalness such that only non-metals 
    // have diffuse lighting, or a linear blend if partly metal (pure metals
    // have no diffuse light).
    kD *= 1.0 - material.metallic;

    // scale light by NdotL
    float NdotL = max(dot(N, L), 0.0);

    float shadow = ShadowCalculation();

    // add to outgoing radiance Lo
    return (kD * material.albedo / PI + specular) * radiance * NdotL * (1.0 - shadow);// note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
}

vec3 pointlightCalculate(LightPointOut light, vec3 N, vec3 V, vec3 F0, PbrMaterial material) {
    vec3 L = normalize(light.tangentPos - TangentFragPosition);
    vec3 H = normalize(V + L);

    vec3 d = light.tangentPos - TangentFragPosition;
    float distance = length(d);
    float attenuation = 1.0 / (distance * distance);
    vec3 radiance = vec3(1.0, 1.0, 1.0) * attenuation; /* add light colors as a dynamic parameter */

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, material.roughness);
    float G   = GeometrySmith(N, V, L, material.roughness);
    vec3 F    = fresnelSchlick(clamp(dot(H, V), 0.0, 1.0), F0);

    vec3 numerator    = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;// + 0.0001 to prevent divide by zero
    vec3 specular = numerator / denominator;

    // kS is equal to Fresnel
    vec3 kS = F;
    // for energy conservation, the diffuse and specular light can't
    // be above 1.0 (unless the surface emits light); to preserve this
    // relationship the diffuse component (kD) should equal 1.0 - kS.
    vec3 kD = vec3(1.0) - kS;
    // multiply kD by the inverse metalness such that only non-metals 
    // have diffuse lighting, or a linear blend if partly metal (pure metals
    // have no diffuse light).
    kD *= 1.0 - material.metallic;

    // scale light by NdotL
    float NdotL = max(dot(N, L), 0.0);

    // add to outgoing radiance Lo
    return (kD * material.albedo / PI + specular) * radiance * NdotL;// note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
}

vec3 spotlightCalculate(LightSpotlightOut light, vec3 N, vec3 V, vec3 F0, PbrMaterial material) {
    vec3 L = normalize(light.tangentPos - TangentFragPosition);
    vec3 H = normalize(V + L);

    float theta = dot(L, light.direction);
    float epsilon = light.cutoff - light.outerCutoff;
    float intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);

    vec3 d = light.tangentPos - TangentFragPosition;
    float distance = length(d);
    float attenuation = 1.0 / (distance * distance);
    vec3 radiance = vec3(1.0, 1.0, 1.0) * attenuation * intensity; /* add light colors as a dynamic parameter */

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, material.roughness);
    float G   = GeometrySmith(N, V, L, material.roughness);
    vec3 F    = fresnelSchlick(clamp(dot(H, V), 0.0, 1.0), F0);

    vec3 numerator    = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;// + 0.0001 to prevent divide by zero
    vec3 specular = numerator / denominator;

    // kS is equal to Fresnel
    vec3 kS = F;
    // for energy conservation, the diffuse and specular light can't
    // be above 1.0 (unless the surface emits light); to preserve this
    // relationship the diffuse component (kD) should equal 1.0 - kS.
    vec3 kD = vec3(1.0) - kS;
    // multiply kD by the inverse metalness such that only non-metals 
    // have diffuse lighting, or a linear blend if partly metal (pure metals
    // have no diffuse light).
    kD *= 1.0 - material.metallic;

    // scale light by NdotL
    float NdotL = max(dot(N, L), 0.0);

    // add to outgoing radiance Lo
    return (kD * material.albedo / PI + specular) * radiance * NdotL;// note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again

}


/* ------------------------------------------ /
/            main shader function             /
/------------------------------------------- */

void main()
{
    vec3 albedo;
    vec3 normal;
    float metallic;
    float roughness;
    float ao;

    vec3 N;
    vec3 V = normalize(TangentViewPos - TangentFragPosition);
    if (useMaps) {
        vec3 albedoRgb = texture(albedoMap, TexCoords).rgb;
        albedo    = pow(albedoRgb, vec3(2.2));
        normal    = texture(normalMap, TexCoords).rgb;
        metallic  = texture(metallicMap, TexCoords).r;
        roughness = texture(roughnessMap, TexCoords).r;
        ao        = texture(aoMap, TexCoords).r;

        N = normalize(normal * 2.0 - 1.0);// this normal is in tangent space, so is the light direction
    } else {
        albedo     = albedoValue;
        normal     = TangentNormal;
        metallic   = metallicValue;
        roughness  = roughnessValue;
        ao         = aoValue;

        N = normal;
    }

    // TODO: apply only for cloth
    //    if (!gl_FrontFacing)
    //    {
    //        N = -N;
    //    }

    PbrMaterial material = PbrMaterial(
    albedo,
    normal,
    metallic,
    roughness,
    ao
    );

    // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
    // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    // reflectance equation
    vec3 Lo = vec3(0.0);

    // calculate per-light radiance
    if (lightDCount > 0) {
        Lo += lightdirectionalCalculate(tangentLightD, N, V, F0, material);
    }
    for (int i = 0; i < lightSCount; ++i)
    {
        Lo += spotlightCalculate(tangentLightS[i], N, V, F0, material);
    }
    for (int i = 0; i < lightPCount; ++i)
    {
        Lo += pointlightCalculate(tangentLightP[i], N, V, F0, material);
    }

    // ambient lighting (IBL as the ambient term)
    // Convert normal from tangent space to world space for cubemap sampling
    vec3 worldN;
    if (useMaps) {
        worldN = normalize(TBN * N);
    } else {
        worldN = normalize(Normal);
    }

    // Calculate world space view direction for reflection
    vec3 worldV = normalize(WorldViewPos - FragPosition);
    vec3 R = reflect(-worldV, worldN);

    vec3 F = fresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);

    vec3 kS = F;
    vec3 kD = 1.0 - kS;
    kD *= 1.0 - metallic;

    vec3 irradiance = texture(irradianceMap, worldN).rgb;
    vec3 diffuse    = irradiance * albedo;

    const float MAX_REFLECTION_LOD = 4.0;
    vec3 prefilteredColor = textureLod(prefilterMap, R, roughness * MAX_REFLECTION_LOD).rgb;
    vec2 envBRDF  = texture(brdfLUT, vec2(max(dot(N, V), 0.0), roughness)).rg;
    vec3 specular = prefilteredColor * (F * envBRDF.x + envBRDF.y);

    vec3 ambient = (kD * diffuse + specular) * ao;
    vec3 color = ambient + Lo;

    // TODO: move HDR tonemapping and gamma correct to post-processing shader
    color = color / (color + vec3(1.0));// HDR tonemapping
    color = pow(color, vec3(1.0/2.2));// gamma correct

    FragColor = vec4(color, 1.0);
}