Shader "Grapple/RoundedRectangle"
{
    Properties
    {
        _Color("Color", Color) = (0.5,0.5,0.5,1)
        _Radius("Radius",float) = 0.2
        _Width("Width",float) = 0.9
        _Height("Height",float) = 0.9
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

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
            };

            struct v2f
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float width : TEXCOORD2;
                float height : TEXCOORD3;
                float radius : TEXCOORD4;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _Width)
            UNITY_DEFINE_INSTANCED_PROP(float, _Height)
            UNITY_DEFINE_INSTANCED_PROP(float, _Radius)
            UNITY_INSTANCING_BUFFER_END(Props)


            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.radius = UNITY_ACCESS_INSTANCED_PROP(Props, _Radius);
                o.width = UNITY_ACCESS_INSTANCED_PROP(Props, _Width);
                o.height = UNITY_ACCESS_INSTANCED_PROP(Props, _Height);
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 dim = (o.width, o.height);
                o.uv = (v.uv - float2(.5f, .5f)) * 2.0f * dim;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Adapted version from:
                // https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Rounded-Rectangle-Node.html
                i.radius = max(min(min(abs(i.radius * 2), abs(i.width)), abs(i.height)), 1e-5);
                i.uv = abs(i.uv) - float2(i.width, i.height) + i.radius;
                float d = length(max(0, i.uv)) / i.radius;
                float alpha = saturate((1 - d) / fwidth(d));
                // i.color.a = alpha - i.color.a;
                i.color.a = alpha - (1-i.color.a);
                i.color.a = saturate(i.color.a);
                return i.color;
            }
            ENDCG
        }
    }
}