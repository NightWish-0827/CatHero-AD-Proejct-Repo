Shader "Custom/2D/SoulAdditive"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(Soul Wobble Effect)]
        _WobbleSpeed ("Wobble Speed (일렁임 속도)", Range(0, 10)) = 3.0
        _WobbleAmount ("Wobble Amount (일렁임 정도/폭)", Range(0, 0.1)) = 0.02
        _WobbleFrequency ("Wobble Frequency (일렁임 빈도/결)", Range(0, 30)) = 15.0
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

        // Additive Blending: 기존 배경색에 현재 텍스처 색상을 더함 (알파값에 영향을 받음)
        Blend SrcAlpha One
        ZWrite Off
        Cull Off // 2D 스프라이트이므로 양면 렌더링
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // WebGL 및 모바일 환경 최적화를 위한 float 정밀도 제한
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR; // 스프라이트 렌더러의 색상 값
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
                half _WobbleSpeed;
                half _WobbleAmount;
                half _WobbleFrequency;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // 오브젝트 스페이스 좌표를 클립 스페이스 좌표로 변환
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _BaseColor;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 최적화를 위해 half 정밀도 사용
                half2 uv = IN.uv;

                // 일렁임 효과 연산: Y축(uv.y)에 따라 X축 UV를 왜곡하여 영혼처럼 일렁이게 만듦
                // _Time.y를 활용해 시간에 따른 애니메이션 적용
                half offset = sin(uv.y * _WobbleFrequency + _Time.y * _WobbleSpeed) * _WobbleAmount;
                uv.x += offset;

                // 텍스처 샘플링
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // 최종 색상 반환 (스프라이트 렌더러 컬러/알파 적용)
                return texColor * IN.color;
            }
            ENDHLSL
        }
    }
}