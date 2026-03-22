#version 410 core

// -- Inputs from vertex shader --
in vec3 v_color;
in vec2 v_texCoord;

// -- Uniforms --
uniform sampler2D   u_texture;
uniform float       u_blendFactor;  // 0.0 = full colour, 1.0 = full texture

// -- Output --
out vec4 frag_color;

void main()
{
    vec4 colorSample   = vec4(v_color, 1.0);
    vec4 textureSample = texture(u_texture, v_texCoord);

    frag_color = mix(colorSample, textureSample, u_blendFactor);
}
