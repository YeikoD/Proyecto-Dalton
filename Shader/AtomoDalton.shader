Shader "Custom/AtomoDaltonURP"
{
    Properties
    {
        [Header(URP Lit Properties)]
        _BaseMap ("Textura Base (Albedo)", 2D) = "white" {}
        _BaseColor ("Color Tinte", Color) = (0.8, 0.1, 0.1, 1)
        
        [Normal] _BumpMap ("Normal Map (Relieve)", 2D) = "bump" {}
        _BumpScale ("Fuerza de Normal", Range(0, 5)) = 1.0
        
        _RoughnessMap ("Textura Roughness", 2D) = "black" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.85
        _Metallic ("Metallic", Range(0, 1)) = 0.1
        
        _DisplacementMap ("Textura Displacement (Altura)", 2D) = "black" {}
        _DisplacementStrength ("Fuerza Displacement", Range(0, 2)) = 0.0
        
        _AmbientLight ("Luz Base (Evita zonas negras)", Range(0, 1)) = 0.3

        [Space(10)]
        [Header(Efecto Vivo (Fresnel Animado))]
        [HDR] _RimColor ("Color del Borde 1", Color) = (1, 1, 1, 1)
        [HDR] _RimColor2 ("Color del Borde 2", Color) = (1, 0.5, 0, 1)
        _RimPower ("Grosor del Borde", Range(0.5, 8.0)) = 3.0
        _RimSpeed ("Velocidad del Pulso", Float) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float3 positionWS               : TEXCOORD0;
                float3 normalWS                 : TEXCOORD1;
                float2 uv                       : TEXCOORD2;
                float3 tangentWS                : TEXCOORD3;
                float3 bitangentWS              : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_DisplacementMap);
            SAMPLER(sampler_DisplacementMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _Smoothness;
                half _Metallic;
                float _DisplacementStrength;
                float _AmbientLight;
                half4 _RimColor;
                half4 _RimColor2;
                float _RimPower;
                float _RimSpeed;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                
                // --- DISPLACEMENT ---
                // Leemos el mapa de altura usando LOD 0 en el vertex shader
                float disp = SAMPLE_TEXTURE2D_LOD(_DisplacementMap, sampler_DisplacementMap, input.uv, 0).r;
                // Deformamos el vértice empujándolo en la dirección de su propia normal
                input.positionOS.xyz += input.normalOS * disp * _DisplacementStrength;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                // --- NORMAL MAP ---
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                half3 unpackedNormal = UnpackNormalScale(normalSample, _BumpScale);
                
                // Matriz Tangente a Mundo (TBN)
                half3x3 tbn = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                // Multiplicamos el normal map por el TBN para alinearlo en el mundo 3D
                float3 normalWS = NormalizeNormalPerPixel(mul(unpackedNormal, tbn));
                
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = float4(0,0,0,0);
                
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // --- ROUGHNESS ---
                // Si no hay textura, será negro (0). 
                half roughnessSample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = texColor.rgb * _BaseColor.rgb;
                surfaceData.metallic = _Metallic;
                // Combinamos el slider original de Smoothness con el mapa de Roughness
                surfaceData.smoothness = _Smoothness * (1.0 - roughnessSample);
                surfaceData.alpha = texColor.a * _BaseColor.a;
                
                surfaceData.emission = surfaceData.albedo * _AmbientLight;
                
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                
                // --- FRESNEL PULSE ---
                float rimDot = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rim = pow(rimDot, _RimPower);
                
                float pulse = (sin(_Time.y * _RimSpeed) * 0.5) + 0.5;
                half3 animatedRimColor = lerp(_RimColor.rgb, _RimColor2.rgb, pulse);
                color.rgb += animatedRimColor * rim;

                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}
