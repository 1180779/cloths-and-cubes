#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

out vec3 P;
out vec3 normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(aPos, 1.0);
    P = vec3(model * vec4(aPos, 1.0));
    //normal = vec4(aNormal, 0.0f).xyz;
    normal = normalize(model * vec4(aNormal, 0.0f)).xyz;
} 