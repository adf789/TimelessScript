Shader "Custom/URP/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Toggle] _OutlineEnabled ("Enable Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineSize;
                float _OutlineEnabled;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                
                // 외곽선이 활성화되어 있을 때만 처리
                if (_OutlineEnabled > 0.5)
                {
                    // 현재 픽셀의 알파값
                    half alpha = color.a;
                    
                    // 외곽선을 그리기 위해 주변 픽셀 샘플링
                    half outline = 0;
                    float pixelSize = _OutlineSize * _MainTex_TexelSize.x;
                    
                    // 8방향 샘플링
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            
                            float2 offset = float2(x, y) * pixelSize;
                            half sampleAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset).a;
                            outline = max(outline, sampleAlpha);
                        }
                    }
                    
                    // 현재 픽셀이 투명하지만 주변에 불투명한 픽셀이 있으면 외곽선 그리기
                    if (alpha < 0.01 && outline > 0.01)
                    {
                        color = _OutlineColor;
                        color.a = outline;
                    }
                }
                
                color.rgb *= color.a;
                return color;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}