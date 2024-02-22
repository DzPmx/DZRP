using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/DZRenderPipeline Asset")]
public class DZRenderAsset: RenderPipelineAsset
{
    //序列化三个字段使其可见
    [SerializeField] private bool useDynamicBatching = true,
        useGPUInstancing = true,
        useSRPBatcher = true,
        useLightsPerObject = true;
    [SerializeField]private ShadowSettings shadowSettings = default;
    [SerializeField]private PostFXSettings postFXSettings = default;
    
    /// <summary>
    /// RenderPipelineAsset是一个抽象类 ，继承自它需要实现抽象成员函数CreatePipeLine ，该方法创建管线资产的实例
    /// </summary>
    protected override RenderPipeline CreatePipeline()  
    {
        return new DZRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher,useLightsPerObject,
            shadowSettings,postFXSettings);
    }
}
