Shader "Custom/MeteorHeatHaze"
{
    Properties
    {
        _MainTex ("Distortion Noise (R)", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _Speed ("Heat Speed", Vector) = (0.5, 1, 0, 0)
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float3 normalOS : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 screenPos : TEXCOORD0;
            float2 uv : TEXCOORD1;
            float fresnel : COLOR;
        };

        // 매크로를 이용한 텍스처와 샘플러 선언 (에러 해결 포인트)
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _DistortionStrength;
            float4 _Speed;
            float _FresnelPower;
        CBUFFER_END

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.screenPos = ComputeScreenPos(output.positionCS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);

            float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);
            float3 worldViewDir = normalize(GetWorldSpaceViewDir(TransformObjectToWorld(input.positionOS.xyz)));
            output.fresnel = pow(1.0 - saturate(dot(worldNormal, worldViewDir)), _FresnelPower);

            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            float2 noiseUV = input.uv + _Time.y * _Speed.xy;
            
            // 수정된 샘플링 방식
            float noise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, noiseUV).r;

            float2 distortion = (noise * 2.0 - 1.0) * _DistortionStrength * input.fresnel;
            float2 finalScreenUV = (input.screenPos.xy / input.screenPos.w) + distortion;

            half3 sceneColor = SampleSceneColor(finalScreenUV);
            half3 heatColor = half3(1.0, 0.4, 0.1) * noise * input.fresnel;
            
            return half4(sceneColor + heatColor, input.fresnel);
        }
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}