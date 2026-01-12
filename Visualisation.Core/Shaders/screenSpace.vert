#version 430 core

layout (location = 0) in vec3 aPos;

uniform mat4 model;

void main()
{
    // Skip view and projection
    // Work in screen space directly
    gl_Position = model * vec4(aPos, 1.0);
}

