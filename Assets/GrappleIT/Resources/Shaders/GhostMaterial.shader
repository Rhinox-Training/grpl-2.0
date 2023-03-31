Shader "Grapple/GhostMaterial"
{
    Properties
    {
        _Color("Main color",Color) = (0,0.5,0.5,0.5)
        _BorderMultiplier("Border multiplier",float) = 1
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
                float border_multiplier: TEXCOORD2;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _BorderMultiplier)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                o.border_multiplier = UNITY_ACCESS_INSTANCED_PROP(Props, _BorderMultiplier);

                o.normal = normalize(v.normal);
                o.view_t = normalize(ObjSpaceViewDir(v.vertex)); //ObjSpaceViewDir is similar, but local space.
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Get the world space viewDir and normal
                const half view_dot_normal = saturate(dot(i.view_t, i.normal));

                // Calculate fresnel value
                float fresnel = saturate(pow(1.0 - view_dot_normal, 3));

                // Apply the multiplier
                fresnel *= i.border_multiplier;

                // Calculate the border

                //TODO Make it pulse with time

                // Add the fresnel to the original alpha 
                i.color.a = fresnel;

                return i.color;
            }
            ENDCG
        }
    }
}