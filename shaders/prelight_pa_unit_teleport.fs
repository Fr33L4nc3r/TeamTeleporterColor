#version 140

// prelight_pa_unit_teleport.fs

#include "prelight_include.fs"

uniform vec4 GBufferDepth_range;
uniform vec3 TeamColor_Primary;
uniform vec3 TeamColor_Secondary;
uniform vec4 TeleportInfo; // vec4(vec3(dir),dist)

uniform sampler2D DiffuseTexture;
uniform sampler2D MaterialTexture;
uniform sampler2D MaskTexture;
uniform sampler2D NoiseTexture;

in vec2 v_TexCoord;
in vec3 v_Forward;
in vec3 v_Normal;
in vec3 v_ModelPosition;

out vec4 out_FragData[4];

void main() 
{
    // Get distance to portal along model relative vector
    float dist_toward_portal = dot(v_ModelPosition, -TeleportInfo.xyz);
    // Discard if past teleporter's event horizon
    float dist_to_portal = TeleportInfo.w - dist_toward_portal - 0.5;
    if( dist_to_portal <= 0.0 )
        discard;

    vec2 tc = v_TexCoord;

    vec4 diffuse_raw = texture(DiffuseTexture, tc);
    vec4 material_raw = texture(MaterialTexture, tc);

    vec4 mask = texture(MaskTexture, tc);
    vec3 viewNormal = normalize(v_Normal);


    // Mix team color - fast & cheap photoshop overlay
    vec3 teamColor = mix(vec3(0.5,0.5,0.5), TeamColor_Secondary, mask.g);
    teamColor = mix(teamColor, TeamColor_Primary, mask.r);
    vec3 team_overlay_mult = clamp(2.0 * diffuse_raw.rgb, 0.0, 1.0);
    vec3 team_overlay_screen = 1.0 - 2.0 * (1.0 - clamp(diffuse_raw.rgb, 0.5, 1.0)) * (1.0 - teamColor);
    vec3 diffuse = team_overlay_mult * team_overlay_screen;

    float specularMask = material_raw.r;
    float specularExp = material_raw.g;
    float specularMetal = material_raw.b;
    float emissive = mask.b;

    vec3 ambientColor = calcAmbient(viewNormal, v_Forward);
    vec3 ambient = ambientColor * diffuse.rgb + diffuse.rgb * 2.0 * emissive;

    vec3 portal_glow_color = TeamColor_Primary;
    float portal_glow_mask = 1.0 - clamp(abs(dist_to_portal) / 15.0, 0.0, 1.0);

    ambient = mix(ambient, portal_glow_color, pow(portal_glow_mask, 3.0));
    diffuse *= (1.0 - portal_glow_mask);

    out_FragData[0] = vec4(ambient, 1.0);
    out_FragData[1] = vec4(diffuse.rgb, specularMask);
    out_FragData[2] = vec4(length(v_Forward) * GBufferDepth_range.z - GBufferDepth_range.w, 0.0, 0.0, 1.0);
    out_FragData[3] = vec4(encodeViewNormal(viewNormal), encodeSpecularExp(specularExp, specularMetal));
}
