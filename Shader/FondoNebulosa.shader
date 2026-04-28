Shader "Custom/FondoNebulosa"
{
    Properties
    {
        [Header(Configuracion de Colores)]
        _ColorRamp ("Textura Gradiente", 2D) = "white" {}
        
        [Header(Parametros)]
        _Escala ("Escala de Ruido", Float) = 5.0
        _Velocidad ("Velocidad", Float) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                // Ya no necesitamos UVs, usaremos las coordenadas 3D del objeto
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0; // Coordenadas 3D locales
            };

            sampler2D _ColorRamp;
            float _Escala;
            float _Velocidad;

            // Función Hash ultra-rápida
            float hash(float n) 
            { 
                return frac(sin(n) * 43758.5453123); 
            }

            // Ruido 3D (Value Noise) de Inigo Quilez.
            // Al ser 3D, envuelve la esfera perfectamente sin "costuras" ni polos pinchados.
            float noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                // Suavizado polinómico (Smoothstep matemático)
                f = f * f * (3.0 - 2.0 * f);

                float n = p.x + p.y * 157.0 + 113.0 * p.z;

                return lerp(lerp(lerp(hash(n + 0.0),   hash(n + 1.0),   f.x),
                                 lerp(hash(n + 157.0), hash(n + 158.0), f.x), f.y),
                            lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                                 lerp(hash(n + 270.0), hash(n + 271.0), f.x), f.y), f.z);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Pasamos la posición local 3D cruda del vértice
                o.localPos = v.vertex.xyz; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float timeOffset = _Time.y * _Velocidad;
                
                // Normalizamos la posición local para tratarla como una esfera matemáticamente perfecta.
                // Esto garantiza que el ruido se estire por igual en toda la superficie 3D.
                float3 pos = normalize(i.localPos);

                // Capa de Ruido A moviéndose en direcciones X e Y
                float3 posA = pos * _Escala + float3(timeOffset, timeOffset * 0.5, 0.0);
                float ruidoA = noise3D(posA);
                
                // Capa de Ruido B moviéndose en direcciones opuestas (Z e Y)
                float3 posB = pos * _Escala * 1.5 + float3(0.0, -timeOffset, timeOffset * 0.5);
                float ruidoB = noise3D(posB);

                // Mezclamos los ruidos
                float finalSample = (ruidoA + ruidoB) / 2.0;

                // Smoothstep para ganar contraste
                finalSample = smoothstep(0.3, 0.7, finalSample);

                // Mapeamos el color leyendo la textura gradiente de C#
                fixed4 colorNebulosa = tex2D(_ColorRamp, float2(finalSample, 0.5));
                
                return colorNebulosa;
            }
            ENDCG
        }
    }
}
