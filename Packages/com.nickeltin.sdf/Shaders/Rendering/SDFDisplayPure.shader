Shader "nickeltin/SDF/UIPure"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture A", 2D) = "white" {}
        [PerRendererData] _AlphaTex ("Texture B", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        
        _DistanceSoftness("Distance Softness", Range(0,1)) = 1
        
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader 
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default SDF"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uvA : TEXCOORD0;
                float4 uvB : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 uvA : TEXCOORD0;
                float4 uvB : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                float4 mask : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            
            sampler2D_float _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            sampler2D_float _AlphaTex;
            float4 _AlphaTex_ST;
            float4 _AlphaTex_TexelSize;
           
            float4 _Color;
            
            float4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            //Custom
            half _DistanceSoftness;
            

            v2f vert (appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                
                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                // float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.uvA = float4(TRANSFORM_TEX(v.uvA.xy, _MainTex), v.uvA.z, v.uvA.w);
                OUT.uvB = float4(TRANSFORM_TEX(v.uvB.xy, _AlphaTex), v.uvB.z, v.uvB.w);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 /
                    (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            
            //Default UI frag 
            fixed4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;
                
                const float4 sdfA = 1 - tex2D(_MainTex, IN.uvA);
                const float4 sdfB = 1 - tex2D(_AlphaTex, IN.uvB);
                
                
                float4 color = IN.color;

                const float layersLerp = IN.uvB.z;
                const float density = lerp(sdfA.a, sdfB.a, layersLerp);
                const float width = IN.uvA.z;
                const float softness = IN.uvA.w;

                // Unused
                // const float uvBW = IN.uvB.w;
                
                const float fWidth = max(0.0001, fwidth(density) * _DistanceSoftness);

                const float halfSoftness = softness * 0.5;
                const float from = max(0, width - halfSoftness);
                const float to = min(1, width + fWidth + halfSoftness);
                const float alpha = smoothstep(to, from, density) * color.a;;
                
                color.a = alpha;
                
                
#ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
#endif

                color.rgb *= color.a;
                return color;
            }
            
            ENDCG
        }
    }
    
    Fallback "UI/Default"
//    CustomEditor "nickeltin.SDF.Editor.SDFShaderGUI"
}