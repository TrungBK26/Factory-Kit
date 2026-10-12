Shader "Universal Render Pipeline/StylizedDioramaLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Stylized Lighting)]
        _ShadowTint("Shadow Color", Color) = (0.35, 0.65, 0.22, 1)
        _RampThreshold("Shadow Threshold", Range(-1, 1)) = -0.05
        _RampSmoothness("Shadow Softness", Range(0.01, 1)) = 0.45

        [Header(Sunlit Highlight)]
        _HighlightColor("Sunlit Highlight Color", Color) = (0.65, 0.92, 0.28, 1)
        _HighlightPower("Highlight Power", Range(0, 1)) = 0.45

        [Header(Warm Ambient & Ground Glow)]
        _GroundGlow("Central Yard Warmth", Range(0, 1)) = 0.25
        _MeadowVariation("Organic Grass Variation", Range(0, 0.5)) = 0.12

        [Header(Rim Lighting)]
        _RimColor("Rim Color", Color) = (1.0, 0.96, 0.85, 1)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimIntensity("Rim Intensity", Range(0, 2)) = 0.35

        [Header(Elevation and Slope Topography)]
        _SlopeColor("Steep Slope Color", Color) = (0.35, 0.52, 0.18, 1)
        _PeakColor("Hilltop Crest Color", Color) = (0.78, 0.96, 0.28, 1)
        _CliffColor("Cliff Rock Color", Color) = (0.54, 0.48, 0.40, 1)
        _SlopeThreshold("Slope Angle Threshold", Range(0.5, 0.98)) = 0.90
        _MaxElevation("Max Elevation Height", Float) = 8.0
        _TopographyStrength("Topography Visibility", Range(0, 1)) = 0.75
        _ContourBanding("Contour Terracing", Range(0, 1)) = 0.35

        [Header(Faceted Shading)]
        [Toggle(_FACETED_NORMAL)] _FacetedNormal("Force Faceted (Flat) Shading", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0

            #pragma shader_feature_local _FACETED_NORMAL
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowTint;
                float _RampThreshold;
                float _RampSmoothness;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _FacetedNormal;
                float4 _HighlightColor;
                float _HighlightPower;
                float _GroundGlow;
                float _MeadowVariation;
                float4 _SlopeColor;
                float4 _PeakColor;
                float4 _CliffColor;
                float _SlopeThreshold;
                float _MaxElevation;
                float _TopographyStrength;
                float _ContourBanding;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 worldPos = input.positionWS;
                float3 normalWS = normalize(input.normalWS);

                #if defined(_FACETED_NORMAL)
                // Tính pháp tuyến phẳng theo mặt tam giác (Faceted Flat Normals)
                float3 ddxPos = ddx(worldPos);
                float3 ddyPos = ddy(worldPos);
                normalWS = normalize(cross(ddxPos, ddyPos));
                #endif

                float3 viewDirWS = normalize(GetCameraPositionWS() - worldPos);

                // Sample Base Texture & Base Color
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // 1. Organic World-Space Meadow Variation
                if (_MeadowVariation > 0.001)
                {
                    float2 p = worldPos.xz * 0.30;
                    float n1 = sin(p.x * 1.5 + cos(p.y * 1.1)) * cos(p.y * 1.3 - sin(p.x * 0.7));
                    float n2 = sin(worldPos.x * 0.75 + worldPos.z * 1.25);
                    float pattern = (n1 * 0.6 + n2 * 0.4);
                    
                    half3 warmGrass = lerp(albedo.rgb, _HighlightColor.rgb, 0.40);
                    albedo.rgb = lerp(albedo.rgb, warmGrass, saturate(pattern * _MeadowVariation + 0.45));
                }

                // 2. Elevation & Slope Topography (Phân định rõ rệt chỗ cao chỗ thấp của địa hình)
                if (_TopographyStrength > 0.001)
                {
                    // Gradient độ cao: Càng lên cao đỉnh đồi càng rực rỡ nắng vàng
                    float heightNorm = saturate(worldPos.y / max(0.1, _MaxElevation));
                    half3 elevatedColor = lerp(albedo.rgb, _PeakColor.rgb, heightNorm * 0.70);

                    // Sườn dốc: Độ nghiêng càng lớn càng sẫm màu rêu đất ấm
                    float slope = 1.0 - saturate(normalWS.y);
                    float slopeBlend = smoothstep(1.0 - _SlopeThreshold, 1.0 - (_SlopeThreshold - 0.18), slope);
                    half3 topographicColor = lerp(elevatedColor, _SlopeColor.rgb, slopeBlend * 0.80);

                    // Vách dốc đứng: Lộ ra đá vách núi diorama
                    float steep = smoothstep(0.24, 0.42, slope);
                    topographicColor = lerp(topographicColor, _CliffColor.rgb, steep * 0.90);

                    // Ngấn cao độ mỹ thuật (Contour terracing): Tạo dải viền bậc thang tự nhiên
                    if (_ContourBanding > 0.001)
                    {
                        float band = abs(frac(worldPos.y * 0.45 + 0.1) - 0.5) * 2.0;
                        float contour = smoothstep(0.70, 0.95, band) * _ContourBanding * saturate(worldPos.y * 0.4);
                        topographicColor = lerp(topographicColor, _PeakColor.rgb, contour * 0.55);
                    }

                    albedo.rgb = lerp(albedo.rgb, topographicColor, _TopographyStrength);
                }

                // Main Light & Shadow
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                Light mainLight = GetMainLight(shadowCoord);

                float NdotL = dot(normalWS, mainLight.direction);
                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Stylized Two-Tone Ramp Lighting
                float halfLambert = NdotL * 0.5 + 0.5;
                float ramp = smoothstep(_RampThreshold, _RampThreshold + _RampSmoothness, halfLambert * shadowAtten);

                // Blend giữa màu bóng xanh tươi (ShadowTint) và màu nắng vàng ấm (HighlightColor)
                half3 sunlitAlbedo = lerp(albedo.rgb, _HighlightColor.rgb, saturate(NdotL * _HighlightPower));
                half3 shadowAlbedo = albedo.rgb * _ShadowTint.rgb;
                half3 directLight = lerp(shadowAlbedo, sunlitAlbedo * mainLight.color, ramp);

                // Ambient Light / Spherical Harmonics (Đẩy sáng môi trường để vùng bóng râm trong trẻo, không bị đen đúa)
                half3 ambient = SampleSH(normalWS) * albedo.rgb * 0.55;

                // Central Yard Warmth (Hắt sáng vàng ấm dịu dàng cho khoảng sân trung tâm)
                float upwardNormal = saturate(normalWS.y);
                float centralFactor = saturate(1.0 - length(worldPos.xz) / 28.0);
                half3 yardWarmth = half3(1.0, 0.96, 0.82) * (upwardNormal * centralFactor * _GroundGlow * albedo.rgb);

                // Rim Lighting (Fresnel viền mềm tỏa sáng diorama cao cấp)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity * ramp;
                half3 rimLight = _RimColor.rgb * rim;

                half3 finalRGB = directLight + ambient + yardWarmth + rimLight;
                return half4(finalRGB, albedo.a);
            }
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
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowPassFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
