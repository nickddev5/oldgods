// The sky: a dome that follows the camera, coloured in its vertices (zenith to horizon), with
// distant hill silhouettes and flat clouds in the same mesh. Drawn first, behind everything,
// without depth. The horizon colour is the fog colour, so far terrain melts into the sky.
Shader "OldGods/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.3, 0.55, 0.85, 1)
        _Horizon ("Horizon (fog)", Color) = (0.7, 0.8, 0.9, 1)
        _Hills ("Far Hills", Color) = (0.45, 0.6, 0.75, 1)
        _Cloud ("Cloud", Color) = (0.9, 0.93, 0.97, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "RenderPipeline" = "UniversalPipeline" "Queue" = "Background" "PreviewType" = "Skybox" }

        Pass
        {
            Name "Sky"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Horizon;
                half4 _Hills;
                half4 _Cloud;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR; // r: height up the dome (0 horizon, 1 zenith); g: hill; b: cloud; a: haze
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
#if UNITY_REVERSED_Z
                OUT.positionCS.z = OUT.positionCS.w * 1e-6;
#else
                OUT.positionCS.z = OUT.positionCS.w * (1 - 1e-6);
#endif
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half h = saturate(IN.color.r);
                // Most of the gradient happens low, as in a real sky.
                half3 c = lerp(_Horizon.rgb, _Zenith.rgb, smoothstep(0.0, 0.55, h));
                // Hills fade towards the horizon colour with distance (a = haze).
                half3 hills = lerp(_Hills.rgb, _Horizon.rgb, saturate(IN.color.a));
                c = lerp(c, hills, IN.color.g);
                c = lerp(c, lerp(_Cloud.rgb, _Horizon.rgb, saturate(IN.color.a) * 0.6), IN.color.b);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
