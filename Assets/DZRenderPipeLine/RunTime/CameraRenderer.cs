using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 相机渲染管理类 每个相机都是独立渲染，因此不如让一个专门管理相机的类来负责
/// </summary>
public partial class CameraRenderer 
{
    private ScriptableRenderContext context;
    private Camera camera;
    private CommandBuffer cmd = new CommandBuffer {  };//命令缓冲区
    private CullingResults cullingResults;
    private Lighting lighting = new Lighting();
    
    private static ShaderTagId 
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagID = new ShaderTagId("DZRPLit");

    private PostFXStack postFXStack= new PostFXStack();

    private static int frameBufferID = Shader.PropertyToID("_CameraFrameBuffer");

    /// <summary>
    /// 单个相机的渲染流程主体
    /// </summary>
    public void Render(
        ScriptableRenderContext context,Camera camera,bool useDynamicBatching,bool useGpuInstancing,
        ShadowSettings shadowSettings,bool useLightsPerObject,PostFXSettings postFXSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))    //如果没有获取相机的剔除参数，直接返回不渲染
        {
            return;
        }
        cmd.BeginSample(SampleName);
        ExcuteCMD();
        lighting.Setup(context,cullingResults,shadowSettings,useLightsPerObject); 
        postFXStack.Setup(context,camera,postFXSettings);
        cmd.EndSample(SampleName);
        Setup();
        DrawVisibilityGeometry(useDynamicBatching,useGpuInstancing,useLightsPerObject);
        DrawUnsupportedShaders();
        DrawGizmosBeforeFX();
        if (postFXStack.isActive)
        {
            postFXStack.Render(frameBufferID);
        }
        DrawGizmosAfterFX();
        CleanUp();
        Submit();
    }

    /// <summary>
    /// 绘制不透明物体
    /// </summary>
    void DrawVisibilityGeometry(bool useDynamicBatching, bool useGPUInstancing,bool useLightsPerObject)
    {
        PerObjectData lightsPerObjectFlags =
            useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //由近到远 绘制不透明物体
        SortingSettings sortingSettings = new SortingSettings(camera){criteria = SortingCriteria.CommonOpaque};  //排序设置 排序方式是不透明物体的排序方式
        
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings) //绘制设置 指定lightmode 和排序设置
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
                perObjectData = PerObjectData.ReflectionProbes |
                                PerObjectData.Lightmaps | PerObjectData.ShadowMask|
                                PerObjectData.LightProbe| PerObjectData.OcclusionProbe|
                                PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume|
                                lightsPerObjectFlags
            };
        drawingSettings.SetShaderPassName(1,litShaderTagID);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);//过滤设置 只绘制不透明物体
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);//先绘制不透明物体
        //绘制天空盒
        context.DrawSkybox(camera);//绘制天空球
        //由远到近绘制半透明透明物体
        sortingSettings.criteria = SortingCriteria.CommonTransparent;   
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange=RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);

    }
    /// <summary>
    /// 初始化相机信息，包括视图、投影、裁剪面信息等
    /// 清除之前的渲染目标以保证绘制正确
    /// 在profiler和frame debugger中加入监视
    /// </summary>
    void Setup() 
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        if (postFXStack.isActive)
        {
            if (flags>CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
                
            }
            cmd.GetTemporaryRT(frameBufferID,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Bilinear,RenderTextureFormat.Default);
            cmd.SetRenderTarget(frameBufferID,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        }
        cmd.ClearRenderTarget(flags<=CameraClearFlags.Depth,flags<=CameraClearFlags.Color,flags == CameraClearFlags.Color ?
            camera.backgroundColor.linear : Color.clear);   
        cmd.BeginSample(SampleName);    
        ExcuteCMD();
    }

    /// <summary>
    ///获得相机的剔除参数/阴影距离 并执行剔除
    /// </summary>
    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);  
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 提交Context命令并结束监视
    /// </summary>
    void Submit()   
    {
        cmd.EndSample(SampleName);  
        ExcuteCMD();
        context.Submit();
    }
    
    /// <summary>
    /// 提交commandbuffer
    /// </summary>
    void ExcuteCMD()
    {
        context.ExecuteCommandBuffer(cmd);  
        cmd.Clear();   
    }

    void CleanUp()
    {
        lighting.CleanUp();
        if (postFXStack.isActive)
        {
            cmd.ReleaseTemporaryRT(frameBufferID);
        }
    }
}
