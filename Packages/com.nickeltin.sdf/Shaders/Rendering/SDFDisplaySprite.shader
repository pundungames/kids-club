Shader "nickeltin/SDF/Sprite" 
{
    Properties 
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        
        // SDF Properties
        _MainColor("Main Color", Color) = (1,1,1,1)
        
        [Toggle(OUTLINE_ON)] _EnableOutline("Enable Outline", Float) = 0
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0,1)) = 0.1    
        _OutlineSoftness("Outline Softness", Range(0,1)) = 0.1
        
        _ShadowColor("Shadow Color", Color) = (1,1,1,1)
        _ShadowWidth("Shadow Width", Range(0,1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0,1)) = 0
        
        _DistanceSoftness("Distance Softness", Range(0,1)) = 1
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "SDF Sprite"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile_local _ OUTLINE_ON

            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            // SDF textures and properties
            // _MainTex is declared in UnitySprites.cginc
            float4 _MainTex_ST;
            
            half4 _MainColor;
            half _DistanceSoftness;
            
            #if OUTLINE_ON
            fixed _OutlineSoftness;
            fixed _OutlineWidth;
            fixed4 _OutlineColor;
            #endif
            
            fixed _ShadowSoftness;
            fixed _ShadowWidth;
            half4 _ShadowColor;
            

            struct appdata_sdf
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 texcoord : TEXCOORD0; // xy = sprite UV, z = SDF width, w = layer type (0=main, 1=outline, 2=shadow)
                float2 texcoord1 : TEXCOORD1; // xy = SDF texture UV
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_sdf
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float4 texcoord : TEXCOORD0; // xy = sprite UV, z = SDF width, w = layer type
                float2 sdfUV    : TEXCOORD1; // SDF texture UV
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_sdf vert(appdata_sdf v)
            {
                v2f_sdf OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(v.vertex);
                #else
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                #endif
                
                OUT.texcoord = float4(TRANSFORM_TEX(v.texcoord.xy, _MainTex), v.texcoord.z, v.texcoord.w);
                OUT.sdfUV = TRANSFORM_TEX(v.texcoord1, _MainTex);
                OUT.color = v.color * _Color * _RendererColor;
                
                return OUT;
            }

            fixed4 frag(v2f_sdf IN) : SV_Target
            {
                // Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                // The incoming alpha could have numerical instability, which makes it very sensible to
                // HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;
                
                // Sample SDF texture (stored in alpha channel)
                float sdf = tex2D(_MainTex, IN.sdfUV).a;
                
                // Compute density value, 1 - outer, 0 - inner
                const float density = 1 - sdf;
                const float width = IN.texcoord.z;
                const float fWidth = max(0.0001, fwidth(density) * _DistanceSoftness);
                
                const float layerType = IN.texcoord.w;
                half4 color;
                
                // Shadow layer (layerType >= 2 or > 1)
                if (layerType > 1)
                {
                    const float shadowWidth = min(0.999, width);
                    const float halfSoftness = _ShadowSoftness * 0.5;
                    const float from = max(0, shadowWidth - halfSoftness);
                    const float to = min(1, shadowWidth + fWidth + halfSoftness);
                    const float shadowAlpha = smoothstep(to, from, density);
                    color = _ShadowColor;
                    color.a *= shadowAlpha;
                }
                // Main SDF layer
                else
                {
                    const float alpha = smoothstep(width, width - fWidth, density);
                    color = _MainColor;
                    color.a *= alpha;
                    
                    #if OUTLINE_ON
                    // Outline layer (layerType == 1) or render outline on main layer
                    if (layerType > 0.5 || _OutlineWidth > 0)
                    {
                        const float outlineWidth = min(0.999, width + _OutlineWidth);
                        if (outlineWidth > 0)
                        {
                            const float halfSoftness = _OutlineSoftness * 0.5;
                            const float from = max(0, outlineWidth - halfSoftness);
                            const float to = min(1, outlineWidth + fWidth + halfSoftness);
                            half4 outlineColor = _OutlineColor;
                            half outlineAlpha = smoothstep(to, from, density);
                            outlineColor.a *= outlineAlpha;
                            
                            // If this is the outline layer (layerType == 1), use outline color directly
                            // Otherwise blend outline with main color
                            if (layerType > 0.5)
                            {
                                color = outlineColor;
                            }
                            else
                            {
                                color = lerp(outlineColor, _MainColor, alpha);
                            }
                        }
                    }
                    #endif
                }
                
                color *= IN.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}

