#version 330 core

struct Material {
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

struct LightDirectional {
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct LightPoint {
    vec3 pos;

    vec3 ambient;
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

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

in vec3 normal;
in vec3 P;

uniform Material material;

uniform LightDirectional lightD[4];
uniform LightSpotlight lightS[4];
uniform LightPoint lightP[4];

uniform int lightDCount;
uniform int lightSCount;
uniform int lightPCount;

uniform vec3 viewPos;

uniform vec3 fogColor;
uniform float fogDensity;

out vec4 FragColor;

vec3 lightdirectionalCalculate(LightDirectional light, vec3 N, vec3 V) {
    vec3 L = light.direction;
    vec3 R = reflect(-L, N);

    vec3 ambient = light.ambient * material.ambient;
    vec3 diffuse = light.diffuse * max(dot(N, L), 0.0) * material.diffuse;
    vec3 specular = light.specular * pow(max(dot(V, R), 0.0), material.shininess) * material.specular;

    return ambient + diffuse + specular;
}

vec3 pointlightCalculate(LightPoint light, vec3 N, vec3 V) {
    vec3 L = normalize(light.pos - P);
    vec3 R = reflect(-L, N);

    vec3 d = light.pos - P;
    float dist = length(d);

    vec3 ambient = light.ambient * material.ambient;
    vec3 diffuse = light.diffuse * max(dot(N, L), 0.0) * material.diffuse;
    vec3 specular = light.specular * pow(max(dot(V, R), 0.0), material.shininess) * material.specular;

    float attenuation = 1.0 / (light.constant + light.linear * dist +
    light.quadratic * (dist * dist));
    ambient  *= attenuation;
    diffuse  *= attenuation;
    specular *= attenuation;

    return ambient + diffuse + specular;
}

vec3 spotlightCalculate(LightSpotlight light, vec3 N, vec3 V) {
    vec3 L = normalize(light.pos - P);
    vec3 R = reflect(-L, N);

    float theta = dot(L, light.direction);
    float epsilon = light.cutoff - light.outerCutoff;
    float intensity = clamp((theta - light.outerCutoff) / epsilon, 0.0, 1.0);

    vec3 ambient = light.ambient * material.ambient;
    vec3 diffuse = light.diffuse * max(dot(N, L), 0.0) * material.diffuse;
    vec3 specular = light.specular * pow(max(dot(V, R), 0.0), material.shininess) * material.specular;

    diffuse  *= intensity;
    specular *= intensity;

    return ambient + diffuse + specular;
}

void main()
{
    vec3 N = normalize(normal);
    vec3 V = normalize(viewPos - P);


    vec3 res = vec3(0.0f, 0.0f, 0.0f);
    for(int i = 0; i < lightDCount; ++i)
    {
        res += lightdirectionalCalculate(lightD[i], N, V);
    }
    for(int i = 0; i < lightSCount; ++i)
    {
        res += spotlightCalculate(lightS[i], N, V);
    }
    for(int i = 0; i < lightPCount; ++i)
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