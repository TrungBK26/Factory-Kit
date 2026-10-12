Shader "Universal Render Pipeline/StylizedDioramaWater"
{
    Properties
    {
        _ShallowColor("Shallow Color (Ven bờ)", Color) = (0.28, 0.88, 0.86, 0.85)
        _DeepColor("Deep Color (Lòng hồ)", Color) = (0.06, 0.42, 0.68, 0.95)
        _FoamColor("Foam Color (Bọt viền)", Color) = (0.95, 1.0, 1.0, 0.9)

        _WaveSpeed("Wave Speed", Float) = 0.6
        _WaveScale("Wave Scale", Float) = 4.0
        _WaveStrength("Wave Distortion", Float) = 0.08

        _SpecularColor("Sun Sparkle Color", Color) = (1.0, 1.0, 0.9, 1.0)
        _Smoothness("Smoothness", Range(0.1, 1.0)) = 0.92
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent" 
        }
        LOD 200

        Pass
        {
            Name "ForwardWater"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveStrength;
                float4 _SpecularColor;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 worldPos = input.positionWS;
                float2 uv = input.uv;

                // Sóng động hình học theo thời gian
                float t = _Time.y * _WaveSpeed;
                float wave1 = sin(worldPos.x * _WaveScale + t) * cos(worldPos.z * _WaveScale + t * 0.7);
                float wave2 = cos(worldPos.x * _WaveScale * 1.5 - t * 0.8) * sin(worldPos.z * _WaveScale * 1.5 + t);
                float wave = (wave1 + wave2) * 0.5;

                // Tính toán pháp tuyến sóng động nhẹ
                float3 normalWS = normalize(input.normalWS + float3(wave1 * _WaveStrength, 0.0, wave2 * _WaveStrength));
                float3 viewDirWS = normalize(GetCameraPositionWS() - worldPos);

                Light mainLight = GetMainLight();

                // Fresnel phản chiếu góc nhìn
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, 3.0);

                // Độ sâu nước dựa trên tâm hình trụ (ràng buộc UV)
                float distFromCenter = length(uv - float2(0.5, 0.5)) * 2.0;
                float depthFactor = saturate(1.0 - distFromCenter);

                // Blend giữa màu nước cạn và nước sâu
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                // Viền bọt nước ven mép hồ (Foam rim)
                float foam = smoothstep(0.75, 0.98, distFromCenter);
                half4 finalColor = lerp(waterColor, _FoamColor, foam * 0.7);

                // Ánh nắng phản chiếu lấp lánh (Specular Highlight)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float spec = pow(NdotH, _Smoothness * 128.0) * _Smoothness;
                finalColor.rgb += _SpecularColor.rgb * spec * mainLight.color;

                // Phản xạ bầu trời tươi tắn qua Fresnel
                finalColor.rgb = lerp(finalColor.rgb, half3(0.75, 0.92, 1.0), fresnel * 0.4);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
