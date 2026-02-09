Shader "Hidden/nickeltin/SDF/RGBSDFGenerator"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DestTex("Source Texture", 2D) = "white" {}
        _Spread("Spread", Float) = 1
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
            #include "Packages/com.nickeltin.sdf/Shaders/SDFImage.cginc"

            sampler2D_float _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            sampler2D_float _DestTex;
            
            int _Channel;
            float _Spread;

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

            float4 frag(v2f input) : SV_Target
            {
                float4 src = tex2D(_MainTex, input.uv);
                float3 hsv = rgb2hsv(src);
                // float value = hsv.b * src.a;

                float2 uv = input.uv;

                // How far each sample should spread
                float2 offsetedUV = _MainTex_TexelSize.xy * _Spread;


                float4 centerColor = tex2Dlod(_MainTex, float4(uv, 0, 0));
                float edgeStrength = 0.0;

                for (int i = 0; i < 8; ++i)
                {
                    float2 neighborUV = uv + offsetedUV * neighbours8offsets[i];
                    float4 neighborColor = tex2Dlod(_MainTex, float4(neighborUV, 0, 0));

                    // Compute color difference between center and neighbor
                    float colorDiff = length(centerColor.rgb - neighborColor.rgb);

                    // Optionally weigh by alpha mask to suppress transparent pixels
                    float weight = neighborColor.a > 0.5 ? 1.0 : 0.0;

                    edgeStrength = max(edgeStrength, colorDiff * weight);
                }

                
                float4 result = tex2D(_DestTex, input.uv);
                float value = edgeStrength;
                switch (_Channel)
                {
                    case 0: result.r = value; break;
                    case 1: result.g = value; break;
                    case 2: result.b = value; break;
                    case 3: result.a = value; break;
                }
                return result;
            }
            ENDCG
        }
    }
}
