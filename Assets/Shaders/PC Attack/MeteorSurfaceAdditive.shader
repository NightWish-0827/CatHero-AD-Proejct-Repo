Shader "Custom/2D/MeteorSurfaceAdditive"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color (Tint)", Color) = (1, 1, 1, 1)
        
        [Header(Meteor Turbulence Effect)]
        _TurbulenceSpeed ("Speed (속도)", Range(0, 10)) = 4.0
        _TurbulenceAmount ("Distortion Amount (왜곡 강도)", Range(0, 0.1)) = 0.03
        _TurbulenceScale ("Scale/Density (패턴 밀도)", Range(1, 50)) = 25.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _TurbulenceSpeed;
                half _TurbulenceAmount;
                half _TurbulenceScale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _BaseColor;
                return OUT;
            }

            half2 calculateTurbulence(half2 uv, half time)
            {
                half2 scaledUV = uv * _TurbulenceScale;
                half wave1 = sin(scaledUV.x + scaledUV.y * 0.7 + time);
                half wave2 = cos(scaledUV.x * 0.8 - scaledUV.y + time * 1.2);
                return half2(wave1, wave2);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half2 currentUV = IN.uv;
                half time = _Time.y * _TurbulenceSpeed;

                half2 turbulenceOffset = calculateTurbulence(currentUV, time);
                currentUV += turbulenceOffset * _TurbulenceAmount;

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, currentUV);
                return texColor * IN.color;
            }
            ENDHLSL
        }
    }
}