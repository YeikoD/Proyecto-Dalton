Shader "Custom/AtomoDaltonURP"
{
    Properties
    {
        [Header(URP Lit Properties)]
        _BaseMap ("Textura Base (Albedo)", 2D) = "white" {}
        _BaseColor ("Color Tinte", Color) = (0.8, 0.1, 0.1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.85
        _AmbientLight ("Luz Base (Evita zonas negras)", Range(0, 1)) = 0.3

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
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float3 positionWS               : TEXCOORD0;
                float3 normalWS                 : TEXCOORD1;
                float2 uv                       : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                float _AmbientLight;
                half4 _RimColor;
                half4 _RimColor2;
                float _RimPower;
                float _RimSpeed;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                
                inputData.shadowCoord = float4(0,0,0,0);
                
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = texColor.rgb * _BaseColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = texColor.a * _BaseColor.a;
                
                surfaceData.emission = surfaceData.albedo * _AmbientLight;
                
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                
                float rimDot = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rim = pow(rimDot, _RimPower);
                
                // MAGIA AÑADIDA: Pulso de vida (Animación en base al tiempo)
                // _Time.y avanza con los segundos. El "sin" crea una onda de -1 a 1.
                // Multiplicamos por 0.5 y sumamos 0.5 para que vaya suavemente de 0 a 1.
                float pulse = (sin(_Time.y * _RimSpeed) * 0.5) + 0.5;
                
                // Interpolar (mezclar) entre el Color 1 y el Color 2 según la onda de pulso
                half3 animatedRimColor = lerp(_RimColor.rgb, _RimColor2.rgb, pulse);
                
                // Sumamos el borde animado
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
