#version 410 core

// -- Inputs --
layout (location = 0) in vec3 a_position;
layout (location = 1) in vec3 a_color;
layout (location = 2) in vec2 a_texCoord;
layout (location = 3) in vec3 a_normal;

// -- Uniforms --
uniform mat4 u_model;
uniform mat4 u_view;
uniform mat4 u_projection;

// -- Outputs to fragment shader --
out vec3 v_color;
out vec2 v_texCoord;
out vec3 v_normal;
out vec3 v_fragPos;

void main()
{
    vec4 worldPos = u_model * vec4(a_position, 1.0);

    gl_Position = u_projection * u_view * worldPos;

    v_color    = a_color;
    v_texCoord = a_texCoord;
    v_fragPos  = worldPos.xyz;

    // Normal transformed into world space.
    // mat3(u_model) is correct for uniform scaling (no shear/stretch).
    v_normal = mat3(u_model) * a_normal;
}
