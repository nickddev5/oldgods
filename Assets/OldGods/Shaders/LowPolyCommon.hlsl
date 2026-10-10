#ifndef OLDGODS_LOWPOLY_COMMON
#define OLDGODS_LOWPOLY_COMMON

// Shared passes for the Old Gods low-poly look: vertex colour times base colour,
// flat normals from the mesh, main light with shadows, ambient from light probes, fog.
// Characters get a dark outline (an inverted hull, _OutlineWidth in pixels). Every surface is
// shaded with a pixel-art detail texture (_PixelAmount, _TexelsPerMeter; see PixelTexture.cs)
// projected in object space, so the coarse texels stick to moving models.
// Define OG_HORDE before including to draw from the horde instance buffer.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half4 _EmissionColor;
    half _AmbientBoost;
    half _WalkSwing;
    float _AnimPhase;
    half _AirPose;
    half _SlidePose;
    half _OutlineWidth;
    half _PixelAmount;
    half _TexelsPerMeter;
    float4 _CapeSwing; // x: backward lift, y: sideways swing (radians), z: ripple 0..1; set by CapeSway
    // The slide pose at full blend (DodgePose, set per renderer by DodgeLook; defaults are the plain slide).
    float4 _DodgeLeg;   // hip left, hip right, knee left, knee right
    float4 _DodgeArm;   // shoulder left, shoulder right, raise left, raise right
    float4 _DodgeMisc;  // elbow left, elbow right, waist, nod
    float4 _DodgeExtra; // gallop, hip height
CBUFFER_END

TEXTURE2D(_OG_PixelTex);
SAMPLER(sampler_OG_PixelTex);

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    half4  color      : COLOR;
    float2 part       : TEXCOORD0; // x: body part (0 body, 1-2 legs, 3-4 arms, 5 head, 6 cape); y: knee or elbow height, or cape length
    float3 joint      : TEXCOORD1; // the joint the part swings around
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
    float3 restOS     : TEXCOORD4; // rest-pose object position in metres: the pixel texture sticks to the surface
    float3 restNormal : TEXCOORD5; // rest-pose object normal, to pick the projection plane
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

// Rotates a point around the x axis through a joint.
float3 SwingX(float3 p, float3 joint, float angle)
{
    float3 d = p - joint;
    float c = cos(angle), s = sin(angle);
    return joint + float3(d.x, d.y * c - d.z * s, d.y * s + d.z * c);
}

// Rotates a point around the z axis through a joint (raises an arm out to the side).
float3 SwingZ(float3 p, float3 joint, float angle)
{
    float3 d = p - joint;
    float c = cos(angle), s = sin(angle);
    return joint + float3(d.x * c - d.y * s, d.x * s + d.y * c, d.z);
}

// Bends the lower half of a limb at the knee or elbow. bend > 0: vertices below that height
// bend, blended over a few centimetres. bend < 0: the whole part turns with the forearm
// (a held weapon). Positive angles swing the lower limb backward.
void BendLimb(inout float3 p, inout float3 n, float bend, float3 joint, float angle)
{
    if (bend == 0.0 || angle == 0.0) return;
    float h = abs(bend);
    float w = bend < 0.0 ? 1.0 : saturate((h - p.y) / 0.08 + 0.5);
    float3 pivot = float3(p.x, h, joint.z);
    p = SwingX(p, pivot, angle * w);
    n = SwingX(n, float3(0, 0, 0), angle * w);
}

// Bends a cape about the shoulder line it hangs from. Each vertex turns by a share of the
// angle that grows from 0 at the shoulders to 1 at the hem, so the cloth curves rather than
// swinging like a board. Only cloth behind the anchor moves (a mantle's front stays on the
// chest), short capes move less than long ones, and a travelling wave ripples the hem.
// A negative drop marks cloth hung in front (a banner over the legs): only cloth in front of
// the anchor moves, and it barely lifts back, so it stays off the legs.
void BendCape(inout float3 p, inout float3 n, float drop, float3 joint, float3 cape)
{
    float front = drop < 0.0 ? 1.0 : 0.0;
    drop = max(abs(drop), 0.05);
    float below = joint.y - p.y;
    float w = saturate(below / drop) * saturate((joint.z - p.z) * (1.0 - 2.0 * front) / 0.08 + 0.5) * saturate(drop / 0.7);
    if (w <= 0.0) return;
    float ripple = sin(_Time.y * 11.0 - below * 9.0 + p.x * 7.0) * cape.z * 0.12;
    float lift = front > 0.0 ? min(cape.x, 0.12) : cape.x;
    float pitch = (lift + ripple * (0.4 + max(lift, 0.0))) * w;
    float roll = (cape.y + ripple * 0.3) * w;
    p = SwingX(p, joint, pitch);
    n = SwingX(n, float3(0, 0, 0), pitch);
    p = SwingZ(p, joint, roll);
    n = SwingZ(n, float3(0, 0, 0), roll);
}

