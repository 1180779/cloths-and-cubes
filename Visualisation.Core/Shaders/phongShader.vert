#version 330 core

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

out LightDirectionalOut tangentLightD;
out LightSpotlightOut tangentLightS[4];
out LightPointOut tangentLightP[4];

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in vec3 aBitangent;

out vec3 FragPosition;
out vec3 TangentFragPosition;
out vec3 Normal;
out vec3 TangentNormal;
out vec2 TexCoords;

out vec3 TangentViewPos;

uniform mat4 lightSpaceMatrix;
uniform int dirLightCount;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

uniform vec3 viewPos;

uniform LightDirectional lightD;
uniform LightSpotlight lightS[4];
uniform LightPoint lightP[4];

uniform int lightDCount;
uniform int lightSCount;
uniform int lightPCount;

void main()
{
    gl_Position = projection * view * model * vec4(aPos, 1.0);
    FragPosition = vec3(model * vec4(aPos, 1.0));
    Normal = (transpose(inverse(mat3(model))) * aNormal).xyz;
    Normal = normalize(Normal);
    TexCoords = aTexCoords;

    /* get the TBN matrix */
    vec3 T = normalize(vec3(model * vec4(aTangent, 0.0)));
    vec3 B = normalize(vec3(model * vec4(aBitangent, 0.0)));
    vec3 N = normalize(vec3(model * vec4(aNormal, 0.0)));

    // Orthonormalize T with respect to N to ensure a stable TBN
    T = normalize(T - N * dot(N, T));
    B = normalize(cross(N, T));

    mat3 TBN = mat3(T, B, N);

    /* change ligthning calculations to be in tangent space 
        this is necessary for normals to work properly */
    mat3 worldToTangent = transpose(TBN);

    TangentNormal = normalize(worldToTangent * N);
    TangentFragPosition = worldToTangent * FragPosition;
    TangentViewPos = worldToTangent * viewPos;

    if (lightDCount > 0) {
        tangentLightD.direction = normalize(worldToTangent * lightD.direction);
    }

    for (int i = 0; i < lightPCount; ++i) {
        tangentLightP[i].tangentPos = worldToTangent * (lightP[i].pos - FragPosition);
    }

    for (int i = 0; i < lightSCount; ++i) {
        tangentLightS[i].tangentPos = worldToTangent * (lightS[i].pos - FragPosition);
        tangentLightS[i].direction = normalize(worldToTangent * lightS[i].direction);
        tangentLightS[i].cutoff = lightS[i].cutoff;
        tangentLightS[i].outerCutoff = lightS[i].outerCutoff;
    }

}
