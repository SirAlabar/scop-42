#version 410 core

// -- Inputs from vertex shader --
in vec3 v_color;
in vec2 v_texCoord;
in vec3 v_normal;
in vec3 v_fragPos;

// -- Uniforms --
uniform sampler2D   u_texture;
uniform float       u_blendFactor;      // 0.0 = full colour, 1.0 = full texture
uniform int         u_flatShading;      // 1 = flat,          0 = smooth

uniform int         u_lightOn;          // 1 = Phong active,  0 = unlit
uniform vec3        u_lightPos;         // world-space light position
uniform vec3        u_lightColor;       // RGB light colour
uniform float       u_ambientStrength;  // ambient factor   [0, 1]
uniform float       u_shininess;        // specular exponent (e.g. 32.0)
uniform vec3        u_viewPos;          // world-space camera position

// -- Output --
out vec4 frag_color;

void main()
{
    // Normal selection:
    // Flat   -- reconstruct face normal from screen-space derivatives.
    // Smooth -- use interpolated vertex normal.
    vec3 normal;

    if (u_flatShading == 1)
    {
        normal = normalize(cross(dFdx(v_fragPos), dFdy(v_fragPos)));
    }
    else
    {
        normal = normalize(v_normal);
    }

    // Base object colour -- blended from vertex colour and texture.
    vec3 objectColor = mix(v_color, texture(u_texture, v_texCoord).rgb, u_blendFactor);

    // Lighting
    vec3 litColor;

    if (u_lightOn == 1)
    {
        // Ambient
        vec3 ambient = u_ambientStrength * u_lightColor;

        // Diffuse
        vec3  lightDir = normalize(u_lightPos - v_fragPos);
        float diff     = max(dot(normal, lightDir), 0.0);
        vec3  diffuse  = diff * u_lightColor;

        // Specular
        vec3  viewDir  = normalize(u_viewPos - v_fragPos);
        vec3  reflDir  = reflect(-lightDir, normal);
        float spec     = pow(max(dot(viewDir, reflDir), 0.0), u_shininess);
        vec3  specular = 0.5 * spec * u_lightColor;

        litColor = (ambient + diffuse + specular) * objectColor;
    }
    else
    {
        // Lighting off -- raw object colour, no modification.
        litColor = objectColor;
    }

    frag_color = vec4(litColor, 1.0);
}
