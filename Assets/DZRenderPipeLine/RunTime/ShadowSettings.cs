using UnityEngine;
using System;

[Serializable]
public class ShadowSettings
{
    
    [Tooltip("阴影最大距离")][Min(0.0001f)] public float maxDistance = 100f;
    [Tooltip("超出最远距离渐消比例")][Range(0.001f, 1f)] public float distanceFade = 0.1f;


    public enum FilterMode
    {
        PCF2x2,
        PCF3x3,
        PCF5x5,
        PCF7x7,
    }
    /// <summary>
    /// 阴影纹理大小
    /// </summary>
    public enum TextutreSize
    {
        _256=256, _512=512, _1024=1024,
        _2048=2048, _4096=4096, _8192=8192,
    }
    
    /// <summary>
    /// 级联阴影混合模式 
    /// </summary>

    /// <summary>
    /// 直射光阴影属性
    /// </summary>
    [Serializable] 
    public struct Directional
    {
        public TextutreSize atlasSize;
        public FilterMode filterMode;
        [Range(1, 4)] public int cascadeCount;
        [Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        [Tooltip("级联阴影边界渐消比例")] [Range(0.001f, 1f)]
        public float cascadeFade;
        public enum CascadeBlendMode
        {
            Hard,Soft,Dither
        }

        public CascadeBlendMode cascadeBlend;
        
        //属性
        public Vector3 CascaeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

    }
    [Serializable]
    public struct Other
    {
        public TextutreSize atlasSize;
        public FilterMode filterMode;
    }

    public Directional directional = new Directional()
    {
        atlasSize = TextutreSize._4096,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f,
        cascadeBlend = Directional.CascadeBlendMode.Dither,
        filterMode = FilterMode.PCF7x7,
        
    };

    public Other other = new Other()
    {
        atlasSize = TextutreSize._1024,
        filterMode = FilterMode.PCF2x2
    };
}
