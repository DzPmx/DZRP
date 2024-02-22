Shader "Hidden/DZRP/PostFXStack"
{
    SubShader
    {
        Cull off
        ZTest Always
        ZWrite OFf
        HLSLINCLUDE
        #include "ShaderLibrary/DZRP_Common.hlsl"
        #include "ShaderLibrary/PostFXStackPasses.hlsl"
        ENDHLSL

        pass
        {
            name "Bloom Horizontal"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        
        pass
        {
            name "Bloom Vertical"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
        
        pass
        {
            name "Bloom Combine"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomCombinePassFragment
            ENDHLSL
        }

        pass
        {
            name "Bloom Prefilter"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterPassFragment
            ENDHLSL
        }
        
        pass
        {
            name "Copy"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            ENDHLSL
        }
    }
}