#ifndef OLDGODS_LOWPOLY_COMMON
#define OLDGODS_LOWPOLY_COMMON

// Shared passes for the Old Gods low-poly look: vertex colour times base colour,
// flat normals from the mesh, main light with shadows, ambient from light probes, fog.
// Define OG_HORDE before including to draw from the horde instance buffer.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half4 _EmissionColor;
    half _AmbientBoost;
    half _WalkSwing;
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    half4  color      : COLOR;
#ifdef OG_HORDE
    uint   instanceID : SV_InstanceID;
#else
    UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    half4  color      : COLOR;
    half   fogFactor  : TEXCOORD2;
    half   flash      : TEXCOORD3;
};

#ifdef OG_HORDE
struct HordeInstance
{
    float3 position;
    float  yaw;
    float  scale;
    float  phase;
    float  flash;
    float  tint;
};
StructuredBuffer<HordeInstance> _Instances;
#endif

// Object space to world space for both ordinary renderers and horde instances.
void OGTransform(Attributes IN, out float3 positionWS, out float3 normalWS, out half flash, out half tint)
{
    flash = 0;
    tint = 1;
#ifdef OG_HORDE
    HordeInstance inst = _Instances[IN.instanceID];
    float3 p = IN.positionOS.xyz;
    float3 n = IN.normalOS;

    // Procedural walk until baked vertex animation lands: legs below the hip swing
    // in opposite phase, the upper body bobs.
    float hip = 0.7;
    float side = p.x > 0.05 ? 1.0 : (p.x < -0.05 ? -1.0 : 0.0);
    if (p.y < hip && side != 0.0)
    {
        float k = (hip - p.y) / hip;
        float s = sin(inst.phase + (side > 0 ? 0.0 : PI));
        p.z += s * _WalkSwing * k;
        p.y += max(0.0, s) * _WalkSwing * 0.35 * k;
    }
    else
    {
        p.y += abs(sin(inst.phase)) * _WalkSwing * 0.18;
        p.z += sin(inst.phase * 2.0) * _WalkSwing * 0.05 * saturate(p.y - hip);
    }

    float c = cos(inst.yaw), s2 = sin(inst.yaw);
    float3 r = float3(p.x * c + p.z * s2, p.y, -p.x * s2 + p.z * c);
    positionWS = r * inst.scale + inst.position;
    normalWS = normalize(float3(n.x * c + n.z * s2, n.y, -n.x * s2 + n.z * c));
    flash = inst.flash;
    tint = inst.tint;
#else
    UNITY_SETUP_INSTANCE_ID(IN);
    positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    normalWS = TransformObjectToWorldNormal(IN.normalOS);
#endif
}

Varyings LitVert(Attributes IN)
{
    Varyings OUT;
    half flash, tint;
    OGTransform(IN, OUT.positionWS, OUT.normalWS, flash, tint);
    OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
    OUT.color = half4(IN.color.rgb * tint, IN.color.a);
    OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
    OUT.flash = flash;
    return OUT;
}

half4 LitFrag(Varyings IN) : SV_Target
{
    float3 n = normalize(IN.normalWS);
    half3 albedo = IN.color.rgb * _BaseColor.rgb;
    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
    half ndl = saturate(dot(n, mainLight.direction));
    // Wrap the terminator a little so unlit faces keep their shape.
    half wrapped = saturate((dot(n, mainLight.direction) + 0.25) / 1.25);
    half lightAmount = lerp(wrapped * 0.35, ndl, 0.65) * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    half3 ambient = SampleSH(n) * (1.0 + _AmbientBoost);
    half3 color = albedo * (mainLight.color * lightAmount + ambient) + _EmissionColor.rgb;
    color = lerp(color, half3(1, 1, 1), saturate(IN.flash));
    color = MixFog(color, IN.fogFactor);
    return half4(color, 1);
}

// Shadow caster
float3 _LightDirection;
float3 _LightPosition;

struct ShadowVaryings
{
    float4 positionCS : SV_POSITION;
};

ShadowVaryings ShadowVert(Attributes IN)
{
    ShadowVaryings OUT;
    float3 positionWS, normalWS;
    half flash, tint;
    OGTransform(IN, positionWS, normalWS, flash, tint);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    OUT.positionCS = positionCS;
    return OUT;
}

half4 ShadowFrag(ShadowVaryings IN) : SV_Target { return 0; }

// Depth only
ShadowVaryings DepthVert(Attributes IN)
{
    ShadowVaryings OUT;
    float3 positionWS, normalWS;
    half flash, tint;
    OGTransform(IN, positionWS, normalWS, flash, tint);
    OUT.positionCS = TransformWorldToHClip(positionWS);
    return OUT;
}

half4 DepthFrag(ShadowVaryings IN) : SV_Target { return IN.positionCS.z; }

// Depth and normals, for screen-space effects such as SSAO
struct NormalVaryings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS   : TEXCOORD0;
};

NormalVaryings DepthNormalsVert(Attributes IN)
{
    NormalVaryings OUT;
    float3 positionWS;
    half flash, tint;
    OGTransform(IN, positionWS, OUT.normalWS, flash, tint);
    OUT.positionCS = TransformWorldToHClip(positionWS);
    return OUT;
}

half4 DepthNormalsFrag(NormalVaryings IN) : SV_Target
{
    float3 n = normalize(IN.normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(n);
    float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
    return half4(PackFloat2To888(remapped), 0.0);
#else
    return half4(n, 0.0);
#endif
}

#endif
