#version 330 core

//#define BIAS_MAX 0.05
//#define BIAS_MIN 0.005
//#define BIAS_MODIFIER 0.5

uniform float BIAS_MAX;
uniform float BIAS_MIN;
uniform float BIAS_MODIFIER;

uniform vec3 globalAmbient;

uniform sampler2DArray shadowMap;
uniform mat4 lightSpaceMatrices[16];
uniform float cascadePlaneDistances[16];
uniform int cascadeCount;
uniform mat4 view;
uniform float farPlane;

struct Material {
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

struct LightDirectional {
    vec3 direction;
    vec3 diffuse;
    vec3 specular;
};

struct LightPoint {
    vec3 pos;

    vec3 diffuse;
    vec3 specular;

    float constant;
    float linear;
    float quadratic;
};

struct LightSpotlight {
    vec3 pos;
    vec3 direction;
    float cutoff;
    float outerCutoff;

    vec3 diffuse;
    vec3 specular;
};

in vec3 normal;
in vec3 P;

uniform Material material;

uniform LightDirectional lightD;
uniform LightSpotlight lightS[4];
uniform LightPoint lightP[4];

uniform int lightDCount;
uniform int lightSCount;
uniform int lightPCount;

uniform vec3 viewPos;

uniform vec3 fogColor;
uniform float fogDensity;

out vec4 FragColor;

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

vec3 lightdirectionalCalculate(LightDirectional light, vec3 N, vec3 V) {
    vec3 L = light.direction;
    vec3 R = reflect(-L, N);

    vec3 diffuse = light.diffuse * max(dot(N, L), 0.0) * material.diffuse;
    vec3 specular = light.specular * pow(max(dot(V, R), 0.0), material.shininess) * material.specular;

    float shadow = ShadowCalculation(P);
    return (1.0 - shadow) * (diffuse + specular);
}

vec3 pointlightCalculate(LightPoint light, vec3 N, vec3 V) {
    vec3 L = normalize(light.pos - P);
    vec3 R = reflect(-L, N);

    vec3 d = light.pos - P;
    float dist = length(d);

    vec3 diffuse = light.diffuse * max(dot(N, L), 0.0) * material.diffuse;
    vec3 specular = light.specular * pow(max(dot(V, R), 0.0), material.shininess) * material.specular;

    float attenuation = 1.0 / (light.constant + light.linear * dist +
    light.quadratic * (dist * dist));
    diffuse  *= attenuation;
    specular *= attenuation;

    return diffuse + specular;
}

vec3 spotlightCalculate(LightSpotlight light, vec3 N, vec3 V) {
    vec3 L = normalize(light.pos - P);
    vec3 R = reflect(-L, N);

    float theta = dot(L, light.direction);
    float epsilon = light.cutoff - light.outerCutoff;
    float intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);

    vec3 diffuse = light.diffuse * max(dot(N, L), 0.0) * material.diffuse;
    vec3 specular = light.specular * pow(max(dot(V, R), 0.0), material.shininess) * material.specular;

    diffuse  *= intensity;
    specular *= intensity;

    return diffuse + specular;
}

void main()
{
    vec3 N = normalize(normal);
    vec3 V = normalize(viewPos - P);


    vec3 res = globalAmbient * material.ambient;
    if (lightDCount > 0) {
        res += lightdirectionalCalculate(lightD, N, V);
    }
    for (int i = 0; i < lightSCount; ++i)
    {
        res += spotlightCalculate(lightS[i], N, V);
    }
    for (int i = 0; i < lightPCount; ++i)
    {
        res += pointlightCalculate(lightP[i], N, V);
    }
    res = min(res, 1.0f);

    // calculate fog
    float fogFactor = 1.0f - exp(-fogDensity * length(viewPos - P));
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    res = mix(res, fogColor, fogFactor);
    FragColor = vec4(res, 1.0);
}