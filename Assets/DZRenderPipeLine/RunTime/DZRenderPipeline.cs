using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

public partial class DZRenderPipeline: RenderPipeline
{
    //像机渲染器实例，管理所有摄像机的渲染
    private CameraRenderer renderer = new CameraRenderer();
    private bool useDynamicBatch, useGpuInstancing, useLightsPerObject;
    private ShadowSettings shadowSettings;
    private PostFXSettings postFXSettings;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public DZRenderPipeline(bool useDynamicBatch,bool useGpuInstancing,bool useSRPBatcher,
        bool useLightsPerObject,ShadowSettings shadowSettings,PostFXSettings postFXSettings)
    {
        this.useGpuInstancing = useGpuInstancing;
        this.useDynamicBatch = useDynamicBatch;
        this.shadowSettings = shadowSettings;
        this.useLightsPerObject = useLightsPerObject;
        this.postFXSettings = postFXSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
    }
    /// <summary>
    /// 继承自RenderPipeline抽象类 需要实现Render抽象方法
    /// </summary>
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
           renderer.Render(context,cameras[i],useDynamicBatch,useGpuInstancing,
               shadowSettings,useLightsPerObject,postFXSettings); //使用之前声明的CameraRenderer类来管理每一个相机的渲染逻辑
        }
    }
}