#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

out vec3 P;
out vec3 normal;

uniform mat4 lightSpaceMatrix;
uniform int dirLightCount;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

void main()
{
    gl_Position = projection * view * model * vec4(aPos, 1.0);
    P = vec3(model * vec4(aPos, 1.0));
    normal = (transpose(inverse(mat3(model))) * aNormal).xyz;
}
