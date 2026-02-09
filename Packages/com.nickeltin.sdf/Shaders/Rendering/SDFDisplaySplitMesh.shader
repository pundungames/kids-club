Shader "nickeltin/SDF/UI" 
{
    Properties 
    {
        //#region Unity UI/Default
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        //#endregion
        
        
        //#region Custom
        _MainColor("Main Color", Color) = (1,1,1,1)
        
        [PerRendererData] _AlphaTex ("SDF Texture", 2D) = "white" {}
        [Toggle(OUTLINE_ON)] _EnableOutline("Enable Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0,1)) = 0.1    
        _OutlineSoftness("Outline Softness", Range(0,1)) = 0.1
        
        _ShadowColor("Shadow Color", Color) = (1,1,1,1)
        _ShadowSoftness("Shadow Softness", Range(0,1)) = 0
        
        _DistanceSoftness("Distance Softness", Range(0,1)) = 1
        _IsSceneViewHidden("Is Scene View Hidden", Float) = 0
        
        [Toggle(CRISP_EDGE_ON)] _EnableCrispEdge("Enable Crisp Edge", Float) = 0
        
        //#endregion
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
            #include "Packages/com.nickeltin.sdf/Shaders/SDFImage.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            #pragma multi_compile_local _ OUTLINE_ON
            #pragma multi_compile_local _ CRISP_EDGE_ON
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                float4 mask : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            //Default Unity UI/Default
            sampler2D_float _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            
            sampler2D_float _AlphaTex;
            float4 _AlphaTex_ST;
            float4 _AlphaTex_TexelSize;
            
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            //Custom
            half4 _MainColor;
            half _DistanceSoftness;
            #if OUTLINE_ON
            fixed _OutlineSoftness;
            fixed _OutlineWidth;
            fixed4 _OutlineColor;
            #endif  
            
            fixed _ShadowSoftness;
            half4 _ShadowColor;

            float _IsSceneViewHidden;
            float _IsSceneViewCamera;
            

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
                OUT.uv0 = float4(TRANSFORM_TEX(v.uv0.xy, _MainTex), v.uv0.z, v.uv0.w);
                OUT.uv1 = float4(TRANSFORM_TEX(v.uv1.xy, _AlphaTex), v.uv1.z, v.uv1.w);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 /
                    (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            
            fixed4 sdfFrag(v2f IN)
            {
                const float width = IN.uv0.z;
                // Compute density value, 1 - outer, 0 - inner
                const float density = 1 - tex2D(_MainTex, IN.uv0).a;
                const float fWidth = max(0.0001, fwidth(density) * _DistanceSoftness);// / maxTextureSize);

                const float alpha = smoothstep(width, width - fWidth, density);
                half4 sdfColor = _MainColor;
                sdfColor.a *= alpha;
                
                #if OUTLINE_ON
                const float outlineWidth = min(0.999, width + _OutlineWidth);
                if (outlineWidth > 0)
                {
                    const float halfSoftness = _OutlineSoftness * 0.5;
                    const float from = max(0, outlineWidth - halfSoftness);
                    const float to = min(1, outlineWidth + fWidth + halfSoftness);
                    half4 outlineColor = _OutlineColor;
                    half outlineAlpha = smoothstep(to, from, density);
                    outlineColor.a *= outlineAlpha;
                    
                    sdfColor = lerp(outlineColor, _MainColor, alpha);
                }
                #endif

                
                if (IN.uv0.w > 1)
                {
                    const float shadowWidth = min(0.999, width);
                    const float halfSoftness = _ShadowSoftness * 0.5;
                    const float from = max(0, shadowWidth - halfSoftness);
                    const float to = min(1, shadowWidth + fWidth + halfSoftness);
                    const float shadowAlpha = smoothstep(to, from, density);
                    sdfColor = _ShadowColor;
                    sdfColor.a *= shadowAlpha;
                }
                
                sdfColor *= IN.color;
                return sdfColor;
            }
            
            //Default UI frag 
            fixed4 frag(v2f IN) : SV_Target
            {
                if (_IsSceneViewHidden > 0 && _IsSceneViewCamera > 0)
                {
                    discard;
                }
                
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;
                
                
                half4 color;

                // Render sdf layer
                if (IN.uv0.w > 0)
                {
                    color = sdfFrag(IN);
                }
                else
                {
                    color = IN.color * (tex2D(_MainTex, IN.uv0) + _TextureSampleAdd);
                    
                    // This block handles crisp edge of the main layer, uses sdf to sample image edge
                    // Render regular layer with multi texture setup
                    // _MainTex = source, _AlphaTex = sdfTexture for crisp edge
                    #if CRISP_EDGE_ON
                    float sdf = tex2D(_AlphaTex, IN.uv1).a;
                    float density = 1 - sdf;
                    float fWidth = max(0.0001, fwidth(density) * _DistanceSoftness);

                    const float width = 0.5;
                    float sdfOut = smoothstep(width, width - fWidth, density);
                    color.a = sdfOut;
                    #endif
                }
                
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
    CustomEditor "nickeltin.SDF.Editor.SDFShaderGUI"
}
