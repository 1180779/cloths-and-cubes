#version 330 core

/* constants */
const float PI = 3.14159265359;

/* struct definitions */

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

/* PBR parameters */
uniform vec3  albedo;
uniform float metallic;
uniform float roughness;
uniform float ao;

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
in vec3 normal;
in vec3 P;

/* lights data */
uniform vec3 globalAmbient;

uniform LightDirectional lightD;
uniform LightSpotlight lightS[4];
uniform LightPoint lightP[4];

uniform int lightDCount;
uniform int lightSCount;
uniform int lightPCount;

uniform vec3 viewPos;

/* for parameters */
uniform vec3 fogColor;
uniform float fogDensity;

out vec4 FragColor;

/* ------------------------------------------ /
/               helper functions              /
/------------------------------------------- */

/* PBR helper functions */

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

float ShadowCalculation(vec3 fragPosWorldSpace)
{
    // select cascade layer
    vec4 fragPosViewSpace = view * vec4(fragPosWorldSpace, 1.0);
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

    vec4 fragPosLightSpace = lightSpaceMatrices[layer] * vec4(fragPosWorldSpace, 1.0);
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
    vec3 normal = normalize(normal);
    float bias = max(BIAS_MAX * (1.0 - dot(normal, lightD.direction)), BIAS_MIN);
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
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, vec3(projCoords.xy + vec2(x, y) * texelSize, layer)).r;
            shadow += (currentDepth - bias) > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 9.0;

    return shadow;
}

vec3 lightdirectionalCalculate(LightDirectional light, vec3 N, vec3 V, vec3 F0) {
    vec3 L = light.direction;
    vec3 H = normalize(V + L);

    vec3 d = light.direction;
    vec3 radiance = vec3(1.0, 1.0, 1.0); /* directional light = no attenuation */

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, V, L, roughness);
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
    kD *= 1.0 - metallic;

    // scale light by NdotL
    float NdotL = max(dot(N, L), 0.0);

    float shadow = ShadowCalculation(P);

    // add to outgoing radiance Lo
    return (kD * albedo / PI + specular) * radiance * NdotL * (1.0 - shadow);// note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
}

vec3 pointlightCalculate(LightPoint light, vec3 N, vec3 V, vec3 F0) {
    vec3 L = normalize(light.pos - P);
    vec3 H = normalize(V + L);

    vec3 d = light.pos - P;
    float distance = length(d);
    float attenuation = 1.0 / (distance * distance);
    vec3 radiance = vec3(1.0, 1.0, 1.0) * attenuation; /* add light colors as a dynamic parameter */

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, V, L, roughness);
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
    kD *= 1.0 - metallic;

    // scale light by NdotL
    float NdotL = max(dot(N, L), 0.0);

    // add to outgoing radiance Lo
    return (kD * albedo / PI + specular) * radiance * NdotL;// note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
}

vec3 spotlightCalculate(LightSpotlight light, vec3 N, vec3 V, vec3 F0) {
    vec3 L = normalize(light.pos - P);
    vec3 H = normalize(V + L);

    float theta = dot(L, light.direction);
    float epsilon = light.cutoff - light.outerCutoff;
    float intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);

    vec3 d = light.pos - P;
    float distance = length(d);
    float attenuation = 1.0 / (distance * distance);
    vec3 radiance = vec3(1.0, 1.0, 1.0) * attenuation * intensity; /* add light colors as a dynamic parameter */

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, roughness);
    float G   = GeometrySmith(N, V, L, roughness);
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
    kD *= 1.0 - metallic;

    // scale light by NdotL
    float NdotL = max(dot(N, L), 0.0);

    // add to outgoing radiance Lo
    return (kD * albedo / PI + specular) * radiance * NdotL;// note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again

}


/* ------------------------------------------ /
/            main shader function             /
/------------------------------------------- */

void main()
{
    vec3 N = normalize(normal);
    vec3 V = normalize(viewPos - P);

    // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
    // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    // reflectance equation
    vec3 Lo = vec3(0.0);

    // calculate per-light radiance
    if (lightDCount > 0) {
        Lo += lightdirectionalCalculate(lightD, N, V, F0);
    }
    for (int i = 0; i < lightSCount; ++i)
    {
        Lo += spotlightCalculate(lightS[i], N, V, F0);
    }
    for (int i = 0; i < lightPCount; ++i)
    {
        Lo += pointlightCalculate(lightP[i], N, V, F0);
    }

    // ambient lighting (note that the next IBL tutorial will replace 
    // this ambient lighting with environment lighting).
    vec3 ambient = globalAmbient * albedo * ao;
    vec3 color = ambient + Lo;

    // calculate fog
    float fogFactor = 1.0f - exp(-fogDensity * length(viewPos - P));
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    // TODO: move HDR tonemapping to post-processing shader
    // HDR tonemapping
    color = color / (color + vec3(1.0));
    // gamma correct
    color = pow(color, vec3(1.0/2.2));

    FragColor = vec4(mix(color, fogColor, fogFactor), 1.0);
}