// Procedural motion from body parts baked into the mesh: legs swing in opposite phase and
// bend at the knee as they come forward; arms counter-swing with bent elbows; the body leans
// into the run and bobs twice per stride; the head nods a little; a cape bends back by the
// cape angles. air and slide (0..1) blend in a jumping pose (knees tucked, arms out) and the
// god's slide pose (_Dodge*): limb angles, a bend at the waist and a galloping swing.
void Animate(inout float3 p, inout float3 n, float2 part, float3 joint, float phase, float swing, float air, float slide, float3 cape)
{
    float id = part.x;
    if (id > 5.5) BendCape(p, n, part.y, joint, cape);
    if (swing <= 0.0 && air <= 0.0 && slide <= 0.0) return;
    float bend = part.y;
    float gallop = sin(phase) * _DodgeExtra.x * slide;
    if (id > 0.5 && id < 2.5)
    {
        bool left = id < 1.5;
        float legPhase = phase + (left ? 0.0 : PI);
        float knee = swing * (0.5 + 3.0 * saturate(-cos(legPhase)));
        knee += air * (left ? 1.4 : 0.7) + slide * (left ? _DodgeLeg.z : _DodgeLeg.w);
        knee += _DodgeExtra.x * slide * saturate(cos(phase)) * 0.8;
        BendLimb(p, n, bend, joint, knee);
        float hip = sin(legPhase) * swing * 2.6 - air * (left ? 0.85 : 0.35) + slide * (left ? _DodgeLeg.x : _DodgeLeg.y) + gallop;
        p = SwingX(p, joint, hip);
        n = SwingX(n, float3(0, 0, 0), hip);
    }
    else if (id > 2.5 && id < 4.5)
    {
        bool left = id < 3.5;
        // The right hand usually carries a weapon, so that arm swings and bends less.
        float carry = left ? 1.0 : 0.5;
        float elbow = -(0.15 + (swing * 2.8 + air * 0.6) * carry) + slide * (left ? _DodgeMisc.x : _DodgeMisc.y);
        BendLimb(p, n, bend, joint, elbow);
        float shoulder = sin(phase + (left ? PI : 0.0)) * swing * 1.8 * carry + slide * (left ? _DodgeArm.x : _DodgeArm.y) - air * 0.3 * carry - gallop;
        p = SwingX(p, joint, shoulder);
        n = SwingX(n, float3(0, 0, 0), shoulder);
        float raise = air * 0.95 * (left ? -1.0 : 0.6) + slide * (left ? _DodgeArm.z : _DodgeArm.w);
        if (raise != 0.0)
        {
            p = SwingZ(p, joint, raise);
            n = SwingZ(n, float3(0, 0, 0), raise);
        }
    }
    else if (id > 4.5 && id < 5.5)
    {
        float nod = sin(phase * 2.0) * swing * 0.25 - air * 0.12 + slide * _DodgeMisc.w;
        p = SwingX(p, joint, nod);
        n = SwingX(n, float3(0, 0, 0), nod);
    }
    // Bend at the waist: arms and head turn whole, the body from the hips up, and capes hung
    // above the hips (a cape on the shoulders, not a banner on the belt).
    float waist = slide * (_DodgeMisc.z + gallop * 0.12);
    if (waist != 0.0 && !(id > 0.5 && id < 2.5))
    {
        float hipY = _DodgeExtra.y;
        float w = id < 0.5 ? saturate((p.y - hipY) / 0.12 + 0.5)
                : id > 5.5 ? saturate((joint.y - hipY) / 0.1 + 0.5)
                : 1.0;
        p = SwingX(p, float3(0, hipY, 0), waist * w);
        n = SwingX(n, float3(0, 0, 0), waist * w);
    }
    // Lean into the run, from the feet.
    float lean = swing * 0.35;
    p = SwingX(p, float3(0, 0, 0), lean);
    n = SwingX(n, float3(0, 0, 0), lean);
    p.y += abs(sin(phase)) * swing * 0.2 + abs(gallop) * 0.15;
}

