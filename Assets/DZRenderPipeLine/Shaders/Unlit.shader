Shader "DZ RenderPipeLine/Unlit"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}

        [HDR]_BaseCol("BaseCol",Color)=(1.0,1.0,1.0,1.0)

        [Toggle(_CLIPING)]_Cliping("Alpha CLiping On",float)=0
        _AlphaClip("AlphaClip",range(0.0,1.0))=0.5

        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",float)=1
        [Enum(UnityEngine.Rendering.BlendMode)]_DestBlend("Dst Blend",float)=0

        [Enum(off,0,on,1)]_Zwrite("ZWrite",float)=1

    }
    SubShader
    {
        HLSLINCLUDE
        #include "ShaderLibrary/DZRP_Common.hlsl"
        #include "ShaderLibrary/DZRP_Unlit_Input.hlsl"
        ENDHLSL

        pass
        {
            name "UnlitPass"
            Blend [_SrcBlend] [_DestBlend]
            Zwrite [_Zwrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DZUnlitPassVertex
            #pragma fragment DZUnlitPassFrag
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPING
            #include "ShaderLibrary/DZRPUnlitPass.hlsl"
            ENDHLSL
        }
    }

}