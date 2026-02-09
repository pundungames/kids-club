Shader "Hidden/nickeltin/SDF/CopyChannel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DestTex("Source Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            
        }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D_float _MainTex;
            sampler2D_float _DestTex;
            int _SourceChannel; 
            int _DestChannel;  

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float value = src[_SourceChannel];

                // Using color for source tex for all other channels
                // float4 result = tex2D(_DestTex, i.uv);
                float4 result = tex2D(_DestTex, i.uv);
                switch (_DestChannel)
                {
                    case 0: result.r = value;
                        break;
                    case 1: result.g = value;
                        break;
                    case 2: result.b = value;
                        break;
                    case 3: result.a = value;
                        break;
                }
                return result;
            }
            ENDCG
        }
    }
}
