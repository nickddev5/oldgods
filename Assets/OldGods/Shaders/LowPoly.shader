Shader "OldGods/LowPoly"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 0)
        _AmbientBoost ("Ambient Boost", Range(0, 2)) = 0.2
        _WalkSwing ("Walk Swing (horde only)", Float) = 0
        _AnimPhase ("Anim Phase (set per renderer)", Float) = 0
        _AirPose ("Air Pose (set per renderer)", Range(0, 1)) = 0
        _SlidePose ("Slide Pose (set per renderer)", Range(0, 1)) = 0
        _OutlineWidth ("Outline Width (pixels, 0 = none)", Range(0, 4)) = 0
        _PixelAmount ("Pixel Texture Amount", Range(0, 0.6)) = 0
        _TexelsPerMeter ("Pixel Texels Per Metre", Range(2, 64)) = 10
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex LitVert
            #pragma fragment LitFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "LowPolyCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "LowPolyCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "LowPolyCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "LowPolyCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "LowPolyCommon.hlsl"
            ENDHLSL
        }
    }
}
