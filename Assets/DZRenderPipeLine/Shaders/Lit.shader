Shader "DZ RenderPipeLine/Lit"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}
        _BaseCol("BaseCol",Color)=(0.5,0.5,0.5,1.0)
        _Metallic("Metallic",range(0,1))=0
        _Smoothness("Smoothness",range(0,1))=0.5
        _Fresnel ("Fresnel", Range(0, 1)) = 1
        
        [NoScaleOffset]_EmissionMap("EmissionMap",2D)="white"{}
        [HDR]_EmissionCol("EmissionCol",Color)=(0.0,0.0,0.0,0.0)
        
        [Toggle(_CLIPPING)]_Clipping("Alpha CLiping On",float)=0
        [Toggle(_PREMULTIPLY_ALPHA)]_PremultiplyAlpha("Premultiply Alpha",float)=0
        _AlphaClip("AlphaClip",range(0.0,1.0))=0.5
        
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",float)=1
        [Enum(UnityEngine.Rendering.BlendMode)]_DestBlend("Dst Blend",float)=0
        
        [Enum(off,0,on,1)]_Zwrite("ZWrite",float)=1
        [KeywordEnum(On,Clip,Dither,Off)]_Shadows("Shadows",float)=0
        
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
        [HideInInspector] _Cutoff("_Cutoff for Lightmap", range(0.0,1.0)) = 0.0
        
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
        
    }
    SubShader
    {
        HLSLINCLUDE
        #include "ShaderLibrary/DZRP_Common.hlsl"
        #include "ShaderLibrary/DZRP_Lit_Input.hlsl"
        ENDHLSL
        pass
        {
            Tags
            {
                "LightMode"="DZRPLit"
                }
            name "ForwardLit"
            Blend [_SrcBlend] [_DestBlend]
            Zwrite [_Zwrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DZLitPassVertex
            #pragma fragment DZLitPassFrag
            #pragma multi_compile_instancing
            #pragma shader_feature _ _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _RECEIVE_SHADOWS
            
            //Shadows
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #include "ShaderLibrary/DZRPLitPass.hlsl"
            ENDHLSL
        }
        
        pass
        {
            tags
            {
               "LightMode"="ShadowCaster"
            }
            name "ShadowCasterPass"
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #include "ShaderLibrary/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        pass
        {
            tags
            {
                "LightMode"="Meta"
            }
            Cull off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "ShaderLibrary/DZRP_MetaPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "DZRPLitGUI"
}
