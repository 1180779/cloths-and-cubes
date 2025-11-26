#version 430 core

uniform sampler2D inTexture;

in vec2 TexCoords;

out vec4 outputColor;

void main()
{
    outputColor = texture(inTexture, TexCoords);
}