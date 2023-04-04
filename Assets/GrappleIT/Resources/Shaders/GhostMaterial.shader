Shader "Grapple/GhostMaterial"
{
    Properties
    {
        [Header(Colour parameters)]
        _Color("Main color",Color) = (0,0.5,0.5,0.5)

        [Header(Border parameters)]
        _BorderColor("Border color",Color) = (0,0.5,0.5,0.5)
        _BorderMultiplier("Border multiplier",float) = 1

        [Header(Pusle)]
        [Toggle]_UsePulse("Use pulse",float) = 0
        _PulseMultiplier("Pulse multiplier",float) = 1
        _PulseMin("Lower pulse value",range(0,1)) = 0.2
        _PulseMax("Upper pulse value",range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
                float3 view_t : TEXCOORD1;
            };

            struct v2f
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 view_t : TEXCOORD1;

                fixed4 color : COLOR;
                fixed4 border_color : COLOR1;
            };

            uniform float _BorderMultiplier;
            uniform float _PulseMultiplier;
            uniform float _PulseMin;
            uniform float _PulseMax;
            uniform float _UsePulse;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _BorderColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                o.border_color = UNITY_ACCESS_INSTANCED_PROP(Props, _BorderColor);

                o.normal = normalize(v.normal);
                o.view_t = normalize(ObjSpaceViewDir(v.vertex));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            // from https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Remap-Node.html
            void unity_remap_float(const float In, float2 in_min_max, float2 out_min_max, out float output)
            {
                output = out_min_max.x + (In - in_min_max.x) * (out_min_max.y - out_min_max.x) / (in_min_max.y -
                    in_min_max.x);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Get the world space viewDir and normal
                const half view_dot_normal = saturate(dot(i.view_t, i.normal));

                // Calculate fresnel value
                float fresnel = saturate(pow(1.0 - view_dot_normal, 3));

                if (_UsePulse > 0)
                {
                    // Make the border pulse with time
                    float remapped_sine_time = 0;
                    float sineTime = sin(_PulseMultiplier * _Time.y);

                    unity_remap_float(sineTime, float2(-1, 1), float2(_PulseMin, _PulseMax), remapped_sine_time);
                    fresnel *= remapped_sine_time;
                }

                // Apply the multiplier
                fresnel *= _BorderMultiplier;

                i.color = lerp(i.color, i.border_color, fresnel);

                return i.color;
            }
            ENDCG
        }
    }
}