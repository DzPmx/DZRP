using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Profiling;


public partial class CameraRenderer //每个相机都是独立渲染，因此不如让一个专门管理相机的类来负责
{
    partial void DrawUnsupportedShaders();//分部类 方法名绘制不支持的Shader
    
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();
#if UNITY_EDITOR
    private string SampleName { get; set; }
    
    private static Material errorMaterial;
    
    /// <summary>
    /// 不支持的Shader
    /// </summary>
    private static ShaderTagId[] legacyShaderTagIDs =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    
    /// <summary>
    /// 编辑器模式下的buffer监视设置
    /// </summary>
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");    
        cmd.name =SampleName= camera.name;
        Profiler.EndSample();   
    }
    
    /// <summary>
    /// Scene视图渲染UI
    /// </summary>
    partial void PrepareForSceneWindow()   
    {
        if (camera.cameraType==CameraType.SceneView)    
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    /// <summary>
    /// 绘制Scene视图的Gizmos
    /// </summary>
    partial void DrawGizmosBeforeFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects); //图像效果前
        }
    }

    partial void DrawGizmosAfterFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
        }
    }
    /// <summary>
    /// 绘制不支持的错误Shader 分部类包裹在Editor里，这样方法的实现会在build宝中被略过
    /// </summary>
    partial void DrawUnsupportedShaders()   
    {
        if (errorMaterial==null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        DrawingSettings drawingSettings = new DrawingSettings(legacyShaderTagIDs[0], new SortingSettings(camera)){overrideMaterial = errorMaterial};
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        for (int i = 0; i < legacyShaderTagIDs.Length; i++)
        {
            drawingSettings.SetShaderPassName(i,legacyShaderTagIDs[i]);
        }
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
    }
    #else
    const string SampleName=bufferName;
#endif
}