// Object space to world space for both ordinary renderers and horde instances.
void OGTransform(Attributes IN, out float3 positionWS, out float3 normalWS, out half flash, out half tint)
{
    flash = 0;
    tint = 1;
    float3 p = IN.positionOS.xyz;
    float3 n = IN.normalOS;
#ifdef OG_HORDE
    HordeInstance inst = _Instances[IN.instanceID];
    // The horde has no cape physics: a running enemy's cape just trails at a fixed lift.
    Animate(p, n, IN.part, IN.joint, inst.phase, _WalkSwing, 0.0, 0.0, float3(_WalkSwing * 2.2, 0.0, 0.5));
    float c = cos(inst.yaw), s2 = sin(inst.yaw);
    float3 r = float3(p.x * c + p.z * s2, p.y, -p.x * s2 + p.z * c);
    positionWS = r * inst.scale + inst.position;
    normalWS = normalize(float3(n.x * c + n.z * s2, n.y, -n.x * s2 + n.z * c));
    flash = inst.flash;
    tint = inst.tint;
#else
    UNITY_SETUP_INSTANCE_ID(IN);
    Animate(p, n, IN.part, IN.joint, _AnimPhase, _WalkSwing, _AirPose, _SlidePose, _CapeSwing.xyz);
    positionWS = TransformObjectToWorld(p);
    normalWS = TransformObjectToWorldNormal(n);
#endif
}

// Object-space position in metres (object scale applied), for noise that does not swim.
float3 RestPosition(Attributes IN)
{
#ifdef OG_HORDE
    return IN.positionOS.xyz;
#else
    float3x3 m = (float3x3)GetObjectToWorldMatrix();
    return IN.positionOS.xyz * float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22));
#endif
}

float OGHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Smooth value noise in 0..1, for broad patches.
float OGValueNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = OGHash(i), b = OGHash(i + float2(1, 0)), c = OGHash(i + float2(0, 1)), d = OGHash(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Pixel-art shading: the detail texture on the plane the face mostly lies in (flat faces
// pick one axis, so texels stay square and crisp, as in a hand-UV'd pixel texture). Ground
// faces use the grass channel, characters the fine grain, other walls grain and streaks.
// Darker texels are also a little more saturated, like a pixel artist's colour ramp.
// Static scenery gets soft broad patches as well, so a wide field is not one tone.
half3 PixelShade(half3 albedo, float3 p, float3 n, bool character)
{
    float3 an = abs(n);
    bool up = an.y >= max(an.x, an.z);
    float2 uv = up ? p.xz : (an.x >= an.z ? p.zy : p.xy);
    half4 t = SAMPLE_TEXTURE2D(_OG_PixelTex, sampler_OG_PixelTex, uv * (_TexelsPerMeter / 128.0));
    half tone = character ? t.r : (up && n.y > 0 ? t.g : lerp(t.r, t.b, 0.6));
    half k = (tone - 0.5) * 2.0 * _PixelAmount;
    half3 c = albedo * (1.0 + k);
    half lum = dot(c, half3(0.2126, 0.7152, 0.0722));
    c = max(0, lerp(lum.xxx, c, 1.0 - k * 0.8));
    if (!character) c *= 1.0 + (OGValueNoise(uv / 4.0 + 17.0) - 0.5) * 0.14;
    return c;
}

Varyings LitVert(Attributes IN)
{
    Varyings OUT;
    half flash, tint;
    OGTransform(IN, OUT.positionWS, OUT.normalWS, flash, tint);
    OUT.restOS = RestPosition(IN);
    OUT.restNormal = IN.normalOS;
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
    // Outlined models are characters: fine grain only, no ground patches.
    if (_PixelAmount > 0) albedo = PixelShade(albedo, IN.restOS, IN.restNormal, _OutlineWidth > 0);
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

// Outline: the back faces, pushed out along the normal by a fixed number of pixels (thinner
// far away so a distant crowd does not turn to ink), in a dark shade of the surface colour.
struct OutlineVaryings
{
    float4 positionCS : SV_POSITION;
    half4  color      : COLOR;
    half   fogFactor  : TEXCOORD0;
};

OutlineVaryings OutlineVert(Attributes IN)
{
    OutlineVaryings OUT;
    float3 positionWS, normalWS;
    half flash, tint;
    OGTransform(IN, positionWS, normalWS, flash, tint);
    float4 positionCS = TransformWorldToHClip(positionWS);
    float3 normalCS = mul((float3x3)GetWorldToHClipMatrix(), normalWS);
    float2 dir = normalCS.xy;
    dir = dot(dir, dir) > 1e-8 ? normalize(dir) : float2(0, 0);
    float width = _OutlineWidth * lerp(1.0, 0.45, saturate((positionCS.w - 20.0) / 60.0));
    positionCS.xy += dir * width * 2.0 / _ScreenParams.xy * positionCS.w;
    // Width 0 turns the pass off: every vertex lands outside the clip volume.
    OUT.positionCS = _OutlineWidth > 0 ? positionCS : float4(2, 2, 2, 1);
    OUT.color = half4(IN.color.rgb * _BaseColor.rgb * tint * 0.16, 1);
    OUT.fogFactor = ComputeFogFactor(positionCS.z);
    return OUT;
}

half4 OutlineFrag(OutlineVaryings IN) : SV_Target
{
    return half4(MixFog(IN.color.rgb, IN.fogFactor), 1);
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
